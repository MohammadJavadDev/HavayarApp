/*
SyncAfterSalesPhase5FromTotalSystem.sql — ServiceRequest header + detail + expert mission
Attachment binaries: follow phase-1 UNC pattern after header sync.

شماره اندیکاتور = Sale.ServiceRequest.Id = HTS Sale_ServiceRequest.ServiceRequest_ID (HtsId).
New rows must be inserted with IDENTITY_INSERT Id = HtsId so the indicator matches HTS.
One-time remap of existing wrong Ids: Remap_SaleServiceRequest_IdToHtsIndicator.sql
*/
SET NOCOUNT ON; SET XACT_ABORT ON;
DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'sync-after-sales-phase5';
IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
    BEGIN RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1); RETURN; END;

BEGIN TRY
BEGIN TRANSACTION;

SELECT CAST(ou.User_ID AS BIGINT) AS OldUserId, w.Id AS NewUserId,
       LEFT(COALESCE(NULLIF(LTRIM(RTRIM(ou.FullName)), N''), NULLIF(LTRIM(RTRIM(w.NameFa)), N''), NULLIF(LTRIM(RTRIM(w.Name)), N''), w.Username), 150) AS NewUserName
INTO #UserMap FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
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

MERGE Sale.ServiceRequest AS t
USING (
    SELECT CAST(s.ServiceRequest_ID AS BIGINT) AS HtsId, c.Id AS CustomerId, s.CustomerPhoneFromHistory,
           s.ContactDate AS ContactMiladiDate, s.ContactDate_Shamsi AS ContactShamsiDate,
           a.Id AS CustomerAddressId, rp.Id AS ResponsiblePersonelId, dz.Id AS DispatchZoneId,
           s.ExpertMission, s.AllProduct_Name AS AllProductName, s.Unreal,
           s.DispachDate AS DispatchMiladiDate, s.DispachDateInText AS DispatchShamsiDate,
           s.CustomerAgentName, s.CustomerAgentJobPosition, s.CustomerAgentTell, s.CustomerAgentEmailAddress,
           s.Comment, s.IsWainigToSend AS IsWaitingToSend, s.RepairRequestsIds AS LegacyRepairRequestIds,
           umCreate.NewUserId AS CreatedById,
           LEFT(COALESCE(NULLIF(LTRIM(RTRIM(htsUser.FullName)), N''), umCreate.NewUserName, N'sync-after-sales-phase5'), 150) AS CreatedByName,
           cd.CreatedMiladi AS CreatedOnMiladiDateTime, cd.CreatedShamsi AS CreatedOnShamsiDateTime,
           org.Id AS CreatedOrgUnitId
    FROM [TMS].[TotalSystem].[dbo].[Sale_ServiceRequest] s
    LEFT JOIN SLS.Customer c ON c.HtsId = s.Customer_FK
    LEFT JOIN SLS.CustomerAddress a ON a.HtsId = s.Customer_Address_FK
    LEFT JOIN #UserMap umCreate ON umCreate.OldUserId = CAST(s.CreatedUser_FK AS BIGINT)
    LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] htsUser ON htsUser.User_ID = s.CreatedUser_FK
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
    ) org
    LEFT JOIN [TMS].[TotalSystem].[dbo].[HRM_Personel] hp ON hp.Personel_ID = s.Responsible_Zone_FK
    OUTER APPLY (
        SELECT TOP (1) p.Id
        FROM Hcm.Personel p
        WHERE (
                hp.PrsCode IS NOT NULL
                AND LTRIM(RTRIM(CAST(hp.PrsCode AS nvarchar(50)))) <> N''
                AND LTRIM(RTRIM(CAST(hp.PrsCode AS nvarchar(50)))) NOT IN (N'999999123', N'999999092', N'-1111', N'-1112')
                AND LTRIM(RTRIM(p.Code)) = LTRIM(RTRIM(CAST(hp.PrsCode AS nvarchar(50))))
            )
           OR (
                hp.NationalID IS NOT NULL AND LTRIM(RTRIM(hp.NationalID)) <> N''
                AND REPLACE(LTRIM(RTRIM(ISNULL(p.NationalID, N''))), N' ', N'')
                    = REPLACE(LTRIM(RTRIM(hp.NationalID)), N' ', N'')
            )
           OR (
                hp.Hamkaran_Personel_FK IS NOT NULL AND hp.Hamkaran_Personel_FK > 0
                AND p.HamkaranId = CAST(hp.Hamkaran_Personel_FK AS bigint)
            )
        ORDER BY
            CASE WHEN LTRIM(RTRIM(ISNULL(p.Code, N''))) = LTRIM(RTRIM(CAST(ISNULL(hp.PrsCode, N'') AS nvarchar(50)))) THEN 0 ELSE 1 END,
            CASE WHEN REPLACE(LTRIM(RTRIM(ISNULL(p.NationalID, N''))), N' ', N'')
                      = REPLACE(LTRIM(RTRIM(ISNULL(hp.NationalID, N''))), N' ', N'') THEN 0 ELSE 1 END
    ) rp
    LEFT JOIN Crm.Zone dz ON dz.HtsId = s.DispachZoneId
) AS s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET
    t.CustomerId=s.CustomerId, t.CustomerPhoneFromHistory=s.CustomerPhoneFromHistory,
    t.ContactMiladiDate=s.ContactMiladiDate, t.ContactShamsiDate=s.ContactShamsiDate,
    t.CustomerAddressId=s.CustomerAddressId, t.ResponsiblePersonelId=s.ResponsiblePersonelId, t.DispatchZoneId=s.DispatchZoneId,
    t.ExpertMission=s.ExpertMission, t.AllProductName=s.AllProductName, t.Unreal=s.Unreal,
    t.DispatchMiladiDate=s.DispatchMiladiDate, t.DispatchShamsiDate=s.DispatchShamsiDate,
    t.CustomerAgentName=s.CustomerAgentName, t.CustomerAgentJobPosition=s.CustomerAgentJobPosition,
    t.CustomerAgentTell=s.CustomerAgentTell, t.CustomerAgentEmailAddress=s.CustomerAgentEmailAddress,
    t.Comment=s.Comment, t.IsWaitingToSend=s.IsWaitingToSend, t.LegacyRepairRequestIds=s.LegacyRepairRequestIds,
    t.ModifiedById=1, t.ModifiedByName=@SeedUser, t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
