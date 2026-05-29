using Data.Contracts.Actions;
using Entities.Base;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Data.Services.Actions
{
	public interface IEntityActionProcessor
	{
		Task ExecuteAsync<TEntity>(TEntity entity, TEntity? oldEntity, ActionType type, ExecutionTime time, CancellationToken ct) where TEntity : BaseEntity;
	}

	public enum ActionType { Add, Update, Delete, Save }
	public enum ExecutionTime { Before, After }

	public class EntityActionProcessor(IServiceProvider serviceProvider) : IEntityActionProcessor
	{
		public async Task ExecuteAsync<TEntity>(TEntity entity, TEntity? oldEntity, ActionType type, ExecutionTime time, CancellationToken ct) where TEntity : BaseEntity
		{
			// دریافت تمام سرویس‌های ثبت شده برای این موجودیت
			var actions = serviceProvider.GetServices<IEntityAction<TEntity>>();

			// فیلتر کردن بر اساس Attribute و اولویت‌بندی
			var orderedActions = actions
			    .Select(a => new { Action = a, Meta = a.GetType().GetCustomAttribute<EntityActionAttribute>() })
			    .Where(x => x.Meta != null && x.Meta.IsActive && x.Meta.EntityType == typeof(TEntity))
			    .OrderBy(x => x.Meta!.Priority)
			    .ToList();

			foreach (var item in orderedActions)
			{
				try
				{
					await InvokeAction(item.Action, entity, oldEntity, type, time, ct);
				}
				catch (Exception ex)
				{
					// تولید خطای غنی شده با نام اکشن برای عیب‌یابی سریع
					throw new Exception($"خطا در اجرای اکشن '{item.Meta!.Title}' ({item.Meta.Name}): {ex.Message}", ex);
				}
			}
		}

		private async Task InvokeAction<TEntity>(IEntityAction<TEntity> action, TEntity entity, TEntity? oldEntity, ActionType type, ExecutionTime time, CancellationToken ct) where TEntity : BaseEntity
		{
			if (time == ExecutionTime.Before)
			{
				switch (type)
				{
					case ActionType.Add: await action.BeforeAddAsync(entity, ct); break;
					case ActionType.Update: await action.BeforeUpdateAsync(entity, oldEntity!, ct); break;
					case ActionType.Delete: await action.BeforeDeleteAsync(entity, ct); break;
					case ActionType.Save: await action.BeforeSaveAsync(entity, ct); break;
				}
			}
			else
			{
				switch (type)
				{
					case ActionType.Add: await action.AfterAddAsync(entity, ct); break;
					case ActionType.Update: await action.AfterUpdateAsync(entity, oldEntity!, ct); break;
					case ActionType.Delete: await action.AfterDeleteAsync(entity, ct); break;
				}
			}
		}
	}
}