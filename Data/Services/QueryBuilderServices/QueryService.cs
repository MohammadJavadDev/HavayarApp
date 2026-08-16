using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data;
using Data.SystemAuth;
using Entities.Base;
using Entities.Base.DataTable;
using Microsoft.EntityFrameworkCore;

using System.Linq;
using System.Text;
using System.Text.Json;
using static Data.Repositories.DataTableQueryBuilder;

namespace Data.Services.QueryBuilderServices
{
	/// <summary>
	/// سرویس مدیریت گزارش‌ها
	/// </summary>
	public interface IQueryService
	{
		Task<SavedQuery> SaveReportAsync(QueryDesignRequest design, long userId);
		Task<SavedQuery> SaveReportAsync(SavedQuery design, long userId);
		Task<SavedQuery> UpdateReportAsync(long id, QueryDesignRequest design, long userId);
		Task<bool> DeleteReportAsync(long id);
		Task<SavedQuery> GetReportAsync(long id);
		Task<SavedQuery> GetReportByNameAsync(string name);
		Task<List<SavedQuery>> GetAllReportsAsync();
		Task<List<SavedQuery>> GetAllSavedQueriesForReportsAsync();
		Task<List<SavedQuery>> GetAllSavedQueriesForReportsFillterByRoleAsync();
		Task<QueryResult> ExecuteReportAsync(long id, IReadOnlyDictionary<string, string> parameterValues = null, string userId = null, string username = null);
		Task<QueryResult> ExecuteReportAsync(long id, IReadOnlyDictionary<string, string> parameterValues, int offset, int limit, string userId = null, string username = null);

		/// <summary>
		/// اجرای یک گزارش ذخیره‌شده که ممکن است شامل چند دستور SELECT باشد (حالت نوشتن Query)؛
		/// یک QueryResult به‌ازای هر SELECT به‌همراه اطلاعات نام‌گذاری آن (QuerySelectInfo) برگردانده می‌شود.
		/// برای گزارش‌های تک-Select، خروجی همیشه دقیقاً یک ResultSet دارد.
		/// </summary>
		Task<QueryMultiResult> ExecuteReportMultiAsync(long id, IReadOnlyDictionary<string, string> parameterValues = null, string userId = null, string username = null);

		Task<bool> IsNameUniqueAsync(string name, long? excludeId = null);
		List<SavedQuery> GetDataTableProfileListByEntityName(string? entityName);
		List<SavedQuery> GetDataTableProfileById(List<long?> ids , string? entityName);
		Dictionary<string, string> ExtractParameterValuesFromRequest(DataTableRequest request);

		Task<QueryResult> ExecuteReportAsync(DataTableRequest request, IReadOnlyDictionary<string, string> parameterValues, int offset, int limit, string userId = null, string username = null);

		/// <summary>
		/// تبدیل گزارش ذخیره‌شده از حالت طراحی بصری (Diagram) به حالت نوشتن Query
		/// </summary>
		Task<SavedQuery> ConvertDesignToQueryModeAsync(long id, long userId);

	}

	public class QueryService : IQueryService
	{
		private readonly ApplicationDbContext _context;
		private readonly IQueryBuilderService _queryBuilder;
		private readonly IDatabaseSchemaService _schemaService;
		private readonly IParameterResolverService _parameterResolver;
		private readonly ISdk _sdk;

		public QueryService(
		    ApplicationDbContext context,
		    IQueryBuilderService queryBuilder,
		    IDatabaseSchemaService schemaService,
		    IParameterResolverService parameterResolver,
		    ISdk sdk)
		{
			_context = context;
			_queryBuilder = queryBuilder;
			_schemaService = schemaService;
			_parameterResolver = parameterResolver;
			_sdk = sdk;
		}

