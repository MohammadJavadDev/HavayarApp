using Entities.Base;
using Entities.Base.DataTable;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

public interface IRepository<TEntity> where TEntity : BaseEntity, new()
{
	#region Properties
	DbSet<TEntity> Entities { get; }
	IQueryable<TEntity> Table { get; }
	IQueryable<TEntity> TableNoTracking { get; }
	string? ConnectionString { get; set; }
	#endregion

	#region Update Field Methods - متدهای آپدیت فیلد خاص

	/// <summary>
	/// آپدیت یک فیلد خاص بدون دریافت کل رکورد (Async)
	/// </summary>
	Task<bool> UpdateFieldsAsync(
	    object id,
	    Expression<Func<TEntity, object>> propertyExpression,
	    object value,
	    CancellationToken cancellationToken = default);

	/// <summary>
	/// آپدیت چند فیلد با استفاده از Dictionary (Async)
	/// </summary>
	Task<bool> UpdateFieldsAsync(
	    object id,
	    Dictionary<string, object> fieldsToUpdate,
	    CancellationToken cancellationToken = default);

	/// <summary>
	/// آپدیت یک فیلد برای چند رکورد (Bulk Update Field) - بسیار بهینه
	/// </summary>
	 Task<int> BulkUpdateFieldAsync<TProperty>(
	    Expression<Func<TEntity, bool>> predicate,
	    Expression<Func<TEntity, TProperty>> propertySelector,
	    TProperty value,
	    CancellationToken cancellationToken = default);

	/// <summary>
	/// آپدیت یک فیلد برای چند رکورد با استفاده از Expression برای مقدار جدید
	/// </summary>
	Task<int> BulkUpdateFieldAsync<TProperty>(
    Expression<Func<TEntity, bool>> predicate,
    Expression<Func<TEntity, TProperty>> propertySelector,
    Expression<Func<TEntity, TProperty>> valueSelector,
    CancellationToken cancellationToken = default);

	/// <summary>
	/// آپدیت با Raw SQL - برای آپدیت های بسیار سریع
	/// </summary>
	Task<int> UpdateFieldByRawSqlAsync(
	    object id,
	    string fieldName,
	    object value,
	    CancellationToken cancellationToken = default);

	/// <summary>
	/// آپدیت یک فیلد (Sync)
	/// </summary>
	bool UpdateFields(
	    object id,
	    Expression<Func<TEntity, object>> propertyExpression,
	    object value);

	#endregion

	#region Async CRUD Methods

	/// <summary>
	/// دریافت Entity با Id
	/// </summary>
	Task<TEntity?> GetByIdAsync(CancellationToken cancellationToken, params object[] ids);

	/// <summary>
	/// ذخیره (Add یا Update بسته به وضعیت)
	/// </summary>
	Task<TEntity> SaveAsync(TEntity entity, CancellationToken cancellationToken, bool saveAudit = true, bool saveNow = true);

