# -*- coding: utf-8 -*-
"""فرم چاپی ورود اضطراری — فیلدهای DevExpress قدیمی EnexEntryFormReport، با قواعد چاپ سیستم جدید."""
from __future__ import annotations

import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
LOGO_B64 = (HERE.parent / "CngReports" / "havayar_logo_from_hts.b64").read_text(encoding="ascii").strip()
JSON_OUT = HERE / "EnexEntryForm.json"
SQL_OUT = ROOT / "Scripts" / "Seed_EnexEntryForm_Report.sql"

# A4 عمودی، همان کاغذ گزارش قدیمی
PAGE_W = 21.0
PAGE_H = 29.7
ML = 0.55
MR = 0.55
W = round(PAGE_W - ML - MR, 2)

FONT = "IRANSansWeb(FaNum) Medium"
BORDER = "All;;;;;;;solid:Black"
NO_BORDER = ";;;;;;;solid:Black"
GRAY = "solid:192,192,192"
WHITE = "solid:255,255,255"
MAROON = "solid:128,0,0"

SCRIPT = (
    "using System;\r\nusing System.Drawing;\r\nusing System.Windows.Forms;\r\n"
    "using System.Data;\r\nusing Stimulsoft.Controls;\r\nusing Stimulsoft.Base.Drawing;\r\n"
    "using Stimulsoft.Report;\r\nusing Stimulsoft.Report.Dialogs;\r\nusing Stimulsoft.Report.Components;\r\n\r\n"
    "namespace Reports\r\n{\r\n    public class EnexEntryForm : Stimulsoft.Report.StiReport\r\n    {\r\n"
    "        public EnexEntryForm()        {\r\n            this.InitializeComponent();\r\n        }}\r\n\r\n"
    "        #region StiReport Designer generated code - do not modify\r\n"
    "        #endregion StiReport Designer generated code - do not modify\r\n    }\r\n}\r\n"
)

# چپ به راست، مطابق سلول‌های DevExpress (ستون # سمت راست)
# توضیحات | مقدار ورود | مقدار خروج | نام کالا | کد | #
COL_WEIGHTS = [1.47, 0.61, 0.72, 2.98, 0.68, 0.14]
COL_DEF = [
    ("comment", "توضیحات", "{dtDetail.PartComment}", "Center", True, True),
    ("enter", "تعداد/مقدار ورود", "{dtDetail.EnterAmount}", "Center", False, False),
    ("exit", "تعداد/مقدار خروج", "{dtDetail.Mount}", "Center", False, False),
    ("name", "نام کالا", "{dtDetail.PartTitle}", "Center", True, True),
    ("code", "کد/سریال کالا", "{dtDetail.PartCode}", "Center", False, False),
    ("row", "#", "{Line}", "Center", False, False),
]


def scale_cols(weights):
    total = sum(weights)
    widths = [round(w / total * W, 2) for w in weights]
    drift = round(W - sum(widths), 2)
    name_i = weights.index(max(weights))
    widths[name_i] = round(widths[name_i] + drift, 2)
    xs = []
    x = 0.0
    for w in widths:
        xs.append(round(x, 2))
        x = round(x + w, 2)
    return list(zip(xs, widths))


COL_POS = scale_cols(COL_WEIGHTS)


def text(name, x, y, w, h, value, *, size=9, ha="Right", brush=WHITE, text_brush="solid:Black",
         border=BORDER, rtl=True, can_grow=False, grow_h=False):
    return {
        "Ident": "StiText",
        "Name": name,
        "ClientRectangle": f"{x:.2f},{y:.2f},{w:.2f},{h:.2f}",
        "Interaction": {"Ident": "StiInteraction"},
        "CanGrow": can_grow,
        "GrowToHeight": grow_h,
        "Text": {"Value": value},
        "HorAlignment": ha,
        "VertAlignment": "Center",
        "Font": f"{FONT};{size};;",
        "Border": border,
        "Brush": brush,
        "TextBrush": text_brush,
        "TextOptions": {"RightToLeft": rtl, "WordWrap": True},
    }


