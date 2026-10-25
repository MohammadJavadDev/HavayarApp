using Common.Attributes;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Edms
{
	[Display(Name = "ترانسمیتال")]
	[Table("Transmital", Schema = "Edms")]
	public class Transmital : BaseEntity
	{
		[DisplayName("پروژه")]
		[DisplayInfo(null, false, type: SystemType.Entity, required: true)]
		public Project Project { get; set; }

		public long ProjectId { get; set; }


		[DisplayName("مدرک")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		public Document? Document { get; set; }

		public long? DocumentId { get; set; }

		/// <summary>
		/// شناسه‌های مدرک انتخاب‌شده در فرم (جدا شده با ویرگول) — فقط برای بایند UI، در دیتابیس ذخیره نمی‌شود.
		/// </summary>
		[NotMapped]
		public string? DocumentIds { get; set; }

		/// <summary>
		/// رزرو شماره بعدی پروژه بدون مدرک. فقط از فرم پست می‌شود.
		/// </summary>
		[NotMapped]
		public bool ReserveNumberOnly { get; set; }

		[DisplayName("شماره ترانسمیتال")]
		[DisplayInfo(null, false, type: SystemType.String)]
		public string? Number { get; set; }


		[DisplayName("شماره تعریف شده توسط کاربر")]
		[DisplayInfo(null, false, type: SystemType.String)]
		public string? DefinedByUserNumber { get; set; }


		[DisplayName("توضیحات")]
		[DisplayInfo(null, false, type: SystemType.String)]
		public string? Comments { get; set; }


		[DisplayName("مربوط به کارفرما")]
		[DisplayInfo(null, false, type: SystemType.Boolean)]
		public bool? EmployerRelated { get; set; } = false;


		[DisplayName("برای ReplySheet")]
		[DisplayInfo(null, false, type: SystemType.Boolean)]
		public bool? IsForReplySheet { get; set; } = false;
		


		[DisplayName("تاریخ ارسال به کارفرما")]
		[DisplayInfo(null, false, type: SystemType.DateShamsi)]
		public string? DeliveryDateToEmployer { get; set; }


		[DisplayName("پیوست")]
		[DisplayInfo(null, false, type: SystemType.File, fileTypes: ".rar,.zip,.pdf,.excel,.word", maxFileSize: 100)]
		public FileEntity? Attachment { get; set; }

		public long? AttachmentId { get; set; }

		/// <summary>
		/// شناسه ردیف Edms_Transmital_Document در TotalSystem / HTS برای همگام‌سازی یکتا
		/// (مدل جدید یک مدرک در هر ترانسمیتال دارد؛ دانهٔ HtsId همان Document-link است)
		/// </summary>
		public long HtsId { get; set; }

	}
}
