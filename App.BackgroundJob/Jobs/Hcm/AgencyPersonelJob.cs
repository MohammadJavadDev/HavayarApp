using Common.Attributes;
using Data;
using Data.Contracts;
using Entities.App.Hcm;
using Entities.App.Sale;
using Entities.Auth;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Services.Job;

namespace App.BackgroundJob.Jobs.Hcm
{
	/// <summary>
	/// پرسنل نمایندگی در HTS شناسه راهکاران ندارد و همگام‌سازی پرسنل آن‌ها را نمی‌آورد.
	/// بدون ردیف Hcm.Personel در انتخاب کارشناس درخواست پشتیبانی دیده نمی‌شوند و گزارش کار به کاربرشان وصل نمی‌شود.
	/// گروه‌های HTS: 53 «نمایندگی ها» و 102 «نمایندگان فروش».
	/// </summary>
	public class AgencyPersonelJob(HtsDbContext htsDb, IUnitOfWork unitOfWork, ApplicationDbContext app)
	{
		private static readonly short[] AgencyGroupIds = [53, 102];

		[JobHandler("همگام‌سازی پرسنل نمایندگی خدمات پس از فروش")]
		public async Task SyncAgencyPersonnel(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			await jobLogger?.LogInfoAsync("شروع همگام‌سازی پرسنل نمایندگی", cn);

			var sources = await LoadSources(cn);
			await jobLogger?.LogInfoAsync($"نمایندگی فعال برای بررسی: {sources.Count}", cn);
			if (sources.Count == 0)
				return;

			var usernames = sources.Select(s => s.Username).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			var portalUsers = await app.Users
				.Where(u => usernames.Contains(u.Username))
				.ToListAsync(cn);
			var usersByName = portalUsers
				.GroupBy(u => u.Username.Trim(), StringComparer.OrdinalIgnoreCase)
				.ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

			var hamkaranIds = sources.Where(s => s.CompanyHamkaranId.HasValue).Select(s => s.CompanyHamkaranId!.Value).Distinct().ToList();
			var companyParties = await unitOfWork.Repository<Entities.App.Gnr.Party>().Table
				.Where(p => p.HamkaranId != null && hamkaranIds.Contains(p.HamkaranId.Value) && p.Id != null)
				.Select(p => new PartyRow(p.Id!.Value, p.FullName, p.HamkaranId))
				.ToListAsync(cn);
			var partyByHamkaran = companyParties
				.GroupBy(p => p.HamkaranId!.Value)
				.ToDictionary(g => g.Key, g => g.OrderBy(x => x.Id).First());

			var plans = BuildPlans(sources, usersByName, partyByHamkaran);
			await AttachPartiesByName(plans, cn);

			var plannedPartyIds = plans.Where(p => p.PartyId.HasValue).Select(p => p.PartyId!.Value).Distinct().ToList();
			var partyOwners = await app.Users.AsNoTracking()
				.Where(u => u.Id != null && u.PartyId != null && plannedPartyIds.Contains(u.PartyId.Value))
				.Select(u => new { u.Id, u.PartyId })
				.ToListAsync(cn);

			var emails = plans.Select(p => p.Email).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			var codes = plans.Where(p => p.Code != null).Select(p => p.Code!).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			var partyIds = plans.Where(p => p.PartyId.HasValue).Select(p => p.PartyId!.Value).Distinct().ToList();
			var existing = await unitOfWork.Repository<Personel>().Table
				.Where(p =>
					(p.Email != null && emails.Contains(p.Email))
					|| (p.Code != null && codes.Contains(p.Code))
					|| (p.PartyId != null && partyIds.Contains(p.PartyId.Value)))
				.ToListAsync(cn);

			var takenEmails = new HashSet<string>(
				existing.Where(p => !string.IsNullOrWhiteSpace(p.Email)).Select(p => p.Email!.Trim()),
				StringComparer.OrdinalIgnoreCase);
			var takenCodes = new HashSet<string>(
				existing.Where(p => !string.IsNullOrWhiteSpace(p.Code)).Select(p => p.Code!.Trim()),
				StringComparer.OrdinalIgnoreCase);

			var created = new List<Personel>();
			var touchedIds = new List<long>();
			var updated = 0;
			var linkedUsers = 0;
			var mapped = new List<AgencyMap>();

			foreach (var plan in plans)
			{
				if (plan.PortalUser?.Id != null && plan.PortalUser.PartyId == null && plan.PartyId.HasValue
					&& !partyOwners.Any(o => o.PartyId == plan.PartyId && o.Id != plan.PortalUser.Id))
				{
					plan.PortalUser.PartyId = plan.PartyId;
					linkedUsers++;
				}

				var match = FindMatch(existing, plan);
				if (match != null && match.HamkaranId.HasValue)
				{
					mapped.Add(new AgencyMap(plan.HtsPersonelId, match.Id, plan.PortalUser?.Id, plan.Display));
					continue;
				}

				if (match != null)
				{
					if (Apply(match, plan, takenEmails, takenCodes))
					{
						updated++;
						if (match.Id.HasValue)
							touchedIds.Add(match.Id.Value);
					}
					mapped.Add(new AgencyMap(plan.HtsPersonelId, match.Id, plan.PortalUser?.Id, plan.Display));
					continue;
				}

				var personel = new Personel
				{
					Name = plan.Name,
					Family = plan.Family,
					FamilyDisplay = plan.Family,
					Code = plan.Code != null && takenCodes.Add(plan.Code) ? plan.Code : null,
					Email = takenEmails.Add(plan.Email) ? plan.Email : null,
					PartyId = plan.PartyId,
					IsActive = IsActiveEnum.Active,
					CreatedOnMiladiDateTime = DateTime.Now
				};
				if (personel.Code == null && plan.Code != null)
					plan.Code = null;
				created.Add(personel);
				existing.Add(personel);
				mapped.Add(new AgencyMap(plan.HtsPersonelId, null, plan.PortalUser?.Id, plan.Display, personel));
			}

			if (created.Count > 0)
				app.Set<Personel>().AddRange(created);

			await app.SaveChangesAsync(cn);
			touchedIds.AddRange(created.Where(p => p.Id.HasValue).Select(p => p.Id!.Value));
			await FillFullName(touchedIds.Distinct().ToList(), cn);

			var missionResult = await BackfillExpertMissions(mapped, jobLogger, cn);

			await jobLogger?.LogInfoAsync(
				$"پرسنل نمایندگی: جدید {created.Count}، به‌روز {updated}، کاربر وصل‌شده {linkedUsers}، اعزام جدید {missionResult.Inserted}، کارشناس وصل‌شده {missionResult.ExpertsLinked}",
				cn);
		}

