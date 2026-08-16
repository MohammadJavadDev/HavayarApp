using Common.Utilities;
using Data.Contracts;
using Data.Contracts.Actions;
using Entities.App.Sale;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Actions.Sale
{
	/// <summary>
	/// معادل تریگرهای HTS:
	/// - UpdateProductionOrderItemVersionInfo (شماره‌گذاری Revision)
	/// - Pln_ProductionOrder_Itm_Serial_Ins_Upd (اقساط از PreparationDate) — روی خود قلم چون جدول Serial جدا نیست
	/// </summary>
	public class ProductionOrderItemAction(IUnitOfWork unitOfWork)
	{
		[EntityAction(typeof(ProductionOrderItem), EntityActionTrigger.AfterAdd,
			"RenumberProductionOrderItemRevision", "شماره‌گذاری نسخه قلم سفارش ساخت پس از درج", Priority = 1)]
		public Task AfterAdd(ProductionOrderItem model, CancellationToken ct)
			=> ApplyAsync(model, ct);

		[EntityAction(typeof(ProductionOrderItem), EntityActionTrigger.AfterUpdate,
			"RenumberProductionOrderItemRevisionUpdate", "شماره‌گذاری نسخه / اقساط پس از ویرایش قلم", Priority = 1)]
		public Task AfterUpdate(ProductionOrderItem model, CancellationToken ct)
			=> ApplyAsync(model, ct);

		[EntityAction(typeof(ProductionOrderItem), EntityActionTrigger.AfterDelete,
			"RenumberProductionOrderItemRevisionDelete", "شماره‌گذاری نسخه پس از حذف قلم", Priority = 1)]
		public Task AfterDelete(ProductionOrderItem model, CancellationToken ct)
			=> RenumberRevisionsAsync(model.ProductionOrderId, model.PartId, ct);

		private async Task ApplyAsync(ProductionOrderItem model, CancellationToken ct)
		{
			await RenumberRevisionsAsync(model.ProductionOrderId, model.PartId, ct);
			await SyncInstallmentDatesAsync(model, ct);
		}

		/// <summary>
		/// معادل تریگر: Revision از -1 به بالا برای همه ردیف‌های هم‌گروه ProductionOrderId+PartId
		/// </summary>
		private async Task RenumberRevisionsAsync(long? productionOrderId, long? partId, CancellationToken ct)
		{
			if (productionOrderId == null || productionOrderId <= 0 || partId == null || partId <= 0)
				return;

			var siblings = await unitOfWork.Repository<ProductionOrderItem>()
				.Table
				.Where(c => c.ProductionOrderId == productionOrderId && c.PartId == partId && !c.IsDeleted)
				.OrderBy(c => c.Id)
				.Select(c => c.Id)
				.ToListAsync(ct);

			decimal revision = -1;
			foreach (var id in siblings)
			{
				if (id == null) continue;
				await unitOfWork.Repository<ProductionOrderItem>()
					.UpdateFieldsAsync(id, new Dictionary<string, object>
					{
						[nameof(ProductionOrderItem.Revision)] = revision
					}, ct);
				revision++;
			}
		}

		/// <summary>
		/// معادل تریگر Serial: +30/+60/+90/+120 روز از PreparationDate → تاریخ اقساط شمسی
		/// </summary>
		private async Task SyncInstallmentDatesAsync(ProductionOrderItem model, CancellationToken ct)
		{
			if (model.Id == null || model.Id <= 0)
				return;

			if (model.PreparationMiladiDate == null)
			{
				var empty = new Dictionary<string, object>
				{
					[nameof(ProductionOrderItem.SecondInstallmentShamsiDate)] = null!,
					[nameof(ProductionOrderItem.ThirdInstallmentShamsiDate)] = null!,
					[nameof(ProductionOrderItem.FourthInstallmentShamsiDate)] = null!,
					[nameof(ProductionOrderItem.FifthInstallmentShamsiDate)] = null!
				};
				await unitOfWork.Repository<ProductionOrderItem>().UpdateFieldsAsync(model.Id, empty, ct);
				return;
			}

			var baseDate = model.PreparationMiladiDate.Value.Date;
			var fields = new Dictionary<string, object>
			{
				[nameof(ProductionOrderItem.SecondInstallmentShamsiDate)] = baseDate.AddDays(30).ToShamsiDate(),
				[nameof(ProductionOrderItem.ThirdInstallmentShamsiDate)] = baseDate.AddDays(60).ToShamsiDate(),
				[nameof(ProductionOrderItem.FourthInstallmentShamsiDate)] = baseDate.AddDays(90).ToShamsiDate(),
				[nameof(ProductionOrderItem.FifthInstallmentShamsiDate)] = baseDate.AddDays(120).ToShamsiDate()
			};
			await unitOfWork.Repository<ProductionOrderItem>().UpdateFieldsAsync(model.Id, fields, ct);
		}
	}
}
