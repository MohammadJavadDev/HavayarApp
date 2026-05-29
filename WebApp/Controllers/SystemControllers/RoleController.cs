using Common.Attributes;
using Common.Auth.Enums;
using Common.Entities;
using Common.Utilities;
using Data;
using Data.Contracts;
using Data.Repositories;
using Entities.Auth;
using Entities.Base;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.AccessServices;
using Services.Auth;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.SystemControllers
{
	[ApiController]
	[ApiResultFilter]
	[Route("[controller]")]
	[ControllerInfoAttribute("نقش", typeof(Role))]
	[Authorize("AuthenticatedUser")]
	public class RoleController(
		IRoleMemoryStorage roleMemoryStorage,
		IUnitOfWork unitOfWork,
		ApplicationDbContext dbContext,
		IAccessMemoryStorage _accessMemoryStorage,
		IUserService userService) : BaseController
	{


		[HttpGet("/panel/{action}")]
		[ActionDisplayName("لیست نقش ها", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ListRole()
		{
			return View("Views/Panel/System/Role/List.cshtml");
		}

		[HttpGet("/panel/Role/Edit")]
		[ActionDisplayName("ویرایش نقش", ActionAccessType.View, ActionAccessItemType.Update)]
		public async Task<IActionResult> EditRole(long? id, CancellationToken cn)
		{

			ViewBag.ListEndPoints = _accessMemoryStorage.GetAllAccessControllers();
			if (id != null)
			{
				var role = await unitOfWork.Repository<Role>().TableNoTracking
					.Include(c => c.RoleAccesses)
					.FirstOrDefaultAsync(t => t.Id == id, cn);

				return View("Views/Panel/System/Role/Edit.cshtml", role);
			}
			return View("Views/Panel/System/Role/Edit.cshtml");
		}

		[HttpGet("/panel/Role/new")]
		[ActionDisplayName("نقش جدید", ActionAccessType.View, ActionAccessItemType.Create)]
		public async Task<IActionResult> New(long? id, CancellationToken cn)
		{

			ViewBag.ListEndPoints = _accessMemoryStorage.GetAllAccessControllers();

			return View("Views/Panel/System/Role/Edit.cshtml");
		}

	[HttpPost("/panel/{action}")]
	[ActionDisplayName("ذخیره نقش ها", ActionAccessType.Api, ActionAccessItemType.Save)]
	public async Task<IActionResult> SaveRole(Role role, CancellationToken cn)
	{

			await unitOfWork.Repository<RoleAccess>().DeleteWhereAsync(c=>c.RoleId == role.Id,cn);

			var res = await unitOfWork.Repository<Role>().SaveAsync(role, cn);

			var roles = unitOfWork.Repository<Role>().TableNoTracking
				    .Include(c => c.RoleAccesses).ToList();

			roleMemoryStorage.SetRoles(roles);

			return Ok(res);
	}

	[HttpGet("/panel/Role/{action}")]
	[ActionDisplayName("دریافت کاربران نقش", ActionAccessType.Api, ActionAccessItemType.FetchData)]
	public async Task<IActionResult> GetRoleUsers(long roleId, CancellationToken cn)
	{
			var users = await userService.GetUsersByRoleId(roleId);

		return Ok(users);
	}

	[HttpPost("{action}")]
	[ActionDisplayName("افزودن کاربر به نقش", ActionAccessType.Api, ActionAccessItemType.Update)]
	public async Task<IActionResult> AddUserToRole(RoleUserRequest ru ,CancellationToken cn)
	{
		var user = await userService
				.TableNoTracking
			.FirstOrDefaultAsync(u => u.Id == ru.UserId, cn);

		if (user == null)
			return NotFound("کاربر یافت نشد");

		var role = await dbContext.Set<Role>()
			.FirstOrDefaultAsync(u => u.Id == ru.RoleId, cn);

			if (user == null)
				return NotFound("نقش یافت نشد");

			if (!user.RoleIds.Contains(ru.RoleId))
		{
		     	user.RoleIds.Add(ru.RoleId);
			 user.Roles.Add(role.Name);
			 await userService.AddAndUpdateUserAsync(
				 new()
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
				 }

				 , cn);
		 
		}

		return Ok(new { message = "کاربر با موفقیت به نقش اضافه شد" });
	}

	[HttpPost("{action}")]
	[ActionDisplayName("حذف کاربر از نقش", ActionAccessType.Api, ActionAccessItemType.Delete)]
	public async Task<IActionResult> RemoveUserFromRole(RoleUserRequest ru, CancellationToken cn)
	{
			var user = await userService
					.TableNoTracking
				.FirstOrDefaultAsync(u => u.Id == ru.UserId, cn);

			if (user == null)
			return NotFound("کاربر یافت نشد");

			var role = await dbContext.Set<Role>()
		.FirstOrDefaultAsync(u => u.Id == ru.RoleId, cn);

			if (user == null)
				return NotFound("نقش یافت نشد");

			if (user.RoleIds.Contains(ru.RoleId))
		{
			user.RoleIds.Remove(ru.RoleId	);

				user.Roles.Remove(role.Name);
				await userService.AddAndUpdateUserAsync(
					new()
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
					}

					, cn);
			}

		return Ok(new { message = "کاربر با موفقیت از نقش حذف شد" });
	}

	[HttpGet("/panel/Role/{action}")]
	[ActionDisplayName("دریافت همه کاربران فعال", ActionAccessType.Api, ActionAccessItemType.FetchData)]
	public async Task<IActionResult> GetAllActiveUsers(CancellationToken cn)
	{
		var users = await dbContext.Set<User>()
			.AsNoTracking()
			.Where(u => u.IsActive == IsActiveEnum.Active)
			.Select(u => new
			{
				u.Id,
				u.Name,
				u.Username,
				u.NameFa,
				u.Email,
				u.RoleIds
			})
			.ToListAsync(cn);

		return Ok(users);
	}

	[HttpPost("{action}")]
	[ActionDisplayName("افزودن گروهی کاربران به نقش", ActionAccessType.Api, ActionAccessItemType.Update)]
	public async Task<IActionResult> AddUsersToRoleByUsernames(AddUsersToRoleRequest request, CancellationToken cn)
	{
		var added = new List<object>();
		var notFound = new List<string>();
		var alreadyHasRole = new List<object>();

		var role = await dbContext.Set<Role>()
			.FirstOrDefaultAsync(u => u.Id == request.RoleId, cn);

		if (role == null)
			return NotFound("نقش یافت نشد");

		foreach (var username in request.Usernames)
		{
			var user = await userService.TableNoTracking
				.FirstOrDefaultAsync(u => u.Username == username, cn);

			if (user == null)
			{
				notFound.Add(username);
				continue;
			}

			if (user.RoleIds.Contains(request.RoleId))
			{
				alreadyHasRole.Add(new { username = user.Username, name = user.Name });
				continue;
			}

		user.RoleIds.Add(request.RoleId);
		var rolesList = user.Roles ?? new List<string>();
		if (!rolesList.Contains(role.Name))
		{
			rolesList.Add(role.Name);
		}

		await userService.AddAndUpdateUserAsync(new()
		{
			Id = user.Id,
			Name = user.Name,
			Password = user.Password,
			Username = user.Username,
			ProfileUrl = user.ProfileUrl,
			Roles = rolesList,
			AuthorizationType = user.AuthorizationType,
			RoleIds = user.RoleIds,
			Email = user.Email,
		}, cn);

			added.Add(new { username = user.Username, name = user.Name });
		}

		return Ok(new
		{
			message = $"{added.Count} کاربر اضافه شد، {notFound.Count} کاربر یافت نشد، {alreadyHasRole.Count} کاربر قبلاً این نقش را داشت",
			data = new
			{
				added,
				notFound,
				alreadyHasRole
			}
		});
	}

	public record RoleUserRequest
	{
		public long RoleId;
		public long UserId;
	}

	public record AddUsersToRoleRequest
	{
		public long RoleId { get; set; }
		public required string[] Usernames { get; set; }
	}
}

}



