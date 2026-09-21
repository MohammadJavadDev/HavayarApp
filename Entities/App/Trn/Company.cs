using Common.Attributes;
using Entities.App.Gnr;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Trn
{
	[Display(Name = "مشتری حقوقی / شرکت")]
	[Table("Company", Schema = "Trn")]
	public class Company : BaseEntity
	{
		[DisplayName("عنوان شرکت")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(256)]
		public string? Title { get; set; }

		[DisplayName("مدیرعامل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? CeoName { get; set; }

		[DisplayName("وب‌سایت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? Website { get; set; }

		[DisplayName("ایمیل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? Email { get; set; }

		[DisplayName("تلفن")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? Phone { get; set; }

		[DisplayName("فکس")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? Fax { get; set; }

		[DisplayName("آدرس")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? Address { get; set; }

		[DisplayName("نوع فعالیت")]
		[DisplayInfo(null, false, type: SystemType.String)]
		public string? ActivityKind { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, false, type: SystemType.String)]
		public string? Description { get; set; }

		// Optional link to the shared Party master (mirrors legacy optional ManCompanyId → Gnr_ManCompany).
		// Purely informational — no cascade, no sync, nullable.
		[DisplayName("طرف حساب مرتبط")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual Party? Party { get; set; }
		public long? PartyId { get; set; }

		public virtual ICollection<PhoneBook> PhoneBooks { get; set; } = new List<PhoneBook>();
		public virtual ICollection<KeyParticipant> KeyParticipants { get; set; } = new List<KeyParticipant>();
		public virtual ICollection<ConnectorParticipant> ConnectorParticipants { get; set; } = new List<ConnectorParticipant>();
		public virtual ICollection<TodoList> TodoLists { get; set; } = new List<TodoList>();
	}
}
