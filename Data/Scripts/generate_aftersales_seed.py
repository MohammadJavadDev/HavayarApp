# -*- coding: utf-8 -*-
"""Generate Seed_AfterSalesServiceSystem_DataProfilesAndAccess.sql from entities + controllers + DB columns JSON."""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ENTITY_ROOT = ROOT / "Entities"
CTRL_ROOT = ROOT / "WebApp" / "Controllers" / "Dynamic"
VIEW_ROOT = ROOT / "WebApp" / "Views" / "Panel"
OUT_SQL = Path(__file__).with_name("Seed_AfterSalesServiceSystem_DataProfilesAndAccess.sql")
COLS_JSON = Path(__file__).with_name("_havayar_columns.json")

SYSTEM_TYPE = {
    "String": 0, "Boolean": 1, "DateTime": 2, "Date": 3, "DateTimeShamsi": 4, "DateShamsi": 5,
    "Long": 6, "Int": 7, "Select": 8, "File": 9, "Entity": 10, "ListEntity": 11,
    "ListString": 12, "ListLong": 13, "Decimal": 14, "AutoNumber": 15,
}
ACCESS_TYPE = {"EntityAction": 0, "View": 1, "Api": 2, "DataProfile": 3, "ImportData": 4, "ReportItem": 5, "Other": 1000}
ACCESS_ITEM = {
    "Show": 0, "List": 1, "FetchData": 2, "Save": 3, "Create": 4, "Update": 5, "Delete": 6,
    "DataProfile": 7, "DataProfileCreate": 8, "DataProfileEdit": 9, "DataProfileDelete": 10,
    "ImportData": 11, "ReportItem": 12, "Dashbord": 13, "Custom": 1000,
}

JOIN_BY_TYPE = {
    "Customer": {"schema": "SLS", "table": "Customer", "via_party": True},
    "User": {"schema": "system", "table": "User", "col": "Name"},
    "Zone": {"schema": "Crm", "table": "Zone", "col": "Title"},
    "Part": {"schema": "Inv", "table": "Part", "cols": [("Code", "کد"), ("Name", "نام")]},
    "CustomerAddress": {"schema": "SLS", "table": "CustomerAddress", "col": "Title"},
    "Region": {"schema": "Gnr", "table": "Region", "col": "Name"},
    "OrgUnit": {"schema": "Hrm", "table": "OrgUnit", "col": "Title"},
    "Order": {"schema": "Sale", "table": "Order", "col": "OrderNumber"},
    "OrderDetail": {"schema": "Sale", "table": "OrderDetail", "nested_order": True},
    "OrderDetailSerial": {"schema": "Sale", "table": "OrderDetailSerial", "col": "Serial"},
    "ServiceRequest": {"schema": "Sale", "table": "ServiceRequest", "col": "ContactShamsiDate"},
    "RepairRequest": {"schema": "Rpr", "table": "RepairRequest", "col": "IdNumber"},
    "Branch": {"schema": "Sale", "table": "Branch", "col": "Title"},
    "Party": {"schema": "Gnr", "table": "Party", "col": "FullName"},
    "Project": {"schema": "Edms", "table": "Project", "col": "Title"},
    "PlaningProject": {"schema": "Pln", "table": "Project", "col": "Name"},
    "DL": {"schema": "FIN", "table": "DL", "col": "Title"},
    "CostCenter": {"schema": "Gnr", "table": "CostCenter", "col": "Title"},
    "Mission": {"schema": "Sale", "table": "Mission", "col": "HokmNumber"},
    "WorkReport": {"schema": "Sale", "table": "WorkReport", "col": "Id"},
    "PriceConfig": {"schema": "Sale", "table": "PriceConfig", "col": "Title"},
    "TenderManagement": {"schema": "Sale", "table": "TenderManagement", "col": "ProjectName"},
    "TenderProductGroup": {"schema": "Sale", "table": "TenderProductGroup", "col": "Title"},
    "WebShopOrder": {"schema": "Sale", "table": "WebShopOrder", "col": "OrderCode"},
}

ROUTE_BY_ENTITY = {
    "Zone": "/Panel/Crm/Zone",
    "CustomerAddress": "/Panel/SLS/CustomerAddress",
    "CustomerSatisfactionSurvey": "/Panel/Crm/CustomerSatisfactionSurvey",
    "RepairRequest": "/Panel/Rpr/RepairRequest",
    "EstimatedCost": "/Panel/Rpr/EstimatedCost",
    "ContractorOrder": "/Panel/Rpr/ContractorOrder",
    "RepairRequestPart": "/Panel/Rpr/RepairRequestPart",
    "RepairRequestComment": "/Panel/Rpr/RepairRequestComment",
    "RepairRequestManHour": "/Panel/Rpr/RepairRequestManHour",
    "RepairRequestAttachment": "/Panel/Rpr/RepairRequestAttachment",
    "RepairRequestWorkExplanation": "/Panel/Rpr/RepairRequestWorkExplanation",
    "RepairRequestWbs": "/Panel/Rpr/RepairRequestWbs",
    "RepairRequestPartFraction": "/Panel/Rpr/RepairRequestPartFraction",
    "TenderPart": "/Panel/Sale/TenderPart",
    "TenderTask": "/Panel/Sale/TenderTask",
    "TenderComment": "/Panel/Sale/TenderComment",
    "TenderAttachment": "/Panel/Sale/TenderAttachment",
    "TenderProductGroup": "/Panel/Sale/TenderProductGroup",
    "OrderPointComment": "/Panel/Sale/OrderPointComment",
    "ResponsibleZoneCustomer": "/Panel/Sale/ResponsibleZoneCustomer",
    "TechnicalQueryComment": "/Panel/Sale/TechnicalQueryComment",
    "ProjectUtilizedMaterialChange": "/Panel/Sale/ProjectUtilizedMaterialChange",
}

MONEY_RENDER = (
    "function(data, type, row) { if (data == null || data === '') return ''; "
    "if (typeof MJUtil !== 'undefined' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }"
)

CRUD_ACTIONS = (
    '[{"dataActionName":"new","enable":true,"title":"جدید"},'
    '{"dataActionName":"edit","enable":true,"title":"ویرایش"},'
    '{"dataActionName":"delete","enable":true,"title":"حذف"},'
    '{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]'
)
READONLY_ACTIONS = (
    '[{"dataActionName":"new","enable":false,"title":"جدید"},'
    '{"dataActionName":"edit","enable":true,"title":"ویرایش"},'
    '{"dataActionName":"delete","enable":false,"title":"حذف"},'
    '{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]'
)

SKIP_CTRL = {
    "ProductionOrderController", "ProductionOrderItemController", "OrderDetailController",
    "RequirmentAdvertiseController", "BranchController",
}


def sql_lit(s: str | None) -> str:
    if s is None:
        return "NULL"
    return "N'" + s.replace("'", "''") + "'"


def json_esc(s: str) -> str:
    return s.replace("\\", "\\\\").replace('"', '\\"').replace("\r", "").replace("\n", "\\n").replace("\t", "\\t")


def brace_block(text: str, open_pos: int) -> str | None:
    depth = 0
    for i in range(open_pos, len(text)):
        ch = text[i]
        if ch == "{":
            depth += 1
        elif ch == "}":
            depth -= 1
            if depth == 0:
                return text[open_pos + 1 : i]
    return None


def load_enums() -> dict[str, str]:
    enums = {"IsActiveEnum": "Entities.Base.IsActiveEnum"}
    for path in (ENTITY_ROOT / "App").rglob("*.cs"):
        text = path.read_text(encoding="utf-8")
        ns_m = re.search(r"namespace\s+([\w.]+)", text)
        ns = ns_m.group(1) if ns_m else ""
        for m in re.finditer(r"public\s+enum\s+(\w+)", text):
            enums[m.group(1)] = f"{ns}.{m.group(1)}"
    return enums


def parse_entity_file(path: Path) -> list[dict]:
    text = path.read_text(encoding="utf-8")
    ns_m = re.search(r"namespace\s+([\w.]+)", text)
    ns = ns_m.group(1) if ns_m else ""
    results = []
    rx = re.compile(
        r'\[Display\(Name\s*=\s*"(?P<disp>[^"]+)"\)\]\s*'
        r'\[Table\("(?P<tbl>[^"]+)"\s*,\s*Schema\s*=\s*"(?P<sch>[^"]+)"\)\]\s*'
        r"public class (?P<cls>\w+)\s*:\s*BaseEntity",
        re.S,
    )
    for m in rx.finditer(text):
        cls_pos = text.find("{", m.end())
        body = brace_block(text, cls_pos)
        if not body:
            continue
        props = []
        buf: list[str] = []
        for line in body.splitlines():
            trim = line.strip()
            if trim.startswith("public class "):
                break
            buf.append(trim)
            pm = re.match(
                r"public(?:\s+virtual)?\s+(?P<ty>[\w.?<>,\s]+)\s+(?P<nm>\w+)\s*\{",
                trim,
            )
            if pm:
                ty = pm.group("ty").strip()
                nm = pm.group("nm")
                block = " ".join(buf)
                if ty.startswith("ICollection"):
                    buf.clear()
                    continue
                dm = re.search(r'DisplayName\("([^"]+)"\)', block)
                display = dm.group(1) if dm else nm
                visible = True
                st_name = None
                di = re.search(r"DisplayInfo\([^)]*", block)
                if di:
                    dis = di.group(0)
                    if re.search(r"DisplayInfo\(\s*[^,]+,\s*false\b", dis):
                        visible = False
                    stm = re.search(r"SystemType\.(\w+)", dis)
                    if stm:
                        st_name = stm.group(1)
                fk_m = re.search(r"ForeignKey\(nameof\((\w+)\)\)", block)
                col_m = re.search(r'Column\("([^"]+)"\)', block)
                is_nav = ("virtual" in trim) or st_name == "Entity"
                props.append({
                    "name": nm, "type": ty, "display": display, "visible": visible,
                    "st": st_name, "fk": fk_m.group(1) if fk_m else None,
                    "column": col_m.group(1) if col_m else nm, "is_nav": is_nav,
                })
                buf.clear()
            elif trim.startswith("public ") and "class " not in trim and "{" not in trim:
                pass
            elif trim.startswith("public ") and "class " not in trim:
                buf.clear()
        results.append({
            "class": m.group("cls"), "display": m.group("disp"),
            "table": m.group("tbl"), "schema": m.group("sch"),
            "ns": ns, "full": f"{ns}.{m.group('cls')}", "props": props,
        })
    return results


def load_entities() -> dict[str, dict]:
    all_e: list[dict] = []
    for folder in [ENTITY_ROOT / "App" / "Sale", ENTITY_ROOT / "App" / "Rpr", ENTITY_ROOT / "App" / "Crm"]:
        for p in folder.rglob("*.cs"):
            if "Enum" in p.name:
                continue
            all_e.extend(parse_entity_file(p))
    ca = ENTITY_ROOT / "App" / "SLS" / "CustomerAddress.cs"
    if ca.exists():
        all_e.extend(parse_entity_file(ca))
    return {e["class"]: e for e in all_e}


def discover_list_entities() -> set[str]:
    names: set[str] = set()
    for p in VIEW_ROOT.rglob("*.cshtml"):
        d = str(p.parent)
        if not any(x in d for x in ("\\Sale\\", "\\Rpr\\", "\\Crm\\", "\\SLS\\CustomerAddress")):
            continue
        text = p.read_text(encoding="utf-8")
        for m in re.finditer(r'datatableprofile entity-Type="typeof\((\w+)\)"', text):
            names.add(m.group(1))
    names -= {
        "OrderDetail", "ProductionOrderItem", "ProductionOrder",
        "RequirmentAdvertise", "RequirementAdvertise",
    }
    return names


