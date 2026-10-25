using Common.Attributes;
using Entities.App.Pln.Enums;
using Entities.App.Sale;
using Entities.Base;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Pln
{
	[Display(Name = "تاخیرات سفارش ساخت")]
	[Table("ProductionOrderDelay", Schema = "Pln")]
	public class ProductionOrderDelay : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }
		[DisplayName("سفارش ساخت")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public ProductionOrder? ProductionOrder { get; set; }

		public long? ProductionOrderId { get; set; }


		[DisplayName("قلم سفارش ساخت")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public ProductionOrderItem? ProductionOrderItem { get; set; }

		public long? ProductionOrderItemId { get; set; }

		[Display(Name = "عامل تاخیرات")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public DelayResponsibleEnum DelayResponsible { get; set; }


		[DisplayName("تعداد روز تاخیر")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? DelayDays { get; set; }


		[DisplayName("علت تاخیر")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? DelayReason { get; set; }


		[DisplayName("تاریخ میلادی شروع تاخیر")]
		[DisplayInfo(null, false, SystemType.Date, systemProprty: true)]
		public DateTime? DelayStartMiladiDate { get; set; } = null;

		[DisplayName("تاریخ شمسی شروع تاخیر")]
		[MaxLength(30)]
		[DisplayInfo("DelayStartMiladiDate", true, SystemType.DateShamsi, systemProprty: true)]
		public string? DelayStartShamsiDate { get; set; } = null;


		[DisplayName("تاریخ میلادی پایان تاخیر")]
		[DisplayInfo(null, false, SystemType.Date, systemProprty: true)]
		public DateTime? DelayEndMiladiDate { get; set; } = null;

		[DisplayName("تاریخ شمسی پایان تاخیر")]
		[MaxLength(30)]
		[DisplayInfo("DelayEndMiladiDate", true, SystemType.DateShamsi, systemProprty: true)]
		public string? DelayEndShamsiDate { get; set; } = null;


		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? Description { get; set; }

		[DisplayName("توضیحات توجیه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? JustificationComment { get; set; }

		[DisplayName("تاریخ میلادی توجیه")]
		[DisplayInfo(null, false, SystemType.DateTime, systemProprty: true)]
		public DateTime? JustificationMiladiDateTime { get; set; }

		[DisplayName("تاریخ شمسی توجیه")]
		[MaxLength(30)]
		[DisplayInfo("JustificationMiladiDateTime", true, SystemType.DateTimeShamsi, systemProprty: true)]
		public string? JustificationShamsiDateTime { get; set; }

		[DisplayName("متولی توجیه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(200)]
		public string? TrusteeName { get; set; }

	}
}
