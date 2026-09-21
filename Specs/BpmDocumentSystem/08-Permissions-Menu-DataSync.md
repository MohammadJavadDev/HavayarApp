# Permissions, menu, TotalSystem data/file sync

> Prerequisite: `00` §5 / §10 (menu + roles), `01`–`07`. Do not run sync until numbering and user/org-unit maps work.

## 1. Menu (Panel Menu Builder)

Top group **سیستم مدیریت اسناد**, two subgroups. Paths **lowercase**:

| Text | Path | Controller action | HTS SystemPage |
|---|---|---|---|
| پوشه سطح مدرک | `/panel/gnr/documentfolder/list` | `DocumentFolderController.List` | 316 |
| اسناد | `/panel/gnr/organizationaldocument/list` | `OrganizationalDocumentController.List` | 319 |
| درخواست ایجاد/تغییر | `/panel/bpm/processdocument/list` | `ProcessDocumentController.List` | 446 |
| مدیریت درخواست‌ها | `/panel/bpm/processdocument/manage` | `ProcessDocumentController.Manage` | 447 |
| لیست اسناد فرآیندی | `/panel/bpm/processdocument/published` | `ProcessDocumentController.Published` | 450 |

No items for folder level 2/3 (HTS commented out).

After adding controllers: **restart WebApp** so `InitializeAccessControllers` / `EndpointService.GetAllEndpoints` picks up `[ControllerInfo]` + `[ActionDisplayName]`. Then tick Role checkboxes on `/panel/Role/List`.

## 2. Roles to seed / assign

Same names as `00` §10:

| Role | Typical HTS holders |
|---|---|
| `Gnr.DocumentFolder.*` standard CRUD | page 316 |
| `Gnr.OrganizationalDocument.*` standard CRUD + download | page 319 + ViewAttachment |
| `Bpm.ProcessDocument` request CRUD + `CancelRequest` | page 446 |
| `Bpm.ProcessDocument.ShowAll` | ShowAll on 446 and/or 447 and/or 450 — grant **per action** if the Role UI is action-scoped |
| `Bpm.ProcessDocument.Supervisor` | permission 128 + group **353** «سرپرست سیستم ها و روش ها در مدیریت اسناد (مدیریت یکپارچه اسناد)» |
| `Bpm.ProcessDocument.Expert` | permission 129 + group **354** «کارشناس سیستم ها و روش ها در مدیریت اسناد (مدیریت یکپارچه اسناد)» |
| `Bpm.ProcessDocument.ViewManage` | group **418** «کارشناس مشاهده مدیریت اسناد (مدیریت یکپارچه اسناد)» — auto-grant when a supervisor saves owner/approver/final-approver (`05`) |
| `Bpm.ProcessDocument.PublishedList` | group **355** «کارشناسان گزارش اسناد (مدیریت یکپارچه اسناد)» — auto-grant on successful ابلاغ (`07`) |
| `Bpm.ProcessDocument.ViewAllAttachments` | permission **143** on page 450 |

Do **not** seed `Bpm_PrivateDocumentPermission = 130`.

Admin (`IsAdministrator`) bypasses cartable filters as in HTS `UserIsAdmin`.

## 3. Sync script

Add `Data/Scripts/SyncBpmDocumentSystemFromTotalSystem.sql` following `SyncCourseBankFromTotalSystem.sql`:

- Runs on HavayarApp with linked server `[TMS]` → `TotalSystem`.
- `SET IDENTITY_INSERT` only if we keep HTS ids (Training CourseBank did). **This module uses new `Id` + `HtsId`** (plan). Do **not** identity-insert unless a later decision says otherwise.
- Idempotent upsert on `HtsId`.

### 3.1 Maps

**Users** (same as Training scripts):

```sql
Gnr_User.Username = system.[User].Username
-- fallback: ActiveDirectoryUsername, then Party/personnel email if needed
```

Store the map in `#UserMap(OldUserId, NewUserId)`.

**Org units** (same as `SyncCourseFromTotalSystem.sql`):

```sql
HRM_OrgUnit.Hamkaran_Unit_FK = Hrm.OrgUnit.HamkaranUnitId
```

`#OrgMap(OldOrgUnitId, NewOrgUnitId)`. Skip / warn rows that do not map; do not invent units.

### 3.2 Folder + org documents

