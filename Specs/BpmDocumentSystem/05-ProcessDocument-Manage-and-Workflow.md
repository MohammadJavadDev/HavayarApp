# ProcessDocument — مدیریت درخواست‌ها و ماشین وضعیت

> Prerequisite: `00` §7–§8 (transitions + trigger behaviour), `03`, `04`. Notifications/numbering details in `07`.

## 1. Purpose

Supervisor / expert / process-owner / approver / final-approver workplace: filter the manage cartable, fill process metadata, and record **exactly** the HTS status transitions.

Legacy: `BpmManageDocumentController` (`GetGridData`, `DoOperation` with `isBySupervisor: true`, `DoDocumentDetailOperation`), `_BpmManageDocument.cshtml` (`bpmManageDocument_loadDocumentStatus`, `bpmManageDocument_UpdateLayout`, validate). `SystemPage.Bpm_ManageDocuments = 447`.

New: `/panel/bpm/processdocument/manage` (list) + manage Edit (or the same `Edit.cshtml` with a `isManage=1` flag). Prefer **one Edit page** with extra fields + workflow buttons shown by role/status (pattern: `WebApp/Views/Panel/Prd/StopRequst/Edit.cshtml`).

## 2. Manage cartable `FetchData` (copy `GetGridData`)

Supervisor status set (HTS `supervisorStatusIds`):

`1 Issue, 2 Updated, 20 BpmUpdated, 12 ProcessOwnerCommented, 14 ApproverCommented, 17 FinalApproverCommented, 16 FinalApproverApproved, 18 Notified, 19 BpmExpertApproved`

```
if (Admin || ShowAll on this page)
    all rows
else if (Supervisor role)
    LastStatus in supervisorStatusIds
    OR LastStatus == InProgress(10)
    OR (LastStatus == BpmApproved(11) AND ProcessOwnerId == me)
    OR (LastStatus == ProcessOwnerApproved(13) AND ApproverId == me)
    OR (LastStatus == ApproverApproved(15) AND FinalApproverId == me)
else if (Expert role)
    LastStatus in { InProgress(10), BpmReject(21), BpmAcceptComments(24) }
    OR LastStatus in supervisorStatusIds
else
    (LastStatus == 11 AND ProcessOwnerId == me)
    OR (LastStatus == 13 AND ApproverId == me)
    OR (LastStatus == 15 AND FinalApproverId == me)
```

**HTS fact:** experts also see the wide `supervisorStatusIds` list (including Issue and even Notified). Port that; do not “simplify” to only 10/21/24.

## 3. Who may open the transition UI (`UpdateLayout` case Initial)

Enable the “تعیین وضعیت” action when:

| LastStatus | Who |
|---|---|
| 1, 2 | Supervisor |
| 12, 14, 16, 17, 19, 20 | Supervisor **and** `DocumentType` is not null |
| 10, 21, 24 | Expert **and** `DocumentType` is not null |
| 11 | ProcessOwner **or** admin |
| 13 | Approver **or** admin |
| 15 | FinalApprover **or** admin |

Force-notify checkbox (`IgnoreProcessAndForceToNotify`) is a **supervisor** control; when checked, require `NotificationShamsiDate`, skip process-set/owner/approver/final-approver required checks, write log **26 then 18** (HTS `DoDocumentDetailOperation`).

## 4. Allowed transitions (invalid otherwise)

Copy `00` §7. Server must reject any other `Status` even if the client combo is tampered.

On `BpmApproved(11)`: if `ProcessOwnerId == FinalApproverId`, after inserting 11 insert automatic 13 with comment:

`بصورت اتوماتیک بعلت تطابق مالک و تصویب کننده`

(`CreatedById` = that user). HTS does this inside `BpmDocumentDetailService.DoOperation`.

## 5. Fields the supervisor/expert may edit (`isBySupervisor`)

HTS `DoOperation(..., isBySupervisor: true)` copies onto the document:

- DocumentType, DocumentNumber, AccessLevel, ProcessSet, ProcessOwner, Approver, FinalApprover
- CompletionForecast date
- Editing / executer / process-executer junctions (CSV in HTS)
- Then `AddUsersPermission(group 418)` for the distinct owner/approver/final-approver user ids → Havayar: ensure those users have `Bpm.ProcessDocument.ViewManage` (Role seed / group membership equivalent; do not invent a SQL user-group table if RoleAccess already covers it — `08`)

`DoDocumentDetailOperation` additionally copies, when present: title, access, process/approvers, send/receive dates on the **log**, notification date, beneficiary, related documents text, notification recipients, related units, and file uploads.

When moving to **InProgress (10)** (and not force-notify), HTS first saves the document as supervisor-update (which itself inserts `BpmUpdated (20)` via `AddDetail`!) **then** inserts InProgress. Live data rarely shows 20 because LastStatus is overwritten by the following InProgress log. **Havayar:** do **not** insert a spurious BpmUpdated when the user is actually transitioning to InProgress. Only insert the StatusLog for the chosen next status. (HTS double-write is a quirk; LastStatus would flicker to 20 then 10. Spec: one log row = the chosen status. Supervisor **Save without a status change** still inserts `BpmUpdated (20)` — that path is the manage “save metadata” button.)

