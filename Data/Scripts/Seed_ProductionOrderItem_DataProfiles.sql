/*
  Seed_ProductionOrderItem_DataProfiles.sql
  Idempotent: Roles + SavedQuery DataProfiles (copy Id=104) + RoleAccess + HTS users
  Safe to re-run.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

PRINT N'=== [1/4] Ensure Roles (IF NOT EXISTS by Name) ===';

IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Sale.ProductionOrder.ShowAll')
BEGIN
    SET IDENTITY_INSERT system.Role ON;
    INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, IsActive)
    VALUES (200020, N'Sale.ProductionOrder.ShowAll', N'فروش - سفارش ساخت - مشاهده همه', 1, N'seed-poi-dataprofiles', 1, N'seed-poi-dataprofiles', GETDATE(), GETDATE(), 1);
    SET IDENTITY_INSERT system.Role OFF;
    PRINT N'  CREATED Role Id=200020 Name=Sale.ProductionOrder.ShowAll';
END
ELSE
    PRINT N'  EXISTS Role Name=Sale.ProductionOrder.ShowAll';

IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Sale.ProductionOrder.FinancialConfirm')
BEGIN
    SET IDENTITY_INSERT system.Role ON;
    INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, IsActive)
    VALUES (200021, N'Sale.ProductionOrder.FinancialConfirm', N'فروش - سفارش ساخت - تایید مالی', 1, N'seed-poi-dataprofiles', 1, N'seed-poi-dataprofiles', GETDATE(), GETDATE(), 1);
    SET IDENTITY_INSERT system.Role OFF;
    PRINT N'  CREATED Role Id=200021 Name=Sale.ProductionOrder.FinancialConfirm';
END
ELSE
    PRINT N'  EXISTS Role Name=Sale.ProductionOrder.FinancialConfirm';

IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Sale.ProductionOrder.Obsolete')
BEGIN
    SET IDENTITY_INSERT system.Role ON;
    INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, IsActive)
    VALUES (200022, N'Sale.ProductionOrder.Obsolete', N'فروش - سفارش ساخت - منسوخ‌سازی', 1, N'seed-poi-dataprofiles', 1, N'seed-poi-dataprofiles', GETDATE(), GETDATE(), 1);
    SET IDENTITY_INSERT system.Role OFF;
    PRINT N'  CREATED Role Id=200022 Name=Sale.ProductionOrder.Obsolete';
END
ELSE
    PRINT N'  EXISTS Role Name=Sale.ProductionOrder.Obsolete';

IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Sale.ProductionOrderItem.AcceptIndustrial')
BEGIN
    SET IDENTITY_INSERT system.Role ON;
    INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, IsActive)
    VALUES (200023, N'Sale.ProductionOrderItem.AcceptIndustrial', N'فروش - اقلام سفارش ساخت - تایید صنایع', 1, N'seed-poi-dataprofiles', 1, N'seed-poi-dataprofiles', GETDATE(), GETDATE(), 1);
    SET IDENTITY_INSERT system.Role OFF;
    PRINT N'  CREATED Role Id=200023 Name=Sale.ProductionOrderItem.AcceptIndustrial';
END
ELSE
    PRINT N'  EXISTS Role Name=Sale.ProductionOrderItem.AcceptIndustrial';

IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Sale.ProductionOrderItem.AcceptEngineeringMechanical')
BEGIN
    SET IDENTITY_INSERT system.Role ON;
    INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, IsActive)
    VALUES (200024, N'Sale.ProductionOrderItem.AcceptEngineeringMechanical', N'فروش - اقلام سفارش ساخت - تایید مهندسی مکانیک', 1, N'seed-poi-dataprofiles', 1, N'seed-poi-dataprofiles', GETDATE(), GETDATE(), 1);
    SET IDENTITY_INSERT system.Role OFF;
    PRINT N'  CREATED Role Id=200024 Name=Sale.ProductionOrderItem.AcceptEngineeringMechanical';
END
ELSE
    PRINT N'  EXISTS Role Name=Sale.ProductionOrderItem.AcceptEngineeringMechanical';

IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Sale.ProductionOrderItem.AcceptEngineeringElectrical')
BEGIN
    SET IDENTITY_INSERT system.Role ON;
    INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, IsActive)
    VALUES (200025, N'Sale.ProductionOrderItem.AcceptEngineeringElectrical', N'فروش - اقلام سفارش ساخت - تایید مهندسی برق', 1, N'seed-poi-dataprofiles', 1, N'seed-poi-dataprofiles', GETDATE(), GETDATE(), 1);
    SET IDENTITY_INSERT system.Role OFF;
    PRINT N'  CREATED Role Id=200025 Name=Sale.ProductionOrderItem.AcceptEngineeringElectrical';
END
ELSE
    PRINT N'  EXISTS Role Name=Sale.ProductionOrderItem.AcceptEngineeringElectrical';

IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Sale.ProductionOrderItem.AcceptProjectManager')
BEGIN
    SET IDENTITY_INSERT system.Role ON;
    INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, IsActive)
    VALUES (200026, N'Sale.ProductionOrderItem.AcceptProjectManager', N'فروش - اقلام سفارش ساخت - تایید مدیر پروژه', 1, N'seed-poi-dataprofiles', 1, N'seed-poi-dataprofiles', GETDATE(), GETDATE(), 1);
    SET IDENTITY_INSERT system.Role OFF;
    PRINT N'  CREATED Role Id=200026 Name=Sale.ProductionOrderItem.AcceptProjectManager';
END
ELSE
    PRINT N'  EXISTS Role Name=Sale.ProductionOrderItem.AcceptProjectManager';

IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Sale.ProductionOrderItem.Inquirer')
BEGIN
    SET IDENTITY_INSERT system.Role ON;
    INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, IsActive)
    VALUES (200027, N'Sale.ProductionOrderItem.Inquirer', N'فروش - اقلام سفارش ساخت - استعلام', 1, N'seed-poi-dataprofiles', 1, N'seed-poi-dataprofiles', GETDATE(), GETDATE(), 1);
    SET IDENTITY_INSERT system.Role OFF;
    PRINT N'  CREATED Role Id=200027 Name=Sale.ProductionOrderItem.Inquirer';
END
ELSE
    PRINT N'  EXISTS Role Name=Sale.ProductionOrderItem.Inquirer';

IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Sale.ProductionOrderItem.MinistryOfIndustryInquirer')
BEGIN
    SET IDENTITY_INSERT system.Role ON;
    INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, IsActive)
    VALUES (200028, N'Sale.ProductionOrderItem.MinistryOfIndustryInquirer', N'فروش - اقلام سفارش ساخت - استعلام وزارت صنایع', 1, N'seed-poi-dataprofiles', 1, N'seed-poi-dataprofiles', GETDATE(), GETDATE(), 1);
    SET IDENTITY_INSERT system.Role OFF;
    PRINT N'  CREATED Role Id=200028 Name=Sale.ProductionOrderItem.MinistryOfIndustryInquirer';
END
ELSE
    PRINT N'  EXISTS Role Name=Sale.ProductionOrderItem.MinistryOfIndustryInquirer';

IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Sale.ProductionOrderItem.SupplyCommitteeBoss')
BEGIN
    SET IDENTITY_INSERT system.Role ON;
    INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, IsActive)
    VALUES (200029, N'Sale.ProductionOrderItem.SupplyCommitteeBoss', N'فروش - اقلام سفارش ساخت - رئیس کمیته تامین', 1, N'seed-poi-dataprofiles', 1, N'seed-poi-dataprofiles', GETDATE(), GETDATE(), 1);
    SET IDENTITY_INSERT system.Role OFF;
    PRINT N'  CREATED Role Id=200029 Name=Sale.ProductionOrderItem.SupplyCommitteeBoss';
END
ELSE
    PRINT N'  EXISTS Role Name=Sale.ProductionOrderItem.SupplyCommitteeBoss';

IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Sale.ProductionOrderItem.ViewRelated')
BEGIN
    SET IDENTITY_INSERT system.Role ON;
    INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, IsActive)
    VALUES (200030, N'Sale.ProductionOrderItem.ViewRelated', N'فروش - اقلام سفارش ساخت - مشاهده مرتبط', 1, N'seed-poi-dataprofiles', 1, N'seed-poi-dataprofiles', GETDATE(), GETDATE(), 1);
    SET IDENTITY_INSERT system.Role OFF;
    PRINT N'  CREATED Role Id=200030 Name=Sale.ProductionOrderItem.ViewRelated';
END
ELSE
    PRINT N'  EXISTS Role Name=Sale.ProductionOrderItem.ViewRelated';

IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Sale.ProductionOrderItem.History')
BEGIN
    SET IDENTITY_INSERT system.Role ON;
    INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, IsActive)
    VALUES (200031, N'Sale.ProductionOrderItem.History', N'فروش - اقلام سفارش ساخت - پیشینه', 1, N'seed-poi-dataprofiles', 1, N'seed-poi-dataprofiles', GETDATE(), GETDATE(), 1);
    SET IDENTITY_INSERT system.Role OFF;
    PRINT N'  CREATED Role Id=200031 Name=Sale.ProductionOrderItem.History';
END
ELSE
    PRINT N'  EXISTS Role Name=Sale.ProductionOrderItem.History';

IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Sale.ProductionOrderItem.ViewActive')
BEGIN
    SET IDENTITY_INSERT system.Role ON;
    INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, IsActive)
    VALUES (200032, N'Sale.ProductionOrderItem.ViewActive', N'فروش - اقلام سفارش ساخت - مشاهده فعال', 1, N'seed-poi-dataprofiles', 1, N'seed-poi-dataprofiles', GETDATE(), GETDATE(), 1);
    SET IDENTITY_INSERT system.Role OFF;
    PRINT N'  CREATED Role Id=200032 Name=Sale.ProductionOrderItem.ViewActive';
END
ELSE
    PRINT N'  EXISTS Role Name=Sale.ProductionOrderItem.ViewActive';

PRINT N'=== [2/4] Upsert SavedQuery DataProfiles (copy from Id=104) ===';

IF NOT EXISTS (SELECT 1 FROM system.SavedQuery WHERE Id = 104)
BEGIN
    RAISERROR(N'Base SavedQuery Id=104 not found. Aborting.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END

DECLARE @BaseQueryJson nvarchar(max), @BaseColumnsJson nvarchar(max), @BaseDiagramJson nvarchar(max);
DECLARE @BaseActionOptions nvarchar(max), @BaseCustomButtons nvarchar(max), @BaseEventScripts nvarchar(max);
DECLARE @BaseEntityFullName nvarchar(max), @BaseType int, @BaseMode int, @BaseIsActive int;
DECLARE @BaseCreatedById bigint;

SELECT
    @BaseQueryJson = QueryJson,
    @BaseColumnsJson = ColumnsJson,
    @BaseDiagramJson = DiagramJson,
    @BaseActionOptions = ActionOptions,
    @BaseCustomButtons = CustomActionButtonsJson,
    @BaseEventScripts = EventScriptsJson,
    @BaseEntityFullName = EntityFullName,
    @BaseType = Type,
    @BaseMode = Mode,
    @BaseIsActive = IsActive,
    @BaseCreatedById = CreatedById
FROM system.SavedQuery
WHERE Id = 104;

-- 104 ممکن است از اجرای Update_ProductionOrderItem_ListInfo_IndustrialScope.sql
-- WHERE و Filters صنعتی داشته باشد. قبل از کپی به نمایه‌های دیگر، SELECT را تمیز کن.
DECLARE @BaseCustomQueryForCopy nvarchar(max);
SELECT @BaseCustomQueryForCopy = CustomQuery
FROM OPENJSON(@BaseQueryJson) WITH (CustomQuery nvarchar(max) '$.CustomQuery');

IF @BaseCustomQueryForCopy IS NULL OR LEN(@BaseCustomQueryForCopy) = 0
BEGIN
    RAISERROR(N'CustomQuery نمایه پایه (Id=104) خالی است. Aborting.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END

SET @BaseCustomQueryForCopy = REPLACE(@BaseCustomQueryForCopy, N'[t6].[Name] AS [t9_Name]', N'[t9].[Name] AS [t9_Name]');
IF CHARINDEX(N'WHERE [t1].[IsDeleted]', @BaseCustomQueryForCopy) > 0
    SET @BaseCustomQueryForCopy = LEFT(@BaseCustomQueryForCopy, CHARINDEX(N'WHERE [t1].[IsDeleted]', @BaseCustomQueryForCopy) - 1);
SET @BaseCustomQueryForCopy = RTRIM(@BaseCustomQueryForCopy);

SET @BaseQueryJson = JSON_MODIFY(@BaseQueryJson, '$.CustomQuery', @BaseCustomQueryForCopy);
SET @BaseQueryJson = JSON_MODIFY(@BaseQueryJson, '$.Filters', JSON_QUERY(N'[]'));

DECLARE @ProfileName nvarchar(100), @ProfileTitle nvarchar(200), @FiltersJson nvarchar(max), @NewQueryJson nvarchar(max), @SqId bigint;

IF OBJECT_ID('tempdb..#PoiProfiles') IS NOT NULL DROP TABLE #PoiProfiles;
CREATE TABLE #PoiProfiles (
    Name nvarchar(100) NOT NULL PRIMARY KEY,
    Title nvarchar(200) NOT NULL,
    FiltersJson nvarchar(max) NOT NULL,
    SavedQueryId bigint NULL
);
INSERT INTO #PoiProfiles (Name, Title, FiltersJson) VALUES (N'POI_All_Active', N'اقلام سفارش ساخت — همه فعال', N'[{"Id":"id_poi_isdel","TableName":"Sale.ProductionOrderItem","ColumnName":"IsDeleted","DataType":"bit","Operator":"equals","Value":"false","LogicalOperator":""},{"Id":"id_poi_islat","TableName":"Sale.ProductionOrderItem","ColumnName":"IsLatestVersion","DataType":"bit","Operator":"equals","Value":"true","LogicalOperator":"AND"}]');
INSERT INTO #PoiProfiles (Name, Title, FiltersJson) VALUES (N'POI_ViewActive', N'اقلام سفارش ساخت — مشاهده فعال', N'[{"Id":"id_poi_isdel","TableName":"Sale.ProductionOrderItem","ColumnName":"IsDeleted","DataType":"bit","Operator":"equals","Value":"false","LogicalOperator":""},{"Id":"id_poi_islat","TableName":"Sale.ProductionOrderItem","ColumnName":"IsLatestVersion","DataType":"bit","Operator":"equals","Value":"true","LogicalOperator":"AND"}]');
INSERT INTO #PoiProfiles (Name, Title, FiltersJson) VALUES (N'POI_Cartable_Industrial', N'کارتابل صنایع — اقلام سفارش ساخت', N'[{"Id":"id_poi_isdel","TableName":"Sale.ProductionOrderItem","ColumnName":"IsDeleted","DataType":"bit","Operator":"equals","Value":"false","LogicalOperator":""},{"Id":"id_poi_islat","TableName":"Sale.ProductionOrderItem","ColumnName":"IsLatestVersion","DataType":"bit","Operator":"equals","Value":"true","LogicalOperator":"AND"},{"Id":"id_poi_chk","TableName":"Sale.ProductionOrderItem","ColumnName":"CheckStatus","DataType":"int","Operator":"in","Value":"1904,1905","LogicalOperator":"AND"}]');
INSERT INTO #PoiProfiles (Name, Title, FiltersJson) VALUES (N'POI_Cartable_Engineering_Mechanical', N'کارتابل مهندسی مکانیک', N'[{"Id":"id_poi_isdel","TableName":"Sale.ProductionOrderItem","ColumnName":"IsDeleted","DataType":"bit","Operator":"equals","Value":"false","LogicalOperator":""},{"Id":"id_poi_islat","TableName":"Sale.ProductionOrderItem","ColumnName":"IsLatestVersion","DataType":"bit","Operator":"equals","Value":"true","LogicalOperator":"AND"},{"Id":"id_poi_chk","TableName":"Sale.ProductionOrderItem","ColumnName":"CheckStatus","DataType":"int","Operator":"equals","Value":"2195","LogicalOperator":"AND"},{"Id":"id_poi_dev","TableName":"Sale.ProductionOrderItem","ColumnName":"DeviceType","DataType":"int","Operator":"notIn","Value":"2403,2404,2212,2211","LogicalOperator":"AND"}]');
INSERT INTO #PoiProfiles (Name, Title, FiltersJson) VALUES (N'POI_Cartable_Engineering_Electrical', N'کارتابل مهندسی برق', N'[{"Id":"id_poi_isdel","TableName":"Sale.ProductionOrderItem","ColumnName":"IsDeleted","DataType":"bit","Operator":"equals","Value":"false","LogicalOperator":""},{"Id":"id_poi_islat","TableName":"Sale.ProductionOrderItem","ColumnName":"IsLatestVersion","DataType":"bit","Operator":"equals","Value":"true","LogicalOperator":"AND"},{"Id":"id_poi_chk","TableName":"Sale.ProductionOrderItem","ColumnName":"CheckStatus","DataType":"int","Operator":"equals","Value":"2195","LogicalOperator":"AND"},{"Id":"id_poi_dev","TableName":"Sale.ProductionOrderItem","ColumnName":"DeviceType","DataType":"int","Operator":"in","Value":"2403,2404,2212,2211","LogicalOperator":"AND"}]');
INSERT INTO #PoiProfiles (Name, Title, FiltersJson) VALUES (N'POI_Cartable_ProjectManager', N'کارتابل مدیر پروژه', N'[{"Id":"id_poi_isdel","TableName":"Sale.ProductionOrderItem","ColumnName":"IsDeleted","DataType":"bit","Operator":"equals","Value":"false","LogicalOperator":""},{"Id":"id_poi_islat","TableName":"Sale.ProductionOrderItem","ColumnName":"IsLatestVersion","DataType":"bit","Operator":"equals","Value":"true","LogicalOperator":"AND"},{"Id":"id_poi_chk","TableName":"Sale.ProductionOrderItem","ColumnName":"CheckStatus","DataType":"int","Operator":"equals","Value":"2258","LogicalOperator":"AND"}]');
INSERT INTO #PoiProfiles (Name, Title, FiltersJson) VALUES (N'POI_Cartable_Inquiry', N'کارتابل استعلام', N'[{"Id":"id_poi_isdel","TableName":"Sale.ProductionOrderItem","ColumnName":"IsDeleted","DataType":"bit","Operator":"equals","Value":"false","LogicalOperator":""},{"Id":"id_poi_islat","TableName":"Sale.ProductionOrderItem","ColumnName":"IsLatestVersion","DataType":"bit","Operator":"equals","Value":"true","LogicalOperator":"AND"},{"Id":"id_poi_chk","TableName":"Sale.ProductionOrderItem","ColumnName":"CheckStatus","DataType":"int","Operator":"equals","Value":"1906","LogicalOperator":"AND"}]');
INSERT INTO #PoiProfiles (Name, Title, FiltersJson) VALUES (N'POI_Cartable_Ministry', N'کارتابل وزارت صنایع', N'[{"Id":"id_poi_isdel","TableName":"Sale.ProductionOrderItem","ColumnName":"IsDeleted","DataType":"bit","Operator":"equals","Value":"false","LogicalOperator":""},{"Id":"id_poi_islat","TableName":"Sale.ProductionOrderItem","ColumnName":"IsLatestVersion","DataType":"bit","Operator":"equals","Value":"true","LogicalOperator":"AND"},{"Id":"id_poi_chk","TableName":"Sale.ProductionOrderItem","ColumnName":"CheckStatus","DataType":"int","Operator":"equals","Value":"1908","LogicalOperator":"AND"}]');
INSERT INTO #PoiProfiles (Name, Title, FiltersJson) VALUES (N'POI_Cartable_SupplyCommittee', N'کارتابل رئیس کمیته تامین', N'[{"Id":"id_poi_isdel","TableName":"Sale.ProductionOrderItem","ColumnName":"IsDeleted","DataType":"bit","Operator":"equals","Value":"false","LogicalOperator":""},{"Id":"id_poi_islat","TableName":"Sale.ProductionOrderItem","ColumnName":"IsLatestVersion","DataType":"bit","Operator":"equals","Value":"true","LogicalOperator":"AND"},{"Id":"id_poi_chk","TableName":"Sale.ProductionOrderItem","ColumnName":"CheckStatus","DataType":"int","Operator":"in","Value":"1909,2202","LogicalOperator":"AND"}]');
INSERT INTO #PoiProfiles (Name, Title, FiltersJson) VALUES (N'POI_Sales_Related_Active', N'اقلام سفارش ساخت — مرتبط با من', N'[{"Id":"id_poi_isdel","TableName":"Sale.ProductionOrderItem","ColumnName":"IsDeleted","DataType":"bit","Operator":"equals","Value":"false","LogicalOperator":""},{"Id":"id_poi_islat","TableName":"Sale.ProductionOrderItem","ColumnName":"IsLatestVersion","DataType":"bit","Operator":"equals","Value":"true","LogicalOperator":"AND"}]');
INSERT INTO #PoiProfiles (Name, Title, FiltersJson) VALUES (N'POI_History_Deleted', N'پیشینه اقلام سفارش ساخت (حذف‌شده)', N'[{"Id":"id_poi_isdel","TableName":"Sale.ProductionOrderItem","ColumnName":"IsDeleted","DataType":"bit","Operator":"equals","Value":"true","LogicalOperator":""}]');

DECLARE profile_cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT Name, Title, FiltersJson FROM #PoiProfiles;
OPEN profile_cur;
FETCH NEXT FROM profile_cur INTO @ProfileName, @ProfileTitle, @FiltersJson;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @NewQueryJson = REPLACE(@BaseQueryJson, N'"Filters":[]', N'"Filters":' + @FiltersJson);

    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @ProfileName)
    BEGIN
        UPDATE system.SavedQuery
        SET Title = @ProfileTitle,
            QueryJson = @NewQueryJson,
            ColumnsJson = @BaseColumnsJson,
            DiagramJson = @BaseDiagramJson,
            ActionOptions = @BaseActionOptions,
            CustomActionButtonsJson = @BaseCustomButtons,
            EventScriptsJson = @BaseEventScripts,
            EntityFullName = @BaseEntityFullName,
            Type = @BaseType,
            Mode = @BaseMode,
            IsActive = @BaseIsActive,
            ModifiedById = 1,
            ModifiedByName = N'seed-poi-dataprofiles',
            ModifiedDateMiladiDateTime = GETDATE()
        WHERE Name = @ProfileName;

        SELECT @SqId = Id FROM system.SavedQuery WHERE Name = @ProfileName;
        PRINT N'  UPDATED SavedQuery Name=' + @ProfileName + N' Id=' + CAST(@SqId AS nvarchar(20));
    END
    ELSE
    BEGIN
        INSERT INTO system.SavedQuery
        (
            Name, Title, QueryJson, ColumnsJson, DiagramJson,
            CreatedById, ModifiedById, CreatedByName, ModifiedByName,
            CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, IsActive,
            EntityFullName, Type, Mode, ActionOptions, CustomActionButtonsJson, EventScriptsJson
        )
        VALUES
        (
            @ProfileName, @ProfileTitle, @NewQueryJson, @BaseColumnsJson, @BaseDiagramJson,
            ISNULL(@BaseCreatedById, 1), 1, N'seed-poi-dataprofiles', N'seed-poi-dataprofiles',
            GETDATE(), GETDATE(), @BaseIsActive,
            @BaseEntityFullName, @BaseType, @BaseMode, @BaseActionOptions, @BaseCustomButtons, @BaseEventScripts
        );
        SET @SqId = SCOPE_IDENTITY();
        PRINT N'  CREATED SavedQuery Name=' + @ProfileName + N' Id=' + CAST(@SqId AS nvarchar(20));
    END

    UPDATE #PoiProfiles SET SavedQueryId = @SqId WHERE Name = @ProfileName;
    FETCH NEXT FROM profile_cur INTO @ProfileName, @ProfileTitle, @FiltersJson;
END
CLOSE profile_cur;
DEALLOCATE profile_cur;

PRINT N'=== [3/4] Rebuild RoleAccess (ActionAccessType=3 DataProfile) ===';

DELETE ra
FROM system.RoleAccess ra
INNER JOIN #PoiProfiles p ON p.SavedQueryId = ra.RowId
WHERE ra.ActionAccessType = 3;
PRINT N'  Cleared prior RoleAccess for POI profiles: ' + CAST(@@ROWCOUNT AS nvarchar(20));

IF OBJECT_ID('tempdb..#PoiRoleAccess') IS NOT NULL DROP TABLE #PoiRoleAccess;
CREATE TABLE #PoiRoleAccess (ProfileName nvarchar(100) NOT NULL, RoleName nvarchar(200) NOT NULL);
INSERT INTO #PoiRoleAccess (ProfileName, RoleName) VALUES (N'POI_All_Active', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #PoiRoleAccess (ProfileName, RoleName) VALUES (N'POI_ViewActive', N'Sale.ProductionOrderItem.ViewActive');
INSERT INTO #PoiRoleAccess (ProfileName, RoleName) VALUES (N'POI_Cartable_Industrial', N'Sale.ProductionOrderItem.AcceptIndustrial');
INSERT INTO #PoiRoleAccess (ProfileName, RoleName) VALUES (N'POI_Cartable_Engineering_Mechanical', N'Sale.ProductionOrderItem.AcceptEngineeringMechanical');
INSERT INTO #PoiRoleAccess (ProfileName, RoleName) VALUES (N'POI_Cartable_Engineering_Electrical', N'Sale.ProductionOrderItem.AcceptEngineeringElectrical');
INSERT INTO #PoiRoleAccess (ProfileName, RoleName) VALUES (N'POI_Cartable_ProjectManager', N'Sale.ProductionOrderItem.AcceptProjectManager');
INSERT INTO #PoiRoleAccess (ProfileName, RoleName) VALUES (N'POI_Cartable_Inquiry', N'Sale.ProductionOrderItem.Inquirer');
INSERT INTO #PoiRoleAccess (ProfileName, RoleName) VALUES (N'POI_Cartable_Ministry', N'Sale.ProductionOrderItem.MinistryOfIndustryInquirer');
INSERT INTO #PoiRoleAccess (ProfileName, RoleName) VALUES (N'POI_Cartable_SupplyCommittee', N'Sale.ProductionOrderItem.SupplyCommitteeBoss');
INSERT INTO #PoiRoleAccess (ProfileName, RoleName) VALUES (N'POI_Sales_Related_Active', N'Sale.ProductionOrderItem.ViewRelated');
INSERT INTO #PoiRoleAccess (ProfileName, RoleName) VALUES (N'POI_History_Deleted', N'Sale.ProductionOrderItem.History');
INSERT INTO #PoiRoleAccess (ProfileName, RoleName) VALUES (N'POI_History_Deleted', N'Sale.ProductionOrder.ShowAll');

INSERT INTO system.RoleAccess
(
    Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName,
    EntityId, RowId, RoleId, CreatedById, ModifiedById, CreatedByName, ModifiedByName,
    CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, IsActive
)
SELECT
    N'dataProfile_' + CAST(p.SavedQueryId AS nvarchar(20)),
    3,
    NULL,
    N'Entities.App.Sale.ProductionOrderItem',
    p.Title,
    NULL,
    p.SavedQueryId,
    r.Id,
    1,
    1,
    N'seed-poi-dataprofiles',
    N'seed-poi-dataprofiles',
    GETDATE(),
    GETDATE(),
    1
FROM #PoiRoleAccess map
INNER JOIN #PoiProfiles p ON p.Name = map.ProfileName
INNER JOIN system.Role r ON r.Name = map.RoleName;
PRINT N'  Inserted RoleAccess rows: ' + CAST(@@ROWCOUNT AS nvarchar(20));

PRINT N'=== [4/4] Assign HTS group users to roles ===';

IF OBJECT_ID('tempdb..#UserRoleMap') IS NOT NULL DROP TABLE #UserRoleMap;
CREATE TABLE #UserRoleMap (Username nvarchar(200) NOT NULL, RoleName nvarchar(200) NOT NULL);
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'abdolmaleki.h', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Aghighi.p', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Ahmadvand.z', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Ahrarnejad.h', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Ahsani.e', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'allahverdi.s', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Asadpour.h', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Ashrafi.m', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Azari.sh', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'bagheri.h', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'bagheri.m', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Bamimohammadi.gh', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'barmaki.a', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'bayat.m', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Beigi.s', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Bosaghzadeh.e', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Daneshmandi.m', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'dehghan.r', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'dordab.y', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Eftekhari.a', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'erfani.k', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Erfani.m', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'esmaillian.r', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'estaki.a', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Farahmand.m', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Farokhi.m', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Fekri.f', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'givehchi.h', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'golestaneh.m', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'habibi.m', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'habibollah.m', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'hosseininezhad.e', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'javadian.p', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'kadkhodaei.v', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'kalhor.m', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Kamandar.m', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'kargar.m', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'khalaj.m', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'khederzadeh.s', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'khodabandeh.f', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'khodakarami.f', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'kiani.f', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'mahani.b', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'MalekMohammadi.m', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'maranaki.m', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'maroufkhani.m', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'masoudi.l', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'mehrafzoon', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Mirani.s', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Mirlohi.a', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'moeini.r', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Mohammadi.a', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Mohammadi.ma', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'mohammadi.r', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'mohammadi.s', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'mohammadi.sam', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Mohammadian.mo', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Moradi.p', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Mosayebi.m', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Naghdi.f', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Nazmi.b', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Neysari.n', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'nezafati.a', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Nikoosefat.m', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'noroozi.s', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'parhizkari.s', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Rajabi.m', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Rajablou.a', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Raman.f', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Ramezani.sh', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'ramezani.y', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'raoufi.a', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Razaghmanesh.z', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Rostampoor.a', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'sabermanesh.s', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Saeedi.m', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'salahi.m', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Samii.m', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'sarmadi.p', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Sepehrnejad.s', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Shaghaghi.m', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Shahpordeli.s', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Sharafi.z', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'sharifi.b', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'sharifi.mo', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'shourmeyj.m', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'sohrabi.z', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'sohrabi.za', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Tabatabaei.z', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'taheran.f', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'taheran.fa', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Tahmasebizadeh.sh', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Vahedian.s', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'yaghyaei.m', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'yaltaghian.f', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Zabihian.s', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Zamani.ar', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'zamani.o', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'khalili.aa', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'salim.sh', N'Sale.ProductionOrder.ShowAll');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Ahmadi.sh', N'Sale.ProductionOrderItem.AcceptIndustrial');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Asadi.re', N'Sale.ProductionOrderItem.AcceptIndustrial');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Azadbakhsh.s', N'Sale.ProductionOrderItem.AcceptIndustrial');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Azadbakhsh.sh', N'Sale.ProductionOrderItem.AcceptIndustrial');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'eezi.a', N'Sale.ProductionOrderItem.AcceptIndustrial');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'golestaneh.a', N'Sale.ProductionOrderItem.AcceptIndustrial');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'khalili.aa', N'Sale.ProductionOrderItem.AcceptIndustrial');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'kian.r', N'Sale.ProductionOrderItem.AcceptIndustrial');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'mehrabi.s', N'Sale.ProductionOrderItem.AcceptIndustrial');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Moradmand.a', N'Sale.ProductionOrderItem.AcceptIndustrial');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Saemian.r', N'Sale.ProductionOrderItem.AcceptIndustrial');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Ahmadi.sh', N'Sale.ProductionOrderItem.AcceptIndustrial');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Asadi.re', N'Sale.ProductionOrderItem.AcceptIndustrial');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Azadbakhsh.s', N'Sale.ProductionOrderItem.AcceptIndustrial');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Azadbakhsh.sh', N'Sale.ProductionOrderItem.AcceptIndustrial');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'eezi.a', N'Sale.ProductionOrderItem.AcceptIndustrial');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'khalili.aa', N'Sale.ProductionOrderItem.AcceptIndustrial');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'kian.r', N'Sale.ProductionOrderItem.AcceptIndustrial');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'mehrabi.s', N'Sale.ProductionOrderItem.AcceptIndustrial');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Moradmand.a', N'Sale.ProductionOrderItem.AcceptIndustrial');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Saemian.r', N'Sale.ProductionOrderItem.AcceptIndustrial');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'bagheri.m', N'Sale.ProductionOrderItem.AcceptEngineeringMechanical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Bamimohammadi.gh', N'Sale.ProductionOrderItem.AcceptEngineeringMechanical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Bosaghzadeh.e', N'Sale.ProductionOrderItem.AcceptEngineeringMechanical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Erfani.m', N'Sale.ProductionOrderItem.AcceptEngineeringMechanical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'esmaillian.r', N'Sale.ProductionOrderItem.AcceptEngineeringMechanical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Farahmand.m', N'Sale.ProductionOrderItem.AcceptEngineeringMechanical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'goudarzi.h', N'Sale.ProductionOrderItem.AcceptEngineeringMechanical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'habibi.m', N'Sale.ProductionOrderItem.AcceptEngineeringMechanical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'javadian.p', N'Sale.ProductionOrderItem.AcceptEngineeringMechanical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'kadkhodaei.v', N'Sale.ProductionOrderItem.AcceptEngineeringMechanical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Kazemzadeh.h', N'Sale.ProductionOrderItem.AcceptEngineeringMechanical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'khalaj.m', N'Sale.ProductionOrderItem.AcceptEngineeringMechanical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'khodakarami.f', N'Sale.ProductionOrderItem.AcceptEngineeringMechanical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Khorshidi.m', N'Sale.ProductionOrderItem.AcceptEngineeringMechanical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Mirlohi.a', N'Sale.ProductionOrderItem.AcceptEngineeringMechanical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Mohammadi.a', N'Sale.ProductionOrderItem.AcceptEngineeringMechanical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Neysari.n', N'Sale.ProductionOrderItem.AcceptEngineeringMechanical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Rajabi.m', N'Sale.ProductionOrderItem.AcceptEngineeringMechanical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'sabermanesh.s', N'Sale.ProductionOrderItem.AcceptEngineeringMechanical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Saeedi.m', N'Sale.ProductionOrderItem.AcceptEngineeringMechanical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Samii.m', N'Sale.ProductionOrderItem.AcceptEngineeringMechanical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'taheran.f', N'Sale.ProductionOrderItem.AcceptEngineeringMechanical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'bagheri.m', N'Sale.ProductionOrderItem.AcceptEngineeringElectrical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Bamimohammadi.gh', N'Sale.ProductionOrderItem.AcceptEngineeringElectrical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Bosaghzadeh.e', N'Sale.ProductionOrderItem.AcceptEngineeringElectrical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Erfani.m', N'Sale.ProductionOrderItem.AcceptEngineeringElectrical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'esmaillian.r', N'Sale.ProductionOrderItem.AcceptEngineeringElectrical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Farahmand.m', N'Sale.ProductionOrderItem.AcceptEngineeringElectrical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'goudarzi.h', N'Sale.ProductionOrderItem.AcceptEngineeringElectrical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'habibi.m', N'Sale.ProductionOrderItem.AcceptEngineeringElectrical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'javadian.p', N'Sale.ProductionOrderItem.AcceptEngineeringElectrical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'kadkhodaei.v', N'Sale.ProductionOrderItem.AcceptEngineeringElectrical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Kazemzadeh.h', N'Sale.ProductionOrderItem.AcceptEngineeringElectrical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'khalaj.m', N'Sale.ProductionOrderItem.AcceptEngineeringElectrical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'khodakarami.f', N'Sale.ProductionOrderItem.AcceptEngineeringElectrical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Khorshidi.m', N'Sale.ProductionOrderItem.AcceptEngineeringElectrical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Mirlohi.a', N'Sale.ProductionOrderItem.AcceptEngineeringElectrical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Mohammadi.a', N'Sale.ProductionOrderItem.AcceptEngineeringElectrical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Neysari.n', N'Sale.ProductionOrderItem.AcceptEngineeringElectrical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Rajabi.m', N'Sale.ProductionOrderItem.AcceptEngineeringElectrical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'sabermanesh.s', N'Sale.ProductionOrderItem.AcceptEngineeringElectrical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Saeedi.m', N'Sale.ProductionOrderItem.AcceptEngineeringElectrical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'Samii.m', N'Sale.ProductionOrderItem.AcceptEngineeringElectrical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'taheran.f', N'Sale.ProductionOrderItem.AcceptEngineeringElectrical');
INSERT INTO #UserRoleMap (Username, RoleName) VALUES (N'shirazi.m', N'Sale.ProductionOrderItem.SupplyCommitteeBoss');

;WITH d AS (
    SELECT Username, RoleName, ROW_NUMBER() OVER (PARTITION BY Username, RoleName ORDER BY Username) AS rn
    FROM #UserRoleMap
)
DELETE FROM d WHERE rn > 1;

DECLARE @MapUsername nvarchar(200), @MapRoleName nvarchar(200), @MapRoleId bigint, @MapUserId bigint;
DECLARE @Assigned int = 0, @Missing int = 0, @SkippedAlready int = 0;

DECLARE map_cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT Username, RoleName FROM #UserRoleMap;
OPEN map_cur;
FETCH NEXT FROM map_cur INTO @MapUsername, @MapRoleName;
WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT @MapRoleId = Id FROM system.Role WHERE Name = @MapRoleName;
    SELECT TOP 1 @MapUserId = Id FROM system.[User] WHERE LOWER(Username) = LOWER(@MapUsername);

    IF @MapUserId IS NULL
    BEGIN
        PRINT N'  MISSING USER (skip): ' + @MapUsername + N' for role ' + @MapRoleName;
        SET @Missing = @Missing + 1;
    END
    ELSE IF @MapRoleId IS NULL
        PRINT N'  MISSING ROLE (skip): ' + @MapRoleName;
    ELSE IF EXISTS (
        SELECT 1 FROM system.[User] u
        CROSS APPLY OPENJSON(u.RoleIds) j
        WHERE u.Id = @MapUserId AND TRY_CAST(j.value AS bigint) = @MapRoleId
    )
        SET @SkippedAlready = @SkippedAlready + 1;
    ELSE
    BEGIN
        UPDATE system.[User]
        SET
            RoleIds = CASE
                WHEN RoleIds IS NULL OR LTRIM(RTRIM(RoleIds)) = N'' OR RoleIds = N'[]'
                    THEN N'[' + CAST(@MapRoleId AS nvarchar(20)) + N']'
                ELSE STUFF(RoleIds, LEN(RoleIds), 1, N',' + CAST(@MapRoleId AS nvarchar(20)) + N']')
            END,
            Roles = CASE
                WHEN EXISTS (SELECT 1 FROM OPENJSON(Roles) j WHERE j.value = @MapRoleName) THEN Roles
                WHEN Roles IS NULL OR LTRIM(RTRIM(Roles)) = N'' OR Roles = N'[]'
                    THEN N'["' + @MapRoleName + N'"]'
                ELSE STUFF(Roles, LEN(Roles), 1, N',"' + @MapRoleName + N'"]')
            END,
            ModifiedById = 1,
            ModifiedByName = N'seed-poi-dataprofiles',
            ModifiedDateMiladiDateTime = GETDATE()
        WHERE Id = @MapUserId;
        SET @Assigned = @Assigned + 1;
    END

    SET @MapUserId = NULL;
    SET @MapRoleId = NULL;
    FETCH NEXT FROM map_cur INTO @MapUsername, @MapRoleName;
END
CLOSE map_cur;
DEALLOCATE map_cur;

PRINT N'  User role assignments applied: ' + CAST(@Assigned AS nvarchar(20));
PRINT N'  Already had role (skipped): ' + CAST(@SkippedAlready AS nvarchar(20));
PRINT N'  Missing users (logged): ' + CAST(@Missing AS nvarchar(20));

PRINT N'=== [5/5] Row-level WHERE for listinfo / cartable / ViewActive / Related ===';
/*
  موتور Query وقتی CustomQuery پر باشد Filters JSON را نادیده می‌گیرد؛ شرط باید داخل CustomQuery باشد.
  HTS GetOriginalGridData:375 — غیر ShowAll فقط اقلام با آغاز فرآیند (State=3).
  اطلاعات ما (104) برای صنایع: فقط کارتابل صنایع (1904/1905)؛ ثبت اولیه و بقیه وضعیت‌ها نه.
*/
DECLARE @BaseCustomQuery nvarchar(max) = @BaseCustomQueryForCopy;

IF @BaseCustomQuery IS NULL OR LEN(@BaseCustomQuery) = 0
BEGIN
    RAISERROR(N'CustomQuery نمایه پایه (Id=104) خالی است. Aborting.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END

DECLARE @WhereViewActive nvarchar(max) = N'
WHERE [t1].[IsDeleted] = 0 AND [t1].[IsLatestVersion] = 1
  AND [t2].[State] = 3';

DECLARE @WhereRelated nvarchar(max) = N'
WHERE [t1].[IsDeleted] = 0 AND [t1].[IsLatestVersion] = 1
  AND [t2].[State] = 3
  AND (
        [t1].[CreatedById]  = TRY_CAST(@CurrentUserId AS BIGINT)
     OR [t1].[ModifiedById] = TRY_CAST(@CurrentUserId AS BIGINT)
     OR [t2].[CreatedById]  = TRY_CAST(@CurrentUserId AS BIGINT)
     OR [t2].[SalesExpertId] = TRY_CAST(@CurrentUserId AS BIGINT)
     OR [t2].[SalesManagerId] = TRY_CAST(@CurrentUserId AS BIGINT)
     OR [t2].[AlternativeExpertId] = TRY_CAST(@CurrentUserId AS BIGINT)
     OR [t2].[IntroducerExpertId] = TRY_CAST(@CurrentUserId AS BIGINT)
     OR [t2].[ProjectManagerId] = TRY_CAST(@CurrentUserId AS BIGINT)
  )';

DECLARE @WhereIndustrial nvarchar(max) = N'
WHERE [t1].[IsDeleted] = 0 AND [t1].[IsLatestVersion] = 1
  AND [t1].[CheckStatus] IN (1904, 1905)';

DECLARE @FiltersIndustrial nvarchar(max) = N'[{"Id":"id_poi_isdel","TableName":"Sale.ProductionOrderItem","ColumnName":"IsDeleted","DataType":"bit","Operator":"equals","Value":"false","LogicalOperator":""},{"Id":"id_poi_islat","TableName":"Sale.ProductionOrderItem","ColumnName":"IsLatestVersion","DataType":"bit","Operator":"equals","Value":"true","LogicalOperator":"AND"},{"Id":"id_poi_chk","TableName":"Sale.ProductionOrderItem","ColumnName":"CheckStatus","DataType":"int","Operator":"in","Value":"1904,1905","LogicalOperator":"AND"}]';

DECLARE @WhereAllActive nvarchar(max) = N'
WHERE [t1].[IsDeleted] = 0 AND [t1].[IsLatestVersion] = 1';

DECLARE @WhereEngMech nvarchar(max) = N'
WHERE [t1].[IsDeleted] = 0 AND [t1].[IsLatestVersion] = 1
  AND [t1].[CheckStatus] = 2195
  AND ([t1].[DeviceType] IS NULL OR [t1].[DeviceType] NOT IN (2403, 2404, 2212, 2211))';

DECLARE @WhereEngElec nvarchar(max) = N'
WHERE [t1].[IsDeleted] = 0 AND [t1].[IsLatestVersion] = 1
  AND [t1].[CheckStatus] = 2195
  AND [t1].[DeviceType] IN (2403, 2404, 2212, 2211)';

DECLARE @WherePM nvarchar(max) = N'
WHERE [t1].[IsDeleted] = 0 AND [t1].[IsLatestVersion] = 1
  AND [t1].[CheckStatus] = 2258';

DECLARE @WhereInquiry nvarchar(max) = N'
WHERE [t1].[IsDeleted] = 0 AND [t1].[IsLatestVersion] = 1
  AND [t1].[CheckStatus] = 1906';

DECLARE @WhereMinistry nvarchar(max) = N'
WHERE [t1].[IsDeleted] = 0 AND [t1].[IsLatestVersion] = 1
  AND [t1].[CheckStatus] = 1908';

DECLARE @WhereCommittee nvarchar(max) = N'
WHERE [t1].[IsDeleted] = 0 AND [t1].[IsLatestVersion] = 1
  AND [t1].[CheckStatus] IN (1909, 2202)';

DECLARE @WhereHistory nvarchar(max) = N'
WHERE [t1].[IsDeleted] = 1';

UPDATE q
SET q.QueryJson = JSON_MODIFY(q.QueryJson, '$.CustomQuery', @BaseCustomQuery + @WhereViewActive),
    q.ModifiedById = 1, q.ModifiedByName = N'seed-poi-dataprofiles', q.ModifiedDateMiladiDateTime = GETDATE()
FROM system.SavedQuery q
WHERE q.Name = N'POI_ViewActive';
PRINT N'  POI_ViewActive: WHERE سربرگ تایید مالی (State=3) اعمال شد';

UPDATE q
SET q.QueryJson = JSON_MODIFY(q.QueryJson, '$.CustomQuery', @BaseCustomQuery + @WhereRelated),
    q.ModifiedById = 1, q.ModifiedByName = N'seed-poi-dataprofiles', q.ModifiedDateMiladiDateTime = GETDATE()
FROM system.SavedQuery q
WHERE q.Name = N'POI_Sales_Related_Active';
PRINT N'  POI_Sales_Related_Active: WHERE کاربر جاری (ایجاد/ویرایش‌کننده، تیم فروش، مدیر پروژه) اعمال شد';

UPDATE q
SET q.QueryJson = JSON_MODIFY(
        JSON_MODIFY(q.QueryJson, '$.CustomQuery', @BaseCustomQuery + @WhereIndustrial),
        '$.Filters', JSON_QUERY(@FiltersIndustrial)),
    q.ModifiedById = 1, q.ModifiedByName = N'seed-poi-dataprofiles', q.ModifiedDateMiladiDateTime = GETDATE()
FROM system.SavedQuery q
WHERE q.Name = N'productionorderitem_listinfonew';
PRINT N'  productionorderitem_listinfonew (اطلاعات ما): WHERE کارتابل صنایع (1904/1905) اعمال شد';

UPDATE q
SET q.QueryJson = JSON_MODIFY(q.QueryJson, '$.CustomQuery', @BaseCustomQuery + @WhereIndustrial),
    q.ModifiedById = 1, q.ModifiedByName = N'seed-poi-dataprofiles', q.ModifiedDateMiladiDateTime = GETDATE()
FROM system.SavedQuery q
WHERE q.Name = N'POI_Cartable_Industrial';
PRINT N'  POI_Cartable_Industrial: WHERE روی CustomQuery اعمال شد';

UPDATE q SET q.QueryJson = JSON_MODIFY(q.QueryJson, '$.CustomQuery', @BaseCustomQuery + @WhereAllActive), q.ModifiedById = 1, q.ModifiedByName = N'seed-poi-dataprofiles', q.ModifiedDateMiladiDateTime = GETDATE() FROM system.SavedQuery q WHERE q.Name = N'POI_All_Active';
UPDATE q SET q.QueryJson = JSON_MODIFY(q.QueryJson, '$.CustomQuery', @BaseCustomQuery + @WhereEngMech), q.ModifiedById = 1, q.ModifiedByName = N'seed-poi-dataprofiles', q.ModifiedDateMiladiDateTime = GETDATE() FROM system.SavedQuery q WHERE q.Name = N'POI_Cartable_Engineering_Mechanical';
UPDATE q SET q.QueryJson = JSON_MODIFY(q.QueryJson, '$.CustomQuery', @BaseCustomQuery + @WhereEngElec), q.ModifiedById = 1, q.ModifiedByName = N'seed-poi-dataprofiles', q.ModifiedDateMiladiDateTime = GETDATE() FROM system.SavedQuery q WHERE q.Name = N'POI_Cartable_Engineering_Electrical';
UPDATE q SET q.QueryJson = JSON_MODIFY(q.QueryJson, '$.CustomQuery', @BaseCustomQuery + @WherePM), q.ModifiedById = 1, q.ModifiedByName = N'seed-poi-dataprofiles', q.ModifiedDateMiladiDateTime = GETDATE() FROM system.SavedQuery q WHERE q.Name = N'POI_Cartable_ProjectManager';
UPDATE q SET q.QueryJson = JSON_MODIFY(q.QueryJson, '$.CustomQuery', @BaseCustomQuery + @WhereInquiry), q.ModifiedById = 1, q.ModifiedByName = N'seed-poi-dataprofiles', q.ModifiedDateMiladiDateTime = GETDATE() FROM system.SavedQuery q WHERE q.Name = N'POI_Cartable_Inquiry';
UPDATE q SET q.QueryJson = JSON_MODIFY(q.QueryJson, '$.CustomQuery', @BaseCustomQuery + @WhereMinistry), q.ModifiedById = 1, q.ModifiedByName = N'seed-poi-dataprofiles', q.ModifiedDateMiladiDateTime = GETDATE() FROM system.SavedQuery q WHERE q.Name = N'POI_Cartable_Ministry';
UPDATE q SET q.QueryJson = JSON_MODIFY(q.QueryJson, '$.CustomQuery', @BaseCustomQuery + @WhereCommittee), q.ModifiedById = 1, q.ModifiedByName = N'seed-poi-dataprofiles', q.ModifiedDateMiladiDateTime = GETDATE() FROM system.SavedQuery q WHERE q.Name = N'POI_Cartable_SupplyCommittee';
UPDATE q SET q.QueryJson = JSON_MODIFY(q.QueryJson, '$.CustomQuery', @BaseCustomQuery + @WhereHistory), q.ModifiedById = 1, q.ModifiedByName = N'seed-poi-dataprofiles', q.ModifiedDateMiladiDateTime = GETDATE() FROM system.SavedQuery q WHERE q.Name = N'POI_History_Deleted';
PRINT N'  سایر نمایه‌های کپی‌شده از 104: WHERE معادل Filters روی CustomQuery اعمال شد';

PRINT N'=== Summary ===';
SELECT Name, SavedQueryId AS Id FROM #PoiProfiles ORDER BY Name;
SELECT r.Id, r.Name
FROM system.Role r
WHERE r.Name LIKE N'Sale.ProductionOrder%'
ORDER BY r.Id;

COMMIT TRANSACTION;
PRINT N'=== DONE: Seed_ProductionOrderItem_DataProfiles committed ===';