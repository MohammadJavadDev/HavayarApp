using Entities.Base;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Data.Services.QueryBuilderServices
{
	/// <summary>
	/// سرویس ساخت پارامترهای SQL - بدون replace، پارامترها به ExecuteQueryAsync پاس داده می‌شوند
	/// </summary>
	public interface IParameterResolverService
	{
		/// <summary>
		/// ساخت Dictionary پارامترها برای پاس به ExecuteQueryAsync
		/// کلیدها با @ هستند مثلاً @StartOfToday
		/// </summary>
		Dictionary<string, object> BuildParameters(string query, IReadOnlyDictionary<string, string> userValues = null, string userId = null, string username = null);

		/// <summary>
		/// برای IN/NOT IN که نمی‌توان از پارامتر استفاده کرد - مقدار را resolve می‌کند
		/// </summary>
		void ResolveFiltersForInOperator(List<FilterCondition> filters, IReadOnlyDictionary<string, string> userValues = null, string userId = null, string username = null);
	}

	public class ParameterResolverService : IParameterResolverService
	{
		public Dictionary<string, object> BuildParameters(string query, IReadOnlyDictionary<string, string> userValues = null, string userId = null, string username = null)
		{
			var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
			if (string.IsNullOrEmpty(query)) return result;

			userValues ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			var now = DateTime.Now;
			var utcNow = DateTime.UtcNow;

			var matches = Regex.Matches(query, @"@([a-zA-Z_][a-zA-Z0-9_]*)");
			foreach (Match m in matches)
			{
				var paramName = m.Groups[1].Value;
				var key = paramName.StartsWith("@") ? paramName : "@" + paramName;
				if (result.ContainsKey(key)) continue;

				object value = null;

				// پارامترهای داینامیک از userValues
				if (userValues.TryGetValue(paramName, out var uv))
				{
					value = string.IsNullOrEmpty(uv) ? (object)DBNull.Value : uv;
				}
				else
				{
					// پارامترهای از پیش تعریف شده
					value = ResolveBuiltInValue(paramName, now, utcNow, userId, username);
				}

				result[key] = value ?? DBNull.Value;
			}

			return result;
		}

		/// <summary>
		/// برای فیلترهای IN/NOT IN - مقدار @Param را با مقدار واقعی جایگزین می‌کند (چون IN نمی‌تواند پارامتر بگیرد)
		/// </summary>
		public void ResolveFiltersForInOperator(List<FilterCondition> filters, IReadOnlyDictionary<string, string> userValues = null, string userId = null, string username = null)
		{
			if (filters == null) return;
			userValues ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			var now = DateTime.Now;
			var utcNow = DateTime.UtcNow;

			foreach (var f in filters)
			{
				var op = (f.Operator ?? "").ToLowerInvariant();
				if (op != "in" && op != "notin") continue;
				if (string.IsNullOrEmpty(f.Value)) continue;

				var val = f.Value.Trim();
				if (!val.StartsWith("@")) continue;

				var paramName = val.TrimStart('@');
				if (userValues.TryGetValue(paramName, out var uv) && !string.IsNullOrEmpty(uv))
					f.Value = uv;
				else
				{
					var builtIn = ResolveBuiltInValue(paramName, now, utcNow, userId, username);
					f.Value = builtIn?.ToString() ?? "";
				}
			}
		}

		private static object ResolveBuiltInValue(string name, DateTime now, DateTime utcNow, string userId, string username)
		{
			switch (name.ToUpperInvariant())
			{
				case "NOW": return now;
				case "TODAY":
				case "CURRENTDATE": return now.Date;
				case "CURRENTDATETIME": return now;
				case "UTCNOW": return utcNow;

				case "STARTOFTODAY": return now.Date;
				case "ENDOFTODAY": return now.Date.AddDays(1).AddTicks(-1);

				case "STARTOFWEEK":
					var sow = now.DayOfWeek == DayOfWeek.Saturday ? 0 : (int)now.DayOfWeek + 1;
					return now.Date.AddDays(-sow);
				case "ENDOFWEEK":
					var eow = now.DayOfWeek == DayOfWeek.Saturday ? 0 : (int)now.DayOfWeek + 1;
					return now.Date.AddDays(-eow).AddDays(6).AddDays(1).AddTicks(-1);

				case "STARTOFMONTH": return new DateTime(now.Year, now.Month, 1);
				case "ENDOFMONTH": return new DateTime(now.Year, now.Month, 1).AddMonths(1).AddTicks(-1);

				case "STARTOFYEAR": return new DateTime(now.Year, 1, 1);
				case "ENDOFYEAR": return new DateTime(now.Year, 12, 31);

				case "CURRENTYEAR": return now.Year;
				case "CURRENTMONTH": return now.Month;
				case "CURRENTQUARTER": return (now.Month - 1) / 3 + 1;

				case "CURRENTUSERID": return userId ?? (object)DBNull.Value;
				case "CURRENTUSERNAME": return username ?? (object)DBNull.Value;
				case "CURRENTUSERFULLNAME": return username ?? (object)DBNull.Value;
				case "CURRENTUSEREMAIL": return DBNull.Value;
				case "CURRENTUSERROLE": return DBNull.Value;
				case "CURRENTUSERROLES": return DBNull.Value;
				case "CURRENTUSERORGANIZATIONID": return DBNull.Value;
				case "CURRENTUSERORGANIZATIONNAME": return DBNull.Value;

				default: return null;
			}
		}

		public static IReadOnlyList<(string Name, string Description)> GetBuiltInParameters()
		{
			return new List<(string, string)>
			{
				("Now", "تاریخ و زمان فعلی"),
				("Today", "تاریخ امروز"),
				("CurrentDate", "تاریخ فعلی"),
				("CurrentDateTime", "تاریخ و زمان فعلی"),
				("UtcNow", "تاریخ و زمان UTC"),
				("StartOfToday", "ابتدای امروز"),
				("EndOfToday", "انتهای امروز"),
				("StartOfWeek", "ابتدای هفته"),
				("EndOfWeek", "انتهای هفته"),
				("StartOfMonth", "ابتدای ماه"),
				("EndOfMonth", "انتهای ماه"),
				("StartOfYear", "ابتدای سال"),
				("EndOfYear", "انتهای سال"),
				("CurrentYear", "سال جاری"),
				("CurrentMonth", "ماه جاری"),
				("CurrentQuarter", "فصل جاری"),
				("CurrentUserId", "شناسه کاربر"),
				("CurrentUsername", "نام کاربری"),
				("CurrentUserFullName", "نام کامل کاربر"),
				("CurrentUserEmail", "ایمیل کاربر"),
				("CurrentUserRole", "نقش کاربر"),
				("CurrentUserRoles", "لیست نقش‌های کاربر"),
				("CurrentUserOrganizationId", "شناسه سازمان کاربر"),
				("CurrentUserOrganizationName", "نام سازمان کاربر")
			};
		}
	}
}
