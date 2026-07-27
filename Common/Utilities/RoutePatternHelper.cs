using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Utilities;

 
public static class RoutePatternHelper
{
	public static bool IsMatch(string actualPath, string templatePath)
	{
		if (string.IsNullOrEmpty(actualPath) || string.IsNullOrEmpty(templatePath))
			return false;

		var pathSegs = actualPath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
		var tmplSegs = templatePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

		int pi = 0, ti = 0;

		while (pi < pathSegs.Length && ti < tmplSegs.Length)
		{
			var tmpl = tmplSegs[ti];

			if (IsParameterSegment(tmpl, out _, out _, out bool isCatchAll))
			{
				if (isCatchAll) return true; // {**rest} — بقیه مسیر رو کاور می‌کنه

				// پارامتر معمولی — یه سگمنت از مسیر واقعی مصرف می‌کنه
				pi++;
				ti++;
			}
			else
			{
				// سگمنت لیترال — باید دقیقاً match کنه
				if (!string.Equals(pathSegs[pi], tmpl, StringComparison.OrdinalIgnoreCase))
					return false;

				pi++;
				ti++;
			}
		}

		// پارامترهای اختیاری باقی‌مانده در تمپلیت رو رد کن
		while (ti < tmplSegs.Length)
		{
			if (IsParameterSegment(tmplSegs[ti], out _, out bool isOptional, out bool isCatchAll)
			    && (isOptional || isCatchAll))
			{
				ti++;
			}
			else break;
		}

		return pi == pathSegs.Length && ti == tmplSegs.Length;
	}

	/// <summary>
	/// پارس کردن سگمنت تمپلیت:
	/// {id} | {id:int} | {id?} | {id:long?} | {*rest} | {**rest}
	/// </summary>
	private static bool IsParameterSegment(
	    string segment,
	    out string paramName,
	    out bool isOptional,
	    out bool isCatchAll)
	{
		paramName = string.Empty;
		isOptional = false;
		isCatchAll = false;

		if (!segment.StartsWith('{') || !segment.EndsWith('}'))
			return false;

		var inner = segment[1..^1]; // حذف { و }

		// Catch-all: {**param} یا {*param}
		if (inner.StartsWith("**")) { isCatchAll = true; paramName = inner[2..]; return true; }
		if (inner.StartsWith('*')) { isCatchAll = true; paramName = inner[1..]; return true; }

		// حذف constraint: {id:int} → id
		var colonIdx = inner.IndexOf(':');
		if (colonIdx >= 0) inner = inner[..colonIdx];

		// اختیاری: {id?}
		if (inner.EndsWith('?')) { isOptional = true; inner = inner[..^1]; }

		paramName = inner;
		return true;
	}
}