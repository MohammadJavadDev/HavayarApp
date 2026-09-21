# Trn.Course — Core Course Entity (هسته دوره آموزشی)

> Prerequisite: read `00-Overview-and-Conventions.md` first (especially §6 enum table and §8 conventions).

## 1. Purpose

The **central entity** of the whole module — a scheduled/held training course instance (یک برگزاری دوره). Everything else hangs off it: participants (both tracks), evaluations, certificates, email logs, reports.

Legacy source: `Training_Course` entity, `CourseController`, `_Course.cshtml`. Menu: Training System → Operation → Course (دوره).

## 2. Entity: `Entities/App/Trn/Course.cs`

```csharp
[Display(Name = "دوره آموزشی")]
[Table("Course", Schema = "Trn")]
public class Course : BaseEntity
{
    // ---- Identity / catalog ----
    [DisplayName("بانک دوره (زمینه)")]
    [DisplayInfo(null, true, type: SystemType.EntitySelector, required: true)]
    public long CourseBankId { get; set; }
    public virtual CourseBank? CourseBank { get; set; }

    [DisplayName("کد آموزش")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(50)]
    public string? TrainingCode { get; set; }   // auto-generated, read-only in UI (see rule R1)

    [DisplayName("نوع دوره (CNG و ...)")]
    [DisplayInfo(null, true, type: SystemType.Select)]
    public CourseTypeEnum? CngCourseType { get; set; }   // legacy CngCourseType (LookupType 123)

    [DisplayName("شیوه برگزاری")]
    [DisplayInfo(null, true, type: SystemType.Select, required: true)]
    public CourseExecutingMethodEnum ExecutingMethod { get; set; } = CourseExecutingMethodEnum.PublicCall;

    [DisplayName("نحوه برگزاری")]
    [DisplayInfo(null, true, type: SystemType.Select)]
    public CourseMethodOfHoldingEnum? MethodOfHolding { get; set; }

    // ---- Scheduling ----
    [DisplayName("تاریخ شروع")]
    [DisplayInfo(null, true, type: SystemType.PersionDatePicker, required: true)]
    public DateTime? StartMiladiDate { get; set; }
    public string? StartShamsiDate { get; set; }

    [DisplayName("تاریخ پایان")]
    [DisplayInfo(null, true, type: SystemType.PersionDatePicker, required: true)]
    public DateTime? EndMiladiDate { get; set; }
    public string? EndShamsiDate { get; set; }

    [DisplayName("مدت (دقیقه)")]
    [DisplayInfo(null, true, type: SystemType.Int, required: true)]
    public int? DurationInMinute { get; set; }

    [DisplayName("تعداد روز")]
    [DisplayInfo(null, true, type: SystemType.Int)]
    public int? Days { get; set; }

    [DisplayName("محل برگزاری")]
    [DisplayInfo(null, true, type: SystemType.EntitySelector, required: true)]
    public long? LocationId { get; set; }     // → Gnr.Region (legacy Gnr_City)
    public virtual Region? Location { get; set; }

    [DisplayName("محل اجرا")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(50)]
    public string? ExecutingPlace { get; set; }

    [DisplayName("جایگاه CNG")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(50)]
    public string? CngPlace { get; set; }

    [DisplayName("آخرین مهلت ثبت نام")]
    [DisplayInfo(null, true, type: SystemType.PersionDatePicker)]
    public DateTime? RegistrationDeadlineMiladiDate { get; set; }
    public string? RegistrationDeadlineShamsiDate { get; set; }

    // ---- People / organization ----
    [DisplayName("مدرس")]
    [DisplayInfo(null, true, type: SystemType.EntitySelector, required: true)]
    public long TeacherId { get; set; }
    public virtual TeacherBank? Teacher { get; set; }

    [DisplayName("مدرس دوم")]
    [DisplayInfo(null, true, type: SystemType.EntitySelector)]
    public long? SecondTeacherId { get; set; }
    public virtual TeacherBank? SecondTeacher { get; set; }

    [DisplayName("سابقه همکاری")]
    [DisplayInfo(null, true, type: SystemType.Boolean)]
    public bool HasSynergyHistory { get; set; }

    [DisplayName("نماینده / بازاریاب")]
    [DisplayInfo(null, true, type: SystemType.EntitySelector)]
    public long? AgentId { get; set; }
    public virtual Agent? Agent { get; set; }

    [DisplayName("شرکت (برای دوره اختصاصی)")]
    [DisplayInfo(null, true, type: SystemType.EntitySelector)]
    public long? CompanyId { get; set; }      // required when ExecutingMethod == Private (rule R3)
    public virtual Company? Company { get; set; }

    [DisplayName("واحد مرتبط")]
    [DisplayInfo(null, true, type: SystemType.EntitySelector)]
    public long? RelatedUnitId { get; set; }  // → Hrm.OrgUnit (legacy HRM_OrgUnit)
    public virtual OrgUnit? RelatedUnit { get; set; }

    // ---- Capacity ----
    [DisplayName("ظرفیت")]
    [DisplayInfo(null, true, type: SystemType.Int, required: true)]
    public short Capacity { get; set; }

    [DisplayName("ظرفیت باقی‌مانده")]
    [DisplayInfo(null, true, type: SystemType.Int)]
    public short? RemainingCapacity { get; set; }  // maintained by participant add/remove (rule R4)

    // ---- Status / workflow ----
    [DisplayName("وضعیت دوره")]
    [DisplayInfo(null, true, type: SystemType.Select, required: true)]
    public CourseStatusEnum? Status { get; set; }              // legacy StatusId (108)

    [DisplayName("وضعیت مالی")]
    [DisplayInfo(null, true, type: SystemType.Select, required: true)]
    public CourseFinancialStatusEnum? FinancialStatus { get; set; }  // legacy FinancialStatusId (109)

    [DisplayName("وضعیت گواهی")]
    [DisplayInfo(null, true, type: SystemType.Select)]
    public CourseCertificateStatusEnum? CertificateStatus { get; set; }  // legacy CertificateStatusId (320)

    [DisplayName("وضعیت ارسال دعوت‌نامه")]
    [DisplayInfo(null, true, type: SystemType.Select)]
    public CourseSendInvitationEnum? SendInvitation { get; set; }        // legacy SendInvitationId (319)

    [DisplayName("نوع پرداخت")]
    [DisplayInfo(null, true, type: SystemType.Select)]
    public CoursePaymentTypeEnum? PaymentType { get; set; }              // legacy PaymentTypeId (321)

    [DisplayName("وضعیت حضور")]
    [DisplayInfo(null, true, type: SystemType.Select)]
    public CourseAttendanceStatusEnum? AttendanceStatus { get; set; }    // legacy AttendanceStatusId (322)

    // ---- Misc ----
    [DisplayName("هزینه دوره")]
    [DisplayInfo(null, true, type: SystemType.Money)]
    public decimal? Cost { get; set; }

    [DisplayName("نقطه سر به سری")]
    [DisplayInfo(null, true, type: SystemType.Int)]
    public int? HeadToTheSeriesPoint { get; set; }

    [DisplayName("گواهینامه")]
    [DisplayInfo(null, true, type: SystemType.Boolean)]
    public bool HasCertificates { get; set; }

    [DisplayName("آزمون")]
    [DisplayInfo(null, true, type: SystemType.Boolean)]
    public bool HasExam { get; set; }

    [DisplayName("مالیات")]
    [DisplayInfo(null, true, type: SystemType.Boolean)]
    public bool? HasVat { get; set; }

    [DisplayName("توضیحات")]
    [DisplayInfo(null, false, type: SystemType.TextArea)]
    public string? Description { get; set; }

    // ---- Warranty-period information (اطلاعات دوره گارانتی) ----
    [DisplayName("نام مامور")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(128)]
    public string? AgentName { get; set; }

    [DisplayName("شماره تماس 1")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(50)]
    public string? FirstPhoneNumber { get; set; }

    [DisplayName("شماره تماس 2")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(50)]
    public string? SecondPhoneNumber { get; set; }

    [DisplayName("نمایش تاریخ راه اندازی")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(256)]
    public string? ShowStartupDate { get; set; }   // free text, not a datepicker

    [DisplayName("تعداد دستگاه")]
    [DisplayInfo(null, true, type: SystemType.Int)]
    public byte? NumberOfDevices { get; set; }

    [DisplayName("تعداد نفرات")]
    [DisplayInfo(null, true, type: SystemType.Int)]
    public short? NumberOfPeople { get; set; }

    [DisplayName("ارسال دعوت نامه")]
    [DisplayInfo(null, true, type: SystemType.Boolean)]
    public bool? InvitationSent { get; set; }

    [DisplayName("تاریخ ارسال دعوت نامه")]
    [DisplayInfo(null, true, type: SystemType.PersionDatePicker)]
    public DateTime? SendInvitationMiladiDate { get; set; }
    public string? SendInvitationShamsiDate { get; set; }

    [DisplayName("نام و نام خانوادگی گیرنده دعوت نامه")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(256)]
    public string? RecipientOfInvitation { get; set; }

    [DisplayName("تاریخ حضور در دوره آموزشی")]
    [DisplayInfo(null, true, type: SystemType.PersionDatePicker)]
    public DateTime? AttendingTrainingCourseMiladiDate { get; set; }
    public string? AttendingTrainingCourseShamsiDate { get; set; }

    [DisplayName("محل برگزاری دوره آموزشی")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(512)]
    public string? PlaceOfTrainingCourse { get; set; }

    // ---- Training course titles (not shown in the list grid) ----
    [DisplayName("عناوین دوره های آموزشی")]
    [DisplayInfo(null, false, type: SystemType.String)]
    [MaxLength(1024)]
    public string? TrainingCourseTitles { get; set; }
    [MaxLength(1024)]
    public string? TrainingCourseTitles1 { get; set; }
    [MaxLength(1024)]
    public string? TrainingCourseTitles2 { get; set; }
    [MaxLength(1024)]
    public string? TrainingCourseTitles3 { get; set; }
    [MaxLength(1024)]
    public string? TrainingCourseTitles4 { get; set; }
    [MaxLength(1024)]
    public string? TrainingCourseTitles5 { get; set; }
    [MaxLength(1024)]
    public string? TrainingCourseTitles6 { get; set; }
    [MaxLength(1024)]
    public string? TrainingCourseTitles7 { get; set; }
    [MaxLength(1024)]
    public string? TrainingCourseTitles8 { get; set; }
    [MaxLength(1024)]
    public string? TrainingCourseTitles9 { get; set; }

    public virtual ICollection<CourseParticipant> CourseParticipants { get; set; } = new List<CourseParticipant>();
    public virtual ICollection<CourseCompanyParticipant> CourseCompanyParticipants { get; set; } = new List<CourseCompanyParticipant>();
    public virtual ICollection<CourseEvaluation> CourseEvaluations { get; set; } = new List<CourseEvaluation>();
    public virtual ICollection<CertificateEmailLog> CertificateEmailLogs { get; set; } = new List<CertificateEmailLog>();
}
```