		/// <summary>
		/// ذخیره گزارش جدید
		/// </summary>
		public async Task<SavedQuery> SaveReportAsync(QueryDesignRequest design, long userId)
		{
			// بررسی یکتا بودن نام
			if (!await IsNameUniqueAsync(design.Name))
				throw new InvalidOperationException($"گزارشی با نام '{design.Name}' قبلاً ثبت شده است");

			// ذخیره Tables بدون Columns - ستون‌ها هنگام ویرایش از سرور دریافت می‌شوند
			var tablesForStorage = (design.QueryDesign.Tables ?? new List<TableInfo>())
				.Select(t => new TableInfo { Schema = t.Schema, Name = t.Name, Type = t.Type, PositionX = t.PositionX, PositionY = t.PositionY, Columns = null, BoxId = t.BoxId, InstanceId = t.InstanceId }).ToList();
			var queryDesignForStorage = new QueryDesign
			{
				Tables = tablesForStorage,
				Relations = design.QueryDesign.Relations,
				Filters = design.QueryDesign.Filters,
				CustomConditions = design.QueryDesign.CustomConditions,
				CustomQuery = design.QueryDesign.CustomQuery,
				Parameters = design.QueryDesign.Parameters,
				Selects = design.QueryDesign.Selects
			};

			var report = new SavedQuery
			{
				Name = design.Name,
				Title = design.Title,
				Type = design.Type,
				Mode = design.Mode,
				EntityFullName = design.EntityFullName,
				QueryJson = JsonSerializer.Serialize(queryDesignForStorage),
				ColumnsJson = JsonSerializer.Serialize(design.Columns),
				DiagramJson = JsonSerializer.Serialize(tablesForStorage),
				CreatedById = userId,
				ModifiedById = userId,
				CreatedOnMiladiDateTime = DateTime.Now,
				CreatedOnShamsiDateTime = DateTime.Now.ToShamsiDateTime(),
				ModifiedDateMiladiDateTime = DateTime.Now,
				ModifiedDateShamsiDateTime = DateTime.Now.ToShamsiDateTime(),
				IsActive = IsActiveEnum.Active,
				ActionOptions = design.ActionOptions,
				CustomActionButtonsJson = design.CustomActionButtons,
				EventScriptsJson = design.EventScripts
			};

			_context.SavedQueries.Add(report);
			await _context.SaveChangesAsync();

			return report;
		}

		public async Task<SavedQuery> SaveReportAsync(SavedQuery design, long userId)
		{
			// بررسی یکتا بودن نام
			if (!await IsNameUniqueAsync(design.Name))
				throw new InvalidOperationException($"گزارشی با نام '{design.Name}' قبلاً ثبت شده است");
			 

			_context.SavedQueries.Add(design);
			await _context.SaveChangesAsync();

			return design;
		}

		/// <summary>
		/// بروزرسانی گزارش
		/// </summary>
		public async Task<SavedQuery> UpdateReportAsync(long id, QueryDesignRequest design, long userId)
		{
			var report = await _context.SavedQueries.FindAsync(id);
			if (report == null)
				throw new KeyNotFoundException("گزارش مورد نظر یافت نشد");

			// بررسی یکتا بودن نام
			if (report.Name != design.Name && !await IsNameUniqueAsync(design.Name, id))
				throw new InvalidOperationException($"گزارشی با نام '{design.Name}' قبلاً ثبت شده است");

			// ذخیره Tables بدون Columns
			var tablesForStorage = (design.QueryDesign.Tables ?? new List<TableInfo>())
				.Select(t => new TableInfo { Schema = t.Schema, Name = t.Name, Type = t.Type, PositionX = t.PositionX, PositionY = t.PositionY, Columns = null,BoxId=t.BoxId,InstanceId = t.InstanceId }).ToList();
			var queryDesignForStorage = new QueryDesign
			{
				Tables = tablesForStorage,
				Relations = design.QueryDesign.Relations,
				Filters = design.QueryDesign.Filters,
				CustomConditions = design.QueryDesign.CustomConditions,
				CustomQuery = design.QueryDesign.CustomQuery,
				Parameters = design.QueryDesign.Parameters,
				Selects = design.QueryDesign.Selects
			};

			report.Name = design.Name;
			report.Title = design.Title;
			report.Type = design.Type;
			report.Mode = design.Mode;
			report.EntityFullName = design.EntityFullName;
			report.QueryJson = JsonSerializer.Serialize(queryDesignForStorage);
			report.ColumnsJson = JsonSerializer.Serialize(design.Columns);
			report.DiagramJson = JsonSerializer.Serialize(tablesForStorage);
			report.ModifiedDateMiladiDateTime = DateTime.Now;
			report.ModifiedDateShamsiDateTime = DateTime.Now.ToShamsiDateTime();
			report.ModifiedById = userId;
			report.ActionOptions = design.ActionOptions;
			report.CustomActionButtonsJson = design.CustomActionButtons;
			report.EventScriptsJson = design.EventScripts;

			await _context.SaveChangesAsync();

			return report;
		}

		/// <summary>
		/// تبدیل گزارش از حالت طراحی بصری به حالت نوشتن Query:
		/// Query نهایی ذخیره می‌شود، ستون‌ها با نام نتیجه (Alliance) هم‌تراز می‌شوند،
		/// دیاگرام/روابط/فیلترها حذف می‌شوند، دکمه‌ها و اسکریپت‌ها حفظ می‌گردند.
		/// </summary>
		public async Task<SavedQuery> ConvertDesignToQueryModeAsync(long id, long userId)
		{
			var report = await _context.SavedQueries.FindAsync(id);
			if (report == null)
				throw new KeyNotFoundException("گزارش مورد نظر یافت نشد");

			if (report.Mode != ModeQuery.Diagram)
				throw new InvalidOperationException("این گزارش از قبل در حالت Query است");

			var queryDesign = string.IsNullOrWhiteSpace(report.QueryJson)
				? null
				: JsonSerializer.Deserialize<QueryDesign>(report.QueryJson);
			var columns = string.IsNullOrWhiteSpace(report.ColumnsJson)
				? new List<QueryColumn>()
				: (JsonSerializer.Deserialize<List<QueryColumn>>(report.ColumnsJson) ?? new List<QueryColumn>());

			if (columns.Count == 0)
				throw new InvalidOperationException("هیچ ستونی برای تبدیل وجود ندارد");

			string query;
			if (!string.IsNullOrWhiteSpace(queryDesign?.CustomQuery))
			{
				query = queryDesign.CustomQuery.Trim();
			}
			else if (queryDesign?.Tables != null && queryDesign.Tables.Any())
			{
				var sqlColumns = columns.Where(c => !string.IsNullOrEmpty(c.Address)).ToList();
				if (sqlColumns.Count == 0)
					throw new InvalidOperationException("هیچ ستون SQL برای ساخت Query وجود ندارد");

				var hasShaping = sqlColumns.Any(c => c.GroupBy || !string.IsNullOrEmpty(c.Aggregate));
				query = hasShaping
					? _queryBuilder.BuildQueryWithShaping(queryDesign, sqlColumns)
					: _queryBuilder.BuildQuery(queryDesign, sqlColumns);
			}
			else
			{
				throw new InvalidOperationException("امکان ساخت Query از طراحی فعلی وجود ندارد");
			}

			foreach (var column in columns)
			{
				var resultColumnName = !string.IsNullOrWhiteSpace(column.Alliance)
					? column.Alliance
					: column.ColumnName;

				if (!string.IsNullOrWhiteSpace(resultColumnName))
					column.ColumnName = resultColumnName;

				column.TableName = null;
				column.Address = null;
				column.SelectIndex = 0;
				column.GroupBy = false;
				column.Aggregate = null;
				column.SortDirection = null;
				column.SortOrder = null;
			}

			var convertedDesign = new QueryDesign
			{
				Tables = new List<TableInfo>(),
				Relations = new List<TableRelation>(),
				Filters = new List<FilterCondition>(),
				CustomConditions = new List<CustomQueryCondition>(),
				CustomQuery = query,
				Parameters = queryDesign?.Parameters ?? new List<QueryParameter>(),
				Selects = new List<QuerySelectInfo>
				{
					new QuerySelectInfo { Index = 0, Name = "Select1", Title = "Select1" }
				}
			};

			report.Mode = ModeQuery.Query;
			report.QueryJson = JsonSerializer.Serialize(convertedDesign);
			report.ColumnsJson = JsonSerializer.Serialize(columns);
			report.DiagramJson = JsonSerializer.Serialize(new List<TableInfo>());
			report.ModifiedById = userId;
			report.ModifiedDateMiladiDateTime = DateTime.Now;
			report.ModifiedDateShamsiDateTime = DateTime.Now.ToShamsiDateTime();

			await _context.SaveChangesAsync();
			return report;
		}

		/// <summary>
		/// حذف گزارش
		/// </summary>
		public async Task<bool> DeleteReportAsync(long id)
		{
			var report = await _context.SavedQueries.FindAsync(id);
			if (report == null)
				return false;

			_context.SavedQueries.Remove(report);
			await _context.SaveChangesAsync();

			return true;
		}

		/// <summary>
		/// دریافت گزارش
		/// </summary>
		public async Task<SavedQuery> GetReportAsync(long id)
		{
			return await _context.SavedQueries.FindAsync(id);
		}

		/// <summary>
		/// دریافت گزارش بر اساس نام
		/// </summary>
		public async Task<SavedQuery> GetReportByNameAsync(string name)
		{
			return await _context.SavedQueries
			    .FirstOrDefaultAsync(r => r.Name == name && r.IsActive == IsActiveEnum.Active);
		}

		/// <summary>
		/// دریافت تمام گزارش‌ها
		/// </summary>
		public async Task<List<SavedQuery>> GetAllReportsAsync()
		{
			return await _context.SavedQueries
			    .Where(r =>   r.IsActive == IsActiveEnum.Active)
			    .OrderByDescending(r => r.CreatedOnMiladiDateTime)
			    .ToListAsync();
		}

