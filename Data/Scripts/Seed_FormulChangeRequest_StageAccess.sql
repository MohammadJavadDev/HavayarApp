/*
  Seed_FormulChangeRequest_StageAccess.sql
  دسترسی مرحله‌ای «درخواست ویرایش اقلام فرمول» (/panel/bom/formulchangerequest) — معادل صفحه 299 HTS:
    گروه 145 برنامه‌ریزی (Permission 80) · 146 QC (81) · 147 مهندسی (82) · 150 اعمال نهایی (84)
  نقش‌ها از قبل (Seed_Engineering_HtsGroups_RolesAndUsers.sql) ساخته شده‌اند ولی RoleAccess نداشتند، پس
  کاربرانی مثل sazvar.m و neysari.n حتی صفحه را باز نمی‌کردند.

  این اسکریپت به نقش‌های مرحله‌ای (برنامه‌ریزی / QC / مهندسی / اعمال نهایی) اضافه می‌کند:
    list, edit (View) · fetchdata, save, update, exporttoexcel (Api) · نمایه داده FormulChangeRequest
  new / add / delete فقط برای نقش «کارشناسان درخواست ایجاد/تغییر قطعه» (گروه 144) می‌ماند.
  خروجی اکسل برای گروه 144 هم (اگر نبود) اضافه می‌شود. نقش «مشاهده همه» (164) تغییری نمی‌کند.
  اجازهٔ هر انتقال وضعیت در کد (FormulChangeRequestWorkflow) با نام همین نقش‌ها کنترل می‌شود.

  Idempotent. نقش‌ها با Name پیدا می‌شوند (بدون Id ثابت).
  Encoding: UTF-8 with BOM. Apply: sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Seed_FormulChangeRequest_StageAccess.sql"
  بعد از اجرا WebApp را restart کنید (کش دسترسی / نمایه داده).
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-formulchange-stage-access';
    DECLARE @Entity NVARCHAR(200) = N'Entities.App.Bom.FormulChangeRequest';

    DECLARE @Roles TABLE (Kind NVARCHAR(20), RoleName NVARCHAR(200), RoleId BIGINT NULL);
    INSERT INTO @Roles (Kind, RoleName) VALUES
        (N'general',     N'کارشناسان درخواست ایجاد/تغییر قطعه'),
        (N'planning',    N'کارشناسان درخواست ایجاد/تغییر قطعه (برنامه ریزی)'),
        (N'qc',          N'کارشناسان درخواست ایجاد/تغییر قطعه (QC)'),
        (N'engineering', N'کارشناسان درخواست ایجاد/تغییر قطعه (مهندسی)'),
        (N'final',       N'کارشناسان درخواست ایجاد/تغییر قطعه (اعمال نهایی)');

    UPDATE r SET RoleId = x.Id
    FROM @Roles r
    INNER JOIN system.Role x ON x.Name = r.RoleName;

    IF EXISTS (SELECT 1 FROM @Roles WHERE RoleId IS NULL)
    BEGIN
        DECLARE @Missing NVARCHAR(MAX) = (SELECT STRING_AGG(RoleName, N' | ') FROM @Roles WHERE RoleId IS NULL);
        RAISERROR(N'Role not found by Name: %s', 16, 1, @Missing);
    END

    -- مسیرها: Path, ActionAccessType (1=View, 2=Api), ActionAccessItemType
    DECLARE @Paths TABLE (Path NVARCHAR(300), ActionAccessType INT, ActionAccessItemType INT, ForStageRoles BIT);
    INSERT INTO @Paths (Path, ActionAccessType, ActionAccessItemType, ForStageRoles) VALUES
        (N'/panel/bom/formulchangerequest/list',          1, 1, 1),
        (N'/panel/bom/formulchangerequest/edit',          1, 5, 1),
        (N'/panel/bom/formulchangerequest/fetchdata',     2, 2, 1),
        (N'/panel/bom/formulchangerequest/save',          2, 3, 1),
        (N'/panel/bom/formulchangerequest/update',        2, 5, 1),
        (N'/panel/bom/formulchangerequest/exporttoexcel', 2, 0, 1);

    -----------------------------------------------------------------------------
    PRINT N'=== [1] RoleAccess controller paths (stage roles + general export) ===';
    -----------------------------------------------------------------------------
    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT p.Path, p.ActionAccessType, p.ActionAccessItemType, @Entity, NULL, NULL, NULL, r.RoleId,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM @Roles r
    CROSS JOIN @Paths p
    WHERE ((r.Kind IN (N'planning', N'qc', N'engineering', N'final') AND p.ForStageRoles = 1)
       OR (r.Kind = N'general' AND p.Path = N'/panel/bom/formulchangerequest/exporttoexcel'))
    AND NOT EXISTS (
        SELECT 1 FROM system.RoleAccess x
        WHERE x.RoleId = r.RoleId AND x.Path = p.Path AND x.ActionAccessType = p.ActionAccessType
    );
    PRINT N'  Inserted controller RoleAccess: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    -----------------------------------------------------------------------------
    PRINT N'=== [2] RoleAccess Data Profile (type 3) for stage roles ===';
    -----------------------------------------------------------------------------
    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(q.Id AS nvarchar(20)),
        3, 7, q.EntityFullName, q.Title, NULL, q.Id, r.RoleId,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM @Roles r
    CROSS JOIN system.SavedQuery q
    WHERE r.Kind IN (N'planning', N'qc', N'engineering', N'final')
      AND LOWER(q.EntityFullName) = LOWER(@Entity)
      AND EXISTS (  -- فقط نمایه‌هایی که نقش عمومی (144) هم دارد
          SELECT 1 FROM system.RoleAccess g
          INNER JOIN @Roles gr ON gr.RoleId = g.RoleId AND gr.Kind = N'general'
          WHERE g.ActionAccessType = 3 AND g.RowId = q.Id)
      AND NOT EXISTS (
          SELECT 1 FROM system.RoleAccess ra
          WHERE ra.ActionAccessType = 3 AND ra.RowId = q.Id AND ra.RoleId = r.RoleId
      );
    PRINT N'  Inserted DataProfile RoleAccess: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'Seed failed: %s', 16, 1, @Err);
END CATCH;

-- ===== Verification =====
SELECT r.Id AS RoleId, r.Name AS RoleName, COUNT(a.Id) AS AccessRows
FROM system.Role r
LEFT JOIN system.RoleAccess a ON a.RoleId = r.Id
    AND (a.Path LIKE N'/panel/bom/formulchangerequest/%' OR a.ActionAccessType = 3 AND LOWER(a.EntityName) = LOWER(N'Entities.App.Bom.FormulChangeRequest'))
WHERE r.Name LIKE N'کارشناسان درخواست ایجاد/تغییر قطعه%'
GROUP BY r.Id, r.Name
ORDER BY r.Id;

SELECT r.Id AS RoleId, a.Path, a.ActionAccessType, a.ActionAccessItemType
FROM system.Role r
INNER JOIN system.RoleAccess a ON a.RoleId = r.Id
WHERE r.Name LIKE N'کارشناسان درخواست ایجاد/تغییر قطعه%'
  AND (a.Path LIKE N'/panel/bom/formulchangerequest/%' OR a.Path LIKE N'dataProfile[_]%')
ORDER BY r.Id, a.Path;
