# -*- coding: utf-8 -*-
"""Emit Seed_ProductionOrderForm_Report.sql (UTF-8 BOM) and the list-button JSON."""
from __future__ import annotations

import json
from pathlib import Path

OUT = Path(r"D:\Projects\Havayar\HavayarApp\Data\Scripts\Seed_ProductionOrderForm_Report.sql")

QUERY = r"""
SELECT
    po.Id AS ProductionOrderId,
    COALESCE(CONVERT(nvarchar(50), po.ProductionOrderNumber), po.Number) AS ProductionOrder_Number,
    LEFT(ISNULL(po.CreatedOnShamsiDateTime, N''), 10) AS CreatedDate,
    custParty.FullName AS Customer_Title,
    ind.Title AS CustomerIndustryHeader,
    po.CustomerIndustry AS CustomerIndustry,
    po.EndUser AS EndUser,
    COALESCE(dl.Code, CONVERT(nvarchar(50), po.ProjectDlCode)) AS ProjectDl,
    dl.Title AS ProjectDlTitle,
    br.Title AS SaleDepartment,
    COALESCE(NULLIF(LTRIM(RTRIM(se.NameFa)), N''), se.Name) AS Sale_Personel,
    agencyParty.FullName AS SalesAgency,
    dbo.fn_MiladiToShamsi(po.MetreDate) AS MetreDate,
    po.MetreNumber AS MetreNumber,
    ct.Number AS ContractNumber,
    CAST(N'' AS nvarchar(50)) AS RequirementsAdvise_Number,
    CASE po.DeliveryType
        WHEN 88 THEN N'پارت به پارت'
        WHEN 89 THEN N'تحویل کل سفارش'
        ELSE N''
    END AS DeliverType,
    CAST(N'' AS nvarchar(50)) AS ProjectType,
    CASE WHEN po.InstallationIsByHy = 1 THEN N'هوایار' ELSE N'' END AS InstallationType,
    city.Name AS InstallationCity,
    CAST(CASE WHEN po.IsNeedProjectManager = 1 THEN 1 ELSE 0 END AS bit) AS IsNeedProjectManager,
    CAST(CASE WHEN po.IsNeedInspectionBeforePacking = 1 THEN 1 ELSE 0 END AS bit) AS IsNeedInspectionBeforePacking,
    CAST(CASE WHEN po.CompressorHouseIsReady = 1 THEN 1 ELSE 0 END AS bit) AS CompressorHouseIsReady,
    CAST(CASE WHEN po.HasGA = 1 THEN 1 ELSE 0 END AS bit) AS HasGA,
    po.SalesComment AS SalesComment,
    CASE po.CentrifugeSetupType
        WHEN 1899 THEN N'تابلو راه اندازی | Star-Delta'
        WHEN 1900 THEN N'تابلو راه اندازی | DOL'
        WHEN 1901 THEN N'تابلو راه اندازی | Soft Starter'
        WHEN 1902 THEN N'تابلو راه اندازی ندارد'
        ELSE N''
    END AS SetupMethod,
    CAST(CASE WHEN po.CentrifugeHasCompressor = 1 THEN 1 ELSE 0 END AS bit) AS CentrifugeHasCompressor,
    CASE WHEN po.CentrifugeCompressorCount IS NULL THEN N''
         ELSE CONVERT(nvarchar(50), CONVERT(decimal(18, 2), po.CentrifugeCompressorCount))
    END AS CentrifugeCount,
    CASE WHEN po.CentrifugeElectromotorVoltage IS NULL THEN N''
         ELSE CONVERT(nvarchar(50), CONVERT(decimal(18, 2), po.CentrifugeElectromotorVoltage))
    END AS ElectromotorVoltage
FROM Sale.ProductionOrder AS po
LEFT JOIN SLS.Contract AS ct ON ct.Id = po.ContractId
LEFT JOIN SLS.Customer AS cu ON cu.Id = ct.CustomerId
LEFT JOIN Gnr.Party AS custParty ON custParty.Id = cu.PartyId
LEFT JOIN Gnr.PartyIndustry AS ind ON ind.Id = cu.IndustryId
LEFT JOIN FIN.DL AS dl ON dl.Id = po.ProjectDlId
LEFT JOIN Sale.Branch AS br ON br.Id = po.BranchId
LEFT JOIN [system].[User] AS se ON se.Id = po.SalesExpertId
LEFT JOIN SLS.Customer AS agency ON agency.Id = po.SalesAgencyId
LEFT JOIN Gnr.Party AS agencyParty ON agencyParty.Id = agency.PartyId
LEFT JOIN Gnr.Region AS city ON city.Id = po.InstallationCityId
WHERE po.Id = TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @productionOrderId))), N''));

SELECT
    i.Id AS ProductionOrderItemId,
    COALESCE(p.Code, i.PartCode) AS Part_Code,
    p.Name AS Part_Name,
    i.Capacity AS Capacity,
    CONVERT(nvarchar(50), i.OutputPressureBar) AS PressureWorking,
    CONVERT(nvarchar(50), i.BarometricPressure) AS BarometricPressure,
    CONVERT(nvarchar(50), i.PurityPercentage) AS PurityPercentage,
    CONVERT(nvarchar(50), i.MaximumTemperature) AS MaximumTemperature,
    CONVERT(nvarchar(50), i.RelativeHumidity) AS RelativeHumidity,
    CONVERT(nvarchar(50), i.Amount) AS Mount,
    dbo.fn_MiladiToShamsi(i.AgreedDeliverDate) AS AgreedDeliverDate,
    CAST(CASE WHEN i.HasInspection = 1 THEN 1 ELSE 0 END AS bit) AS HasInspection
FROM Sale.ProductionOrderItem AS i
LEFT JOIN Inv.Part AS p ON p.Id = i.PartId
WHERE i.ProductionOrderId = TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @productionOrderId))), N''))
  AND i.IsDeleted = 0
  AND i.IsLatestVersion = 1
ORDER BY i.Id;
""".strip()

