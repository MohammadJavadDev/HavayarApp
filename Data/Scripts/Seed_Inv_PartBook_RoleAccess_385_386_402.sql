/*
  Seed_Inv_PartBook_RoleAccess_385_386_402.sql
  HTS pages 385 (product links), 386 (sections), 402 (custom BOM) → Havayar paths
  UTF-8 with BOM. Apply: sqlcmd -C -b -I -f 65001. Requires [TMS].
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-inv-partbook-385-402';

    PRINT N'=== [1] Ensure roles ===';
    IF OBJECT_ID('tempdb..#Roles') IS NOT NULL DROP TABLE #Roles;
    CREATE TABLE #Roles (RoleId BIGINT NOT NULL PRIMARY KEY, Name NVARCHAR(200) NOT NULL, Title NVARCHAR(200) NOT NULL);
    INSERT INTO #Roles (RoleId, Name, Title) VALUES
        (260239, N'کارشناسان بخش بندی کاتالوگ قطعات', N'کارشناسان بخش بندی کاتالوگ قطعات'),
        (260240, N'کارشناسان قطعه - بخش بندی کاتالوگ', N'کارشناسان قطعه - بخش بندی کاتالوگ'),
        (260259, N'کارشناسان BOM سفارشی دفترچه قطعات', N'کارشناسان BOM سفارشی دفترچه قطعات'),
        (260350, N'مشاهده کلی HTS', N'مشاهده کلی HTS');

    UPDATE r SET Name = COALESCE(g.UserGroup_Title, r.Name), Title = COALESCE(g.UserGroup_Title, r.Title)
    FROM #Roles r
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_UserGroup] g ON g.UserGroup_ID = CAST(r.RoleId - 260000 AS INT);

    DECLARE @Rid BIGINT, @RName NVARCHAR(200), @RTitle NVARCHAR(200), @UseName NVARCHAR(200);
    DECLARE rc CURSOR LOCAL FAST_FORWARD FOR SELECT RoleId, Name, Title FROM #Roles;
    OPEN rc; FETCH NEXT FROM rc INTO @Rid, @RName, @RTitle;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF EXISTS (SELECT 1 FROM system.Role WHERE Id = @Rid)
            PRINT N'  EXISTS ' + CAST(@Rid AS nvarchar(20));
        ELSE
        BEGIN
            SET @UseName = @RName;
            IF EXISTS (SELECT 1 FROM system.Role WHERE Name = @UseName)
                SET @UseName = @RName + N' (' + CAST(@Rid AS nvarchar(20)) + N')';
            SET IDENTITY_INSERT system.Role ON;
            INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
                CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
            VALUES (@Rid, @UseName, @RTitle, 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
            SET IDENTITY_INSERT system.Role OFF;
            PRINT N'  CREATED ' + @UseName;
        END
        FETCH NEXT FROM rc INTO @Rid, @RName, @RTitle;
    END
    CLOSE rc; DEALLOCATE rc;

    PRINT N'=== [2] Page paths ===';
    IF OBJECT_ID('tempdb..#PagePath') IS NOT NULL DROP TABLE #PagePath;
    CREATE TABLE #PagePath
    (
        PageId               INT            NOT NULL,
        Path                 NVARCHAR(300)  NOT NULL,
        ActionAccessType     INT            NOT NULL,
        ActionAccessItemType INT            NOT NULL,
        EntityName           NVARCHAR(200)  NULL,
        Flag                 NVARCHAR(20)   NOT NULL
    );
    INSERT INTO #PagePath (PageId, Path, ActionAccessType, ActionAccessItemType, EntityName, Flag) VALUES
        -- 385 product book
        (385, N'/panel/inv/partbook/list', 1, 1, N'Entities.App.Inv.PartPartBookSection', N'read'),
        (385, N'/panel/inv/partbook/edit', 1, 5, N'Entities.App.Inv.PartPartBookSection', N'read'),
        (385, N'/panel/inv/partbook/manage', 1, 5, N'Entities.App.Inv.PartPartBookSection', N'read'),
        (385, N'/panel/inv/partbook/fetchdata', 2, 2, N'Entities.App.Inv.PartPartBookSection', N'read'),
        (385, N'/panel/inv/partbook/exporttoexcel', 2, 0, N'Entities.App.Inv.PartPartBookSection', N'export'),
        (385, N'/panel/inv/partbook/print', 1, 0, N'Entities.App.Inv.PartPartBookSection', N'read'),
        (385, N'/panel/inv/partbook/new', 1, 4, N'Entities.App.Inv.PartPartBookSection', N'write'),
        (385, N'/panel/inv/partbook/assignsection', 2, 3, N'Entities.App.Inv.PartPartBookSection', N'write'),
        (385, N'/panel/inv/partbook/removesection', 2, 6, N'Entities.App.Inv.PartPartBookSection', N'delete'),
        -- 386 shared sections (edit/reorder/picture on same UI)
        (386, N'/panel/inv/partbook/list', 1, 1, N'Entities.App.Inv.PartBookSection', N'read'),
        (386, N'/panel/inv/partbook/manage', 1, 5, N'Entities.App.Inv.PartBookSection', N'read'),
        (386, N'/panel/inv/partbook/savesection', 2, 3, N'Entities.App.Inv.PartBookSection', N'write'),
        (386, N'/panel/inv/partbook/reordersection', 2, 5, N'Entities.App.Inv.PartBookSection', N'write'),
        (386, N'/panel/inv/partbook/setsectionpicture', 2, 5, N'Entities.App.Inv.PartBookSection', N'write'),
        (386, N'/panel/inv/partbook/print', 1, 0, N'Entities.App.Inv.PartBookSection', N'read'),
        -- 402 custom BOM
        (402, N'/panel/inv/partcustombom/list', 1, 1, N'Entities.App.Inv.PartCustomBom', N'read'),
        (402, N'/panel/inv/partcustombom/edit', 1, 5, N'Entities.App.Inv.PartCustomBom', N'read'),
        (402, N'/panel/inv/partcustombom/new', 1, 4, N'Entities.App.Inv.PartCustomBom', N'write'),
        (402, N'/panel/inv/partcustombom/fetchdata', 2, 2, N'Entities.App.Inv.PartCustomBom', N'read'),
        (402, N'/panel/inv/partcustombom/exporttoexcel', 2, 0, N'Entities.App.Inv.PartCustomBom', N'export'),
        (402, N'/panel/inv/partcustombom/save', 2, 3, N'Entities.App.Inv.PartCustomBom', N'write'),
        (402, N'/panel/inv/partcustombom/delete', 2, 6, N'Entities.App.Inv.PartCustomBom', N'delete');

    IF OBJECT_ID('tempdb..#HtsGroup') IS NOT NULL DROP TABLE #HtsGroup;
    CREATE TABLE #HtsGroup (HtsGroupId INT NOT NULL PRIMARY KEY, RoleId BIGINT NOT NULL);
    INSERT INTO #HtsGroup (HtsGroupId, RoleId)
    SELECT DISTINCT UserGroup_ID, 260000 + UserGroup_ID
    FROM [TMS].[TotalSystem].[dbo].[Vw_Permission]
    WHERE System_ID = 5 AND IsActive = 1 AND Page_ID IN (385, 386, 402)
      AND UserGroup_ID IS NOT NULL;

    IF OBJECT_ID('tempdb..#Grant') IS NOT NULL DROP TABLE #Grant;
    CREATE TABLE #Grant (HtsGroupId INT NOT NULL, PageId INT NOT NULL, PermissionId INT NOT NULL);
    INSERT INTO #Grant (HtsGroupId, PageId, PermissionId)
    SELECT DISTINCT UserGroup_ID, Page_ID, Permission_ID
    FROM [TMS].[TotalSystem].[dbo].[Vw_Permission]
    WHERE System_ID = 5 AND IsActive = 1 AND Page_ID IN (385, 386, 402)
      AND UserGroup_ID IS NOT NULL;

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT p.Path, p.ActionAccessType, p.ActionAccessItemType, p.EntityName, NULL, NULL, NULL, h.RoleId,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #Grant g
    INNER JOIN #HtsGroup h ON h.HtsGroupId = g.HtsGroupId
    INNER JOIN #PagePath p ON p.PageId = g.PageId
    WHERE (
            (p.Flag = N'read'   AND g.PermissionId IN (2, 3, 7))
         OR (p.Flag = N'export' AND g.PermissionId IN (2, 3, 7, 10, 62))
         OR (p.Flag = N'write'  AND g.PermissionId IN (2, 4, 5))
         OR (p.Flag = N'delete' AND g.PermissionId IN (2, 6))
      )
      AND NOT EXISTS (
          SELECT 1 FROM system.RoleAccess x
          WHERE x.RoleId = h.RoleId AND x.Path = p.Path AND x.ActionAccessType = p.ActionAccessType
      );
    PRINT N'Inserted from HTS 385/386/402: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    -- ShowAllMenus: full access
    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT p.Path, p.ActionAccessType, p.ActionAccessItemType, p.EntityName, NULL, NULL, NULL, 200052,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #PagePath p
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess x
        WHERE x.RoleId = 200052 AND x.Path = p.Path AND x.ActionAccessType = p.ActionAccessType
    );
    PRINT N'ShowAllMenus grants: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_Inv_PartBook_RoleAccess_385_386_402 ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;
