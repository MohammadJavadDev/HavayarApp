/*
Seed_SecEmploymentStatuteSpecial_Menu.sql
افزودن «لیست استثنائات سابقه حضور» زیر دبیرخانه در منوی AllMenus.
UTF-8 with BOM. sqlcmd -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-sec-employment-statute-special-menu';
DECLARE @Path NVARCHAR(200) = N'/panel/sec/employmentstatutespecial/list';
DECLARE @HrText NVARCHAR(200) = N'سیستم منابع انسانی';

DECLARE @SecretariatGroup NVARCHAR(MAX) = N'{"text":"دبیرخانه","icon":"ki-sms ki-outline","iconColor":"#a78bfa","path":"","a_attr":{"href":""},"data":{"iconColor":"#a78bfa","path":""},"children":[{"text":"لیست استثنائات سابقه حضور","icon":"ki-calendar ki-outline","iconColor":"#a78bfa","path":"/panel/sec/employmentstatutespecial/list","a_attr":{"href":"/panel/sec/employmentstatutespecial/list"},"data":{"iconColor":"#a78bfa","path":"/panel/sec/employmentstatutespecial/list"},"children":[]}]}';

DECLARE @LeafOnly NVARCHAR(MAX) = N'{"text":"لیست استثنائات سابقه حضور","icon":"ki-calendar ki-outline","iconColor":"#a78bfa","path":"/panel/sec/employmentstatutespecial/list","a_attr":{"href":"/panel/sec/employmentstatutespecial/list"},"data":{"iconColor":"#a78bfa","path":"/panel/sec/employmentstatutespecial/list"},"children":[]}';

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @MenuId BIGINT;
    DECLARE @Content NVARCHAR(MAX);
    SELECT TOP 1 @MenuId = Id, @Content = Content
    FROM system.SystemMenu
    WHERE Name = N'AllMenus'
    ORDER BY Id;

    IF @MenuId IS NULL
        PRINT N'WARN: منوی AllMenus یافت نشد';
    ELSE IF CHARINDEX(N'"path":"' + @Path + N'"', @Content) > 0
        PRINT N'  برگ لیست استثنائات سابقه حضور از قبل موجود است';
    ELSE
    BEGIN
        IF @Content IS NULL OR LTRIM(RTRIM(@Content)) = N'' SET @Content = N'[]';
        IF ISJSON(@Content) = 0
            THROW 51200, N'Content منوی AllMenus JSON معتبر نیست', 1;

        DECLARE @HrIdx INT = NULL;
        SELECT TOP 1 @HrIdx = CAST([key] AS INT)
        FROM OPENJSON(@Content)
        WHERE JSON_VALUE([value], '$.text') IN (N'منابع انسانی', N'سیستم منابع انسانی')
        ORDER BY CASE WHEN JSON_VALUE([value], '$.text') = N'منابع انسانی' THEN 0 ELSE 1 END, CAST([key] AS INT);

        IF @HrIdx IS NOT NULL
        BEGIN
            DECLARE @HrChildren NVARCHAR(MAX) = JSON_QUERY(@Content, '$[' + CAST(@HrIdx AS nvarchar(10)) + '].children');
            IF @HrChildren IS NULL SET @HrChildren = N'[]';

            DECLARE @SecIdx INT = NULL;
            SELECT TOP 1 @SecIdx = CAST([key] AS INT)
            FROM OPENJSON(@HrChildren)
            WHERE JSON_VALUE([value], '$.text') = N'دبیرخانه';

            IF @SecIdx IS NOT NULL
            BEGIN
                SET @Content = JSON_MODIFY(@Content,
                    N'append $[' + CAST(@HrIdx AS nvarchar(10)) + N'].children[' + CAST(@SecIdx AS nvarchar(10)) + N'].children',
                    JSON_QUERY(@LeafOnly));
                PRINT N'  برگ به زیرمنوی دبیرخانه موجود اضافه شد';
            END
            ELSE
            BEGIN
                SET @Content = JSON_MODIFY(@Content,
                    N'append $[' + CAST(@HrIdx AS nvarchar(10)) + N'].children',
                    JSON_QUERY(@SecretariatGroup));
                PRINT N'  گروه دبیرخانه زیر منابع انسانی اضافه شد';
            END
        END
        ELSE
        BEGIN
            DECLARE @HrRoot NVARCHAR(MAX) = N'{"text":"سیستم منابع انسانی","icon":"ki-people ki-outline","iconColor":"#c084fc","path":"","a_attr":{"href":""},"data":{"iconColor":"#c084fc","path":""},"children":[' + @SecretariatGroup + N']}';
            SET @Content = JSON_MODIFY(@Content, N'append $', JSON_QUERY(@HrRoot));
            PRINT N'  سیستم منابع انسانی + دبیرخانه به ریشه منو اضافه شد';
        END

        IF ISJSON(@Content) = 0
            THROW 51201, N'Content تولیدشده JSON معتبر نیست', 1;

        UPDATE system.SystemMenu
        SET Content = @Content,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @MenuId;
    END

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_SecEmploymentStatuteSpecial_Menu ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH
