using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Qc
{
	[Display(Name = "ریز اقلام پارامتر اندازه گیری")]
	[Table("MeasurementParameterSubItem", Schema = "Qc")]
	public class MeasurementParameterSubItem : BaseEntity
	{
		public long MeasurementParameterItemId { get; set; }

		[ForeignKey(nameof(MeasurementParameterItemId))]
		public virtual MeasurementParameterItem? MeasurementParameterItem { get; set; }

		[DisplayName("عنوان")]
		[MaxLength(2048)]
		public string? Title { get; set; }
	}
}
