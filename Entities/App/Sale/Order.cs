using Common.Attributes;
using Entities.App.Gnr;
using Entities.App.Sale;
using Entities.App.Sale.Enums;
using Entities.App.SLS;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Entities.App.Sale;
[Display(Name = "سفارش")]
[Table("Order", Schema = "Sale")]
public class Order : BaseEntity
{

	[DisplayName("سال")]
	[DisplayInfo(null, true, type: SystemType.String)]
	public string? Year { get; set; }


	[DisplayName("تاریخ سندمیلادی")]
	[DisplayInfo(null, true, type: SystemType.Date)]
	public DateTime? VchDateMiladiDate { get; set; } = null;

	[DisplayName("تاریخ سند شمسی")]
	[DisplayInfo(null, true, type: SystemType.DateShamsi)]
	public string? VchDateShamsiDate { get; set; } = null;



	[DisplayName("مشتری")]
	[DisplayInfo(null, true, type: SystemType.Entity)]
	public virtual Customer Customer { get; set; }
	public long? CustomerId { get; set; }



	[DisplayName("نوع سند")]
	[DisplayInfo(null, true, type: SystemType.Select)]
	public SaleOrderTypeEnum? VoucherTypeStatus { get; set; } = null;


	[DisplayName("وضعیت")]
	[DisplayInfo(null, true, type: SystemType.Select)]
	public SaleOrderStatusEnum? StatusEnum { get; set; } = null;



	[DisplayName(" شعبه فروش")]
	[DisplayInfo(null, true, type: SystemType.Entity)]
	public virtual Branch Branch { get; set; }
	public long? BranchId { get; set; }



	[DisplayName("شرکت")]
	[DisplayInfo(null, true, type: SystemType.Entity)]
	public virtual Party Party { get; set; }
	public long? PartyId { get; set; }



	[DisplayName("شماره قرارداد")]
	[DisplayInfo(null, true, type: SystemType.String)]
	public string? ContractNumber { get; set; } = null;


	[DisplayName("تاریخ فاکتور میلادی")]
	[DisplayInfo(null, true, type: SystemType.Date)]
	public DateTime? FactorMiladiDate { get; set; } = null;


	[DisplayName("تاریخ فاکتور  شمسی")]
	[DisplayInfo(null, true, type: SystemType.DateShamsi)]
	public string? FactorShamsiDate { get; set; } = null;


	[DisplayName("شناسه راهکاران")]
	[DisplayInfo(null, true, type: SystemType.Long)]
	public long? HamkaranId { get; set; }


	[DisplayName("شماره سفارش")]
	[DisplayInfo(null, true, type: SystemType.Long)]
	public long? OrderNumber { get; set; }


	[DisplayName("قرارداد")]
	[DisplayInfo(null, true, type: SystemType.Entity)]
	[ForeignKey(nameof(ContractId))]
	public virtual Contract? Contract { get; set; }
	public long? ContractId { get; set; }


	[DisplayName("سال مالی راهکاران")]
	[DisplayInfo(null, true, type: SystemType.Long)]
	public long? FiscalYearRef { get; set; }

	[DisplayName("شناسه HTS")]
	[DisplayInfo(null, false, type: SystemType.Long)]
	public long HtsId { get; set; }

	[DisplayName("تاییدکننده")]
	[DisplayInfo(null, true, type: SystemType.Entity)]
	[ForeignKey(nameof(ConfirmerUserId))]
	public virtual Entities.Auth.User? Confirmer { get; set; }
	public long? ConfirmerUserId { get; set; }

	[DisplayName("اپراتور")]
	[DisplayInfo(null, true, type: SystemType.Entity)]
	[ForeignKey(nameof(OperatorUserId))]
	public virtual Entities.Auth.User? Operator { get; set; }
	public long? OperatorUserId { get; set; }

	[DisplayName("انبار HTS")]
	[DisplayInfo(null, false, type: SystemType.Long)]
	public long? HtsStockId { get; set; }

	[DisplayName("نوع سند پایه HTS")]
	[DisplayInfo(null, false, type: SystemType.Long)]
	public long? HtsBaseVoucherTypeId { get; set; }

	[DisplayName("شماره سند HTS")]
	[DisplayInfo(null, false, type: SystemType.Long)]
	public long? HtsVchNo { get; set; }

	[DisplayName("هدر قرارداد HTS")]
	[DisplayInfo(null, false, type: SystemType.Long)]
	public long? HtsContractVchHeaderId { get; set; }

}
