# -*- coding: utf-8 -*-
"""قالب چاپ برچسب و شناسنامه درخواست تعمیر + اسکریپت SavedQuery / ReportBuilder."""
from __future__ import annotations

import copy
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
CNG = HERE.parent / "CngReports"
LOGO_B64 = (CNG / "havayar_logo_from_hts.b64").read_text(encoding="ascii").strip()
SQL_OUT = HERE.parent.parent / "Scripts" / "Seed_RepairRequest_Reports.sql"

FONT = "IRANSansWeb(FaNum) Medium"
SCRIPT = (
    "using System;\r\nusing System.Drawing;\r\nusing System.Windows.Forms;\r\n"
    "using System.Data;\r\nusing Stimulsoft.Controls;\r\nusing Stimulsoft.Base.Drawing;\r\n"
    "using Stimulsoft.Report;\r\nusing Stimulsoft.Report.Dialogs;\r\nusing Stimulsoft.Report.Components;\r\n\r\n"
    "namespace Reports\r\n{\r\n    public class RepairRequestLabel : Stimulsoft.Report.StiReport\r\n    {\r\n"
    "        public RepairRequestLabel()        {\r\n            this.InitializeComponent();\r\n        }}\r\n\r\n"
    "        #region StiReport Designer generated code - do not modify\r\n"
    "        #endregion StiReport Designer generated code - do not modify\r\n    }\r\n}\r\n"
)


def text(name, rect, value, *, font=f"{FONT};9;;", ha="Right", va="Center", border=";;;;;;;solid:Black",
         brush="solid:", text_brush="solid:Black", can_grow=False, rtl=True):
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


def logo(name, rect):
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
        "ImageBytes": LOGO_B64,
    }


def pair_row(prefix, y, x_lbl, w_lbl, x_val, w_val, label, value, rtl, brush):
    h = 0.62
    return [
        text(f"{prefix}L", f"{x_lbl},{y},{w_lbl},{h}", label, font=f"{FONT};8;;"),
        text(
            f"{prefix}V",
            f"{x_val},{y},{w_val},{h}",
            value,
            font=f"{FONT};10;;" if not rtl else f"{FONT};9;;",
            ha="Right" if rtl else "Left",
            border=";;1;;;;;solid:Black",
            text_brush=brush,
            can_grow=prefix in ("prod", "fail", "addr"),
            rtl=rtl,
        ),
    ]


def build_label():
    right = [
        ("id", "شماره شناسنامه", "{Report.IDNumber}", False, "solid:128,0,0"),
        ("ent", "تاریخ ورود", "{Report.EntryDate_Shamsi}", False, "solid:128,0,0"),
        ("ex", "تاریخ خروج", "{Report.ExitDate_Shamsi}", False, "solid:128,0,0"),
        ("del", "تحویل انبار", "{Report.DeliveryDate_Warehouse_Shamsi}", False, "solid:128,0,0"),
        ("sup", "مسئول منطقه", "{Report.Zone_SupervisorTitle}", True, "solid:Black"),
        ("gar", "گارانتی / وارانتی", "{Report.GuaranteeTitle}", True, "solid:Black"),
        ("ag1", "تاریخ توافقی", "{Report.AgreedDateInText}", False, "solid:Black"),
        ("ag2", "تاریخ توافقی ۲", "{Report.AgreedDate2InText}", False, "solid:Black"),
    ]
    left = [
        ("cust", "مشتری", "{Report.Customer_Title}", True, "solid:Black"),
        ("addr", "سایت مشتری", "{Report.CustomerAddressManual}", True, "solid:Black"),
        ("ser", "سریال", "{Report.Serial}", False, "solid:Black"),
        ("prod", "تجهیز", "{Report.Product}", True, "solid:Black"),
        ("fail", "علت خرابی", "{Report.FailureReason}", True, "solid:Black"),
        ("ag3", "تاریخ توافقی ۳", "{Report.AgreedDate3InText}", False, "solid:Black"),
        ("dest", "مقصد پس از تعمیر", "{Report.DestinationAfterRepair}", True, "solid:Black"),
        ("st", "وضعیت", "{Report.RepairRequestStatus}", True, "solid:Black"),
    ]
    header = {
        "0": logo("Logo", "0.2,0.15,4.2,1.15"),
        "1": text("Title", "8.2,0.25,10.5,0.95", "برچسب تعمیر", font=f"{FONT};18;;", ha="Center"),
    }
    comps = []
    for i, (key, label, value, rtl, brush) in enumerate(right):
        y = 0.12 + i * 0.78
        comps.extend(pair_row(key, y, 23.15, 3.15, 13.55, 9.4, label, value, rtl, brush))
    for i, (key, label, value, rtl, brush) in enumerate(left):
        y = 0.12 + i * 0.78
        comps.extend(pair_row(key, y, 10.15, 3.2, 0.2, 9.75, label, value, rtl, brush))
    data = {str(i): c for i, c in enumerate(comps)}
    report = {
        "ReportGuid": "b7e1c4a2-9d30-4f18-8c55-1a6e0d92b441",
        "ReportName": "RepairRequestLabel",
        "ReportAlias": "برچسب تعمیر",
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
        "Pages": {
            "0": {
                "Ident": "StiPage",
                "Name": "Page1",
                "Guid": "c8f2a1b0-6e44-4d91-a7c2-0b5e9a1f4d20",
                "Interaction": {"Ident": "StiInteraction"},
                "Border": ";;2;;;;;solid:Black",
                "Brush": "solid:",
                "RightToLeft": True,
                "Components": {
                    "0": {
                        "Ident": "StiPageHeaderBand",
                        "Name": "PageHeader",
                        "ClientRectangle": "0,0,27.14,1.45",
                        "Interaction": {"Ident": "StiInteraction"},
                        "Border": ";;;;;;;solid:Black",
                        "Brush": "solid:",
                        "Components": header,
                    },
                    "1": {
                        "Ident": "StiDataBand",
                        "Name": "DataBand1",
                        "ClientRectangle": "0,1.85,27.14,6.5",
                        "Interaction": {"Ident": "StiInteraction"},
                        "Border": ";;;;;;;solid:Black",
                        "Brush": "solid:",
                        "DataSourceName": "Report",
                        "Components": data,
                    },
                },
                "PaperSize": "A4",
                "PageWidth": 29.7,
                "PageHeight": 21.0,
                "Watermark": {"TextBrush": "solid:50,0,0,0"},
                "Margins": {"Left": 0.4, "Right": 0.4, "Top": 0.35, "Bottom": 0.35},
            }
        },
        "ReportVersion": "2024.3.2.0",
        "CalculationMode": "Interpretation",
    }
    _attach_dictionary(report, [_data_source("Report", [
        "Id", "IDNumber", "EntryDate_Shamsi", "ExitDate_Shamsi", "DeliveryDate_Warehouse_Shamsi",
        "Zone_SupervisorTitle", "Customer_Title", "CustomerAddressManual", "Serial", "Product",
        "FailureReason", "GuaranteeTitle", "AgreedDateInText", "AgreedDate2InText", "AgreedDate3InText",
        "DestinationAfterRepair", "RepairRequestStatus",
    ])])
    return report