		private async Task<List<AgencySource>> LoadSources(CancellationToken cn)
		{
			var memberIds = await htsDb.Hts_Gnr_UserGroupMembers.AsNoTracking()
				.Where(m => AgencyGroupIds.Contains(m.UserGroup_FK))
				.Select(m => m.User_FK)
				.Distinct()
				.ToListAsync(cn);

			var agencyNamedIds = await htsDb.Hts_HRM_Personels.AsNoTracking()
				.Where(p => p.IsActive == true && p.PrsName != null && p.PrsName.StartsWith("نمایندگی"))
				.Select(p => p.Personel_ID)
				.ToListAsync(cn);

			var htsUsers = await htsDb.Hts_Gnr_Users.AsNoTracking()
				.Where(u => u.IsActive && u.Username != null && u.Username.Trim() != "")
				.Where(u => memberIds.Contains(u.User_ID)
					|| (u.Personel_FK != null && agencyNamedIds.Contains(u.Personel_FK.Value)))
				.Select(u => new { u.User_ID, u.Username, u.Personel_FK, u.Company_FK })
				.ToListAsync(cn);

			var personelIds = htsUsers.Where(u => u.Personel_FK.HasValue).Select(u => u.Personel_FK!.Value).Distinct().ToList();
			var emails = htsUsers.Select(u => u.Username!.Trim().ToLowerInvariant() + "@havayar.com").Distinct().ToList();
			var htsPersonels = await htsDb.Hts_HRM_Personels.AsNoTracking()
				.Where(p => personelIds.Contains(p.Personel_ID)
					|| (p.PrsEmail != null && emails.Contains(p.PrsEmail.ToLower())))
				.ToListAsync(cn);

			var companyIds = htsUsers.Where(u => u.Company_FK.HasValue).Select(u => u.Company_FK!.Value).Distinct().ToList();
			var companies = await htsDb.Hts_Gnr_ManCompanies.AsNoTracking()
				.Where(c => companyIds.Contains(c.ManCompany_ID))
				.ToListAsync(cn);
			var companyById = companies.ToDictionary(c => c.ManCompany_ID);

			var list = new List<AgencySource>();
			foreach (var user in htsUsers.GroupBy(u => u.User_ID).Select(g => g.First()))
			{
				var username = user.Username!.Trim();
				var email = username.ToLowerInvariant() + "@havayar.com";
				var personel = user.Personel_FK.HasValue
					? htsPersonels.FirstOrDefault(p => p.Personel_ID == user.Personel_FK.Value)
					: htsPersonels.FirstOrDefault(p => string.Equals(p.PrsEmail?.Trim(), email, StringComparison.OrdinalIgnoreCase));
				companyById.TryGetValue(user.Company_FK ?? -1, out var company);
				var companyTitle = Meaningful(company?.CompanyName) ?? Meaningful(company?.FullName);
				list.Add(new AgencySource(
					user.User_ID,
					username,
					personel?.Personel_ID,
					Meaningful(personel?.PrsName),
					Meaningful(personel?.PrsFamily_Display) ?? Meaningful(personel?.PrsFamily),
					personel?.PrsCode?.Trim(),
					user.Company_FK,
					company?.Hamkaran_ManCompany_FK is int hamkaran and > 0 ? hamkaran : null,
					companyTitle));
			}

			return list;
		}

