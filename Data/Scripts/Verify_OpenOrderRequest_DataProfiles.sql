/*
  Verify_OpenOrderRequest_DataProfiles.sql
  WP2 — فقط SELECT (هیچ INSERT/UPDATE/DELETE).

  بعد از Update_OpenOrderRequest_DataProfiles_HtsParity.sql (@DryRun=0) اجرا کنید.
  قبل از اعمال هم قابل اجرا است: بخش «ذخیره‌شده» وضعیت فعلی SavedQuery را نشان می‌دهد
  و بخش «جوین مطلوب» همان LEFT/APPLY بدون fan-out را می‌شمارد (پیش‌نمایش اثر D34).

  خروجی‌ها:
    1) کاتالوگ ۸ نمایه + ستون‌ها + INNER JOIN باقی‌مانده + RoleAccess
    2) تعداد ردیف در برابر DISTINCT Id در برابر کل جدول (جوین مطلوب)
    3) درخواست‌های فعال که در هیچ نمایهٔ غیرپیشینه نمی‌آیند
    4) تکرار Id (fan-out)
    5) ماتریس RoleAccess (انتظار در برابر واقعیت)
    6) بهداشت: نمایهٔ فعال ناشناخته / JSON / رنگ / ActionOptions
*/
SET NOCOUNT ON;

DECLARE @EntityLower NVARCHAR(200) = N'entities.app.sup.openorderrequest';
DECLARE @EntityPascal NVARCHAR(200) = N'Entities.App.Sup.OpenOrderRequest';

-----------------------------------------------------------------------------
PRINT N'=== [1] کاتالوگ SavedQuery (ذخیره‌شده) ===';
-----------------------------------------------------------------------------
SELECT
    q.Id,
    q.Name,
    q.Title,
    q.Mode,
    q.Type,
    q.IsActive,
    CASE WHEN ISJSON(q.ColumnsJson) = 1 THEN (SELECT COUNT(*) FROM OPENJSON(q.ColumnsJson)) ELSE NULL END AS ColumnCount,
    CASE WHEN ISJSON(q.QueryJson) = 1 THEN 1 ELSE 0 END AS QueryJsonValid,
    CASE WHEN ISJSON(q.ColumnsJson) = 1 THEN 1 ELSE 0 END AS ColumnsJsonValid,
    CASE WHEN ISJSON(q.EventScriptsJson) = 1 THEN 1 ELSE 0 END AS EventScriptsValid,
    CASE WHEN ISJSON(q.ActionOptions) = 1 THEN 1 ELSE 0 END AS ActionOptionsValid,
    CASE WHEN ISJSON(q.CustomActionButtonsJson) = 1 THEN 1 ELSE 0 END AS ButtonsValid,
    CASE WHEN j.CustomQuery LIKE N'%INNER JOIN%BuyCategoryItem%' THEN 1 ELSE 0 END AS HasInnerJoinBuyCategoryItem,
    CASE WHEN j.CustomQuery LIKE N'%OUTER APPLY%' THEN 1 ELSE 0 END AS HasOuterApply,
    CASE WHEN j.CustomQuery LIKE N'%--!--mainsection%' THEN 1 ELSE 0 END AS HasMainSectionMarker,
    CASE WHEN q.Name = N'vw_openRequestConfig' AND j.CustomQuery LIKE N'%IsDeleted] = 0%' THEN 1
         WHEN q.Name = N'vw_openOrderRequestPurchaseCompleted' AND j.CustomQuery LIKE N'%IsDeleted] = 1%' THEN 1
         WHEN q.Name NOT IN (N'vw_openRequestConfig', N'vw_openOrderRequestPurchaseCompleted') THEN NULL
         ELSE 0 END AS FilterMatchesHts,
    CASE WHEN j.CustomQuery LIKE N'%RequestedPersonelIds%' THEN 1 ELSE 0 END AS HasD8CurrentUserFilter,
    (SELECT COUNT(*) FROM system.RoleAccess ra WHERE ra.ActionAccessType = 3 AND ra.RowId = q.Id) AS RoleAccessRows