def _sti_column(name):
    return {
        "Name": name,
        "Index": -1,
        "NameInSource": name,
        "Alias": name,
        "Type": "System.String",
    }


def _data_source(name, columns):
    return {
        "Ident": "StiDataTableSource",
        "Name": name,
        "Alias": name,
        "Key": name,
        "Columns": {str(i): _sti_column(col) for i, col in enumerate(columns)},
        "NameInSource": f"NewDataSet.{name}",
    }


def _attach_dictionary(data, sources):
    """بدون DataSources عبارت‌های {RepairRequest.IDNumber} خالی می‌مانند."""
    dictionary = {"DataSources": {str(i): src for i, src in enumerate(sources)}}
    ordered = {}
    inserted = False
    for key, value in data.items():
        if key == "Dictionary":
            continue
        if key == "Pages" and not inserted:
            ordered["Dictionary"] = dictionary
            inserted = True
        ordered[key] = value
    if not inserted:
        ordered["Dictionary"] = dictionary
    data.clear()
    data.update(ordered)


def _walk(node, fn):
    if isinstance(node, dict):
        fn(node)
        for value in node.values():
            _walk(value, fn)
    elif isinstance(node, list):
        for item in node:
            _walk(item, fn)


def _set_text(root, name, value, **extra):
    def apply(node):
        if node.get("Name") == name and isinstance(node.get("Text"), dict):
            node["Text"]["Value"] = value
            node.update(extra)

    _walk(root, apply)


def _shift_below(components, min_y, dy):
    for comp in components.values():
        parts = str(comp.get("ClientRectangle", "")).split(",")
        if len(parts) != 4:
            continue
        y = float(parts[1])
        if y >= min_y:
            parts[1] = f"{y + dy:g}"
            comp["ClientRectangle"] = ",".join(parts)


def _find_node(root, name):
    found = []

    def visit(node):
        if node.get("Name") == name and ("ClientRectangle" in node or "Ident" in node):
            found.append(node)

    _walk(root, visit)
    return found[0] if found else None


def _require(root, name):
    node = _find_node(root, name)
    if node is None:
        raise KeyError(f"template component not found: {name}")
    return node


def _band_of(page, name):
    for comp in page["Components"].values():
        if comp.get("Name") == name:
            return comp
    raise KeyError(f"band not found: {name}")


def _append(band, comp):
    comps = band["Components"]
    comps[str(max((int(k) for k in comps), default=-1) + 1)] = comp


def _rect(root, name, rect, **extra):
    node = _require(root, name)
    node["ClientRectangle"] = rect
    node.update(extra)
    return node


def _clone(root, src_name, new_name, rect, text, **extra):
    node = copy.deepcopy(_require(root, src_name))
    node["Name"] = new_name
    node["ClientRectangle"] = rect
    node["Text"] = {"Value": text}
    node.update(extra)
    return node


def _checkbox(name, rect, checked=None):
    """Vector check box (does not depend on font glyphs, so it prints the same in viewer, PDF and Excel)."""
    box = {
        "Ident": "StiCheckBox",
        "Name": name,
        "ClientRectangle": rect,
        "Interaction": {"Ident": "StiInteraction"},
        "Brush": "solid:",
        "Border": ";;;;;;;solid:Black",
        "TextBrush": "solid:Black",
        # default styles draw nothing when unchecked, so the empty box would be invisible on paper
        "CheckStyleForTrue": "CheckRectangle",
        "CheckStyleForFalse": "NoneRectangle",
    }
    if checked:
        box["Checked"] = {"Value": checked}
    return box


# ReportBuilder fills every data-table column as a *string* (see ReportBuilderController.BuildDataTableFromQueryResult),
# so numbers must be converted explicitly: Format("{0:#,0}", "123456") does nothing and "1" + "2" concatenates.
def _dec(expr):
    return f"Convert.ToDecimal({expr})"


def _money(expr, blank_zero=False):
    shown = f'Format("{{0:#,0}}", {_dec(expr)})'
    if blank_zero:
        return "{" + f'IIF({_dec(expr)} == 0, "", {shown})' + "}"
    return "{" + shown + "}"


def _flag(expr):
    """bit columns arrive as the strings "True" / "False"."""
    return "{" + f'{expr} == "True"' + "}"


