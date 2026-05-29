
const url = window.AppConfig?.realtimeHubUrl || "https://localhost:62350/hubs/realtime";

async function getConnectionToken() {
	try {
		const res = await fetch('/api/realtime/token', { method: 'POST' });
		if (!res.ok) return null;
		const data = await res.json();
		return data.token;
	} catch {
		return null;
	}
}

const connection = new signalR.HubConnectionBuilder()
	.withUrl(url, {
		accessTokenFactory: () => getConnectionToken(),
		transport: signalR.HttpTransportType.WebSockets,
		withCredentials: true
	})
	.withAutomaticReconnect()
	.build();

// simple audio beeper (inline)
function playNotificationSound() {
	try {
		const ctx = new (window.AudioContext || window.webkitAudioContext)();
		const o = ctx.createOscillator();
		const g = ctx.createGain();
		o.type = "sine";
		o.frequency.value = 880; // A5
		o.connect(g);
		g.connect(ctx.destination);
		g.gain.setValueAtTime(0.001, ctx.currentTime);
		g.gain.exponentialRampToValueAtTime(0.2, ctx.currentTime + 0.01);
		g.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.25);
		o.start();
		o.stop(ctx.currentTime + 0.26);
	} catch (e) {
		// ignore if autoplay blocked
	}
}

// دریافت پیام
connection.on("ReceiveUnreadNotification", message => {
	
	if (Array.isArray(message)) {

		message.forEach(z => {
			 
			  z.path = z?.viewPath ?? "#";
			var tmpl = notificationTmpl(z);
			$("#notificationSection")
				.append(tmpl)
		})
	}
	 
	playNotificationSound();

	CalculationCountUnreadNotifications();


});

// دریافت پیام
connection.on("ReceiveNotification", message => {
	 
	message.path = message?.viewPath ?? "#";
	 
	var tmpl = notificationTmpl(message);
	$("#notificationSection")
		.prepend(tmpl)
		 
	playNotificationSound();

	CalculationCountUnreadNotifications();

});


function notificationTmpl(z) {
      

	var notiTmpl = `
					<div class="d-flex flex-stack py-4 px-3 notification-item"
					data-row="notification">

				    <div class="d-flex align-items-center">

				 
					   <div class="symbol symbol-40px me-4">
						  <div class=" d-flex flex-column justify-content-center gap-1" >

							 <div class="symbol-label bg-light-danger" style="    width: 20px;    height: 20px;">
								  <span class="notification-icon"
									  data-action="readNotification"
									  data-id="${z.id}"
									   >
									<i class="ki-duotone ki-eye fs-3 text-danger">
									    <span class="path1"></span>
									    <span class="path2"></span>
									    <span class="path3"></span>
									</i>
								 </span>
							 </div>
							

						  <div class="symbol-label bg-light-primary" style="    width: 20px;    height: 20px;">
							 <a href="${z.path}"
							    class="notification-link-icon"
							     >
								<i class="ki-duotone ki-arrow-right fs-4 text-gray-600">
								    <span class="path1"></span>
								    <span class="path2"></span>
								</i>
							 </a>
							  </div>
						  </div>
					   </div>

					 
					   <div class="me-2">
						  <a href="${z.path}"
							class="fs-6 text-gray-800 text-hover-primary fw-bold d-block">
							${z.title}
						  </a>
						 
					   </div>

				    </div>

				  
				    <span class="badge badge-light fs-8 text-gray-600" style="direction:ltr;">
					${z.createdOnShamsiDateTime}
				    </span>
				</div>
					`;
	return notiTmpl;

}

connection.on("EntityChanged", event => {
	console.log("Entity changed:", event);


	if (event.entityName === "User" && event.operation === "Create") {

		toastr.info(event.message || `موجودیت ${event.entityName} با شناسه ${event.entityId} ایجاد شد`, "تغییر موجودیت");
	}

 
});

// اتصال
connection.start()
	.then()
	.catch(err => console.error(err));

// clear highlight when user opens the menu (click on the bell)
document.addEventListener("click", function (ev) {
	const bellBtn = document.getElementById("kt_menu_item_notification");
	if (!bellBtn) return;
	if (bellBtn.contains(ev.target)) {
		// toggle off on click; next notification will add it again
		bellBtn.classList.remove("has-notification");
	}
});


window.UpdateCurrentPage = async function (path,title) {

	if (connection && connection.state === signalR.HubConnectionState.Connected) {
		connection.invoke("UpdateOpenedPage", path, title)
			.catch(err => console.error("UpdateOpenedPage error:", err));
	}
}



 

window.ClosePage = async function (path) {

	connection.invoke("ClosePage", path).catch(() => { });
}


/**
* Query Designer - Main Application
* سیستم طراحی Query با قابلیت Drag & Drop
*/

class QueryDesigner {
     constructor() {
          this.jsPlumbInstance = null;
          this.tables = [];
          this.selectedTables = [];
          this.relations = [];
          this.filters = [];
          this.columns = [];
          this.parameters = [];
          this.builtInParams = [];
          this.zoom = 1;
          this.editMode = false;
          this.reportType = 0;
          this.reportId = null;
          this.selectedConnection = null;
          this.firstClickedEndpoint = null;
          this.mode = null;
          this.entityFullName = null;
          this._writeModePreviewed = false;
          this._writeModeColumns = [];
          this.storedProcedures = [];
          this.systemEnums = [];
          this.actionOptions = [];
          this.customActionButtons = [];
          this.eventScripts = {
                       onSelectedRow: '',
                       onRowAdded: '',
                   };
          this.actionOptionsDefualt = [
               { dataActionName: "new", enable: true, title:"جدید" },
               { dataActionName: "edit", enable: true, title:"ویرایش" },
               { dataActionName: "delete", enable: true, title:"حذف" },
               { dataActionName: "exportExcell", enable: true, title:"خروجی اکسل" },

          ];
          this.sqlEditor = null;
          this.onSelectedRowEditor = null;
          this.onRowAddedEditor = null;

          this.init();
     }

     async init() {
          this.reportType = window.__reportType;
          if (!this.reportType) this.reportType = 0;
          this.checkEditMode();
          this.initMode();
          this.initJsPlumb();
          this.bindEvents();
          await this.loadTablesAndViews();
          await this.loadStoredProcedures();
          this.updateFiltersList();
          this.loadBuiltInParameters();
          this.updateParametersList();
          await this.loadSystemEnums();
          this.entityFullName = $("#entityFullName").val()
          if (this.editMode && this.reportId) this.loadReport(this.reportId)
          else {
           this.loadActionOptions();
           this.initSqlEditor();
          }

          this.initonSelectedRowEditor();
          this.initonOnRowAddedEditor();

          this.loadCustomActionButtons();
          this.loadEventScripts();


          this.initDraggableForColumns();
          this.initDraggableForWritedColumns();

        
     }

     initonSelectedRowEditor(script = "") {
          this.onSelectedRowEditor = ace.edit('scriptOnSelectedRow');
          this.onSelectedRowEditor.setTheme('ace/theme/chrome');
          this.onSelectedRowEditor.session.setMode('ace/mode/javascript');

      
          this.onSelectedRowEditor.getSession().on('change', (e) => {
               const value = this.onSelectedRowEditor.getValue()
               this.eventScripts['onSelectedRow'] = value;
               this._refreshEventBadge("onSelectedRow");
          });

          this.onSelectedRowEditor.setValue(script);
     }

     initonOnRowAddedEditor(script = "") {
          this.onRowAddedEditor = ace.edit('scriptOnRowAdded');
          this.onRowAddedEditor.setTheme('ace/theme/chrome');
          this.onRowAddedEditor.session.setMode('ace/mode/javascript');

          // تغییر textarea → ذخیره در state + بروزرسانی badge
          this.onRowAddedEditor.getSession().on('change', (e) => {
               const value = this.onRowAddedEditor.getValue()
               this.eventScripts['onRowAdded'] = value;
               this._refreshEventBadge("onRowAdded");
          });
         

          this.onRowAddedEditor.setValue(script);
     }

     initSqlEditor(query = "") {
          this.sqlEditor = ace.edit('queryEditorWrite');
          this.sqlEditor.setTheme('ace/theme/chrome');
          this.sqlEditor.session.setMode('ace/mode/sqlserver');
          this.sqlEditor.setValue(query);
     }

     initDraggableForColumns() {
          const container = document.getElementById('columnsTableBody');
          if (!container || container._draggableInitialized) return;

          const sortable = new Draggable.Sortable(container, {
               draggable: 'tr',
               handle: '[data-handler="true"]',
               mirror: {
                    constrainDimensions: true,
                    // ایجاد placeholder در حین جابجایی
                    appendTo: 'body',
                    element: (sourceElement) => {
                         
                         const clone = sourceElement.cloneNode(true);
                         clone.style.width = `${sourceElement.offsetWidth}px`;
                         clone.style.backgroundColor = 'red';
                         clone.style.opacity = '0.8';
                         return clone;
                    }
               },
               classes: {
                    'source:dragging': 'dragging-source',
                    'mirror': 'dragging-mirror'
               }
          });

          sortable.on('mirror:attached', (event) => {
               event.mirror.innerHTML = `<div style="font-size:24px;text-align:center">${$(event.mirror.innerHTML).find('[data-field="displayName"]').val()}</div>`
                
          })

          // رویداد پایان جابجایی
          sortable.on('sortable:stop', (event) => {
          
               const oldIndex = event.oldIndex;
               const newIndex = event.newIndex;
               if (oldIndex !== undefined && newIndex !== undefined && oldIndex !== newIndex) {
                    const [movedItem] = this.columns.splice(oldIndex, 1);
                    this.columns.splice(newIndex, 0, movedItem);
                    this.updateColumnsTable();
                    this.refreshQuery();
               }
          });

          container._draggableInitialized = true;
     }

     initDraggableForWritedColumns() {
          const container = document.getElementById('writeModeColumnsTableBody');
          if (!container || container._draggableInitialized) return;

          const sortable = new Draggable.Sortable(container, {
               draggable: 'tr',
               handle: '[data-handler="true"]',
               mirror: {
                    constrainDimensions: true,
                    // ایجاد placeholder در حین جابجایی
                    appendTo: 'body',
                    element: (sourceElement) => {
                    
                         const clone = sourceElement.cloneNode(true);
                         clone.style.width = `${sourceElement.offsetWidth}px`;
                         clone.style.backgroundColor = 'red';
                         clone.style.opacity = '0.8';
                         return clone;
                    }
               },
               classes: {
                    'source:dragging': 'dragging-source',
                    'mirror': 'dragging-mirror'
               }
          });

          sortable.on('mirror:attached', (event) => {
               event.mirror.innerHTML = `<div style="font-size:24px;text-align:center">${$(event.mirror.innerHTML).find('[data-field="displayName"]').val()}</div>`
 

          })

          
          // رویداد پایان جابجایی
          sortable.on('sortable:stop', (event) => {
           
       
               const oldIndex = event.oldIndex;
               const newIndex = event.newIndex;
               if (oldIndex !== undefined && newIndex !== undefined && oldIndex !== newIndex) {
                    const [movedItem] = this._writeModeColumns.splice(oldIndex, 1);
                    this._writeModeColumns.splice(newIndex, 0, movedItem);
                    this.updateWriteModeColumnsTable();
                 
               }
 
          
          });

          container._draggableInitialized = true;
     }


     loadActionOptions() {
          if (this.actionOptions.length == 0) {
               this.actionOptions = this.actionOptionsDefualt;
          }
         

          this.actionOptions.forEach(c => {
               var checked = c.enable == true ? "checked":"" 

               let $tplOption = $(`<div class='col-md-3 align-content-center' >
			                    <div class="form-check">
				                    <input  class="form-check-input" type="checkbox" asp-for='${c.dataActionName}' ${checked} title="${c.title}" />
				                         <label class="form-check-label" asp-for='${c.dataActionName}'>
					                    ${c.title}
				                    </label>
			                    </div>
		                    </div>
                              `)

               let actionOptions = this.actionOptions;

               $tplOption.find("input").change(function () {
                     
                    var actionName = $(this).attr("asp-for");
                    if (!actionName) return;

                    if (actionOptions.any(c => c.dataActionName == actionName)) {
                         actionOptions.filter(c => c.dataActionName == actionName)[0].enable = this.checked
                    }
                    else {
                         actionOptions.push(
                              {
                                   dataActionName: actionName,
                                   enable: this.checked ,
                                   title: $(this).attr("title")
                              }
                         )
                    }


               })
               $("#actionOptionsSection").append($tplOption);

          })

        
     }

     initMode() {
          const urlMode = new URLSearchParams(window.location.search).get('mode');
          const storedMode = sessionStorage.getItem('queryDesignerMode');
          
             //  urlMode || storedMode || (window.__reportMode || null);

          if (this.mode != null || this.mode != undefined) {
               $('#modeSelection').addClass('d-none');
               if (this.mode === 0) {
                    $('#designModeContent').removeClass('d-none');
                    $('#writeModeContent').addClass('d-none');
                    // TODO: if (user.IsAdmin) { $('#queryEditor').prop('readonly', false); }
                    $('#queryEditor').prop('readonly', true);
               } else if (this.mode === 1) {
                    $('#designModeContent').addClass('d-none');
                    $('#writeModeContent').removeClass('d-none');
               }
          } else {
               $('#modeSelection').removeClass('d-none');
               $('#designModeContent').addClass('d-none');
               $('#writeModeContent').addClass('d-none');
          }
     }

     applyMode(mode) {
          if (mode === undefined)
               return;
               
          this.mode = mode;
          sessionStorage.setItem('queryDesignerMode', mode);
          $('#modeSelection').addClass('d-none');
          if (mode === 'design' || mode === 0) {
               $('#designModeContent').removeClass('d-none');
               $('#writeModeContent').addClass('d-none');
               // TODO: if (user.IsAdmin) { $('#queryEditor').prop('readonly', false); }
               $('#queryEditor').prop('readonly', true);
          } else if (mode === 'write' || mode === 1) {
               $('#designModeContent').addClass('d-none');
               $('#writeModeContent').removeClass('d-none');
          }
     }

     loadEventScripts() {
          if (!this.eventScripts) {
               this.eventScripts = { onSelectedRow: '', onRowAdded: '' };
          }

 
          this.onSelectedRowEditor.setValue(this.eventScripts.onSelectedRow || '');
          $('#scriptOnRowAdded').val(this.eventScripts.onRowAdded || '');

          this._refreshEventBadge('onSelectedRow');
          this._refreshEventBadge('onRowAdded');
     }

     /**
 * اتصال رویدادهای UI – دکمه‌های قالب / پاک کردن و تغییر textarea
 * @private
 */
     _bindEventScriptEditors() {
        

          // دکمه درج قالب
          $(document).on('click', '[data-action="insertTemplate"][data-event]', (e) => {
               const eventName = $(e.currentTarget).data('event');
               this._insertEventTemplate(eventName);
          });

          // دکمه پاک کردن
          $(document).on('click', '[data-action="clearScript"][data-event]', (e) => {
               const eventName = $(e.currentTarget).data('event');
               this._clearEventScript(eventName);
          });
     }

     /**
      * اعتبارسنجی هر دو اسکریپت رویداد قبل از ذخیره
      * @returns {{ valid: boolean, error: string }}
      */
     validateEventScripts() {
          const checks = [
               { key: 'onSelectedRow', label: 'onSelectedRow' },
               { key: 'onRowAdded', label: 'onRowAdded' },
          ];

          for (const { key, label } of checks) {
               const script = (this.eventScripts[key] || '').trim();
               if (!script) continue; // اسکریپت خالی مجاز است

               try {
                    // eslint-disable-next-line no-new-func
                    new Function('ctx', `return (${script})(ctx)`);
               } catch (e) {
                    return {
                         valid: false,
                         error: `خطای syntax در رویداد ${label}: ${e.message}`,
                    };
               }
          }

          return { valid: true, error: '' };
     }

     /**
      * بروزرسانی badge نشان‌دهنده وجود اسکریپت
      * @param {string} eventName
      * @private
      */
     _refreshEventBadge(eventName) {
          const hasScript = !!(this.eventScripts[eventName] || '').trim();
          $(`#badge-${eventName}`).toggleClass('d-none', !hasScript);
     }

     /**
      * درج قالب پیش‌فرض برای هر رویداد
      * @param {string} eventName
      * @private
      */
     _insertEventTemplate(eventName) {
           
          const $textarea = ace.edit($(`#script${_capitalize(eventName)}`)[0]);
          const current = $textarea.getValue();

          if (current && !confirm('محتوای فعلی جایگزین می‌شود. ادامه می‌دهید؟')) return;

          $textarea.setValue(this._getEventTemplate(eventName)) 
     }

     /**
      * پاک کردن اسکریپت یک رویداد
      * @param {string} eventName
      * @private
      */
     _clearEventScript(eventName) {
           
          if (!(this.eventScripts[eventName] || '').trim()) return;

          if (!confirm('اسکریپت این رویداد پاک می‌شود. ادامه می‌دهید؟')) return;

          const $textarea = ace.edit($(`#script${_capitalize(eventName)}`)[0]);
          $textarea.setValue("");               ;
      
     }

