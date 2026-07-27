using Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Services.DynamicCompilation;

public interface IDynamicCompilationService
{
	CompilationResult CompileAssembly(
		string assemblyName,
		IEnumerable<(string FileName, string SourceCode)> sources,
		CollectibleAssemblyLoadContext? loadContext = null,
		IEnumerable<MetadataReference>? additionalReferences = null,
		IEnumerable<MetadataReference>? directReferences = null);

	MetadataReference CreateMetadataReference(Assembly assembly);
	MetadataReference CreateMetadataReference(byte[] assemblyBytes);
}

public sealed class CompilationResult
{
	public bool Success { get; init; }
	public Assembly? Assembly { get; init; }
	public byte[]? AssemblyBytes { get; init; }
	public CollectibleAssemblyLoadContext? LoadContext { get; init; }
	public IReadOnlyList<string> Errors { get; init; } = [];
}

public sealed class DynamicCompilationService : IDynamicCompilationService, ISingletonDependency
{
	private readonly Lazy<IReadOnlyList<MetadataReference>> _defaultReferences;

	public DynamicCompilationService()
	{
		_defaultReferences = new Lazy<IReadOnlyList<MetadataReference>>(BuildDefaultReferences);
	}

	public CompilationResult CompileAssembly(
		string assemblyName,
		IEnumerable<(string FileName, string SourceCode)> sources,
		CollectibleAssemblyLoadContext? loadContext = null,
		IEnumerable<MetadataReference>? additionalReferences = null,
		IEnumerable<MetadataReference>? directReferences = null)
	{
		var references = directReferences?.ToList() ?? new List<MetadataReference>(_defaultReferences.Value);
		if (additionalReferences != null)
			references.AddRange(additionalReferences);

		var syntaxTrees = sources.Select(source =>
			CSharpSyntaxTree.ParseText(
				source.SourceCode,
				new CSharpParseOptions(LanguageVersion.Latest),
				path: source.FileName)).ToArray();

		var compilation = CSharpCompilation.Create(
			assemblyName,
			syntaxTrees,
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
				.WithOptimizationLevel(OptimizationLevel.Release)
				.WithPlatform(Platform.AnyCpu));

		loadContext ??= new CollectibleAssemblyLoadContext(assemblyName);
		using var ms = new MemoryStream();
		EmitResult emitResult = compilation.Emit(ms);

		if (!emitResult.Success)
		{
			var errors = emitResult.Diagnostics
				.Where(d => d.Severity == DiagnosticSeverity.Error)
				.Select(d => d.ToString())
				.ToList();

			return new CompilationResult { Success = false, Errors = errors, LoadContext = loadContext };
		}

		var assemblyBytes = ms.ToArray();
		var assembly = loadContext.LoadFromStream(new MemoryStream(assemblyBytes));

		return new CompilationResult
		{
			Success = true,
			Assembly = assembly,
			AssemblyBytes = assemblyBytes,
			LoadContext = loadContext,
			Errors = []
		};
	}

	public MetadataReference CreateMetadataReference(byte[] assemblyBytes)
	{
		return MetadataReference.CreateFromImage(assemblyBytes);
	}

	public MetadataReference CreateMetadataReference(Assembly assembly)
	{
		if (!string.IsNullOrEmpty(assembly.Location))
			return MetadataReference.CreateFromFile(assembly.Location);

		throw new InvalidOperationException($"Assembly '{assembly.FullName}' has no load location.");
	}

	private static IReadOnlyList<MetadataReference> BuildDefaultReferences()
	{
		var references = new List<MetadataReference>();
		var addedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		void AddReference(string? path)
		{
			if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
				return;

			if (addedPaths.Add(path))
				references.Add(MetadataReference.CreateFromFile(path));
		}

		if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string trustedAssemblies)
		{
			foreach (var assemblyPath in trustedAssemblies.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
				AddReference(assemblyPath);
		}

		var assemblies = new HashSet<Assembly>(ReferenceEqualityComparer.Instance)
		{
			typeof(object).Assembly,
			typeof(Enumerable).Assembly,
			typeof(Task).Assembly,
			typeof(Uri).Assembly,
			typeof(Entities.Base.BaseEntity).Assembly,
			typeof(Common.Attributes.SystemType).Assembly,
			typeof(Microsoft.EntityFrameworkCore.DbContext).Assembly,
			typeof(Microsoft.AspNetCore.Mvc.ControllerBase).Assembly,
		};

		foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			if (assembly.IsDynamic || string.IsNullOrEmpty(assembly.Location))
				continue;

			assemblies.Add(assembly);
		}

		foreach (var assembly in assemblies)
			AddReference(assembly.Location);

		return references;
	}
}

public sealed class ReferenceEqualityComparer : IEqualityComparer<Assembly>
{
	public static ReferenceEqualityComparer Instance { get; } = new();

	public bool Equals(Assembly? x, Assembly? y) => ReferenceEquals(x, y);

	public int GetHashCode(Assembly obj) => RuntimeHelpers.GetHashCode(obj);
}
