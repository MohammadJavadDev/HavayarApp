using Data.Contracts;
using Data.Contracts.Actions;
using Entities.App.Sale;
using Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Actions.Sale
{
	/// <summary>
	/// معادل تریگر HTS: UpdateProductionOrderItemBomVersionInfo روی Pln_ProductionOrderItemBom
	/// </summary>
	public class ProductionOrderItemBomAction(IUnitOfWork unitOfWork)
	{
		[EntityAction(typeof(ProductionOrderItemBom), EntityActionTrigger.AfterAdd,
			"SyncBomVersionInfo", "همگام‌سازی نسخه BOM پس از درج", Priority = 1)]
		public Task AfterAdd(ProductionOrderItemBom model, CancellationToken ct)
			=> SyncVersionInfoAsync(model.ProductionOrderItemId, model.PartId, ct);

		[EntityAction(typeof(ProductionOrderItemBom), EntityActionTrigger.AfterUpdate,
			"SyncBomVersionInfoUpdate", "همگام‌سازی نسخه BOM پس از ویرایش", Priority = 1)]
		public Task AfterUpdate(ProductionOrderItemBom model, CancellationToken ct)
			=> SyncVersionInfoAsync(model.ProductionOrderItemId, model.PartId, ct);

		[EntityAction(typeof(ProductionOrderItemBom), EntityActionTrigger.AfterDelete,
			"SyncBomVersionInfoDelete", "همگام‌سازی نسخه BOM پس از حذف", Priority = 1)]
		public Task AfterDelete(ProductionOrderItemBom model, CancellationToken ct)
			=> SyncVersionInfoAsync(model.ProductionOrderItemId, model.PartId, ct);

		private async Task SyncVersionInfoAsync(long productionOrderItemId, long partId, CancellationToken ct)
		{
			if (productionOrderItemId <= 0 || partId <= 0)
				return;

			var siblings = await unitOfWork.Repository<ProductionOrderItemBom>()
				.Table
				.Where(c => c.ProductionOrderItemId == productionOrderItemId
					&& c.PartId == partId
					&& c.IsActive == IsActiveEnum.Active)
				.OrderBy(c => c.Id)
				.Select(c => c.Id)
				.ToListAsync(ct);

			if (siblings.Count == 0)
				return;

			int revision = -1;
			long? maxId = siblings.Where(id => id != null).Max();

			foreach (var id in siblings)
			{
				if (id == null) continue;
				var fields = new Dictionary<string, object>
				{
					[nameof(ProductionOrderItemBom.Revision)] = revision,
					[nameof(ProductionOrderItemBom.IsLatest)] = id == maxId
				};
				await unitOfWork.Repository<ProductionOrderItemBom>().UpdateFieldsAsync(id, fields, ct);
				revision++;
			}
		}
	}
}
