# -*- coding: utf-8 -*-
"""نمایه داده، نقش، منو و دو قالب چاپ شکایات مشتری."""
from __future__ import annotations

import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
SQL_PROFILE = HERE.parent.parent / "Scripts" / "Seed_CrmCustomerComplaint_DataProfiles.sql"
SQL_REPORT = HERE.parent.parent / "Scripts" / "Seed_CrmCustomerComplaint_Reports.sql"

FONT = "IRANSansWeb(FaNum) Medium"
SEED_USER = "seed-crm-complaint"


def sql_n(value: str) -> str:
    return "N'" + value.replace("'", "''") + "'"


def compact(obj) -> str:
    return json.dumps(obj, ensure_ascii=False, separators=(",", ":"))


def col(alliance: str, display: str, system_type: int = 0, *, visible: bool = True, pk: bool = False,
        sort_desc: bool = False, width: int = 120) -> dict:
    names = {0: "String", 1: "Boolean", 5: "DateShamsi", 6: "Long", 7: "Int"}
    return {
        "TableName": "Crm.CustomerComplaint",
        "ColumnName": alliance,
        "DisplayName": display,
        "Alliance": alliance,
        "Address": f"[{alliance}]",
        "SystemTypeName": names.get(system_type, "String"),
        "SelectIndex": 0,
        "Visible": visible,
        "PrimaryKey": pk,
        "SystemType": system_type,
        "SortDirection": 1 if sort_desc else None,
        "SortOrder": 0 if sort_desc else None,
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
        "Width": width,
        "Filterable": True,
        "Sortable": True,
        "ClassName": "",
    }


QUERY = r"""
DECLARE @CuId BIGINT = TRY_CAST(CONVERT(NVARCHAR(30), @CurrentUserId) AS BIGINT);
DECLARE @Roles NVARCHAR(MAX) = N'[]';
SELECT @Roles = CASE WHEN ISJSON(u.Roles) = 1 THEN u.Roles ELSE N'[]' END
FROM [system].[User] u WHERE u.Id = @CuId;
DECLARE @OwnOnly BIT = 0;
IF EXISTS (SELECT 1 FROM OPENJSON(@Roles) j WHERE j.[value] = N'Crm.Complaint.OwnItems')
   AND NOT EXISTS (SELECT 1 FROM OPENJSON(@Roles) j WHERE j.[value] IN (N'Crm.Complaint.Manager', N'Crm.Complaint.Expert'))
    SET @OwnOnly = 1;
--!--mainsection
SELECT
    t1.Id AS t1_Id,
    CASE t1.OwnerType WHEN 1499 THEN N'هوایاری' WHEN 1500 THEN N'غیر هوایاری' ELSE N'' END AS t1_OwnerType,
    cust.Code AS t2_Code,
    party.FullName AS t2_FullName,
    CASE t1.CustomerType WHEN 106 THEN N'حقیقی' WHEN 107 THEN N'حقوقی' ELSE N'' END AS t1_CustomerType,
    t1.CustomerTitle AS t1_CustomerTitle,
    t1.SerialNumber AS t1_SerialNumber,
    t1.CompletionShamsiDate AS t1_CompletionShamsiDate,
    CASE WHEN EXISTS (SELECT 1 FROM Crm.CustomerComplaintAttachment a WHERE a.ComplaintId = t1.Id) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS t1_HasAttachment,
    contact.PrePhoneNumber AS t3_PrePhoneNumber,
    contact.PhoneNumber AS t3_PhoneNumber,
    contact.MobileNumber AS t3_MobileNumber,
    contact.ResponserFullName AS t3_ResponserFullName,
    contact.ResponserJob AS t3_ResponserJob,
    t1.Address AS t1_Address,
    t1.ComplaintReceiver AS t1_ComplaintReceiver,
    t1.ComplaintPresenter AS t1_ComplaintPresenter,
    t1.OrganizationUnitInText AS t1_OrganizationUnitInText,
    t1.ExpertOrManagerInText AS t1_ExpertOrManagerInText,
    CASE t1.Priority WHEN 1122 THEN N'فوری' WHEN 1123 THEN N'غیر فوری' ELSE N'' END AS t1_Priority,
    t1.TrackingCode AS t1_TrackingCode,
    channel.Title AS t4_Title,
    CASE t1.GeneralComplaintSubject WHEN 1495 THEN N'محصول' WHEN 1496 THEN N'فرآیند' WHEN 1497 THEN N'سایر' WHEN 1775 THEN N'محصول - فرآیند' ELSE N'' END AS t1_GeneralComplaintSubject,
    t1.GeneralComplaintSubjectTitle AS t1_GeneralComplaintSubjectTitle,
    CASE t1.ComplaintType WHEN 1531 THEN N'بحرانی' WHEN 1532 THEN N'بسیار مهم' WHEN 1533 THEN N'مهم' WHEN 2739 THEN N'سایر' ELSE N'' END AS t1_ComplaintType,
    t1.RaisedSubjects AS t1_RaisedSubjects,
    CASE t1.ComplaintMainSubject
        WHEN 2551 THEN N'رد شدن در ارزیابی کیفی' WHEN 2552 THEN N'عدم تأیید پیشنهاد فنی' WHEN 2553 THEN N'عدم تأیید پیشنهاد مالی'
        WHEN 2554 THEN N'تاخیر بازرگانی' WHEN 2555 THEN N'تاخیر تدارکات' WHEN 2556 THEN N'تاخیر مالی' WHEN 2557 THEN N'تاخیر خدمات'
        WHEN 2558 THEN N'داشتن الزام ساخت داخل' WHEN 2559 THEN N'ابطال مناقصه' WHEN 2560 THEN N'قیمت قطعه'
        WHEN 2561 THEN N'کیفیت خدمات' WHEN 2562 THEN N'کیفیت فروش' WHEN 2563 THEN N'کیفیت قطعه' WHEN 2564 THEN N'کیفیت محصول'
        ELSE ISNULL(t1.MainSubjectsInText, N'') END AS t1_ComplaintMainSubject,
    t1.Repeatability AS t1_Repeatability,
    t1.HasFinancialLoad AS t1_HasFinancialLoad,
    t1.HasUrgencyHandling AS t1_HasUrgencyHandling,
    t1.HasComplexity AS t1_HasComplexity,
    inform.Title AS t5_Title,
    CASE t1.ComplaintStatus
        WHEN 1168 THEN N'دریافت شکایت' WHEN 1169 THEN N'رسیدگی اولیه' WHEN 1170 THEN N'تکمیل اطلاعات و رسیدگی نهایی'
        WHEN 1171 THEN N'اعلام نتیجه به مشتری' WHEN 1172 THEN N'عدم جلب رضایت و بسته' WHEN 1173 THEN N'جلب رضایت و بسته'
        ELSE N'' END AS t1_ComplaintStatus,
    t1.HandlingResultIdsInText AS t1_HandlingResultIdsInText,
    CASE t1.InformComplainant WHEN 1178 THEN N'انجام شد' WHEN 1179 THEN N'انجام نشد' ELSE N'' END AS t1_InformComplainant,
    CASE t1.ComplaintConsent WHEN 1180 THEN N'حاصل شد' WHEN 1181 THEN N'حاصل نشد' ELSE N'' END AS t1_ComplaintConsent,
    CASE t1.FinalComplaintStatus WHEN 1182 THEN N'بسته شد' WHEN 1183 THEN N'بسته نشد' ELSE N'' END AS t1_FinalComplaintStatus,
    CAST(t1.FinalComplaintStatus AS int) AS t1_FinalStatusValue,
    t1.ComplaintClosingShamsiDate AS t1_ComplaintClosingShamsiDate,
    CASE WHEN t1.ComplaintClosingMiladiDate IS NULL OR t1.CompletionMiladiDate IS NULL THEN 0
         ELSE DATEDIFF(DAY, t1.CompletionMiladiDate, t1.ComplaintClosingMiladiDate) END AS t1_CloseDurationInDay,
    t1.DelayReason AS t1_DelayReason,
    nxt.NextActionShamsiDate AS t6_NextActionShamsiDate,
    t1.CreatedOnShamsiDateTime AS t1_CreatedOnShamsiDateTime,
    t1.ModifiedDateShamsiDateTime AS t1_ModifiedDateShamsiDateTime
FROM Crm.CustomerComplaint AS t1
LEFT JOIN SLS.Customer AS cust ON cust.Id = t1.CustomerId
LEFT JOIN Gnr.Party AS party ON party.Id = cust.PartyId
LEFT JOIN Crm.ComplaintLookupItem AS channel ON channel.Id = t1.CommunicationChannelId
LEFT JOIN Crm.ComplaintLookupItem AS inform ON inform.Id = t1.InformForReceiveComplaintId
OUTER APPLY (
    SELECT TOP (1) c.PrePhoneNumber, c.PhoneNumber, c.MobileNumber, c.ResponserFullName, c.ResponserJob
    FROM Crm.CustomerComplaintContact AS c
    WHERE c.ComplaintId = t1.Id
    ORDER BY c.Id
) AS contact
OUTER APPLY (
    SELECT TOP (1) a.NextActionShamsiDate
    FROM Crm.CustomerComplaintAction AS a
    WHERE a.ComplaintId = t1.Id
    ORDER BY a.NextActionMiladiDate DESC, a.Id DESC
) AS nxt
WHERE t1.ModuleTypeId = 1691
  AND (
        @OwnOnly = 0
        OR (N',' + ISNULL(t1.ViewersIds, N'') + N',') LIKE N'%,' + CONVERT(nvarchar(20), @CuId) + N',%'
      )
""".strip()

