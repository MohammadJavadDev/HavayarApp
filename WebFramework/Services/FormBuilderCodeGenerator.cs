using Common.Attributes;
using Common.Utilities;
using Entities.Base;
using Entities.Base.FormBuilder;
using System.Text;
using WebFramework.Abstractions;

namespace WebFramework.Services
{
	public interface IFormBuilderCodeGenerator
	{
		GeneratedCodeResult GenerateCode(FormDefinition formDefinition);
		Dictionary<string, string> GetFilePaths(FormDefinition formDefinition);
		bool SaveAllFiles(FormDefinition formDefinition, GeneratedCodeResult code);
	}

	public class GeneratedCodeResult
	{
		public string EntityClass { get; set; }
		public string ControllerClass { get; set; }
		public string EditView { get; set; }
		public string ListView { get; set; }
		public Dictionary<string, string> PartialViews { get; set; } = new();
		public Dictionary<string, string> EnumClasses { get; set; } = new();
	}

	public class FormBuilderCodeGenerator : IFormBuilderCodeGenerator
	{
		private readonly IEnvironmentService _webHostEnvironment;
		private string viewFolderPath => Path.Combine(_webHostEnvironment.ContentRootPath, "Views");
		private string controllerFolderPath => Path.Combine(_webHostEnvironment.ContentRootPath, "Controllers");
		private string entitiesFolderPath => Path.Combine(_webHostEnvironment.ContentRootPath, "..", "Entities");

		public FormBuilderCodeGenerator(IEnvironmentService webHostEnvironment)
		{
			_webHostEnvironment = webHostEnvironment;
		}

	public GeneratedCodeResult GenerateCode(FormDefinition formDefinition)
	{
		var result = new GeneratedCodeResult
		{
			EntityClass = GenerateEntityClass(formDefinition),
			ControllerClass = GenerateControllerClass(formDefinition),
 
			EditView = GenerateEditView(formDefinition),
			ListView = GenerateListView(formDefinition)
		};

		// Collect all properties from sections (backward compatibility)
		var allProperties = new List<FormProperty>();
		if (formDefinition.Sections != null && formDefinition.Sections.Any())
		{
			// If sections exist, collect properties from sections
			foreach (var section in formDefinition.Sections.OrderBy(s => s.OrderIndex))
			{
				if (section.Properties != null)
				{
					allProperties.AddRange(section.Properties);
				}
			}
		}
		else
		{
			// Fallback to old Properties collection for backward compatibility
		 
		}

		// Generate partial views for ListEntity properties
		foreach (var prop in allProperties.Where(p => p.SystemType == SystemType.ListEntity))
		{
			var partialViewName = $"_{prop.ChildEntityName}Partial";
			result.PartialViews[partialViewName] = GeneratePartialView(prop, formDefinition.EntityName, formDefinition);
		}

		// Generate enum classes for Select properties with inline enums
		foreach (var prop in allProperties.Where(p => p.SystemType == SystemType.Select && !string.IsNullOrEmpty(p.EnumName)))
		{
			result.EnumClasses[prop.EnumName] = GenerateEnumClass(prop, formDefinition.Module);
		}

		return result;
	}

	public Dictionary<string, string> GetFilePaths(FormDefinition formDefinition)
	{
		var paths = new Dictionary<string, string>();
		var module = formDefinition.Module;
		var entityName = formDefinition.EntityName;
		var schema = formDefinition.Schema ?? "dbo";

		// Entity path
		paths["Entity"] = Path.Combine(entitiesFolderPath, "App", module, $"{entityName}.cs");

		// Controller path
		paths["Controller"] = Path.Combine(controllerFolderPath, "Dynamic", module, $"{entityName}Controller.cs");

		// View paths
		var viewFolder = Path.Combine(viewFolderPath, "Panel", module, entityName);
		paths["EditView"] = Path.Combine(viewFolder, "Edit.cshtml");
		paths["ListView"] = Path.Combine(viewFolder, "List.cshtml");

		// Collect all properties from sections (backward compatibility)
		var allProperties = new List<FormProperty>();
		if (formDefinition.Sections != null && formDefinition.Sections.Any())
		{
			// If sections exist, collect properties from sections
			foreach (var section in formDefinition.Sections.OrderBy(s => s.OrderIndex))
			{
				if (section.Properties != null)
				{
					allProperties.AddRange(section.Properties);
				}
			}
		}
		else
		{
			// Fallback to old Properties collection for backward compatibility
		 
		}

		// Partial views
		foreach (var prop in allProperties.Where(p => p.SystemType == SystemType.ListEntity))
		{
			paths[$"Partial_{prop.ChildEntityName}"] = Path.Combine(viewFolder, $"_{prop.ChildEntityName}Partial.cshtml");
		}

		// Enum paths
		foreach (var prop in allProperties.Where(p => p.SystemType == SystemType.Select && !string.IsNullOrEmpty(p.EnumName)))
		{
			paths[$"Enum_{prop.EnumName}"] = Path.Combine(entitiesFolderPath, "App", module, "Enums", $"{prop.EnumName}.cs");
		}

		return paths;
	}

		public bool SaveAllFiles(FormDefinition formDefinition, GeneratedCodeResult code)
		{
			try
			{
				var paths = GetFilePaths(formDefinition);

				// Save entity
				Directory.CreateDirectory(Path.GetDirectoryName(paths["Entity"])!);
				File.WriteAllText(paths["Entity"], code.EntityClass);

				// Save controller
				Directory.CreateDirectory(Path.GetDirectoryName(paths["Controller"])!);
				File.WriteAllText(paths["Controller"], code.ControllerClass);

				// Save views
				Directory.CreateDirectory(Path.GetDirectoryName(paths["EditView"])!);
				File.WriteAllText(paths["EditView"], code.EditView);
				File.WriteAllText(paths["ListView"], code.ListView);

				// Save partial views
				foreach (var kvp in code.PartialViews)
				{
					var key = $"Partial_{kvp.Key.Replace("_", "").Replace("Partial", "")}";
					if (paths.ContainsKey(key))
					{
						File.WriteAllText(paths[key], kvp.Value);
					}
				}

				// Save enum classes
				foreach (var kvp in code.EnumClasses)
				{
					var key = $"Enum_{kvp.Key}";
					if (paths.ContainsKey(key))
					{
						Directory.CreateDirectory(Path.GetDirectoryName(paths[key])!);
						File.WriteAllText(paths[key], kvp.Value);
					}
				}

				return true;
			}
			catch
			{
				return false;
			}
		}

		private string GenerateEntityClass(FormDefinition formDefinition)
		{
			var sb = new StringBuilder();
			var entityName = formDefinition.EntityName;
			var displayName = formDefinition.DisplayName;
			var module = formDefinition.Module;
			var schema = formDefinition.Schema ?? "dbo";
			var baseType = formDefinition.BaseEntityType ?? "BaseEntity";
			var namespacePrefix = formDefinition.Namespace ?? $"Entities.App.{module}";

			// Using statements
			sb.AppendLine("using Common.Attributes;");
			sb.AppendLine("using Entities.Base;");
			sb.AppendLine("using System.ComponentModel;");
			sb.AppendLine("using System.ComponentModel.DataAnnotations;");
			sb.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
			sb.AppendLine();

			// Namespace
			sb.AppendLine($"namespace {namespacePrefix}");
			sb.AppendLine("{");

			// Main entity class
			sb.AppendLine($"\t[Display(Name = \"{displayName}\")]");
			sb.AppendLine($"\t[Table(\"{entityName}\", Schema = \"{schema}\")]");
			sb.AppendLine($"\tpublic class {entityName} : {baseType}");
			sb.AppendLine("\t{");

			// Collect all properties from sections (backward compatibility)
			var allProperties = new List<FormProperty>();
			if (formDefinition.Sections != null && formDefinition.Sections.Any())
			{
				// If sections exist, collect properties from sections
				foreach (var section in formDefinition.Sections.OrderBy(s => s.OrderIndex))
				{
					if (section.Properties != null)
					{
						allProperties.AddRange(section.Properties.OrderBy(p => p.OrderIndex));
					}
				}
			}
			else
			{
				// Fallback to old Properties collection for backward compatibility
 			}

			// Generate properties
			foreach (var prop in allProperties)
			{
				sb.AppendLine(GeneratePropertyCode(prop, entityName));
			}

			sb.AppendLine("\t}");

			// Generate child entity classes for ListEntity properties
			foreach (var prop in allProperties.Where(p => p.SystemType == SystemType.ListEntity))
			{
				sb.AppendLine();
				sb.AppendLine(GenerateChildEntityClass(prop, entityName, schema));
			}

			sb.AppendLine("}");

			return sb.ToString();
		}