		private static List<AgencyPlan> BuildPlans(
			List<AgencySource> sources,
			Dictionary<string, User> usersByName,
			Dictionary<long, PartyRow> partyByHamkaran)
		{
			var companyCounts = sources
				.Where(s => s.CompanyId.HasValue)
				.GroupBy(s => s.CompanyId!.Value)
				.ToDictionary(g => g.Key, g => g.Count());

			var plans = new List<AgencyPlan>();
			foreach (var source in sources)
			{
				usersByName.TryGetValue(source.Username, out var portalUser);
				PartyRow? companyParty = null;
				if (source.CompanyHamkaranId.HasValue)
					partyByHamkaran.TryGetValue(source.CompanyHamkaranId.Value, out companyParty);

				var sole = source.CompanyId.HasValue && companyCounts[source.CompanyId.Value] == 1;
				var personalParty = portalUser?.PartyId is long partyId
					&& companyParty != null
					&& partyId != companyParty.Id;

				string? name;
				string? family;
				if (source.HtsPersonelId.HasValue && (source.PrsName != null || source.PrsFamily != null))
				{
					(name, family) = FitParts(source.PrsName, source.PrsFamily);
				}
				else if (source.CompanyTitle != null && (!personalParty || sole))
				{
					(name, family) = FitSingle(source.CompanyTitle);
				}
				else if (companyParty != null && (portalUser?.PartyId == null || portalUser.PartyId == companyParty.Id))
				{
					(name, family) = FitSingle(companyParty.FullName ?? source.Username);
				}
				else
				{
					(name, family) = FitSingle(Meaningful(portalUser?.NameFa) ?? Meaningful(portalUser?.Name) ?? source.Username);
				}

				var display = JoinName(name, family);
				long? targetParty = portalUser?.PartyId ?? companyParty?.Id;
				plans.Add(new AgencyPlan(
					source,
					portalUser,
					name,
					family,
					display,
					targetParty,
					source.Username.ToLowerInvariant() + "@havayar.com",
					source.PrsCode is { Length: > 0 and <= 20 } ? source.PrsCode : null));
			}

			return plans;
		}

		private async Task AttachPartiesByName(List<AgencyPlan> plans, CancellationToken cn)
		{
			var needed = plans
				.Where(p => p.PartyId == null && p.Display.Length > 0)
				.Select(p => p.Display)
				.Distinct()
				.ToList();
			if (needed.Count == 0)
				return;

			var found = await unitOfWork.Repository<Entities.App.Gnr.Party>().TableNoTracking
				.Where(p => p.FullName != null && p.Id != null && needed.Contains(p.FullName))
				.Select(p => new PartyRow(p.Id!.Value, p.FullName, p.HamkaranId))
				.ToListAsync(cn);
			var byName = found
				.GroupBy(p => Norm(p.FullName))
				.ToDictionary(
					g => g.Key,
					g => g.OrderByDescending(x => x.HamkaranId.HasValue).ThenBy(x => x.Id).First());

			foreach (var plan in plans.Where(p => p.PartyId == null))
			{
				if (!byName.TryGetValue(Norm(plan.Display), out var party))
					continue;
				plan.PartyId = party.Id;
			}
		}

