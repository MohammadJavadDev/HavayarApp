/**
 * Database Importer Module for Form Builder
 * مدیریت فرآیند Import فرم از دیتابیس
 */

var DatabaseImporter = (function () {
	'use strict';

	// State Management
	var state = {
		currentStep: 1,
		totalSteps: 4,
		connectionString: null,
		selectedTable: null,
		columns: [],
		selectedColumns: [],
		stepper: null,
		modal: null
	};

	// DOM Elements
	var elements = {
		modal: null,
		stepper: null,
		form: null,
		btnNext: null,
		btnPrevious: null,
		btnConfirm: null,
		// Step 1
		connectionSourceRadios: null,
		existingConnectionSelect: null,
		customConnectionInput: null,
		btnTestConnection: null,
		connectionStatus: null,
		// Step 2
		tablesSelect: null,
		tableInfoSection: null,
		// Step 3
		columnsTable: null,
		selectAllCheckbox: null,
		// Step 4
		importModeRadios: null,
		sectionNameInput: null,
		moduleNameInput: null,
		importSummary: null
	};

	/**
	 * مقداردهی اولیه
	 */
	function init() {
		console.log('DatabaseImporter: Initializing...');
		
		// Cache DOM elements
		cacheElements();
		
		// Initialize Stepper
		initStepper();
		
		// Bind Events
		bindEvents();
		
		console.log('DatabaseImporter: Initialized successfully');
	}

	/**
	 * ذخیره المان‌های DOM
	 */
	function cacheElements() {
		elements.modal = document.getElementById('loadFromDatabaseModal');
		elements.stepper = document.getElementById('db_import_stepper');
		elements.form = document.getElementById('db_import_form');
		elements.btnNext = document.getElementById('btnNextStep');
		elements.btnPrevious = document.getElementById('btnPreviousStep');
		elements.btnConfirm = document.getElementById('btnConfirmImport');
		
		// Step 1
		elements.connectionSourceRadios = document.getElementsByName('connectionSource');
		elements.existingConnectionSelect = document.getElementById('existingConnectionStrings');
		elements.customConnectionInput = document.getElementById('customConnectionString');
		elements.btnTestConnection = document.getElementById('btnTestConnection');
		elements.connectionStatus = document.getElementById('connectionStatus');
		
		// Step 2
		elements.tablesSelect = document.getElementById('databaseTables');
		elements.tableInfoSection = document.getElementById('tableInfoSection');
		
		// Step 3
		elements.columnsTable = document.getElementById('columnsPreviewTable');
		elements.selectAllCheckbox = document.getElementById('selectAllColumns');
		
		// Step 4
		elements.importModeRadios = document.getElementsByName('importMode');
		elements.sectionNameInput = document.getElementById('sectionNameInput');
		elements.moduleNameInput = document.getElementById('moduleNameInput');
		elements.importSummary = document.getElementById('importSummary');
	}

	/**
	 * مقداردهی Stepper
	 */
	function initStepper() {

		 	
		if (!elements.stepper) return;
		
		state.stepper = new KTStepper(elements.modal);
		
		// Handle stepper navigation
		state.stepper.on('kt.stepper.changed', function (stepper) {
			state.currentStep = stepper.getCurrentStepIndex();
			updateNavigationButtons();
		});
	}

	/**
	 * اتصال رویدادها
	 */
	function bindEvents() {
		// Open Modal Button
		var btnLoadFromDatabase = document.getElementById('btnLoadFromDatabase');
		if (btnLoadFromDatabase) {
			btnLoadFromDatabase.addEventListener('click', openModal);
		}
		
		// Navigation Buttons
		if (elements.btnNext) {
			elements.btnNext.addEventListener('click', handleNextStep);
		}
		
		if (elements.btnPrevious) {
			elements.btnPrevious.addEventListener('click', handlePreviousStep);
		}
		
		if (elements.btnConfirm) {
			elements.btnConfirm.addEventListener('click', handleConfirmImport);
		}
		
		// Step 1: Connection Source
		Array.from(elements.connectionSourceRadios).forEach(function (radio) {
			radio.addEventListener('change', handleConnectionSourceChange);
		});
		
		if (elements.btnTestConnection) {
			elements.btnTestConnection.addEventListener('click', testConnection);
		}
		
		// Step 2: Table Selection
		if (elements.tablesSelect) {
			$(elements.tablesSelect).on('change', handleTableSelection);
		}
		
		// Step 3: Select All Columns
		if (elements.selectAllCheckbox) {
			elements.selectAllCheckbox.addEventListener('change', handleSelectAllColumns);
		}
		
		// Modal Reset on Close
		if (elements.modal) {
			$(elements.modal).on('hidden.bs.modal', resetModal);
		}
	}

	/**
	 * باز کردن Modal و بارگذاری اولیه
	 */
	function openModal() {
		console.log('DatabaseImporter: Opening modal...');
		
		// Reset state
		state.currentStep = 1;
		state.connectionString = null;
		state.selectedTable = null;
		state.columns = [];
		state.selectedColumns = [];
		
		// Show modal
		var modal = new bootstrap.Modal(elements.modal);
		modal.show();
		state.modal = modal;
		
		// Load connection strings
		loadConnectionStrings();
		
		// Reset stepper to first step
		if (state.stepper) {
			state.stepper.goTo(1);
		}
		
		updateNavigationButtons();
	}

	/**
	 * بارگذاری لیست Connection String ها
	 */
	function loadConnectionStrings() {
		console.log('DatabaseImporter: Loading connection strings...');
		
		var $select = $(elements.existingConnectionSelect);
		$select.html('<option value="">در حال بارگذاری...</option>');
		
		$.ajax({
			url: '/Panel/FormBuilder/GetConnectionStrings',
			method: 'GET',
			success: function (r) {

				console.log('Connection strings loaded:', r);
				var response = r.data;
				
				$select.empty();
				$select.append('<option value="">انتخاب کنید...</option>');
				
				if (response && response.length > 0) {
					response.forEach(function (cs) {
						$select.append(new Option(cs.name, cs.value));
					});
				} else {
					$select.append('<option value="" disabled>هیچ Connection String موجود نیست</option>');
				}
				
				// Initialize Select2
				$select.select2({
					placeholder: 'انتخاب کنید...',
					dropdownParent: $(elements.modal)
				});
			},
			error: function (xhr, status, error) {
				console.error('Error loading connection strings:', error);
				toastr.error('خطا در بارگذاری Connection String ها');
				$select.html('<option value="">خطا در بارگذاری</option>');
			}
		});
	}

	/**
	 * تغییر منبع Connection String
	 */
	function handleConnectionSourceChange(e) {
		var source = e.target.value;
		
		if (source === 'existing') {
			elements.existingConnectionSelect.disabled = false;
			elements.customConnectionInput.disabled = true;
			elements.customConnectionInput.value = '';
		} else {
			elements.existingConnectionSelect.disabled = true;
			elements.customConnectionInput.disabled = false;
		}
	}

	/**
	 * تست اتصال به دیتابیس
	 */
	function testConnection() {
		console.log('DatabaseImporter: Testing connection...');
		
		var connectionString = getSelectedConnectionString();
		
		if (!connectionString) {
			toastr.warning('لطفاً Connection String را انتخاب یا وارد کنید');
			return;
		}
		
		// Show loading
		var $btn = $(elements.btnTestConnection);
		$btn.attr('disabled', true);
		$btn.html('<span class="spinner-border spinner-border-sm me-2"></span>در حال تست...');
		
		$.ajax({
			url: '/Panel/FormBuilder/TestConnection',
			method: 'POST',
			contentType: 'application/json',
			data: JSON.stringify({ connectionString: connectionString }),
			success: function (response) {
				console.log('Connection test result:', response);
				
				if (response.isSuccess) {
					state.connectionString = connectionString;
					showConnectionStatus(true, response.message, response);
					toastr.success('اتصال با موفقیت برقرار شد');
				} else {
					showConnectionStatus(false, response.message);
					toastr.error('اتصال ناموفق بود');
				}
			},
			error: function (xhr, status, error) {
				console.error('Connection test error:', error);
				var message = xhr.responseJSON?.message || 'خطا در تست اتصال';
				showConnectionStatus(false, message);
				toastr.error(message);
			},
			complete: function () {
				$btn.attr('disabled', false);
				$btn.html('<i class="ki-duotone ki-check-circle fs-2"><span class="path1"></span><span class="path2"></span></i> تست اتصال');
			}
		});
	}

	/**
	 * نمایش وضعیت اتصال
	 */
	function showConnectionStatus(success, message, details) {
		var $status = $(elements.connectionStatus);
		var $alert = $status.find('.alert');
		
		$alert.removeClass('alert-success alert-danger');
		$alert.addClass(success ? 'alert-success' : 'alert-danger');
		
		var html = '<strong>' + (success ? 'موفق' : 'ناموفق') + ':</strong> ' + message;
		
		if (success && details) {
			html += '<br><small>دیتابیس: ' + (details.databaseName || 'N/A') + '</small>';
			html += '<br><small>نسخه: ' + (details.serverVersion || 'N/A') + '</small>';
		}
		
		$alert.html(html);
		$status.show();
	}

	/**
	 * دریافت Connection String انتخاب شده
	 */
	function getSelectedConnectionString() {
		var source = document.querySelector('input[name="connectionSource"]:checked').value;
		
		if (source === 'existing') {
			return elements.existingConnectionSelect.value;
		} else {
			return elements.customConnectionInput.value.trim();
		}
	}

	/**
	 * مدیریت دکمه بعدی
	 */
	function handleNextStep() {
		console.log('DatabaseImporter: Next step from', state.currentStep);
		
		// Validate current step
		if (!validateCurrentStep()) {
			return;
		}
		
		// Perform step-specific actions
		if (state.currentStep === 1) {
			// Load tables after connection test
			loadDatabaseTables();
		} else if (state.currentStep === 2) {
			// Load columns after table selection
			loadTableColumns();
		} else if (state.currentStep === 3) {
			// Prepare final summary
			prepareFinalSummary();
		}
		
		// Move to next step
		if (state.stepper) {
			state.stepper.goNext();
		}
	}

	/**
	 * مدیریت دکمه قبلی
	 */
	function handlePreviousStep() {
		console.log('DatabaseImporter: Previous step from', state.currentStep);
		
		if (state.stepper) {
			state.stepper.goPrevious();
		}
	}

	/**
	 * اعتبارسنجی مرحله فعلی
	 */
	function validateCurrentStep() {
		if (state.currentStep === 1) {
			if (!state.connectionString) {
				toastr.warning('لطفاً ابتدا اتصال به دیتابیس را تست کنید');
				return false;
			}
		} else if (state.currentStep === 2) {
			if (!state.selectedTable) {
				toastr.warning('لطفاً یک جدول انتخاب کنید');
				return false;
			}
		} else if (state.currentStep === 3) {
			state.selectedColumns = getSelectedColumns();
			if (state.selectedColumns.length === 0) {
				toastr.warning('لطفاً حداقل یک ستون انتخاب کنید');
				return false;
			}
		} else if (state.currentStep === 4) {
			if (!elements.moduleNameInput.value.trim()) {
				toastr.warning('لطفاً نام ماژول را وارد کنید');
				return false;
			}
		}
		
		return true;
	}

	/**
	 * بارگذاری جداول دیتابیس
	 */
	function loadDatabaseTables() {
		console.log('DatabaseImporter: Loading tables...');
		
		var $select = $(elements.tablesSelect);
		$select.html('<option value="">در حال بارگذاری...</option>');
		
		$.ajax({
			url: '/Panel/FormBuilder/GetDatabaseTables',
			method: 'POST',
			contentType: 'application/json',
			data: JSON.stringify({ connectionString: state.connectionString }),
			success: function (r) {
				console.log('Tables loaded:', r);
				var response = r.data
				
				$select.empty();
				$select.append('<option value="">انتخاب کنید...</option>');
				
				if (response && response.length > 0) {
					response.forEach(function (table) {
						var optionText = table.schema + '.' + table.tableName + ' (' + table.rowCount + ' رکورد)';
						var optionValue = JSON.stringify({ schema: table.schema, tableName: table.tableName, rowCount: table.rowCount });
						$select.append(new Option(optionText, optionValue));
					});
					
					// Initialize Select2
					$select.select2({
						placeholder: 'انتخاب کنید...',
						dropdownParent: $(elements.modal)
					});
				} else {
					$select.append('<option value="" disabled>هیچ جدولی یافت نشد</option>');
					toastr.info('هیچ جدولی در دیتابیس یافت نشد');
				}
			},
			error: function (xhr, status, error) {
				console.error('Error loading tables:', error);
				var message = xhr.responseJSON?.message || 'خطا در بارگذاری جداول';
				toastr.error(message);
				$select.html('<option value="">خطا در بارگذاری</option>');
			}
		});
	}

	/**
	 * مدیریت انتخاب جدول
	 */
	function handleTableSelection() {
		var selectedValue = elements.tablesSelect.value;
		
		if (!selectedValue) {
			elements.tableInfoSection.style.display = 'none';
			state.selectedTable = null;
			return;
		}
		
		try {
			state.selectedTable = JSON.parse(selectedValue);
			console.log('Table selected:', state.selectedTable);
			
			// Show table info
			document.getElementById('selectedSchema').textContent = state.selectedTable.schema;
			document.getElementById('selectedTableName').textContent = state.selectedTable.tableName;
			document.getElementById('selectedRowCount').textContent = state.selectedTable.rowCount;
			elements.tableInfoSection.style.display = 'block';
			
			// Auto-fill module name if empty
			if (!elements.moduleNameInput.value) {
				elements.moduleNameInput.value = state.selectedTable.tableName;
			}
		} catch (e) {
			console.error('Error parsing table selection:', e);
		}
	}

	/**
	 * بارگذاری ستون‌های جدول
	 */
	function loadTableColumns() {
		console.log('DatabaseImporter: Loading columns...');
		
		var $tbody = $(elements.columnsTable).find('tbody');
		$tbody.html('<tr><td colspan="8" class="text-center"><span class="spinner-border spinner-border-sm me-2"></span>در حال بارگذاری...</td></tr>');
		
		$.ajax({
			url: '/Panel/FormBuilder/GetTableColumns',
			method: 'POST',
			contentType: 'application/json',
			data: JSON.stringify({
				connectionString: state.connectionString,
				schema: state.selectedTable.schema,
				tableName: state.selectedTable.tableName
			}),
			success: function (r) {
				console.log('Columns loaded:', r);

				var response = r.data;

				if (response && response.columns && response.columns.length > 0) {
					state.columns = response.columns;
					renderColumnsPreview(response.columns);
				} else {
					$tbody.html('<tr><td colspan="8" class="text-center text-muted">هیچ ستونی یافت نشد</td></tr>');
					toastr.warning('جدول انتخاب شده هیچ ستونی ندارد');
				}
			},
			error: function (xhr, status, error) {
				console.error('Error loading columns:', error);
				var message = xhr.responseJSON?.message || 'خطا در بارگذاری ستون‌ها';
				toastr.error(message);
				$tbody.html('<tr><td colspan="8" class="text-center text-danger">خطا در بارگذاری</td></tr>');
			}
		});
	}

	/**
	 * نمایش پیش‌نمایش ستون‌ها
	 */
	function renderColumnsPreview(columns) {
		var $tbody = $(elements.columnsTable).find('tbody');
		$tbody.empty();
		
		columns.forEach(function (column, index) {
			var row = createColumnRow(column, index);
			$tbody.append(row);
		});
		
		// Bind events for inline editing
		bindColumnRowEvents();
	}

	/**
	 * ایجاد ردیف ستون
	 */
	function createColumnRow(column, index) {
		var badges = '';
		if (column.isPrimaryKey) {
			badges += '<span class="badge badge-light-primary me-1">PK</span>';
		}
		if (column.isForeignKey) {
			badges += '<span class="badge badge-light-info me-1">FK</span>';
		}
		if (!column.isNullable) {
			badges += '<span class="badge badge-light-warning me-1">NOT NULL</span>';
		}
		
		var row = $('<tr data-column-index="' + index + '">');
		
		row.append('<td><div class="form-check form-check-sm form-check-custom form-check-solid"><input class="form-check-input column-checkbox" type="checkbox" checked /></div></td>');
		row.append('<td><strong>' + column.columnName + '</strong></td>');
		row.append('<td><code>' + column.dataType + (column.maxLength ? '(' + column.maxLength + ')' : '') + '</code></td>');
		row.append('<td><span class="badge badge-light-success">' + column.mappedSystemType + '</span></td>');
		row.append('<td><input type="text" class="form-control form-control-sm column-display-name" value="' + column.suggestedDisplayName + '" /></td>');
		row.append('<td><div class="form-check form-check-sm form-check-custom form-check-solid"><input class="form-check-input column-required" type="checkbox" ' + (!column.isNullable && !column.isPrimaryKey ? 'checked' : '') + ' /></div></td>');
		row.append('<td><div class="form-check form-check-sm form-check-custom form-check-solid"><input class="form-check-input column-add-to-table" type="checkbox" ' + (column.isPrimaryKey || !column.isNullable ? 'checked' : '') + ' /></div></td>');
		row.append('<td>' + badges + '</td>');
		
		return row;
	}

	/**
	 * اتصال رویدادهای ردیف ستون
	 */
	function bindColumnRowEvents() {
		// Individual checkbox change
		$('.column-checkbox').on('change', function () {
			updateSelectAllCheckbox();
		});
	}

	/**
	 * مدیریت انتخاب همه ستون‌ها
	 */
	function handleSelectAllColumns(e) {
		var checked = e.target.checked;
		$('.column-checkbox').prop('checked', checked);
	}

	/**
	 * بروزرسانی وضعیت چک‌باکس انتخاب همه
	 */
	function updateSelectAllCheckbox() {
		var total = $('.column-checkbox').length;
		var checked = $('.column-checkbox:checked').length;
		
		elements.selectAllCheckbox.checked = (total === checked);
		elements.selectAllCheckbox.indeterminate = (checked > 0 && checked < total);
	}

	/**
	 * دریافت ستون‌های انتخاب شده
	 */
	function getSelectedColumns() {
		var selected = [];
		
		$('#columnsPreviewBody tr').each(function () {
			var $row = $(this);
			var index = parseInt($row.data('column-index'));
			var $checkbox = $row.find('.column-checkbox');
			
			if ($checkbox.is(':checked')) {
				var column = $.extend(true, {}, state.columns[index]);
				column.suggestedDisplayName = $row.find('.column-display-name').val();
				column.required = $row.find('.column-required').is(':checked');
				column.addToTable = $row.find('.column-add-to-table').is(':checked');
				selected.push(column);
			}
		});
		
		return selected;
	}

	/**
	 * آماده‌سازی خلاصه نهایی
	 */
	function prepareFinalSummary() {
		var importMode = document.querySelector('input[name="importMode"]:checked').value;
		var sectionName = elements.sectionNameInput.value || 'اطلاعات اصلی';
		var moduleName = elements.moduleNameInput.value || state.selectedTable.tableName;
		
		var summary = 'تعداد ' + state.selectedColumns.length + ' ستون از جدول <strong>' + 
			state.selectedTable.schema + '.' + state.selectedTable.tableName + '</strong> ' +
			'در بخش "<strong>' + sectionName + '</strong>" ' +
			'با حالت "<strong>' + (importMode === 'replace' ? 'جایگزینی کامل' : 'افزودن') + '</strong>" ' +
			'ایجاد خواهد شد.';
		
		elements.importSummary.innerHTML = summary;
	}

	/**
	 * تایید و ایجاد فرم
	 */
	function handleConfirmImport() {
		console.log('DatabaseImporter: Confirming import...');
		
		if (!validateCurrentStep()) {
			return;
		}
		
		var importMode = document.querySelector('input[name="importMode"]:checked').value;
		var sectionName = elements.sectionNameInput.value.trim() || 'اطلاعات اصلی';
		var moduleName = elements.moduleNameInput.value.trim();
		
		var requestData = {
			connectionString: state.connectionString,
			schema: state.selectedTable.schema,
			tableName: state.selectedTable.tableName,
			moduleName: moduleName,
			importMode: importMode,
			sectionName: sectionName,
			selectedColumns: state.selectedColumns,
			existingFormDefinitionId: null // TODO: Get from FormBuilder if in edit mode
		};
		
		// Show loading
		var $btn = $(elements.btnConfirm);
		$btn.attr('disabled', true);
		$btn.html('<span class="spinner-border spinner-border-sm me-2"></span>در حال ایجاد...');
		
		$.ajax({
			url: '/Panel/FormBuilder/GenerateFormFromTable',
			method: 'POST',
			contentType: 'application/json',
			data: JSON.stringify(requestData),
			success: function (response) {
				console.log('Form generated successfully:', response);
				
				toastr.success('فرم با موفقیت ایجاد شد');
				
				// Close modal
				if (state.modal) {
					$(elements.modal).modal('hide');
				}
				
				// Reload or update FormBuilder
				if (typeof FormBuilderApp !== 'undefined' && response.formDefinition) {
					// Integrate with FormBuilder
					FormBuilderApp.populateFormFromDefinition(response.formDefinition);
				} else {
					// Reload page
					setTimeout(function () {
						window.location.reload();
					}, 1000);
				}
			},
			error: function (xhr, status, error) {
				console.error('Error generating form:', error);
				var message = xhr.responseJSON?.message || 'خطا در ایجاد فرم';
				toastr.error(message);
			},
			complete: function () {
				$btn.attr('disabled', false);
				$btn.html('<i class="ki-duotone ki-check fs-2"><span class="path1"></span><span class="path2"></span></i> تایید و ایجاد فرم');
			}
		});
	}

	/**
	 * بروزرسانی دکمه‌های ناوبری
	 */
	function updateNavigationButtons() {
		// Previous button
		if (state.currentStep === 1) {
			elements.btnPrevious.style.display = 'none';
		} else {
			elements.btnPrevious.style.display = 'inline-block';
		}
		
		// Next vs Confirm button
		if (state.currentStep === state.totalSteps) {
			elements.btnNext.style.display = 'none';
			elements.btnConfirm.style.display = 'inline-block';
		} else {
			elements.btnNext.style.display = 'inline-block';
			elements.btnConfirm.style.display = 'none';
		}
	}

	/**
	 * بازنشانی Modal
	 */
	function resetModal() {
		console.log('DatabaseImporter: Resetting modal...');
		
		// Reset state
		state.currentStep = 1;
		state.connectionString = null;
		state.selectedTable = null;
		state.columns = [];
		state.selectedColumns = [];
		
		// Reset form
		if (elements.form) {
			elements.form.reset();
		}
		
		// Reset connection status
		if (elements.connectionStatus) {
			elements.connectionStatus.style.display = 'none';
		}
		
		// Reset table info
		if (elements.tableInfoSection) {
			elements.tableInfoSection.style.display = 'none';
		}
		
		// Reset stepper
		if (state.stepper) {
			state.stepper.goTo(1);
		}
	}

	// Public API
	return {
		init: init,
		openModal: openModal
	};
})();

// Initialize on document ready
$(document).ready(function () {
	DatabaseImporter.init();
});