COLUMNS = [
    col("t1_Id", "شناسه", 6, visible=False, pk=True),
    col("t1_OwnerType", "نوع", width=100),
    col("t2_Code", "کد مشتری", 7, width=100),
    col("t2_FullName", "نام مشتری", width=230),
    col("t1_CustomerType", "نوع مشتری", width=110),
    col("t1_CustomerTitle", "عنوان مشتری", width=150),
    col("t1_SerialNumber", "شماره سریال", width=110),
    col("t1_CompletionShamsiDate", "تاریخ تکمیل", 5, width=110),
    col("t1_HasAttachment", "پیوست", 1, width=90),
    col("t3_PrePhoneNumber", "پیش‌شماره", 7, width=90),
    col("t3_PhoneNumber", "تلفن", width=110),
    col("t3_MobileNumber", "موبایل", width=110),
    col("t3_ResponserFullName", "نام پاسخگو", width=140),
    col("t3_ResponserJob", "سمت پاسخگو", width=140),
    col("t1_Address", "آدرس", width=140),
    col("t1_ComplaintReceiver", "دریافت کننده", width=150),
    col("t1_ComplaintPresenter", "طرح کننده", width=150),
    col("t1_OrganizationUnitInText", "واحد سازمانی", width=150),
    col("t1_ExpertOrManagerInText", "کارشناس یا مدیر", width=150),
    col("t1_Priority", "اولویت", width=110),
    col("t1_TrackingCode", "کد رهگیری", width=120),
    col("t4_Title", "کانال ارتباط", width=140),
    col("t1_GeneralComplaintSubject", "موضوع کلی", width=120),
    col("t1_GeneralComplaintSubjectTitle", "عنوان موضوع کلی", width=140),
    col("t1_ComplaintType", "نوع شکایت", width=120),
    col("t1_RaisedSubjects", "موضوعات مطرح شده", width=200),
    col("t1_ComplaintMainSubject", "موضوع اصلی", width=180),
    col("t1_Repeatability", "تکرارپذیری", 1, width=110),
    col("t1_HasFinancialLoad", "بار مالی", 1, width=100),
    col("t1_HasUrgencyHandling", "فوریت رسیدگی", 1, width=120),
    col("t1_HasComplexity", "پیچیدگی", 1, width=100),
    col("t5_Title", "اطلاع از دریافت شکایت", width=180),
    col("t1_ComplaintStatus", "وضعیت شکایت", width=160),
    col("t1_HandlingResultIdsInText", "نتیجه رسیدگی", width=160),
    col("t1_InformComplainant", "اطلاع به شاکی", width=120),
    col("t1_ComplaintConsent", "رضایت شاکی", width=110),
    col("t1_FinalComplaintStatus", "وضعیت نهایی", width=120),
    col("t1_FinalStatusValue", "کد وضعیت نهایی", 7, visible=False),
    col("t1_ComplaintClosingShamsiDate", "تاریخ بستن", 5, width=110),
    col("t1_CloseDurationInDay", "مدت بستن (روز)", 7, width=120),
    col("t1_DelayReason", "علت تاخیر", width=160),
    col("t6_NextActionShamsiDate", "تاریخ اقدام بعدی", 5, width=130),
    col("t1_CreatedOnShamsiDateTime", "تاریخ ایجاد", 5, sort_desc=True, width=140),
    col("t1_ModifiedDateShamsiDateTime", "تاریخ ویرایش", 5, width=140),
]

