/*
================================================================================
Patch_OrganizationalDocument_ArchiveRoot.sql
اتصال فایل‌های اسناد سازمانی منتقل‌شده از HTS به آرشیو محلی سرور:

    \\172.20.30.14\COTMS\Hts_Documents\Catalogs\   ->   D:\PortalData\Hts_Documents\Catalogs\

- فقط FileEntity رکوردهای OrganizationalDocument (EntityPropName = File) که PhysicalPath آن‌ها
  با ریشهٔ قدیمی شروع می‌شود.
- با xp_fileexist (از روی همان سرور) وجود هر فایل جدید بررسی می‌شود؛ فقط فایل‌های موجود تغییر می‌کنند.
- مسیر قبلی در dbo.FileEntity_PathBackup_20261010 ذخیره می‌شود (برگشت: بخش Rollback انتهای فایل).
- Idempotent: ردیف‌هایی که قبلاً به ریشهٔ جدید تغییر کرده‌اند دوباره پردازش نمی‌شوند.

اجرا (UTF-8 با BOM):
  Dry-run : sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -v Apply=0 -i "Data\Scripts\Patch_OrganizationalDocument_ArchiveRoot.sql"
  Apply   : sqlcmd ... -v Apply=1 -i ...
================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Apply BIT = $(Apply);
DECLARE @OldRoot NVARCHAR(400) = N'\\172.20.30.14\COTMS\Hts_Documents\Catalogs\';
DECLARE @NewRoot NVARCHAR(400) = N'D:\PortalData\Hts_Documents\Catalogs\';

IF OBJECT_ID('tempdb..#Chk') IS NOT NULL DROP TABLE #Chk;
CREATE TABLE #Chk (
    FileId BIGINT NOT NULL PRIMARY KEY,
    DocumentId BIGINT NOT NULL,
    OldPath NVARCHAR(4000) NOT NULL,
    NewPath NVARCHAR(4000) NOT NULL,
    NewExists INT NULL
);

INSERT INTO #Chk (FileId, DocumentId, OldPath, NewPath)
SELECT f.Id, d.Id, f.PhysicalPath, @NewRoot + SUBSTRING(f.PhysicalPath, LEN(@OldRoot) + 1, 4000)
FROM Gnr.OrganizationalDocument d
INNER JOIN dbo.FileEntity f ON f.Id = d.FileId
WHERE f.PhysicalPath LIKE @OldRoot + N'%';

DECLARE @Id BIGINT, @P NVARCHAR(4000);
IF OBJECT_ID('tempdb..#R') IS NOT NULL DROP TABLE #R;
CREATE TABLE #R (FileExists INT, IsDir INT, ParentExists INT);

DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT FileId, NewPath FROM #Chk;
OPEN c;
FETCH NEXT FROM c INTO @Id, @P;
WHILE @@FETCH_STATUS = 0
BEGIN
    DELETE FROM #R;
    INSERT INTO #R EXEC master.sys.xp_fileexist @P;
    UPDATE #Chk SET NewExists = (SELECT TOP 1 CASE WHEN FileExists = 1 THEN 1 ELSE 0 END FROM #R) WHERE FileId = @Id;
    FETCH NEXT FROM c INTO @Id, @P;
END
CLOSE c;
DEALLOCATE c;

-- حالت خاص HTS: عنوان پوشه‌ای با «/» (".../Manual/Catalogue") در آرشیو واقعی به
-- Catalogs\Manual\Catalogue\... تبدیل شده است. اگر مسیر مستقیم نبود، این مسیر جایگزین امتحان می‌شود.
DECLARE @Marker NVARCHAR(100) = N'\.../Manual/Catalogue\';
DECLARE c2 CURSOR LOCAL FAST_FORWARD FOR
    SELECT FileId, @NewRoot + N'Manual\Catalogue\' + SUBSTRING(OldPath, CHARINDEX(@Marker, OldPath) + LEN(@Marker), 4000)
    FROM #Chk WHERE NewExists = 0 AND CHARINDEX(@Marker, OldPath) > 0;
OPEN c2;
FETCH NEXT FROM c2 INTO @Id, @P;
WHILE @@FETCH_STATUS = 0
BEGIN
    DELETE FROM #R;
    INSERT INTO #R EXEC master.sys.xp_fileexist @P;
    IF EXISTS (SELECT 1 FROM #R WHERE FileExists = 1)
        UPDATE #Chk SET NewPath = @P, NewExists = 1 WHERE FileId = @Id;
    FETCH NEXT FROM c2 INTO @Id, @P;
END
CLOSE c2;
DEALLOCATE c2;

SELECT COUNT(*) AS TotalToRemap,
       SUM(CASE WHEN NewExists = 1 THEN 1 ELSE 0 END) AS FoundInArchive,
       SUM(CASE WHEN NewExists = 1 THEN 0 ELSE 1 END) AS MissingInArchive
FROM #Chk;

SELECT FileId, DocumentId, NewPath AS MissingPath FROM #Chk WHERE NewExists <> 1 ORDER BY DocumentId;

IF @Apply = 0
    PRINT N'Dry-run only (Apply=0): nothing changed.';
ELSE
BEGIN
BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('dbo.FileEntity_PathBackup_20261010') IS NULL
        CREATE TABLE dbo.FileEntity_PathBackup_20261010 (
            FileId BIGINT NOT NULL PRIMARY KEY,
            OldPhysicalPath NVARCHAR(MAX) NOT NULL,
            NewPhysicalPath NVARCHAR(MAX) NOT NULL,
            ChangedAt DATETIME2 NOT NULL DEFAULT GETDATE()
        );

    INSERT INTO dbo.FileEntity_PathBackup_20261010 (FileId, OldPhysicalPath, NewPhysicalPath)
    SELECT c.FileId, c.OldPath, c.NewPath
    FROM #Chk c
    WHERE c.NewExists = 1
      AND NOT EXISTS (SELECT 1 FROM dbo.FileEntity_PathBackup_20261010 b WHERE b.FileId = c.FileId);

    UPDATE f
    SET f.PhysicalPath = c.NewPath
    FROM dbo.FileEntity f
    INNER JOIN #Chk c ON c.FileId = f.Id
    WHERE c.NewExists = 1;

    PRINT N'Updated rows: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'Patch failed: %s', 16, 1, @Err);
END CATCH;
END;

/* Rollback:
UPDATE f SET f.PhysicalPath = b.OldPhysicalPath
FROM dbo.FileEntity f INNER JOIN dbo.FileEntity_PathBackup_20261010 b ON b.FileId = f.Id;
*/
