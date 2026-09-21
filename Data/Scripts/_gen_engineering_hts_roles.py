# -*- coding: utf-8 -*-
"""Emit Seed_Engineering_HtsGroups_RolesAndUsers.sql (UTF-8 BOM)."""
from pathlib import Path

OUT = Path(__file__).with_name("Seed_Engineering_HtsGroups_RolesAndUsers.sql")
OUT_LEFTOVER = Path(__file__).with_name("Seed_Engineering_HtsLeftover_DirectAndFixes.sql")


def esc(s: str) -> str:
    return s.replace("'", "''")


def crud(prefix: str, entity: str, *, include_delete: bool = True, extra=None):
    rows = [
        (f"{prefix}/list", 1, 1, entity, "read"),
        (f"{prefix}/edit", 1, 5, entity, "read"),
        (f"{prefix}/fetchdata", 2, 2, entity, "read"),
        (f"{prefix}/exporttoexcel", 2, 0, entity, "export"),
        (f"{prefix}/new", 1, 4, entity, "write"),
        (f"{prefix}/add", 2, 4, entity, "write"),
        (f"{prefix}/save", 2, 3, entity, "write"),
        (f"{prefix}/update", 2, 5, entity, "write"),
    ]
    if include_delete:
        rows.append((f"{prefix}/delete", 2, 6, entity, "delete"))
    if extra:
        rows.extend(extra)
    return rows


EDMS_DOC = "Entities.App.Edms.Document"
EDMS_PRJ = "Entities.App.Edms.Project"
EDMS_VPIS = "Entities.App.Edms.ProjectVpis"
EDMS_TR = "Entities.App.Edms.Transmital"
EDMS_ACT = "Entities.App.Edms.ProjectActivity"
EPMS_PRO = "Entities.App.Epms.Proposal"
EPMS_ACT = "Entities.App.Epms.ProposalActivity"
EPMS_VPIS = "Entities.App.Epms.ProposalVpis"
EPMS_EST = "Entities.App.Epms.EquipmentPriceEstimate"
BOM_FG = "Entities.App.Bom.FormulGroup"
BOM_F = "Entities.App.Bom.Formul"
BOM_PF = "Entities.App.Bom.ProductFormul"
BOM_PG = "Entities.App.Bom.ProductGroup"
BOM_CR = "Entities.App.Bom.FormulChangeRequest"
BOM_PC = "Entities.App.Bom.ProductPriceCompare"
ENG_PL = "Entities.App.Eng.PartList"
ENG_PLG = "Entities.App.Eng.PartListGroup"
ENG_PUM = "Entities.App.Eng.PartList"
INV_PART = "Entities.App.Inv.Part"
INV_PEI = "Entities.App.Inv.PartExtraInfo"

# page_id -> path rows
PAGE_PATHS: dict[int, list] = {}

PAGE_PATHS[163] = crud("/panel/edms/project", EDMS_PRJ, extra=[
    ("/panel/edms/project/projectattachmentlist", 1, 1, EDMS_PRJ, "read"),
])
PAGE_PATHS[165] = crud("/panel/edms/projectvpis", EDMS_VPIS)
PAGE_PATHS[166] = crud("/panel/edms/document", EDMS_DOC, extra=[
    ("/panel/edms/document/documentproductfetchdata", 2, 2, EDMS_DOC, "read"),
    ("/panel/edms/document/editasgroup", 1, 5, EDMS_DOC, "write"),
    ("/panel/edms/document/save/{caneditdocument?}", 2, 3, EDMS_DOC, "write"),
])
PAGE_PATHS[168] = [
    ("/panel/edms/document/edit", 1, 5, EDMS_DOC, "read"),
    ("/panel/edms/document/fetchdata", 2, 2, EDMS_DOC, "read"),
]
PAGE_PATHS[169] = crud("/panel/edms/transmital", EDMS_TR)
PAGE_PATHS[218] = [
    ("/panel/edms/project/projectattachmentlist", 1, 1, EDMS_PRJ, "read"),
    ("/panel/edms/project/edit", 1, 5, EDMS_PRJ, "read"),
    ("/panel/edms/project/fetchdata", 2, 2, EDMS_PRJ, "read"),
]
PAGE_PATHS[220] = [
    ("/panel/edms/transmital/edit", 1, 5, EDMS_TR, "read"),
    ("/panel/edms/transmital/list", 1, 1, EDMS_TR, "read"),
    ("/panel/edms/transmital/fetchdata", 2, 2, EDMS_TR, "read"),
]
PAGE_PATHS[221] = [
    ("/panel/edms/document/list", 1, 1, EDMS_DOC, "read"),
    ("/panel/edms/document/edit", 1, 5, EDMS_DOC, "read"),
    ("/panel/edms/document/fetchdata", 2, 2, EDMS_DOC, "read"),
]
# Copy, do not alias: group 363 is Excel-only on control documents.
PAGE_PATHS[223] = list(PAGE_PATHS[221]) + [
    ("/panel/edms/document/exporttoexcel", 2, 0, EDMS_DOC, "export"),
]
PAGE_PATHS[232] = PAGE_PATHS[221]
PAGE_PATHS[233] = PAGE_PATHS[221]
PAGE_PATHS[235] = [
    ("/panel/edms/document/mdrreport", 1, 1, EDMS_DOC, "read"),
    ("/panel/edms/document/getmdrreport", 2, 2, EDMS_DOC, "read"),
    ("/panel/edms/document/getmdrreportpost", 2, 2, EDMS_DOC, "read"),
    ("/panel/edms/document/exportmdrtoexcel", 2, 0, EDMS_DOC, "export"),
]
PAGE_PATHS[238] = PAGE_PATHS[221]
PAGE_PATHS[248] = PAGE_PATHS[221]
PAGE_PATHS[275] = PAGE_PATHS[221]
PAGE_PATHS[279] = PAGE_PATHS[218]
PAGE_PATHS[288] = crud("/panel/edms/projectactivity", EDMS_ACT)
PAGE_PATHS[361] = [
    ("/panel/edms/document/personnelworkloadreport", 1, 1, EDMS_DOC, "read"),
    ("/panel/edms/document/getpersonnelworkloadreport", 2, 2, EDMS_DOC, "read"),
]
PAGE_PATHS[532] = PAGE_PATHS[221]
PAGE_PATHS[543] = [
    ("/panel/edms/document/edit", 1, 5, EDMS_DOC, "read"),
    ("/panel/edms/document/list", 1, 1, EDMS_DOC, "read"),
    ("/panel/edms/document/fetchdata", 2, 2, EDMS_DOC, "read"),
    ("/panel/edms/document/update", 2, 5, EDMS_DOC, "write"),
    ("/panel/edms/document/save", 2, 3, EDMS_DOC, "write"),
]

PAGE_PATHS[252] = crud("/panel/epms/proposal", EPMS_PRO)
PAGE_PATHS[253] = [
    ("/panel/epms/proposal/edit", 1, 5, EPMS_PRO, "read"),
    ("/panel/epms/proposal/list", 1, 1, EPMS_PRO, "read"),
    ("/panel/epms/proposal/fetchdata", 2, 2, EPMS_PRO, "read"),
]
PAGE_PATHS[254] = crud("/panel/epms/proposalvpis", EPMS_VPIS)
PAGE_PATHS[255] = PAGE_PATHS[221]
PAGE_PATHS[256] = PAGE_PATHS[168]
PAGE_PATHS[257] = PAGE_PATHS[221]
PAGE_PATHS[260] = PAGE_PATHS[221]
PAGE_PATHS[261] = crud("/panel/epms/equipmentpriceestimate", EPMS_EST)
PAGE_PATHS[264] = PAGE_PATHS[221]
PAGE_PATHS[267] = crud("/panel/epms/equipmentpriceestimate", EPMS_EST)
PAGE_PATHS[272] = crud("/panel/epms/equipmentpriceestimate", EPMS_EST)
PAGE_PATHS[273] = crud("/panel/epms/equipmentpriceestimate", EPMS_EST)
PAGE_PATHS[289] = PAGE_PATHS[221]
PAGE_PATHS[294] = crud("/panel/epms/proposalactivity", EPMS_ACT)
PAGE_PATHS[350] = PAGE_PATHS[221]

PAGE_PATHS[95] = crud("/panel/bom/formulgroup", BOM_FG)
PAGE_PATHS[96] = crud("/panel/bom/formul", BOM_F)
PAGE_PATHS[97] = crud("/panel/bom/formul", BOM_F)
PAGE_PATHS[98] = crud("/panel/bom/productformul", BOM_PF)
PAGE_PATHS[99] = crud("/panel/bom/productformul", BOM_PF)
PAGE_PATHS[100] = [
    ("/panel/bom/productformul/edit", 1, 5, BOM_PF, "read"),
    ("/panel/bom/productformul/list", 1, 1, BOM_PF, "read"),
    ("/panel/bom/productformul/fetchdata", 2, 2, BOM_PF, "read"),
]
PAGE_PATHS[101] = PAGE_PATHS[98]
PAGE_PATHS[102] = PAGE_PATHS[96]
PAGE_PATHS[103] = crud("/panel/bom/productgroup", BOM_PG)
PAGE_PATHS[106] = crud("/panel/inv/part", INV_PART, include_delete=False)
PAGE_PATHS[107] = crud("/panel/bom/productformul", BOM_PF)
PAGE_PATHS[108] = PAGE_PATHS[98]
PAGE_PATHS[109] = crud("/panel/inv/partextrainfo", INV_PEI)
PAGE_PATHS[110] = [
    ("/panel/inv/partextrainfo/list", 1, 1, INV_PEI, "read"),
    ("/panel/inv/partextrainfo/edit", 1, 5, INV_PEI, "read"),
    ("/panel/inv/partextrainfo/fetchdata", 2, 2, INV_PEI, "read"),
    ("/panel/inv/partextrainfo/exporttoexcel", 2, 0, INV_PEI, "export"),
]
PAGE_PATHS[116] = [
    ("/panel/bom/productpricecompare/list", 1, 1, BOM_PC, "read"),
    ("/panel/bom/productpricecompare/fetchdata", 2, 2, BOM_PC, "read"),
    ("/panel/bom/productpricecompare/details", 1, 1, BOM_PC, "read"),
    ("/panel/bom/productpricecompare/availabledates", 2, 2, BOM_PC, "read"),
    ("/panel/bom/productpricecompare/sendemail", 2, 1000, BOM_PC, "write"),
]
PAGE_PATHS[125] = PAGE_PATHS[106]
PAGE_PATHS[293] = PAGE_PATHS[109]
PAGE_PATHS[299] = crud("/panel/bom/formulchangerequest", BOM_CR, extra=[
    ("/panel/inv/partcodingcategory/index", 1, 1, INV_PART, "read"),
    ("/panel/inv/partcodingcategory/listrequest", 1, 1, INV_PART, "read"),
    ("/panel/inv/partcodingcategory/requestnewpart", 1, 4, INV_PART, "write"),
    ("/panel/inv/partcodingcategory/fetchpartsgriddata", 2, 2, INV_PART, "read"),
    ("/panel/inv/partcodingcategory/gettreedata", 2, 2, INV_PART, "read"),
    ("/panel/inv/partcodingcategory/getpartcodeprefix", 2, 1000, INV_PART, "read"),
    ("/panel/inv/partcodingcategory/applycreationpart", 2, 1000, INV_PART, "write"),
])
PAGE_PATHS[377] = PAGE_PATHS[109]
PAGE_PATHS[526] = PAGE_PATHS[116]
PAGE_PATHS[541] = PAGE_PATHS[109]
PAGE_PATHS[561] = [
    ("/panel/bom/formul/list", 1, 1, BOM_F, "read"),
    ("/panel/bom/formul/fetchdata", 2, 2, BOM_F, "read"),
    ("/panel/bom/formul/exporttoexcel", 2, 0, BOM_F, "export"),
]
PAGE_PATHS[563] = PAGE_PATHS[109]

PAGE_PATHS[495] = crud("/panel/eng/partlistgroup", ENG_PLG)
PAGE_PATHS[496] = crud("/panel/eng/partlist", ENG_PL)
PAGE_PATHS[497] = [
    ("/panel/eng/projectutilizedmaterial/list", 1, 1, ENG_PUM, "read"),
    ("/panel/eng/projectutilizedmaterial/edit", 1, 5, ENG_PUM, "read"),
    ("/panel/eng/projectutilizedmaterial/new", 1, 4, ENG_PUM, "write"),
    ("/panel/eng/projectutilizedmaterial/add", 2, 4, ENG_PUM, "write"),
    ("/panel/eng/projectutilizedmaterial/save", 2, 3, ENG_PUM, "write"),
    ("/panel/eng/projectutilizedmaterial/update", 2, 5, ENG_PUM, "write"),
    ("/panel/eng/projectutilizedmaterial/delete", 2, 6, ENG_PUM, "delete"),
]

PAGE_PATHS[449] = [
    ("/panel/eng/compressorsizing/calculator", 1, 0, None, "read"),
    ("/panel/eng/compressorsizing/reportviewer", 1, 0, None, "read"),
    ("/panel/eng/compressorsizing/getviewreport", 2, 2, None, "read"),
    ("/panel/eng/compressorsizing/viewerevent", 2, 2, None, "read"),
    ("/panel/eng/compressorsizing/selectbyefficiency", 2, 1000, None, "fullonly"),
    ("/panel/eng/compressorsizing/selectbyreliability", 2, 1000, None, "fullonly"),
    ("/panel/eng/compressorsizing/analyze", 2, 1000, None, "fullonly"),
    ("/panel/eng/compressorsizing/exportdatasheet", 2, 1000, None, "fullonly"),
]

# Data profiles on document form / related lists. flag: read | price
PAGE_PROFILES: dict[int, list[tuple[str, str]]] = {
    163: [("vw_ProjectInfo", "read")],
    165: [("vw_ProjectVpisManagment", "read")],
    166: [("vw_DocumentAllProjectInfo", "read"), ("vw_DocumentAllProjectInfonew", "write")],
    169: [("vw_TransmitalInfo", "read")],
    218: [("projectattachment_listinfo", "read")],
    221: [("vw_DocumentDCC", "read"), ("vw_DocumentReciveAndSend", "read")],
    223: [("vw_DocumentMyActions", "read")],
    232: [("vw_DocumentAllProjectInfo", "read")],
    233: [("vw_DocumentMyArchive", "read")],
    238: [("vw_DocumentAllProjectInfo", "read")],
    248: [("vw_DocumentAllProjectInfo", "read")],
    275: [("vw_DocumentMyCartabl", "read")],
    279: [("projectattachment_listinfo", "read")],
    288: [("projectactivity_listinfo", "read")],
    532: [("vw_DocumentAllProjectInfo", "read")],
    543: [("vw_DocumentAllProjectInfo", "read")],
    255: [("vw_DocumentMyCartabl", "read"), ("vw_DocumentAllProjectInfo", "read")],
    257: [("vw_DocumentMyActions", "read")],
    260: [("vw_DocumentReciveAndSend", "read"), ("vw_DocumentAllProjectInfo", "read")],
    264: [("vw_DocumentAllProjectInfo", "read")],
    289: [("vw_DocumentMyArchive", "read")],
    350: [("vw_DocumentMyCartabl", "read")],
    95: [("formulgroup_listinfo", "read")],
    96: [("formul_listinfo", "read"), ("vw_formulwithItems", "read")],
    97: [("vw_formulwithItems", "read")],
    98: [("productformul_listinfo", "read"), ("vw_productformulwithBom", "read"), ("vw_productformulwithBomWithPrice", "price")],
    99: [("vw_productformulwithBom", "read"), ("vw_productformulwithBomWithPrice", "price")],
    107: [("productformul_listinfo", "read"), ("vw_productformulwithBom", "read"), ("vw_productformulwithBomWithPrice", "price")],
    108: [("vw_productformulwithBom", "read"), ("vw_productformulwithBomWithPrice", "price")],
    103: [("productgroup_listinfo", "read")],
    106: [("part_listinfo", "read")],
    109: [("partextrainfo_listinfo", "read"), ("vw_PartextrainfoOilInject", "read")],
    110: [("partextrainfo_listinfo", "read"), ("vw_PartextrainfoOilInject", "read")],
    116: [("vw_productformulwithBomWithPrice", "price")],
    125: [("part_listinfo", "read")],
    293: [("partextrainfo_listinfo", "read"), ("vw_PartextrainfoDerayer", "read")],
    299: [("formulchangerequest_listinfo", "read")],
    377: [("partextrainfo_listinfo", "read"), ("vw_PartextrainfoPsa", "read")],
    526: [("vw_productformulwithBomWithPrice", "price")],
    541: [("partextrainfo_listinfo", "read"), ("vw_PartextrainfoOilFree", "read")],
    561: [("vw_formulwithItems", "read")],
    563: [("partextrainfo_listinfo", "read"), ("vw_PartextrainfoStatic", "read")],
    252: [("proposal_listinfo", "read")],
    294: [("proposalactivity_listinfo", "read")],
}

