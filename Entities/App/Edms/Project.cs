using Common.Attributes;
using Entities.App.Edms.Enums;
using Entities.App.Hrm;
using Entities.Auth;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Edms
{
	[Display(Name = "پروژه ")]
	[Table("Project", Schema = "Edms")]
	public class Project : BaseEntity
	{
		[DisplayName("عنوان")]
		[DisplayInfo(null, true, SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(500)]
		public string Title { get; set; }

		[DisplayName("نام")]
		[DisplayInfo(null, true, SystemType.String)]
		[MaxLength(500)]
		public string? Name { get; set; }

		[DisplayName("کد")]
		[DisplayInfo(null, true, SystemType.String)]
		[MaxLength(200)]
		public string? Code { get; set; }

		[DisplayName("شرکت طرف قرارداد (مشتری)")]
		[DisplayInfo(null, true, SystemType.String)]
		[MaxLength(500)]
		public string? ContractPartyCustomer { get; set; }

		[DisplayName("نام سایت (یا نام پروژه اصلی در مدارک کارفرما)")]
		[DisplayInfo(null, true, SystemType.String)]
		[MaxLength(500)]
		public string? ProjectName { get; set; }

		[DisplayName("نوع پکیج (هوا، درایر، نیتروژن و غیره)")]
		[DisplayInfo(null, true, SystemType.String)]
		[MaxLength(200)]
		public string? PackageType { get; set; }

		[DisplayName("تاریخ شروع میلادی")]
		[DisplayInfo(null, true, SystemType.Date)]
		public DateTime? StartMiladiDate { get; set; }

		[DisplayName("تاریخ شروع شمسی")]
		[DisplayInfo("StartMiladiDate", true, SystemType.DateShamsi)]
		[MaxLength(30)]
		public string? StartShamsiDate { get; set; }

		[DisplayName("تاریخ پایان میلادی")]
		[DisplayInfo(null, true, SystemType.Date)]
		public DateTime? ExpirationMiladiDate { get; set; }

		[DisplayName("تاریخ پایان شمسی")]
		[DisplayInfo("ExpirationMiladiDate", true, SystemType.DateShamsi)]
		[MaxLength(30)]
		public string? ExpirationShamsiDate { get; set; }

		[DisplayName("شناسه مدیر پروژه")]
		[DisplayInfo(null, true, SystemType.Long)]
		public long? ProjectManagerId { get; set; }

		[DisplayName("مدیر پروژه")]
		[DisplayInfo(null, true, SystemType.Entity)]
		public User? ProjectManager { get; set; }

		[DisplayName("وضعیت")]
		[DisplayInfo(null, true, SystemType.Select, required: true)]
		public ProjectStatusEnum Status { get; set; }

		[DisplayName("شناسه مهندس مسئول psl")]
		[DisplayInfo(null, true, SystemType.Long)]
		public long? ProjectManagerPSLId { get; set; }

		[DisplayName("مهندس مسئول psl")]
		[DisplayInfo(null, true, SystemType.Entity)]
		public User? ProjectManagerPSL { get; set; }

		[DisplayName("شناسه هماهنگ کننده")]
		[DisplayInfo(null, true, SystemType.Long)]
		public long? CoordinatorId { get; set; }

		[DisplayName("هماهنگ کننده")]
		[DisplayInfo(null, true, SystemType.Entity)]
		public User? Coordinator { get; set; }

		[DisplayName("شناسه واحد ذینفع")]
		[DisplayInfo(null, true, SystemType.Long)]
		public long SubjectUnitId { get; set; }

		[DisplayName("واحد ذینفع")]
		[DisplayInfo(null, true, SystemType.Entity)]
		public OrgUnit SubjectUnit { get; set; }

		[DisplayName("شناسه کارشناس فروش")]
		[DisplayInfo(null, true, SystemType.Long)]
		public long? SalesExpertId { get; set; }

		[DisplayName("کارشناس فروش")]
		[DisplayInfo(null, true, SystemType.Entity)]
		public User? SalesExpert { get; set; }

		[DisplayName("شناسه dcc")]
		[DisplayInfo(null, true, SystemType.Long)]
		public long? DccId { get; set; }

		[DisplayName("dcc")]
		[DisplayInfo(null, true, SystemType.Entity)]
		public User? Dcc { get; set; }

		[DisplayName("شناسه کارشناس فرآیند")]
		[DisplayInfo(null, true, SystemType.Long)]
		public long? ProcessEngineerId { get; set; }

		[DisplayName("کارشناس فرآیند")]
		[DisplayInfo(null, true, SystemType.Entity)]
		public User? ProcessEngineer { get; set; }

		[DisplayName("شناسه کارشناس مکانیک")]
		[DisplayInfo(null, true, SystemType.Long)]
		public long? MechanicExpertId { get; set; }

		[DisplayName("کارشناس مکانیک")]
		[DisplayInfo(null, true, SystemType.Entity)]
		public User? MechanicExpert { get; set; }

		[DisplayName("شناسه کارشناس کنترل")]
		[DisplayInfo(null, true, SystemType.Long)]
		public long? ControlEngineerId { get; set; }

		[DisplayName("کارشناس کنترل")]
		[DisplayInfo(null, true, SystemType.Entity)]
		public User? ControlEngineer { get; set; }

		[DisplayName("شناسه کارشناس ابزار دقیق")]
		[DisplayInfo(null, true, SystemType.Long)]
		public long? ToolExpertId { get; set; }

		[DisplayName("کارشناس ابزار دقیق")]
		[DisplayInfo(null, true, SystemType.Entity)]
		public User? ToolExpert { get; set; }

		[DisplayName("شناسه کارشناس برق")]
		[DisplayInfo(null, true, SystemType.Long)]
		public long? ElectricEngineerId { get; set; }

		[DisplayName("کارشناس برق")]
		[DisplayInfo(null, true, SystemType.Entity)]
		public User? ElectricEngineer { get; set; }

		[DisplayName("شناسه کارشناس پایپینگ")]
		[DisplayInfo(null, true, SystemType.Long)]
		public long? PipingExpertId { get; set; }

		[DisplayName("کارشناس پایپینگ")]
		[DisplayInfo(null, true, SystemType.Entity)]
		public User? PipingExpert { get; set; }

		[DisplayName("شناسه بازرس پروژه")]
		[DisplayInfo(null, true, SystemType.Long)]
		public long? ProjectReviewerId { get; set; }

		[DisplayName("بازرس پروژه")]
		[DisplayInfo(null, true, SystemType.Entity)]
		public User? ProjectReviewer { get; set; }

		[DisplayName("شناسه مسئول تدارکات برق/ابزار دقیق")]
		[DisplayInfo(null, true, SystemType.Long)]
		public long? PowerAndPrecisionPrepManagerId { get; set; }

		[DisplayName("مسئول تدارکات برق/ابزار دقیق")]
		[DisplayInfo(null, true, SystemType.Entity)]
		public User? PowerAndPrecisionPrepManager { get; set; }

		[DisplayName("شناسه مسئول تدارکات مکانیک")]
		[DisplayInfo(null, true, SystemType.Long)]
		public long? MechanicalPreparationsResponsibleId { get; set; }

		[DisplayName("مسئول تدارکات مکانیک")]
		[DisplayInfo(null, true, SystemType.Entity)]
		public User? MechanicalPreparationsResponsible { get; set; }

		[DisplayName("شناسه مسئول تدارکات خارجی")]
		[DisplayInfo(null, true, SystemType.Long)]
		public long? ForeignPreparationsAgentId { get; set; }

		[DisplayName("مسئول تدارکات خارجی")]
		[DisplayInfo(null, true, SystemType.Entity)]
		public User? ForeignPreparationsAgent { get; set; }

		[DisplayName("شناسه دارای دسترسی به آرشیو و اسناد ورودی (غیر مهندسی)")]
		[DisplayInfo(null, true, SystemType.Long)]
		public long? HasAccessToArchiveAndInputDocumentsId { get; set; }

		[DisplayName("دارای دسترسی به آرشیو و اسناد ورودی (غیر مهندسی)")]
		[DisplayInfo(null, true, SystemType.Entity)]
		public User? HasAccessToArchiveAndInputDocuments { get; set; }

		[DisplayName("پروژه از نوع وندوری هست")]
		[DisplayInfo(null, true, SystemType.Boolean)]
		public bool ProjectIsVendoriType { get; set; } = false;

		[DisplayName("پیش کد ترانسمیتال")]
		[DisplayInfo(null, true, SystemType.String)]
		[MaxLength(200)]
		public string? TransmissionPrefix { get; set; }

		[DisplayName("گیرنده ترانسمیتال")]
		[DisplayInfo(null, true, SystemType.String)]
		[MaxLength(500)]
		public string? TransmitterRecipient { get; set; }

		[DisplayName("گیرنده کپی ترانسمیتال")]
		[DisplayInfo(null, true, SystemType.String)]
		[MaxLength(500)]
		public string? ReceiverCopyTransmital { get; set; }

		[DisplayName("ایمیل متفرقه/عمومی")]
		[DisplayInfo(null, true, SystemType.String)]
		[MaxLength(500)]
		public string? EmailAddress { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, SystemType.String)]
		[MaxLength(2000)]
		public string? Explanation { get; set; }

		[DisplayName("محرمانه است")]
		[DisplayInfo(null, true, SystemType.Boolean)]
		public bool IsConfidential { get; set; } = false;

		[DisplayName("کاربران محرمانه")]
		[DisplayInfo(null, true, SystemType.String)]
		public string? SensitiveUsers { get; set; }  

		[DisplayName("شناسه کاربران محرمانه")]
		[DisplayInfo(null, true, SystemType.String)]
		public string? SensitiveUsersIds { get; set; }  

		[DisplayName("پروژه تکوین است")]
		[DisplayInfo(null, true, SystemType.Boolean)]
		public bool IsTakvinProject { get; set; } = false;

		[DisplayName("کاربران تکوین")]
		[DisplayInfo(null, true, SystemType.String)]
		public string? UserBenefitTakvin { get; set; } 

		[DisplayName("شناسه کاربران تکوین")]
		[DisplayInfo(null, true, SystemType.ListLong)]
		public string? UserBenefitTakvinIds { get; set; }  

		[DisplayName("روز تاخیر مجاز هوایار")]
		[DisplayInfo(null, true, SystemType.Int, regex: @"^[\u06F0-\u06F90-9]+$", regexInvalidError: "فقط عدد")]
		public int? LegalDelayHavayar { get; set; }

		[DisplayName("روز تاخیر مجاز کارفرما")]
		[DisplayInfo(null, true, SystemType.Int, regex: @"^[\u06F0-\u06F90-9]+$", regexInvalidError: "فقط عدد")]
		public int? LegalClientDayDelay { get; set; }
		[DisplayName("درصد پیشرفت")]
		[DisplayInfo(null, true, type: SystemType.ListEntity)]
		public List<ProjectProgressPercentage> ProgressPercentage { get; set; } = new();

		[DisplayName("پیوست")]
		[DisplayInfo(null, true, type: SystemType.ListEntity)]
		public List<ProjectAttachment> ProjectAttachments { get; set; } = new();
	}

	[Display(Name = "درصد پیشرفت")]
	[Table("ProjectProgressPercentage", Schema = "Edms")]
	public class ProjectProgressPercentage : BaseEntity
	{
		public long ProjectId { get; set; }
		public Project Project { get; set; }

		[DisplayName("وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public DocumentStatusEnums? Status { get; set; }


		[DisplayName("درصد")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? Percentage { get; set; }


	}

	[Display(Name = "پیوست پروژه")]
	[Table("ProjectAttachment", Schema = "Edms")]
	public class ProjectAttachment : BaseEntity
	{
		public long ProjectId { get; set; }
		public Project Project { get; set; }

		[DisplayName("فایل پیوست")]
		[DisplayInfo(null, false, type: SystemType.File, fileTypes: ".rar,.zip,.pdf,.excel,.word")]

		public FileEntity Attachment { get; set; }
		public long AttachmentId { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, SystemType.String)]
		public string Comment { get; set; }

		[DisplayName("نوع مدرک")]
		[DisplayInfo(null, true, type: SystemType.Select)]

		public ProjectAttachmentTypeEnum Type { get; set; }
	}
}
