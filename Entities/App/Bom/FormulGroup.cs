using Common.Attributes;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Bom
{
	[Display(Name = "گروه بندی فرمول")]
	[Table("FormulGroup", Schema = "Bom")]
	public class FormulGroup : BaseEntity
	{
		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		public string Title { get; set; }


	}
}
