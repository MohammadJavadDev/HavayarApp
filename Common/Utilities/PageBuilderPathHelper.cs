namespace Common.Utilities;

public static class PageBuilderPathHelper
{
	public static string NormalizeViewPath(string? path)
	{
		if (string.IsNullOrWhiteSpace(path))
			return string.Empty;

		var normalized = path.Replace('\\', '/').Trim().TrimStart('@');
		if (normalized.StartsWith("~/", StringComparison.Ordinal))
			normalized = normalized[2..];

		while (normalized.StartsWith('/'))
			normalized = normalized[1..];

		var viewsIndex = normalized.IndexOf("/Views/", StringComparison.OrdinalIgnoreCase);
		if (viewsIndex >= 0)
			normalized = normalized[(viewsIndex + 1)..];

		if (!normalized.StartsWith("Views/", StringComparison.OrdinalIgnoreCase))
		{
			if (normalized.StartsWith("Views", StringComparison.OrdinalIgnoreCase))
				normalized = "Views/" + normalized[5..].TrimStart('/');
			else
				normalized = "Views/" + normalized.TrimStart('/');
		}

		return normalized;
	}

	public static IEnumerable<string> GetViewLookupPaths(string? path)
	{
		var normalized = NormalizeViewPath(path);
		if (string.IsNullOrEmpty(normalized))
			yield break;

		yield return "~/" + normalized;
		yield return ToFileProviderSubpath(normalized);
		yield return normalized;
	}

	public static string ToPhysicalViewPath(string contentRootPath, string? path)
	{
		var normalized = NormalizeViewPath(path);
		var relative = normalized.Replace('/', Path.DirectorySeparatorChar);
		return Path.Combine(contentRootPath, relative);
	}

	public static string ToFileProviderSubpath(string normalizedViewPath)
	{
		var path = NormalizeViewPath(normalizedViewPath);
		return path.StartsWith('/') ? path : "/" + path;
	}

	public static (string ListPath, string EditPath) GetStandardFormViewPaths(string module, string entityName)
	{
		var listPath = NormalizeViewPath($"Views/Panel/{module}/{entityName}/List.cshtml");
		var editPath = NormalizeViewPath($"Views/Panel/{module}/{entityName}/Edit.cshtml");
		return (listPath, editPath);
	}

	public static void TryDeleteDiskView(string contentRootPath, string normalizedViewPath)
	{
		if (string.IsNullOrWhiteSpace(contentRootPath) || string.IsNullOrWhiteSpace(normalizedViewPath))
			return;

		var relative = NormalizeViewPath(normalizedViewPath).Replace('/', Path.DirectorySeparatorChar);
		var fullPath = Path.Combine(contentRootPath, relative);
		if (File.Exists(fullPath))
			File.Delete(fullPath);
	}
}
