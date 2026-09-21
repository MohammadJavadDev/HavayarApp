/*
  SyncAfterSales_ServiceRequestCreatedByFromTotalSystem.sql
  Sale.ServiceRequest creator audit from HTS Sale_ServiceRequest:
    CreatedUser_FK -> CreatedById / CreatedByName (Gnr_User.FullName)
    CreatedDate + CreatedTime -> CreatedOnShamsiDateTime / CreatedOnMiladiDateTime
    CreatedOrgUnit_FK -> CreatedOrgUnitId (Hrm.OrgUnit.Title = HRM_OrgUnit.OrgUnit_Title)
  Only HtsId <> 0. Idempotent. Encoding: UTF-8 with BOM.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1);
    RETURN;
END;

BEGIN TRY
BEGIN TRANSACTION;

IF OBJECT_ID('tempdb..#UserMap') IS NOT NULL DROP TABLE #UserMap;
SELECT CAST(ou.User_ID AS BIGINT) AS OldUserId, w.Id AS NewUserId,
       LEFT(COALESCE(NULLIF(LTRIM(RTRIM(ou.FullName)), N''), NULLIF(LTRIM(RTRIM(w.NameFa)), N''), NULLIF(LTRIM(RTRIM(w.Name)), N''), w.Username), 150) AS NewUserName
INTO #UserMap
FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
CROSS APPLY (
    SELECT TOP 1 nu.Id, nu.NameFa, nu.Name, nu.Username
    FROM [system].[User] nu
    WHERE LOWER(nu.Username) = LOWER(LTRIM(RTRIM(ou.Username)))
       OR (NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N'') IS NOT NULL
           AND LOWER(nu.Username) = LOWER(LTRIM(RTRIM(ou.ActiveDirectoryUsername))))
       OR (NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N'') IS NOT NULL
           AND LOWER(REPLACE(ISNULL(nu.Email, N''), N'@havayar.com', N'')) = LOWER(LTRIM(RTRIM(ou.ActiveDirectoryUsername))))
       OR LOWER(REPLACE(ISNULL(nu.Email, N''), N'@havayar.com', N'')) = LOWER(LTRIM(RTRIM(ou.Username)))
    ORDER BY CASE WHEN nu.IsActive = 1 THEN 0 ELSE 1 END, nu.Id
) w;

PRINT N'UserMap rows: ' + CAST((SELECT COUNT(*) FROM #UserMap) AS nvarchar(20));

UPDATE sr
SET
    sr.CreatedById = um.NewUserId,
    sr.CreatedByName = LEFT(COALESCE(NULLIF(LTRIM(RTRIM(htsUser.FullName)), N''), um.NewUserName, sr.CreatedByName), 150),
    sr.CreatedOnShamsiDateTime = COALESCE(NULLIF(LTRIM(RTRIM(cd.CreatedShamsi)), N''), sr.CreatedOnShamsiDateTime),
    sr.CreatedOnMiladiDateTime = COALESCE(cd.CreatedMiladi, sr.CreatedOnMiladiDateTime),
    sr.CreatedOrgUnitId = COALESCE(org.Id, sr.CreatedOrgUnitId)
FROM Sale.ServiceRequest sr
INNER JOIN [TMS].[TotalSystem].[dbo].[Sale_ServiceRequest] s
    ON sr.HtsId = CAST(s.ServiceRequest_ID AS BIGINT) AND sr.HtsId <> 0
LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] htsUser ON htsUser.User_ID = s.CreatedUser_FK
LEFT JOIN #UserMap um ON um.OldUserId = CAST(s.CreatedUser_FK AS BIGINT)
LEFT JOIN [TMS].[TotalSystem].[dbo].[HRM_OrgUnit] hou ON hou.OrgUnit_ID = s.CreatedOrgUnit_FK
OUTER APPLY (
    SELECT
        LEFT(LTRIM(RTRIM(s.CreatedDate)) + CASE WHEN NULLIF(LTRIM(RTRIM(s.CreatedTime)), N'') IS NULL THEN N'' ELSE N' ' + LTRIM(RTRIM(s.CreatedTime)) END, 30) AS CreatedShamsi,
        COALESCE(
            dbo.fn_ShamsiToMiladiDateTime(s.CreatedDate, NULLIF(LTRIM(RTRIM(s.CreatedTime)), N''), N'/', N':'),
            CAST(dbo.fn_ShamsiToMiladiDate(s.CreatedDate, N'/') AS DATETIME2)
        ) AS CreatedMiladi
) cd
OUTER APPLY (
    SELECT TOP (1) o.Id
    FROM Hrm.OrgUnit o
    WHERE hou.OrgUnit_Title IS NOT NULL
      AND LTRIM(RTRIM(o.Title)) = LTRIM(RTRIM(hou.OrgUnit_Title))
    ORDER BY o.Id
) org;

PRINT N'ServiceRequest creator rows updated: ' + CAST(@@ROWCOUNT AS nvarchar(20));

COMMIT TRANSACTION;
PRINT N'ServiceRequest CreatedBy sync committed';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

PRINT N'--- creator name sample ---';
SELECT TOP 15 sr.Id, sr.HtsId, sr.CreatedById, sr.CreatedByName, sr.CreatedOnShamsiDateTime, sr.CreatedOrgUnitId
FROM Sale.ServiceRequest sr
WHERE sr.HtsId <> 0
ORDER BY sr.Id;

PRINT N'--- creator name histogram ---';
SELECT TOP 20 CreatedByName, COUNT(*) AS Cnt
FROM Sale.ServiceRequest
GROUP BY CreatedByName
ORDER BY Cnt DESC;

PRINT N'--- mapping coverage ---';
SELECT
    COUNT(*) AS Total,
    SUM(CASE WHEN CreatedByName = N'sync-after-sales-phase5' THEN 1 ELSE 0 END) AS StillSeed,
    SUM(CASE WHEN CreatedById IS NULL THEN 1 ELSE 0 END) AS NullUserId,
    SUM(CASE WHEN CreatedOrgUnitId IS NULL THEN 1 ELSE 0 END) AS NullOrg
FROM Sale.ServiceRequest
WHERE HtsId <> 0;
