using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Inv
{
	[Display(Name = "دسته دفترچه قطعات")]
	[Table("PartBookSection", Schema = "Inv")]
	public class PartBookSection : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(255)]
		public string Title { get; set; } = string.Empty;

		[DisplayName("ترتیب")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int Order { get; set; }

		[DisplayName("عنوان تصویر")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(255)]
		public string? PictureTitle { get; set; }

		[DisplayName("تصویر")]
		[DisplayInfo(null, true, type: SystemType.File, fileTypes: ".jpg,.jpeg,.png")]
		public FileEntity? Picture { get; set; }
		public long? PictureId { get; set; }

		[DisplayName("برای BOM سفارشی")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsForCustomBom { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Comment { get; set; }

		public ICollection<PartPartBookSection> ProductLinks { get; set; } = new List<PartPartBookSection>();
	}

	public class PartBookSectionConfiguration : IEntityTypeConfiguration<PartBookSection>
	{
		public void Configure(EntityTypeBuilder<PartBookSection> builder)
		{
			builder.HasIndex(x => x.HtsId)
				.IsUnique()
				.HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Inv_PartBookSection_HtsId");

			builder.HasOne(x => x.Picture)
				.WithMany()
				.HasForeignKey(x => x.PictureId)
				.OnDelete(DeleteBehavior.SetNull);
		}
	}
}