class DbCols:
    def __init__(self, data: dict):
        self.tables = {k.lower(): set(v) for k, v in data.get("tables", {}).items()}

    def has_table(self, schema: str, table: str) -> bool:
        return f"{schema}.{table}".lower() in self.tables

    def has_col(self, schema: str, table: str, col: str) -> bool:
        return col in self.tables.get(f"{schema}.{table}".lower(), set())


def infer_st(prop: dict, enums: dict[str, str]) -> tuple[str, int]:
    st = prop.get("st")
    if st and st in SYSTEM_TYPE:
        return st, SYSTEM_TYPE[st]
    n, t = prop["name"], prop["type"]
    if "ShamsiDateTime" in n:
        return "DateTimeShamsi", 4
    if "ShamsiDate" in n:
        return "DateShamsi", 5
    if t.startswith("bool"):
        return "Boolean", 1
    if t.startswith("DateTime"):
        return ("Date", 3) if n.endswith("Date") or "MiladiDate" in n else ("DateTime", 2)
    if t.startswith("decimal"):
        return "Decimal", 14
    if t.startswith("long"):
        return "Long", 6
    if t.startswith(("int", "byte", "short")):
        return "Int", 7
    tn = t.rstrip("?")
    if "Enum" in t or tn in enums:
        return "Select", 8
    return "String", 0


def is_money(name: str, display: str) -> bool:
    if re.search(r"Price|Amount|Cost|Salary|Fee|Discount|Vat|Tax|Total", name):
        return True
    return any(x in display for x in ("مبلغ", "قیمت", "هزینه", "حقوق"))


def _col(
    table: str, column: str, display: str, alias: str, addr: str,
    st_name: str, st_num: int, visible: bool, width: int, *,
    pk: bool = False, render: str | None = None, enum: str | None = None,
) -> dict:
    return {
        "table": table, "column": column, "display": display, "alias": alias,
        "addr": addr, "st_name": st_name, "st_num": st_num, "visible": visible,
        "pk": pk, "width": width, "render": render, "enum": enum,
    }


def apply_tender_hts_layout(select: list[str], frm: list[str], cols: list[dict], db: DbCols) -> None:
    """Reorder/relabel TenderManagement columns to match HTS grid; hide process extras."""
    party_alias = None
    for line in frm:
        m = re.search(r"LEFT JOIN \[Gnr\]\.\[Party\] AS \[(\w+)\] ON \[t1\]\.\[CompanyId\]", line)
        if m:
            party_alias = m.group(1)
            break

    def add(expr: str, col: dict) -> None:
        if any(c["alias"] == col["alias"] for c in cols):
            return
        select.append(expr)
        cols.append(col)

    has_tender_company_fields = db.has_col("Sale", "TenderManagement", "CompanyAddress")
    if has_tender_company_fields:
        add(
            "[t1].[CompanyAddress] AS [t1_CompanyAddress]",
            _col("Sale.TenderManagement", "CompanyAddress", "آدرس شرکت", "t1_CompanyAddress",
                 "[t1].[CompanyAddress]", "String", 0, True, 200),
        )
        add(
            "[t1].[CompanyPhone] AS [t1_CompanyPhone]",
            _col("Sale.TenderManagement", "CompanyPhone", "تلفن شرکت", "t1_CompanyPhone",
                 "[t1].[CompanyPhone]", "String", 0, True, 180),
        )
    elif party_alias:
        add(
            f"[{party_alias}].[Address] AS [{party_alias}_Address]",
            _col("Gnr.Party", "Address", "آدرس شرکت", f"{party_alias}_Address",
                 f"[{party_alias}].[Address]", "String", 0, True, 200),
        )
        add(
            f"[{party_alias}].[Phone] AS [{party_alias}_Phone]",
            _col("Gnr.Party", "Phone", "تلفن شرکت", f"{party_alias}_Phone",
                 f"[{party_alias}].[Phone]", "String", 0, True, 180),
        )

    if db.has_col("Sale", "TenderManagement", "CompanyIndustryItemId") and db.has_table("Gnr", "PartyIndustryItem"):
        if not any("[Gnr].[PartyIndustryItem]" in x for x in frm):
            frm.append("LEFT JOIN [Gnr].[PartyIndustryItem] AS [tItm] ON [t1].[CompanyIndustryItemId] = [tItm].[Id]")
        if not any("[Gnr].[PartyIndustry]" in x for x in frm):
            frm.append("LEFT JOIN [Gnr].[PartyIndustry] AS [tInd] ON [tItm].[PartyIndustryId] = [tInd].[Id]")
        add(
            "[tInd].[Title] AS [tInd_Title]",
            _col("Gnr.PartyIndustry", "Title", "صنعت مشتری", "tInd_Title",
                 "[tInd].[Title]", "String", 0, True, 180),
        )
        add(
            "[tItm].[Title] AS [tItm_Title]",
            _col("Gnr.PartyIndustryItem", "Title", "ریز صنعت مشتری", "tItm_Title",
                 "[tItm].[Title]", "String", 0, True, 180),
        )
    elif db.has_table("SLS", "Customer") and db.has_table("Gnr", "PartyIndustry"):
        if not any("[SLS].[Customer]" in x for x in frm):
            frm.append("LEFT JOIN [SLS].[Customer] AS [tCust] ON [t1].[CompanyId] = [tCust].[PartyId]")
            frm.append("LEFT JOIN [Gnr].[PartyIndustry] AS [tInd] ON [tCust].[IndustryId] = [tInd].[Id]")
        add(
            "[tInd].[Title] AS [tInd_Title]",
            _col("Gnr.PartyIndustry", "Title", "صنعت مشتری", "tInd_Title",
                 "[tInd].[Title]", "String", 0, True, 180),
        )

    if db.has_col("Sale", "TenderManagement", "ModifiedDateShamsiDateTime"):
        add(
            "[t1].[ModifiedDateShamsiDateTime] AS [t1_ModifiedDateShamsiDateTime]",
            _col("Sale.TenderManagement", "ModifiedDateShamsiDateTime", "آخرین بروزرسانی",
                 "t1_ModifiedDateShamsiDateTime", "[t1].[ModifiedDateShamsiDateTime]",
                 "DateTimeShamsi", 4, True, 140),
        )

    specs: list[tuple] = [
        (lambda c: c["alias"] == "t1_Id", "شماره مرجع", True, 100),
        (lambda c: c["alias"] == "t1_Organization", "واحد سازمانی", True, 140),
        (lambda c: c["alias"] == "t1_ProjectName", "عنوان مناقصه", True, 150),
        (lambda c: c["alias"] == "t1_TenderType", "نوع مناقصه", True, 160),
        (lambda c: c["table"] == "Gnr.Party" and c["column"] == "FullName", "شرکت", True, 200),
        (lambda c: c["alias"] == "tInd_Title", "صنعت مشتری", True, 180),
        (lambda c: c["alias"] == "tItm_Title", "ریز صنعت مشتری", True, 180),
        (lambda c: c["alias"] == "t1_CompanyAddress" or (c["table"] == "Gnr.Party" and c["column"] == "Address"), "آدرس شرکت", True, 200),
        (lambda c: c["alias"] == "t1_CompanyPhone" or (c["table"] == "Gnr.Party" and c["column"] == "Phone"), "تلفن شرکت", True, 180),
        (lambda c: c["alias"] == "t1_SalesAgent", "نمایندگی فروش", True, 150),
        (lambda c: c["alias"] == "t1_InquiryType", "نوع استعلام", True, 130),
        (lambda c: c["display"] == "کارشناس فروش" or c["alias"].endswith("_Name") and "کارشناس" in c["display"], "کارشناس فروش", True, 150),
        (lambda c: c["alias"] == "t1_FinalResult", "نتیجه نهایی", True, 150),
        (lambda c: c["alias"] == "t1_CurrentStatus", "آخرین وضعیت", True, 150),
        (lambda c: c["alias"] == "t1_HasSecondaryConfirm", "تایید نهایی", True, 100),
        (lambda c: c["alias"] == "t1_FailureReasonText", "علت راکدی", True, 250),
        (lambda c: c["alias"] == "t1_ContractNumber", "شماره قرارداد", True, 160),
        (lambda c: "کشور نصب" in c["display"], "کشور نصب", True, 160),
        (lambda c: "استان نصب" in c["display"], "استان نصب", True, 160),
        (lambda c: c["alias"] == "t1_CustomerAgentInfo", "نماینده کارفرما", True, 180),
        (lambda c: c["alias"] == "t1_CustomerAgentPhone", "تلفن کارفرما", True, 160),
        (lambda c: c["alias"] == "t1_PreInvoicePrice", "مبلغ پیش‌فاکتور ریالی", True, 150),
        (lambda c: c["alias"] == "t1_PriceUnit", "واحد پیش‌فاکتور ریالی", True, 140),
        (lambda c: c["alias"] == "t1_PreInvoiceShamsiDate", "تاریخ پیش‌فاکتور", True, 140),
        (lambda c: c["alias"] == "t1_PreInvoicePrice2", "مبلغ پیش‌فاکتور ارزی", True, 150),
        (lambda c: c["alias"] == "t1_PreInvoicePriceUnit2", "واحد پیش‌فاکتور ارزی", True, 140),
        (lambda c: c["alias"] == "t1_IndicatorId", "شماره پیش‌فاکتور", True, 140),
        (lambda c: c["alias"] == "t1_ProjectValue", "ارزش پروژه", True, 140),
        (lambda c: c["alias"] == "t1_EndUser", "کاربر نهایی", True, 150),
        (lambda c: c["alias"] == "t1_Explain", "توضیحات", True, 250),
        (lambda c: c["alias"] == "t1_SendForApproveShamsiDateTime", "تاریخ ارسال به تایید", True, 150),
        (lambda c: c["alias"] == "t1_OurPrice", "قیمت هوایار", True, 140),
        (lambda c: c["alias"] == "t1_FixedPrice", "قیمت تمام‌شده", True, 140),
        (lambda c: c["alias"] == "t1_OurPriceUnit", "واحد قیمت", True, 120),
        (lambda c: c["alias"] == "t1_CompetitivePrice", "قیمت رقیب", True, 140),
        (lambda c: c["alias"] == "t1_EuroPrice", "قیمت یورو", True, 120),
        (lambda c: c["alias"] == "t1_CompetitiveCompany", "شرکت رقیب", True, 150),
        (lambda c: c["alias"] == "t1_WarrantyType", "نوع ضمانت", True, 140),
        (lambda c: c["alias"] == "t1_WarrantyStatus", "وضعیت ضمانت", True, 140),
        (lambda c: c["alias"] == "t1_WarrantyNumber", "شماره ضمانت", True, 140),
        (lambda c: c["alias"] == "t1_WarrantyDueShamsiDate", "سررسید ضمانت", True, 140),
        (lambda c: c["alias"] == "t1_CreatedByName", "ایجادکننده", True, 140),
        (lambda c: c["alias"] == "t1_CreatedOnShamsiDateTime", "تاریخ ایجاد", True, 140),
        (lambda c: c["alias"] == "t1_ModifiedDateShamsiDateTime", "آخرین بروزرسانی", True, 140),
    ]

    used: set[str] = set()
    ordered: list[dict] = []
    for pred, display, vis, width in specs:
        for c in cols:
            if c["alias"] in used:
                continue
            if pred(c):
                c["display"] = display
                c["visible"] = vis
                c["width"] = width
                if c["alias"] == "t1_Id":
                    c["pk"] = True
                    c["visible"] = True
                ordered.append(c)
                used.add(c["alias"])
                break
    for c in cols:
        if c["alias"] in used:
            continue
        c["visible"] = False
        ordered.append(c)
        used.add(c["alias"])
    cols[:] = ordered
    expr_by_alias: dict[str, str] = {}
    for line in select:
        m = re.search(r"AS \[([^\]]+)\]", line)
        if m:
            expr_by_alias[m.group(1)] = line
    select[:] = [expr_by_alias[c["alias"]] for c in cols if c["alias"] in expr_by_alias]


