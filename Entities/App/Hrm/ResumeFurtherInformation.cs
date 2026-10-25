using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Hrm
{
	/// <summary>پاسخ فرم کارجو. ویرایش صفحه مدیریت این ردیف‌ها را پاک نمی‌کند.</summary>
	[Display(Name = "اطلاعات تکمیلی فرم کارجو")]
	[Table("ResumeFurtherInformation", Schema = "Hrm")]
	public class ResumeFurtherInformation : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[ForeignKey(nameof(ResumeId))]
		public virtual Resume? Resume { get; set; }

		public long ResumeId { get; set; }

		[DisplayName("سؤال")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(256)]
		public string? QuestionTitle { get; set; }

		[DisplayName("پاسخ")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(64)]
		public string? Answer { get; set; }

		[DisplayName("توضیح")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(512)]
		public string? Explain { get; set; }

		[DisplayName("شناسه سؤال")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(64)]
		public string? IdentifierName { get; set; }
	}

	public class ResumeFurtherInformationConfiguration : IEntityTypeConfiguration<ResumeFurtherInformation>
	{
		public void Configure(EntityTypeBuilder<ResumeFurtherInformation> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Hrm_ResumeFurtherInformation_HtsId");
			builder.HasOne(x => x.Resume).WithMany(x => x.FurtherInformations).HasForeignKey(x => x.ResumeId).OnDelete(DeleteBehavior.Cascade);
		}
	}
}
