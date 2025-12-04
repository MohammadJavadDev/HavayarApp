using Entities.Base.DataTable;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Data.Contracts;

public interface IRepository<TEntity> where TEntity : class
{
    DbSet<TEntity> Entities { get; }
    IQueryable<TEntity> Table { get; }
    IQueryable<TEntity> TableNoTracking { get; }

    Task<IEnumerable<Dictionary<string, object>>> ExecuteQueryAsync(string query, object? parameters = null,
        CancellationToken cancellationToken = default);

    Task<List<List<Dictionary<string, object>>>> ExecuteQueryGetHeadersAsync(string query, object? parameters = null,
        CancellationToken cancellationToken = default);


    Task<TEntity> SaveAsync(TEntity entity, CancellationToken cancellationToken, bool saveAudit = true, bool saveNow = true);
    TEntity Save(TEntity entity, bool saveAudit = true, bool saveNow = true);
    TEntity Add(TEntity entity, bool saveAudit = true, bool saveNow = true);
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken,bool saveAudit = true, bool saveNow = true);
    List<TEntity> AddRange(List<TEntity> entities, bool saveNow = true);
    Task<List<TEntity>> AddRangeAsync(List<TEntity> entities, CancellationToken cancellationToken, bool saveNow = true);
    void Attach(TEntity entity);
    void Delete(TEntity entity, bool saveNow = true);
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken, bool saveNow = true);
    void DeleteRange(IEnumerable<TEntity> entities, bool saveNow = true);
    Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken, bool saveNow = true);
    void Detach(TEntity entity);
    TEntity GetById(params object[] ids);
    Task<TEntity> GetByIdAsync(CancellationToken cancellationToken, params object[] ids);
    Task<DataTableResponse> FetchDataAsync(DataTableRequest request ,CancellationToken cancellationToken);
    public Task ExportLargeDataToExcelAsync(DataTableRequest request, Stream outputStream, string licensePath);
    void LoadCollection<TProperty>(TEntity entity, Expression<Func<TEntity, IEnumerable<TProperty>>> collectionProperty) where TProperty : class;
    Task LoadCollectionAsync<TProperty>(TEntity entity, Expression<Func<TEntity, IEnumerable<TProperty>>> collectionProperty, CancellationToken cancellationToken) where TProperty : class;
    void LoadReference<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> referenceProperty) where TProperty : class;
    Task LoadReferenceAsync<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> referenceProperty, CancellationToken cancellationToken) where TProperty : class;
    TEntity Update(TEntity entity, bool saveAudit = true, bool saveNow = true);
    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken, bool saveAudit = true, bool saveNow = true);
    List<TEntity> UpdateRange(List<TEntity> entities, bool saveNow = true);
    Task<List<TEntity>> UpdateRangeAsync(List<TEntity> entities, CancellationToken cancellationToken, bool saveNow = true);
 
}