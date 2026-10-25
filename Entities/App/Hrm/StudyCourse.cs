using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Hrm
{
	/// <summary>رشته تحصیلی — معادل HTS <c>HRM_StudyCourse</c>. شناسه همکاران از SGHRM یک‌بار منتقل شده و جاب جدید ندارد.</summary>
	[Display(Name = "رشته تحصیلی")]
	[Table("StudyCourse", Schema = "Hrm")]
	public class StudyCourse : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(400)]
		public string? Title { get; set; }

		/// <summary>معادل <c>HRM_StudyCourse.Hamkaran_StudyCourse_FK</c>. یکتا نیست؛ در داده قدیمی کد تکراری وجود دارد.</summary>
		[DisplayName("شناسه همکاران")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? HamkaranStudyCourseId { get; set; }
	}

	public class StudyCourseConfiguration : IEntityTypeConfiguration<StudyCourse>
	{
		public void Configure(EntityTypeBuilder<StudyCourse> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Hrm_StudyCourse_HtsId");
		}
	}
}
