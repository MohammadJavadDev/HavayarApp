using System.ComponentModel.DataAnnotations;

namespace Entities.App.Srv.Enums
{
	/// <summary>پیک خارج از سازمان — Gnr_Lookup (HTS Srv_AmbassadorRequest.OutdoorAmbassadorServiceId).</summary>
	public enum OutdoorAmbassadorServiceEnum
	{
		[Display(Name = "سرویس اینترنتی")]
		InternetService = 1042,

		[Display(Name = "سرویس محلی")]
		LocalService = 1043
	}
}
