using Common.Attributes;
using Entities.App.Inv;
using Entities.App.SLS;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Display(Name = "جزئیات سفارش فروش")]
[Table("OrderDetail", Schema = "Sale")]
public class OrderDetail : BaseEntity
{

	[DisplayName("سفارش")]
	[DisplayInfo(null, true, type: SystemType.Entity)]
	public virtual Order Sale_Order { get; set; }
	public long? SaleOrderId { get; set; }


	[DisplayName("شماره پیش فاکتور")]
	[DisplayInfo(null, true, type: SystemType.Long)]
	public long? Prefactor_VchNo { get; set; }

	[DisplayName("ترتیب")]
	[DisplayInfo(null, true, type: SystemType.Int)]
	public int Seq { get; set; }


	[DisplayName("کالا")]
	[DisplayInfo(null, true, type: SystemType.Entity)]
	public virtual Part Part { get; set; }
	public long? PartId { get; set; }


	[DisplayName("واحد اندازه گیری")]
	[DisplayInfo(null, true, type: SystemType.Entity)]
	public PartUnit? Unit { get; set; }
	public long? UnitId { get; set; }


	[DisplayName("تعداد ")]
	[DisplayInfo(null, true, type: SystemType.Decimal)]
	public decimal Qty { get; set; }


	[DisplayName("کد سند پایه")]
	[DisplayInfo(null, true, type: SystemType.Long)]
	public long? BaseVchItmRef { get; set; }

	[DisplayName("توضیحات")]
	[DisplayInfo(null, true, type: SystemType.Long)]
	public string? Description { get; set; }


	[DisplayName("قیمت واحد")]
	[DisplayInfo(null, true, type: SystemType.Long)]
	public long? UnitPrice { get; set; }


	[DisplayName("قیمت")]
	[DisplayInfo(null, true, type: SystemType.Long)]
	public long? Price { get; set; }


	[DisplayName("قیمت فزایشی")]
	[DisplayInfo(null, true, type: SystemType.Long)]
	public long? IncPrice { get; set; }


	[DisplayName("قیمت کاهشی")]
	[DisplayInfo(null, true, type: SystemType.Select)]

	public PayMethodEnum PayMethod { get; set; }


	[DisplayName("قیمت موقت")]
	[DisplayInfo(null, true, type: SystemType.Long)]
	public long? PriceTemp { get; set; }


	[DisplayName("تعداد موقت")]
	[DisplayInfo(null, true, type: SystemType.Decimal)]
	public decimal QtyTemp { get; set; }



	[DisplayName("تاریخ  فاکتور میلادی")]
	[DisplayInfo(null, true, type: SystemType.Date)]
	public DateTime? FactorMiladiDate { get; set; } = null;


	[DisplayName("تاریخ فاکتور شمسی")]
	[DisplayInfo(null, true, type: SystemType.DateShamsi)]
	public string? FactorShamsiDate { get; set; } = null;


	[DisplayName("مشتری")]
	[DisplayInfo(null, true, type: SystemType.Entity)]
	public virtual Customer Customer { get; set; }
	public long? CustomerId { get; set; }

}