def col_json(cols: list[dict]) -> str:
    parts = []
    for c in cols:
        sd = "1" if c["pk"] else "null"
        vis = "true" if c["visible"] else "false"
        pk = "true" if c["pk"] else "false"
        render = f'"{json_esc(c["render"])}"' if c.get("render") else "null"
        opt = (
            '{"ListOptions":[],"TypeOption":3,"SystemTypeName":"%s"}' % json_esc(c["enum"])
            if c.get("enum") else "null"
        )
        parts.append(
            '{"TableName":"%s","ColumnName":"%s","DisplayName":"%s","Alliance":"%s","Address":"%s",'
            '"SystemTypeName":"%s","SelectIndex":0,"Visible":%s,"PrimaryKey":%s,"SystemType":%s,'
            '"SortDirection":%s,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":%s,'
            '"Aggregate":null,"Render":%s,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,'
            '"BtnConfig":null,"InputConfig":null,"Width":%s,"Filterable":true,"Sortable":true,"ClassName":""}'
            % (
                json_esc(c["table"]), json_esc(c["alias"]), json_esc(c["display"]),
                json_esc(c["alias"]), json_esc(c["addr"]), json_esc(c["st_name"]),
                vis, pk, c["st_num"], sd, opt, render, c["width"],
            )
        )
    return "[" + ",\n".join(parts) + "]"


def build_profile(ent: dict, db: DbCols, enums: dict[str, str], extra_where: str | None) -> dict | None:
    schema, table = ent["schema"], ent["table"]
    if not db.has_table(schema, table):
        print(f"WARN skip {ent['class']}: missing {schema}.{table}")
        return None
    tbl_sql = f"[{schema}].[{table}]"
    select, frm, cols = [], [f"FROM {tbl_sql} AS [t1]"], []
    alias_n = [1]

    def next_alias() -> str:
        alias_n[0] += 1
        return f"t{alias_n[0]}"

    nav_by_fk: dict[str, dict] = {}
    for p in ent["props"]:
        if not p["is_nav"]:
            continue
        fk = p["fk"]
        if not fk:
            cand = next((x for x in ent["props"] if x["name"] == p["name"] + "Id"), None)
            if cand:
                fk = cand["name"]
        if fk:
            rel = p["type"].replace("?", "").replace("virtual ", "").strip()
            nav_by_fk[fk] = {"display": p["display"], "rel": rel}

    if db.has_col(schema, table, "Id"):
        select.append("[t1].[Id] AS [t1_Id]")
        cols.append({"table": f"{schema}.{table}", "column": "Id", "display": "شناسه",
                     "alias": "t1_Id", "addr": "[t1].[Id]", "st_name": "Long", "st_num": 6,
                     "visible": False, "pk": True, "width": 80, "render": None, "enum": None})
    if db.has_col(schema, table, "HtsId"):
        select.append("[t1].[HtsId] AS [t1_HtsId]")
        cols.append({"table": f"{schema}.{table}", "column": "HtsId", "display": "شناسه HTS",
                     "alias": "t1_HtsId", "addr": "[t1].[HtsId]", "st_name": "Long", "st_num": 6,
                     "visible": False, "pk": False, "width": 80, "render": None, "enum": None})

    skip = {
        "Id", "HtsId", "CreatedById", "ModifiedById", "ModifiedByName",
        "ModifiedDateMiladiDateTime", "ModifiedDateShamsiDateTime", "CreatedOnMiladiDateTime",
    }
    for p in ent["props"]:
        if p["is_nav"] or p["name"] in skip or "Miladi" in p["name"]:
            continue
        if re.match(r"^(Hamkaran|Rahkaran|Legacy|HtsPart|HtsCng|HtsPersonel)", p["name"]):
            continue
        if not db.has_col(schema, table, p["column"]):
            continue
        if p["name"] in nav_by_fk:
            nav = nav_by_fk[p["name"]]
            spec = JOIN_BY_TYPE.get(nav["rel"])
            if spec and db.has_table(spec["schema"], spec["table"]):
                a = next_alias()
                qt = f"[{spec['schema']}].[{spec['table']}]"
                frm.append(f"LEFT JOIN {qt} AS [{a}] ON [t1].[{p['column']}] = [{a}].[Id]")
                if spec.get("via_party") and db.has_table("Gnr", "Party"):
                    ap = next_alias()
                    frm.append(f"LEFT JOIN [Gnr].[Party] AS [{ap}] ON [{a}].[PartyId] = [{ap}].[Id]")
                    al = f"{ap}_FullName"
                    select.append(f"[{ap}].[FullName] AS [{al}]")
                    cols.append({"table": "Gnr.Party", "column": "FullName", "display": nav["display"],
                                 "alias": al, "addr": f"[{ap}].[FullName]", "st_name": "String", "st_num": 0,
                                 "visible": True, "pk": False, "width": 200, "render": None, "enum": None})
                elif spec.get("nested_order") and db.has_table("Sale", "Order"):
                    ao = next_alias()
                    frm.append(f"LEFT JOIN [Sale].[Order] AS [{ao}] ON [{a}].[Sale_OrderId] = [{ao}].[Id]")
                    select.append(f"[{ao}].[OrderNumber] AS [{ao}_OrderNumber]")
                    select.append(f"[{a}].[Seq] AS [{a}_Seq]")
                    cols.append({"table": "Sale.Order", "column": "OrderNumber", "display": "شماره حواله",
                                 "alias": f"{ao}_OrderNumber", "addr": f"[{ao}].[OrderNumber]",
                                 "st_name": "Long", "st_num": 6, "visible": True, "pk": False, "width": 110,
                                 "render": None, "enum": None})
                    cols.append({"table": "Sale.OrderDetail", "column": "Seq", "display": "ردیف حواله",
                                 "alias": f"{a}_Seq", "addr": f"[{a}].[Seq]",
                                 "st_name": "Int", "st_num": 7, "visible": True, "pk": False, "width": 70,
                                 "render": None, "enum": None})
                elif spec.get("cols"):
                    for col, suffix in spec["cols"]:
                        if not db.has_col(spec["schema"], spec["table"], col):
                            continue
                        al = f"{a}_{col}"
                        select.append(f"[{a}].[{col}] AS [{al}]")
                        cols.append({"table": f"{spec['schema']}.{spec['table']}", "column": col,
                                     "display": f"{nav['display']} — {suffix}",
                                     "alias": al, "addr": f"[{a}].[{col}]", "st_name": "String", "st_num": 0,
                                     "visible": True, "pk": False, "width": 140, "render": None, "enum": None})
                elif spec.get("col") and db.has_col(spec["schema"], spec["table"], spec["col"]):
                    col = spec["col"]
                    al = f"{a}_{col}"
                    select.append(f"[{a}].[{col}] AS [{al}]")
                    st_name, st_num = "String", 0
                    if col in ("Id", "OrderNumber"):
                        st_name, st_num = "Long", 6
                    elif col == "HokmNumber":
                        st_name, st_num = "Int", 7
                    cols.append({"table": f"{spec['schema']}.{spec['table']}", "column": col,
                                 "display": nav["display"], "alias": al, "addr": f"[{a}].[{col}]",
                                 "st_name": st_name, "st_num": st_num, "visible": True, "pk": False,
                                 "width": 160, "render": None, "enum": None})
                continue

        st_name, st_num = infer_st(p, enums)
        enum_name = None
        if st_num == 8:
            tn = p["type"].rstrip("?")
            enum_name = enums.get(tn)
        vis = p["visible"]
        if p["name"] == "IsActive":
            vis = True
            st_name, st_num = "Select", 8
            enum_name = "Entities.Base.IsActiveEnum"
        width = 140
        if st_num in (4, 5):
            width = 120
        if st_num == 1:
            width = 80
        if re.search(r"Comment|Description|Address|Feedback", p["name"]):
            width = 220
        render = None
        if is_money(p["name"], p["display"]) and st_num in (6, 7, 14):
            render = MONEY_RENDER
            width = 120
        al = f"t1_{p['column']}"
        select.append(f"[t1].[{p['column']}] AS [{al}]")
        cols.append({"table": f"{schema}.{table}", "column": p["column"], "display": p["display"],
                     "alias": al, "addr": f"[t1].[{p['column']}]", "st_name": st_name, "st_num": st_num,
                     "visible": vis, "pk": False, "width": width, "render": render, "enum": enum_name})

    if db.has_col(schema, table, "CreatedByName") and not any(c["alias"] == "t1_CreatedByName" for c in cols):
        select.append("[t1].[CreatedByName] AS [t1_CreatedByName]")
        cols.append({"table": f"{schema}.{table}", "column": "CreatedByName", "display": "ایجادکننده",
                     "alias": "t1_CreatedByName", "addr": "[t1].[CreatedByName]", "st_name": "String",
                     "st_num": 0, "visible": True, "pk": False, "width": 140, "render": None, "enum": None})
    if db.has_col(schema, table, "CreatedOnShamsiDateTime") and not any(c["alias"] == "t1_CreatedOnShamsiDateTime" for c in cols):
        select.append("[t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime]")
        cols.append({"table": f"{schema}.{table}", "column": "CreatedOnShamsiDateTime", "display": "تاریخ ایجاد",
                     "alias": "t1_CreatedOnShamsiDateTime", "addr": "[t1].[CreatedOnShamsiDateTime]",
                     "st_name": "DateTimeShamsi", "st_num": 4, "visible": True, "pk": False, "width": 140,
                     "render": None, "enum": None})

    if ent["class"] == "TenderManagement":
        for col, display, st_name, st_num in (
            ("ParentId", "والد", "Long", 6),
            ("CreatedById", "شناسه ایجادکننده", "Long", 6),
            ("SaleExpertId", "شناسه کارشناس", "Long", 6),
        ):
            alias = f"t1_{col}"
            if db.has_col(schema, table, col) and not any(c["alias"] == alias for c in cols):
                select.append(f"[t1].[{col}] AS [{alias}]")
                cols.append({"table": f"{schema}.{table}", "column": col, "display": display,
                             "alias": alias, "addr": f"[t1].[{col}]", "st_name": st_name, "st_num": st_num,
                             "visible": False, "pk": False, "width": 80, "render": None, "enum": None})
        apply_tender_hts_layout(select, frm, cols, db)

    sql = "SELECT\n    " + ",\n    ".join(select) + "\n" + "\n".join(frm)
    if extra_where:
        sql += "\n" + extra_where
    return {"select": sql, "columns": cols}


def entity_route(cls: str) -> str:
    return ROUTE_BY_ENTITY.get(cls, f"/Panel/Sale/{cls}")


def btn(id_, title, data_action, color, icon, js, requires=True) -> str:
    return (
        '{"id":"%s","title":"%s","dataActionName":"%s","colorClass":"%s","iconClass":"%s",'
        '"requiresSelection":%s,"requiresMultiSelection":false,"useHtml":false,"html":"",'
        '"actionScript":"%s"}'
        % (id_, json_esc(title), data_action, color, icon, "true" if requires else "false", json_esc(js))
    )


