using Common.Attributes;
using Entities.App.Hrm;
using Entities.App.Inv.Enums;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Inv
{
	[Display(Name = "کالا")]
	[Table("Part", Schema = "Inv")]
	public class Part : BaseEntity
	{
		[DisplayName("نام")]
		[DisplayInfo("Name", true, type: SystemType.String, required: true, showInRelationData: true, regexInvalidError: "کاراکتر وارد شده غیر مجاز میباشد. ")]
		public string Name { get; set; }


		[DisplayName("کد")]
		[DisplayInfo("Code", true, type: SystemType.String, regexInvalidError: "کاراکتر وارد شده غیر مجاز میباشد. ")]
		public string? Code { get; set; }


		[DisplayName("عنوان لاتین")]
		[DisplayInfo("LatinTitle", true, type: SystemType.String, regexInvalidError: "کاراکتر وارد شده غیر مجاز میباشد. ")]
		public string? LatinTitle { get; set; }


		[DisplayName("شماره")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		public string? Number { get; set; }


		[DisplayName("نوع")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public PartTypeEnum? Type { get; set; }


		[DisplayName("واحد اندازه گیری")]
 
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public PartUnit? Unit { get; set; }

		public long? UnitId { get; set; }

 


		[DisplayName("خارجی است")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool Foreign { get; set; } = false;


		[DisplayName("روتین مهندسی")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool EngineeringRoutine { get; set; } = false;


		[DisplayName("نیاز به پیوست مدارک ندارد")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool DocumentsNotRequired { get; set; } = false;


		[DisplayName("ابزار دقیق")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool PreciseTool { get; set; } = false;


		[DisplayName("استفاده جهت دیتاشیت")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool DataSheetUsage { get; set; } = false;


		[DisplayName("دیتاشیت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public PartDataSheetEnum? DataSheet { get; set; }


		[DisplayName("برند در دیتاشیت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? BrandInDataSheet { get; set; }


		[DisplayName("قطعات یدکی")]
		[DisplayInfo(null, true, type: SystemType.ListEntity)]
		public List<PartSparePart> SpareParts { get; set; } = new();


		[DisplayName("برند")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(500)]
		public string? Brand { get; set; }


		[DisplayName("ابعاد")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(500)]
		public string? Dimensions { get; set; }


		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? Description { get; set; }

		public long? HamkaranId { get; set; }

		[DisplayName("مدارک")]
		[DisplayInfo(null, true, type: SystemType.ListEntity)]
		public List<PartDocument> Documents { get; set; } = new();


	}

	[Display(Name = "قطعات یدکی")]
	[Table("PartSparePart", Schema = "Inv")]
	public class PartSparePart : BaseEntity
	{
		public long PartId { get; set; }
		public Part Part { get; set; }

		[DisplayName("قطعه یدکی")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Part? SparePart { get; set; }

		public long? SparePartId { get; set; }


		[DisplayName("ساعت کارکرد")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? WorkingHours { get; set; }


		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? Description { get; set; }

	


	}


	[Display(Name = "مدارک")]
	[Table("PartDocument", Schema = "Inv")]
	public class PartDocument : BaseEntity
	{
		public long PartId { get; set; }
		public Part Part { get; set; }

		[DisplayName("اصلی")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? Main { get; set; } = false;


		[DisplayName("نوع")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public PartDocumentTypeEnum? Type { get; set; }


		[DisplayName("واحد سازمانی")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public OrgUnit? OrganizationUnit { get; set; }

		public long? OrganizationUnitId { get; set; }


		[DisplayName("کامنت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? Comment { get; set; }


		[DisplayName("پیوست")]
		[DisplayInfo(null, true, type: SystemType.File, required: true ,fileTypes: ".pdf,.zip,.png,.jpg")]
		public FileEntity Attachment { get; set; }

		public long AttachmentId { get; set; }


	}

	public class PartConfiguration : IEntityTypeConfiguration<Part>
	{
		public void Configure(EntityTypeBuilder<Part> builder)
		{
			builder.ToTable("Part", "Inv");

			builder.Property(x => x.Name)
				  .IsRequired();

			builder.HasMany(x => x.SpareParts)
				  .WithOne(x => x.Part)
				  .HasForeignKey(x => x.PartId)
				  .OnDelete(DeleteBehavior.Restrict);
		}
	}
	public class PartSparePartConfiguration : IEntityTypeConfiguration<PartSparePart>
	{
		public void Configure(EntityTypeBuilder<PartSparePart> builder)
		{
			builder.ToTable("PartSparePart", "Inv");



			builder.HasOne(x => x.Part)
				  .WithMany(p => p.SpareParts)
				  .HasForeignKey(x => x.PartId)
				  .OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.SparePart)
				  .WithMany()
				  .HasForeignKey(x => x.SparePartId)
				  .OnDelete(DeleteBehavior.Restrict);
		}
	}

}
