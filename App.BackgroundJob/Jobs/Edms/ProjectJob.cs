using Common.Attributes;
using Data;
using Data.Contracts;
using Entities.App.Edms;
using Entities.App.Edms.Enums;
using Entities.App.Hcm;
using Entities.App.Hrm;
using Entities.Auth;
using Entities.Hts.Edms;
using Microsoft.EntityFrameworkCore;
using Services.FileServices;
using Services.Job;
using System.Globalization;

namespace App.BackgroundJob.Jobs.Edms
{
	public class ProjectJob(
		HtsDbContext htsDb,
		IUnitOfWork unitOfWork,
		ApplicationDbContext appContext,
		IFileService fileService)
	{
		private static readonly string[] DateFormats =
		[
			"yyyy/MM/dd",
			"yyyy/M/d",
			"yyyy-MM-dd",
			"yyyy-M-d",
			"dd/MM/yyyy"
		];

		[JobHandler("هماهنگ کردن پروژه‌ها از HTS")]
		public async Task SyncProjectsFromHts(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("شروع همگام‌سازی پروژه‌ها از HTS", cn);

				var htsProjects = await (
					from p in htsDb.Hts_Edms_Projects.AsNoTracking()
					join company in htsDb.Hts_Gnr_ManCompanies.AsNoTracking()
						on p.Main_Client_FK equals company.ManCompany_ID into companies
					from company in companies.DefaultIfEmpty()
					join unit in htsDb.Hts_HRM_OrgUnits.AsNoTracking()
						on p.BeneficiaryUnit_FK equals unit.OrgUnit_ID into units
					from unit in units.DefaultIfEmpty()
					select new HtsProjectRow
					{
						Project = p,
						CompanyName = company != null ? (company.FullName ?? company.CompanyName) : null,
						OrgUnitHamkaranId = unit != null ? unit.Hamkaran_Unit_FK : null,
						OrgUnitTitle = unit != null ? unit.OrgUnit_Title : null
					}).ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد {htsProjects.Count} پروژه از HTS دریافت شد", cn);

				var confidentialByProject = (await htsDb.Hts_Edms_ConfidentialProject_Users.AsNoTracking()
						.ToListAsync(cn))
					.GroupBy(c => c.ProjectId)
					.ToDictionary(g => g.Key, g => g.Select(x => x.UserId).ToList());

				var userMaps = await BuildUserMapsAsync(jobLogger, cn);
				var orgUnitMap = await BuildOrgUnitMapAsync(cn);

				var appProjects = await unitOfWork.Repository<Project>().Table.ToListAsync(cn);
				var byHtsId = appProjects
					.Where(p => p.HtsId != 0)
					.GroupBy(p => p.HtsId)
					.ToDictionary(g => g.Key, g => g.First());
				var byCode = appProjects
					.Where(p => p.HtsId == 0 && !string.IsNullOrWhiteSpace(p.Code))
					.GroupBy(p => p.Code!.Trim(), StringComparer.OrdinalIgnoreCase)
					.ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

				var newProjects = new List<Project>();
				var skippedUnitCount = 0;
				var updatedCount = 0;
				var insertedCount = 0;
				var unmatchedUsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

				foreach (var row in htsProjects)
				{
					var hts = row.Project;
					if (!TryResolveSubjectUnitId(row, orgUnitMap, out var subjectUnitId))
					{
						skippedUnitCount++;
						await jobLogger?.LogWarningAsync(
							$"واحد ذینفع برای پروژه HTS {hts.Edms_Project_ID} ({hts.Project_Code}) یافت نشد. واحد: {row.OrgUnitTitle ?? "-"}",
							0,
							cn);
						continue;
					}

					var mapped = MapProject(hts, row.CompanyName, subjectUnitId, userMaps, confidentialByProject, unmatchedUsers);

					if (byHtsId.TryGetValue(hts.Edms_Project_ID, out var existing)
						|| (!string.IsNullOrWhiteSpace(mapped.Code) && byCode.TryGetValue(mapped.Code, out existing)))
					{
						ApplyProjectFields(existing, mapped);
						if (existing.HtsId != hts.Edms_Project_ID)
							existing.HtsId = hts.Edms_Project_ID;
						byHtsId[hts.Edms_Project_ID] = existing;
						updatedCount++;
					}
					else
					{
						newProjects.Add(mapped);
						insertedCount++;
					}
				}

				if (newProjects.Count > 0)
					await unitOfWork.Repository<Project>().AddRangeAsync(newProjects, cn, false);

				await unitOfWork.SaveChangesAsync(cn);

				foreach (var project in newProjects)
				{
					if (project.Id.HasValue)
						byHtsId[project.HtsId] = project;
				}

				await jobLogger?.LogInfoAsync(
					$"پروژه‌ها ذخیره شدند. جدید: {insertedCount}، به‌روزرسانی: {updatedCount}، رد شده به‌خاطر واحد: {skippedUnitCount}",
					cn);

				if (unmatchedUsers.Count > 0)
				{
					await jobLogger?.LogWarningAsync(
						$"تعداد {unmatchedUsers.Count} شناسه کاربر/پرسنل HTS در App یافت نشد. نمونه: {string.Join(", ", unmatchedUsers.Take(20))}",
						0,
						cn);
				}

				await SyncProjectProgressAsync(byHtsId, jobLogger, cn);
				await jobLogger?.LogInfoAsync("همگام‌سازی پروژه‌ها با موفقیت انجام شد", cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
				throw;
			}
		}

		[JobHandler("انتقال پیوست‌های پروژه از HTS")]
		public async Task SyncProjectAttachmentsFromHts(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("شروع انتقال پیوست‌های پروژه از HTS", cn);

				var projectByHtsId = await unitOfWork.Repository<Project>().TableNoTracking
					.Where(p => p.HtsId != 0 && p.Id.HasValue)
					.Select(p => new { p.HtsId, Id = p.Id!.Value })
					.ToDictionaryAsync(p => p.HtsId, p => p.Id, cn);

				if (projectByHtsId.Count == 0)
				{
					await jobLogger?.LogWarningAsync("هیچ پروژه همگام‌شده‌ای با HtsId یافت نشد. ابتدا جاب پروژه‌ها را اجرا کنید", 0, cn);
					return;
				}

				var htsAttachments = await htsDb.Hts_Edms_Project_Attachments.AsNoTracking()
					.Select(a => new
					{
						a.Project_Attachment_ID,
						a.Project_FK,
						a.Attachment_FileName,
						a.AttachmentFilePath,
						a.Attachment_Comment,
						a.Project_Document_Type_FK,
						HasDbContent = a.Attachment_FileContent != null
					})
					.ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد {htsAttachments.Count} پیوست کاندید در HTS", cn);

				var existing = await unitOfWork.Repository<ProjectAttachment>().Table
					.Where(a => a.HtsId != 0)
					.ToListAsync(cn);
				var existingByHtsId = existing
					.GroupBy(a => a.HtsId)
					.ToDictionary(g => g.Key, g => g.First());

				var migrated = 0;
				var updated = 0;
				var skipped = 0;
				var failed = 0;

				foreach (var pa in htsAttachments)
				{
					try
					{
						if (!projectByHtsId.TryGetValue(pa.Project_FK, out var projectId))
						{
							skipped++;
							continue;
						}

						var type = MapAttachmentType(pa.Project_Document_Type_FK);
						var comment = pa.Attachment_Comment ?? string.Empty;

						if (existingByHtsId.TryGetValue(pa.Project_Attachment_ID, out var existingAttachment))
						{
							existingAttachment.ProjectId = projectId;
							existingAttachment.Comment = comment;
							existingAttachment.Type = type;
							updated++;
							continue;
						}

						var fileName = string.IsNullOrWhiteSpace(pa.Attachment_FileName)
							? $"attachment_{pa.Project_Attachment_ID}"
							: pa.Attachment_FileName;

						var fileBytes = await ReadHtsAttachmentBytesAsync(
							pa.Project_Attachment_ID,
							pa.AttachmentFilePath,
							pa.HasDbContent,
							cn);

						if (fileBytes == null || fileBytes.Length == 0)
						{
							skipped++;
							await jobLogger?.LogWarningAsync(
								$"فایل پیوست HTS یافت نشد. AttachmentId={pa.Project_Attachment_ID}, Path={pa.AttachmentFilePath}",
								0,
								cn);
							continue;
						}

						var uploaded = await fileService.UploadAsync(
							fileBytes,
							fileName,
							null,
							typeof(ProjectAttachment).FullName,
							nameof(ProjectAttachment.Attachment),
							null,
							cn);

						var entity = new ProjectAttachment
						{
							ProjectId = projectId,
							AttachmentId = uploaded.Id!.Value,
							Comment = comment,
							Type = type,
							HtsId = pa.Project_Attachment_ID
						};

						await unitOfWork.Repository<ProjectAttachment>().AddAsync(entity, cn, false);
						existingByHtsId[pa.Project_Attachment_ID] = entity;
						migrated++;
					}
					catch (Exception ex)
					{
						failed++;
						await jobLogger?.LogErrorAsync(
							$"خطا در انتقال پیوست HTS Id={pa.Project_Attachment_ID}: {ex.Message}",
							0,
							cn);
					}
				}

				await unitOfWork.SaveChangesAsync(cn);
				await jobLogger?.LogInfoAsync(
					$"پایان انتقال پیوست‌ها. Migrated={migrated}, Updated={updated}, Skipped={skipped}, Failed={failed}",
					cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
				throw;
			}
		}

		private async Task SyncProjectProgressAsync(
			Dictionary<long, Project> projectByHtsId,
			IJobLogger? jobLogger,
			CancellationToken cn)
		{
			var htsProgress = await htsDb.Hts_Edms_Project_Progresses.AsNoTracking().ToListAsync(cn);
			await jobLogger?.LogInfoAsync($"تعداد {htsProgress.Count} رکورد درصد پیشرفت از HTS", cn);

			var appProgress = await unitOfWork.Repository<ProjectProgressPercentage>().Table.ToListAsync(cn);
			var byHtsId = appProgress
				.Where(p => p.HtsId != 0)
				.GroupBy(p => p.HtsId)
				.ToDictionary(g => g.Key, g => g.First());

			var syncedProjectIds = projectByHtsId.Values
				.Where(p => p.Id.HasValue)
				.Select(p => p.Id!.Value)
				.ToHashSet();

			var desiredHtsIds = new HashSet<long>();
			var added = 0;
			var updated = 0;

			foreach (var row in htsProgress)
			{
				if (!projectByHtsId.TryGetValue(row.Project_FK, out var project) || !project.Id.HasValue)
					continue;

				desiredHtsIds.Add(row.Project_Progress_ID);
				var status = MapDocumentStatus(row.Document_Status_FK);

				if (byHtsId.TryGetValue(row.Project_Progress_ID, out var existing))
				{
					existing.ProjectId = project.Id.Value;
					existing.Percentage = row.Progress_Percentage;
					existing.Status = status;
					updated++;
				}
				else
				{
					var entity = new ProjectProgressPercentage
					{
						ProjectId = project.Id.Value,
						Percentage = row.Progress_Percentage,
						Status = status,
						HtsId = row.Project_Progress_ID
					};
					await unitOfWork.Repository<ProjectProgressPercentage>().AddAsync(entity, cn, false);
					byHtsId[row.Project_Progress_ID] = entity;
					added++;
				}
			}

			var toDelete = appProgress
				.Where(p => p.HtsId != 0
					&& syncedProjectIds.Contains(p.ProjectId)
					&& !desiredHtsIds.Contains(p.HtsId))
				.ToList();

			if (toDelete.Count > 0)
				await unitOfWork.Repository<ProjectProgressPercentage>().DeleteRangeAsync(toDelete, cn, false);

			await unitOfWork.SaveChangesAsync(cn);
			await jobLogger?.LogInfoAsync(
				$"درصد پیشرفت همگام شد. جدید: {added}، به‌روزرسانی: {updated}، حذف: {toDelete.Count}",
				cn);
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

		private async Task<OrgUnitMaps> BuildOrgUnitMapAsync(CancellationToken cn)
		{
			var units = await unitOfWork.Repository<OrgUnit>().TableNoTracking
				.Where(u => u.Id.HasValue)
				.Select(u => new { u.Id, u.HamkaranUnitId, u.Title })
				.ToListAsync(cn);

			var byHamkaran = units
				.GroupBy(u => u.HamkaranUnitId)
				.ToDictionary(g => g.Key, g => g.First().Id!.Value);

			var byTitle = units
				.Where(u => !string.IsNullOrWhiteSpace(u.Title))
				.GroupBy(u => u.Title.Trim(), StringComparer.Ordinal)
				.ToDictionary(g => g.Key, g => g.First().Id!.Value, StringComparer.Ordinal);

			return new OrgUnitMaps(byHamkaran, byTitle);
		}

		private static bool TryResolveSubjectUnitId(HtsProjectRow row, OrgUnitMaps maps, out long subjectUnitId)
		{
			if (row.OrgUnitHamkaranId.HasValue && maps.ByHamkaran.TryGetValue(row.OrgUnitHamkaranId.Value, out subjectUnitId))
				return true;

			if (!string.IsNullOrWhiteSpace(row.OrgUnitTitle)
				&& maps.ByTitle.TryGetValue(row.OrgUnitTitle.Trim(), out subjectUnitId))
				return true;

			subjectUnitId = 0;
			return false;
		}

		private static Project MapProject(
			Hts_Edms_Project hts,
			string? companyName,
			long subjectUnitId,
			UserMaps users,
			IReadOnlyDictionary<int, List<short>> confidentialByProject,
			HashSet<string> unmatchedUsers)
		{
			ParseProjectTitle(hts.Project_Title, companyName, out var customer, out var siteName, out var packageType);

			var title = FirstNonEmpty(hts.Project_Title, hts.Project_Name, hts.Project_Code, "بدون عنوان");
			var confidentialIds = MergeConfidentialUserIds(hts, confidentialByProject);
			var (sensitiveIds, sensitiveNames) = MapUserIdList(confidentialIds, users, unmatchedUsers, preferHtsUser: true);
			var beneficiaryIds = ParseIdList(hts.BeneficiariesIds);
			var (takvinIds, takvinNames) = MapUserIdList(beneficiaryIds, users, unmatchedUsers, preferHtsUser: true);

			return new Project
			{
				HtsId = hts.Edms_Project_ID,
				Title = Truncate(title, 500)!,
				Name = Truncate(hts.Project_Name, 500),
				Code = Truncate(hts.Project_Code, 200),
				ContractPartyCustomer = Truncate(customer, 500),
				ProjectName = Truncate(siteName, 500),
				PackageType = Truncate(packageType, 200),
				StartMiladiDate = ParseDate(hts.Project_Startdate),
				StartShamsiDate = Truncate(hts.Project_Startdate_Shamsi, 30),
				ExpirationMiladiDate = ParseDate(hts.Project_Enddate),
				ExpirationShamsiDate = Truncate(hts.Project_Enddate_Shamsi, 30),
				Status = MapProjectStatus(hts.Project_Status_FK),
				SubjectUnitId = subjectUnitId,
				ProjectManagerId = ResolvePersonel(hts.Project_Manager_FK, users, unmatchedUsers),
				ProjectManagerPSLId = ResolvePersonel(hts.Project_Engineer_FK, users, unmatchedUsers),
				CoordinatorId = ResolvePersonel(hts.Project_Coordinator_FK, users, unmatchedUsers),
				SalesExpertId = ResolvePersonel(hts.SalesExpertUserId, users, unmatchedUsers),
				DccId = ResolveHtsUser(hts.DccUserId, users, unmatchedUsers),
				ProcessEngineerId = ResolvePersonel(hts.ProcessExpertId, users, unmatchedUsers),
				MechanicExpertId = ResolvePersonel(hts.MechanicalExpertId, users, unmatchedUsers),
				ControlEngineerId = ResolvePersonel(hts.ControlExpertId, users, unmatchedUsers),
				ToolExpertId = ResolvePersonel(hts.InstrumentationExpertId, users, unmatchedUsers),
				ElectricEngineerId = ResolvePersonel(hts.ElectricalExpertId, users, unmatchedUsers),
				PipingExpertId = ResolvePersonel(hts.PipingExpertId, users, unmatchedUsers),
				ProjectReviewerId = ResolvePersonel(hts.ProjectInspectorId, users, unmatchedUsers),
				PowerAndPrecisionPrepManagerId = ResolvePersonel(hts.ElectricalSuppliesResponsibleId, users, unmatchedUsers),
				MechanicalPreparationsResponsibleId = ResolvePersonel(hts.MechanicalSuppliesResponsibleId, users, unmatchedUsers),
				ForeignPreparationsAgentId = ResolvePersonel(hts.ExternalSuppliesResponsibleId, users, unmatchedUsers),
				ProjectIsVendoriType = hts.IsVendorProject,
				TransmissionPrefix = Truncate(hts.Transmital_PreField, 200),
				TransmitterRecipient = Truncate(hts.Transmital_Recipient, 500),
				ReceiverCopyTransmital = Truncate(hts.Transmital_CC, 500),
				EmailAddress = Truncate(hts.MiscEmailAddress, 500),
				Explanation = Truncate(hts.Project_Description, 2000),
				IsConfidential = hts.IsConfidential,
				SensitiveUsersIds = sensitiveIds,
				SensitiveUsers = FirstNonEmpty(sensitiveNames, hts.CustomPermissionUsersInText),
				IsTakvinProject = hts.IsTakvinProject,
				UserBenefitTakvinIds = takvinIds,
				UserBenefitTakvin = takvinNames,
				LegalDelayHavayar = hts.AllowedHavayar_DelayDay,
				LegalClientDayDelay = hts.AllowedEmployer_DelayDay
			};
		}

		private static void ApplyProjectFields(Project target, Project source)
		{
			target.Title = source.Title;
			target.Name = source.Name;
			target.Code = source.Code;
			target.ContractPartyCustomer = source.ContractPartyCustomer;
			target.ProjectName = source.ProjectName;
			target.PackageType = source.PackageType;
			target.StartMiladiDate = source.StartMiladiDate;
			target.StartShamsiDate = source.StartShamsiDate;
			target.ExpirationMiladiDate = source.ExpirationMiladiDate;
			target.ExpirationShamsiDate = source.ExpirationShamsiDate;
			target.Status = source.Status;
			target.SubjectUnitId = source.SubjectUnitId;
			target.ProjectManagerId = source.ProjectManagerId;
			target.ProjectManagerPSLId = source.ProjectManagerPSLId;
			target.CoordinatorId = source.CoordinatorId;
			target.SalesExpertId = source.SalesExpertId;
			target.DccId = source.DccId;
			target.ProcessEngineerId = source.ProcessEngineerId;
			target.MechanicExpertId = source.MechanicExpertId;
			target.ControlEngineerId = source.ControlEngineerId;
			target.ToolExpertId = source.ToolExpertId;
			target.ElectricEngineerId = source.ElectricEngineerId;
			target.PipingExpertId = source.PipingExpertId;
			target.ProjectReviewerId = source.ProjectReviewerId;
			target.PowerAndPrecisionPrepManagerId = source.PowerAndPrecisionPrepManagerId;
			target.MechanicalPreparationsResponsibleId = source.MechanicalPreparationsResponsibleId;
			target.ForeignPreparationsAgentId = source.ForeignPreparationsAgentId;
			target.ProjectIsVendoriType = source.ProjectIsVendoriType;
			target.TransmissionPrefix = source.TransmissionPrefix;
			target.TransmitterRecipient = source.TransmitterRecipient;
			target.ReceiverCopyTransmital = source.ReceiverCopyTransmital;
			target.EmailAddress = source.EmailAddress;
			target.Explanation = source.Explanation;
			target.IsConfidential = source.IsConfidential;
			target.SensitiveUsersIds = source.SensitiveUsersIds;
			target.SensitiveUsers = source.SensitiveUsers;
			target.IsTakvinProject = source.IsTakvinProject;
			target.UserBenefitTakvinIds = source.UserBenefitTakvinIds;
			target.UserBenefitTakvin = source.UserBenefitTakvin;
			target.LegalDelayHavayar = source.LegalDelayHavayar;
			target.LegalClientDayDelay = source.LegalClientDayDelay;
			target.HtsId = source.HtsId;
		}

		private static void ParseProjectTitle(
			string? title,
			string? companyName,
			out string? customer,
			out string? siteName,
			out string? packageType)
		{
			customer = null;
			siteName = null;
			packageType = null;

			if (string.IsNullOrWhiteSpace(title))
			{
				customer = companyName;
				return;
			}

			var parts = title.Split('_', StringSplitOptions.None).ToList();
			while (parts.Count > 0 && (string.IsNullOrWhiteSpace(parts[^1]) || parts[^1] == "0000"))
				parts.RemoveAt(parts.Count - 1);

			if (parts.Count >= 1)
				customer = parts[0].Trim();
			if (parts.Count >= 2)
				siteName = parts[1].Trim();
			if (parts.Count >= 3)
				packageType = string.Join('_', parts.Skip(2)).Trim();

			if (string.IsNullOrWhiteSpace(customer))
				customer = companyName;
		}

		private static ProjectStatusEnum MapProjectStatus(short htsStatus)
		{
			return htsStatus switch
			{
				3 => ProjectStatusEnum.StartNotStarted,
				4 => ProjectStatusEnum.InProcess,
				5 => ProjectStatusEnum.Completed,
				6 => ProjectStatusEnum.Canceled,
				7 => ProjectStatusEnum.Stopped,
				2432 => ProjectStatusEnum.PendingFinalBook,
				2748 => ProjectStatusEnum.Loss,
				2773 => ProjectStatusEnum.Win,
				_ => ProjectStatusEnum.InProcess
			};
		}

		private static DocumentStatusEnums? MapDocumentStatus(byte statusFk)
		{
			return Enum.IsDefined(typeof(DocumentStatusEnums), (int)statusFk)
				? (DocumentStatusEnums)statusFk
				: null;
		}

		private static ProjectAttachmentTypeEnum MapAttachmentType(short? documentTypeId)
		{
			if (documentTypeId.HasValue
				&& Enum.IsDefined(typeof(ProjectAttachmentTypeEnum), (int)documentTypeId.Value))
			{
				return (ProjectAttachmentTypeEnum)documentTypeId.Value;
			}

			return ProjectAttachmentTypeEnum.Document;
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

		private static List<short> MergeConfidentialUserIds(
			Hts_Edms_Project hts,
			IReadOnlyDictionary<int, List<short>> confidentialByProject)
		{
			var ids = new HashSet<short>(ParseIdList(hts.CustomPermissionUserIds));
			if (confidentialByProject.TryGetValue(hts.Edms_Project_ID, out var tableIds))
			{
				foreach (var id in tableIds)
					ids.Add(id);
			}

			return ids.ToList();
		}

		private static (string? Ids, string? Names) MapUserIdList(
			IEnumerable<short> htsIds,
			UserMaps users,
			HashSet<string> unmatched,
			bool preferHtsUser)
		{
			var appIds = new List<long>();
			var names = new List<string>();

			foreach (var htsId in htsIds.Distinct())
			{
				long? appId = preferHtsUser
					? ResolveHtsUser(htsId, users, unmatched) ?? ResolvePersonel(htsId, users, unmatched)
					: ResolvePersonel(htsId, users, unmatched) ?? ResolveHtsUser(htsId, users, unmatched);

				if (!appId.HasValue)
					continue;

				appIds.Add(appId.Value);
				if (users.UsersById.TryGetValue(appId.Value, out var info) && !string.IsNullOrWhiteSpace(info.DisplayName))
					names.Add(info.DisplayName);
			}

			if (appIds.Count == 0)
				return (null, null);

			return (string.Join(',', appIds.Distinct()), string.Join(',', names.Distinct()));
		}

		private static IEnumerable<short> ParseIdList(string? raw)
		{
			if (string.IsNullOrWhiteSpace(raw))
				yield break;

			foreach (var part in raw.Split([',', ';', '|', ' '], StringSplitOptions.RemoveEmptyEntries))
			{
				if (short.TryParse(part.Trim(), out var id))
					yield return id;
			}
		}

		private static DateTime? ParseDate(string? value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return null;

			if (DateTime.TryParseExact(value.Trim(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
				return date;

			return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date) ? date : null;
		}

		private static string? Truncate(string? value, int maxLength)
		{
			if (string.IsNullOrEmpty(value))
				return value;

			var trimmed = value.Trim();
			return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
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

		private async Task<byte[]?> ReadHtsAttachmentBytesAsync(
			int attachmentId,
			string? attachmentFilePath,
			bool hasDbContent,
			CancellationToken cn)
		{
			if (!string.IsNullOrWhiteSpace(attachmentFilePath) && File.Exists(attachmentFilePath))
				return await File.ReadAllBytesAsync(attachmentFilePath, cn);

			if (!hasDbContent)
				return null;

			return await htsDb.Hts_Edms_Project_Attachments.AsNoTracking()
				.Where(a => a.Project_Attachment_ID == attachmentId)
				.Select(a => a.Attachment_FileContent)
				.FirstOrDefaultAsync(cn);
		}

		private sealed class HtsProjectRow
		{
			public Hts_Edms_Project Project { get; init; } = null!;
			public string? CompanyName { get; init; }
			public int? OrgUnitHamkaranId { get; init; }
			public string? OrgUnitTitle { get; init; }
		}

		private sealed record UserMaps(
			Dictionary<short, long> PersonelToUser,
			Dictionary<short, long> HtsUserToAppUser,
			Dictionary<long, AppUserInfo> UsersById);

		private sealed record AppUserInfo(long Id, string DisplayName);

		private sealed record OrgUnitMaps(
			Dictionary<long, long> ByHamkaran,
			Dictionary<string, long> ByTitle);
	}
}
