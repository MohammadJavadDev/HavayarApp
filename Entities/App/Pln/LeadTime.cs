using Common.Attributes;
using Entities.App.Inv;
using Entities.App.Pln.Enums;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Pln
{
	[Display(Name = "زمان در راه")]
	[Table("LeadTime", Schema = "Pln")]
	public class LeadTime : BaseEntity
	{
		[DisplayName("کالا")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Part? Part { get; set; }

		public long? PartId { get; set; }


		[DisplayName("زمان در راه (روز)")]
		[DisplayInfo(null, true, type: SystemType.Int, required: true)]
		public int LeadTimeDay { get; set; }


		/// <summary>
		/// زمان در راه برای درخواست‌های غیرروتین (HTS: Pln_LeadTime.NonRoutineLeadTimeInDay) — D17
		/// </summary>
		[DisplayName("زمان در راه غیرروتین (روز)")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? NonRoutineLeadTimeInDay { get; set; }


		[DisplayName("تامین کننده")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public LeadTimeSupplierEnum? Supplier { get; set; }


		[DisplayName("کامنت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? Comment { get; set; }


	}
}
