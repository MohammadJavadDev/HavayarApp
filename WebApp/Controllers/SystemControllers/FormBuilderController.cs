using Common.Attributes;
using Common.Auth.Enums;
using Common.Entities.EntityMetadatas;
using Data.Contracts;
using Entities.Base;
using Entities.Base.DataTable;
using Entities.Base.FormBuilder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.InMemoryData;
using WebApp.Services;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.SystemControllers
{
	[Route("Panel/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("فرم ساز", typeof(FormDefinition))]
	public class FormBuilderController : BaseController
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IFormBuilderCodeGenerator _codeGenerator;
		private readonly IEntityMetadataCache _entityMetadataCache;
		private readonly IWebHostEnvironment _webHostEnvironment;
		private readonly Data.ApplicationDbContext _dbContext;

		public FormBuilderController(
			IUnitOfWork unitOfWork,
			IFormBuilderCodeGenerator codeGenerator,
			IEntityMetadataCache entityMetadataCache,
			IWebHostEnvironment webHostEnvironment,
			Data.ApplicationDbContext dbContext)
		{
			_unitOfWork = unitOfWork;
			_codeGenerator = codeGenerator;
			_entityMetadataCache = entityMetadataCache;
			_webHostEnvironment = webHostEnvironment;
			_dbContext = dbContext;
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(FormDefinition formDefinition, CancellationToken cn)
		{
			NormalizeFormDefinitionGraph(formDefinition);

			if (formDefinition.Id == null || formDefinition.Id == 0)
			{
				return await Add(formDefinition, cn);
			}
			var exist = await _unitOfWork.Repository<FormDefinition>().TableNoTracking.AnyAsync(c => c.Id == formDefinition.Id, cn);
			if (exist)
			{
				return await Update(formDefinition, cn);
			}
			return await Add(formDefinition, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(FormDefinition formDefinition, CancellationToken cn)
		{
			NormalizeFormDefinitionGraph(formDefinition);

			foreach (var p in formDefinition.Properties)
			{
				p.Id = null;
			}

			foreach (var section in formDefinition.Sections)
			{
				section.Id = null;
				foreach (var prop in section.Properties)
				{
					prop.Id = null;
					prop.FormSection = section;
					prop.FormSectionId = null;
				}
			}
			var entity = await _unitOfWork.Repository<FormDefinition>().SaveAsync(formDefinition, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(FormDefinition formDefinition, CancellationToken cn)
		{
			// Detach all tracked entities related to FormDefinition to avoid tracking conflicts
			var trackedEntries = _dbContext.ChangeTracker.Entries()
				.Where(e => e.Entity is FormDefinition ||
						   e.Entity is Entities.Base.FormBuilder.FormProperty ||
						   e.Entity is Entities.Base.FormBuilder.FormPropertyEnumOption ||
						   e.Entity is Entities.Base.FormBuilder.FormSection)
				.ToList();

			foreach (var entry in trackedEntries)
			{
				if (entry.State != EntityState.Detached)
				{
					entry.State = EntityState.Detached;
				}
			}

			NormalizeFormDefinitionGraph(formDefinition);

			// Reset IDs for new properties (those without ID or with ID=0)
			foreach (var prop in formDefinition.Properties)
			{
				if (prop.Id == null || prop.Id == 0)
				{
					ResetPropertyIds(prop);
				}
				else
				{
					// For existing properties, reset child properties and enum options IDs if they are new
					ResetPropertyIds(prop, resetSelf: false);
				}
			}

			if (formDefinition.Sections != null)
			{
				foreach (var section in formDefinition.Sections)
				{
					if (section.Properties == null) continue;
					foreach (var prop in section.Properties)
					{
						prop.FormSection = section;
						if (section.Id == null || section.Id == 0)
						{
							prop.FormSectionId = null;
						}
					}
				}
			}

			// Set FormDefinition navigation property for all properties
			foreach (var prop in formDefinition.Properties)
			{
				prop.FormDefinition = formDefinition;
				foreach (var childProp in prop.ChildProperties)
				{
					childProp.FormDefinition = formDefinition;
					childProp.ParentProperty = prop;
				}
			}

			// Set FormDefinition navigation property for sections and their properties
			foreach (var section in formDefinition.Sections)
			{
				section.FormDefinition = formDefinition;
				foreach (var prop in section.Properties)
				{
					prop.FormDefinition = formDefinition;
					foreach (var childProp in prop.ChildProperties)
					{
						childProp.FormDefinition = formDefinition;
						childProp.ParentProperty = prop;
					}
				}
			}

			// Now update the entity
			var entity = await _unitOfWork.Repository<FormDefinition>().UpdateAsync(formDefinition, cn, true);

			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = _unitOfWork.Repository<FormDefinition>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await _unitOfWork.Repository<FormDefinition>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = _unitOfWork.Repository<FormDefinition>().TableNoTracking
					.AsSplitQuery()
					.Include(c => c.Properties)
						.ThenInclude(p => p.EnumOptions)
					.Include(c => c.Properties)
						.ThenInclude(p => p.ChildProperties)
							.ThenInclude(cp => cp.EnumOptions)
					.Include(c => c.Sections)
						.ThenInclude(s => s.Properties)
							.ThenInclude(p => p.EnumOptions)
					.Include(c => c.Sections)
						.ThenInclude(s => s.Properties)
							.ThenInclude(p => p.ChildProperties)
								.ThenInclude(cp => cp.EnumOptions)
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\System\FormBuilder\Edit.cshtml", entity);
			}
			var newEntity = new FormDefinition();
			return View(@"\Views\Panel\System\FormBuilder\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var newEntity = new FormDefinition();
			return View(@"\Views\Panel\System\FormBuilder\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\System\FormBuilder\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await _unitOfWork.Repository<FormDefinition>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"exportExcel.xlsx");
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message);
			}
		}

		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		[HttpPost("[action]")]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
		{
			return Ok(await _unitOfWork.Repository<FormDefinition>().FetchDataAsync(request, cn));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("تولید کد", ActionAccessType.Api)]
		public IActionResult GenerateCode([FromBody] FormDefinition formDefinition)
		{
			try
			{
				var code = _codeGenerator.GenerateCode(formDefinition);
				var paths = _codeGenerator.GetFilePaths(formDefinition);

				var result = new
				{
					code = new
					{
						entityClass = code.EntityClass,
						controllerClass = code.ControllerClass,
						editView = code.EditView,
						listView = code.ListView,
						partialViews = code.PartialViews,
						enumClasses = code.EnumClasses
					},
					paths = paths,
					existingFiles = paths.Where(kvp => System.IO.File.Exists(kvp.Value)).Select(kvp => kvp.Key).ToList()
				};

				return Ok(result);
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در تولید کد: " + ex.Message);
			}
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره فایل‌ها", ActionAccessType.Api)]
		public async Task<IActionResult> SaveFiles([FromBody] SaveFilesRequest request, CancellationToken cn)
		{
			try
			{
				var formDefinition = await _unitOfWork.Repository<FormDefinition>()
					.TableNoTracking
					.AsSplitQuery()
					.Include(c => c.Properties)
						.ThenInclude(p => p.EnumOptions)
					.Include(c => c.Properties)
						.ThenInclude(p => p.ChildProperties)
							.ThenInclude(cp => cp.EnumOptions)
					.Include(c => c.Sections)
						.ThenInclude(s => s.Properties)
							.ThenInclude(p => p.EnumOptions)
					.Include(c => c.Sections)
						.ThenInclude(s => s.Properties)
							.ThenInclude(p => p.ChildProperties)
								.ThenInclude(cp => cp.EnumOptions)
					.FirstOrDefaultAsync(c => c.Id == request.FormDefinitionId, cn);

				if (formDefinition == null)
					return NotFound("تعریف فرم یافت نشد");

				var code = _codeGenerator.GenerateCode(formDefinition);
				var success = _codeGenerator.SaveAllFiles(formDefinition, code);

				if (success)
				{
					// Refresh entity metadata cache
					_entityMetadataCache.Refresh();
					return Ok(new { message = "فایل‌ها با موفقیت ذخیره شدند" });
				}
				else
				{
					return StatusCode(500, "خطا در ذخیره فایل‌ها");
				}
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در ذخیره فایل‌ها: " + ex.Message);
			}
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت موجودیت‌های موجود", ActionAccessType.Api)]
		public IActionResult GetAvailableEntities()
		{
			try
			{
				var entities = _entityMetadataCache.GetAll()
					.Select(e => new
					{
						name = e.EntityName,
						displayName = e.DisplayName,
						fullName = e.EntityFullName,
						module = e.EntityFullName?.Split('.').ElementAtOrDefault(2) ?? "Unknown",
						schema = e.Schema
					})
					.OrderBy(e => e.module)
					.ThenBy(e => e.name)
					.ToList();

				return Ok(entities);
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در دریافت موجودیت‌ها: " + ex.Message);
			}
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت ماژول‌های موجود", ActionAccessType.Api)]
		public IActionResult GetAvailableModules()
		{
			try
			{
				var modules = _entityMetadataCache.GetAll()
					.Select(e => e.EntityFullName?.Split('.').ElementAtOrDefault(2))
					.Where(m => !string.IsNullOrEmpty(m))
					.Distinct()
					.OrderBy(m => m)
					.ToList();

				return Ok(modules);
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در دریافت ماژول‌ها: " + ex.Message);
			}
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت فیلدهای موجودیت", ActionAccessType.Api)]
		public IActionResult GetEntityProperties([FromQuery] string entityFullName, [FromQuery] int maxDepth = 4)
		{
			try
			{
				if (string.IsNullOrEmpty(entityFullName))
					return BadRequest("نام موجودیت الزامی است");

				var entityMeta = _entityMetadataCache.Get(entityFullName);
				if (entityMeta == null)
					return NotFound("موجودیت یافت نشد");

				var properties = new List<object>();

				// Add direct properties
				properties.AddRange(entityMeta.Properties
					.Where(p => !p.SystemProperty &&
						(new[] { "String", "Int", "Long", "Decimal", "Date", "DateTime", "DateShamsi", "DateTimeShamsi" })
						.Contains(p.DataType, StringComparer.OrdinalIgnoreCase))
					.Select(p => new
					{
						name = p.Name,
						displayName = p.DisplayName,
						dataType = p.DataType,
						systemType = p.SystemType.ToString(),
						fullPath = p.Name,
						isNested = false
					}));

				// Recursively add nested properties
				AddNestedPropertiesRecursive(entityMeta, properties, "", "", maxDepth, 0);

				return Ok(new
				{
					entityName = entityMeta.EntityName,
					displayName = entityMeta.DisplayName,
					properties = properties.OrderBy(p => ((dynamic)p).isNested ? 1 : 0).ThenBy(p => ((dynamic)p).displayName).ToList()
				});
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در دریافت فیلدهای موجودیت: " + ex.Message);
			}
		}

		[HttpPost("[action]")]
		[ActionDisplayName("بارگذاری از موجودیت موجود", ActionAccessType.Api)]
		public IActionResult LoadFromEntity([FromBody] LoadFromEntityRequest request)
		{
			try
			{
				var entityMeta = _entityMetadataCache.Get(request.EntityFullName);
				if (entityMeta == null)
					return NotFound("موجودیت یافت نشد");

				var formDefinition = new FormDefinition
				{
					EntityName = entityMeta.EntityName,
					DisplayName = entityMeta.DisplayName,
					Module = entityMeta.EntityFullName?.Split('.').ElementAtOrDefault(2) ?? "App",
					Schema = entityMeta.Schema,
					Namespace = string.Join(".", entityMeta.EntityFullName?.Split('.').Take(3) ?? new[] { "Entities.App" }),
					IsFromExistingEntity = true,
					SourceEntityFullName = entityMeta.EntityFullName,
					Properties = new List<FormProperty>(),
					Sections = new List<FormSection>()
				};

				// Create default section
				var defaultSection = new FormSection
				{
					Title = "اطلاعات اصلی",
					OrderIndex = 0,
					Properties = new List<FormProperty>()
				};

				int order = 0;
				foreach (var propMeta in entityMeta.Properties.Where(p => !p.SystemProperty))
				{
					var formProp = new FormProperty
					{
						PropertyName = propMeta.Name,
						DisplayName = propMeta.DisplayName,
						SystemType = propMeta.SystemType,
						SearchPath = propMeta.SearchPath,
						AddToTable = propMeta.AddToTable,
						ShowInRelationData = propMeta.ShowInRelationData,
						Required = propMeta.Required,
						FileTypes = propMeta.FileTypes,
						MaxFileSize = propMeta.MaxFileSize,
						Regex = propMeta.Regex,
						RegexInvalidError = propMeta.RegexInvalidError,
						OrderIndex = order++,
						RelatedEntityFullName = propMeta.RelatedEntityTypeFullName,
						RelatedEntityName = propMeta.RelatedEntityType
					};

					// Handle enum options
					if (propMeta.SystemType == SystemType.Select && propMeta.Options != null && propMeta.Options.Any())
					{
						formProp.EnumOptions = propMeta.Options.Select((opt, idx) => new FormPropertyEnumOption
						{
							Value = opt.Value,
							Title = opt.Text,
							EnglishName = MakeValidIdentifier(opt.Text), // Generate from Persian
							OrderIndex = idx
						}).ToList();
					}

					defaultSection.Properties.Add(formProp);
				}

				formDefinition.Sections.Add(defaultSection);

				return Ok(formDefinition);
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در بارگذاری موجودیت: " + ex.Message);
			}
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی JSON", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToJson([FromBody] FormDefinition formDefinition, [FromQuery] bool preview = false, CancellationToken cn = default)
		{
			try
			{
				// Collect all properties data
				if (formDefinition.Properties == null || !formDefinition.Properties.Any())
				{
					return BadRequest("هیچ ویژگی‌ای برای export وجود ندارد");
				}

				// Load full FormDefinition from database if ID is provided
				FormDefinition fullFormDefinition = null;
				if (formDefinition.Id.HasValue && formDefinition.Id.Value > 0)
				{
					fullFormDefinition = await _unitOfWork.Repository<FormDefinition>()
						.TableNoTracking
						.AsSplitQuery()
						.Include(f => f.Properties)
							.ThenInclude(p => p.EnumOptions)
						.Include(f => f.Properties)
							.ThenInclude(p => p.ChildProperties)
							.ThenInclude(cp => cp.EnumOptions)
						.Include(f => f.Sections)
							.ThenInclude(s => s.Properties)
								.ThenInclude(p => p.EnumOptions)
						.Include(f => f.Sections)
							.ThenInclude(s => s.Properties)
								.ThenInclude(p => p.ChildProperties)
									.ThenInclude(cp => cp.EnumOptions)
						.FirstOrDefaultAsync(f => f.Id == formDefinition.Id.Value, cn);

					if (fullFormDefinition == null)
					{
						return NotFound("تعریف فرم یافت نشد");
					}
				}
				else
				{
					// Use provided formDefinition but ensure all properties are loaded
					fullFormDefinition = formDefinition;
				}

				// Serialize to JSON with proper options
				var jsonOptions = new System.Text.Json.JsonSerializerOptions
				{
					WriteIndented = true,
					ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
					DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
					PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
					Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
				};

				var json = System.Text.Json.JsonSerializer.Serialize(fullFormDefinition, jsonOptions);

				// If preview mode, return as JSON text (not file download)
				if (preview)
				{
					return Content(json, "application/json", System.Text.Encoding.UTF8);
				}

				// Return as file download
				var fileName = $"{fullFormDefinition.EntityName ?? "FormDefinition"}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
				var bytes = System.Text.Encoding.UTF8.GetBytes(json);
				return File(bytes, "application/json", fileName);
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در ایجاد فایل JSON: " + ex.Message);
			}
		}

		[HttpPost("[action]")]
		[ActionDisplayName("اعتبارسنجی مسیرها", ActionAccessType.Api)]
		public IActionResult ValidatePaths([FromBody] FormDefinition formDefinition)
		{
			try
			{
				var paths = _codeGenerator.GetFilePaths(formDefinition);
				var existingFiles = new Dictionary<string, bool>();

				foreach (var kvp in paths)
				{
					existingFiles[kvp.Key] = System.IO.File.Exists(kvp.Value);
				}

				return Ok(new
				{
					paths = paths,
					existingFiles = existingFiles,
					hasConflicts = existingFiles.Any(kvp => kvp.Value)
				});
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در اعتبارسنجی مسیرها: " + ex.Message);
			}
		}
		[HttpGet("[action]")]
		public IActionResult PropertyRowPartial()
		{
			return PartialView(@"\Views\Panel\System\FormBuilder\_PropertyRowPartial.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ترجمه نام", ActionAccessType.Api)]
		public async Task<IActionResult> TranslatePropertyName([FromBody] TranslateRequest request)
		{
			try
			{
				// LM Studio configuration
				var _baseUrl = "http://localhost:1234/v1/chat/completions";
				var _model = "openai/gpt-oss-20b"; // As requested

				var payload = new
				{
					model = _model,
					messages = new[]
					{
						new
						{
							role = "system",
							content = @"You are a professional .NET naming assistant.

Task:
Convert the following Persian or English field name into a clean and valid C# property name.

Rules:
- Output MUST be only ONE word (no explanation).
- Use PascalCase.
- Use correct English meaning.
- Do NOT add punctuation.
- Do NOT return quotes.
- Do NOT return camelCase or snake_case.
- If the phrase contains multiple words, merge them in PascalCase.
- Remove spaces, hyphens, and non-alphanumeric characters.
- Always choose standard English naming that is used in C# data models.

Example Conversions:
'نام' → Name  
'نام خانوادگی' → LastName  
'شماره ملی' → NationalId  
'تاریخ تولد' → BirthDate  
'کد پستی' → PostalCode  
'تلفن همراه' → MobileNumber  

Return ONLY the final C# property name."
						},
						new { role = "user", content = request.Text }
					},
					temperature = 0.7,
					max_tokens = -1,
					stream = false
				};

				using var httpClient = new HttpClient();
				var json = new StringContent(
					System.Text.Json.JsonSerializer.Serialize(payload),
					System.Text.Encoding.UTF8,
					"application/json"
				);

				var response = await httpClient.PostAsync(_baseUrl, json);
				response.EnsureSuccessStatusCode();

				var result = await response.Content.ReadAsStringAsync();
				var parsed = System.Text.Json.JsonSerializer.Deserialize<LmStudioResponse>(result);

				var translatedName = parsed?.choices?.FirstOrDefault()?.message?.content?.Trim() ?? "Property";

				// Clean up result
				translatedName = translatedName.Replace("\"", "").Replace("'", "").Trim();

				return Ok(translatedName);
			}
			catch (Exception ex)
			{
				// Fallback to simple translation
				var fallbackName = SimpleFallbackTranslate(request.Text);
				return Ok(fallbackName);
			}
		}

		private void AddNestedPropertiesRecursive(EntityMetadata currentEntity, List<object> propsList, string pathPrefix, string displayPrefix, int remainingDepth, int currentDepth)
		{
			if (remainingDepth <= 0 || currentDepth >= 4) return;

			foreach (var prop in currentEntity.Properties.Where(p => !p.SystemProperty && p.SystemType == SystemType.Entity && !string.IsNullOrEmpty(p.RelatedEntityTypeFullName)))
			{
				var relatedEntityMeta = _entityMetadataCache.Get(prop.RelatedEntityTypeFullName);
				if (relatedEntityMeta != null)
				{
					var currentPath = string.IsNullOrEmpty(pathPrefix) ? prop.Name : $"{pathPrefix}.{prop.Name}";
					var currentDisplay = string.IsNullOrEmpty(displayPrefix) ? prop.DisplayName : $"{displayPrefix} > {prop.DisplayName}";

					// Add properties from related entity
					var nestedProps = relatedEntityMeta.Properties
						.Where(p => !p.SystemProperty &&
							(new[] { "String", "Int", "Long", "Decimal", "Date", "DateTime", "DateShamsi", "DateTimeShamsi" })
							.Contains(p.DataType, StringComparer.OrdinalIgnoreCase))
						.Select(p => new
						{
							name = p.Name,
							displayName = p.DisplayName,
							dataType = p.DataType,
							systemType = p.SystemType.ToString(),
							fullPath = $"{currentPath}.{p.Name}",
							isNested = true,
							parentEntityName = currentPath,
							parentEntityDisplayName = currentDisplay
						});

					propsList.AddRange(nestedProps);

					// Recursively add deeper nested properties
					if (remainingDepth > 1)
					{
						AddNestedPropertiesRecursive(relatedEntityMeta, propsList, currentPath, currentDisplay, remainingDepth - 1, currentDepth + 1);
					}
				}
			}
		}

		private string SimpleFallbackTranslate(string persianText)
		{
			// Simple fallback translations
			var translations = new Dictionary<string, string>
			{
				{ "نام", "Name" },
				{ "عنوان", "Title" },
				{ "توضیحات", "Description" },
				{ "تاریخ", "Date" },
				{ "کد", "Code" },
				{ "شماره", "Number" },
				{ "مبلغ", "Amount" },
				{ "قیمت", "Price" },
				{ "تعداد", "Count" },
				{ "وضعیت", "Status" }
			};

			foreach (var key in translations.Keys)
			{
				if (persianText.Contains(key))
					return translations[key];
			}

			return "Property";
		}

		private class LmStudioResponse
		{
			public List<Choice>? choices { get; set; }
		}

		private class Choice
		{
			public Message? message { get; set; }
		}

		private class Message
		{
			public string? content { get; set; }
		}

		private string MakeValidIdentifier(string input)
		{
			if (string.IsNullOrWhiteSpace(input))
				return "Value";

			// Remove invalid characters and replace spaces
			var result = new System.Text.StringBuilder();
			foreach (var c in input)
			{
				if (char.IsLetterOrDigit(c) || c == '_')
					result.Append(c);
				else if (char.IsWhiteSpace(c))
					result.Append('_');
			}

			var identifier = result.ToString();

			// Ensure it doesn't start with a digit
			if (identifier.Length > 0 && char.IsDigit(identifier[0]))
				identifier = "_" + identifier;

			return string.IsNullOrWhiteSpace(identifier) ? "Value" : identifier;
		}

		/// <summary>
		/// Reset IDs for a single property and its nested entities (recursive)
		/// </summary>
		private void ResetPropertyIds(FormProperty property, bool resetSelf = true)
		{
			if (resetSelf)
			{
				property.Id = null;
			}

			// Reset child properties IDs
			foreach (var childProp in property.ChildProperties)
			{
				ResetPropertyIds(childProp);
			}

			// Reset enum options IDs
			foreach (var enumOption in property.EnumOptions)
			{
				enumOption.Id = null;
			}
		}

		private void NormalizeFormDefinitionGraph(FormDefinition formDefinition)
		{
			formDefinition.Sections ??= new List<FormSection>();
			formDefinition.Properties ??= new List<FormProperty>();

			if (formDefinition.Sections.Any())
			{
				var orderedSections = formDefinition.Sections
					.Where(section => section != null)
					.OrderBy(section => section.OrderIndex)
					.ToList();

				int sectionIndex = 0;
				foreach (var section in orderedSections)
				{
					section.OrderIndex = sectionIndex++;
					section.FormDefinition = formDefinition;
					section.Properties ??= new List<FormProperty>();

					int propertyIndex = 0;
					foreach (var prop in section.Properties)
					{
						prop.OrderIndex = propertyIndex++;
						prop.FormDefinition = formDefinition;
						prop.FormSection = section;
						if (section.Id == null || section.Id == 0)
						{
							prop.FormSectionId = null;
						}
						else
						{
							prop.FormSectionId = section.Id;
						}
					}
				}

				formDefinition.Sections = orderedSections;
				formDefinition.Properties = orderedSections.SelectMany(s => s.Properties).ToList();
			}
			else if (formDefinition.Properties.Any())
			{
				var defaultSection = new FormSection
				{
					Title = formDefinition.DisplayName ?? "اطلاعات",
					OrderIndex = 0,
					FormDefinition = formDefinition,
					Properties = formDefinition.Properties
				};

				int propertyIndex = 0;
				foreach (var prop in defaultSection.Properties)
				{
					prop.OrderIndex = propertyIndex++;
					prop.FormDefinition = formDefinition;
					prop.FormSection = defaultSection;
					prop.FormSectionId = defaultSection.Id > 0 ? defaultSection.Id : (long?)null;
				}

				formDefinition.Sections = new List<FormSection> { defaultSection };
			}
			else
			{
				formDefinition.Sections = new List<FormSection>();
			}
		}
	}

	public class TranslateRequest
	{
		public string Text { get; set; }
	}

	public class SaveFilesRequest
	{
		public long FormDefinitionId { get; set; }
		public bool OverwriteExisting { get; set; }
	}

	public class LoadFromEntityRequest
	{
		public string EntityFullName { get; set; }
	}
}



