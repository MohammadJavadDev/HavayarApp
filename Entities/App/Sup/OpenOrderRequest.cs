using Common.Attributes;
using Entities.App.Edms;
using Entities.App.FIN;
using Entities.App.Gnr;
using Entities.App.Inv;
using Entities.App.Sale;
using Entities.App.Sup.Enums;
using Entities.Auth;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
 

namespace Entities.App.Sup
{
	[Display(Name = "درخواست های باز")]
	[Table("OpenOrderRequest", Schema = "Sup")]
	public class OpenOrderRequest : BaseEntity
	{
		[DisplayName("شناسه OrderRow")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? OrderRowId { get; set; }


		[DisplayName("سال")]
		[DisplayInfo(null, true, type: SystemType.Long, required: true)]
		public long Year { get; set; }


		[DisplayName("شماره حواله فروش")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? OrderNo { get; set; }


		[DisplayName("تاریخ حواله فروش شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		[MaxLength(10)]
		public string? OrderShamsiDate { get; set; }


		[DisplayName("تاریخ حواله فروش میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? OrderMiladiDate { get; set; }


		[DisplayName("تفصیل")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public DL? Dl { get; set; }

		public long? DlId { get; set; }


		[DisplayName("کالا")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public Part Part { get; set; }

		public long PartId { get; set; }
 
		[DisplayName("تاریخ نیاز شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? NeedDateShamsiDate { get; set; }
		[DisplayName("تاریخ نیاز میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? NeedDateMiladiDate { get; set; }


		[DisplayName("تاریخ تایید شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? OrderConfirmShamsiDate { get; set; }

		[DisplayName("تاریخ تایید میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? OrderConfirmMiladiDate { get; set; }


		[DisplayName("تعداد ارسال شده")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? SumSendQty { get; set; }


		[DisplayName("تعداد رسیده شده")]
		[DisplayInfo(null, true, type: SystemType.Int, required: true)]
		public int FactoredCount { get; set; }


		[DisplayName("تعداد سفارش")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? OrderQty { get; set; }


		[DisplayName("تعداد نیاز")]
		[DisplayInfo(null, true, type: SystemType.Int, required: true)]
		public int RequiredQty { get; set; }


		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1600)]
		public string? OrderItemComment { get; set; }


		[DisplayName("متوقف شده")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsStop { get; set; } = false;


		[DisplayName("تاریخ توقف شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? StopShamsiDate { get; set; }

		[DisplayName("تاریخ توقف میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? StopMiladiDate { get; set; }


		[DisplayName("حذف شده")]
		[DisplayInfo(null, true, type: SystemType.Boolean, required: true)]
		public bool IsDeleted { get; set; } = false;


		[DisplayName("تاریخ حذف شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? IsDeletedShamsiDate { get; set; }

		[DisplayName("تاریخ حذف میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? IsDeletedMiladiDate { get; set; }


		[DisplayName("حذف اجباری توسط کاربر")]
		[DisplayInfo(null, true, type: SystemType.Boolean, required: true)]
		public bool IsForceDeletedByUser { get; set; } = false;


		[DisplayName("تاریخ ثبت پرسنل درخواست کننده شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? RequestedPersonelRegShamsiDate { get; set; }

		[DisplayName("تاریخ ثبت پرسنل درخواست کننده میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? RequestedPersonelRegMiladiDate { get; set; }


		[DisplayName("پرسنل درخواست کننده")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? RequestedPersonel { get; set; }


		[DisplayName("ایمیل پرسنل درخواست ‌کننده")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(4000)]
		public string? RequestedPersonelEmail { get; set; }

		[DisplayName("شناسه پرسنل درخواست کننده")]
		[DisplayInfo(null, true, type: SystemType.ListLong)]
 
		public List<long>? RequestedPersonelIds { get; set; }



		[DisplayName("وضعیت درخواست")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(160)]
		public string? Status { get; set; }


		[DisplayName("ویرایش شده")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool Changed { get; set; } = false;


		[DisplayName("ایمیل اعلان به پرسنل درخواست‌کننده")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool NotifyEmailRequestedPersonel { get; set; } = false;


		[DisplayName("شناسه‌های کاغذ ارسال‌شده ایمیل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? NotifyEmailSendPaperIds { get; set; }


		[DisplayName("تایید مهندسی")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool EngineeringAccept { get; set; } = false;


		[DisplayName("کاربر مهندسی")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public User? EngineeringAcceptUser { get; set; }

		public long? EngineeringAcceptUserId { get; set; }


		[DisplayName("تاریخ تایید مهندسی شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateTimeShamsi)]
		public string? EngineeringAcceptShamsiDateTime { get; set; }


		[DisplayName("تاریخ تایید مهندسی میلادی")]
		[DisplayInfo(null, true, type: SystemType.DateTime)]
		public DateTime? EngineeringConfirmationMiladiDateTime { get; set; }


		[DisplayName("تایید خودکار توسط مهندسی")]
		[DisplayInfo(null, true, type: SystemType.Boolean, required: true)]
		public bool IsAcceptedAutomaticallyByEngineering { get; set; } = false;


		[DisplayName("تاریخ اختتام شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? CompletionShamsiDate { get; set; }

		[DisplayName("تاریخ اختتام میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? CompletionMiladiDate { get; set; }


		[DisplayName("تاخیر خرید")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? DelaysBuyDay { get; set; }


		[DisplayName("شماره سفارش ساخت")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? ProductionOrderNumber { get; set; }


		[DisplayName("عدم تایید QC")]
		[DisplayInfo(null, true, type: SystemType.Boolean, required: true)]
		public bool IsRejectedByInspection { get; set; } = false;


		[DisplayName("تاریخ فاکتوردهی میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? FactoredMiladiDate { get; set; }


		[DisplayName("تاریخ فاکتوردهی شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? FactoredShamsiDate { get; set; }


		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2024)]
		public string? Comment { get; set; }


		[DisplayName("شناسه قلم درخواست خرید")]
		[DisplayInfo(null, true, type: SystemType.Long, required: true)]
		public long PurchaseRequestItemId { get; set; }


		[DisplayName("شماره درخواست خرید")]
		[DisplayInfo(null, true, type: SystemType.Long, required: true)]
		public long PurchaseRequestNumber { get; set; }


		[DisplayName("تاریخ درخواست خرید میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? PurchaseRequestMiladiDate { get; set; }


		[DisplayName("تاریخ درخواست خرید شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? PurchaseRequestShamsiDate { get; set; }


		[DisplayName("تایید پروژه/فروش")]
		[DisplayInfo(null, true, type: SystemType.Boolean, required: true)]
		public bool HasSalesUnitConfirmation { get; set; } = false;


		[DisplayName("کاربر تأیید واحد فروش")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public User? SalesUnitConfirmationUser { get; set; }

		public long? SalesUnitConfirmationUserId { get; set; }


		[DisplayName("تاریخ و زمان تأیید واحد فروش میلادی ")]
		[DisplayInfo(null, true, type: SystemType.DateTime)]
		public DateTime? SalesUnitConfirmationMiladiDateTime { get; set; }


		[DisplayName("تاریخ و زمان تأیید واحد فروش شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateTimeShamsi)]
		public string? SalesUnitConfirmationShamsiDateTime { get; set; }


		[DisplayName("توضیحات تأیید واحد فروش")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? SalesUnitConfirmationComment { get; set; }


		[DisplayName("سفارش ساخت")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public ProductionOrder? ProductionOrder { get; set; }

		public long? ProductionOrderId { get; set; }


		[DisplayName("کارشناس واحد فروش")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public User? SalesUnitSalesExpert { get; set; }

		public long? SalesUnitSalesExpertId { get; set; }


		[DisplayName("مدیر واحد فروش")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public User? SalesUnitSalesManager { get; set; }

		public long? SalesUnitSalesManagerId { get; set; }


		[DisplayName("مدیر پروژه واحد فروش")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public User? SalesUnitProjectManager { get; set; }

		public long? SalesUnitProjectManagerId { get; set; }


		[DisplayName("کالای مرتبط")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Part? RelatedPart { get; set; }

		public long? RelatedPartId { get; set; }


		[DisplayName("تاریخ سند تحویل میلادی ")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? DeliveryVoucherMiladiDate { get; set; }


		[DisplayName("تاریخ سند  تحویل شمسی ")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? DeliveryVoucherShamsiDate { get; set; }


		[DisplayName("تاریخ سند موقت میلادی ")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? TemporaryInventoryVoucherMiladiDate { get; set; }


		[DisplayName("تاریخ سند  موقت شمسی ")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? TemporaryInventoryVoucherShamsiDate { get; set; }


		[DisplayName("تاریخ سند Qc میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? QcVoucherMiladiDate { get; set; }


		[DisplayName("تاریخ سند Qc  شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? QcVoucherShamsiDate { get; set; }


		[DisplayName("تاریخ سند دائم میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? FinalInventoryVoucherMiladiDate { get; set; }


		[DisplayName("تاریخ سند دائم شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? FinalInventoryVoucherShamsiDate { get; set; }


		[DisplayName("تاریخ تامین میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? SupplyMiladiDate { get; set; }


		[DisplayName("تاریخ تامین شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? SupplyShamsiDate { get; set; }


		[DisplayName("تایید اولیه واحد فروش")]
		[DisplayInfo(null, true, type: SystemType.Boolean, required: true)]
		public bool HasSalesUnitPrimitiveApprove { get; set; } = false;


		[DisplayName("درخواست روتین")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsRoutineRequest { get; set; } = false;


		[DisplayName("ایمیل های مهندسی درخواست کنندگان")]
		[DisplayInfo(null, true, type: SystemType.String)]
 
		public string? RequestedEngineeringPersonelEmail { get; set; }


		[DisplayName("مهندسی درخواست کنندگان")]
		[DisplayInfo(null, true, type: SystemType.String)]
 
		public string? RequestedEngineeringPersonel { get; set; }

		[DisplayName("شناسه مهندسی درخواست کنندگان")]
		[DisplayInfo(null, true, type: SystemType.ListLong)]

		public List<long>? RequestedEngineeringPersonelIds { get; set; }



		[DisplayName("تامین کننده")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Supplier? ManCompany { get; set; }

		public long? ManCompanyId { get; set; }


		[DisplayName("وضعیت پایان بررسی ")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public OpenOrderRequestStopCheckingStatusEnum? StopCheckingStatus { get; set; }


		[DisplayName("وضعیت توقف")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public OpenOrderRequestStopStatusEnum? StopStatus { get; set; }


		[DisplayName("پیوست مدارک")]
		[DisplayInfo(null, true, type: SystemType.ListEntity)]
		public List<OpenOrderRequestAttachment> Attachments { get; set; } = new();


		[DisplayName("کامنت ها")]
		[DisplayInfo(null, true, type: SystemType.ListEntity)]
		public List<OpenOrderRequestComment> Comments { get; set; } = new();


	}

	[Display(Name = "پیوست درخواست های باز")]
	[Table("OpenOrderRequestAttachment", Schema = "Sup")]
	public class OpenOrderRequestAttachment : BaseEntity
	{
		public long OpenOrderRequestId { get; set; }
		public OpenOrderRequest OpenOrderRequest { get; set; }

		[DisplayName("Attachment")]
		[DisplayInfo(null, true, type: SystemType.File, fileTypes: ".rar,.zip,.pdf,")]
		public FileEntity? Attachment { get; set; }

		public long? AttachmentId { get; set; }


		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? Comment { get; set; }


		[DisplayName("نوع فایل")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public OpenOrderRequestAttachmentFileTypeEnum? FileType { get; set; }


	}


	[Display(Name = "کامنت های درخواست های باز")]
	[Table("OpenOrderRequestComment", Schema = "Sup")]
	public class OpenOrderRequestComment : BaseEntity
	{
		public long OpenOrderRequestId { get; set; }
		public OpenOrderRequest OpenOrderRequest { get; set; }

		[DisplayName("تاریخ میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? MiladiDate { get; set; }


		[DisplayName("تاریخ شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? ShamsiDate { get; set; }


		[DisplayName("متوقف شده")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsStop { get; set; } = false;


		[DisplayName("راه اندازی شده")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsLaunched { get; set; } = false;


		[DisplayName("دارای تایید واحد فروش")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool HasSalesUnitConfirmation { get; set; } = false;


		[DisplayName("کامنت تایید واحد فروش")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? SalesUnitConfirmationComment { get; set; }


		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? CommentValue { get; set; }


		[DisplayName("نوع توقف")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public OpenOrderRequestStopTypeEnum? StopType { get; set; }


		[DisplayName("عامل توقف")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public User? StopOperator { get; set; }

		public long? StopOperatorId { get; set; }


		[DisplayName("شناسه کاربران ذینفع")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? BeneficiariesIds { get; set; }


		[DisplayName("نام کاربران ذینفع")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? BeneficiariesNames { get; set; }


		[DisplayName("تاریخ مهلت پاسخ میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? ResponseDeadlineMiladiDate { get; set; }


		[DisplayName("تاریخ مهلت پاسخ شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? ResponseDeadlineShamsiDate { get; set; }


		[DisplayName("کامنت عامل توقف")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? StopOperatorComment { get; set; }


		[DisplayName("پیوست")]
		[DisplayInfo(null, true, type: SystemType.File, fileTypes: ".pdf,.zip,.rar")]
		public FileEntity? Attachment { get; set; }

		public long? AttachmentId { get; set; }


		[DisplayName("نتیجه بررسی توقف")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public OpenOrderRequestCommentStopCheckingResultEnum? StopCheckingResult { get; set; }


		[DisplayName("تاریخ بررسی توقف میلادی")]
		[DisplayInfo(null, true, type: SystemType.DateTime)]
		public DateTime? StopCheckingMiladiDateTime { get; set; }


		[DisplayName("تاریخ بررسی توقف")]
		[DisplayInfo(null, true, type: SystemType.DateTimeShamsi)]
		public string? StopCheckingShamsiDateTime { get; set; }


		[DisplayName("توضیحات بررسی توقف")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? StopCheckingComment { get; set; }


		[DisplayName("پیوست جایگزین")]
		[DisplayInfo(null, true, type: SystemType.File, fileTypes: ".pdf,.rar,.zip")]
		public FileEntity? AlternativeAttachment { get; set; }

		public long? AlternativeAttachmentId { get; set; }


		[DisplayName("متن دلیل توقف بررسی تأخیر")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? StopCheckingDelayReasonText { get; set; }


		[DisplayName("نیاز به بروزرسانی Bom")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsNeedToUpdateBom { get; set; } = false;


		[DisplayName("نیاز به حذف Bom")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsNeedToDeleteBom { get; set; } = false;


		[DisplayName("تاريخ تقريبي اعمال نظر مدير پروژه/ كارفرما میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? ProjectManagerApproximateCommentMiladiDate { get; set; }


		[DisplayName("تاريخ تقريبي اعمال نظر مدير پروژه/ كارفرما شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? ProjectManagerApproximateCommentShamsiDate { get; set; }


		[DisplayName("وضعیت بررسی توقف")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public OpenOrderRequestStopCheckingStatusEnum? StopCheckingStatus { get; set; }


		[DisplayName("توضیحات توقف بررسی وضعیت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? StopCheckingStatusComment { get; set; }


		[DisplayName("توضیحات تکمیلی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? AdditionalDescription { get; set; }


	}

	[Display(Name = "Vpis درخواست های باز")]
	[Table("OpenOrderRequestVpis", Schema = "Sup")]
	public class OpenOrderRequestVpis : BaseEntity
	{

		public long OpenOrderRequestId { get; set; }

		[DisplayName("درخواست باز")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public OpenOrderRequest OpenOrderRequest { get; set; }

		[DisplayName("پروژه")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Project Project { get; set; }
		public long ProjectId { get; set; }

		[DisplayName("Vpis پروژه")]
		[DisplayInfo(null, true, type: SystemType.Entity)]

		public ProjectVpis ProjectVpis { get; set; }
		public long ProjectVpisId { get; set; }

		[DisplayName("مدارک مهندسی")]
		[DisplayInfo(null, true, type: SystemType.Entity)]

		public Document? Document { get; set; }
		public long? DocumentId { get; set; }

		[DisplayName("نسخه")]
		[DisplayInfo(null, true, type: SystemType.Int)]

		public int? RevisionNumber { get; set; }


		[DisplayName("آخرین نسخه")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsLatest { get; set; }

		[DisplayName("کامنت ")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? Comment { get; set; }

	}
}
 