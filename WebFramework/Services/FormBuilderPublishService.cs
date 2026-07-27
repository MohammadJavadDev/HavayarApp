using Common;
using Common.Utilities;
using Data;
using Data.Contracts;
using Data.Services;
using Entities.Base.FormBuilder;
using Entities.Services;
using Microsoft.EntityFrameworkCore;
using Services.DynamicCompilation;
using System.Reflection;
using WebFramework.Abstractions;
using WebFramework.Initializes;

namespace WebFramework.Services;

public interface IFormBuilderPublishService
{
	Task<FormPublishResult> PublishAsync(long formDefinitionId, CancellationToken cancellationToken = default);
	Task<FormPublishResult> UnpublishAsync(long formDefinitionId, CancellationToken cancellationToken = default);
	Task<FormPublishResult> RepublishAsync(long formDefinitionId, CancellationToken cancellationToken = default);
	Task<FormPublishStatusResult> GetPublishStatusAsync(long formDefinitionId, CancellationToken cancellationToken = default);
	Task LoadAllPublishedFormsAsync(CancellationToken cancellationToken = default);
}

public sealed class FormPublishResult
{
	public bool Success { get; init; }
	public string? Message { get; init; }
	public string? ListUrl { get; init; }
	public string? EditUrl { get; init; }
	public IReadOnlyList<string> Errors { get; init; } = [];
}

public sealed class FormPublishStatusResult
{
	public bool IsPublished { get; init; }
	public DateTime? PublishedAt { get; init; }
	public int PublishVersion { get; init; }
	public string? LastPublishError { get; init; }
	public string? ListUrl { get; init; }
	public string? EditUrl { get; init; }
}

public sealed class FormBuilderPublishService(
	IUnitOfWork unitOfWork,
	ApplicationDbContext dbContext,
	IFormBuilderCodeGenerator codeGenerator,
	IFormBuilderSchemaService schemaService,
	IDynamicCompilationService compilationService,
	IDynamicTypeRegistry typeRegistry,
	IDynamicControllerRegistrar controllerRegistrar,
	IFormDefinitionMetadataBuilder metadataBuilder,
	IEntityMetadataCache entityMetadataCache,
	IInitializeProgram initializeProgram,
	IPageBuilderService pageBuilderService,
	IEnvironmentService environmentService) : IFormBuilderPublishService, IScopedDependency
{
	public async Task<FormPublishResult> PublishAsync(long formDefinitionId, CancellationToken cancellationToken = default)
	{
		var formDefinition = await LoadFormDefinitionAsync(formDefinitionId, cancellationToken);
		if (formDefinition == null)
			return Fail("تعریف فرم یافت نشد");

		if (formDefinition.IsPublished)
			return await RepublishAsync(formDefinitionId, cancellationToken);

		return await PublishInternalAsync(formDefinition, cancellationToken);
	}

	public async Task<FormPublishResult> RepublishAsync(long formDefinitionId, CancellationToken cancellationToken = default)
	{
		var formDefinition = await LoadFormDefinitionAsync(formDefinitionId, cancellationToken);
		if (formDefinition == null)
			return Fail("تعریف فرم یافت نشد");

		return await PublishInternalAsync(formDefinition, cancellationToken);
	}

	public async Task<FormPublishResult> UnpublishAsync(long formDefinitionId, CancellationToken cancellationToken = default)
	{
		var formDefinition = await unitOfWork.Repository<FormDefinition>()
			.Table
			.FirstOrDefaultAsync(f => f.Id == formDefinitionId, cancellationToken);

		if (formDefinition == null)
			return Fail("تعریف فرم یافت نشد");

		var registration = typeRegistry.GetRegistration(formDefinitionId);
		if (registration != null)
			entityMetadataCache.RemoveDynamicMetadata(registration.EntityFullName);

		typeRegistry.Unregister(formDefinitionId);
		controllerRegistrar.Unregister(formDefinitionId);
		await pageBuilderService.RemoveFormPagesAsync(formDefinitionId, cancellationToken);

		formDefinition.IsPublished = false;
		formDefinition.LastPublishError = null;
		await dbContext.SaveChangesAsync(cancellationToken);

		initializeProgram.InitializeAccessControllers();
		entityMetadataCache.Refresh();

		return new FormPublishResult
		{
			Success = true,
			Message = "فرم با موفقیت از حالت انتشار خارج شد"
		};
	}

	public async Task<FormPublishStatusResult> GetPublishStatusAsync(long formDefinitionId, CancellationToken cancellationToken = default)
	{
		var formDefinition = await unitOfWork.Repository<FormDefinition>()
			.TableNoTracking
			.FirstOrDefaultAsync(f => f.Id == formDefinitionId, cancellationToken);

		if (formDefinition == null)
		{
			return new FormPublishStatusResult();
		}

		var routePrefix = GetRoutePrefix(formDefinition);
		return new FormPublishStatusResult
		{
			IsPublished = formDefinition.IsPublished,
			PublishedAt = formDefinition.PublishedAt,
			PublishVersion = formDefinition.PublishVersion,
			LastPublishError = formDefinition.LastPublishError,
			ListUrl = $"/{routePrefix}/{formDefinition.EntityName}/List",
			EditUrl = $"/{routePrefix}/{formDefinition.EntityName}/Edit"
		};
	}

	public async Task LoadAllPublishedFormsAsync(CancellationToken cancellationToken = default)
	{
		await schemaService.EnsureFormDefinitionPublishColumnsAsync(cancellationToken);

		var publishedForms = await unitOfWork.Repository<FormDefinition>()
			.TableNoTracking
			.Where(f => f.IsPublished)
			.Select(f => f.Id!.Value)
			.ToListAsync(cancellationToken);

		foreach (var formId in publishedForms)
		{
			try
			{
				await RepublishAsync(formId, cancellationToken);
			}
			catch
			{
				// startup should continue for other forms
			}
		}
	}

	private async Task<FormPublishResult> PublishInternalAsync(FormDefinition formDefinition, CancellationToken cancellationToken)
	{
		try
		{
			await schemaService.EnsureFormDefinitionPublishColumnsAsync(cancellationToken);

			var previousRegistration = typeRegistry.GetRegistration(formDefinition.Id!.Value);
			var previousDefinition = previousRegistration == null
				? null
				: await LoadFormDefinitionAsync(formDefinition.Id.Value, cancellationToken);

			if (previousDefinition != null)
				await schemaService.SyncSchemaAsync(previousDefinition, formDefinition, cancellationToken);
			else
				await schemaService.EnsureSchemaAsync(formDefinition, cancellationToken);

			var code = codeGenerator.GenerateCode(formDefinition);
			codeGenerator.SaveAllFiles(formDefinition, code, FormBuilderSaveOptions.PublishArtifacts);

			await pageBuilderService.EnsureFormPagesAsync(formDefinition, new FormPageContent
			{
				ListContent = code.ListView,
				EditContent = code.EditView
			}, cancellationToken);

			var (listViewPath, editViewPath) = PageBuilderPathHelper.GetStandardFormViewPaths(
				formDefinition.Module,
				formDefinition.EntityName);
			PageBuilderPathHelper.TryDeleteDiskView(environmentService.ContentRootPath, listViewPath);
			PageBuilderPathHelper.TryDeleteDiskView(environmentService.ContentRootPath, editViewPath);

			var entitySources = BuildEntitySources(formDefinition, code);
			var publishVersion = formDefinition.PublishVersion + 1;
			var loadContext = new CollectibleAssemblyLoadContext($"DynamicForm_{formDefinition.Id}_v{publishVersion}");
			var entityAssemblyName = $"DynamicEntities_{formDefinition.Id}_v{publishVersion}";

			var entityCompilation = compilationService.CompileAssembly(
				entityAssemblyName,
				entitySources,
				loadContext: loadContext);
			if (!entityCompilation.Success || entityCompilation.Assembly == null)
				return await SavePublishErrorAsync(formDefinition, entityCompilation.Errors, cancellationToken);

			var entityType = FindType(entityCompilation.Assembly, formDefinition.EntityName, t =>
				t.IsClass && !t.IsAbstract && t.Name == formDefinition.EntityName);

			if (entityType == null)
				return await SavePublishErrorAsync(formDefinition, ["Entity type not found in compiled assembly"], cancellationToken);

			if (entityCompilation.AssemblyBytes == null)
				return await SavePublishErrorAsync(formDefinition, ["Entity assembly bytes are missing"], cancellationToken);

			var entityReference = compilationService.CreateMetadataReference(entityCompilation.AssemblyBytes);
			var controllerSources = new List<(string FileName, string SourceCode)>
			{
				($"{formDefinition.EntityName}Controller.cs", code.ControllerClass)
			};

			var controllerAssemblyName = $"DynamicControllers_{formDefinition.Id}_v{publishVersion}";
			var controllerCompilation = compilationService.CompileAssembly(
				controllerAssemblyName,
				controllerSources,
				loadContext: loadContext,
				additionalReferences: [entityReference]);

			if (!controllerCompilation.Success || controllerCompilation.Assembly == null)
				return await SavePublishErrorAsync(formDefinition, controllerCompilation.Errors, cancellationToken);

			var controllerTypeName = $"{formDefinition.EntityName}Controller";
			var controllerType = FindType(controllerCompilation.Assembly, controllerTypeName, t =>
				t.IsClass && !t.IsAbstract && t.Name == controllerTypeName);

			if (controllerType == null)
				return await SavePublishErrorAsync(formDefinition, ["Controller type not found in compiled assembly"], cancellationToken);

			var routePrefix = GetRoutePrefix(formDefinition);
			var metadata = metadataBuilder.BuildFromType(entityType, formDefinition);

			if (previousRegistration != null)
				entityMetadataCache.RemoveDynamicMetadata(previousRegistration.EntityFullName);

			typeRegistry.Unregister(formDefinition.Id!.Value);
			controllerRegistrar.Unregister(formDefinition.Id!.Value);

			var entityReferencePath = SaveDynamicAssemblyReference(
				entityAssemblyName,
				entityCompilation.AssemblyBytes,
				publishVersion);

			var registration = new PublishedFormRegistration
			{
				FormDefinitionId = formDefinition.Id!.Value,
				EntityName = formDefinition.EntityName,
				EntityFullName = entityType.FullName ?? formDefinition.EntityName,
				Module = formDefinition.Module,
				ControllerRoutePrefix = routePrefix,
				EntityType = entityType,
				ControllerType = controllerType,
				EntityAssembly = entityCompilation.Assembly,
				ControllerAssembly = controllerCompilation.Assembly,
				PublishVersion = publishVersion,
				Metadata = metadata
			};

			typeRegistry.Register(registration);
			controllerRegistrar.Register(
				formDefinition.Id!.Value,
				controllerCompilation.Assembly,
				[entityReferencePath]);
			entityMetadataCache.SetDynamicMetadata(metadata);

			formDefinition.IsPublished = true;
			formDefinition.PublishedAt = DateTime.Now;
			formDefinition.PublishVersion = registration.PublishVersion;
			formDefinition.LastPublishError = null;
			formDefinition.ControllerRoutePrefix = routePrefix;

			await dbContext.SaveChangesAsync(cancellationToken);

			initializeProgram.InitializeAccessControllers();
			entityMetadataCache.Refresh();
			entityMetadataCache.SetDynamicMetadata(metadata);

			return new FormPublishResult
			{
				Success = true,
				Message = "فرم با موفقیت منتشر شد",
				ListUrl = $"/{routePrefix}/{formDefinition.EntityName}/List",
				EditUrl = $"/{routePrefix}/{formDefinition.EntityName}/Edit"
			};
		}
		catch (Exception ex)
		{
			return await SavePublishErrorAsync(formDefinition, [ex.Message], cancellationToken);
		}
	}

	private async Task<FormDefinition?> LoadFormDefinitionAsync(long formDefinitionId, CancellationToken cancellationToken)
	{
		return await unitOfWork.Repository<FormDefinition>()
			.Table
			.AsSplitQuery()
			.Include(f => f.Sections)
				.ThenInclude(s => s.Properties)
					.ThenInclude(p => p.EnumOptions)
			.Include(f => f.Sections)
				.ThenInclude(s => s.Properties)
					.ThenInclude(p => p.ChildProperties)
						.ThenInclude(cp => cp.EnumOptions)
			.FirstOrDefaultAsync(f => f.Id == formDefinitionId, cancellationToken);
	}

	private static List<(string FileName, string SourceCode)> BuildEntitySources(FormDefinition formDefinition, GeneratedCodeResult code)
	{
		var sources = new List<(string FileName, string SourceCode)>
		{
			($"{formDefinition.EntityName}.cs", code.EntityClass)
		};

		foreach (var enumClass in code.EnumClasses)
			sources.Add(($"{enumClass.Key}.cs", enumClass.Value));

		return sources;
	}

	private string SaveDynamicAssemblyReference(string assemblyName, byte[] assemblyBytes, int publishVersion)
	{
		var directory = Path.Combine(environmentService.ContentRootPath, "App_Data", "DynamicAssemblies");
		Directory.CreateDirectory(directory);

		var safeAssemblyName = string.Join("_", assemblyName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
		var path = Path.Combine(directory, $"{safeAssemblyName}_{publishVersion}.dll");
		File.WriteAllBytes(path, assemblyBytes);
		return path;
	}

	private static string GetRoutePrefix(FormDefinition formDefinition)
	{
		if (!string.IsNullOrWhiteSpace(formDefinition.ControllerRoutePrefix))
			return formDefinition.ControllerRoutePrefix.Trim('/');

		return $"Panel/{formDefinition.Module}";
	}

	private async Task<FormPublishResult> SavePublishErrorAsync(
		FormDefinition formDefinition,
		IReadOnlyList<string> errors,
		CancellationToken cancellationToken)
	{
		formDefinition.LastPublishError = string.Join(Environment.NewLine, errors);
		await dbContext.SaveChangesAsync(cancellationToken);

		return new FormPublishResult
		{
			Success = false,
			Message = "خطا در انتشار فرم",
			Errors = errors
		};
	}

	private static FormPublishResult Fail(string message)
	{
		return new FormPublishResult
		{
			Success = false,
			Message = message,
			Errors = [message]
		};
	}

	private static Type? FindType(Assembly assembly, string typeName, Func<Type, bool> predicate)
	{
		try
		{
			return assembly.GetTypes().FirstOrDefault(predicate);
		}
		catch (ReflectionTypeLoadException ex)
		{
			var loaderErrors = ex.LoaderExceptions?
				.Where(e => e != null)
				.Select(e => e!.Message)
				.ToList() ?? [];

			if (loaderErrors.Count > 0)
				throw new InvalidOperationException(
					$"Unable to load types from assembly '{assembly.GetName().Name}': {string.Join(" | ", loaderErrors)}",
					ex);

			return ex.Types?.FirstOrDefault(t => t != null && predicate(t!));
		}
	}
}
