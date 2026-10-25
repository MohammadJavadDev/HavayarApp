/*
  صفحه کاتالوگ گزارشات خدمات پس از فروش.
  - منوی «گزارشات» در انتهای system.SystemMenu با Name = AllMenus
  - دسترسی باز کردن صفحه برای نقش‌هایی که در HTS به کاتالوگ گزارش‌ها (System_FK = 14) دسترسی داشتند
    و همان عنوان گروه در system.Role وجود دارد، به‌اضافه ShowAllMenus و Sale.AfterSales.
  نمایه‌های هر گزارش در مرحله بعد با EntityFullName = N'AfterSalesReports' ساخته می‌شوند.
  UTF-8 BOM. Apply: sqlcmd -C -b -I -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-aftersales-report-page';
    DECLARE @Path NVARCHAR(300) = N'/panel/sale/aftersalesreport/list';
    DECLARE @Item NVARCHAR(MAX) = N'{"text":"گزارشات","icon":"ki-chart-simple-3 ki-outline","iconColor":"#A78BFA","path":"/panel/sale/aftersalesreport/list","a_attr":{"href":"/panel/sale/aftersalesreport/list"},"data":{"iconColor":"#A78BFA","path":"/panel/sale/aftersalesreport/list"},"children":[]}';

    IF NOT EXISTS (SELECT 1 FROM system.SystemMenu WHERE Name = N'AllMenus')
        THROW 50001, N'منوی AllMenus پیدا نشد.', 1;

    UPDATE system.SystemMenu
    SET Content = JSON_MODIFY(Content, 'append $', JSON_QUERY(@Item)),
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Name = N'AllMenus'
      AND ISJSON(Content) = 1
      AND Content NOT LIKE N'%' + @Path + N'%';

    PRINT N'  Menu append rowcount: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT @Path, 1, 1, NULL, N'لیست گزارشات', NULL, NULL, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    WHERE r.Name IN (
            N'ShowAllMenus',
            N'Sale.AfterSales',
            N'دسترسی مشاهده-سیستم خدمات پس از فروش',
            N'نمایندگان فروش',
            N'مشاهده کلی HTS'
        )
      AND NOT EXISTS (
            SELECT 1 FROM system.RoleAccess x
            WHERE x.RoleId = r.Id AND x.Path = @Path AND x.ActionAccessType = 1
        );

    PRINT N'  RoleAccess insert rowcount: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH

SELECT Name, ISJSON(Content) AS IsJson,
       CASE WHEN Content LIKE N'%/panel/sale/aftersalesreport/list%' THEN 1 ELSE 0 END AS HasReportMenu
FROM system.SystemMenu
WHERE Name = N'AllMenus';

SELECT r.Name, ra.Path, ra.ActionAccessType, ra.ActionAccessItemType
FROM system.RoleAccess ra
INNER JOIN system.Role r ON r.Id = ra.RoleId
WHERE ra.Path = N'/panel/sale/aftersalesreport/list';