		private string GeneratePropertyCode(FormProperty prop, string parentEntityName)
		{
			var sb = new StringBuilder();

			// DisplayName attribute
			sb.AppendLine($"\t\t[DisplayName(\"{prop.DisplayName}\")]");

			// DisplayInfo attribute
			var displayInfoParams = new List<string>
			{
				prop.SearchPath != null ? $"\"{prop.SearchPath}\"" : "null",
				prop.AddToTable.ToString().ToLower(),
				$"type: SystemType.{prop.SystemType}"
			};

			if (prop.SystemProperty) displayInfoParams.Add("systemProprty: true");
			if (prop.Required) displayInfoParams.Add("required: true");
			if (prop.ShowInRelationData) displayInfoParams.Add("showInRelationData: true");
			if (!string.IsNullOrEmpty(prop.FileTypes)) displayInfoParams.Add($"fileTypes: \"{prop.FileTypes}\"");
			if (prop.MaxFileSize != 10) displayInfoParams.Add($"maxFileSize: {prop.MaxFileSize}");
			if (!string.IsNullOrEmpty(prop.Regex)) displayInfoParams.Add($"regex: @\"{prop.Regex}\"");
			if (!string.IsNullOrEmpty(prop.RegexInvalidError)) displayInfoParams.Add($"regexInvalidError: \"{prop.RegexInvalidError}\"");
			if (prop.SystemType == SystemType.AutoNumber)
			{
				displayInfoParams.Add($"start: {prop.AutoNumberStart}");
				displayInfoParams.Add($"step: {prop.AutoNumberStep}");
			}

			sb.AppendLine($"\t\t[DisplayInfo({string.Join(", ", displayInfoParams)})]");

			// MaxLength for string properties
			if (prop.MaxLength.HasValue && (prop.SystemType == SystemType.String || prop.SystemType == SystemType.DateShamsi || prop.SystemType == SystemType.DateTimeShamsi))
			{
				sb.AppendLine($"\t\t[MaxLength({prop.MaxLength})]");
			}

		// Property declaration
		var propertyType = GetCSharpType(prop);
		var nullable = prop.Required ? "" : "?";

		// These types should never be nullable
		if (prop.SystemType == SystemType.ListEntity || 
		    prop.SystemType == SystemType.ListString || 
		    prop.SystemType == SystemType.ListLong ||
		    prop.SystemType == SystemType.Boolean)
		{
			nullable = "";
		}

		var defaultValue = GetDefaultValue(prop);
		// GetDefaultValue already includes semicolon if it has a value, otherwise add semicolon
		if (string.IsNullOrEmpty(defaultValue))
		{
			sb.AppendLine($"\t\tpublic {propertyType}{nullable} {prop.PropertyName} {{ get; set; }}");
		}
		else
		{
			sb.AppendLine($"\t\tpublic {propertyType}{nullable} {prop.PropertyName} {{ get; set; }}{defaultValue}");
		}

		// Foreign key for Entity type
		if (prop.SystemType == SystemType.Entity && !string.IsNullOrEmpty(prop.RelatedEntityName))
		{
			sb.AppendLine();
			sb.AppendLine($"\t\tpublic long{(prop.Required ? "" : "?")} {prop.PropertyName}Id {{ get; set; }}");
		}

		// Foreign key for File type
		if (prop.SystemType == SystemType.File)
		{
			sb.AppendLine();
			sb.AppendLine($"\t\tpublic long{(prop.Required ? "" : "?")} {prop.PropertyName}Id {{ get; set; }}");
		}

			sb.AppendLine();

			return sb.ToString();
		}

		private string GenerateChildEntityClass(FormProperty prop, string parentEntityName, string schema)
		{
			var sb = new StringBuilder();
			var childEntityName = prop.ChildEntityName;
			var childDisplayName = prop.ChildEntityDisplayName;

			sb.AppendLine($"\t[Display(Name = \"{childDisplayName}\")]");
			sb.AppendLine($"\t[Table(\"{childEntityName}\", Schema = \"{schema}\")]");
			sb.AppendLine($"\tpublic class {childEntityName} : BaseEntity");
			sb.AppendLine("\t{");

		// Parent reference
		sb.AppendLine($"\t\tpublic long {parentEntityName}Id {{ get; set; }};");
		sb.AppendLine($"\t\tpublic {parentEntityName} {parentEntityName} {{ get; set; }}");
			sb.AppendLine();

			// Generate child properties
			foreach (var childProp in prop.ChildProperties.OrderBy(p => p.OrderIndex))
			{
				sb.AppendLine(GeneratePropertyCode(childProp, childEntityName));
			}

			sb.AppendLine("\t}");

			// Recursively generate nested child entities
			foreach (var nestedProp in prop.ChildProperties.Where(p => p.SystemType == SystemType.ListEntity))
			{
				sb.AppendLine();
				sb.AppendLine(GenerateChildEntityClass(nestedProp, childEntityName, schema));
			}

			return sb.ToString();
		}

		private string GenerateEnumClass(FormProperty prop, string module)
		{
			var sb = new StringBuilder();
			var enumName = prop.EnumName;

			sb.AppendLine("using System.ComponentModel.DataAnnotations;");
			sb.AppendLine();
			sb.AppendLine($"namespace Entities.App.{module}.Enums");
			sb.AppendLine("{");
			sb.AppendLine($"\tpublic enum {enumName}");
			sb.AppendLine("\t{");

			foreach (var option in prop.EnumOptions.OrderBy(o => o.OrderIndex))
			{
				// Use EnglishName for enum member identifier (must be valid C# identifier)
				var enumMemberName = !string.IsNullOrWhiteSpace(option.EnglishName)
					? MakeValidIdentifier(option.EnglishName)
					: MakeValidIdentifier(option.Title);

				// Use Title (Persian) for Display attribute
				var displayName = option.Title ?? enumMemberName;

				sb.AppendLine($"\t\t[Display(Name = \"{displayName}\")]");
				sb.AppendLine($"\t\t{enumMemberName} = {option.Value},");
			}

			sb.AppendLine("\t}");
			sb.AppendLine("}");

			return sb.ToString();
		}

		private string GetCSharpType(FormProperty prop)
		{
			return prop.SystemType switch
			{
				SystemType.String => "string",
				SystemType.Boolean => "bool",
				SystemType.Int => "int",
				SystemType.Long => "long",
				SystemType.Decimal => "decimal",
				SystemType.DateTime => "DateTime",
				SystemType.Date => "DateTime",
				SystemType.DateShamsi => "string",
				SystemType.DateTimeShamsi => "string",
				SystemType.Select => prop.EnumName ?? "int",
				SystemType.Entity => prop.RelatedEntityName ?? "object",
				SystemType.File => "FileEntity",
				SystemType.ListEntity => $"List<{prop.ChildEntityName}>",
				SystemType.ListString => "List<string>",
				SystemType.ListLong => "List<long>",
				SystemType.AutoNumber => "long",
				_ => "string"
			};
		}

	private string GetDefaultValue(FormProperty prop)
	{
		if (prop.SystemType == SystemType.ListEntity)
			return " = new();";
		if (prop.SystemType == SystemType.ListString)
			return " = new List<string>();";
		if (prop.SystemType == SystemType.ListLong)
			return " = new List<long>();";
		if (prop.SystemType == SystemType.Boolean)
			return " = false;";

		return "";
	}

