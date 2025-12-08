using Entities.Base;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Z.BulkOperations;

namespace Data.Services;

public interface IAuditService
{
 
 	Task SaveAuditAsync<TEntity>(TEntity entity, TEntity? oldEntity, AuditLogType type, CancellationToken cancellationToken = default)
	    where TEntity : BaseEntity;
}
