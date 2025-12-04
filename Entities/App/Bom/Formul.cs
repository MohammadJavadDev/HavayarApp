using Common.Attributes;
using Entities.App.Inv;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Bom
{
	[Display(Name = "فرمول Bom")]
	[Table("Formul", Schema = "Bom")]
	public class Formul : BaseEntity
	{
		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		public string Title { get; set; }


		[DisplayName("قطعه")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		public Part? Part { get; set; }

		public long? PartId { get; set; }


		[DisplayName("گروه")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true, showInRelationData: true)]
		public FormulGroup Group { get; set; }

		public long GroupId { get; set; }
		 

		[DisplayName("اقلام")]
		[DisplayInfo(null, false, type: SystemType.ListEntity)]
		public List<FormulItem> Items { get; set; } = new();


	}

	[Display(Name = "افلام Bom")]
	[Table("FormulItem", Schema = "Bom")]
	public class FormulItem : BaseEntity
	{
		public long FormulId { get; set; }
		public Formul Formul { get; set; }

		[DisplayName("قطعه")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		public Part? Part { get; set; }

		public long? PartId { get; set; }


		[DisplayName("ضریب مصرفی")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int? UsingRate { get; set; }


	}

}
