using Common.System;
using Data;
using Data.Contracts;
using Data.Repositories;
using Data.Services;
using Data.Services.Edms;
using Data.Services.Eng.CompressorSizing;
using Data.Services.Pln;
using Data.Services.QueryBuilderServices;
using Data.SystemAuth;
using Entities.Auth;
using Entities.Services;
using Infrastructure.CrudEventInterceptors;
using Infrastructure.Messaging;
using Infrastructure.NotificationServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
 
using ReportBuilder.WebApp;
 
using ReportBuilder.WebApp.Controllers;
using Services;
using Services.AccessServices;
using Services.Auth;
using Services.FileServices;
using Services.ImportDefinitionServices;
using Services.InMemoryData;
using Services.NotificationGroupServices;
using Services.NotificationServices;
using Services.NotifitactionBuilderServices;
using Data.Actions;
 
using Shared.Realtime.Options;
using System.IO.Compression;
using System.Text;
using WebApp.Services;
using WebApp.Services.Realtime;
using WebFramework.Abstractions;
using WebFramework.Initializes;
using WebFramework.Middlewares;
using WebFramework.Services;
using WebFramework.TagHelpers;
using WebFramework.Views;
using Z.EntityFramework.Extensions;


var builder = WebApplication.CreateBuilder(args);
 
 
builder.Services.AddControllersWithViews()

    .AddNewtonsoftJson(o =>
    {
        o.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        o.SerializerSettings.Formatting = Formatting.Indented;
        o.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

    }).AddMvcOptions(o =>
    {
        o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    })
    .AddRazorRuntimeCompilation()
    .AddApplicationPart(typeof(WebFramework.Controllers.DataTableProfileBuilderController).Assembly)
	 .AddApplicationPart(typeof(ReportBuilderController).Assembly)
    .AddControllersAsServices();

builder.Services.AddSingleton<Microsoft.AspNetCore.Mvc.Controllers.IControllerActivator, DynamicFallbackControllerActivator>();

 

builder.Services.AddScoped<CrudEventInterceptor>();

builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("db"), sql =>
    {
        sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
    }).AddInterceptors(sp.GetRequiredService<CrudEventInterceptor>()));

builder.Services.AddDbContext<RahkaranDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Rahkaran")));

builder.Services.AddDbContext<HtsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Hts")));


builder.Services.AddSingleton<IEnvironmentService, EnvironmentService>();


builder.Services.AddScoped(typeof(IUnitOfWork), typeof(UnitOfWork));
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped(typeof(IEntityRepository), typeof(EntityRepository));

builder.Services.AddEntityActions(
	typeof(Program).Assembly
	);

builder.Services.AddScoped(typeof(INotificationService), typeof(NotificationService));

builder.Services.AddTransient<IAuthService, AuthService>();

builder.Services.AddScoped<IPropertyIdentityService, PropertyIdentityService>();
builder.Services.AddScoped<IDataTableQueryBuilder, DataTableQueryBuilder>();
builder.Services.AddSingleton<EndpointService>();

builder.Services.AddScoped<IInitializeProgram, InitializeProgram>();
builder.Services.AddScoped<IInitializeDataBase, InitializeDataBase>();

builder.Services.AddScoped<IEntityHistoryRepository, EntityHistoryRepository>();

builder.Services.AddScoped<INotifitactionBuilderService, NotifitactionBuilderService>();
builder.Services.AddSingleton<INotificationRuleCache, NotificationRuleCache>();

builder.Services.AddSingleton<IAccessMemoryStorage, AccessMemoryStorage>();
 

// Redis-backed caches
builder.Services.AddSingleton<IRoleMemoryStorage, RedisRoleMemoryStorage>();

