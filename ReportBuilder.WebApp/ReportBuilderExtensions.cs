using System.Reflection;
using Data.Contracts;
using Data.Repositories;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using ReportBuilder.Services.Contracts;
using ReportBuilder.Services.Repositories;
using ReportBuilder.WebApp.Controllers;


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

            services.Configure<MvcRazorRuntimeCompilationOptions>(options =>
                { options.FileProviders.Add(new EmbeddedFileProvider(assembly)); });


        }
    }
}
