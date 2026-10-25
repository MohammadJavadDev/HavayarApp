/*
  Seed_InquiryPartPrice_DataProfiles.sql
  نمایه داده استعلام قیمت کالا (entities.app.sup.inquirypartprice)
  بر اساس صفحه HTS Inquiry_PartPrice (88) و GetInquiryPartPriceGridData.

  فیلتر HTS: بدون AfterSaleAccess رکوردهای InquiryType_FK = 1675 دیده نمی‌شوند.
  PermissionType.AfterSaleAccess روی این صفحه به هیچ گروهی داده نشده است.
  استثنای OrganizationUnitId = 51 (تامین و خرید) در کد قدیم همه انواع از جمله خدمات پس از فروش را نشان می‌دهد؛
  این سید آن استثنا را اعمال نمی‌کند: کاربران تدارکات نباید استعلام خدمات پس از فروش را ببینند.
  گروه 388 «کارشناسان خدمات پس از فروش استعلام قیمت» نمایه شامل نوع 1675 را می‌گیرد.

  نوع 1675 = InquiryPartPriceTypeEnum.AfterSalesInquiry
  Encoding: UTF-8 with BOM. Apply: sqlcmd -C -b -I -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-inquiry-partprice-profiles';
    DECLARE @EntityLower NVARCHAR(200) = N'entities.app.sup.inquirypartprice';
    DECLARE @EntityPascal NVARCHAR(200) = N'Entities.App.Sup.InquiryPartPrice';

    DECLARE @NameAll NVARCHAR(200) = N'inquirypartprice_listinfo';
    DECLARE @TitleAll NVARCHAR(200) = N'سوابق خرید — شامل خدمات پس از فروش';
    DECLARE @NameFiltered NVARCHAR(200) = N'InquiryPartPrice_WithoutAfterSales';
    DECLARE @TitleFiltered NVARCHAR(200) = N'سوابق خرید — بدون خدمات پس از فروش';

    -----------------------------------------------------------------------------
    PRINT N'=== [1] ColumnsJson: sort on تاریخ ایجاد + شماره فاکتور ===';
    -----------------------------------------------------------------------------
    DECLARE @Cols NVARCHAR(MAX);
    SELECT @Cols = ColumnsJson FROM system.SavedQuery WHERE Name = @NameAll;
    IF @Cols IS NULL OR ISJSON(@Cols) = 0
        THROW 51001, N'SavedQuery inquirypartprice_listinfo ColumnsJson missing or invalid', 1;

    DECLARE @NewCols NVARCHAR(MAX);
    SELECT @NewCols = N'[' + STRING_AGG(CAST(
        CASE
            WHEN JSON_VALUE(j.[value], '$.Alliance') = N't1_CreatedOnShamsiDateTime'
                THEN JSON_MODIFY(JSON_MODIFY(j.[value], '$.SortDirection', CAST(1 AS int)), '$.SortOrder', CAST(0 AS int))
            WHEN JSON_VALUE(j.[value], '$.Alliance') = N't1_InvoiceNumber'
                THEN JSON_MODIFY(j.[value], '$.DisplayName', N'شماره فاکتور')
            WHEN JSON_VALUE(j.[value], '$.Alliance') IN (N't1_Type', N't1_PriceStatus')
                THEN JSON_MODIFY(j.[value], '$.Render', NULL) -- Select/enum: grid shows label, not raw number
            WHEN JSON_VALUE(j.[value], '$.Alliance') = N't3_Title'
                THEN JSON_MODIFY(j.[value], '$.DisplayName', N'واحد ارز')
            ELSE j.[value]
        END AS NVARCHAR(MAX)), N',') WITHIN GROUP (ORDER BY CAST(j.[key] AS int)) + N']'
    FROM OPENJSON(@Cols) j;

    IF @NewCols IS NULL OR ISJSON(@NewCols) = 0
        THROW 51002, N'ColumnsJson rebuild failed', 1;

    DECLARE @ActionOptions NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"},{"dataActionName":"defaultMultiSelect","enable":false,"title":"انتخاب چندتایی پیش‌فرض"}]';

    DECLARE @BaseSelect NVARCHAR(MAX) = N'SELECT
    [t1].[Id] AS [t1_Id],
    [t1].[Type] AS [t1_Type],
    [t1].[CompanyName] AS [t1_CompanyName],
    [t2].[Name] AS [t2_Name],
    [t2].[Code] AS [t2_Code],
    [t1].[BuyCount] AS [t1_BuyCount],
    [t1].[PriceUnitValue] AS [t1_PriceUnitValue],
    [t1].[TransportationCost] AS [t1_TransportationCost],
    [t1].[UnitPrice] AS [t1_UnitPrice],
    [t1].[IsFixedPrice] AS [t1_IsFixedPrice],
    [t1].[Comment] AS [t1_Comment],
    [t1].[ValidShamsiDate] AS [t1_ValidShamsiDate],
    [t1].[CurrencyStandardPrice] AS [t1_CurrencyStandardPrice],
    [t1].[CurrencyActualPrice] AS [t1_CurrencyActualPrice],
    [t1].[ShippingStandardPrice] AS [t1_ShippingStandardPrice],
    [t1].[ShippingActualPrice] AS [t1_ShippingActualPrice],
    [t1].[TotalStandardPrice] AS [t1_TotalStandardPrice],
    [t1].[TotalActualPrice] AS [t1_TotalActualPrice],
    [t1].[IsSpecialCases] AS [t1_IsSpecialCases],
    [t1].[SpecialCasesDescription] AS [t1_SpecialCasesDescription],
    [t1].[PriceStatus] AS [t1_PriceStatus],
    [t1].[NewFeature] AS [t1_NewFeature],
    [t3].[Title] AS [t3_Title],
    [t1].[CreatedByName] AS [t1_CreatedByName],
    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime],
    [t1].[InvoiceNumber] AS [t1_InvoiceNumber]
FROM [Sup].[InquiryPartPrice] AS [t1]
LEFT JOIN [Inv].[Part] AS [t2]
    ON [t1].[PartId] = [t2].[Id]
LEFT JOIN [Acc].[PriceUnit] AS [t3]
    ON [t1].[PriceUnitId] = [t3].[Id]';

    -- 1675 = استعلام توسط خدمات پس از فروش
    DECLARE @FilteredSelect NVARCHAR(MAX) = @BaseSelect + N'
WHERE ISNULL([t1].[Type], 0) <> 1675';

    DECLARE @QueryAll NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"Parameters":[],"Selects":null}';
    SET @QueryAll = JSON_MODIFY(@QueryAll, '$.CustomQuery', @BaseSelect);
    DECLARE @QueryFiltered NVARCHAR(MAX) = JSON_MODIFY(@QueryAll, '$.CustomQuery', @FilteredSelect);

    -----------------------------------------------------------------------------
    PRINT N'=== [2] Upsert SavedQuery ===';
    -----------------------------------------------------------------------------
    UPDATE system.SavedQuery
    SET Title = @TitleAll,
        QueryJson = @QueryAll,
        ColumnsJson = @NewCols,
        DiagramJson = N'{}',
        Mode = 1,
        [Type] = 1,
        EntityFullName = @EntityLower,
        ActionOptions = @ActionOptions,
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Name = @NameAll;

    DECLARE @IdAll BIGINT = (SELECT Id FROM system.SavedQuery WHERE Name = @NameAll);

    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @NameFiltered)
    BEGIN
        UPDATE system.SavedQuery
        SET Title = @TitleFiltered,
            QueryJson = @QueryFiltered,
            ColumnsJson = @NewCols,
            DiagramJson = N'{}',
            Mode = 1,
            [Type] = 1,
            EntityFullName = @EntityLower,
            ActionOptions = @ActionOptions,
            CustomActionButtonsJson = N'[]',
            EventScriptsJson = N'{"onSelectedRow":"","onRowAdded":""}',
            IsActive = 1,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Name = @NameFiltered;
    END
    ELSE
    BEGIN
        INSERT INTO system.SavedQuery
            (Name, Title, QueryJson, ColumnsJson, DiagramJson,
             CustomActionButtonsJson, EventScriptsJson,
             Mode, [Type], EntityFullName, ActionOptions,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
             IsActive)
        VALUES
            (@NameFiltered, @TitleFiltered, @QueryFiltered, @NewCols, N'{}',
             N'[]', N'{"onSelectedRow":"","onRowAdded":""}',
             1, 1, @EntityLower, @ActionOptions,
             1, 1, @SeedUser, @SeedUser,
             @Now, @NowShamsi, @Now, @NowShamsi,
             1);
    END

    DECLARE @IdFiltered BIGINT = (SELECT Id FROM system.SavedQuery WHERE Name = @NameFiltered);

    -----------------------------------------------------------------------------
    PRINT N'=== [3] Roles (HTS groups 93 / 332 / 388 + direct readers) ===';
    -----------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#RoleWanted') IS NOT NULL DROP TABLE #RoleWanted;
    CREATE TABLE #RoleWanted
    (
        WantedId BIGINT NOT NULL PRIMARY KEY,
        RoleName NVARCHAR(200) NOT NULL,
        RoleTitle NVARCHAR(200) NOT NULL
    );
    INSERT INTO #RoleWanted (WantedId, RoleName, RoleTitle) VALUES
        (260093, N'دسترسی های کارشناسان مهندسی به سیستم تدارکات', N'دسترسی های کارشناسان مهندسی به سیستم تدارکات'),
        (260332, N'کارشناسان استعلام قیمت (تدارکات)', N'کارشناسان استعلام قیمت (تدارکات)'),
        (260388, N'کارشناسان خدمات پس از فروش استعلام قیمت', N'کارشناسان خدمات پس از فروش استعلام قیمت');

    DECLARE @WantedId BIGINT, @RoleName NVARCHAR(200), @RoleTitle NVARCHAR(200);
    DECLARE role_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT WantedId, RoleName, RoleTitle FROM #RoleWanted;
    OPEN role_cur;
    FETCH NEXT FROM role_cur INTO @WantedId, @RoleName, @RoleTitle;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF EXISTS (SELECT 1 FROM system.Role WHERE Name = @RoleName AND Id <> @WantedId)
            SET @RoleName = @RoleTitle + N' (' + CAST(@WantedId - 260000 AS nvarchar(20)) + N')';

        IF EXISTS (SELECT 1 FROM system.Role WHERE Id = @WantedId)
        BEGIN
            UPDATE system.Role
            SET Name = @RoleName, Title = @RoleTitle, IsActive = 1,
                ModifiedById = 1, ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Id = @WantedId;
        END
        ELSE
        BEGIN
            SET IDENTITY_INSERT system.Role ON;
            INSERT INTO system.Role
                (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
                 CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime,
                 CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
            VALUES
                (@WantedId, @RoleName, @RoleTitle, 1, @SeedUser, 1, @SeedUser,
                 @Now, @Now, @NowShamsi, @NowShamsi, 1);
            SET IDENTITY_INSERT system.Role OFF;
        END
        FETCH NEXT FROM role_cur INTO @WantedId, @RoleName, @RoleTitle;
    END
    CLOSE role_cur;
    DEALLOCATE role_cur;

    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Sup.InquiryPartPrice.Read')
    BEGIN
        INSERT INTO system.Role
            (Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
             CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime,
             CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (N'Sup.InquiryPartPrice.Read', N'استعلام قیمت کالا - مشاهده',
             1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
    END
    ELSE
    BEGIN
        UPDATE system.Role
        SET Title = N'استعلام قیمت کالا - مشاهده', IsActive = 1,
            ModifiedById = 1, ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Name = N'Sup.InquiryPartPrice.Read';
    END

    DECLARE @DirectRoleId BIGINT = (SELECT Id FROM system.Role WHERE Name = N'Sup.InquiryPartPrice.Read');

    -----------------------------------------------------------------------------
    PRINT N'=== [4] Page + DataProfile RoleAccess ===';
    -----------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#ProfileRole') IS NOT NULL DROP TABLE #ProfileRole;
    CREATE TABLE #ProfileRole
    (
        RoleId BIGINT NOT NULL PRIMARY KEY,
        ProfileId BIGINT NOT NULL
    );
    INSERT INTO #ProfileRole (RoleId, ProfileId)
    SELECT r.Id, @IdFiltered
    FROM system.Role r
    WHERE r.Id IN (260027, 260093, 260332, 260350)
       OR r.Id = @DirectRoleId;

    INSERT INTO #ProfileRole (RoleId, ProfileId)
    SELECT r.Id, @IdAll
    FROM system.Role r
    WHERE r.Id IN (260388, 600055, 200052)
      AND NOT EXISTS (SELECT 1 FROM #ProfileRole x WHERE x.RoleId = r.Id);

    -- این نقش‌ها فقط یکی از دو نمایه را می‌بینند
    DELETE ra
    FROM system.RoleAccess ra
    INNER JOIN #ProfileRole pr ON pr.RoleId = ra.RoleId
    WHERE ra.ActionAccessType = 3
      AND ra.EntityName = @EntityPascal
      AND (ra.RowId IS NULL OR ra.RowId <> pr.ProfileId);

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(pr.ProfileId AS nvarchar(20)),
           3, 7, @EntityPascal, NULL, NULL, pr.ProfileId, pr.RoleId,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #ProfileRole pr
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess ra
        WHERE ra.RoleId = pr.RoleId AND ra.ActionAccessType = 3 AND ra.RowId = pr.ProfileId
    );

    IF OBJECT_ID('tempdb..#PageAccess') IS NOT NULL DROP TABLE #PageAccess;
    CREATE TABLE #PageAccess
    (
        Path NVARCHAR(300) NOT NULL,
        ActionAccessType INT NOT NULL,
        ActionAccessItemType INT NOT NULL
    );
    INSERT INTO #PageAccess (Path, ActionAccessType, ActionAccessItemType) VALUES
        (N'/panel/sup/inquirypartprice/list', 1, 1),
        (N'/panel/sup/inquirypartprice/edit', 1, 5),
        (N'/panel/sup/inquirypartprice/fetchdata', 2, 2),
        (N'/panel/sup/inquirypartprice/exporttoexcel', 2, 0);

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT p.Path, p.ActionAccessType, p.ActionAccessItemType, @EntityPascal, NULL, NULL, NULL, pr.RoleId,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #ProfileRole pr
    CROSS JOIN #PageAccess p
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess ra
        WHERE ra.RoleId = pr.RoleId AND ra.Path = p.Path AND ra.ActionAccessType = p.ActionAccessType
    );

    -- نقش نمایش‌همه از قبل صفحه را دارد؛ خروجی اکسل را هم اگر نباشد اضافه کن
    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT p.Path, p.ActionAccessType, p.ActionAccessItemType, @EntityPascal, NULL, NULL, NULL, r.Id,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    CROSS JOIN #PageAccess p
    WHERE r.Id IN (600055, 200052)
      AND NOT EXISTS (
          SELECT 1 FROM system.RoleAccess ra
          WHERE ra.RoleId = r.Id AND ra.Path = p.Path AND ra.ActionAccessType = p.ActionAccessType
      );

    -----------------------------------------------------------------------------
    PRINT N'=== [5] Assign HTS group members and direct page readers ===';
    -----------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#UserRoleMap') IS NOT NULL DROP TABLE #UserRoleMap;
    CREATE TABLE #UserRoleMap
    (
        Username NVARCHAR(200) NOT NULL,
        RoleName NVARCHAR(200) NOT NULL,
        RoleId   BIGINT        NOT NULL
    );

    INSERT INTO #UserRoleMap (Username, RoleName, RoleId)
    SELECT DISTINCT COALESCE(
            NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''),
            NULLIF(LTRIM(RTRIM(u.Username)), N'')),
           rl.Name,
           rl.Id
    FROM [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
    INNER JOIN system.Role rl ON rl.Id = (260000 + m.UserGroup_FK)
    WHERE m.UserGroup_FK IN (27, 93, 332, 350, 388)
      AND u.IsActive = 1
      AND COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')) IS NOT NULL;

    INSERT INTO #UserRoleMap (Username, RoleName, RoleId)
    SELECT DISTINCT COALESCE(
            NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''),
            NULLIF(LTRIM(RTRIM(u.Username)), N'')),
           N'Sup.InquiryPartPrice.Read',
           @DirectRoleId
    FROM [TMS].[TotalSystem].[dbo].[Vw_Permission] v
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = v.User_FK
    WHERE v.Page_ID = 88
      AND v.UserGroup_ID IS NULL
      AND v.IsActive = 1
      AND v.Permission_ID IN (3, 8)
      AND u.IsActive = 1
      AND NOT EXISTS (
          SELECT 1
          FROM [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m
          WHERE m.User_FK = u.User_ID
            AND m.UserGroup_FK IN (27, 93, 332, 350, 388)
      )
      AND COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')) IS NOT NULL
      AND NOT EXISTS (
          SELECT 1 FROM #UserRoleMap x
          WHERE LOWER(x.Username) = LOWER(COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), u.Username))
            AND x.RoleId = @DirectRoleId
      );

    ;WITH d AS (
        SELECT Username, RoleId,
               ROW_NUMBER() OVER (PARTITION BY LOWER(Username), RoleId ORDER BY Username) AS rn
        FROM #UserRoleMap
    )
    DELETE FROM d WHERE rn > 1;

    DECLARE @MapUsername NVARCHAR(200), @MapRoleName NVARCHAR(200), @MapRoleId BIGINT, @MapUserId BIGINT;
    DECLARE @Assigned INT = 0, @Missing INT = 0, @SkippedAlready INT = 0;

    DECLARE map_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT Username, RoleName, RoleId FROM #UserRoleMap;
    OPEN map_cur;
    FETCH NEXT FROM map_cur INTO @MapUsername, @MapRoleName, @MapRoleId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT TOP 1 @MapUserId = Id
        FROM system.[User]
        WHERE LOWER(Username) = LOWER(@MapUsername);

        IF @MapUserId IS NULL
        BEGIN
            PRINT N'  MISSING USER (skip): ' + @MapUsername;
            SET @Missing += 1;
        END
        ELSE IF EXISTS (
            SELECT 1 FROM system.[User] u
            CROSS APPLY OPENJSON(CASE WHEN ISJSON(u.RoleIds) = 1 THEN u.RoleIds ELSE N'[]' END) j
            WHERE u.Id = @MapUserId AND TRY_CAST(j.value AS bigint) = @MapRoleId
        )
            SET @SkippedAlready += 1;
        ELSE
        BEGIN
            UPDATE system.[User]
            SET
                RoleIds = CASE
                    WHEN RoleIds IS NULL OR LTRIM(RTRIM(RoleIds)) = N'' OR ISJSON(RoleIds) = 0 OR RoleIds = N'[]'
                        THEN N'[' + CAST(@MapRoleId AS nvarchar(20)) + N']'
                    ELSE STUFF(RoleIds, LEN(RoleIds), 1, N',' + CAST(@MapRoleId AS nvarchar(20)) + N']')
                END,
                Roles = CASE
                    WHEN ISJSON(Roles) = 1 AND EXISTS (SELECT 1 FROM OPENJSON(Roles) j WHERE j.value = @MapRoleName) THEN Roles
                    WHEN Roles IS NULL OR LTRIM(RTRIM(Roles)) = N'' OR ISJSON(Roles) = 0 OR Roles = N'[]'
                        THEN N'["' + REPLACE(@MapRoleName, N'"', N'') + N'"]'
                    ELSE STUFF(Roles, LEN(Roles), 1, N',"' + REPLACE(@MapRoleName, N'"', N'') + N'"]')
                END,
                ModifiedById = 1,
                ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now,
                ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Id = @MapUserId;
            SET @Assigned += 1;
        END

        SET @MapUserId = NULL;
        FETCH NEXT FROM map_cur INTO @MapUsername, @MapRoleName, @MapRoleId;
    END
    CLOSE map_cur;
    DEALLOCATE map_cur;

    PRINT N'  Assigned: ' + CAST(@Assigned AS nvarchar(20));
    PRINT N'  Already had role: ' + CAST(@SkippedAlready AS nvarchar(20));
    PRINT N'  Missing users: ' + CAST(@Missing AS nvarchar(20));

    -----------------------------------------------------------------------------
    PRINT N'=== [6] Supply menu AccessRoleIds ===';
    -----------------------------------------------------------------------------
    DECLARE @MenuId BIGINT = (SELECT TOP 1 Id FROM system.SystemMenu WHERE Name = N'Supply');
    IF @MenuId IS NOT NULL
    BEGIN
        IF OBJECT_ID('tempdb..#MenuRoleIds') IS NOT NULL DROP TABLE #MenuRoleIds;
        CREATE TABLE #MenuRoleIds (RoleId BIGINT NOT NULL PRIMARY KEY);

        INSERT INTO #MenuRoleIds (RoleId)
        SELECT DISTINCT TRY_CAST(j.[value] AS bigint)
        FROM system.SystemMenu m
        CROSS APPLY OPENJSON(CASE WHEN ISJSON(m.AccessRoleIds) = 1 THEN m.AccessRoleIds ELSE N'[]' END) j
        WHERE m.Id = @MenuId AND TRY_CAST(j.[value] AS bigint) IS NOT NULL;

        INSERT INTO #MenuRoleIds (RoleId)
        SELECT pr.RoleId FROM #ProfileRole pr
        WHERE pr.RoleId <> 200052
          AND NOT EXISTS (SELECT 1 FROM #MenuRoleIds x WHERE x.RoleId = pr.RoleId);

        DECLARE @AccessRoleIds NVARCHAR(MAX);
        DECLARE @AccessRoles NVARCHAR(MAX);
        SELECT @AccessRoleIds = N'[' + ISNULL(STUFF((
            SELECT N',' + CAST(RoleId AS nvarchar(20))
            FROM #MenuRoleIds ORDER BY RoleId
            FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N''), N'') + N']';
        SELECT @AccessRoles = N'[' + ISNULL(STUFF((
            SELECT N',"' + REPLACE(r.Name, N'"', N'') + N'"'
            FROM #MenuRoleIds m
            INNER JOIN system.Role r ON r.Id = m.RoleId
            ORDER BY r.Id
            FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N''), N'') + N']';

        UPDATE system.SystemMenu
        SET AccessRoleIds = @AccessRoleIds,
            AccessRoles = @AccessRoles,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @MenuId;
    END

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_InquiryPartPrice_DataProfiles ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@Err, 16, 1);
END CATCH;

SELECT q.Id, q.Name, q.Title, q.Mode,
       CASE WHEN q.QueryJson LIKE N'%<> 1675%' THEN 1 ELSE 0 END AS ExcludesAfterSales
FROM system.SavedQuery q
WHERE q.Name IN (N'inquirypartprice_listinfo', N'InquiryPartPrice_WithoutAfterSales');

SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance,
       JSON_VALUE(j.value, '$.DisplayName') AS DisplayName,
       JSON_VALUE(j.value, '$.SortDirection') AS SortDirection,
       JSON_VALUE(j.value, '$.SortOrder') AS SortOrder
FROM system.SavedQuery q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'InquiryPartPrice_WithoutAfterSales'
  AND JSON_VALUE(j.value, '$.Alliance') IN (N't1_CreatedOnShamsiDateTime', N't1_InvoiceNumber', N't1_Id');

SELECT r.Name AS RoleName, q.Title AS ProfileTitle, ra.Path
FROM system.RoleAccess ra
INNER JOIN system.Role r ON r.Id = ra.RoleId
INNER JOIN system.SavedQuery q ON q.Id = ra.RowId
WHERE ra.ActionAccessType = 3
  AND ra.EntityName = N'Entities.App.Sup.InquiryPartPrice'
ORDER BY q.Name, r.Name;
