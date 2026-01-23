using Common.Attributes;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using WebFramework.ViewModels.FormBuilder;

namespace WebFramework.Services
{
	/// <summary>
	/// پیاده‌سازی سرویس خواندن اطلاعات Schema دیتابیس
	/// </summary>
	public class DatabaseSchemaService : IDatabaseSchemaService
	{
		private readonly IConfiguration _configuration;
		private readonly ILogger<DatabaseSchemaService> _logger;

		public DatabaseSchemaService(
			IConfiguration configuration,
			ILogger<DatabaseSchemaService> logger)
		{
			_configuration = configuration;
			_logger = logger;
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
				_logger.LogError(ex, "خطا در دریافت Connection String ها");
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
				_logger.LogWarning(sqlEx, "خطای SQL در تست اتصال");
				return new ConnectionTestResult
				{
					IsSuccess = false,
					Message = $"خطا در اتصال به دیتابیس: {sqlEx.Message}"
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "خطا در تست اتصال به دیتابیس");
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

				_logger.LogInformation($"تعداد {tables.Count} جدول از دیتابیس خوانده شد");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "خطا در دریافت لیست جداول");
				throw;
			}

			return tables;
		}

	/// <summary>
	/// دریافت ستون‌های یک جدول با تمام جزئیات و Mapping خودکار به SystemType
	/// </summary>
	public async Task<List<TableColumnInfo>> GetTableColumnsAsync(string connectionString, string schema, string tableName)
	{
		var columns = new List<TableColumnInfo>();

		try
		{
			// Validation
			if (string.IsNullOrWhiteSpace(connectionString))
			{
				throw new ArgumentException("Connection String نمی‌تواند خالی باشد", nameof(connectionString));
			}

			if (string.IsNullOrWhiteSpace(schema))
			{
				throw new ArgumentException("نام Schema نمی‌تواند خالی باشد", nameof(schema));
			}

			if (string.IsNullOrWhiteSpace(tableName))
			{
				throw new ArgumentException("نام جدول نمی‌تواند خالی باشد", nameof(tableName));
			}

			using var connection = new SqlConnection(connectionString);
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
						MappedSystemType = MapSqlTypeToSystemType(dataType, isForeignKey),
						SuggestedDisplayName = GenerateDisplayName(columnName)
					};

					columns.Add(columnInfo);
				}

				_logger.LogInformation($"تعداد {columns.Count} ستون از جدول {schema}.{tableName} خوانده شد");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"خطا در دریافت ستون‌های جدول {schema}.{tableName}");
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