		/// <summary>
		/// دریافت تمام گزارش‌ها
		/// </summary>
		public async Task<List<SavedQuery>> GetAllSavedQueriesForReportsAsync()
		{
			return await _context.SavedQueries
			    .Where(r => r.IsActive == IsActiveEnum.Active && r.Type ==TypeQuery.Report)
			    .OrderByDescending(r => r.CreatedOnMiladiDateTime)
			    .ToListAsync();
		}

		/// <summary>
		///  دریافت تمام گزارش‌ها با دسترسی
		/// </summary>
		public async Task<List<SavedQuery>> GetAllSavedQueriesForReportsFillterByRoleAsync()
		{
			if(_sdk.IsAdministrator)
				return await _context.SavedQueries
						    .Where(r => r.IsActive == IsActiveEnum.Active && r.Type == TypeQuery.Report)
						    .OrderByDescending(r => r.CreatedOnMiladiDateTime)
						    .ToListAsync();


			var saveQueryIds = _sdk.CurrentUser.RoleAccess
				.Where(c => c.ActionAccessItemType == ActionAccessItemType.ReportItem)
				.Select(c => c.RowId);

			return await _context.SavedQueries
			    .Where(r => r.IsActive == IsActiveEnum.Active
			    && r.Type == TypeQuery.Report
			    && saveQueryIds.Contains(r.Id) 
			    )
			    .OrderByDescending(r => r.CreatedOnMiladiDateTime)
			    .ToListAsync();
		}

		/// <summary>
		/// اجرای گزارش (بدون صفحه‌بندی - کل داده)
		/// </summary>
		public async Task<QueryResult> ExecuteReportAsync(long id, IReadOnlyDictionary<string, string> parameterValues = null, string userId = null, string username = null)
		{
			return await ExecuteReportInternalAsync(id, parameterValues, null, null, userId, username);
		}

		/// <summary>
		/// اجرای گزارش با صفحه‌بندی
		/// </summary>
		public async Task<QueryResult> ExecuteReportAsync(long id, IReadOnlyDictionary<string, string> parameterValues, int offset, int limit, string userId = null, string username = null)
		{
			return await ExecuteReportInternalAsync(id, parameterValues, offset, limit, userId, username);
		}

		public async Task<QueryResult> ExecuteReportAsync(DataTableRequest request , IReadOnlyDictionary<string, string> parameterValues, int offset, int limit, string userId = null, string username = null)
		{
			return await ExecuteReportInternalAsync(request, parameterValues, offset, limit, userId, username);
		}

		/// <summary>
		/// اجرای گزارش با پشتیبانی از چند SELECT (حالت نوشتن Query)
		/// </summary>
		public async Task<QueryMultiResult> ExecuteReportMultiAsync(long id, IReadOnlyDictionary<string, string> parameterValues = null, string userId = null, string username = null)
		{
			var report = await GetReportAsync(id);
			if (report == null)
				throw new KeyNotFoundException("گزارش مورد نظر یافت نشد");

			var queryDesign = JsonSerializer.Deserialize<QueryDesign>(report.QueryJson);
			var columns = JsonSerializer.Deserialize<List<QueryColumn>>(report.ColumnsJson);

			// ادغام مقادیر پارامترهای داینامیک با مقادیر پیش‌فرض
			var paramValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (var p in queryDesign.Parameters ?? Enumerable.Empty<QueryParameter>())
			{
				if (!string.IsNullOrEmpty(p.Name) && !paramValues.ContainsKey(p.Name))
					paramValues[p.Name] = p.DefaultValue ?? "";
			}
			if (parameterValues != null)
			{
				foreach (var kv in parameterValues)
					paramValues[kv.Key] = kv.Value ?? "";
			}

			// برای IN/NOT IN که نمی‌توان از پارامتر استفاده کرد - resolve در filter value
			_parameterResolver.ResolveFiltersForInOperator(queryDesign.Filters ?? new List<FilterCondition>(), paramValues, userId, username);

			// ساخت Query (بدون replace - پارامترها در query باقی می‌مانند)
			string query;
			if (!string.IsNullOrEmpty(queryDesign.CustomQuery))
			{
				query = queryDesign.CustomQuery;
			}
			else if (columns != null && columns.Any())
			{
				query = _queryBuilder.BuildQueryWithShaping(queryDesign, columns);
			}
			else
			{
				query = _queryBuilder.BuildQuery(queryDesign, columns);
			}

			// ساخت Dictionary پارامترها
			var sqlParams = _parameterResolver.BuildParameters(query, paramValues, userId, username);

			var resultSets = await _schemaService.ExecuteMultiResultQueryAsync(query, sqlParams);

			return new QueryMultiResult
			{
				ResultSets = resultSets,
				Selects = queryDesign.Selects ?? new List<QuerySelectInfo>()
			};
		}

