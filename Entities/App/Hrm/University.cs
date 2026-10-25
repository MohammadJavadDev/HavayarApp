using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Hrm
{
	/// <summary>دانشگاه — معادل HTS <c>Training_University</c>. صفحه مدیریت جدا ندارد.</summary>
	[Display(Name = "دانشگاه")]
	[Table("University", Schema = "Hrm")]
	public class University : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(100)]
		public string? Title { get; set; }
	}

	public class UniversityConfiguration : IEntityTypeConfiguration<University>
	{
		public void Configure(EntityTypeBuilder<University> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Hrm_University_HtsId");
		}
	}
}
