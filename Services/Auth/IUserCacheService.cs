using Entities.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Auth
{
	public interface IUserCacheService
	{
		Task<UserRoleAccessesInfo> GetAsync(long userId);
		Task SetAsync(UserRoleAccessesInfo info);
		Task RemoveAsync(long userId);
		Task RefreshAsync(long userId, Func<Task<UserRoleAccessesInfo>> loader);
	}
}
