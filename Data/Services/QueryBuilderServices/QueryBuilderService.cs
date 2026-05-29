using Entities.Base;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Data.Services.QueryBuilderServices
{
	/// <summary>
	/// سرویس ساخت Query از طراحی بصری
	/// </summary>
	public interface IQueryBuilderService
	{
		string BuildQuery(QueryDesign design, List<QueryColumn> reportColumns = null);
		string BuildQueryWithSorting(QueryDesign design, List<QueryColumn> columns);
		string BuildQueryWithShaping(QueryDesign design, List<QueryColumn> columns);
	}

	public class QueryBuilderService : IQueryBuilderService
	{
		/// <summary>
		/// ساخت Query از طراحی - reportColumns برای زمانی که Tables.Columns خالی است (گزارش ذخیره شده)
		/// </summary>
		public string BuildQuery(QueryDesign design, List<QueryColumn> reportColumns = null)
		{
			if (design == null || design.Tables == null || !design.Tables.Any())
				throw new ArgumentException("حداقل یک جدول باید انتخاب شود");

			var sb = new StringBuilder();

			// ایجاد Aliases یکتا برای جداول (T1, T2, T3...) برای جلوگیری از تداخل در JOIN و Filter
			var tableToAlias = BuildTableAliasMap(design.Tables);

			// SELECT clause با استفاده از Alias
			sb.AppendLine("SELECT");
			var selectedColumns = GetSelectedColumns(design, tableToAlias, reportColumns.Where(c=> !string.IsNullOrEmpty(c.Address)).ToList());
			if (!selectedColumns.Any())
				throw new ArgumentException("حداقل یک ستون باید انتخاب شود");

			sb.AppendLine($"    {string.Join($",{Environment.NewLine}    ", selectedColumns)}");

			// FROM clause - جدول اول با Alias
			var firstTable = design.Tables.First();
			var firstTableKey = $"{firstTable.Schema}.{firstTable.Name}";
			var firstAlias = tableToAlias[firstTableKey];
			sb.AppendLine($"FROM [{firstTable.Schema}].[{firstTable.Name}] AS [{firstAlias}]");

			// JOIN clauses با ترتیب صحیح (Topological Sort) - پشتیبانی دوطرفه: Source در joined یا Target در joined
			var joinedTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { firstTableKey };
			var relations = (design.Relations ?? Enumerable.Empty<TableRelation>()).ToList();

			while (relations.Any())
			{
				var addedAny = false;
				foreach (var relation in relations.ToList())
				{
					var sourceInJoined = joinedTables.Contains(relation.SourceTable);
					var targetInJoined = joinedTables.Contains(relation.TargetTable);

					// هر دو در query هستند - این relation قبلاً پوشش داده شده
					if (sourceInJoined && targetInJoined)
					{
						relations.Remove(relation);
						addedAny = true;
						continue;
					}

					// Case A: Source در joined → اضافه کردن Target
					if (sourceInJoined && !targetInJoined)
					{
						var tableToAdd = design.Tables.FirstOrDefault(t =>
							string.Equals($"{t.Schema}.{t.Name}", relation.TargetTable, StringComparison.OrdinalIgnoreCase));

						if (tableToAdd != null && tableToAlias.TryGetValue(relation.SourceTable, out var sourceAlias) &&
							tableToAlias.TryGetValue(relation.TargetTable, out var targetAlias))
						{
							var joinType = GetJoinKeyword(relation.JoinType);
							sb.AppendLine($"{joinType} [{tableToAdd.Schema}].[{tableToAdd.Name}] AS [{targetAlias}]");
							sb.AppendLine($"    ON [{sourceAlias}].[{relation.SourceColumn}] = [{targetAlias}].[{relation.TargetColumn}]");
							joinedTables.Add(relation.TargetTable);
							relations.Remove(relation);
							addedAny = true;
						}
						continue;
					}

					// Case B: Target در joined → اضافه کردن Source
					if (targetInJoined && !sourceInJoined)
					{
						var tableToAdd = design.Tables.FirstOrDefault(t =>
							string.Equals($"{t.Schema}.{t.Name}", relation.SourceTable, StringComparison.OrdinalIgnoreCase));

						if (tableToAdd != null && tableToAlias.TryGetValue(relation.SourceTable, out var sourceAlias) &&
							tableToAlias.TryGetValue(relation.TargetTable, out var targetAlias))
						{
							var joinType = GetJoinKeyword(relation.JoinType);
							sb.AppendLine($"{joinType} [{tableToAdd.Schema}].[{tableToAdd.Name}] AS [{sourceAlias}]");
							sb.AppendLine($"    ON [{sourceAlias}].[{relation.SourceColumn}] = [{targetAlias}].[{relation.TargetColumn}]");
							joinedTables.Add(relation.SourceTable);
							relations.Remove(relation);
							addedAny = true;
						}
					}
				}

				if (!addedAny)
					break;
			}

			// WHERE clause با استفاده از Alias
			if (design.Filters != null && design.Filters.Any())
			{
				sb.AppendLine("WHERE");
				var filterClauses = new List<string>();

				for (int i = 0; i < design.Filters.Count; i++)
				{
					var filter = design.Filters[i];
					var clause = BuildFilterClause(filter, tableToAlias);

					if (i > 0 && !string.IsNullOrEmpty(filter.LogicalOperator))
					{
						filterClauses.Add($"    {filter.LogicalOperator} {clause}");
					}
					else
					{
						filterClauses.Add($"    {clause}");
					}
				}

				sb.AppendLine(string.Join(Environment.NewLine, filterClauses));
			}

			return sb.ToString();
		}

		/// <summary>
		/// ساخت Query با Sort
		/// </summary>
		public string BuildQueryWithSorting(QueryDesign design, List<QueryColumn> columns)
		{
			var baseQuery = BuildQuery(design, columns);
			var tableToAlias = BuildTableAliasMap(design.Tables);

			var sortColumns = columns
				.Where(c => c.SortDirection != null)
				.OrderBy(c => c.SortOrder ?? int.MaxValue)
				.ToList();

			if (sortColumns.Any())
			{
				var sb = new StringBuilder(baseQuery.TrimEnd());
				sb.AppendLine();
				sb.AppendLine("ORDER BY");

				var orderByClauses = sortColumns.Select(c =>
				{
					var direction = c.SortDirection == SortDirection.Ascending ? "ASC" : "DESC";
					// استفاده از Alias - TableName ممکن است Schema.Table یا فقط Table باشد
					var alias = ResolveTableAlias(c.TableName, design.Tables, tableToAlias);
					return $"    [{alias}].[{c.ColumnName}] {direction}";
				});

				sb.AppendLine(string.Join($",{Environment.NewLine}", orderByClauses));
				return sb.ToString();
			}

			return baseQuery;
		}

		/// <summary>
		/// ساخت Query با Group By، Aggregate و Sort (مشابه DevExpress Shape Data)
		/// </summary>
		public string BuildQueryWithShaping(QueryDesign design, List<QueryColumn> columns)
		{
			if (design == null || design.Tables == null || !design.Tables.Any())
				throw new ArgumentException("حداقل یک جدول باید انتخاب شود");
			if (columns == null || !columns.Any())
				throw new ArgumentException("حداقل یک ستون باید انتخاب شود");

			var tableToAlias = BuildTableAliasMap(design.Tables);
			var hasGroupBy = columns.Any(c => c.GroupBy);
			var hasAggregate = columns.Any(c => !string.IsNullOrEmpty(c.Aggregate));

			if (hasGroupBy || hasAggregate)
			{
				foreach (var c in columns)
				{
					if (!c.GroupBy && string.IsNullOrEmpty(c.Aggregate))
						throw new ArgumentException($"ستون {c.TableName}.{c.ColumnName} باید Group By یا Aggregate داشته باشد وقتی ستون‌های دیگر دارند");
				}
			}

			var sb = new StringBuilder();
			sb.AppendLine("SELECT");
			var selectClauses = BuildShapedSelectClauses(columns, tableToAlias, design.Tables);
			sb.AppendLine($"    {string.Join($",{Environment.NewLine}    ", selectClauses)}");

			sb.Append(BuildQueryFromAndJoins(design, tableToAlias));
			sb.Append(BuildQueryWhere(design, tableToAlias));

			if (hasGroupBy)
			{
				var groupByColumns = columns.Where(c => c.GroupBy).ToList();
				if (groupByColumns.Any())
				{
					sb.AppendLine("GROUP BY");
					var groupByClauses = groupByColumns.Select(c =>
					{
						var alias = ResolveTableAlias(c.TableName, design.Tables, tableToAlias);
						return $"    [{alias}].[{c.ColumnName}]";
					});
					sb.AppendLine(string.Join($",{Environment.NewLine}", groupByClauses));
				}
			}

			var sortColumns = columns.Where(c => c.SortDirection != null).OrderBy(c => c.SortOrder ?? int.MaxValue).ToList();
			if (sortColumns.Any())
			{
				sb.AppendLine("ORDER BY");
				var orderByClauses = sortColumns.Select(c =>
				{
					var direction = c.SortDirection == SortDirection.Ascending ? "ASC" : "DESC";
					var alias = ResolveTableAlias(c.TableName, design.Tables, tableToAlias);
					if (!string.IsNullOrEmpty(c.Aggregate))
						return $"    {GetAggregateSql(c.Aggregate, alias, c.ColumnName)} {direction}";
					return $"    [{alias}].[{c.ColumnName}] {direction}";
				});
				sb.AppendLine(string.Join($",{Environment.NewLine}", orderByClauses));
			}

			return sb.ToString().TrimEnd();
		}

		private string GetAggregateSql(string aggregate, string alias, string columnName)
		{
			var colRef = $"[{alias}].[{columnName}]";
			return (aggregate?.ToUpperInvariant()) switch
			{
				"COUNT" => $"COUNT({colRef})",
				"MAX" => $"MAX({colRef})",
				"MIN" => $"MIN({colRef})",
				"AVG" => $"AVG({colRef})",
				"SUM" => $"SUM({colRef})",
				"COUNTDISTINCT" => $"COUNT(DISTINCT {colRef})",
				"AVGDISTINCT" => $"AVG(DISTINCT {colRef})",
				"SUMDISTINCT" => $"SUM(DISTINCT {colRef})",
				_ => colRef
			};
		}

		private List<string> BuildShapedSelectClauses(List<QueryColumn> columns, Dictionary<string, string> tableToAlias, List<TableInfo> tables)
		{
			var result = new List<string>();
			foreach (var c in columns)
			{
				var alias = ResolveTableAlias(c.TableName, tables, tableToAlias);
				var aliasName = $"{alias}_{c.ColumnName}";
				string expr;
				if (!string.IsNullOrEmpty(c.Aggregate))
					expr = GetAggregateSql(c.Aggregate, alias, c.ColumnName);
				else
					expr = $"[{alias}].[{c.ColumnName}]";
				result.Add($"{expr} AS [{aliasName}]");
			}
			return result;
		}

		private string BuildQueryFromAndJoins(QueryDesign design, Dictionary<string, string> tableToAlias)
		{
			var sb = new StringBuilder();
			var firstTable = design.Tables.First();
			var firstTableKey = $"{firstTable.Schema}.{firstTable.Name}";
			var firstAlias = tableToAlias[firstTableKey];
			sb.AppendLine($"FROM [{firstTable.Schema}].[{firstTable.Name}] AS [{firstAlias}]");

			var joinedTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { firstTableKey };
			var relations = (design.Relations ?? Enumerable.Empty<TableRelation>()).ToList();

			while (relations.Any())
			{
				var addedAny = false;
				foreach (var relation in relations.ToList())
				{
					var sourceInJoined = joinedTables.Contains(relation.SourceTable);
					var targetInJoined = joinedTables.Contains(relation.TargetTable);
					if (sourceInJoined && targetInJoined) { relations.Remove(relation); addedAny = true; continue; }
					if (sourceInJoined && !targetInJoined)
					{
						var tableToAdd = design.Tables.FirstOrDefault(t => string.Equals($"{t.Schema}.{t.Name}", relation.TargetTable, StringComparison.OrdinalIgnoreCase));
						if (tableToAdd != null && tableToAlias.TryGetValue(relation.SourceTable, out var sourceAlias) && tableToAlias.TryGetValue(relation.TargetTable, out var targetAlias))
						{
							sb.AppendLine($"{GetJoinKeyword(relation.JoinType)} [{tableToAdd.Schema}].[{tableToAdd.Name}] AS [{targetAlias}]");
							sb.AppendLine($"    ON [{sourceAlias}].[{relation.SourceColumn}] = [{targetAlias}].[{relation.TargetColumn}]");
							joinedTables.Add(relation.TargetTable); relations.Remove(relation); addedAny = true;
						}
						continue;
					}
					if (targetInJoined && !sourceInJoined)
					{
						var tableToAdd = design.Tables.FirstOrDefault(t => string.Equals($"{t.Schema}.{t.Name}", relation.SourceTable, StringComparison.OrdinalIgnoreCase));
						if (tableToAdd != null && tableToAlias.TryGetValue(relation.SourceTable, out var sourceAlias) && tableToAlias.TryGetValue(relation.TargetTable, out var targetAlias))
						{
							sb.AppendLine($"{GetJoinKeyword(relation.JoinType)} [{tableToAdd.Schema}].[{tableToAdd.Name}] AS [{sourceAlias}]");
							sb.AppendLine($"    ON [{sourceAlias}].[{relation.SourceColumn}] = [{targetAlias}].[{relation.TargetColumn}]");
							joinedTables.Add(relation.SourceTable); relations.Remove(relation); addedAny = true;
						}
					}
				}
				if (!addedAny) break;
			}
			return sb.ToString();
		}

		private string BuildQueryWhere(QueryDesign design, Dictionary<string, string> tableToAlias)
		{
			if (design.Filters == null || !design.Filters.Any()) return "";
			var sb = new StringBuilder();
			sb.AppendLine("WHERE");
			for (int i = 0; i < design.Filters.Count; i++)
			{
				var filter = design.Filters[i];
				var clause = BuildFilterClause(filter, tableToAlias);
				sb.AppendLine(i > 0 && !string.IsNullOrEmpty(filter.LogicalOperator) ? $"    {filter.LogicalOperator} {clause}" : $"    {clause}");
			}
			return sb.ToString();
		}

		/// <summary>
		/// ایجاد نقشه Alias یکتا برای جداول (T1, T2, T3...)
		/// </summary>
		private Dictionary<string, string> BuildTableAliasMap(List<TableInfo> tables)
		{
			var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			var index = 1;
			foreach (var table in tables)
			{
				var fullName = $"{table.Schema}.{table.Name}";
				if (!map.ContainsKey(fullName))
				{
					map[fullName] = $"T{index}";
					index++;
				}
			}
			return map;
		}

		/// <summary>
		/// دریافت لیست ستون‌های انتخاب شده با Alias - استفاده از table.Columns یا reportColumns وقتی Columns خالی است
		/// </summary>
		private List<string> GetSelectedColumns(QueryDesign design, Dictionary<string, string> tableToAlias, List<QueryColumn> reportColumns = null)
		{
			var columns = new List<string>();

			foreach (var table in design.Tables)
			{
				var fullName = $"{table.Schema}.{table.Name}";
				var alias = tableToAlias.TryGetValue(fullName, out var a) ? a : table.Name;

				// استفاده از table.Columns اگر موجود باشد
				if (table.Columns != null && table.Columns.Any())
				{
					foreach (var col in table.Columns.Where(c => c.IsSelected))
					{
						var colRef = $"[{alias}].[{col.Name}]";
						columns.Add($"{colRef} AS [{alias}_{col.Name}]");
					}
				}
				// وقتی Columns خالی است (گزارش ذخیره شده) از reportColumns استفاده می‌کنیم
				else if (reportColumns != null)
				{
					var tableCols = reportColumns.Where(c => string.Equals(c.TableName, fullName, StringComparison.OrdinalIgnoreCase));
					foreach (var rc in tableCols)
					{
						var colRef = $"[{alias}].[{rc.ColumnName}]";
						columns.Add($"{colRef} AS [{alias}_{rc.ColumnName}]");
					}
				}
			}

			return columns;
		}

		/// <summary>
		/// دریافت کلمه کلیدی JOIN
		/// </summary>
		private string GetJoinKeyword(string joinType)
		{
			return (joinType?.ToUpperInvariant()) switch
			{
				"INNER" => "INNER JOIN",
				"LEFT" => "LEFT JOIN",
				"RIGHT" => "RIGHT JOIN",
				_ => "INNER JOIN"
			};
		}

		/// <summary>
		/// ساخت شرط Filter با استفاده از Alias
		/// </summary>
		private string BuildFilterClause(FilterCondition filter, Dictionary<string, string> tableToAlias)
		{
			var operatorName = filter.Operator?.ToLowerInvariant();
			var operatorSymbol = GetOperatorSymbol(filter.Operator);
			var alias = ResolveTableAlias(filter.TableName, null, tableToAlias);
			var columnRef = $"[{alias}].[{filter.ColumnName}]";

			// IS NULL و IS NOT NULL مقدار ندارند
			if (operatorName is "isnull" or "isnotnull")
				return $"{columnRef} {operatorSymbol}";

			// IN و NOT IN - مقدار به صورت لیست جدا شده با کاما (مثال: 1,2,3 یا 'a','b')
			if (operatorName is "in" or "notin")
			{
				var inValues = FormatInNotInValues(filter.Value, filter.DataType);
				return $"{columnRef} {operatorSymbol} ({inValues})";
			}

			// LIKE و NOT LIKE - برای پارامتر: N'%'+@Param+N'%' برای جستجوی contains
			if (operatorName is "like" or "notlike")
			{
				if (IsParameterReference(filter.Value))
					return $"{columnRef} {operatorSymbol} N'%'+{filter.Value.Trim()}+N'%'";
				var likeValue = FormatLikeValue(filter.Value, filter.DataType);
				return $"{columnRef} {operatorSymbol} {likeValue}";
			}

			// پارامتر SQL - استفاده مستقیم بدون format
			if (IsParameterReference(filter.Value))
				return $"{columnRef} {operatorSymbol} {filter.Value.Trim()}";

			var value = FormatValue(filter.Value, filter.DataType);
			return $"{columnRef} {operatorSymbol} {value}";
		}

		/// <summary>
		/// یافتن Alias جدول - پشتیبانی از format های Schema.Table و Table
		/// </summary>
		private string ResolveTableAlias(string tableRef, List<TableInfo> tables, Dictionary<string, string> tableToAlias)
		{
			if (string.IsNullOrEmpty(tableRef)) return "T1";

			// جستجو با نام کامل (Schema.Table)
			if (tableToAlias.TryGetValue(tableRef, out var alias))
				return alias;

			// جستجو با نام کوتاه (Table) - وقتی فقط نام جدول ارسال شده
			if (tables != null)
			{
				var table = tables.FirstOrDefault(t =>
					$"{t.Schema}.{t.Name}" == tableRef || t.Name == tableRef);
				if (table != null && tableToAlias.TryGetValue($"{table.Schema}.{table.Name}", out alias))
					return alias;
			}
			else
			{
				// جستجو در tableToAlias با نام کوتاه
				var match = tableToAlias.FirstOrDefault(kv =>
					kv.Key.EndsWith("." + tableRef, StringComparison.OrdinalIgnoreCase) || kv.Key.Equals(tableRef, StringComparison.OrdinalIgnoreCase));
				if (match.Key != null)
					return match.Value;
			}

			// fallback
			return GetTableNameFromFullName(tableRef) ?? "T1";
		}

		private static bool IsParameterReference(string value)
		{
			return !string.IsNullOrEmpty(value) && value.TrimStart().StartsWith("@") &&
				  Regex.IsMatch(value.Trim(), @"^@[a-zA-Z_][a-zA-Z0-9_]*$");
		}

		/// <summary>
		/// فرمت مقدار بر اساس نوع داده - N برای nvarchar/nchar برای پشتیبانی Unicode
		/// </summary>
		private string FormatValue(string value, string dataType)
		{
			if (string.IsNullOrEmpty(value))
				return "NULL";

			var lowerDataType = dataType?.ToLower() ?? "";
			var escapedValue = value.Replace("'", "''");

			// انواع عددی
			if (lowerDataType.Contains("int") || lowerDataType.Contains("decimal") ||
				lowerDataType.Contains("numeric") || lowerDataType.Contains("float") ||
				lowerDataType.Contains("money"))
			{
				return value;
			}

			// تاریخ و زمان
			if (lowerDataType.Contains("date") || lowerDataType.Contains("time"))
			{
				return $"'{escapedValue}'";
			}

			// بولین
			if (lowerDataType.Contains("bit"))
			{
				return value.ToLower() == "true" || value == "1" ? "1" : "0";
			}

			// nvarchar, nchar و انواع رشته Unicode - استفاده از N prefix
			if (lowerDataType.Contains("nvarchar") || lowerDataType.Contains("nchar") ||
				lowerDataType.Contains("ntext") || lowerDataType.Contains("national"))
			{
				return $"N'{escapedValue}'";
			}

			// varchar, char و سایر رشته‌ها - N prefix برای پشتیبانی بهتر از فارسی
			if (lowerDataType.Contains("varchar") || lowerDataType.Contains("char") ||
				lowerDataType.Contains("text") || string.IsNullOrEmpty(lowerDataType))
			{
				return $"N'{escapedValue}'";
			}

			// پیش‌فرض: رشته با N
			return $"N'{escapedValue}'";
		}

		/// <summary>
		/// فرمت مقادیر برای IN و NOT IN - مقدار به صورت لیست جدا شده با کاما
		/// </summary>
		private string FormatInNotInValues(string value, string dataType)
		{
			if (string.IsNullOrWhiteSpace(value))
				return "NULL";

			var parts = value.Split(',')
				.Select(s => s.Trim())
				.Where(s => !string.IsNullOrEmpty(s))
				.Select(s => FormatValue(s, dataType))
				.ToList();

			return parts.Any() ? string.Join(", ", parts) : "NULL";
		}

		/// <summary>
		/// فرمت مقدار برای LIKE - اضافه کردن % برای جستجوی contains
		/// </summary>
		private string FormatLikeValue(string value, string dataType)
		{
			if (string.IsNullOrEmpty(value))
				return "N'%%'";

			// اگر کاربر خودش % گذاشته، تغییر نده
			var trimmed = value.Trim();
			if (trimmed.Contains('%') || trimmed.Contains('_'))
				return FormatValue(value, dataType);

			return FormatValue($"%{value}%", dataType);
		}

		/// <summary>
		/// دریافت علامت Operator
		/// </summary>
		private string GetOperatorSymbol(string operatorName)
		{
			return operatorName?.ToLowerInvariant() switch
			{
				"equals" => "=",
				"notequals" => "!=",
				"greater" or "greaterthan" => ">",
				"less" or "lessthan" => "<",
				"greaterorequal" or "greaterthanorequal" => ">=",
				"lessorequal" or "lessthanorequal" => "<=",
				"like" => "LIKE",
				"notlike" => "NOT LIKE",
				"in" => "IN",
				"notin" => "NOT IN",
				"isnull" => "IS NULL",
				"isnotnull" => "IS NOT NULL",
				_ => "="
			};
		}

		/// <summary>
		/// استخراج نام جدول از نام کامل (Schema.Table)
		/// </summary>
		private string GetTableNameFromFullName(string fullName)
		{
			return fullName?.Contains('.') == true
			    ? fullName.Split('.')[1]
			    : fullName;
		}
	}
}
