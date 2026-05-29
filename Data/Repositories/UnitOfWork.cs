 
using Common.System;
using Data.Contracts;
using Data.Services;
using Data.SystemAuth;
using Entities.Base;
using Microsoft.EntityFrameworkCore.Storage;

namespace Data.Repositories
{
    public class UnitOfWork(ApplicationDbContext context,  ISdk? sdk  , IAuditService _auditService) : IUnitOfWork
    {
        private readonly IDictionary<Type, object> _repositories = new Dictionary<Type, object>();
        private IDbContextTransaction _transaction;

        public IRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity, new()
        {
            if (_repositories.TryGetValue(typeof(TEntity), out var repository))
            {
                return (IRepository<TEntity>)repository;
            }

            var newRepository = new Repository<TEntity>(context , sdk, _auditService);
            _repositories[typeof(TEntity)] = newRepository;
            return newRepository;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await context.SaveChangesAsync(cancellationToken);
        }

        public void BeginTransaction()
        {
            _transaction = context.Database.BeginTransaction();
        }
		public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
		{
			_transaction = await context.Database.BeginTransactionAsync(cancellationToken);
		}

		public async Task CommitTransactionAsync(CancellationToken cancellationToken =default)
        {
            try
            {
                await SaveChangesAsync(cancellationToken);
                await _transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                RollbackTransaction();
                throw;
            }
            finally
            {
                DisposeTransaction();
            }
        }

        public void RollbackTransaction()
        {
            _transaction?.Rollback();
            DisposeTransaction();
        }

		public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
		{
		await	_transaction?.RollbackAsync(cancellationToken);
			DisposeTransaction();
		}

		private void DisposeTransaction()
        {
            if (_transaction != null)
            {
                _transaction.Dispose();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            context.Dispose();
        }
    }

}
