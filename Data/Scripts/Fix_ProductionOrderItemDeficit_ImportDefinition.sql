/*
  ورود اکسل کسری اقلام سفارش ساخت (system.ImportDefinition Id = 10)

  ستون‌های میلادی جدول Pln.ProductionOrderItemDeficit پسوند DateTime دارند.
  کوئری قبلی MiladiDate می‌نوشت و SQL خطای Invalid column name می‌داد.
  تاریخ با نقطه (1405.07.15) و مقدارهای 0 و #N/A هم به NULL تبدیل می‌شوند.
  قلم سفارش ساخت با سریال و شماره سفارش پیدا می‌شود، نه فقط سریال.
  اگر همان کالا روی همان قلم قبلاً وارد شده باشد، ردیف به‌روز می‌شود.

  اعمال:
    sqlcmd -S 172.20.40.42 -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Fix_ProductionOrderItemDeficit_ImportDefinition.sql"
*/
SET NOCOUNT ON;

DECLARE @Sql nvarchar(max) = N'
declare @ProductionOrderItemId bigint = null;
declare @ProductionOrderId bigint = null;
declare @PartId bigint = null;
declare @ErrorMessage nvarchar(1000);
declare @MatchCount int;

declare @StdShamsi nvarchar(100) = NULLIF(LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(ISNULL(@StandardDeliveryShamsiDateTime, N''''), N''.'', N''/''), N''-'', N''/''), N''\'', N''/''))), N'''');
if (@StdShamsi is null or len(@StdShamsi) < 8 or len(@StdShamsi) > 10 or @StdShamsi in (N''0'', N''#N/A'', N''#N/A!'', N''#REF!'', N''#VALUE!'', N''#DIV/0!''))
    set @StdShamsi = null;
declare @StdMiladi datetime2 = dbo.fn_ShamsiToMiladiDate(@StdShamsi, N''/'');

declare @IssShamsi nvarchar(100) = NULLIF(LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(ISNULL(@PurchaseRequestIssuanceShamsiDateTime, N''''), N''.'', N''/''), N''-'', N''/''), N''\'', N''/''))), N'''');
if (@IssShamsi is null or len(@IssShamsi) < 8 or len(@IssShamsi) > 10 or @IssShamsi in (N''0'', N''#N/A'', N''#N/A!'', N''#REF!'', N''#VALUE!'', N''#DIV/0!''))
    set @IssShamsi = null;
declare @IssMiladi datetime2 = dbo.fn_ShamsiToMiladiDate(@IssShamsi, N''/'');

declare @CfmShamsi nvarchar(100) = NULLIF(LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(ISNULL(@PurchaseRequestConfirmationShamsiDateTime, N''''), N''.'', N''/''), N''-'', N''/''), N''\'', N''/''))), N'''');
if (@CfmShamsi is null or len(@CfmShamsi) < 8 or len(@CfmShamsi) > 10 or @CfmShamsi in (N''0'', N''#N/A'', N''#N/A!'', N''#REF!'', N''#VALUE!'', N''#DIV/0!''))
    set @CfmShamsi = null;
declare @CfmMiladi datetime2 = dbo.fn_ShamsiToMiladiDate(@CfmShamsi, N''/'');

declare @NeedShamsi nvarchar(100) = NULLIF(LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(ISNULL(@PurchaseRequestNeedShamsiDateTime, N''''), N''.'', N''/''), N''-'', N''/''), N''\'', N''/''))), N'''');
if (@NeedShamsi is null or len(@NeedShamsi) < 8 or len(@NeedShamsi) > 10 or @NeedShamsi in (N''0'', N''#N/A'', N''#N/A!'', N''#REF!'', N''#VALUE!'', N''#DIV/0!''))
    set @NeedShamsi = null;
declare @NeedMiladi datetime2 = dbo.fn_ShamsiToMiladiDate(@NeedShamsi, N''/'');

declare @PreShamsi nvarchar(100) = NULLIF(LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(ISNULL(@PreReportSupplierUnitForecastShamsiDateTime, N''''), N''.'', N''/''), N''-'', N''/''), N''\'', N''/''))), N'''');
if (@PreShamsi is null or len(@PreShamsi) < 8 or len(@PreShamsi) > 10 or @PreShamsi in (N''0'', N''#N/A'', N''#N/A!'', N''#REF!'', N''#VALUE!'', N''#DIV/0!''))
    set @PreShamsi = null;
declare @PreMiladi datetime2 = dbo.fn_ShamsiToMiladiDate(@PreShamsi, N''/'');

declare @TwoShamsi nvarchar(100) = NULLIF(LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(ISNULL(@LastTwoWeeksForecastSupplierShamsiDateTime, N''''), N''.'', N''/''), N''-'', N''/''), N''\'', N''/''))), N'''');
if (@TwoShamsi is null or len(@TwoShamsi) < 8 or len(@TwoShamsi) > 10 or @TwoShamsi in (N''0'', N''#N/A'', N''#N/A!'', N''#REF!'', N''#VALUE!'', N''#DIV/0!''))
    set @TwoShamsi = null;
declare @TwoMiladi datetime2 = dbo.fn_ShamsiToMiladiDate(@TwoShamsi, N''/'');

declare @SupShamsi nvarchar(100) = NULLIF(LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(ISNULL(@SupplierUnitForecastDateShamsiDateTime, N''''), N''.'', N''/''), N''-'', N''/''), N''\'', N''/''))), N'''');
if (@SupShamsi is null or len(@SupShamsi) < 8 or len(@SupShamsi) > 10 or @SupShamsi in (N''0'', N''#N/A'', N''#N/A!'', N''#REF!'', N''#VALUE!'', N''#DIV/0!''))
    set @SupShamsi = null;
declare @SupMiladi datetime2 = dbo.fn_ShamsiToMiladiDate(@SupShamsi, N''/'');

select @PartId = min(Id), @MatchCount = count(*)
from Inv.Part
where LTRIM(RTRIM(Code)) = LTRIM(RTRIM(@partCode));

if (@PartId is null or @MatchCount = 0)
begin
    set @ErrorMessage = N''کالایی با کد '' + isnull(@partCode, N'''') + N'' یافت نشد.'';
    throw 50000, @ErrorMessage, 1;
end

select @MatchCount = count(*)
from Sale.ProductionOrderItem pi
inner join Sale.ProductionOrder p on p.Id = pi.ProductionOrderId
where LTRIM(RTRIM(pi.Serial)) = LTRIM(RTRIM(@Serial))
  and (
        @ProductionOrderNumber is null
        or p.ProductionOrderNumber = @ProductionOrderNumber
      );

if (@MatchCount = 0)
begin
    set @ErrorMessage = N''قلم سفارش ساختی با سریال '' + isnull(@Serial, N'''')
        + N'' و شماره سفارش '' + isnull(cast(@ProductionOrderNumber as nvarchar(30)), N'''') + N'' یافت نشد.'';
    throw 50000, @ErrorMessage, 1;
end

if (@MatchCount > 1)
begin
    set @ErrorMessage = N''بیش از یک قلم سفارش ساخت با سریال '' + isnull(@Serial, N'''')
        + N'' و شماره سفارش '' + isnull(cast(@ProductionOrderNumber as nvarchar(30)), N'''') + N'' یافت شد.'';
    throw 50000, @ErrorMessage, 1;
end

select @ProductionOrderItemId = pi.Id, @ProductionOrderId = pi.ProductionOrderId
from Sale.ProductionOrderItem pi
inner join Sale.ProductionOrder p on p.Id = pi.ProductionOrderId
where LTRIM(RTRIM(pi.Serial)) = LTRIM(RTRIM(@Serial))
  and (
        @ProductionOrderNumber is null
        or p.ProductionOrderNumber = @ProductionOrderNumber
      );

update Pln.ProductionOrderItemDeficit
set ProductionOrderId = @ProductionOrderId,
    Serial = @Serial,
    DeficitCount = @DeficitCount,
    DeviceProgressPercentage = @DeviceProgressPercentage,
    DevicePriority = @DevicePriority,
    StandardDeliveryMiladiDateTime = @StdMiladi,
    StandardDeliveryShamsiDateTime = @StdShamsi,
    ExecutiveUnit = @ExecutiveUnit,
    PurchaseRequestIssuanceMiladiDateTime = @IssMiladi,
    PurchaseRequestIssuanceShamsiDateTime = @IssShamsi,
    PurchaseRequestConfirmationMiladiDateTime = @CfmMiladi,
    PurchaseRequestConfirmationShamsiDateTime = @CfmShamsi,
    PurchaseRequestNeedMiladiDateTime = @NeedMiladi,
    PurchaseRequestNeedShamsiDateTime = @NeedShamsi,
    OnWayPurchasedCount = @OnWayPurchasedCount,
    PreReportSupplierUnitForecastMiladiDateTime = @PreMiladi,
    PreReportSupplierUnitForecastShamsiDateTime = @PreShamsi,
    LastTwoWeeksForecastSupplierMiladiDateTime = @TwoMiladi,
    LastTwoWeeksForecastSupplierShamsiDateTime = @TwoShamsi,
    SupplierUnitForecastDateMiladiDateTime = @SupMiladi,
    SupplierUnitForecastDateShamsiDateTime = @SupShamsi,
    SupplierDescription = @SupplierDescription,
    PurchaseExpert = @PurchaseExpert,
    Description = @Description,
    ModifiedById = @CurrentUserId,
    ModifiedByName = @CurrentUserName,
    ModifiedDateMiladiDateTime = @CurrentDateTimeMiladi,
    ModifiedDateShamsiDateTime = @CurrentDateTimeShamsi,
    IsActive = 1,
    PartId = @PartId
where ProductionOrderItemId = @ProductionOrderItemId
  and PartId = @PartId;

if (@@ROWCOUNT = 0)
begin
    insert into Pln.ProductionOrderItemDeficit
        (ProductionOrderItemId, ProductionOrderId, Serial, DeficitCount, DeviceProgressPercentage, DevicePriority,
         StandardDeliveryMiladiDateTime, StandardDeliveryShamsiDateTime, ExecutiveUnit,
         PurchaseRequestIssuanceMiladiDateTime, PurchaseRequestIssuanceShamsiDateTime,
         PurchaseRequestConfirmationMiladiDateTime, PurchaseRequestConfirmationShamsiDateTime,
         PurchaseRequestNeedMiladiDateTime, PurchaseRequestNeedShamsiDateTime,
         OnWayPurchasedCount,
         PreReportSupplierUnitForecastMiladiDateTime, PreReportSupplierUnitForecastShamsiDateTime,
         LastTwoWeeksForecastSupplierMiladiDateTime, LastTwoWeeksForecastSupplierShamsiDateTime,
         SupplierUnitForecastDateMiladiDateTime, SupplierUnitForecastDateShamsiDateTime,
         SupplierDescription, PurchaseExpert, Description,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         IsActive, PartId)
    values
        (@ProductionOrderItemId, @ProductionOrderId, @Serial, @DeficitCount, @DeviceProgressPercentage, @DevicePriority,
         @StdMiladi, @StdShamsi, @ExecutiveUnit,
         @IssMiladi, @IssShamsi,
         @CfmMiladi, @CfmShamsi,
         @NeedMiladi, @NeedShamsi,
         @OnWayPurchasedCount,
         @PreMiladi, @PreShamsi,
         @TwoMiladi, @TwoShamsi,
         @SupMiladi, @SupShamsi,
         @SupplierDescription, @PurchaseExpert, @Description,
         @CurrentUserId, @CurrentUserId, @CurrentUserName, @CurrentUserName,
         @CurrentDateTimeMiladi, @CurrentDateTimeShamsi,
         @CurrentDateTimeMiladi, @CurrentDateTimeShamsi,
         1, @PartId);
end
';

UPDATE system.ImportDefinition
SET SqlQuery = @Sql
WHERE Id = 10;

IF @@ROWCOUNT <> 1
    THROW 50010, N'تعریف ورود شماره 10 به‌روز نشد.', 1;