### Field/enum notes

- All legacy `short? ...Id` lookup fields become nullable enums per `00-Overview-and-Conventions.md` §6 (all member lists confirmed 2026-09-09 from `TotalSystem.dbo.Gnr_Lookup`): `CourseStatusEnum` (108): `617` برنامه ریزی, `618` بازاریابی, `619` در حال برگزاری, `620` اجرا شده, `621` به تعویق افتاده; `CourseFinancialStatusEnum` (109): `622` بدهکار, `623` تسویه; `CourseCertificateStatusEnum` (320): `2393` صدور و ارسال, `2394` عدم صدور, `2395` صدور; `CourseSendInvitationEnum` (319): `2390` پست الکترونیک, `2391` نمابر, `2392` شبکه های اجتماعی; `CoursePaymentTypeEnum` (321): `2396` رایگان, `2397` اخذ هزینه; `CourseAttendanceStatusEnum` (322): `2398` اعلام عدم نیاز, `2399` عدم حضور, `2400` حضور قطعی, `2401` عدم پاسخگویی; `CourseMethodOfHoldingEnum` (316): `2379` اختصاصی, `2380` آنلاین, `2381` در محل شرکت هوایار; `CourseTypeEnum` (123): `701` آموزش و صدور, `702` بازآموزی, `703` گارانتی, `1184` آنلاین, `2411` سمینار آموزشی, `2412` هوایار سوخت, `2787` سمینار مشتریان حقوقی, `2788` سمینار مشتریان حقیقی.
- `ExecutingMethod` was **free text** in legacy (`"فراخوان عمومی"` / `"اختصاصی"` — confirmed as the only two `Gnr_Lookup` rows for `LookupType_FK = 113`: `635` = فراخوان عمومی, `636` = اختصاصی) — new system stores `CourseExecutingMethodEnum { PublicCall, Private }`. Data migration must map the two Persian strings to these two members.
- `StartTime`/`EndTime` were Havayar-only extras and have been **removed** (they do not exist on `Training_Course` or `_Course.cshtml`).
- `DurationInMinute` vs `CourseBank.Duration` (hours): these are intentionally different units on different entities — do not "fix" by unifying.
- `DurationInMinute`, `LocationId`, `Status`, and `FinancialStatus` are required (legacy `validatePropertyGrid` in `_Course.cshtml`).
- `Content` / `ExecuteCondition` / `CourseTitle` exist on the legacy entity but **not** on `_Course.cshtml` — they are not ported.
- Shamsi/Miladi pairs (`RegistrationDeadline*`, `SendInvitation*`, `AttendingTrainingCourse*`) are the Havayar equivalent of legacy `*InText` date strings; `ShowStartupDate` stays free text (not a datepicker), matching HTS.
- `TrainingCourseTitles`…`TrainingCourseTitles9` are on the Edit form only — they were never list-grid columns in HTS.
- `TrainingCode` is system-generated and shown read-only (rule R1).

