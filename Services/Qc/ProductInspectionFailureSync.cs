using Common.Utilities;
using Data.Contracts;
using Entities.App.Qc;
using Entities.App.Sale.Enums;
using Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace Services.Qc
{
	/// <summary>
	/// معادل ProductInspectionService.AddFailureTypesFromProductionOrderItemsSection در HTS.
	/// خرابی کامنت‌های توقف بازرسی نهایی (۱۷۹۶) و توقف تست تولید (۲۲۲۶) را روی بازرسی همان شماره سفارش و سریال می‌نویسد.
	/// </summary>
	public static class ProductInspectionFailureSync
	{
		public static async Task<int> AddFromProductionOrderItemCommentsAsync(IUnitOfWork unitOfWork, CancellationToken ct)
		{
			var statuses = new[]
			{
				ProductionOrderItemProductionStatusEnum.FinalInspectionStageStopStarted,
				ProductionOrderItemProductionStatusEnum.ProductionTestStageStopStarted
			};

			var comments = await unitOfWork.Repository<Entities.App.Sale.ProductionOrderItemComment>().TableNoTracking
				.Where(c => statuses.Contains(c.ProductionStatus)
					&& c.FailureTypeIds != null
					&& c.FailureTypeIds != ""
					&& c.IsActive != IsActiveEnum.Deleted
					&& c.ProductionOrderItem.Serial != null
					&& c.ProductionOrderItem.ProductionOrder.Number != null)
				.Select(c => new CommentRow
				{
					Id = c.Id!.Value,
					FailureTypeIds = c.FailureTypeIds!,
					Comment = c.Comment,
					StartMiladiDateTime = c.StartMiladiDateTime,
					StartShamsiDate = c.StartShamsiDate,
					EndMiladiDateTime = c.EndMiladiDateTime,
					EndShamsiDate = c.EndShamsiDate,
					CreatedById = c.CreatedById,
					CreatedByName = c.CreatedByName,
					CreatedOnMiladiDateTime = c.CreatedOnMiladiDateTime,
					CreatedOnShamsiDateTime = c.CreatedOnShamsiDateTime,
					Serial = c.ProductionOrderItem.Serial!,
					OrderNumber = c.ProductionOrderItem.ProductionOrder.Number
				})
				.ToListAsync(ct);

			if (comments.Count == 0)
				return 0;

			var linkedIds = await unitOfWork.Repository<ProductInspectionFailure>().TableNoTracking
				.Where(f => f.ProductionOrderItemCommentId != null)
				.Select(f => f.ProductionOrderItemCommentId!.Value)
				.ToListAsync(ct);
			var linked = linkedIds.ToHashSet();

			var inspections = await unitOfWork.Repository<ProductInspection>().TableNoTracking
				.Where(p => p.InspectionType == ProductInspectionConstants.InCompany
					&& p.ProductionOrderItem.Serial != null
					&& p.ProductionOrderItem.ProductionOrder.Number != null)
				.OrderBy(p => p.Id)
				.Select(p => new
				{
					Id = p.Id!.Value,
					Serial = p.ProductionOrderItem.Serial!,
					OrderNumber = p.ProductionOrderItem.ProductionOrder.Number
				})
				.ToListAsync(ct);

			var inspectionByKey = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
			foreach (var inspection in inspections)
			{
				var key = Key(inspection.OrderNumber, inspection.Serial);
				if (!inspectionByKey.ContainsKey(key))
					inspectionByKey[key] = inspection.Id;
			}

			var validTypeIds = (await unitOfWork.Repository<FailureType>().TableNoTracking
				.Where(f => f.Id != null)
				.Select(f => f.Id!.Value)
				.ToListAsync(ct)).ToHashSet();

			var rows = new List<ProductInspectionFailure>();
			foreach (var comment in comments)
			{
				if (linked.Contains(comment.Id))
					continue;
				if (!inspectionByKey.TryGetValue(Key(comment.OrderNumber, comment.Serial), out var inspectionId))
					continue;

				SplitShamsi(comment.StartShamsiDate, out var startDate, out var startTime);
				SplitShamsi(comment.EndShamsiDate, out var endDate, out var endTime);
				if (!startDate.HasValue())
					startDate = comment.CreatedOnShamsiDateTime?.Split(' ').FirstOrDefault();
				if (!endDate.HasValue())
					endDate = startDate;

				foreach (var typeText in comment.FailureTypeIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
				{
					if (!long.TryParse(typeText, out var typeId) || !validTypeIds.Contains(typeId))
						continue;

					rows.Add(new ProductInspectionFailure
					{
						ProductInspectionId = inspectionId,
						ProductionOrderItemCommentId = comment.Id,
						FailureTypeId = typeId,
						FailureStartShamsiDate = startDate,
						FailureStartMiladiDate = comment.StartMiladiDateTime?.Date,
						FailureStartTime = startTime,
						FailureEndShamsiDate = endDate,
						FailureEndMiladiDate = comment.EndMiladiDateTime?.Date ?? comment.StartMiladiDateTime?.Date,
						FailureEndTime = endTime,
						Comment = comment.Comment,
						CreatedById = comment.CreatedById,
						CreatedByName = comment.CreatedByName,
						CreatedOnMiladiDateTime = comment.CreatedOnMiladiDateTime,
						CreatedOnShamsiDateTime = comment.CreatedOnShamsiDateTime,
						IsActive = IsActiveEnum.Active
					});
				}

				linked.Add(comment.Id);
			}

			if (rows.Count == 0)
				return 0;

			var repository = unitOfWork.Repository<ProductInspectionFailure>();
			foreach (var row in rows)
				await repository.AddAsync(row, ct, saveNow: false);

			await unitOfWork.SaveChangesAsync(ct);
			return rows.Count;
		}

		private static string Key(string? orderNumber, string? serial)
			=> (orderNumber ?? "").Trim() + "|" + (serial ?? "").Trim();

		private static void SplitShamsi(string? value, out string? date, out string? time)
		{
			date = null;
			time = null;
			if (!value.HasValue())
				return;

			var parts = value!.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
			date = parts[0];
			if (parts.Length > 1 && parts[1].Length >= 5)
				time = parts[1][..5];
		}

		private sealed class CommentRow
		{
			public long Id { get; set; }
			public string FailureTypeIds { get; set; } = "";
			public string? Comment { get; set; }
			public DateTime? StartMiladiDateTime { get; set; }
			public string? StartShamsiDate { get; set; }
			public DateTime? EndMiladiDateTime { get; set; }
			public string? EndShamsiDate { get; set; }
			public long? CreatedById { get; set; }
			public string? CreatedByName { get; set; }
			public DateTime? CreatedOnMiladiDateTime { get; set; }
			public string? CreatedOnShamsiDateTime { get; set; }
			public string Serial { get; set; } = "";
			public string OrderNumber { get; set; } = "";
		}
	}
}
