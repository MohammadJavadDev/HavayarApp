using Common.Utilities;
using Data.Services;
using Entities.Base;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using WebFramework.Abstractions;
using System.Text;

namespace WebFramework.Views;

/// <summary>
/// Primary Razor file provider: database pages first, then physical files under ContentRoot.
/// </summary>
public sealed class DiskThenDatabaseFileProvider : IFileProvider
{
	private readonly PhysicalFileProvider _physical;
	private readonly IPageBuilderCache _cache;
	private readonly IServiceProvider _serviceProvider;
	private readonly string _contentRootPath;

	public DiskThenDatabaseFileProvider(
		IEnvironmentService environment,
		IPageBuilderCache cache,
		IServiceProvider serviceProvider)
	{
		_contentRootPath = environment.ContentRootPath;
		_physical = new PhysicalFileProvider(_contentRootPath);
		_cache = cache;
		_serviceProvider = serviceProvider;
	}

	public IDirectoryContents GetDirectoryContents(string subpath)
	{
		foreach (var candidate in GetPathCandidates(subpath))
		{
			if (_cache.GetContent(candidate) != null)
				return new SingleFileDirectoryContents(_contentRootPath, candidate, ResolveDatabaseContent(candidate)!);
		}

		return _physical.GetDirectoryContents(NormalizeSubpath(subpath));
	}

	public IFileInfo GetFileInfo(string subpath)
	{
		foreach (var candidate in GetPathCandidates(subpath))
		{
			var content = ResolveDatabaseContent(candidate);
			if (content == null)
				continue;

			var lastModified = _cache.GetLastModified(candidate) ?? DateTimeOffset.UtcNow;
			return new InMemoryRazorFileInfo(_contentRootPath, candidate, content, lastModified);
		}

		return GetPhysicalFileInfo(subpath);
	}

	private IFileInfo GetPhysicalFileInfo(string subpath)
	{
		var physical = _physical.GetFileInfo(NormalizeSubpath(subpath));
		if (physical.Exists)
			return physical;

		var trimmed = NormalizeSubpath(subpath).TrimStart('/');
		if (!string.Equals(trimmed, NormalizeSubpath(subpath), StringComparison.Ordinal))
		{
			physical = _physical.GetFileInfo(trimmed);
			if (physical.Exists)
				return physical;
		}

		return new NotFoundFileInfo(subpath);
	}

	public IChangeToken Watch(string filter)
	{
		var tokens = new List<IChangeToken> { _physical.Watch(filter) };
		tokens.AddRange(GetPathCandidates(filter).Distinct(StringComparer.OrdinalIgnoreCase).Select(_cache.Watch));
		return new CompositeChangeToken(tokens);
	}

	private string? ResolveDatabaseContent(string normalizedPath)
	{
		var cached = _cache.GetContent(normalizedPath);
		if (!string.IsNullOrEmpty(cached))
			return cached;

		using var scope = _serviceProvider.CreateScope();
		var pageBuilder = scope.ServiceProvider.GetRequiredService<IPageBuilderService>();
		var page = pageBuilder.GetByPathAsync(normalizedPath).ConfigureAwait(false).GetAwaiter().GetResult();
		if (page == null || page.IsActive != IsActiveEnum.Active || string.IsNullOrWhiteSpace(page.Content))
			return null;

		var lastModified = page.ModifiedDateMiladiDateTime ?? page.CreatedOnMiladiDateTime ?? DateTime.Now;
		_cache.Set(normalizedPath, page.Content, new DateTimeOffset(lastModified));
		return page.Content;
	}

	private static string NormalizeSubpath(string subpath)
	{
		if (string.IsNullOrWhiteSpace(subpath))
			return subpath;

		return subpath.Replace('\\', '/');
	}

	private static IEnumerable<string> GetPathCandidates(string subpath)
	{
		if (string.IsNullOrWhiteSpace(subpath))
			yield break;

		yield return PageBuilderPathHelper.NormalizeViewPath(subpath);

		var trimmed = subpath.Trim().Replace('\\', '/');
		while (trimmed.StartsWith('/'))
			trimmed = trimmed[1..];

		var normalized = PageBuilderPathHelper.NormalizeViewPath(trimmed);
		if (!string.IsNullOrEmpty(normalized))
			yield return normalized;

		if (trimmed.StartsWith("Views/", StringComparison.OrdinalIgnoreCase))
			yield return PageBuilderPathHelper.NormalizeViewPath(trimmed);
	}

	private sealed class InMemoryRazorFileInfo : IFileInfo
	{
		private readonly byte[] _bytes;

		public InMemoryRazorFileInfo(string contentRootPath, string virtualPath, string content, DateTimeOffset lastModified)
		{
			Name = Path.GetFileName(virtualPath);
			PhysicalPath = PageBuilderPathHelper.ToPhysicalViewPath(contentRootPath, virtualPath);
			_bytes = Encoding.UTF8.GetBytes(content);
			Length = _bytes.Length;
			LastModified = lastModified;
			Exists = true;
		}

		public bool Exists { get; }
		public long Length { get; }
		public string? PhysicalPath { get; }
		public string Name { get; }
		public DateTimeOffset LastModified { get; }
		public bool IsDirectory => false;

		public Stream CreateReadStream() => new MemoryStream(_bytes);
	}

	private sealed class SingleFileDirectoryContents : IDirectoryContents
	{
		private readonly IFileInfo _file;

		public SingleFileDirectoryContents(string contentRoot, string path, string content)
		{
			_file = new InMemoryRazorFileInfo(contentRoot, path, content, DateTimeOffset.UtcNow);
		}

		public bool Exists => true;
		public IEnumerator<IFileInfo> GetEnumerator() { yield return _file; }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