## 3. Controller: `WebApp/Controllers/Dynamic/Trn/CourseController.cs`

Standard CRUD (`Save`/`Add`/`Update`/`Delete`/`Edit`/`New`/`List`/`FetchData`/`ExportToExcel`), route `Panel/Trn/[controller]`, `[ControllerInfo("دوره‌های آموزشی", typeof(Course))]`, plus:

- **`New()` prefill:** when opened from a CourseBank context (optional `courseBankId` query param), prefill `CourseBankId` **and Include/load `CourseBank`** so the EntitySelector shows the title — mirrors the legacy "create course from bank row" affordance.
- **`Edit`/`New` Includes:** `CourseBank`, `Teacher`, `SecondTeacher`, `Agent`, `Company`, `RelatedUnit`, `Location` so EntitySelectors have display values.
- **`ValidateCourse`:** required HTS fields — `DurationInMinute`, `LocationId`, `Status`, `FinancialStatus` — plus R3 (Private → `CompanyId`).
- No other custom endpoints here — participant/evaluation/certificate/email actions live on their own controllers/pages (see `07`/`08`/`09`).

## 4. Business rules (must preserve)

- **R1 — TrainingCode auto-generation (BeforeAdd trigger):** on first save, generate `TrainingCode`: prefix `TRCNG` if the selected `CourseBank.CourseField == CourseFieldEnum.Cng`, else `TRHY`, followed by a sequential number (legacy used a count-based sequence — implement as `prefix + next available sequence`, enforced unique via a unique index on `TrainingCode`). Put this in a `WebApp/Actions/Trn/CourseAction.cs` `[EntityAction(typeof(Course), BeforeAdd)]` so it runs regardless of caller. `TrainingCode` is read-only in the UI after creation.
- **R2 — RemainingCapacity init:** on first save, `RemainingCapacity = Capacity`. On later edits of `Capacity`, do **not** reset `RemainingCapacity` blindly — participant add/remove maintains it (rule R4 in `07-CourseParticipant-and-CourseCompanyParticipant.md`). If `Capacity` is lowered below the already-registered count, reject with a friendly error.
- **R3 — Private courses require a company:** if `ExecutingMethod == Private`, `CompanyId` is required (validate server-side in `Save`/`Add`/`Update`, plus client-side required toggling in the view).
- **R3b — HTS required fields:** `DurationInMinute`, `LocationId`, `Status`, and `FinancialStatus` are required (server `ValidateCourse` + client `data-required` / `validateError`).
- **R4 — Capacity accounting** lives on the participant controllers (see `07`), not here.
- **R5 — Second evaluation creates a follow-up TodoList** lives on the evaluation side (see `08-CourseEvaluation.md`).

