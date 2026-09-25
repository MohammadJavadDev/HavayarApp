using System.ComponentModel.DataAnnotations;

namespace Entities.App.Srv.Enums
{
	/// <summary>نوع درخواست پیک — Gnr_Lookup (HTS Srv_AmbassadorRequest.TypeId).</summary>
	public enum AmbassadorRequestTypeEnum
	{
		[Display(Name = "عادی")]
		Normal = 1013,

		[Display(Name = "فوری")]
		Urgent = 1014
	}
}
