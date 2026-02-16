using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Services.QueryBuilderServices
{
	public class SqlQueryValidator
	{
		private static readonly HashSet<string> DangerousKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
	   "INSERT", "UPDATE", "DELETE", "DROP", "CREATE", "ALTER", "TRUNCATE",
	   "EXEC", "EXECUTE", "SP_", "XP_", "MERGE", "INTO", "SET", "GRANT",
	   "REVOKE", "DENY", "BACKUP", "RESTORE", "SHUTDOWN"
    };

		private static readonly HashSet<string> DangerousPatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
	   "xp_cmdshell", "sp_executesql", "sp_oacreate", "sp_oamethod",
	   "openrowset", "opendatasource", "INFORMATION_SCHEMA"
    };

		public class ValidationResult
		{
			public bool IsValid { get; set; }
			public List<string> Errors { get; set; } = new List<string>();
			public List<string> Warnings { get; set; } = new List<string>();
		}

		/// <summary>
		/// اعتبارسنجی Query برای جلوگیری از SQL Injection و دستورات خطرناک
		/// </summary>
		public ValidationResult ValidateQuery(string query)
		{
			var result = new ValidationResult { IsValid = true };

			if (string.IsNullOrWhiteSpace(query))
			{
				result.IsValid = false;
				result.Errors.Add("Query نمی‌تواند خالی باشد");
				return result;
			}

			// حذف کامنت‌ها و فضاهای خالی اضافی
			string cleanedQuery = RemoveComments(query);

			// 1. بررسی دستورات خطرناک
			CheckDangerousKeywords(cleanedQuery, result);

			// 2. بررسی الگوهای خطرناک
			CheckDangerousPatterns(cleanedQuery, result);

			// 3. بررسی چند دستوری بودن (SQL Injection)
			CheckMultipleStatements(cleanedQuery, result);

			// 4. بررسی استفاده از Dynamic SQL
			CheckDynamicSql(cleanedQuery, result);

			// 5. بررسی استفاده از توابع سیستمی خطرناک
			CheckSystemFunctions(cleanedQuery, result);

			// 6. بررسی تنها SELECT بودن
			CheckOnlySelectAllowed(cleanedQuery, result);

			return result;
		}

		private string RemoveComments(string query)
		{
			// حذف کامنت‌های -- 
			query = Regex.Replace(query, @"--[^\r\n]*", "", RegexOptions.Multiline);

			// حذف کامنت‌های /* */
			query = Regex.Replace(query, @"/\*.*?\*/", "", RegexOptions.Singleline);

			return query;
		}

		private void CheckDangerousKeywords(string query, ValidationResult result)
		{
			var words = Regex.Split(query, @"\W+");

			foreach (var word in words)
			{
				if (DangerousKeywords.Contains(word))
				{
					result.IsValid = false;
					result.Errors.Add($"استفاده از دستور '{word}' مجاز نیست");
				}
			}
		}

		private void CheckDangerousPatterns(string query, ValidationResult result)
		{
			foreach (var pattern in DangerousPatterns)
			{
				if (query.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					result.IsValid = false;
					result.Errors.Add($"استفاده از '{pattern}' مجاز نیست");
				}
			}
		}

		private void CheckMultipleStatements(string query, ValidationResult result)
		{
			// بررسی وجود ; برای جداسازی دستورات
			var statements = query.Split(';')
			    .Select(s => s.Trim())
			    .Where(s => !string.IsNullOrWhiteSpace(s))
			    .ToList();

			if (statements.Count > 1)
			{
				result.IsValid = false;
				result.Errors.Add("اجرای چند دستور همزمان (با استفاده از ;) مجاز نیست");
			}
		}

		private void CheckDynamicSql(string query, ValidationResult result)
		{
			var dynamicSqlPatterns = new[]
			{
		  @"EXEC\s*\(",
		  @"EXECUTE\s*\(",
		  @"sp_executesql"
	   };

			foreach (var pattern in dynamicSqlPatterns)
			{
				if (Regex.IsMatch(query, pattern, RegexOptions.IgnoreCase))
				{
					result.IsValid = false;
					result.Errors.Add("استفاده از Dynamic SQL مجاز نیست");
					break;
				}
			}
		}

		private void CheckSystemFunctions(string query, ValidationResult result)
		{
			var systemFunctions = new[]
			{
		  "xp_", "sp_password", "sp_addlogin", "sp_droplogin",
		  "master..", "msdb..", "tempdb.."
	   };

			foreach (var func in systemFunctions)
			{
				if (query.IndexOf(func, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					result.IsValid = false;
					result.Errors.Add($"استفاده از تابع سیستمی '{func}' مجاز نیست");
				}
			}
		}

		private void CheckOnlySelectAllowed(string query, ValidationResult result)
		{
			// تنها SELECT و WITH (برای CTE) مجاز است
			var trimmedQuery = query.Trim().ToUpper();

			if (!trimmedQuery.StartsWith("SELECT") && !trimmedQuery.StartsWith("WITH"))
			{
				result.IsValid = false;
				result.Errors.Add("تنها دستورات SELECT و CTE (WITH) مجاز هستند");
			}

			// بررسی وجود INTO (SELECT INTO)
			if (Regex.IsMatch(query, @"\bINTO\b", RegexOptions.IgnoreCase))
			{
				result.IsValid = false;
				result.Errors.Add("استفاده از SELECT INTO مجاز نیست");
			}
		}
	}
}
