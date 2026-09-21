# Gnr.DocumentFolder — Organizational document folder (سطح مدرک)

> Prerequisite: `00-Overview-and-Conventions.md` (schema `Gnr`, no EDMS names, junction tables not CSV).

## 1. Purpose

Master table of **organizational archive folders** (HTS «پوشه سطح مدرک»). Each folder belongs to one org unit, has a Persian title + Latin/folder-path title, and may be marked private with an explicit user list.

Legacy: `Gnr_Document1stLevel`, `Document1StLevelController`, `_Document1stLevel.cshtml`. Menu: سیستم مدیریت اسناد → اسناد سازمانی → پوشه سطح مدرک. HTS `SystemPage.Gnr_Document1stLevel = 316`.

Live TotalSystem (2026-09-14): **65** level-1 rows. **3** `Gnr_Document2ndLevel` rows and **1** `Gnr_Document3rdLevel` row still exist. The HTS menu for 2nd/3rd is commented out. **Keep the hierarchy in the model; do not add Panel menu items** for levels 2/3.

HTS `GetData` on this page has **no row filter** — anyone who can open the page sees every folder. Privacy is enforced on the **document list**, not here.

## 2. Entity: `Entities/App/Gnr/DocumentFolder.cs`

```csharp
[Display(Name = "پوشه اسناد سازمانی")]
[Table("DocumentFolder", Schema = "Gnr")]
public class DocumentFolder : BaseEntity
{
    [DisplayName("شناسه HTS")]
    [DisplayInfo(null, false, type: SystemType.Long)]
    public long HtsId { get; set; }

    /// <summary>1 = Gnr_Document1stLevel, 2 = 2nd, 3 = 3rd. Used by sync and by OrganizationalDocument FKs.</summary>
    [DisplayName("سطح")]
    [DisplayInfo(null, true, type: SystemType.Int, required: true)]
    public int FolderLevel { get; set; } = 1;

    [DisplayName("پوشه والد")]
    [DisplayInfo(null, true, type: SystemType.EntitySelector)]
    public long? ParentId { get; set; }
    public virtual DocumentFolder? Parent { get; set; }

    [DisplayName("واحد سازمانی")]
    [DisplayInfo(null, true, type: SystemType.EntitySelector, required: true)]
    public long? OrganizationUnitId { get; set; }
    public virtual OrgUnit? OrganizationUnit { get; set; }

    [DisplayName("عنوان")]
    [DisplayInfo(null, true, type: SystemType.String, required: true)]
    [MaxLength(2048)]
    public string? Title { get; set; }

    [DisplayName("عنوان پوشه (مسیر فایل)")]
    [DisplayInfo(null, true, type: SystemType.String, required: true)]
    [MaxLength(256)]
    public string? FolderTitle { get; set; }

    [DisplayName("خصوصی")]
    [DisplayInfo(null, true, type: SystemType.Boolean)]
    public bool IsPrivate { get; set; }

    [DisplayName("توضیحات")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(4000)]
    public string? Comment { get; set; }

    public virtual ICollection<DocumentFolderUser> AllowedUsers { get; set; } = new List<DocumentFolderUser>();
    public virtual ICollection<DocumentFolder> Children { get; set; } = new List<DocumentFolder>();
}

[Display(Name = "کاربر مجاز پوشه")]
[Table("DocumentFolderUser", Schema = "Gnr")]
public class DocumentFolderUser : BaseEntity
{
    public long DocumentFolderId { get; set; }
    public virtual DocumentFolder? DocumentFolder { get; set; }

    public long UserId { get; set; }
    public virtual User? User { get; set; }
}
```

`OrganizationUnitId` is **required on root folders** (`FolderLevel = 1` / `ParentId == null`). Child folders inherit the root org unit for path/ACL purposes; they do not re-store `IsPrivate` / allowed users (HTS 2nd/3rd tables have no those columns).

HTS `CreatedTime` (nchar 40, Persian time) is an audit quirk — **do not** add a column; `BaseEntity` timestamps cover it.

### Field map vs HTS

| HTS `Gnr_Document1stLevel` | Havayar | Notes |
|---|---|---|
| `Id` (smallint) | `HtsId` | New `Id` is `long` identity |
| `OrganizationUnitId` | `OrganizationUnitId` → `Hrm.OrgUnit` | EntitySelector; filter `IsActive && !Title.Contains("*")` (HTS `FillData`) |
| `Title` nvarchar(2048) | `Title` | Required |
| `FolderTitle` nvarchar(256) | `FolderTitle` | Used in catalog path `{OrgUnit.Title}/{FolderTitle}/...` |
| `UserIds` nvarchar(50) CSV | `DocumentFolderUser` rows | HTS length 50 is tight; junction has no such cap |
| `UserIdsInText` | *(omit)* | Derive from joined `User` names in the grid if needed |
| `IsPrivate` | `IsPrivate` | ACL for **documents**, not this list |
| `Comment` | `Comment` | |
| `CreatedUserId/Date/InText/Time` | `BaseEntity` audit | |

