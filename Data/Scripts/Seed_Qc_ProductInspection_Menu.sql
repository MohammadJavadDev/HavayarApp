/*
Seed_Qc_ProductInspection_Menu.sql
منو: کیفیت → کنترل کیفیت → عملیات → بازرسی محصول در هوایار
شاخه عملیات اگر نباشد ساخته می‌شود. UTF-8 BOM. Idempotent.
sqlcmd -S 172.20.40.42 -d HavayarApp -U sa -P <pwd> -C -b -I -f 65001 -i "Data\Scripts\Seed_Qc_ProductInspection_Menu.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-qc-productinspection-menu';
DECLARE @Path NVARCHAR(200) = N'/panel/qc/productinspection/list';
DECLARE @Leaf NVARCHAR(MAX) = N'{"text":"بازرسی محصول در هوایار","icon":"ki-verify ki-outline","iconColor":"#FACC15","path":"/panel/qc/productinspection/list","a_attr":{"href":"/panel/qc/productinspection/list"},"data":{"iconColor":"#FACC15","path":"/panel/qc/productinspection/list"},"children":[]}';
DECLARE @Branch NVARCHAR(MAX) = N'{"text":"عملیات","icon":"ki-setting-2 ki-outline","iconColor":"#FACC15","path":"","a_attr":{"href":""},"data":{"iconColor":"#FACC15","path":""},"children":[' + @Leaf + N']}';

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @MenuId BIGINT;
    DECLARE @Content NVARCHAR(MAX);
    SELECT TOP 1 @MenuId = Id, @Content = Content
    FROM system.SystemMenu
    WHERE Name = N'AllMenus'
    ORDER BY Id;

    IF @MenuId IS NULL
        THROW 51750, N'منوی AllMenus یافت نشد', 1;

    IF CHARINDEX(N'"path":"' + @Path + N'"', @Content) > 0
        PRINT N'  برگ بازرسی محصول در هوایار از قبل موجود است';
    ELSE
    BEGIN
        IF @Content IS NULL OR LTRIM(RTRIM(@Content)) = N'' OR ISJSON(@Content) = 0
            THROW 51751, N'Content منوی AllMenus JSON معتبر نیست', 1;

        DECLARE @QIdx INT = NULL;
        SELECT TOP 1 @QIdx = CAST([key] AS INT)
        FROM OPENJSON(@Content)
        WHERE JSON_VALUE([value], '$.text') IN (N'کیفیت', N'سیستم کیفیت')
        ORDER BY CASE JSON_VALUE([value], '$.text') WHEN N'کیفیت' THEN 0 ELSE 1 END, CAST([key] AS INT);

        IF @QIdx IS NULL
            THROW 51752, N'شاخه کیفیت در منوی AllMenus یافت نشد', 1;

        DECLARE @QChildren NVARCHAR(MAX) = JSON_QUERY(@Content, '$[' + CAST(@QIdx AS nvarchar(10)) + '].children');
        IF @QChildren IS NULL SET @QChildren = N'[]';

        DECLARE @CIdx INT = NULL;
        SELECT TOP 1 @CIdx = CAST([key] AS INT)
        FROM OPENJSON(@QChildren)
        WHERE JSON_VALUE([value], '$.text') = N'کنترل کیفیت';

        IF @CIdx IS NULL
            THROW 51753, N'شاخه کنترل کیفیت یافت نشد', 1;

        DECLARE @CPath NVARCHAR(200) = N'$[' + CAST(@QIdx AS nvarchar(10)) + N'].children[' + CAST(@CIdx AS nvarchar(10)) + N'].children';
        DECLARE @CChildren NVARCHAR(MAX) = JSON_QUERY(@Content, @CPath);
        IF @CChildren IS NULL SET @CChildren = N'[]';

        DECLARE @OIdx INT = NULL;
        SELECT TOP 1 @OIdx = CAST([key] AS INT)
        FROM OPENJSON(@CChildren)
        WHERE JSON_VALUE([value], '$.text') = N'عملیات';

        IF @OIdx IS NULL
        BEGIN
            SET @Content = JSON_MODIFY(@Content, N'append ' + @CPath, JSON_QUERY(@Branch));
            PRINT N'  شاخه عملیات و برگ بازرسی محصول در هوایار اضافه شد';
        END
        ELSE
        BEGIN
            DECLARE @OPath NVARCHAR(300) = @CPath + N'[' + CAST(@OIdx AS nvarchar(10)) + N'].children';
            SET @Content = JSON_MODIFY(@Content, N'append ' + @OPath, JSON_QUERY(@Leaf));
            PRINT N'  برگ بازرسی محصول در هوایار به شاخه عملیات اضافه شد';
        END

        IF ISJSON(@Content) = 0
            THROW 51754, N'Content تولیدشده JSON معتبر نیست', 1;

        UPDATE system.SystemMenu
        SET Content = @Content,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @MenuId;
    END

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_Qc_ProductInspection_Menu ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;