EDIT_JS = """function(ctx) {
    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));
    if (!id) { toastr.error('لطفاً یک ردیف را انتخاب کنید'); return; }
    appController.addPage('%s/Edit?id=' + id, true, 'ویرایش');
}"""

CHILD_JS = """function(ctx) {
    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && ctx.selectedRow.t1_Id));
    if (!id) { toastr.error('لطفاً یک ردیف را انتخاب کنید'); return; }
    appController.addPage('%s' + id, true, '%s');
}"""


def action_buttons(cls: str, kind: str) -> str:
    base = entity_route(cls)
    items = [btn(f"as_edit_{cls.lower()}", "ویرایش", "editRow", "btn-color-primary",
                 "ki-pencil ki-outline", EDIT_JS % base, True)]
    if cls == "ServiceRequest" and kind == "List":
        items.append(btn("as_print_sr", "چاپ برگه درخواست", "printSheet", "btn-color-info",
                         "ki-printer ki-outline",
                         EDIT_JS.replace("/Edit?id=", "/PrintSheet?id=").replace("'ویرایش'", "'چاپ برگه درخواست'") % "/Panel/Sale/ServiceRequest",
                         True))
        items.append(btn("as_cartable_sr", "کارتابل مسئول منطقه", "zoneCartable", "btn-color-success",
                         "ki-tablet ki-outline",
                         "function(ctx) { appController.addPage('/Panel/Sale/ServiceRequestDetail/Cartable', true, 'کارتابل مسئول منطقه'); }",
                         False))
        for i, title, path in (
            ("details", "جزئیات", "/Panel/Sale/ServiceRequestDetail/ListByParentId?serviceRequestId="),
            ("dispatch", "اعزام", "/Panel/Sale/ServiceRequestExpertMission/ListByParentId?serviceRequestId="),
            ("att", "پیوست", "/Panel/Sale/ServiceRequestAttachment/ListByParentId?serviceRequestId="),
            ("msn", "ماموریت", "/Panel/Sale/Mission/ListByParentId?serviceRequestId="),
            ("wr", "گزارش کار", "/Panel/Sale/WorkReport/ListByParentId?serviceRequestId="),
            ("srp", "قطعات", "/Panel/Sale/ServiceRequestPart/ListByParentId?serviceRequestId="),
        ):
            items.append(btn(f"as_sr_{i}", title, i, "btn-color-primary", "ki-exit-right-corner ki-outline",
                             CHILD_JS % (path, title), True))
    if cls == "RepairRequest":
        for i, title, path, icon, color in (
            ("parts", "قطعات", "/Panel/Rpr/RepairRequestPart/ListByParentId?repairRequestId=",
             "ki-cube-2 ki-outline", "btn-color-primary"),
            ("mh", "نفرساعت", "/Panel/Rpr/RepairRequestManHour/ListByParentId?repairRequestId=",
             "ki-time ki-outline", "btn-color-info"),
            ("cmt", "نظرات", "/Panel/Rpr/RepairRequestComment/ListByParentId?repairRequestId=",
             "ki-message-text-2 ki-outline", "btn-color-warning"),
            ("att", "پیوست", "/Panel/Rpr/RepairRequestAttachment/ListByParentId?repairRequestId=",
             "ki-paper-clip ki-outline", "btn-color-success"),
            ("ctr", "پیمانکار", "/Panel/Rpr/ContractorOrder/ListByParentId?repairRequestId=",
             "ki-people ki-outline", "btn-color-info"),
        ):
            items.append(btn(f"as_rr_{i}", title, i, color, icon,
                             CHILD_JS % (path, title), True))
    if cls == "OrderDetailSerial":
        items.append(btn("as_ods_att", "پیوست سریال", "serialAttachments", "btn-color-primary",
                         "ki-paper-clip ki-outline",
                         CHILD_JS % ("/Panel/Sale/OrderDetailSerialAttachment/ListByParentId?orderDetailSerialId=", "پیوست سریال"),
                         True))
    if cls == "TenderManagement":
        items.append(btn("as_tender_parts", "اقلام مناقصه", "tenderParts", "btn-color-primary",
                         "ki-some-files ki-outline",
                         CHILD_JS % ("/Panel/Sale/TenderPart/ListByParentId?tenderId=", "اقلام مناقصه"), True))
        items.append(btn("as_tender_att", "پیوست", "tenderAtt", "btn-color-success",
                         "ki-paper-clip ki-outline",
                         CHILD_JS % ("/Panel/Sale/TenderAttachment/ListByParentId?tenderId=", "پیوست مناقصه"), True))
        items.append(btn("as_tender_cmt", "یادداشت", "tenderCmt", "btn-color-warning",
                         "ki-message-text-2 ki-outline",
                         CHILD_JS % ("/Panel/Sale/TenderComment/ListByParentId?tenderId=", "یادداشت مناقصه"), True))
        items.append(btn("as_tender_task", "تسک", "tenderTask", "btn-color-info",
                         "ki-notepad-edit ki-outline",
                         CHILD_JS % ("/Panel/Sale/TenderTask/ListByParentId?tenderId=", "تسک مناقصه"), True))
        if kind == "Approve":
            items.append(btn("as_tender_secondary", "تایید", "secondaryConfirm", "btn-color-success",
                             "ki-double-check ki-outline",
                             "function(ctx) { var id = ctx && (ctx.primaryKeyValue || (ctx.selectedRow && ctx.selectedRow.t1_Id)); if (!id) { toastr.error('لطفاً یک ردیف را انتخاب کنید'); return; } post('/Panel/Sale/TenderManagement/SecondaryConfirm?id=' + id, {}, function (r) { if (!r || !r.isSuccess) { toastr.error((r && r.message) || 'خطا'); return; } toastr.success('تایید انجام شد'); if (ctx.draw) ctx.draw(); }); }",
                             True))
    if cls == "OrderPoint":
        items.append(btn("as_op_cmt", "یادداشت", "orderPointComments", "btn-color-warning",
                         "ki-message-text-2 ki-outline",
                         CHILD_JS % ("/Panel/Sale/OrderPointComment/ListByParentId?orderPointId=", "یادداشت نقطه سفارش"), True))
    if cls == "ProjectUtilizedMaterial":
        items.append(btn("as_pum_changes", "سوابق تغییرات", "materialChanges", "btn-color-info",
                         "ki-time ki-outline",
                         CHILD_JS % ("/Panel/Sale/ProjectUtilizedMaterialChange/ListByParentId?projectUtilizedMaterialId=", "سوابق تغییرات اقلام مصرفی"), True))
    if cls == "ResponsibleZone":
        items.append(btn("as_rz_cust", "مشتریان", "zoneCustomers", "btn-color-info",
                         "ki-people ki-outline",
                         CHILD_JS % ("/Panel/Sale/ResponsibleZoneCustomer/ListByParentId?responsibleZoneId=", "مشتریان مسئول منطقه"), True))
    if cls == "TechnicalQuery":
        items.append(btn("as_tq_cmt", "یادداشت", "tqComments", "btn-color-warning",
                         "ki-message-text-2 ki-outline",
                         CHILD_JS % ("/Panel/Sale/TechnicalQueryComment/ListByParentId?technicalQueryId=", "یادداشت پرسش فنی"), True))
        if kind == "Approve":
            items.append(btn("as_tq_approve", "تایید", "approveTq", "btn-color-success",
                             "ki-check ki-outline",
                             "function(ctx) { var id = ctx && (ctx.primaryKeyValue || (ctx.selectedRow && ctx.selectedRow.t1_Id)); if (!id) { toastr.error('لطفاً یک ردیف را انتخاب کنید'); return; } post('/Panel/Sale/TechnicalQuery/Approve?id=' + id, {}, function (r) { if (!r || !r.isSuccess) { toastr.error((r && r.message) || 'خطا'); return; } toastr.success('تایید انجام شد'); if (ctx.draw) ctx.draw(); }); }",
                             True))
            items.append(btn("as_tq_reject", "رد", "rejectTq", "btn-color-danger",
                             "ki-cross ki-outline",
                             "function(ctx) { var id = ctx && (ctx.primaryKeyValue || (ctx.selectedRow && ctx.selectedRow.t1_Id)); if (!id) { toastr.error('لطفاً یک ردیف را انتخاب کنید'); return; } post('/Panel/Sale/TechnicalQuery/Reject?id=' + id, {}, function (r) { if (!r || !r.isSuccess) { toastr.error((r && r.message) || 'خطا'); return; } toastr.success('رد انجام شد'); if (ctx.draw) ctx.draw(); }); }",
                             True))
    if cls == "AgencyCartable":
        items.append(btn("as_ag_accept", "پذیرش", "acceptAgency", "btn-color-success",
                         "ki-check ki-outline",
                         "function(ctx) { var id = ctx && (ctx.primaryKeyValue || (ctx.selectedRow && ctx.selectedRow.t1_Id)); if (!id) { toastr.error('لطفاً یک ردیف را انتخاب کنید'); return; } post('/Panel/Sale/AgencyCartable/Accept?id=' + id, {}, function (r) { if (!r || !r.isSuccess) { toastr.error((r && r.message) || 'خطا'); return; } toastr.success('پذیرش انجام شد'); if (ctx.draw) ctx.draw(); }); }",
                         True))
        items.append(btn("as_ag_reject", "رد", "rejectAgency", "btn-color-danger",
                         "ki-cross ki-outline",
                         "function(ctx) { var id = ctx && (ctx.primaryKeyValue || (ctx.selectedRow && ctx.selectedRow.t1_Id)); if (!id) { toastr.error('لطفاً یک ردیف را انتخاب کنید'); return; } post('/Panel/Sale/AgencyCartable/Reject?id=' + id, {}, function (r) { if (!r || !r.isSuccess) { toastr.error((r && r.message) || 'خطا'); return; } toastr.success('رد انجام شد'); if (ctx.draw) ctx.draw(); }); }",
                         True))
    if cls == "CollectionClaim":
        items.append(btn("as_cc_fish", "فیش‌های وصول", "payFish", "btn-color-warning",
                         "ki-wallet ki-outline",
                         "function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error('لطفاً یک ردیف را انتخاب کنید'); return; }\n    appController.addPage('/Panel/Sale/CollectionClaimPayFish/List?collectionClaimId=' + id, true, 'فیش‌های وصول');\n}",
                         True))
    return "[" + ",".join(items) + "]"


def parse_controllers(entity_by: dict[str, dict]) -> list[dict]:
    files = list((CTRL_ROOT / "Sale").glob("*Controller.cs"))
    files += list((CTRL_ROOT / "Rpr").glob("*Controller.cs"))
    files += list((CTRL_ROOT / "Crm").glob("*Controller.cs"))
    actions = []
    rx = re.compile(
        r'\[ActionDisplayName\("(?P<dn>[^"]+)"\s*,\s*ActionAccessType\.(?P<at>\w+)'
        r"(?:\s*,\s*ActionAccessItemType\.(?P<it>\w+))?\)\]\s*"
        r"(?:\[[^\]]+\]\s*)*"
        r"public\s+(?:async\s+)?(?:Task<IActionResult>|IActionResult)\s+(?P<mn>\w+)",
        re.S,
    )
    for f in files:
        if f.name in SKIP_CTRL or f.stem in {x.replace(".cs", "") for x in SKIP_CTRL}:
            continue
        if f.stem in SKIP_CTRL:
            continue
        text = f.read_text(encoding="utf-8")
        rm = re.search(r'\[Route\("([^"]+)"\)\]', text)
        if not rm:
            continue
        ctrl = f.stem.replace("Controller", "")
        if ctrl in ("ProductionOrder", "ProductionOrderItem", "OrderDetail", "RequirmentAdvertise", "Branch"):
            continue
        prefix = rm.group(1).replace("[controller]", ctrl).lower()
        entity_name = None
        em = re.search(r'ControllerInfo\("[^"]+"\s*,\s*typeof\((\w+)\)', text)
        if em and em.group(1) in entity_by:
            entity_name = entity_by[em.group(1)]["full"]
        for m in rx.finditer(text):
            at = ACCESS_TYPE.get(m.group("at"), 1)
            it = ACCESS_ITEM.get(m.group("it"), 0) if m.group("it") else 0
            path = "/" + prefix.strip("/") + "/" + m.group("mn").lower()
            actions.append({
                "path": path, "access": at, "item": it,
                "entity": entity_name, "display": m.group("dn"), "controller": ctrl,
            })
    return actions


