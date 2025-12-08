
using Aspose.Cells;
using Common;
using Common.Utilities;
using Data.Contracts;
using Data.Services;
using Data.SystemAuth;
using Entities.Base;
using Entities.Base.DataTable;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq.Expressions;
 

using License = Aspose.Cells.License;

namespace Data.Repositories;

public class Repository<TEntity> : IRepository<TEntity>, IScopedDependency
    where TEntity : BaseEntity, new()
{
    protected readonly ApplicationDbContext DbContext;
    private readonly ISdk? sdk;
    private readonly IDataTableQueryBuilder? _dataTableQuery;
    private readonly IAuditService _auditService;

    public DbSet<TEntity> Entities { get; }
    public virtual IQueryable<TEntity> Table => Entities;
    public virtual IQueryable<TEntity> TableNoTracking => Entities.AsNoTracking();

    public string? ConnectionString { get; set; }



    public Repository(ApplicationDbContext dbContext, ISdk? sdk, IDataTableQueryBuilder dataTableQuery, IAuditService auditService )
    {
        DbContext = dbContext;
        Entities = DbContext.Set<TEntity>();
        this.sdk = sdk;
        _dataTableQuery = dataTableQuery;
        _auditService = auditService;
        ConnectionString = dbContext.Database.GetDbConnection().ConnectionString;

        string licenseName = "134;100-DOWNLOADDEVTOOLS.COM";
        string licenseKey = "1519351-28861E0-148651C-25E14B3-9428";

        Z.EntityFramework.Extensions.LicenseManager.AddLicense(licenseName, licenseKey);

        string licenseErrorMessage;
        if (!Z.EntityFramework.Extensions.LicenseManager.ValidateLicense(out licenseErrorMessage))
        {
            throw new Exception(licenseErrorMessage);
        }
    }

    #region Async Method
    public virtual async Task<TEntity> GetByIdAsync(CancellationToken cancellationToken, params object[] ids)
    {
        var entity = await Entities.FindAsync(ids, cancellationToken);
        return entity!;
    }

    #region Helper Methods
    private void SetEntityDates(TEntity entity, bool isNew)
    {
        entity.ModifiedDateMiladiDateTime = DateTime.Now;
        entity.ModifiedDateShamsiDateTime = DateTime.Now.ToShamsiDateTime();

        if (isNew)
        {
            entity.CreatedOnMiladiDateTime = DateTime.Now;
            entity.CreatedOnShamsiDateTime = DateTime.Now.ToShamsiDateTime();
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
    }
    #endregion



    public int ExecuteCommand(string query, object? parameters = null)
    {
        int affectedRows = 0;
        using (var connection = new SqlConnection(ConnectionString))
        {
            using (var command = new SqlCommand(query, connection))
            {

                if (parameters != null)
                {
                    foreach (var prop in parameters.GetType().GetProperties())
                    {
                        command.Parameters.AddWithValue($"@{prop.Name}", prop.GetValue(parameters));
                    }
                }

                try
                {
                    connection.Open();
                    affectedRows = command.ExecuteNonQuery();

                }
                catch (SqlException ex)
                {
                    throw new Exception(ex.Message);
                }
                catch (Exception ex)
                {
                    var e = ex.Message;
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        return affectedRows;
    }

    public int ExecuteCommand(string query, Dictionary<string, string>? parameters = null)
    {
        int affectedRows = 0;
        using (var connection = new SqlConnection(ConnectionString))
        {
            using (var command = new SqlCommand(query, connection))
            {

                if (parameters != null)
                {
                    foreach (var prop in parameters)
                    {
                        command.Parameters.AddWithValue($"@{prop.Key}", prop.Value);
                    }
                }

                try
                {
                    connection.Open();
                    affectedRows = command.ExecuteNonQuery();

                }
                catch (SqlException ex)
                {
                    throw new Exception(ex.Message);
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
                finally
                {
                    connection.Close();
                }


            }
        }

        return affectedRows;
    }

    public IEnumerable<TEntity> ExecuteQuery<TEntity>
        (string query, object? parameters = null) where TEntity : class, new()
    {

        var results = new List<TEntity>();
        using (var connection = new SqlConnection(ConnectionString))
        {
            using (var command = new SqlCommand(query, connection))
            {
                connection.Open();
                if (parameters != null)
                {
                    foreach (var prop in parameters.GetType().GetProperties())
                    {
                        command.Parameters.AddWithValue($"@{prop.Name}", prop.GetValue(parameters));
                    }
                }

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var entity = new TEntity();

                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            var propertyName = reader.GetName(i);
                            var property = entity.GetType().GetProperty(propertyName);

                            if (property != null && !reader.IsDBNull(i))
                            {
                                var value = reader.GetValue(i);
                                var propertyType = property.PropertyType;

                                if (propertyType.IsGenericType &&
                                    propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                                {
                                    propertyType = Nullable.GetUnderlyingType(propertyType);
                                }

                                if (propertyType is { IsEnum: true })
                                {
                                    var enumValue = (Enum)Enum.ToObject(propertyType, value);
                                    property.SetValue(entity, enumValue);
                                }
                                else
                                {
                                    property.SetValue(entity, value);
                                }
                            }
                        }

                        results.Add(entity);
                    }
                }
            }
        }

        return results;
    }

    public IEnumerable<Dictionary<string, object>> ExecuteQuery(string query, object? parameters = null)
    {

        var results = new List<Dictionary<string, object>>();
        using (var connection = new SqlConnection(ConnectionString))
        {
            using (var command = new SqlCommand(query, connection))
            {
                connection.Open();
                if (parameters != null)
                {
                    foreach (var prop in parameters.GetType().GetProperties())
                    {
                        command.Parameters.AddWithValue($"@{prop.Name}", prop.GetValue(parameters));
                    }
                }

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var entity = new Dictionary<string, object>();

                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            var propertyName = reader.GetName(i);


                            if (!reader.IsDBNull(i))
                            {
                                var value = reader.GetValue(i);
                                entity.Add(propertyName, value);
                            }
                            else
                            {
                                entity.Add(propertyName, null);
                            }
                        }

                        results.Add(entity);
                    }
                }
            }
        }

        return results;
    }

    public async Task<IEnumerable<TEntity>> ExecuteQueryAsync<TEntity>(string query, object? parameters = null
        , CancellationToken cancellationToken = default)
        where TEntity : class, new()
    {
        var results = new List<TEntity>();

        using (var connection = new SqlConnection(ConnectionString))
        {
            await connection.OpenAsync(cancellationToken);

            using (var command = new SqlCommand(query, connection))
            {
                if (parameters != null)
                {
                    foreach (var prop in parameters.GetType().GetProperties())
                    {
                        command.Parameters.AddWithValue($"@{prop.Name}", prop.GetValue(parameters));
                    }
                }

                using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {

                        cancellationToken.ThrowIfCancellationRequested();

                        var entity = new TEntity();

                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            var propertyName = reader.GetName(i);
                            var property = entity.GetType().GetProperty(propertyName);

                            if (property != null && !reader.IsDBNull(i))
                            {
                                var value = reader.GetValue(i);
                                var propertyType = property.PropertyType;

                                if (propertyType.IsGenericType &&
                                    propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                                {
                                    propertyType = Nullable.GetUnderlyingType(propertyType);
                                }

                                if (propertyType is { IsEnum: true })
                                {
                                    var enumValue = (Enum)Enum.ToObject(propertyType, value);
                                    property.SetValue(entity, enumValue);
                                }
                                else
                                {
                                    property.SetValue(entity, value);
                                }
                            }
                        }

                        results.Add(entity);
                    }
                }
            }
        }

        return results;
    }

    public async Task<List<List<Dictionary<string, object>>>> ExecuteQueryGetHeadersAsync(string query, object? parameters = null, CancellationToken cancellationToken = default)
    {
        var results = new List<List<Dictionary<string, object>>>();

        using (var connection = new SqlConnection(ConnectionString))
        {
            await connection.OpenAsync(cancellationToken);

            using (var command = new SqlCommand(query, connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                if (parameters != null)
                {
                    if (parameters is Dictionary<string, object> outDictionary)
                    {
                        foreach (var prop in outDictionary)
                        {
                            command.Parameters.AddWithValue($"@{prop.Key}", prop.Value);
                        }
                    }
                    else
                    {
                        foreach (var prop in parameters.GetType().GetProperties())
                        {
                            command.Parameters.AddWithValue($"@{prop.Name}", prop.GetValue(parameters));
                        }
                    }
                }

                using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    do
                    {
                        var resultSet = new List<Dictionary<string, object>>();

                        foreach (var dbColumn in reader.GetColumnSchema().ToList())
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
                }
            }
        }

        return results;
    }
    public async Task<IEnumerable<Dictionary<string, object>>> ExecuteQueryAsync(string query, object? parameters = null, CancellationToken cancellationToken = default)
    {
        var results = new List<Dictionary<string, object>>();

        using (var connection = new SqlConnection(ConnectionString))
        {
            await connection.OpenAsync(cancellationToken);

            using (var command = new SqlCommand(query, connection))
            {
                if (parameters != null)
                {
                    foreach (var prop in parameters.GetType().GetProperties())
                    {
                        command.Parameters.AddWithValue($"@{prop.Name}", prop.GetValue(parameters));
                    }
                }

                using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {

                        cancellationToken.ThrowIfCancellationRequested();

                        var entity = new Dictionary<string, object>();

                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            var propertyName = reader.GetName(i);


                            if (!reader.IsDBNull(i))
                            {
                                var value = reader.GetValue(i);
                                entity.Add(propertyName, value);
                            }
                        }

                        results.Add(entity);
                    }
                }
            }
        }

        return results;
    }

    public async Task<IEnumerable<Dictionary<string, object>>> ExecuteQueryAsync(string query, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default)
    {
        var results = new List<Dictionary<string, object>>();



        using (var connection = new SqlConnection(ConnectionString))
        {
            await connection.OpenAsync(cancellationToken);

            using (var command = new SqlCommand(query, connection))
            {
                if (parameters != null)
                {
                    foreach (var prop in parameters)
                    {
                        command.Parameters.AddWithValue($"@{prop.Key}", prop.Value);
                    }
                }

                using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {

                        cancellationToken.ThrowIfCancellationRequested();

                        var entity = new Dictionary<string, object>();

                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            var propertyName = reader.GetName(i);


                            if (!reader.IsDBNull(i))
                            {
                                var value = reader.GetValue(i);
                                entity.Add(propertyName, value);
                            }
                        }

                        results.Add(entity);
                    }
                }
            }
        }

        return results;
    }


    public async Task<TEntity?> ExecuteQueryFirstOrDefaultAsync<TEntity>(string query, object? parameters = null
        , CancellationToken cancellationToken = default)
        where TEntity : class, new()
    {
        var entity = new TEntity();

        using (var connection = new SqlConnection(ConnectionString))
        {
            await connection.OpenAsync(cancellationToken);

            using (var command = new SqlCommand(query, connection))
            {
                if (parameters != null)
                {
                    foreach (var prop in parameters.GetType().GetProperties())
                    {
                        command.Parameters.AddWithValue($"@{prop.Name}", prop.GetValue(parameters));
                    }
                }

                using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    if (!reader.HasRows)
                        return null;
                    while (await reader.ReadAsync(cancellationToken))
                    {

                        cancellationToken.ThrowIfCancellationRequested();

                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            var propertyName = reader.GetName(i);
                            var property = entity.GetType().GetProperty(propertyName);

                            if (property != null && !reader.IsDBNull(i))
                            {
                                var value = reader.GetValue(i);
                                var propertyType = property.PropertyType;

                                if (propertyType.IsGenericType &&
                                    propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                                {
                                    propertyType = Nullable.GetUnderlyingType(propertyType);
                                }

                                if (propertyType is { IsEnum: true })
                                {
                                    var enumValue = (Enum)Enum.ToObject(propertyType, value);
                                    property.SetValue(entity, enumValue);
                                }
                                else
                                {
                                    property.SetValue(entity, value);
                                }
                            }
                        }


                    }
                }
            }
        }

        return entity;
    }




    public async Task<DataTableResponse> FetchDataAsync(DataTableRequest request, CancellationToken cn)
    {
        if (!request.TableName.HasValue(true))
        {
            request.TableName = typeof(TEntity).Name;
        }
        var resQuery = _dataTableQuery!.BuildSqlServerQuery(request);

        var items = await ExecuteQueryAsync(resQuery.MainQuery, (object?)null, cn);
        var recordsFilteredResult = await ExecuteQueryAsync(resQuery.CountFiltterdQuery, (object?)null, cn);
        var recordsTotalResult = await ExecuteQueryAsync(resQuery.CountTotalQuery, (object?)null, cn);

        var recordsFiltered = recordsFilteredResult.First()["TotalCount"];
        var recordsTotal = recordsTotalResult.First()["TotalCount"];

        var res = new DataTableResponse()
        {
            Data = items,
            Draw = request.draw,
            RecordsFiltered = recordsFiltered,
            RecordsTotal = recordsTotal
        };

        return res;
    }

    public async Task ExportLargeDataToExcelAsync(DataTableRequest request, Stream outputStream, string licensePath)
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

        int row = 1;

        await AddChunkDataToWorksheet(worksheet, firstChunk, row, request);
        row += firstChunk.Count();

        worksheet.AutoFitColumns();

        if (!workbook.IsLicensed)
            new License().SetLicense(licensePath);
        workbook.Save(outputStream, SaveFormat.Xlsx);
        await Task.CompletedTask;
    }


    private Task AddChunkDataToWorksheet(Worksheet worksheet, IEnumerable<Dictionary<string, object>> chunkData,
        int startRow, DataTableRequest request)
    {
        int row = startRow;
        if (request.columns == null) return Task.CompletedTask;

        foreach (var record in chunkData)
        {
            int col = 0;
            foreach (var value in record.Values)
            {
                if (col < request.columns.Count)
                {
                    var colInfo = request.columns[col];
                    var putValue = value;
                    if (colInfo?.type == "select")
                    {
                        if (putValue is not null)
                        {
                            putValue = colInfo.options?.FirstOrDefault(c => c.value == putValue?.ToString())?.name ?? putValue;
                        }
                    }
                    worksheet.Cells[row, col].PutValue(putValue?.ToString() ?? string.Empty);
                }
                col++;
            }
            row++;
        }

        return Task.CompletedTask;
    }

    public virtual async Task<TEntity> SaveAsync(TEntity entity, CancellationToken cancellationToken, bool saveAudit = false, bool saveNow = true)
    {
        var count = await TableNoTracking.CountAsync(c => c.Id == entity.Id);

        if (entity.Id == 0 || entity.Id == null || count == 0)
        {
            return await AddAsync(entity, cancellationToken, saveAudit, saveNow);

        }

        return await UpdateAsync(entity, cancellationToken, saveAudit, saveNow);

    }

    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken, bool saveAudit = false, bool saveNow = true)
    {
        Assert.NotNull(entity, nameof(entity));

        SetEntityDates(entity, isNew: true);
        SetEntityUser(entity, isNew: true);
        entity.IsActive = IsActiveEnum.Active;

        await Entities.AddAsync(entity, cancellationToken).ConfigureAwait(false);

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

        await Entities.BulkInsertAsync(entities, cancellationToken).ConfigureAwait(false);

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
        var oldEntities = await TableNoTracking.Where(e => e.Id.HasValue && entityIds.Contains(e.Id.Value)).ToListAsync(cancellationToken);
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

        // Create a copy of entity for audit before removing
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



    #endregion

    #region Sync Methods
    public virtual TEntity GetById(params object[] ids)
    {
        return Entities.Find(ids);
    }


    public virtual TEntity Save(TEntity entity, bool saveAudit = true, bool saveNow = true)
    {
        var count = TableNoTracking.Count(c => c.Id == entity.Id);

        if (entity.Id == 0 || entity.Id == null || count == 0)
        {
            return Add(entity, saveNow);

        }

        return Update(entity, saveNow);

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
                _auditService.SaveAuditAsync(entity, null, AuditLogType.Add).Wait();
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
                _auditService.SaveAuditAsync(entity, oldEntity, AuditLogType.Update).Wait();
            }
        }

        return entity;
    }

    public virtual List<TEntity> UpdateRange(List<TEntity> entities, bool saveNow = true)
    {
        Assert.NotNull(entities, nameof(entities));

        var entityIds = entities.Where(e => e.Id.HasValue).Select(e => e.Id!.Value).ToList();
        var oldEntities = TableNoTracking.Where(e => e.Id.HasValue && entityIds.Contains(e.Id.Value)).ToList();
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

        Entities.BulkUpdate(entities);

        return entities;
    }

    public virtual void Delete(TEntity entity, bool saveNow = true)
    {
        Assert.NotNull(entity, nameof(entity));

        // Create a copy of entity for audit before removing
        var entityForAudit = TableNoTracking.FirstOrDefault(c => c.Id == entity.Id);

        Entities.Remove(entity);

        if (saveNow)
        {
            DbContext.SaveChanges();

            if (_auditService != null && entityForAudit != null)
            {
                _auditService.SaveAuditAsync(entityForAudit, null, AuditLogType.Delete).Wait();
            }
        }
    }

    public virtual void DeleteRange(IEnumerable<TEntity> entities, bool saveNow = true)
    {
        Assert.NotNull(entities, nameof(entities));
        Entities.BulkDelete(entities);

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
    public virtual async Task LoadCollectionAsync<TProperty>(TEntity entity, Expression<Func<TEntity, IEnumerable<TProperty>>> collectionProperty, CancellationToken cancellationToken)
        where TProperty : class
    {
        Attach(entity);

        var collection = DbContext.Entry(entity).Collection(collectionProperty);
        if (!collection.IsLoaded)
            await collection.LoadAsync(cancellationToken).ConfigureAwait(false);
    }

    public virtual void LoadCollection<TProperty>(TEntity entity, Expression<Func<TEntity, IEnumerable<TProperty>>> collectionProperty)
        where TProperty : class
    {
        Attach(entity);
        var collection = DbContext.Entry(entity).Collection(collectionProperty);
        if (!collection.IsLoaded)
            collection.Load();
    }

    public virtual async Task LoadReferenceAsync<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> referenceProperty, CancellationToken cancellationToken)
        where TProperty : class
    {
        Attach(entity);
        var reference = DbContext.Entry(entity).Reference(referenceProperty);
        if (!reference.IsLoaded)
            await reference.LoadAsync(cancellationToken).ConfigureAwait(false);
    }

    public virtual void LoadReference<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> referenceProperty)
        where TProperty : class
    {
        Attach(entity);
        var reference = DbContext.Entry(entity).Reference(referenceProperty);
        if (!reference.IsLoaded)
            reference.Load();
    }


    #endregion
}