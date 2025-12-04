
using System.Reflection;
using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Entities.Auth;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
 

namespace Services.AccessServices
{
    public class EndpointService(IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider)
    {

        public List<AccessController> GetAllEndpoints()
        {
            var controllers = new List<AccessController>();

            var actionDescriptors = _actionDescriptorCollectionProvider.ActionDescriptors.Items;

            // Group actions by controller name
            var groupedByController = actionDescriptors
                .OfType<ControllerActionDescriptor>()
                .GroupBy(ad => ad.ControllerName);

            foreach (var controllerGroup in groupedByController)
            {
                var firstAction = controllerGroup.FirstOrDefault();
                if (firstAction == null) continue;

                var controllerType = firstAction.ControllerTypeInfo;
                var ControllerInfoAttribute = controllerType.GetCustomAttribute<ControllerInfoAttribute>()?.Name;

                if (!ControllerInfoAttribute.HasValue(true)) continue;

                var entityType = controllerType.GetCustomAttribute<ControllerInfoAttribute>()?.EntityType;

                var controller = new AccessController
                {
                    Name = controllerGroup.Key,
				 DisplayName = ControllerInfoAttribute,
                    Path = "", // Path can be set dynamically based on routing rules,
                    EntityType = entityType,

				Actions = new List<AccessAction>()
                };

                // Add actions to the controller
                foreach (var actionDescriptor in controllerGroup)
                {
                    var methodInfo = (actionDescriptor as ControllerActionDescriptor)?.MethodInfo;

                    // Get the custom display name for the action, if exists
                    var actionDisplayName = methodInfo?.GetCustomAttribute<ActionDisplayNameAttribute>()?.Name;

                    if (!actionDisplayName.HasValue(true)) continue;

                    var actionAccessType = methodInfo?.GetCustomAttribute<ActionDisplayNameAttribute>()?.Type
                                            ?? ActionAccessType.Other;
				 var actionAccessItemType = methodInfo?.GetCustomAttribute<ActionDisplayNameAttribute>()?.ActionAccessItemType
								    ?? ActionAccessItemType.Custom;
					var action = new AccessAction
                    {
                        Name = actionDescriptor.ActionName,
					DisplayName = actionDisplayName, // Action method name as title
                        Path = $"/{actionDescriptor.AttributeRouteInfo.Template.ToLower()}".Replace("{action}", actionDescriptor.ActionName).Replace("[action]", actionDescriptor.ActionName), // Route to the action
                        ActionAccessType = actionAccessType,
                        AccessController = controller,
				    ActionAccessItemType = actionAccessItemType
					};
                    controller.Actions.Add(action);
                }

                controllers.Add(controller);
            }

            return controllers;

        }
    }
}