def tweak_customer_allowed_guarantee(built: dict) -> dict:
    """HTS page 206 grid: کد مشتری، مشتری، مقدار قطعه، مقدار ماموریت، شماره سفارش ساخت."""
    cols = built["columns"]
    select = built["select"]
    if not any(c.get("alias") == "t2_Code" for c in cols):
        insert_at = 0
        for i, line in enumerate(select):
            if "t1_HtsId" in line:
                insert_at = i + 1
                break
        select.insert(insert_at, "[t2].[Code] AS [t2_Code]")
        code_col = {
            "table": "SLS.Customer", "column": "Code", "display": "کد مشتری",
            "alias": "t2_Code", "addr": "[t2].[Code]", "st_name": "Int", "st_num": 7,
            "visible": True, "pk": False, "width": 110, "render": None, "enum": None,
        }
        hts_idx = next((i for i, c in enumerate(cols) if c.get("alias") == "t1_HtsId"), 1)
        cols.insert(hts_idx + 1, code_col)
    for c in cols:
        if c.get("alias") in ("t1_CreatedByName", "t1_CreatedOnShamsiDateTime"):
            c["visible"] = False
        if c.get("alias") == "t1_AllowedGuaranteePartAmount":
            c["display"] = "مقدار گارانتی مجاز قطعه"
            c["width"] = 160
        if c.get("alias") == "t1_AllowedMissionGuaranteeAmount":
            c["display"] = "مقدار گارانتی مجاز ماموریت"
            c["width"] = 160
        if c.get("alias") == "t3_FullName":
            c["display"] = "مشتری"
    desired = [
        "t1_Id", "t1_HtsId", "t2_Code", "t3_FullName",
        "t1_AllowedGuaranteePartAmount", "t1_AllowedMissionGuaranteeAmount",
        "t1_ProductionOrderNumber", "t1_CreatedByName", "t1_CreatedOnShamsiDateTime",
    ]
    by_alias = {c["alias"]: c for c in cols}
    ordered = [by_alias[a] for a in desired if a in by_alias]
    ordered.extend(c for c in cols if c["alias"] not in by_alias)
    built["columns"] = ordered
    expr_by_alias = {}
    for line in select:
        m = re.search(r"AS \[([^\]]+)\]", line)
        if m:
            expr_by_alias[m.group(1)] = line
    built["select"] = [expr_by_alias[c["alias"]] for c in ordered if c["alias"] in expr_by_alias]
    return built


def tweak_responsible_zone(built: dict) -> dict:
    """HTS page 356 grid: مسئول، استان، منطقه، توضیحات، ایجادکننده، تاریخ ایجاد. CustomersTitle hidden.

    Decisions 2026-09-18: header+customers save together on Edit (not this list button);
    Responsible combo is OrgUnit 119; leftover provinces mapped to استان البرز/تهران/اصفهان;
    CreatedBy/CreatedOn come from HTS; list «مشتریان» button stays for viewing children.
    """
    cols = built["columns"]
    for c in cols:
        if c.get("alias") == "t1_CustomersTitle":
            c["visible"] = False
        if c.get("alias") == "t2_Name":
            c["display"] = "مسئول"
            c["width"] = 150
        if c.get("alias") == "t3_Name":
            c["display"] = "استان"
            c["width"] = 150
        if c.get("alias") == "t4_Title":
            c["display"] = "منطقه"
            c["width"] = 150
        if c.get("alias") == "t1_Comment":
            c["display"] = "توضیحات"
            c["width"] = 250
        if c.get("alias") == "t1_CreatedByName":
            c["display"] = "ایجادکننده"
            c["width"] = 100
        if c.get("alias") == "t1_CreatedOnShamsiDateTime":
            c["display"] = "تاریخ ایجاد"
            c["width"] = 100
    return built


def tweak_customer_address(built: dict) -> dict:
    """HTS page 234 grid: کد مشتری، مشتری، منطقه، استان، گرید، نام جایگاه، آدرس، تفصیل، ویرایشگر."""
    cols = built["columns"]
    select = built["select"]
    if not any(c.get("alias") == "t2_Code" for c in cols):
        insert_at = 0
        for i, line in enumerate(select):
            if "t1_HtsId" in line:
                insert_at = i + 1
                break
        select.insert(insert_at, "[t2].[Code] AS [t2_Code]")
        code_col = {
            "table": "SLS.Customer", "column": "Code", "display": "کد مشتری",
            "alias": "t2_Code", "addr": "[t2].[Code]", "st_name": "Int", "st_num": 7,
            "visible": True, "pk": False, "width": 100, "render": None, "enum": None,
        }
        hts_idx = next((i for i, c in enumerate(cols) if c.get("alias") == "t1_HtsId"), 1)
        cols.insert(hts_idx + 1, code_col)
    for c in cols:
        if c.get("alias") == "t1_Title":
            c["display"] = "نام جایگاه"
            c["width"] = 160
        if c.get("alias") == "t1_Address":
            c["display"] = "آدرس"
            c["width"] = 220
        if c.get("alias") == "t3_FullName":
            c["display"] = "مشتری"
            c["width"] = 180
        if c.get("alias") == "t4_Title":
            c["display"] = "منطقه"
            c["width"] = 130
        if c.get("alias") == "t5_Name":
            c["display"] = "استان"
            c["width"] = 130
        if c.get("alias") == "t1_Grade":
            c["display"] = "گرید"
            c["width"] = 90
        if c.get("alias") == "t7_Title":
            c["display"] = "تفصیل نمایندگی"
            c["width"] = 180
        if c.get("alias") == "t6_FullName":
            c["visible"] = False
        if c.get("alias") in ("t1_CreatedByName", "t1_CreatedOnShamsiDateTime"):
            c["visible"] = False
        if c.get("alias") == "t1_ModifiedByName":
            c["display"] = "کاربر ویرایشگر"
            c["visible"] = True
            c["width"] = 130
        if c.get("alias") == "t1_ModifiedDateShamsiDateTime":
            c["display"] = "زمان ویرایش"
            c["visible"] = True
            c["width"] = 140
    desired = [
        "t1_Id", "t1_HtsId", "t2_Code", "t3_FullName", "t4_Title", "t5_Name",
        "t1_Grade", "t1_Title", "t1_Address", "t7_Title",
        "t1_ModifiedByName", "t1_ModifiedDateShamsiDateTime",
    ]
    by_alias = {c["alias"]: c for c in cols}
    ordered = [by_alias[a] for a in desired if a in by_alias]
    ordered.extend(c for c in cols if c["alias"] not in desired)
    built["columns"] = ordered
    expr_by_alias = {}
    for line in select:
        m = re.search(r"AS \[([^\]]+)\]", line)
        if m:
            expr_by_alias[m.group(1)] = line
    built["select"] = [expr_by_alias[c["alias"]] for c in ordered if c["alias"] in expr_by_alias]
    return built


def tweak_responsible_zone_customer(built: dict) -> dict:
    """HTS child grid: کد مشتری، عنوان مشتری. ResponsibleZoneId hidden."""
    cols = built["columns"]
    for c in cols:
        if c.get("alias") == "t1_ResponsibleZoneId":
            c["visible"] = False
        if c.get("alias") == "t2_Code":
            c["display"] = "کد مشتری"
            c["width"] = 130
        if c.get("alias") == "t3_FullName":
            c["display"] = "مشتری"
            c["width"] = 400
        if c.get("alias") in ("t1_CreatedByName", "t1_CreatedOnShamsiDateTime"):
            c["visible"] = False
    return built


def roles_for_path(path: str) -> list[str]:
    p = path.lower()
    if "/customerrequest/" in p:
        view = [
            "ShowAllMenus",
            "Sale.AfterSales.CustomerRequest.View",
            "Sale.AfterSales.CustomerRequest.Manage",
        ]
        write = ["ShowAllMenus", "Sale.AfterSales.CustomerRequest.Manage"]
        if p.endswith(("/list", "/fetchdata", "/exporttoexcel", "/edit", "/getcomments", "/downloadattachment")):
            return view
        if p.endswith(("/new", "/add", "/delete", "/save", "/update")):
            return []
        return write
    if "/customeraddress/" in p:
        # HTS page 234: AfterSales (group 69) has no access. View 48+350; Edit three users.
        view = [
            "ShowAllMenus",
            "Sale.AfterSales.CustomerAddress.View",
            "Sale.AfterSales.CustomerAddress.Manage",
        ]
        write = ["ShowAllMenus", "Sale.AfterSales.CustomerAddress.Manage"]
        if p.endswith(("/list", "/fetchdata", "/exporttoexcel", "/edit")):
            return view
        if p.endswith(("/new", "/add", "/delete")):
            return []
        return write
    if "/customerallowedguarantee/" in p:
        view = [
            "ShowAllMenus", "Sale.AfterSales",
            "Sale.AfterSales.CustomerAllowedGuarantee.View",
            "Sale.AfterSales.CustomerAllowedGuarantee.Manage",
        ]
        write = ["ShowAllMenus", "Sale.AfterSales", "Sale.AfterSales.CustomerAllowedGuarantee.Manage"]
        if p.endswith(("/list", "/fetchdata")):
            return view
        return write
    if "/responsiblezonecustomer/" in p or p.startswith("/panel/sale/responsiblezone/"):
        view = [
            "ShowAllMenus", "Sale.AfterSales",
            "Sale.AfterSales.ResponsibleZone.View",
            "Sale.AfterSales.ResponsibleZone.Manage",
        ]
        write = ["ShowAllMenus", "Sale.AfterSales", "Sale.AfterSales.ResponsibleZone.Manage"]
        if p.endswith(("/list", "/fetchdata", "/listbyparentid", "/getlistbyparentid")):
            return view
        return write
    tender_write = [
        "ShowAllMenus", "Sale.AfterSales", "Sale.Tenders.ShowAll",
        "Sale.Tenders.CompressedAir", "Sale.Tenders.OilGas", "Sale.Tenders.Manager",
    ]
    tender_view = tender_write + ["Sale.Tenders.View", "Sale.Tenders.Approver"]
    if any(x in p for x in ("/tenderpart/", "/tendertask/", "/tendercomment/", "/tenderattachment/", "/tenderproductgroup/")):
        if p.endswith(("/save", "/add", "/update", "/delete", "/new")):
            return list(tender_write)
        return list(tender_view)
    if "/tendermanagement/" in p:
        if p.endswith("/tenderscompressedair"):
            return ["ShowAllMenus", "Sale.Tenders.CompressedAir"]
        if p.endswith("/tendersoilgas"):
            return ["ShowAllMenus", "Sale.Tenders.OilGas"]
        if p.endswith("/tendersextraoperations"):
            return ["ShowAllMenus", "Sale.AfterSales", "Sale.Tenders.ShowAll"]
        if p.endswith("/tendersmanagerapprove") or p.endswith("/secondaryconfirm"):
            return ["ShowAllMenus", "Sale.Tenders.Manager"]
        if p.endswith("/primaryconfirm"):
            return ["ShowAllMenus", "Sale.AfterSales", "Sale.Tenders.Approver", "Sale.Tenders.Manager"]
        if p.endswith("/approvequeue") or p.endswith("/fetchapprovequeue"):
            return ["ShowAllMenus", "Sale.AfterSales", "Sale.Tenders.Approver", "Sale.Tenders.Manager", "Sale.Tenders.View"]
        roles = list(tender_write)
        if p.endswith(("/list", "/fetchdata", "/exporttoexcel", "/edit", "/getcompanyinfo", "/getindustryiteminfo")):
            roles.extend(["Sale.Tenders.Approver", "Sale.Tenders.View"])
        return roles
    if p.endswith("/industrialconfirm"):
        # HTS perm 53 / group 304 only — members via Seed_AfterSales_Monitoring_RolesAndUsers.sql
        return ["ShowAllMenus"]
    if p.endswith("/monitoringinventoryaccess") or p.endswith("/monitoringexitaccess"):
        # HTS 188/189 — groups 543/544 only; not Sale.AfterSales
        return ["ShowAllMenus"]
    roles = ["ShowAllMenus"]
    if path.startswith("/panel/rpr/"):
        roles.append("Rpr.Repairs")
        if "/repairrequest/" in path:
            roles.append("Sale.AfterSales")
    elif path.startswith("/panel/crm/customersatisfactionsurvey"):
        roles += ["Crm.AfterSalesSurvey", "Sale.AfterSales"]
    else:
        roles.append("Sale.AfterSales")
    return roles


