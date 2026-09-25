using Common.Attributes;
using Entities.App.Gnr;
using Entities.Auth;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Sup
{
	/// <summary>
	/// پیشینه ارسال به پیمانکاران — معادل HTS <c>Sup_OpenOrderRequest_EmailToSupplier</c> (صفحه 545).
	/// HTS موضوع/بدنه را ذخیره نمی‌کرد؛ در سامانه جدید برای صف <c>system.Notification</c> اختیاری‌اند.
	/// </summary>
	[Display(Name = "پیشینه ارسال به پیمانکاران")]
	[Table("OpenOrderRequestEmailToSupplier", Schema = "Sup")]
	[Index(nameof(OpenOrderRequestId))]
	[Index(nameof(SupplierId))]
	[Index(nameof(HtsId), IsUnique = true)]
	public class OpenOrderRequestEmailToSupplier : BaseEntity
	{
		[DisplayName("درخواست باز")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public OpenOrderRequest OpenOrderRequest { get; set; } = null!;

		public long OpenOrderRequestId { get; set; }

		[DisplayName("پیمانکار / تامین‌کننده")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public Supplier Supplier { get; set; } = null!;

		public long SupplierId { get; set; }

		[DisplayName("تاریخ و زمان ارسال میلادی")]
		[DisplayInfo(null, true, type: SystemType.DateTime)]
		public DateTime? SendMiladiDateTime { get; set; }

		[DisplayName("تاریخ و زمان ارسال شمسی")]
		[DisplayInfo("SendMiladiDateTime", true, type: SystemType.DateTimeShamsi)]
		[MaxLength(30)]
		public string? SendShamsiDateTime { get; set; }

		[DisplayName("کاربر ارسال‌کننده")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public User? SenderUser { get; set; }

		public long? SenderUserId { get; set; }

		[DisplayName("ایمیل گیرنده")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(200)]
		public string? ToEmail { get; set; }

		[DisplayName("موضوع")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(500)]
		public string? Subject { get; set; }

		[DisplayName("متن نامه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? Body { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Comment { get; set; }

		[DisplayName("تعداد اعلام‌شده")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? SendingCount { get; set; }

		/// <summary>پیوست درخواست باز — معادل HTS <c>Attachment_FK</c> → <c>Sup_OpenOrderRequest_Attachment</c> (فایل در <see cref="FileEntity"/>).</summary>
		[DisplayName("پیوست درخواست باز")]
		[DisplayInfo(null, true, type: SystemType.File, fileTypes: ".rar,.zip,.pdf,")]
		public FileEntity? Attachment { get; set; }

		public long? AttachmentId { get; set; }

		/// <summary>پیوست کالا — معادل HTS <c>PartAttachmentId</c> → <c>Inv_Part_Attachment</c> (فایل در <see cref="FileEntity"/>).</summary>
		[DisplayName("پیوست کالا")]
		[DisplayInfo(null, true, type: SystemType.File, fileTypes: ".rar,.zip,.pdf,")]
		public FileEntity? PartAttachment { get; set; }

		public long? PartAttachmentId { get; set; }

		/// <summary>پیوست مدرک EDMS — معادل HTS <c>EdmsDocumentAttachmentId</c> → <c>Edms_Document</c> (معمولاً MainFile).</summary>
		[DisplayName("پیوست مدرک EDMS")]
		[DisplayInfo(null, true, type: SystemType.File, fileTypes: ".rar,.zip,.pdf,")]
		public FileEntity? EdmsDocumentAttachment { get; set; }

		public long? EdmsDocumentAttachmentId { get; set; }

		/// <summary>شناسه ردیف در HTS (<c>OpenOrderRequest_EmailToSupplier_ID</c>) برای مهاجرت یکتا.</summary>
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? HtsId { get; set; }
	}
}