def logo(name, x, y, w, h):
    return {
        "Ident": "StiImage",
        "Name": name,
        "ClientRectangle": f"{x:.2f},{y:.2f},{w:.2f},{h:.2f}",
        "Interaction": {"Ident": "StiInteraction"},
        "Border": NO_BORDER,
        "Brush": "empty",
        "Stretch": True,
        "AspectRatio": True,
        "HorAlignment": "Center",
        "VertAlignment": "Center",
        "ImageBytes": LOGO_B64,
    }


def header_components():
    comps = []
    side = 7.4
    mid = round(W - side * 2, 2)
    row_h = 0.62
    # شماره و تاریخ سمت چپ مختصات (با RightToLeft صفحه، همان جای گزارش سفارش ساخت)
    fields = [
        ("شماره فرم", "{dtMaster.FormNumber}"),
        ("زمان ورود", "{dtMaster.CreatedDateInText}"),
        ("شماره فرم خروج", "{dtMaster.ExitFormNumber}"),
    ]
    lbl_w = 2.55
    for i, (label, expr) in enumerate(fields):
        y = 0.12 + i * row_h
        comps.append(text(f"hVal{i}", 0.12, y, side - lbl_w - 0.2, row_h - 0.04, expr,
                          size=10, ha="Left", brush="solid:", text_brush=MAROON, border=NO_BORDER, rtl=False))
        comps.append(text(f"hLbl{i}", side - lbl_w, y, lbl_w, row_h - 0.04, label,
                          size=9, ha="Right", brush="solid:", border=NO_BORDER, rtl=True))
    title_h = round(0.12 + len(fields) * row_h, 2)
    comps.append(text("hTitle", side, 0.15, mid, title_h - 0.3, "فرم ورود اضطراری",
                      size=18, ha="Center", brush="solid:", border=NO_BORDER, rtl=True))
    comps.append(logo("hLogo", side + mid + 0.25, 0.15, side - 0.45, title_h - 0.35))
    y = round(title_h + 0.15, 2)
    col_h = 0.7
    for (x, w), (key, title, _expr, ha, _grow, _rtl) in zip(COL_POS, COL_DEF):
        comps.append(text(f"ch_{key}", x, y, w, col_h, title, size=7, ha="Center", brush=GRAY, rtl=True, grow_h=True))
    return comps, round(y + col_h, 2)


def detail_components(row_h):
    comps = []
    for (x, w), (key, _title, expr, ha, grow, rtl) in zip(COL_POS, COL_DEF):
        comps.append(text(f"cd_{key}", x, 0, w, row_h, expr, size=8, ha=ha, rtl=rtl, can_grow=grow, grow_h=True))
    return comps


def build_report():
    header_comps, header_h = header_components()
    row_h = 0.62
    page = {
        "Ident": "StiPage",
        "Name": "Page1",
        "Guid": "a7c31e20-4b18-4d6a-9f02-6e1b8c0d55a1",
        "Interaction": {"Ident": "StiInteraction"},
        "Border": ";;2;;;;;solid:Black",
        "Brush": "solid:",
        "Components": {
            "0": {
                "Ident": "StiPageHeaderBand",
                "Name": "PageHeader",
                "ClientRectangle": f"0,0,{W},{header_h}",
                "Interaction": {"Ident": "StiInteraction"},
                "Border": NO_BORDER,
                "Brush": "solid:",
                "Components": {str(i): c for i, c in enumerate(header_comps)},
            },
            "1": {
                "Ident": "StiDataBand",
                "Name": "ItemsBand",
                "ClientRectangle": f"0,{header_h},{W},{row_h}",
                "Interaction": {"Ident": "StiInteraction"},
                "CanGrow": True,
                "Border": NO_BORDER,
                "Brush": "solid:",
                "DataSourceName": "dtDetail",
                "Components": {str(i): c for i, c in enumerate(detail_components(row_h))},
            },
        },
        "PaperSize": "A4",
        "PageWidth": PAGE_W,
        "PageHeight": PAGE_H,
        "RightToLeft": True,
        "Watermark": {"TextBrush": "solid:50,0,0,0"},
        "Margins": {"Left": ML, "Right": MR, "Top": 0.4, "Bottom": 0.4},
    }
    return {
        "ReportGuid": "b2e84f11-9c55-4a70-8d13-2f6a0e77c901",
        "ReportName": "EnexEntryForm",
        "ReportAlias": "فرم ورود اضطراری",
        "ReportCreated": "/Date(1761000000000+0330)/",
        "ReportChanged": "/Date(1761000000000+0330)/",
        "EngineVersion": "EngineV2",
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
        "Pages": {"0": page},
    }


