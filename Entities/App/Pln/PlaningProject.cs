using Common.Attributes;
using Entities.App.Edms.Enums;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Pln
{
    [Display(Name = "پروژه")]
    [Table("Project", Schema = "Pln")]
    public class PlaningProject : BaseEntity
    {

        [DisplayName("شناسه HTS")]
        [DisplayInfo(null, false, type: SystemType.Long)]
        public long HtsId { get; set; }

        [DisplayName("کد پروژه")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? Code { get; set; }

        [DisplayName("نام پروژه ")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? Name { get; set; }


        [DisplayName("تاریخ شروع پروژه شمسی")]
        [DisplayInfo(null, true, type: SystemType.DateShamsi)]
        public string? ProjectStartShamsiDate { get; set; } = null;


        [DisplayName("تاریخ شروع پروژه میلادی")]
        [DisplayInfo(null, true, type: SystemType.Date)]
        public DateTime? ProjectStartMiladiDate { get; set; } = null;


        [DisplayName("تاریخ پایان پروژه شمسی")]
        [DisplayInfo(null, true, type: SystemType.DateShamsi)]
        public string? ProjectEndShamsiDate { get; set; } = null;


        [DisplayName("تاریخ پایان پروژه میلادی")]
        [DisplayInfo(null, true, type: SystemType.Date)]
        public DateTime? ProjectEndMiladiDate { get; set; } = null;


        [DisplayName("توضیحات")]
        [DisplayInfo(null, true, type: SystemType.Select)]
        public string? Description { get; set; }


        [DisplayName("وضعیت")]
        [DisplayInfo(null, true, type: SystemType.Select)]
        public ProjectStatusEnum ProjectStatus { get; set; }

    }

    public class PlaningProjectConfiguration : IEntityTypeConfiguration<PlaningProject>
    {
        public void Configure(EntityTypeBuilder<PlaningProject> builder)
        {
            builder.HasIndex(x => x.HtsId)
                .IsUnique()
                .HasFilter("[HtsId] <> CAST(0 AS bigint)")
                .HasDatabaseName("IX_Pln_Project_HtsId");
        }
    }
}
