# -*- coding: utf-8 -*-
from pathlib import Path

OUT = Path(__file__).with_name("SyncAfterSalesPhase8b_RepairRequestWidenAndChildren.sql")
SEED = Path(__file__).with_name("Seed_AfterSales_RepairRequest_RolesAndDelay.sql")

HEADER_COLS = [
    ("ZoneSupervisorId", "BIGINT NULL"),
    ("CustomerAgencyId", "BIGINT NULL"),
    ("CustomerPhone", "NVARCHAR(50) NULL"),
    ("CustomerPhoneFromHistory", "NVARCHAR(2048) NULL"),
    ("RepresentationReception", "NVARCHAR(400) NULL"),
    ("DestinationAfterRepair", "INT NULL"),
    ("DestinationAddress", "NVARCHAR(1024) NULL"),
    ("JobDescription", "INT NULL"),
    ("PreFactorNumber", "INT NULL"),
    ("LoanFormNumber", "INT NULL"),
    ("DeliveryFormNumber", "NVARCHAR(80) NULL"),
    ("SafekeepingReturnFormNumber", "NVARCHAR(20) NULL"),
    ("SafekeepingReturnMiladiDate", "DATETIME2 NULL"),
    ("SafekeepingReturnShamsiDate", "NVARCHAR(30) NULL"),
    ("HasContentProductionImaging", "BIT NULL"),
    ("IsHavayarEquipment", "BIT NULL"),
    ("InspectionPersonelId", "BIGINT NULL"),
    ("ExpertIds", "NVARCHAR(50) NULL"),
    ("ExpertsInText", "NVARCHAR(255) NULL"),
    ("StationId", "BIGINT NULL"),
    ("ConfirmMiladiDate", "DATETIME2 NULL"),
    ("ConfirmShamsiDate", "NVARCHAR(30) NULL"),
    ("ConfirmTime", "NVARCHAR(5) NULL"),
    ("Confirm2MiladiDate", "DATETIME2 NULL"),
    ("Confirm2ShamsiDate", "NVARCHAR(30) NULL"),
    ("IsConfirmed2", "BIT NULL"),
    ("StartMiladiDate", "DATETIME2 NULL"),
    ("StartShamsiDate", "NVARCHAR(30) NULL"),
    ("EndMiladiDate", "DATETIME2 NULL"),
    ("EndShamsiDate", "NVARCHAR(30) NULL"),
    ("RepairEntranceMiladiDate", "DATETIME2 NULL"),
    ("RepairEntranceShamsiDate", "NVARCHAR(30) NULL"),
    ("WarehouseDeliveryMiladiDate", "DATETIME2 NULL"),
    ("WarehouseDeliveryShamsiDate", "NVARCHAR(30) NULL"),
    ("AgreedMiladiDate", "DATETIME2 NULL"),
    ("AgreedShamsiDate", "NVARCHAR(30) NULL"),
    ("Agreed2MiladiDate", "DATETIME2 NULL"),
    ("Agreed2ShamsiDate", "NVARCHAR(30) NULL"),
    ("Agreed3MiladiDate", "DATETIME2 NULL"),
    ("Agreed3ShamsiDate", "NVARCHAR(30) NULL"),
    ("DeliveringRecipient", "NVARCHAR(256) NULL"),
    ("BuyRequestMiladiDate", "DATETIME2 NULL"),
    ("BuyRequestShamsiDate", "NVARCHAR(30) NULL"),
    ("SupplyMiladiDate", "DATETIME2 NULL"),
    ("SupplyShamsiDate", "NVARCHAR(30) NULL"),
    ("QcRequestMiladiDate", "DATETIME2 NULL"),
    ("QcRequestShamsiDate", "NVARCHAR(30) NULL"),
    ("PartFractionIds", "NVARCHAR(256) NULL"),
    ("PartFractionsInText", "NVARCHAR(1024) NULL"),
    ("PreCheckMiladiDate", "DATETIME2 NULL"),
    ("PreCheckShamsiDate", "NVARCHAR(30) NULL"),
    ("PreCheck2MiladiDate", "DATETIME2 NULL"),
    ("PreCheck2ShamsiDate", "NVARCHAR(30) NULL"),
    ("IsPreChecked2", "BIT NULL"),
    ("FinancialProposalMiladiDate", "DATETIME2 NULL"),
    ("FinancialProposalShamsiDate", "NVARCHAR(30) NULL"),
    ("FinancialProposal2MiladiDate", "DATETIME2 NULL"),
    ("FinancialProposal2ShamsiDate", "NVARCHAR(30) NULL"),
    ("IsApprovedFinancialProposal2", "BIT NULL"),
    ("IsSentFinancialProposal", "BIT NULL"),
    ("FinancialProposalSentMiladiDate", "DATETIME2 NULL"),
    ("FinancialProposalSentShamsiDate", "NVARCHAR(30) NULL"),
]

alter = "\n".join(
    f"IF COL_LENGTH('Rpr.RepairRequest', '{n}') IS NULL ALTER TABLE Rpr.RepairRequest ADD {n} {t};"
    for n, t in HEADER_COLS
)