		private static Personel? FindMatch(List<Personel> existing, AgencyPlan plan)
		{
			var owned = existing.FirstOrDefault(p =>
				p.HamkaranId == null && (
					string.Equals(p.Email?.Trim(), plan.Email, StringComparison.OrdinalIgnoreCase)
					|| (plan.Code != null && string.Equals(p.Code?.Trim(), plan.Code, StringComparison.OrdinalIgnoreCase)
						&& PersonNorm(p) == Norm(plan.Display))));
			if (owned != null)
				return owned;

			var sameParty = existing.FirstOrDefault(p =>
				plan.PartyId.HasValue && p.PartyId == plan.PartyId && PersonNorm(p) == Norm(plan.Display));
			if (sameParty != null)
				return sameParty;

			if (plan.Code == null)
				return null;

			return existing.FirstOrDefault(p =>
				string.Equals(p.Code?.Trim(), plan.Code, StringComparison.OrdinalIgnoreCase)
				&& PersonNorm(p) == Norm(plan.Display));
		}

		private static bool Apply(Personel personel, AgencyPlan plan, HashSet<string> takenEmails, HashSet<string> takenCodes)
		{
			var changed = false;
			if (personel.Name != plan.Name)
			{
				personel.Name = plan.Name;
				changed = true;
			}

			if (personel.Family != plan.Family)
			{
				personel.Family = plan.Family;
				personel.FamilyDisplay = plan.Family;
				changed = true;
			}

			if (plan.PartyId.HasValue && personel.PartyId != plan.PartyId)
			{
				personel.PartyId = plan.PartyId;
				changed = true;
			}

			if (string.IsNullOrWhiteSpace(personel.Email) && takenEmails.Add(plan.Email))
			{
				personel.Email = plan.Email;
				changed = true;
			}

			if (string.IsNullOrWhiteSpace(personel.Code) && plan.Code != null && takenCodes.Add(plan.Code))
			{
				personel.Code = plan.Code;
				changed = true;
			}

			if (personel.IsActive != IsActiveEnum.Active)
			{
				personel.IsActive = IsActiveEnum.Active;
				changed = true;
			}

			return changed;
		}

		private async Task FillFullName(List<long> ids, CancellationToken cn)
		{
			if (ids.Count == 0)
				return;

			var idList = string.Join(",", ids);
			await app.Database.ExecuteSqlRawAsync(
				$"""
				UPDATE Hcm.Personel
				SET FullName = LTRIM(RTRIM(CONCAT(ISNULL(Name, N''), N' ', ISNULL(Family, N''))))
				WHERE Id IN ({idList})
				""",
				cn);
		}