FROM system.SavedQuery q
OUTER APPLY OPENJSON(q.QueryJson) WITH (CustomQuery NVARCHAR(MAX) '$.CustomQuery') AS j
WHERE q.Type = 1 AND LOWER(q.EntityFullName) = @EntityLower
ORDER BY q.Id;

-----------------------------------------------------------------------------
PRINT N'=== [2] ردیف در برابر DISTINCT در برابر کل — جوین مطلوب (بدون fan-out) ===';
-----------------------------------------------------------------------------
;WITH Intended AS (
    SELECT
        t1.Id,
        t1.IsDeleted,
        t1.EngineeringAccept,
        t3.PurchaseResponsibleId
    FROM Sup.OpenOrderRequest AS t1
    LEFT JOIN Inv.Part AS t2 ON t1.PartId = t2.Id
    OUTER APPLY (
        SELECT TOP (1) bci.BuyCategoryId
        FROM Sup.BuyCategoryItem AS bci
        WHERE bci.PartId = t1.PartId
        ORDER BY bci.Id DESC
    ) AS t4
    LEFT JOIN Sup.BuyCategory AS t3 ON t3.Id = COALESCE(t4.BuyCategoryId, t2.BuyCategoryId)
),
InnerLegacy AS (
    SELECT t1.Id, t1.IsDeleted
    FROM Sup.OpenOrderRequest AS t1
    INNER JOIN Sup.BuyCategoryItem AS bci ON bci.PartId = t1.PartId
)
SELECT
    N'تمامی درخواست ها' AS ProfileTitle,
    N'openorderrequest_listinfo' AS ProfileName,
    N'(بدون WHERE)' AS FilterNote,
    (SELECT COUNT(*) FROM Intended) AS ProfileRowCount,
    (SELECT COUNT(DISTINCT Id) FROM Intended) AS DistinctIds,
    (SELECT COUNT(*) FROM Sup.OpenOrderRequest) AS TotalMatchingEntity,
    (SELECT COUNT(*) FROM InnerLegacy) AS LegacyInnerJoinRows,
    (SELECT COUNT(DISTINCT Id) FROM InnerLegacy) AS LegacyInnerJoinDistinct
UNION ALL
SELECT N'همه فعال', N'vw_OpenOrderRequestAllActive', N'IsDeleted=0',
    (SELECT COUNT(*) FROM Intended WHERE IsDeleted = 0),
    (SELECT COUNT(DISTINCT Id) FROM Intended WHERE IsDeleted = 0),
    (SELECT COUNT(*) FROM Sup.OpenOrderRequest WHERE IsDeleted = 0),
    (SELECT COUNT(*) FROM InnerLegacy WHERE IsDeleted = 0),
    (SELECT COUNT(DISTINCT Id) FROM InnerLegacy WHERE IsDeleted = 0)
UNION ALL
SELECT N'تنظیمات درخواست های باز', N'vw_openRequestConfig', N'IsDeleted=0 — بدون فیلتر واحد',
    (SELECT COUNT(*) FROM Intended WHERE IsDeleted = 0),
    (SELECT COUNT(DISTINCT Id) FROM Intended WHERE IsDeleted = 0),
    (SELECT COUNT(*) FROM Sup.OpenOrderRequest WHERE IsDeleted = 0),
    (SELECT COUNT(*) FROM InnerLegacy WHERE IsDeleted = 0),
    (SELECT COUNT(DISTINCT Id) FROM InnerLegacy WHERE IsDeleted = 0)
UNION ALL
SELECT N'پیشینه درخواست ها', N'vw_openOrderRequestPurchaseCompleted', N'IsDeleted=1',
    (SELECT COUNT(*) FROM Intended WHERE IsDeleted = 1),
    (SELECT COUNT(DISTINCT Id) FROM Intended WHERE IsDeleted = 1),
    (SELECT COUNT(*) FROM Sup.OpenOrderRequest WHERE IsDeleted = 1),
    (SELECT COUNT(*) FROM InnerLegacy WHERE IsDeleted = 1),
    (SELECT COUNT(DISTINCT Id) FROM InnerLegacy WHERE IsDeleted = 1)
