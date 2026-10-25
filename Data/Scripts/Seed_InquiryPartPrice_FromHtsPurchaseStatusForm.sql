/*
================================================================================
Seed_InquiryPartPrice_FromHtsPurchaseStatusForm.sql

اقلام فرم وضعیت خرید HTS (نوع ۸۱ خرید و ۸۲ پیمانکاری) که در استعلام قیمت نیستند
را با شکل رکورد فاکتور خرید وارد Sup.InquiryPartPrice می‌کند:
  Type = 1676 (فاکتور خرید)
  PriceStatus = 2831 (خرید)

منبع: [TMS].[TotalSystem].dbo.Sup_Purchase_StatusForm + Sup_Purchase_StatusForm_Itm
همان دو نوعی که Vw_Sup_PartPrice_History در گزارش سوابق خرید نشان می‌دهد.

ضدتکرار:
  Hamkaran_VchItm_FK > 0  -> InvoiceItemId ؛ اگر از قبل باشد درج نمی‌شود
  Hamkaran_VchItm_FK = 0  -> InvoiceItemId خالی می‌ماند و نشانه
                             hts-psf-item:{Purchase_StatusForm_Product_ID}. داخل Comment است

Encoding: UTF-8 with BOM.
sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Seed_InquiryPartPrice_FromHtsPurchaseStatusForm.sql"
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @SeedUser NVARCHAR(150) = N'seed-hts-purchase-status-form';

IF OBJECT_ID('tempdb..#HtsPurchase') IS NOT NULL DROP TABLE #HtsPurchase;
IF OBJECT_ID('tempdb..#PartMap') IS NOT NULL DROP TABLE #PartMap;
IF OBJECT_ID('tempdb..#ExistingInvoiceItem') IS NOT NULL DROP TABLE #ExistingInvoiceItem;
IF OBJECT_ID('tempdb..#ExistingHtsItem') IS NOT NULL DROP TABLE #ExistingHtsItem;
IF OBJECT_ID('tempdb..#Ready') IS NOT NULL DROP TABLE #Ready;

PRINT N'=== [1] خواندن اقلام فرم وضعیت خرید از HTS ===';

SELECT
    i.Purchase_StatusForm_Product_ID,
    CAST(ISNULL(i.Hamkaran_VchItm_FK, 0) AS bigint) AS HamkaranVchItmId,
    p.Purchase_StatusForm_ID,
    p.StatusForm_Type_FK,
    p.PRC3InvoiceId,
    p.CreatedDateLatin,
    RTRIM(p.CreatedDate) AS CreatedDate,
    RTRIM(p.CreatedTime) AS CreatedTime,
    hp.Hamkaran_Part_FK,
    i.BuyCount,
    CAST(CAST(
        CASE
            WHEN i.BuyCount IS NULL OR i.BuyCount = 0 THEN ISNULL(i.UnitPrice, 0)
            ELSE ISNULL(i.UnitPrice, 0) - (CAST(ISNULL(i.Discount_Payment, 0) AS decimal(18, 4)) / i.BuyCount)
        END
    AS bigint) AS decimal(18, 2)) AS UnitPrice,
    NULLIF(LEFT(LTRIM(RTRIM(i.FactorNum)), 100), N'') AS InvoiceNumber,
    LEFT(LTRIM(RTRIM(
        CASE
            WHEN c.CompanyName IS NULL OR LEN(LTRIM(RTRIM(c.CompanyName))) < 3 THEN c.FullName
            ELSE c.CompanyName
        END
    )), 1000) AS CompanyName,
    LEFT(LTRIM(RTRIM(u.FullName)), 150) AS CreatorName,
    ROW_NUMBER() OVER (
        PARTITION BY CASE
            WHEN ISNULL(i.Hamkaran_VchItm_FK, 0) > 0 THEN CAST(i.Hamkaran_VchItm_FK AS bigint)
            ELSE -CAST(i.Purchase_StatusForm_Product_ID AS bigint)
        END
        ORDER BY p.CreatedDateLatin DESC, p.Purchase_StatusForm_ID DESC, i.Purchase_StatusForm_Product_ID DESC
    ) AS rn
INTO #HtsPurchase
FROM [TMS].[TotalSystem].[dbo].[Sup_Purchase_StatusForm_Itm] AS i
INNER JOIN [TMS].[TotalSystem].[dbo].[Sup_Purchase_StatusForm] AS p
    ON p.Purchase_StatusForm_ID = i.Purchase_StatusForm_FK
LEFT JOIN [TMS].[TotalSystem].[dbo].[Inv_Part] AS hp
    ON hp.Part_ID = i.Part_FK
LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_ManCompany] AS c
    ON c.ManCompany_ID = p.Company_FK
LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] AS u
    ON u.User_ID = p.CreatedUser_FK
WHERE p.StatusForm_Type_FK IN (81, 82);

CREATE CLUSTERED INDEX IX_HtsPurchase_Rn ON #HtsPurchase (rn, HamkaranVchItmId, Purchase_StatusForm_Product_ID);

PRINT N'=== [2] نگاشت کالا و کلیدهای موجود ===';

SELECT HamkaranId, Id
INTO #PartMap
FROM (
    SELECT
        HamkaranId,
        Id,
        ROW_NUMBER() OVER (
            PARTITION BY HamkaranId
            ORDER BY CASE WHEN IsActive = 1 THEN 0 ELSE 1 END, Id
        ) AS rn
    FROM Inv.Part
    WHERE HamkaranId IS NOT NULL
) AS d
WHERE d.rn = 1;

CREATE UNIQUE CLUSTERED INDEX IX_PartMap_HamkaranId ON #PartMap (HamkaranId);

SELECT DISTINCT InvoiceItemId
INTO #ExistingInvoiceItem
FROM Sup.InquiryPartPrice
WHERE InvoiceItemId IS NOT NULL;

CREATE CLUSTERED INDEX IX_ExistingInvoiceItem ON #ExistingInvoiceItem (InvoiceItemId);

SELECT DISTINCT TRY_CAST(
    SUBSTRING(
        Comment,
        LEN(N'hts-psf-item:') + 1,
        CHARINDEX(N'.', Comment) - LEN(N'hts-psf-item:') - 1
    ) AS int
) AS ProductId
INTO #ExistingHtsItem
FROM Sup.InquiryPartPrice
WHERE Comment LIKE N'hts-psf-item:%'
  AND CHARINDEX(N'.', Comment) > LEN(N'hts-psf-item:');

DELETE FROM #ExistingHtsItem WHERE ProductId IS NULL;
CREATE CLUSTERED INDEX IX_ExistingHtsItem ON #ExistingHtsItem (ProductId);

PRINT N'=== [3] ردیف‌های آماده درج ===';

SELECT
    s.Purchase_StatusForm_Product_ID,
    s.HamkaranVchItmId,
    s.Purchase_StatusForm_ID,
    s.StatusForm_Type_FK,
    s.PRC3InvoiceId,
    s.CreatedDateLatin,
    s.CreatedDate,
    s.CreatedTime,
    s.BuyCount,
    s.UnitPrice,
    s.InvoiceNumber,
    s.CompanyName,
    s.CreatorName,
    part.Id AS PartId
INTO #Ready
FROM #HtsPurchase AS s
INNER JOIN #PartMap AS part
    ON part.HamkaranId = s.Hamkaran_Part_FK
WHERE s.rn = 1
  AND (
        (
            s.HamkaranVchItmId > 0
            AND NOT EXISTS (
                SELECT 1
                FROM #ExistingInvoiceItem AS x
                WHERE x.InvoiceItemId = s.HamkaranVchItmId
            )
        )
        OR
        (
            s.HamkaranVchItmId = 0
            AND NOT EXISTS (
                SELECT 1
                FROM #ExistingHtsItem AS h
                WHERE h.ProductId = s.Purchase_StatusForm_Product_ID
            )
        )
      );

DECLARE @SourceLines int = (SELECT COUNT(*) FROM #HtsPurchase);
DECLARE @DistinctKeys int = (SELECT COUNT(*) FROM #HtsPurchase WHERE rn = 1);
DECLARE @MissingPart int = (
    SELECT COUNT(*)
    FROM #HtsPurchase AS s
    WHERE s.rn = 1
      AND NOT EXISTS (SELECT 1 FROM #PartMap AS part WHERE part.HamkaranId = s.Hamkaran_Part_FK)
);
DECLARE @Ready int = (SELECT COUNT(*) FROM #Ready);
DECLARE @ReadyZeroVch int = (SELECT COUNT(*) FROM #Ready WHERE HamkaranVchItmId = 0);
DECLARE @AlreadyPresent int = @DistinctKeys - @MissingPart - @Ready;

PRINT N'=== [4] درج ===';

BEGIN TRY
    BEGIN TRANSACTION;

    INSERT INTO Sup.InquiryPartPrice
        (Type, CompanyName, PartId, BuyCount, PriceUnitValue, TransportationCost, UnitPrice,
         IsFixedPrice, Comment, IsSpecialCases, PriceStatus, PriceUnitId,
         CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
         IsActive, InvoiceId, InvoiceItemId, InvoiceNumber)
    SELECT
        1676,
        r.CompanyName,
        r.PartId,
        CAST(ROUND(r.BuyCount, 0) AS int),
        r.UnitPrice,
        CAST(0 AS decimal(18, 2)),
        r.UnitPrice,
        CAST(0 AS bit),
        LEFT(
            N'hts-psf-item:' + CAST(r.Purchase_StatusForm_Product_ID AS nvarchar(20))
            + N'. فرم '
            + CASE r.StatusForm_Type_FK WHEN 81 THEN N'خرید' WHEN 82 THEN N'پیمانکاری' ELSE N'' END
            + N' شماره ' + CAST(r.Purchase_StatusForm_ID AS nvarchar(20))
            + N'. از فرم وضعیت خرید اضافه شده.',
            2800),
        CAST(0 AS bit),
        2831,
        CAST(1 AS bigint),
        COALESCE(r.CreatorName, @SeedUser),
        @SeedUser,
        TRY_CONVERT(datetime2,
            CONVERT(varchar(10), r.CreatedDateLatin, 23) + ' ' +
            CASE
                WHEN r.CreatedTime LIKE '[0-9][0-9]:[0-9][0-9]' THEN r.CreatedTime + ':00'
                ELSE '00:00:00'
            END),
        LEFT(RTRIM(ISNULL(r.CreatedDate, N'')) + N' ' + RTRIM(ISNULL(r.CreatedTime, N'')), 30),
        TRY_CONVERT(datetime2,
            CONVERT(varchar(10), r.CreatedDateLatin, 23) + ' ' +
            CASE
                WHEN r.CreatedTime LIKE '[0-9][0-9]:[0-9][0-9]' THEN r.CreatedTime + ':00'
                ELSE '00:00:00'
            END),
        LEFT(RTRIM(ISNULL(r.CreatedDate, N'')) + N' ' + RTRIM(ISNULL(r.CreatedTime, N'')), 30),
        1,
        CASE WHEN r.HamkaranVchItmId > 0 THEN r.PRC3InvoiceId ELSE NULL END,
        CASE WHEN r.HamkaranVchItmId > 0 THEN r.HamkaranVchItmId ELSE NULL END,
        r.InvoiceNumber
    FROM #Ready AS r;

    DECLARE @Inserted int = @@ROWCOUNT;

    COMMIT TRANSACTION;

    PRINT N'درج انجام شد';

    SELECT
        @SourceLines AS SourceLines,
        @DistinctKeys AS DistinctKeys,
        @MissingPart AS MissingPart,
        @AlreadyPresent AS AlreadyPresent,
        @Ready AS Ready,
        @ReadyZeroVch AS ReadyZeroVch,
        @Inserted AS Inserted;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@Err, 16, 1);
    RETURN;
END CATCH;

PRINT N'=== [5] بررسی کد 1410111032 ===';

SELECT
    ip.Id,
    ip.Type,
    ip.PriceStatus,
    ip.CompanyName,
    ip.BuyCount,
    ip.UnitPrice,
    ip.InvoiceNumber,
    ip.InvoiceItemId,
    ip.CreatedOnShamsiDateTime,
    ip.Comment
FROM Sup.InquiryPartPrice AS ip
INNER JOIN Inv.Part AS part ON part.Id = ip.PartId
WHERE part.Code = N'1410111032'
  AND ip.Comment LIKE N'hts-psf-item:286288.%';
