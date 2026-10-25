/*
Seed_Qc_MeasurementParameterItem_Menu.sql
منو: کیفیت → کنترل کیفیت → اطلاعات پایه → پارامتر اندازه گیری
کنار گروه بندی پارامترهای اندازه گیری. UTF-8 BOM. Idempotent.
sqlcmd -S 172.20.40.42 -d HavayarApp -U sa -P <pwd> -C -b -I -f 65001 -i "Data\Scripts\Seed_Qc_MeasurementParameterItem_Menu.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-qc-measurementparameteritem-menu';
DECLARE @Path NVARCHAR(200) = N'/panel/qc/measurementparameteritem/list';
DECLARE @Leaf NVARCHAR(MAX) = N'{"text":"پارامتر اندازه گیری","icon":"ki-category ki-outline","iconColor":"#FACC15","path":"/panel/qc/measurementparameteritem/list","a_attr":{"href":"/panel/qc/measurementparameteritem/list"},"data":{"iconColor":"#FACC15","path":"/panel/qc/measurementparameteritem/list"},"children":[]}';

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @MenuId BIGINT;
    DECLARE @Content NVARCHAR(MAX);
    SELECT TOP 1 @MenuId = Id, @Content = Content
    FROM system.SystemMenu
    WHERE Name = N'AllMenus'
    ORDER BY Id;

    IF @MenuId IS NULL
        THROW 51740, N'منوی AllMenus یافت نشد', 1;

    IF CHARINDEX(N'"path":"' + @Path + N'"', @Content) > 0
        PRINT N'  برگ پارامتر اندازه گیری از قبل موجود است';
    ELSE
    BEGIN
        IF @Content IS NULL OR LTRIM(RTRIM(@Content)) = N'' OR ISJSON(@Content) = 0
            THROW 51741, N'Content منوی AllMenus JSON معتبر نیست', 1;

        DECLARE @QIdx INT = NULL;
        SELECT TOP 1 @QIdx = CAST([key] AS INT)
        FROM OPENJSON(@Content)
        WHERE JSON_VALUE([value], '$.text') IN (N'کیفیت', N'سیستم کیفیت')
        ORDER BY CASE JSON_VALUE([value], '$.text') WHEN N'کیفیت' THEN 0 ELSE 1 END, CAST([key] AS INT);

        IF @QIdx IS NULL
            THROW 51742, N'شاخه کیفیت در منوی AllMenus یافت نشد', 1;

        DECLARE @QChildren NVARCHAR(MAX) = JSON_QUERY(@Content, '$[' + CAST(@QIdx AS nvarchar(10)) + '].children');
        IF @QChildren IS NULL SET @QChildren = N'[]';

        DECLARE @CIdx INT = NULL;
        SELECT TOP 1 @CIdx = CAST([key] AS INT)
        FROM OPENJSON(@QChildren)
        WHERE JSON_VALUE([value], '$.text') = N'کنترل کیفیت';

        IF @CIdx IS NULL
            THROW 51743, N'شاخه کنترل کیفیت یافت نشد', 1;

        DECLARE @CChildren NVARCHAR(MAX) = JSON_QUERY(@Content,
            '$[' + CAST(@QIdx AS nvarchar(10)) + '].children[' + CAST(@CIdx AS nvarchar(10)) + '].children');
        IF @CChildren IS NULL SET @CChildren = N'[]';

        DECLARE @BIdx INT = NULL;
        SELECT TOP 1 @BIdx = CAST([key] AS INT)
        FROM OPENJSON(@CChildren)
        WHERE JSON_VALUE([value], '$.text') = N'اطلاعات پایه';

        IF @BIdx IS NULL
            THROW 51744, N'شاخه اطلاعات پایه یافت نشد', 1;

        SET @Content = JSON_MODIFY(@Content,
            N'append $[' + CAST(@QIdx AS nvarchar(10)) + N'].children[' + CAST(@CIdx AS nvarchar(10)) + N'].children[' + CAST(@BIdx AS nvarchar(10)) + N'].children',
            JSON_QUERY(@Leaf));

        IF ISJSON(@Content) = 0
            THROW 51745, N'Content تولیدشده JSON معتبر نیست', 1;

        UPDATE system.SystemMenu
        SET Content = @Content,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @MenuId;
        PRINT N'  برگ پارامتر اندازه گیری اضافه شد';
    END

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_Qc_MeasurementParameterItem_Menu ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;