		private string GenerateControllerClass(FormDefinition formDefinition)
		{
			var sb = new StringBuilder();
			var entityName = formDefinition.EntityName;
			var displayName = formDefinition.DisplayName;
			var module = formDefinition.Module;
			var namespacePrefix = formDefinition.Namespace ?? $"Entities.App.{module}";
			var prefixPath = "Panel"+"/"+ formDefinition.Module;

			var viewFolder = Path.Combine("Panel", module, entityName);
			var listPath = $"\\Views\\{viewFolder}\\List.cshtml";
			var editPath = $"\\Views\\{viewFolder}\\Edit.cshtml";

			sb.AppendLine("using Common.Attributes;");
			sb.AppendLine("using Common.Auth.Enums;");
			sb.AppendLine("using Data.Contracts;");
			sb.AppendLine("using Data.SystemAuth;");
			sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
			sb.AppendLine("using Microsoft.EntityFrameworkCore;");
			sb.AppendLine("using Entities.Base.DataTable;");
			sb.AppendLine("using WebFramework.Filtters;");
			sb.AppendLine("using WebFramework.Page;");
			sb.AppendLine($"using {namespacePrefix};");
			sb.AppendLine();
			sb.AppendLine("namespace WebApp.Controllers.Dynamic");
			sb.AppendLine("{");
			sb.AppendLine($"\t[Route(\"{prefixPath}/[controller]\")]");
			sb.AppendLine("\t[ApiController]");
			sb.AppendLine("\t[ApiResultFilter]");
			sb.AppendLine($"\t[ControllerInfo(\"{displayName}\", typeof({entityName}))]");
			sb.AppendLine($"\tpublic class {entityName}Controller(IUnitOfWork unitOfWork, IPropertyIdentityService identityService, IWebHostEnvironment _webHostEnvironment) : BaseController");
			sb.AppendLine("\t{");

			// Save method
			sb.AppendLine("\t\t[HttpPost(\"[action]\")]");
			sb.AppendLine("\t\t[ActionDisplayName(\"ذخیره\", ActionAccessType.Api, ActionAccessItemType.Save)]");
			sb.AppendLine($"\t\tpublic async Task<IActionResult> Save({entityName} {entityName.ToCamelCase()}, CancellationToken cn)");
			sb.AppendLine("\t\t{");
			sb.AppendLine($"\t\t\tif ({entityName.ToCamelCase()}.Id == null || {entityName.ToCamelCase()}.Id == 0)");
			sb.AppendLine("\t\t\t{");
			sb.AppendLine($"\t\t\t\treturn await Add({entityName.ToCamelCase()}, cn);");
			sb.AppendLine("\t\t\t}");
			sb.AppendLine($"\t\t\tvar exist = await unitOfWork.Repository<{entityName}>().TableNoTracking.AnyAsync(c => c.Id == {entityName.ToCamelCase()}.Id);");
			sb.AppendLine("\t\t\tif (exist)");
			sb.AppendLine("\t\t\t{");
			sb.AppendLine($"\t\t\t\treturn await Update({entityName.ToCamelCase()}, cn);");
			sb.AppendLine("\t\t\t}");
			sb.AppendLine($"\t\t\treturn await Add({entityName.ToCamelCase()}, cn);");
			sb.AppendLine("\t\t}");
			sb.AppendLine();

			// Add method
			sb.AppendLine("\t\t[HttpPost(\"[action]\")]");
			sb.AppendLine("\t\t[ActionDisplayName(\"درج\", ActionAccessType.Api, ActionAccessItemType.Create)]");
			sb.AppendLine($"\t\tpublic async Task<IActionResult> Add({entityName} {entityName.ToCamelCase()}, CancellationToken cn)");
			sb.AppendLine("\t\t{");
			sb.AppendLine($"\t\t\tvar entity = await unitOfWork.Repository<{entityName}>().SaveAsync({entityName.ToCamelCase()}, cn, true);");
			sb.AppendLine("\t\t\treturn Ok(entity);");
			sb.AppendLine("\t\t}");
			sb.AppendLine();

			// Update method
			sb.AppendLine("\t\t[HttpPost(\"[action]\")]");
			sb.AppendLine("\t\t[ActionDisplayName(\"ویرایش\", ActionAccessType.Api, ActionAccessItemType.Update)]");
			sb.AppendLine($"\t\tpublic async Task<IActionResult> Update({entityName} {entityName.ToCamelCase()}, CancellationToken cn)");
			sb.AppendLine("\t\t{");
			sb.AppendLine($"\t\t\tvar entity = await unitOfWork.Repository<{entityName}>().UpdateAsync({entityName.ToCamelCase()}, cn, true);");
			sb.AppendLine("\t\t\treturn Ok(entity);");
			sb.AppendLine("\t\t}");
			sb.AppendLine();

			// Delete method
			sb.AppendLine("\t\t[HttpGet(\"[action]\")]");
			sb.AppendLine("\t\t[ActionDisplayName(\"حذف\", ActionAccessType.Api, ActionAccessItemType.Delete)]");
			sb.AppendLine("\t\tpublic async Task<IActionResult> Delete(long id, CancellationToken cn)");
			sb.AppendLine("\t\t{");
			sb.AppendLine($"\t\t\tvar model = unitOfWork.Repository<{entityName}>().TableNoTracking.FirstOrDefault(c => c.Id == id);");
			sb.AppendLine("\t\t\tif (model != null)");
			sb.AppendLine($"\t\t\t\tawait unitOfWork.Repository<{entityName}>().DeleteAsync(model, cn, true);");
			sb.AppendLine("\t\t\treturn Ok();");
			sb.AppendLine("\t\t}");
			sb.AppendLine();

			// Edit method
			sb.AppendLine("\t\t[HttpGet(\"[action]\")]");
			sb.AppendLine("\t\t[ActionDisplayName(\"ویرایش اطلاعات\", ActionAccessType.View, ActionAccessItemType.Update)]");
			sb.AppendLine("\t\tpublic IActionResult Edit(long? id)");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\tif (id != null && id != 0)");
			sb.AppendLine("\t\t\t{");

			// Collect all properties from sections (backward compatibility)
			var allProperties = new List<FormProperty>();
			if (formDefinition.Sections != null && formDefinition.Sections.Any())
			{
				// If sections exist, collect properties from sections
				foreach (var section in formDefinition.Sections.OrderBy(s => s.OrderIndex))
				{
					if (section.Properties != null)
					{
						allProperties.AddRange(section.Properties);
					}
				}
			}
			else
			{
				// Fallback to old Properties collection for backward compatibility
 			}

			// Build includes for related entities
			var includes = allProperties
				.Where(p => p.SystemType == SystemType.Entity || p.SystemType == SystemType.ListEntity)
				.Select(p => $"\t\t\t\t\t.Include(c => c.{p.PropertyName})")
				.ToList();

			sb.AppendLine($"\t\t\t\tvar entity = unitOfWork.Repository<{entityName}>().TableNoTracking");
			foreach (var include in includes)
			{
				sb.AppendLine(include);
			}
			sb.AppendLine("\t\t\t\t\t.FirstOrDefault(c => c.Id == id);");
			sb.AppendLine($"\t\t\t\treturn View(@\"{editPath}\", entity);");
			sb.AppendLine("\t\t\t}");
			sb.AppendLine($"\t\t\tvar newEntity = new {entityName}();");
			sb.AppendLine($"\t\t\treturn View(@\"{editPath}\", newEntity);");
			sb.AppendLine("\t\t}");
			sb.AppendLine();

			// New method
			sb.AppendLine("\t\t[HttpGet(\"[action]\")]");
			sb.AppendLine("\t\t[ActionDisplayName(\"درج اطلاعات\", ActionAccessType.View, ActionAccessItemType.Create)]");
			sb.AppendLine("\t\tpublic IActionResult New()");
			sb.AppendLine("\t\t{");
			sb.AppendLine($"\t\t\tvar newEntity = new {entityName}();");
			sb.AppendLine($"\t\t\treturn View(@\"{editPath}\", newEntity);");
			sb.AppendLine("\t\t}");
			sb.AppendLine();

			// List method
			sb.AppendLine("\t\t[HttpGet(\"[action]\")]");
			sb.AppendLine("\t\t[ActionDisplayName(\"لیست اطلاعات\", ActionAccessType.View, ActionAccessItemType.List)]");
			sb.AppendLine("\t\tpublic IActionResult List()");
			sb.AppendLine("\t\t{");
			sb.AppendLine($"\t\t\treturn View(@\"{listPath}\");");
			sb.AppendLine("\t\t}");
			sb.AppendLine();

			// ExportToExcel method
			sb.AppendLine("\t\t[HttpPost(\"[action]\")]");
			sb.AppendLine("\t\t[ActionDisplayName(\"خروجی اکسل\", ActionAccessType.Api)]");
			sb.AppendLine("\t\tpublic async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\tvar licensePath = _webHostEnvironment.WebRootPath + \"\\\\Aspose.Total.NET.lic\";");
			sb.AppendLine("\t\t\tvar memoryStream = new MemoryStream();");
			sb.AppendLine("\t\t\ttry");
			sb.AppendLine("\t\t\t{");
			sb.AppendLine($"\t\t\t\tawait unitOfWork.Repository<{entityName}>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);");
			sb.AppendLine("\t\t\t\tmemoryStream.Position = 0;");
			sb.AppendLine("\t\t\t\treturn File(memoryStream, \"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet\", $\"exportExcel.xlsx\");");
			sb.AppendLine("\t\t\t}");
			sb.AppendLine("\t\t\tcatch (Exception ex)");
			sb.AppendLine("\t\t\t{");
			sb.AppendLine("\t\t\t\treturn StatusCode(500, \"خطا در زمان ایجاد فایل اکسل: \" + ex.Message);");
			sb.AppendLine("\t\t\t}");
			sb.AppendLine("\t\t}");
			sb.AppendLine();

			// FetchData method
			sb.AppendLine("\t\t[ActionDisplayName(\"دریافت اطلاعات\", ActionAccessType.Api, ActionAccessItemType.FetchData)]");
			sb.AppendLine("\t\t[HttpPost(\"[action]\")]");
			sb.AppendLine("\t\tpublic async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)");
			sb.AppendLine("\t\t{");
			sb.AppendLine($"\t\t\treturn Ok(await unitOfWork.Repository<{entityName}>().FetchDataAsync(request, cn));");
			sb.AppendLine("\t\t}");

			// Partial view methods for ListEntity properties
			foreach (var prop in allProperties.Where(p => p.SystemType == SystemType.ListEntity))
			{
				sb.AppendLine();
				sb.AppendLine("\t\t[HttpGet(\"[action]\")]");
				sb.AppendLine($"\t\tpublic IActionResult {prop.ChildEntityName}Partial()");
				sb.AppendLine("\t\t{");
				sb.AppendLine($"\t\t\treturn PartialView(@\"\\Views\\{viewFolder}\\_{prop.ChildEntityName}Partial.cshtml\");");
				sb.AppendLine("\t\t}");
			}

			sb.AppendLine("\t}");
			sb.AppendLine("}");

			return sb.ToString();
		}

