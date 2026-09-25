# -*- coding: utf-8 -*-
"""Generate CngRepairsLoanReturn.json matching HTS CngTurnedBackFromBorrowingForm."""
from __future__ import annotations

import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
LOGO_B64 = (HERE / "havayar_logo_from_hts.b64").read_text(encoding="ascii").strip()


def text(
    name: str,
    rect: str,
    value: str,
    *,
    font: str = "Tahoma;9;;",
    ha: str = "Right",
    va: str = "Center",
    border: str = ";;;;;;;solid:Black",
    brush: str = "solid:",
    text_brush: str = "solid:Black",
    can_grow: bool = False,
    rtl: bool = True,
) -> dict:
    return {
        "Ident": "StiText",
        "Name": name,
        "ClientRectangle": rect,
        "Interaction": {"Ident": "StiInteraction"},
        "CanGrow": can_grow,
        "Text": {"Value": value},
        "HorAlignment": ha,
        "VertAlignment": va,
        "Font": font,
        "Border": border,
        "Brush": brush,
        "TextBrush": text_brush,
        "TextOptions": {"RightToLeft": rtl, "WordWrap": True},
    }


def image(name: str, rect: str, b64: str) -> dict:
    return {
        "Ident": "StiImage",
        "Name": name,
        "ClientRectangle": rect,
        "Interaction": {"Ident": "StiInteraction"},
        "Border": ";;;;;;;solid:Black",
        "Brush": "empty",
        "Stretch": True,
        "AspectRatio": True,
        "HorAlignment": "Center",
        "VertAlignment": "Center",
        "ImageBytes": b64,
    }


# Page printable width ~20.5 cm (A5 landscape margins)
# LTR coords: left of page = metadata; right of page = logo (matches HTS / TARGET image)

# Three header cells (LTR x): meta | title | logo  — matches HTS / TARGET
header_components = {
    "0": text("MetaFrame", "0.15,0.15,6.5,2.25", "", border="All;;;;;;;solid:Black", ha="Center"),
    "1": text(
        "TitleFrame",
        "6.65,0.15,7.4,2.25",
        "برگشت امانی دیگران نزد ما",
        font="Tahoma;15;Bold;",
        ha="Center",
        va="Center",
        border="All;;;;;;;solid:Black",
    ),
    "2": text("LogoFrame", "14.05,0.15,6.25,2.25", "", border="All;;;;;;;solid:Black", ha="Center"),
    "3": image("Logo", "14.55,0.4,5.2,1.7", LOGO_B64),
    # Meta cell: values | labels | checkboxes (left → right toward title)
    "4": text(
        "ValNo",
        "0.3,0.3,2.4,0.5",
        "{Report.FormNumber}",
        font="Tahoma;10;Bold;",
        ha="Left",
        text_brush="solid:128,0,0",
        rtl=False,
    ),
    "5": text("LblNo", "2.7,0.3,1.2,0.5", "شماره :", font="Tahoma;9;Bold;"),
    "6": text("ChkSale", "4.0,0.3,2.5,0.5", "☐ فروش", font="Tahoma;8;;", ha="Right"),
    "7": text("ValDate", "0.3,0.95,2.4,0.5", "", ha="Left"),
    "8": text("LblDate", "2.7,0.95,1.2,0.5", "تاریخ :", font="Tahoma;9;Bold;"),
    "9": text(
        "ChkHy",
        "3.9,0.95,2.6,0.5",
        "☐ خدمات پس از فروش HY",
        font="Tahoma;7;;",
        ha="Right",
    ),
    "10": text("ValRef", "0.3,1.6,2.0,0.5", "", ha="Left"),
    "11": text("LblRef", "2.3,1.6,1.6,0.5", "عطف به ورود :", font="Tahoma;9;Bold;"),
    "12": text(
        "ChkCng",
        "3.9,1.6,2.6,0.5",
        "☒ خدمات پس از فروش CNG",
        font="Tahoma;7;;",
        ha="Right",
    ),
}

# Columns RTL visual order: ردیف | کد کالا | شرح کالا | مقدار | واحد | توضیحات
# In LTR x: توضیحات(left) ... ردیف(right)
# widths approx: row 1.2 | code 2.8 | name 7.0 | qty 1.6 | unit 1.6 | desc 5.3  = 19.5
COL = {
    "row": ("18.3", "1.9"),
    "code": ("15.3", "2.9"),
    "name": ("8.1", "7.0"),
    "qty": ("6.3", "1.7"),
    "unit": ("4.5", "1.7"),
    "desc": ("0.2", "4.2"),
}


