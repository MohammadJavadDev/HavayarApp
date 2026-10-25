/*
Seed_Qc_ProductInspection_DataProfiles.sql
نمایه بازرسی محصول در هوایار. ستون تکمیل‌نشده: خانه شماره ردیف نارنجی.
نمایه کامل برای نقش‌های ویرایش (کپی و جدید + پیوست). نمایه مشاهده بدون کپی.
UTF-8 BOM. Idempotent.
sqlcmd -S 172.20.40.42 -d HavayarApp -U sa -P <pwd> -C -b -I -f 65001 -i "Data\Scripts\Seed_Qc_ProductInspection_DataProfiles.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-qc-productinspection-dataprofiles';
    DECLARE @QueryJsonSkeleton NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}';
    DECLARE @Sel NVARCHAR(MAX) = N'SELECT
    [t1].[Id] AS [t1_Id],
    [t1].[IsCompleted] AS [t1_IsCompleted],
    [t2].[Serial] AS [t2_Serial],
    [t1].[ProductionOrderNumber] AS [t1_ProductionOrderNumber],
    [t1].[ProjectTitle] AS [t1_ProjectTitle],
    [t3].[Code] AS [t3_Code],
    [t3].[Name] AS [t3_Name],
    [t1].[TestStartShamsiDate] AS [t1_TestStartShamsiDate],
    [t1].[TestStartTime] AS [t1_TestStartTime],
    [t1].[TestEndShamsiDate] AS [t1_TestEndShamsiDate],
    [t1].[TestEndTime] AS [t1_TestEndTime],
    LTRIM(RTRIM(ISNULL([t4].[Name], N'''') + N'' '' + ISNULL([t4].[Family], N''''))) AS [t4_FullName],
    [t5].[Title] AS [t5_Title],
    [t1].[InspectionResult] AS [t1_InspectionResult],
    [t1].[CreatedByName] AS [t1_CreatedByName],
    CASE WHEN [t1].[CreatedOnShamsiDateTime] IS NULL THEN NULL ELSE LEFT([t1].[CreatedOnShamsiDateTime], 10) END AS [t1_CreatedDate],
    CASE WHEN [t1].[CreatedOnShamsiDateTime] IS NULL OR LEN([t1].[CreatedOnShamsiDateTime]) < 12 THEN NULL ELSE SUBSTRING([t1].[CreatedOnShamsiDateTime], 12, 5) END AS [t1_CreatedTime],
    [t1].[ModifiedByName] AS [t1_ModifiedByName],
    [t1].[ModifiedDateShamsiDateTime] AS [t1_ModifiedDateShamsiDateTime]
FROM [Qc].[ProductInspection] AS [t1]
LEFT JOIN [Sale].[ProductionOrderItem] AS [t2] ON [t2].[Id] = [t1].[ProductionOrderItemId]
LEFT JOIN [Inv].[Part] AS [t3] ON [t3].[Id] = [t2].[PartId]
LEFT JOIN [Hcm].[Personel] AS [t4] ON [t4].[Id] = [t1].[TestPersonelId]
LEFT JOIN [Hrm].[OrgUnit] AS [t5] ON [t5].[Id] = [t1].[SalesDepartmentId]
WHERE [t1].[InspectionType] = 469';
    DECLARE @Cols NVARCHAR(MAX) = N'[{"TableName":"Qc.ProductInspection","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Qc.ProductInspection","ColumnName":"t1_IsCompleted","DisplayName":"تکمیل شده","Alliance":"t1_IsCompleted","Address":"[t1].[IsCompleted]","SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { var yes = data === true || data === 1 || data === \"1\" || data === \"true\" || data === \"True\" || data === \"بله\" || data === \"بلی\"; return yes ? \"بلی\" : \"خیر\"; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ProductionOrderItem","ColumnName":"t2_Serial","DisplayName":"سریال قلم سفارش ساخت","Alliance":"t2_Serial","Address":"[t2].[Serial]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Qc.ProductInspection","ColumnName":"t1_ProductionOrderNumber","DisplayName":"شماره سفارش ساخت","Alliance":"t1_ProductionOrderNumber","Address":"[t1].[ProductionOrderNumber]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Qc.ProductInspection","ColumnName":"t1_ProjectTitle","DisplayName":"عنوان پروژه","Alliance":"t1_ProjectTitle","Address":"[t1].[ProjectTitle]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Inv.Part","ColumnName":"t3_Code","DisplayName":"کد کالا","Alliance":"t3_Code","Address":"[t3].[Code]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Inv.Part","ColumnName":"t3_Name","DisplayName":"نام کالا","Alliance":"t3_Name","Address":"[t3].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":450,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Qc.ProductInspection","ColumnName":"t1_TestStartShamsiDate","DisplayName":"تاریخ شروع تست","Alliance":"t1_TestStartShamsiDate","Address":"[t1].[TestStartShamsiDate]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Qc.ProductInspection","ColumnName":"t1_TestStartTime","DisplayName":"ساعت شروع","Alliance":"t1_TestStartTime","Address":"[t1].[TestStartTime]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Qc.ProductInspection","ColumnName":"t1_TestEndShamsiDate","DisplayName":"تاریخ پایان تست","Alliance":"t1_TestEndShamsiDate","Address":"[t1].[TestEndShamsiDate]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Qc.ProductInspection","ColumnName":"t1_TestEndTime","DisplayName":"ساعت خاتمه","Alliance":"t1_TestEndTime","Address":"[t1].[TestEndTime]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Hcm.Personel","ColumnName":"t4_FullName","DisplayName":"بازرس","Alliance":"t4_FullName","Address":"LTRIM(RTRIM(ISNULL([t4].[Name], N'''') + N'' '' + ISNULL([t4].[Family], N'''')))","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Hrm.OrgUnit","ColumnName":"t5_Title","DisplayName":"دپارتمان فروش","Alliance":"t5_Title","Address":"[t5].[Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Qc.ProductInspection","ColumnName":"t1_InspectionResult","DisplayName":"نتیجه بازرسی","Alliance":"t1_InspectionResult","Address":"[t1].[InspectionResult]","SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { var yes = data === true || data === 1 || data === \"1\" || data === \"true\" || data === \"True\" || data === \"بله\" || data === \"بلی\"; return yes ? \"بلی\" : \"خیر\"; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Qc.ProductInspection","ColumnName":"t1_CreatedByName","DisplayName":"کاربر ایجادکننده","Alliance":"t1_CreatedByName","Address":"[t1].[CreatedByName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Qc.ProductInspection","ColumnName":"t1_CreatedDate","DisplayName":"تاریخ ایجاد","Alliance":"t1_CreatedDate","Address":"CASE WHEN [t1].[CreatedOnShamsiDateTime] IS NULL THEN NULL ELSE LEFT([t1].[CreatedOnShamsiDateTime], 10) END","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":1,"SortOrder":0,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Qc.ProductInspection","ColumnName":"t1_CreatedTime","DisplayName":"زمان ایجاد","Alliance":"t1_CreatedTime","Address":"CASE WHEN [t1].[CreatedOnShamsiDateTime] IS NULL OR LEN([t1].[CreatedOnShamsiDateTime]) < 12 THEN NULL ELSE SUBSTRING([t1].[CreatedOnShamsiDateTime], 12, 5) END","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Qc.ProductInspection","ColumnName":"t1_ModifiedByName","DisplayName":"کاربر ویرایشگر","Alliance":"t1_ModifiedByName","Address":"[t1].[ModifiedByName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Qc.ProductInspection","ColumnName":"t1_ModifiedDateShamsiDateTime","DisplayName":"زمان ویرایش","Alliance":"t1_ModifiedDateShamsiDateTime","Address":"[t1].[ModifiedDateShamsiDateTime]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""}]';
    DECLARE @Event NVARCHAR(MAX) = N'{"onSelectedRow":"","onRowAdded":"function(ctx){var data=ctx.rowData||{};var done=data.t1_IsCompleted;if(done===true||done===1||done===\"1\"||done===\"true\"||done===\"True\"||done===\"بله\"||done===\"بلی\")return;if(!ctx.table)return;var colIdx=-1;ctx.table.columns().every(function(i){if(this.dataSrc()===\"_rowNumber\")colIdx=i;});var node=colIdx>=0?ctx.table.cell(ctx.$row,colIdx).node():ctx.$row.find(\"td\").get(0);if(node)$(node).css(\"background-color\",\"orange\");}"}';
    DECLARE @Ent NVARCHAR(200) = N'entities.app.qc.productinspection';
    DECLARE @Pascal NVARCHAR(200) = N'Entities.App.Qc.ProductInspection';
    DECLARE @Qj NVARCHAR(MAX) = JSON_MODIFY(@QueryJsonSkeleton, '$.CustomQuery', @Sel);

    DECLARE @Profiles TABLE (Name NVARCHAR(100), Title NVARCHAR(200), Act NVARCHAR(MAX), Btn NVARCHAR(MAX), Roles NVARCHAR(MAX), Qry NVARCHAR(MAX) NULL, Cols NVARCHAR(MAX) NULL);
    INSERT INTO @Profiles (Name, Title, Act, Btn, Roles) VALUES
        (N'Qc_ProductInspection_InCompany', N'بازرسی محصول در هوایار', N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]', N'[{"id":"copy","title":"کپی و جدید","colorClass":"btn-color-success","iconClass":"fa-light fa-copy","requiresSelection":true,"useHtml":false,"html":null,"actionScript":"function(ctx){var id=ctx.primaryKeyValue;if(!id){toastr.warning(\"یک ردیف را انتخاب کنید\");return;}appController.addPage(\"/panel/qc/productinspection/edit?copyFrom=\"+id);}"},{"id":"attachments","title":"پیوست مدارک","colorClass":"btn-color-info","iconClass":"fa-light fa-paperclip","requiresSelection":true,"useHtml":false,"html":null,"actionScript":"function(ctx){var id=ctx.primaryKeyValue;if(!id){toastr.warning(\"یک ردیف را انتخاب کنید\");return;}appController.addPage(\"/panel/qc/productinspectionattachment/listbyparentid?productInspectionId=\"+id,true,\"پیوست مدارک\");}"}]', N'ShowAllMenus|Qc.ProductInspection.Experts|Qc.ProductInspection.SystemExperts|Qc.ProductInspection.EngineeringExperts'),
        (N'Qc_ProductInspection_InCompany_View', N'بازرسی محصول در هوایار (مشاهده)', N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]', N'[{"id":"attachments","title":"پیوست مدارک","colorClass":"btn-color-info","iconClass":"fa-light fa-paperclip","requiresSelection":true,"useHtml":false,"html":null,"actionScript":"function(ctx){var id=ctx.primaryKeyValue;if(!id){toastr.warning(\"یک ردیف را انتخاب کنید\");return;}appController.addPage(\"/panel/qc/productinspectionattachment/listbyparentid?productInspectionId=\"+id,true,\"پیوست مدارک\");}"}]', N'مشاهده کلی HTS|Qc.ProductInspection.Viewers');

    UPDATE @Profiles SET Qry = @Sel, Cols = @Cols;

    DECLARE @ColManual NVARCHAR(MAX) = N',{"TableName":"Qc.ProductInspection","ColumnName":"t1_ManualSerial","DisplayName":"سریال دستی","Alliance":"t1_ManualSerial","Address":"[t1].[ManualSerial]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""}';
    DECLARE @ColContractor NVARCHAR(MAX) = N',{"TableName":"Qc.ProductInspection","ColumnName":"t1_Contractor","DisplayName":"پیمانکار","Alliance":"t1_Contractor","Address":"[t1].[Contractor]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":true,"Sortable":true,"ClassName":""}';
    DECLARE @ColPlace NVARCHAR(MAX) = N',{"TableName":"Qc.ProductInspection","ColumnName":"t1_InspectionPlace","DisplayName":"محل بازرسی","Alliance":"t1_InspectionPlace","Address":"CASE [t1].[InspectionType] WHEN 470 THEN N''در محل پیمانکار'' ELSE N''در هوایار'' END","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""}';
    DECLARE @ColsExtra NVARCHAR(MAX) = STUFF(@Cols, LEN(@Cols), 1, @ColManual + @ColContractor + N']');
    DECLARE @ColsAll NVARCHAR(MAX) = STUFF(@Cols, LEN(@Cols), 1, @ColPlace + @ColManual + @ColContractor + N']');
    DECLARE @SelOut NVARCHAR(MAX) = REPLACE(REPLACE(@Sel,
        N'[t1].[Id] AS [t1_Id],',
        N'[t1].[Id] AS [t1_Id], [t1].[ManualSerial] AS [t1_ManualSerial], [t1].[Contractor] AS [t1_Contractor],'),
        N'WHERE [t1].[InspectionType] = 469', N'WHERE [t1].[InspectionType] = 470');
    DECLARE @SelAll NVARCHAR(MAX) = REPLACE(REPLACE(@Sel,
        N'[t1].[Id] AS [t1_Id],',
        N'[t1].[Id] AS [t1_Id], CASE [t1].[InspectionType] WHEN 470 THEN N''در محل پیمانکار'' ELSE N''در هوایار'' END AS [t1_InspectionPlace], [t1].[ManualSerial] AS [t1_ManualSerial], [t1].[Contractor] AS [t1_Contractor],'),
        N'WHERE [t1].[InspectionType] = 469', N'WHERE [t1].[InspectionType] IN (469, 470)');

    DECLARE @ActView NVARCHAR(MAX) = (SELECT Act FROM @Profiles WHERE Name = N'Qc_ProductInspection_InCompany_View');
    DECLARE @BtnView NVARCHAR(MAX) = (SELECT Btn FROM @Profiles WHERE Name = N'Qc_ProductInspection_InCompany_View');
    DECLARE @RolesSee NVARCHAR(MAX) = N'ShowAllMenus|Qc.ProductInspection.Experts|Qc.ProductInspection.SystemExperts|مشاهده کلی HTS';

    INSERT INTO @Profiles (Name, Title, Act, Btn, Roles, Qry, Cols) VALUES
        (N'Qc_ProductInspection_OutCompany', N'بازرسی محصول در محل پیمانکار', @ActView, @BtnView, @RolesSee, @SelOut, @ColsExtra),
        (N'Qc_ProductInspection_All', N'نمایش کلی بازرسی محصول', @ActView, @BtnView, @RolesSee, @SelAll, @ColsAll);

    DECLARE @Name NVARCHAR(100), @Title NVARCHAR(200), @Act NVARCHAR(MAX), @Btn NVARCHAR(MAX), @Roles NVARCHAR(MAX), @Qry NVARCHAR(MAX), @Col NVARCHAR(MAX), @Pid BIGINT, @QjRow NVARCHAR(MAX);
    DECLARE pc CURSOR LOCAL FAST_FORWARD FOR SELECT Name, Title, Act, Btn, Roles, Qry, Cols FROM @Profiles;
    OPEN pc; FETCH NEXT FROM pc INTO @Name, @Title, @Act, @Btn, @Roles, @Qry, @Col;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @Name)
        BEGIN
            SET @QjRow = JSON_MODIFY(@QueryJsonSkeleton, '$.CustomQuery', @Qry);
            UPDATE system.SavedQuery SET Title=@Title, QueryJson=@QjRow, ColumnsJson=@Col, EntityFullName=@Ent, Mode=1, Type=1,
                ActionOptions=@Act, CustomActionButtonsJson=@Btn, EventScriptsJson=@Event,
                ModifiedById=1, ModifiedByName=@SeedUser, ModifiedDateMiladiDateTime=@Now, ModifiedDateShamsiDateTime=@NowShamsi, IsActive=1
            WHERE Name=@Name;
            SELECT @Pid = Id FROM system.SavedQuery WHERE Name=@Name;
            PRINT N'  UPDATED ' + @Name;
        END
        ELSE
        BEGIN
            SET @QjRow = JSON_MODIFY(@QueryJsonSkeleton, '$.CustomQuery', @Qry);
            INSERT INTO system.SavedQuery
                (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
                 Mode, Type, EntityFullName, ActionOptions,
                 CreatedById, ModifiedById, CreatedByName, ModifiedByName,
                 CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
            VALUES (@Name, @Title, @QjRow, @Col, N'{}', @Btn, @Event,
                 1, 1, @Ent, @Act, 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
            SET @Pid = SCOPE_IDENTITY();
            PRINT N'  CREATED ' + @Name;
        END

        DELETE ra FROM system.RoleAccess ra WHERE ra.ActionAccessType=3 AND ra.RowId=@Pid;
        INSERT INTO system.RoleAccess
            (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        SELECT N'dataProfile_' + CAST(@Pid AS nvarchar(20)), 3, 7, @Pascal, @Title, NULL, @Pid, r.Id,
            1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
        FROM system.Role r
        INNER JOIN STRING_SPLIT(@Roles, N'|') s ON s.value = r.Name;
        PRINT N'  Profile RoleAccess ' + @Name + N': ' + CAST(@@ROWCOUNT AS nvarchar(20));

        FETCH NEXT FROM pc INTO @Name, @Title, @Act, @Btn, @Roles, @Qry, @Col;
    END
    CLOSE pc; DEALLOCATE pc;

    UPDATE system.SavedQuery
    SET IsActive = 0,
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Name IN (N'Qc_ProductInspection_OutCompany_View', N'Qc_ProductInspection_All_View');

    DELETE ra
    FROM system.RoleAccess ra
    INNER JOIN system.SavedQuery q ON q.Id = ra.RowId
    WHERE ra.ActionAccessType = 3
      AND q.Name IN (N'Qc_ProductInspection_OutCompany_View', N'Qc_ProductInspection_All_View');

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_Qc_ProductInspection_DataProfiles ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT Id, Name, Title FROM system.SavedQuery WHERE Name LIKE N'Qc_ProductInspection%';
