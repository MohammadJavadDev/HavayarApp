using Common;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using System.Reflection;
using WebFramework.Services;

namespace WebFramework.Services;

public interface IDynamicControllerRegistrar
{
	void Register(long formDefinitionId, Assembly controllerAssembly, IEnumerable<string>? compilationReferencePaths = null);
	void Unregister(long formDefinitionId);
}

public sealed class DynamicControllerRegistrar : IDynamicControllerRegistrar, ISingletonDependency
{
	private readonly ApplicationPartManager _partManager;
	private readonly DynamicActionDescriptorChangeProvider _changeProvider;
	private readonly Dictionary<long, ApplicationPart> _parts = new();
	private readonly object _lock = new();

	public DynamicControllerRegistrar(
		ApplicationPartManager partManager,
		DynamicActionDescriptorChangeProvider changeProvider)
	{
		_partManager = partManager;
		_changeProvider = changeProvider;
	}

	public void Register(long formDefinitionId, Assembly controllerAssembly, IEnumerable<string>? compilationReferencePaths = null)
	{
		lock (_lock)
		{
			if (_parts.TryGetValue(formDefinitionId, out var existingPart))
				_partManager.ApplicationParts.Remove(existingPart);

			var referencePaths = compilationReferencePaths?
				.Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToArray() ?? [];

			var part = string.IsNullOrWhiteSpace(controllerAssembly.Location)
				? new RuntimeCompiledControllerPart(controllerAssembly, referencePaths)
				: new AssemblyPart(controllerAssembly);
			_partManager.ApplicationParts.Add(part);
			_parts[formDefinitionId] = part;
		}

		_changeProvider.NotifyChanges();
	}

	public void Unregister(long formDefinitionId)
	{
		lock (_lock)
		{
			if (_parts.TryGetValue(formDefinitionId, out var part))
			{
				_partManager.ApplicationParts.Remove(part);
				_parts.Remove(formDefinitionId);
			}
		}

		_changeProvider.NotifyChanges();
	}

	private sealed class RuntimeCompiledControllerPart(Assembly assembly, IReadOnlyCollection<string> referencePaths)
		: AssemblyPart(assembly), ICompilationReferencesProvider
	{
		public IEnumerable<string> GetReferencePaths()
		{
			return referencePaths;
		}
	}
}
