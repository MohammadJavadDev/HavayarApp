/*
================================================================================
Report_Party_DuplicateHamkaranId.sql  (WP5 / D12 — فقط SELECT، بدون تغییر داده)
================================================================================
علت خرابی جاب SyncBuyCategoryJobFromRahkaran (Def 30) از 2026-08-18:
  «An item with the same key has already been added. Key: 286»
  → چند ردیف system.[User] به یک Gnr.Party.HamkaranId مشترک اشاره می‌کنند
    (286 = Soleimani.a / soleimani.ali و ۴ زوج دیگر).

این اسکریپت تکراری‌ها را گزارش می‌دهد تا تیم داده تصمیم بگیرد کدام کاربر باید
Party/HamkaranId متفاوت بگیرد یا غیرفعال شود.
ایندکس یکتا اضافه نمی‌شود (تکراری‌های موجود آن را شکست می‌دهند) — جاب با GroupBy مقاوم شده است.

اجرا روی دیتابیس HavayarApp.
================================================================================
*/
SET NOCOUNT ON;

-----------------------------------------------------------------------------
PRINT N'=== [1/4] HamkaranId های مشترک بین چند کاربر (system.User → Gnr.Party.HamkaranId) ===';
-----------------------------------------------------------------------------
;WITH UserParty AS
(
    SELECT
        u.Id            AS UserId,
        u.Username,
        u.Name          AS UserName_Fa,
        u.IsActive      AS UserIsActive,
        u.PartyId,
        p.HamkaranId    AS PartyHamkaranId,
        p.FullName      AS PartyFullName,
        u.HamkaranId    AS UserHamkaranId
    FROM system.[User] u
    INNER JOIN Gnr.Party p ON p.Id = u.PartyId
    WHERE p.HamkaranId IS NOT NULL
)
SELECT
    up.PartyHamkaranId,
    COUNT(*) OVER (PARTITION BY up.PartyHamkaranId)                      AS UsersSharingThisHamkaranId,
    up.UserId,
    up.Username,
    up.UserName_Fa,
    up.UserIsActive,
    up.PartyId,
    up.PartyFullName,
    up.UserHamkaranId,
    -- برنده‌ای که جاب انتخاب می‌کند: کاربر فعال، سپس کم‌ترین Id
    CASE WHEN ROW_NUMBER() OVER (PARTITION BY up.PartyHamkaranId
                                 ORDER BY CASE WHEN up.UserIsActive = 1 THEN 0 ELSE 1 END, up.UserId) = 1
         THEN 1 ELSE 0 END                                               AS JobPicksThisUser,
    (SELECT COUNT(*) FROM Sup.BuyCategory bc WHERE bc.PurchaseResponsibleId = up.UserId) AS BuyCategoriesAsResponsible
FROM UserParty up
WHERE up.PartyHamkaranId IN
(
    SELECT PartyHamkaranId FROM UserParty GROUP BY PartyHamkaranId HAVING COUNT(*) > 1
)
ORDER BY up.PartyHamkaranId, JobPicksThisUser DESC, up.UserId;

-----------------------------------------------------------------------------
PRINT N'=== [2/4] HamkaranId های مشترک روی ستون system.User.HamkaranId (InquiryPartPriceJob) ===';
-----------------------------------------------------------------------------
SELECT
    u.HamkaranId,
    COUNT(*) OVER (PARTITION BY u.HamkaranId) AS UsersSharingThisHamkaranId,
    u.Id AS UserId,
    u.Username,
    u.Name AS UserName_Fa,
    u.IsActive AS UserIsActive,
    u.PartyId
FROM system.[User] u
WHERE u.HamkaranId IS NOT NULL
  AND u.HamkaranId IN (SELECT HamkaranId FROM system.[User] WHERE HamkaranId IS NOT NULL GROUP BY HamkaranId HAVING COUNT(*) > 1)
ORDER BY u.HamkaranId, u.IsActive DESC, u.Id;

-----------------------------------------------------------------------------
PRINT N'=== [3/4] Party هایی که خودشان HamkaranId تکراری دارند (چند Party با یک HamkaranId) ===';
-----------------------------------------------------------------------------
SELECT
    p.HamkaranId,
    COUNT(*) OVER (PARTITION BY p.HamkaranId) AS PartiesSharingThisHamkaranId,
    p.Id AS PartyId,
    p.FullName,
    p.IsActive,
    (SELECT COUNT(*) FROM system.[User] u WHERE u.PartyId = p.Id) AS LinkedUsers
FROM Gnr.Party p
WHERE p.HamkaranId IS NOT NULL
  AND p.HamkaranId IN (SELECT HamkaranId FROM Gnr.Party WHERE HamkaranId IS NOT NULL GROUP BY HamkaranId HAVING COUNT(*) > 1)
ORDER BY p.HamkaranId, p.Id;

-----------------------------------------------------------------------------
PRINT N'=== [4/4] خلاصه ===';
-----------------------------------------------------------------------------
SELECT
    (SELECT COUNT(*) FROM (SELECT p.HamkaranId
                           FROM system.[User] u INNER JOIN Gnr.Party p ON p.Id = u.PartyId
                           WHERE p.HamkaranId IS NOT NULL
                           GROUP BY p.HamkaranId HAVING COUNT(*) > 1) x) AS DuplicatePartyHamkaranIds_AcrossUsers,
    (SELECT COUNT(*) FROM (SELECT HamkaranId FROM system.[User]
                           WHERE HamkaranId IS NOT NULL
                           GROUP BY HamkaranId HAVING COUNT(*) > 1) x)   AS DuplicateUserHamkaranIds,
    (SELECT COUNT(*) FROM (SELECT HamkaranId FROM Gnr.Party
                           WHERE HamkaranId IS NOT NULL
                           GROUP BY HamkaranId HAVING COUNT(*) > 1) x)   AS DuplicatePartyHamkaranIds,
    (SELECT COUNT(*) FROM Sup.BuyCategory)                                AS BuyCategoryCount,
    (SELECT COUNT(*) FROM Sup.BuyCategory WHERE PurchaseResponsibleId IS NULL) AS BuyCategoryWithoutResponsible;