CUSTOM_QUERY = r"""SELECT
    e.Id AS Id,
    CONVERT(nvarchar(50), e.FormNumber) AS FormNumber,
    ISNULL(e.CreatedOnShamsiDateTime, N'') AS CreatedDateInText,
    CASE WHEN x.FormNumber IS NULL THEN N'' ELSE CONVERT(nvarchar(50), x.FormNumber) END AS ExitFormNumber
FROM Hrm.EnexEntryForm AS e
LEFT JOIN Hrm.EnexExitForm AS x ON x.Id = e.ExitFormId
WHERE e.Id = TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @parentId))), N''));

SELECT
    p.Id AS PartId,
    COALESCE(ep.Code, ip.Code, N'') AS PartCode,
    COALESCE(ep.Title, ip.Name, N'') AS PartTitle,
    CASE WHEN p.Mount IS NULL THEN N'' ELSE CONVERT(nvarchar(50), CONVERT(float, p.Mount)) END AS Mount,
    CASE WHEN p.EnterAmount IS NULL THEN N'' ELSE CONVERT(nvarchar(50), CONVERT(float, p.EnterAmount)) END AS EnterAmount,
    ISNULL(p.Comment, N'') AS PartComment
FROM Hrm.EnexFormPart AS p
LEFT JOIN Hrm.EnexPart AS ep ON ep.Id = p.EnexPartId
LEFT JOIN Inv.Part AS ip ON ip.Id = p.PartId
WHERE p.EntryFormId = TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @parentId))), N''))
  AND (ep.Id IS NOT NULL OR ip.Id IS NOT NULL)
ORDER BY p.Id"""

QUERY_JSON = {
    "Tables": [],
    "Relations": [],
    "Filters": [],
    "CustomConditions": None,
    "CustomQuery": CUSTOM_QUERY,
    "Parameters": [{
        "Id": "parentId",
        "Name": "parentId",
        "DisplayName": "شناسه فرم ورود",
        "DefaultValue": "",
        "DataType": "bigint",
    }],
    "Selects": [
        {"Index": 0, "Name": "dtMaster", "Title": "dtMaster"},
        {"Index": 1, "Name": "dtDetail", "Title": "dtDetail"},
    ],
}

COLUMNS = [
    ("Id", "شناسه", 0, True, False, 6),
    ("FormNumber", "شماره فرم", 0, False, True, 0),
    ("CreatedDateInText", "زمان ورود", 0, False, True, 0),
    ("ExitFormNumber", "شماره فرم خروج", 0, False, True, 0),
    ("PartId", "شناسه قلم", 1, True, False, 6),
    ("PartCode", "کد/سریال کالا", 1, False, True, 0),
    ("PartTitle", "نام کالا", 1, False, True, 0),
    ("Mount", "تعداد/مقدار خروج", 1, False, True, 0),
    ("EnterAmount", "تعداد/مقدار ورود", 1, False, True, 0),
    ("PartComment", "توضیحات", 1, False, True, 0),
]


def col(name, display, select_index, pk, visible, system_type):
    return {
        "TableName": None, "ColumnName": name, "DisplayName": display, "Alliance": name,
        "Address": None, "SystemTypeName": None, "SelectIndex": select_index, "Visible": visible,
        "PrimaryKey": pk, "SystemType": system_type, "SortDirection": None, "SortOrder": None,
        "GroupBy": False, "Options": [], "OptionSetting": None, "Aggregate": None, "Render": None,
        "IsCustom": False, "CustomColType": None, "HtmlTemplate": None, "BtnConfig": None,
        "InputConfig": None, "Width": 120, "Filterable": True, "Sortable": True, "ClassName": None,
    }


def sql_n(value: str) -> str:
    return "N'" + value.replace("'", "''") + "'"


