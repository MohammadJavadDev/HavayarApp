using Data.Contracts.Actions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Data.Actions
{
	public class EntityActionDefinition
	{
		public MethodInfo Method { get; set; }
		public Type DeclaringType { get; set; }
		public EntityActionAttribute Metadata { get; set; }
	}

	public interface IEntityActionRegistry
	{
		IReadOnlyList<EntityActionDefinition> GetActions(Type entityType, EntityActionTrigger trigger);
	}

	public class EntityActionRegistry : IEntityActionRegistry
	{
		private readonly Dictionary<(Type, EntityActionTrigger), List<EntityActionDefinition>> _cache = new();

		public EntityActionRegistry(params Assembly[] assemblies)
		{
			foreach (var assembly in assemblies)
			{
				var methods = assembly.GetTypes()
				    .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
				    .Select(m => new { Method = m, Attr = m.GetCustomAttribute<EntityActionAttribute>() })
				    .Where(x => x.Attr != null);

				foreach (var item in methods)
				{
					var key = (item.Attr!.EntityType, item.Attr.Trigger);
					if (!_cache.ContainsKey(key)) _cache[key] = new List<EntityActionDefinition>();

					_cache[key].Add(new EntityActionDefinition
					{
						Method = item.Method,
						DeclaringType = item.Method.DeclaringType!,
						Metadata = item.Attr
					});
				}
			}

			// مرتب‌سازی بر اساس اولویت در زمان لود اولیه
			foreach (var key in _cache.Keys)
			{
				_cache[key] = _cache[key].OrderBy(x => x.Metadata.Priority).ToList();
			}
		}

		public IReadOnlyList<EntityActionDefinition> GetActions(Type entityType, EntityActionTrigger trigger)
		{
			return _cache.TryGetValue((entityType, trigger), out var actions) ? actions : new List<EntityActionDefinition>();
		}
	}
}
