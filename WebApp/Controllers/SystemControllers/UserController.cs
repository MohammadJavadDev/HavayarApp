using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
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
	    IAccessMemoryStorage _accessMemoryStorage) : BaseController
	{


		[HttpGet("/panel/{action}")]
		[ActionDisplayName("لیست کاربر ها", ActionAccessType.View , ActionAccessItemType.List)]
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
		[ActionDisplayName("ذخیره کاربر ها", ActionAccessType.Api , ActionAccessItemType.Save)]
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
					Roles = user.Roles
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
					Roles = user.Roles
				}, cn);
			}

			return Ok(user);
		}

		[HttpGet("/panel/User/Search")]
 
		public async Task<IActionResult> SearchUsers(string term, int page = 1, CancellationToken cn = default)
		{
			var query = service.TableNoTracking.AsQueryable();

			if (!string.IsNullOrWhiteSpace(term))
			{
				query = query.Where(u => u.Name.Contains(term) || u.Username.Contains(term));
			}

			var pageSize = 20;
			var totalCount = await query.CountAsync(cn);
			var skip = (page - 1) * pageSize;

			var users = await query
				.OrderBy(u => u.Name)
				.Skip(skip)
				.Take(pageSize)
				.Select(u => new { id = u.Id, text = u.Name })
				.ToListAsync(cn);

			var hasMore = (skip + users.Count) < totalCount;

			return Ok(new 
			{ 
				results = users,
				pagination = new { more = hasMore }
			});
		}
	}
 
}


