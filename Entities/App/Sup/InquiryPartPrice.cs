using Common.Attributes;
using Entities.App.Acc;
using Entities.App.Inv;
using Entities.App.Sup.Enums;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Sup
{
	[Display(Name = "استعلام قیمت کالا")]
	[Table("InquiryPartPrice", Schema = "Sup")]
	public class InquiryPartPrice : BaseEntity
	{
		[DisplayName("تاریخ اعتبار میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? ValidityMiladiDate { get; set; }


		[DisplayName("نوع")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public InquiryPartPriceTypeEnum? Type { get; set; }


		[DisplayName("عنوان شرکت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1000)]
		public string? CompanyName { get; set; }


		[DisplayName("کالا")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public Part Part { get; set; }

		public long PartId { get; set; }


		[DisplayName("تعداد خرید")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? BuyCount { get; set; }


		[DisplayName("نرخ به ریال")]
		[DisplayInfo(null, true, type: SystemType.Decimal, required: true)]
		public decimal PriceUnitValue { get; set; }


		[DisplayName("هزینه حمل")]
		[DisplayInfo(null, true, type: SystemType.Decimal, required: true)]
		public decimal TransportationCost { get; set; }


		[DisplayName("قیمت واحد")]
		[DisplayInfo(null, true, type: SystemType.Decimal, required: true)]
		public decimal UnitPrice { get; set; }


		[DisplayName("قیمت ثابت")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsFixedPrice { get; set; } = false;


		[DisplayName("کامنت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2800)]
		public string? Comment { get; set; }


		[DisplayName("تاریخ اعتبار")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? ValidityDate { get; set; }


		[DisplayName("تاریخ اعتبار شمسی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(20)]
		public string? ValidShamsiDate { get; set; }


		[DisplayName("مبلغ استاندار ارزی")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? CurrencyStandardPrice { get; set; }


		[DisplayName("مبلغ واقعی ارزی")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? CurrencyActualPrice { get; set; }


		[DisplayName("مبلغ استاندارد حمل و نقل")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? ShippingStandardPrice { get; set; }


		[DisplayName("مبلغ واقعی حمل و نقل")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? ShippingActualPrice { get; set; }


		[DisplayName("مبلغ تمام شده استاندارد")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? TotalStandardPrice { get; set; }


		[DisplayName("مبلغ تمام شده واقعی")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? TotalActualPrice { get; set; }


		[DisplayName("موارد خاص")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsSpecialCases { get; set; } = false;


		[DisplayName("توضیحات موارد خاص")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? SpecialCasesDescription { get; set; }


		[DisplayName("وضعیت قیمت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public InquiryPartPricePriceStatusEnum? PriceStatus { get; set; }


		[DisplayName("قیمت خرید بازار")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? NewFeature { get; set; }


		[DisplayName("واحد ارز")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public PriceUnit? PriceUnit { get; set; }

		public long? PriceUnitId { get; set; }

		/// <summary>
		/// شناسه راهکاران (Sup_InquiryPartPriceID) برای نگاشت با سیستم راهکاران
		/// </summary>
		public long? RahkaranId { get; set; }

		/// <summary>
		/// شناسه فاکتور خرید در راهکاران (InvoiceID) - برای رکوردهای با نوع فاکتور
		/// </summary>
		public long? InvoiceId { get; set; }

		/// <summary>
		/// شناسه قلم فاکتور در راهکاران (InvoiceItemID) - برای رکوردهای با نوع فاکتور
		/// </summary>
		public long? InvoiceItemId { get; set; }

		/// <summary>
		/// شماره فاکتور - برای رکوردهای با نوع فاکتور
		/// </summary>
		[MaxLength(100)]
		public string? InvoiceNumber { get; set; }
	}
}
