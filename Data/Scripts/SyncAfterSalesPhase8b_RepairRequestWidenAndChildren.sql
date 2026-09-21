/*
  SyncAfterSalesPhase8b_RepairRequestWidenAndChildren.sql
  Widen Rpr.RepairRequest to HTS page-270 fields and sync children from TMS.
  Header INSERT uses NOT EXISTS on HtsId (Havayar Id is IDENTITY ≠ HTS RepairRequest_ID).
  Then UPDATE fills widened columns. Children MERGE on HtsId via RepairRequest.HtsId.
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

IF COL_LENGTH('Rpr.RepairRequest', 'ZoneSupervisorId') IS NULL ALTER TABLE Rpr.RepairRequest ADD ZoneSupervisorId BIGINT NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'CustomerAgencyId') IS NULL ALTER TABLE Rpr.RepairRequest ADD CustomerAgencyId BIGINT NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'CustomerPhone') IS NULL ALTER TABLE Rpr.RepairRequest ADD CustomerPhone NVARCHAR(50) NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'CustomerPhoneFromHistory') IS NULL ALTER TABLE Rpr.RepairRequest ADD CustomerPhoneFromHistory NVARCHAR(2048) NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'RepresentationReception') IS NULL ALTER TABLE Rpr.RepairRequest ADD RepresentationReception NVARCHAR(400) NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'DestinationAfterRepair') IS NULL ALTER TABLE Rpr.RepairRequest ADD DestinationAfterRepair INT NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'DestinationAddress') IS NULL ALTER TABLE Rpr.RepairRequest ADD DestinationAddress NVARCHAR(1024) NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'JobDescription') IS NULL ALTER TABLE Rpr.RepairRequest ADD JobDescription INT NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'PreFactorNumber') IS NULL ALTER TABLE Rpr.RepairRequest ADD PreFactorNumber INT NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'LoanFormNumber') IS NULL ALTER TABLE Rpr.RepairRequest ADD LoanFormNumber INT NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'DeliveryFormNumber') IS NULL ALTER TABLE Rpr.RepairRequest ADD DeliveryFormNumber NVARCHAR(80) NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'SafekeepingReturnFormNumber') IS NULL ALTER TABLE Rpr.RepairRequest ADD SafekeepingReturnFormNumber NVARCHAR(20) NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'SafekeepingReturnMiladiDate') IS NULL ALTER TABLE Rpr.RepairRequest ADD SafekeepingReturnMiladiDate DATETIME2 NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'SafekeepingReturnShamsiDate') IS NULL ALTER TABLE Rpr.RepairRequest ADD SafekeepingReturnShamsiDate NVARCHAR(30) NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'HasContentProductionImaging') IS NULL ALTER TABLE Rpr.RepairRequest ADD HasContentProductionImaging BIT NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'IsHavayarEquipment') IS NULL ALTER TABLE Rpr.RepairRequest ADD IsHavayarEquipment BIT NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'InspectionPersonelId') IS NULL ALTER TABLE Rpr.RepairRequest ADD InspectionPersonelId BIGINT NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'ExpertIds') IS NULL ALTER TABLE Rpr.RepairRequest ADD ExpertIds NVARCHAR(50) NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'ExpertsInText') IS NULL ALTER TABLE Rpr.RepairRequest ADD ExpertsInText NVARCHAR(255) NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'StationId') IS NULL ALTER TABLE Rpr.RepairRequest ADD StationId BIGINT NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'ConfirmMiladiDate') IS NULL ALTER TABLE Rpr.RepairRequest ADD ConfirmMiladiDate DATETIME2 NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'ConfirmShamsiDate') IS NULL ALTER TABLE Rpr.RepairRequest ADD ConfirmShamsiDate NVARCHAR(30) NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'ConfirmTime') IS NULL ALTER TABLE Rpr.RepairRequest ADD ConfirmTime NVARCHAR(5) NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'Confirm2MiladiDate') IS NULL ALTER TABLE Rpr.RepairRequest ADD Confirm2MiladiDate DATETIME2 NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'Confirm2ShamsiDate') IS NULL ALTER TABLE Rpr.RepairRequest ADD Confirm2ShamsiDate NVARCHAR(30) NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'IsConfirmed2') IS NULL ALTER TABLE Rpr.RepairRequest ADD IsConfirmed2 BIT NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'StartMiladiDate') IS NULL ALTER TABLE Rpr.RepairRequest ADD StartMiladiDate DATETIME2 NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'StartShamsiDate') IS NULL ALTER TABLE Rpr.RepairRequest ADD StartShamsiDate NVARCHAR(30) NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'EndMiladiDate') IS NULL ALTER TABLE Rpr.RepairRequest ADD EndMiladiDate DATETIME2 NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'EndShamsiDate') IS NULL ALTER TABLE Rpr.RepairRequest ADD EndShamsiDate NVARCHAR(30) NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'RepairEntranceMiladiDate') IS NULL ALTER TABLE Rpr.RepairRequest ADD RepairEntranceMiladiDate DATETIME2 NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'RepairEntranceShamsiDate') IS NULL ALTER TABLE Rpr.RepairRequest ADD RepairEntranceShamsiDate NVARCHAR(30) NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'WarehouseDeliveryMiladiDate') IS NULL ALTER TABLE Rpr.RepairRequest ADD WarehouseDeliveryMiladiDate DATETIME2 NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'WarehouseDeliveryShamsiDate') IS NULL ALTER TABLE Rpr.RepairRequest ADD WarehouseDeliveryShamsiDate NVARCHAR(30) NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'AgreedMiladiDate') IS NULL ALTER TABLE Rpr.RepairRequest ADD AgreedMiladiDate DATETIME2 NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'AgreedShamsiDate') IS NULL ALTER TABLE Rpr.RepairRequest ADD AgreedShamsiDate NVARCHAR(30) NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'Agreed2MiladiDate') IS NULL ALTER TABLE Rpr.RepairRequest ADD Agreed2MiladiDate DATETIME2 NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'Agreed2ShamsiDate') IS NULL ALTER TABLE Rpr.RepairRequest ADD Agreed2ShamsiDate NVARCHAR(30) NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'Agreed3MiladiDate') IS NULL ALTER TABLE Rpr.RepairRequest ADD Agreed3MiladiDate DATETIME2 NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'Agreed3ShamsiDate') IS NULL ALTER TABLE Rpr.RepairRequest ADD Agreed3ShamsiDate NVARCHAR(30) NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'DeliveringRecipient') IS NULL ALTER TABLE Rpr.RepairRequest ADD DeliveringRecipient NVARCHAR(256) NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'BuyRequestMiladiDate') IS NULL ALTER TABLE Rpr.RepairRequest ADD BuyRequestMiladiDate DATETIME2 NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'BuyRequestShamsiDate') IS NULL ALTER TABLE Rpr.RepairRequest ADD BuyRequestShamsiDate NVARCHAR(30) NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'SupplyMiladiDate') IS NULL ALTER TABLE Rpr.RepairRequest ADD SupplyMiladiDate DATETIME2 NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'SupplyShamsiDate') IS NULL ALTER TABLE Rpr.RepairRequest ADD SupplyShamsiDate NVARCHAR(30) NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'QcRequestMiladiDate') IS NULL ALTER TABLE Rpr.RepairRequest ADD QcRequestMiladiDate DATETIME2 NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'QcRequestShamsiDate') IS NULL ALTER TABLE Rpr.RepairRequest ADD QcRequestShamsiDate NVARCHAR(30) NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'PartFractionIds') IS NULL ALTER TABLE Rpr.RepairRequest ADD PartFractionIds NVARCHAR(256) NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'PartFractionsInText') IS NULL ALTER TABLE Rpr.RepairRequest ADD PartFractionsInText NVARCHAR(1024) NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'PreCheckMiladiDate') IS NULL ALTER TABLE Rpr.RepairRequest ADD PreCheckMiladiDate DATETIME2 NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'PreCheckShamsiDate') IS NULL ALTER TABLE Rpr.RepairRequest ADD PreCheckShamsiDate NVARCHAR(30) NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'PreCheck2MiladiDate') IS NULL ALTER TABLE Rpr.RepairRequest ADD PreCheck2MiladiDate DATETIME2 NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'PreCheck2ShamsiDate') IS NULL ALTER TABLE Rpr.RepairRequest ADD PreCheck2ShamsiDate NVARCHAR(30) NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'IsPreChecked2') IS NULL ALTER TABLE Rpr.RepairRequest ADD IsPreChecked2 BIT NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'FinancialProposalMiladiDate') IS NULL ALTER TABLE Rpr.RepairRequest ADD FinancialProposalMiladiDate DATETIME2 NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'FinancialProposalShamsiDate') IS NULL ALTER TABLE Rpr.RepairRequest ADD FinancialProposalShamsiDate NVARCHAR(30) NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'FinancialProposal2MiladiDate') IS NULL ALTER TABLE Rpr.RepairRequest ADD FinancialProposal2MiladiDate DATETIME2 NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'FinancialProposal2ShamsiDate') IS NULL ALTER TABLE Rpr.RepairRequest ADD FinancialProposal2ShamsiDate NVARCHAR(30) NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'IsApprovedFinancialProposal2') IS NULL ALTER TABLE Rpr.RepairRequest ADD IsApprovedFinancialProposal2 BIT NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'IsSentFinancialProposal') IS NULL ALTER TABLE Rpr.RepairRequest ADD IsSentFinancialProposal BIT NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'FinancialProposalSentMiladiDate') IS NULL ALTER TABLE Rpr.RepairRequest ADD FinancialProposalSentMiladiDate DATETIME2 NULL;
IF COL_LENGTH('Rpr.RepairRequest', 'FinancialProposalSentShamsiDate') IS NULL ALTER TABLE Rpr.RepairRequest ADD FinancialProposalSentShamsiDate NVARCHAR(30) NULL;

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

INSERT INTO Rpr.RepairRequest (
    HtsId, IdNumber, CustomerId, CustomerAddressId, CustomerAddressManual, ProductId, Serial, ZoneId,
    IsGuarantee, Unrepairable, NeedForSendToEmployer,
    EntryMiladiDate, EntryShamsiDate, CustomerNeedMiladiDate, CustomerNeedShamsiDate,
    ExitMiladiDate, ExitShamsiDate, Status, ConfirmerId, ResponsibleId, HasDelay,
    FailureReason, RepairDescription, Comment, HasContractor,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
    ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
SELECT CAST(s.RepairRequest_ID AS BIGINT), s.IDNumber, c.Id, a.Id, s.CustomerAddressManual, p.Id, s.Serial, z.Id,
    s.IsGuarantee, s.Unrepairable, s.NeedForSendToEmployer,
    s.EntryDate, s.EntryDate_Shamsi, s.CustomerNeedDate, s.CustomerNeedDate_Shamsi,
    s.ExitDate, s.ExitDate_Shamsi, s.RepairRequestStatus_FK, umc.NewUserId, umr.NewUserId, s.HasDelay,
    s.FailureReason, s.RepairDescription, s.Comment, s.HasContractor,
    COALESCE(umCreate.NewUserId, 1), COALESCE(umCreate.NewUserName, @SeedUser),
    COALESCE(cd.CreatedMiladi, @Now), COALESCE(cd.CreatedShamsi, @NowShamsi),
    umEdit.NewUserId, umEdit.NewUserName, COALESCE(md.ModifiedMiladi, @Now), COALESCE(md.ModifiedShamsi, @NowShamsi), 1
FROM [TMS].[TotalSystem].[dbo].[Rpr_RepairRequest] s
LEFT JOIN SLS.Customer c ON c.HtsId = s.Customer_FK AND ISNULL(c.HtsId,0) <> 0
LEFT JOIN SLS.CustomerAddress a ON a.HtsId = s.CustomerAddressId AND ISNULL(a.HtsId,0) <> 0
LEFT JOIN Inv.Part p ON p.HtsId = s.Product_FK
LEFT JOIN Crm.Zone z ON z.HtsId = s.Zone_FK
LEFT JOIN #UserMap umc ON umc.OldUserId = s.ConfirmerUser_FK
LEFT JOIN #UserMap umr ON umr.OldUserId = s.ResponsiblePersonnel_FK
LEFT JOIN #UserMap umCreate ON umCreate.OldUserId = s.CreatedUser_FK
LEFT JOIN #UserMap umEdit ON umEdit.OldUserId = s.UpdatedUserId
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
WHERE NOT EXISTS (SELECT 1 FROM Rpr.RepairRequest x WHERE x.HtsId = CAST(s.RepairRequest_ID AS BIGINT));

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
