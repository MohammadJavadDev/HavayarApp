using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Data.Services.QueryBuilderServices
{
	public class SqlQueryValidator
	{
		private static readonly HashSet<string> DangerousKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
	   "INSERT", "UPDATE", "DELETE", "DROP", "CREATE", "ALTER", "TRUNCATE",
	   "SP_", "XP_", "MERGE", "INTO", "SET", "GRANT",
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

			CheckMainSectionMarker(query, result);

			// حذف کامنت‌ها و فضاهای خالی اضافی
			string cleanedQuery = RemoveComments(query);

			// 1. بررسی دستورات خطرناک
			CheckDangerousKeywords(cleanedQuery, result);

			// 2. بررسی الگوهای خطرناک
			CheckDangerousPatterns(cleanedQuery, result);

			// 3. بررسی استفاده از Dynamic SQL
			CheckDynamicSql(cleanedQuery, result);

			// 4. بررسی استفاده از توابع سیستمی خطرناک
			CheckSystemFunctions(cleanedQuery, result);

			// 5. بررسی چند دستوری بودن (فقط چند SELECT/WITH پشت سرهم مجاز است) و
			//    بررسی این‌که هر دستور فقط SELECT/CTE/EXEC باشد
			CheckStatements(cleanedQuery, result);

			return result;
		}

		private string RemoveComments(string query)
		{
			const string mainSectionPlaceholder = "__HAVAYAR_QUERY_MAIN_SECTION__";
			query = Regex.Replace(
				query,
				Regex.Escape(SqlStatementHelper.MainSectionMarker),
				mainSectionPlaceholder,
				RegexOptions.IgnoreCase);

			// حذف کامنت‌های -- 
			query = Regex.Replace(query, @"--[^\r\n]*", "", RegexOptions.Multiline);

			// حذف کامنت‌های /* */
			query = Regex.Replace(query, @"/\*.*?\*/", "", RegexOptions.Singleline);

			return query.Replace(
				mainSectionPlaceholder,
				SqlStatementHelper.MainSectionMarker,
				StringComparison.Ordinal);
		}

		private static void CheckMainSectionMarker(string query, ValidationResult result)
		{
			var firstMarkerIndex = query.IndexOf(
				SqlStatementHelper.MainSectionMarker,
				StringComparison.OrdinalIgnoreCase);

			if (firstMarkerIndex < 0)
				return;

			var secondMarkerIndex = query.IndexOf(
				SqlStatementHelper.MainSectionMarker,
				firstMarkerIndex + SqlStatementHelper.MainSectionMarker.Length,
				StringComparison.OrdinalIgnoreCase);

			if (secondMarkerIndex >= 0)
			{
				result.IsValid = false;
				result.Errors.Add("نشانگر بخش اصلی Query فقط یک‌بار مجاز است");
				return;
			}

			var mainQuery = query.Substring(firstMarkerIndex + SqlStatementHelper.MainSectionMarker.Length);
			mainQuery = RemoveLeadingComments(mainQuery).TrimStart();
			if (!mainQuery.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
			{
				result.IsValid = false;
				result.Errors.Add("بعد از نشانگر --!--mainsection باید SELECT اصلی قرار گیرد");
			}
		}

		private static string RemoveLeadingComments(string query)
		{
			while (true)
			{
				var trimmed = query.TrimStart();
				if (trimmed.StartsWith("--", StringComparison.Ordinal))
				{
					var lineEnd = trimmed.IndexOfAny(new[] { '\r', '\n' });
					return lineEnd < 0 ? string.Empty : RemoveLeadingComments(trimmed.Substring(lineEnd + 1));
				}

				if (trimmed.StartsWith("/*", StringComparison.Ordinal))
				{
					var commentEnd = trimmed.IndexOf("*/", StringComparison.Ordinal);
					return commentEnd < 0 ? string.Empty : RemoveLeadingComments(trimmed.Substring(commentEnd + 2));
				}

				return trimmed;
			}
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

		/// <summary>
		/// حداکثر تعداد دستور SELECT/WITH مجاز در یک Query چند-دستوری (برای جلوگیری از سوءاستفاده)
		/// </summary>
		private const int MaxAllowedStatements = 20;

		/// <summary>
		/// بررسی دستورات Query: چند دستور جدا شده با ; فقط زمانی مجاز است که همگی SELECT یا WITH (CTE) باشند.
		/// EXEC/EXECUTE فقط زمانی مجاز است که تنها دستور موجود باشد (نه در ترکیب با دستورات دیگر).
		/// </summary>
		private void CheckStatements(string query, ValidationResult result)
		{
			var statements = SqlStatementHelper.SplitTopLevelStatements(query);

			if (statements.Count == 0)
			{
				result.IsValid = false;
				result.Errors.Add("Query نمی‌تواند خالی باشد");
				return;
			}

			if (statements.Count > MaxAllowedStatements)
			{
				result.IsValid = false;
				result.Errors.Add($"حداکثر {MaxAllowedStatements} دستور SELECT در یک Query مجاز است");
				return;
			}

			var isMultiStatement = statements.Count > 1;
			var hasMainSectionMarker = query.IndexOf(
				SqlStatementHelper.MainSectionMarker,
				StringComparison.OrdinalIgnoreCase) >= 0;
			var reachedMainSection = false;

			foreach (var statement in statements)
			{
				var trimmedUpper = statement.Trim().ToUpperInvariant();
				var containsMainSectionMarker = statement.IndexOf(
					SqlStatementHelper.MainSectionMarker,
					StringComparison.OrdinalIgnoreCase) >= 0;

				var isSelectOrCte = trimmedUpper.StartsWith("SELECT") || trimmedUpper.StartsWith("WITH");
				var isExec = trimmedUpper.StartsWith("EXEC ") || trimmedUpper.StartsWith("EXECUTE ")
					|| trimmedUpper == "EXEC" || trimmedUpper == "EXECUTE";
				var isSafeSetupDeclaration = hasMainSectionMarker
					&& !reachedMainSection
					&& !containsMainSectionMarker
					&& IsSafeScalarDeclaration(statement);

				if (isExec)
				{
					if (isMultiStatement)
					{
						result.IsValid = false;
						result.Errors.Add("استفاده از EXEC در ترکیب با چند دستور دیگر مجاز نیست");
					}
					continue;
				}

				if (!isSelectOrCte && !isSafeSetupDeclaration)
				{
					result.IsValid = false;
					result.Errors.Add(isMultiStatement
						? "در حالت چند SELECT، تمام دستورات باید SELECT یا WITH (CTE) باشند"
						: "تنها دستورات SELECT، CTE (WITH) و EXEC (Stored Procedure) مجاز هستند");
				}

				// بررسی وجود INTO (SELECT INTO) در هر دستور
				if (Regex.IsMatch(statement, @"\bINTO\b", RegexOptions.IgnoreCase))
				{
					result.IsValid = false;
					result.Errors.Add("استفاده از SELECT INTO مجاز نیست");
				}

				if (containsMainSectionMarker)
					reachedMainSection = true;
			}
		}

		private static bool IsSafeScalarDeclaration(string statement)
		{
			const string scalarType =
				@"(?:BIGINT|INT|SMALLINT|TINYINT|BIT|DECIMAL(?:\s*\(\s*\d+\s*,\s*\d+\s*\))?|" +
				@"NUMERIC(?:\s*\(\s*\d+\s*,\s*\d+\s*\))?|MONEY|SMALLMONEY|FLOAT|REAL|" +
				@"DATE|DATETIME2?(?:\s*\(\s*\d+\s*\))?|SMALLDATETIME|TIME(?:\s*\(\s*\d+\s*\))?|" +
				@"UNIQUEIDENTIFIER|N?VARCHAR\s*\(\s*(?:\d+|MAX)\s*\)|N?CHAR\s*\(\s*\d+\s*\))";

			return Regex.IsMatch(
				statement,
				$@"^\s*DECLARE\s+@[A-Za-z_][A-Za-z0-9_]*\s+{scalarType}\s*$",
				RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
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

	}
}
