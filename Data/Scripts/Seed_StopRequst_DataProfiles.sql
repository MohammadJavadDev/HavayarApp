/*
================================================================================
Seed_StopRequst_DataProfiles.sql
================================================================================
هدف: راه‌اندازی کامل RBAC و نمایه‌های داده (DataProfile) ماژول «توقفات تولید»
(Entities.App.Prd.StopRequst) شامل:
  [1] نقش‌ها (system.Role)                — شناسه‌های 200040 تا 200045
  [2] نمایه‌های داده (system.SavedQuery)   — Type=DataProfile(1), Mode=Query(1)
  [3] دسترسی نقش↔نمایه (system.RoleAccess) — ActionAccessType = DataProfile(3)
  [4] دسترسی نقش↔اکشن کنترلر (system.RoleAccess) — ActionAccessType = View(1)/Api(2)
  [5] راهنمای متنی افزودن دستی منو (system.SystemMenu) — فقط PRINT، بدون تغییر DB

پیش‌نیاز مهم (Dependency):
  این اسکریپت به ستون‌های جدید Prd.StopRequst (از جمله LastStatus، Description،
  ConformityCheckRequestNumber، IsNeedToCftTeam، IsEffective و ...) ارجاع می‌دهد
  که توسط Migration زیر افزوده می‌شوند:
      Data/Migrations/ApplicationDb/20260729114749_ExpandStopRequstModule.cs
  در زمان نگارش این اسکریپت، این Migration هنوز روی دیتابیس اجرا نشده بود.
  حتماً پیش از اجرای این Seed، Migrationهای EF Core را تا آخر اعمال کنید،
  در غیر این صورت اجرای این اسکریپت با خطای «Invalid column name» متوقف می‌شود.

مفروضات مستندشده (چون در Requirements صراحتاً مشخص نشده بود):
  - ستون Prd.StopRequst.ResponsibleUserIds به‌صورت رشته Comma-Delimited
    (مثل ",12,34,56,") در نظر گرفته شده - دقیقاً طبق دستورالعمل داده‌شده در
    Requirements (نه JSON Array مثل system.User.RoleIds). اگر بعداً مشخص شد
    این فیلد در واقع JSON Array ذخیره می‌شود، عبارت‌های LIKE زیر باید با
    OPENJSON جایگزین شوند (نقاطی که با کامنت «ResponsibleUserIds-Match»
    مشخص شده‌اند).
  - اکشن‌های SaveAttachment/DeleteAttachment در جدول Requirements به هیچ نقشی
    صراحتاً نگاشت نشده بودند؛ در این اسکریپت به Requester + ProductionExpert +
    ResponsibleUsers + ShowAll داده شده‌اند (هر نقشی که امکان ویرایش رکورد را
    در یکی از مراحل دارد). در صورت نیاز، بعداً از طریق UI قابل اصلاح است.
  - اکشن Delete (که در StopRequstController وجود دارد) در Requirements به هیچ
    نقشی نگاشت نشده بود؛ لذا در این اسکریپت RoleAccess ای برایش ثبت نمی‌شود
    (صرفاً از طریق پنل ادمین/UI قابل تخصیص است).
  - فیلد ModifiedDateShamsiDateTime/CreatedOnShamsiDateTime برای رکوردهای این
    اسکریپت (Role/SavedQuery/RoleAccess) با یک مقدار placeholder میلادی
    (CONVERT با style 120) پر می‌شود - دقیقاً همان قراردادی که در
    Seed_ProductionOrderItem_MyCartable_DataProfile.sql استفاده شده (این
    فیلدها صرفاً برای نمایش ادمین هستند و روی منطق برنامه اثر ندارند).

اکشن‌های کنترلر StopRequstController که هم‌اکنون در کد وجود دارند: List, Edit,
New, FetchData, Save, Add, Update, Delete, ExportToExcel. اکشن‌های
ManagementList/ManagementEdit/ReportList/Expert*/Responsible*/SendToCft/
CftOperate/Effectivity*/QcManager*/SaveAttachment/DeleteAttachment هنوز در
کنترلر پیاده‌سازی نشده‌اند (بخشی از ماژول «مدیریت توقفات» که در حال توسعه
است). با این‌حال، الگوی واقعی Path برای RoleAccess نوع View/Api در این پروژه
(بررسی‌شده روی نقش‌های موجود ProductionOrderItem) به‌صورت زیر است:
      /panel/{controller-lowercase}/{action-lowercase}[/{id}]
این اسکریپت طبق همین الگوی واقعی، RoleAccess اکشن‌های بالا را از هم‌اکنون
seed می‌کند تا به‌محض پیاده‌سازی کنترلر، دسترسی‌ها آماده باشند.

این اسکریپت کاملاً Idempotent است (Upsert بر اساس Name/Path+Role) و اجرای
مکرر آن بی‌خطر است.
================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