Split buttons:

- **ذخیره مشخصات** → supervisor update, StatusLog `BpmUpdated (20)` if the requester-facing fields/process fields changed without a transition.
- **ثبت وضعیت** → insert StatusLog with the selected next status (and the InProgress field freeze below).

### Required when next status is InProgress (10) or BpmAcceptComments (24), unless force-notify

HTS `validatePropertyGrid`: DocumentType, DocumentNumber (if the number box is visible), ProcessSet, ProcessOwner, Approver, FinalApprover. CompletionForecast if that box is visible.

Reject/comment statuses **9, 12, 14, 17, 21** require a description (`StatusLog.Comment`).

When next status is **11** and the main-file box is visible (supervisor on 19): HTS requires a newly selected main file if the control is shown. Port if the layout shows it.

When final-file box is visible (supervisor on 16) and request type ≠ 1720: require Final file. HTS also requires FinalPdf when that box is shown (`validate` continues after the snippet).

Expert on 10/21/24: show SendDate, ReceiveDate, Comment file.

## 6. Downloads on this page

HTS manage download serves Main, Temp, Final, FinalPdf, Comment.

Temp file extra rule (`UpdateLayout`):

- Supervisor / Expert / ViewAllAttachments → can download temp.
- Else: temp download is enabled when the current user is **not** owner/approver/final-approver (the HTS condition is inverted English: `if (owner !== me && approver !== me && final !== me) enable`). Port **that** predicate; do not “fix” it without a product decision.
- Comment file: supervisor or expert only (plus ViewAllAttachments if you align with the comment-file click guard).

## 7. Controller

Same `ProcessDocumentController` extra actions **or** `ProcessDocumentManageController` sharing the entity. Each action needs `[ActionDisplayName]` so RoleAccess can grant Manage separately from Request.

| Action | Role |
|---|---|
| `Manage` (GET list) / `FetchManageData` | View / List on manage |
| `ManageEdit(id)` | View |
| `SaveManage` | Update — supervisor metadata, `isBySupervisor` |
| `ChangeStatus` | Update — body: `{ id, status, comment, sendDate, receiveDate, ignoreProcessAndForceNotification, notificationDate, junctions, files }` |
| `Download*` | View + ViewAllAttachments where required |

`ChangeStatus` algorithm:

1. Load document; verify cartable access and §3 permission.
2. Verify `status` ∈ allowed set for `LastStatus` (§4). If `ignoreProcessAndForceNotification`: ignore combo; write 26 then 18.
3. Apply supervisor field updates (type, number, process, people, junctions, dates, files).
4. If transitioning to InProgress: generate/move number if type/process changed (`07`).
5. Duplicate-number check on notify for request type 1719 (HTS): if another non-deprecate, non-cancel document has same `DocumentNumber` **and** `DocumentTitle`, refuse with the HTS message «شماره سند در اسناد دیگر استفاده شده».
6. Insert StatusLog (and auto-13 when needed).
7. If final status is Notified: run deprecate + announcement email (`07`); if request type 1720, move files to history.
8. `appController.refreshCurrentPage()` on the client.

## 8. View

Manage list: datatableprofile with manage FetchData.

Manage edit sections:

1. Read-only requester block (type, title, source, priority, comment, main/temp links).
2. Supervisor block: document type, number, process set, owner, approver, final approver, access, forecast date, notify date, related documents, editing units, executer units, process executers, notify recipients, related units, beneficiaries.
3. Transition block: status `<select>` filtered to allowed next ids (labels = `TitleInView` if set else `TitleInText`), force-notify checkbox, description, send/receive, file slots per §5.
4. Status log child grid.

Workflow buttons via `form-action-buttons` extra actions, shown with the same predicates as §3. Use `$$.post('changeStatus', …)` + `Swal`/`$.confirm` for reject comments (`rtl: true`). Never `location.reload()`.

## 9. Acceptance criteria

1. A user who is only process owner sees manage rows in status 11 assigned to them — not the whole Issue queue.
2. Supervisor can take 1/2/20 → 9 or 10; expert can take 10/24 → 19; nobody can jump Issue → Notified except via force-notify (26 then 18).
3. Status combo on the client **and** server allow only the table in `00` §7.
4. Owner = final approver + transition to 11 produces two log rows (11 then 13) with the exact automatic comment.
5. Requester Cancel remains 22; supervisor/expert «رد درخواست» is 23.
6. Junctions persist as rows, not CSV; group 418 equivalent is granted to owner/approver/final-approver on supervisor save.
7. Force-notify writes 26 and 18, requires notify date, skips process-owner required fields.
8. After ChangeStatus, `LastStatus` equals the latest StatusLog Id’s status (EntityAction, not SQL).
9. No service class; no EDMS types; refresh via `appController.refreshCurrentPage()`.