UNION ALL
SELECT N'منتظر تایید مهندسی', N'openorderrequest_listinfonew', N'IsDeleted=0 ∧ EngineeringAccept=0',
    (SELECT COUNT(*) FROM Intended WHERE IsDeleted = 0 AND EngineeringAccept = 0),
    (SELECT COUNT(DISTINCT Id) FROM Intended WHERE IsDeleted = 0 AND EngineeringAccept = 0),
    (SELECT COUNT(*) FROM Sup.OpenOrderRequest WHERE IsDeleted = 0 AND EngineeringAccept = 0),
    NULL, NULL
UNION ALL
SELECT N'تأیید مهندسی شده', N'vw_OpenOrderRequestEngineeringAccepted', N'IsDeleted=0 ∧ EngineeringAccept=1',
    (SELECT COUNT(*) FROM Intended WHERE IsDeleted = 0 AND EngineeringAccept = 1),
    (SELECT COUNT(DISTINCT Id) FROM Intended WHERE IsDeleted = 0 AND EngineeringAccept = 1),
    (SELECT COUNT(*) FROM Sup.OpenOrderRequest WHERE IsDeleted = 0 AND EngineeringAccept = 1),
    NULL, NULL
UNION ALL
SELECT N'درخواست های من', N'vw_OpenOrderRequestMyRequestes', N'IsDeleted=0 ∧ PurchaseResponsibleId IS NOT NULL (بدون @CurrentUserId)',
    (SELECT COUNT(*) FROM Intended WHERE IsDeleted = 0 AND PurchaseResponsibleId IS NOT NULL),
    (SELECT COUNT(DISTINCT Id) FROM Intended WHERE IsDeleted = 0 AND PurchaseResponsibleId IS NOT NULL),
    (SELECT COUNT(*) FROM Intended WHERE IsDeleted = 0 AND PurchaseResponsibleId IS NOT NULL),
    NULL, NULL
UNION ALL
SELECT N'مشاهده عمومی', N'vw_OpenOrderRequestOtherView', N'IsDeleted=0 ∧ EngineeringAccept=1 ∧ حداقل یک Id ذینفع (بدون @CurrentUserId)',
    (SELECT COUNT(*) FROM Intended i
      WHERE i.IsDeleted = 0 AND i.EngineeringAccept = 1
        AND EXISTS (
            SELECT 1 FROM Sup.OpenOrderRequest o
            WHERE o.Id = i.Id
              AND (
                  (ISJSON(o.RequestedPersonelIds) = 1 AND EXISTS (SELECT 1 FROM OPENJSON(o.RequestedPersonelIds) j WHERE TRY_CAST(j.value AS bigint) IS NOT NULL))
                  OR (ISJSON(o.RequestedEngineeringPersonelIds) = 1 AND EXISTS (SELECT 1 FROM OPENJSON(o.RequestedEngineeringPersonelIds) j WHERE TRY_CAST(j.value AS bigint) IS NOT NULL))
              )
        )),
    (SELECT COUNT(DISTINCT i.Id) FROM Intended i
      WHERE i.IsDeleted = 0 AND i.EngineeringAccept = 1
        AND EXISTS (
            SELECT 1 FROM Sup.OpenOrderRequest o
            WHERE o.Id = i.Id
              AND (
                  (ISJSON(o.RequestedPersonelIds) = 1 AND EXISTS (SELECT 1 FROM OPENJSON(o.RequestedPersonelIds) j WHERE TRY_CAST(j.value AS bigint) IS NOT NULL))
                  OR (ISJSON(o.RequestedEngineeringPersonelIds) = 1 AND EXISTS (SELECT 1 FROM OPENJSON(o.RequestedEngineeringPersonelIds) j WHERE TRY_CAST(j.value AS bigint) IS NOT NULL))
              )
        )),
    (SELECT COUNT(*) FROM Sup.OpenOrderRequest o
      WHERE o.IsDeleted = 0 AND o.EngineeringAccept = 1
        AND (
            (ISJSON(o.RequestedPersonelIds) = 1 AND EXISTS (SELECT 1 FROM OPENJSON(o.RequestedPersonelIds) j WHERE TRY_CAST(j.value AS bigint) IS NOT NULL))
            OR (ISJSON(o.RequestedEngineeringPersonelIds) = 1 AND EXISTS (SELECT 1 FROM OPENJSON(o.RequestedEngineeringPersonelIds) j WHERE TRY_CAST(j.value AS bigint) IS NOT NULL))
        )),
    NULL, NULL;