| HTS `Gnr_Document2ndLevel` | Havayar |
|---|---|
| `Id` | `HtsId` + `FolderLevel = 2` |
| `Document1stLevelId` | `ParentId` → the level-1 folder |
| `Title` / `FolderTitle` / `Comment` | same columns |

| HTS `Gnr_Document3rdLevel` | Havayar |
|---|---|
| `Id` | `HtsId` + `FolderLevel = 3` |
| `Document2ndLevelId` | `ParentId` → the level-2 folder |

Live 2nd-level titles: «مهندسی هوای فشرده» / Compressed Air Engineering (parent folder 13); «اطلس کوپکو» / AtlasCopco (parent 8); «ایمنی» / Safety (parent 8). Live 3rd-level: «درایر» / Dryer under 2nd-level Id 5. **Zero** `Gnr_Document` rows currently point at level 2 (`Document2ndLevelId` all null). Keep the FKs anyway.

## 3. Controller: `WebApp/Controllers/Dynamic/Gnr/DocumentFolderController.cs`

`[Route("Panel/Gnr/[controller]")]`, `[ControllerInfo("پوشه اسناد سازمانی", typeof(DocumentFolder))]`.

Standard CRUD. No custom workflow.

- `List` / `FetchData`: return **all root folders** (`FolderLevel == 1` or `ParentId == null`). Do **not** filter by `IsPrivate` or current user (HTS `GetData` is unfiltered).
- `Save`: replace `AllowedUsers` from the posted user-id list (junction, not CSV).
- Optional `SaveChild` only if the Folder Edit page lets a supervisor add a level-2/3 row in-place. No public List menu for children.
- Org-unit EntitySelector filter: `OrgUnit.IsActive && !OrgUnit.Title.Contains("*")`, ordered by title — same as HTS `_organizationUnitService.Where(p => p.IsActive == true && !p.OrgUnit_Title.Contains("*"))`.
- User EntitySelector for allowed users: active users only (HTS `p.IsActive`).

Delete: block if any `OrganizationalDocument` still references the folder (or any child). HTS had no extra rule beyond `DoOperation`.

## 4. Views

### `WebApp/Views/Panel/Gnr/DocumentFolder/List.cshtml`

```cshtml
@using Entities.App.Gnr
<datatableprofile entity-Type="typeof(DocumentFolder)"></datatableprofile>
```

Grid should show Title, FolderTitle, OrganizationUnit, IsPrivate, Comment. Default profile can hide `HtsId` / `FolderLevel`.

### `WebApp/Views/Panel/Gnr/DocumentFolder/Edit.cshtml`

Single card, `form-group-inline` fields:

- `OrganizationUnitId` — `Html.EntitySelector<OrgUnit>` with the active / no-`*` filter
- `Title` — required
- `FolderTitle` — required (LTR is acceptable; HTS used it as a path segment)
- `IsPrivate` — checkbox
- Allowed users — multi EntitySelector bound to junction (visible/required when `IsPrivate` is true; still stored if the user listed people on a non-private folder — HTS allowed that)
- `Comment` — textarea

Standard `savefn` (`validateError` → `page.$pageEl.dataBind()` → `$$.post('save', …)`). No workflow buttons.

Child folders (level 2/3): a small nested table on this Edit page is enough (add/edit/delete child rows posting `ParentId = current folder`). **No** `/panel/gnr/documentfolderlevel2/list` menu.

## 5. Access rules (this page only)

| Actor | Folder grid | Folder edit |
|---|---|---|
| User with List permission | All root folders | Standard CRUD if they have Insert/Update/Delete |
| Others | Page hidden by RoleAccess | |

Do **not** hide private folders here. Document-list ACL is specified in `02-OrganizationalDocument.md`.

## 6. Acceptance criteria

1. Creating a folder with org unit + title + folder title persists a `Gnr.DocumentFolder` row (`FolderLevel = 1`) and appears in the unfiltered list for any user who can open the page.
2. Private folder + three allowed users writes **three** `DocumentFolderUser` rows and **zero** CSV columns.
3. Org-unit picker omits inactive units and titles containing `*`.
4. Level-2/3 can be saved as children (`ParentId` set, `FolderLevel` 2/3) from the parent Edit page; they do **not** appear as their own menu item.
5. `HtsId` is stored (0 until sync). After sync (`08`), the 65 + 3 + 1 HTS rows are present and parent links match TotalSystem.
6. No application code references `Edms.Document`. No SQL trigger. No `Data/Services/Gnr/DocumentFolderService.cs`.
