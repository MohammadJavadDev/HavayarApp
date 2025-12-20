using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.App.Inv.Enums
{
	public enum PartDocumentTypeEnum
	{
		[Display(Name = "*** Catalog (Barcode) ***")]
		Catalog_Barcode = 2747,
		[Display(Name = "BOM")]
		BOM = 1857,
		[Display(Name = "Control Philosophy")]
		Control_Philosophy = 273,
		[Display(Name = "Data Sheet")]
		Data_Sheet = 264,
		[Display(Name = "Detail Drawing")]
		Detail_Drawing = 265,
		[Display(Name = "General Arrangement")]
		General_Arrangement = 485,
		[Display(Name = "Hydrotest Procedure")]
		Hydrotest_Procedure = 268,
		[Display(Name = "I/O List")]
		IO_List = 272,
		[Display(Name = "ITP")]
		ITP = 270,
		[Display(Name = "Nameplate")]
		Nameplate = 307,
		[Display(Name = "NDT Procedure")]
		NDT_Procedure = 269,
		[Display(Name = "O&M")]
		OM = 275,
		[Display(Name = "P&ID")]
		PID = 266,
		[Display(Name = "Packing & Shipping Procedure")]
		Packing__Shipping_Procedure = 274,
		[Display(Name = "Painting Procedure")]
		Painting_Procedure = 267,
		[Display(Name = "Part List Book")]
		Part_List_Book = 484,
		[Display(Name = "Performance Test")]
		Performance_Test = 423,
		[Display(Name = "Spare Part List")]
		Spare_Part_List = 860,
		[Display(Name = "Sub Vendor List")]
		Sub_Vendor_List = 705,
		[Display(Name = "Technical Documents")]
		Technical_Documents = 308,
		[Display(Name = "Wiring Diagram")]
		Wiring_Diagram = 271,
		[Display(Name = "WPS-PQR")]
		WPSPQR = 276,
	}
}
