/*
Seed_CngInvoice_DataProfiles.sql
نمایه داده فاکتور CNG (HTS صفحه 476). UTF-8 BOM. Idempotent.
فیلتر ShowAll: admin / ShowAllMenus / Cng.Invoice.Full
ActionOptions: جدید / ویرایش / حذف / خروجی اکسل
sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Seed_CngInvoice_DataProfiles.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-cng-invoice-dataprofile';
    DECLARE @QueryJsonSkeleton NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}';

    DECLARE @Name NVARCHAR(100) = N'Cng_Invoice';
    DECLARE @Title NVARCHAR(200) = N'فاکتور';
    DECLARE @Ent NVARCHAR(200) = N'entities.app.cng.invoice';
    DECLARE @Pascal NVARCHAR(200) = N'Entities.App.Cng.Invoice';
    DECLARE @Sel NVARCHAR(MAX) = N'DECLARE @CuId BIGINT = TRY_CAST(@CurrentUserId AS BIGINT);
DECLARE @CuRoles NVARCHAR(MAX);
SELECT @CuRoles = Roles FROM system.[User] WHERE Id = @CuId;
DECLARE @HasShowAll BIT = CASE
    WHEN EXISTS (
        SELECT 1 FROM OPENJSON(ISNULL(@CuRoles, N''[]''))
        WHERE [value] IN (N''admin'', N''ShowAllMenus'', N''Cng.Invoice.Full'')
    ) THEN 1 ELSE 0 END;
--!--mainsection
SELECT
    [t1].[Id] AS [t1_Id],
    COALESCE([p].[CompanyName], [p].[FullName], N'''') AS [Customer_Title],
    [t1].[InvoiceNumber] AS [t1_InvoiceNumber],
    [t1].[InvoiceDate] AS [t1_InvoiceDate],
    [t1].[CreatedByName] AS [t1_CreatedByName],
    [t1].[CreatedDateInText] AS [t1_CreatedDateInText]
FROM [Cng].[Invoice] AS [t1]
LEFT JOIN [SLS].[Customer] AS [c] ON [c].[Id] = [t1].[CustomerId]
LEFT JOIN [Gnr].[Party] AS [p] ON [p].[Id] = [c].[PartyId]
WHERE @HasShowAll = 1 OR [t1].[CreatedById] = @CuId';

    DECLARE @Cols NVARCHAR(MAX) = N'[{"TableName":"Cng.Invoice","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SortDirection":1,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SystemType":6,"Width":60},{"TableName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","ColumnName":"Customer_Title","DisplayName":"مشتری","Alliance":"Customer_Title","Address":null,"SystemTypeName":"String","SystemType":0,"Width":220},{"TableName":"Cng.Invoice","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","ColumnName":"t1_InvoiceNumber","DisplayName":"شماره فاکتور","Alliance":"t1_InvoiceNumber","Address":"[t1].[InvoiceNumber]","SystemTypeName":"String","SystemType":0,"Width":140},{"TableName":"Cng.Invoice","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","ColumnName":"t1_InvoiceDate","DisplayName":"تاریخ فاکتور","Alliance":"t1_InvoiceDate","Address":"[t1].[InvoiceDate]","SystemTypeName":"String","SystemType":0,"Width":120},{"TableName":"Cng.Invoice","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","ColumnName":"t1_CreatedByName","DisplayName":"کاربر ایجادکننده","Alliance":"t1_CreatedByName","Address":"[t1].[CreatedByName]","SystemTypeName":"String","SystemType":0,"Width":150},{"TableName":"Cng.Invoice","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","ColumnName":"t1_CreatedDateInText","DisplayName":"تاریخ ایجاد","Alliance":"t1_CreatedDateInText","Address":"[t1].[CreatedDateInText]","SystemTypeName":"String","SystemType":0,"Width":120}]';

    DECLARE @Act NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
    DECLARE @Btn NVARCHAR(MAX) = N'[]';
    DECLARE @EventScripts NVARCHAR(MAX) = N'{"onSelectedRow":"","onRowAdded":""}';
    DECLARE @Qj NVARCHAR(MAX);
    DECLARE @Pid BIGINT;

    SET @Qj = JSON_MODIFY(@QueryJsonSkeleton, '$.CustomQuery', @Sel);
    IF ISJSON(@Cols) <> 1 OR ISJSON(@Qj) <> 1 OR ISJSON(@Act) <> 1 OR ISJSON(@Btn) <> 1 OR ISJSON(@EventScripts) <> 1
        THROW 51630, N'JSON نمایه Cng_Invoice معتبر نیست.', 1;

    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @Name)
    BEGIN
        UPDATE system.SavedQuery SET Title=@Title, QueryJson=@Qj, ColumnsJson=@Cols, EntityFullName=@Ent, Mode=1, Type=1,
            ActionOptions=@Act, CustomActionButtonsJson=@Btn, EventScriptsJson=@EventScripts, DiagramJson=N'{}',
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
        VALUES (@Name, @Title, @Qj, @Cols, N'{}', @Btn, @EventScripts,
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
    WHERE r.Name IN (
        N'ShowAllMenus', N'مشاهده کلی HTS',
        N'Cng.Invoice.Manage', N'Cng.Invoice.Full'
    );
    PRINT N'  Profile RoleAccess rows: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_CngInvoice_DataProfiles ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT Id, Name, Title FROM system.SavedQuery WHERE Name = N'Cng_Invoice';
SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance, JSON_VALUE(j.value, '$.DisplayName') AS DisplayName
FROM system.SavedQuery q CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'Cng_Invoice'
ORDER BY CAST(j.[key] AS int);
