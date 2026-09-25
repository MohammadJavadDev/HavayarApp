/*
Sync_CngPartPrice_FromTotalSystem.sql
همگام‌سازی Cng.PartPrice از [TMS].[TotalSystem].[dbo].[Sale_CngPartPrice] روی HtsId.
Part_FK → Inv.Part.HtsId. پیوست‌های قدیمی منتقل نمی‌شوند.
UTF-8 BOM. sqlcmd -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'Cng.PartPrice', N'U') IS NULL
BEGIN
    RAISERROR(N'جدول Cng.PartPrice وجود ندارد. ابتدا migration AddCngPartPrice را اعمال کنید.', 16, 1);
    RETURN;
END;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = dbo.ToShamsiDate(CAST(@Now AS DATE), '/');
DECLARE @Seed NVARCHAR(80) = N'sync-cng-partprice';

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('tempdb..#Src') IS NOT NULL DROP TABLE #Src;
    SELECT
        CAST(s.CngPartPrice_ID AS BIGINT) AS HtsId,
        p.Id AS PartId,
        CAST(s.Part_FK AS BIGINT) AS HtsPartId,
        CAST(s.Price AS BIGINT) AS Price,
        s.Comment,
        s.Description,
        CAST(s.UpdatedDate AS datetime2) AS UpdatedDate,
        NULLIF(LTRIM(RTRIM(s.UpdatedDate_Shamsi)), N'') AS UpdatedDateShamsi,
        CAST(s.IsInternal AS BIT) AS IsInternal,
        CAST(s.IsExternal AS BIT) AS IsExternal,
        CASE WHEN p.Id IS NULL THEN 1 ELSE 0 END AS UnmappedPart
    INTO #Src
    FROM [TMS].[TotalSystem].[dbo].[Sale_CngPartPrice] s
    LEFT JOIN Inv.Part p ON p.HtsId = CAST(s.Part_FK AS BIGINT) AND ISNULL(p.HtsId, 0) <> 0;

    DECLARE @Unmapped INT = (SELECT COUNT(*) FROM #Src WHERE UnmappedPart = 1);
    IF @Unmapped > 0
        PRINT N'  WARN unmapped Part_FK rows skipped: ' + CAST(@Unmapped AS nvarchar(20));

    UPDATE t SET
        t.PartId = s.PartId,
        t.Price = s.Price,
        t.Comment = s.Comment,
        t.Description = s.Description,
        t.UpdatedDate = s.UpdatedDate,
        t.UpdatedDateShamsi = LEFT(COALESCE(s.UpdatedDateShamsi, t.UpdatedDateShamsi), 10),
        t.IsInternal = s.IsInternal,
        t.IsExternal = s.IsExternal,
        t.ModifiedById = 1,
        t.ModifiedByName = @Seed,
        t.ModifiedDateMiladiDateTime = @Now,
        t.ModifiedDateShamsiDateTime = @NowShamsi,
        t.IsActive = 1
    FROM Cng.PartPrice t
    INNER JOIN #Src s ON s.HtsId = t.HtsId
    WHERE s.PartId IS NOT NULL;
    PRINT N'  PartPrice updated: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    INSERT INTO Cng.PartPrice
        (HtsId, PartId, Price, Comment, Description, UpdatedDate, UpdatedDateShamsi, IsInternal, IsExternal,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.PartId, s.Price, s.Comment, s.Description, s.UpdatedDate, LEFT(s.UpdatedDateShamsi, 10),
        s.IsInternal, s.IsExternal,
        1, @Seed, @Now, @NowShamsi,
        1, @Seed, @Now, @NowShamsi, 1
    FROM #Src s
    WHERE s.PartId IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM Cng.PartPrice t WHERE t.HtsId = s.HtsId);
    PRINT N'  PartPrice inserted: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Sync_CngPartPrice_FromTotalSystem ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT COUNT(*) AS PartPriceCount FROM Cng.PartPrice;
SELECT COUNT(*) AS AttachmentCount FROM Cng.PartPriceAttachment;
SELECT COUNT(*) AS HtsSourceCount FROM [TMS].[TotalSystem].[dbo].[Sale_CngPartPrice];