SKIPPED_PAGES = {
    164: "اقلام پروژه EDMS — کنترلر جدا در هاویار نیست",
    558: "اقلام پروژه 558 — کنترلر جدا نیست",
    245: "گزارش عملکرد پرسنل EDMS — صفحه هاویار نیست",
    246: "گزارش پیشرفت پروژه — صفحه هاویار نیست",
    282: "گزارش تجمعی پروژه — صفحه هاویار نیست",
    291: "گزارش تجمعی عملکرد — صفحه هاویار نیست",
    306: "برنامه زمانی EDMS — منوی مرده / صفحه نیست",
    380: "گزارش شاخص ارزیابی — صفحه هاویار نیست",
    249: "مناقصه EPMS — منوی مرده / صفحه هاویار نیست",
    250: "پیوست مناقصه EPMS — صفحه نیست",
    265: "استعلام قیمت تجهیزات پروپوزال — کنترلر نیست",
    268: "آرشیو متره — منوی مرده",
    274: "آرشیو مدارک مالی — منوی مرده",
    325: "آرشیو اسناد مناقصه EPMS — منوی مرده",
    351: "گزارش عملکرد پرسنل EPMS — صفحه نیست",
    236: "سایزینگ درایر خاص — فقط CompressorSizing پیاده شده",
    239: "سایزینگ کمپرسور قدیمی — صفحه 449 جایگزین",
    240: "سایزینگ تله آبگیر — کنترلر نیست",
    242: "سایزینگ ACT — کنترلر نیست",
    243: "سایزینگ Air Receiver — کنترلر نیست",
    244: "سایزینگ HDT — کنترلر نیست",
    411: "وزن مخازن — کنترلر نیست",
    429: "فیلتر سایزینگ — کنترلر نیست",
    430: "Water Trap — کنترلر نیست",
    465: "سایزینگ نیتروژن‌ساز — کنترلر نیست",
    307: "استعلام فنی گروه کالا — صفحه هاویار نیست",
    308: "استعلام فنی پیمانکار — صفحه هاویار نیست",
    309: "استعلام فنی — صفحه هاویار نیست",
    315: "کامنت استعلام فنی — صفحه نیست",
    566: "استعلام فنی پیمانکاران — صفحه نیست",
    405: "تحقیق و توسعه — صفحه هاویار نیست",
    406: "درخواست تغییر طراحی — صفحه هاویار نیست",
    311: "گروه کالا محصولات خاص — صفحه نیست",
    312: "کالا محصولات خاص — صفحه نیست",
    313: "تجهیز محصولات خاص — صفحه نیست",
    314: "BOM تجهیزات خاص — صفحه نیست",
    320: "گزارش تجهیزات خاص — صفحه نیست",
    407: "نرخ ارز مهندسی — به Acc داده نمی‌شود",
    61: "صفحه خدمات پس از فروش روی سیستم مهندسی — خارج از محدوده",
}

REUSE = [
    # gid, existing role id, existing name, skip extra user assign
    (85, 200000, "EdmsDocumentsDccUsers", 1),
    (141, 200007, "Edms.Reports.MDRReport", 0),
    (212, 200008, "Edms.Reports.PersonnelWorkLoadReport", 0),
    (440, 100019, "پارت لیست مصرفی", 0),
]


def values_paths() -> str:
    lines = []
    for page_id, rows in sorted(PAGE_PATHS.items()):
        for path, at, ait, entity, flag in rows:
            ent = f"N'{esc(entity)}'" if entity else "NULL"
            lines.append(f"        ({page_id}, N'{esc(path)}', {at}, {ait}, {ent}, N'{flag}')")
    return ",\n".join(lines)


def values_profiles() -> str:
    lines = []
    for page_id, rows in sorted(PAGE_PROFILES.items()):
        for name, flag in rows:
            lines.append(f"        ({page_id}, N'{esc(name)}', N'{flag}')")
    return ",\n".join(lines)


def values_reuse() -> str:
    lines = []
    for gid, rid, name, skip in REUSE:
        lines.append(f"        ({gid}, {rid}, N'{esc(name)}', {skip})")
    return ",\n".join(lines)


def values_skipped() -> str:
    lines = []
    for pid, reason in sorted(SKIPPED_PAGES.items()):
        lines.append(f"        ({pid}, N'{esc(reason)}')")
    return ",\n".join(lines)