		private string GenerateListView(FormDefinition formDefinition)
		{
			var sb = new StringBuilder();
			var entityName = formDefinition.EntityName;
			var namespacePrefix = formDefinition.Namespace ?? $"Entities.App.{formDefinition.Module}";

			sb.AppendLine($"@using {namespacePrefix};");
			sb.AppendLine($"<datatableprofile entity-Type=\"typeof({entityName})\" ></datatableprofile>");

			return sb.ToString();
		}

		private class SectionsFormDefinition
		{
			public string Title { get; set; }
			public List<FormProperty> Properties { get; set; } = new();
			public List<FormProperty> ListEntityProperties { get; set; } = new();
		}
	private string GenerateEditView(FormDefinition formDefinition)
	{
		var sb = new StringBuilder();
		var entityName = formDefinition.EntityName;
		var namespacePrefix = formDefinition.Namespace ?? $"Entities.App.{formDefinition.Module}";
		var module = formDefinition.Module;

		// Collect all namespaces needed
		var namespaces = new HashSet<string> { namespacePrefix };
		
		// Collect all properties from sections
		var allProperties = new List<FormProperty>();
		if (formDefinition.Sections != null && formDefinition.Sections.Any())
		{
			foreach (var section in formDefinition.Sections.OrderBy(s => s.OrderIndex))
			{
				if (section.Properties != null)
				{
					allProperties.AddRange(section.Properties);
				}
			}
		}
		
		// Add namespaces for Entity properties
		foreach (var prop in allProperties.Where(p => p.SystemType == SystemType.Entity))
		{
			if (!string.IsNullOrEmpty(prop.RelatedEntityFullName))
			{
				// Extract namespace from full name (e.g., "Entities.App.Parts.Part" -> "Entities.App.Parts")
				var lastDotIndex = prop.RelatedEntityFullName.LastIndexOf('.');
				if (lastDotIndex > 0)
				{
					var entityNamespace = prop.RelatedEntityFullName.Substring(0, lastDotIndex);
					namespaces.Add(entityNamespace);
				}
			}
		}
		
		// Add namespaces for Enum properties
		foreach (var prop in allProperties.Where(p => p.SystemType == SystemType.Select && !string.IsNullOrEmpty(p.EnumName)))
		{
			// Add enum namespace
			var enumNamespace = $"{namespacePrefix}.Enums";
			namespaces.Add(enumNamespace);
		}
		
		// Add namespaces for nested Entity properties in ListEntity
		foreach (var prop in allProperties.Where(p => p.SystemType == SystemType.ListEntity))
		{
			if (prop.ChildProperties != null)
			{
				foreach (var childProp in prop.ChildProperties.Where(cp => cp.SystemType == SystemType.Entity))
				{
					if (!string.IsNullOrEmpty(childProp.RelatedEntityFullName))
					{
						var lastDotIndex = childProp.RelatedEntityFullName.LastIndexOf('.');
						if (lastDotIndex > 0)
						{
							var entityNamespace = childProp.RelatedEntityFullName.Substring(0, lastDotIndex);
							namespaces.Add(entityNamespace);
						}
					}
				}
				
				// Add namespaces for nested enums in ListEntity
				foreach (var childProp in prop.ChildProperties.Where(cp => cp.SystemType == SystemType.Select && !string.IsNullOrEmpty(cp.EnumName)))
				{
					var enumNamespace = $"{namespacePrefix}.Enums";
					namespaces.Add(enumNamespace);
				}
			}
		}
		
		// Write all using statements
		foreach (var ns in namespaces.OrderBy(n => n))
		{
			sb.AppendLine($"@using {ns};");
		}
		
		sb.AppendLine($"@model {entityName}");

		// Check if we have sections or use backward compatibility
		var sections = new List<SectionsFormDefinition>();
		if (formDefinition.Sections != null && formDefinition.Sections.Any())
		{
			// Use sections
			foreach (var section in formDefinition.Sections.OrderBy(s => s.OrderIndex))
			{
				sections.Add(new SectionsFormDefinition
				{
					Title = section.Title,
					Properties = section.Properties?.Where(p => p.SystemType != SystemType.ListEntity).OrderBy(p => p.OrderIndex).ToList() ?? new List<FormProperty>(),
					ListEntityProperties = section.Properties?.Where(p => p.SystemType == SystemType.ListEntity).ToList() ?? new List<FormProperty>()
				});
			}
		}
		else
		{
			// Backward compatibility: create a single section with all properties
			 
		}

		// Generate sections
		foreach (var section in sections)
		{
			sb.AppendLine("<div class=\"card\">");
			sb.AppendLine("\t<div class=\"card-body row\">");

			// Add section title at the top of card-body
			if (section.Title != "اطلاعات" || sections.Count > 1)
			{
				sb.AppendLine("\t\t<div class=\"col-12 mb-4\">");
				sb.AppendLine($"\t\t\t<h3 class=\"fw-bold text-dark\">{section.Title}</h3>");
				sb.AppendLine("\t\t</div>");
			}

			if (sections.Count == 1 && sections[0].Title == "اطلاعات")
			{
				// Backward compatibility: include form action buttons in the first section
				sb.AppendLine($"\t\t<div class=\"form-action-buttons\"><form-action-buttons entity-type=\"typeof({entityName})\"></form-action-buttons></div>");
			}

			sb.AppendLine("\t\t<input class='form-control' data-bind='id' value='@Model?.Id' type='hidden' />");

			// Generate fields for each property in this section
			foreach (var prop in section.Properties)
			{
				sb.AppendLine(GenerateViewField(prop, formDefinition));
			}

			// Generate tab manager for ListEntity properties in this section
			if (section.ListEntityProperties.Any())
			{
				sb.AppendLine("\t\t<div class='col-md-12'>");
				sb.AppendLine($"\t\t\t<div data-tab-item-manager=\"{entityName.ToCamelCase()}Items\" data-item-selector=\".entity-item\">");
				sb.AppendLine("\t\t\t\t<ul class=\"nav nav-tabs nav-line-tabs nav-line-tabs-2x border-transparent fs-4 fw-bold mb-5\" role=\"tablist\">");

				bool isFirst = true;
				foreach (var prop in section.ListEntityProperties)
				{
					var tabId = $"{prop.PropertyName.ToCamelCase()}Tab";
					var activeClass = isFirst ? "active" : "";
					var ariaSelected = isFirst ? "true" : "false";

					sb.AppendLine($"\t\t\t\t\t<li class=\"nav-item\" role=\"presentation\">");
					sb.AppendLine($"\t\t\t\t\t\t<a class=\"nav-link {activeClass}\" data-bs-toggle=\"tab\" href=\"#{tabId}\" role=\"tab\" aria-selected=\"{ariaSelected}\">");
					sb.AppendLine("\t\t\t\t\t\t\t<span class=\"svg-icon svg-icon-2 me-2\"></span>");
					sb.AppendLine($"\t\t\t\t\t\t\t{prop.DisplayName}");
					sb.AppendLine("\t\t\t\t\t\t</a>");
					sb.AppendLine("\t\t\t\t\t</li>");

					isFirst = false;
				}

				sb.AppendLine("\t\t\t\t</ul>");
				sb.AppendLine("\t\t\t\t<div class=\"tab-content\">");

				isFirst = true;
				foreach (var prop in section.ListEntityProperties)
				{
					var tabId = $"{prop.PropertyName.ToCamelCase()}Tab";
					var showClass = isFirst ? "show active" : "";
					var containerId = $"{prop.PropertyName}Container";

					sb.AppendLine($"\t\t\t\t\t<div class=\"tab-pane fade {showClass}\" id=\"{tabId}\" role=\"tabpanel\" data-add-url=\"/Panel/{entityName}/{prop.ChildEntityName}Partial\">");
					sb.AppendLine($"\t\t\t\t\t\t<div id='{containerId}' class=\"entity-item-container\">");
					sb.AppendLine($"\t\t\t\t\t\t\t@if (Model?.{prop.PropertyName} != null)");
					sb.AppendLine("\t\t\t\t\t\t\t{");
					sb.AppendLine($"\t\t\t\t\t\t\t\t@foreach (var item in Model.{prop.PropertyName})");
					sb.AppendLine("\t\t\t\t\t\t\t\t{");
					sb.AppendLine("\t\t\t\t\t\t\t\t\t<div data-item-id=\"@(item.Id ?? 0)\">");
					sb.AppendLine($"\t\t\t\t\t\t\t\t\t\t@await Html.PartialAsync(\"_{prop.ChildEntityName}Partial.cshtml\", item)");
					sb.AppendLine("\t\t\t\t\t\t\t\t\t</div>");
					sb.AppendLine("\t\t\t\t\t\t\t\t}");
					sb.AppendLine("\t\t\t\t\t\t\t}");
					sb.AppendLine("\t\t\t\t\t\t</div>");
					sb.AppendLine("\t\t\t\t\t</div>");

					isFirst = false;
				}

				sb.AppendLine("\t\t\t\t</div>");
				sb.AppendLine("\t\t\t</div>");
				sb.AppendLine("\t\t</div>");
			}

			sb.AppendLine("\t</div>");
			sb.AppendLine("</div>");
			sb.AppendLine();
		}

		// Footer as separate card
		sb.AppendLine("<div class=\"card\">");
		sb.AppendLine($"\t<div class=\"form-action-buttons\"><form-action-buttons entity-type=\"typeof({entityName})\"></form-action-buttons></div>");
		sb.AppendLine("\t<div class=\"card-body\">");
		sb.AppendLine("\t</div>");
		sb.AppendLine("\t<div class='card-footer row'>");
		sb.AppendLine("\t\t<div class='col-md-3'>");
		sb.AppendLine("\t\t\t<span>ایجاد کننده :</span>");
		sb.AppendLine("\t\t\t<span data-bind=\"createdByName\">@Model?.CreatedByName</span>");
		sb.AppendLine("\t\t</div>");
		sb.AppendLine("\t\t<div class='col-md-3'>");
		sb.AppendLine("\t\t\t<span>تاریخ ایجاد :</span>");
		sb.AppendLine("\t\t\t<span data-bind=\"createdOnShamsiDateTime\">@Model?.CreatedOnShamsiDateTime</span>");
		sb.AppendLine("\t\t</div>");
		sb.AppendLine("\t\t<div class='col-md-3'>");
		sb.AppendLine("\t\t\t<span>ویرایش کننده :</span>");
		sb.AppendLine("\t\t\t<span data-bind=\"modifiedByName\">@Model?.ModifiedByName</span>");
		sb.AppendLine("\t\t</div>");
		sb.AppendLine("\t\t<div class='col-md-3'>");
		sb.AppendLine("\t\t\t<span>تاریخ ویرایش :</span>");
		sb.AppendLine("\t\t\t<span data-bind=\"modifiedDateShamsiDateTime\">@Model?.ModifiedDateShamsiDateTime</span>");
		sb.AppendLine("\t\t</div>");
		sb.AppendLine("\t</div>");
		sb.AppendLine("</div>");

			// Script
			sb.AppendLine();
			sb.AppendLine("<script>");
			sb.AppendLine("\tfunction savefn($btnAction) {");
			sb.AppendLine("\t\tif (validateError(page.$pageEl)) { return; }");
			sb.AppendLine("\t\tvar model = page.$pageEl.dataBind();");
			sb.AppendLine("\t\tconst $btn = $(this.element).block();");
			sb.AppendLine("\t\t$$.post('save',");
			sb.AppendLine("\t\t\tmodel,");
			sb.AppendLine("\t\t\tfunction (r) {");
			sb.AppendLine("\t\t\t\t$btn.block(false);");
			sb.AppendLine("\t\t\t\tif (!r.isSuccess) return toastr.error(`${r.message}`, 'خطا');");
			sb.AppendLine("\t\t\t\ttoastr.success('ذخیره سازی با موفقیت انجام شد .');");
			sb.AppendLine("\t\t\t\tpage.$pageEl.dataBind(r.data);");
			sb.AppendLine("\t\t\t\t$btnAction.baseaction();");
			sb.AppendLine("\t\t\t})");
			sb.AppendLine("\t}");
			sb.AppendLine();
			sb.AppendLine("\tself.FormActionButtons.save.onclick(function () { savefn(this) })");
			sb.AppendLine("\tself.FormActionButtons.saveandnew.onclick(function () { savefn(this) })");
			sb.AppendLine("\tself.FormActionButtons.saveandclose.onclick(function () { savefn(this) })");
			sb.AppendLine("</script>");

			return sb.ToString();
		}

