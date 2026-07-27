using System.ComponentModel.DataAnnotations;

namespace Entities.App.Sale.Enums
{
    public enum ProductTypeEnum
    {
        [Display(Name = "هوایاری")]
        Havayari = 1,

        [Display(Name = "غیر هوایاری")]
        NonHavayari = 2,
    }


    public enum ServiceTypesEnum
    {
        [Display(Name = "مصرف پروژه")]
        ProjectConsumption = 1,

        [Display(Name = "گارانتی")]
        Warranty = 2,

        [Display(Name = "وارانتی")]
        Warrantee = 3,

        [Display(Name = "گارانتی مشروط")]
        ConditionalWarranty = 4,

        [Display(Name = "تخفیفی")]
        Discounted = 5,

        [Display(Name = "مصرف پروژه گارانتی")]
        ProjectWarrantyConsumption = 6
    }

    public enum DamagedPartTypeEnum
    {
        [Display(Name = "سالم")]
        Healthy = 1,

        [Display(Name = "معیوب")]
        Defective = 2
    }


}
