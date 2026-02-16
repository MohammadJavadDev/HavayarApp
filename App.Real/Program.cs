using App.Real.Hubs;
using App.Real.Services;
using Common.System;
using Data;
using Data.Contracts;
using Data.Repositories;
using Data.Services;
using Data.SystemAuth;
using Entities.Services;
using Infrastructure.CrudEventInterceptors;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Services.AccessServices;
using Services.Auth;
using Services.NotifitactionBuilderServices;
using Shared.Realtime.Options;
using System.Security.Claims;
using System.Text;
using WebFramework.Initializes;
using Z.EntityFramework.Extensions;


var builder = WebApplication.CreateBuilder(args);

// Read allowed origins from appsettings
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
    ?? new[] { "https://localhost:7073", "http://localhost:7073" };

// Configure CORS for SignalR cross-origin requests
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebApp", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Required for SignalR
    });
});

// Redis configuration
var redisConnection = builder.Configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>()?.ConnectionString ?? "localhost:6379";
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnection;
    options.InstanceName = "HavayarApp:";
});

builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection(RedisOptions.SectionName));

// SignalR with Redis backplane
var redisOpts = builder.Configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>();
if (redisOpts?.EnableSignalRBackplane == true)
{
    builder.Services.AddSignalR()
        .AddStackExchangeRedis(redisOpts.ConnectionString, options =>
        {
            options.Configuration.ChannelPrefix = "HavayarApp.SignalR";
        });
}
else
{
    builder.Services.AddSignalR();
}

builder.Services.Configure<ConnectionTokenOptions>(builder.Configuration.GetSection(ConnectionTokenOptions.SectionName));
builder.Services.Configure<RabbitMQOptions>(builder.Configuration.GetSection(RabbitMQOptions.SectionName));

// JwtBearer for connection tokens
var tokenOptions = builder.Configuration.GetSection(ConnectionTokenOptions.SectionName).Get<ConnectionTokenOptions>() ?? new ConnectionTokenOptions();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = tokenOptions.Issuer,
            ValidAudience = tokenOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenOptions.Secret))
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"].ToString();
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/realtime"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });


builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("db"), sql =>
    {
        sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
    }));

builder.Services.AddScoped(typeof(IUnitOfWork), typeof(UnitOfWork));
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped(typeof(IEntityRepository), typeof(EntityRepository));


builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddTransient<IAuthService, AuthService>();

builder.Services.AddScoped<IPropertyIdentityService, PropertyIdentityService>();
builder.Services.AddScoped<IDataTableQueryBuilder, DataTableQueryBuilder>();
builder.Services.AddSingleton<EndpointService>();

builder.Services.AddScoped<IInitializeProgram, InitializeProgram>();
builder.Services.AddScoped<IInitializeDataBase, InitializeDataBase>();

builder.Services.AddScoped<IEntityHistoryRepository, EntityHistoryRepository>();

builder.Services.AddScoped<INotifitactionBuilderService, NotifitactionBuilderService>();
builder.Services.AddSingleton<INotificationRuleCache, NotificationRuleCache>();

// Redis-backed caches (shared با WebApp)
builder.Services.AddSingleton<IRoleMemoryStorage, RedisRoleMemoryStorage>();
builder.Services.AddSingleton<IAccessMemoryStorage, AccessMemoryStorage>();
builder.Services.AddSingleton<IEntityMetadataCache, RedisEntityMetadataCache>();
builder.Services.AddSingleton<EntityMetadataCache>();
builder.Services.AddSingleton<IMenuBuilderService, MenuBuilderService>();
builder.Services.AddSingleton<IDataTableProfileService, DataTableProfileService>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ISdk, Sdk>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddAuthorization();
 

builder.Services.AddSingleton<IUserIdProvider, NameIdentifierUserIdProvider>();
builder.Services.AddSingleton<IEntityChangeNotificationHandler, EntityChangeNotificationHandler>();
builder.Services.AddSingleton<IOnlineUserService, RedisOnlineUserService>();
builder.Services.AddHostedService<RabbitMqSubscriberHostedService>();
builder.Services.AddScoped<IActiveDirectoryService, ActiveDirectoryService>();
builder.Services.AddHealthChecks();


var app = builder.Build();

app.UseHttpsRedirection();
app.UseRouting();

// Enable CORS before authentication
app.UseCors("AllowWebApp");

app.UseAuthentication();
app.UseAuthorization();

InitializeApplication(app);

app.MapHub<RealtimeHub>("/hubs/realtime");
app.MapHealthChecks("/health");

// Simple status endpoint
app.MapGet("/status", () => Results.Ok(new
{
    Status = "Running",
    Service = "App.Real",
    Timestamp = DateTime.UtcNow,
    Environment = app.Environment.EnvironmentName
}));

app.Run();



void InitializeApplication(WebApplication app)
{
	using (var scope = app.Services.CreateScope())
	{

		string licenseName = "134;100-DOWNLOADDEVTOOLS.COM";
		string licenseKey = "1519351-28861E0-148651C-25E14B3-9428";

		LicenseManager.AddLicense(licenseName, licenseKey);

		if (!LicenseManager.ValidateLicense(out string licenseErrorMessage))
		{
			throw new Exception(licenseErrorMessage);
		}

	}
}