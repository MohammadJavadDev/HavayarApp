/*
Sync_EpmsProposal_FromTotalSystem.sql
One-time / idempotent sync: Epms_Proposal (header only, via [TMS]) -> Epms.Proposal

NOT included: attachments, equipment, VPIS, documents, inquiry, log of a proposal.
Fields that Epms.Proposal does not have are not copied:
  Tender_FK, Action_Priority, ProposalDate(_Shamsi), ProposalSecondStatus.

HtsId = Epms_Proposal.Proposal_ID

Mappings:
  CreatedById / ModifiedById : Gnr_User -> system.User (ActiveDirectoryUsername / Username). Unmapped -> NULL.
  RequestedOrgUnitId         : HRM_OrgUnit.Hamkaran_Unit_FK -> Hrm.OrgUnit.HamkaranUnitId only.
                               Required column: a proposal whose unit cannot be mapped is SKIPPED and listed
                               (never given another unit).
  SalesManagerId / SalesSupervisorId / SalesExpertIds (CSV):
                               HTS stores HRM_Personel ids; Portal stores system.User ids.
                               personel -> Gnr_User.Personel_FK -> system.User. Unmapped ids are dropped from
                               SalesExpertIds (names stay in SalesExperts) or NULL for manager / supervisor.
  PartyIndustryId            : Crm_IndustryHdr.HamkaranIndustryhdr_FK -> Gnr.PartyIndustry.HamkaranId. Unmapped -> NULL.
  VpisTypeId                 : Epms.VpisType.HtsId (run Sync_EpmsVpisType_FromTotalSystem.sql first).
  ProposalRequest / Status   : copied as is (same lookup ids as the Portal enums); NULL stays NULL.
  StartShamsiDate / TenderShamsiDate : StartDateInText / TenderDateInText as is.
  Dates created              : CreatedDate + CreatedTime; Shamsi = CreatedDate_Shamsi + time.

Idempotent UPDATE-then-INSERT on HtsId. Never deletes target rows.
Portal-only rows (HtsId = 0) are never overwritten.
UTF-8 with BOM. sqlcmd -S PortalSRV\PORTAL -d HavayarApp -C -b -I -f 65001 -i ...
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] not found.', 16, 1);
    RETURN;
END;

IF COL_LENGTH(N'Epms.Proposal', N'HtsId') IS NULL OR COL_LENGTH(N'Epms.VpisType', N'HtsId') IS NULL
BEGIN
    RAISERROR(N'HtsId columns missing - apply migration AddActivityHtsIdAndDocumentPreparedUser first.', 16, 1);
    RETURN;
END;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = dbo.ToShamsiDate(CAST(@Now AS DATE), N'/') + N' ' + CONVERT(VARCHAR(8), @Now, 108);
DECLARE @Seed NVARCHAR(80) = N'sync-epms-proposal';

BEGIN TRY
    BEGIN TRANSACTION;

    /* ========== User map (HTS User_ID -> system.User) ========== */
    IF OBJECT_ID('tempdb..#UserMap') IS NOT NULL DROP TABLE #UserMap;
    SELECT OldUserId, OldPersonelId, NewUserId, NewUserName
    INTO #UserMap
    FROM (
        SELECT
            CAST(ou.User_ID AS INT) AS OldUserId,
            CAST(ou.Personel_FK AS INT) AS OldPersonelId,
            nu.Id AS NewUserId,
            LEFT(COALESCE(NULLIF(LTRIM(RTRIM(nu.NameFa)), N''), nu.Name, nu.Username), 150) AS NewUserName,
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
    CREATE UNIQUE CLUSTERED INDEX IX_UserMap ON #UserMap (OldUserId);

    /* ========== Personel map (HRM_Personel_ID -> system.User): only when exactly one Portal user ========== */
    IF OBJECT_ID('tempdb..#PersMap') IS NOT NULL DROP TABLE #PersMap;
    SELECT OldPersonelId, MIN(NewUserId) AS NewUserId
    INTO #PersMap
    FROM #UserMap
    WHERE OldPersonelId IS NOT NULL
    GROUP BY OldPersonelId
    HAVING COUNT(DISTINCT NewUserId) = 1;
    CREATE UNIQUE CLUSTERED INDEX IX_PersMap ON #PersMap (OldPersonelId);

    /* ========== OrgUnit map: Hamkaran_Unit_FK only ========== */
    IF OBJECT_ID('tempdb..#OrgUnitMap') IS NOT NULL DROP TABLE #OrgUnitMap;
    SELECT
        hrm.OrgUnit_ID AS OldOrgUnitId,
        byHam.Id AS NewOrgUnitId,
        NULLIF(LTRIM(RTRIM(hrm.OrgUnit_Title)), N'') AS HtsTitle
    INTO #OrgUnitMap
    FROM [TMS].[TotalSystem].[dbo].[HRM_OrgUnit] hrm
    OUTER APPLY (
        SELECT TOP (1) o.Id
        FROM Hrm.OrgUnit o
        WHERE hrm.Hamkaran_Unit_FK IS NOT NULL
          AND ISNULL(hrm.Hamkaran_Unit_FK, 0) <> 0
          AND o.HamkaranUnitId = CAST(hrm.Hamkaran_Unit_FK AS BIGINT)
        ORDER BY o.Id
    ) byHam;
    CREATE UNIQUE CLUSTERED INDEX IX_OrgUnitMap ON #OrgUnitMap (OldOrgUnitId);

    /* ========== Sales experts: split personel CSV, map, re-join in original order ========== */
    IF OBJECT_ID('tempdb..#Experts') IS NOT NULL DROP TABLE #Experts;
    SELECT
        p.Proposal_ID,
        ExpertsUserIds = STRING_AGG(CAST(pm.NewUserId AS NVARCHAR(20)), N',') WITHIN GROUP (ORDER BY sp.ordinal),
        TotalExperts = COUNT(*),
        UnmappedExperts = SUM(CASE WHEN pm.NewUserId IS NULL THEN 1 ELSE 0 END)
    INTO #Experts
    FROM [TMS].[TotalSystem].[dbo].[Epms_Proposal] p
    CROSS APPLY STRING_SPLIT(p.SalesExpertIds, N',', 1) sp
    LEFT JOIN #PersMap pm ON pm.OldPersonelId = TRY_CAST(LTRIM(RTRIM(sp.value)) AS INT)
    WHERE NULLIF(LTRIM(RTRIM(sp.value)), N'') IS NOT NULL
    GROUP BY p.Proposal_ID;

    /* ========== Staging ========== */
    IF OBJECT_ID('tempdb..#Src') IS NOT NULL DROP TABLE #Src;
    SELECT
        CAST(p.Proposal_ID AS BIGINT) AS HtsId,
        ISNULL(LTRIM(RTRIM(p.Proposal_Title)), N'') AS Title,
        NULLIF(LTRIM(RTRIM(p.Proposal_Code)), N'') AS Code,
        oum.NewOrgUnitId AS RequestedOrgUnitId,
        p.RequestedOrgUnit_FK AS OldOrgUnitId,
        NULLIF(LTRIM(RTRIM(p.SalesExperts)), N'') AS SalesExperts,
        ex.ExpertsUserIds AS SalesExpertIds,
        ind.Id AS PartyIndustryId,
        CAST(p.ProposalRequest_FK AS INT) AS ProposalRequest,
        CAST(p.Proposal_Status_FK AS INT) AS Status,
        NULLIF(LTRIM(RTRIM(p.Comment)), N'') AS Comment,
        NULLIF(LTRIM(RTRIM(p.TenderName)), N'') AS TenderName,
        mgr.NewUserId AS SalesManagerId,
        sup.NewUserId AS SalesSupervisorId,
        CAST(p.StartDate AS DATETIME2) AS StartMiladiDate,
        NULLIF(LEFT(LTRIM(RTRIM(p.StartDateInText)), 10), N'') AS StartShamsiDate,
        CAST(p.TenderDate AS DATETIME2) AS TenderMiladiDate,
        NULLIF(LEFT(LTRIM(RTRIM(p.TenderDateInText)), 10), N'') AS TenderShamsiDate,
        CAST(ISNULL(p.IsSendToMetre, 0) AS BIT) AS IsSendToMetre,
        vt.Id AS VpisTypeId,
        CAST(ISNULL(p.IsForProductEngineeringDepartment, 0) AS BIT) AS IsForProductEngineeringDepartment,
        CAST(ISNULL(p.HasGeneralPackage, 0) AS BIT) AS HasGeneralPackage,
        ISNULL(p.Revision, 0) AS Revision,
        ISNULL(LTRIM(RTRIM(p.RevisionDescription)), N'') AS RevisionDescription,
        cu.NewUserId AS CreatedById,
        cu.NewUserName AS CreatedByName,
        DATEADD(MINUTE,
            ISNULL(DATEDIFF(MINUTE, CAST('00:00' AS TIME), TRY_CAST(LTRIM(RTRIM(p.CreatedTime)) AS TIME)), 0),
            CAST(p.CreatedDate AS DATETIME2)) AS CreatedOnMiladiDateTime,
        CASE WHEN LEN(LTRIM(RTRIM(p.CreatedDate_Shamsi))) = 10
             THEN LTRIM(RTRIM(p.CreatedDate_Shamsi)) + N' ' + ISNULL(LTRIM(RTRIM(p.CreatedTime)), N'00:00') + N':00' END AS CreatedOnShamsiDateTime,
        uu.NewUserId AS ModifiedById,
        uu.NewUserName AS ModifiedByName,
        CAST(p.UpdatedDateTime AS DATETIME2) AS ModifiedDateMiladiDateTime,
        NULLIF(LEFT(LTRIM(RTRIM(p.UpdatedDateInText)), 16), N'') AS ModifiedDateShamsiDateTime,
        ISNULL(ex.UnmappedExperts, 0) AS UnmappedExperts,
        CASE WHEN p.SalesManagerId IS NOT NULL AND mgr.NewUserId IS NULL THEN 1 ELSE 0 END AS UnmappedManager,
        CASE WHEN p.SalesSupervisorId IS NOT NULL AND sup.NewUserId IS NULL THEN 1 ELSE 0 END AS UnmappedSupervisor,
        CASE WHEN p.IndustryHdr_FK IS NOT NULL AND ind.Id IS NULL THEN 1 ELSE 0 END AS UnmappedIndustry,
        CASE WHEN p.VpisTypeId IS NOT NULL AND vt.Id IS NULL THEN 1 ELSE 0 END AS UnmappedVpisType
    INTO #Src
    FROM [TMS].[TotalSystem].[dbo].[Epms_Proposal] p
    LEFT JOIN #OrgUnitMap oum ON oum.OldOrgUnitId = p.RequestedOrgUnit_FK
    LEFT JOIN #Experts ex ON ex.Proposal_ID = p.Proposal_ID
    LEFT JOIN #PersMap mgr ON mgr.OldPersonelId = p.SalesManagerId
    LEFT JOIN #PersMap sup ON sup.OldPersonelId = p.SalesSupervisorId
    LEFT JOIN #UserMap cu ON cu.OldUserId = p.CreatedUser_FK
    LEFT JOIN #UserMap uu ON uu.OldUserId = p.UpdatedUserId
    LEFT JOIN [TMS].[TotalSystem].[dbo].[Crm_IndustryHdr] ih ON ih.Industryhdr_ID = p.IndustryHdr_FK
    LEFT JOIN Gnr.PartyIndustry ind ON ind.HamkaranId = ih.HamkaranIndustryhdr_FK AND ih.HamkaranIndustryhdr_FK IS NOT NULL
    LEFT JOIN Epms.VpisType vt ON vt.HtsId = p.VpisTypeId AND vt.HtsId <> 0;

    IF EXISTS (SELECT 1 FROM #Src WHERE LEN(Title) > 500 OR LEN(Code) > 500 OR LEN(Comment) > 1000)
        THROW 51702, N'A proposal Title/Code is longer than 500 chars or Comment longer than 1000 chars.', 1;

    PRINT N'--- Skipped (RequestedOrgUnit not mappable) ---';
    SELECT HtsId, Code, Title, OldOrgUnitId FROM #Src WHERE RequestedOrgUnitId IS NULL;

    DELETE FROM #Src WHERE RequestedOrgUnitId IS NULL;

    UPDATE t SET
        t.Title = s.Title,
        t.Code = s.Code,
        t.RequestedOrgUnitId = s.RequestedOrgUnitId,
        t.SalesExperts = s.SalesExperts,
        t.SalesExpertIds = s.SalesExpertIds,
        t.PartyIndustryId = s.PartyIndustryId,
        t.ProposalRequest = s.ProposalRequest,
        t.Status = s.Status,
        t.Comment = s.Comment,
        t.TenderName = s.TenderName,
        t.SalesManagerId = s.SalesManagerId,
        t.SalesSupervisorId = s.SalesSupervisorId,
        t.StartMiladiDate = s.StartMiladiDate,
        t.StartShamsiDate = s.StartShamsiDate,
        t.TenderMiladiDate = s.TenderMiladiDate,
        t.TenderShamsiDate = s.TenderShamsiDate,
        t.IsSendToMetre = s.IsSendToMetre,
        t.VpisTypeId = s.VpisTypeId,
        t.IsForProductEngineeringDepartment = s.IsForProductEngineeringDepartment,
        t.HasGeneralPackage = s.HasGeneralPackage,
        t.Revision = s.Revision,
        t.RevisionDescription = s.RevisionDescription,
        t.CreatedById = s.CreatedById,
        t.CreatedByName = s.CreatedByName,
        t.CreatedOnMiladiDateTime = s.CreatedOnMiladiDateTime,
        t.CreatedOnShamsiDateTime = s.CreatedOnShamsiDateTime,
        t.ModifiedById = s.ModifiedById,
        t.ModifiedByName = ISNULL(s.ModifiedByName, @Seed),
        t.ModifiedDateMiladiDateTime = ISNULL(s.ModifiedDateMiladiDateTime, s.CreatedOnMiladiDateTime),
        t.ModifiedDateShamsiDateTime = ISNULL(s.ModifiedDateShamsiDateTime, s.CreatedOnShamsiDateTime),
        t.IsActive = 1
    FROM Epms.Proposal t
    INNER JOIN #Src s ON s.HtsId = t.HtsId
    WHERE t.HtsId <> 0;
    PRINT N'  Proposal updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    INSERT INTO Epms.Proposal (
        HtsId, Title, Code, RequestedOrgUnitId, SalesExperts, SalesExpertIds, PartyIndustryId,
        ProposalRequest, Status, Comment, TenderName, SalesManagerId, SalesSupervisorId,
        StartMiladiDate, StartShamsiDate, TenderMiladiDate, TenderShamsiDate,
        IsSendToMetre, VpisTypeId, IsForProductEngineeringDepartment, HasGeneralPackage,
        Revision, RevisionDescription,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.Title, s.Code, s.RequestedOrgUnitId, s.SalesExperts, s.SalesExpertIds, s.PartyIndustryId,
        s.ProposalRequest, s.Status, s.Comment, s.TenderName, s.SalesManagerId, s.SalesSupervisorId,
        s.StartMiladiDate, s.StartShamsiDate, s.TenderMiladiDate, s.TenderShamsiDate,
        s.IsSendToMetre, s.VpisTypeId, s.IsForProductEngineeringDepartment, s.HasGeneralPackage,
        s.Revision, s.RevisionDescription,
        s.CreatedById, s.CreatedByName, s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime,
        s.ModifiedById, ISNULL(s.ModifiedByName, @Seed),
        ISNULL(s.ModifiedDateMiladiDateTime, s.CreatedOnMiladiDateTime),
        ISNULL(s.ModifiedDateShamsiDateTime, s.CreatedOnShamsiDateTime), 1
    FROM #Src s
    WHERE NOT EXISTS (SELECT 1 FROM Epms.Proposal t WHERE t.HtsId = s.HtsId AND t.HtsId <> 0);
    PRINT N'  Proposal inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    COMMIT TRANSACTION;

    PRINT N'=== VERIFY COUNTS ===';
    SELECT
        (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Epms_Proposal]) AS TmsProposals,
        (SELECT COUNT(*) FROM Epms.Proposal WHERE HtsId <> 0) AS PortalSynced,
        (SELECT COUNT(*) FROM Epms.Proposal) AS PortalAll;

    PRINT N'=== UNMAPPED (dropped / NULL, informational) ===';
    SELECT
        SUM(CASE WHEN UnmappedExperts > 0 THEN 1 ELSE 0 END) AS ProposalsWithUnmappedExperts,
        SUM(UnmappedExperts) AS UnmappedExpertRefs,
        SUM(UnmappedManager) AS UnmappedManager,
        SUM(UnmappedSupervisor) AS UnmappedSupervisor,
        SUM(UnmappedIndustry) AS UnmappedIndustry,
        SUM(UnmappedVpisType) AS UnmappedVpisType,
        SUM(CASE WHEN CreatedById IS NULL THEN 1 ELSE 0 END) AS NoCreatorMapped
    FROM #Src;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @Line INT = ERROR_LINE();
    RAISERROR(N'Sync failed (line %d): %s', 16, 1, @Line, @Err);
END CATCH;
