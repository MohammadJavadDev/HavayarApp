using System.ComponentModel.DataAnnotations;

namespace Entities.App.Srv.Enums
{
	/// <summary>وضعیت درخواست بلیط و هتل — Gnr_Lookup LookupType_FK = 196.</summary>
	public enum RequestStatusEnum
	{
		[Display(Name = "شخصی")]
		Personal = 1300,

		[Display(Name = "شرکتی")]
		Corporate = 1301
	}
}
