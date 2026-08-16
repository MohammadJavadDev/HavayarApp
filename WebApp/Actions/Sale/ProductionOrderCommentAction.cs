using Data.Contracts;
using Data.Contracts.Actions;
using Entities.App.Sale;
using Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Actions.Sale
{
	/// <summary>
	/// معادل تریگر HTS: UpdateProductionOrderStatus روی Pln_ProductionOrderComment
	/// آخرین کامنت → State سفارش ساخت
	/// </summary>
	public class ProductionOrderCommentAction(IUnitOfWork unitOfWork)
	{
		[EntityAction(typeof(ProductionOrderComment), EntityActionTrigger.AfterAdd,
			"SyncProductionOrderStateFromComment", "همگام‌سازی وضعیت سفارش ساخت از کامنت", Priority = 1)]
		public Task SyncAfterAdd(ProductionOrderComment model, CancellationToken ct)
			=> SyncStateAsync(model.ProductionOrderId, ct);

		[EntityAction(typeof(ProductionOrderComment), EntityActionTrigger.AfterUpdate,
			"SyncProductionOrderStateFromCommentUpdate", "همگام‌سازی وضعیت سفارش ساخت پس از ویرایش کامنت", Priority = 1)]
		public Task SyncAfterUpdate(ProductionOrderComment model, CancellationToken ct)
			=> SyncStateAsync(model.ProductionOrderId, ct);

		[EntityAction(typeof(ProductionOrderComment), EntityActionTrigger.AfterDelete,
			"SyncProductionOrderStateFromCommentDelete", "همگام‌سازی وضعیت سفارش ساخت پس از حذف کامنت", Priority = 1)]
		public Task SyncAfterDelete(ProductionOrderComment model, CancellationToken ct)
			=> SyncStateAsync(model.ProductionOrderId, ct);

		private async Task SyncStateAsync(long productionOrderId, CancellationToken ct)
		{
			if (productionOrderId <= 0)
				return;

			var latest = await unitOfWork.Repository<ProductionOrderComment>()
				.TableNoTracking
				.Where(c => c.ProductionOrderId == productionOrderId
					&& c.IsActive == IsActiveEnum.Active
					&& c.NewState != null)
				.OrderByDescending(c => c.Id)
				.Select(c => new { c.NewState, c.Comment, c.CreatedById })
				.FirstOrDefaultAsync(ct);

			if (latest?.NewState == null)
				return;

			await unitOfWork.Repository<ProductionOrder>()
				.UpdateFieldsAsync(productionOrderId,
					c => c.State,
					latest.NewState.Value,
					ct);
		}
	}
}
