/*
================================================================================
Update_OpenOrderRequestEmailToSupplier_DataProfile_Comment.sql
================================================================================
ستون «توضیحات» (Sup.OpenOrderRequestEmailToSupplier.Comment) به نمایه
OpenOrderRequestEmailToSupplier_List — پیشینه ارسال به پیمانکاران.

فقط QueryJson و ColumnsJson را به‌روز می‌کند. نقش و RoleAccess دست نمی‌خورد.
بعد از اجرا: Restart WebApp یا پاک‌کردن Redis DataTableProfileCacheDb
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'Sup.OpenOrderRequestEmailToSupplier', N'U') IS NULL
BEGIN
    RAISERROR(N'جدول Sup.OpenOrderRequestEmailToSupplier وجود ندارد.', 16, 1);
    RETURN;
END

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @ProfileName NVARCHAR(100) = N'OpenOrderRequestEmailToSupplier_List';
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);

    IF NOT EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @ProfileName)
    BEGIN
        RAISERROR(N'نمایه OpenOrderRequestEmailToSupplier_List وجود ندارد. ابتدا Seed_OpenOrderRequestEmailToSupplier_DataProfile.sql را اجرا کنید.', 16, 1);
    END

    IF EXISTS (
        SELECT 1
        FROM system.SavedQuery q
        CROSS APPLY OPENJSON(q.ColumnsJson) j
        WHERE q.Name = @ProfileName
          AND JSON_VALUE(j.value, '$.Alliance') = N't1_Comment')
    BEGIN
        PRINT N'ستون توضیحات از قبل روی نمایه است';
    END
    ELSE
    BEGIN
        DECLARE @CustomQuery NVARCHAR(MAX);
        SELECT @CustomQuery = j.CustomQuery
        FROM system.SavedQuery q
        CROSS APPLY OPENJSON(q.QueryJson) WITH (CustomQuery NVARCHAR(MAX) '$.CustomQuery') j
        WHERE q.Name = @ProfileName;

        IF CHARINDEX(N'AS [t1_Comment]', @CustomQuery) = 0
        BEGIN
            IF CHARINDEX(N'AS [tSender_Name]', @CustomQuery) = 0
            BEGIN
                RAISERROR(N'جای درج ستون توضیحات در CustomQuery پیدا نشد.', 16, 1);
            END
            SET @CustomQuery = REPLACE(
                @CustomQuery,
                N'AS [tSender_Name]',
                N'AS [tSender_Name],' + NCHAR(10) + N'    [t1].[Comment] AS [t1_Comment]');
        END

        DECLARE @QueryJson NVARCHAR(MAX) = JSON_MODIFY(
            N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}',
            '$.CustomQuery',
            @CustomQuery);

        DECLARE @CommentCol NVARCHAR(MAX) = N'{"TableName":"Sup.OpenOrderRequestEmailToSupplier","ColumnName":"t1_Comment","DisplayName":"توضیحات","Alliance":"t1_Comment","Address":"[t1].[Comment]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":220,"Filterable":true,"Sortable":true,"ClassName":""}';

        UPDATE system.SavedQuery
        SET
            QueryJson = @QueryJson,
            ColumnsJson = CASE
                WHEN RIGHT(RTRIM(ColumnsJson), 1) = N']'
                    THEN LEFT(RTRIM(ColumnsJson), LEN(RTRIM(ColumnsJson)) - 1) + N',' + @CommentCol + N']'
                ELSE ColumnsJson
            END,
            ModifiedById = 1,
            ModifiedByName = N'update-emailtosupplier-comment',
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Name = @ProfileName;

        PRINT N'ستون توضیحات به نمایه اضافه شد';
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH

SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance, JSON_VALUE(j.value, '$.DisplayName') AS DisplayName
FROM system.SavedQuery q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'OpenOrderRequestEmailToSupplier_List'
ORDER BY CAST(j.[key] AS int);
