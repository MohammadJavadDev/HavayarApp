using System.ComponentModel.DataAnnotations;

namespace Entities.App.Srv.Enums
{
	/// <summary>سرویس بیرونی خودرو — Gnr_Lookup (HTS Srv_CarRequest.OutdoorCarServiceId).</summary>
	public enum OutdoorCarServiceEnum
	{
		[Display(Name = "سرویس اینترنتی")]
		InternetService = 1042,

		[Display(Name = "سرویس محلی")]
		LocalService = 1043
	}
}
