using System.ComponentModel.DataAnnotations;

namespace Entities.App.Srv.Enums
{
	/// <summary>نوع کار درخواست پیک — Gnr_Lookup (HTS Srv_AmbassadorRequest.JobTypeId).</summary>
	public enum AmbassadorRequestJobTypeEnum
	{
		[Display(Name = "وصول مطالبات")]
		DebtCollection = 1044,

		[Display(Name = "امور اداری (عادی)")]
		AdministrativeNormal = 1045,

		[Display(Name = "امور اداری (خاص)")]
		AdministrativeSpecial = 1046
	}
}
