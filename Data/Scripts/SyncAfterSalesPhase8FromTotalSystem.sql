-- Phase 8 RepairRequest (link ServiceRequest via LegacyRepairRequestIds / HtsId)
-- MERGE ON HtsId = HTS RepairRequest_ID. Do not filter by max(Id), date, or required Customer/Product
-- joins (LEFT JOIN only). Havayar Id is IDENTITY and is NOT HTS RepairRequest_ID — list grids must
-- default-sort by IdNumber/HtsId, not Havayar Id, or 7016/7015 appear after 7017/7007.
-- DlId: Acc_DL.Acc_DL_ID = HTS DL_FK then FIN.DL.Title = Acc_DLTitle (not HamkaranId = DL_FK).
-- Header audit: CreatedUser_FK / UpdatedUserId + Shamsi CreatedDate/Time / UpdatedDateInText.
SET NOCOUNT ON; SET XACT_ABORT ON;
DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'sync-after-sales-phase8';
IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
    BEGIN RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1); RETURN; END;
BEGIN TRY
BEGIN TRANSACTION;
SELECT CAST(ou.User_ID AS BIGINT) AS OldUserId, w.Id AS NewUserId,
       LEFT(COALESCE(NULLIF(LTRIM(RTRIM(w.NameFa)), N''), NULLIF(LTRIM(RTRIM(w.Name)), N''), w.Username), 150) AS NewUserName
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

MERGE Rpr.RepairRequest AS t
USING (
    SELECT CAST(s.RepairRequest_ID AS BIGINT) AS HtsId, s.IDNumber, c.Id AS CustomerId, a.Id AS CustomerAddressId, s.CustomerAddressManual,
           p.Id AS ProductId, s.Serial, z.Id AS ZoneId, s.IsGuarantee, s.Unrepairable, s.NeedForSendToEmployer,
           s.EntryDate AS EntryMiladiDate, s.EntryDate_Shamsi AS EntryShamsiDate, s.CustomerNeedDate AS CustomerNeedMiladiDate,
           s.CustomerNeedDate_Shamsi AS CustomerNeedShamsiDate, s.ExitDate AS ExitMiladiDate, s.ExitDate_Shamsi AS ExitShamsiDate,
           s.RepairRequestStatus_FK AS Status, umc.NewUserId AS ConfirmerId, umr.NewUserId AS ResponsibleId, s.HasDelay, s.FailureReason,
           s.RepairDescription, s.Comment, s.HasContractor, dl.Id AS DlId,
           umCreate.NewUserId AS CreatedById, umCreate.NewUserName AS CreatedByName,
           umEdit.NewUserId AS ModifiedById, umEdit.NewUserName AS ModifiedByName,
           cd.CreatedMiladi AS CreatedOnMiladiDateTime, cd.CreatedShamsi AS CreatedOnShamsiDateTime,
           md.ModifiedMiladi AS ModifiedDateMiladiDateTime, md.ModifiedShamsi AS ModifiedDateShamsiDateTime
    FROM [TMS].[TotalSystem].[dbo].[Rpr_RepairRequest] s
    LEFT JOIN SLS.Customer c ON c.HtsId = s.Customer_FK AND ISNULL(c.HtsId,0) <> 0
    LEFT JOIN SLS.CustomerAddress a ON a.HtsId = s.CustomerAddressId AND ISNULL(a.HtsId,0) <> 0
    LEFT JOIN Inv.Part p ON p.HtsId = s.Product_FK
    LEFT JOIN Crm.Zone z ON z.HtsId = s.Zone_FK
    LEFT JOIN #UserMap umc ON umc.OldUserId = s.ConfirmerUser_FK
    LEFT JOIN #UserMap umr ON umr.OldUserId = s.ResponsiblePersonnel_FK
    LEFT JOIN #UserMap umCreate ON umCreate.OldUserId = s.CreatedUser_FK
    LEFT JOIN #UserMap umEdit ON umEdit.OldUserId = s.UpdatedUserId
    OUTER APPLY (
        SELECT TOP 1 d.Id
        FROM [TMS].[TotalSystem].[dbo].[Acc_DL] ad
        INNER JOIN FIN.DL d ON LTRIM(RTRIM(d.Title)) = LTRIM(RTRIM(ad.Acc_DLTitle))
        WHERE ad.Acc_DL_ID = s.DL_FK
        ORDER BY CASE WHEN d.Code = CAST(ad.Hamkaran_Acc_DL_FK AS nvarchar(64)) THEN 0 ELSE 1 END, d.Id
    ) dl
    CROSS APPLY (
        SELECT
            LEFT(LTRIM(RTRIM(s.CreatedDate)) + CASE WHEN NULLIF(LTRIM(RTRIM(s.CreatedTime)), N'') IS NULL THEN N'' ELSE N' ' + LTRIM(RTRIM(s.CreatedTime)) END, 30) AS CreatedShamsi,
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
                CASE WHEN s.UpdatedDateInText LIKE N'____/__/__ __:__%'
                     THEN dbo.fn_ShamsiToMiladiDateTime(LEFT(s.UpdatedDateInText, 10), SUBSTRING(s.UpdatedDateInText, 12, 5), N'/', N':')
                     WHEN NULLIF(LTRIM(RTRIM(s.UpdatedDateInText)), N'') IS NOT NULL
                     THEN CAST(dbo.fn_ShamsiToMiladiDate(LEFT(LTRIM(RTRIM(s.UpdatedDateInText)), 10), N'/') AS DATETIME2)
                END,
                cd.CreatedMiladi
            ) AS ModifiedMiladi
    ) md
) AS s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.IdNumber=s.IdNumber, t.CustomerId=s.CustomerId, t.CustomerAddressId=s.CustomerAddressId, t.CustomerAddressManual=s.CustomerAddressManual,
    t.ProductId=s.ProductId, t.Serial=s.Serial, t.ZoneId=s.ZoneId, t.IsGuarantee=s.IsGuarantee, t.Unrepairable=s.Unrepairable, t.NeedForSendToEmployer=s.NeedForSendToEmployer,
    t.EntryMiladiDate=s.EntryMiladiDate, t.EntryShamsiDate=s.EntryShamsiDate, t.CustomerNeedMiladiDate=s.CustomerNeedMiladiDate, t.CustomerNeedShamsiDate=s.CustomerNeedShamsiDate,
    t.ExitMiladiDate=s.ExitMiladiDate, t.ExitShamsiDate=s.ExitShamsiDate, t.Status=s.Status, t.ConfirmerId=s.ConfirmerId, t.ResponsibleId=s.ResponsibleId,
    t.HasDelay=s.HasDelay, t.FailureReason=s.FailureReason, t.RepairDescription=s.RepairDescription, t.Comment=s.Comment, t.HasContractor=s.HasContractor,
    t.DlId=s.DlId, t.CreatedById=s.CreatedById, t.CreatedByName=s.CreatedByName,
    t.CreatedOnMiladiDateTime=s.CreatedOnMiladiDateTime, t.CreatedOnShamsiDateTime=s.CreatedOnShamsiDateTime,
    t.ModifiedById=s.ModifiedById, t.ModifiedByName=s.ModifiedByName,
    t.ModifiedDateMiladiDateTime=s.ModifiedDateMiladiDateTime, t.ModifiedDateShamsiDateTime=s.ModifiedDateShamsiDateTime
