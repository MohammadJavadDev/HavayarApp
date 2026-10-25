/*
Sync_SecEmploymentStatuteSpecial_FromTotalSystem.sql
همگام‌سازی Sec.EmploymentStatuteSpecial از HRM_EmplymentStatuteSpecialList.
اتصال پرسنل: اولویت Code = PrsCode، سپس HamkaranId = PersonelId.
ردیف بدون پرسنل درج نمی‌شود. تکرارپذیر روی HtsId.
CreatedByName = Gnr_User.FullName. CreatedOnShamsiDateTime = CreatedDateTimeInText.
کاربر seed ایجادکننده نیست. UTF-8 with BOM. sqlcmd -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'Sec.EmploymentStatuteSpecial', N'U') IS NULL
BEGIN
    RAISERROR(N'Sec.EmploymentStatuteSpecial وجود ندارد — ابتدا migration را اعمال کنید.', 16, 1);
    RETURN;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('tempdb..#UserMap') IS NOT NULL DROP TABLE #UserMap;
    SELECT OldUserId, NewUserId
    INTO #UserMap
    FROM (
        SELECT
            CAST(ou.User_ID AS INT) AS OldUserId,
            nu.Id AS NewUserId,
            ROW_NUMBER() OVER (
                PARTITION BY ou.User_ID
                ORDER BY
                    CASE
                        WHEN LOWER(nu.Username) = LOWER(COALESCE(
                                NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N''),
                                LTRIM(RTRIM(ou.Username))))
                        THEN 0
                        WHEN LOWER(nu.Username) = LOWER(LTRIM(RTRIM(ou.Username))) THEN 1
                        ELSE 2
                    END,
                    nu.Id
            ) AS rn
        FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
        INNER JOIN [system].[User] nu
            ON LOWER(nu.Username) = LOWER(COALESCE(
                    NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N''),
                    LTRIM(RTRIM(ou.Username))))
            OR LOWER(nu.Username) = LOWER(LTRIM(RTRIM(ou.Username)))
    ) x
    WHERE rn = 1;

    IF NOT EXISTS (SELECT 1 FROM #UserMap WHERE OldUserId = 40)
    BEGIN
        INSERT INTO #UserMap (OldUserId, NewUserId)
        SELECT 40, nu.Id
        FROM [system].[User] nu
        WHERE nu.Username = N'allahvirdi.s';
    END;
    CREATE UNIQUE CLUSTERED INDEX IX_UserMap ON #UserMap (OldUserId);

    IF OBJECT_ID('tempdb..#Src') IS NOT NULL DROP TABLE #Src;
    SELECT
        CAST(e.Id AS BIGINT) AS HtsId,
        COALESCE(pByCode.Id, pByHam.Id) AS PersonelId,
        NULLIF(LTRIM(RTRIM(e.RealEmploymentDate)), N'') AS RealEmploymentShamsiDate,
        CASE
            WHEN NULLIF(LTRIM(RTRIM(e.RealEmploymentDate)), N'') IS NULL THEN NULL
            ELSE dbo.fn_ShamsiToMiladiDate(NULLIF(LTRIM(RTRIM(e.RealEmploymentDate)), N''), N'/')
        END AS RealEmploymentMiladiDate,
        e.AdditionalYear,
        e.AdditionalMonth,
        um.NewUserId AS CreatedById,
        NULLIF(LTRIM(RTRIM(u.FullName)), N'') AS CreatedByName,
        e.CreatedDateTime AS CreatedOnMiladiDateTime,
        NULLIF(LTRIM(RTRIM(e.CreatedDateTimeInText)), N'') AS CreatedOnShamsiDateTime,
        CASE WHEN COALESCE(pByCode.Id, pByHam.Id) IS NULL THEN 1 ELSE 0 END AS Unmatched
    INTO #Src
    FROM [TMS].[TotalSystem].[dbo].[HRM_EmplymentStatuteSpecialList] e
    LEFT JOIN [TMS].[TotalSystem].[dbo].[HRM_Personel] h ON h.Personel_ID = e.PersonelId
    OUTER APPLY (
        SELECT TOP (1) p.Id
        FROM Hcm.Personel p
        WHERE h.PrsCode IS NOT NULL AND p.Code = LTRIM(RTRIM(h.PrsCode))
        ORDER BY p.Id
    ) pByCode
    OUTER APPLY (
        SELECT TOP (1) p.Id
        FROM Hcm.Personel p
        WHERE p.HamkaranId = CAST(e.PersonelId AS BIGINT)
        ORDER BY p.Id
    ) pByHam
    LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = e.CreatedUserId
    LEFT JOIN #UserMap um ON um.OldUserId = e.CreatedUserId;

    DECLARE @SourceCount INT = (SELECT COUNT(*) FROM #Src);
    DECLARE @Unmatched INT = (SELECT COUNT(*) FROM #Src WHERE Unmatched = 1);
    DECLARE @Matched INT = (SELECT COUNT(*) FROM #Src WHERE Unmatched = 0);
    PRINT N'Source: ' + CAST(@SourceCount AS nvarchar(20))
        + N' | Matched: ' + CAST(@Matched AS nvarchar(20))
        + N' | Unmatched (skipped): ' + CAST(@Unmatched AS nvarchar(20));

    SELECT HtsId, PersonelId, RealEmploymentShamsiDate
    FROM #Src
    WHERE Unmatched = 1;

    UPDATE t SET
        t.PersonelId = s.PersonelId,
        t.RealEmploymentShamsiDate = s.RealEmploymentShamsiDate,
        t.RealEmploymentMiladiDate = s.RealEmploymentMiladiDate,
        t.AdditionalYear = s.AdditionalYear,
        t.AdditionalMonth = s.AdditionalMonth,
        t.CreatedById = s.CreatedById,
        t.CreatedByName = s.CreatedByName,
        t.CreatedOnMiladiDateTime = s.CreatedOnMiladiDateTime,
        t.CreatedOnShamsiDateTime = s.CreatedOnShamsiDateTime,
        t.ModifiedById = s.CreatedById,
        t.ModifiedByName = s.CreatedByName,
        t.ModifiedDateMiladiDateTime = s.CreatedOnMiladiDateTime,
        t.ModifiedDateShamsiDateTime = s.CreatedOnShamsiDateTime,
        t.IsActive = 1
    FROM Sec.EmploymentStatuteSpecial t
    INNER JOIN #Src s ON s.HtsId = t.HtsId AND s.Unmatched = 0;
    PRINT N'  Updated: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    INSERT INTO Sec.EmploymentStatuteSpecial
        (HtsId, PersonelId, RealEmploymentShamsiDate, RealEmploymentMiladiDate, AdditionalYear, AdditionalMonth,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.PersonelId, s.RealEmploymentShamsiDate, s.RealEmploymentMiladiDate, s.AdditionalYear, s.AdditionalMonth,
        s.CreatedById, s.CreatedByName, s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime,
        s.CreatedById, s.CreatedByName, s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime, 1
    FROM #Src s
    WHERE s.Unmatched = 0
      AND NOT EXISTS (SELECT 1 FROM Sec.EmploymentStatuteSpecial t WHERE t.HtsId = s.HtsId);
    PRINT N'  Inserted: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    COMMIT TRANSACTION;

    SELECT
        (SELECT COUNT(*) FROM Sec.EmploymentStatuteSpecial) AS DestCount,
        @SourceCount AS SourceCount,
        @Matched AS Matched,
        @Unmatched AS UnmatchedSkipped;

    PRINT N'=== DONE Sync_SecEmploymentStatuteSpecial_FromTotalSystem ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH
