using Common;
using Entities.Services;
using System.Collections.Concurrent;
using System.Reflection;

namespace Services.DynamicCompilation;

public sealed class DynamicTypeRegistry : IDynamicTypeRegistry, ISingletonDependency
{
	private readonly object _lock = new();
	private readonly ConcurrentDictionary<long, PublishedFormRegistration> _registrations = new();
	private long _version;

	public long Version => Volatile.Read(ref _version);

	public IReadOnlyCollection<Type> GetEntityTypes()
	{
		return _registrations.Values.Select(r => r.EntityType).ToList();
	}

	public PublishedFormRegistration? GetRegistration(long formDefinitionId)
	{
		_registrations.TryGetValue(formDefinitionId, out var registration);
		return registration;
	}

	public PublishedFormRegistration? GetRegistrationByEntityFullName(string entityFullName)
	{
		return _registrations.Values.FirstOrDefault(r =>
			r.EntityFullName.Equals(entityFullName, StringComparison.OrdinalIgnoreCase));
	}

	public IReadOnlyCollection<PublishedFormRegistration> GetAllRegistrations()
	{
		return _registrations.Values.ToList();
	}

	public void Register(PublishedFormRegistration registration)
	{
		lock (_lock)
		{
			_registrations.AddOrUpdate(registration.FormDefinitionId, registration, (_, _) => registration);
			Interlocked.Increment(ref _version);
		}
	}

	public void Unregister(long formDefinitionId)
	{
		lock (_lock)
		{
			if (_registrations.TryRemove(formDefinitionId, out _))
				Interlocked.Increment(ref _version);
		}
	}
}
