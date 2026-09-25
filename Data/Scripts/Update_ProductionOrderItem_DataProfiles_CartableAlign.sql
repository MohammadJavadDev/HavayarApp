/*
================================================================================
Update_ProductionOrderItem_DataProfiles_CartableAlign.sql
================================================================================
هم‌ترازسازی نمایه‌های اقلام سفارش ساخت با تصمیم محصول ۲۰۲۶-۰۹-۲۵:
  - یک کارتابل «منتظر تایید مهندسی» (فقط مشاهده) به‌جای برق/مکانیک جدا
  - کارتابل صنایع = همه ۱۹۰۴/۱۹۰۵ (بدون محدودیت ProductionStep) — عین HTS
  - عناوین/فیلتر/ActionOptions/RoleAccess برای مجموعه تأییدشده
  - دسترسی CRUD صنایع برای AcceptIndustrial + نمایه برای Industries
Idempotent. UTF-8 with BOM required.
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY

    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-poi-cartable-align-20260925';
    DECLARE @Entity NVARCHAR(200) = N'Entities.App.Sale.ProductionOrderItem';
    DECLARE @EntityLower NVARCHAR(200) = N'entities.app.sale.productionorderitem';

    -----------------------------------------------------------------------------
    -- 0) Base CustomQuery بدون WHERE
    -----------------------------------------------------------------------------
    DECLARE @BaseCq NVARCHAR(MAX);
    SELECT TOP 1 @BaseCq = CAST(JSON_VALUE(QueryJson, '$.CustomQuery') AS NVARCHAR(MAX))
    FROM system.SavedQuery
    WHERE Name IN (N'POI_All_Active', N'POI_Cartable_Industrial', N'productionorderitem_listinfonew')
      AND JSON_VALUE(QueryJson, '$.CustomQuery') IS NOT NULL
    ORDER BY CASE Name WHEN N'POI_All_Active' THEN 1 WHEN N'POI_Cartable_Industrial' THEN 2 ELSE 3 END;

    IF @BaseCq IS NULL OR LEN(@BaseCq) < 50
        THROW 52001, N'Base CustomQuery یافت نشد.', 1;

    SET @BaseCq = REPLACE(@BaseCq, N'[t6].[Name] AS [t9_Name]', N'[t9].[Name] AS [t9_Name]');
    IF CHARINDEX(N'WHERE', @BaseCq) > 0
        SET @BaseCq = LEFT(@BaseCq, CHARINDEX(N'WHERE', @BaseCq) - 1);
    SET @BaseCq = RTRIM(@BaseCq);

    DECLARE @Cols NVARCHAR(MAX), @Diag NVARCHAR(MAX), @Ev NVARCHAR(MAX);
    SELECT TOP 1 @Cols = ColumnsJson, @Diag = DiagramJson, @Ev = EventScriptsJson
    FROM system.SavedQuery WHERE Name = N'POI_All_Active';
    IF @Cols IS NULL
        SELECT TOP 1 @Cols = ColumnsJson, @Diag = DiagramJson, @Ev = EventScriptsJson
        FROM system.SavedQuery WHERE Id = 104;

    DECLARE @ActFull NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"},{"dataActionName":"defaultMultiSelect","enable":false,"title":"انتخاب چندتایی پیش‌فرض"}]';
    DECLARE @ActView NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"},{"dataActionName":"defaultMultiSelect","enable":false,"title":"انتخاب چندتایی پیش‌فرض"}]';
    DECLARE @ActIndustrial NVARCHAR(MAX) = @ActFull;

    DECLARE @SwapBtn NVARCHAR(MAX);
    SELECT TOP 1 @SwapBtn = CustomActionButtonsJson FROM system.SavedQuery WHERE Name = N'POI_Cartable_Industrial';
    IF @SwapBtn IS NULL OR @SwapBtn = N'' OR @SwapBtn = N'null'
        SELECT TOP 1 @SwapBtn = CustomActionButtonsJson FROM system.SavedQuery WHERE Name = N'POI_All_Active';
    IF @SwapBtn IS NULL SET @SwapBtn = N'[]';

    DECLARE @EmptyBtn NVARCHAR(MAX) = N'[]';
    IF @Ev IS NULL SET @Ev = N'{"onSelectedRow":"","onRowAdded":""}';
    IF @Diag IS NULL SET @Diag = N'{}';

    DECLARE @Profiles TABLE (
        Name NVARCHAR(100) PRIMARY KEY,
        Title NVARCHAR(200),
        WhereSql NVARCHAR(MAX),
        ActionOptions NVARCHAR(MAX),
        CustomButtons NVARCHAR(MAX),
        IsActive BIT
    );

    INSERT INTO @Profiles (Name, Title, WhereSql, ActionOptions, CustomButtons, IsActive) VALUES
    (N'POI_Cartable_Engineering', N'منتظر تایید مهندسی',
     N'
WHERE [t1].[IsDeleted] = 0 AND [t1].[IsLatestVersion] = 1
  AND [t1].[CheckStatus] = 2195',
     @ActView, @EmptyBtn, 1),
    (N'POI_Cartable_Industrial', N'کارتابل صنایع — اقلام سفارش ساخت',
     N'
WHERE [t1].[IsDeleted] = 0 AND [t1].[IsLatestVersion] = 1
  AND [t1].[CheckStatus] IN (1904, 1905)',
     @ActIndustrial, @SwapBtn, 1),
    (N'POI_Cartable_ProjectManager', N'منتظر تایید مدیر پروژه',
     N'
WHERE [t1].[IsDeleted] = 0 AND [t1].[IsLatestVersion] = 1
  AND [t1].[CheckStatus] = 2258',
     @ActFull, @EmptyBtn, 1),
    (N'POI_Cartable_SupplyCommittee', N'کارتابل رئیس کمیته تامین',
     N'
WHERE [t1].[IsDeleted] = 0 AND [t1].[IsLatestVersion] = 1
  AND [t1].[CheckStatus] IN (1909, 2202)',
     @ActFull, @EmptyBtn, 1),
    (N'POI_Cartable_Inquiry', N'کارتابل استعلام',
     N'
WHERE [t1].[IsDeleted] = 0 AND [t1].[IsLatestVersion] = 1
  AND [t1].[CheckStatus] = 1906',
     @ActFull, @EmptyBtn, 1),
    (N'POI_Cartable_Ministry', N'کارتابل وزارت صنایع',
     N'
WHERE [t1].[IsDeleted] = 0 AND [t1].[IsLatestVersion] = 1
  AND [t1].[CheckStatus] = 1908',
     @ActFull, @EmptyBtn, 1),
    (N'POI_All_Active', N'اقلام سفارش ساخت — همه فعال',
     N'
WHERE [t1].[IsDeleted] = 0 AND [t1].[IsLatestVersion] = 1',
     @ActFull, @SwapBtn, 1),
    (N'POI_Sales_Related_Active', N'اقلام سفارش ساخت — مرتبط با من',
     N'
WHERE [t1].[IsDeleted] = 0 AND [t1].[IsLatestVersion] = 1
  AND [t2].[State] = 3
  AND (
        [t1].[CreatedById]  = TRY_CAST(@CurrentUserId AS BIGINT)
     OR [t1].[ModifiedById] = TRY_CAST(@CurrentUserId AS BIGINT)
     OR [t2].[CreatedById]  = TRY_CAST(@CurrentUserId AS BIGINT)
     OR [t2].[SalesExpertId] = TRY_CAST(@CurrentUserId AS BIGINT)
     OR [t2].[SalesManagerId] = TRY_CAST(@CurrentUserId AS BIGINT)
     OR [t2].[AlternativeExpertId] = TRY_CAST(@CurrentUserId AS BIGINT)
     OR [t2].[IntroducerExpertId] = TRY_CAST(@CurrentUserId AS BIGINT)
     OR [t2].[ProjectManagerId] = TRY_CAST(@CurrentUserId AS BIGINT)
  )',
     @ActFull, @EmptyBtn, 1),
    (N'POI_ViewActive', N'اقلام سفارش ساخت — در فرآیند',
     N'
WHERE [t1].[IsDeleted] = 0 AND [t1].[IsLatestVersion] = 1
  AND [t2].[State] = 3',
     @ActView, @EmptyBtn, 1),
    (N'POI_History_Deleted', N'پیشینه اقلام سفارش ساخت (حذف‌شده)',
     N'
WHERE [t1].[IsDeleted] = 1',
     @ActView, @EmptyBtn, 1);

    DECLARE @PName NVARCHAR(100), @PTitle NVARCHAR(200), @PWhere NVARCHAR(MAX), @PAct NVARCHAR(MAX), @PBtn NVARCHAR(MAX), @PActive BIT;
    DECLARE @NewQj NVARCHAR(MAX);

    DECLARE pc CURSOR LOCAL FAST_FORWARD FOR
        SELECT Name, Title, WhereSql, ActionOptions, CustomButtons, IsActive FROM @Profiles;
    OPEN pc;
    FETCH NEXT FROM pc INTO @PName, @PTitle, @PWhere, @PAct, @PBtn, @PActive;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @NewQj = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":null,"Parameters":[],"Selects":null}';
        SET @NewQj = JSON_MODIFY(@NewQj, '$.CustomQuery', @BaseCq + @PWhere);

        IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @PName)
        BEGIN
            UPDATE system.SavedQuery
            SET Title = @PTitle,
                QueryJson = @NewQj,
                ColumnsJson = ISNULL(@Cols, ColumnsJson),
                DiagramJson = ISNULL(@Diag, DiagramJson),
                ActionOptions = @PAct,
                CustomActionButtonsJson = @PBtn,
                EventScriptsJson = ISNULL(@Ev, EventScriptsJson),
                EntityFullName = @EntityLower,
                Type = 1,
                Mode = 1,
                IsActive = @PActive,
                ModifiedById = 1,
                ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now,
                ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Name = @PName;
            PRINT N'  UPDATED ' + @PName;
        END
        ELSE
        BEGIN
            INSERT INTO system.SavedQuery
                (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
                 CreatedById, ModifiedById, CreatedByName, ModifiedByName,
                 CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime,
                 IsActive, EntityFullName, Type, Mode, ActionOptions)
            VALUES
                (@PName, @PTitle, @NewQj, @Cols, @Diag, @PBtn, @Ev,
                 1, 1, @SeedUser, @SeedUser,
                 @Now, @Now, @NowShamsi, @NowShamsi,
                 @PActive, @EntityLower, 1, 1, @PAct);
            PRINT N'  CREATED ' + @PName;
        END

        FETCH NEXT FROM pc INTO @PName, @PTitle, @PWhere, @PAct, @PBtn, @PActive;
    END
    CLOSE pc;
    DEALLOCATE pc;

    -----------------------------------------------------------------------------
    -- 1) بازنشسته کردن کارتابل‌های مهندسی برق/مکانیک
    -----------------------------------------------------------------------------
    UPDATE system.SavedQuery
    SET IsActive = 0,
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Name IN (N'POI_Cartable_Engineering_Electrical', N'POI_Cartable_Engineering_Mechanical');
    PRINT N'  Retired old eng cartables: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    DELETE ra
    FROM system.RoleAccess ra
    INNER JOIN system.SavedQuery q ON q.Id = ra.RowId
    WHERE ra.ActionAccessType = 3
      AND q.Name IN (N'POI_Cartable_Engineering_Electrical', N'POI_Cartable_Engineering_Mechanical');
    PRINT N'  Removed RoleAccess for old eng profiles: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    -----------------------------------------------------------------------------
    -- 2) RoleAccess DataProfile
    -----------------------------------------------------------------------------
    DECLARE @Map TABLE (ProfileName NVARCHAR(100), RoleName NVARCHAR(200));
    INSERT INTO @Map VALUES
        (N'POI_Cartable_Engineering', N'Sale.ProductionOrderItem.AcceptEngineeringMechanical'),
        (N'POI_Cartable_Engineering', N'Sale.ProductionOrderItem.AcceptEngineeringElectrical'),
        (N'POI_Cartable_Industrial', N'Sale.ProductionOrderItem.AcceptIndustrial'),
        (N'POI_Cartable_Industrial', N'Industries'),
        (N'POI_Cartable_ProjectManager', N'Sale.ProductionOrderItem.AcceptProjectManager'),
        (N'POI_Cartable_SupplyCommittee', N'Sale.ProductionOrderItem.SupplyCommitteeBoss'),
        (N'POI_Cartable_Inquiry', N'Sale.ProductionOrderItem.Inquirer'),
        (N'POI_Cartable_Ministry', N'Sale.ProductionOrderItem.MinistryOfIndustryInquirer'),
        (N'POI_All_Active', N'Sale.ProductionOrder.ShowAll'),
        (N'POI_Sales_Related_Active', N'Sale.ProductionOrderItem.ViewRelated'),
        (N'POI_ViewActive', N'Sale.ProductionOrderItem.ViewActive'),
        (N'POI_History_Deleted', N'Sale.ProductionOrderItem.History'),
        (N'POI_History_Deleted', N'Sale.ProductionOrder.ShowAll'),
        (N'POI_MyCartable', N'Sale.ProductionOrderItem.AcceptIndustrial'),
        (N'POI_MyCartable', N'Sale.ProductionOrderItem.AcceptEngineeringMechanical'),
        (N'POI_MyCartable', N'Sale.ProductionOrderItem.AcceptEngineeringElectrical'),
        (N'POI_MyCartable', N'Sale.ProductionOrderItem.AcceptProjectManager'),
        (N'POI_MyCartable', N'Sale.ProductionOrderItem.Inquirer'),
        (N'POI_MyCartable', N'Sale.ProductionOrderItem.MinistryOfIndustryInquirer'),
        (N'POI_MyCartable', N'Sale.ProductionOrderItem.SupplyCommitteeBoss'),
        (N'POI_MyCartable', N'Sale.ProductionOrder.ShowAll'),
        (N'POI_All_ReadyForShipping', N'Sale.ProductionOrderItem.ReadyForShipping');

    DELETE ra
    FROM system.RoleAccess ra
    INNER JOIN system.SavedQuery q ON q.Id = ra.RowId
    WHERE ra.ActionAccessType = 3
      AND q.Name IN (
        N'POI_Cartable_Engineering', N'POI_Cartable_Industrial', N'POI_Cartable_ProjectManager',
        N'POI_Cartable_SupplyCommittee', N'POI_Cartable_Inquiry', N'POI_Cartable_Ministry',
        N'POI_All_Active', N'POI_Sales_Related_Active', N'POI_ViewActive', N'POI_History_Deleted',
        N'POI_MyCartable', N'POI_All_ReadyForShipping');
    PRINT N'  Cleared mapped profile RoleAccess: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName,
         EntityId, RowId, RoleId, CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        N'dataProfile_' + CAST(q.Id AS NVARCHAR(20)),
        3, 7, @Entity, q.Title,
        NULL, q.Id, r.Id, 1, 1, @SeedUser, @SeedUser,
        @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM @Map m
    INNER JOIN system.SavedQuery q ON q.Name = m.ProfileName AND q.IsActive = 1
    INNER JOIN system.Role r ON r.Name = m.RoleName;
    PRINT N'  Inserted DataProfile RoleAccess: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    -- ShowAll ممکن است دو ردیف هم‌نام داشته باشد
    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName,
         EntityId, RowId, RoleId, CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        N'dataProfile_' + CAST(q.Id AS NVARCHAR(20)),
        3, 7, @Entity, q.Title,
        NULL, q.Id, r.Id, 1, 1, @SeedUser, @SeedUser,
        @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    CROSS JOIN system.SavedQuery q
    WHERE r.Name = N'Sale.ProductionOrder.ShowAll'
      AND q.Name IN (N'POI_All_Active', N'POI_History_Deleted', N'POI_MyCartable')
      AND q.IsActive = 1
      AND NOT EXISTS (
          SELECT 1 FROM system.RoleAccess ra
          WHERE ra.RoleId = r.Id AND ra.RowId = q.Id AND ra.ActionAccessType = 3);
    PRINT N'  Extra ShowAll profile grants: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    -----------------------------------------------------------------------------
    -- 3) دسترسی صفحه + CRUD
    -----------------------------------------------------------------------------
    DECLARE @PageRows TABLE (Path NVARCHAR(400), AccessType INT, ItemType INT, RoleName NVARCHAR(200));
    INSERT INTO @PageRows (Path, AccessType, ItemType, RoleName)
    SELECT p.Path, p.AccessType, p.ItemType, r.RoleName
    FROM (VALUES
        (N'/panel/productionorderitem/list', 1, 1),
        (N'/panel/productionorderitem/edit', 1, 5),
        (N'/panel/productionorderitem/new', 1, 4),
        (N'/panel/productionorderitem/fetchdata', 2, 2),
        (N'/panel/productionorderitem/exporttoexcel', 2, 0)
    ) p(Path, AccessType, ItemType)
    CROSS JOIN (VALUES
        (N'Sale.ProductionOrderItem.AcceptIndustrial'),
        (N'Sale.ProductionOrderItem.AcceptEngineeringMechanical'),
        (N'Sale.ProductionOrderItem.AcceptEngineeringElectrical'),
        (N'Sale.ProductionOrderItem.AcceptProjectManager'),
        (N'Sale.ProductionOrderItem.Inquirer'),
        (N'Sale.ProductionOrderItem.MinistryOfIndustryInquirer'),
        (N'Sale.ProductionOrderItem.SupplyCommitteeBoss'),
        (N'Sale.ProductionOrderItem.ViewActive'),
        (N'Sale.ProductionOrderItem.ViewRelated'),
        (N'Sale.ProductionOrderItem.History'),
        (N'Sale.ProductionOrderItem.ReadyForShipping')
    ) r(RoleName);

    INSERT INTO @PageRows (Path, AccessType, ItemType, RoleName) VALUES
        (N'/panel/productionorderitem/add', 2, 4, N'Sale.ProductionOrderItem.AcceptIndustrial'),
        (N'/panel/productionorderitem/save', 2, 3, N'Sale.ProductionOrderItem.AcceptIndustrial'),
        (N'/panel/productionorderitem/update', 2, 5, N'Sale.ProductionOrderItem.AcceptIndustrial'),
        (N'/panel/productionorderitem/delete', 2, 6, N'Sale.ProductionOrderItem.AcceptIndustrial'),
        (N'/panel/productionorderitem/swap', 2, 0, N'Sale.ProductionOrderItem.AcceptIndustrial'),
        -- BOM paths: فقط نقش Sale.ProductionOrderItemBom (Seed_ProductionOrderItemBom_RolesAndAccess.sql)
        (N'/panel/productionorderitem/add', 2, 4, N'Industries'),
        (N'/panel/productionorderitem/save', 2, 3, N'Industries'),
        (N'/panel/productionorderitem/update', 2, 5, N'Industries'),
        (N'/panel/productionorderitem/delete', 2, 6, N'Industries'),
        (N'/panel/productionorderitem/swap', 2, 0, N'Industries'),
        (N'/panel/productionorderitem/acceptengineering/{id}', 2, 1000, N'Sale.ProductionOrderItem.AcceptEngineeringMechanical'),
        (N'/panel/productionorderitem/acceptengineering/{id}', 2, 1000, N'Sale.ProductionOrderItem.AcceptEngineeringElectrical'),
        (N'/panel/productionorderitem/acceptprojectmanager/{id}', 2, 1000, N'Sale.ProductionOrderItem.AcceptProjectManager');

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT x.Path, x.AccessType, x.ItemType, @Entity, N'', NULL, NULL, r.Id,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM @PageRows x
    INNER JOIN system.Role r ON r.Name = x.RoleName
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess ra
        WHERE ra.RoleId = r.Id AND ra.Path = x.Path AND ra.ActionAccessType = x.AccessType);
    PRINT N'  Inserted page/API RoleAccess: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    -----------------------------------------------------------------------------
    -- 4) Industries users → AcceptIndustrial (برای کارتابل من)
    -----------------------------------------------------------------------------
    DECLARE @RoleIndustrial BIGINT = (SELECT TOP 1 Id FROM system.Role WHERE Name = N'Sale.ProductionOrderItem.AcceptIndustrial');
    DECLARE @AssignedInd INT = 0, @SkipInd INT = 0;
    DECLARE @Uid BIGINT, @Uname NVARCHAR(200);

    DECLARE ic CURSOR LOCAL FAST_FORWARD FOR
        SELECT u.Id, u.UserName
        FROM system.[User] u
        WHERE u.IsActive = 1 AND ISJSON(u.RoleIds) = 1
          AND EXISTS (SELECT 1 FROM OPENJSON(u.RoleIds) j WHERE TRY_CAST(j.[value] AS BIGINT) = 3);
    OPEN ic;
    FETCH NEXT FROM ic INTO @Uid, @Uname;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF EXISTS (SELECT 1 FROM system.[User] u CROSS APPLY OPENJSON(u.RoleIds) j WHERE u.Id = @Uid AND TRY_CAST(j.[value] AS BIGINT) = @RoleIndustrial)
            SET @SkipInd = @SkipInd + 1;
        ELSE IF @RoleIndustrial IS NOT NULL
        BEGIN
            UPDATE system.[User]
            SET RoleIds = CASE
                    WHEN RoleIds IS NULL OR LTRIM(RTRIM(RoleIds)) = N'' OR RoleIds = N'[]'
                        THEN N'[' + CAST(@RoleIndustrial AS NVARCHAR(20)) + N']'
                    ELSE STUFF(RoleIds, LEN(RoleIds), 1, N',' + CAST(@RoleIndustrial AS NVARCHAR(20)) + N']')
                END,
                Roles = CASE
                    WHEN EXISTS (SELECT 1 FROM OPENJSON(Roles) j WHERE j.[value] = N'Sale.ProductionOrderItem.AcceptIndustrial') THEN Roles
                    WHEN Roles IS NULL OR LTRIM(RTRIM(Roles)) = N'' OR Roles = N'[]'
                        THEN N'["Sale.ProductionOrderItem.AcceptIndustrial"]'
                    ELSE STUFF(Roles, LEN(Roles), 1, N',"Sale.ProductionOrderItem.AcceptIndustrial"]')
                END,
                ModifiedById = 1, ModifiedByName = @SeedUser, ModifiedDateMiladiDateTime = @Now
            WHERE Id = @Uid;
            SET @AssignedInd = @AssignedInd + 1;
            PRINT N'  +AcceptIndustrial for Industries user: ' + @Uname;
        END
        FETCH NEXT FROM ic INTO @Uid, @Uname;
    END
    CLOSE ic; DEALLOCATE ic;
    PRINT N'  Industries→AcceptIndustrial assigned=' + CAST(@AssignedInd AS NVARCHAR(20)) + N' skipped=' + CAST(@SkipInd AS NVARCHAR(20));

    -- PO header profiles
    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(q.Id AS NVARCHAR(20)), 3, 7, N'Entities.App.Sale.ProductionOrder', q.Title, NULL, q.Id, r.Id,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.SavedQuery q
    CROSS JOIN system.Role r
    WHERE q.Name = N'PO_All' AND r.Name = N'Sale.ProductionOrder.ShowAll' AND q.IsActive = 1
      AND NOT EXISTS (SELECT 1 FROM system.RoleAccess ra WHERE ra.RoleId = r.Id AND ra.RowId = q.Id AND ra.ActionAccessType = 3);

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(q.Id AS NVARCHAR(20)), 3, 7, N'Entities.App.Sale.ProductionOrder', q.Title, NULL, q.Id, r.Id,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.SavedQuery q
    INNER JOIN system.Role r ON r.Name = N'Sale.ProductionOrder.ViewRelated'
    WHERE q.Name = N'PO_Related' AND q.IsActive = 1
      AND NOT EXISTS (SELECT 1 FROM system.RoleAccess ra WHERE ra.RoleId = r.Id AND ra.RowId = q.Id AND ra.ActionAccessType = 3);

    COMMIT TRANSACTION;
    PRINT N'=== DONE Update_ProductionOrderItem_DataProfiles_CartableAlign ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH

SELECT Id, Name, Title, IsActive,
  CASE WHEN CHARINDEX(N'WHERE', CAST(JSON_VALUE(QueryJson,'$.CustomQuery') AS NVARCHAR(MAX)))>0
       THEN SUBSTRING(CAST(JSON_VALUE(QueryJson,'$.CustomQuery') AS NVARCHAR(MAX)), CHARINDEX(N'WHERE', CAST(JSON_VALUE(QueryJson,'$.CustomQuery') AS NVARCHAR(MAX))), 220)
       ELSE N'(no WHERE)' END AS WherePart,
  LEFT(ISNULL(ActionOptions,N''), 160) AS Act
FROM system.SavedQuery
WHERE Name LIKE N'POI_%' OR Name LIKE N'PO_%'
ORDER BY Name;

SELECT q.Name, q.Title, r.Name AS RoleName
FROM system.RoleAccess ra
JOIN system.SavedQuery q ON q.Id = ra.RowId
JOIN system.Role r ON r.Id = ra.RoleId
WHERE ra.ActionAccessType = 3
  AND (q.Name LIKE N'POI_%' OR q.Name LIKE N'PO_%')
ORDER BY q.Name, r.Name;
