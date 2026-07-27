using Common.Attributes;
using Entities.App.Pln;
using Entities.App.SLS;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Display(Name = "جزئیات سریال سفارش فروش")]
[Table("OrderDetailSerial", Schema = "Sale")]
public class OrderDetailSerial : BaseEntity
{

    [DisplayName("سفارش مرتبط")]
    [DisplayInfo(null, true, type: SystemType.Entity)]
    public virtual OrderDetail OrderDetail { get; set; }
    public long? OrderDetailId { get; set; }


    [DisplayName("سفارش مرتبط")]
    [DisplayInfo(null, true, type: SystemType.Entity)]
    public virtual PlaningProject PlaningProject { get; set; }
    public long? PlaningProjectId { get; set; }


    [DisplayName("مشتری")]
    [DisplayInfo(null, true, type: SystemType.Entity)]
    public virtual Customer Customer { get; set; }
    public long? CustomerId { get; set; }


    [DisplayName("آدرس مشتری")]
    [DisplayInfo(null, true, type: SystemType.Entity)]
    public virtual Customer CustomerAddress { get; set; }
    public long? CustomerAddressId { get; set; }


    [DisplayName("سریال")]
    [DisplayInfo(null, true, type: SystemType.String)]
    public string? Serial { get; set; }


    [DisplayName("مدل")]
    [DisplayInfo(null, true, type: SystemType.String)]
    public string? Model { get; set; }


    [DisplayName("ساعت روزانه")]
    [DisplayInfo(null, true, type: SystemType.String)]
    public string? HoursDaily { get; set; }


    [DisplayName("زمان اجرا")]
    [DisplayInfo(null, true, type: SystemType.Long)]
    public long? Runtime { get; set; }


    [DisplayName("زمان گارانتی")]
    [DisplayInfo(null, true, type: SystemType.Int)]
    public int? GuaranteeTime { get; set; }


    [DisplayName("روز ارسال گارانتی")]
    [DisplayInfo(null, true, type: SystemType.Int)]
    public int? Guarantee_SendDay { get; set; }


    [DisplayName("روز راه اندازی گارنتی")]
    [DisplayInfo(null, true, type: SystemType.Int)]
    public int? Guarantee_LaunchDay { get; set; }


    [DisplayName("دارای بیمه")]
    [DisplayInfo(null, true, type: SystemType.Boolean)]
    public bool? HasInsurance { get; set; }


    [DisplayName("تاریخ ارسال میلادی")]
    [DisplayInfo(null, true, type: SystemType.Date)]
    public DateTime? SendMiladiDate { get; set; } = null;


    [DisplayName("تاریخ ارسال شمسی")]
    [DisplayInfo(null, true, type: SystemType.DateShamsi)]
    public string? SendShamsiDate { get; set; } = null;


    [DisplayName("ساعت ارسال")]
    [DisplayInfo(null, true, type: SystemType.String)]
    public string? SendTime { get; set; }


    [DisplayName("تاریخ راه اندازی میلادی")]
    [DisplayInfo(null, true, type: SystemType.Date)]
    public DateTime? LaunchMiladiDate { get; set; } = null;

    [DisplayName("تاریخ راه اندازی شمسی")]
    [DisplayInfo(null, true, type: SystemType.DateShamsi)]
    public string? LaunchShamsiDate { get; set; } = null;


    [DisplayName("تاریخ  خروج از کارخانه میلادی")]
    [DisplayInfo(null, true, type: SystemType.Date)]
    public DateTime? ExitFactoryMiladiDate { get; set; } = null;


    [DisplayName("تاریخ  خروج از کارخانه شمسی")]
    [DisplayInfo(null, true, type: SystemType.String)]
    public string? ExitFactoryShamsiDate { get; set; } = null;


    [DisplayName("ساعت خروج از کارخانه")]
    [DisplayInfo(null, true, type: SystemType.String)]
    public string? ExitFactoryTime { get; set; } = null;


    [DisplayName("شماره سفارش تولید")]
    [DisplayInfo(null, true, type: SystemType.String)]
    public string? ProductionOrderNumber { get; set; } = null;



}
