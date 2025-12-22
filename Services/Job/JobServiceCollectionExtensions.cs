using Common.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Services.Job
{
	public static class JobServiceCollectionExtensions
	{
		public static IServiceCollection AddJobServices(this IServiceCollection services, params Assembly[] assemblies)
		{
			// اگر اسمبلی خاصی پاس داده نشد، اسمبلی اجرایی برنامه را در نظر بگیر
			if (assemblies.Length == 0)
			{
				assemblies = new[] { Assembly.GetEntryAssembly() };
			}

			// پیدا کردن تمام کلاس‌هایی که حداقل یک متد با اتریبیوت JobHandler دارند
			var jobClasses = assemblies
			    .SelectMany(a => a.GetTypes())
			    .Where(t => t.IsClass && !t.IsAbstract)
			    .Where(t => t.GetMethods().Any(m => m.GetCustomAttributes(typeof(JobHandlerAttribute), false).Length > 0))
			    .ToList();

			foreach (var jobClass in jobClasses)
			{
				// ثبت کلاس در DI (معمولا Scoped بهترین گزینه برای جاب‌هاست)
				services.AddScoped(jobClass);
			}

			return services;
		}
	}
}
