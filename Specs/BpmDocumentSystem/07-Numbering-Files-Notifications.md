# Numbering, files, ابلاغ, email / notification

> Prerequisite: `00` (enums, trigger revision rules), `03` (EntityAction hooks, module setting), `05` (when notify happens), `06` (form-type download).

## 1. Numbering — `ProcessDocumentAction` BeforeSave

Port `BpmDocumentService.GetDocumentNumber` + the **fixed** `GetLatestValidDocument` (exclude cancel statuses; do **not** require StatusId > 10). Havayar improvements that are **required** (HTS bugs, not behaviour we want):

1. **Numeric max, not `ORDER BY DocumentNumber` string sort.** Live data has `F-9` and `F-575`; string desc would pick `F-9` and mint a colliding `F-10`. After sync, compute `MAX(parsed suffix)` among valid rows whose number starts with the type prefix.
2. **Pad width:** HTS first number is `{Code}-001`; subsequent `PadLeft(digitCount: 2)` so values look like `R-02`, `F-575`. Live catalog is mixed (`R-1` … `W-150`, `WT-02`). Next number = `(max suffix) + 1` with `PadLeft(2)` (no upper cap). If no valid predecessor: `{LookupCode}-001` (HTS return for the empty catalog).
3. Case-insensitive prefix match (`W-` vs `w-` both exist).

### Rules (HTS switch)

| Request type | Document type | Result |
|---|---|---|
| 1720 حذف, 1721 بازنگری | any | Return **existing** `DocumentNumber` (from parent). Never mint. |
| 1719 ایجاد | R P W WT C F M T (`1724,1732,1733,1734,1735,1793,1794,1823`) | `{LookupCode}-n`. One-letter codes are stored with a hyphen (`R-26`). Two-letter `WT` uses `WT-02` in live data. |
| 1719 | CL (`1725`) | `CL-{ProcessSet.LookupCode}-nn` (2-digit). If no predecessor: `CL-{code}-01`. Live TotalSystem has **one** CL row numbered `HI-BM` (data quality); algorithm still applies for new rows. |
| 1719 | anything else (FL, BPM, PI, IN, QM, QP, CO, WM, HI, ST, …) | Return empty; supervisor types the number on Manage. |
| 1722 / 1723 | any | Empty (fall-through in HTS). |

`GetLatestValidDocument` validity (fixed version):

- Number starts with the prefix
- RequestType **not** in {1720, 1721}
- Has at least one StatusLog
- **None** of the logs are `CancelRequest(22)` or `BpmCancelRequest(23)`

Supervisor save: if `isBySupervisor` and number is empty, regenerate when `DocumentType` changed, or when `ProcessSet` changed **and** type is CL (1725). Then `MoveFilesToRightPath` if number/type changed (`07` §2).

### Revision (EntityAction, from trigger `UpdateBpmDocumentTrigger`)

Skip if `Comment == "Created by system owner"` or `IsDeprecate`.

- RequestType 1719 → `Revision = 0`
- RequestType ≠ 1721 and ParentId set → copy parent Revision; else 0
- RequestType 1721 → **do not overwrite** (request page already set parent+1)

## 2. Files

Panel `FileEntity` upload. No Session lists.

Logical path (HTS `UploadDocumentFiles` / `UploadDocumentFilesWithRevision`):

```
{ProcessFilesRoot}/{DocumentType.TitleFa}/{DocumentNumber}/Rev{Revision}/{filename}
```

Temp uploads may land under `ProcessFilesTempFolder` (`1_Temp`) until notify, then `MoveFilesToRightPath` rewrites paths when type/number changes or status becomes Notified.

On notified **حذف (1720)**: `MoveDocumentToHistory` → `ProcessFilesHistoryFolder` (`2_History`).

Name collision: HTS appends `_yyyyMMdd_hhMMssfff`. Port.

Max size on request main/temp: **20480 KB**, count **1**. Manage final/pdf/comment: same order of magnitude; follow the request-page cap unless a manage view specifies otherwise.

Sync (`08`): create `FileEntity` rows with `PhysicalPath` = HTS `*FilePath` UNC; do not copy bytes unless the Havayar file service cannot read that UNC.

## 3. Deprecate on ابلاغ

HTS `CheckForDeprecateDocument` (called from `SendAnnouncementNotificationEmail`):

If request is **1720**, or **1721 with ParentId**:

```
set IsDeprecate = true
append Comment += "- منسوخ شده در تاریخ {persian now}"
```

on every other document with `Id == ParentId` **or** (`Id != current` and same `DocumentNumber` case-insensitive).

Run this in EntityAction / notify pipeline **even if email fails**? HTS runs it **before** composing the email, so deprecate happens even when later recipient lists are empty and the method `return`s. Port: deprecate first, then email; empty recipient list still deprecates.

## 4. Module setting

`Bpm.ProcessDocumentModuleSetting` (`03`): `LastAnnouncementNumber` (seed from HTS **446** on sync). Increment **only if** `EmailJob` send succeeded (HTS `if (!emailResultIsSuccess) return;` then updates config).

Also store always-CC and conditional-CC JSON so `mirlohi.m`, `sajdeh.n` → `Yaghyaei.m`, `momenirad.s` → personnel emails of HTS org unit **268** (سیستم‌ها و روش‌ها) are **not** hardcoded.

## 5. Emails

Use `EmailJob` + in-app `NotifitactionBuilder` where a user should get a bell as well as mail. Subjects and HTML below are copied from HTS (Zar font / `#000aa0` header can be restyled to Havayar email chrome, but **text and columns stay**).

### 5.1 Requester add/update/cancel — `SendEditOrUpdateEmail`

- Subject: `اعلان اصلاح/حذف سند`
- Body (not supervisor): `درخواست ایجاد/ تغییر سند یا سیستم با شماره درخواست {Id} توسط درخواست دهنده {ایجاد|اصلاح|لغو} گردید`
- Body (supervisor save): same with «توسط سرپرست», plus `<ul>`: شماره درخواست، نام سند، نوع سند، کد سند، مجموعه فرآیندی، مالک، تایید کننده، تصویب کننده، تاریخ پیش بینی.
- To: AD usernames of group **353**; if supervisor, also **354**. Apply `GetHavayarEmailSuffix`.
- Conditional CC from settings.

Fired on requester Update/Cancel (`04`) and supervisor metadata save (`05`). Not on every status change.

### 5.2 Status-change — `BpmDocumentDetailService.SendNotificationEmail`

HTS sends **two** flavours (`isSendForCreator`):

| Status | `isSendForCreator` | Subject | To | CC / groups |
|---|---|---|---|---|
| Issue (1), Reject (9) | true | Issue: `اعلان درخواست ایجاد/تغییر سند`; else `اعلان تایید درخواست ایجاد/تغییر سند` | creator AD | group 353 |
| InProgress (10) | both true and false (two emails) | creator mail as above; expert mail `اعلان اسناد در انتظار اقدام کارشناس` | creator; groups 353+354 | |
| BpmExpertApproved (19) | false | `اعلان اسناد در انتظار اقدام سیستم ها وروش ها` | groups 353+354 | *(no extra To)* |
| BpmApproved (11) | false | `اعلان اسناد در انتظار اقدام` | process owner | group 353 |
| ProcessOwnerApproved (13) | false | `اعلان تایید/کامنت مالک مجموعه فرآیندی` | approver | owner CC, group 353 |
| ProcessOwnerCommented (12) | false | same subject | (see HTS remainder — owner comment mail to supervisor groups) | |
| ApproverApproved (15) / ApproverCommented (14) | false | analogous, To = final approver / groups | |
| FinalApproverApproved (16) / Commented (17) | false | analogous | |
| BpmCancelRequest (23), BpmReject (21) | false | `اعلان رد درخواست` | creator | groups 353+354 |
| BpmAcceptComments (24) | false | same as InProgress expert mail | groups 353+354 | |

A second, older HTML (`SendNotification` private) lists عنوان سند، کد سند، نوع سند، وضعیت فعلی (`TitleInText`) and mails creator+updater To, CC unit 268 + owner/approver/final emails from `PrsEmail`. Port **only if** it is still called — in current `DoOperation` the `Task.Run(() => SendNotificationEmail(...))` path is the one above. Implement the `SendNotificationEmail` matrix; skip the unused `SendNotification` helper unless a remaining call site is found.

**Body blocks to keep (Persian):**

Shared header: `گروه صنعتی هوایار`.

Creator block (`GetCreatorEmailContent`): سلام + وضعیت سند … تغییر یافت / درخواست ثبت شد, then list عنوان، کد، نوع، وضعیت فعلی (`TitleInText`).

