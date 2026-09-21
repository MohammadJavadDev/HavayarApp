using Common.Utilities;
using Data.Contracts;
using Data.Contracts.Actions;
using Entities.App.Gnr;
using Entities.App.Sale;
using Entities.App.Sale.Enums;
using Entities.App.SLS;
using Entities.Auth;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Microsoft.EntityFrameworkCore;
using Services.Auth;

namespace WebApp.Actions.Sale
{
	/// <summary>
	/// معادل DoOperation مناقصه HTS: پیش‌فرض درج، تسک خودکار، به‌روزرسانی شرکت، ارسال به تایید، هشدار تکراری.
	/// </summary>
	public class TenderManagementAction(
		IUnitOfWork unitOfWork,
		IUserService userService)
	{
		public const string RoleApprover = "Sale.Tenders.Approver";
		public const string RoleDuplicateNotify = "Sale.Tenders.DuplicateNotify";
		public const string RoleManager = "Sale.Tenders.Manager";
		public const string RoleShowAll = "Sale.Tenders.ShowAll";
		public const string RoleCompressedAir = "Sale.Tenders.CompressedAir";
		public const string RoleOilGas = "Sale.Tenders.OilGas";

		[EntityAction(typeof(TenderManagement), EntityActionTrigger.BeforeAdd,
			"TenderDefaultsOnAdd", "پیش‌فرض‌های درج مناقصه خدمات پس از فروش", Priority = 1)]
		public Task BeforeAdd(TenderManagement entity, CancellationToken ct)
		{
			entity.CurrentStatus ??= TenderStatusEnum.FollowingUp;
			entity.HasPrimitiveConfirm = true;
			entity.HasSecondaryConfirm = false;
			entity.InCartable = true;
			entity.RequestNumber = null;
			ApplyContractPrice(entity);
			return Task.CompletedTask;
		}

		[EntityAction(typeof(TenderManagement), EntityActionTrigger.BeforeUpdate,
			"TenderUpdateRules", "قواعد ویرایش مناقصه خدمات پس از فروش", Priority = 1)]
		public async Task BeforeUpdate(TenderManagement entity, CancellationToken ct)
		{
			if (entity.Id == null || entity.Id == 0)
				return;

			var existing = await unitOfWork.Repository<TenderManagement>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == entity.Id, ct);
			if (existing == null)
				return;

			entity.PreviousCurrentStatus = existing.CurrentStatus;
			entity.PreviousFinalResult = existing.FinalResult;
			entity.PreviousInCartable = existing.InCartable;

			if (entity.FinalResult == TenderFinalResultEnum.BuyFromHavayar)
			{
				var hasPart = await unitOfWork.Repository<TenderPart>().TableNoTracking
					.AnyAsync(c => c.TenderId == entity.Id, ct);
				if (!hasPart)
					throw new InvalidOperationException("برای نتیجه «خرید از هوایار» حداقل یک قلم مناقصه لازم است.");
			}

			ApplyContractPrice(entity);

			if (existing.InCartable == true && entity.InCartable == false)
			{
				var now = DateTime.Now;
				entity.SendForApproveMiladiDateTime = now;
				entity.SendForApproveShamsiDateTime = now.ToShamsiDateTime();
			}
		}

		[EntityAction(typeof(TenderManagement), EntityActionTrigger.AfterAdd,
			"TenderAfterAdd", "تسک خودکار، به‌روزرسانی شرکت و هشدار تکراری", Priority = 1)]
		public async Task AfterAdd(TenderManagement entity, CancellationToken ct)
		{
			await UpdateCompanyAsync(entity, ct);
			await AddTaskAsync(entity.Id, "مناقصه جدید با وضعیت در حال پیگیری ثبت شد", ct);
			await NotifyDuplicateCompanyAsync(entity, ct);
		}

		[EntityAction(typeof(TenderManagement), EntityActionTrigger.AfterUpdate,
			"TenderAfterUpdate", "تسک تغییر وضعیت/نتیجه، به‌روزرسانی شرکت و ایمیل تایید", Priority = 1)]
		public async Task AfterUpdate(TenderManagement entity, CancellationToken ct)
		{
			await UpdateCompanyAsync(entity, ct);

			if (entity.PreviousCurrentStatus != entity.CurrentStatus && entity.CurrentStatus != null)
			{
				var status = entity.CurrentStatus.Value.ToDisplay();
				await AddTaskAsync(entity.Id, "وضعیت فعلی پروژه به حالت " + status + " تغییر یافت.", ct);
			}

			if (entity.PreviousFinalResult != entity.FinalResult && entity.FinalResult != null)
			{
				var result = entity.FinalResult.Value.ToDisplay();
				await AddTaskAsync(entity.Id, "وضعیت نهایی پروژه به حالت " + result + " تغییر یافت.", ct);
			}

			var leftCartable = entity.PreviousInCartable == true && entity.InCartable == false;
			if (leftCartable && !entity.SkipCartableEmail)
			{
				await NotifyRoleAsync(RoleApprover, "اعلان ثبت مناقصه خدمات پس از فروش جدید",
					"<div style='direction:rtl;text-align:right'>مناقصه جدید ثبت شد.<br/>شماره مرجع مناقصه : "
					+ entity.Id + "</div>", entity.Id, ct);
			}
		}

