using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace HavayarApp.Data.Actions
{
    /// <summary>
    /// ActionRegistry is a static class responsible for managing and registering application actions.
    /// Automatically discovers and registers actions marked with ActionAttribute.
    /// </summary>
    public static class ActionRegistry
    {
        private static readonly Dictionary<Type, List<ActionMetadata>> _registeredActions =
            new Dictionary<Type, List<ActionMetadata>>();

        private static readonly object _lockObject = new object();
        private static bool _isInitialized = false;

        /// <summary>
        /// Represents metadata about a registered action.
        /// </summary>
        public class ActionMetadata
        {
            /// <summary>
            /// Gets the unique identifier for this action metadata.
            /// </summary>
            public string Id { get; set; }

            /// <summary>
            /// Gets the name of the action.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Gets the description of the action.
            /// </summary>
            public string Description { get; set; }

            /// <summary>
            /// Gets a value indicating whether the action is enabled.
            /// </summary>
            public bool IsEnabled { get; set; }

            /// <summary>
            /// Gets the type of the action (Create, Update, Delete, etc.).
            /// </summary>
            public ActionType ActionType { get; set; }

            /// <summary>
            /// Gets the entity type this action is associated with.
            /// </summary>
            public Type EntityType { get; set; }

            /// <summary>
            /// Gets the method information for this action.
            /// </summary>
            public MethodInfo Method { get; set; }

            /// <summary>
            /// Gets the class that contains this action method.
            /// </summary>
            public Type ActionClass { get; set; }

            /// <summary>
            /// Gets the execution order of this action (lower values execute first).
            /// </summary>
            public int ExecutionOrder { get; set; }

            /// <summary>
            /// Gets the timestamp when this action was registered.
            /// </summary>
            public DateTime RegisteredAt { get; set; }

            /// <summary>
            /// Gets or sets the instance of the action class.
            /// </summary>
            public object Instance { get; set; }
        }

        /// <summary>
        /// Initializes the action registry by scanning all assemblies for ActionAttribute.
        /// This method is called automatically on first use.
        /// </summary>
        public static void Initialize()
        {
            lock (_lockObject)
            {
                if (_isInitialized)
                    return;

                DiscoverAndRegisterActions();
                _isInitialized = true;
            }
        }

        /// <summary>
        /// Discovers and registers all actions in the application.
        /// Scans all types in the current AppDomain and looks for ActionAttribute.
        /// </summary>
        private static void DiscoverAndRegisterActions()
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                foreach (var assembly in assemblies)
                {
                    try
                    {
                        var types = assembly.GetTypes();
                        foreach (var type in types)
                        {
                            // Check if class has ActionAttribute
                            var classAttribute = type.GetCustomAttribute<ActionAttribute>();
                            if (classAttribute != null && classAttribute.IsEnabled)
                            {
                                RegisterActionsFromClass(type, classAttribute);
                            }

                            // Check methods for ActionAttribute
                            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                            foreach (var method in methods)
                            {
                                var methodAttribute = method.GetCustomAttribute<ActionAttribute>();
                                if (methodAttribute != null && methodAttribute.IsEnabled)
                                {
                                    RegisterActionFromMethod(type, method, methodAttribute);
                                }
                            }

                            // Check properties for ActionAttribute
                            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                            foreach (var property in properties)
                            {
                                var propertyAttribute = property.GetCustomAttribute<ActionAttribute>();
                                if (propertyAttribute != null && propertyAttribute.IsEnabled)
                                {
                                    // Properties can be registered as data actions
                                    RegisterActionFromProperty(type, property, propertyAttribute);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log exception for this assembly
                        System.Diagnostics.Debug.WriteLine($"Error scanning assembly {assembly.FullName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during action discovery: {ex.Message}");
            }
        }

        /// <summary>
        /// Registers actions from a class that has ActionAttribute.
        /// </summary>
        private static void RegisterActionsFromClass(Type classType, ActionAttribute attribute)
        {
            try
            {
                // Try to instantiate the class
                var instance = Activator.CreateInstance(classType);
                if (instance == null)
                    return;

                // Get the generic entity type from class inheritance
                var baseType = classType.BaseType;
                Type entityType = null;

                while (baseType != null)
                {
                    if (baseType.IsGenericType)
                    {
                        var genericArgs = baseType.GetGenericArguments();
                        if (genericArgs.Length > 0)
                        {
                            entityType = genericArgs[0];
                            break;
                        }
                    }
                    baseType = baseType.BaseType;
                }

                if (entityType == null)
                    entityType = typeof(object);

                var metadata = new ActionMetadata
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = attribute.Name ?? classType.Name,
                    Description = attribute.Description,
                    IsEnabled = attribute.IsEnabled,
                    ActionType = ActionType.Execute,
                    EntityType = entityType,
                    ActionClass = classType,
                    Instance = instance,
                    RegisteredAt = DateTime.UtcNow,
                    ExecutionOrder = 0
                };

                if (!_registeredActions.ContainsKey(entityType))
                {
                    _registeredActions[entityType] = new List<ActionMetadata>();
                }

                _registeredActions[entityType].Add(metadata);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error registering class {classType.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Registers an action from a method that has ActionAttribute.
        /// </summary>
        private static void RegisterActionFromMethod(Type classType, MethodInfo method, ActionAttribute attribute)
        {
            try
            {
                // Try to get entity type from method parameters or return type
                Type entityType = GetEntityTypeFromMethod(classType, method);
                if (entityType == null)
                    entityType = typeof(object);

                var metadata = new ActionMetadata
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = attribute.Name ?? method.Name,
                    Description = attribute.Description,
                    IsEnabled = attribute.IsEnabled,
                    ActionType = ActionType.Execute,
                    EntityType = entityType,
                    Method = method,
                    ActionClass = classType,
                    RegisteredAt = DateTime.UtcNow,
                    ExecutionOrder = 0
                };

                if (!_registeredActions.ContainsKey(entityType))
                {
                    _registeredActions[entityType] = new List<ActionMetadata>();
                }

                _registeredActions[entityType].Add(metadata);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error registering method {method.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Registers an action from a property that has ActionAttribute.
        /// </summary>
        private static void RegisterActionFromProperty(Type classType, PropertyInfo property, ActionAttribute attribute)
        {
            try
            {
                Type entityType = property.PropertyType;
                if (entityType == null)
                    entityType = typeof(object);

                var metadata = new ActionMetadata
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = attribute.Name ?? property.Name,
                    Description = attribute.Description,
                    IsEnabled = attribute.IsEnabled,
                    ActionType = ActionType.Execute,
                    EntityType = entityType,
                    ActionClass = classType,
                    RegisteredAt = DateTime.UtcNow,
                    ExecutionOrder = 0
                };

                if (!_registeredActions.ContainsKey(entityType))
                {
                    _registeredActions[entityType] = new List<ActionMetadata>();
                }

                _registeredActions[entityType].Add(metadata);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error registering property {property.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Extracts the entity type from a method's parameters or return type.
        /// </summary>
        private static Type GetEntityTypeFromMethod(Type classType, MethodInfo method)
        {
            var parameters = method.GetParameters();

            // Check first parameter
            if (parameters.Length > 0)
            {
                return parameters[0].ParameterType;
            }

            // Check return type
            if (method.ReturnType != typeof(void))
            {
                return method.ReturnType;
            }

            return null;
        }

        /// <summary>
        /// Gets all registered actions for a specific entity type.
        /// </summary>
        /// <param name="entityType">The entity type to get actions for</param>
        /// <returns>A list of action metadata for the entity type</returns>
        public static IReadOnlyList<ActionMetadata> GetActionsForEntity(Type entityType)
        {
            Initialize();

            lock (_lockObject)
            {
                if (_registeredActions.TryGetValue(entityType, out var actions))
                {
                    return actions
                        .Where(a => a.IsEnabled)
                        .OrderBy(a => a.ExecutionOrder)
                        .ToList()
                        .AsReadOnly();
                }

                return new List<ActionMetadata>().AsReadOnly();
            }
        }

        /// <summary>
        /// Gets all registered actions for a specific entity type and action type.
        /// </summary>
        /// <param name="entityType">The entity type to get actions for</param>
        /// <param name="actionType">The type of actions to retrieve</param>
        /// <returns>A list of action metadata matching the criteria</returns>
        public static IReadOnlyList<ActionMetadata> GetActionsForEntity(Type entityType, ActionType actionType)
        {
            Initialize();

            lock (_lockObject)
            {
                if (_registeredActions.TryGetValue(entityType, out var actions))
                {
                    return actions
                        .Where(a => a.IsEnabled && a.ActionType == actionType)
                        .OrderBy(a => a.ExecutionOrder)
                        .ToList()
                        .AsReadOnly();
                }

                return new List<ActionMetadata>().AsReadOnly();
            }
        }

        /// <summary>
        /// Manually registers an action metadata.
        /// </summary>
        /// <param name="metadata">The action metadata to register</param>
        public static void RegisterAction(ActionMetadata metadata)
        {
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            lock (_lockObject)
            {
                if (!_registeredActions.ContainsKey(metadata.EntityType))
                {
                    _registeredActions[metadata.EntityType] = new List<ActionMetadata>();
                }

                _registeredActions[metadata.EntityType].Add(metadata);
            }
        }

        /// <summary>
        /// Manually registers multiple action metadata.
        /// </summary>
        /// <param name="metadataList">The list of action metadata to register</param>
        public static void RegisterActions(IEnumerable<ActionMetadata> metadataList)
        {
            if (metadataList == null)
                throw new ArgumentNullException(nameof(metadataList));

            lock (_lockObject)
            {
                foreach (var metadata in metadataList.Where(m => m != null))
                {
                    RegisterAction(metadata);
                }
            }
        }

        /// <summary>
        /// Executes all registered actions for an entity.
        /// </summary>
        /// <typeparam name="TEntity">The type of entity</typeparam>
        /// <param name="entity">The entity to execute actions on</param>
        /// <returns>A task representing the asynchronous operation. Returns true if all actions executed successfully.</returns>
        public static async Task<bool> ExecuteActionsAsync<TEntity>(TEntity entity) where TEntity : class
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            Initialize();

            var actions = GetActionsForEntity(typeof(TEntity));
            bool allSuccessful = true;

            foreach (var action in actions)
            {
                try
                {
                    await ExecuteActionAsync(entity, action);
                }
                catch (Exception ex)
                {
                    allSuccessful = false;
                    System.Diagnostics.Debug.WriteLine($"Error executing action {action.Name}: {ex.Message}");
                }
            }

            return allSuccessful;
        }

        /// <summary>
        /// Executes a specific action on an entity.
        /// </summary>
        /// <typeparam name="TEntity">The type of entity</typeparam>
        /// <param name="entity">The entity to execute the action on</param>
        /// <param name="actionMetadata">The action metadata to execute</param>
        /// <returns>A task representing the asynchronous operation</returns>
        private static async Task ExecuteActionAsync<TEntity>(TEntity entity, ActionMetadata actionMetadata) where TEntity : class
        {
            try
            {
                if (actionMetadata.Method != null)
                {
                    // Execute method
                    var instance = actionMetadata.Instance ?? Activator.CreateInstance(actionMetadata.ActionClass);
                    var result = actionMetadata.Method.Invoke(instance, new object[] { entity });

                    // If result is a task, await it
                    if (result is Task task)
                    {
                        await task;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error executing action method: {ex.InnerException?.Message ?? ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets all registered actions across all entity types.
        /// </summary>
        /// <returns>A list of all registered actions</returns>
        public static IReadOnlyList<ActionMetadata> GetAllActions()
        {
            Initialize();

            lock (_lockObject)
            {
                return _registeredActions
                    .SelectMany(kvp => kvp.Value)
                    .Where(a => a.IsEnabled)
                    .OrderBy(a => a.RegisteredAt)
                    .ToList()
                    .AsReadOnly();
            }
        }

        /// <summary>
        /// Gets the count of registered actions for a specific entity type.
        /// </summary>
        /// <param name="entityType">The entity type</param>
        /// <returns>The count of registered actions</returns>
        public static int GetActionCount(Type entityType)
        {
            return GetActionsForEntity(entityType).Count;
        }

        /// <summary>
        /// Gets the total count of all registered actions.
        /// </summary>
        /// <returns>The total count of registered actions</returns>
        public static int GetTotalActionCount()
        {
            Initialize();

            lock (_lockObject)
            {
                return _registeredActions.Sum(kvp => kvp.Value.Count(a => a.IsEnabled));
            }
        }

        /// <summary>
        /// Clears all registered actions.
        /// </summary>
        public static void Clear()
        {
            lock (_lockObject)
            {
                _registeredActions.Clear();
                _isInitialized = false;
            }
        }

        /// <summary>
        /// Resets the registry and re-initializes it.
        /// </summary>
        public static void Reset()
        {
            Clear();
            Initialize();
        }
    }
}
