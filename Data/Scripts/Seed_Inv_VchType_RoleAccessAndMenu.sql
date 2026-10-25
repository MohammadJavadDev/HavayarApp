/*
  دسترسی نقش ShowAllMenus و آیتم منوی «نوع رسید انبار» در منوی همه منوها.
  کنار لیست کالا در شاخه انبار / اطلاعات پایه.
  Encoding: UTF-8 with BOM (sqlcmd -f 65001). Idempotent.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = SYSDATETIME();
    DECLARE @NowShamsi NVARCHAR(30) = dbo.ToShamsiDate(CAST(@Now AS DATE), '/');
    DECLARE @Seed NVARCHAR(150) = N'seed-inv-vchtype';
    DECLARE @RoleId BIGINT = (SELECT Id FROM system.Role WHERE Name = N'ShowAllMenus');

    IF @RoleId IS NULL
        THROW 50001, N'نقش ShowAllMenus یافت نشد.', 1;

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT a.Path, a.AType, a.IType, N'Entities.App.Inv.VchType', NULL, NULL, NULL, @RoleId,
        1, 1, @Seed, @Seed, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM (VALUES
        (N'/panel/inv/vchtype/list', 1, 1),
        (N'/panel/inv/vchtype/edit', 1, 5),
        (N'/panel/inv/vchtype/new', 1, 4),
        (N'/panel/inv/vchtype/fetchdata', 2, 2),
        (N'/panel/inv/vchtype/save', 2, 3),
        (N'/panel/inv/vchtype/add', 2, 4),
        (N'/panel/inv/vchtype/update', 2, 5),
        (N'/panel/inv/vchtype/delete', 2, 6),
        (N'/panel/inv/vchtype/exporttoexcel', 2, 0)
    ) a(Path, AType, IType)
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess ra
        WHERE ra.RoleId = @RoleId AND ra.Path = a.Path
    );
    PRINT N'  RoleAccess inserted: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    DECLARE @Anchor NVARCHAR(MAX) = N'{"text":"کالا","icon":"ki-cube-2 ki-outline","iconColor":"#FDE047","path":"/panel/inv/part/list","a_attr":{"href":"/panel/inv/part/list"},"data":{"iconColor":"#FDE047","path":"/panel/inv/part/list"},"children":[]}';
    DECLARE @MenuItem NVARCHAR(MAX) = N',{"text":"نوع رسید انبار","icon":"ki-document ki-outline","iconColor":"#FDE047","path":"/panel/inv/vchtype/list","a_attr":{"href":"/panel/inv/vchtype/list"},"data":{"iconColor":"#FDE047","path":"/panel/inv/vchtype/list"},"children":[]}';

    UPDATE system.SystemMenu
    SET Content = REPLACE(Content, @Anchor, @Anchor + @MenuItem),
        ModifiedById = 1,
        ModifiedByName = @Seed,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Id = 13
      AND CHARINDEX(N'/panel/inv/vchtype/list', Content) = 0
      AND CHARINDEX(@Anchor, Content) > 0;
    PRINT N'  Menu rows updated: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    IF NOT EXISTS (
        SELECT 1 FROM system.SystemMenu
        WHERE Id = 13 AND CHARINDEX(N'/panel/inv/vchtype/list', Content) > 0
    )
        THROW 50002, N'آیتم منوی نوع رسید انبار در همه منوها درج نشد.', 1;

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_Inv_VchType_RoleAccessAndMenu ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT Path, ActionAccessType, ActionAccessItemType
FROM system.RoleAccess
WHERE RoleId = (SELECT Id FROM system.Role WHERE Name = N'ShowAllMenus')
  AND Path LIKE N'/panel/inv/vchtype/%'
ORDER BY Path;
