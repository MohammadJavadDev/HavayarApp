using Data.Contracts;
using Data.Repositories;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using RB.WebApp;
using ReportBuilder.Services.Contracts;
using ReportBuilder.Services.Repositories;
using ReportBuilder.WebApp.Controllers;
using Stimulsoft.Report.Dictionary;
using System.Reflection;


namespace ReportBuilder.WebApp
{
    public static class ReportBuilderExtensions
    {

        public static void ConfigureProfilesModule(this IServiceCollection services)
        {
            var assembly = typeof(ReportBuilderController).GetTypeInfo().Assembly;

            services.AddScoped<IReportBuilderService, ReportBuilderService>();
            services.AddControllersWithViews()
                .AddTagHelpersAsServices()
                .AddApplicationPart(assembly);


			var ParamNames = new string[2];
			var ParamTypes = new Type[2];
			var ParamDescriptions = new string[2];

			ParamNames[0] = "json";
			ParamDescriptions[0] = "Json string";
			ParamTypes[0] = typeof(string);

			ParamNames[1] = "path";
			ParamDescriptions[1] = "Json path";
			ParamTypes[1] = typeof(string);

			StiFunctions.AddFunction(
			    "Custom",                      // Category در Designer
			    "Json",                        // Name
			    "Json",                        // Alias
			    "Get value from json",         // Description
			    typeof(ReportCustomFunctions), // کلاس حاوی متد
			    typeof(string),                // نوع خروجی تابع
			    "Json value as string",        // توضیح خروجی
			    ParamTypes,                    // آرایه Type پارامترها
			    ParamNames,                    // آرایه Name پارامترها
			    ParamDescriptions              // آرایه Description پارامترها
			);

			  ParamNames = new string[2];
			  ParamTypes = new Type[2];
			  ParamDescriptions = new string[2];

			// param 1: enum name
			ParamNames[0] = "enumName";
			ParamDescriptions[0] = "Name of Enum type";
			ParamTypes[0] = typeof(string);

			// param 2: enum numeric value
			ParamNames[1] = "value";
			ParamDescriptions[1] = "Numeric value of enum";
			ParamTypes[1] = typeof(object);

			StiFunctions.AddFunction(
			    "Custom",                                // Category
			    "EnumDisplayName",                       // Name
			    "EnumDisplayName",                       // Alias
			    "Gets DisplayAttribute.Name of an enum", // Description
			    typeof(ReportCustomFunctions),           // Class
			    typeof(string),                          // Return type
			    "Display Name of enum",                  // Return description
			    ParamTypes,                              // Param types
			    ParamNames,                              // Param names
			    ParamDescriptions                        // Param descriptions
			);




		}
	}
}