def _fix_main_page(page, title):
    # ---- parts grid: money / quantity / flags -------------------------------------------------
    _set_text(page, "PD_amt", _money("RepairRequestPart.TotalPrice", True))
    _set_text(page, "PD_sale", _money("RepairRequestPart.SalePrice", True))
    _set_text(page, "PD_buy", _money("RepairRequestPart.BuyPrice", True))
    _set_text(
        page,
        "PD_qty",
        "{" + f'IIF({_dec("RepairRequestPart.Tafazol")} == 0, "", Format("{{0:0.###}}", {_dec("RepairRequestPart.Tafazol")}))' + "}",
    )
    _set_text(
        page,
        "PD_req",
        '{IIF(Convert.ToInt64(RepairRequestPart.Hamkaran_InvVchItm_FK) > 0, RepairRequestPart.Hamkaran_InvVchItm_FK, "")}',
    )
    _set_text(page, "PD_row", '{IIF(RepairRequestPart.Part_Code == "", "", Line)}')
    _set_text(page, "PD_dmg", "")

    # a row grows with the part name: every other cell must grow with it, otherwise the borders break
    for name in ("PD_amt", "PD_sale", "PD_buy", "PD_loan", "PD_dmg", "PD_req", "PD_unit", "PD_qty", "PD_pcode", "PD_row"):
        _require(page, name)["GrowToHeight"] = True
    _append(_band_of(page, "ID_PartData"), _checkbox("PD_dmgChk", "7.125,0.1,0.35,0.35", _flag("RepairRequestPart.DamagedPart")))

    # ---- warranty / guarantee check boxes (the old DevExpress form had the "گارانتی" box bound to IsGuarantee) ----
    _set_text(title, "ChkG", "گارانتی", ClientRectangle="6.75,5.75,1.3,0.45", HorAlignment="Right")
    _set_text(title, "ChkW", "وارانتی", ClientRectangle="5.05,5.75,1.25,0.45", HorAlignment="Right")
    _append(title, _checkbox("ChkGBox", "8.1,5.8,0.35,0.35", _flag("RepairRequest.IsGuarantee")))
    _append(title, _checkbox("ChkWBox", "6.35,5.8,0.35,0.35", _flag("RepairRequest.ISWarranty")))

    # ---- failure / description box: grows with the text, "قابل تعمیر" tick box as in the old form ----
    _require(title, "VFail")["CanGrow"] = True
    _require(title, "R6")["GrowToHeight"] = True
    _append(title, _clone(title, "LFail", "LRepairable", "18.15,7.35,1.45,0.45", "قابل تعمیر", HorAlignment="Right"))
    _append(title, _checkbox("RepairableBox", "19.65,7.4,0.35,0.35"))

    # ---- totals footer: same arrangement as the HTS form ------------------------------------------
    footer = _band_of(page, "ID_Footer")
    _set_text(footer, "C_TotalBuy", _money("RepairRequest.TotalBuy"))
    _set_text(footer, "C_Vend", _money("RepairRequest.VendoringCost"))
    _set_text(footer, "C_Mh", _money("RepairRequestTotalManHourPrice.PriceManInHour"))
    _set_text(
        footer,
        "C_Grand",
        "{"
        + 'Format("{0:#,0}", '
        + " + ".join(
            _dec(x)
            for x in (
                "RepairRequest.TotalBuy",
                "RepairRequest.VendoringCost",
                "RepairRequestTotalManHourPrice.PriceManInHour",
            )
        )
        + ")}",
    )
    _set_text(
        footer,
        "C_BlankLoan",
        '{IIF(Convert.ToInt64(RepairRequest.LoanForm_Number) > 0, RepairRequest.LoanForm_Number, "")}',
    )
    _set_text(
        footer,
        "C_BlankPre",
        '{IIF(Convert.ToInt64(RepairRequest.PreFactor_Number) > 0, RepairRequest.PreFactor_Number, "")}',
    )
    _set_text(footer, "C_LMh", "مجموع هزینه نفر/ساعت")
    # row 1: parts | total man-hours (was missing) | delivery form
    _append(footer, _clone(footer, "C_Mh", "C_ManHours", "6.2,0.0,3.5,0.55", "{RepairRequestManHourMaster.SumWorkingHour}"))
    _append(footer, _clone(footer, "C_LMh", "C_LManHours", "9.7,0.0,3.5,0.55", "مجموع نفر/ساعت"))
    # row 3: grand total | loan form | items note
    _rect(footer, "C_BlankLoan", "6.2,1.1,3.5,0.9")
    _rect(footer, "C_LLoan", "9.7,1.1,3.5,0.9")
    _rect(footer, "C_ItemsNote", "13.2,1.1,7.1,0.9")


def _tall_name_row(title, extra=0.6):
    """Equipment names are long (63% of requests have > 40 chars, max 210): room for 3 lines instead of 1."""

    def resize(name, height, **changes):
        node = _require(title, name)
        parts = node["ClientRectangle"].split(",")
        parts[3] = f"{height:g}"
        node["ClientRectangle"] = ",".join(parts)
        node.update(changes)
        return node

    _shift_below(title["Components"], 3.1, extra)
    for name in ("R1a", "R1b", "R1c"):
        resize(name, 0.65 + extra)
    for name in ("LSer", "VSer", "LCode", "VCode", "LName"):
        resize(name, 0.55 + extra)
    name_cell = resize("VName", 0.55 + extra)
    name_cell["Font"] = name_cell["Font"].replace(";8;", ";7;")
    rect = title["ClientRectangle"].split(",")
    rect[3] = f"{float(rect[3]) + extra:g}"
    title["ClientRectangle"] = ",".join(rect)


def _fix_manhour_page(page2):
    _set_text(
        page2,
        "MHD_price",
        _money("RepairRequestManHour.PriceManInHour", True),
    )
    _set_text(page2, "MhSumPrice", _money("RepairRequestManHourMaster.SumOfMissionPrice"))
    _set_text(
        page2,
        "MHD_row",
        '{IIF(Convert.ToInt64(RepairRequestManHour.RowNo) == 0, "", RepairRequestManHour.RowNo)}',
    )
    # the comment cell grows with long text: the neighbouring cells must follow
    for name in ("MHD_price", "MHD_wh", "MHD_fam", "MHD_nam", "MHD_row"):
        _require(page2, name)["GrowToHeight"] = True

    # HTS grouped this list by repairer: column titles per repairer + his own hours / cost under the rows.
    header = _band_of(page2, "MH_Header")
    header["Ident"] = "StiGroupHeaderBand"
    header["Name"] = "MH_GroupHeader"
    # Stimulsoft sorts the rows by the group expression; GroupKey is a zero padded rank of (family, name, id),
    # so that sort keeps the order produced by the query.
    header["Condition"] = {"Value": "{RepairRequestManHour.GroupKey}"}

    def subtotal(name, src, rect, text, **extra):
        node = _clone(page2, src, name, rect, text, **extra)
        node["Brush"] = "solid:230,230,230"
        return node

    group_footer = {
        "Ident": "StiGroupFooterBand",
        "Name": "MH_GroupFooter",
        "ClientRectangle": "0,2.6,20.3,0.6",
        "Interaction": {"Ident": "StiInteraction"},
        "Border": ";;;;;;;solid:Black",
        "Brush": "solid:",
        "Components": {},
    }
    _append(
        group_footer,
        subtotal("MHG_price", "MhSumPrice", "6.5,0.05,2.5,0.5", _money("RepairRequestManHour.PersonPrice")),
    )
    _append(group_footer, subtotal("MHG_wh", "MhSumWh", "9.0,0.05,2.2,0.5", "{RepairRequestManHour.PersonHours}"))
    _append(
        group_footer,
        subtotal(
            "MHG_lbl",
            "MhSumLbl",
            "11.2,0.05,7.1,0.5",
            "جمع {RepairRequestManHour.PrsName} {RepairRequestManHour.PrsFamily}",
            HorAlignment="Right",
        ),
    )

    footer = _band_of(page2, "MH_Footer")
    footer["ClientRectangle"] = "0,3.3,20.3,3.85"
    ordered = []
    for comp in page2["Components"].values():
        ordered.append(comp)
        if comp.get("Name") == "MH_Data":
            ordered.append(group_footer)
    page2["Components"] = {str(i): comp for i, comp in enumerate(ordered)}


def build_id():
    raw = (CNG / "CngRepairsID.json").read_text(encoding="utf-8-sig")
    for src, dst in (
        ("CngRepairsTotalManHourPrice", "RepairRequestTotalManHourPrice"),
        ("CngRepairsManHourMaster", "RepairRequestManHourMaster"),
        ("CngRepairsManHour", "RepairRequestManHour"),
        ("CngRepairsPart", "RepairRequestPart"),
        ("CngRepairs", "RepairRequest"),
    ):
        raw = raw.replace(src, dst)
    raw = raw.replace("شناسنامه تعمیرات CNG", "شناسنامه درخواست تعمیر")
    raw = raw.replace("نمایندگی:", "منطقه:")
    data = json.loads(raw)
    data["ReportGuid"] = "d4a8e210-7c55-4b19-9e61-2f0c8b73a519"
    data["ReportName"] = "RepairRequestID"
    data["ReportAlias"] = "شناسنامه درخواست تعمیر"

    page = data["Pages"]["0"]
    title = page["Components"]["0"]
    title_rect = title["ClientRectangle"].split(",")
    title_rect[3] = f"{float(title_rect[3]) + 0.55:g}"
    title["ClientRectangle"] = ",".join(title_rect)
    title["CanGrow"] = True
    _shift_below(title["Components"], 3.75, 0.55)
    _set_text(title, "R2a", "", ClientRectangle="0.0,3.15,6.5,1.2")
    _set_text(title, "R2b", "", ClientRectangle="6.5,3.15,4.0,1.2")
    _set_text(title, "R2c", "", ClientRectangle="10.5,3.15,9.8,1.2")
    _set_text(title, "LCnt", "عملیات:", ClientRectangle="5.2,3.18,1.2,0.4")
    _set_text(
        title,
        "VCnt",
        "{RepairRequest.Operations}",
        CanGrow=False,
        ClientRectangle="0.1,3.18,5.05,0.7",
        VertAlignment="Top",
    )
    _set_text(title, "LSend", "تاریخ ارسال:", ClientRectangle="5.2,3.92,1.2,0.38")
    _set_text(title, "VSend", "{RepairRequest.ExitDate_Shamsi}", ClientRectangle="3.3,3.92,1.85,0.38")
    _set_text(title, "LVen", "دریافت از:")
    _set_text(title, "VVen", "{RepairRequest.RepresentationReception}", CanGrow=False)
    _set_text(title, "VNeed", "{RepairRequest.CustomerNeedDate_Shamsi}")
    _set_text(title, "LRecv", "نام تحویل گیرنده: {RepairRequest.DeliveringRecipient}")
    _set_text(page, "C_BlankDel", "{RepairRequest.DeliveryForm_Number}")
    _set_text(page, "PD_pname", "{RepairRequestPart.Part_Name}", HorAlignment="Right", CanGrow=True)
    _set_text(title, "R6", "", ClientRectangle="0.0,6.75,20.3,3.9")
    _set_text(title, "LFail", "علت خرابی:", ClientRectangle="18.15,6.85,2.0,0.45")
    _set_text(
        title,
        "VFail",
        "{RepairRequest.FailureReason}\n{RepairRequest.RepairDescription}",
        CanGrow=False,
        VertAlignment="Top",
        ClientRectangle="0.15,6.85,17.9,3.65",
    )
    title_rect = title["ClientRectangle"].split(",")
    title_rect[3] = "10.8"
    title["ClientRectangle"] = ",".join(title_rect)

    page2 = data["Pages"]["1"]
    _fix_main_page(page, title)
    _tall_name_row(title)
    _fix_manhour_page(page2)
    _attach_dictionary(data, [
        _data_source("RepairRequest", [
            "Id", "IDNumber", "DeliveryDate_Warehouse_Shamsi", "EntryDate_Shamsi", "Part_Name", "Part_Code",
            "Serial", "ExitDate_Shamsi", "Agency", "VendorName", "Customer_Title", "DestinationAddress",
            "Customer_Phone", "IsGuarantee", "ISWarranty", "RepairRequestStatus", "DestinationAfterRepair",
            "FailureReason", "CustomerNeedDate_Shamsi", "RepresentationReception", "Operations",
            "DeliveringRecipient", "RepairDescription", "LoanForm_Number", "DeliveryForm_Number",
            "PreFactor_Number", "TotalBuy", "TotalSale", "Total", "VendoringCost",
        ]),
        _data_source("RepairRequestPart", [
            "Id", "Part_Code", "Part_Name", "Part_Number", "PartUnit_Title", "DamagedPart",
            "Hamkaran_InvVchItm_FK", "BuyPrice", "RepairRequestId", "Tafazol", "SalePrice", "TotalPrice",
        ]),
        _data_source("RepairRequestManHour", [
            "Id", "ExpertId", "PrsName", "PrsFamily", "WorkDate", "StartTime", "EndTime", "ManHourPrice",
            "WorkingHour", "PriceManInHour", "Comment", "PersonHours", "PersonPrice", "RowNo", "GroupKey",
        ]),
        _data_source("RepairRequestManHourMaster", ["SumWorkingHour", "SumOfMissionPrice"]),
        _data_source("RepairRequestTotalManHourPrice", ["Id", "PriceManInHour"]),
    ])
    return data


LABEL_SQL = r"""SELECT
    r.Id,
    r.IdNumber AS IDNumber,
    r.EntryShamsiDate AS EntryDate_Shamsi,
    r.ExitShamsiDate AS ExitDate_Shamsi,
    r.WarehouseDeliveryShamsiDate AS DeliveryDate_Warehouse_Shamsi,
    COALESCE(NULLIF(LTRIM(RTRIM(supParty.FullName)), N''), sup.NameFa, N'') AS Zone_SupervisorTitle,
    COALESCE(NULLIF(LTRIM(RTRIM(custParty.FullName)), N''), custParty.CompanyName, N'') AS Customer_Title,
    COALESCE(NULLIF(LTRIM(RTRIM(r.CustomerAddressManual)), N''), ca.Address, N'') AS CustomerAddressManual,
    r.Serial,
    LTRIM(RTRIM(ISNULL(p.Code, N'') + CASE WHEN p.Code IS NOT NULL AND p.Name IS NOT NULL THEN N' ' ELSE N'' END + ISNULL(p.Name, N''))) AS Product,
    r.FailureReason,
    CASE WHEN r.IsGuarantee = 1 THEN N'گارانتی' ELSE N'وارانتی' END AS GuaranteeTitle,
    CASE WHEN r.AgreedShamsiDate IS NULL OR LTRIM(RTRIM(r.AgreedShamsiDate)) = N'' THEN N'تعریف نشده' ELSE r.AgreedShamsiDate END AS AgreedDateInText,
    CASE WHEN r.Agreed2ShamsiDate IS NULL OR LTRIM(RTRIM(r.Agreed2ShamsiDate)) = N'' THEN N'تعریف نشده' ELSE r.Agreed2ShamsiDate END AS AgreedDate2InText,
    CASE WHEN r.Agreed3ShamsiDate IS NULL OR LTRIM(RTRIM(r.Agreed3ShamsiDate)) = N'' THEN N'تعریف نشده' ELSE r.Agreed3ShamsiDate END AS AgreedDate3InText,
    CASE r.DestinationAfterRepair
        WHEN 293 THEN N'مشتری'
        WHEN 294 THEN N'نمایندگی'
        WHEN 295 THEN N'هوایار (داغی)'
        WHEN 296 THEN N'هوایار (برگشت امانی)'
        WHEN 2700 THEN N'هوایار'
        ELSE N''
    END AS DestinationAfterRepair,
    CASE r.Status
        WHEN 301 THEN N'1-در حال انجام'
        WHEN 302 THEN N'2-توقف'
        WHEN 304 THEN N'3-غیرقابل تعمیر'
        WHEN 305 THEN N'4-تکمیل شده'
        WHEN 1535 THEN N'5-ارسال به پیمانکار'
        WHEN 1536 THEN N'6-در انتظار تایید مشتری'
        WHEN 1537 THEN N'7-در انتظار خرید قطعه'
        WHEN 1774 THEN N'8-در حال تهیه PreCheck'
        WHEN 1871 THEN N'9-در انتظار تست'
        WHEN 2092 THEN N'10-عدم تایید مشتری'
        WHEN 2093 THEN N'11-تحویل به انبار'
        WHEN 2236 THEN N'12-آیتم صوری'
        WHEN 2413 THEN N'13-در انتظار تعمیر'
        WHEN 3169 THEN N'14-در انتظار بازدید مشتری'
        WHEN 3175 THEN N'15-توقف-تعیین تکلیف مشتری'
        WHEN 3176 THEN N'16-توقف-خرید قطعه'
        ELSE N''
    END AS RepairRequestStatus
FROM Rpr.RepairRequest AS r
LEFT JOIN Inv.Part AS p ON p.Id = r.ProductId
LEFT JOIN SLS.Customer AS cust ON cust.Id = r.CustomerId
LEFT JOIN Gnr.Party AS custParty ON custParty.Id = cust.PartyId
LEFT JOIN SLS.CustomerAddress AS ca ON ca.Id = r.CustomerAddressId
LEFT JOIN [system].[User] AS sup ON sup.Id = r.ZoneSupervisorId
LEFT JOIN Gnr.Party AS supParty ON supParty.Id = sup.PartyId
WHERE r.Id = TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @repairRequestId))), N''));"""