;

-- Insert missing headers with Id = HtsId (شماره اندیکاتور must match HTS ServiceRequest_ID)
SET IDENTITY_INSERT Sale.ServiceRequest ON;
INSERT INTO Sale.ServiceRequest
    (Id, HtsId, CustomerId, CustomerPhoneFromHistory, ContactMiladiDate, ContactShamsiDate, CustomerAddressId, ResponsiblePersonelId, DispatchZoneId,
     ExpertMission, AllProductName, Unreal, DispatchMiladiDate, DispatchShamsiDate, CustomerAgentName, CustomerAgentJobPosition,
     CustomerAgentTell, CustomerAgentEmailAddress, Comment, IsWaitingToSend, LegacyRepairRequestIds, CreatedOrgUnitId,
     CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
SELECT
    s.HtsId, s.HtsId, s.CustomerId, s.CustomerPhoneFromHistory, s.ContactMiladiDate, s.ContactShamsiDate, s.CustomerAddressId, s.ResponsiblePersonelId, s.DispatchZoneId,
    s.ExpertMission, s.AllProductName, s.Unreal, s.DispatchMiladiDate, s.DispatchShamsiDate, s.CustomerAgentName, s.CustomerAgentJobPosition,
    s.CustomerAgentTell, s.CustomerAgentEmailAddress, s.Comment, s.IsWaitingToSend, s.LegacyRepairRequestIds, s.CreatedOrgUnitId,
    COALESCE(s.CreatedById, 1), COALESCE(s.CreatedByName, @SeedUser), COALESCE(s.CreatedOnMiladiDateTime, @Now), COALESCE(s.CreatedOnShamsiDateTime, @NowShamsi), 1, @SeedUser, @Now, @NowShamsi, 1
FROM (
    SELECT CAST(s.ServiceRequest_ID AS BIGINT) AS HtsId, c.Id AS CustomerId, s.CustomerPhoneFromHistory,
           s.ContactDate AS ContactMiladiDate, s.ContactDate_Shamsi AS ContactShamsiDate,
           a.Id AS CustomerAddressId, rp.Id AS ResponsiblePersonelId, dz.Id AS DispatchZoneId,
           s.ExpertMission, s.AllProduct_Name AS AllProductName, s.Unreal,
           s.DispachDate AS DispatchMiladiDate, s.DispachDateInText AS DispatchShamsiDate,
           s.CustomerAgentName, s.CustomerAgentJobPosition, s.CustomerAgentTell, s.CustomerAgentEmailAddress,
           s.Comment, s.IsWainigToSend AS IsWaitingToSend, s.RepairRequestsIds AS LegacyRepairRequestIds,
           umCreate.NewUserId AS CreatedById,
           LEFT(COALESCE(NULLIF(LTRIM(RTRIM(htsUser.FullName)), N''), umCreate.NewUserName, N'sync-after-sales-phase5'), 150) AS CreatedByName,
           cd.CreatedMiladi AS CreatedOnMiladiDateTime, cd.CreatedShamsi AS CreatedOnShamsiDateTime,
           org.Id AS CreatedOrgUnitId
    FROM [TMS].[TotalSystem].[dbo].[Sale_ServiceRequest] s
    LEFT JOIN SLS.Customer c ON c.HtsId = s.Customer_FK
    LEFT JOIN SLS.CustomerAddress a ON a.HtsId = s.Customer_Address_FK
    LEFT JOIN #UserMap umCreate ON umCreate.OldUserId = CAST(s.CreatedUser_FK AS BIGINT)
    LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] htsUser ON htsUser.User_ID = s.CreatedUser_FK
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
    ) org
    LEFT JOIN [TMS].[TotalSystem].[dbo].[HRM_Personel] hp ON hp.Personel_ID = s.Responsible_Zone_FK
    OUTER APPLY (
        SELECT TOP (1) p.Id
        FROM Hcm.Personel p
        WHERE (
                hp.PrsCode IS NOT NULL
                AND LTRIM(RTRIM(CAST(hp.PrsCode AS nvarchar(50)))) <> N''
                AND LTRIM(RTRIM(CAST(hp.PrsCode AS nvarchar(50)))) NOT IN (N'999999123', N'999999092', N'-1111', N'-1112')
                AND LTRIM(RTRIM(p.Code)) = LTRIM(RTRIM(CAST(hp.PrsCode AS nvarchar(50))))
            )
           OR (
                hp.NationalID IS NOT NULL AND LTRIM(RTRIM(hp.NationalID)) <> N''
                AND REPLACE(LTRIM(RTRIM(ISNULL(p.NationalID, N''))), N' ', N'')
                    = REPLACE(LTRIM(RTRIM(hp.NationalID)), N' ', N'')
            )
           OR (
                hp.Hamkaran_Personel_FK IS NOT NULL AND hp.Hamkaran_Personel_FK > 0
                AND p.HamkaranId = CAST(hp.Hamkaran_Personel_FK AS bigint)
            )
        ORDER BY
            CASE WHEN LTRIM(RTRIM(ISNULL(p.Code, N''))) = LTRIM(RTRIM(CAST(ISNULL(hp.PrsCode, N'') AS nvarchar(50)))) THEN 0 ELSE 1 END,
            CASE WHEN REPLACE(LTRIM(RTRIM(ISNULL(p.NationalID, N''))), N' ', N'')
                      = REPLACE(LTRIM(RTRIM(ISNULL(hp.NationalID, N''))), N' ', N'') THEN 0 ELSE 1 END
    ) rp
    LEFT JOIN Crm.Zone dz ON dz.HtsId = s.DispachZoneId
) AS s
WHERE NOT EXISTS (SELECT 1 FROM Sale.ServiceRequest t WHERE t.HtsId = s.HtsId AND t.HtsId <> 0);
SET IDENTITY_INSERT Sale.ServiceRequest OFF;
DBCC CHECKIDENT ('Sale.ServiceRequest', RESEED) WITH NO_INFOMSGS;