SQL = f"""/*
  Seed_Engineering_HtsGroups_RolesAndUsers.sql
  سینک دسترسی سیستم مهندسی HTS (System_ID=26) با نقش‌های هاویار:
    - یک نقش به‌ازای هر گروه کاربری HTS (۱۱۷ گروه)
    - RoleAccess فقط روی کنترلر/نمایهٔ موجود هاویار
    - صفحات بدون کنترلر (کارتابل/آرشیو/DCC/تأیید) → نمایه داده فرم مدارک
    - نقش تست eng (Id=2) از همه کاربران برداشته می‌شود؛ ردیف نقش خالی می‌ماند
    - منوی system.SystemMenu تغییر نمی‌کند
    - گروه 85 DCC: نقش موجود، کاربران جدید اضافه نمی‌شوند
    - گروه 440 → پارت لیست مصرفی ؛ گروه 441 نقش جدا
    - ShowPrice فقط نمایهٔ قیمت (گروه‌های ۳۷/۴۷ و Permission 8)
    - صفحه ۲۲۳ خروجی اکسل جدا است (گروه ۳۶۳). اعطاهای مستقیم و تکوین ۴۸۲:
      Seed_Engineering_HtsLeftover_DirectAndFixes.sql
  Idempotent. Requires linked server [TMS] → TotalSystem.
  Encoding: UTF-8 with BOM. Apply: sqlcmd -C -b -I -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-eng-hts-groups';

    -----------------------------------------------------------------------------
    PRINT N'=== [1] Unassign test role eng (Id=2) and paired EngMenu (400006) ===';
    -----------------------------------------------------------------------------
    DECLARE @Uid BIGINT, @RoleIds NVARCHAR(MAX), @Roles NVARCHAR(MAX);
    DECLARE @NewRoleIds NVARCHAR(MAX), @NewRoles NVARCHAR(MAX);
    DECLARE @UnassignedEng INT = 0, @UnassignedMenu INT = 0;

    DECLARE u_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT u.Id, ISNULL(u.RoleIds, N'[]'), ISNULL(u.Roles, N'[]')
        FROM system.[User] u
        WHERE EXISTS (
            SELECT 1 FROM OPENJSON(ISNULL(u.RoleIds, N'[]')) j
            WHERE TRY_CAST(j.value AS bigint) IN (2, 400006)
        );
    OPEN u_cur;
    FETCH NEXT FROM u_cur INTO @Uid, @RoleIds, @Roles;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF ISJSON(@RoleIds) = 0 SET @RoleIds = N'[]';
        IF ISJSON(@Roles) = 0 SET @Roles = N'[]';

        IF EXISTS (SELECT 1 FROM OPENJSON(@RoleIds) j WHERE TRY_CAST(j.value AS bigint) = 2)
            SET @UnassignedEng += 1;
        IF EXISTS (SELECT 1 FROM OPENJSON(@RoleIds) j WHERE TRY_CAST(j.value AS bigint) = 400006)
            SET @UnassignedMenu += 1;

        SELECT @NewRoleIds = N'[' + ISNULL(STUFF((
            SELECT N',' + CAST(TRY_CAST(j.value AS bigint) AS nvarchar(20))
            FROM OPENJSON(@RoleIds) j
            WHERE TRY_CAST(j.value AS bigint) IS NOT NULL
              AND TRY_CAST(j.value AS bigint) NOT IN (2, 400006)
            FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N''), N'') + N']';

        SELECT @NewRoles = N'[' + ISNULL(STUFF((
            SELECT N',"' + REPLACE(j.value, N'"', N'\\"') + N'"'
            FROM OPENJSON(@Roles) j
            WHERE j.value NOT IN (N'eng', N'EngMenu')
            FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N''), N'') + N']';

        UPDATE system.[User]
        SET RoleIds = @NewRoleIds,
            Roles = @NewRoles,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @Uid;

        FETCH NEXT FROM u_cur INTO @Uid, @RoleIds, @Roles;
    END
    CLOSE u_cur;
    DEALLOCATE u_cur;
    PRINT N'  Removed eng from users: ' + CAST(@UnassignedEng AS nvarchar(20));
    PRINT N'  Removed EngMenu from users: ' + CAST(@UnassignedMenu AS nvarchar(20));

    DELETE FROM system.RoleAccess WHERE RoleId = 2;
    PRINT N'  Cleared RoleAccess of test role eng: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    UPDATE system.Role
    SET IsActive = 0,
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Id = 2;
    PRINT N'  Deactivated Role Id=2 (row kept; no code FK on name eng)';

    -----------------------------------------------------------------------------
    PRINT N'=== [2] Mapping tables ===';
    -----------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#Reuse') IS NOT NULL DROP TABLE #Reuse;
    CREATE TABLE #Reuse
    (
        HtsGroupId     INT            NOT NULL PRIMARY KEY,
        RoleId         BIGINT         NOT NULL,
        RoleName       NVARCHAR(200)  NOT NULL,
        SkipUserAssign BIT            NOT NULL
    );
    INSERT INTO #Reuse (HtsGroupId, RoleId, RoleName, SkipUserAssign) VALUES
{values_reuse()};

    IF OBJECT_ID('tempdb..#PagePath') IS NOT NULL DROP TABLE #PagePath;
    CREATE TABLE #PagePath
    (
        PageId               INT            NOT NULL,
        Path                 NVARCHAR(300)  NOT NULL,
        ActionAccessType     INT            NOT NULL,
        ActionAccessItemType INT            NOT NULL,
        EntityName           NVARCHAR(200)  NULL,
        Flag                 NVARCHAR(20)   NOT NULL
    );
    INSERT INTO #PagePath (PageId, Path, ActionAccessType, ActionAccessItemType, EntityName, Flag) VALUES
{values_paths()};

    IF OBJECT_ID('tempdb..#PageProfile') IS NOT NULL DROP TABLE #PageProfile;
    CREATE TABLE #PageProfile
    (
        PageId      INT           NOT NULL,
        ProfileName NVARCHAR(150) NOT NULL,
        Flag        NVARCHAR(20)  NOT NULL
    );
    INSERT INTO #PageProfile (PageId, ProfileName, Flag) VALUES
{values_profiles()};

    IF OBJECT_ID('tempdb..#SkippedPage') IS NOT NULL DROP TABLE #SkippedPage;
    CREATE TABLE #SkippedPage (PageId INT NOT NULL PRIMARY KEY, Reason NVARCHAR(300) NOT NULL);
    INSERT INTO #SkippedPage (PageId, Reason) VALUES
{values_skipped()};

    -----------------------------------------------------------------------------
    PRINT N'=== [3] Ensure one Havayar role per HTS Engineering group ===';
    -----------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#HtsGroup') IS NOT NULL DROP TABLE #HtsGroup;
    CREATE TABLE #HtsGroup
    (
        HtsGroupId INT            NOT NULL PRIMARY KEY,
        Title      NVARCHAR(200)  NOT NULL,
        RoleName   NVARCHAR(200)  NULL,
        RoleId     BIGINT         NULL
    );

    INSERT INTO #HtsGroup (HtsGroupId, Title)
    SELECT g.UserGroup_ID, MAX(LTRIM(RTRIM(g.UserGroup_Title)))
    FROM [TMS].[TotalSystem].[dbo].[Vw_Permission] g
    WHERE g.System_ID = 26
      AND g.IsActive = 1
      AND g.UserGroup_ID IS NOT NULL
    GROUP BY g.UserGroup_ID;

    -- Reuse existing named roles
    UPDATE h
    SET RoleId = r.RoleId,
        RoleName = r.RoleName
    FROM #HtsGroup h
    INNER JOIN #Reuse r ON r.HtsGroupId = h.HtsGroupId;

    -- Preferred id = 260000 + group id
    DECLARE @Gid INT, @Title NVARCHAR(200), @WantedId BIGINT, @RoleName NVARCHAR(200), @RoleId BIGINT;
    DECLARE @Created INT = 0, @Reused INT = 0, @ExistsById INT = 0;

    DECLARE g_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT HtsGroupId, Title FROM #HtsGroup WHERE RoleId IS NULL;
    OPEN g_cur;
    FETCH NEXT FROM g_cur INTO @Gid, @Title;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @WantedId = 260000 + @Gid;
        SET @RoleName = @Title;
        IF EXISTS (SELECT 1 FROM system.Role r WHERE r.Name = @RoleName AND r.Id <> @WantedId)
            SET @RoleName = @Title + N' (' + CAST(@Gid AS nvarchar(20)) + N')';

        IF EXISTS (SELECT 1 FROM system.Role WHERE Id = @WantedId)
        BEGIN
            UPDATE system.Role
            SET Name = @RoleName,
                Title = @Title,
                IsActive = 1,
                ModifiedById = 1,
                ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now,
                ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Id = @WantedId;
            SET @RoleId = @WantedId;
            SET @ExistsById += 1;
        END
        ELSE
        BEGIN
            SET IDENTITY_INSERT system.Role ON;
            INSERT INTO system.Role
                (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
                 CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime,
                 CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
            VALUES
                (@WantedId, @RoleName, @Title, 1, @SeedUser, 1, @SeedUser,
                 @Now, @Now, @NowShamsi, @NowShamsi, 1);
            SET IDENTITY_INSERT system.Role OFF;
            SET @RoleId = @WantedId;
            SET @Created += 1;
        END

        UPDATE #HtsGroup SET RoleId = @RoleId, RoleName = @RoleName WHERE HtsGroupId = @Gid;
        SET @RoleId = NULL;
        FETCH NEXT FROM g_cur INTO @Gid, @Title;
    END
    CLOSE g_cur;
    DEALLOCATE g_cur;

    -- Keep reused role titles as-is; just ensure active
    UPDATE r
    SET IsActive = 1,
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    FROM system.Role r
    INNER JOIN #Reuse x ON x.RoleId = r.Id
    WHERE r.IsActive <> 1 OR r.IsActive IS NULL;

    SELECT @Reused = COUNT(*) FROM #Reuse;
    PRINT N'  Roles created: ' + CAST(@Created AS nvarchar(20));
    PRINT N'  Roles reused by name/id: ' + CAST(@ExistsById AS nvarchar(20));
    PRINT N'  Roles mapped to existing (DCC/MDR/PUM/workload): ' + CAST(@Reused AS nvarchar(20));

    -----------------------------------------------------------------------------
    PRINT N'=== [4] Controller RoleAccess from HTS group grants ===';
    -----------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#Grant') IS NOT NULL DROP TABLE #Grant;
    CREATE TABLE #Grant
    (
        HtsGroupId   INT NOT NULL,
        PageId       INT NOT NULL,
        PermissionId INT NOT NULL
    );
    INSERT INTO #Grant (HtsGroupId, PageId, PermissionId)
    SELECT DISTINCT UserGroup_ID, Page_ID, Permission_ID
    FROM [TMS].[TotalSystem].[dbo].[Vw_Permission]
    WHERE System_ID = 26 AND IsActive = 1 AND UserGroup_ID IS NOT NULL;

    IF OBJECT_ID('tempdb..#ActionAccess') IS NOT NULL DROP TABLE #ActionAccess;
    CREATE TABLE #ActionAccess
    (
        RoleId               BIGINT         NOT NULL,
        Path                 NVARCHAR(300)  NOT NULL,
        ActionAccessType     INT            NOT NULL,
        ActionAccessItemType INT            NOT NULL,
        EntityName           NVARCHAR(200)  NULL
    );

    INSERT INTO #ActionAccess (RoleId, Path, ActionAccessType, ActionAccessItemType, EntityName)
    SELECT DISTINCT h.RoleId, p.Path, p.ActionAccessType, p.ActionAccessItemType, p.EntityName
    FROM #Grant g
    INNER JOIN #HtsGroup h ON h.HtsGroupId = g.HtsGroupId
    INNER JOIN #PagePath p ON p.PageId = g.PageId
    WHERE
        (p.Flag = N'read'     AND g.PermissionId IN (2, 3, 7))
     OR (p.Flag = N'export'   AND g.PermissionId IN (2, 3, 7, 10, 62))
     OR (p.Flag = N'write'    AND g.PermissionId IN (2, 4, 5))
     OR (p.Flag = N'delete'   AND g.PermissionId IN (2, 6))
     OR (p.Flag = N'fullonly' AND g.PermissionId = 2);

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT a.Path, a.ActionAccessType, a.ActionAccessItemType, a.EntityName, NULL, NULL, NULL, a.RoleId,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #ActionAccess a
    WHERE a.RoleId IS NOT NULL
      AND NOT EXISTS (
          SELECT 1 FROM system.RoleAccess x
          WHERE x.RoleId = a.RoleId AND x.Path = a.Path AND x.ActionAccessType = a.ActionAccessType
      );
    PRINT N'  Inserted controller RoleAccess: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    -----------------------------------------------------------------------------
    PRINT N'=== [5] DataProfile RoleAccess ===';
    -----------------------------------------------------------------------------
    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT
        N'dataProfile_' + CAST(q.Id AS nvarchar(20)),
        3, 7, q.EntityFullName, q.Title, NULL, q.Id, h.RoleId,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #Grant g
    INNER JOIN #HtsGroup h ON h.HtsGroupId = g.HtsGroupId
    INNER JOIN #PageProfile pp ON pp.PageId = g.PageId
    INNER JOIN system.SavedQuery q ON q.Name = pp.ProfileName
    WHERE h.RoleId IS NOT NULL
      AND (
            (pp.Flag = N'read'  AND g.PermissionId IN (2, 3, 7, 4, 5))
         OR (pp.Flag = N'write' AND g.PermissionId IN (2, 5))
         OR (pp.Flag = N'price' AND g.PermissionId = 8)
      )
      AND NOT EXISTS (
          SELECT 1 FROM system.RoleAccess ra
          WHERE ra.ActionAccessType = 3 AND ra.RowId = q.Id AND ra.RoleId = h.RoleId
      );
    PRINT N'  Inserted DataProfile RoleAccess: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    -----------------------------------------------------------------------------
    PRINT N'=== [6] Assign group members (skip extra DCC users) ===';
    -----------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#UserRoleMap') IS NOT NULL DROP TABLE #UserRoleMap;
    CREATE TABLE #UserRoleMap
    (
        Username NVARCHAR(200) NOT NULL,
        RoleName NVARCHAR(200) NOT NULL,
        RoleId   BIGINT        NOT NULL
    );

    INSERT INTO #UserRoleMap (Username, RoleName, RoleId)
    SELECT DISTINCT COALESCE(
            NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''),
            NULLIF(LTRIM(RTRIM(u.Username)), N'')),
           h.RoleName,
           h.RoleId
    FROM [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
    INNER JOIN #HtsGroup h ON h.HtsGroupId = m.UserGroup_FK
    LEFT JOIN #Reuse ru ON ru.HtsGroupId = h.HtsGroupId
    WHERE u.IsActive = 1
      AND h.RoleId IS NOT NULL
      AND h.RoleName IS NOT NULL
      AND ISNULL(ru.SkipUserAssign, 0) = 0
      AND COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')) IS NOT NULL;

    -- Also map alternate HTS Username when it differs from AD
    INSERT INTO #UserRoleMap (Username, RoleName, RoleId)
    SELECT DISTINCT u.Username, m.RoleName, m.RoleId
    FROM #UserRoleMap m
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u
        ON LOWER(u.ActiveDirectoryUsername) = LOWER(m.Username)
    WHERE u.Username IS NOT NULL
      AND LOWER(u.Username) <> LOWER(m.Username)
      AND NOT EXISTS (
          SELECT 1 FROM #UserRoleMap x
          WHERE LOWER(x.Username) = LOWER(u.Username) AND x.RoleId = m.RoleId
      );

    ;WITH d AS (
        SELECT Username, RoleId,
               ROW_NUMBER() OVER (PARTITION BY LOWER(Username), RoleId ORDER BY Username) AS rn
        FROM #UserRoleMap
    )
    DELETE FROM d WHERE rn > 1;

    DECLARE @MapUsername nvarchar(200), @MapRoleName nvarchar(200), @MapRoleId bigint, @MapUserId bigint;
    DECLARE @Assigned int = 0, @Missing int = 0, @SkippedAlready int = 0;

    DECLARE map_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT Username, RoleName, RoleId FROM #UserRoleMap;
    OPEN map_cur;
    FETCH NEXT FROM map_cur INTO @MapUsername, @MapRoleName, @MapRoleId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT TOP 1 @MapUserId = Id
        FROM system.[User]
        WHERE LOWER(Username) = LOWER(@MapUsername);

        IF @MapUserId IS NULL
        BEGIN
            PRINT N'  MISSING USER (skip): ' + @MapUsername + N' for role ' + @MapRoleName;
            SET @Missing = @Missing + 1;
        END
        ELSE IF EXISTS (
            SELECT 1 FROM system.[User] u
            CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
            WHERE u.Id = @MapUserId AND TRY_CAST(j.value AS bigint) = @MapRoleId
        )
            SET @SkippedAlready = @SkippedAlready + 1;
        ELSE
        BEGIN
            UPDATE system.[User]
            SET
                RoleIds = CASE
                    WHEN RoleIds IS NULL OR LTRIM(RTRIM(RoleIds)) = N'' OR RoleIds = N'[]'
                        THEN N'[' + CAST(@MapRoleId AS nvarchar(20)) + N']'
                    ELSE STUFF(RoleIds, LEN(RoleIds), 1, N',' + CAST(@MapRoleId AS nvarchar(20)) + N']')
                END,
                Roles = CASE
                    WHEN EXISTS (SELECT 1 FROM OPENJSON(ISNULL(Roles, N'[]')) j WHERE j.value = @MapRoleName) THEN Roles
                    WHEN Roles IS NULL OR LTRIM(RTRIM(Roles)) = N'' OR Roles = N'[]'
                        THEN N'["' + REPLACE(@MapRoleName, N'"', N'') + N'"]'
                    ELSE STUFF(Roles, LEN(Roles), 1, N',"' + REPLACE(@MapRoleName, N'"', N'') + N'"]')
                END,
                ModifiedById = 1,
                ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now,
                ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Id = @MapUserId;
            SET @Assigned = @Assigned + 1;
        END

        SET @MapUserId = NULL;
        FETCH NEXT FROM map_cur INTO @MapUsername, @MapRoleName, @MapRoleId;
    END
    CLOSE map_cur;
    DEALLOCATE map_cur;

    PRINT N'  User role assignments applied: ' + CAST(@Assigned AS nvarchar(20));
    PRINT N'  Already had role (skipped): ' + CAST(@SkippedAlready AS nvarchar(20));
    PRINT N'  Missing users (logged): ' + CAST(@Missing AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE: Seed_Engineering_HtsGroups_RolesAndUsers committed ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

PRINT N'--- verify ---';
SELECT N'eng still on users' AS CheckName, COUNT(*) AS Cnt
FROM system.[User] u
CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
WHERE TRY_CAST(j.value AS bigint) = 2;

SELECT N'EngMenu still on users' AS CheckName, COUNT(*) AS Cnt
FROM system.[User] u
CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
WHERE TRY_CAST(j.value AS bigint) = 400006;

SELECT N'DCC role users' AS CheckName, COUNT(DISTINCT u.Id) AS Cnt
FROM system.[User] u
CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
WHERE TRY_CAST(j.value AS bigint) = 200000;

SELECT N'PUM 100019 users' AS CheckName, COUNT(DISTINCT u.Id) AS Cnt
FROM system.[User] u
CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
WHERE TRY_CAST(j.value AS bigint) = 100019;

SELECT N'441 delete-role users' AS CheckName, COUNT(DISTINCT u.Id) AS Cnt
FROM system.[User] u
CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
WHERE TRY_CAST(j.value AS bigint) = 260441;

SELECT N'CompressorSizing RoleAccess' AS CheckName, COUNT(*) AS Cnt
FROM system.RoleAccess
WHERE Path LIKE N'/panel/eng/compressorsizing/%';

SELECT N'Eng HTS roles 260000+' AS CheckName, COUNT(*) AS Cnt
FROM system.Role
WHERE Id BETWEEN 260000 AND 260999 AND IsActive = 1;

SELECT TOP 20 r.Id, r.Name, r.Title,
       (SELECT COUNT(*) FROM system.RoleAccess ra WHERE ra.RoleId = r.Id) AS AccessCnt,
       (SELECT COUNT(*) FROM system.[User] u CROSS APPLY OPENJSON(ISNULL(u.RoleIds,N'[]')) j
        WHERE TRY_CAST(j.value AS bigint) = r.Id) AS Users
FROM system.Role r
WHERE r.Id BETWEEN 260000 AND 260999 OR r.Id IN (200000, 200007, 200008, 100019)
ORDER BY r.Id;
"""


