# -*- coding: utf-8 -*-
"""فرم چاپی ماموریت «درخواست خدمات پس از فروش» — معادل DevExpress قدیمی Mission1
(HTS: مدیریت درخواست‌های پشتیبانی ← مدیریت ماموریت‌ها ← گزارش).

خروجی:
  Data/Seed/SaleReports/SaleMission.json      قالب Stimulsoft
  Data/Scripts/Seed_SaleMission_Report.sql    SavedQuery + ReportBuilderReport + دکمه چاپ لیست ماموریت
"""
from __future__ import annotations

import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
LOGO_B64 = (HERE.parent / "CngReports" / "havayar_logo_from_hts.b64").read_text(encoding="ascii").strip()
JSON_OUT = HERE / "SaleMission.json"
SQL_OUT = HERE.parent.parent / "Scripts" / "Seed_SaleMission_Report.sql"

NAME = "SaleMission"
TITLE = "درخواست خدمات پس از فروش (ماموریت)"
PARAM = "missionId"
PARAM_TITLE = "شناسه ماموریت"
SEED_USER = "seed-sale-mission-report"
LIST_PROFILE = "AfterSales_Mission_List"
PRINT_BUTTON_ID = "as_print_mission"

FONT = "IRANSansWeb(FaNum) Medium"
MAROON = "solid:128,0,0"
HEADER_BRUSH = "solid:230,230,230"
SECTION_BRUSH = "solid:211,211,211"

# A4 portrait, همان کاغذ گزارش قدیمی
PAGE_W = 21.0
PAGE_H = 29.7
ML = MR = 1.0
W = round(PAGE_W - ML - MR, 2)  # 19.0

SCRIPT = (
    "using System;\r\nusing System.Drawing;\r\nusing System.Windows.Forms;\r\n"
    "using System.Data;\r\nusing Stimulsoft.Controls;\r\nusing Stimulsoft.Base.Drawing;\r\n"
    "using Stimulsoft.Report;\r\nusing Stimulsoft.Report.Dialogs;\r\nusing Stimulsoft.Report.Components;\r\n\r\n"
    "namespace Reports\r\n{\r\n    public class SaleMission : Stimulsoft.Report.StiReport\r\n    {\r\n"
    "        public SaleMission()        {\r\n            this.InitializeComponent();\r\n        }}\r\n\r\n"
    "        #region StiReport Designer generated code - do not modify\r\n"
    "        #endregion StiReport Designer generated code - do not modify\r\n    }\r\n}\r\n"
)


def border(sides: str) -> str:
    return f"{sides};;;;;;;solid:Black"


def text(
    name: str,
    x: float,
    y: float,
    w: float,
    h: float,
    value: str,
    *,
    size: int = 9,
    ha: str | None = None,
    va: str = "Center",
    sides: str = "All",
    brush: str = "solid:",
    text_brush: str = "solid:Black",
    can_grow: bool = False,
    grow_h: bool = False,
    rtl: bool = True,
) -> dict:
    # RightToLeft does not mirror HorAlignment; without an explicit Right, Persian text hugs the left edge.
    if ha is None and rtl:
        ha = "Right"
    comp = {
        "Ident": "StiText",
        "Name": name,
        "ClientRectangle": f"{x:.2f},{y:.2f},{w:.2f},{h:.2f}",
        "Interaction": {"Ident": "StiInteraction"},
        "CanGrow": can_grow,
        "GrowToHeight": grow_h,
        "Text": {"Value": value},
        "Margins": {"Left": 4, "Right": 4, "Top": 0, "Bottom": 0},
        "VertAlignment": va,
        "Font": f"{FONT};{size};;",
        "Border": border(sides) if sides else ";;;;;;;solid:Black",
        "Brush": brush,
        "TextBrush": text_brush,
        "TextOptions": {"RightToLeft": rtl, "WordWrap": True},
    }
    if ha:
        comp["HorAlignment"] = ha
    return comp