sql = f"""/*
  SyncAfterSalesPhase8b_RepairRequestWidenAndChildren.sql
  Widen Rpr.RepairRequest to HTS page-270 fields and sync children from TMS.
  DlId: Acc_DL.Acc_DL_ID = HTS DL_FK then FIN.DL.Title = Acc_DLTitle (not HamkaranId = DL_FK).
  Header audit from HTS CreatedUser_FK / UpdatedUserId + Shamsi dates — do not overwrite with seed user.
  Encoding: UTF-8 with BOM. Apply: sqlcmd -C -b -I -f 65001 -i this file
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'sync-after-sales-phase8b';
IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1); RETURN; END;

{alter}

IF OBJECT_ID(N'Rpr.Station', N'U') IS NULL
BEGIN
    CREATE TABLE Rpr.Station (
        Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        HtsId BIGINT NOT NULL CONSTRAINT DF_Rpr_Station_HtsId DEFAULT (0),
        Title NVARCHAR(128) NOT NULL,
        Comment NVARCHAR(1024) NULL,
        CreatedById BIGINT NULL, ModifiedById BIGINT NULL,
        CreatedByName NVARCHAR(150) NULL, ModifiedByName NVARCHAR(150) NULL,
        ModifiedDateMiladiDateTime DATETIME2 NULL, ModifiedDateShamsiDateTime NVARCHAR(30) NULL,
        CreatedOnMiladiDateTime DATETIME2 NULL, CreatedOnShamsiDateTime NVARCHAR(30) NULL,
        IsActive INT NULL
    );
    CREATE UNIQUE INDEX IX_Rpr_Station_HtsId ON Rpr.Station(HtsId) WHERE HtsId <> CAST(0 AS bigint);
END
IF OBJECT_ID(N'Rpr.Contractor', N'U') IS NULL
BEGIN
    CREATE TABLE Rpr.Contractor (
        Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        HtsId BIGINT NOT NULL CONSTRAINT DF_Rpr_Contractor_HtsId DEFAULT (0),
        Title NVARCHAR(255) NOT NULL,
        Comment NVARCHAR(1024) NULL,
        CreatedById BIGINT NULL, ModifiedById BIGINT NULL,
        CreatedByName NVARCHAR(150) NULL, ModifiedByName NVARCHAR(150) NULL,
        ModifiedDateMiladiDateTime DATETIME2 NULL, ModifiedDateShamsiDateTime NVARCHAR(30) NULL,
        CreatedOnMiladiDateTime DATETIME2 NULL, CreatedOnShamsiDateTime NVARCHAR(30) NULL,
        IsActive INT NULL
    );
    CREATE UNIQUE INDEX IX_Rpr_Contractor_HtsId ON Rpr.Contractor(HtsId) WHERE HtsId <> CAST(0 AS bigint);
END
IF OBJECT_ID(N'Rpr.RepairRequestContractor', N'U') IS NULL
BEGIN
    CREATE TABLE Rpr.RepairRequestContractor (
        Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        HtsId BIGINT NOT NULL CONSTRAINT DF_Rpr_RepairRequestContractor_HtsId DEFAULT (0),
        RepairRequestId BIGINT NULL,
        ContractorId BIGINT NULL,
        ContractorTitle NVARCHAR(255) NULL,
        Cost BIGINT NULL,
        PartId BIGINT NULL,
        PartTitle NVARCHAR(255) NULL,
        SendPartMiladiDate DATETIME2 NULL, SendPartShamsiDate NVARCHAR(30) NULL,
        AcceptMiladiDate DATETIME2 NULL, AcceptShamsiDate NVARCHAR(30) NULL,
        AgreementMiladiDate DATETIME2 NULL, AgreementShamsiDate NVARCHAR(30) NULL,
        ReturnMiladiDate DATETIME2 NULL, ReturnShamsiDate NVARCHAR(30) NULL,
        CostAnnouncementMiladiDate DATETIME2 NULL, CostAnnouncementShamsiDate NVARCHAR(30) NULL,
        Comment NVARCHAR(4000) NULL,
        CreatedById BIGINT NULL, ModifiedById BIGINT NULL,
        CreatedByName NVARCHAR(150) NULL, ModifiedByName NVARCHAR(150) NULL,
        ModifiedDateMiladiDateTime DATETIME2 NULL, ModifiedDateShamsiDateTime NVARCHAR(30) NULL,
        CreatedOnMiladiDateTime DATETIME2 NULL, CreatedOnShamsiDateTime NVARCHAR(30) NULL,
        IsActive INT NULL
    );
    CREATE UNIQUE INDEX IX_Rpr_RepairRequestContractor_HtsId ON Rpr.RepairRequestContractor(HtsId) WHERE HtsId <> CAST(0 AS bigint);
END
IF OBJECT_ID(N'Rpr.RepairRequestUsedPart', N'U') IS NULL
BEGIN
    CREATE TABLE Rpr.RepairRequestUsedPart (
        Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        HtsId BIGINT NOT NULL CONSTRAINT DF_Rpr_RepairRequestUsedPart_HtsId DEFAULT (0),
        RepairRequestId BIGINT NULL,
        DlId BIGINT NULL,
        PartId BIGINT NULL,
        PartUnitId BIGINT NULL,
        DamagedPart BIT NOT NULL CONSTRAINT DF_Rpr_UsedPart_Damaged DEFAULT (0),
        StockName NVARCHAR(1024) NULL,
        UnitName NVARCHAR(1024) NULL,
        UsedMount DECIMAL(18,4) NOT NULL CONSTRAINT DF_Rpr_UsedPart_Used DEFAULT (0),
        ReturnMount DECIMAL(18,4) NOT NULL CONSTRAINT DF_Rpr_UsedPart_Ret DEFAULT (0),
        BuyPrice BIGINT NULL,
        SalePrice BIGINT NULL,
        TotalPrice DECIMAL(18,4) NULL,
        TotalBuyPrice DECIMAL(18,4) NULL,
        RequestFromStockMiladiDate DATETIME2 NULL,
        RequestFromStockShamsiDate NVARCHAR(50) NULL,
        Revision INT NULL,
        HtsHamkaranInvVchItemId INT NULL,
        CreatedById BIGINT NULL, ModifiedById BIGINT NULL,
        CreatedByName NVARCHAR(150) NULL, ModifiedByName NVARCHAR(150) NULL,
        ModifiedDateMiladiDateTime DATETIME2 NULL, ModifiedDateShamsiDateTime NVARCHAR(30) NULL,
        CreatedOnMiladiDateTime DATETIME2 NULL, CreatedOnShamsiDateTime NVARCHAR(30) NULL,
        IsActive INT NULL
    );
    CREATE UNIQUE INDEX IX_Rpr_RepairRequestUsedPart_HtsId ON Rpr.RepairRequestUsedPart(HtsId) WHERE HtsId <> CAST(0 AS bigint);
END

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

MERGE Rpr.Station AS t
USING (
    SELECT CAST(s.Id AS BIGINT) AS HtsId, NULLIF(LTRIM(RTRIM(s.Title)), N'') AS Title, s.Comment
    FROM [TMS].[TotalSystem].[dbo].[Rpr_Station] s
) s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.Title = ISNULL(s.Title, t.Title), t.Comment = s.Comment,
    t.ModifiedById=1, t.ModifiedByName=@SeedUser, t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED AND s.Title IS NOT NULL THEN INSERT
    (HtsId, Title, Comment, CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
     ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES (s.HtsId, s.Title, s.Comment, 1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);

MERGE Rpr.Contractor AS t
USING (
    SELECT CAST(s.Id AS BIGINT) AS HtsId, ISNULL(NULLIF(LTRIM(RTRIM(s.Title)), N''), N'-') AS Title, s.Comment
    FROM [TMS].[TotalSystem].[dbo].[Rpr_Contractor] s
) s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.Title = s.Title, t.Comment = s.Comment,
    t.ModifiedById=1, t.ModifiedByName=@SeedUser, t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED THEN INSERT
    (HtsId, Title, Comment, CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
     ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES (s.HtsId, s.Title, s.Comment, 1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);

MERGE Rpr.RepairRequestWorkExplanation AS t
USING (
    SELECT CAST(s.Id AS BIGINT) AS HtsId, s.Title, s.Code, s.Description
    FROM [TMS].[TotalSystem].[dbo].[Rpr_RepairRequest_WorkExplanation] s
) s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.Title=s.Title, t.Code=s.Code, t.Description=s.Description,
    t.ModifiedById=1, t.ModifiedByName=@SeedUser, t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED THEN INSERT
    (HtsId, Title, Code, Description, CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
     ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES (s.HtsId, s.Title, s.Code, s.Description, 1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);

MERGE Rpr.RepairRequestPartFraction AS t
USING (
    SELECT CAST(s.Id AS BIGINT) AS HtsId, s.Title, s.Description
    FROM [TMS].[TotalSystem].[dbo].[Rpr_RepairRequest_PartFraction] s
) s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.Title=s.Title, t.Description=s.Description,
    t.ModifiedById=1, t.ModifiedByName=@SeedUser, t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED THEN INSERT
    (HtsId, Title, Description, CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
     ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES (s.HtsId, s.Title, s.Description, 1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);

UPDATE t SET
    t.CustomerId = COALESCE(c.Id, t.CustomerId),
    t.CustomerAddressId = COALESCE(a.Id, t.CustomerAddressId),
    t.ZoneSupervisorId = umz.NewUserId,
    t.CustomerAgencyId = ag.Id,
    t.CustomerPhone = s.Customer_Phone,
    t.CustomerPhoneFromHistory = s.CustomerPhoneFromHistory,
    t.RepresentationReception = s.RepresentationReception,
    t.DestinationAfterRepair = s.DestinationAfterRepair_FK,
    t.DestinationAddress = s.DestinationAddress,
    t.JobDescription = s.JobDescriptionId,
    t.PreFactorNumber = s.PreFactor_Number,
    t.LoanFormNumber = s.LoanForm_Number,
    t.DeliveryFormNumber = s.DeliveryForm_Number,
    t.SafekeepingReturnFormNumber = s.SafekeepingReturnFormNumber,
    t.SafekeepingReturnMiladiDate = s.SafekeepingReturnDate,
    t.SafekeepingReturnShamsiDate = s.SafekeepingReturnDateInText,
    t.HasContentProductionImaging = s.HasContentProductionImaging,
    t.IsHavayarEquipment = s.IsHavayarEquipment,
    t.InspectionPersonelId = pe.Id,
    t.ExpertIds = s.ExpertIds,
    t.ExpertsInText = s.ExpertsInText,
    t.StationId = st.Id,
    t.DlId = dl.Id,
    t.CostCenterId = COALESCE(cc.Id, t.CostCenterId),
    t.ConfirmMiladiDate = s.Confirm_DateLatin,
    t.ConfirmShamsiDate = s.Confirm_Date,
    t.ConfirmTime = s.Confirm_Time,
    t.Confirm2MiladiDate = s.Confirm_Date2,
    t.Confirm2ShamsiDate = s.Confirm_Date2InText,
    t.IsConfirmed2 = s.IsConfirmed2,
    t.StartMiladiDate = s.StartDate,
    t.StartShamsiDate = s.StartDateInText,
    t.EndMiladiDate = s.EndDate,
    t.EndShamsiDate = s.EndDateInText,
    t.RepairEntranceMiladiDate = s.RepairEnteranceDate,
    t.RepairEntranceShamsiDate = s.RepairEnteranceDateInText,
    t.WarehouseDeliveryMiladiDate = s.DeliveryDate_Warehouse,
    t.WarehouseDeliveryShamsiDate = s.DeliveryDate_Warehouse_Shamsi,
    t.AgreedMiladiDate = s.AgreedDate,
    t.AgreedShamsiDate = s.AgreedDateInText,
    t.Agreed2MiladiDate = s.AgreedDate2,
    t.Agreed2ShamsiDate = s.AgreedDate2InText,
    t.Agreed3MiladiDate = s.AgreedDate3,
    t.Agreed3ShamsiDate = s.AgreedDate3InText,
    t.DeliveringRecipient = s.DeliveringRecipient,
    t.BuyRequestMiladiDate = s.BuyRequestDate,
    t.BuyRequestShamsiDate = s.BuyRequestDateInText,
    t.SupplyMiladiDate = s.SupplyDate,
    t.SupplyShamsiDate = s.SupplyDateInText,
    t.QcRequestMiladiDate = s.QcRequestDate,
    t.QcRequestShamsiDate = s.QcRequestDateInText,
    t.PartFractionIds = s.RepairRequestPartFractionsIds,
    t.PartFractionsInText = s.RepairRequestPartFractionsInText,
    t.PreCheckMiladiDate = s.PreCheckDate,
    t.PreCheckShamsiDate = s.PreCheckDateInText,
    t.PreCheck2MiladiDate = s.PreCheckDate2,
    t.PreCheck2ShamsiDate = s.PreCheckDate2InText,
    t.IsPreChecked2 = s.IsPreChecked2,
    t.FinancialProposalMiladiDate = s.FinancialProposalDate,
    t.FinancialProposalShamsiDate = s.FinancialProposalDateInText,
    t.FinancialProposal2MiladiDate = s.FinancialProposalDate2,
    t.FinancialProposal2ShamsiDate = s.FinancialProposalDate2InText,
    t.IsApprovedFinancialProposal2 = s.IsApprovedFinancialProposalDate2,
    t.IsSentFinancialProposal = s.IsSentFinancialProposal,
    t.FinancialProposalSentMiladiDate = s.FinancialProposalSentDate,
    t.FinancialProposalSentShamsiDate = s.FinancialProposalSentDateInText,
    t.CreatedById = COALESCE(umCreate.NewUserId, t.CreatedById),
    t.CreatedByName = COALESCE(umCreate.NewUserName, t.CreatedByName),
    t.CreatedOnMiladiDateTime = COALESCE(cd.CreatedMiladi, t.CreatedOnMiladiDateTime),
    t.CreatedOnShamsiDateTime = COALESCE(cd.CreatedShamsi, t.CreatedOnShamsiDateTime),
    t.ModifiedById = umEdit.NewUserId,
    t.ModifiedByName = umEdit.NewUserName,
    t.ModifiedDateMiladiDateTime = COALESCE(md.ModifiedMiladi, t.ModifiedDateMiladiDateTime),
    t.ModifiedDateShamsiDateTime = COALESCE(md.ModifiedShamsi, t.ModifiedDateShamsiDateTime)
FROM Rpr.RepairRequest t
INNER JOIN [TMS].[TotalSystem].[dbo].[Rpr_RepairRequest] s ON t.HtsId = s.RepairRequest_ID
LEFT JOIN SLS.Customer c ON c.HtsId = s.Customer_FK AND ISNULL(c.HtsId,0) <> 0
LEFT JOIN SLS.CustomerAddress a ON a.HtsId = s.CustomerAddressId AND ISNULL(a.HtsId,0) <> 0
LEFT JOIN #UserMap umz ON umz.OldUserId = s.ZoneSupervisor_FK
LEFT JOIN #UserMap umCreate ON umCreate.OldUserId = s.CreatedUser_FK
LEFT JOIN #UserMap umEdit ON umEdit.OldUserId = s.UpdatedUserId
LEFT JOIN SLS.Customer ag ON ag.HtsId = s.CustomerAgencyAddressId AND ISNULL(ag.HtsId,0) <> 0
LEFT JOIN Hcm.Personel pe ON pe.HamkaranId = s.InspectionPersonel_FK
LEFT JOIN Rpr.Station st ON st.HtsId = s.StationId
OUTER APPLY (
    SELECT TOP 1 d.Id
    FROM [TMS].[TotalSystem].[dbo].[Acc_DL] ad
    INNER JOIN FIN.DL d ON LTRIM(RTRIM(d.Title)) = LTRIM(RTRIM(ad.Acc_DLTitle))
    WHERE ad.Acc_DL_ID = s.DL_FK
    ORDER BY CASE WHEN d.Code = CAST(ad.Hamkaran_Acc_DL_FK AS nvarchar(64)) THEN 0 ELSE 1 END, d.Id
) dl
LEFT JOIN Gnr.CostCenter cc ON cc.HamkaranId = s.CostCenterId OR cc.Number = s.CostCenterId
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
) md;

MERGE Rpr.RepairRequestPart AS t
USING (
    SELECT CAST(s.RepairRequest_Part_ID AS BIGINT) AS HtsId, rr.Id AS RepairRequestId, p.Id AS PartId, pu.Id AS PartUnitId, dl.Id AS DlId,
           s.DamagedPart, s.StockName, s.UnitName, s.UsedMount, s.ReturnMount, s.BuyPrice, s.SalePrice,
           s.RequestDateFromStock AS RequestFromStockMiladiDate, s.RequestDateFromStockInText AS RequestFromStockShamsiDate,
           s.Description, s.ReplacementPartDescription, s.PartRequestItemId AS HtsPartRequestItemId,
           ISNULL(s.Hamkaran_InvVchItm_FK, 0) AS HtsHamkaranInvVchItemId
    FROM [TMS].[TotalSystem].[dbo].[Rpr_RepairRequest_Part] s
    INNER JOIN Rpr.RepairRequest rr ON rr.HtsId = s.RepairRequestId
    LEFT JOIN Inv.Part p ON p.HtsId = s.Part_FK AND ISNULL(p.HtsId,0) <> 0
    LEFT JOIN Inv.PartUnit pu ON pu.HamkaranId = s.PartUnit_FK
    LEFT JOIN FIN.DL dl ON dl.HamkaranId = s.AccDL_FK
) s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET
    t.RepairRequestId=s.RepairRequestId, t.PartId=s.PartId, t.PartUnitId=s.PartUnitId, t.DlId=s.DlId,
    t.DamagedPart=s.DamagedPart, t.StockName=s.StockName, t.UnitName=s.UnitName, t.UsedMount=s.UsedMount, t.ReturnMount=s.ReturnMount,
    t.BuyPrice=s.BuyPrice, t.SalePrice=s.SalePrice, t.RequestFromStockMiladiDate=s.RequestFromStockMiladiDate,
    t.RequestFromStockShamsiDate=s.RequestFromStockShamsiDate, t.Description=s.Description,
    t.ReplacementPartDescription=s.ReplacementPartDescription, t.HtsPartRequestItemId=s.HtsPartRequestItemId,
    t.HtsHamkaranInvVchItemId=s.HtsHamkaranInvVchItemId,
    t.ModifiedById=1, t.ModifiedByName=@SeedUser, t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED AND s.PartId IS NOT NULL THEN INSERT
    (HtsId, RepairRequestId, PartId, PartUnitId, DlId, DamagedPart, StockName, UnitName, UsedMount, ReturnMount,
     BuyPrice, SalePrice, RequestFromStockMiladiDate, RequestFromStockShamsiDate, Description, ReplacementPartDescription,
     HtsPartRequestItemId, HtsHamkaranInvVchItemId,
     CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
     ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES (s.HtsId, s.RepairRequestId, s.PartId, s.PartUnitId, s.DlId, s.DamagedPart, s.StockName, s.UnitName, s.UsedMount, s.ReturnMount,
     s.BuyPrice, s.SalePrice, s.RequestFromStockMiladiDate, s.RequestFromStockShamsiDate, s.Description, s.ReplacementPartDescription,
     s.HtsPartRequestItemId, s.HtsHamkaranInvVchItemId, 1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);

MERGE Rpr.RepairRequestUsedPart AS t
USING (
    SELECT CAST(s.Id AS BIGINT) AS HtsId, rr.Id AS RepairRequestId, p.Id AS PartId, pu.Id AS PartUnitId, dl.Id AS DlId,
           CAST(ISNULL(s.DamagedPart,0) AS BIT) AS DamagedPart, s.StockName, s.UnitName, s.UsedMount, s.ReturnMount,
           s.BuyPrice, s.SalePrice, s.TotalPrice, s.TotalBuyPrice,
           s.RequestDateFromStock AS RequestFromStockMiladiDate, s.RequestDateFromStockInText AS RequestFromStockShamsiDate,
           s.Revision, s.Hamkaran_InvVchItm_FK AS HtsHamkaranInvVchItemId
    FROM [TMS].[TotalSystem].[dbo].[Rpr_RepairRequestUsedPart] s
    INNER JOIN Rpr.RepairRequest rr ON rr.HtsId = s.RepairRequestId
    LEFT JOIN Inv.Part p ON p.HtsId = s.Part_FK AND ISNULL(p.HtsId,0) <> 0
    LEFT JOIN Inv.PartUnit pu ON pu.HamkaranId = s.PartUnit_FK
    LEFT JOIN FIN.DL dl ON dl.HamkaranId = s.AccDL_FK
) s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET
    t.RepairRequestId=s.RepairRequestId, t.PartId=s.PartId, t.PartUnitId=s.PartUnitId, t.DlId=s.DlId,
    t.DamagedPart=s.DamagedPart, t.StockName=s.StockName, t.UnitName=s.UnitName, t.UsedMount=s.UsedMount, t.ReturnMount=s.ReturnMount,
    t.BuyPrice=s.BuyPrice, t.SalePrice=s.SalePrice, t.TotalPrice=s.TotalPrice, t.TotalBuyPrice=s.TotalBuyPrice,
    t.RequestFromStockMiladiDate=s.RequestFromStockMiladiDate, t.RequestFromStockShamsiDate=s.RequestFromStockShamsiDate,
    t.Revision=s.Revision, t.HtsHamkaranInvVchItemId=s.HtsHamkaranInvVchItemId,
    t.ModifiedById=1, t.ModifiedByName=@SeedUser, t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED AND s.PartId IS NOT NULL THEN INSERT
    (HtsId, RepairRequestId, PartId, PartUnitId, DlId, DamagedPart, StockName, UnitName, UsedMount, ReturnMount,
     BuyPrice, SalePrice, TotalPrice, TotalBuyPrice, RequestFromStockMiladiDate, RequestFromStockShamsiDate, Revision, HtsHamkaranInvVchItemId,
     CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
     ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES (s.HtsId, s.RepairRequestId, s.PartId, s.PartUnitId, s.DlId, s.DamagedPart, s.StockName, s.UnitName, s.UsedMount, s.ReturnMount,
     s.BuyPrice, s.SalePrice, s.TotalPrice, s.TotalBuyPrice, s.RequestFromStockMiladiDate, s.RequestFromStockShamsiDate, s.Revision, s.HtsHamkaranInvVchItemId,
     1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);

MERGE Rpr.RepairRequestComment AS t
USING (
    SELECT CAST(s.RepairRequest_Comment_ID AS BIGINT) AS HtsId, rr.Id AS RepairRequestId, s.Comment, s.Cost
    FROM [TMS].[TotalSystem].[dbo].[Rpr_RepairRequest_Comment] s
    INNER JOIN Rpr.RepairRequest rr ON rr.HtsId = s.RepairRequest_FK
) s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.RepairRequestId=s.RepairRequestId, t.Comment=s.Comment, t.Cost=s.Cost,
    t.ModifiedById=1, t.ModifiedByName=@SeedUser, t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED THEN INSERT
    (HtsId, RepairRequestId, Comment, Cost, CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
     ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES (s.HtsId, s.RepairRequestId, s.Comment, s.Cost, 1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);

MERGE Rpr.RepairRequestAttachment AS t
USING (
    SELECT CAST(s.RepairRequest_Attachment_ID AS BIGINT) AS HtsId, rr.Id AS RepairRequestId,
           s.Attachment_FileName AS Title, s.Attachment_Comment AS Comment
    FROM [TMS].[TotalSystem].[dbo].[Rpr_RepairRequest_Attachment] s
    INNER JOIN Rpr.RepairRequest rr ON rr.HtsId = s.RepairRequest_FK
) s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.RepairRequestId=s.RepairRequestId, t.Title=s.Title, t.Comment=s.Comment,
    t.ModifiedById=1, t.ModifiedByName=@SeedUser, t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED THEN INSERT
    (HtsId, RepairRequestId, Title, Comment, CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
     ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES (s.HtsId, s.RepairRequestId, s.Title, s.Comment, 1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);

MERGE Rpr.RepairRequestManHour AS t
USING (
    SELECT CAST(s.RepairRequest_ManHours_ID AS BIGINT) AS HtsId, rr.Id AS RepairRequestId,
           CAST(s.Personel_FK AS BIGINT) AS HtsPersonelId, u.Id AS ExpertId,
           s.WorkDate AS WorkShamsiDate, s.StartTime, s.EndTime, s.ManHourPrice, s.ManHourSalePrice, s.Comment
    FROM [TMS].[TotalSystem].[dbo].[Rpr_RepairRequest_ManHours] s
    INNER JOIN Rpr.RepairRequest rr ON rr.HtsId = s.RepairRequest_FK
    LEFT JOIN Hcm.Personel pe ON pe.HamkaranId = s.Personel_FK
    LEFT JOIN [system].[User] u ON u.PartyId = pe.PartyId AND pe.PartyId IS NOT NULL
) s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET
    t.RepairRequestId=s.RepairRequestId, t.HtsPersonelId=s.HtsPersonelId, t.ExpertId=COALESCE(s.ExpertId, t.ExpertId),
    t.WorkShamsiDate=s.WorkShamsiDate, t.StartTime=s.StartTime, t.EndTime=s.EndTime,
    t.ManHourPrice=s.ManHourPrice, t.ManHourSalePrice=s.ManHourSalePrice, t.Comment=s.Comment,
    t.ModifiedById=1, t.ModifiedByName=@SeedUser, t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED THEN INSERT
    (HtsId, RepairRequestId, ExpertId, HtsPersonelId, WorkShamsiDate, StartTime, EndTime, ManHourPrice, ManHourSalePrice, Comment,
     CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
     ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES (s.HtsId, s.RepairRequestId, s.ExpertId, s.HtsPersonelId, s.WorkShamsiDate, s.StartTime, s.EndTime, s.ManHourPrice, s.ManHourSalePrice, s.Comment,
     1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);

MERGE Rpr.RepairRequestWbs AS t
USING (
    SELECT CAST(s.Id AS BIGINT) AS HtsId, mh.Id AS RepairRequestManHourId, we.Id AS WorkExplanationId, s.ManHour, s.Description
    FROM [TMS].[TotalSystem].[dbo].[Rpr_RepairRequest_Wbs] s
    INNER JOIN Rpr.RepairRequestManHour mh ON mh.HtsId = s.RepairRequestManHoursId
    INNER JOIN Rpr.RepairRequestWorkExplanation we ON we.HtsId = s.WorkExplanationsId
) s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.RepairRequestManHourId=s.RepairRequestManHourId, t.WorkExplanationId=s.WorkExplanationId,
    t.ManHour=s.ManHour, t.Description=s.Description,
    t.ModifiedById=1, t.ModifiedByName=@SeedUser, t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED THEN INSERT
    (HtsId, RepairRequestManHourId, WorkExplanationId, ManHour, Description,
     CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
     ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES (s.HtsId, s.RepairRequestManHourId, s.WorkExplanationId, s.ManHour, s.Description,
     1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);

MERGE Rpr.RepairRequestContractor AS t
USING (
    SELECT CAST(s.Id AS BIGINT) AS HtsId, rr.Id AS RepairRequestId, c.Id AS ContractorId, s.Contractor AS ContractorTitle,
           s.Cost, p.Id AS PartId, s.PartTitle,
           s.SendPartDate, s.SendPartDateInText, s.AcceptDate, s.AcceptDateInText,
           s.AgreegmentDate, s.AgreegmentDateInText, s.ReturnDate, s.ReturnDateInText,
           s.CostAnnouncementDate, s.CostAnnouncementDateInText, s.Comment
    FROM [TMS].[TotalSystem].[dbo].[Rpr_RepairRequestContractor] s
    INNER JOIN Rpr.RepairRequest rr ON rr.HtsId = s.RepairRequestId
    LEFT JOIN Rpr.Contractor c ON c.HtsId = s.ContractorId
    LEFT JOIN Inv.Part p ON p.HtsId = s.PartId AND ISNULL(p.HtsId,0) <> 0
) s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET
    t.RepairRequestId=s.RepairRequestId, t.ContractorId=s.ContractorId, t.ContractorTitle=s.ContractorTitle, t.Cost=s.Cost,
    t.PartId=s.PartId, t.PartTitle=s.PartTitle,
    t.SendPartMiladiDate=s.SendPartDate, t.SendPartShamsiDate=s.SendPartDateInText,
    t.AcceptMiladiDate=s.AcceptDate, t.AcceptShamsiDate=s.AcceptDateInText,
    t.AgreementMiladiDate=s.AgreegmentDate, t.AgreementShamsiDate=s.AgreegmentDateInText,
    t.ReturnMiladiDate=s.ReturnDate, t.ReturnShamsiDate=s.ReturnDateInText,
    t.CostAnnouncementMiladiDate=s.CostAnnouncementDate, t.CostAnnouncementShamsiDate=s.CostAnnouncementDateInText,
    t.Comment=s.Comment,
    t.ModifiedById=1, t.ModifiedByName=@SeedUser, t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED THEN INSERT
    (HtsId, RepairRequestId, ContractorId, ContractorTitle, Cost, PartId, PartTitle,
     SendPartMiladiDate, SendPartShamsiDate, AcceptMiladiDate, AcceptShamsiDate,
     AgreementMiladiDate, AgreementShamsiDate, ReturnMiladiDate, ReturnShamsiDate,
     CostAnnouncementMiladiDate, CostAnnouncementShamsiDate, Comment,
     CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
     ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES (s.HtsId, s.RepairRequestId, s.ContractorId, s.ContractorTitle, s.Cost, s.PartId, s.PartTitle,
     s.SendPartDate, s.SendPartDateInText, s.AcceptDate, s.AcceptDateInText,
     s.AgreegmentDate, s.AgreegmentDateInText, s.ReturnDate, s.ReturnDateInText,
     s.CostAnnouncementDate, s.CostAnnouncementDateInText, s.Comment,
     1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);

MERGE Rpr.EstimatedCost AS t
USING (
    SELECT CAST(s.EstimatedCost_ID AS BIGINT) AS HtsId, rr.Id AS RepairRequestId, c.Id AS CustomerId,
           s.CustomerTitle, z.Id AS ZoneId, p.Id AS ProductId, s.ManHour, s.Comment
    FROM [TMS].[TotalSystem].[dbo].[Rpr_EstimatedCost] s
    LEFT JOIN Rpr.RepairRequest rr ON rr.HtsId = s.RepairRequest_FK
    LEFT JOIN SLS.Customer c ON c.HtsId = s.Customer_FK AND ISNULL(c.HtsId,0) <> 0
    LEFT JOIN Crm.Zone z ON z.HtsId = s.Zone_FK
    LEFT JOIN Inv.Part p ON p.HtsId = s.Product_FK AND ISNULL(p.HtsId,0) <> 0
) s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET
    t.RepairRequestId=s.RepairRequestId, t.CustomerId=s.CustomerId, t.CustomerTitle=ISNULL(s.CustomerTitle, t.CustomerTitle),
    t.ZoneId=s.ZoneId, t.ProductId=s.ProductId, t.ManHour=s.ManHour, t.Comment=s.Comment,
    t.ModifiedById=1, t.ModifiedByName=@SeedUser, t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED THEN INSERT
    (HtsId, RepairRequestId, CustomerId, CustomerTitle, ZoneId, ProductId, ManHour, Comment,
     CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
     ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES (s.HtsId, s.RepairRequestId, s.CustomerId, ISNULL(s.CustomerTitle, N'-'), s.ZoneId, s.ProductId, s.ManHour, s.Comment,
     1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);

MERGE Rpr.ContractorOrder AS t
USING (
    SELECT CAST(s.Id AS BIGINT) AS HtsId, rr.Id AS RepairRequestId, s.Comment
    FROM [TMS].[TotalSystem].[dbo].[Rpr_ContractorOrder] s
    LEFT JOIN Rpr.RepairRequest rr ON rr.HtsId = s.RepairRequestId
) s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.RepairRequestId=s.RepairRequestId, t.Comment=s.Comment,
    t.ModifiedById=1, t.ModifiedByName=@SeedUser, t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED THEN INSERT
    (HtsId, RepairRequestId, Comment, CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
     ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES (s.HtsId, s.RepairRequestId, s.Comment, 1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);

UPDATE rr SET rr.HasContractor = CASE WHEN EXISTS (
    SELECT 1 FROM Rpr.RepairRequestContractor c WHERE c.RepairRequestId = rr.Id) THEN 1 ELSE 0 END
FROM Rpr.RepairRequest rr;

IF COL_LENGTH('Sale.ServiceRequest', 'LegacyRepairRequestIds') IS NOT NULL
BEGIN
    UPDATE rr SET rr.ServiceRequestId = x.Id
    FROM Rpr.RepairRequest rr
    CROSS APPLY (
        SELECT TOP 1 sr.Id
        FROM Sale.ServiceRequest sr
        WHERE sr.LegacyRepairRequestIds IS NOT NULL AND (
            sr.LegacyRepairRequestIds = CAST(rr.HtsId AS nvarchar(20))
            OR N',' + REPLACE(sr.LegacyRepairRequestIds, N' ', N'') + N',' LIKE N'%,' + CAST(rr.HtsId AS nvarchar(20)) + N',%'
        )
        ORDER BY sr.Id
    ) x
    WHERE rr.ServiceRequestId IS NULL;
END

COMMIT;
PRINT N'Phase 8b RepairRequest widen+children committed';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
"""