		private async Task<QueryResult> ExecuteReportInternalAsync(DataTableRequest request, IReadOnlyDictionary<string, string> parameterValues, int? offset, int? limit, string userId, string username)
		{
			var report = await GetReportAsync(request.profileId.Value);
			if (report == null)
				throw new KeyNotFoundException("گزارش مورد نظر یافت نشد");

			var queryDesign = JsonSerializer.Deserialize<QueryDesign>(report.QueryJson);
			var columns = JsonSerializer.Deserialize<List<QueryColumn>>(report.ColumnsJson);

			// ادغام مقادیر پارامترهای داینامیک با مقادیر پیش‌فرض
			var paramValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (var p in queryDesign.Parameters ?? Enumerable.Empty<QueryParameter>())
			{
				if (!string.IsNullOrEmpty(p.Name) && !paramValues.ContainsKey(p.Name))
					paramValues[p.Name] = p.DefaultValue ?? "";
			}
			if (parameterValues != null)
			{
				foreach (var kv in parameterValues)
					paramValues[kv.Key] = kv.Value ?? "";
			}
			// برای IN/NOT IN که نمی‌توان از پارامتر استفاده کرد - resolve در filter value
			_parameterResolver.ResolveFiltersForInOperator(queryDesign.Filters ?? new List<FilterCondition>(), paramValues, userId, username);

			// ساخت Query (بدون replace - پارامترها در query باقی می‌مانند)
			string query;
			if (!string.IsNullOrEmpty(queryDesign.CustomQuery))
			{
				query = queryDesign.CustomQuery;
			}
			else if (columns != null && columns.Any())
			{
				query = _queryBuilder.BuildQueryWithShaping(queryDesign, columns);
			}
			else
			{
				query = _queryBuilder.BuildQuery(queryDesign, columns);
			}

			// ساخت Dictionary پارامترها
			var sqlParams = _parameterResolver.BuildParameters(query, paramValues, userId, username);

			var columnSearchQuery = BuildSearchQueryProfile(request, columns);
			var searchBuilderQuery = request.searchBuilder is { criteria.Count: > 0 }
				? BuildCriteriaQueryProfile(request.searchBuilder.criteria, request.searchBuilder.logic, request, columns)
				: string.Empty;

			// فیلتر سرستون و جستجوی پیشرفته باید با AND ترکیب شوند
			var whereParts = new List<string>();
			if (columnSearchQuery.HasValue())
				whereParts.Add($"({columnSearchQuery})");
			if (searchBuilderQuery.HasValue())
				whereParts.Add($"({searchBuilderQuery})");

			var criteriaQuery = whereParts.Count > 0
				? " Where " + string.Join(" AND ", whereParts)
				: string.Empty;


				var orderByQuery = "";
			// Add order conditions
			if ((request.order is { Length: > 0 }) && request.order.All(c=>c.column != null))
			{
 
				var orderConditions = request.order.Select(o => $"{o.column} {o.dir}");

				orderByQuery = string.Join(", ", orderConditions);
				 
			}
			else
			{
				var colForSort = columns.FirstOrDefault(c => c.Sortable == true);

				orderByQuery = $"[{colForSort?.Alliance ?? colForSort.ColumnName}] DESC";
			}

			   

			return await _schemaService.ExecuteQueryWithPaginationAsync(query , criteriaQuery , orderByQuery, sqlParams, offset.Value, limit.Value);
			 
		}

		private static bool IsDateColumnType(string? type)
		{
			var t = (type ?? "").ToLowerInvariant();
			return t is "datetime" or "date" or "shamsidatetime" or "datetimeshamsi" or "shamsidate" or "dateshamsi";
		}

		private static string EscapeSqlLiteral(string? value)
		{
			return (value ?? string.Empty).Replace("'", "''");
		}

		private static string EscapeLikeLiteral(string? value)
		{
			return EscapeSqlLiteral(value)
				.Replace("[", "[[]")
				.Replace("%", "[%]")
				.Replace("_", "[_]");
		}

