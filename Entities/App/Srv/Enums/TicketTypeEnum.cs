using System.ComponentModel.DataAnnotations;

namespace Entities.App.Srv.Enums
{
	/// <summary>نوع بلیط — Gnr_Lookup LookupType_FK = 197.</summary>
	public enum TicketTypeEnum
	{
		[Display(Name = "هواپیما")]
		Airplane = 1302,

		[Display(Name = "قطار")]
		Train = 1303,

		[Display(Name = "اتوبوس")]
		Bus = 1304
	}
}