		private string GenerateViewField(FormProperty prop, FormDefinition formDefinition)
		{
			var sb = new StringBuilder();
			var bindName = prop.PropertyName.ToCamelCase();
			var requiredClass = prop.Required ? "required" : "";
			var requiredAttr = prop.Required ? "required" : "";
			var colSize = prop.ColSize ?? "col-md-6";

			switch (prop.SystemType)
			{
				case SystemType.String:
					sb.AppendLine($"\t\t<div class='{colSize}'>");
					sb.AppendLine($"\t\t\t<label class='{requiredClass}'>{prop.DisplayName}</label>");
					sb.AppendLine($"\t\t\t<input class='form-control' data-bind='{bindName}' {requiredAttr} asp-for='{prop.PropertyName}' />");
					sb.AppendLine($"\t\t\t<div data-invalidmessagespan class='my-1 mx-1'> <span class='text-danger'></span> </div>");
					sb.AppendLine($"\t\t</div>");
					break;

				case SystemType.Boolean:
					sb.AppendLine($"\t\t<div class='{colSize} align-content-center'>");
					sb.AppendLine($"\t\t\t<div class=\"form-check\">");
					sb.AppendLine($"\t\t\t\t<input data-bind='{bindName}' class=\"form-check-input\" type=\"checkbox\" asp-for='{prop.PropertyName}' />");
					sb.AppendLine($"\t\t\t\t<label class=\"form-check-label\" asp-for='{prop.PropertyName}'>");
					sb.AppendLine($"\t\t\t\t\t{prop.DisplayName}");
					sb.AppendLine($"\t\t\t\t</label>");
					sb.AppendLine($"\t\t\t</div>");
					sb.AppendLine($"\t\t</div>");
					break;

				case SystemType.Int:
				case SystemType.Long:
				case SystemType.Decimal:
				case SystemType.AutoNumber:
					var disabled = prop.SystemType == SystemType.AutoNumber ? "disabled=\"disabled\"" : "";
					sb.AppendLine($"\t\t<div class='{colSize}'>");
					sb.AppendLine($"\t\t\t<label class='{requiredClass}'>{prop.DisplayName}</label>");
					sb.AppendLine($"\t\t\t<input class='form-control' {disabled} data-bind='{bindName}' {requiredAttr} data-invalidMessage='فقط عداد' data-inputmask=\"'regex':'^[\\u06F0-\\u06F90-9]+$' , 'placeholder' : ''\" asp-for='{prop.PropertyName}' />");
					sb.AppendLine($"\t\t\t<div data-invalidmessagespan class='my-1 mx-1'> <span class='text-danger'></span> </div>");
					sb.AppendLine($"\t\t</div>");
					break;

				case SystemType.Select:
					var enumType = !string.IsNullOrEmpty(prop.EnumName) ? $"{formDefinition.Namespace ?? $"Entities.App.{formDefinition.Module}"}.Enums.{prop.EnumName}" : prop.RelatedEntityFullName;
					sb.AppendLine($"\t\t<div class='{colSize}'>");
					sb.AppendLine($"\t\t\t<label class='{requiredClass}'>{prop.DisplayName}</label>");
					sb.AppendLine($"\t\t\t<select class='form-control' data-bind='{bindName}' {requiredAttr} asp-for='{prop.PropertyName}' asp-items=\"Html.GetEnumSelectList(typeof({enumType}))\">");
					sb.AppendLine($"\t\t\t</select>");
					sb.AppendLine($"\t\t\t<div data-invalidmessagespan class='my-1 mx-1'> <span class='text-danger'></span> </div>");
					sb.AppendLine($"\t\t</div>");
					break;

				case SystemType.Entity:
					var relatedEntityName = prop.RelatedEntityName ?? "BaseEntity";
					var displayProp = GetDisplayPropertyName(prop);
					var searchProp = displayProp; // Use same property for search
					var displayTemplate = !string.IsNullOrEmpty(prop.DisplayTemplate) 
						? prop.DisplayTemplate 
						: $"{{{displayProp}}}";
					
					// Extract all properties used in template for projection
					var templateProps = ExtractPropertiesFromTemplate(displayTemplate);
					if (!templateProps.Any(p => p.Equals("Id", StringComparison.OrdinalIgnoreCase) || p.StartsWith("Id.", StringComparison.OrdinalIgnoreCase)))
						templateProps.Insert(0, "Id");
					if (!templateProps.Any(p => p.Equals(displayProp, StringComparison.OrdinalIgnoreCase) || p.EndsWith($".{displayProp}", StringComparison.OrdinalIgnoreCase)) && string.IsNullOrEmpty(prop.DisplayTemplate))
						templateProps.Add(displayProp);
					
					var selectorId = $"{prop.PropertyName}Selector";
					var bindProperty = $"{prop.PropertyName}Id";
					var bindNameProperty = $"{prop.PropertyName}Name";
					var selectedIdValue = $"Model?.{prop.PropertyName}Id?.ToString()";
					
					// Build projection object (handles nested properties)
					var projectionProps = BuildProjectionForTemplate(templateProps);
					
					// Build search columns (handles nested properties)
					var searchColumns = BuildSearchColumnsForTemplate(templateProps);
					
					sb.AppendLine($"\t\t<div class=\"{colSize}\">");
					sb.AppendLine($"\t\t\t<label class='{requiredClass}'>{prop.DisplayName}</label>");
					sb.AppendLine($"\t\t\t@(Html.EntitySelector<{relatedEntityName}>(");
					sb.AppendLine($"\t\t\t\t\"{selectorId}\",");
					sb.AppendLine($"\t\t\t\tc => c.IsActive == Entities.Base.IsActiveEnum.Active,");
					sb.AppendLine($"\t\t\t\tc => new");
					sb.AppendLine($"\t\t\t\t{{");
					sb.AppendLine($"\t\t\t\t\t{projectionProps}");
					sb.AppendLine($"\t\t\t\t}},");
					sb.AppendLine($"\t\t\t\tc => new {{ {searchColumns} }},");
					sb.AppendLine($"\t\t\t\t\"{displayTemplate}\",");
					sb.AppendLine($"\t\t\t\t\"{bindProperty}\",");
					sb.AppendLine($"\t\t\t\t\"{bindNameProperty}\",");
					sb.AppendLine($"\t\t\t\t{selectedIdValue}");
					sb.AppendLine($"\t\t\t))");
					sb.AppendLine($"\t\t\t<div data-invalidmessagespan class='my-1 mx-1'> <span class='text-danger'></span> </div>");
					sb.AppendLine($"\t\t</div>");
					break;

				case SystemType.File:
					sb.AppendLine($"\t\t<div class='{colSize}'>");
					sb.AppendLine($"\t\t\t<fileuploader bind=\"{bindName}Id\" accepted-file-types=\"{prop.FileTypes}\"");
					sb.AppendLine($"\t\t\t\tfile-id=\"@Model?.{prop.PropertyName}Id\"");
					sb.AppendLine($"\t\t\t\tfile-entity=\"@Model?.{prop.PropertyName}\"");
					sb.AppendLine($"\t\t\t\tentity-prop-name=\"{prop.PropertyName}\"");
					sb.AppendLine($"\t\t\t\tentity-type=\"{formDefinition.EntityName}\"");
					sb.AppendLine($"\t\t\t\tmax-file-size=\"{prop.MaxFileSize}\"");
					sb.AppendLine($"\t\t\t\tlabel=\"{prop.DisplayName}\"></fileuploader>");
					sb.AppendLine($"\t\t</div>");
					break;

				case SystemType.Date:
				case SystemType.DateShamsi:
					sb.AppendLine($"\t\t<div class='{colSize}'>");
					sb.AppendLine($"\t\t\t<label class='{requiredClass}'>{prop.DisplayName}</label>");
					sb.AppendLine($"\t\t\t<input class='form-control' {requiredAttr} data-bind='{bindName}' data-persionDatePicker=\"true\" persion-datetimepicker='{{\"format\": \"YYYY/MM/DD\",\"autoClose\": true,\"initialValue\": true}}' value='@Model?.{prop.PropertyName}' />");
					sb.AppendLine($"\t\t\t<div data-invalidmessagespan class='my-1 mx-1'> <span class='text-danger'></span> </div>");
					sb.AppendLine($"\t\t</div>");
					break;

				case SystemType.DateTime:
				case SystemType.DateTimeShamsi:
					sb.AppendLine($"\t\t<div class='{colSize}'>");
					sb.AppendLine($"\t\t\t<label class='{requiredClass}'>{prop.DisplayName}</label>");
					sb.AppendLine($"\t\t\t<input class='form-control' {requiredAttr} data-bind='{bindName}' data-persionDatePicker=\"true\" persion-datetimepicker='{{\"format\": \"YYYY/MM/DD HH:mm:ss\",\"autoClose\": true,\"initialValue\": true,\"timePicker\":{{\"enabled\": true}}}}' value='@Model?.{prop.PropertyName}' />");
					sb.AppendLine($"\t\t\t<div data-invalidmessagespan class='my-1 mx-1'> <span class='text-danger'></span> </div>");
					sb.AppendLine($"\t\t</div>");
					break;

				case SystemType.ListString:
				case SystemType.ListLong:
					// These require custom JavaScript handling - simplified version
					sb.AppendLine($"\t\t<div class='{colSize}'>");
					sb.AppendLine($"\t\t\t<label class='{requiredClass}'>{prop.DisplayName}</label>");
					sb.AppendLine($"\t\t\t<input class='form-control' data-bind='{bindName}' value='@(Model?.{prop.PropertyName} != null ? string.Join(\",\", Model.{prop.PropertyName}) : \"\")' />");
					sb.AppendLine($"\t\t\t<div data-invalidmessagespan class='my-1 mx-1'> <span class='text-danger'></span> </div>");
					sb.AppendLine($"\t\t</div>");
					break;
			}

			return sb.ToString();
		}

