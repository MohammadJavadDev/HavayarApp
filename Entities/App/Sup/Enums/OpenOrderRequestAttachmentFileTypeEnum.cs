using System.ComponentModel.DataAnnotations;

namespace Entities.App.Sup.Enums
{
    public enum OpenOrderRequestAttachmentFileTypeEnum
    {
		[Display(Name = "Data Sheet")]
		DataSheet = 264,

		[Display(Name = "Detail Drawing")]

		DetailDrawing = 265,

		[Display(Name = "P&ID")]

		PAndID = 266,

		[Display(Name = "Painting Procedure")]

		PaintingProcedure = 267,

		[Display(Name = "Hydrotest Procedure")]

		HydrotestProcedure = 268,

		[Display(Name = "NDT Procedure")]

		NDTProcedure = 269,

		[Display(Name = "ITP")]

		ITP = 270,

		[Display(Name = "Wiring Diagram")]

		WiringDiagram = 271,

		[Display(Name = "I/O List")]

		IOList = 272,

		[Display(Name = "Control Philosophy")]

		ControlPhilosophy = 273,

		[Display(Name = "Packing & Shipping Procedure")]

		PackingAndShippingProcedure = 274,

		[Display(Name = "O&M")]

		OAndM = 275,

		[Display(Name = "WPS-PQR")]

		WPSPQR = 276,

		[Display(Name = "Nameplate")]

		Nameplate = 307,

		[Display(Name = "Technical Documents")]

		TechnicalDocuments = 308,

		[Display(Name = "Performance Test")]

		PerformanceTest = 423,

		[Display(Name = "Part List Book")]

		PartListBook = 484,

		[Display(Name = "General Arrangement")]

		GeneralArrangement = 485,

		[Display(Name = "Sub Vendor List")]

		SubVendorList = 705,

		[Display(Name = "Spare Part List")]

		SparePartList = 860,

		[Display(Name = "BOM")]

		BOM = 1857,

		[Display(Name = "*** Catalog (Barcode) ***")]
 
		CatalogBarcode = 2747
	}
}