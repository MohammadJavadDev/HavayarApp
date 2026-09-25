using System.ComponentModel.DataAnnotations;

namespace Entities.App.Srv.Enums
{
	/// <summary>نوع درخواست PM — Gnr_Lookup type 159 (HTS Srv_PmRequest.TypeId).</summary>
	public enum PmRequestTypeEnum
	{
		[Display(Name = "تعویض")]
		Replacement = 1015,

		[Display(Name = "تعمیر")]
		Repair = 1016,

		[Display(Name = "نصب")]
		Install = 1017,

		[Display(Name = "بررسی")]
		Review = 1018,

		[Display(Name = "نقاشی")]
		Painting = 1076
	}
}
