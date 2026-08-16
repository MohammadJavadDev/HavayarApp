using Common.Utilities;
using Data.Contracts;
using Data.Contracts.Actions;
using Entities.App.Sale;
using Entities.App.Sale.Enums;
using Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Actions.Sale
{
	/// <summary>
	/// معادل تریگر HTS: UpdateProductionOrderItemBuyStatus روی Pln_ProductionOrderItemInquiry
	/// آخرین استعلام → CheckStatus قلم (معادل BuyStatusId)
	/// </summary>
	public class ProductionOrderItemInquiryAction(IUnitOfWork unitOfWork)
	{
		[EntityAction(typeof(ProductionOrderItemInquiry), EntityActionTrigger.AfterAdd,
			"SyncBuyStatusFromInquiry", "همگام‌سازی وضعیت بررسی از استعلام", Priority = 1)]
		public Task AfterAdd(ProductionOrderItemInquiry model, CancellationToken ct)
			=> SyncCheckStatusAsync(model.ProductionOrderItemId, ct);

		[EntityAction(typeof(ProductionOrderItemInquiry), EntityActionTrigger.AfterUpdate,
			"SyncBuyStatusFromInquiryUpdate", "همگام‌سازی وضعیت بررسی پس از ویرایش استعلام", Priority = 1)]
		public Task AfterUpdate(ProductionOrderItemInquiry model, CancellationToken ct)
			=> SyncCheckStatusAsync(model.ProductionOrderItemId, ct);

		[EntityAction(typeof(ProductionOrderItemInquiry), EntityActionTrigger.AfterDelete,
			"SyncBuyStatusFromInquiryDelete", "همگام‌سازی وضعیت بررسی پس از حذف استعلام", Priority = 1)]
		public Task AfterDelete(ProductionOrderItemInquiry model, CancellationToken ct)
			=> SyncCheckStatusAsync(model.ProductionOrderItemId, ct);

		private async Task SyncCheckStatusAsync(long productionOrderItemId, CancellationToken ct)
		{
			if (productionOrderItemId <= 0)
				return;

			var latestStatus = await unitOfWork.Repository<ProductionOrderItemInquiry>()
				.TableNoTracking
				.Where(c => c.ProductionOrderItemId == productionOrderItemId
					&& c.IsActive == IsActiveEnum.Active)
				.OrderByDescending(c => c.Id)
				.Select(c => (ProductionOrderItemCheckStatusEnum?)c.Status)
				.FirstOrDefaultAsync(ct);

			if (latestStatus == null)
				return;

			var now = DateTime.Now;
			var fields = new Dictionary<string, object>
			{
				[nameof(ProductionOrderItem.CheckStatus)] = latestStatus.Value,
				[nameof(ProductionOrderItem.CheckStatusChangedOnMiladiDate)] = now,
				[nameof(ProductionOrderItem.CheckStatusChangedOnShamsiDate)] = now.ToShamsiDateTime()
			};

			await unitOfWork.Repository<ProductionOrderItem>()
				.UpdateFieldsAsync(productionOrderItemId, fields, ct);
		}
	}
}
