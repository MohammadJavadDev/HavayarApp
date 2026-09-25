/*
Seed_SrvTicketHotelRequest_DataProfiles.sql
نمایه داده درخواست بلیط و هتل (HTS صفحه 417 / Srv_Mission). UTF-8 BOM. Idempotent.
چهار نمایه با ستون‌های یکسان؛ فیلتر نقش در CustomQuery.
Unit: DepartmentId = system.User.OrgUnitId (ستون واقعی؛ OrganizationUnitId روی جدول User نیست).
sqlcmd -S <server> -d HavayarApp -C -b -I -x -f 65001 -i "Data\Scripts\Seed_SrvTicketHotelRequest_DataProfiles.sql"
(-x disables sqlcmd $ variable substitution; $$.post is also rebuilt via CHAR(36).)
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-srv-tickethotel-dataprofile';
    DECLARE @QueryJsonSkeleton NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}';
    DECLARE @Ent NVARCHAR(200) = N'entities.app.srv.tickethotelrequest';
    DECLARE @Pascal NVARCHAR(200) = N'Entities.App.Srv.TicketHotelRequest';
    DECLARE @Cols NVARCHAR(MAX) = N'[{"TableName":"Srv.TicketHotelRequest","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SortDirection":1,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":70,"ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SystemType":6},{"TableName":"Srv.TicketHotelRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data){return (data===true||data===1||data===''true''||data===''1'')?''<i class=\\\"ki-outline ki-check text-success\\\"></i>'':'''';}","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":70,"ColumnName":"t1_IsApproved","DisplayName":"تایید","Alliance":"t1_IsApproved","Address":"[t1].[IsApproved]","SystemTypeName":"Boolean","SystemType":1},{"TableName":"Srv.TicketHotelRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Srv.Enums.RequestTypeEnum"},"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":140,"ColumnName":"t1_RequestTypeId","DisplayName":"نوع درخواست","Alliance":"t1_RequestTypeId","Address":"[t1].[RequestTypeId]","SystemTypeName":"Select","SystemType":8},{"TableName":"Srv.TicketHotelRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Srv.Enums.ServiceTypeEnum"},"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":110,"ColumnName":"t1_ServiceTypeId","DisplayName":"نوع خدمات","Alliance":"t1_ServiceTypeId","Address":"[t1].[ServiceTypeId]","SystemTypeName":"Select","SystemType":8},{"TableName":"Srv.TicketHotelRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":120,"ColumnName":"t1_ServiceTypeText","DisplayName":"متن نوع خدمات","Alliance":"t1_ServiceTypeText","Address":"[t1].[ServiceTypeText]","SystemTypeName":"String","SystemType":0},{"TableName":"Srv.TicketHotelRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Srv.Enums.RequestReasonEnum"},"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":120,"ColumnName":"t1_RequestReasonId","DisplayName":"علت درخواست","Alliance":"t1_RequestReasonId","Address":"[t1].[RequestReasonId]","SystemTypeName":"Select","SystemType":8},{"TableName":"Srv.TicketHotelRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":120,"ColumnName":"t1_RequestReasonText","DisplayName":"متن علت","Alliance":"t1_RequestReasonText","Address":"[t1].[RequestReasonText]","SystemTypeName":"String","SystemType":0},{"TableName":"Srv.TicketHotelRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":110,"ColumnName":"t1_MissionNumber","DisplayName":"شماره ماموریت","Alliance":"t1_MissionNumber","Address":"[t1].[MissionNumber]","SystemTypeName":"String","SystemType":0},{"TableName":"Srv.TicketHotelRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data){return (data===true||data===1||data===''true''||data===''1'')?''<i class=\\\"ki-outline ki-check text-success\\\"></i>'':'''';}","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":110,"ColumnName":"t1_IsRelatedToProject","DisplayName":"مرتبط با پروژه","Alliance":"t1_IsRelatedToProject","Address":"[t1].[IsRelatedToProject]","SystemTypeName":"Boolean","SystemType":1},{"TableName":"Gnr.Party","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":180,"ColumnName":"t2_FullName","DisplayName":"شرکت","Alliance":"t2_FullName","Address":"[t2].[FullName]","SystemTypeName":"String","SystemType":0},{"TableName":"Gnr.CostCenter","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":140,"ColumnName":"t3_Title","DisplayName":"مرکز هزینه","Alliance":"t3_Title","Address":"[t3].[Title]","SystemTypeName":"String","SystemType":0},{"TableName":"FIN.DL","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":220,"ColumnName":"t5_ProjectDl","DisplayName":"پروژه","Alliance":"t5_ProjectDl","Address":"CONCAT(ISNULL([t5].[Title], N''''), N'' | '', ISNULL(CAST([t5].[HamkaranId] AS nvarchar(50)), N''''))","SystemTypeName":"String","SystemType":0},{"TableName":"Srv.TicketHotelRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":180,"ColumnName":"t1_ApplicantsInText","DisplayName":"متقاضیان","Alliance":"t1_ApplicantsInText","Address":"[t1].[ApplicantsInText]","SystemTypeName":"String","SystemType":0},{"TableName":"Hrm.OrgUnit","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":140,"ColumnName":"t4_Title","DisplayName":"واحد","Alliance":"t4_Title","Address":"[t4].[Title]","SystemTypeName":"String","SystemType":0},{"TableName":"Srv.TicketHotelRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":100,"ColumnName":"t1_DepartureSource","DisplayName":"مبدا رفت","Alliance":"t1_DepartureSource","Address":"[t1].[DepartureSource]","SystemTypeName":"String","SystemType":0},{"TableName":"Srv.TicketHotelRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":100,"ColumnName":"t1_DepartureDestination","DisplayName":"مقصد رفت","Alliance":"t1_DepartureDestination","Address":"[t1].[DepartureDestination]","SystemTypeName":"String","SystemType":0},{"TableName":"Srv.TicketHotelRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":100,"ColumnName":"t1_ReturnSource","DisplayName":"مبدا برگشت","Alliance":"t1_ReturnSource","Address":"[t1].[ReturnSource]","SystemTypeName":"String","SystemType":0},{"TableName":"Srv.TicketHotelRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":100,"ColumnName":"t1_ReturnDestination","DisplayName":"مقصد برگشت","Alliance":"t1_ReturnDestination","Address":"[t1].[ReturnDestination]","SystemTypeName":"String","SystemType":0},{"TableName":"Srv.TicketHotelRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":100,"ColumnName":"t1_DepartureDate","DisplayName":"تاریخ رفت","Alliance":"t1_DepartureDate","Address":"[t1].[DepartureDate]","SystemTypeName":"String","SystemType":0},{"TableName":"Srv.TicketHotelRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":80,"ColumnName":"t1_DepartureTime","DisplayName":"ساعت رفت","Alliance":"t1_DepartureTime","Address":"[t1].[DepartureTime]","SystemTypeName":"String","SystemType":0},{"TableName":"Srv.TicketHotelRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":100,"ColumnName":"t1_ReturnDate","DisplayName":"تاریخ برگشت","Alliance":"t1_ReturnDate","Address":"[t1].[ReturnDate]","SystemTypeName":"String","SystemType":0},{"TableName":"Srv.TicketHotelRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":80,"ColumnName":"t1_ReturnTime","DisplayName":"ساعت برگشت","Alliance":"t1_ReturnTime","Address":"[t1].[ReturnTime]","SystemTypeName":"String","SystemType":0},{"TableName":"Srv.TicketHotelRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Srv.Enums.RequestStatusEnum"},"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":120,"ColumnName":"t1_RequestStatusId","DisplayName":"وضعیت درخواست","Alliance":"t1_RequestStatusId","Address":"[t1].[RequestStatusId]","SystemTypeName":"Select","SystemType":8},{"TableName":"system.User","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":130,"ColumnName":"t6_Name","DisplayName":"تاییدکننده","Alliance":"t6_Name","Address":"[t6].[Name]","SystemTypeName":"String","SystemType":0},{"TableName":"Srv.TicketHotelRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":120,"ColumnName":"t1_ApprovedDateTimeInText","DisplayName":"تاریخ تایید","Alliance":"t1_ApprovedDateTimeInText","Address":"[t1].[ApprovedDateTimeInText]","SystemTypeName":"String","SystemType":0},{"TableName":"Srv.TicketHotelRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":180,"ColumnName":"t1_Comment","DisplayName":"توضیحات","Alliance":"t1_Comment","Address":"[t1].[Comment]","SystemTypeName":"String","SystemType":0},{"TableName":"system.User","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":130,"ColumnName":"t7_Name","DisplayName":"ایجادکننده","Alliance":"t7_Name","Address":"[t7].[Name]","SystemTypeName":"String","SystemType":0},{"TableName":"Srv.TicketHotelRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":120,"ColumnName":"t1_CreatedDateInText","DisplayName":"تاریخ ایجاد","Alliance":"t1_CreatedDateInText","Address":"[t1].[CreatedDateInText]","SystemTypeName":"String","SystemType":0}]';
    -- sqlcmd treats $$ as escape; build $$ via CHAR so $$.post survives -f 65001 apply
    DECLARE @DD NVARCHAR(10) = CHAR(36) + CHAR(36);
    DECLARE @OnSelConfirm NVARCHAR(MAX) = REPLACE(N'function(ctx) {
    var $btn = (ctx.dataActionBtns && ctx.dataActionBtns.confirm)
        || (ctx.$toolbar && ctx.$toolbar.find("[data-action=\\"confirm\\"]"));
    if (!$btn || !$btn.length) return;
    var row = (!ctx.isDeselect && ctx.selectedRow) ? ctx.selectedRow : null;
    if (!row) { $btn.prop("disabled", true); return; }
    var approved = row.t1_IsApproved === true || row.t1_IsApproved === 1 || row.t1_IsApproved === "true" || row.t1_IsApproved === "1";
    $btn.prop("disabled", !!approved);
}', N'__DD__', @DD);
    DECLARE @EvConfirm NVARCHAR(MAX) = (SELECT @OnSelConfirm AS onSelectedRow, CAST(N'' AS nvarchar(max)) AS onRowAdded FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    DECLARE @BtnConfirm NVARCHAR(MAX) = REPLACE(N'[{"id":"srv_tickethotel_confirm","title":"تایید","dataActionName":"confirm","colorClass":"btn-color-success","iconClass":"ki-outline ki-check","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    var row = (ctx && ctx.selectedRow) || {};\n    var approved = row.t1_IsApproved === true || row.t1_IsApproved === 1 || row.t1_IsApproved === ''true'' || row.t1_IsApproved === ''1'';\n    if (approved) { toastr.warning(''این درخواست قبلاً تایید شده است''); return; }\n    Swal.fire({ title: ''تایید'', text: ''آیا از تایید این درخواست اطمینان دارید؟'', icon: ''warning'', showCancelButton: true, confirmButtonText: ''بله'', cancelButtonText: ''خیر'' })\n        .then(function (result) {\n            if (!result.isConfirmed) return;\n            var $btn = ctx.$btn ? $(ctx.$btn.element || ctx.$btn).block() : null;\n            __DD__.post(''/Panel/Srv/TicketHotelRequest/confirm'', { id: id }, function (r) {\n                if ($btn) $btn.block(false);\n                if (!r || !r.isSuccess) { toastr.error((r && r.message) || ''تایید انجام نشد'', ''خطا''); return; }\n                toastr.success(''تایید با موفقیت انجام شد'');\n                if (ctx.table && ctx.table.ajax) ctx.table.ajax.reload(null, false);\n                else if (typeof appController !== ''undefined'' && appController.refreshCurrentPage) appController.refreshCurrentPage();\n            });\n        });\n}"}]', N'__DD__', @DD);

    IF OBJECT_ID('tempdb..#ThrProfiles') IS NOT NULL DROP TABLE #ThrProfiles;
    CREATE TABLE #ThrProfiles (
        Name NVARCHAR(100) NOT NULL PRIMARY KEY,
        Title NVARCHAR(200) NOT NULL,
        CustomQuery NVARCHAR(MAX) NOT NULL,
        ActionOptions NVARCHAR(MAX) NOT NULL,
        CustomButtons NVARCHAR(MAX) NOT NULL,
        EventScripts NVARCHAR(MAX) NOT NULL,
        RoleCsv NVARCHAR(500) NOT NULL,
        SavedQueryId BIGINT NULL
    );

    INSERT INTO #ThrProfiles (Name, Title, CustomQuery, ActionOptions, CustomButtons, EventScripts, RoleCsv)
    VALUES (N'Srv_TicketHotelRequest_All', N'درخواست بلیط و هتل — همه', N'--!--mainsection
SELECT
    [t1].[Id] AS [t1_Id],
    [t1].[IsApproved] AS [t1_IsApproved],
    [t1].[RequestTypeId] AS [t1_RequestTypeId],
    [t1].[ServiceTypeId] AS [t1_ServiceTypeId],
    [t1].[ServiceTypeText] AS [t1_ServiceTypeText],
    [t1].[RequestReasonId] AS [t1_RequestReasonId],
    [t1].[RequestReasonText] AS [t1_RequestReasonText],
    [t1].[MissionNumber] AS [t1_MissionNumber],
    [t1].[IsRelatedToProject] AS [t1_IsRelatedToProject],
    [t2].[FullName] AS [t2_FullName],
    [t3].[Title] AS [t3_Title],
    CASE WHEN [t5].[Id] IS NULL THEN NULL
         ELSE CONCAT(ISNULL([t5].[Title], N''''), N'' | '', ISNULL(CAST([t5].[HamkaranId] AS nvarchar(50)), N''''))
    END AS [t5_ProjectDl],
    [t1].[ApplicantsInText] AS [t1_ApplicantsInText],
    [t4].[Title] AS [t4_Title],
    [t1].[DepartureSource] AS [t1_DepartureSource],
    [t1].[DepartureDestination] AS [t1_DepartureDestination],
    [t1].[ReturnSource] AS [t1_ReturnSource],
    [t1].[ReturnDestination] AS [t1_ReturnDestination],
    [t1].[DepartureDate] AS [t1_DepartureDate],
    [t1].[DepartureTime] AS [t1_DepartureTime],
    [t1].[ReturnDate] AS [t1_ReturnDate],
    [t1].[ReturnTime] AS [t1_ReturnTime],
    [t1].[RequestStatusId] AS [t1_RequestStatusId],
    [t6].[Name] AS [t6_Name],
    [t1].[ApprovedDateTimeInText] AS [t1_ApprovedDateTimeInText],
    [t1].[Comment] AS [t1_Comment],
    [t7].[Name] AS [t7_Name],
    [t1].[CreatedDateInText] AS [t1_CreatedDateInText]
FROM [Srv].[TicketHotelRequest] AS [t1]
LEFT JOIN [Gnr].[Party] AS [t2] ON [t2].[Id] = [t1].[ManCompanyId]
LEFT JOIN [Gnr].[CostCenter] AS [t3] ON [t3].[Id] = [t1].[CostCenterId]
LEFT JOIN [Hrm].[OrgUnit] AS [t4] ON [t4].[Id] = [t1].[DepartmentId]
LEFT JOIN [FIN].[DL] AS [t5] ON [t5].[Id] = [t1].[ProjectDlId]
LEFT JOIN [system].[User] AS [t6] ON [t6].[Id] = [t1].[ApproverId]
LEFT JOIN [system].[User] AS [t7] ON [t7].[Id] = [t1].[CreatedById]', N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]', N'[]', N'{"onSelectedRow":"","onRowAdded":""}', N'Srv.TicketHotel.FullAccess,ShowAllMenus');
    INSERT INTO #ThrProfiles (Name, Title, CustomQuery, ActionOptions, CustomButtons, EventScripts, RoleCsv)
    VALUES (N'Srv_TicketHotelRequest_AllConfirm', N'درخواست بلیط و هتل — همه با تایید', N'--!--mainsection
SELECT
    [t1].[Id] AS [t1_Id],
    [t1].[IsApproved] AS [t1_IsApproved],
    [t1].[RequestTypeId] AS [t1_RequestTypeId],
    [t1].[ServiceTypeId] AS [t1_ServiceTypeId],
    [t1].[ServiceTypeText] AS [t1_ServiceTypeText],
    [t1].[RequestReasonId] AS [t1_RequestReasonId],
    [t1].[RequestReasonText] AS [t1_RequestReasonText],
    [t1].[MissionNumber] AS [t1_MissionNumber],
    [t1].[IsRelatedToProject] AS [t1_IsRelatedToProject],
    [t2].[FullName] AS [t2_FullName],
    [t3].[Title] AS [t3_Title],
    CASE WHEN [t5].[Id] IS NULL THEN NULL
         ELSE CONCAT(ISNULL([t5].[Title], N''''), N'' | '', ISNULL(CAST([t5].[HamkaranId] AS nvarchar(50)), N''''))
    END AS [t5_ProjectDl],
    [t1].[ApplicantsInText] AS [t1_ApplicantsInText],
    [t4].[Title] AS [t4_Title],
    [t1].[DepartureSource] AS [t1_DepartureSource],
    [t1].[DepartureDestination] AS [t1_DepartureDestination],
    [t1].[ReturnSource] AS [t1_ReturnSource],
    [t1].[ReturnDestination] AS [t1_ReturnDestination],
    [t1].[DepartureDate] AS [t1_DepartureDate],
    [t1].[DepartureTime] AS [t1_DepartureTime],
    [t1].[ReturnDate] AS [t1_ReturnDate],
    [t1].[ReturnTime] AS [t1_ReturnTime],
    [t1].[RequestStatusId] AS [t1_RequestStatusId],
    [t6].[Name] AS [t6_Name],
    [t1].[ApprovedDateTimeInText] AS [t1_ApprovedDateTimeInText],
    [t1].[Comment] AS [t1_Comment],
    [t7].[Name] AS [t7_Name],
    [t1].[CreatedDateInText] AS [t1_CreatedDateInText]
FROM [Srv].[TicketHotelRequest] AS [t1]
LEFT JOIN [Gnr].[Party] AS [t2] ON [t2].[Id] = [t1].[ManCompanyId]
LEFT JOIN [Gnr].[CostCenter] AS [t3] ON [t3].[Id] = [t1].[CostCenterId]
LEFT JOIN [Hrm].[OrgUnit] AS [t4] ON [t4].[Id] = [t1].[DepartmentId]
LEFT JOIN [FIN].[DL] AS [t5] ON [t5].[Id] = [t1].[ProjectDlId]
LEFT JOIN [system].[User] AS [t6] ON [t6].[Id] = [t1].[ApproverId]
LEFT JOIN [system].[User] AS [t7] ON [t7].[Id] = [t1].[CreatedById]', N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]', @BtnConfirm, @EvConfirm, N'Srv.TicketHotel.FullAccessConfirm,ShowAllMenus');
    INSERT INTO #ThrProfiles (Name, Title, CustomQuery, ActionOptions, CustomButtons, EventScripts, RoleCsv)
    VALUES (N'Srv_TicketHotelRequest_Unit', N'درخواست بلیط و هتل — واحد سازمانی', N'DECLARE @CuId BIGINT = TRY_CAST(@CurrentUserId AS BIGINT);
--!--mainsection
SELECT
    [t1].[Id] AS [t1_Id],
    [t1].[IsApproved] AS [t1_IsApproved],
    [t1].[RequestTypeId] AS [t1_RequestTypeId],
    [t1].[ServiceTypeId] AS [t1_ServiceTypeId],
    [t1].[ServiceTypeText] AS [t1_ServiceTypeText],
    [t1].[RequestReasonId] AS [t1_RequestReasonId],
    [t1].[RequestReasonText] AS [t1_RequestReasonText],
    [t1].[MissionNumber] AS [t1_MissionNumber],
    [t1].[IsRelatedToProject] AS [t1_IsRelatedToProject],
    [t2].[FullName] AS [t2_FullName],
    [t3].[Title] AS [t3_Title],
    CASE WHEN [t5].[Id] IS NULL THEN NULL
         ELSE CONCAT(ISNULL([t5].[Title], N''''), N'' | '', ISNULL(CAST([t5].[HamkaranId] AS nvarchar(50)), N''''))
    END AS [t5_ProjectDl],
    [t1].[ApplicantsInText] AS [t1_ApplicantsInText],
    [t4].[Title] AS [t4_Title],
    [t1].[DepartureSource] AS [t1_DepartureSource],
    [t1].[DepartureDestination] AS [t1_DepartureDestination],
    [t1].[ReturnSource] AS [t1_ReturnSource],
    [t1].[ReturnDestination] AS [t1_ReturnDestination],
    [t1].[DepartureDate] AS [t1_DepartureDate],
    [t1].[DepartureTime] AS [t1_DepartureTime],
    [t1].[ReturnDate] AS [t1_ReturnDate],
    [t1].[ReturnTime] AS [t1_ReturnTime],
    [t1].[RequestStatusId] AS [t1_RequestStatusId],
    [t6].[Name] AS [t6_Name],
    [t1].[ApprovedDateTimeInText] AS [t1_ApprovedDateTimeInText],
    [t1].[Comment] AS [t1_Comment],
    [t7].[Name] AS [t7_Name],
    [t1].[CreatedDateInText] AS [t1_CreatedDateInText]
FROM [Srv].[TicketHotelRequest] AS [t1]
LEFT JOIN [Gnr].[Party] AS [t2] ON [t2].[Id] = [t1].[ManCompanyId]
LEFT JOIN [Gnr].[CostCenter] AS [t3] ON [t3].[Id] = [t1].[CostCenterId]
LEFT JOIN [Hrm].[OrgUnit] AS [t4] ON [t4].[Id] = [t1].[DepartmentId]
LEFT JOIN [FIN].[DL] AS [t5] ON [t5].[Id] = [t1].[ProjectDlId]
LEFT JOIN [system].[User] AS [t6] ON [t6].[Id] = [t1].[ApproverId]
LEFT JOIN [system].[User] AS [t7] ON [t7].[Id] = [t1].[CreatedById]
WHERE [t1].[DepartmentId] = (SELECT [OrgUnitId] FROM [system].[User] WHERE [Id] = @CuId)', N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]', @BtnConfirm, @EvConfirm, N'Srv.TicketHotel.Approver,ShowAllMenus');
    INSERT INTO #ThrProfiles (Name, Title, CustomQuery, ActionOptions, CustomButtons, EventScripts, RoleCsv)
    VALUES (N'Srv_TicketHotelRequest_Own', N'درخواست بلیط و هتل — درخواست‌های من', N'--!--mainsection
SELECT
    [t1].[Id] AS [t1_Id],
    [t1].[IsApproved] AS [t1_IsApproved],
    [t1].[RequestTypeId] AS [t1_RequestTypeId],
    [t1].[ServiceTypeId] AS [t1_ServiceTypeId],
    [t1].[ServiceTypeText] AS [t1_ServiceTypeText],
    [t1].[RequestReasonId] AS [t1_RequestReasonId],
    [t1].[RequestReasonText] AS [t1_RequestReasonText],
    [t1].[MissionNumber] AS [t1_MissionNumber],
    [t1].[IsRelatedToProject] AS [t1_IsRelatedToProject],
    [t2].[FullName] AS [t2_FullName],
    [t3].[Title] AS [t3_Title],
    CASE WHEN [t5].[Id] IS NULL THEN NULL
         ELSE CONCAT(ISNULL([t5].[Title], N''''), N'' | '', ISNULL(CAST([t5].[HamkaranId] AS nvarchar(50)), N''''))
    END AS [t5_ProjectDl],
    [t1].[ApplicantsInText] AS [t1_ApplicantsInText],
    [t4].[Title] AS [t4_Title],
    [t1].[DepartureSource] AS [t1_DepartureSource],
    [t1].[DepartureDestination] AS [t1_DepartureDestination],
    [t1].[ReturnSource] AS [t1_ReturnSource],
    [t1].[ReturnDestination] AS [t1_ReturnDestination],
    [t1].[DepartureDate] AS [t1_DepartureDate],
    [t1].[DepartureTime] AS [t1_DepartureTime],
    [t1].[ReturnDate] AS [t1_ReturnDate],
    [t1].[ReturnTime] AS [t1_ReturnTime],
    [t1].[RequestStatusId] AS [t1_RequestStatusId],
    [t6].[Name] AS [t6_Name],
    [t1].[ApprovedDateTimeInText] AS [t1_ApprovedDateTimeInText],
    [t1].[Comment] AS [t1_Comment],
    [t7].[Name] AS [t7_Name],
    [t1].[CreatedDateInText] AS [t1_CreatedDateInText]
FROM [Srv].[TicketHotelRequest] AS [t1]
LEFT JOIN [Gnr].[Party] AS [t2] ON [t2].[Id] = [t1].[ManCompanyId]
LEFT JOIN [Gnr].[CostCenter] AS [t3] ON [t3].[Id] = [t1].[CostCenterId]
LEFT JOIN [Hrm].[OrgUnit] AS [t4] ON [t4].[Id] = [t1].[DepartmentId]
LEFT JOIN [FIN].[DL] AS [t5] ON [t5].[Id] = [t1].[ProjectDlId]
LEFT JOIN [system].[User] AS [t6] ON [t6].[Id] = [t1].[ApproverId]
LEFT JOIN [system].[User] AS [t7] ON [t7].[Id] = [t1].[CreatedById]
WHERE [t1].[CreatedById] = TRY_CAST(@CurrentUserId AS BIGINT)', N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]', N'[]', N'{"onSelectedRow":"","onRowAdded":""}', N'Srv.TicketHotel.Requester,Srv.TicketHotel.View,ShowAllMenus');

    DECLARE @Name NVARCHAR(100), @Title NVARCHAR(200), @Sel NVARCHAR(MAX);
    DECLARE @Act NVARCHAR(MAX), @Btn NVARCHAR(MAX), @Ev NVARCHAR(MAX), @RoleCsv NVARCHAR(500);
    DECLARE @Qj NVARCHAR(MAX), @Pid BIGINT;

    DECLARE pc CURSOR LOCAL FAST_FORWARD FOR
        SELECT Name, Title, CustomQuery, ActionOptions, CustomButtons, EventScripts, RoleCsv FROM #ThrProfiles;
    OPEN pc;
    FETCH NEXT FROM pc INTO @Name, @Title, @Sel, @Act, @Btn, @Ev, @RoleCsv;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @Qj = JSON_MODIFY(@QueryJsonSkeleton, '$.CustomQuery', @Sel);
        IF ISJSON(@Cols) <> 1 OR ISJSON(@Qj) <> 1 OR ISJSON(@Act) <> 1 OR ISJSON(@Btn) <> 1 OR ISJSON(@Ev) <> 1
            THROW 51710, N'JSON نمایه درخواست بلیط و هتل معتبر نیست.', 1;

        IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @Name)
        BEGIN
            UPDATE system.SavedQuery SET Title=@Title, QueryJson=@Qj, ColumnsJson=@Cols, EntityFullName=@Ent, Mode=1, Type=1,
                ActionOptions=@Act, CustomActionButtonsJson=@Btn, EventScriptsJson=@Ev, DiagramJson=N'{}',
                ModifiedById=1, ModifiedByName=@SeedUser, ModifiedDateMiladiDateTime=@Now, ModifiedDateShamsiDateTime=@NowShamsi, IsActive=1
            WHERE Name=@Name;
            SELECT @Pid = Id FROM system.SavedQuery WHERE Name=@Name;
            PRINT N'  UPDATED ' + @Name;
        END
        ELSE
        BEGIN
            INSERT INTO system.SavedQuery
                (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
                 Mode, Type, EntityFullName, ActionOptions,
                 CreatedById, ModifiedById, CreatedByName, ModifiedByName,
                 CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
            VALUES (@Name, @Title, @Qj, @Cols, N'{}', @Btn, @Ev,
                 1, 1, @Ent, @Act, 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
            SET @Pid = SCOPE_IDENTITY();
            PRINT N'  CREATED ' + @Name;
        END

        UPDATE #ThrProfiles SET SavedQueryId = @Pid WHERE Name = @Name;

        DELETE ra FROM system.RoleAccess ra WHERE ra.ActionAccessType=3 AND ra.RowId=@Pid;
        INSERT INTO system.RoleAccess
            (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        SELECT N'dataProfile_' + CAST(@Pid AS nvarchar(20)), 3, 7, @Pascal, @Title, NULL, @Pid, r.Id,
            1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
        FROM system.Role r
        WHERE r.Name IN (SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(@RoleCsv, N','));
        PRINT N'  Profile RoleAccess rows: ' + CAST(@@ROWCOUNT AS nvarchar(20)) + N' for ' + @Name;

        FETCH NEXT FROM pc INTO @Name, @Title, @Sel, @Act, @Btn, @Ev, @RoleCsv;
    END
    CLOSE pc; DEALLOCATE pc;

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_SrvTicketHotelRequest_DataProfiles ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT Id, Name, Title FROM system.SavedQuery WHERE Name LIKE N'Srv_TicketHotelRequest_%' ORDER BY Name;
SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance, JSON_VALUE(j.value, '$.DisplayName') AS DisplayName
FROM system.SavedQuery q CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'Srv_TicketHotelRequest_All'
ORDER BY CAST(j.[key] AS int);
