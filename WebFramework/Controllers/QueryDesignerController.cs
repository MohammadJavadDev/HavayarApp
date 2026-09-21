using Azure.Core;
using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Services.QueryBuilderServices;
using Entities.Auth;
using Entities.Base;
using Entities.Base.FormBuilder;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;

using Data.Services.QueryBuilderServices;
using System.Text.Json;
using WebFramework.Filtters;
using WebFramework.Page;
 


namespace WebFramework.Controllers
{
	/// <summary>
	/// کنترلر اصلی طراح Query
	/// </summary>
	[Route("Panel/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("پرسو جو ساز", typeof(SavedQuery))]
	public class QueryDesignerController : BaseController
	{
		private readonly IDatabaseSchemaService _schemaService;
		private readonly IQueryBuilderService _queryBuilder;
		private readonly IQueryService _queryService;
		private readonly IParameterResolverService _parameterResolver;
		private readonly SqlQueryValidator _queryValidator;

		public QueryDesignerController(
		 IDatabaseSchemaService schemaService,
		 IQueryBuilderService queryBuilder,
		 IQueryService queryService,
		 IParameterResolverService parameterResolver,
		 SqlQueryValidator queryValidator)
		{
			_schemaService = schemaService;
			_queryBuilder = queryBuilder;
			_queryService = queryService;
			_parameterResolver = parameterResolver;
			_queryValidator = queryValidator;
		}

		/// <summary>
		/// صفحه لیست گزارش‌های ذخیره شده
		/// </summary>
		/// 
		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public async Task<IActionResult> List()
		{
			var reports = await _queryService.GetAllReportsAsync();
	 
			return View(@"\Views\Panel\System\QueryDesigner\List.cshtml", reports);
		}
		 

		/// <summary>
		/// ویرایش گزارش
		/// </summary>
		/// 
		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public async Task<IActionResult> Edit(long? id,string? entityName)
		{
			if(entityName.HasValue())
			{
				ViewBag.EntityFullName = entityName;
				ViewBag.ReportType = 1;
			}

			if(id != null)
			{
				var report = await _queryService.GetReportAsync((long)id);
				if (report == null)
					return NotFound();
				 

				ViewBag.ReportId = id;
				ViewBag.ReportType = (int)report.Type;
				ViewBag.EntityFullName = report.EntityFullName;
				ViewBag.ReportData = report;
			}
			
			return View(@"\Views\Panel\System\QueryDesigner\Edit.cshtml");
		}

		[HttpPost("[action]/{id}")]
		[ActionDisplayName("حذف اطلاعات", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Remove( long id)
		{
			 

			if (id != null)
			{
				var report = await _queryService.GetReportAsync((long)id);
				if (report == null)
					return NotFound();


				var res = await _queryService.DeleteReportAsync((long)id);

				if(res == false)
				{
					  throw new Exception("خطا در حذف نمایه داده");
				}


			}
			else
			{
				return NotFound("هیچ نمایه داده ای با این شناسه یافت نشد."); 
			}


				return Ok();
		}


		[HttpPost("[action]/{id}")]
		[ActionDisplayName("کپی اطلاعات", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> Copy(long id)
		{


			if (id != null)
			{
				var report = await _queryService.GetReportAsync((long)id);
				if (report == null)
					return NotFound();

				report.Name = report.Name + "new" + (report.Id + 1);
				report.Title = report.Title + (report.Id + 1);

				report.Id = null;
				

				var res = await _queryService.SaveReportAsync(report,CurrentUserId.Value);
				  
			}
			else
			{
				return NotFound("هیچ نمایه داده ای با این شناسه یافت نشد.");
			}


			return Ok();
		}

		[HttpPost("[action]/{id}")]
		[ActionDisplayName("ذخیره عرض ستون‌ها", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> SetColumnWidths(long id, [FromBody] List<DataProfileColumnWidthItem> columns)
		{
			if (id <= 0)
				return NotFound("هیچ نمایه داده ای با این شناسه یافت نشد.");

			if (!CurrentUserId.HasValue)
				throw new Exception("کاربر جاری مشخص نیست.");

			var report = await _queryService.GetReportAsync(id);
			if (report == null)
				return NotFound();

			var updated = await _queryService.UpdateDataProfileColumnWidthsAsync(id, columns ?? new List<DataProfileColumnWidthItem>(), CurrentUserId.Value);
			return Ok(new { updated });
		}

		#region API Methods

		/// <summary>
		/// دریافت لیست جداول و ویوها
		/// </summary>
		[HttpGet("[action]")]
		public async Task<IActionResult> GetTablesAndViews()
		{
			var tables = await _schemaService.GetTablesAndViewsAsync();
			return Ok(tables);
		}

		/// <summary>
		/// دریافت لیست Stored Procedure ها
		/// </summary>
		[HttpGet("[action]")]
		public async Task<IActionResult> GetStoredProcedures()
		{
			var procedures = await _schemaService.GetStoredProceduresAsync();
			return Ok(procedures);
		}

		/// <summary>
		/// دریافت پارامترهای Stored Procedure
		/// </summary>
		[HttpGet("[action]")]
		public async Task<IActionResult> GetStoredProcedureParameters(string schema, string procedureName)
		{
			var parameters = await _schemaService.GetStoredProcedureParametersAsync(schema, procedureName);
			return Ok(parameters);
		}

		/// <summary>
		/// دریافت ستون‌های جدول
		/// </summary>
		[HttpGet("[action]")]
		public async Task<IActionResult> GetTableColumns(string tableName, string schema = "dbo", string? entityFullName = null)
		{
			 
				var columns = await _schemaService.GetTableColumnsAsync(tableName, schema, entityFullName);
			return Ok(columns);
		}

		/// <summary>
		/// دریافت لیست پارامترهای از پیش تعریف شده
		/// </summary>
		[HttpGet("[action]")]
		public IActionResult GetBuiltInParameters()
		{
			var list = ParameterResolverService.GetBuiltInParameters()
				.Select(p => new { name = p.Name, description = p.Description });
			return Ok(list);
		}

		/// <summary>
		/// دریافت روابط جدول
		/// </summary>
		[HttpGet("[action]")]
		public async Task<IActionResult> GetTableRelations(string tableName, string schema = "dbo")
		{
			 
				var relations = await _schemaService.GetTableRelationsAsync(tableName, schema);
				return Ok(relations);
			 
		}

		/// <summary>
		/// ساخت Query از طراحی
		/// </summary>
		[HttpPost("[action]")]
		public IActionResult BuildQuery([FromBody] BuildQueryRequest request)
		{
			 
				var design = request;
				if (design == null)
					return Json(new { success = false, message = "طراحی معتبر نیست" });

				string query;
				if (request?.Columns != null && request.Columns.Any())
				{
					var hasShaping = request.Columns.Any(c => c.GroupBy || !string.IsNullOrEmpty(c.Aggregate));
					if (hasShaping)
						query = _queryBuilder.BuildQueryWithShaping(design, request.Columns);
					else
						query = _queryBuilder.BuildQueryWithSorting(design, request.Columns);
				}
				else
				{
					query = _queryBuilder.BuildQuery(design);
				}
				return Ok(query);
			 
		}

		/// <summary>
		/// اعتبارسنجی Query
		/// </summary>
		[HttpPost("[action]")]
		public async Task<IActionResult> ValidateQuery([FromBody] QueryValidationRequest request)
		{


			// ✅ اعتبارسنجی Query
			var validationResult = _queryValidator.ValidateQuery(request.Query);

			if (!validationResult.IsValid)
			{
				return BadRequest(

					  "Query نامعتبر است" + "||" + string.Join(",,", validationResult.Errors) + "||" + string.Join(",,", validationResult.Warnings)
				 );
			}

			var isValid = await _schemaService.ValidateQueryAsync(request.Query);
			 return Ok(isValid);
			 
		}

		/// <summary>
		/// پیش‌نمایش نتیجه Query
		/// </summary>
		[HttpPost("[action]")]
		public async Task<IActionResult> PreviewQuery([FromBody] QueryPreviewRequest request)
		{
			var query = request.Query;

			if (string.IsNullOrEmpty(query))
				return BadRequest(new { error = "Query خالی است" });

			// ✅ اعتبارسنجی Query
			var validationResult = _queryValidator.ValidateQuery(query);

			if (!validationResult.IsValid)
			{
				return BadRequest( 
				 
					  "Query نامعتبر است" + "||" + string.Join(",,", validationResult.Errors) + "||" + string.Join(",,", validationResult.Warnings)   
				 );
			}

			// محدود کردن تعداد رکوردها برای پیش‌نمایش (فقط برای SELECT، نه EXEC) - روی هر دستور SELECT به‌صورت مجزا
			var statements = SqlStatementHelper.SplitTopLevelStatements(query);
			var previewQuery = statements.Count > 0
				? SqlStatementHelper.JoinStatements(statements.Select(s => SqlStatementHelper.InjectTopN(s, 100)))
				: query;

			// ساخت پارامترها و اجرا
			var paramValues = request.Parameters ?? new Dictionary<string, string>();


			var sqlParams = _parameterResolver.BuildParameters(previewQuery, paramValues, CurrentUserId.Value.ToString(), CurrentUserName);
			var paramsObj = new Dictionary<string, object>();

			foreach (var kv in sqlParams)
				paramsObj[kv.Key] = kv.Value;

			var resultSets = await _schemaService.ExecuteMultiResultQueryAsync(previewQuery, paramsObj);
			var first = resultSets.First();

	 
			var response = new QueryResult
			{
				ColumnNames = first.ColumnNames,
				ColumnTypes = first.ColumnTypes,
				Rows = first.Rows,
				TotalRows = first.TotalRows,
				TotalFilterdRows = first.TotalFilterdRows,
				GeneratedQuery = first.GeneratedQuery,
				ResultSets = resultSets
			};
			return Ok(response);
		}
		/// <summary>
		/// ذخیره گزارش
		/// </summary>
		[HttpPost("[action]")]
		public async Task<IActionResult> SaveReport([FromBody] QueryDesignRequest design)
		{
			 
				if (design == null)
					throw new Exception("داده‌های گزارش ارسال نشده است. لطفاً نام و عنوان گزارش را وارد کنید و مجدداً تلاش کنید");

				var userId = CurrentUserId.Value;

				var report = await _queryService.SaveReportAsync(design, userId);
				return Ok(report);
			 
			 
		}

		/// <summary>
		/// بروزرسانی گزارش
		/// </summary>
		[HttpPost("[action]")]
		public async Task<IActionResult> UpdateReport(int id, [FromBody] QueryDesignRequest design)
		{
			 
				if (design == null)
					throw new Exception("داده‌های گزارش ارسال نشده است. لطفاً مجدداً تلاش کنید." );

				var userId = CurrentUserId.Value;
			var report = await _queryService.UpdateReportAsync(id, design, userId);
				return Ok(report);
			 
		}

		/// <summary>
		/// حذف گزارش
		/// </summary>
		[HttpPost("[action]")]
		public async Task<IActionResult> DeleteReport(int id)
		{
			 
				var result = await _queryService.DeleteReportAsync(id);
				return Ok(result);
			 
		}

		/// <summary>
		/// تبدیل گزارش از حالت طراحی بصری به حالت نوشتن Query
		/// </summary>
		[HttpPost("[action]")]
		public async Task<IActionResult> ConvertToQueryMode(int id)
		{
			if (!CurrentUserId.HasValue)
				throw new Exception("کاربر احراز هویت نشده است");

			var report = await _queryService.ConvertDesignToQueryModeAsync(id, CurrentUserId.Value);
			return Ok(new
			{
				report.Id,
				report.Name,
				report.Title,
				Mode = report.Mode
			});
		}

		/// <summary>
		/// دریافت گزارش
		/// </summary>
		[HttpGet("[action]")]
		public async Task<IActionResult> GetReport(int id)
		{
			 
				var report = await _queryService.GetReportAsync(id);
				if (report == null)
					throw new Exception("گزارش یافت نشد");

				var queryDesign = JsonSerializer.Deserialize<QueryDesign>(report.QueryJson);
				var columns = JsonSerializer.Deserialize<List<QueryColumn>>(report.ColumnsJson);
				
				foreach(var column in columns)
				{
					column.SystemTypeName = column.SystemType.ToString();
				}

				var design = new QueryDesignRequest
				{
					Name = report.Name,
					Title = report.Title,
					EntityFullName = report.EntityFullName,
					Mode = report.Mode,
					Type = report.Type,
					QueryDesign = queryDesign,
					Columns = columns,
					ActionOptions = report.ActionOptions,
					CustomActionButtons = report.CustomActionButtonsJson,
					EventScripts = report.EventScriptsJson
				};

				return Ok(design);
			 
		}

		/// <summary>
		/// اجرای گزارش - پارامترهای داینامیک از query string: ?id=1&ParamName=value
		/// </summary>
		[HttpGet("[action]")]
		public async Task<IActionResult> ExecuteReport(int id)
		{
			 
				var paramDict = Request.Query
					.Where(q => !string.Equals(q.Key, "id", StringComparison.OrdinalIgnoreCase))
					.ToDictionary(q => q.Key, q => q.Value.ToString());
				var userId = User?.Identity?.Name;
				var result = await _queryService.ExecuteReportAsync(id, paramDict, userId, userId);
				return Ok(result);
			 
		}

		/// <summary>
		/// بررسی یکتا بودن نام
		/// </summary>
		[HttpGet("[action]")]
		public async Task<IActionResult> CheckNameUnique(string name, int? excludeId = null)
		{
			 
				var isUnique = await _queryService.IsNameUniqueAsync(name, excludeId);
				return Ok(isUnique);
			 
		}





		/// <summary>
		/// دانلود SQL Script گزارش
		/// </summary>
		[HttpGet("[action]")]
		public async Task<IActionResult> DownloadSql(int id)
		{
			 
				var report = await _queryService.GetReportAsync(id);
				if (report == null)
					return NotFound();

				var queryDesign = System.Text.Json.JsonSerializer.Deserialize<QueryDesign>(report.QueryJson);
				var columns = System.Text.Json.JsonSerializer.Deserialize<List<QueryColumn>>(report.ColumnsJson);

				string query;
				if (!string.IsNullOrEmpty(queryDesign.CustomQuery))
				{
					query = queryDesign.CustomQuery;
				}
				else if (columns != null && columns.Any())
				{
					var hasShaping = columns.Any(c => c.GroupBy || !string.IsNullOrEmpty(c.Aggregate));
					query = hasShaping
						? _queryBuilder.BuildQueryWithShaping(queryDesign, columns)
						: _queryBuilder.BuildQueryWithSorting(queryDesign, columns);
				}
				else
				{
					query = _queryBuilder.BuildQuery(queryDesign, columns);
				}

				// افزودن کامنت‌های توضیحی
				var sqlScript = $@"
					/*
					    گزارش: {report.Title}
					    نام: {report.Name}
					    تاریخ ایجاد: {report.CreatedOnMiladiDateTime:yyyy/MM/dd HH:mm}
					    ایجاد کننده: {report.CreatedBy}
					*/

					{query}
					";

				var fileName = $"{report.Name}.sql";
				var bytes = System.Text.Encoding.UTF8.GetBytes(sqlScript);

				return File(bytes, "text/plain", fileName);
			 
		}
	}

	#region Export Request Models

	public class ExportPreviewRequest
	{
		public string Query { get; set; }
		public string Title { get; set; }
		public Dictionary<string, string> Parameters { get; set; }
	}

	#endregion

	#endregion
	#region Request Models

	public class BuildQueryRequest : QueryDesign
	{
		public List<QueryColumn> Columns { get; set; }
	}

	public class QueryValidationRequest
	{
		public string Query { get; set; }
	}

	public class QueryPreviewRequest
	{
		public string Query { get; set; }
		public Dictionary<string, string> Parameters { get; set; }
	}

	#endregion

}
