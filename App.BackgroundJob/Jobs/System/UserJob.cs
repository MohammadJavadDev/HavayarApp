using Common.Attributes;
using Data;
using Data.Contracts;
using Entities.App.Gnr;
using Entities.App.Hcm;
using Entities.Auth;
using Entities.Base;
using Entities.Hts.Gnr;
using Entities.Hts.Hrm;
using Entities.Rahkaran.SYS3;
using Microsoft.EntityFrameworkCore;
using Services.Job;

namespace App.BackgroundJob.Jobs.System
{
	public class UserJob(RahkaranDbContext Rdb, HtsDbContext htsDb, IUnitOfWork unitOfWork, ApplicationDbContext appContext)
	{
		[JobHandler("افزودن کاربران از HTS")]
		public async Task AddContractFromRahkaran(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("شروع همگام‌سازی تمام کاربران HTS", cn);

				var htsUsers = await htsDb.Hts_Gnr_Users.AsNoTracking()
					.Where(u => (u.Username != null && u.Username.Trim() != "")
						|| (u.ActiveDirectoryUsername != null && u.ActiveDirectoryUsername.Trim() != ""))
					.ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد کاربران HTS: {htsUsers.Count}", cn);

				var rahkaranUsers = await Rdb.RahkaranUsers.AsNoTracking()
					.Where(u => u.Type == 2
						&& u.DomainUserName != null
						&& u.DomainUserName.Trim() != "")
					.ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد کاربران دامنه راهکاران برای نگاشت: {rahkaranUsers.Count}", cn);

				var rahkaranByUsername = rahkaranUsers
					.Select(u => new { User = u, Key = NormalizeUsername(u.DomainUserName) })
					.Where(x => x.Key != null)
					.GroupBy(x => x.Key!, StringComparer.OrdinalIgnoreCase)
					.ToDictionary(
						g => g.Key,
						g => g.OrderBy(x => x.User.Status == 1 ? 0 : 1).First().User,
						StringComparer.OrdinalIgnoreCase);

				var htsPersonelById = (await htsDb.Hts_HRM_Personels.AsNoTracking().ToListAsync(cn))
					.GroupBy(p => p.Personel_ID)
					.ToDictionary(g => g.Key, g => g.First());

				var appParties = await unitOfWork.Repository<Party>().TableNoTracking
					.Where(p => p.HamkaranId.HasValue && p.Id.HasValue)
					.Select(p => new { p.Id, p.HamkaranId, p.FullName })
					.ToListAsync(cn);

				var partiesByHamkaranId = appParties
					.GroupBy(p => p.HamkaranId!.Value)
					.ToDictionary(g => g.Key, g => new PartyInfo(g.First().Id!.Value, g.First().FullName));

				var partiesById = appParties
					.GroupBy(p => p.Id!.Value)
					.ToDictionary(g => g.Key, g => new PartyInfo(g.First().Id!.Value, g.First().FullName));

				await jobLogger?.LogInfoAsync($"تعداد Party با HamkaranId: {partiesByHamkaranId.Count}", cn);

				var appPersonels = await unitOfWork.Repository<Personel>().TableNoTracking
					.Where(p => p.Code != null && p.Code != "")
					.Select(p => new { p.Code, p.PartyId, p.OrgUnitId })
					.ToListAsync(cn);

				var personelsByCode = appPersonels
					.GroupBy(p => p.Code!.Trim(), StringComparer.OrdinalIgnoreCase)
					.ToDictionary(
						g => g.Key,
						g => new PersonelInfo(g.First().PartyId, g.First().OrgUnitId),
						StringComparer.OrdinalIgnoreCase);

				await jobLogger?.LogInfoAsync($"تعداد پرسنل با کد: {personelsByCode.Count}", cn);

				var appUsers = await appContext.Users.ToListAsync(cn);
				var usersByUsername = new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase);
				var usersByHamkaranId = new Dictionary<long, User>();

				foreach (var user in appUsers)
				{
					var existingUsernameKey = NormalizeUsername(user.Username);
					if (existingUsernameKey != null)
						usersByUsername.TryAdd(existingUsernameKey, user);

					if (user.HamkaranId.HasValue)
						usersByHamkaranId.TryAdd(user.HamkaranId.Value, user);
				}

				await jobLogger?.LogInfoAsync($"تعداد کاربران موجود در سیستم: {appUsers.Count}", cn);

				var uniqueByUsername = new Dictionary<string, HtsUserRow>(StringComparer.OrdinalIgnoreCase);
				var skippedNoUsername = 0;
				var duplicateUsernameCount = 0;

				foreach (var htsUser in htsUsers)
				{
					var rahkaranUser = FindRahkaranUser(htsUser, rahkaranByUsername);
					var usernameKey = ResolveUsernameKey(htsUser, rahkaranUser);
					if (usernameKey == null)
					{
						skippedNoUsername++;
						continue;
					}

					var row = new HtsUserRow(htsUser, rahkaranUser, usernameKey);
					if (!uniqueByUsername.TryGetValue(usernameKey, out var existing))
					{
						uniqueByUsername[usernameKey] = row;
						continue;
					}

					duplicateUsernameCount++;
					if (Prefer(row, existing))
						uniqueByUsername[usernameKey] = row;
				}