WHEN NOT MATCHED THEN INSERT (HtsId, IdNumber, CustomerId, CustomerAddressId, CustomerAddressManual, ProductId, Serial, ZoneId, IsGuarantee, Unrepairable, NeedForSendToEmployer,
    EntryMiladiDate, EntryShamsiDate, CustomerNeedMiladiDate, CustomerNeedShamsiDate, ExitMiladiDate, ExitShamsiDate, Status, ConfirmerId, ResponsibleId, HasDelay,
    FailureReason, RepairDescription, Comment, HasContractor, DlId,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
VALUES (s.HtsId, s.IdNumber, s.CustomerId, s.CustomerAddressId, s.CustomerAddressManual, s.ProductId, s.Serial, s.ZoneId, s.IsGuarantee, s.Unrepairable, s.NeedForSendToEmployer,
    s.EntryMiladiDate, s.EntryShamsiDate, s.CustomerNeedMiladiDate, s.CustomerNeedShamsiDate, s.ExitMiladiDate, s.ExitShamsiDate, s.Status, s.ConfirmerId, s.ResponsibleId, s.HasDelay,
    s.FailureReason, s.RepairDescription, s.Comment, s.HasContractor, s.DlId,
    COALESCE(s.CreatedById, 1), COALESCE(s.CreatedByName, @SeedUser), COALESCE(s.CreatedOnMiladiDateTime, @Now), COALESCE(s.CreatedOnShamsiDateTime, @NowShamsi),
    s.ModifiedById, s.ModifiedByName, COALESCE(s.ModifiedDateMiladiDateTime, @Now), COALESCE(s.ModifiedDateShamsiDateTime, @NowShamsi), 1);
COMMIT; PRINT N'Phase 8 sync committed';
END TRY BEGIN CATCH IF @@TRANCOUNT > 0 ROLLBACK; THROW; END CATCH;