def logo(name: str, x: float, y: float, w: float, h: float) -> dict:
    return {
        "Ident": "StiImage",
        "Name": name,
        "ClientRectangle": f"{x:.2f},{y:.2f},{w:.2f},{h:.2f}",
        "Interaction": {"Ident": "StiInteraction"},
        "Border": ";;;;;;;solid:Black",
        "Brush": "empty",
        "Stretch": True,
        "AspectRatio": True,
        "HorAlignment": "Center",
        "VertAlignment": "Center",
        "ImageBytes": LOGO_B64,
    }


def band(ident: str, name: str, y: float, h: float, comps: list[dict], **extra) -> dict:
    data = {
        "Ident": ident,
        "Name": name,
        "ClientRectangle": f"0,{y:.2f},{W},{h:.2f}",
        "Interaction": {"Ident": "StiInteraction"},
        "Border": ";;;;;;;solid:Black",
        "Brush": "solid:",
    }
    data.update(extra)
    data["Components"] = {str(i): c for i, c in enumerate(comps)}
    return data


def pair_row(prefix: str, y: float, h: float, left: tuple[str, str, bool], right: tuple[str, str, bool]) -> list[dict]:
    """[مقدار چپ | برچسب چپ] [مقدار راست | برچسب راست] — هر برچسب و مقدارش یک کادر، مثل جدول HTS."""
    lv, ll, rv, rl = 5.85, 2.10, 8.95, 2.10
    comps = []
    for side, x0, val_w, (label, expr, val_rtl) in (("l", 0.0, lv, left), ("r", lv + ll, rv, right)):
        comps.append(text(
            f"{prefix}{side}V", x0, y, val_w, h, expr, sides="Left, Bottom",
            ha=None if val_rtl else "Center", rtl=val_rtl,
        ))
        comps.append(text(f"{prefix}{side}L", x0 + val_w, y, ll if side == "l" else rl, h, label, sides="Right, Bottom"))
    return comps


def title_components() -> tuple[list[dict], float]:
    comps: list[dict] = []
    top_h = 2.10
    side_w = 5.60
    logo_w = 4.80
    mid_w = round(W - side_w - logo_w, 2)
    row = top_h / 2

    # چپ: تاریخ و شماره درخواست
    comps.append(text("hDateFrame", 0, 0, side_w, row, " "))
    comps.append(text("hDateLbl", side_w - 1.45, 0.10, 1.35, row - 0.20, "تاریخ:", sides=""))
    comps.append(text("hDateVal", 0.15, 0.10, side_w - 1.65, row - 0.20, "{dtMaster.CreatedDate}",
                      size=10, sides="", text_brush=MAROON, rtl=False))
    comps.append(text("hNumFrame", 0, row, side_w, row, " "))
    comps.append(text("hNumLbl", side_w - 1.45, row + 0.10, 1.35, row - 0.20, "شماره:", sides=""))
    comps.append(text("hNumVal", 0.15, row + 0.10, side_w - 1.65, row - 0.20, "{dtMaster.RequestNumber}",
                      size=10, sides="", text_brush=MAROON, rtl=False))
    # وسط: عنوان، راست: لوگو
    comps.append(text("hTitle", side_w, 0, mid_w, top_h, "درخواست خدمات پس از فروش", size=18, ha="Center"))
    comps.append(text("hLogoFrame", side_w + mid_w, 0, logo_w, top_h, " "))
    comps.append(logo("hLogo", side_w + mid_w + 0.20, 0.15, logo_w - 0.40, top_h - 0.30))

    y = top_h
    rh = 0.75
    comps += pair_row(
        "rA", y, rh,
        ("نام جایگاه:", "{dtMaster.Customer_Station_Title}", True),
        ("نام مشتری:", "{dtMaster.Customer_Title}", True),
    )
    y += rh
    comps += pair_row(
        "rB", y, rh,
        ("تاریخ اعزام:", "{dtMaster.Dispatch_Date}", False),
        ("نام تکنسین:", "{dtMaster.Technician}", True),
    )
    y += rh

    desc_h = 2.60
    sig_w = 7.00
    comps.append(text("sigSupervisor", 0, y, sig_w, desc_h, "نام و امضا\nسرپرست فنی",
                      ha="Center", va="Top", sides="Left, Right, Bottom", grow_h=True))
    comps.append(text("reqComment", sig_w, y, round(W - sig_w, 2), desc_h, "شرح درخواست: {dtMaster.Comment}",
                      va="Top", sides="Right, Bottom", can_grow=True))
    y += desc_h
    return comps, round(y, 2)