ID_SQL = r"""SELECT
    r.Id,
    r.IdNumber AS IDNumber,
    r.WarehouseDeliveryShamsiDate AS DeliveryDate_Warehouse_Shamsi,
    r.EntryShamsiDate AS EntryDate_Shamsi,
    p.Name AS Part_Name,
    p.Code AS Part_Code,
    r.Serial,
    r.ExitShamsiDate AS ExitDate_Shamsi,
    z.Title AS Agency,
    STUFF((
        SELECT N' ، ' + c.ContractorTitle
        FROM Rpr.RepairRequestContractor AS c
        WHERE c.RepairRequestId = r.Id AND NULLIF(LTRIM(RTRIM(c.ContractorTitle)), N'') IS NOT NULL
        FOR XML PATH(''), TYPE
    ).value('.', 'nvarchar(max)'), 1, 3, N'') AS VendorName,
    COALESCE(NULLIF(LTRIM(RTRIM(custParty.FullName)), N''), custParty.CompanyName, N'') AS Customer_Title,
    COALESCE(NULLIF(LTRIM(RTRIM(r.DestinationAddress)), N''), NULLIF(LTRIM(RTRIM(r.CustomerAddressManual)), N''), ca.Address, N'') AS DestinationAddress,
    r.CustomerPhone AS Customer_Phone,
    r.IsGuarantee,
    CAST(CASE WHEN r.IsGuarantee = 0 THEN 1 ELSE 0 END AS bit) AS ISWarranty,
    CASE r.Status
        WHEN 301 THEN N'1-در حال انجام'
        WHEN 302 THEN N'2-توقف'
        WHEN 304 THEN N'3-غیرقابل تعمیر'
        WHEN 305 THEN N'4-تکمیل شده'
        WHEN 1535 THEN N'5-ارسال به پیمانکار'
        WHEN 1536 THEN N'6-در انتظار تایید مشتری'
        WHEN 1537 THEN N'7-در انتظار خرید قطعه'
        WHEN 1774 THEN N'8-در حال تهیه PreCheck'
        WHEN 1871 THEN N'9-در انتظار تست'
        WHEN 2092 THEN N'10-عدم تایید مشتری'
        WHEN 2093 THEN N'11-تحویل به انبار'
        WHEN 2236 THEN N'12-آیتم صوری'
        WHEN 2413 THEN N'13-در انتظار تعمیر'
        WHEN 3169 THEN N'14-در انتظار بازدید مشتری'
        WHEN 3175 THEN N'15-توقف-تعیین تکلیف مشتری'
        WHEN 3176 THEN N'16-توقف-خرید قطعه'
        ELSE N''
    END AS RepairRequestStatus,
    CASE r.DestinationAfterRepair
        WHEN 293 THEN N'مشتری'
        WHEN 294 THEN N'نمایندگی'
        WHEN 295 THEN N'هوایار (داغی)'
        WHEN 296 THEN N'هوایار (برگشت امانی)'
        WHEN 2700 THEN N'هوایار'
        ELSE N''
    END AS DestinationAfterRepair,
    r.FailureReason,
    r.CustomerNeedShamsiDate AS CustomerNeedDate_Shamsi,
    r.RepresentationReception,
    r.Comment AS Operations,
    r.DeliveringRecipient,
    LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(ISNULL(r.RepairDescription, N''), N'*', N' '), N'  ', N' '), N'  ', N' '))) AS RepairDescription,
    r.LoanFormNumber AS LoanForm_Number,
    r.DeliveryFormNumber AS DeliveryForm_Number,
    r.PreFactorNumber AS PreFactor_Number,
    ISNULL(pt.TotalBuy, 0) AS TotalBuy,
    ISNULL(pt.TotalSale, 0) AS TotalSale,
    ISNULL(pt.Total, 0) AS Total,
    ISNULL((
        SELECT SUM(c.Cost)
        FROM Rpr.RepairRequestContractor AS c
        WHERE c.RepairRequestId = r.Id
    ), 0) AS VendoringCost
FROM Rpr.RepairRequest AS r
LEFT JOIN Inv.Part AS p ON p.Id = r.ProductId
LEFT JOIN SLS.Customer AS cust ON cust.Id = r.CustomerId
LEFT JOIN Gnr.Party AS custParty ON custParty.Id = cust.PartyId
LEFT JOIN SLS.CustomerAddress AS ca ON ca.Id = r.CustomerAddressId
LEFT JOIN Crm.Zone AS z ON z.Id = r.ZoneId
OUTER APPLY (
    SELECT SUM(g.BuyPrice) AS TotalBuy, SUM(g.SalePrice) AS TotalSale, SUM(g.TotalPrice) AS Total
    FROM (
        SELECT
            MAX(x.BuyPrice) AS BuyPrice,
            x.SalePrice AS SalePrice,
            CAST(ISNULL(x.SalePrice, 0) * (SUM(ISNULL(x.UsedMount, 0)) - SUM(ISNULL(x.ReturnMount, 0))) AS decimal(18, 2)) AS TotalPrice
        FROM Rpr.RepairRequestPart AS x
        INNER JOIN Inv.Part AS xp ON xp.Id = x.PartId
        LEFT JOIN Inv.PartUnit AS xpu ON xpu.Id = COALESCE(x.PartUnitId, xp.UnitId)
        WHERE x.RepairRequestId = r.Id
        GROUP BY xp.Code, xp.Name, xp.Number, ISNULL(NULLIF(LTRIM(RTRIM(x.UnitName)), N''), xpu.Title), x.RepairRequestId, x.DamagedPart, x.SalePrice
        HAVING (SUM(ISNULL(x.UsedMount, 0)) - SUM(ISNULL(x.ReturnMount, 0))) <> 0
    ) AS g
) AS pt
WHERE r.Id = TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @repairRequestId))), N''));

SELECT
    MAX(rp.Id) AS Id,
    p.Code AS Part_Code,
    p.Name AS Part_Name,
    p.Number AS Part_Number,
    ISNULL(NULLIF(LTRIM(RTRIM(rp.UnitName)), N''), pu.Title) AS PartUnit_Title,
    CAST(MAX(CASE WHEN rp.DamagedPart = 1 THEN 1 ELSE 0 END) AS bit) AS DamagedPart,
    MAX(NULLIF(rp.HtsHamkaranInvVchItemId, 0)) AS Hamkaran_InvVchItm_FK,
    MAX(rp.BuyPrice) AS BuyPrice,
    rp.RepairRequestId,
    CAST(SUM(ISNULL(rp.UsedMount, 0)) - SUM(ISNULL(rp.ReturnMount, 0)) AS decimal(18, 3)) AS Tafazol,
    rp.SalePrice AS SalePrice,
    CAST(ISNULL(rp.SalePrice, 0) * (SUM(ISNULL(rp.UsedMount, 0)) - SUM(ISNULL(rp.ReturnMount, 0))) AS decimal(18, 2)) AS TotalPrice
FROM Rpr.RepairRequestPart AS rp
INNER JOIN Inv.Part AS p ON p.Id = rp.PartId
LEFT JOIN Inv.PartUnit AS pu ON pu.Id = COALESCE(rp.PartUnitId, p.UnitId)
WHERE rp.RepairRequestId = TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @repairRequestId))), N''))
GROUP BY p.Code, p.Name, p.Number, ISNULL(NULLIF(LTRIM(RTRIM(rp.UnitName)), N''), pu.Title), rp.RepairRequestId, rp.DamagedPart, rp.SalePrice
HAVING (SUM(ISNULL(rp.UsedMount, 0)) - SUM(ISNULL(rp.ReturnMount, 0))) <> 0;

SELECT
    d.Id,
    d.ExpertId,
    d.PrsName,
    d.PrsFamily,
    d.WorkDate,
    d.StartTime,
    d.EndTime,
    d.ManHourPrice,
    d.WorkingHour,
    d.PriceManInHour,
    d.Comment,
    CAST(d.PersonMinutes / 60 AS nvarchar(10)) + N':' + RIGHT(N'0' + CAST(d.PersonMinutes % 60 AS nvarchar(10)), 2) AS PersonHours,
    d.PersonPrice,
    ROW_NUMBER() OVER (ORDER BY d.PrsFamily, d.PrsName, d.ExpertId, d.Id) AS RowNo,
    RIGHT(N'0000000' + CAST(DENSE_RANK() OVER (ORDER BY d.PrsFamily, d.PrsName, d.ExpertId) AS nvarchar(7)), 7) AS GroupKey
FROM (
    SELECT
        mh.Id,
        mh.ExpertId,
        ISNULL(prs.Name, N'') AS PrsName,
        ISNULL(prs.Family, N'') AS PrsFamily,
        mh.WorkShamsiDate AS WorkDate,
        mh.StartTime,
        mh.EndTime,
        CAST(ISNULL(sal.DailySalary, 0) / 8 AS bigint) AS ManHourPrice,
        CASE
            WHEN m.Minutes IS NULL THEN N''
            ELSE RIGHT(N'0' + CAST(m.Minutes / 60 AS nvarchar(10)), 2) + N':' + RIGHT(N'0' + CAST(m.Minutes % 60 AS nvarchar(10)), 2)
        END AS WorkingHour,
        CAST((ISNULL(sal.DailySalary, 0) / 8) * (ISNULL(m.Minutes, 0) / 60) AS bigint) AS PriceManInHour,
        mh.Comment,
        SUM(ISNULL(m.Minutes, 0)) OVER (PARTITION BY mh.ExpertId) AS PersonMinutes,
        SUM(CAST((ISNULL(sal.DailySalary, 0) / 8) * (ISNULL(m.Minutes, 0) / 60) AS bigint)) OVER (PARTITION BY mh.ExpertId) AS PersonPrice
    FROM Rpr.RepairRequestManHour AS mh
    LEFT JOIN Hcm.Personel AS prs ON prs.Id = mh.ExpertId
    OUTER APPLY (
        SELECT TOP 1 ms.DailySalary
        FROM Sale.MissionSalary AS ms
        WHERE ms.PersonelId = mh.ExpertId
        ORDER BY ms.Id DESC
    ) AS sal
    OUTER APPLY (
        SELECT CASE
            WHEN TRY_CONVERT(time, mh.StartTime) IS NULL OR TRY_CONVERT(time, mh.EndTime) IS NULL THEN NULL
            ELSE DATEDIFF(MINUTE, TRY_CONVERT(time, mh.StartTime), TRY_CONVERT(time, mh.EndTime))
        END AS Minutes
    ) AS m
    WHERE mh.RepairRequestId = TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @repairRequestId))), N''))
) AS d
ORDER BY d.PrsFamily, d.PrsName, d.ExpertId, d.Id;

SELECT
    ISNULL(
        CAST(SUM(DATEDIFF(MINUTE, TRY_CONVERT(time, mh.StartTime), TRY_CONVERT(time, mh.EndTime))) / 60 AS nvarchar(10))
        + N':' +
        RIGHT(N'0' + CAST(SUM(DATEDIFF(MINUTE, TRY_CONVERT(time, mh.StartTime), TRY_CONVERT(time, mh.EndTime))) % 60 AS nvarchar(10)), 2),
        N'0:00') AS SumWorkingHour,
    CAST(ISNULL(SUM(
        (ISNULL(sal.DailySalary, 0) / 8) * (
            CASE
                WHEN TRY_CONVERT(time, mh.StartTime) IS NULL OR TRY_CONVERT(time, mh.EndTime) IS NULL THEN 0
                ELSE DATEDIFF(MINUTE, TRY_CONVERT(time, mh.StartTime), TRY_CONVERT(time, mh.EndTime)) / 60
            END
        )
    ), 0) AS bigint) AS SumOfMissionPrice
FROM Rpr.RepairRequestManHour AS mh
OUTER APPLY (
    SELECT TOP 1 ms.DailySalary
    FROM Sale.MissionSalary AS ms
    WHERE ms.PersonelId = mh.ExpertId
    ORDER BY ms.Id DESC
) AS sal
WHERE mh.RepairRequestId = TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @repairRequestId))), N''));

SELECT
    r.Id,
    CAST(ISNULL(SUM(
        (ISNULL(sal.DailySalary, 0) / 8) * (
            CASE
                WHEN TRY_CONVERT(time, mh.StartTime) IS NULL OR TRY_CONVERT(time, mh.EndTime) IS NULL THEN 0
                ELSE DATEDIFF(MINUTE, TRY_CONVERT(time, mh.StartTime), TRY_CONVERT(time, mh.EndTime)) / 60
            END
        )
    ), 0) AS bigint) AS PriceManInHour
FROM Rpr.RepairRequest AS r
LEFT JOIN Rpr.RepairRequestManHour AS mh ON mh.RepairRequestId = r.Id
OUTER APPLY (
    SELECT TOP 1 ms.DailySalary
    FROM Sale.MissionSalary AS ms
    WHERE ms.PersonelId = mh.ExpertId
    ORDER BY ms.Id DESC
) AS sal
WHERE r.Id = TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @repairRequestId))), N''))
GROUP BY r.Id;"""