		private static void ApplyContractPrice(TenderManagement entity)
		{
			if (entity.CompressorTechPrice != null && entity.CompressorTechPriceRate != null)
			{
				entity.ContractPrice = entity.CompressorTechPrice * entity.CompressorTechPriceRate;
				entity.CompressorTechPriceInRial = entity.ContractPrice;
			}
		}

		private async Task AddTaskAsync(long? tenderId, string comment, CancellationToken ct)
		{
			if (tenderId == null || tenderId == 0)
				return;
			await unitOfWork.Repository<TenderTask>().SaveAsync(new TenderTask
			{
				TenderId = tenderId,
				Comment = comment
			}, ct, true);
		}

		private async Task UpdateCompanyAsync(TenderManagement entity, CancellationToken ct)
		{
			if (entity.CompanyId == null || entity.CompanyId == 0)
				return;

			var party = await unitOfWork.Repository<Party>().Table.FirstOrDefaultAsync(c => c.Id == entity.CompanyId, ct);
			if (party == null)
				return;

			var changed = false;
			if (!string.IsNullOrWhiteSpace(entity.CompanyPhone) && party.Phone != entity.CompanyPhone)
			{
				party.Phone = entity.CompanyPhone.Length > 50 ? entity.CompanyPhone[..50] : entity.CompanyPhone;
				changed = true;
			}
			if (entity.CompanyAddress != null && party.Address != entity.CompanyAddress)
			{
				party.Address = entity.CompanyAddress.Length > 2000 ? entity.CompanyAddress[..2000] : entity.CompanyAddress;
				changed = true;
			}
			if (changed)
				await unitOfWork.Repository<Party>().UpdateAsync(party, ct, true, true, false);

			if (entity.CompanyIndustryItemId == null || entity.CompanyIndustryItemId == 0)
				return;

			var item = await unitOfWork.Repository<PartyIndustryItem>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == entity.CompanyIndustryItemId, ct);
			if (item == null)
				return;

			var customer = await unitOfWork.Repository<Customer>().Table
				.FirstOrDefaultAsync(c => c.PartyId == entity.CompanyId, ct);
			if (customer != null && customer.IndustryId != item.PartyIndustryId)
			{
				customer.IndustryId = item.PartyIndustryId;
				await unitOfWork.Repository<Customer>().UpdateAsync(customer, ct, true, true, false);
			}
		}

		private async Task NotifyDuplicateCompanyAsync(TenderManagement entity, CancellationToken ct)
		{
			if (entity.CompanyId == null)
				return;
			var duplicate = await unitOfWork.Repository<TenderManagement>().TableNoTracking
				.AnyAsync(c => c.CompanyId == entity.CompanyId && c.Id != entity.Id, ct);
			if (!duplicate)
				return;

			var others = await unitOfWork.Repository<TenderManagement>().TableNoTracking
				.Where(c => c.CompanyId == entity.CompanyId)
				.OrderByDescending(c => c.Id)
				.Take(20)
				.ToListAsync(ct);
			var body = "<div style='direction:rtl;text-align:right'>مناقصه جدید در سیستم ثبت گردید که تکراری می‌باشد.<br/>"
				+ string.Join("<br/>", others.Select(c => $"{c.Id} — {c.ProjectName}"))
				+ "</div>";
			await NotifyRoleAsync(RoleDuplicateNotify, "اعلان ثبت مناقصه تکراری", body, entity.Id, ct);
		}

		internal async Task NotifyRoleAsync(string roleName, string title, string body, long? entityId, CancellationToken ct)
		{
			var users = await userService.GetUsersByRoleName(roleName);
			if (users == null || users.Length == 0)
				return;
			foreach (var user in users)
			{
				if (user.Id == null)
					continue;
				await unitOfWork.Repository<Notification>().AddAsync(new Notification
				{
					Type = NotificationType.Email,
					Title = title,
					Body = body,
					EntityId = entityId,
					OwnerId = user.Id.Value,
					ViewPath = "/Panel/Sale/TenderManagement/Edit?id=" + entityId,
					IsRead = false,
					IsSend = false,
					ToEmails = string.IsNullOrWhiteSpace(user.Email) ? null : new List<string> { user.Email }
				}, ct);
			}
			await unitOfWork.SaveChangesAsync(ct);
		}
	}
}