OUT.write_text(sql, encoding="utf-8-sig")
print("wrote", OUT, OUT.stat().st_size)

delay_select = r"""SELECT
    [t1].[Id] AS [t1_Id],
    [t1].[IdNumber] AS [t1_IdNumber],
    [t2].[Code] AS [t2_Code],
    [t3].[Code] AS [t3_Code],
    [t3].[Name] AS [t3_Name],
    [t1].[IsGuarantee] AS [t1_IsGuarantee],
    [t1].[CustomerAddressManual] AS [t1_CustomerAddressManual],
    [t1].[Status] AS [t1_Status],
    CASE WHEN DATEDIFF(DAY, [t1].[RepairEntranceMiladiDate], [t1].[PreCheckMiladiDate]) > 2
         THEN DATEDIFF(DAY, [t1].[RepairEntranceMiladiDate], [t1].[PreCheckMiladiDate]) ELSE 0 END AS [PreCheckDelay],
    DATEDIFF(DAY, [t1].[AgreedMiladiDate], [t1].[WarehouseDeliveryMiladiDate]) AS [RepairDelay],
    DATEDIFF(DAY, [t1].[BuyRequestMiladiDate], [t1].[SupplyMiladiDate]) AS [SupplyDelay],
    DATEDIFF(DAY, [t1].[QcRequestMiladiDate], [t1].[WarehouseDeliveryMiladiDate]) AS [TestDelay],
    CASE WHEN [t1].[Status] NOT IN (2093, 304, 2236) AND [t1].[AgreedMiladiDate] < GETDATE()
         THEN DATEDIFF(DAY, [t1].[AgreedMiladiDate], GETDATE()) ELSE NULL END AS [AgreedDelayInDay],
    CASE WHEN [t1].[SafekeepingReturnMiladiDate] IS NULL
         THEN DATEDIFF(DAY, [t1].[WarehouseDeliveryMiladiDate], GETDATE())
         ELSE DATEDIFF(DAY, [t1].[WarehouseDeliveryMiladiDate], [t1].[SafekeepingReturnMiladiDate]) END AS [StayInWarehouseAfterRepair]
FROM [Rpr].[RepairRequest] AS [t1]
LEFT JOIN [SLS].[Customer] AS [t2] ON [t1].[CustomerId] = [t2].[Id]
LEFT JOIN [Inv].[Part] AS [t3] ON [t1].[ProductId] = [t3].[Id]"""

