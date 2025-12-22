using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HavayarApp.Data.Actions
{
    /// <summary>
    /// Generic class for managing entity actions with type-safe operations.
    /// Provides functionality for executing, tracking, and managing actions on entities.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity that this action manager operates on</typeparam>
    public class EntityActionManager<TEntity> where TEntity : class
    {
        private readonly List<IEntityAction<TEntity>> _actions;
        private readonly List<IEntityAction<TEntity>> _executedActions;
        private readonly List<IEntityAction<TEntity>> _failedActions;

        /// <summary>
        /// Initializes a new instance of the EntityActionManager class.
        /// </summary>
        public EntityActionManager()
        {
            _actions = new List<IEntityAction<TEntity>>();
            _executedActions = new List<IEntityAction<TEntity>>();
            _failedActions = new List<IEntityAction<TEntity>>();
        }

        /// <summary>
        /// Gets the count of pending actions.
        /// </summary>
        public int PendingActionCount => _actions.Count;

        /// <summary>
        /// Gets the count of executed actions.
        /// </summary>
        public int ExecutedActionCount => _executedActions.Count;

        /// <summary>
        /// Gets the count of failed actions.
        /// </summary>
        public int FailedActionCount => _failedActions.Count;

        /// <summary>
        /// Registers an action to be executed.
        /// </summary>
        /// <param name="action">The action to register</param>
        public void RegisterAction(IEntityAction<TEntity> action)
        {
            if (action != null)
            {
                _actions.Add(action);
            }
        }

        /// <summary>
        /// Registers multiple actions to be executed.
        /// </summary>
        /// <param name="actions">The actions to register</param>
        public void RegisterActions(IEnumerable<IEntityAction<TEntity>> actions)
        {
            if (actions != null)
            {
                _actions.AddRange(actions.Where(a => a != null));
            }
        }

        /// <summary>
        /// Executes all registered actions sequentially.
        /// </summary>
        /// <param name="entity">The entity to execute actions on</param>
        /// <returns>True if all actions executed successfully, false otherwise</returns>
        public async Task<bool> ExecuteAllActionsAsync(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            bool allSuccessful = true;

            foreach (var action in _actions.ToList())
            {
                try
                {
                    await action.ExecuteAsync(entity);
                    _executedActions.Add(action);
                    _actions.Remove(action);
                }
                catch (Exception)
                {
                    allSuccessful = false;
                    _failedActions.Add(action);
                    _actions.Remove(action);
                }
            }

            return allSuccessful;
        }

        /// <summary>
        /// Executes a single registered action.
        /// </summary>
        /// <param name="entity">The entity to execute the action on</param>
        /// <param name="actionIndex">The index of the action to execute</param>
        /// <returns>True if the action executed successfully, false otherwise</returns>
        public async Task<bool> ExecuteActionAsync(TEntity entity, int actionIndex)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (actionIndex < 0 || actionIndex >= _actions.Count)
                throw new IndexOutOfRangeException($"Action index {actionIndex} is out of range");

            var action = _actions[actionIndex];

            try
            {
                await action.ExecuteAsync(entity);
                _executedActions.Add(action);
                _actions.RemoveAt(actionIndex);
                return true;
            }
            catch (Exception)
            {
                _failedActions.Add(action);
                _actions.RemoveAt(actionIndex);
                return false;
            }
        }

        /// <summary>
        /// Gets all pending actions.
        /// </summary>
        /// <returns>A list of pending actions</returns>
        public IReadOnlyList<IEntityAction<TEntity>> GetPendingActions()
        {
            return _actions.AsReadOnly();
        }

        /// <summary>
        /// Gets all executed actions.
        /// </summary>
        /// <returns>A list of executed actions</returns>
        public IReadOnlyList<IEntityAction<TEntity>> GetExecutedActions()
        {
            return _executedActions.AsReadOnly();
        }

        /// <summary>
        /// Gets all failed actions.
        /// </summary>
        /// <returns>A list of failed actions</returns>
        public IReadOnlyList<IEntityAction<TEntity>> GetFailedActions()
        {
            return _failedActions.AsReadOnly();
        }

        /// <summary>
        /// Clears all pending actions.
        /// </summary>
        public void ClearPendingActions()
        {
            _actions.Clear();
        }

        /// <summary>
        /// Clears all action history (executed and failed actions).
        /// </summary>
        public void ClearActionHistory()
        {
            _executedActions.Clear();
            _failedActions.Clear();
        }

        /// <summary>
        /// Resets the action manager to its initial state.
        /// </summary>
        public void Reset()
        {
            _actions.Clear();
            _executedActions.Clear();
            _failedActions.Clear();
        }
    }

    /// <summary>
    /// Interface for entity actions that can be executed on an entity.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity the action operates on</typeparam>
    public interface IEntityAction<TEntity> where TEntity : class
    {
        /// <summary>
        /// Gets the name or description of the action.
        /// </summary>
        string ActionName { get; }

        /// <summary>
        /// Executes the action on the specified entity.
        /// </summary>
        /// <param name="entity">The entity to execute the action on</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task ExecuteAsync(TEntity entity);
    }
}