				await jobLogger?.LogInfoAsync(
					$"کاربران یکتا برای انتقال: {uniqueByUsername.Count} — یوزرنیم تکراری HTS: {duplicateUsernameCount} — بدون یوزرنیم: {skippedNoUsername}",
					cn);

				var createdCount = 0;
				var updatedCount = 0;
				var missingPartyCount = 0;
				var missingPersonelCount = 0;
				var withRahkaranCount = 0;

				foreach (var row in uniqueByUsername.Values)
				{
					var htsUser = row.HtsUser;
					var rahkaranUser = row.RahkaranUser;
					var usernameKey = row.UsernameKey;

					if (rahkaranUser != null)
						withRahkaranCount++;

					var party = ResolveParty(rahkaranUser, htsUser, htsPersonelById, personelsByCode, partiesByHamkaranId, partiesById);
					var personel = ResolveAppPersonel(htsUser, htsPersonelById, personelsByCode);
					var displayUsername = ResolveDisplayUsername(htsUser, rahkaranUser);

					if (!party.Found)
					{
						missingPartyCount++;
						if (rahkaranUser?.PartyRef != null || htsUser.Personel_FK.HasValue)
						{
							await jobLogger?.LogWarningAsync(
								$"Party برای کاربر {displayUsername} (HTS User_ID={htsUser.User_ID}, PartyRef={rahkaranUser?.PartyRef}) یافت نشد",
								0,
								cn);
						}
					}

					if (htsUser.Personel_FK.HasValue && personel == null)
					{
						missingPersonelCount++;
						await jobLogger?.LogWarningAsync(
							$"پرسنل برای کاربر {displayUsername} (HTS Personel_FK={htsUser.Personel_FK}) یافت نشد",
							0,
							cn);
					}

					var nameFa = FirstNonEmpty(party.FullName, htsUser.FullName);
					var name = FirstNonEmpty(rahkaranUser?.Name, htsUser.FullName, displayUsername) ?? displayUsername;
					var email = $"{displayUsername}@havayar.com";
					var orgUnitId = personel?.OrgUnitId;
					var isActive = htsUser.IsActive ? IsActiveEnum.Active : IsActiveEnum.DeActive;
					var authorizationType = rahkaranUser != null || !string.IsNullOrWhiteSpace(htsUser.ActiveDirectoryUsername)
						? AuthorizationTypeEnum.ActiveDirectory
						: AuthorizationTypeEnum.Custom;

					User? existUser = null;
					if (rahkaranUser != null)
						usersByHamkaranId.TryGetValue(rahkaranUser.UserID, out existUser);

					if (existUser == null)
						usersByUsername.TryGetValue(usernameKey, out existUser);

					if (existUser == null)
					{
						var user = new User
						{
							Username = displayUsername,
							Name = name,
							NameFa = nameFa,
							Roles = [],
							RoleIds = [],
							AuthorizationType = authorizationType,
							HamkaranId = rahkaranUser?.UserID,
							IsActive = isActive,
							Email = email,
							PartyId = party.Id,
							OrgUnitId = orgUnitId
						};

						appContext.Users.Add(user);
						usersByUsername[usernameKey] = user;
						if (user.HamkaranId.HasValue)
							usersByHamkaranId[user.HamkaranId.Value] = user;
						createdCount++;
						continue;
					}

					var updated = false;

					if (rahkaranUser != null && existUser.HamkaranId != rahkaranUser.UserID)
					{
						existUser.HamkaranId = rahkaranUser.UserID;
						updated = true;
					}

					if (party.Id.HasValue && existUser.PartyId != party.Id)
					{
						existUser.PartyId = party.Id;
						updated = true;
					}

					if (orgUnitId.HasValue && existUser.OrgUnitId != orgUnitId)
					{
						existUser.OrgUnitId = orgUnitId;
						updated = true;
					}

					if (!string.IsNullOrWhiteSpace(nameFa) && existUser.NameFa != nameFa)
					{
						existUser.NameFa = nameFa;
						updated = true;
					}

					if (string.IsNullOrWhiteSpace(existUser.Name) && !string.IsNullOrWhiteSpace(name))
					{
						existUser.Name = name;
						updated = true;
					}

					if (string.IsNullOrWhiteSpace(existUser.Email))
					{
						existUser.Email = email;
						updated = true;
					}

					if (existUser.IsActive != isActive)
					{
						existUser.IsActive = isActive;
						updated = true;
					}

					if (existUser.HamkaranId.HasValue)
						usersByHamkaranId.TryAdd(existUser.HamkaranId.Value, existUser);

					if (updated)
						updatedCount++;
				}

				await appContext.SaveChangesAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد کاربران جدید: {createdCount}", cn);
				await jobLogger?.LogInfoAsync($"تعداد کاربران به‌روزرسانی‌شده: {updatedCount}", cn);
				await jobLogger?.LogInfoAsync($"منطبق با راهکاران: {withRahkaranCount}", cn);
				await jobLogger?.LogInfoAsync($"بدون Party: {missingPartyCount} — بدون پرسنل: {missingPersonelCount}", cn);
				await jobLogger?.LogInfoAsync("عملیات همگام‌سازی کاربران با موفقیت انجام شد", cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
				throw;
			}
		}

