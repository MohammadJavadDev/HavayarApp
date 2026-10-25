using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Hrm
{
	/// <summary>نرم‌افزار — معادل HTS <c>Training_Software</c>. صفحه مدیریت جدا ندارد.</summary>
	[Display(Name = "نرم‌افزار")]
	[Table("TrainingSoftware", Schema = "Hrm")]
	public class TrainingSoftware : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(256)]
		public string? Title { get; set; }
	}

	public class TrainingSoftwareConfiguration : IEntityTypeConfiguration<TrainingSoftware>
	{
		public void Configure(EntityTypeBuilder<TrainingSoftware> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Hrm_TrainingSoftware_HtsId");
		}
	}
}
