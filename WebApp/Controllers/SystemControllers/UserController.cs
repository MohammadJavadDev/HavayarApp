using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using WebApp.Framework.ViewAs;
using Data.Contracts;
using Data.Repositories;
using Data.SystemAuth;
using Entities.Auth;
using Entities.Base;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.AccessServices;
using Services.Auth;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using WebApp.Models;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.SystemControllers
{
	[ApiController]
	[ApiResultFilter]
	[Route("[controller]")]
	[ControllerInfoAttribute("کاربر", typeof(User))]
	[Authorize("AuthenticatedUser")]
	public class UserController(
			IUserService service,
		IUnitOfWork unitOfWork,
		IAccessMemoryStorage _accessMemoryStorage,
		IActiveDirectoryService activeDirectoryService,
		IOnlineUserService onlineUserService,
		IWebHostEnvironment env,
			IConfiguration config,
			IAuthService authService,
			ViewAsSessionStore viewAsSessions,
			Data.ApplicationDbContext dbContext) : BaseController
	{


		[HttpGet("/panel/{action}")]
		[ActionDisplayName("لیست کاربر ها", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ListUser()
		{
			return View("Views/Panel/System/User/List.cshtml");
		}

		[HttpGet("/panel/User/Edit")]
		[ActionDisplayName("ویرایش کاربر", ActionAccessType.View, ActionAccessItemType.Update)]
		public async Task<IActionResult> EditUser(long? id, CancellationToken cn)
		{

			if (id != null)
			{
				var user = await service.TableNoTracking.FirstOrDefaultAsync(t => t.Id == id, cn);
				return View("Views/Panel/System/User/Edit.cshtml", user);
			}
			return View("Views/Panel/System/User/Edit.cshtml");
		}

		[HttpGet("/panel/User/new")]
		[ActionDisplayName("کاربر جدید", ActionAccessType.View, ActionAccessItemType.Create)]
		public async Task<IActionResult> New(long? id, CancellationToken cn)
		{

			return View("Views/Panel/System/User/Edit.cshtml");
		}

		[HttpPost("/panel/{action}")]
		[ActionDisplayName("ذخیره کاربر ها", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> SaveUser(User user, CancellationToken cn)
		{
			if (!ModelState.IsValid)
			{

				return BadRequest(ModelState.GetModelStateErrors());
			}

			if (user.Id == null || user.Id == 0)
			{

				user = await service.CreateUserAsync(new()
				{
					Name = user.Name,
					Password = user.Password,
					Username = user.Username,
					ProfileUrl = user.ProfileUrl,
					Roles = user.Roles,
					AuthorizationType = user.AuthorizationType,
					RoleIds = user.RoleIds,
					Email = user.Email,
					FName = user.NameFa,
					PartyId = user.PartyId

				}, cn);
			}
			else
			{

				user = await service.AddAndUpdateUserAsync(new()
				{
					Id = user.Id,
					Name = user.Name,
					Password = user.Password,
					Username = user.Username,
					ProfileUrl = user.ProfileUrl,
					Roles = user.Roles,
					AuthorizationType = user.AuthorizationType,
					RoleIds = user.RoleIds,
					Email = user.Email,
					NameFa = user.NameFa,
					PartyId = user.PartyId
				}, cn);
			}

			return Ok(user);
		}

		 
		 

		[HttpGet("/panel/User/ImportFromAd")]
		[ActionDisplayName("دریافت کاربران از Active Directory", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ImportFromAd()
		{
			return View("Views/Panel/System/User/ImportFromAd.cshtml");
		}

		[HttpPost("/panel/User/GetAdUsers")]
		[ActionDisplayName("دریافت لیست کاربران AD", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public IActionResult GetAdUsers([FromBody] AdUserRequest request)
		{
			try
			{

				if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
				{
					return BadRequest(new { isSuccess = false, message = "نام کاربری و رمز عبور الزامی است." });
				}


				var adUsers = activeDirectoryService.GetAllUsers(request.Username, request.Password);

				// Check which users already exist in system
				var existingUsernames = service.TableNoTracking
					.Where(u => adUsers.Select(ad => ad.UserName).Where(un => un != null).Contains(u.Username))
					.Select(u => u.Username.ToLower())
					.ToList();

				var result = adUsers.Select(ad => new
				{
					ad.UserName,
					ad.DisplayName,
					ad.Email,
					ad.Mobile,
					ad.Department,
					ad.Title,
					ad.Enabled,
					ad.DistinguishedName,
					ExistsInSystem = !string.IsNullOrEmpty(ad.UserName) && existingUsernames.Contains(ad.UserName.ToLower()),
					SystemUserId = (!string.IsNullOrEmpty(ad.UserName) && existingUsernames.Contains(ad.UserName.ToLower()))
						? service.TableNoTracking.FirstOrDefault(u => u.Username.ToLower() == ad.UserName.ToLower())?.Id
						: null
				}).ToList();

				return Ok(new { isSuccess = true, data = result });
			}
			catch (Exception ex)
			{
				return BadRequest(new { isSuccess = false, message = $"خطا در دریافت کاربران: {ex.Message}" });
			}
		}

		[HttpPost("/panel/User/ImportAdUser")]
		[ActionDisplayName("افزودن کاربر از AD", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> ImportAdUser([FromBody] ImportAdUserRequest request, CancellationToken cn)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(request.AdUsername) || string.IsNullOrWhiteSpace(request.AdPassword))
				{
					return BadRequest(new { isSuccess = false, message = "نام کاربری و رمز عبور AD الزامی است." });
				}

				// Get user info from AD
				var adUserInfo = activeDirectoryService.GetUserByAdminUserInfo(request.AdUsername, request.AdPassword, request.Username);
				if (adUserInfo == null)
				{
					return BadRequest(new { isSuccess = false, message = "کاربر در Active Directory یافت نشد." });
				}

				// Check if user already exists
				var existingUser = await service.TableNoTracking
					.FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower(), cn);

				if (existingUser != null)
				{
					return BadRequest(new { isSuccess = false, message = "این کاربر قبلاً در سیستم ثبت شده است." });
				}

				// Save profile image if exists
				string? profileUrl = null;
				if (adUserInfo.ProfileImage != null && adUserInfo.ProfileImage.Length > 0)
				{

					var uploadsPath = config["Storage:ProfileImagesPath"]
				  ?? throw new Exception("Storage:ProfileImagesPath not configured");

					var _uploadsRoot = Path.IsPathRooted(uploadsPath)
						? uploadsPath
						: Path.Combine(env.ContentRootPath, uploadsPath);

					Directory.CreateDirectory(_uploadsRoot);

					var fileName = $"{request.Username}_{DateTime.Now:yyyyMMddHHmmss}.jpg";
					var folderPath = _uploadsRoot;
					if (!Directory.Exists(folderPath))
					{
						Directory.CreateDirectory(folderPath);
					}
					var filePath = Path.Combine(folderPath, fileName);
					await System.IO.File.WriteAllBytesAsync(filePath, adUserInfo.ProfileImage, cn);
					profileUrl = fileName;
				}

				// Create user
				var user = await service.CreateUserAsync(new()
				{
					Name = adUserInfo.DisplayName ?? request.Username,
					Username = request.Username,
					Password = "", // AD users don't need password stored
					ProfileUrl = profileUrl,
					Roles = request.Roles ?? new List<string>(),
					AuthorizationType = AuthorizationTypeEnum.ActiveDirectory,
					RoleIds = request.RoleIds ?? new List<long>(),
					Email = adUserInfo.Email,
				}, cn);

				return Ok(new { isSuccess = true, data = user, message = "کاربر با موفقیت افزوده شد." });
			}
			catch (Exception ex)
			{
				return BadRequest(new { isSuccess = false, message = $"خطا در افزودن کاربر: {ex.Message}" });
			}
		}

		[HttpPost("/panel/User/UpdateAdUser")]
		[ActionDisplayName("به‌روزرسانی کاربر از AD", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> UpdateAdUser([FromBody] UpdateAdUserRequest request, CancellationToken cn)
		{
			try
			{
				if (request.SystemUserId == null || request.SystemUserId == 0)
				{
					return BadRequest(new { isSuccess = false, message = "شناسه کاربر سیستم الزامی است." });
				}

				if (string.IsNullOrWhiteSpace(request.AdUsername) || string.IsNullOrWhiteSpace(request.AdPassword))
				{
					return BadRequest(new { isSuccess = false, message = "نام کاربری و رمز عبور AD الزامی است." });
				}

				// Get existing user
				var existingUser = await service.TableNoTracking
					.FirstOrDefaultAsync(u => u.Id == request.SystemUserId, cn);

				if (existingUser == null)
				{
					return BadRequest(new { isSuccess = false, message = "کاربر در سیستم یافت نشد." });
				}

				// Get updated info from AD
				var adUserInfo = activeDirectoryService.GetUserByAdminUserInfo(request.AdUsername, request.AdPassword, request.Username);
				if (adUserInfo == null)
				{
					return BadRequest(new { isSuccess = false, message = "کاربر در Active Directory یافت نشد." });
				}

				// Update profile image if exists
				string? profileUrl = existingUser.ProfileUrl;
				if (adUserInfo.ProfileImage != null && adUserInfo.ProfileImage.Length > 0)
				{

					var uploadsPath = config["Storage:ProfileImagesPath"]
						 ?? throw new Exception("Storage:ProfileImagesPath not configured");

					var _uploadsRoot = Path.IsPathRooted(uploadsPath)
						? uploadsPath
						: Path.Combine(env.ContentRootPath, uploadsPath);

					Directory.CreateDirectory(_uploadsRoot);

					var fileName = $"{request.Username}_{DateTime.Now:yyyyMMddHHmmss}.jpg";
					var folderPath = _uploadsRoot;
					if (!Directory.Exists(folderPath))
					{
						Directory.CreateDirectory(folderPath);
					}
					var filePath = Path.Combine(folderPath, fileName);
					await System.IO.File.WriteAllBytesAsync(filePath, adUserInfo.ProfileImage, cn);
					profileUrl = fileName;
				}

				// Update user
				var user = await service.AddAndUpdateUserAsync(new()
				{
					Id = request.SystemUserId,
					Name = adUserInfo.DisplayName ?? existingUser.Name,
					Username = existingUser.Username, // Don't change username
					Password = existingUser.Password, // Keep existing password
					ProfileUrl = profileUrl,
					Roles = request.Roles ?? existingUser.Roles,
					AuthorizationType = AuthorizationTypeEnum.ActiveDirectory,
					RoleIds = request.RoleIds ?? existingUser.RoleIds,
					Email = adUserInfo.Email,
					NameFa = existingUser.NameFa,
					PartyId = existingUser.PartyId,
				}, cn);

				return Ok(new { isSuccess = true, data = user, message = "کاربر با موفقیت به‌روزرسانی شد." });
			}
			catch (Exception ex)
			{
				return BadRequest(new { isSuccess = false, message = $"خطا در به‌روزرسانی کاربر: {ex.Message}" });
			}
		}

		[HttpPost("/panel/User/StartViewAs")]
		[ActionDisplayName("مشاهده پنل با دسترسی کاربر", ActionAccessType.View, ActionAccessItemType.Custom)]
		public IActionResult StartViewAs(StartViewAsRequest request)
		{
			if (request == null || request.Id <= 0)
				return BadRequest("کاربر مشخص نیست.");

			var adminId = User.Identity?.GetUserId() ?? 0;
			if (adminId <= 0)
				return BadRequest("نشست شما معتبر نیست.");

			var user = service.TableNoTracking.FirstOrDefault(u => u.Id == request.Id);
			if (user == null)
				return BadRequest("کاربر پیدا نشد.");

			if (user.IsActive != IsActiveEnum.Active)
				return BadRequest("این کاربر غیرفعال است و پنل برای او باز نمی‌شود.");

			onlineUserService.RefreshUserRoleAccesses(request.Id);

			var token = authService.GenerateToken(user);
			var targetName = string.IsNullOrWhiteSpace(user.NameFa) ? user.Name : user.NameFa;
			var session = viewAsSessions.Create(adminId, request.Id, targetName ?? user.Username, token);

			return Ok(new { url = "/?viewAs=" + session.Id, name = session.TargetName });
		}

		[HttpPost("/panel/User/CopyAccess")]
		[ActionDisplayName("ایجاد دسترسی مشابه کاربر دیگر", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> CopyAccess(CopyUserAccessRequest request, CancellationToken cn)
		{
			if (request == null || request.TargetUserId <= 0 || request.SourceUserId <= 0)
				return BadRequest("کاربر مقصد یا مبدأ مشخص نیست.");
			if (request.TargetUserId == request.SourceUserId)
				return BadRequest("کاربر مبدأ و مقصد نمی‌توانند یکسان باشند.");

			var target = await service.TableNoTracking.FirstOrDefaultAsync(u => u.Id == request.TargetUserId, cn);
			if (target == null)
				return BadRequest("کاربر مقصد یافت نشد.");

			var source = await service.TableNoTracking.FirstOrDefaultAsync(u => u.Id == request.SourceUserId, cn);
			if (source == null)
				return BadRequest("کاربر مبدأ یافت نشد.");

			var sourceIds = (source.RoleIds ?? []).Distinct().ToList();
			var sourceNames = (source.Roles ?? []).ToList();

			// نام نقش‌ها را از جدول نقش بگیر تا شناسه و نام همیشه هم‌خوان باشند
			var roles = await dbContext.Set<Role>().AsNoTracking()
				.Where(r => sourceIds.Contains(r.Id.Value) || sourceNames.Contains(r.Name))
				.Select(r => new { Id = r.Id.Value, r.Name })
				.ToListAsync(cn);

			var roleIds = request.RemoveExisting ? new List<long>() : (target.RoleIds ?? []).Distinct().ToList();
			var roleNames = request.RemoveExisting ? new List<string>() : (target.Roles ?? []).ToList();

			var addedCount = 0;
			foreach (var role in roles)
			{
				var idExists = roleIds.Contains(role.Id);
				var nameExists = roleNames.Contains(role.Name, StringComparer.OrdinalIgnoreCase);
				if (!idExists) { roleIds.Add(role.Id); }
				if (!nameExists) { roleNames.Add(role.Name); }
				if (!idExists || !nameExists) addedCount++;
			}

			await service.AddAndUpdateUserAsync(new()
			{
				Id = target.Id,
				Name = target.Name,
				Password = target.Password,
				Username = target.Username,
				ProfileUrl = target.ProfileUrl,
				Roles = roleNames,
				AuthorizationType = target.AuthorizationType,
				RoleIds = roleIds,
				Email = target.Email,
				NameFa = target.NameFa,
				PartyId = target.PartyId,
			}, cn);

			try { await onlineUserService.RefreshUserRoleAccessesAsync(target.Id.Value, cn); }
			catch { /* با ورود بعدی کاربر کش تازه می‌شود */ }

			return Ok(new
			{
				message = $"دسترسی‌ها اعمال شد. {addedCount} نقش جدید اضافه شد.",
				roleIds,
				roles = roleNames
			});
		}

		[HttpGet("/panel/User/OnlineUsers")]
		[ActionDisplayName("کاربران آنلاین", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult OnlineUsers()
		{
			return View("Views/Panel/System/User/OnlineUsers.cshtml");
		}

		[HttpGet("/panel/User/GetOnlineUsers")]
		[ActionDisplayName("دریافت لیست کاربران آنلاین", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetOnlineUsers(CancellationToken cn)
		{
			try
			{
				var onlineUsers = await onlineUserService.GetAllOnlineUsersAsync(cn);
				return Ok(onlineUsers);
			}
			catch (Exception ex)
			{
				return BadRequest($"خطا در دریافت کاربران آنلاین: {ex.Message}");
			}
		}
	}

	public class CopyUserAccessRequest
	{
		public long TargetUserId { get; set; }
		public long SourceUserId { get; set; }
		public bool RemoveExisting { get; set; }
	}

	public class StartViewAsRequest
	{
		public long Id { get; set; }
	}

	public class AdUserRequest
	{
		public string Username { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;
	}

	public class ImportAdUserRequest
	{
		public string AdUsername { get; set; } = string.Empty;
		public string AdPassword { get; set; } = string.Empty;
		public string Username { get; set; } = string.Empty;
		public List<string>? Roles { get; set; }
		public List<long>? RoleIds { get; set; }
	}

	public class UpdateAdUserRequest
	{
		public long? SystemUserId { get; set; }
		public string AdUsername { get; set; } = string.Empty;
		public string AdPassword { get; set; } = string.Empty;
		public string Username { get; set; } = string.Empty;
		public List<string>? Roles { get; set; }
		public List<long>? RoleIds { get; set; }
	}

}


