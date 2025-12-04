using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Data;

/// <summary>
/// Design-time factory برای EF Core Migrations
/// این کلاس فقط در زمان اجرای Add-Migration/Update-Database استفاده می‌شود
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // ساخت configuration برای خواندن connection string
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../WebApp"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        // ساخت DbContextOptions
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        var connectionString = configuration.GetConnectionString("Db");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'Db' not found in WebApp/appsettings.json. " +
                "Make sure you're running migrations from the correct directory.");
        }

        optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
        });

        // ایجاد DbContext بدون dependencies (ISdk null می‌شود)
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}

