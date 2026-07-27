using System.Reflection;
using System.Runtime.Loader;

namespace Services.DynamicCompilation;

public sealed class CollectibleAssemblyLoadContext : AssemblyLoadContext
{
	public CollectibleAssemblyLoadContext(string name) : base(name, isCollectible: true)
	{
	}

	protected override Assembly? Load(AssemblyName assemblyName)
	{
		return null;
	}
}