def build_sql(content: str) -> str:
    columns = json.dumps([col(*c) for c in COLUMNS], ensure_ascii=False, separators=(",", ":"))
    query_json = json.dumps(QUERY_JSON, ensure_ascii=False, separators=(",", ":"))
    return f"""/*
فرم چاپی ورود اضطراری — فیلدهای گزارش DevExpress قدیمی EnexEntryFormReport.
SavedQuery دو نتیجه دارد: dtMaster (سربرگ) و dtDetail (اقلام).
قالب: Data/Seed/HrmReports/EnexEntryForm.json
Content فقط وقتی بازنویسی می‌شود که خالی باشد یا آخرین تغییرش با همین seed بوده.
UTF-8 BOM. sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\\Scripts\\Seed_EnexEntryForm_Report.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = SYSDATETIME();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-enex-entry-form';
    DECLARE @Name NVARCHAR(200) = N'EnexEntryForm';
    DECLARE @Title NVARCHAR(200) = N'فرم ورود اضطراری';
    DECLARE @CustomQuery NVARCHAR(MAX) = {sql_n(CUSTOM_QUERY)};
    DECLARE @QueryJson NVARCHAR(MAX) = {sql_n(query_json)};
    DECLARE @ColumnsJson NVARCHAR(MAX) = {sql_n(columns)};
    DECLARE @Content NVARCHAR(MAX) = {sql_n(content)};
    DECLARE @SavedQueryId BIGINT;

    IF EXISTS (SELECT 1 FROM [system].[SavedQuery] WHERE Name = @Name)
    BEGIN
        UPDATE [system].[SavedQuery]
        SET Title = @Title, QueryJson = @QueryJson, ColumnsJson = @ColumnsJson,
            EntityFullName = NULL, Mode = 1, Type = 0, ActionOptions = N'',
            CustomActionButtonsJson = N'[]', EventScriptsJson = N'{{"onSelectedRow":"","onRowAdded":""}}',
            DiagramJson = N'[]', ModifiedById = 1, ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi, IsActive = 1
        WHERE Name = @Name;
        SELECT @SavedQueryId = Id FROM [system].[SavedQuery] WHERE Name = @Name;
    END
    ELSE
    BEGIN
        INSERT INTO [system].[SavedQuery]
            (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
             Mode, Type, EntityFullName, ActionOptions,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (@Name, @Title, @QueryJson, @ColumnsJson, N'[]', N'[]', N'{{"onSelectedRow":"","onRowAdded":""}}',
             1, 0, NULL, N'',
             1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
        SET @SavedQueryId = SCOPE_IDENTITY();
    END;

    IF EXISTS (SELECT 1 FROM dbo.ReportBuilderReport WHERE Name = @Name)
    BEGIN
        UPDATE dbo.ReportBuilderReport
        SET Title = @Title, BaseQuery = @CustomQuery, SavedQueryId = @SavedQueryId, Type = 1,
            ObjectType = N'SavedQuery',
            Content = CASE WHEN Content IS NULL OR ModifiedByName = @SeedUser THEN @Content ELSE Content END,
            ModifiedById = 1,
            ModifiedByName = CASE WHEN Content IS NULL OR ModifiedByName = @SeedUser THEN @SeedUser ELSE ModifiedByName END,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi, IsActive = 1
        WHERE Name = @Name;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.ReportBuilderReport
            (Name, Title, ObjectType, SavedQueryId, Type, Content, BaseQuery,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (@Name, @Title, N'SavedQuery', @SavedQueryId, 1, @Content, @CustomQuery,
             1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
    END;

    COMMIT TRANSACTION;
    PRINT N'EnexEntryForm SavedQueryId=' + CONVERT(nvarchar(20), @SavedQueryId);
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
"""


def main():
    report = build_report()
    content = json.dumps(report, ensure_ascii=False, indent=2)
    JSON_OUT.write_text(content, encoding="utf-8-sig")
    SQL_OUT.write_text(build_sql(content), encoding="utf-8-sig")
    print("json", JSON_OUT.stat().st_size, "sql", SQL_OUT.stat().st_size)


if __name__ == "__main__":
    main()
