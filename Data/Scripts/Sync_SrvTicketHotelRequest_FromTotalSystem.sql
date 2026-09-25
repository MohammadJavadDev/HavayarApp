/*
Sync_SrvTicketHotelRequest_FromTotalSystem.sql
One-time sync: Srv_Mission / Srv_MissionTicket / Srv_MissionHotel (TotalSystem via [TMS])
  -> Srv.TicketHotelRequest / TicketHotelRequestTicket / TicketHotelRequestHotel (HavayarApp)

HtsId:
  Header = Srv_Mission.Id
  Ticket = Srv_MissionTicket.Id
  Hotel  = Srv_MissionHotel.Id
Parent FK on children: TicketHotelRequestId (not MissionId).
Ticket date columns keep HTS spelling: DeparturDate / DeparturTime.
Lookup enums stored as int with same HTS Gnr_Lookup ids (1290, ...).

Mappings:
  CreatedById / ApproverId: Gnr_User.Username -> system.User.Username
    + only alias: allahverdi.s missing -> allahvirdi.s (see SyncCourseFromTotalSystem.sql)
  DepartmentId: HRM_OrgUnit.Hamkaran_Unit_FK -> Hrm.OrgUnit.HamkaranUnitId, else OrgUnit_Title = Title
  ManCompanyId: Gnr_ManCompany.Hamkaran_ManCompany_FK -> Gnr.Party.HamkaranId
  CostCenterId: Acc_CostCenter.Hamkaran_CostCenter_FK -> Gnr.CostCenter.HamkaranId, else Title
  ProjectDlId: Acc_DL.Hamkaran_Acc_DL_FK -> FIN.DL.HamkaranId
  ProjectId: Edms.Project.HtsId (nullable historical)
  ApplicantIds: HTS comma-separated HRM Personel_ID (Sec_Personnel.Personel_FK)
    -> Sec.Personnel.Id via Sec_Personnel.Personel_ID = Sec.Personnel.HtsId
  ApplicantsInText, travel strings, amounts, cancel flags, CreatedDateInText, ApprovedDateTimeInText: copy as-is
  IsActive = 1; CreatedOnMiladiDateTime from HTS CreatedDateTime

NULL FKs in HTS stay NULL.
Company / ProjectDl / Department: HTS non-null with no Havayar match -> store NULL; row still loaded.
ApplicantIds: keep only ids that resolve to Sec.Personnel; drop unresolved tokens; ApplicantsInText unchanged.
  If every applicant id is unresolved -> ApplicantIds NULL.
CostCenter / Approver / Edms Project: same map; NULL if unmatched (nullable).
CreatedById: Username map + allahverdi.s -> allahvirdi.s only. If still missing -> THROW with username (do not invent).

Idempotent MERGE-style on HtsId. No background job.
UTF-8 with BOM. sqlcmd -S PortalSRV\PORTAL -d HavayarApp -C -b -I -f 65001 -i ...
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'Srv.TicketHotelRequest', N'U') IS NULL
   OR OBJECT_ID(N'Srv.TicketHotelRequestTicket', N'U') IS NULL
   OR OBJECT_ID(N'Srv.TicketHotelRequestHotel', N'U') IS NULL
BEGIN
    RAISERROR(N'جداول Srv.TicketHotelRequest* وجود ندارند — ابتدا migration را اعمال کنید.', 16, 1);
    RETURN;
END;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = dbo.ToShamsiDate(CAST(@Now AS DATE), N'/') + N' ' + CONVERT(VARCHAR(8), @Now, 108);
DECLARE @Seed NVARCHAR(80) = N'sync-srv-tickethotelrequest';

BEGIN TRY
    BEGIN TRANSACTION;

    /* ========== User map (Username + allahverdi.s -> allahvirdi.s only) ========== */
    IF OBJECT_ID('tempdb..#UserMap') IS NOT NULL DROP TABLE #UserMap;
    SELECT
        CAST(ou.User_ID AS INT) AS OldUserId,
        nu.Id AS NewUserId,
        COALESCE(NULLIF(LTRIM(RTRIM(nu.NameFa)), N''), nu.Name, nu.Username) AS NewUserName,
        ou.Username AS Username
    INTO #UserMap
    FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
    INNER JOIN [system].[User] nu
        ON nu.Username = ou.Username;

    IF NOT EXISTS (SELECT 1 FROM #UserMap WHERE OldUserId = 40)
    BEGIN
        INSERT INTO #UserMap (OldUserId, NewUserId, NewUserName, Username)
        SELECT
            40,
            nu.Id,
            COALESCE(NULLIF(LTRIM(RTRIM(nu.NameFa)), N''), nu.Name, nu.Username),
            nu.Username
        FROM [system].[User] nu
        WHERE nu.Username = N'allahvirdi.s';
    END;
    CREATE UNIQUE CLUSTERED INDEX IX_UserMap ON #UserMap (OldUserId);

    /* ========== CostCenter map ========== */
    IF OBJECT_ID('tempdb..#CostCenterMap') IS NOT NULL DROP TABLE #CostCenterMap;
    SELECT
        cc.CostCenter_ID AS OldCostCenterId,
        COALESCE(byHam.Id, byTitle.Id) AS NewCostCenterId
    INTO #CostCenterMap
    FROM [TMS].[TotalSystem].[dbo].[Acc_CostCenter] cc
    OUTER APPLY (
        SELECT TOP (1) c.Id
        FROM Gnr.CostCenter c
        WHERE ISNULL(cc.Hamkaran_CostCenter_FK, 0) <> 0
          AND c.HamkaranId = CAST(cc.Hamkaran_CostCenter_FK AS BIGINT)
        ORDER BY c.Id
    ) byHam
    OUTER APPLY (
        SELECT TOP (1) c.Id
        FROM Gnr.CostCenter c
        WHERE byHam.Id IS NULL
          AND NULLIF(LTRIM(RTRIM(cc.CostCenter_Title)), N'') IS NOT NULL
          AND LTRIM(RTRIM(c.Title)) = LTRIM(RTRIM(cc.CostCenter_Title))
        ORDER BY c.Id
    ) byTitle;
    CREATE UNIQUE CLUSTERED INDEX IX_CostCenterMap ON #CostCenterMap (OldCostCenterId);

    /* ========== ManCompany map ========== */
    IF OBJECT_ID('tempdb..#ManCompanyMap') IS NOT NULL DROP TABLE #ManCompanyMap;
    SELECT OldManCompanyId, NewManCompanyId
    INTO #ManCompanyMap
    FROM (
        SELECT
            mc.ManCompany_ID AS OldManCompanyId,
            py.Id AS NewManCompanyId,
            ROW_NUMBER() OVER (PARTITION BY mc.ManCompany_ID ORDER BY py.Id) AS rn
        FROM [TMS].[TotalSystem].[dbo].[Gnr_ManCompany] mc
        INNER JOIN Gnr.Party py
            ON py.HamkaranId = CAST(mc.Hamkaran_ManCompany_FK AS BIGINT)
           AND ISNULL(mc.Hamkaran_ManCompany_FK, 0) <> 0
    ) x
    WHERE rn = 1;
    CREATE UNIQUE CLUSTERED INDEX IX_ManCompanyMap ON #ManCompanyMap (OldManCompanyId);

    /* ========== OrgUnit / Department map ========== */
    IF OBJECT_ID('tempdb..#OrgUnitMap') IS NOT NULL DROP TABLE #OrgUnitMap;
    SELECT
        hrm.OrgUnit_ID AS OldOrgUnitId,
        COALESCE(byHam.Id, byTitle.Id) AS NewOrgUnitId
    INTO #OrgUnitMap
    FROM [TMS].[TotalSystem].[dbo].[HRM_OrgUnit] hrm
    OUTER APPLY (
        SELECT TOP (1) o.Id
        FROM Hrm.OrgUnit o
        WHERE hrm.Hamkaran_Unit_FK IS NOT NULL
          AND ISNULL(hrm.Hamkaran_Unit_FK, 0) <> 0
          AND o.HamkaranUnitId = CAST(hrm.Hamkaran_Unit_FK AS BIGINT)
        ORDER BY o.Id
    ) byHam
    OUTER APPLY (
        SELECT TOP (1) o.Id
        FROM Hrm.OrgUnit o
        WHERE byHam.Id IS NULL
          AND NULLIF(LTRIM(RTRIM(hrm.OrgUnit_Title)), N'') IS NOT NULL
          AND LTRIM(RTRIM(o.Title)) = LTRIM(RTRIM(hrm.OrgUnit_Title))
        ORDER BY o.Id
    ) byTitle;
    CREATE UNIQUE CLUSTERED INDEX IX_OrgUnitMap ON #OrgUnitMap (OldOrgUnitId);

    /* ========== Project DL map ========== */
    IF OBJECT_ID('tempdb..#ProjectDlMap') IS NOT NULL DROP TABLE #ProjectDlMap;
    SELECT OldDlId, NewDlId
    INTO #ProjectDlMap
    FROM (
        SELECT
            dl.Acc_DL_ID AS OldDlId,
            fdl.Id AS NewDlId,
            ROW_NUMBER() OVER (PARTITION BY dl.Acc_DL_ID ORDER BY fdl.Id) AS rn
        FROM [TMS].[TotalSystem].[dbo].[Acc_DL] dl
        INNER JOIN FIN.DL fdl
            ON fdl.HamkaranId = CAST(dl.Hamkaran_Acc_DL_FK AS BIGINT)
           AND ISNULL(dl.Hamkaran_Acc_DL_FK, 0) <> 0
    ) x
    WHERE rn = 1;
    CREATE UNIQUE CLUSTERED INDEX IX_ProjectDlMap ON #ProjectDlMap (OldDlId);

    /* ========== Applicant map: HRM Personel_ID (Sec_Personnel.Personel_FK) -> Sec.Personnel.Id via HtsId=Personel_ID ========== */
    IF OBJECT_ID('tempdb..#ApplicantMap') IS NOT NULL DROP TABLE #ApplicantMap;
    SELECT OldHrmId, NewPersonnelId
    INTO #ApplicantMap
    FROM (
        SELECT
            CAST(sp.Personel_FK AS BIGINT) AS OldHrmId,
            p.Id AS NewPersonnelId,
            ROW_NUMBER() OVER (PARTITION BY sp.Personel_FK ORDER BY p.Id) AS rn
        FROM [TMS].[TotalSystem].[dbo].[Sec_Personnel] sp
        INNER JOIN Sec.Personnel p
            ON p.HtsId = CAST(sp.Personel_ID AS BIGINT)
           AND ISNULL(p.HtsId, 0) <> 0
        WHERE sp.Personel_FK IS NOT NULL
          AND sp.Personel_FK <> 0
    ) x
    WHERE rn = 1;
    CREATE UNIQUE CLUSTERED INDEX IX_ApplicantMap ON #ApplicantMap (OldHrmId);

    IF OBJECT_ID('tempdb..#ApplicantTok') IS NOT NULL DROP TABLE #ApplicantTok;
    SELECT
        CAST(m.Id AS BIGINT) AS MissionHtsId,
        TRY_CAST(LTRIM(RTRIM(tok.value)) AS BIGINT) AS OldHrmId
    INTO #ApplicantTok
    FROM [TMS].[TotalSystem].[dbo].[Srv_Mission] m
    CROSS APPLY STRING_SPLIT(REPLACE(ISNULL(m.ApplicantIds, N''), N' ', N''), N',') tok
    WHERE TRY_CAST(LTRIM(RTRIM(tok.value)) AS BIGINT) IS NOT NULL
      AND TRY_CAST(LTRIM(RTRIM(tok.value)) AS BIGINT) > 0;

    IF OBJECT_ID('tempdb..#ApplicantIdsMapped') IS NOT NULL DROP TABLE #ApplicantIdsMapped;
    SELECT
        t.MissionHtsId,
        STUFF((
            SELECT N',' + CAST(am.NewPersonnelId AS NVARCHAR(20))
            FROM #ApplicantTok t2
            INNER JOIN #ApplicantMap am ON am.OldHrmId = t2.OldHrmId
            WHERE t2.MissionHtsId = t.MissionHtsId
            FOR XML PATH(N''), TYPE
        ).value(N'.[1]', N'nvarchar(max)'), 1, 1, N'') AS NewApplicantIds
    INTO #ApplicantIdsMapped
    FROM #ApplicantTok t
    GROUP BY t.MissionHtsId;

    /* ========== Header staging ========== */
    IF OBJECT_ID('tempdb..#HdrSrc') IS NOT NULL DROP TABLE #HdrSrc;
    SELECT
        CAST(m.Id AS BIGINT) AS HtsId,
        CAST(m.RequestTypeId AS INT) AS RequestTypeId,
        CAST(m.ServiceTypeId AS INT) AS ServiceTypeId,
        NULLIF(LTRIM(RTRIM(m.ServiceTypeText)), N'') AS ServiceTypeText,
        CASE WHEN m.CostCenterId IS NULL THEN NULL ELSE ccm.NewCostCenterId END AS CostCenterId,
        m.CostCenterId AS OldCostCenterId,
        oum.NewOrgUnitId AS DepartmentId,
        CAST(m.DepartmentId AS INT) AS OldDepartmentId,
        CAST(m.RequestReasonId AS INT) AS RequestReasonId,
        NULLIF(LTRIM(RTRIM(m.RequestReasonText)), N'') AS RequestReasonText,
        NULLIF(LTRIM(RTRIM(m.MissionNumber)), N'') AS MissionNumber,
        CAST(m.IsRelatedToProject AS BIT) AS IsRelatedToProject,
        CASE WHEN m.ManCompanyId IS NULL THEN NULL ELSE mcm.NewManCompanyId END AS ManCompanyId,
        m.ManCompanyId AS OldManCompanyId,
        CASE WHEN m.ProjectId IS NULL THEN NULL ELSE prj.Id END AS ProjectId,
        m.ProjectId AS OldProjectId,
        aim.NewApplicantIds AS ApplicantIds,
        NULLIF(LTRIM(RTRIM(m.ApplicantsInText)), N'') AS ApplicantsInText,
        NULLIF(LTRIM(RTRIM(m.ApplicantIds)), N'') AS OldApplicantIds,
        NULLIF(LTRIM(RTRIM(m.DepartureSource)), N'') AS DepartureSource,
        NULLIF(LTRIM(RTRIM(m.DepartureDestination)), N'') AS DepartureDestination,
        NULLIF(LTRIM(RTRIM(m.ReturnSource)), N'') AS ReturnSource,
        NULLIF(LTRIM(RTRIM(m.ReturnDestination)), N'') AS ReturnDestination,
        NULLIF(LTRIM(RTRIM(m.DepartureDate)), N'') AS DepartureDate,
        NULLIF(LTRIM(RTRIM(m.DepartureTime)), N'') AS DepartureTime,
        NULLIF(LTRIM(RTRIM(m.ReturnDate)), N'') AS ReturnDate,
        NULLIF(LTRIM(RTRIM(m.ReturnTime)), N'') AS ReturnTime,
        CAST(m.RequestStatusId AS INT) AS RequestStatusId,
        m.IsApproved,
        CASE WHEN m.ApproverId IS NULL THEN NULL ELSE uma.NewUserId END AS ApproverId,
        CAST(m.ApproverId AS INT) AS OldApproverId,
        NULLIF(LTRIM(RTRIM(m.ApproverComment)), N'') AS ApproverComment,
        NULLIF(LTRIM(RTRIM(m.ApprovedDateTimeInText)), N'') AS ApprovedDateTimeInText,
        NULLIF(LTRIM(RTRIM(m.Comment)), N'') AS Comment,
        NULLIF(LTRIM(RTRIM(m.CreatedDateInText)), N'') AS CreatedDateInText,
        CASE WHEN m.ProjectDlId IS NULL THEN NULL ELSE pdm.NewDlId END AS ProjectDlId,
        m.ProjectDlId AS OldProjectDlId,
        umc.NewUserId AS CreatedById,
        CAST(m.CreatedUserId AS INT) AS OldCreatedUserId,
        umc.NewUserName AS CreatedByName,
        CAST(m.CreatedDateTime AS DATETIME2) AS CreatedOnMiladiDateTime,
        NULLIF(LTRIM(RTRIM(m.CreatedDateInText)), N'') AS CreatedOnShamsiDateTime
    INTO #HdrSrc
    FROM [TMS].[TotalSystem].[dbo].[Srv_Mission] m
    LEFT JOIN #CostCenterMap ccm ON ccm.OldCostCenterId = m.CostCenterId
    LEFT JOIN #OrgUnitMap oum ON oum.OldOrgUnitId = m.DepartmentId
    LEFT JOIN #ManCompanyMap mcm ON mcm.OldManCompanyId = m.ManCompanyId
    LEFT JOIN #ProjectDlMap pdm ON pdm.OldDlId = m.ProjectDlId
    LEFT JOIN Edms.Project prj
        ON m.ProjectId IS NOT NULL
       AND prj.HtsId = CAST(m.ProjectId AS BIGINT)
       AND ISNULL(prj.HtsId, 0) <> 0
    LEFT JOIN #UserMap umc ON umc.OldUserId = CAST(m.CreatedUserId AS INT)
    LEFT JOIN #UserMap uma ON m.ApproverId IS NOT NULL AND uma.OldUserId = CAST(m.ApproverId AS INT)
    LEFT JOIN #ApplicantIdsMapped aim ON aim.MissionHtsId = CAST(m.Id AS BIGINT);

    /* CreatedById must resolve (Username map only; no invented aliases). */
    IF EXISTS (SELECT 1 FROM #HdrSrc WHERE CreatedById IS NULL)
    BEGIN
        PRINT N'--- Unmapped CreatedById (abort) ---';
        SELECT TOP (50)
            s.HtsId,
            s.OldCreatedUserId,
            ou.Username AS HtsUsername
        FROM #HdrSrc s
        LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] ou
            ON ou.User_ID = s.OldCreatedUserId
        WHERE s.CreatedById IS NULL
        ORDER BY s.HtsId;

        DECLARE @MissingUser NVARCHAR(200) =
        (
            SELECT TOP (1) COALESCE(ou.Username, N'(unknown User_ID=' + CAST(s.OldCreatedUserId AS NVARCHAR(20)) + N')')
            FROM #HdrSrc s
            LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] ou ON ou.User_ID = s.OldCreatedUserId
            WHERE s.CreatedById IS NULL
            ORDER BY s.HtsId
        );
        DECLARE @Msg NVARCHAR(400) =
            N'CreatedById unmapped for user ''' + ISNULL(@MissingUser, N'?') + N''' — sync aborted; do not invent aliases.';
        THROW 50002, @Msg, 1;
    END;

    PRINT N'Syncing business rows (unmapped company/projectDl/department/applicant -> NULL)...';

    /* ========== Headers: UPDATE then INSERT ========== */
    UPDATE t SET
        t.RequestTypeId = s.RequestTypeId,
        t.ServiceTypeId = s.ServiceTypeId,
        t.ServiceTypeText = s.ServiceTypeText,
        t.CostCenterId = s.CostCenterId,
        t.DepartmentId = s.DepartmentId,
        t.RequestReasonId = s.RequestReasonId,
        t.RequestReasonText = s.RequestReasonText,
        t.MissionNumber = s.MissionNumber,
        t.IsRelatedToProject = s.IsRelatedToProject,
        t.ManCompanyId = s.ManCompanyId,
        t.ProjectId = s.ProjectId,
        t.ApplicantIds = s.ApplicantIds,
        t.ApplicantsInText = s.ApplicantsInText,
        t.DepartureSource = s.DepartureSource,
        t.DepartureDestination = s.DepartureDestination,
        t.ReturnSource = s.ReturnSource,
        t.ReturnDestination = s.ReturnDestination,
        t.DepartureDate = s.DepartureDate,
        t.DepartureTime = s.DepartureTime,
        t.ReturnDate = s.ReturnDate,
        t.ReturnTime = s.ReturnTime,
        t.RequestStatusId = s.RequestStatusId,
        t.IsApproved = s.IsApproved,
        t.ApproverId = s.ApproverId,
        t.ApproverComment = s.ApproverComment,
        t.ApprovedDateTimeInText = s.ApprovedDateTimeInText,
        t.Comment = s.Comment,
        t.CreatedDateInText = s.CreatedDateInText,
        t.ProjectDlId = s.ProjectDlId,
        t.CreatedById = s.CreatedById,
        t.CreatedByName = s.CreatedByName,
        t.CreatedOnMiladiDateTime = s.CreatedOnMiladiDateTime,
        t.CreatedOnShamsiDateTime = s.CreatedOnShamsiDateTime,
        t.ModifiedById = 1,
        t.ModifiedByName = @Seed,
        t.ModifiedDateMiladiDateTime = @Now,
        t.ModifiedDateShamsiDateTime = @NowShamsi,
        t.IsActive = 1
    FROM Srv.TicketHotelRequest t
    INNER JOIN #HdrSrc s ON s.HtsId = t.HtsId;
    PRINT N'  TicketHotelRequest updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    INSERT INTO Srv.TicketHotelRequest (
        HtsId, RequestTypeId, ServiceTypeId, ServiceTypeText, CostCenterId, DepartmentId,
        RequestReasonId, RequestReasonText, MissionNumber, IsRelatedToProject, ManCompanyId, ProjectId,
        ApplicantIds, ApplicantsInText, DepartureSource, DepartureDestination, ReturnSource, ReturnDestination,
        DepartureDate, DepartureTime, ReturnDate, ReturnTime, RequestStatusId, IsApproved, ApproverId,
        ApproverComment, ApprovedDateTimeInText, Comment, CreatedDateInText, ProjectDlId,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.RequestTypeId, s.ServiceTypeId, s.ServiceTypeText, s.CostCenterId, s.DepartmentId,
        s.RequestReasonId, s.RequestReasonText, s.MissionNumber, s.IsRelatedToProject, s.ManCompanyId, s.ProjectId,
        s.ApplicantIds, s.ApplicantsInText, s.DepartureSource, s.DepartureDestination, s.ReturnSource, s.ReturnDestination,
        s.DepartureDate, s.DepartureTime, s.ReturnDate, s.ReturnTime, s.RequestStatusId, s.IsApproved, s.ApproverId,
        s.ApproverComment, s.ApprovedDateTimeInText, s.Comment, s.CreatedDateInText, s.ProjectDlId,
        s.CreatedById, s.CreatedByName, s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime,
        1, @Seed, @Now, @NowShamsi, 1
    FROM #HdrSrc s
    WHERE NOT EXISTS (SELECT 1 FROM Srv.TicketHotelRequest t WHERE t.HtsId = s.HtsId);
    PRINT N'  TicketHotelRequest inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    /* ========== Tickets ========== */
    IF OBJECT_ID('tempdb..#TktSrc') IS NOT NULL DROP TABLE #TktSrc;
    SELECT
        CAST(tk.Id AS BIGINT) AS HtsId,
        hdr.Id AS TicketHotelRequestId,
        CAST(tk.TicketTypeId AS INT) AS TicketTypeId,
        NULLIF(LTRIM(RTRIM(tk.Airline)), N'') AS Airline,
        NULLIF(LTRIM(RTRIM(tk.PassengerTerminal)), N'') AS PassengerTerminal,
        NULLIF(LTRIM(RTRIM(tk.RailwayStation)), N'') AS RailwayStation,
        NULLIF(LTRIM(RTRIM(tk.TravelAgency)), N'') AS TravelAgency,
        NULLIF(LTRIM(RTRIM(tk.IssueDate)), N'') AS IssueDate,
        NULLIF(LTRIM(RTRIM(tk.TicketNumber)), N'') AS TicketNumber,
        CAST(tk.TicketAmount AS INT) AS TicketAmount,
        NULLIF(LTRIM(RTRIM(tk.Source)), N'') AS Source,
        NULLIF(LTRIM(RTRIM(tk.Destination)), N'') AS Destination,
        NULLIF(LTRIM(RTRIM(tk.DeparturDate)), N'') AS DeparturDate,
        NULLIF(LTRIM(RTRIM(tk.DeparturTime)), N'') AS DeparturTime,
        tk.IsCanceled,
        NULLIF(LTRIM(RTRIM(tk.CancelDateInText)), N'') AS CancelDateInText,
        NULLIF(LTRIM(RTRIM(tk.CancelReason)), N'') AS CancelReason,
        tk.IsPaidByApplicant,
        tk.IsPaidByFinancialDepartment,
        NULLIF(LTRIM(RTRIM(tk.CreatedDateInText)), N'') AS CreatedDateInText,
        NULLIF(LTRIM(RTRIM(tk.Comment)), N'') AS Comment,
        um.NewUserId AS CreatedById,
        um.NewUserName AS CreatedByName,
        CAST(tk.CreatedDateTime AS DATETIME2) AS CreatedOnMiladiDateTime,
        NULLIF(LTRIM(RTRIM(tk.CreatedDateInText)), N'') AS CreatedOnShamsiDateTime
    INTO #TktSrc
    FROM [TMS].[TotalSystem].[dbo].[Srv_MissionTicket] tk
    INNER JOIN Srv.TicketHotelRequest hdr ON hdr.HtsId = CAST(tk.MissionId AS BIGINT)
    LEFT JOIN #UserMap um ON um.OldUserId = CAST(tk.CreatedUserId AS INT);

    UPDATE t SET
        t.TicketHotelRequestId = s.TicketHotelRequestId,
        t.TicketTypeId = s.TicketTypeId,
        t.Airline = s.Airline,
        t.PassengerTerminal = s.PassengerTerminal,
        t.RailwayStation = s.RailwayStation,
        t.TravelAgency = s.TravelAgency,
        t.IssueDate = s.IssueDate,
        t.TicketNumber = s.TicketNumber,
        t.TicketAmount = s.TicketAmount,
        t.Source = s.Source,
        t.Destination = s.Destination,
        t.DeparturDate = s.DeparturDate,
        t.DeparturTime = s.DeparturTime,
        t.IsCanceled = s.IsCanceled,
        t.CancelDateInText = s.CancelDateInText,
        t.CancelReason = s.CancelReason,
        t.IsPaidByApplicant = s.IsPaidByApplicant,
        t.IsPaidByFinancialDepartment = s.IsPaidByFinancialDepartment,
        t.CreatedDateInText = s.CreatedDateInText,
        t.Comment = s.Comment,
        t.CreatedById = COALESCE(s.CreatedById, t.CreatedById),
        t.CreatedByName = COALESCE(s.CreatedByName, t.CreatedByName),
        t.CreatedOnMiladiDateTime = COALESCE(s.CreatedOnMiladiDateTime, t.CreatedOnMiladiDateTime),
        t.CreatedOnShamsiDateTime = COALESCE(s.CreatedOnShamsiDateTime, t.CreatedOnShamsiDateTime),
        t.ModifiedById = 1,
        t.ModifiedByName = @Seed,
        t.ModifiedDateMiladiDateTime = @Now,
        t.ModifiedDateShamsiDateTime = @NowShamsi,
        t.IsActive = 1
    FROM Srv.TicketHotelRequestTicket t
    INNER JOIN #TktSrc s ON s.HtsId = t.HtsId;
    PRINT N'  TicketHotelRequestTicket updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    INSERT INTO Srv.TicketHotelRequestTicket (
        HtsId, TicketHotelRequestId, TicketTypeId, Airline, PassengerTerminal, RailwayStation, TravelAgency,
        IssueDate, TicketNumber, TicketAmount, Source, Destination, DeparturDate, DeparturTime,
        IsCanceled, CancelDateInText, CancelReason, IsPaidByApplicant, IsPaidByFinancialDepartment,
        CreatedDateInText, Comment,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.TicketHotelRequestId, s.TicketTypeId, s.Airline, s.PassengerTerminal, s.RailwayStation, s.TravelAgency,
        s.IssueDate, s.TicketNumber, s.TicketAmount, s.Source, s.Destination, s.DeparturDate, s.DeparturTime,
        s.IsCanceled, s.CancelDateInText, s.CancelReason, s.IsPaidByApplicant, s.IsPaidByFinancialDepartment,
        s.CreatedDateInText, s.Comment,
        ISNULL(s.CreatedById, 1), ISNULL(s.CreatedByName, @Seed),
        ISNULL(s.CreatedOnMiladiDateTime, @Now), ISNULL(s.CreatedOnShamsiDateTime, @NowShamsi),
        1, @Seed, @Now, @NowShamsi, 1
    FROM #TktSrc s
    WHERE NOT EXISTS (SELECT 1 FROM Srv.TicketHotelRequestTicket t WHERE t.HtsId = s.HtsId);
    PRINT N'  TicketHotelRequestTicket inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    /* ========== Hotels ========== */
    IF OBJECT_ID('tempdb..#HtlSrc') IS NOT NULL DROP TABLE #HtlSrc;
    SELECT
        CAST(h.Id AS BIGINT) AS HtsId,
        hdr.Id AS TicketHotelRequestId,
        NULLIF(LTRIM(RTRIM(h.HotelTitle)), N'') AS HotelTitle,
        h.StarsNumber,
        CAST(h.HotelAmount AS INT) AS HotelAmount,
        NULLIF(LTRIM(RTRIM(h.Comment)), N'') AS Comment,
        NULLIF(LTRIM(RTRIM(h.CreatedDateInText)), N'') AS CreatedDateInText,
        um.NewUserId AS CreatedById,
        um.NewUserName AS CreatedByName,
        CAST(h.CreatedDateTime AS DATETIME2) AS CreatedOnMiladiDateTime,
        NULLIF(LTRIM(RTRIM(h.CreatedDateInText)), N'') AS CreatedOnShamsiDateTime
    INTO #HtlSrc
    FROM [TMS].[TotalSystem].[dbo].[Srv_MissionHotel] h
    INNER JOIN Srv.TicketHotelRequest hdr ON hdr.HtsId = CAST(h.MissionId AS BIGINT)
    LEFT JOIN #UserMap um ON um.OldUserId = CAST(h.CreatedUserId AS INT);

    UPDATE t SET
        t.TicketHotelRequestId = s.TicketHotelRequestId,
        t.HotelTitle = s.HotelTitle,
        t.StarsNumber = s.StarsNumber,
        t.HotelAmount = s.HotelAmount,
        t.Comment = s.Comment,
        t.CreatedDateInText = s.CreatedDateInText,
        t.CreatedById = COALESCE(s.CreatedById, t.CreatedById),
        t.CreatedByName = COALESCE(s.CreatedByName, t.CreatedByName),
        t.CreatedOnMiladiDateTime = COALESCE(s.CreatedOnMiladiDateTime, t.CreatedOnMiladiDateTime),
        t.CreatedOnShamsiDateTime = COALESCE(s.CreatedOnShamsiDateTime, t.CreatedOnShamsiDateTime),
        t.ModifiedById = 1,
        t.ModifiedByName = @Seed,
        t.ModifiedDateMiladiDateTime = @Now,
        t.ModifiedDateShamsiDateTime = @NowShamsi,
        t.IsActive = 1
    FROM Srv.TicketHotelRequestHotel t
    INNER JOIN #HtlSrc s ON s.HtsId = t.HtsId;
    PRINT N'  TicketHotelRequestHotel updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    INSERT INTO Srv.TicketHotelRequestHotel (
        HtsId, TicketHotelRequestId, HotelTitle, StarsNumber, HotelAmount, Comment, CreatedDateInText,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.TicketHotelRequestId, s.HotelTitle, s.StarsNumber, s.HotelAmount, s.Comment, s.CreatedDateInText,
        ISNULL(s.CreatedById, 1), ISNULL(s.CreatedByName, @Seed),
        ISNULL(s.CreatedOnMiladiDateTime, @Now), ISNULL(s.CreatedOnShamsiDateTime, @NowShamsi),
        1, @Seed, @Now, @NowShamsi, 1
    FROM #HtlSrc s
    WHERE NOT EXISTS (SELECT 1 FROM Srv.TicketHotelRequestHotel t WHERE t.HtsId = s.HtsId);
    PRINT N'  TicketHotelRequestHotel inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    COMMIT TRANSACTION;

    PRINT N'=== VERIFY COUNTS ===';
    SELECT
        (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Srv_Mission]) AS TmsHeaders,
        (SELECT COUNT(*) FROM Srv.TicketHotelRequest) AS HavayarHeaders,
        (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Srv_MissionTicket]) AS TmsTickets,
        (SELECT COUNT(*) FROM Srv.TicketHotelRequestTicket) AS HavayarTickets,
        (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Srv_MissionHotel]) AS TmsHotels,
        (SELECT COUNT(*) FROM Srv.TicketHotelRequestHotel) AS HavayarHotels;

    PRINT N'=== INFO: soft-NULL FK rows (loaded with NULL; not aborted) ===';
    SELECT
        (SELECT COUNT(*) FROM Srv.TicketHotelRequest WHERE ManCompanyId IS NULL) AS NullManCompanyId,
        (SELECT COUNT(*) FROM Srv.TicketHotelRequest WHERE ProjectDlId IS NULL) AS NullProjectDlId,
        (SELECT COUNT(*) FROM Srv.TicketHotelRequest WHERE DepartmentId IS NULL) AS NullDepartmentId,
        (SELECT COUNT(*) FROM #HdrSrc WHERE OldManCompanyId IS NOT NULL AND ManCompanyId IS NULL) AS SoftNullCompanyFromUnmapped,
        (SELECT COUNT(*) FROM #HdrSrc WHERE OldProjectDlId IS NOT NULL AND ProjectDlId IS NULL) AS SoftNullProjectDlFromUnmapped,
        (SELECT COUNT(*) FROM #HdrSrc WHERE OldDepartmentId IS NOT NULL AND DepartmentId IS NULL) AS SoftNullDepartmentFromUnmapped,
        (SELECT COUNT(DISTINCT t.OldHrmId)
         FROM #ApplicantTok t
         LEFT JOIN #ApplicantMap am ON am.OldHrmId = t.OldHrmId
         WHERE am.NewPersonnelId IS NULL) AS DroppedApplicantHrmIds;

    PRINT N'Sync_SrvTicketHotelRequest_FromTotalSystem completed.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @Line INT = ERROR_LINE();
    RAISERROR(N'Sync failed (line %d): %s', 16, 1, @Line, @Err);
END CATCH;
