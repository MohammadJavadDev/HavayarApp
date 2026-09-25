using Common.Attributes;
using Entities.App.Inv;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Eng
{
	[Display(Name = "دسته بندی پارت لیست محصول")]
	[Table("PartListProductSection", Schema = "Eng")]
	public class PartListProductSection : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("محصول")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public Part? Product { get; set; }
		public long ProductId { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(255)]
		public string Title { get; set; } = string.Empty;

		[DisplayName("ترتیب")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public byte Order { get; set; }

		[DisplayName("تصویر")]
		[DisplayInfo(null, true, type: SystemType.File, fileTypes: ".jpg,.jpeg,.png")]
		public FileEntity? Picture { get; set; }
		public long? PictureId { get; set; }

		[DisplayName("مسیر تصویر")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(1024)]
		public string? PicturePath { get; set; }

		[DisplayName("تصویر کاور")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsCover { get; set; }

		[DisplayName("غیرفعال")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsDisabled { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? Comment { get; set; }

		public ICollection<PartListProductBom> ProductBoms { get; set; } = new List<PartListProductBom>();
		public ICollection<PartListDlBom> DlBoms { get; set; } = new List<PartListDlBom>();
	}

	public class PartListProductSectionConfiguration : IEntityTypeConfiguration<PartListProductSection>
	{
		public void Configure(EntityTypeBuilder<PartListProductSection> builder)
		{
			builder.HasIndex(x => x.HtsId)
				.IsUnique()
				.HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Eng_PartListProductSection_HtsId");

			builder.HasOne(x => x.Product)
				.WithMany()
				.HasForeignKey(x => x.ProductId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.Picture)
				.WithMany()
				.HasForeignKey(x => x.PictureId)
				.OnDelete(DeleteBehavior.SetNull);
		}
	}
}
