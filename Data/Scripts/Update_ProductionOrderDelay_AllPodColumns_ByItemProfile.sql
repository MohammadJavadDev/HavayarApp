/*
================================================================================
Update_ProductionOrderDelay_AllPodColumns_ByItemProfile.sql
================================================================================
1) vw_All_POD (سفارش ساخت های دارای تاخیر)
   ستون ها = کپی vw_All_Need_POD
   فیلتر همان قبلی می ماند: MaxDelayDays > 0
   (بدون شرط RegisteredDelayDays < MaxDelayDays)

2) نمایه جدید vw_Need_POD_ByItem
   همان شرط نمایه «نیاز به ثبت علت تاخیر»، یک ردیف به ازای هر قلم با تاخیر > 0

UTF-8 BOM. Idempotent.
sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Update_ProductionOrderDelay_AllPodColumns_ByItemProfile.sql"
================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'update-pod-columns-by-item';
    DECLARE @Marker NVARCHAR(40) = N'--!--mainsection';
    DECLARE @NeedWhere NVARCHAR(200) = N'WHERE r.MaxDelayDays > 0 AND ISNULL(da.RegisteredDelayDays, 0) < r.MaxDelayDays';
    DECLARE @AllWhere NVARCHAR(80) = N'WHERE r.MaxDelayDays > 0';

    DECLARE @NeedJson NVARCHAR(MAX);
    DECLARE @NeedQuery NVARCHAR(MAX);
    DECLARE @NeedColumns NVARCHAR(MAX);
    DECLARE @NeedButtons NVARCHAR(MAX);
    DECLARE @NeedActions NVARCHAR(MAX);
    DECLARE @NeedEvents NVARCHAR(MAX);

    SELECT
        @NeedJson = s.QueryJson,
        @NeedColumns = s.ColumnsJson,
        @NeedButtons = s.CustomActionButtonsJson,
        @NeedActions = s.ActionOptions,
        @NeedEvents = s.EventScriptsJson
    FROM system.SavedQuery s
    WHERE s.Name = N'vw_All_Need_POD';

    SELECT @NeedQuery = CAST(j.[value] AS NVARCHAR(MAX))
    FROM OPENJSON(@NeedJson) j
    WHERE j.[key] = N'CustomQuery';

    IF @NeedQuery IS NULL OR CHARINDEX(@NeedWhere, @NeedQuery) = 0 OR CHARINDEX(@Marker, @NeedQuery) = 0
        THROW 51020, N'کوئری نمایه vw_All_Need_POD برای همگام سازی ستون ها قابل استفاده نیست.', 1;

    DECLARE @AllQuery NVARCHAR(MAX) = REPLACE(@NeedQuery, @NeedWhere, @AllWhere);
    IF CHARINDEX(@NeedWhere, @AllQuery) > 0
        THROW 51021, N'فیلتر نمایه دارای تاخیر حذف نشد.', 1;

    UPDATE system.SavedQuery
    SET QueryJson = JSON_MODIFY(QueryJson, N'$.CustomQuery', @AllQuery),
        ColumnsJson = @NeedColumns,
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Name = N'vw_All_POD';

    IF @@ROWCOUNT <> 1
        THROW 51022, N'نمایه vw_All_POD یافت نشد.', 1;

    DECLARE @Head NVARCHAR(MAX) = LEFT(@NeedQuery, CHARINDEX(@Marker, @NeedQuery) + LEN(@Marker) - 1);
    DECLARE @ItemSelect NVARCHAR(MAX) = N'
SELECT
    idl.ProductionOrderItemId,
    idl.ProductionOrderId,
    [t5].FullName AS CustomerName,
    ISNULL(part.Name, N''-'') AS DelayCausePartName,
    ISNULL(part.Code, N''-'') AS DelayCausePartCode,
    ISNULL(idl.Serial, N''-'') AS DelayCauseSerial,
    po.ProductionOrderNumber,
    r.MaxDeliverDateOfOrder,
    dbo.fn_MiladiToShamsi(r.MaxDeliverDateOfOrder) AS MaxDeliverDateOfOrderShamsi,
    idl.PreparationMiladiDate AS PreparationDate,
    dbo.fn_MiladiToShamsi(idl.PreparationMiladiDate) AS PreparationDateShamsi,
    idl.ItemDelayDays AS DelayDay,
    CASE WHEN r.MissingPreparationCount = 0 THEN N''کامل شده'' ELSE N''کامل نشده'' END AS PreparationStatus,
    r.MissingPreparationCount,
    r.TotalItemsCount,
    ISNULL(da.RegisteredDelayDays, 0) AS RegisteredDelayDays,
    r.MaxDelayDays - ISNULL(da.RegisteredDelayDays, 0) AS NeedsDelayRegistrationDays
FROM ItemDelay idl
INNER JOIN OrderAgg r ON r.ProductionOrderId = idl.ProductionOrderId
INNER JOIN Sale.ProductionOrder po ON po.Id = idl.ProductionOrderId
LEFT JOIN DelayAgg da ON da.ProductionOrderId = idl.ProductionOrderId
LEFT JOIN Inv.Part part ON part.Id = idl.PartId
LEFT JOIN [SLS].[Contract] AS [t3] ON po.[ContractId] = [t3].[Id]
LEFT JOIN [SLS].[Customer] AS [t4] ON [t3].[CustomerId] = [t4].[Id]
LEFT JOIN [Gnr].[Party] AS [t5] ON [t4].[PartyId] = [t5].[Id]
WHERE r.MaxDelayDays > 0
  AND ISNULL(da.RegisteredDelayDays, 0) < r.MaxDelayDays
  AND idl.ItemDelayDays > 0';

    DECLARE @ItemQuery NVARCHAR(MAX) = @Head + @ItemSelect;

    DECLARE @ProbeTail NVARCHAR(MAX) = STUFF(@ItemSelect, CHARINDEX(N'SELECT', @ItemSelect), 6, N'SELECT TOP 1');
    DECLARE @ProbeQuery NVARCHAR(MAX) = @Head + @ProbeTail;
    EXEC sys.sp_executesql @ProbeQuery;

    DECLARE @ItemPk NVARCHAR(MAX);
    SELECT @ItemPk = CAST(j.[value] AS NVARCHAR(MAX))
    FROM OPENJSON(@NeedColumns) j
    WHERE JSON_VALUE(j.[value], '$.ColumnName') = N'ProductionOrderId';

    SET @ItemPk = JSON_MODIFY(@ItemPk, '$.ColumnName', N'ProductionOrderItemId');
    SET @ItemPk = JSON_MODIFY(@ItemPk, '$.Alliance', N'ProductionOrderItemId');
    SET @ItemPk = JSON_MODIFY(@ItemPk, '$.DisplayName', N'شناسه قلم');
    SET @ItemPk = JSON_MODIFY(@ItemPk, '$.PrimaryKey', CAST(1 AS bit));
    SET @ItemPk = JSON_MODIFY(@ItemPk, '$.Visible', CAST(0 AS bit));
    SET @ItemPk = JSON_MODIFY(@ItemPk, '$.SortDirection', 1);

    DECLARE @ItemColumns NVARCHAR(MAX);
    SELECT @ItemColumns = N'[' + @ItemPk + N',' + STRING_AGG(CAST(
        CASE
            WHEN JSON_VALUE(j.[value], '$.ColumnName') = N'ProductionOrderId' THEN
                JSON_MODIFY(JSON_MODIFY(j.[value], '$.PrimaryKey', CAST(0 AS bit)), '$.Visible', CAST(0 AS bit))
            WHEN JSON_VALUE(j.[value], '$.ColumnName') = N'DelayCausePartName' THEN
                JSON_MODIFY(JSON_MODIFY(j.[value], '$.Visible', CAST(1 AS bit)), '$.Width', 220)
            WHEN JSON_VALUE(j.[value], '$.ColumnName') = N'DelayCausePartCode' THEN
                JSON_MODIFY(JSON_MODIFY(j.[value], '$.Visible', CAST(1 AS bit)), '$.Width', 140)
            WHEN JSON_VALUE(j.[value], '$.ColumnName') = N'DelayCauseSerial' THEN
                JSON_MODIFY(JSON_MODIFY(j.[value], '$.Visible', CAST(1 AS bit)), '$.Width', 140)
            ELSE j.[value]
        END AS NVARCHAR(MAX)), N',') WITHIN GROUP (ORDER BY CAST(j.[key] AS INT)) + N']'
    FROM OPENJSON(@NeedColumns) j;

    DECLARE @ItemButtons NVARCHAR(MAX) = @NeedButtons;
    SET @ItemButtons = REPLACE(@ItemButtons, N'${ctx.primaryKeyValue}', N'${ctx.selectedRow.ProductionOrderId || ctx.selectedRow.productionOrderId}');
    SET @ItemButtons = REPLACE(@ItemButtons, N'var productionOrderId = ctx.primaryKeyValue;', N'var productionOrderId = ctx.selectedRow.ProductionOrderId || ctx.selectedRow.productionOrderId;');

    IF @ItemButtons = @NeedButtons
        THROW 51023, N'دکمه های نمایه تفکیک قلم به شناسه سفارش ساخت وصل نشد.', 1;

    DECLARE @ItemName NVARCHAR(100) = N'vw_Need_POD_ByItem';
    DECLARE @ItemTitle NVARCHAR(200) = N'تاخیرات نیازمند ثبت علت به تفکیک قلم';
    DECLARE @ItemJson NVARCHAR(MAX) = JSON_MODIFY(@NeedJson, N'$.CustomQuery', @ItemQuery);
    DECLARE @ItemId BIGINT;

    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @ItemName)
    BEGIN
        UPDATE system.SavedQuery
        SET Title = @ItemTitle,
            QueryJson = @ItemJson,
            ColumnsJson = @ItemColumns,
            EntityFullName = N'entities.app.pln.productionorderdelay',
            Mode = 1,
            Type = 1,
            ActionOptions = @NeedActions,
            CustomActionButtonsJson = @ItemButtons,
            EventScriptsJson = ISNULL(@NeedEvents, N'{"onSelectedRow":"","onRowAdded":""}'),
            DiagramJson = N'{}',
            IsActive = 1,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Name = @ItemName;

        SELECT @ItemId = Id FROM system.SavedQuery WHERE Name = @ItemName;
    END
    ELSE
    BEGIN
        INSERT INTO system.SavedQuery
            (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
             Mode, Type, EntityFullName, ActionOptions,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (@ItemName, @ItemTitle, @ItemJson, @ItemColumns, N'{}', @ItemButtons, ISNULL(@NeedEvents, N'{"onSelectedRow":"","onRowAdded":""}'),
             1, 1, N'entities.app.pln.productionorderdelay', @NeedActions,
             1, 1, @SeedUser, @SeedUser,
             @Now, @NowShamsi, @Now, @NowShamsi, 1);

        SET @ItemId = SCOPE_IDENTITY();
    END

    DELETE ra
    FROM system.RoleAccess ra
    WHERE ra.ActionAccessType = 3
      AND ra.RowId = @ItemId;

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(@ItemId AS NVARCHAR(20)), 3, 7, N'Entities.App.Pln.ProductionOrderDelay', @ItemTitle, NULL, @ItemId, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    WHERE r.Name IN (N'Pln.ProductionOrderDelay.FullAccess', N'ShowAllMenus');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT q.Id, q.Name, q.Title
FROM system.SavedQuery q
WHERE q.Name IN (N'vw_All_Need_POD', N'vw_All_POD', N'vw_Need_POD_ByItem');

SELECT q.Name,
       JSON_VALUE(j.[value], '$.Alliance') AS Alliance,
       JSON_VALUE(j.[value], '$.DisplayName') AS DisplayName,
       JSON_VALUE(j.[value], '$.Visible') AS Visible,
       JSON_VALUE(j.[value], '$.PrimaryKey') AS PK
FROM system.SavedQuery q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name IN (N'vw_All_POD', N'vw_Need_POD_ByItem')
ORDER BY q.Name, CAST(j.[key] AS INT);