	private string GeneratePartialView(FormProperty prop, string parentEntityName, FormDefinition formDefinition)
	{
		var sb = new StringBuilder();
		var childEntityName = prop.ChildEntityName;
		var arrayPropertyName = prop.PropertyName;
		var namespacePrefix = formDefinition.Namespace ?? $"Entities.App.{formDefinition.Module}";

		// Collect all namespaces needed for partial view
		var namespaces = new HashSet<string> { namespacePrefix };
		
		// Add namespaces for Entity properties in child properties
		if (prop.ChildProperties != null)
		{
			foreach (var childProp in prop.ChildProperties.Where(cp => cp.SystemType == SystemType.Entity))
			{
				if (!string.IsNullOrEmpty(childProp.RelatedEntityFullName))
				{
					var lastDotIndex = childProp.RelatedEntityFullName.LastIndexOf('.');
					if (lastDotIndex > 0)
					{
						var entityNamespace = childProp.RelatedEntityFullName.Substring(0, lastDotIndex);
						namespaces.Add(entityNamespace);
					}
				}
			}
			
			// Add namespaces for Enum properties in child properties
			foreach (var childProp in prop.ChildProperties.Where(cp => cp.SystemType == SystemType.Select && !string.IsNullOrEmpty(cp.EnumName)))
			{
				var enumNamespace = $"{namespacePrefix}.Enums";
				namespaces.Add(enumNamespace);
			}
		}
		
		// Write all using statements
		foreach (var ns in namespaces.OrderBy(n => n))
		{
			sb.AppendLine($"@using {ns};");
		}
		
		sb.AppendLine($"@model {childEntityName}");
			sb.AppendLine("<div class=\"entity-item\">");
			sb.AppendLine($"\t<input class='form-control' type=\"hidden\" data-bind='{arrayPropertyName.ToCamelCase()}.id' value='@Model?.Id' />");

			foreach (var childProp in prop.ChildProperties.Where(p => p.SystemType != SystemType.ListEntity).OrderBy(p => p.OrderIndex))
			{
				sb.AppendLine(GeneratePartialViewField(childProp, arrayPropertyName));
			}

			sb.AppendLine("</div>");

			return sb.ToString();
		}

