using Common.Utilities;
using Data.Contracts;
using Data.Contracts.Actions;
using Data.SystemAuth;
using Entities.App.Edms;
using Entities.App.Edms.Enums;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Actions.Edms
{
    public class DocumentCommentAction(IUnitOfWork unitOfWork,ISdk sdk)  
	{

		[EntityAction(typeof(DocumentComment), EntityActionTrigger.AfterAdd, "ChangeDocumentStatus", "تغییر وضعیت مدرک", Priority = 1)]
		public async Task ChangeDocumentStatus(DocumentComment model, CancellationToken ct)
		{

			if (model.Status == null)
				throw new Exception("وضعیت مدرک انتخاب نشده است");

			var now = DateTime.Now;
			var document = await unitOfWork.Repository<Document>().
				Table
				.FirstAsync(c => c.Id == model.DocumentId);
			 

			if (document.ApproverId == sdk.CurrentUser.Id)
			{
				if (model.Status == DocumentStatusEnums.ApproveByApprover)
				{
					document.ApprovedShamsiDateTime = now.ToShamsiDateTime();
					document.ApprovedMiladiDateTime = now;
				}

			}

			if (document.ReviewerId == sdk.CurrentUser.Id)
			{
				if (model.Status == DocumentStatusEnums.ApproveByReviewer)
				{
					document.RevieweShamsiDateTime = now.ToShamsiDateTime();
					document.RevieweMiladiDateTime = now;
				}

			}
			document.Status = model.Status;
			document.ModifiedDateMiladiDateTime = DateTime.Now;
			document.ModifiedDateShamsiDateTime = DateTime.Now.ToShamsiDateTime();
			document.ModifiedById = sdk.CurrentUser.Id;
			document.ModifiedByName = sdk.CurrentUser.FullName;

			await unitOfWork.SaveChangesAsync(ct);

			await Task.CompletedTask;
		}
	}
}
