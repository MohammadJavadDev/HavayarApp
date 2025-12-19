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
					var userInfo = GetUserInfo(username,password);
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
	string GetProp(SearchResult result, string prop)
	{
		return result.Properties.Contains(prop)
		    ? result.Properties[prop][0]?.ToString()
		    : null;
	}

	byte[] GetPhoto(SearchResult result)
	{
		return result.Properties.Contains("thumbnailPhoto")
		    ? (byte[])result.Properties["thumbnailPhoto"][0]
		    : null;
	}

}
