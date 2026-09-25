/*
  Seed_Engineering_HtsGroups_RolesAndUsers.sql
  سینک دسترسی سیستم مهندسی HTS (System_ID=26) با نقش‌های هاویار:
    - یک نقش به‌ازای هر گروه کاربری HTS (۱۱۷ گروه)
    - RoleAccess فقط روی کنترلر/نمایهٔ موجود هاویار
    - صفحات بدون کنترلر (کارتابل/آرشیو/DCC/تأیید) → نمایه داده فرم مدارک
    - نقش تست eng (Id=2) از همه کاربران برداشته می‌شود؛ ردیف نقش خالی می‌ماند
    - منوی system.SystemMenu تغییر نمی‌کند
    - گروه 85 DCC: نقش موجود، کاربران جدید اضافه نمی‌شوند
    - گروه 440 → پارت لیست مصرفی ؛ گروه 441 نقش جدا
    - ShowPrice فقط نمایهٔ قیمت (گروه‌های ۳۷/۴۷ و Permission 8)
    - صفحه ۲۲۳ خروجی اکسل جدا است (گروه ۳۶۳). اعطاهای مستقیم و تکوین ۴۸۲:
      Seed_Engineering_HtsLeftover_DirectAndFixes.sql
  Idempotent. Requires linked server [TMS] → TotalSystem.
  Encoding: UTF-8 with BOM. Apply: sqlcmd -C -b -I -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-eng-hts-groups';

    -----------------------------------------------------------------------------
    PRINT N'=== [1] Unassign test role eng (Id=2) and paired EngMenu (400006) ===';
    -----------------------------------------------------------------------------
    DECLARE @Uid BIGINT, @RoleIds NVARCHAR(MAX), @Roles NVARCHAR(MAX);
    DECLARE @NewRoleIds NVARCHAR(MAX), @NewRoles NVARCHAR(MAX);
    DECLARE @UnassignedEng INT = 0, @UnassignedMenu INT = 0;

    DECLARE u_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT u.Id, ISNULL(u.RoleIds, N'[]'), ISNULL(u.Roles, N'[]')
        FROM system.[User] u
        WHERE EXISTS (
            SELECT 1 FROM OPENJSON(ISNULL(u.RoleIds, N'[]')) j
            WHERE TRY_CAST(j.value AS bigint) IN (2, 400006)
        );
    OPEN u_cur;
    FETCH NEXT FROM u_cur INTO @Uid, @RoleIds, @Roles;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF ISJSON(@RoleIds) = 0 SET @RoleIds = N'[]';
        IF ISJSON(@Roles) = 0 SET @Roles = N'[]';

        IF EXISTS (SELECT 1 FROM OPENJSON(@RoleIds) j WHERE TRY_CAST(j.value AS bigint) = 2)
            SET @UnassignedEng += 1;
        IF EXISTS (SELECT 1 FROM OPENJSON(@RoleIds) j WHERE TRY_CAST(j.value AS bigint) = 400006)
            SET @UnassignedMenu += 1;

        SELECT @NewRoleIds = N'[' + ISNULL(STUFF((
            SELECT N',' + CAST(TRY_CAST(j.value AS bigint) AS nvarchar(20))
            FROM OPENJSON(@RoleIds) j
            WHERE TRY_CAST(j.value AS bigint) IS NOT NULL
              AND TRY_CAST(j.value AS bigint) NOT IN (2, 400006)
            FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N''), N'') + N']';

        SELECT @NewRoles = N'[' + ISNULL(STUFF((
            SELECT N',"' + REPLACE(j.value, N'"', N'\"') + N'"'
            FROM OPENJSON(@Roles) j
            WHERE j.value NOT IN (N'eng', N'EngMenu')
            FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N''), N'') + N']';

        UPDATE system.[User]
        SET RoleIds = @NewRoleIds,
            Roles = @NewRoles,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @Uid;

        FETCH NEXT FROM u_cur INTO @Uid, @RoleIds, @Roles;
    END
    CLOSE u_cur;
    DEALLOCATE u_cur;
    PRINT N'  Removed eng from users: ' + CAST(@UnassignedEng AS nvarchar(20));
    PRINT N'  Removed EngMenu from users: ' + CAST(@UnassignedMenu AS nvarchar(20));

    DELETE FROM system.RoleAccess WHERE RoleId = 2;
    PRINT N'  Cleared RoleAccess of test role eng: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    UPDATE system.Role
    SET IsActive = 0,
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Id = 2;
    PRINT N'  Deactivated Role Id=2 (row kept; no code FK on name eng)';

    -----------------------------------------------------------------------------
    PRINT N'=== [2] Mapping tables ===';
    -----------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#Reuse') IS NOT NULL DROP TABLE #Reuse;
    CREATE TABLE #Reuse
    (
        HtsGroupId     INT            NOT NULL PRIMARY KEY,
        RoleId         BIGINT         NOT NULL,
        RoleName       NVARCHAR(200)  NOT NULL,
        SkipUserAssign BIT            NOT NULL
    );
    INSERT INTO #Reuse (HtsGroupId, RoleId, RoleName, SkipUserAssign) VALUES
        (85, 200000, N'EdmsDocumentsDccUsers', 1),
        (141, 200007, N'Edms.Reports.MDRReport', 0),
        (212, 200008, N'Edms.Reports.PersonnelWorkLoadReport', 0),
        (440, 100019, N'پارت لیست مصرفی', 0);

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
        (98, N'/panel/bom/productformul/list', 1, 1, N'Entities.App.Bom.ProductFormul', N'read'),
        (98, N'/panel/bom/productformul/edit', 1, 5, N'Entities.App.Bom.ProductFormul', N'read'),
        (98, N'/panel/bom/productformul/fetchdata', 2, 2, N'Entities.App.Bom.ProductFormul', N'read'),
        (98, N'/panel/bom/productformul/exporttoexcel', 2, 0, N'Entities.App.Bom.ProductFormul', N'export'),
        (98, N'/panel/bom/productformul/new', 1, 4, N'Entities.App.Bom.ProductFormul', N'write'),
        (98, N'/panel/bom/productformul/add', 2, 4, N'Entities.App.Bom.ProductFormul', N'write'),
        (98, N'/panel/bom/productformul/save', 2, 3, N'Entities.App.Bom.ProductFormul', N'write'),
        (98, N'/panel/bom/productformul/update', 2, 5, N'Entities.App.Bom.ProductFormul', N'write'),
        (98, N'/panel/bom/productformul/delete', 2, 6, N'Entities.App.Bom.ProductFormul', N'delete'),
        (99, N'/panel/bom/productformul/list', 1, 1, N'Entities.App.Bom.ProductFormul', N'read'),
        (99, N'/panel/bom/productformul/edit', 1, 5, N'Entities.App.Bom.ProductFormul', N'read'),
        (99, N'/panel/bom/productformul/fetchdata', 2, 2, N'Entities.App.Bom.ProductFormul', N'read'),
        (99, N'/panel/bom/productformul/exporttoexcel', 2, 0, N'Entities.App.Bom.ProductFormul', N'export'),
        (99, N'/panel/bom/productformul/new', 1, 4, N'Entities.App.Bom.ProductFormul', N'write'),
        (99, N'/panel/bom/productformul/add', 2, 4, N'Entities.App.Bom.ProductFormul', N'write'),
        (99, N'/panel/bom/productformul/save', 2, 3, N'Entities.App.Bom.ProductFormul', N'write'),
        (99, N'/panel/bom/productformul/update', 2, 5, N'Entities.App.Bom.ProductFormul', N'write'),
        (99, N'/panel/bom/productformul/delete', 2, 6, N'Entities.App.Bom.ProductFormul', N'delete'),
        (100, N'/panel/bom/productformul/edit', 1, 5, N'Entities.App.Bom.ProductFormul', N'read'),
        (100, N'/panel/bom/productformul/list', 1, 1, N'Entities.App.Bom.ProductFormul', N'read'),
        (100, N'/panel/bom/productformul/fetchdata', 2, 2, N'Entities.App.Bom.ProductFormul', N'read'),
        (101, N'/panel/bom/productformul/list', 1, 1, N'Entities.App.Bom.ProductFormul', N'read'),
        (101, N'/panel/bom/productformul/edit', 1, 5, N'Entities.App.Bom.ProductFormul', N'read'),
        (101, N'/panel/bom/productformul/fetchdata', 2, 2, N'Entities.App.Bom.ProductFormul', N'read'),
        (101, N'/panel/bom/productformul/exporttoexcel', 2, 0, N'Entities.App.Bom.ProductFormul', N'export'),
        (101, N'/panel/bom/productformul/new', 1, 4, N'Entities.App.Bom.ProductFormul', N'write'),
        (101, N'/panel/bom/productformul/add', 2, 4, N'Entities.App.Bom.ProductFormul', N'write'),
        (101, N'/panel/bom/productformul/save', 2, 3, N'Entities.App.Bom.ProductFormul', N'write'),
        (101, N'/panel/bom/productformul/update', 2, 5, N'Entities.App.Bom.ProductFormul', N'write'),
        (101, N'/panel/bom/productformul/delete', 2, 6, N'Entities.App.Bom.ProductFormul', N'delete'),
        (103, N'/panel/bom/productgroup/list', 1, 1, N'Entities.App.Bom.ProductGroup', N'read'),
        (103, N'/panel/bom/productgroup/edit', 1, 5, N'Entities.App.Bom.ProductGroup', N'read'),
        (103, N'/panel/bom/productgroup/fetchdata', 2, 2, N'Entities.App.Bom.ProductGroup', N'read'),
        (103, N'/panel/bom/productgroup/exporttoexcel', 2, 0, N'Entities.App.Bom.ProductGroup', N'export'),
        (103, N'/panel/bom/productgroup/new', 1, 4, N'Entities.App.Bom.ProductGroup', N'write'),
        (103, N'/panel/bom/productgroup/add', 2, 4, N'Entities.App.Bom.ProductGroup', N'write'),
        (103, N'/panel/bom/productgroup/save', 2, 3, N'Entities.App.Bom.ProductGroup', N'write'),
        (103, N'/panel/bom/productgroup/update', 2, 5, N'Entities.App.Bom.ProductGroup', N'write'),
        (103, N'/panel/bom/productgroup/delete', 2, 6, N'Entities.App.Bom.ProductGroup', N'delete'),
        (106, N'/panel/inv/part/list', 1, 1, N'Entities.App.Inv.Part', N'read'),
        (106, N'/panel/inv/part/edit', 1, 5, N'Entities.App.Inv.Part', N'read'),
        (106, N'/panel/inv/part/fetchdata', 2, 2, N'Entities.App.Inv.Part', N'read'),
        (106, N'/panel/inv/part/exporttoexcel', 2, 0, N'Entities.App.Inv.Part', N'export'),
        (106, N'/panel/inv/part/new', 1, 4, N'Entities.App.Inv.Part', N'write'),
        (106, N'/panel/inv/part/add', 2, 4, N'Entities.App.Inv.Part', N'write'),
        (106, N'/panel/inv/part/save', 2, 3, N'Entities.App.Inv.Part', N'write'),
        (106, N'/panel/inv/part/update', 2, 5, N'Entities.App.Inv.Part', N'write'),
        (107, N'/panel/bom/productformul/list', 1, 1, N'Entities.App.Bom.ProductFormul', N'read'),
        (107, N'/panel/bom/productformul/edit', 1, 5, N'Entities.App.Bom.ProductFormul', N'read'),
        (107, N'/panel/bom/productformul/fetchdata', 2, 2, N'Entities.App.Bom.ProductFormul', N'read'),
        (107, N'/panel/bom/productformul/exporttoexcel', 2, 0, N'Entities.App.Bom.ProductFormul', N'export'),
        (107, N'/panel/bom/productformul/new', 1, 4, N'Entities.App.Bom.ProductFormul', N'write'),
        (107, N'/panel/bom/productformul/add', 2, 4, N'Entities.App.Bom.ProductFormul', N'write'),
        (107, N'/panel/bom/productformul/save', 2, 3, N'Entities.App.Bom.ProductFormul', N'write'),
        (107, N'/panel/bom/productformul/update', 2, 5, N'Entities.App.Bom.ProductFormul', N'write'),
        (107, N'/panel/bom/productformul/delete', 2, 6, N'Entities.App.Bom.ProductFormul', N'delete'),
        (108, N'/panel/bom/productformul/list', 1, 1, N'Entities.App.Bom.ProductFormul', N'read'),
        (108, N'/panel/bom/productformul/edit', 1, 5, N'Entities.App.Bom.ProductFormul', N'read'),
        (108, N'/panel/bom/productformul/fetchdata', 2, 2, N'Entities.App.Bom.ProductFormul', N'read'),
        (108, N'/panel/bom/productformul/exporttoexcel', 2, 0, N'Entities.App.Bom.ProductFormul', N'export'),
        (108, N'/panel/bom/productformul/new', 1, 4, N'Entities.App.Bom.ProductFormul', N'write'),
        (108, N'/panel/bom/productformul/add', 2, 4, N'Entities.App.Bom.ProductFormul', N'write'),
        (108, N'/panel/bom/productformul/save', 2, 3, N'Entities.App.Bom.ProductFormul', N'write'),
        (108, N'/panel/bom/productformul/update', 2, 5, N'Entities.App.Bom.ProductFormul', N'write'),
        (108, N'/panel/bom/productformul/delete', 2, 6, N'Entities.App.Bom.ProductFormul', N'delete'),
        (109, N'/panel/inv/partextrainfo/list', 1, 1, N'Entities.App.Inv.PartExtraInfo', N'read'),
        (109, N'/panel/inv/partextrainfo/edit', 1, 5, N'Entities.App.Inv.PartExtraInfo', N'read'),
        (109, N'/panel/inv/partextrainfo/fetchdata', 2, 2, N'Entities.App.Inv.PartExtraInfo', N'read'),
        (109, N'/panel/inv/partextrainfo/exporttoexcel', 2, 0, N'Entities.App.Inv.PartExtraInfo', N'export'),
        (109, N'/panel/inv/partextrainfo/new', 1, 4, N'Entities.App.Inv.PartExtraInfo', N'write'),
        (109, N'/panel/inv/partextrainfo/add', 2, 4, N'Entities.App.Inv.PartExtraInfo', N'write'),
        (109, N'/panel/inv/partextrainfo/save', 2, 3, N'Entities.App.Inv.PartExtraInfo', N'write'),
        (109, N'/panel/inv/partextrainfo/update', 2, 5, N'Entities.App.Inv.PartExtraInfo', N'write'),
        (109, N'/panel/inv/partextrainfo/delete', 2, 6, N'Entities.App.Inv.PartExtraInfo', N'delete'),
        (110, N'/panel/inv/partextrainfo/list', 1, 1, N'Entities.App.Inv.PartExtraInfo', N'read'),
        (110, N'/panel/inv/partextrainfo/edit', 1, 5, N'Entities.App.Inv.PartExtraInfo', N'read'),
        (110, N'/panel/inv/partextrainfo/fetchdata', 2, 2, N'Entities.App.Inv.PartExtraInfo', N'read'),
        (110, N'/panel/inv/partextrainfo/exporttoexcel', 2, 0, N'Entities.App.Inv.PartExtraInfo', N'export'),
        (116, N'/panel/bom/productpricecompare/list', 1, 1, N'Entities.App.Bom.ProductPriceCompare', N'read'),
        (116, N'/panel/bom/productpricecompare/fetchdata', 2, 2, N'Entities.App.Bom.ProductPriceCompare', N'read'),
        (116, N'/panel/bom/productpricecompare/details', 1, 1, N'Entities.App.Bom.ProductPriceCompare', N'read'),
        (116, N'/panel/bom/productpricecompare/availabledates', 2, 2, N'Entities.App.Bom.ProductPriceCompare', N'read'),
        (116, N'/panel/bom/productpricecompare/sendemail', 2, 1000, N'Entities.App.Bom.ProductPriceCompare', N'write'),
        (125, N'/panel/inv/part/list', 1, 1, N'Entities.App.Inv.Part', N'read'),
        (125, N'/panel/inv/part/edit', 1, 5, N'Entities.App.Inv.Part', N'read'),
        (125, N'/panel/inv/part/fetchdata', 2, 2, N'Entities.App.Inv.Part', N'read'),
        (125, N'/panel/inv/part/exporttoexcel', 2, 0, N'Entities.App.Inv.Part', N'export'),
        (125, N'/panel/inv/part/new', 1, 4, N'Entities.App.Inv.Part', N'write'),
        (125, N'/panel/inv/part/add', 2, 4, N'Entities.App.Inv.Part', N'write'),
        (125, N'/panel/inv/part/save', 2, 3, N'Entities.App.Inv.Part', N'write'),
        (125, N'/panel/inv/part/update', 2, 5, N'Entities.App.Inv.Part', N'write'),
        (163, N'/panel/edms/project/list', 1, 1, N'Entities.App.Edms.Project', N'read'),
        (163, N'/panel/edms/project/edit', 1, 5, N'Entities.App.Edms.Project', N'read'),
        (163, N'/panel/edms/project/fetchdata', 2, 2, N'Entities.App.Edms.Project', N'read'),
        (163, N'/panel/edms/project/exporttoexcel', 2, 0, N'Entities.App.Edms.Project', N'export'),
        (163, N'/panel/edms/project/new', 1, 4, N'Entities.App.Edms.Project', N'write'),
        (163, N'/panel/edms/project/add', 2, 4, N'Entities.App.Edms.Project', N'write'),
        (163, N'/panel/edms/project/save', 2, 3, N'Entities.App.Edms.Project', N'write'),
        (163, N'/panel/edms/project/update', 2, 5, N'Entities.App.Edms.Project', N'write'),
        (163, N'/panel/edms/project/delete', 2, 6, N'Entities.App.Edms.Project', N'delete'),
        (163, N'/panel/edms/project/projectattachmentlist', 1, 1, N'Entities.App.Edms.Project', N'read'),
        (165, N'/panel/edms/projectvpis/list', 1, 1, N'Entities.App.Edms.ProjectVpis', N'read'),
        (165, N'/panel/edms/projectvpis/edit', 1, 5, N'Entities.App.Edms.ProjectVpis', N'read'),
        (165, N'/panel/edms/projectvpis/fetchdata', 2, 2, N'Entities.App.Edms.ProjectVpis', N'read'),
        (165, N'/panel/edms/projectvpis/exporttoexcel', 2, 0, N'Entities.App.Edms.ProjectVpis', N'export'),
        (165, N'/panel/edms/projectvpis/new', 1, 4, N'Entities.App.Edms.ProjectVpis', N'write'),
        (165, N'/panel/edms/projectvpis/add', 2, 4, N'Entities.App.Edms.ProjectVpis', N'write'),
        (165, N'/panel/edms/projectvpis/save', 2, 3, N'Entities.App.Edms.ProjectVpis', N'write'),
        (165, N'/panel/edms/projectvpis/update', 2, 5, N'Entities.App.Edms.ProjectVpis', N'write'),
        (165, N'/panel/edms/projectvpis/delete', 2, 6, N'Entities.App.Edms.ProjectVpis', N'delete'),
        (166, N'/panel/edms/document/list', 1, 1, N'Entities.App.Edms.Document', N'read'),
        (166, N'/panel/edms/document/edit', 1, 5, N'Entities.App.Edms.Document', N'read'),
        (166, N'/panel/edms/document/fetchdata', 2, 2, N'Entities.App.Edms.Document', N'read'),
        (166, N'/panel/edms/document/exporttoexcel', 2, 0, N'Entities.App.Edms.Document', N'export'),
        (166, N'/panel/edms/document/new', 1, 4, N'Entities.App.Edms.Document', N'write'),
        (166, N'/panel/edms/document/add', 2, 4, N'Entities.App.Edms.Document', N'write'),
        (166, N'/panel/edms/document/save', 2, 3, N'Entities.App.Edms.Document', N'write'),
        (166, N'/panel/edms/document/update', 2, 5, N'Entities.App.Edms.Document', N'write'),
        (166, N'/panel/edms/document/delete', 2, 6, N'Entities.App.Edms.Document', N'delete'),
        (166, N'/panel/edms/document/documentproductfetchdata', 2, 2, N'Entities.App.Edms.Document', N'read'),
        (166, N'/panel/edms/document/editasgroup', 1, 5, N'Entities.App.Edms.Document', N'write'),
        (166, N'/panel/edms/document/save/{caneditdocument?}', 2, 3, N'Entities.App.Edms.Document', N'write'),
        (168, N'/panel/edms/document/edit', 1, 5, N'Entities.App.Edms.Document', N'read'),
        (168, N'/panel/edms/document/fetchdata', 2, 2, N'Entities.App.Edms.Document', N'read'),
        (169, N'/panel/edms/transmital/list', 1, 1, N'Entities.App.Edms.Transmital', N'read'),
        (169, N'/panel/edms/transmital/edit', 1, 5, N'Entities.App.Edms.Transmital', N'read'),
        (169, N'/panel/edms/transmital/fetchdata', 2, 2, N'Entities.App.Edms.Transmital', N'read'),
        (169, N'/panel/edms/transmital/exporttoexcel', 2, 0, N'Entities.App.Edms.Transmital', N'export'),
        (169, N'/panel/edms/transmital/new', 1, 4, N'Entities.App.Edms.Transmital', N'write'),
        (169, N'/panel/edms/transmital/add', 2, 4, N'Entities.App.Edms.Transmital', N'write'),
        (169, N'/panel/edms/transmital/save', 2, 3, N'Entities.App.Edms.Transmital', N'write'),
        (169, N'/panel/edms/transmital/update', 2, 5, N'Entities.App.Edms.Transmital', N'write'),
        (169, N'/panel/edms/transmital/delete', 2, 6, N'Entities.App.Edms.Transmital', N'delete'),
        (218, N'/panel/edms/project/projectattachmentlist', 1, 1, N'Entities.App.Edms.Project', N'read'),
        (218, N'/panel/edms/project/edit', 1, 5, N'Entities.App.Edms.Project', N'read'),
        (218, N'/panel/edms/project/fetchdata', 2, 2, N'Entities.App.Edms.Project', N'read'),
        (220, N'/panel/edms/transmital/edit', 1, 5, N'Entities.App.Edms.Transmital', N'read'),
        (220, N'/panel/edms/transmital/list', 1, 1, N'Entities.App.Edms.Transmital', N'read'),
        (220, N'/panel/edms/transmital/fetchdata', 2, 2, N'Entities.App.Edms.Transmital', N'read'),
        (221, N'/panel/edms/document/list', 1, 1, N'Entities.App.Edms.Document', N'read'),
        (221, N'/panel/edms/document/edit', 1, 5, N'Entities.App.Edms.Document', N'read'),
        (221, N'/panel/edms/document/fetchdata', 2, 2, N'Entities.App.Edms.Document', N'read'),
        (223, N'/panel/edms/document/list', 1, 1, N'Entities.App.Edms.Document', N'read'),
        (223, N'/panel/edms/document/edit', 1, 5, N'Entities.App.Edms.Document', N'read'),
        (223, N'/panel/edms/document/fetchdata', 2, 2, N'Entities.App.Edms.Document', N'read'),
        (223, N'/panel/edms/document/exporttoexcel', 2, 0, N'Entities.App.Edms.Document', N'export'),
        (232, N'/panel/edms/document/list', 1, 1, N'Entities.App.Edms.Document', N'read'),
        (232, N'/panel/edms/document/edit', 1, 5, N'Entities.App.Edms.Document', N'read'),
        (232, N'/panel/edms/document/fetchdata', 2, 2, N'Entities.App.Edms.Document', N'read'),
        (233, N'/panel/edms/document/list', 1, 1, N'Entities.App.Edms.Document', N'read'),
        (233, N'/panel/edms/document/edit', 1, 5, N'Entities.App.Edms.Document', N'read'),
        (233, N'/panel/edms/document/fetchdata', 2, 2, N'Entities.App.Edms.Document', N'read'),
        (235, N'/panel/edms/document/mdrreport', 1, 1, N'Entities.App.Edms.Document', N'read'),
        (235, N'/panel/edms/document/getmdrreport', 2, 2, N'Entities.App.Edms.Document', N'read'),
        (235, N'/panel/edms/document/getmdrreportpost', 2, 2, N'Entities.App.Edms.Document', N'read'),
        (235, N'/panel/edms/document/exportmdrtoexcel', 2, 0, N'Entities.App.Edms.Document', N'export'),
        (238, N'/panel/edms/document/list', 1, 1, N'Entities.App.Edms.Document', N'read'),
        (238, N'/panel/edms/document/edit', 1, 5, N'Entities.App.Edms.Document', N'read'),
        (238, N'/panel/edms/document/fetchdata', 2, 2, N'Entities.App.Edms.Document', N'read'),
        (248, N'/panel/edms/document/list', 1, 1, N'Entities.App.Edms.Document', N'read'),
        (248, N'/panel/edms/document/edit', 1, 5, N'Entities.App.Edms.Document', N'read'),
        (248, N'/panel/edms/document/fetchdata', 2, 2, N'Entities.App.Edms.Document', N'read'),
        (252, N'/panel/epms/proposal/list', 1, 1, N'Entities.App.Epms.Proposal', N'read'),
        (252, N'/panel/epms/proposal/edit', 1, 5, N'Entities.App.Epms.Proposal', N'read'),
        (252, N'/panel/epms/proposal/fetchdata', 2, 2, N'Entities.App.Epms.Proposal', N'read'),
        (252, N'/panel/epms/proposal/exporttoexcel', 2, 0, N'Entities.App.Epms.Proposal', N'export'),
        (252, N'/panel/epms/proposal/new', 1, 4, N'Entities.App.Epms.Proposal', N'write'),
        (252, N'/panel/epms/proposal/add', 2, 4, N'Entities.App.Epms.Proposal', N'write'),
        (252, N'/panel/epms/proposal/save', 2, 3, N'Entities.App.Epms.Proposal', N'write'),
        (252, N'/panel/epms/proposal/update', 2, 5, N'Entities.App.Epms.Proposal', N'write'),
        (252, N'/panel/epms/proposal/delete', 2, 6, N'Entities.App.Epms.Proposal', N'delete'),
        (253, N'/panel/epms/proposal/edit', 1, 5, N'Entities.App.Epms.Proposal', N'read'),
        (253, N'/panel/epms/proposal/list', 1, 1, N'Entities.App.Epms.Proposal', N'read'),
        (253, N'/panel/epms/proposal/fetchdata', 2, 2, N'Entities.App.Epms.Proposal', N'read'),
        (254, N'/panel/epms/proposalvpis/list', 1, 1, N'Entities.App.Epms.ProposalVpis', N'read'),
        (254, N'/panel/epms/proposalvpis/edit', 1, 5, N'Entities.App.Epms.ProposalVpis', N'read'),
        (254, N'/panel/epms/proposalvpis/fetchdata', 2, 2, N'Entities.App.Epms.ProposalVpis', N'read'),
        (254, N'/panel/epms/proposalvpis/exporttoexcel', 2, 0, N'Entities.App.Epms.ProposalVpis', N'export'),
        (254, N'/panel/epms/proposalvpis/new', 1, 4, N'Entities.App.Epms.ProposalVpis', N'write'),
        (254, N'/panel/epms/proposalvpis/add', 2, 4, N'Entities.App.Epms.ProposalVpis', N'write'),
        (254, N'/panel/epms/proposalvpis/save', 2, 3, N'Entities.App.Epms.ProposalVpis', N'write'),
        (254, N'/panel/epms/proposalvpis/update', 2, 5, N'Entities.App.Epms.ProposalVpis', N'write'),
        (254, N'/panel/epms/proposalvpis/delete', 2, 6, N'Entities.App.Epms.ProposalVpis', N'delete'),
        (255, N'/panel/edms/document/list', 1, 1, N'Entities.App.Edms.Document', N'read'),
        (255, N'/panel/edms/document/edit', 1, 5, N'Entities.App.Edms.Document', N'read'),
        (255, N'/panel/edms/document/fetchdata', 2, 2, N'Entities.App.Edms.Document', N'read'),
        (256, N'/panel/edms/document/edit', 1, 5, N'Entities.App.Edms.Document', N'read'),
        (256, N'/panel/edms/document/fetchdata', 2, 2, N'Entities.App.Edms.Document', N'read'),
        (257, N'/panel/edms/document/list', 1, 1, N'Entities.App.Edms.Document', N'read'),
        (257, N'/panel/edms/document/edit', 1, 5, N'Entities.App.Edms.Document', N'read'),
        (257, N'/panel/edms/document/fetchdata', 2, 2, N'Entities.App.Edms.Document', N'read'),
        (260, N'/panel/edms/document/list', 1, 1, N'Entities.App.Edms.Document', N'read'),
        (260, N'/panel/edms/document/edit', 1, 5, N'Entities.App.Edms.Document', N'read'),
        (260, N'/panel/edms/document/fetchdata', 2, 2, N'Entities.App.Edms.Document', N'read'),
        (261, N'/panel/epms/equipmentpriceestimate/list', 1, 1, N'Entities.App.Epms.EquipmentPriceEstimate', N'read'),
        (261, N'/panel/epms/equipmentpriceestimate/edit', 1, 5, N'Entities.App.Epms.EquipmentPriceEstimate', N'read'),
        (261, N'/panel/epms/equipmentpriceestimate/fetchdata', 2, 2, N'Entities.App.Epms.EquipmentPriceEstimate', N'read'),
        (261, N'/panel/epms/equipmentpriceestimate/exporttoexcel', 2, 0, N'Entities.App.Epms.EquipmentPriceEstimate', N'export'),
        (261, N'/panel/epms/equipmentpriceestimate/new', 1, 4, N'Entities.App.Epms.EquipmentPriceEstimate', N'write'),
        (261, N'/panel/epms/equipmentpriceestimate/add', 2, 4, N'Entities.App.Epms.EquipmentPriceEstimate', N'write'),
        (261, N'/panel/epms/equipmentpriceestimate/save', 2, 3, N'Entities.App.Epms.EquipmentPriceEstimate', N'write'),
        (261, N'/panel/epms/equipmentpriceestimate/update', 2, 5, N'Entities.App.Epms.EquipmentPriceEstimate', N'write'),
        (261, N'/panel/epms/equipmentpriceestimate/delete', 2, 6, N'Entities.App.Epms.EquipmentPriceEstimate', N'delete'),
        (264, N'/panel/edms/document/list', 1, 1, N'Entities.App.Edms.Document', N'read'),
        (264, N'/panel/edms/document/edit', 1, 5, N'Entities.App.Edms.Document', N'read'),
        (264, N'/panel/edms/document/fetchdata', 2, 2, N'Entities.App.Edms.Document', N'read'),
        (267, N'/panel/epms/equipmentpriceestimate/list', 1, 1, N'Entities.App.Epms.EquipmentPriceEstimate', N'read'),
        (267, N'/panel/epms/equipmentpriceestimate/edit', 1, 5, N'Entities.App.Epms.EquipmentPriceEstimate', N'read'),
        (267, N'/panel/epms/equipmentpriceestimate/fetchdata', 2, 2, N'Entities.App.Epms.EquipmentPriceEstimate', N'read'),
        (267, N'/panel/epms/equipmentpriceestimate/exporttoexcel', 2, 0, N'Entities.App.Epms.EquipmentPriceEstimate', N'export'),
        (267, N'/panel/epms/equipmentpriceestimate/new', 1, 4, N'Entities.App.Epms.EquipmentPriceEstimate', N'write'),
        (267, N'/panel/epms/equipmentpriceestimate/add', 2, 4, N'Entities.App.Epms.EquipmentPriceEstimate', N'write'),
        (267, N'/panel/epms/equipmentpriceestimate/save', 2, 3, N'Entities.App.Epms.EquipmentPriceEstimate', N'write'),
        (267, N'/panel/epms/equipmentpriceestimate/update', 2, 5, N'Entities.App.Epms.EquipmentPriceEstimate', N'write'),
        (267, N'/panel/epms/equipmentpriceestimate/delete', 2, 6, N'Entities.App.Epms.EquipmentPriceEstimate', N'delete'),
        (272, N'/panel/epms/equipmentpriceestimate/list', 1, 1, N'Entities.App.Epms.EquipmentPriceEstimate', N'read'),
        (272, N'/panel/epms/equipmentpriceestimate/edit', 1, 5, N'Entities.App.Epms.EquipmentPriceEstimate', N'read'),
        (272, N'/panel/epms/equipmentpriceestimate/fetchdata', 2, 2, N'Entities.App.Epms.EquipmentPriceEstimate', N'read'),
        (272, N'/panel/epms/equipmentpriceestimate/exporttoexcel', 2, 0, N'Entities.App.Epms.EquipmentPriceEstimate', N'export'),
        (272, N'/panel/epms/equipmentpriceestimate/new', 1, 4, N'Entities.App.Epms.EquipmentPriceEstimate', N'write'),
        (272, N'/panel/epms/equipmentpriceestimate/add', 2, 4, N'Entities.App.Epms.EquipmentPriceEstimate', N'write'),
        (272, N'/panel/epms/equipmentpriceestimate/save', 2, 3, N'Entities.App.Epms.EquipmentPriceEstimate', N'write'),
        (272, N'/panel/epms/equipmentpriceestimate/update', 2, 5, N'Entities.App.Epms.EquipmentPriceEstimate', N'write'),
        (272, N'/panel/epms/equipmentpriceestimate/delete', 2, 6, N'Entities.App.Epms.EquipmentPriceEstimate', N'delete'),
        (273, N'/panel/epms/equipmentpriceestimate/list', 1, 1, N'Entities.App.Epms.EquipmentPriceEstimate', N'read'),
        (273, N'/panel/epms/equipmentpriceestimate/edit', 1, 5, N'Entities.App.Epms.EquipmentPriceEstimate', N'read'),
        (273, N'/panel/epms/equipmentpriceestimate/fetchdata', 2, 2, N'Entities.App.Epms.EquipmentPriceEstimate', N'read'),
        (273, N'/panel/epms/equipmentpriceestimate/exporttoexcel', 2, 0, N'Entities.App.Epms.EquipmentPriceEstimate', N'export'),
        (273, N'/panel/epms/equipmentpriceestimate/new', 1, 4, N'Entities.App.Epms.EquipmentPriceEstimate', N'write'),
        (273, N'/panel/epms/equipmentpriceestimate/add', 2, 4, N'Entities.App.Epms.EquipmentPriceEstimate', N'write'),
        (273, N'/panel/epms/equipmentpriceestimate/save', 2, 3, N'Entities.App.Epms.EquipmentPriceEstimate', N'write'),
        (273, N'/panel/epms/equipmentpriceestimate/update', 2, 5, N'Entities.App.Epms.EquipmentPriceEstimate', N'write'),
        (273, N'/panel/epms/equipmentpriceestimate/delete', 2, 6, N'Entities.App.Epms.EquipmentPriceEstimate', N'delete'),
        (275, N'/panel/edms/document/list', 1, 1, N'Entities.App.Edms.Document', N'read'),
        (275, N'/panel/edms/document/edit', 1, 5, N'Entities.App.Edms.Document', N'read'),
        (275, N'/panel/edms/document/fetchdata', 2, 2, N'Entities.App.Edms.Document', N'read'),
        (279, N'/panel/edms/project/projectattachmentlist', 1, 1, N'Entities.App.Edms.Project', N'read'),
        (279, N'/panel/edms/project/edit', 1, 5, N'Entities.App.Edms.Project', N'read'),
        (279, N'/panel/edms/project/fetchdata', 2, 2, N'Entities.App.Edms.Project', N'read'),
        (288, N'/panel/edms/projectactivity/list', 1, 1, N'Entities.App.Edms.ProjectActivity', N'read'),
        (288, N'/panel/edms/projectactivity/edit', 1, 5, N'Entities.App.Edms.ProjectActivity', N'read'),
        (288, N'/panel/edms/projectactivity/fetchdata', 2, 2, N'Entities.App.Edms.ProjectActivity', N'read'),
        (288, N'/panel/edms/projectactivity/exporttoexcel', 2, 0, N'Entities.App.Edms.ProjectActivity', N'export'),
        (288, N'/panel/edms/projectactivity/new', 1, 4, N'Entities.App.Edms.ProjectActivity', N'write'),
        (288, N'/panel/edms/projectactivity/add', 2, 4, N'Entities.App.Edms.ProjectActivity', N'write'),
        (288, N'/panel/edms/projectactivity/save', 2, 3, N'Entities.App.Edms.ProjectActivity', N'write'),
        (288, N'/panel/edms/projectactivity/update', 2, 5, N'Entities.App.Edms.ProjectActivity', N'write'),
        (288, N'/panel/edms/projectactivity/delete', 2, 6, N'Entities.App.Edms.ProjectActivity', N'delete'),
        (289, N'/panel/edms/document/list', 1, 1, N'Entities.App.Edms.Document', N'read'),
        (289, N'/panel/edms/document/edit', 1, 5, N'Entities.App.Edms.Document', N'read'),
        (289, N'/panel/edms/document/fetchdata', 2, 2, N'Entities.App.Edms.Document', N'read'),
        (293, N'/panel/inv/partextrainfo/list', 1, 1, N'Entities.App.Inv.PartExtraInfo', N'read'),
        (293, N'/panel/inv/partextrainfo/edit', 1, 5, N'Entities.App.Inv.PartExtraInfo', N'read'),
        (293, N'/panel/inv/partextrainfo/fetchdata', 2, 2, N'Entities.App.Inv.PartExtraInfo', N'read'),
        (293, N'/panel/inv/partextrainfo/exporttoexcel', 2, 0, N'Entities.App.Inv.PartExtraInfo', N'export'),
        (293, N'/panel/inv/partextrainfo/new', 1, 4, N'Entities.App.Inv.PartExtraInfo', N'write'),
        (293, N'/panel/inv/partextrainfo/add', 2, 4, N'Entities.App.Inv.PartExtraInfo', N'write'),
        (293, N'/panel/inv/partextrainfo/save', 2, 3, N'Entities.App.Inv.PartExtraInfo', N'write'),
        (293, N'/panel/inv/partextrainfo/update', 2, 5, N'Entities.App.Inv.PartExtraInfo', N'write'),
        (293, N'/panel/inv/partextrainfo/delete', 2, 6, N'Entities.App.Inv.PartExtraInfo', N'delete'),
        (294, N'/panel/epms/proposalactivity/list', 1, 1, N'Entities.App.Epms.ProposalActivity', N'read'),
        (294, N'/panel/epms/proposalactivity/edit', 1, 5, N'Entities.App.Epms.ProposalActivity', N'read'),
        (294, N'/panel/epms/proposalactivity/fetchdata', 2, 2, N'Entities.App.Epms.ProposalActivity', N'read'),
        (294, N'/panel/epms/proposalactivity/exporttoexcel', 2, 0, N'Entities.App.Epms.ProposalActivity', N'export'),
        (294, N'/panel/epms/proposalactivity/new', 1, 4, N'Entities.App.Epms.ProposalActivity', N'write'),
        (294, N'/panel/epms/proposalactivity/add', 2, 4, N'Entities.App.Epms.ProposalActivity', N'write'),
        (294, N'/panel/epms/proposalactivity/save', 2, 3, N'Entities.App.Epms.ProposalActivity', N'write'),
        (294, N'/panel/epms/proposalactivity/update', 2, 5, N'Entities.App.Epms.ProposalActivity', N'write'),
        (294, N'/panel/epms/proposalactivity/delete', 2, 6, N'Entities.App.Epms.ProposalActivity', N'delete'),
        (299, N'/panel/bom/formulchangerequest/list', 1, 1, N'Entities.App.Bom.FormulChangeRequest', N'read'),
        (299, N'/panel/bom/formulchangerequest/edit', 1, 5, N'Entities.App.Bom.FormulChangeRequest', N'read'),
        (299, N'/panel/bom/formulchangerequest/fetchdata', 2, 2, N'Entities.App.Bom.FormulChangeRequest', N'read'),
        (299, N'/panel/bom/formulchangerequest/exporttoexcel', 2, 0, N'Entities.App.Bom.FormulChangeRequest', N'export'),
        (299, N'/panel/bom/formulchangerequest/new', 1, 4, N'Entities.App.Bom.FormulChangeRequest', N'write'),
        (299, N'/panel/bom/formulchangerequest/add', 2, 4, N'Entities.App.Bom.FormulChangeRequest', N'write'),
        (299, N'/panel/bom/formulchangerequest/save', 2, 3, N'Entities.App.Bom.FormulChangeRequest', N'write'),
        (299, N'/panel/bom/formulchangerequest/update', 2, 5, N'Entities.App.Bom.FormulChangeRequest', N'write'),
        (299, N'/panel/bom/formulchangerequest/delete', 2, 6, N'Entities.App.Bom.FormulChangeRequest', N'delete'),
        (299, N'/panel/inv/partcodingcategory/index', 1, 1, N'Entities.App.Inv.Part', N'read'),
        (299, N'/panel/inv/partcodingcategory/listrequest', 1, 1, N'Entities.App.Inv.Part', N'read'),
        (299, N'/panel/inv/partcodingcategory/requestnewpart', 1, 4, N'Entities.App.Inv.Part', N'write'),
        (299, N'/panel/inv/partcodingcategory/fetchpartsgriddata', 2, 2, N'Entities.App.Inv.Part', N'read'),
        (299, N'/panel/inv/partcodingcategory/gettreedata', 2, 2, N'Entities.App.Inv.Part', N'read'),
        (299, N'/panel/inv/partcodingcategory/getpartcodeprefix', 2, 1000, N'Entities.App.Inv.Part', N'read'),
        (299, N'/panel/inv/partcodingcategory/applycreationpart', 2, 1000, N'Entities.App.Inv.Part', N'write'),
        (350, N'/panel/edms/document/list', 1, 1, N'Entities.App.Edms.Document', N'read'),
        (350, N'/panel/edms/document/edit', 1, 5, N'Entities.App.Edms.Document', N'read'),
        (350, N'/panel/edms/document/fetchdata', 2, 2, N'Entities.App.Edms.Document', N'read'),
        (361, N'/panel/edms/document/personnelworkloadreport', 1, 1, N'Entities.App.Edms.Document', N'read'),
        (361, N'/panel/edms/document/getpersonnelworkloadreport', 2, 2, N'Entities.App.Edms.Document', N'read'),
        (377, N'/panel/inv/partextrainfo/list', 1, 1, N'Entities.App.Inv.PartExtraInfo', N'read'),
        (377, N'/panel/inv/partextrainfo/edit', 1, 5, N'Entities.App.Inv.PartExtraInfo', N'read'),
        (377, N'/panel/inv/partextrainfo/fetchdata', 2, 2, N'Entities.App.Inv.PartExtraInfo', N'read'),
        (377, N'/panel/inv/partextrainfo/exporttoexcel', 2, 0, N'Entities.App.Inv.PartExtraInfo', N'export'),
        (377, N'/panel/inv/partextrainfo/new', 1, 4, N'Entities.App.Inv.PartExtraInfo', N'write'),
        (377, N'/panel/inv/partextrainfo/add', 2, 4, N'Entities.App.Inv.PartExtraInfo', N'write'),
        (377, N'/panel/inv/partextrainfo/save', 2, 3, N'Entities.App.Inv.PartExtraInfo', N'write'),
        (377, N'/panel/inv/partextrainfo/update', 2, 5, N'Entities.App.Inv.PartExtraInfo', N'write'),
        (377, N'/panel/inv/partextrainfo/delete', 2, 6, N'Entities.App.Inv.PartExtraInfo', N'delete'),
        (449, N'/panel/eng/compressorsizing/calculator', 1, 0, NULL, N'read'),
        (449, N'/panel/eng/compressorsizing/reportviewer', 1, 0, NULL, N'read'),
        (449, N'/panel/eng/compressorsizing/getviewreport', 2, 2, NULL, N'read'),
        (449, N'/panel/eng/compressorsizing/viewerevent', 2, 2, NULL, N'read'),
        (449, N'/panel/eng/compressorsizing/selectbyefficiency', 2, 1000, NULL, N'fullonly'),
        (449, N'/panel/eng/compressorsizing/selectbyreliability', 2, 1000, NULL, N'fullonly'),
        (449, N'/panel/eng/compressorsizing/analyze', 2, 1000, NULL, N'fullonly'),
        (449, N'/panel/eng/compressorsizing/exportdatasheet', 2, 1000, NULL, N'fullonly'),
        (430, N'/panel/eng/filterwatertrap/calculator', 1, 0, NULL, N'read'),
        (430, N'/panel/eng/filterwatertrap/watertrapcalculation', 2, 1000, NULL, N'read'),
        (430, N'/panel/eng/filterwatertrap/filterselectioncalculation', 2, 1000, NULL, N'read'),
        (430, N'/panel/eng/filterwatertrap/hdtselectioncalculation', 2, 1000, NULL, N'read'),
        (430, N'/panel/eng/filterwatertrap/hdttoexcelfile', 2, 1000, NULL, N'read'),
        -- 495: مدیریت دسته داخل پارت لیست محصول (منوی جدا حذف شد)
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
        -- 496: پارت لیست محصول
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
        -- 497: پارت لیست مصرفی
        (497, N'/panel/eng/partlistdlbom/list', 1, 1, N'Entities.App.Eng.PartListDlBom', N'read'),
        (497, N'/panel/eng/partlistdlbom/edit', 1, 5, N'Entities.App.Eng.PartListDlBom', N'read'),
        (497, N'/panel/eng/partlistdlbom/fetchdata', 2, 2, N'Entities.App.Eng.PartListDlBom', N'read'),
        (497, N'/panel/eng/partlistdlbom/exporttoexcel', 2, 0, N'Entities.App.Eng.PartListDlBom', N'export'),
        (497, N'/panel/eng/partlistdlbom/save', 2, 3, N'Entities.App.Eng.PartListDlBom', N'write'),
        (497, N'/panel/eng/partlistdlbom/renew', 2, 1000, N'Entities.App.Eng.PartListDlBom', N'write'),
        (497, N'/panel/eng/partlistdlbom/print', 1, 0, N'Entities.App.Eng.PartListDlBom', N'read'),
        (526, N'/panel/bom/productpricecompare/list', 1, 1, N'Entities.App.Bom.ProductPriceCompare', N'read'),
        (526, N'/panel/bom/productpricecompare/fetchdata', 2, 2, N'Entities.App.Bom.ProductPriceCompare', N'read'),
        (526, N'/panel/bom/productpricecompare/details', 1, 1, N'Entities.App.Bom.ProductPriceCompare', N'read'),
        (526, N'/panel/bom/productpricecompare/availabledates', 2, 2, N'Entities.App.Bom.ProductPriceCompare', N'read'),
        (526, N'/panel/bom/productpricecompare/sendemail', 2, 1000, N'Entities.App.Bom.ProductPriceCompare', N'write'),
        (532, N'/panel/edms/document/list', 1, 1, N'Entities.App.Edms.Document', N'read'),
        (532, N'/panel/edms/document/edit', 1, 5, N'Entities.App.Edms.Document', N'read'),
        (532, N'/panel/edms/document/fetchdata', 2, 2, N'Entities.App.Edms.Document', N'read'),
        (541, N'/panel/inv/partextrainfo/list', 1, 1, N'Entities.App.Inv.PartExtraInfo', N'read'),
        (541, N'/panel/inv/partextrainfo/edit', 1, 5, N'Entities.App.Inv.PartExtraInfo', N'read'),
        (541, N'/panel/inv/partextrainfo/fetchdata', 2, 2, N'Entities.App.Inv.PartExtraInfo', N'read'),
        (541, N'/panel/inv/partextrainfo/exporttoexcel', 2, 0, N'Entities.App.Inv.PartExtraInfo', N'export'),
        (541, N'/panel/inv/partextrainfo/new', 1, 4, N'Entities.App.Inv.PartExtraInfo', N'write'),
        (541, N'/panel/inv/partextrainfo/add', 2, 4, N'Entities.App.Inv.PartExtraInfo', N'write'),
        (541, N'/panel/inv/partextrainfo/save', 2, 3, N'Entities.App.Inv.PartExtraInfo', N'write'),
        (541, N'/panel/inv/partextrainfo/update', 2, 5, N'Entities.App.Inv.PartExtraInfo', N'write'),
        (541, N'/panel/inv/partextrainfo/delete', 2, 6, N'Entities.App.Inv.PartExtraInfo', N'delete'),
        (543, N'/panel/edms/document/edit', 1, 5, N'Entities.App.Edms.Document', N'read'),
        (543, N'/panel/edms/document/list', 1, 1, N'Entities.App.Edms.Document', N'read'),
        (543, N'/panel/edms/document/fetchdata', 2, 2, N'Entities.App.Edms.Document', N'read'),
        (543, N'/panel/edms/document/update', 2, 5, N'Entities.App.Edms.Document', N'write'),
        (543, N'/panel/edms/document/save', 2, 3, N'Entities.App.Edms.Document', N'write'),
        (563, N'/panel/inv/partextrainfo/list', 1, 1, N'Entities.App.Inv.PartExtraInfo', N'read'),
        (563, N'/panel/inv/partextrainfo/edit', 1, 5, N'Entities.App.Inv.PartExtraInfo', N'read'),
        (563, N'/panel/inv/partextrainfo/fetchdata', 2, 2, N'Entities.App.Inv.PartExtraInfo', N'read'),
        (563, N'/panel/inv/partextrainfo/exporttoexcel', 2, 0, N'Entities.App.Inv.PartExtraInfo', N'export'),
        (563, N'/panel/inv/partextrainfo/new', 1, 4, N'Entities.App.Inv.PartExtraInfo', N'write'),
        (563, N'/panel/inv/partextrainfo/add', 2, 4, N'Entities.App.Inv.PartExtraInfo', N'write'),
        (563, N'/panel/inv/partextrainfo/save', 2, 3, N'Entities.App.Inv.PartExtraInfo', N'write'),
        (563, N'/panel/inv/partextrainfo/update', 2, 5, N'Entities.App.Inv.PartExtraInfo', N'write'),
        (563, N'/panel/inv/partextrainfo/delete', 2, 6, N'Entities.App.Inv.PartExtraInfo', N'delete');

    IF OBJECT_ID('tempdb..#PageProfile') IS NOT NULL DROP TABLE #PageProfile;
    CREATE TABLE #PageProfile
    (
        PageId      INT           NOT NULL,
        ProfileName NVARCHAR(150) NOT NULL,
        Flag        NVARCHAR(20)  NOT NULL
    );
    INSERT INTO #PageProfile (PageId, ProfileName, Flag) VALUES
        (98, N'productformul_listinfo', N'read'),
        (98, N'vw_productformulwithBom', N'read'),
        (98, N'vw_productformulwithBomWithPrice', N'price'),
        (99, N'vw_productformulwithBom', N'read'),
        (99, N'vw_productformulwithBomWithPrice', N'price'),
        (103, N'productgroup_listinfo', N'read'),
        (106, N'part_listinfo', N'read'),
        (107, N'productformul_listinfo', N'read'),
        (107, N'vw_productformulwithBom', N'read'),
        (107, N'vw_productformulwithBomWithPrice', N'price'),
        (108, N'vw_productformulwithBom', N'read'),
        (108, N'vw_productformulwithBomWithPrice', N'price'),
        (109, N'partextrainfo_listinfo', N'read'),
        (109, N'vw_PartextrainfoOilInject', N'read'),
        (110, N'partextrainfo_listinfo', N'read'),
        (110, N'vw_PartextrainfoOilInject', N'read'),
        (116, N'vw_productformulwithBomWithPrice', N'price'),
        (125, N'part_listinfo', N'read'),
        (163, N'vw_ProjectInfo', N'read'),
        (165, N'vw_ProjectVpisManagment', N'read'),
        (166, N'vw_DocumentAllProjectInfo', N'read'),
        (166, N'vw_DocumentAllProjectInfonew', N'write'),
        (169, N'vw_TransmitalInfo', N'read'),
        (218, N'projectattachment_listinfo', N'read'),
        (221, N'vw_DocumentDCC', N'read'),
        (221, N'vw_DocumentReciveAndSend', N'read'),
        (223, N'vw_DocumentMyActions', N'read'),
        (232, N'vw_DocumentAllProjectInfo', N'read'),
        (233, N'vw_DocumentMyArchive', N'read'),
        (238, N'vw_DocumentAllProjectInfo', N'read'),
        (248, N'vw_DocumentAllProjectInfo', N'read'),
        (252, N'proposal_listinfo', N'read'),
        (255, N'vw_DocumentMyCartabl', N'read'),
        (255, N'vw_DocumentAllProjectInfo', N'read'),
        (257, N'vw_DocumentMyActions', N'read'),
        (260, N'vw_DocumentReciveAndSend', N'read'),
        (260, N'vw_DocumentAllProjectInfo', N'read'),
        (264, N'vw_DocumentAllProjectInfo', N'read'),
        (275, N'vw_DocumentMyCartabl', N'read'),
        (279, N'projectattachment_listinfo', N'read'),
        (288, N'projectactivity_listinfo', N'read'),
        (289, N'vw_DocumentMyArchive', N'read'),
        (293, N'partextrainfo_listinfo', N'read'),
        (293, N'vw_PartextrainfoDerayer', N'read'),
        (294, N'proposalactivity_listinfo', N'read'),
        (299, N'formulchangerequest_listinfo', N'read'),
        (350, N'vw_DocumentMyCartabl', N'read'),
        (377, N'partextrainfo_listinfo', N'read'),
        (377, N'vw_PartextrainfoPsa', N'read'),
        (526, N'vw_productformulwithBomWithPrice', N'price'),
        (532, N'vw_DocumentAllProjectInfo', N'read'),
        (541, N'partextrainfo_listinfo', N'read'),
        (541, N'vw_PartextrainfoOilFree', N'read'),
        (543, N'vw_DocumentAllProjectInfo', N'read'),
        (563, N'partextrainfo_listinfo', N'read'),
        (563, N'vw_PartextrainfoStatic', N'read');

    IF OBJECT_ID('tempdb..#SkippedPage') IS NOT NULL DROP TABLE #SkippedPage;
    CREATE TABLE #SkippedPage (PageId INT NOT NULL PRIMARY KEY, Reason NVARCHAR(300) NOT NULL);
    INSERT INTO #SkippedPage (PageId, Reason) VALUES
        (61, N'صفحه خدمات پس از فروش روی سیستم مهندسی — خارج از محدوده'),
        (164, N'اقلام پروژه EDMS — کنترلر جدا در هاویار نیست'),
        (236, N'سایزینگ درایر خاص — فقط CompressorSizing پیاده شده'),
        (239, N'سایزینگ کمپرسور قدیمی — صفحه 449 جایگزین'),
        (240, N'سایزینگ تله آبگیر — کنترلر نیست'),
        (242, N'سایزینگ ACT — کنترلر نیست'),
        (243, N'سایزینگ Air Receiver — کنترلر نیست'),
        (244, N'سایزینگ HDT — کنترلر نیست'),
        (245, N'گزارش عملکرد پرسنل EDMS — صفحه هاویار نیست'),
        (246, N'گزارش پیشرفت پروژه — صفحه هاویار نیست'),
        (249, N'مناقصه EPMS — منوی مرده / صفحه هاویار نیست'),
        (250, N'پیوست مناقصه EPMS — صفحه نیست'),
        (265, N'استعلام قیمت تجهیزات پروپوزال — کنترلر نیست'),
        (268, N'آرشیو متره — منوی مرده'),
        (274, N'آرشیو مدارک مالی — منوی مرده'),
        (282, N'گزارش تجمعی پروژه — صفحه هاویار نیست'),
        (291, N'گزارش تجمعی عملکرد — صفحه هاویار نیست'),
        (306, N'برنامه زمانی EDMS — منوی مرده / صفحه نیست'),
        (307, N'استعلام فنی گروه کالا — صفحه هاویار نیست'),
        (308, N'استعلام فنی پیمانکار — صفحه هاویار نیست'),
        (309, N'استعلام فنی — صفحه هاویار نیست'),
        (311, N'گروه کالا محصولات خاص — صفحه نیست'),
        (312, N'کالا محصولات خاص — صفحه نیست'),
        (313, N'تجهیز محصولات خاص — صفحه نیست'),
        (314, N'BOM تجهیزات خاص — صفحه نیست'),
        (315, N'کامنت استعلام فنی — صفحه نیست'),
        (320, N'گزارش تجهیزات خاص — صفحه نیست'),
        (325, N'آرشیو اسناد مناقصه EPMS — منوی مرده'),
        (351, N'گزارش عملکرد پرسنل EPMS — صفحه نیست'),
        (380, N'گزارش شاخص ارزیابی — صفحه هاویار نیست'),
        (405, N'تحقیق و توسعه — صفحه هاویار نیست'),
        (406, N'درخواست تغییر طراحی — صفحه هاویار نیست'),
        (407, N'نرخ ارز مهندسی — به Acc داده نمی‌شود'),
        (411, N'وزن مخازن — کنترلر نیست'),
        (429, N'فیلتر سایزینگ — کنترلر نیست'),
        (465, N'سایزینگ نیتروژن‌ساز — کنترلر نیست'),
        (558, N'اقلام پروژه 558 — کنترلر جدا نیست'),
        (566, N'استعلام فنی پیمانکاران — صفحه نیست');

    -----------------------------------------------------------------------------
    PRINT N'=== [3] Ensure one Havayar role per HTS Engineering group ===';
    -----------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#HtsGroup') IS NOT NULL DROP TABLE #HtsGroup;
    CREATE TABLE #HtsGroup
    (
        HtsGroupId INT            NOT NULL PRIMARY KEY,
        Title      NVARCHAR(200)  NOT NULL,
        RoleName   NVARCHAR(200)  NULL,
        RoleId     BIGINT         NULL
    );

    INSERT INTO #HtsGroup (HtsGroupId, Title)
    SELECT g.UserGroup_ID, MAX(LTRIM(RTRIM(g.UserGroup_Title)))
    FROM [TMS].[TotalSystem].[dbo].[Vw_Permission] g
    WHERE g.System_ID = 26
      AND g.IsActive = 1
      AND g.UserGroup_ID IS NOT NULL
    GROUP BY g.UserGroup_ID;

    -- Reuse existing named roles
    UPDATE h
    SET RoleId = r.RoleId,
        RoleName = r.RoleName
    FROM #HtsGroup h
    INNER JOIN #Reuse r ON r.HtsGroupId = h.HtsGroupId;

    -- Preferred id = 260000 + group id
    DECLARE @Gid INT, @Title NVARCHAR(200), @WantedId BIGINT, @RoleName NVARCHAR(200), @RoleId BIGINT;
    DECLARE @Created INT = 0, @Reused INT = 0, @ExistsById INT = 0;

    DECLARE g_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT HtsGroupId, Title FROM #HtsGroup WHERE RoleId IS NULL;
    OPEN g_cur;
    FETCH NEXT FROM g_cur INTO @Gid, @Title;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @WantedId = 260000 + @Gid;
        SET @RoleName = @Title;
        IF EXISTS (SELECT 1 FROM system.Role r WHERE r.Name = @RoleName AND r.Id <> @WantedId)
            SET @RoleName = @Title + N' (' + CAST(@Gid AS nvarchar(20)) + N')';

        IF EXISTS (SELECT 1 FROM system.Role WHERE Id = @WantedId)
        BEGIN
            UPDATE system.Role
            SET Name = @RoleName,
                Title = @Title,
                IsActive = 1,
                ModifiedById = 1,
                ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now,
                ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Id = @WantedId;
            SET @RoleId = @WantedId;
            SET @ExistsById += 1;
        END
        ELSE
        BEGIN
            SET IDENTITY_INSERT system.Role ON;
            INSERT INTO system.Role
                (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
                 CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime,
                 CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
            VALUES
                (@WantedId, @RoleName, @Title, 1, @SeedUser, 1, @SeedUser,
                 @Now, @Now, @NowShamsi, @NowShamsi, 1);
            SET IDENTITY_INSERT system.Role OFF;
            SET @RoleId = @WantedId;
            SET @Created += 1;
        END

        UPDATE #HtsGroup SET RoleId = @RoleId, RoleName = @RoleName WHERE HtsGroupId = @Gid;
        SET @RoleId = NULL;
        FETCH NEXT FROM g_cur INTO @Gid, @Title;
    END
    CLOSE g_cur;
    DEALLOCATE g_cur;

    -- Keep reused role titles as-is; just ensure active
    UPDATE r
    SET IsActive = 1,
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    FROM system.Role r
    INNER JOIN #Reuse x ON x.RoleId = r.Id
    WHERE r.IsActive <> 1 OR r.IsActive IS NULL;

    SELECT @Reused = COUNT(*) FROM #Reuse;
    PRINT N'  Roles created: ' + CAST(@Created AS nvarchar(20));
    PRINT N'  Roles reused by name/id: ' + CAST(@ExistsById AS nvarchar(20));
    PRINT N'  Roles mapped to existing (DCC/MDR/PUM/workload): ' + CAST(@Reused AS nvarchar(20));

    -----------------------------------------------------------------------------
    PRINT N'=== [4] Controller RoleAccess from HTS group grants ===';
    -----------------------------------------------------------------------------
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
    WHERE System_ID = 26 AND IsActive = 1 AND UserGroup_ID IS NOT NULL;

    IF OBJECT_ID('tempdb..#ActionAccess') IS NOT NULL DROP TABLE #ActionAccess;
    CREATE TABLE #ActionAccess
    (
        RoleId               BIGINT         NOT NULL,
        Path                 NVARCHAR(300)  NOT NULL,
        ActionAccessType     INT            NOT NULL,
        ActionAccessItemType INT            NOT NULL,
        EntityName           NVARCHAR(200)  NULL
    );

    INSERT INTO #ActionAccess (RoleId, Path, ActionAccessType, ActionAccessItemType, EntityName)
    SELECT DISTINCT h.RoleId, p.Path, p.ActionAccessType, p.ActionAccessItemType, p.EntityName
    FROM #Grant g
    INNER JOIN #HtsGroup h ON h.HtsGroupId = g.HtsGroupId
    INNER JOIN #PagePath p ON p.PageId = g.PageId
    WHERE
        (p.Flag = N'read'     AND g.PermissionId IN (2, 3, 7))
     OR (p.Flag = N'export'   AND g.PermissionId IN (2, 3, 7, 10, 62))
     OR (p.Flag = N'write'    AND g.PermissionId IN (2, 4, 5))
     OR (p.Flag = N'delete'   AND g.PermissionId IN (2, 6))
     OR (p.Flag = N'fullonly' AND g.PermissionId = 2);

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT a.Path, a.ActionAccessType, a.ActionAccessItemType, a.EntityName, NULL, NULL, NULL, a.RoleId,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #ActionAccess a
    WHERE a.RoleId IS NOT NULL
      AND NOT EXISTS (
          SELECT 1 FROM system.RoleAccess x
          WHERE x.RoleId = a.RoleId AND x.Path = a.Path AND x.ActionAccessType = a.ActionAccessType
      );
    PRINT N'  Inserted controller RoleAccess: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    -----------------------------------------------------------------------------
    PRINT N'=== [5] DataProfile RoleAccess ===';
    -----------------------------------------------------------------------------
    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT
        N'dataProfile_' + CAST(q.Id AS nvarchar(20)),
        3, 7, q.EntityFullName, q.Title, NULL, q.Id, h.RoleId,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #Grant g
    INNER JOIN #HtsGroup h ON h.HtsGroupId = g.HtsGroupId
    INNER JOIN #PageProfile pp ON pp.PageId = g.PageId
    INNER JOIN system.SavedQuery q ON q.Name = pp.ProfileName
    WHERE h.RoleId IS NOT NULL
      AND (
            (pp.Flag = N'read'  AND g.PermissionId IN (2, 3, 7, 4, 5))
         OR (pp.Flag = N'write' AND g.PermissionId IN (2, 5))
         OR (pp.Flag = N'price' AND g.PermissionId = 8)
      )
      AND NOT EXISTS (
          SELECT 1 FROM system.RoleAccess ra
          WHERE ra.ActionAccessType = 3 AND ra.RowId = q.Id AND ra.RoleId = h.RoleId
      );
    PRINT N'  Inserted DataProfile RoleAccess: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    -----------------------------------------------------------------------------
    PRINT N'=== [6] Assign group members (skip extra DCC users) ===';
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
           h.RoleName,
           h.RoleId
    FROM [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
    INNER JOIN #HtsGroup h ON h.HtsGroupId = m.UserGroup_FK
    LEFT JOIN #Reuse ru ON ru.HtsGroupId = h.HtsGroupId
    WHERE u.IsActive = 1
      AND h.RoleId IS NOT NULL
      AND h.RoleName IS NOT NULL
      AND ISNULL(ru.SkipUserAssign, 0) = 0
      AND COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')) IS NOT NULL;

    -- Also map alternate HTS Username when it differs from AD
    INSERT INTO #UserRoleMap (Username, RoleName, RoleId)
    SELECT DISTINCT u.Username, m.RoleName, m.RoleId
    FROM #UserRoleMap m
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u
        ON LOWER(u.ActiveDirectoryUsername) = LOWER(m.Username)
    WHERE u.Username IS NOT NULL
      AND LOWER(u.Username) <> LOWER(m.Username)
      AND NOT EXISTS (
          SELECT 1 FROM #UserRoleMap x
          WHERE LOWER(x.Username) = LOWER(u.Username) AND x.RoleId = m.RoleId
      );

    ;WITH d AS (
        SELECT Username, RoleId,
               ROW_NUMBER() OVER (PARTITION BY LOWER(Username), RoleId ORDER BY Username) AS rn
        FROM #UserRoleMap
    )
    DELETE FROM d WHERE rn > 1;

    DECLARE @MapUsername nvarchar(200), @MapRoleName nvarchar(200), @MapRoleId bigint, @MapUserId bigint;
    DECLARE @Assigned int = 0, @Missing int = 0, @SkippedAlready int = 0;

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
            PRINT N'  MISSING USER (skip): ' + @MapUsername + N' for role ' + @MapRoleName;
            SET @Missing = @Missing + 1;
        END
        ELSE IF EXISTS (
            SELECT 1 FROM system.[User] u
            CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
            WHERE u.Id = @MapUserId AND TRY_CAST(j.value AS bigint) = @MapRoleId
        )
            SET @SkippedAlready = @SkippedAlready + 1;
        ELSE
        BEGIN
            UPDATE system.[User]
            SET
                RoleIds = CASE
                    WHEN RoleIds IS NULL OR LTRIM(RTRIM(RoleIds)) = N'' OR RoleIds = N'[]'
                        THEN N'[' + CAST(@MapRoleId AS nvarchar(20)) + N']'
                    ELSE STUFF(RoleIds, LEN(RoleIds), 1, N',' + CAST(@MapRoleId AS nvarchar(20)) + N']')
                END,
                Roles = CASE
                    WHEN EXISTS (SELECT 1 FROM OPENJSON(ISNULL(Roles, N'[]')) j WHERE j.value = @MapRoleName) THEN Roles
                    WHEN Roles IS NULL OR LTRIM(RTRIM(Roles)) = N'' OR Roles = N'[]'
                        THEN N'["' + REPLACE(@MapRoleName, N'"', N'') + N'"]'
                    ELSE STUFF(Roles, LEN(Roles), 1, N',"' + REPLACE(@MapRoleName, N'"', N'') + N'"]')
                END,
                ModifiedById = 1,
                ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now,
                ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Id = @MapUserId;
            SET @Assigned = @Assigned + 1;
        END

        SET @MapUserId = NULL;
        FETCH NEXT FROM map_cur INTO @MapUsername, @MapRoleName, @MapRoleId;
    END
    CLOSE map_cur;
    DEALLOCATE map_cur;

    PRINT N'  User role assignments applied: ' + CAST(@Assigned AS nvarchar(20));
    PRINT N'  Already had role (skipped): ' + CAST(@SkippedAlready AS nvarchar(20));
    PRINT N'  Missing users (logged): ' + CAST(@Missing AS nvarchar(20));

    -----------------------------------------------------------------------------
    PRINT N'=== [6b] Ensure Zare.mo has Filter/WaterTrap FullAccess role ===';
    -----------------------------------------------------------------------------
    -- HTS granted page 430 FullAccess to Zare.mo without a group; keep idempotent.
    DECLARE @ZareRoleId BIGINT = 260089;
    DECLARE @ZareUserId BIGINT = NULL;
    SELECT TOP 1 @ZareUserId = u.Id
    FROM system.[User] u
    WHERE u.Id = 240 OR LOWER(u.Username) = N'zare.mo';

    IF @ZareUserId IS NOT NULL
       AND EXISTS (SELECT 1 FROM system.Role r WHERE r.Id = @ZareRoleId AND r.IsActive = 1)
       AND NOT EXISTS (
            SELECT 1
            FROM system.[User] u
            CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
            WHERE u.Id = @ZareUserId
              AND TRY_CAST(j.value AS bigint) IN (260089, 260350, 260488)
       )
    BEGIN
        UPDATE system.[User]
        SET RoleIds = CASE
                WHEN RoleIds IS NULL OR LTRIM(RTRIM(RoleIds)) = N'' OR RoleIds = N'[]'
                    THEN N'[' + CAST(@ZareRoleId AS nvarchar(20)) + N']'
                ELSE STUFF(RoleIds, LEN(RoleIds), 1, N',' + CAST(@ZareRoleId AS nvarchar(20)) + N']')
            END,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @ZareUserId;
        PRINT N'  Assigned role 260089 to Zare.mo for Filter/WaterTrap.';
    END
    ELSE
        PRINT N'  Zare.mo already has 260089/260350/260488 (or user/role missing) — skipped.';

    COMMIT TRANSACTION;
    PRINT N'=== DONE: Seed_Engineering_HtsGroups_RolesAndUsers committed ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

PRINT N'--- verify ---';
SELECT N'eng still on users' AS CheckName, COUNT(*) AS Cnt
FROM system.[User] u
CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
WHERE TRY_CAST(j.value AS bigint) = 2;

SELECT N'EngMenu still on users' AS CheckName, COUNT(*) AS Cnt
FROM system.[User] u
CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
WHERE TRY_CAST(j.value AS bigint) = 400006;

SELECT N'DCC role users' AS CheckName, COUNT(DISTINCT u.Id) AS Cnt
FROM system.[User] u
CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
WHERE TRY_CAST(j.value AS bigint) = 200000;

SELECT N'PUM 100019 users' AS CheckName, COUNT(DISTINCT u.Id) AS Cnt
FROM system.[User] u
CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
WHERE TRY_CAST(j.value AS bigint) = 100019;

SELECT N'441 delete-role users' AS CheckName, COUNT(DISTINCT u.Id) AS Cnt
FROM system.[User] u
CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
WHERE TRY_CAST(j.value AS bigint) = 260441;

SELECT N'CompressorSizing RoleAccess' AS CheckName, COUNT(*) AS Cnt
FROM system.RoleAccess
WHERE Path LIKE N'/panel/eng/compressorsizing/%';

SELECT N'FilterWaterTrap RoleAccess' AS CheckName, COUNT(*) AS Cnt
FROM system.RoleAccess
WHERE Path LIKE N'/panel/eng/filterwatertrap/%';

SELECT N'Eng HTS roles 260000+' AS CheckName, COUNT(*) AS Cnt
FROM system.Role
WHERE Id BETWEEN 260000 AND 260999 AND IsActive = 1;

SELECT TOP 20 r.Id, r.Name, r.Title,
       (SELECT COUNT(*) FROM system.RoleAccess ra WHERE ra.RoleId = r.Id) AS AccessCnt,
       (SELECT COUNT(*) FROM system.[User] u CROSS APPLY OPENJSON(ISNULL(u.RoleIds,N'[]')) j
        WHERE TRY_CAST(j.value AS bigint) = r.Id) AS Users
FROM system.Role r
WHERE r.Id BETWEEN 260000 AND 260999 OR r.Id IN (200000, 200007, 200008, 100019)
ORDER BY r.Id;