PRINT_SUMMARY = r"""function(ctx) {
  Swal.fire({
    title: 'چاپ',
    html: '<div class="text-end"><label>از تاریخ</label><input id="ccFrom" class="form-control mb-2" placeholder="1404/01/01" /><label>تا تاریخ</label><input id="ccTo" class="form-control" placeholder="1404/12/29" /></div>',
    showCancelButton: true,
    confirmButtonText: 'چاپ',
    cancelButtonText: 'انصراف',
    preConfirm: function () {
      var from = document.getElementById('ccFrom').value;
      var to = document.getElementById('ccTo').value;
      if (!from || !to) { Swal.showValidationMessage('از تاریخ و تا تاریخ را وارد کنید'); return false; }
      return { from: from, to: to };
    }
  }).then(function (r) {
    if (!r.isConfirmed || !r.value) return;
    var q = 'fromDate={\'values\':[\'' + r.value.from + '\'],\'condition\':\'=\'}&toDate={\'values\':[\'' + r.value.to + '\'],\'condition\':\'=\'}&autoRun=True';
    appController.addPage('/System/ReportBuilder/ViewReportByName/CustomerComplaintSummary?' + q);
  });
}"""

PRINT_ACTIONS = r"""function(ctx) {
  var row = ctx.selectedRow || {};
  var id = ctx.primaryKeyValue || row.t1_Id;
  if (!id) { toastr.error('لطفاً یک ردیف را انتخاب کنید'); return; }
  appController.addPage('/System/ReportBuilder/ViewReportByName/CustomerComplaintActions?complaintId={\'values\':[\'' + id + '\'],\'condition\':\'=\'}&autoRun=True');
}"""

SMS_RECEIPT = r"""function(ctx) {
  var row = ctx.selectedRow || {};
  var id = ctx.primaryKeyValue || row.t1_Id;
  var status = String(row.t1_FinalStatusValue == null ? '' : row.t1_FinalStatusValue);
  if (!id) { toastr.error('لطفاً یک ردیف را انتخاب کنید'); return; }
  if (status !== '1183') { toastr.warning('ارسال پیام ثبت شکایت فقط وقتی وضعیت نهایی بسته نشد است'); return; }
  Swal.fire({ title: 'ارسال پیام ثبت شکایت', icon: 'warning', showCancelButton: true, confirmButtonText: 'ارسال', cancelButtonText: 'خیر' }).then(function (r) {
    if (!r.isConfirmed) return;
    post('/Panel/Crm/CustomerComplaint/SendReceiptSms?id=' + id, {}, function (resp) {
      if (!resp.isSuccess) { toastr.error(resp.message || 'خطا', 'خطا'); return; }
      toastr.success('ارسال پیامک با موفقیت انجام شد');
    });
  });
}"""

SMS_CLOSED = r"""function(ctx) {
  var row = ctx.selectedRow || {};
  var id = ctx.primaryKeyValue || row.t1_Id;
  var status = String(row.t1_FinalStatusValue == null ? '' : row.t1_FinalStatusValue);
  if (!id) { toastr.error('لطفاً یک ردیف را انتخاب کنید'); return; }
  if (status !== '1182') { toastr.warning('ارسال پیام پایان شکایت فقط وقتی وضعیت نهایی بسته شد است'); return; }
  Swal.fire({ title: 'ارسال پیام پایان شکایت', icon: 'warning', showCancelButton: true, confirmButtonText: 'ارسال', cancelButtonText: 'خیر' }).then(function (r) {
    if (!r.isConfirmed) return;
    post('/Panel/Crm/CustomerComplaint/SendClosedSms?id=' + id, {}, function (resp) {
      if (!resp.isSuccess) { toastr.error(resp.message || 'خطا', 'خطا'); return; }
      toastr.success('ارسال پیامک با موفقیت انجام شد');
    });
  });
}"""

ATTACHMENT = r"""function(ctx) {
  var row = ctx.selectedRow || {};
  var id = ctx.primaryKeyValue || row.t1_Id;
  if (!id) { toastr.error('لطفاً یک ردیف را انتخاب کنید'); return; }
  appController.addPage('/Panel/Crm/CustomerComplaintAttachment/ListByParentId?complaintId=' + id, true, 'پیوست شکایت');
}"""


def button(bid: str, title: str, script: str, icon: str, color: str, requires: bool = True) -> dict:
    return {
        "id": bid,
        "title": title,
        "dataActionName": bid,
        "colorClass": color,
        "iconClass": icon,
        "requiresSelection": requires,
        "requiresMultiSelection": False,
        "useHtml": False,
        "html": "",
        "actionScript": script,
    }


BUTTONS = [
    button("cc_attachment", "پیوست", ATTACHMENT, "ki-paper-clip ki-outline", "btn-color-primary"),
    button("cc_print", "چاپ", PRINT_SUMMARY, "ki-printer ki-outline", "btn-color-info", False),
    button("cc_print_actions", "چاپ اقدامات انجام شده", PRINT_ACTIONS, "ki-printer ki-outline", "btn-color-info"),
    button("cc_sms_receipt", "ارسال پیام ثبت شکایت", SMS_RECEIPT, "ki-sms ki-outline", "btn-color-success"),
    button("cc_sms_closed", "ارسال پیام پایان شکایت", SMS_CLOSED, "ki-sms ki-outline", "btn-color-success"),
]

