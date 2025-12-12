using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
	public class CacheKeys
	{
		public static string UserPermissions(long userId)
	  => $"cache:user:permissions:{userId}";
	}
}