		private string GeneratePartialViewField(FormProperty prop, string arrayPropertyName)
		{
			var sb = new StringBuilder();
			var bindPath = $"{arrayPropertyName.ToCamelCase()}.{prop.PropertyName.ToCamelCase()}";
			var requiredClass = prop.Required ? "required" : "";
			var requiredAttr = prop.Required ? "required" : "";

			switch (prop.SystemType)
			{
				case SystemType.String:
					sb.AppendLine($"\t<div class='col-md-3'>");
					sb.AppendLine($"\t\t<label class='{requiredClass}'>{prop.DisplayName}</label>");
					sb.AppendLine($"\t\t<input class='form-control' data-bind='{bindPath}' {requiredAttr} value='@Model?.{prop.PropertyName}' />");
					sb.AppendLine($"\t\t<div data-invalidmessagespan class='my-1 mx-1'> <span class='text-danger'></span> </div>");
					sb.AppendLine($"\t</div>");
					break;

				case SystemType.Boolean:
					sb.AppendLine($"\t<div class='col-md-3 align-content-center'>");
					sb.AppendLine($"\t\t<div class=\"form-check\">");
					sb.AppendLine($"\t\t\t<input data-bind='{bindPath}' class=\"form-check-input\" type=\"checkbox\" value='@Model?.{prop.PropertyName}' />");
					sb.AppendLine($"\t\t\t<label class=\"form-check-label\">");
					sb.AppendLine($"\t\t\t\t{prop.DisplayName}");
					sb.AppendLine($"\t\t\t</label>");
					sb.AppendLine($"\t\t</div>");
					sb.AppendLine($"\t</div>");
					break;

				case SystemType.Int:
				case SystemType.Long:
				case SystemType.Decimal:
					sb.AppendLine($"\t<div class='col-md-3'>");
					sb.AppendLine($"\t\t<label class='{requiredClass}'>{prop.DisplayName}</label>");
					sb.AppendLine($"\t\t<input class='form-control' data-bind='{bindPath}' {requiredAttr} data-invalidMessage='فقط عداد' data-inputmask=\"'regex':'^[\\u06F0-\\u06F90-9]+$' , 'placeholder' : ''\" value='@Model?.{prop.PropertyName}' />");
					sb.AppendLine($"\t\t<div data-invalidmessagespan class='my-1 mx-1'> <span class='text-danger'></span> </div>");
					sb.AppendLine($"\t</div>");
					break;

			case SystemType.Select:
				// For partial views, we don't have access to formDefinition, so we'll just use the enum name
				// The namespace will be added at the top of the partial view
				sb.AppendLine($"\t<div class='col-md-3'>");
				sb.AppendLine($"\t\t<label class='{requiredClass}'>{prop.DisplayName}</label>");
				sb.AppendLine($"\t\t<select class='form-control' data-bind='{bindPath}' {requiredAttr} asp-for='{prop.PropertyName}' asp-items=\"Html.GetEnumSelectList(typeof({prop.EnumName}))\">");
				sb.AppendLine($"\t\t</select>");
				sb.AppendLine($"\t\t<div data-invalidmessagespan class='my-1 mx-1'> <span class='text-danger'></span> </div>");
				sb.AppendLine($"\t</div>");
				break;

				case SystemType.Entity:
					var nestedRelatedEntityName = prop.RelatedEntityName ?? "BaseEntity";
					var nestedDisplayProp = GetDisplayPropertyName(prop);
					var nestedDisplayTemplate = !string.IsNullOrEmpty(prop.DisplayTemplate) 
						? prop.DisplayTemplate 
						: $"{{{nestedDisplayProp}}}";
					
					// Extract all properties used in template for projection
					var nestedTemplateProps = ExtractPropertiesFromTemplate(nestedDisplayTemplate);
					if (!nestedTemplateProps.Any(p => p.Equals("Id", StringComparison.OrdinalIgnoreCase) || p.StartsWith("Id.", StringComparison.OrdinalIgnoreCase)))
						nestedTemplateProps.Insert(0, "Id");
					if (!nestedTemplateProps.Any(p => p.Equals(nestedDisplayProp, StringComparison.OrdinalIgnoreCase) || p.EndsWith($".{nestedDisplayProp}", StringComparison.OrdinalIgnoreCase)) && string.IsNullOrEmpty(prop.DisplayTemplate))
						nestedTemplateProps.Add(nestedDisplayProp);
					
					var nestedSelectorId = $"{arrayPropertyName.ToCamelCase()}{prop.PropertyName}Selector";
					var nestedBindProperty = $"{bindPath}Id";
					var nestedBindNameProperty = $"{bindPath}Name";
					var nestedSelectedIdValue = $"Model?.{prop.PropertyName}Id?.ToString()";
					
					// Build projection object (handles nested properties)
					var nestedProjectionProps = BuildProjectionForTemplate(nestedTemplateProps);
					
					// Build search columns (handles nested properties)
					var nestedSearchColumns = BuildSearchColumnsForTemplate(nestedTemplateProps);
					
					sb.AppendLine($"\t<div class='col-md-3'>");
					sb.AppendLine($"\t\t<label class='{requiredClass}'>{prop.DisplayName}</label>");
					sb.AppendLine($"\t\t@(Html.EntitySelector<{nestedRelatedEntityName}>(");
					sb.AppendLine($"\t\t\t\"{nestedSelectorId}\",");
					sb.AppendLine($"\t\t\tc => c.IsActive == Entities.Base.IsActiveEnum.Active,");
					sb.AppendLine($"\t\t\tc => new");
					sb.AppendLine($"\t\t\t{{");
					sb.AppendLine($"\t\t\t\t{nestedProjectionProps}");
					sb.AppendLine($"\t\t\t}},");
					sb.AppendLine($"\t\t\tc => new {{ {nestedSearchColumns} }},");
					sb.AppendLine($"\t\t\t\"{nestedDisplayTemplate}\",");
					sb.AppendLine($"\t\t\t\"{nestedBindProperty}\",");
					sb.AppendLine($"\t\t\t\"{nestedBindNameProperty}\",");
					sb.AppendLine($"\t\t\t{nestedSelectedIdValue}");
					sb.AppendLine($"\t\t))");
					sb.AppendLine($"\t\t<div data-invalidmessagespan class='my-1 mx-1'> <span class='text-danger'></span> </div>");
					sb.AppendLine($"\t</div>");
					break;

				case SystemType.File:
					sb.AppendLine($"\t<div class='col-md-3'>");
					sb.AppendLine($"\t\t<fileuploader bind=\"{bindPath}Id\" accepted-file-types=\"{prop.FileTypes}\"");
					sb.AppendLine($"\t\t\tfile-id=\"@Model?.{prop.PropertyName}Id\"");
					sb.AppendLine($"\t\t\tfile-entity=\"@Model?.{prop.PropertyName}\"");
					sb.AppendLine($"\t\t\tentity-prop-name=\"{prop.PropertyName}\"");
					sb.AppendLine($"\t\t\tmax-file-size=\"{prop.MaxFileSize}\"");
					sb.AppendLine($"\t\t\tlabel=\"{prop.DisplayName}\"></fileuploader>");
					sb.AppendLine($"\t</div>");
					break;
			}

			return sb.ToString();
		}

