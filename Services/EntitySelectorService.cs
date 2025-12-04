using Common.Entities;
using Data;
using Microsoft.EntityFrameworkCore;
using Services.InMemoryData;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
			string searchCols = definition.SearchCols;  


			string searchTerm = string.IsNullOrEmpty(request.Q) ? "%" : $"%{request.Q}%";
			string runnableSql = cachedSql.Replace(TOKEN_LITERAL_SQL, "@searchTerm").Replace(TOKEN_LITERAL_SQL_ALT, "@searchTerm");

			// Keyset Pagination
			string seekSql = (request.LastId.HasValue && request.LastId.Value > 0) ? "AND [Id] > @lastId" : "";

			 
			string finalSql = $@"
                WITH BaseQuery AS ( {runnableSql} )
                SELECT TOP ({request.PageSize}) *, COUNT(*) OVER() as TotalCount
                FROM BaseQuery WHERE (1=1 {seekSql}) ORDER BY [Id] ASC";

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
				 

				// استفاده از SequentialAccess برای پرفورمنس بهتر
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

	 
		public List<EntitySelectorItem> GetInitialItemsSync(List<string> ids, string cleanSql, Dictionary<string, string> sqlParams, string displayTemplate, string[] colMap)
		{
			string runnableSql = cleanSql.Replace(TOKEN_LITERAL_SQL, "'%'").Replace(TOKEN_LITERAL_SQL_ALT, "'%'");
			var idParamNames = new List<string>();
			for (int i = 0; i < ids.Count; i++)
			{
				if (long.TryParse(ids[i] , out var x))
				{
					idParamNames.Add($"@idVal{i}");
				}
			} 

			string finalSql = $@"WITH BaseQuery AS ( {runnableSql} ) SELECT * FROM BaseQuery WHERE Id IN ({string.Join(",", idParamNames)})";

			var resultMap = new Dictionary<string, EntitySelectorItem>();
			var connection = _dbContext.Database.GetDbConnection();
			if (connection.State != ConnectionState.Open) connection.Open(); // Sync Open

			using (var command = connection.CreateCommand())
			{
				command.CommandText = finalSql;
				for (int i = 0; i < ids.Count; i++)
				{
					if (long.TryParse(ids[i], out var x))
					{
						AddParameter(command, idParamNames[i], x, true);
					}
				
				} 
				foreach (var kvp in sqlParams) AddParameter(command, kvp.Key, kvp.Value, true);

				using (var reader = command.ExecuteReader(CommandBehavior.SequentialAccess))
				{
					while (reader.Read())
					{
						var row = ReadRow(reader, colMap, out _);
						string idVal = row.ContainsKey("Id") ? row["Id"]?.ToString() : null;
						if (idVal != null)
						{
							resultMap[idVal] = new EntitySelectorItem { Id = idVal, Display = FormatDisplay(row, displayTemplate), Row = row };
						}
					}
				}
			}

			// بازگرداندن به ترتیب ورودی
			var results = new List<EntitySelectorItem>();
			foreach (var id in ids) if (resultMap.ContainsKey(id)) results.Add(resultMap[id]);
			return results;
		}

		public async Task<List<EntitySelectorItem>> GetItemsByIdsAsync(List<string> ids, string queryId, string encryptedParams, string displayTemplate, string colMapStr)
		{
			 
			return new List<EntitySelectorItem>();
		}

 

		private Dictionary<string, object> ReadRow(IDataReader reader, string[] colMap, out int totalCount)
		{
			totalCount = -1;
			var row = new Dictionary<string, object>(reader.FieldCount);
			for (int i = 0; i < reader.FieldCount; i++)
			{
				string colName = reader.GetName(i);
				
				// Use colMap as the source of truth for column names (especially for nested properties)
				// This ensures that flattened property names (e.g., Parent_Parent_Title) are correctly mapped
				if (i < colMap.Length && !string.IsNullOrEmpty(colMap[i]))
				{
					colName = colMap[i];
				}
				else if (string.IsNullOrEmpty(colName))
				{
					colName = "Col" + i;
				}
				
				// Skip duplicate Id columns
				if (colName.StartsWith("Id_") && i < colMap.Length && colMap[i] == "Id") 
				{
					continue;
				}

				if (colName == "TotalCount") 
				{ 
					totalCount = Convert.ToInt32(reader[i]); 
					continue; 
				}

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
			
			// First, replace nested properties (e.g., {Parent.Parent.Title} with flattened keys like {Parent_Parent_Title})
			// This handles properties that were flattened in the projection (Parent.Parent.Title -> Parent_Parent_Title)
			var nestedPattern = new System.Text.RegularExpressions.Regex(@"\{([^}]+)\}");
			display = nestedPattern.Replace(display, match =>
			{
				var propPath = match.Groups[1].Value;
				// If it contains dots, try to find the flattened version
				if (propPath.Contains('.'))
				{
					var flattenedKey = propPath.Replace(".", "_");
					if (row.ContainsKey(flattenedKey))
					{
						return row[flattenedKey]?.ToString() ?? "";
					}
				}
				// Try direct key match
				if (row.ContainsKey(propPath))
				{
					return row[propPath]?.ToString() ?? "";
				}
				// If not found, return empty string
				return "";
			});
			
			return string.IsNullOrWhiteSpace(display) ? "No Title" : display;
		}
	}
}