def col(table, name, display, alliance, address, typen, typei, visible=True, pk=False, sort=None, render=None, option=None, width=140):
    opt = option or "null"
    rend = "null" if not render else '"' + render.replace('"', '\\"') + '"'
    return (
        '{"TableName":"%s","ColumnName":"%s","DisplayName":"%s","Alliance":"%s","Address":"%s",'
        '"SystemTypeName":"%s","SelectIndex":0,"Visible":%s,"PrimaryKey":%s,"SystemType":%s,'
        '"SortDirection":%s,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":%s,'
        '"Aggregate":null,"Render":%s,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,'
        '"BtnConfig":null,"InputConfig":null,"Width":%s,"Filterable":true,"Sortable":true,"ClassName":""}'
    ) % (table, name, display, alliance, address, typen, "true" if visible else "false",
         "true" if pk else "false", typei, "1" if sort else "null", opt, rend, width)

delay_cols = ",".join([
    col("Rpr.RepairRequest", "Id", "شناسه", "t1_Id", "[t1].[Id]", "Long", 6, False, True, True, width=80),
    col("Rpr.RepairRequest", "IdNumber", "شماره", "t1_IdNumber", "[t1].[IdNumber]", "String", 0, True, False, width=120),
    col("SLS.Customer", "Code", "کد مشتری", "t2_Code", "[t2].[Code]", "String", 0, True, False, width=100),
    col("Inv.Part", "Code", "کد تجهیز", "t3_Code", "[t3].[Code]", "String", 0, True, False, width=120),
    col("Inv.Part", "Name", "عنوان تجهیز", "t3_Name", "[t3].[Name]", "String", 0, True, False, width=180),
    col("Rpr.RepairRequest", "IsGuarantee", "گارانتی", "t1_IsGuarantee", "[t1].[IsGuarantee]", "Boolean", 1, True, False, width=80),
    col("Rpr.RepairRequest", "CustomerAddressManual", "سایت مشتری", "t1_CustomerAddressManual", "[t1].[CustomerAddressManual]", "String", 0, True, False, width=180),
    col("Rpr.RepairRequest", "Status", "وضعیت", "t1_Status", "[t1].[Status]", "Select", 8, True, False,
        option='{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Rpr.Enums.RepairRequestStatusEnum"}', width=160),
    col("Rpr.RepairRequest", "PreCheckDelay", "تاخیر پیش‌بررسی", "PreCheckDelay",
        "CASE WHEN DATEDIFF(DAY, [t1].[RepairEntranceMiladiDate], [t1].[PreCheckMiladiDate]) > 2 THEN DATEDIFF(DAY, [t1].[RepairEntranceMiladiDate], [t1].[PreCheckMiladiDate]) ELSE 0 END",
        "Int", 7, True, False, width=120),
    col("Rpr.RepairRequest", "RepairDelay", "تاخیر تعمیر", "RepairDelay",
        "DATEDIFF(DAY, [t1].[AgreedMiladiDate], [t1].[WarehouseDeliveryMiladiDate])", "Int", 7, True, False, width=110),
    col("Rpr.RepairRequest", "SupplyDelay", "تاخیر تأمین", "SupplyDelay",
        "DATEDIFF(DAY, [t1].[BuyRequestMiladiDate], [t1].[SupplyMiladiDate])", "Int", 7, True, False, width=110),
    col("Rpr.RepairRequest", "TestDelay", "تاخیر تست", "TestDelay",
        "DATEDIFF(DAY, [t1].[QcRequestMiladiDate], [t1].[WarehouseDeliveryMiladiDate])", "Int", 7, True, False, width=110),
    col("Rpr.RepairRequest", "AgreedDelayInDay", "تاخیر توافقی", "AgreedDelayInDay",
        "CASE WHEN [t1].[Status] NOT IN (2093, 304, 2236) AND [t1].[AgreedMiladiDate] < GETDATE() THEN DATEDIFF(DAY, [t1].[AgreedMiladiDate], GETDATE()) ELSE NULL END",
        "Int", 7, True, False, width=120),
    col("Rpr.RepairRequest", "StayInWarehouseAfterRepair", "ماندگاری انبار", "StayInWarehouseAfterRepair",
        "CASE WHEN [t1].[SafekeepingReturnMiladiDate] IS NULL THEN DATEDIFF(DAY, [t1].[WarehouseDeliveryMiladiDate], GETDATE()) ELSE DATEDIFF(DAY, [t1].[WarehouseDeliveryMiladiDate], [t1].[SafekeepingReturnMiladiDate]) END",
        "Int", 7, True, False, width=130),
])