def emit_sql(profiles: list[dict], actions: list[dict]) -> str:
    lines = []
    a = lines.append
    a("""/*
  Seed_AfterSalesServiceSystem_DataProfilesAndAccess.sql
  Idempotent. Generated by generate_aftersales_seed.py
  [1] Roles Sale.AfterSales / Rpr.Repairs / Crm.AfterSalesSurvey
  [2] SavedQuery Type=DataProfile for after-sales List pages
  [3] RoleAccess DataProfile (type 3) rebuilt only for these SavedQuery ids
  [4] RoleAccess View/Api for dedicated after-sales roles only (does not wipe Sale.OrderDetail.*)
  [5] Menu AfterSalesServiceSystem AccessRoleIds merge
  OrderDetail_WithPrice / OrderDetail_NoPrice reused — no duplicate حواله profile.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-after-sales-dataprofiles';

    PRINT N'=== [1] Roles ===';
    IF OBJECT_ID('tempdb..#AsRoles') IS NOT NULL DROP TABLE #AsRoles;
    CREATE TABLE #AsRoles (WantId BIGINT NOT NULL, Name NVARCHAR(200) NOT NULL, Title NVARCHAR(200) NOT NULL);
    INSERT INTO #AsRoles (WantId, Name, Title) VALUES
        (500000, N'Sale.AfterSales', N'خدمات پس از فروش - دسترسی کامل'),
        (500001, N'Rpr.Repairs', N'تعمیرات - دسترسی کامل'),
        (500002, N'Crm.AfterSalesSurvey', N'رضایت‌سنجی بعد از فروش'),
        (500010, N'Sale.Tenders.Approver', N'مناقصه خدمات پس از فروش - تاییدکننده'),
        (500011, N'Sale.Tenders.DuplicateNotify', N'مناقصه خدمات پس از فروش - هشدار تکراری'),
        (500012, N'Sale.Tenders.Manager', N'مناقصه خدمات پس از فروش - مدیر'),
        (500013, N'Sale.Tenders.ShowAll', N'مناقصه خدمات پس از فروش - نمایش همه'),
        (500014, N'Sale.Tenders.CompressedAir', N'مناقصه خدمات پس از فروش - هوای فشرده'),
        (500015, N'Sale.Tenders.OilGas', N'مناقصه خدمات پس از فروش - نفت و گاز'),
        (500016, N'Sale.Tenders.View', N'مناقصه خدمات پس از فروش - مشاهده'),
        (500017, N'Sale.AfterSales.FactoryExitNotify', N'مانیتورینگ فروش - اعلان خروج کارخانه (HTS 542)'),
        (500018, N'Sale.AfterSales.FactoryExitCustomerCall', N'مانیتورینگ فروش - تماس خروج کارخانه (HTS 570)'),
        (500019, N'Sale.AfterSales.Monitoring.View', N'مانیتورینگ فروش - مشاهده (HTS 61/350/590)'),
        (500020, N'Sale.AfterSales.Monitoring.Edit', N'مانیتورینگ فروش - ویرایش (HTS 51/48)'),
        (500021, N'Sale.AfterSales.Monitoring.IndustrialConfirm', N'مانیتورینگ فروش - تایید صنایع (HTS 304)'),
        (500022, N'Sale.AfterSales.Monitoring.Exit', N'مانیتورینگ فروش - ثبت ساعت خروج (HTS 543)'),
        (500023, N'Sale.AfterSales.Monitoring.Inventory', N'مانیتورینگ فروش - دسترسی انبار (HTS 544)'),
        (500024, N'Sale.AfterSales.CustomerAllowedGuarantee.Manage', N'گارانتی مجاز مشتریان - مدیریت (HTS 48/69)'),
        (500025, N'Sale.AfterSales.CustomerAllowedGuarantee.View', N'گارانتی مجاز مشتریان - مشاهده (HTS 300/350)'),
        (500026, N'Sale.AfterSales.ResponsibleZone.Manage', N'مسئولین مناطق - مدیریت (HTS 207)'),
        (500027, N'Sale.AfterSales.ResponsibleZone.View', N'مسئولین مناطق - مشاهده (HTS 350)'),
        (500028, N'Sale.AfterSales.CustomerAddress.Manage', N'سایت های مشتریان - مدیریت (HTS 234)'),
        (500029, N'Sale.AfterSales.CustomerAddress.View', N'سایت های مشتریان - مشاهده (HTS 48/350)'),
        (500030, N'Sale.AfterSales.CustomerRequest.Manage', N'درخواست مشتریان - مدیریت (HTS 285)'),
        (500031, N'Sale.AfterSales.CustomerRequest.View', N'درخواست مشتریان - مشاهده (HTS 350/516)');

    DECLARE @Rid BIGINT, @RName NVARCHAR(200), @RTitle NVARCHAR(200);
    DECLARE rc CURSOR LOCAL FAST_FORWARD FOR SELECT WantId, Name, Title FROM #AsRoles;
    OPEN rc; FETCH NEXT FROM rc INTO @Rid, @RName, @RTitle;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = @RName)
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Id = @Rid)
            BEGIN
                SET IDENTITY_INSERT system.Role ON;
                INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
                    CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
                VALUES (@Rid, @RName, @RTitle, 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
                SET IDENTITY_INSERT system.Role OFF;
                PRINT N'  CREATED ' + @RName;
            END
            ELSE
            BEGIN
                INSERT INTO system.Role (Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
                    CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
                VALUES (@RName, @RTitle, 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
                PRINT N'  CREATED (new id) ' + @RName;
            END
        END
        ELSE PRINT N'  EXISTS ' + @RName;
        FETCH NEXT FROM rc INTO @Rid, @RName, @RTitle;
    END
    CLOSE rc; DEALLOCATE rc;

    DECLARE @IdAfterSales BIGINT = (SELECT Id FROM system.Role WHERE Name = N'Sale.AfterSales');
    DECLARE @IdRepairs BIGINT = (SELECT Id FROM system.Role WHERE Name = N'Rpr.Repairs');
    DECLARE @IdSurvey BIGINT = (SELECT Id FROM system.Role WHERE Name = N'Crm.AfterSalesSurvey');
    IF @IdAfterSales IS NULL OR @IdRepairs IS NULL OR @IdSurvey IS NULL
        THROW 51020, N'نقش‌های خدمات پس از فروش ساخته نشدند.', 1;

    PRINT N'=== [2] SavedQuery DataProfiles ===';
    IF OBJECT_ID('tempdb..#AsProfiles') IS NOT NULL DROP TABLE #AsProfiles;
    CREATE TABLE #AsProfiles (
        Name NVARCHAR(100) NOT NULL PRIMARY KEY,
        Title NVARCHAR(200) NOT NULL,
        EntityLower NVARCHAR(200) NOT NULL,
        EntityPascal NVARCHAR(200) NOT NULL,
        CustomQuery NVARCHAR(MAX) NOT NULL,
        ColumnsJson NVARCHAR(MAX) NOT NULL,
        ActionOptions NVARCHAR(MAX) NOT NULL,
        CustomButtons NVARCHAR(MAX) NOT NULL,
        SavedQueryId BIGINT NULL
    );
""")
    for p in profiles:
        a(
            "INSERT INTO #AsProfiles (Name, Title, EntityLower, EntityPascal, CustomQuery, ColumnsJson, ActionOptions, CustomButtons) VALUES ({}, {}, {}, {}, {}, {}, {}, {});".format(
                sql_lit(p["name"]), sql_lit(p["title"]), sql_lit(p["entity_lower"]), sql_lit(p["entity_pascal"]),
                sql_lit(p["query"]), sql_lit(p["columns_json"]), sql_lit(p["actions"]), sql_lit(p["buttons"]),
            )
        )

    a("""
    DECLARE @PName NVARCHAR(100), @PTitle NVARCHAR(200), @PEntLo NVARCHAR(200), @PEntPa NVARCHAR(200);
    DECLARE @PQuery NVARCHAR(MAX), @PCols NVARCHAR(MAX), @PAct NVARCHAR(MAX), @PBtn NVARCHAR(MAX), @PId BIGINT;
    DECLARE @QJson NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}';
    DECLARE pc CURSOR LOCAL FAST_FORWARD FOR
        SELECT Name, Title, EntityLower, EntityPascal, CustomQuery, ColumnsJson, ActionOptions, CustomButtons FROM #AsProfiles;
    OPEN pc;
    FETCH NEXT FROM pc INTO @PName, @PTitle, @PEntLo, @PEntPa, @PQuery, @PCols, @PAct, @PBtn;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        DECLARE @PQueryJson NVARCHAR(MAX) = JSON_MODIFY(@QJson, '$.CustomQuery', @PQuery);
        IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @PName)
        BEGIN
            UPDATE system.SavedQuery
            SET Title = @PTitle, QueryJson = @PQueryJson, ColumnsJson = @PCols, EntityFullName = @PEntLo,
                Mode = 1, Type = 1, ActionOptions = @PAct, CustomActionButtonsJson = @PBtn, DiagramJson = N'{}',
                ModifiedById = 1, ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi, IsActive = 1
            WHERE Name = @PName;
            SELECT @PId = Id FROM system.SavedQuery WHERE Name = @PName;
            PRINT N'  UPDATED ' + @PName + N' Id=' + CAST(@PId AS nvarchar(20));
        END
        ELSE
        BEGIN
            INSERT INTO system.SavedQuery
                (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
                 Mode, Type, EntityFullName, ActionOptions,
                 CreatedById, ModifiedById, CreatedByName, ModifiedByName,
                 CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
            VALUES
                (@PName, @PTitle, @PQueryJson, @PCols, N'{}', @PBtn, NULL,
                 1, 1, @PEntLo, @PAct,
                 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
            SET @PId = SCOPE_IDENTITY();
            PRINT N'  CREATED ' + @PName + N' Id=' + CAST(@PId AS nvarchar(20));
        END
        UPDATE #AsProfiles SET SavedQueryId = @PId WHERE Name = @PName;
        SET @PId = NULL;
        FETCH NEXT FROM pc INTO @PName, @PTitle, @PEntLo, @PEntPa, @PQuery, @PCols, @PAct, @PBtn;
    END
    CLOSE pc; DEALLOCATE pc;

    PRINT N'=== [2b] TenderManagement EventScriptsJson.onRowAdded (confirm cell color) ===';
    DECLARE @TenderOnRowAdded NVARCHAR(MAX) = N'function(ctx) {
    var data = ctx.rowData || {};
    var $cell = ctx.$row.find(''td'').eq(1);
    var secondary = data.t1_HasSecondaryConfirm === true || data.t1_HasSecondaryConfirm === 1 || data.t1_HasSecondaryConfirm === ''true'';
    var primitive = data.t1_HasPrimitiveConfirm === true || data.t1_HasPrimitiveConfirm === 1 || data.t1_HasPrimitiveConfirm === ''true'';
    if (secondary) $cell.css(''background-color'', ''green'');
    else if (primitive) $cell.css(''background-color'', ''yellow'');
    else $cell.css(''background-color'', ''red'');
}';
    DECLARE @TenderEventScripts NVARCHAR(MAX) = (
        SELECT
            CAST(N'' AS nvarchar(max)) AS onSelectedRow,
            @TenderOnRowAdded AS onRowAdded
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    );
    UPDATE system.SavedQuery
    SET EventScriptsJson = @TenderEventScripts,
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Name IN (N'AfterSales_TenderManagement_List', N'AfterSales_TenderManagement_ApproveQueue');
    PRINT N'  Updated EventScriptsJson for TenderManagement profiles: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    PRINT N'=== [3] RoleAccess DataProfile ===';
    DELETE ra FROM system.RoleAccess ra
    INNER JOIN #AsProfiles p ON p.SavedQueryId = ra.RowId
    WHERE ra.ActionAccessType = 3;
    PRINT N'  Cleared prior profile RoleAccess: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    IF OBJECT_ID('tempdb..#AsProfileRole') IS NOT NULL DROP TABLE #AsProfileRole;
    CREATE TABLE #AsProfileRole (ProfileName NVARCHAR(100) NOT NULL, RoleName NVARCHAR(200) NOT NULL);

    INSERT INTO #AsProfileRole (ProfileName, RoleName)
    SELECT p.Name, r.Name
    FROM #AsProfiles p
    CROSS JOIN (VALUES (N'Sale.AfterSales'), (N'ShowAllMenus')) r(Name)
    WHERE p.EntityPascal NOT LIKE N'Entities.App.Rpr.%'
      AND p.EntityPascal NOT LIKE N'Entities.App.Crm.CustomerSatisfactionSurvey'
      AND p.Name <> N'AfterSales_CustomerAddress_List';

    INSERT INTO #AsProfileRole (ProfileName, RoleName)
    SELECT p.Name, r.Name
    FROM #AsProfiles p
    CROSS JOIN (VALUES
        (N'ShowAllMenus'),
        (N'Sale.AfterSales.CustomerAddress.Manage'),
        (N'Sale.AfterSales.CustomerAddress.View')
    ) r(Name)
    WHERE p.Name = N'AfterSales_CustomerAddress_List';

    INSERT INTO #AsProfileRole (ProfileName, RoleName)
    SELECT p.Name, r.Name
    FROM #AsProfiles p
    CROSS JOIN (VALUES
        (N'Sale.AfterSales.CustomerAllowedGuarantee.Manage'),
        (N'Sale.AfterSales.CustomerAllowedGuarantee.View')
    ) r(Name)
    WHERE p.Name = N'AfterSales_CustomerAllowedGuarantee_List';

    INSERT INTO #AsProfileRole (ProfileName, RoleName)
    SELECT p.Name, r.Name
    FROM #AsProfiles p
    CROSS JOIN (VALUES
        (N'Sale.AfterSales.ResponsibleZone.Manage'),
        (N'Sale.AfterSales.ResponsibleZone.View')
    ) r(Name)
    WHERE p.Name IN (N'AfterSales_ResponsibleZone_List', N'AfterSales_ResponsibleZoneCustomer_List');

    INSERT INTO #AsProfileRole (ProfileName, RoleName)
    SELECT p.Name, r.Name
    FROM #AsProfiles p
    CROSS JOIN (VALUES
        (N'Sale.Tenders.ShowAll'), (N'Sale.Tenders.CompressedAir'), (N'Sale.Tenders.OilGas'),
        (N'Sale.Tenders.Manager'), (N'Sale.Tenders.View'), (N'Sale.Tenders.Approver')
    ) r(Name)
    WHERE p.Name = N'AfterSales_TenderManagement_List';

    INSERT INTO #AsProfileRole (ProfileName, RoleName)
    SELECT p.Name, r.Name
    FROM #AsProfiles p
    CROSS JOIN (VALUES
        (N'Sale.Tenders.Approver'), (N'Sale.Tenders.Manager'), (N'Sale.Tenders.View')
    ) r(Name)
    WHERE p.Name = N'AfterSales_TenderManagement_ApproveQueue';

    INSERT INTO #AsProfileRole (ProfileName, RoleName)
    SELECT p.Name, r.Name
    FROM #AsProfiles p
    CROSS JOIN (VALUES
        (N'Sale.Tenders.ShowAll'), (N'Sale.Tenders.CompressedAir'), (N'Sale.Tenders.OilGas'),
        (N'Sale.Tenders.Manager'), (N'Sale.Tenders.View'), (N'Sale.Tenders.Approver')
    ) r(Name)
    WHERE p.Name IN (
        N'AfterSales_TenderPart_List', N'AfterSales_TenderTask_List',
        N'AfterSales_TenderComment_List', N'AfterSales_TenderAttachment_List',
        N'AfterSales_TenderProductGroup_List');

    INSERT INTO #AsProfileRole (ProfileName, RoleName)
    SELECT p.Name, r.Name
    FROM #AsProfiles p
    CROSS JOIN (VALUES (N'Rpr.Repairs'), (N'Sale.AfterSales'), (N'ShowAllMenus')) r(Name)
    WHERE p.EntityPascal LIKE N'Entities.App.Rpr.%';

    INSERT INTO #AsProfileRole (ProfileName, RoleName)
    SELECT p.Name, r.Name
    FROM #AsProfiles p
    CROSS JOIN (VALUES (N'Crm.AfterSalesSurvey'), (N'Sale.AfterSales'), (N'ShowAllMenus')) r(Name)
    WHERE p.EntityPascal LIKE N'Entities.App.Crm.CustomerSatisfactionSurvey'
       OR p.Name LIKE N'AfterSales_Survey_%';

    INSERT INTO #AsProfileRole (ProfileName, RoleName)
    SELECT q.Name, r.Name
    FROM system.SavedQuery q
    CROSS JOIN (VALUES (N'Sale.AfterSales'), (N'ShowAllMenus')) r(Name)
    WHERE q.Name IN (N'OrderDetail_WithPrice', N'OrderDetail_NoPrice');

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT
        N'dataProfile_' + CAST(q.Id AS nvarchar(20)),
        3, 7,
        N'Entities.App.Sale.OrderDetailSerial',
        q.Title,
        NULL, q.Id, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.SavedQuery q
    CROSS JOIN (VALUES (N'Sale.AfterSales'), (N'ShowAllMenus')) rmap(Name)
    INNER JOIN system.Role r ON r.Name = rmap.Name
    WHERE q.Name IN (
        N'AfterSales_OrderDetailSerial_Monitoring_Industries',
        N'AfterSales_OrderDetailSerial_Monitoring_Warehouse')
      AND NOT EXISTS (
          SELECT 1 FROM system.RoleAccess ra
          WHERE ra.ActionAccessType = 3 AND ra.RowId = q.Id AND ra.RoleId = r.Id
      );

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT
        N'dataProfile_' + CAST(COALESCE(p.SavedQueryId, q.Id) AS nvarchar(20)),
        3, 7,
        COALESCE(p.EntityPascal, N'Entities.App.Sale.OrderDetail'),
        COALESCE(p.Title, q.Title),
        NULL, COALESCE(p.SavedQueryId, q.Id), r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #AsProfileRole map
    LEFT JOIN #AsProfiles p ON p.Name = map.ProfileName
    LEFT JOIN system.SavedQuery q ON q.Name = map.ProfileName AND p.Name IS NULL
    INNER JOIN system.Role r ON r.Name = map.RoleName
    WHERE COALESCE(p.SavedQueryId, q.Id) IS NOT NULL
      AND NOT EXISTS (
          SELECT 1 FROM system.RoleAccess ra
          WHERE ra.ActionAccessType = 3 AND ra.RowId = COALESCE(p.SavedQueryId, q.Id) AND ra.RoleId = r.Id
      );
    PRINT N'  Inserted DataProfile RoleAccess: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    PRINT N'=== [4] RoleAccess controller actions ===';
    IF OBJECT_ID('tempdb..#AsActionAccess') IS NOT NULL DROP TABLE #AsActionAccess;
    CREATE TABLE #AsActionAccess (
        RoleName NVARCHAR(200) NOT NULL,
        Path NVARCHAR(300) NOT NULL,
        ActionAccessType INT NOT NULL,
        ActionAccessItemType INT NOT NULL,
        EntityName NVARCHAR(200) NULL
    );
""")
    seen = set()
    for act in actions:
        for rn in roles_for_path(act["path"]):
            key = (rn, act["path"], act["access"])
            if key in seen:
                continue
            seen.add(key)
            ent = sql_lit(act["entity"]) if act["entity"] else "NULL"
            a(
                "INSERT INTO #AsActionAccess (RoleName, Path, ActionAccessType, ActionAccessItemType, EntityName) VALUES ({}, {}, {}, {}, {});".format(
                    sql_lit(rn), sql_lit(act["path"]), act["access"], act["item"], ent
                )
            )
    for path, at, it, ent in (
        ("/panel/sale/orderdetail/list", 1, 1, "Entities.App.Sale.OrderDetail"),
        ("/panel/sale/orderdetail/edit", 1, 5, "Entities.App.Sale.OrderDetail"),
        ("/panel/sale/orderdetail/fetchdata", 2, 2, "Entities.App.Sale.OrderDetail"),
        ("/panel/sale/orderdetail/exporttoexcel", 2, 0, "Entities.App.Sale.OrderDetail"),
    ):
        for rn in ("Sale.AfterSales", "ShowAllMenus"):
            a(
                "INSERT INTO #AsActionAccess (RoleName, Path, ActionAccessType, ActionAccessItemType, EntityName) VALUES ({}, {}, {}, {}, {});".format(
                    sql_lit(rn), sql_lit(path), at, it, sql_lit(ent)
                )
            )

    a("""
    DELETE ra FROM system.RoleAccess ra
    INNER JOIN system.Role r ON r.Id = ra.RoleId
    WHERE ra.ActionAccessType IN (1, 2)
      AND r.Name IN (N'Sale.AfterSales', N'Rpr.Repairs', N'Crm.AfterSalesSurvey', N'ShowAllMenus',
                     N'Sale.Tenders.Approver', N'Sale.Tenders.DuplicateNotify', N'Sale.Tenders.Manager',
                     N'Sale.Tenders.ShowAll', N'Sale.Tenders.CompressedAir', N'Sale.Tenders.OilGas',
                     N'Sale.Tenders.View',
                     N'Sale.AfterSales.CustomerAllowedGuarantee.Manage',
                     N'Sale.AfterSales.CustomerAllowedGuarantee.View',
                     N'Sale.AfterSales.ResponsibleZone.Manage',
                     N'Sale.AfterSales.ResponsibleZone.View',
                     N'Sale.AfterSales.CustomerAddress.Manage',
                     N'Sale.AfterSales.CustomerAddress.View')
      AND (
            ra.Path LIKE N'/panel/sale/priceconfig/%'
         OR ra.Path LIKE N'/panel/sale/partprice/%'
         OR ra.Path LIKE N'/panel/sale/webshop%'
         OR ra.Path LIKE N'/panel/sale/tendermanagement/%'
         OR ra.Path LIKE N'/panel/sale/tenderpart/%'
         OR ra.Path LIKE N'/panel/sale/tendertask/%'
         OR ra.Path LIKE N'/panel/sale/tendercomment/%'
         OR ra.Path LIKE N'/panel/sale/tenderattachment/%'
         OR ra.Path LIKE N'/panel/sale/tenderproductgroup/%'
         OR ra.Path LIKE N'/panel/sale/orderpoint/%'
         OR ra.Path LIKE N'/panel/sale/orderpointcomment/%'
         OR ra.Path LIKE N'/panel/sale/customerallowedguarantee/%'
         OR ra.Path LIKE N'/panel/sale/responsiblezone%'
         OR ra.Path LIKE N'/panel/sale/productactivityitem/%'
         OR ra.Path LIKE N'/panel/sale/orderdetailproduct/%'
         OR ra.Path LIKE N'/panel/sale/projectutilizedmaterial/%'
         OR ra.Path LIKE N'/panel/sale/servicerequest%'
         OR ra.Path LIKE N'/panel/sale/mission%'
         OR ra.Path LIKE N'/panel/sale/workreport/%'
         OR ra.Path LIKE N'/panel/sale/collectionclaim%'
         OR ra.Path LIKE N'/panel/sale/technicalquery/%'
         OR ra.Path LIKE N'/panel/sale/technicalquerycomment/%'
         OR ra.Path LIKE N'/panel/sale/agency%'
         OR ra.Path LIKE N'/panel/sale/oilgasworkreport/%'
         OR ra.Path LIKE N'/panel/sale/equipmentmonitoring/%'
         OR ra.Path LIKE N'/panel/sale/aftersalesmobileuser/%'
         OR ra.Path LIKE N'/panel/sale/aftersalesdashboard/%'
         OR ra.Path LIKE N'/panel/sale/orderdetailserial%'
         OR ra.Path LIKE N'/panel/crm/%'
         OR ra.Path LIKE N'/panel/sls/customeraddress/%'
         OR ra.Path LIKE N'/panel/rpr/%'
      );
    PRINT N'  Cleared prior after-sales RoleAccess for dedicated roles: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT a.Path, a.ActionAccessType, a.ActionAccessItemType, a.EntityName, NULL, NULL, NULL, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #AsActionAccess a
    INNER JOIN system.Role r ON r.Name = a.RoleName
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess x
        WHERE x.RoleId = r.Id AND x.Path = a.Path AND x.ActionAccessType = a.ActionAccessType
    );
    PRINT N'  Inserted controller RoleAccess: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    PRINT N'=== [5] Menu AccessRoleIds ===';
    DECLARE @AccessRoles NVARCHAR(MAX);
    DECLARE @AccessRoleIds NVARCHAR(MAX);
    SELECT @AccessRoles = N'[' + STUFF((
        SELECT N',"' + Name + N'"'
        FROM (VALUES (N'Sale.AfterSales'), (N'Rpr.Repairs'), (N'Crm.AfterSalesSurvey'), (N'ShowAllMenus'),
                      (N'Sale.Tenders.Approver'), (N'Sale.Tenders.Manager'), (N'Sale.Tenders.ShowAll'),
                      (N'Sale.Tenders.CompressedAir'), (N'Sale.Tenders.OilGas'), (N'Sale.Tenders.View'),
                      (N'Sale.AfterSales.CustomerAllowedGuarantee.Manage'),
                      (N'Sale.AfterSales.CustomerAllowedGuarantee.View'),
                      (N'Sale.AfterSales.ResponsibleZone.Manage'),
                      (N'Sale.AfterSales.ResponsibleZone.View'),
                      (N'Sale.AfterSales.CustomerAddress.Manage'),
                      (N'Sale.AfterSales.CustomerAddress.View')) x(Name)
        FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N'') + N']';
    SELECT @AccessRoleIds = N'[' + STUFF((
        SELECT N',' + CAST(Id AS nvarchar(20))
        FROM system.Role
        WHERE Name IN (N'Sale.AfterSales', N'Rpr.Repairs', N'Crm.AfterSalesSurvey', N'ShowAllMenus',
                        N'Sale.Tenders.Approver', N'Sale.Tenders.Manager', N'Sale.Tenders.ShowAll',
                        N'Sale.Tenders.CompressedAir', N'Sale.Tenders.OilGas', N'Sale.Tenders.View',
                        N'Sale.AfterSales.CustomerAllowedGuarantee.Manage',
                        N'Sale.AfterSales.CustomerAllowedGuarantee.View',
                        N'Sale.AfterSales.ResponsibleZone.Manage',
                        N'Sale.AfterSales.ResponsibleZone.View',
                        N'Sale.AfterSales.CustomerAddress.Manage',
                        N'Sale.AfterSales.CustomerAddress.View')
        FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N'') + N']';

    UPDATE system.SystemMenu
    SET AccessRoles = @AccessRoles,
        AccessRoleIds = @AccessRoleIds,
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Name = N'AfterSalesServiceSystem';
    PRINT N'  Menu rows updated: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE AfterSales DataProfiles + RoleAccess ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

PRINT N'--- verify ---';
SELECT Name, Title, EntityFullName, Id FROM system.SavedQuery
WHERE Name LIKE N'AfterSales_%' OR Name IN (N'OrderDetail_WithPrice', N'OrderDetail_NoPrice')
ORDER BY Name;

SELECT Name, LEFT(EventScriptsJson, 400) AS EventScriptsJson
FROM system.SavedQuery
WHERE Name IN (N'AfterSales_TenderManagement_List', N'AfterSales_TenderManagement_ApproveQueue');

SELECT r.Name AS RoleName, COUNT(*) AS AccessRows
FROM system.RoleAccess ra
INNER JOIN system.Role r ON r.Id = ra.RoleId
WHERE r.Name IN (N'Sale.AfterSales', N'Rpr.Repairs', N'Crm.AfterSalesSurvey', N'ShowAllMenus')
  AND (
        ra.Path LIKE N'/panel/sale/%' OR ra.Path LIKE N'/panel/rpr/%'
     OR ra.Path LIKE N'/panel/crm/%' OR ra.Path LIKE N'/panel/sls/customeraddress/%'
     OR ra.Path LIKE N'dataProfile_%'
  )
GROUP BY r.Name;

SELECT Name, AccessRoles, AccessRoleIds FROM system.SystemMenu WHERE Name = N'AfterSalesServiceSystem';
""")
    return "\n".join(lines)