# ستون‌ها از چپ به راست (ستون # سمت راست، مثل HTS)
# key, header, expression, rtl, can_grow
PRODUCT_COLS = [
    ("type", 2.50, "نوع درخواست", "{dtProduct.RequestType_Title}", True, False),
    ("serial", 2.90, "سریال", "{dtProduct.Serial}", False, False),
    ("name", 10.35, "نام محصول", "{dtProduct.Product_Name}", True, True),
    ("code", 2.40, "کد محصول", "{dtProduct.Product_Code}", False, False),
    ("row", 0.85, "#", "{dtProduct.RowNo}", False, False),
]
PART_COLS = [
    ("comment", 5.40, "ملاحظات", "{dtDetail.Comment}", True, True),
    ("unit", 1.40, "واحد", "{dtDetail.PartUnit_Title}", True, False),
    ("qty", 1.30, "مقدار", "{dtDetail.Qty}", False, False),
    ("code", 2.40, "کد قطعه", "{dtDetail.Part_Code}", False, False),
    ("name", 7.65, "عنوان قطعه", "{dtDetail.Part_Name}", True, True),
    ("row", 0.85, "#", "{dtDetail.RowNo}", False, False),
]


def column_positions(cols):
    total = round(sum(c[1] for c in cols), 2)
    assert total == W, f"column widths {total} != {W}"
    x = 0.0
    for col in cols:
        yield round(x, 2), col
        x += col[1]


def header_cells(prefix: str, cols, y: float, h: float) -> list[dict]:
    return [
        text(f"{prefix}H_{key}", x, y, w, h, title, size=7, ha="Center", brush=HEADER_BRUSH, grow_h=True)
        for x, (key, w, title, _expr, _rtl, _grow) in column_positions(cols)
    ]


def data_cells(prefix: str, cols, h: float) -> list[dict]:
    return [
        text(f"{prefix}D_{key}", x, 0, w, h, expr, size=8, ha="Center", can_grow=grow, grow_h=True, rtl=rtl)
        for x, (key, w, _title, expr, rtl, grow) in column_positions(cols)
    ]


def footer_components() -> tuple[list[dict], float]:
    comps: list[dict] = []
    y = 0.35
    approve_h = 1.50
    comps.append(text("fApprove", 0, y, W, approve_h, "تایید مدیر خدمات پس از فروش", ha="Center", va="Top"))
    y += approve_h
    addr_h = 0.70
    comps.append(text("fAddress", 0, y, W, addr_h, "آدرس: {dtMaster.Address}",
                      sides="Left, Right, Bottom", can_grow=True))
    y += addr_h
    tel_h = 0.65
    fax_val_w, fax_lbl_w = 5.50, 2.00
    comps.append(text("fFaxVal", 0, y, fax_val_w, tel_h, " ", sides="Left, Bottom"))
    comps.append(text("fFaxLbl", fax_val_w, y, fax_lbl_w, tel_h, "شماره فکس:", sides="Bottom"))
    comps.append(text("fTel", fax_val_w + fax_lbl_w, y, round(W - fax_val_w - fax_lbl_w, 2), tel_h,
                      "تلفن: {dtMaster.Phone}", sides="Left, Right, Bottom"))
    y += tel_h
    dist_h = 0.60
    comps.append(text("fCopies", 0, y, W, dist_h,
                      "توزیع نسخ: 1- سفید (واحد مالی)   2- سبز (خدمات پس از فروش)   3- آبی (واحد مالی)",
                      size=8, ha="Left", sides="Left, Right, Bottom"))
    y += dist_h
    return comps, round(y, 2)


