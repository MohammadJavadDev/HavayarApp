using Common.Attributes;
using Common.Utilities;
using Entities.Base;
using Entities.Base.FormBuilder;
using Entities.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Configuration;
using System.Reflection;
 

namespace Services.QueryBuilderServices
{
	/// <summary>
	/// سرویس دسترسی به اطلاعات Schema دیتابیس
	/// </summary>
	public interface IDatabaseSchemaService
	{
		Task<List<TableInfo>> GetTablesAndViewsAsync();
 
		Task<List<TableRelation>> GetTableRelationsAsync(string tableName, string schema = "dbo");
		Task<ReportResult> ExecuteQueryAsync(string query, Dictionary<string, object> parameters = null);
		Task<bool> ValidateQueryAsync(string query);
		/// <summary>
		/// دریافت لیست Connection String های موجود از تنظیمات
		/// </summary>
		/// <returns>لیست نام و مقدار Connection String ها</returns>
		Task<List<ConnectionStringInfo>> GetConnectionStringsAsync();

		/// <summary>
		/// تست اتصال به دیتابیس
		/// </summary>
		/// <param name="connectionString">رشته اتصال به دیتابیس</param>
		/// <returns>نتیجه تست اتصال</returns>
		Task<ConnectionTestResult> TestConnectionAsync(string connectionString);

		/// <summary>
		/// دریافت لیست جداول دیتابیس
		/// </summary>
		/// <param name="connectionString">رشته اتصال به دیتابیس</param>
		/// <returns>لیست جداول با اطلاعات Schema و تعداد رکورد</returns>
		Task<List<DatabaseTableInfo>> GetTablesAsync(string connectionString);

		/// <summary>
		/// دریافت ستون‌های یک جدول با جزئیات کامل
		/// </summary>
		/// <param name="connectionString">رشته اتصال به دیتابیس</param>
		/// <param name="schema">نام Schema جدول</param>
		/// <param name="tableName">نام جدول</param>
		/// <returns>لیست ستون‌ها با تمام جزئیات و Mapping خودکار</returns>
		Task<List<TableColumnInfo>> GetTableColumnsAsync(string tableName, string schema = "dbo", string? entityName = null);
	}

	public class DatabaseSchemaService(IConfiguration _configuration, IEntityMetadataCache entityMetadataCache) : IDatabaseSchemaService
	{
		private readonly string _connectionString = _configuration.GetConnectionString("Db");
 

        /// <summary>
        /// دریافت لیست جداول و ویوها
        /// </summary>
        public async Task<List<TableInfo>> GetTablesAndViewsAsync()
		{
			var tables = new List<TableInfo>();

			const string query = @"
                SELECT 
                    t.TABLE_SCHEMA as [Schema],
                    t.TABLE_NAME as [Name],
                    t.TABLE_TYPE as [Type]
                FROM INFORMATION_SCHEMA.TABLES t
                WHERE t.TABLE_TYPE IN ('BASE TABLE', 'VIEW')
                ORDER BY t.TABLE_SCHEMA, t.TABLE_NAME";

			using (var connection = new SqlConnection(_connectionString))
			{
				await connection.OpenAsync();
				using (var command = new SqlCommand(query, connection))
				{
					using (var reader = await command.ExecuteReaderAsync())
					{
						while (await reader.ReadAsync())
						{
							var tableInfo = new TableInfo
							{
								Schema = reader.GetString(0),
								Name = reader.GetString(1),
								Type = reader.GetString(2) == "BASE TABLE" ? "Table" : "View",
								Columns = new List<ColumnInfo>()
							};
							tables.Add(tableInfo);
						}
					}
				}
			}

			var allEntityData = entityMetadataCache.GetAll();

			var tableLookup = tables.ToDictionary(
				    t => (Schema: t.Schema ?? "", Name: t.Name ?? "")
				);

			foreach (var entityData in allEntityData)
			{
				var key = (Schema: entityData.Schema ?? "", Name: entityData.TabelName ?? "");
				if (tableLookup.TryGetValue(key, out var table))
				{
					table.DisplayName = entityData.DisplayName;
					table.EntityFullName = entityData.EntityFullName;
					table.HasEntity = true;
				}
			}

			return tables;
		}

		 

