/*
Sync_ProductionOrderDelay_FromTotalSystem.sql
انتقال تاخیر توقف سفارش ساخت از TotalSystem.dbo.Pln_ProductionOrder_Delay
به HavayarApp.Pln.ProductionOrderDelay

قاعده (تأییدشده):
  - فقط ردیف‌های قلمی منتقل می‌شود که جمع روز ثبت‌شده‌اش در آن سفارش از بقیه بیشتر است.
  - اگر جمع روز مساوی بود: قلم با تعداد ردیف بیشتر؛ اگر باز هم مساوی بود شناسه بزرگ‌تر قلم.
  - تاخیر بقیه اقلام همان سفارش داخل توضیحات نوشته می‌شود.
  - ۳۴ ردیفی که سفارش سربرگ با سفارش والد قلم یکی نیست منتقل نمی‌شود و در توضیحات هم نمی‌آید.
  - ۵۱ ردیف فقط-قدیمی: اگر دقیقاً یک قلم جدید با همان کالا روی همان سفارش باشد به آن قلم وصل می‌شود.
    بقیه بدون قلم می‌مانند و کد کالا در توضیحات می‌آید.
  - سه نسخه عیناً تکراری (همان قلم، عامل، بازه و متن) حذف می‌شود. زوجی که متنش فرق دارد هر دو می‌ماند.
  - تاریخ پایان خالی، خالی می‌ماند و تعداد روز هم خالی می‌ماند.
  - توجیه و متولی در ستون جدا ذخیره می‌شود.
  - ۳۱ ردیف دستی سیستم جدید (HtsId = 0) دست نمی‌خورد.
  - اجرای دوباره ردیف‌های همین اسکریپت را جایگزین می‌کند.

Encoding: UTF-8 with BOM
Apply:
  sqlcmd -S 172.20.40.42 -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Sync_ProductionOrderDelay_FromTotalSystem.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1);
    RETURN;
END;

IF COL_LENGTH(N'Pln.ProductionOrderDelay', N'HtsId') IS NULL
BEGIN
    ALTER TABLE [Pln].[ProductionOrderDelay]
        ADD [HtsId] BIGINT NOT NULL CONSTRAINT DF_ProductionOrderDelay_HtsId DEFAULT (0);
END;

IF COL_LENGTH(N'Pln.ProductionOrderDelay', N'JustificationComment') IS NULL
BEGIN
    ALTER TABLE [Pln].[ProductionOrderDelay]
        ADD [JustificationComment] NVARCHAR(1024) NULL;
END;

IF COL_LENGTH(N'Pln.ProductionOrderDelay', N'JustificationMiladiDateTime') IS NULL
BEGIN
    ALTER TABLE [Pln].[ProductionOrderDelay]
        ADD [JustificationMiladiDateTime] DATETIME2 NULL;
END;

IF COL_LENGTH(N'Pln.ProductionOrderDelay', N'JustificationShamsiDateTime') IS NULL
BEGIN
    ALTER TABLE [Pln].[ProductionOrderDelay]
        ADD [JustificationShamsiDateTime] NVARCHAR(30) NULL;
END;

IF COL_LENGTH(N'Pln.ProductionOrderDelay', N'TrusteeName') IS NULL
BEGIN
    ALTER TABLE [Pln].[ProductionOrderDelay]
        ADD [TrusteeName] NVARCHAR(200) NULL;
END;

IF EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = N'Pln'
      AND TABLE_NAME = N'ProductionOrderDelay'
      AND COLUMN_NAME = N'Description'
      AND (DATA_TYPE <> N'nvarchar' OR CHARACTER_MAXIMUM_LENGTH <> -1)
)
BEGIN
    ALTER TABLE [Pln].[ProductionOrderDelay] ALTER COLUMN [Description] NVARCHAR(MAX) NULL;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ProductionOrderDelay_HtsId'
      AND object_id = OBJECT_ID(N'Pln.ProductionOrderDelay')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ProductionOrderDelay_HtsId
        ON [Pln].[ProductionOrderDelay]([HtsId])
        WHERE [HtsId] <> 0;
END;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @OrphanItem INT;
SELECT @OrphanItem = COUNT(*)
FROM [TMS].[TotalSystem].[dbo].[Pln_ProductionOrder_Delay] d
LEFT JOIN [TMS].[TotalSystem].[dbo].[Pln_ProductionOrderItem] hi ON hi.Id = d.ProductionOrderItemNewId
WHERE d.ProductionOrderItemNewId IS NOT NULL
  AND hi.Id IS NULL;

IF @OrphanItem > 0
BEGIN
    RAISERROR(N'قلم جدید برخی تاخیرها در سیستم قدیم پیدا نشد.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'tempdb..#D') IS NOT NULL DROP TABLE #D;
SELECT
    d.Id,
    d.ProductionOrderId,
    h.Number AS PoNumber,
    d.ProductionOrderItemId,
    d.ProductionOrderItemNewId,
    d.DelayResponsibleId,
    d.FromDate,
    d.ToDate,
    d.FromDateInText,
    d.ToDateInText,
    d.DelayDateDifferenceInDay AS DelayDaysRaw,
    ISNULL(d.DelayDateDifferenceInDay, 0) AS Days,
    ISNULL(d.DelayReason, N'') AS DelayReason,
    ISNULL(d.Comment, N'') AS Comment,
    ISNULL(l.Lookup_Title_Fa, N'') AS ResponsibleTitle,
    COALESCE(pNew.Part_Code, pOld.Part_Code, N'') AS PartCode,
    hi.RahkaranId AS ItemRahkaranId,
    ISNULL(d.JustificationComment, N'') AS JustificationComment,
    d.JustificationDate,
    d.JustificationDateInText,
    LTRIM(RTRIM(CONCAT(ISNULL(prs.PrsName, N''), N' ', ISNULL(prs.PrsFamily, N'')))) AS TrusteeName,
    d.CreatedUserId,
    d.CreatedDate,
    d.CreatedDateInText
INTO #D
FROM [TMS].[TotalSystem].[dbo].[Pln_ProductionOrder_Delay] d
JOIN [TMS].[TotalSystem].[dbo].[Pln_ProductionOrder] h ON h.Id = d.ProductionOrderId
LEFT JOIN [TMS].[TotalSystem].[dbo].[Pln_ProductionOrderItem] hi ON hi.Id = d.ProductionOrderItemNewId
LEFT JOIN [TMS].[TotalSystem].[dbo].[Inv_Part] pNew ON pNew.Part_ID = hi.PartId
LEFT JOIN [TMS].[TotalSystem].[dbo].[Pln_ProductionOrder_Itm] itm ON itm.ProductionOrder_Itm_ID = d.ProductionOrderItemId
LEFT JOIN [TMS].[TotalSystem].[dbo].[Inv_Part] pOld ON pOld.Part_ID = itm.Part_FK
LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_Lookup] l ON l.Lookup_ID = d.DelayResponsibleId
LEFT JOIN [TMS].[TotalSystem].[dbo].[HRM_Personel] prs ON prs.Personel_ID = d.TrusteePersonnelId
WHERE d.ProductionOrderItemNewId IS NULL
   OR d.ProductionOrderId = hi.ProductionOrderId;

;WITH ranked AS (
    SELECT
        Id,
        ROW_NUMBER() OVER (
            PARTITION BY ProductionOrderItemNewId, DelayResponsibleId, FromDate, ToDate, DelayReason, Comment
            ORDER BY Id
        ) AS Rn
    FROM #D
    WHERE ProductionOrderItemNewId IS NOT NULL
)
DELETE d
FROM #D d
JOIN ranked r ON r.Id = d.Id AND r.Rn > 1;

IF OBJECT_ID(N'tempdb..#Leg') IS NOT NULL DROP TABLE #Leg;
SELECT
    k.Id AS DelayId,
    COUNT(*) AS Cnt,
    MIN(ni.Id) AS MatchedItemId,
    MIN(ni.RahkaranId) AS MatchedRahkaranId,
    MIN(p.Part_Code) AS MatchedPartCode
INTO #Leg
FROM #D k
JOIN [TMS].[TotalSystem].[dbo].[Pln_ProductionOrder_Itm] itm ON itm.ProductionOrder_Itm_ID = k.ProductionOrderItemId
JOIN [TMS].[TotalSystem].[dbo].[Pln_ProductionOrderItem] ni
    ON ni.ProductionOrderId = k.ProductionOrderId
   AND ni.PartId = itm.Part_FK
LEFT JOIN [TMS].[TotalSystem].[dbo].[Inv_Part] p ON p.Part_ID = ni.PartId
WHERE k.ProductionOrderItemNewId IS NULL
GROUP BY k.Id;

IF OBJECT_ID(N'tempdb..#Res') IS NOT NULL DROP TABLE #Res;
SELECT
    d.Id,
    d.ProductionOrderId,
    d.PoNumber,
    d.ProductionOrderItemId,
    d.DelayResponsibleId,
    d.FromDate,
    d.ToDate,
    d.FromDateInText,
    d.ToDateInText,
    d.DelayDaysRaw,
    d.Days,
    d.DelayReason,
    d.Comment,
    d.ResponsibleTitle,
    CASE
        WHEN d.ProductionOrderItemNewId IS NOT NULL THEN d.PartCode
        WHEN lg.Cnt = 1 THEN ISNULL(lg.MatchedPartCode, N'')
        ELSE d.PartCode
    END AS PartCode,
    CASE
        WHEN d.ProductionOrderItemNewId IS NOT NULL THEN d.ProductionOrderItemNewId
        WHEN lg.Cnt = 1 THEN lg.MatchedItemId
        ELSE NULL
    END AS ResolvedItemId,
    CASE
        WHEN d.ProductionOrderItemNewId IS NOT NULL THEN d.ItemRahkaranId
        WHEN lg.Cnt = 1 THEN lg.MatchedRahkaranId
        ELSE NULL
    END AS ItemRahkaranId,
    COALESCE(
        CASE
            WHEN d.ProductionOrderItemNewId IS NOT NULL THEN d.ProductionOrderItemNewId
            WHEN lg.Cnt = 1 THEN lg.MatchedItemId
            ELSE NULL
        END,
        -d.ProductionOrderItemId
    ) AS GroupKey,
    d.JustificationComment,
    d.JustificationDate,
    d.JustificationDateInText,
    d.TrusteeName,
    d.CreatedUserId,
    d.CreatedDate,
    d.CreatedDateInText
INTO #Res
FROM #D d
LEFT JOIN #Leg lg ON lg.DelayId = d.Id;

IF EXISTS (SELECT 1 FROM #Res WHERE GroupKey IS NULL)
BEGIN
    RAISERROR(N'برخی ردیف‌ها نه قلم جدید دارند و نه قلم قدیمی.', 16, 1);
    RETURN;
END;

IF EXISTS (
    SELECT 1
    FROM #Res
    WHERE DelayResponsibleId NOT IN (
        752, 753, 754, 755, 756, 757, 758, 759, 760, 761,
        2376, 2422, 2423, 2424, 2425, 2426, 2427, 2428, 2429, 2430, 2431, 2540
    )
)
BEGIN
    RAISERROR(N'عامل توقف ناشناخته در داده مبدأ وجود دارد.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'tempdb..#G') IS NOT NULL DROP TABLE #G;
SELECT
    ProductionOrderId,
    GroupKey,
    MAX(ResolvedItemId) AS ResolvedItemId,
    SUM(Days) AS ItemDays,
    COUNT(*) AS ItemRows
INTO #G
FROM #Res
GROUP BY ProductionOrderId, GroupKey;

IF OBJECT_ID(N'tempdb..#WinKey') IS NOT NULL DROP TABLE #WinKey;
;WITH ranked AS (
    SELECT
        g.*,
        ROW_NUMBER() OVER (
            PARTITION BY g.ProductionOrderId
            ORDER BY g.ItemDays DESC, g.ItemRows DESC, g.GroupKey DESC
        ) AS Rn
    FROM #G g
)
SELECT ProductionOrderId, GroupKey, ResolvedItemId, ItemDays, ItemRows
INTO #WinKey
FROM ranked
WHERE Rn = 1;

IF OBJECT_ID(N'tempdb..#Narr') IS NOT NULL DROP TABLE #Narr;
SELECT
    r.ProductionOrderId,
    N'سایر اقلام این سفارش:' + NCHAR(10) + STRING_AGG(CAST(
        N'قلم با کد محصول ' + ISNULL(NULLIF(r.PartCode, N''), N'-')
        + N' از تاریخ ' + ISNULL(NULLIF(r.FromDateInText, N''), N'-')
        + N' تا تاریخ ' + ISNULL(NULLIF(r.ToDateInText, N''), N'بدون پایان')
        + N' تاخیر به علت ' + ISNULL(NULLIF(r.DelayReason, N''), N'-')
        + N' توضیحات ' + ISNULL(NULLIF(r.Comment, N''), N'-')
        + N' عامل توقف ' + ISNULL(NULLIF(LTRIM(RTRIM(r.ResponsibleTitle)), N''), N'-')
        + CASE
            WHEN NULLIF(r.JustificationComment, N'') IS NOT NULL OR NULLIF(r.TrusteeName, N'') IS NOT NULL
                THEN N' توجیه ' + ISNULL(NULLIF(r.JustificationComment, N''), N'-')
                   + N' متولی ' + ISNULL(NULLIF(r.TrusteeName, N''), N'-')
            ELSE N''
          END
        AS NVARCHAR(MAX)), NCHAR(10)) WITHIN GROUP (ORDER BY r.FromDate, r.Id) AS Text
INTO #Narr
FROM #Res r
WHERE NOT EXISTS (
    SELECT 1
    FROM #WinKey w
    WHERE w.ProductionOrderId = r.ProductionOrderId
      AND w.GroupKey = r.GroupKey
)
GROUP BY r.ProductionOrderId;

IF OBJECT_ID(N'tempdb..#UserMap') IS NOT NULL DROP TABLE #UserMap;
SELECT
    CAST(ou.User_ID AS INT) AS OldUserId,
    nu.Id AS NewUserId,
    COALESCE(NULLIF(LTRIM(RTRIM(nu.NameFa)), N''), nu.Name, nu.Username) AS NewUserName
INTO #UserMap
FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
INNER JOIN [system].[User] nu ON nu.Username = ou.ActiveDirectoryUsername
WHERE EXISTS (SELECT 1 FROM #Res r WHERE r.CreatedUserId = ou.User_ID);

IF EXISTS (
    SELECT 1
    FROM #UserMap
    GROUP BY OldUserId
    HAVING COUNT(*) > 1
)
BEGIN
    RAISERROR(N'یک کاربر سیستم قدیم به بیش از یک کاربر سیستم جدید وصل شد.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'tempdb..#Win') IS NOT NULL DROP TABLE #Win;
SELECT
    r.*,
    w.ResolvedItemId AS WinnerItemId,
    n.Text AS NarrativeText,
    um.NewUserId,
    um.NewUserName,
    po.Id AS AppPoId,
    po.PoCnt,
    it.AppItemId,
    it.ItemCnt
INTO #Win
FROM #Res r
JOIN #WinKey w
    ON w.ProductionOrderId = r.ProductionOrderId
   AND w.GroupKey = r.GroupKey
LEFT JOIN #Narr n ON n.ProductionOrderId = r.ProductionOrderId
LEFT JOIN #UserMap um ON um.OldUserId = r.CreatedUserId
OUTER APPLY (
    SELECT MIN(p.Id) AS Id, COUNT(*) AS PoCnt
    FROM [Sale].[ProductionOrder] p
    WHERE p.ProductionOrderNumber = r.PoNumber
) po
OUTER APPLY (
    SELECT MIN(ai.Id) AS AppItemId, COUNT(*) AS ItemCnt
    FROM [Sale].[ProductionOrderItem] ai
    WHERE r.ResolvedItemId IS NOT NULL
      AND ai.RahkaranHistoryId = CASE
            WHEN r.ItemRahkaranId IS NOT NULL THEN CAST(r.ItemRahkaranId AS BIGINT)
            ELSE -CAST(r.ResolvedItemId AS BIGINT)
          END
) it;

IF EXISTS (SELECT 1 FROM #Win WHERE AppPoId IS NULL OR PoCnt <> 1)
BEGIN
    RAISERROR(N'سفارش ساخت معادل در سیستم جدید یکتا پیدا نشد.', 16, 1);
    RETURN;
END;

IF EXISTS (SELECT 1 FROM #Win WHERE ResolvedItemId IS NOT NULL AND (AppItemId IS NULL OR ItemCnt <> 1))
BEGIN
    RAISERROR(N'قلم سفارش ساخت معادل در سیستم جدید یکتا پیدا نشد.', 16, 1);
    RETURN;
END;

IF EXISTS (SELECT 1 FROM #Win WHERE NewUserId IS NULL)
BEGIN
    RAISERROR(N'کاربر ثبت‌کننده برخی تاخیرها در سیستم جدید پیدا نشد.', 16, 1);
    RETURN;
END;

IF EXISTS (SELECT 1 FROM #Win WHERE LEN(DelayReason) > 2048 OR LEN(JustificationComment) > 1024 OR LEN(ISNULL(TrusteeName, N'')) > 200 OR LEN(ISNULL(NewUserName, N'')) > 150)
BEGIN
    RAISERROR(N'طول یکی از فیلدهای متنی از ستون مقصد بیشتر است.', 16, 1);
    RETURN;
END;

DECLARE @ManualBefore INT = (SELECT COUNT(*) FROM [Pln].[ProductionOrderDelay] WHERE [HtsId] = 0);
DECLARE @Expected INT = (SELECT COUNT(*) FROM #Win);

BEGIN TRANSACTION;

DELETE FROM [Pln].[ProductionOrderDelay] WHERE [HtsId] <> 0;

INSERT INTO [Pln].[ProductionOrderDelay]
(
    [HtsId],
    [ProductionOrderId],
    [ProductionOrderItemId],
    [DelayResponsible],
    [DelayDays],
    [DelayReason],
    [DelayStartMiladiDate],
    [DelayStartShamsiDate],
    [DelayEndMiladiDate],
    [DelayEndShamsiDate],
    [Description],
    [JustificationComment],
    [JustificationMiladiDateTime],
    [JustificationShamsiDateTime],
    [TrusteeName],
    [CreatedById],
    [CreatedByName],
    [CreatedOnMiladiDateTime],
    [CreatedOnShamsiDateTime],
    [IsActive]
)
SELECT
    w.Id,
    w.AppPoId,
    w.AppItemId,
    w.DelayResponsibleId,
    w.DelayDaysRaw,
    NULLIF(w.DelayReason, N''),
    CAST(w.FromDate AS DATETIME2),
    w.FromDateInText,
    CAST(w.ToDate AS DATETIME2),
    NULLIF(w.ToDateInText, N''),
    NULLIF(
        CONCAT(
            CASE
                WHEN w.ResolvedItemId IS NULL
                    THEN N'قلم با کد محصول ' + ISNULL(NULLIF(w.PartCode, N''), N'-')
                       + N' (قلم قدیمی بدون معادل یکتا)' + NCHAR(10) + NCHAR(10)
                ELSE N''
            END,
            CASE WHEN w.Comment <> N'' THEN w.Comment ELSE N'' END,
            CASE
                WHEN w.Comment <> N'' AND w.NarrativeText IS NOT NULL THEN NCHAR(10) + NCHAR(10)
                ELSE N''
            END,
            CASE WHEN w.NarrativeText IS NOT NULL THEN w.NarrativeText ELSE N'' END
        ),
        N''
    ),
    NULLIF(w.JustificationComment, N''),
    w.JustificationDate,
    NULLIF(w.JustificationDateInText, N''),
    NULLIF(w.TrusteeName, N''),
    w.NewUserId,
    w.NewUserName,
    w.CreatedDate,
    w.CreatedDateInText,
    1
FROM #Win w;

IF @@ROWCOUNT <> @Expected
BEGIN
    THROW 50001, N'تعداد ردیف درج‌شده با انتظار یکی نیست.', 1;
END;

IF (SELECT COUNT(*) FROM [Pln].[ProductionOrderDelay] WHERE [HtsId] = 0) <> @ManualBefore
BEGIN
    THROW 50002, N'ردیف‌های دستی سیستم جدید تغییر کردند.', 1;
END;

IF EXISTS (
    SELECT [HtsId]
    FROM [Pln].[ProductionOrderDelay]
    WHERE [HtsId] <> 0
    GROUP BY [HtsId]
    HAVING COUNT(*) > 1
)
BEGIN
    THROW 50003, N'شناسه HTS تکراری درج شد.', 1;
END;

IF EXISTS (SELECT 1 FROM [Pln].[ProductionOrderDelay] WHERE [HtsId] IN (845, 846, 847, 848))
BEGIN
    THROW 50004, N'ردیف‌های ناسازگار سفارش و قلم درج شدند.', 1;
END;

COMMIT TRANSACTION;

SELECT
    @Expected AS ImportedRows,
    @ManualBefore AS ManualRowsKept,
    (SELECT COUNT(*) FROM #WinKey) AS Orders,
    (SELECT COUNT(*) FROM #Win WHERE NarrativeText IS NOT NULL) AS ImportedRowsWithOtherItemsText,
    (SELECT COUNT(*) FROM #Win WHERE AppItemId IS NULL) AS ImportedRowsWithoutItem,
    (SELECT COUNT(*) FROM #Win WHERE DelayDaysRaw IS NULL) AS ImportedOpenEnded,
    (SELECT COUNT(*) FROM #Win WHERE NULLIF(JustificationComment, N'') IS NOT NULL) AS ImportedWithJustification,
    (SELECT COUNT(*) FROM #Win WHERE NULLIF(TrusteeName, N'') IS NOT NULL) AS ImportedWithTrustee,
    (SELECT MAX(LEN(Description)) FROM [Pln].[ProductionOrderDelay] WHERE [HtsId] <> 0) AS MaxDescriptionLen;
