/*
نمایه و دسترسی مدیریت درخواست نیروی انسانی.
UTF-8 with BOM.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @Seed NVARCHAR(80) = N'seed-hrm-staff-request';
DECLARE @Query NVARCHAR(MAX) = N'DECLARE @CuId BIGINT = TRY_CAST(@CurrentUserId AS BIGINT);
DECLARE @Roles NVARCHAR(MAX) = (SELECT Roles FROM [system].[User] WHERE Id = @CuId);
DECLARE @IsFull BIT = CASE WHEN EXISTS (SELECT 1 FROM OPENJSON(ISNULL(@Roles, N''[]'')) j WHERE j.value IN (N''admin'', N''Admin'', N''مدیرسیستم جذب واستخدام'')) THEN 1 ELSE 0 END;
DECLARE @IsViewer BIT = CASE WHEN EXISTS (SELECT 1 FROM OPENJSON(ISNULL(@Roles, N''[]'')) j WHERE j.value = N''دسترسی مشاهده-سیستم جذب و استخدام'') THEN 1 ELSE 0 END;
DECLARE @IsOwn BIT = CASE WHEN @IsFull = 0 AND @IsViewer = 0 AND EXISTS (SELECT 1 FROM OPENJSON(ISNULL(@Roles, N''[]'')) j WHERE j.value IN (N''درخواست کنندگان نیرو'', N''کارشناسان سیستم مهندسی دارای دسترسی سیستم منابع انسانی'')) THEN 1 ELSE 0 END;
--!--mainsection
SELECT [t1].[Id] AS [t1_Id], [t1].[HtsId] AS [t1_HtsId], [t1].[HasManagerAccept] AS [t1_HasManagerAccept],
 [ou].[Title] AS [t1_Org], LTRIM(RTRIM(CONCAT(ISNULL([p].[Name], N''''), N'' '', ISNULL([p].[Family], N'''')))) AS [t1_Person],
 CASE [t1].[Status] WHEN 76 THEN N''در جریان'' WHEN 77 THEN N''انجام شده'' WHEN 78 THEN N''آرشیو'' WHEN 80 THEN N''کنسل'' WHEN 1773 THEN N''در حال بررسی توسط منابع انسانی'' WHEN 2539 THEN N''هولد شده'' ELSE N'''' END AS [t1_Status],
 [j].[Title] AS [t1_Job], [t1].[RequiredNumber] AS [t1_RequiredNumber],
 CASE [t1].[CooperationType] WHEN 40 THEN N''تمام وقت'' WHEN 41 THEN N''پاره وقت'' WHEN 42 THEN N''نیمه وقت'' ELSE N'''' END AS [t1_Cooperation],
 CASE [t1].[EmploymentKind] WHEN 50 THEN N''نیروی جدید'' WHEN 51 THEN N''نیروی جایگزین'' ELSE N'''' END AS [t1_EmploymentKind],
 CASE [t1].[Workplace] WHEN 43 THEN N''دفتر مرکزی'' WHEN 44 THEN N''کارخانه'' ELSE N'''' END AS [t1_Workplace],
 [t1].[RequestShamsiDate] AS [t1_RequestDate], [t1].[TasksExpected] AS [t1_Tasks], [t1].[Comment] AS [t1_Comment]
FROM [Hrm].[StaffRequest] AS [t1]
LEFT JOIN [Hrm].[OrgUnit] AS [ou] ON [ou].[Id] = [t1].[RequestOrgUnitId]
LEFT JOIN [Hcm].[Personel] AS [p] ON [p].[Id] = [t1].[RequestPersonelId]
LEFT JOIN [Hcm].[Job] AS [j] ON [j].[Id] = [t1].[JobId]
WHERE @IsFull = 1 OR @IsViewer = 1 OR (@IsOwn = 1 AND [t1].[RequestUserId] = @CuId)';
DECLARE @Qj NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"Parameters":[],"Selects":null}';
SET @Qj = JSON_MODIFY(@Qj, '$.CustomQuery', @Query);
DECLARE @Cols NVARCHAR(MAX) = N'[
{"TableName":"Hrm.StaffRequest","ColumnName":"t1_Id","DisplayName":"شناسه داخلی","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Hrm.StaffRequest","ColumnName":"t1_HtsId","DisplayName":"شناسه","Alliance":"t1_HtsId","Address":"[t1].[HtsId]","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Hrm.StaffRequest","ColumnName":"t1_HasManagerAccept","DisplayName":"تایید مدیر","Alliance":"t1_HasManagerAccept","Address":"[t1].[HasManagerAccept]","SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Hrm.StaffRequest","ColumnName":"t1_Org","DisplayName":"واحد درخواست‌کننده","Alliance":"t1_Org","Address":"[ou].[Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Hrm.StaffRequest","ColumnName":"t1_Person","DisplayName":"درخواست‌کننده","Alliance":"t1_Person","Address":"[p].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Hrm.StaffRequest","ColumnName":"t1_Status","DisplayName":"وضعیت","Alliance":"t1_Status","Address":"[t1].[Status]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Hrm.StaffRequest","ColumnName":"t1_Job","DisplayName":"شغل","Alliance":"t1_Job","Address":"[j].[Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":220,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Hrm.StaffRequest","ColumnName":"t1_RequiredNumber","DisplayName":"تعداد مورد نیاز","Alliance":"t1_RequiredNumber","Address":"[t1].[RequiredNumber]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Hrm.StaffRequest","ColumnName":"t1_Cooperation","DisplayName":"نوع همکاری","Alliance":"t1_Cooperation","Address":"[t1].[CooperationType]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Hrm.StaffRequest","ColumnName":"t1_EmploymentKind","DisplayName":"وضعیت جایگزینی","Alliance":"t1_EmploymentKind","Address":"[t1].[EmploymentKind]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Hrm.StaffRequest","ColumnName":"t1_Workplace","DisplayName":"محل کار","Alliance":"t1_Workplace","Address":"[t1].[Workplace]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Hrm.StaffRequest","ColumnName":"t1_RequestDate","DisplayName":"تاریخ ایجاد","Alliance":"t1_RequestDate","Address":"[t1].[RequestShamsiDate]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":1,"SortOrder":0,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Hrm.StaffRequest","ColumnName":"t1_Tasks","DisplayName":"شرح وظایف","Alliance":"t1_Tasks","Address":"[t1].[TasksExpected]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":260,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Hrm.StaffRequest","ColumnName":"t1_Comment","DisplayName":"توضیحات","Alliance":"t1_Comment","Address":"[t1].[Comment]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":220,"Filterable":true,"Sortable":true,"ClassName":""}
]';
DECLARE @Act NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
DECLARE @Btn NVARCHAR(MAX) = N'[{"id":"staff_approve","title":"تایید","dataActionName":"staffApprove","colorClass":"btn-light-success","iconClass":"ki-outline ki-check","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx){ var pg=(window.appController&&appController.activePage); if(pg&&pg.$pageEl) pg.$pageEl.trigger(''staffAction'',[''approve'', ctx]); }"}]';

BEGIN TRY
BEGIN TRANSACTION;
IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = N'Hrm_StaffRequest_List')
    UPDATE system.SavedQuery SET Title = N'مدیریت درخواست نیروی انسانی', QueryJson = @Qj, ColumnsJson = @Cols, EntityFullName = N'entities.app.hrm.staffrequest', Mode = 1, Type = 1,
        ActionOptions = @Act, CustomActionButtonsJson = @Btn, EventScriptsJson = N'{"onSelectedRow":"","onRowAdded":""}', IsActive = 1,
        ModifiedById = 1, ModifiedByName = @Seed, ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Name = N'Hrm_StaffRequest_List';
ELSE
    INSERT INTO system.SavedQuery
        (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson, Mode, Type, EntityFullName, ActionOptions,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES (N'Hrm_StaffRequest_List', N'مدیریت درخواست نیروی انسانی', @Qj, @Cols, N'{}', @Btn, N'{"onSelectedRow":"","onRowAdded":""}', 1, 1, N'entities.app.hrm.staffrequest', @Act,
         1, 1, @Seed, @Seed, @Now, @NowShamsi, @Now, @NowShamsi, 1);

DECLARE @Roles TABLE (Name NVARCHAR(200), CanWrite BIT, CanDelete BIT);
INSERT INTO @Roles VALUES
 (N'مدیرسیستم جذب واستخدام', 1, 1),
 (N'دسترسی مشاهده-سیستم جذب و استخدام', 0, 0),
 (N'درخواست کنندگان نیرو', 1, 1),
 (N'کارشناسان سیستم مهندسی دارای دسترسی سیستم منابع انسانی', 1, 0);

INSERT INTO system.Role (Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
SELECT r.Name, r.Name, 1, @Seed, 1, @Seed, @Now, @Now, @NowShamsi, @NowShamsi, 1
FROM @Roles r WHERE NOT EXISTS (SELECT 1 FROM system.Role x WHERE x.Name = r.Name);

DELETE ra FROM system.RoleAccess ra
INNER JOIN system.Role r ON r.Id = ra.RoleId
INNER JOIN @Roles n ON n.Name = r.Name
WHERE ra.Path LIKE N'/panel/hrm/staffrequest/%' OR (ra.ActionAccessType = 3 AND ra.EntityName = N'Entities.App.Hrm.StaffRequest');

INSERT INTO system.RoleAccess
    (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId, RoleId, CreatedById, ModifiedById, CreatedByName, ModifiedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
SELECT v.Path, v.AType, v.IType, N'Entities.App.Hrm.StaffRequest', v.Path, NULL, r.Id, 1, 1, @Seed, @Seed, @Now, @NowShamsi, @Now, @NowShamsi, 1
FROM system.Role r
INNER JOIN @Roles n ON n.Name = r.Name
CROSS APPLY (VALUES
    (N'/panel/hrm/staffrequest/list', 1, 1, 1),
    (N'/panel/hrm/staffrequest/fetchdata', 2, 2, 1),
    (N'/panel/hrm/staffrequest/exporttoexcel', 2, 1000, 1),
    (N'/panel/hrm/staffrequest/new', 1, 4, n.CanWrite),
    (N'/panel/hrm/staffrequest/add', 2, 4, n.CanWrite),
    (N'/panel/hrm/staffrequest/edit', 1, 5, n.CanWrite),
    (N'/panel/hrm/staffrequest/update', 2, 5, n.CanWrite),
    (N'/panel/hrm/staffrequest/delete', 2, 6, n.CanDelete)
) v(Path, AType, IType, Allow)
WHERE v.Allow = 1;

INSERT INTO system.RoleAccess
    (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId, RoleId, CreatedById, ModifiedById, CreatedByName, ModifiedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
SELECT N'dataProfile_' + CAST(q.Id AS NVARCHAR(20)), 3, 7, N'Entities.App.Hrm.StaffRequest', q.Title, q.Id, r.Id, 1, 1, @Seed, @Seed, @Now, @NowShamsi, @Now, @NowShamsi, 1
FROM system.SavedQuery q
CROSS JOIN system.Role r
INNER JOIN @Roles n ON n.Name = r.Name
WHERE q.Name = N'Hrm_StaffRequest_List';

DECLARE @Leaf NVARCHAR(MAX) = N'{"text":"مدیریت درخواست نیروی انسانی","icon":"ki-people ki-outline","iconColor":"#c084fc","path":"/panel/hrm/staffrequest/list","a_attr":{"href":"/panel/hrm/staffrequest/list"},"data":{"iconColor":"#c084fc","path":"/panel/hrm/staffrequest/list"},"children":[]}';
DECLARE @All NVARCHAR(MAX) = (SELECT Content FROM system.SystemMenu WHERE Name = N'AllMenus');
DECLARE @HrIdx INT = (SELECT TOP 1 CAST(j.[key] AS INT) FROM OPENJSON(@All) j WHERE JSON_VALUE(j.value, '$.text') = N'منابع انسانی');
DECLARE @Hr NVARCHAR(MAX) = (SELECT TOP 1 CAST(j.value AS NVARCHAR(MAX)) FROM OPENJSON(@All) j WHERE CAST(j.[key] AS INT) = @HrIdx);
DECLARE @HrChildren NVARCHAR(MAX) = JSON_QUERY(@Hr, '$.children');
DECLARE @RecIdx INT = (SELECT TOP 1 CAST(c.[key] AS INT) FROM OPENJSON(@HrChildren) c WHERE JSON_VALUE(c.value, '$.text') = N'سیستم جذب و استخدام');
DECLARE @Rec NVARCHAR(MAX) = (SELECT TOP 1 CAST(c.value AS NVARCHAR(MAX)) FROM OPENJSON(@HrChildren) c WHERE CAST(c.[key] AS INT) = @RecIdx);
DECLARE @RecChildren NVARCHAR(MAX) = JSON_QUERY(@Rec, '$.children');
DECLARE @OpIdx INT = (SELECT TOP 1 CAST(c.[key] AS INT) FROM OPENJSON(@RecChildren) c WHERE JSON_VALUE(c.value, '$.text') = N'عملیات');
DECLARE @Op NVARCHAR(MAX) = (SELECT TOP 1 CAST(c.value AS NVARCHAR(MAX)) FROM OPENJSON(@RecChildren) c WHERE CAST(c.[key] AS INT) = @OpIdx);
DECLARE @OpChildren NVARCHAR(MAX) = JSON_QUERY(@Op, '$.children');
IF @OpIdx IS NOT NULL AND NOT EXISTS (SELECT 1 FROM OPENJSON(ISNULL(@OpChildren, N'[]')) c WHERE JSON_VALUE(c.value, '$.path') = N'/panel/hrm/staffrequest/list')
BEGIN
    SET @OpChildren = JSON_MODIFY(ISNULL(@OpChildren, N'[]'), 'append $', JSON_QUERY(@Leaf));
    SET @Op = JSON_MODIFY(@Op, '$.children', JSON_QUERY(@OpChildren));
    SET @RecChildren = JSON_MODIFY(@RecChildren, '$[' + CAST(@OpIdx AS NVARCHAR(10)) + ']', JSON_QUERY(@Op));
    SET @Rec = JSON_MODIFY(@Rec, '$.children', JSON_QUERY(@RecChildren));
    SET @HrChildren = JSON_MODIFY(@HrChildren, '$[' + CAST(@RecIdx AS NVARCHAR(10)) + ']', JSON_QUERY(@Rec));
    SET @Hr = JSON_MODIFY(@Hr, '$.children', JSON_QUERY(@HrChildren));
    SET @All = JSON_MODIFY(@All, '$[' + CAST(@HrIdx AS NVARCHAR(10)) + ']', JSON_QUERY(@Hr));
    UPDATE system.SystemMenu SET Content = @All, ModifiedById = 1, ModifiedByName = @Seed, ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi WHERE Name = N'AllMenus';
END
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@Err, 16, 1);
END CATCH;

SELECT Name, Title FROM system.SavedQuery WHERE Name = N'Hrm_StaffRequest_List';
SELECT JSON_VALUE(j.value, '$.DisplayName') AS DisplayName FROM system.SavedQuery q CROSS APPLY OPENJSON(q.ColumnsJson) j WHERE q.Name = N'Hrm_StaffRequest_List' AND JSON_VALUE(j.value, '$.Alliance') = N't1_Org';