Expert block (`GetExpertEmailContent`): «در انتظار اقدام کارشناس» + request id, title, number, type, priority, requester, requester unit, forecast date.

Supervisor/owner blocks (`GetSupervisorEmailContent` / `GetProcessOwnerEmailContent`): «جهت تعیین وضعیت به کارتابل شما ارسال شده است» + شماره درخواست، شرح درخواست، درخواست کننده، واحد درخواست کننده. Owner-approved variant includes last owner comment when `isComment=true`.

Implement the remaining Get* helpers by reading `BpmDocumentDetailService.cs` from `GetProcessOwnerEmailContent` onward when coding `07` — do not invent new sentences.

### 5.3 ابلاغ — `SendAnnouncementNotificationEmail`

Subject: `ابلاغیه سیستم مدیریت یکپارچه اسناد` (falls back from `سیستم مدیریت یکپارچه اسناد`).

Header table:

- Right: «ابلاغیه» / «مستندات سیستم مدیریت یکپارچه» / «آخرین ویرایش مستندات زیر در سیستم HTS بارگذاری گردیده و از تاریخ ابلاغ لازم الاجرا می‏باشند.» — replace «سیستم HTS» with the Havayar product name in the new text, keep the rest.
- Left: `شماره ابلاغیه : {nnn}-{year}` where `{nnn}` is `LastAnnouncementNumber+1` zero-padded to 3 (`{announcementNumber:000}`) and year is Persian year; `تاریخ : {persian date}`.

Columns (exact headers):

| شرح اقدام | نام سند / سیستم | کد سند | مجموعه فرآیندی | مالک مجموعه فرآیندی | واحدهای مجری/ همکار | توضیحات |
|---|---|---|---|---|---|---|
| RequestType title | DocumentTitle | DocumentNumber | ProcessSet title + code | ProcessOwner name | ProcessExecuter **names** (`ProcessExecuterIdsInText`) | last StatusLog comment |

If AccessLevel is محرمانه or محدود: **no file attachment**; extra line:

`جهت مشاهده و دریافت فایل به آدرس سامانه جامع هوایار / سیستم مدیریت اسناد / اسناد فرآیندی / لیست اسناد فرآیندی مراجعه نموده و فایل، با شناسه {Id} را دانلود نمایید`

(Update the path words to the new menu if the Panel title differs, keep the instruction.)

If عمومی: attach Final file when type **1793 فرم**, else FinalPdf if present. Skip missing files.

Recipients:

- Always CC: setting `AnnouncementAlwaysCc` (HTS `mirlohi.m`) + AD of groups 353 and 354
- Org-unit emails (`PrsEmail`, active personnel) for Beneficiary units + Executer units (**not** for محرمانه executer units)
- User AD emails for NotificationRecipients
- Editing **org units** → personnel emails (see `03`: do not treat those ids as user ids)
- If all of beneficiary, executer, recipients, editing are empty, HTS **returns without sending** (after already deprecating). Port that.

On success: grant `Bpm.ProcessDocument.PublishedList` to notification recipient users; if محدود, also to users whose personnel org unit is in executer units (HTS `AddUsersPermission(UserGroup.BpmDocumentReport, …)`). Then increment `LastAnnouncementNumber`.

## 6. In-app notifications

Mirror the email audience with `NotifitactionBuilder` rules on `ProcessDocument` / `ProcessDocumentStatusLog` create. Do not build a parallel recipient engine — EntityAction after status insert can enqueue the same user ids as the email matrix.

## 7. Acceptance criteria

1. First R document in an empty catalog → `R-001`; next after live max suffix `26` → `R-27` (not `R-10` from string sort).
2. CL with process MS → `CL-MS-01` (or next 2-digit). Code for process 1760 is `TI`.
3. Delete/review never mint a new number; review revision is parent+1; create revision is 0.
4. Valid-latest ignores 22/23 even if LastStatus was notified.
5. ابلاغ of 1720/1721 deprecates parent and same-number siblings and appends the Persian obsolete sentence.
6. محرمانه/محدود ابلاغ has no attachment and includes the “go to published list” sentence; عمومی فرم attaches Final, others FinalPdf.
7. Announcement number becomes 447 only if mail succeeded (starting from synced 446).
8. CC list comes from module setting, not `mirlohi.m` literals in C#.
9. Files live in `FileEntity`; no Session; notify moves off `1_Temp`; delete-notify moves to `2_History`.
10. No SQL trigger; numbering/last-status/revision are EntityAction.