SCRIPT = (
    "function(ctx) {\n"
    "    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n"
    "    if (!id) { toastr.error('لطفاً یک ردیف را انتخاب کنید'); return; }\n"
    "    appController.addPage("
    + r"'/System/ReportBuilder/ViewReportByName/ProductionOrderForm?productionOrderId={\'values\':[\'' + id + '\'],\'condition\':\'=\'}&autoRun=True'"
    + ");\n}"
)

BUTTON = [{
    "id": "po_print_form",
    "title": "چاپ سفارش ساخت",
    "dataActionName": "printProductionOrder",
    "colorClass": "btn-color-primary",
    "iconClass": "ki-printer ki-outline",
    "requiresSelection": True,
    "requiresMultiSelection": False,
    "useHtml": False,
    "html": "",
    "actionScript": SCRIPT,
}]
BUTTON_JSON = json.dumps(BUTTON, ensure_ascii=False, separators=(",", ":"))


def col(name, title, select_index, system_type=0, visible=True, pk=False):
    return {
        "TableName": None,
        "ColumnName": name,
        "DisplayName": title,
        "Alliance": name,
        "Address": None,
        "SystemTypeName": None,
        "SelectIndex": select_index,
        "Visible": visible,
        "PrimaryKey": pk,
        "SystemType": system_type,
        "SortDirection": None,
        "SortOrder": None,
        "GroupBy": False,
        "Options": [],
        "OptionSetting": None,
        "Aggregate": None,
        "Render": None,
        "IsCustom": False,
        "CustomColType": None,
        "HtmlTemplate": None,
        "BtnConfig": None,
        "InputConfig": None,
        "Width": 120,
        "Filterable": True,
        "Sortable": True,
        "ClassName": None,
    }


