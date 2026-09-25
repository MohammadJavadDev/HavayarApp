# -*- coding: utf-8 -*-
"""Generate CngRepairsID.json matching HTS CngRepairsID + CngRepairsIDManHour (combined viewer)."""
from __future__ import annotations

import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
LOGO_B64 = (HERE / "havayar_logo_from_hts.b64").read_text(encoding="ascii").strip()

# A4 portrait printable width ~20.3 cm (margins ~0.5/0.2)
W = 20.3
DIST = (
    "توزيع نسخ: خدمات پس از فروش ، تعميرگاه ، انبار "
    "(  پيمانكار : S، تعميرات كارخانه :  F ، واحد ماشين كاري كار خانه : C )"
)
SIG_BODY = "نام و نام خانوادگی:\n\nتاریخ:\nامضا"
SCRIPT = (
    "using System;\r\nusing System.Drawing;\r\nusing System.Windows.Forms;\r\n"
    "using System.Data;\r\nusing Stimulsoft.Controls;\r\nusing Stimulsoft.Base.Drawing;\r\n"
    "using Stimulsoft.Report;\r\nusing Stimulsoft.Report.Dialogs;\r\nusing Stimulsoft.Report.Components;\r\n\r\n"
    "namespace Reports\r\n{\r\n    public class CngRepairsID : Stimulsoft.Report.StiReport\r\n    {\r\n"
    "        public CngRepairsID()        {\r\n            this.InitializeComponent();\r\n        }}\r\n\r\n"
    "        #region StiReport Designer generated code - do not modify\r\n"
    "        #endregion StiReport Designer generated code - do not modify\r\n    }\r\n}\r\n"
)


