using System.Data;

namespace ReportBuilder.Services;

/// <summary>
/// Stimulsoft ImageURL cannot call the authenticated /File/download endpoint.
/// Convert FileEntity.PhysicalPath (relative) to a full disk path the engine can load.
/// </summary>
public static class CertificatePhotoResolver
{
	public static void ResolveToFullPaths(DataSet dataSet, string? uploadsRoot)
	{
		if (dataSet == null || string.IsNullOrWhiteSpace(uploadsRoot))
			return;

		foreach (DataTable table in dataSet.Tables)
		{
			if (!table.Columns.Contains("PhotoUrl"))
				continue;

			foreach (DataRow row in table.Rows)
			{
				var value = row["PhotoUrl"]?.ToString()?.Trim();
				if (string.IsNullOrEmpty(value))
					continue;

				var fullPath = ResolveFullPath(value, uploadsRoot);
				row["PhotoUrl"] = string.IsNullOrEmpty(fullPath) ? DBNull.Value : fullPath;
			}
		}
	}

	private static string? ResolveFullPath(string value, string uploadsRoot)
	{
		if (Path.IsPathRooted(value) && File.Exists(value))
			return value;

		var relative = value.Replace('/', Path.DirectorySeparatorChar).TrimStart('\\', '/');
		if (relative.StartsWith("uploads" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
			relative = relative["uploads".Length..].TrimStart('\\', '/');

		var combined = Path.GetFullPath(Path.Combine(uploadsRoot, relative));
		return File.Exists(combined) ? combined : null;
	}
}
