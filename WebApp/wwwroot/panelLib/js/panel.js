
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
						  <span class="text-muted fs-8">
							 ${z.body}
						  </span>
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
* VERSION: Random Colors for Connections
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
          this.reportId = null;
          this.selectedConnection = null;
          this.firstClickedEndpoint = null;
          this.mode = null;
          this._writeModePreviewed = false;
          this._writeModeColumns = [];

          this.init();
     }

     async init() {
          this.checkEditMode();
          this.initMode();
          this.initJsPlumb();
          this.bindEvents();
          await this.loadTablesAndViews();
          this.updateFiltersList();
          this.loadBuiltInParameters();
          this.updateParametersList();
          if (this.editMode && this.reportId) this.loadReport(this.reportId);
     }

     initMode() {
          const urlMode = new URLSearchParams(window.location.search).get('mode');
          const storedMode = sessionStorage.getItem('queryDesignerMode');
          this.mode = null
             //  urlMode || storedMode || (window.__reportMode || null);

          if (this.mode) {
               $('#modeSelection').addClass('d-none');
               if (this.mode === 'design') {
                    $('#designModeContent').removeClass('d-none');
                    $('#writeModeContent').addClass('d-none');
                    // TODO: if (user.IsAdmin) { $('#queryEditor').prop('readonly', false); }
                    $('#queryEditor').prop('readonly', true);
               } else if (this.mode === 'write') {
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
          this.mode = mode;
          sessionStorage.setItem('queryDesignerMode', mode);
          $('#modeSelection').addClass('d-none');
          if (mode === 'design') {
               $('#designModeContent').removeClass('d-none');
               $('#writeModeContent').addClass('d-none');
               // TODO: if (user.IsAdmin) { $('#queryEditor').prop('readonly', false); }
               $('#queryEditor').prop('readonly', true);
          } else if (mode === 'write') {
               $('#designModeContent').addClass('d-none');
               $('#writeModeContent').removeClass('d-none');
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
               if (mode) this.applyMode(mode);
          });
          $('#btnChangeMode').on('click', () => {
               sessionStorage.removeItem('queryDesignerMode');
               //TODO RefreshPage
          });
          $('#btnValidateQueryWrite').on('click', () => this.validateQueryWrite());
          $('#btnPreviewQueryWrite').on('click', () => this.previewQueryWrite());
          $('#btnSaveReportWrite').on('click', () => this.saveReportWrite());
          $('#queryEditorWrite').on('input', () => {
               if (this.mode === 'write') this._writeModePreviewed = false;
          });

          $('#tableSearch').on('input', (e) => this.filterTables(e.target.value));
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

     renderTablesList() {
          const $list = $('#tablesList');
          $list.empty();

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

     async addTableToDiagram(tableName, schema = 'dbo', displayName, entityFullName, hasEntity, x = null, y = null, skipAutoRelations = false, existingInstanceId = null) {
          try {
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
          this.columns = [];

          this.selectedTables.forEach(table => {
               const selectedCols = table.columns.filter(c => c.isSelected);
               const fullTableName = `${table.schema}.${table.name}`;
               selectedCols.forEach(col => {
                    this.columns.push({
                         tableRef: table.boxId,
                         tableName: fullTableName,
                         columnName: col.columnName,
                         name: col.columnName,
                         displayName: col.displayName || col.columnName,
                         systemType: this.mapDbTypeToSystemType(col.mappedSystemType, col.dataType),
                         sortDirection: null,
                         sortOrder: null,
                         groupBy: false,
                         aggregate: null
                    });
               });
          });

          this.updateColumnsTable();
          this.refreshQuery();
     }

     updateColumnsTable() {
          const $tbody = $('#columnsTableBody');
          $tbody.empty();

          if (this.columns.length === 0) {
               $tbody.append(`
                <tr>
                    <td colspan="7" class="text-center text-muted">
                        ستونی انتخاب نشده است
                    </td>
                </tr>
            `);
               return;
          }

          const systemTypeOptions = [
               { value: 'String', label: 'String' },
               { value: 'Boolean', label: 'Boolean' },
               { value: 'DateTime', label: 'DateTime' },
               { value: 'Date', label: 'Date' },
               { value: 'DateTimeShamsi', label: 'DateTimeShamsi' },
               { value: 'DateShamsi', label: 'DateShamsi' },
               { value: 'Long', label: 'Long' },
               { value: 'Int', label: 'Int' },
               { value: 'Select', label: 'Select' },
               { value: 'File', label: 'File' },
               { value: 'Decimal', label: 'Decimal' }
          ];

          const aggregateOptions = [
               { value: '', label: '-' },
               { value: 'Count', label: 'Count' },
               { value: 'Max', label: 'Max' },
               { value: 'Min', label: 'Min' },
               { value: 'Avg', label: 'Avg' },
               { value: 'Sum', label: 'Sum' },
               { value: 'CountDistinct', label: 'CountDistinct' },
               { value: 'AvgDistinct', label: 'AvgDistinct' },
               { value: 'SumDistinct', label: 'SumDistinct' }
          ];

          this.columns.forEach((col, index) => {
               const grpChecked = col.groupBy ? 'checked' : '';
               const sysType = col.systemType || 'String';
               const sysTypeOpts = systemTypeOptions.map(o =>
                    `<option value="${o.value}" ${(sysType === o.value) ? 'selected' : ''}>${o.label}</option>`
               ).join('');
               const aggOpts = aggregateOptions.map(o =>
                    `<option value="${o.value}" ${(col.aggregate === o.value) ? 'selected' : ''}>${o.label}</option>`
               ).join('');
               const sortSel = ['Ascending', '0', 0].includes(col.sortDirection) ? 'Ascending' : ['Descending', '1', 1].includes(col.sortDirection) ? 'Descending' : '';
               const $row = $(`
                <tr>
                    <td>${col.tableName}.${col.columnName}</td>
                    <td>
                        <input type="text" class="form-control form-control-sm" 
                               value="${(col.displayName || '').replace(/"/g, '&quot;')}" 
                               data-index="${index}"
                               data-field="displayName">
                    </td>
                    <td>
                        <select class="form-select form-select-sm" 
                                data-index="${index}"
                                data-field="systemType">
                            ${sysTypeOpts}
                        </select>
                    </td>
                    <td class="text-center">
                        <input type="checkbox" class="form-check-input" 
                               data-index="${index}"
                               data-field="groupBy"
                               ${grpChecked}>
                    </td>
                    <td>
                        <select class="form-select form-select-sm" 
                                data-index="${index}"
                                data-field="aggregate">
                            ${aggOpts}
                        </select>
                    </td>
                    <td>
                        <select class="form-select form-select-sm" 
                                data-index="${index}"
                                data-field="sortDirection">
                            <option value="">بدون</option>
                            <option value="0" ${sortSel === 'Ascending' ? 'selected' : ''}>ASC</option>
                            <option value="1" ${sortSel === 'Descending' ? 'selected' : ''}>DESC</option>
                        </select>
                    </td>
                    <td>
                        <input type="number" class="form-control form-control-sm" 
                               min="1" value="${col.sortOrder || ''}"
                               data-index="${index}"
                               data-field="sortOrder"
                               ${col.sortDirection ? '' : 'disabled'}>
                    </td>
                </tr>
            `);

               $row.find('input, select').on('change', (e) => {
                    const idx = $(e.target).data('index');
                    const field = $(e.target).data('field');
                    const val = $(e.target).val();
                    const isChecked = $(e.target).is(':checked');

                    if (field === 'displayName') this.columns[idx].displayName = val;
                    else if (field === 'systemType') this.columns[idx].systemType = val || 'String';
                    else if (field === 'groupBy') this.columns[idx].groupBy = isChecked;
                    else if (field === 'aggregate') this.columns[idx].aggregate = val || null;
                    else if (field === 'sortDirection') {
                         this.columns[idx].sortDirection = parseInt(val) || null;
                         $row.find('[data-field="sortOrder"]').prop('disabled', !val);
                    } else if (field === 'sortOrder') {
                         this.columns[idx].sortOrder = val ? parseInt(val) : null;
                    }
                    this.refreshQuery();
               });

               $tbody.append($row);
          });
     }

     /**
      * ساخت Query در سمت کلاینت - بدون نیاز به سرور
      */
     buildQueryClient() {
          if (!this.selectedTables || this.selectedTables.length === 0) return '';
          const tables = this.selectedTables;
          const relations = this.relations || [];
          const filters = this.filters || [];
          const columns = this.columns || [];

          const tableToAlias = {};
          let aliasIdx = 1;
          tables.forEach(t => {
               const key = t.boxId || `table-${(t.schema + '.' + t.name).replace(/\./g, '-')}`;
               tableToAlias[key] = `t${aliasIdx}`;
               aliasIdx++;
          });

          const resolveAlias = (tableRef) => {
               if (!tableRef) return Object.values(tableToAlias)[0] || 't1';
               if (tableToAlias[tableRef]) return tableToAlias[tableRef];
               const tbl = tables.find(t => t.boxId === tableRef || `${t.schema}.${t.name}` === tableRef);
               return tbl && tableToAlias[tbl.boxId] ? tableToAlias[tbl.boxId] : (Object.values(tableToAlias)[0] || 't1');
          };

          const getJoinKeyword = (jt) => {
               const j = (jt || 'INNER').toUpperCase();
               return j === 'LEFT' ? 'LEFT JOIN' : j === 'RIGHT' ? 'RIGHT JOIN' : 'INNER JOIN';
          };

          const getOperatorSymbol = (op) => {
               const o = (op || '').toLowerCase();
               const map = { equals: '=', notequals: '!=', greater: '>', less: '<', greaterorequal: '>=', lessorequal: '<=', like: 'LIKE', notlike: 'NOT LIKE', in: 'IN', notin: 'NOT IN', isnull: 'IS NULL', isnotnull: 'IS NOT NULL' };
               return map[o] || '=';
          };

          const formatValue = (val, dataType) => {
               if (!val || val.trim() === '') return 'NULL';
               const esc = val.replace(/'/g, "''");
               const dt = (dataType || '').toLowerCase();
               if (dt.includes('int') || dt.includes('decimal') || dt.includes('numeric') || dt.includes('float') || dt.includes('money')) return val;
               if (dt.includes('date') || dt.includes('time')) return `'${esc}'`;
               if (dt.includes('bit')) return (val.toLowerCase() === 'true' || val === '1') ? '1' : '0';
               return `N'${esc}'`;
          };

          const formatInValues = (val, dataType) => {
               if (!val || !val.trim()) return 'NULL';
               return val.split(',').map(s => formatValue(s.trim(), dataType)).filter(Boolean).join(', ') || 'NULL';
          };

          const isParameterReference = (val) => val && /^@[a-zA-Z_][a-zA-Z0-9_]*$/.test((val || '').trim());

          const formatLikeValue = (val, dataType) => {
               if (!val) return "N'%%'";
               const t = val.trim();
               if (t.includes('%') || t.includes('_')) return formatValue(val, dataType);
               return formatValue(`%${val}%`, dataType);
          };

          const buildFilterClause = (f) => {
               const op = (f.operator || '').toLowerCase();
               const alias = resolveAlias(f.tableRef || f.tableName);
               const colRef = `[${alias}].[${f.columnName}]`;
               if (op === 'isnull' || op === 'isnotnull') return `${colRef} ${getOperatorSymbol(f.operator)}`;
               if (op === 'in' || op === 'notin') {
                    if (isParameterReference(f.value)) return `${colRef} ${getOperatorSymbol(f.operator)} (${f.value.trim()})`;
                    return `${colRef} ${getOperatorSymbol(f.operator)} (${formatInValues(f.value, f.dataType)})`;
               }
               if (op === 'like' || op === 'notlike') {
                    if (isParameterReference(f.value)) return `${colRef} ${getOperatorSymbol(f.operator)} N'%'+${f.value.trim()}+N'%'`;
                    return `${colRef} ${getOperatorSymbol(f.operator)} ${formatLikeValue(f.value, f.dataType)}`;
               }
               if (isParameterReference(f.value)) return `${colRef} ${getOperatorSymbol(f.operator)} ${f.value.trim()}`;
               return `${colRef} ${getOperatorSymbol(f.operator)} ${formatValue(f.value, f.dataType)}`;
          };

          const getAggregateSql = (agg, alias, col) => {
               const ref = `[${alias}].[${col}]`;
               const a = (agg || '').toUpperCase();
               if (a === 'COUNT') return `COUNT(${ref})`;
               if (a === 'MAX') return `MAX(${ref})`;
               if (a === 'MIN') return `MIN(${ref})`;
               if (a === 'AVG') return `AVG(${ref})`;
               if (a === 'SUM') return `SUM(${ref})`;
               if (a === 'COUNTDISTINCT') return `COUNT(DISTINCT ${ref})`;
               if (a === 'AVGDISTINCT') return `AVG(DISTINCT ${ref})`;
               if (a === 'SUMDISTINCT') return `SUM(DISTINCT ${ref})`;
               return ref;
          };

          const hasGroupBy = columns.some(c => c.groupBy);
          const hasAggregate = columns.some(c => c.aggregate);
          const useShaping = hasGroupBy || hasAggregate;

          if (useShaping && columns.length > 0) {
               const allShaped = columns.every(c => c.groupBy || c.aggregate);
               if (!allShaped) return ''; // invalid mix
          }

          let selectClauses = [];
          if (columns.length > 0 && useShaping) {
               columns.forEach(c => {
                    const alias = resolveAlias(c.tableRef || c.tableName);
                    const aliasName = `${alias}_${c.columnName}`;
                    const expr = c.aggregate ? getAggregateSql(c.aggregate, alias, c.columnName) : `[${alias}].[${c.columnName}]`;
                    selectClauses.push(`${expr} AS [${aliasName}]`);
               });
          } else {
               const selectedCols = [];
               tables.forEach(t => {
                    const alias = tableToAlias[t.boxId];
                    (t.columns || []).filter(c => c.isSelected).forEach(col => {
                         const colName = col.columnName || col.name;
                         selectedCols.push(`[${alias}].[${colName}] AS [${alias}_${colName}]`);
                    });
               });
               if (selectedCols.length === 0) return '';
               selectClauses = selectedCols;
          }

          const first = tables[0];
          const firstKey = first.boxId;
          let sql = 'SELECT\n    ' + selectClauses.join(',\n    ') + '\n';
          sql += `FROM [${first.schema}].[${first.name}] AS [${tableToAlias[firstKey]}]\n`;

          const joined = new Set([firstKey]);
          let rels = relations.filter(r =>
               (r.sourceBoxId && r.targetBoxId && tables.some(t => t.boxId === r.sourceBoxId) && tables.some(t => t.boxId === r.targetBoxId)) ||
               (r.sourceTable && r.targetTable && tables.some(t => `${t.schema}.${t.name}` === r.sourceTable) && tables.some(t => `${t.schema}.${t.name}` === r.targetTable)));
          while (rels.length) {
               let added = false;
               for (let i = 0; i < rels.length; i++) {
                    const r = rels[i];
                    const srcBox = r.sourceBoxId || (tables.find(t => `${t.schema}.${t.name}` === r.sourceTable)?.boxId);
                    const tgtBox = r.targetBoxId || (tables.find(t => `${t.schema}.${t.name}` === r.targetTable)?.boxId);
                    const srcIn = joined.has(srcBox);
                    const tgtIn = joined.has(tgtBox);
                    if (srcIn && tgtIn) { rels.splice(i, 1); added = true; i--; continue; }
                    if (srcIn && !tgtIn) {
                         const t = tables.find(t => t.boxId === tgtBox);
                         if (t && tableToAlias[srcBox] && tableToAlias[tgtBox]) {
                              sql += `${getJoinKeyword(r.joinType || r.JoinType)} [${t.schema}].[${t.name}] AS [${tableToAlias[tgtBox]}]\n`;
                              sql += `    ON [${tableToAlias[srcBox]}].[${r.sourceColumn}] = [${tableToAlias[tgtBox]}].[${r.targetColumn}]\n`;
                              joined.add(tgtBox); rels.splice(i, 1); added = true; i--;
                         }
                         continue;
                    }
                    if (tgtIn && !srcIn) {
                         const t = tables.find(t => t.boxId === srcBox);
                         if (t && tableToAlias[srcBox] && tableToAlias[tgtBox]) {
                              sql += `${getJoinKeyword(r.joinType || r.JoinType)} [${t.schema}].[${t.name}] AS [${tableToAlias[srcBox]}]\n`;
                              sql += `    ON [${tableToAlias[srcBox]}].[${r.sourceColumn}] = [${tableToAlias[tgtBox]}].[${r.targetColumn}]\n`;
                              joined.add(srcBox); rels.splice(i, 1); added = true; i--;
                         }
                    }
               }
               if (!added) break;
          }

          if (filters.length > 0) {
               sql += 'WHERE\n';
               sql += filters.map((f, i) => (i > 0 && f.logicalOperator ? `    ${f.logicalOperator} ${buildFilterClause(f)}` : `    ${buildFilterClause(f)}`)).join('\n') + '\n';
          }

          if (useShaping && hasGroupBy && columns.some(c => c.groupBy)) {
               const gb = columns.filter(c => c.groupBy).map(c => `    [${resolveAlias(c.tableRef || c.tableName)}].[${c.columnName}]`).join(',\n');
               sql += 'GROUP BY\n' + gb + '\n';
          }

          const sortCols = columns.filter(c => c.sortDirection && (c.sortDirection === 'Ascending' || c.sortDirection === 'Descending' || c.sortDirection === '0' || c.sortDirection === '1'));
          if (sortCols.length > 0) {
               sortCols.sort((a, b) => (a.sortOrder || 999) - (b.sortOrder || 999));
               const dir = c => (c.sortDirection === 'Descending' || c.sortDirection === '1') ? 'DESC' : 'ASC';
               const ob = sortCols.map(c => {
                    const alias = resolveAlias(c.tableRef || c.tableName);
                    if (c.aggregate) return `    ${getAggregateSql(c.aggregate, alias, c.columnName)} ${dir(c)}`;
                    return `    [${alias}].[${c.columnName}] ${dir(c)}`;
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
                    if (this.mode === 'write') {
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
               systemType: this.mapDbTypeToSystemType(null, types[i] || 'nvarchar')
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
          this._writeModeColumns.forEach((col, index) => {
               const opts = systemTypeOptions.map(o =>
                    `<option value="${o.value}" ${(col.systemType === o.value) ? 'selected' : ''}>${o.label}</option>`
               ).join('');
               const $row = $(`
                    <tr>
                         <td>${col.columnName}</td>
                         <td><input type="text" class="form-control form-control-sm" value="${(col.displayName || '').replace(/"/g, '&quot;')}" data-index="${index}" data-field="displayName"></td>
                         <td><select class="form-select form-select-sm" data-index="${index}" data-field="systemType">${opts}</select></td>
                    </tr>
               `);
               $row.find('input, select').on('change', (e) => {
                    const idx = $(e.target).data('index');
                    const field = $(e.target).data('field');
                    const val = $(e.target).val();
                    if (field === 'displayName') this._writeModeColumns[idx].displayName = val;
                    else if (field === 'systemType') this._writeModeColumns[idx].systemType = val || 'String';
               });
               $tbody.append($row);
          });
     }

     getQueryEditorForMode() {
          return this.mode === 'write' ? $('#queryEditorWrite') : $('#queryEditor');
     }

     async validateQueryWrite() {
          const query = $('#queryEditorWrite').val().trim();
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
          const query = $('#queryEditorWrite').val().trim();
          if (!query) { this.showWarning('Query خالی است'); return; }
          await this.doPreviewWithParams(query);
     }

     async saveReportWrite() {
          const name = $('#reportName').val().trim();
          const title = $('#reportTitle').val().trim();
          const query = $('#queryEditorWrite').val().trim();
          if (!name) { this.showError('نام گزارش را وارد کنید'); return; }
          if (!title) { this.showError('عنوان گزارش را وارد کنید'); return; }
          if (!query) { this.showError('Query را وارد کنید'); return; }
          if (!this._writeModePreviewed || !this._writeModeColumns?.length) {
               this.showError('قبل از ذخیره حتماً یک بار پیش‌نمایش بگیرید تا ستون‌ها تعریف شوند');
               return;
          }
          const columns = this._writeModeColumns.map(c => ({
               columnName: c.columnName,
               displayName: c.displayName || c.columnName,
               systemType: c.systemType || 'String'
          }));
          try {
               this.showLoading();
               const design = {
                    name: name,
                    title: title,
                    queryDesign: { tables: [], relations: [], filters: [], customQuery: query, parameters: this.parameters },
                    columns: columns
               };
               const url = this.editMode && this.reportId
                    ? `UpdateReport?id=${this.reportId}`
                    : 'SaveReport';
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
                    setTimeout(() => { window.location.href = ' Reports'; }, 1500);
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

          try {
               this.showLoading();

               const design = {
                    name: name,
                    title: title,
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

                    setTimeout(() => {
                         window.location.href = ' Reports';
                    }, 1500);
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

               this.hideLoading();

               if (response.isSuccess) {
                    const design = response.data;

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
                         if (!col.systemType) {
                              const tbl = this.selectedTables.find(t => t.boxId === col.tableRef || `${t.schema}.${t.name}` === (c.tableName || '').trim());
                              const tblCol = tbl?.columns?.find(x => (x.columnName || x.Name) === c.columnName);
                              col.systemType = this.mapDbTypeToSystemType(tblCol?.mappedSystemType, tblCol?.dataType);
                         }
                         return col;
                    });

                    if (design.queryDesign.customQuery) {
                         $('#queryEditor').val(design.queryDesign.customQuery).trigger('input');
                         $('#queryEditorWrite').val(design.queryDesign.customQuery).trigger('input');
                         if (tables.length === 0 && (design.columns || []).length > 0) {
                              this._writeModeColumns = (design.columns || []).map(c => ({
                                   columnName: c.columnName,
                                   displayName: c.displayName || c.columnName,
                                   systemType: c.systemType || 'String'
                              }));
                              this._writeModePreviewed = true;
                              this.updateWriteModeColumnsTable();
                              $('#writeModeColumnsSection').removeClass('d-none');
                         }
                    } else {
                         await this.generateQuery();
                    }

                    this.updateColumnsTable();
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