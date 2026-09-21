# ProcessDocument — لیست اسناد فرآیندی ابلاغ‌شده

> Prerequisite: `00` §6.3 (access levels), `03`, `05`. File download quirks for type فرم (1793) are here and in `07`.

## 1. Purpose

Read-only catalog of **notified** process documents for the company, with محرمانه / محدود / عمومی filtering. HTS title «لیست اسناد فرآیندی».

Legacy: `BpmDocumentController.LoadReportPage` + `GetReportGridData`, view `_BpmDocumentReport.cshtml`. Downloads go through `BpmManageDocumentController.DownloadDocumentFiles`. `SystemPage.Bpm_DocumentReport = 450`.

New: `/panel/bpm/processdocument/published`.

## 2. Row filter (copy `GetReportGridData` — three tiers)

```
statusNotified = LastStatus == Notified (18)

if (IsAdministrator)
    GetReportModel() with NO extra predicate
    // HTS admin sees every process document in this grid, including in-flight and deprecated
else if (ShowAll on THIS page)   // PermissionType.ShowAll on page 450
    statusNotified only
    // does NOT exclude IsDeprecate or RequestType 1720
else
    statusNotified
    AND IsDeprecate == false
    AND RequestType != حذف (1720)
    AND (
         AccessLevel is neither محرمانه nor محدود     // i.e. عمومی 1739 (and any future non-secret/limit)
         OR (AccessLevel == محرمانه (1737) AND (CreatedById == me OR me ∈ NotificationRecipients))
         OR (AccessLevel == محدود (1738) AND (CreatedById == me OR my OrgUnit ∈ ExecuterOrgUnits))
        )
```

**HTS fact vs plan wording:** the plan said “فقط ابلاغ‌شده” for everyone. Live HTS:

- **Admin:** unfiltered (all statuses).
- **ShowAll:** notified only, including deprecate/delete.
- **Everyone else:** notified + not deprecate + not delete + access-level predicate.

Port the three-tier behaviour. Optionally add a **second DataProfile** “قابل مشاهده برای من” that is always the non-admin predicate, and “همه ابلاغ‌شده‌ها” for Admin/ShowAll — the plan asked for that split; implement as two profiles or two FetchData flags, but **default** the published menu to the HTS three-tier rules so an admin opening the HTS-equivalent page is not surprised.

Contains-checks in HTS used `NotificationRecipientIds.Contains(userIdInText)` / `ExecuterOrganizationUnitIds.Contains(organizationUnitIdInText)` (string contains, which can false-positive on substring ids). **Havayar:** use junction equality (`Recipient.UserId == me`, `Executer.OrgUnitId == myUnit`) — this is a field-level reason to diverge (CSV contains is wrong for id `1` vs `11`).

## 3. Client download guard — do **not** port the bug

`_BpmDocumentReport.cshtml` `bpmDocumentReport_btnDownloadFile`:

```javascript
// محرمانه: if ANY recipient id !== current user → deny
notificationRecipientIdsNumbers.any(p => p !== bpmDocumentReport_userId)
// محدود: if ANY executer unit !== current unit → deny
```

That denies access whenever there is a second recipient/unit even if the current user **is** in the list. **Do not port.** Server-side download must use the same predicate as §2 (user is allowed to **see** the row ⇒ user may download the published files, unless ViewAllAttachments is required for extras).

`ViewAllAttachments` (role `Bpm.ProcessDocument.ViewAllAttachments`, HTS 143) bypasses the محرمانه/محدود client check. Keep it as a bypass on the server too.

## 4. Which files are offered (HTS `bpmDocumentReport_UpdateLayout`)

For a selected notified row:

1. If `DocumentType == فرم (1793)` **and** Final file exists → enable **Final** download.
2. If FinalPdf exists → enable **FinalPdf**.
3. Else if Final exists → enable **Final** (fallback for non-forms).

Main / Temp / Comment buttons are commented out on the report page — do **not** show them on Published. (They remain on Manage.)

Email attachments in `07` follow the same فرم-vs-pdf rule and **never** attach files for محرمانه/محدود.

## 5. Controller / view

`ProcessDocumentController.Published` + `FetchPublishedData` (or a thin `ProcessDocumentPublishedController`). `[ActionDisplayName("لیست اسناد فرآیندی", ActionAccessType.View, ActionAccessItemType.List)]`.

List: datatableprofile. Columns from HTS report grid: Id, title, number, revision, type, access, process set, owner, executer units, notify date, request type. Status-log child optional.

No Edit. Download actions only.

Row highlight: HTS sets first-cell `steelblue` when `IsRevisedAndHasSupervisorApprove` (AutoMapper field). If that computed flag exists after porting the report model, keep the cue; otherwise skip rather than inventing a new colour language.

## 6. DataProfiles (plan)

| Profile | Filter |
|---|---|
| `Bpm_Published_Mine` | non-admin predicate in §2 |
| `Bpm_Published_AllNotified` | `LastStatus == Notified` (Admin / ShowAll / PublishedList role) |

`Bpm.ProcessDocument.PublishedList` (group 355) is granted on notify to recipients (and for محدود, users of executer units) so they can open this page at all. Opening the page without that role should 403 even for عمومی docs — HTS hid the menu via `SystemPage.Bpm_DocumentReport`. Users who only have Request access must **not** see this list unless also granted 450/355.

## 7. Acceptance criteria

1. A user without ShowAll/Admin cannot see `IsDeprecate` rows or `RequestType = حذف` in the published grid.
2. A محرمانه doc is visible to its creator and its notification-recipient users only (junction, not string-contains).
3. A محدود doc is visible to its creator and members of executer org units.
4. عمومی notified (non-deprecate, non-delete) docs are visible to every user who can open the page.
5. Admin’s HTS-equivalent grid is unfiltered; document this in the Role UI so it is not treated as a bug.
6. Form type 1793 prefers Final download; other types prefer FinalPdf then Final.
7. The inverted `any() !== userId` download check is **absent**.
8. Main/Temp/Comment are not downloadable from this page.
9. No Kendo, no EDMS types, no service class.