BEGIN TRY

    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-stoprequst-dataprofiles';
    DECLARE @EntityFullName NVARCHAR(200) = N'Entities.App.Prd.StopRequst';
    DECLARE @Q CHAR(1) = CHAR(39); -- کاراکتر تک‌کوتیشن، برای ساخت امن رشته‌های حاوی ' بدون اشتباه در Escaping دستی

    -----------------------------------------------------------------------------
    PRINT N'=== [1/6] اطمینان از وجود نقش‌ها (system.Role) - شناسه‌های 200040..200045 ===';
    -----------------------------------------------------------------------------

    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Prd.StopRequst.Requester')
    BEGIN
        SET IDENTITY_INSERT system.Role ON;
        INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (200040, N'Prd.StopRequst.Requester', N'تولید - درخواست توقف - درخواست‌کننده', 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
        SET IDENTITY_INSERT system.Role OFF;
        PRINT N'  CREATED Role Id=200040 Name=Prd.StopRequst.Requester';
    END
    ELSE
        PRINT N'  EXISTS Role Name=Prd.StopRequst.Requester';

    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Prd.StopsManagement.ProductionExpert')
    BEGIN
        SET IDENTITY_INSERT system.Role ON;
        INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (200041, N'Prd.StopsManagement.ProductionExpert', N'تولید - مدیریت توقفات - کارشناس تولید', 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
        SET IDENTITY_INSERT system.Role OFF;
        PRINT N'  CREATED Role Id=200041 Name=Prd.StopsManagement.ProductionExpert';
    END
    ELSE
        PRINT N'  EXISTS Role Name=Prd.StopsManagement.ProductionExpert';

    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Prd.StopsManagement.ResponsibleUsers')
    BEGIN
        SET IDENTITY_INSERT system.Role ON;
        INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (200042, N'Prd.StopsManagement.ResponsibleUsers', N'تولید - مدیریت توقفات - مسئول عامل', 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
        SET IDENTITY_INSERT system.Role OFF;
        PRINT N'  CREATED Role Id=200042 Name=Prd.StopsManagement.ResponsibleUsers';
    END
    ELSE
        PRINT N'  EXISTS Role Name=Prd.StopsManagement.ResponsibleUsers';

    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Prd.StopsManagement.CftTeam')
    BEGIN
        SET IDENTITY_INSERT system.Role ON;
        INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (200043, N'Prd.StopsManagement.CftTeam', N'تولید - مدیریت توقفات - تیم CFT', 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
        SET IDENTITY_INSERT system.Role OFF;
        PRINT N'  CREATED Role Id=200043 Name=Prd.StopsManagement.CftTeam';
    END
    ELSE
        PRINT N'  EXISTS Role Name=Prd.StopsManagement.CftTeam';

    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Prd.StopsManagement.QcManager')
    BEGIN
        SET IDENTITY_INSERT system.Role ON;
        INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (200044, N'Prd.StopsManagement.QcManager', N'تولید - مدیریت توقفات - مدیر QC', 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
        SET IDENTITY_INSERT system.Role OFF;
        PRINT N'  CREATED Role Id=200044 Name=Prd.StopsManagement.QcManager';
    END
    ELSE
        PRINT N'  EXISTS Role Name=Prd.StopsManagement.QcManager';

    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Prd.StopsManagement.ShowAll')
    BEGIN
        SET IDENTITY_INSERT system.Role ON;
        INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (200045, N'Prd.StopsManagement.ShowAll', N'تولید - مدیریت توقفات - مشاهده همه', 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
        SET IDENTITY_INSERT system.Role OFF;
        PRINT N'  CREATED Role Id=200045 Name=Prd.StopsManagement.ShowAll';
    END
    ELSE
        PRINT N'  EXISTS Role Name=Prd.StopsManagement.ShowAll';

    -----------------------------------------------------------------------------
    PRINT N'=== [2/6] ساخت اجزای مشترک: ColumnsJson / SELECT پایه / اسکلت QueryJson ===';
    -----------------------------------------------------------------------------

    -- SELECT + FROM + JOIN مشترک بین همه نمایه‌ها (alias t1=StopRequst, t2=ProductionOrderItem, t3=ProductionOrder)
    DECLARE @BaseSelect NVARCHAR(MAX) = N'
SELECT
    [t1].[Id] AS [t1_Id],
    [t1].[LastStatus] AS [t1_LastStatus],
    [t1].[StopStartShamsiDateTime] AS [t1_StopStartShamsiDateTime],
    [t1].[StopStartMiladiDateTime] AS [t1_StopStartMiladiDateTime],
    [t1].[ObservationLocation] AS [t1_ObservationLocation],
    [t1].[ProductionStatus] AS [t1_ProductionStatus],
    [t1].[OperatingHours] AS [t1_OperatingHours],
    [t1].[StopReasonTitles] AS [t1_StopReasonTitles],
    [t1].[ResponsibleUnitTitles] AS [t1_ResponsibleUnitTitles],
    [t1].[ResponsibleUserNames] AS [t1_ResponsibleUserNames],
    [t1].[ResponsibleUserIds] AS [t1_ResponsibleUserIds],
    [t1].[BeneficiarieUserNames] AS [t1_BeneficiarieUserNames],
    [t1].[Description] AS [t1_Description],
    [t1].[ConformityCheckRequestNumber] AS [t1_ConformityCheckRequestNumber],
    [t1].[CreatedById] AS [t1_CreatedById],
    [t1].[CreatedByName] AS [t1_CreatedByName],
    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime],
    [t1].[CreatedOnMiladiDateTime] AS [t1_CreatedOnMiladiDateTime],
    [t1].[IsActive] AS [t1_IsActive],
    [t2].[Serial] AS [t2_Serial],
    [t3].[ProductionOrderNumber] AS [t3_ProductionOrderNumber]
FROM [Prd].[StopRequst] AS [t1]
LEFT JOIN [Sale].[ProductionOrderItem] AS [t2] ON [t1].[ProductionOrderItemId] = [t2].[Id]
LEFT JOIN [Sale].[ProductionOrder] AS [t3] ON [t2].[ProductionOrderId] = [t3].[Id]';

    -- ColumnsJson مشترک بین همه نمایه‌ها (مطابق ساختار QueryColumn / الگوی SavedQuery Id=104)
    DECLARE @ColumnsJson NVARCHAR(MAX) = N'[
{"TableName":"Prd.StopRequst","ColumnName":"Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":1,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Prd.StopRequst","ColumnName":"LastStatus","DisplayName":"وضعیت","Alliance":"t1_LastStatus","Address":"[t1].[LastStatus]","SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Prd.Enums.StopRequstModuleStatusEnum"},"Aggregate":null,"Render":"function(data, type, row) { return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":220,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Prd.StopRequst","ColumnName":"StopStartShamsiDateTime","DisplayName":"تاریخ شروع توقف","Alliance":"t1_StopStartShamsiDateTime","Address":"[t1].[StopStartShamsiDateTime]","SystemTypeName":"DateTimeShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":4,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Prd.StopRequst","ColumnName":"StopStartMiladiDateTime","DisplayName":"تاریخ شروع توقف (میلادی)","Alliance":"t1_StopStartMiladiDateTime","Address":"[t1].[StopStartMiladiDateTime]","SystemTypeName":"DateTime","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":2,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":false,"Sortable":true,"ClassName":""},
{"TableName":"Prd.StopRequst","ColumnName":"ObservationLocation","DisplayName":"محل مشاهده","Alliance":"t1_ObservationLocation","Address":"[t1].[ObservationLocation]","SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Prd.Enums.StopRequstObservationLocationEnum"},"Aggregate":null,"Render":"function(data, type, row) { return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Prd.StopRequst","ColumnName":"ProductionStatus","DisplayName":"وضعیت تولید","Alliance":"t1_ProductionStatus","Address":"[t1].[ProductionStatus]","SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Prd.Enums.StopRequstProductionStatusEnum"},"Aggregate":null,"Render":"function(data, type, row) { return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":220,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Prd.StopRequst","ColumnName":"OperatingHours","DisplayName":"ساعت کارکرد دستگاه","Alliance":"t1_OperatingHours","Address":"[t1].[OperatingHours]","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Prd.StopRequst","ColumnName":"StopReasonTitles","DisplayName":"علت توقف","Alliance":"t1_StopReasonTitles","Address":"[t1].[StopReasonTitles]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":220,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Prd.StopRequst","ColumnName":"ResponsibleUnitTitles","DisplayName":"واحدهای عامل توقف","Alliance":"t1_ResponsibleUnitTitles","Address":"[t1].[ResponsibleUnitTitles]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":220,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Prd.StopRequst","ColumnName":"ResponsibleUserNames","DisplayName":"مسئولان عامل توقف","Alliance":"t1_ResponsibleUserNames","Address":"[t1].[ResponsibleUserNames]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Prd.StopRequst","ColumnName":"ResponsibleUserIds","DisplayName":"شناسه‌های مسئولان عامل توقف","Alliance":"t1_ResponsibleUserIds","Address":"[t1].[ResponsibleUserIds]","SystemTypeName":"String","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":false,"Sortable":false,"ClassName":""},
{"TableName":"Prd.StopRequst","ColumnName":"BeneficiarieUserNames","DisplayName":"ذینفعان","Alliance":"t1_BeneficiarieUserNames","Address":"[t1].[BeneficiarieUserNames]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Prd.StopRequst","ColumnName":"Description","DisplayName":"شرح","Alliance":"t1_Description","Address":"[t1].[Description]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":260,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Prd.StopRequst","ColumnName":"ConformityCheckRequestNumber","DisplayName":"شماره عدم انطباق","Alliance":"t1_ConformityCheckRequestNumber","Address":"[t1].[ConformityCheckRequestNumber]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Prd.StopRequst","ColumnName":"CreatedById","DisplayName":"شناسه ایجادکننده","Alliance":"t1_CreatedById","Address":"[t1].[CreatedById]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":false,"Sortable":false,"ClassName":""},
{"TableName":"Prd.StopRequst","ColumnName":"CreatedByName","DisplayName":"نام ایجادکننده","Alliance":"t1_CreatedByName","Address":"[t1].[CreatedByName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Prd.StopRequst","ColumnName":"CreatedOnShamsiDateTime","DisplayName":"تاریخ ایجاد","Alliance":"t1_CreatedOnShamsiDateTime","Address":"[t1].[CreatedOnShamsiDateTime]","SystemTypeName":"DateTimeShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":4,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data) { return data.split(\" \")[0]; } return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Prd.StopRequst","ColumnName":"CreatedOnMiladiDateTime","DisplayName":"تاریخ ایجاد (میلادی)","Alliance":"t1_CreatedOnMiladiDateTime","Address":"[t1].[CreatedOnMiladiDateTime]","SystemTypeName":"DateTime","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":2,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":false,"Sortable":true,"ClassName":""},
{"TableName":"Prd.StopRequst","ColumnName":"IsActive","DisplayName":"وضعیت فعال بودن","Alliance":"t1_IsActive","Address":"[t1].[IsActive]","SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.Base.IsActiveEnum"},"Aggregate":null,"Render":"function(data, type, row) { return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrderItem","ColumnName":"Serial","DisplayName":"سریال قلم سفارش ساخت","Alliance":"t2_Serial","Address":"[t2].[Serial]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrder","ColumnName":"ProductionOrderNumber","DisplayName":"شماره سفارش ساخت","Alliance":"t3_ProductionOrderNumber","Address":"[t3].[ProductionOrderNumber]","SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""}
]';

    -- اسکلت خام QueryJson (مطابق کلاس QueryDesign) - CustomQuery/Parameters هر نمایه با JSON_MODIFY جایگزین می‌شود
    DECLARE @QueryJsonSkeleton NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}';

    -- عبارت تطبیق «آیا کاربر جاری یکی از مسئولان عامل توقف در ResponsibleUserIds است؟» (فرمت Comma-Delimited طبق Requirements)
    DECLARE @ResponsibleMatchExpr NVARCHAR(300) =
        N'(' + @Q + ',' + @Q + ' + ISNULL([t1].[ResponsibleUserIds], ' + @Q + @Q + ') + ' + @Q + ',' + @Q + ') LIKE ' + @Q + '%,' + @Q + ' + CAST(@CuId AS NVARCHAR(20)) + ' + @Q + ',%' + @Q;

    -- عبارت بررسی نقش admin (بر مبنای system.User.Roles که یک JSON Array از نام نقش‌هاست)
    DECLARE @AdminCheckExpr NVARCHAR(300) =
        N'CASE WHEN EXISTS (SELECT 1 FROM OPENJSON(@CuRoles) WHERE [value] = N' + @Q + 'admin' + @Q + ') THEN 1 ELSE 0 END';

    -----------------------------------------------------------------------------
    PRINT N'=== [3/6] Upsert نمایه‌های داده (system.SavedQuery) ===';
    -----------------------------------------------------------------------------

    IF OBJECT_ID('tempdb..#StopProfiles') IS NOT NULL DROP TABLE #StopProfiles;
    CREATE TABLE #StopProfiles
    (
        Name           NVARCHAR(100) NOT NULL PRIMARY KEY,
        Title          NVARCHAR(200) NOT NULL,
        CustomQuery    NVARCHAR(MAX) NOT NULL,
        ParametersJson NVARCHAR(MAX) NOT NULL DEFAULT (N'[]'),
        ActionOptions  NVARCHAR(MAX) NOT NULL DEFAULT (N'[]'),
        SavedQueryId   BIGINT NULL
    );

    DECLARE @ActionOptionsDefault NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
    DECLARE @ActionOptionsRequester NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';

    ----------------------------------------------------------------
    -- 3.1) StopRequst_Requester — درخواست توقف: درخواست‌کننده
    ----------------------------------------------------------------
    DECLARE @Query_Requester NVARCHAR(MAX) = N'
DECLARE @CuId BIGINT = TRY_CAST(@CurrentUserId AS BIGINT);
--!--mainsection
' + @BaseSelect + N'
WHERE
    [t1].[CreatedById] = @CuId
    AND [t1].[LastStatus] IN (2687, 2688, 2699) -- PreRegister, Edited, RegisteredInProductTest
';
    INSERT INTO #StopProfiles (Name, Title, CustomQuery, ParametersJson, ActionOptions)
    VALUES (N'StopRequst_Requester', N'درخواست توقف — درخواست‌کننده', @Query_Requester, N'[]', @ActionOptionsRequester);

    ----------------------------------------------------------------
    -- 3.2) Stop_ExpertCartable — کارتابل کارشناس تولید
    -- (دسترسی به این نمایه صرفاً از طریق RoleAccess به ProductionExpert/ShowAll داده می‌شود؛
    --  چون هیچ فیلد اختصاصیِ «مسئول من» در این مرحله دخیل نیست، WHERE نیازی به تفکیک نقش ندارد)
    ----------------------------------------------------------------
    DECLARE @Query_ExpertCartable NVARCHAR(MAX) = @BaseSelect + N'
WHERE
    [t1].[LastStatus] IN (2687, 2688, 2692, 2693, 2697)
    -- PreRegister, Edited, StopingFactorDisApprove, StopingFactorApprove, EffectivenessMeasuresDisApproved
';
    INSERT INTO #StopProfiles (Name, Title, CustomQuery, ParametersJson, ActionOptions)
    VALUES (N'Stop_ExpertCartable', N'کارتابل کارشناس تولید', @Query_ExpertCartable, N'[]', @ActionOptionsDefault);

    ----------------------------------------------------------------
    -- 3.3) Stop_ResponsibleCartable — کارتابل مسئول عامل
    ----------------------------------------------------------------
    DECLARE @Query_ResponsibleCartable NVARCHAR(MAX) = N'
DECLARE @CuId BIGINT = TRY_CAST(@CurrentUserId AS BIGINT);
DECLARE @CuRoles NVARCHAR(MAX);
DECLARE @CuRoleIds NVARCHAR(MAX);
SELECT @CuRoles = Roles, @CuRoleIds = RoleIds FROM system.[User] WHERE Id = @CuId;
DECLARE @IsAdmin BIT = ' + @AdminCheckExpr + N';
DECLARE @HasShowAll BIT = CASE WHEN @IsAdmin = 1 OR EXISTS (SELECT 1 FROM OPENJSON(@CuRoleIds) WHERE TRY_CAST([value] AS BIGINT) = 200045) THEN 1 ELSE 0 END;
--!--mainsection
' + @BaseSelect + N'
WHERE
    [t1].[LastStatus] IN (2691, 2699, 2701) -- ProductionExpertApprove, RegisteredInProductTest, CftLeadApproved
    AND (
        @HasShowAll = 1
        OR ' + @ResponsibleMatchExpr + N' -- ResponsibleUserIds-Match
    )
';
    INSERT INTO #StopProfiles (Name, Title, CustomQuery, ParametersJson, ActionOptions)
    VALUES (N'Stop_ResponsibleCartable', N'کارتابل مسئول عامل', @Query_ResponsibleCartable, N'[]', @ActionOptionsDefault);

    ----------------------------------------------------------------
    -- 3.4) Stop_CftCartable — کارتابل تیم CFT
    ----------------------------------------------------------------
    DECLARE @Query_CftCartable NVARCHAR(MAX) = @BaseSelect + N'
WHERE
    [t1].[LastStatus] = 2695 -- SendToCFTTeam
';
    INSERT INTO #StopProfiles (Name, Title, CustomQuery, ParametersJson, ActionOptions)
    VALUES (N'Stop_CftCartable', N'کارتابل تیم CFT', @Query_CftCartable, N'[]', @ActionOptionsDefault);

    ----------------------------------------------------------------
    -- 3.5) Stop_QcCartable — کارتابل مدیر QC
    ----------------------------------------------------------------
    DECLARE @Query_QcCartable NVARCHAR(MAX) = @BaseSelect + N'
WHERE
    [t1].[LastStatus] = 2737 -- CheckByQcManager
';
    INSERT INTO #StopProfiles (Name, Title, CustomQuery, ParametersJson, ActionOptions)
    VALUES (N'Stop_QcCartable', N'کارتابل مدیر QC', @Query_QcCartable, N'[]', @ActionOptionsDefault);

    ----------------------------------------------------------------
    -- 3.6) Stop_MyCartable — کارتابل من: توقفات (اجتماع تمام شاخه‌های بالا بر مبنای نقش‌های واقعی کاربر)
    ----------------------------------------------------------------
    DECLARE @Query_MyCartable NVARCHAR(MAX) = N'