		private string GetDisplayPropertyName(FormProperty prop)
		{
			// Try to get display property from SearchPath if available
			// SearchPath format is usually "PropertyName.DisplayProperty" for Entity types
			if (!string.IsNullOrEmpty(prop.SearchPath) && prop.SearchPath.Contains('.'))
			{
				var parts = prop.SearchPath.Split('.');
				if (parts.Length > 1)
				{
					return parts[1]; // Return the display property name
				}
			}

			// Default to common property names
			// Most entities use Title or Name for display
			// We'll default to Title as it's more common in this codebase
			return "Id";
		}

		private List<string> ExtractPropertiesFromTemplate(string template)
		{
			var properties = new List<string>();
			if (string.IsNullOrEmpty(template))
				return properties;

			// Extract all {PropertyName} or {Nested.Property.Name} patterns from template
			var regex = new System.Text.RegularExpressions.Regex(@"\{([^}]+)\}");
			var matches = regex.Matches(template);
			
			foreach (System.Text.RegularExpressions.Match match in matches)
			{
				if (match.Groups.Count > 1)
				{
					var propPath = match.Groups[1].Value;
					if (!properties.Contains(propPath, StringComparer.OrdinalIgnoreCase))
					{
						properties.Add(propPath);
					}
				}
			}

			return properties;
		}

		private string BuildProjectionForTemplate(List<string> templateProps, string entityVarName = "c")
		{
			var projectionParts = new List<string>();
			var includedProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (var propPath in templateProps.Distinct())
			{
				if (propPath.Contains('.'))
				{
					// Nested property like "Parent.Parent.Title" or "Contact.City.Name"
					// We need to create a flattened property name for the projection
					// e.g., "Parent_Parent_Title" = c.Parent.Parent.Title
					var flattenedName = propPath.Replace(".", "_");
					if (!includedProperties.Contains(flattenedName))
					{
						projectionParts.Add($"{flattenedName} = {entityVarName}.{propPath}");
						includedProperties.Add(flattenedName);
					}
				}
				else
				{
					// Direct property
					if (!includedProperties.Contains(propPath))
					{
						projectionParts.Add($"{propPath} = {entityVarName}.{propPath}");
						includedProperties.Add(propPath);
					}
				}
			}

			// Always include Id if not already included
			if (!templateProps.Any(p => p.Equals("Id", StringComparison.OrdinalIgnoreCase) || p.StartsWith("Id.", StringComparison.OrdinalIgnoreCase)))
			{
				if (!includedProperties.Contains("Id"))
				{
					projectionParts.Insert(0, $"Id = {entityVarName}.Id");
				}
			}

			return string.Join(", ", projectionParts);
		}

		private string BuildSearchColumnsForTemplate(List<string> templateProps, string entityVarName = "c")
		{
			var searchParts = new List<string>();
			var includedDirectProps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (var propPath in templateProps.Distinct())
			{
				if (propPath.Equals("Id", StringComparison.OrdinalIgnoreCase))
					continue;

				if (propPath.Contains('.'))
				{
					// Nested property like "Contact.City.Name"
					// For search, we need to include the navigation property and then the nested property
					var parts = propPath.Split('.');
					if (parts.Length >= 2)
					{
						// Add navigation property for search (e.g., c.Contact)
						var navProperty = parts[0];
						if (!includedDirectProps.Contains(navProperty))
						{
							searchParts.Add($"{entityVarName}.{navProperty}");
							includedDirectProps.Add(navProperty);
						}
					}
				}
				else
				{
					// Direct property
					if (!includedDirectProps.Contains(propPath))
					{
						searchParts.Add($"{entityVarName}.{propPath}");
						includedDirectProps.Add(propPath);
					}
				}
			}

			return string.Join(", ", searchParts);
		}

		private string MakeValidIdentifier(string input)
		{
			if (string.IsNullOrWhiteSpace(input))
				return "Value";

			// Remove invalid characters and replace spaces/hyphens
			var result = new StringBuilder();
			bool lastWasSeparator = false;

			foreach (var c in input)
			{
				// Allow English letters, digits, and underscore
				if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_')
				{
					result.Append(c);
					lastWasSeparator = false;
				}
				// Replace spaces, hyphens, and other separators with underscore (but not consecutive)
				else if (char.IsWhiteSpace(c) || c == '-' || c == '.' || c == '/')
				{
					if (!lastWasSeparator && result.Length > 0)
					{
						result.Append('_');
						lastWasSeparator = true;
					}
				}
				// Skip all other characters (including Persian/Arabic)
			}

			var identifier = result.ToString().Trim('_');

			// Ensure it doesn't start with a digit
			if (identifier.Length > 0 && char.IsDigit(identifier[0]))
				identifier = "_" + identifier;

			// If result is empty or invalid, return a default
			if (string.IsNullOrWhiteSpace(identifier))
				return "Value";

			return identifier;
		}
	}
}

