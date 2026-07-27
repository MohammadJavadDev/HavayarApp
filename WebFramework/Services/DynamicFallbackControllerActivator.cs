using Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace WebFramework.Services;

/// <summary>
/// Supports both statically registered controllers (AddControllersAsServices) and
/// Roslyn hot-loaded controllers that are not in the DI container.
/// </summary>
public sealed class DynamicFallbackControllerActivator : IControllerActivator, ISingletonDependency
{
	public object Create(ControllerContext context)
	{
		var controllerType = context.ActionDescriptor.ControllerTypeInfo.AsType();
		var requestServices = context.HttpContext.RequestServices;

		return requestServices.GetService(controllerType)
			?? ActivatorUtilities.CreateInstance(requestServices, controllerType);
	}

	public void Release(ControllerContext context, object controller)
	{
		if (context.HttpContext.RequestServices.GetService(controller.GetType()) != null)
			return;

		if (controller is IDisposable disposable)
			disposable.Dispose();
	}
}