		/// <summary>
		/// دریافت روابط Foreign Key یک جدول
		/// </summary>
		public async Task<List<TableRelation>> GetTableRelationsAsync(string tableName, string schema = "dbo")
		{
			var relations = new List<TableRelation>();

			const string query = @"
                SELECT 
                    fk.name as FK_Name,
                    OBJECT_SCHEMA_NAME(fk.parent_object_id) as SourceSchema,
                    OBJECT_NAME(fk.parent_object_id) as SourceTable,
                    COL_NAME(fkc.parent_object_id, fkc.parent_column_id) as SourceColumn,
                    OBJECT_SCHEMA_NAME(fk.referenced_object_id) as TargetSchema,
                    OBJECT_NAME(fk.referenced_object_id) as TargetTable,
                    COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) as TargetColumn
                FROM sys.foreign_keys fk
                INNER JOIN sys.foreign_key_columns fkc 
                    ON fk.object_id = fkc.constraint_object_id
                WHERE (OBJECT_SCHEMA_NAME(fk.parent_object_id) = @Schema AND OBJECT_NAME(fk.parent_object_id) = @TableName)
                   OR (OBJECT_SCHEMA_NAME(fk.referenced_object_id) = @Schema AND OBJECT_NAME(fk.referenced_object_id) = @TableName)";

			using (var connection = new SqlConnection(_connectionString))
			{
				await connection.OpenAsync();
				using (var command = new SqlCommand(query, connection))
				{
					command.Parameters.AddWithValue("@Schema", schema);
					command.Parameters.AddWithValue("@TableName", tableName);

					using (var reader = await command.ExecuteReaderAsync())
					{
						while (await reader.ReadAsync())
						{
							var relation = new TableRelation
							{
								Id = Guid.NewGuid().ToString(),
								SourceTable = $"{reader.GetString(1)}.{reader.GetString(2)}",
								SourceColumn = reader.GetString(3),
								TargetTable = $"{reader.GetString(4)}.{reader.GetString(5)}",
								TargetColumn = reader.GetString(6),
								JoinType = "INNER"
							};
							relations.Add(relation);
						}
					}
				}
			}

			return relations;
		}

		/// <summary>
		/// اجرای Query و دریافت نتیجه
		/// </summary>
		public async Task<ReportResult> ExecuteQueryAsync(string query, Dictionary<string, object> parameters = null)
		{
			var result = new ReportResult
			{
				ColumnNames = new List<string>(),
				ColumnTypes = new List<string>(),
				Rows = new List<Dictionary<string, object>>(),
				GeneratedQuery = query
			};

			using (var connection = new SqlConnection(_connectionString))
			{
				await connection.OpenAsync();
				using (var command = new SqlCommand(query, connection))
				{
					command.CommandTimeout = 300; // 5 دقیقه

					if (parameters != null)
					{
						foreach (var param in parameters)
						{
							command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
						}
					}

					using (var reader = await command.ExecuteReaderAsync())
					{
						for (int i = 0; i < reader.FieldCount; i++)
						{
							result.ColumnNames.Add(reader.GetName(i));
							result.ColumnTypes.Add(reader.GetDataTypeName(i) ?? "nvarchar");
						}

						// دریافت داده‌ها
						while (await reader.ReadAsync())
						{
							var row = new Dictionary<string, object>();
							for (int i = 0; i < reader.FieldCount; i++)
							{
								row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
							}
							result.Rows.Add(row);
						}

						result.TotalRows = result.Rows.Count;
					}
				}
			}

			return result;
		}

