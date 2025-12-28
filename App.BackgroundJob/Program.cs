using Data;
using Data.Contracts;
using Data.Repositories;
using Data.Services;
using Data.SystemAuth;
using Entities.Services;
using Infrastructure.CrudEventInterceptors;
using Microsoft.EntityFrameworkCore;
using Services.FileServices;
using Services.Job;

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

builder.Services.AddHostedService<JobDiscoveryService>();
builder.Services.AddHostedService<JobWorkerWithLogging>();

builder.Services.AddScoped<IFileService, FileService>();

builder.Services.AddScoped<ISdk, Sdk>();
builder.Services.AddScoped<IDataTableQueryBuilder, DataTableQueryBuilder>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddSingleton<IDataTableProfileService, DataTableProfileService>();
builder.Services.AddJobServices();

// Register JobLogger service
builder.Services.AddScoped<IJobLogger, JobLogger>();

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

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
