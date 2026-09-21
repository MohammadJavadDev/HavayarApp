/*
  Fix_RepairRequest_CustomerAndHeaderFks.sql
  Phase8 JOIN کرد SLS.Customer.HtsId = HTS Customer_FK قبل از widen شدن Customer.HtsId.
  بعد از WidenCustomerHtsId، CustomerId / CustomerAddressId خالی ماندند.
  این اسکریپت همان نگاشت را دوباره SET می‌کند (بدون ساخت مشتری جعلی)
  و ردیف‌های هدر HTS که در Havayar نیستند را MERGE می‌کند (الان RepairRequest_ID=3202).
  Encoding: UTF-8 with BOM. Apply: sqlcmd -C -b -I -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'fix-repairrequest-customer-fks';
DECLARE @Rc INT;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1);
    RETURN;
END;

PRINT N'=== BEFORE RepairRequest FK fill ===';
SELECT
    COUNT(*) AS Total,
    SUM(CASE WHEN CustomerId IS NULL THEN 1 ELSE 0 END) AS CustomerNull,
    SUM(CASE WHEN CustomerAddressId IS NULL THEN 1 ELSE 0 END) AS AddressNull,
    SUM(CASE WHEN CustomerAgencyId IS NULL THEN 1 ELSE 0 END) AS AgencyNull
FROM Rpr.RepairRequest;

BEGIN TRY
    BEGIN TRANSACTION;

    PRINT N'=== [1] Rematch CustomerId via Customer.HtsId = HTS Customer_FK ===';
    UPDATE rr
    SET rr.CustomerId = c.Id
    FROM Rpr.RepairRequest rr
    INNER JOIN [TMS].[TotalSystem].[dbo].[Rpr_RepairRequest] s
        ON s.RepairRequest_ID = rr.HtsId
    INNER JOIN SLS.Customer c
        ON c.HtsId = s.Customer_FK AND c.HtsId <> 0
    WHERE rr.CustomerId IS NULL OR rr.CustomerId <> c.Id;
    SET @Rc = @@ROWCOUNT;
    PRINT N'  CustomerId rematched: ' + CAST(@Rc AS NVARCHAR(20));

    PRINT N'=== [2] Rematch CustomerAddressId via CustomerAddress.HtsId ===';
    UPDATE rr
    SET rr.CustomerAddressId = a.Id
    FROM Rpr.RepairRequest rr
    INNER JOIN [TMS].[TotalSystem].[dbo].[Rpr_RepairRequest] s
        ON s.RepairRequest_ID = rr.HtsId
    INNER JOIN SLS.CustomerAddress a
        ON a.HtsId = s.CustomerAddressId AND ISNULL(a.HtsId, 0) <> 0
    WHERE rr.CustomerAddressId IS NULL OR rr.CustomerAddressId <> a.Id;
    SET @Rc = @@ROWCOUNT;
    PRINT N'  CustomerAddressId rematched: ' + CAST(@Rc AS NVARCHAR(20));

    PRINT N'=== [3] Rematch CustomerAgencyId via Customer.HtsId = CustomerAgencyAddressId ===';
    UPDATE rr
    SET rr.CustomerAgencyId = ag.Id
    FROM Rpr.RepairRequest rr
    INNER JOIN [TMS].[TotalSystem].[dbo].[Rpr_RepairRequest] s
        ON s.RepairRequest_ID = rr.HtsId
    INNER JOIN SLS.Customer ag
        ON ag.HtsId = s.CustomerAgencyAddressId AND ag.HtsId <> 0
    WHERE rr.CustomerAgencyId IS NULL OR rr.CustomerAgencyId <> ag.Id;
    SET @Rc = @@ROWCOUNT;
    PRINT N'  CustomerAgencyId rematched: ' + CAST(@Rc AS NVARCHAR(20));

    PRINT N'=== [4] MERGE missing HTS headers (Id via IDENTITY, HtsId = RepairRequest_ID) ===';
    SELECT CAST(ou.User_ID AS BIGINT) AS OldUserId, w.Id AS NewUserId,
           LEFT(COALESCE(NULLIF(LTRIM(RTRIM(w.NameFa)), N''), NULLIF(LTRIM(RTRIM(w.Name)), N''), w.Username), 150) AS NewUserName
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

    MERGE Rpr.RepairRequest AS t
    USING (
        SELECT
            CAST(s.RepairRequest_ID AS BIGINT) AS HtsId,
            s.IDNumber AS IdNumber,
            c.Id AS CustomerId,
            a.Id AS CustomerAddressId,
            s.CustomerAddressManual,
            p.Id AS ProductId,
            s.Serial,
            z.Id AS ZoneId,
            s.IsGuarantee,
            s.Unrepairable,
            s.NeedForSendToEmployer,
            CAST(s.EntryDate AS DATETIME2) AS EntryMiladiDate,
            s.EntryDate_Shamsi AS EntryShamsiDate,
            CAST(s.CustomerNeedDate AS DATETIME2) AS CustomerNeedMiladiDate,
            s.CustomerNeedDate_Shamsi AS CustomerNeedShamsiDate,
            CAST(s.ExitDate AS DATETIME2) AS ExitMiladiDate,
            CAST(s.ExitDate_Shamsi AS NVARCHAR(MAX)) AS ExitShamsiDate,
            s.RepairRequestStatus_FK AS Status,
            umc.NewUserId AS ConfirmerId,
            umr.NewUserId AS ResponsibleId,
            s.HasDelay,
            s.FailureReason,
            s.RepairDescription,
            s.Comment,
            s.HasContractor,
            umz.NewUserId AS ZoneSupervisorId,
            ag.Id AS CustomerAgencyId,
            s.Customer_Phone AS CustomerPhone,
            s.CustomerPhoneFromHistory,
            s.RepresentationReception,
            s.DestinationAfterRepair_FK AS DestinationAfterRepair,
            s.DestinationAddress,
            s.JobDescriptionId AS JobDescription,
            s.PreFactor_Number AS PreFactorNumber,
            s.LoanForm_Number AS LoanFormNumber,
            s.DeliveryForm_Number AS DeliveryFormNumber,
            s.SafekeepingReturnFormNumber,
            CAST(s.SafekeepingReturnDate AS DATETIME2) AS SafekeepingReturnMiladiDate,
            s.SafekeepingReturnDateInText AS SafekeepingReturnShamsiDate,
            s.HasContentProductionImaging,
            s.IsHavayarEquipment,
            pe.Id AS InspectionPersonelId,
            s.ExpertIds,
            s.ExpertsInText,
            st.Id AS StationId,
            dl.Id AS DlId,
            cc.Id AS CostCenterId,
            CAST(s.Confirm_DateLatin AS DATETIME2) AS ConfirmMiladiDate,
            CAST(s.Confirm_Date AS NVARCHAR(30)) AS ConfirmShamsiDate,
            CAST(s.Confirm_Time AS NVARCHAR(5)) AS ConfirmTime,
            CAST(s.Confirm_Date2 AS DATETIME2) AS Confirm2MiladiDate,
            s.Confirm_Date2InText AS Confirm2ShamsiDate,
            s.IsConfirmed2,
            CAST(s.StartDate AS DATETIME2) AS StartMiladiDate,
            s.StartDateInText AS StartShamsiDate,
            CAST(s.EndDate AS DATETIME2) AS EndMiladiDate,
            s.EndDateInText AS EndShamsiDate,
            CAST(s.RepairEnteranceDate AS DATETIME2) AS RepairEntranceMiladiDate,
            s.RepairEnteranceDateInText AS RepairEntranceShamsiDate,
            CAST(s.DeliveryDate_Warehouse AS DATETIME2) AS WarehouseDeliveryMiladiDate,
            s.DeliveryDate_Warehouse_Shamsi AS WarehouseDeliveryShamsiDate,
            CAST(s.AgreedDate AS DATETIME2) AS AgreedMiladiDate,
            s.AgreedDateInText AS AgreedShamsiDate,
            CAST(s.AgreedDate2 AS DATETIME2) AS Agreed2MiladiDate,
            s.AgreedDate2InText AS Agreed2ShamsiDate,
            CAST(s.AgreedDate3 AS DATETIME2) AS Agreed3MiladiDate,
            s.AgreedDate3InText AS Agreed3ShamsiDate,
            s.DeliveringRecipient,
            CAST(s.BuyRequestDate AS DATETIME2) AS BuyRequestMiladiDate,
            s.BuyRequestDateInText AS BuyRequestShamsiDate,
            CAST(s.SupplyDate AS DATETIME2) AS SupplyMiladiDate,
            s.SupplyDateInText AS SupplyShamsiDate,
            CAST(s.QcRequestDate AS DATETIME2) AS QcRequestMiladiDate,
            s.QcRequestDateInText AS QcRequestShamsiDate,
            s.RepairRequestPartFractionsIds AS PartFractionIds,
            s.RepairRequestPartFractionsInText AS PartFractionsInText,
            CAST(s.PreCheckDate AS DATETIME2) AS PreCheckMiladiDate,
            s.PreCheckDateInText AS PreCheckShamsiDate,
            CAST(s.PreCheckDate2 AS DATETIME2) AS PreCheck2MiladiDate,
            s.PreCheckDate2InText AS PreCheck2ShamsiDate,
            s.IsPreChecked2,
            CAST(s.FinancialProposalDate AS DATETIME2) AS FinancialProposalMiladiDate,
            s.FinancialProposalDateInText AS FinancialProposalShamsiDate,
            CAST(s.FinancialProposalDate2 AS DATETIME2) AS FinancialProposal2MiladiDate,
            s.FinancialProposalDate2InText AS FinancialProposal2ShamsiDate,
            s.IsApprovedFinancialProposalDate2 AS IsApprovedFinancialProposal2,
            s.IsSentFinancialProposal,
            CAST(s.FinancialProposalSentDate AS DATETIME2) AS FinancialProposalSentMiladiDate,
            s.FinancialProposalSentDateInText AS FinancialProposalSentShamsiDate,
            umCreate.NewUserId AS CreatedById,
            umCreate.NewUserName AS CreatedByName,
            umEdit.NewUserId AS ModifiedById,
            umEdit.NewUserName AS ModifiedByName,
            cd.CreatedMiladi AS CreatedOnMiladiDateTime,
            cd.CreatedShamsi AS CreatedOnShamsiDateTime,
            md.ModifiedMiladi AS ModifiedDateMiladiDateTime,
            md.ModifiedShamsi AS ModifiedDateShamsiDateTime
        FROM [TMS].[TotalSystem].[dbo].[Rpr_RepairRequest] s
        LEFT JOIN SLS.Customer c ON c.HtsId = s.Customer_FK AND c.HtsId <> 0
        LEFT JOIN SLS.CustomerAddress a ON a.HtsId = s.CustomerAddressId AND ISNULL(a.HtsId, 0) <> 0
        LEFT JOIN Inv.Part p ON p.HtsId = s.Product_FK
        LEFT JOIN Crm.Zone z ON z.HtsId = s.Zone_FK
        LEFT JOIN #UserMap umc ON umc.OldUserId = s.ConfirmerUser_FK
        LEFT JOIN #UserMap umr ON umr.OldUserId = s.ResponsiblePersonnel_FK
        LEFT JOIN #UserMap umz ON umz.OldUserId = s.ZoneSupervisor_FK
        LEFT JOIN #UserMap umCreate ON umCreate.OldUserId = s.CreatedUser_FK
        LEFT JOIN #UserMap umEdit ON umEdit.OldUserId = s.UpdatedUserId
        LEFT JOIN SLS.Customer ag ON ag.HtsId = s.CustomerAgencyAddressId AND ISNULL(ag.HtsId, 0) <> 0
        LEFT JOIN Hcm.Personel pe ON pe.HamkaranId = s.InspectionPersonel_FK
        LEFT JOIN Rpr.Station st ON st.HtsId = s.StationId
        OUTER APPLY (
            SELECT TOP 1 d.Id
            FROM [TMS].[TotalSystem].[dbo].[Acc_DL] ad
            INNER JOIN FIN.DL d ON LTRIM(RTRIM(d.Title)) = LTRIM(RTRIM(ad.Acc_DLTitle))
            WHERE ad.Acc_DL_ID = s.DL_FK
            ORDER BY CASE WHEN d.Code = CAST(ad.Hamkaran_Acc_DL_FK AS nvarchar(64)) THEN 0 ELSE 1 END, d.Id
        ) dl
        OUTER APPLY (
            SELECT TOP 1 ccx.Id
            FROM Gnr.CostCenter ccx
            WHERE ccx.HamkaranId = s.CostCenterId OR ccx.Number = s.CostCenterId
            ORDER BY CASE WHEN ccx.HamkaranId = s.CostCenterId THEN 0 ELSE 1 END
        ) cc
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
        WHERE NOT EXISTS (
            SELECT 1 FROM Rpr.RepairRequest x WHERE x.HtsId = CAST(s.RepairRequest_ID AS BIGINT)
        )
    ) AS s ON t.HtsId = s.HtsId
    WHEN NOT MATCHED THEN INSERT (
        HtsId, IdNumber, CustomerId, CustomerAddressId, CustomerAddressManual, ProductId, Serial, ZoneId,
        IsGuarantee, Unrepairable, NeedForSendToEmployer,
        EntryMiladiDate, EntryShamsiDate, CustomerNeedMiladiDate, CustomerNeedShamsiDate,
        ExitMiladiDate, ExitShamsiDate, Status, ConfirmerId, ResponsibleId, HasDelay,
        FailureReason, RepairDescription, Comment, HasContractor,
        ZoneSupervisorId, CustomerAgencyId, CustomerPhone, CustomerPhoneFromHistory,
        RepresentationReception, DestinationAfterRepair, DestinationAddress, JobDescription,
        PreFactorNumber, LoanFormNumber, DeliveryFormNumber,
        SafekeepingReturnFormNumber, SafekeepingReturnMiladiDate, SafekeepingReturnShamsiDate,
        HasContentProductionImaging, IsHavayarEquipment, InspectionPersonelId, ExpertIds, ExpertsInText,
        StationId, DlId, CostCenterId,
        ConfirmMiladiDate, ConfirmShamsiDate, ConfirmTime, Confirm2MiladiDate, Confirm2ShamsiDate, IsConfirmed2,
        StartMiladiDate, StartShamsiDate, EndMiladiDate, EndShamsiDate,
        RepairEntranceMiladiDate, RepairEntranceShamsiDate,
        WarehouseDeliveryMiladiDate, WarehouseDeliveryShamsiDate,
        AgreedMiladiDate, AgreedShamsiDate, Agreed2MiladiDate, Agreed2ShamsiDate, Agreed3MiladiDate, Agreed3ShamsiDate,
        DeliveringRecipient, BuyRequestMiladiDate, BuyRequestShamsiDate, SupplyMiladiDate, SupplyShamsiDate,
        QcRequestMiladiDate, QcRequestShamsiDate, PartFractionIds, PartFractionsInText,
        PreCheckMiladiDate, PreCheckShamsiDate, PreCheck2MiladiDate, PreCheck2ShamsiDate, IsPreChecked2,
        FinancialProposalMiladiDate, FinancialProposalShamsiDate, FinancialProposal2MiladiDate, FinancialProposal2ShamsiDate,
        IsApprovedFinancialProposal2, IsSentFinancialProposal, FinancialProposalSentMiladiDate, FinancialProposalSentShamsiDate,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive
    )
    VALUES (
        s.HtsId, s.IdNumber, s.CustomerId, s.CustomerAddressId, s.CustomerAddressManual, s.ProductId, s.Serial, s.ZoneId,
        s.IsGuarantee, s.Unrepairable, s.NeedForSendToEmployer,
        s.EntryMiladiDate, s.EntryShamsiDate, s.CustomerNeedMiladiDate, s.CustomerNeedShamsiDate,
        s.ExitMiladiDate, s.ExitShamsiDate, s.Status, s.ConfirmerId, s.ResponsibleId, s.HasDelay,
        s.FailureReason, s.RepairDescription, s.Comment, s.HasContractor,
        s.ZoneSupervisorId, s.CustomerAgencyId, s.CustomerPhone, s.CustomerPhoneFromHistory,
        s.RepresentationReception, s.DestinationAfterRepair, s.DestinationAddress, s.JobDescription,
        s.PreFactorNumber, s.LoanFormNumber, s.DeliveryFormNumber,
        s.SafekeepingReturnFormNumber, s.SafekeepingReturnMiladiDate, s.SafekeepingReturnShamsiDate,
        s.HasContentProductionImaging, s.IsHavayarEquipment, s.InspectionPersonelId, s.ExpertIds, s.ExpertsInText,
        s.StationId, s.DlId, s.CostCenterId,
        s.ConfirmMiladiDate, s.ConfirmShamsiDate, s.ConfirmTime, s.Confirm2MiladiDate, s.Confirm2ShamsiDate, s.IsConfirmed2,
        s.StartMiladiDate, s.StartShamsiDate, s.EndMiladiDate, s.EndShamsiDate,
        s.RepairEntranceMiladiDate, s.RepairEntranceShamsiDate,
        s.WarehouseDeliveryMiladiDate, s.WarehouseDeliveryShamsiDate,
        s.AgreedMiladiDate, s.AgreedShamsiDate, s.Agreed2MiladiDate, s.Agreed2ShamsiDate, s.Agreed3MiladiDate, s.Agreed3ShamsiDate,
        s.DeliveringRecipient, s.BuyRequestMiladiDate, s.BuyRequestShamsiDate, s.SupplyMiladiDate, s.SupplyShamsiDate,
        s.QcRequestMiladiDate, s.QcRequestShamsiDate, s.PartFractionIds, s.PartFractionsInText,
        s.PreCheckMiladiDate, s.PreCheckShamsiDate, s.PreCheck2MiladiDate, s.PreCheck2ShamsiDate, s.IsPreChecked2,
        s.FinancialProposalMiladiDate, s.FinancialProposalShamsiDate, s.FinancialProposal2MiladiDate, s.FinancialProposal2ShamsiDate,
        s.IsApprovedFinancialProposal2, s.IsSentFinancialProposal, s.FinancialProposalSentMiladiDate, s.FinancialProposalSentShamsiDate,
        COALESCE(s.CreatedById, 1), COALESCE(s.CreatedByName, @SeedUser),
        COALESCE(s.CreatedOnMiladiDateTime, @Now), COALESCE(s.CreatedOnShamsiDateTime, @NowShamsi),
        s.ModifiedById, s.ModifiedByName,
        COALESCE(s.ModifiedDateMiladiDateTime, @Now), COALESCE(s.ModifiedDateShamsiDateTime, @NowShamsi), 1
    );
    SET @Rc = @@ROWCOUNT;
    PRINT N'  Missing headers inserted: ' + CAST(@Rc AS NVARCHAR(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Fix_RepairRequest_CustomerAndHeaderFks ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

PRINT N'=== AFTER RepairRequest FK fill ===';
SELECT
    COUNT(*) AS Total,
    SUM(CASE WHEN CustomerId IS NULL THEN 1 ELSE 0 END) AS CustomerNull,
    SUM(CASE WHEN CustomerId IS NOT NULL THEN 1 ELSE 0 END) AS CustomerFilled,
    SUM(CASE WHEN CustomerAddressId IS NULL THEN 1 ELSE 0 END) AS AddressNull,
    SUM(CASE WHEN CustomerAddressId IS NOT NULL THEN 1 ELSE 0 END) AS AddressFilled,
    SUM(CASE WHEN CustomerAgencyId IS NULL THEN 1 ELSE 0 END) AS AgencyNull
FROM Rpr.RepairRequest;

SELECT TOP 8
    rr.Id, rr.HtsId, rr.IdNumber, rr.CustomerId, p.FullName AS CustomerName
FROM Rpr.RepairRequest rr
LEFT JOIN SLS.Customer c ON c.Id = rr.CustomerId
LEFT JOIN Gnr.Party p ON p.Id = c.PartyId
ORDER BY rr.HtsId DESC;
