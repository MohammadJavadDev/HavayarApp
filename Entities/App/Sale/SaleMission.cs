using Common.Attributes;
using Entities.App.Gnr;
using Entities.App.Hcm;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Sale;

[Display(Name = "مدیریت ماموریت ها")]
[Table("SaleMission", Schema = "Sale")]
public class SaleMission : BaseEntity
{

	[DisplayName("درخواست خدمات")]
	[DisplayInfo(null, true, type: SystemType.Entity)]
	public ServiceRequest ServiceRequest { get; set; }
	public long? ServiceRequestId { get; set; }


	[DisplayName("پرسنل")]
	[DisplayInfo(null, true, type: SystemType.Entity)]
	public Personel? Personel { get; set; }
	public long? PersonelId { get; set; }

	[DisplayName("مرکز هزینه")]
	[DisplayInfo(null, true, type: SystemType.Entity)]
	public CostCenter? CostCenter { get; set; }
	public long? CostCenterId { get; set; }

	[DisplayName("گارانتی دارد؟")]
	[DisplayInfo(null, true, type: SystemType.Boolean)]
	public bool HaveGurantee { get; set; }

	[DisplayName("شماره حکم")]
	[DisplayInfo(null, true, type: SystemType.Int)]
	public int StatementNumber { get; set; }


	[DisplayName("تاریخ اعزام میلادی")]
	[DisplayInfo(null, true, type: SystemType.DateTime)]
	public DateTime? DispatchMiladiDate { get; set; } = null;


	[DisplayName("تاریخ اعزام شمسی")]
	[DisplayInfo(null, true, type: SystemType.DateTimeShamsi)]
	public string? DispatchShamsiDate { get; set; }


	[DisplayName("از ساعت")]
	[DisplayInfo(null, true, type: SystemType.String)]
	public string? FromHour { get; set; }


	[DisplayName("تاریخ برگشت میلادی")]
	[DisplayInfo(null, true, type: SystemType.DateTime)]
	public DateTime? ReturnMiladiDate { get; set; } = null;


	[DisplayName("تاریخ برگشت شمسی")]
	[DisplayInfo(null, true, type: SystemType.DateTimeShamsi)]
	public string? ReturnShamsiDate { get; set; }


	[DisplayName("تا ساعت")]
	[DisplayInfo(null, true, type: SystemType.String)]
	public string? ToHour { get; set; }


	[DisplayName("مبلغ اضافه کاری")]
	[DisplayInfo(null, true, type: SystemType.Long)]
	public long? WorkOvertimeFee { get; set; }


	[DisplayName("ساعت اضافه کاری")]
	[DisplayInfo(null, true, type: SystemType.Long)]
	public string? WorkOvertimeHour { get; set; }


	[DisplayName("ضریب ماموریت")]
	[DisplayInfo(null, true, type: SystemType.Long)]
	public long? MissionRightFactor { get; set; }


	[DisplayName("حق ماموریت")]
	[DisplayInfo(null, true, type: SystemType.Long)]
	public long? MissionRight { get; set; }


	[DisplayName("ایاب ذهاب شخصی")]
	[DisplayInfo(null, true, type: SystemType.Long)]
	public long? TransportationPersonal { get; set; }


	[DisplayName("ایاب ذهاب سواری")]
	[DisplayInfo(null, true, type: SystemType.Long)]
	public long? TransportationDriving { get; set; }


	[DisplayName("حق تعطیل کاری")]
	[DisplayInfo(null, true, type: SystemType.Long)]
	public long? MissionHolidayRight { get; set; }


	[DisplayName("تعداد تعطیل کاری")]
	[DisplayInfo(null, true, type: SystemType.Long)]
	public long? MissionHolidayRightFactor { get; set; }


	[DisplayName("حقوق روزانه")]
	[DisplayInfo(null, true, type: SystemType.Long)]
	public long? DailySalary { get; set; }


	[DisplayName("هزینه صبحانه")]
	[DisplayInfo(null, true, type: SystemType.Decimal)]
	public decimal? BreakfastPrice { get; set; }

	[DisplayName("تعداد صبحانه")]
	[DisplayInfo(null, true, type: SystemType.Int)]
	public int? BreakfastQuantity { get; set; }

	[DisplayName("هزینه ناهار")]
	[DisplayInfo(null, true, type: SystemType.Decimal)]
	public decimal? LunchPrice { get; set; }

	[DisplayName("تعداد ناهار")]
	[DisplayInfo(null, true, type: SystemType.Int)]
	public int? LunchQuantity { get; set; }

	[DisplayName("هزینه شام")]
	[DisplayInfo(null, true, type: SystemType.Decimal)]
	public decimal? DinnerPrice { get; set; }

	[DisplayName("تعداد شام")]
	[DisplayInfo(null, true, type: SystemType.Int)]
	public int? DinnerQuantity { get; set; }

	[DisplayName("مبلغ حق شب")]
	[DisplayInfo(null, true, type: SystemType.Decimal)]
	public decimal? NightShiftAllowance { get; set; }

	[DisplayName("ضریب حق شب")]
	[DisplayInfo(null, true, type: SystemType.Decimal)]
	public decimal? NightShiftCoefficient { get; set; }


	[DisplayName("سایر هزینه")]
	[DisplayInfo(null, true, type: SystemType.Long)]
	public long? OtherCost { get; set; }

	[DisplayName("مجموع هزینه")]
	[DisplayInfo(null, true, type: SystemType.Long)]
	public long? TotalCost { get; set; }


	[DisplayName("کامنت")]
	[DisplayInfo(null, true, type: SystemType.String)]
	public string? Comment { get; set; }


}
