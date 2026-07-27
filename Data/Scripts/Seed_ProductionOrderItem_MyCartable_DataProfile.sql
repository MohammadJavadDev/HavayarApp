/*
================================================================================
نمایه داده «کارتابل من» برای اقلام سفارش ساخت (Sale.ProductionOrderItem)
================================================================================
هدف:
  یک SavedQuery از نوع DataProfile با Name = 'POI_MyCartable' می‌سازد/به‌روزرسانی
  می‌کند که برای هر کاربر لاگین‌شده، تجمیع تمام رکوردهایی را نشان می‌دهد که «منتظر
  اقدام همان کاربر» هستند؛ دقیقاً معادل کارتابل‌های ترکیبی سیستم قدیم (HTS).

  تشخیص اقلام منتظر اقدام صرفاً بر مبنای Roleهای واقعی کاربر (system.User.RoleIds)
  و در یک مورد بر مبنای شناسه شخص جاری (Sale.ProductionOrder.ProjectManagerId)
  انجام می‌شود - دقیقاً همان منطقی که در ProductionOrderItemController برای مجاز
  بودن هر اکشن (AcceptIndustrial/AcceptEngineering/AcceptProjectManager/...)
  استفاده شده است.

  چون عملیات صفحه سرفصل سفارش ساخت (تایید مالی) به صفحه اقلام منتقل شده، ردیف‌های
  اقلامِ سفارش‌های «منتظر تایید مالی» (State=2) هم برای نقش
  Sale.ProductionOrder.FinancialConfirm در همین کارتابل لحاظ می‌شوند.

منبع پایه: SavedQuery Id = 104 (productionorderitem_listinfonew) - ستون‌ها/Joinها
           عیناً از آن کپی می‌شوند و فقط CustomQuery با یک بخش DECLARE (تشخیص
           نقش‌های کاربر جاری) و یک WHERE جدید (منطق کارتابل) تکمیل می‌شود.

نکته اجرایی: مقدار «--!--mainsection» یک Marker داخلی است که در
  DatabaseSchemaService.ExecuteQueryWithPaginationAsync برای جداسازی بخش DECLARE
  (که نباید داخل زیرکوئری Pagination قرار گیرد) از SELECT اصلی استفاده می‌شود.

این اسکریپت کاملاً Idempotent است: با نام نمایه (Name) و نام Roleها Upsert می‌کند
و می‌توان چندبار اجرا کرد. نمایه‌های داده قبلی (104، 107 تا 117) دست‌نخورده باقی
می‌مانند.
================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

BEGIN TRY

    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-poi-mycartable';

    -----------------------------------------------------------------------------
    -- 1) بازیابی نمایه پایه (Id = 104) برای استخراج ستون‌ها/Joinهای آماده
    -----------------------------------------------------------------------------
    DECLARE @BaseId BIGINT = 104;
    DECLARE @BaseQueryJson NVARCHAR(MAX);

    SELECT @BaseQueryJson = QueryJson
    FROM system.SavedQuery
    WHERE Id = @BaseId;

    IF @BaseQueryJson IS NULL
        THROW 51000, N'نمایه پایه (SavedQuery Id=104) یافت نشد؛ عملیات متوقف شد.', 1;

    DECLARE @BaseCustomQuery NVARCHAR(MAX) = JSON_VALUE(@BaseQueryJson, '$.CustomQuery');

    IF @BaseCustomQuery IS NULL OR LEN(@BaseCustomQuery) = 0
        THROW 51001, N'CustomQuery نمایه پایه (Id=104) خالی است؛ عملیات متوقف شد.', 1;

    -- رفع اشکالِ از پیش موجود در ۱۰۴/کپی‌های آن: ستون «نام مدیر پروژه» به‌اشتباه
    -- به Alias کارشناس فروش (t6) متصل بود؛ در نمایه جدید به‌درستی به t9 وصل می‌شود.
    SET @BaseCustomQuery = REPLACE(@BaseCustomQuery, N'[t6].[Name] AS [t9_Name]', N'[t9].[Name] AS [t9_Name]');

    -----------------------------------------------------------------------------
    -- 2) شناسه Roleهای مرتبط با هر کارتابل (باید از قبل موجود باشند - این اسکریپت
    --    Role جدید نمی‌سازد، فقط از Roleهای ایجادشده در Seed_ProductionOrderItem_DataProfiles
    --    استفاده می‌کند)
    -----------------------------------------------------------------------------
    DECLARE @RoleShowAll         BIGINT = 200020; -- Sale.ProductionOrder.ShowAll
    DECLARE @RoleFinancial       BIGINT = 200021; -- Sale.ProductionOrder.FinancialConfirm
    DECLARE @RoleIndustrial      BIGINT = 200023; -- Sale.ProductionOrderItem.AcceptIndustrial
    DECLARE @RoleEngMechanical   BIGINT = 200024; -- Sale.ProductionOrderItem.AcceptEngineeringMechanical
    DECLARE @RoleEngElectrical   BIGINT = 200025; -- Sale.ProductionOrderItem.AcceptEngineeringElectrical
    DECLARE @RoleProjectManager  BIGINT = 200026; -- Sale.ProductionOrderItem.AcceptProjectManager
    DECLARE @RoleInquirer        BIGINT = 200027; -- Sale.ProductionOrderItem.Inquirer
    DECLARE @RoleMinistry        BIGINT = 200028; -- Sale.ProductionOrderItem.MinistryOfIndustryInquirer
    DECLARE @RoleSupplyCommittee BIGINT = 200029; -- Sale.ProductionOrderItem.SupplyCommitteeBoss

    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Id IN (
            @RoleShowAll, @RoleFinancial, @RoleIndustrial, @RoleEngMechanical, @RoleEngElectrical,
            @RoleProjectManager, @RoleInquirer, @RoleMinistry, @RoleSupplyCommittee)
            HAVING COUNT(*) = 9)
        THROW 51002, N'برخی از Roleهای پایه (200020..200029) یافت نشدند. ابتدا Seed_ProductionOrderItem_DataProfiles.sql را اجرا کنید.', 1;

    -----------------------------------------------------------------------------
    -- 3) ساخت بخش DECLARE پویا (تشخیص نقش‌های کاربر جاری از system.[User])
    --    فقط دو مقدار واقعاً توسط ParameterResolverService پر می‌شوند: @CurrentUserId
    --    و @CurrentUserName. بقیه (نقش/واحد سازمانی) با Join به system.[User] از
    --    همان @CurrentUserId استخراج می‌شوند.
    -----------------------------------------------------------------------------
    DECLARE @DeclareBlock NVARCHAR(MAX) = N'
DECLARE @CuId BIGINT = TRY_CAST(@CurrentUserId AS BIGINT);
DECLARE @CuRoles NVARCHAR(MAX);
DECLARE @CuRoleIds NVARCHAR(MAX);
SELECT @CuRoles = Roles, @CuRoleIds = RoleIds FROM system.[User] WHERE Id = @CuId;

DECLARE @IsAdmin BIT = CASE WHEN EXISTS (SELECT 1 FROM OPENJSON(@CuRoles) WHERE [value] = N''admin'') THEN 1 ELSE 0 END;
DECLARE @HasShowAll BIT = CASE WHEN @IsAdmin = 1 OR EXISTS (SELECT 1 FROM OPENJSON(@CuRoleIds) WHERE TRY_CAST([value] AS BIGINT) = ' + CAST(@RoleShowAll AS NVARCHAR(20)) + N') THEN 1 ELSE 0 END;
DECLARE @HasFinancial BIT = CASE WHEN EXISTS (SELECT 1 FROM OPENJSON(@CuRoleIds) WHERE TRY_CAST([value] AS BIGINT) = ' + CAST(@RoleFinancial AS NVARCHAR(20)) + N') THEN 1 ELSE 0 END;
DECLARE @HasIndustrial BIT = CASE WHEN EXISTS (SELECT 1 FROM OPENJSON(@CuRoleIds) WHERE TRY_CAST([value] AS BIGINT) = ' + CAST(@RoleIndustrial AS NVARCHAR(20)) + N') THEN 1 ELSE 0 END;
DECLARE @HasEngMech BIT = CASE WHEN EXISTS (SELECT 1 FROM OPENJSON(@CuRoleIds) WHERE TRY_CAST([value] AS BIGINT) = ' + CAST(@RoleEngMechanical AS NVARCHAR(20)) + N') THEN 1 ELSE 0 END;
DECLARE @HasEngElec BIT = CASE WHEN EXISTS (SELECT 1 FROM OPENJSON(@CuRoleIds) WHERE TRY_CAST([value] AS BIGINT) = ' + CAST(@RoleEngElectrical AS NVARCHAR(20)) + N') THEN 1 ELSE 0 END;
DECLARE @HasPM BIT = CASE WHEN EXISTS (SELECT 1 FROM OPENJSON(@CuRoleIds) WHERE TRY_CAST([value] AS BIGINT) = ' + CAST(@RoleProjectManager AS NVARCHAR(20)) + N') THEN 1 ELSE 0 END;
DECLARE @HasInquirer BIT = CASE WHEN EXISTS (SELECT 1 FROM OPENJSON(@CuRoleIds) WHERE TRY_CAST([value] AS BIGINT) = ' + CAST(@RoleInquirer AS NVARCHAR(20)) + N') THEN 1 ELSE 0 END;
DECLARE @HasMinistry BIT = CASE WHEN EXISTS (SELECT 1 FROM OPENJSON(@CuRoleIds) WHERE TRY_CAST([value] AS BIGINT) = ' + CAST(@RoleMinistry AS NVARCHAR(20)) + N') THEN 1 ELSE 0 END;
DECLARE @HasCommittee BIT = CASE WHEN EXISTS (SELECT 1 FROM OPENJSON(@CuRoleIds) WHERE TRY_CAST([value] AS BIGINT) = ' + CAST(@RoleSupplyCommittee AS NVARCHAR(20)) + N') THEN 1 ELSE 0 END;
--!--mainsection
';

    -----------------------------------------------------------------------------
    -- 4) WHERE نهایی کارتابل - بر اساس CheckStatus/ProductionStep واقعی و اکشن‌های
    --    مجاز در ProductionOrderItemController (نگاشت کامل در کامنت‌های زیر)
    -----------------------------------------------------------------------------
    DECLARE @WhereClause NVARCHAR(MAX) = N'
WHERE
    [t1].[IsDeleted] = 0
    AND [t1].[IsLatestVersion] = 1
    AND (
        -- کارتابل صنایع (داخلی ۱۹۰۴ / خارجی ۱۹۰۵): AcceptIndustrial کلید CheckStatus را
        -- تغییر نمی‌دهد، فقط ProductionStep را به AwaitingProduction(214) می‌برد؛ بنابراین
        -- فقط اقلامی که هنوز مسیریابی‌نشده (ProductionStep=0) یا صراحتاً در انتظار صنایع
        -- (ProductionStep=2744) هستند واقعاً «در انتظار اقدام» محسوب می‌شوند (نه هزاران قلم
        -- قدیمی که سال‌ها پیش تحویل/ارسال شده‌اند ولی هنوز CheckStatus=1904 دارند).
        -- طبق SendToInquiry/SendToMinistryReview/SendToSupplyCommittee، صنایع، استعلام،
        -- وزارت صنایع و رئیس کمیته هرکدام می‌توانند از همین وضعیت اقدام کنند. Admin/ShowAll
        -- نیز همین مجموعه (نه کل اقلام فعال) را می‌بیند.
        (
            (@HasShowAll = 1 OR @HasIndustrial = 1 OR @HasInquirer = 1 OR @HasMinistry = 1 OR @HasCommittee = 1)
            AND [t1].[CheckStatus] IN (1904, 1905)
            AND ([t1].[ProductionStep] = 2744 OR [t1].[ProductionStep] = 0)
        )

        -- کارتابل مهندسی مکانیک: CheckStatus=2195 با DeviceType غیر برقی
        OR (
            (@HasShowAll = 1 OR @HasEngMech = 1)
            AND [t1].[CheckStatus] = 2195
            AND ([t1].[DeviceType] IS NULL OR [t1].[DeviceType] NOT IN (2403, 2404, 2212, 2211))
        )

        -- کارتابل مهندسی برق: CheckStatus=2195 با DeviceType برقی (تابلو کنترل/برق/اینورتر/سکوئنسر)
        OR (
            (@HasShowAll = 1 OR @HasEngElec = 1)
            AND [t1].[CheckStatus] = 2195
            AND [t1].[DeviceType] IN (2403, 2404, 2212, 2211)
        )

        -- کارتابل مدیر پروژه: نقش عمومی AcceptProjectManager یا مدیر پروژه واقعیِ همان
        -- سفارش ساخت (Sale.ProductionOrder.ProjectManagerId = شناسه شخص جاری)
        OR (
            (@HasShowAll = 1 OR @HasPM = 1 OR [t2].[ProjectManagerId] = @CuId)
            AND [t1].[CheckStatus] = 2258
        )

        -- نیاز به استعلام (۱۹۰۶): طبق SendToSupplyCommittee، استعلام‌گر/وزارت صنایع/رئیس
        -- کمیته هرکدام می‌توانند اقدام بعدی را انجام دهند
        OR (
            (@HasShowAll = 1 OR @HasInquirer = 1 OR @HasMinistry = 1 OR @HasCommittee = 1)
            AND [t1].[CheckStatus] = 1906
        )

        -- نیاز به بررسی وزارت صنایع (۱۹۰۸): طبق SendToSupplyCommittee
        OR (
            (@HasShowAll = 1 OR @HasMinistry = 1 OR @HasInquirer = 1 OR @HasCommittee = 1)
            AND [t1].[CheckStatus] = 1908
        )

        -- در کارتابل رئیس کمیته تامین (۱۹۰۹ / ۲۲۰۲): فقط CommitteeAccept/CommitteeReject
        -- که منحصراً نیازمند SupplyCommitteeBoss است
        OR (
            (@HasShowAll = 1 OR @HasCommittee = 1)
            AND [t1].[CheckStatus] IN (1909, 2202)
        )

        -- سرفصل سفارش ساخت منتظر تایید مالی (State=2/FinancialUnitReview) - این عملیات از
        -- صفحه سرفصل به صفحه اقلام منتقل شده، بنابراین اقلام همان سفارش‌ها اینجا نمایش داده می‌شود
        OR (
            (@HasShowAll = 1 OR @HasFinancial = 1)
            AND [t2].[State] = 2
        )
    )';

    DECLARE @FinalCustomQuery NVARCHAR(MAX) = @DeclareBlock + @BaseCustomQuery + @WhereClause;

    -----------------------------------------------------------------------------
    -- 5) جایگزینی CustomQuery در QueryJson پایه (بدون دستکاری دستی رشته JSON - از
    --    JSON_MODIFY استفاده می‌شود تا Escaping به‌طور خودکار و صحیح انجام شود)
    -----------------------------------------------------------------------------
    DECLARE @FinalQueryJson NVARCHAR(MAX) = JSON_MODIFY(@BaseQueryJson, '$.CustomQuery', @FinalCustomQuery);

    -----------------------------------------------------------------------------
    -- 6) Upsert نمایه SavedQuery با Name = 'POI_MyCartable'
    -----------------------------------------------------------------------------
    DECLARE @ProfileName  NVARCHAR(100) = N'POI_MyCartable';
    DECLARE @ProfileTitle NVARCHAR(200) = N'کارتابل من — اقلام سفارش ساخت';
    DECLARE @ExistingId   BIGINT;

    SELECT @ExistingId = Id FROM system.SavedQuery WHERE Name = @ProfileName;

    IF @ExistingId IS NULL
    BEGIN
        INSERT INTO system.SavedQuery
            (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
             Mode, Type, EntityFullName, ActionOptions,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
             IsActive)
        SELECT
            @ProfileName, @ProfileTitle, @FinalQueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
            Mode, Type, EntityFullName, ActionOptions,
            1, 1, @SeedUser, @SeedUser,
            @Now, @NowShamsi, @Now, @NowShamsi,
            1
        FROM system.SavedQuery
        WHERE Id = @BaseId;

        SET @ExistingId = SCOPE_IDENTITY();
        PRINT N'نمایه POI_MyCartable ایجاد شد. Id=' + CAST(@ExistingId AS NVARCHAR(20));
    END
    ELSE
    BEGIN
        UPDATE q
        SET q.QueryJson                   = @FinalQueryJson,
            q.Title                       = @ProfileTitle,
            q.ColumnsJson                 = b.ColumnsJson,
            q.DiagramJson                 = b.DiagramJson,
            q.CustomActionButtonsJson     = b.CustomActionButtonsJson,
            q.EventScriptsJson            = b.EventScriptsJson,
            q.ActionOptions               = b.ActionOptions,
            q.EntityFullName              = b.EntityFullName,
            q.ModifiedById                = 1,
            q.ModifiedByName              = @SeedUser,
            q.ModifiedDateMiladiDateTime  = @Now,
            q.ModifiedDateShamsiDateTime  = @NowShamsi,
            q.IsActive                    = 1
        FROM system.SavedQuery q
        CROSS JOIN (SELECT ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson, ActionOptions, EntityFullName FROM system.SavedQuery WHERE Id = @BaseId) b
        WHERE q.Id = @ExistingId;

        PRINT N'نمایه POI_MyCartable از قبل موجود بود؛ به‌روزرسانی شد. Id=' + CAST(@ExistingId AS NVARCHAR(20));
    END

    -----------------------------------------------------------------------------
    -- 7) RoleAccess برای نقش‌هایی که در منطق کارتابل بالا دارای شاخه فعال هستند
    --    (فقط اگر برای همین RoleId + SavedQueryId قبلاً ثبت نشده باشد)
    -----------------------------------------------------------------------------
    DECLARE @CartableRoles TABLE (RoleId BIGINT);
    INSERT INTO @CartableRoles (RoleId) VALUES
        (@RoleShowAll),
        (@RoleFinancial),
        (@RoleIndustrial),
        (@RoleEngMechanical),
        (@RoleEngElectrical),
        (@RoleProjectManager),
        (@RoleInquirer),
        (@RoleMinistry),
        (@RoleSupplyCommittee);

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
         IsActive)
    SELECT
        NULL, 3, 7, N'Entities.App.Sale.ProductionOrderItem', @ProfileTitle, NULL, @ExistingId, r.RoleId,
        1, 1, @SeedUser, @SeedUser,
        @Now, @NowShamsi, @Now, @NowShamsi,
        1
    FROM @CartableRoles r
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess ra
        WHERE ra.RoleId = r.RoleId
          AND ra.ActionAccessType = 3
          AND ra.RowId = @ExistingId
    );

    PRINT N'RoleAccess نمایه POI_MyCartable برای Roleهای کارتابل بررسی/تکمیل شد.';

    COMMIT TRANSACTION;
    PRINT N'--- پایان موفق Seed_ProductionOrderItem_MyCartable_DataProfile ---';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH

-----------------------------------------------------------------------------
-- تایید نتیجه
-----------------------------------------------------------------------------
SELECT Id, Name, Title, EntityFullName, Mode, Type, IsActive
FROM system.SavedQuery
WHERE Name = 'POI_MyCartable';

SELECT ra.Id, ra.RoleId, r.Name AS RoleName, ra.ActionAccessType, ra.ActionAccessItemType, ra.RowId
FROM system.RoleAccess ra
JOIN system.Role r ON r.Id = ra.RoleId
WHERE ra.RowId = (SELECT Id FROM system.SavedQuery WHERE Name = 'POI_MyCartable')
  AND ra.ActionAccessType = 3
ORDER BY ra.RoleId;
