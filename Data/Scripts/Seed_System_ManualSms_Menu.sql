/*
  Seed_System_ManualSms_Menu.sql
  منو و دسترسی admin برای صفحه «ارسال پیامک دستی» (/panel/system/manualsms/index).
  Idempotent. UTF-8 with BOM؛ با sqlcmd -x -f 65001 اجرا شود.
  نقش‌های دیگر را از «گروه‌های کاربری» به عملیات «ارسال پیامک دستی» دسترسی بدهید.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-manualsms';
    DECLARE @Path NVARCHAR(300) = N'/panel/system/manualsms/index';
    DECLARE @Leaf NVARCHAR(MAX) = N'{"text":"ارسال پیامک دستی","icon":"ki-send ki-outline","iconColor":"#34D399","path":"/panel/system/manualsms/index","a_attr":{"href":"/panel/system/manualsms/index"},"data":{"iconColor":"#34D399","path":"/panel/system/manualsms/index"},"children":[]}';

    INSERT INTO [system].[RoleAccess]
    (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
     CreatedById, ModifiedById, CreatedByName, ModifiedByName,
     CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT v.Path, v.AType, 1000, NULL, v.Title, NULL, NULL, r.Id,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM [system].[Role] r
    CROSS JOIN (VALUES
        (N'/panel/system/manualsms/index', 1, N'ارسال پیامک دستی'),
        (N'/panel/system/manualsms/send', 2, N'ثبت ارسال پیامک دستی')
    ) v(Path, AType, Title)
    WHERE r.Name = N'admin'
      AND NOT EXISTS (SELECT 1 FROM [system].[RoleAccess] ra WHERE ra.RoleId = r.Id AND ra.Path = v.Path AND ra.ActionAccessType = v.AType);

    DECLARE @Content NVARCHAR(MAX);
    SELECT @Content = Content FROM [system].[SystemMenu] WHERE Name = N'AllMenus';
    IF @Content IS NOT NULL AND ISJSON(@Content) = 1
       AND JSON_VALUE(@Content, N'$[1].text') = N'حقوق دسترسی'
       AND CHARINDEX(@Path, @Content) = 0
    BEGIN
        DECLARE @Children NVARCHAR(MAX) = JSON_QUERY(@Content, N'$[1].children');
        DECLARE @Rebuilt NVARCHAR(MAX) = N'[';
        DECLARE @n INT = (SELECT COUNT(*) FROM OPENJSON(@Children));
        DECLARE @x INT = 0;
        DECLARE @comma BIT = 0;
        DECLARE @inserted BIT = 0;
        WHILE @x < @n
        BEGIN
            IF @comma = 1 SET @Rebuilt += N',';
            SET @comma = 1;
            SET @Rebuilt += JSON_QUERY(@Children, N'$[' + CAST(@x AS NVARCHAR(10)) + N']');
            IF JSON_VALUE(@Children, N'$[' + CAST(@x AS NVARCHAR(10)) + N'].path') = N'/panel/system/generalsetting/list'
            BEGIN
                SET @Rebuilt += N',' + @Leaf;
                SET @inserted = 1;
            END
            SET @x += 1;
        END
        IF @inserted = 0 SET @Rebuilt += N',' + @Leaf;
        SET @Rebuilt += N']';
        SET @Content = JSON_MODIFY(@Content, N'$[1].children', JSON_QUERY(@Rebuilt));
        IF @Content IS NULL OR ISJSON(@Content) <> 1
            THROW 51671, N'JSON منو بعد از افزودن ارسال پیامک دستی معتبر نیست.', 1;
        UPDATE [system].[SystemMenu]
        SET Content = @Content, ModifiedById = 1, ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Name = N'AllMenus';
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@Err, 16, 1);
END CATCH;

SELECT c.[key] AS Idx, JSON_VALUE(c.value, '$.text') AS Txt, JSON_VALUE(c.value, '$.path') AS Pth
FROM [system].[SystemMenu] m CROSS APPLY OPENJSON(m.Content, '$[1].children') c
WHERE m.Name = N'AllMenus' ORDER BY CAST(c.[key] AS INT);