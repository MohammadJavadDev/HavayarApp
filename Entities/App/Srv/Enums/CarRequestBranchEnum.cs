using System.ComponentModel.DataAnnotations;

namespace Entities.App.Srv.Enums
{
	/// <summary>محل سرویس‌دهی درخواست خودرو — Gnr_Lookup (HTS Srv_CarRequest.BranchId).</summary>
	public enum CarRequestBranchEnum
	{
		[Display(Name = "دفتر مرکزی")]
		HeadOffice = 43,

		[Display(Name = "کارخانه")]
		Factory = 44
	}
}
