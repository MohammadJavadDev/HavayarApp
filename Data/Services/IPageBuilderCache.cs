namespace Data.Services;

using Microsoft.Extensions.Primitives;

public interface IPageBuilderCache
{
	string? GetContent(string normalizedViewPath);
	DateTimeOffset? GetLastModified(string normalizedViewPath);
	IChangeToken Watch(string normalizedViewPath);
	void Set(string normalizedViewPath, string content, DateTimeOffset lastModified);
	void Remove(string normalizedViewPath);
	void Clear();
	void LoadAll(IEnumerable<(string ViewPath, string Content, DateTimeOffset LastModified)> pages);
}