-----------------------------------------------------------------------------
PRINT N'=== [3] فعال‌هایی که در هیچ نمایهٔ غیرپیشینه نیستند ===';
-----------------------------------------------------------------------------
;WITH Intended AS (
    SELECT t1.Id, t1.IsDeleted
    FROM Sup.OpenOrderRequest AS t1
    LEFT JOIN Inv.Part AS t2 ON t1.PartId = t2.Id
    OUTER APPLY (
        SELECT TOP (1) bci.BuyCategoryId
        FROM Sup.BuyCategoryItem AS bci
        WHERE bci.PartId = t1.PartId
        ORDER BY bci.Id DESC
    ) AS t4
    LEFT JOIN Sup.BuyCategory AS t3 ON t3.Id = COALESCE(t4.BuyCategoryId, t2.BuyCategoryId)
)
SELECT
    (SELECT COUNT(*) FROM Sup.OpenOrderRequest WHERE IsDeleted = 0) AS ActiveTotal,
    (SELECT COUNT(*) FROM Intended WHERE IsDeleted = 0) AS ActiveInNonHistoryIntended,
    (SELECT COUNT(*) FROM Sup.OpenOrderRequest o
      WHERE o.IsDeleted = 0
        AND NOT EXISTS (SELECT 1 FROM Intended i WHERE i.Id = o.Id AND i.IsDeleted = 0)
    ) AS ActiveMissingFromNonHistory,
    (SELECT COUNT(*) FROM Sup.OpenOrderRequest WHERE IsDeleted = 1) AS HistoryTotal,
    (SELECT COUNT(*) FROM Intended WHERE IsDeleted = 1) AS HistoryInIntended,
    (SELECT COUNT(*) FROM Sup.OpenOrderRequest o
      WHERE o.IsDeleted = 1
        AND NOT EXISTS (SELECT 1 FROM Intended i WHERE i.Id = o.Id AND i.IsDeleted = 1)
    ) AS DeletedMissingFromHistory;

-- فهرست نمونهٔ فعال‌های غایب (با INNER JOIN قدیمی — D34)
SELECT TOP (50)
    o.Id,
    o.PurchaseRequestNumber,
    o.PartId,
    o.IsDeleted,
    o.EngineeringAccept,
    N'فعال بدون BuyCategoryItem — با INNER JOIN نامرئی است' AS Note
FROM Sup.OpenOrderRequest AS o
WHERE o.IsDeleted = 0
  AND NOT EXISTS (SELECT 1 FROM Sup.BuyCategoryItem AS bci WHERE bci.PartId = o.PartId)
ORDER BY o.Id DESC;

-----------------------------------------------------------------------------
PRINT N'=== [4] تکرار Id (fan-out) ===';
-----------------------------------------------------------------------------
;WITH IntendedDup AS (
    SELECT t1.Id
    FROM Sup.OpenOrderRequest AS t1
    LEFT JOIN Inv.Part AS t2 ON t1.PartId = t2.Id
    OUTER APPLY (
        SELECT TOP (1) bci.BuyCategoryId
        FROM Sup.BuyCategoryItem AS bci
        WHERE bci.PartId = t1.PartId
        ORDER BY bci.Id DESC
    ) AS t4
    LEFT JOIN Sup.BuyCategory AS t3 ON t3.Id = COALESCE(t4.BuyCategoryId, t2.BuyCategoryId)
),
InnerDup AS (
    SELECT t1.Id
    FROM Sup.OpenOrderRequest AS t1
    INNER JOIN Sup.BuyCategoryItem AS bci ON bci.PartId = t1.PartId
)
SELECT N'جوین مطلوب LEFT/APPLY' AS JoinKind, d.Id, d.Cnt
FROM (SELECT Id, COUNT(*) AS Cnt FROM IntendedDup GROUP BY Id HAVING COUNT(*) > 1) AS d
UNION ALL
SELECT N'INNER JOIN BuyCategoryItem (قدیمی)', d.Id, d.Cnt
FROM (SELECT Id, COUNT(*) AS Cnt FROM InnerDup GROUP BY Id HAVING COUNT(*) > 1) AS d
ORDER BY JoinKind, Cnt DESC, Id;