     /**
      * قالب‌های پیش‌فرض هر رویداد
      * @param {string} eventName
      * @returns {string}
      * @private
      */
     _getEventTemplate(eventName) {
          const templates = {

               onSelectedRow: `function(ctx) {
    // ctx.selectedRow  → آبجکت داده ردیف انتخاب‌شده (null هنگام deselect)
    // ctx.$row         → jQuery المان <tr>
    // ctx.table        → DataTable API
    // ctx.$toolbar     → jQuery نوار ابزار
    // ctx.isDeselect   → true اگر ردیف deselect شد
    // ctx.draw()       → بارگذاری مجدد جدول

    if (ctx.isDeselect) {
        // ردیف deselect شد
        return;
    }

    var row = ctx.selectedRow;
    console.log('ردیف انتخاب شد:', row);

    // مثال: رنگ‌دهی ردیف
    // ctx.$row.addClass('table-warning');
}`,

               onRowAdded: `function(ctx) {
    // ctx.rowData   → آبجکت داده ردیف
    // ctx.$row      → jQuery المان <tr>
    // ctx.rowIndex  → ایندکس ردیف در صفحه جاری
    // ctx.table     → DataTable API
    // ctx.draw()    → بارگذاری مجدد جدول
    //
    // ⚠ این تابع برای هر ردیف در هر draw اجرا می‌شود.
    //   از عملیات سنگین یا درخواست‌های شبکه‌ای خودداری کنید.

    var data = ctx.rowData;
    var $row = ctx.$row;

    // مثال: رنگ‌دهی شرطی
    // if (data.status === 'inactive') {
    //     $row.addClass('table-danger');
    // }

    // مثال: فرمت‌دهی یک سلول
    // var cellIdx = 2;
    // $row.find('td').eq(cellIdx).css('font-weight', 'bold');
}`,
          };

          return templates[eventName] || `function(ctx) {\n    // کد رویداد ${eventName}\n}`;
     }

    

     async loadSystemEnums() {
            
          try {
               const response = await $.get("/System/ListSystemEnums");
               if (response.isSuccess) {
                    this.systemEnums = response.data || [];
               }
          } catch (error) {
               error2("خطا در بارگذاری enum های سیستم");
               console.error(error);
          }
 
     }

     initJsPlumb() {
          this.jsPlumbInstance = jsPlumb.getInstance({
               Container: 'diagramCanvas',
               Connector: ['Flowchart', {
                    stub: 40,
                    gap: 0,
                    cornerRadius: 5,
                    alwaysRespectStubs: true,
                    midpoint: 0.5
               }],
               ConnectionOverlays: [
                    ['Arrow', {
                         location: 1,
                         width: 12,
                         length: 12,
                         foldback: 0.8
                    }],
                    ['Label', {
                         location: 0.5,
                         cssClass: 'jtk-overlay',
                         label: 'INNER',
                         id: 'label'
                    }]
               ],
               PaintStyle: {
                    strokeWidth: 3,
                    stroke: '#dc3545'
               },
               HoverPaintStyle: {
                    strokeWidth: 4,
                    stroke: '#c82333'
               },
               EndpointStyle: {
                    fill: '#0d6efd',
                    radius: 6,
                    outlineStroke: 'white',
                    outlineWidth: 2
               },
               EndpointHoverStyle: {
                    fill: '#0a58ca',
                    radius: 7,
                    outlineWidth: 3
               },
               ConnectionsDetachable: true,
               ReattachConnections: false
          });

          this.jsPlumbInstance.bind('click', (conn) => {
               this.onConnectionClick(conn);
          });

          this.jsPlumbInstance.bind('dblclick', (conn) => {
                
               this.onConnectionDoubleClick(conn);
          });

          this.jsPlumbInstance.bind('connection', (info) => {
               this.onConnectionCreated(info);
          });

          this.jsPlumbInstance.bind('connectionDetached', (info) => {
               this.onConnectionDetached(info);
          });
     }

     bindEvents() {
          $('.mode-card').on('click', (e) => {

               const mode = $(e.currentTarget).data('mode');
               this.applyMode(mode); 
          });
          $('#btnChangeMode').on('click', () => {
               sessionStorage.removeItem('queryDesignerMode');
               //TODO RefreshPage
          });
          $('#btnValidateQueryWrite').on('click', () => this.validateQueryWrite());
          $('#btnPreviewQueryWrite').on('click', () => this.previewQueryWrite());
          $('#btnSaveReportWrite').on('click', () => this.saveReportWrite());
          $('#queryEditorWrite').on('input', () => {
               if (this.mode === 1) this._writeModePreviewed = false;
          });

          $('#tableSearch').on('input', (e) => this.filterTables(e.target.value));
          $('#spSearch').on('input', (e) => this.filterStoredProcedures(e.target.value));
          $('#btnClearDiagram').on('click', () => this.clearDiagram());
          $('#btnZoomIn').on('click', () => this.setZoom(this.zoom + 0.1));
          $('#btnZoomOut').on('click', () => this.setZoom(this.zoom - 0.1));
          $('#btnFitView').on('click', () => this.setZoom(1));
          $('#btnValidateQuery').on('click', () => this.validateQuery());
          $('#btnPreviewQuery').on('click', () => this.previewQuery());
          $('#btnSaveReport').on('click', () => this.saveReport());
          $('#btnCancel').on('click', () => {

          });
          $('#filterOperator').on('change', (e) => {
               const needsNoValue = ['isnull', 'isnotnull'].includes(e.target.value);
               $('#filterValueGroup').toggle(!needsNoValue);
               const isInOperator = ['in', 'notin'].includes(e.target.value);
               $('#filterValue').attr('placeholder', isInOperator ? 'مقادیر جدا شده با کاما (مثال: 1,2,3 یا \'a\',\'b\')' : 'مقدار فیلتر را وارد کنید');
          });
          $('#btnSaveFilter').on('click', () => this.saveFilter());
          $('#btnAddParameter').on('click', () => this.showParameterModal());
          $('#btnSaveParameter').on('click', () => this.saveParameter());
          $('#reportName').on('blur', () => this.checkNameUnique());

          $('#btnAddCustomButton').on('click', () => this.showCustomButtonModal());
          $('#btnSaveCustomButton').on('click', () => this.saveCustomButton());
          $('#customBtnUseHtml').on('change', () => this._toggleCustomButtonMode());
          $('#btnInsertScriptTemplate').on('click', () => this._insertScriptTemplate());
          $('#customBtnColorClass, #customBtnIconClass').on('change input', () => this._updateButtonPreview());


           $('#btnAddColumnManual').on('click', () => this.showManualColumnModal());
 
           $('#btnSaveManualColumn').on('click', () => this.saveManualColumn());
           $('#manualColTable').on('change', () => this._onManualColTableChange());
           $('#btnInsertColRenderTemplate').on('click', () => this._insertColRenderTemplate());
           $(document).on('click', '.col-type-card', (e) => this._selectColType($(e.currentTarget)));

          $(document).on('keydown', (e) => {
               if (e.key === 'Delete' && this.selectedConnection) {
                    this.jsPlumbInstance.deleteConnection(this.selectedConnection);
                    this.selectedConnection = null;
               }
               if (e.key === 'Escape' && this.firstClickedEndpoint) {
                    this.resetEndpointSelection();
                    this.showInfo('انتخاب لغو شد');
               }
          });

          const diagramContainer = document.querySelector('.diagram-container');
          if (diagramContainer) diagramContainer.addEventListener('wheel', (e) => {
               if (e.ctrlKey) {
                    e.preventDefault();
                    const delta = e.deltaY > 0 ? -0.1 : 0.1;
                    this.setZoom(this.zoom + delta);
               }
          }, { passive: false });

          this._bindEventScriptEditors();
     }

     /**
      * نمایش Modal افزودن / ویرایش ستون دستی
      * @param {Object|null} col  – اگر null باشد حالت افزودن، وگرنه ویرایش
      */
     showManualColumnModal(col = null) {
          // ── reset فرم ─────────────────────────────────────────────────────────
          $('#manualColEditId').val(col?.id ?? '');
          $('#manualColumnModalTitle').text(col ? 'ویرایش ستون' : 'افزودن ستون');

          const colType = col?.customColType ?? 'data';
          this._selectColType($(`.col-type-card[data-col-type="${colType}"]`));

          $('#manualColDisplayName').val(col?.displayName ?? '');
          $('#manualColVisible').prop('checked', col?.visible !== false);
          $('#manualColSystemType').val(col?.systemType ?? 'String');
          $('#manualColRender').val(col?.render ?? '');

          // ── data section ──────────────────────────────────────────────────────
          this._populateManualColTables();
          if (col?.tableRef) {
               $('#manualColTable').val(col.tableRef);
               this._onManualColTableChange();
               setTimeout(() => $('#manualColColumn').val(col.columnName ?? ''), 50);
          } else {
               $('#manualColTable').val('');
               $('#manualColColumn').prop('disabled', true).html('<option value="">-- ابتدا جدول را انتخاب کنید --</option>');
          }

          // ── button section ────────────────────────────────────────────────────
          $('#btnColColorClass').val(col?.btnConfig?.colorClass ?? 'btn btn-sm btn-primary');
          $('#btnColIcon').val(col?.btnConfig?.icon ?? '');
          $('#btnColText').val(col?.btnConfig?.text ?? col?.displayName ?? '');

          // ── input section ─────────────────────────────────────────────────────
          $('#inputColType').val(col?.inputConfig?.type ?? 'text');
          $('#inputColClass').val(col?.inputConfig?.cssClass ?? 'form-control form-control-sm');
          $('#inputColDataAttr').val(col?.inputConfig?.dataAttr ?? '');

          // ── html section ──────────────────────────────────────────────────────
          $('#manualColHtml').val(col?.htmlTemplate ?? '');

          new bootstrap.Modal(document.getElementById('manualColumnModal')).show();
     }

     /**
      * پر کردن select جداول در Modal
      * @private
      */
     _populateManualColTables() {
          const $sel = $('#manualColTable');
          $sel.html('<option value="">-- بدون لینک (ستون مجازی) --</option>');
          this.selectedTables.forEach(t => {
               $sel.append(
                    `<option value="${t.boxId}">${t.displayName || t.name} (${t.schema}.${t.name})</option>`
               );
          });
     }

     /**
      * تغییر جدول انتخاب‌شده → بارگذاری ستون‌هایش در select
      * @private
      */
     _onManualColTableChange() {
          const boxId = $('#manualColTable').val();
          const $colSel = $('#manualColColumn');

          if (!boxId) {
               $colSel.prop('disabled', true).html('<option value="">--</option>');
               return;
          }

          const table = this.selectedTables.find(t => t.boxId === boxId);
          if (!table) return;

          $colSel.prop('disabled', false).html('<option value="">-- ستون --</option>');
          (table.columns || []).forEach(col => {
               $colSel.append(
                    `<option value="${col.columnName}"
                     data-type="${col.mappedSystemType ?? ''}"
                     data-dbtype="${col.dataType ?? ''}">
                ${col.displayName || col.columnName} (${col.columnName})
            </option>`
               );
          });

          // وقتی ستون تغییر کرد نوع سیستمی را auto-fill کن
          $colSel.off('change.autoType').on('change.autoType', () => {
               const $opt = $colSel.find(':selected');
               if (!$opt.val()) return;
               const mapped = this.mapDbTypeToSystemType(
                    $opt.data('type'),
                    $opt.data('dbtype')
               );
               $('#manualColSystemType').val(mapped);
          });
     }

     /**
      * انتخاب نوع ستون در Modal
      * @param {jQuery} $card
      * @private
      */
     _selectColType($card) {
          if (!$card.length) return;
          $('.col-type-card').removeClass('active');
          $card.addClass('active');

          const type = $card.data('col-type');
          $('#manualColType').val(type);

          // نمایش section مربوطه
          $('.col-section').addClass('d-none');
          $(`#section${type.charAt(0).toUpperCase() + type.slice(1)}`).removeClass('d-none');

          // برای انواع غیر از data نوع سیستمی را مخفی کنیم
          const showSysType = type === 'data';
          $('#manualColSystemType').closest('.col-md-4').toggleClass('d-none', !showSysType);
     }

     /**
      * درج قالب پیش‌فرض تابع render
      * @private
      */
     _insertColRenderTemplate() {
          const colType = $('#manualColType').val();
          const templates = {
               data: `function(data, type, row, meta) {\n    return data ?? '';\n}`,
               button: `function(data, type, row, meta) {\n    var id = row.id ?? row.Id ?? '';\n    return '<a href=\"/edit/' + id + '\" class=\"btn btn-sm btn-primary\"><i class=\"fa fa-edit\"></i></a>';\n}`,
               input: `function(data, type, row, meta) {\n    return '<input type=\"text\" class=\"form-control form-control-sm col-input\" data-id=\"' + row.id + '\" value=\"' + (data ?? '') + '\">';\n}`,
               html: `function(data, type, row, meta) {\n    return '<span class=\"badge bg-primary\">' + (data ?? '') + '</span>';\n}`,
          };
          const cur = $('#manualColRender').val().trim();
          if (!cur || confirm('محتوای فعلی جایگزین می‌شود. ادامه می‌دهید؟')) {
               $('#manualColRender').val(templates[colType] ?? templates.data);
          }
     }

     /**
      * ذخیره ستون دستی (افزودن یا ویرایش)
      */
     saveManualColumn() {
          const editId = $('#manualColEditId').val();
          const colType = $('#manualColType').val();
          const displayName = $('#manualColDisplayName').val().trim();
          const visible = $('#manualColVisible').is(':checked');
          const render = $('#manualColRender').val().trim() || null;

          // ── اعتبارسنجی ────────────────────────────────────────────────────────
          if (!displayName) { this.showError('عنوان نمایشی الزامی است'); return; }

          if (render) {
               try { new Function('data,type,row,meta', `return (${render})(data,type,row,meta)`); }
               catch (e) { this.showError(`خطای syntax در render: ${e.message}`); return; }
          }

          // ── ساخت مدل ستون ─────────────────────────────────────────────────────
          const tableBoxId = $('#manualColTable').val() || null;
          const columnName = $('#manualColColumn').val() || null;
          const tableInfo = tableBoxId ? this.selectedTables.find(t => t.boxId === tableBoxId) : null;
          const tableName = tableInfo ? `${tableInfo.schema}.${tableInfo.name}` : '';

          // alias و alliance
          const alias = tableBoxId ? this.resolveAlias(tableBoxId) : null;
          const uniqueSuffix = editId || this.generateId();      // برای چند نمونه از یک ستون
          const alliance = alias && columnName
               ? `${alias}_${columnName}_${uniqueSuffix}`        // چند نمونه از یک ستون → کلید یکتا
               : `custom_${uniqueSuffix}`;
          const address = alias && columnName
               ? `[${alias}].[${columnName}]`
               : null;

          const col = {
               // شناسه یکتا برای مدیریت داخلی
               id: uniqueSuffix,
               // فلگ ستون دستی
               isCustom: true,
               // نوع ستون دستی
               customColType: colType,
               // فیلدهای استاندارد
               tableRef: tableBoxId,
               tableName: tableName,
               columnName: columnName,
               address: address,
               alliance: alliance,
               name: columnName,
               displayName: displayName,
               systemType: colType === 'data' ? ($('#manualColSystemType').val() || 'String') : 'String',
               visible: visible,
               primaryKey: false,
               sortDirection: null,
               sortOrder: null,
               groupBy: false,
               aggregate: null,
               render: render,
               optionSetting: null,
               // تنظیمات خاص هر نوع
               btnConfig: colType === 'button' ? {
                    colorClass: $('#btnColColorClass').val(),
                    icon: $('#btnColIcon').val().trim(),
                    text: $('#btnColText').val().trim(),
               } : null,
               inputConfig: colType === 'input' ? {
                    type: $('#inputColType').val(),
                    cssClass: $('#inputColClass').val().trim(),
                    dataAttr: $('#inputColDataAttr').val().trim(),
               } : null,
               htmlTemplate: colType === 'html' ? $('#manualColHtml').val().trim() : null,
          };

          // render پیش‌فرض برای button/input/html اگر render خالی باشد
          if (!col.render) {
               col.render = this._buildDefaultRender(col);
          }

          // ── افزودن یا ویرایش ──────────────────────────────────────────────────
          const existingIdx = editId
               ? this.columns.findIndex(c => c.id === editId)
               : -1;

          if (existingIdx >= 0) {
               // در ویرایش alliance و id را حفظ کن
               col.id = this.columns[existingIdx].id;
               col.alliance = this.columns[existingIdx].alliance;
               this.columns[existingIdx] = col;
               this.showSuccess('ستون بروزرسانی شد');
          } else {
               this.columns.push(col);
               this.showSuccess('ستون اضافه شد');
          }

          bootstrap.Modal.getInstance(document.getElementById('manualColumnModal'))?.hide();
          this.updateColumnsTable();
          this.refreshQuery();
     }

