using Data;
using Data.Actions;
using Data.Contracts;
using Data.Repositories;
using Data.Services;
using Data.Services.QueryBuilderServices;
using Data.SystemAuth;
using Entities.Auth;
using Entities.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Services.Auth;
 using Services.FileServices;
using Services.Job;
 using System.Text;
using WebFramework.Services;
using Z.EntityFramework.Extensions;

try
{
	var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("db"), sql =>
    {
	    sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
    }) );

builder.Services.AddDbContext<RahkaranDbContext>((sp, options) =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Rahkaran"), sql =>
    {
	    sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
    }));

builder.Services.AddDbContext<HtsDbContext>((sp, options) =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Hts"), sql =>
    {
	 sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
    }));

builder.Services.AddScoped(typeof(IUnitOfWork), typeof(UnitOfWork));
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped(typeof(IEntityRepository), typeof(EntityRepository));


builder.Services.AddSingleton<IEntityMetadataCache, RedisEntityMetadataCache>();
builder.Services.AddSingleton<EntityMetadataCache>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IOnlineUserService, RedisOnlineUserService>();


	builder.Services.AddSingleton<DynamicActionDescriptorChangeProvider>();
	builder.Services.AddSingleton<Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorChangeProvider>(
		sp => sp.GetRequiredService<DynamicActionDescriptorChangeProvider>());
	builder.Services.AddSingleton<IDynamicTypeRegistry, Services.DynamicCompilation.DynamicTypeRegistry>();
	builder.Services.AddSingleton<Services.DynamicCompilation.IDynamicCompilationService, Services.DynamicCompilation.DynamicCompilationService>();
	builder.Services.AddSingleton<Services.DynamicCompilation.IFormDefinitionMetadataBuilder, Services.DynamicCompilation.FormDefinitionMetadataBuilder>();
	builder.Services.AddSingleton<IDynamicControllerRegistrar, DynamicControllerRegistrar>();
	builder.Services.AddSingleton<Microsoft.AspNetCore.Mvc.Abstractions.IActionDescriptorProvider, ExcludeShadowedFormControllersProvider>();
	builder.Services.AddSingleton<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory, DynamicModelCacheKeyFactory>();

builder.Services.AddHostedService<JobDiscoveryService>();
builder.Services.AddHostedService<JobWorkerWithLogging>();

builder.Services.AddScoped<IFileService, FileService>();
 
	builder.Services.AddScoped<IQueryBuilderService, QueryBuilderService>();

	builder.Services.AddEntityActions(
	typeof(Program).Assembly
	);

builder.Services.AddScoped<ISdk, Sdk>();
builder.Services.AddScoped<IQueryService, QueryService>();
builder.Services.AddScoped<IDataTableQueryBuilder, DataTableQueryBuilder>();
builder.Services.AddScoped<IDatabaseSchemaService, DatabaseSchemaService>();
builder.Services.AddScoped<IParameterResolverService, ParameterResolverService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddSingleton<IDataTableProfileService, DataTableProfileService>();
builder.Services.AddJobServices();

// Register JobLogger service
builder.Services.AddScoped<IJobLogger, JobLogger>();

// Realtime: replace NoOp with SignalR notifier (single-instance app, no Redis backplane)
builder.Services.AddSignalR();
builder.Services.RemoveAll<IJobRealtimeNotifier>();
builder.Services.AddSingleton<IJobRealtimeNotifier, App.BackgroundJob.Services.SignalRJobRealtimeNotifier>();

// Register Authentication services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IActiveDirectoryService, ActiveDirectoryService>();

// Configure JWT Authentication
builder.Services
    .AddAuthentication(x =>
    {
        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = AppSettings.Issuer,
            ValidAudience = AppSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AppSettings.PrivateKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                else if (string.IsNullOrEmpty(context.Token) &&
                         context.Request.Cookies.TryGetValue("JwtToken", out var cookieToken))
                {
                    context.Token = cookieToken;
                }

                return Task.CompletedTask;
            }
        };
    });

// Configure Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AuthenticatedUser", policy =>
    {
        policy.RequireAuthenticatedUser();
    });
    options.AddPolicy("admin", policy =>
    {
        policy.RequireRole("admin");
    });
});

 

	var app = builder.Build();

	// Configure the HTTP request pipeline.
	if (!app.Environment.IsDevelopment())
	{
		app.UseExceptionHandler("/Home/Error");
		app.UseHsts();
	}



	app.UseHttpsRedirection();
	app.UseStaticFiles();

	app.UseRouting();

	// Middleware to read JWT from cookie and add to Authorization header
	app.Use(async (context, next) =>
	{
		if (context.Request.Cookies.TryGetValue("JwtToken", out var token))
		{
			context.Request.Headers["Authorization"] = $"Bearer {token}";
		}
		await next();
	});

	// Redirect unauthorized users to login
	app.UseStatusCodePages(async context =>
	{
		if (context.HttpContext.Response.StatusCode == 401)
		{
			context.HttpContext.Response.Redirect("/Authenticate/Login");
		}
		if (context.HttpContext.Response.StatusCode == 403)
		{
			context.HttpContext.Response.Redirect("/Authenticate/Login");
		}
		await Task.CompletedTask;
	});

	app.UseAuthentication();
	app.UseAuthorization();


	InitializeApplication(app);

	app.MapHub<App.BackgroundJob.Hubs.JobMonitorHub>("/hubs/jobs");

	app.MapControllerRoute(
	    name: "default",
	    pattern: "{controller=Jobs}/{action=Index}");

	app.Run();

}
catch (Exception ex)
{
	File.WriteAllText("startup-crash.log", ex.ToString());
	throw;
}


void InitializeApplication(WebApplication app)
{
	using (var scope = app.Services.CreateScope())
	{
		var applicationDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		applicationDbContext.Database.Migrate();

		string licenseName = "134;100-DOWNLOADDEVTOOLS.COM";
		string licenseKey = "1519351-28861E0-148651C-25E14B3-9428";

		LicenseManager.AddLicense(licenseName, licenseKey);

		if (!LicenseManager.ValidateLicense(out string licenseErrorMessage))
		{
			throw new Exception(licenseErrorMessage);
		}

	}
}