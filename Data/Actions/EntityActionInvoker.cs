using Data.Contracts.Actions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Data.Actions
{
	public interface IEntityActionInvoker
	{
		Task InvokeAsync<TEntity>(TEntity entity, TEntity? oldEntity, EntityActionTrigger trigger, CancellationToken ct) where TEntity : class;
		void InvokeSync<TEntity>(TEntity entity, TEntity? oldEntity, EntityActionTrigger trigger) where TEntity : class;
	}

	public class EntityActionInvoker(IServiceProvider serviceProvider, IEntityActionRegistry registry) : IEntityActionInvoker
	{
		public async Task InvokeAsync<TEntity>(TEntity entity, TEntity? oldEntity, EntityActionTrigger trigger, CancellationToken ct) where TEntity : class
		{
			var actions = registry.GetActions(typeof(TEntity), trigger);
			foreach (var action in actions)
			{
				await ExecuteMethod(action, entity, oldEntity, ct);
			}
		}

		public void InvokeSync<TEntity>(TEntity entity, TEntity? oldEntity, EntityActionTrigger trigger) where TEntity : class
		{
			var actions = registry.GetActions(typeof(TEntity), trigger);
			foreach (var action in actions)
			{
				// اجرای Sync با مدیریت Task ها برای جلوگیری از Deadlock
				var task = ExecuteMethod(action, entity, oldEntity, CancellationToken.None);
				task.GetAwaiter().GetResult();
			}
		}

		private async Task ExecuteMethod(EntityActionDefinition definition, object entity, object? oldEntity, CancellationToken ct)
		{
			try
			{
				var instance = serviceProvider.GetRequiredService(definition.DeclaringType);
				var parameters = definition.Method.GetParameters();
				object[] args = new object[parameters.Length];

				for (int i = 0; i < parameters.Length; i++)
				{
					var pType = parameters[i].ParameterType;
					if (pType.IsAssignableFrom(entity.GetType())) args[i] = entity;
					else if (oldEntity != null && pType.IsAssignableFrom(oldEntity.GetType())) args[i] = oldEntity;
					else if (pType == typeof(CancellationToken)) args[i] = ct;
				}

				var result = definition.Method.Invoke(instance, args);

				if (result is Task task) await task;
				else if (result is ValueTask valueTask) await valueTask;
			}
			catch (TargetInvocationException ex)
			{
				// استخراج خطای اصلی از داخل متد invoke شده
				throw new Exception($"خطا در اکشن [{definition.Metadata.Title}]: {ex.InnerException?.Message ?? ex.Message}");
			}
		}
	}
}
