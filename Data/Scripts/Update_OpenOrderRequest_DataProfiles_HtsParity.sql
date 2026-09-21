/*
  Update_OpenOrderRequest_DataProfiles_HtsParity.sql
  WP2 — هم‌ترازی نمایه‌های داده (Data Profile) لیست «درخواست های باز» با HTS (صفحات 74 / 76 / 77)

  چه می‌کند (idempotent — UPDATE بر اساس Name، INSERT اگر نبود؛ هیچ DELETE روی SavedQuery):
    a. کوئری مشترک هشت نمایه Sup.OpenOrderRequest بازنویسی می‌شود:
         - INNER JOIN Sup.BuyCategoryItem  →  OUTER APPLY TOP(1) (بدون fan-out؛ ۱۴۷ درخواست نامرئی و ۳۹ تکراری رفع می‌شود)
         - دسته خرید = COALESCE(آخرین BuyCategoryItem کالا, Inv.Part.BuyCategoryId)  (مثل Vw_Sup_OpenOrderRequest که LEFT می‌گرفت)
         - Pln.LeadTime قبلاً به‌اشتباه روی LeadTime.Id = PartId جوین شده بود → OUTER APPLY TOP(1) روی LeadTime.PartId
         - ستون‌های غایب HTS اضافه شد: حاوی پیوست، خرید انجام شده، تامین کننده منتخب، دپارتمان فروش، تاریخ تایید سفارش،
           توضیحات تدارکات/صنایع/کامنت آخر (بازسازی با نقش نویسنده کامنت)، نام شرکت ها، شرکت دارد؟، درخواست کنندگان (مهندسی)،
           کد/عنوان محصول مرتبط، کامنت پروژه/فروش، تاخیر خرید، تاریخ تکمیل، درصد پیشرفت خرید، تاریخ حذف/اختتام
         - Alias ستون‌های قبلی (t1_… t10_…) حفظ شده تا ColumnsJson/فیلترهای ذخیره‌شده کاربران معتبر بمانند.
    b. نمایه‌های اصلی = گرید صفحه 74 HTS (ترتیب و عنوان فارسی ستون‌ها از CaptionsLibrary.fa-IR.resx)
    c. vw_openRequestConfig (تنظیمات) = صفحه 76: IsDeleted = 0 بدون فیلتر کارتابل؛ ستون‌های صفحه 76؛ دکمه چندانتخابی پرسنل اصلاح شد
    d. vw_openOrderRequestPurchaseCompleted (پیشینه) = صفحه 77: IsDeleted = 1 (بررسی شد: فیلتر فعلی هم IsDeleted=1 بود و درست است)؛
       فقط‌خواندنی (delete غیرفعال)، دکمه‌ها محدود به مشاهده (لیست کامنت‌ها؛ پیوست از صفحه Edit)
    e. EventScriptsJson.onRowAdded با قواعد رنگ HTS (تایید فروش، پنجره تامین، Changed، Stop) — قاعده «ایمیل پیمانکار» با TODO WP1 کامنت است
    f. RoleAccess نمایه‌ها بر اساس ماتریس دید HTS (GetOpenOrderRequestGridData + گرنت‌های گروه‌ها روی صفحات 74/76/77)
    g. Hygiene: نمایه‌های ناشناخته/جایگزین‌شده روی همین موجودیت فقط گزارش می‌شوند (و با @DeactivateUnknownProfiles=1 غیرفعال — هرگز DELETE)
    h. D8: «مشاهده عمومی» به کاربر جاری محدود است (OPENJSON روی RequestedPersonelIds / RequestedEngineeringPersonelIds = @CurrentUserId)

  اجرا:
    - ابتدا با @DryRun = 1 (پیش‌فرض): همه تغییرات داخل تراکنش اعمال، گزارش «چه چیزی تغییر می‌کند» چاپ و در پایان ROLLBACK می‌شود.
    - سپس @DryRun = 0 برای COMMIT.
    - قبل/بعد: Data\Scripts\Verify_OpenOrderRequest_DataProfiles.sql
    - بعد از COMMIT: ری‌استارت WebApp یا پاک‌کردن Redis DataTableProfileCacheDb (نمایه‌ها کش می‌شوند).

  وابستگی‌ها / TODO WP1:
    - جدول Sup.OpenOrderRequestEmailToSupplier (موجودیت OpenOrderRequestEmailToSupplier؛ FK OpenOrderRequestId؛ SupplierId؛ SentDate؛ CreatedById)
      توسط WP1 ساخته می‌شود. در این اسکریپت فقط در بلوک‌های کامنت‌شده «TODO WP1» به آن ارجاع شده؛ ستون‌های
      tEml_EmailSendToSupplier / tEml_CompanyNameEmailSended امروز placeholder (0 / NULL) هستند تا ColumnsJson بعداً تغییر نکند.
    - نقش‌های Sup.OpenOrderRequest.View و Sup.OpenOrderRequest.HistoryView (WP4 / D39) اگر وجود نداشته باشند، ردیف RoleAccess آن‌ها
      با هشدار رد می‌شود؛ اجرای مجدد اسکریپت بعد از WP4 آن‌ها را اضافه می‌کند.

  ستون‌های HTS بدون معادل (در ColumnsJson نیستند — جزئیات در Specs\SuppliesOpenOrder\99-Discrepancies.md بلوک WP2):
    - PartCodingTitle «عنوان کدینگ»: Inv.Part در سیستم جدید ستون کدینگ ندارد (فقط Code/Number/Type).
    - Engineering_Accept_Time «ساعت تایید مهندسی»: در t1_EngineeringAcceptShamsiDateTime (تاریخ و ساعت) ادغام شده است.
    - Acc_DL_FK: HTS شناسه عددی تفصیل را نشان می‌داد؛ اینجا FIN.DL.Code (کد تفصیل) نمایش داده می‌شود.
    - فیلتر part-permission (PartPermissionService در HTS): جدولی معادل در سیستم جدید نیست.
    - شاخه کاربر «شرکت/تامین‌کننده» (CompanyId) و ستون‌های پنهان برای آن: سیستم جدید لاگین تامین‌کننده ندارد.
    - EmailSendToSupplier / CompanyName_EmailSended: placeholder تا WP1.
    - SuppComment / IndustrialComment / OtherComment: HTS با CreatedOrgUnit_FK (51/12) تفکیک می‌کرد؛ کامنت جدید واحد سازمانی ندارد
      → نویسنده با نقش Sup.OpenOrderRequest.Supply = تدارکات، با نقش Sup.OpenOrderRequest.Industrial (و بدون Supply) = صنایع، بقیه = سایر.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @DryRun BIT = 1;                       -- 1 = فقط گزارش (ROLLBACK در پایان) · 0 = اعمال (COMMIT)
DECLARE @DeactivateUnknownProfiles BIT = 0;    -- 1 = نمایه‌های فعال ناشناخته روی entities.app.sup.openorderrequest غیرفعال شوند (IsActive=0)

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-wp2-oor-dataprofiles-htsparity';
DECLARE @EntityFullName NVARCHAR(200) = N'entities.app.sup.openorderrequest';
DECLARE @EntityFullNamePascal NVARCHAR(200) = N'Entities.App.Sup.OpenOrderRequest';

PRINT N'=== Update_OpenOrderRequest_DataProfiles_HtsParity — DryRun=' + CAST(@DryRun AS NVARCHAR(1)) + N' ===';

BEGIN TRANSACTION;
BEGIN TRY

    ---------------------------------------------------------------------------
    PRINT N'=== [1/7] کوئری مشترک (LEFT JOIN / OUTER APPLY — بدون fan-out) ===';
    ---------------------------------------------------------------------------
    -- بخش آماده‌سازی قبل از نشانگر --!--mainsection (الگوی Seed_ProductionOrderItem_MyCartable_DataProfile.sql):
    -- شناسه نقش‌ها با Name حل می‌شود (نه literal). ParameterResolverService متغیرهای DECLARE شده را پارامتر نمی‌کند.
    DECLARE @Setup NVARCHAR(MAX) = CAST(N'' AS NVARCHAR(MAX)) + N'DECLARE @RoleSupplyId BIGINT = (SELECT TOP (1) [Id] FROM [system].[Role] WHERE [Name] = N''Sup.OpenOrderRequest.Supply'');
DECLARE @RoleIndustrialId BIGINT = (SELECT TOP (1) [Id] FROM [system].[Role] WHERE [Name] = N''Sup.OpenOrderRequest.Industrial'');
DECLARE @TodayDate DATE = CAST(GETDATE() AS DATE);
--!--mainsection
';

    DECLARE @SelectBody NVARCHAR(MAX) = CAST(N'' AS NVARCHAR(MAX)) + N'SELECT
    [t1].[Id] AS [t1_Id],
    [tAtt].[HaveAttachment] AS [tAtt_HaveAttachment],
    CAST(CASE WHEN [t1].[FactoredCount] = [t1].[RequiredQty] THEN 1 ELSE 0 END AS bit) AS [tCalc_PurchaseCompleted],
    [t1].[EngineeringAccept] AS [t1_EngineeringAccept],
    [t1].[HasSalesUnitConfirmation] AS [t1_HasSalesUnitConfirmation],
    [t5].[NameFa] AS [t5_NameFa],
    [t3].[Title] AS [t3_Title],
    [t1].[PurchaseRequestNumber] AS [t1_PurchaseRequestNumber],
    [t1].[PurchaseRequestShamsiDate] AS [t1_PurchaseRequestShamsiDate],
    [t1].[OrderNo] AS [t1_OrderNo],
    [t12].[FullName] AS [t12_FullName],
    [t2].[Code] AS [t2_Code],
    [t2].[Name] AS [t2_Name],
    [t6].[Title] AS [t6_Title],
    [t1].[RequiredQty] AS [t1_RequiredQty],
    [t1].[OrderQty] AS [t1_OrderQty],
    [t1].[SumSendQty] AS [t1_SumSendQty],
    [t1].[FactoredCount] AS [t1_FactoredCount],
    [t1].[OrderItemComment] AS [t1_OrderItemComment],
    [t1].[OrderShamsiDate] AS [t1_OrderShamsiDate],
    [t1].[NeedDateShamsiDate] AS [t1_NeedDateShamsiDate],
    [t7].[LeadTimeDay] AS [t7_LeadTimeDay],
    [t1].[SupplyShamsiDate] AS [t1_SupplyShamsiDate],
    [tCmtSup].[CommentShamsiDate] AS [tCmtSup_ShamsiDate],
    [tCmtSup].[CommentValue] AS [tCmtSup_CommentValue],
    [t1].[ProductionOrderNumber] AS [t1_ProductionOrderNumber],
    [t15].[Title] AS [t15_Title],
    [t8].[Code] AS [t8_Code],
    [t8].[Title] AS [t8_Title],
    [t1].[OrderConfirmShamsiDate] AS [t1_OrderConfirmShamsiDate],
    [tCmtInd].[CommentShamsiDate] AS [tCmtInd_ShamsiDate],
    [tCmtInd].[CommentValue] AS [tCmtInd_CommentValue],
    [tCmtOth].[CommentShamsiDate] AS [tCmtOth_ShamsiDate],
    [tCmtOth].[CommentValue] AS [tCmtOth_CommentValue],
    [tCo].[CompanyNames] AS [tCo_CompanyNames],
    [tCo].[IsHasCompany] AS [tCo_IsHasCompany],
    [tEml].[EmailSendToSupplier] AS [tEml_EmailSendToSupplier],
    [tEml].[CompanyNameEmailSended] AS [tEml_CompanyNameEmailSended],
    [t1].[RequestedPersonel] AS [t1_RequestedPersonel],
    [t1].[RequestedEngineeringPersonel] AS [t1_RequestedEngineeringPersonel],
    [t1].[Status] AS [t1_Status],
    [t13].[Code] AS [t13_Code],
    [t13].[Name] AS [t13_Name],
    [t1].[IsRejectedByInspection] AS [t1_IsRejectedByInspection],
    [t1].[IsStop] AS [t1_IsStop],
    [t1].[StopShamsiDate] AS [t1_StopShamsiDate],
    [t1].[Changed] AS [t1_Changed],
    [t9].[NameFa] AS [t9_NameFa],
    [t1].[EngineeringAcceptShamsiDateTime] AS [t1_EngineeringAcceptShamsiDateTime],
    [t10].[NameFa] AS [t10_NameFa],
    [t1].[SalesUnitConfirmationShamsiDateTime] AS [t1_SalesUnitConfirmationShamsiDateTime],
    [t1].[SalesUnitConfirmationComment] AS [t1_SalesUnitConfirmationComment],
    [t1].[DeliveryVoucherShamsiDate] AS [t1_DeliveryVoucherShamsiDate],
    [t1].[TemporaryInventoryVoucherShamsiDate] AS [t1_TemporaryInventoryVoucherShamsiDate],
    [t1].[FinalInventoryVoucherShamsiDate] AS [t1_FinalInventoryVoucherShamsiDate],
    [t1].[QcVoucherShamsiDate] AS [t1_QcVoucherShamsiDate],
    [t1].[DelaysBuyDay] AS [t1_DelaysBuyDay],
    [t1].[CompletionShamsiDate] AS [t1_CompletionShamsiDate],
    CAST(CASE WHEN ISNULL([t1].[OrderQty], 0) = 0 THEN 0 ELSE ROUND(100.0 * [t1].[FactoredCount] / [t1].[OrderQty], 0) END AS int) AS [tCalc_BuyProgress],
    [t1].[StopStatus] AS [t1_StopStatus],
    [t1].[Comment] AS [t1_Comment],
    [t1].[IsDeletedShamsiDate] AS [t1_IsDeletedShamsiDate],
    [t1].[IsForceDeletedByUser] AS [t1_IsForceDeletedByUser],
    CAST(CASE WHEN [t1].[SupplyMiladiDate] IS NOT NULL
               AND @TodayDate BETWEEN DATEADD(DAY, -3, CAST([t1].[SupplyMiladiDate] AS date)) AND CAST([t1].[SupplyMiladiDate] AS date)
              THEN 1 ELSE 0 END AS bit) AS [tClr_InSupplyWindow]
FROM [Sup].[OpenOrderRequest] AS [t1]
LEFT JOIN [Inv].[Part] AS [t2]
    ON [t1].[PartId] = [t2].[Id]
OUTER APPLY (
    SELECT TOP (1) [bci].[BuyCategoryId]
    FROM [Sup].[BuyCategoryItem] AS [bci]
    WHERE [bci].[PartId] = [t1].[PartId]
    ORDER BY [bci].[Id] DESC
) AS [t4]
LEFT JOIN [Sup].[BuyCategory] AS [t3]
    ON [t3].[Id] = COALESCE([t4].[BuyCategoryId], [t2].[BuyCategoryId])
LEFT JOIN [system].[User] AS [t5]
    ON [t3].[PurchaseResponsibleId] = [t5].[Id]
LEFT JOIN [Inv].[PartUnit] AS [t6]
    ON [t2].[UnitId] = [t6].[Id]
OUTER APPLY (
    SELECT TOP (1) [lt].[LeadTimeDay]
    FROM [Pln].[LeadTime] AS [lt]
    WHERE [lt].[PartId] = [t1].[PartId]
    ORDER BY [lt].[Id] DESC
) AS [t7]
LEFT JOIN [FIN].[DL] AS [t8]
    ON [t1].[DlId] = [t8].[Id]
LEFT JOIN [system].[User] AS [t9]
    ON [t1].[EngineeringAcceptUserId] = [t9].[Id]
LEFT JOIN [system].[User] AS [t10]
    ON [t1].[SalesUnitConfirmationUserId] = [t10].[Id]
LEFT JOIN [Gnr].[Supplier] AS [t11]
    ON [t1].[ManCompanyId] = [t11].[Id]
LEFT JOIN [Gnr].[Party] AS [t12]
    ON [t11].[PartyId] = [t12].[Id]
LEFT JOIN [Inv].[Part] AS [t13]
    ON [t1].[RelatedPartId] = [t13].[Id]
LEFT JOIN [Sale].[ProductionOrder] AS [t14]
    ON [t1].[ProductionOrderId] = [t14].[Id]
LEFT JOIN [Sale].[Branch] AS [t15]
    ON [t14].[BranchId] = [t15].[Id]
CROSS APPLY (
    SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM [Sup].[OpenOrderRequestAttachment] AS [a] WHERE [a].[OpenOrderRequestId] = [t1].[Id])
                       OR EXISTS (SELECT 1 FROM [Sup].[OpenOrderRequestVpis] AS [v] WHERE [v].[OpenOrderRequestId] = [t1].[Id])
                  THEN 1 ELSE 0 END AS bit) AS [HaveAttachment]
) AS [tAtt]
OUTER APPLY (
    SELECT TOP (1) [c].[CommentValue], COALESCE([c].[ShamsiDate], LEFT([c].[CreatedOnShamsiDateTime], 10)) AS [CommentShamsiDate]
    FROM [Sup].[OpenOrderRequestComment] AS [c]
    INNER JOIN [system].[User] AS [cu] ON [cu].[Id] = [c].[CreatedById]
    WHERE [c].[OpenOrderRequestId] = [t1].[Id]
      AND EXISTS (SELECT 1 FROM OPENJSON(CASE WHEN ISJSON([cu].[RoleIds]) = 1 THEN [cu].[RoleIds] ELSE N''[]'' END) AS [j] WHERE TRY_CAST([j].[value] AS bigint) = @RoleSupplyId)
    ORDER BY [c].[Id] DESC
) AS [tCmtSup]
OUTER APPLY (
    SELECT TOP (1) [c].[CommentValue], COALESCE([c].[ShamsiDate], LEFT([c].[CreatedOnShamsiDateTime], 10)) AS [CommentShamsiDate]
    FROM [Sup].[OpenOrderRequestComment] AS [c]
    INNER JOIN [system].[User] AS [cu] ON [cu].[Id] = [c].[CreatedById]
    WHERE [c].[OpenOrderRequestId] = [t1].[Id]
      AND EXISTS (SELECT 1 FROM OPENJSON(CASE WHEN ISJSON([cu].[RoleIds]) = 1 THEN [cu].[RoleIds] ELSE N''[]'' END) AS [j] WHERE TRY_CAST([j].[value] AS bigint) = @RoleIndustrialId)
      AND NOT EXISTS (SELECT 1 FROM OPENJSON(CASE WHEN ISJSON([cu].[RoleIds]) = 1 THEN [cu].[RoleIds] ELSE N''[]'' END) AS [j] WHERE TRY_CAST([j].[value] AS bigint) = @RoleSupplyId)
    ORDER BY [c].[Id] DESC
) AS [tCmtInd]
OUTER APPLY (
    SELECT TOP (1) [c].[CommentValue], COALESCE([c].[ShamsiDate], LEFT([c].[CreatedOnShamsiDateTime], 10)) AS [CommentShamsiDate]
    FROM [Sup].[OpenOrderRequestComment] AS [c]
    LEFT JOIN [system].[User] AS [cu] ON [cu].[Id] = [c].[CreatedById]
    WHERE [c].[OpenOrderRequestId] = [t1].[Id]
      AND NOT EXISTS (SELECT 1 FROM OPENJSON(CASE WHEN ISJSON([cu].[RoleIds]) = 1 THEN [cu].[RoleIds] ELSE N''[]'' END) AS [j] WHERE TRY_CAST([j].[value] AS bigint) IN (@RoleSupplyId, @RoleIndustrialId))
    ORDER BY [c].[Id] DESC
) AS [tCmtOth]
OUTER APPLY (
    SELECT STRING_AGG(CAST([p].[FullName] AS nvarchar(max)), N''، '') WITHIN GROUP (ORDER BY [p].[FullName]) AS [CompanyNames],
           CAST(CASE WHEN COUNT(*) > 0 THEN 1 ELSE 0 END AS bit) AS [IsHasCompany]
    FROM [Inv].[PartCompany] AS [pc]
    INNER JOIN [Gnr].[Supplier] AS [s] ON [s].[Id] = [pc].[SupplierId]
    LEFT JOIN [Gnr].[Party] AS [p] ON [p].[Id] = [s].[PartyId]
    WHERE [pc].[PartId] = [t1].[PartId]
) AS [tCo]
CROSS APPLY (
    SELECT CAST(0 AS bit) AS [EmailSendToSupplier],
           CAST(NULL AS nvarchar(500)) AS [CompanyNameEmailSended]
) AS [tEml]';

    /* -----------------------------------------------------------------------
       TODO WP1 — پس از ایجاد Sup.OpenOrderRequestEmailToSupplier (WP1):
         1) CROSS APPLY placeholder [tEml] را با این OUTER APPLY عوض کنید (آخرین ایمیل هر درخواست — مثل Vw_Sup_OpenOrderRequest):
            OUTER APPLY (
                SELECT TOP (1) CAST(1 AS bit) AS [EmailSendToSupplier], [ep].[FullName] AS [CompanyNameEmailSended]
                FROM [Sup].[OpenOrderRequestEmailToSupplier] AS [e]
                LEFT JOIN [Gnr].[Supplier] AS [es] ON [es].[Id] = [e].[SupplierId]
                LEFT JOIN [Gnr].[Party]    AS [ep] ON [ep].[Id] = [es].[PartyId]
                WHERE [e].[OpenOrderRequestId] = [t1].[Id]
                ORDER BY [e].[SentDate] DESC, [e].[Id] DESC
            ) AS [tEml]
         2) ستون‌های SELECT از قبل [tEml].[…] هستند — تغییر ندهید.
         3) در @OnRowAddedFull / @OnRowAddedBasic خط «TODO WP1» (رنگ lightgreen) را از کامنت خارج کنید.
       ----------------------------------------------------------------------- */

    ---------------------------------------------------------------------------
    PRINT N'=== [2/7] کاتالوگ ستون‌ها + چیدمان صفحات 74 / 76 / 77 ===';
    ---------------------------------------------------------------------------
    DECLARE @MoneyRender NVARCHAR(300) = N'function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }';
    DECLARE @EnumRender NVARCHAR(100)  = N'function(data, type, row) { return data; }';
    DECLARE @StopStatusOptionSetting NVARCHAR(400) = N'{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Sup.Enums.OpenOrderRequestStopStatusEnum"}';

    IF OBJECT_ID('tempdb..#ColDef') IS NOT NULL DROP TABLE #ColDef;
    CREATE TABLE #ColDef
    (
        Seq               INT            NOT NULL,
        Alliance          NVARCHAR(100)  NOT NULL PRIMARY KEY,
        TableName         NVARCHAR(100)  NOT NULL,
        ColumnName        NVARCHAR(100)  NOT NULL,
        Address           NVARCHAR(1000) NOT NULL,
        DisplayName       NVARCHAR(200)  NOT NULL,
        SystemType        INT            NOT NULL,   -- 0 String · 1 Boolean · 4 DateTimeShamsi · 5 DateShamsi · 6 Long · 7 Int · 8 Select
        SystemTypeName    NVARCHAR(30)   NOT NULL,
        Width             INT            NOT NULL,
        Render            NVARCHAR(400)  NULL,
        OptionSettingJson NVARCHAR(400)  NULL,
        IsPk              BIT            NOT NULL DEFAULT 0
    );

    -- ترتیب Seq = ترتیب گرید صفحه 74 HTS (ستون‌های افزوده در انتها)؛ DisplayName = عنوان HTS (CaptionsLibrary.fa-IR)
    INSERT INTO #ColDef (Seq, Alliance, TableName, ColumnName, Address, DisplayName, SystemType, SystemTypeName, Width, Render, OptionSettingJson, IsPk) VALUES
    ( 1, N't1_Id',                                  N'Sup.OpenOrderRequest',            N'Id',                                  N'[t1].[Id]',                                  N'شناسه',                        6, N'Long',           80, NULL, NULL, 1),
    ( 2, N'tAtt_HaveAttachment',                    N'Sup.OpenOrderRequestAttachment',  N'HaveAttachment',                      N'[tAtt].[HaveAttachment]',                    N'حاوی پیوست',                   1, N'Boolean',        90, NULL, NULL, 0),
    ( 3, N'tCalc_PurchaseCompleted',                N'Sup.OpenOrderRequest',            N'PurchaseCompleted',                   N'CAST(CASE WHEN [t1].[FactoredCount] = [t1].[RequiredQty] THEN 1 ELSE 0 END AS bit)', N'خرید انجام شده است', 1, N'Boolean', 110, NULL, NULL, 0),
    ( 4, N't1_EngineeringAccept',                   N'Sup.OpenOrderRequest',            N'EngineeringAccept',                   N'[t1].[EngineeringAccept]',                   N'تایید مهندسی',                 1, N'Boolean',       100, NULL, NULL, 0),
    ( 5, N't1_HasSalesUnitConfirmation',            N'Sup.OpenOrderRequest',            N'HasSalesUnitConfirmation',            N'[t1].[HasSalesUnitConfirmation]',            N'تایید پروژه/فروش',             1, N'Boolean',       110, NULL, NULL, 0),
    ( 6, N't5_NameFa',                              N'system.User',                     N'NameFa',                              N'[t5].[NameFa]',                              N'متولی خرید',                   0, N'String',        130, NULL, NULL, 0),
    ( 7, N't3_Title',                               N'Sup.BuyCategory',                 N'Title',                               N'[t3].[Title]',                               N'عنوان دسته خرید',              0, N'String',        150, NULL, NULL, 0),
    ( 8, N't1_PurchaseRequestNumber',               N'Sup.OpenOrderRequest',            N'PurchaseRequestNumber',               N'[t1].[PurchaseRequestNumber]',               N'شماره درخواست خرید',           6, N'Long',          120, NULL, NULL, 0),
    ( 9, N't1_PurchaseRequestShamsiDate',           N'Sup.OpenOrderRequest',            N'PurchaseRequestShamsiDate',           N'[t1].[PurchaseRequestShamsiDate]',           N'تاریخ درخواست خرید',           5, N'DateShamsi',    110, NULL, NULL, 0),
    (10, N't1_OrderNo',                             N'Sup.OpenOrderRequest',            N'OrderNo',                             N'[t1].[OrderNo]',                             N'شماره سفارش',                  6, N'Long',           90, NULL, NULL, 0),
    (11, N't12_FullName',                           N'Gnr.Party',                       N'FullName',                            N'[t12].[FullName]',                           N'تامین کننده منتخب',            0, N'String',        160, NULL, NULL, 0),
    (12, N't2_Code',                                N'Inv.Part',                        N'Code',                                N'[t2].[Code]',                                N'کد کالا',                      0, N'String',        100, NULL, NULL, 0),
    (13, N't2_Name',                                N'Inv.Part',                        N'Name',                                N'[t2].[Name]',                                N'نام کالا',                     0, N'String',        300, NULL, NULL, 0),
    (14, N't6_Title',                               N'Inv.PartUnit',                    N'Title',                               N'[t6].[Title]',                               N'واحد کالا',                    0, N'String',         90, NULL, NULL, 0),
    (15, N't1_RequiredQty',                         N'Sup.OpenOrderRequest',            N'RequiredQty',                         N'[t1].[RequiredQty]',                         N'تعداد درخواست',                7, N'Int',           100, NULL, NULL, 0),
    (16, N't1_OrderQty',                            N'Sup.OpenOrderRequest',            N'OrderQty',                            N'[t1].[OrderQty]',                            N'تعداد سفارش',                  7, N'Int',            90, NULL, NULL, 0),
    (17, N't1_SumSendQty',                          N'Sup.OpenOrderRequest',            N'SumSendQty',                          N'[t1].[SumSendQty]',                          N'تعداد برگ ارسال',              6, N'Long',          100, NULL, NULL, 0),
    (18, N't1_FactoredCount',                       N'Sup.OpenOrderRequest',            N'FactoredCount',                       N'[t1].[FactoredCount]',                       N'تعداد خرید',                   7, N'Int',            90, NULL, NULL, 0),
    (19, N't1_OrderItemComment',                    N'Sup.OpenOrderRequest',            N'OrderItemComment',                    N'[t1].[OrderItemComment]',                    N'توضیحات',                      0, N'String',        300, NULL, NULL, 0),
    (20, N't1_OrderShamsiDate',                     N'Sup.OpenOrderRequest',            N'OrderShamsiDate',                     N'[t1].[OrderShamsiDate]',                     N'تاریخ سفارش',                  5, N'DateShamsi',    100, NULL, NULL, 0),
    (21, N't1_NeedDateShamsiDate',                  N'Sup.OpenOrderRequest',            N'NeedDateShamsiDate',                  N'[t1].[NeedDateShamsiDate]',                  N'تاریخ نیاز',                   5, N'DateShamsi',    100, NULL, NULL, 0),
    (22, N't7_LeadTimeDay',                         N'Pln.LeadTime',                    N'LeadTimeDay',                         N'[t7].[LeadTimeDay]',                         N'زمان در راه (روز)',            7, N'Int',           100, NULL, NULL, 0),
    (23, N't1_SupplyShamsiDate',                    N'Sup.OpenOrderRequest',            N'SupplyShamsiDate',                    N'[t1].[SupplyShamsiDate]',                    N'تاریخ تامین',                  5, N'DateShamsi',    100, NULL, NULL, 0),
    (24, N'tCmtSup_ShamsiDate',                     N'Sup.OpenOrderRequestComment',     N'ShamsiDate',                          N'[tCmtSup].[CommentShamsiDate]',              N'تاریخ توضیحات تدارکات',        5, N'DateShamsi',    120, NULL, NULL, 0),
    (25, N'tCmtSup_CommentValue',                   N'Sup.OpenOrderRequestComment',     N'CommentValue',                        N'[tCmtSup].[CommentValue]',                   N'توضیحات تدارکات',              0, N'String',        250, NULL, NULL, 0),
    (26, N't1_ProductionOrderNumber',               N'Sup.OpenOrderRequest',            N'ProductionOrderNumber',               N'[t1].[ProductionOrderNumber]',               N'شماره سفارش ساخت',             7, N'Int',           110, NULL, NULL, 0),
    (27, N't15_Title',                              N'Sale.Branch',                     N'Title',                               N'[t15].[Title]',                              N'دپارتمان فروش',                0, N'String',        150, NULL, NULL, 0),
    (28, N't8_Code',                                N'FIN.DL',                          N'Code',                                N'[t8].[Code]',                                N'کد تفضیل',                     0, N'String',         90, NULL, NULL, 0),
    (29, N't8_Title',                               N'FIN.DL',                          N'Title',                               N'[t8].[Title]',                               N'تفضیل',                        0, N'String',        220, NULL, NULL, 0),
    (30, N't1_OrderConfirmShamsiDate',              N'Sup.OpenOrderRequest',            N'OrderConfirmShamsiDate',              N'[t1].[OrderConfirmShamsiDate]',              N'تاریخ تایید',                  5, N'DateShamsi',    100, NULL, NULL, 0),
    (31, N'tCmtInd_ShamsiDate',                     N'Sup.OpenOrderRequestComment',     N'ShamsiDate',                          N'[tCmtInd].[CommentShamsiDate]',              N'تاریخ توضیحات صنایع',          5, N'DateShamsi',    120, NULL, NULL, 0),
    (32, N'tCmtInd_CommentValue',                   N'Sup.OpenOrderRequestComment',     N'CommentValue',                        N'[tCmtInd].[CommentValue]',                   N'توضیحات صنایع',                0, N'String',        250, NULL, NULL, 0),
    (33, N'tCmtOth_ShamsiDate',                     N'Sup.OpenOrderRequestComment',     N'ShamsiDate',                          N'[tCmtOth].[CommentShamsiDate]',              N'تاریخ توضیحات درخواست کننده',  5, N'DateShamsi',    130, NULL, NULL, 0),
    (34, N'tCmtOth_CommentValue',                   N'Sup.OpenOrderRequestComment',     N'CommentValue',                        N'[tCmtOth].[CommentValue]',                   N'کامنت آخر',                    0, N'String',        250, NULL, NULL, 0),
    (35, N'tCo_CompanyNames',                       N'Gnr.Party',                       N'FullName',                            N'[tCo].[CompanyNames]',                       N'نام شرکت ها',                  0, N'String',        200, NULL, NULL, 0),
    (36, N'tCo_IsHasCompany',                       N'Inv.PartCompany',                 N'IsHasCompany',                        N'[tCo].[IsHasCompany]',                       N'شرکت دارد؟',                   1, N'Boolean',        90, NULL, NULL, 0),
    (37, N'tEml_EmailSendToSupplier',               N'Sup.OpenOrderRequestEmailToSupplier', N'EmailSendToSupplier',             N'[tEml].[EmailSendToSupplier]',               N'ایمیل به تامین کننده',         1, N'Boolean',       120, NULL, NULL, 0),
    (38, N'tEml_CompanyNameEmailSended',            N'Sup.OpenOrderRequestEmailToSupplier', N'CompanyNameEmailSended',          N'[tEml].[CompanyNameEmailSended]',            N'ایمیل به شرکت',                0, N'String',        150, NULL, NULL, 0),
    (39, N't1_RequestedPersonel',                   N'Sup.OpenOrderRequest',            N'RequestedPersonel',                   N'[t1].[RequestedPersonel]',                   N'ذینفعان',                      0, N'String',        250, NULL, NULL, 0),
    (40, N't1_RequestedEngineeringPersonel',        N'Sup.OpenOrderRequest',            N'RequestedEngineeringPersonel',        N'[t1].[RequestedEngineeringPersonel]',        N'درخواست کنندگان',              0, N'String',        250, NULL, NULL, 0),
    (41, N't1_Status',                              N'Sup.OpenOrderRequest',            N'Status',                              N'[t1].[Status]',                              N'وضعیت درخواست',                0, N'String',        110, NULL, NULL, 0),
    (42, N't13_Code',                               N'Inv.Part',                        N'Code',                                N'[t13].[Code]',                               N'کد محصول مرتبط',               0, N'String',        110, NULL, NULL, 0),
    (43, N't13_Name',                               N'Inv.Part',                        N'Name',                                N'[t13].[Name]',                               N'عنوان محصول مرتبط',            0, N'String',        250, NULL, NULL, 0),
    (44, N't1_IsRejectedByInspection',              N'Sup.OpenOrderRequest',            N'IsRejectedByInspection',              N'[t1].[IsRejectedByInspection]',              N'عدم تایید QC',                 1, N'Boolean',       110, NULL, NULL, 0),
    (45, N't1_IsStop',                              N'Sup.OpenOrderRequest',            N'IsStop',                              N'[t1].[IsStop]',                              N'متوقف شده',                    1, N'Boolean',       100, NULL, NULL, 0),
    (46, N't1_StopShamsiDate',                      N'Sup.OpenOrderRequest',            N'StopShamsiDate',                      N'[t1].[StopShamsiDate]',                      N'تاریخ توقف',                   5, N'DateShamsi',    100, NULL, NULL, 0),
    (47, N't1_Changed',                             N'Sup.OpenOrderRequest',            N'Changed',                             N'[t1].[Changed]',                             N'تغییر یافته',                  1, N'Boolean',       100, NULL, NULL, 0),
    (48, N't9_NameFa',                              N'system.User',                     N'NameFa',                              N'[t9].[NameFa]',                              N'کاربر مهندسی',                 0, N'String',        150, NULL, NULL, 0),
    (49, N't1_EngineeringAcceptShamsiDateTime',     N'Sup.OpenOrderRequest',            N'EngineeringAcceptShamsiDateTime',     N'[t1].[EngineeringAcceptShamsiDateTime]',     N'تاریخ و ساعت تایید مهندسی',    4, N'DateTimeShamsi',150, NULL, NULL, 0),
    (50, N't10_NameFa',                             N'system.User',                     N'NameFa',                              N'[t10].[NameFa]',                             N'تایید کننده پروژه/فروش',       0, N'String',        160, NULL, NULL, 0),
    (51, N't1_SalesUnitConfirmationShamsiDateTime', N'Sup.OpenOrderRequest',            N'SalesUnitConfirmationShamsiDateTime', N'[t1].[SalesUnitConfirmationShamsiDateTime]', N'تاریخ تایید پروژه/فروش',       4, N'DateTimeShamsi',150, NULL, NULL, 0),
    (52, N't1_SalesUnitConfirmationComment',        N'Sup.OpenOrderRequest',            N'SalesUnitConfirmationComment',        N'[t1].[SalesUnitConfirmationComment]',        N'کامنت پروژه/فروش',             0, N'String',        250, NULL, NULL, 0),
    (53, N't1_DeliveryVoucherShamsiDate',           N'Sup.OpenOrderRequest',            N'DeliveryVoucherShamsiDate',           N'[t1].[DeliveryVoucherShamsiDate]',           N'تاریخ سند تحویل',              5, N'DateShamsi',    110, NULL, NULL, 0),
    (54, N't1_TemporaryInventoryVoucherShamsiDate', N'Sup.OpenOrderRequest',            N'TemporaryInventoryVoucherShamsiDate', N'[t1].[TemporaryInventoryVoucherShamsiDate]', N'تاریخ سند موقت',               5, N'DateShamsi',    110, NULL, NULL, 0),
    (55, N't1_FinalInventoryVoucherShamsiDate',     N'Sup.OpenOrderRequest',            N'FinalInventoryVoucherShamsiDate',     N'[t1].[FinalInventoryVoucherShamsiDate]',     N'تاریخ سند دائم',               5, N'DateShamsi',    110, NULL, NULL, 0),
    -- ستون‌های افزوده (خارج از گرید 74 HTS؛ بخشی در 76/77 نمایش داده می‌شوند)
    (56, N't1_QcVoucherShamsiDate',                 N'Sup.OpenOrderRequest',            N'QcVoucherShamsiDate',                 N'[t1].[QcVoucherShamsiDate]',                 N'تاریخ سند QC',                 5, N'DateShamsi',    110, NULL, NULL, 0),
    (57, N't1_DelaysBuyDay',                        N'Sup.OpenOrderRequest',            N'DelaysBuyDay',                        N'[t1].[DelaysBuyDay]',                        N'تاخیر خرید (روز)',             7, N'Int',           100, NULL, NULL, 0),
    (58, N't1_CompletionShamsiDate',                N'Sup.OpenOrderRequest',            N'CompletionShamsiDate',                N'[t1].[CompletionShamsiDate]',                N'تاریخ تکمیل',                  5, N'DateShamsi',    100, NULL, NULL, 0),
    (59, N'tCalc_BuyProgress',                      N'Sup.OpenOrderRequest',            N'BuyProgress',                         N'CAST(CASE WHEN ISNULL([t1].[OrderQty], 0) = 0 THEN 0 ELSE ROUND(100.0 * [t1].[FactoredCount] / [t1].[OrderQty], 0) END AS int)', N'درصد پیشرفت خرید', 7, N'Int',      110, NULL, NULL, 0),
    (60, N't1_StopStatus',                          N'Sup.OpenOrderRequest',            N'StopStatus',                          N'[t1].[StopStatus]',                          N'وضعیت توقف',                   8, N'Select',        180, @EnumRender, @StopStatusOptionSetting, 0),
    (61, N't1_Comment',                             N'Sup.OpenOrderRequest',            N'Comment',                             N'[t1].[Comment]',                             N'توضیحات تکمیلی',               0, N'String',        250, NULL, NULL, 0),
    (62, N't1_IsDeletedShamsiDate',                 N'Sup.OpenOrderRequest',            N'IsDeletedShamsiDate',                 N'[t1].[IsDeletedShamsiDate]',                 N'تاریخ حذف/اختتام',             5, N'DateShamsi',    110, NULL, NULL, 0),
    (63, N't1_IsForceDeletedByUser',                N'Sup.OpenOrderRequest',            N'IsForceDeletedByUser',                N'[t1].[IsForceDeletedByUser]',                N'خاتمه دستی توسط کاربر',        1, N'Boolean',       110, NULL, NULL, 0),
    (64, N'tClr_InSupplyWindow',                    N'Sup.OpenOrderRequest',            N'InSupplyWindow',                      N'CAST(CASE WHEN [t1].[SupplyMiladiDate] IS NOT NULL AND @TodayDate BETWEEN DATEADD(DAY, -3, CAST([t1].[SupplyMiladiDate] AS date)) AND CAST([t1].[SupplyMiladiDate] AS date) THEN 1 ELSE 0 END AS bit)', N'در پنجره تامین (۳ روز)',       1, N'Boolean',       100, NULL, NULL, 0);

    -- چیدمان هر صفحه HTS: (LayoutKey, Seq نمایش, Alliance, Visible). ستون‌هایی که در چیدمان نیستند با Visible=0 در انتها اضافه می‌شوند.
    IF OBJECT_ID('tempdb..#Layout') IS NOT NULL DROP TABLE #Layout;
    CREATE TABLE #Layout (LayoutKey NVARCHAR(20) NOT NULL, Seq INT NOT NULL, Alliance NVARCHAR(100) NOT NULL, Visible BIT NOT NULL,
                          PRIMARY KEY (LayoutKey, Alliance));

    -- MAIN = صفحه 74 (_OpenOrderRequestManagement.cshtml L1639–1710)
    INSERT INTO #Layout (LayoutKey, Seq, Alliance, Visible) VALUES
    (N'MAIN',  1, N't1_Id', 0),
    (N'MAIN',  2, N'tAtt_HaveAttachment', 1),
    (N'MAIN',  3, N'tCalc_PurchaseCompleted', 1),
    (N'MAIN',  4, N't1_EngineeringAccept', 1),
    (N'MAIN',  5, N't1_HasSalesUnitConfirmation', 1),
    (N'MAIN',  6, N't5_NameFa', 1),
    (N'MAIN',  7, N't3_Title', 1),
    (N'MAIN',  8, N't1_PurchaseRequestNumber', 1),
    (N'MAIN',  9, N't1_PurchaseRequestShamsiDate', 1),
    (N'MAIN', 10, N't1_OrderNo', 1),
    (N'MAIN', 11, N't12_FullName', 1),        -- HTS: فقط isSupply — با ManCompanyVisible هر نمایه override می‌شود
    (N'MAIN', 12, N't2_Code', 1),
    (N'MAIN', 13, N't2_Name', 1),
    (N'MAIN', 14, N't6_Title', 1),
    (N'MAIN', 15, N't1_RequiredQty', 1),
    (N'MAIN', 16, N't1_OrderQty', 1),
    (N'MAIN', 17, N't1_SumSendQty', 1),
    (N'MAIN', 18, N't1_FactoredCount', 1),
    (N'MAIN', 19, N't1_OrderItemComment', 1),
    (N'MAIN', 20, N't1_OrderShamsiDate', 1),
    (N'MAIN', 21, N't1_NeedDateShamsiDate', 1),
    (N'MAIN', 22, N't7_LeadTimeDay', 1),
    (N'MAIN', 23, N't1_SupplyShamsiDate', 1),
    (N'MAIN', 24, N'tCmtSup_ShamsiDate', 1),
    (N'MAIN', 25, N'tCmtSup_CommentValue', 1),
    (N'MAIN', 26, N't1_ProductionOrderNumber', 1),
    (N'MAIN', 27, N't15_Title', 1),
    (N'MAIN', 28, N't8_Code', 1),
    (N'MAIN', 29, N't8_Title', 1),
    (N'MAIN', 30, N't1_OrderConfirmShamsiDate', 1),
    (N'MAIN', 31, N'tCmtInd_ShamsiDate', 1),
    (N'MAIN', 32, N'tCmtInd_CommentValue', 1),
    (N'MAIN', 33, N'tCmtOth_ShamsiDate', 1),
    (N'MAIN', 34, N'tCmtOth_CommentValue', 1),
    (N'MAIN', 35, N'tCo_CompanyNames', 1),
    (N'MAIN', 36, N'tEml_EmailSendToSupplier', 1),
    (N'MAIN', 37, N't1_RequestedPersonel', 1),
    (N'MAIN', 38, N't1_RequestedEngineeringPersonel', 1),
    (N'MAIN', 39, N't1_Status', 1),
    -- (PartCodingTitle «عنوان کدینگ» — بدون معادل)
    (N'MAIN', 40, N't13_Code', 1),
    (N'MAIN', 41, N't13_Name', 1),
    (N'MAIN', 42, N't1_IsRejectedByInspection', 1),
    (N'MAIN', 43, N't1_IsStop', 1),
    (N'MAIN', 44, N't1_StopShamsiDate', 1),
    (N'MAIN', 45, N't1_Changed', 1),
    (N'MAIN', 46, N't9_NameFa', 1),
    (N'MAIN', 47, N't1_EngineeringAcceptShamsiDateTime', 1),   -- تاریخ + ساعت تایید مهندسی (دو ستون HTS)
    (N'MAIN', 48, N't10_NameFa', 1),
    (N'MAIN', 49, N't1_SalesUnitConfirmationShamsiDateTime', 1),
    (N'MAIN', 50, N't1_SalesUnitConfirmationComment', 1),
    (N'MAIN', 51, N't1_DeliveryVoucherShamsiDate', 1),
    (N'MAIN', 52, N't1_TemporaryInventoryVoucherShamsiDate', 1),
    (N'MAIN', 53, N't1_FinalInventoryVoucherShamsiDate', 1),
    -- ستون‌های قبلی نمایه که در HTS نبودند (حفظ نمایش)
    (N'MAIN', 54, N't1_QcVoucherShamsiDate', 1),
    (N'MAIN', 55, N't1_StopStatus', 1);

    -- CONFIG = صفحه 76 (_OpenOrderRequestConfigManagement.cshtml L737–790)
    INSERT INTO #Layout (LayoutKey, Seq, Alliance, Visible) VALUES
    (N'CONFIG',  1, N't1_Id', 0),
    (N'CONFIG',  2, N't1_PurchaseRequestNumber', 1),
    (N'CONFIG',  3, N't1_PurchaseRequestShamsiDate', 1),
    (N'CONFIG',  4, N't1_EngineeringAccept', 1),
    (N'CONFIG',  5, N't1_HasSalesUnitConfirmation', 1),
    (N'CONFIG',  6, N'tAtt_HaveAttachment', 1),
    (N'CONFIG',  7, N't5_NameFa', 1),
    (N'CONFIG',  8, N'tCo_CompanyNames', 1),
    (N'CONFIG',  9, N't12_FullName', 1),
    (N'CONFIG', 10, N't2_Code', 1),
    (N'CONFIG', 11, N't2_Name', 1),
    (N'CONFIG', 12, N't1_OrderItemComment', 1),
    (N'CONFIG', 13, N't1_OrderShamsiDate', 1),
    (N'CONFIG', 14, N't1_NeedDateShamsiDate', 1),
    (N'CONFIG', 15, N't1_SupplyShamsiDate', 1),
    (N'CONFIG', 16, N'tCmtSup_CommentValue', 1),
    (N'CONFIG', 17, N'tCmtSup_ShamsiDate', 1),
    (N'CONFIG', 18, N't1_RequiredQty', 1),
    (N'CONFIG', 19, N't1_OrderQty', 1),
    (N'CONFIG', 20, N't1_OrderConfirmShamsiDate', 1),
    (N'CONFIG', 21, N't1_OrderNo', 1),
    (N'CONFIG', 22, N't1_SumSendQty', 1),
    (N'CONFIG', 23, N't1_FactoredCount', 1),
    (N'CONFIG', 24, N't3_Title', 1),
    (N'CONFIG', 25, N't1_ProductionOrderNumber', 1),
    (N'CONFIG', 26, N't15_Title', 1),
    (N'CONFIG', 27, N't8_Code', 1),
    (N'CONFIG', 28, N't8_Title', 1),
    (N'CONFIG', 29, N'tCmtInd_ShamsiDate', 1),
    (N'CONFIG', 30, N'tCmtInd_CommentValue', 1),
    (N'CONFIG', 31, N'tCmtOth_ShamsiDate', 1),
    (N'CONFIG', 32, N'tCmtOth_CommentValue', 1),
    (N'CONFIG', 33, N'tCo_IsHasCompany', 1),
    (N'CONFIG', 34, N'tEml_EmailSendToSupplier', 1),
    (N'CONFIG', 35, N't1_RequestedPersonel', 1),
    (N'CONFIG', 36, N't1_RequestedEngineeringPersonel', 1),
    (N'CONFIG', 37, N't1_Status', 1),
    (N'CONFIG', 38, N't1_IsStop', 1),
    (N'CONFIG', 39, N't1_StopShamsiDate', 1),
    (N'CONFIG', 40, N't1_Changed', 1),
    (N'CONFIG', 41, N't9_NameFa', 1),
    (N'CONFIG', 42, N't1_EngineeringAcceptShamsiDateTime', 1),
    (N'CONFIG', 43, N't1_DelaysBuyDay', 1),
    (N'CONFIG', 44, N't1_CompletionShamsiDate', 1),
    (N'CONFIG', 45, N'tCalc_BuyProgress', 1);

    -- HISTORY = صفحه 77 (_OpenOrderRequestHistoryManagement.cshtml L376–445)
    INSERT INTO #Layout (LayoutKey, Seq, Alliance, Visible) VALUES
    (N'HISTORY',  1, N't1_Id', 0),
    (N'HISTORY',  2, N'tAtt_HaveAttachment', 1),
    (N'HISTORY',  3, N't1_EngineeringAccept', 1),
    (N'HISTORY',  4, N't1_HasSalesUnitConfirmation', 1),
    (N'HISTORY',  5, N't5_NameFa', 1),
    (N'HISTORY',  6, N't3_Title', 1),
    (N'HISTORY',  7, N't1_PurchaseRequestNumber', 1),
    (N'HISTORY',  8, N't1_PurchaseRequestShamsiDate', 1),
    (N'HISTORY',  9, N't1_OrderNo', 1),
    (N'HISTORY', 10, N't2_Code', 1),
    (N'HISTORY', 11, N't2_Name', 1),
    (N'HISTORY', 12, N't6_Title', 1),
    (N'HISTORY', 13, N't1_RequiredQty', 1),
    (N'HISTORY', 14, N't1_OrderQty', 1),
    (N'HISTORY', 15, N't1_SumSendQty', 1),
    (N'HISTORY', 16, N't1_FactoredCount', 1),
    (N'HISTORY', 17, N't1_OrderItemComment', 1),
    (N'HISTORY', 18, N't1_OrderShamsiDate', 1),
    (N'HISTORY', 19, N't1_NeedDateShamsiDate', 1),
    (N'HISTORY', 20, N't1_SupplyShamsiDate', 1),
    (N'HISTORY', 21, N'tCmtSup_ShamsiDate', 1),
    (N'HISTORY', 22, N'tCmtSup_CommentValue', 1),
    (N'HISTORY', 23, N't1_DelaysBuyDay', 1),
    (N'HISTORY', 24, N't1_CompletionShamsiDate', 1),
    (N'HISTORY', 25, N'tCalc_BuyProgress', 1),
    (N'HISTORY', 26, N't7_LeadTimeDay', 1),
    (N'HISTORY', 27, N't1_ProductionOrderNumber', 1),
    (N'HISTORY', 28, N't15_Title', 1),
    (N'HISTORY', 29, N't8_Code', 1),
    (N'HISTORY', 30, N't8_Title', 1),
    (N'HISTORY', 31, N't1_OrderConfirmShamsiDate', 1),
    (N'HISTORY', 32, N'tCmtInd_ShamsiDate', 1),
    (N'HISTORY', 33, N'tCmtInd_CommentValue', 1),
    (N'HISTORY', 34, N'tCmtOth_ShamsiDate', 1),
    (N'HISTORY', 35, N'tCmtOth_CommentValue', 1),
    (N'HISTORY', 36, N'tCo_CompanyNames', 1),
    (N'HISTORY', 37, N'tEml_CompanyNameEmailSended', 1),
    (N'HISTORY', 38, N't1_RequestedPersonel', 1),
    (N'HISTORY', 39, N't1_RequestedEngineeringPersonel', 1),
    (N'HISTORY', 40, N't1_Status', 1),
    -- (PartCodingTitle — بدون معادل)
    (N'HISTORY', 41, N't12_FullName', 1),
    (N'HISTORY', 42, N't13_Code', 1),
    (N'HISTORY', 43, N't13_Name', 1),
    (N'HISTORY', 44, N't1_IsRejectedByInspection', 1),
    (N'HISTORY', 45, N't1_IsStop', 1),
    (N'HISTORY', 46, N't1_StopShamsiDate', 1),
    (N'HISTORY', 47, N't1_Changed', 1),
    (N'HISTORY', 48, N't9_NameFa', 1),
    (N'HISTORY', 49, N't1_EngineeringAcceptShamsiDateTime', 1),
    (N'HISTORY', 50, N't1_DeliveryVoucherShamsiDate', 1),
    (N'HISTORY', 51, N't1_TemporaryInventoryVoucherShamsiDate', 1),
    (N'HISTORY', 52, N't1_FinalInventoryVoucherShamsiDate', 1),
    -- افزوده برای آرشیو
    (N'HISTORY', 53, N't1_IsDeletedShamsiDate', 1),
    (N'HISTORY', 54, N't1_IsForceDeletedByUser', 1);

    ---------------------------------------------------------------------------
    PRINT N'=== [3/7] دکمه‌های سفارشی و EventScripts (رنگ ردیف مطابق HTS) ===';
    ---------------------------------------------------------------------------
    -- «لیست کامنت ها» — بدون debugger/console.log (D49)؛ مسیر مطلق
    DECLARE @ScriptListComments NVARCHAR(MAX) = N'function(ctx) {
    if (!ctx.selectedRow) {
        toastr.warning("لطفاً یک ردیف انتخاب کنید");
        return;
    }
    var openOrderRequestId = ctx.primaryKeyValue;
    get("/Panel/Sup/OpenOrderRequest/CommentListPartial?openOrderRequestId=" + openOrderRequestId, function (result) {
        var bodyCn = "<div style=\"overflow-x: hidden; overflow-y: auto; max-height: 70vh;\">" + result + "</div>";
        $.confirm({
            title: "لیست کامنت ها",
            columnClass: "col-md-12",
            content: bodyCn,
            rtl: true,
            buttons: {
                cancel: { text: "بستن", btnClass: "btn-secondary" }
            }
        });
    });
}';

    -- «افزودن درخواست کنندگان و ذینفعان» (صفحه 76) — openOrderRequestId=1 هاردکد و debugger حذف شد (D49)؛ رفرش گرید بعد از ذخیره
    DECLARE @ScriptMultiAddPersonel NVARCHAR(MAX) = N'function(ctx) {
    var ids = ctx.multiSelectMode ? (ctx.primaryKeyValues || []) : (ctx.primaryKeyValue ? [ctx.primaryKeyValue] : []);
    if (!ids.length) {
        toastr.warning("لطفاً حداقل یک ردیف انتخاب کنید");
        return;
    }
    if (!AppSdk.hasRole("Sup.OpenOrderRequest.ConfigManage")) {
        toastr.error("شما دسترسی افزودن ذینفعان را ندارید");
        return;
    }
    var toIds = function (v) {
        var arr = Array.isArray(v) ? v : String(v || "").split(",");
        return arr.map(function (c) { return parseInt(c, 10); }).filter(function (n) { return !isNaN(n); });
    };
    get("/Panel/Sup/OpenOrderRequest/AddRequestedPersonelPartial?openOrderRequestId=" + ids[0], function (result) {
        var bodyCn = "<div style=\"overflow-x: hidden; overflow-y: hidden; max-height: 70vh;\">" + result + "</div>";
        $.confirm({
            title: "افزودن درخواست کنندگان و ذینفعان",
            columnClass: "col-md-10",
            content: bodyCn,
            rtl: true,
            buttons: {
                save: {
                    text: "ذخیره",
                    btnClass: "btn-success",
                    action: function () {
                        var $content = this.$content;
                        if (validateError($content)) { return false; }
                        var model = $content.dataBind();
                        model.OpenOrderRequestIds = ids;
                        model.requestedPersonelIds = toIds(model.requestedPersonelIds);
                        model.requestedEngineeringPersonelIds = toIds(model.requestedEngineeringPersonelIds);
                        var self = this;
                        var $btn = this.$$save.block();
                        post("/Panel/Sup/OpenOrderRequest/AddRequestedPersonelToOpenRequest", model, function (r) {
                            $btn.block(false);
                            if (!r.isSuccess) {
                                toastr.error(r.message, "خطا");
                                return false;
                            }
                            toastr.success("اطلاعات با موفقیت ثبت شد");
                            self.close();
                            ctx.draw();
                        });
                        return false;
                    }
                },
                cancel: { text: "انصراف", btnClass: "btn-secondary" }
            }
        });
    });
}';

    DECLARE @BtnListComments NVARCHAR(MAX) = (
        SELECT N'id_7m7we7buq' AS id, N'لیست کامنت ها' AS title, N'listComments' AS dataActionName,
               N'btn-color-warning' AS colorClass, N'fa fa-note' AS iconClass,
               CAST(1 AS bit) AS requiresSelection, CAST(0 AS bit) AS requiresMultiSelection,
               CAST(0 AS bit) AS useHtml, N'' AS html, @ScriptListComments AS actionScript
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

    DECLARE @BtnMultiAddPersonel NVARCHAR(MAX) = (
        SELECT N'id_dx3c3wury' AS id, N'افزودن درخواست کنندگان و ذینفعان' AS title, N'multyAddRequestedPersonel' AS dataActionName,
               N'btn-color-primary' AS colorClass, N'fa fa-user-plus' AS iconClass,
               CAST(0 AS bit) AS requiresSelection, CAST(1 AS bit) AS requiresMultiSelection,
               CAST(0 AS bit) AS useHtml, N'' AS html, @ScriptMultiAddPersonel AS actionScript
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

    /* -----------------------------------------------------------------------
       TODO WP1 — دکمه «ارسال به پیمانکار» روی نمایه تنظیمات (HTS: SendEmailToContractor، Permission SendEmail=27، صفحه 76).
       بعد از WP1 (اکشن POST /Panel/Sup/OpenOrderRequest/SendEmailToSupplier + پارشیال _SendEmailToSupplierPartial + نقش
       Sup.OpenOrderRequest.SendEmail) بلوک زیر را از کامنت خارج و در @ButtonsConfig اضافه کنید:
           SET @ButtonsConfig = N'[' + @BtnListComments + N',' + @BtnMultiAddPersonel + N',' + @BtnSendToSupplier + N']';

    DECLARE @ScriptSendToSupplier NVARCHAR(MAX) = N'function(ctx) {
    var ids = ctx.multiSelectMode ? (ctx.primaryKeyValues || []) : (ctx.primaryKeyValue ? [ctx.primaryKeyValue] : []);
    if (!ids.length) { toastr.warning("لطفاً حداقل یک ردیف انتخاب کنید"); return; }
    if (!AppSdk.hasRole("Sup.OpenOrderRequest.SendEmail")) { toastr.error("شما دسترسی ارسال به پیمانکار را ندارید"); return; }
    get("/Panel/Sup/OpenOrderRequest/SendEmailToSupplierPartial?openOrderRequestId=" + ids[0], function (result) {
        $.confirm({
            title: "ارسال به پیمانکار",
            columnClass: "col-md-10",
            content: "<div style=\"overflow-x: hidden; overflow-y: auto; max-height: 70vh;\">" + result + "</div>",
            rtl: true,
            buttons: {
                send: {
                    text: "ارسال",
                    btnClass: "btn-success",
                    action: function () {
                        var $content = this.$content;
                        if (validateError($content)) { return false; }
                        var model = $content.dataBind();
                        model.OpenOrderRequestIds = ids;
                        var self = this;
                        var $btn = this.$$send.block();
                        post("/Panel/Sup/OpenOrderRequest/SendEmailToSupplier", model, function (r) {
                            $btn.block(false);
                            if (!r.isSuccess) { toastr.error(r.message, "خطا"); return false; }
                            toastr.success("ایمیل به پیمانکار ثبت/ارسال شد");
                            self.close();
                            ctx.draw();
                        });
                        return false;
                    }
                },
                cancel: { text: "انصراف", btnClass: "btn-secondary" }
            },
            onContentReady: function () { initFileUploaders(this.$content); }
        });
    });
}';
    DECLARE @BtnSendToSupplier NVARCHAR(MAX) = (
        SELECT N'id_oor_sendToSupplier' AS id, N'ارسال به پیمانکار' AS title, N'sendToSupplier' AS dataActionName,
               N'btn-color-success' AS colorClass, N'fa fa-envelope' AS iconClass,
               CAST(0 AS bit) AS requiresSelection, CAST(1 AS bit) AS requiresMultiSelection,
               CAST(0 AS bit) AS useHtml, N'' AS html, @ScriptSendToSupplier AS actionScript
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
       ----------------------------------------------------------------------- */

    DECLARE @ButtonsMain   NVARCHAR(MAX) = N'[' + @BtnListComments + N']';
    DECLARE @ButtonsConfig NVARCHAR(MAX) = N'[' + @BtnListComments + N',' + @BtnMultiAddPersonel + N']';
    DECLARE @ButtonsHistory NVARCHAR(MAX) = N'[' + @BtnListComments + N']';   -- فقط مشاهده (پیوست‌ها از صفحه Edit)

    -- رنگ ردیف صفحه 74 HTS (changeRowColor در _OpenOrderRequestManagement.cshtml L192–224) — ترتیب if ها عین HTS
    -- (هر قاعده همان سلول را بازنویسی می‌کند؛ نتیجه موثر: پنجره تامین > توقف > تغییر یافته > ایمیل پیمانکار؛ تایید فروش روی سلول دوم).
    -- HTS td:first  → Havayar  $row td eq(1)  (eq(0) = چک‌باکس انتخاب ردیف)
    -- HTS td:nth-child(2) → Havayar $row td eq(2)
    -- شرط پنجره تامین در SQL محاسبه شده: امروز BETWEEN (تاریخ تامین − ۳ روز) AND تاریخ تامین  → tClr_InSupplyWindow
    DECLARE @OnRowAddedFull NVARCHAR(MAX) = N'function(ctx) {
    var d = ctx.rowData || {};
    var yes = function (v) { return v === true || v === 1 || v === "true" || v === "بله"; };
    var $cells = ctx.$row.children("td");
    var $c1 = $cells.eq(1);
    var $c2 = $cells.eq(2);
    $c1.css("background-color", "");
    $c2.css("background-color", "");
    // TODO WP1: ایمیل به پیمانکار (Sup.OpenOrderRequestEmailToSupplier) — بعد از WP1 خط بعدی را فعال کنید
    // if (yes(d.tEml_EmailSendToSupplier)) { $c1.css("background-color", "lightgreen"); }
    if (yes(d.t1_Changed)) { $c1.css("background-color", "violet"); }
    if (yes(d.t1_IsStop)) { $c1.css("background-color", "orangered"); }
    if (yes(d.t1_HasSalesUnitConfirmation)) { $c2.css("background-color", "orange"); }
    if (yes(d.tClr_InSupplyWindow)) { $c1.css("background-color", "deepskyblue"); }
}';

    -- رنگ ردیف صفحات 76 / 77 HTS (فقط سبز/بنفش/قرمز — _OpenOrderRequestConfigManagement L115–125، _OpenOrderRequestHistoryManagement L132–142)
    DECLARE @OnRowAddedBasic NVARCHAR(MAX) = N'function(ctx) {
    var d = ctx.rowData || {};
    var yes = function (v) { return v === true || v === 1 || v === "true" || v === "بله"; };
    var $c1 = ctx.$row.children("td").eq(1);
    $c1.css("background-color", "");
    // TODO WP1: ایمیل به پیمانکار (Sup.OpenOrderRequestEmailToSupplier) — بعد از WP1 خط بعدی را فعال کنید
    // if (yes(d.tEml_EmailSendToSupplier)) { $c1.css("background-color", "lightgreen"); }
    if (yes(d.t1_Changed)) { $c1.css("background-color", "violet"); }
    if (yes(d.t1_IsStop)) { $c1.css("background-color", "orangered"); }
}';

    DECLARE @EventsFull  NVARCHAR(MAX) = (SELECT N'' AS onSelectedRow, @OnRowAddedFull  AS onRowAdded FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    DECLARE @EventsBasic NVARCHAR(MAX) = (SELECT N'' AS onSelectedRow, @OnRowAddedBasic AS onRowAdded FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

    -- ActionOptions (D35/D45: new و delete در همه نمایه‌ها غیرفعال؛ HTS نه Delete داشت نه New)
    DECLARE @ActionsView NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"},{"dataActionName":"defaultMultiSelect","enable":false,"title":"انتخاب چندتایی پیش‌فرض"}]';
    DECLARE @ActionsConfig NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"},{"dataActionName":"defaultMultiSelect","enable":true,"title":"انتخاب چندتایی پیش‌فرض"}]';

    ---------------------------------------------------------------------------
    PRINT N'=== [4/7] کاتالوگ نمایه‌ها (Name ثابت — RoleAccess/فیلترهای کاربران حفظ می‌شود) ===';
    ---------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#Profiles') IS NOT NULL DROP TABLE #Profiles;
    CREATE TABLE #Profiles
    (
        Ord               INT           NOT NULL,
        Name              NVARCHAR(100) NOT NULL PRIMARY KEY,
        Title             NVARCHAR(200) NOT NULL,
        HtsPage           NVARCHAR(200) NOT NULL,
        LayoutKey         NVARCHAR(20)  NOT NULL,
        ManCompanyVisible BIT           NOT NULL,
        WhereClause       NVARCHAR(MAX) NOT NULL,
        ActionOptions     NVARCHAR(MAX) NOT NULL,
        ButtonsJson       NVARCHAR(MAX) NOT NULL,
        EventScriptsJson  NVARCHAR(MAX) NOT NULL,
        ColumnsJson       NVARCHAR(MAX) NULL,
        CustomQuery       NVARCHAR(MAX) NULL,
        SavedQueryId      BIGINT        NULL
    );

    INSERT INTO #Profiles (Ord, Name, Title, HtsPage, LayoutKey, ManCompanyVisible, WhereClause, ActionOptions, ButtonsJson, EventScriptsJson) VALUES
    (1, N'openorderrequest_listinfo',               N'تمامی درخواست ها',        N'74 — Admin/FullAccess (شامل حذف‌شده)',                      N'MAIN',    1, N'',                                                                                        @ActionsView,   @ButtonsMain,    @EventsFull),
    (2, N'vw_OpenOrderRequestAllActive',            N'همه فعال',                N'74 — ShowAll / صنایع / مهندسی (!IsDeleted)',                 N'MAIN',    0, N'
WHERE
    [t1].[IsDeleted] = 0',                                                                                                                       @ActionsView,   @ButtonsMain,    @EventsFull),
    (3, N'openorderrequest_listinfonew',            N'منتظر تایید مهندسی',      N'74 — کارتابل Accept_Engineering',                            N'MAIN',    0, N'
WHERE
    [t1].[EngineeringAccept] = 0
    AND [t1].[IsDeleted] = 0',                                                                                                                   @ActionsView,   @ButtonsMain,    @EventsFull),
    (4, N'vw_OpenOrderRequestEngineeringAccepted',  N'تأیید مهندسی شده',        N'74 — فروش/پروژه و تدارکات (!IsDeleted && Engineering_Accept)', N'MAIN',    1, N'
WHERE
    [t1].[IsDeleted] = 0
    AND [t1].[EngineeringAccept] = 1',                                                                                                           @ActionsView,   @ButtonsMain,    @EventsFull),
    (5, N'vw_OpenOrderRequestMyRequestes',          N'درخواست های من',          N'74 — متولی خرید (افزوده در سیستم جدید)',                     N'MAIN',    1, N'
WHERE
    [t1].[IsDeleted] = 0
    AND [t3].[PurchaseResponsibleId] = @CurrentUserId',                                                                                          @ActionsView,   @ButtonsMain,    @EventsFull),
    (6, N'vw_OpenOrderRequestOtherView',            N'مشاهده عمومی',            N'74 — شاخه else (IsDeleted=0 ∧ EngineeringAccept ∧ ذینفع/درخواست‌کننده = کاربر جاری — D8)', N'MAIN', 0, N'
WHERE
    [t1].[IsDeleted] = 0
    AND [t1].[EngineeringAccept] = 1
    AND (
        EXISTS (
            SELECT 1
            FROM OPENJSON(CASE WHEN ISJSON([t1].[RequestedPersonelIds]) = 1 THEN [t1].[RequestedPersonelIds] ELSE N''[]'' END) AS [rp]
            WHERE TRY_CAST([rp].[value] AS bigint) = TRY_CAST(@CurrentUserId AS bigint)
        )
        OR EXISTS (
            SELECT 1
            FROM OPENJSON(CASE WHEN ISJSON([t1].[RequestedEngineeringPersonelIds]) = 1 THEN [t1].[RequestedEngineeringPersonelIds] ELSE N''[]'' END) AS [re]
            WHERE TRY_CAST([re].[value] AS bigint) = TRY_CAST(@CurrentUserId AS bigint)
        )
    )',                                                                                                                                          @ActionsView,   @ButtonsMain,    @EventsFull),
    (7, N'vw_openRequestConfig',                    N'تنظیمات درخواست های باز', N'76 — همه فعال بدون فیلتر کارتابل',                           N'CONFIG',  1, N'
WHERE
    [t1].[IsDeleted] = 0',                                                                                                                       @ActionsConfig, @ButtonsConfig,  @EventsBasic),
    (8, N'vw_openOrderRequestPurchaseCompleted',    N'پیشینه درخواست ها',       N'77 — حذف/اختتام‌شده (IsDeleted)',                            N'HISTORY', 1, N'
WHERE
    [t1].[IsDeleted] = 1',                                                                                                                       @ActionsView,   @ButtonsHistory, @EventsBasic);

    -- ساخت CustomQuery و ColumnsJson هر نمایه
    DECLARE @PName NVARCHAR(100), @PLayout NVARCHAR(20), @PManVisible BIT, @PWhere NVARCHAR(MAX), @PColumns NVARCHAR(MAX);
    DECLARE profile_build_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT Name, LayoutKey, ManCompanyVisible, WhereClause FROM #Profiles ORDER BY Ord;
    OPEN profile_build_cur;
    FETCH NEXT FROM profile_build_cur INTO @PName, @PLayout, @PManVisible, @PWhere;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @PColumns = (
            SELECT
                x.TableName,
                x.Alliance AS ColumnName,          -- حالت Query: ColumnName = Alliance (مثل Seed_SaleOrderDetail_DataProfiles)
                x.DisplayName,
                x.Alliance,
                x.Address,
                x.SystemTypeName,
                0 AS SelectIndex,
                x.VisibleFinal AS Visible,
                x.IsPk AS PrimaryKey,
                x.SystemType,
                CASE WHEN x.IsPk = 1 THEN 1 END AS SortDirection,
                CAST(NULL AS INT) AS SortOrder,
                CAST(0 AS bit) AS GroupBy,
                JSON_QUERY(N'[]') AS Options,
                JSON_QUERY(x.OptionSettingJson) AS OptionSetting,
                CAST(NULL AS NVARCHAR(50)) AS Aggregate,
                x.Render,
                CAST(0 AS bit) AS IsCustom,
                CAST(NULL AS NVARCHAR(50)) AS CustomColType,
                CAST(NULL AS NVARCHAR(50)) AS HtmlTemplate,
                CAST(NULL AS NVARCHAR(50)) AS BtnConfig,
                CAST(NULL AS NVARCHAR(50)) AS InputConfig,
                x.Width,
                CAST(1 AS bit) AS Filterable,
                CAST(1 AS bit) AS Sortable,
                N'' AS ClassName
            FROM (
                SELECT l.Seq AS OrderSeq, c.TableName, c.Alliance, c.DisplayName, c.Address, c.SystemTypeName, c.SystemType,
                       c.IsPk, c.OptionSettingJson, c.Render, c.Width,
                       CAST(CASE WHEN c.Alliance = N't12_FullName' THEN @PManVisible ELSE l.Visible END AS bit) AS VisibleFinal
                FROM #Layout l
                INNER JOIN #ColDef c ON c.Alliance = l.Alliance
                WHERE l.LayoutKey = @PLayout
                UNION ALL
                SELECT 1000 + c.Seq, c.TableName, c.Alliance, c.DisplayName, c.Address, c.SystemTypeName, c.SystemType,
                       c.IsPk, c.OptionSettingJson, c.Render, c.Width,
                       CAST(0 AS bit)
                FROM #ColDef c
                WHERE NOT EXISTS (SELECT 1 FROM #Layout l WHERE l.LayoutKey = @PLayout AND l.Alliance = c.Alliance)
            ) AS x
            ORDER BY x.OrderSeq
            FOR JSON PATH, INCLUDE_NULL_VALUES);

        UPDATE #Profiles
        SET ColumnsJson = @PColumns,
            CustomQuery = @Setup + @SelectBody + @PWhere
        WHERE Name = @PName;

        FETCH NEXT FROM profile_build_cur INTO @PName, @PLayout, @PManVisible, @PWhere;
    END
    CLOSE profile_build_cur;
    DEALLOCATE profile_build_cur;

    IF EXISTS (SELECT 1 FROM #Profiles
               WHERE ColumnsJson IS NULL OR ISJSON(ColumnsJson) <> 1
                  OR ISJSON(EventScriptsJson) <> 1
                  OR ISJSON(ActionOptions) <> 1
                  OR ISJSON(ButtonsJson) <> 1)
        THROW 51010, N'ساخت JSON نمایه ناموفق بود (ColumnsJson / EventScripts / ActionOptions / Buttons).', 1;

    ---------------------------------------------------------------------------
    PRINT N'=== [5/7] گزارش تغییرات + Upsert system.SavedQuery ===';
    ---------------------------------------------------------------------------
    DECLARE @QueryJsonSkeleton NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}';

    DECLARE @PTitle NVARCHAR(200), @PHts NVARCHAR(200), @PActions NVARCHAR(MAX), @PButtons NVARCHAR(MAX), @PEvents NVARCHAR(MAX),
            @PCustomQuery NVARCHAR(MAX), @PQueryJson NVARCHAR(MAX), @PId BIGINT;
    DECLARE @CurId BIGINT, @CurTitle NVARCHAR(200), @CurMode INT, @CurIsActive INT, @CurCustomQuery NVARCHAR(MAX),
            @CurColumns NVARCHAR(MAX), @CurButtons NVARCHAR(MAX), @CurEvents NVARCHAR(MAX), @CurActions NVARCHAR(MAX);
    DECLARE @Msg NVARCHAR(4000);

    DECLARE profile_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT Name, Title, HtsPage, ActionOptions, ButtonsJson, EventScriptsJson, ColumnsJson, CustomQuery FROM #Profiles ORDER BY Ord;
    OPEN profile_cur;
    FETCH NEXT FROM profile_cur INTO @PName, @PTitle, @PHts, @PActions, @PButtons, @PEvents, @PColumns, @PCustomQuery;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @PQueryJson = JSON_MODIFY(@QueryJsonSkeleton, '$.CustomQuery', @PCustomQuery);
        IF ISJSON(@PQueryJson) <> 1
            THROW 51011, N'QueryJson نامعتبر است.', 1;

        SELECT @CurId = NULL, @CurTitle = NULL, @CurMode = NULL, @CurIsActive = NULL, @CurCustomQuery = NULL,
               @CurColumns = NULL, @CurButtons = NULL, @CurEvents = NULL, @CurActions = NULL;

        SELECT @CurId = q.Id, @CurTitle = q.Title, @CurMode = q.Mode, @CurIsActive = q.IsActive,
               @CurColumns = q.ColumnsJson, @CurButtons = q.CustomActionButtonsJson, @CurEvents = q.EventScriptsJson, @CurActions = q.ActionOptions,
               @CurCustomQuery = j.CustomQuery
        FROM system.SavedQuery q
        OUTER APPLY OPENJSON(q.QueryJson) WITH (CustomQuery NVARCHAR(MAX) '$.CustomQuery') AS j
        WHERE q.Name = @PName;

        IF @CurId IS NULL
        BEGIN
            PRINT N'  [' + @PName + N'] → INSERT (نمایه وجود نداشت) — ' + @PTitle + N' | HTS ' + @PHts;
        END
        ELSE
        BEGIN
            SET @Msg = N'  [' + @PName + N'] Id=' + CAST(@CurId AS NVARCHAR(20)) + N' → UPDATE:';
            IF ISNULL(@CurTitle, N'') <> @PTitle                           SET @Msg = @Msg + N' Title(' + ISNULL(@CurTitle, N'NULL') + N' → ' + @PTitle + N')';
            IF ISNULL(@CurMode, -1) <> 1                                   SET @Msg = @Msg + N' Mode(' + CAST(ISNULL(@CurMode, -1) AS NVARCHAR(5)) + N' → 1 Query)';
            IF ISNULL(@CurIsActive, -1) <> 1                               SET @Msg = @Msg + N' IsActive(→1)';
            IF ISNULL(@CurCustomQuery, N'') <> @PCustomQuery
                SET @Msg = @Msg + N' CustomQuery(changed' + CASE WHEN ISNULL(@CurCustomQuery, N'') LIKE N'%INNER JOIN%' THEN N'; INNER JOIN → LEFT/APPLY' ELSE N'' END + N')';
            IF ISNULL(@CurColumns, N'') <> @PColumns
                SET @Msg = @Msg + N' ColumnsJson(' + CAST((SELECT COUNT(*) FROM OPENJSON(ISNULL(@CurColumns, N'[]'))) AS NVARCHAR(5)) + N' → ' + CAST((SELECT COUNT(*) FROM OPENJSON(@PColumns)) AS NVARCHAR(5)) + N' ستون)';
            IF ISNULL(@CurButtons, N'') <> @PButtons                       SET @Msg = @Msg + N' CustomActionButtons(changed)';
            IF ISNULL(@CurEvents, N'') <> @PEvents                         SET @Msg = @Msg + N' EventScripts(changed)';
            IF ISNULL(@CurActions, N'') <> @PActions                       SET @Msg = @Msg + N' ActionOptions(changed)';
            IF RIGHT(@Msg, 7) = N'UPDATE:'                                 SET @Msg = @Msg + N' بدون تغییر';
            PRINT @Msg;
        END

        IF @CurId IS NOT NULL
        BEGIN
            UPDATE system.SavedQuery
            SET Title = @PTitle,
                QueryJson = @PQueryJson,
                ColumnsJson = @PColumns,
                DiagramJson = N'{}',
                CustomActionButtonsJson = @PButtons,
                EventScriptsJson = @PEvents,
                Mode = 1,
                Type = 1,
                EntityFullName = @EntityFullName,
                ActionOptions = @PActions,
                ModifiedById = 1,
                ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now,
                ModifiedDateShamsiDateTime = @NowShamsi,
                IsActive = 1
            WHERE Id = @CurId;
            SET @PId = @CurId;
        END
        ELSE
        BEGIN
            INSERT INTO system.SavedQuery
            (
                Name, Title, QueryJson, ColumnsJson, DiagramJson,
                CustomActionButtonsJson, EventScriptsJson,
                Mode, Type, EntityFullName, ActionOptions,
                CreatedById, ModifiedById, CreatedByName, ModifiedByName,
                CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
                IsActive
            )
            VALUES
            (
                @PName, @PTitle, @PQueryJson, @PColumns, N'{}',
                @PButtons, @PEvents,
                1, 1, @EntityFullName, @PActions,
                1, 1, @SeedUser, @SeedUser,
                @Now, @NowShamsi, @Now, @NowShamsi,
                1
            );
            SET @PId = SCOPE_IDENTITY();
        END

        UPDATE #Profiles SET SavedQueryId = @PId WHERE Name = @PName;
        FETCH NEXT FROM profile_cur INTO @PName, @PTitle, @PHts, @PActions, @PButtons, @PEvents, @PColumns, @PCustomQuery;
    END
    CLOSE profile_cur;
    DEALLOCATE profile_cur;

    ---------------------------------------------------------------------------
    PRINT N'=== [6/7] RoleAccess نمایه‌ها بر اساس ماتریس دید HTS ===';
    ---------------------------------------------------------------------------
    /* ماتریس (HTS GetOpenOrderRequestGridData + گرنت گروه‌ها روی 74/76/77 → نقش‌های سیستم جدید):
         Admin / FullAccess (گروه 17)                → ShowAllMenus, SupplyAndPurchase : همه نمایه‌ها
         ShowAll (7) (گروه 44: 74 Read/ShowAll, 77 Read) → Sup.OpenOrderRequest.ShowAll : همه فعال + پیشینه
         Industrial (OrgUnit 12؛ گروه 28: 74 Accept_Engineering/Start, 76 Full, 77 Read)
                                                     → Sup.OpenOrderRequest.Industrial, Industries : همه فعال، منتظر تایید مهندسی، تنظیمات، پیشینه
         Engineering (Accept_Engineering؛ گروه 70: 74 Accept_Engineering/Edit/HasEng, 77 Read)
                                                     → Sup.OpenOrderRequest.EngineeringAccept : همه فعال، منتظر تایید مهندسی، پیشینه
         HasEngineering (82) (گروه 462: 74 Read+HasEng؛ اعضا عمدتاً واحدهای مهندسی → شاخه isEngineering)
                                                     → Sup.OpenOrderRequest.HasEngineering : همه فعال
         SalesOrProject (187) (گروه 536)             → Sup.OpenOrderRequest.SalesOrProjectAccept : تأیید مهندسی شده
         Supply (OrgUnit 51/63؛ گروه 27: 74 Edit/Stop, 77 Full) → Sup.OpenOrderRequest.Supply : تأیید مهندسی شده، درخواست های من، پیشینه
         Config (گروه 430: 76 Full)                  → Sup.OpenOrderRequest.ConfigManage : تنظیمات
         Stop-only Read (گروه 593: 74 Read+Stop)     → Sup.OpenOrderRequest.Stop : مشاهده عمومی (شاخه else)
         Read-only (گروه‌های 442/172/303/33/60/350)   → Sup.OpenOrderRequest.View  (WP4 — اگر نبود رد می‌شود) : مشاهده عمومی
         History Read (گروه 395)                     → Sup.OpenOrderRequest.HistoryView (WP4 — اگر نبود رد می‌شود) : پیشینه
       بدون نمایه (فلگ فقط): Start, Sending, Query, Terminate, ConfigManageStaticPersonel, SupplierMenu.
       توجه: نقش‌هایی که RoleAccess مسیرهای /panel/sup/openorderrequest/{list,fetchdata} را ندارند (مثل Stop) تا اجرای WP4 عملاً لیست را نمی‌بینند. */
    IF OBJECT_ID('tempdb..#ProfileRole') IS NOT NULL DROP TABLE #ProfileRole;
    CREATE TABLE #ProfileRole (ProfileName NVARCHAR(100) NOT NULL, RoleName NVARCHAR(200) NOT NULL, PRIMARY KEY (ProfileName, RoleName));
    INSERT INTO #ProfileRole (ProfileName, RoleName) VALUES
    -- 32 تمامی درخواست ها (Admin / FullAccess)
    (N'openorderrequest_listinfo',              N'ShowAllMenus'),
    (N'openorderrequest_listinfo',              N'SupplyAndPurchase'),
    -- 102 همه فعال
    (N'vw_OpenOrderRequestAllActive',           N'Sup.OpenOrderRequest.ShowAll'),
    (N'vw_OpenOrderRequestAllActive',           N'Sup.OpenOrderRequest.Industrial'),
    (N'vw_OpenOrderRequestAllActive',           N'Industries'),
    (N'vw_OpenOrderRequestAllActive',           N'Sup.OpenOrderRequest.EngineeringAccept'),
    (N'vw_OpenOrderRequestAllActive',           N'Sup.OpenOrderRequest.HasEngineering'),
    (N'vw_OpenOrderRequestAllActive',           N'SupplyAndPurchase'),
    (N'vw_OpenOrderRequestAllActive',           N'ShowAllMenus'),
    -- 74 منتظر تایید مهندسی
    (N'openorderrequest_listinfonew',           N'Sup.OpenOrderRequest.EngineeringAccept'),
    (N'openorderrequest_listinfonew',           N'Sup.OpenOrderRequest.Industrial'),
    (N'openorderrequest_listinfonew',           N'Industries'),
    (N'openorderrequest_listinfonew',           N'SupplyAndPurchase'),
    (N'openorderrequest_listinfonew',           N'ShowAllMenus'),
    -- 72 تأیید مهندسی شده
    (N'vw_OpenOrderRequestEngineeringAccepted', N'Sup.OpenOrderRequest.SalesOrProjectAccept'),
    (N'vw_OpenOrderRequestEngineeringAccepted', N'Sup.OpenOrderRequest.Supply'),
    (N'vw_OpenOrderRequestEngineeringAccepted', N'SupplyAndPurchase'),
    (N'vw_OpenOrderRequestEngineeringAccepted', N'ShowAllMenus'),
    -- 141 درخواست های من
    (N'vw_OpenOrderRequestMyRequestes',         N'Sup.OpenOrderRequest.Supply'),
    (N'vw_OpenOrderRequestMyRequestes',         N'SupplyAndPurchase'),
    (N'vw_OpenOrderRequestMyRequestes',         N'ShowAllMenus'),
    -- 103 مشاهده عمومی (شاخه else)
    (N'vw_OpenOrderRequestOtherView',           N'Sup.OpenOrderRequest.Stop'),
    (N'vw_OpenOrderRequestOtherView',           N'Sup.OpenOrderRequest.View'),          -- WP4 / D39
    (N'vw_OpenOrderRequestOtherView',           N'SupplyAndPurchase'),
    (N'vw_OpenOrderRequestOtherView',           N'ShowAllMenus'),
    -- 99 تنظیمات
    (N'vw_openRequestConfig',                   N'Sup.OpenOrderRequest.ConfigManage'),
    (N'vw_openRequestConfig',                   N'Sup.OpenOrderRequest.Industrial'),
    (N'vw_openRequestConfig',                   N'Industries'),
    (N'vw_openRequestConfig',                   N'SupplyAndPurchase'),
    (N'vw_openRequestConfig',                   N'ShowAllMenus'),
    -- 73 پیشینه
    (N'vw_openOrderRequestPurchaseCompleted',   N'Sup.OpenOrderRequest.ShowAll'),
    (N'vw_openOrderRequestPurchaseCompleted',   N'Sup.OpenOrderRequest.Industrial'),
    (N'vw_openOrderRequestPurchaseCompleted',   N'Industries'),
    (N'vw_openOrderRequestPurchaseCompleted',   N'Sup.OpenOrderRequest.EngineeringAccept'),
    (N'vw_openOrderRequestPurchaseCompleted',   N'Sup.OpenOrderRequest.Supply'),
    (N'vw_openOrderRequestPurchaseCompleted',   N'Sup.OpenOrderRequest.HistoryView'),   -- WP4 / D39
    (N'vw_openOrderRequestPurchaseCompleted',   N'SupplyAndPurchase'),
    (N'vw_openOrderRequestPurchaseCompleted',   N'ShowAllMenus');

    -- نقش‌های غایب (هشدار — بعد از WP4 دوباره اجرا کنید)
    DECLARE @MissingRoles NVARCHAR(MAX) = (
        SELECT STRING_AGG(m.RoleName, N', ')
        FROM (SELECT DISTINCT pr.RoleName FROM #ProfileRole pr WHERE NOT EXISTS (SELECT 1 FROM system.Role r WHERE r.Name = pr.RoleName)) AS m);
    IF @MissingRoles IS NOT NULL
        PRINT N'  WARN: نقش‌های زیر در system.Role وجود ندارند و رد شدند: ' + @MissingRoles;

    -- وضعیت مطلوب
    IF OBJECT_ID('tempdb..#Desired') IS NOT NULL DROP TABLE #Desired;
    SELECT p.SavedQueryId, p.Name AS ProfileName, p.Title AS ProfileTitle, r.Id AS RoleId, r.Name AS RoleName
    INTO #Desired
    FROM #ProfileRole map
    INNER JOIN #Profiles p ON p.Name = map.ProfileName
    INNER JOIN system.Role r ON r.Name = map.RoleName;

    -- گزارش تفاوت با وضعیت فعلی
    DECLARE @Removed NVARCHAR(MAX) = (
        SELECT STRING_AGG(CAST(p.Name + N' ✗ ' + ISNULL(r.Name, N'RoleId=' + CAST(ra.RoleId AS NVARCHAR(20))) AS NVARCHAR(MAX)), N' | ')
        FROM system.RoleAccess ra
        INNER JOIN #Profiles p ON p.SavedQueryId = ra.RowId
        LEFT JOIN system.Role r ON r.Id = ra.RoleId
        WHERE ra.ActionAccessType = 3
          AND NOT EXISTS (SELECT 1 FROM #Desired d WHERE d.SavedQueryId = ra.RowId AND d.RoleId = ra.RoleId));
    DECLARE @Added NVARCHAR(MAX) = (
        SELECT STRING_AGG(CAST(d.ProfileName + N' + ' + d.RoleName AS NVARCHAR(MAX)), N' | ')
        FROM #Desired d
        WHERE NOT EXISTS (SELECT 1 FROM system.RoleAccess ra WHERE ra.ActionAccessType = 3 AND ra.RowId = d.SavedQueryId AND ra.RoleId = d.RoleId));
    PRINT N'  RoleAccess حذف‌شده: ' + ISNULL(@Removed, N'(هیچ)');
    PRINT N'  RoleAccess افزوده: '  + ISNULL(@Added,   N'(هیچ)');

    DELETE ra
    FROM system.RoleAccess ra
    INNER JOIN #Profiles p ON p.SavedQueryId = ra.RowId
    WHERE ra.ActionAccessType = 3;
    PRINT N'  ردیف‌های قبلی DataProfile RoleAccess پاک شد: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    INSERT INTO system.RoleAccess
    (
        Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName,
        EntityId, RowId, RoleId, CreatedById, ModifiedById, CreatedByName, ModifiedByName,
        CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
        IsActive
    )
    SELECT
        N'dataProfile_' + CAST(d.SavedQueryId AS NVARCHAR(20)),
        3,                        -- ActionAccessType.DataProfile
        7,                        -- ActionAccessItemType «نمایه داده» (مطابق ردیف‌های زنده)
        @EntityFullNamePascal,
        d.ProfileTitle,
        NULL,
        d.SavedQueryId,
        d.RoleId,
        1, 1, @SeedUser, @SeedUser,
        @Now, @NowShamsi, @Now, @NowShamsi,
        1
    FROM #Desired d;
    PRINT N'  ردیف‌های DataProfile RoleAccess درج شد: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    ---------------------------------------------------------------------------
    PRINT N'=== [7/7] Hygiene — نمایه‌های دیگر روی همین موجودیت ===';
    ---------------------------------------------------------------------------
    /* هیچ‌کدام از ۸ نمایه بالا تکراری دقیق نیست (32 ⊇ 102 ∪ 73 از نظر ردیف، ولی هدف/نقش/دکمه‌ها متفاوت است) → غیرفعال نمی‌شوند.
       نمایه‌های راهنمای Data\Scripts\OpenOrderRequest_DataProfiles_Guide.html (OOR_*) هرگز در DB ساخته نشدند.
       هر نمایه فعال دیگری روی entities.app.sup.openorderrequest که در کاتالوگ نیست فقط گزارش می‌شود؛
       با @DeactivateUnknownProfiles = 1 غیرفعال (IsActive = 0) می‌شود — هرگز DELETE. */
    DECLARE @Unknown NVARCHAR(MAX) = (
        SELECT STRING_AGG(CAST(N'Id=' + CAST(q.Id AS NVARCHAR(20)) + N' ' + q.Name + N' («' + q.Title + N'»)' AS NVARCHAR(MAX)), N' | ')
        FROM system.SavedQuery q
        WHERE q.Type = 1 AND q.IsActive = 1
          AND LOWER(q.EntityFullName) = @EntityFullName
          AND NOT EXISTS (SELECT 1 FROM #Profiles p WHERE p.Name = q.Name));
    IF @Unknown IS NULL
        PRINT N'  نمایه ناشناخته‌ای وجود ندارد.';
    ELSE
    BEGIN
        PRINT N'  نمایه‌های فعال خارج از کاتالوگ: ' + @Unknown;
        IF @DeactivateUnknownProfiles = 1
        BEGIN
            UPDATE q
            SET IsActive = 0,
                ModifiedById = 1,
                ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now,
                ModifiedDateShamsiDateTime = @NowShamsi
            FROM system.SavedQuery q
            WHERE q.Type = 1 AND q.IsActive = 1
              AND LOWER(q.EntityFullName) = @EntityFullName
              AND NOT EXISTS (SELECT 1 FROM #Profiles p WHERE p.Name = q.Name);
            PRINT N'  غیرفعال شد (IsActive=0): ' + CAST(@@ROWCOUNT AS NVARCHAR(20));
        END
        ELSE
            PRINT N'  (فقط گزارش — برای غیرفعال‌کردن @DeactivateUnknownProfiles = 1)';
    END

    IF @DryRun = 1
    BEGIN
        ROLLBACK TRANSACTION;
        PRINT N'=== DryRun: همه تغییرات ROLLBACK شد. برای اعمال @DryRun = 0 ===';
    END
    ELSE
    BEGIN
        COMMIT TRANSACTION;
        PRINT N'=== DONE: Update_OpenOrderRequest_DataProfiles_HtsParity committed — WebApp را ری‌استارت کنید (کش نمایه‌ها) ===';
    END
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    PRINT N'=== ERROR: Update_OpenOrderRequest_DataProfiles_HtsParity rolled back ===';
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

-- بازبینی (خارج از تراکنش؛ در DryRun وضعیت فعلی DB را نشان می‌دهد)
SELECT q.Id, q.Name, q.Title, q.Mode, q.Type, q.IsActive,
       (SELECT COUNT(*) FROM OPENJSON(q.ColumnsJson)) AS ColumnCount,
       CASE WHEN j.CustomQuery LIKE N'%INNER JOIN%' THEN 1 ELSE 0 END AS HasInnerJoin,
       CASE WHEN j.CustomQuery LIKE N'%OUTER APPLY%' THEN 1 ELSE 0 END AS HasOuterApply,
       (SELECT COUNT(*) FROM system.RoleAccess ra WHERE ra.ActionAccessType = 3 AND ra.RowId = q.Id) AS RoleAccessRows
FROM system.SavedQuery q
OUTER APPLY OPENJSON(q.QueryJson) WITH (CustomQuery NVARCHAR(MAX) '$.CustomQuery') AS j
WHERE q.Type = 1 AND LOWER(q.EntityFullName) = N'entities.app.sup.openorderrequest'
ORDER BY q.Id;

SELECT ra.RowId AS SavedQueryId, q.Name AS ProfileName, r.Id AS RoleId, r.Name AS RoleName, ra.Path, ra.ActionAccessItemType
FROM system.RoleAccess ra
INNER JOIN system.SavedQuery q ON q.Id = ra.RowId
LEFT JOIN system.Role r ON r.Id = ra.RoleId
WHERE ra.ActionAccessType = 3
  AND LOWER(q.EntityFullName) = N'entities.app.sup.openorderrequest'
ORDER BY ra.RowId, r.Name;