def col_rect(key: str, y: str, h: str) -> str:
    x, w = COL[key]
    return f"{x},{y},{w},{h}"


header_band_cols = {
    "0": text("HRow", col_rect("row", "0.05", "0.55"), "ردیف", font="Tahoma;9;Bold;", ha="Center", border="All;;;;;;;solid:Black", brush="solid:230,230,230"),
    "1": text("HCode", col_rect("code", "0.05", "0.55"), "کد کالا", font="Tahoma;9;Bold;", ha="Center", border="All;;;;;;;solid:Black", brush="solid:230,230,230"),
    "2": text("HName", col_rect("name", "0.05", "0.55"), "شرح کالا", font="Tahoma;9;Bold;", ha="Center", border="All;;;;;;;solid:Black", brush="solid:230,230,230"),
    "3": text("HQty", col_rect("qty", "0.05", "0.55"), "مقدار", font="Tahoma;9;Bold;", ha="Center", border="All;;;;;;;solid:Black", brush="solid:230,230,230"),
    "4": text("HUnit", col_rect("unit", "0.05", "0.55"), "واحد", font="Tahoma;9;Bold;", ha="Center", border="All;;;;;;;solid:Black", brush="solid:230,230,230"),
    "5": text("HDesc", col_rect("desc", "0.05", "0.55"), "توضیحات", font="Tahoma;9;Bold;", ha="Center", border="All;;;;;;;solid:Black", brush="solid:230,230,230"),
}

data_band_cols = {
    "0": text("DRow", col_rect("row", "0.02", "0.5"), "{Line}", font="Tahoma;9;;", ha="Center", border="All;;;;;;;solid:Black"),
    "1": text("DCode", col_rect("code", "0.02", "0.5"), "{Parts.Part_Code}", font="Tahoma;9;;", ha="Center", border="All;;;;;;;solid:Black", rtl=False),
    "2": text("DName", col_rect("name", "0.02", "0.5"), "{Parts.Part_Name}", font="Tahoma;9;;", ha="Right", border="All;;;;;;;solid:Black", can_grow=True),
    "3": text("DQty", col_rect("qty", "0.02", "0.5"), "{Parts.Qty}", font="Tahoma;9;;", ha="Center", border="All;;;;;;;solid:Black"),
    "4": text("DUnit", col_rect("unit", "0.02", "0.5"), "{Parts.PartUnit_Title}", font="Tahoma;9;;", ha="Center", border="All;;;;;;;solid:Black"),
    "5": text("DDesc", col_rect("desc", "0.02", "0.5"), "{Parts.Description}", font="Tahoma;9;;", ha="Right", border="All;;;;;;;solid:Black"),
}

footer_components = {
    "0": text(
        "InsLabel",
        "0.2,0.1,6.5,0.45",
        "مبلغ بیمه کالای ارسالی :",
        font="Tahoma;9;Bold;",
        ha="Right",
    ),
    "1": text(
        "AddrBand",
        "0.2,0.65,20.1,1.3",
        "لطفا اجناس فوق به آدرس شرکت:                                                                                          ارسال گردد\n"
        "نام تحویل گیرنده                                                      شماره های تماس",
        font="Tahoma;9;;",
        ha="Right",
        va="Top",
        border="All;;;;;;;solid:Black",
        can_grow=True,
    ),
    "2": text(
        "SendFrame",
        "10.4,2.15,9.9,2.0",
        "",
        border="All;;;;;;;solid:Black",
    ),
    "3": text("LblSend", "16.5,2.7,3.5,0.5", "نحوه ارسال:", font="Tahoma;9;Bold;"),
    "4": text(
        "ValSend",
        "10.6,2.7,5.7,0.5",
        "{Report.SendMethodTitle}",
        font="Tahoma;10;;",
        ha="Right",
    ),
    "5": text(
        "Prep",
        "0.2,2.15,10.0,2.0",
        "تهیه کننده :\nنام :\nامضاء :",
        font="Tahoma;10;Bold;",
        ha="Right",
        va="Top",
        border="All;;;;;;;solid:Black",
    ),
    "6": text(
        "ApprMgr",
        "10.4,4.35,9.9,1.6",
        "تایید مدیر واحد مربوطه:\nامضاء",
        font="Tahoma;10;Bold;",
        ha="Right",
        va="Top",
        border="All;;;;;;;solid:Black",
    ),
    "7": text(
        "ApprSenior",
        "0.2,4.35,10.0,1.6",
        "تصویب مدیر ارشد مربوطه:\nامضاء",
        font="Tahoma;10;Bold;",
        ha="Right",
        va="Top",
        border="All;;;;;;;solid:Black",
    ),
}

