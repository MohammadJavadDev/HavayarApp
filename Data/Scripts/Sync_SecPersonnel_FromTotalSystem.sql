/*
Sync_SecPersonnel_FromTotalSystem.sql
همگام‌سازی Sec.Personnel از [TMS].[TotalSystem].[dbo].[Sec_Personnel].
اتصال به Hcm.Personel: اولویت با Code = HRM.PrsCode، سپس HamkaranId = Personel_FK.
اگر هیچ‌کدام match نشد، ردیف با PersonelId = NULL درج می‌شود (جعل Personel ممنوع).
SendToEveryOne=1 → ReciversGroupId=3 (همه)؛ 0 و NULL → NULL.
Idempotent روی HtsId. UTF-8 with BOM. sqlcmd -f 65001
Requires: Seed_SecPersonnel_Lookup.sql
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'Sec.Personnel', N'U') IS NULL
BEGIN
    RAISERROR(N'Sec.Personnel وجود ندارد — ابتدا migration را اعمال کنید.', 16, 1);
    RETURN;
END;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @Seed NVARCHAR(80) = N'sync-sec-personnel';

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('tempdb..#Src') IS NOT NULL DROP TABLE #Src;
    SELECT
        CAST(s.Personel_ID AS BIGINT) AS HtsId,
        CAST(s.Personel_FK AS BIGINT) AS OldPersonelFk,
        h.PrsCode,
        COALESCE(pByCode.Id, pByFk.Id) AS PersonelId,
        LTRIM(RTRIM(s.PrsName_Display)) AS NameDisplay,
        LTRIM(RTRIM(s.PrsFamily_Display)) AS FamilyDisplay,
        LTRIM(RTRIM(s.BirthDate)) AS BirthDate,
        LTRIM(RTRIM(s.EmploymentDate)) AS EmploymentDate,
        s.Picture,
        s.SecondPicture,
        CAST(CASE WHEN s.IsActive = 1 THEN 1 ELSE 0 END AS INT) AS IsActiveVal,
        CAST(s.ForceToSend AS BIT) AS ForceToSend,
        CASE WHEN s.SendToEveryOne = 1 THEN CAST(3 AS BIGINT) ELSE NULL END AS ReciversGroupId,
        CASE WHEN COALESCE(pByCode.Id, pByFk.Id) IS NULL THEN 1 ELSE 0 END AS Unmatched
    INTO #Src
    FROM [TMS].[TotalSystem].[dbo].[Sec_Personnel] s
    LEFT JOIN [TMS].[TotalSystem].[dbo].[HRM_Personel] h ON h.Personel_ID = s.Personel_FK
    LEFT JOIN Hcm.Personel pByCode ON h.PrsCode IS NOT NULL AND pByCode.Code = LTRIM(RTRIM(h.PrsCode))
    LEFT JOIN Hcm.Personel pByFk ON pByFk.HamkaranId = CAST(s.Personel_FK AS BIGINT);

    DECLARE @Unmatched INT = (SELECT COUNT(*) FROM #Src WHERE Unmatched = 1);
    DECLARE @Matched INT = (SELECT COUNT(*) FROM #Src WHERE Unmatched = 0);
    DECLARE @SourceCount INT = (SELECT COUNT(*) FROM #Src);
    PRINT N'Source: ' + CAST(@SourceCount AS nvarchar(20))
        + N' | Matched Personel: ' + CAST(@Matched AS nvarchar(20))
        + N' | Unmatched (PersonelId NULL): ' + CAST(@Unmatched AS nvarchar(20));

    SELECT TOP 50 HtsId, OldPersonelFk, PrsCode, NameDisplay, FamilyDisplay
    FROM #Src WHERE Unmatched = 1
    ORDER BY HtsId;

    -- به‌روزرسانی همه ردیف‌های موجود (با یا بدون Personel)
    UPDATE t SET
        t.PersonelId = s.PersonelId,
        t.NameDisplay = s.NameDisplay,
        t.FamilyDisplay = s.FamilyDisplay,
        t.BirthDate = s.BirthDate,
        t.EmploymentDate = s.EmploymentDate,
        t.Picture = s.Picture,
        t.SecondPicture = s.SecondPicture,
        t.ForceToSend = s.ForceToSend,
        t.ReciversGroupId = s.ReciversGroupId,
        t.IsActive = s.IsActiveVal,
        t.ModifiedById = 1,
        t.ModifiedByName = @Seed,
        t.ModifiedDateMiladiDateTime = @Now,
        t.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Sec.Personnel t
    INNER JOIN #Src s ON s.HtsId = t.HtsId;
    PRINT N'  Personnel updated: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    -- درج همه ردیف‌های جدید، از جمله Unmatched با PersonelId = NULL
    INSERT INTO Sec.Personnel
        (HtsId, PersonelId, NameDisplay, FamilyDisplay, BirthDate, EmploymentDate,
         Picture, SecondPicture, ForceToSend, ReciversGroupId,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.PersonelId, s.NameDisplay, s.FamilyDisplay, s.BirthDate, s.EmploymentDate,
        s.Picture, s.SecondPicture, s.ForceToSend, s.ReciversGroupId,
        1, @Seed, @Now, @NowShamsi, 1, @Seed, @Now, @NowShamsi, s.IsActiveVal
    FROM #Src s
    WHERE NOT EXISTS (SELECT 1 FROM Sec.Personnel t WHERE t.HtsId = s.HtsId);
    PRINT N'  Personnel inserted: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    COMMIT TRANSACTION;

    SELECT
        (SELECT COUNT(*) FROM Sec.Personnel) AS SecPersonnelCount,
        @SourceCount AS SourceCount,
        @Matched AS MatchedWithPersonel,
        @Unmatched AS UnmatchedNullPersonelId,
        (SELECT COUNT(*) FROM Sec.Personnel WHERE PersonelId IS NULL) AS DestNullPersonelId;

    PRINT N'=== DONE Sync_SecPersonnel_FromTotalSystem ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH
