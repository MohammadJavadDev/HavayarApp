using Common.Attributes;
using Entities.Auth;
using Entities.Base.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Base.Notification
{
	[Display(Name = "اعلان")]
	[Table(name: "Notification", Schema = "system")]
	public class Notification : BaseEntity
	{
		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		public string Title { get; set; }
		[DisplayName("بدنه")]
		[DisplayInfo(null, true, type: SystemType.String, required: false, showInRelationData: false)]

		public string? Body { get; set; }

		[DisplayName("موجودیت مربوطه")]
		[DisplayInfo(null, true, type: SystemType.Long)]

		public long? EntityId { get; set; }

		[DisplayName("آدرس صفحه")]
		[DisplayInfo(null, true, type: SystemType.String)]

		public string? ViewPath { get; set; }
		public bool IsRead { get; set; } = false;
	
		public long OwnerId { get; set; }
 
		public User Owner {  get; set; }

		public NotificationType Type { get; set; } = NotificationType.Appliaction;
		public bool IsSend { get; set; } = false;
		public DateTime? SendDateTime { get; set; }
		public List<string>? ToEmails { get; set; }
		public List<string>? CcEmails { get; set; }

		public Notification Clone()
		{
			return (Notification)this.MemberwiseClone();
		}

	}

}
