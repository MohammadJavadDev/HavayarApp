using Common.System;
using Data;
using Data.Contracts;
using Data.Repositories;
using Data.Services;
using Data.SystemAuth;
using Entities.Auth;
using Entities.Services;
using Infrastructure.CrudEventInterceptors;
using Infrastructure.Messaging;
using Infrastructure.NotificationServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ReportBuilder.WebApp;
using Services;
using Services.AccessServices;
using Services.Auth;
using Services.InMemoryData;
using Services.NotificationServices;
using Services.NotifitactionBuilderServices;
using Shared.Realtime.Options;
using System.Text;
using WebApp.Framework.File;
using WebApp.Services;
using WebApp.Services.Realtime;
using WebFramework.Initializes;
using WebFramework.Middlewares;


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
    }).AddRazorRuntimeCompilation();


 

builder.Services.AddScoped<CrudEventInterceptor>();

builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("db"), sql =>
    {
        sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
    }).AddInterceptors(sp.GetRequiredService<CrudEventInterceptor>()));



builder.Services.AddScoped(typeof(IUnitOfWork), typeof(UnitOfWork));
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped(typeof(IEntityRepository), typeof(EntityRepository));

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

 

builder.Services.AddScoped<IActiveDirectoryService, ActiveDirectoryService>();

 


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
app.UseStaticFiles();
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
        context.HttpContext.Response.Redirect("/Panel/NotFounded");
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

        var initializeProgram = scope.ServiceProvider.GetRequiredService<IInitializeProgram>();
        initializeProgram.InitializeDataProfiles();
        initializeProgram.InitializeMenu();
        initializeProgram.InitializeRoles();
        initializeProgram.InitializeAccessControllers();
        initializeProgram.InitializeEntityMetadataCache();

        // Initialize notification rules cache
        var notificationService = scope.ServiceProvider.GetRequiredService<INotifitactionBuilderService>();
        notificationService.InitializeCacheAsync().GetAwaiter().GetResult();
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