using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Hrm
{
	/// <summary>زبان — معادل HTS <c>Training_Language</c>. صفحه مدیریت جدا ندارد.</summary>
	[Display(Name = "زبان")]
	[Table("TrainingLanguage", Schema = "Hrm")]
	public class TrainingLanguage : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(128)]
		public string? Title { get; set; }
	}

	public class TrainingLanguageConfiguration : IEntityTypeConfiguration<TrainingLanguage>
	{
		public void Configure(EntityTypeBuilder<TrainingLanguage> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Hrm_TrainingLanguage_HtsId");
		}
	}
}
