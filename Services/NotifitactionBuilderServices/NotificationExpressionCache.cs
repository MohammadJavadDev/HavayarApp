using Common.Entities.EntityMetadatas;
using Entities.Base.NotifitactionBuilder;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;

namespace Services.NotifitactionBuilderServices
{
    /// <summary>
    /// Thread-safe cache for notification rule expressions
    /// Caches both Expression trees (for EF Core) and compiled Func delegates (for in-memory evaluation)
    /// </summary>
    public class NotificationExpressionCache
    {
        private static readonly ConcurrentDictionary<string, object> _expressionCache
            = new ConcurrentDictionary<string, object>();

        private static readonly ConcurrentDictionary<string, object> _compiledCache
            = new ConcurrentDictionary<string, object>();

        /// <summary>
        /// Gets or builds an expression tree (for use with EF Core queries)
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="notificationId">Notification rule ID</param>
        /// <param name="rule">The RolesNode rule structure</param>
        /// <param name="entityMetadata">Entity metadata</param>
        /// <param name="forceRebuild">If true, rebuilds even if cached</param>
        /// <returns>Expression tree for EF Core</returns>
        public static Expression<Func<T, bool>> GetOrBuildExpression<T>(
            long notificationId,
            RolesNode rule,
            EntityMetadata entityMetadata,
            bool forceRebuild = false)
        {
            var cacheKey = CreateCacheKey(notificationId, typeof(T));

            // Force rebuild if requested
            if (forceRebuild)
            {
                _expressionCache.TryRemove(cacheKey, out _);
            }

            // Try get from cache
            if (_expressionCache.TryGetValue(cacheKey, out var cached))
            {
                return (Expression<Func<T, bool>>)cached;
            }

            // Build new expression
            var expression = DynamicExpressionBuilder.BuildExpression<T>(rule, entityMetadata);

            // Cache it
            _expressionCache.TryAdd(cacheKey, expression);

            return expression;
        }

        /// <summary>
        /// Gets or builds a compiled function (for in-memory evaluation, NOT for EF Core)
        /// Use this when you need to evaluate individual entities in memory
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="notificationId">Notification rule ID</param>
        /// <param name="rule">The RolesNode rule structure</param>
        /// <param name="entityMetadata">Entity metadata</param>
        /// <param name="forceRebuild">If true, rebuilds even if cached</param>
        /// <returns>Compiled delegate for fast in-memory evaluation</returns>
        public static Func<T, bool> GetOrBuildCompiledFunc<T>(
            long notificationId,
            RolesNode rule,
            EntityMetadata entityMetadata,
            bool forceRebuild = false)
        {
            var cacheKey = CreateCacheKey(notificationId, typeof(T));

            // Force rebuild if requested
            if (forceRebuild)
            {
                _compiledCache.TryRemove(cacheKey, out _);
            }

            // Try get from cache
            if (_compiledCache.TryGetValue(cacheKey, out var cached))
            {
                return (Func<T, bool>)cached;
            }

            // Build and compile
            var expression = DynamicExpressionBuilder.BuildExpression<T>(rule, entityMetadata);
            var compiled = expression.Compile();

            // Cache it
            _compiledCache.TryAdd(cacheKey, compiled);

            return compiled;
        }

        /// <summary>
        /// Clears cached expressions for a specific notification
        /// Call this when a notification rule is updated or deleted
        /// </summary>
        /// <param name="notificationId">Notification rule ID</param>
        public static void ClearCache(long notificationId)
        {
            var prefix = $"notif_{notificationId}_";

            // Clear expression cache
            var expressionKeys = _expressionCache.Keys.Where(k => k.StartsWith(prefix)).ToList();
            foreach (var key in expressionKeys)
            {
                _expressionCache.TryRemove(key, out _);
            }

            // Clear compiled cache
            var compiledKeys = _compiledCache.Keys.Where(k => k.StartsWith(prefix)).ToList();
            foreach (var key in compiledKeys)
            {
                _compiledCache.TryRemove(key, out _);
            }
        }

        /// <summary>
        /// Clears all cached expressions
        /// </summary>
        public static void ClearAllCache()
        {
            _expressionCache.Clear();
            _compiledCache.Clear();
        }

        /// <summary>
        /// Builds a dynamic expression using EntityFullName from EntityMetadata
        /// The entity type is determined at runtime from the EntityFullName
        /// </summary>
        /// <param name="notificationId">Notification rule ID</param>
        /// <param name="rule">The RolesNode rule structure</param>
        /// <param name="entityMetadata">Entity metadata (EntityFullName is used to find type)</param>
        /// <param name="forceRebuild">If true, rebuilds even if cached</param>
        /// <returns>Dynamic lambda expression</returns>
        public static LambdaExpression GetOrBuildExpressionDynamic(
            long notificationId,
            RolesNode rule,
            EntityMetadata entityMetadata,
            bool forceRebuild = false)
        {
            var cacheKey = CreateCacheKey(notificationId, entityMetadata.EntityFullName ?? "Unknown");

            // Force rebuild if requested
            if (forceRebuild)
            {
                _expressionCache.TryRemove(cacheKey, out _);
            }

            // Try get from cache
            if (_expressionCache.TryGetValue(cacheKey, out var cached))
            {
                return (LambdaExpression)cached;
            }

            // Build new expression dynamically
            var expression = DynamicExpressionBuilder.BuildExpressionDynamic(rule, entityMetadata);

            // Cache it
            _expressionCache.TryAdd(cacheKey, expression);

            return expression;
        }

        /// <summary>
        /// Gets cache statistics
        /// </summary>
        public static CacheStatistics GetStatistics()
        {
            return new CacheStatistics
            {
                ExpressionCacheCount = _expressionCache.Count,
                CompiledCacheCount = _compiledCache.Count,
                TotalMemoryEstimate = (_expressionCache.Count + _compiledCache.Count) * 1024 // Rough estimate in bytes
            };
        }

        /// <summary>
        /// Creates a unique cache key for notification + entity type
        /// </summary>
        private static string CreateCacheKey(long notificationId, Type entityType)
        {
            return $"notif_{notificationId}_{entityType.FullName}";
        }

        /// <summary>
        /// Creates a unique cache key for notification + entity full name string
        /// </summary>
        private static string CreateCacheKey(long notificationId, string entityFullName)
        {
            return $"notif_{notificationId}_{entityFullName}";
        }
    }

    /// <summary>
    /// Cache statistics
    /// </summary>
    public class CacheStatistics
    {
        public int ExpressionCacheCount { get; set; }
        public int CompiledCacheCount { get; set; }
        public long TotalMemoryEstimate { get; set; }
    }
}


