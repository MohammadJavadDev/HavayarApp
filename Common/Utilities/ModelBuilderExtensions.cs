using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

using System.Reflection;

namespace Common.Utilities
{
	public static class ModelBuilderExtensions
	{

		public static void RegisterEntityTypeConfiguration(
	  this ModelBuilder modelBuilder,
	  params Assembly[] assemblies)
		{
			var applyConfigMethod = typeof(ModelBuilder)
			    .GetMethods()
			    .First(m =>
				   m.Name == nameof(ModelBuilder.ApplyConfiguration) &&
				   m.GetParameters().Length == 1);

			var configTypes = assemblies
			    .SelectMany(a => a.GetExportedTypes())
			    .Where(t =>
				   t.IsClass &&
				   !t.IsAbstract &&
				   t.GetInterfaces().Any(i =>
					  i.IsGenericType &&
					  i.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>)));

			foreach (var configType in configTypes)
			{
				var interfaceType = configType.GetInterfaces()
				    .First(i =>
					   i.IsGenericType &&
					   i.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>));

				var entityType = interfaceType.GenericTypeArguments[0];

				var applyConcreteMethod = applyConfigMethod
				    .MakeGenericMethod(entityType);

				var configurationInstance = Activator.CreateInstance(configType);

				applyConcreteMethod.Invoke(modelBuilder, new[] { configurationInstance });
			}
		}
		public static void AddRestrictDeleteBehaviorConvention(this ModelBuilder modelBuilder)
		{
			IEnumerable<IMutableForeignKey> cascadeFKs = modelBuilder.Model.GetEntityTypes()
			    .SelectMany(c => c.GetForeignKeys())
			    .Where(c => !c.IsOwnership && c.DeleteBehavior == DeleteBehavior.Cascade);

			foreach (IMutableForeignKey fk in cascadeFKs)
			{
				fk.DeleteBehavior = DeleteBehavior.Restrict;
			}
		}

		public static void AddSequentialGuidForIdConvention<BaseType>(this ModelBuilder modelBuilder)
		{
			modelBuilder.AddDefaultValueSqlConvention<BaseType>("Id", typeof(Guid), "NEWSEQUENTIALID()");
		}

		public static void AddDefaultValueSqlConvention<BaseType>(this ModelBuilder modelBuilder, string propertyName, Type propertyType, string defaultValueSql)
		{
			foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
			{


				if (typeof(BaseType).IsAssignableFrom(entityType.ClrType))
				{
					IMutableProperty property = entityType.GetProperty(propertyName);
					if (property != null && property.ClrType == propertyType)
						property.SetDefaultValueSql(defaultValueSql);
				}

			}
		}

		public static void RegisterAllEntities<BaseType>(this ModelBuilder modelBuilder, params Assembly[] assemblies)
		{

			IEnumerable<Type> types = assemblies.SelectMany(a => a.GetExportedTypes())
			    .Where(c => c.IsClass && !c.IsAbstract && c.IsPublic && typeof(BaseType).IsAssignableFrom(c));

			foreach (Type type in types)
			{
				modelBuilder.Entity(type);
			}


		}
	}
}
