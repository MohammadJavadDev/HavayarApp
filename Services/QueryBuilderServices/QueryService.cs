using Common.Utilities;
using Data;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Services.QueryBuilderServices
{
	/// <summary>
	/// سرویس مدیریت گزارش‌ها
	/// </summary>
	public interface IQueryService
	{
		Task<SavedQuery> SaveReportAsync(ReportDesign design, long userId);
		Task<SavedQuery> UpdateReportAsync(long id, ReportDesign design, long userId);
		Task<bool> DeleteReportAsync(long id);
		Task<SavedQuery> GetReportAsync(long id);
		Task<SavedQuery> GetReportByNameAsync(string name);
		Task<List<SavedQuery>> GetAllReportsAsync();
		Task<ReportResult> ExecuteReportAsync(long id, IReadOnlyDictionary<string, string> parameterValues = null, string userId = null, string username = null);
		Task<bool> IsNameUniqueAsync(string name, long? excludeId = null);
	}

	public class QueryService : IQueryService
	{
		private readonly ApplicationDbContext _context;
		private readonly IQueryBuilderService _queryBuilder;
		private readonly IDatabaseSchemaService _schemaService;
		private readonly IParameterResolverService _parameterResolver;

		public QueryService(
		    ApplicationDbContext context,
		    IQueryBuilderService queryBuilder,
		    IDatabaseSchemaService schemaService,
		    IParameterResolverService parameterResolver)
		{
			_context = context;
			_queryBuilder = queryBuilder;
			_schemaService = schemaService;
			_parameterResolver = parameterResolver;
		}

		/// <summary>
		/// ذخیره گزارش جدید
		/// </summary>
		public async Task<SavedQuery> SaveReportAsync(ReportDesign design, long userId)
		{
			// بررسی یکتا بودن نام
			if (!await IsNameUniqueAsync(design.Name))
				throw new InvalidOperationException($"گزارشی با نام '{design.Name}' قبلاً ثبت شده است");

			// ذخیره Tables بدون Columns - ستون‌ها هنگام ویرایش از سرور دریافت می‌شوند
			var tablesForStorage = (design.QueryDesign.Tables ?? new List<TableInfo>())
				.Select(t => new TableInfo { Schema = t.Schema, Name = t.Name, Type = t.Type, PositionX = t.PositionX, PositionY = t.PositionY, Columns = null }).ToList();
			var queryDesignForStorage = new QueryDesign
			{
				Tables = tablesForStorage,
				Relations = design.QueryDesign.Relations,
				Filters = design.QueryDesign.Filters,
				CustomQuery = design.QueryDesign.CustomQuery,
				Parameters = design.QueryDesign.Parameters
			};

			var report = new SavedQuery
			{
				Name = design.Name,
				Title = design.Title,
				QueryJson = JsonSerializer.Serialize(queryDesignForStorage),
				ColumnsJson = JsonSerializer.Serialize(design.Columns),
				DiagramJson = JsonSerializer.Serialize(tablesForStorage),
				CreatedById = userId,
				ModifiedById = userId,
				CreatedOnMiladiDateTime = DateTime.Now,
				CreatedOnShamsiDateTime = DateTime.Now.ToShamsiDateTime(),
				ModifiedDateMiladiDateTime = DateTime.Now,
				ModifiedDateShamsiDateTime = DateTime.Now.ToShamsiDateTime(),
				IsActive = IsActiveEnum.Active
			};

			_context.SavedQueries.Add(report);
			await _context.SaveChangesAsync();

			return report;
		}

		/// <summary>
		/// بروزرسانی گزارش
		/// </summary>
		public async Task<SavedQuery> UpdateReportAsync(long id, ReportDesign design, long userId)
		{
			var report = await _context.SavedQueries.FindAsync(id);
			if (report == null)
				throw new KeyNotFoundException("گزارش مورد نظر یافت نشد");

			// بررسی یکتا بودن نام
			if (report.Name != design.Name && !await IsNameUniqueAsync(design.Name, id))
				throw new InvalidOperationException($"گزارشی با نام '{design.Name}' قبلاً ثبت شده است");

			// ذخیره Tables بدون Columns
			var tablesForStorage = (design.QueryDesign.Tables ?? new List<TableInfo>())
				.Select(t => new TableInfo { Schema = t.Schema, Name = t.Name, Type = t.Type, PositionX = t.PositionX, PositionY = t.PositionY, Columns = null }).ToList();
			var queryDesignForStorage = new QueryDesign
			{
				Tables = tablesForStorage,
				Relations = design.QueryDesign.Relations,
				Filters = design.QueryDesign.Filters,
				CustomQuery = design.QueryDesign.CustomQuery,
				Parameters = design.QueryDesign.Parameters
			};

			report.Name = design.Name;
			report.Title = design.Title;
			report.QueryJson = JsonSerializer.Serialize(queryDesignForStorage);
			report.ColumnsJson = JsonSerializer.Serialize(design.Columns);
			report.DiagramJson = JsonSerializer.Serialize(tablesForStorage);
			report.ModifiedDateMiladiDateTime = DateTime.Now;
			report.ModifiedDateShamsiDateTime = DateTime.Now.ToShamsiDateTime();
			report.ModifiedById = userId;

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
		/// اجرای گزارش
		/// </summary>
		public async Task<ReportResult> ExecuteReportAsync(long id, IReadOnlyDictionary<string, string> parameterValues = null, string userId = null, string username = null)
		{
			var report = await GetReportAsync(id);
			if (report == null)
				throw new KeyNotFoundException("گزارش مورد نظر یافت نشد");

			var queryDesign = JsonSerializer.Deserialize<QueryDesign>(report.QueryJson);
			var columns = JsonSerializer.Deserialize<List<ReportColumn>>(report.ColumnsJson);

			// ادغام مقادیر پارامترهای داینامیک با مقادیر پیش‌فرض
			var paramValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (var p in queryDesign.Parameters ?? Enumerable.Empty<ReportParameter>())
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

			// ساخت Dictionary پارامترها و اجرا (پارامترها با AddWithValue به command اضافه می‌شوند)
			var sqlParams = _parameterResolver.BuildParameters(query, paramValues, userId, username);
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
	}
}
