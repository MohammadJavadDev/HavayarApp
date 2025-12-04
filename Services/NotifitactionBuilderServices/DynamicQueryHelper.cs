using Common.Entities.EntityMetadatas;
using Entities.Base.NotifitactionBuilder;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Services.NotifitactionBuilderServices
{
    /// <summary>
    /// Helper methods for querying database dynamically using notification rules
    /// </summary>
    public static class DynamicQueryHelper
    {
        /// <summary>
        /// Applies a notification rule to a DbSet dynamically
        /// Uses the EntityFullName from EntityMetadata to determine the entity type
        /// </summary>
        /// <param name="context">The DbContext instance</param>
        /// <param name="rule">The notification rule</param>
        /// <param name="entityMetadata">Entity metadata (EntityFullName is used)</param>
        /// <returns>IQueryable with the filter applied</returns>
        /// <example>
        /// var query = DynamicQueryHelper.ApplyRule(context, rule, entityMetadata);
        /// var results = await query.ToListAsync();
        /// </example>
        public static IQueryable ApplyRule(
            DbContext context,
            RolesNode rule,
            EntityMetadata entityMetadata)
        {
            // Build the expression dynamically
            var expression = DynamicExpressionBuilder.BuildExpressionDynamic(rule, entityMetadata);

            // Get the entity type from the expression
            var entityType = expression.Parameters[0].Type;

            // Get DbSet<T> using reflection
            var setMethod = typeof(DbContext).GetMethod(nameof(DbContext.Set), Array.Empty<Type>());
            if (setMethod == null)
                throw new InvalidOperationException("Cannot find DbContext.Set method");

            var genericSetMethod = setMethod.MakeGenericMethod(entityType);
            var dbSet = genericSetMethod.Invoke(context, null) as IQueryable;

            if (dbSet == null)
                throw new InvalidOperationException($"Cannot create DbSet for type {entityType.FullName}");

            // Apply the where clause
            return ApplyWhereClause(dbSet, expression);
        }

        /// <summary>
        /// Applies a notification rule to a DbSet with caching
        /// </summary>
        /// <param name="context">The DbContext instance</param>
        /// <param name="notificationId">Notification ID for caching</param>
        /// <param name="rule">The notification rule</param>
        /// <param name="entityMetadata">Entity metadata (EntityFullName is used)</param>
        /// <returns>IQueryable with the filter applied</returns>
        public static IQueryable ApplyRuleWithCache(
            DbContext context,
            long notificationId,
            RolesNode rule,
            EntityMetadata entityMetadata)
        {
            // Build or get cached expression
            var expression = NotificationExpressionCache.GetOrBuildExpressionDynamic(
                notificationId,
                rule,
                entityMetadata
            );

            // Get the entity type from the expression
            var entityType = expression.Parameters[0].Type;

            // Get DbSet<T> using reflection
            var setMethod = typeof(DbContext).GetMethod(nameof(DbContext.Set), Array.Empty<Type>());
            if (setMethod == null)
                throw new InvalidOperationException("Cannot find DbContext.Set method");

            var genericSetMethod = setMethod.MakeGenericMethod(entityType);
            var dbSet = genericSetMethod.Invoke(context, null) as IQueryable;

            if (dbSet == null)
                throw new InvalidOperationException($"Cannot create DbSet for type {entityType.FullName}");

            // Apply the where clause
            return ApplyWhereClause(dbSet, expression);
        }

        /// <summary>
        /// Applies a LambdaExpression as a Where clause to an IQueryable
        /// </summary>
        private static IQueryable ApplyWhereClause(IQueryable source, LambdaExpression expression)
        {
            // Use reflection to call Queryable.Where<T>(IQueryable<T>, Expression<Func<T, bool>>)
            var whereMethod = typeof(Queryable)
                .GetMethods()
                .First(m => m.Name == "Where" && m.GetParameters().Length == 2)
                .MakeGenericMethod(expression.Parameters[0].Type);

            var result = whereMethod.Invoke(null, new object[] { source, expression });
            return result as IQueryable ?? source;
        }
    }
}


