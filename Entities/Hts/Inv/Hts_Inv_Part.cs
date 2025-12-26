using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Entities.Hts.Inv
{

	[Table("Inv_Part")]
	public class Hts_Inv_Part
    {

		[Key]
		public long Part_ID { get; set; }

		[Required]
		[StringLength(40)]
		public string? Part_Code { get; set; }

		[Required]
		[StringLength(510)]
		public string? Part_Name { get; set; }

		[StringLength(256)]
		public string? Part_Number { get; set; }

		[StringLength(512)]
		public string? LatinName { get; set; }

		public short? PartType_Fk { get; set; }

		public byte? PartUnit_FK { get; set; }

		[StringLength(800)]
		public string? Part_Comment { get; set; }

		public int? Company_FK { get; set; }

		public long? Hamkaran_Part_FK { get; set; }

		public long? Last_BuyPrice { get; set; }

		[StringLength(50)]
		public string? Last_BuyDate { get; set; }

		public int? Last_BuyCompany_FK { get; set; }

		public int? AccCtgryRef { get; set; }

		public bool? IsExternal { get; set; }

		public bool? IsRoutine { get; set; }

		public bool? IsActive { get; set; }

		public bool? Engineering_Accept_NotNeed { get; set; }

		public bool? IsActiveForDataSheet { get; set; }

		public short? DataSheetTypeId { get; set; }

		[StringLength(256)]
		public string? DataSheetBrand { get; set; }

		[StringLength(128)]
		public string? SparePartIds { get; set; }

		public int? ModelId { get; set; }

		[StringLength(128)]
		public string? ModelTitle { get; set; }

		public string? PartBookSection { get; set; }

		public bool? IsForInstrumentation { get; set; }

		public bool? IsInvisibleBom { get; set; }

		[StringLength(128)]
		public string? CodingTitle { get; set; }

		public bool DesignTypeIsRoutine { get; set; }

		public short? UpdatedUserId { get; set; }

		public DateTime? UpdatedDate { get; set; }

		[StringLength(16)]
		public string? UpdatedDateInText { get; set; }

		[StringLength(2048)]
		public string? CompanyNames { get; set; }

		public decimal? SaleRate { get; set; }

		public bool? NotNeedToAttachDocuments { get; set; }

		[StringLength(64)]
		public string? Brand { get; set; }

		[StringLength(64)]
		public string? Dimensions { get; set; }

		[StringLength(1024)]
		public string? CatalogDescription { get; set; }

		[StringLength(15)]
		public string? TaxId { get; set; }
	}
}