MERGE Sale.ServiceRequestDetail AS t
USING (
    SELECT CAST(s.ServiceRequest_Detail_ID AS BIGINT) AS HtsId, sr.Id AS ServiceRequestId, s.RequestType_FK AS RequestType,
           od.Id AS OrderDetailId, ser.Id AS OrderDetailSerialId, s.NoticeDate AS NoticeShamsiDate, s.NoticeTime
    FROM [TMS].[TotalSystem].[dbo].[Sale_ServiceRequest_Detail] s
    LEFT JOIN Sale.ServiceRequest sr ON sr.HtsId = s.ServiceRequest_FK
    LEFT JOIN Sale.OrderDetail od ON od.HtsId = s.SaleOrderDetail_FK
    LEFT JOIN Sale.OrderDetailSerial ser ON ser.HtsId = s.SaleOrderDetailSerial_FK
) AS s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.ServiceRequestId=s.ServiceRequestId, t.RequestType=s.RequestType, t.OrderDetailId=s.OrderDetailId,
    t.OrderDetailSerialId=s.OrderDetailSerialId, t.NoticeShamsiDate=s.NoticeShamsiDate, t.NoticeTime=s.NoticeTime,
    t.ModifiedById=1, t.ModifiedByName=@SeedUser, t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED THEN INSERT (HtsId, ServiceRequestId, RequestType, OrderDetailId, OrderDetailSerialId, NoticeShamsiDate, NoticeTime,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
VALUES (s.HtsId, s.ServiceRequestId, s.RequestType, s.OrderDetailId, s.OrderDetailSerialId, s.NoticeShamsiDate, s.NoticeTime,
    1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);

COMMIT;
PRINT N'Phase 5 sync committed';
END TRY
BEGIN CATCH IF @@TRANCOUNT > 0 ROLLBACK; THROW; END CATCH;
