using Common.Attributes;
using Entities.App.FIN;
using Entities.App.Sale;
using Entities.App.SLS.Enums;
using Entities.Auth;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.SLS
{
	[Display(Name = "قرارداد")]
	[Table("Contract", Schema = "SLS")]
	public class Contract : BaseEntity
	{
		[DisplayName("شماره")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? Number { get; set; }


		[DisplayName("تاریخ")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? DateMiladi { get; set; }


		[DisplayName("تاریخ شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? DateShamsi { get; set; }


		[DisplayName("مشتری")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Customer? Customer { get; set; }

		public long? CustomerId { get; set; }


		[DisplayName("تفصیل")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public DL? Detail { get; set; }

		public long? DetailId { get; set; }


		[DisplayName("سال مالی راهکاران")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? FiscalYearRef { get; set; }


		[DisplayName("ماه")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? Month { get; set; }


		[DisplayName("تاییدکننده")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public User? Confirmer { get; set; }

		public long? ConfirmerId { get; set; }


		[DisplayName("وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ContractStatusEnum? Status { get; set; }


		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? Title { get; set; }


		[DisplayName("شماره قرارداد")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? ContractNumber { get; set; }


		[DisplayName("محل تحویل")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? DeliveryLocationId { get; set; }


		[DisplayName("درصد پیش‌پرداخت")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public double? PercentPrePaid { get; set; }


		[DisplayName("نحوه فروش")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ContractSalesTypeEnum? SalesType { get; set; }


		[DisplayName("پیش‌نویس قرارداد")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? ContractDraftId { get; set; }


		[DisplayName("قیمت خالص")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? NetPrice { get; set; }


		[DisplayName("دپارتمان فروش")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Branch? Branch { get; set; }

		public long? BranchId { get; set; }


		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? Description { get; set; }


		[DisplayName("شناسه راهکاران")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? HamkaranId { get; set; }


		[DisplayName("نسخه راهکاران")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? RahkaranVersion { get; set; }
	}
}
