using Data.Contracts;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Services.Settings
{
	/// <summary>
	/// خواندن/نوشتن <see cref="GeneralSetting"/>.
	/// کل جدول با کش کوتاه‌مدت (۳۰ ثانیه) در حافظه نگه‌داری می‌شود؛ بنابراین تغییر مقدار در دیتابیس
	/// بدون ری‌استارت (حداکثر پس از ۳۰ ثانیه، در هر پروسه‌ای از جمله App.BackgroundJob) اعمال می‌شود.
	/// </summary>
	public interface IGeneralSettingService
	{
		Task<string?> GetAsync(string key, CancellationToken ct = default);
		Task<string> GetRequiredAsync(string key, CancellationToken ct = default);
		Task<int> GetIntAsync(string key, int fallback, CancellationToken ct = default);
		Task<bool> GetBoolAsync(string key, bool fallback, CancellationToken ct = default);
		Task SetAsync(string key, string? value, CancellationToken ct = default);
		void InvalidateCache();
	}

	public class GeneralSettingService(IUnitOfWork unitOfWork) : IGeneralSettingService
	{
		private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);
		private static readonly SemaphoreSlim Gate = new(1, 1);
		private static Dictionary<string, string?>? _cache;
		private static DateTime _loadedAt = DateTime.MinValue;

		public async Task<string?> GetAsync(string key, CancellationToken ct = default)
		{
			var map = await GetMapAsync(ct);
			return map.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
				? value.Trim()
				: GeneralSettingKeys.Default(key);
		}

		public async Task<string> GetRequiredAsync(string key, CancellationToken ct = default)
		{
			var value = await GetAsync(key, ct);
			if (string.IsNullOrWhiteSpace(value))
				throw new InvalidOperationException($"تنظیم «{key}» مقدار ندارد (جدول system.GeneralSetting).");
			return value;
		}

		public async Task<int> GetIntAsync(string key, int fallback, CancellationToken ct = default)
		{
			var value = await GetAsync(key, ct);
			return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : fallback;
		}

		public async Task<bool> GetBoolAsync(string key, bool fallback, CancellationToken ct = default)
		{
			var value = await GetAsync(key, ct);
			return bool.TryParse(value, out var b) ? b : fallback;
		}

		public async Task SetAsync(string key, string? value, CancellationToken ct = default)
		{
			var repo = unitOfWork.Repository<GeneralSetting>();
			var row = await repo.Table.FirstOrDefaultAsync(x => x.SettingKey == key, ct);
			if (row == null)
			{
				var info = GeneralSettingKeys.All.FirstOrDefault(x => x.Key == key);
				row = new GeneralSetting
				{
					SettingKey = key,
					SettingValue = value,
					Title = info?.Title,
					Description = info?.Description,
					IsSecret = info?.IsSecret ?? false
				};
				await repo.AddAsync(row, ct, saveAudit: false);
			}
			else
			{
				row.SettingValue = value;
				await repo.UpdateAsync(row, ct, saveAudit: false);
			}

			InvalidateCache();
		}

		public void InvalidateCache()
		{
			_cache = null;
			_loadedAt = DateTime.MinValue;
		}

		private async Task<Dictionary<string, string?>> GetMapAsync(CancellationToken ct)
		{
			var cache = _cache;
			if (cache != null && DateTime.UtcNow - _loadedAt < CacheTtl)
				return cache;

			await Gate.WaitAsync(ct);
			try
			{
				if (_cache != null && DateTime.UtcNow - _loadedAt < CacheTtl)
					return _cache;

				var rows = await unitOfWork.Repository<GeneralSetting>().TableNoTracking
					.Select(x => new { x.SettingKey, x.SettingValue })
					.ToListAsync(ct);

				_cache = rows.ToDictionary(x => x.SettingKey, x => x.SettingValue, StringComparer.OrdinalIgnoreCase);
				_loadedAt = DateTime.UtcNow;
				return _cache;
			}
			finally
			{
				Gate.Release();
			}
		}
	}
}