		private async Task<(int Inserted, int ExpertsLinked)> BackfillExpertMissions(
			List<AgencyMap> mapped,
			IJobLogger? jobLogger,
			CancellationToken cn)
		{
			var links = mapped
				.Where(m => m.HtsPersonelId.HasValue && (m.PersonelId.HasValue || m.Created != null))
				.Select(m => new AgencyMap(
					m.HtsPersonelId,
					m.PersonelId ?? m.Created?.Id,
					m.UserId,
					m.Display,
					m.Created))
				.Where(m => m.PersonelId.HasValue)
				.ToList();
			if (links.Count == 0)
				return (0, 0);

			var htsIds = links.Select(m => m.HtsPersonelId!.Value).Distinct().ToList();
			var idList = string.Join(",", htsIds);
			var htsMissions = await htsDb.Database.SqlQueryRaw<HtsExpertLink>(
				$"""
				SELECT CAST(ServiceRequest_ExpertMission_ID AS bigint) AS Id,
				       CAST(ServiceRequest_FK AS bigint) AS ServiceRequestId,
				       CAST(Personel_FK AS bigint) AS PersonelId
				FROM Sale_ServiceRequest_ExpertMission
				WHERE Personel_FK IN ({idList}) AND ServiceRequest_FK IS NOT NULL
				""").ToListAsync(cn);

			if (htsMissions.Count == 0)
				return (0, 0);

			var byHtsPersonel = links
				.GroupBy(m => (long)m.HtsPersonelId!.Value)
				.ToDictionary(g => g.Key, g => g.First());
			var serviceHtsIds = htsMissions.Select(m => m.ServiceRequestId).Distinct().ToList();
			var requests = new List<RequestKey>();
			foreach (var slice in Chunk(serviceHtsIds, 1000))
			{
				var part = await unitOfWork.Repository<ServiceRequest>().TableNoTracking
					.Where(s => slice.Contains(s.HtsId) && s.Id != null)
					.Select(s => new RequestKey(s.Id!.Value, s.HtsId))
					.ToListAsync(cn);
				requests.AddRange(part);
			}
			var requestByHts = requests.GroupBy(s => s.HtsId).ToDictionary(g => g.Key, g => g.First().Id);

			var requestIds = requestByHts.Values.Distinct().ToList();
			var existing = new List<ServiceRequestExpertMission>();
			foreach (var slice in Chunk(requestIds, 1000))
			{
				var part = await unitOfWork.Repository<ServiceRequestExpertMission>().Table
					.Where(e => e.ServiceRequestId != null && slice.Contains(e.ServiceRequestId.Value))
					.ToListAsync(cn);
				existing.AddRange(part);
			}
			var byHtsMission = existing.Where(e => e.HtsId > 0).GroupBy(e => e.HtsId).ToDictionary(g => g.Key, g => g.First());
			var byRequestPersonel = existing
				.Where(e => e.PersonelId.HasValue)
				.GroupBy(e => (e.ServiceRequestId!.Value, e.PersonelId!.Value))
				.ToDictionary(g => g.Key, g => g.First());

			var inserted = 0;
			var expertsLinked = 0;
			var pending = new List<ServiceRequestExpertMission>();
			foreach (var htsMission in htsMissions)
			{
				if (!byHtsPersonel.TryGetValue(htsMission.PersonelId, out var map))
					continue;
				if (!requestByHts.TryGetValue(htsMission.ServiceRequestId, out var requestId))
					continue;

				if (byHtsMission.TryGetValue(htsMission.Id, out var byMission))
				{
					if (byMission.ExpertId == null && map.UserId.HasValue)
					{
						byMission.ExpertId = map.UserId;
						expertsLinked++;
					}
					continue;
				}

				if (byRequestPersonel.TryGetValue((requestId, map.PersonelId!.Value), out var byPerson))
				{
					if (byPerson.ExpertId == null && map.UserId.HasValue)
					{
						byPerson.ExpertId = map.UserId;
						expertsLinked++;
					}
					if (byPerson.HtsId == 0)
						byPerson.HtsId = htsMission.Id;
					continue;
				}

				var row = new ServiceRequestExpertMission
				{
					HtsId = htsMission.Id,
					ServiceRequestId = requestId,
					PersonelId = map.PersonelId,
					ExpertId = map.UserId,
					IsActive = IsActiveEnum.Active,
					CreatedOnMiladiDateTime = DateTime.Now
				};
				pending.Add(row);
				byHtsMission[htsMission.Id] = row;
				byRequestPersonel[(requestId, map.PersonelId!.Value)] = row;
				inserted++;
			}

			if (pending.Count > 0)
				app.Set<ServiceRequestExpertMission>().AddRange(pending);

			await app.SaveChangesAsync(cn);
			await FillEmptyExpertNames(requestIds, cn);
			await app.SaveChangesAsync(cn);
			await jobLogger?.LogInfoAsync($"اعزام HTS بررسی‌شده برای نمایندگی: {htsMissions.Count}", cn);
			return (inserted, expertsLinked);
		}

