using System.Net;
using System.Security.Claims;
using Common.Utilities;
using Microsoft.AspNetCore.Http;

namespace Data.SystemAuth;

public class Sdk : ISdk 
{
    private readonly IHttpContextAccessor _httpContextAccessor;
	private readonly IOnlineUserService _onlineUserService;
    public CurrentUser CurrentUser { get; private set; }
    public bool Authenticated { get; set; }
	public Sdk(IHttpContextAccessor httpContextAccessor ,
          IOnlineUserService onlineUserService)
    {
   
        _httpContextAccessor = httpContextAccessor;
		_onlineUserService = onlineUserService;
	   var userIdentity = _httpContextAccessor?.HttpContext?.User?.Identity;

        if(userIdentity != null && userIdentity.IsAuthenticated)
        {

            var onlineUser = onlineUserService.GetOnlineUser(userIdentity.GetUserId());

               if(onlineUser !=null)
               {
				CurrentUser = new CurrentUser
				{
					Id = userIdentity.GetUserId(),
					Username = onlineUser?.Username,
					ProfileImage = onlineUser?.ProfileImage,
					FullName = onlineUser?.FullName,
					IpAddress = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress,
					RoleAccess = onlineUser?.RoleAccesses ?? [],
					Roles = onlineUser?.Roles ?? [],
					RoleIds = onlineUser?.RoleIds,
					IsAdministrator = onlineUser?.Roles.Any(c => c == "admin") ?? false,
				};

				Authenticated = true;
			}
        }
    }

	public void RefreshUserRole()
	{
		_onlineUserService.RefreshUserRoleAccesses(CurrentUser.Id);
	}
}
public class CurrentUser
{
    public long Id { get; set; }
    public string? Username { get; set; }
    public string? FullName { get; set; }
    public string[] Roles { get; set; }
    public List<long> RoleIds { get; set; } = [];
    public string? ProfileImage { get; set; }
    public IPAddress? IpAddress { get; set; }
	public List<RoleAccessDto> RoleAccess { get; set; } = new();
     public bool IsAdministrator { get; set; } = false;
}