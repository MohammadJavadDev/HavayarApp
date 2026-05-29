using Data.Contracts.Actions;
using Data.Services.Actions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Data.Actions
{
	public static class EntityActionExtensions
	{
		public static void AddEntityActions(this IServiceCollection services, params Assembly[] assemblies)
		{
			 
			services.AddSingleton<IEntityActionRegistry>(new EntityActionRegistry(assemblies));
			services.AddScoped<IEntityActionInvoker, EntityActionInvoker>();


			foreach (Assembly assembly in assemblies) {

				var actionClasses = assembly.GetTypes()
				    .Where(t => t.GetMethods().Any(m => m.GetCustomAttribute<EntityActionAttribute>() != null));

				foreach (var type in actionClasses)
				{
					services.AddScoped(type);
				}

			}

			
		}
	}
}
