using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Common.Utilities
{
	public static class SqlUtils
	{
		public static (string CleanSql, Dictionary<string, string> Params) ParseAndClean(string rawSql)
		{
			var paramsDict = new Dictionary<string, string>();

			// Regex برای استخراج DECLARE ها
			var regex = new Regex(@"DECLARE\s+(@\w+)\s+.+?\s*=\s*(.+?);", RegexOptions.IgnoreCase);

			var matches = regex.Matches(rawSql);
			foreach (Match match in matches)
			{
				string key = match.Groups[1].Value;
				string rawValue = match.Groups[2].Value.Trim();

				// تمیز کردن مقادیر (حذف CAST و N'...')
				var castMatch = Regex.Match(rawValue, @"CAST\((.+?)\s+AS\s+.+?\)", RegexOptions.IgnoreCase);
				if (castMatch.Success) rawValue = castMatch.Groups[1].Value.Trim();

				if (rawValue.StartsWith("N'") && rawValue.EndsWith("'")) rawValue = rawValue.Substring(2, rawValue.Length - 3);
				else if (rawValue.StartsWith("'") && rawValue.EndsWith("'")) rawValue = rawValue.Substring(1, rawValue.Length - 2);

				rawValue = rawValue.Trim('(', ')');
				paramsDict[key] = rawValue;
			}

			// حذف DECLARE ها از بدنه SQL
			string body = regex.Replace(rawSql, "").Trim();

			// حذف ORDER BY
			body = Regex.Replace(body, @"ORDER\s+BY\s+.*$", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);

			return (body, paramsDict);
		}

		public static string InjectIdIfMissing(string sql, bool hasId, out string alias)
		{
			alias = "";
			var selectMatch = Regex.Match(sql, @"SELECT.*", RegexOptions.Singleline | RegexOptions.IgnoreCase);
			string cleanSql = selectMatch.Success ? selectMatch.Value : sql;

			var aliasMatch = Regex.Match(cleanSql, @"FROM\s+\[.+?\]\s+AS\s+\[(\w+)\]", RegexOptions.IgnoreCase);
			if (aliasMatch.Success) alias = aliasMatch.Groups[1].Value;

			if (!hasId && !string.IsNullOrEmpty(alias))
			{
				var regexSelect = new Regex(@"SELECT\s", RegexOptions.IgnoreCase);
				cleanSql = regexSelect.Replace(cleanSql, $"SELECT [{alias}].[Id], ", 1);
			}

			// حل مشکل آیدی‌های تکراری
			var fromIndex = cleanSql.IndexOf("FROM", StringComparison.OrdinalIgnoreCase);
			if (fromIndex > 0)
			{
				string selectList = cleanSql.Substring(0, fromIndex);
				string rest = cleanSql.Substring(fromIndex);
				int idCounter = 0;
				string fixedList = Regex.Replace(selectList, @"(\[\w+\]\.\[Id\])", m =>
				{
					idCounter++;
					return idCounter > 1 ? $"{m.Value} AS [Id_{idCounter}]" : m.Value;
				});
				cleanSql = fixedList + rest;
			}

			return cleanSql;
		}
	}
	}
