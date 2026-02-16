using Entities.Auth;
using Entities.Base;
using Entities.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace Services.NotificationGroupServices
{

	public interface INotificationGroupService
	{
		/// <summary>
		/// دریافت اعضای یک گروه بر اساس کد
		/// </summary>
		Task<List<GroupMembersViewModel>> GetGroupMembersAsync(string groupCode, CancellationToken ct = default);

		/// <summary>
		/// افزودن عضو به گروه
		/// </summary>
		 Task<bool> AddMemberToGroupAsync(string groupCode, long userId, CancellationToken ct = default);

		/// <summary>
		/// حذف عضو از گروه
		/// </summary>
		Task<bool> RemoveMemberFromGroupAsync(string groupCode, long userId, CancellationToken ct = default);

		/// <summary>
		/// دریافت لیست تمام گروه‌ها
		/// </summary>
		Task<List<NotificationGroup>> GetAllGroupsAsync(CancellationToken ct = default);

		/// <summary>
		/// ایجاد گروه جدید
		/// </summary>
		Task<NotificationGroup> CreateGroupAsync(string code, string displayName, string? description = null, CancellationToken ct = default);
	}
}