		/// <summary>
		/// اعتبارسنجی Query
		/// </summary>
		public async Task<bool> ValidateQueryAsync(string query)
		{
			try
			{
				using (var connection = new SqlConnection(_connectionString))
				{
					await connection.OpenAsync();
					using (var command = new SqlCommand($"SET PARSEONLY ON; {query}; SET PARSEONLY OFF;", connection))
					{
						await command.ExecuteNonQueryAsync();
					}
				}
				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// دریافت Display Name از Attribute های C#
		/// </summary>
		private string GetDisplayName(string tableName, string columnName)
		{
			// جستجو در Assembly های loaded برای پیدا کردن Model مربوطه
			try
			{
				var modelType = AppDomain.CurrentDomain.GetAssemblies()
				    .SelectMany(a => a.GetTypes())
				    .FirstOrDefault(t => t.Name.Equals(tableName, StringComparison.OrdinalIgnoreCase));

				if (modelType != null)
				{
					var property = modelType.GetProperty(columnName);
					if (property != null)
					{
						var displayAttribute = property.GetCustomAttribute<System.ComponentModel.DataAnnotations.DisplayAttribute>();
						if (displayAttribute != null)
						{
							return displayAttribute.Name ?? columnName;
						}
					}
				}
			}
			catch
			{
				// در صورت خطا، نام ستون را برمی‌گردانیم
			}

			return columnName;
		}

		/// <summary>
		/// دریافت لیست Connection String های موجود از appsettings.json
		/// </summary>
		public Task<List<ConnectionStringInfo>> GetConnectionStringsAsync()
		{
			try
			{
				var connectionStrings = new List<ConnectionStringInfo>();
				var configSection = _configuration.GetSection("ConnectionStrings");

				foreach (var item in configSection.GetChildren())
				{
					connectionStrings.Add(new ConnectionStringInfo
					{
						Name = item.Key,
						Value = item.Value ?? string.Empty,
						IsFromConfig = true
					});
				}

				return Task.FromResult(connectionStrings);
			}
			catch (Exception ex)
			{
			 
				return Task.FromResult(new List<ConnectionStringInfo>());
			}
		}

		/// <summary>
		/// تست اتصال به دیتابیس SQL Server
		/// </summary>
		public async Task<ConnectionTestResult> TestConnectionAsync(string connectionString)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(connectionString))
				{
					return new ConnectionTestResult
					{
						IsSuccess = false,
						Message = "Connection String نمی‌تواند خالی باشد"
					};
				}

				using var connection = new SqlConnection(connectionString);
				await connection.OpenAsync();

				var result = new ConnectionTestResult
				{
					IsSuccess = true,
					Message = "اتصال با موفقیت برقرار شد",
					DatabaseName = connection.Database,
					ServerVersion = connection.ServerVersion
				};

				return result;
			}
			catch (SqlException sqlEx)
			{
				 
				return new ConnectionTestResult
				{
					IsSuccess = false,
					Message = $"خطا در اتصال به دیتابیس: {sqlEx.Message}"
				};
			}
			catch (Exception ex)
			{
			 
				return new ConnectionTestResult
				{
					IsSuccess = false,
					Message = $"خطای غیرمنتظره: {ex.Message}"
				};
			}
		}

		/// <summary>
		/// دریافت لیست جداول دیتابیس با اطلاعات Schema و تعداد رکورد
		/// </summary>
		public async Task<List<DatabaseTableInfo>> GetTablesAsync(string connectionString)
		{
			var tables = new List<DatabaseTableInfo>();

			try
			{
				// Validation
				if (string.IsNullOrWhiteSpace(connectionString))
				{
					throw new ArgumentException("Connection String نمی‌تواند خالی باشد", nameof(connectionString));
				}

				using var connection = new SqlConnection(connectionString);
				await connection.OpenAsync();

				var query = @"
					SELECT 
						t.TABLE_SCHEMA as [Schema],
						t.TABLE_NAME as TableName,
					ISNULL(p.rows, 0) as 'RowCount'
					FROM INFORMATION_SCHEMA.TABLES t
					LEFT JOIN sys.tables st ON t.TABLE_NAME = st.name
					LEFT JOIN (
						SELECT 
							OBJECT_NAME(object_id) as TableName,
							SUM(rows) as rows
						FROM sys.partitions
						WHERE index_id IN (0,1)
						GROUP BY object_id
					) p ON st.name = p.TableName
					WHERE t.TABLE_TYPE = 'BASE TABLE'
						AND t.TABLE_SCHEMA NOT IN ('sys', 'INFORMATION_SCHEMA')
					ORDER BY t.TABLE_SCHEMA, t.TABLE_NAME";

				using var command = new SqlCommand(query, connection);
				using var reader = await command.ExecuteReaderAsync();

				while (await reader.ReadAsync())
				{
					tables.Add(new DatabaseTableInfo
					{
						Schema = reader["Schema"]?.ToString() ?? "dbo",
						TableName = reader["TableName"]?.ToString() ?? string.Empty,
						RowCount = reader["RowCount"] != DBNull.Value
							? Convert.ToInt64(reader["RowCount"])
							: 0
					});
				}

				 
			}
			catch (Exception ex)
			{
				 
				throw;
			}

			return tables;
		}

		/// <summary>
		/// دریافت ستون‌های یک جدول با تمام جزئیات و Mapping خودکار به SystemType
		/// </summary>
		public async Task<List<TableColumnInfo>> GetTableColumnsAsync(string tableName, string schema, string? entityName)
		{
			var columns = new List<TableColumnInfo>();

			try
			{
				 

				if (string.IsNullOrWhiteSpace(schema))
				{
					throw new ArgumentException("نام Schema نمی‌تواند خالی باشد", nameof(schema));
				}

				if (string.IsNullOrWhiteSpace(tableName))
				{
					throw new ArgumentException("نام جدول نمی‌تواند خالی باشد", nameof(tableName));
				}

				using var connection = new SqlConnection(_connectionString);
				await connection.OpenAsync();

				// Query برای دریافت اطلاعات ستون‌ها
				var query = @"
					SELECT 
						c.COLUMN_NAME,
						c.DATA_TYPE,
						c.CHARACTER_MAXIMUM_LENGTH,
						c.IS_NULLABLE,
						c.COLUMN_DEFAULT,
						c.NUMERIC_PRECISION,
						c.NUMERIC_SCALE,
						CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END as IsPrimaryKey,
						CASE WHEN fk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END as IsForeignKey,
						fk.REFERENCED_TABLE_NAME,
						fk.REFERENCED_COLUMN_NAME,
						c.ORDINAL_POSITION
					FROM INFORMATION_SCHEMA.COLUMNS c
					LEFT JOIN (
						SELECT ku.TABLE_SCHEMA, ku.TABLE_NAME, ku.COLUMN_NAME
						FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS tc
						INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS ku
							ON tc.CONSTRAINT_TYPE = 'PRIMARY KEY' 
							AND tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
							AND tc.TABLE_SCHEMA = ku.TABLE_SCHEMA
							AND tc.TABLE_NAME = ku.TABLE_NAME
					) pk ON c.TABLE_SCHEMA = pk.TABLE_SCHEMA 
						AND c.TABLE_NAME = pk.TABLE_NAME 
						AND c.COLUMN_NAME = pk.COLUMN_NAME
					LEFT JOIN (
						SELECT 
							fk.name as FK_NAME,
							tp.name as TABLE_NAME,
							cp.name as COLUMN_NAME,
							tr.name as REFERENCED_TABLE_NAME,
							cr.name as REFERENCED_COLUMN_NAME,
							SCHEMA_NAME(tp.schema_id) as TABLE_SCHEMA
						FROM sys.foreign_keys fk
						INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
						INNER JOIN sys.tables tp ON fkc.parent_object_id = tp.object_id
						INNER JOIN sys.columns cp ON fkc.parent_object_id = cp.object_id AND fkc.parent_column_id = cp.column_id
						INNER JOIN sys.tables tr ON fkc.referenced_object_id = tr.object_id
						INNER JOIN sys.columns cr ON fkc.referenced_object_id = cr.object_id AND fkc.referenced_column_id = cr.column_id
					) fk ON c.TABLE_SCHEMA = fk.TABLE_SCHEMA 
						AND c.TABLE_NAME = fk.TABLE_NAME 
						AND c.COLUMN_NAME = fk.COLUMN_NAME
					WHERE c.TABLE_SCHEMA = @Schema 
						AND c.TABLE_NAME = @TableName
					ORDER BY c.ORDINAL_POSITION";

				using var command = new SqlCommand(query, connection);
				command.Parameters.AddWithValue("@Schema", schema);
				command.Parameters.AddWithValue("@TableName", tableName);

				using var reader = await command.ExecuteReaderAsync();

				while (await reader.ReadAsync())
				{
					var columnName = reader["COLUMN_NAME"]?.ToString() ?? string.Empty;
					var dataType = reader["DATA_TYPE"]?.ToString()?.ToLower() ?? "nvarchar";
					var isNullable = reader["IS_NULLABLE"]?.ToString()?.Equals("YES", StringComparison.OrdinalIgnoreCase) ?? false;
					var isPrimaryKey = Convert.ToBoolean(reader["IsPrimaryKey"]);
					var isForeignKey = Convert.ToBoolean(reader["IsForeignKey"]);

					var columnInfo = new TableColumnInfo
					{
						ColumnName = columnName,
						DataType = dataType,
						MaxLength = reader["CHARACTER_MAXIMUM_LENGTH"] != DBNull.Value
							? Convert.ToInt32(reader["CHARACTER_MAXIMUM_LENGTH"])
							: null,
						IsNullable = isNullable,
						IsPrimaryKey = isPrimaryKey,
						IsForeignKey = isForeignKey,
						ForeignKeyTable = reader["REFERENCED_TABLE_NAME"]?.ToString(),
						ForeignKeyColumn = reader["REFERENCED_COLUMN_NAME"]?.ToString(),
						DefaultValue = reader["COLUMN_DEFAULT"]?.ToString(),
						NumericPrecision = reader["NUMERIC_PRECISION"] != DBNull.Value
							? Convert.ToInt32(reader["NUMERIC_PRECISION"])
							: null,
						NumericScale = reader["NUMERIC_SCALE"] != DBNull.Value
							? Convert.ToInt32(reader["NUMERIC_SCALE"])
							: null,
						SuggestedDisplayName = GenerateDisplayName(columnName),
						MappedSystemType = MapSqlTypeToSystemType(dataType, isForeignKey),
						MappedSystemTypeName = MapSqlTypeToSystemType(dataType, isForeignKey).ToString()
					};

					columns.Add(columnInfo);
				}




				if (entityName.HasValue())
				{
					var allEntityData = entityMetadataCache.Get(entityName);
					if (allEntityData?.Properties != null)
					{
						 
						var columnLookup = columns
						    .Where(c => c.ColumnName != null) 
						    .ToDictionary(
							   c => c.ColumnName,
							   c => c,
							   StringComparer.OrdinalIgnoreCase
						    );
						 
						foreach (var property in allEntityData.Properties.Where(p => p?.Name != null))
						{
							if (columnLookup.TryGetValue(property.Name, out var column))
							{
								column.DisplayName = property.DisplayName;
								column.MappedSystemType = property.SystemType;
								column.MappedSystemTypeName = property.SystemType.ToString();
								column.Options = property.Options;
							}
							else if(property.SystemType == SystemType.Entity)
							{
								if (columnLookup.TryGetValue(property.Name+"Id", out column))
								{
									column.DisplayName = "شناسه " + property.DisplayName ;
									column.MappedSystemType = property.SystemType;
									column.Options = property.Options;
									column.MappedSystemTypeName = property.SystemType.ToString();
								}
							}
						}
					}
				}


			}
			catch (Exception ex)
			{
			 
				throw;
			}

			return columns;
		}

		/// <summary>
		/// تبدیل نوع داده SQL Server به SystemType
		/// </summary>
		private SystemType MapSqlTypeToSystemType(string sqlType, bool isForeignKey)
		{
			// اگر Foreign Key است، Entity در نظر بگیر
			if (isForeignKey)
			{
				return SystemType.Entity;
			}

			return sqlType.ToLower() switch
			{
				// String types
				"varchar" or "nvarchar" or "char" or "nchar" or "text" or "ntext" => SystemType.String,

				// Boolean
				"bit" => SystemType.Boolean,

				// Integer types
				"int" or "smallint" or "tinyint" => SystemType.Int,
				"bigint" => SystemType.Long,

				// Decimal types
				"decimal" or "numeric" or "money" or "smallmoney" or "float" or "real" => SystemType.Decimal,

				// Date/Time types
				"datetime" or "datetime2" or "smalldatetime" => SystemType.DateTime,
				"date" => SystemType.Date,
				"time" => SystemType.String, // Time را به صورت String ذخیره می‌کنیم

				// GUID
				"uniqueidentifier" => SystemType.String,

				// Binary types (File)
				"varbinary" or "binary" or "image" => SystemType.File,

				// Default
				_ => SystemType.String
			};
		}

		/// <summary>
		/// تولید نام نمایشی فارسی پیشنهادی از نام ستون انگلیسی
		/// </summary>
		private string GenerateDisplayName(string columnName)
		{
			// نقشه‌برداری رایج نام‌های انگلیسی به فارسی
			var commonMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				{ "Id", "شناسه" },
				{ "Name", "نام" },
				{ "Title", "عنوان" },
				{ "Description", "توضیحات" },
				{ "Code", "کد" },
				{ "Date", "تاریخ" },
				{ "DateTime", "تاریخ و زمان" },
				{ "CreatedDate", "تاریخ ایجاد" },
				{ "ModifiedDate", "تاریخ ویرایش" },
				{ "CreatedBy", "ایجاد کننده" },
				{ "ModifiedBy", "ویرایش کننده" },
				{ "IsActive", "فعال" },
				{ "IsDeleted", "حذف شده" },
				{ "Status", "وضعیت" },
				{ "Type", "نوع" },
				{ "Email", "ایمیل" },
				{ "Phone", "تلفن" },
				{ "Mobile", "موبایل" },
				{ "Address", "آدرس" },
				{ "City", "شهر" },
				{ "Country", "کشور" },
				{ "PostalCode", "کد پستی" },
				{ "Price", "قیمت" },
				{ "Amount", "مبلغ" },
				{ "Quantity", "تعداد" },
				{ "Total", "جمع" },
				{ "Discount", "تخفیف" },
				{ "Tax", "مالیات" },
				{ "Note", "یادداشت" },
				{ "Order", "ترتیب" },
				{ "OrderIndex", "ترتیب" },
				{ "DisplayOrder", "ترتیب نمایش" },
				{ "ParentId", "شناسه والد" },
				{ "UserId", "شناسه کاربر" },
				{ "Username", "نام کاربری" },
				{ "Password", "رمز عبور" },
				{ "FirstName", "نام" },
				{ "LastName", "نام خانوادگی" },
				{ "FullName", "نام کامل" },
				{ "Image", "تصویر" },
				{ "ImageUrl", "آدرس تصویر" },
				{ "Url", "آدرس" },
				{ "Website", "وب‌سایت" },
				{ "StartDate", "تاریخ شروع" },
				{ "EndDate", "تاریخ پایان" },
				{ "DueDate", "تاریخ سررسید" }
			};

			// بررسی نقشه‌برداری مستقیم
			if (commonMappings.TryGetValue(columnName, out var persianName))
			{
				return persianName;
			}

			// اگر با Id ختم شود
			if (columnName.EndsWith("Id", StringComparison.OrdinalIgnoreCase) && columnName.Length > 2)
			{
				var baseName = columnName.Substring(0, columnName.Length - 2);
				return $"شناسه {baseName}";
			}

			// اگر با Date شروع شود
			if (columnName.StartsWith("Date", StringComparison.OrdinalIgnoreCase) && columnName.Length > 4)
			{
				var baseName = columnName.Substring(4);
				return $"تاریخ {baseName}";
			}

			// اگر با Is شروع شود (Boolean)
			if (columnName.StartsWith("Is", StringComparison.OrdinalIgnoreCase) && columnName.Length > 2)
			{
				var baseName = columnName.Substring(2);
				return baseName;
			}

			// در غیر این صورت، همان نام انگلیسی را برگردان
			return columnName;
		}
	}
}
