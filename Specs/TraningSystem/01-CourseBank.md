# Trn.CourseBank — Course Catalog / Field Master

> Prerequisite: read `00-Overview-and-Conventions.md` first (schema/namespace conventions, enum conversion policy).

## 1. Purpose

Master/catalog table of "course fields" (subjects that can be taught) — e.g. "اپراتوری CNG", generic technical/HSE topics, etc. Every `Course` must reference exactly one `CourseBank` row. This is simple reference/base data with no workflow.

Legacy source: `Training_CourseBank` entity, `CourseBankController` (`Areas\TrainingSystem\Controllers\Banks\CourseBankController.cs`), view `_CourseBank.cshtml`. Menu: Training System → Base Information → Banks → Course Bank.

## 2. Entity: `Entities/App/Trn/CourseBank.cs`

```csharp
[Display(Name = "بانک دوره‌ها")]
[Table("CourseBank", Schema = "Trn")]
public class CourseBank : BaseEntity
{
    [DisplayName("زمینه دوره")]
    [DisplayInfo(null, true, type: SystemType.Select, required: true)]
    public CourseFieldEnum CourseField { get; set; }

    [DisplayName("عنوان زمینه دوره")]
    [DisplayInfo(null, true, type: SystemType.String, required: true)]
    [MaxLength(256)]
    public string? CourseFieldTitle { get; set; }

    [DisplayName("عنوان زمینه دوره (لاتین)")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(256)]
    public string? CourseFieldTitleInLatin { get; set; }

    [DisplayName("نوع دوره")]
    [DisplayInfo(null, true, type: SystemType.Select)]
    public CourseBankTypeEnum? Type { get; set; }

    [DisplayName("مدت زمان (ساعت)")]
    [DisplayInfo(null, true, type: SystemType.Int, required: true)]
    public short Duration { get; set; }
}
```

Notes:
- `Duration` is in **hours** in the legacy system (UI label was "DurationHours") — keep the same unit; do not silently convert to minutes (that conversion only happens on `Course.DurationInMinute`, a separate concept — see `06-Course.md`).
- `CourseField` (legacy `CourseFieldId`, `LookupType_FK = 99`, confirmed 2026-09-09 from `TotalSystem.dbo.Gnr_Lookup`) and `Type` (legacy `Type`, `LookupType_FK = 122`, confirmed) are lookup-driven in the legacy system — convert to real enums per `00-Overview-and-Conventions.md` §6. Confirmed members: `CourseField` = `502` سی ان جی (CNG — drives training-code prefix and CNG certificate templates downstream, see `06-Course.md` and `09-Certificate-and-CertificateEmailLog.md`), `504` فنی مهندسی, `505` سایر; `Type` = `698` اپراتوری, `699` تکنسینی, `700` غیر سی ان جی. Make sure the `502` member exists in `CourseFieldEnum` with a clear name (e.g. `Cng = 502`).

### Enums

`Entities/App/Trn/Enums/CourseFieldEnum.cs` — confirmed members (legacy `Gnr_Lookup`, `LookupType_FK = 99`, "زمینه دوره"): `502` = سی ان جی (name it `Cng`), `504` = فنی مهندسی, `505` = سایر.

`Entities/App/Trn/Enums/CourseBankTypeEnum.cs` — confirmed members (legacy `Gnr_Lookup`, `LookupType_FK = 122`, "نوع دوره"): `698` = اپراتوری, `699` = تکنسینی, `700` = غیر سی ان جی.

## 3. Controller: `WebApp/Controllers/Dynamic/Trn/CourseBankController.cs`

Standard CRUD controller, no custom business rules (legacy `DoOperation` had no logic beyond audit-field overwrite, which the new framework already handles automatically):

- `[Route("Panel/Trn/[controller]")]`, `[ControllerInfo("بانک دوره‌ها", typeof(CourseBank))]`
- `Save` / `Add` / `Update` / `Delete` / `Edit(long? id)` / `New()` / `List()` / `FetchData` / `ExportToExcel` — exactly the shape in `CourseController.cs`.

No permission subtleties beyond the standard `[ActionDisplayName(...)]` on each action per `havayar-add-new-page.mdc`.

## 4. Views

### `WebApp/Views/Panel/Trn/CourseBank/List.cshtml`
```cshtml
@using Entities.App.Trn
<datatableprofile entity-Type="typeof(CourseBank)"></datatableprofile>
```

### `WebApp/Views/Panel/Trn/CourseBank/Edit.cshtml`
Simple single-section form:
- `CourseField` — dropdown/select bound to `CourseFieldEnum` (`data-bind="courseField"`, `asp-for="CourseField"`, `SystemType.Select` renders as a normal select automatically per framework convention — no `EntitySelector` needed since it's an enum, not an entity FK).
- `CourseFieldTitle` — text input, required.
- `CourseFieldTitleInLatin` — text input, LTR (`dir="ltr"` or matching class used elsewhere for Latin-text fields, e.g. compare to the `Email` input in `TeacherBank/Edit.cshtml`).
- `Type` — dropdown bound to `CourseBankTypeEnum`.
- `Duration` — numeric input with the standard numeric `data-inputmask` pattern (see `havayar-js-jquery-conventions.mdc` §"Numeric inputs").

Standard `savefn` save wiring (see `havayar-js-jquery-conventions.mdc` §4) with `save`/`saveandnew`/`saveandclose` buttons only — no custom buttons needed.

## 5. Downstream consumers (context only — do not implement here)

- `Course.CourseBankId` (required FK) — see `06-Course.md`. The Course-creation training-code logic checks whether the selected `CourseBank.CourseField == CourseFieldEnum.Cng` to decide the `TRCNG`/`TRHY` prefix and CNG-specific certificate routing.