;WITH IntendedDup AS (
    SELECT t1.Id
    FROM Sup.OpenOrderRequest AS t1
    LEFT JOIN Inv.Part AS t2 ON t1.PartId = t2.Id
    OUTER APPLY (
        SELECT TOP (1) bci.BuyCategoryId
        FROM Sup.BuyCategoryItem AS bci
        WHERE bci.PartId = t1.PartId
        ORDER BY bci.Id DESC
    ) AS t4
    LEFT JOIN Sup.BuyCategory AS t3 ON t3.Id = COALESCE(t4.BuyCategoryId, t2.BuyCategoryId)
),
InnerDup AS (
    SELECT t1.Id
    FROM Sup.OpenOrderRequest AS t1
    INNER JOIN Sup.BuyCategoryItem AS bci ON bci.PartId = t1.PartId
)
SELECT
    (SELECT COUNT(*) FROM (SELECT Id FROM IntendedDup GROUP BY Id HAVING COUNT(*) > 1) x) AS IntendedDuplicateIdCount,
    (SELECT COUNT(*) FROM (SELECT Id FROM InnerDup GROUP BY Id HAVING COUNT(*) > 1) x) AS LegacyInnerDuplicateIdCount;

-----------------------------------------------------------------------------
PRINT N'=== [5] RoleAccess — انتظار در برابر واقعیت ===';
-----------------------------------------------------------------------------
DECLARE @Expected TABLE (ProfileName NVARCHAR(100) NOT NULL, RoleName NVARCHAR(200) NOT NULL, PRIMARY KEY (ProfileName, RoleName));
INSERT INTO @Expected (ProfileName, RoleName) VALUES
    (N'openorderrequest_listinfo',              N'ShowAllMenus'),
    (N'openorderrequest_listinfo',              N'SupplyAndPurchase'),
    (N'vw_OpenOrderRequestAllActive',           N'Sup.OpenOrderRequest.ShowAll'),
    (N'vw_OpenOrderRequestAllActive',           N'Sup.OpenOrderRequest.Industrial'),
    (N'vw_OpenOrderRequestAllActive',           N'Industries'),
    (N'vw_OpenOrderRequestAllActive',           N'Sup.OpenOrderRequest.EngineeringAccept'),
    (N'vw_OpenOrderRequestAllActive',           N'Sup.OpenOrderRequest.HasEngineering'),
    (N'vw_OpenOrderRequestAllActive',           N'SupplyAndPurchase'),
    (N'vw_OpenOrderRequestAllActive',           N'ShowAllMenus'),
    (N'openorderrequest_listinfonew',           N'Sup.OpenOrderRequest.EngineeringAccept'),
    (N'openorderrequest_listinfonew',           N'Sup.OpenOrderRequest.Industrial'),
    (N'openorderrequest_listinfonew',           N'Industries'),
    (N'openorderrequest_listinfonew',           N'SupplyAndPurchase'),
    (N'openorderrequest_listinfonew',           N'ShowAllMenus'),
    (N'vw_OpenOrderRequestEngineeringAccepted', N'Sup.OpenOrderRequest.SalesOrProjectAccept'),
    (N'vw_OpenOrderRequestEngineeringAccepted', N'Sup.OpenOrderRequest.Supply'),
    (N'vw_OpenOrderRequestEngineeringAccepted', N'SupplyAndPurchase'),
    (N'vw_OpenOrderRequestEngineeringAccepted', N'ShowAllMenus'),
    (N'vw_OpenOrderRequestMyRequestes',         N'Sup.OpenOrderRequest.Supply'),
    (N'vw_OpenOrderRequestMyRequestes',         N'SupplyAndPurchase'),
    (N'vw_OpenOrderRequestMyRequestes',         N'ShowAllMenus'),
    (N'vw_OpenOrderRequestOtherView',           N'Sup.OpenOrderRequest.Stop'),
    (N'vw_OpenOrderRequestOtherView',           N'Sup.OpenOrderRequest.View'),
    (N'vw_OpenOrderRequestOtherView',           N'SupplyAndPurchase'),
    (N'vw_OpenOrderRequestOtherView',           N'ShowAllMenus'),
    (N'vw_openRequestConfig',                   N'Sup.OpenOrderRequest.ConfigManage'),
    (N'vw_openRequestConfig',                   N'Sup.OpenOrderRequest.Industrial'),
    (N'vw_openRequestConfig',                   N'Industries'),
    (N'vw_openRequestConfig',                   N'SupplyAndPurchase'),
    (N'vw_openRequestConfig',                   N'ShowAllMenus'),
    (N'vw_openOrderRequestPurchaseCompleted',   N'Sup.OpenOrderRequest.ShowAll'),
    (N'vw_openOrderRequestPurchaseCompleted',   N'Sup.OpenOrderRequest.Industrial'),
    (N'vw_openOrderRequestPurchaseCompleted',   N'Industries'),
    (N'vw_openOrderRequestPurchaseCompleted',   N'Sup.OpenOrderRequest.EngineeringAccept'),
    (N'vw_openOrderRequestPurchaseCompleted',   N'Sup.OpenOrderRequest.Supply'),
    (N'vw_openOrderRequestPurchaseCompleted',   N'Sup.OpenOrderRequest.HistoryView'),
    (N'vw_openOrderRequestPurchaseCompleted',   N'SupplyAndPurchase'),
    (N'vw_openOrderRequestPurchaseCompleted',   N'ShowAllMenus');

