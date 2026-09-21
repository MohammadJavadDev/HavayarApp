/*
  Seed_SaleCollectionClaim_DataProfiles.sql
  نمایه وصول مطالبات و فیش وصول مطابق گرید HTS صفحه Sale_CollectionClaim.
  ColumnName = Alliance (حالت Query).
  Idempotent — safe to re-run.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-sale-collectionclaim-dataprofiles';
    DECLARE @QueryJsonSkeleton NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}';

    -----------------------------------------------------------------------------
    PRINT N'=== [1] Backfill CollectionClaim.DlId from TMS Acc_DL / FIN.DL ===';
    -----------------------------------------------------------------------------
    UPDATE t
    SET t.DlId = dl.Id,
        t.ModifiedById = 1,
        t.ModifiedByName = @SeedUser,
        t.ModifiedDateMiladiDateTime = @Now,
        t.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Sale.CollectionClaim t
    INNER JOIN [TMS].[TotalSystem].[dbo].[Sale_CollectionClaim] s ON s.Id = t.HtsId AND t.HtsId <> 0
    INNER JOIN FIN.DL dl ON dl.HamkaranId = s.Dl_FK AND dl.HamkaranId <> 0
    WHERE t.DlId IS NULL OR t.DlId = 0;
    PRINT N'  DlId backfilled: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    -----------------------------------------------------------------------------
    PRINT N'=== [2] Upsert DataProfiles ===';
    -----------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#CcProfiles') IS NOT NULL DROP TABLE #CcProfiles;
    CREATE TABLE #CcProfiles
    (
        Name         NVARCHAR(100) NOT NULL PRIMARY KEY,
        Title        NVARCHAR(200) NOT NULL,
        EntityLower  NVARCHAR(200) NOT NULL,
        ColumnsJson  NVARCHAR(MAX) NOT NULL,
        CustomQuery  NVARCHAR(MAX) NOT NULL,
        ActionOptions NVARCHAR(MAX) NOT NULL,
        CustomButtons NVARCHAR(MAX) NOT NULL
    );

    INSERT INTO #CcProfiles (Name, Title, EntityLower, ColumnsJson, CustomQuery, ActionOptions, CustomButtons) VALUES
        (N'AfterSales_CollectionClaim_List', N'وصول مطالبات', N'entities.app.sale.collectionclaim',
         N'[{"TableName":"Sale.CollectionClaim","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":1,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"SLS.Customer","ColumnName":"t2_Code","DisplayName":"کد مشتری","Alliance":"t2_Code","Address":"[t2].[Code]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"FIN.DL","ColumnName":"tDl_Code","DisplayName":"کد تفضیل","Alliance":"tDl_Code","Address":"COALESCE([t4].[Code], CAST([t4].[HamkaranId] AS nvarchar(64)), CAST([t1].[DlId] AS nvarchar(64)))","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Gnr.Party","ColumnName":"t3_FullName","DisplayName":"مشتری","Alliance":"t3_FullName","Address":"[t3].[FullName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":250,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Gnr.Region","ColumnName":"tProv_Name","DisplayName":"استان","Alliance":"tProv_Name","Address":"COALESCE([tProvCust].[Name], [tProvAddr].[Name])","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.CollectionClaim","ColumnName":"t1_VchNumber","DisplayName":"شماره سند","Alliance":"t1_VchNumber","Address":"[t1].[VchNumber]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.CollectionClaim","ColumnName":"t1_VchShamsiDate","DisplayName":"تاریخ","Alliance":"t1_VchShamsiDate","Address":"[t1].[VchShamsiDate]","SystemTypeName":"DateShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.CollectionClaim","ColumnName":"t1_Debit","DisplayName":"بدهکار","Alliance":"t1_Debit","Address":"[t1].[Debit]","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.CollectionClaim","ColumnName":"t1_Credit","DisplayName":"بستانکار","Alliance":"t1_Credit","Address":"[t1].[Credit]","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.CollectionClaimPayFish","ColumnName":"tFish_CollectedPrice","DisplayName":"وصول شده","Alliance":"tFish_CollectedPrice","Address":"ISNULL([tFish].[CollectedPrice], CAST(0 AS bigint))","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.CollectionClaim","ColumnName":"tFish_RemainingDebt","DisplayName":"مانده بدهی","Alliance":"tFish_RemainingDebt","Address":"[t1].[Debit] - ISNULL([tFish].[CollectedPrice], CAST(0 AS bigint))","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.CollectionClaim","ColumnName":"t1_VchDescription","DisplayName":"شرح فاکتور","Alliance":"t1_VchDescription","Address":"[t1].[VchDescription]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":300,"Filterable":true,"Sortable":true,"ClassName":""}]', N'SELECT
    [t1].[Id] AS [t1_Id],
    [t2].[Code] AS [t2_Code],
    COALESCE([t4].[Code], CAST([t4].[HamkaranId] AS nvarchar(64)), CAST([t1].[DlId] AS nvarchar(64))) AS [tDl_Code],
    [t3].[FullName] AS [t3_FullName],
    COALESCE([tProvCust].[Name], [tProvAddr].[Name]) AS [tProv_Name],
    [t1].[VchNumber] AS [t1_VchNumber],
    [t1].[VchShamsiDate] AS [t1_VchShamsiDate],
    [t1].[Debit] AS [t1_Debit],
    [t1].[Credit] AS [t1_Credit],
    ISNULL([tFish].[CollectedPrice], CAST(0 AS bigint)) AS [tFish_CollectedPrice],
    [t1].[Debit] - ISNULL([tFish].[CollectedPrice], CAST(0 AS bigint)) AS [tFish_RemainingDebt],
    [t1].[VchDescription] AS [t1_VchDescription]
FROM [Sale].[CollectionClaim] AS [t1]
LEFT JOIN [SLS].[Customer] AS [t2] ON [t1].[CustomerId] = [t2].[Id]
LEFT JOIN [Gnr].[Party] AS [t3] ON [t2].[PartyId] = [t3].[Id]
LEFT JOIN [FIN].[DL] AS [t4] ON [t1].[DlId] = [t4].[Id]
LEFT JOIN [Gnr].[Region] AS [tProvCust] ON [t2].[ProvinceId] = [tProvCust].[Id]
OUTER APPLY (
    SELECT TOP (1) [addr].[ProvinceId]
    FROM [SLS].[CustomerAddress] AS [addr]
    WHERE [addr].[CustomerId] = [t1].[CustomerId]
    ORDER BY [addr].[Id]
) AS [tAddr]
LEFT JOIN [Gnr].[Region] AS [tProvAddr] ON [tAddr].[ProvinceId] = [tProvAddr].[Id]
OUTER APPLY (
    SELECT SUM([pf].[CollectedPrice]) AS [CollectedPrice]
    FROM [Sale].[CollectionClaimPayFish] AS [pf]
    WHERE [pf].[CollectionClaimId] = [t1].[Id]
) AS [tFish]', N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]', N'[{"id":"as_cc_fish","title":"فیش‌های وصول","dataActionName":"payFish","colorClass":"btn-color-warning","iconClass":"ki-wallet ki-outline","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    appController.addPage(''/Panel/Sale/CollectionClaimPayFish/List?collectionClaimId='' + id, true, ''فیش‌های وصول'');\n}"}]'),
        (N'AfterSales_CollectionClaimPayFish_List', N'فیش وصول', N'entities.app.sale.collectionclaimpayfish',
         N'[{"TableName":"Sale.CollectionClaimPayFish","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":1,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.CollectionClaimPayFish","ColumnName":"t1_CollectedPrice","DisplayName":"وصول شده","Alliance":"t1_CollectedPrice","Address":"[t1].[CollectedPrice]","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.CollectionClaimPayFish","ColumnName":"t1_PayFishShamsiDate","DisplayName":"تاریخ تقریبی وصول","Alliance":"t1_PayFishShamsiDate","Address":"[t1].[PayFishShamsiDate]","SystemTypeName":"DateShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Gnr.Party","ColumnName":"tResp_Name","DisplayName":"مسئول","Alliance":"tResp_Name","Address":"COALESCE([t3].[FullName], [t2].[NameFa], [t2].[Username])","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.CollectionClaimPayFish","ColumnName":"t1_FishNumber","DisplayName":"شماره فیش","Alliance":"t1_FishNumber","Address":"[t1].[FishNumber]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.CollectionClaimPayFish","ColumnName":"t1_Uncollectable","DisplayName":"غیرقابل وصول","Alliance":"t1_Uncollectable","Address":"[t1].[Uncollectable]","SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.CollectionClaimPayFish","ColumnName":"t1_HasDiscount","DisplayName":"تخفیف","Alliance":"t1_HasDiscount","Address":"[t1].[HasDiscount]","SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.CollectionClaimPayFish","ColumnName":"t1_IsAgency","DisplayName":"نمایندگی","Alliance":"t1_IsAgency","Address":"[t1].[IsAgency]","SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.CollectionClaimPayFish","ColumnName":"t1_Comment","DisplayName":"توضیحات","Alliance":"t1_Comment","Address":"[t1].[Comment]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":350,"Filterable":true,"Sortable":true,"ClassName":""}]', N'SELECT
    [t1].[Id] AS [t1_Id],
    [t1].[CollectedPrice] AS [t1_CollectedPrice],
    [t1].[PayFishShamsiDate] AS [t1_PayFishShamsiDate],
    COALESCE([t3].[FullName], [t2].[NameFa], [t2].[Username]) AS [tResp_Name],
    [t1].[FishNumber] AS [t1_FishNumber],
    [t1].[Uncollectable] AS [t1_Uncollectable],
    [t1].[HasDiscount] AS [t1_HasDiscount],
    [t1].[IsAgency] AS [t1_IsAgency],
    [t1].[Comment] AS [t1_Comment]
FROM [Sale].[CollectionClaimPayFish] AS [t1]
LEFT JOIN [system].[User] AS [t2] ON [t1].[ResponsibleId] = [t2].[Id]
LEFT JOIN [Gnr].[Party] AS [t3] ON [t2].[PartyId] = [t3].[Id]', N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]', N'[{"id":"as_edit_collectionclaimpayfish","title":"ویرایش","dataActionName":"editRow","colorClass":"btn-color-primary","iconClass":"ki-pencil ki-outline","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    appController.addPage(''/Panel/Sale/CollectionClaimPayFish/Edit?id='' + id, true, ''ویرایش'');\n}"}]');

    DECLARE @PName NVARCHAR(100), @PTitle NVARCHAR(200), @PEnt NVARCHAR(200);
    DECLARE @PCols NVARCHAR(MAX), @PSelect NVARCHAR(MAX), @PAct NVARCHAR(MAX), @PBtn NVARCHAR(MAX);
    DECLARE @PQueryJson NVARCHAR(MAX), @PId BIGINT;

    DECLARE cc_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT Name, Title, EntityLower, ColumnsJson, CustomQuery, ActionOptions, CustomButtons FROM #CcProfiles;
    OPEN cc_cur;
    FETCH NEXT FROM cc_cur INTO @PName, @PTitle, @PEnt, @PCols, @PSelect, @PAct, @PBtn;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @PQueryJson = JSON_MODIFY(@QueryJsonSkeleton, '$.CustomQuery', @PSelect);

        IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @PName)
        BEGIN
            UPDATE system.SavedQuery
            SET Title = @PTitle,
                QueryJson = @PQueryJson,
                ColumnsJson = @PCols,
                EntityFullName = @PEnt,
                Mode = 1,
                Type = 1,
                ActionOptions = @PAct,
                CustomActionButtonsJson = @PBtn,
                DiagramJson = N'{}',
                ModifiedById = 1,
                ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now,
                ModifiedDateShamsiDateTime = @NowShamsi,
                IsActive = 1
            WHERE Name = @PName;
            SELECT @PId = Id FROM system.SavedQuery WHERE Name = @PName;
            PRINT N'  UPDATED SavedQuery Name=' + @PName + N' Id=' + CAST(@PId AS nvarchar(20));
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
                @PName, @PTitle, @PQueryJson, @PCols, N'{}',
                @PBtn, NULL,
                1, 1, @PEnt, @PAct,
                1, 1, @SeedUser, @SeedUser,
                @Now, @NowShamsi, @Now, @NowShamsi,
                1
            );
            SET @PId = SCOPE_IDENTITY();
            PRINT N'  CREATED SavedQuery Name=' + @PName + N' Id=' + CAST(@PId AS nvarchar(20));
        END

        FETCH NEXT FROM cc_cur INTO @PName, @PTitle, @PEnt, @PCols, @PSelect, @PAct, @PBtn;
    END
    CLOSE cc_cur;
    DEALLOCATE cc_cur;

    COMMIT TRANSACTION;
    PRINT N'=== DONE: Seed_SaleCollectionClaim_DataProfiles committed ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    PRINT N'=== ERROR: Seed_SaleCollectionClaim_DataProfiles rolled back ===';
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

PRINT N'--- verify ---';
SELECT Id, Name, Title, Mode, Type
FROM system.SavedQuery
WHERE Name IN (N'AfterSales_CollectionClaim_List', N'AfterSales_CollectionClaimPayFish_List');

SELECT
    (SELECT COUNT(*) FROM Sale.CollectionClaim WHERE DlId IS NOT NULL AND DlId <> 0) AS ClaimsWithDl,
    (SELECT COUNT(*) FROM Sale.CollectionClaim) AS ClaimsTotal;
