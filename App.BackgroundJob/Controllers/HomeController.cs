using App.BackgroundJob.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Configuration;
using System.Diagnostics;
using System.Text;

namespace App.BackgroundJob.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		private readonly string _connectionString ;

		public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
		{

			_connectionString = configuration.GetConnectionString("Rahkaran")
		    ?? throw new InvalidOperationException("Connection string 'Rahkaran' not found.");

			_logger = logger;
		}
		public IActionResult Index()
		{
			return View();
		}

		[HttpGet]
		public async Task<IActionResult> GetDatabaseObjects()
		{
			try
			{
				var objects = new List<DatabaseObject>();

				using (var conn = new SqlConnection(_connectionString))
				{
					await conn.OpenAsync();

					// دریافت جداول با Schema
					string tableQuery = @"
                        SELECT 
                            TABLE_SCHEMA,
                            TABLE_NAME, 
                            'Table' as ObjectType 
                        FROM INFORMATION_SCHEMA.TABLES 
                        WHERE TABLE_TYPE = 'BASE TABLE'
                        ORDER BY TABLE_SCHEMA, TABLE_NAME";

					using (var cmd = new SqlCommand(tableQuery, conn))
					using (var reader = await cmd.ExecuteReaderAsync())
					{
						while (await reader.ReadAsync())
						{
							objects.Add(new DatabaseObject
							{
								Schema = reader["TABLE_SCHEMA"].ToString(),
								Name = reader["TABLE_NAME"].ToString(),
								Type = reader["ObjectType"].ToString()
							});
						}
					}

					// دریافت ویوها با Schema
					string viewQuery = @"
                        SELECT 
                            TABLE_SCHEMA,
                            TABLE_NAME, 
                            'View' as ObjectType 
                        FROM INFORMATION_SCHEMA.VIEWS 
                        ORDER BY TABLE_SCHEMA, TABLE_NAME";

					using (var cmd = new SqlCommand(viewQuery, conn))
					using (var reader = await cmd.ExecuteReaderAsync())
					{
						while (await reader.ReadAsync())
						{
							objects.Add(new DatabaseObject
							{
								Schema = reader["TABLE_SCHEMA"].ToString(),
								Name = reader["TABLE_NAME"].ToString(),
								Type = reader["ObjectType"].ToString()
							});
						}
					}
				}

				return Json(objects);
			}
			catch (Exception ex)
			{
				return BadRequest(new { error = "خطا در دریافت اطلاعات: " + ex.Message });
			}
		}

		[HttpPost]
		public async Task<IActionResult> GenerateClass([FromBody] GenerateRequest request)
		{
			try
			{
				using (var conn = new SqlConnection(_connectionString))
				{
					await conn.OpenAsync();

					string query = @"
                        SELECT 
                            COLUMN_NAME,
                            DATA_TYPE,
                            IS_NULLABLE,
                            CHARACTER_MAXIMUM_LENGTH,
                            NUMERIC_PRECISION,
                            NUMERIC_SCALE
                        FROM INFORMATION_SCHEMA.COLUMNS
                        WHERE TABLE_NAME = @TableName
                        ORDER BY ORDINAL_POSITION";

					var columns = new List<ColumnInfo>();

					using (var cmd = new SqlCommand(query, conn))
					{
						cmd.Parameters.AddWithValue("@TableName", request.TableName);

						using (var reader = await cmd.ExecuteReaderAsync())
						{
							while (await reader.ReadAsync())
							{
								columns.Add(new ColumnInfo
								{
									Name = reader["COLUMN_NAME"].ToString(),
									DataType = reader["DATA_TYPE"].ToString(),
									IsNullable = reader["IS_NULLABLE"].ToString() == "YES",
									MaxLength = reader["CHARACTER_MAXIMUM_LENGTH"] != DBNull.Value
									   ? Convert.ToInt32(reader["CHARACTER_MAXIMUM_LENGTH"])
									   : null
								});
							}
						}
					}

					string code = BuildClassCode(request.TableName, columns, request.ObjectType, request.Schema);
					return Json(new { code });
				}
			}
			catch (Exception ex)
			{
				return BadRequest(new { error = "خطا در تولید کلاس: " + ex.Message });
			}
		}

		private string BuildClassCode(string tableName, List<ColumnInfo> columns, string objectType, string schema)
		{
			var sb = new StringBuilder();

			sb.AppendLine("using System;");
			sb.AppendLine("using System.ComponentModel.DataAnnotations;");
			sb.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
			sb.AppendLine();
			sb.AppendLine("namespace YourNamespace.Models");
			sb.AppendLine("{");

			// اضافه کردن Schema در Table Attribute
			if (!string.IsNullOrEmpty(schema))
			{
				sb.AppendLine($"    [Table(name: \"{tableName}\", Schema = \"{schema}\")]");
			}
			else
			{
				sb.AppendLine($"    [Table(\"{tableName}\")]");
			}

			sb.AppendLine($"    public class {tableName}");
			sb.AppendLine("    {");

			foreach (var column in columns)
			{
				string csharpType = SqlTypeToCSharpType(column.DataType, column.IsNullable);

				// اضافه کردن Attributes
				if (column.Name.ToLower() == "id" || column.Name.ToLower() == tableName.ToLower() + "id")
				{
					sb.AppendLine("        [Key]");
				}

				if (!column.IsNullable && csharpType == "string")
				{
					sb.AppendLine("        [Required]");
				}

				if (column.MaxLength.HasValue && csharpType == "string")
				{
					sb.AppendLine($"        [MaxLength({column.MaxLength.Value})]");
				}

				sb.AppendLine($"        public {csharpType} {column.Name} {{ get; set; }}");
				sb.AppendLine();
			}

			sb.AppendLine("    }");
			sb.AppendLine("}");

			return sb.ToString();
		}

		private string SqlTypeToCSharpType(string sqlType, bool isNullable)
		{
			string csharpType = sqlType.ToLower() switch
			{
				"bigint" => "long",
				"int" => "int",
				"smallint" => "short",
				"tinyint" => "byte",
				"bit" => "bool",
				"decimal" or "numeric" or "money" or "smallmoney" => "decimal",
				"float" => "double",
				"real" => "float",
				"datetime" or "datetime2" or "smalldatetime" or "date" => "DateTime",
				"time" => "TimeSpan",
				"datetimeoffset" => "DateTimeOffset",
				"uniqueidentifier" => "Guid",
				"char" or "varchar" or "text" or "nchar" or "nvarchar" or "ntext" => "string",
				"binary" or "varbinary" or "image" => "byte[]",
				_ => "object"
			};

			if (isNullable && csharpType != "string" && csharpType != "byte[]" && csharpType != "object")
			{
				csharpType += "?";
			}

			return csharpType;
		}
	}

	public class DatabaseObject
	{
		public string Schema { get; set; }
		public string Name { get; set; }
		public string Type { get; set; }
	}

	public class ColumnInfo
	{
		public string Name { get; set; }
		public string DataType { get; set; }
		public bool IsNullable { get; set; }
		public int? MaxLength { get; set; }
	}

	public class GenerateRequest
	{
		public string TableName { get; set; }
		public string ObjectType { get; set; }
		public string Schema { get; set; }
	}


}
