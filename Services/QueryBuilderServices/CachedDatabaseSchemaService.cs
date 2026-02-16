using Entities.Base;
using Entities.Base.FormBuilder;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
 

namespace Services.QueryBuilderServices
{
	/// <summary>
	/// سرویس کش‌دار برای Schema دیتابیس - بهبود Performance
	/// </summary>
	public class CachedDatabaseSchemaService : IDatabaseSchemaService
	{
		private readonly IDatabaseSchemaService _innerService;
		private readonly IMemoryCache _cache;
		private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(10);

		public CachedDatabaseSchemaService(
		    DatabaseSchemaService innerService,
		    IMemoryCache cache)
		{
			_innerService = innerService;
			_cache = cache;
		}

		public async Task<List<TableInfo>> GetTablesAndViewsAsync()
		{
			const string cacheKey = "schema:tables-and-views";

			return await _cache.GetOrCreateAsync(cacheKey, async entry =>
			{
				entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
				return await _innerService.GetTablesAndViewsAsync();
			});
		}

		public async Task<List<TableColumnInfo>> GetTableColumnsAsync(string tableName, string schema = "dbo", string? entityName = null)
		{
			var cacheKey = $"schema:columns:{schema}.{tableName}";
			var entityFullName = entityName;

			return await _cache.GetOrCreateAsync(cacheKey, async entry =>
			{
				entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
				return await _innerService.GetTableColumnsAsync(tableName, schema, entityFullName);
			});
		}

		public async Task<List<TableRelation>> GetTableRelationsAsync(string tableName, string schema = "dbo")
		{
			var cacheKey = $"schema:relations:{schema}.{tableName}";

			return await _cache.GetOrCreateAsync(cacheKey, async entry =>
			{
				entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
				return await _innerService.GetTableRelationsAsync(tableName, schema);
			});
		}

		// این متدها کش نمی‌شوند چون نتایج Query ها همیشه متغیر هستند
		public Task<ReportResult> ExecuteQueryAsync(string query, Dictionary<string, object> parameters = null)
		{
			return _innerService.ExecuteQueryAsync(query, parameters);
		}

		public Task<bool> ValidateQueryAsync(string query)
		{
			return _innerService.ValidateQueryAsync(query);
		}

		/// <summary>
		/// پاک کردن Cache
		/// </summary>
		public void ClearCache()
		{
			// در صورت نیاز می‌توانید کلیدهای خاص را پاک کنید
			// یا تمام cache را پاک کنید
		}

        public Task<List<ConnectionStringInfo>> GetConnectionStringsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<ConnectionTestResult> TestConnectionAsync(string connectionString)
        {
            throw new NotImplementedException();
        }

        public Task<List<DatabaseTableInfo>> GetTablesAsync(string connectionString)
        {
            throw new NotImplementedException();
        }

         
    }

	/// <summary>
	/// Extension Methods برای ثبت سرویس Cached
	/// </summary>
	public static class CachedSchemaServiceExtensions
	{
		public static IServiceCollection AddCachedDatabaseSchema(this IServiceCollection services)
		{
			services.AddScoped<DatabaseSchemaService>();
			services.AddScoped<IDatabaseSchemaService>(provider =>
			{
				var innerService = provider.GetRequiredService<DatabaseSchemaService>();
				var cache = provider.GetRequiredService<IMemoryCache>();
				return new CachedDatabaseSchemaService(innerService, cache);
			});

			return services;
		}
	}
}