LEFTOVER_SQL = r"""/*
  Seed_Engineering_HtsLeftover_DirectAndFixes.sql
  باقی‌ماندهٔ دسترسی مهندسی بعد از Seed_Engineering_HtsGroups_RolesAndUsers:
    1) گروه ۳۶۳ — فقط ExportToExcel روی لیست کنترل مدارک
    2) گروه ۴۸۲ تکوین (perm 171) — نمایه آرشیو کلیه پروژه‌ها + list/fetch، بدون CRUD
    3) ۱۰ کاربر اعطای مستقیم HTS (بدون گروه) → نقش گروه نزدیک یا نقش کوچک Eng.Direct
  تغییر نمی‌دهد: SystemMenu، نقش eng، DCC، زیرگروه‌های درخواست تغییر قطعه ۲۹۹، ۴۲ نقش خالی.
  Idempotent. Encoding: UTF-8 with BOM. Apply: sqlcmd -C -b -I -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-eng-hts-leftover';

    -----------------------------------------------------------------------------
    PRINT N'=== [1] Group 363: document control Excel export only ===';
    -----------------------------------------------------------------------------
    IF NOT EXISTS (
        SELECT 1 FROM system.RoleAccess
        WHERE RoleId = 260363
          AND Path = N'/panel/edms/document/exporttoexcel'
          AND ActionAccessType = 2
    )
    BEGIN
        INSERT INTO system.RoleAccess
            (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (N'/panel/edms/document/exporttoexcel', 2, 0, N'Entities.App.Edms.Document', N'خروجی اکسل', NULL, NULL, 260363,
             1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
        PRINT N'  Inserted 260363 /panel/edms/document/exporttoexcel';
    END
    ELSE
        PRINT N'  260363 exporttoexcel already present';

    -----------------------------------------------------------------------------
    PRINT N'=== [2] Group 482 Takvin: archive Data Profile + list path (no CRUD) ===';
    -----------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#TakvinAccess') IS NOT NULL DROP TABLE #TakvinAccess;
    CREATE TABLE #TakvinAccess
    (
        Path                 NVARCHAR(300) NOT NULL,
        ActionAccessType     INT           NOT NULL,
        ActionAccessItemType INT           NOT NULL,
        EntityName           NVARCHAR(200) NULL,
        DisplayName          NVARCHAR(200) NULL,
        RowId                BIGINT        NULL
    );

    INSERT INTO #TakvinAccess (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId)
    VALUES
        (N'/panel/edms/document/list', 1, 1, N'Entities.App.Edms.Document', N'لیست اطلاعات', NULL),
        (N'/panel/edms/document/fetchdata', 2, 2, N'Entities.App.Edms.Document', N'دریافت داده', NULL);

    INSERT INTO #TakvinAccess (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId)
    SELECT
        N'dataProfile_' + CAST(q.Id AS nvarchar(20)),
        3, 7, q.EntityFullName, q.Title, q.Id
    FROM system.SavedQuery q
    WHERE q.Name = N'vw_DocumentAllProjectInfo';

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT a.Path, a.ActionAccessType, a.ActionAccessItemType, a.EntityName, a.DisplayName, NULL, a.RowId, 260482,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #TakvinAccess a
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess x
        WHERE x.RoleId = 260482
          AND x.Path = a.Path
          AND x.ActionAccessType = a.ActionAccessType
    );
    PRINT N'  Inserted 260482 archive/list RoleAccess: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    -----------------------------------------------------------------------------
    PRINT N'=== [3] Small per-grant-set roles for direct HTS users ===';
    -----------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#DirectRole') IS NOT NULL DROP TABLE #DirectRole;
    CREATE TABLE #DirectRole
    (
        RoleId BIGINT        NOT NULL PRIMARY KEY,
        Name   NVARCHAR(200) NOT NULL,
        Title  NVARCHAR(200) NOT NULL
    );
    INSERT INTO #DirectRole (RoleId, Name, Title) VALUES
        (269001, N'اعطا مستقیم - آرشیو شخصی اسناد', N'اعطا مستقیم - آرشیو شخصی اسناد'),
        (269002, N'اعطا مستقیم - مشاهده شناسنامه کمپرسور', N'اعطا مستقیم - مشاهده شناسنامه کمپرسور'),
        (269003, N'اعطا مستقیم - مشاهده محصول', N'اعطا مستقیم - مشاهده محصول'),
        (269004, N'اعطا مستقیم - مشاهده BOM و آرشیو', N'اعطا مستقیم - مشاهده BOM و آرشیو'),
        (269005, N'اعطا مستقیم - محصول کامل و مقایسه قیمت', N'اعطا مستقیم - محصول کامل و مقایسه قیمت'),
        (269006, N'اعطا مستقیم - مقایسه قیمت محصولات', N'اعطا مستقیم - مقایسه قیمت محصولات');

    DECLARE @DrId BIGINT, @DrName NVARCHAR(200), @DrTitle NVARCHAR(200);
    DECLARE @CreatedDirect INT = 0, @ExistsDirect INT = 0;
    DECLARE dr_cur CURSOR LOCAL FAST_FORWARD FOR SELECT RoleId, Name, Title FROM #DirectRole;
    OPEN dr_cur;
    FETCH NEXT FROM dr_cur INTO @DrId, @DrName, @DrTitle;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF EXISTS (SELECT 1 FROM system.Role WHERE Id = @DrId)
        BEGIN
            UPDATE system.Role
            SET Name = @DrName, Title = @DrTitle, IsActive = 1,
                ModifiedById = 1, ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Id = @DrId;
            SET @ExistsDirect += 1;
        END
        ELSE
        BEGIN
            SET IDENTITY_INSERT system.Role ON;
            INSERT INTO system.Role
                (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
                 CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime,
                 CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
            VALUES
                (@DrId, @DrName, @DrTitle, 1, @SeedUser, 1, @SeedUser,
                 @Now, @Now, @NowShamsi, @NowShamsi, 1);
            SET IDENTITY_INSERT system.Role OFF;
            SET @CreatedDirect += 1;
        END
        FETCH NEXT FROM dr_cur INTO @DrId, @DrName, @DrTitle;
    END
    CLOSE dr_cur;
    DEALLOCATE dr_cur;
    PRINT N'  Direct roles created: ' + CAST(@CreatedDirect AS nvarchar(20));
    PRINT N'  Direct roles already existed: ' + CAST(@ExistsDirect AS nvarchar(20));

    IF OBJECT_ID('tempdb..#DirectAccess') IS NOT NULL DROP TABLE #DirectAccess;
    CREATE TABLE #DirectAccess
    (
        RoleId               BIGINT         NOT NULL,
        Path                 NVARCHAR(300)  NOT NULL,
        ActionAccessType     INT            NOT NULL,
        ActionAccessItemType INT            NOT NULL,
        EntityName           NVARCHAR(200)  NULL,
        DisplayName          NVARCHAR(200)  NULL,
        RowId                BIGINT         NULL
    );

    -- 269001 personal archive read (HTS 233)
    INSERT INTO #DirectAccess (RoleId, Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId) VALUES
        (269001, N'/panel/edms/document/list', 1, 1, N'Entities.App.Edms.Document', N'لیست اطلاعات', NULL),
        (269001, N'/panel/edms/document/edit', 1, 5, N'Entities.App.Edms.Document', N'ویرایش اطلاعات', NULL),
        (269001, N'/panel/edms/document/fetchdata', 2, 2, N'Entities.App.Edms.Document', N'دریافت داده', NULL);
    INSERT INTO #DirectAccess (RoleId, Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId)
    SELECT 269001, N'dataProfile_' + CAST(q.Id AS nvarchar(20)), 3, 7, q.EntityFullName, q.Title, q.Id
    FROM system.SavedQuery q WHERE q.Name = N'vw_DocumentMyArchive';

    -- 269002 compressor identity read (HTS 109 Read; skip ShowSalePrice 182)
    INSERT INTO #DirectAccess (RoleId, Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId) VALUES
        (269002, N'/panel/inv/partextrainfo/list', 1, 1, N'Entities.App.Inv.PartExtraInfo', N'لیست اطلاعات', NULL),
        (269002, N'/panel/inv/partextrainfo/edit', 1, 5, N'Entities.App.Inv.PartExtraInfo', N'ویرایش اطلاعات', NULL),
        (269002, N'/panel/inv/partextrainfo/fetchdata', 2, 2, N'Entities.App.Inv.PartExtraInfo', N'دریافت داده', NULL),
        (269002, N'/panel/inv/partextrainfo/exporttoexcel', 2, 0, N'Entities.App.Inv.PartExtraInfo', N'خروجی اکسل', NULL);
    INSERT INTO #DirectAccess (RoleId, Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId)
    SELECT 269002, N'dataProfile_' + CAST(q.Id AS nvarchar(20)), 3, 7, q.EntityFullName, q.Title, q.Id
    FROM system.SavedQuery q WHERE q.Name IN (N'partextrainfo_listinfo', N'vw_PartextrainfoOilInject');

    -- 269003 product formul view read (HTS 107+108 Read, no price)
    INSERT INTO #DirectAccess (RoleId, Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId) VALUES
        (269003, N'/panel/bom/productformul/list', 1, 1, N'Entities.App.Bom.ProductFormul', N'لیست اطلاعات', NULL),
        (269003, N'/panel/bom/productformul/edit', 1, 5, N'Entities.App.Bom.ProductFormul', N'ویرایش اطلاعات', NULL),
        (269003, N'/panel/bom/productformul/fetchdata', 2, 2, N'Entities.App.Bom.ProductFormul', N'دریافت داده', NULL),
        (269003, N'/panel/bom/productformul/exporttoexcel', 2, 0, N'Entities.App.Bom.ProductFormul', N'خروجی اکسل', NULL);
    INSERT INTO #DirectAccess (RoleId, Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId)
    SELECT 269003, N'dataProfile_' + CAST(q.Id AS nvarchar(20)), 3, 7, q.EntityFullName, q.Title, q.Id
    FROM system.SavedQuery q WHERE q.Name IN (N'productformul_listinfo', N'vw_productformulwithBom');

    -- 269004 Sadeghpour: 100/106/107/108/109/232/541 Read
    INSERT INTO #DirectAccess (RoleId, Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId) VALUES
        (269004, N'/panel/bom/productformul/list', 1, 1, N'Entities.App.Bom.ProductFormul', N'لیست اطلاعات', NULL),
        (269004, N'/panel/bom/productformul/edit', 1, 5, N'Entities.App.Bom.ProductFormul', N'ویرایش اطلاعات', NULL),
        (269004, N'/panel/bom/productformul/fetchdata', 2, 2, N'Entities.App.Bom.ProductFormul', N'دریافت داده', NULL),
        (269004, N'/panel/bom/productformul/exporttoexcel', 2, 0, N'Entities.App.Bom.ProductFormul', N'خروجی اکسل', NULL),
        (269004, N'/panel/inv/part/list', 1, 1, N'Entities.App.Inv.Part', N'لیست اطلاعات', NULL),
        (269004, N'/panel/inv/part/edit', 1, 5, N'Entities.App.Inv.Part', N'ویرایش اطلاعات', NULL),
        (269004, N'/panel/inv/part/fetchdata', 2, 2, N'Entities.App.Inv.Part', N'دریافت داده', NULL),
        (269004, N'/panel/inv/part/exporttoexcel', 2, 0, N'Entities.App.Inv.Part', N'خروجی اکسل', NULL),
        (269004, N'/panel/inv/partextrainfo/list', 1, 1, N'Entities.App.Inv.PartExtraInfo', N'لیست اطلاعات', NULL),
        (269004, N'/panel/inv/partextrainfo/edit', 1, 5, N'Entities.App.Inv.PartExtraInfo', N'ویرایش اطلاعات', NULL),
        (269004, N'/panel/inv/partextrainfo/fetchdata', 2, 2, N'Entities.App.Inv.PartExtraInfo', N'دریافت داده', NULL),
        (269004, N'/panel/inv/partextrainfo/exporttoexcel', 2, 0, N'Entities.App.Inv.PartExtraInfo', N'خروجی اکسل', NULL),
        (269004, N'/panel/edms/document/list', 1, 1, N'Entities.App.Edms.Document', N'لیست اطلاعات', NULL),
        (269004, N'/panel/edms/document/edit', 1, 5, N'Entities.App.Edms.Document', N'ویرایش اطلاعات', NULL),
        (269004, N'/panel/edms/document/fetchdata', 2, 2, N'Entities.App.Edms.Document', N'دریافت داده', NULL);
    INSERT INTO #DirectAccess (RoleId, Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId)
    SELECT 269004, N'dataProfile_' + CAST(q.Id AS nvarchar(20)), 3, 7, q.EntityFullName, q.Title, q.Id
    FROM system.SavedQuery q
    WHERE q.Name IN (
        N'productformul_listinfo', N'vw_productformulwithBom', N'part_listinfo',
        N'partextrainfo_listinfo', N'vw_PartextrainfoOilInject', N'vw_PartextrainfoOilFree',
        N'vw_DocumentAllProjectInfo'
    );

    -- 269005 toosi: 107 FullAccess + 116 Read (no ShowPrice profile)
    INSERT INTO #DirectAccess (RoleId, Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId) VALUES
        (269005, N'/panel/bom/productformul/list', 1, 1, N'Entities.App.Bom.ProductFormul', N'لیست اطلاعات', NULL),
        (269005, N'/panel/bom/productformul/edit', 1, 5, N'Entities.App.Bom.ProductFormul', N'ویرایش اطلاعات', NULL),
        (269005, N'/panel/bom/productformul/fetchdata', 2, 2, N'Entities.App.Bom.ProductFormul', N'دریافت داده', NULL),
        (269005, N'/panel/bom/productformul/exporttoexcel', 2, 0, N'Entities.App.Bom.ProductFormul', N'خروجی اکسل', NULL),
        (269005, N'/panel/bom/productformul/new', 1, 4, N'Entities.App.Bom.ProductFormul', N'درج اطلاعات', NULL),
        (269005, N'/panel/bom/productformul/add', 2, 4, N'Entities.App.Bom.ProductFormul', N'درج اطلاعات', NULL),
        (269005, N'/panel/bom/productformul/save', 2, 3, N'Entities.App.Bom.ProductFormul', N'ذخیره', NULL),
        (269005, N'/panel/bom/productformul/update', 2, 5, N'Entities.App.Bom.ProductFormul', N'بروزرسانی', NULL),
        (269005, N'/panel/bom/productformul/delete', 2, 6, N'Entities.App.Bom.ProductFormul', N'حذف', NULL),
        (269005, N'/panel/bom/productpricecompare/list', 1, 1, N'Entities.App.Bom.ProductPriceCompare', N'لیست اطلاعات', NULL),
        (269005, N'/panel/bom/productpricecompare/fetchdata', 2, 2, N'Entities.App.Bom.ProductPriceCompare', N'دریافت داده', NULL),
        (269005, N'/panel/bom/productpricecompare/details', 1, 1, N'Entities.App.Bom.ProductPriceCompare', N'جزئیات', NULL),
        (269005, N'/panel/bom/productpricecompare/availabledates', 2, 2, N'Entities.App.Bom.ProductPriceCompare', N'تاریخ‌ها', NULL);
    INSERT INTO #DirectAccess (RoleId, Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId)
    SELECT 269005, N'dataProfile_' + CAST(q.Id AS nvarchar(20)), 3, 7, q.EntityFullName, q.Title, q.Id
    FROM system.SavedQuery q WHERE q.Name IN (N'productformul_listinfo', N'vw_productformulwithBom');

    -- 269006 Zaeim: 116 FullAccess, no ShowPrice
    INSERT INTO #DirectAccess (RoleId, Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId) VALUES
        (269006, N'/panel/bom/productpricecompare/list', 1, 1, N'Entities.App.Bom.ProductPriceCompare', N'لیست اطلاعات', NULL),
        (269006, N'/panel/bom/productpricecompare/fetchdata', 2, 2, N'Entities.App.Bom.ProductPriceCompare', N'دریافت داده', NULL),
        (269006, N'/panel/bom/productpricecompare/details', 1, 1, N'Entities.App.Bom.ProductPriceCompare', N'جزئیات', NULL),
        (269006, N'/panel/bom/productpricecompare/availabledates', 2, 2, N'Entities.App.Bom.ProductPriceCompare', N'تاریخ‌ها', NULL),
        (269006, N'/panel/bom/productpricecompare/sendemail', 2, 1000, N'Entities.App.Bom.ProductPriceCompare', N'ارسال ایمیل', NULL);

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT a.Path, a.ActionAccessType, a.ActionAccessItemType, a.EntityName, a.DisplayName, NULL, a.RowId, a.RoleId,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #DirectAccess a
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess x
        WHERE x.RoleId = a.RoleId AND x.Path = a.Path AND x.ActionAccessType = a.ActionAccessType
    );
    PRINT N'  Inserted direct-role RoleAccess: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    -----------------------------------------------------------------------------
    PRINT N'=== [4] Assign leftover users (skip missing / inactive) ===';
    -----------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#UserRoleMap') IS NOT NULL DROP TABLE #UserRoleMap;
    CREATE TABLE #UserRoleMap
    (
        Username NVARCHAR(200) NOT NULL,
        RoleId   BIGINT        NOT NULL
    );
    INSERT INTO #UserRoleMap (Username, RoleId) VALUES
        (N'Bostanpira.h', 269001),
        (N'Commissioning', 269001),
        (N'Shahini.a', 269001),
        (N'a', 269002),
        (N'Khalilnezhad.a', 269003),
        (N'Sadeghpour.j', 269004),
        (N'toosi.f', 269005),
        (N'Zaeim.s', 269006),
        (N'Kooravand.m', 260092);

    DECLARE @MapUsername nvarchar(200), @MapRoleName nvarchar(200), @MapRoleId bigint, @MapUserId bigint;
    DECLARE @MapIsActive bit;
    DECLARE @Assigned int = 0, @Missing int = 0, @Inactive int = 0, @SkippedAlready int = 0;

    DECLARE map_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT m.Username, m.RoleId, r.Name
        FROM #UserRoleMap m
        INNER JOIN system.Role r ON r.Id = m.RoleId;
    OPEN map_cur;
    FETCH NEXT FROM map_cur INTO @MapUsername, @MapRoleId, @MapRoleName;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @MapUserId = NULL;
        SET @MapIsActive = NULL;
        SELECT TOP 1 @MapUserId = Id, @MapIsActive = IsActive
        FROM system.[User]
        WHERE LOWER(Username) = LOWER(@MapUsername);

        IF @MapUserId IS NULL
        BEGIN
            PRINT N'  MISSING USER (skip): ' + @MapUsername + N' for role ' + @MapRoleName;
            SET @Missing = @Missing + 1;
        END
        ELSE IF ISNULL(@MapIsActive, 0) = 0
        BEGIN
            PRINT N'  INACTIVE USER (skip): ' + @MapUsername + N' for role ' + @MapRoleName;
            SET @Inactive = @Inactive + 1;
        END
        ELSE IF EXISTS (
            SELECT 1 FROM system.[User] u
            CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
            WHERE u.Id = @MapUserId AND TRY_CAST(j.value AS bigint) = @MapRoleId
        )
            SET @SkippedAlready = @SkippedAlready + 1;
        ELSE
        BEGIN
            UPDATE system.[User]
            SET
                RoleIds = CASE
                    WHEN RoleIds IS NULL OR LTRIM(RTRIM(RoleIds)) = N'' OR RoleIds = N'[]'
                        THEN N'[' + CAST(@MapRoleId AS nvarchar(20)) + N']'
                    ELSE STUFF(RoleIds, LEN(RoleIds), 1, N',' + CAST(@MapRoleId AS nvarchar(20)) + N']')
                END,
                Roles = CASE
                    WHEN EXISTS (SELECT 1 FROM OPENJSON(ISNULL(Roles, N'[]')) j WHERE j.value = @MapRoleName) THEN Roles
                    WHEN Roles IS NULL OR LTRIM(RTRIM(Roles)) = N'' OR Roles = N'[]'
                        THEN N'["' + REPLACE(@MapRoleName, N'"', N'') + N'"]'
                    ELSE STUFF(Roles, LEN(Roles), 1, N',"' + REPLACE(@MapRoleName, N'"', N'') + N'"]')
                END,
                ModifiedById = 1,
                ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now,
                ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Id = @MapUserId;
            SET @Assigned = @Assigned + 1;
        END

        FETCH NEXT FROM map_cur INTO @MapUsername, @MapRoleId, @MapRoleName;
    END
    CLOSE map_cur;
    DEALLOCATE map_cur;

    PRINT N'  User role assignments applied: ' + CAST(@Assigned AS nvarchar(20));
    PRINT N'  Already had role (skipped): ' + CAST(@SkippedAlready AS nvarchar(20));
    PRINT N'  Missing users (logged): ' + CAST(@Missing AS nvarchar(20));
    PRINT N'  Inactive users (logged): ' + CAST(@Inactive AS nvarchar(20));
    PRINT N'  SKIPPED unimplemented page 246: kouchehbaghi.i (Edms_ProjectProgress_Report)';

    COMMIT TRANSACTION;
    PRINT N'=== DONE: Seed_Engineering_HtsLeftover_DirectAndFixes committed ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;
"""


def main():
    OUT.write_text(SQL, encoding="utf-8-sig")
    print(f"Wrote {OUT} ({OUT.stat().st_size} bytes)")
    OUT_LEFTOVER.write_text(LEFTOVER_SQL, encoding="utf-8-sig")
    print(f"Wrote {OUT_LEFTOVER} ({OUT_LEFTOVER.stat().st_size} bytes)")


if __name__ == "__main__":
    main()
