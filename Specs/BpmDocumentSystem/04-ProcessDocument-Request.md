# ProcessDocument — صفحه «درخواست ایجاد/تغییر»

> Prerequisite: `00` (enums, roles, cartable rules), `03` (entity). Manage/workflow is `05`; published list is `06`.

## 1. Purpose

Requester-facing page: create or edit a process-document **request**, attach main/temp files, pick a parent for delete/review, and see the requester cartable.

Legacy: `BpmDocumentController` (`LoadPage`, `GetGridData`, `DoOperation`, `DownloadDocumentFiles`), `_BpmDocument.cshtml`. `SystemPage.Bpm_Document = 446`. HTS URL `shared-bpmDocument`.

New route prefix: `Panel/Bpm/ProcessDocument`. List = `/panel/bpm/processdocument/list`. Edit = standard `Edit`/`New`.

## 2. Request form fields (only these — process fields belong on Manage)

HTS property grid (`_BpmDocument.cshtml` `PropertyGridSection`):

| UI | Bind | Required | Rules |
|---|---|---|---|
| نوع درخواست | `RequestType` | yes | Enum 229 |
| سند (والد) | `ParentId` | **yes when type is 1720 or 1721** on Add | EntitySelector of **Notified and not deprecate** docs (`FillData`: `LastStatusId == 18 && !IsDeprecate`). Display `{DocumentNumber} \| {DocumentTitle} Ver: {Revision}` |
| نوع سند | `DocumentType` | yes **except** 1722 / 1723 | On **1719**, exclude types 1727, 1730, 1731 from the dropdown. On 1720/1721: locked, copied from parent |
| عنوان سند | `DocumentTitle` | yes (for 1722/1723, HTS auto-fills from request-type text if empty) | Max 128. Locked on 1720/1721 |
| سطح دسترسی | `AccessLevel` | yes | Locked on 1720/1721 (from parent) |
| ورودی سند | `Source` | yes | |
| فوریت | `Priority` | yes | |
| درخواست‌کننده | `RequesterUserId` | yes | EntitySelector; HTS FillData = active or ForceLogin users with personnel + job. Default = current user |
| شرح درخواست | `Comment` | yes | |
| فایل اصلی | `MainFileId` | not enforced in current HTS (the “must upload” block is commented out) | Max **1** file, max **20480 KB**. On 1720/1721 HTS names the upload from the parent title |
| فایل متفرقه | `TempFileId` | no | Same size/count cap |

On Add, also set `CreatedOrganizationUnitId` from the current user’s org unit (HTS `OrganizationUnitId`).

**Not on this form:** process set, owner, approver, final approver, org-unit multi-selects, forecast/notify dates, final/pdf/comment files. Those are supervisor fields (`05`).

### Parent select behaviour (`bpmDocument_cmbDocuments_onChange`)

Copy parent `DocumentType`, `DocumentTitle`, `AccessLevel`. If request type ≠ 1720 and parent `Revision` is not null: `Revision = parent.Revision + 1`. Delete (1720) does **not** bump revision (trigger later copies parent revision — `00` §8).

### Document number on this page

- 1720 / 1721: copy `DocumentNumber` from parent; `GetDocumentNumber` returns it unchanged.
- 1719: EntityAction BeforeSave generates the number (`07`). Requester does not type it.
- 1722 / 1723: left empty unless later filled on Manage.

## 3. Save / status log

HTS `BpmDocumentService.DoOperation`:

| Operation | StatusLog inserted | Email |
|---|---|---|
| Add | `Issue (1)` | creator-style notify (`07`) |
| Update (requester) | `Updated (2)` | «اعلان اصلاح/حذف سند» to group 353 |
| Delete | **not physical** — insert `CancelRequest (22)` | same edit/cancel email, `isBySupervisor = false` |

Port:

- Add → insert StatusLog Issue (EntityAction AfterAdd on document **or** controller after save; StatusLog AfterAdd then stamps `LastStatus`).
- Update allowed only in statuses **1, 2, 9** (`Issue` / `Updated` / `Reject`) and only for **admin or `CreatedById == current user`** (HTS `statusIds = [1, 2, 9]`).
- “Delete” button → confirm with `Swal.fire` → POST that inserts CancelRequest(22). Do not `Repository.Delete` the document.

