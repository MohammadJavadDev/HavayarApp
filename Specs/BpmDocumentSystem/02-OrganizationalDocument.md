# Gnr.OrganizationalDocument — Organizational archive files

> Prerequisite: `00-Overview-and-Conventions.md`, `01-DocumentFolder.md`.

## 1. Purpose

Simple **file catalog** under an organizational folder. No approval workflow. One uploaded file = one document row. Used as «اسناد» under سیستم مدیریت اسناد → اسناد سازمانی.

Legacy: `Gnr_Document`, `DocumentController`, `_Document.cshtml`. `SystemPage.Gnr_Document = 319`. Live **428** rows, **all** with `IsPrivate = 0`.

## 2. Decision: in-place update (not HTS replace-as-new)

HTS `DoOperation` on **Update** copies the existing file into the Session upload list, **switches the operation to Add**, inserts a **new** row, then **deletes** the original (`isUpdateMode` → `_gnrDocumentService.Delete(entity)`). That is replace-as-new.

**Havayar decision (this spec):** **in-place update of the same row**. If the user uploads a new file, replace the `FileEntity` on that row (old file remains in storage until housekeeping; do not delete the DB row). Reasons:

- Havayar `FileEntity` + audit fields on `BaseEntity` already record who changed the row.
- Replace-as-new would mint a new `Id`, break `HtsId` stability after sync, and confuse Panel tabs/`appController`.
- HTS `Gnr_Document.IsPrivate` is unused in filters and is **false on every live row** — no field-level behaviour depends on “new id on edit”.
- Multi-file **Add** (up to 20 files → 20 rows) stays as HTS: each file is still a **new** row.

There is no field whose business meaning requires replace-as-new. Do not port the Add-then-Delete trick.

## 3. Entity: `Entities/App/Gnr/OrganizationalDocument.cs`

```csharp
[Display(Name = "اسناد سازمانی")]
[Table("OrganizationalDocument", Schema = "Gnr")]
public class OrganizationalDocument : BaseEntity
{
    [DisplayName("شناسه HTS")]
    [DisplayInfo(null, false, type: SystemType.Long)]
    public long HtsId { get; set; }

    [DisplayName("پوشه")]
    [DisplayInfo(null, true, type: SystemType.EntitySelector, required: true)]
    public long FolderId { get; set; }
    public virtual DocumentFolder? Folder { get; set; }

    [DisplayName("پوشه سطح ۲")]
    [DisplayInfo(null, true, type: SystemType.EntitySelector)]
    public long? SecondLevelFolderId { get; set; }
    public virtual DocumentFolder? SecondLevelFolder { get; set; }

    [DisplayName("پوشه سطح ۳")]
    [DisplayInfo(null, true, type: SystemType.EntitySelector)]
    public long? ThirdLevelFolderId { get; set; }
    public virtual DocumentFolder? ThirdLevelFolder { get; set; }

    [DisplayName("عنوان")]
    [DisplayInfo(null, true, type: SystemType.String, required: true)]
    [MaxLength(2048)]
    public string? Title { get; set; }

    [DisplayName("عنوان لاتین")]
    [DisplayInfo(null, true, type: SystemType.String, required: true)]
    [MaxLength(512)]
    public string? TitleInLatin { get; set; }

    [DisplayName("برند")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(100)]
    public string? Brand { get; set; }

    [DisplayName("بازنگری")]
    [DisplayInfo(null, true, type: SystemType.Int, required: true)]
    public short Revision { get; set; }

    [DisplayName("سال")]
    [DisplayInfo(null, true, type: SystemType.Int)]
    public short? Year { get; set; }

    [DisplayName("فایل")]
    [DisplayInfo(null, true, type: SystemType.File, required: true)]
    public long? FileId { get; set; }
    public virtual FileEntity? File { get; set; }

    [DisplayName("توضیحات")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(4000)]
    public string? Comment { get; set; }
}
```

### Do **not** add `IsPrivate` on this entity

HTS column `Gnr_Document.IsPrivate` exists (`bit NOT NULL default 0`) but:

- `DocumentController.GetData` filters only on **`Gnr_Document1stLevel.IsPrivate`** (plus folder org unit / `UserIds.Contains(userId)`).
- Live data: **428 / 428 rows have `IsPrivate = 0`**.

Repeating the flag would invite a second, conflicting ACL. Folder.IsPrivate is the only switch.

### Field map vs HTS

