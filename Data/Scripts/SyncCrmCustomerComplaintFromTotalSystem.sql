/*
سینک شکایات مشتری از TotalSystem.
پرسنل: HRM_Personel.Hamkaran_Personel_FK = Hcm.Personel.Code
مشاهده‌گر: همان پرسنل از راه Gnr_User به system.User
واحد: HRM_OrgUnit.Hamkaran_Unit_FK = Hrm.OrgUnit.HamkaranUnitId
پیوست باینری در SyncCrmCustomerComplaintAttachments.ps1 نوشته می‌شود.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID('tempdb..#Gap') IS NOT NULL DROP TABLE #Gap;
CREATE TABLE #Gap (Kind NVARCHAR(80), HtsId BIGINT, Detail NVARCHAR(400));

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = SYSDATETIME();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'sync-crm-complaint';

    IF OBJECT_ID('tempdb..#UserMap') IS NOT NULL DROP TABLE #UserMap;
    SELECT OldUserId, NewUserId, NewName
    INTO #UserMap
    FROM (
        SELECT ou.User_ID AS OldUserId, nu.Id AS NewUserId,
               COALESCE(NULLIF(nu.NameFa, N''), nu.Name) AS NewName,
               ROW_NUMBER() OVER (PARTITION BY ou.User_ID ORDER BY nu.Id) AS rn
        FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
        INNER JOIN [system].[User] nu
            ON LOWER(nu.Username) = LOWER(COALESCE(NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N''), LTRIM(RTRIM(ou.Username))))
            OR LOWER(nu.Username) = LOWER(LTRIM(RTRIM(ou.Username)))
    ) x WHERE rn = 1;
    CREATE UNIQUE CLUSTERED INDEX IX_UserMap ON #UserMap (OldUserId);

    IF OBJECT_ID('tempdb..#PersonMap') IS NOT NULL DROP TABLE #PersonMap;
    SELECT hrm.Personel_ID AS OldPersonelId, MIN(p.Id) AS PersonelId
    INTO #PersonMap
    FROM [TMS].[TotalSystem].[dbo].[HRM_Personel] hrm
    INNER JOIN Hcm.Personel p ON TRY_CONVERT(int, p.Code) = hrm.Hamkaran_Personel_FK
    WHERE ISNULL(hrm.Hamkaran_Personel_FK, 0) <> 0
    GROUP BY hrm.Personel_ID;
    CREATE UNIQUE CLUSTERED INDEX IX_PersonMap ON #PersonMap (OldPersonelId);

    IF OBJECT_ID('tempdb..#ViewerMap') IS NOT NULL DROP TABLE #ViewerMap;
    SELECT OldPersonelId, MIN(NewUserId) AS NewUserId
    INTO #ViewerMap
    FROM (
        SELECT hrm.Personel_ID AS OldPersonelId, um.NewUserId
        FROM [TMS].[TotalSystem].[dbo].[HRM_Personel] hrm
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] ou ON ou.Personel_FK = hrm.Personel_ID
        INNER JOIN #UserMap um ON um.OldUserId = ou.User_ID
    ) v
    GROUP BY OldPersonelId
    HAVING COUNT(DISTINCT NewUserId) = 1;
    CREATE UNIQUE CLUSTERED INDEX IX_ViewerMap ON #ViewerMap (OldPersonelId);

    IF OBJECT_ID('tempdb..#OrgMap') IS NOT NULL DROP TABLE #OrgMap;
    SELECT hrm.OrgUnit_ID AS OldOrgUnitId, MIN(o.Id) AS OrgUnitId
    INTO #OrgMap
    FROM [TMS].[TotalSystem].[dbo].[HRM_OrgUnit] hrm
    INNER JOIN Hrm.OrgUnit o ON o.HamkaranUnitId = CAST(hrm.Hamkaran_Unit_FK AS bigint)
    WHERE ISNULL(hrm.Hamkaran_Unit_FK, 0) <> 0
    GROUP BY hrm.OrgUnit_ID;
    CREATE UNIQUE CLUSTERED INDEX IX_OrgMap ON #OrgMap (OldOrgUnitId);

    /* لوکاپ‌های قابل افزودن */
    MERGE Crm.ComplaintLookupItem AS t
    USING (
        SELECT l.Lookup_ID AS HtsId, l.LookupType_FK AS LookupType, l.Lookup_Title_Fa AS Title
        FROM [TMS].[TotalSystem].[dbo].[Gnr_Lookup] l
        WHERE l.LookupType_FK IN (173, 174, 177, 179)
          AND NULLIF(LTRIM(RTRIM(l.Lookup_Title_Fa)), N'') IS NOT NULL
    ) AS s ON t.HtsId = s.HtsId AND t.LookupType = s.LookupType AND t.HtsId <> 0
    WHEN MATCHED THEN UPDATE SET Title = s.Title, IsActive = 1, ModifiedById = 1, ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
    WHEN NOT MATCHED THEN INSERT (HtsId, LookupType, Title, IsActive, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
        CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime)
    VALUES (s.HtsId, s.LookupType, s.Title, 1, 1, @SeedUser, 1, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi);

    /* نمایندگی خاطی: فهرست فرم + هر شناسه‌ای که روی شکایت آمده */
    MERGE Crm.ComplaintAgency AS t
    USING (
        SELECT m.ManCompany_ID AS HtsId,
               COALESCE(NULLIF(LTRIM(RTRIM(m.CompanyName)), N''), NULLIF(LTRIM(RTRIM(m.FullName)), N''), N'شرکت ' + CAST(m.ManCompany_ID AS nvarchar(20))) AS Title
        FROM [TMS].[TotalSystem].[dbo].[Gnr_ManCompany] m
        WHERE m.ManCompany_ID IN (
            175813,176672,175901,175441,175783,165421,7743,176357,165413,175902,165511,165404,176130,
            165420,176158,165484,165519,165414,175905,165502,1504,165415,178590,177433,181189,177244,181918)
           OR m.ManCompany_ID IN (SELECT DISTINCT WrongdoerAgencyId FROM [TMS].[TotalSystem].[dbo].[Crm_Customer_Complaint] WHERE WrongdoerAgencyId IS NOT NULL)
    ) AS s ON t.HtsId = s.HtsId AND t.HtsId <> 0
    WHEN MATCHED THEN UPDATE SET Title = s.Title, IsActive = 1, ModifiedByName = @SeedUser, ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
    WHEN NOT MATCHED THEN INSERT (HtsId, Title, IsActive, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
        CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime)
    VALUES (s.HtsId, s.Title, 1, 1, @SeedUser, 1, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi);

    MERGE Crm.ComplaintType AS t
    USING (SELECT ComplaintType_ID AS HtsId, ComplaintType_Title AS Title FROM [TMS].[TotalSystem].[dbo].[Crm_ComplaintType]) AS s
    ON t.HtsId = s.HtsId AND t.HtsId <> 0
    WHEN MATCHED THEN UPDATE SET Title = s.Title, IsActive = 1
    WHEN NOT MATCHED THEN INSERT (HtsId, Title, IsActive, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
        CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime)
    VALUES (s.HtsId, s.Title, 1, 1, @SeedUser, 1, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi);

    MERGE Crm.ComplaintReceiveMethod AS t
    USING (SELECT Complaint_ReciveMethod_ID AS HtsId, Complaint_ReciveMethod_Title AS Title FROM [TMS].[TotalSystem].[dbo].[Crm_Complaint_ReciveMethod]) AS s
    ON t.HtsId = s.HtsId AND t.HtsId <> 0
    WHEN MATCHED THEN UPDATE SET Title = s.Title, IsActive = 1
    WHEN NOT MATCHED THEN INSERT (HtsId, Title, IsActive, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
        CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime)
    VALUES (s.HtsId, s.Title, 1, 1, @SeedUser, 1, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi);

    IF OBJECT_ID('tempdb..#Src') IS NOT NULL DROP TABLE #Src;
    SELECT
        h.Id AS HtsId, h.ModuleTypeId, h.SerialNumber, h.TrackingCode,
        cust.Id AS CustomerId, h.CustomerTypeId AS CustomerType, h.CustomerTitle, h.TypeId AS OwnerType,
        h.CompletionDateInText AS CompletionShamsiDate, CAST(h.CompletionDate AS date) AS CompletionMiladiDate,
        h.Address, h.PhoneNumber, h.MobileNumber, h.Fax, h.ComplaintReceiver,
        recv.PersonelId AS ComplaintReceiverPersonelId, h.ComplaintPresenter, h.ComplaintPresenterJob,
        org1.OrgUnitId, h.OrganizationUnitInText, exp1.PersonelId AS ExpertPersonelId, h.ExpertOrManagerInText,
        h.PriorityId AS Priority, h.ComplaintTypeId AS ComplaintType,
        ch.Id AS CommunicationChannelId, h.GeneralComplaintSubjectId AS GeneralComplaintSubject, h.GeneralComplaintSubjectTitle,
        part.Id AS PartId, sub.Id AS ComplaintSubjectId, ag.Id AS AgencyId, h.CompressorFailureId AS CompressorFailure,
        h.ComplaintDescription, h.RaisedSubjects, h.RaisedSubjectIds,
        ISNULL(h.Repeatability, 0) AS Repeatability, ISNULL(h.HasFinancialLoad, 0) AS HasFinancialLoad,
        ISNULL(h.HasUrgencyHandling, 0) AS HasUrgencyHandling, ISNULL(h.HasComplexity, 0) AS HasComplexity,
        inf.Id AS InformForReceiveComplaintId, h.ActionsDescription, h.ComplaintStatusId AS ComplaintStatus,
        h.FinalHandlingDescription, h.HandlingResultIdsInText, h.InformComplainantId AS InformComplainant,
        h.ComplaintConsentId AS ComplaintConsent, h.ManagerDescription, h.FinalComplaintStatusId AS FinalComplaintStatus,
        NULLIF(h.ComplaintClosingDate, N'') AS ComplaintClosingShamsiDate,
        dbo.fn_ShamsiToMiladiDate(NULLIF(h.ComplaintClosingDate, N''), '/') AS ComplaintClosingMiladiDate,
        h.Comment, h.DelayReason, h.ComplaintMainSubjectId AS ComplaintMainSubject, h.MainSubjectsIds, h.MainSubjectsInText,
        ISNULL(h.HasComplaintSurvey, 0) AS HasComplaintSurvey, h.IsSurveyLinkSended, h.SurveyCompletionFormDate AS SurveyCompletionShamsiDate,
        sint.PersonelId AS SurveyInterviewerPersonelId, h.SurveyResponder, h.SurveyDescription,
        h.SurveyLinkSentDateInText AS SurveyLinkSentShamsiDateTime, h.SurveyLinkSentDate AS SurveyLinkSentMiladiDateTime,
        h.SurveyResponseDateInText AS SurveyResponseShamsiDateTime, h.SurveyResponseDate AS SurveyResponseMiladiDateTime,
        h.SurveySubjectsIds, h.SurveySubjectsInText, h.SurveyStatusId,
        cu.NewUserId AS CreatedById, ISNULL(cu.NewName, @SeedUser) AS CreatedByName,
        uu.NewUserId AS ModifiedById, h.CreatedDate AS CreatedOnMiladiDateTime, h.CreatedDateInText AS CreatedOnShamsiDateTime,
        h.UpdatedDate AS ModifiedDateMiladiDateTime, h.UpdateDateInText AS ModifiedDateShamsiDateTime,
        h.OrganizationUnitId AS OldOrgUnitId, h.OrganizationUnitIds AS OldOrgUnitIds,
        h.ExpertOrManagerId AS OldExpertId, h.ExpertOrManagerIds AS OldExpertIds,
        h.ViewersIds AS OldViewerIds, h.HandlingResultIds AS OldHandlingIds,
        h.CustomerId AS OldCustomerId, h.PartId AS OldPartId, h.WrongdoerAgencyId AS OldAgencyId, h.ParentId AS OldParentId
    INTO #Src
    FROM [TMS].[TotalSystem].[dbo].[Crm_Customer_Complaint] h
    LEFT JOIN SLS.Customer cust ON cust.HtsId = h.CustomerId AND cust.HtsId <> 0
    LEFT JOIN #PersonMap recv ON recv.OldPersonelId = h.ComplaintReceiverId
    LEFT JOIN #PersonMap exp1 ON exp1.OldPersonelId = h.ExpertOrManagerId
    LEFT JOIN #PersonMap sint ON sint.OldPersonelId = h.SurveyInterviewerId
    LEFT JOIN #OrgMap org1 ON org1.OldOrgUnitId = h.OrganizationUnitId
    LEFT JOIN Inv.Part part ON part.HtsId = h.PartId AND part.HtsId <> 0
    LEFT JOIN Crm.ComplaintLookupItem ch ON ch.HtsId = h.CommunicationChannelId AND ch.LookupType = 173 AND ch.HtsId <> 0
    LEFT JOIN Crm.ComplaintLookupItem sub ON sub.HtsId = h.ComplaintSubjectId AND sub.LookupType = 174 AND sub.HtsId <> 0
    LEFT JOIN Crm.ComplaintLookupItem inf ON inf.HtsId = h.InformForReceiveComplaintId AND inf.LookupType = 177 AND inf.HtsId <> 0
    LEFT JOIN Crm.ComplaintAgency ag ON ag.HtsId = h.WrongdoerAgencyId AND ag.HtsId <> 0
    LEFT JOIN #UserMap cu ON cu.OldUserId = h.CreatedUserId
    LEFT JOIN #UserMap uu ON uu.OldUserId = h.UpdatedUserId;

    INSERT INTO #Gap (Kind, HtsId, Detail)
    SELECT N'Customer', HtsId, N'HTS CustomerId ' + CAST(OldCustomerId AS nvarchar(20)) FROM #Src WHERE OldCustomerId IS NOT NULL AND CustomerId IS NULL
    UNION ALL SELECT N'Part', HtsId, N'HTS PartId ' + CAST(OldPartId AS nvarchar(20)) FROM #Src WHERE OldPartId IS NOT NULL AND PartId IS NULL
    UNION ALL SELECT N'Agency', HtsId, N'HTS AgencyId ' + CAST(OldAgencyId AS nvarchar(20)) FROM #Src WHERE OldAgencyId IS NOT NULL AND AgencyId IS NULL
    UNION ALL SELECT N'OrgUnit', HtsId, N'HTS OrgUnitId ' + CAST(OldOrgUnitId AS nvarchar(20)) FROM #Src WHERE OldOrgUnitId IS NOT NULL AND OrgUnitId IS NULL
    UNION ALL SELECT N'Expert', HtsId, N'HTS ExpertId ' + CAST(OldExpertId AS nvarchar(20)) FROM #Src WHERE OldExpertId IS NOT NULL AND ExpertPersonelId IS NULL;

    MERGE Crm.CustomerComplaint AS t
    USING #Src AS s ON t.HtsId = s.HtsId AND t.HtsId <> 0
    WHEN MATCHED THEN UPDATE SET
        ModuleTypeId = s.ModuleTypeId, SerialNumber = s.SerialNumber, TrackingCode = s.TrackingCode,
        CustomerId = s.CustomerId, CustomerType = s.CustomerType, CustomerTitle = s.CustomerTitle, OwnerType = s.OwnerType,
        CompletionShamsiDate = s.CompletionShamsiDate, CompletionMiladiDate = s.CompletionMiladiDate,
        Address = s.Address, PhoneNumber = s.PhoneNumber, MobileNumber = s.MobileNumber, Fax = s.Fax,
        ComplaintReceiver = s.ComplaintReceiver, ComplaintReceiverPersonelId = s.ComplaintReceiverPersonelId,
        ComplaintPresenter = s.ComplaintPresenter, ComplaintPresenterJob = s.ComplaintPresenterJob,
        OrgUnitId = s.OrgUnitId, OrganizationUnitInText = s.OrganizationUnitInText,
        ExpertPersonelId = s.ExpertPersonelId, ExpertOrManagerInText = s.ExpertOrManagerInText,
        Priority = s.Priority, ComplaintType = s.ComplaintType, CommunicationChannelId = s.CommunicationChannelId,
        GeneralComplaintSubject = s.GeneralComplaintSubject, GeneralComplaintSubjectTitle = s.GeneralComplaintSubjectTitle,
        PartId = s.PartId, ComplaintSubjectId = s.ComplaintSubjectId, AgencyId = s.AgencyId, CompressorFailure = s.CompressorFailure,
        ComplaintDescription = s.ComplaintDescription, RaisedSubjects = s.RaisedSubjects, RaisedSubjectIds = s.RaisedSubjectIds,
        Repeatability = s.Repeatability, HasFinancialLoad = s.HasFinancialLoad, HasUrgencyHandling = s.HasUrgencyHandling, HasComplexity = s.HasComplexity,
        InformForReceiveComplaintId = s.InformForReceiveComplaintId, ActionsDescription = s.ActionsDescription, ComplaintStatus = s.ComplaintStatus,
        FinalHandlingDescription = s.FinalHandlingDescription, HandlingResultIdsInText = s.HandlingResultIdsInText,
        InformComplainant = s.InformComplainant, ComplaintConsent = s.ComplaintConsent, ManagerDescription = s.ManagerDescription,
        FinalComplaintStatus = s.FinalComplaintStatus, ComplaintClosingShamsiDate = s.ComplaintClosingShamsiDate,
        ComplaintClosingMiladiDate = s.ComplaintClosingMiladiDate,
        Comment = s.Comment, DelayReason = s.DelayReason, ComplaintMainSubject = s.ComplaintMainSubject,
        MainSubjectsIds = s.MainSubjectsIds, MainSubjectsInText = s.MainSubjectsInText,
        HasComplaintSurvey = s.HasComplaintSurvey, IsSurveyLinkSended = s.IsSurveyLinkSended,
        SurveyCompletionShamsiDate = s.SurveyCompletionShamsiDate, SurveyInterviewerPersonelId = s.SurveyInterviewerPersonelId,
        SurveyResponder = s.SurveyResponder, SurveyDescription = s.SurveyDescription,
        SurveyLinkSentShamsiDateTime = s.SurveyLinkSentShamsiDateTime, SurveyLinkSentMiladiDateTime = s.SurveyLinkSentMiladiDateTime,
        SurveyResponseShamsiDateTime = s.SurveyResponseShamsiDateTime, SurveyResponseMiladiDateTime = s.SurveyResponseMiladiDateTime,
        SurveySubjectsIds = s.SurveySubjectsIds, SurveySubjectsInText = s.SurveySubjectsInText, SurveyStatusId = s.SurveyStatusId,
        CreatedById = s.CreatedById, CreatedByName = s.CreatedByName, ModifiedById = s.ModifiedById,
        CreatedOnMiladiDateTime = s.CreatedOnMiladiDateTime, CreatedOnShamsiDateTime = s.CreatedOnShamsiDateTime,
        ModifiedDateMiladiDateTime = s.ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime = s.ModifiedDateShamsiDateTime,
        IsActive = 1, ModifiedByName = @SeedUser
    WHEN NOT MATCHED THEN INSERT (
        HtsId, ModuleTypeId, SerialNumber, TrackingCode, CustomerId, CustomerType, CustomerTitle, OwnerType,
        CompletionShamsiDate, CompletionMiladiDate, Address, PhoneNumber, MobileNumber, Fax, ComplaintReceiver, ComplaintReceiverPersonelId,
        ComplaintPresenter, ComplaintPresenterJob, OrgUnitId, OrganizationUnitInText, ExpertPersonelId, ExpertOrManagerInText,
        Priority, ComplaintType, CommunicationChannelId, GeneralComplaintSubject, GeneralComplaintSubjectTitle, PartId, ComplaintSubjectId, AgencyId,
        CompressorFailure, ComplaintDescription, RaisedSubjects, RaisedSubjectIds, Repeatability, HasFinancialLoad, HasUrgencyHandling, HasComplexity,
        InformForReceiveComplaintId, ActionsDescription, ComplaintStatus, FinalHandlingDescription, HandlingResultIdsInText, InformComplainant, ComplaintConsent,
        ManagerDescription, FinalComplaintStatus, ComplaintClosingShamsiDate, ComplaintClosingMiladiDate, Comment, DelayReason, ComplaintMainSubject, MainSubjectsIds, MainSubjectsInText,
        HasComplaintSurvey, IsSurveyLinkSended, SurveyCompletionShamsiDate, SurveyInterviewerPersonelId, SurveyResponder, SurveyDescription,
        SurveyLinkSentShamsiDateTime, SurveyLinkSentMiladiDateTime, SurveyResponseShamsiDateTime, SurveyResponseMiladiDateTime,
        SurveySubjectsIds, SurveySubjectsInText, SurveyStatusId, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
        CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES (
        s.HtsId, s.ModuleTypeId, s.SerialNumber, s.TrackingCode, s.CustomerId, s.CustomerType, s.CustomerTitle, s.OwnerType,
        s.CompletionShamsiDate, s.CompletionMiladiDate, s.Address, s.PhoneNumber, s.MobileNumber, s.Fax, s.ComplaintReceiver, s.ComplaintReceiverPersonelId,
        s.ComplaintPresenter, s.ComplaintPresenterJob, s.OrgUnitId, s.OrganizationUnitInText, s.ExpertPersonelId, s.ExpertOrManagerInText,
        s.Priority, s.ComplaintType, s.CommunicationChannelId, s.GeneralComplaintSubject, s.GeneralComplaintSubjectTitle, s.PartId, s.ComplaintSubjectId, s.AgencyId,
        s.CompressorFailure, s.ComplaintDescription, s.RaisedSubjects, s.RaisedSubjectIds, s.Repeatability, s.HasFinancialLoad, s.HasUrgencyHandling, s.HasComplexity,
        s.InformForReceiveComplaintId, s.ActionsDescription, s.ComplaintStatus, s.FinalHandlingDescription, s.HandlingResultIdsInText, s.InformComplainant, s.ComplaintConsent,
        s.ManagerDescription, s.FinalComplaintStatus, s.ComplaintClosingShamsiDate, s.ComplaintClosingMiladiDate, s.Comment, s.DelayReason, s.ComplaintMainSubject, s.MainSubjectsIds, s.MainSubjectsInText,
        s.HasComplaintSurvey, s.IsSurveyLinkSended, s.SurveyCompletionShamsiDate, s.SurveyInterviewerPersonelId, s.SurveyResponder, s.SurveyDescription,
        s.SurveyLinkSentShamsiDateTime, s.SurveyLinkSentMiladiDateTime, s.SurveyResponseShamsiDateTime, s.SurveyResponseMiladiDateTime,
        s.SurveySubjectsIds, s.SurveySubjectsInText, s.SurveyStatusId, s.CreatedById, s.CreatedByName, s.ModifiedById, @SeedUser,
        s.CreatedOnMiladiDateTime, s.CreatedOnShamsiDateTime, s.ModifiedDateMiladiDateTime, s.ModifiedDateShamsiDateTime, 1);

    UPDATE c SET ParentId = p.Id
    FROM Crm.CustomerComplaint c
    INNER JOIN #Src s ON s.HtsId = c.HtsId
    INNER JOIN Crm.CustomerComplaint p ON p.HtsId = s.OldParentId AND p.HtsId <> 0
    WHERE s.OldParentId IS NOT NULL;

    /* بازنویسی شناسه‌های چندتایی به شناسه سیستم جدید */
    UPDATE c SET
        OrganizationUnitIds = org.Csv,
        OrgUnitId = COALESCE(c.OrgUnitId, org.FirstId),
        ExpertOrManagerIds = exp.Csv,
        ExpertPersonelId = COALESCE(c.ExpertPersonelId, exp.FirstId),
        ViewersIds = vw.Csv,
        HandlingResultIds = hd.Csv
    FROM Crm.CustomerComplaint c
    INNER JOIN #Src s ON s.HtsId = c.HtsId
    OUTER APPLY (
        SELECT STRING_AGG(CAST(m.OrgUnitId AS nvarchar(20)), N',') WITHIN GROUP (ORDER BY TRY_CAST(j.[key] AS int)) AS Csv,
               MIN(m.OrgUnitId) AS FirstId
        FROM OPENJSON(CASE WHEN ISNULL(s.OldOrgUnitIds, N'') = N'' OR ISJSON(N'[' + REPLACE(s.OldOrgUnitIds, N' ', N'') + N']') <> 1 THEN N'[]'
                           ELSE N'[' + REPLACE(s.OldOrgUnitIds, N' ', N'') + N']' END) j
        INNER JOIN #OrgMap m ON m.OldOrgUnitId = TRY_CAST(j.[value] AS bigint)
    ) org
    OUTER APPLY (
        SELECT STRING_AGG(CAST(m.PersonelId AS nvarchar(20)), N',') WITHIN GROUP (ORDER BY TRY_CAST(j.[key] AS int)) AS Csv,
               MIN(m.PersonelId) AS FirstId
        FROM OPENJSON(CASE WHEN ISNULL(s.OldExpertIds, N'') = N'' OR ISJSON(N'[' + REPLACE(s.OldExpertIds, N' ', N'') + N']') <> 1 THEN N'[]'
                           ELSE N'[' + REPLACE(s.OldExpertIds, N' ', N'') + N']' END) j
        INNER JOIN #PersonMap m ON m.OldPersonelId = TRY_CAST(j.[value] AS bigint)
    ) exp
    OUTER APPLY (
        SELECT STRING_AGG(CAST(m.NewUserId AS nvarchar(20)), N',') WITHIN GROUP (ORDER BY TRY_CAST(j.[key] AS int)) AS Csv
        FROM OPENJSON(CASE WHEN ISNULL(s.OldViewerIds, N'') = N'' OR ISJSON(N'[' + REPLACE(s.OldViewerIds, N' ', N'') + N']') <> 1 THEN N'[]'
                           ELSE N'[' + REPLACE(s.OldViewerIds, N' ', N'') + N']' END) j
        INNER JOIN #ViewerMap m ON m.OldPersonelId = TRY_CAST(j.[value] AS bigint)
    ) vw
    OUTER APPLY (
        SELECT STRING_AGG(CAST(l.Id AS nvarchar(20)), N',') WITHIN GROUP (ORDER BY TRY_CAST(j.[key] AS int)) AS Csv
        FROM OPENJSON(CASE WHEN ISNULL(s.OldHandlingIds, N'') = N'' OR ISJSON(N'[' + REPLACE(s.OldHandlingIds, N' ', N'') + N']') <> 1 THEN N'[]'
                           ELSE N'[' + REPLACE(s.OldHandlingIds, N' ', N'') + N']' END) j
        INNER JOIN Crm.ComplaintLookupItem l ON l.HtsId = TRY_CAST(j.[value] AS bigint) AND l.LookupType = 179 AND l.HtsId <> 0
    ) hd;

    MERGE Crm.ComplaintSubjectOrganizationUnit AS t
    USING (
        SELECT so.Id AS HtsId, sub.Id AS SubjectId, om.OrgUnitId, so.OrganizationUnitId AS HtsOrgUnitId
        FROM [TMS].[TotalSystem].[dbo].[Crm_SubjectOrganizationUnit] so
        LEFT JOIN Crm.ComplaintLookupItem sub ON sub.HtsId = so.SubjectId AND sub.LookupType = 174 AND sub.HtsId <> 0
        LEFT JOIN #OrgMap om ON om.OldOrgUnitId = so.OrganizationUnitId
    ) AS s ON t.HtsId = s.HtsId AND t.HtsId <> 0
    WHEN MATCHED THEN UPDATE SET SubjectId = s.SubjectId, OrgUnitId = s.OrgUnitId, HtsOrgUnitId = s.HtsOrgUnitId, IsActive = 1
    WHEN NOT MATCHED THEN INSERT (HtsId, SubjectId, OrgUnitId, HtsOrgUnitId, IsActive, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
        CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime)
    VALUES (s.HtsId, s.SubjectId, s.OrgUnitId, s.HtsOrgUnitId, 1, 1, @SeedUser, 1, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi);

    INSERT INTO #Gap (Kind, HtsId, Detail)
    SELECT N'SubjectOrgUnit', so.Id, N'موضوع یا واحد جور نشد'
    FROM [TMS].[TotalSystem].[dbo].[Crm_SubjectOrganizationUnit] so
    LEFT JOIN Crm.ComplaintSubjectOrganizationUnit t ON t.HtsId = so.Id
    WHERE t.SubjectId IS NULL OR (so.OrganizationUnitId IS NOT NULL AND t.OrgUnitId IS NULL);

    MERGE Crm.CustomerComplaintContact AS t
    USING (
        SELECT c.Id AS HtsId, p.Id AS ComplaintId, c.PrePhoneNumber, c.PhoneNumber, c.MobileNumber, c.ResponserFullName, c.ResponserJob, c.Comment
        FROM [TMS].[TotalSystem].[dbo].[Crm_Customer_ComplaintContactInfo] c
        INNER JOIN Crm.CustomerComplaint p ON p.HtsId = c.CustomerComplaintId AND p.HtsId <> 0
    ) AS s ON t.HtsId = s.HtsId AND t.HtsId <> 0
    WHEN MATCHED THEN UPDATE SET ComplaintId = s.ComplaintId, PrePhoneNumber = s.PrePhoneNumber, PhoneNumber = s.PhoneNumber,
        MobileNumber = s.MobileNumber, ResponserFullName = s.ResponserFullName, ResponserJob = s.ResponserJob, Comment = s.Comment, IsActive = 1
    WHEN NOT MATCHED THEN INSERT (HtsId, ComplaintId, PrePhoneNumber, PhoneNumber, MobileNumber, ResponserFullName, ResponserJob, Comment, IsActive,
        CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime)
    VALUES (s.HtsId, s.ComplaintId, s.PrePhoneNumber, s.PhoneNumber, s.MobileNumber, s.ResponserFullName, s.ResponserJob, s.Comment, 1,
        1, @SeedUser, 1, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi);

    MERGE Crm.CustomerComplaintAction AS t
    USING (
        SELECT a.Id AS HtsId, p.Id AS ComplaintId, a.ActionDateInText AS ActionShamsiDate, CAST(a.ActionDate AS date) AS ActionMiladiDate,
               a.ActionDescription, a.NextActionDateInText AS NextActionShamsiDate, CAST(a.NextActionDate AS date) AS NextActionMiladiDate,
               a.NextActionDescription, a.Comment
        FROM [TMS].[TotalSystem].[dbo].[Crm_Customer_Complaint_Actions] a
        INNER JOIN Crm.CustomerComplaint p ON p.HtsId = a.ComplaintId AND p.HtsId <> 0
    ) AS s ON t.HtsId = s.HtsId AND t.HtsId <> 0
    WHEN MATCHED THEN UPDATE SET ComplaintId = s.ComplaintId, ActionShamsiDate = s.ActionShamsiDate, ActionMiladiDate = s.ActionMiladiDate,
        ActionDescription = s.ActionDescription, NextActionShamsiDate = s.NextActionShamsiDate, NextActionMiladiDate = s.NextActionMiladiDate,
        NextActionDescription = s.NextActionDescription, Comment = s.Comment, IsActive = 1
    WHEN NOT MATCHED THEN INSERT (HtsId, ComplaintId, ActionShamsiDate, ActionMiladiDate, ActionDescription, NextActionShamsiDate, NextActionMiladiDate,
        NextActionDescription, Comment, IsActive, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
        CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime)
    VALUES (s.HtsId, s.ComplaintId, s.ActionShamsiDate, s.ActionMiladiDate, s.ActionDescription, s.NextActionShamsiDate, s.NextActionMiladiDate,
        s.NextActionDescription, s.Comment, 1, 1, @SeedUser, 1, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi);

    MERGE Crm.CustomerComplaintSurveyAction AS t
    USING (
        SELECT a.Id AS HtsId, p.Id AS ComplaintId, a.ActionDateInText AS ActionShamsiDateTime, a.ActionDate AS ActionMiladiDateTime,
               a.ActionDescription, a.NextActionDate AS NextActionMiladiDate, a.NextActionDateInText AS NextActionShamsiDate,
               a.NextActionDescription, a.Comment
        FROM [TMS].[TotalSystem].[dbo].[Crm_CustomerComplaintSurveyAction] a
        INNER JOIN Crm.CustomerComplaint p ON p.HtsId = a.CustomerComplaintId AND p.HtsId <> 0
    ) AS s ON t.HtsId = s.HtsId AND t.HtsId <> 0
    WHEN MATCHED THEN UPDATE SET ComplaintId = s.ComplaintId, ActionDescription = s.ActionDescription, Comment = s.Comment, IsActive = 1
    WHEN NOT MATCHED THEN INSERT (HtsId, ComplaintId, ActionShamsiDateTime, ActionMiladiDateTime, ActionDescription, NextActionMiladiDate, NextActionShamsiDate,
        NextActionDescription, Comment, IsActive, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
        CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime)
    VALUES (s.HtsId, s.ComplaintId, s.ActionShamsiDateTime, s.ActionMiladiDateTime, s.ActionDescription, s.NextActionMiladiDate, s.NextActionShamsiDate,
        s.NextActionDescription, s.Comment, 1, 1, @SeedUser, 1, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi);

    MERGE Crm.CustomerComplaintSurveyResult AS t
    USING (
        SELECT r.Id AS HtsId, p.Id AS ComplaintId, r.QuestionId AS QuestionHtsId, r.AnswerId AS AnswerHtsId
        FROM [TMS].[TotalSystem].[dbo].[Crm_CustomerComplaintSurveyResult] r
        INNER JOIN Crm.CustomerComplaint p ON p.HtsId = r.CustomerComplaintId AND p.HtsId <> 0
    ) AS s ON t.HtsId = s.HtsId AND t.HtsId <> 0
    WHEN MATCHED THEN UPDATE SET ComplaintId = s.ComplaintId, QuestionHtsId = s.QuestionHtsId, AnswerHtsId = s.AnswerHtsId, IsActive = 1
    WHEN NOT MATCHED THEN INSERT (HtsId, ComplaintId, QuestionHtsId, AnswerHtsId, IsActive, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
        CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime)
    VALUES (s.HtsId, s.ComplaintId, s.QuestionHtsId, s.AnswerHtsId, 1, 1, @SeedUser, 1, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi);

    /* نقش کاربران از گروه‌ها و مجوز 126 */
    IF OBJECT_ID('tempdb..#RoleUser') IS NOT NULL DROP TABLE #RoleUser;
    CREATE TABLE #RoleUser (RoleName NVARCHAR(200), NewUserId BIGINT);
    INSERT INTO #RoleUser (RoleName, NewUserId)
    SELECT DISTINCT CASE m.UserGroup_FK
               WHEN 55 THEN N'Crm.Complaint.Manager'
               WHEN 292 THEN N'Crm.Complaint.Expert'
               WHEN 65 THEN N'Crm.Complaint.Read'
               WHEN 349 THEN N'Crm.Complaint.Observer'
               WHEN 350 THEN N'Crm.Complaint.Observer'
           END, um.NewUserId
    FROM [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
    INNER JOIN #UserMap um ON um.OldUserId = u.User_ID
    WHERE m.UserGroup_FK IN (55, 292, 65, 349, 350) AND u.IsActive = 1;

    INSERT INTO #RoleUser (RoleName, NewUserId)
    SELECT DISTINCT N'Crm.Complaint.OwnItems', um.NewUserId
    FROM [TMS].[TotalSystem].[dbo].[Gnr_PageAction_User] pau
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = pau.User_FK
    INNER JOIN #UserMap um ON um.OldUserId = u.User_ID
    WHERE pau.PageAction_FK = 2581 AND u.IsActive = 1
      AND NOT EXISTS (SELECT 1 FROM #RoleUser x WHERE x.NewUserId = um.NewUserId AND x.RoleName = N'Crm.Complaint.OwnItems');

    UPDATE nu
    SET Roles = JSON_MODIFY(CASE WHEN ISJSON(nu.Roles) = 1 THEN nu.Roles ELSE N'[]' END, N'append $', ru.RoleName),
        RoleIds = JSON_MODIFY(CASE WHEN ISJSON(nu.RoleIds) = 1 THEN nu.RoleIds ELSE N'[]' END, N'append $', r.Id),
        ModifiedById = 1, ModifiedByName = @SeedUser, ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
    FROM [system].[User] nu
    INNER JOIN (SELECT DISTINCT RoleName, NewUserId FROM #RoleUser) ru ON ru.NewUserId = nu.Id
    INNER JOIN [system].[Role] r ON r.Name = ru.RoleName
    WHERE NOT EXISTS (
        SELECT 1 FROM OPENJSON(CASE WHEN ISJSON(nu.RoleIds) = 1 THEN nu.RoleIds ELSE N'[]' END) j
        WHERE TRY_CAST(j.[value] AS bigint) = r.Id);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT Kind, COUNT(*) AS Cnt FROM #Gap GROUP BY Kind ORDER BY Kind;
SELECT TOP (200) Kind, HtsId, Detail FROM #Gap ORDER BY Kind, HtsId;
SELECT COUNT(*) AS ComplaintCount FROM Crm.CustomerComplaint;
SELECT COUNT(*) AS ActionCount FROM Crm.CustomerComplaintAction;
SELECT COUNT(*) AS ContactCount FROM Crm.CustomerComplaintContact;
