# -*- coding: utf-8 -*-
"""فرم چاپی سفارش ساخت — هم‌تراز با DevExpress قدیمی Pln_ProductionOrderReport (صفحه ۴۶۳)."""
from __future__ import annotations

import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
LOGO_B64 = (HERE.parent / "CngReports" / "havayar_logo_from_hts.b64").read_text(encoding="ascii").strip()

# Letter landscape, همان کاغذ گزارش قدیمی (11 x 8.5 اینچ)
PAGE_W = 27.94
PAGE_H = 21.59
ML = 0.40
MR = 0.40
W = round(PAGE_W - ML - MR, 2)  # 27.14

SCRIPT = (
    "using System;\r\nusing System.Drawing;\r\nusing System.Windows.Forms;\r\n"
    "using System.Data;\r\nusing Stimulsoft.Controls;\r\nusing Stimulsoft.Base.Drawing;\r\n"
    "using Stimulsoft.Report;\r\nusing Stimulsoft.Report.Dialogs;\r\nusing Stimulsoft.Report.Components;\r\n\r\n"
    "namespace Reports\r\n{\r\n    public class ProductionOrderForm : Stimulsoft.Report.StiReport\r\n    {\r\n"
    "        public ProductionOrderForm()        {\r\n            this.InitializeComponent();\r\n        }}\r\n\r\n"
    "        #region StiReport Designer generated code - do not modify\r\n"
    "        #endregion StiReport Designer generated code - do not modify\r\n    }\r\n}\r\n"
)

BORDER = "All;;;;;;;solid:Black"
GRAY = "solid:220,220,220"
WHITE = "solid:255,255,255"
SECTION = "solid:211,211,211"
MAROON = "solid:128,0,0"


def text(
    name: str,
    x: float,
    y: float,
    w: float,
    h: float,
    value: str,
    *,
    font: str = "Tahoma;8;;",
    ha: str = "Right",
    va: str = "Center",
    border: str = BORDER,
    brush: str = WHITE,
    text_brush: str = "solid:Black",
    can_grow: bool = False,
    grow_h: bool = False,
    rtl: bool = True,
) -> dict:
    return {
        "Ident": "StiText",
        "Name": name,
        "ClientRectangle": f"{x:.2f},{y:.2f},{w:.2f},{h:.2f}",
        "Interaction": {"Ident": "StiInteraction"},
        "CanGrow": can_grow,
        "GrowToHeight": grow_h,
        "Text": {"Value": value},
        "HorAlignment": ha,
        "VertAlignment": va,
        "Font": font,
        "Border": border,
        "Brush": brush,
        "TextBrush": text_brush,
        "TextOptions": {"RightToLeft": rtl, "WordWrap": True},
    }


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


def add_quad(comps, y, h, left_label, left_expr, right_label, right_expr, left_ha="Right", right_ha="Right"):
    lbl = 3.55
    half = round(W / 2, 2)
    val_w = round(half - lbl, 2)
    comps.append(text(f"qv_{len(comps)}", 0, y, val_w, h, left_expr, font="Tahoma;8;Bold;", ha=left_ha))
    comps.append(text(f"ql_{len(comps)}", val_w, y, lbl, h, left_label, brush=GRAY, font="Tahoma;8;;", ha="Right"))
    comps.append(text(f"qv_{len(comps)}", half, y, val_w, h, right_expr, font="Tahoma;8;Bold;", ha=right_ha))
    comps.append(text(f"ql_{len(comps)}", round(half + val_w, 2), y, round(W - half - val_w, 2), h, right_label, brush=GRAY, font="Tahoma;8;;", ha="Right"))


def add_full(comps, y, h, label, expr, *, value_ha="Right", value_font="Tahoma;8;Bold;", can_grow=False):
    lbl = 3.55
    val_w = round(W - lbl, 2)
    comps.append(text(f"fv_{len(comps)}", 0, y, val_w, h, expr, font=value_font, ha=value_ha, can_grow=can_grow, va="Center"))
    comps.append(text(f"fl_{len(comps)}", val_w, y, round(W - val_w, 2), h, label, brush=GRAY, font="Tahoma;8;;", ha="Right"))


def scale_cols(weights):
    total = sum(weights)
    widths = [round(w / total * W, 2) for w in weights]
    drift = round(W - sum(widths), 2)
    # اختلاف رُند را روی ستون عنوان (پهن‌ترین) می‌گذاریم
    name_i = weights.index(max(weights))
    widths[name_i] = round(widths[name_i] + drift, 2)
    xs = []
    x = 0.0
    for w in widths:
        xs.append(round(x, 2))
        x = round(x + w, 2)
    return list(zip(xs, widths))