DECLARE @CuId BIGINT = TRY_CAST(@CurrentUserId AS BIGINT);
DECLARE @CuRoles NVARCHAR(MAX);
DECLARE @CuRoleIds NVARCHAR(MAX);
SELECT @CuRoles = Roles, @CuRoleIds = RoleIds FROM system.[User] WHERE Id = @CuId;
DECLARE @IsAdmin BIT = ' + @AdminCheckExpr + N';
DECLARE @HasShowAll BIT = CASE WHEN @IsAdmin = 1 OR EXISTS (SELECT 1 FROM OPENJSON(@CuRoleIds) WHERE TRY_CAST([value] AS BIGINT) = 200045) THEN 1 ELSE 0 END;
DECLARE @HasRequester BIT = CASE WHEN EXISTS (SELECT 1 FROM OPENJSON(@CuRoleIds) WHERE TRY_CAST([value] AS BIGINT) = 200040) THEN 1 ELSE 0 END;
DECLARE @HasExpert BIT = CASE WHEN EXISTS (SELECT 1 FROM OPENJSON(@CuRoleIds) WHERE TRY_CAST([value] AS BIGINT) = 200041) THEN 1 ELSE 0 END;
DECLARE @HasResponsible BIT = CASE WHEN EXISTS (SELECT 1 FROM OPENJSON(@CuRoleIds) WHERE TRY_CAST([value] AS BIGINT) = 200042) THEN 1 ELSE 0 END;
DECLARE @HasCft BIT = CASE WHEN EXISTS (SELECT 1 FROM OPENJSON(@CuRoleIds) WHERE TRY_CAST([value] AS BIGINT) = 200043) THEN 1 ELSE 0 END;
DECLARE @HasQc BIT = CASE WHEN EXISTS (SELECT 1 FROM OPENJSON(@CuRoleIds) WHERE TRY_CAST([value] AS BIGINT) = 200044) THEN 1 ELSE 0 END;
--!--mainsection
' + @BaseSelect + N'
WHERE
    (
        -- کارتابل درخواست‌کننده: درخواست‌های خودم که هنوز در چرخه ثبت/ویرایش/تست هستند
        (
            (@HasRequester = 1 OR @HasShowAll = 1)
            AND [t1].[CreatedById] = @CuId
            AND [t1].[LastStatus] IN (2687, 2688, 2699)
        )

        -- کارتابل کارشناس تولید
        OR (
            (@HasExpert = 1 OR @HasShowAll = 1)
            AND [t1].[LastStatus] IN (2687, 2688, 2692, 2693, 2697)
        )

        -- کارتابل مسئول عامل توقف (فقط رکوردهای اختصاص‌یافته به خودم، مگر ShowAll)
        OR (
            (@HasResponsible = 1 OR @HasShowAll = 1)
            AND [t1].[LastStatus] IN (2691, 2699, 2701)
            AND (
                @HasShowAll = 1
                OR ' + @ResponsibleMatchExpr + N' -- ResponsibleUserIds-Match
            )
        )

        -- کارتابل تیم CFT
        OR (
            (@HasCft = 1 OR @HasShowAll = 1)
            AND [t1].[LastStatus] = 2695
        )

        -- کارتابل مدیر کنترل کیفیت
        OR (
            (@HasQc = 1 OR @HasShowAll = 1)
            AND [t1].[LastStatus] = 2737
        )
    )
