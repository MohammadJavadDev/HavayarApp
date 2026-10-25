using Data.Contracts;
using Entities.App.Sale;
using Entities.App.Sale.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Services.ProductionOrderServices
{
	/// <summary>داده‌های یک ردیف BOM جدید.</summary>
	public sealed class NewBomLine
	{
		public long ProductionOrderItemId { get; set; }
		public long PartId { get; set; }
		public decimal Amount { get; set; }
		public string? Description { get; set; }
		public string? SaleUnitDetails { get; set; }
		public bool NeedsAVL { get; set; }
		public bool NeedsInspection { get; set; }
		public ProductionOrderItemBomProductionStepEnum ProductionStep { get; set; }
		public int? NumberSupplied { get; set; }
		public ProductionOrderItemBomEnum Status { get; set; }
		public int Revision { get; set; }

		/// <summary>برای جاب‌ها (بدون کاربر جاری): ایجادکنندهٔ سیستمی. در WebApp خالی می‌ماند و از کاربر جاری پر می‌شود.</summary>
		public long? CreatedById { get; set; }
		public string? CreatedByName { get; set; }
	}

	/// <summary>
	/// تنها نقطهٔ نوشتن روی BOM اقلام سفارش ساخت (WebApp و App.BackgroundJob).
	/// جدول اصلی فقط آخرین نسخهٔ هر (قلم، کالا) را نگه می‌دارد و هر ایجاد، ویرایش و ثبت ملاحظات یک ردیف در
	/// <see cref="ProductionOrderItemBomHistory"/> می‌سازد. حذف: پرچم IsDeleted روی همهٔ ردیف‌های تاریخچهٔ آن
	/// (قلم، کالا) و حذف کامل ردیف اصلی. همهٔ عملیات در یک تراکنش‌اند.
	/// دلیل وجود این لایه: درج دسته‌ای (BulkInsert) اکشن‌های Entity را اجرا نمی‌کند و جاب‌ها اکشن‌های WebApp را ندارند.
	/// </summary>
	public interface IProductionOrderItemBomService
	{
		/// <summary>ایجاد یک ردیف (نسخهٔ ۰) + رخداد «ایجاد» در تاریخچه.</summary>
		Task<ProductionOrderItemBom> CreateLineAsync(NewBomLine line, CancellationToken cancellationToken);

		/// <summary>ایجاد دسته‌ای ردیف‌ها + رخدادهای «ایجاد». فراخواننده تکراری نبودن (قلم، کالا) را تضمین می‌کند.</summary>
		Task<List<ProductionOrderItemBom>> CreateLinesAsync(IReadOnlyList<NewBomLine> lines, CancellationToken cancellationToken);

		/// <summary>
		/// ویرایش ردیف موجود: <paramref name="apply"/> روی ردیف بارگذاری‌شده از دیتابیس اجرا می‌شود.
		/// اگر مقدار یا توضیحات عوض شود Revision یکی بالا می‌رود؛ همیشه یک رخداد «ویرایش» در تاریخچه ثبت می‌شود.
		/// </summary>
		Task<ProductionOrderItemBom> UpdateLineAsync(long lineId, Action<ProductionOrderItemBom> apply, CancellationToken cancellationToken);

		/// <summary>ثبت ملاحظات واحد پروژه/فروش روی چند ردیف؛ Revision تغییر نمی‌کند، رخداد «ملاحظات» ثبت می‌شود.</summary>
		Task<List<ProductionOrderItemBom>> ApplyConsiderationAsync(
			IReadOnlyList<long> lineIds, bool needsAvl, bool needsInspection, string? saleUnitDetails, CancellationToken cancellationToken);

		/// <summary>حذف: IsDeleted روی همهٔ ردیف‌های تاریخچهٔ این (قلم، کالا) و حذف کامل ردیف اصلی.</summary>
		Task DeleteLineAsync(long lineId, CancellationToken cancellationToken);
	}

	public class ProductionOrderItemBomService(IUnitOfWork unitOfWork) : IProductionOrderItemBomService
	{
		public async Task<ProductionOrderItemBom> CreateLineAsync(NewBomLine line, CancellationToken cancellationToken)
		{
			return await unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var entity = ToEntity(line);
				await unitOfWork.Repository<ProductionOrderItemBom>().AddAsync(entity, cancellationToken, saveAudit: true);
				await unitOfWork.Repository<ProductionOrderItemBomHistory>()
					.AddAsync(ToHistory(entity, ProductionOrderItemBomHistoryEventEnum.Create), cancellationToken, saveAudit: false);
				return entity;
			}, cancellationToken);
		}

		public async Task<List<ProductionOrderItemBom>> CreateLinesAsync(IReadOnlyList<NewBomLine> lines, CancellationToken cancellationToken)
		{
			if (lines.Count == 0)
				return new List<ProductionOrderItemBom>();

			return await unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var entities = lines.Select(ToEntity).ToList();
				await unitOfWork.Repository<ProductionOrderItemBom>().AddRangeAsync(entities, cancellationToken);

				var history = entities
					.Select(e => ToHistory(e, ProductionOrderItemBomHistoryEventEnum.Create))
					.ToList();
				await unitOfWork.Repository<ProductionOrderItemBomHistory>().AddRangeAsync(history, cancellationToken);
				return entities;
			}, cancellationToken);
		}

		public async Task<ProductionOrderItemBom> UpdateLineAsync(long lineId, Action<ProductionOrderItemBom> apply, CancellationToken cancellationToken)
		{
			return await unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var repository = unitOfWork.Repository<ProductionOrderItemBom>();
				var line = await repository.Table.FirstOrDefaultAsync(c => c.Id == lineId, cancellationToken)
					?? throw new InvalidOperationException("رکورد BOM یافت نشد");

				var oldAmount = line.Amount;
				var oldDescription = line.Description ?? string.Empty;

				apply(line);

				if (line.Amount != oldAmount || (line.Description ?? string.Empty) != oldDescription)
					line.Revision = (line.Revision ?? 0) + 1;
				line.Revision ??= 0;
				line.IsLatest = true;

				await repository.UpdateAsync(line, cancellationToken, saveAudit: true);
				await unitOfWork.Repository<ProductionOrderItemBomHistory>()
					.AddAsync(ToHistory(line, ProductionOrderItemBomHistoryEventEnum.Update), cancellationToken, saveAudit: false);
				return line;
			}, cancellationToken);
		}

		public async Task<List<ProductionOrderItemBom>> ApplyConsiderationAsync(
			IReadOnlyList<long> lineIds, bool needsAvl, bool needsInspection, string? saleUnitDetails, CancellationToken cancellationToken)
		{
			return await unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var repository = unitOfWork.Repository<ProductionOrderItemBom>();
				var historyRepository = unitOfWork.Repository<ProductionOrderItemBomHistory>();
				var lines = await repository.Table.Where(c => lineIds.Contains(c.Id!.Value)).ToListAsync(cancellationToken);

				foreach (var line in lines)
				{
					line.NeedsAVL = needsAvl;
					line.NeedsInspection = needsInspection;
					line.SaleUnitDetails = saleUnitDetails;
					await repository.UpdateAsync(line, cancellationToken, saveAudit: true);
					await historyRepository.AddAsync(
						ToHistory(line, ProductionOrderItemBomHistoryEventEnum.Consideration), cancellationToken, saveAudit: false);
				}

				return lines;
			}, cancellationToken);
		}

		public async Task DeleteLineAsync(long lineId, CancellationToken cancellationToken)
		{
			await unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var repository = unitOfWork.Repository<ProductionOrderItemBom>();
				var historyRepository = unitOfWork.Repository<ProductionOrderItemBomHistory>();

				var line = await repository.Table.FirstOrDefaultAsync(c => c.Id == lineId, cancellationToken);
				if (line == null)
					return 0;

				var historyRows = await historyRepository.Table
					.Where(h => h.ProductionOrderItemId == line.ProductionOrderItemId && h.PartId == line.PartId && !h.IsDeleted)
					.ToListAsync(cancellationToken);

				if (historyRows.Count > 0)
				{
					foreach (var row in historyRows)
						row.IsDeleted = true;
					await historyRepository.UpdateRangeAsync(historyRows, cancellationToken);
				}

				await repository.DeleteAsync(line, cancellationToken, saveNow: true);
				return 0;
			}, cancellationToken);
		}

		/// <summary>نقض ایندکس یکتای (قلم، کالا) روی جدول اصلی.</summary>
		public static bool IsDuplicateKeyException(Exception exception)
		{
			for (var current = exception; current != null; current = current.InnerException)
			{
				if (current is SqlException sql && (sql.Number == 2601 || sql.Number == 2627))
					return true;
			}
			return false;
		}

		private static ProductionOrderItemBom ToEntity(NewBomLine line) => new()
		{
			ProductionOrderItemId = line.ProductionOrderItemId,
			PartId = line.PartId,
			Amount = line.Amount,
			Description = line.Description,
			SaleUnitDetails = line.SaleUnitDetails,
			NeedsAVL = line.NeedsAVL,
			NeedsInspection = line.NeedsInspection,
			ProductionStep = line.ProductionStep,
			NumberSupplied = line.NumberSupplied,
			Status = line.Status,
			Revision = line.Revision,
			IsLatest = true,
			CreatedById = line.CreatedById,
			CreatedByName = line.CreatedByName
		};

		private static ProductionOrderItemBomHistory ToHistory(ProductionOrderItemBom line, ProductionOrderItemBomHistoryEventEnum eventType) => new()
		{
			// رخداد «ایجاد»: ایجادکنندهٔ ردیف؛ ویرایش و ملاحظات: کاربر جاری (توسط Repository پر می‌شود)
			CreatedById = eventType == ProductionOrderItemBomHistoryEventEnum.Create ? line.CreatedById : null,
			CreatedByName = eventType == ProductionOrderItemBomHistoryEventEnum.Create ? line.CreatedByName : null,
			ProductionOrderItemId = line.ProductionOrderItemId,
			PartId = line.PartId,
			Revision = line.Revision ?? 0,
			EventType = eventType,
			Amount = line.Amount,
			Description = line.Description,
			SaleUnitDetails = line.SaleUnitDetails,
			NeedsAVL = line.NeedsAVL,
			NeedsInspection = line.NeedsInspection,
			ProductionStep = line.ProductionStep,
			NumberSupplied = line.NumberSupplied,
			Status = line.Status,
			IsDeleted = false
		};
	}
}
