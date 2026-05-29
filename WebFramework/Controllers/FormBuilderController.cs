using Common.Attributes;
using Common.Auth.Enums;
using Common.Entities.EntityMetadatas;
using Data.Contracts;
using Entities.Base;
using Entities.Base.DataTable;
using Entities.Base.FormBuilder;
using Entities.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data.Services.QueryBuilderServices;
using WebFramework.Abstractions;
using WebFramework.Filtters;
using WebFramework.Page;
using WebFramework.Services;

namespace WebFramework.Controllers
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
		private readonly IEnvironmentService _webHostEnvironment;
		private readonly Data.ApplicationDbContext _dbContext;
		private readonly IDatabaseSchemaService _databaseSchemaService;

		public FormBuilderController(
			IUnitOfWork unitOfWork,
			IFormBuilderCodeGenerator codeGenerator,
			IEntityMetadataCache entityMetadataCache,
			IEnvironmentService webHostEnvironment,
			Data.ApplicationDbContext dbContext,
			IDatabaseSchemaService databaseSchemaService)
		{
			_unitOfWork = unitOfWork;
			_codeGenerator = codeGenerator;
			_entityMetadataCache = entityMetadataCache;
			_webHostEnvironment = webHostEnvironment;
			_dbContext = dbContext;
			_databaseSchemaService = databaseSchemaService;
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(FormDefinition formDefinition, CancellationToken cn)
		{
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
			// مرحله 1: ذخیره FormDefinition (بدون فرزندان)
			var newFormDefinition = new FormDefinition
			{
				EntityName = formDefinition.EntityName,
				DisplayName = formDefinition.DisplayName,
				Module = formDefinition.Module,
				Schema = formDefinition.Schema,
				Namespace = formDefinition.Namespace,
				BaseEntityType = formDefinition.BaseEntityType,
				Description = formDefinition.Description,
				IsFromExistingEntity = formDefinition.IsFromExistingEntity,
				SourceEntityFullName = formDefinition.SourceEntityFullName,
				Sections = new List<FormSection>()
			};

			var savedFormDefinition = await _unitOfWork.Repository<FormDefinition>().SaveAsync(newFormDefinition, cn, true);

			// مرحله 2: ذخیره Sections (بدون Properties)
			if (formDefinition.Sections != null && formDefinition.Sections.Any())
			{
				foreach (var section in formDefinition.Sections.OrderBy(s => s.OrderIndex))
				{
					var newSection = new FormSection
					{
						FormDefinitionId = savedFormDefinition.Id.Value,
						Title = section.Title,
						OrderIndex = section.OrderIndex,
						Properties = new List<FormProperty>()
					};

					_dbContext.Set<FormSection>().Add(newSection);
					await _dbContext.SaveChangesAsync(cn);

					// مرحله 3: ذخیره Properties سطح اول (بدون EnumOptions و ChildProperties)
					if (section.Properties != null && section.Properties.Any())
					{
						foreach (var prop in section.Properties.OrderBy(p => p.OrderIndex))
						{
							var newProperty = new FormProperty
							{
								FormDefinitionId = savedFormDefinition.Id.Value,
								FormSectionId = newSection.Id.Value,
								PropertyName = prop.PropertyName,
								DisplayName = prop.DisplayName,
								SystemType = prop.SystemType,
								SearchPath = prop.SearchPath,
								AddToTable = prop.AddToTable,
								SystemProperty = prop.SystemProperty,
								ShowInRelationData = prop.ShowInRelationData,
								Required = prop.Required,
								FileTypes = prop.FileTypes,
								MaxFileSize = prop.MaxFileSize,
								Regex = prop.Regex,
								RegexInvalidError = prop.RegexInvalidError,
								MaxLength = prop.MaxLength,
								AutoNumberStart = prop.AutoNumberStart,
								AutoNumberStep = prop.AutoNumberStep,
								RelatedEntityFullName = prop.RelatedEntityFullName,
								RelatedEntityName = prop.RelatedEntityName,
								EnumName = prop.EnumName,
								ChildEntityName = prop.ChildEntityName,
								ChildEntityDisplayName = prop.ChildEntityDisplayName,
								ColSize = prop.ColSize,
								OrderIndex = prop.OrderIndex,
								DisplayTemplate = prop.DisplayTemplate,
								EnumOptions = new List<FormPropertyEnumOption>(),
								ChildProperties = new List<FormProperty>()
							};

							_dbContext.Set<FormProperty>().Add(newProperty);
							await _dbContext.SaveChangesAsync(cn);

							// مرحله 4: ذخیره EnumOptions
							if (prop.EnumOptions != null && prop.EnumOptions.Any())
							{
								foreach (var enumOption in prop.EnumOptions.OrderBy(e => e.OrderIndex))
								{
									var newEnumOption = new FormPropertyEnumOption
									{
										FormPropertyId = newProperty.Id.Value,
										Value = enumOption.Value,
										Title = enumOption.Title,
										EnglishName = enumOption.EnglishName,
										OrderIndex = enumOption.OrderIndex
									};

									_dbContext.Set<FormPropertyEnumOption>().Add(newEnumOption);
								}
								await _dbContext.SaveChangesAsync(cn);
							}

							// مرحله 5: ذخیره ChildProperties به صورت بازگشتی
							if (prop.ChildProperties != null && prop.ChildProperties.Any())
							{
								await SaveChildPropertiesRecursively(prop.ChildProperties, newProperty.Id.Value, savedFormDefinition.Id.Value, cn);
							}
						}
					}
				}
			}

			// بازخوانی کل FormDefinition با تمام فرزندان
			var result = await _unitOfWork.Repository<FormDefinition>()
				.TableNoTracking
				.AsSplitQuery()
				.Include(f => f.Sections)
					.ThenInclude(s => s.Properties)
						.ThenInclude(p => p.EnumOptions)
				.Include(f => f.Sections)
					.ThenInclude(s => s.Properties)
						.ThenInclude(p => p.ChildProperties)
							.ThenInclude(cp => cp.EnumOptions)
				.FirstOrDefaultAsync(f => f.Id == savedFormDefinition.Id, cn);

			return Ok(result);
		}

		/// <summary>
		/// ذخیره بازگشتی ChildProperties
		/// </summary>
		private async Task SaveChildPropertiesRecursively(
			ICollection<FormProperty> childProperties,
			long parentPropertyId,
			long formDefinitionId,
			CancellationToken cn)
		{
			if (childProperties == null || !childProperties.Any()) return;

			foreach (var childProp in childProperties.OrderBy(c => c.OrderIndex))
			{
				var newChildProperty = new FormProperty
				{
					FormDefinitionId = formDefinitionId,
					ParentPropertyId = parentPropertyId,
					PropertyName = childProp.PropertyName,
					DisplayName = childProp.DisplayName,
					SystemType = childProp.SystemType,
					SearchPath = childProp.SearchPath,
					AddToTable = childProp.AddToTable,
					SystemProperty = childProp.SystemProperty,
					ShowInRelationData = childProp.ShowInRelationData,
					Required = childProp.Required,
					FileTypes = childProp.FileTypes,
					MaxFileSize = childProp.MaxFileSize,
					Regex = childProp.Regex,
					RegexInvalidError = childProp.RegexInvalidError,
					MaxLength = childProp.MaxLength,
					AutoNumberStart = childProp.AutoNumberStart,
					AutoNumberStep = childProp.AutoNumberStep,
					RelatedEntityFullName = childProp.RelatedEntityFullName,
					RelatedEntityName = childProp.RelatedEntityName,
					EnumName = childProp.EnumName,
					ChildEntityName = childProp.ChildEntityName,
					ChildEntityDisplayName = childProp.ChildEntityDisplayName,
					ColSize = childProp.ColSize,
					OrderIndex = childProp.OrderIndex,
					DisplayTemplate = childProp.DisplayTemplate,
					EnumOptions = new List<FormPropertyEnumOption>(),
					ChildProperties = new List<FormProperty>()
				};

				_dbContext.Set<FormProperty>().Add(newChildProperty);
				await _dbContext.SaveChangesAsync(cn);

				// ذخیره EnumOptions این فرزند
				if (childProp.EnumOptions != null && childProp.EnumOptions.Any())
				{
					foreach (var enumOption in childProp.EnumOptions.OrderBy(e => e.OrderIndex))
					{
						var newEnumOption = new FormPropertyEnumOption
						{
							FormPropertyId = newChildProperty.Id.Value,
							Value = enumOption.Value,
							Title = enumOption.Title,
							EnglishName = enumOption.EnglishName,
							OrderIndex = enumOption.OrderIndex
						};

						_dbContext.Set<FormPropertyEnumOption>().Add(newEnumOption);
					}
					await _dbContext.SaveChangesAsync(cn);
				}

				// ذخیره بازگشتی فرزندان عمیق‌تر
				if (childProp.ChildProperties != null && childProp.ChildProperties.Any())
				{
					await SaveChildPropertiesRecursively(
						childProp.ChildProperties,
						newChildProperty.Id.Value,
						formDefinitionId,
						cn);
				}
			}
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(FormDefinition formDefinition, CancellationToken cn)
		{
			if (formDefinition.Id == null || formDefinition.Id == 0) return BadRequest("شناسه فرم نامعتبر است");

			// مرحله 1: بارگذاری FormDefinition موجود
			var existingForm = await _unitOfWork.Repository<FormDefinition>()
				.Table
				.Include(f => f.Sections)
					.ThenInclude(s => s.Properties)
						.ThenInclude(p => p.EnumOptions)
				.Include(f => f.Sections)
					.ThenInclude(s => s.Properties)
						.ThenInclude(p => p.ChildProperties)
							.ThenInclude(cp => cp.EnumOptions)
				.FirstOrDefaultAsync(f => f.Id == formDefinition.Id, cn);

			if (existingForm == null) return NotFound("فرم یافت نشد");

			// مرحله 2: بروزرسانی ویژگی‌های اصلی FormDefinition
			existingForm.EntityName = formDefinition.EntityName;
			existingForm.DisplayName = formDefinition.DisplayName;
			existingForm.Module = formDefinition.Module;
			existingForm.Schema = formDefinition.Schema;
			existingForm.Namespace = formDefinition.Namespace;
			existingForm.BaseEntityType = formDefinition.BaseEntityType;
			existingForm.Description = formDefinition.Description;
			existingForm.IsFromExistingEntity = formDefinition.IsFromExistingEntity;
			existingForm.SourceEntityFullName = formDefinition.SourceEntityFullName;

			await _dbContext.SaveChangesAsync(cn);

			// مرحله 3: همگام‌سازی Sections
			await SyncSectionsStepByStep(existingForm, formDefinition, cn);

			// مرحله 4: بازخوانی کل FormDefinition با تمام فرزندان
			var result = await _unitOfWork.Repository<FormDefinition>()
				.TableNoTracking
				.AsSplitQuery()
				.Include(f => f.Sections)
					.ThenInclude(s => s.Properties)
						.ThenInclude(p => p.EnumOptions)
				.Include(f => f.Sections)
					.ThenInclude(s => s.Properties)
						.ThenInclude(p => p.ChildProperties)
							.ThenInclude(cp => cp.EnumOptions)
				.FirstOrDefaultAsync(f => f.Id == formDefinition.Id, cn);

			return Ok(result);
		}
		/// <summary>
		/// همگام‌سازی مرحله‌ای Sections
		/// </summary>
		private async Task SyncSectionsStepByStep(FormDefinition existingForm, FormDefinition incomingForm, CancellationToken cn)
		{
			var incomingSections = incomingForm.Sections ?? new List<FormSection>();

			// مرحله 1: حذف Sections که در داده‌های جدید نیستند
			var incomingSectionIds = incomingSections
				.Where(s => s.Id.HasValue && s.Id > 0)
				.Select(s => s.Id.Value)
				.ToHashSet();

			var sectionsToDelete = existingForm.Sections
				.Where(s => s.Id.HasValue && s.Id > 0 && !incomingSectionIds.Contains(s.Id.Value))
				.ToList();

			foreach (var sectionToDelete in sectionsToDelete)
			{
				_dbContext.Set<FormSection>().Remove(sectionToDelete);
			}

			if (sectionsToDelete.Any())
			{
				await _dbContext.SaveChangesAsync(cn);
			}

			// مرحله 2: بروزرسانی یا افزودن Sections
			foreach (var incomingSection in incomingSections.OrderBy(s => s.OrderIndex))
			{
				FormSection? targetSection = null;

				if (incomingSection.Id.HasValue && incomingSection.Id > 0)
				{
					// بروزرسانی Section موجود
					targetSection = existingForm.Sections.FirstOrDefault(s => s.Id == incomingSection.Id);
					if (targetSection != null)
					{
						targetSection.Title = incomingSection.Title;
						targetSection.OrderIndex = incomingSection.OrderIndex;
						await _dbContext.SaveChangesAsync(cn);
					}
				}
				else
				{
					// افزودن Section جدید
					var newSection = new FormSection
					{
						FormDefinitionId = existingForm.Id.Value,
						Title = incomingSection.Title,
						OrderIndex = incomingSection.OrderIndex,
						Properties = new List<FormProperty>()
					};

					_dbContext.Set<FormSection>().Add(newSection);
					await _dbContext.SaveChangesAsync(cn);

					targetSection = newSection;
					existingForm.Sections.Add(newSection);
				}

				// مرحله 3: همگام‌سازی Properties این Section
				if (targetSection != null && incomingSection.Properties != null)
				{
					await SyncPropertiesStepByStep(targetSection, incomingSection.Properties, existingForm.Id.Value, cn);
				}
			}
		}
		/// <summary>
		/// همگام‌سازی مرحله‌ای Properties
		/// </summary>
		private async Task SyncPropertiesStepByStep(
			FormSection targetSection,
			List<FormProperty> incomingProperties,
			long formDefinitionId,
			CancellationToken cn)
		{
			incomingProperties ??= new List<FormProperty>();

			// مرحله 1: حذف Properties که در داده‌های جدید نیستند
			var incomingPropertyIds = incomingProperties
				.Where(p => p.Id.HasValue && p.Id > 0)
				.Select(p => p.Id.Value)
				.ToHashSet();

			var propertiesToDelete = targetSection.Properties
				.Where(p => p.Id.HasValue && p.Id > 0 && !incomingPropertyIds.Contains(p.Id.Value))
				.ToList();

			foreach (var propertyToDelete in propertiesToDelete)
			{
				_dbContext.Set<FormProperty>().Remove(propertyToDelete);
			}

			if (propertiesToDelete.Any())
			{
				await _dbContext.SaveChangesAsync(cn);
			}

			// مرحله 2: بروزرسانی یا افزودن Properties
			foreach (var incomingProperty in incomingProperties.OrderBy(p => p.OrderIndex))
			{
				FormProperty? targetProperty = null;

				if (incomingProperty.Id.HasValue && incomingProperty.Id > 0)
				{
					// بروزرسانی Property موجود
					targetProperty = targetSection.Properties.FirstOrDefault(p => p.Id == incomingProperty.Id);
					if (targetProperty != null)
					{
						// بروزرسانی فیلدهای پایه
						UpdatePropertyBasicFields(targetProperty, incomingProperty);
						targetProperty.FormDefinitionId = formDefinitionId;
						targetProperty.FormSectionId = targetSection.Id;

						await _dbContext.SaveChangesAsync(cn);
					}
				}
				else
				{
					// افزودن Property جدید
					var newProperty = new FormProperty
					{
						FormDefinitionId = formDefinitionId,
						FormSectionId = targetSection.Id,
						PropertyName = incomingProperty.PropertyName,
						DisplayName = incomingProperty.DisplayName,
						SystemType = incomingProperty.SystemType,
						SearchPath = incomingProperty.SearchPath,
						AddToTable = incomingProperty.AddToTable,
						SystemProperty = incomingProperty.SystemProperty,
						ShowInRelationData = incomingProperty.ShowInRelationData,
						Required = incomingProperty.Required,
						FileTypes = incomingProperty.FileTypes,
						MaxFileSize = incomingProperty.MaxFileSize,
						Regex = incomingProperty.Regex,
						RegexInvalidError = incomingProperty.RegexInvalidError,
						MaxLength = incomingProperty.MaxLength,
						AutoNumberStart = incomingProperty.AutoNumberStart,
						AutoNumberStep = incomingProperty.AutoNumberStep,
						RelatedEntityFullName = incomingProperty.RelatedEntityFullName,
						RelatedEntityName = incomingProperty.RelatedEntityName,
						EnumName = incomingProperty.EnumName,
						ChildEntityName = incomingProperty.ChildEntityName,
						ChildEntityDisplayName = incomingProperty.ChildEntityDisplayName,
						ColSize = incomingProperty.ColSize,
						OrderIndex = incomingProperty.OrderIndex,
						DisplayTemplate = incomingProperty.DisplayTemplate,
						EnumOptions = new List<FormPropertyEnumOption>(),
						ChildProperties = new List<FormProperty>()
					};

					_dbContext.Set<FormProperty>().Add(newProperty);
					await _dbContext.SaveChangesAsync(cn);

					targetProperty = newProperty;
					targetSection.Properties.Add(newProperty);
				}

				// مرحله 3: همگام‌سازی EnumOptions
				if (targetProperty != null && incomingProperty.EnumOptions != null)
				{
					await SyncEnumOptionsStepByStep(targetProperty, incomingProperty.EnumOptions, cn);
				}

				// مرحله 4: همگام‌سازی ChildProperties به صورت بازگشتی
				if (targetProperty != null && incomingProperty.ChildProperties != null)
				{
					await SyncChildPropertiesStepByStep(targetProperty, incomingProperty.ChildProperties, formDefinitionId, cn);
				}
			}
		}
		/// <summary>
		/// همگام‌سازی مرحله‌ای EnumOptions
		/// </summary>
		private async Task SyncEnumOptionsStepByStep(
			FormProperty targetProperty,
			List<FormPropertyEnumOption> incomingOptions,
			CancellationToken cn)
		{
			incomingOptions ??= new List<FormPropertyEnumOption>();

			// مرحله 1: حذف EnumOptions که در داده‌های جدید نیستند
			var incomingOptionIds = incomingOptions
				.Where(eo => eo.Id.HasValue && eo.Id > 0)
				.Select(eo => eo.Id.Value)
				.ToHashSet();

			var optionsToDelete = targetProperty.EnumOptions
				.Where(eo => eo.Id.HasValue && eo.Id > 0 && !incomingOptionIds.Contains(eo.Id.Value))
				.ToList();

			foreach (var optionToDelete in optionsToDelete)
			{
				_dbContext.Set<FormPropertyEnumOption>().Remove(optionToDelete);
				targetProperty.EnumOptions.Remove(optionToDelete);
			}

			if (optionsToDelete.Any())
			{
				await _dbContext.SaveChangesAsync(cn);
			}

			// مرحله 2: بروزرسانی یا افزودن EnumOptions
			foreach (var incomingOption in incomingOptions.OrderBy(eo => eo.OrderIndex))
			{
				if (incomingOption.Id.HasValue && incomingOption.Id > 0)
				{
					// بروزرسانی EnumOption موجود
					var existingOption = targetProperty.EnumOptions.FirstOrDefault(eo => eo.Id == incomingOption.Id);
					if (existingOption != null)
					{
						existingOption.Value = incomingOption.Value;
						existingOption.Title = incomingOption.Title;
						existingOption.EnglishName = incomingOption.EnglishName;
						existingOption.OrderIndex = incomingOption.OrderIndex;

						await _dbContext.SaveChangesAsync(cn);
					}
				}
				else
				{
					// افزودن EnumOption جدید
					var newOption = new FormPropertyEnumOption
					{
						FormPropertyId = targetProperty.Id.Value,
						Value = incomingOption.Value,
						Title = incomingOption.Title,
						EnglishName = incomingOption.EnglishName,
						OrderIndex = incomingOption.OrderIndex
					};

					_dbContext.Set<FormPropertyEnumOption>().Add(newOption);
					await _dbContext.SaveChangesAsync(cn);

					targetProperty.EnumOptions.Add(newOption);
				}
			}
		}           /// <summary>
					/// همگام‌سازی مرحله‌ای ChildProperties به صورت بازگشتی
					/// </summary>
		private async Task SyncChildPropertiesStepByStep(
			FormProperty parentProperty,
			List<FormProperty> incomingChildren,
			long formDefinitionId,
			CancellationToken cn)
		{
			incomingChildren ??= new List<FormProperty>();

			// مرحله 1: حذف ChildProperties که در داده‌های جدید نیستند
			var incomingChildIds = incomingChildren
				.Where(cp => cp.Id.HasValue && cp.Id > 0)
				.Select(cp => cp.Id.Value)
				.ToHashSet();

			var childrenToDelete = parentProperty.ChildProperties
				.Where(cp => cp.Id.HasValue && cp.Id > 0 && !incomingChildIds.Contains(cp.Id.Value))
				.ToList();

			foreach (var childToDelete in childrenToDelete)
			{
				_dbContext.Set<FormProperty>().Remove(childToDelete);
				parentProperty.ChildProperties.Remove(childToDelete);
			}

			if (childrenToDelete.Any())
			{
				await _dbContext.SaveChangesAsync(cn);
			}

			// مرحله 2: بروزرسانی یا افزودن ChildProperties
			foreach (var incomingChild in incomingChildren.OrderBy(cp => cp.OrderIndex))
			{
				FormProperty? targetChild = null;

				if (incomingChild.Id.HasValue && incomingChild.Id > 0)
				{
					// بروزرسانی ChildProperty موجود
					targetChild = parentProperty.ChildProperties.FirstOrDefault(cp => cp.Id == incomingChild.Id);
					if (targetChild != null)
					{
						// بروزرسانی فیلدهای پایه
						UpdatePropertyBasicFields(targetChild, incomingChild);
						targetChild.FormDefinitionId = formDefinitionId;
						targetChild.ParentPropertyId = parentProperty.Id;

						await _dbContext.SaveChangesAsync(cn);
					}
				}
				else
				{
					// افزودن ChildProperty جدید
					var newChild = new FormProperty
					{
						FormDefinitionId = formDefinitionId,
						ParentPropertyId = parentProperty.Id,
						PropertyName = incomingChild.PropertyName,
						DisplayName = incomingChild.DisplayName,
						SystemType = incomingChild.SystemType,
						SearchPath = incomingChild.SearchPath,
						AddToTable = incomingChild.AddToTable,
						SystemProperty = incomingChild.SystemProperty,
						ShowInRelationData = incomingChild.ShowInRelationData,
						Required = incomingChild.Required,
						FileTypes = incomingChild.FileTypes,
						MaxFileSize = incomingChild.MaxFileSize,
						Regex = incomingChild.Regex,
						RegexInvalidError = incomingChild.RegexInvalidError,
						MaxLength = incomingChild.MaxLength,
						AutoNumberStart = incomingChild.AutoNumberStart,
						AutoNumberStep = incomingChild.AutoNumberStep,
						RelatedEntityFullName = incomingChild.RelatedEntityFullName,
						RelatedEntityName = incomingChild.RelatedEntityName,
						EnumName = incomingChild.EnumName,
						ChildEntityName = incomingChild.ChildEntityName,
						ChildEntityDisplayName = incomingChild.ChildEntityDisplayName,
						ColSize = incomingChild.ColSize,
						OrderIndex = incomingChild.OrderIndex,
						DisplayTemplate = incomingChild.DisplayTemplate,
						EnumOptions = new List<FormPropertyEnumOption>(),
						ChildProperties = new List<FormProperty>()
					};

					_dbContext.Set<FormProperty>().Add(newChild);
					await _dbContext.SaveChangesAsync(cn);

					targetChild = newChild;
					parentProperty.ChildProperties.Add(newChild);
				}

				// مرحله 3: همگام‌سازی EnumOptions این فرزند
				if (targetChild != null && incomingChild.EnumOptions != null)
				{
					await SyncEnumOptionsStepByStep(targetChild, incomingChild.EnumOptions, cn);
				}

				// مرحله 4: همگام‌سازی بازگشتی ChildProperties عمیق‌تر
				if (targetChild != null && incomingChild.ChildProperties != null && incomingChild.ChildProperties.Any())
				{
					await SyncChildPropertiesStepByStep(targetChild, incomingChild.ChildProperties, formDefinitionId, cn);
				}
			}
		}

		// تابع کمکی برای کپی مقادیر
		private void MapPropertyValues(FormProperty target, FormProperty source)
		{
			target.PropertyName = source.PropertyName;
			target.DisplayName = source.DisplayName;
			target.SystemType = source.SystemType;
			target.SearchPath = source.SearchPath;
			target.AddToTable = source.AddToTable;
			target.SystemProperty = source.SystemProperty;
			target.ShowInRelationData = source.ShowInRelationData;
			target.Required = source.Required;
			target.FileTypes = source.FileTypes;
			target.MaxFileSize = source.MaxFileSize;
			target.Regex = source.Regex;
			target.RegexInvalidError = source.RegexInvalidError;
			target.MaxLength = source.MaxLength;
			target.AutoNumberStart = source.AutoNumberStart;
			target.AutoNumberStep = source.AutoNumberStep;
			target.RelatedEntityFullName = source.RelatedEntityFullName;
			target.RelatedEntityName = source.RelatedEntityName;
			target.EnumName = source.EnumName;
			target.ChildEntityName = source.ChildEntityName;
			target.ChildEntityDisplayName = source.ChildEntityDisplayName;
			target.ColSize = source.ColSize;
			target.OrderIndex = source.OrderIndex;
			target.DisplayTemplate = source.DisplayTemplate;

		}

		/// <summary>
		/// کپی مقادیر ساده ویژگی
		/// </summary>
		private void UpdatePropertyValues(FormProperty target, FormProperty source)
		{
			target.PropertyName = source.PropertyName;
			target.DisplayName = source.DisplayName;
			target.SystemType = source.SystemType;
			target.SearchPath = source.SearchPath;
			target.AddToTable = source.AddToTable;
			target.SystemProperty = source.SystemProperty;
			target.ShowInRelationData = source.ShowInRelationData;
			target.Required = source.Required;
			target.FileTypes = source.FileTypes;
			target.MaxFileSize = source.MaxFileSize;
			target.Regex = source.Regex;
			target.RegexInvalidError = source.RegexInvalidError;
			target.MaxLength = source.MaxLength;
			target.AutoNumberStart = source.AutoNumberStart;
			target.AutoNumberStep = source.AutoNumberStep;
			target.RelatedEntityFullName = source.RelatedEntityFullName;
			target.RelatedEntityName = source.RelatedEntityName;
			target.EnumName = source.EnumName;
			target.ColSize = source.ColSize;
			target.OrderIndex = source.OrderIndex;
			target.DisplayTemplate = source.DisplayTemplate;
			target.ChildEntityName = source.ChildEntityName;
			target.ChildEntityDisplayName = source.ChildEntityDisplayName;
		}

		/// <summary>
		/// صفر کردن آی‌دی‌ها برای آبجکت‌های جدیدی که فرزند دارند
		/// </summary>
		private void ResetIdsRecursively(FormProperty prop)
		{
			prop.Id = null;
			prop.FormDefinitionId = 0;

			if (prop.EnumOptions != null)
			{
				foreach (var opt in prop.EnumOptions)
				{
					opt.Id = null;
				}
			}

			if (prop.ChildProperties != null)
			{
				foreach (var child in prop.ChildProperties)
				{
					ResetIdsRecursively(child);
				}
			}
		}

		/// <summary>
		/// Update basic properties of FormDefinition directly (entity is already tracked)
		/// </summary>
		private void UpdateFormDefinitionBasicPropertiesDirect(
			FormDefinition existing,
			FormDefinition incoming)
		{
			existing.EntityName = incoming.EntityName;
			existing.DisplayName = incoming.DisplayName;
			existing.Module = incoming.Module;
			existing.Schema = incoming.Schema;
			existing.Namespace = incoming.Namespace;
			existing.BaseEntityType = incoming.BaseEntityType;
			existing.Description = incoming.Description;
			existing.IsFromExistingEntity = incoming.IsFromExistingEntity;
			existing.SourceEntityFullName = incoming.SourceEntityFullName;
		}

		/// <summary>
		/// Update Sections properly (all entities are tracked)
		/// </summary>
		private void UpdateSectionsProperly(
			FormDefinition existing,
			FormDefinition incoming)
		{
			// Get IDs of incoming sections
			var incomingSectionIds = incoming.Sections
				.Where(s => s.Id.HasValue && s.Id > 0)
				.Select(s => s.Id.Value)
				.ToHashSet();

			// Delete sections that are not in incoming data
			var sectionsToDelete = existing.Sections
				.Where(s => s.Id.HasValue && s.Id.Value > 0 && !incomingSectionIds.Contains(s.Id.Value))
				.ToList();

			foreach (var sectionToDelete in sectionsToDelete)
			{
				_dbContext.Set<FormSection>().Remove(sectionToDelete);
				existing.Sections.Remove(sectionToDelete);
			}

			// Update existing sections or add new ones
			foreach (var incomingSection in incoming.Sections)
			{
				if (incomingSection.Id.HasValue && incomingSection.Id > 0)
				{
					// Update existing section (already tracked)
					var existingSection = existing.Sections.FirstOrDefault(s => s.Id == incomingSection.Id);
					if (existingSection != null)
					{
						existingSection.Title = incomingSection.Title;
						existingSection.OrderIndex = incomingSection.OrderIndex;
						existingSection.FormDefinitionId = existing.Id!.Value;
					}
				}
				else
				{
					// Add new section
					incomingSection.Id = null;
					incomingSection.FormDefinitionId = existing.Id!.Value;
					incomingSection.FormDefinition = existing;
					existing.Sections.Add(incomingSection);
					_dbContext.Set<FormSection>().Add(incomingSection);
				}
			}
		}

		/// <summary>
		/// Update all Properties properly (all entities are tracked)
		/// </summary>
		private void UpdateAllPropertiesProperly(
			FormDefinition existing,
			FormDefinition incoming)
		{
			// Process each section's properties
			foreach (var incomingSection in incoming.Sections)
			{
				if (!incomingSection.Id.HasValue) continue;

				var existingSection = existing.Sections.FirstOrDefault(s => s.Id == incomingSection.Id);
				if (existingSection == null) continue;

				// Get IDs of incoming properties
				var incomingPropertyIds = incomingSection.Properties
					.Where(p => p.Id.HasValue && p.Id > 0)
					.Select(p => p.Id.Value)
					.ToHashSet();

				// Delete properties that are not in incoming data
				var propertiesToDelete = existingSection.Properties
					.Where(p => p.Id.HasValue && p.Id.Value > 0 && !incomingPropertyIds.Contains(p.Id.Value))
					.ToList();

				foreach (var propertyToDelete in propertiesToDelete)
				{
					_dbContext.Set<FormProperty>().Remove(propertyToDelete);
					existingSection.Properties.Remove(propertyToDelete);
				}

				// Update existing properties or add new ones
				foreach (var incomingProperty in incomingSection.Properties)
				{
					if (incomingProperty.Id.HasValue && incomingProperty.Id > 0)
					{
						// Update existing property (already tracked)
						var existingProperty = existingSection.Properties.FirstOrDefault(p => p.Id == incomingProperty.Id);
						if (existingProperty != null)
						{
							UpdatePropertyBasicFields(existingProperty, incomingProperty);
							existingProperty.FormDefinitionId = existing.Id!.Value;
							existingProperty.FormSectionId = existingSection.Id;
							existingProperty.FormSection = null;
							existingProperty.FormDefinition = null;

							// Update EnumOptions
							UpdateEnumOptionsProperly(existingProperty, incomingProperty);

							// Update ChildProperties
							UpdateChildPropertiesProperly(existingProperty, incomingProperty, existing);
						}
					}
					else
					{
						// Add new property
						incomingProperty.Id = null;
						incomingProperty.FormDefinitionId = existing.Id!.Value;
						incomingProperty.FormDefinition = null;
						incomingProperty.FormSectionId = existingSection.Id;
						incomingProperty.FormSection = null;
						existingSection.Properties.Add(incomingProperty);
						_dbContext.Set<FormProperty>().Add(incomingProperty);

						// Note: EnumOptions and ChildProperties will be added after SaveChanges
						// to ensure the property has an ID
					}
				}
			}
		}

		/// <summary>
		/// Update EnumOptions properly (all entities are tracked)
		/// </summary>
		private void UpdateEnumOptionsProperly(
			FormProperty existingProperty,
			FormProperty incomingProperty)
		{
			// Get IDs of incoming enum options
			var incomingEnumIds = incomingProperty.EnumOptions
				.Where(eo => eo.Id.HasValue && eo.Id > 0)
				.Select(eo => eo.Id.Value)
				.ToHashSet();

			// Delete enum options that are not in incoming data
			var enumOptionsToDelete = existingProperty.EnumOptions
				.Where(eo => eo.Id.HasValue && eo.Id.Value > 0 && !incomingEnumIds.Contains(eo.Id.Value))
				.ToList();

			foreach (var enumOptionToDelete in enumOptionsToDelete)
			{
				_dbContext.Set<FormPropertyEnumOption>().Remove(enumOptionToDelete);
				existingProperty.EnumOptions.Remove(enumOptionToDelete);
			}

			// Update existing enum options or add new ones
			foreach (var incomingEnumOption in incomingProperty.EnumOptions)
			{
				if (incomingEnumOption.Id.HasValue && incomingEnumOption.Id > 0)
				{
					// Update existing enum option (already tracked)
					var existingEnumOption = existingProperty.EnumOptions.FirstOrDefault(eo => eo.Id == incomingEnumOption.Id);
					if (existingEnumOption != null)
					{
						existingEnumOption.Value = incomingEnumOption.Value;
						existingEnumOption.Title = incomingEnumOption.Title;
						existingEnumOption.EnglishName = incomingEnumOption.EnglishName;
						existingEnumOption.OrderIndex = incomingEnumOption.OrderIndex;
						existingEnumOption.FormPropertyId = existingProperty.Id!.Value;
					}
				}
				else
				{
					// Add new enum option
					incomingEnumOption.Id = null;
					incomingEnumOption.FormPropertyId = existingProperty.Id!.Value;
					incomingEnumOption.FormProperty = existingProperty;
					existingProperty.EnumOptions.Add(incomingEnumOption);
					_dbContext.Set<FormPropertyEnumOption>().Add(incomingEnumOption);
				}
			}
		}

		/// <summary>
		/// Update ChildProperties properly (all entities are tracked)
		/// </summary>
		private void UpdateChildPropertiesProperly(
			FormProperty existingProperty,
			FormProperty incomingProperty,
			FormDefinition formDefinition)
		{
			// Get IDs of incoming child properties
			var incomingChildIds = incomingProperty.ChildProperties
				.Where(cp => cp.Id.HasValue && cp.Id > 0)
				.Select(cp => cp.Id.Value)
				.ToHashSet();

			// Delete child properties that are not in incoming data
			var childrenToDelete = existingProperty.ChildProperties
				.Where(cp => cp.Id.HasValue && cp.Id.Value > 0 && !incomingChildIds.Contains(cp.Id.Value))
				.ToList();

			foreach (var childToDelete in childrenToDelete)
			{
				_dbContext.Set<FormProperty>().Remove(childToDelete);
				existingProperty.ChildProperties.Remove(childToDelete);
			}

			// Update existing child properties or add new ones
			foreach (var incomingChild in incomingProperty.ChildProperties)
			{
				if (incomingChild.Id.HasValue && incomingChild.Id > 0)
				{
					// Update existing child property (already tracked)
					var existingChild = existingProperty.ChildProperties.FirstOrDefault(cp => cp.Id == incomingChild.Id);
					if (existingChild != null)
					{
						UpdatePropertyBasicFields(existingChild, incomingChild);
						existingChild.ParentPropertyId = existingProperty.Id;
						existingChild.ParentProperty = null;
						existingChild.FormDefinitionId = formDefinition.Id!.Value;

						// Update EnumOptions recursively
						UpdateEnumOptionsProperly(existingChild, incomingChild);

						// Update ChildProperties recursively
						UpdateChildPropertiesProperly(existingChild, incomingChild, formDefinition);

						_dbContext.Set<FormProperty>().Update(existingChild);
					}
				}
				else
				{
					// Add new child property recursively
					AddChildPropertyRecursively(incomingChild, existingProperty, formDefinition);
				}
			}
		}

		/// <summary>
		/// Add EnumOptions and ChildProperties for new properties (after they have IDs)
		/// </summary>
		private void AddEnumOptionsAndChildPropertiesForNewProperties(
			FormDefinition existing,
			FormDefinition incoming)
		{
			// Process each section's properties
			foreach (var incomingSection in incoming.Sections)
			{
				if (!incomingSection.Id.HasValue) continue;

				var existingSection = existing.Sections.FirstOrDefault(s => s.Id == incomingSection.Id);
				if (existingSection == null) continue;

				foreach (var incomingProperty in incomingSection.Properties)
				{
					FormProperty? existingProperty = null;

					// Find matching property in existing
					if (incomingProperty.Id.HasValue && incomingProperty.Id > 0)
					{
						existingProperty = existingSection.Properties.FirstOrDefault(p => p.Id == incomingProperty.Id);
					}
					else
					{
						// For new properties, find by propertyName and orderIndex
						existingProperty = existingSection.Properties
							.Where(p => p.PropertyName == incomingProperty.PropertyName && p.OrderIndex == incomingProperty.OrderIndex)
							.OrderByDescending(p => p.Id) // Get the most recent one
							.FirstOrDefault();
					}

					if (existingProperty == null || !existingProperty.Id.HasValue) continue;

					// Add EnumOptions for new properties (only those without ID)
					if (incomingProperty.EnumOptions != null && incomingProperty.EnumOptions.Any())
					{
						foreach (var enumOption in incomingProperty.EnumOptions)
						{
							// Skip if already exists
							if (enumOption.Id.HasValue && enumOption.Id > 0) continue;

							// Check if already added
							if (existingProperty.EnumOptions.Any(eo =>
								eo.Value == enumOption.Value &&
								eo.Title == enumOption.Title))
								continue;

							enumOption.Id = null;
							enumOption.FormPropertyId = existingProperty.Id.Value;
							enumOption.FormProperty = existingProperty;
							existingProperty.EnumOptions.Add(enumOption);
							_dbContext.Set<FormPropertyEnumOption>().Add(enumOption);
						}
					}

					// Add ChildProperties recursively for new properties
					if (incomingProperty.ChildProperties != null && incomingProperty.ChildProperties.Any())
					{
						foreach (var childProperty in incomingProperty.ChildProperties)
						{
							// Check if child already exists
							if (childProperty.Id.HasValue && childProperty.Id > 0)
							{
								var existingChild = existingProperty.ChildProperties.FirstOrDefault(cp => cp.Id == childProperty.Id);
								if (existingChild != null)
								{
									// Update existing child's nested data
									UpdateEnumOptionsProperly(existingChild, childProperty);
									if (childProperty.ChildProperties != null && childProperty.ChildProperties.Any())
									{
										foreach (var nestedChild in childProperty.ChildProperties)
										{
											AddChildPropertyRecursively(nestedChild, existingChild, existing);
										}
									}
									continue;
								}
							}

							// Add new child property
							AddChildPropertyRecursively(childProperty, existingProperty, existing);
						}
					}
				}
			}
		}

		/// <summary>
		/// Add child property recursively with all its nested data (after parent has ID)
		/// </summary>
		private void AddChildPropertyRecursively(
			FormProperty childProperty,
			FormProperty parentProperty,
			FormDefinition formDefinition)
		{
			// Add new child property
			childProperty.Id = null;
			childProperty.ParentPropertyId = parentProperty.Id;
			childProperty.ParentProperty = parentProperty;
			childProperty.FormDefinitionId = formDefinition.Id!.Value;
			parentProperty.ChildProperties.Add(childProperty);
			_dbContext.Set<FormProperty>().Add(childProperty);
		}

		/// <summary>
		/// Add nested data (EnumOptions and ChildProperties) for child properties after they have IDs
		/// </summary>
		private void AddNestedDataForChildProperties(
			FormDefinition existing,
			FormDefinition incoming)
		{
			// Process all properties recursively
			foreach (var incomingSection in incoming.Sections)
			{
				if (!incomingSection.Id.HasValue) continue;

				var existingSection = existing.Sections.FirstOrDefault(s => s.Id == incomingSection.Id);
				if (existingSection == null) continue;

				foreach (var incomingProperty in incomingSection.Properties)
				{
					var existingProperty = existingSection.Properties.FirstOrDefault(p =>
						(incomingProperty.Id.HasValue && p.Id == incomingProperty.Id) ||
						(!incomingProperty.Id.HasValue && p.PropertyName == incomingProperty.PropertyName));

					if (existingProperty == null || !existingProperty.Id.HasValue) continue;

					// Process child properties recursively
					if (incomingProperty.ChildProperties != null && incomingProperty.ChildProperties.Any())
					{
						ProcessChildPropertiesRecursively(incomingProperty.ChildProperties, existingProperty, existing, incoming);
					}
				}
			}
		}

		/// <summary>
		/// Process child properties recursively to add their EnumOptions and nested children
		/// </summary>
		private void ProcessChildPropertiesRecursively(
			List<FormProperty> incomingChildren,
			FormProperty parentProperty,
			FormDefinition existing,
			FormDefinition incoming)
		{
			foreach (var incomingChild in incomingChildren)
			{
				FormProperty? existingChild = null;

				if (incomingChild.Id.HasValue && incomingChild.Id > 0)
				{
					existingChild = parentProperty.ChildProperties.FirstOrDefault(cp => cp.Id == incomingChild.Id);
				}
				else
				{
					existingChild = parentProperty.ChildProperties
						.Where(cp => cp.PropertyName == incomingChild.PropertyName)
						.OrderByDescending(cp => cp.Id)
						.FirstOrDefault();
				}

				if (existingChild == null || !existingChild.Id.HasValue) continue;

				// Add EnumOptions
				if (incomingChild.EnumOptions != null && incomingChild.EnumOptions.Any())
				{
					foreach (var enumOption in incomingChild.EnumOptions)
					{
						if (enumOption.Id.HasValue && enumOption.Id > 0) continue;
						if (existingChild.EnumOptions.Any(eo => eo.Value == enumOption.Value && eo.Title == enumOption.Title)) continue;

						enumOption.Id = null;
						enumOption.FormPropertyId = existingChild.Id.Value;
						enumOption.FormProperty = existingChild;
						existingChild.EnumOptions.Add(enumOption);
						_dbContext.Set<FormPropertyEnumOption>().Add(enumOption);
					}
				}

				// Process nested children recursively
				if (incomingChild.ChildProperties != null && incomingChild.ChildProperties.Any())
				{
					ProcessChildPropertiesRecursively(incomingChild.ChildProperties, existingChild, existing, incoming);
				}
			}
		}

		/// <summary>
		/// Update basic fields of a property
		/// </summary>
		private void UpdatePropertyBasicFields(FormProperty existing, FormProperty incoming)
		{
			existing.PropertyName = incoming.PropertyName;
			existing.DisplayName = incoming.DisplayName;
			existing.SystemType = incoming.SystemType;
			existing.SearchPath = incoming.SearchPath;
			existing.AddToTable = incoming.AddToTable;
			existing.SystemProperty = incoming.SystemProperty;
			existing.ShowInRelationData = incoming.ShowInRelationData;
			existing.Required = incoming.Required;
			existing.FileTypes = incoming.FileTypes;
			existing.MaxFileSize = incoming.MaxFileSize;
			existing.Regex = incoming.Regex;
			existing.RegexInvalidError = incoming.RegexInvalidError;
			existing.MaxLength = incoming.MaxLength;
			existing.AutoNumberStart = incoming.AutoNumberStart;
			existing.AutoNumberStep = incoming.AutoNumberStep;
			existing.RelatedEntityFullName = incoming.RelatedEntityFullName;
			existing.RelatedEntityName = incoming.RelatedEntityName;
			existing.EnumName = incoming.EnumName;
			existing.ChildEntityName = incoming.ChildEntityName;
			existing.ChildEntityDisplayName = incoming.ChildEntityDisplayName;
			existing.ColSize = incoming.ColSize;
			existing.OrderIndex = incoming.OrderIndex;
			existing.DisplayTemplate = incoming.DisplayTemplate;
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
		public async Task<IActionResult> Edit(long? id, CancellationToken cn)
		{
			if (id != null && id != 0)
			{
				var entity = await _unitOfWork.Repository<FormDefinition>().TableNoTracking
					.AsSplitQuery()
					.Include(c => c.Sections)
						.ThenInclude(s => s.Properties)
							.ThenInclude(p => p.EnumOptions)
					.Include(c => c.Sections)
						.ThenInclude(s => s.Properties)
							.ThenInclude(p => p.ChildProperties)
								.ThenInclude(cp => cp.EnumOptions)

					.FirstOrDefaultAsync(c => c.Id == id, cn);

				if (entity == null)
				{
					return NotFound("فرم یافت نشد");
				}

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
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetById(long id, CancellationToken cn)
		{
			if (id == 0)
			{
				return BadRequest("شناسه معتبر نیست");
			}

			var entity = await _unitOfWork.Repository<FormDefinition>().TableNoTracking
				.AsSplitQuery()
				.Include(c => c.Sections)
					.ThenInclude(s => s.Properties)
						.ThenInclude(p => p.EnumOptions)
				.Include(c => c.Sections)
					.ThenInclude(s => s.Properties)
						.ThenInclude(p => p.ChildProperties)
							.ThenInclude(cp => cp.EnumOptions)

				.FirstOrDefaultAsync(c => c.Id == id, cn);

			if (entity == null)
			{
				return NotFound("فرم یافت نشد");
			}

			return Ok(entity);
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
						enumClasses = code.EnumClasses,
						partialViews = code.PartialViews
					},
					paths = paths
				};

				return Ok(result);
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در تولید کد: " + ex.Message);
			}
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت ماژول‌ها", ActionAccessType.Api)]
		public IActionResult GetAvailableModules()
		{
			var modules = _entityMetadataCache.GetAll()
				.Select(e => e.EntityFullName?.Split('.').ElementAtOrDefault(2) ?? "App")
				.Distinct()
				.OrderBy(m => m)
				.ToList();

			return Ok(modules);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت موجودیت‌ها", ActionAccessType.Api)]
		public IActionResult GetAvailableEntities()
		{
			var entities = _entityMetadataCache.GetAll()
				.Select(e => new
				{
					name = e.EntityName,
					fullName = e.EntityFullName,
					displayName = e.DisplayName
				})
				.OrderBy(e => e.displayName)
				.ToList();

			return Ok(entities);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("بارگذاری از موجودیت", ActionAccessType.Api)]
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
				var allProperties = new List<FormProperty>();

				if (formDefinition.Sections != null)
				{
					foreach (var section in formDefinition.Sections)
					{
						if (section.Properties != null)
						{
							allProperties.AddRange(section.Properties);
						}
					}
				}



				// Create a clean FormDefinition for export
				var exportData = new
				{
					formDefinition.Id,
					formDefinition.EntityName,
					formDefinition.DisplayName,
					formDefinition.Module,
					formDefinition.Schema,
					formDefinition.Namespace,
					formDefinition.BaseEntityType,
					formDefinition.Description,
					formDefinition.IsFromExistingEntity,
					formDefinition.SourceEntityFullName,
					Sections = formDefinition.Sections?.Select(s => new
					{
						s.Id,
						s.Title,
						s.OrderIndex,
						Properties = s.Properties?.Select(p => MapPropertyForExport(p))
					}),
					Properties = allProperties.Select(p => MapPropertyForExport(p))
				};

				var json = System.Text.Json.JsonSerializer.Serialize(exportData, new System.Text.Json.JsonSerializerOptions
				{
					WriteIndented = true,
					PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
				});

				if (preview)
				{
					return Content(json, "application/json");
				}

				var fileName = $"FormDefinition_{formDefinition.EntityName}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
				var bytes = System.Text.Encoding.UTF8.GetBytes(json);
				return File(bytes, "application/json", fileName);
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در تولید JSON: " + ex.Message);
			}
		}

		private object MapPropertyForExport(FormProperty prop)
		{
			return new
			{
				prop.Id,
				prop.PropertyName,
				prop.DisplayName,
				prop.SystemType,
				prop.SearchPath,
				prop.AddToTable,
				prop.SystemProperty,
				prop.ShowInRelationData,
				prop.Required,
				prop.FileTypes,
				prop.MaxFileSize,
				prop.Regex,
				prop.RegexInvalidError,
				prop.MaxLength,
				prop.AutoNumberStart,
				prop.AutoNumberStep,
				prop.RelatedEntityFullName,
				prop.RelatedEntityName,
				prop.EnumName,
				prop.ColSize,
				prop.OrderIndex,
				prop.DisplayTemplate,
				prop.FormSectionId,
				prop.ParentPropertyId,
				EnumOptions = prop.EnumOptions?.Select(eo => new
				{
					eo.Id,
					eo.Value,
					eo.Title,
					eo.EnglishName,
					eo.OrderIndex
				}),
				ChildProperties = prop.ChildProperties?.Select(cp => MapPropertyForExport(cp))
			};
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ترجمه نام ویژگی", ActionAccessType.Api)]
		public async Task<IActionResult> TranslatePropertyName([FromBody] TranslateRequest request)
		{
			try
			{
				var _baseUrl = "http://localhost:1234/v1/chat/completions";

				var payload = new
				{
					model = "local-model",
					messages = new[]
					{
						new { role = "system", content = @"You are a translator that converts Persian text to English C# property names.

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

		[HttpPost("[action]")]
		[ActionDisplayName("ترجمه به فارسی", ActionAccessType.Api)]
		public async Task<IActionResult> TranslateToFarsi([FromBody] TranslateRequest request)
		{
			try
			{
				var _baseUrl = "http://localhost:1234/v1/chat/completions";

				var payload = new
				{
					model = "local-model",
					messages = new[]
					{
						new { role = "system", content = @"You are a translator that converts English C# property names to Persian display names.

Rules:
- Output MUST be only the Persian translation (no explanation).
- Convert PascalCase or camelCase to natural Persian phrase.
- Use common Persian terms for technical words.
- Do NOT add punctuation at the end.
- Do NOT return quotes.
- Keep translations short and meaningful.
- Use standard Persian naming conventions for data fields.

Example Conversions:
'Name' → نام
'LastName' → نام خانوادگی
'NationalId' → شماره ملی
'BirthDate' → تاریخ تولد
'PostalCode' → کد پستی
'MobileNumber' → شماره موبایل
'Email' → ایمیل
'Address' → آدرس
'PhoneNumber' → شماره تلفن
'CreatedDate' → تاریخ ایجاد
'ModifiedDate' → تاریخ ویرایش
'IsActive' → فعال
'Description' → توضیحات
'Title' → عنوان
'Code' → کد
'Price' → قیمت
'Quantity' → تعداد
'Status' → وضعیت
'Type' → نوع
'CategoryId' → شناسه دسته‌بندی
'UserId' → شناسه کاربر

Return ONLY the Persian display name."
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

				var translatedName = parsed?.choices?.FirstOrDefault()?.message?.content?.Trim() ?? request.Text;

				// Clean up result
				translatedName = translatedName.Replace("\"", "").Replace("'", "").Trim();

				return Ok(translatedName);
			}
			catch (Exception ex)
			{
				// Fallback to simple translation
				var fallbackName = SimpleFallbackTranslateToFarsi(request.Text);
				return Ok(fallbackName);
			}
		}

		/// <summary>
		/// ترجمه ساده نام انگلیسی به فارسی (Fallback)
		/// </summary>
		private string SimpleFallbackTranslateToFarsi(string englishName)
		{
			if (string.IsNullOrWhiteSpace(englishName))
				return englishName;

			// نقشه‌برداری رایج نام‌های انگلیسی به فارسی
			var commonMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				{ "Id", "شناسه" },
				{ "Name", "نام" },
				{ "Title", "عنوان" },
				{ "Description", "توضیحات" },
				{ "Code", "کد" },
				{ "Date", "تاریخ" },
				{ "DateTime", "تاریخ و زمان" },
				{ "CreatedDate", "تاریخ ایجاد" },
				{ "ModifiedDate", "تاریخ ویرایش" },
				{ "CreatedBy", "ایجاد کننده" },
				{ "ModifiedBy", "ویرایش کننده" },
				{ "IsActive", "فعال" },
				{ "IsDeleted", "حذف شده" },
				{ "Status", "وضعیت" },
				{ "Type", "نوع" },
				{ "Email", "ایمیل" },
				{ "Phone", "تلفن" },
				{ "PhoneNumber", "شماره تلفن" },
				{ "Mobile", "موبایل" },
				{ "MobileNumber", "شماره موبایل" },
				{ "Address", "آدرس" },
				{ "City", "شهر" },
				{ "Country", "کشور" },
				{ "PostalCode", "کد پستی" },
				{ "Price", "قیمت" },
				{ "Amount", "مبلغ" },
				{ "Quantity", "تعداد" },
				{ "Total", "جمع" },
				{ "Discount", "تخفیف" },
				{ "Tax", "مالیات" },
				{ "Note", "یادداشت" },
				{ "Notes", "یادداشت‌ها" },
				{ "Order", "ترتیب" },
				{ "OrderIndex", "ترتیب" },
				{ "DisplayOrder", "ترتیب نمایش" },
				{ "ParentId", "شناسه والد" },
				{ "UserId", "شناسه کاربر" },
				{ "Username", "نام کاربری" },
				{ "Password", "رمز عبور" },
				{ "FirstName", "نام" },
				{ "LastName", "نام خانوادگی" },
				{ "FullName", "نام کامل" },
				{ "NationalId", "شماره ملی" },
				{ "NationalCode", "کد ملی" },
				{ "BirthDate", "تاریخ تولد" },
				{ "Image", "تصویر" },
				{ "ImageUrl", "آدرس تصویر" },
				{ "Url", "آدرس" },
				{ "Website", "وب‌سایت" },
				{ "StartDate", "تاریخ شروع" },
				{ "EndDate", "تاریخ پایان" },
				{ "DueDate", "تاریخ سررسید" },
				{ "Category", "دسته‌بندی" },
				{ "CategoryId", "شناسه دسته‌بندی" },
				{ "Comment", "نظر" },
				{ "Comments", "نظرات" },
				{ "Content", "محتوا" },
				{ "Summary", "خلاصه" },
				{ "Active", "فعال" },
				{ "Enabled", "فعال" },
				{ "Visible", "قابل نمایش" },
				{ "Required", "الزامی" },
				{ "Optional", "اختیاری" },
				{ "Default", "پیش‌فرض" },
				{ "Min", "حداقل" },
				{ "Max", "حداکثر" },
				{ "Count", "تعداد" },
				{ "Size", "اندازه" },
				{ "Width", "عرض" },
				{ "Height", "ارتفاع" },
				{ "Color", "رنگ" },
				{ "Weight", "وزن" },
				{ "Unit", "واحد" },
				{ "Version", "نسخه" },
				{ "Label", "برچسب" },
				{ "Tag", "تگ" },
				{ "Tags", "تگ‌ها" }
			};

			// بررسی نقشه‌برداری مستقیم
			if (commonMappings.TryGetValue(englishName, out var persianName))
			{
				return persianName;
			}

			// اگر با Id ختم شود
			if (englishName.EndsWith("Id", StringComparison.OrdinalIgnoreCase) && englishName.Length > 2)
			{
				var baseName = englishName.Substring(0, englishName.Length - 2);
				if (commonMappings.TryGetValue(baseName, out var baseTranslation))
				{
					return $"شناسه {baseTranslation}";
				}
				return $"شناسه {baseName}";
			}

			// اگر با Date شروع شود
			if (englishName.StartsWith("Date", StringComparison.OrdinalIgnoreCase) && englishName.Length > 4)
			{
				var baseName = englishName.Substring(4);
				return $"تاریخ {baseName}";
			}

			// اگر با Is شروع شود (Boolean)
			if (englishName.StartsWith("Is", StringComparison.OrdinalIgnoreCase) && englishName.Length > 2)
			{
				var baseName = englishName.Substring(2);
				if (commonMappings.TryGetValue(baseName, out var baseTranslation))
				{
					return baseTranslation;
				}
				return baseName;
			}

			// در غیر این صورت، همان نام انگلیسی را برگردان
			return englishName;
		}

		private void AddNestedPropertiesRecursive(EntityMetadata currentEntity, List<object> propsList, string pathPrefix, string displayPrefix, int remainingDepth, int currentDepth)
		{
			if (remainingDepth <= 0 || currentDepth >= 5) return; // Max depth protection

			// فقط Entity های مرتبط را پردازش کن (نه همه property ها)
			foreach (var prop in currentEntity.Properties.Where(p => !p.SystemProperty && p.SystemType == SystemType.Entity && !string.IsNullOrEmpty(p.RelatedEntityTypeFullName)))
			{
				var currentPath = string.IsNullOrEmpty(pathPrefix) ? prop.Name : $"{pathPrefix}.{prop.Name}";
				var currentDisplay = string.IsNullOrEmpty(displayPrefix) ? prop.DisplayName : $"{displayPrefix} > {prop.DisplayName}";

				var relatedEntityMeta = _entityMetadataCache.Get(prop.RelatedEntityTypeFullName);
				if (relatedEntityMeta != null)
				{
					// اضافه کردن فیلدهای این Entity مرتبط
					var nestedProps = relatedEntityMeta.Properties
						.Where(p => !p.SystemProperty)
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

					// پردازش بازگشتی برای Entity های عمیق‌تر
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

						// Normalize enum options - only clear ID for new options
						foreach (var op in prop.EnumOptions)
						{
							op.FormProperty = prop;
							if (prop.Id == null || prop.Id == 0)
							{
								op.FormProperty = null;
								op.FormPropertyId = 0;
							}
							else
							{
								op.FormPropertyId = prop.Id.Value;
							}

							// Only clear ID if it's a new enum option
							if (op.Id == null || op.Id == 0)
							{
								op.Id = null;
							}
						}

						// Recursively normalize child properties
						NormalizeChildProperties(prop.ChildProperties, prop);
					}
				}

				formDefinition.Sections = orderedSections;
			}
			else
			{
				formDefinition.Sections = new List<FormSection>();
			}
		}

		/// <summary>
		/// Recursively normalize child properties (for ListEntity type)
		/// </summary>
		private void NormalizeChildProperties(ICollection<FormProperty> childProperties, FormProperty parentProperty)
		{
			if (childProperties == null || !childProperties.Any()) return;

			int childIndex = 0;
			foreach (var childProp in childProperties)
			{
				childProp.OrderIndex = childIndex++;
				childProp.ParentProperty = parentProperty;

				if (parentProperty.Id == null || parentProperty.Id == 0)
				{
					childProp.ParentPropertyId = null;
				}
				else
				{
					childProp.ParentPropertyId = parentProperty.Id;
				}

				// Normalize enum options for child properties
				foreach (var op in childProp.EnumOptions)
				{
					op.FormProperty = childProp;
					if (childProp.Id == null || childProp.Id == 0)
					{
						op.FormProperty = null;
						op.FormPropertyId = 0;
					}
					else
					{
						op.FormPropertyId = childProp.Id.Value;
					}

					// Only clear ID if it's a new enum option
					if (op.Id == null || op.Id == 0)
					{
						op.Id = null;
					}
				}

				// Recursively normalize deeper levels
				NormalizeChildProperties(childProp.ChildProperties, childProp);
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

		#region Database Import Endpoints

		/// <summary>
		/// دریافت لیست Connection String های موجود
		/// </summary>
		[HttpGet("[action]")]
		[ActionDisplayName("دریافت Connection String ها", ActionAccessType.Api)]
		public async Task<IActionResult> GetConnectionStrings()
		{
			try
			{
				var connectionStrings = await _databaseSchemaService.GetConnectionStringsAsync();
				return Ok(connectionStrings);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "خطا در دریافت Connection String ها", error = ex.Message });
			}
		}

		/// <summary>
		/// تست اتصال به دیتابیس
		/// </summary>
		[HttpPost("[action]")]
		[ActionDisplayName("تست اتصال به دیتابیس", ActionAccessType.Api)]
		public async Task<IActionResult> TestConnection([FromBody] TestConnectionRequest request)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(request?.ConnectionString))
				{
					return BadRequest(new { message = "Connection String نمی‌تواند خالی باشد" });
				}

				var result = await _databaseSchemaService.TestConnectionAsync(request.ConnectionString);
				return Ok(result);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "خطا در تست اتصال", error = ex.Message });
			}
		}

		/// <summary>
		/// دریافت لیست جداول دیتابیس
		/// </summary>
		[HttpPost("[action]")]
		[ActionDisplayName("دریافت لیست جداول", ActionAccessType.Api)]
		public async Task<IActionResult> GetDatabaseTables([FromBody] GetTablesRequest request)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(request?.ConnectionString))
				{
					return BadRequest(new { message = "Connection String نمی‌تواند خالی باشد" });
				}

				var tables = await _databaseSchemaService.GetTablesAsync(request.ConnectionString);
				return Ok(tables);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "خطا در دریافت لیست جداول", error = ex.Message });
			}
		}

		/// <summary>
		/// دریافت ستون‌های یک جدول
		/// </summary>
		[HttpPost("[action]")]
		[ActionDisplayName("دریافت ستون‌های جدول", ActionAccessType.Api)]
		public async Task<IActionResult> GetTableColumns([FromBody] GetTableColumnsRequest request)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(request?.ConnectionString))
				{
					return BadRequest(new { message = "Connection String نمی‌تواند خالی باشد" });
				}

				if (string.IsNullOrWhiteSpace(request?.Schema) || string.IsNullOrWhiteSpace(request?.TableName))
				{
					return BadRequest(new { message = "نام Schema و جدول الزامی است" });
				}

				var columns = await _databaseSchemaService.GetTableColumnsAsync(
					request.ConnectionString,
					request.Schema,
					request.TableName);

				return Ok(new { columns });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "خطا در دریافت ستون‌های جدول", error = ex.Message });
			}
		}

		/// <summary>
		/// ایجاد فرم از روی جدول دیتابیس
		/// </summary>
		[HttpPost("[action]")]
		[ActionDisplayName("ایجاد فرم از جدول", ActionAccessType.Api)]
		public async Task<IActionResult> GenerateFormFromTable([FromBody] GenerateFormFromTableRequest request, CancellationToken cn)
		{
			try
			{
				// Validation
				if (string.IsNullOrWhiteSpace(request?.ConnectionString))
				{
					return BadRequest(new { message = "Connection String نمی‌تواند خالی باشد" });
				}

				if (string.IsNullOrWhiteSpace(request?.Schema) || string.IsNullOrWhiteSpace(request?.TableName))
				{
					return BadRequest(new { message = "نام Schema و جدول الزامی است" });
				}

				if (string.IsNullOrWhiteSpace(request?.ModuleName))
				{
					return BadRequest(new { message = "نام ماژول الزامی است" });
				}

				// دریافت ستون‌های جدول اگر ارسال نشده
				var columns = request.SelectedColumns;
				if (columns == null || !columns.Any())
				{
					columns = await _databaseSchemaService.GetTableColumnsAsync(
						request.ConnectionString,
						request.Schema,
						request.TableName);
				}

				if (!columns.Any())
				{
					return BadRequest(new { message = "جدول انتخاب شده هیچ ستونی ندارد" });
				}

				// ایجاد FormProperties از ستون‌ها
				var properties = new List<FormProperty>();
				int orderIndex = 0;

				foreach (var column in columns)
				{
					// استفاده از مقدار AddToTable ارسالی از کاربر (اگر موجود باشد)
					var addToTable = column.AddToTable;

					// استفاده از مقدار Required ارسالی از کاربر (اگر موجود باشد)
					var required = column.Required;

					var property = new FormProperty
					{
						PropertyName = column.ColumnName,
						DisplayName = column.SuggestedDisplayName,
						SystemType = column.MappedSystemType,
						Required = required,
						AddToTable = addToTable,
						SystemProperty = column.IsPrimaryKey,
						ShowInRelationData = column.IsPrimaryKey,
						MaxLength = column.MaxLength,
						OrderIndex = orderIndex++,
						ColSize = "col-md-6"
					};

					// تنظیمات ویژه برای Foreign Key یا Entity انتخاب شده توسط کاربر
					if (column.MappedSystemType == Common.Attributes.SystemType.Entity)
					{
						// اگر کاربر Entity انتخاب کرده، از آن استفاده کن
						if (!string.IsNullOrEmpty(column.RelatedEntityFullName))
						{
							property.RelatedEntityFullName = column.RelatedEntityFullName;
							property.RelatedEntityName = column.RelatedEntityName;
						}
						else if (column.IsForeignKey && !string.IsNullOrEmpty(column.ForeignKeyTable))
						{
							// در غیر این صورت از نام جدول FK استفاده کن
							property.RelatedEntityName = column.ForeignKeyTable;
						}

						property.DisplayName = column.SuggestedDisplayName.Replace("شناسه ", "");
						property.ColSize = "col-md-6";
					}

					// تنظیمات ویژه برای AutoNumber (شناسه خودکار)
					if (column.IsPrimaryKey && column.DataType == "bigint")
					{
						property.SystemType = Common.Attributes.SystemType.AutoNumber;
						property.AutoNumberStart = 1;
						property.AutoNumberStep = 1;
					}

					properties.Add(property);
				}

				// ایجاد یا بروزرسانی FormDefinition
				FormDefinition formDefinition;

				if (request.ImportMode == "append" && request.ExistingFormDefinitionId.HasValue)
				{
					// حالت Append: اضافه کردن به فرم موجود
					formDefinition = await _unitOfWork.Repository<FormDefinition>()
						.Table
						.Include(f => f.Sections)
							.ThenInclude(s => s.Properties)
						.FirstOrDefaultAsync(f => f.Id == request.ExistingFormDefinitionId.Value);

					if (formDefinition == null)
					{
						return NotFound(new { message = "فرم مورد نظر یافت نشد" });
					}

					// ایجاد بخش جدید یا اضافه به بخش موجود
					var sectionName = request.SectionName ?? $"اطلاعات {request.TableName}";
					var section = formDefinition.Sections.FirstOrDefault(s => s.Title == sectionName);

					if (section == null)
					{
						section = new FormSection
						{
							Title = sectionName,
							OrderIndex = formDefinition.Sections.Count,
							Properties = properties
						};
						formDefinition.Sections.Add(section);
					}
					else
					{
						// اضافه کردن به بخش موجود
						var maxOrder = section.Properties.Any() ? section.Properties.Max(p => p.OrderIndex) : -1;
						foreach (var prop in properties)
						{
							prop.OrderIndex = ++maxOrder;
							section.Properties.Add(prop);
						}
					}
				}
				else
				{
					// حالت Replace: ایجاد فرم جدید
					formDefinition = new FormDefinition
					{
						EntityName = request.TableName,
						DisplayName = request.TableName,
						Module = request.ModuleName,
						Schema = request.Schema,
						BaseEntityType = "BaseEntity",
						Description = $"فرم ایجاد شده از جدول {request.Schema}.{request.TableName}",
						IsFromExistingEntity = false
					};

					var section = new FormSection
					{
						Title = request.SectionName ?? "اطلاعات اصلی",
						OrderIndex = 0,
						Properties = properties
					};

					formDefinition.Sections.Add(section);
				}

				// ذخیره با استفاده از متدهای مرحله‌ای
				if (formDefinition.Id.HasValue && formDefinition.Id > 0)
				{
					// بروزرسانی فرم موجود
					return await Update(formDefinition, cn);
				}
				else
				{
					// افزودن فرم جدید
					return await Add(formDefinition, cn);
				}
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "خطا در ایجاد فرم از جدول", error = ex.Message });
			}
		}

		#endregion
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

	public class TestConnectionRequest
	{
		public string ConnectionString { get; set; }
	}

	public class GetTablesRequest
	{
		public string ConnectionString { get; set; }
	}

	public class GetTableColumnsRequest
	{
		public string ConnectionString { get; set; }
		public string Schema { get; set; }
		public string TableName { get; set; }
	}
}

