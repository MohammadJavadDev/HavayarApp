using Data.Contracts.Actions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

	public class EntityActionInvoker(IServiceProvider serviceProvider, IEntityActionRegistry registry, ILogger<EntityActionInvoker> logger) : IEntityActionInvoker
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
				object?[] args = new object?[parameters.Length];

				// اولین پارامتر هم‌نوع با موجودیت = entity ؛ پارامتر بعدی هم‌نوع = oldEntity (فقط در ویرایش مقدار دارد).
				// قبلا هر دو پارامتر هم‌نوع مقدار entity می‌گرفتند و oldEntity هرگز bind نمی‌شد.
				var entityBound = false;
				var oldEntityBound = false;

				for (int i = 0; i < parameters.Length; i++)
				{
					var pType = parameters[i].ParameterType;
					if (!entityBound && pType.IsAssignableFrom(entity.GetType()))
					{
						args[i] = entity;
						entityBound = true;
					}
					else if (entityBound && !oldEntityBound && pType.IsAssignableFrom(entity.GetType()))
					{
						args[i] = oldEntity;
						oldEntityBound = true;
					}
					else if (pType == typeof(CancellationToken)) args[i] = ct;
				}

				var result = definition.Method.Invoke(instance, args);

				if (result is Task task) await task;
				else if (result is ValueTask valueTask) await valueTask;
			}
			catch (TargetInvocationException ex)
			{
				// خطای اصلی لاگ می‌شود؛ پیام کاربر مثل قبل است. (InnerException عمدا ست نمی‌شود چون
				// CustomExceptionHandlerMiddleware و چند کنترلر InnerException?.Message را نمایش می‌دهند
				// و پیشوند «خطا در اکشن» حذف می‌شد.)
				logger.LogWarning(ex.InnerException ?? ex, "Entity action {ActionName} ({Trigger}) on {EntityType} failed", definition.Metadata.Name, definition.Metadata.Trigger, definition.Metadata.EntityType.Name);
				// استخراج خطای اصلی از داخل متد invoke شده
				throw new Exception($"خطا در اکشن [{definition.Metadata.Title}]: {ex.InnerException?.Message ?? ex.Message}");
			}
			catch (Exception ex)
			{
				// خطای داخل متدهای async (و خطای resolve شدن کلاس Action) با همان نوع و پیام بالا می‌رود؛
				// فقط نام Action لاگ می‌شود تا عیب‌یابی ممکن باشد.
				logger.LogWarning(ex, "Entity action {ActionName} ({Trigger}) on {EntityType} failed", definition.Metadata.Name, definition.Metadata.Trigger, definition.Metadata.EntityType.Name);
				throw;
			}
		}
	}
}
