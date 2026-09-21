/*
  Fix_RepairRequest_DlAndAuditFromHts.sql
  Tafsil (DlId) was empty because Phase8/8b joined FIN.DL.HamkaranId = HTS DL_FK.
  HTS DL_FK is Acc_DL.Acc_DL_ID. The HTS grid shows Acc_DLTitle.
  Correct remap: Acc_DL.Acc_DL_ID = DL_FK, then FIN.DL.Title = Acc_DLTitle
  (Code = Hamkaran_Acc_DL_FK as tie-break). Do NOT use HamkaranId = DL_FK
  (false positives) or HamkaranId = Hamkaran_Acc_DL_FK (Rahkaran SD* title).

  Also overwrite seed audit with HTS CreatedUser_FK / CreatedDate+Time /
  UpdatedUserId / UpdatedDateTime|UpdatedDateInText. Users via Username / AD / email.
  Encoding: UTF-8 with BOM. Apply: sqlcmd -C -b -I -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1);
    RETURN;
END;

PRINT N'=== BEFORE RepairRequest DL + audit ===';
SELECT
    COUNT(*) AS Total,
    SUM(CASE WHEN ISNULL(DlId,0) <> 0 THEN 1 ELSE 0 END) AS DlFilled,
    SUM(CASE WHEN CreatedByName LIKE N'sync-%' OR CreatedByName LIKE N'fix-%' THEN 1 ELSE 0 END) AS CreatorSeed,
    SUM(CASE WHEN ModifiedByName LIKE N'sync-%' OR ModifiedByName LIKE N'fix-%' THEN 1 ELSE 0 END) AS EditorSeed,
    SUM(CASE WHEN CreatedOnShamsiDateTime LIKE N'[12][0-9][0-9][0-9]/[0-1][0-9]/%' THEN 1 ELSE 0 END) AS CreatedShamsiOk,
    SUM(CASE WHEN ModifiedDateShamsiDateTime LIKE N'[12][0-9][0-9][0-9]/[0-1][0-9]/%' THEN 1 ELSE 0 END) AS ModifiedShamsiOk
FROM Rpr.RepairRequest;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('tempdb..#UserMap') IS NOT NULL DROP TABLE #UserMap;
    SELECT CAST(ou.User_ID AS BIGINT) AS OldUserId,
           w.Id AS NewUserId,
           LEFT(COALESCE(NULLIF(LTRIM(RTRIM(w.NameFa)), N''), NULLIF(LTRIM(RTRIM(w.Name)), N''), w.Username), 150) AS NewUserName
    INTO #UserMap
    FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
    CROSS APPLY (
        SELECT TOP 1 nu.Id, nu.NameFa, nu.Name, nu.Username
        FROM [system].[User] nu
        WHERE LOWER(nu.Username) = LOWER(LTRIM(RTRIM(ou.Username)))
           OR (
                NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N'') IS NOT NULL
                AND LOWER(nu.Username) = LOWER(LTRIM(RTRIM(ou.ActiveDirectoryUsername)))
           )
           OR (
                NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N'') IS NOT NULL
                AND LOWER(REPLACE(ISNULL(nu.Email, N''), N'@havayar.com', N'')) = LOWER(LTRIM(RTRIM(ou.ActiveDirectoryUsername)))
           )
           OR LOWER(REPLACE(ISNULL(nu.Email, N''), N'@havayar.com', N'')) = LOWER(LTRIM(RTRIM(ou.Username)))
        ORDER BY CASE WHEN nu.IsActive = 1 THEN 0 ELSE 1 END, nu.Id
    ) w;

    PRINT N'=== [1] Rematch DlId via Acc_DLTitle + sync HTS creator/editor/dates ===';
    UPDATE t SET
        t.DlId = dl.Id,
        t.CreatedById = umc.NewUserId,
        t.CreatedByName = umc.NewUserName,
        t.CreatedOnMiladiDateTime = cd.CreatedMiladi,
        t.CreatedOnShamsiDateTime = cd.CreatedShamsi,
        t.ModifiedById = ume.NewUserId,
        t.ModifiedByName = ume.NewUserName,
        t.ModifiedDateMiladiDateTime = md.ModifiedMiladi,
        t.ModifiedDateShamsiDateTime = md.ModifiedShamsi
    FROM Rpr.RepairRequest t
    INNER JOIN [TMS].[TotalSystem].[dbo].[Rpr_RepairRequest] s ON t.HtsId = s.RepairRequest_ID
    LEFT JOIN #UserMap umc ON umc.OldUserId = s.CreatedUser_FK
    LEFT JOIN #UserMap ume ON ume.OldUserId = s.UpdatedUserId
    OUTER APPLY (
        SELECT TOP 1 d.Id
        FROM [TMS].[TotalSystem].[dbo].[Acc_DL] ad
        INNER JOIN FIN.DL d ON LTRIM(RTRIM(d.Title)) = LTRIM(RTRIM(ad.Acc_DLTitle))
        WHERE ad.Acc_DL_ID = s.DL_FK
        ORDER BY CASE WHEN d.Code = CAST(ad.Hamkaran_Acc_DL_FK AS nvarchar(64)) THEN 0 ELSE 1 END, d.Id
    ) dl
    CROSS APPLY (
        SELECT
            LEFT(
                LTRIM(RTRIM(s.CreatedDate))
                + CASE WHEN NULLIF(LTRIM(RTRIM(s.CreatedTime)), N'') IS NULL THEN N''
                       ELSE N' ' + LTRIM(RTRIM(s.CreatedTime)) END,
                30
            ) AS CreatedShamsi,
            COALESCE(
                dbo.fn_ShamsiToMiladiDateTime(s.CreatedDate, NULLIF(LTRIM(RTRIM(s.CreatedTime)), N''), N'/', N':'),
                CAST(dbo.fn_ShamsiToMiladiDate(s.CreatedDate, N'/') AS DATETIME2)
            ) AS CreatedMiladi
    ) cd
    CROSS APPLY (
        SELECT
            LEFT(COALESCE(NULLIF(LTRIM(RTRIM(s.UpdatedDateInText)), N''), cd.CreatedShamsi), 30) AS ModifiedShamsi,
            COALESCE(
                CAST(s.UpdatedDateTime AS DATETIME2),
                CASE
                    WHEN s.UpdatedDateInText LIKE N'____/__/__ __:__%'
                        THEN dbo.fn_ShamsiToMiladiDateTime(LEFT(s.UpdatedDateInText, 10), SUBSTRING(s.UpdatedDateInText, 12, 5), N'/', N':')
                    WHEN NULLIF(LTRIM(RTRIM(s.UpdatedDateInText)), N'') IS NOT NULL
                        THEN CAST(dbo.fn_ShamsiToMiladiDate(LEFT(LTRIM(RTRIM(s.UpdatedDateInText)), 10), N'/') AS DATETIME2)
                END,
                cd.CreatedMiladi
            ) AS ModifiedMiladi
    ) md;

    PRINT N'  Rows updated: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Fix_RepairRequest_DlAndAuditFromHts ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

PRINT N'=== AFTER RepairRequest DL + audit ===';
SELECT
    COUNT(*) AS Total,
    SUM(CASE WHEN ISNULL(DlId,0) <> 0 THEN 1 ELSE 0 END) AS DlFilled,
    SUM(CASE WHEN CreatedById IS NOT NULL AND CreatedById <> 1 THEN 1 ELSE 0 END) AS CreatorRemapped,
    SUM(CASE WHEN ModifiedById IS NOT NULL AND ModifiedById <> 1 THEN 1 ELSE 0 END) AS EditorRemapped,
    SUM(CASE WHEN CreatedOnShamsiDateTime LIKE N'[12][0-9][0-9][0-9]/[0-1][0-9]/%' THEN 1 ELSE 0 END) AS CreatedShamsiOk,
    SUM(CASE WHEN ModifiedDateShamsiDateTime LIKE N'[12][0-9][0-9][0-9]/[0-1][0-9]/%' THEN 1 ELSE 0 END) AS ModifiedShamsiOk
FROM Rpr.RepairRequest;

SELECT rr.IdNumber, rr.HtsId, rr.DlId, dl.Title AS Tafsil, rr.CreatedByName, rr.CreatedOnShamsiDateTime,
       rr.ModifiedByName, rr.ModifiedDateShamsiDateTime
FROM Rpr.RepairRequest rr
LEFT JOIN FIN.DL dl ON dl.Id = rr.DlId
WHERE rr.IdNumber IN (N'7017', N'7016', N'7015')
ORDER BY rr.IdNumber DESC;

SELECT COUNT(*) AS LeftoverDlNull
FROM Rpr.RepairRequest rr
INNER JOIN [TMS].[TotalSystem].[dbo].[Rpr_RepairRequest] s ON s.RepairRequest_ID = rr.HtsId
WHERE rr.DlId IS NULL AND s.DL_FK IS NOT NULL;