| HTS `Gnr_Document` | Havayar | Notes |
|---|---|---|
| `Id` | `HtsId` | |
| `Document1stLevelId` | `FolderId` | Required |
| `Document2ndLevelId` | `SecondLevelFolderId` | Optional; 0 live usages |
| `Document3rdLevelId` | `ThirdLevelFolderId` | Optional |
| `Title` | `Title` | Required |
| `TitleInEurope` | `TitleInLatin` | Required; HTS `$('#gnrDocument_txtTitleInEurope').onlyTypeEnglish()` — enforce ASCII/English on the client (`dir="ltr"`) and reject non-English on save |
| `Brand` | `Brand` | Optional (HTS required check is commented out) |
| `Revision` | `Revision` | Required; HTS numeric, used in path `Rev{n}` when `> 0` |
| `Year` | `Year` | Optional; `0` is invalid if provided |
| `DocumentFileName` / `DocumentFileSize` | `File.OriginalName` / `File.Size` | |
| `RawFilePath` | *(omit as source of truth)* | HTS relative `{OrgUnit}/{FolderTitle}/{Brand?}/{Year?}/Rev{Revision}/{filename}` |
| `FinalFilePath` | `File.PhysicalPath` | Absolute UNC on sync |
| `IsPrivate` | **omit** | Unused in list filter |
| `Comment` | `Comment` | |
| audit columns | `BaseEntity` | |

## 4. Files

- One `FileEntity` per document row. HTS Add with N files (max **20**, max **51200 KB** ≈ 50 MB each) creates N rows sharing the same metadata — **keep that on Add only**.
- Logical folder (for `PhysicalPath` when Havayar stores a new file):

```
{DocumentsRoot}/{OrgUnit.Title}/{Folder.FolderTitle}/{Brand?}/{Year?}/Rev{Revision}/{safeFileName}
```

HTS `DocumentsStoragePath` root is `\\172.20.30.14\COTMS\Hts_Documents\Catalogs\`. Level 2/3 folder titles are **not** appended in live HTS (`path = Path.Combine` for 2nd/3rd is commented out). Do not add them.

- Duplicate file name in the same directory: HTS skips that file and reports a partial-success toastr. Port: skip duplicates, return a warning listing skipped names; persist the rest.
- Download: HTS `DownloadFiles` requires `PermissionType.ViewAttachment` on page 319. Havayar: a dedicated `Download` action with `[ActionDisplayName(..., ActionAccessType.View, …)]`. Apply the **same list ACL** before streaming (do not let a guessed id bypass the folder privacy filter).
- No Session upload bag (HTS anti-pattern). Standard Panel `SystemType.File` control.

## 5. Controller: `WebApp/Controllers/Dynamic/Gnr/OrganizationalDocumentController.cs`

`[Route("Panel/Gnr/[controller]")]`, `[ControllerInfo("اسناد سازمانی", typeof(OrganizationalDocument))]`.

Standard CRUD.

### `FetchData` row filter (copy HTS `GetData`)

```
if (IsAdministrator)
    all rows
else
    Folder.IsPrivate == false
    OR Folder.OrganizationUnitId == currentUser.OrgUnitId
    OR Folder.AllowedUsers contains currentUser.Id
```

Do **not** consult a document-level private flag.

### `Save`

- **Add:** if the client posts multiple files (framework file control / extra `fileIds[]`), insert one row per file with the same metadata. Cap 20. Each file ≤ 51200 KB.
- **Update:** update the existing row in place. New file → new `FileEntity`, point `FileId` at it. Do **not** insert-then-delete.
- Validate: folder, title, Latin title (English-only), revision required; year if present ≠ 0; at least one file on Add.

Folder EntitySelector display format in HTS FillData: `{OrgUnit} | {Title} | {FolderTitle}`. Match that in `EntitySelector` display/projection.

Level 2 selector filtered by selected level-1; level 3 by selected level-2. Both optional and not shown in the menu.

## 6. Views

### List

```cshtml
@using Entities.App.Gnr
<datatableprofile entity-Type="typeof(OrganizationalDocument)"></datatableprofile>
```

Suggested columns: Title, TitleInLatin, Folder, Brand, Year, Revision, file name, size, Comment. Download via a row action or Edit.

### Edit

Fields (all `form-group-inline`):

- Folder (required EntitySelector)
- Second / third level (optional, cascaded; can stay hidden if no children exist)
- Title (required)
- TitleInLatin (required, `dir="ltr"`, English-only helper equivalent to HTS `onlyTypeEnglish`)
- Brand (optional)
- Year (numeric `data-inputmask`)
- Revision (required numeric)
- File (required on new; optional on edit if a file already exists)
- Comment

Standard save wiring. After save: `page.$pageEl.dataBind(r.data)` — do not `location.reload()`.

## 7. Acceptance criteria

1. Non-admin users do **not** see documents whose folder is private unless they belong to the folder’s org unit or `DocumentFolderUser`. Admins see all  (and, after sync, all 428 HTS rows).
2. A document row has **no** `IsPrivate` column. Changing folder privacy immediately changes who sees its documents.
3. Add with 3 files creates 3 rows, 3 `FileEntity`s, same title/folder/revision.
4. Edit changes title on the **same** `Id`; uploading a new file keeps `Id`/`HtsId` and updates `FileId`.
5. Latin title is required and rejects Persian letters.
6. More than 20 files or a file &gt; 50 MB is rejected with `toastr.error` (no raw `alert`).
7. Duplicate destination file name is skipped with a partial-success message; other files still save.
8. Catalog path does not include level-2/3 folder titles (HTS live behaviour).
9. No replace-as-new, no CSV, no EDMS types, no service class.
