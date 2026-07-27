using Common;
using Entities.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace WebFramework.Services;

/// <summary>
/// When a form is published via Roslyn hot-load, the same controller may also exist in the WebApp assembly
/// (from a previous code generation). This provider removes the static endpoints so routing stays unambiguous.
/// </summary>
public sealed class ExcludeShadowedFormControllersProvider(IDynamicTypeRegistry typeRegistry)
	: IActionDescriptorProvider, ISingletonDependency
{
	public int Order => 1000;

	public void OnProvidersExecuting(ActionDescriptorProviderContext context)
	{
	}

	public void OnProvidersExecuted(ActionDescriptorProviderContext context)
	{
		var registrations = typeRegistry.GetAllRegistrations();
		if (registrations.Count == 0)
			return;

		var dynamicAssemblies = registrations
			.Select(r => r.ControllerAssembly)
			.ToHashSet();

		var shadowedControllerNames = registrations
			.Select(r => $"{r.EntityName}Controller")
			.ToHashSet(StringComparer.Ordinal);

		for (var i = context.Results.Count - 1; i >= 0; i--)
		{
			if (ShouldExclude(context.Results[i], shadowedControllerNames, dynamicAssemblies))
				context.Results.RemoveAt(i);
		}
	}

	private static bool ShouldExclude(
		ActionDescriptor descriptor,
		HashSet<string> shadowedControllerNames,
		HashSet<System.Reflection.Assembly> dynamicAssemblies)
	{
		if (descriptor is not ControllerActionDescriptor controllerAction)
			return false;

		if (!shadowedControllerNames.Contains(controllerAction.ControllerName))
			return false;

		var assembly = controllerAction.ControllerTypeInfo.Assembly;
		return !dynamicAssemblies.Contains(assembly);
	}
}