1. `Gnr_Document1stLevel` → `Gnr.DocumentFolder` (`FolderLevel=1`, `HtsId=Id`, org via `#OrgMap`). Split `UserIds` CSV → `DocumentFolderUser` via `#UserMap`.
2. `Gnr_Document2ndLevel` → `DocumentFolder` `FolderLevel=2`, `ParentId` = the Havayar id of `HtsId = Document1stLevelId AND FolderLevel=1`.
3. `Gnr_Document3rdLevel` → `FolderLevel=3`, parent = level-2 folder `HtsId`.
4. `Gnr_Document` → `Gnr.OrganizationalDocument`. `FolderId` from level-1 `HtsId`. `SecondLevelFolderId` / `ThirdLevelFolderId` when HTS FKs set (live: **0** level-2 FKs). `FileEntity` with `PhysicalPath = FinalFilePath`, `OriginalName = DocumentFileName`, `Size = DocumentFileSize`. **Ignore `Gnr_Document.IsPrivate`.**

### 3.3 Process documents

1. Insert `Bpm.ProcessDocument` from `Bpm_Document` with enums as the raw Lookup/status ids (they match). Map every user FK and `CreatedOrganizationUnitId`. `HtsId = Id`.
2. Split CSV → junctions (`03`). Unmapped tokens: log and skip that id, do not fail the whole batch unless `--strict`.
3. `Bpm_DocumentDetail` → `ProcessDocumentStatusLog` (`HtsId=Id`, parent via document `HtsId`). Preserve `CreatedOn*` from HTS so log order (`max Id` / chronology) stays the same. After insert, `LastStatus` / `LastStatusUserId` must equal the log with **max HtsId** per document (same as trigger). Prefer setting them from the HTS columns first, then verify.
4. Files: five `FileEntity` rows when the corresponding `*FilePath` is non-empty. Keep UNC.
5. `ProcessDocumentModuleSetting.LastAnnouncementNumber` ← `Gnr_MainConfiguration` where `Configuration_Key = 'Bpm_LastAnnouncementNumber'` (live **446**).

Do **not** sync `Edms_Document*`. Do **not** sync `Qa_CorrectiveActionActions`.

### 3.4 Role membership from HTS groups

Optional second script or section:

- Members of `Gnr_UserGroup` 353 → `Bpm.ProcessDocument.Supervisor`
- 354 → Expert
- 355 → PublishedList
- 418 → ViewManage

Plus anyone with `PermissionType` 128/129/143 on the three BPM pages.

## 4. Files on disk

Sync does **not** copy binaries by default. If the WebApp process cannot read `\\172.20.40.27\Uploads\Bpm\DMS\` or `\\172.20.30.14\COTMS\Hts_Documents\Catalogs\`, a later ops task can robocopy into the Havayar upload root and UPDATE `FileEntity.PhysicalPath`. Spec that as ops, not as a C# job in this wave.

## 5. Acceptance criteria

1. Menu shows five items, no level-2/3 entries, paths lowercase, hidden when RoleAccess is missing.
2. Restart required once; new actions appear in Role edit.
3. Upsert by `HtsId` can re-run without duplicating the 65/428/1178/3643 rows.
4. A sample HTS process doc keeps the same `DocumentNumber`, `LastStatus` id, lookup ids, and status-log order after sync.
5. CSV user `12,44` becomes two junction rows mapped through `#UserMap`; unmapped ids are reported.
6. Org catalog files open via `FileEntity.PhysicalPath` pointing at the existing UNC.
7. `LastAnnouncementNumber` after sync is 446 (or whatever TotalSystem has at run time).
8. Engineering `Edms.Document` row counts unchanged.
9. No SQL triggers created on Havayar tables; EntityAction covers last-status/revision/number.

## 7. Operator how-to (فاز ۸)

ترتیب اجرا روی دیتابیس موجود:

1. `.\add-migration.ps1` / `.\update-database.ps1` تا نقش‌های `400000`–`400005` و ایندکس‌های `HtsId` اعمال شوند.
2. `Data/Scripts/Seed_BpmDocumentSystem_DataProfiles.sql` — نقش (اگر نبود)، پنج SavedQuery/DataProfile با ACL، RoleAccess، منوی `BpmDocuments`.
3. **Restart WebApp** تا `EndpointService` اکشن‌های جدید را در Role UI ببیند.
4. `Data/Scripts/SyncBpmDocumentSystemFromTotalSystem.sql` — upsert با `HtsId`، بدون کپی باینری. `@Strict = 1` کل تراکنش را در صورت ردیف بدون نگاشت rollback می‌کند.
5. در `/panel/Role/List` نقش‌های BPM را به کاربران بدهید (یا بگذارید بخش گروه HTS در sync، اعضای ۳۵۳/۳۵۴/۳۵۵/۴۱۸ را پر کند).
6. منو: Menu Builder → منوی «سیستم مدیریت اسناد» (`Name=BpmDocuments`). `AccessRoleIds` اولیه شش نقش BPM + `ShowAllMenus` است؛ برای بایگان سازمانی که نقش BPM ندارد، همان نقش را به منو **و** به اکشن‌های `Gnr.DocumentFolder` / `OrganizationalDocument` اضافه کنید.
7. درخواست‌دهنده عادی (بدون Supervisor/Expert/ShowAll): در Role، تیک `List`/`Edit`/`New`/`Save` مسیر `/panel/bpm/processdocument/...` **و** نمایه `Bpm_ProcessDocument_Request` لازم است؛ در غیر این صورت گرید «بدون نمایه داده» می‌ماند. گرید زنده به `/System/FetchDataProfile` می‌زند نه `FetchData` کنترلر.

مسیرهای منو (lowercase، تطابق دقیق با `RoleAccess.Path`):

| آیتم | Path |
|---|---|
| پوشه سطح مدرک | `/panel/gnr/documentfolder/list` |
| اسناد | `/panel/gnr/organizationaldocument/list` |
| درخواست ایجاد/تغییر | `/panel/bpm/processdocument/list` |
| مدیریت درخواست‌ها | `/panel/bpm/processdocument/manage` |
| لیست اسناد فرآیندی | `/panel/bpm/processdocument/published` |

نمایه داده (unicode گرید): `Entities.App.Gnr.DocumentFolder`، `...OrganizationalDocument`، `...ProcessDocument.Request` / `.Manage` / `.Published`.

## 8. Phase 9 remaining HTS gaps

رفع‌شده در این موج (blocking): ACL گرید در SavedQuery (folder بدون فیلتر ردیف؛ org با `Folder.IsPrivate`؛ request/manage/published مطابق `04`/`05`/`06`)؛ اعطای `PublishedList` هنگام ابلاغ؛ unicode جدا برای سه کارتابل؛ `t1_Id` برای دکمه دانلود لیست ابلاغ.

باقی‌مانده (غیر blocking یا خارج از دامنه):

- **QA `CorrectiveAction`:** خارج از دامنه (`00`).
- **نقشه PermissionType 7/128/129/143 روی صفحه HTS:** sync فقط گروه‌های ۳۵۳/۳۵۴/۳۵۵/۴۱۸ را به نقش می‌برد؛ ShowAll صفحه‌ای HTS باید دستی در Role UI تیک شود.
- **فونت/قالب ایمیل HTS:** هنوز hardcoded در dispatcher/`07` (CC در `ProcessDocumentModuleSetting`).
- **Excel کنترلر** (`ExportToExcel` روی خود entity) فیلتر ACL ندارد؛ دکمه گرید از `/System/ExportToExcelProfile` و همان SavedQuery استفاده می‌کند.
- **`fetch-data-path`:** در tag helper/`site.js` هست ولی به کارتابل BPM وصل نشده (ناسازگاری ستون `t1_*` با JSON کنترلر).
- **Restart Role UI** بعد از کنترلر جدید الزامی است.
- **منوی سطح ۲/۳ پوشه:** آگاهانه نیست (HTS کامنت شده).
- **تریگر SQL:** پورت نشده؛ EntityAction جایگزین است.
- **Edms مهندسی:** دست‌نخورده؛ شمارش `Edms.Document` در انتهای sync برای کنترل است.
- **ممیزی طرفینی زنده** (یک نمونه ایجاد/بازنگری/حذف با هر نقش در HTS و Havayar، شماره سند، ایمیل): نیاز به لاگین و داده syncشده دارد؛ در این پاس فقط ممیزی استاتیک انجام شد. چک‌لیست وقتی داده آمد: فیلد فرم/گرید، هر گذار + متن StatusLog، شماره سند در برابر HTS، فیلتر محرمانه/محدود، دانلود Main/Final/Pdf، مخاطب ایمیل، تیک منو.
- **`Bpm_PrivateDocumentPermission = 130`:** عمداً نقش ندارد.
