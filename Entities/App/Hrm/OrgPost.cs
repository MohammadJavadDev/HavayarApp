using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Hrm
{
	/// <summary>پست سازمانی — معادل HTS <c>HRM_OrgPost</c>. منبع جاری عنوان، راهکاران <c>HCM3.Post</c> است.</summary>
	[Display(Name = "پست سازمانی")]
	[Table("OrgPost", Schema = "Hrm")]
	public class OrgPost : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(500)]
		public string? Title { get; set; }

		/// <summary>معادل <c>HRM_OrgPost.Hamkaran_Post_FK</c> و <c>HCM3.Post.PostID</c>. پست دستی بدون کلید راهکاران null است.</summary>
		[DisplayName("شناسه راهکاران")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? HamkaranPostId { get; set; }

		[DisplayName("پست خارجی")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool ExternalPost { get; set; }
	}

	public class OrgPostConfiguration : IEntityTypeConfiguration<OrgPost>
	{
		public void Configure(EntityTypeBuilder<OrgPost> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Hrm_OrgPost_HtsId");
			builder.HasIndex(x => x.HamkaranPostId).IsUnique().HasFilter("[HamkaranPostId] IS NOT NULL")
				.HasDatabaseName("IX_Hrm_OrgPost_HamkaranPostId");
		}
	}
}
