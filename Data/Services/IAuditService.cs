using Entities.Base;
using System.Threading;
using System.Threading.Tasks;

namespace Data.Services;

public interface IAuditService
{
    Task SaveAuditAsync<TEntity>(TEntity entity, TEntity? oldEntity, AuditLogType type, CancellationToken cancellationToken = default) 
        where TEntity : BaseEntity;
}

