using Common.Auth.Enums;
using Entities.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Auth
{
	[Table(name: "RoleAccess", Schema = "system")]
	public class RoleAccess:BaseEntity
	{
		public string? Path { get; set; }
		public ActionAccessType ActionAccessType { get; set; }
		public ActionAccessItemType? ActionAccessItemType { get; set; }
		public string? EntityName { get; set; }
		public string? DisplayName { get; set; }
		public long? EntityId { get; set; }
		public long? RowId { get; set; }
		public long  RoleId { get; set; }
		public Role?  Role { get; set; }
	}

 
}