MASTER_COLUMNS = [
    "MissionId", "ServiceRequestId", "RequestNumber", "CreatedDate", "Customer_Title",
    "Customer_Station_Title", "Comment", "Dispatch_Date", "Technician", "Address", "Phone",
]
PRODUCT_COLUMNS = ["RowNo", "Product_Code", "Product_Name", "Serial", "RequestType_Title"]
DETAIL_COLUMNS = ["RowNo", "Part_Code", "Part_Name", "Qty", "PartUnit_Title", "Comment"]


def data_source(name: str, columns: list[str]) -> dict:
    return {
        "Ident": "StiDataTableSource",
        "Name": name,
        "Alias": name,
        "Key": name,
        "Columns": {
            str(i): {"Name": c, "Index": -1, "NameInSource": c, "Alias": c, "Type": "System.String"}
            for i, c in enumerate(columns)
        },
        "NameInSource": f"NewDataSet.{name}",
    }


def build_report() -> dict:
    title_comps, title_h = title_components()
    product_head_h = 0.95
    product_row_h = 0.75
    part_head_h = 1.65
    part_row_h = 0.70
    footer_comps, footer_h = footer_components()

    part_header = [text("secParts", 0, 0.35, W, 0.60, "شرح درخواست مواد و لوازم یدکی",
                        ha="Center", brush=SECTION_BRUSH)]
    part_header += header_cells("p", PART_COLS, 0.95, 0.70)

    y = 0.0
    bands = []
    bands.append(band("StiReportTitleBand", "MissionTitle", y, title_h, title_comps, CanGrow=True))
    y += title_h + 0.10
    bands.append(band("StiHeaderBand", "ProductHeader", y, product_head_h,
                      header_cells("pr", PRODUCT_COLS, 0.25, 0.70)))
    y += product_head_h + 0.10
    bands.append(band("StiDataBand", "ProductData", y, product_row_h, data_cells("pr", PRODUCT_COLS, product_row_h),
                      CanGrow=True, DataSourceName="dtProduct", PrintIfEmpty=True))
    y += product_row_h + 0.10
    bands.append(band("StiHeaderBand", "PartHeader", y, part_head_h, part_header))
    y += part_head_h + 0.10
    bands.append(band("StiDataBand", "PartData", y, part_row_h, data_cells("pa", PART_COLS, part_row_h),
                      CanGrow=True, DataSourceName="dtDetail", PrintIfEmpty=True))
    y += part_row_h + 0.10
    bands.append(band("StiFooterBand", "PartFooter", y, footer_h, footer_comps, CanGrow=True))

    page = {
        "Ident": "StiPage",
        "Name": "Page1",
        "Guid": "5d2e8a41-3c7b-4f06-9a1e-7b4c2d9e0f13",
        "Interaction": {"Ident": "StiInteraction"},
        "Border": ";;2;;;;;solid:Black",
        "Brush": "solid:",
        "Components": {str(i): b for i, b in enumerate(bands)},
        "PaperSize": "A4",
        "PageWidth": PAGE_W,
        "PageHeight": PAGE_H,
        "RightToLeft": True,
        "Watermark": {"TextBrush": "solid:50,0,0,0"},
        "Margins": {"Left": ML, "Right": MR, "Top": 0.8, "Bottom": 0.8},
    }
    return {
        "ReportVersion": "2024.3.2.0",
        "ReportGuid": "a6f4c1d8-2b95-4e37-8c10-3e7d5b2a9f64",
        "ReportName": NAME,
        "ReportAlias": TITLE,
        "ReportCreated": "/Date(1790000000000+0330)/",
        "ReportChanged": "/Date(1790000000000+0330)/",
        "EngineVersion": "EngineV2",
        "CalculationMode": "Interpretation",
        "ReportUnit": "Centimeters",
        "Script": SCRIPT,
        "ReferencedAssemblies": {
            "0": "System.Dll",
            "1": "System.Drawing.Dll",
            "2": "System.Windows.Forms.Dll",
            "3": "System.Data.Dll",
            "4": "System.Xml.Dll",
            "5": "Stimulsoft.Controls.Dll",
            "6": "Stimulsoft.Base.Dll",
            "7": "Stimulsoft.Report.Dll",
        },
        # بدون DataSources عبارت‌های {dtMaster.X} خالی می‌مانند.
        "Dictionary": {"DataSources": {
            "0": data_source("dtMaster", MASTER_COLUMNS),
            "1": data_source("dtProduct", PRODUCT_COLUMNS),
            "2": data_source("dtDetail", DETAIL_COLUMNS),
        }},
        "Pages": {"0": page},
    }