def col(name, title, select_index, system_type, visible=True, primary=False):
    return {
        "TableName": None,
        "ColumnName": name,
        "DisplayName": title,
        "Alliance": name,
        "Address": None,
        "SystemTypeName": None,
        "SelectIndex": select_index,
        "Visible": visible,
        "PrimaryKey": primary,
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


def columns_for(specs):
    rows = []
    for select_index, items in enumerate(specs):
        for name, title, system_type, visible, primary in items:
            rows.append(col(name, title, select_index, system_type, visible, primary))
    return json.dumps(rows, ensure_ascii=False, separators=(",", ":"))


LABEL_COLS = columns_for([[
    ("Id", "شناسه", 6, False, True),
    ("IDNumber", "شماره شناسنامه", 0, True, False),
    ("EntryDate_Shamsi", "تاریخ ورود", 0, True, False),
    ("ExitDate_Shamsi", "تاریخ خروج", 0, True, False),
    ("DeliveryDate_Warehouse_Shamsi", "تحویل انبار", 0, True, False),
    ("Zone_SupervisorTitle", "مسئول منطقه", 0, True, False),
    ("Customer_Title", "مشتری", 0, True, False),
    ("CustomerAddressManual", "سایت مشتری", 0, True, False),
    ("Serial", "سریال", 0, True, False),
    ("Product", "تجهیز", 0, True, False),
    ("FailureReason", "علت خرابی", 0, True, False),
    ("GuaranteeTitle", "گارانتی / وارانتی", 0, True, False),
    ("AgreedDateInText", "تاریخ توافقی", 0, True, False),
    ("AgreedDate2InText", "تاریخ توافقی ۲", 0, True, False),
    ("AgreedDate3InText", "تاریخ توافقی ۳", 0, True, False),
    ("DestinationAfterRepair", "مقصد پس از تعمیر", 0, True, False),
    ("RepairRequestStatus", "وضعیت", 0, True, False),
]])

ID_COLS = columns_for([
    [
        ("Id", "شناسه", 6, False, True),
        ("IDNumber", "شماره شناسنامه", 0, True, False),
        ("DeliveryDate_Warehouse_Shamsi", "تحویل انبار", 0, True, False),
        ("EntryDate_Shamsi", "تاریخ ورود", 0, True, False),
        ("Part_Name", "نام کالا", 0, True, False),
        ("Part_Code", "کد کالا", 0, True, False),
        ("Serial", "سریال", 0, True, False),
        ("ExitDate_Shamsi", "تاریخ ارسال", 0, True, False),
        ("Agency", "منطقه", 0, True, False),
        ("VendorName", "پیمانکار", 0, True, False),
        ("Customer_Title", "مشتری", 0, True, False),
        ("DestinationAddress", "آدرس مقصد", 0, True, False),
        ("Customer_Phone", "تلفن", 0, True, False),
        ("IsGuarantee", "گارانتی", 1, True, False),
        ("ISWarranty", "وارانتی", 1, True, False),
        ("RepairRequestStatus", "وضعیت", 0, True, False),
        ("DestinationAfterRepair", "مقصد", 0, True, False),
        ("FailureReason", "علت خرابی", 0, True, False),
        ("CustomerNeedDate_Shamsi", "تاریخ نیاز مشتری", 0, True, False),
        ("RepresentationReception", "دریافت از", 0, True, False),
        ("Operations", "عملیات", 0, True, False),
        ("DeliveringRecipient", "تحویل گیرنده", 0, True, False),
        ("RepairDescription", "شرح قطعه", 0, True, False),
        ("LoanForm_Number", "شماره فرم امانی", 6, True, False),
        ("DeliveryForm_Number", "شماره فرم تحویل", 0, True, False),
        ("PreFactor_Number", "شماره پیش فاکتور", 6, True, False),
        ("TotalBuy", "جمع خرید", 6, True, False),
        ("TotalSale", "جمع فروش", 6, True, False),
        ("Total", "جمع", 7, True, False),
        ("VendoringCost", "هزینه پیمانکار", 6, True, False),
    ],
    [
        ("Id", "شناسه قطعه", 6, False, True),
        ("Part_Code", "کد کالا", 0, True, False),
        ("Part_Name", "نام کالا", 0, True, False),
        ("Part_Number", "شماره کالا", 0, False, False),
        ("PartUnit_Title", "واحد", 0, True, False),
        ("DamagedPart", "داغی", 1, True, False),
        ("Hamkaran_InvVchItm_FK", "شماره درخواست", 6, True, False),
        ("BuyPrice", "فی خرید", 6, True, False),
        ("RepairRequestId", "شناسه درخواست", 6, False, False),
        ("Tafazol", "تعداد", 7, True, False),
        ("SalePrice", "فی فروش", 6, True, False),
        ("TotalPrice", "مبلغ", 7, True, False),
    ],
    [
        ("Id", "شناسه", 6, False, True),
        ("ExpertId", "شناسه تعمیرکار", 6, False, False),
        ("PrsName", "نام", 0, True, False),
        ("PrsFamily", "نام خانوادگی", 0, True, False),
        ("WorkDate", "تاریخ کار", 0, True, False),
        ("StartTime", "شروع", 0, True, False),
        ("EndTime", "پایان", 0, True, False),
        ("ManHourPrice", "نرخ", 6, True, False),
        ("WorkingHour", "کارکرد", 0, True, False),
        ("PriceManInHour", "هزینه", 6, True, False),
        ("Comment", "توضیحات", 0, True, False),
        ("PersonHours", "جمع کارکرد تعمیرکار", 0, True, False),
        ("PersonPrice", "جمع هزینه تعمیرکار", 6, True, False),
        ("RowNo", "ردیف", 6, False, False),
        ("GroupKey", "کلید گروه", 0, False, False),
    ],
    [
        ("SumWorkingHour", "جمع نفرساعت", 0, True, False),
        ("SumOfMissionPrice", "جمع هزینه نفرساعت", 6, True, False),
    ],
    [
        ("Id", "شناسه", 6, False, True),
        ("PriceManInHour", "هزینه نفرساعت", 6, True, False),
    ],
])


def query_json(custom, parameters_name, selects):
    payload = {
        "Tables": [],
        "Relations": [],
        "Filters": [],
        "CustomConditions": None,
        "CustomQuery": custom,
        "Parameters": [{
            "Id": parameters_name,
            "Name": parameters_name,
            "DisplayName": "شناسه درخواست تعمیر",
            "DefaultValue": "",
            "DataType": "bigint",
        }],
        "Selects": selects,
    }
    return json.dumps(payload, ensure_ascii=False, separators=(",", ":"))


def upsert_block(name, title, custom, columns, selects):
    qj = query_json(custom, "repairRequestId", selects).replace("'", "''")
    cols = columns.replace("'", "''")
    base = custom.replace("'", "''")
    return f"""
    DECLARE @Name_{name} NVARCHAR(200) = N'{name}';
    DECLARE @Title_{name} NVARCHAR(200) = N'{title}';
    DECLARE @Base_{name} NVARCHAR(MAX) = N'{base}';
    DECLARE @QueryJson_{name} NVARCHAR(MAX) = N'{qj}';
    DECLARE @ColumnsJson_{name} NVARCHAR(MAX) = N'{cols}';
    DECLARE @SavedQueryId_{name} BIGINT;

    IF EXISTS (SELECT 1 FROM [system].[SavedQuery] WHERE Name = @Name_{name})
    BEGIN
        UPDATE [system].[SavedQuery]
        SET Title = @Title_{name},
            QueryJson = @QueryJson_{name},
            ColumnsJson = @ColumnsJson_{name},
            EntityFullName = N'entities.app.rpr.repairrequest',
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
        WHERE Name = @Name_{name};
        SELECT @SavedQueryId_{name} = Id FROM [system].[SavedQuery] WHERE Name = @Name_{name};
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
            @Name_{name}, @Title_{name}, @QueryJson_{name}, @ColumnsJson_{name}, N'[]',
            N'[]', N'{{"onSelectedRow":"","onRowAdded":""}}',
            1, 0, N'entities.app.rpr.repairrequest', N'',
            1, 1, @SeedUser, @SeedUser,
            @Now, @NowShamsi, @Now, @NowShamsi,
            1
        );
        SET @SavedQueryId_{name} = SCOPE_IDENTITY();
    END;

    IF EXISTS (SELECT 1 FROM dbo.ReportBuilderReport WHERE Name = @Name_{name})
    BEGIN
        UPDATE dbo.ReportBuilderReport
        SET Title = @Title_{name},
            BaseQuery = @Base_{name},
            SavedQueryId = @SavedQueryId_{name},
            Type = 1,
            ObjectType = N'Table',
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi,
            IsActive = 1
        WHERE Name = @Name_{name};
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
            @Name_{name}, @Title_{name}, N'Table', @SavedQueryId_{name}, 1, NULL,
            @Base_{name},
            1, 1, @SeedUser, @SeedUser,
            @Now, @NowShamsi, @Now, @NowShamsi, 1
        );
    END;
"""


def build_sql():
    label_selects = [{"Index": 0, "Name": "Report", "Title": "Report"}]
    id_selects = [
        {"Index": 0, "Name": "RepairRequest", "Title": "RepairRequest"},
        {"Index": 1, "Name": "RepairRequestPart", "Title": "RepairRequestPart"},
        {"Index": 2, "Name": "RepairRequestManHour", "Title": "RepairRequestManHour"},
        {"Index": 3, "Name": "RepairRequestManHourMaster", "Title": "RepairRequestManHourMaster"},
        {"Index": 4, "Name": "RepairRequestTotalManHourPrice", "Title": "RepairRequestTotalManHourPrice"},
    ]
    body = upsert_block("RepairRequestLabel", "برچسب تعمیر", LABEL_SQL, LABEL_COLS, label_selects)
    body += upsert_block("RepairRequestID", "شناسنامه درخواست تعمیر", ID_SQL, ID_COLS, id_selects)
    return f"""/*
چاپ برچسب و شناسنامه درخواست تعمیر.
ViewReportByName فقط وقتی گزارش در dbo.ReportBuilderReport باشد صفحه را باز می‌کند؛
نبودن این دو نام همان خطای 404 است.
قالب Stimulsoft در Data/Seed/RprReports است و با Update_RepairRequest_Report_Content.ps1 روی Content می‌نشیند.
اعمال: sqlcmd -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-repair-request-reports';
{body}
SELECT
    (SELECT Id FROM [system].[SavedQuery] WHERE Name = N'RepairRequestLabel') AS LabelSavedQueryId,
    (SELECT Id FROM [system].[SavedQuery] WHERE Name = N'RepairRequestID') AS IdSavedQueryId,
    (SELECT Title FROM dbo.ReportBuilderReport WHERE Name = N'RepairRequestLabel') AS LabelTitle,
    (SELECT Title FROM dbo.ReportBuilderReport WHERE Name = N'RepairRequestID') AS IdTitle;
"""


def main():
    label = build_label()
    ident = build_id()
    (HERE / "RepairRequestLabel.json").write_text(json.dumps(label, ensure_ascii=False, indent=2), encoding="utf-8")
    (HERE / "RepairRequestID.json").write_text(json.dumps(ident, ensure_ascii=False, indent=2), encoding="utf-8")
    SQL_OUT.write_text(build_sql(), encoding="utf-8-sig")
    print("wrote", HERE / "RepairRequestLabel.json")
    print("wrote", HERE / "RepairRequestID.json")
    print("wrote", SQL_OUT)


if __name__ == "__main__":
    main()