# ستون‌ها از چپ به راست، مطابق ترتیب سلول‌های DevExpress (ستون # سمت راست)
# بازرسی | تاریخ | تعداد | رطوبت | دما | خلوص | بارومتریک | فشار | ظرفیت | عنوان | کد | #
COL_WEIGHTS = [0.49, 0.32, 0.17, 0.21, 0.25, 0.20, 0.24, 0.17, 0.26, 2.26, 0.41, 0.14]
COL_POS = scale_cols(COL_WEIGHTS)
COL_DEF = [
    ("insp", "بازرسی حین ساخت", '{IIF(dtDetail.HasInspection, "☑", "☐")}', "Center", False),
    ("date", "ت تحویل توافقی", "{dtDetail.AgreedDeliverDate}", "Center", False),
    ("qty", "تعداد", "{dtDetail.Mount}", "Center", False),
    ("hum", "رطوبت", "{dtDetail.RelativeHumidity}", "Center", False),
    ("temp", "دمای بیشینه", "{dtDetail.MaximumTemperature}", "Center", False),
    ("pur", "درصد خلوص", "{dtDetail.PurityPercentage}", "Center", False),
    ("baro", "فشار بارومتریک", "{dtDetail.BarometricPressure}", "Center", False),
    ("pres", "فشار", "{dtDetail.PressureWorking}", "Center", False),
    ("cap", "ظرفیت", "{dtDetail.Capacity}", "Center", False),
    ("name", "عنوان", "{dtDetail.Part_Name}", "Right", True),
    ("code", "کد تجهیز", "{dtDetail.Part_Code}", "Center", False),
    ("row", "#", "{Line}", "Center", False),
]


def header_components():
    comps = []
    # نوار عنوان: شماره/تاریخ چپ، عنوان وسط، لوگو راست
    side = 6.6
    mid = round(W - side * 2, 2)
    comps.append(text("hNumLbl", side - 1.35, 0.12, 1.25, 0.58, "شماره", border=";;;;;;;solid:Black", brush="solid:", font="Tahoma;9;;", ha="Right"))
    comps.append(text("hNumVal", 0.15, 0.12, side - 1.55, 0.58, "{dtMaster.ProductionOrder_Number}", border=";;;;;;;solid:Black", brush="solid:", font="Tahoma;10;Bold;", ha="Left", text_brush=MAROON, rtl=False))
    comps.append(text("hDateLbl", side - 1.35, 0.78, 1.25, 0.58, "تاریخ", border=";;;;;;;solid:Black", brush="solid:", font="Tahoma;9;;", ha="Right"))
    comps.append(text("hDateVal", 0.15, 0.78, side - 1.55, 0.58, "{dtMaster.CreatedDate}", border=";;;;;;;solid:Black", brush="solid:", font="Tahoma;10;Bold;", ha="Left", text_brush=MAROON, rtl=False))
    comps.append(text("hTitle", side, 0.0, mid, 1.50, "فرم سفارش ساخت", font="Tahoma;18;Bold;", ha="Center", brush="solid:"))
    comps.append(logo("hLogo", side + mid + 0.35, 0.12, side - 0.7, 1.26))

    y = 1.55
    rh = 0.58
    add_full(comps, y, rh, "مشتری", "{dtMaster.Customer_Title}")
    y += rh
    add_quad(comps, y, rh, "نوع صنعت", "{dtMaster.CustomerIndustryHeader}", "End-User", "{dtMaster.EndUser}")
    y += rh
    add_quad(comps, y, rh, "کد تفضیل پروژه", "{dtMaster.ProjectDl}", "ریز صنعت", "{dtMaster.CustomerIndustry}")
    y += rh
    add_quad(comps, y, rh, "واحد فروش", "{dtMaster.SaleDepartment}", "تفضیل پروژه", "{dtMaster.ProjectDlTitle}")
    y += rh
    add_quad(comps, y, rh, "نمایندگی فروش", "{dtMaster.SalesAgency}", "کارشناس فروش", "{dtMaster.Sale_Personel}")
    y += rh
    add_quad(comps, y, rh, "تاریخ متره", "{dtMaster.MetreDate}", "عطف به متره برآورد شماره", "{dtMaster.MetreNumber}", left_ha="Center")
    y += rh
    add_quad(comps, y, rh, "قرارداد/پیش فاکتور", "{dtMaster.ContractNumber}", "اعلام نیازمندی", "{dtMaster.RequirementsAdvise_Number}", left_ha="Center", right_ha="Center")
    y += rh
    add_quad(comps, y, rh, "نحوه تحویل", "{dtMaster.DeliverType}", "نوع پروژه", "{dtMaster.ProjectType}")
    y += rh
    add_quad(comps, y, rh, "نوع/متولی نصب", "{dtMaster.InstallationType}", "محل نصب", "{dtMaster.InstallationCity}")
    y += rh
    add_quad(
        comps, y, rh,
        "نیاز به مدیر پروژه دارد", '{IIF(dtMaster.IsNeedProjectManager, "☑", "☐")}',
        "نیاز به بازرسی قبل از بسته بندی", '{IIF(dtMaster.IsNeedInspectionBeforePacking, "☑", "☐")}',
        left_ha="Center", right_ha="Center",
    )
    y += rh
    add_quad(
        comps, y, rh,
        "آیا کمپرسور خانه آماده است", '{IIF(dtMaster.CompressorHouseIsReady, "☑", "☐")}',
        "دارد GA", '{IIF(dtMaster.HasGA, "☑", "☐")}',
        left_ha="Center", right_ha="Center",
    )
    y += rh
    comment_h = 1.15
    add_full(comps, y, comment_h, "توضیحات فروش", "{dtMaster.SalesComment}", value_font="Tahoma;8;;", can_grow=True)
    y += comment_h
    sec_h = 0.48
    comps.append(text("secCentrifuge", 0, y, W, sec_h, "کمپرسور سانتریفیوژ", font="Tahoma;9;Bold;", ha="Center", brush=SECTION))
    y += sec_h
    add_quad(
        comps, y, rh,
        "نحوه راه اندازی", "{dtMaster.SetupMethod}",
        "مشتری از قبل دارد", '{IIF(dtMaster.CentrifugeHasCompressor, "☑ بلی", "☐")}',
        right_ha="Center",
    )
    y += rh
    add_quad(
        comps, y, rh,
        "تعداد", "{dtMaster.CentrifugeCount}",
        "ولتاژ الکتروموتور", "{dtMaster.ElectromotorVoltage}",
        left_ha="Center", right_ha="Center",
    )
    y += rh

    col_h = 0.85
    for (x, w), (key, title, _expr, ha, _grow) in zip(COL_POS, COL_DEF):
        comps.append(
            text(
                f"ch_{key}",
                x,
                y,
                w,
                col_h,
                title,
                font="Tahoma;7;Bold;",
                ha="Center",
                brush=GRAY,
                grow_h=True,
            )
        )
    header_h = round(y + col_h, 2)
    return comps, header_h


