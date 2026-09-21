# Trn.CourseEvaluation — Post-Course Evaluation (ارزیابی دوره)

> Prerequisite: read `00-Overview-and-Conventions.md` first.

## 1. Purpose

Post-course evaluation questionnaires filled per course (one row per respondent/evaluation round), with **19 scored questions** plus a computed average, an "evaluated" flag on the course, and a rule that auto-creates a follow-up note on the second evaluation.

Legacy source: `Training_CourseEvaluation` entity, evaluation actions on `CourseController`, view `_CourseEvaluation.cshtml`, plus the read views `VwTrainingCourseEvaluationHeader/Details` and the `CourseEvaluationChart` report class (chart → ReportBuilder, see §5).

## 2. Entity: `Entities/App/Trn/CourseEvaluation.cs`

```csharp
[Display(Name = "ارزیابی دوره")]
[Table("CourseEvaluation", Schema = "Trn")]
public class CourseEvaluation : BaseEntity
{
    public long CourseId { get; set; }
    public virtual Course? Course { get; set; }

    [DisplayName("ارزیاب (شرکت‌کننده)")]
    [DisplayInfo(null, true, type: SystemType.EntitySelector)]
    public long? ParticipantId { get; set; }   // respondent; optional to allow anonymous/aggregate rows
    public virtual Participant? Participant { get; set; }

    // 19 scored questions (legacy Q1..Q19, each 1–5). Keep individual columns — reports average per question.
    [DisplayName("سوال ۱")]
    [DisplayInfo(null, true, type: SystemType.Int)]
    public short? Q1 { get; set; }
    // ... Q2 .. Q19, same shape ...
    [DisplayName("سوال ۱۹")]
    [DisplayInfo(null, true, type: SystemType.Int)]
    public short? Q19 { get; set; }

    [DisplayName("میانگین امتیاز")]
    [DisplayInfo(null, true, type: SystemType.Decimal)]
    public decimal? AverageScore { get; set; }  // computed in app layer (rule R6), read-only in UI

    [DisplayName("نظر / پیشنهاد")]
    [DisplayInfo(null, false, type: SystemType.TextArea)]
    public string? Comment { get; set; }
}
```

Notes:
- Validate each `Qn` is within 1–5 server-side (legacy had no strict validation — add it; the scale is fixed by the questionnaire).
- `AverageScore` is **computed in the application layer** on save (rule R6) — the new system has no DB-computed columns; do not try to replicate the legacy computed-column behavior in SQL.

## 3. Controller: `WebApp/Controllers/Dynamic/Trn/CourseEvaluationController.cs`

CRUD + `ListByCourseId(long courseId)`, route `Panel/Trn/[controller]`, `[ControllerInfo("ارزیابی دوره", typeof(CourseEvaluation))]`, plus:

- **R5 — Auto follow-up on 2nd evaluation (AfterAdd trigger):** when a second `CourseEvaluation` row is added for the same `CourseId`, auto-create a `TodoList` row (see `04-Company.md`) as a follow-up reminder linked to the course's company (if the course has one). Implement in `WebApp/Actions/Trn/CourseEvaluationAction.cs` (`AfterAdd`). If the course has no company, still count the evaluation but skip the TodoList creation silently (do not error).
- **R6 — Average + evaluated flag (BeforeSave trigger):** on every save, recompute `AverageScore = AVG(Q1..Q19, ignoring nulls)` and set the parent `Course`'s evaluated state (legacy flipped an `IsEvaluated`-style flag on the course once any evaluation existed — preserve the semantics: course counts as evaluated iff ≥1 evaluation row exists). Same Action class (`BeforeSave`/`AfterSave`).

## 4. Views

- `CourseEvaluation/ListByCourseId.cshtml` — child grid (respondent, average, date) opened from the Course edit page's "ارزیابی دوره" button (see `06-Course.md` §5).
- `CourseEvaluation/Edit.cshtml` — respondent picker (`EntitySelector<Participant>` limited to participants enrolled in this course — pass `courseId` as the selector filter) + the 19 question inputs (numeric 1–5, grouped under a "سوالات ارزیابی" section header) + comment. `AverageScore` shown read-only.

## 5. Evaluation print/chart (ReportBuilder — no code)

- The legacy `CourseEvaluationChart` report class becomes a **Stimulsoft chart template** in ReportBuilder fed by a SavedQuery over `Trn.CourseEvaluation` joined to `Trn.Course` (per-question averages + overall average per course). Add a "چاپ نمودار ارزیابی" button on the `ListByCourseId` page that opens `ReportBuilderController.ViewReport` for that template with `courseId` as the parameter (see `00-Overview-and-Conventions.md` §7 and `10-Reports.md` for the SavedQuery mechanics).
- Legacy read views `VwTrainingCourseEvaluationHeader/Details` and `VwCourseEvaluation` were report backing views only — do **not** create entities for them; their logic becomes the SavedQuery behind the chart/print template.