';
    INSERT INTO #StopProfiles (Name, Title, CustomQuery, ParametersJson, ActionOptions)
    VALUES (N'Stop_MyCartable', N'کارتابل من — توقفات', @Query_MyCartable, N'[]', @ActionOptionsDefault);

    ----------------------------------------------------------------
    -- 3.7) Stop_Report — گزارش توقفات (همه رکوردهای غیرحذف‌شده + فیلتر بازه تاریخ اختیاری)
    ----------------------------------------------------------------
    DECLARE @Query_Report NVARCHAR(MAX) = @BaseSelect + N'
WHERE
    ([t1].[LastStatus] IS NULL OR [t1].[LastStatus] <> 2689) -- به‌جز Deleted
    AND (@FromDate IS NULL OR [t1].[CreatedOnMiladiDateTime] >= @FromDate)
    AND (@ToDate IS NULL OR [t1].[CreatedOnMiladiDateTime] < DATEADD(day, 1, @ToDate))
';
    DECLARE @ReportParametersJson NVARCHAR(MAX) = N'[{"Id":"id_stopreq_fromdate","Name":"FromDate","DisplayName":"از تاریخ ایجاد","DefaultValue":null,"DataType":"datetime"},{"Id":"id_stopreq_todate","Name":"ToDate","DisplayName":"تا تاریخ ایجاد","DefaultValue":null,"DataType":"datetime"}]';
    INSERT INTO #StopProfiles (Name, Title, CustomQuery, ParametersJson, ActionOptions)
    VALUES (N'Stop_Report', N'گزارش توقفات', @Query_Report, @ReportParametersJson, @ActionOptionsDefault);

    ----------------------------------------------------------------
    -- 3.8) حلقه Upsert روی system.SavedQuery
    ----------------------------------------------------------------
    DECLARE @PName NVARCHAR(100), @PTitle NVARCHAR(200), @PCustomQuery NVARCHAR(MAX), @PParametersJson NVARCHAR(MAX), @PActionOptions NVARCHAR(MAX), @PQueryJson NVARCHAR(MAX), @PId BIGINT;

    DECLARE stop_profile_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT Name, Title, CustomQuery, ParametersJson, ActionOptions FROM #StopProfiles;
    OPEN stop_profile_cur;
    FETCH NEXT FROM stop_profile_cur INTO @PName, @PTitle, @PCustomQuery, @PParametersJson, @PActionOptions;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @PQueryJson = JSON_MODIFY(@QueryJsonSkeleton, '$.CustomQuery', @PCustomQuery);
        SET @PQueryJson = JSON_MODIFY(@PQueryJson, '$.Parameters', JSON_QUERY(@PParametersJson));

        IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @PName)
        BEGIN
            UPDATE system.SavedQuery
            SET Title = @PTitle,
                QueryJson = @PQueryJson,
                ColumnsJson = @ColumnsJson,
                EntityFullName = @EntityFullName,
                Mode = 1,
                Type = 1,
                ActionOptions = @PActionOptions,
                ModifiedById = 1,
                ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now,
                ModifiedDateShamsiDateTime = @NowShamsi,
                IsActive = 1
            WHERE Name = @PName;

            SELECT @PId = Id FROM system.SavedQuery WHERE Name = @PName;
            PRINT N'  UPDATED DataProfile: ' + @PName + N' (Id=' + CAST(@PId AS NVARCHAR(20)) + N')';
        END
        ELSE
        BEGIN
            INSERT INTO system.SavedQuery
                (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
                 Mode, Type, EntityFullName, ActionOptions,
                 CreatedById, ModifiedById, CreatedByName, ModifiedByName,
                 CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
                 IsActive)
            VALUES
                (@PName, @PTitle, @PQueryJson, @ColumnsJson, N'{}', N'[]', NULL,
                 1, 1, @EntityFullName, @PActionOptions,
                 1, 1, @SeedUser, @SeedUser,
                 @Now, @NowShamsi, @Now, @NowShamsi,
                 1);

            SET @PId = SCOPE_IDENTITY();
            PRINT N'  CREATED DataProfile: ' + @PName + N' (Id=' + CAST(@PId AS NVARCHAR(20)) + N')';
        END

        UPDATE #StopProfiles SET SavedQueryId = @PId WHERE Name = @PName;

        FETCH NEXT FROM stop_profile_cur INTO @PName, @PTitle, @PCustomQuery, @PParametersJson, @PActionOptions;
    END
    CLOSE stop_profile_cur;
    DEALLOCATE stop_profile_cur;

    -----------------------------------------------------------------------------
    PRINT N'=== [4/6] بازسازی RoleAccess برای نمایه‌های داده (ActionAccessType=DataProfile) ===';
    -----------------------------------------------------------------------------

    DELETE ra
    FROM system.RoleAccess ra
    INNER JOIN #StopProfiles p ON p.SavedQueryId = ra.RowId
    WHERE ra.ActionAccessType = 3; -- DataProfile
    PRINT N'  حذف RoleAccess قبلی نمایه‌های ماژول توقف: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    IF OBJECT_ID('tempdb..#StopProfileRoleAccess') IS NOT NULL DROP TABLE #StopProfileRoleAccess;
    CREATE TABLE #StopProfileRoleAccess (ProfileName NVARCHAR(100) NOT NULL, RoleName NVARCHAR(200) NOT NULL);

    INSERT INTO #StopProfileRoleAccess (ProfileName, RoleName) VALUES
        (N'StopRequst_Requester',     N'Prd.StopRequst.Requester'),
        (N'StopRequst_Requester',     N'Prd.StopsManagement.ShowAll'),
        (N'Stop_ExpertCartable',      N'Prd.StopsManagement.ProductionExpert'),
        (N'Stop_ExpertCartable',      N'Prd.StopsManagement.ShowAll'),
        (N'Stop_ResponsibleCartable', N'Prd.StopsManagement.ResponsibleUsers'),
        (N'Stop_ResponsibleCartable', N'Prd.StopsManagement.ShowAll'),
        (N'Stop_CftCartable',         N'Prd.StopsManagement.CftTeam'),
        (N'Stop_CftCartable',         N'Prd.StopsManagement.ShowAll'),
        (N'Stop_QcCartable',          N'Prd.StopsManagement.QcManager'),
        (N'Stop_QcCartable',          N'Prd.StopsManagement.ShowAll'),
        (N'Stop_MyCartable',          N'Prd.StopRequst.Requester'),
        (N'Stop_MyCartable',          N'Prd.StopsManagement.ProductionExpert'),
        (N'Stop_MyCartable',          N'Prd.StopsManagement.ResponsibleUsers'),
        (N'Stop_MyCartable',          N'Prd.StopsManagement.CftTeam'),
        (N'Stop_MyCartable',          N'Prd.StopsManagement.QcManager'),
        (N'Stop_MyCartable',          N'Prd.StopsManagement.ShowAll'),
        (N'Stop_Report',              N'Prd.StopsManagement.ShowAll'),
        (N'Stop_Report',              N'Prd.StopsManagement.ProductionExpert'),
        (N'Stop_Report',              N'Prd.StopsManagement.QcManager'),
        (N'Stop_Report',              N'Prd.StopRequst.Requester');

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
         IsActive)
    SELECT
        N'dataProfile_' + CAST(p.SavedQueryId AS NVARCHAR(20)),
        3,   -- DataProfile
        7,   -- ActionAccessItemType.DataProfile
        @EntityFullName,
        p.Title,
        NULL,
        p.SavedQueryId,
        r.Id,
        1, 1, @SeedUser, @SeedUser,
        @Now, @NowShamsi, @Now, @NowShamsi,
        1
    FROM #StopProfileRoleAccess map
    INNER JOIN #StopProfiles p ON p.Name = map.ProfileName
    INNER JOIN system.Role r ON r.Name = map.RoleName;
    PRINT N'  RoleAccess نمایه‌های داده درج شد: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    -----------------------------------------------------------------------------
    PRINT N'=== [5/6] بازسازی RoleAccess برای اکشن‌های کنترلر (View=1 / Api=2) ===';
    -----------------------------------------------------------------------------

    DELETE FROM system.RoleAccess
    WHERE ActionAccessType IN (1, 2)
      AND Path LIKE N'/panel/stoprequst/%';
    PRINT N'  حذف RoleAccess قبلی اکشن‌های کنترلر StopRequst: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    IF OBJECT_ID('tempdb..#StopActionAccess') IS NOT NULL DROP TABLE #StopActionAccess;
    CREATE TABLE #StopActionAccess
    (
        RoleName             NVARCHAR(200) NOT NULL,
        Path                 NVARCHAR(300) NOT NULL,
        ActionAccessType     INT           NOT NULL, -- 1=View, 2=Api
        ActionAccessItemType INT           NOT NULL
    );

    -- درخواست‌کننده: عملیات پایه CRUD روی درخواست توقف خودش (این اکشن‌ها هم‌اکنون در StopRequstController وجود دارند)
    INSERT INTO #StopActionAccess (RoleName, Path, ActionAccessType, ActionAccessItemType) VALUES
        (N'Prd.StopRequst.Requester', N'/panel/stoprequst/list',          1, 1), -- List
        (N'Prd.StopRequst.Requester', N'/panel/stoprequst/edit',          1, 5), -- Update(view)
        (N'Prd.StopRequst.Requester', N'/panel/stoprequst/new',           1, 4), -- Create(view)
        (N'Prd.StopRequst.Requester', N'/panel/stoprequst/fetchdata',     2, 2), -- FetchData
        (N'Prd.StopRequst.Requester', N'/panel/stoprequst/save',          2, 3), -- Save
        (N'Prd.StopRequst.Requester', N'/panel/stoprequst/add',           2, 4), -- Create
        (N'Prd.StopRequst.Requester', N'/panel/stoprequst/update',        2, 5), -- Update
        (N'Prd.StopRequst.Requester', N'/panel/stoprequst/exporttoexcel', 2, 0);

    -- کارشناس تولید + مشاهده همه: مدیریت گردش‌کار (اکشن‌های زیر هنوز در کنترلر پیاده‌سازی نشده‌اند - پیشاپیش seed می‌شود)
    INSERT INTO #StopActionAccess (RoleName, Path, ActionAccessType, ActionAccessItemType)
    SELECT r.RoleName, r.Path, r.ActionAccessType, r.ActionAccessItemType
    FROM (VALUES
        (N'/panel/stoprequst/managementlist',              1, 1),
        (N'/panel/stoprequst/managementedit',              1, 5),
        (N'/panel/stoprequst/fetchdata',                   2, 2),
        (N'/panel/stoprequst/expertconfirm/{id}',          2, 1000),
        (N'/panel/stoprequst/expertreject/{id}',           2, 1000),
        (N'/panel/stoprequst/sendtocft/{id}',              2, 1000),
        (N'/panel/stoprequst/effectivityapprove/{id}',     2, 1000),
        (N'/panel/stoprequst/effectivitydisapprove/{id}',  2, 1000),
        (N'/panel/stoprequst/exporttoexcel',                2, 0),
        (N'/panel/stoprequst/reportlist',                  1, 1)
    ) AS r(Path, ActionAccessType, ActionAccessItemType)
    CROSS JOIN (VALUES (N'Prd.StopsManagement.ProductionExpert'), (N'Prd.StopsManagement.ShowAll')) AS r2(RoleName);

    -- مسئول عامل توقف
    INSERT INTO #StopActionAccess (RoleName, Path, ActionAccessType, ActionAccessItemType) VALUES
        (N'Prd.StopsManagement.ResponsibleUsers', N'/panel/stoprequst/managementlist',         1, 1),
        (N'Prd.StopsManagement.ResponsibleUsers', N'/panel/stoprequst/managementedit',         1, 5),
        (N'Prd.StopsManagement.ResponsibleUsers', N'/panel/stoprequst/fetchdata',              2, 2),
        (N'Prd.StopsManagement.ResponsibleUsers', N'/panel/stoprequst/responsibleconfirm/{id}',2, 1000),
        (N'Prd.StopsManagement.ResponsibleUsers', N'/panel/stoprequst/responsiblereject/{id}', 2, 1000);

    -- تیم CFT
    INSERT INTO #StopActionAccess (RoleName, Path, ActionAccessType, ActionAccessItemType) VALUES
        (N'Prd.StopsManagement.CftTeam', N'/panel/stoprequst/managementlist',   1, 1),
        (N'Prd.StopsManagement.CftTeam', N'/panel/stoprequst/managementedit',   1, 5),
        (N'Prd.StopsManagement.CftTeam', N'/panel/stoprequst/fetchdata',        2, 2),
        (N'Prd.StopsManagement.CftTeam', N'/panel/stoprequst/cftoperate/{id}',  2, 1000);

    -- مدیر QC
    INSERT INTO #StopActionAccess (RoleName, Path, ActionAccessType, ActionAccessItemType) VALUES
        (N'Prd.StopsManagement.QcManager', N'/panel/stoprequst/managementlist',        1, 1),
        (N'Prd.StopsManagement.QcManager', N'/panel/stoprequst/managementedit',        1, 5),
        (N'Prd.StopsManagement.QcManager', N'/panel/stoprequst/fetchdata',             2, 2),
        (N'Prd.StopsManagement.QcManager', N'/panel/stoprequst/qcmanagerapprove/{id}', 2, 1000),
        (N'Prd.StopsManagement.QcManager', N'/panel/stoprequst/qcmanagerdisapprove/{id}', 2, 1000),
        (N'Prd.StopsManagement.QcManager', N'/panel/stoprequst/reportlist',            1, 1);

    -- پیوست‌ها (SaveAttachment/DeleteAttachment در Requirements به نقش خاصی نگاشت نشده بود - نگاه کنید به مفروضات ابتدای اسکریپت)
    INSERT INTO #StopActionAccess (RoleName, Path, ActionAccessType, ActionAccessItemType)
    SELECT r2.RoleName, r.Path, r.ActionAccessType, r.ActionAccessItemType
    FROM (VALUES
        (N'/panel/stoprequst/saveattachment',   2, 1000),
        (N'/panel/stoprequst/deleteattachment', 2, 1000)
    ) AS r(Path, ActionAccessType, ActionAccessItemType)
    CROSS JOIN (VALUES
        (N'Prd.StopRequst.Requester'),
        (N'Prd.StopsManagement.ProductionExpert'),
        (N'Prd.StopsManagement.ResponsibleUsers'),
        (N'Prd.StopsManagement.ShowAll')
    ) AS r2(RoleName);

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
         IsActive)
    SELECT DISTINCT
        a.Path, a.ActionAccessType, a.ActionAccessItemType, @EntityFullName, NULL, NULL, NULL, r.Id,
        1, 1, @SeedUser, @SeedUser,
        @Now, @NowShamsi, @Now, @NowShamsi,
        1
    FROM #StopActionAccess a
    INNER JOIN system.Role r ON r.Name = a.RoleName;
    PRINT N'  RoleAccess اکشن‌های کنترلر درج شد: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    -----------------------------------------------------------------------------
    PRINT N'=== [6/6] راهنمای افزودن دستی منو (system.SystemMenu) ===';
    -----------------------------------------------------------------------------
    -- ساختار JSON منوی سیستم پیچیده است و نمونهٔ Seed مطمئنی برای آن یافت نشد؛
    -- طبق دستورالعمل، به‌جای حدس زدن ساختار، صرفاً راهنمای افزودن دستی چاپ می‌شود.
    PRINT N'  برای تکمیل ماژول، موارد زیر را به‌صورت دستی از طریق پنل مدیریت منو (system.SystemMenu) اضافه کنید:';
    PRINT N'    گروه منو: «ماژول توقفات»';
    PRINT N'      - درخواست توقف     -> /Panel/StopRequst/List';
    PRINT N'      - مدیریت توقفات    -> /Panel/StopRequst/ManagementList';
    PRINT N'      - گزارش توقفات     -> /Panel/StopRequst/ReportList';

    COMMIT TRANSACTION;
    PRINT N'=== DONE: Seed_StopRequst_DataProfiles اجرا و Commit شد ===';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    PRINT N'=== ERROR: Seed_StopRequst_DataProfiles ناموفق بود و Rollback شد ===';
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH

-----------------------------------------------------------------------------
-- تایید نتیجه
-----------------------------------------------------------------------------
SELECT Id, Name, Title FROM system.Role WHERE Id BETWEEN 200040 AND 200045 ORDER BY Id;

SELECT Id, Name, Title, EntityFullName, Mode, Type, IsActive
FROM system.SavedQuery
WHERE Name IN (N'StopRequst_Requester', N'Stop_ExpertCartable', N'Stop_ResponsibleCartable', N'Stop_CftCartable', N'Stop_QcCartable', N'Stop_MyCartable', N'Stop_Report')
ORDER BY Name;

SELECT ra.Id, ra.Path, ra.ActionAccessType, ra.ActionAccessItemType, ra.RowId, r.Name AS RoleName
FROM system.RoleAccess ra
JOIN system.Role r ON r.Id = ra.RoleId
WHERE r.Id BETWEEN 200040 AND 200045
ORDER BY ra.ActionAccessType, ra.RowId, ra.Path, r.Name;
