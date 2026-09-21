/*
SyncAfterSalesPhase3FromTotalSystem.sql
TenderManagement + ProductGroup/Part/Attachment/Comment/Task
Company: Gnr_ManCompany.Hamkaran_ManCompany_FK -> Gnr.Party.HamkaranId (fallback SLS.Customer.HamkaranId)
SaleExpert: Gnr_User.Personel_FK = SaleExpertId then Username -> system.User
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'sync-after-sales-phase3';
IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1); RETURN; END;

BEGIN TRY
BEGIN TRANSACTION;

SELECT CAST(ou.User_ID AS BIGINT) AS OldUserId, MIN(nu.Id) AS NewUserId
INTO #UserMap
FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
INNER JOIN [system].[User] nu ON nu.Username = ou.Username
GROUP BY CAST(ou.User_ID AS BIGINT);

SELECT CAST(ou.Personel_FK AS BIGINT) AS PersonelId, MIN(nu.Id) AS NewUserId
INTO #ExpertMap
FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
INNER JOIN [system].[User] nu ON nu.Username = ou.Username
WHERE ou.Personel_FK IS NOT NULL
GROUP BY CAST(ou.Personel_FK AS BIGINT);

SELECT CAST(s.Id AS BIGINT) AS HtsId,
       s.RequestNumber,
       CAST(s.ParentId AS BIGINT) AS ParentHtsId,
       CAST(s.OrganizationId AS INT) AS Organization,
       COALESCE(p.Id, c.PartyId) AS CompanyId,
       s.SalesAgentId AS SalesAgent,
       s.ProjectName,
       s.InquiryTypeId AS InquiryType,
       em.NewUserId AS SaleExpertId,
       s.IsBudget, s.HasBudget, s.HasBudgetChild,
       s.CurrentStatusId AS CurrentStatus,
       s.FinalResultId AS FinalResult,
       s.FailureReasonText,
       s.InCartable, s.HasPrimitiveConfirm, s.HasSecondaryConfirm,
       rc.Id AS InstallCountryId,
       rp.Id AS InstallProvinceId,
       s.CustomerAgentInfo, s.CustomerAgentPhone, s.EndUser, s.ContractNumber, s.Explain,
       s.PreInvoicePrice, s.PriceUnitId AS PriceUnit, LEFT(s.PreInvoiceDate, 16) AS PreInvoiceShamsiDate,
       s.PreInvoicePrice2, s.PreInvoicePriceUnitId2 AS PreInvoicePriceUnit2,
       s.ProjectValueId AS ProjectValue,
       s.OurPrice, s.OurPriceUnitId AS OurPriceUnit, s.FixedPrice, s.CompetitivePrice, s.CompetitiveCompany,
       s.EuroPrice, s.Price, s.IsHavayar, s.ContractPrice,
       s.SendForApproveDateTime AS SendForApproveMiladiDateTime,
       s.SendForApproveDateTimeInText AS SendForApproveShamsiDateTime,
       ISNULL(s.IsComputedForSalesStatistics, 0) AS IsComputedForSalesStatistics,
       s.CompressorTechPriceUnitId AS CompressorTechPriceUnit,
       s.CompressorTechPrice, s.CompressorTechPriceRate, s.CompressorTechPriceInRial,
       s.TenderNumber, s.TenderTypeId AS TenderType,
       s.WarrantyNumber, s.WarrantyTypeID AS WarrantyType, s.WarrantyPrice, s.WarrantyStatusId AS WarrantyStatus,
       s.WarrantyDueDate AS WarrantyDueMiladiDate, s.WarrantyDueDateInText AS WarrantyDueShamsiDate,
       CAST(s.SaleProjectReportId AS BIGINT) AS SaleProjectReportId,
       s.IndicatorId,
       s.TenderRegisterDate AS TenderRegisterMiladiDate, s.TenderRegisterDateInText AS TenderRegisterShamsiDate,
       s.SendingDocumentsDeadlineDate AS SendingDocumentsDeadlineMiladiDate,
       s.SendingDocumentsDeadlineDateInText AS SendingDocumentsDeadlineShamsiDate,
       s.SendingTechnicalDocumentsDate AS SendingTechnicalDocumentsMiladiDate,
       s.SendingTechnicalDocumentsDateInText AS SendingTechnicalDocumentsShamsiDate,
       s.SendingTechnicalProposalDate AS SendingTechnicalProposalMiladiDate,
       s.SendingTechnicalProposalInText AS SendingTechnicalProposalShamsiDate,
       s.NumberOfParticipatingCompanies,
       s.ParticipatingTenderCompaniesNames, s.TenderWinnerName,
       s.QualitativeAssessmentQuorum, s.HavayarScore, s.HavayarRank,
       s.ChangePreInvoiceToFinancialProposal,
       s.FailureCauseId AS FailureCause, s.FailureCauseDescription,
       LEFT(CONVERT(nvarchar(4000), mc.Address), 4000) AS CompanyAddress,
       (
           SELECT LEFT(STRING_AGG(v.Part, N' '), 200)
           FROM (VALUES
               (NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(100), mc.Phone))), N'')),
               (NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(200), mc.Office_Tel))), N'')),
               (NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(100), mc.Mobile))), N''))
           ) v(Part)
           WHERE v.Part IS NOT NULL
       ) AS CompanyPhone,
       pii.Id AS CompanyIndustryItemId,
       umc.NewUserId AS CreatedById,
       s.CreatedDateTime AS CreatedOnMiladiDateTime,
       s.CreatedDateInText AS CreatedOnShamsiDateTime
INTO #TenderSrc
FROM [TMS].[TotalSystem].[dbo].[Sale_TenderManagement] s
LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_ManCompany] mc ON mc.ManCompany_ID = s.CompanyId
OUTER APPLY (
    SELECT TOP (1) i.Id
    FROM Gnr.PartyIndustryItem i
    WHERE mc.IndustryItm_FK IS NOT NULL
      AND i.Code = CAST(mc.IndustryItm_FK AS BIGINT)
    ORDER BY i.Id
) pii
OUTER APPLY (
    SELECT TOP (1) p.Id
    FROM Gnr.Party p
    WHERE (mc.Hamkaran_ManCompany_FK IS NOT NULL AND mc.Hamkaran_ManCompany_FK <> 0
            AND p.HamkaranId = CAST(mc.Hamkaran_ManCompany_FK AS BIGINT))
       OR p.HamkaranId = CAST(mc.ManCompany_ID AS BIGINT)
    ORDER BY CASE
        WHEN mc.Hamkaran_ManCompany_FK IS NOT NULL AND mc.Hamkaran_ManCompany_FK <> 0
             AND p.HamkaranId = CAST(mc.Hamkaran_ManCompany_FK AS BIGINT) THEN 0
        ELSE 1 END, p.Id
) p
OUTER APPLY (
    SELECT TOP (1) c.PartyId
    FROM SLS.Customer c
    WHERE mc.Hamkaran_ManCompany_FK IS NOT NULL AND mc.Hamkaran_ManCompany_FK <> 0
      AND c.HamkaranId = CAST(mc.Hamkaran_ManCompany_FK AS BIGINT)
    ORDER BY c.Id
) c
LEFT JOIN #ExpertMap em ON em.PersonelId = CAST(s.SaleExpertId AS BIGINT)
LEFT JOIN #UserMap umc ON umc.OldUserId = CAST(s.CreatedUserId AS BIGINT)
LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_Country] gc ON gc.Country_ID = s.InstallCountryId
OUTER APPLY (
    SELECT TOP (1) rc.Id
    FROM Gnr.Region rc
    WHERE rc.Type = 1 AND gc.Country_ID IS NOT NULL AND (
        rc.RahkaranId = CAST(gc.Country_ID AS BIGINT)
        OR rc.Name = gc.Country_Title)
    ORDER BY CASE WHEN rc.RahkaranId = CAST(gc.Country_ID AS BIGINT) THEN 0 ELSE 1 END, rc.Id
) rc
LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_Province] gp ON gp.Province_ID = s.InstallProvinceId
OUTER APPLY (
    SELECT TOP (1) rp.Id
    FROM Gnr.Region rp
    WHERE rp.Type = 2 AND gp.Province_ID IS NOT NULL AND (
        rp.RahkaranId = CAST(gp.Province_ID AS BIGINT)
        OR rp.Name = gp.Province_Title)
    ORDER BY CASE WHEN rp.RahkaranId = CAST(gp.Province_ID AS BIGINT) THEN 0 ELSE 1 END, rp.Id
) rp;

DECLARE @TmBefore INT = (SELECT COUNT(*) FROM Sale.TenderManagement);
DECLARE @TmAfter INT;
DECLARE @TmCompany INT;
DECLARE @TpgCnt INT;
DECLARE @TpCnt INT;
DECLARE @TcCnt INT;
DECLARE @TtCnt INT;
DECLARE @TaCnt INT;

MERGE Sale.TenderManagement AS t
USING #TenderSrc AS s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET
    t.RequestNumber = s.RequestNumber,
    t.Organization = s.Organization,
    t.CompanyId = s.CompanyId,
    t.SalesAgent = s.SalesAgent,
    t.ProjectName = s.ProjectName,
    t.InquiryType = s.InquiryType,
    t.SaleExpertId = s.SaleExpertId,
    t.IsBudget = s.IsBudget, t.HasBudget = s.HasBudget, t.HasBudgetChild = s.HasBudgetChild,
    t.CurrentStatus = s.CurrentStatus, t.FinalResult = s.FinalResult, t.FailureReasonText = s.FailureReasonText,
    t.InCartable = s.InCartable, t.HasPrimitiveConfirm = s.HasPrimitiveConfirm, t.HasSecondaryConfirm = s.HasSecondaryConfirm,
    t.InstallCountryId = s.InstallCountryId, t.InstallProvinceId = s.InstallProvinceId,
    t.CustomerAgentInfo = s.CustomerAgentInfo, t.CustomerAgentPhone = s.CustomerAgentPhone,
    t.EndUser = s.EndUser, t.ContractNumber = s.ContractNumber, t.Explain = s.Explain,
    t.PreInvoicePrice = s.PreInvoicePrice, t.PriceUnit = s.PriceUnit, t.PreInvoiceShamsiDate = s.PreInvoiceShamsiDate,
    t.PreInvoicePrice2 = s.PreInvoicePrice2, t.PreInvoicePriceUnit2 = s.PreInvoicePriceUnit2,
    t.ProjectValue = s.ProjectValue,
    t.OurPrice = s.OurPrice, t.OurPriceUnit = s.OurPriceUnit, t.FixedPrice = s.FixedPrice,
    t.CompetitivePrice = s.CompetitivePrice, t.CompetitiveCompany = s.CompetitiveCompany,
    t.EuroPrice = s.EuroPrice, t.Price = s.Price, t.IsHavayar = s.IsHavayar, t.ContractPrice = s.ContractPrice,
    t.SendForApproveMiladiDateTime = s.SendForApproveMiladiDateTime, t.SendForApproveShamsiDateTime = s.SendForApproveShamsiDateTime,
    t.IsComputedForSalesStatistics = s.IsComputedForSalesStatistics,
    t.CompressorTechPriceUnit = s.CompressorTechPriceUnit, t.CompressorTechPrice = s.CompressorTechPrice,
    t.CompressorTechPriceRate = s.CompressorTechPriceRate, t.CompressorTechPriceInRial = s.CompressorTechPriceInRial,
    t.TenderNumber = s.TenderNumber, t.TenderType = s.TenderType,
    t.WarrantyNumber = s.WarrantyNumber, t.WarrantyType = s.WarrantyType, t.WarrantyPrice = s.WarrantyPrice,
    t.WarrantyStatus = s.WarrantyStatus, t.WarrantyDueMiladiDate = s.WarrantyDueMiladiDate, t.WarrantyDueShamsiDate = s.WarrantyDueShamsiDate,
    t.SaleProjectReportId = s.SaleProjectReportId, t.IndicatorId = s.IndicatorId,
    t.TenderRegisterMiladiDate = s.TenderRegisterMiladiDate, t.TenderRegisterShamsiDate = s.TenderRegisterShamsiDate,
    t.SendingDocumentsDeadlineMiladiDate = s.SendingDocumentsDeadlineMiladiDate,
    t.SendingDocumentsDeadlineShamsiDate = s.SendingDocumentsDeadlineShamsiDate,
    t.SendingTechnicalDocumentsMiladiDate = s.SendingTechnicalDocumentsMiladiDate,
    t.SendingTechnicalDocumentsShamsiDate = s.SendingTechnicalDocumentsShamsiDate,
    t.SendingTechnicalProposalMiladiDate = s.SendingTechnicalProposalMiladiDate,
    t.SendingTechnicalProposalShamsiDate = s.SendingTechnicalProposalShamsiDate,
    t.NumberOfParticipatingCompanies = s.NumberOfParticipatingCompanies,
    t.ParticipatingTenderCompaniesNames = s.ParticipatingTenderCompaniesNames,
    t.TenderWinnerName = s.TenderWinnerName,
    t.QualitativeAssessmentQuorum = s.QualitativeAssessmentQuorum,
    t.HavayarScore = s.HavayarScore, t.HavayarRank = s.HavayarRank,
    t.ChangePreInvoiceToFinancialProposal = s.ChangePreInvoiceToFinancialProposal,
    t.FailureCause = s.FailureCause, t.FailureCauseDescription = s.FailureCauseDescription,
    t.CompanyAddress = s.CompanyAddress, t.CompanyPhone = s.CompanyPhone,
    t.CompanyIndustryItemId = s.CompanyIndustryItemId,
    t.ModifiedById = 1, t.ModifiedByName = @SeedUser,
    t.ModifiedDateMiladiDateTime = @Now, t.ModifiedDateShamsiDateTime = @NowShamsi
WHEN NOT MATCHED THEN INSERT
    (HtsId, RequestNumber, Organization, CompanyId, SalesAgent, ProjectName, InquiryType, SaleExpertId,
     IsBudget, HasBudget, HasBudgetChild, CurrentStatus, FinalResult, FailureReasonText,
     InCartable, HasPrimitiveConfirm, HasSecondaryConfirm, InstallCountryId, InstallProvinceId,
     CustomerAgentInfo, CustomerAgentPhone, EndUser, ContractNumber, Explain,
     PreInvoicePrice, PriceUnit, PreInvoiceShamsiDate, PreInvoicePrice2, PreInvoicePriceUnit2, ProjectValue,
     OurPrice, OurPriceUnit, FixedPrice, CompetitivePrice, CompetitiveCompany, EuroPrice, Price, IsHavayar, ContractPrice,
     SendForApproveMiladiDateTime, SendForApproveShamsiDateTime, IsComputedForSalesStatistics,
     CompressorTechPriceUnit, CompressorTechPrice, CompressorTechPriceRate, CompressorTechPriceInRial,
     TenderNumber, TenderType, WarrantyNumber, WarrantyType, WarrantyPrice, WarrantyStatus,
     WarrantyDueMiladiDate, WarrantyDueShamsiDate, SaleProjectReportId, IndicatorId,
     TenderRegisterMiladiDate, TenderRegisterShamsiDate,
     SendingDocumentsDeadlineMiladiDate, SendingDocumentsDeadlineShamsiDate,
     SendingTechnicalDocumentsMiladiDate, SendingTechnicalDocumentsShamsiDate,
     SendingTechnicalProposalMiladiDate, SendingTechnicalProposalShamsiDate,
     NumberOfParticipatingCompanies, ParticipatingTenderCompaniesNames, TenderWinnerName,
     QualitativeAssessmentQuorum, HavayarScore, HavayarRank, ChangePreInvoiceToFinancialProposal,
     FailureCause, FailureCauseDescription, CompanyAddress, CompanyPhone, CompanyIndustryItemId,
     CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
     ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
VALUES
    (s.HtsId, s.RequestNumber, s.Organization, s.CompanyId, s.SalesAgent, s.ProjectName, s.InquiryType, s.SaleExpertId,
     s.IsBudget, s.HasBudget, s.HasBudgetChild, s.CurrentStatus, s.FinalResult, s.FailureReasonText,
     s.InCartable, s.HasPrimitiveConfirm, s.HasSecondaryConfirm, s.InstallCountryId, s.InstallProvinceId,
     s.CustomerAgentInfo, s.CustomerAgentPhone, s.EndUser, s.ContractNumber, s.Explain,
     s.PreInvoicePrice, s.PriceUnit, s.PreInvoiceShamsiDate, s.PreInvoicePrice2, s.PreInvoicePriceUnit2, s.ProjectValue,
     s.OurPrice, s.OurPriceUnit, s.FixedPrice, s.CompetitivePrice, s.CompetitiveCompany, s.EuroPrice, s.Price, s.IsHavayar, s.ContractPrice,
     s.SendForApproveMiladiDateTime, s.SendForApproveShamsiDateTime, s.IsComputedForSalesStatistics,
     s.CompressorTechPriceUnit, s.CompressorTechPrice, s.CompressorTechPriceRate, s.CompressorTechPriceInRial,
     s.TenderNumber, s.TenderType, s.WarrantyNumber, s.WarrantyType, s.WarrantyPrice, s.WarrantyStatus,
     s.WarrantyDueMiladiDate, s.WarrantyDueShamsiDate, s.SaleProjectReportId, s.IndicatorId,
     s.TenderRegisterMiladiDate, s.TenderRegisterShamsiDate,
     s.SendingDocumentsDeadlineMiladiDate, s.SendingDocumentsDeadlineShamsiDate,
     s.SendingTechnicalDocumentsMiladiDate, s.SendingTechnicalDocumentsShamsiDate,
     s.SendingTechnicalProposalMiladiDate, s.SendingTechnicalProposalShamsiDate,
     s.NumberOfParticipatingCompanies, s.ParticipatingTenderCompaniesNames, s.TenderWinnerName,
     s.QualitativeAssessmentQuorum, s.HavayarScore, s.HavayarRank, s.ChangePreInvoiceToFinancialProposal,
     s.FailureCause, s.FailureCauseDescription, s.CompanyAddress, s.CompanyPhone, s.CompanyIndustryItemId,
     ISNULL(s.CreatedById, 1), @SeedUser, ISNULL(s.CreatedOnMiladiDateTime, @Now), ISNULL(s.CreatedOnShamsiDateTime, @NowShamsi),
     1, @SeedUser, @Now, @NowShamsi, 1);

SET @TmAfter = (SELECT COUNT(*) FROM Sale.TenderManagement);
SET @TmCompany = (SELECT COUNT(*) FROM Sale.TenderManagement WHERE CompanyId IS NOT NULL);
PRINT N'TenderManagement rows now: ' + CAST(@TmAfter AS nvarchar(20))
    + N' (before ' + CAST(@TmBefore AS nvarchar(20)) + N')';
PRINT N'TenderManagement company mapped: ' + CAST(@TmCompany AS nvarchar(20));

;WITH McPick AS (
    SELECT p.Id AS PartyId,
           LEFT(CONVERT(nvarchar(4000), mc.Address), 2000) AS Address,
           (
               SELECT LEFT(STRING_AGG(v.Part, N' '), 50)
               FROM (VALUES
                   (NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(100), mc.Phone))), N'')),
                   (NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(200), mc.Office_Tel))), N'')),
                   (NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(100), mc.Mobile))), N''))
               ) v(Part)
               WHERE v.Part IS NOT NULL
           ) AS Phone,
           pii.PartyIndustryId
    FROM Gnr.Party p
    INNER JOIN (
        SELECT DISTINCT CompanyId
        FROM Sale.TenderManagement
        WHERE CompanyId IS NOT NULL
    ) t ON t.CompanyId = p.Id
    OUTER APPLY (
        SELECT TOP (1) mc.Address, mc.Phone, mc.Office_Tel, mc.Mobile, mc.IndustryItm_FK
        FROM [TMS].[TotalSystem].[dbo].[Gnr_ManCompany] mc
        WHERE p.HamkaranId IS NOT NULL AND (
            (mc.Hamkaran_ManCompany_FK IS NOT NULL AND mc.Hamkaran_ManCompany_FK <> 0
             AND CAST(mc.Hamkaran_ManCompany_FK AS BIGINT) = p.HamkaranId)
            OR CAST(mc.ManCompany_ID AS BIGINT) = p.HamkaranId
        )
        ORDER BY CASE
            WHEN mc.Hamkaran_ManCompany_FK IS NOT NULL AND mc.Hamkaran_ManCompany_FK <> 0
             AND CAST(mc.Hamkaran_ManCompany_FK AS BIGINT) = p.HamkaranId THEN 0
            ELSE 1 END, mc.ManCompany_ID
    ) mc
    OUTER APPLY (
        SELECT TOP (1) i.PartyIndustryId
        FROM Gnr.PartyIndustryItem i
        WHERE mc.IndustryItm_FK IS NOT NULL
          AND i.Code = CAST(mc.IndustryItm_FK AS BIGINT)
        ORDER BY i.Id
    ) pii
)
UPDATE p
SET p.Address = CASE WHEN McPick.Address IS NOT NULL THEN McPick.Address ELSE p.Address END,
    p.Phone = CASE WHEN McPick.Phone IS NOT NULL THEN McPick.Phone ELSE p.Phone END
FROM Gnr.Party p
INNER JOIN McPick ON McPick.PartyId = p.Id;

;WITH McPick AS (
    SELECT p.Id AS PartyId, pii.PartyIndustryId
    FROM Gnr.Party p
    INNER JOIN (
        SELECT DISTINCT CompanyId
        FROM Sale.TenderManagement
        WHERE CompanyId IS NOT NULL
    ) t ON t.CompanyId = p.Id
    OUTER APPLY (
        SELECT TOP (1) mc.IndustryItm_FK
        FROM [TMS].[TotalSystem].[dbo].[Gnr_ManCompany] mc
        WHERE p.HamkaranId IS NOT NULL AND (
            (mc.Hamkaran_ManCompany_FK IS NOT NULL AND mc.Hamkaran_ManCompany_FK <> 0
             AND CAST(mc.Hamkaran_ManCompany_FK AS BIGINT) = p.HamkaranId)
            OR CAST(mc.ManCompany_ID AS BIGINT) = p.HamkaranId
        )
        ORDER BY CASE
            WHEN mc.Hamkaran_ManCompany_FK IS NOT NULL AND mc.Hamkaran_ManCompany_FK <> 0
             AND CAST(mc.Hamkaran_ManCompany_FK AS BIGINT) = p.HamkaranId THEN 0
            ELSE 1 END, mc.ManCompany_ID
    ) mc
    OUTER APPLY (
        SELECT TOP (1) i.PartyIndustryId
        FROM Gnr.PartyIndustryItem i
        WHERE mc.IndustryItm_FK IS NOT NULL
          AND i.Code = CAST(mc.IndustryItm_FK AS BIGINT)
        ORDER BY i.Id
    ) pii
)
UPDATE c
SET c.IndustryId = McPick.PartyIndustryId
FROM SLS.Customer c
INNER JOIN McPick ON McPick.PartyId = c.PartyId
WHERE McPick.PartyIndustryId IS NOT NULL;

UPDATE t
SET t.ParentId = p.Id
FROM Sale.TenderManagement t
INNER JOIN #TenderSrc s ON s.HtsId = t.HtsId
INNER JOIN Sale.TenderManagement p ON p.HtsId = s.ParentHtsId
WHERE ISNULL(s.ParentHtsId, 0) <> 0;

MERGE Sale.TenderProductGroup AS t
USING (
    SELECT CAST(s.Id AS BIGINT) AS HtsId, s.Title
    FROM [TMS].[TotalSystem].[dbo].[Sale_TenderProductGroup] s
) AS s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.Title = s.Title,
    t.ModifiedById = 1, t.ModifiedByName = @SeedUser, t.ModifiedDateMiladiDateTime = @Now, t.ModifiedDateShamsiDateTime = @NowShamsi
WHEN NOT MATCHED THEN INSERT (HtsId, Title,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
    ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
VALUES (s.HtsId, s.Title, 1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);
SET @TpgCnt = (SELECT COUNT(*) FROM Sale.TenderProductGroup);
PRINT N'TenderProductGroup rows: ' + CAST(@TpgCnt AS nvarchar(20));

MERGE Sale.TenderPart AS t
USING (
    SELECT CAST(s.Id AS BIGINT) AS HtsId, tm.Id AS TenderId, p.Id AS PartId, pg.Id AS ProductGroupId, s.Number, s.NotExistProducts
    FROM [TMS].[TotalSystem].[dbo].[Sale_TenderPart] s
    LEFT JOIN Sale.TenderManagement tm ON tm.HtsId = s.TenderMangementId
    LEFT JOIN Inv.Part p ON p.HtsId = s.PartId
    LEFT JOIN Sale.TenderProductGroup pg ON pg.HtsId = CAST(s.ProductGroupId AS BIGINT)
) AS s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.TenderId = s.TenderId, t.PartId = s.PartId, t.ProductGroupId = s.ProductGroupId,
    t.Number = s.Number, t.NotExistProducts = s.NotExistProducts,
    t.ModifiedById = 1, t.ModifiedByName = @SeedUser, t.ModifiedDateMiladiDateTime = @Now, t.ModifiedDateShamsiDateTime = @NowShamsi
WHEN NOT MATCHED THEN INSERT (HtsId, TenderId, PartId, ProductGroupId, Number, NotExistProducts,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
    ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
VALUES (s.HtsId, s.TenderId, s.PartId, s.ProductGroupId, s.Number, s.NotExistProducts,
    1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);
SET @TpCnt = (SELECT COUNT(*) FROM Sale.TenderPart);
PRINT N'TenderPart rows: ' + CAST(@TpCnt AS nvarchar(20));

MERGE Sale.TenderComment AS t
USING (
    SELECT CAST(s.Id AS BIGINT) AS HtsId, tm.Id AS TenderId, s.Comment
    FROM [TMS].[TotalSystem].[dbo].[Sale_TenderComment] s
    LEFT JOIN Sale.TenderManagement tm ON tm.HtsId = s.TenderMangementId
) AS s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.TenderId = s.TenderId, t.Comment = s.Comment,
    t.ModifiedById = 1, t.ModifiedByName = @SeedUser, t.ModifiedDateMiladiDateTime = @Now, t.ModifiedDateShamsiDateTime = @NowShamsi
WHEN NOT MATCHED THEN INSERT (HtsId, TenderId, Comment,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
    ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
VALUES (s.HtsId, s.TenderId, s.Comment, 1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);
SET @TcCnt = (SELECT COUNT(*) FROM Sale.TenderComment);
PRINT N'TenderComment rows: ' + CAST(@TcCnt AS nvarchar(20));

MERGE Sale.TenderTask AS t
USING (
    SELECT CAST(s.Id AS BIGINT) AS HtsId, tm.Id AS TenderId, s.Comment
    FROM [TMS].[TotalSystem].[dbo].[Sale_Tender_TaskList] s
    LEFT JOIN Sale.TenderManagement tm ON tm.HtsId = s.TenderMangementId
) AS s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.TenderId = s.TenderId, t.Comment = s.Comment,
    t.ModifiedById = 1, t.ModifiedByName = @SeedUser, t.ModifiedDateMiladiDateTime = @Now, t.ModifiedDateShamsiDateTime = @NowShamsi
WHEN NOT MATCHED THEN INSERT (HtsId, TenderId, Comment,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
    ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
VALUES (s.HtsId, s.TenderId, s.Comment, 1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);
SET @TtCnt = (SELECT COUNT(*) FROM Sale.TenderTask);
PRINT N'TenderTask rows: ' + CAST(@TtCnt AS nvarchar(20));

MERGE Sale.TenderAttachment AS t
USING (
    SELECT CAST(s.Id AS BIGINT) AS HtsId, tm.Id AS TenderId,
           LEFT(s.Attachment_FileName, 256) AS Title,
           s.Attachment_Comment AS Comment
    FROM [TMS].[TotalSystem].[dbo].[Sale_TenderFileAttachment] s
    LEFT JOIN Sale.TenderManagement tm ON tm.HtsId = s.TenderManagementId
) AS s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.TenderId = s.TenderId, t.Title = s.Title, t.Comment = s.Comment,
    t.ModifiedById = 1, t.ModifiedByName = @SeedUser, t.ModifiedDateMiladiDateTime = @Now, t.ModifiedDateShamsiDateTime = @NowShamsi
WHEN NOT MATCHED THEN INSERT (HtsId, TenderId, Title, Comment,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
    ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
VALUES (s.HtsId, s.TenderId, s.Title, s.Comment, 1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);
SET @TaCnt = (SELECT COUNT(*) FROM Sale.TenderAttachment);
PRINT N'TenderAttachment rows (file blobs not copied): ' + CAST(@TaCnt AS nvarchar(20));

COMMIT;
PRINT N'Phase 3 sync committed';
END TRY
BEGIN CATCH IF @@TRANCOUNT > 0 ROLLBACK; THROW; END CATCH;
