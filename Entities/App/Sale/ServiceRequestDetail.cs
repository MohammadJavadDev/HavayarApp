using Common.Attributes;
using Entities.App.Inv;
using Entities.App.Sale;
using Entities.App.Sale.Enums;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Display(Name = "جزئیات درخواست سرویس")]
[Table("ServiceRequestDetail", Schema = "Sale")]
public class ServiceRequestDetail : BaseEntity
{

    [DisplayName("درخواست سرویس")]
    [DisplayInfo(null, true, type: SystemType.Entity)]
    public ServiceRequest? ServiceRequest { get; set; }
    public long? ServiceRequestId { get; set; }


    [DisplayName("نوع درخواست")]
    [DisplayInfo(null, true, type: SystemType.Select)]
    public SaleRequestTypeEnum RequestType { get; set; }



    [DisplayName("درخواست سرویس")]
    [DisplayInfo(null, true, type: SystemType.Entity)]
    public OrderDetail? OrderDetail { get; set; }
    public long? OrderDetailId { get; set; }


    [DisplayName("درخواست سرویس")]
    [DisplayInfo(null, true, type: SystemType.Entity)]
    public OrderDetailSerial? OrderDetailSerial { get; set; }
    public long? OrderDetailSerialId { get; set; }


    [DisplayName("تاریخ ابلاغ میلادی")]
    [DisplayInfo(null, true, type: SystemType.Date)]
    public DateTime? NoticeMiladiDate { get; set; } = null;

    [DisplayName("تاریخ ابلاغ شمسی")]
    [DisplayInfo(null, true, type: SystemType.DateShamsi)]
    public string? NoticeShamsiDate { get; set; } = null;


    [DisplayName("ساعت ابلاغ")]
    [DisplayInfo(null, true, type: SystemType.String)]
    public string? NoticeTime { get; set; }


    [DisplayName("محصول مرتبط")]
    [DisplayInfo(null, true, type: SystemType.Entity)]
    public virtual Part Part { get; set; }  
    public long? PartId { get; set; }


    [DisplayName("نوع محصول")]
    [DisplayInfo(null, true, type: SystemType.Select)]
    public ProductTypeEnum ProductType { get; set; }




}
