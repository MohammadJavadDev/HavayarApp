using System.ComponentModel.DataAnnotations;

namespace Entities.App.Cng.Enums
{
	/// <summary>نوع روغن مصرفی گزارش کار CNG — Gnr_Lookup LookupType_FK = 222.</summary>
	public enum WorkReportConsumedOilTypeEnum
	{
		[Display(Name = "هوایاری")]
		Havayar = 1561,

		[Display(Name = "غیر هوایاری")]
		NonHavayar = 1562
	}
}
