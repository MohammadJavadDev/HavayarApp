using Entities.Auth;
using Entities.Base.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Base.NotifitactionBuilder
{
	[Table("NotificationBuidler",Schema ="system")]
	public class NotificationBuidler: BaseEntity
	{
		public  string EntityFullName { get; set; } = string.Empty;
		public string Operation { get; set; } = string.Empty;
		public SystemOpertions SystemOpertion { get; set; }
		public string? ConditionsJson { get; set; }
		public string MessageTemplate { get; set; } = string.Empty;
		public string MessageTitle { get; set; } = string.Empty;
		public long UserId { get; set; }
		 
		public User? User { get; set; } 
	}
}
