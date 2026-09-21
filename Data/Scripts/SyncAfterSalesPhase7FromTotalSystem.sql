/*
================================================================================
SyncAfterSalesPhase7FromTotalSystem.sql
================================================================================
وصول مطالبات، فیش وصول، پیوست وصول (فقط عنوان؛ باینری کپی نمی‌شود).
MERGE روی HtsId. کاربر مسئول فیش: Gnr_User.Username → system.User.Username.
FK بدون نگاشت لاگ می‌شود؛ کل دسته فقط با @Strict=1 شکست می‌خورد.
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Strict BIT = 0;
DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'sync-after-sales-phase7';
DECLARE @Rc INT;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1); RETURN; END;

BEGIN TRY
    EXEC sp_testlinkedserver [TMS];
END TRY
BEGIN CATCH
    DECLARE @Ls NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'sp_testlinkedserver [TMS] ناموفق: %s', 16, 1, @Ls);
    RETURN;
END CATCH;

BEGIN TRY
BEGIN TRANSACTION;

IF OBJECT_ID('tempdb..#Unmapped') IS NOT NULL DROP TABLE #Unmapped;
CREATE TABLE #Unmapped (Kind NVARCHAR(80) NOT NULL, OldId BIGINT NULL, Detail NVARCHAR(400) NULL);

SELECT CAST(ou.User_ID AS BIGINT) AS OldUserId, MIN(nu.Id) AS NewUserId
INTO #UserMap
FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
INNER JOIN [system].[User] nu
    ON nu.Username = ou.Username
    OR (NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N'') IS NOT NULL
        AND nu.Username = LTRIM(RTRIM(ou.ActiveDirectoryUsername)))
GROUP BY CAST(ou.User_ID AS BIGINT);

PRINT N'=== Sale.CollectionClaim ===';
MERGE Sale.CollectionClaim AS t
USING (
    SELECT CAST(s.Id AS BIGINT) AS HtsId, c.Id AS CustomerId, dl.Id AS DlId, s.VchNumber,
           s.HdrVchId AS HtsHdrVchId, s.VchItmID AS HtsVchItemId,
           s.VchDate AS VchMiladiDate, s.VchDateShamsi AS VchShamsiDate, s.Dl_Title AS DlTitle,
           s.VchDescription, s.Debit, s.Credit
    FROM [TMS].[TotalSystem].[dbo].[Sale_CollectionClaim] s
    LEFT JOIN SLS.Customer c ON c.HtsId = s.Customer_FK AND c.HtsId <> 0
    LEFT JOIN FIN.DL dl ON dl.HamkaranId = s.Dl_FK AND dl.HamkaranId <> 0
) AS s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.CustomerId=s.CustomerId, t.DlId=s.DlId, t.VchNumber=s.VchNumber,
    t.HtsHdrVchId=s.HtsHdrVchId, t.HtsVchItemId=s.HtsVchItemId, t.VchMiladiDate=s.VchMiladiDate,
    t.VchShamsiDate=s.VchShamsiDate, t.DlTitle=s.DlTitle, t.VchDescription=s.VchDescription,
    t.Debit=s.Debit, t.Credit=s.Credit, t.ModifiedById=1, t.ModifiedByName=@SeedUser,
    t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED THEN INSERT (HtsId, CustomerId, DlId, VchNumber, HtsHdrVchId, HtsVchItemId, VchMiladiDate,
    VchShamsiDate, DlTitle, VchDescription, Debit, Credit,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedById, ModifiedByName,
    ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
VALUES (s.HtsId, s.CustomerId, s.DlId, s.VchNumber, s.HtsHdrVchId, s.HtsVchItemId, s.VchMiladiDate,
    s.VchShamsiDate, s.DlTitle, s.VchDescription, s.Debit, s.Credit,
    1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);
SET @Rc = @@ROWCOUNT;
PRINT N'CollectionClaim MERGE touched: ' + CAST(@Rc AS NVARCHAR(20));

INSERT INTO #Unmapped (Kind, OldId, Detail)
SELECT N'CollectionClaimCustomer', CAST(s.Id AS BIGINT), N'Customer_FK not mapped'
FROM [TMS].[TotalSystem].[dbo].[Sale_CollectionClaim] s
WHERE NOT EXISTS (SELECT 1 FROM SLS.Customer c WHERE c.HtsId = s.Customer_FK AND c.HtsId <> 0);

PRINT N'=== Sale.CollectionClaimPayFish ===';
IF OBJECT_ID('tempdb..#TmsPayFish') IS NOT NULL DROP TABLE #TmsPayFish;
SELECT * INTO #TmsPayFish
FROM OPENQUERY([TMS], N'
    SELECT
        CAST(Id AS BIGINT) AS HtsId,
        CAST(CollectionClaims_FK AS BIGINT) AS ClaimHtsId,
        CollectedPrice,
        Uncollectable,
        HasDiscount,
        IsAgency,
        LEFT(PayFish_Date, 10) AS PayFishShamsiDate,
        CAST(ResponsibleId AS BIGINT) AS ResponsibleUserHtsId,
        LEFT(FishNumber, 400) AS FishNumber,
        LEFT(Comment, 4000) AS Comment
    FROM dbo.Sale_CollectionClaim_PayFish
');

MERGE Sale.CollectionClaimPayFish AS t
USING (
    SELECT s.HtsId, cc.Id AS CollectionClaimId, s.CollectedPrice, s.Uncollectable, s.HasDiscount, s.IsAgency,
           s.PayFishShamsiDate, um.NewUserId AS ResponsibleId, s.FishNumber, s.Comment
    FROM #TmsPayFish s
    INNER JOIN Sale.CollectionClaim cc ON cc.HtsId = s.ClaimHtsId AND cc.HtsId <> 0
    LEFT JOIN #UserMap um ON um.OldUserId = s.ResponsibleUserHtsId
) AS s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.CollectionClaimId=s.CollectionClaimId, t.CollectedPrice=s.CollectedPrice,
    t.Uncollectable=s.Uncollectable, t.HasDiscount=s.HasDiscount, t.IsAgency=s.IsAgency,
    t.PayFishShamsiDate=s.PayFishShamsiDate, t.ResponsibleId=s.ResponsibleId, t.FishNumber=s.FishNumber,
    t.Comment=s.Comment, t.ModifiedById=1, t.ModifiedByName=@SeedUser,
    t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED THEN INSERT (HtsId, CollectionClaimId, CollectedPrice, Uncollectable, HasDiscount, IsAgency,
    PayFishShamsiDate, ResponsibleId, FishNumber, Comment,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedById, ModifiedByName,
    ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
VALUES (s.HtsId, s.CollectionClaimId, s.CollectedPrice, s.Uncollectable, s.HasDiscount, s.IsAgency,
    s.PayFishShamsiDate, s.ResponsibleId, s.FishNumber, s.Comment,
    1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);
SET @Rc = @@ROWCOUNT;
PRINT N'CollectionClaimPayFish MERGE touched: ' + CAST(@Rc AS NVARCHAR(20));

INSERT INTO #Unmapped (Kind, OldId, Detail)
SELECT N'PayFishParent', s.HtsId, N'CollectionClaims_FK not mapped'
FROM #TmsPayFish s
WHERE NOT EXISTS (SELECT 1 FROM Sale.CollectionClaim cc WHERE cc.HtsId = s.ClaimHtsId AND cc.HtsId <> 0);

INSERT INTO #Unmapped (Kind, OldId, Detail)
SELECT N'PayFishResponsible', s.HtsId, N'ResponsibleId user not mapped'
FROM #TmsPayFish s
WHERE s.ResponsibleUserHtsId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM #UserMap u WHERE u.OldUserId = s.ResponsibleUserHtsId);

PRINT N'=== Sale.CollectionClaimAttachment (metadata only; no binary) ===';
IF OBJECT_ID('tempdb..#TmsClaimAtt') IS NOT NULL DROP TABLE #TmsClaimAtt;
SELECT * INTO #TmsClaimAtt
FROM OPENQUERY([TMS], N'
    SELECT
        CAST(Id AS BIGINT) AS HtsId,
        CAST(CollectionClaim_FK AS BIGINT) AS ClaimHtsId,
        LEFT(Attachment_FileName, 256) AS Title
    FROM dbo.Sale_CollectionClaim_Attachment
');

MERGE Sale.CollectionClaimAttachment AS t
USING (
    SELECT s.HtsId, cc.Id AS CollectionClaimId, s.Title
    FROM #TmsClaimAtt s
    INNER JOIN Sale.CollectionClaim cc ON cc.HtsId = s.ClaimHtsId AND cc.HtsId <> 0
) AS s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.CollectionClaimId=s.CollectionClaimId, t.Title=s.Title,
    t.ModifiedById=1, t.ModifiedByName=@SeedUser, t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED THEN INSERT (HtsId, CollectionClaimId, Title,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedById, ModifiedByName,
    ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
VALUES (s.HtsId, s.CollectionClaimId, s.Title, 1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);
SET @Rc = @@ROWCOUNT;
PRINT N'CollectionClaimAttachment MERGE touched: ' + CAST(@Rc AS NVARCHAR(20));

INSERT INTO #Unmapped (Kind, OldId, Detail)
SELECT N'CollectionClaimAttachment', s.HtsId, N'CollectionClaim parent not mapped'
FROM #TmsClaimAtt s
WHERE NOT EXISTS (SELECT 1 FROM Sale.CollectionClaim cc WHERE cc.HtsId = s.ClaimHtsId AND cc.HtsId <> 0);

SELECT Kind, COUNT(*) AS Cnt FROM #Unmapped GROUP BY Kind ORDER BY Kind;
SELECT TOP 20 * FROM #Unmapped ORDER BY Kind, OldId;

SELECT
    (SELECT COUNT(*) FROM Sale.CollectionClaim) AS ClaimHavayar,
    (SELECT COUNT(*) FROM Sale.CollectionClaimPayFish) AS PayFishHavayar,
    (SELECT COUNT(*) FROM Sale.CollectionClaimAttachment) AS ClaimAttHavayar;

IF @Strict = 1 AND EXISTS (SELECT 1 FROM #Unmapped)
BEGIN
    RAISERROR(N'@Strict=1 and unmapped FK rows remain in phase 7.', 16, 1);
END;

COMMIT TRANSACTION;
PRINT N'=== DONE SyncAfterSalesPhase7FromTotalSystem ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