COLUMNS = [
    col("ProductionOrderId", "شناسه", 0, 6, False, True),
    col("ProductionOrder_Number", "شماره سفارش ساخت", 0),
    col("CreatedDate", "تاریخ", 0),
    col("Customer_Title", "مشتری", 0),
    col("CustomerIndustryHeader", "نوع صنعت", 0),
    col("CustomerIndustry", "ریز صنعت", 0),
    col("EndUser", "End-User", 0),
    col("ProjectDl", "کد تفضیل پروژه", 0),
    col("ProjectDlTitle", "تفضیل پروژه", 0),
    col("SaleDepartment", "واحد فروش", 0),
    col("Sale_Personel", "کارشناس فروش", 0),
    col("SalesAgency", "نمایندگی فروش", 0),
    col("MetreDate", "تاریخ متره", 0),
    col("MetreNumber", "عطف به متره برآورد شماره", 0),
    col("ContractNumber", "قرارداد/پیش فاکتور", 0),
    col("RequirementsAdvise_Number", "اعلام نیازمندی", 0),
    col("DeliverType", "نحوه تحویل", 0),
    col("ProjectType", "نوع پروژه", 0),
    col("InstallationType", "نوع/متولی نصب", 0),
    col("InstallationCity", "محل نصب", 0),
    col("IsNeedProjectManager", "نیاز به مدیر پروژه", 0, 1),
    col("IsNeedInspectionBeforePacking", "بازرسی قبل از بسته بندی", 0, 1),
    col("CompressorHouseIsReady", "کمپرسورخانه آماده", 0, 1),
    col("HasGA", "GA", 0, 1),
    col("SalesComment", "توضیحات فروش", 0),
    col("SetupMethod", "نحوه راه اندازی", 0),
    col("CentrifugeHasCompressor", "مشتری از قبل دارد", 0, 1),
    col("CentrifugeCount", "تعداد", 0),
    col("ElectromotorVoltage", "ولتاژ الکتروموتور", 0),
    col("ProductionOrderItemId", "شناسه قلم", 1, 6, False, True),
    col("Part_Code", "کد تجهیز", 1),
    col("Part_Name", "عنوان", 1),
    col("Capacity", "ظرفیت", 1),
    col("PressureWorking", "فشار", 1),
    col("BarometricPressure", "فشار بارومتریک", 1),
    col("PurityPercentage", "درصد خلوص", 1),
    col("MaximumTemperature", "دمای بیشینه", 1),
    col("RelativeHumidity", "رطوبت", 1),
    col("Mount", "تعداد", 1),
    col("AgreedDeliverDate", "تاریخ تحویل توافقی", 1),
    col("HasInspection", "بازرسی حین ساخت", 1, 1),
]
COLUMNS_JSON = json.dumps(COLUMNS, ensure_ascii=False, separators=(",", ":"))

QUERY_JSON_SHELL = {
    "Tables": [],
    "Relations": [],
    "Filters": [],
    "CustomConditions": None,
    "CustomQuery": None,
    "Parameters": [{
        "Id": "productionOrderId",
        "Name": "productionOrderId",
        "DisplayName": "شناسه سفارش ساخت",
        "DefaultValue": "",
        "DataType": "bigint",
    }],
    "Selects": [
        {"Index": 0, "Name": "dtMaster", "Title": "dtMaster"},
        {"Index": 1, "Name": "dtDetail", "Title": "dtDetail"},
    ],
}


def sql_n(value: str) -> str:
    return "N'" + value.replace("'", "''") + "'"


