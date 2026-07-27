using System.ComponentModel.DataAnnotations;

namespace Entities.App.Pln.Enums
{
    public enum ProjectStatusEnum
    {
        [Display(Name = "عدد")]
        Number = 13,

        [Display(Name = "قطعه")]
        Piece = 1414,

        [Display(Name = "دستگاه")]
        Device = 1415,

        [Display(Name = "پرس")]
        Press = 1416,

        [Display(Name = "بشکه")]
        Barrel = 1417,

        [Display(Name = "گالن")]
        Gallon = 1418,

        [Display(Name = "پارچه")]
        Cloth = 1419,

        [Display(Name = "آیتم")]
        Item = 1420,

        [Display(Name = "سری")]
        Set = 1421,

        [Display(Name = "حلقه")]
        Ring = 1422,

        [Display(Name = "دست")]
        Hand = 1423,

        [Display(Name = "شاخه")]
        Branch = 1424,

        [Display(Name = "دهنه")]
        Nozzle = 1425,

        [Display(Name = "لیتر")]
        Liter = 1430,

        [Display(Name = "متر")]
        Meter = 1469,

        [Display(Name = "کیسه")]
        Bag = 1492,

        [Display(Name = "برگ")]
        Sheet = 1502,

        [Display(Name = "کیلوگرم")]
        Kilogram = 1519,

        [Display(Name = "کارتن")]
        Carton = 1534,

        [Display(Name = "پالت")]
        Pallet = 1764,

        [Display(Name = "متر مربع")]
        SquareMeter = 1765,

        [Display(Name = "گرم")]
        Gram = 2109
    }


}
