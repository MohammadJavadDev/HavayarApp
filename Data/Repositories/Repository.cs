using Aspose.Cells;
using Common;
using Common.Utilities;
using Data;
using Data.Contracts;
using Data.Services;
using Data.SystemAuth;
using Entities.Base;
using Entities.Base.DataTable;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Z.EntityFramework.Extensions;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

public class Repository<TEntity> : IRepository<TEntity>, IScopedDependency
	where TEntity : BaseEntity, new()
{
	protected readonly ApplicationDbContext DbContext;
	private readonly ISdk? sdk;
	private readonly IDataTableQueryBuilder? _dataTableQuery;
	private readonly IAuditService _auditService;

	public  DbSet<TEntity> Entities { get; }
	public virtual IQueryable<TEntity> Table => Entities;
	public virtual IQueryable<TEntity> TableNoTracking => Entities.AsNoTracking();

	public string? ConnectionString { get; set; }

	public Repository(ApplicationDbContext dbContext, ISdk? sdk, IDataTableQueryBuilder dataTableQuery, IAuditService auditService)
	{
		DbContext = dbContext;
		Entities = DbContext.Set<TEntity>();
		this.sdk = sdk;
		_dataTableQuery = dataTableQuery;
		_auditService = auditService;
		ConnectionString = dbContext.Database.GetConnectionString();
		 
	}

	

	#region Helper Methods
	private void SetEntityDates(TEntity entity, bool isNew)
	{
		var now = DateTime.Now;
		entity.ModifiedDateMiladiDateTime = now;
		entity.ModifiedDateShamsiDateTime = now.ToShamsiDateTime();

		if (isNew)
		{
			entity.CreatedOnMiladiDateTime = now;
			entity.CreatedOnShamsiDateTime = now.ToShamsiDateTime();
		}
	}

	private void SetEntityUser(TEntity entity, bool isNew)
	{
		if (sdk?.CurrentUser != null)
		{
			entity.ModifiedById = sdk.CurrentUser.Id;
			entity.ModifiedByName = sdk.CurrentUser.FullName;

			if (isNew)
			{
				entity.IsActive = IsActiveEnum.Active;
				entity.CreatedById = sdk.CurrentUser.Id;
				entity.CreatedByName = sdk.CurrentUser.FullName;
			}
		}
	}

	private void PreserveCreationInfo(TEntity entity, TEntity oldEntity)
	{
		entity.CreatedById = oldEntity.CreatedById;
		entity.CreatedByName = oldEntity.CreatedByName;
		entity.CreatedOnMiladiDateTime = oldEntity.CreatedOnMiladiDateTime;
		entity.CreatedOnShamsiDateTime = oldEntity.CreatedOnShamsiDateTime;
		entity.IsActive = oldEntity.IsActive;
	}

	private void AddParametersToCommand(SqlCommand command, object? parameters)
	{
		if (parameters == null) return;

		if (parameters is Dictionary<string, object> dict)
		{
			foreach (var param in dict)
			{
				command.Parameters.AddWithValue($"@{param.Key}", param.Value ?? DBNull.Value);
			}
		}
		else
		{
			foreach (var prop in parameters.GetType().GetProperties())
			{
				command.Parameters.AddWithValue($"@{prop.Name}", prop.GetValue(parameters) ?? DBNull.Value);
			}
		}
	}

	private T MapReaderToEntity<T>(SqlDataReader reader) where T : class, new()
	{
		var entity = new T();

		for (var i = 0; i < reader.FieldCount; i++)
		{
			if (reader.IsDBNull(i)) continue;

			var propertyName = reader.GetName(i);
			var property = typeof(T).GetProperty(propertyName);

			if (property == null) continue;

			var value = reader.GetValue(i);
			var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

			if (propertyType.IsEnum)
			{
				property.SetValue(entity, Enum.ToObject(propertyType, value));
			}
			else
			{
				property.SetValue(entity, value);
			}
		}

		return entity;
	}
	#endregion

	#region New Update Methods - بهینه شده برای آپدیت فیلدهای خاص

	/// <summary>
	/// آپدیت یک یا چند فیلد خاص بدون دریافت کل رکورد (Async)
	/// </summary>
	public virtual async Task<bool> UpdateFieldsAsync(
		object id,
		Expression<Func<TEntity, object>> propertyExpression,
		object value,
		CancellationToken cancellationToken = default)
	{
		var entity = await Entities.FindAsync(new object[] { id }, cancellationToken);
		if (entity == null) return false;

		var memberExpression = propertyExpression.Body as MemberExpression
			?? ((UnaryExpression)propertyExpression.Body).Operand as MemberExpression;

		var property = memberExpression?.Member as PropertyInfo;
		if (property == null) return false;

		property.SetValue(entity, value);
		SetEntityDates(entity, isNew: false);
		SetEntityUser(entity, isNew: false);

		await DbContext.SaveChangesAsync(cancellationToken);
		return true;
	}

	/// <summary>
	/// آپدیت چند فیلد با استفاده از Dictionary (Async)
	/// </summary>
	public virtual async Task<bool> UpdateFieldsAsync(
		object id,
		Dictionary<string, object> fieldsToUpdate,
		CancellationToken cancellationToken = default)
	{
		var entity = await Entities.FindAsync(new object[] { id }, cancellationToken);
		if (entity == null) return false;

		var entityType = typeof(TEntity);
		foreach (var field in fieldsToUpdate)
		{
			var property = entityType.GetProperty(field.Key);
			if (property != null && property.CanWrite)
			{
				property.SetValue(entity, field.Value);
			}
		}

		SetEntityDates(entity, isNew: false);
		SetEntityUser(entity, isNew: false);

		await DbContext.SaveChangesAsync(cancellationToken);
		return true;
	}

	/// <summary>
	/// آپدیت یک فیلد برای چند رکورد (Bulk Update Field) - بسیار بهینه
	/// </summary>
	public virtual async Task<int> BulkUpdateFieldAsync<TProperty>(
	    Expression<Func<TEntity, bool>> predicate,
	  
	    Expression<Func<TEntity, TProperty>> propertySelector,
	    TProperty value,
	    CancellationToken cancellationToken = default)
	{
		return await Entities
		    .Where(predicate)
		    .ExecuteUpdateAsync(
			   setters => setters.SetProperty(
				  propertySelector,
				  value
			   ),
			   cancellationToken
		    );
	}


	/// <summary>
	/// آپدیت یک فیلد برای چند رکورد با استفاده از Expression برای مقدار جدید
	/// </summary>
	public virtual async Task<int> BulkUpdateFieldAsync<TProperty>(
    Expression<Func<TEntity, bool>> predicate,
    Expression<Func<TEntity, TProperty>> propertySelector,
    Expression<Func<TEntity, TProperty>> valueSelector,
    CancellationToken cancellationToken = default)
	{
		return await Entities
		    .Where(predicate)
		    .ExecuteUpdateAsync(
			   setters => setters.SetProperty(
				  propertySelector,
				  valueSelector
			   ),
			   cancellationToken
		    );
	}


	/// <summary>
	/// آپدیت با Raw SQL - برای آپدیت های بسیار سریع
	/// </summary>
	public virtual async Task<int> UpdateFieldByRawSqlAsync(
		object id,
		string fieldName,
		object value,
		CancellationToken cancellationToken = default)
	{
		var tableName = typeof(TEntity).Name;
		var sql = $"UPDATE {tableName} SET {fieldName} = @Value, ModifiedDateMiladiDateTime = @ModifiedDate WHERE Id = @Id";

		return await DbContext.Database.ExecuteSqlRawAsync(
			sql,
			new[]
			{
			 new SqlParameter("@Value", value ?? DBNull.Value),
			 new SqlParameter("@ModifiedDate", DateTime.Now),
			 new SqlParameter("@Id", id)
			},
			cancellationToken);
	}

	/// <summary>
	/// آپدیت یک فیلد (Sync)
	/// </summary>
	public virtual bool UpdateFields(
		object id,
		Expression<Func<TEntity, object>> propertyExpression,
		object value)
	{
		var entity = Entities.Find(id);
		if (entity == null) return false;

		var memberExpression = propertyExpression.Body as MemberExpression
			?? ((UnaryExpression)propertyExpression.Body).Operand as MemberExpression;

		var property = memberExpression?.Member as PropertyInfo;
		if (property == null) return false;

		property.SetValue(entity, value);
		SetEntityDates(entity, isNew: false);
		SetEntityUser(entity, isNew: false);

		DbContext.SaveChanges();
		return true;
	}

	#endregion

	#region Optimized Async Methods

	public virtual async Task<TEntity?> GetByIdAsync(CancellationToken cancellationToken, params object[] ids)
	{
		return await Entities.FindAsync(ids, cancellationToken);
	}

	public virtual async Task<TEntity> SaveAsync(TEntity entity, CancellationToken cancellationToken, bool saveAudit = false, bool saveNow = true)
	{
		var isNew = entity.Id == 0 || entity.Id == null || !await TableNoTracking.AnyAsync(c => c.Id == entity.Id, cancellationToken);

		return isNew
			? await AddAsync(entity, cancellationToken, saveAudit, saveNow)
			: await UpdateAsync(entity, cancellationToken, saveAudit, saveNow);
	}

	public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken, bool saveAudit = false, bool saveNow = true)
	{
		Assert.NotNull(entity, nameof(entity));

		SetEntityDates(entity, isNew: true);
		SetEntityUser(entity, isNew: true);
		entity.IsActive = IsActiveEnum.Active;

		await Entities.AddAsync(entity, cancellationToken);

		if (saveNow)
		{
			await DbContext.SaveChangesAsync(cancellationToken);

			if (saveAudit && _auditService != null)
			{
				await _auditService.SaveAuditAsync(entity, null, AuditLogType.Add, cancellationToken);
			}
		}

		return entity;
	}

	public virtual async Task<List<TEntity>> AddRangeAsync(List<TEntity> entities, CancellationToken cancellationToken, bool saveNow = true)
	{
		Assert.NotNull(entities, nameof(entities));

		foreach (var entity in entities)
		{
			SetEntityDates(entity, isNew: true);
			SetEntityUser(entity, isNew: true);
			entity.IsActive = IsActiveEnum.Active;
		}

		await Entities.BulkInsertAsync(entities, cancellationToken);
		return entities;
	}

	public virtual async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken, bool saveAudit = false, bool saveNow = true)
	{
		Assert.NotNull(entity, nameof(entity));

		var oldEntity = await TableNoTracking.FirstOrDefaultAsync(c => c.Id == entity.Id, cancellationToken);
		if (oldEntity == null)
		{
			throw new InvalidOperationException($"Entity with Id {entity.Id} not found.");
		}

		SetEntityDates(entity, isNew: false);
		SetEntityUser(entity, isNew: false);
		PreserveCreationInfo(entity, oldEntity);

		Entities.Update(entity);

		if (saveNow)
		{
			await DbContext.SaveChangesAsync(cancellationToken);

			if (saveAudit && _auditService != null)
			{
				await _auditService.SaveAuditAsync(entity, oldEntity, AuditLogType.Update, cancellationToken);
			}
		}

		return entity;
	}

	public virtual async Task<List<TEntity>> UpdateRangeAsync(List<TEntity> entities, CancellationToken cancellationToken, bool saveNow = true)
	{
		Assert.NotNull(entities, nameof(entities));

		var entityIds = entities.Where(e => e.Id.HasValue).Select(e => e.Id!.Value).ToList();
		var oldEntities = await TableNoTracking
			.Where(e => e.Id.HasValue && entityIds.Contains(e.Id.Value))
			.ToListAsync(cancellationToken);

		var oldEntitiesDict = oldEntities.Where(e => e.Id.HasValue).ToDictionary(e => e.Id!.Value);

		foreach (var entity in entities)
		{
			if (entity.Id.HasValue && oldEntitiesDict.TryGetValue(entity.Id.Value, out var oldEntity))
			{
				SetEntityDates(entity, isNew: false);
				SetEntityUser(entity, isNew: false);
				PreserveCreationInfo(entity, oldEntity);
			}
		}

		await Entities.BulkUpdateAsync(entities, cancellationToken);
		return entities;
	}

	public virtual async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken, bool saveNow = true)
	{
		Assert.NotNull(entity, nameof(entity));

		var entityForAudit = await TableNoTracking.FirstOrDefaultAsync(c => c.Id == entity.Id, cancellationToken);

		Entities.Remove(entity);

		if (saveNow)
		{
			await DbContext.SaveChangesAsync(cancellationToken);

			if (_auditService != null && entityForAudit != null)
			{
				await _auditService.SaveAuditAsync(entityForAudit, null, AuditLogType.Delete, cancellationToken);
			}
		}
	}

	public virtual async Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken, bool saveNow = true)
	{
		Assert.NotNull(entities, nameof(entities));
		await Entities.BulkDeleteAsync(entities, cancellationToken);
	}

	/// <summary>
	/// حذف با شرط (بهینه شده)
	/// </summary>
	public virtual async Task<int> DeleteWhereAsync(
		Expression<Func<TEntity, bool>> predicate,
		CancellationToken cancellationToken = default)
	{
		return await Entities.Where(predicate).ExecuteDeleteAsync(cancellationToken);
	}

	#endregion

	#region Optimized Execute Query Methods

	public virtual async Task<int> ExecuteCommandAsync(
		string query,
		object? parameters = null,
		CancellationToken cancellationToken = default)
	{
		using var connection = new SqlConnection(ConnectionString);
		using var command = new SqlCommand(query, connection);

		AddParametersToCommand(command, parameters);

		try
		{
			await connection.OpenAsync(cancellationToken);
			return await command.ExecuteNonQueryAsync(cancellationToken);
		}
		catch (SqlException ex)
		{
			throw new Exception($"SQL Error: {ex.Message}", ex);
		}
	}

	public virtual async Task<IEnumerable<T>> ExecuteQueryAsync<T>(
		string query,
		object? parameters = null,
		CancellationToken cancellationToken = default) where T : class, new()
	{
		var results = new List<T>();

		using var connection = new SqlConnection(ConnectionString);
		using var command = new SqlCommand(query, connection);

		await connection.OpenAsync(cancellationToken);
		AddParametersToCommand(command, parameters);

		using var reader = await command.ExecuteReaderAsync(cancellationToken);
		while (await reader.ReadAsync(cancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			results.Add(MapReaderToEntity<T>(reader));
		}

		return results;
	}

	public virtual async Task<IEnumerable<Dictionary<string, object>>> ExecuteQueryAsync(
		string query,
		object? parameters = null,
		CancellationToken cancellationToken = default)
	{
		var results = new List<Dictionary<string, object>>();

		using var connection = new SqlConnection(ConnectionString);
		using var command = new SqlCommand(query, connection);

		await connection.OpenAsync(cancellationToken);
		AddParametersToCommand(command, parameters);

		using var reader = await command.ExecuteReaderAsync(cancellationToken);
		while (await reader.ReadAsync(cancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();

			var entity = new Dictionary<string, object>();
			for (var i = 0; i < reader.FieldCount; i++)
			{
				var propertyName = reader.GetName(i);
				entity.Add(propertyName, reader.IsDBNull(i) ? null! : reader.GetValue(i));
			}
			results.Add(entity);
		}

		return results;
	}

	public virtual async Task<T?> ExecuteQueryFirstOrDefaultAsync<T>(
		string query,
		object? parameters = null,
		CancellationToken cancellationToken = default) where T : class, new()
	{
		using var connection = new SqlConnection(ConnectionString);
		using var command = new SqlCommand(query, connection);

		await connection.OpenAsync(cancellationToken);
		AddParametersToCommand(command, parameters);

		using var reader = await command.ExecuteReaderAsync(cancellationToken);
		if (!reader.HasRows) return null;

		if (await reader.ReadAsync(cancellationToken))
		{
			return MapReaderToEntity<T>(reader);
		}

		return null;
	}

	public virtual async Task<object?> ExecuteScalarAsync(
		string query,
		object? parameters = null,
		CancellationToken cancellationToken = default)
	{
		using var connection = new SqlConnection(ConnectionString);
		using var command = new SqlCommand(query, connection);

		await connection.OpenAsync(cancellationToken);
		AddParametersToCommand(command, parameters);

		return await command.ExecuteScalarAsync(cancellationToken);
	}

	#endregion

	#region Sync Methods (Optimized)

	public virtual TEntity? GetById(params object[] ids)
	{
		return Entities.Find(ids);
	}

	public virtual TEntity Save(TEntity entity, bool saveAudit = false, bool saveNow = true)
	{
		var isNew = entity.Id == 0 || entity.Id == null || !TableNoTracking.Any(c => c.Id == entity.Id);

		return isNew
			? Add(entity, saveAudit, saveNow)
			: Update(entity, saveAudit, saveNow);
	}

	public virtual TEntity Add(TEntity entity, bool saveAudit = false, bool saveNow = true)
	{
		Assert.NotNull(entity, nameof(entity));

		SetEntityDates(entity, isNew: true);
		SetEntityUser(entity, isNew: true);
		entity.IsActive = IsActiveEnum.Active;

		Entities.Add(entity);

		if (saveNow)
		{
			DbContext.SaveChanges();

			if (saveAudit && _auditService != null)
			{
				_auditService.SaveAuditAsync(entity, null, AuditLogType.Add).GetAwaiter().GetResult();
			}
		}

		return entity;
	}

	public virtual List<TEntity> AddRange(List<TEntity> entities, bool saveNow = true)
	{
		Assert.NotNull(entities, nameof(entities));

		foreach (var entity in entities)
		{
			SetEntityDates(entity, isNew: true);
			SetEntityUser(entity, isNew: true);
			entity.IsActive = IsActiveEnum.Active;
		}

		 Entities.BulkInsert(entities);
		return entities;
	}

	public virtual TEntity Update(TEntity entity, bool saveAudit = false, bool saveNow = true)
	{
		Assert.NotNull(entity, nameof(entity));

		var oldEntity = TableNoTracking.FirstOrDefault(c => c.Id == entity.Id);
		if (oldEntity == null)
		{
			throw new InvalidOperationException($"Entity with Id {entity.Id} not found.");
		}

		SetEntityDates(entity, isNew: false);
		SetEntityUser(entity, isNew: false);
		PreserveCreationInfo(entity, oldEntity);

		Entities.Update(entity);

		if (saveNow)
		{
			DbContext.SaveChanges();

			if (saveAudit && _auditService != null)
			{
				_auditService.SaveAuditAsync(entity, oldEntity, AuditLogType.Update).GetAwaiter().GetResult();
			}
		}

		return entity;
	}


	public virtual List<TEntity> UpdateRange(List<TEntity> entities, bool saveNow = true)
	{
		Assert.NotNull(entities, nameof(entities));

		var entityIds = entities.Where(e => e.Id.HasValue).Select(e => e.Id!.Value).ToList();
		var oldEntities =   TableNoTracking
			.Where(e => e.Id.HasValue && entityIds.Contains(e.Id.Value))
			.ToList();

		var oldEntitiesDict = oldEntities.Where(e => e.Id.HasValue).ToDictionary(e => e.Id!.Value);

		foreach (var entity in entities)
		{
			if (entity.Id.HasValue && oldEntitiesDict.TryGetValue(entity.Id.Value, out var oldEntity))
			{
				SetEntityDates(entity, isNew: false);
				SetEntityUser(entity, isNew: false);
				PreserveCreationInfo(entity, oldEntity);
			}
		}

		 Entities.BulkUpdateAsync(entities);
		return entities;
	}

	public virtual void Delete(TEntity entity, bool saveNow = true)
	{
		Assert.NotNull(entity, nameof(entity));

		var entityForAudit = TableNoTracking.FirstOrDefault(c => c.Id == entity.Id);

		Entities.Remove(entity);

		if (saveNow)
		{
			DbContext.SaveChanges();

			if (_auditService != null && entityForAudit != null)
			{
				_auditService.SaveAuditAsync(entityForAudit, null, AuditLogType.Delete).GetAwaiter().GetResult();
			}
		}
	}

	public virtual int ExecuteCommand(string query, object? parameters = null)
	{
		using var connection = new SqlConnection(ConnectionString);
		using var command = new SqlCommand(query, connection);

		AddParametersToCommand(command, parameters);

		try
		{
			connection.Open();
			return command.ExecuteNonQuery();
		}
		catch (SqlException ex)
		{
			throw new Exception($"SQL Error: {ex.Message}", ex);
		}
	}

	#endregion

	#region DataTable & Excel Methods

	public virtual async Task<DataTableResponse> FetchDataAsync(DataTableRequest request, CancellationToken cn)
	{
		if (!request.TableName.HasValue(true))
		{
			request.TableName = typeof(TEntity).Name;
		}

		var resQuery = _dataTableQuery!.BuildSqlServerQuery(request);

		var itemsTask = ExecuteQueryAsync(resQuery.MainQuery, null, cn);
		var recordsFilteredTask = ExecuteQueryAsync(resQuery.CountFiltterdQuery, null, cn);
		var recordsTotalTask = ExecuteQueryAsync(resQuery.CountTotalQuery, null, cn);

		await Task.WhenAll(itemsTask, recordsFilteredTask, recordsTotalTask);

		var items = await itemsTask;
		var recordsFilteredResult = await recordsFilteredTask;
		var recordsTotalResult = await recordsTotalTask;

		var recordsFiltered = recordsFilteredResult.First()["TotalCount"];
		var recordsTotal = recordsTotalResult.First()["TotalCount"];

		return new DataTableResponse
		{
			Data = items,
			Draw = request.draw,
			RecordsFiltered = recordsFiltered,
			RecordsTotal = recordsTotal
		};
	}

	public virtual async Task ExportLargeDataToExcelAsync(DataTableRequest request, Stream outputStream, string licensePath)
	{
		var workbook = new Workbook();

		if (!workbook.IsLicensed)
			new License().SetLicense(licensePath);

		if (!request.TableName.HasValue(true))
		{
			request.TableName = typeof(TEntity).Name;
		}

		var resQuery = _dataTableQuery!.BuildSqlServerQuery(request);
		var query = resQuery.WithoutPagnationQuery;

		var worksheet = workbook.Worksheets[0];
		worksheet.Name = "LargeData";

		var firstChunk = ExecuteQuery(query);
		if (firstChunk == null || !firstChunk.Any())
		{
			return;
		}

		var headers = firstChunk.First().Keys.ToList();
		if (request.columns != null)
		{
			for (int i = 0; i < headers.Count && i < request.columns.Count; i++)
			{
				var header = request.columns[i]?.title ?? string.Empty;
				worksheet.Cells[0, i].PutValue(header);
			}
		}

		await AddChunkDataToWorksheet(worksheet, firstChunk, 1, request);

		worksheet.AutoFitColumns();

		if (!workbook.IsLicensed)
			new License().SetLicense(licensePath);

		workbook.Save(outputStream, SaveFormat.Xlsx);
	}

	private Task AddChunkDataToWorksheet(Worksheet worksheet, IEnumerable<Dictionary<string, object>> chunkData, int startRow, DataTableRequest request)
	{
		if (request.columns == null) return Task.CompletedTask;

		int row = startRow;
		foreach (var record in chunkData)
		{
			int col = 0;
			foreach (var value in record.Values)
			{
				if (col < request.columns.Count)
				{
					var colInfo = request.columns[col];
					var putValue = value;

					if (colInfo?.type == "select" && putValue != null)
					{
						putValue = colInfo.options?.FirstOrDefault(c => c.value == putValue?.ToString())?.name ?? putValue;
					}

					worksheet.Cells[row, col].PutValue(putValue?.ToString() ?? string.Empty);
				}
				col++;
			}
			row++;
		}

		return Task.CompletedTask;
	}

	public virtual async Task<List<List<Dictionary<string, object>>>> ExecuteQueryGetHeadersAsync(
		string query,
		object? parameters = null,
		CancellationToken cancellationToken = default)
	{
		var results = new List<List<Dictionary<string, object>>>();

		using var connection = new SqlConnection(ConnectionString);
		await connection.OpenAsync(cancellationToken);

		using var command = new SqlCommand(query, connection);
		command.CommandType = CommandType.StoredProcedure;

		AddParametersToCommand(command, parameters);

		using var reader = await command.ExecuteReaderAsync(cancellationToken);
		do
		{
			var resultSet = new List<Dictionary<string, object>>();

			foreach (var dbColumn in reader.GetColumnSchema())
			{
				var entity = new Dictionary<string, object>
			 {
				{ "ColumnName", dbColumn.ColumnName ?? string.Empty },
				{ "DataType", dbColumn.DataTypeName ?? string.Empty }
			 };
				resultSet.Add(entity);
			}

			results.Add(resultSet);
		}
		while (await reader.NextResultAsync(cancellationToken));

		return results;
	}

	public virtual IEnumerable<Dictionary<string, object>> ExecuteQuery(string query, object? parameters = null)
	{
		var results = new List<Dictionary<string, object>>();

		using var connection = new SqlConnection(ConnectionString);
		using var command = new SqlCommand(query, connection);

		connection.Open();
		AddParametersToCommand(command, parameters);

		using var reader = command.ExecuteReader();
		while (reader.Read())
		{
			var entity = new Dictionary<string, object>();
			for (var i = 0; i < reader.FieldCount; i++)
			{
				var propertyName = reader.GetName(i);
				entity.Add(propertyName, reader.IsDBNull(i) ? null! : reader.GetValue(i));
			}
			results.Add(entity);
		}

		return results;
	}

	#endregion

	#region Attach & Detach

	public virtual void Detach(TEntity entity)
	{
		Assert.NotNull(entity, nameof(entity));
		var entry = DbContext.Entry(entity);
		if (entry != null)
			entry.State = EntityState.Detached;
	}

	public virtual void Attach(TEntity entity)
	{
		Assert.NotNull(entity, nameof(entity));
		if (DbContext.Entry(entity).State == EntityState.Detached)
			Entities.Attach(entity);
	}

	#endregion

	#region Explicit Loading

	public virtual async Task LoadCollectionAsync<TProperty>(
		TEntity entity,
		Expression<Func<TEntity, IEnumerable<TProperty>>> collectionProperty,
		CancellationToken cancellationToken) where TProperty : class
	{
		Attach(entity);

		var collection = DbContext.Entry(entity).Collection(collectionProperty);
		if (!collection.IsLoaded)
			await collection.LoadAsync(cancellationToken);
	}

	public virtual void LoadCollection<TProperty>(
		TEntity entity,
		Expression<Func<TEntity, IEnumerable<TProperty>>> collectionProperty) where TProperty : class
	{
		Attach(entity);
		var collection = DbContext.Entry(entity).Collection(collectionProperty);
		if (!collection.IsLoaded)
			collection.Load();
	}

	public virtual async Task LoadReferenceAsync<TProperty>(
		TEntity entity,
		Expression<Func<TEntity, TProperty>> referenceProperty,
		CancellationToken cancellationToken) where TProperty : class
	{
		Attach(entity);
		var reference = DbContext.Entry(entity).Reference(referenceProperty);
		if (!reference.IsLoaded)
			await reference.LoadAsync(cancellationToken);
	}

	public virtual void LoadReference<TProperty>(
		TEntity entity,
		Expression<Func<TEntity, TProperty>> referenceProperty) where TProperty : class
	{
		Attach(entity);
		var reference = DbContext.Entry(entity).Reference(referenceProperty);
		if (!reference.IsLoaded)
			reference.Load();
	}

	#endregion
}