	/// <summary>
	/// اضافه کردن یک Entity
	/// </summary>
	Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken, bool saveAudit = true, bool saveNow = true);

	/// <summary>
	/// اضافه کردن لیست Entity
	/// </summary>
	Task<List<TEntity>> AddRangeAsync(List<TEntity> entities, CancellationToken cancellationToken, bool saveNow = true);

	/// <summary>
	/// آپدیت یک Entity
	/// </summary>
	Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken, bool saveAudit = true, bool saveNow = true);

	/// <summary>
	/// آپدیت لیست Entity
	/// </summary>
	Task<List<TEntity>> UpdateRangeAsync(List<TEntity> entities, CancellationToken cancellationToken, bool saveNow = true);

	/// <summary>
	/// حذف یک Entity
	/// </summary>
	Task DeleteAsync(TEntity entity, CancellationToken cancellationToken, bool saveNow = true);

	/// <summary>
	/// حذف لیست Entity
	/// </summary>
	Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken, bool saveNow = true);

	/// <summary>
	/// حذف با شرط (بهینه شده)
	/// </summary>
	Task<int> DeleteWhereAsync(
	    Expression<Func<TEntity, bool>> predicate,
	    CancellationToken cancellationToken = default);

	#endregion

	#region Sync CRUD Methods

	/// <summary>
	/// دریافت Entity با Id (Sync)
	/// </summary>
	TEntity? GetById(params object[] ids);

	/// <summary>
	/// ذخیره (Add یا Update بسته به وضعیت) - Sync
	/// </summary>
	TEntity Save(TEntity entity, bool saveAudit = true, bool saveNow = true);

	/// <summary>
	/// اضافه کردن یک Entity (Sync)
	/// </summary>
	TEntity Add(TEntity entity, bool saveAudit = true, bool saveNow = true);

	/// <summary>
	/// اضافه کردن یک Entity (Sync)
	/// </summary>
	List<TEntity> AddRange(List<TEntity> entities, bool saveNow = true);


	/// <summary>
	/// آپدیت یک Entity (Sync)
	/// </summary>
	TEntity Update(TEntity entity, bool saveAudit = true, bool saveNow = true);


	/// <summary>
	/// آپدیت یک Entity (Sync)
	/// </summary>
	  List<TEntity> UpdateRange(List<TEntity> entities, bool saveNow = true);

	/// <summary>
	/// حذف یک Entity (Sync)
	/// </summary>
	void Delete(TEntity entity, bool saveNow = true);

	#endregion

	#region Execute Command Methods

	/// <summary>
	/// اجرای دستور SQL (Insert, Update, Delete) - Async
	/// </summary>
	Task<int> ExecuteCommandAsync(
	    string query,
	    object? parameters = null,
	    CancellationToken cancellationToken = default);

	/// <summary>
	/// اجرای دستور SQL (Insert, Update, Delete) - Sync
	/// </summary>
	int ExecuteCommand(string query, object? parameters = null);

	#endregion

	#region Execute Query Methods

	/// <summary>
	/// اجرای Query و دریافت لیست Entity - Async
	/// </summary>
	Task<IEnumerable<T>> ExecuteQueryAsync<T>(
	    string query,
	    object? parameters = null,
	    CancellationToken cancellationToken = default) where T : class, new();

	/// <summary>
	/// اجرای Query و دریافت Dictionary - Async
	/// </summary>
	Task<IEnumerable<Dictionary<string, object>>> ExecuteQueryAsync(
	    string query,
	    object? parameters = null,
	    CancellationToken cancellationToken = default);

	/// <summary>
	/// اجرای Query و دریافت اولین رکورد - Async
	/// </summary>
	Task<T?> ExecuteQueryFirstOrDefaultAsync<T>(
	    string query,
	    object? parameters = null,
	    CancellationToken cancellationToken = default) where T : class, new();

	/// <summary>
	/// اجرای Query و دریافت مقدار Scalar - Async
	/// </summary>
	Task<object?> ExecuteScalarAsync(
	    string query,
	    object? parameters = null,
	    CancellationToken cancellationToken = default);

	/// <summary>
	/// اجرای Query و دریافت Dictionary - Sync
	/// </summary>
	IEnumerable<Dictionary<string, object>> ExecuteQuery(string query, object? parameters = null);

	/// <summary>
	/// دریافت Header های Stored Procedure
	/// </summary>
	Task<List<List<Dictionary<string, object>>>> ExecuteQueryGetHeadersAsync(
	    string query,
	    object? parameters = null,
	    CancellationToken cancellationToken = default);

	#endregion

	#region DataTable & Excel Methods

	/// <summary>
	/// دریافت داده برای DataTable با Pagination
	/// </summary>
	Task<DataTableResponse> FetchDataAsync(DataTableRequest request, CancellationToken cn);

	/// <summary>
	/// Export داده به Excel
	/// </summary>
	Task ExportLargeDataToExcelAsync(DataTableRequest request, Stream outputStream, string licensePath);

	#endregion

	#region Attach & Detach Methods

	/// <summary>
	/// Detach کردن Entity از Context
	/// </summary>
	void Detach(TEntity entity);

	/// <summary>
	/// Attach کردن Entity به Context
	/// </summary>
	void Attach(TEntity entity);

	#endregion

	#region Explicit Loading Methods

	/// <summary>
	/// Load کردن Collection Navigation Property - Async
	/// </summary>
	Task LoadCollectionAsync<TProperty>(
	    TEntity entity,
	    Expression<Func<TEntity, IEnumerable<TProperty>>> collectionProperty,
	    CancellationToken cancellationToken) where TProperty : class;

	/// <summary>
	/// Load کردن Collection Navigation Property - Sync
	/// </summary>
	void LoadCollection<TProperty>(
	    TEntity entity,
	    Expression<Func<TEntity, IEnumerable<TProperty>>> collectionProperty) where TProperty : class;

	/// <summary>
	/// Load کردن Reference Navigation Property - Async
	/// </summary>
	Task LoadReferenceAsync<TProperty>(
	    TEntity entity,
	    Expression<Func<TEntity, TProperty>> referenceProperty,
	    CancellationToken cancellationToken) where TProperty : class;

	/// <summary>
	/// Load کردن Reference Navigation Property - Sync
	/// </summary>
	void LoadReference<TProperty>(
	    TEntity entity,
	    Expression<Func<TEntity, TProperty>> referenceProperty) where TProperty : class;

	#endregion
}