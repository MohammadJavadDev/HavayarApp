using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Srv
{
	/// <summary>کامنت درخواست PM — معادل ردیف‌های <c>Srv_RequestComment</c> با <c>PmRequestId</c>.</summary>
	[Display(Name = "کامنت درخواست PM")]
	[Table("PmRequestComment", Schema = "Srv")]
	public class PmRequestComment : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("درخواست PM")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(PmRequestId))]
		public virtual PmRequest? PmRequest { get; set; }
		public long PmRequestId { get; set; }

		[DisplayName("کامنت")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(256)]
		public string Comment { get; set; } = "";

		[DisplayName("وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? StatusId { get; set; }

		[DisplayName("تاریخ تقریبی اجرا")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(20)]
		public string? ApproximateExecutionDate { get; set; }

		[DisplayName("تاریخ ایجاد")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(20)]
		public string? CreatedDateInText { get; set; }
	}

	public class PmRequestCommentConfiguration : IEntityTypeConfiguration<PmRequestComment>
	{
		public void Configure(EntityTypeBuilder<PmRequestComment> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Srv_PmRequestComment_HtsId");

			builder.HasOne(x => x.PmRequest)
				.WithMany(x => x.Comments)
				.HasForeignKey(x => x.PmRequestId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}
}