report = {
    "ReportGuid": "10c6ba93-fd67-4739-bbe5-207ede99f679",
    "ReportName": "CngRepairsLoanReturn",
    "ReportAlias": "برگشت امانی دیگران نزد ما",
    "ReportCreated": "/Date(1788956672674+0330)/",
    "ReportChanged": "/Date(1789999999999+0330)/",
    "EngineVersion": "EngineV2",
    "ReportUnit": "Centimeters",
    "Script": (
        "using System;\r\nusing System.Drawing;\r\nusing System.Windows.Forms;\r\n"
        "using System.Data;\r\nusing Stimulsoft.Controls;\r\nusing Stimulsoft.Base.Drawing;\r\n"
        "using Stimulsoft.Report;\r\nusing Stimulsoft.Report.Dialogs;\r\nusing Stimulsoft.Report.Components;\r\n\r\n"
        "namespace Reports\r\n{\r\n    public class CngRepairsLoanReturn : Stimulsoft.Report.StiReport\r\n    {\r\n"
        "        public CngRepairsLoanReturn()        {\r\n            this.InitializeComponent();\r\n        }}\r\n\r\n"
        "        #region StiReport Designer generated code - do not modify\r\n"
        "        #endregion StiReport Designer generated code - do not modify\r\n    }\r\n}\r\n"
    ),
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
    "Pages": {
        "0": {
            "Ident": "StiPage",
            "Name": "Page1",
            "Guid": "5c116cf7-ed4f-401e-8f11-10988710cef9",
            "Interaction": {"Ident": "StiInteraction"},
            "Border": "All;;2;;;;;solid:Black",
            "Brush": "solid:",
            "Components": {
                "0": {
                    "Ident": "StiPageHeaderBand",
                    "Name": "PageHeaderBand1",
                    "ClientRectangle": "0,0,20.5,2.5",
                    "Interaction": {"Ident": "StiInteraction"},
                    "Border": "All;;;;;;;solid:Black",
                    "Brush": "solid:",
                    "Components": header_components,
                },
                "1": {
                    "Ident": "StiHeaderBand",
                    "Name": "HeaderBand1",
                    "ClientRectangle": "0,2.7,20.5,0.65",
                    "Interaction": {"Ident": "StiInteraction"},
                    "Border": ";;;;;;;solid:Black",
                    "Brush": "solid:",
                    "Components": header_band_cols,
                },
                "2": {
                    "Ident": "StiDataBand",
                    "Name": "DataBand1",
                    "ClientRectangle": "0,3.5,20.5,0.55",
                    "Interaction": {"Ident": "StiInteraction"},
                    "Border": ";;;;;;;solid:Black",
                    "Brush": "solid:",
                    "DataSourceName": "Parts",
                    "Components": data_band_cols,
                },
                "3": {
                    "Ident": "StiFooterBand",
                    "Name": "FooterBand1",
                    "ClientRectangle": "0,4.2,20.5,6.2",
                    "Interaction": {"Ident": "StiInteraction"},
                    "Border": ";;;;;;;solid:Black",
                    "Brush": "solid:",
                    "Components": footer_components,
                },
            },
            "PaperSize": "A5",
            "Orientation": "Landscape",
            "PageWidth": 21.0,
            "PageHeight": 14.8,
            "Watermark": {"TextBrush": "solid:50,0,0,0"},
            "Margins": {"Left": 0.25, "Right": 0.25, "Top": 0.25, "Bottom": 0.2},
        }
    },
}

out = HERE / "CngRepairsLoanReturn.json"
out.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
print("wrote", out, "bytes", out.stat().st_size)
