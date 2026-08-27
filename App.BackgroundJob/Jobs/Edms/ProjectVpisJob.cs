using Common.Attributes;
using Data;
using Data.Contracts;
using Entities.App.Edms;
using Entities.App.Edms.Enums;
using Entities.App.Hcm;
using Entities.Hts.Edms;
using Microsoft.EntityFrameworkCore;
using Services.Job;

namespace App.BackgroundJob.Jobs.Edms
{
	public class ProjectVpisJob(
		HtsDbContext htsDb,
		IUnitOfWork unitOfWork,
		ApplicationDbContext appContext)
	{
		private const int SaveBatchSize = 500;
		private const int HtsDocClassIfa = 309;
		private const int HtsDocClassIfi = 310;

		[JobHandler("هماهنگ کردن VPIS پروژه‌ها از HTS")]
		public async Task SyncProjectVpisFromHts(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("شروع همگام‌سازی VPIS پروژه‌ها از HTS", cn);

				var projectByHtsId = (await unitOfWork.Repository<Project>().TableNoTracking
						.Where(p => p.HtsId != 0 && p.Id.HasValue)
						.Select(p => new { p.HtsId, Id = p.Id!.Value })
						.ToListAsync(cn))
					.GroupBy(p => p.HtsId)
					.ToDictionary(g => g.Key, g => g.First().Id);

				if (projectByHtsId.Count == 0)
				{
					await jobLogger?.LogWarningAsync(
						"هیچ پروژه همگام‌شده‌ای با HtsId یافت نشد. ابتدا جاب پروژه‌ها را اجرا کنید",
						0,
						cn);
					return;
				}

				var htsVpis = await htsDb.Hts_Edms_Project_Vpis.AsNoTracking().ToListAsync(cn);
				await jobLogger?.LogInfoAsync($"تعداد {htsVpis.Count} رکورد VPIS از HTS دریافت شد", cn);

				var producersByVpis = (await htsDb.Hts_Edms_Project_Vpis_Responsibles.AsNoTracking().ToListAsync(cn))
					.GroupBy(r => r.Project_Vpis_FK)
					.ToDictionary(g => g.Key, g => g.ToList());

				var userMaps = await BuildUserMapsAsync(jobLogger, cn);

				var appVpis = await unitOfWork.Repository<ProjectVpis>().Table.ToListAsync(cn);
				var byHtsId = appVpis
					.Where(v => v.HtsId != 0)
					.GroupBy(v => v.HtsId)
					.ToDictionary(g => g.Key, g => g.First());
				var byProjectAndCode = appVpis
					.Where(v => v.HtsId == 0 && v.ProjectNameId.HasValue && !string.IsNullOrWhiteSpace(v.Code))
					.GroupBy(v => ProjectCodeKey(v.ProjectNameId!.Value, v.Code!))
					.ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

				var unmatchedUsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				var skippedProjectIds = new HashSet<long>();
				var insertedCount = 0;
				var updatedCount = 0;
				var skippedCount = 0;
				var pendingChanges = 0;

				foreach (var hts in htsVpis)
				{
					if (!projectByHtsId.TryGetValue(hts.Project_FK, out var projectId))
					{
						skippedProjectIds.Add(hts.Project_FK);
						skippedCount++;
						continue;
					}

					producersByVpis.TryGetValue(hts.Project_Vpis_ID, out var producerRows);
					var mapped = MapVpis(hts, projectId, producerRows, userMaps, unmatchedUsers);

					if (byHtsId.TryGetValue(hts.Project_Vpis_ID, out var existing)
						|| TryMatchByProjectAndCode(mapped, byProjectAndCode, out existing))
					{
						ApplyVpisFields(existing, mapped);
						if (existing.HtsId != hts.Project_Vpis_ID)
							existing.HtsId = hts.Project_Vpis_ID;
						byHtsId[hts.Project_Vpis_ID] = existing;
						updatedCount++;
					}
					else
					{
						await unitOfWork.Repository<ProjectVpis>().AddAsync(mapped, cn, false);
						byHtsId[hts.Project_Vpis_ID] = mapped;
						if (!string.IsNullOrWhiteSpace(mapped.Code) && mapped.ProjectNameId.HasValue)
							byProjectAndCode[ProjectCodeKey(mapped.ProjectNameId.Value, mapped.Code)] = mapped;
						insertedCount++;
					}

					pendingChanges++;
					if (pendingChanges >= SaveBatchSize)
					{
						await unitOfWork.SaveChangesAsync(cn);
						pendingChanges = 0;
					}
				}

				if (pendingChanges > 0)
					await unitOfWork.SaveChangesAsync(cn);

				await jobLogger?.LogInfoAsync(
					$"VPIS ذخیره شد. جدید: {insertedCount}، به‌روزرسانی: {updatedCount}، رد شده به‌خاطر پروژه: {skippedCount}",
					cn);

				if (skippedProjectIds.Count > 0)
				{
					await jobLogger?.LogWarningAsync(
						$"تعداد {skippedProjectIds.Count} پروژه HTS برای VPIS در App یافت نشد. نمونه: {string.Join(", ", skippedProjectIds.Take(20))}",
						0,
						cn);
				}

				if (unmatchedUsers.Count > 0)
				{
					await jobLogger?.LogWarningAsync(
						$"تعداد {unmatchedUsers.Count} شناسه کاربر/پرسنل HTS در App یافت نشد. نمونه: {string.Join(", ", unmatchedUsers.Take(20))}",
						0,
						cn);
				}

				await jobLogger?.LogInfoAsync("همگام‌سازی VPIS پروژه‌ها با موفقیت انجام شد", cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
				throw;
			}
		}

		private static bool TryMatchByProjectAndCode(
			ProjectVpis mapped,
			Dictionary<string, ProjectVpis> byProjectAndCode,
			out ProjectVpis existing)
		{
			existing = null!;
			if (!mapped.ProjectNameId.HasValue || string.IsNullOrWhiteSpace(mapped.Code))
				return false;

			return byProjectAndCode.TryGetValue(ProjectCodeKey(mapped.ProjectNameId.Value, mapped.Code), out existing!);
		}

		private static ProjectVpis MapVpis(
			Hts_Edms_Project_Vpis hts,
			long projectId,
			List<Hts_Edms_Project_Vpis_Responsible>? producerRows,
			UserMaps users,
			HashSet<string> unmatchedUsers)
		{
			var (reviewerIds, reviewerNames) = MapReviewerFromCheckedUser(hts.CheckedUser_FK, users, unmatchedUsers);

			return new ProjectVpis
			{
				HtsId = hts.Project_Vpis_ID,
				ProjectNameId = projectId,
				Code = Truncate(hts.Document_Number, 200),
				Title = Truncate(hts.Document_Title, 500),
				Weight = hts.Document_Weight,
				Type = MapDocType(hts.DocType_FK),
				ClassDocument = MapDocClass(hts.DocClass_FK),
				Displaying = MapDiscipline(hts.DocDisipline_FK),
				PageSize = MapPageSize(hts.PageSize_FK),
				FirstDegreeDocumentMiladiDate = hts.First_Issue_Baseline,
				FirstDegreeDocumentShamsiDate = Truncate(hts.First_Issue_BaselineInText, 30),
				ReplaceMiladiDate = hts.First_Issue_Plan,
				ReplaceShamsiDate = Truncate(hts.First_Issue_PlanInText, 30),
				PersonHourRevisionZero = hts.Rev0_ManHour,
				PersonHourRevisionOne = hts.Rev1_ManHour,
				PersonHourRevisionTwo = hts.RevN_ManHour,
				PersonHourRevisionThree = null,
				ProducerId = MapMainProducer(producerRows, users, unmatchedUsers),
				ApproverId = ResolveHtsUser(hts.ApprovedUser_FK, users, unmatchedUsers),
				ReviewersId = reviewerIds,
				ReviewerNames = reviewerNames,
				Description = BuildDescription(hts.Description, hts.RevN_ManHourPercent)
			};
		}

		private static void ApplyVpisFields(ProjectVpis target, ProjectVpis source)
		{
			target.HtsId = source.HtsId;
			target.ProjectNameId = source.ProjectNameId;
			target.Code = source.Code;
			target.Title = source.Title;
			target.Weight = source.Weight;
			target.Type = source.Type;
			target.ClassDocument = source.ClassDocument;
			target.Displaying = source.Displaying;
			target.PageSize = source.PageSize;
			target.FirstDegreeDocumentMiladiDate = source.FirstDegreeDocumentMiladiDate;
			target.FirstDegreeDocumentShamsiDate = source.FirstDegreeDocumentShamsiDate;
			target.ReplaceMiladiDate = source.ReplaceMiladiDate;
			target.ReplaceShamsiDate = source.ReplaceShamsiDate;
			target.PersonHourRevisionZero = source.PersonHourRevisionZero;
			target.PersonHourRevisionOne = source.PersonHourRevisionOne;
			target.PersonHourRevisionTwo = source.PersonHourRevisionTwo;
			target.PersonHourRevisionThree = source.PersonHourRevisionThree;
			target.ProducerId = source.ProducerId;
			target.ApproverId = source.ApproverId;
			target.ReviewersId = source.ReviewersId;
			target.ReviewerNames = source.ReviewerNames;
			target.Description = source.Description;
		}

		private static ProjectVpisDocTypeEnum? MapDocType(byte? docTypeFk)
		{
			if (!docTypeFk.HasValue)
				return null;

			var value = docTypeFk.Value - 1;
			return Enum.IsDefined(typeof(ProjectVpisDocTypeEnum), value)
				? (ProjectVpisDocTypeEnum)value
				: null;
		}

		private static ProjectVpisClassDocumentEnum? MapDocClass(short? docClassFk)
		{
			return docClassFk switch
			{
				HtsDocClassIfa => ProjectVpisClassDocumentEnum.IFA,
				HtsDocClassIfi => ProjectVpisClassDocumentEnum.IFI,
				_ => null
			};
		}

		private static ProjectVpisDisplayingEnum? MapDiscipline(short? disciplineFk)
		{
			if (!disciplineFk.HasValue)
				return null;

			var value = disciplineFk.Value - 1;
			return Enum.IsDefined(typeof(ProjectVpisDisplayingEnum), value)
				? (ProjectVpisDisplayingEnum)value
				: null;
		}

		private static ProjectVpisPageSizeEnum? MapPageSize(byte? pageSizeFk)
		{
			return pageSizeFk switch
			{
				1 => ProjectVpisPageSizeEnum.A1,
				2 => ProjectVpisPageSizeEnum.A2,
				3 => ProjectVpisPageSizeEnum.A3,
				4 => ProjectVpisPageSizeEnum.A4,
				5 => ProjectVpisPageSizeEnum.A5,
				6 => ProjectVpisPageSizeEnum.A6,
				_ => null
			};
		}

		private static string? BuildDescription(string? description, byte? revNPercent)
		{
			var baseDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
			if (!revNPercent.HasValue)
				return baseDescription;

			var suffix = $" [RevN: {revNPercent.Value}%]";
			return string.IsNullOrEmpty(baseDescription) ? suffix.Trim() : baseDescription + suffix;
		}

		private static long? MapMainProducer(
			List<Hts_Edms_Project_Vpis_Responsible>? producerRows,
			UserMaps users,
			HashSet<string> unmatched)
		{
			if (producerRows == null || producerRows.Count == 0)
				return null;

			var main = producerRows.FirstOrDefault(r => r.IsMain == true) ?? producerRows[0];
			return ResolvePersonel(main.Responsible_FK, users, unmatched);
		}

		private static (string? Ids, string? Names) MapReviewerFromCheckedUser(
			short? checkedUserFk,
			UserMaps users,
			HashSet<string> unmatched)
		{
			var appId = ResolveHtsUser(checkedUserFk, users, unmatched);
			if (!appId.HasValue)
				return (null, null);

			string? name = null;
			if (users.UsersById.TryGetValue(appId.Value, out var info) && !string.IsNullOrWhiteSpace(info.DisplayName))
				name = info.DisplayName;

			return (appId.Value.ToString(), name);
		}

		private async Task<UserMaps> BuildUserMapsAsync(IJobLogger? jobLogger, CancellationToken cn)
		{
			var appUsers = await appContext.Users.AsNoTracking()
				.Where(u => u.Id.HasValue)
				.Select(u => new { u.Id, u.Username, u.PartyId, u.Name, u.NameFa })
				.ToListAsync(cn);

			var usersByUsername = new Dictionary<string, AppUserInfo>(StringComparer.OrdinalIgnoreCase);
			var usersByPartyId = new Dictionary<long, AppUserInfo>();
			var usersById = new Dictionary<long, AppUserInfo>();

			foreach (var user in appUsers)
			{
				var info = new AppUserInfo(user.Id!.Value, user.NameFa ?? user.Name ?? user.Username);
				usersById[info.Id] = info;
				if (!string.IsNullOrWhiteSpace(user.Username))
					usersByUsername[user.Username.Trim()] = info;
				if (user.PartyId.HasValue)
					usersByPartyId[user.PartyId.Value] = info;
			}

			var personels = await unitOfWork.Repository<Personel>().TableNoTracking
				.Where(p => p.HamkaranId.HasValue && p.PartyId.HasValue)
				.Select(p => new { p.HamkaranId, p.PartyId })
				.ToListAsync(cn);

			var hamkaranToUser = new Dictionary<long, long>();
			foreach (var personel in personels)
			{
				if (usersByPartyId.TryGetValue(personel.PartyId!.Value, out var user))
					hamkaranToUser[personel.HamkaranId!.Value] = user.Id;
			}

			var htsPersonels = await htsDb.Hts_HRM_Personels.AsNoTracking().ToListAsync(cn);
			var htsUsers = await htsDb.Hts_Gnr_Users.AsNoTracking().ToListAsync(cn);

			var personelToUser = new Dictionary<short, long>();
			var htsUserToAppUser = new Dictionary<short, long>();

			foreach (var personel in htsPersonels)
			{
				if (personel.Hamkaran_Personel_FK.HasValue
					&& hamkaranToUser.TryGetValue(personel.Hamkaran_Personel_FK.Value, out var userId))
				{
					personelToUser[personel.Personel_ID] = userId;
				}
			}

			foreach (var htsUser in htsUsers)
			{
				if (!string.IsNullOrWhiteSpace(htsUser.Username)
					&& usersByUsername.TryGetValue(htsUser.Username.Trim(), out var byName))
				{
					htsUserToAppUser[htsUser.User_ID] = byName.Id;
					if (htsUser.Personel_FK.HasValue && !personelToUser.ContainsKey(htsUser.Personel_FK.Value))
						personelToUser[htsUser.Personel_FK.Value] = byName.Id;
				}
				else if (htsUser.Personel_FK.HasValue
					&& personelToUser.TryGetValue(htsUser.Personel_FK.Value, out var fromPersonel))
				{
					htsUserToAppUser[htsUser.User_ID] = fromPersonel;
				}
			}

			await jobLogger?.LogInfoAsync(
				$"نگاشت کاربر: {personelToUser.Count} پرسنل و {htsUserToAppUser.Count} کاربر HTS",
				cn);

			return new UserMaps(personelToUser, htsUserToAppUser, usersById);
		}

		private static long? ResolvePersonel(short? personelId, UserMaps users, HashSet<string> unmatched)
		{
			if (!personelId.HasValue)
				return null;

			if (users.PersonelToUser.TryGetValue(personelId.Value, out var userId))
				return userId;

			unmatched.Add($"P:{personelId.Value}");
			return null;
		}

		private static long? ResolveHtsUser(short? htsUserId, UserMaps users, HashSet<string> unmatched)
		{
			if (!htsUserId.HasValue)
				return null;

			if (users.HtsUserToAppUser.TryGetValue(htsUserId.Value, out var userId))
				return userId;

			unmatched.Add($"U:{htsUserId.Value}");
			return null;
		}

		private static string ProjectCodeKey(long projectId, string code) =>
			$"{projectId}|{code.Trim()}";

		private static string? Truncate(string? value, int maxLength)
		{
			if (string.IsNullOrEmpty(value))
				return value;

			var trimmed = value.Trim();
			return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
		}

		private sealed record UserMaps(
			Dictionary<short, long> PersonelToUser,
			Dictionary<short, long> HtsUserToAppUser,
			Dictionary<long, AppUserInfo> UsersById);

		private sealed record AppUserInfo(long Id, string? DisplayName);
	}
}
