using Common.Attributes;
using Entities.App.Gnr.Enums;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Gnr
{
	[Display(Name = "مناطق")]
	[Table("Region", Schema = "Gnr")]
	public class Region : BaseEntity
	{
		[DisplayName("نام")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(128)]
		public string Name { get; set; }


		[DisplayName("نام انگیلیسی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(128)]
		public string? Title_EN { get; set; }


		[DisplayName("والد")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		public Region? Parent { get; set; }

		public long? ParentId { get; set; }


		[DisplayName("نوع")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public RegionTypeEnum Type { get; set; }


		[DisplayName("کد")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(64)]
		public string? Code { get; set; }


		[DisplayName("Abbreviation")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(32)]
		public string? Abbreviation { get; set; }


		[DisplayName("LocalCode")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(32)]
		public string? LocalCode { get; set; }


		[DisplayName("FinanceCode")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(32)]
		public string? FinanceCode { get; set; }

		public long? RahkaranId { get; set; }


	}
}
