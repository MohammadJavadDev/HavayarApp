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
		IAccessMemoryStorage _accessMemoryStorage) : BaseController
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

			foreach (var item in role.RoleAccesses)
			{
				if (item.ActionAccessType == ActionAccessType.DataProfile && item.ActionAccessItemType == ActionAccessItemType.DataProfile)
				{
					item.RowId = item.Path.ToInt();
				}
			}

			var res = await unitOfWork.Repository<Role>().SaveAsync(role, cn);

			var existRole = roleMemoryStorage.GetRoleById((long)res.Id);

			if (existRole != null)
			{
				roleMemoryStorage.UpdateRoleAccessPaths((long)existRole.Id, []);
			}
			else
			{
				roleMemoryStorage.AddRole(res);
			}

			return Ok(res);
		}
	}

}