		private static string StripDatePrefix(string? value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return string.Empty;

			var v = value.Trim();
			if (v.StartsWith("from ", StringComparison.OrdinalIgnoreCase))
				return v.Substring(5).Trim();
			if (v.StartsWith("to ", StringComparison.OrdinalIgnoreCase))
				return v.Substring(3).Trim();
			if (v.StartsWith("from", StringComparison.OrdinalIgnoreCase))
				return v.Substring(4).Trim();
			if (v.StartsWith("to", StringComparison.OrdinalIgnoreCase))
				return v.Substring(2).Trim();
			return v;
		}

		private static string? ResolveDefaultSearchCondition(string? type, string[] values)
		{
			if (IsDateColumnType(type))
			{
				if (values.Length >= 2)
					return "between";
				if (values.Length == 1)
				{
					var first = values[0] ?? "";
					if (first.Contains("from", StringComparison.OrdinalIgnoreCase))
						return ">";
					if (first.Contains("to", StringComparison.OrdinalIgnoreCase))
						return "<";
				}
				return "between";
			}

			return "contains";
		}

		private static string? BuildColumnSearchClause(string path, string? type, string condition, string[] values)
		{
			var bracketPath = $"[{path}]";

			switch (condition)
			{
				case "null":
					return $"{bracketPath} IS NULL";
				case "!null":
					return $"{bracketPath} IS NOT NULL";
				case "=":
					return values.Length < 1 ? null : $"{bracketPath} = N'{EscapeSqlLiteral(StripDatePrefix(values[0]))}'";
				case "!=":
					return values.Length < 1 ? null : $"{bracketPath} <> N'{EscapeSqlLiteral(StripDatePrefix(values[0]))}'";
				case ">":
					return values.Length < 1 ? null : $"{bracketPath} > N'{EscapeSqlLiteral(StripDatePrefix(values[0]))}'";
				case "<":
					return values.Length < 1 ? null : $"{bracketPath} < N'{EscapeSqlLiteral(StripDatePrefix(values[0]))}'";
				case ">=":
					return values.Length < 1 ? null : $"{bracketPath} >= N'{EscapeSqlLiteral(StripDatePrefix(values[0]))}'";
				case "<=":
					return values.Length < 1 ? null : $"{bracketPath} <= N'{EscapeSqlLiteral(StripDatePrefix(values[0]))}'";
				case "contains":
					return values.Length < 1 ? null : $"{bracketPath} LIKE N'%{EscapeLikeLiteral(values[0])}%'";
				case "!contains":
					return values.Length < 1 ? null : $"{bracketPath} NOT LIKE N'%{EscapeLikeLiteral(values[0])}%'";
				case "starts":
					return values.Length < 1 ? null : $"{bracketPath} LIKE N'{EscapeLikeLiteral(values[0])}%'";
				case "!starts":
					return values.Length < 1 ? null : $"{bracketPath} NOT LIKE N'{EscapeLikeLiteral(values[0])}%'";
				case "ends":
					return values.Length < 1 ? null : $"{bracketPath} LIKE N'%{EscapeLikeLiteral(values[0])}'";
				case "!ends":
					return values.Length < 1 ? null : $"{bracketPath} NOT LIKE N'%{EscapeLikeLiteral(values[0])}'";
				case "between":
					{
						if (values.Length >= 2)
						{
							var fromVal = EscapeSqlLiteral(StripDatePrefix(values[0]));
							var toVal = EscapeSqlLiteral(StripDatePrefix(values[1]));
							return $"{bracketPath} BETWEEN N'{fromVal}' AND N'{toVal}'";
						}
						if (values.Length == 1)
						{
							var first = values[0] ?? "";
							if (first.Contains("from", StringComparison.OrdinalIgnoreCase))
								return $"{bracketPath} > N'{EscapeSqlLiteral(StripDatePrefix(first))}'";
							if (first.Contains("to", StringComparison.OrdinalIgnoreCase))
								return $"{bracketPath} < N'{EscapeSqlLiteral(StripDatePrefix(first))}'";
						}
						return null;
					}
				default:
					// سازگاری با رفتار قبلی
					if (IsDateColumnType(type))
					{
						if (values.Length >= 2)
						{
							var fromVal = EscapeSqlLiteral(StripDatePrefix(values[0]));
							var toVal = EscapeSqlLiteral(StripDatePrefix(values[1]));
							return $"{bracketPath} BETWEEN N'{fromVal}' AND N'{toVal}'";
						}
						if (values.Length == 1)
						{
							var first = values[0] ?? "";
							if (first.Contains("from", StringComparison.OrdinalIgnoreCase))
								return $"{bracketPath} > N'{EscapeSqlLiteral(StripDatePrefix(first))}'";
							return $"{bracketPath} < N'{EscapeSqlLiteral(StripDatePrefix(first))}'";
						}
						return null;
					}
					return values.Length < 1 ? null : $"{bracketPath} LIKE N'%{EscapeLikeLiteral(values[0])}%'";
			}
		}

