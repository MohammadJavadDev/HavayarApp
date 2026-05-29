using Common.Entities;
using Common.Utilities;
using Data;
using Microsoft.EntityFrameworkCore;
using Services.InMemoryData;
using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
 

namespace Services
{
	public class EntitySelectorService : IEntitySelectorService
	{
		private readonly ApplicationDbContext _dbContext;
		private readonly SqlCacheStore _sqlStore;
		private readonly PayloadProtector _protector;

		private const string TOKEN_LITERAL_SQL = "N'%***SEARCH*TOKEN***%'";
		private const string TOKEN_LITERAL_SQL_ALT = "'%***SEARCH*TOKEN***%'";

		public EntitySelectorService(ApplicationDbContext dbContext, SqlCacheStore sqlStore, PayloadProtector protector)
		{
			_dbContext = dbContext;
			_sqlStore = sqlStore;
			_protector = protector;
		}

		public async Task<EntitySelectorResult> QueryAsync(string entityName, EntitySelectorRequest request)
		{
			if (request.Extra == null || !request.Extra.TryGetValue("queryId", out string queryId))
				return new EntitySelectorResult();

			var definition = _sqlStore.Get(queryId);
			if (definition == null) throw new Exception("Query expired or invalid.");

			string cachedSql = definition.Sql;
			string displayTemplate = definition.DisplayTemplate;
			string[] colMap = definition.ColMap;

			// ← اطلاعات ستون‌ها مستقیم از cache، بدون نیاز به JS
			var searchColumnInfos = definition.SearchColumnInfos ?? new List<SearchColumnInfo>();

			var extraFilters = new Dictionary<string, string>();
			if (request.Extra.TryGetValue("extraFilters", out string extraFiltersJson) && !string.IsNullOrEmpty(extraFiltersJson))
			{
				try { extraFilters = JsonSerializer.Deserialize<Dictionary<string, string>>(extraFiltersJson); }
				catch { }
			}

			string searchTerm = string.IsNullOrWhiteSpace(request.Q) ? "%" : $"%{request.Q.Trim()}%";

			// ← searchColumnInfos به BuildFinalSql پاس می‌شود
			string finalSql = BuildFinalSql(cachedSql, extraFilters, request.LastId, request.PageSize, searchColumnInfos);

			var resultItems = new List<EntitySelectorItem>();
			int totalCount = 0;

			var connection = _dbContext.Database.GetDbConnection();
			if (connection.State != ConnectionState.Open) await connection.OpenAsync();

			using (var command = connection.CreateCommand())
			{
				command.CommandText = finalSql;
				AddParameter(command, "@searchTerm", searchTerm);
				if (request.LastId.HasValue) AddParameter(command, "@lastId", request.LastId.Value);

				if (request.Extra.TryGetValue("defaultParams", out string encryptedParams))
					AddDefaultParams(command, encryptedParams);

				foreach (var filter in extraFilters)
					AddParameter(command, $"@extra_{filter.Key}", filter.Value);

				using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess))
				{
					while (await reader.ReadAsync())
					{
						var row = ReadRow(reader, colMap, out int count);
						if (count > -1) totalCount = count;

						resultItems.Add(new EntitySelectorItem
						{
							Id = row.ContainsKey("Id") ? row["Id"] : null,
							Display = FormatDisplay(row, displayTemplate),
							Row = row
						});
					}
				}
			}

