using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

using System.Reflection;

namespace Common.Utilities
{
    public static class ModelBuilderExtensions
    {


        public static void RegisterEntityTypeConfiguration(this ModelBuilder modelBuilder, params Assembly[] assemblies)
        {

            MethodInfo applyGenericMethod = typeof(ModelBuilder).GetMethods().FirstOrDefault(c => c.Name == nameof(ModelBuilder));

            if (applyGenericMethod != null)
            {
                IEnumerable<Type> types = assemblies.SelectMany(c => c.GetExportedTypes())
                .Where(c => c.IsClass && !c.IsAbstract && c.IsPublic);

                foreach (Type type in types)
                {
                    foreach (Type ifce in type.GetInterfaces())
                    {
                        if (ifce.IsConstructedGenericType && ifce.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>))
                        {
                            MethodInfo applyConcreatedMethod = applyGenericMethod.MakeGenericMethod(ifce.GenericTypeArguments[0]);
                            applyConcreatedMethod.Invoke(modelBuilder, new object[] { Activator.CreateInstance(type) });

                        }
                    }
                }
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

        public static void AddSequentialGuidForIdConvention(this ModelBuilder modelBuilder)
        {
            modelBuilder.AddDefaultValueSqlConvention("Id", typeof(Guid), "NEWSEQUENTIALID()");
        }

        public static void AddDefaultValueSqlConvention(this ModelBuilder modelBuilder, string propertyName, Type propertyType, string defaultValueSql)
        {
            foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
            {
                IMutableProperty property = entityType.GetProperty(propertyName);
                if (property != null && property.ClrType == propertyType)
                    property.SetDefaultValueSql(defaultValueSql);
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
