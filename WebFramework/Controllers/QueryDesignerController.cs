using Azure.Core;
using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Entities.Auth;
using Entities.Base;
using Entities.Base.FormBuilder;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
 
using Services.QueryBuilderServices;
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
		public async Task<IActionResult> Edit(long? id)
		{

			if(id != null)
			{
				var report = await _queryService.GetReportAsync((long)id);
				if (report == null)
					return NotFound();

				var queryDesign = JsonSerializer.Deserialize<QueryDesign>(report.QueryJson);
				var reportMode = !string.IsNullOrEmpty(queryDesign?.CustomQuery) ? "write" : "design";

				ViewBag.ReportId = id;
				ViewBag.ReportMode = reportMode;
				ViewBag.ReportData = report;
			}
			
			return View(@"\Views\Panel\System\QueryDesigner\Edit.cshtml");
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

			// محدود کردن تعداد رکوردها برای پیش‌نمایش
			if (!query.ToUpper().Contains("TOP "))
			{
				query = query.Replace("SELECT", "SELECT TOP 100");
			}

			// ساخت پارامترها و اجرا
			var paramValues = request.Parameters ?? new Dictionary<string, string>();
			var userId = User?.Identity?.Name;

			var sqlParams = _parameterResolver.BuildParameters(query, paramValues, userId, userId);
			var paramsObj = new Dictionary<string, object>();

			foreach (var kv in sqlParams)
				paramsObj[kv.Key] = kv.Value;

			 
				var result = await _schemaService.ExecuteQueryAsync(query, paramsObj);
				return Ok(result);
			 
		}
		/// <summary>
		/// ذخیره گزارش
		/// </summary>
		[HttpPost("[action]")]
		public async Task<IActionResult> SaveReport([FromBody] ReportDesign design)
		{
			 
				if (design == null)
					throw new Exception("داده‌های گزارش ارسال نشده است. لطفاً نام و عنوان گزارش را وارد کنید و مجدداً تلاش کنید");

				var userId = User.Identity.GetUserId();

				var report = await _queryService.SaveReportAsync(design, userId);
				return Ok(report);
			 
			 
		}

		/// <summary>
		/// بروزرسانی گزارش
		/// </summary>
		[HttpPost("[action]")]
		public async Task<IActionResult> UpdateReport(int id, [FromBody] ReportDesign design)
		{
			 
				if (design == null)
					throw new Exception("داده‌های گزارش ارسال نشده است. لطفاً مجدداً تلاش کنید." );

				var userId = User.Identity.GetUserId();
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
		/// دریافت گزارش
		/// </summary>
		[HttpGet("[action]")]
		public async Task<IActionResult> GetReport(int id)
		{
			 
				var report = await _queryService.GetReportAsync(id);
				if (report == null)
					throw new Exception("گزارش یافت نشد");

				var queryDesign = JsonSerializer.Deserialize<QueryDesign>(report.QueryJson);
				var columns = JsonSerializer.Deserialize<List<ReportColumn>>(report.ColumnsJson);

				var design = new ReportDesign
				{
					Name = report.Name,
					Title = report.Title,
					QueryDesign = queryDesign,
					Columns = columns
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
				var columns = System.Text.Json.JsonSerializer.Deserialize<List<ReportColumn>>(report.ColumnsJson);

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
		public List<ReportColumn> Columns { get; set; }
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
