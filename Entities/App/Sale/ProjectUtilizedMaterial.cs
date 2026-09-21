using Common.Attributes;
using Entities.App.FIN;
using Entities.App.Inv;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Sale
{
    [Display(Name = "اقلام مصرفی در پروژه")]
    [Table("ProjectUtilizedMaterial", Schema = "Sale")]
    public class ProjectUtilizedMaterial : BaseEntity
    {
        [DisplayName("تفصیل")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public virtual DL DL { get; set; }
        public long? DLId { get; set; }

        [DisplayName("سال")]
        [DisplayInfo(null, true, type: SystemType.Int)]
        public int Year { get; set; }

        [DisplayName("شماره سند")]
        [DisplayInfo(null, true, type: SystemType.Int)]
        public int? VchNum { get; set; }

        [DisplayName("تاریخ سند میلادی")]
        [DisplayInfo(null, true, type: SystemType.Date)]
        public DateTime? VchMiladiDate { get; set; } = null;

        [DisplayName("تاریخ سند")]
        [DisplayInfo(null, true, type: SystemType.DateShamsi)]
        public string? VchDateShamsiDate { get; set; } = null;

        [DisplayName("مقدار")]
        [DisplayInfo(null, true, type: SystemType.Decimal)]
        public double Qty { get; set; }

        [DisplayName("کالا")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public Part? Part { get; set; }
        public long? PartId { get; set; }

        [DisplayName("نوع تفصیل")]
        [DisplayInfo(null, false, type: SystemType.Entity)]
        public DLType? DLType { get; set; }
        public byte DLTypeId { get; set; }

        [DisplayName("شناسه HTS")]
        [DisplayInfo(null, false, type: SystemType.Long)]
        public long HtsId { get; set; }

        [DisplayName("شناسه قلم سند")]
        [DisplayInfo(null, false, type: SystemType.Int)]
        public int? VchItemId { get; set; }

        [DisplayName("شناسه هدر سند")]
        [DisplayInfo(null, false, type: SystemType.Int)]
        public int? VchHdrId { get; set; }

        [DisplayName("نوع رسید")]
        [DisplayInfo(null, false, type: SystemType.Int)]
        public byte? VchTypeId { get; set; }

        [DisplayName("کالای جایگزین")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        [ForeignKey(nameof(ReplacedPartId))]
        public virtual Part? ReplacedPart { get; set; }
        public long? ReplacedPartId { get; set; }

        [DisplayName("تعداد/مقدار جایگزین")]
        [DisplayInfo(null, true, type: SystemType.Decimal)]
        public double ReplacedQty { get; set; }

        [DisplayName("توضیحات")]
        [DisplayInfo(null, true, type: SystemType.String)]
        [MaxLength(2048)]
        public string? Comment { get; set; }
    }

    [Display(Name = "سوابق تغییرات اقلام مصرفی در پروژه")]
    [Table("ProjectUtilizedMaterialChange", Schema = "Sale")]
    public class ProjectUtilizedMaterialChange : BaseEntity
    {
        [DisplayName("شناسه HTS")]
        [DisplayInfo(null, false, type: SystemType.Long)]
        public long HtsId { get; set; }

        [DisplayName("قلم مصرفی")]
        [DisplayInfo(null, true, type: SystemType.Entity, required: true)]
        [ForeignKey(nameof(ProjectUtilizedMaterialId))]
        public virtual ProjectUtilizedMaterial? ProjectUtilizedMaterial { get; set; }
        public long? ProjectUtilizedMaterialId { get; set; }

        [DisplayName("توضیحات")]
        [DisplayInfo(null, true, type: SystemType.String)]
        [MaxLength(2048)]
        public string? Comment { get; set; }
    }

    public class ProjectUtilizedMaterialConfiguration : IEntityTypeConfiguration<ProjectUtilizedMaterial>
    {
        public void Configure(EntityTypeBuilder<ProjectUtilizedMaterial> builder)
        {
            builder.HasIndex(x => x.HtsId)
                .IsUnique()
                .HasFilter("[HtsId] <> CAST(0 AS bigint)")
                .HasDatabaseName("IX_Sale_ProjectUtilizedMaterial_HtsId");
        }
    }

    public class ProjectUtilizedMaterialChangeConfiguration : IEntityTypeConfiguration<ProjectUtilizedMaterialChange>
    {
        public void Configure(EntityTypeBuilder<ProjectUtilizedMaterialChange> builder)
        {
            builder.HasOne(x => x.ProjectUtilizedMaterial)
                .WithMany()
                .HasForeignKey(x => x.ProjectUtilizedMaterialId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.HtsId)
                .IsUnique()
                .HasFilter("[HtsId] <> CAST(0 AS bigint)")
                .HasDatabaseName("IX_Sale_ProjectUtilizedMaterialChange_HtsId");
        }
    }
}