MISSION_ID = "TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @missionId))), N''))"

REQUEST_TYPE_TITLE = """CASE d.RequestType
        WHEN 1 THEN N'آموزش'
        WHEN 2 THEN N'اورهال'
        WHEN 3 THEN N'بازدید و مشاوره'
        WHEN 4 THEN N'تعمیرات گارانتی'
        WHEN 5 THEN N'تعمیرات وارانتی'
        WHEN 6 THEN N'راه اندازی اولیه'
        WHEN 7 THEN N'پایپینگ'
        WHEN 8 THEN N'سرویس دوره ای'
        WHEN 9 THEN N'سایر موارد'
        WHEN 10 THEN N'OPI'
        WHEN 11 THEN N'راه اندازی تجهیز تعمیری'
        WHEN 12 THEN N'پیش راه اندازی'
        WHEN 13 THEN N'نصب'
        ELSE N''
    END"""

# Empty result sets come back from QueryService as one placeholder row, so every printed column is text
# and the row number comes from SQL: an empty product/part list prints as one blank line, not "0".
# RequestNumber: دو رقم آخر سال شمسی جاری، مثل شماره نامه (GetPersianYear). ۹۴ سال طراحی قالب HTS بود و ثابت مانده بود.
QUERY = f"""
SELECT
    m.Id AS MissionId,
    sr.Id AS ServiceRequestId,
    N'HY/' + RIGHT(FORMAT(GETDATE(), N'yyyy', N'fa-IR'), 2) + N'/' + CONVERT(nvarchar(20), sr.Id) AS RequestNumber,
    LEFT(ISNULL(sr.CreatedOnShamsiDateTime, N''), 10) AS CreatedDate,
    COALESCE(NULLIF(LTRIM(RTRIM(custParty.FullName)), N''), custParty.CompanyName, N'') AS Customer_Title,
    ISNULL(ca.Title, N'') AS Customer_Station_Title,
    ISNULL(sr.Comment, N'') AS Comment,
    ISNULL(m.DispatchShamsiDate, N'') AS Dispatch_Date,
    COALESCE(
        NULLIF(LTRIM(RTRIM(ISNULL(prs.Name, N'') + N' ' + ISNULL(prs.Family, N''))), N''),
        NULLIF(LTRIM(RTRIM(ex.NameFa)), N''),
        ex.Name,
        N'') AS Technician,
    COALESCE(NULLIF(LTRIM(RTRIM(ca.Address)), N''), NULLIF(LTRIM(RTRIM(custParty.Address)), N''), N'') AS Address,
    COALESCE(NULLIF(LTRIM(RTRIM(custParty.Phone)), N''), NULLIF(LTRIM(RTRIM(custParty.Mobile)), N''), N'') AS Phone
FROM Sale.Mission AS m
INNER JOIN Sale.ServiceRequest AS sr ON sr.Id = m.ServiceRequestId
LEFT JOIN SLS.Customer AS cust ON cust.Id = sr.CustomerId
LEFT JOIN Gnr.Party AS custParty ON custParty.Id = cust.PartyId
LEFT JOIN SLS.CustomerAddress AS ca ON ca.Id = sr.CustomerAddressId
LEFT JOIN Hcm.Personel AS prs ON prs.Id = m.PersonelId
LEFT JOIN [system].[User] AS ex ON ex.Id = m.ExpertId
WHERE m.Id = {MISSION_ID};

SELECT
    CONVERT(nvarchar(10), ROW_NUMBER() OVER (ORDER BY d.Id)) AS RowNo,
    ISNULL(COALESCE(op.Code, odp.Code, odsp.Code), N'') AS Product_Code,
    ISNULL(COALESCE(op.Name, odp.Name, odsp.Name), N'') AS Product_Name,
    ISNULL(COALESCE(ods.Serial, d.OtherPartSerial), N'') AS Serial,
    {REQUEST_TYPE_TITLE} AS RequestType_Title
FROM Sale.Mission AS m
INNER JOIN Sale.ServiceRequestDetail AS d ON d.ServiceRequestId = m.ServiceRequestId
LEFT JOIN Inv.Part AS op ON op.Id = d.OtherPartId
LEFT JOIN Sale.OrderDetail AS od ON od.Id = d.OrderDetailId
LEFT JOIN Inv.Part AS odp ON odp.Id = od.PartId
LEFT JOIN Sale.OrderDetailSerial AS ods ON ods.Id = d.OrderDetailSerialId
LEFT JOIN Sale.OrderDetail AS sod ON sod.Id = ods.OrderDetailId
LEFT JOIN Inv.Part AS odsp ON odsp.Id = sod.PartId
WHERE m.Id = {MISSION_ID}
ORDER BY d.Id;

SELECT
    CONVERT(nvarchar(10), ROW_NUMBER() OVER (ORDER BY MIN(x.Id))) AS RowNo,
    x.Part_Code,
    x.Part_Name,
    FORMAT(SUM(x.Mount), N'#,0.###') AS Qty,
    x.PartUnit_Title,
    x.Comment
FROM (
    SELECT
        p.Id,
        part.Code AS Part_Code,
        ISNULL(part.Name, N'') AS Part_Name,
        p.Mount,
        COALESCE(partUnit.Title, rowUnit.Title, N'') AS PartUnit_Title,
        ISNULL(p.Comment, N'') AS Comment
    FROM Sale.Mission AS m
    INNER JOIN Sale.ServiceRequestDetail AS d ON d.ServiceRequestId = m.ServiceRequestId
    INNER JOIN Sale.ServiceRequestPart AS p ON p.ServiceRequestDetailId = d.Id
    LEFT JOIN Sale.OrderDetail AS od ON od.Id = p.OrderDetailId
    OUTER APPLY (
        SELECT TOP (1) vp.Id
        FROM Inv.Part AS vp
        WHERE p.HtsId > 0 AND vp.HtsId = p.HtsProjectVchPartId
    ) AS htsVch
    LEFT JOIN Inv.Part AS part ON part.Id = CASE
        WHEN p.OrderDetailId IS NOT NULL THEN od.PartId
        WHEN htsVch.Id IS NOT NULL THEN htsVch.Id
        ELSE p.ReplacePartId
    END
    LEFT JOIN Inv.PartUnit AS partUnit ON partUnit.Id = part.UnitId
    LEFT JOIN Inv.PartUnit AS rowUnit ON rowUnit.Id = p.PartUnitId
    WHERE m.Id = {MISSION_ID}
      AND p.IsRegisteredManually = 0
) AS x
WHERE x.Part_Code IS NOT NULL
GROUP BY x.Part_Code, x.Part_Name, x.PartUnit_Title, x.Comment
ORDER BY MIN(x.Id);
""".strip()


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
    col("MissionId", "شناسه ماموریت", 0, 6, False, True),
    col("ServiceRequestId", "شماره اندیکاتور", 0, 6),
    col("RequestNumber", "شماره درخواست", 0),
    col("CreatedDate", "تاریخ", 0),
    col("Customer_Title", "نام مشتری", 0),
    col("Customer_Station_Title", "نام جایگاه", 0),
    col("Comment", "شرح درخواست", 0),
    col("Dispatch_Date", "تاریخ اعزام", 0),
    col("Technician", "نام تکنسین", 0),
    col("Address", "آدرس", 0),
    col("Phone", "تلفن", 0),
    col("RowNo", "#", 1),
    col("Product_Code", "کد محصول", 1),
    col("Product_Name", "نام محصول", 1),
    col("Serial", "سریال", 1),
    col("RequestType_Title", "نوع درخواست", 1),
    col("RowNo", "#", 2),
    col("Part_Code", "کد قطعه", 2),
    col("Part_Name", "عنوان قطعه", 2),
    col("Qty", "مقدار", 2),
    col("PartUnit_Title", "واحد", 2),
    col("Comment", "ملاحظات", 2),
]