		private async Task FillEmptyExpertNames(List<long> requestIds, CancellationToken cn)
		{
			if (requestIds.Count == 0)
				return;

			var requests = new List<ServiceRequest>();
			foreach (var slice in Chunk(requestIds, 1000))
			{
				var part = await unitOfWork.Repository<ServiceRequest>().Table
					.Where(s => s.Id != null && slice.Contains(s.Id.Value)
						&& (s.ExpertMission == null || s.ExpertMission == ""))
					.ToListAsync(cn);
				requests.AddRange(part);
			}
			if (requests.Count == 0)
				return;

			var emptyIds = requests.Select(s => s.Id!.Value).ToList();
			var names = new List<ExpertNameRow>();
			foreach (var slice in Chunk(emptyIds, 1000))
			{
				var part = await unitOfWork.Repository<ServiceRequestExpertMission>().TableNoTracking
					.Where(e => e.ServiceRequestId != null && slice.Contains(e.ServiceRequestId.Value) && e.PersonelId != null)
				.Join(unitOfWork.Repository<Personel>().TableNoTracking,
					e => e.PersonelId,
					p => p.Id,
					(e, p) => new ExpertNameRow(e.ServiceRequestId, ((p.Name ?? "") + " " + (p.Family ?? "")).Trim()))
					.ToListAsync(cn);
				names.AddRange(part);
			}
			var byRequest = names
				.Where(n => n.Name.Length > 0 && n.ServiceRequestId.HasValue)
				.GroupBy(n => n.ServiceRequestId!.Value)
				.ToDictionary(g => g.Key, g => string.Join(" ، ", g.Select(x => x.Name).Distinct()));

			foreach (var request in requests)
			{
				if (request.Id.HasValue && byRequest.TryGetValue(request.Id.Value, out var text) && text.Length > 0)
					request.ExpertMission = text.Length > 2048 ? text[..2048] : text;
			}
		}

		private static (string Name, string? Family) FitParts(string? name, string? family)
		{
			name = Cut(name, 40);
			family = Cut(family, 100);
			if (name.Length == 0)
				return FitSingle(family ?? "");
			return (name, string.IsNullOrEmpty(family) ? null : family);
		}

		private static (string Name, string? Family) FitSingle(string text)
		{
			text = Norm(text);
			if (text.Length <= 40)
				return (text, null);

			var chunk = text[..40];
			var space = chunk.LastIndexOf(' ');
			var cut = space >= 10 ? space : 40;
			var name = text[..cut].Trim();
			var family = text[cut..].Trim();
			if (family.Length > 100)
				family = family[..100];
			return (name, family.Length == 0 ? null : family);
		}

		private static string Cut(string? value, int max)
		{
			var text = value?.Trim() ?? "";
			return text.Length <= max ? text : text[..max];
		}

		private static string? Meaningful(string? value)
		{
			var text = value?.Trim();
			if (string.IsNullOrEmpty(text) || text == "-" || text == "—")
				return null;
			return text;
		}

		private static string Norm(string? value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return "";
			return string.Join(' ', value.Split(' ', StringSplitOptions.RemoveEmptyEntries));
		}

		private static string PersonNorm(Personel personel) => Norm($"{personel.Name} {personel.Family}");

		private static string JoinName(string name, string? family) => Norm($"{name} {family}");

		private static IEnumerable<List<T>> Chunk<T>(List<T> items, int size)
		{
			for (var index = 0; index < items.Count; index += size)
				yield return items.GetRange(index, Math.Min(size, items.Count - index));
		}

		private sealed record RequestKey(long Id, long HtsId);

		private sealed record ExpertNameRow(long? ServiceRequestId, string Name);

		private sealed record AgencySource(
			short UserId,
			string Username,
			short? HtsPersonelId,
			string? PrsName,
			string? PrsFamily,
			string? PrsCode,
			int? CompanyId,
			long? CompanyHamkaranId,
			string? CompanyTitle);

		private sealed class AgencyPlan(
			AgencySource source,
			User? portalUser,
			string name,
			string? family,
			string display,
			long? partyId,
			string email,
			string? code)
		{
			public short? HtsPersonelId { get; } = source.HtsPersonelId;
			public User? PortalUser { get; } = portalUser;
			public string Name { get; } = name;
			public string? Family { get; } = family;
			public string Display { get; } = display;
			public long? PartyId { get; set; } = partyId;
			public string Email { get; } = email;
			public string? Code { get; set; } = code;
		}

		private sealed record AgencyMap(short? HtsPersonelId, long? PersonelId, long? UserId, string Display, Personel? Created = null);

		private sealed record PartyRow(long Id, string? FullName, long? HamkaranId);

		private sealed class HtsExpertLink
		{
			public long Id { get; set; }
			public long ServiceRequestId { get; set; }
			public long PersonelId { get; set; }
		}
	}
}