ACTIONS = [
    ("/panel/crm/customercomplaint/list", 1, 1),
    ("/panel/crm/customercomplaint/edit", 1, 5),
    ("/panel/crm/customercomplaint/new", 1, 4),
    ("/panel/crm/customercomplaint/save", 2, 3),
    ("/panel/crm/customercomplaint/add", 2, 4),
    ("/panel/crm/customercomplaint/update", 2, 5),
    ("/panel/crm/customercomplaint/delete", 2, 6),
    ("/panel/crm/customercomplaint/fetchdata", 2, 2),
    ("/panel/crm/customercomplaint/exporttoexcel", 2, 1000),
    ("/panel/crm/customercomplaint/sendreceiptsms", 2, 1000),
    ("/panel/crm/customercomplaint/sendclosedsms", 2, 1000),
    ("/panel/crm/customercomplaintcontact/save", 2, 3),
    ("/panel/crm/customercomplaintcontact/delete", 2, 6),
    ("/panel/crm/customercomplaintcontact/edit", 1, 5),
    ("/panel/crm/customercomplaintcontact/new", 1, 4),
    ("/panel/crm/customercomplaintcontact/getlistbyparentid", 2, 2),
    ("/panel/crm/customercomplaintaction/save", 2, 3),
    ("/panel/crm/customercomplaintaction/delete", 2, 6),
    ("/panel/crm/customercomplaintaction/edit", 1, 5),
    ("/panel/crm/customercomplaintaction/new", 1, 4),
    ("/panel/crm/customercomplaintaction/getlistbyparentid", 2, 2),
    ("/panel/crm/customercomplaintattachment/save", 2, 3),
    ("/panel/crm/customercomplaintattachment/delete", 2, 6),
    ("/panel/crm/customercomplaintattachment/edit", 1, 5),
    ("/panel/crm/customercomplaintattachment/new", 1, 4),
    ("/panel/crm/customercomplaintattachment/listbyparentid", 1, 1),
    ("/panel/crm/customercomplaintattachment/getlistbyparentid", 2, 2),
]

READ_PATHS = {
    "/panel/crm/customercomplaint/list",
    "/panel/crm/customercomplaint/fetchdata",
    "/panel/crm/customercomplaint/exporttoexcel",
    "/panel/crm/customercomplaint/sendreceiptsms",
    "/panel/crm/customercomplaint/sendclosedsms",
    "/panel/crm/customercomplaintattachment/listbyparentid",
    "/panel/crm/customercomplaintattachment/getlistbyparentid",
    "/panel/crm/customercomplaintattachment/new",
    "/panel/crm/customercomplaintattachment/edit",
    "/panel/crm/customercomplaintattachment/save",
    "/panel/crm/customercomplaintattachment/delete",
}
OBSERVER_PATHS = {
    "/panel/crm/customercomplaint/list",
    "/panel/crm/customercomplaint/fetchdata",
    "/panel/crm/customercomplaint/sendreceiptsms",
    "/panel/crm/customercomplaint/sendclosedsms",
}
EXPERT_SKIP = {"/panel/crm/customercomplaint/exporttoexcel"}

ROLES = {
    "Crm.Complaint.Manager": ("مدیر سیستم ارتباط با مشتریان", None),
    "Crm.Complaint.Expert": ("کارشناسان شکایات مشتریان", EXPERT_SKIP),
    "Crm.Complaint.Read": ("دسترسی مشاهده سیستم ارتباط با مشتری", None),
    "Crm.Complaint.Observer": ("مشاهده شکایات مشتری", None),
    "Crm.Complaint.OwnItems": ("مشاهده شکایت‌های خود", None),
}


def role_paths(name: str, skip) -> list[tuple]:
    if name == "Crm.Complaint.Manager":
        return ACTIONS
    if name == "Crm.Complaint.Expert":
        return [a for a in ACTIONS if a[0] not in EXPERT_SKIP]
    if name == "Crm.Complaint.Read":
        return [a for a in ACTIONS if a[0] in READ_PATHS]
    return [a for a in ACTIONS if a[0] in OBSERVER_PATHS]


MENU = [{
    "text": "عملیات",
    "icon": "ki-setting-2 ki-outline",
    "iconColor": "#3699ff",
    "path": "",
    "children": [{
        "text": "شکایات مشتری",
        "icon": "ki-message-text ki-outline",
        "iconColor": "#d78819",
        "path": "/panel/crm/customercomplaint/list",
        "a_attr": {"href": "/panel/crm/customercomplaint/list"},
        "data": {"iconColor": "#d78819", "path": "/panel/crm/customercomplaint/list"},
        "children": [],
    }],
}]

ACTION_OPTIONS = [
    {"dataActionName": "new", "enable": True, "title": "جدید"},
    {"dataActionName": "edit", "enable": True, "title": "ویرایش"},
    {"dataActionName": "delete", "enable": True, "title": "حذف"},
    {"dataActionName": "exportExcell", "enable": True, "title": "خروجی اکسل"},
]


def build_profile_sql() -> str:
    grants = []
    manager_grants = []
    for role, (title, _) in ROLES.items():
        for path, atype, itype in role_paths(role, None):
            row = f"    (N'{role}', N'{path}', {atype}, {itype})"
            grants.append(row)
            if role == "Crm.Complaint.Manager":
                manager_grants.append(f"        (N'{role}', N'{path}', {atype}, {itype})")
    grant_sql = ",\n".join(grants)
    manager_sql = ",\n".join(manager_grants)
    return f"""/*
نمایه داده، دسترسی و منوی شکایات مشتری.
اعمال: sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\\Scripts\\Seed_CrmCustomerComplaint_DataProfiles.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = SYSDATETIME();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = {sql_n(SEED_USER)};
    DECLARE @Name NVARCHAR(200) = N'Crm_CustomerComplaint_List';
    DECLARE @Title NVARCHAR(200) = N'شکایات مشتری';
    DECLARE @CustomQuery NVARCHAR(MAX) = {sql_n(QUERY)};
    DECLARE @QueryJson NVARCHAR(MAX) = N'{{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}}';
    SET @QueryJson = JSON_MODIFY(@QueryJson, '$.CustomQuery', @CustomQuery);
    DECLARE @ColumnsJson NVARCHAR(MAX) = {sql_n(compact(COLUMNS))};
    DECLARE @Buttons NVARCHAR(MAX) = {sql_n(compact(BUTTONS))};
    DECLARE @Actions NVARCHAR(MAX) = {sql_n(compact(ACTION_OPTIONS))};
    DECLARE @Events NVARCHAR(MAX) = N'{{"onSelectedRow":"","onRowAdded":""}}';
    DECLARE @Id BIGINT;

    IF EXISTS (SELECT 1 FROM [system].[SavedQuery] WHERE Name = @Name)
    BEGIN
        UPDATE [system].[SavedQuery]
        SET Title = @Title, QueryJson = @QueryJson, ColumnsJson = @ColumnsJson,
            CustomActionButtonsJson = @Buttons, EventScriptsJson = @Events, ActionOptions = @Actions,
            EntityFullName = N'entities.app.crm.customercomplaint', Mode = 1, Type = 1, DiagramJson = N'{{}}',
            ModifiedById = 1, ModifiedByName = @SeedUser, ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi, IsActive = 1
        WHERE Name = @Name;
        SELECT @Id = Id FROM [system].[SavedQuery] WHERE Name = @Name;
    END
    ELSE
    BEGIN
        INSERT INTO [system].[SavedQuery]
            (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
             Mode, Type, EntityFullName, ActionOptions,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (@Name, @Title, @QueryJson, @ColumnsJson, N'{{}}', @Buttons, @Events,
             1, 1, N'entities.app.crm.customercomplaint', @Actions,
             1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
        SET @Id = SCOPE_IDENTITY();
    END

    DECLARE @Roles TABLE (Name NVARCHAR(200), Title NVARCHAR(200));
    INSERT INTO @Roles (Name, Title) VALUES
        (N'Crm.Complaint.Manager', N'مدیر سیستم ارتباط با مشتریان'),
        (N'Crm.Complaint.Expert', N'کارشناسان شکایات مشتریان'),
        (N'Crm.Complaint.Read', N'دسترسی مشاهده سیستم ارتباط با مشتری'),
        (N'Crm.Complaint.Observer', N'مشاهده شکایات مشتری'),
        (N'Crm.Complaint.OwnItems', N'مشاهده شکایت‌های خود');

    INSERT INTO [system].[Role]
        (Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
         CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT r.Name, r.Title, 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1
    FROM @Roles r
    WHERE NOT EXISTS (SELECT 1 FROM [system].[Role] x WHERE x.Name = r.Name);

    DELETE ra FROM [system].[RoleAccess] ra
    INNER JOIN [system].[Role] r ON r.Id = ra.RoleId
    WHERE r.Name LIKE N'Crm.Complaint.%'
      AND (ra.ActionAccessType = 3 OR ra.Path LIKE N'/panel/crm/customercomplaint%');

    INSERT INTO [system].[RoleAccess]
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(@Id AS nvarchar(20)), 3, 7, N'Entities.App.Crm.CustomerComplaint', @Title, @Id, r.Id,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM [system].[Role] r
    WHERE r.Name LIKE N'Crm.Complaint.%';

    INSERT INTO [system].[RoleAccess]
        (Path, ActionAccessType, ActionAccessItemType, EntityName, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT g.Path, g.AType, g.IType, N'Entities.App.Crm.CustomerComplaint', r.Id,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM (VALUES
{grant_sql}
    ) g(RoleName, Path, AType, IType)
    INNER JOIN [system].[Role] r ON r.Name = g.RoleName;

    DECLARE @Menu NVARCHAR(MAX) = {sql_n(compact(MENU))};
    IF EXISTS (SELECT 1 FROM [system].[SystemMenu] WHERE Name = N'CrmSystem')
        UPDATE [system].[SystemMenu]
        SET Title = N'سیستم ارتباط با مشتری', Content = @Menu, IsActive = 1,
            ModifiedById = 1, ModifiedByName = @SeedUser, ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Name = N'CrmSystem';
    ELSE
        INSERT INTO [system].[SystemMenu]
            (Title, Name, Content, AccessRoles, AccessRoleIds,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (N'سیستم ارتباط با مشتری', N'CrmSystem', @Menu, N'[]', N'[]',
             1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);

    DECLARE @RoleIds NVARCHAR(MAX) = (
        SELECT N'[' + STRING_AGG(CAST(Id AS nvarchar(20)), N',') + N']'
        FROM [system].[Role] WHERE Name LIKE N'Crm.Complaint.%');
    DECLARE @RoleNames NVARCHAR(MAX) = (
        SELECT N'[' + STRING_AGG(N'"' + STRING_ESCAPE(Name, 'json') + N'"', N',') + N']'
        FROM [system].[Role] WHERE Name LIKE N'Crm.Complaint.%');
    UPDATE [system].[SystemMenu]
    SET AccessRoleIds = @RoleIds, AccessRoles = @RoleNames
    WHERE Name = N'CrmSystem';

    /* همه منوها: ارتباط با مشتری / عملیات / شکایات مشتری */
    DECLARE @AllItem NVARCHAR(MAX) = N'{{"text":"شکایات مشتری","icon":"ki-message-text ki-outline","iconColor":"#6EE7B7","path":"/panel/crm/customercomplaint/list","a_attr":{{"href":"/panel/crm/customercomplaint/list"}},"data":{{"iconColor":"#6EE7B7","path":"/panel/crm/customercomplaint/list"}},"children":[]}}';
    DECLARE @CrmOrd INT = (
        SELECT TOP 1 TRY_CAST(r.[key] AS int)
        FROM [system].[SystemMenu] m
        CROSS APPLY OPENJSON(m.Content) r
        WHERE m.Name = N'AllMenus' AND JSON_VALUE(r.value, '$.text') = N'ارتباط با مشتری');
    DECLARE @OpOrd INT = (
        SELECT TOP 1 TRY_CAST(c.[key] AS int)
        FROM [system].[SystemMenu] m
        CROSS APPLY OPENJSON(m.Content, CONCAT('$[', @CrmOrd, '].children')) c
        WHERE m.Name = N'AllMenus' AND JSON_VALUE(c.value, '$.text') = N'عملیات');
    IF @CrmOrd IS NOT NULL AND @OpOrd IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM [system].[SystemMenu] m
            CROSS APPLY OPENJSON(m.Content, CONCAT('$[', @CrmOrd, '].children[', @OpOrd, '].children')) x
            WHERE m.Name = N'AllMenus' AND JSON_VALUE(x.value, '$.path') = N'/panel/crm/customercomplaint/list')
        UPDATE [system].[SystemMenu]
        SET Content = JSON_MODIFY(Content, CONCAT('append $[', @CrmOrd, '].children[', @OpOrd, '].children'), JSON_QUERY(@AllItem)),
            ModifiedById = 1, ModifiedByName = @SeedUser, ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Name = N'AllMenus';

    DECLARE @ShowAllId BIGINT = (SELECT Id FROM [system].[Role] WHERE Name = N'ShowAllMenus');
    IF @ShowAllId IS NOT NULL
    BEGIN
        DELETE FROM [system].[RoleAccess]
        WHERE RoleId = @ShowAllId AND Path LIKE N'/panel/crm/customercomplaint%';
        INSERT INTO [system].[RoleAccess]
            (Path, ActionAccessType, ActionAccessItemType, EntityName, RoleId,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        SELECT g.Path, g.AType, g.IType, N'Entities.App.Crm.CustomerComplaint', @ShowAllId,
               1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
        FROM (VALUES
{manager_sql}
        ) g(RoleName, Path, AType, IType);
        IF NOT EXISTS (
            SELECT 1 FROM [system].[RoleAccess]
            WHERE RoleId = @ShowAllId AND ActionAccessType = 3 AND RowId = @Id)
            INSERT INTO [system].[RoleAccess]
                (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId, RoleId,
                 CreatedById, ModifiedById, CreatedByName, ModifiedByName,
                 CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
            VALUES
                (N'dataProfile_' + CAST(@Id AS nvarchar(20)), 3, 7, N'Entities.App.Crm.CustomerComplaint', @Title, @Id, @ShowAllId,
                 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
    END

    COMMIT TRANSACTION;
    PRINT N'Crm_CustomerComplaint_List Id=' + CONVERT(nvarchar(20), @Id);
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT q.Name, q.Title, JSON_VALUE(j.value, '$.DisplayName') AS DisplayName
FROM [system].[SavedQuery] q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'Crm_CustomerComplaint_List'
  AND JSON_VALUE(j.value, '$.Alliance') IN (N't1_SerialNumber', N't1_ComplaintStatus', N't2_FullName');
"""


