using Common.Attributes;
using Entities.App.Cng.Enums;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Cng
{
	/// <summary>گزارش کار CNG — معادل HTS <c>Cng_WorkReport</c>.</summary>
	[Display(Name = "گزارش کار")]
	[Table("WorkReport", Schema = "Cng")]
	public class WorkReport : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("جایگاه")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(StationInfoId))]
		public virtual StationInfo? Station { get; set; }
		public long StationInfoId { get; set; }

		[DisplayName("نوع درخواست")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public WorkReportRequestTypeEnum RequestTypeId { get; set; }

		[DisplayName("نوع خدمات")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public WorkReportServiceTypeEnum? ServiceTypeId { get; set; }

		[DisplayName("شماره گزارش")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? ReportNumber { get; set; }

		[DisplayName("تاریخ گزارش میلادی")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime ReportMiladiDate { get; set; }

		[DisplayName("تاریخ گزارش")]
		[MaxLength(10)]
		[DisplayInfo("ReportMiladiDate", true, type: SystemType.DateShamsi, required: true)]
		public string? ReportShamsiDate { get; set; }

		[DisplayName("ساعت کارکرد")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? WorkingHours { get; set; }

		[DisplayName("ساعت کارکرد متنی")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(10)]
		public string? WorkingHoursInText { get; set; }

		[DisplayName("مقدار Coss تتا")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? CossTetaValue { get; set; }

		[DisplayName("نوع روغن مصرفی")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public WorkReportConsumedOilTypeEnum? ConsumedOilTypeId { get; set; }

		/// <summary>ستون گرید — بدون اینپوت در فرم (دیتای HTS همیشه false/خالی).</summary>
		[DisplayName("تایید جایگاه دار")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsApprovedByOwner { get; set; }

		[DisplayName("گزارش حضور در جایگاه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? FailureExpertComment { get; set; }

		/// <summary>ستون گرید — بدون اینپوت در فرم.</summary>
		[DisplayName("توضیحات مالک")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? OwnerComment { get; set; }

		[DisplayName("مسیر فایل گزارش کار")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(256)]
		public string? WorkReportFileAddress { get; set; }

		[DisplayName("نوع فایل گزارش کار")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(10)]
		public string? WorkReportFileType { get; set; }

		[DisplayName("فایل")]
		[DisplayInfo(null, true, type: SystemType.File)]
		public long? FileId { get; set; }
		public virtual FileEntity? File { get; set; }

		/// <summary>ستون داده / گرید — بدون دکمه ارسال رضایت‌سنجی.</summary>
		[DisplayName("لینک ارسال شده")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? IsSurveyLinkSended { get; set; }

		[DisplayName("تاریخ ارسال لینک")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(16)]
		public string? SurveyLinkSendedDateTime { get; set; }

		/// <summary>ستون HTS Comment — در دیتا خالی؛ فیلد فرم ندارد.</summary>
		[DisplayName("کامنت")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Comment { get; set; }

		/// <summary>نام نماینده جایگاه — فقط فرم؛ روی StationInfo ذخیره می‌شود.</summary>
		[NotMapped]
		[DisplayName("عنوان و سمت نماینده")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		public string? RepresentativeName { get; set; }

		[NotMapped]
		[DisplayName("شماره موبایل نماینده")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		public string? RepresentativeMobileNumber { get; set; }

		[NotMapped]
		[DisplayName("آدرس ایمیل نماینده")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? RepresentativeEmailAddress { get; set; }

		/// <summary>اقلام مصرفی — فقط با ذخیره والد نوشته می‌شوند.</summary>
		[NotMapped]
		[DisplayInfo(null, false, type: SystemType.ListEntity)]
		public List<WorkReportConsumedPart>? ConsumedParts { get; set; }

		/// <summary>پارامترهای فنی و خطاها — فقط با ذخیره والد نوشته می‌شوند.</summary>
		[NotMapped]
		[DisplayInfo(null, false, type: SystemType.ListEntity)]
		public List<WorkReportFailure>? Failures { get; set; }
	}

	public class WorkReportConfiguration : IEntityTypeConfiguration<WorkReport>
	{
		public void Configure(EntityTypeBuilder<WorkReport> builder)
		{
			builder.Ignore(x => x.RepresentativeName);
			builder.Ignore(x => x.RepresentativeMobileNumber);
			builder.Ignore(x => x.RepresentativeEmailAddress);
			builder.Ignore(x => x.ConsumedParts);
			builder.Ignore(x => x.Failures);

			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Cng_WorkReport_HtsId");

			builder.HasOne(x => x.Station)
				.WithMany()
				.HasForeignKey(x => x.StationInfoId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.File)
				.WithMany()
				.HasForeignKey(x => x.FileId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}
}