QUERY_JSON_SHELL = {
    "Tables": [],
    "Relations": [],
    "Filters": [],
    "CustomConditions": None,
    "CustomQuery": None,
    "Parameters": [{
        "Id": PARAM,
        "Name": PARAM,
        "DisplayName": PARAM_TITLE,
        "DefaultValue": "",
        "DataType": "bigint",
    }],
    "Selects": [
        {"Index": 0, "Name": "dtMaster", "Title": "dtMaster"},
        {"Index": 1, "Name": "dtProduct", "Title": "dtProduct"},
        {"Index": 2, "Name": "dtDetail", "Title": "dtDetail"},
    ],
}

PRINT_SCRIPT = (
    "function(ctx) {\n"
    "    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n"
    "    if (!id) { toastr.error('لطفاً یک ردیف را انتخاب کنید'); return; }\n"
    "    appController.addPage("
    + rf"'/System/ReportBuilder/ViewReportByName/{NAME}?{PARAM}={{\'values\':[\'' + id + '\'],\'condition\':\'=\'}}&autoRun=True'"
    + ");\n}"
)
PRINT_BUTTON = {
    "id": PRINT_BUTTON_ID,
    "title": "چاپ ماموریت",
    "dataActionName": "printMission",
    "colorClass": "btn-color-info",
    "iconClass": "ki-printer ki-outline",
    "requiresSelection": True,
    "requiresMultiSelection": False,
    "useHtml": False,
    "html": "",
    "actionScript": PRINT_SCRIPT,
}


