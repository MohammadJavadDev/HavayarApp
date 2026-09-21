using Entities.Base;
using Services.Job;

namespace App.BackgroundJob.Jobs;

/// <summary>
/// نگاشت کلیدهای غیر یکتا (مثلاً HamkaranId تکراری) بدون توقف کل جاب.
/// الگوی D12: GroupBy + رکورد فعال + کم‌ترین Id، و لاگ warning برای تکراری‌ها.
/// </summary>
internal static class JobLookup
{
	public static async Task<Dictionary<TKey, TValue>> ToUniqueValueMapAsync<TSource, TKey, TValue>(
		IReadOnlyCollection<TSource> source,
		Func<TSource, TKey> keySelector,
		Func<TSource, TValue> valueSelector,
		IJobLogger? jobLogger,
		string duplicateLabel,
		CancellationToken cancellationToken,
		Func<TSource, long?>? idSelector = null,
		Func<TSource, bool>? isPreferred = null)
		where TKey : notnull
	{
		var groups = source.GroupBy(keySelector).ToList();
		var duplicateKeys = groups.Where(g => g.Count() > 1).Select(g => g.Key).ToList();
		if (duplicateKeys.Count > 0 && jobLogger != null)
		{
			var preview = string.Join(", ", duplicateKeys.Take(30));
			var suffix = duplicateKeys.Count > 30 ? " ..." : string.Empty;
			await jobLogger.LogWarningAsync(
				$"{duplicateKeys.Count} کلید تکراری در {duplicateLabel} — رکورد فعال / کم‌ترین Id انتخاب شد: {preview}{suffix}",
				0,
				cancellationToken);
		}

		return groups.ToDictionary(
			g => g.Key,
			g => valueSelector(PickPreferred(g, idSelector, isPreferred)));
	}

	public static async Task<Dictionary<TKey, TSource>> ToUniqueMapAsync<TSource, TKey>(
		IReadOnlyCollection<TSource> source,
		Func<TSource, TKey> keySelector,
		IJobLogger? jobLogger,
		string duplicateLabel,
		CancellationToken cancellationToken,
		Func<TSource, long?>? idSelector = null,
		Func<TSource, bool>? isPreferred = null)
		where TKey : notnull
	{
		return await ToUniqueValueMapAsync(
			source,
			keySelector,
			x => x,
			jobLogger,
			duplicateLabel,
			cancellationToken,
			idSelector,
			isPreferred);
	}

	private static TSource PickPreferred<TSource>(
		IEnumerable<TSource> group,
		Func<TSource, long?>? idSelector,
		Func<TSource, bool>? isPreferred)
	{
		return group
			.OrderByDescending(x => isPreferred?.Invoke(x) == true)
			.ThenBy(x => ResolveId(x, idSelector) ?? long.MaxValue)
			.First();
	}

	private static long? ResolveId<TSource>(TSource source, Func<TSource, long?>? idSelector)
	{
		if (idSelector != null)
			return idSelector(source);

		if (source is BaseEntity entity)
			return entity.Id;

		return null;
	}
}