SELECT
    e.ProfileName,
    e.RoleName,
    CASE WHEN r.Id IS NULL THEN N'نقش غایب (WP4)'
         WHEN ra.Id IS NULL THEN N'RoleAccess غایب'
         ELSE N'هست' END AS Status,
    q.Id AS SavedQueryId,
    ra.Path
FROM @Expected e
LEFT JOIN system.Role r ON r.Name = e.RoleName
LEFT JOIN system.SavedQuery q ON q.Name = e.ProfileName AND q.Type = 1 AND LOWER(q.EntityFullName) = @EntityLower
LEFT JOIN system.RoleAccess ra ON ra.ActionAccessType = 3 AND ra.RowId = q.Id AND ra.RoleId = r.Id
ORDER BY e.ProfileName, e.RoleName;

SELECT q.Name AS ProfileName, r.Name AS ExtraRoleName, ra.Path
FROM system.RoleAccess ra
INNER JOIN system.SavedQuery q ON q.Id = ra.RowId
LEFT JOIN system.Role r ON r.Id = ra.RoleId
WHERE ra.ActionAccessType = 3
  AND LOWER(q.EntityFullName) = @EntityLower
  AND NOT EXISTS (
        SELECT 1 FROM @Expected e
        WHERE e.ProfileName = q.Name AND e.RoleName = r.Name)
ORDER BY q.Name, r.Name;

-----------------------------------------------------------------------------
PRINT N'=== [6] بهداشت JSON / رنگ / ActionOptions / نمایهٔ ناشناخته ===';
-----------------------------------------------------------------------------
SELECT q.Name, q.Title, q.Id
FROM system.SavedQuery q
WHERE q.Type = 1 AND q.IsActive = 1
  AND LOWER(q.EntityFullName) = @EntityLower
  AND q.Name NOT IN (
        N'openorderrequest_listinfo', N'vw_OpenOrderRequestAllActive', N'openorderrequest_listinfonew',
        N'vw_OpenOrderRequestEngineeringAccepted', N'vw_OpenOrderRequestMyRequestes', N'vw_OpenOrderRequestOtherView',
        N'vw_openRequestConfig', N'vw_openOrderRequestPurchaseCompleted');

