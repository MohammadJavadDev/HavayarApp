using Common.Utilities;
using Data;
using Data.SystemAuth;
using Entities.Auth;
using Entities.Base;
using Entities.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Text;

namespace Services.NotificationGroupServices
{

	public class NotificationGroupService : INotificationGroupService
	{
		private readonly ApplicationDbContext _db;
		private readonly ISdk _sdk;
		private readonly IMemoryCache _cache;
		private const string CACHE_KEY_PREFIX = "NotificationGroup_";
		private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);

		public NotificationGroupService(
			ApplicationDbContext db,
			IMemoryCache cache,
			ISdk sdk
			)
		{
			_db = db;
			_cache = cache;
			_sdk = sdk;
		}

		public async Task<List<GroupMembersViewModel>> GetGroupMembersAsync(string groupCode, CancellationToken ct = default)
		{
			var cacheKey = $"{CACHE_KEY_PREFIX}{groupCode}";

			if (_cache.TryGetValue(cacheKey, out List<GroupMembersViewModel>? cachedUsers) && cachedUsers != null)
			{
				return cachedUsers;
			}

			var users = await _db.NotificationGroupMembers
			    .AsNoTracking()
			    .Where(m => m.NotificationGroup.Code == groupCode && m.IsActive == IsActiveEnum.Active)
			    .Include(m => m.User)
			    .Select(m =>  new GroupMembersViewModel() 
			    {Username= m.User.Username,
				    Email = m.Email,
				    GroupMemberId = m.Id,
				    UserId = m.UserId
			    })
			    .ToListAsync(ct);

			_cache.Set(cacheKey, users, _cacheExpiration);

			return users;
		}

		public async Task<bool> AddMemberToGroupAsync(string groupCode, long userId, CancellationToken ct = default)
		{
			var group = await _db.NotificationGroups
			    .FirstOrDefaultAsync(g => g.Code == groupCode, ct);

			if (group == null)
				return false;

			var user = await _db.Users
			    .FirstOrDefaultAsync(u => u.Id == userId, ct);

			if (user == null)
				return false;

			var exists = await _db.NotificationGroupMembers
			    .AnyAsync(m => m.NotificationGroupId == group.Id && m.UserId == userId, ct);

			if (exists)
				return false;

			var now = DateTime.Now;

			var member = new NotificationGroupMember
			{
				NotificationGroupId = group.Id!.Value,
				UserId = userId,
				Email = user.Email ?? string.Empty,
				FullName = user.Name ?? string.Empty,
				IsActive = IsActiveEnum.Active,
				CreatedById = _sdk.CurrentUser.Id,
				ModifiedById = _sdk.CurrentUser.Id,
				CreatedOnMiladiDateTime = now,
				CreatedOnShamsiDateTime = now.ToShamsiDateTime(),
				ModifiedDateMiladiDateTime = now,
				ModifiedDateShamsiDateTime = now.ToShamsiDateTime()

			};

			await _db.NotificationGroupMembers.AddAsync(member, ct);
			await _db.SaveChangesAsync(ct);

			// Clear cache
			_cache.Remove($"{CACHE_KEY_PREFIX}{groupCode}");

			return true;
		}

		public async Task<bool> RemoveMemberFromGroupAsync(string groupCode, long userId, CancellationToken ct = default)
		{
			var member = await _db.NotificationGroupMembers
			    .FirstOrDefaultAsync(m => m.NotificationGroup.Code == groupCode && m.UserId == userId, ct);

			if (member == null)
				return false;

			_db.NotificationGroupMembers.Remove(member);
			await _db.SaveChangesAsync(ct);

			// Clear cache
			_cache.Remove($"{CACHE_KEY_PREFIX}{groupCode}");

			return true;
		}

		public async Task<List<NotificationGroup>> GetAllGroupsAsync(CancellationToken ct = default)
		{
			return await _db.NotificationGroups
			    .AsNoTracking()
			    .Include(g => g.Members)
			    .ThenInclude(m => m.User)
			    .OrderBy(g => g.DisplayName)
			    .ToListAsync(ct);
		}

		public async Task<NotificationGroup> CreateGroupAsync(
		    string code,
		    string displayName,
		    string? description = null,
		    CancellationToken ct = default)
		{
			var now = DateTime.Now;
			var group = new NotificationGroup
			{
				Code = code,
				DisplayName = displayName,
				Description = description,
				IsActive = IsActiveEnum.Active,
				CreatedById = _sdk.CurrentUser.Id,
				ModifiedById = _sdk.CurrentUser.Id,
				CreatedOnMiladiDateTime = now,
				CreatedOnShamsiDateTime = now.ToShamsiDateTime(),
				ModifiedDateMiladiDateTime = now,
				ModifiedDateShamsiDateTime = now.ToShamsiDateTime()
			};

			await _db.NotificationGroups.AddAsync(group, ct);
			await _db.SaveChangesAsync(ct);

			return group;
		}
	}
}
