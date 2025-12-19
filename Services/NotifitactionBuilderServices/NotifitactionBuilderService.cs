using Common.Entities.EntityMetadatas;
using Common.System;
using Common.Utilities;
using Data;
using Data.Contracts;
using Data.SystemAuth;
using Entities.Base;
using Entities.Base.Enums;
using Entities.Base.NotifitactionBuilder;
using Entities.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Services.NotifitactionBuilderServices
{
	public class NotifitactionBuilderService(
		IEntityMetadataCache _entityMetadataCache,
		ApplicationDbContext _dbContext,
		IUnitOfWork _unitOfWork,
		INotificationRuleCache _ruleCache,
		ISdk sdk) : INotifitactionBuilderService
	{

		public async Task Save(SaveNotificationBuidler entity, CancellationToken tn)
		{

			var existNotificationBuilders = await _unitOfWork.Repository<NotificationBuidler>().Table
				.Where(c => c.UserId == sdk.CurrentUser.Id && c.EntityFullName == entity.EntityFullName)
				 .ToListAsync();

			var createEntity = existNotificationBuilders.FirstOrDefault(c => c.SystemOpertion == SystemOpertions.Create);
			var editEntity = existNotificationBuilders.FirstOrDefault(c => c.SystemOpertion == SystemOpertions.Edit);
			var deleteEntity = existNotificationBuilders.FirstOrDefault(c => c.SystemOpertion == SystemOpertions.Delete);

			// ─────────────────────────────────────────────────────────
			// Create Operation
			// ─────────────────────────────────────────────────────────
			if (entity.Create.Enable)
			{
				if (createEntity is null)
				{
					createEntity = await _unitOfWork.Repository<NotificationBuidler>().AddAsync(new()
					{
						EntityFullName = entity.EntityFullName,
						ConditionsJson = entity.Create.Roles.JsonSerialize(),
						UserId = sdk.CurrentUser.Id,
						SystemOpertion = SystemOpertions.Create,
						Operation = SystemOpertions.Create.ToString(),
						MessageTitle = entity.Create.Title,
						MessageTemplate = "",
						IsActive = IsActiveEnum.Active

					}, tn, false, false);
				}
				else
				{
					createEntity.ConditionsJson = entity.Create.Roles.JsonSerialize();
					createEntity.MessageTitle = entity.Create.Title;
					createEntity.IsActive = IsActiveEnum.Active;
				}

				// Update cache
				if (createEntity != null)
					await _ruleCache.SetRuleAsync(createEntity, tn);
			}
			else if (createEntity is not null)
			{
				createEntity.IsActive = IsActiveEnum.DeActive;
				createEntity.ConditionsJson = entity.Create.Roles.JsonSerialize();

				// Remove from cache (غیرفعال شد)
				await _ruleCache.RemoveRuleAsync(sdk.CurrentUser.Id, entity.EntityFullName, SystemOpertions.Create, tn);
			}

			// ─────────────────────────────────────────────────────────
			// Edit Operation
			// ─────────────────────────────────────────────────────────
			if (entity.Edit.Enable)
			{

				if (editEntity is null)
				{
					editEntity = await _unitOfWork.Repository<NotificationBuidler>().AddAsync(new()
					{
						EntityFullName = entity.EntityFullName,
						ConditionsJson = entity.Edit.Roles.JsonSerialize(),
						UserId = sdk.CurrentUser.Id,
						SystemOpertion = SystemOpertions.Edit,
						Operation = SystemOpertions.Edit.ToString(),
						MessageTitle = entity.Edit.Title,
						MessageTemplate = "",
						IsActive = IsActiveEnum.Active

					}, tn, false, false);
				}
				else
				{
					editEntity.ConditionsJson = entity.Edit.Roles.JsonSerialize();
					editEntity.MessageTitle = entity.Edit.Title;
					editEntity.IsActive = IsActiveEnum.Active;
				}

				// Update cache
				if (editEntity != null)
					await _ruleCache.SetRuleAsync(editEntity, tn);
			}
			else if (editEntity is not null)
			{
				editEntity.IsActive = IsActiveEnum.DeActive;
				editEntity.ConditionsJson = entity.Edit.Roles.JsonSerialize();

				// Remove from cache
				await _ruleCache.RemoveRuleAsync(sdk.CurrentUser.Id, entity.EntityFullName, SystemOpertions.Edit, tn);
			}

			// ─────────────────────────────────────────────────────────
			// Delete Operation
			// ─────────────────────────────────────────────────────────
			if (entity.Delete.Enable)
			{
				if (deleteEntity is null)
				{
					deleteEntity = await _unitOfWork.Repository<NotificationBuidler>().AddAsync(new()
					{
						EntityFullName = entity.EntityFullName,
						ConditionsJson = entity.Delete.Roles.JsonSerialize(),
						UserId = sdk.CurrentUser.Id,
						SystemOpertion = SystemOpertions.Delete,
						Operation = SystemOpertions.Delete.ToString(),
						MessageTitle = entity.Delete.Title,
						MessageTemplate = "",
						IsActive = IsActiveEnum.Active

					}, tn, false, false);
				}
				else
				{
					deleteEntity.ConditionsJson = entity.Delete.Roles.JsonSerialize();
					deleteEntity.MessageTitle = entity.Delete.Title;
					deleteEntity.IsActive = IsActiveEnum.Active;
				}

				// Update cache
				if (deleteEntity != null)
					await _ruleCache.SetRuleAsync(deleteEntity, tn);
			}
			else if (deleteEntity is not null)
			{
				deleteEntity.IsActive = IsActiveEnum.DeActive;
				deleteEntity.ConditionsJson = entity.Delete.Roles.JsonSerialize();

				// Remove from cache
				await _ruleCache.RemoveRuleAsync(sdk.CurrentUser.Id, entity.EntityFullName, SystemOpertions.Delete, tn);
			}

			// ذخیره در دیتابیس
			await _unitOfWork.SaveChangesAsync(tn);





			// TODO: Implement save logic
			// Example of how to use the Expression Builder with caching (DYNAMIC VERSION):
			//
			// 1. Get the entity metadata from cache
			var entityMetadata = _entityMetadataCache.Get(entity.EntityFullName);
			//
			// 2. Parse the conditions JSON to RolesNode
			var createRule = entity.Create.Roles;
			// var editRule = entity.Edit.Roles;
			// var deleteRule = entity.Delete.Roles;
			//
			// 3. Build expressions for each operation (WITH CACHING - DYNAMIC)
			// var expression = NotificationExpressionCache.GetOrBuildExpressionDynamic(
			//     notificationId: entity.Id,
			//     rule: createRule,
			//     entityMetadata: entityMetadata
			// );
			//
			// OR use DynamicQueryHelper for easier usage:
			var query = DynamicQueryHelper.ApplyRule(
				context: _dbContext,
				rule: createRule,
				entityMetadata: entityMetadata
			);
			var sqlQuery = query.ToQueryString();
			var affectedEntities = await query.Cast<BaseEntity>().ToListAsync();


			var id = affectedEntities;
			//
			// 4. Send notifications to users
			//
			// 5. When updating rules, clear the cache
			// NotificationExpressionCache.ClearCache(entity.Id);

			await Task.CompletedTask;
		}

		/// <summary>
		/// Evaluates a notification rule and returns an expression that can be used to query entities
		/// </summary>
		/// <typeparam name="T">The entity type to query</typeparam>
		/// <param name="rule">The notification rule to evaluate</param>
		/// <param name="entityMetadata">The entity metadata for the target entity</param>
		/// <returns>An expression tree that can be used with EF Core queries</returns>
		public Expression<Func<T, bool>> EvaluateRule<T>(RolesNode rule, EntityMetadata entityMetadata)
		{
			return DynamicExpressionBuilder.BuildExpression<T>(rule, entityMetadata);
		}

		/// <summary>
		/// Evaluates a notification rule with caching for better performance
		/// </summary>
		/// <typeparam name="T">The entity type to query</typeparam>
		/// <param name="notificationId">The notification ID (for cache key)</param>
		/// <param name="rule">The notification rule to evaluate</param>
		/// <param name="entityMetadata">The entity metadata for the target entity</param>
		/// <returns>An expression tree that can be used with EF Core queries</returns>
		public Expression<Func<T, bool>> EvaluateRuleWithCache<T>(
			long notificationId,
			RolesNode rule,
			EntityMetadata entityMetadata)
		{
			return NotificationExpressionCache.GetOrBuildExpression<T>(
				notificationId,
				rule,
				entityMetadata
			);
		}

		/// <summary>
		/// Clears cached expressions for a specific notification
		/// Call this when notification rules are updated
		/// </summary>
		public void ClearNotificationCache(long notificationId)
		{
			NotificationExpressionCache.ClearCache(notificationId);
		}

		/// <summary>
		/// بررسی می‌کند که آیا یک entity تغییر یافته با rules کاربران match می‌کند
		/// و لیست کاربرانی که باید notification دریافت کنند را برمی‌گرداند
		/// </summary>
		/// <param name="entityFullName">نام کامل entity (مثلاً "User")</param>
		/// <param name="operation">نوع عملیات (Create/Edit/Delete)</param>
		/// <param name="entityId">شناسه entity تغییر یافته</param>
		/// <returns>لیست (UserId, MessageTitle) برای ارسال notification</returns>
		public async Task<List<(long UserId, string MessageTitle, string Body)>> EvaluateEntityChangeAsync(
			string entityFullName,
			SystemOpertions operation,
			string entityId,
			CancellationToken ct = default)
		{
			var result = new List<(long UserId, string MessageTitle , string Body)>();

			// دریافت rules فعال از cache (Redis)
			var rules = await _ruleCache.GetRulesAsync(entityFullName, operation, ct);

			if (rules.Count == 0)
			{
				// هیچ rule‌ای برای این entity/operation وجود ندارد
				return result;
			}

			// دریافت entity metadata از cache
			var entityMetadata = _entityMetadataCache.Get(entityFullName);
			if (entityMetadata == null)
			{
				// metadata پیدا نشد
				return result;
			}

			// پیدا کردن Type از EntityFullName (از assembly)
			var entityType = FindEntityType(entityMetadata.EntityFullName);
			if (entityType == null)
			{
				// Type پیدا نشد - ممکن است EntityFullName اشتباه باشد
				return result;
			}

			// دریافت DbSet برای این entity
			var dbSet = _dbContext.GetType()
				.GetMethod("Set", Type.EmptyTypes)?
				.MakeGenericMethod(entityType)
				.Invoke(_dbContext, null);

			if (dbSet == null)
				return result;

			// تبدیل entityId به long
			if (!long.TryParse(entityId, out var id))
				return result;

			// پیدا کردن entity با Id
			var queryable = (IQueryable<BaseEntity>)dbSet;
			var entity = await queryable.FirstOrDefaultAsync(e => e.Id == id, ct);

			if (entity == null)
			{
				// entity پیدا نشد (ممکن است حذف شده باشد)
				return result;
			}

			// بررسی هر rule
			foreach (var rule in rules)
			{
				try
				{
					// Parse کردن ConditionsJson به RolesNode
					var rolesNode = rule.ConditionsJson.JsonDeserialize<RolesNode>();
					if (rolesNode == null)
						continue;

					// ساخت expression از rule
					var expressionMethod = typeof(DynamicExpressionBuilder)
						.GetMethod("BuildExpression")
						?.MakeGenericMethod(entityType);

					if (expressionMethod == null)
						continue;

					var expression = expressionMethod.Invoke(null, new object[] { rolesNode, entityMetadata });
					if (expression == null)
						continue;

					// compile و اجرای expression روی entity
					var compiled = ((LambdaExpression)expression).Compile();
					var matches = (bool)compiled.DynamicInvoke(entity);

					if (matches)
					{
						rule.MessageTitle = rule.MessageTitle ;
						// این entity با rule این کاربر match کرد
						result.Add((rule.UserId, rule.MessageTitle, $" با شناسه '{entity.Id}' توسط '{entity.ModifiedByName}' {OperationConvetor(rule.SystemOpertion)} شد"));
					}
				}
				catch (Exception ex)
				{
					// خطا در evaluate کردن rule - ignore و ادامه
					// TODO: log کردن
					continue;
				}
			}

			return result;
		}

		/// <summary>
		/// Load کردن تمام rules فعال از دیتابیس به cache
		/// این متد را در startup فراخوانی کنید
		/// </summary>
		public async Task InitializeCacheAsync(CancellationToken ct = default)
		{
			await _ruleCache.LoadFromDatabaseAsync(async () =>
			{
				return await _unitOfWork.Repository<NotificationBuidler>()
					.TableNoTracking
					.Where(r => r.IsActive == IsActiveEnum.Active)
					.ToListAsync();
			});
		}

		/// <summary>
		/// Helper برای پیدا کردن Type از EntityFullName
		/// </summary>
		private Type? FindEntityType(string entityFullName)
		{
			// روش 1: با EntityFullName کامل (شامل namespace)
			var type = Type.GetType(entityFullName);
			if (type != null)
				return type;

			// روش 2: جستجو در assembly موجودیت‌ها
			var entitiesAssembly = typeof(BaseEntity).Assembly;

			// با نام کامل
			type = entitiesAssembly.GetType(entityFullName);
			if (type != null)
				return type;

			// روش 3: جستجو با نام ساده (بدون namespace)
			var simpleName = entityFullName.Split('.').Last();
			type = entitiesAssembly.GetTypes()
				.FirstOrDefault(t => t.Name == simpleName && typeof(BaseEntity).IsAssignableFrom(t));

			if (type != null)
				return type;

			// روش 4: جستجو در تمام types assembly
			type = entitiesAssembly.GetTypes()
				.FirstOrDefault(t => t.FullName == entityFullName || t.Name == entityFullName);

			return type;
		}

		public Task<NotificationBuidler[]> GetExist(string entityFullName, long userId)
		{
			return _unitOfWork.Repository<NotificationBuidler>().TableNoTracking
				.Where(r => r.UserId == userId && r.EntityFullName == entityFullName).ToArrayAsync();
		}

		public string OperationConvetor(SystemOpertions so)
		{
			var convert = "";
				switch(so)
			{
				case SystemOpertions.Create:
					convert = "ایجاد";
					break;
				case SystemOpertions.Edit:
					convert = "ویرایش";
					break;
				case SystemOpertions.Delete:
					convert = "حذف";
					break;
				
			}
	

				return convert;
		}

	}
}
