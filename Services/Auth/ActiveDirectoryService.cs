using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;

namespace Services.Auth;

public interface IActiveDirectoryService
{
	Task<bool> ValidateCredentialsAsync(string username, string password);
	Task<ADUserInfo?> GetUserInfoAsync(string username);
	ADUserInfo GetUserInfo(string username, string password);
	ADUserInfo GetUserByAdminUserInfo(string usernameAdmin, string passwordAdmin, string username);
	List<AdUserDto> GetAllUsers(string username, string password);
}

public class ADUserInfo
{
	public string Username { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string FullName { get; set; } = string.Empty;
	public string DisplayName { get; set; } = string.Empty;
	public string? Phone { get; set; }
	public byte[]? ProfileImage { get; set; }
}

public class AdUserDto
{
	public string? UserName { get; set; }
	public string? DisplayName { get; set; }
	public string? Email { get; set; }
	public string? Mobile { get; set; }
	public string? Department { get; set; }
	public string? Title { get; set; }
	public bool Enabled { get; set; }
	public string? DistinguishedName { get; set; }
}

public class ActiveDirectoryService : IActiveDirectoryService
{
	private readonly IConfiguration _configuration;
	private readonly ILogger<ActiveDirectoryService> _logger;

	public ActiveDirectoryService(IConfiguration configuration, ILogger<ActiveDirectoryService> logger)
	{
		_configuration = configuration;
		_logger = logger;
	}

	public async Task<bool> ValidateCredentialsAsync(string username, string password)
	{
		return await Task.Run(() =>
		{
			try
			{
				var domain = _configuration["ActiveDirectory:Domain"];

				using var context = new PrincipalContext(ContextType.Domain, domain);
				var res = context.ValidateCredentials(username, password);
				var userInfo = GetUserInfo(username, password);
				return res;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "خطا در اعتبارسنجی کاربر AD");
				return false;
			}
		});
	}

	public async Task<ADUserInfo?> GetUserInfoAsync(string username)
	{
		return await Task.Run(() =>
		{
			try
			{
				var domain = _configuration["ActiveDirectory:Domain"];

				using var context = new PrincipalContext(ContextType.Domain, domain);
				using var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);

				if (user == null)
					return null;

				return new ADUserInfo
				{
					Username = user.SamAccountName ?? string.Empty,
					Email = user.EmailAddress ?? string.Empty,
					FullName = user.DisplayName ?? string.Empty,
					DisplayName = user.Name ?? string.Empty
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "خطا در دریافت اطلاعات کاربر AD");
				return null;
			}
		});
	}

	public ADUserInfo GetUserInfo(string username, string password)
	{

		 
		var domain = _configuration["ActiveDirectory:Domain"];
		using var entry = new DirectoryEntry(
			$"LDAP://{domain}",
			$"{domain}\\{username}",
			password
		);

		using var searcher = new DirectorySearcher(entry);
		searcher.Filter = $"(sAMAccountName={username})";

		searcher.PropertiesToLoad.Add("displayName");
		searcher.PropertiesToLoad.Add("mail");
		searcher.PropertiesToLoad.Add("telephoneNumber");
		searcher.PropertiesToLoad.Add("thumbnailPhoto");

		var result = searcher.FindOne();
		if (result == null) return null;

		return new ADUserInfo
		{
			DisplayName = GetProp(result, "displayName"),
			Email = GetProp(result, "mail"),
			Phone = GetProp(result, "telephoneNumber"),
			ProfileImage = GetPhoto(result)
		};
	}
	public ADUserInfo GetUserByAdminUserInfo(string usernameAdmin, string passwordAdmin, string username)
	{


		var domain = _configuration["ActiveDirectory:Domain"];
		using var entry = new DirectoryEntry(
			$"LDAP://{domain}",
			$"{domain}\\{usernameAdmin}",
			passwordAdmin
		);

		using var searcher = new DirectorySearcher(entry);
		searcher.Filter = $"(sAMAccountName={username})";

		searcher.PropertiesToLoad.Add("displayName");
		searcher.PropertiesToLoad.Add("mail");
		searcher.PropertiesToLoad.Add("telephoneNumber");
		searcher.PropertiesToLoad.Add("thumbnailPhoto");

		var result = searcher.FindOne();
		if (result == null) return null;

		return new ADUserInfo
		{
			DisplayName = GetProp(result, "displayName"),
			Email = GetProp(result, "mail"),
			Phone = GetProp(result, "telephoneNumber"),
			ProfileImage = GetPhoto(result)
		};
	}
	public List<AdUserDto> GetAllUsers(string username, string password)
	{
		var domain = _configuration["ActiveDirectory:Domain"];
		var users = new List<AdUserDto>();
	 

		var ldapPath = $"LDAP://{domain}";

		using (var entry = new DirectoryEntry(ldapPath, username, password))
		using (var searcher = new DirectorySearcher(entry))
		{
			searcher.Filter = "(&(objectCategory=person)(objectClass=user))";
			searcher.PageSize = 1000;

			searcher.PropertiesToLoad.AddRange(new[]
			{
			 "samaccountname",
			 "displayname",
			 "mail",
			 "mobile",
			 "department",
			 "title",
			 "useraccountcontrol",
			 "distinguishedname"
		  });

			foreach (SearchResult result in searcher.FindAll())
			{
				users.Add(new AdUserDto
				{
					UserName = GetProp(result, "samaccountname"),
					DisplayName = GetProp(result, "displayname"),
					Email = GetProp(result, "mail"),
					Mobile = GetProp(result, "mobile"),
					Department = GetProp(result, "department"),
					Title = GetProp(result, "title"),
					Enabled = IsUserEnabled(result),
					DistinguishedName = GetProp(result, "distinguishedname")
				});
			}
		}

		return users;
	}

	private static string GetProp(SearchResult result, string prop)
	  => result.Properties.Contains(prop)
		 ? result.Properties[prop][0]?.ToString()
		 : null;
	private static bool IsUserEnabled(SearchResult result)
	{
		if (!result.Properties.Contains("useraccountcontrol"))
			return true;

		int flags = Convert.ToInt32(result.Properties["useraccountcontrol"][0]);
		return (flags & 2) == 0; // ACCOUNTDISABLE
	}

	byte[] GetPhoto(SearchResult result)
	{
		return result.Properties.Contains("thumbnailPhoto")
			? (byte[])result.Properties["thumbnailPhoto"][0]
			: null;
	}

}
