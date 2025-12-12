using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Auth
{
	public class UserRoleAccessesInfo
	{
		public long UserId { get; set; }
		public string Username { get; set; }
		public string ProfileUrl { get; set; }
		public string Name { get; set; }
		public DateTime LastLoginDate { get; set; }
		public string LastIp { get; set; } = string.Empty;
		public IReadOnlyList<string> Roles { get; set; }
		public IReadOnlyList<long> RoleIds { get; set; }
		public IReadOnlyList<RoleAccess> RoleAccesses { get; set; }
	}
}