Supervisor updates from the manage page use `BpmUpdated (20)` instead of `Updated (2)` (`isBySupervisor: true`). That path is `05`, not this page.

## 4. Cartable `FetchData` (copy `GetGridData` exactly)

```
if (IsAdministrator || CurrentUserHasAnyRole("Bpm.ProcessDocument.ShowAll"))
    all ProcessDocument rows
else if (user has Supervisor OR Expert role)   // HTS groups 353/354 via UserGroup membership
    all rows whose LastStatus is NOT Notified(18)
else
    rows where (CreatedById == me OR ModifiedById == me)   // HTS CreatedUserId OR UpdatedUserId
    AND LastStatus is NOT Notified(18)
```

**HTS fact vs plan wording:** plan said “فقط رکوردهای خود کاربر”. Live code is `CreatedUserId == UserId || UpdatedUserId == UserId`. Use that.

ShowAll on **this page** is a distinct RoleAccess from ShowAll on Manage/Published (HTS per-`SystemPage` permission). Same role name is acceptable if RoleAccess is granted per action; otherwise suffix the action (`ShowAll` on `List`/`FetchData` of the request controller).

## 5. Controller actions

`ProcessDocumentController`:

| Action | `[ActionDisplayName]` | Notes |
|---|---|---|
| `List` / `FetchData` | View / List | Cartable filter above |
| `New` / `Edit` | View / Insert or Update | |
| `Save` / `Add` / `Update` | Insert/Update | Requester field set only; ignore posted process-owner fields |
| `CancelRequest` | Update or Delete | Maps HTS Delete → status 22 |
| `DownloadMain` / `DownloadTemp` | View | Stream `FileEntity`; 404 if missing |

Do not accept `LastStatus` from the client on this page.

Downloads: HTS request page only serves Main and Temp (`DocumentFileType` 0 and 16). Final/PDF/Comment stay on Manage/Published.

## 6. Views

### `List.cshtml`

`<datatableprofile entity-Type="typeof(ProcessDocument)"></datatableprofile>`

Suggested columns (HTS grid): Id, LastStatus, Title, Number, Revision, RequestType, DocumentType, AccessLevel, Source, Priority, Requester, CreatedOn.

Row actions: open Edit when status ∈ {1,2,9} and user is creator/admin; download main/temp when files exist.

### `Edit.cshtml`

Request fields from §2, `form-group-inline`, enum `<select>`s, EntitySelectors for parent + requester, `SystemType.File` for main/temp.

Child: status-log grid (`ListByProcessDocumentId` or an inline table loaded with `$$.get`) — read-only.

Show/hide parent selector when RequestType is 1720/1721 (`page.$pageEl.on('change', …)`).

`savefn` canonical. After success `page.$pageEl.dataBind(r.data)` and `appController.refreshCurrentPage()` only if status log must appear immediately.

Cancel button: `Swal.fire` then `$$.post('cancelRequest', { id })`.

## 7. Acceptance criteria

1. New «ایجاد» request with title/source/priority/access/requester/comment saves as `LastStatus = Issue (1)` and one StatusLog row with that status.
2. Cartable: a normal user sees only their created/modified non-notified rows; Supervisor/Expert see all non-notified; Admin/ShowAll see all including notified.
3. Edit/Cancel buttons exist only for statuses 1, 2, 9 and only for creator/admin.
4. «حذف» / «بازنگری» refuse save without `ParentId`; number and type/title/access copy from parent; review increments revision; delete does not.
5. «ایجاد» dropdown does not list BPM (1727), QM (1730), QP (1731).
6. Cancel is status 22, row remains, `IsDeprecate` stays false until a later notify of a delete/review (`07`).
7. Main/temp uploads are `FileEntity`, max 1 each, max 20 MB, no Session bag.
8. Posted `ProcessOwnerId` from a tampered form is ignored on this controller.
9. No `location.reload()`, no `alert()`, tab-scoped `$$`.