def border(sides: str) -> str:
    return f"{sides};;;;;;;solid:Black"


def text(name, x, y, w, h, value, *, size=9, ha="Right") -> dict:
    return {
        "Ident": "StiText",
        "Name": name,
        "ClientRectangle": f"{x:.2f},{y:.2f},{w:.2f},{h:.2f}",
        "Interaction": {"Ident": "StiInteraction"},
        "Text": {"Value": value},
        "HorAlignment": ha,
        "VertAlignment": "Center",
        "Font": f"{FONT};{size};;",
        "Border": border("All"),
        "Brush": "solid:",
        "TextBrush": "solid:Black",
        "TextOptions": {"RightToLeft": True, "WordWrap": True},
        "Margins": {"Left": 2, "Right": 2, "Top": 0, "Bottom": 0},
    }


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


def table_report(name: str, title: str, columns: list[tuple[str, str, float]], guid: str) -> dict:
    w = 19.0
    y = 0.0
    header = [text("title", 0, 0, w, 1.0, title, size=14, ha="Center")]
    x = 0.0
    head_cells = []
    data_cells = []
    for key, caption, width in columns:
        head_cells.append(text("h_" + key, x, 0, width, 0.7, caption, ha="Center"))
        data_cells.append(text("d_" + key, x, 0, width, 0.7, "{dtRows." + key + "}"))
        x += width
    bands = [
        {"Ident": "StiReportTitleBand", "Name": "Title", "ClientRectangle": f"0,0,{w},1.00",
         "Components": {"0": header[0]}, "Brush": "solid:"},
        {"Ident": "StiHeaderBand", "Name": "Head", "ClientRectangle": f"0,1.10,{w},0.70",
         "Components": {str(i): c for i, c in enumerate(head_cells)}, "Brush": "solid:230,230,230"},
        {"Ident": "StiDataBand", "Name": "Data", "ClientRectangle": f"0,1.90,{w},0.70",
         "Components": {str(i): c for i, c in enumerate(data_cells)},
         "DataSourceName": "dtRows", "PrintIfEmpty": True, "CanGrow": True, "Brush": "solid:"},
    ]
    page = {
        "Ident": "StiPage", "Name": "Page1", "Guid": guid,
        "Border": ";;2;;;;;solid:Black", "Brush": "solid:",
        "Components": {str(i): b for i, b in enumerate(bands)},
        "PaperSize": "A4", "Orientation": "Landscape",
        "PageWidth": 29.7, "PageHeight": 21.0, "RightToLeft": True,
        "Watermark": {"TextBrush": "solid:50,0,0,0"},
        "Margins": {"Left": 1.0, "Right": 1.0, "Top": 0.8, "Bottom": 0.8},
    }
    cols = [c[0] for c in columns]
    return {
        "ReportVersion": "2024.3.2.0",
        "ReportGuid": guid,
        "ReportName": name,
        "ReportAlias": title,
        "EngineVersion": "EngineV2",
        "CalculationMode": "Interpretation",
        "ReportUnit": "Centimeters",
        "Dictionary": {"DataSources": {"0": data_source("dtRows", cols)}},
        "Pages": {"0": page},
    }


