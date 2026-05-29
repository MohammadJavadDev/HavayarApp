using Entities.Base;

namespace Data.Contracts.Actions
{
	public interface IEntityAction<TEntity> where TEntity : BaseEntity
	{
		Task BeforeAddAsync(TEntity entity, CancellationToken ct) => Task.CompletedTask;
		Task AfterAddAsync(TEntity entity, CancellationToken ct) => Task.CompletedTask;

		Task BeforeUpdateAsync(TEntity entity, TEntity oldEntity, CancellationToken ct) => Task.CompletedTask;
		Task AfterUpdateAsync(TEntity entity, TEntity oldEntity, CancellationToken ct) => Task.CompletedTask;

		Task BeforeDeleteAsync(TEntity entity, CancellationToken ct) => Task.CompletedTask;
		Task AfterDeleteAsync(TEntity entity, CancellationToken ct) => Task.CompletedTask;

		// این متد برای SaveAsync (قبل از تشخیص وضعیت Add/Update) کاربرد دارد
		Task BeforeSaveAsync(TEntity entity, CancellationToken ct) => Task.CompletedTask;
	}
}