def text(
    name: str,
    rect: str,
    value: str,
    *,
    font: str = "IRANSansWeb(FaNum) Medium;8;;",
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


def sti_image(name: str, rect: str, b64: str) -> dict:
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


def cell(name, x, y, w, h, value, **kw):
    return text(name, f"{x},{y},{w},{h}", value, **kw)


# ---- Parts table columns (LTR x, RTL visual: # on right) ----
# widths sum ~20.3
PART_COLS = [
    # name, x, w, header, data expr, font_h, font_d, ha
    ("amt", 0.0, 1.9, "مبلغ", "{Format(\"{0:#,0}\", CngRepairsPart.TotalPrice)}", "IRANSansWeb(FaNum) Medium;7;;", "IRANSansWeb(FaNum) Medium;7;;", "Center"),
    ("sale", 1.9, 1.6, "فی(فروش)", "{Format(\"{0:#,0}\", CngRepairsPart.SalePrice)}", "IRANSansWeb(FaNum) Medium;7;;", "IRANSansWeb(FaNum) Medium;7;;", "Center"),
    ("buy", 3.5, 1.7, "فی(خرید)", "{Format(\"{0:#,0}\", CngRepairsPart.BuyPrice)}", "IRANSansWeb(FaNum) Medium;7;;", "IRANSansWeb(FaNum) Medium;7;;", "Center"),
    ("loan", 5.2, 1.6, "شماره فرم تحویل اقلام امانی", "", "IRANSansWeb(FaNum) Medium;6;;", "IRANSansWeb(FaNum) Medium;7;;", "Center"),
    ("dmg", 6.8, 1.0, "داغی", "{IIF(CngRepairsPart.DamagedPart, \"☑\", \"☐\")}", "IRANSansWeb(FaNum) Medium;7;;", "IRANSansWeb(FaNum) Medium;9;;", "Center"),
    ("req", 7.8, 2.2, "شماره درخواست\n/ نام گیرنده", "", "IRANSansWeb(FaNum) Medium;6;;", "IRANSansWeb(FaNum) Medium;7;;", "Center"),
    ("unit", 10.0, 1.0, "واحد", "{CngRepairsPart.PartUnit_Title}", "IRANSansWeb(FaNum) Medium;7;;", "IRANSansWeb(FaNum) Medium;7;;", "Center"),
    ("qty", 11.0, 0.9, "تعداد", "{CngRepairsPart.Tafazol}", "IRANSansWeb(FaNum) Medium;7;;", "IRANSansWeb(FaNum) Medium;7;;", "Center"),
    ("pname", 11.9, 4.0, "نام کالا", "{CngRepairsPart.Part_Name}", "IRANSansWeb(FaNum) Medium;7;;", "IRANSansWeb(FaNum) Medium;7;;", "Center"),
    ("pcode", 15.9, 2.5, "کد کالا", "{CngRepairsPart.Part_Code}", "IRANSansWeb(FaNum) Medium;7;;", "IRANSansWeb(FaNum) Medium;7;;", "Center"),
    ("row", 18.4, 1.9, "#", "{Line}", "IRANSansWeb(FaNum) Medium;7;;", "IRANSansWeb(FaNum) Medium;7;;", "Center"),
]


def parts_header_comps():
    comps = []
    for name, x, w, hdr, _expr, fh, _fd, ha in PART_COLS:
        comps.append(
            cell(
                f"PH_{name}",
                x,
                0.02,
                w,
                0.7,
                hdr,
                font=fh,
                ha=ha,
                border="All;;;;;;;solid:Black",
                brush="solid:192,192,192",
            )
        )
    return {str(i): c for i, c in enumerate(comps)}


def parts_data_comps():
    comps = []
    for name, x, w, _hdr, expr, _fh, fd, ha in PART_COLS:
        comps.append(
            cell(
                f"PD_{name}",
                x,
                0.02,
                w,
                0.55,
                expr,
                font=fd,
                ha=ha,
                border="All;;;;;;;solid:Black",
                can_grow=(name == "pname"),
                rtl=(name not in ("pcode", "row", "qty", "buy", "sale", "amt")),
            )
        )
    return {str(i): c for i, c in enumerate(comps)}


# ---- Man-hour columns ----
MH_COLS = [
    ("cmt", 0.0, 6.5, "نظرات", "{CngRepairsManHour.Comment}", "Right"),
    ("price", 6.5, 2.5, "هزینه واحد", "{Format(\"{0:#,0}\", CngRepairsManHour.ManHourPrice)}", "Center"),
    ("wh", 9.0, 2.2, "کارکرد", "{CngRepairsManHour.WorkingHour}", "Center"),
    ("fam", 11.2, 3.8, "نام خانوادگی", "{CngRepairsManHour.PrsFamily}", "Right"),
    ("nam", 15.0, 3.3, "نام", "{CngRepairsManHour.PrsName}", "Right"),
    ("row", 18.3, 2.0, "#", "{Line}", "Center"),
]


def mh_header_comps():
    comps = []
    for name, x, w, hdr, _e, ha in MH_COLS:
        comps.append(
            cell(
                f"MHH_{name}",
                x,
                0.02,
                w,
                0.55,
                hdr,
                font="IRANSansWeb(FaNum) Medium;8;;",
                ha=ha,
                border="All;;;;;;;solid:Black",
                brush="solid:192,192,192",
            )
        )
    return {str(i): c for i, c in enumerate(comps)}


def mh_data_comps():
    comps = []
    for name, x, w, _h, expr, ha in MH_COLS:
        comps.append(
            cell(
                f"MHD_{name}",
                x,
                0.02,
                w,
                0.5,
                expr,
                font="IRANSansWeb(FaNum) Medium;8;;",
                ha=ha,
                border="All;;;;;;;solid:Black",
                can_grow=(name == "cmt"),
                rtl=(name not in ("price", "wh", "row")),
            )
        )
    return {str(i): c for i, c in enumerate(comps)}


def signature_block(y0: float = 0.0) -> list:
    """5 approval columns matching HTS PageFooter."""
    titles = [
        ("تایید انبار", 0.0),
        ("تایید مدیر ارشد", 4.06),
        ("تایید مدیر واحد", 8.12),
        ("سرپرست منطقه", 12.18),
        ("سرپرست تعمیرات", 16.24),
    ]
    w = 4.06
    comps = []
    for i, (title, x) in enumerate(titles):
        comps.append(
            cell(
                f"SigT{i}",
                x,
                y0,
                w,
                0.45,
                title,
                font="IRANSansWeb(FaNum) Medium;8;;",
                ha="Center",
                border="All;;;;;;;solid:Black",
                brush="solid:230,230,230",
            )
        )
        comps.append(
            cell(
                f"SigB{i}",
                x,
                y0 + 0.45,
                w,
                1.9,
                SIG_BODY,
                font="IRANSansWeb(FaNum) Medium;8;;",
                ha="Right",
                va="Top",
                border="All;;;;;;;solid:Black",
            )
        )
    comps.append(
        cell(
            "FormCode",
            0.0,
            y0 + 2.4,
            3.5,
            0.55,
            "FR09-020/03",
            font="IRANSansWeb(FaNum) Medium;10;;",
            ha="Center",
            border="All;;;;;;;solid:Black",
            rtl=False,
        )
    )
    comps.append(
        cell(
            "FormDist",
            3.5,
            y0 + 2.4,
            16.8,
            0.55,
            DIST,
            font="IRANSansWeb(FaNum) Medium;7;;",
            ha="Center",
            border="All;;;;;;;solid:Black",
        )
    )
    return comps


# ========== PAGE 1: ID form ==========
# Header: meta | title | logo  (HTS: meta left, logo right in LTR; RTL report flips visually)
p1_title = []
# Frames
p1_title.append(cell("MetaFrame", 0.0, 0.05, 5.8, 2.3, "", border="All;;;;;;;solid:Black"))
p1_title.append(
    cell(
        "TitleFrame",
        5.8,
        0.05,
        8.7,
        2.3,
        "فرم درخواست تعمیر قطعات",
        font="IRANSansWeb(FaNum) Medium;18;;",
        ha="Center",
        border="All;;;;;;;solid:Black",
    )
)
p1_title.append(cell("LogoFrame", 14.5, 0.05, 5.8, 2.3, "", border="All;;;;;;;solid:Black"))
p1_title.append(sti_image("Logo", "14.9,0.25,5.0,1.9", LOGO_B64))

# Meta labels/values (right-aligned Persian labels on the right of meta cell)
p1_title += [
    cell("LReq", 3.5, 0.2, 2.1, 0.55, "شماره درخواست:", font="IRANSansWeb(FaNum) Medium;7;;", ha="Right"),
    cell("VReq", 0.15, 0.2, 3.3, 0.55, "{CngRepairs.IDNumber}", font="IRANSansWeb(FaNum) Medium;10;;", ha="Left", rtl=False, text_brush="solid:128,0,0"),
    cell("LWh", 3.5, 0.85, 2.1, 0.55, "تاریخ تحویل به انبار:", font="IRANSansWeb(FaNum) Medium;7;;", ha="Right"),
    cell(
        "VWh",
        0.15,
        0.85,
        3.3,
        0.55,
        "{CngRepairs.DeliveryDate_Warehouse_Shamsi}",
        font="IRANSansWeb(FaNum) Medium;10;;",
        ha="Left",
        rtl=False,
        text_brush="solid:128,0,0",
    ),
    cell("LEnt", 3.5, 1.5, 2.1, 0.55, "تاریخ ورود به تعمیرگاه:", font="IRANSansWeb(FaNum) Medium;7;;", ha="Right"),
    cell(
        "VEnt",
        0.15,
        1.5,
        3.3,
        0.55,
        "{CngRepairs.EntryDate_Shamsi}",
        font="IRANSansWeb(FaNum) Medium;10;;",
        ha="Left",
        rtl=False,
        text_brush="solid:128,0,0",
    ),
]

# Equipment / customer block (silver rows like HTS)
y = 2.5
row_h = 0.65
# Row1: serial | code | name
p1_title += [
    cell("R1a", 0.0, y, 6.5, row_h, "", border="All;;;;;;;solid:Black", brush="solid:192,192,192"),
    cell("LSer", 4.2, y + 0.05, 2.2, 0.55, "شماره سریال تجهیز/قطعه:", font="IRANSansWeb(FaNum) Medium;7;;"),
    cell("VSer", 0.1, y + 0.05, 4.0, 0.55, "{CngRepairs.Serial}", font="IRANSansWeb(FaNum) Medium;8;;", ha="Center", rtl=False),
    cell("R1b", 6.5, y, 4.0, row_h, "", border="All;;;;;;;solid:Black", brush="solid:192,192,192"),
    cell("LCode", 9.0, y + 0.05, 1.4, 0.55, "کد کالا:", font="IRANSansWeb(FaNum) Medium;7;;"),
    cell("VCode", 6.6, y + 0.05, 2.3, 0.55, "{CngRepairs.Part_Code}", font="IRANSansWeb(FaNum) Medium;8;;", ha="Center", rtl=False),
    cell("R1c", 10.5, y, 9.8, row_h, "", border="All;;;;;;;solid:Black", brush="solid:192,192,192"),
    cell("LName", 17.5, y + 0.05, 2.7, 0.55, "نام تجهیز قابل تعمیر:", font="IRANSansWeb(FaNum) Medium;7;;"),
    cell("VName", 10.6, y + 0.05, 6.8, 0.55, "{CngRepairs.Part_Name}", font="IRANSansWeb(FaNum) Medium;8;;", ha="Center"),
]

y += row_h
# Row2: send date (blank) + count | agency | customer
p1_title += [
    cell("R2a", 0.0, y, 6.5, row_h, "", border="All;;;;;;;solid:Black", brush="solid:192,192,192"),
    cell("LSend", 4.5, y + 0.05, 1.9, 0.55, "تاریخ ارسال:", font="IRANSansWeb(FaNum) Medium;7;;"),
    cell("VSend", 3.2, y + 0.05, 1.2, 0.55, "", font="IRANSansWeb(FaNum) Medium;8;;", ha="Center", rtl=False),  # blank — no HTS field
    cell("LCnt", 2.2, y + 0.05, 0.9, 0.55, "تعداد:", font="IRANSansWeb(FaNum) Medium;7;;"),
    cell("VCnt", 0.1, y + 0.05, 2.0, 0.55, "{CngRepairs.Count}", font="IRANSansWeb(FaNum) Medium;8;;", ha="Center", rtl=False),
    cell("R2b", 6.5, y, 4.0, row_h, "", border="All;;;;;;;solid:Black", brush="solid:192,192,192"),
    cell("LAg", 9.0, y + 0.05, 1.4, 0.55, "نمایندگی:", font="IRANSansWeb(FaNum) Medium;7;;"),
    cell("VAg", 6.6, y + 0.05, 2.3, 0.55, "{CngRepairs.Agency}", font="IRANSansWeb(FaNum) Medium;8;;", ha="Center"),
    cell("R2c", 10.5, y, 9.8, row_h, "", border="All;;;;;;;solid:Black", brush="solid:192,192,192"),
    cell("LCust", 18.0, y + 0.05, 2.2, 0.55, "نام مشتری:", font="IRANSansWeb(FaNum) Medium;7;;"),
    cell("VCust", 10.6, y + 0.05, 7.3, 0.55, "{CngRepairs.Customer_Title}", font="IRANSansWeb(FaNum) Medium;8;;", ha="Center"),
]

y += row_h
# Row3: destination | vendor
p1_title += [
    cell("R3a", 0.0, y, 7.5, row_h, "", border="All;;;;;;;solid:Black"),
    cell("LDest", 5.5, y + 0.05, 1.9, 0.55, "مقصد پس از تعمیر:", font="IRANSansWeb(FaNum) Medium;6;;"),
    cell(
        "VDest",
        0.1,
        y + 0.05,
        5.3,
        0.55,
        "{CngRepairs.DestinationAfterRepair}",
        font="IRANSansWeb(FaNum) Medium;8;;",
        ha="Right",
    ),
    cell("R3b", 7.5, y, 12.8, row_h, "", border="All;;;;;;;solid:Black"),
    cell("LVen", 18.5, y + 0.05, 1.7, 0.55, "پیمانکار :", font="IRANSansWeb(FaNum) Medium;7;;"),
    cell("VVen", 7.6, y + 0.05, 10.8, 0.55, "{CngRepairs.VendorName}", font="IRANSansWeb(FaNum) Medium;8;;", ha="Right"),
]

y += row_h
# Row4: address / customer need date (blank) / phone
p1_title += [
    cell("R4", 0.0, y, 20.3, row_h, "", border="All;;;;;;;solid:Black"),
    cell("VPhone", 0.15, y + 0.05, 2.8, 0.55, "{CngRepairs.Customer_Phone}", font="IRANSansWeb(FaNum) Medium;8;;", ha="Center", rtl=False),
    cell("LPhone", 3.0, y + 0.05, 1.0, 0.55, "تلفن:", font="IRANSansWeb(FaNum) Medium;7;;", ha="Center"),
    cell("VNeed", 4.1, y + 0.05, 2.5, 0.55, "", font="IRANSansWeb(FaNum) Medium;8;;", ha="Center", rtl=False),  # blank — تاریخ نیاز مشتری
    cell("LNeed", 6.7, y + 0.05, 2.2, 0.55, "تاریخ نیاز مشتری:", font="IRANSansWeb(FaNum) Medium;7;;", ha="Center"),
    cell(
        "VAddr",
        9.0,
        y + 0.05,
        9.5,
        0.55,
        "{CngRepairs.DestinationAddress}",
        font="IRANSansWeb(FaNum) Medium;8;;",
        ha="Center",
    ),
    cell("LAddr", 18.6, y + 0.05, 1.5, 0.55, "به آدرس:", font="IRANSansWeb(FaNum) Medium;7;;"),
]

y += row_h
# Row5: status / warranty / receiver
p1_title += [
    cell("R5a", 0.0, y, 5.0, 1.1, "", border="All;;;;;;;solid:Black"),
    cell("LRecv", 0.15, y + 0.05, 4.7, 0.45, "نام تحویل گیرنده:", font="IRANSansWeb(FaNum) Medium;7;;", ha="Right"),
    cell("LSignR", 0.15, y + 0.55, 4.7, 0.45, "امضاء", font="IRANSansWeb(FaNum) Medium;7;;", ha="Right"),
    cell("R5b", 5.0, y, 3.5, 1.1, "", border="All;;;;;;;solid:Black"),
    cell(
        "ChkG",
        6.6,
        y + 0.1,
        1.8,
        0.4,
        '{IIF(CngRepairs.IsGuarantee, "☑", "☐")} گارانتی',
        font="IRANSansWeb(FaNum) Medium;7;;",
        ha="Center",
    ),
    cell(
        "ChkW",
        5.1,
        y + 0.1,
        1.5,
        0.4,
        '{IIF(CngRepairs.ISWarranty, "☑", "☐")} وارانتی',
        font="IRANSansWeb(FaNum) Medium;7;;",
        ha="Center",
    ),
    cell("LMgr", 5.1, y + 0.55, 3.3, 0.45, "امضاء مدیر واحد", font="IRANSansWeb(FaNum) Medium;7;;", ha="Center"),
    cell("R5c", 8.5, y, 11.8, 1.1, "", border="All;;;;;;;solid:Black"),
    cell(
        "LStatus",
        14.5,
        y + 0.3,
        5.6,
        0.5,
        "شرح وضعيت قطعه و متعلقات آن:",
        font="IRANSansWeb(FaNum) Medium;8;;",
        ha="Left",
    ),
]

y += 1.1
# Failure reason
p1_title += [
    cell("R6", 0.0, y, 20.3, 0.7, "", border="All;;;;;;;solid:Black"),
    cell(
        "VFail",
        0.1,
        y + 0.05,
        17.8,
        0.6,
        "{CngRepairs.FailureReason}",
        font="IRANSansWeb(FaNum) Medium;8;;",
        ha="Left",
        can_grow=True,
    ),
    cell("LFail", 18.0, y + 0.05, 2.2, 0.6, "علت خرابی:", font="IRANSansWeb(FaNum) Medium;8;;", ha="Center"),
]

title_h = y + 0.85  # ~7.9

# Cost footer (after parts)
cost_comps = [
    # Row A: TotalBuy | label | blank loan form | blank | blank delivery
    cell("C_TotalBuy", 0.0, 0.0, 4.0, 0.55, "{Format(\"{0:#,0}\", CngRepairs.TotalBuy)}", font="IRANSansWeb(FaNum) Medium;8;;", ha="Center", border="All;;;;;;;solid:Black"),
    cell("C_LBuy", 4.0, 0.0, 2.2, 0.55, "جمع هزینه قطعه", font="IRANSansWeb(FaNum) Medium;6;;", ha="Center", border="All;;;;;;;solid:Black"),
    cell("C_BlankLoan", 6.2, 0.0, 3.5, 0.55, "", font="IRANSansWeb(FaNum) Medium;8;;", border="All;;;;;;;solid:Black"),
    cell("C_LLoan", 9.7, 0.0, 3.5, 0.55, "شماره فرم حواله ارسال امانی", font="IRANSansWeb(FaNum) Medium;6;;", ha="Center", border="All;;;;;;;solid:Black"),
    cell("C_BlankDel", 13.2, 0.0, 4.0, 0.55, "", font="IRANSansWeb(FaNum) Medium;8;;", border="All;;;;;;;solid:Black"),
    cell("C_LDel", 17.2, 0.0, 3.1, 0.55, "شماره فرم تحویل", font="IRANSansWeb(FaNum) Medium;6;;", ha="Center", border="All;;;;;;;solid:Black"),
    # Row B: VendoringCost | label | manhour price | label | blank preinvoice
    cell(
        "C_Vend",
        0.0,
        0.55,
        4.0,
        0.55,
        "{Format(\"{0:#,0}\", CngRepairs.VendoringCost)}",
        font="IRANSansWeb(FaNum) Medium;8;;",
        ha="Center",
        border="All;;;;;;;solid:Black",
    ),
    cell("C_LVend", 4.0, 0.55, 2.2, 0.55, "سایرهزینه ها/پیمانکاری", font="IRANSansWeb(FaNum) Medium;6;;", ha="Center", border="All;;;;;;;solid:Black"),
    cell(
        "C_Mh",
        6.2,
        0.55,
        3.5,
        0.55,
        "{Format(\"{0:#,0}\", CngRepairsTotalManHourPrice.PriceManInHour)}",
        font="IRANSansWeb(FaNum) Medium;8;;",
        ha="Center",
        border="All;;;;;;;solid:Black",
    ),
    cell("C_LMh", 9.7, 0.55, 3.5, 0.55, "هزینه نفر/ساعت", font="IRANSansWeb(FaNum) Medium;6;;", ha="Center", border="All;;;;;;;solid:Black"),
    cell("C_BlankPre", 13.2, 0.55, 4.0, 0.55, "", font="IRANSansWeb(FaNum) Medium;8;;", border="All;;;;;;;solid:Black"),
    cell("C_LPre", 17.2, 0.55, 3.1, 0.55, "شماره پیش فاکتور", font="IRANSansWeb(FaNum) Medium;6;;", ha="Center", border="All;;;;;;;solid:Black"),
    # Total row
    cell(
        "C_Grand",
        0.0,
        1.1,
        4.0,
        0.9,
        "{Format(\"{0:#,0}\", CngRepairs.TotalBuy + CngRepairs.VendoringCost + CngRepairsTotalManHourPrice.PriceManInHour)}",
        font="IRANSansWeb(FaNum) Medium;9;;",
        ha="Center",
        border="All;;;;;;;solid:Black",
    ),
    cell("C_LGrand", 4.0, 1.1, 2.2, 0.9, "جمع کل", font="IRANSansWeb(FaNum) Medium;7;;", ha="Center", border="All;;;;;;;solid:Black"),
    cell(
        "C_ItemsNote",
        6.2,
        1.1,
        14.1,
        0.9,
        "مشخصات و تعداد اقلام تعمير شده تحويلي به انبار: ",
        font="IRANSansWeb(FaNum) Medium;8;;",
        ha="Right",
        va="Top",
        border="All;;;;;;;solid:Black",
    ),
]
cost_comps += signature_block(2.15)
footer1_h = 2.15 + 3.1

# ========== PAGE 2: Man hours ==========
p2_title = [
    cell(
        "MhTitle",
        0.0,
        0.1,
        20.3,
        0.8,
        "لیست تعمیرکاران",
        font="IRANSansWeb(FaNum) Medium;18;;",
        ha="Center",
        border="All;;;;;;;solid:Black",
    )
]

mh_footer = [
    cell(
        "MhSumPrice",
        0.0,
        0.05,
        4.0,
        0.55,
        "{Format(\"{0:#,0}\", Sum(CngRepairsManHour.ManHourPrice))}",
        font="IRANSansWeb(FaNum) Medium;8;;",
        ha="Center",
        border="All;;;;;;;solid:Black",
    ),
    cell(
        "MhSumWh",
        4.0,
        0.05,
        3.5,
        0.55,
        "{CngRepairsManHourMaster.SumWorkingHour}",
        font="IRANSansWeb(FaNum) Medium;8;;",
        ha="Center",
        border="All;;;;;;;solid:Black",
    ),
    cell(
        "MhSumLbl",
        7.5,
        0.05,
        4.5,
        0.55,
        "کارکرد (نفر/ساعت)",
        font="IRANSansWeb(FaNum) Medium;8;;",
        ha="Center",
        border="All;;;;;;;solid:Black",
        brush="solid:230,230,230",
    ),
]
mh_footer += signature_block(0.75)
footer2_h = 0.75 + 3.1

page1 = {
    "Ident": "StiPage",
    "Name": "PageID",
    "Guid": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "Interaction": {"Ident": "StiInteraction"},
    "Border": ";;2;;;;;solid:Black",
    "Brush": "solid:",
    "Components": {
        "0": {
            "Ident": "StiReportTitleBand",
            "Name": "ID_Title",
            "ClientRectangle": f"0,0,{W},{title_h:.2f}",
            "Interaction": {"Ident": "StiInteraction"},
            "Border": ";;;;;;;solid:Black",
            "Brush": "solid:",
            "Components": {str(i): c for i, c in enumerate(p1_title)},
        },
        "1": {
            "Ident": "StiHeaderBand",
            "Name": "ID_PartHeader",
            "ClientRectangle": f"0,{title_h + 0.1:.2f},{W},0.75",
            "Interaction": {"Ident": "StiInteraction"},
            "Border": ";;;;;;;solid:Black",
            "Brush": "solid:",
            "Components": parts_header_comps(),
        },
        "2": {
            "Ident": "StiDataBand",
            "Name": "ID_PartData",
            "ClientRectangle": f"0,{title_h + 0.95:.2f},{W},0.6",
            "Interaction": {"Ident": "StiInteraction"},
            "Border": ";;;;;;;solid:Black",
            "Brush": "solid:",
            "DataSourceName": "CngRepairsPart",
            "Components": parts_data_comps(),
        },
        "3": {
            "Ident": "StiFooterBand",
            "Name": "ID_Footer",
            "ClientRectangle": f"0,{title_h + 1.7:.2f},{W},{footer1_h:.2f}",
            "Interaction": {"Ident": "StiInteraction"},
            "Border": ";;;;;;;solid:Black",
            "Brush": "solid:",
            "Components": {str(i): c for i, c in enumerate(cost_comps)},
        },
    },
    "PaperSize": "A4",
    "PageWidth": 21.0,
    "PageHeight": 29.7,
    "RightToLeft": True,
    "Watermark": {"TextBrush": "solid:50,0,0,0"},
    "Margins": {"Left": 0.35, "Right": 0.35, "Top": 0.25, "Bottom": 0.5},
}

page2 = {
    "Ident": "StiPage",
    "Name": "PageManHour",
    "Guid": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
    "Interaction": {"Ident": "StiInteraction"},
    "Border": ";;2;;;;;solid:Black",
    "Brush": "solid:",
    "Components": {
        "0": {
            "Ident": "StiReportTitleBand",
            "Name": "MH_Title",
            "ClientRectangle": f"0,0,{W},1.0",
            "Interaction": {"Ident": "StiInteraction"},
            "Border": ";;;;;;;solid:Black",
            "Brush": "solid:",
            "Components": {str(i): c for i, c in enumerate(p2_title)},
        },
        "1": {
            "Ident": "StiHeaderBand",
            "Name": "MH_Header",
            "ClientRectangle": f"0,1.15,{W},0.6",
            "Interaction": {"Ident": "StiInteraction"},
            "Border": ";;;;;;;solid:Black",
            "Brush": "solid:",
            "Components": mh_header_comps(),
        },
        "2": {
            "Ident": "StiDataBand",
            "Name": "MH_Data",
            "ClientRectangle": f"0,1.9,{W},0.55",
            "Interaction": {"Ident": "StiInteraction"},
            "Border": ";;;;;;;solid:Black",
            "Brush": "solid:",
            "DataSourceName": "CngRepairsManHour",
            "Components": mh_data_comps(),
        },
        "3": {
            "Ident": "StiFooterBand",
            "Name": "MH_Footer",
            "ClientRectangle": f"0,2.6,{W},{footer2_h:.2f}",
            "Interaction": {"Ident": "StiInteraction"},
            "Border": ";;;;;;;solid:Black",
            "Brush": "solid:",
            "Components": {str(i): c for i, c in enumerate(mh_footer)},
        },
    },
    "PaperSize": "A4",
    "PageWidth": 21.0,
    "PageHeight": 29.7,
    "RightToLeft": True,
    "Watermark": {"TextBrush": "solid:50,0,0,0"},
    "Margins": {"Left": 0.35, "Right": 0.35, "Top": 0.25, "Bottom": 0.5},
}

report = {
    "ReportGuid": "16321174-d200-4c3f-8e0f-8b379aecd809",
    "ReportName": "CngRepairsID",
    "ReportAlias": "شناسنامه تعمیرات CNG",
    "ReportCreated": "/Date(1788956672674+0330)/",
    "ReportChanged": "/Date(1790000000000+0330)/",
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
    "Pages": {"0": page1, "1": page2},
}

out = HERE / "CngRepairsID.json"
out.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8-sig")
print("wrote", out, "bytes", out.stat().st_size, "title_h", round(title_h, 2))
