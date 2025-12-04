using Common.Entities.EntityMetadatas;
using Entities.Base.Enums;
using Entities.Base.NotifitactionBuilder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Services.NotifitactionBuilderServices
{
	public interface INotifitactionBuilderService
	{
		Task Save(SaveNotificationBuidler entity, CancellationToken tn);

		/// <summary>
		/// Evaluates a notification rule and returns an expression that can be used to query entities
		/// </summary>
		/// <typeparam name="T">The entity type to query</typeparam>
		/// <param name="rule">The notification rule to evaluate</param>
		/// <param name="entityMetadata">The entity metadata for the target entity</param>
		/// <returns>An expression tree that can be used with EF Core queries</returns>
		Expression<Func<T, bool>> EvaluateRule<T>(RolesNode rule, EntityMetadata entityMetadata);

		/// <summary>
		/// بررسی entity تغییر یافته در برابر rules کاربران و برگرداندن لیست کاربران برای notification
		/// </summary>
		Task<List<(long UserId, string MessageTitle, string Body)>> EvaluateEntityChangeAsync(
			string entityFullName,
			SystemOpertions operation,
			string entityId,
			CancellationToken ct = default);

		/// <summary>
		/// Initialize کردن cache از دیتابیس
		/// </summary>
		Task InitializeCacheAsync(CancellationToken ct = default);

		Task<NotificationBuidler[]> GetExist(string entityFullName, long userId);
	}
}
