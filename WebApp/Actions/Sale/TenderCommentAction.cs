using Data.Contracts;
using Data.Contracts.Actions;
using Entities.App.Sale;
using Entities.Auth;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Microsoft.EntityFrameworkCore;
using Services.Auth;

namespace WebApp.Actions.Sale
{
	public class TenderCommentAction(IUnitOfWork unitOfWork, IUserService userService)
	{
		[EntityAction(typeof(TenderComment), EntityActionTrigger.AfterAdd,
			"TenderCommentReturnToCartable", "بازگشت مناقصه به کارتابل پس از یادداشت", Priority = 1)]
		public async Task AfterAdd(TenderComment entity, CancellationToken ct)
		{
			if (entity.TenderId == null || entity.TenderId == 0)
				return;

			var tender = await unitOfWork.Repository<TenderManagement>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == entity.TenderId, ct);
			if (tender == null)
				return;

			tender.InCartable = true;
			tender.SkipCartableEmail = true;
			await unitOfWork.Repository<TenderManagement>().UpdateAsync(tender, ct, true, true, false);

			var receivers = new List<User>();
			if (tender.SaleExpertId != null)
			{
				var expert = await userService.GetById(tender.SaleExpertId.Value);
				if (expert != null) receivers.Add(expert);
			}
			if (tender.CreatedById != null)
			{
				var creator = await userService.GetById(tender.CreatedById.Value);
				if (creator != null && receivers.All(c => c.Id != creator.Id))
					receivers.Add(creator);
			}

			var companyName = tender.ProjectName;
			var body = "<div style='direction:rtl;text-align:right'><strong>درج کامنت برای مناقصه</strong><br/>"
				+ "پروژه: " + companyName
				+ "<br/>متن کامنت: " + entity.Comment
				+ "<br/>شماره درخواست: " + tender.Id + "</div>";
			foreach (var user in receivers)
			{
				if (user.Id == null) continue;
				await unitOfWork.Repository<Notification>().AddAsync(new Notification
				{
					Type = NotificationType.Email,
					Title = "کامنت مناقصه",
					Body = body,
					EntityId = tender.Id,
					OwnerId = user.Id.Value,
					ViewPath = "/Panel/Sale/TenderManagement/Edit?id=" + tender.Id,
					IsRead = false,
					IsSend = false,
					ToEmails = string.IsNullOrWhiteSpace(user.Email) ? null : new List<string> { user.Email }
				}, ct);
			}
		}
	}
}
