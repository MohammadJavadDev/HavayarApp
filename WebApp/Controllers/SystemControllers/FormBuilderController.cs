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
			if (formDefinition.Id == null || formDefinition.Id == 0) return BadRequest("شناسه فرم نامعتبر است");

			// 1. Load Data
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

			// 2. Update Main Properties
			existingForm.EntityName = formDefinition.EntityName;
			existingForm.DisplayName = formDefinition.DisplayName;
			existingForm.Module = formDefinition.Module;
			existingForm.Schema = formDefinition.Schema;
			existingForm.Namespace = formDefinition.Namespace;
			existingForm.BaseEntityType = formDefinition.BaseEntityType;
			existingForm.Description = formDefinition.Description;
			existingForm.IsFromExistingEntity = formDefinition.IsFromExistingEntity;
			existingForm.SourceEntityFullName = formDefinition.SourceEntityFullName;

			// 3. Sync Children (Passing formId)
			SyncSections(existingForm.Sections, formDefinition.Sections, existingForm.Id.Value);

			// 4. Save
			await _unitOfWork.SaveChangesAsync(cn);
			return Ok(existingForm);
		}
		/// <summary>
		/// همگام‌سازی لیست بخش‌ها
		/// </summary>
		private void SyncSections(ICollection<FormSection> existingSections, List<FormSection> incomingSections, long formId)
		{
			if (incomingSections == null) incomingSections = new List<FormSection>();

			// Delete
			var incomingIds = incomingSections.Where(x => x.Id > 0).Select(x => x.Id).ToList();
			var toDelete = existingSections.Where(x => !incomingIds.Contains(x.Id)).ToList();
			foreach (var item in toDelete)
			{
				existingSections.Remove(item);
				_dbContext.Entry(item).State = EntityState.Deleted;
			}

			// Upsert
			foreach (var incoming in incomingSections)
			{
				var existing = existingSections.FirstOrDefault(x => x.Id == incoming.Id && x.Id > 0);
				if (existing != null)
				{
					existing.Title = incoming.Title;
					existing.OrderIndex = incoming.OrderIndex;
					SyncProperties(existing.Properties, incoming.Properties, formId);
				}
				else
				{
					var newSection = new FormSection
					{
						// Id = null/0 (Default)
						FormDefinitionId = formId,
						Title = incoming.Title,
						OrderIndex = incoming.OrderIndex,
						Properties = new List<FormProperty>()
					};
					SyncProperties(newSection.Properties, incoming.Properties, formId);
					existingSections.Add(newSection);
				}
			}
		}
		/// <summary>
		/// همگام‌سازی لیست ویژگی‌ها (به صورت بازگشتی برای فرزندان)
		/// </summary>
		private void SyncProperties(ICollection<FormProperty> existingProperties, List<FormProperty> incomingProperties, long formId)
		{
			if (incomingProperties == null) incomingProperties = new List<FormProperty>();

			// Delete
			var incomingIds = incomingProperties.Where(x => x.Id > 0).Select(x => x.Id).ToList();
			var toDelete = existingProperties.Where(x => !incomingIds.Contains(x.Id)).ToList();
			foreach (var item in toDelete)
			{
				existingProperties.Remove(item);
				_dbContext.Entry(item).State = EntityState.Deleted;
			}

			// Upsert
			foreach (var incoming in incomingProperties)
			{
				var existing = existingProperties.FirstOrDefault(x => x.Id == incoming.Id && x.Id > 0);
				if (existing != null)
				{
					MapPropertyValues(existing, incoming);
					existing.FormDefinitionId = formId; // Ensure correct parent

					SyncEnumOptions(existing.EnumOptions, incoming.EnumOptions);

					if (existing.ChildProperties == null)
						_dbContext.Entry(existing).Collection(x => x.ChildProperties).Load();
					SyncProperties(existing.ChildProperties, incoming.ChildProperties, formId);
				}
				else
				{
					var newProp = new FormProperty();
					MapPropertyValues(newProp, incoming);

					// Explicitly Clear ID
					newProp.Id = null;
					newProp.FormDefinitionId = formId;
					newProp.FormSectionId = null;
					newProp.ParentPropertyId = null;

					newProp.EnumOptions = new List<FormPropertyEnumOption>();
					SyncEnumOptions(newProp.EnumOptions, incoming.EnumOptions);

					newProp.ChildProperties = new List<FormProperty>();
					SyncProperties(newProp.ChildProperties, incoming.ChildProperties, formId);

					existingProperties.Add(newProp);
				}
			}
		}
		/// <summary>
		/// همگام‌سازی گزینه‌های Enum
		/// </summary>
		private void SyncEnumOptions(ICollection<FormPropertyEnumOption> existingOptions, List<FormPropertyEnumOption> incomingOptions)
		{
			if (incomingOptions == null) incomingOptions = new List<FormPropertyEnumOption>();

			// Delete
			var incomingIds = incomingOptions.Where(x => x.Id > 0).Select(x => x.Id).ToList();
			var toDelete = existingOptions.Where(x => !incomingIds.Contains(x.Id)).ToList();
			foreach (var item in toDelete)
			{
				existingOptions.Remove(item);
				_dbContext.Entry(item).State = EntityState.Deleted;
			}

			// Upsert
			foreach (var incoming in incomingOptions)
			{
				var existing = existingOptions.FirstOrDefault(x => x.Id == incoming.Id && x.Id > 0);
				if (existing != null)
				{
					existing.Value = incoming.Value;
					existing.Title = incoming.Title;
					existing.EnglishName = incoming.EnglishName;
					existing.OrderIndex = incoming.OrderIndex;
				}
				else
				{
					var newOption = new FormPropertyEnumOption
					{
						// NO ID MAPPING HERE!
						Value = incoming.Value,
						Title = incoming.Title,
						EnglishName = incoming.EnglishName,
						OrderIndex = incoming.OrderIndex
					};

					// Safe guard:
					newOption.Id = null;

					existingOptions.Add(newOption);
				}
			}
		}         // تابع کمکی برای کپی مقادیر
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

		private void AddNestedPropertiesRecursive(EntityMetadata currentEntity, List<object> propsList, string pathPrefix, string displayPrefix, int remainingDepth, int currentDepth)
		{
			if (remainingDepth <= 0 || currentDepth >= 5) return; // Max depth protection

			foreach (var prop in currentEntity.Properties.Where(p => !p.SystemProperty))
			{
				var currentPath = string.IsNullOrEmpty(pathPrefix) ? prop.Name : $"{pathPrefix}.{prop.Name}";
				var currentDisplay = string.IsNullOrEmpty(displayPrefix) ? prop.DisplayName : $"{displayPrefix} - {prop.DisplayName}";

				propsList.Add(new
				{
					name = prop.Name,
					displayName = prop.DisplayName,
					dataType = prop.DataType,
					systemType = prop.SystemType.ToString(),
					fullPath = currentPath,
					isNested = currentDepth > 0,
					parentEntityName = pathPrefix,
					parentEntityDisplayName = displayPrefix
				});

				// Handle related entities
				if (prop.SystemType == SystemType.Entity && !string.IsNullOrEmpty(prop.RelatedEntityTypeFullName))
				{
					var relatedEntityMeta = _entityMetadataCache.Get(prop.RelatedEntityTypeFullName);
					if (relatedEntityMeta != null)
					{
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

						// Recursively add deeper nested properties
						if (remainingDepth > 1)
						{
							AddNestedPropertiesRecursive(relatedEntityMeta, propsList, currentPath, currentDisplay, remainingDepth - 1, currentDepth + 1);
						}
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
					}
				}

				formDefinition.Sections = orderedSections;
		 
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

