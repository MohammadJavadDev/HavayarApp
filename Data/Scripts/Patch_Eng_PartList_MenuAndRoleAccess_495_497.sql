/*
  Patch_Eng_PartList_MenuAndRoleAccess_495_497.sql
  Live follow-up only:
    1) Update system.SystemMenu Name=eng part-list paths (AllMenus already OK)
    2) Re-apply RoleAccess for HTS pages 495-497 only (insert missing; remove stale dlbom CRUD)
  Does NOT touch AllMenus, ListDocumentProduct, Inv_PartBook, Sale PUM, TotalSystem.
  Encoding: UTF-8 with BOM. Apply: sqlcmd -C -b -I -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'patch-eng-partlist-495-497';

    -----------------------------------------------------------------------------
    PRINT N'=== [1] Update eng SystemMenu part-list entries ===';
    -----------------------------------------------------------------------------
    DECLARE @EngContent NVARCHAR(MAX) = N'[{"text":"پروپوزال","icon":"ki-archive-tick ki-outline","iconColor":"#e5ca1f","path":"","a_attr":{"href":""},"data":{"iconColor":"#e5ca1f","path":""},"children":[{"text":"پروپوزال ها","icon":"bi-list-ul bi","iconColor":"#1fe57f","path":"/panel/epms/proposal/list","a_attr":{"href":"/panel/epms/proposal/list"},"data":{"iconColor":"#1fe57f","path":"/panel/epms/proposal/list"},"children":[]},{"text":"فعالیت ها","icon":"ki-abstract-35 ki-outline","iconColor":"#d78819","path":"/panel/epms/proposalactivity/list","a_attr":{"href":"/panel/epms/proposalactivity/list"},"data":{"iconColor":"#d78819","path":"/panel/epms/proposalactivity/list"},"children":[]}]},{"text":"پروژه","icon":"ki-design-frame ki-outline","iconColor":"#e5ca1f","path":"","a_attr":{"href":""},"data":{"iconColor":"#e5ca1f","path":""},"children":[{"text":"پروژه ها","icon":"bi-list-stars bi","iconColor":"#1fe57f","path":"/panel/edms/project/list","a_attr":{"href":"/panel/edms/project/list"},"data":{"iconColor":"#1fe57f","path":"/panel/edms/project/list"},"children":[]},{"text":"فعالیت ها","icon":"ki-abstract-37 ki-outline","iconColor":"#d78819","path":"/panel/edms/projectactivity/list","a_attr":{"href":"/panel/edms/projectactivity/list"},"data":{"iconColor":"#d78819","path":"/panel/edms/projectactivity/list"},"children":[]},{"text":"Vpis","icon":"ki-laravel ki-outline","iconColor":"#1fe5c4","path":"/panel/edms/projectvpis/list","a_attr":{"href":"/panel/edms/projectvpis/list"},"data":{"iconColor":"#1fe5c4","path":"/panel/edms/projectvpis/list"},"children":[]},{"text":"ترانسمیتال","icon":"ki-delivery-3 ki-solid","iconColor":"#e55b1f","path":"/panel/edms/transmital/list","a_attr":{"href":"/panel/edms/transmital/list"},"data":{"iconColor":"#e55b1f","path":"/panel/edms/transmital/list"},"children":[]},{"text":"مدارک","icon":"bi-filetype-doc bi","iconColor":"#c019d7","path":"/panel/edms/document/list","a_attr":{"href":"/panel/edms/document/list"},"data":{"iconColor":"#c019d7","path":"/panel/edms/document/list"},"children":[]}]},{"text":"محصول","icon":"bi-box-seam bi","iconColor":"#e5ca1f","path":"","a_attr":{"href":""},"data":{"iconColor":"#e5ca1f","path":""},"children":[{"text":"فرمول","icon":"ki-add-folder ki-solid","iconColor":"#2de51f","path":"","a_attr":{"href":""},"data":{"iconColor":"#2de51f","path":""},"children":[{"text":"گروه بندی فرمول ","icon":"la-object-group la","iconColor":"#e51f64","path":"/panel/bom/productformul/list","a_attr":{"href":"/panel/bom/productformul/list"},"data":{"iconColor":"#e51f64","path":"/panel/bom/productformul/list"},"children":[]},{"text":"Bom فرمول ","icon":"ki-setting-3 ki-outline","iconColor":"#e51fd4","path":"/panel/bom/productformul/list","a_attr":{"href":"/panel/bom/productformul/list"},"data":{"iconColor":"#e51fd4","path":"/panel/bom/productformul/list"},"children":[]}]},{"text":"محصولات","icon":"ki-abstract-24 ki-outline","iconColor":"#d78819","path":"","a_attr":{"href":""},"data":{"iconColor":"#d78819","path":""},"children":[{"text":"گروه بندی محصولات","icon":"la-object-group la","iconColor":"#e51f64","path":"/panel/bom/productgroup/list","a_attr":{"href":"/panel/bom/productgroup/list"},"data":{"iconColor":"#e51f64","path":"/panel/bom/productgroup/list"},"children":[]},{"text":"Bom محصولات","icon":"ki-setting-3 ki-outline","iconColor":"#e51fd4","path":"/panel/bom/productformul/list","a_attr":{"href":"/panel/bom/productformul/list"},"data":{"iconColor":"#e51fd4","path":"/panel/bom/productformul/list"},"children":[]}]},{"text":"پارت لیست","icon":"ki-abstract-26 ki-outline","iconColor":"#c019d7","path":"","a_attr":{"href":""},"data":{"iconColor":"#c019d7","path":""},"children":[{"text":"پارت لیست","icon":"ki-abstract-12 ki-outline","iconColor":"#19d726","path":"/panel/eng/partlistproduct/list","a_attr":{"href":"/panel/eng/partlistproduct/list"},"data":{"iconColor":"#19d726","path":"/panel/eng/partlistproduct/list"},"children":[]},{"text":"پارت لیست مصرفی","icon":"ki-abstract-13 ki-outline","iconColor":"#199ed7","path":"/panel/eng/partlistdlbom/list","a_attr":{"href":"/panel/eng/partlistdlbom/list"},"data":{"iconColor":"#199ed7","path":"/panel/eng/partlistdlbom/list"},"children":[]}]},{"text":"شناسنامه","icon":"ki-scroll ki-outline","iconColor":"#1fcee5","path":"/panel/inv/partextrainfo/list","a_attr":{"href":"/panel/inv/partextrainfo/list"},"data":{"iconColor":"#1fcee5","path":"/panel/inv/partextrainfo/list"},"children":[]},{"text":"کالا ","icon":"ki-gear ki-outline","iconColor":"#64e51f","path":"/panel/inv/part/list","a_attr":{"href":"/panel/inv/part/list"},"data":{"iconColor":"#64e51f","path":"/panel/inv/part/list"},"children":[]},{"text":"درخواست ایجاد تغییر اقلام","icon":"la-exchange-alt la","iconColor":"#e5ca1f","path":"/panel/bom/formulchangerequest/list","a_attr":{"href":"/panel/bom/formulchangerequest/list"},"data":{"iconColor":"#e5ca1f","path":"/panel/bom/formulchangerequest/list"},"children":[]},{"text":"کدینگ کالا","icon":"la-stream la","iconColor":"#c333d7","path":"/panel/inv/partcodingcategory/index","a_attr":{"href":"/panel/inv/partcodingcategory/index"},"data":{"iconColor":"#c333d7","path":"/panel/inv/partcodingcategory/index"},"children":[]},{"text":"لیست درخواست کدینگ","icon":"ki-trello ki-outline","iconColor":"#eed84f","path":"/panel/inv/partcodingcategory/listrequest","a_attr":{"href":"/panel/inv/partcodingcategory/listrequest"},"data":{"iconColor":"#eed84f","path":"/panel/inv/partcodingcategory/listrequest"},"children":[]}]},{"text":"درخواست های باز","icon":"ki-book-open ki-outline","iconColor":"#e5ca1f","path":"/panel/sup/openorderrequest/list","a_attr":{"href":"/panel/sup/openorderrequest/list"},"data":{"iconColor":"#e5ca1f","path":"/panel/sup/openorderrequest/list"},"children":[]},{"text":"گزارشات","icon":"ki-chart-pie-simple ki-outline","iconColor":"#e5ca1f","path":"","a_attr":{"href":""},"data":{"iconColor":"#e5ca1f","path":""},"children":[{"text":"MDR","icon":"la-calendar-alt la","iconColor":"#2de51f","path":"/panel/edms/document/mdrreport","a_attr":{"href":"/panel/edms/document/mdrreport"},"data":{"iconColor":"#2de51f","path":"/panel/edms/document/mdrreport"},"children":[]},{"text":"حجم کاری پرسنل","icon":"ki-tablet-ok ki-outline","iconColor":"#d78819","path":"/panel/edms/document/personnelworkloadreport","a_attr":{"href":"/panel/edms/document/personnelworkloadreport"},"data":{"iconColor":"#d78819","path":"/panel/edms/document/personnelworkloadreport"},"children":[]}]},{"text":"سفارش ساخت","icon":"ki-cube-3 ki-outline","iconColor":"#e5cd86","path":"/panel/productionorder/list","a_attr":{"href":"/panel/productionorder/list"},"data":{"iconColor":"#e5cd86","path":"/panel/productionorder/list"},"children":[]},{"text":"قلم سفارش ساخت","icon":"ki-technology-4 ki-outline","iconColor":"#e5cd86","path":"/panel/productionorderitem/list","a_attr":{"href":"/panel/productionorderitem/list"},"data":{"iconColor":"#e5cd86","path":"/panel/productionorderitem/list"},"children":[]}]';

    IF ISJSON(@EngContent) <> 1
        THROW 51201, N'Patched eng menu JSON is invalid', 1;

    UPDATE system.SystemMenu
    SET Content = @EngContent,
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Name = N'eng';

    PRINT N'  eng menu rows updated: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    -----------------------------------------------------------------------------
    PRINT N'=== [2] Remove stale PartListDlBom new/add/delete/update RoleAccess ===';
    -----------------------------------------------------------------------------
    DELETE FROM system.RoleAccess
    WHERE Path IN (
        N'/panel/eng/partlistdlbom/new',
        N'/panel/eng/partlistdlbom/add',
        N'/panel/eng/partlistdlbom/delete',
        N'/panel/eng/partlistdlbom/update'
    );
    PRINT N'  Deleted stale dlbom CRUD RoleAccess: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    -----------------------------------------------------------------------------
    PRINT N'=== [3] Re-apply RoleAccess for pages 495-497 (HTS eng roles) ===';
    -----------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#Reuse') IS NOT NULL DROP TABLE #Reuse;
    CREATE TABLE #Reuse
    (
        HtsGroupId     INT            NOT NULL PRIMARY KEY,
        RoleId         BIGINT         NOT NULL,
        RoleName       NVARCHAR(200)  NOT NULL
    );
    INSERT INTO #Reuse (HtsGroupId, RoleId, RoleName) VALUES
        (440, 100019, N'پارت لیست مصرفی');

    IF OBJECT_ID('tempdb..#HtsGroup') IS NOT NULL DROP TABLE #HtsGroup;
    CREATE TABLE #HtsGroup
    (
        HtsGroupId INT            NOT NULL PRIMARY KEY,
        RoleId     BIGINT         NULL,
        RoleName   NVARCHAR(200)  NULL
    );

    INSERT INTO #HtsGroup (HtsGroupId, RoleId, RoleName)
    SELECT g.UserGroup_ID,
           COALESCE(ru.RoleId, r.Id),
           COALESCE(ru.RoleName, r.Name)
    FROM (
        SELECT DISTINCT UserGroup_ID
        FROM [TMS].[TotalSystem].[dbo].[Vw_Permission]
        WHERE System_ID = 26 AND IsActive = 1 AND Page_ID IN (495, 496, 497)
          AND UserGroup_ID IS NOT NULL
    ) g
    LEFT JOIN #Reuse ru ON ru.HtsGroupId = g.UserGroup_ID
    LEFT JOIN system.Role r ON r.Id = 260000 + g.UserGroup_ID
    WHERE COALESCE(ru.RoleId, r.Id) IS NOT NULL;

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
        -- 495
        (495, N'/panel/eng/partlistproduct/list', 1, 1, N'Entities.App.Eng.PartListProductSection', N'read'),
        (495, N'/panel/eng/partlistproduct/edit', 1, 5, N'Entities.App.Eng.PartListProductSection', N'read'),
        (495, N'/panel/eng/partlistproduct/manage', 1, 5, N'Entities.App.Eng.PartListProductSection', N'read'),
        (495, N'/panel/eng/partlistproduct/fetchdata', 2, 2, N'Entities.App.Eng.PartListProductSection', N'read'),
        (495, N'/panel/eng/partlistproduct/exporttoexcel', 2, 0, N'Entities.App.Eng.PartListProductSection', N'export'),
        (495, N'/panel/eng/partlistproduct/new', 1, 4, N'Entities.App.Eng.PartListProductSection', N'write'),
        (495, N'/panel/eng/partlistproduct/savesection', 2, 3, N'Entities.App.Eng.PartListProductSection', N'write'),
        (495, N'/panel/eng/partlistproduct/reordersection', 2, 5, N'Entities.App.Eng.PartListProductSection', N'write'),
        (495, N'/panel/eng/partlistproduct/deletesection', 2, 6, N'Entities.App.Eng.PartListProductSection', N'delete'),
        (495, N'/panel/eng/partlistproduct/savebom', 2, 3, N'Entities.App.Eng.PartListProductBom', N'write'),
        (495, N'/panel/eng/partlistproduct/deletebom', 2, 6, N'Entities.App.Eng.PartListProductBom', N'delete'),
        (495, N'/panel/eng/partlistproduct/setsectionpicture', 2, 5, N'Entities.App.Eng.PartListProductSection', N'write'),
        -- 496
        (496, N'/panel/eng/partlistproduct/list', 1, 1, N'Entities.App.Eng.PartListProductSection', N'read'),
        (496, N'/panel/eng/partlistproduct/edit', 1, 5, N'Entities.App.Eng.PartListProductSection', N'read'),
        (496, N'/panel/eng/partlistproduct/manage', 1, 5, N'Entities.App.Eng.PartListProductSection', N'read'),
        (496, N'/panel/eng/partlistproduct/fetchdata', 2, 2, N'Entities.App.Eng.PartListProductSection', N'read'),
        (496, N'/panel/eng/partlistproduct/exporttoexcel', 2, 0, N'Entities.App.Eng.PartListProductSection', N'export'),
        (496, N'/panel/eng/partlistproduct/new', 1, 4, N'Entities.App.Eng.PartListProductSection', N'write'),
        (496, N'/panel/eng/partlistproduct/savesection', 2, 3, N'Entities.App.Eng.PartListProductSection', N'write'),
        (496, N'/panel/eng/partlistproduct/reordersection', 2, 5, N'Entities.App.Eng.PartListProductSection', N'write'),
        (496, N'/panel/eng/partlistproduct/deletesection', 2, 6, N'Entities.App.Eng.PartListProductSection', N'delete'),
        (496, N'/panel/eng/partlistproduct/savebom', 2, 3, N'Entities.App.Eng.PartListProductBom', N'write'),
        (496, N'/panel/eng/partlistproduct/deletebom', 2, 6, N'Entities.App.Eng.PartListProductBom', N'delete'),
        (496, N'/panel/eng/partlistproduct/setsectionpicture', 2, 5, N'Entities.App.Eng.PartListProductSection', N'write'),
        -- 497 (no new/add/delete/update)
        (497, N'/panel/eng/partlistdlbom/list', 1, 1, N'Entities.App.Eng.PartListDlBom', N'read'),
        (497, N'/panel/eng/partlistdlbom/edit', 1, 5, N'Entities.App.Eng.PartListDlBom', N'read'),
        (497, N'/panel/eng/partlistdlbom/fetchdata', 2, 2, N'Entities.App.Eng.PartListDlBom', N'read'),
        (497, N'/panel/eng/partlistdlbom/exporttoexcel', 2, 0, N'Entities.App.Eng.PartListDlBom', N'export'),
        (497, N'/panel/eng/partlistdlbom/save', 2, 3, N'Entities.App.Eng.PartListDlBom', N'write'),
        (497, N'/panel/eng/partlistdlbom/renew', 2, 1000, N'Entities.App.Eng.PartListDlBom', N'write'),
        (497, N'/panel/eng/partlistdlbom/print', 1, 0, N'Entities.App.Eng.PartListDlBom', N'read');

    IF OBJECT_ID('tempdb..#Grant') IS NOT NULL DROP TABLE #Grant;
    CREATE TABLE #Grant
    (
        HtsGroupId   INT NOT NULL,
        PageId       INT NOT NULL,
        PermissionId INT NOT NULL
    );
    INSERT INTO #Grant (HtsGroupId, PageId, PermissionId)
    SELECT DISTINCT UserGroup_ID, Page_ID, Permission_ID
    FROM [TMS].[TotalSystem].[dbo].[Vw_Permission]
    WHERE System_ID = 26 AND IsActive = 1 AND Page_ID IN (495, 496, 497)
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
    WHERE h.RoleId IS NOT NULL
      AND (
            (p.Flag = N'read'   AND g.PermissionId IN (2, 3, 7))
         OR (p.Flag = N'export' AND g.PermissionId IN (2, 3, 7, 10, 62))
         OR (p.Flag = N'write'  AND g.PermissionId IN (2, 4, 5))
         OR (p.Flag = N'delete' AND g.PermissionId IN (2, 6))
      )
      AND NOT EXISTS (
          SELECT 1 FROM system.RoleAccess x
          WHERE x.RoleId = h.RoleId AND x.Path = p.Path AND x.ActionAccessType = p.ActionAccessType
      );
    PRINT N'  Inserted RoleAccess from HTS 495-497: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    -- Also ensure ShowAllMenus / مشاهده کلی HTS that already have dlbom list get fetch/export
    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT
        p.Path, p.ActionAccessType, p.ActionAccessItemType, p.EntityName, NULL, NULL, NULL, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    CROSS JOIN (
        SELECT N'/panel/eng/partlistdlbom/fetchdata' AS Path, 2 AS ActionAccessType, 2 AS ActionAccessItemType, N'Entities.App.Eng.PartListDlBom' AS EntityName
        UNION ALL
        SELECT N'/panel/eng/partlistdlbom/exporttoexcel', 2, 0, N'Entities.App.Eng.PartListDlBom'
    ) p
    WHERE EXISTS (
        SELECT 1 FROM system.RoleAccess ra
        WHERE ra.RoleId = r.Id AND ra.Path = N'/panel/eng/partlistdlbom/list'
    )
    AND NOT EXISTS (
        SELECT 1 FROM system.RoleAccess x
        WHERE x.RoleId = r.Id AND x.Path = p.Path AND x.ActionAccessType = p.ActionAccessType
    );
    PRINT N'  Inserted fetch/export for roles with dlbom list: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Patch_Eng_PartList_MenuAndRoleAccess_495_497 ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;