SUMMARY_SQL = r"""
SELECT N'1' AS SortKey, N'تعداد شکایات' AS Section, N'کل' AS Title, N'' AS Detail,
       COUNT(*) AS Number, CAST(100 AS decimal(10,2)) AS Percentage
FROM Crm.CustomerComplaint c
WHERE c.ModuleTypeId = 1691
  AND c.CompletionShamsiDate >= @fromDate AND c.CompletionShamsiDate <= @toDate
UNION ALL
SELECT N'2', N'تعداد شکایات', CASE c.CustomerType WHEN 107 THEN N'حقوقی' WHEN 106 THEN N'حقیقی' ELSE N'نامشخص' END, N'',
       COUNT(*), CAST(ROUND(COUNT(*) * 100.0 / NULLIF(SUM(COUNT(*)) OVER (), 0), 2) AS decimal(10,2))
FROM Crm.CustomerComplaint c
WHERE c.ModuleTypeId = 1691 AND c.CompletionShamsiDate >= @fromDate AND c.CompletionShamsiDate <= @toDate
GROUP BY c.CustomerType
UNION ALL
SELECT N'3', N'واحدهای درگیر', ISNULL(NULLIF(c.OrganizationUnitInText, N''), N'نامشخص'), N'',
       COUNT(*), CAST(ROUND(COUNT(*) * 100.0 / NULLIF(SUM(COUNT(*)) OVER (), 0), 2) AS decimal(10,2))
FROM Crm.CustomerComplaint c
WHERE c.ModuleTypeId = 1691 AND c.CompletionShamsiDate >= @fromDate AND c.CompletionShamsiDate <= @toDate
GROUP BY c.OrganizationUnitInText
UNION ALL
SELECT N'4', N'موضوع شکایت', ISNULL(s.Title, N'نامشخص'), ISNULL(p.Code, N''),
       COUNT(*), CAST(ROUND(COUNT(*) * 100.0 / NULLIF(SUM(COUNT(*)) OVER (), 0), 2) AS decimal(10,2))
FROM Crm.CustomerComplaint c
LEFT JOIN Crm.ComplaintLookupItem s ON s.Id = c.ComplaintSubjectId
LEFT JOIN Inv.Part p ON p.Id = c.PartId
WHERE c.ModuleTypeId = 1691 AND c.CompletionShamsiDate >= @fromDate AND c.CompletionShamsiDate <= @toDate
GROUP BY s.Title, p.Code
UNION ALL
SELECT N'5', N'وضعیت شکایت',
       CASE c.ComplaintStatus WHEN 1168 THEN N'دریافت شکایت' WHEN 1169 THEN N'رسیدگی اولیه' WHEN 1170 THEN N'تکمیل اطلاعات و رسیدگی نهایی'
            WHEN 1171 THEN N'اعلام نتیجه به مشتری' WHEN 1172 THEN N'عدم جلب رضایت و بسته' WHEN 1173 THEN N'جلب رضایت و بسته' ELSE N'نامشخص' END,
       N'', COUNT(*), CAST(ROUND(COUNT(*) * 100.0 / NULLIF(SUM(COUNT(*)) OVER (), 0), 2) AS decimal(10,2))
FROM Crm.CustomerComplaint c
WHERE c.ModuleTypeId = 1691 AND c.CompletionShamsiDate >= @fromDate AND c.CompletionShamsiDate <= @toDate
GROUP BY c.ComplaintStatus
UNION ALL
SELECT N'6', N'وضعیت نهایی',
       CASE c.FinalComplaintStatus WHEN 1182 THEN N'بسته شد' WHEN 1183 THEN N'بسته نشد' ELSE N'نامشخص' END,
       N'', COUNT(*), CAST(ROUND(COUNT(*) * 100.0 / NULLIF(SUM(COUNT(*)) OVER (), 0), 2) AS decimal(10,2))
FROM Crm.CustomerComplaint c
WHERE c.ModuleTypeId = 1691 AND c.CompletionShamsiDate >= @fromDate AND c.CompletionShamsiDate <= @toDate
GROUP BY c.FinalComplaintStatus
UNION ALL
SELECT N'7', N'نوع شکایت',
       CASE c.ComplaintType WHEN 1531 THEN N'بحرانی' WHEN 1532 THEN N'بسیار مهم' WHEN 1533 THEN N'مهم' WHEN 2739 THEN N'سایر' ELSE N'نامشخص' END,
       N'', COUNT(*), CAST(ROUND(COUNT(*) * 100.0 / NULLIF(SUM(COUNT(*)) OVER (), 0), 2) AS decimal(10,2))
FROM Crm.CustomerComplaint c
WHERE c.ModuleTypeId = 1691 AND c.CompletionShamsiDate >= @fromDate AND c.CompletionShamsiDate <= @toDate
GROUP BY c.ComplaintType
ORDER BY SortKey, Number DESC
""".strip()

ACTIONS_SQL = r"""
SELECT c.SerialNumber, c.TrackingCode,
       ISNULL(party.FullName, c.CustomerTitle) AS CustomerName,
       a.ActionShamsiDate, a.ActionDescription, a.NextActionShamsiDate, a.NextActionDescription
FROM Crm.CustomerComplaint c
LEFT JOIN SLS.Customer cust ON cust.Id = c.CustomerId
LEFT JOIN Gnr.Party party ON party.Id = cust.PartyId
LEFT JOIN Crm.CustomerComplaintAction a ON a.ComplaintId = c.Id
WHERE c.Id = TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @complaintId))), N''))
ORDER BY a.ActionMiladiDate, a.Id
""".strip()


