using Common.Attributes;
using Common.Entities.EntityMetadatas;
using Common.Utilities;
using Entities.Base.NotifitactionBuilder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Services.NotifitactionBuilderServices
{
    /// <summary>
    /// Dynamic Expression Builder for Notification Rules
    /// Converts nested RolesNode JSON structures into EF Core compatible Expression trees
    /// </summary>
    /// <remarks>
    /// Supports:
    /// - All field types (String, Int, Bool, DateTime, DateTimeShamsi, Entity, ListEntity, Select)
    /// - All operators (=, !=, >, <, >=, <=, contains, starts, ends, between, null, !null, any, all)
    /// - Nested entity navigation with SubField
    /// - Collection filtering with Any/All
    /// - Persian DateTime conversion
    /// 
    /// Example Usage:
    /// <code>
    /// var rule = JsonConvert.DeserializeObject&lt;RolesNode&gt;(jsonString);
    /// var entityMeta = EntityCache.Get("MyNamespace.Contact");
    /// var expr = DynamicExpressionBuilder.BuildExpression&lt;Contact&gt;(rule, entityMeta);
    /// var results = context.Contacts.Where(expr).ToList();
    /// </code>
    /// </remarks>
    public static class DynamicExpressionBuilder
    {
        #region Public API

        /// <summary>
        /// Builds an Expression&lt;Func&lt;T, bool&gt;&gt; from a RolesNode rule structure
        /// </summary>
        /// <typeparam name="T">Entity type to query</typeparam>
        /// <param name="rule">The root RolesNode containing the rule logic</param>
        /// <param name="entityMetadata">Cached metadata for the entity type</param>
        /// <returns>EF Core compatible expression tree</returns>
        /// <exception cref="ArgumentNullException">When rule or entityMetadata is null</exception>
        public static Expression<Func<T, bool>> BuildExpression<T>(RolesNode rule, EntityMetadata entityMetadata)
        {
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));
            if (entityMetadata == null)
                throw new ArgumentNullException(nameof(entityMetadata));

            // Create parameter expression: x => ...
            var parameter = Expression.Parameter(typeof(T), "x");

            // Build the expression body recursively
            var body = BuildNodeExpression(rule, parameter, entityMetadata, typeof(T));

            // If no valid expression was built, return a default true expression
            if (body == null)
                body = Expression.Constant(true);

            // Create and return the lambda expression
            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }

        /// <summary>
        /// Builds a dynamic expression using the EntityFullName from EntityMetadata to determine the type
        /// This method uses reflection to find the entity type from the full name
        /// </summary>
        /// <param name="rule">The root RolesNode containing the rule logic</param>
        /// <param name="entityMetadata">Cached metadata for the entity type (EntityFullName is used)</param>
        /// <returns>EF Core compatible expression tree as LambdaExpression</returns>
        /// <exception cref="ArgumentNullException">When rule or entityMetadata is null</exception>
        /// <exception cref="InvalidOperationException">When entity type cannot be found</exception>
        public static LambdaExpression BuildExpressionDynamic(RolesNode rule, EntityMetadata entityMetadata)
        {
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));
            if (entityMetadata == null)
                throw new ArgumentNullException(nameof(entityMetadata));
            if (string.IsNullOrEmpty(entityMetadata.EntityFullName))
                throw new ArgumentException("EntityFullName cannot be null or empty", nameof(entityMetadata));

            // Find the entity type from the full name using reflection
            var entityType = FindTypeByFullName(entityMetadata.EntityFullName);
            if (entityType == null)
                throw new InvalidOperationException($"Cannot find entity type: {entityMetadata.EntityFullName}");

            // Create parameter expression: x => ...
            var parameter = Expression.Parameter(entityType, "x");

            // Build the expression body recursively
            var body = BuildNodeExpression(rule, parameter, entityMetadata, entityType);

            // If no valid expression was built, return a default true expression
            if (body == null)
                body = Expression.Constant(true);

            // Create lambda expression dynamically
            // Returns Expression<Func<EntityType, bool>>
            var funcType = typeof(Func<,>).MakeGenericType(entityType, typeof(bool));
            var lambda = Expression.Lambda(funcType, body, parameter);

            return lambda;
        }

        /// <summary>
        /// Finds a type by its full name across all loaded assemblies
        /// </summary>
        /// <param name="fullName">The full name of the type (e.g., "MyApp.Entities.Contact")</param>
        /// <returns>The Type if found, null otherwise</returns>
        private static Type? FindTypeByFullName(string fullName)
        {
            // First try the current assembly and common assemblies
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                try
                {
                    var type = assembly.GetType(fullName);
                    if (type != null)
                        return type;
                }
                catch
                {
                    // Skip assemblies that can't be loaded
                    continue;
                }
            }

            // Try using Type.GetType with assembly qualified name
            try
            {
                var type = Type.GetType(fullName);
                if (type != null)
                    return type;
            }
            catch
            {
                // Ignore
            }

            return null;
        }

        #endregion

        #region Group & Logic Handling

        /// <summary>
        /// Builds expression for a single RolesNode (either a group or a condition)
        /// </summary>
        private static Expression? BuildNodeExpression(RolesNode node, Expression parameter, EntityMetadata entityMetadata, Type currentType)
        {
            // Check if this is a group node (has Logic and Criteria)
            if (!string.IsNullOrEmpty(node.Logic) && node.Criteria != null && node.Criteria.Count > 0)
            {
                return BuildGroupExpression(node, parameter, entityMetadata, currentType);
            }

            // Otherwise, it's a condition node
            if (!string.IsNullOrEmpty(node.Data))
            {
                return BuildConditionExpression(node, parameter, entityMetadata, currentType);
            }

            // Invalid node structure
            return null;
        }

        /// <summary>
        /// Builds expression for a group with AND/OR logic
        /// </summary>
        private static Expression? BuildGroupExpression(RolesNode groupNode, Expression parameter, EntityMetadata entityMetadata, Type currentType)
        {
            var expressions = new List<Expression>();

            if (groupNode.Criteria == null)
                return null;

            foreach (var criterion in groupNode.Criteria)
            {
                var expr = BuildNodeExpression(criterion, parameter, entityMetadata, currentType);
                if (expr != null)
                    expressions.Add(expr);
            }

            if (expressions.Count == 0)
                return null;

            // Combine expressions based on logic
            Expression result = expressions[0];
            for (int i = 1; i < expressions.Count; i++)
            {
                if (groupNode.Logic?.ToUpper() == "AND")
                    result = Expression.AndAlso(result, expressions[i]);
                else if (groupNode.Logic?.ToUpper() == "OR")
                    result = Expression.OrElse(result, expressions[i]);
            }

            return result;
        }

        #endregion

        #region Condition Building

        /// <summary>
        /// Builds expression for a single condition (field + operator + value)
        /// </summary>
        private static Expression BuildConditionExpression(RolesNode condition, Expression parameter, EntityMetadata entityMetadata, Type currentType)
        {
            // Find the property metadata
            var propertyMeta = entityMetadata.Properties.FirstOrDefault(p => p.Name == condition.Data);
            if (propertyMeta == null)
                return Expression.Constant(true); // Skip unknown properties

            // Build property access expression
            var propertyAccess = BuildPropertyAccess(parameter, propertyMeta, currentType);
            if (propertyAccess == null)
                return Expression.Constant(true);

            // Handle SubField (nested entity navigation or collection operations)
            if (condition.SubField != null)
            {
                return BuildSubFieldExpression(propertyAccess, condition, propertyMeta, entityMetadata);
            }

            // Apply operator to the property
            return ApplyOperator(propertyAccess, condition.Condition, condition.Value, propertyMeta.SystemType, propertyMeta);
        }

        /// <summary>
        /// Builds nested SubField expressions for Entity and ListEntity types
        /// </summary>
        private static Expression BuildSubFieldExpression(Expression parentProperty, RolesNode condition, PropertyMetadata propertyMeta, EntityMetadata? parentEntityMeta)
        {
            // For ListEntity: must use Any or All operators
            if (propertyMeta.SystemType == SystemType.ListEntity)
            {
                return BuildCollectionExpression(parentProperty, condition, propertyMeta);
            }

            // For Entity: navigate to the SubField
            if (propertyMeta.SystemType == SystemType.Entity)
            {
                // SubField is a nested condition on the related entity
                var subFieldNode = condition.SubField;

                if (subFieldNode == null || string.IsNullOrEmpty(subFieldNode.Data))
                    return Expression.Constant(true);

                // Get the related entity metadata (would need to be provided or cached)
                // For now, we'll build the expression based on the property path
                var subPropertyAccess = BuildNestedPropertyAccess(parentProperty, subFieldNode.Data);

                if (subPropertyAccess == null)
                    return Expression.Constant(true);

                // Determine the field type from SubField's FieldType or infer it
                var subFieldType = ParseFieldType(subFieldNode.FieldType ?? "string");

                // If SubField has its own SubField, recursively handle it
                if (subFieldNode.SubField != null)
                {
                    // Create a temporary PropertyMetadata for the sub-property
                    var subPropMeta = new PropertyMetadata
                    {
                        SystemType = subFieldType,
                        Name = subFieldNode.Data ?? string.Empty
                    };
                    return BuildSubFieldExpression(subPropertyAccess, subFieldNode, subPropMeta, parentEntityMeta);
                }

                // Apply the operator to the nested property
                return ApplyOperator(subPropertyAccess, subFieldNode.Condition, subFieldNode.Value, subFieldType, null);
            }

            return Expression.Constant(true);
        }

        #endregion

        #region Collection Handling

        /// <summary>
        /// Builds Any/All expressions for ListEntity collections
        /// </summary>
        private static Expression BuildCollectionExpression(Expression collectionProperty, RolesNode condition, PropertyMetadata propertyMeta)
        {
            var collectionType = collectionProperty.Type;

            // Get the element type of the collection
            Type elementType = null;
            if (collectionType.IsGenericType)
            {
                var genericArgs = collectionType.GetGenericArguments();
                if (genericArgs.Length > 0)
                    elementType = genericArgs[0];
            }
            else if (collectionType.IsArray)
            {
                elementType = collectionType.GetElementType();
            }
            else
            {
                // Try to get from IEnumerable<T>
                var enumerableInterface = collectionType.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
                if (enumerableInterface != null)
                    elementType = enumerableInterface.GetGenericArguments()[0];
            }

            if (elementType == null)
                return Expression.Constant(true);

            // Create lambda parameter for collection item: item => ...
            var itemParameter = Expression.Parameter(elementType, "item");

            // Build the condition expression for the collection item
            var subFieldNode = condition.SubField;
            if (subFieldNode == null)
                return Expression.Constant(true);

            // Build property access on the item
            var itemPropertyAccess = BuildNestedPropertyAccess(itemParameter, subFieldNode.Data ?? string.Empty);
            if (itemPropertyAccess == null)
                return Expression.Constant(true);

            // Determine the field type
            var subFieldType = ParseFieldType(subFieldNode.FieldType ?? "string");

            // Build the condition expression
            Expression itemCondition;

            // If SubField has its own SubField, handle nested navigation
            if (subFieldNode.SubField != null)
            {
                var subPropMeta = new PropertyMetadata
                {
                    SystemType = subFieldType,
                    Name = subFieldNode.Data ?? string.Empty
                };
                itemCondition = BuildSubFieldExpression(itemPropertyAccess, subFieldNode, subPropMeta, null);
            }
            else
            {
                itemCondition = ApplyOperator(itemPropertyAccess, subFieldNode.Condition, subFieldNode.Value, subFieldType, null);
            }

            if (itemCondition == null)
                return Expression.Constant(true);

            // Create lambda: item => condition
            var lambda = Expression.Lambda(itemCondition, itemParameter);

            // Apply Any or All
            var methodName = condition.Condition?.ToLower() == "all" ? "All" : "Any";
            var method = typeof(Enumerable).GetMethods()
                .FirstOrDefault(m => m.Name == methodName && m.GetParameters().Length == 2)
                ?.MakeGenericMethod(elementType);

            if (method == null)
                return Expression.Constant(true);

            // Call Enumerable.Any/All(collection, lambda)
            return Expression.Call(method, collectionProperty, lambda);
        }

        #endregion

        #region Property Navigation

        /// <summary>
        /// Builds property access expression using PropertyMetadata
        /// </summary>
        private static Expression? BuildPropertyAccess(Expression parameter, PropertyMetadata propertyMeta, Type currentType)
        {
            // Use SearchPath if available, otherwise use Name
            var propertyPath = !string.IsNullOrEmpty(propertyMeta.SearchPath)
                ? propertyMeta.SearchPath
                : propertyMeta.Name;

            return BuildNestedPropertyAccess(parameter, propertyPath ?? string.Empty);
        }

        /// <summary>
        /// Builds nested property access for paths like "Address.City.Name"
        /// </summary>
        private static Expression? BuildNestedPropertyAccess(Expression parameter, string propertyPath)
        {
            if (string.IsNullOrEmpty(propertyPath))
                return null;

            var properties = propertyPath.Split('.');
            Expression current = parameter;

            foreach (var propName in properties)
            {
                if (string.IsNullOrEmpty(propName))
                    continue;

                var propertyInfo = current.Type.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (propertyInfo == null)
                    return null; // Property not found

                current = Expression.Property(current, propertyInfo);
            }

            return current;
        }

        #endregion

        #region Operator Application

        /// <summary>
        /// Applies the operator to the property expression
        /// </summary>
        private static Expression ApplyOperator(Expression property, string? operatorSymbol, List<string>? values, SystemType fieldType, PropertyMetadata? propertyMeta)
        {
            if (string.IsNullOrEmpty(operatorSymbol))
                return Expression.Constant(true);

            var op = operatorSymbol.ToLower().Trim();

            // Null checks
            if (op == "null" || op == "!null")
            {
                return BuildNullCheckExpression(property, op == "null");
            }

            // Get values
            var valueList = values ?? new List<string>();

            // Operators requiring values
            switch (op)
            {
                case "=":
                    return BuildEqualsExpression(property, valueList.FirstOrDefault(), fieldType);

                case "!=":
                    return BuildNotEqualsExpression(property, valueList.FirstOrDefault(), fieldType);

                case ">":
                    return BuildGreaterThanExpression(property, valueList.FirstOrDefault(), fieldType);

                case "<":
                    return BuildLessThanExpression(property, valueList.FirstOrDefault(), fieldType);

                case ">=":
                    return BuildGreaterThanOrEqualExpression(property, valueList.FirstOrDefault(), fieldType);

                case "<=":
                    return BuildLessThanOrEqualExpression(property, valueList.FirstOrDefault(), fieldType);

                case "contains":
                    return BuildContainsExpression(property, valueList.FirstOrDefault());

                case "starts":
                case "startswith":
                    return BuildStartsWithExpression(property, valueList.FirstOrDefault());

                case "ends":
                case "endswith":
                    return BuildEndsWithExpression(property, valueList.FirstOrDefault());

                case "between":
                    return BuildBetweenExpression(property, valueList, fieldType);

                case "!between":
                    return BuildNotBetweenExpression(property, valueList, fieldType);

                case "in":
                    return BuildInExpression(property, valueList, fieldType);

                case "!in":
                case "notin":
                    return BuildNotInExpression(property, valueList, fieldType);

                default:
                    return Expression.Constant(true); // Unknown operator
            }
        }

        /// <summary>
        /// Builds null/not null check expression
        /// </summary>
        private static Expression BuildNullCheckExpression(Expression property, bool checkNull)
        {
            var nullValue = Expression.Constant(null, property.Type);
            return checkNull
                ? Expression.Equal(property, nullValue)
                : Expression.NotEqual(property, nullValue);
        }

        /// <summary>
        /// Builds equals expression: property == value
        /// </summary>
        private static Expression BuildEqualsExpression(Expression property, string? value, SystemType fieldType)
        {
            var constantValue = ConvertValue(value, fieldType, property.Type);
            return Expression.Equal(property, constantValue);
        }

        /// <summary>
        /// Builds not equals expression: property != value
        /// </summary>
        private static Expression BuildNotEqualsExpression(Expression property, string? value, SystemType fieldType)
        {
            var constantValue = ConvertValue(value, fieldType, property.Type);
            return Expression.NotEqual(property, constantValue);
        }

        /// <summary>
        /// Builds greater than expression: property > value
        /// </summary>
        private static Expression BuildGreaterThanExpression(Expression property, string? value, SystemType fieldType)
        {
            var constantValue = ConvertValue(value, fieldType, property.Type);
            return Expression.GreaterThan(property, constantValue);
        }

        /// <summary>
        /// Builds less than expression: property < value
        /// </summary>
        private static Expression BuildLessThanExpression(Expression property, string? value, SystemType fieldType)
        {
            var constantValue = ConvertValue(value, fieldType, property.Type);
            return Expression.LessThan(property, constantValue);
        }

        /// <summary>
        /// Builds greater than or equal expression: property >= value
        /// </summary>
        private static Expression BuildGreaterThanOrEqualExpression(Expression property, string? value, SystemType fieldType)
        {
            var constantValue = ConvertValue(value, fieldType, property.Type);
            return Expression.GreaterThanOrEqual(property, constantValue);
        }

        /// <summary>
        /// Builds less than or equal expression: property <= value
        /// </summary>
        private static Expression BuildLessThanOrEqualExpression(Expression property, string? value, SystemType fieldType)
        {
            var constantValue = ConvertValue(value, fieldType, property.Type);
            return Expression.LessThanOrEqual(property, constantValue);
        }

        /// <summary>
        /// Builds contains expression: property.Contains(value)
        /// </summary>
        private static Expression BuildContainsExpression(Expression property, string? value)
        {
            if (string.IsNullOrEmpty(value))
                return Expression.Constant(true);

            // Ensure property is string type
            if (property.Type != typeof(string))
                return Expression.Constant(true);

            var method = typeof(string).GetMethod("Contains", new[] { typeof(string) });
            if (method == null)
                return Expression.Constant(true);

            var constantValue = Expression.Constant(value, typeof(string));

            // Null-safe: property != null && property.Contains(value)
            var nullCheck = Expression.NotEqual(property, Expression.Constant(null, typeof(string)));
            var containsCall = Expression.Call(property, method, constantValue);

            return Expression.AndAlso(nullCheck, containsCall);
        }

        /// <summary>
        /// Builds starts with expression: property.StartsWith(value)
        /// </summary>
        private static Expression BuildStartsWithExpression(Expression property, string? value)
        {
            if (string.IsNullOrEmpty(value))
                return Expression.Constant(true);

            if (property.Type != typeof(string))
                return Expression.Constant(true);

            var method = typeof(string).GetMethod("StartsWith", new[] { typeof(string) });
            if (method == null)
                return Expression.Constant(true);

            var constantValue = Expression.Constant(value, typeof(string));

            var nullCheck = Expression.NotEqual(property, Expression.Constant(null, typeof(string)));
            var startsWithCall = Expression.Call(property, method, constantValue);

            return Expression.AndAlso(nullCheck, startsWithCall);
        }

        /// <summary>
        /// Builds ends with expression: property.EndsWith(value)
        /// </summary>
        private static Expression BuildEndsWithExpression(Expression property, string? value)
        {
            if (string.IsNullOrEmpty(value))
                return Expression.Constant(true);

            if (property.Type != typeof(string))
                return Expression.Constant(true);

            var method = typeof(string).GetMethod("EndsWith", new[] { typeof(string) });
            if (method == null)
                return Expression.Constant(true);

            var constantValue = Expression.Constant(value, typeof(string));

            var nullCheck = Expression.NotEqual(property, Expression.Constant(null, typeof(string)));
            var endsWithCall = Expression.Call(property, method, constantValue);

            return Expression.AndAlso(nullCheck, endsWithCall);
        }

        /// <summary>
        /// Builds between expression: value1 <= property <= value2
        /// </summary>
        private static Expression BuildBetweenExpression(Expression property, List<string> values, SystemType fieldType)
        {
            if (values == null || values.Count < 2)
                return Expression.Constant(true);

            var minValue = ConvertValue(values[0], fieldType, property.Type);
            var maxValue = ConvertValue(values[1], fieldType, property.Type);

            var greaterThanOrEqual = Expression.GreaterThanOrEqual(property, minValue);
            var lessThanOrEqual = Expression.LessThanOrEqual(property, maxValue);

            return Expression.AndAlso(greaterThanOrEqual, lessThanOrEqual);
        }

        /// <summary>
        /// Builds not between expression: !(value1 <= property <= value2)
        /// </summary>
        private static Expression BuildNotBetweenExpression(Expression property, List<string> values, SystemType fieldType)
        {
            var betweenExpr = BuildBetweenExpression(property, values, fieldType);
            return Expression.Not(betweenExpr);
        }

        /// <summary>
        /// Builds in expression: property IN (value1, value2, ...)
        /// </summary>
        private static Expression BuildInExpression(Expression property, List<string> values, SystemType fieldType)
        {
            if (values == null || values.Count == 0)
                return Expression.Constant(true);

            Expression result = null;

            foreach (var value in values)
            {
                var constantValue = ConvertValue(value, fieldType, property.Type);
                var equals = Expression.Equal(property, constantValue);

                result = result == null ? equals : Expression.OrElse(result, equals);
            }

            return result ?? Expression.Constant(true);
        }

        /// <summary>
        /// Builds not in expression: property NOT IN (value1, value2, ...)
        /// </summary>
        private static Expression BuildNotInExpression(Expression property, List<string> values, SystemType fieldType)
        {
            var inExpr = BuildInExpression(property, values, fieldType);
            return Expression.Not(inExpr);
        }

        #endregion

        #region Type Conversion

        /// <summary>
        /// Converts string value to typed constant expression based on SystemType
        /// </summary>
        private static Expression ConvertValue(string? value, SystemType fieldType, Type targetType)
        {
            if (value == null)
                return Expression.Constant(null, targetType);

            try
            {
                object convertedValue = null;

                switch (fieldType)
                {
                    case SystemType.String:
                        convertedValue = value;
                        break;

                    case SystemType.Int:
                        convertedValue = int.Parse(value.Fa2En());
                        break;

                    case SystemType.Long:
                        convertedValue = long.Parse(value.Fa2En());
                        break;

                    case SystemType.Boolean:
                        convertedValue = bool.Parse(value);
                        break;

                    case SystemType.DateTime:
                        convertedValue = DateTime.Parse(value);
                        break;

                    case SystemType.Date:
                        convertedValue = DateTime.Parse(value).Date;
                        break;

                    case SystemType.DateTimeShamsi:
                        // Parse Persian DateTime: "1404/10/09 10:40:16"
                        convertedValue = value.ToMiladiDateTime();
                        break;

                    case SystemType.DateShamsi:
                        // Parse Persian Date: "1404/10/09"
                        convertedValue = value.ToMiladiDate();
                        break;

                    case SystemType.Decimal:
                        convertedValue = decimal.Parse(value.Fa2En());
                        break;

                    case SystemType.Select:
                        // Try to parse as int for select values (often used for enums)
                        if (int.TryParse(value.Fa2En(), out int selectValue))
                            convertedValue = selectValue;
                        else
                            convertedValue = value;
                        break;

                    case SystemType.AutoNumber:
                        // AutoNumber is typically long or int
                        if (targetType == typeof(long) || targetType == typeof(long?))
                            convertedValue = long.Parse(value.Fa2En());
                        else
                            convertedValue = int.Parse(value.Fa2En());
                        break;

                    case SystemType.File:
                        // File is typically stored as string (path) or byte array
                        // For expression building, we'll treat it as string
                        convertedValue = value;
                        break;

                    case SystemType.Entity:
                        // Entity IDs are typically long or int, but could be enum
                        var entityUnderlyingType = targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>)
                            ? Nullable.GetUnderlyingType(targetType)
                            : targetType;

                        if (entityUnderlyingType != null && entityUnderlyingType.IsEnum)
                        {
                            // If target is enum, parse as int first, will be converted to enum later
                            if (int.TryParse(value.Fa2En(), out int enumIntValue))
                                convertedValue = enumIntValue;
                            else
                                convertedValue = value; // Try as enum name string
                        }
                        else if (targetType == typeof(long) || targetType == typeof(long?))
                        {
                            convertedValue = long.Parse(value.Fa2En());
                        }
                        else if (targetType == typeof(int) || targetType == typeof(int?))
                        {
                            convertedValue = int.Parse(value.Fa2En());
                        }
                        else
                        {
                            convertedValue = value;
                        }
                        break;

                    default:
                        // Default to string
                        convertedValue = value;
                        break;
                }

                // Handle nullable types
                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    var underlyingType = Nullable.GetUnderlyingType(targetType);
                    if (underlyingType != null && convertedValue != null && convertedValue.GetType() != underlyingType)
                    {
                        // Handle enum conversion for nullable types
                        if (underlyingType.IsEnum)
                        {
                            if (convertedValue is string enumString)
                            {
                                // Try parsing as enum name, if fails try as numeric value
                                if (Enum.TryParse(underlyingType, enumString, true, out var parsedEnum))
                                    convertedValue = parsedEnum;
                                else if (int.TryParse(enumString.Fa2En(), out int numericValue))
                                    convertedValue = Enum.ToObject(underlyingType, numericValue);
                                else
                                    convertedValue = Convert.ChangeType(convertedValue, underlyingType);
                            }
                            else if (IsNumericType(convertedValue.GetType()))
                            {
                                convertedValue = Enum.ToObject(underlyingType, convertedValue);
                            }
                            else if (convertedValue.GetType() != underlyingType)
                            {
                                // Try to convert to underlying type
                                convertedValue = Convert.ChangeType(convertedValue, underlyingType);
                            }
                            // else: already the correct enum type, no conversion needed
                            {
                                convertedValue = Convert.ChangeType(convertedValue, underlyingType);
                            }
                        }
                        else
                        {
                            convertedValue = Convert.ChangeType(convertedValue, underlyingType);
                        }
                    }
                }
                else if (convertedValue != null && convertedValue.GetType() != targetType)
                {
                    // Handle enum conversion for non-nullable types
                    if (targetType.IsEnum)
                    {
                        if (convertedValue is string enumString)
                        {
                            // Try parsing as enum name, if fails try as numeric value
                            if (Enum.TryParse(targetType, enumString, true, out var parsedEnum))
                                convertedValue = parsedEnum;
                            else if (int.TryParse(enumString.Fa2En(), out int numericValue))
                                convertedValue = Enum.ToObject(targetType, numericValue);
                            else
                                convertedValue = Convert.ChangeType(convertedValue, targetType);
                        }
                        else if (IsNumericType(convertedValue.GetType()))
                        {
                            convertedValue = Enum.ToObject(targetType, convertedValue);
                        }
                        else if (convertedValue.GetType() != targetType)
                        {
                            // Try to convert to target type
                            convertedValue = Convert.ChangeType(convertedValue, targetType);
                        }
                        // else: already the correct enum type, no conversion needed
                        {
                            convertedValue = Convert.ChangeType(convertedValue, targetType);
                        }
                    }
                    else
                    {
                        // Try to convert to target type
                        convertedValue = Convert.ChangeType(convertedValue, targetType);
                    }
                }

                return Expression.Constant(convertedValue, targetType);
            }
            catch
            {
                // If conversion fails, return default value for the type
                return Expression.Constant(GetDefaultValue(targetType), targetType);
            }
        }

        /// <summary>
        /// Parses field type string to SystemType enum
        /// </summary>
        private static SystemType ParseFieldType(string? fieldType)
        {
            if (string.IsNullOrEmpty(fieldType))
                return SystemType.String;

            if (Enum.TryParse<SystemType>(fieldType, true, out var result))
                return result;

            // Handle common type name variations
            var lowerType = fieldType.ToLower();
            if (lowerType.Contains("int") || lowerType.Contains("number"))
                return SystemType.Int;
            if (lowerType.Contains("bool"))
                return SystemType.Boolean;
            if (lowerType.Contains("date"))
            {
                if (lowerType.Contains("shamsi"))
                    return lowerType.Contains("time") ? SystemType.DateTimeShamsi : SystemType.DateShamsi;
                return lowerType.Contains("time") ? SystemType.DateTime : SystemType.Date;
            }
            if (lowerType.Contains("select"))
                return SystemType.Select;
            if (lowerType.Contains("entity"))
                return lowerType.Contains("list") ? SystemType.ListEntity : SystemType.Entity;

            return SystemType.String;
        }

        /// <summary>
        /// Gets the default value for a type
        /// </summary>
        private static object? GetDefaultValue(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);
            return null;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Checks if a type is a collection type
        /// </summary>
        private static bool IsCollectionType(Type type)
        {
            if (type == typeof(string))
                return false;

            return typeof(IEnumerable).IsAssignableFrom(type);
        }

        /// <summary>
        /// Gets the element type of a collection
        /// </summary>
        private static Type? GetCollectionElementType(Type collectionType)
        {
            if (collectionType.IsArray)
                return collectionType.GetElementType();

            if (collectionType.IsGenericType)
            {
                var genericArgs = collectionType.GetGenericArguments();
                if (genericArgs.Length > 0)
                    return genericArgs[0];
            }

            var enumerableInterface = collectionType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            return enumerableInterface?.GetGenericArguments()[0];
        }

        /// <summary>
        /// Checks if a type is a numeric type (int, long, short, byte, etc.)
        /// </summary>
        private static bool IsNumericType(Type type)
        {
            return type == typeof(int) || type == typeof(int?) ||
                   type == typeof(long) || type == typeof(long?) ||
                   type == typeof(short) || type == typeof(short?) ||
                   type == typeof(byte) || type == typeof(byte?) ||
                   type == typeof(uint) || type == typeof(uint?) ||
                   type == typeof(ulong) || type == typeof(ulong?) ||
                   type == typeof(ushort) || type == typeof(ushort?) ||
                   type == typeof(sbyte) || type == typeof(sbyte?) ||
                   type == typeof(decimal) || type == typeof(decimal?) ||
                   type == typeof(double) || type == typeof(double?) ||
                   type == typeof(float) || type == typeof(float?);
        }

        #endregion
    }
}

