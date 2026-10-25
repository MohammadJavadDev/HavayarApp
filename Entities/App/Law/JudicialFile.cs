using Common.Attributes;
using Entities.App.Hcm;
using Entities.App.Hrm;
using Entities.App.Law.Enums;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Law
{
	/// <summary>پرونده حقوقی (HTS Law_JudicialFile).</summary>
	[Display(Name = "پرونده حقوقی")]
	[Table("JudicialFile", Schema = "Law")]
	public class JudicialFile : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("کد پرونده")]
		[DisplayInfo(null, true, type: SystemType.String, showInRelationData: true)]
		[MaxLength(20)]
		public string? JudicialFileCode { get; set; }

		[DisplayName("ماهیت پرونده")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public JudicialFileNatureEnum? Nature { get; set; }

		[DisplayName("موضوع پرونده")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? Subject { get; set; }

		[DisplayName("نوع")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public JudicialFileTypeEnum? Type { get; set; }

		[DisplayName("شماره پرونده مرجع قضایی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? JudicialReferenceFileNumber { get; set; }

		[DisplayName("کلاسه بایگانی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? FileArchiveClass { get; set; }

		[DisplayName("واحد ارجاع")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual OrgUnit? ReferralUnit { get; set; }
		public long? ReferralUnitId { get; set; }

		[DisplayName("تاریخ ارجاع میلادی")]
		[DisplayInfo(null, false, SystemType.Date)]
		[Column(TypeName = "date")]
		public DateTime? ReferralDateMiladiDate { get; set; }

		[DisplayName("تاریخ ارجاع")]
		[MaxLength(20)]
		[DisplayInfo("ReferralDateMiladiDate", true, SystemType.DateShamsi)]
		public string? ReferralDateShamsiDate { get; set; }

		[DisplayName("کارشناس حقوقی")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual Personel? LegalExpert { get; set; }
		public long? LegalExpertId { get; set; }

		[DisplayName("شماره رهگیری")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? TrackingNumber { get; set; }

		[DisplayName("وکیل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? Lawyer { get; set; }

		[DisplayName("مختومه")]
		[DisplayInfo(null, false, type: SystemType.Boolean)]
		public bool? IsClosed { get; set; }

		[DisplayName("تاریخ ثبت در مرجع قضایی میلادی")]
		[DisplayInfo(null, false, SystemType.Date)]
		[Column(TypeName = "date")]
		public DateTime? RegistrationDateMiladiDate { get; set; }

		[DisplayName("تاریخ ثبت در مرجع قضایی")]
		[MaxLength(20)]
		[DisplayInfo("RegistrationDateMiladiDate", true, SystemType.DateShamsi)]
		public string? RegistrationDateShamsiDate { get; set; }

		[DisplayName("وضعیت فعلی")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public JudicialFileStatusEnum? CurrentStatus { get; set; }
	}

	public class JudicialFileConfiguration : IEntityTypeConfiguration<JudicialFile>
	{
		public void Configure(EntityTypeBuilder<JudicialFile> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Law_JudicialFile_HtsId");
			builder.HasIndex(x => x.JudicialFileCode).IsUnique()
				.HasFilter("[JudicialFileCode] IS NOT NULL")
				.HasDatabaseName("IX_Law_JudicialFile_JudicialFileCode");
			builder.HasOne(x => x.ReferralUnit).WithMany().HasForeignKey(x => x.ReferralUnitId)
				.OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.LegalExpert).WithMany().HasForeignKey(x => x.LegalExpertId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}

	/// <summary>خواسته (HTS Law_JudicialFileDemand).</summary>
	[Display(Name = "خواسته پرونده حقوقی")]
	[Table("JudicialFileDemand", Schema = "Law")]
	public class JudicialFileDemand : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("پرونده حقوقی")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual JudicialFile? JudicialFile { get; set; }
		public long? JudicialFileId { get; set; }

		[DisplayName("خواسته")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public JudicialDemandTypeEnum? DemandType { get; set; }

		[DisplayName("مبلغ خواسته (ریال)")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? DemandValue { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? Comment { get; set; }
	}

	public class JudicialFileDemandConfiguration : IEntityTypeConfiguration<JudicialFileDemand>
	{
		public void Configure(EntityTypeBuilder<JudicialFileDemand> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Law_JudicialFileDemand_HtsId");
			builder.HasOne(x => x.JudicialFile).WithMany().HasForeignKey(x => x.JudicialFileId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}

	/// <summary>طرفین پرونده (HTS Law_JudicialFileParties).</summary>
	[Display(Name = "طرف پرونده حقوقی")]
	[Table("JudicialFileParty", Schema = "Law")]
	public class JudicialFileParty : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("پرونده حقوقی")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual JudicialFile? JudicialFile { get; set; }
		public long? JudicialFileId { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public JudicialPartyTypeEnum? Type { get; set; }

		[DisplayName("نام و نام خانوادگی / نام شرکت")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(50)]
		public string? Fullname { get; set; }
	}

	public class JudicialFilePartyConfiguration : IEntityTypeConfiguration<JudicialFileParty>
	{
		public void Configure(EntityTypeBuilder<JudicialFileParty> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Law_JudicialFileParty_HtsId");
			builder.HasOne(x => x.JudicialFile).WithMany().HasForeignKey(x => x.JudicialFileId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}

	/// <summary>نتیجه (HTS Law_JudicialFileResult).</summary>
	[Display(Name = "نتیجه پرونده حقوقی")]
	[Table("JudicialFileResult", Schema = "Law")]
	public class JudicialFileResult : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("پرونده حقوقی")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual JudicialFile? JudicialFile { get; set; }
		public long? JudicialFileId { get; set; }

		[DisplayName("نتیجه")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public JudicialFinalResultEnum? Result { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? Comment { get; set; }
	}

	public class JudicialFileResultConfiguration : IEntityTypeConfiguration<JudicialFileResult>
	{
		public void Configure(EntityTypeBuilder<JudicialFileResult> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Law_JudicialFileResult_HtsId");
			builder.HasOne(x => x.JudicialFile).WithMany().HasForeignKey(x => x.JudicialFileId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}

	/// <summary>هزینه ها (HTS Law_JudicialFileCosts).</summary>
	[Display(Name = "هزینه پرونده حقوقی")]
	[Table("JudicialFileCost", Schema = "Law")]
	public class JudicialFileCost : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("پرونده حقوقی")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual JudicialFile? JudicialFile { get; set; }
		public long? JudicialFileId { get; set; }

		[DisplayName("نوع هزینه")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public JudicialCostTypeEnum? Type { get; set; }

		[DisplayName("مبلغ (ریال)")]
		[DisplayInfo(null, true, type: SystemType.Long, required: true)]
		public long? Value { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? Comment { get; set; }
	}

	public class JudicialFileCostConfiguration : IEntityTypeConfiguration<JudicialFileCost>
	{
		public void Configure(EntityTypeBuilder<JudicialFileCost> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Law_JudicialFileCost_HtsId");
			builder.HasOne(x => x.JudicialFile).WithMany().HasForeignKey(x => x.JudicialFileId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}

	/// <summary>کارهای مرتبط (HTS Law_JudicialFileRelatedWork).</summary>
	[Display(Name = "کار مرتبط پرونده حقوقی")]
	[Table("JudicialFileRelatedWork", Schema = "Law")]
	public class JudicialFileRelatedWork : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("پرونده حقوقی")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual JudicialFile? JudicialFile { get; set; }
		public long? JudicialFileId { get; set; }

		[DisplayName("متن")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(512)]
		public string? Text { get; set; }

		[DisplayName("تاریخ میلادی")]
		[DisplayInfo(null, false, SystemType.Date)]
		[Column(TypeName = "date")]
		public DateTime? DateMiladiDate { get; set; }

		[DisplayName("تاریخ")]
		[MaxLength(20)]
		[DisplayInfo("DateMiladiDate", true, SystemType.DateShamsi)]
		public string? DateShamsiDate { get; set; }
	}

	public class JudicialFileRelatedWorkConfiguration : IEntityTypeConfiguration<JudicialFileRelatedWork>
	{
		public void Configure(EntityTypeBuilder<JudicialFileRelatedWork> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Law_JudicialFileRelatedWork_HtsId");
			builder.HasOne(x => x.JudicialFile).WithMany().HasForeignKey(x => x.JudicialFileId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}

	/// <summary>مرجع رسیدگی (HTS Law_JudicialReviewAuthority).</summary>
	[Display(Name = "مرجع رسیدگی پرونده حقوقی")]
	[Table("JudicialReviewAuthority", Schema = "Law")]
	public class JudicialReviewAuthority : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("پرونده حقوقی")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual JudicialFile? JudicialFile { get; set; }
		public long? JudicialFileId { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(50)]
		public string? Title { get; set; }

		[DisplayName("پیوست")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(800)]
		public string? FileAttachment { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? Comment { get; set; }
	}

	public class JudicialReviewAuthorityConfiguration : IEntityTypeConfiguration<JudicialReviewAuthority>
	{
		public void Configure(EntityTypeBuilder<JudicialReviewAuthority> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Law_JudicialReviewAuthority_HtsId");
			builder.HasOne(x => x.JudicialFile).WithMany().HasForeignKey(x => x.JudicialFileId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}

	/// <summary>تاریخ های مهم (HTS Law_JudicialImportantDates).</summary>
	[Display(Name = "تاریخ مهم پرونده حقوقی")]
	[Table("JudicialImportantDate", Schema = "Law")]
	public class JudicialImportantDate : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("پرونده حقوقی")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual JudicialFile? JudicialFile { get; set; }
		public long? JudicialFileId { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(50)]
		public string? Title { get; set; }

		[DisplayName("تاریخ میلادی")]
		[DisplayInfo(null, false, SystemType.Date)]
		[Column(TypeName = "date")]
		public DateTime? DateMiladiDate { get; set; }

		[DisplayName("تاریخ")]
		[MaxLength(20)]
		[DisplayInfo("DateMiladiDate", true, SystemType.DateShamsi, required: true)]
		public string? DateShamsiDate { get; set; }

		[DisplayName("ارسال شده")]
		[DisplayInfo(null, false, type: SystemType.Boolean)]
		public bool? IsSend { get; set; }

		[DisplayName("گیرندگان پیامک")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? Recipients { get; set; }
	}

	public class JudicialImportantDateConfiguration : IEntityTypeConfiguration<JudicialImportantDate>
	{
		public void Configure(EntityTypeBuilder<JudicialImportantDate> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Law_JudicialImportantDate_HtsId");
			builder.HasOne(x => x.JudicialFile).WithMany().HasForeignKey(x => x.JudicialFileId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}

	/// <summary>پیوست پرونده (HTS Law_JudicialFileAttachment).</summary>
	[Display(Name = "پیوست پرونده حقوقی")]
	[Table("JudicialFileAttachment", Schema = "Law")]
	public class JudicialFileAttachment : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("پرونده حقوقی")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual JudicialFile? JudicialFile { get; set; }
		public long? JudicialFileId { get; set; }

		[DisplayName("فایل")]
		[DisplayInfo(null, true, type: SystemType.File, fileTypes: ".pdf,.jpg,.jpeg,.png,.doc,.docx,.xls,.xlsx,.zip,.rar", maxFileSize: 20)]
		public virtual FileEntity? File { get; set; }
		public long? FileId { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(4000)]
		public string? Comment { get; set; }

		/// <summary>مسیر UNC قدیمی HTS؛ فقط برای جاب انتقال یک‌باره فایل (بعد از انتقال FileId پر می‌شود).</summary>
		[DisplayName("مسیر فایل HTS")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(2048)]
		public string? HtsFilePath { get; set; }
	}

	public class JudicialFileAttachmentConfiguration : IEntityTypeConfiguration<JudicialFileAttachment>
	{
		public void Configure(EntityTypeBuilder<JudicialFileAttachment> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Law_JudicialFileAttachment_HtsId");
			builder.HasOne(x => x.JudicialFile).WithMany().HasForeignKey(x => x.JudicialFileId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}
}