def add_profile(profiles, name, title, ent, built, actions, kind):
    if not built:
        return
    profiles.append({
        "name": name, "title": title,
        "entity_lower": ent["full"].lower(), "entity_pascal": ent["full"],
        "query": built["select"], "columns_json": col_json(built["columns"]),
        "actions": actions, "buttons": action_buttons(ent["class"], kind),
        "class": ent["class"],
    })


def main():
    if not COLS_JSON.exists():
        raise SystemExit(f"Missing {COLS_JSON} — run _DumpHavayarColumns.ps1 first")
    db = DbCols(json.loads(COLS_JSON.read_text(encoding="utf-8")))
    enums = load_enums()
    entity_by = load_entities()
    print(f"Parsed {len(entity_by)} entities, {len(enums)} enums, {len(db.tables)} tables")
    list_names = discover_list_entities()
    print(f"List pages: {sorted(list_names)}")

    profiles: list[dict] = []
    for cls in sorted(list_names):
        ent = entity_by.get(cls)
        if not ent:
            print(f"WARN no metadata for {cls}")
            continue
        built = build_profile(ent, db, enums, None)
        if cls == "CustomerAllowedGuarantee" and built:
            built = tweak_customer_allowed_guarantee(built)
        if cls == "ResponsibleZone" and built:
            built = tweak_responsible_zone(built)
        if cls == "ResponsibleZoneCustomer" and built:
            built = tweak_responsible_zone_customer(built)
        if cls == "CustomerAddress" and built:
            built = tweak_customer_address(built)
            add_profile(profiles, f"AfterSales_{cls}_List", "سایت های مشتریان", ent, built, READONLY_ACTIONS, "List")
            continue
        if cls == "ProjectUtilizedMaterial":
            add_profile(profiles, f"AfterSales_{cls}_List", "اقلام مصرفی در پروژه", ent, built, READONLY_ACTIONS, "List")
            continue
        add_profile(profiles, f"AfterSales_{cls}_List", ent["display"], ent, built, CRUD_ACTIONS, "List")

    # AfterSales_ServiceRequest_Cartable is owned by Seed_SaleServiceRequest_DataProfiles.sql
    # (detail-grain, HTS page 158). Do not emit a header-level cartable here.

    rr = entity_by.get("RepairRequest")
    if rr:
        built = build_profile(rr, db, enums, "WHERE [t1].[HasDelay] = 1")
        add_profile(profiles, "AfterSales_RepairRequest_Delay", "تاخیرات تعمیرات", rr, built, READONLY_ACTIONS, "Delay")
    tm = entity_by.get("TenderManagement")
    if tm:
        built = build_profile(tm, db, enums,
            "WHERE ISNULL([t1].[InCartable], 0) = 0 AND ISNULL([t1].[HasSecondaryConfirm], 0) <> 1")
        add_profile(profiles, "AfterSales_TenderManagement_ApproveQueue", "بررسی و تایید مناقصات",
                    tm, built, READONLY_ACTIONS, "Approve")
    tq = entity_by.get("TechnicalQuery")
    if tq:
        built = build_profile(tq, db, enums, "WHERE [t1].[CartableStatusId] IS NOT NULL")
        add_profile(profiles, "AfterSales_TechnicalQuery_ApproveQueue", "بررسی و تایید پرسش فنی",
                    tq, built, READONLY_ACTIONS, "Approve")
    css = entity_by.get("CustomerSatisfactionSurvey")
    if css:
        for name, title, where in (
            ("AfterSales_Survey_AfterSales", "رضایت‌سنجی بعد از فروش", "WHERE [t1].[TypeId] = 1008"),
            ("AfterSales_Survey_Cng", "رضایت‌سنجی بعد از فروش CNG", "WHERE [t1].[TypeId] = 1942"),
            ("AfterSales_Survey_CompressedAir", "رضایت‌سنجی خدمات هوای فشرده", "WHERE [t1].[TypeId] = 2332"),
        ):
            built = build_profile(css, db, enums, where)
            add_profile(profiles, name, title, css, built, CRUD_ACTIONS, "List")

    print(f"Built {len(profiles)} profiles")
    actions = parse_controllers(entity_by)
    print(f"Controller actions: {len(actions)}")
    sql = emit_sql(profiles, actions)
    OUT_SQL.write_text(sql, encoding="utf-8-sig")
    print(f"Wrote {OUT_SQL} ({OUT_SQL.stat().st_size // 1024} KB)")
    # sidecar summary
    summary = {
        "profiles": [{"name": p["name"], "title": p["title"], "entity": p["entity_pascal"]} for p in profiles],
        "action_count": len(actions),
        "list_entities": sorted(list_names),
    }
    Path(__file__).with_name("_aftersales_seed_summary.json").write_text(
        json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8"
    )


if __name__ == "__main__":
    main()
