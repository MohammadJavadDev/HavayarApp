using System.ComponentModel.DataAnnotations;

namespace Entities.App.Cng.Enums
{
	/// <summary>وضعیت قرارداد تعمیر و نگهداشت CNG — Gnr_Lookup LookupType_FK = 281.</summary>
	public enum MaintenanceContractStatusEnum
	{
		[Display(Name = "جاری")]
		Current = 2085,

		[Display(Name = "اتمام قرارداد")]
		Finished = 2086
	}
}
