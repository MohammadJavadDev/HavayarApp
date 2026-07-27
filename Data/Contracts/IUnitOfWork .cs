using Entities.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Contracts
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity, new();
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
        void BeginTransaction();
        Task BeginTransactionAsync(CancellationToken cancellationToken);
        Task CommitTransactionAsync(CancellationToken cancellationToken);
        void RollbackTransaction();
        Task RollbackTransactionAsync(CancellationToken cancellationToken);

		/// <summary>
		/// اجرای یک عملیات در تراکنش با پشتیبانی از استراتژی Retry
		/// </summary>
		Task<TResult> ExecuteInTransactionAsync<TResult>(
		    Func<Task<TResult>> action,
		    CancellationToken cancellationToken = default);
	}
}
