using Data.Contracts;
using Data.Contracts.Actions;
using Entities.App.Sale;
using Entities.App.Sale.Enums;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Actions.Sale
{
	/// <summary>
	/// معادل تریگر HTS: UpdatePlnProductionOrderItemLastStatus روی Pln_ProductionOrder_Itm_New_Comment
	/// پس از درج/ویرایش/حذف ProductionOrderItemComment، وضعیت‌های والد را همگام می‌کند.
	/// </summary>
	public class ProductionOrderItemCommentAction(IUnitOfWork unitOfWork)
	{
		/// <summary>
		/// وضعیت‌های تولید (LookupType 238 در HTS) — باعث IsForProductionMode و به‌روز شدن ProductionStatus می‌شوند.
		/// 3166/3167 برای توقف بازدید کارفرما در سیستم جدید اضافه شده‌اند.
		/// </summary>
		private static readonly HashSet<int> ProductionModeStatusIds =
		[
			(int)ProductionOrderItemProductionStatusEnum.ProductionCompleted,           // 1789
			(int)ProductionOrderItemProductionStatusEnum.FinalInspectionStarted,        // 1790
			(int)ProductionOrderItemProductionStatusEnum.PackagingCompleted,            // 1791
			(int)ProductionOrderItemProductionStatusEnum.ProductionStarted,             // 1792
			(int)ProductionOrderItemProductionStatusEnum.ProductionStageStopStarted,    // 1795
			(int)ProductionOrderItemProductionStatusEnum.FinalInspectionStageStopStarted, // 1796
			(int)ProductionOrderItemProductionStatusEnum.FinalInspectionStageStopEnded, // 1842
			(int)ProductionOrderItemProductionStatusEnum.FinalInspectionCompleted,      // 1843
			(int)ProductionOrderItemProductionStatusEnum.ProductionStageStopEnded,      // 1844
			(int)ProductionOrderItemProductionStatusEnum.ProductionTestStarted,         // 2209
			(int)ProductionOrderItemProductionStatusEnum.ProductionTestCompleted,       // 2210
			(int)ProductionOrderItemProductionStatusEnum.ProductionTestStageStopStarted, // 2226
			(int)ProductionOrderItemProductionStatusEnum.ProductionTestStageStopEnded,  // 2227
			(int)ProductionOrderItemProductionStatusEnum.ClientVisitStopEnded,          // 3166
			(int)ProductionOrderItemProductionStatusEnum.ClientVisitStopStarted         // 3167
		];

		/// <summary>
		/// وضعیت‌هایی که ملاحظات مهندسی را روی قلم به‌روز می‌کنند (همان لیست تریگر قدیم).
		/// </summary>
		private static readonly HashSet<int> EngineeringConsiderationStatusIds =
		[
			(int)ProductionOrderItemProductionStatusEnum.MechanicalEngineeringApproved, // 2197
			(int)ProductionOrderItemProductionStatusEnum.MechanicalEngineeringRejected, // 2198
			(int)ProductionOrderItemProductionStatusEnum.ProjectManagerApproved,        // 2259
			(int)ProductionOrderItemProductionStatusEnum.ProjectManagerRejected         // 2260
		];

		[EntityAction(typeof(ProductionOrderItemComment), EntityActionTrigger.AfterAdd,
			"SyncProductionOrderItemFromComment", "همگام‌سازی وضعیت قلم سفارش ساخت پس از درج کامنت", Priority = 1)]
		public Task SyncAfterAdd(ProductionOrderItemComment model, CancellationToken ct)
			=> SyncParentAsync(model, isDeleteMode: false, ct);

		[EntityAction(typeof(ProductionOrderItemComment), EntityActionTrigger.AfterUpdate,
			"SyncProductionOrderItemFromCommentUpdate", "همگام‌سازی وضعیت قلم سفارش ساخت پس از ویرایش کامنت", Priority = 1)]
		public Task SyncAfterUpdate(ProductionOrderItemComment model, CancellationToken ct)
			=> SyncParentAsync(model, isDeleteMode: false, ct);

		[EntityAction(typeof(ProductionOrderItemComment), EntityActionTrigger.AfterDelete,
			"SyncProductionOrderItemFromCommentDelete", "همگام‌سازی وضعیت قلم سفارش ساخت پس از حذف کامنت", Priority = 1)]
		public Task SyncAfterDelete(ProductionOrderItemComment model, CancellationToken ct)
			=> SyncParentAsync(model, isDeleteMode: true, ct);

		private async Task SyncParentAsync(ProductionOrderItemComment model, bool isDeleteMode, CancellationToken ct)
		{
			if (model.ProductionOrderItemId <= 0)
				return;

			var statusId = (int)model.ProductionStatus;
			var parentId = model.ProductionOrderItemId;

			if (!isDeleteMode)
			{
				if (ProductionModeStatusIds.Contains(statusId))
				{
					if (!model.IsForProductionMode && model.Id is > 0)
					{
						await unitOfWork.Repository<ProductionOrderItemComment>()
							.UpdateFieldsAsync(model.Id, c => c.IsForProductionMode, true, ct);
						model.IsForProductionMode = true;
					}
					else if (!model.IsForProductionMode)
					{
						model.IsForProductionMode = true;
					}

					var fields = new Dictionary<string, object>
					{
						[nameof(ProductionOrderItem.ProductionStatus)] = model.ProductionStatus,
						[nameof(ProductionOrderItem.LastComment)] = model.Comment ?? string.Empty
					};
					await unitOfWork.Repository<ProductionOrderItem>()
						.UpdateFieldsAsync(parentId, fields, ct);
				}
				else if (!model.IsForProductionStepStatus && Enum.IsDefined(typeof(ProductionOrderItemCheckStatusEnum), statusId))
				{
					// معادل LookupType 254 → BuyStatusId در HTS.
					// کامنت‌های «مرحله ساخت» (IsForProductionStepStatus) در HTS کد مرحله (LookupType 58) دارند و BuyStatus را
					// عوض نمی‌کنند؛ در سیستم جدید ProductionStatus این کامنت‌ها فقط برچسب وضعیت جاری است، پس نباید CheckStatus را بازنویسی کند.
					await unitOfWork.Repository<ProductionOrderItem>()
						.UpdateFieldsAsync(parentId,
							c => c.CheckStatus,
							(ProductionOrderItemCheckStatusEnum)statusId,
							ct);
				}

				if (EngineeringConsiderationStatusIds.Contains(statusId))
				{
					await unitOfWork.Repository<ProductionOrderItem>()
						.UpdateFieldsAsync(parentId,
							c => c.EngineeringConsideration,
							model.Comment ?? string.Empty,
							ct);
				}
			}

			// همیشه: ProductionStatus = آخرین کامنت با IsForProductionMode (معادل بخش پایانی تریگر)
			var lastProductionStatus = await unitOfWork.Repository<ProductionOrderItemComment>()
				.TableNoTracking
				.Where(c => c.ProductionOrderItemId == parentId
					&& c.IsForProductionMode
					&& c.IsActive == Entities.Base.IsActiveEnum.Active)
				.OrderByDescending(c => c.Id)
				.Select(c => (ProductionOrderItemProductionStatusEnum?)c.ProductionStatus)
				.FirstOrDefaultAsync(ct);

			if (lastProductionStatus.HasValue)
			{
				var lastCommentText = await unitOfWork.Repository<ProductionOrderItemComment>()
					.TableNoTracking
					.Where(c => c.ProductionOrderItemId == parentId
						&& c.IsForProductionMode
						&& c.IsActive == Entities.Base.IsActiveEnum.Active)
					.OrderByDescending(c => c.Id)
					.Select(c => c.Comment)
					.FirstOrDefaultAsync(ct);

				await unitOfWork.Repository<ProductionOrderItem>()
					.UpdateFieldsAsync(parentId, new Dictionary<string, object>
					{
						[nameof(ProductionOrderItem.ProductionStatus)] = lastProductionStatus.Value,
						[nameof(ProductionOrderItem.LastComment)] = lastCommentText ?? string.Empty
					}, ct);
			}
		}
	}
}