     /**
      * ساخت تابع render پیش‌فرض بر اساس نوع ستون دستی
      * @param {Object} col
      * @returns {string}
      * @private
      */
     _buildDefaultRender(col) {
          switch (col.customColType) {
               case 'button': {
                    const cfg = col.btnConfig || {};
                    const cls = cfg.colorClass || 'btn btn-sm btn-primary';
                    const icon = cfg.icon ? `<i class="${cfg.icon}"></i> ` : '';
                    const text = cfg.text || col.displayName || 'اکشن';
                    return `function(data, type, row, meta) {
    return '<button class="${cls} dt-custom-btn" data-id="' + (row.id ?? row.Id ?? '') + '" data-col="${col.alliance}">${icon}${text}</button>';
}`;
               }
               case 'input': {
                    const cfg = col.inputConfig || {};
                    const tp = cfg.type || 'text';
                    const cls = cfg.cssClass || 'form-control form-control-sm';
                    const da = cfg.dataAttr ? ` ${cfg.dataAttr}` : '';
                    return `function(data, type, row, meta) {
    return '<input type="${tp}" class="${cls} dt-custom-input" value="' + (data ?? '') + '" data-id="' + (row.id ?? row.Id ?? '') + '"${da}>';
}`;
               }
               case 'html': {
                    const tpl = (col.htmlTemplate || '{value}')
                         .replace(/`/g, '\\`');
                    return `function(data, type, row, meta) {
    var value = data ?? '';
    return \`${tpl}\`.replace(/{value}/g, value).replace(/{row\\.([^}]+)}/g, function(m, f){ return row[f] ?? row[f.charAt(0).toUpperCase()+f.slice(1)] ?? ''; });
}`;
               }
               default:
                    return `function(data, type, row, meta) { return data ?? ''; }`;
          }
     }

     /**
      * حذف ستون از جدول ستون‌ها
      * اگر به یک ستون دیاگرام لینک بود، تیکش را برمی‌دارد
      * @param {number} idx – ایندکس در this.columns
      */
     removeColumn(idx) {
          const col = this.columns[idx];
          if (!col) return;

          // ── اگر به ستون دیاگرام لینک شده → تیک را بردار ──────────────────────
          if (col.tableRef && col.columnName && !col.isCustom) {
               // پیدا کردن جدول مربوطه
               const table = this.selectedTables.find(t => t.boxId === col.tableRef);
               if (table) {
                    const tblCol = table.columns.find(c => c.columnName === col.columnName);
                    if (tblCol) {
                         // بررسی: آیا ستون دیگری با همین tableRef+columnName هنوز وجود دارد؟
                         const otherExists = this.columns.some(
                              (c, i) => i !== idx && c.tableRef === col.tableRef && c.columnName === col.columnName
                         );
                         if (!otherExists) {
                              tblCol.isSelected = false;
                              $(`#${col.tableRef} [data-column-name="${col.columnName}"] .column-checkbox`)
                                   .prop('checked', false);
                         }
                    }
               }
          }

          this.columns.splice(idx, 1);
          this.updateColumnsTable();
          this.refreshQuery();
          this.showInfo('ستون حذف شد');
     }


     async loadTablesAndViews() {
          try {
               const response = await $.get('GetTablesAndViews');
               if (response.isSuccess) {
                    this.tables = response.data;
                    this.renderTablesList();
               } else {
                    this.showError('خطا در بارگذاری جداول');
               }
          } catch (error) {
               this.showError('خطا در ارتباط با سرور');
               console.error(error);
          }
     }

     async loadStoredProcedures() {
          try {
               const response = await $.get('GetStoredProcedures');
               if (response.isSuccess) {
                    this.storedProcedures = response.data || [];
                    this.renderStoredProceduresList();
               }
          } catch (error) {
               $('#storedProceduresList').html('<div class="text-danger small">خطا در بارگذاری</div>');
               console.error(error);
          }
     }

     renderStoredProceduresList() {
          const $list = $('#storedProceduresList');
          $list.empty();
          if (!this.storedProcedures?.length) {
               $list.html('<div class="text-muted text-center py-2">SP یافت نشد</div>');
               return;
          }
          this.storedProcedures.forEach(sp => {
               const fullName = `${sp.schema}.${sp.name}`;
               const $item = $(`
                    <div class="sp-list-item py-1 px-2 rounded cursor-pointer" data-schema="${sp.schema}" data-name="${sp.name}" title="کلیک برای درج">
                         <i class="bi bi-gear text-secondary"></i> ${fullName}
                    </div>
               `);
               $item.on('click', () => this.insertStoredProcedureToEditor(sp.schema, sp.name));
               $list.append($item);
          });
     }

     filterStoredProcedures(term) {
          const t = (term || '').toLowerCase();
          $('#storedProceduresList .sp-list-item').each(function () {
               const full = $(this).text().toLowerCase();
               $(this).toggle(!t || full.includes(t));
          });
     }

     async insertStoredProcedureToEditor(schema, procedureName) {
          try {
               const response = await $.get('GetStoredProcedureParameters', { schema, procedureName });
               if (!response.isSuccess) {
                    this.showError('خطا در دریافت پارامترها');
                    return;
               }
               const params = response.data || [];
               const inputParams = params.filter(p => !p.isOutput);
               const paramNames = inputParams.map(p => p.name.startsWith('@') ? p.name : `@${p.name}`);
               const execQuery = `EXEC [${schema}].[${procedureName}] ${paramNames.join(', ')}`;

               this.sqlEditor.setValue(execQuery);
          
               this._writeModePreviewed = false;
               this.showInfo('درج شد');
          } catch (error) {
               this.showError('خطا در درج');
               console.error(error);
          }
     }

     renderTablesList() {
          const $list = $('#tablesList');
          $list.empty();

         this.entityFullName = $("#entityFullName").val()
          this.tables.forEach(table => {
               const icon = table.type === 'Table' ? 'bi-table' : 'bi-eye';
               const badge = table.type === 'Table' ? 'primary' : 'info';
               const displayName = table?.displayName ?? "-";
               const $item = $(`
                <div class="table-list-item" 
                     data-table="${table.name}" 
                     data-schema="${table.schema}"
                     data-type="${table.type}"
                     data-entityFullName="${table?.entityFullName}",
                     data-hasEntity="${table.hasEntity}"
                     data-displayName="${table?.displayName}"
                     draggable="true">
                    <i class="bi ${icon} table-icon"></i>
                   <div style="display: flex;flex-direction: column;align-items: center; overflow: hidden;">
                    <span class="table-displayName">${displayName}</span>
                    <span class="table-name">${table.schema}.${table.name}</span>
                   </div>
                    <span class="badge bg-${badge} float-end text-white">${table.type}</span>
                </div>
            `);

               $item.on('dragstart', (e) => {
                    e.originalEvent.dataTransfer.setData('table', JSON.stringify({
                         name: table.name,
                         schema: table.schema,
                         type: table.type,
                         entityFullName: table.entityFullName,
                         hasEntity: table.hasEntity
                    }));
               });

               $item.on('dblclick', () => {
                    this.addTableToDiagram(table.name, table.schema, table.displayName, table.entityFullName, table.hasEntity);
               });
                
               if (this.entityFullName && !window.__reportId && table.entityFullName && table.entityFullName.toLowerCase() === this.entityFullName.toLowerCase()) {
                    $item.trigger("dblclick");
               }

               $list.append($item);
          });

          const canvas = document.getElementById('diagramCanvas');
          canvas.addEventListener('dragover', (e) => {
               e.preventDefault();
          });

          canvas.addEventListener('drop', (e) => {
               e.preventDefault();
               const tableData = JSON.parse(e.dataTransfer.getData('table'));
               const rect = canvas.getBoundingClientRect();
               const x = (e.clientX - rect.left) / this.zoom;
               const y = (e.clientY - rect.top) / this.zoom;
               this.addTableToDiagram(tableData.name, tableData.schema, tableData.displayName, tableData.entityFullName, tableData.hasEntity, x, y);
          });
     }

     filterTables(searchTerm) {
          const term = searchTerm.toLowerCase();
          $('#tablesList .table-list-item').each(function () {
               const tableName = $(this).data('table').toLowerCase();
               const schema = $(this).data('schema').toLowerCase();
               const displayName = $(this).data('displayname')
               const fullName = `${schema}.${tableName}`;
               if (fullName.includes(term) || (displayName && displayName.toLowerCase().includes(term))) {
                    $(this).show();
               } else {
                    $(this).hide();
               }
          });
     }

     /**
      * تولید شناسه یکتا برای هر instance جدول در دیاگرام
      */
     getTableBoxId(tableInfo) {
          const fullName = `${tableInfo.schema}.${tableInfo.name}`;
          const safeName = fullName.replace(/\./g, '-');
          return tableInfo.instanceId ? `table-${safeName}-${tableInfo.instanceId}` : `table-${safeName}`;
     }

     async addTableToDiagram(tableName, schema = 'dbo', displayName, entityFullName, hasEntity, x = null, y = null, skipAutoRelations = true, existingInstanceId = null) {
          try {
               if (!entityFullName || entityFullName.length == 0) {
               entityFullName =  this.tables.firstOrDefault(c => c.schema == schema && c.name == tableName)?.entityFullName
          }
               const response = await $.get('GetTableColumns', {
                    tableName: tableName,
                    schema: schema,
                    entityFullName
               });

               if (!response.isSuccess) {
                    this.showError('خطا در بارگذاری ستون‌های جدول');
                    return;
               }

               const columns = response.data;
               const tableInfo = {
                    instanceId: existingInstanceId || this.generateId(),
                    name: tableName,
                    schema: schema,
                    displayName,
                    entityFullName,
                    hasEntity,
                    type: this.tables.find(t => t.name === tableName && t.schema === schema)?.type || 'Table',
                    positionX: x || (100 + this.selectedTables.length * 50),
                    positionY: y || (100 + this.selectedTables.length * 50),
                    columns: columns
               };
               tableInfo.boxId = this.getTableBoxId(tableInfo);

               this.selectedTables.push(tableInfo);
               this.renderTableBox(tableInfo);
               $('.empty-state').hide();
               if (!skipAutoRelations) await this.loadAutoRelations(tableName, schema);
               this.refreshQuery();

          } catch (error) {
               this.showError('خطا در افزودن جدول');
               console.error(error);
          }
     }

     renderTableBox(tableInfo) {
          const template = document.getElementById('tableBoxTemplate');
          const $box = $(template.content.cloneNode(true));

          const boxId = tableInfo.boxId || this.getTableBoxId(tableInfo);
          $box.find('.table-box')
               .attr('id', boxId)
               .attr('data-table-name', tableInfo.name)
               .attr('data-schema', tableInfo.schema)
               .attr('data-entityfullname', tableInfo.entityFullName)
               .attr('data-hasentity', tableInfo.hasEntity)
               .css({
                    left: tableInfo.positionX + 'px',
                    top: tableInfo.positionY + 'px'
               });
 

          const sameTables = this.selectedTables.filter(t => `${t.schema}.${t.name}` === `${tableInfo.schema}.${tableInfo.name}`);
          const instanceNum = sameTables.findIndex(t => t.boxId === tableInfo.boxId) + 1;
          const displayLabel = sameTables.length > 1
               ? `${tableInfo.displayName || tableInfo.name} (${instanceNum})`
               : (tableInfo.displayName || tableInfo.name);
          $box.find('.table-displayname').text(displayLabel);
          $box.find('.table-name').text(`${tableInfo.schema}.${tableInfo.name}`);
          $box.find('.btn-close').on('click', () => {
               this.removeTable(boxId);
          });

          const $columnsList = $box.find('.columns-list');
          tableInfo.columns.forEach(column => {
               const $columnItem = this.renderColumnItem(column, tableInfo);
               $columnsList.append($columnItem);
          });

          $('#diagramCanvas').append($box);

          this.jsPlumbInstance.draggable(boxId, {
               handle: '.table-box-header',
               scroll: true,
               drag: (params) => {
                    this.jsPlumbInstance.revalidate(params.el);
               },
               stop: (params) => {
                    const table = this.selectedTables.find(t => t.boxId === boxId);
                    if (table) {
                         table.positionX = params.pos[0];
                         table.positionY = params.pos[1];
                    }
                    this.jsPlumbInstance.revalidate(params.el);
               }
          });

          this.addColumnEndpoints(boxId, tableInfo);
     }

     renderColumnItem(column, tableInfo) {
          const template = document.getElementById('columnItemTemplate');
          const $item = $(template.content.cloneNode(true));

          $item.find('.column-item')
               .attr('data-column-name', column.columnName)
               .attr('data-data-type', column.dataType)
               .attr('data-data-systemtype', column.mappedSystemType);

          if (column.isPrimaryKey) {
               $item.find('.column-item').addClass('pk');
          }
          if (column.isForeignKey) {
               $item.find('.column-item').addClass('fk');
          }
           
          $item.find('.column-name').text(column.columnName);
          $item.find('.column-displayname').text(column.displayName || "-");
          $item.find('.column-type').text(column.mappedSystemTypeName || column.dataType);
          //mappedSystemType

          const $checkbox = $item.find('.column-checkbox');
          $checkbox.on('change', (e) => {
               column.isSelected = e.target.checked;
               this.updateSelectedColumns();
          });

          $item.find('.btn-add-filter').on('click', () => {
               this.showFilterModal(tableInfo, column);
          });

           
          if (this.entityFullName && !window.__reportId &&
               tableInfo.entityFullName &&
               tableInfo.entityFullName.toLowerCase() === this.entityFullName.toLowerCase() &&
               column.columnName === "Id")
          {
            $checkbox.prop("checked",true).trigger("change");
          }

          return $item;
     }

     addColumnEndpoints(boxId, tableInfo) {
           
          const $box = $(`#${boxId}`);

          setTimeout(() => {
               $box.find('.column-item').each((index, element) => {
                    const $columnItem = $(element);
                    const columnName = $columnItem.data('column-name');

                    const $sourceEl = $columnItem.find('.column-endpoint-right');
                    const $targetEl = $columnItem.find('.column-endpoint-left');

                    if (!$sourceEl.length || !$targetEl.length) return;

                    const sourceUUID = `${boxId}-${columnName}-right`;
                    const targetUUID = `${boxId}-${columnName}-left`;

                    const endpointData = {
                         table: `${tableInfo.schema}.${tableInfo.name}`,
                         column: columnName,
                         boxId: boxId
                    };

                    $sourceEl.attr('id', sourceUUID).data('endpoint-info', { ...endpointData, uuid: sourceUUID, side: 'right' });
                    $targetEl.attr('id', targetUUID).data('endpoint-info', { ...endpointData, uuid: targetUUID, side: 'left' });

                    $sourceEl.on('click', (e) => {
                         e.stopPropagation();
                         this.onEndpointClick($sourceEl);
                    });

                    $targetEl.on('click', (e) => {
                         e.stopPropagation();
                         this.onEndpointClick($targetEl);
                    });
               });

               this.jsPlumbInstance.repaintEverything();
          }, 200);
     }

     /**
      * ایجاد اتصال با رنگ رندوم
      */
     onEndpointClick($endpointDiv) {
          const info = $endpointDiv.data('endpoint-info');

          if (!this.firstClickedEndpoint) {
               this.firstClickedEndpoint = { element: $endpointDiv, info: info };
               $endpointDiv.addClass('endpoint-selected');
               this.showInfo('اکنون روی ستون مقصد کلیک کنید');
          } else {
               if (this.firstClickedEndpoint.element[0] === $endpointDiv[0]) {
                    this.resetEndpointSelection();
                    return;
               }

               if (this.firstClickedEndpoint.info.boxId === info.boxId) {
                    this.showWarning('نمی‌توانید ستون‌های یک جدول را به هم وصل کنید');
                    this.resetEndpointSelection();
                    return;
               }

               const sourceBoxId = this.firstClickedEndpoint.info.boxId;
               const targetBoxId = info.boxId;
               const sourceTable = this.firstClickedEndpoint.info.table;
               const targetTable = info.table;

               const existingRelationsCount = this.relations.filter(r =>
                    (r.sourceBoxId === sourceBoxId && r.targetBoxId === targetBoxId) ||
                    (r.sourceBoxId === targetBoxId && r.targetBoxId === sourceBoxId)
               ).length;

               const dynamicStub = 30 + (existingRelationsCount * 20);
               const dynamicMidpoint = Math.min(0.8, 0.2 + (existingRelationsCount * 0.1));

               const sourceAnchor = this.firstClickedEndpoint.info.side === 'right' ? [1, 0.5, 1, 0] : [0, 0.5, -1, 0];
               const targetAnchor = info.side === 'right' ? [1, 0.5, 1, 0] : [0, 0.5, -1, 0];

               // انتخاب رنگ رندوم برای این خط
               const lineColor = this.getRandomColor();

               const conn = this.jsPlumbInstance.connect({
                    source: this.firstClickedEndpoint.element[0],
                    target: $endpointDiv[0],
                    anchors: [sourceAnchor, targetAnchor],

                    connector: ['Flowchart', {
                         stub: dynamicStub,
                         midpoint: dynamicMidpoint,
                         alwaysRespectStubs: true,
                         cornerRadius: 5,
                         gap: 2
                    }],

                    // اضافه کردن رنگ رندوم
                    paintStyle: {
                         strokeWidth: 3,
                         stroke: lineColor
                    },
                    hoverPaintStyle: {
                         strokeWidth: 4,
                         stroke: this.darkenColor(lineColor, 20)
                    },

                    parameters: {
                         relationId: this.generateId(),
                         sourceBoxId: sourceBoxId,
                         targetBoxId: targetBoxId,
                         sourceTable: sourceTable,
                         sourceColumn: this.firstClickedEndpoint.info.column,
                         targetTable: targetTable,
                         targetColumn: info.column,
                         lineColor: lineColor
                    }
               });

               if (conn) {
                    const relation = {
                         id: conn.getParameter('relationId'),
                         sourceBoxId: sourceBoxId,
                         targetBoxId: targetBoxId,
                         sourceTable: sourceTable,
                         sourceColumn: this.firstClickedEndpoint.info.column,
                         targetTable: targetTable,
                         targetColumn: info.column,
                         joinType: 'INNER',
                         lineColor: lineColor
                    };
                    this.relations.push(relation);

                    const overlay = conn.getOverlay('label');
                    if (overlay) {
                         overlay.setLabel(`INNER: ${relation.sourceColumn} ↔ ${relation.targetColumn}`);
                    }
                    this.refreshQuery();
               }

               this.resetEndpointSelection();
          }
     }

     resetEndpointSelection() {
          if (this.firstClickedEndpoint) {
               this.firstClickedEndpoint.element.removeClass('endpoint-selected');
               this.firstClickedEndpoint = null;
          }
     }

     async loadAutoRelations(tableName, schema) {
          try {
               const response = await $.get('GetTableRelations', {
                    tableName: tableName,
                    schema: schema
               });

               if (response.isSuccess && response.data.length > 0) {
                    response.data.forEach(relation => {
                         if (relation.sourceTable === relation.targetTable) return;
                         const sourceExists = this.selectedTables.find(t =>
                              `${t.schema}.${t.name}` === relation.sourceTable);
                         const targetExists = this.selectedTables.find(t =>
                              `${t.schema}.${t.name}` === relation.targetTable);
                         if (sourceExists && targetExists) this.addRelation(relation);
                    });
               }
          } catch (error) {
               console.error('Error loading relations:', error);
          }
     }

     /**
      * اضافه کردن relation - خط از سمت صحیح کشیده می‌شود:
      * جدول چپ: از دایره راست به دایره چپ جدول راست
      * جدول راست: از دایره چپ به دایره راست جدول چپ
      */
     addRelation(relation) {
          let sourceBoxId = relation.sourceBoxId;
          let targetBoxId = relation.targetBoxId;
          if (!sourceBoxId || !targetBoxId) {
               const srcTable = this.selectedTables.find(t => `${t.schema}.${t.name}` === relation.sourceTable);
               const tgtTable = this.selectedTables.find(t => `${t.schema}.${t.name}` === relation.targetTable);
               if (!srcTable || !tgtTable) return;
               sourceBoxId = sourceBoxId || srcTable.boxId;
               targetBoxId = targetBoxId || tgtTable.boxId;
          }

          const sourceTable = this.selectedTables.find(t => t.boxId === sourceBoxId);
          const targetTable = this.selectedTables.find(t => t.boxId === targetBoxId);
          if (!sourceTable || !targetTable) return;

          const sourceLeft = sourceTable.positionX > targetTable.positionX;
          const sourceRight = !sourceLeft;

          const sourceEl = sourceRight
               ? document.getElementById(`${sourceBoxId}-${relation.sourceColumn}-right`)
               : document.getElementById(`${sourceBoxId}-${relation.sourceColumn}-left`);
          const targetEl = sourceRight
               ? document.getElementById(`${targetBoxId}-${relation.targetColumn}-left`)
               : document.getElementById(`${targetBoxId}-${relation.targetColumn}-right`);

          if (sourceEl && targetEl) {
               const relIndex = this.relations.filter(r =>
                    (r.sourceBoxId === sourceBoxId && r.targetBoxId === targetBoxId) ||
                    (r.sourceBoxId === targetBoxId && r.targetBoxId === sourceBoxId)
               ).length;

               const dynamicStub = 30 + (relIndex * 15);
               const lineColor = relation.lineColor || this.getRandomColor();
               const sourceAnchor = sourceRight ? [1, 0.5, 1, 0] : [0, 0.5, -1, 0];
               const targetAnchor = sourceRight ? [0, 0.5, -1, 0] : [1, 0.5, 1, 0];

               const conn = this.jsPlumbInstance.connect({
                    source: sourceEl,
                    target: targetEl,
                    anchors: [sourceAnchor, targetAnchor],
                    connector: ['Flowchart', {
                         stub: dynamicStub,
                         alwaysRespectStubs: true,
                         cornerRadius: 5
                    }],
                    paintStyle: { strokeWidth: 3, stroke: lineColor },
                    hoverPaintStyle: { strokeWidth: 4, stroke: this.darkenColor(lineColor, 20) },
                    parameters: {
                         relationId: relation.id || this.generateId(),
                         sourceBoxId: sourceBoxId,
                         targetBoxId: targetBoxId,
                         lineColor: lineColor
                    }
               });

               if (conn) {
                    const joinType = (relation.joinType || 'INNER').toUpperCase();
                    const overlay = conn.getOverlay('label');
                    if (overlay) overlay.setLabel(`${joinType}: ${relation.sourceColumn} ↔ ${relation.targetColumn}`);
                    if (!this.relations.find(r => r.id === relation.id)) {
                         this.relations.push({
                              ...relation,
                              sourceBoxId: sourceBoxId,
                              targetBoxId: targetBoxId,
                              joinType: joinType,
                              lineColor: lineColor
                         });
                    }
               }
          }
     }

     onConnectionClick(conn) {
          this.selectedConnection = conn;

          // بازگرداندن رنگ اصلی به همه خطوط
          this.jsPlumbInstance.select().each(c => {
               const originalColor = c.getParameter('lineColor') || '#dc3545';
               c.setPaintStyle({ stroke: originalColor, strokeWidth: 3 });
          });

          // رنگ سبز برای خط انتخاب شده
          conn.setPaintStyle({ stroke: '#198754', strokeWidth: 4 });
     }

     onConnectionDoubleClick(conn) {
          const relationId = conn.getParameter('relationId');
          const relation = this.relations.find(r => r.id === relationId);

          if (!relation) return;

          this.cycleJoinType(conn, relation);
     }

     cycleJoinType(conn, relation) {
          const joinTypes = ['INNER', 'LEFT', 'RIGHT'];
          const currentIndex = joinTypes.indexOf(relation.joinType.toUpperCase());
          const nextIndex = (currentIndex + 1) % joinTypes.length;
          const nextJoinType = joinTypes[nextIndex];

          relation.joinType = nextJoinType;

          const overlay = conn.getOverlay('label');
          if (overlay) {
               overlay.setLabel(`${relation.joinType}: ${relation.sourceColumn} ↔ ${relation.targetColumn}`);
          }

          this.refreshQuery();
          this.showInfo(`نوع Join به ${relation.joinType} تغییر کرد`);
     }

     onConnectionCreated(info) {
          // این متد فعلاً خالی است چون همه چیز در onEndpointClick مدیریت می‌شود
     }

     onConnectionDetached(info) {
          const relationId = info.connection.getParameter('relationId');
          const index = this.relations.findIndex(r => r.id === relationId);

          if (index > -1) {
               this.relations.splice(index, 1);
               this.refreshQuery();
          }
     }

     showFilterModal(tableInfo, column) {
          $('#filterEditId').val('');
          $('#filterTableRef').val(tableInfo.boxId || '');
          $('#filterTableName').val(`${tableInfo.schema}.${tableInfo.name}`);
          $('#filterColumnName').val(column.columnName || column.name);
          $('#filterDataType').val(column.dataType);
          $('#filterColumnDisplay').val(`${tableInfo.schema}.${tableInfo.name}.${column.displayName || column.name}`);

          $('#filterOperator').val('equals');
          $('#filterValue').val('');
          $('#filterLogicalOperator').val('AND');
          $('#filterValueGroup').show();
          $('#btnSaveFilterText').text('افزودن');
          this.updateFilterParamsList();

          const modal = new bootstrap.Modal(document.getElementById('filterModal'));
          modal.show();
     }

     showFilterModalForEdit(filter) {
          $('#filterEditId').val(filter.id);
          $('#filterTableRef').val(filter.tableRef || '');
          $('#filterTableName').val(filter.tableName);
          $('#filterColumnName').val(filter.columnName);
          $('#filterDataType').val(filter.dataType);
          $('#filterColumnDisplay').val(`${filter.tableName}.${filter.columnName}`);

          $('#filterOperator').val(filter.operator || 'equals');
          $('#filterValue').val(filter.value || '');
          $('#filterLogicalOperator').val(filter.logicalOperator || 'AND');

          const needsNoValue = ['isnull', 'isnotnull'].includes(filter.operator);
          $('#filterValueGroup').toggle(!needsNoValue);
          const isInOperator = ['in', 'notin'].includes(filter.operator);
          $('#filterValue').attr('placeholder', isInOperator ? 'مقادیر جدا شده با کاما' : 'مقدار فیلتر را وارد کنید');

          $('#btnSaveFilterText').text('ذخیره تغییرات');
          this.updateFilterParamsList();

          const modal = new bootstrap.Modal(document.getElementById('filterModal'));
          modal.show();
     }

     saveFilter() {
          const editId = $('#filterEditId').val();
          const filterIndex = editId ? this.filters.findIndex(f => f.id === editId) : -1;
          const isFirstFilter = (editId && filterIndex === 0) || (!editId && this.filters.length === 0);
          const filter = {
               id: editId || this.generateId(),
               tableRef: $('#filterTableRef').val() || null,
               tableName: $('#filterTableName').val(),
               columnName: $('#filterColumnName').val(),
               dataType: $('#filterDataType').val(),
               operator: $('#filterOperator').val(),
               value: $('#filterValue').val(),
               logicalOperator: isFirstFilter ? '' : ($('#filterLogicalOperator').val() || 'AND')
          };

          if (editId && filterIndex > -1) {
               this.filters[filterIndex] = filter;
               this.showSuccess('فیلتر بروزرسانی شد');
          } else {
               this.filters.push(filter);
               this.showSuccess('فیلتر اضافه شد');
          }

          const modal = bootstrap.Modal.getInstance(document.getElementById('filterModal'));
          modal.hide();

          this.updateFiltersList();
          this.refreshQuery();
     }

     removeFilter(filterId) {
          const index = this.filters.findIndex(f => f.id === filterId);
          if (index > -1) {
               this.filters.splice(index, 1);
               this.updateFiltersList();
               this.refreshQuery();
          }
     }

     async loadBuiltInParameters() {
          try {
               const response = await $.get('GetBuiltInParameters');
               if (response.isSuccess) {
                    this.builtInParams = response.data || [];
                    const html = this.builtInParams.map(p =>
                         `<div class="py-1"><code>@${p.name}</code> <span class="text-muted">${p.description}</span></div>`
                    ).join('');
                    $('#builtInParamsList').html(html || '<span class="text-muted">-</span>');
               }
          } catch (e) { $('#builtInParamsList').html('<span class="text-danger">خطا در بارگذاری</span>'); }
     }

     updateFilterParamsList() {
          const $list = $('#filterParamsList');
          $list.empty();
          const allParams = [
               ...this.builtInParams.map(p => ({ name: p.name, desc: p.description, isBuiltIn: true })),
               ...this.parameters.map(p => ({ name: p.name, desc: p.displayName || '', isBuiltIn: false }))
          ];
          allParams.forEach(p => {
               const $item = $(`
                    <div class="d-flex align-items-center justify-content-between py-1 px-2 mb-1 rounded border bg-light">
                         <code class="small">@${p.name}</code>
                         <button type="button" class="btn btn-sm btn-outline-success btn-insert-param" title="درج در مقدار">
                              <i class="bi bi-plus-lg"></i>
                         </button>
                    </div>
               `);
               $item.find('.btn-insert-param').on('click', () => {
                    const $val = $('#filterValue');
                    const cur = $val.val();
                    const insert = `@${p.name}`;
                    $val.val(cur ? cur + insert : insert);
               });
               $list.append($item);
          });
          if (allParams.length === 0) $list.html('<span class="text-muted small">پارامتری موجود نیست</span>');
     }

     updateParametersList() {
          const $list = $('#parametersList');
          const $empty = $('#parametersEmpty');
          $list.empty();
          if (this.parameters.length === 0) {
               $empty.show();
               return;
          }
          $empty.hide();
          this.parameters.forEach(p => {
               const $item = $(`
                    <div class="d-flex align-items-center justify-content-between py-1 px-2 mb-1 rounded bg-white border" data-param-id="${p.id}">
                         <div class="flex-grow-1 overflow-hidden">
                              <code class="small">@${p.name}</code>
                              ${p.displayName ? `<span class="text-muted ms-1 small">${p.displayName}</span>` : ''}
                         </div>
                         <div class="btn-group btn-group-sm">
                              <button type="button" class="btn btn-outline-secondary btn-sm btn-edit-param" title="ویرایش"><i class="bi bi-pencil"></i></button>
                              <button type="button" class="btn btn-outline-danger btn-sm btn-remove-param" title="حذف"><i class="bi bi-trash"></i></button>
                         </div>
                    </div>
               `);
               $item.find('.btn-edit-param').on('click', () => this.editParameter(p.id));
               $item.find('.btn-remove-param').on('click', () => this.removeParameter(p.id));
               $list.append($item);
          });
     }

     showParameterModal(param = null) {
          $('#paramEditId').val(param ? param.id : '');
          $('#paramName').val(param ? param.name : '').prop('readonly', !!param);
          $('#paramDisplayName').val(param ? param.displayName || '' : '');
          $('#paramDefaultValue').val(param ? param.defaultValue || '' : '');
          const modal = new bootstrap.Modal(document.getElementById('parameterModal'));
          modal.show();
     }

     saveParameter() {
          const editId = $('#paramEditId').val();
          const name = $('#paramName').val().trim().replace(/^@/, '');
          const displayName = $('#paramDisplayName').val().trim();
          const defaultValue = $('#paramDefaultValue').val();
          if (!name) { this.showError('نام پارامتر الزامی است'); return; }
          if (!/^[a-zA-Z_][a-zA-Z0-9_]*$/.test(name)) { this.showError('نام پارامتر نامعتبر است'); return; }
          const exists = this.parameters.find(p => p.name.toLowerCase() === name.toLowerCase() && p.Id !== editId);
          if (exists) { this.showError('این نام قبلاً استفاده شده'); return; }
          const param = { Id: editId || this.generateId(), name: name, displayName: displayName || null, defaultValue: defaultValue || null };
          const idx = editId ? this.parameters.findIndex(p => p.id === editId) : -1;
          if (idx >= 0) this.parameters[idx] = param;
          else this.parameters.push(param);
          bootstrap.Modal.getInstance(document.getElementById('parameterModal')).hide();
          this.updateParametersList();
          this.showSuccess('پارامتر ذخیره شد');
     }

     editParameter(paramId) {
          const p = this.parameters.find(x => x.id === paramId);
          if (p) this.showParameterModal(p);
     }

     removeParameter(paramId) {
          const idx = this.parameters.findIndex(p => p.id === paramId);
          if (idx >= 0) { this.parameters.splice(idx, 1); this.updateParametersList(); }
     }

     editFilter(filterId) {
          const filter = this.filters.find(f => f.id === filterId);
          if (filter) {
               this.showFilterModalForEdit(filter);
          }
     }

     getOperatorLabel(operator) {
          const labels = {
               equals: '=',
               notequals: '≠',
               greater: '>',
               greaterorequal: '≥',
               less: '<',
               lessorequal: '≤',
               like: 'شامل',
               notlike: 'شامل نباشد',
               in: 'در لیست',
               notin: 'خارج از لیست',
               isnull: 'خالی',
               isnotnull: 'خالی نباشد'
          };
          return labels[operator] || operator;
     }

     updateFiltersList() {
          const $list = $('#filtersList');
          const $empty = $('#filtersEmpty');

          $list.empty();

          if (this.filters.length === 0) {
               $empty.show();
               return;
          }

          $empty.hide();

          this.filters.forEach((filter, index) => {
               const opLabel = this.getOperatorLabel(filter.operator);
               const valueDisplay = ['isnull', 'isnotnull'].includes(filter.operator)
                    ? '' : ` ${opLabel} ${filter.value || '(خالی)'}`;
               const logicalPrefix = filter.logicalOperator ? `[${filter.logicalOperator}] ` : '';

               const $item = $(`
                    <div class="filter-item" data-filter-id="${filter.id}">
                         <div class="filter-item-content">
                              <div class="filter-item-label">${filter.tableName}.${filter.columnName}</div>
                              <div class="filter-item-condition">${logicalPrefix}${opLabel}${valueDisplay}</div>
                         </div>
                         <div class="filter-item-actions">
                              <button type="button" class="btn btn-sm btn-outline-primary btn-edit-filter" title="ویرایش">
                                   <i class="bi bi-pencil"></i>
                              </button>
                              <button type="button" class="btn btn-sm btn-outline-danger btn-remove-filter" title="حذف">
                                   <i class="bi bi-trash"></i>
                              </button>
                         </div>
                    </div>
               `);

               $item.find('.btn-edit-filter').on('click', () => this.editFilter(filter.id));
               $item.find('.btn-remove-filter').on('click', () => this.removeFilter(filter.id));

               $list.append($item);
          });
     }

     removeTable(boxId) {
          this.jsPlumbInstance.remove(boxId);

          const table = this.selectedTables.find(t => t.boxId === boxId);
          const index = table ? this.selectedTables.indexOf(table) : -1;
          if (index > -1) this.selectedTables.splice(index, 1);

          this.relations = this.relations.filter(r =>
               r.sourceBoxId !== boxId && r.targetBoxId !== boxId);

          const removedFullName = table ? `${table.schema}.${table.name}` : null;
          const hasOtherInstance = this.selectedTables.some(t => `${t.schema}.${t.name}` === removedFullName);
          this.filters = this.filters.filter(f => {
               if (f.tableRef === boxId) return false;
               if (!f.tableRef && removedFullName && f.tableName === removedFullName && !hasOtherInstance) return false;
               return true;
          });
          this.updateFiltersList();
          this.refreshQuery();

          if (this.selectedTables.length === 0) {
               $('.empty-state').show();
          }

          this.updateSelectedColumns();
     }

     clearDiagram() {
          if (this.selectedTables.length === 0) return;

          if (confirm('آیا از پاک کردن کامل دیاگرام اطمینان دارید؟')) {
               this.jsPlumbInstance.deleteEveryConnection();
               this.jsPlumbInstance.deleteEveryEndpoint();

               $('#diagramCanvas .table-box').remove();

               this.selectedTables = [];
               this.relations = [];
               this.filters = [];
               this.columns = [];

               $('.empty-state').show();
               $('#queryEditor').val('').trigger('input');
               this.updateColumnsTable();
               this.updateFiltersList();
          }
     }

     setZoom(zoom) {
          this.zoom = Math.max(0.3, Math.min(3, zoom));

          $('#diagramCanvas').css('transform', `scale(${this.zoom})`);
          this.jsPlumbInstance.setZoom(this.zoom);

          this.showInfo(`Zoom: ${Math.round(this.zoom * 100)}%`);
     }

     /**
      * مپ کردن نوع دیتابیس به SystemType - بدون Entity, ListEntity, ListString, ListLong, AutoNumber
      */
     mapDbTypeToSystemType(mappedType, dataType) {
          const excluded = ['Entity', 'ListEntity', 'ListString', 'ListLong', 'AutoNumber', 10, 11, 12, 13, 15];
          const typeStr = (mappedType ?? '').toString();
          const typeNum = typeof mappedType === 'number' ? mappedType : null;
          const typeMap = { 0: 'String', 1: 'Boolean', 2: 'DateTime', 3: 'Date', 4: 'DateTimeShamsi', 5: 'DateShamsi', 6: 'Long', 7: 'Int', 8: 'Select', 9: 'File', 14: 'Decimal' };
          if (excluded.includes(typeStr) || excluded.includes(typeNum)) return 'String';
          if (typeNum != null && typeMap[typeNum]) return typeMap[typeNum];
          const allowed = ['String', 'Boolean', 'DateTime', 'Date', 'DateTimeShamsi', 'DateShamsi', 'Long', 'Int', 'Select', 'File', 'Decimal'];
          if (allowed.includes(typeStr)) return typeStr;
          const sqlMap = { varchar: 'String', nvarchar: 'String', char: 'String', nchar: 'String', text: 'String', ntext: 'String', bit: 'Boolean', int: 'Int', smallint: 'Int', tinyint: 'Int', bigint: 'Long', decimal: 'Decimal', numeric: 'Decimal', money: 'Decimal', float: 'Decimal', real: 'Decimal', datetime: 'DateTime', datetime2: 'DateTime', date: 'Date', time: 'String', uniqueidentifier: 'String', varbinary: 'File', binary: 'File', image: 'File' };
          const dt = (dataType || '').toLowerCase();
          return sqlMap[dt] || 'String';
     }
     updateSelectedColumns() {
     
          const oldColumns = this.columns;

          const customCols = oldColumns.filter(c => c.isCustom);
     
          const oldColMap = new Map();
          oldColumns.filter(c => !c.isCustom).forEach(col => {
               oldColMap.set(col.alliance, col);
          });
          const newDataCols = [];
   
          // حفظ ستون‌های data موجود که هنوز انتخاب شده‌اند
          for (const oldCol of oldColumns.filter(c => !c.isCustom)) {
               let found = false;
               for (const table of this.selectedTables) {
                    const alias = this.resolveAlias(table.boxId ?? `${table.schema}.${table.name}`);
                    const alliance = `${alias}_${oldCol.columnName}`;
                    if (alliance === oldCol.alliance) {
                         const colFromTable = table.columns.find(
                              c => c.columnName === oldCol.columnName && c.isSelected
                         );
                         if (colFromTable) {
                              newDataCols.push({
                                   ...oldCol,
                                   tableRef: table.boxId,
                                   tableName: `${table.schema}.${table.name}`,
                                   address: `[${alias}].[${oldCol.columnName}]`,
                                   alliance: alliance,
                                   name: oldCol.columnName,
                                   displayName: oldCol.displayName ?? (colFromTable.displayName || colFromTable.columnName),
                                   systemType: oldCol.systemType ?? this.mapDbTypeToSystemType(colFromTable.mappedSystemType, colFromTable.dataType),
                                   render: oldCol.render ?? colFromTable.render,
                              });
                              found = true;
                              break;
                         }
                    }
               }
          }
 
          // اضافه کردن ستون‌های data جدید (تازه تیک خورده)
          for (const table of this.selectedTables) {
               const alias = this.resolveAlias(table.boxId ?? `${table.schema}.${table.name}`);
               const fullTableName = `${table.schema}.${table.name}`;
               for (const col of table.columns.filter(c => c.isSelected)) {
                    const alliance = `${alias}_${col.columnName}`;
                    if (!oldColMap.has(alliance)) {
                         newDataCols.push({
                              id: alliance,          // ستون‌های data از alliance به عنوان id
                              isCustom: false,
                              customColType: 'data',
                              tableRef: table.boxId,
                              tableName: fullTableName,
                              columnName: col.columnName,
                              address: `[${alias}].[${col.columnName}]`,
                              alliance: alliance,
                              name: col.columnName,
                              displayName: col.displayName || col.columnName,
                              systemType: this.mapDbTypeToSystemType(col.mappedSystemType, col.dataType),
                              sortDirection: null,
                              sortOrder: null,
                              groupBy: false,
                              aggregate: null,
                              visible: true,
                              primaryKey: false,
                              optionSetting: null,
                              render: col.render ?? null,
                         });
                    }
               }
          }

          // ── ترکیب: data-linked + custom (ستون‌های دستی همیشه بعد از data می‌آیند)
          this.columns = [...newDataCols, ...customCols];
          this.updateColumnsTable();
          this.refreshQuery();
     }

     updateColumnsTable() {
          const $tbody = $('#columnsTableBody');
          $tbody.empty();

          if (!this.columns.length) {
               $tbody.append(`
            <tr>
                <td colspan="10" class="text-center text-muted">ستونی انتخاب نشده است</td>
            </tr>
        `);
               return;
          }

          const thisColumns = this.columns;

          const systemTypeOptions = [
               'String', 'Boolean', 'DateTime', 'Date', 'DateTimeShamsi',
               'DateShamsi', 'Long', 'Int', 'Select', 'File', 'Decimal'
          ].map(v => `<option value="${v}">${v}</option>`).join('');

          const aggregateOptions = [
               { value: '', label: '-' },
               { value: 'Count', label: 'Count' },
               { value: 'Max', label: 'Max' },
               { value: 'Min', label: 'Min' },
               { value: 'Avg', label: 'Avg' },
               { value: 'Sum', label: 'Sum' },
               { value: 'CountDistinct', label: 'CountDistinct' },
               { value: 'AvgDistinct', label: 'AvgDistinct' },
               { value: 'SumDistinct', label: 'SumDistinct' },
          ].map(o => `<option value="${o.value}">${o.label}</option>`).join('');

          this.columns.forEach((col, index) => {
               const isCustom = col.isCustom === true;
               const grpChecked = col.groupBy ? 'checked' : '';
               const visChecked = col.visible !== false ? 'checked' : '';
               const pkChecked = col.primaryKey ? 'checked' : '';
               const sysType = col.systemType || 'String';

               const sysTypeOpts = systemTypeOptions.replace(
                    `value="${sysType}"`,
                    `value="${sysType}" selected`
               );
               const aggOpts = aggregateOptions.replace(
                    `value="${col.aggregate || ''}"`,
                    `value="${col.aggregate || ''}" selected`
               );
               const sortSel = ['Ascending', '0', 0].includes(col.sortDirection) ? '0'
                    : ['Descending', '1', 1].includes(col.sortDirection) ? '1' : '';

               // ── نشانگر نوع ستون ───────────────────────────────────────────────
               const colTypeIcon = isCustom
                    ? (() => {
                         const icons = { button: 'bi-hand-index text-success', input: 'bi-input-cursor-text text-warning', html: 'bi-code-slash text-danger', data: 'bi-link text-info' };
                         return `<i class="bi ${icons[col.customColType] || 'bi-star'}" title="${col.customColType}"></i>`;
                    })()
                    : '<i class="bi bi-database text-primary" title="data"></i>';

               // ── ستون نام ──────────────────────────────────────────────────────
               const colNameCell = col.tableName && col.columnName
                    ? `${col.tableName}.${col.columnName}`
                    : `<span class="text-muted fst-italic">${col.customColType || 'custom'}</span>`;

               let primaryKeyTd = '';
               if (this.reportType && this.reportType == 1) {
                    primaryKeyTd = `<td><input type="checkbox" data-index="${index}" data-field="primaryKey" ${pkChecked}></td>`;
               }

               const $row = $(`
            <tr data-col-id="${col.id ?? index}" data-is-custom="${isCustom}">
                <td class="drag-handle-cell" style="width:40px;">
                    <i class="fa fa-arrows-alt" data-handler="true" style="cursor:grab;"></i>
                    ${colTypeIcon}
                </td>
                <td>
                    <div class="d-flex gap-1">
                        <a href="#" data-action="setting" data-index="${index}"
                           class="btn btn-xs btn-outline-warning" title="تنظیمات">
                            <i class="bi bi-gear"></i>
                        </a>
                        ${isCustom ? `
                        <a href="#" data-action="editCustomCol" data-index="${index}"
                           class="btn btn-xs btn-outline-info" title="ویرایش ستون">
                            <i class="bi bi-pencil"></i>
                        </a>` : ''}
                        <a href="#" data-action="removeCol" data-index="${index}"
                           class="btn btn-xs btn-outline-danger" title="حذف ستون">
                            <i class="bi bi-trash"></i>
                        </a>
                    </div>
                </td>
                <td>
                    <input type="checkbox" data-index="${index}" data-field="visible" ${visChecked}>
                </td>
                ${primaryKeyTd}
                <td class="small" style="max-width:150px; overflow:hidden; text-overflow:ellipsis; white-space:nowrap;"
                    title="${col.alliance}">
                    ${colNameCell}
                    <div class="text-muted" style="font-size:.7rem;">${col.alliance ?? ''}</div>
                </td>
                <td>
                    <input type="text" class="form-control form-control-sm"
                           value="${(col.displayName || '').replace(/"/g, '&quot;')}"
                           data-index="${index}" data-field="displayName">
                </td>
                <td>
                    <select class="form-select form-select-sm"
                            data-index="${index}" data-field="systemType"
                            ${isCustom && col.customColType !== 'data' ? 'disabled' : ''}>
                        ${sysTypeOpts}
                    </select>
                </td>
                <td class="text-center">
                    <input type="checkbox" class="form-check-input"
                           data-index="${index}" data-field="groupBy" ${grpChecked}
                           ${isCustom ? 'disabled' : ''}>
                </td>
                <td>
                    <select class="form-select form-select-sm"
                            data-index="${index}" data-field="aggregate"
                            ${isCustom ? 'disabled' : ''}>
                        ${aggOpts}
                    </select>
                </td>
                <td>
                    <select class="form-select form-select-sm"
                            data-index="${index}" data-field="sortDirection">
                        <option value="">بدون</option>
                        <option value="0" ${sortSel === '0' ? 'selected' : ''}>ASC</option>
                        <option value="1" ${sortSel === '1' ? 'selected' : ''}>DESC</option>
                    </select>
                </td>
                <td>
                    <input type="number" class="form-control form-control-sm"
                           min="1" value="${col.sortOrder || ''}"
                           data-index="${index}" data-field="sortOrder"
                           ${col.sortDirection ? '' : 'disabled'}>
                </td>
            </tr>
        `);

               // ── رویدادهای input/select ─────────────────────────────────────────
               $row.find('input[data-field], select[data-field]').on('change', (e) => {
                    const i = parseInt($(e.target).data('index'));
                    const field = $(e.target).data('field');
                    const val = $(e.target).val();
                    const chk = $(e.target).is(':checked');

                    if (field === 'displayName') this.columns[i].displayName = val;
                    else if (field === 'systemType') this.columns[i].systemType = val || 'String';
                    else if (field === 'groupBy') this.columns[i].groupBy = chk;
                    else if (field === 'visible') this.columns[i].visible = chk;
                    else if (field === 'primaryKey') this.columns[i].primaryKey = chk;
                    else if (field === 'aggregate') this.columns[i].aggregate = val || null;
                    else if (field === 'sortDirection') {
                         this.columns[i].sortDirection = val ? parseInt(val) : null;
                         $row.find('[data-field="sortOrder"]').prop('disabled', !val);
                    }
                    else if (field === 'sortOrder') this.columns[i].sortOrder = val ? parseInt(val) : null;

                    this.refreshQuery();
               });

               // ── دکمه حذف ──────────────────────────────────────────────────────
               $row.find('[data-action="removeCol"]').on('click', (e) => {
                    e.preventDefault();
                    const i = parseInt($(e.currentTarget).data('index'));
                    if (confirm(`ستون "${this.columns[i]?.displayName}" حذف شود؟`)) {
                         this.removeColumn(i);
                    }
               });

               // ── دکمه ویرایش ستون سفارشی ──────────────────────────────────────
               $row.find('[data-action="editCustomCol"]').on('click', (e) => {
                    e.preventDefault();
                    const i = parseInt($(e.currentTarget).data('index'));
                    this.showManualColumnModal(this.columns[i]);
               });

               // ── دکمه تنظیمات (render / select options) – بدون تغییر ──────────
               $row.find('[data-action="setting"]').on('click', (e) => {
                    e.preventDefault();
                    const i = parseInt($(e.currentTarget).data('index'))
                         ?? $(e.currentTarget).closest('tr').index();
                    const systemType = $row.find('[data-field="systemType"]').val();
                    const fieldName = $row.find('[data-field="displayName"]').val();
                    $.confirm({
                         title: `تنظیمات فیلد '${fieldName}'`,
                         content: this.settingFeildContetn(systemType, thisColumns[i]),
                         type: 'green',
                         columnClass: 'col-md-9',
                         typeAnimated: true,
                         buttons: {
                              acc: {
                                   text: 'ثبت', btnClass: 'btn btn-success',
                                   action: function () {
                                        // [بدون تغییر – همان کد settingFeild موجود]
                                        let $content = this.$content;
                                        if (systemType === 'Select') {
                                             let typeOption = $content.find('[data-action=typeOptions]').val();
                                             let listOptions = [], systemTypeName = '';
                                             if (typeOption === '0') { toastr.error('نوع گزینه‌ها انتخاب نشده'); return false; }
                                             if (typeOption === '1') {
                                                  let $rows = $content.find('[data-section="defOptions"] tbody tr');
                                                  if (!$rows.length) { toastr.error('هیچ گزینه‌ای تعریف نشده'); return false; }
                                                  let err = false;
                                                  listOptions = $rows.map((ii, c) => {
                                                       const v = $(c).find('[data-feild="value"]').val();
                                                       const n = $(c).find('[data-feild="name"]').val();
                                                       if (!v || isNaN(parseInt(v))) { $(c).find('[data-feild="value"]').addClass('border-danger'); err = true; }
                                                       if (!n) { $(c).find('[data-feild="name"]').addClass('border-danger'); err = true; }
                                                       return { value: v, name: n };
                                                  }).get();
                                                  if (err) { toastr.error('اطلاعات ناقص است'); return false; }
                                             } else if (typeOption === '3') {
                                                  systemTypeName = $content.find('[data-action="systemOptions"]').val();
                                             }
                                             thisColumns[i].optionSetting = { listOptions, typeOption, systemTypeName };
                                        }
                                        thisColumns[i].render = $content.find('#renderCode').val();
                                   }
                              },
                              close: { text: 'بستن', btnClass: 'btn btn-danger', action() { } }
                         }
                    });
               });

               $tbody.append($row);
          });
     }
     settingFeildContetn(type ,column) {
          let $tpl = $();
          if (type === "Select") {
               $tpl = $(`
               <div class="row col-md-12">
                    <div class="col-md-4">
                         <label class="form-label">نوع تبدیل</label>
                         <select class="form-select" data-action="typeOptions">
                              <option value="0">انتخاب کنید</option>
                              <option value="1">تعریف گزینه</option>
                              <option value="2">گزینه موجود</option>
                              <option value="3">گزینه سیستمی</option>
                         </select>
                    </div>

                     <div class="col-md-8 d-none">
                         <label class="form-label" >گزینه های موجود</label>
                         <select class="form-select w-100" data-action="existOptions">
                             
                         </select>
                    </div>
                    <div class="col-md-8 d-none">
                       <label class="form-label" >گزینه های سیستمی</label>
                         <select class="form-select w-100" data-action="systemOptions">
                             
                         </select>
                    </div>

                    <div class="col-md-12 d-none mt-3" data-section="defOptions">
                    <button class="btn btn-success" data-action="addnewOptions">افزودن گزینه جدید</button>
                          <table class="table table-rounded table-striped border table-bordered">
                               <thead>
                                    <tr>
                                          <th>
                                         #
                                         </th>
                                         <th>
                                         مقدار
                                         </th>
                                         <th>
                                             عنوان
                                          </th>
                                    </tr>
                               </thead>
                               <tbody>
                               </tbody>
                          </table>
                    </div>
                     
               </div>
               `);

               $tpl.find("[data-action='addnewOptions']").click(function () {
                    let $newOptionTpl = $(`<tr> 
                                        <td>
                                             <button class="btn-sm btn btn-icon btn-active-icon-dark btn-color-danger" data-action="deleteOption">
		                                      <i class="fs-2 fa-light fa-trash"></i>
		                                   </button>
                                        </td>
                                        <td>
                                        <input class="form-control" data-feild="value" placeholder="فقط اعداد" />
                                        </td>
                                        <td>
                                           <input class="form-control" data-feild="name" />
                                        </td>
                                   </tr>`)
                    $newOptionTpl.find('[data-action="deleteOption"]').click(function () {
                         $newOptionTpl.remove();
                    })

                    $tpl.find("[data-section='defOptions'] tbody")
                         .append($newOptionTpl)
               })

               this.systemEnums.forEach((c) => {

                    let $systemEnumOption = $(`<option value="${c.type}">${c.parentEntityTypeName}.${c.name} || ${c.displayName} </option>`)
                    $tpl.find("[data-action='systemOptions']").append($systemEnumOption)
               })

               $tpl.find("[data-action='systemOptions']").select2({
                    width: '100%',
               });



               $tpl.find("[data-action='typeOptions']")
                    .change(function () {
                         let typeOption = $(this).val();

                         if (typeOption === "1") {
                              $tpl.find("[data-section='defOptions']").removeClass("d-none");
                              $tpl.find("[data-action='existOptions']").parent().addClass("d-none");
                              $tpl.find("[data-action='systemOptions']").parent().addClass("d-none");
                         }
                         else if (typeOption === "2") {
                              $tpl.find("[data-section='defOptions']").addClass("d-none");
                              $tpl.find("[data-action='existOptions']").parent().removeClass("d-none");
                              $tpl.find("[data-action='systemOptions']").parent().addClass("d-none");
                         }

                         else if (typeOption === "3") {
                              $tpl.find("[data-section='defOptions']").addClass("d-none");
                              $tpl.find("[data-action='existOptions']").parent().addClass("d-none");
                              $tpl.find("[data-action='systemOptions']").parent().removeClass("d-none");

                         }
                         else {
                              $tpl.find("[data-section='defOptions']").addClass("d-none");
                              $tpl.find("[data-action='systemOptions']").parent().addClass("d-none");
                              $tpl.find("[data-action='existOptions']").parent().addClass("d-none");
                         }
                    })

               var render = column.render ?? "function(data, type, row) { return data; }"
               $(`  <div class="col-md-12   mt-3" data-section="render">
                         <div style=" resize: auto;  height: 300px;"  id="renderCode"></div>
                     </div>`).appendTo($tpl);

               var newjsEditor = ace.edit($tpl.find("#renderCode")[0]);
               newjsEditor.setTheme('ace/theme/chrome');
               newjsEditor.session.setMode('ace/mode/javascript');
               newjsEditor.setValue(render);

               return $tpl;
          }
          else {
               var render = column.render ?? "function(data, type, row) { return data; }"
               
               $tpl = $(`  <div class="col-md-12   mt-3" data-section="render">
                         <div style=" resize: auto;  height: 300px;"   id="renderCode">${render}</div>
                     </div>`);
               var newjsEditor = ace.edit($tpl.find("#renderCode")[0]);
               newjsEditor.setTheme('ace/theme/chrome');
               newjsEditor.session.setMode('ace/mode/javascript');
               newjsEditor.setValue(render);

               return $tpl
          }
 
     }

     resolveAlias = (tableRef) => {
           
          const tableToAlias = {};
          const tables = this.selectedTables;
          let aliasIdx = 1;
          tables.forEach(t => {
               const key = t.boxId || `table-${(t.schema + '.' + t.name).replace(/\./g, '-')}`;
               tableToAlias[key] = `t${aliasIdx}`;
               aliasIdx++;
          });

          if (!tableRef) return Object.values(tableToAlias)[0] || 't1';
          if (tableToAlias[tableRef]) return tableToAlias[tableRef];
          const tbl = tables.find(t => t.boxId === tableRef || `${t.schema}.${t.name}` === tableRef);
          return tbl && tableToAlias[tbl.boxId] ? tableToAlias[tbl.boxId] : (Object.values(tableToAlias)[0] || 't1');
     };

     /**
      * ساخت Query در سمت کلاینت - بدون نیاز به سرور
      */

     buildQueryClient() {

           
          if (!this.selectedTables?.length) return '';

          const tables = this.selectedTables;
          const relations = this.relations || [];
          const filters = this.filters || [];
          const columns = this.columns || [];

          // ── alias map ──────────────────────────────────────────────────────────
          const tableToAlias = {};
          let aliasIdx = 1;
          tables.forEach(t => {
               tableToAlias[t.boxId || `table-${(t.schema + '.' + t.name).replace(/\./g, '-')}`] = `t${aliasIdx++}`;
          });

          const resolveAlias = (ref) => {
               if (!ref) return Object.values(tableToAlias)[0] || 't1';
               if (tableToAlias[ref]) return tableToAlias[ref];
               const tbl = tables.find(t => t.boxId === ref || `${t.schema}.${t.name}` === ref);
               return tbl && tableToAlias[tbl.boxId] ? tableToAlias[tbl.boxId] : (Object.values(tableToAlias)[0] || 't1');
          };

          // ── helpers (بدون تغییر از نسخه قبل) ─────────────────────────────────
          const getJoinKeyword = jt => ({ LEFT: 'LEFT JOIN', RIGHT: 'RIGHT JOIN' }[(jt || 'INNER').toUpperCase()] ?? 'INNER JOIN');
          const getOperatorSymbol = op => ({ equals: '=', notequals: '!=', greater: '>', less: '<', greaterorequal: '>=', lessorequal: '<=', like: 'LIKE', notlike: 'NOT LIKE', in: 'IN', notin: 'NOT IN', isnull: 'IS NULL', isnotnull: 'IS NOT NULL' })[(op || '').toLowerCase()] ?? '=';
          const isParam = v => v && /^@[a-zA-Z_][a-zA-Z0-9_]*$/.test((v || '').trim());
          const fmtVal = (val, dt) => {
               if (!val?.trim()) return 'NULL';
               const esc = val.replace(/'/g, "''"), d = (dt || '').toLowerCase();
               if (/int|decimal|numeric|float|money/.test(d)) return val;
               if (/date|time/.test(d)) return `'${esc}'`;
               if (d.includes('bit')) return (['true', '1'].includes(val.toLowerCase())) ? '1' : '0';
               return `N'${esc}'`;
          };
          const fmtLike = (v, dt) => (!v ? "N'%%'" : v.trim().includes('%') ? fmtVal(v, dt) : fmtVal(`%${v}%`, dt));
          const fmtIn = (v, dt) => (!v?.trim() ? 'NULL' : v.split(',').map(s => fmtVal(s.trim(), dt)).join(', ') || 'NULL');
          const buildFilter = (f) => {
               const op = (f.operator || '').toLowerCase(), alias = resolveAlias(f.tableRef || f.tableName);
               const ref = `[${alias}].[${f.columnName}]`;
               if (op === 'isnull' || op === 'isnotnull') return `${ref} ${getOperatorSymbol(f.operator)}`;
               if (op === 'in' || op === 'notin') return isParam(f.value) ? `${ref} ${getOperatorSymbol(f.operator)} (${f.value.trim()})` : `${ref} ${getOperatorSymbol(f.operator)} (${fmtIn(f.value, f.dataType)})`;
               if (op === 'like' || op === 'notlike') return isParam(f.value) ? `${ref} ${getOperatorSymbol(f.operator)} N'%'+${f.value.trim()}+N'%'` : `${ref} ${getOperatorSymbol(f.operator)} ${fmtLike(f.value, f.dataType)}`;
               return isParam(f.value) ? `${ref} ${getOperatorSymbol(f.operator)} ${f.value.trim()}` : `${ref} ${getOperatorSymbol(f.operator)} ${fmtVal(f.value, f.dataType)}`;
          };
          const getAgg = (agg, alias, col) => {
               const ref = `[${alias}].[${col}]`, a = (agg || '').toUpperCase();
               const map = { COUNT: `COUNT(${ref})`, MAX: `MAX(${ref})`, MIN: `MIN(${ref})`, AVG: `AVG(${ref})`, SUM: `SUM(${ref})`, COUNTDISTINCT: `COUNT(DISTINCT ${ref})`, AVGDISTINCT: `AVG(DISTINCT ${ref})`, SUMDISTINCT: `SUM(DISTINCT ${ref})` };
               return map[a] || ref;
          };

          // ══════════════════════════════════════════════════════════════════════
          // ★ تغییر اصلی: فقط ستون‌هایی که آدرس DB دارند وارد SELECT می‌شوند
          // ستون‌های custom بدون tableRef (button, html, input بدون لینک) حذف می‌شوند
          // ستون‌های custom با tableRef (لینک‌شده) همانند ستون‌های data پردازش می‌شوند
          // ══════════════════════════════════════════════════════════════════════
          const selectableColumns = columns.filter(c => c.address && c.tableName);

          const hasGroupBy = selectableColumns.some(c => c.groupBy);
          const hasAggregate = selectableColumns.some(c => c.aggregate);
          const useShaping = hasGroupBy || hasAggregate;

          if (useShaping && selectableColumns.length > 0) {
               if (!selectableColumns.every(c => c.groupBy || c.aggregate)) return '';
          }

          // ── ساخت SELECT ────────────────────────────────────────────────────────
          let selectClauses;

          if (selectableColumns.length > 0 && useShaping) {
               selectClauses = selectableColumns.map(c => {
                    const alias = resolveAlias(c.tableRef || c.tableName);
                    const aliasName = c.alliance;
                    const expr = c.aggregate
                         ? getAgg(c.aggregate, alias, c.columnName)
                         : `[${alias}].[${c.columnName}]`;
                    return `${expr} AS [${aliasName}]`;
               });
          } else {
               // از selectableColumns به جای همه ستون‌ها
               const seen = new Set();
               selectClauses = [];

               selectableColumns.forEach(c => {
                    // alliance یکتاست (برای multi-instance هم کار می‌کند)
                    if (seen.has(c.alliance)) return;
                    seen.add(c.alliance);

                    const alias = resolveAlias(c.tableRef || c.tableName);
                    selectClauses.push(`[${alias}].[${c.columnName}] AS [${c.alliance}]`);

                    // ── side-effect: اگر این ستون در دیاگرام تیک نخورده، تیکش بزن
                    //    تا دیاگرام با state همخوانی داشته باشد
                    const tableInfo = this.selectedTables.find(t => t.boxId === c.tableRef);
                    if (tableInfo) {
                         const diagramCol = tableInfo.columns.find(dc => dc.columnName === c.columnName);
                         if (diagramCol && !diagramCol.isSelected) {
                              diagramCol.isSelected = true;
                              // checkbox را هم بصری آپدیت کن (بدون trigger change برای جلوگیری از حلقه)
                              $(`#${c.tableRef} [data-column-name="${c.columnName}"] .column-checkbox`)
                                   .prop('checked', true);
                         }
                    }
               });

               if (!selectClauses.length) return '';
          }

          // ── FROM ───────────────────────────────────────────────────────────────
          const first = tables[0];
          const firstKey = first.boxId;
          let sql = 'SELECT\n    ' + selectClauses.join(',\n    ') + '\n';
          sql += `FROM [${first.schema}].[${first.name}] AS [${tableToAlias[firstKey]}]\n`;

          // ── JOINs (بدون تغییر) ─────────────────────────────────────────────────
          const joined = new Set([firstKey]);
          let rels = relations.filter(r =>
               (r.sourceBoxId && r.targetBoxId &&
                    tables.some(t => t.boxId === r.sourceBoxId) &&
                    tables.some(t => t.boxId === r.targetBoxId)) ||
               (r.sourceTable && r.targetTable &&
                    tables.some(t => `${t.schema}.${t.name}` === r.sourceTable) &&
                    tables.some(t => `${t.schema}.${t.name}` === r.targetTable))
          );
          while (rels.length) {
               let added = false;
               for (let i = 0; i < rels.length; i++) {
                    const r = rels[i];
                    const srcBox = r.sourceBoxId || tables.find(t => `${t.schema}.${t.name}` === r.sourceTable)?.boxId;
                    const tgtBox = r.targetBoxId || tables.find(t => `${t.schema}.${t.name}` === r.targetTable)?.boxId;
                    const srcIn = joined.has(srcBox), tgtIn = joined.has(tgtBox);
                    if (srcIn && tgtIn) { rels.splice(i--, 1); added = true; continue; }
                    if (srcIn && !tgtIn) {
                         const t = tables.find(t => t.boxId === tgtBox);
                         if (t && tableToAlias[srcBox] && tableToAlias[tgtBox]) {
                              sql += `${getJoinKeyword(r.joinType || r.JoinType)} [${t.schema}].[${t.name}] AS [${tableToAlias[tgtBox]}]\n`;
                              sql += `    ON [${tableToAlias[srcBox]}].[${r.sourceColumn}] = [${tableToAlias[tgtBox]}].[${r.targetColumn}]\n`;
                              joined.add(tgtBox); rels.splice(i--, 1); added = true;
                         }
                    } else if (tgtIn && !srcIn) {
                         const t = tables.find(t => t.boxId === srcBox);
                         if (t && tableToAlias[srcBox] && tableToAlias[tgtBox]) {
                              sql += `${getJoinKeyword(r.joinType || r.JoinType)} [${t.schema}].[${t.name}] AS [${tableToAlias[srcBox]}]\n`;
                              sql += `    ON [${tableToAlias[srcBox]}].[${r.sourceColumn}] = [${tableToAlias[tgtBox]}].[${r.targetColumn}]\n`;
                              joined.add(srcBox); rels.splice(i--, 1); added = true;
                         }
                    }
               }
               if (!added) break;
          }

          // ── WHERE ──────────────────────────────────────────────────────────────
          if (filters.length) {
               sql += 'WHERE\n';
               sql += filters.map((f, i) => i > 0 && f.logicalOperator
                    ? `    ${f.logicalOperator} ${buildFilter(f)}`
                    : `    ${buildFilter(f)}`
               ).join('\n') + '\n';
          }

          // ── GROUP BY ───────────────────────────────────────────────────────────
          if (useShaping && hasGroupBy) {
               const gb = selectableColumns.filter(c => c.groupBy)
                    .map(c => `    [${resolveAlias(c.tableRef || c.tableName)}].[${c.columnName}]`)
                    .join(',\n');
               sql += 'GROUP BY\n' + gb + '\n';
          }

          // ── ORDER BY ───────────────────────────────────────────────────────────
          const sortCols = selectableColumns.filter(c =>
               ['Ascending', 'Descending', '0', '1'].includes(c.sortDirection)
          );
          if (sortCols.length) {
               sortCols.sort((a, b) => (a.sortOrder || 999) - (b.sortOrder || 999));
               const dir = c => (['Descending', '1'].includes(String(c.sortDirection))) ? 'DESC' : 'ASC';
               const ob = sortCols.map(c => {
                    const alias = resolveAlias(c.tableRef || c.tableName);
                    return c.aggregate
                         ? `    ${getAgg(c.aggregate, alias, c.columnName)} ${dir(c)}`
                         : `    [${alias}].[${c.columnName}] ${dir(c)}`;
               }).join(',\n');
               sql += 'ORDER BY\n' + ob;
          }

          return sql.trim();
     }

     /**
      * بروزرسانی خودکار Query در textarea - بدون پیام
      */
     refreshQuery() {
          const query = this.buildQueryClient();
          $('#queryEditor').val(query || '').trigger('input');
     }

     async generateQuery() {
          if (this.selectedTables.length === 0) {
               this.showWarning('ابتدا جداول را انتخاب کنید');
               return;
          }
          this.refreshQuery();
     }

     async validateQuery() {
          const query = $('#queryEditor').val().trim();

          if (!query) {
               this.showWarning('Query خالی است');
               return;
          }

          try {
               const response = await $.ajax({
                    url: 'ValidateQuery',
                    method: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({ Query: query })
               });

                
               if (response.isSuccess) {
                    if (response.data) {
                         this.showSuccess('Query معتبر است');
                    } else {
                         this.showError('Query نامعتبر است');
                    }
               } else {
                    this.showError(response.message || 'خطا در اعتبارسنجی');
               }
          } catch (error) {
                 
               if (error.responseJSON.statusCode == 2)
                    this.showValidationErrors(error.responseJSON.message)
               else {

                    this.showError('خطا در ارتباط با سرور');
                    console.error(error);
               }
                
          }
     }

     async previewQuery() {
          const query = $('#queryEditor').val().trim();
          if (!query) { this.showWarning('Query خالی است'); return; }
          await this.doPreviewWithParams(query);
     }

     getParamsFromQuery(query) {
          const matches = query.match(/@([a-zA-Z_][a-zA-Z0-9_]*)/g) || [];
          return [...new Set(matches.map(m => m.substring(1)))];
     }

     async doPreviewWithParams(query) {
          const paramNames = this.getParamsFromQuery(query);
          const paramValues = {};
          this.parameters.forEach(p => {
               if (p.defaultValue != null && p.defaultValue !== '') paramValues[p.name] = p.defaultValue;
          });
          const userDefinedNames = this.parameters.map(p => p.name);
          const needsValue = paramNames.filter(n => userDefinedNames.includes(n) && (paramValues[n] == null || paramValues[n] === ''));

          if (needsValue.length === 0) {
               await this.executePreviewQuery(query, paramValues);
               return;
          }
          this._pendingPreview = { query, paramValues };
          this._pendingPreviewNeeds = needsValue;
          this.showParameterValuesModal(needsValue);
     }

     showParameterValuesModal(needsValue) {
          const $form = $('#parameterValuesForm');
          $form.empty();
          needsValue.forEach(name => {
               const p = this.parameters.find(x => x.name === name);
               const label = p?.displayName || name;
               $form.append(`
                    <div class="mb-3">
                         <label class="form-label">${label} (@${name})</label>
                         <input type="text" class="form-control param-value-input" data-param-name="${name}" placeholder="مقدار را وارد کنید">
                    </div>
               `);
          });
          $('#btnConfirmParameterValues').off('click').on('click', () => this.confirmParameterValuesAndPreview());
          const modal = new bootstrap.Modal(document.getElementById('parameterValuesModal'));
          modal.show();
     }

     async confirmParameterValuesAndPreview() {
          const paramValues = { ...this._pendingPreview.paramValues };
          this._pendingPreviewNeeds.forEach(name => {
               const val = $(`.param-value-input[data-param-name="${name}"]`).val();
               if (val != null) paramValues[name] = val;
          });
          bootstrap.Modal.getInstance(document.getElementById('parameterValuesModal')).hide();
          await this.executePreviewQuery(this._pendingPreview.query, paramValues);
          this._pendingPreview = null;
          this._pendingPreviewNeeds = null;
     }

     async executePreviewQuery(query, paramValues) {
          try {
               this.showLoading();
               const response = await $.ajax({
                    url: 'PreviewQuery',
                    method: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({ query: query, parameters: paramValues }),
                    processData: false
               });
               this.hideLoading();
                
               if (response.isSuccess) {
                    this.showPreviewModal(response.data);
                    if (this.mode === 1) {
                          
                         this._writeModePreviewed = true;
                         this._writeModeColumns = this.buildWriteModeColumnsFromPreview(response.data);
                         this.updateWriteModeColumnsTable();
                         $('#writeModeColumnsSection').removeClass('d-none');
                    }
               } else {
                    if (response.statusCode == 2)
                         this.showValidationErrors(response.message)
                    else
                       this.showError(response.message || 'خطا در اجرای Query');
               }
          } catch (error) {
               this.hideLoading();

               if (error.responseJSON.statusCode == 2)
                    this.showValidationErrors(error.responseJSON.message)
               else {

                    this.showError('خطا در ارتباط با سرور');
                    console.error(error);
               }

          }
     }

     /**
 * Escape HTML برای جلوگیری از XSS
 */
     escapeHtml(text) {
          const map = {
               '&': '&amp;',
               '<': '&lt;',
               '>': '&gt;',
               '"': '&quot;',
               "'": '&#039;'
          };
          return text.replace(/[&<>"']/g, m => map[m]);
     }

     showValidationErrors(response) {
          var splitedMessage = response.split("||")
          const errors = splitedMessage[1].split(",,");
          const warnings = splitedMessage[2].split(",,");

          let htmlContent = '<div class="text-right" style="direction: rtl;">';

          if (errors.length > 0) {
               htmlContent += '<div class="mb-3">';
               htmlContent += '<h6 class="text-danger"><i class="fas fa-times-circle"></i> خطاها:</h6>';
               htmlContent += '<ul class="list-unstyled">';
               errors.forEach(error => {
                    htmlContent += `
                <li class="p-2 mb-2" style="background: #ffe6e6; border-right: 3px solid #dc3545; border-radius: 4px;">
                    ${this.escapeHtml(error)}
                </li>
            `;
               });
               htmlContent += '</ul></div>';
          }

          if (warnings.length > 0) {
               htmlContent += '<div>';
               htmlContent += '<h6 class="text-warning"><i class="fas fa-exclamation-triangle"></i> هشدارها:</h6>';
               htmlContent += '<ul class="list-unstyled">';
               warnings.forEach(warning => {
                    htmlContent += `
                <li class="p-2 mb-2" style="background: #fff8e6; border-right: 3px solid #ffc107; border-radius: 4px;">
                    ${this.escapeHtml(warning)}
                </li>
            `;
               });
               htmlContent += '</ul></div>';
          }

          htmlContent += '</div>';

          Swal.fire({
               icon: 'error',
               title: '<span style="direction: rtl;">خطای امنیتی Query</span>',
               html: htmlContent,
               width: 600,
               confirmButtonText: 'متوجه شدم',
               confirmButtonColor: '#dc3545',
               footer: `
            <div class="text-right" style="direction: rtl;">
                <small class="text-muted">
                    <i class="fas fa-info-circle"></i>
                    تنها دستورات SELECT مجاز هستند
                </small>
            </div>
        `
          });
     }

     buildWriteModeColumnsFromPreview(result) {
          const names = result.columnNames || [];
          const types = result.columnTypes || [];
          return names.map((name, i) => ({
               columnName: name,
               displayName: this.generateDisplayNameFromColumn(name),
               systemType: this.mapDbTypeToSystemType(null, types[i] || 'nvarchar'),
               visible: true,
               primaryKey: false
          }));
     }

     generateDisplayNameFromColumn(columnName) {
          const map = { Id: 'شناسه', Name: 'نام', Title: 'عنوان', Code: 'کد', Date: 'تاریخ', Description: 'توضیحات' };
          return map[columnName] || columnName;
     }

     updateWriteModeColumnsTable() {
          const $tbody = $('#writeModeColumnsTableBody');
          $tbody.empty();
          const systemTypeOptions = [
               { value: 'String', label: 'String' }, { value: 'Boolean', label: 'Boolean' },
               { value: 'DateTime', label: 'DateTime' }, { value: 'Date', label: 'Date' },
               { value: 'DateTimeShamsi', label: 'DateTimeShamsi' }, { value: 'DateShamsi', label: 'DateShamsi' },
               { value: 'Long', label: 'Long' }, { value: 'Int', label: 'Int' },
               { value: 'Select', label: 'Select' }, { value: 'File', label: 'File' },
               { value: 'Decimal', label: 'Decimal' }
          ];

          let thisColumns = this._writeModeColumns;
          this._writeModeColumns.forEach((col, index) => {
               const opts = systemTypeOptions.map(o =>
                    `<option value="${o.value}" ${(col.systemType === o.value) ? 'selected' : ''}>${o.label}</option>`
               ).join('');

               const visibleChecked = col.visible ? 'checked' : '';
               const primaryKeyChecked = col.primaryKey ? 'checked' : '';

               let primaryKeyTd = ""
               if (this.reportType && this.reportType == 1) {
                    primaryKeyTd = `<td> <input type="checkbox"
                                   data-index="${index}"
                                   data-field="primaryKey"
                                   ${primaryKeyChecked}/> </td>`
               }

             

               const $row = $(`
                    <tr>
                 <td class="drag-handle-cell" style="cursor: grab; width: 40px;">
                           <i class="fa fa-arrows-alt" data-handler="true" style="cursor: grab;" data-index="${index}"></i>
                         
                          <a href="#" data-action="setting" data-index="${index}" class="btn btn-sm btn-outline-warning" title="ویرایش">
                               <i class="bi bi-pencil"></i>
                         </a>
                         
                    </td>
                    <td>
                         <input type="checkbox"
                         data-index="${index}"
                         data-field="visible"
                         ${visibleChecked}/>
                    </td>
                    ${primaryKeyTd}
                         <td>${col.columnName}</td>
                         <td><input type="text" class="form-control form-control-sm" value="${(col.displayName || '').replace(/"/g, '&quot;')}" data-index="${index}" data-field="displayName"></td>
                         <td><select class="form-select form-select-sm" data-index="${index}" data-field="systemType">${opts}</select></td>
                    </tr>
               `);
               $row.find('input, select').on('change', (e) => {
                    const idx = $(e.target).data('index');
                    const field = $(e.target).data('field');
                    const val = $(e.target).val();
                    const isChecked = $(e.target).is(':checked');

                    if (field === 'displayName') this._writeModeColumns[idx].displayName = val;
                    else if (field === 'visible') this._writeModeColumns[idx].visible = isChecked;
                    else if (field === 'primaryKey') {
                         this._writeModeColumns[idx].primaryKey = isChecked;
                    }
                    else if (field === 'systemType') this._writeModeColumns[idx].systemType = val || 'String';
               });

          


               $row.find('[data-action="setting"]').on('click', (e) => {
                    const idx = $(e.target).data('index') ?? $(e.target).closest("tr").index();
                    let systemType = $row.find('[data-field="systemType"]').val()
                    let feildName = $row.find('[data-field="displayName"]').val()
                    $.confirm({
                         title: `تنظیمات فیلد '${feildName}'`,
                         content: this.settingFeildContetn(systemType, thisColumns[idx]),
                         type: 'green',
                         columnClass: "col-md-9",
                         typeAnimated: true,
                         buttons: {
                              acc: {
                                   text: 'ثبت',
                                   btnClass: 'btn  btn-success',
                                   action: function () {
                                        let $content = this.$content;
                                        if (systemType == "Select") {
                                            
                                             let typeOption = $content.find("[data-action=typeOptions]").val()
                                             let listOptions = [];
                                             let systemTypeName = "";

                                             if (typeOption === "0") {
                                                  toastr.error("نوع گزینه ها انتخاب نشده است")
                                                  return false;
                                             }

                                             else if (typeOption === "1") {
                                                  let $trOptions = $content.find('[data-section="defOptions"] tbody tr');
                                                  if ($trOptions.length === 0) {
                                                       toastr.error("هیچ گزینه ای برای ثبت ایجاد نشده است")
                                                       return false;
                                                  }
                                                  let haveError = false;
                                                  listOptions = $trOptions.map((i, c) => {

                                                       let value = $(c).find('[data-feild="value"]').val();
                                                       if (!value || value.length === 0) {
                                                            $(c).find('[data-feild="value"]').addClass("border border-danger");
                                                            haveError = true;
                                                       }
                                                       else {
                                                            if (isNaN(parseInt(value))) {
                                                                 $(c).find('[data-feild="value"]').addClass("border border-danger");
                                                                 haveError = true;
                                                            }
                                                            else {
                                                                 $(c).find('[data-feild="value"]').removeClass("border border-danger")

                                                            }
                                                       }
                                                       let name = $(c).find('[data-feild="name"]').val();

                                                       if (!name || name.length === 0) {
                                                            $(c).find('[data-feild="name"]').addClass("border border-danger");
                                                            haveError = true;
                                                       }
                                                       else {
                                                            $(c).find('[data-feild="name"]').removeClass("border border-danger")

                                                       }

                                                       return { value, name };
                                                  }).get();

                                                  if (haveError === true) {
                                                       toastr.error("برای یک یا چند گزینه وارد شده اطلاعات صحیح وارد نشده است")
                                                       return false;
                                                  }
                                             }
                                             else if (typeOption === "2") {

                                                  toastr.error("قابلیت پیاده سازی نشده است")
                                                  return false;
                                             }
                                             else if (typeOption === "3") {
                                                  systemTypeName = $content.find('[data-action="systemOptions"]').val();
                                             }

                                             thisColumns[idx].optionSetting = { listOptions, typeOption, systemTypeName };
                                           

                                        }

                                        var newjsEditor = ace.edit($content.find("#renderCode")[0]);
                                      
                                    
                                        thisColumns[idx].render = newjsEditor.getValue();;
                                   }
                              },
                              close:
                              {
                                   text: 'بستن',
                                   btnClass: 'btn  btn-danger',
                                   action: function () {
                                   }
                              }
                         }
                    });

               })
               $tbody.append($row);
          });
     }

     getQueryEditorForMode() {
          return this.mode === 1 ? $('#queryEditorWrite') : $('#queryEditor');
     }

     async validateQueryWrite() {
          const query = this.sqlEditor.getValue();
          if (!query) { this.showWarning('Query خالی است'); return; }
          try {
               const response = await $.ajax({
                    url: 'ValidateQuery',
                    method: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({ query: query })
               });
               if (response.isSuccess) {
                    response.isValid ? this.showSuccess('Query معتبر است') : this.showError('Query نامعتبر است');
               } else this.showError(response.message || 'خطا در اعتبارسنجی');
          } catch (error) {
          
               if (error.responseJSON.statusCode == 2)
                    this.showValidationErrors(error.responseJSON.message)
               else {

                    this.showError('خطا در ارتباط با سرور');
                    console.error(error);
               }
               
          }
     }

     async previewQueryWrite() {
          const query = this.sqlEditor.getValue();
          if (!query) { this.showWarning('Query خالی است'); return; }
          await this.doPreviewWithParams(query);
     }

     async saveReportWrite() {
          const name = $('#reportName').val().trim();
          const title = $('#reportTitle').val().trim();
          const query = this.sqlEditor.getValue();
          if (!name) { this.showError('نام گزارش را وارد کنید'); return; }
          if (!title) { this.showError('عنوان گزارش را وارد کنید'); return; }
          if (!query) { this.showError('Query را وارد کنید'); return; }
          if (!this._writeModePreviewed || !this._writeModeColumns?.length) {
               this.showError('قبل از ذخیره حتماً یک بار پیش‌نمایش بگیرید تا ستون‌ها تعریف شوند');
               return;
          }

             const eventScriptsValidation = this.validateEventScripts();
             if (!eventScriptsValidation.valid) {
                 this.showError(eventScriptsValidation.error);
                 return;
             }
           
          const columns = this._writeModeColumns.map(c => ({
               columnName: c.columnName,
               displayName: c.displayName || c.columnName,
               systemType: c.systemType || 'String',
               visible: c.visible,
               primaryKey: c.primaryKey,
               optionSetting: c.optionSetting,
               render: c.render
          }));
          try {
              
               const design = {
                    name: name,
                    title: title,
                    queryDesign: { tables: [], relations: [], filters: [], customQuery: query, parameters: this.parameters },
                    columns: columns,
                    type: this.reportType,
                    mode: this.mode,
                    entityFullName: this.entityFullName,
                    actionOptions: JSON.stringify(this.actionOptions),
                    customActionButtons: JSON.stringify(this.customActionButtons),
                    eventScripts: JSON.stringify(this.eventScripts),
            

               };
               const url = this.editMode && this.reportId
                    ? `UpdateReport?id=${this.reportId}`
                    : 'SaveReport';

               let haveError = false;
               let havePrimaryKey = 0;
               let errorMessage = "";
               design.columns.forEach(c => {
                    if (c.systemType === "Select") {
                         if (!c.optionSetting) {
                              haveError = true;
                              errorMessage += `\n ستون '${c.displayName}' از نوع select میباشد اما تنظیمات مربوطه ثبت نشده است.`
                         }
                    }
                    if (design.type == 1) {
                         if (c.primaryKey == true) {
                              if (c.systemType != "Long") {
                                   haveError = true;
                                   errorMessage += `\n کلید اصلی فقط از نوع Long میتواند باشد.`
                              }
                              havePrimaryKey++;
                         }
                    }

               })

               if (haveError === true) {
                    return error2(errorMessage);
               }

               if (design.type == 1 && havePrimaryKey == 0) {
                    return error2("برای نمایه داده کلید اصلی مشخص نشده است");
               }

               if (design.type == 1 && havePrimaryKey > 1) {
                    return error2("فقط یک ستون به عنوان کلید اصلی باید انتخاب شود");
               }

               this.showLoading();
               const response = await $.ajax({
                    url: url,
                    method: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify(design),
                    processData: false
               });
               this.hideLoading();
               if (response.isSuccess) {
                    this.showSuccess('گزارش ذخیره شد');
                    
               } else this.showError(response.message || 'خطا در ذخیره');
          } catch (error) { this.hideLoading(); this.showError('خطا در ارتباط با سرور'); console.error(error); }
     }

     showPreviewModal(result) {
          const $content = $('#previewContent');
          $content.empty();

          if (!result.rows || result.rows.length === 0) {
               $content.html('<div class="alert alert-info">نتیجه‌ای یافت نشد</div>');
          } else {
               let html = `
                <div class="mb-2">
                    <strong>تعداد رکوردها:</strong> ${result.totalRows}
                    <span class="text-muted ms-3">(نمایش 100 رکورد اول)</span>
                </div>
                <div class="table-responsive" style="    max-height: 61vh;">
                    <table class="table table-sm table-bordered table-striped">
                        <thead class="table-dark">
                            <tr>
            `;
             

               let columnNamesConverted = [];
               result.columnNames.forEach(col => {
                    html += `<th>${col}</th>`;

                    columnNamesConverted.push(toCamelCase(col));
               });
               html += '</tr></thead><tbody>';

               

               result.rows.forEach(row => {
                    html += '<tr>';
                    columnNamesConverted.forEach(col => {
                         const value = row[col];
                         html += `<td>${value !== null && value !== undefined ? value : '<span class="text-muted">NULL</span>'}</td>`;
                    });
                    html += '</tr>';
               });

               html += '</tbody></table></div>';
               $content.html(html);
          }

          const modal = new bootstrap.Modal(document.getElementById('previewModal'));
          modal.show();
     }

     async checkNameUnique() {
          const name = $('#reportName').val().trim();

          if (!name) return;

          try {
               const response = await $.get('CheckNameUnique', {
                    name: name,
                    excludeId: this.reportId
               });

               if (response.isSuccess) {
                    if (!response.data) {
                         this.showError('این نام قبلاً استفاده شده است');
                         $('#reportName').addClass('is-invalid');
                    } else {
                         $('#reportName').removeClass('is-invalid');
                    }
               }
          } catch (error) {
               console.error(error);
          }
     }

     async saveReport() {
           
          const name = $('#reportName').val().trim();
          const title = $('#reportTitle').val().trim();

          if (!name) {
               this.showError('نام گزارش را وارد کنید');
               return;
          }

          if (!title) {
               this.showError('عنوان گزارش را وارد کنید');
               return;
          }

          if (this.selectedTables.length === 0) {
               this.showError('حداقل یک جدول را انتخاب کنید');
               return;
          }

          if (this.columns.length === 0) {
               this.showError('حداقل یک ستون را انتخاب کنید');
               return;
          }

          const eventScriptsValidation = this.validateEventScripts();
          if (!eventScriptsValidation.valid) {
               this.showError(eventScriptsValidation.error);
               return;
          }

            
          try {
               

               const design = {
                    name: name,
                    title: title,
                    entityFullName :this.entityFullName ,
                    type : this.reportType,
                    mode: this.mode,
                    actionOptions: JSON.stringify(this.actionOptions),
                    customActionButtons: JSON.stringify(this.customActionButtons),
                    eventScripts: JSON.stringify(this.eventScripts),
                    queryDesign: {
                         tables: this.selectedTables,
                         relations: this.relations,
                         filters: this.filters,
                         customQuery: $('#queryEditor').val(),
                         parameters: this.parameters
                    },
                    columns: this.columns
               };

               const url = this.editMode
                    ? `UpdateReport?id=${this.reportId}`
                    : 'SaveReport';
               let haveError = false;
               let havePrimaryKey = 0;
               let errorMessage = "";
               design.columns.forEach(c => {
                    if (c.systemType === "Select") {
                         if (!c.optionSetting) {
                              haveError = true;
                              errorMessage += `\n ستون '${c.displayName}' از نوع select میباشد اما تنظیمات مربوطه ثبت نشده است.`
                         }
                    }
                    if (design.type == 1) {
                         if (c.primaryKey == true) {
                              if (c.systemType != "Long") {
                                   haveError = true;
                                   errorMessage += `\n کلید اصلی فقط از نوع Long میتواند باشد.`
                              }
                              havePrimaryKey++;
                         }
                    }
                    
               })
             
               if (haveError === true) {
                    return error2(errorMessage);
               }

               if (design.type == 1 && havePrimaryKey == 0) {
                    return error2("برای نمایه داده کلید اصلی مشخص نشده است");
               }

               if (design.type == 1 && havePrimaryKey > 1) {
                    return error2("فقط یک ستون به عنوان کلید اصلی باید انتخاب شود");
               }

               this.showLoading();

               const response = await $.ajax({
                    url: url,
                    method: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify(design),
                    processData: false
               });

               this.hideLoading();

               if (response.isSuccess) {
                    this.showSuccess('گزارش با موفقیت ذخیره شد');

                   
               } else {
                    this.showError(response.message || 'خطا در ذخیره گزارش');
               }
          } catch (error) {
               this.hideLoading();
               this.showError('خطا در ارتباط با سرور');
               console.error(error);
          }
     }

     checkEditMode() {
          let reportId = window.__reportId;
          if (!reportId) reportId = new URLSearchParams(window.location.search).get('id');
          if (!reportId) {
               const match = window.location.pathname.match(/\/Edit\/(\d+)/);
               if (match) reportId = match[1];
          }

          if (reportId) {
               this.editMode = true;
               this.reportId = parseInt(reportId);
          }
     }

     async loadReport(id) {
          try {

               this.showLoading();

               const response = await $.get('GetReport', { id: id });

               debugger

               this.hideLoading();

               if (response.isSuccess) {

                    this.applyMode(response.data.mode);
                    this.reportType = response.data.type;
                     
                    const design = response.data;
                    design.columns.forEach((c) => {
                         c.systemType = c.systemTypeName
                    });
                    this.entityFullName = design.entityFullName;
                    $("#entityFullName").val(this.entityFullName)
                     

                    $('#reportName').val(design.name);
                    $('#reportTitle').val(design.title);

                    const tables = design.queryDesign.tables || [];
                    for (const table of tables) {
                         await this.addTableToDiagram(
                              table.name,
                              table.schema || 'dbo',
                              table.displayName,
                              table.entityFullName,
                              table.hasEntity,
                              table.positionX,
                              table.positionY,
                              true,
                              table.instanceId
                         );

                         const fullTableName = `${table.schema || 'dbo'}.${table.name}`;
                         const addedTable = this.selectedTables.filter(t => `${t.schema}.${t.name}` === fullTableName).pop();
                         const boxId = addedTable?.boxId;
                         (design.columns || []).filter(c => {
                              const tn = (c.tableName || c.tableRef || '').trim();
                              return tn === fullTableName || tn.toLowerCase() === fullTableName.toLowerCase() ||
                                   (c.tableRef && addedTable && c.tableRef === boxId);
                         }).forEach(dc => {
                              const tbl = addedTable || this.selectedTables.find(t => t.boxId === dc.tableRef || `${t.schema}.${t.name}` === fullTableName);
                              if (tbl && boxId) {
                                   const col = tbl.columns.find(c => (c.columnName || c.Name) === dc.columnName);
                                   if (col) {
                                        col.isSelected = true;
                                        col.displayName = dc.displayName || col.displayName;
                                        $(`#${boxId} [data-column-name="${dc.columnName}"] .column-checkbox`).prop('checked', true);
                                   }
                              }
                         });
                    }

                    this.relations = design.queryDesign?.relations || design.QueryDesign?.Relations || [];
                    // تاخیر برای اطمینان از آماده بودن endpointها (addColumnEndpoints با setTimeout 200ms)
                    setTimeout(() => {
                         this.relations.forEach(rel => this.addRelation(rel));
                         this.jsPlumbInstance.repaintEverything();
                    }, 450);

                    this.filters = (design.queryDesign.filters || []).map(f => {
                         const filter = { ...f };
                         if (!filter.tableRef) {
                              const t = this.selectedTables.find(t => `${t.schema}.${t.name}` === (f.tableName || '').trim());
                              if (t) filter.tableRef = t.boxId;
                         }
                         return filter;
                    });
                    this.updateFiltersList();

                    this.parameters = design.queryDesign.parameters || [];
                    this.updateParametersList();

                     

                    this.columns = (design.columns || []).map(c => {
                         const col = { ...c };
                         if (!col.tableRef) {
                              const t = this.selectedTables.find(t => `${t.schema}.${t.name}` === (c.tableName || '').trim());
                              if (t) col.tableRef = t.boxId;
                         }
                         if (col.systemType == undefined || col.systemType == null) {
                              const tbl = this.selectedTables.find(t => t.boxId === col.tableRef || `${t.schema}.${t.name}` === (c.tableName || '').trim());
                              const tblCol = tbl?.columns?.find(x => (x.columnName || x.Name) === c.columnName);
                              col.systemType = this.mapDbTypeToSystemType(tblCol?.mappedSystemType, tblCol?.dataType);
                         }
                         return col;
                    });

                    if (design.queryDesign.customQuery) {
                         $('#queryEditor').val(design.queryDesign.customQuery).trigger('input');
                         this.initSqlEditor(design.queryDesign.customQuery)
                       
                         if (tables.length === 0 && (design.columns || []).length > 0) {
                              this._writeModeColumns = (design.columns || []).map(c => ({
                                   columnName: c.columnName,
                                   displayName: c.displayName || c.columnName,
                                   systemType: c.systemType || 'String',
                                   visible: c.visible === null ? true : c.visible,
                                   primaryKey: c.primaryKey || false,
                                   optionSetting: c.optionSetting || null,
                                   render: c.render
                              }));
                              this._writeModePreviewed = true;
                              this.updateWriteModeColumnsTable();
                              $('#writeModeColumnsSection').removeClass('d-none');
                         }
                    } else {
                         await this.generateQuery();
                    }

                    this.updateColumnsTable();

                    this.actionOptions = design.actionOptions ? JSON.parse(design.actionOptions) : this.actionOptionsDefualt;
                    this.loadActionOptions();

                    this.customActionButtons = design.customActionButtons
                       ? JSON.parse(design.customActionButtons)
                         : [];

                    this.renderCustomButtonsList();

                    design.eventScriptsParsed = design.eventScripts
                         ? JSON.parse(design.eventScripts)
                         : { onSelectedRow: '', onRowAdded: '' };

                    this.eventScripts = design.eventScriptsParsed ?? { onSelectedRow: '', onRowAdded: '' };
                    this.loadEventScripts();
                                
                               
                               

                    this.showSuccess('گزارش بارگذاری شد');


               } else {
                    this.showError('خطا در بارگذاری گزارش');
               }
          } catch (error) {
               this.hideLoading();
               this.showError('خطا در ارتباط با سرور');
               console.error(error);
          }
     }

     generateId() {
          return 'id_' + Math.random().toString(36).substr(2, 9);
     }

     /**
      * بارگذاری اولیه دکمه‌های سفارشی
      */
     loadCustomActionButtons() {
          if (!this.customActionButtons) this.customActionButtons = [];
          this.renderCustomButtonsList();
     }


     /**
      * رندر لیست دکمه‌های سفارشی در صفحه طراح
      */
     renderCustomButtonsList() {
          const $list = $('#customButtonsList');
          const $empty = $('#customButtonsEmpty');

          // پاک کردن آیتم‌های قبلی (نه empty placeholder)
          $list.find('.custom-btn-card').remove();

          if (!this.customActionButtons.length) {
               $empty.show();
               return;
          }

          $empty.hide();

          this.customActionButtons.forEach((btn) => {
               const $card = this._buildCustomButtonCard(btn);
               $list.append($card);
          });
     }

     /**
      * ساخت کارت نمایش یک دکمه سفارشی
      * @param {Object} btn
      */
          _buildCustomButtonCard(btn) {
               const iconHtml = btn.useHtml
                    ? `<span class="badge bg-secondary">HTML</span>`
                    : `<i class="${btn.iconClass || 'bi bi-lightning'} ${btn.colorClass || 'btn-color-primary'} fs-4"></i>`;

               const $card = $(`
             <div class="col-md-4 custom-btn-card" data-btn-id="${btn.id}">
                 <div class="card border h-100">
                     <div class="card-body p-3">
                         <div class="d-flex align-items-start justify-content-between mb-2">
                             <div class="d-flex align-items-center gap-2">
                                 ${iconHtml}
                                 <strong class="fs-6">${this._escapeHtml(btn.title || '(بدون عنوان)')}</strong>
                                  <strong class="fs-6">${this._escapeHtml(btn.dataActionName || '(بدون dataActionName)')}</strong>
                             </div>
                             <div class="btn-group btn-group-sm">
                                 <button type="button" class="btn btn-outline-warning btn-edit-custom-btn" title="ویرایش">
                                     <i class="bi bi-pencil"></i>
                                 </button>
                                 <button type="button" class="btn btn-outline-danger btn-remove-custom-btn" title="حذف">
                                     <i class="bi bi-trash"></i>
                                 </button>
                             </div>
                         </div>
                         <div class="small text-muted">
                             ${btn.requiresSelection
                         ? '<span class="badge bg-light text-dark border"><i class="bi bi-cursor"></i> نیاز به انتخاب ردیف</span>'
                         : ''}
                             ${btn.useHtml
                         ? '<span class="badge bg-info text-white ms-1">HTML سفارشی</span>'
                         : `<code class="small">${this._escapeHtml(btn.colorClass || '')}</code>`}
                         </div>
                     </div>
                 </div>
             </div>
         `);

               $card.find('.btn-edit-custom-btn').on('click', () => this.editCustomButton(btn.id));
               $card.find('.btn-remove-custom-btn').on('click', () => this.removeCustomButton(btn.id));

               return $card;
     }

     /**
 * نمایش Modal افزودن / ویرایش دکمه سفارشی
 * @param {Object|null} btn
 */
     showCustomButtonModal(btn = null) {
          // reset
          $('#customBtnEditId').val(btn ? btn.id : '');
          $('#customButtonModalTitle').text(btn ? 'ویرایش دکمه سفارشی' : 'افزودن دکمه اکشن سفارشی');
          $('#customBtnUseHtml').prop('checked', btn?.useHtml ?? false);
          $('#customBtnTitle').val(btn?.title ?? '');
          $('#dataActionName').val(btn?.dataActionName ?? '');
          $('#customBtnColorClass').val(btn?.colorClass ?? 'btn-color-primary');
          $('#customBtnIconClass').val(btn?.iconClass ?? '');
          $('#customBtnRequiresSelection').prop('checked', btn?.requiresSelection ?? false);
          $('#customBtnHtml').val(btn?.html ?? '');

          var jsEditor = ace.edit('customBtnScript');
          jsEditor.setTheme('ace/theme/chrome');
          jsEditor.session.setMode('ace/mode/javascript');
          jsEditor.setValue(btn?.actionScript ?? this._defaultScriptTemplate());
          

        

          this._toggleCustomButtonMode();
          this._updateButtonPreview();

          new bootstrap.Modal(document.getElementById('customButtonModal')).show();
     }

     /**
      * ذخیره یک دکمه سفارشی (افزودن یا ویرایش)
      */
     saveCustomButton() {
          const editId = $('#customBtnEditId').val();
          const useHtml = $('#customBtnUseHtml').is(':checked');
          const title = $('#customBtnTitle').val().trim();
          const dataActionName = $('#dataActionName').val().trim();
          const script = ace.edit('customBtnScript').getValue(); 

          // اعتبارسنجی
          if (!useHtml && !title) {
               this.showError('عنوان دکمه الزامی است');
               return;
          }
          if (!script) {
               this.showError('کد JavaScript الزامی است');
               return;
          }

          // بررسی syntax ساده
          try {
               // eslint-disable-next-line no-new-func
               new Function('ctx', `return (${script})(ctx)`);
          } catch (e) {
               this.showError(`خطای syntax در کد JavaScript: ${e.message}`);
               return;
          }

          const btn = {
               id: editId || this.generateId(),
               title: title,
               dataActionName: dataActionName,
               colorClass: $('#customBtnColorClass').val(),
               iconClass: $('#customBtnIconClass').val().trim(),
               requiresSelection: $('#customBtnRequiresSelection').is(':checked'),
               useHtml: useHtml,
               html: useHtml ? $('#customBtnHtml').val().trim() : '',
               actionScript: script,
          };

          const idx = editId
               ? this.customActionButtons.findIndex(b => b.id === editId)
               : -1;

          if (idx >= 0) {
               this.customActionButtons[idx] = btn;
               this.showSuccess('دکمه بروزرسانی شد');
          } else {
               this.customActionButtons.push(btn);
               this.showSuccess('دکمه اضافه شد');
          }

          bootstrap.Modal.getInstance(document.getElementById('customButtonModal'))?.hide();
          this.renderCustomButtonsList();
     }

     /**
      * ویرایش یک دکمه سفارشی بر اساس id
      * @param {string} btnId
      */
     editCustomButton(btnId) {
          const btn = this.customActionButtons.find(b => b.id === btnId);
          if (btn) this.showCustomButtonModal(btn);
     }

     /**
      * حذف یک دکمه سفارشی
      * @param {string} btnId
      */
     removeCustomButton(btnId) {
          const idx = this.customActionButtons.findIndex(b => b.id === btnId);
          if (idx < 0) return;
          this.customActionButtons.splice(idx, 1);
          this.renderCustomButtonsList();
          this.showInfo('دکمه حذف شد');
     }

     /**
      * toggle بین حالت پیش‌فرض و HTML
      * @private
      */
     _toggleCustomButtonMode() {
          const useHtml = $('#customBtnUseHtml').is(':checked');
          $('#customBtnStandardFields').toggleClass('d-none', useHtml);
          $('#customBtnHtmlFields').toggleClass('d-none', !useHtml);
     }

     /**
      * بروزرسانی پیش‌نمایش دکمه در Modal
      * @private
      */
     _updateButtonPreview() {
          const colorClass = $('#customBtnColorClass').val() || 'btn-color-primary';
          const iconClass = $('#customBtnIconClass').val() || 'bi bi-star';
          const title = $('#customBtnTitle').val() || '';

          $('#customBtnPreview')
               .attr('class', `btn-sm btn btn-icon btn-active-icon-dark ${colorClass}`)
               .attr('title', title)
               .html(`<i class="${iconClass} fs-2"></i>`);
     }

     /**
      * درج قالب پیش‌فرض اسکریپت
      * @private
      */
     _insertScriptTemplate() {
          const template = this._defaultScriptTemplate();
          const current = $('#customBtnScript').val().trim();
          if (!current || confirm('محتوای فعلی جایگزین می‌شود. ادامه می‌دهید؟')) {
               $('#customBtnScript').val(template);
          }
     }

     /**
      * قالب پیش‌فرض کد JS
      * @returns {string}
      * @private
      */
     _defaultScriptTemplate() {
          return `function(ctx) {
              // ctx.selectedRow  → آبجکت ردیف انتخاب شده (null اگر انتخاب نشده)
              // ctx.table        → DataTable API
              // ctx.tableData    → آرایه داده‌های صفحه جاری
              // ctx.$toolbar     → jQuery نوار ابزار
              // ctx.dataActionBtns     → دکمه های موجود بر اساس dataAction
              // ctx.$btn         → jQuery دکمه کلیک شده
              // ctx.draw()       → بارگذاری مجدد جدول
 
              if (!ctx.selectedRow) {
                  toastr.warning('لطفاً یک ردیف انتخاب کنید');
                  return;
              }
 
              console.log('Selected row:', ctx.selectedRow);
          }`;
     }

     /**
      * escape کردن HTML برای جلوگیری از XSS در رندر کارت‌ها
      * @param {string} text
      * @returns {string}
      * @private
      */
     _escapeHtml(text) {
          const map = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#039;' };
          return String(text).replace(/[&<>"']/g, m => map[m]);
     }

     /**
      * تولید رنگ رندوم
      */
     getRandomColor() {
          const colors = [
               '#dc3545', // قرمز
               '#198754', // سبز
               '#0d6efd', // آبی
               '#d63384', // صورتی
               '#fd7e14', // نارنجی
               '#6f42c1', // بنفش
               '#20c997', // فیروزه‌ای
               '#0dcaf0', // آبی روشن
               '#6610f2', // نیلی
               '#ffc107'  // زرد
          ];
          return colors[Math.floor(Math.random() * colors.length)];
     }

     /**
      * تیره کردن رنگ برای hover
      */
     darkenColor(color, percent) {
          const num = parseInt(color.replace("#", ""), 16);
          const amt = Math.round(2.55 * percent);
          const R = (num >> 16) - amt;
          const G = (num >> 8 & 0x00FF) - amt;
          const B = (num & 0x0000FF) - amt;
          return "#" + (0x1000000 + (R < 255 ? R < 1 ? 0 : R : 255) * 0x10000 +
               (G < 255 ? G < 1 ? 0 : G : 255) * 0x100 +
               (B < 255 ? B < 1 ? 0 : B : 255))
               .toString(16).slice(1);
     }

     showSuccess(message) {
          this.showAlert(message, 'success');
     }

     showError(message) {
          this.showAlert(message, 'danger');
     }

     showWarning(message) {
          this.showAlert(message, 'warning');
     }

     showInfo(message) {
          this.showAlert(message, 'info');
     }

     showAlert(message, type) {
          const $alert = $(`
            <div class="alert alert-${type} alert-dismissible fade show alert-floating" role="alert">
                ${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        `);

          $('body').append($alert);

          setTimeout(() => {
               $alert.alert('close');
          }, 5000);
     }

     showLoading() {
          if ($('.loading-overlay').length === 0) {
               $('body').append(`
                <div class="loading-overlay">
                    <div class="spinner-border text-primary" role="status">
                        <span class="visually-hidden">در حال بارگذاری...</span>
                    </div>
                </div>
            `);
          }
     }

     hideLoading() {
          $('.loading-overlay').remove();
     }
}

function toCamelCase(str) {
     if (!str) return "";

     if (!str || str.length === 0) return str;
     return str.charAt(0).toLowerCase() + str.slice(1);
}

/**
* capitalize اول کلمه برای ساخت id textarea
* @param {string} str
* @returns {string}
* @private
*/
function _capitalize(str) {
     return str.charAt(0).toUpperCase() + str.slice(1);
}