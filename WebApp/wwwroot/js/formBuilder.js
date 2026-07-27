/**
 * Form Builder Application
 * Handles dynamic form building for entity definitions
 */

var FormBuilderApp = (function () {
	'use strict';

	var $$ = null; // jQuery reference

	// Helper functions for AJAX
	function get(url, callback) {
		$$.get(url, callback);
	}

	function post(url, data, callback) {
		$$.post(url, data, callback);
	}

	// Application state
	var state = {
		availableModules: [],
		availableEntities: [],
		currentFormDefinitionId: null,
		generatedCode: null,
		currentEditPropertyIndex: null,
		nestedEditPath: [], // Stack for tracking nested entity editing path
		nestedPropertiesStack: [], // Stack for nested properties at each level
		currentNestingLevel: 0,
		sections: [], // Array of sections: {id, title, orderIndex, properties: []}
		currentEditSectionIndex: null
	};

	/**
	 * Initialize the application
	 */
	function init($jq) {
		$$ = $jq || window.$$; // Use passed jQuery or fallback to global $$
		loadModules();
		loadEntities();
		bindEvents();
		initializeSections();
		updatePropertyIndices();
		initSortable();
	}

	/**
	 * Load available modules
	 */
	function loadModules() {
		get('/Panel/FormBuilder/GetAvailableModules', function (response) {
			if (response.isSuccess && response.data) {
				state.availableModules = response.data;
				populateModuleDropdown();
			}
		});
	}

	/**
	 * Load available entities
	 */
	function loadEntities() {
		get('/Panel/FormBuilder/GetAvailableEntities', function (response) {
			if (response.isSuccess && response.data) {
				state.availableEntities = response.data;
				populateEntityDropdowns();
			}
		});
	}

	/**
	 * Initialize sections (create default section if none exist)
	 */
	function initializeSections() {
		if (!state.sections || state.sections.length === 0) {
			state.sections = [{
				 
				title: 'اطلاعات اصلی',
				orderIndex: 0,
				properties: [],
				uqniqId :1
			}];
		}
		renderSections();
	}

	/**
	 * Add a new section
	 */
	function addSection() {
		const sectionId = Math.max(...state.sections.map(s => s.uqniqId), 0) + 1;
		const newSection = {
			uqniqId: sectionId,
			title: 'بخش جدید',
			orderIndex: state.sections.length,
			properties: []
		};

		state.sections.push(newSection);
		renderSections();
		updatePropertyIndices();
		initSortable();

		// Open edit modal for the new section
		openSectionEditModal(newSection);
	}

	/**
	 * Edit section
	 */
	function editSection(sectionId) {
		const section = state.sections.find(s => s.uqniqId === sectionId);
		if (section) {
			openSectionEditModal(section);
		}
	}

	/**
	 * Delete section
	 */
	function deleteSection(sectionId) {
		const sectionIndex = state.sections.findIndex(s => s.uqniqId === sectionId);
		if (sectionIndex === -1) return;

		const section = state.sections[sectionIndex];

		// Don't allow deleting the last section
		if (state.sections.length <= 1) {
			toastr.error('حداقل یک بخش باید وجود داشته باشد', 'خطا');
			return;
		}

		// Check if section has properties
		if (section.properties && section.properties.length > 0) {
			if (!confirm('این بخش دارای ویژگی‌هایی است. با حذف بخش، ویژگی‌ها نیز حذف خواهند شد. آیا اطمینان دارید؟')) {
				return;
			}
		}

		// Remove section
		state.sections.splice(sectionIndex, 1);

		// Update order indices
		state.sections.forEach((section, index) => {
			section.orderIndex = index;
		});

		renderSections();
		updatePropertyIndices();
		initSortable();

		toastr.success('بخش با موفقیت حذف شد', 'موفق');
	}

	/**
	 * Open section edit modal
	 */
	function openSectionEditModal(section) {
		state.currentEditSectionIndex = state.sections.findIndex(s => s.uqniqId === section.uqniqId);
		debugger
		$$('#sectionTitle').val(section.title);
		$$('#sectionId').val(section.uqniqId);

		if (typeof ModalHelper !== 'undefined') {
			ModalHelper.showModal($$('#sectionEditModal'));
		} else {
			$$('#sectionEditModal').modal('show');
		}
	}

	/**
	 * Save section edit
	 */
	function saveSectionEdit() {
		debugger
		const sectionId = parseInt($$('#sectionId').val());
		const title = $$('#sectionTitle').val().trim();

		if (!title) {
			toastr.error('عنوان بخش را وارد کنید', 'خطا');
			return;
		}

		const section = state.sections.find(s => s.uqniqId === sectionId);
		if (section) {
			section.title = title;
			renderSections();

			if (typeof ModalHelper !== 'undefined') {
				ModalHelper.hideModal($$('#sectionEditModal'));
			} else {
				$$('#sectionEditModal').modal('hide');
			}

			toastr.success('بخش با موفقیت ذخیره شد', 'موفق');
		}
	}

	/**
	 * Add property to specific section
	 */
	function addPropertyToSection(sectionId) {
		const section = state.sections.find(s => s.uqniqId === sectionId);
		if (!section) return;

		// Create new property with default values
		const newProp = {
			id: 0,
			propertyName: 'NewProperty',
			displayName: 'ویژگی جدید',
			systemType: 'String',
			colSize: 'col-md-6',
			orderIndex: section.properties.length, // Order within section
			sectionId: sectionId,
			addToTable: true, // Default to true
			required: false,
			showInRelationData: false,
			systemProperty: false,
			maxLength: null,
			enumOptions: [],
			childProperties: []
		};

		// Open modal directly without adding to DOM first
		openPropertyEditModal(null, newProp);
	}

	/**
	 * Render all sections and their properties
	 */
	function renderSections() {
		const $container = $$('#sectionsContainer');
		$container.empty();

		state.sections.forEach(section => {
			const $sectionCard = createSectionCard(section);
			$container.append($sectionCard);
		});
	}

	/**
	 * Create section card
	 */
	function createSectionCard(section) {
		const $card = $(`
			<div class="section-card card card-bordered mb-4" data-section-id="${section.uqniqId}" data-section-order="${section.orderIndex}">
				<div class="card-header">
					<div class="d-flex align-items-center justify-content-between">
						<div class="d-flex align-items-center">
							<i class="ki-duotone ki-menu fs-1 me-3 section-drag-handle" style="cursor: move;">
								<span class="path1"></span>
								<span class="path2"></span>
								<span class="path3"></span>
								<span class="path4"></span>
							</i>
							<h5 class="mb-0 section-title">${section.title}</h5>
						</div>
						<div class="d-flex gap-2">
							<button type="button" class="btn btn-sm btn-icon btn-light-primary btn-edit-section" title="ویرایش بخش">
								<i class="ki-duotone ki-pencil fs-4">
									<span class="path1"></span>
									<span class="path2"></span>
								</i>
							</button>
							<button type="button" class="btn btn-sm btn-icon btn-light-danger btn-delete-section" title="حذف بخش">
								<i class="ki-duotone ki-trash fs-4">
									<span class="path1"></span>
									<span class="path2"></span>
									<span class="path3"></span>
									<span class="path4"></span>
									<span class="path5"></span>
								</i>
							</button>
						</div>
					</div>
				</div>
				<div class="card-body">
					<div class="properties-container row g-3 section-properties" data-section-id="${section.uqniqId}">
						<!-- Properties will be rendered here -->
					</div>
					<div class="text-center mt-3">
						<button type="button" class="btn btn-light-primary btn-add-property-to-section" data-section-id="${section.uqniqId}">
							<i class="ki-duotone ki-plus fs-2"></i>
							افزودن ویژگی به این بخش
						</button>
					</div>
				</div>
			</div>
		`);

		// Render properties for this section
		const $propertiesContainer = $card.find('.section-properties');
		if (section.properties && section.properties.length > 0) {
			section.properties.forEach(propData => {
				const $propCard = createPropertySummaryCard(propData);
				$propertiesContainer.append($propCard);
			});
		}

		return $card;
	}

	/**
	 * Populate module dropdown
	 */
	function populateModuleDropdown() {
		const $select = $$('#moduleSelect');
		let currentValue = $select.val();
		
		// Check for initial value if current value is empty
		if (!currentValue) {
			currentValue = $select.data('initial-value');
		}

		$select.find('option:not(:first)').remove();

		state.availableModules.forEach(function (module) {
			$select.append($('<option></option>').val(module).text(module));
		});

		if (currentValue) {
			$select.val(currentValue);
		}
	}

	/**
	 * Populate entity dropdowns
	 */
	function populateEntityDropdowns() {
		// Populate entity selector in modal
		const $entitySelector = $$('#entitySelector');
		$entitySelector.find('option:not(:first)').remove();

		state.availableEntities.forEach(function (entity) {
			$entitySelector.append(
				$('<option></option>')
					.val(entity.fullName)
					.text(`${entity.displayName} (${entity.name}) - ${entity.module}`)
					.data('entity', entity)
			);
		});

		// Populate related entity dropdowns
		$$('#editPropertyRelatedEntity, #editPropertyRelatedEntityEnum, #nestedEditPropertyRelatedEntity').each(function () {
			const $this = $(this);
			const currentValue = $this.val();

			$this.find('option:not(:first)').remove();

			state.availableEntities.forEach(function (entity) {
				$this.append(
					$('<option></option>')
						.val(entity.fullName)
						.text(`${entity.displayName} (${entity.name})`)
				);
			});

			if (currentValue) {
				$this.val(currentValue);
			}
		});
	}

	/**
	 * Bind all event handlers
	 */
	function bindEvents() {
		console.log('FormBuilder: Binding events');
		
		// Add property button
		$$('#btnAddProperty').on('click', addProperty);

		// Add section button
		$$('#btnAddSection').on('click', addSection);

		// Edit section (delegated)
		$(document).on('click', '.btn-edit-section', function (e) {
			e.stopPropagation();
			const sectionId = parseInt($(this).closest('.section-card').data('section-id'));
			debugger
			editSection(sectionId);
		});

		// Delete section (delegated)
		$(document).on('click', '.btn-delete-section', function (e) {
			e.stopPropagation();
			const sectionId = parseInt($(this).closest('.section-card').data('section-id'));
			deleteSection(sectionId);
		});

		// Add property to section (delegated)
		$(document).on('click', '.btn-add-property-to-section', function (e) {
			debugger
			e.stopPropagation();
			const sectionId = parseInt($(this).data('section-id'));
			addPropertyToSection(sectionId);
		});

		// New module button
		$$('#btnNewModule').on('click', function () {
			var moduleName = prompt('نام ماژول جدید را وارد کنید:', '');
			if (moduleName) {
				$$('#moduleSelect').append($('<option></option>').val(moduleName).text(moduleName));
				$$('#moduleSelect').val(moduleName).trigger('change');
			}
		});

		// Module change - sync with Schema
		$$('#moduleSelect').on('change', function () {
			const module = $(this).val();
			if (module) {
				$$('#formBuilderCard input[data-bind="schema"]').val(module);
			}
		});

		// Load from entity
		$$('#btnLoadFromEntity').on('click', function () {
			$$('#loadEntityModal').modal('show');
		});

		$$('#btnConfirmLoadEntity').on('click', loadFromEntity);

		// Import JSON
		$$('#btnImportJson').on('click', function() {
			$$('#jsonFileInput').click();
		});

		$$('#jsonFileInput').on('change', function(e) {
			const file = e.target.files[0];
			if (!file) return;

			if (!file.name.endsWith('.json')) {
				toastr.error('لطفا یک فایل JSON انتخاب کنید', 'خطا');
				$$('#jsonFileInput').val('');
				return;
			}

			importFromJsonFile(file);
		});

		// Delete property (delegated)
		$(document).on('click', '.properties-container .btn-delete-property', function (e) {
			 
			e.stopPropagation();
			if (confirm('آیا از حذف این ویژگی اطمینان دارید؟')) {
				$(this).closest('.property-summary-card').fadeOut(300, function () {
					var index = $(this).closest("[data-property-index]").data("property-index");
					var sectionId = $(this).closest("[data-section-id]").data("section-id");

					const section = state.sections.find(c => c.uqniqId == sectionId);
					if (section && section.properties) {
						section.properties = section.properties.filter(z => z.orderIndex != index);
					}
					
					$(this).remove();
					updatePropertyIndices();
					// Reinitialize sortable after deleting property
					initSortable();
				});
			}
		});

		// Edit property (delegated)
		$(document).on('click', '.properties-container .btn-edit-property', function (e) {
			e.stopPropagation();
			console.log('Edit button clicked', this);
			const $card = $(this).closest('.property-summary-card');
			openPropertyEditModal($card);
		});

		// Property card click (also opens edit modal)
		$(document).on('click', '.properties-container .property-summary-card', function (e) {
			if (!$(e.target).closest('.btn').length) {
				console.log('Property card clicked', this);
				openPropertyEditModal($(this));
			}
		});

		// Property type change in edit modal
		$$('#editPropertySystemType').on('change', function () {
			updateConditionalEditFields($(this).val());
		});

		// Entity change - load fields for display template
		$$('#editPropertyRelatedEntity').on('change', function () {
			const entityFullName = $(this).val();
			if (entityFullName) {
				loadEntityFieldsForTemplate(entityFullName);
			} else {
				$$('#entityFieldsTree').html('<div class="text-muted text-center py-3">لطفا موجودیت مرتبط را انتخاب کنید</div>');
			}
		});

		// Add field to display template (from tree view) - delegated event
		$(document).on('click', '#entityFieldsTree .tree-field', function (e) {
			e.stopPropagation();
			const $field = $(this);
			const fieldPath = $field.data('field-path');
			const prop = $field.data('field-prop');
			
			if (!fieldPath) {
				toastr.warning('فیلد معتبر نیست', 'هشدار');
				return;
			}
			
			const $template = $$('#editPropertyDisplayTemplate');
			const currentValue = $template.val() || '';
			const fieldPlaceholder = `{${fieldPath}}`;
			$template.val(currentValue + (currentValue ? ' ' : '') + fieldPlaceholder);
			
			// Visual feedback
			$field.css('background-color', 'var(--bs-success-bg-subtle)').delay(300).queue(function() {
				$(this).css('background-color', 'transparent').dequeue();
			});
			
			toastr.success('فیلد به قالب اضافه شد', 'موفق', { timeOut: 1000 });
		});

		// Auto-translate display name to property name
		$$('#editPropertyDisplayName').on('input', debounce(function () {
			const displayName = $(this).val();
			
			if (displayName && displayName.trim()) {
				// Show loading state
				$$('#editPropertyName').addClass('is-loading').val('در حال تولید...');
				$$('#propertyNameStatus').hide();
				
				translatePropertyName(displayName, function(englishName) {
					$$('#editPropertyName').removeClass('is-loading').val(englishName);
					
					// Check for duplicates
					const currentIndex = $$('#editPropertyIndex').val();
					if (checkPropertyExists(englishName, currentIndex)) {
						$$('#editPropertyName').addClass('is-invalid');
						$$('#propertyNameStatus').show().removeClass('text-success').addClass('text-danger').find('span').text('نام تکراری است: ' + englishName);
						$$('#btnSavePropertyEdit').prop('disabled', true);
					} else {
						$$('#editPropertyName').removeClass('is-invalid');
						$$('#propertyNameStatus').show().removeClass('text-danger').addClass('text-success').find('span').text('تولید شد: ' + englishName);
						$$('#btnSavePropertyEdit').prop('disabled', false);
						
						setTimeout(function() {
							if (!$$('#editPropertyName').hasClass('is-invalid')) {
								$$('#propertyNameStatus').fadeOut();
							}
						}, 3000);
					}
				});
			}
		}, 800));

		// Check for duplicates on manual property name change
		$$('#editPropertyName').on('input', function() {
			const name = $(this).val();
			const currentIndex = $$('#editPropertyIndex').val();
			
			if (checkPropertyExists(name, currentIndex)) {
				$(this).addClass('is-invalid');
				$$('#propertyNameStatus').show().removeClass('text-success').addClass('text-danger').find('span').text('نام تکراری است');
				$$('#btnSavePropertyEdit').prop('disabled', true);
			} else {
				$(this).removeClass('is-invalid');
				$$('#propertyNameStatus').hide();
				$$('#btnSavePropertyEdit').prop('disabled', false);
			}
		});

		// Auto-translate for nested property edit modal
		$$('#nestedEditPropertyDisplayName').on('input', debounce(function () {
			const displayName = $(this).val();
			
			if (displayName && displayName.trim()) {
				// Show loading state
				$$('#nestedEditPropertyName').addClass('is-loading').val('در حال تولید...');
				$$('#nestedPropertyNameStatus').hide();
				
				translatePropertyName(displayName, function(englishName) {
					$$('#nestedEditPropertyName').removeClass('is-loading').val(englishName);
					
					// Check for duplicates
					const currentIndex = $$('#nestedEditPropertyIndex').val();
					if (checkNestedPropertyExists(englishName, currentIndex)) {
						$$('#nestedEditPropertyName').addClass('is-invalid');
						$$('#nestedPropertyNameStatus').show().removeClass('text-success').addClass('text-danger').find('span').text('نام تکراری است: ' + englishName);
						$$('#btnSaveNestedPropertyEdit').prop('disabled', true);
					} else {
						$$('#nestedEditPropertyName').removeClass('is-invalid');
						$$('#nestedPropertyNameStatus').show().removeClass('text-danger').addClass('text-success').find('span').text('تولید شد: ' + englishName);
						$$('#btnSaveNestedPropertyEdit').prop('disabled', false);
						
						setTimeout(function() {
							if (!$$('#nestedEditPropertyName').hasClass('is-invalid')) {
								$$('#nestedPropertyNameStatus').fadeOut();
							}
						}, 3000);
					}
				});
			}
		}, 800));

		// Check for duplicates on manual nested property name change
		$$('#nestedEditPropertyName').on('input', function() {
			const name = $(this).val();
			const currentIndex = $$('#nestedEditPropertyIndex').val();
			
			if (checkNestedPropertyExists(name, currentIndex)) {
				$(this).addClass('is-invalid');
				$$('#nestedPropertyNameStatus').show().removeClass('text-success').addClass('text-danger').find('span').text('نام تکراری است');
				$$('#btnSaveNestedPropertyEdit').prop('disabled', true);
			} else {
				$(this).removeClass('is-invalid');
				$$('#nestedPropertyNameStatus').hide();
				$$('#btnSaveNestedPropertyEdit').prop('disabled', false);
			}
		});

		// Save property edit
		$$('#btnSavePropertyEdit').on('click', savePropertyEdit);

		// Add enum option in edit modal
		$$('#btnAddEditEnumOption').on('click', function () {
			addEditEnumOption();
		});

		// Delete enum option in edit modal (delegated)
		$(document).on('click', '#editEnumOptionsContainer .btn-delete-enum-option', function () {
			$(this).closest('.enum-option-row').remove();
		});

		// Show JSON import section
		$('#btnImportEnumFromJson').on('click', function () {
			$('#enumJsonImportSection').slideDown();
			$('#editEnumJsonInput').focus();
		});

		// Cancel JSON import
		$('#btnCancelEnumJson').on('click', function () {
			$('#enumJsonImportSection').slideUp();
			$('#editEnumJsonInput').val('');
		});

		// Parse JSON and import enum options
		$('#btnParseEnumJson').on('click', function () {
			importEnumOptionsFromJson();
		});

		// Edit nested entity button in property edit modal
		$('#btnEditNestedEntity').on('click', function () {
			debugger
			openNestedEntityModal(0); // Level 0 = from main property edit
		});

		// Close nested modal button
		$$('#btnCloseNestedModal').on('click', closeNestedEntityModal);
		$$('#btnCancelNestedEntity').on('click', closeNestedEntityModal);

		// Add nested property
		$$('#btnAddNestedProperty').on('click', function () {
			addNestedProperty();
		});

		// Save nested entity
		$$('#btnSaveNestedEntity').on('click', saveNestedEntity);

		// Edit nested property (delegated) - for properties in nested entity modal
		$(document).on('click', '#nestedPropertiesContainer .btn-edit-property', function (e) {
			e.stopPropagation();
			const $card = $(this).closest('.property-summary-card');
			openNestedPropertyEditModal($card);
		});

		// Delete nested property (delegated)
		$(document).on('click', '#nestedPropertiesContainer .btn-delete-property', function (e) {
			e.stopPropagation();
			if (confirm('آیا از حذف این ویژگی اطمینان دارید؟')) {
				$(this).closest('.nested-property-row').fadeOut(300, function () {
					$(this).remove();
				});
			}
		});

		// Save nested property edit
		$$('#btnSaveNestedPropertyEdit').on('click', saveNestedPropertyEdit);

		// Cancel nested property edit
		$(document).on('click', '#nestedPropertyEditModal [data-bs-dismiss="modal"], #nestedPropertyEditModal .btn-secondary', function() {
			if (typeof ModalHelper !== 'undefined') {
				ModalHelper.hideModal($$('#nestedPropertyEditModal'));
			} else {
				$$('#nestedPropertyEditModal').modal('hide');
				// Manual cleanup
				setTimeout(function() {
					const $backdrops = $('.modal-backdrop');
					if ($backdrops.length > 1) {
						$backdrops.last().remove();
					}
				}, 300);
			}
		});

		// Type change in nested property edit modal
		$$('#nestedEditPropertySystemType').on('change', function () {
			updateNestedConditionalEditFields($(this).val());
		});

		// Nested Entity change - load fields for display template
		$$('#nestedEditPropertyRelatedEntity').on('change', function () {
			const entityFullName = $(this).val();
			if (entityFullName) {
				loadNestedEntityFieldsForTemplate(entityFullName);
			} else {
				$$('#nestedEntityFieldsTree').html('<div class="text-muted text-center py-3">لطفا موجودیت مرتبط را انتخاب کنید</div>');
			}
		});

		// Add field to nested display template (from tree view) - delegated event
		$(document).on('click', '#nestedEntityFieldsTree .tree-field', function (e) {
			e.stopPropagation();
			const $field = $(this);
			const fieldPath = $field.data('field-path');
			const prop = $field.data('field-prop');
			
			if (!fieldPath) {
				toastr.warning('فیلد معتبر نیست', 'هشدار');
				return;
			}
			
			const $template = $$('#nestedEditPropertyDisplayTemplate');
			const currentValue = $template.val() || '';
			const fieldPlaceholder = `{${fieldPath}}`;
			$template.val(currentValue + (currentValue ? ' ' : '') + fieldPlaceholder);
			
			// Visual feedback
			$field.css('background-color', 'var(--bs-success-bg-subtle)').delay(300).queue(function() {
				$(this).css('background-color', 'transparent').dequeue();
			});
			
			toastr.success('فیلد به قالب اضافه شد', 'موفق', { timeOut: 1000 });
		});

		// Add enum option in nested property edit
		$$('#btnAddNestedEditEnumOption').on('click', function () {
			addNestedEditEnumOption();
		});

		// Delete nested enum option
		$(document).on('click', '#nestedEditEnumOptionsContainer .btn-delete-enum-option', function () {
			$(this).closest('.enum-option-row').remove();
		});

		// Edit deeper nested entity (recursive)
		$$('#btnEditDeeperNestedEntity').on('click', function () {
			openDeeperNestedEntityModal();
		});

		// Regenerate property name button (manual trigger)
		$$('#btnRegeneratePropertyName').on('click', function () {
			const displayName = $$('#editPropertyDisplayName').val();
			if (displayName) {
				translatePropertyName(displayName, function(englishName) {
					$$('#editPropertyName').val(englishName);
				});
			} else {
				toastr.warning('لطفا ابتدا نام نمایشی را وارد کنید', 'هشدار');
			}
		});

		// دکمه ترجمه نام انگلیسی به فارسی
		$$('#btnTranslateToFarsi').on('click', function () {
			const propertyName = $$('#editPropertyName').val();
			if (propertyName && propertyName.trim()) {
				const $btn = $$(this);
				$btn.prop('disabled', true);
				$btn.html('<span class="spinner-border spinner-border-sm"></span>');
				
				translateToFarsi(propertyName, function(persianName) {
					$$('#editPropertyDisplayName').val(persianName);
					$btn.prop('disabled', false);
					$btn.html('<i class="ki-duotone ki-abstract-26 fs-2"><span class="path1"></span><span class="path2"></span></i>');
				});
			} else {
				toastr.warning('لطفا ابتدا نام ویژگی (Property Name) را وارد کنید', 'هشدار');
			}
		});

		// Regenerate nested property name button
		$$('#btnRegenerateNestedPropertyName').on('click', function () {
			const displayName = $$('#nestedEditPropertyDisplayName').val();
			if (displayName) {
				translatePropertyName(displayName, function(englishName) {
					$$('#nestedEditPropertyName').val(englishName);
				});
			} else {
				toastr.warning('لطفا ابتدا نام نمایشی را وارد کنید', 'هشدار');
			}
		});

		// Preview code
		$$('#btnPreviewCode').on('click', previewCode);

		// Save files
		$$('#btnSaveFiles, #btnSaveFilesFromPreview').on('click', saveFiles);

		// Publish online
		$$('#btnPublish').on('click', publishForm);
		$$('#btnUnpublish').on('click', unpublishForm);

		// Show JSON preview
		$$('#btnShowJson').on('click', showJsonPreview);
		
		// Download JSON
		$$('#btnExportJsonDownload').on('click', function(e) {
			e.preventDefault();
			downloadJson();
		});
		
		// Download JSON from preview modal
		$$('#btnDownloadJsonFromPreview').on('click', downloadJson);

		// Section modal events
		$$('#btnSaveSectionEdit').on('click', saveSectionEdit);

		// Copy code button (delegated for dynamic content)
		$(document).on('click', '.btn-copy-code', function (e) {
			e.preventDefault();
			const codeId = $(this).data('code-id');
			copyCodeToClipboard(codeId, $(this));
		});
	}

	/**
	 * Add a new property
	 */
	function addProperty() {
		debugger
		// Add to first section by default
		if (state.sections.length > 0) {
			addPropertyToSection(state.sections[0].uqniqId);
		} else {
			// Fallback if no sections exist
			const newProp = {
				id: 0,
				propertyName: 'NewProperty',
				displayName: 'ویژگی جدید',
				systemType: 'String',
				colSize: 'col-md-6',
				orderIndex: -1, // Indicates new property
				sectionId: null,
				addToTable: true, // Default to true
				required: false,
				showInRelationData: false,
				systemProperty: false,
				maxLength: null,
				enumOptions: [],
				childProperties: []
			};
			openPropertyEditModal(null, newProp);
		}
	}

	/**
	 * Create property summary card
	 */
	function createPropertySummaryCard(propData) {
		const colSize = propData.colSize || 'col-md-6';
		// Normalize SystemType for display
		const normalizedSystemType = normalizeSystemType(propData.systemType);
		
		const $card = $(`
			<div class="property-summary-card card card-bordered mb-2 ${colSize}" data-property-index="${propData.orderIndex}">
				<div class="card-body p-3">
					<input type="hidden" class="property-id" value="${propData.id || 0}" />
					<input type="hidden" class="property-order" value="${propData.orderIndex || 0}" />
					<input type="hidden" class="property-data" value='${JSON.stringify(propData)}' />
					
					<div class="d-flex align-items-center justify-content-between">
						<div class="d-flex align-items-center flex-grow-1">
							<i class="ki-duotone ki-menu fs-1 me-3 drag-handle" style="cursor: move;">
								<span class="path1"></span>
								<span class="path2"></span>
								<span class="path3"></span>
								<span class="path4"></span>
							</i>
							<div class="flex-grow-1">
								<div class="d-flex align-items-center mb-1">
									<span class="badge badge-light-primary me-2 property-type-badge">${normalizedSystemType}</span>
									<h6 class="mb-0 property-name-display">${propData.propertyName || 'PropertyName'}</h6>
								</div>
								<small class="text-muted property-display-name-display">${propData.displayName || 'نام نمایشی'}</small>
							</div>
						</div>
						<div class="d-flex gap-2">
							<button type="button" class="btn btn-sm btn-icon btn-light-primary btn-edit-property" title="ویرایش">
								<i class="ki-duotone ki-pencil fs-4">
									<span class="path1"></span>
									<span class="path2"></span>
								</i>
							</button>
							<button type="button" class="btn btn-sm btn-icon btn-light-danger btn-delete-property" title="حذف">
								<i class="ki-duotone ki-trash fs-4">
									<span class="path1"></span>
									<span class="path2"></span>
									<span class="path3"></span>
									<span class="path4"></span>
									<span class="path5"></span>
								</i>
							</button>
						</div>
					</div>
				</div>
			</div>
		`);

		return $card;
	}

	/**
	 * Open property edit modal
	 */
	function openPropertyEditModal($card, data = null) {
		console.log('Opening property edit modal', $card);

		let propData;
		let index;

		if ($card) {
			const propDataJson = $card.find('.property-data').val();
			propData = JSON.parse(propDataJson);
			// Calculate global index
			const $section = $card.closest('.section-card');
			const sectionId = $section.data('section-id');
			const localIndex = $card.closest('.section-properties').find('.property-summary-card').index($card);
			index = getGlobalPropertyIndex(sectionId, localIndex);
		} else if (data) {
			propData = data;
			index = -1; // Indicates new property
		} else {
			return;
		}

		state.currentEditPropertyIndex = index;

		// Normalize SystemType in propData
		propData.systemType = normalizeSystemType(propData.systemType);

		// Reset validation state
		$$('#editPropertyName').removeClass('is-invalid');
		$$('#propertyNameStatus').hide();
		$$('#btnSavePropertyEdit').prop('disabled', false);

		// Populate modal fields
		$$('#editPropertyIndex').val(index);
		$$('#editPropertyName').val(propData.propertyName || '');
		$$('#editPropertyDisplayName').val(propData.displayName || '');
		$$('#editPropertySystemType').val(propData.systemType);
		$$('#editPropertyColSize').val(propData.colSize || 'col-md-6');
		$$('#editPropertySectionId').val(propData.sectionId || 1);
		$$('#editPropertyMaxLength').val(propData.maxLength || '');
		$$('#editPropertyAddToTable').prop('checked', propData.addToTable || false);
		$$('#editPropertyRequired').prop('checked', propData.required || false);
		$$('#editPropertyShowInRelation').prop('checked', propData.showInRelationData || false);
		$$('#editPropertySystemProperty').prop('checked', propData.systemProperty || false);

		// Conditional fields
		$$('#editPropertyRegex').val(propData.regex || '');
		$$('#editPropertyRegexError').val(propData.regexInvalidError || '');
		$$('#editPropertyFileTypes').val(propData.fileTypes || '');
		$$('#editPropertyMaxFileSize').val(propData.maxFileSize || 10);
		$$('#editPropertyAutoStart').val(propData.autoNumberStart || 1);
		$$('#editPropertyAutoStep').val(propData.autoNumberStep || 1);
		$$('#editPropertyRelatedEntity').val(propData.relatedEntityFullName || '');
		$$('#editPropertyEnumName').val(propData.enumName || '');
		$$('#editPropertyRelatedEntityEnum').val(propData.relatedEntityFullName || '');
		$$('#editPropertyChildEntityName').val(propData.childEntityName || '');
		$$('#editPropertyChildEntityDisplayName').val(propData.childEntityDisplayName || '');
		$$('#editPropertyDisplayTemplate').val(propData.displayTemplate || '');

		// Populate enum options
		$$('#editEnumOptionsContainer').empty();
		if (propData.enumOptions && propData.enumOptions.length > 0) {
			propData.enumOptions.forEach(function (opt) {
				addEditEnumOption(opt.value, opt.title, opt.englishName, opt.id || null);
			});
		}

		// Hide JSON import section
		$('#enumJsonImportSection').hide();
		$('#editEnumJsonInput').val('');

		// Store nested properties
		$('#editNestedPropertiesJson').val(JSON.stringify(propData.childProperties || []));
		$('#nestedPropsCount').text((propData.childProperties || []).length);

		// Update conditional fields visibility
		updateConditionalEditFields(propData.systemType);

		// Populate entity dropdowns
		populateEntityDropdowns();

		// Load entity fields for display template if Entity type
		if (propData.systemType === 'Entity' && propData.relatedEntityFullName) {
			loadEntityFieldsForTemplate(propData.relatedEntityFullName);
		}

		// Show modal with proper z-index
		if (typeof ModalHelper !== 'undefined') {
			ModalHelper.showModal($$('#propertyEditModal'), 0);
		} else {
			$$('#propertyEditModal').css('z-index', 1055);
			$$('#propertyEditModal').modal('show');
		}
	}

	/**
	 * Update conditional edit fields visibility
	 */
	function updateConditionalEditFields(systemType) {
		$$('.conditional-edit-field').hide();
		if (systemType) {
			$$(`.conditional-edit-field[data-show-for="${systemType}"]`).show();
		}

		// Max Length visibility (Only for String)
		const $maxLengthInput = $$('#editPropertyMaxLength');
		if ($maxLengthInput.length) {
			const $wrapper = $maxLengthInput.closest('[class*="col-"]');
			if (systemType === 'String') {
				$wrapper.show();
			} else {
				$wrapper.hide();
				$maxLengthInput.val(''); // Clear value if hidden
			}
		}
	}

	/**
	 * Check if property name already exists
	 */
	function checkPropertyExists(name, excludeIndex) {
		let exists = false;
		if (!name) return false;

		// Check across all sections
		for (const section of state.sections) {
			for (let i = 0; i < section.properties.length; i++) {
				const prop = section.properties[i];
				const globalIndex = getGlobalPropertyIndex(section.uqniqId, i);

				// Skip the property currently being edited
				if (excludeIndex !== undefined && excludeIndex !== null && excludeIndex !== '' && globalIndex == excludeIndex) {
					continue;
				}

				if (prop.propertyName && prop.propertyName.toLowerCase() === name.toLowerCase()) {
					exists = true;
					break;
				}
			}
			if (exists) break;
		}

		return exists;
	}

	/**
	 * Get global property index from section and local index
	 */
	function getGlobalPropertyIndex(sectionId, localIndex) {
		let globalIndex = 0;

		for (const section of state.sections) {
			if (section.uqniqId === sectionId) {
				return globalIndex + localIndex;
			}
			globalIndex += section.properties.length;
		}

		return -1;
	}

	/**
	 * Save property edit
	 */
	function savePropertyEdit() {
		debugger
		const index = parseInt($$('#editPropertyIndex').val());
		const systemType = $$('#editPropertySystemType').val();
		const propertyName = $$('#editPropertyName').val();

		if (!propertyName || !$$('#editPropertyDisplayName').val() || !systemType) {
			toastr.error('لطفا فیلدهای الزامی را پر کنید', 'خطا');
			return;
		}

		if (checkPropertyExists(propertyName, index)) {
			toastr.error('نام ویژگی تکراری است', 'خطا');
			$$('#editPropertyName').addClass('is-invalid');
			return;
		}

		const propData = {
			id: 0, // Default for new
			propertyName: propertyName,
			displayName: $$('#editPropertyDisplayName').val(),
			systemType: systemType,
			colSize: $$('#editPropertyColSize').val() || 'col-md-6',
			maxLength: (systemType === 'String') ? (parseInt($$('#editPropertyMaxLength').val()) || null) : null,
			addToTable: $$('#editPropertyAddToTable').is(':checked'),
			required: $$('#editPropertyRequired').is(':checked'),
			showInRelationData: $$('#editPropertyShowInRelation').is(':checked'),
			systemProperty: $$('#editPropertySystemProperty').is(':checked'),
			sectionId: parseInt($$('#editPropertySectionId').val()),
			orderIndex: 0 // Will be set properly below
		};

		// Handle section assignment
		let sectionId = propData.sectionId || null;
		let sectionIndex = -1;

		if (index === -1) {
			// New property - get section from propData or use first section
			sectionId = propData.sectionId || (state.sections.length > 0 ? state.sections[0].uqniqId : null);
		} else {
			// Existing property - find its current section
			const $currentCard = findPropertyCardByIndex(index);
			if ($currentCard) {
				const currentSectionId = $currentCard.closest('.section-card').data('section-id');
				if (currentSectionId) {
					sectionId = currentSectionId;
				}
			}
		}

		propData.sectionId = sectionId;

		// Preserve ID for existing property
		if (index !== -1) {
			const $currentCard = findPropertyCardByIndex(index);
			if ($currentCard) {
				propData.id = parseInt($currentCard.find('.property-id').val()) || 0;
			}
		}

		// Conditional fields
		if (systemType === 'String') {
			propData.regex = $$('#editPropertyRegex').val();
			propData.regexInvalidError = $$('#editPropertyRegexError').val();
		}

		if (systemType === 'File') {
			propData.fileTypes = $$('#editPropertyFileTypes').val();
			propData.maxFileSize = parseInt($$('#editPropertyMaxFileSize').val()) || 10;
		}

		if (systemType === 'AutoNumber') {
			propData.autoNumberStart = parseInt($$('#editPropertyAutoStart').val()) || 1;
			propData.autoNumberStep = parseInt($$('#editPropertyAutoStep').val()) || 1;
		}

		if (systemType === 'Entity') {
			propData.relatedEntityFullName = $$('#editPropertyRelatedEntity').val();
			propData.displayTemplate = $$('#editPropertyDisplayTemplate').val() || '';
			const selectedEntity = state.availableEntities.find(e => e.fullName === propData.relatedEntityFullName);
			if (selectedEntity) {
				propData.relatedEntityName = selectedEntity.name;
			}
		}

		if (systemType === 'Select') {
			propData.enumName = $$('#editPropertyEnumName').val();
			propData.relatedEntityFullName = $$('#editPropertyRelatedEntityEnum').val();

			// Collect enum options
			propData.enumOptions = [];
			$$('#editEnumOptionsContainer .enum-option-row').each(function (idx) {
				const $row = $(this);
				const enumOptionId = $row.find('.enum-option-id').val();
				propData.enumOptions.push({
					id: enumOptionId && parseInt(enumOptionId) > 0 ? parseInt(enumOptionId) : null,
					value: parseInt($row.find('.enum-option-value').val()) || 0,
					title: $row.find('.enum-option-title').val() || '',
					englishName: $row.find('.enum-option-english-name').val() || '',
					orderIndex: idx
				});
			});
		}

		if (systemType === 'ListEntity') {
			propData.childEntityName = $$('#editPropertyChildEntityName').val();
			propData.childEntityDisplayName = $$('#editPropertyChildEntityDisplayName').val();

			try {
				propData.childProperties = JSON.parse($$('#editNestedPropertiesJson').val() || '[]');
			} catch (e) {
				propData.childProperties = [];
			}
		}

		// Add/Update property in the appropriate section
		if (index === -1) {
			// New Property
			addPropertyToSectionData(propData, sectionId);
		} else {
			// Update Existing Property
			updatePropertyInSectionData(propData, index);
		}

		// Re-render sections
		renderSections();
		updatePropertyIndices();

		// Reinitialize sortable after updating property
		initSortable();

		if (typeof ModalHelper !== 'undefined') {
			ModalHelper.hideModal($$('#propertyEditModal'));
		} else {
			$$('#propertyEditModal').modal('hide');
		}

		toastr.success('ویژگی با موفقیت ذخیره شد', 'موفق');
	}

	/**
	 * Add enum option in edit modal
	 */
	function addEditEnumOption(value, title, englishName, id) {
		const $container = $$('#editEnumOptionsContainer');
		const count = $container.find('.enum-option-row').length;

		const $row = $(`
			<div class="enum-option-row row g-2 mb-2 align-items-center">
				<input type="hidden" class="enum-option-id" value="${id || ''}" />
				<div class="col-auto" style="width: 80px;">
					<input type="number" class="form-control form-control-sm enum-option-value" value="${value !== undefined ? value : count}" placeholder="مقدار" />
				</div>
				<div class="col-md-3">
					<input type="text" class="form-control form-control-sm enum-option-title" value="${title || ''}" placeholder="عنوان فارسی" />
				</div>
				<div class="col-md-3">
					<div class="input-group input-group-sm">
						<input type="text" class="form-control form-control-sm enum-option-english-name" value="${englishName || ''}" placeholder="نام انگلیسی" />
						<button type="button" class="btn btn-sm btn-light-primary btn-regen-enum-name" title="تولید مجدد">
							<i class="ki-duotone ki-arrows-circle fs-4">
								<span class="path1"></span>
								<span class="path2"></span>
							</i>
						</button>
					</div>
				</div>
				<div class="col-auto">
					<button type="button" class="btn btn-sm btn-icon btn-light-danger btn-delete-enum-option">
						<i class="ki-duotone ki-trash fs-4">
							<span class="path1"></span>
							<span class="path2"></span>
							<span class="path3"></span>
							<span class="path4"></span>
							<span class="path5"></span>
						</i>
					</button>
				</div>
			</div>
		`);

		$container.append($row);

		// Auto-translate on title change
		$row.find('.enum-option-title').on('input', debounce(function() {
			const persianTitle = $(this).val();
			const $englishInput = $row.find('.enum-option-english-name');
			
			if (persianTitle && persianTitle.trim()) {
				$englishInput.addClass('is-loading').val('ترجمه...');
				
				translatePropertyName(persianTitle, function(englishName) {
					$englishInput.removeClass('is-loading').val(englishName).prop('readonly', false);
				});
			}
		}, 800));

		// Manual regenerate button
		$row.find('.btn-regen-enum-name').on('click', function() {
			const persianTitle = $row.find('.enum-option-title').val();
			const $englishInput = $row.find('.enum-option-english-name');
			
			if (persianTitle && persianTitle.trim()) {
				$englishInput.addClass('is-loading').val('ترجمه...');
				
				translatePropertyName(persianTitle, function(englishName) {
					$englishInput.removeClass('is-loading').val(englishName).prop('readonly', false);
				});
			} else {
				toastr.warning('لطفا ابتدا عنوان فارسی را وارد کنید', 'هشدار');
			}
		});
	}

	/**
	 * Simple function to create a valid identifier from Persian text
	 * This is a fallback - auto-translate will still work after import
	 */
	function makeValidIdentifier(input) {
		if (!input || !input.trim()) {
			return 'Value';
		}

		// Remove invalid characters and replace spaces
		let result = '';
		for (let i = 0; i < input.length; i++) {
			const c = input[i];
			if (/[a-zA-Z0-9_]/.test(c)) {
				result += c;
			} else if (/\s/.test(c)) {
				result += '_';
			}
		}

		// Ensure it doesn't start with a digit
		if (result.length > 0 && /[0-9]/.test(result[0])) {
			result = '_' + result;
		}

		return result || 'Value';
	}

	/**
	 * Import enum options from JSON
	 */
	function importEnumOptionsFromJson() {
		const jsonText = $('#editEnumJsonInput').val().trim();
		
		if (!jsonText) {
			toastr.warning('لطفا JSON را وارد کنید', 'هشدار');
			return;
		}

		try {
			const jsonData = JSON.parse(jsonText);
			
			if (!Array.isArray(jsonData)) {
				toastr.error('JSON باید یک آرایه باشد', 'خطا');
				return;
			}

			if (jsonData.length === 0) {
				toastr.warning('آرایه JSON خالی است', 'هشدار');
				return;
			}

			// Validate and parse each item
			const validOptions = [];
			for (let i = 0; i < jsonData.length; i++) {
				const item = jsonData[i];
				
				if (!item || typeof item !== 'object') {
					toastr.warning(`آیتم ${i + 1} معتبر نیست و نادیده گرفته شد`, 'هشدار');
					continue;
				}

				const id = item.Id !== undefined ? parseInt(item.Id) : (item.id !== undefined ? parseInt(item.id) : null);
				const text = item.Text || item.text || '';
				
				if (id === null || isNaN(id)) {
					toastr.warning(`آیتم ${i + 1}: فیلد Id معتبر نیست`, 'هشدار');
					continue;
				}

				if (!text || !text.trim()) {
					toastr.warning(`آیتم ${i + 1}: فیلد Text خالی است`, 'هشدار');
					continue;
				}

				validOptions.push({
					id: id,
					text: text.trim()
				});
			}

			if (validOptions.length === 0) {
				toastr.error('هیچ گزینه معتبری یافت نشد', 'خطا');
				return;
			}

			// Ask user if they want to clear existing options
			const existingCount = $('#editEnumOptionsContainer .enum-option-row').length;
			let shouldClear = false;
			if (existingCount > 0) {
				shouldClear = confirm(`در حال حاضر ${existingCount} گزینه وجود دارد. آیا می‌خواهید گزینه‌های موجود پاک شوند و گزینه‌های جدید جایگزین شوند؟`);
			}

			if (shouldClear) {
				$('#editEnumOptionsContainer').empty();
			}

			// Add each valid option
			let addedCount = 0;
			validOptions.forEach(function(option) {
				// Generate temporary English name from Persian text (auto-translate will improve it)
				const tempEnglishName = makeValidIdentifier(option.text);
				// Pass id as the 4th parameter (for existing enum options), value as the 1st parameter
				addEditEnumOption(option.id, option.text, tempEnglishName, option.id);
				addedCount++;
			});

			// Hide JSON import section and clear input
			$('#enumJsonImportSection').slideUp();
			$('#editEnumJsonInput').val('');
			
			toastr.success(`${addedCount} گزینه با موفقیت اضافه شد. نام‌های انگلیسی به صورت خودکار بهبود می‌یابند.`, 'موفق');
		} catch (e) {
			toastr.error('خطا در پارس JSON: ' + e.message, 'خطا');
			console.error('JSON Parse Error:', e);
		}
	}

	/**
	 * Open nested entity modal (from property edit modal)
	 */
	function openNestedEntityModal(level) {
		state.currentNestingLevel = level || 0;
		
		const childEntityName = $$('#editPropertyChildEntityName').val();
		const childEntityDisplayName = $$('#editPropertyChildEntityDisplayName').val();

		$$('#childEntityName').val(childEntityName);
		$$('#childEntityDisplayName').val(childEntityDisplayName);
		$$('#currentNestingLevel').text(state.currentNestingLevel + 1);

		// Update breadcrumb
		updateBreadcrumb([{ name: 'ویژگی اصلی', level: 0 }, { name: childEntityDisplayName || 'کلاس فرزند', level: 1 }]);

		// Load existing nested properties
		const $nestedContainer = $$('#nestedPropertiesContainer');
		$nestedContainer.empty();

		try {
			const nestedProps = JSON.parse($$('#editNestedPropertiesJson').val() || '[]');
			state.nestedPropertiesStack = [nestedProps];
			nestedProps.forEach(function (prop) {
				addNestedProperty(prop);
			});
		} catch (e) {
			console.error('Error parsing nested properties:', e);
			state.nestedPropertiesStack = [[]];
		}

		// Show modal with proper z-index
		if (typeof ModalHelper !== 'undefined') {
			ModalHelper.showModal($$('#nestedEntityModal'), 1 + level);
		} else {
			$$('#nestedEntityModal').css('z-index', 1060 + (level * 10));
			$$('#nestedEntityModal').modal('show');
		}
	}

	/**
	 * Close nested entity modal
	 */
	function closeNestedEntityModal() {
		if (state.currentNestingLevel > 0 || state.nestedPropertiesStack.length > 0) {
			// If we're in a deeper level, ask for confirmation
			if (confirm('آیا از بستن این پنجره اطمینان دارید؟ تغییرات ذخیره نشده از بین خواهد رفت.')) {
				if (typeof ModalHelper !== 'undefined') {
					ModalHelper.hideModal($$('#nestedEntityModal'));
				} else {
					$$('#nestedEntityModal').modal('hide');
					// Manual cleanup
					setTimeout(function() {
						const $backdrops = $('.modal-backdrop');
						if ($backdrops.length > 1) {
							$backdrops.last().remove();
						}
					}, 300);
				}
				state.currentNestingLevel = 0;
				state.nestedPropertiesStack = [];
			}
		} else {
			if (typeof ModalHelper !== 'undefined') {
				ModalHelper.hideModal($$('#nestedEntityModal'));
			} else {
				$$('#nestedEntityModal').modal('hide');
				// Manual cleanup
				setTimeout(function() {
					const $backdrops = $('.modal-backdrop');
					if ($backdrops.length > 1) {
						$backdrops.last().remove();
					}
				}, 300);
			}
			state.currentNestingLevel = 0;
			state.nestedPropertiesStack = [];
		}
	}

	/**
	 * Update breadcrumb
	 */
	function updateBreadcrumb(path) {
		const $breadcrumb = $$('#nestedBreadcrumb');
		$breadcrumb.empty();

		path.forEach(function (item, idx) {
			const isActive = idx === path.length - 1;
			const $li = $(`<li class="breadcrumb-item ${isActive ? 'active' : ''}">${item.name}</li>`);
			$breadcrumb.append($li);
		});
	}

	/**
	 * Add nested property
	 */
	function addNestedProperty(propData) {
		// If propData is provided (loading from existing), add directly
		if (propData) {
			const $container = $$('#nestedPropertiesContainer');
			const colSize = propData.colSize || 'col-md-6';
			
			// Normalize SystemType for display
			const normalizedSystemType = normalizeSystemType(propData.systemType);

			const $card = $(`
				<div class="nested-property-row ${colSize}">
					<div class="property-summary-card card card-bordered mb-2">
						<div class="card-body p-3">
							<input type="hidden" class="property-data" value='${JSON.stringify(propData)}' />
							
							<div class="d-flex align-items-center justify-content-between">
								<div class="d-flex align-items-center flex-grow-1">
									<div class="flex-grow-1">
										<div class="d-flex align-items-center mb-1">
											<span class="badge badge-light-primary me-2 property-type-badge">${normalizedSystemType}</span>
											<h6 class="mb-0 property-name-display">${propData.propertyName || 'PropertyName'}</h6>
										</div>
										<small class="text-muted property-display-name-display">${propData.displayName || 'نام نمایشی'}</small>
									</div>
								</div>
								<div class="d-flex gap-2">
									<button type="button" class="btn btn-sm btn-icon btn-light-primary btn-edit-property" title="ویرایش">
										<i class="ki-duotone ki-pencil fs-4">
											<span class="path1"></span>
											<span class="path2"></span>
										</i>
									</button>
									<button type="button" class="btn btn-sm btn-icon btn-light-danger btn-delete-property" title="حذف">
										<i class="ki-duotone ki-trash fs-4">
											<span class="path1"></span>
											<span class="path2"></span>
											<span class="path3"></span>
											<span class="path4"></span>
											<span class="path5"></span>
										</i>
									</button>
								</div>
							</div>
						</div>
					</div>
				</div>
			`);

			$container.append($card);
			return;
		}

		// Create default property if no data provided (User clicked Add)
		const newProp = {
			id: 0,
			propertyName: 'NewProperty',
			displayName: 'ویژگی جدید',
			systemType: 'String',
			colSize: 'col-md-6',
			orderIndex: -1, // Indicates new
			addToTable: true, // Default true
			required: false,
			showInRelationData: false,
			enumOptions: [],
			childProperties: []
		};

		openNestedPropertyEditModal(null, newProp);
	}

	/**
	 * Check if nested property name already exists
	 */
	function checkNestedPropertyExists(name, excludeIndex) {
		let exists = false;
		if (!name) return false;
		
		$$('#nestedPropertiesContainer .nested-property-row').each(function (index) {
			// Skip the property currently being edited
			if (excludeIndex !== undefined && excludeIndex !== null && excludeIndex !== '' && index == excludeIndex) {
				return;
			}
			
			const propDataJson = $(this).find('.property-data').val();
			try {
				const propData = JSON.parse(propDataJson);
				if (propData.propertyName && propData.propertyName.toLowerCase() === name.toLowerCase()) {
					exists = true;
					return false; // break loop
				}
			} catch (e) {}
		});
		
		return exists;
	}

	/**
	 * Open nested property edit modal (for editing properties inside nested entity modal)
	 */
	function openNestedPropertyEditModal($card, data = null) {
		let propData;
		let index;

		if ($card) {
			const propDataJson = $card.find('.property-data').val();
			propData = JSON.parse(propDataJson);
			index = $card.closest('.nested-property-row').index();
		} else if (data) {
			propData = data;
			index = -1; // Indicates new property
		} else {
			return;
		}

		// Normalize SystemType in propData
		propData.systemType = normalizeSystemType(propData.systemType);

		// Reset validation state
		$$('#nestedEditPropertyName').removeClass('is-invalid');
		$$('#nestedPropertyNameStatus').hide();

		// Populate modal fields
		$$('#nestedEditPropertyIndex').val(index);
		$$('#nestedEditPropertyName').val(propData.propertyName || '');
		$$('#nestedEditPropertyDisplayName').val(propData.displayName || '');
		$$('#nestedEditPropertySystemType').val(propData.systemType);
		$$('#nestedEditPropertyColSize').val(propData.colSize || 'col-md-6');
		$$('#nestedEditPropertyMaxLength').val(propData.maxLength || '');
		$$('#nestedEditPropertyAddToTable').prop('checked', propData.addToTable || false);
		$$('#nestedEditPropertyRequired').prop('checked', propData.required || false);
		$$('#nestedEditPropertyShowInRelation').prop('checked', propData.showInRelationData || false);

		// Conditional fields
		$$('#nestedEditPropertyRegex').val(propData.regex || '');
		$$('#nestedEditPropertyRegexError').val(propData.regexInvalidError || '');
		$$('#nestedEditPropertyFileTypes').val(propData.fileTypes || '');
		$$('#nestedEditPropertyMaxFileSize').val(propData.maxFileSize || 10);
		$$('#nestedEditPropertyRelatedEntity').val(propData.relatedEntityFullName || '');
		$$('#nestedEditPropertyDisplayTemplate').val(propData.displayTemplate || '');
		$$('#nestedEditPropertyEnumName').val(propData.enumName || '');
		$$('#nestedEditPropertyChildEntityName').val(propData.childEntityName || '');
		$$('#nestedEditPropertyChildEntityDisplayName').val(propData.childEntityDisplayName || '');

		// Populate enum options
		$$('#nestedEditEnumOptionsContainer').empty();
		if (propData.enumOptions && propData.enumOptions.length > 0) {
			propData.enumOptions.forEach(function (opt) {
				addNestedEditEnumOption(opt.value, opt.title, opt.englishName, opt.id || null);
			});
		}

		// Store deeper nested properties
		$$('#nestedEditNestedPropertiesJson').val(JSON.stringify(propData.childProperties || []));
		$$('#deeperNestedPropsCount').text((propData.childProperties || []).length);
		$$('#nestedEditLevel').text(state.currentNestingLevel + 2);

		// Update conditional fields visibility
		updateNestedConditionalEditFields(propData.systemType);

		// Populate entity dropdowns
		populateEntityDropdowns();

		// Load entity fields for display template if Entity type
		if (propData.systemType === 'Entity' && propData.relatedEntityFullName) {
			loadNestedEntityFieldsForTemplate(propData.relatedEntityFullName);
		}

		// Show modal with proper z-index (higher than nested entity modal)
		if (typeof ModalHelper !== 'undefined') {
			ModalHelper.showModal($$('#nestedPropertyEditModal'), 2 + state.currentNestingLevel);
		} else {
			$$('#nestedPropertyEditModal').css('z-index', 1065 + (state.currentNestingLevel * 10));
			$$('#nestedPropertyEditModal').modal('show');
		}
	}

	/**
	 * Update conditional fields in nested property edit modal
	 */
	function updateNestedConditionalEditFields(systemType) {
		$$('.nested-conditional-edit-field').hide();
		if (systemType) {
			$$(`.nested-conditional-edit-field[data-show-for="${systemType}"]`).show();
		}

		// Max Length visibility (Only for String)
		const $maxLengthInput = $$('#nestedEditPropertyMaxLength');
		if ($maxLengthInput.length) {
			const $wrapper = $maxLengthInput.closest('[class*="col-"]');
			if (systemType === 'String') {
				$wrapper.show();
			} else {
				$wrapper.hide();
				$maxLengthInput.val(''); // Clear value if hidden
			}
		}
	}

	/**
	 * Save nested property edit
	 */
	function saveNestedPropertyEdit() {
		const index = parseInt($$('#nestedEditPropertyIndex').val());
		const systemType = $$('#nestedEditPropertySystemType').val();
		const propertyName = $$('#nestedEditPropertyName').val();

		if (!propertyName || !$$('#nestedEditPropertyDisplayName').val() || !systemType) {
			toastr.error('لطفا فیلدهای الزامی را پر کنید', 'خطا');
			return;
		}

		if (checkNestedPropertyExists(propertyName, index)) {
			toastr.error('نام ویژگی تکراری است', 'خطا');
			$$('#nestedEditPropertyName').addClass('is-invalid');
			return;
		}

		const propData = {
			id: 0,
			propertyName: propertyName,
			displayName: $$('#nestedEditPropertyDisplayName').val(),
			systemType: systemType,
			colSize: $$('#nestedEditPropertyColSize').val() || 'col-md-6',
			maxLength: (systemType === 'String') ? (parseInt($$('#nestedEditPropertyMaxLength').val()) || null) : null,
			addToTable: $$('#nestedEditPropertyAddToTable').is(':checked'),
			required: $$('#nestedEditPropertyRequired').is(':checked'),
			showInRelationData: $$('#nestedEditPropertyShowInRelation').is(':checked'),
			orderIndex: index === -1 ? $$('#nestedPropertiesContainer .nested-property-row').length : index
		};
		
		// Conditional fields
		if (systemType === 'String') {
			propData.regex = $$('#nestedEditPropertyRegex').val();
			propData.regexInvalidError = $$('#nestedEditPropertyRegexError').val();
		}

		if (systemType === 'File') {
			propData.fileTypes = $$('#nestedEditPropertyFileTypes').val();
			propData.maxFileSize = parseInt($$('#nestedEditPropertyMaxFileSize').val()) || 10;
		}

		if (systemType === 'Entity') {
			propData.relatedEntityFullName = $$('#nestedEditPropertyRelatedEntity').val();
			propData.displayTemplate = $$('#nestedEditPropertyDisplayTemplate').val() || '';
			const selectedEntity = state.availableEntities.find(e => e.fullName === propData.relatedEntityFullName);
			if (selectedEntity) {
				propData.relatedEntityName = selectedEntity.name;
			}
		}

		if (systemType === 'Select') {
			propData.enumName = $$('#nestedEditPropertyEnumName').val();
			
			// Collect enum options
			propData.enumOptions = [];
			$$('#nestedEditEnumOptionsContainer .enum-option-row').each(function (idx) {
				const $row = $(this);
				const enumOptionId = $row.find('.enum-option-id').val();
				propData.enumOptions.push({
					id: enumOptionId && parseInt(enumOptionId) > 0 ? parseInt(enumOptionId) : null,
					value: parseInt($row.find('.enum-option-value').val()) || 0,
					title: $row.find('.enum-option-title').val() || '',
					englishName: $row.find('.enum-option-english-name').val() || '',
					orderIndex: idx
				});
			});
		}

		if (systemType === 'ListEntity') {
			propData.childEntityName = $$('#nestedEditPropertyChildEntityName').val();
			propData.childEntityDisplayName = $$('#nestedEditPropertyChildEntityDisplayName').val();
			
			try {
				propData.childProperties = JSON.parse($$('#nestedEditNestedPropertiesJson').val() || '[]');
			} catch (e) {
				propData.childProperties = [];
			}
		}

		// Create/Update card
		const colSize = propData.colSize || 'col-md-6';
		const $newRow = $(`
			<div class="nested-property-row ${colSize}">
				<div class="property-summary-card card card-bordered mb-2">
					<div class="card-body p-3">
						<input type="hidden" class="property-data" value='${JSON.stringify(propData)}' />
						
						<div class="d-flex align-items-center justify-content-between">
							<div class="d-flex align-items-center flex-grow-1">
								<div class="flex-grow-1">
									<div class="d-flex align-items-center mb-1">
										<span class="badge badge-light-primary me-2 property-type-badge">${propData.systemType}</span>
										<h6 class="mb-0 property-name-display">${propData.propertyName}</h6>
									</div>
									<small class="text-muted property-display-name-display">${propData.displayName}</small>
								</div>
							</div>
							<div class="d-flex gap-2">
								<button type="button" class="btn btn-sm btn-icon btn-light-primary btn-edit-property" title="ویرایش">
									<i class="ki-duotone ki-pencil fs-4">
										<span class="path1"></span>
										<span class="path2"></span>
									</i>
								</button>
								<button type="button" class="btn btn-sm btn-icon btn-light-danger btn-delete-property" title="حذف">
									<i class="ki-duotone ki-trash fs-4">
										<span class="path1"></span>
										<span class="path2"></span>
										<span class="path3"></span>
										<span class="path4"></span>
										<span class="path5"></span>
									</i>
								</button>
							</div>
						</div>
					</div>
				</div>
			</div>
		`);

		if (index === -1) {
			$$('#nestedPropertiesContainer').append($newRow);
		} else {
			const $oldRow = $$('#nestedPropertiesContainer .nested-property-row').eq(index);
			$oldRow.replaceWith($newRow);
		}
		
		// Close modal using ModalHelper to properly clean up backdrop
		if (typeof ModalHelper !== 'undefined') {
			ModalHelper.hideModal($$('#nestedPropertyEditModal'));
		} else {
			$$('#nestedPropertyEditModal').modal('hide');
			// Manual cleanup if ModalHelper not available
			setTimeout(function() {
				const $backdrops = $('.modal-backdrop');
				if ($backdrops.length > 1) {
					$backdrops.last().remove();
				}
			}, 300);
		}
		
		toastr.success('ویژگی با موفقیت ذخیره شد', 'موفق');
	}

	/**
	 * Save nested entity
	 */
	function saveNestedEntity() {
		const childEntityName = $$('#childEntityName').val();
		const childEntityDisplayName = $$('#childEntityDisplayName').val();

		if (!childEntityName || !childEntityDisplayName) {
			toastr.error('لطفا نام کلاس فرزند و نام نمایشی را وارد کنید', 'خطا');
			return;
		}

		// Collect all nested properties
		const nestedProperties = [];
		$$('#nestedPropertiesContainer .nested-property-row').each(function (index) {
			const propDataJson = $(this).find('.property-data').val();
			try {
				const propData = JSON.parse(propDataJson);
				propData.orderIndex = index;
				nestedProperties.push(propData);
			} catch (e) {
				console.error('Error parsing nested property data:', e);
			}
		});

		// Save to the main property's nested properties JSON
		$$('#editNestedPropertiesJson').val(JSON.stringify(nestedProperties));
		$$('#nestedPropsCount').text(nestedProperties.length);

		// Update child entity names in main property
		$$('#editPropertyChildEntityName').val(childEntityName);
		$$('#editPropertyChildEntityDisplayName').val(childEntityDisplayName);

		if (typeof ModalHelper !== 'undefined') {
			ModalHelper.hideModal($$('#nestedEntityModal'));
		} else {
			$$('#nestedEntityModal').modal('hide');
			// Manual cleanup
			setTimeout(function() {
				const $backdrops = $('.modal-backdrop');
				if ($backdrops.length > 1) {
					$backdrops.last().remove();
				}
			}, 300);
		}

		state.currentNestingLevel = 0;
		state.nestedPropertiesStack = [];

		toastr.success('موجودیت فرزند با موفقیت ذخیره شد', 'موفق');
	}

	/**
	 * Add enum option in nested property edit modal
	 */
	function addNestedEditEnumOption(value, title, englishName, id) {
		const $container = $$('#nestedEditEnumOptionsContainer');
		const count = $container.find('.enum-option-row').length;

		const $row = $(`
			<div class="enum-option-row row g-2 mb-2 align-items-center">
				<input type="hidden" class="enum-option-id" value="${id || ''}" />
				<div class="col-auto" style="width: 80px;">
					<input type="number" class="form-control form-control-sm enum-option-value" value="${value !== undefined ? value : count}" placeholder="مقدار" />
				</div>
				<div class="col-md-3">
					<input type="text" class="form-control form-control-sm enum-option-title" value="${title || ''}" placeholder="عنوان فارسی" />
				</div>
				<div class="col-md-3">
					<div class="input-group input-group-sm">
						<input type="text" class="form-control form-control-sm enum-option-english-name" value="${englishName || ''}" placeholder="نام انگلیسی" />
						<button type="button" class="btn btn-sm btn-light-primary btn-regen-enum-name" title="تولید مجدد">
							<i class="ki-duotone ki-arrows-circle fs-4">
								<span class="path1"></span>
								<span class="path2"></span>
							</i>
						</button>
					</div>
				</div>
				<div class="col-auto">
					<button type="button" class="btn btn-sm btn-icon btn-light-danger btn-delete-enum-option">
						<i class="ki-duotone ki-trash fs-4">
							<span class="path1"></span>
							<span class="path2"></span>
							<span class="path3"></span>
							<span class="path4"></span>
							<span class="path5"></span>
						</i>
					</button>
				</div>
			</div>
		`);

		$container.append($row);

		// Auto-translate on title change
		$row.find('.enum-option-title').on('input', debounce(function() {
			const persianTitle = $(this).val();
			const $englishInput = $row.find('.enum-option-english-name');
			
			if (persianTitle && persianTitle.trim()) {
				$englishInput.addClass('is-loading').val('ترجمه...');
				
				translatePropertyName(persianTitle, function(englishName) {
					$englishInput.removeClass('is-loading').val(englishName).prop('readonly', false);
				});
			}
		}, 800));

		// Manual regenerate button
		$row.find('.btn-regen-enum-name').on('click', function() {
			const persianTitle = $row.find('.enum-option-title').val();
			const $englishInput = $row.find('.enum-option-english-name');
			
			if (persianTitle && persianTitle.trim()) {
				$englishInput.addClass('is-loading').val('ترجمه...');
				
				translatePropertyName(persianTitle, function(englishName) {
					$englishInput.removeClass('is-loading').val(englishName).prop('readonly', false);
				});
			} else {
				toastr.warning('لطفا ابتدا عنوان فارسی را وارد کنید', 'هشدار');
			}
		});
	}

	/**
	 * Open deeper nested entity modal (for ListEntity inside nested properties)
	 * This handles n-level recursion
	 */
	function openDeeperNestedEntityModal() {
		const childEntityName = $$('#nestedEditPropertyChildEntityName').val();
		const childEntityDisplayName = $$('#nestedEditPropertyChildEntityDisplayName').val();

		if (!childEntityName || !childEntityDisplayName) {
			toastr.error('لطفا نام کلاس فرزند را وارد کنید', 'خطا');
			return;
		}

		// Save current nested property data to stack
		const currentNestedProps = [];
		$$('#nestedPropertiesContainer .nested-property-row').each(function (idx) {
			const propDataJson = $(this).find('.property-data').val();
			currentNestedProps.push(JSON.parse(propDataJson));
		});
		state.nestedPropertiesStack.push(currentNestedProps);

		// Increment nesting level
		state.currentNestingLevel++;

		// Hide nested property edit modal using ModalHelper
		if (typeof ModalHelper !== 'undefined') {
			ModalHelper.hideModal($$('#nestedPropertyEditModal'));
		} else {
			$$('#nestedPropertyEditModal').modal('hide');
		}

		// Update nested entity modal for deeper level
		$$('#childEntityName').val(childEntityName);
		$$('#childEntityDisplayName').val(childEntityDisplayName);
		$$('#currentNestingLevel').text(state.currentNestingLevel + 1);

		// Update breadcrumb to show path
		const breadcrumbPath = [{ name: 'ویژگی اصلی', level: 0 }];
		for (let i = 0; i <= state.currentNestingLevel; i++) {
			breadcrumbPath.push({ 
				name: i === 0 ? $$('#editPropertyChildEntityDisplayName').val() : childEntityDisplayName,
				level: i + 1 
			});
		}
		updateBreadcrumb(breadcrumbPath);

		// Load existing deeper nested properties
		const $nestedContainer = $$('#nestedPropertiesContainer');
		$nestedContainer.empty();

		try {
			const deeperProps = JSON.parse($$('#nestedEditNestedPropertiesJson').val() || '[]');
			deeperProps.forEach(function (prop) {
				addNestedProperty(prop);
			});
		} catch (e) {
			console.error('Error parsing deeper nested properties:', e);
		}

		// Show modal with increased z-index for deeper level
		if (typeof ModalHelper !== 'undefined') {
			ModalHelper.showModal($$('#nestedEntityModal'), 1 + state.currentNestingLevel);
		} else {
			$$('#nestedEntityModal').css('z-index', 1060 + (state.currentNestingLevel * 10));
			$$('#nestedEntityModal').modal('show');
		}
	}

	/**
	 * Add property to section data
	 */
	function addPropertyToSectionData(propData, sectionId) {
		const section = state.sections.find(s => s.uqniqId === sectionId);
		if (section) {
			propData.orderIndex = section.properties.length;
			section.properties.push(propData);
		}
	}

	/**
	 * Update property in section data
	 */
	function updatePropertyInSectionData(propData, globalIndex) {
		// Find the property across all sections
		let found = false;
		for (const section of state.sections) {
			const propIndex = section.properties.findIndex(p => {
				// Find by property ID or by matching criteria
				return p.id === propData.id || (
					p.propertyName === propData.propertyName &&
					p.displayName === propData.displayName
				);
			});

			if (propIndex !== -1) {
				section.properties[propIndex] = propData;
				found = true;
				break;
			}
		}

		// If not found (shouldn't happen), add to first section
		if (!found && state.sections.length > 0) {
			state.sections[0].properties.push(propData);
		}
	}

	/**
	 * Find property card by global index
	 */
	function findPropertyCardByIndex(globalIndex) {
		let currentIndex = 0;
		for (const section of state.sections) {
			for (let i = 0; i < section.properties.length; i++) {
				if (currentIndex === globalIndex) {
					const sectionId = section.uqniqId;
					return $$(`.section-properties[data-section-id="${sectionId}"] .property-summary-card`).eq(i);
				}
				currentIndex++;
			}
		}
		return null;
	}

	/**
	 * Collect all properties data
	 */
	function collectPropertiesData() {
		const properties = [];

		// Collect properties from all sections
		state.sections.forEach(section => {
			if (section.properties) {
				section.properties.forEach(prop => {
					properties.push(prop);
				});
			}
		});

		const sectionsData = getSectionsData();
		$$('#formBuilderCard').data('properties', properties);
		$$('#formBuilderCard').data('sections', sectionsData);
		return properties;
	}

	/**
	 * Get deep-cloned sections data for server payloads
	 */
	function getSectionsData() {
		return state.sections.map((section, sectionIndex) => {
			const sectionData = {
				id: (section.id && section.id > 0) ? section.id : null,
				title: section.title || `بخش ${sectionIndex + 1}`,
				orderIndex: sectionIndex,
				properties: []
			};

			(section.properties || []).forEach((prop, propIndex) => {
				const propClone = JSON.parse(JSON.stringify(prop));
				propClone.orderIndex = propClone.orderIndex ?? propIndex;
				propClone.formSectionId = (section.id && section.id > 0) ? section.id : null;
				sectionData.properties.push(propClone);
			});

			return sectionData;
		});
	}

	/**
	 * Update property indices
	 */
	function updatePropertyIndices() {
		let globalIndex = 0;

		state.sections.forEach(section => {
			section.properties.forEach((prop, sectionIndex) => {
				prop.orderIndex = globalIndex;
				globalIndex++;
			});
		});

		// Update DOM elements
		$$('.section-properties .property-summary-card').each(function (index) {
			$(this).find('.property-order').val(index);
			$(this).attr('data-property-index', index);

			// Update property data
			const propDataJson = $(this).find('.property-data').val();
			const propData = JSON.parse(propDataJson);
			propData.orderIndex = index;
			$(this).find('.property-data').val(JSON.stringify(propData));
		});
	}

	/**
	 * Initialize sortable for drag and drop
	 */
	var sectionSortableInstance = null;
	var propertySortableInstances = [];

	function initSortable() {
		// Initialize section sorting
		initSectionSortable();

		// Initialize property sorting within each section
		initPropertySortables();
	}

	function initSectionSortable() {
		const $container = $$('#sectionsContainer');

		if (!$container.length) {
			return;
		}

		// Destroy existing sortable if any
		if (sectionSortableInstance) {
			try {
				if (typeof sectionSortableInstance.destroy === 'function') {
					sectionSortableInstance.destroy();
				} else {
					$container.sortable('destroy');
				}
			} catch (e) {
				// Ignore if already destroyed
			}
			sectionSortableInstance = null;
		}

		// Try jQuery UI Sortable first (most likely available)
		if (typeof $.fn.sortable !== 'undefined') {
			$container.sortable({
				handle: '.section-drag-handle',
				items: '.section-card',
				placeholder: 'sortable-placeholder',
				opacity: 0.8,
				tolerance: 'pointer',
				axis: 'y',
				revert: 200,
				start: function (e, ui) {
					ui.placeholder.height(ui.item.height());
					ui.placeholder.addClass('border border-primary border-dashed');
				},
				stop: function (e, ui) {
					updateSectionOrder();
					updatePropertyIndices();
				}
			});
			sectionSortableInstance = $container;
			return;
		}

		// Fallback to SortableJS if available
		if (typeof Sortable !== 'undefined') {
			const container = document.getElementById('sectionsContainer');
			if (container) {
				sectionSortableInstance = new Sortable(container, {
					handle: '.section-drag-handle',
					animation: 150,
					ghostClass: 'sortable-ghost',
					chosenClass: 'sortable-chosen',
					dragClass: 'sortable-drag',
					onEnd: function () {
						updateSectionOrder();
						updatePropertyIndices();
					}
				});
				return;
			}
		}

		console.warn('No sortable library found for sections. Section drag and drop will not work.');
	}

	function initPropertySortables() {
		// Destroy existing property sortables
		propertySortableInstances.forEach(instance => {
			try {
				if (typeof instance.destroy === 'function') {
					instance.destroy();
				}
			} catch (e) {
				// Ignore errors
			}
		});
		propertySortableInstances = [];

		// Initialize sortable for each section's properties
		$$('.section-properties').each(function() {
			const $container = $(this);

			// Try jQuery UI Sortable first
			if (typeof $.fn.sortable !== 'undefined') {
				$container.sortable({
					handle: '.drag-handle',
					items: '.property-summary-card',
					placeholder: 'sortable-placeholder',
					opacity: 0.8,
					tolerance: 'pointer',
					connectWith: '.section-properties',
					axis: false,
					revert: 200,
					start: function (e, ui) {
						ui.placeholder.height(ui.item.height());
						ui.placeholder.addClass('border border-primary border-dashed');
					},
					stop: function (e, ui) {
						updatePropertiesAfterDrag();
					}
				});
				propertySortableInstances.push($container);
			}
			// Fallback to SortableJS
			else if (typeof Sortable !== 'undefined') {
				const instance = new Sortable(this, {
					handle: '.drag-handle',
					animation: 150,
					ghostClass: 'sortable-ghost',
					chosenClass: 'sortable-chosen',
					dragClass: 'sortable-drag',
					group: 'properties',
					onEnd: function () {
						updatePropertiesAfterDrag();
					}
				});
				propertySortableInstances.push(instance);
			}
		});
	}

	function updateSectionOrder() {
		$$('.section-card').each(function(index) {
			const sectionId = $(this).data('section-id');
			const section = state.sections.find(s => s.uqniqId === sectionId);
			if (section) {
				section.orderIndex = index;
			}
		});

		// Reorder sections array
		state.sections.sort((a, b) => a.orderIndex - b.orderIndex);
	}

	function updatePropertiesAfterDrag() {
		// Update state.sections based on current DOM structure
		state.sections.forEach(section => {
			section.properties = [];
		});

		$$('.section-card').each(function() {
			const sectionId = $(this).data('section-id');
			const section = state.sections.find(s => s.uqniqId === sectionId);
			if (section) {
				$(this).find('.section-properties .property-summary-card').each(function() {
					const propDataJson = $(this).find('.property-data').val();
					try {
						const propData = JSON.parse(propDataJson);
						section.properties.push(propData);
					} catch (e) {
						console.error('Error parsing property data:', e);
					}
				});
			}
		});

		updatePropertyIndices();
	}

	/**
	 * Convert SystemType enum to string
	 * Handles both numeric enum values and string values
	 */
	function normalizeSystemType(systemType) {
		if (!systemType && systemType !== 0) {
			return 'String';
		}

		// If already a string, return as is
		if (typeof systemType === 'string') {
			return systemType;
		}

		// Map numeric enum values to string
		const SystemTypeMap = {
			0: 'String',
			1: 'Boolean',
			2: 'DateTime',
			3: 'Date',
			4: 'DateTimeShamsi',
			5: 'DateShamsi',
			6: 'Long',
			7: 'Int',
			8: 'Select',
			9: 'File',
			10: 'Entity',
			11: 'ListEntity',
			12: 'ListString',
			13: 'ListLong',
			14: 'Decimal',
			15: 'AutoNumber'
		};

		return SystemTypeMap[systemType] || 'String';
	}

	/**
	 * Populate form from FormDefinition object
	 */
	function populateFormFromDefinition(formDef) {
		if (!formDef) {
			initializeSections();
			return;
		}

		// Populate form fields
		$$('#formBuilderCard input[data-bind="entityName"]').val(formDef.entityName || '');
		$$('#formBuilderCard input[data-bind="displayName"]').val(formDef.displayName || '');
		$$('#formBuilderCard select[data-bind="module"]').val(formDef.module || '');
		$$('#formBuilderCard input[data-bind="schema"]').val(formDef.schema || 'dbo');
		$$('#formBuilderCard input[data-bind="namespace"]').val(formDef.namespace || '');
		$$('#formBuilderCard input[data-bind="baseEntityType"]').val(formDef.baseEntityType || 'BaseEntity');
		$$('#formBuilderCard textarea[data-bind="description"]').val(formDef.description || '');
		$$('#formBuilderCard input[data-bind="id"]').val(formDef.id || '');

		// Set current form definition ID
		state.currentFormDefinitionId = formDef.id || null;

		// Handle sections - normalize and map all properties correctly
		if (formDef.sections && Array.isArray(formDef.sections) && formDef.sections.length > 0) {
			state.sections = formDef.sections.map((section, sectionIndex) => {
				// Ensure unique ID for sections
				const sectionId = section.id && section.id > 0 ? section.id : null;
				const uqniqId = section.uqniqId || sectionId || (sectionIndex + 1);

				return {
					id: sectionId,
					uqniqId: uqniqId,
					title: section.title || `بخش ${sectionIndex + 1}`,
					orderIndex: section.orderIndex ?? sectionIndex,
					properties: (section.properties || []).map((prop, propIndex) => {
						return normalizePropertyData(prop, propIndex);
					})
				};
			});
		} else if (formDef.properties && Array.isArray(formDef.properties) && formDef.properties.length > 0) {
			// Legacy support: if no sections but has properties, create default section with all properties
			state.sections = [{
				id: null,
				title: 'اطلاعات اصلی',
				orderIndex: 0,
				properties: [],
				uqniqId: 1
			}];

			formDef.properties.forEach(function (prop, index) {
				state.sections[0].properties.push(normalizePropertyData(prop, index));
			});
		} else {
			// No data, initialize with empty default section
			initializeSections();
			return;
		}

		renderSections();
		updatePropertyIndices();

		// Reinitialize sortable after loading properties
		initSortable();
	}

	/**
	 * Normalize property data including enumOptions and childProperties
	 */
	function normalizePropertyData(prop, index) {
		// Normalize system type
		prop.systemType = normalizeSystemType(prop.systemType);

		// Ensure ID is set correctly
		if (!prop.id && prop.id !== null) {
			prop.id = null;
		}

		// Normalize enumOptions
		if (prop.enumOptions && Array.isArray(prop.enumOptions)) {
			prop.enumOptions = prop.enumOptions.map((opt, optIndex) => ({
				id: opt.id && opt.id > 0 ? opt.id : null,
				value: opt.value ?? optIndex,
				title: opt.title || '',
				englishName: opt.englishName || '',
				orderIndex: opt.orderIndex ?? optIndex
			}));
		} else {
			prop.enumOptions = [];
		}

		// Normalize childProperties recursively
		if (prop.childProperties && Array.isArray(prop.childProperties)) {
			prop.childProperties = prop.childProperties.map((childProp, childIndex) => {
				return normalizePropertyData(childProp, childIndex);
			});
		} else {
			prop.childProperties = [];
		}

		// Ensure orderIndex
		if (prop.orderIndex === undefined || prop.orderIndex === null) {
			prop.orderIndex = index;
		}

		return prop;
	}

	 
	function loadFormDefinitionById(id) {
		if (!id || id === 0) {
			initializeSections();
			return;
		}

		get(`/Panel/FormBuilder/GetById?id=${id}`, function (response) {
			if (response.isSuccess && response.data) {
				populateFormFromDefinition(response.data);
				loadPublishStatus(id);
			} else {
				toastr.error('خطا در بارگذاری اطلاعات فرم', 'خطا');
				initializeSections();
			}
		});
	}

	/**
	 * Import from JSON file
	 */
	function importFromJsonFile(file) {
		const reader = new FileReader();
		
		reader.onload = function(e) {
			try {
				const jsonText = e.target.result;
				const formDefinition = JSON.parse(jsonText);
				
				// Validate JSON structure
				if (!formDefinition.entityName || !formDefinition.displayName) {
					toastr.error('فایل JSON معتبر نیست. لطفا فایل صحیح را انتخاب کنید.', 'خطا');
					$$('#jsonFileInput').val('');
					return;
				}

				// Populate form fields
				populateFormFromDefinition(formDefinition);
				
				// Clear file input
				$$('#jsonFileInput').val('');
				
				toastr.success('فایل JSON با موفقیت بارگذاری شد', 'موفق');
			} catch (error) {
				console.error('Error parsing JSON:', error);
				toastr.error('خطا در خواندن فایل JSON: ' + error.message, 'خطا');
				$$('#jsonFileInput').val('');
			}
		};
		
		reader.onerror = function() {
			toastr.error('خطا در خواندن فایل', 'خطا');
			$$('#jsonFileInput').val('');
		};
		
		reader.readAsText(file);
	}

	/**
	 * Load from existing entity
	 */
	function loadFromEntity() {
		const entityFullName = $$('#entitySelector').val();
		if (!entityFullName) {
			toastr.error('لطفا یک موجودیت انتخاب کنید', 'خطا');
			return;
		}

		const $btn = $$('#btnConfirmLoadEntity').block();

		post('/Panel/FormBuilder/LoadFromEntity', { entityFullName: entityFullName }, function (response) {
			$btn.block(false);

			if (!response.isSuccess) {
				toastr.error(response.message || 'خطا در بارگذاری موجودیت', 'خطا');
				return;
			}

			const formDef = response.data;

			// Use shared function to populate form
			populateFormFromDefinition(formDef);
			
			$$('#loadEntityModal').modal('hide');
			toastr.success('موجودیت با موفقیت بارگذاری شد', 'موفق');
		});
	}

	/**
	 * Preview generated code
	 */
	/**
	 * Get FormDefinition data
	 */
	function getFormDefinitionData() {
		// Collect all properties data
		collectPropertiesData();

		// Get form data
		const formData = $$('#formBuilderCard').dataBind();

		// Validate
		if (!formData.entityName || !formData.displayName) {
			toastr.error('لطفا ابتدا اطلاعات موجودیت را تکمیل کنید', 'خطا');
			return null;
		}

		const sectionsData = $$('#formBuilderCard').data('sections') || getSectionsData();

		// Prepare FormDefinition object
		return {
			id: formData.id || null,
			entityName: formData.entityName,
			displayName: formData.displayName,
			module: formData.module,
			schema: formData.schema || 'dbo',
			namespace: formData.namespace,
			baseEntityType: formData.baseEntityType || 'BaseEntity',
			description: formData.description,
			isFromExistingEntity: formData.isFromExistingEntity || false,
			sourceEntityFullName: formData.sourceEntityFullName,
			sections: sectionsData,
			properties: collectPropertiesData() // Keep for backward compatibility
		};
	}

	/**
	 * Show JSON preview in modal
	 */
	function showJsonPreview() {
		const formDefinition = getFormDefinitionData();
		if (!formDefinition) return;

		const $btn = $$('#btnShowJson').block();
		
		// Use XMLHttpRequest to get JSON as text
		// Use same endpoint as download with preview=true query parameter
		const xhr = new XMLHttpRequest();
		xhr.open('POST', '/Panel/FormBuilder/ExportToJson?preview=true', true);
		xhr.setRequestHeader('Content-Type', 'application/json');
		xhr.responseType = 'text'; // Get as text for display
		
		xhr.onload = function() {
			$btn.block(false);
			
			if (xhr.status === 200) {
				// Get filename from response header
				let filename = 'FormDefinition.json';
				const disposition = xhr.getResponseHeader('Content-Disposition');
				if (disposition && disposition.indexOf('attachment') !== -1) {
					const filenameRegex = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/;
					const matches = filenameRegex.exec(disposition);
					if (matches != null && matches[1]) {
						filename = matches[1].replace(/['"]/g, '');
					}
				}

				// Store filename for download
				$$('#jsonPreviewModal').data('formDefinition', formDefinition);
				$$('#jsonPreviewModal').data('filename', filename);

				// Display JSON in modal
				$$('#jsonFileName').text(filename);
				
				// Format JSON for display (already formatted from server)
				let jsonText = xhr.responseText;
				
				// Try to parse and reformat if needed
				try {
					const jsonObj = JSON.parse(jsonText);
					jsonText = JSON.stringify(jsonObj, null, 2);
				} catch (e) {
					// Use as is if already formatted
				}
				
				$$('#jsonCode').text(jsonText);
				
				// Highlight syntax if Prism is available
				if (typeof Prism !== 'undefined') {
					Prism.highlightElement($$('#jsonCode')[0]);
				}
				
				// Show modal
				$$('#jsonPreviewModal').modal('show');
			} else {
				// Handle error response
				try {
					const errorObj = JSON.parse(xhr.responseText);
					toastr.error(errorObj.message || 'خطا در دریافت JSON', 'خطا');
				} catch (e) {
					toastr.error(xhr.responseText || 'خطا در دریافت JSON', 'خطا');
				}
			}
		};
		
		xhr.onerror = function() {
			$btn.block(false);
			toastr.error('خطا در ارتباط با سرور', 'خطا');
		};
		
		xhr.send(JSON.stringify(formDefinition));
	}

	/**
	 * Download JSON file
	 */
	function downloadJson() {
		const formDefinition = getFormDefinitionData();
		if (!formDefinition) return;

		const $btn = $$('#jsonPreviewModal').is(':visible') 
			? $$('#btnDownloadJsonFromPreview').block() 
			: $$('#btnExportJsonDownload').block();
		
		// Use XMLHttpRequest for blob response
		const xhr = new XMLHttpRequest();
		xhr.open('POST', '/Panel/FormBuilder/ExportToJson', true);
		xhr.setRequestHeader('Content-Type', 'application/json');
		xhr.responseType = 'blob';
		
		xhr.onload = function() {
			$btn.block(false);
			
			if (xhr.status === 200) {
				// Get filename from response header or use default
				let filename = 'FormDefinition.json';
				const disposition = xhr.getResponseHeader('Content-Disposition');
				if (disposition && disposition.indexOf('attachment') !== -1) {
					const filenameRegex = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/;
					const matches = filenameRegex.exec(disposition);
					if (matches != null && matches[1]) {
						filename = matches[1].replace(/['"]/g, '');
					}
				}

				// Create download link
				const blob = xhr.response;
				const url = window.URL.createObjectURL(blob);
				const a = document.createElement('a');
				a.href = url;
				a.download = filename;
				document.body.appendChild(a);
				a.click();
				window.URL.revokeObjectURL(url);
				document.body.removeChild(a);
				
				toastr.success('فایل JSON با موفقیت دانلود شد', 'موفق');
			} else {
				// Handle error response
				const blob = xhr.response;
				if (blob instanceof Blob) {
					blob.text().then(function(text) {
						try {
							const errorObj = JSON.parse(text);
							toastr.error(errorObj.message || 'خطا در ایجاد فایل JSON', 'خطا');
						} catch (e) {
							toastr.error(text || 'خطا در ایجاد فایل JSON', 'خطا');
						}
					});
				} else {
					toastr.error('خطا در ایجاد فایل JSON', 'خطا');
				}
			}
		};
		
		xhr.onerror = function() {
			$btn.block(false);
			toastr.error('خطا در ارتباط با سرور', 'خطا');
		};
		
		xhr.send(JSON.stringify(formDefinition));
	}

	function previewCode() {
		 
		collectPropertiesData();
		const formData = $$('#formBuilderCard').dataBind();
		formData.properties = $$('#formBuilderCard').data('properties');
		formData.sections = $$('#formBuilderCard').data('sections') || getSectionsData();

		const $btn = $$('#btnPreviewCode').block();

		post('/Panel/FormBuilder/GenerateCode', formData, function (response) {
			$btn.block(false);

			if (!response.isSuccess) {
				toastr.error(response.message || 'خطا در تولید کد', 'خطا');
				return;
			}

			state.generatedCode = response.data;
			displayCodePreview(response.data);
			$$('#codePreviewModal').modal('show');
		});
	}

	/**
	 * Display code preview
	 */
	function displayCodePreview(data) {
		// Entity
		$$('#entityCode').text(data.code.entityClass);
		$$('#entityPath').text(data.paths.Entity);

		// Controller
		$$('#controllerCode').text(data.code.controllerClass);
		$$('#controllerPath').text(data.paths.Controller);

		// Edit View
		$$('#editViewCode').text(data.code.editView);
		$$('#editViewPath').text(data.paths.EditView);

		// List View
		$$('#listViewCode').text(data.code.listView);
		$$('#listViewPath').text(data.paths.ListView);

		// Partial Views
		if (data.code.partialViews && Object.keys(data.code.partialViews).length > 0) {
			$$('#partialViewsTabNav').show();
			const $container = $$('#partialViewsContent');
			$container.empty();

			Object.keys(data.code.partialViews).forEach(function (key) {
				const pathKey = 'Partial_' + key.replace('_', '').replace('Partial', '');
				const codeId = 'partialViewCode_' + key.replace(/[^a-zA-Z0-9]/g, '_');
				$container.append(`
					<div class="mb-4" dir="rtl">
						<div class="d-flex justify-content-between align-items-center mb-2">
							<h5 class="mb-0">${key}</h5>
							<button type="button" class="btn btn-sm btn-light-primary btn-copy-code" data-code-id="${codeId}" title="کپی کد">
								<i class="ki-duotone ki-copy fs-3">
									<span class="path1"></span>
									<span class="path2"></span>
									<span class="path3"></span>
									<span class="path4"></span>
								</i>
								کپی
							</button>
						</div>
						<div class="mb-2">
							<label class="form-label fw-bold">مسیر فایل:</label>
							<code class="d-block bg-light p-2 rounded">${data.paths[pathKey] || 'N/A'}</code>
						</div>
						<pre dir="ltr"><code id="${codeId}" class="language-cshtml">${escapeHtml(data.code.partialViews[key])}</code></pre>
					</div>
				`);
			});
		} else {
			$$('#partialViewsTabNav').hide();
		}

		// Enums
		if (data.code.enumClasses && Object.keys(data.code.enumClasses).length > 0) {
			$$('#enumsTabNav').show();
			const $container = $$('#enumsContent');
			$container.empty();

			Object.keys(data.code.enumClasses).forEach(function (key) {
				const pathKey = 'Enum_' + key;
				const codeId = 'enumCode_' + key.replace(/[^a-zA-Z0-9]/g, '_');
				$container.append(`
					<div class="mb-4" dir="rtl">
						<div class="d-flex justify-content-between align-items-center mb-2">
							<h5 class="mb-0">${key}</h5>
							<button type="button" class="btn btn-sm btn-light-primary btn-copy-code" data-code-id="${codeId}" title="کپی کد">
								<i class="ki-duotone ki-copy fs-3">
									<span class="path1"></span>
									<span class="path2"></span>
									<span class="path3"></span>
									<span class="path4"></span>
								</i>
								کپی
							</button>
						</div>
						<div class="mb-2">
							<label class="form-label fw-bold">مسیر فایل:</label>
							<code class="d-block bg-light p-2 rounded">${data.paths[pathKey] || 'N/A'}</code>
						</div>
						<pre dir="ltr"><code id="${codeId}" class="language-csharp">${escapeHtml(data.code.enumClasses[key])}</code></pre>
					</div>
				`);
			});
		} else {
			$$('#enumsTabNav').hide();
		}

		// Highlight code if Prism is available
		if (typeof Prism !== 'undefined') {
			setTimeout(function () {
				Prism.highlightAll();
			}, 100);
		}

		// Show warning if files exist
		if (data.existingFiles && data.existingFiles.length > 0) {
			toastr.warning('برخی فایل‌ها از قبل وجود دارند و بازنویسی خواهند شد: ' + data.existingFiles.join(', '), 'هشدار', {
				timeOut: 10000
			});
		}
	}

	/**
	 * Copy code to clipboard
	 */
	function copyCodeToClipboard(codeId, $button) {
		const $codeElement = $$('#' + codeId);
		
		if (!$codeElement.length) {
			toastr.error('کد یافت نشد', 'خطا');
			return;
		}

		// Get text content (remove syntax highlighting spans if any)
		let codeText = $codeElement.text();
		
		// If text is empty, try to get from innerHTML and strip HTML tags
		if (!codeText || codeText.trim() === '') {
			codeText = $codeElement.html();
			// Remove HTML tags but preserve text
			const tempDiv = document.createElement('div');
			tempDiv.innerHTML = codeText;
			codeText = tempDiv.textContent || tempDiv.innerText || '';
		}

		if (!codeText || codeText.trim() === '') {
			toastr.warning('کد خالی است', 'هشدار');
			return;
		}

		// Use Clipboard API
		if (navigator.clipboard && navigator.clipboard.writeText) {
			navigator.clipboard.writeText(codeText).then(function () {
				// Show success feedback
				const originalHtml = $button.html();
				$button.html('<i class="ki-duotone ki-check fs-3"><span class="path1"></span><span class="path2"></span></i> کپی شد!');
				$button.removeClass('btn-light-primary').addClass('btn-success');
				
				setTimeout(function () {
					$button.html(originalHtml);
					$button.removeClass('btn-success').addClass('btn-light-primary');
				}, 2000);
				
				toastr.success('کد با موفقیت کپی شد', 'موفق');
			}).catch(function (err) {
				console.error('Failed to copy:', err);
				// Fallback to old method
				fallbackCopyTextToClipboard(codeText, $button);
			});
		} else {
			// Fallback for older browsers
			fallbackCopyTextToClipboard(codeText, $button);
		}
	}

	/**
	 * Fallback copy method for older browsers
	 */
	function fallbackCopyTextToClipboard(text, $button) {
		const textArea = document.createElement('textarea');
		textArea.value = text;
		textArea.style.position = 'fixed';
		textArea.style.left = '-999999px';
		textArea.style.top = '-999999px';
		document.body.appendChild(textArea);
		textArea.focus();
		textArea.select();

		try {
			const successful = document.execCommand('copy');
			if (successful) {
				const originalHtml = $button.html();
				$button.html('<i class="ki-duotone ki-check fs-3"><span class="path1"></span><span class="path2"></span></i> کپی شد!');
				$button.removeClass('btn-light-primary').addClass('btn-success');
				
				setTimeout(function () {
					$button.html(originalHtml);
					$button.removeClass('btn-success').addClass('btn-light-primary');
				}, 2000);
				
				toastr.success('کد با موفقیت کپی شد', 'موفق');
			} else {
				toastr.error('کپی انجام نشد. لطفا دستی کپی کنید.', 'خطا');
			}
		} catch (err) {
			console.error('Fallback copy failed:', err);
			toastr.error('کپی انجام نشد. لطفا دستی کپی کنید.', 'خطا');
		} finally {
			document.body.removeChild(textArea);
		}
	}

	/**
	 * Save files to disk
	 */
	function saveFiles() {
		const formDefinitionId = $$('#formBuilderCard input[data-bind="id"]').val();

		if (!formDefinitionId || formDefinitionId === '0') {
			toastr.error('لطفا ابتدا فرم را ذخیره کنید', 'خطا');
			return;
		}

		if (!confirm('آیا از ذخیره فایل‌ها در دیسک اطمینان دارید؟ فایل‌های موجود بازنویسی خواهند شد.')) {
			return;
		}

		// Get the button that triggered this (from event context)
		const $btn = $$('#btnSaveFiles, #btnSaveFilesFromPreview').filter(':visible').first().block();

		post('/Panel/FormBuilder/SaveFiles', {
			formDefinitionId: parseInt(formDefinitionId),
			overwriteExisting: true
		}, function (response) {
			$btn.block(false);

			if (!response.isSuccess) {
				toastr.error(response.message || 'خطا در ذخیره فایل‌ها', 'خطا');
				return;
			}

			toastr.success('فایل‌ها با موفقیت ذخیره شدند', 'موفق');
			$$('#codePreviewModal').modal('hide');
		});
	}

	function collectFormModel() {
		collectPropertiesData();
		var model = $$('.card').dataBind();
		model.Properties = $$('#formBuilderCard').data('properties');
		var sectionsData = $$('#formBuilderCard').data('sections');
		if (!sectionsData && typeof getSectionsData === 'function') {
			sectionsData = getSectionsData();
		}
		model.Sections = sectionsData || [];
		return model;
	}

	function loadPublishStatus(formDefinitionId) {
		if (!formDefinitionId || formDefinitionId === '0') {
			updatePublishStatusUI(null);
			return;
		}

		get(`/Panel/FormBuilder/PublishStatus?id=${formDefinitionId}`, function (response) {
			if (response.isSuccess && response.data) {
				updatePublishStatusUI(response.data);
			}
		});
	}

	function updatePublishStatusUI(status) {
		const $badge = $$('#publishStatusBadge');
		const $url = $$('#publishStatusUrl');
		const $error = $$('#publishStatusError');
		const $btnUnpublish = $$('#btnUnpublish');

		if (!status || !status.isPublished) {
			$badge.removeClass('badge-light-success badge-light-danger').addClass('badge-light-secondary');
			$badge.text(status && status.lastPublishError ? 'وضعیت: خطا در انتشار' : 'وضعیت: پیش‌نویس');
			$url.addClass('d-none').empty();
			$btnUnpublish.addClass('d-none');

			if (status && status.lastPublishError) {
				$error.removeClass('d-none').text(status.lastPublishError);
				$badge.removeClass('badge-light-secondary').addClass('badge-light-danger');
			} else {
				$error.addClass('d-none').empty();
			}
			return;
		}

		$badge.removeClass('badge-light-secondary badge-light-danger').addClass('badge-light-success');
		$badge.text('وضعیت: منتشر شده');
		$error.addClass('d-none').empty();
		$btnUnpublish.removeClass('d-none');

		if (status.listUrl) {
			$url.removeClass('d-none').html(`<a href="${status.listUrl}" target="_blank">باز کردن فرم منتشرشده</a>`);
		} else {
			$url.addClass('d-none').empty();
		}
	}

	function publishForm() {
		if (validateError($$('.card'))) {
			return;
		}

		if (!confirm('فرم ذخیره و به صورت آنلاین منتشر شود؟')) {
			return;
		}

		const $btn = $$('#btnPublish').block();
		const model = collectFormModel();

		post('/Panel/FormBuilder/Save', model, function (saveResponse) {
			if (!saveResponse.isSuccess) {
				$btn.block(false);
				toastr.error(saveResponse.message || 'خطا در ذخیره فرم', 'خطا');
				return;
			}

			$$('.card').dataBind(saveResponse.data);
			const formDefinitionId = saveResponse.data.id;

			post('/Panel/FormBuilder/Publish', { formDefinitionId: formDefinitionId }, function (publishResponse) {
				$btn.block(false);

				if (!publishResponse.isSuccess) {
					const errors = publishResponse.data && publishResponse.data.errors
						? publishResponse.data.errors.join('\n')
						: (publishResponse.message || 'خطا در انتشار فرم');
					toastr.error(errors, 'خطا');
					loadPublishStatus(formDefinitionId);
					return;
				}

				toastr.success(publishResponse.data.message || 'فرم با موفقیت منتشر شد', 'موفق');
				loadPublishStatus(formDefinitionId);
			});
		});
	}

	function unpublishForm() {
		const formDefinitionId = $$('#formBuilderCard input[data-bind="id"]').val();
		if (!formDefinitionId || formDefinitionId === '0') {
			toastr.error('لطفا ابتدا فرم را ذخیره کنید', 'خطا');
			return;
		}

		if (!confirm('آیا از لغو انتشار این فرم اطمینان دارید؟')) {
			return;
		}

		const $btn = $$('#btnUnpublish').block();
		post('/Panel/FormBuilder/Unpublish', { formDefinitionId: parseInt(formDefinitionId) }, function (response) {
			$btn.block(false);

			if (!response.isSuccess) {
				toastr.error(response.message || 'خطا در لغو انتشار', 'خطا');
				return;
			}

			toastr.success(response.data.message || 'انتشار فرم لغو شد', 'موفق');
			loadPublishStatus(formDefinitionId);
		});
	}

	/**
	 * Load entity fields for display template builder (Tree View)
	 */
	function loadEntityFieldsForTemplate(entityFullName) {
		const $treeContainer = $$('#entityFieldsTree');
		$treeContainer.html('<div class="text-muted text-center py-3"><i class="ki-duotone ki-loader fs-2"><span class="path1"></span><span class="path2"></span></i><div class="mt-2">در حال بارگذاری...</div></div>');

		// Find entity in available entities
		const entity = state.availableEntities.find(e => e.fullName === entityFullName);
		if (!entity) {
			$treeContainer.html('<div class="text-danger text-center py-3">موجودیت یافت نشد</div>');
			return;
		}

		// Get entity metadata from API with nested properties (maxDepth=4)
		get(`/Panel/FormBuilder/GetEntityProperties?entityFullName=${encodeURIComponent(entityFullName)}&maxDepth=4`, function (response) {
			if (!response.isSuccess || !response.data || !response.data.properties) {
				$treeContainer.html('<div class="text-danger text-center py-3">خطا در بارگذاری فیلدها</div>');
				return;
			}

			var properties = response.data.properties;
			if (properties.length === 0) {
				$treeContainer.html('<div class="text-muted text-center py-3">فیلدی یافت نشد</div>');
				return;
			}

			// Build tree structure
			var tree = buildFieldsTree(properties);
			$treeContainer.empty().append(renderTree(tree));
		});
	}

	/**
	 * Load entity fields for nested property display template builder (Tree View)
	 */
	function loadNestedEntityFieldsForTemplate(entityFullName) {
		const $treeContainer = $$('#nestedEntityFieldsTree');
		$treeContainer.html('<div class="text-muted text-center py-3"><i class="ki-duotone ki-loader fs-2"><span class="path1"></span><span class="path2"></span></i><div class="mt-2">در حال بارگذاری...</div></div>');

		// Find entity in available entities
		const entity = state.availableEntities.find(e => e.fullName === entityFullName);
		if (!entity) {
			$treeContainer.html('<div class="text-danger text-center py-3">موجودیت یافت نشد</div>');
			return;
		}

		// Get entity metadata from API with nested properties (maxDepth=4)
		get(`/Panel/FormBuilder/GetEntityProperties?entityFullName=${encodeURIComponent(entityFullName)}&maxDepth=4`, function (response) {
			if (!response.isSuccess || !response.data || !response.data.properties) {
				$treeContainer.html('<div class="text-danger text-center py-3">خطا در بارگذاری فیلدها</div>');
				return;
			}

			var properties = response.data.properties;
			if (properties.length === 0) {
				$treeContainer.html('<div class="text-muted text-center py-3">فیلدی یافت نشد</div>');
				return;
			}

			// Build tree structure
			var tree = buildFieldsTree(properties);
			$treeContainer.empty().append(renderTree(tree));
		});
	}

	/**
	 * Build tree structure from flat properties list
	 */
	function buildFieldsTree(properties) {
		var tree = {
			direct: [],
			nested: {}
		};

		// Helper to get display name for entity at specific level
		function getEntityDisplayName(parentDisplayName, level) {
			if (!parentDisplayName) return null;
			var parts = parentDisplayName.split(' > ');
			return parts[level] || null;
		}

		properties.forEach(function (prop) {
			if (!prop.isNested) {
				// Direct property
				tree.direct.push(prop);
			} else {
				// Nested property - parse path
				var pathParts = prop.fullPath.split('.');
				if (pathParts.length >= 2) {
					var current = tree.nested;
					var displayParts = prop.parentEntityDisplayName ? prop.parentEntityDisplayName.split(' > ') : [];
					
					// Build nested structure recursively
					for (var i = 0; i < pathParts.length - 1; i++) {
						var part = pathParts[i];
						if (!current[part]) {
							current[part] = {
								name: part,
								displayName: displayParts[i] || part,
								children: {},
								fields: []
							};
						}
						
						if (i === pathParts.length - 2) {
							// Last entity in path - add field
							current[part].fields.push({
								name: pathParts[pathParts.length - 1],
								displayName: prop.displayName,
								fullPath: prop.fullPath,
								dataType: prop.dataType,
								prop: prop
							});
						} else {
							// Navigate deeper
							current = current[part].children;
						}
					}
				}
			}
		});

		return tree;
	}

	/**
	 * Render tree structure as HTML
	 */
	function renderTree(tree, level = 0) {
		var $container = $('<div class="tree-container"></div>');
		
		// Direct properties
		if (tree.direct && tree.direct.length > 0) {
			var $directGroup = $('<div class="tree-group mb-3"></div>');
		 
			tree.direct.forEach(function (prop) {
				var $item = $('<div class="tree-item tree-field" data-field-path="' + (prop.fullPath || prop.name) + '" data-field-prop=\'' + JSON.stringify(prop).replace(/'/g, "&#39;") + '\'>' +
					'<i class="ki-duotone ki-file fs-4 me-2 text-primary">' +
					'<span class="path1"></span><span class="path2"></span><span class="path3"></span><span class="path4"></span>' +
					'</i>' +
					'<span class="field-name">' + (prop.displayName || prop.name) + '</span>' +
					'<small class="text-muted ms-2">(' + prop.name + ')</small>' +
					'</div>');
				$directGroup.append($item);
			});
			$container.append($directGroup);
		}

		// Nested properties
		if (tree.nested && Object.keys(tree.nested).length > 0) {
			 
			Object.keys(tree.nested).forEach(function (key) {
				var node = tree.nested[key];
				var $node = renderTreeNode(node, level);
				$container.append($node);
			});
		}

		return $container;
	}

	/**
	 * Render a tree node (entity with children)
	 */
	function renderTreeNode(node, level = 0) {
		var hasChildren = (node.children && Object.keys(node.children).length > 0) || (node.fields && node.fields.length > 0);
		var nodeId = 'tree-node-' + Math.random().toString(36).substr(2, 9);
		
		var $node = $('<div class="tree-node mb-2"></div>');
		
		// Node header (expandable if has children)
		var $header = $('<div class="tree-node-header d-flex align-items-center py-1 px-2 rounded cursor-pointer" style="transition: background-color 0.2s;">' +
			(hasChildren ? '<i class="ki-duotone ki-arrow-down tree-toggle me-2" data-target="' + nodeId + '">' +
			'<span class="path1"></span></i>' : 
			'<span class="me-2" style="width: 20px; display: inline-block;"></span>') +
			'<i class="ki-duotone ki-folder fs-4 me-2 text-warning">' +
			'<span class="path1"></span><span class="path2"></span></i>' +
			'<span class="fw-bold">' + (node.displayName || node.name) + '</span>' +
			'</div>');
		
		// Hover effect handled by CSS
		
		$node.append($header);

		// Node content (children and fields)
		if (hasChildren) {
			var $content = $('<div class="tree-node-content" id="' + nodeId + '" style="display: none;"></div>');
			
			// Fields in this node
			if (node.fields && node.fields.length > 0) {
				node.fields.forEach(function (field) {
					var $field = $('<div class="tree-item tree-field py-1 px-2 rounded" data-field-path="' + field.fullPath + '" data-field-prop=\'' + JSON.stringify(field.prop).replace(/'/g, "&#39;") + '\'>' +
						'<i class="ki-duotone ki-file fs-4 me-2 text-primary">' +
						'<span class="path1"></span><span class="path2"></span><span class="path3"></span><span class="path4"></span>' +
						'</i>' +
						'<span class="field-name">' + field.displayName + '</span>' +
						'<small class="text-muted ms-2">(' + field.name + ')</small>' +
						'</div>');
					
					$content.append($field);
				});
			}
			
			// Child nodes
			if (node.children && Object.keys(node.children).length > 0) {
				Object.keys(node.children).forEach(function (childKey) {
					var $childNode = renderTreeNode(node.children[childKey], level + 1);
					$content.append($childNode);
				});
			}
			
			$node.append($content);
			
			// Toggle on header click
			$header.on('click', function(e) {
				if (!hasChildren) return;
				e.stopPropagation();
				var $toggle = $(this).find('.tree-toggle');
				var $target = $('#' + $toggle.data('target'));
				if ($target.is(':visible')) {
					$target.slideUp(200);
					$toggle.removeClass('ki-arrow-down').addClass('ki-arrow-up');
				} else {
					$target.slideDown(200);
					$toggle.removeClass('ki-arrow-up').addClass('ki-arrow-down');
				}
			});
		}

		return $node;
	}

	/**
	 * Debounce helper function
	 */
	function debounce(func, wait) {
		var timeout;
		return function() {
			var context = this, args = arguments;
			clearTimeout(timeout);
			timeout = setTimeout(function() {
				func.apply(context, args);
			}, wait);
		};
	}

	/**
	 * Translate Persian name to English property name using AI
	 */
	function translatePropertyName(persianName, callback) {
		if (!persianName) {
			if (callback) callback('');
			return;
		}

		post('/Panel/FormBuilder/TranslatePropertyName', { text: persianName }, function (response) {
			if (response.isSuccess && response.data) {
				const englishName = response.data.trim();
				// Clean up the result (remove quotes, extra spaces, etc.)
				const cleanName = englishName.replace(/['"]/g, '').trim();
				
				if (cleanName) {
					if (callback) callback(cleanName);
				} else {
					if (callback) callback('Property');
				}
			} else {
				if (callback) callback('Property');
			}
		});
	}

	/**
	 * Translate English property name to Persian display name using AI
	 */
	function translateToFarsi(englishName, callback) {
		if (!englishName) {
			if (callback) callback('');
			return;
		}

		post('/Panel/FormBuilder/TranslateToFarsi', { text: englishName }, function (response) {
			if (response.isSuccess && response.data) {
				const persianName = response.data.trim();
				// Clean up the result (remove quotes, extra spaces, etc.)
				const cleanName = persianName.replace(/['"]/g, '').trim();
				
				if (cleanName) {
					if (callback) callback(cleanName);
				} else {
					if (callback) callback(englishName);
				}
			} else {
				if (callback) callback(englishName);
			}
		});
	}

	/**
	 * Escape HTML
	 */
	function escapeHtml(text) {
		const map = {
			'&': '&amp;',
			'<': '&lt;',
			'>': '&gt;',
			'"': '&quot;',
			"'": '&#039;'
		};
		return text.replace(/[&<>"']/g, function (m) { return map[m]; });
	}

	// Public API
	return {
		init: init,
		collectPropertiesData: collectPropertiesData,
		addProperty: addProperty,
		previewCode: previewCode,
		saveFiles: saveFiles,
		publishForm: publishForm,
		unpublishForm: unpublishForm,
		loadPublishStatus: loadPublishStatus,
		getSectionsData: getSectionsData,
		loadFormDefinitionById: loadFormDefinitionById,
		populateFormFromDefinition: populateFormFromDefinition,
		translateToFarsi: translateToFarsi,
		translatePropertyName: translatePropertyName
	};
})();
