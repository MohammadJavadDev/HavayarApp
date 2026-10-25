using Common.Attributes;
using Entities.App.Inv;
using Entities.App.Sale.Enums;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Sale
{
	/// <summary>
	/// تاریخچهٔ کامل Bom اقلام سفارش ساخت: هر ایجاد، ویرایش و ثبت ملاحظات یک ردیف می‌سازد.
	/// جدول اصلی (<see cref="ProductionOrderItemBom"/>) فقط آخرین نسخهٔ هر کالا روی هر قلم را نگه می‌دارد.
	/// با حذف یک ردیف Bom، همهٔ ردیف‌های تاریخچهٔ همان (قلم، کالا) پرچم <see cref="IsDeleted"/> می‌گیرند.
	/// کلید منطقی: (ProductionOrderItemId, PartId)؛ وابسته به شناسهٔ ردیف اصلی نیست چون ردیف اصلی با حذف پاک می‌شود.
	/// </summary>
	[Display(Name = "تاریخچه Bom اقلام سفارش ساخت")]
	[Table("ProductionOrderItemBomHistory", Schema = "Sale")]
	[Index(nameof(ProductionOrderItemId), nameof(PartId), nameof(IsDeleted))]
	public class ProductionOrderItemBomHistory : BaseEntity
	{
		public long ProductionOrderItemId { get; set; }
		public ProductionOrderItem ProductionOrderItem { get; set; }

		[DisplayName("کالا")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public Part Part { get; set; }

		public long PartId { get; set; }

		[DisplayName("نسخه")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int Revision { get; set; }

		[DisplayName("نوع رخداد")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ProductionOrderItemBomHistoryEventEnum EventType { get; set; }

		[DisplayName("تعداد/مقدار")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal Amount { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? Description { get; set; }

		[DisplayName("توضیحات واحد فروش")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? SaleUnitDetails { get; set; }

		[DisplayName("نیاز به AVL دارد")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool NeedsAVL { get; set; } = false;

		[DisplayName("نیاز به بازرسی/مدارک بازرسی دارد")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool NeedsInspection { get; set; } = false;

		[DisplayName("مرحله ساخت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ProductionOrderItemBomProductionStepEnum ProductionStep { get; set; }

		[DisplayName("تعداد تامین شده")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? NumberSupplied { get; set; }

		[DisplayName("وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ProductionOrderItemBomEnum Status { get; set; }

		[DisplayName("حذف شده")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsDeleted { get; set; } = false;
	}
}