		private static RahkaranUser? FindRahkaranUser(
			Hts_Gnr_User htsUser,
			Dictionary<string, RahkaranUser> rahkaranByUsername)
		{
			var usernameKey = NormalizeUsername(htsUser.Username);
			if (usernameKey != null && rahkaranByUsername.TryGetValue(usernameKey, out var byUsername))
				return byUsername;

			var adKey = NormalizeUsername(htsUser.ActiveDirectoryUsername);
			if (adKey != null && rahkaranByUsername.TryGetValue(adKey, out var byAd))
				return byAd;

			return null;
		}

		private static string? ResolveUsernameKey(Hts_Gnr_User htsUser, RahkaranUser? rahkaranUser)
		{
			return NormalizeUsername(rahkaranUser?.DomainUserName)
				?? NormalizeUsername(htsUser.Username)
				?? NormalizeUsername(htsUser.ActiveDirectoryUsername);
		}

		private static string ResolveDisplayUsername(Hts_Gnr_User htsUser, RahkaranUser? rahkaranUser)
		{
			if (!string.IsNullOrWhiteSpace(rahkaranUser?.DomainUserName))
				return rahkaranUser.DomainUserName.Trim();

			if (!string.IsNullOrWhiteSpace(htsUser.Username))
				return htsUser.Username.Trim();

			return htsUser.ActiveDirectoryUsername!.Trim();
		}

		private static bool Prefer(HtsUserRow candidate, HtsUserRow current)
		{
			var candidateScore = (candidate.RahkaranUser != null ? 4 : 0)
				+ (candidate.HtsUser.IsActive ? 2 : 0)
				+ (candidate.HtsUser.Personel_FK.HasValue ? 1 : 0);
			var currentScore = (current.RahkaranUser != null ? 4 : 0)
				+ (current.HtsUser.IsActive ? 2 : 0)
				+ (current.HtsUser.Personel_FK.HasValue ? 1 : 0);

			if (candidateScore != currentScore)
				return candidateScore > currentScore;

			return candidate.HtsUser.User_ID < current.HtsUser.User_ID;
		}

		private static PartyInfo ResolveParty(
			RahkaranUser? rahkaranUser,
			Hts_Gnr_User htsUser,
			Dictionary<short, Hts_HRM_Personel> htsPersonelById,
			Dictionary<string, PersonelInfo> personelsByCode,
			Dictionary<long, PartyInfo> partiesByHamkaranId,
			Dictionary<long, PartyInfo> partiesById)
		{
			if (rahkaranUser?.PartyRef is long partyRef
				&& partiesByHamkaranId.TryGetValue(partyRef, out var partyByHamkaran))
			{
				return partyByHamkaran with { Found = true };
			}

			var personel = ResolveAppPersonel(htsUser, htsPersonelById, personelsByCode);
			if (personel?.PartyId is long personelPartyId)
			{
				if (partiesById.TryGetValue(personelPartyId, out var partyById))
					return partyById with { Found = true };

				return new PartyInfo(personelPartyId, null, true);
			}

			return PartyInfo.Missing;
		}

		private static PersonelInfo? ResolveAppPersonel(
			Hts_Gnr_User htsUser,
			Dictionary<short, Hts_HRM_Personel> htsPersonelById,
			Dictionary<string, PersonelInfo> personelsByCode)
		{
			if (!htsUser.Personel_FK.HasValue)
				return null;

			if (!htsPersonelById.TryGetValue(htsUser.Personel_FK.Value, out var htsPersonel))
				return null;

			if (!htsPersonel.Hamkaran_Personel_FK.HasValue)
				return null;

			var code = htsPersonel.Hamkaran_Personel_FK.Value.ToString();
			return personelsByCode.TryGetValue(code, out var appPersonel) ? appPersonel : null;
		}

		private static string? NormalizeUsername(string? value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return null;

			var trimmed = value.Trim();
			var slash = trimmed.LastIndexOf('\\');
			if (slash >= 0 && slash < trimmed.Length - 1)
				trimmed = trimmed[(slash + 1)..];

			var at = trimmed.IndexOf('@');
			if (at > 0)
				trimmed = trimmed[..at];

			return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed.ToLowerInvariant();
		}

		private static string? FirstNonEmpty(params string?[] values)
		{
			foreach (var value in values)
			{
				if (!string.IsNullOrWhiteSpace(value))
					return value.Trim();
			}

			return null;
		}

		private sealed record HtsUserRow(Hts_Gnr_User HtsUser, RahkaranUser? RahkaranUser, string UsernameKey);

		private sealed record PartyInfo(long? Id, string? FullName, bool Found = true)
		{
			public static readonly PartyInfo Missing = new(null, null, false);
		}

		private sealed record PersonelInfo(long? PartyId, long? OrgUnitId);
	}
}
