using Common.Attributes;
using Entities.App.Hcm;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Entities.App.Sale
{
	[Display(Name = "حق ماموریت پرسنل")]
	[Table("MissionSalaryPersonnel", Schema = "Sale")]
	public class MissionSalaryPersonnel : BaseEntity
	{
		[DisplayName("پرسنل")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual Personel Personel { get; set; }
		public long? PersonelId { get; set; }
		[DisplayName("هزینه صبحانه")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? BreakfastFee { get; set; }
		[DisplayName("هزینه ناهار")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? LunchFee { get; set; }
		[DisplayName("هزینه شام")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? DinnerFee { get; set; }
		[DisplayName("حق شب")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? NightRight { get; set; }
		[DisplayName("حق ماموریت")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? MissionRight { get; set; }
		[DisplayName("مبلغ اضافه کاری")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? WorkOvertimeFee { get; set; }
		[DisplayName("حق تعطیل کاری")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? MissionHolidayRight { get; set; }
		[DisplayName("حقوق روزانه")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? DailySalary { get; set; }
	}
}