builder.Services.AddSingleton<IEntityMetadataCache, RedisEntityMetadataCache>();
builder.Services.AddSingleton<EntityMetadataCache>();
builder.Services.AddSingleton<IMenuBuilderService, MenuBuilderService>();
builder.Services.AddSingleton<IDataTableProfileService, DataTableProfileService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<ISdk, Sdk>();
builder.Services.AddScoped<IFormBuilderCodeGenerator,FormBuilderCodeGenerator>();
builder.Services.AddScoped<IFormBuilderSchemaService, FormBuilderSchemaService>();
builder.Services.AddScoped<IFormBuilderPublishService, FormBuilderPublishService>();
builder.Services.AddScoped<IPageBuilderSchemaService, PageBuilderSchemaService>();
builder.Services.AddScoped<IPageBuilderService, PageBuilderService>();
builder.Services.AddScoped<IPageBuilderViewRenderService, PageBuilderViewRenderService>();
builder.Services.AddSingleton<IPageBuilderCache, PageBuilderCache>();


//builder.Services.AddSingleton<DiskThenDatabaseFileProvider>();
//builder.Services.AddOptions<Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation.MvcRazorRuntimeCompilationOptions>()
//	.Configure<DiskThenDatabaseFileProvider>((options, provider) =>
//	{
//		options.FileProviders.Remove(provider);
//		options.FileProviders.Insert(0, provider);
//	});

builder.Services.AddSingleton<DynamicActionDescriptorChangeProvider>();
builder.Services.AddSingleton<Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorChangeProvider>(
	sp => sp.GetRequiredService<DynamicActionDescriptorChangeProvider>());
builder.Services.AddSingleton<IDynamicTypeRegistry, Services.DynamicCompilation.DynamicTypeRegistry>();
builder.Services.AddSingleton<Services.DynamicCompilation.IDynamicCompilationService, Services.DynamicCompilation.DynamicCompilationService>();
builder.Services.AddSingleton<Services.DynamicCompilation.IFormDefinitionMetadataBuilder, Services.DynamicCompilation.FormDefinitionMetadataBuilder>();
builder.Services.AddSingleton<IDynamicControllerRegistrar, DynamicControllerRegistrar>();
builder.Services.AddSingleton<Microsoft.AspNetCore.Mvc.Abstractions.IActionDescriptorProvider, ExcludeShadowedFormControllersProvider>();
builder.Services.AddSingleton<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory, DynamicModelCacheKeyFactory>();
builder.Services.AddScoped<IDatabaseSchemaService, DatabaseSchemaService>();
builder.Services.AddHttpClient();
// Redis Distributed Cache

var redisConnection = builder.Configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>()?.ConnectionString ?? "localhost:6379";
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnection;
    options.InstanceName = "HavayarApp:";
});

builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection(RedisOptions.SectionName));
builder.Services.ConfigureProfilesModule();


 
builder.Services.AddTransient<DataTableProfileTagHelper>();
builder.Services.AddTransient<FormActionButtons>();
builder.Services.AddTransient<FormActionButton>();
 
builder.Services.AddTransient<FileUploaderTagHelper>();

builder.Services.AddScoped<IEntitySelectorService, EntitySelectorService>();
builder.Services.AddDataProtection();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<PayloadProtector>();
builder.Services.AddSingleton<SqlCacheStore>(); // The Bridge
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddSingleton<IOnlineUserService, RedisOnlineUserService>();
// Realtime connection token options and service
builder.Services.Configure<ConnectionTokenOptions>(builder.Configuration.GetSection(ConnectionTokenOptions.SectionName));
builder.Services.AddSingleton<IConnectionTokenService, ConnectionTokenService>();
builder.Services.Configure<RabbitMQOptions>(builder.Configuration.GetSection(RabbitMQOptions.SectionName));
builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
builder.Services.AddSingleton<INotificationEventPublisher, NotificationEventPublisher>();
builder.Services.AddSingleton<ICrudEventPublisher, CrudEventPublisher>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<INotificationGroupService, NotificationGroupService>();

builder.Services.AddScoped<IImportRepository, ImportRepository>();
builder.Services.AddSingleton<ImportApiEndpointInspector>();

 
   

//QueryBuilder Services 

// سرویس Cache دار
builder.Services.AddScoped<IDatabaseSchemaService, CachedDatabaseSchemaService>();

builder.Services.AddScoped<DatabaseSchemaService>();

// سرویس ساخت Query
builder.Services.AddScoped<IQueryBuilderService, QueryBuilderService>();

// سرویس مدیریت گزارش‌ها
builder.Services.AddScoped<IQueryService, QueryService>();

builder.Services.AddScoped<IEdmsMdrReportService, EdmsMdrReportService>();

// گزارش انحراف از تحویل به‌موقع سفارش ساخت
builder.Services.AddScoped<ITimelyDeliveryReportService, TimelyDeliveryReportService>();

builder.Services.AddSingleton<ICompressorSizingService>(sp =>
{
	var env = sp.GetRequiredService<IWebHostEnvironment>();
	var config = sp.GetRequiredService<IConfiguration>();
	var directories = new List<string>();

	var configured = config["CompressorSizing:DataDirectory"];
	if (!string.IsNullOrWhiteSpace(configured))
	{
		directories.Add(Path.IsPathRooted(configured)
			? configured
			: Path.Combine(env.ContentRootPath, configured));
	}

	directories.Add(Path.Combine(env.ContentRootPath, "App_Data", "SizingExcels"));
	if (!string.IsNullOrWhiteSpace(env.WebRootPath))
		directories.Add(Path.Combine(env.WebRootPath, "SizingExcels"));

	var uploads = config["Storage:UploadsPath"];
	if (!string.IsNullOrWhiteSpace(uploads))
	{
		var uploadsFull = Path.IsPathRooted(uploads) ? uploads : Path.Combine(env.ContentRootPath, uploads);
		directories.Add(Path.Combine(uploadsFull, "SizingExcels"));
	}

	directories.Add(@"E:\Uploads\SizingExcels");
	return new CompressorSizingService(directories);
});

// سرویس جایگزینی پارامترها
builder.Services.AddScoped<IParameterResolverService, ParameterResolverService>();

 
builder.Services.AddScoped<IActiveDirectoryService, ActiveDirectoryService>();
 
builder.Services.AddSingleton<SqlQueryValidator>();

builder.Services.AddDistributedMemoryCache();

builder.Services.ConfigureProfilesModule();



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
    });

builder.Services.AddResponseCompression(options =>
{
	options.EnableForHttps = true;
	options.Providers.Add<BrotliCompressionProvider>();
	options.Providers.Add<GzipCompressionProvider>();
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
	options.Level = CompressionLevel.Optimal;
});


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
    options.AddPolicy("User", policy =>
    {
        policy.RequireRole("User");
    });
});

// SignalR with Redis backplane (for scale-out)
var redisOptions = builder.Configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>();
if (redisOptions?.EnableSignalRBackplane == true)
{
    builder.Services.AddSignalR()
        .AddStackExchangeRedis(redisOptions.ConnectionString, options =>
        {
            options.Configuration.ChannelPrefix = "HavayarApp.SignalR";
        });
}
else
{
    builder.Services.AddSignalR();
}

builder.Services.AddHealthChecks();


var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseCustomExceptionHandler();

app.UseResponseCompression();
app.UseStaticFiles(new StaticFileOptions
{
	OnPrepareResponse = ctx =>
	{
		var file = ctx.File;
		var headers = ctx.Context.Response.Headers;
           
		headers.CacheControl = "public,max-age=31536000";
	}
});

app.UseHttpsRedirection();


app.UseRouting();


app.Use(async (context, next) =>
{
    if (context.Request.Cookies.TryGetValue("JwtToken", out var token))
    {
        context.Request.Headers["Authorization"] = $"Bearer {token}";
    }
    await next();
});

app.UseStatusCodePages(context =>
{
    if (context.HttpContext.Response.StatusCode == 403)
    {
        context.HttpContext.Response.Redirect("/Authenticate/Forbidden");
    }


    if (context.HttpContext.Response.StatusCode == 401)
    {
        context.HttpContext.Response.Redirect("/Authenticate/Login");
    }

    if (context.HttpContext.Response.StatusCode == 404)
    {
		var request = context.HttpContext.Request;
		var path = request.Path.Value;

		bool isStatic =
		    Path.HasExtension(path) &&
		    (
			   path.EndsWith(".js") ||
			   path.EndsWith(".css") ||
			   path.EndsWith(".png") ||
			   path.EndsWith(".jpg") ||
			   path.EndsWith(".svg") ||
			   path.EndsWith(".ico")
		    );

		if (!isStatic)
		{
			context.HttpContext.Response.Redirect("/Panel/NotFounded");
		}
	}
    return Task.CompletedTask;
});

var storage = builder.Configuration.GetSection("Storage");

var uploadsPath = ResolvePath(storage["UploadsPath"], app.Environment);
var profilePath = ResolvePath(storage["ProfileImagesPath"], app.Environment);

app.UseStaticFiles(new StaticFileOptions
{
	FileProvider = new PhysicalFileProvider(uploadsPath),
	RequestPath = "/uploads"
});

app.UseStaticFiles(new StaticFileOptions
{
	FileProvider = new PhysicalFileProvider(profilePath),
	RequestPath = "/UserProfileImage"
});


app.UseAuthentication();
app.UseAuthorization();


app.UseMiddleware<CustomAuthorizationMiddleware>();
//app.UseMiddleware<ScriptExtractionMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Panel}/{action=Index}");

InitializeApplication(app);
// client will use App.Real hub instead; keep existing mapping temporarily if needed
// app.MapHub<NotificationHub>("/hubs/notification");

app.MapHealthChecks("/health");


app.Run();
// Method to initialize the AccessControllers
void InitializeApplication(WebApplication app)
{
	using (var scope = app.Services.CreateScope())
	{
		var initializeDataBase = scope.ServiceProvider.GetRequiredService<IInitializeDataBase>();

		initializeDataBase.InitAdminUser();

		//var schemaService = scope.ServiceProvider.GetRequiredService<IFormBuilderSchemaService>();
		//schemaService.EnsureFormDefinitionPublishColumnsAsync().GetAwaiter().GetResult();

		//var pageBuilderSchemaService = scope.ServiceProvider.GetRequiredService<IPageBuilderSchemaService>();
		//pageBuilderSchemaService.EnsureTableAsync().GetAwaiter().GetResult();

		//var pageBuilderService = scope.ServiceProvider.GetRequiredService<IPageBuilderService>();
		//pageBuilderService.WarmCacheAsync().GetAwaiter().GetResult();

		var initializeProgram = scope.ServiceProvider.GetRequiredService<IInitializeProgram>();
		initializeProgram.InitializeDataProfiles();
		initializeProgram.InitializeMenu();
		initializeProgram.InitializeRoles();
		initializeProgram.InitializeAccessControllers();
		initializeProgram.InitializeEntityMetadataCache();

		//var publishService = scope.ServiceProvider.GetRequiredService<IFormBuilderPublishService>();
		//publishService.LoadAllPublishedFormsAsync().GetAwaiter().GetResult();

		// Initialize notification rules cache
		var notificationService = scope.ServiceProvider.GetRequiredService<INotifitactionBuilderService>();
		notificationService.InitializeCacheAsync().GetAwaiter().GetResult();


		string licenseName = "134;100-DOWNLOADDEVTOOLS.COM";
		string licenseKey = "1519351-28861E0-148651C-25E14B3-9428";

		LicenseManager.AddLicense(licenseName, licenseKey);

		if (!LicenseManager.ValidateLicense(out string licenseErrorMessage))
		{
			throw new Exception(licenseErrorMessage);
		}

	}
}


string ResolvePath(string path, IWebHostEnvironment env)
{
	var rpath =Path.IsPathRooted(path)
	    ? path
	    : Path.Combine(env.ContentRootPath, path);

     Directory.CreateDirectory(rpath);
     return rpath;
}