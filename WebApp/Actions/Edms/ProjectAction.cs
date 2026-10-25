using Common.Utilities;
using Data.Contracts;
using Data.Contracts.Actions;
using Data.Repositories;
using Data.SystemAuth;
using Entities.App.Edms;
using Entities.App.Hrm;
using Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Actions.Edms
{

	public class ProjectAction(IUnitOfWork unitOfWork, ISdk sdk) : IEntityAction<Project>
	{
		[EntityAction(typeof(Project), EntityActionTrigger.BeforeSave, "CheckCode", "بررسی و تولید کد پروژه", Priority = 1)]
		public async Task CheckCode(Project entity, CancellationToken ct)
		{
		  
			if (!entity.Code.HasValue())
				entity.Code = GetProjectCode(entity);

			await Task.CompletedTask;
		}

		[EntityAction(typeof(Project), EntityActionTrigger.BeforeAdd, "StampProjectAttachmentAuditOnAdd", "ثبت تاریخ و ایجادکننده پیوست پروژه", Priority = 5)]
		public Task StampProjectAttachmentAuditOnAdd(Project entity, CancellationToken ct)
			=> StampProjectAttachmentAudit(entity, ct);

		[EntityAction(typeof(Project), EntityActionTrigger.BeforeUpdate, "StampProjectAttachmentAuditOnUpdate", "ثبت تاریخ و ایجادکننده پیوست پروژه", Priority = 5)]
		public Task StampProjectAttachmentAuditOnUpdate(Project entity, CancellationToken ct)
			=> StampProjectAttachmentAudit(entity, ct);

		private async Task StampProjectAttachmentAudit(Project entity, CancellationToken ct)
		{
			var attachments = entity.ProjectAttachments;
			if (attachments == null || attachments.Count == 0)
				return;

			var existingIds = attachments
				.Where(a => a.Id is > 0)
				.Select(a => a.Id!.Value)
				.Distinct()
				.ToList();

			var previous = existingIds.Count == 0
				? new Dictionary<long, ProjectAttachment>()
				: await unitOfWork.Repository<ProjectAttachment>().TableNoTracking
					.Where(a => a.Id.HasValue && existingIds.Contains(a.Id.Value))
					.ToDictionaryAsync(a => a.Id!.Value, ct);

			var now = DateTime.Now;
			var user = sdk.CurrentUser;

			foreach (var attachment in attachments)
			{
				if (attachment.ProjectId == 0 && entity.Id is > 0)
					attachment.ProjectId = entity.Id.Value;

				if (attachment.Id is > 0 && previous.TryGetValue(attachment.Id.Value, out var stored))
				{
					attachment.CreatedById = stored.CreatedById;
					attachment.CreatedByName = stored.CreatedByName;
					attachment.CreatedOnMiladiDateTime = stored.CreatedOnMiladiDateTime;
					attachment.CreatedOnShamsiDateTime = stored.CreatedOnShamsiDateTime;
					attachment.IsActive = stored.IsActive;
					attachment.HtsId = stored.HtsId;
					attachment.DocumentCode = stored.DocumentCode;
					attachment.DocumentName = stored.DocumentName;
					continue;
				}

				attachment.CreatedOnMiladiDateTime ??= now;
				if (!attachment.CreatedOnShamsiDateTime.HasValue())
					attachment.CreatedOnShamsiDateTime = now.ToShamsiDateTime();
				attachment.IsActive ??= IsActiveEnum.Active;

				if (user == null)
					continue;

				if (!attachment.CreatedById.HasValue)
					attachment.CreatedById = user.Id;
				if (!attachment.CreatedByName.HasValue())
					attachment.CreatedByName = user.FullName;
			}
		}


		private string GetOrganizationUnit(long organizationUnitId)
		{

			var orgUnitCode = unitOfWork.Repository<OrgUnit>()
				.TableNoTracking
				.Select(c => new { c.Id, c.ProjectPrefixCode })
				.FirstOrDefault(c => c.Id == organizationUnitId);

			if (orgUnitCode == null)
				throw new Exception("واحد سازمانی مورد نظر یافت نشد.");


			if (!orgUnitCode.ProjectPrefixCode.HasValue())
				throw new Exception("برای واحد سازمانی انتخاب شده پیش کد پروژه تعریف نشده .");

			return orgUnitCode.ProjectPrefixCode;



			//    1   مهندسی فروش NULL    121000002
			//    24  هئیت مدیره  NULL    121000082
			//    30  فروش گازهای صنعتی NULL    121000090
			//    31  فروش کمپرسورهای فرآیندی NULL    121000091
			//    32  فروش کمپرسورهای صنعتی NULL    121000092
			//    33  فروش تجهیزات پزشکی NULL    121000093
			//    140 فروش کمپرسورهای مهندسی NULL    121000133
			//    142 فروش توربو ماشین NULL    121000135
			//    225 فروش صنعتی  NULL    121000149

			switch (organizationUnitId)
			{
				case 1:
				case 140:
					return "EC";
				case 24:
					return "EN";
				case 30:
					return "IG";
				case 31:
					return "EN";
				case 32:
					return "IC";
				case 33:
					return "ME";
				case 41:
					return "TC";
				case 131:
					return "IG";
				case 142:
					return "TM";
				case 225:
					return "IS";
				case 226:
					return "HGI";
			}


		}

		private string GetProjectCode(Project projectEntity)
		{
			//XX / XX / XX / 00
			//SP , VP / شمارنده اتوماتیک / سال / مخفف واحدهای فروش

			//SP: Special project
			//VP: Vendor project
			//TP: Takvin project

			var latestOrganizationUnit = unitOfWork.Repository<Project>().TableNoTracking
				.Where(p => p.SubjectUnitId == projectEntity.SubjectUnitId)
			    .OrderByDescending(p => p.Id)
			    .FirstOrDefault();

			var latestOrganizationUnitCounterInText = latestOrganizationUnit != null && latestOrganizationUnit.Code.HasValue() ? latestOrganizationUnit.Code.Split('/').Last() : "00";

			var now = DateTime.Now;
			var year = now.GetShamsiYear();

			if (latestOrganizationUnit != null && latestOrganizationUnit.Code.HasValue())
			{
				var latestOrganizationUnitCodeYear = latestOrganizationUnit.Code.Split('/')[2];
				if (latestOrganizationUnitCodeYear != year.ToString())
				{
					latestOrganizationUnitCounterInText = "00";
				}
			}


			var projectType = projectEntity.IsTakvinProject ? "TP" : projectEntity.ProjectIsVendoriType ? "VP" : "SP";
			var counter = $"{(Convert.ToInt32(latestOrganizationUnitCounterInText) + 1):00}";

		SetAgian:

			var projectCode = $"{projectType}/{GetOrganizationUnit(projectEntity.SubjectUnitId)}/{now.GetShamsiYear()}/{counter}";

			var isAlreadyExist = unitOfWork.Repository<Project>().TableNoTracking.
				Any(p => p.Code == projectCode);
			if (!isAlreadyExist)
				return projectCode;


			counter += 1;
			goto SetAgian;


		}


	}
}