def compact(obj) -> str:
    return json.dumps(obj, ensure_ascii=False, separators=(",", ":"))


def sql_n(value: str) -> str:
    return "N'" + value.replace("'", "''") + "'"


def build_sql(content: str) -> str:
    return f"""/*
فرم چاپی ماموریت «درخواست خدمات پس از فروش» — معادل DevExpress قدیمی Mission1
(HTS: مدیریت درخواست‌های پشتیبانی ← مدیریت ماموریت‌ها ← گزارش).
SavedQuery سه نتیجه دارد: dtMaster (سربرگ و پاورقی)، dtProduct (محصولات درخواست)، dtDetail (قطعات درخواستی).
ساخته شده با Data/Seed/SaleReports/_gen_sale_mission_report.py — این فایل را دستی ویرایش نکنید.
Content فقط وقتی بازنویسی می‌شود که خالی باشد یا آخرین تغییرش با همین seed بوده (ویرایش‌های Designer حفظ می‌شود).
اعمال: sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\\Scripts\\Seed_SaleMission_Report.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY

    DECLARE @Now DATETIME2 = SYSDATETIME();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = {sql_n(SEED_USER)};
    DECLARE @Name NVARCHAR(200) = {sql_n(NAME)};
    DECLARE @Title NVARCHAR(200) = {sql_n(TITLE)};

    DECLARE @CustomQuery NVARCHAR(MAX) = {sql_n(QUERY)};
    DECLARE @QueryJson NVARCHAR(MAX) = {sql_n(compact(QUERY_JSON_SHELL))};
    SET @QueryJson = JSON_MODIFY(@QueryJson, '$.CustomQuery', @CustomQuery);
    DECLARE @ColumnsJson NVARCHAR(MAX) = {sql_n(compact(COLUMNS))};
    DECLARE @Content NVARCHAR(MAX) = {sql_n(content)};
    DECLARE @PrintBtn NVARCHAR(MAX) = {sql_n(compact(PRINT_BUTTON))};

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
            Content = CASE WHEN Content IS NULL OR ModifiedByName = @SeedUser THEN @Content ELSE Content END,
            ModifiedById = 1,
            ModifiedByName = CASE WHEN Content IS NULL OR ModifiedByName = @SeedUser THEN @SeedUser ELSE ModifiedByName END,
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
            @Name, @Title, N'SavedQuery', @SavedQueryId, 1, @Content, @CustomQuery,
            1, 1, @SeedUser, @SeedUser,
            @Now, @NowShamsi, @Now, @NowShamsi, 1
        );
    END;

    -- صفحه ViewReport فقط پارامترهایی را به Stimulsoft می‌فرستد که ردیف فیلتر دارند (مثل ProductionOrderForm)؛
    -- بدون این ردیف missionId خالی می‌رسد و فرم بدون داده چاپ می‌شود.
    DECLARE @ReportId BIGINT = (SELECT Id FROM dbo.ReportBuilderReport WHERE Name = @Name);
    IF NOT EXISTS (SELECT 1 FROM dbo.ReportBuilderReportItem WHERE ReportBuilderReportId = @ReportId AND ColumnName = {sql_n(PARAM)})
        INSERT INTO dbo.ReportBuilderReportItem
            (ColumnName, DataType, Title, Show, CanFilter, Required, ReportBuilderReportId, ReportBuilderReportSelectId,
             CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime)
        VALUES
            ({sql_n(PARAM)}, N'nvarchar', {sql_n(PARAM_TITLE)}, 0, 0, 0, @ReportId, NULL,
             1, @SeedUser, @Now, @NowShamsi);

    -- دکمه «چاپ ماموریت» روی Data Profile لیست ماموریت؛ دکمه‌های فعلی دست نمی‌خورند.
    DECLARE @Buttons NVARCHAR(MAX) = (SELECT CustomActionButtonsJson FROM [system].[SavedQuery] WHERE Name = {sql_n(LIST_PROFILE)});
    IF @Buttons IS NOT NULL
    BEGIN
        IF ISJSON(@Buttons) <> 1 SET @Buttons = N'[]';
        SELECT @Buttons = N'[' + ISNULL(STRING_AGG(CONVERT(NVARCHAR(MAX), j.value), N',') WITHIN GROUP (ORDER BY CONVERT(int, j.[key])), N'') + N']'
        FROM OPENJSON(@Buttons) AS j
        WHERE ISNULL(JSON_VALUE(j.value, '$.id'), N'') <> {sql_n(PRINT_BUTTON_ID)};
        SET @Buttons = JSON_MODIFY(@Buttons, 'append $', JSON_QUERY(@PrintBtn));

        UPDATE [system].[SavedQuery]
        SET CustomActionButtonsJson = @Buttons,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Name = {sql_n(LIST_PROFILE)};
    END;

    COMMIT TRANSACTION;
    PRINT N'{NAME} SavedQueryId=' + CONVERT(nvarchar(20), @SavedQueryId);
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
"""


def main():
    report = build_report()
    JSON_OUT.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    SQL_OUT.write_text(build_sql(compact(report)), encoding="utf-8-sig")
    print("wrote", JSON_OUT, "bytes", JSON_OUT.stat().st_size)
    print("wrote", SQL_OUT, "bytes", SQL_OUT.stat().st_size)


if __name__ == "__main__":
    main()
