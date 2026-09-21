# Trn.TeacherBank — Instructor Master (+ Attachments)

> Prerequisite: read `00-Overview-and-Conventions.md` first.

## 1. Purpose

Master table of course **instructors** (مدرسین). Each `Course` references one or two teachers (`TeacherId`, optional `SecondTeacherId`). Teachers have their own CRUD page plus a child attachment grid (CVs, certificates, etc.).

Legacy source: `Training_TeacherBank` + `Training_TeacherBank_Attachment` entities, `TeacherBankController`, views `_TeacherBank.cshtml` + `_TeacherBankAttachments.cshtml`. Menu: Training System → Base Information → Banks → Teacher Bank.

> **Known legacy gap to close, not copy:** the legacy controller writes uploaded files as raw `byte[]` into `Attachment_FileContent` and never implements delete. Port attachments properly with `FileEntity` (framework pattern) and support delete.

## 2. Entities

### `Entities/App/Trn/TeacherBank.cs`

```csharp
[Display(Name = "بانک مدرسین")]
[Table("TeacherBank", Schema = "Trn")]
public class TeacherBank : BaseEntity
{
    [DisplayName("نام")]
    [DisplayInfo(null, true, type: SystemType.String, required: true)]
    [MaxLength(100)]
    public string? Firstname { get; set; }

    [DisplayName("نام خانوادگی")]
    [DisplayInfo(null, true, type: SystemType.String, required: true)]
    [MaxLength(100)]
    public string? Lastname { get; set; }

    // Full name is a computed display value → derive in UI/query, NOT a stored column.

    [DisplayName("تحصیلات")]
    [DisplayInfo(null, true, type: SystemType.Select)]
    public EducationEnum? Education { get; set; }

    [DisplayName("رشته تحصیلی")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(256)]
    public string? FieldOfStudy { get; set; }

    [DisplayName("تلفن همراه")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(20)]
    public string? Mobile { get; set; }

    [DisplayName("ایمیل")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(256)]
    public string? Email { get; set; }

    [DisplayName("زمینه تدریس")]
    [DisplayInfo(null, true, type: SystemType.TextArea)]
    public string? TeachingField { get; set; }

    [DisplayName("توضیحات")]
    [DisplayInfo(null, false, type: SystemType.TextArea)]
    public string? Description { get; set; }

    public virtual ICollection<TeacherBankAttachment> Attachments { get; set; } = new List<TeacherBankAttachment>();
}
```

### `Entities/App/Trn/TeacherBankAttachment.cs`

Child entity — one row per uploaded file. Follow the `TeacherBankAttachment` child-entity pattern:

```csharp
[Display(Name = "پیوست مدرس")]
[Table("TeacherBankAttachment", Schema = "Trn")]
public class TeacherBankAttachment : BaseEntity
{
    public long TeacherBankId { get; set; }
    public virtual TeacherBank? TeacherBank { get; set; }

    [DisplayName("عنوان")]
    [DisplayInfo(null, true, type: SystemType.String, required: true)]
    [MaxLength(256)]
    public string? Title { get; set; }

    [DisplayName("فایل")]
    [DisplayInfo(null, true, type: SystemType.File, required: true)]
    public long? FileId { get; set; }
    public virtual FileEntity? File { get; set; }
}
```

### Enum

`EducationEnum` (shared with `Participant.Education` — see `05-Participant.md`): confirmed members from legacy `Gnr_Lookup` where `LookupType_FK = 100` ("تحصیلات", queried 2026-09-09): `506` = دیپلم, `507` = فوق دیپلم, `508` = لیسانس, `509` = فوق لیسانس, `510` = دکترا, `2457` = فوق دکتری, `2458` = دانشجوی (فوق دیپلم), `2459` = دانشجوی (لیسانس), `2460` = دانشجوی (فوق لیسانس), `2461` = دانشجوی (دکتری). No further members exist.

## 3. Controllers

### `WebApp/Controllers/Dynamic/Trn/TeacherBankController.cs`

Standard CRUD (`Save`/`Add`/`Update`/`Delete`/`Edit`/`New`/`List`/`FetchData`/`ExportToExcel`), route `Panel/Trn/[controller]`, `[ControllerInfo("بانک مدرسین", typeof(TeacherBank))]`. No custom business rules.

### `WebApp/Controllers/Dynamic/Trn/TeacherBankAttachmentController.cs`

Child-entity controller following `TeacherBankAttachmentController` conventions:

- Own `Save`/`Delete`/`Edit`/`List`/`FetchData`, plus `ListByTeacherBankId(long teacherBankId)` returning the child list view filtered by parent.
- No `ExportToExcel` needed for the child grid.

## 4. Views

- `WebApp/Views/Panel/Trn/TeacherBank/List.cshtml` — `<datatableprofile entity-Type="typeof(TeacherBank)"></datatableprofile>`.
- `WebApp/Views/Panel/Trn/TeacherBank/Edit.cshtml` — normal field form (`Firstname`, `Lastname`, `Education`, `FieldOfStudy`, `Mobile`, `Email`, `TeachingField`, `Description`) + a `form-action-buttons` button **"پیوست‌ها"** wired exactly like `openSubPage` in `Course/Edit.cshtml`: `appController.addPage('/Panel/Trn/TeacherBankAttachment/ListByTeacherBankId?teacherBankId=' + id)`. Only show the attachments button when editing an existing row (`id > 0`); hide it on `New()`.
- `TeacherBankAttachment/ListByTeacherBankId.cshtml` + `Edit.cshtml` — child list/edit views per the child-entity convention in `00-Overview-and-Conventions.md` §8.

## 5. Business rules

1. Delete of a `TeacherBank` row is blocked if any `Course` references it as `TeacherId` or `SecondTeacherId` — enforce via FK constraint and surface the standard framework FK-violation message (do not add custom cascade logic).
2. File upload uses the framework `FileEntity` mechanism — do **not** port the legacy raw-`byte[]` storage.