sql = f"""/*
فرم چاپی «سفارش ساخت» — معادل DevExpress قدیمی Pln_ProductionOrderReport (چاپ روی صفحه سفارش ساخت).
SavedQuery دو نتیجه دارد: dtMaster (سربرگ) و dtDetail (اقلام آخرین نسخه).
قالب Stimulsoft جداگانه در Data/Seed/SaleReports/ProductionOrderForm.json است و با اسکریپت محتوا روی ReportBuilderReport.Content می‌نشیند.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY

    DECLARE @Now DATETIME2 = SYSDATETIME();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-production-order-form';
    DECLARE @Name NVARCHAR(200) = N'ProductionOrderForm';
    DECLARE @Title NVARCHAR(200) = N'سفارش ساخت';

    DECLARE @CustomQuery NVARCHAR(MAX) = {sql_n(QUERY)};
    DECLARE @QueryJson NVARCHAR(MAX) = {sql_n(json.dumps(QUERY_JSON_SHELL, ensure_ascii=False, separators=(",", ":")))};
    SET @QueryJson = JSON_MODIFY(@QueryJson, '$.CustomQuery', @CustomQuery);
    DECLARE @ColumnsJson NVARCHAR(MAX) = {sql_n(COLUMNS_JSON)};
    DECLARE @PrintBtn NVARCHAR(MAX) = {sql_n(BUTTON_JSON)};

    DECLARE @SavedQueryId BIGINT;

    IF EXISTS (SELECT 1 FROM [system].[SavedQuery] WHERE Name = @Name)
    BEGIN
        UPDATE [system].[SavedQuery]
        SET Title = @Title,
            QueryJson = @QueryJson,
            ColumnsJson = @ColumnsJson,
            EntityFullName = NULL,
            Mode = 1,
            Type = 0,
            ActionOptions = N'',
            CustomActionButtonsJson = N'[]',
            EventScriptsJson = N'{{"onSelectedRow":"","onRowAdded":""}}',
            DiagramJson = N'[]',
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi,
            IsActive = 1
        WHERE Name = @Name;
        SELECT @SavedQueryId = Id FROM [system].[SavedQuery] WHERE Name = @Name;
    END
    ELSE
    BEGIN
        INSERT INTO [system].[SavedQuery]
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
            @Name, @Title, @QueryJson, @ColumnsJson, N'[]',
            N'[]', N'{{"onSelectedRow":"","onRowAdded":""}}',
            1, 0, NULL, N'',
            1, 1, @SeedUser, @SeedUser,
            @Now, @NowShamsi, @Now, @NowShamsi,
            1
        );
        SET @SavedQueryId = SCOPE_IDENTITY();
    END;

    IF EXISTS (SELECT 1 FROM dbo.ReportBuilderReport WHERE Name = @Name)
    BEGIN
        UPDATE dbo.ReportBuilderReport
        SET Title = @Title,
            BaseQuery = @CustomQuery,
            SavedQueryId = @SavedQueryId,
            Type = 1,
            ObjectType = N'SavedQuery',
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi,
            IsActive = 1
        WHERE Name = @Name;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.ReportBuilderReport
        (
            Name, Title, ObjectType, SavedQueryId, Type, Content, BaseQuery,
            CreatedById, ModifiedById, CreatedByName, ModifiedByName,
            CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
            ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive
        )
        VALUES
        (
            @Name, @Title, N'SavedQuery', @SavedQueryId, 1, NULL, @CustomQuery,
            1, 1, @SeedUser, @SeedUser,
            @Now, @NowShamsi, @Now, @NowShamsi, 1
        );
    END;

    UPDATE [system].[SavedQuery]
    SET CustomActionButtonsJson = @PrintBtn,
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Name IN (N'PO_All', N'PO_Related', N'productionorder_listinfo')
      AND EntityFullName = N'entities.app.sale.productionorder';

    COMMIT TRANSACTION;
    PRINT N'ProductionOrderForm SavedQueryId=' + CONVERT(nvarchar(20), @SavedQueryId);
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
"""

# The f-string doubled braces for JSON event scripts. Verify they survived.
OUT.write_text(sql, encoding="utf-8-sig")
stored = json.loads(BUTTON_JSON)[0]["actionScript"]
(OUT.parent / "_po_btn_check.txt").write_text(stored, encoding="utf-8")
print("wrote", OUT, "bytes", OUT.stat().st_size)
