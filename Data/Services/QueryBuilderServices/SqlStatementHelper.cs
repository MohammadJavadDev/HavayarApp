using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Data.Services.QueryBuilderServices
{
	/// <summary>
	/// کمک‌کننده برای کار با کوئری‌های چند-دستوری (چند SELECT جدا شده با ;)
	/// </summary>
	public static class SqlStatementHelper
	{
		public const string MainSectionMarker = "--!--mainsection";

		/// <summary>
		/// تفکیک یک متن SQL به دستورات مجزا بر اساس ; در سطح بالا (با آگاهی از رشته‌های تحت‌الفظی
		/// تا ; داخل '...' یا "..." به اشتباه جداکننده در نظر گرفته نشود). دستورات خالی حذف می‌شوند.
		/// </summary>
		public static List<string> SplitTopLevelStatements(string sql)
		{
			var statements = new List<string>();
			if (string.IsNullOrWhiteSpace(sql))
				return statements;

			var current = new StringBuilder();
			char? stringDelimiter = null;

			for (var i = 0; i < sql.Length; i++)
			{
				var ch = sql[i];

				if (stringDelimiter.HasValue)
				{
					current.Append(ch);
					if (ch == stringDelimiter.Value)
					{
						// دو کوتیشن پشت‌سرهم یعنی escape شده - همچنان داخل رشته باقی می‌مانیم
						var isEscaped = i + 1 < sql.Length && sql[i + 1] == stringDelimiter.Value;
						if (isEscaped)
						{
							current.Append(sql[i + 1]);
							i++;
						}
						else
						{
							stringDelimiter = null;
						}
					}
					continue;
				}

				if (ch == '\'' || ch == '"')
				{
					stringDelimiter = ch;
					current.Append(ch);
					continue;
				}

				if (ch == ';')
				{
					var stmt = current.ToString().Trim();
					if (!string.IsNullOrWhiteSpace(stmt))
						statements.Add(stmt);
					current.Clear();
					continue;
				}

				current.Append(ch);
			}

			var last = current.ToString().Trim();
			if (!string.IsNullOrWhiteSpace(last))
				statements.Add(last);

			return statements;
		}

		/// <summary>
		/// درج TOP n بلافاصله بعد از اولین کلمه SELECT یک دستور (در صورتی که از قبل TOP نداشته باشد
		/// و دستور EXEC/EXECUTE نباشد). برای محدود کردن تعداد رکوردهای پیش‌نمایش استفاده می‌شود.
		/// </summary>
		public static string InjectTopN(string statement, int n)
		{
			if (string.IsNullOrWhiteSpace(statement))
				return statement;

			var mainSectionIndex = statement.IndexOf(MainSectionMarker, StringComparison.OrdinalIgnoreCase);
			if (mainSectionIndex >= 0)
			{
				var mainQueryIndex = mainSectionIndex + MainSectionMarker.Length;
				var mainQuery = statement.Substring(mainQueryIndex);
				return statement.Substring(0, mainQueryIndex) + InjectTopN(mainQuery, n);
			}

			var trimmedUpper = statement.TrimStart().ToUpperInvariant();
			if (trimmedUpper.StartsWith("EXEC"))
				return statement;

			if (Regex.IsMatch(statement, @"^\s*SELECT\s+TOP\b", RegexOptions.IgnoreCase))
				return statement;

			var match = Regex.Match(statement, @"\bSELECT\b(\s+DISTINCT\b)?", RegexOptions.IgnoreCase);
			if (!match.Success)
				return statement;

			var insertAt = match.Index + match.Length;
			return statement.Substring(0, insertAt) + $" TOP {n} " + statement.Substring(insertAt);
		}

		/// <summary>
		/// بازساخت متن کامل کوئری از روی لیست دستورات (جدا شده با ;)
		/// </summary>
		public static string JoinStatements(IEnumerable<string> statements)
		{
			return string.Join(";\n", statements);
		}
	}
}