seed = f"""/*
  Seed_AfterSales_RepairRequest_RolesAndDelay.sql
  نقش‌های صفحه 270 از گروه‌های HTS 109/117/61/350/359/537
  و نمایه تاخیر با فرمول VwRepairRequestDelays
  Encoding: UTF-8 with BOM.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-repairrequest-roles';

    IF OBJECT_ID('tempdb..#RrRoles') IS NOT NULL DROP TABLE #RrRoles;
    CREATE TABLE #RrRoles (WantId BIGINT NOT NULL, Name NVARCHAR(200) NOT NULL, Title NVARCHAR(200) NOT NULL);
    INSERT INTO #RrRoles VALUES
        (500032, N'Rpr.RepairRequest.AfterSale', N'درخواست تعمیر - خدمات (HTS 109)'),
        (500033, N'Rpr.RepairRequest.Repairs', N'درخواست تعمیر - تعمیرات (HTS 117)'),
        (500034, N'Rpr.RepairRequest.View', N'درخواست تعمیر - مشاهده (HTS 61/350)'),
        (500035, N'Rpr.RepairRequest.ShowAll', N'درخواست تعمیر - نمایش همه (HTS 537)'),
        (500036, N'Rpr.RepairRequest.Imaging', N'تصویربرداری محتوا (HTS 359)');

    DECLARE @Rid BIGINT, @RName NVARCHAR(200), @RTitle NVARCHAR(200);
    DECLARE rc CURSOR LOCAL FAST_FORWARD FOR SELECT WantId, Name, Title FROM #RrRoles;
    OPEN rc; FETCH NEXT FROM rc INTO @Rid, @RName, @RTitle;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = @RName)
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Id = @Rid)
            BEGIN
                SET IDENTITY_INSERT system.Role ON;
                INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
                    CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
                VALUES (@Rid, @RName, @RTitle, 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
                SET IDENTITY_INSERT system.Role OFF;
            END
            ELSE
                INSERT INTO system.Role (Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
                    CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
                VALUES (@RName, @RTitle, 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
        END
        FETCH NEXT FROM rc INTO @Rid, @RName, @RTitle;
    END
    CLOSE rc; DEALLOCATE rc;

    DELETE ra FROM system.RoleAccess ra
    WHERE ra.Path LIKE N'/panel/rpr/repairrequest%'
      AND ra.RoleId IN (SELECT Id FROM system.Role WHERE Name = N'Sale.AfterSales');

    DELETE ra FROM system.RoleAccess ra
    WHERE ra.Path = N'/panel/rpr/repairrequest/sendtocontractor';

    DELETE ra FROM system.RoleAccess ra
    WHERE ra.Path LIKE N'/panel/rpr/repairrequest%'
      AND ra.RoleId IN (SELECT Id FROM system.Role WHERE Name IN (
          N'Rpr.RepairRequest.AfterSale', N'Rpr.RepairRequest.Repairs', N'Rpr.RepairRequest.View',
          N'Rpr.RepairRequest.ShowAll', N'Rpr.RepairRequest.Imaging'));

    IF OBJECT_ID('tempdb..#RrAcc') IS NOT NULL DROP TABLE #RrAcc;
    CREATE TABLE #RrAcc (Path NVARCHAR(300) NOT NULL, AType INT NOT NULL, IType INT NOT NULL, RoleName NVARCHAR(200) NOT NULL);

    INSERT INTO #RrAcc (Path, AType, IType, RoleName)
    SELECT p.Path, p.AType, p.IType, r.RoleName
    FROM (VALUES
        (N'/panel/rpr/repairrequest/list', 1, 1),
        (N'/panel/rpr/repairrequest/fetchdata', 2, 2),
        (N'/panel/rpr/repairrequest/edit', 1, 5),
        (N'/panel/rpr/repairrequest/exporttoexcel', 2, 0),
        (N'/panel/rpr/repairrequest/delaylist', 1, 1),
        (N'/panel/rpr/repairrequest/delaychart', 1, 8),
        (N'/panel/rpr/repairrequest/delaychartdata', 2, 2)
    ) p(Path, AType, IType)
    CROSS JOIN (VALUES
        (N'Rpr.RepairRequest.AfterSale'),
        (N'Rpr.RepairRequest.Repairs'),
        (N'Rpr.RepairRequest.View'),
        (N'Rpr.RepairRequest.ShowAll'),
        (N'ShowAllMenus'),
        (N'Rpr.Repairs')
    ) r(RoleName);

    INSERT INTO #RrAcc
    SELECT p.Path, p.AType, p.IType, r.RoleName
    FROM (VALUES
        (N'/panel/rpr/repairrequest/new', 1, 4),
        (N'/panel/rpr/repairrequest/save', 2, 3),
        (N'/panel/rpr/repairrequest/add', 2, 4),
        (N'/panel/rpr/repairrequest/update', 2, 5),
        (N'/panel/rpr/repairrequest/aftersaleaccess', 1, 7),
        (N'/panel/rpr/repairrequest/confirm', 2, 7)
    ) p(Path, AType, IType)
    CROSS JOIN (VALUES (N'Rpr.RepairRequest.AfterSale'), (N'ShowAllMenus'), (N'Rpr.Repairs')) r(RoleName);

    INSERT INTO #RrAcc
    SELECT p.Path, p.AType, p.IType, r.RoleName
    FROM (VALUES
        (N'/panel/rpr/repairrequest/save', 2, 3),
        (N'/panel/rpr/repairrequest/update', 2, 5),
        (N'/panel/rpr/repairrequest/repairsaccess', 1, 7)
    ) p(Path, AType, IType)
    CROSS JOIN (VALUES (N'Rpr.RepairRequest.Repairs'), (N'ShowAllMenus'), (N'Rpr.Repairs')) r(RoleName);

    INSERT INTO #RrAcc
    SELECT p.Path, p.AType, p.IType, r.RoleName
    FROM (VALUES
        (N'/panel/rpr/repairrequest/contentproductionimaging', 1, 7),
        (N'/panel/rpr/repairrequest/edit', 1, 5),
        (N'/panel/rpr/repairrequest/save', 2, 3),
        (N'/panel/rpr/repairrequest/update', 2, 5)
    ) p(Path, AType, IType)
    CROSS JOIN (VALUES (N'Rpr.RepairRequest.Imaging'), (N'ShowAllMenus'), (N'Rpr.Repairs')) r(RoleName);

    INSERT INTO #RrAcc
    SELECT p.Path, p.AType, p.IType, r.RoleName
    FROM (VALUES
        (N'/panel/rpr/repairrequestcomment/listbyparentid', 1, 1),
        (N'/panel/rpr/repairrequestcomment/getlistbyparentid', 2, 2),
        (N'/panel/rpr/repairrequestcomment/new', 1, 4),
        (N'/panel/rpr/repairrequestcomment/edit', 1, 5),
        (N'/panel/rpr/repairrequestcomment/save', 2, 3),
        (N'/panel/rpr/repairrequestattachment/listbyparentid', 1, 1),
        (N'/panel/rpr/repairrequestattachment/getlistbyparentid', 2, 2),
        (N'/panel/rpr/repairrequestattachment/new', 1, 4),
        (N'/panel/rpr/repairrequestattachment/edit', 1, 5),
        (N'/panel/rpr/repairrequestattachment/save', 2, 3)
    ) p(Path, AType, IType)
    CROSS JOIN (VALUES (N'Rpr.RepairRequest.AfterSale'), (N'ShowAllMenus'), (N'Rpr.Repairs')) r(RoleName);

    INSERT INTO #RrAcc
    SELECT p.Path, p.AType, p.IType, r.RoleName
    FROM (VALUES
        (N'/panel/rpr/repairrequestpart/listbyparentid', 1, 1),
        (N'/panel/rpr/repairrequestpart/getlistbyparentid', 2, 2),
        (N'/panel/rpr/repairrequestpart/new', 1, 4),
        (N'/panel/rpr/repairrequestpart/edit', 1, 5),
        (N'/panel/rpr/repairrequestpart/save', 2, 3),
        (N'/panel/rpr/repairrequestusedpart/listbyparentid', 1, 1),
        (N'/panel/rpr/repairrequestusedpart/getlistbyparentid', 2, 2),
        (N'/panel/rpr/repairrequestusedpart/new', 1, 4),
        (N'/panel/rpr/repairrequestusedpart/edit', 1, 5),
        (N'/panel/rpr/repairrequestusedpart/save', 2, 3),
        (N'/panel/rpr/repairrequestmanhour/listbyparentid', 1, 1),
        (N'/panel/rpr/repairrequestmanhour/getlistbyparentid', 2, 2),
        (N'/panel/rpr/repairrequestmanhour/new', 1, 4),
        (N'/panel/rpr/repairrequestmanhour/edit', 1, 5),
        (N'/panel/rpr/repairrequestmanhour/save', 2, 3),
        (N'/panel/rpr/repairrequestcontractor/listbyparentid', 1, 1),
        (N'/panel/rpr/repairrequestcontractor/getlistbyparentid', 2, 2),
        (N'/panel/rpr/repairrequestcontractor/new', 1, 4),
        (N'/panel/rpr/repairrequestcontractor/edit', 1, 5),
        (N'/panel/rpr/repairrequestcontractor/save', 2, 3),
        (N'/panel/rpr/repairrequestwbs/listbyrepairrequestid', 1, 1),
        (N'/panel/rpr/repairrequestwbs/getlistbyrepairrequestid', 2, 2)
    ) p(Path, AType, IType)
    CROSS JOIN (VALUES (N'Rpr.RepairRequest.Repairs'), (N'ShowAllMenus'), (N'Rpr.Repairs')) r(RoleName);

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT a.Path, a.AType, a.IType, N'Entities.App.Rpr.RepairRequest', NULL, NULL, NULL, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #RrAcc a
    INNER JOIN system.Role r ON r.Name = a.RoleName
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess x
        WHERE x.RoleId = r.Id AND x.Path = a.Path AND x.ActionAccessType = a.AType);

    IF OBJECT_ID('tempdb..#UserRoleMap') IS NOT NULL DROP TABLE #UserRoleMap;
    CREATE TABLE #UserRoleMap (Username NVARCHAR(200) NOT NULL, RoleName NVARCHAR(200) NOT NULL);
    INSERT INTO #UserRoleMap
    SELECT DISTINCT COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), u.Username),
           CASE
                WHEN g.UserGroup_FK = 109 THEN N'Rpr.RepairRequest.AfterSale'
                WHEN g.UserGroup_FK = 117 THEN N'Rpr.RepairRequest.Repairs'
                WHEN g.UserGroup_FK = 359 THEN N'Rpr.RepairRequest.Imaging'
                WHEN g.UserGroup_FK = 537 THEN N'Rpr.RepairRequest.ShowAll'
                ELSE N'Rpr.RepairRequest.View'
           END
    FROM (
        SELECT pug.UserGroup_FK, m.User_FK
        FROM [TMS].[TotalSystem].[dbo].[Gnr_PageAction_UserGroup] pug
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_PageAction] pa ON pa.PageAction_ID = pug.PageAction_FK
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m ON m.UserGroup_FK = pug.UserGroup_FK
        WHERE pa.Page_FK = 270 AND pug.UserGroup_FK IN (61, 109, 117, 350, 359, 537)
        UNION
        SELECT CASE pa.Permission_FK
                   WHEN 70 THEN 109 WHEN 71 THEN 117 WHEN 130 THEN 359 WHEN 7 THEN 537 ELSE 61 END, pau.User_FK
        FROM [TMS].[TotalSystem].[dbo].[Gnr_PageAction_User] pau
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_PageAction] pa ON pa.PageAction_ID = pau.PageAction_FK
        WHERE pa.Page_FK = 270
    ) g
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = g.User_FK
    WHERE COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), u.Username) IS NOT NULL;

    DECLARE @MapUsername nvarchar(200), @MapRoleName nvarchar(200), @MapRoleId bigint, @MapUserId bigint;
    DECLARE um CURSOR LOCAL FAST_FORWARD FOR SELECT Username, RoleName FROM #UserRoleMap;
    OPEN um; FETCH NEXT FROM um INTO @MapUsername, @MapRoleName;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT @MapRoleId = Id FROM system.Role WHERE Name = @MapRoleName;
        SELECT @MapUserId = Id FROM [system].[User] WHERE LOWER(Username) = LOWER(@MapUsername);
        IF @MapRoleId IS NOT NULL AND @MapUserId IS NOT NULL
           AND NOT EXISTS (SELECT 1 FROM system.UserRole ur WHERE ur.UserId = @MapUserId AND ur.RoleId = @MapRoleId)
            INSERT INTO system.UserRole (UserId, RoleId, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
                CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
            VALUES (@MapUserId, @MapRoleId, 1, @SeedUser, 1, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
        FETCH NEXT FROM um INTO @MapUsername, @MapRoleName;
    END
    CLOSE um; DEALLOCATE um;

    DECLARE @PName NVARCHAR(100) = N'AfterSales_RepairRequest_Delay';
    DECLARE @PTitle NVARCHAR(200) = N'تاخیرات تعمیرات';
    DECLARE @PEnt NVARCHAR(200) = N'entities.app.rpr.repairrequest';
    DECLARE @PPascal NVARCHAR(200) = N'Entities.App.Rpr.RepairRequest';
    DECLARE @PSelect NVARCHAR(MAX) = N'{delay_select}';
    DECLARE @PCols NVARCHAR(MAX) = N'[{delay_cols}]';
    DECLARE @PAct NVARCHAR(MAX) = N'[{{"dataActionName":"new","enable":false,"title":"جدید"}},{{"dataActionName":"edit","enable":true,"title":"ویرایش"}},{{"dataActionName":"delete","enable":false,"title":"حذف"}},{{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}}]';
    DECLARE @PBtn NVARCHAR(MAX) = N'[{{"id":"as_edit_repairrequest_delay","title":"ویرایش","dataActionName":"editRow","colorClass":"btn-color-primary","iconClass":"ki-pencil ki-outline","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {{\\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\\n    if (!id) {{ toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }}\\n    appController.addPage(''/Panel/Rpr/RepairRequest/Edit?id='' + id, true, ''ویرایش'');\\n}}"}}]';
    DECLARE @QueryJsonSkeleton NVARCHAR(MAX) = N'{{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}}';
    DECLARE @PQueryJson NVARCHAR(MAX) = JSON_MODIFY(@QueryJsonSkeleton, '$.CustomQuery', @PSelect);
    DECLARE @PId BIGINT;

    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @PName)
    BEGIN
        UPDATE system.SavedQuery
        SET Title=@PTitle, QueryJson=@PQueryJson, ColumnsJson=@PCols, EntityFullName=@PEnt, Mode=1, Type=1,
            ActionOptions=@PAct, CustomActionButtonsJson=@PBtn, EventScriptsJson=N'{{"onSelectedRow":"","onRowAdded":""}}',
            ModifiedById=1, ModifiedByName=@SeedUser, ModifiedDateMiladiDateTime=@Now, ModifiedDateShamsiDateTime=@NowShamsi, IsActive=1
        WHERE Name=@PName;
        SELECT @PId = Id FROM system.SavedQuery WHERE Name=@PName;
    END
    ELSE
    BEGIN
        INSERT INTO system.SavedQuery
            (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
             Mode, Type, EntityFullName, ActionOptions,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (@PName, @PTitle, @PQueryJson, @PCols, N'{{}}', @PBtn, N'{{"onSelectedRow":"","onRowAdded":""}}',
             1, 1, @PEnt, @PAct, 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
        SET @PId = SCOPE_IDENTITY();
    END

    DELETE ra FROM system.RoleAccess ra WHERE ra.ActionAccessType = 3 AND ra.RowId = @PId;
    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(@PId AS nvarchar(20)), 3, 7, @PPascal, @PTitle, NULL, @PId, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    WHERE r.Name IN (N'ShowAllMenus', N'Rpr.Repairs', N'Rpr.RepairRequest.AfterSale', N'Rpr.RepairRequest.Repairs',
                     N'Rpr.RepairRequest.View', N'Rpr.RepairRequest.ShowAll');

    DECLARE @MenuId BIGINT = (SELECT TOP 1 Id FROM system.SystemMenu WHERE Name = N'AfterSalesServiceSystem');
    IF @MenuId IS NOT NULL
    BEGIN
        IF OBJECT_ID('tempdb..#MenuRoles') IS NOT NULL DROP TABLE #MenuRoles;
        CREATE TABLE #MenuRoles (RoleName NVARCHAR(200) NOT NULL PRIMARY KEY);
        INSERT INTO #MenuRoles SELECT DISTINCT LTRIM(RTRIM(j.value))
        FROM system.SystemMenu m CROSS APPLY OPENJSON(ISNULL(m.AccessRoles, N'[]')) j
        WHERE m.Id = @MenuId AND LTRIM(RTRIM(j.value)) <> N'';
        INSERT INTO #MenuRoles
        SELECT v.RoleName FROM (VALUES
            (N'Rpr.RepairRequest.AfterSale'),
            (N'Rpr.RepairRequest.Repairs'),
            (N'Rpr.RepairRequest.View'),
            (N'Rpr.RepairRequest.ShowAll'),
            (N'Rpr.RepairRequest.Imaging')
        ) v(RoleName)
        WHERE NOT EXISTS (SELECT 1 FROM #MenuRoles x WHERE x.RoleName = v.RoleName);

        DECLARE @AccessRoles NVARCHAR(MAX);
        DECLARE @AccessRoleIds NVARCHAR(MAX);
        SELECT @AccessRoles = N'[' + STUFF((
            SELECT N',"' + RoleName + N'"' FROM #MenuRoles ORDER BY RoleName
            FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N'') + N']';
        SELECT @AccessRoleIds = N'[' + STUFF((
            SELECT N',' + CAST(r.Id AS nvarchar(20))
            FROM #MenuRoles m INNER JOIN system.Role r ON r.Name = m.RoleName
            ORDER BY r.Id
            FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N'') + N']';
        UPDATE system.SystemMenu
        SET AccessRoles = @AccessRoles, AccessRoleIds = @AccessRoleIds,
            ModifiedById = 1, ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @MenuId;
    END

    DECLARE @RrOnRowAdded NVARCHAR(MAX) = N'function(ctx) {{
    var data = ctx.rowData || {{}};
    var $cell = ctx.$row.find(''td'').eq(0);
    var status = data.t1_Status;
    var color = null;
    if (status == 303) color = ''orange'';
    else if (status == 301 || status == 614 || status == 615) color = ''#6e85da'';
    else if (status == 305) color = ''lightgreen'';
    else if (status == 2092) color = ''yellow'';
    else if (status == 304) color = ''orangered'';
    else if (status == 2236) color = ''grey'';
    ctx.$row.css(''background-color'', '''');
    if (color) $cell.css(''background-color'', color);
}}';
    DECLARE @RrEventScripts NVARCHAR(MAX) = (
        SELECT
            CAST(N'' AS nvarchar(max)) AS onSelectedRow,
            @RrOnRowAdded AS onRowAdded
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    );
    UPDATE system.SavedQuery
    SET EventScriptsJson = @RrEventScripts,
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Name IN (N'AfterSales_RepairRequest_List', N'AfterSales_RepairRequest_Delay');

    COMMIT;
    PRINT N'=== DONE Seed_AfterSales_RepairRequest_RolesAndDelay ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
"""

# delay_select/cols inserted with format - the f-string already used {{ for braces in SQL.
# I used {delay_select} in the seed f-string above - need to actually interpolate.
seed = seed.replace("{delay_select}", delay_select).replace("{delay_cols}", delay_cols)
SEED.write_text(seed, encoding="utf-8-sig")
print("wrote", SEED, SEED.stat().st_size)
