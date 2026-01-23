using Common.Attributes;
using Entities.App.FIN;
using Entities.App.SLS.Enums;
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


		[DisplayName("نحوه فروش")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ContractSalesTypeEnum? SalesType { get; set; }


		[DisplayName("قیمت خالص")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? NetPrice { get; set; }


		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? Description { get; set; }


		[DisplayName("شناسه راهکاران")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? HamkaranId { get; set; }


	}
}
