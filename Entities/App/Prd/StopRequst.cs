using Common.Attributes;
using Entities.App.Prd.Enums;
using Entities.App.Sale;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Prd
{
	[Display(Name = "درخواست توقف")]
	[Table("StopRequst", Schema = "Prd")]
	public class StopRequst : BaseEntity
	{
		[DisplayName("قلم سفارش ساخت")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public ProductionOrderItem? ProductionOrderItem { get; set; }

		public long? ProductionOrderItemId { get; set; }


		[DisplayName(" تاریخ شروع توقف شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateTimeShamsi)]
		public string? StopStartShamsiDateTime { get; set; }


		[DisplayName("محل مشاهده")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public StopRequstObservationLocationEnum? ObservationLocation { get; set; }


		[DisplayName("وضعیت تولید")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public StopRequstProductionStatusEnum? ProductionStatus { get; set; }


		[DisplayName(" تاریخ شروع توقف میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? StopStartMiladiDateTime { get; set; }


		[DisplayName("ساعت کارکرد دستگاه")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? OperatingHours { get; set; }


		[DisplayName("علت توقف")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? StoppingReason { get; set; }


	}
}
