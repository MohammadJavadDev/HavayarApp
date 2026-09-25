using Data.Contracts;
using Data.Contracts.Actions;
using Microsoft.EntityFrameworkCore;
using CngWorkReport = Entities.App.Cng.WorkReport;

namespace WebApp.Actions.Cng
{
	/// <summary>
	/// معادل منطق حفظ فیلدهای ایجاد و فایل در HTS CngWorkReportController.DoOperation (Update).
	/// به‌روزرسانی نماینده جایگاه در کنترلر Save است (فیلدهای فرم، نه ستون WorkReport).
	/// </summary>
	public class WorkReportAction(IUnitOfWork unitOfWork)
	{
		[EntityAction(typeof(CngWorkReport), EntityActionTrigger.BeforeUpdate,
			"WorkReportPreserveAuditAndFile", "حفظ فیلدهای ایجاد و فایل گزارش کار CNG", Priority = 1)]
		public async Task BeforeUpdate(CngWorkReport entity, CancellationToken ct)
		{
			if (entity.Id == null || entity.Id == 0)
				return;

			var existing = await unitOfWork.Repository<CngWorkReport>().TableNoTracking
				.FirstOrDefaultAsync(r => r.Id == entity.Id, ct);
			if (existing == null)
				return;

			entity.HtsId = existing.HtsId;
			entity.CreatedById = existing.CreatedById;
			entity.CreatedByName = existing.CreatedByName;
			entity.CreatedOnMiladiDateTime = existing.CreatedOnMiladiDateTime;
			entity.CreatedOnShamsiDateTime = existing.CreatedOnShamsiDateTime;

			entity.IsApprovedByOwner = existing.IsApprovedByOwner;
			entity.OwnerComment = existing.OwnerComment;
			entity.IsSurveyLinkSended = existing.IsSurveyLinkSended;
			entity.SurveyLinkSendedDateTime = existing.SurveyLinkSendedDateTime;
			entity.Comment = existing.Comment;

			var newFileUploaded = entity.FileId is > 0 && entity.FileId != existing.FileId;
			if (!newFileUploaded)
			{
				entity.FileId = existing.FileId;
				entity.WorkReportFileAddress = existing.WorkReportFileAddress;
				entity.WorkReportFileType = existing.WorkReportFileType;
			}
		}
	}
}