		private string BuildSearchQueryProfile(DataTableRequest request, List<QueryColumn> column)
		{
			var searchSection = new StringBuilder();
			if (request.columns is not { Count: > 0 })
				return searchSection.ToString();

			foreach (var cl in request.columns)
			{
				if (cl.Search == null)
					continue;

				var values = (cl.Search.value ?? Array.Empty<string>())
					.Where(v => !string.IsNullOrWhiteSpace(v))
					.ToArray();

				var condition = string.IsNullOrWhiteSpace(cl.Search.condition)
					? null
					: cl.Search.condition.Trim();

				if (string.Equals(condition, "none", StringComparison.OrdinalIgnoreCase))
					continue;

				condition ??= ResolveDefaultSearchCondition(cl.type, values);

				var needsValue = condition is not ("null" or "!null");
				if (needsValue && values.Length == 0)
					continue;

				var pc = column.FirstOrDefault(c =>
					c.Alliance == cl.data || c.ColumnName.ToCamelCase() == cl.data);
				if (pc == null)
					continue;

				var path = pc.Alliance ?? pc.ColumnName.ToCamelCase();
				var clause = BuildColumnSearchClause(path, cl.type, condition, values);
				if (string.IsNullOrEmpty(clause))
					continue;

				if (searchSection.Length > 0)
					searchSection.Append(" and ");
				searchSection.Append(clause);
			}

			return searchSection.ToString();
		}

		private string BuildCriteriaQueryProfile(List<DataTableCriterion> criteria, string logic, DataTableRequest request, List<QueryColumn> queryColumn)
		{
			var queryParts = new List<string>();
			foreach (var criterion in criteria)
			{
				if (criterion.criteria != null && criterion.criteria.Count > 0)
				{
					queryParts.Add($"({BuildCriteriaQueryProfile(criterion.criteria, criterion.logic, request, queryColumn)})");
				}
				else if ((criterion.value != null && criterion.value.Any()) || (criterion.condition == "null" || criterion.condition == "!null"))
				{
					string condition = criterion.condition;

					string part;
					var valuePlaceholders = string.Join(",", criterion.value.Select(v => $"{v}"));

					var colName = criterion.name;

					var pc =
					    queryColumn
						   .FirstOrDefault(c => c.Alliance == criterion.name);

					var propAddress = pc?.Alliance ?? pc.ColumnName.ToCamelCase();

					if (pc.SystemType == SystemType.DateTimeShamsi || pc.SystemType == SystemType.DateShamsi)
					{
						propAddress = $" dbo.fn_shmasiToMiladi({propAddress}) ";
					}

					switch (condition)
					{
						case "=":
							part = $" {propAddress} = N'{valuePlaceholders}'";
							break;
						case "!=":
							part = $"{propAddress} <> N'{valuePlaceholders}'";
							break;
						case ">":
							part = $"{propAddress} > N'{valuePlaceholders}'";
							break;
						case ">=":
							part = $"{propAddress} >= N'{valuePlaceholders}'";
							break;
						case "<":
							part = $"{propAddress} < N'{valuePlaceholders}'";
							break;
						case "<=":
							part = $"{propAddress} <= N'{valuePlaceholders}'";
							break;
						case "contains":
							part = $"{propAddress} LIKE N'%{valuePlaceholders}%'";
							break;
						case "!contains":
							part = $"{propAddress} NOT LIKE N'%{valuePlaceholders}%'";
							break;
						case "starts":
							part = $"{propAddress} LIKE N'{valuePlaceholders}%'";
							break;
						case "!starts":
							part = $"{propAddress} Not LIKE N'{valuePlaceholders}%'";
							break;
						case "ends":
							part = $"{propAddress} LIKE N'%{valuePlaceholders}'";
							break;
						case "!ends":
							part = $"{propAddress} Not LIKE N'%{valuePlaceholders}'";
							break;
						case "IN":
							part = $"{propAddress} IN ({valuePlaceholders})";
							break;
						case "NOT IN":
							part = $"{propAddress} NOT IN ({valuePlaceholders})";
							break;
						case "null":
							part = $"{propAddress} IS NULL";
							break;
						case "!null":
							part = $"{propAddress} IS NOT NULL";
							break;
						case "between":
							part = $"{propAddress} BETWEEN N'{criterion.value[0]}' And N'{criterion.value[1]}'";
							break;
						case "!between":
							part = $"{propAddress} NOT BETWEEN N'{criterion.value[0]}' And N'{criterion.value[1]}'";
							break;
						default:
							throw new ArgumentException($"Unsupported operator: {criterion.condition}");
					}
					queryParts.Add($"({part})");
				}

			}

			if (queryParts.Count() > 0)
				return string.Join($" {logic} ", queryParts);
			else
			{
				return "";

			}
		}

 
		private async Task<QueryResult> ExecuteReportInternalAsync(long id, IReadOnlyDictionary<string, string> parameterValues, int? offset, int? limit, string userId, string username)
		{
			var report = await GetReportAsync(id);
			if (report == null)
				throw new KeyNotFoundException("گزارش مورد نظر یافت نشد");

			var queryDesign = JsonSerializer.Deserialize<QueryDesign>(report.QueryJson);
			var columns = JsonSerializer.Deserialize<List<QueryColumn>>(report.ColumnsJson);

			// ادغام مقادیر پارامترهای داینامیک با مقادیر پیش‌فرض
			var paramValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (var p in queryDesign.Parameters ?? Enumerable.Empty<QueryParameter>())
			{
				if (!string.IsNullOrEmpty(p.Name) && !paramValues.ContainsKey(p.Name))
					paramValues[p.Name] = p.DefaultValue ?? "";
			}
			if (parameterValues != null)
			{
				foreach (var kv in parameterValues)
					paramValues[kv.Key] = kv.Value ?? "";
			}

			// برای IN/NOT IN که نمی‌توان از پارامتر استفاده کرد - resolve در filter value
			_parameterResolver.ResolveFiltersForInOperator(queryDesign.Filters ?? new List<FilterCondition>(), paramValues, userId, username);

			// ساخت Query (بدون replace - پارامترها در query باقی می‌مانند)
			string query;
			if (!string.IsNullOrEmpty(queryDesign.CustomQuery))
			{
				query = queryDesign.CustomQuery;
			}
			else if (columns != null && columns.Any())
			{
				query = _queryBuilder.BuildQueryWithShaping(queryDesign, columns);
			}
			else
			{
				query = _queryBuilder.BuildQuery(queryDesign, columns);
			}

			// ساخت Dictionary پارامترها
			var sqlParams = _parameterResolver.BuildParameters(query, paramValues, userId, username);

			if (offset.HasValue && limit.HasValue && limit.Value > 0)
			{
				return await _schemaService.ExecuteQueryWithPaginationAsync(query, sqlParams, offset.Value, limit.Value);
			}

			return await _schemaService.ExecuteQueryAsync(query, sqlParams);
		}

