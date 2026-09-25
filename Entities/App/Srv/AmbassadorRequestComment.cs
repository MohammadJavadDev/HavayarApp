using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Srv
{
	/// <summary>کامنت درخواست پیک — معادل ردیف‌های <c>Srv_RequestComment</c> با <c>AmbassadorRequestId</c>.</summary>
	[Display(Name = "کامنت درخواست پیک")]
	[Table("AmbassadorRequestComment", Schema = "Srv")]
	public class AmbassadorRequestComment : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("درخواست پیک")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(AmbassadorRequestId))]
		public virtual AmbassadorRequest? AmbassadorRequest { get; set; }
		public long AmbassadorRequestId { get; set; }

		[DisplayName("کامنت")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(256)]
		public string Comment { get; set; } = "";

		[DisplayName("وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? StatusId { get; set; }

		[DisplayName("تاریخ ایجاد")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(20)]
		public string? CreatedDateInText { get; set; }
	}

	public class AmbassadorRequestCommentConfiguration : IEntityTypeConfiguration<AmbassadorRequestComment>
	{
		public void Configure(EntityTypeBuilder<AmbassadorRequestComment> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Srv_AmbassadorRequestComment_HtsId");

			builder.HasOne(x => x.AmbassadorRequest)
				.WithMany(x => x.Comments)
				.HasForeignKey(x => x.AmbassadorRequestId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}
}