			return new EntitySelectorResult { Page = request.Page, PageSize = request.PageSize, Total = totalCount, Items = resultItems };
		}

		private string BuildFinalSql(
	    string baseSql,
	    Dictionary<string, string> extraFilters,
	    long? lastId,
	    int pageSize,
	    List<SearchColumnInfo> searchColumnInfos = null)
		{
			string runnableSql = baseSql
			    .Replace(TOKEN_LITERAL_SQL, "@searchTerm")
			    .Replace(TOKEN_LITERAL_SQL_ALT, "@searchTerm");

			var whereConditions = new List<string>();

			if (searchColumnInfos != null && searchColumnInfos.Any())
			{
				var searchClauses = searchColumnInfos.Select(col =>
				    col.IsString
					   // ← SqlAlias به جای ColumnName
					   ? $"([{col.SqlAlias}] IS NOT NULL AND [{col.SqlAlias}] LIKE @searchTerm)"
					   : $"CAST([{col.SqlAlias}] AS NVARCHAR(100)) LIKE @searchTerm"
				);
				whereConditions.Add("(" + string.Join(" OR ", searchClauses) + ")");
			}

			foreach (var filter in extraFilters)
				whereConditions.Add($"[{filter.Key}] = @extra_{filter.Key}");

			string seekCondition = (lastId.HasValue && lastId.Value > 0) ? "AND [Id] > @lastId" : "";
			string additionalWhere = whereConditions.Any()
			    ? " AND " + string.Join(" AND ", whereConditions)
			    : "";

			return $@"
        WITH BaseQuery AS (
            {runnableSql}
        )
        SELECT TOP ({pageSize}) *, COUNT(*) OVER() AS TotalCount
        FROM BaseQuery
        WHERE 1=1 {seekCondition} {additionalWhere}
        ORDER BY [Id] ASC";
		}
		/// <summary>
		/// اضافه کردن CAST برای ستون‌های غیر string در SQL
		/// </summary>
		private string AddCastToNonStringColumns(string sql, List<SearchColumnInfo> searchColumns)
		{
			if (searchColumns == null || searchColumns.Count == 0)
				return sql;

			string modifiedSql = sql;

			foreach (var col in searchColumns.Where(c => !c.IsString))
			{
				// پیدا کردن pattern: [ColumnName] LIKE
				// و تبدیل به: CAST([ColumnName] AS NVARCHAR(50)) LIKE

				string columnPattern = $@"\[{Regex.Escape(col.ColumnName)}\]\s+(LIKE|like)";
				string replacement = $"CAST([{col.ColumnName}] AS NVARCHAR(50)) LIKE";

				modifiedSql = Regex.Replace(modifiedSql, columnPattern, replacement, RegexOptions.IgnoreCase);
			}

			return modifiedSql;
		}

		public List<EntitySelectorItem> GetInitialItemsSync(List<string> ids, string cleanSql, Dictionary<string, string> sqlParams, string displayTemplate, string[] colMap)
		{
			if (ids == null || ids.Count == 0)
				return new List<EntitySelectorItem>();

			string runnableSql = cleanSql
			    .Replace(TOKEN_LITERAL_SQL, "'%'")
			    .Replace(TOKEN_LITERAL_SQL_ALT, "'%'");

			var idParamNames = new List<string>();
			var idValues = new List<long>();
			foreach (var id in ids)
			{
				if (long.TryParse(id, out long idLong))
				{
					idParamNames.Add($"@idVal{idParamNames.Count}");
					idValues.Add(idLong);
				}
			}

			if (idParamNames.Count == 0)
				return new List<EntitySelectorItem>();

			// ساخت لیست ستون‌ها (بدون تغییر نام برای کوئری)
			string columnList = string.Join(", ", colMap.Select(c => $"[{c}]"));
			string finalSql = $@"
WITH BaseQuery AS ( {runnableSql} )
SELECT {columnList}
FROM BaseQuery 
WHERE Id IN ({string.Join(",", idParamNames)})
ORDER BY Id ASC";

			var resultMap = new Dictionary<string, EntitySelectorItem>();
			var connection = _dbContext.Database.GetDbConnection();
			if (connection.State != ConnectionState.Open)
				connection.Open();

			using (var command = connection.CreateCommand())
			{
				command.CommandText = finalSql;
				for (int i = 0; i < idValues.Count; i++)
					AddParameter(command, idParamNames[i], idValues[i], true);
				foreach (var kvp in sqlParams)
					AddParameter(command, kvp.Key, kvp.Value, true);

				using (var reader = command.ExecuteReader(CommandBehavior.SequentialAccess))
				{
					while (reader.Read())
					{
						var row = new Dictionary<string, object>();
						for (int i = 0; i < colMap.Length; i++)
						{
							string colName = colMap[i];
							object value = reader.IsDBNull(i) ? null : reader[i];
							 
							row[colName] = value;
						}

						string idVal = row.ContainsKey("Id") ? row["Id"]?.ToString() : null;  
						if (idVal != null)
						{
							resultMap[idVal] = new EntitySelectorItem
							{
								Id = idVal,
								Display = FormatDisplay(row, displayTemplate),
								Row = row
							};
						}
					}
				}
			}

			var results = new List<EntitySelectorItem>();
			foreach (var id in ids)
				if (resultMap.ContainsKey(id))
					results.Add(resultMap[id]);
			return results;
		}
		public async Task<List<EntitySelectorItem>> GetItemsByIdsAsync(List<string> ids, string queryId, string encryptedParams, string displayTemplate, string colMapStr)
		{
			if (ids == null || ids.Count == 0)
				return new List<EntitySelectorItem>();

			var definition = _sqlStore.Get(queryId);
			if (definition == null)
				throw new Exception("Query expired or invalid.");

			string cachedSql = definition.Sql;
			string template = string.IsNullOrEmpty(displayTemplate) ? definition.DisplayTemplate : displayTemplate;
			string[] colMap = string.IsNullOrEmpty(colMapStr) ? definition.ColMap : colMapStr.Split(',');

			string runnableSql = cachedSql
				.Replace(TOKEN_LITERAL_SQL, "'%'")
				.Replace(TOKEN_LITERAL_SQL_ALT, "'%'");

			var idParamNames = new List<string>();
			for (int i = 0; i < ids.Count; i++)
			{
				if (long.TryParse(ids[i], out _))
				{
					idParamNames.Add($"@idVal{i}");
				}
			}

			if (idParamNames.Count == 0)
				return new List<EntitySelectorItem>();

			string finalSql = $@"
WITH BaseQuery AS ( {runnableSql} )
SELECT * FROM BaseQuery 
WHERE Id IN ({string.Join(",", idParamNames)})
ORDER BY Id ASC";

			var resultMap = new Dictionary<string, EntitySelectorItem>();
			var connection = _dbContext.Database.GetDbConnection();
			if (connection.State != ConnectionState.Open)
				await connection.OpenAsync();

			using (var command = connection.CreateCommand())
			{
				command.CommandText = finalSql;

				for (int i = 0; i < ids.Count; i++)
				{
					if (long.TryParse(ids[i], out var idValue))
					{
						AddParameter(command, idParamNames[i], idValue, true);
					}
				}

				AddDefaultParams(command, encryptedParams);

				using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess))
				{
					while (await reader.ReadAsync())
					{
						var row = ReadRow(reader, colMap, out _);
						string idVal = row.ContainsKey("Id") ? row["Id"]?.ToString() : null;

						if (idVal != null)
						{
							resultMap[idVal] = new EntitySelectorItem
							{
								Id = idVal,
								Display = FormatDisplay(row, template),
								Row = row
							};
						}
					}
				}
			}

			var results = new List<EntitySelectorItem>();
			foreach (var id in ids)
			{
				if (resultMap.ContainsKey(id))
					results.Add(resultMap[id]);
			}

			return results;
		}

		private Dictionary<string, object> ReadRow(IDataReader reader, string[] colMap, out int totalCount)
		{
			totalCount = -1;
			var row = new Dictionary<string, object>(reader.FieldCount);
			for (int i = 0; i < reader.FieldCount; i++)
			{
				string colName = reader.GetName(i);
				if (colName == "TotalCount")
				{
					totalCount = Convert.ToInt32(reader[i]);
					continue;
				}

				// پیدا کردن نام ستون منطبق با colMap (بدون حساسیت به حروف)
				var matchedCol = colMap.FirstOrDefault(c => string.Equals(c, colName, StringComparison.OrdinalIgnoreCase));
				if (!string.IsNullOrEmpty(matchedCol))
					colName = matchedCol;

 
				row[colName] = reader.IsDBNull(i) ? null : reader[i];
			}
			return row;
		}
		private void AddParameter(IDbCommand cmd, string name, object value, bool autoType = false)
		{
			var p = cmd.CreateParameter();
			p.ParameterName = name;
			if (autoType && value is string sVal)
			{
				if (long.TryParse(sVal, out long l)) p.Value = l;
				else if (bool.TryParse(sVal, out bool b)) p.Value = b;
				else p.Value = sVal;
			}
			else p.Value = value ?? DBNull.Value;
			cmd.Parameters.Add(p);
		}

		private void AddDefaultParams(IDbCommand cmd, string encryptedParams)
		{
			if (string.IsNullOrEmpty(encryptedParams)) return;
			string json = _protector.Unprotect(encryptedParams);
			if (json == null) return;
			try
			{
				var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
				if (dict != null) foreach (var kv in dict) AddParameter(cmd, kv.Key, kv.Value, true);
			}
			catch { }
		}

		private string FormatDisplay(Dictionary<string, object> row, string template)
		{
			string display = template;
			var nestedPattern = new Regex(@"\{([^}]+)\}");
			display = nestedPattern.Replace(display, match =>
			{
				var propPath = match.Groups[1].Value; // مثلاً "hasReplySheet" یا "row.title"

			 
				string[] parts = propPath.Split('.');
				string camelPath = string.Join(".", parts.Select(p => p));

				object value = null;
				if (camelPath.Contains('.'))
				{
					// پشتیبانی از nested properties (در صورت وجود)
					var flattenedKey = camelPath.Replace(".", "_");
					if (row.ContainsKey(flattenedKey))
						value = row[flattenedKey];
					else if (row.ContainsKey(camelPath))
						value = row[camelPath];
				}
				else
				{
					if (row.ContainsKey(camelPath))
						value = row[camelPath];
				}

				return value?.ToString() ?? "";
			});
			return string.IsNullOrWhiteSpace(display) ? "No Title" : display;
		}
	}
	
}