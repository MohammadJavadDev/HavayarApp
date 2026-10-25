namespace Services.AccessServices;

/// <summary>
/// نسخهٔ کش «دارد / ندارد» در میان‌افزار دسترسی.
/// با افزایش نسخه، جواب‌های قبلی دیگر خوانده نمی‌شوند.
/// </summary>
public interface IAccessDecisionCache
{
	long Version { get; }
	void Invalidate();
}

public sealed class AccessDecisionCache : IAccessDecisionCache
{
	private long _version;

	public long Version => Volatile.Read(ref _version);

	public void Invalidate() => Interlocked.Increment(ref _version);
}
