using System.ComponentModel.DataAnnotations;

namespace Entities.App.Edms.Enums
{
	public enum ProjectVpisDocTypeEnum
	{
		[Display(Name = "SCH")]
		SCH = 0,
		[Display(Name = "RPT")]
		RPT = 1,
		[Display(Name = "CHR")]
		CHR = 2,
		[Display(Name = "LST")]
		LST = 3,
		[Display(Name = "DWG")]
		Dwg = 4,
		[Display(Name = "DGM")]
		DGM = 5,
		[Display(Name = "DSN")]
		DSN = 6,
		[Display(Name = "PHL")]
		PHL = 7,
		[Display(Name = "CAL")]
		Cal = 8,
		[Display(Name = "DSH")]
		DSH = 9,
		[Display(Name = "BOM")]
		BOM = 10,
		[Display(Name = "PRC")]
		PRC = 11,
		[Display(Name = "ANL")]
		Anl = 12,
		[Display(Name = "SPC")]
		Spc = 13,
		[Display(Name = "CRT")]
		Crt = 14,
		[Display(Name = "MAN")]
		ManName = 15,
	}
}
