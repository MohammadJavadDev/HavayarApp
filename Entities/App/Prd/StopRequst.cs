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


		[DisplayName("وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public StopRequstModuleStatusEnum? LastStatus { get; set; }


		[DisplayName("تاریخ شروع توقف شمسی")]
		[DisplayInfo("StopStartMiladiDateTime", true, type: SystemType.DateTimeShamsi, required: true)]
		[MaxLength(30)]
		public string? StopStartShamsiDateTime { get; set; }


		[DisplayName("تاریخ شروع توقف میلادی")]
		[DisplayInfo(null, false, type: SystemType.DateTime)]
		public DateTime? StopStartMiladiDateTime { get; set; }


		[DisplayName("محل مشاهده")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public StopRequstObservationLocationEnum? ObservationLocation { get; set; }


		[DisplayName("وضعیت تولید")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public StopRequstProductionStatusEnum? ProductionStatus { get; set; }


		[DisplayName("ساعت کارکرد دستگاه")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? OperatingHours { get; set; }


		[DisplayName("شناسه های علت توقف")]
		[DisplayInfo(null, true, type: SystemType.ListLong, required: true)]
		public string? StopReasonIds { get; set; }

		[DisplayName("علت توقف")]
		[DisplayInfo(null, true, type: SystemType.ListString)]
		public string? StopReasonTitles { get; set; }

		[DisplayName("شناسه های واحد های عامل توقف")]
		[DisplayInfo(null, true, type: SystemType.ListLong)]
		public string? ResponsibleUnitIds { get; set; }

		[DisplayName("واحد های عامل توقف")]
		[DisplayInfo(null, true, type: SystemType.ListString)]
		public string? ResponsibleUnitTitles { get; set; }


		[DisplayName("شناسه های مسئولان عامل توقف")]
		[DisplayInfo(null, true, type: SystemType.ListLong)]
		public string? ResponsibleUserIds { get; set; }

		[DisplayName("مسئولان عامل توقف")]
		[DisplayInfo(null, true, type: SystemType.ListString)]
		public string? ResponsibleUserNames { get; set; }


		[DisplayName("شناسه ذینفعان")]
		[DisplayInfo(null, true, type: SystemType.ListLong)]
		public string? BeneficiarieUserIds { get; set; }

		[DisplayName("ذینفعان")]
		[DisplayInfo(null, true, type: SystemType.ListString)]
		public string? BeneficiarieUserNames { get; set; }


		[DisplayName("شناسه ذینفعان جایگزین")]
		[DisplayInfo(null, true, type: SystemType.ListLong)]
		public string? AlternativeBeneficiarieUserIds { get; set; }

		[DisplayName("ذینفعان جایگزین")]
		[DisplayInfo(null, true, type: SystemType.ListString)]
		public string? AlternativeBeneficiarieUserNames { get; set; }


		[DisplayName("شرح")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(2048)]
		public string? Description { get; set; }


		[DisplayName("شماره عدم انطباق")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(100)]
		public string? ConformityCheckRequestNumber { get; set; }


		[DisplayName("شرح اقدامات مسئول عامل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? ResponsibleUserDescription { get; set; }


		[DisplayName("تاریخ پیشنهادی پایان توقف شمسی")]
		[DisplayInfo("SuggestedDateForFinishOfStop", true, type: SystemType.DateShamsi)]
		[MaxLength(16)]
		public string? SuggestedDateForFinishOfStopShamsi { get; set; }

		[DisplayName("تاریخ پیشنهادی پایان توقف میلادی")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? SuggestedDateForFinishOfStop { get; set; }


		[DisplayName("تاریخ جلسه تولید شمسی")]
		[DisplayInfo("ProductionSessionDate", true, type: SystemType.DateShamsi)]
		[MaxLength(16)]
		public string? ProductionSessionShamsiDate { get; set; }

		[DisplayName("تاریخ جلسه تولید میلادی")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? ProductionSessionDate { get; set; }


		[DisplayName("شناسه شرکت‌کنندگان جلسه")]
		[DisplayInfo(null, true, type: SystemType.ListLong)]
		public string? SessionUserIds { get; set; }

		[DisplayName("شرکت‌کنندگان جلسه")]
		[DisplayInfo(null, true, type: SystemType.ListString)]
		public string? SessionUserNames { get; set; }


		[DisplayName("نتیجه جلسه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? SessionResult { get; set; }


		[DisplayName("نیاز به تیم CFT")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? IsNeedToCftTeam { get; set; }


		[DisplayName("شرح CFT")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? CftDescription { get; set; }


		[DisplayName("اثر بخش بودن")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? IsEffective { get; set; }


		[DisplayName("شرح اثربخشی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? EffectivityDescription { get; set; }


		[DisplayName("تاریخ پایان توقف شمسی")]
		[DisplayInfo("StopFinishDate", true, type: SystemType.DateTimeShamsi)]
		[MaxLength(30)]
		public string? StopFinishShamsiDate { get; set; }

		[DisplayName("تاریخ پایان توقف میلادی")]
		[DisplayInfo(null, false, type: SystemType.DateTime)]
		public DateTime? StopFinishDate { get; set; }


		[DisplayName("پیوست‌ها")]
		[DisplayInfo(null, true, type: SystemType.ListEntity)]
		public List<StopRequstAttachment> Attachments { get; set; } = new();


		[DisplayName("کامنت‌ها")]
		[DisplayInfo(null, false, type: SystemType.ListEntity)]
		public List<StopRequstComment> Comments { get; set; } = new();
	}


	[Display(Name = "کامنت درخواست توقف")]
	[Table("StopRequstComment", Schema = "Prd")]
	public class StopRequstComment : BaseEntity
	{
		public long StopRequstId { get; set; }
		public StopRequst StopRequst { get; set; } = null!;

		[DisplayName("وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public StopRequstModuleStatusEnum? Status { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(4000)]
		public string? Description { get; set; }
	}


	[Display(Name = "پیوست درخواست توقف")]
	[Table("StopRequstAttachment", Schema = "Prd")]
	public class StopRequstAttachment : BaseEntity
	{
		public long StopRequstId { get; set; }
		public StopRequst StopRequst { get; set; } = null!;

		[DisplayName("پیوست")]
		[DisplayInfo(null, true, type: SystemType.File, fileTypes: ".rar,.zip,.pdf,.excel,.word,.jpg,.png,")]
		public FileEntity? Attachment { get; set; }

		public long? AttachmentId { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? Comment { get; set; }
	}
}
