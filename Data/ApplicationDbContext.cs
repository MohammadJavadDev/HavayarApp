 
using Common.Utilities;
using Entities.App.Bom.Views;
using Entities.App.Edms.Views;
using Entities.Auth;
using Entities.Base;
using Entities.Base.Job;
using Entities.Services;
 
using Microsoft.EntityFrameworkCore;
 
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReportBuilder.Entities;
using System.Linq.Expressions;
using System.Text.Encodings.Web;
using System.Text.Json;


namespace Data;

public class ApplicationDbContext(
	DbContextOptions<ApplicationDbContext> options,
	IEntityMetadataCache _entityMetadataCache,
	IDynamicTypeRegistry _dynamicTypeRegistry) : DbContext(options)
{
	public long DynamicModelVersion => _dynamicTypeRegistry.Version;
	private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
	{
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
	};

	// helper برای serialize
	private static string SerializeList(List<string> list)
	{
		// JsonSerializer.Serialize هیچ پارامتر اختیاری‌ای استفاده نمی‌کند وقتی گزینه‌ها رو صریح بدهیم
		return JsonSerializer.Serialize(list, _jsonOptions);
	}

	// helper برای deserialize (ایمن در برابر null/empty)
	private static List<string> DeserializeList(string json)
	{
		if (string.IsNullOrEmpty(json))
			return new List<string>();

		// اگر Deserialize null برگرداند، جایگزینش کن
		return JsonSerializer.Deserialize<List<string>>(json, _jsonOptions) ?? new List<string>();
	}

	// helper برای serialize List<long>
	private static string SerializeLongList(List<long> list)
	{
		return JsonSerializer.Serialize(list, _jsonOptions);
	}

	// helper برای deserialize List<long> (ایمن در برابر null/empty)
	private static List<long> DeserializeLongList(string json)
	{
		if (string.IsNullOrEmpty(json))
			return new List<long>();

		// اگر Deserialize null برگرداند، جایگزینش کن
		return JsonSerializer.Deserialize<List<long>>(json, _jsonOptions) ?? new List<long>();
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<User>()
            .ToTable("User", schema: "system")
            .HasKey(x => x.Id);

          modelBuilder.Entity<Role>()
               .ToTable("Role", schema: "system")
               .HasKey(x => x.Id);

		modelBuilder.Entity<RoleAccess>()
		    .ToTable("RoleAccess", schema: "system")
		    .HasKey(x => x.Id);


		modelBuilder.Entity<NotificationGroup>()
		    .ToTable("NotificationGroup", schema: "system")
		    .HasKey(x => x.Id);

		modelBuilder.Entity<NotificationGroupMember>()
		    .ToTable("NotificationGroupMember", schema: "system")
		    .HasKey(x => x.Id);


		modelBuilder.Entity<SavedQuery>()
		    .ToTable("SavedQuery", schema: "system")
		    .HasKey(x => x.Id);


		var entitiesAssembly = typeof(BaseEntity).Assembly;

		//reportBuilderAssembly
		var reportBuilderAssembly = typeof(ReportBuilderReport).Assembly;
     

        modelBuilder.RegisterAllEntities<BaseEntity>(entitiesAssembly, reportBuilderAssembly);
        modelBuilder.AddSequentialGuidForIdConvention<BaseEntity>();
        modelBuilder.RegisterEntityTypeConfiguration(entitiesAssembly);

		foreach (var dynamicEntityType in _dynamicTypeRegistry.GetEntityTypes())
		{
			if (modelBuilder.Model.FindEntityType(dynamicEntityType) == null)
				modelBuilder.Entity(dynamicEntityType);
		}

		var listStringConverter = new ValueConverter<List<string>, string>(
			 v => SerializeList(v),
			 v => DeserializeList(v)
		  );

		var listLongConverter = new ValueConverter<List<long>, string>(
			 v => SerializeLongList(v),
			 v => DeserializeLongList(v)
		  );

		foreach (var entityType in modelBuilder.Model.GetEntityTypes())
		{
			var stringProps = entityType.ClrType
			    .GetProperties()
			    .Where(p => p.PropertyType == typeof(List<string>));

			foreach (var prop in stringProps)
			{
				modelBuilder
				    .Entity(entityType.ClrType)   // بهتر از استفاده از نام رشته‌ای
				    .Property(prop.Name)
				    .HasConversion(listStringConverter);
			}

			var longProps = entityType.ClrType
			    .GetProperties()
			    .Where(p => p.PropertyType == typeof(List<long>));

			foreach (var prop in longProps)
			{
				modelBuilder
				    .Entity(entityType.ClrType)
				    .Property(prop.Name)
				    .HasConversion(listLongConverter);
			}
		}


	}

    public override int SaveChanges()
    {
        try
        {

            return base.SaveChanges();

        }
        catch (DbUpdateException ex)
        {
            throw TranslateDbUpdateException(ex);
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw TranslateDbUpdateException(ex);
        }
    }
	private Exception TranslateDbUpdateException(DbUpdateException ex)
	{
		string persianMessage = "خطایی در عملیات پایگاه داده رخ داده است.";
		if (ex.InnerException != null)
		{
			var message = ex.InnerException.Message;

			// کنترل خطای NULL
			if (message.Contains("Cannot insert the value NULL into column"))
			{
				try
				{
					// استخراج نام ستون و جدول
					var columnStartIndex = message.IndexOf("column '") + "column '".Length;
					var columnEndIndex = message.IndexOf("'", columnStartIndex);
					var columnName = message.Substring(columnStartIndex, columnEndIndex - columnStartIndex);

					var tableStartIndex = message.IndexOf("table '") + "table '".Length;
					var tableEndIndex = message.IndexOf("'", tableStartIndex);
					var fullTableName = message.Substring(tableStartIndex, tableEndIndex - tableStartIndex);

					// استخراج نام جدول (بدون schema)
					var tableName = fullTableName.Split('.').LastOrDefault() ?? fullTableName;

					// دریافت متادیتای entity
					var entityMetadata = _entityMetadataCache.Get(tableName);

					if (entityMetadata != null)
					{
						// پیدا کردن property مربوطه
						var property = entityMetadata.Properties
						    .FirstOrDefault(p => p.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase));

						if (property != null && !string.IsNullOrEmpty(property.DisplayName))
						{
							persianMessage = $"فیلد '{property.DisplayName}' اجباری است و نمی‌تواند خالی باشد.";
						}
						else
						{
							persianMessage = $"فیلد '{columnName}' اجباری است و نمی‌تواند خالی باشد.";
						}
					}
					else
					{
						persianMessage = $"فیلد '{columnName}' در جدول '{tableName}' اجباری است و نمی‌تواند خالی باشد.";
					}
				}
				catch
				{
					persianMessage = "یک یا چند فیلد اجباری خالی مانده است. لطفاً تمام فیلدهای ضروری را پر کنید.";
				}
			}
			else if (message.Contains("REFERENCE constraint"))
			{
				var columnStartIndex = message.IndexOf("column '") + "column '".Length;
				var columnEndIndex = message.IndexOf("'", columnStartIndex);
				var columnName = message.Substring(columnStartIndex, columnEndIndex - columnStartIndex);
				persianMessage = $"امکان حذف این رکورد وجود ندارد زیرا به رکوردهایی در جدول مرتبط وابسته است. ستون مربوطه: '{columnName}'.";
			}
			else if (message.Contains("UNIQUE constraint") || message.Contains("PRIMARY KEY"))
			{
				var constraintStartIndex = message.IndexOf("constraint \"") + "constraint \"".Length;
				var constraintEndIndex = message.IndexOf("\"", constraintStartIndex);
				var constraintName = message.Substring(constraintStartIndex, constraintEndIndex - constraintStartIndex);
				persianMessage = $"مقادیر تکراری در فیلدهایی که باید یکتا باشند وجود دارد. نام محدودیت: '{constraintName}'.";
			}
			else if (message.Contains("String or binary data would be truncated"))
			{
				persianMessage = "طول داده وارد شده بیش از حد مجاز است. لطفاً اطلاعات خود را بررسی کنید.";
			}
			else if (message.Contains("timeout") || message.Contains("could not open connection"))
			{
				persianMessage = "ارتباط با پایگاه داده امکان‌پذیر نیست یا زمان انجام عملیات به پایان رسیده است. لطفاً دوباره تلاش کنید.";
			}
			else
			{
				persianMessage = "خطایی در اجرای عملیات پایگاه داده رخ داده است. لطفاً جزئیات بیشتر را بررسی کنید.";
			}
		}
		return new Exception(persianMessage);
	}



	public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<AuditLogDetail> AuditLogDetails { get; set; }
    public DbSet<JobLog> JobLogs { get; set; }
    public virtual DbSet<User> Users { get; set; }
	public virtual DbSet<Role> Roles { get; set; }
	public virtual DbSet<RoleAccess> RoleAccesses { get; set; }
	public virtual DbSet<JobDefinition> JobDefinitions { get; set; }
	public virtual DbSet<JobSchedule> JobSchedules { get; set; }
	public virtual DbSet<JobHistory> JobHistories { get; set; }
	public virtual DbSet<NotificationGroupMember> NotificationGroupMembers { get; set; }
	public virtual DbSet<NotificationGroup> NotificationGroups { get; set; }
	public virtual DbSet<SavedQuery> SavedQueries { get; set; }
	public virtual DbSet<vw_PartDocumentPrice> vw_PartDocumentPrices { get; set; }
	public virtual DbSet<vw_ProductItems> vw_ProductItems { get; set; }
	public virtual DbSet<Vw_ServiceRequest_Detail> Vw_ServiceRequest_Details { get; set; }





}