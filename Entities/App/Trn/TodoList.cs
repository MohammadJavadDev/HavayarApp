using Common.Attributes;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Trn
{
	[Display(Name = "تاریخچه مذاکرات / پیگیری")]
	[Table("TodoList", Schema = "Trn")]
	public class TodoList : BaseEntity
	{
		public long? CompanyId { get; set; }
		public virtual Company? Company { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(256)]
		public string? Title { get; set; }

		[DisplayName("شرح پیگیری")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		public string? Description { get; set; }

		// Standard Shamsi/Miladi pair (same shape as ServiceRequest.ContactShamsiDate/ContactMiladiDate):
		// Shamsi string bound via data-bind, Miladi DateTime bound via value="@Model?.TodoMiladiDate".
		[DisplayName("تاریخ پیگیری شمسی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? TodoShamsiDate { get; set; }

		[DisplayName("تاریخ پیگیری میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? TodoMiladiDate { get; set; }
	}
}