def report_sql(name: str, title: str, query: str, content: dict, params: list[tuple[str, str]], prefix: str) -> str:
    p = prefix
    param_json = [{"Id": n, "Name": n, "DisplayName": t, "DefaultValue": "", "DataType": "nvarchar"} for n, t in params]
    shell = {"Tables": [], "Relations": [], "Filters": [], "CustomConditions": None, "CustomQuery": "",
             "Parameters": param_json, "Selects": [{"Index": 0, "Name": "dtRows", "Title": "dtRows"}]}
    inserts = []
    for n, t in params:
        inserts.append(f"""
    IF NOT EXISTS (SELECT 1 FROM dbo.ReportBuilderReportItem WHERE ReportBuilderReportId = @{p}ReportId AND ColumnName = N'{n}')
        INSERT INTO dbo.ReportBuilderReportItem
            (ColumnName, DataType, Title, Show, CanFilter, Required, ReportBuilderReportId, ReportBuilderReportSelectId,
             CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime)
        VALUES (N'{n}', N'nvarchar', {sql_n(t)}, 0, 0, 0, @{p}ReportId, NULL, 1, @SeedUser, @Now, @NowShamsi);""")
    return f"""
    DECLARE @{p}Name NVARCHAR(200) = N'{name}';
    DECLARE @{p}Title NVARCHAR(200) = {sql_n(title)};
    DECLARE @{p}CustomQuery NVARCHAR(MAX) = {sql_n(query)};
    DECLARE @{p}QueryJson NVARCHAR(MAX) = {sql_n(compact(shell))};
    SET @{p}QueryJson = JSON_MODIFY(@{p}QueryJson, '$.CustomQuery', @{p}CustomQuery);
    DECLARE @{p}ColumnsJson NVARCHAR(MAX) = N'[]';
    DECLARE @{p}Content NVARCHAR(MAX) = {sql_n(compact(content))};
    DECLARE @{p}SavedQueryId BIGINT;
    IF EXISTS (SELECT 1 FROM [system].[SavedQuery] WHERE Name = @{p}Name)
    BEGIN
        UPDATE [system].[SavedQuery]
        SET Title = @{p}Title, QueryJson = @{p}QueryJson, ColumnsJson = @{p}ColumnsJson, EntityFullName = NULL,
            Mode = 1, Type = 0, ActionOptions = N'', CustomActionButtonsJson = N'[]',
            EventScriptsJson = N'{{"onSelectedRow":"","onRowAdded":""}}', DiagramJson = N'[]',
            ModifiedById = 1, ModifiedByName = @SeedUser, ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi, IsActive = 1
        WHERE Name = @{p}Name;
        SELECT @{p}SavedQueryId = Id FROM [system].[SavedQuery] WHERE Name = @{p}Name;
    END
    ELSE
    BEGIN
        INSERT INTO [system].[SavedQuery]
            (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
             Mode, Type, EntityFullName, ActionOptions, CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (@{p}Name, @{p}Title, @{p}QueryJson, @{p}ColumnsJson, N'[]', N'[]', N'{{"onSelectedRow":"","onRowAdded":""}}',
                1, 0, NULL, N'', 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
        SET @{p}SavedQueryId = SCOPE_IDENTITY();
    END
    IF EXISTS (SELECT 1 FROM dbo.ReportBuilderReport WHERE Name = @{p}Name)
        UPDATE dbo.ReportBuilderReport
        SET Title = @{p}Title, BaseQuery = @{p}CustomQuery, SavedQueryId = @{p}SavedQueryId, Type = 1, ObjectType = N'SavedQuery',
            Content = CASE WHEN Content IS NULL OR ModifiedByName = @SeedUser THEN @{p}Content ELSE Content END,
            ModifiedById = 1,
            ModifiedByName = CASE WHEN Content IS NULL OR ModifiedByName = @SeedUser THEN @SeedUser ELSE ModifiedByName END,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi, IsActive = 1
        WHERE Name = @{p}Name;
    ELSE
        INSERT INTO dbo.ReportBuilderReport
            (Name, Title, ObjectType, SavedQueryId, Type, Content, BaseQuery,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (@{p}Name, @{p}Title, N'SavedQuery', @{p}SavedQueryId, 1, @{p}Content, @{p}CustomQuery,
                1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
    DECLARE @{p}ReportId BIGINT = (SELECT Id FROM dbo.ReportBuilderReport WHERE Name = @{p}Name);
    {''.join(inserts)}
"""


def build_report_sql() -> str:
    summary = table_report(
        "CustomerComplaintSummary", "خلاصه شکایات مشتری",
        [("Section", "بخش", 4), ("Title", "عنوان", 6), ("Detail", "جزئیات", 4), ("Number", "تعداد", 2.5), ("Percentage", "درصد", 2.5)],
        "c1a0b2d4-6e18-4a77-9f30-1b2c3d4e5f60")
    actions = table_report(
        "CustomerComplaintActions", "اقدامات شکایت",
        [("SerialNumber", "سریال", 2.5), ("TrackingCode", "رهگیری", 2.2), ("CustomerName", "مشتری", 4),
         ("ActionShamsiDate", "تاریخ", 2.3), ("ActionDescription", "شرح", 4),
         ("NextActionShamsiDate", "تاریخ بعدی", 2.3), ("NextActionDescription", "شرح بعدی", 3.7)],
        "d2b1c3e5-7f29-4b88-a041-2c3d4e5f6071")
    body = report_sql("CustomerComplaintSummary", "خلاصه شکایات مشتری", SUMMARY_SQL, summary,
                      [("fromDate", "از تاریخ"), ("toDate", "تا تاریخ")], "Sum")
    body += report_sql("CustomerComplaintActions", "چاپ اقدامات انجام شده", ACTIONS_SQL, actions,
                       [("complaintId", "شناسه شکایت")], "Act")
    return f"""/*
قالب چاپ خلاصه و اقدامات شکایات مشتری.
اعمال: sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\\Scripts\\Seed_CrmCustomerComplaint_Reports.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = SYSDATETIME();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = {sql_n(SEED_USER)};
    {body}
    COMMIT TRANSACTION;
    PRINT N'Customer complaint reports ready';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
"""


def main() -> None:
    SQL_PROFILE.write_text(build_profile_sql(), encoding="utf-8-sig")
    SQL_REPORT.write_text(build_report_sql(), encoding="utf-8-sig")
    print("profile", SQL_PROFILE.stat().st_size)
    print("report", SQL_REPORT.stat().st_size)


if __name__ == "__main__":
    main()
