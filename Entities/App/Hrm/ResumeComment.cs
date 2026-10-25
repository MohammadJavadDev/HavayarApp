using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Hrm
{
	[Display(Name = "توضیح رزومه")]
	[Table("ResumeComment", Schema = "Hrm")]
	public class ResumeComment : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[ForeignKey(nameof(ResumeId))]
		public virtual Resume? Resume { get; set; }

		public long ResumeId { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(128)]
		public string? Title { get; set; }

		[DisplayName("توضیح")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(256)]
		public string? Comment { get; set; }

		[DisplayName("شناسه کاربر HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? HtsCreatedUserId { get; set; }

		[DisplayName("تاریخ")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(20)]
		public string? CreatedDateTimeInText { get; set; }

		[DisplayName("شناسه وضعیت HTS")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int? HtsStatusId { get; set; }

		[DisplayName("وضعیت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(128)]
		public string? StatusTitle { get; set; }
	}

	public class ResumeCommentConfiguration : IEntityTypeConfiguration<ResumeComment>
	{
		public void Configure(EntityTypeBuilder<ResumeComment> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Hrm_ResumeComment_HtsId");
			builder.HasOne(x => x.Resume).WithMany(x => x.Comments).HasForeignKey(x => x.ResumeId).OnDelete(DeleteBehavior.Cascade);
		}
	}
}
