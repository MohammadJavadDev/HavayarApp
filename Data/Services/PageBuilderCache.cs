using Common;
using Microsoft.Extensions.Primitives;
using System.Collections.Concurrent;

namespace Data.Services;

public sealed class PageBuilderCache : IPageBuilderCache, ISingletonDependency
{
	private readonly ConcurrentDictionary<string, CachedPageEntry> _entries = new(StringComparer.OrdinalIgnoreCase);
	private readonly ConcurrentDictionary<string, CancellationTokenSource> _changeTokens = new(StringComparer.OrdinalIgnoreCase);
	private readonly object _changeTokenLock = new();

	public string? GetContent(string normalizedViewPath)
	{
		return _entries.TryGetValue(normalizedViewPath, out var entry) ? entry.Content : null;
	}

	public DateTimeOffset? GetLastModified(string normalizedViewPath)
	{
		return _entries.TryGetValue(normalizedViewPath, out var entry) ? entry.LastModified : null;
	}

	public IChangeToken Watch(string normalizedViewPath)
	{
		var source = _changeTokens.GetOrAdd(normalizedViewPath, _ => new CancellationTokenSource());
		return new CancellationChangeToken(source.Token);
	}

	public void Set(string normalizedViewPath, string content, DateTimeOffset lastModified)
	{
		if (_entries.TryGetValue(normalizedViewPath, out var existing) &&
		    existing.Content == content &&
		    existing.LastModified == lastModified)
			return;

		_entries[normalizedViewPath] = new CachedPageEntry(content, lastModified);
		SignalChange(normalizedViewPath);
	}

	public void Remove(string normalizedViewPath)
	{
		_entries.TryRemove(normalizedViewPath, out _);
		SignalChange(normalizedViewPath);
	}

	public void Clear()
	{
		_entries.Clear();
		foreach (var path in _changeTokens.Keys)
			SignalChange(path);
	}

	public void LoadAll(IEnumerable<(string ViewPath, string Content, DateTimeOffset LastModified)> pages)
	{
		_entries.Clear();
		foreach (var page in pages)
			_entries[page.ViewPath] = new CachedPageEntry(page.Content, page.LastModified);

		foreach (var path in _changeTokens.Keys)
			SignalChange(path);
	}

	private void SignalChange(string normalizedViewPath)
	{
		var replacement = new CancellationTokenSource();
		CancellationTokenSource? previous = null;

		lock (_changeTokenLock)
		{
			_changeTokens.TryGetValue(normalizedViewPath, out previous);
			_changeTokens[normalizedViewPath] = replacement;
		}

		if (previous == null)
			return;

		previous.Cancel();
		previous.Dispose();
	}

	private sealed record CachedPageEntry(string Content, DateTimeOffset LastModified);
}
