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
using System.ComponentModel.DataAnnotations;
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
			if (request == null || string.IsNullOrWhiteSpace(request.GroupCode) || request.UserId <= 0)
				return Ok(new { success = false, message = "داده نامعتبر" });

			var result = await _notificationGroupService.AddMemberToGroupAsync(request.GroupCode, request.UserId);
			return Ok(new { success = result, message = result ? "عضو با موفقیت اضافه شد" : "خطا در افزودن عضو" });
		}

		[HttpPost("[action]")]
		[ActionDisplayName("حذف اعضا", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> RemoveMember([FromBody] AddRemoveMemberRequest request)
		{
			if (request == null || string.IsNullOrWhiteSpace(request.GroupCode) || request.UserId <= 0)
				return Ok(new { success = false, message = "داده نامعتبر" });
			if (!CurrentUserHasRole("admin"))
				return Ok(new { success = false, message = "عدم دسترسی" });
			var result = await _notificationGroupService.RemoveMemberFromGroupAsync(request.GroupCode, request.UserId);
			return Ok(new { success = result, message = result ? "عضو با موفقیت حذف شد" : "خطا در حذف عضو" });
		}

		 
		 
		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public async Task<IActionResult> Edit(long? id, CancellationToken cn)
		{
			var group = await _db.NotificationGroups
					.AsNoTracking()
				    .Include(g => g.Members)
				    .ThenInclude(m => m.User)
				    .FirstOrDefaultAsync(g => g.Id == id, cn);

			if (group == null)
				return NotFound();

			var allUsers = await _userService.TableNoTracking.ToListAsync(cn);

			var viewModel = new ManageMembersViewModel
			{
				Group = group,
				AllUsers = allUsers,
				CurrentMembers = group.Members.Where(m => m.IsActive == IsActiveEnum.Active).ToList()
			};

			return View(@"\Views\Panel\System\NotificationGroup\Edit.cshtml", viewModel);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public async Task<IActionResult> New(CancellationToken cn)
		{
			var viewModel = new ManageMembersViewModel
			{
				Group = new NotificationGroup
				{
					IsActive = IsActiveEnum.Active
				},
				AllUsers = await _userService.TableNoTracking.ToListAsync(cn)
			};

			return View(@"\Views\Panel\System\NotificationGroup\Edit.cshtml", viewModel);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save([FromBody] NotificationGroupInputModel model, CancellationToken cn)
		{
			try
			{
				if (!model.Id.HasValue || model.Id.Value == 0)
				{
					var createdGroup = await _notificationGroupService.CreateGroupAsync(
						model.Code,
						model.DisplayName,
						model.Description,
						cn,
						model.IsActive);

					return Ok(createdGroup);
				}

				var updatedGroup = await _notificationGroupService.UpdateGroupAsync(
					model.Id.Value,
					model.Code,
					model.DisplayName,
					model.Description,
					model.IsActive,
					cn);

				return updatedGroup == null
					? NotFound("گروه اعلان یافت نشد.")
					: Ok(updatedGroup);
			}
			catch (ArgumentException ex)
			{
				return BadRequest(ex.Message);
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(ex.Message);
			}
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
		public NotificationGroup Group { get; set; } = new();
		public List<User> AllUsers { get; set; } = new();
		public List<NotificationGroupMember> CurrentMembers { get; set; } = new();
		public bool IsNew => !Group.Id.HasValue || Group.Id.Value == 0;
    }

	public class AddRemoveMemberRequest
	{
		public string GroupCode { get; set; } = string.Empty;
		public long UserId { get; set; }
	}

	public class NotificationGroupInputModel
	{
		public long? Id { get; set; }

		[Required(ErrorMessage = "کد گروه الزامی است.")]
		[MaxLength(100, ErrorMessage = "کد گروه نمی‌تواند بیشتر از ۱۰۰ کاراکتر باشد.")]
		[RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9._-]*$", ErrorMessage = "کد گروه فقط می‌تواند شامل حروف انگلیسی، عدد، نقطه، خط تیره و زیرخط باشد.")]
		public string Code { get; set; } = string.Empty;

		[Required(ErrorMessage = "نام گروه الزامی است.")]
		[MaxLength(200, ErrorMessage = "نام گروه نمی‌تواند بیشتر از ۲۰۰ کاراکتر باشد.")]
		public string DisplayName { get; set; } = string.Empty;

		[MaxLength(1500, ErrorMessage = "توضیحات نمی‌تواند بیشتر از ۱۵۰۰ کاراکتر باشد.")]
		public string? Description { get; set; }

		public IsActiveEnum? IsActive { get; set; }
	}
}
