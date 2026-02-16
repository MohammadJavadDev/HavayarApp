using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Entities.Rahkaran.USR3
{
	[Table(name: "Sup_BuyCategory", Schema = "USR3")]

	public class RahkaranSup_BuyCategory

	{

		[Key]

		public long Sup_BuyCategoryID { get; set; }



		[MaxLength(255)]

		public string Title { get; set; }



		public long? PurchaseResponsibleRef { get; set; }



		public decimal? RoutineLeadTimeInDay { get; set; }



		public decimal? NonRoutineLeadTimeInDay { get; set; }



		public int? TypeRef { get; set; }



		public long Creator { get; set; }



		public DateTime CreationDate { get; set; }



		public long LastModifier { get; set; }



		public DateTime LastModificationDate { get; set; }



		public byte[]? Version { get; set; }



	}



	[Table(name: "Sup_BuyCategoryItems", Schema = "USR3")]

	public class RahkaranSup_BuyCategoryItems

	{

		[Key]

		public long Sup_BuyCategoryItemsID { get; set; }



		public long _MasterRef { get; set; }



		public long? Number { get; set; }



		public long? ProductIdRef { get; set; }



		[MaxLength(255)]

		public string? PartCode { get; set; }



		public decimal? TimeInWay { get; set; }



		public int? Supplier { get; set; }



		[MaxLength(255)]

		public string? Comments { get; set; }



		public long Creator { get; set; }



		public DateTime CreationDate { get; set; }



		public long LastModifier { get; set; }



		public DateTime LastModificationDate { get; set; }



		public byte[]? Version { get; set; }



	}
}
