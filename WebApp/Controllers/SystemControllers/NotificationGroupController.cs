using Common.Attributes;
using Common.Auth.Enums;
using Data;
using Data.Contracts;
using Data.SystemAuth;
using Entities.Auth;
using Entities.Base;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Auth;
using Services.NotificationGroupServices;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.SystemControllers
{
	[Route("Panel/System/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("گروه اعلانات", typeof(NotificationGroup))]
	public class NotificationGroupController(
		IUnitOfWork unitOfWork,
		INotificationGroupService _notificationGroupService,
		IUserService _userService,
		ApplicationDbContext _db) : BaseController
	{
	  
		[HttpPost("[action]")]
		[ActionDisplayName("درج اعضا", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> AddMember([FromBody] AddRemoveMemberRequest request)
		{
			if (request == null)
				return Ok(new { success = false, message = "داده نامعتبر" });

			var result = await _notificationGroupService.AddMemberToGroupAsync(request.GroupCode, request.UserId);
			return Ok(new { success = result, message = result ? "عضو با موفقیت اضافه شد" : "خطا در افزودن عضو" });
		}

		[HttpPost("[action]")]
		[ActionDisplayName("حذف اعضا", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> RemoveMember([FromBody] AddRemoveMemberRequest request)
		{
			if (request == null)
				return Ok(new { success = false, message = "داده نامعتبر" });
			if (!HasRole("admin"))
				return Ok(new { success = false, message = "عدم دسترسی" });
			var result = await _notificationGroupService.RemoveMemberFromGroupAsync(request.GroupCode, request.UserId);
			return Ok(new { success = result, message = result ? "عضو با موفقیت حذف شد" : "خطا در حذف عضو" });
		}

		 
		 
		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public async Task<IActionResult> Edit(long? id)
		{

			var group = await _db.NotificationGroups
				    .Include(g => g.Members)
				    .ThenInclude(m => m.User)
				    .FirstOrDefaultAsync(g => g.Id == id);

			if (group == null)
				return NotFound();

			var allUsers = await _userService.TableNoTracking.ToListAsync();

			var viewModel = new ManageMembersViewModel
			{
				Group = group,
				AllUsers = allUsers,
				CurrentMembers = group.Members.Where(m => m.IsActive == IsActiveEnum.Active).ToList()
			};

			return View(@"\Views\Panel\System\NotificationGroup\Edit.cshtml", viewModel);
		}

		 
		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\System\NotificationGroup\List.cshtml");
		}

	 

		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		[HttpPost("[action]")]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
		{
			return Ok(await unitOfWork.Repository<NotificationGroup>().FetchDataAsync(request, cn));
		}

	}

    public class ManageMembersViewModel
    {
        public NotificationGroup Group { get; set; }
        public List<User> AllUsers { get; set; }
		public List<NotificationGroupMember> CurrentMembers { get; set; } = new();
    }

	public class AddRemoveMemberRequest
	{
		public string GroupCode { get; set; }
		public long UserId { get; set; }
	}
}