		/// <summary>
		/// بررسی یکتا بودن نام
		/// </summary>
		public async Task<bool> IsNameUniqueAsync(string name, long? excludeId = null)
		{
			var query = _context.SavedQueries.Where(r => r.Name == name && r.IsActive == IsActiveEnum.Active);

			if (excludeId.HasValue)
				query = query.Where(r => r.Id != excludeId.Value);

			return !await query.AnyAsync();
		}

        

        List<SavedQuery> IQueryService.GetDataTableProfileListByEntityName(string? entityName)
        {
			return _context.SavedQueries
				.Select(c=>new SavedQuery() 
				{	Id = c.Id ,
					IsActive = c.IsActive,
					Title = c.Title,
					EntityFullName = c.EntityFullName,
					Name = c.Name,
				})
				.Where(r => r.EntityFullName != null &&  r.EntityFullName.ToLower() == entityName.ToLower()
			&& r.IsActive == IsActiveEnum.Active).ToList();
		}

        List<SavedQuery> IQueryService.GetDataTableProfileById(List<long?> ids , string? entityName)
        {
			return _context.SavedQueries
				    .Select(c => new SavedQuery()
				    {
					    Id = c.Id,
					    IsActive = c.IsActive,
					    Title = c.Title,
					    EntityFullName = c.EntityFullName,
					    Name = c.Name,
				    })
				    .Where(r => ids.Contains(r.Id)
				    && r.EntityFullName.ToLower() == entityName.ToLower()
			    && r.IsActive == IsActiveEnum.Active).ToList();
		}

	public Dictionary<string, string> ExtractParameterValuesFromRequest(DataTableRequest request)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (request?.searchBuilder?.criteria == null) return dict;
            foreach (var c in request.searchBuilder.criteria)
            {
                if (!string.IsNullOrEmpty(c.data) && c.value != null && c.value.Count > 0)
                    dict[c.data] = c.value[0] ?? "";
            }
            return dict;
        }

      

        
    }
}
