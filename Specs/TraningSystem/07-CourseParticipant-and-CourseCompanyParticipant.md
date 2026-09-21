# Trn.CourseParticipant + Trn.CourseCompanyParticipant — The Two Enrollment Tracks

> Prerequisite: read `00-Overview-and-Conventions.md` first (especially §4 — the two-track model).

## 1. Purpose

Course enrollment rows. The legacy system keeps **two parallel tables** for the two ways a course runs (see `06-Course.md`, `Course.ExecutingMethod`):

| Track | Legacy table | New entity | Used when |
|---|---|---|---|
| Public / CNG (فراخوان عمومی) | `Training_CourseParticipant` | `Trn.CourseParticipant` | `Course.ExecutingMethod == PublicCall` (or CNG-field courses) |
| Private / company-dedicated (اختصاصی) | `Training_CourseCompanyParticipant` | `Trn.CourseCompanyParticipant` | `Course.ExecutingMethod == Private`, always tied to `Course.CompanyId` |

Both point at the same `Participant` master (`05-Participant.md`). Keep the two tables — do not merge them: certificates, evaluations, and reports all branch on which track a row came from (see `08`/`09`/`10`).

Legacy source: `Training_CourseParticipant` + `Training_CourseCompanyParticipant` entities, controller actions on `CourseController` (add/remove participant endpoints), views `_CourseParticipant.cshtml` + `_CourseCompanyParticipant.cshtml` (+ certificate variants `_CourseParticipantCertificate.cshtml` / `_CourseCompanyParticipantCertificate.cshtml`, which belong to `09`, not here).

## 2. Entities

### `Entities/App/Trn/CourseParticipant.cs`

```csharp
[Display(Name = "شرکت‌کننده دوره (عمومی)")]
[Table("CourseParticipant", Schema = "Trn")]
public class CourseParticipant : BaseEntity
{
    public long CourseId { get; set; }
    public virtual Course? Course { get; set; }

    [DisplayName("شرکت‌کننده")]
    [DisplayInfo(null, true, type: SystemType.EntitySelector, required: true)]
    public long ParticipantId { get; set; }
    public virtual Participant? Participant { get; set; }

    [DisplayName("وضعیت ثبت‌نام")]
    [DisplayInfo(null, true, type: SystemType.Select)]
    public ParticipantRegisterStatusEnum? ParticipantRegisterStatus { get; set; }  // legacy ParticipantRegisterStatusId (110)

    [DisplayName("توضیحات")]
    [DisplayInfo(null, false, type: SystemType.TextArea)]
    public string? Description { get; set; }

    public virtual Certificate? Certificate { get; set; }  // 0..1 (see 09)
}
```

### `Entities/App/Trn/CourseCompanyParticipant.cs`

Same shape, private track:

```csharp
[Display(Name = "شرکت‌کننده دوره (اختصاصی)")]
[Table("CourseCompanyParticipant", Schema = "Trn")]
public class CourseCompanyParticipant : BaseEntity
{
    public long CourseId { get; set; }
    public virtual Course? Course { get; set; }

    [DisplayName("شرکت‌کننده")]
    [DisplayInfo(null, true, type: SystemType.EntitySelector, required: true)]
    public long ParticipantId { get; set; }
    public virtual Participant? Participant { get; set; }

    [DisplayName("وضعیت ثبت‌نام")]
    [DisplayInfo(null, true, type: SystemType.Select)]
    public ParticipantRegisterStatusEnum? ParticipantRegisterStatus { get; set; }

    [DisplayName("توضیحات")]
    [DisplayInfo(null, false, type: SystemType.TextArea)]
    public string? Description { get; set; }

    public virtual Certificate? Certificate { get; set; }  // 0..1 (see 09)
}
```

### Enum

`ParticipantRegisterStatusEnum` (shared by both): confirmed members from legacy `Gnr_Lookup` where `LookupType_FK = 110` ("وضعیت ثبت نام", queried 2026-09-09) — exactly two rows: `624` = رزرو, `625` = قطعی.

## 3. Controllers

- `WebApp/Controllers/Dynamic/Trn/CourseParticipantController.cs` — CRUD + `ListByCourseId(long courseId)`.
- `WebApp/Controllers/Dynamic/Trn/CourseCompanyParticipantController.cs` — CRUD + `ListByCourseId(long courseId)`.

Both enforce the same rules (see §4). Route `Panel/Trn/[controller]` with `[ControllerInfo(...)]` on each.

## 4. Business rules (must preserve)

- **R4a — Capacity decrement (AfterAdd trigger):** adding a participant row decrements the parent `Course.RemainingCapacity` by 1. Implement in `WebApp/Actions/Trn/CourseParticipantAction.cs` + `CourseCompanyParticipantAction.cs` (`AfterAdd`), so it runs regardless of caller.
- **R4b — Capacity increment (AfterDelete trigger):** deleting a participant row increments `Course.RemainingCapacity` by 1 (same Action classes, `AfterDelete`).
- **R4c — No overbooking:** refuse the add (friendly `toastr.error`) when `Course.RemainingCapacity <= 0`.
- **R4d — No double registration:** refuse the add when the same `ParticipantId` is already enrolled in the same `CourseId` **in either track** (check both tables, not just the current one).
- **Track guard:** `CourseParticipant` rows may only be added to courses with `ExecutingMethod == PublicCall`; `CourseCompanyParticipant` rows only to `ExecutingMethod == Private`. Enforce server-side; the Course edit page only shows the button for the matching track (see `06-Course.md` §5).

## 5. Views

- `CourseParticipant/ListByCourseId.cshtml` + `Edit.cshtml`; `CourseCompanyParticipant/ListByCourseId.cshtml` + `Edit.cshtml` — child list/edit views per the convention in `00-Overview-and-Conventions.md` §8. The edit form is just the participant picker (`EntitySelector<Participant>` with search by name/national code) + register-status select + description.
- The Course edit page links to exactly one of these two lists depending on `ExecutingMethod` (see `06-Course.md` §5) — do not show both buttons at once.
