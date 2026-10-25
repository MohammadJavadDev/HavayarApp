using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Crm
{
	[Display(Name = "اقدام شکایت")]
	[Table("CustomerComplaintAction", Schema = "Crm")]
	public class CustomerComplaintAction : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		public long? ComplaintId { get; set; }
		public virtual CustomerComplaint? Complaint { get; set; }

		[DisplayName("تاریخ اقدام")]
		[DisplayInfo("ActionMiladiDate", true, SystemType.DateShamsi, required: true)]
		[MaxLength(10)]
		public string? ActionShamsiDate { get; set; }

		[DisplayName("تاریخ اقدام میلادی")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		[Column(TypeName = "date")]
		public DateTime? ActionMiladiDate { get; set; }

		[DisplayName("شرح اقدام")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(2048)]
		public string? ActionDescription { get; set; }

		[DisplayName("تاریخ اقدام بعدی")]
		[DisplayInfo("NextActionMiladiDate", true, SystemType.DateShamsi)]
		[MaxLength(10)]
		public string? NextActionShamsiDate { get; set; }

		[DisplayName("تاریخ اقدام بعدی میلادی")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		[Column(TypeName = "date")]
		public DateTime? NextActionMiladiDate { get; set; }

		[DisplayName("شرح اقدام بعدی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? NextActionDescription { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Comment { get; set; }
	}

	public class CustomerComplaintActionConfiguration : IEntityTypeConfiguration<CustomerComplaintAction>
	{
		public void Configure(EntityTypeBuilder<CustomerComplaintAction> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Crm_CustomerComplaintAction_HtsId");
		}
	}

	[Display(Name = "راه ارتباط شکایت")]
	[Table("CustomerComplaintContact", Schema = "Crm")]
	public class CustomerComplaintContact : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		public long? ComplaintId { get; set; }
		public virtual CustomerComplaint? Complaint { get; set; }

		[DisplayName("پیش‌شماره")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? PrePhoneNumber { get; set; }

		[DisplayName("تلفن")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? PhoneNumber { get; set; }

		[DisplayName("موبایل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? MobileNumber { get; set; }

		[DisplayName("نام پاسخگو")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(128)]
		public string? ResponserFullName { get; set; }

		[DisplayName("سمت پاسخگو")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? ResponserJob { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Comment { get; set; }
	}

	public class CustomerComplaintContactConfiguration : IEntityTypeConfiguration<CustomerComplaintContact>
	{
		public void Configure(EntityTypeBuilder<CustomerComplaintContact> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Crm_CustomerComplaintContact_HtsId");
		}
	}

	[Display(Name = "پیوست شکایت")]
	[Table("CustomerComplaintAttachment", Schema = "Crm")]
	public class CustomerComplaintAttachment : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		public long? ComplaintId { get; set; }
		public virtual CustomerComplaint? Complaint { get; set; }

		[DisplayName("فایل")]
		[DisplayInfo(null, true, type: SystemType.File, fileTypes: ".pdf,.zip,.rar,.png,.jpg,.jpeg,.doc,.docx,.xls,.xlsx,.txt", maxFileSize: 20)]
		[ForeignKey(nameof(FileId))]
		public virtual FileEntity? File { get; set; }

		public long? FileId { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Comment { get; set; }
	}

	public class CustomerComplaintAttachmentConfiguration : IEntityTypeConfiguration<CustomerComplaintAttachment>
	{
		public void Configure(EntityTypeBuilder<CustomerComplaintAttachment> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Crm_CustomerComplaintAttachment_HtsId");
			builder.HasOne(x => x.File).WithMany().HasForeignKey(x => x.FileId).OnDelete(DeleteBehavior.Restrict);
		}
	}

	[Display(Name = "اقدام نظرسنجی شکایت")]
	[Table("CustomerComplaintSurveyAction", Schema = "Crm")]
	public class CustomerComplaintSurveyAction : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		public long? ComplaintId { get; set; }
		public virtual CustomerComplaint? Complaint { get; set; }

		[DisplayName("تاریخ اقدام")]
		[MaxLength(16)]
		public string? ActionShamsiDateTime { get; set; }

		public DateTime? ActionMiladiDateTime { get; set; }

		[MaxLength(2048)]
		public string? ActionDescription { get; set; }

		[Column(TypeName = "date")]
		public DateTime? NextActionMiladiDate { get; set; }

		[MaxLength(16)]
		public string? NextActionShamsiDate { get; set; }

		[MaxLength(2048)]
		public string? NextActionDescription { get; set; }

		[MaxLength(2048)]
		public string? Comment { get; set; }
	}

	public class CustomerComplaintSurveyActionConfiguration : IEntityTypeConfiguration<CustomerComplaintSurveyAction>
	{
		public void Configure(EntityTypeBuilder<CustomerComplaintSurveyAction> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Crm_CustomerComplaintSurveyAction_HtsId");
			builder.HasOne(x => x.Complaint).WithMany().HasForeignKey(x => x.ComplaintId).OnDelete(DeleteBehavior.Cascade);
		}
	}

	[Display(Name = "نتیجه نظرسنجی شکایت")]
	[Table("CustomerComplaintSurveyResult", Schema = "Crm")]
	public class CustomerComplaintSurveyResult : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		public long? ComplaintId { get; set; }
		public virtual CustomerComplaint? Complaint { get; set; }

		public int QuestionHtsId { get; set; }
		public int AnswerHtsId { get; set; }
	}

	public class CustomerComplaintSurveyResultConfiguration : IEntityTypeConfiguration<CustomerComplaintSurveyResult>
	{
		public void Configure(EntityTypeBuilder<CustomerComplaintSurveyResult> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Crm_CustomerComplaintSurveyResult_HtsId");
			builder.HasOne(x => x.Complaint).WithMany().HasForeignKey(x => x.ComplaintId).OnDelete(DeleteBehavior.Cascade);
		}
	}
}