def detail_components(row_h):
    comps = []
    for (x, w), (key, _title, expr, ha, grow) in zip(COL_POS, COL_DEF):
        comps.append(
            text(
                f"cd_{key}",
                x,
                0,
                w,
                row_h,
                expr,
                font="Tahoma;8;;",
                ha=ha,
                can_grow=grow,
                grow_h=True,
                rtl=(key not in ("code", "row", "qty", "date")),
            )
        )
    return comps


header_comps, header_h = header_components()
row_h = 0.62
detail_comps = detail_components(row_h)

page = {
    "Ident": "StiPage",
    "Name": "Page1",
    "Guid": "c4a1e0b2-7d33-4f1a-9c55-0b6e2a91d4f0",
    "Interaction": {"Ident": "StiInteraction"},
    "Border": ";;2;;;;;solid:Black",
    "Brush": "solid:",
    "Components": {
        "0": {
            "Ident": "StiPageHeaderBand",
            "Name": "PageHeader",
            "ClientRectangle": f"0,0,{W},{header_h}",
            "Interaction": {"Ident": "StiInteraction"},
            "Border": ";;;;;;;solid:Black",
            "Brush": "solid:",
            "Components": {str(i): c for i, c in enumerate(header_comps)},
        },
        "1": {
            "Ident": "StiDataBand",
            "Name": "ItemsBand",
            "ClientRectangle": f"0,{header_h + 0.05},{W},{row_h}",
            "Interaction": {"Ident": "StiInteraction"},
            "CanGrow": True,
            "Border": ";;;;;;;solid:Black",
            "Brush": "solid:",
            "DataSourceName": "dtDetail",
            "Components": {str(i): c for i, c in enumerate(detail_comps)},
        },
        "2": {
            "Ident": "StiPageFooterBand",
            "Name": "PageFooter",
            "ClientRectangle": f"0,0,{W},0.55",
            "Interaction": {"Ident": "StiInteraction"},
            "Border": ";;;;;;;solid:Black",
            "Brush": "solid:",
            "Components": {
                "0": text(
                    "pageNo",
                    round((W - 6) / 2, 2),
                    0.05,
                    6,
                    0.45,
                    "صفحه {PageNumber} از {TotalPageCount}",
                    font="Tahoma;8;;",
                    ha="Center",
                    border=";;;;;;;solid:Black",
                    brush="solid:",
                )
            },
        },
    },
    "PaperSize": "Letter",
    "PageWidth": PAGE_W,
    "PageHeight": PAGE_H,
    "RightToLeft": True,
    "Watermark": {"TextBrush": "solid:50,0,0,0"},
    "Margins": {"Left": ML, "Right": MR, "Top": 0.25, "Bottom": 0.30},
}

report = {
    "ReportGuid": "8f3c2a10-6b44-4e91-a7d2-1c5e9b0f4a77",
    "ReportName": "ProductionOrderForm",
    "ReportAlias": "سفارش ساخت",
    "ReportCreated": "/Date(1758620000000+0330)/",
    "ReportChanged": "/Date(1758620000000+0330)/",
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

out = HERE / "ProductionOrderForm.json"
out.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8-sig")
print("wrote", out, "bytes", out.stat().st_size, "header_h", header_h)