SELECT
    q.Name,
    CASE WHEN q.EventScriptsJson LIKE N'%orangered%' THEN 1 ELSE 0 END AS HasStopColor,
    CASE WHEN q.EventScriptsJson LIKE N'%violet%' THEN 1 ELSE 0 END AS HasChangedColor,
    CASE WHEN q.Name IN (N'vw_openRequestConfig', N'vw_openOrderRequestPurchaseCompleted') THEN NULL
         WHEN q.EventScriptsJson LIKE N'%deepskyblue%' THEN 1 ELSE 0 END AS HasSupplyWindowColor,
    CASE WHEN q.Name IN (N'vw_openRequestConfig', N'vw_openOrderRequestPurchaseCompleted') THEN NULL
         WHEN q.EventScriptsJson LIKE N'%orange%' THEN 1 ELSE 0 END AS HasSalesConfirmColor,
    CASE WHEN q.EventScriptsJson LIKE N'%TODO WP1%' THEN 1 ELSE 0 END AS HasEmailTodo,
    CASE WHEN q.ActionOptions LIKE N'%"dataActionName":"delete","enable":false%'
           OR q.ActionOptions LIKE N'%"dataActionName":"delete","enable":false,%' THEN 1 ELSE 0 END AS DeleteDisabled,
    CASE WHEN q.ActionOptions LIKE N'%"dataActionName":"new","enable":false%' THEN 1 ELSE 0 END AS NewDisabled
FROM system.SavedQuery q
WHERE q.Type = 1 AND LOWER(q.EntityFullName) = @EntityLower
ORDER BY q.Name;

-- ستون‌های صفحه 74 که باید در ColumnsJson باشند
SELECT q.Name, req.Alliance, CASE WHEN c.Alliance IS NULL THEN N'غایب' ELSE N'هست' END AS Status
FROM system.SavedQuery q
CROSS JOIN (VALUES
    (N't1_Id'), (N'tAtt_HaveAttachment'), (N'tCalc_PurchaseCompleted'),
    (N't1_EngineeringAccept'), (N't1_HasSalesUnitConfirmation'),
    (N't5_NameFa'), (N't3_Title'), (N't1_PurchaseRequestNumber'),
    (N't12_FullName'), (N't2_Code'), (N't2_Name'), (N't6_Title'),
    (N'tCmtSup_CommentValue'), (N't15_Title'), (N'tCmtInd_CommentValue'),
    (N'tCmtOth_CommentValue'), (N'tCo_CompanyNames'),
    (N'tEml_EmailSendToSupplier'), (N't1_RequestedEngineeringPersonel'),
    (N't13_Code'), (N't13_Name'), (N't1_DelaysBuyDay'),
    (N't1_CompletionShamsiDate'), (N'tCalc_BuyProgress'),
    (N't1_Changed'), (N't1_IsStop'), (N'tClr_InSupplyWindow')
) AS req (Alliance)
LEFT JOIN system.SavedQuery q2 ON q2.Id = q.Id
OUTER APPLY (
    SELECT j.Alliance
    FROM OPENJSON(q.ColumnsJson)
    WITH (Alliance NVARCHAR(100) '$.Alliance') AS j
    WHERE j.Alliance = req.Alliance
) AS c
WHERE q.Type = 1 AND LOWER(q.EntityFullName) = @EntityLower
  AND q.Name IN (
        N'openorderrequest_listinfo', N'vw_OpenOrderRequestAllActive', N'openorderrequest_listinfonew',
        N'vw_OpenOrderRequestEngineeringAccepted', N'vw_OpenOrderRequestMyRequestes', N'vw_OpenOrderRequestOtherView',
        N'vw_openRequestConfig', N'vw_openOrderRequestPurchaseCompleted')
ORDER BY q.Name, req.Alliance;
