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
			var normalizedCode = NormalizeGroupCode(groupCode);
			var cacheKey = GetCacheKey(normalizedCode);

			if (_cache.TryGetValue(cacheKey, out List<GroupMembersViewModel>? cachedUsers) && cachedUsers != null)
			{
				return cachedUsers;
			}

			var users = await _db.NotificationGroupMembers
			    .AsNoTracking()
			    .Where(m => m.NotificationGroup.Code == normalizedCode
				    && m.NotificationGroup.IsActive == IsActiveEnum.Active
				    && m.IsActive == IsActiveEnum.Active)
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
			var normalizedCode = NormalizeGroupCode(groupCode);
			var group = await _db.NotificationGroups
			    .FirstOrDefaultAsync(g => g.Code == normalizedCode, ct);

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
			InvalidateGroupCache(group.Code);

			return true;
		}

		public async Task<bool> RemoveMemberFromGroupAsync(string groupCode, long userId, CancellationToken ct = default)
		{
			var normalizedCode = NormalizeGroupCode(groupCode);
			var member = await _db.NotificationGroupMembers
			    .Include(m => m.NotificationGroup)
			    .FirstOrDefaultAsync(m => m.NotificationGroup.Code == normalizedCode && m.UserId == userId, ct);

			if (member == null)
				return false;

			_db.NotificationGroupMembers.Remove(member);
			await _db.SaveChangesAsync(ct);

			// Clear cache
			InvalidateGroupCache(member.NotificationGroup.Code);

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
		    CancellationToken ct = default,
		    IsActiveEnum? isActive = null)
		{
			var normalizedCode = NormalizeGroupCode(code);
			var normalizedDisplayName = NormalizeDisplayName(displayName);

			var codeExists = await _db.NotificationGroups
				.AsNoTracking()
				.AnyAsync(g => g.Code == normalizedCode, ct);
			if (codeExists)
				throw new InvalidOperationException("گروهی با این کد قبلاً ثبت شده است.");

			var now = DateTime.Now;
			var group = new NotificationGroup
			{
				Code = normalizedCode,
				DisplayName = normalizedDisplayName,
				Description = NormalizeDescription(description),
				IsActive = isActive ?? IsActiveEnum.Active,
				CreatedById = _sdk.CurrentUser.Id,
				CreatedByName = _sdk.CurrentUser.FullName ?? _sdk.CurrentUser.Username,
				ModifiedById = _sdk.CurrentUser.Id,
				ModifiedByName = _sdk.CurrentUser.FullName ?? _sdk.CurrentUser.Username,
				CreatedOnMiladiDateTime = now,
				CreatedOnShamsiDateTime = now.ToShamsiDateTime(),
				ModifiedDateMiladiDateTime = now,
				ModifiedDateShamsiDateTime = now.ToShamsiDateTime()
			};

			await _db.NotificationGroups.AddAsync(group, ct);
			await _db.SaveChangesAsync(ct);
			InvalidateGroupCache(group.Code);

			return group;
		}

		public async Task<NotificationGroup?> UpdateGroupAsync(
			long id,
			string code,
			string displayName,
			string? description = null,
			IsActiveEnum? isActive = null,
			CancellationToken ct = default)
		{
			var normalizedCode = NormalizeGroupCode(code);
			var normalizedDisplayName = NormalizeDisplayName(displayName);
			var group = await _db.NotificationGroups.FirstOrDefaultAsync(g => g.Id == id, ct);

			if (group == null)
				return null;

			var codeExists = await _db.NotificationGroups
				.AsNoTracking()
				.AnyAsync(g => g.Id != id && g.Code == normalizedCode, ct);
			if (codeExists)
				throw new InvalidOperationException("گروهی با این کد قبلاً ثبت شده است.");

			var oldCode = group.Code;
			var now = DateTime.Now;

			group.Code = normalizedCode;
			group.DisplayName = normalizedDisplayName;
			group.Description = NormalizeDescription(description);
			group.IsActive = isActive ?? group.IsActive ?? IsActiveEnum.Active;
			group.ModifiedById = _sdk.CurrentUser.Id;
			group.ModifiedByName = _sdk.CurrentUser.FullName ?? _sdk.CurrentUser.Username;
			group.ModifiedDateMiladiDateTime = now;
			group.ModifiedDateShamsiDateTime = now.ToShamsiDateTime();

			await _db.SaveChangesAsync(ct);

			// تغییر کد، کلید کش را نیز تغییر می‌دهد؛ هر دو کلید باید پاک شوند.
			InvalidateGroupCache(oldCode, group.Code);

			return group;
		}

		private static string NormalizeGroupCode(string groupCode)
		{
			if (string.IsNullOrWhiteSpace(groupCode))
				throw new ArgumentException("کد گروه نمی‌تواند خالی باشد.", nameof(groupCode));

			return groupCode.Trim();
		}

		private static string NormalizeDisplayName(string displayName)
		{
			if (string.IsNullOrWhiteSpace(displayName))
				throw new ArgumentException("نام گروه نمی‌تواند خالی باشد.", nameof(displayName));

			return displayName.Trim();
		}

		private static string? NormalizeDescription(string? description)
		{
			return string.IsNullOrWhiteSpace(description) ? null : description.Trim();
		}

		private static string GetCacheKey(string groupCode)
		{
			return $"{CACHE_KEY_PREFIX}{groupCode.Trim().ToUpperInvariant()}";
		}

		private void InvalidateGroupCache(params string?[] groupCodes)
		{
			foreach (var groupCode in groupCodes.Where(code => !string.IsNullOrWhiteSpace(code)).Distinct(StringComparer.OrdinalIgnoreCase))
			{
				_cache.Remove(GetCacheKey(groupCode!));
			}
		}
	}
}
