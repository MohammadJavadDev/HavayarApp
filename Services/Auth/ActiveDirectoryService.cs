using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;

namespace Services.Auth;
 
	public interface IActiveDirectoryService
	{
		Task<bool> ValidateCredentialsAsync(string username, string password);
		Task<ADUserInfo?> GetUserInfoAsync(string username);
	}

	public class ADUserInfo
	{
		public string Username { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string FullName { get; set; } = string.Empty;
		public string DisplayName { get; set; } = string.Empty;
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
					return context.ValidateCredentials(username, password);
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
	}
 