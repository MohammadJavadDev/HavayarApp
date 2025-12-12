/**
 * QueryBuilder - Advanced Query Builder Library (v5.1.0)
 * Dependencies: jQuery
 * Updated: 2025-10-19
 *
 * New Features:
 * - Boolean type support (true/false select)
 * - Entity & listentity subfield operator rebuild fix
 * - getSQL() boolean safe handling (true/false → 1/0)
 * - fieldType tracking in getRules()
 */

(function ($) {
     'use strict';

     const QueryBuilder = function (element, options) {
          this.element = $(element);
          this.settings = $.extend(true, {}, QueryBuilder.DEFAULTS, options);
          this.groupCounter = 0;
          this.conditionCounter = 0;
          this.entityCache = {}; // cache entity fields
          this.init();
     };

     QueryBuilder.DEFAULTS = {
          fields: [],
          conditions: {},
         operators: {
              string: ['=', '!=', 'contains', 'starts', 'ends', 'null', '!null'],
              number: ['=', '!=', '>', '<', '>=', '<=', 'between', '!between'],
              date: ['=', '!=', '>', '<', 'between', '!between'],
              datetime: ['=', '!=', '>', '<', 'between', '!between'],
              datetimeshamsi: ['=', '!=', '>', '<', 'between', '!between'],
              dateshamsi: ['=', '!=', '>', '<', 'between', '!between'],
              select: ['=', '!=', 'null', '!null'],
              entity: ['=', '!=', 'null', '!null'],
              listentity: ['any', 'all'],
              boolean: ['=', '!='],
              listlong: ['contains', 'containsany', 'containsall', 'null', '!null'],
              liststring: ['contains', 'containsany', 'containsall', 'null', '!null']
         },
         operatorLabels: {
              '=': 'برابر',
              '!=': 'نابرابر',
              '>': 'بزرگتر از',
              '<': 'کوچکتر از',
              '>=': 'بزرگتر مساوی',
              '<=': 'کوچکتر مساوی',
              'contains': 'شامل',
              'starts': 'شروع با',
              'ends': 'پایان با',
              'between': 'بین',
              '!between': 'خارج از',
              'null': 'خالی',
              '!null': 'پر',
              'any': 'یکی از',
              'all': 'همه ی',
              'containsany': 'شامل یکی از',
              'containsall': 'شامل همه ی'
         },
          logic: ['AND', 'OR'],
          logicLabels: { 'AND': 'و', 'OR': 'یا' },
          lang: {
               addCondition: 'افزودن شرط',
               addGroup: 'افزودن گروه',
               delete: 'حذف',
               selectField: 'انتخاب فیلد',
               selectOperator: 'انتخاب عملگر',
               selectSubField: 'انتخاب زیرفیلد',
               loading: 'در حال بارگذاری...',
               errorLoading: 'خطا در بارگذاری'
          },
          classes: {
               group: 'qb-group',
               groupHeader: 'qb-group-header',
               groupBody: 'qb-group-body',
               condition: 'qb-condition',
               logic: 'qb-logic',
               field: 'qb-field',
               operator: 'qb-operator',
               subOperator: 'qb-sub-operator',
               subfield: 'qb-subfield',
               value: 'qb-value',
               input: 'qb-input',
               select: 'qb-select',
               btn: 'qb-btn',
               btnAdd: 'qb-btn-add',
               btnDelete: 'qb-btn-delete'
          },
          entityApiUrl: null,
          entityCacheTTL: 5 * 60 * 1000,
          readonly: false,
          onChange: null,
          onEntityLoad: null
     };

     QueryBuilder.prototype = {
        normalizeType: function (type) {
             const t = (type || '').toString().toLowerCase();
             if (t === 'bool' || t === 'boolean' || t === 'bit') return 'boolean';
             if (t === 'int' || t === 'integer' || t === 'long' || t === 'float' || t === 'double' || t === 'decimal' || t === 'number' || t === 'tinyint' || t === 'smallint' || t === 'bigint') return 'number';
             if (t === 'datetime2' || t === 'smalldatetime') return 'datetime';
             if (t === 'date' || t === 'datetime' || t === 'dateshamsi' || t === 'datetimeshamsi') return t;
             if (t === 'select' || t === 'entity' || t === 'listentity' || t === 'string') return t;
             if (t === 'listlong' || t === 'list<long>' || t === 'long[]' || t === 'int64[]' || t === 'list<int64>') return 'listlong';
             if (t === 'liststring' || t === 'list<string>' || t === 'string[]') return 'liststring';
             if (t === 'varchar' || t === 'nvarchar' || t === 'text' || t === 'ntext' || t === 'char' || t === 'nchar') return 'string';
             return 'string';
        },
          init: function () {
               this.element.addClass('query-builder');
               this.buildInitialGroup();
               this.attachEvents();
               this.injectStyles();
          },

          injectStyles: function () {
               if ($('#qb-custom-styles').length) return;

               const styles = `
        <style id="qb-custom-styles">
            .query-builder {
                 direction: rtl;
                padding: 20px;
            }

            .qb-group {
                position: relative;
                background: #f8f9fa;
                border-radius: 8px;
                padding: 5px;
                padding-right: 35px;
                margin-bottom: 12px;
                box-shadow: inset -1px 0px 13px 0px #d4dbe1;
            }

            .qb-group-header {
                display: flex;
                align-items: center;
                gap: 8px;
                margin-bottom: 16px;
            }

            .qb-group-body {
                display: flex;
                flex-direction: column;
              
            }

            .qb-logic-container {
                position: absolute;
                top: 5px;
                right: 5px;
                display: flex;
                gap: 4px;
                align-items: center;
                height: 90%;
                flex-direction: column;
                border: 1px solid #dee2e6;
                background: #ffffff;
                color: #6c757d;
                border-radius: 6px;
            }

            .qb-logic-btn {
                  min-width: 25px;
                    height: 90%;
                    background: #ffffff;
                    border-bottom: 1px solid #dee2e6;
                    border: none;
                    font-weight: 500;
                    cursor: pointer;
                    transition: all 0.2s;
                    outline: none;
                    position: absolute;
                    right: 5px;
                    top: 4px;
            }
            .qb-logic-btn div{
                     transform: rotateZ(90deg);
            }

            .qb-logic-btn:hover {
                background: #e9ecef;
            }

            .qb-logic-btn:active {
                transform: scale(0.95);
            }

            .qb-clear-group {
                width: 100%;
                height: 70px;
                background: #ffffff;
                border-top: 1px solid #dee2e6;
                border: none;
                
                font-weight: 500;
                cursor: pointer;
                transition: all 0.2s;
                outline: none;
            }

            .qb-clear-group:hover {
                background: #dc3545;
                color: white;
            }

            .qb-condition {
                display: flex;
                align-items: center;
                gap: 8px;
                flex-wrap: wrap;
            }

            .qb-select, .qb-input {
                    padding: 8px 8px 8px 30px;
                border: 1px solid #dee2e6;
                border-radius: 6px;
                background: #ffffff;
                color: #212529;
        
                outline: none;
                transition: all 0.2s;
                appearance: none;
                background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='12' height='12' viewBox='0 0 12 12'%3E%3Cpath fill='%23666' d='M6 9L1 4h10z'/%3E%3C/svg%3E");
                background-repeat: no-repeat;
                background-position: left 12px center;
            }

            .qb-input {
                background-image: none;
                padding: 8px 12px;
            }

            .qb-select:focus, .qb-input:focus {
                border-color: #0d6efd;
                box-shadow: 0 0 0 3px rgba(13, 110, 253, 0.1);
            }

            .qb-select:hover, .qb-input:hover {
                border-color: #adb5bd;
            }

            .qb-select:disabled {
                background-color: #e9ecef;
                cursor: not-allowed;
                opacity: 0.6;
            }

            .qb-field {
                min-width: 200px;
                flex: 1;
                border-color: #dc3545;
            }

            .qb-field:focus {
                border-color: #dc3545;
                box-shadow: 0 0 0 3px rgba(220, 53, 69, 0.1);
            }

            .qb-operator {
                min-width: 160px;
                border-color: #198754;
            }

            .qb-operator:focus {
                border-color: #198754;
                box-shadow: 0 0 0 3px rgba(25, 135, 84, 0.1);
            }

            .qb-subfield {
                min-width: 200px;
                flex: 1;
                border-color: #6f42c1;
            }

            .qb-subfield:focus {
                border-color: #6f42c1;
                box-shadow: 0 0 0 3px rgba(111, 66, 193, 0.1);
            }

            .qb-value {
                min-width: 200px;
                flex: 1;
                border-color: #0d6efd;
            }

            .qb-value:focus {
                border-color: #0d6efd;
                box-shadow: 0 0 0 3px rgba(13, 110, 253, 0.1);
            }

            .qb-btn {
                height: 40px;
                padding: 8px 16px;
                border: none;
                border-radius: 6px;
         
                font-weight: 500;
                cursor: pointer;
                transition: all 0.2s;
                white-space: nowrap;
                background: #e9ecef;
                color: #495057;
            }

            .qb-btn:hover {
                background: #dee2e6;
                transform: translateY(-1px);
            }

            .qb-btn:active {
                transform: translateY(0);
            }

            .qb-btn-add {
                background: #f8f9fa;
                color: #6c757d;
                border: 1px solid #dee2e6;
            }

            .qb-btn-delete {
                background: transparent;
                color: #6c757d;
                padding: 8px 12px;
                font-size: 18px;
            }

            .qb-btn-delete:hover {
                background: #dc3545;
                color: white;
            }

            [data-bs-theme="dark"] .qb-group {
                background: #1a1d20;
                border: 1px solid #2d3238;
            }

            [data-bs-theme="dark"] .qb-logic-container {
                background: #212529;
                border-color: #495057;
            }

            [data-bs-theme="dark"] .qb-logic-btn,
            [data-bs-theme="dark"] .qb-clear-group {
                background: #212529;
                border-color: #495057;
                color: #adb5bd;
            }

            [data-bs-theme="dark"] .qb-logic-btn:hover {
                background: #2d3238;
            }

            [data-bs-theme="dark"] .qb-clear-group:hover {
                background: #dc3545;
                color: white;
            }

            [data-bs-theme="dark"] .qb-select,
            [data-bs-theme="dark"] .qb-input {
                background: #212529;
                border-color: #495057;
                color: #f8f9fa;
                background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='12' height='12' viewBox='0 0 12 12'%3E%3Cpath fill='%23adb5bd' d='M6 9L1 4h10z'/%3E%3C/svg%3E");
            }

            [data-bs-theme="dark"] .qb-input {
                background-image: none;
            }

            [data-bs-theme="dark"] .qb-select:hover,
            [data-bs-theme="dark"] .qb-input:hover {
                border-color: #6c757d;
            }

            [data-bs-theme="dark"] .qb-select:focus,
            [data-bs-theme="dark"] .qb-input:focus {
                background: #2b3035;
            }

            [data-bs-theme="dark"] .qb-select:disabled {
                background-color: #2d3238;
            }

            [data-bs-theme="dark"] .qb-field {
                border-color: #dc3545;
            }

            [data-bs-theme="dark"] .qb-field:focus {
                border-color: #dc3545;
                box-shadow: 0 0 0 3px rgba(220, 53, 69, 0.25);
            }

            [data-bs-theme="dark"] .qb-operator {
                border-color: #198754;
            }

            [data-bs-theme="dark"] .qb-operator:focus {
                border-color: #198754;
                box-shadow: 0 0 0 3px rgba(25, 135, 84, 0.25);
            }

            [data-bs-theme="dark"] .qb-subfield {
                border-color: #6f42c1;
            }

            [data-bs-theme="dark"] .qb-subfield:focus {
                border-color: #6f42c1;
                box-shadow: 0 0 0 3px rgba(111, 66, 193, 0.25);
            }

            [data-bs-theme="dark"] .qb-value {
                border-color: #0d6efd;
            }

            [data-bs-theme="dark"] .qb-value:focus {
                border-color: #0d6efd;
                box-shadow: 0 0 0 3px rgba(13, 110, 253, 0.25);
            }

            [data-bs-theme="dark"] .qb-btn {
                background: #2d3238;
                color: #adb5bd;
            }

            [data-bs-theme="dark"] .qb-btn:hover {
                background: #383d44;
            }

            [data-bs-theme="dark"] .qb-btn-add {
                background: #212529;
                border-color: #495057;
                color: #adb5bd;
            }

            [data-bs-theme="dark"] .qb-btn-delete {
                background: transparent;
                color: #adb5bd;
            }

            [data-bs-theme="dark"] .qb-btn-delete:hover {
                background: #dc3545;
                color: white;
            }

            /* Multi-value input (listlong, liststring) */
            .qb-multi-value-wrapper .qb-tags-container {
                cursor: text;
            }

            .qb-multi-value-wrapper .qb-tags-container:focus-within {
                box-shadow: 0 0 0 3px rgba(13, 110, 253, 0.1);
            }

            .qb-tag {
                animation: tagSlide 0.2s ease;
            }

            @keyframes tagSlide {
                from { opacity: 0; transform: scale(0.8); }
                to { opacity: 1; transform: scale(1); }
            }

            [data-bs-theme="dark"] .qb-multi-value-wrapper .qb-tags-container {
                background: #212529;
                border-color: #0d6efd;
            }

            [data-bs-theme="dark"] .qb-multi-value-wrapper .qb-tag-input {
                background: transparent;
                color: #f8f9fa;
            }

            [data-bs-theme="dark"] .qb-multi-value-wrapper .qb-tag {
                background: #0a58ca;
            }

            .qb-loading {
                position: relative;
                pointer-events: none;
                opacity: 0.6;
            }

            .qb-loading::after {
                content: '';
                position: absolute;
                top: 50%;
                left: 12px;
                width: 14px;
                height: 14px;
                margin-top: -7px;
                border: 2px solid #0d6efd;
                border-radius: 50%;
                border-top-color: transparent;
                animation: spin 0.6s linear infinite;
            }

            @keyframes spin {
                to { transform: rotate(360deg); }
            }

            @media (max-width: 768px) {
                .qb-group {
                    padding-right: 16px;
                }

                .qb-logic-container {
                    position: static;
                    margin-bottom: 12px;
                    flex-direction: row;
                    height: auto;
                }

                .qb-logic-btn {
                    border-bottom: none;
                    border-left: 1px solid #dee2e6;
                }

                .qb-clear-group {
                    height: 40px;
                    border-top: none;
                    border-right: 1px solid #dee2e6;
                }

                .qb-condition {
                    flex-direction: column;
                    align-items: stretch;
                }

                .qb-select, .qb-input, .qb-field, .qb-operator, .qb-subfield, .qb-value {
                    width: 100%;
                    min-width: 100%;
                }

                .qb-group-header {
                    flex-wrap: wrap;
                }
            }

            .qb-group, .qb-condition {
                animation: slideIn 0.3s ease;
            }

            @keyframes slideIn {
                from {
                    opacity: 0;
                    transform: translateY(-10px);
                }
                to {
                    opacity: 1;
                    transform: translateY(0);
                }
            }
        </style>
    `;

               $('head').append(styles);
          },

          // ======================== UI BUILDERS ========================
          buildInitialGroup: function () {
               const group = this.createGroup(null, 'AND');
               this.element.append(group);
          },

          createGroup: function (parent, logic = 'AND') {
               const groupId = 'group-' + (++this.groupCounter);
               const level = parent ? parseInt(parent.data('level')) + 1 : 0;

               const $group = $('<div>', {
                    class: this.settings.classes.group,
                    'data-group-id': groupId,
                    'data-level': level,
                    'data-logic': logic
               });

               const $header = $('<div>', { class: this.settings.classes.groupHeader });
               const $addCondBtn = $('<button>', {
                    type: 'button',
                    class: this.settings.classes.btn + ' ' + this.settings.classes.btnAdd,
                    'data-action': 'add-condition',
                    text: this.settings.lang.addCondition
               });

               const $addGroupBtn = $('<button>', {
                    type: 'button',
                    class: this.settings.classes.btn + ' ' + this.settings.classes.btnAdd,
                    'data-action': 'add-group',
                    text: this.settings.lang.addGroup
               });

               const $logicBtn = $('<button>', {
                    type: 'button',
                    class: 'qb-logic-btn',
                    'data-action': 'toggle-logic',
                    html: '<div>' + this.settings.logicLabels[logic] + '</div>'
               });

               const $delGroupBtn = parent ? $('<button>', {
                    type: 'button',
                    class: this.settings.classes.btn + ' ' + this.settings.classes.btnDelete,
                    'data-action': 'delete-group',
                    html: '×'
               }) : null;

               const $body = $('<div>', { class: this.settings.classes.groupBody });
               $header.append($logicBtn, $addCondBtn, $addGroupBtn);
               if ($delGroupBtn) $header.append($delGroupBtn);
               $group.append($header, $body);
               return $group;
          },

          createCondition: function (group, fieldName = null, operator = null, subFieldName = null, value = null) {
               const conditionId = 'condition-' + (++this.conditionCounter);

               const $condition = $('<div>', {
                    class: this.settings.classes.condition,
                    'data-condition-id': conditionId
               });

               const $fieldSelect = this.createFieldSelect(fieldName);
               // Main operator (for top-level field)
               const $operatorSelect = this.createOperatorSelect(fieldName, (subFieldName ? null : operator));
               const field = fieldName ? this.settings.fields.find(f => f.name === fieldName) : null;

               let $subFieldSelect = null;
               if (field && (field.type === 'entity' || field.type === 'listentity')) {
                    let selectedFirst = null;
                    let remaining = [];
                    if (subFieldName) {
                         const parts = String(subFieldName).split('.');
                         selectedFirst = parts.shift();
                         remaining = parts;
                    }
                    $subFieldSelect = this.createSubFieldSelect(field, selectedFirst, 1);
                    if ($subFieldSelect) {
                         if (remaining && remaining.length) $subFieldSelect.data('remainingPath', remaining);
                         if (subFieldName) $subFieldSelect.data('initSubOpSelected', operator || null);
                         if (value != null) $subFieldSelect.data('initValue', value);
                    }
               }

               // If subField is present, value input will be based on sub-operator (built after entity fields load)
               const $valueInput = this.createValueInput(fieldName, (subFieldName ? null : operator), subFieldName, value);

               const $deleteBtn = $('<button>', {
                    type: 'button',
                    class: this.settings.classes.btn + ' ' + this.settings.classes.btnDelete,
                    'data-action': 'delete-condition',
                    html: '×'
               });

               $condition.append($fieldSelect, $operatorSelect);
               if ($subFieldSelect) $condition.append($subFieldSelect);
               $condition.append($valueInput, $deleteBtn);
               return $condition;
          },

          createFieldSelect: function (selected = null) {
               const $select = $('<select>', { class: this.settings.classes.field + ' ' + this.settings.classes.select });
               $select.append($('<option>', { value: '', text: this.settings.lang.selectField }));
               this.settings.fields.forEach(field => {
                    $select.append($('<option>', {
                         value: field.name,
                         text: field.label,
                         'data-type': field.type,
                         'data-fullname': field.fullName || '',
                         selected: field.name === selected
                    }));
               });
               return $select;
          },

          createOperatorSelect: function (fieldName, selected = null, forcedType = null) {
               const $select = $('<select>', { class: this.settings.classes.operator + ' ' + this.settings.classes.select });
               $select.append($('<option>', { value: '', text: this.settings.lang.selectOperator }));

               let type = forcedType;
               if (!type && fieldName) {
                    const field = this.settings.fields.find(f => f.name === fieldName);
                    type = field ? field.type : null;
               }

               if (type) {
                    type = this.normalizeType(type);
                    const operators = this.settings.operators[type] || [];
                    operators.forEach(op => {
                         $select.append($('<option>', {
                              value: op,
                              text: this.settings.operatorLabels[op] || op,
                              selected: op === selected
                         }));
                    });
               }
               return $select;
          },

         createSubOperatorSelect: function (parentFieldName, subFieldType, selected = null) {
              const $select = $('<select>', { class: this.settings.classes.subOperator + ' ' + this.settings.classes.select });
              $select.append($('<option>', { value: '', text: this.settings.lang.selectOperator }));

              if (subFieldType) {
                   const type = this.normalizeType(subFieldType);
                   const operators = this.settings.operators[type] || [];
                   operators.forEach(op => {
                        $select.append($('<option>', {
                             value: op,
                             text: this.settings.operatorLabels[op] || op,
                             selected: op === selected
                        }));
                   });
              }
              return $select;
         },

        createSubFieldSelect: function (parentEntityOrField, selected = null, level = 1) {
         const $select = $('<select>', { class: this.settings.classes.subfield + ' ' + this.settings.classes.select, 'data-level': level });
              $select.append($('<option>', { value: '', text: this.settings.lang.loading }));
              this.loadEntityFields(parentEntityOrField, $select, selected);
              return $select;
         },

         // ======================== VALUE INPUT ========================
         createValueInput: function (fieldName, operator, subFieldName = null, value = null) {
              const $container = $('<div>', { style: 'display:flex;gap:8px;flex:1;' });
              if (!fieldName || !operator || operator === 'null' || operator === '!null') return $container;
              const field = this.settings.fields.find(f => f.name === fieldName);
              if (!field) return $container;

              if ((field.type === 'entity' || field.type === 'listentity') && !subFieldName) return $container;

              // Resolve type and options by path using cache across levels
              const actualType = this.resolveTypeForPath(field, subFieldName);
              const inputMeta = { type: actualType };
              if (!subFieldName) {
                   if (field.type === 'select' && Array.isArray(field.options)) {
                        inputMeta.options = field.options;
                   }
              } else {
                   // Try to fetch options from subfield meta if available
                   const parts = String(subFieldName).split('.');
                   let lastMeta = null;
                   for (let i = 0; i < parts.length; i++) {
                        lastMeta = this.getSubFieldMetaByLevel(field, parts, i);
                        if (!lastMeta) break;
                   }
                   if (lastMeta && lastMeta.type === 'select' && Array.isArray(lastMeta.options)) {
                        inputMeta.options = lastMeta.options;
                   }
              }

              if (operator === 'between' || operator === '!between') {
                   const values = Array.isArray(value) ? value : [null, null];
                   const $input1 = this.createBasicInput(inputMeta, values[0]);
                   const $input2 = this.createBasicInput(inputMeta, values[1]);
                   $container.append($input1, $input2);
              } else if (operator === 'containsany' || operator === 'containsall') {
                   // Multi-value input for list types
                   const $input = this.createMultiValueInput(inputMeta, Array.isArray(value) ? value : (value ? [value] : []));
                   $container.append($input);
              } else {
                   const $input = this.createBasicInput(inputMeta, Array.isArray(value) ? value[0] : value);
                   $container.append($input);
              }
              return $container;
         },

         createBasicInput: function (field, value = null) {
              let $input;
              if (field.type === 'boolean') {
                   $input = $('<select>', { class: this.settings.classes.value + ' ' + this.settings.classes.select });
                   $input.append($('<option>', { value: 'true', text: 'درست', selected: value === 'true' }));
                   $input.append($('<option>', { value: 'false', text: 'غلط', selected: value === 'false' }));
              } else if (field.type === 'select' && field.options) {
                   $input = $('<select>', { class: this.settings.classes.value + ' ' + this.settings.classes.select });
                   const options = Array.isArray(field.options) ? field.options : [];
                   options.forEach(opt => {
                        let val; let label;
                        if (typeof opt === 'object') {
                             val = opt.value ?? opt.id ?? opt.key ?? opt.code ?? opt.name ?? opt;
                             label = opt.label ?? opt.text ?? opt.title ?? opt.name ?? String(val);
                        } else {
                             val = opt;
                             label = String(opt);
                        }
                        $input.append($('<option>', { value: val, text: label, selected: (val == value) }));
                   });
              } else {
                   const inputType = (field.type === 'number' || field.type === 'listlong' ? 'number' : field.type === "date" ? "date" : field.type === "datetime" ? "date" : 'text');
                   $input = $('<input>', { type: inputType, class: this.settings.classes.value + ' ' + this.settings.classes.input, value: value || '' });
                   if (field.type == "datetimeshamsi") {
                        $input.persianDatepicker(persionDatePickerOptionsDateTime)
                   }
                   if (field.type == "dateshamsi") {
                        $input.persianDatepicker(persionDatePickerOptionsDate)
                   }
              }
              return $input;
         },

         createMultiValueInput: function (field, values = []) {
              const self = this;
              const isNumeric = field.type === 'listlong';
              
              const $wrapper = $('<div>', { 
                   class: 'qb-multi-value-wrapper',
                   style: 'display:flex;flex-direction:column;gap:4px;flex:1;min-width:200px;'
              });
              
              const $tagsContainer = $('<div>', {
                   class: 'qb-tags-container',
                   style: 'display:flex;flex-wrap:wrap;gap:4px;padding:6px;border:1px solid #0d6efd;border-radius:6px;background:#fff;min-height:38px;'
              });
              
              const $hiddenInput = $('<input>', {
                   type: 'hidden',
                   class: this.settings.classes.value,
                   value: JSON.stringify(values || [])
              });
              
              const $input = $('<input>', {
                   type: isNumeric ? 'number' : 'text',
                   class: 'qb-tag-input',
                   placeholder: 'مقدار را وارد کنید و Enter بزنید',
                   style: 'border:none;outline:none;flex:1;min-width:100px;padding:4px;'
              });

              // Render existing tags
              const renderTags = function() {
                   $tagsContainer.find('.qb-tag').remove();
                   let currentValues = [];
                   try {
                        currentValues = JSON.parse($hiddenInput.val() || '[]');
                   } catch(e) { currentValues = []; }
                   
                   currentValues.forEach((val, idx) => {
                        const $tag = $('<span>', {
                             class: 'qb-tag',
                             style: 'display:inline-flex;align-items:center;gap:4px;padding:2px 8px;background:#0d6efd;color:#fff;border-radius:4px;font-size:12px;'
                        });
                        $tag.append($('<span>', { text: val }));
                        const $removeBtn = $('<span>', {
                             html: '×',
                             style: 'cursor:pointer;font-weight:bold;margin-right:2px;',
                             'data-index': idx
                        });
                        $removeBtn.on('click', function() {
                             currentValues.splice(idx, 1);
                             $hiddenInput.val(JSON.stringify(currentValues)).trigger('change');
                             renderTags();
                        });
                        $tag.append($removeBtn);
                        $tagsContainer.prepend($tag);
                   });
              };

              // Add value on Enter or comma
              $input.on('keydown', function(e) {
                   if (e.key === 'Enter' || e.key === ',') {
                        e.preventDefault();
                        let val = $(this).val().trim();
                        if (val) {
                             if (isNumeric) {
                                  val = parseFloat(val);
                                  if (isNaN(val)) return;
                             }
                             let currentValues = [];
                             try {
                                  currentValues = JSON.parse($hiddenInput.val() || '[]');
                             } catch(e) { currentValues = []; }
                             
                             if (!currentValues.includes(val)) {
                                  currentValues.push(val);
                                  $hiddenInput.val(JSON.stringify(currentValues)).trigger('change');
                                  renderTags();
                             }
                             $(this).val('');
                        }
                   }
              });

              // Also add on blur if there's a value
              $input.on('blur', function() {
                   let val = $(this).val().trim();
                   if (val) {
                        if (isNumeric) {
                             val = parseFloat(val);
                             if (isNaN(val)) return;
                        }
                        let currentValues = [];
                        try {
                             currentValues = JSON.parse($hiddenInput.val() || '[]');
                        } catch(e) { currentValues = []; }
                        
                        if (!currentValues.includes(val)) {
                             currentValues.push(val);
                             $hiddenInput.val(JSON.stringify(currentValues)).trigger('change');
                             renderTags();
                        }
                        $(this).val('');
                   }
              });

              $tagsContainer.append($input);
              $wrapper.append($tagsContainer, $hiddenInput);
              
              renderTags();
              
              return $wrapper;
         },

          // ======================== ENTITY ========================
          loadEntityFields: function (parentField, $select, selected = null) {
              
               const self = this;
              const fullName = parentField.fullName || parentField; // allow chained full name string
             if (!this.settings.entityApiUrl) return;

               if (self.entityCache[parentField.fullName]) return self.createEntityFields(parentField, $select, self.entityCache[parentField.fullName].data, selected);


               $.ajax({
                    url: this.settings.entityApiUrl + fullName,
                    method: 'GET',
                    dataType: 'json',
                    success: function (r) {
                         const props = r?.data?.properties || [];
                         const fields = props.map(p => ({
                              name: p.name,
                              type: self.normalizeType(p.dataType || 'string'),
                              label: p.displayName || p.name,
                              fullName: p.relatedEntityTypeFullName,
                              options:p.options
                         }));
                         self.entityCache[fullName] = { ts: Date.now(), data: fields };
                         self.createEntityFields(parentField, $select, fields, selected)
                    }
               });
          },
          createEntityFields: function (parentField, $select, fields, selected = null) {
               $select.empty().append($('<option>', { value: '', text: this.settings.lang.selectSubField }));
               const nextLevel = ($select.data('level') || 1)
               fields.forEach(f => {
                    if (nextLevel == 4 && f.type == "entity" || f.type == 'listentity') {
                         //do noting
                    }
                    else {

                         $select.append($('<option>', { value: f.name, text: f.label, 'data-type': f.type, selected: f.name === selected }));
                    }
               });

               // If an initial subfield is selected
               const initSelected = selected || null;
               if (initSelected) {

                    const $condition = $select.closest('.' + this.settings.classes.condition);
                    const sub = fields.find(x => x.name === initSelected);
                    const initOp = $select.data('initSubOpSelected') || null;
                    const $existing = $condition.find('.' + this.settings.classes.subOperator + '[data-level="' + ($select.data('level') || 1) + '"]');
                    // Only add sub-operator at this level if sub is a leaf (not entity/listentity)
                    if (sub && sub.type !== 'entity' && sub.type !== 'listentity') {
                         const $subOp = this.createSubOperatorSelect(parentField.name || parentField, sub ? sub.type : null, initOp).attr('data-level', $select.data('level') || 1);
                         if ($existing.length) $existing.replaceWith($subOp); else $select.after($subOp);

                         // Build value input based on sub-operator selection
                         if (initOp) {
                              const $v = $condition.find('> div').first();
                              const initValue = $select.data('initValue');
                              $v.replaceWith(this.createValueInput(parentField.name || parentField, initOp, self.composeSubPath($condition), initValue));
                         }
                    } else {
                         // Ensure any sub-operator at this level is removed for non-leaf
                         if ($existing.length) $existing.remove();
                    }

                    // If there is remaining path, auto-create next level
                    const remaining = $select.data('remainingPath') || [];
                    if (remaining.length && sub && (sub.type === 'entity' || sub.type === 'listentity')) {
                         const nextLevel = ($select.data('level') || 1) + 1;
                         if (nextLevel <= 3) {
                              const nextSelected = remaining.shift();
                              // chain: keep both display path and entity full name for API
                              const parentChain = ((parentField.path || (parentField.fullName || parentField)) + '.' + initSelected);
                              const nextParent = { fullName: sub.fullName || (parentField.fullName || parentField), path: parentChain };
                              const $next = this.createSubFieldSelect(nextParent, nextSelected, nextLevel);
                              $next.data('remainingPath', remaining);
                              $select.after($next);
                         }
                    }
                    this.updateConditionTree($condition);
               }
          },

        composeSubPath: function ($condition) {
              const parts = [];
              $condition.find('.' + this.settings.classes.subfield).each(function () {
                   const v = $(this).val();
                   if (v) parts.push(v);
              });
              return parts.join('.') || null;
         },

        composeSubPathUpTo: function ($condition, uptoLevelInclusive) {
             const parts = [];
             $condition.find('.' + this.settings.classes.subfield).each(function () {
                  const lvl = parseInt($(this).attr('data-level') || '1', 10);
                  if (lvl <= uptoLevelInclusive) {
                       const v = $(this).val();
                       if (v) parts.push(v);
                  }
             });
             return parts.join('.') || null;
        },

        getSubPathParts: function ($condition) {
             const parts = [];
             $condition.find('.' + this.settings.classes.subfield).each(function () {
                  const v = $(this).val();
                  if (v) parts.push(v);
             });
             return parts;
        },

        getParentEntityFullName: function (baseField, parts, uptoIndexExclusive) {
             let currentEntity = baseField.fullName;
             for (let i = 0; i < uptoIndexExclusive; i++) {
                  const segment = parts[i];
                  const cached = this.entityCache[currentEntity];
                  if (!cached || !cached.data) break;
                  const meta = cached.data.find(f => f.name === segment);
                  if (!meta || !meta.fullName) break;
                  currentEntity = meta.fullName;
             }
             return currentEntity;
        },

        getSubFieldMetaByLevel: function (baseField, parts, levelIndex) {
             const parentEntity = this.getParentEntityFullName(baseField, parts, levelIndex);
             const cached = this.entityCache[parentEntity];
             if (!cached || !cached.data) return null;
             const name = parts[levelIndex];
             return cached.data.find(f => f.name === name) || null;
        },

        resolveTypeForPath: function (baseField, subPath) {
             if (!subPath) return baseField.type || 'string';
             const parts = String(subPath).split('.');
             let lastMeta = null;
             for (let i = 0; i < parts.length; i++) {
                  lastMeta = this.getSubFieldMetaByLevel(baseField, parts, i);
                  if (!lastMeta) break;
             }
             return this.normalizeType(lastMeta?.type || baseField.type || 'string');
        },

        updateConditionTree: function ($condition) {
             const fieldName = $condition.find('.' + this.settings.classes.field).val();
             const fieldObj = this.settings.fields.find(f => f.name === fieldName) || {};
             const parts = this.getSubPathParts($condition);
             const levels = [];
             for (let i = 0; i < parts.length; i++) {
                  const meta = this.getSubFieldMetaByLevel(fieldObj, parts, i) || {};
                  const $sel = $condition.find('.' + this.settings.classes.subfield + '[data-level="' + (i + 1) + '"]');
                  const $op = $condition.find('.' + this.settings.classes.subOperator + '[data-level="' + (i + 1) + '"]');
                  levels.push({
                       level: i + 1,
                       name: parts[i] || null,
                       type: meta.type || null,
                       fullName: meta.fullName || null,
                       selectEl: $sel.get(0) || null,
                       operatorEl: $op.get(0) || null
                  });
             }
             const tree = {
                  base: {
                       name: fieldObj.name || null,
                       type: fieldObj.type || null,
                       fullName: fieldObj.fullName || null
                  },
                  levels: levels
             };
             $condition.data('qbTree', tree);
             return tree;
        },

          // ======================== EVENTS ========================
          attachEvents: function () {
               const self = this;

               // Toggle logic (AND / OR)
               this.element.on('click', '[data-action="toggle-logic"]', function () {
                    const $group = $(this).closest('.' + self.settings.classes.group);
                    const logic = $group.attr('data-logic') === 'AND' ? 'OR' : 'AND';
                    $group.attr('data-logic', logic);
                    $(this).html('<div>' + self.settings.logicLabels[logic] + '</div>');
                    self.triggerChange();
               });

               // Add condition
               this.element.on('click', '[data-action="add-condition"]', function () {
                    const $group = $(this).closest('.' + self.settings.classes.group);
                    const $body = $group.find('> .' + self.settings.classes.groupBody);
                    $body.append(self.createCondition($group));
                    self.triggerChange();
               });

               // Add group
               this.element.on('click', '[data-action="add-group"]', function () {
                    const $group = $(this).closest('.' + self.settings.classes.group);
                    const $body = $group.find('> .' + self.settings.classes.groupBody);
                    $body.append(self.createGroup($group));
                    self.triggerChange();
               });

               // Delete condition
               this.element.on('click', '[data-action="delete-condition"]', function () {
                    $(this).closest('.' + self.settings.classes.condition).remove();
                    self.triggerChange();
               });

               // Delete group
               this.element.on('click', '[data-action="delete-group"]', function () {
                    $(this).closest('.' + self.settings.classes.group).remove();
                    self.triggerChange();
               });

              // Field change
             this.element.on('change', '.' + this.settings.classes.field, function () {
                   const $condition = $(this).closest('.' + self.settings.classes.condition);
                   const fieldName = $(this).val();
                   const $oldOperator = $condition.find('.' + self.settings.classes.operator);
                   const $valueDiv = $condition.find('> div').first();

                   const newOp = self.createOperatorSelect(fieldName);
                   $oldOperator.replaceWith(newOp);

                   // Add/remove subfield select based on field type
                   const field = self.settings.fields.find(f => f.name === fieldName);
                   const requiresSub = field && (field.type === 'entity' || field.type === 'listentity');
                   const $oldSub = $condition.find('.' + self.settings.classes.subfield);

                   if (requiresSub) {
                        const $newSub = self.createSubFieldSelect(field);
                        if ($oldSub.length) {
                             $oldSub.replaceWith($newSub);
                        } else {
                             newOp.after($newSub);
                        }
                   } else if ($oldSub.length) {
                        $oldSub.remove();
                   }

                   $valueDiv.replaceWith(self.createValueInput(fieldName, null));
                   self.triggerChange();
              });

               // Operator change
               this.element.on('change', '.' + this.settings.classes.operator, function () {
                    const $c = $(this).closest('.' + self.settings.classes.condition);
                    const fieldName = $c.find('.' + self.settings.classes.field).val();
                    const op = $(this).val();
                   const $v = $c.find('> div').first();

                   // If there are subfields, reset level 1 and remove deeper levels
                   const $subfields = $c.find('.' + self.settings.classes.subfield);
                   if ($subfields.length) {
                        // Remove deeper levels (>1)
                        $subfields.each(function () {
                             const lvl = parseInt($(this).attr('data-level') || '1', 10);
                             if (lvl > 1) $(this).remove();
                        });
                        $c.find('.' + self.settings.classes.subOperator).each(function () {
                             const lvl = parseInt($(this).attr('data-level') || '1', 10);
                             if (lvl >= 1) $(this).remove();
                        });

                        // Reset subfield at level 1 to empty and avoid creating any sub-operator yet
                        const $sf1 = $c.find('.' + self.settings.classes.subfield + '[data-level="1"]');
                        if ($sf1.length) {
                             $sf1.val('');
                        }

                        // Build value for main operator only (no subpath)
                        $v.replaceWith(self.createValueInput(fieldName, op, null));
                   } else {
                        // No subfields: build value for main operator
                        $v.replaceWith(self.createValueInput(fieldName, op, null));
                   }
                    self.triggerChange();
               });

              // Subfield change (multi-level)
               this.element.on('change', '.' + this.settings.classes.subfield, function () {
                    const $c = $(this).closest('.' + self.settings.classes.condition);
                   const fieldName = $c.find('.' + self.settings.classes.field).val();
                   const subName = $(this).val();
                   const level = parseInt($(this).attr('data-level') || '1', 10);

                    const field = self.settings.fields.find(f => f.name === fieldName);
                  // Resolve subField meta at current level using tree and cache
                  const parts = self.getSubPathParts($c);
                  const subField = (field && parts[level - 1]) ? self.getSubFieldMetaByLevel(field, parts, level - 1) : null;

                   // Remove deeper levels and sub-operators beyond this level
                   $c.find('.' + self.settings.classes.subfield).each(function () {
                        const l = parseInt($(this).attr('data-level') || '1', 10);
                        if (l > level) $(this).remove();
                   });
                   $c.find('.' + self.settings.classes.subOperator).each(function () {
                        const l = parseInt($(this).attr('data-level') || '1', 10);
                        if (l >= level) $(this).remove();
                   });

                   // Only add sub-operator after user selects a subfield value
                   if (!subName) {
                        const $v = $c.find('> div').first();
                        $v.replaceWith(self.createValueInput(fieldName, null, self.composeSubPath($c)));
                        self.updateConditionTree($c);
                        self.triggerChange();
                        return;
                   }
 

                   // Reset value until sub-operator selected
                   const $v = $c.find('> div').first();
                   $v.replaceWith(self.createValueInput(fieldName, null, self.composeSubPath($c)));

                   // If current selected sub is entity/listentity and level < 4, add next subfield select
                   if (subName && (subField && (subField.type === 'entity' || subField.type === 'listentity')) && level < 4) {
                       const parentChain = (field.fullName) + '.' + subName;
                       const $next = self.createSubFieldSelect({ fullName: subField.fullName , path: parentChain }, null, level + 1);
                        $(this).after($next);
                   }
                   else
                   {
                    const $subOp = self.createSubOperatorSelect(fieldName, subField ? subField.type : null).attr('data-level', level);
                    $(this).after($subOp);
                   }
                   self.updateConditionTree($c);
                    self.triggerChange();
               });

              // Sub-operator change
              this.element.on('change', '.' + this.settings.classes.subOperator, function () {
                   const $c = $(this).closest('.' + self.settings.classes.condition);
                   const fieldName = $c.find('.' + self.settings.classes.field).val();
                   const level = parseInt($(this).attr('data-level') || '1', 10);
                   // Remove deeper levels (subfields and suboperators)
                   $c.find('.' + self.settings.classes.subfield).each(function () {
                        const l = parseInt($(this).attr('data-level') || '1', 10);
                        if (l > level) $(this).remove();
                   });
                   $c.find('.' + self.settings.classes.subOperator).each(function () {
                        const l = parseInt($(this).attr('data-level') || '1', 10);
                        if (l > level) $(this).remove();
                   });

                   const subName = self.composeSubPathUpTo($c, level);
                   const op = $(this).val();
                   const $v = $c.find('> div').first();
                   $v.replaceWith(self.createValueInput(fieldName, op, subName));
                   self.updateConditionTree($c);
                   self.triggerChange();
              });

               // Value change
               this.element.on('change input', '.' + this.settings.classes.value, function () {
                    self.triggerChange();
               });
          },

          // ======================== RULES ========================
          getRules: function () {
               const self = this;
               function parseGroup($g) {
                    const logic = $g.attr('data-logic') || 'AND';
                    const criteria = [];
                    $g.find('> .' + self.settings.classes.groupBody).children().each(function () {
                         const $ch = $(this);
                         if ($ch.hasClass(self.settings.classes.condition)) {
                              const field = $ch.find('.' + self.settings.classes.field).val();
                              const op = $ch.find('.' + self.settings.classes.operator).val();
                              // deepest sub-operator at highest data-level
                              let subOp = null;
                              let subLevel = -1;
                              $ch.find('.' + self.settings.classes.subOperator).each(function () {
                                   const l = parseInt($(this).attr('data-level') || '1', 10);
                                   const v = $(this).val();
                                   if (v && l > subLevel) { subOp = v; subLevel = l; }
                              });
                             const sub = self.composeSubPath($ch) || null;
                             const effectiveOp = sub ? subOp : op;
                             
                             // Parse values - handle JSON arrays for multi-value inputs
                             let vals = [];
                             $ch.find('.' + self.settings.classes.value).each(function () {
                                  const rawVal = $(this).val();
                                  // Check if it's a JSON array (from multi-value input)
                                  if (rawVal && rawVal.startsWith('[')) {
                                       try {
                                            const parsed = JSON.parse(rawVal);
                                            if (Array.isArray(parsed)) {
                                                 vals = vals.concat(parsed);
                                            } else {
                                                 vals.push(rawVal);
                                            }
                                       } catch(e) {
                                            vals.push(rawVal);
                                       }
                                  } else {
                                       vals.push(rawVal);
                                  }
                             });

                             // If subfield present, require sub-operator; otherwise require main operator
                             if (!field || (sub ? !subOp : !op)) return;

                             const fieldObj = self.settings.fields.find(f => f.name === field);
                             const fieldType = self.resolveTypeForPath(fieldObj || {}, sub || null);

                              // Build nested subField tree for entity/listentity when a sub path exists
                              if ((fieldObj?.type === 'entity' || fieldObj?.type === 'listentity') && sub) {
                                   const parts = sub.split('.');
                                   function buildTree(idx) {
                                        const meta = self.getSubFieldMetaByLevel(fieldObj, parts, idx) || {};
                                        const node = {
                                             data: parts[idx],
                                             condition: null,
                                             value: [],
                                             fieldType: meta.type || 'string'
                                        };
                                        if (idx < parts.length - 1) {
                                             node.subField = buildTree(idx + 1);
                                        } else {
                                             node.condition = subOp;
                                             node.value = vals;
                                             node.fieldType = self.normalizeType(meta.type || 'string');
                                        }
                                        return node;
                                   }
                                   const subTree = buildTree(0);
                                   const top = {
                                        data: field,
                                        condition: (fieldObj.type === 'listentity') ? op : (op || null),
                                        subField: subTree,
                                        value: (fieldObj.type === 'listentity') ? [] : [],
                                        fieldType: fieldObj.type
                                   };
                                   criteria.push(top);
                              } else {
                                   const c = {
                                        data: field,
                                        condition: sub ? subOp : op,
                                        subField: sub,
                                        value: vals,
                                        fieldType: fieldType
                                   };
                                   criteria.push(c);
                              }
                         } else if ($ch.hasClass(self.settings.classes.group)) {
                              criteria.push(parseGroup($ch));
                         }
                    });
                    return { logic, criteria };
               }

               const $root = this.element.find('> .' + this.settings.classes.group).first();
               return parseGroup($root);
          },

          // ======================== VALIDATION ========================
          validate: function () {
               const self = this;
               const errors = [];

               function isEmpty(v) {
                    return v === null || v === undefined || (typeof v === 'string' && v.trim() === '');
               }

               function walk($g) {
                    $g.find('> .' + self.settings.classes.groupBody).children().each(function () {
                         const $ch = $(this);
                         if ($ch.hasClass(self.settings.classes.condition)) {
                              const conditionId = $ch.attr('data-condition-id') || null;
                              const field = $ch.find('.' + self.settings.classes.field).val();
                              const mainOp = $ch.find('.' + self.settings.classes.operator).val();

                              // deepest sub-operator at highest data-level
                              let subOp = null;
                              let subLevel = -1;
                              $ch.find('.' + self.settings.classes.subOperator).each(function () {
                                   const l = parseInt($(this).attr('data-level') || '1', 10);
                                   const v = $(this).val();
                                   if (v && l > subLevel) { subOp = v; subLevel = l; }
                              });
                              const sub = self.composeSubPath($ch) || null;

                              if (!field) {
                                   errors.push({ id: conditionId, type: 'field', message: 'فیلد انتخاب نشده است.' });
                                   return; // skip further checks for this condition
                              }

                              const fieldObj = self.settings.fields.find(f => f.name === field) || {};
                              if ((fieldObj.type === 'entity' || fieldObj.type === 'listentity') && !sub) {
                                   errors.push({ id: conditionId, type: 'subfield', message: 'زیرفیلد انتخاب نشده است.' });
                              }

                              if (sub && !subOp) {
                                   errors.push({ id: conditionId, type: 'subOperator', message: 'عملگر زیرفیلد انتخاب نشده است.' });
                              }
                              if (!sub && !mainOp) {
                                   errors.push({ id: conditionId, type: 'operator', message: 'عملگر انتخاب نشده است.' });
                              }

                              const op = sub ? subOp : mainOp;
                              const vals = $ch.find('.' + self.settings.classes.value).map(function () {
                                   return $(this).val();
                              }).get();

                             // Value requirements
                             if (op && op !== 'null' && op !== '!null') {
                                  if (op === 'between' || op === '!between') {
                                       if (vals.length < 2 || isEmpty(vals[0]) || isEmpty(vals[1])) {
                                            errors.push({ id: conditionId, type: 'value', message: 'محدوده مقدار کامل نیست.' });
                                       }
                                  } else if (op === 'containsany' || op === 'containsall') {
                                       // For multi-value: check JSON array has at least one value
                                       let parsedVals = [];
                                       try {
                                            if (vals.length > 0 && vals[0].startsWith('[')) {
                                                 parsedVals = JSON.parse(vals[0]);
                                            }
                                       } catch(e) {}
                                       if (!parsedVals.length) {
                                            errors.push({ id: conditionId, type: 'value', message: 'حداقل یک مقدار باید وارد شود.' });
                                       }
                                  } else {
                                       if (vals.length < 1 || isEmpty(vals[0])) {
                                            errors.push({ id: conditionId, type: 'value', message: 'مقدار وارد نشده است.' });
                                       }
                                  }
                             }
                         } else if ($ch.hasClass(self.settings.classes.group)) {
                              walk($ch);
                         }
                    });
               }

               const $root = this.element.find('> .' + this.settings.classes.group).first();
               walk($root);
               return { valid: errors.length === 0, errors };
          },

          // ======================== SQL GENERATION ========================
          getSQL: function () {
               const rules = this.getRules();
               const map = (t, v) => {
                    if (t === 'boolean') return v === 'true' ? 1 : 0;
                    if (v === null || v === undefined || v === '') return 'NULL';
                    return `'${String(v).replace(/'/g, "''")}'`;
               };

               function renderCondition(c) {
                    const field = c.data + (c.subField ? '.' + c.subField : '');
                    const op = c.condition;
                    const type = c.fieldType || 'string';
                    if (op === 'null') return `${field} IS NULL`;
                    if (op === '!null') return `${field} IS NOT NULL`;

                    const val = c.value || [];
                    switch (op) {
                         // String operators
                         case 'contains': 
                              // For listlong/liststring: check if list contains value
                              if (type === 'listlong' || type === 'liststring') {
                                   return `${field}.Contains(${map(type === 'listlong' ? 'number' : 'string', val[0])})`;
                              }
                              return `${field} LIKE ${map(type, '%' + val[0] + '%')}`;
                         case 'starts': return `${field} LIKE ${map(type, val[0] + '%')}`;
                         case 'ends': return `${field} LIKE ${map(type, '%' + val[0])}`;
                         case 'between': return `${field} BETWEEN ${map(type, val[0])} AND ${map(type, val[1])}`;
                         case '!between': return `NOT (${field} BETWEEN ${map(type, val[0])} AND ${map(type, val[1])})`;
                         case 'any': return `${field} IN (${val.map(v => map(type, v)).join(',')})`;
                         case 'all': return val.map(v => `${field} = ${map(type, v)}`).join(' AND ');
                         // List operators (listlong, liststring)
                         case 'containsany': 
                              // List contains any of the provided values
                              const anyVals = val.map(v => map(type === 'listlong' ? 'number' : 'string', v)).join(',');
                              return `${field}.Any(x => [${anyVals}].Contains(x))`;
                         case 'containsall':
                              // List contains all of the provided values
                              const allVals = val.map(v => map(type === 'listlong' ? 'number' : 'string', v)).join(',');
                              return `[${allVals}].All(x => ${field}.Contains(x))`;
                         default: return `${field} ${op} ${map(type, val[0])}`;
                    }
               }

               function renderGroup(g) {
                    if (!g.criteria || !g.criteria.length) return '';
                    const parts = g.criteria.map(x => x.criteria ? '(' + renderGroup(x) + ')' : renderCondition(x));
                    return parts.join(' ' + g.logic + ' ');
               }

               const sql = renderGroup(rules);
               return sql ? 'WHERE ' + sql : '';
          },

          // ======================== JSON OUTPUT ========================
          toJSON: function () {
               return JSON.stringify(this.getRules(), null, 2);
          },

          fromJSON: function (json) {
               if (!json) return;
               try {
                    const obj = typeof json === 'string' ? JSON.parse(json) : json;
                    this.setRules(obj);
               } catch (e) { console.error('Invalid JSON', e); }
          },

          setRules: function (rules) {
               this.element.empty();
               this.groupCounter = 0;
               this.conditionCounter = 0;
               if (rules) this.loadGroup(this.element, rules);
               else this.buildInitialGroup();
          },

          loadGroup: function ($parent, data) {
               const $group = this.createGroup($parent.hasClass('query-builder') ? null : $parent, data.logic);
               const $body = $group.find('> .' + this.settings.classes.groupBody);
               (data.criteria || []).forEach(item => {
                    if (item.criteria) this.loadGroup($body, item);
                    else $body.append(this.createCondition($group, item.data, item.condition, item.subField, item.value));
               });
               $parent.append($group);
          },

          triggerChange: function () {
               if (typeof this.settings.onChange === 'function')
                    this.settings.onChange.call(this, this.getRules());
          }
     };

     $.fn.queryBuilder = function (options) {
          return this.each(function () {
               const $this = $(this);
               let data = $this.data('querybuilder');
               if (!data) {
                    data = new QueryBuilder(this, options);
                    $this.data('querybuilder', data);
               }
          });
     };

     $.fn.queryBuilder.Constructor = QueryBuilder;

})(jQuery);

