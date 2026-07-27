using Common.Attributes;
using Common.Entities.EntityMetadatas;
using System.Reflection;

namespace Entities.Services;

public interface IDynamicTypeRegistry
{
	long Version { get; }

	IReadOnlyCollection<Type> GetEntityTypes();

	PublishedFormRegistration? GetRegistration(long formDefinitionId);

	PublishedFormRegistration? GetRegistrationByEntityFullName(string entityFullName);

	IReadOnlyCollection<PublishedFormRegistration> GetAllRegistrations();

	void Register(PublishedFormRegistration registration);

	void Unregister(long formDefinitionId);
}

public sealed class PublishedFormRegistration
{
	public long FormDefinitionId { get; init; }
	public string EntityName { get; init; } = "";
	public string EntityFullName { get; init; } = "";
	public string Module { get; init; } = "";
	public string ControllerRoutePrefix { get; init; } = "";
	public Type EntityType { get; init; } = null!;
	public Type ControllerType { get; init; } = null!;
	public Assembly EntityAssembly { get; init; } = null!;
	public Assembly ControllerAssembly { get; init; } = null!;
	public int PublishVersion { get; init; }
	public EntityMetadata? Metadata { get; set; }
}

public sealed class NullDynamicTypeRegistry : IDynamicTypeRegistry
{
	public static readonly NullDynamicTypeRegistry Instance = new();

	public long Version => 0;

	public IReadOnlyCollection<Type> GetEntityTypes() => [];

	public PublishedFormRegistration? GetRegistration(long formDefinitionId) => null;

	public PublishedFormRegistration? GetRegistrationByEntityFullName(string entityFullName) => null;

	public IReadOnlyCollection<PublishedFormRegistration> GetAllRegistrations() => [];

	public void Register(PublishedFormRegistration registration) { }

	public void Unregister(long formDefinitionId) { }
}
