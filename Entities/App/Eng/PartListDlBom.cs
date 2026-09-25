using Common.Attributes;
using Entities.App.FIN;
using Entities.App.Inv;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Eng
{
	[Display(Name = "پارت لیست مصرفی")]
	[Table("PartListDlBom", Schema = "Eng")]
	public class PartListDlBom : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("مرکز پروژه")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public DL? DL { get; set; }
		/// <summary>HTS Acc_DL id mapped to FIN.DL; null/absent means product-only snapshot (HTS DlId &lt;= 0).</summary>
		public long? DlId { get; set; }

		[DisplayName("محصول")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public Part? Product { get; set; }
		public long ProductId { get; set; }

		[DisplayName("دسته بندی")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public PartListProductSection? ProductSection { get; set; }
		public long? ProductSectionId { get; set; }

		[DisplayName("کالا")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public Part? Part { get; set; }
		public long PartId { get; set; }

		[DisplayName("مقدار")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal Qty { get; set; }

		[DisplayName("ترتیب")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short Order { get; set; }

		[DisplayName("موجود در پارت لیست")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsExistInOriginalBom { get; set; }

		[DisplayName("غیرفعال")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsIgnored { get; set; }

		[DisplayName("فقط موجود در پارت لیست")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsOnlyExistInOriginalBom { get; set; }

		[DisplayName("خوانده‌شده از کالای مشابه")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsReadFromSimilarPart { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? Comment { get; set; }
	}

	public class PartListDlBomConfiguration : IEntityTypeConfiguration<PartListDlBom>
	{
		public void Configure(EntityTypeBuilder<PartListDlBom> builder)
		{
			builder.HasIndex(x => x.HtsId)
				.IsUnique()
				.HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Eng_PartListDlBom_HtsId");

			builder.HasOne(x => x.DL)
				.WithMany()
				.HasForeignKey(x => x.DlId)
				.OnDelete(DeleteBehavior.Restrict)
				.IsRequired(false);

			builder.HasOne(x => x.Product)
				.WithMany()
				.HasForeignKey(x => x.ProductId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.ProductSection)
				.WithMany(x => x.DlBoms)
				.HasForeignKey(x => x.ProductSectionId)
				.OnDelete(DeleteBehavior.SetNull);

			builder.HasOne(x => x.Part)
				.WithMany()
				.HasForeignKey(x => x.PartId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.Property(x => x.Qty).HasPrecision(18, 4);
		}
	}
}
