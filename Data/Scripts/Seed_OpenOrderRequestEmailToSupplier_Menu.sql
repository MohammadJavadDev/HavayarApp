/*
================================================================================
Seed_OpenOrderRequestEmailToSupplier_Menu.sql  (WP1 / D1)
================================================================================
منوی «تامین و خرید» (system.SystemMenu Name='Supply', Id=3):
  برگ «پیشینه ارسال به پیمانکاران» → /panel/sup/openorderrequestemailtosupplier/list
  (HTS صفحه 545). تنظیمات روی همان List درخواست باز می‌ماند (Q7=a).

آینه‌ی Seed_BuyCategory_Menu.sql: اگر برگ موجود نبود به انتهای ریشه (یا کنار درخواست باز) اضافه می‌شود.
AccessRoleIds منو دست نمی‌خورد؛ دسترسی مسیر از Seed_OpenOrderRequestEmailToSupplier_DataProfile.sql می‌آید.
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-emailtosupplier-menu';

DECLARE @Path NVARCHAR(200) = N'/panel/sup/openorderrequestemailtosupplier/list';
DECLARE @Text NVARCHAR(200) = N'پیشینه ارسال به پیمانکاران';
DECLARE @OorPath NVARCHAR(200) = N'/panel/sup/openorderrequest/list';

DECLARE @Leaf NVARCHAR(MAX) = N'{"text":"پیشینه ارسال به پیمانکاران","icon":"ki-sms ki-outline","iconColor":"#ea603e","path":"/panel/sup/openorderrequestemailtosupplier/list","a_attr":{"href":"/panel/sup/openorderrequestemailtosupplier/list"},"data":{"iconColor":"#ea603e","path":"/panel/sup/openorderrequestemailtosupplier/list"},"children":[]}';

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @MenuId BIGINT;
    DECLARE @Content NVARCHAR(MAX);
    SELECT TOP 1 @MenuId = Id, @Content = Content
    FROM system.SystemMenu
    WHERE Name = N'Supply' OR Title = N'تامین و خرید'
    ORDER BY CASE WHEN Name = N'Supply' THEN 0 ELSE 1 END, Id;

    IF @MenuId IS NULL
        PRINT N'WARN: منوی Supply (تامین و خرید) یافت نشد؛ کاری انجام نشد';
    ELSE
    BEGIN
        IF @Content IS NULL OR LTRIM(RTRIM(@Content)) = N'' SET @Content = N'[]';
        IF ISJSON(@Content) = 0
            THROW 51200, N'Content منوی Supply JSON معتبر نیست', 1;

        IF CHARINDEX(N'"path":"' + @Path + N'"', @Content) > 0
            PRINT N'  برگ «' + @Text + N'» از قبل موجود است';
        ELSE
        BEGIN
            DECLARE @OorRootIdx INT = NULL;
            SELECT TOP 1 @OorRootIdx = CAST([key] AS INT)
            FROM OPENJSON(@Content)
            WHERE JSON_VALUE([value], '$.path') = @OorPath
            ORDER BY CAST([key] AS INT);

            IF @OorRootIdx IS NOT NULL
            BEGIN
                SELECT @Content = N'[' + STRING_AGG(CAST(x.Item AS NVARCHAR(MAX)), N',') WITHIN GROUP (ORDER BY x.Ord) + N']'
                FROM (
                    SELECT CAST([key] AS INT) * 2 AS Ord, [value] AS Item FROM OPENJSON(@Content)
                    UNION ALL
                    SELECT @OorRootIdx * 2 + 1, @Leaf
                ) x;
                PRINT N'  برگ «' + @Text + N'» بعد از درخواست‌های باز درج شد';
            END
            ELSE
            BEGIN
                SET @Content = JSON_MODIFY(@Content, N'append $', JSON_QUERY(@Leaf));
                PRINT N'  برگ «' + @Text + N'» به انتهای منو اضافه شد';
            END
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

        PRINT N'  SystemMenu Id=' + CAST(@MenuId AS NVARCHAR(20)) + N' به‌روزرسانی شد';
    END

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_OpenOrderRequestEmailToSupplier_Menu ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH

SELECT m.Id, m.Name, m.Title,
       CAST(r.[key] AS INT) AS RootIdx,
       JSON_VALUE(r.[value], '$.text') AS [Text],
       JSON_VALUE(r.[value], '$.path') AS [Path]
FROM system.SystemMenu m
CROSS APPLY OPENJSON(m.Content) r
WHERE (m.Name = N'Supply' OR m.Title = N'تامین و خرید')
  AND JSON_VALUE(r.[value], '$.path') LIKE N'/panel/sup/openorderrequest%'
ORDER BY m.Id, RootIdx;
