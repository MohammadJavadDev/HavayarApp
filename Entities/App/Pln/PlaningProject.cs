using Common.Attributes;
using Entities.App.Edms.Enums;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Pln
{
    [Display(Name = "پروژه")]
    [Table("Project", Schema = "Pln")]
    public class PlaningProject : BaseEntity
    {

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
}
