/*
Seed_SrvPmRequest_Menu.sql
منو: سیستم عمومی/مشترک → پشتیبانی و خدمات → عملیات → درخواست PM
فقط این برگ؛ بدون حذف همسایه‌ها. UTF-8 BOM. Idempotent.
sqlcmd -S PortalSRV\PORTAL -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Seed_SrvPmRequest_Menu.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-srv-pmrequest-menu';
DECLARE @Path NVARCHAR(200) = N'/panel/srv/pmrequest/list';
DECLARE @GeneralText NVARCHAR(200) = N'سیستم عمومی/مشترک';
DECLARE @SupportText NVARCHAR(200) = N'پشتیبانی و خدمات';
DECLARE @OpsText NVARCHAR(200) = N'عملیات';
DECLARE @LeafText NVARCHAR(200) = N'درخواست PM';

DECLARE @Leaf NVARCHAR(MAX) = N'{"text":"درخواست PM","icon":"ki-element-11 ki-outline","iconColor":"#5EEAD4","path":"/panel/srv/pmrequest/list","a_attr":{"href":"/panel/srv/pmrequest/list"},"data":{"iconColor":"#5EEAD4","path":"/panel/srv/pmrequest/list"},"children":[]}';
DECLARE @OpsGroup NVARCHAR(MAX) = N'{"text":"عملیات","icon":"ki-element-11 ki-outline","iconColor":"#5EEAD4","path":"","a_attr":{"href":""},"data":{"iconColor":"#5EEAD4","path":""},"children":[{"text":"درخواست PM","icon":"ki-element-11 ki-outline","iconColor":"#5EEAD4","path":"/panel/srv/pmrequest/list","a_attr":{"href":"/panel/srv/pmrequest/list"},"data":{"iconColor":"#5EEAD4","path":"/panel/srv/pmrequest/list"},"children":[]}]}';
DECLARE @SupportGroup NVARCHAR(MAX) = N'{"text":"پشتیبانی و خدمات","icon":"ki-element-11 ki-outline","iconColor":"#5EEAD4","path":"","a_attr":{"href":""},"data":{"iconColor":"#5EEAD4","path":""},"children":[{"text":"عملیات","icon":"ki-element-11 ki-outline","iconColor":"#5EEAD4","path":"","a_attr":{"href":""},"data":{"iconColor":"#5EEAD4","path":""},"children":[{"text":"درخواست PM","icon":"ki-element-11 ki-outline","iconColor":"#5EEAD4","path":"/panel/srv/pmrequest/list","a_attr":{"href":"/panel/srv/pmrequest/list"},"data":{"iconColor":"#5EEAD4","path":"/panel/srv/pmrequest/list"},"children":[]}]}]}';

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
        PRINT N'  برگ درخواست PM از قبل موجود است';
    ELSE
    BEGIN
        IF @Content IS NULL OR LTRIM(RTRIM(@Content)) = N'' SET @Content = N'[]';
        IF ISJSON(@Content) = 0
            THROW 51720, N'Content منوی AllMenus JSON معتبر نیست', 1;

        DECLARE @GIdx INT = NULL;
        SELECT TOP 1 @GIdx = CAST([key] AS INT)
        FROM OPENJSON(@Content)
        WHERE JSON_VALUE([value], '$.text') = @GeneralText
        ORDER BY CAST([key] AS INT);

        IF @GIdx IS NULL
        BEGIN
            DECLARE @GeneralRoot NVARCHAR(MAX) = N'{"text":"سیستم عمومی/مشترک","icon":"ki-element-11 ki-outline","iconColor":"#5EEAD4","path":"","a_attr":{"href":""},"data":{"iconColor":"#5EEAD4","path":""},"children":[' + @SupportGroup + N']}';
            SET @Content = JSON_MODIFY(@Content, N'append $', JSON_QUERY(@GeneralRoot));
            PRINT N'  سیستم عمومی/مشترک + شاخه پشتیبانی اضافه شد';
        END
        ELSE
        BEGIN
            DECLARE @GChildren NVARCHAR(MAX) = JSON_QUERY(@Content, '$[' + CAST(@GIdx AS nvarchar(10)) + '].children');
            IF @GChildren IS NULL SET @GChildren = N'[]';

            DECLARE @SIdx INT = NULL;
            SELECT TOP 1 @SIdx = CAST([key] AS INT)
            FROM OPENJSON(@GChildren)
            WHERE JSON_VALUE([value], '$.text') = @SupportText;

            IF @SIdx IS NULL
            BEGIN
                SET @Content = JSON_MODIFY(@Content,
                    N'append $[' + CAST(@GIdx AS nvarchar(10)) + N'].children',
                    JSON_QUERY(@SupportGroup));
                PRINT N'  گروه پشتیبانی و خدمات زیر سیستم عمومی اضافه شد';
            END
            ELSE
            BEGIN
                DECLARE @SChildren NVARCHAR(MAX) = JSON_QUERY(@Content,
                    '$[' + CAST(@GIdx AS nvarchar(10)) + '].children[' + CAST(@SIdx AS nvarchar(10)) + '].children');
                IF @SChildren IS NULL SET @SChildren = N'[]';

                DECLARE @OIdx INT = NULL;
                SELECT TOP 1 @OIdx = CAST([key] AS INT)
                FROM OPENJSON(@SChildren)
                WHERE JSON_VALUE([value], '$.text') = @OpsText;

                IF @OIdx IS NULL
                BEGIN
                    SET @Content = JSON_MODIFY(@Content,
                        N'append $[' + CAST(@GIdx AS nvarchar(10)) + N'].children[' + CAST(@SIdx AS nvarchar(10)) + N'].children',
                        JSON_QUERY(@OpsGroup));
                    PRINT N'  گروه عملیات زیر پشتیبانی اضافه شد';
                END
                ELSE
                BEGIN
                    SET @Content = JSON_MODIFY(@Content,
                        N'append $[' + CAST(@GIdx AS nvarchar(10)) + N'].children[' + CAST(@SIdx AS nvarchar(10)) + N'].children[' + CAST(@OIdx AS nvarchar(10)) + N'].children',
                        JSON_QUERY(@Leaf));
                    PRINT N'  برگ درخواست PM به عملیات موجود اضافه شد';
                END
            END
        END

        IF ISJSON(@Content) = 0
            THROW 51721, N'Content تولیدشده JSON معتبر نیست', 1;

        UPDATE system.SystemMenu
        SET Content = @Content,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @MenuId;
    END

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_SrvPmRequest_Menu ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH
