using Common.Attributes;
using Entities.App.Gnr;
using Entities.App.Pln.Enums;
using Entities.App.Sale;
using Entities.App.SLS;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
    public virtual Customer CustomerAddress { get; set; }
    public long? CustomerAddressId { get; set; }



    [DisplayName("نوع سند")]
    [DisplayInfo(null, true, type: SystemType.DateShamsi)]
    public VoucherTypeStatusEnum? VoucherTypeStatus { get; set; } = null;


    [DisplayName("وضعیت")]
    [DisplayInfo(null, true, type: SystemType.DateShamsi)]
    public StatusEnum? StatusEnum { get; set; } = null;



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

}