## 5. Views

- `WebApp/Views/Panel/Trn/Course/List.cshtml` — `<datatableprofile entity-Type="typeof(Course)"></datatableprofile>`. Seed profile `Data/Seed/TrnProfiles/course_listinfo.json` includes HTS grid columns (`Days`, flags, places, warranty fields). Training-course titles are Edit-only. If the profile already exists in `system.SystemDataTableProfile`, re-import from the DataTable Profile UI (startup does not upsert JSON).
- `WebApp/Views/Panel/Trn/Course/Edit.cshtml` — the largest form in the module. Sections:
  1. Course identity: `CourseBankId` (`EntitySelector<CourseBank>`), `TrainingCode` (read-only text), `CngCourseType` (select), `ExecutingMethod` (select — toggles visibility/requiredness of the `CompanyId` picker per R3 and enables the email-log button only when Private), `MethodOfHolding` (select).
  2. Scheduling: `StartShamsiDate`/`EndShamsiDate` (Shamsi pickers), `DurationInMinute` (required), `Days`, `LocationId` (`EntitySelector<Region>`, required), `ExecutingPlace`, `CngPlace`, `RegistrationDeadlineShamsiDate`.
  3. People: `TeacherId` (`EntitySelector<TeacherBank>`, required), `SecondTeacherId` (optional, same picker), `HasSynergyHistory` (checkbox), `AgentId` (optional `EntitySelector<Agent>` — must support inline create like the legacy dropdown did), `CompanyId` (`EntitySelector<Company>`, required iff Private), `RelatedUnitId` (`EntitySelector<OrgUnit>`).
  4. Capacity: `Capacity`, `RemainingCapacity` (read-only display).
  5. Status/workflow: the six status selects (`Status` and `FinancialStatus` required).
  6. Misc: `Cost`, `HeadToTheSeriesPoint`, `HasCertificates` / `HasExam` / `HasVat` (checkboxes), `Description`.
  7. Warranty-period information: `AgentName`, `FirstPhoneNumber`, `SecondPhoneNumber`, `ShowStartupDate` (free text), `NumberOfDevices`, `NumberOfPeople`, `InvitationSent`, `SendInvitationShamsiDate`, `RecipientOfInvitation`, `AttendingTrainingCourseShamsiDate`, `PlaceOfTrainingCourse`. (`RelatedUnit` / `PaymentType` / `AttendanceStatus` / `SendInvitation` / `MethodOfHolding` / `CertificateStatus` / `Description` stay in the sections above — not duplicated here.)
  8. Training course titles: `TrainingCourseTitles` … `TrainingCourseTitles9`.
  9. `form-action-buttons` child-page buttons (visible only when `id > 0`), wired via `appController.addPage(...)` exactly like `openSubPage` in `Course/Edit.cshtml`:
     - "شرکت‌کنندگان" → if `TrainingCode` starts with `TRCNG` **or** `ExecutingMethod == PublicCall (635)` → `/Panel/Trn/CourseParticipant/ListByCourseId?courseId=` + id; otherwise (Private) → `/Panel/Trn/CourseCompanyParticipant/ListByCourseId?courseId=` + id
     - "ارزیابی دوره" → `/Panel/Trn/CourseEvaluation/ListByCourseId?courseId=` + id (see `08`)
     - "گواهی‌ها" → `/Panel/Trn/Certificate/ListByCourseId?courseId=` + id (unified page covering both tracks)
     - "لاگ ایمیل گواهی" → `/Panel/Trn/CertificateEmailLog/ListByCourseId?courseId=` + id — **enabled only when `ExecutingMethod == Private (636)`**
- Standard `savefn` wiring (`save`/`saveandnew`/`saveandclose`).
