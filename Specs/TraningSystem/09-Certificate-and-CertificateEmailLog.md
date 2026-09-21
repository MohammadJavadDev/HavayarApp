# Trn.Certificate + Trn.CertificateEmailLog — Certificates & Email Sending (گواهی‌نامه‌ها)

> Prerequisite: read `00-Overview-and-Conventions.md` first (especially §7 — ReportBuilder strategy).

## 1. Purpose

Issuing course **certificates** (گواهی‌نامه) per participant and **emailing** them, with a send log. The legacy system has 10 certificate layout variants + 1 CNG layout, chosen by course field/type — all become Stimulsoft templates in ReportBuilder (no C# report code).

Legacy source: `Training_Certificate` + `Training_CertificateEmailLog` entities, certificate/email actions on `CourseController`, views `_CourseParticipantCertificate.cshtml` + `_CourseCompanyParticipantCertificate.cshtml`, report classes `Certificate1.cs`…`Certificate10.cs` + `Cng.cs`.

## 2. Entities

### `Entities/App/Trn/Certificate.cs`

One row per issued certificate. Exactly one of the two participant FKs is set (mirrors the legacy schema — the track determines which):

```csharp
[Display(Name = "گواهی‌نامه دوره")]
[Table("Certificate", Schema = "Trn")]
public class Certificate : BaseEntity
{
    // Exactly one of these two is set; the other stays null.
    public long? CourseParticipantId { get; set; }
    public virtual CourseParticipant? CourseParticipant { get; set; }

    public long? CourseCompanyParticipantId { get; set; }
    public virtual CourseCompanyParticipant? CourseCompanyParticipant { get; set; }

    [DisplayName("شماره گواهی")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(50)]
    public string? CertificateNo { get; set; }  // auto-generated (rule R7), read-only in UI

    [DisplayName("تاریخ صدور")]
    [DisplayInfo(null, true, type: SystemType.PersionDatePicker)]
    public DateTime? IssueMiladiDate { get; set; }
    public string? IssueShamsiDate { get; set; }

    [DisplayName("قالب گواهی")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(50)]
    public string? TemplateKey { get; set; }  // which of the 11 templates was used (see §4)

    [DisplayName("توضیحات")]
    [DisplayInfo(null, false, type: SystemType.TextArea)]
    public string? Description { get; set; }
}
```

### `Entities/App/Trn/CertificateEmailLog.cs`

One row per certificate-email send attempt. Legacy: `Training_CertificateEmailLog`.

```csharp
[Display(Name = "لاگ ارسال ایمیل گواهی")]
[Table("CertificateEmailLog", Schema = "Trn")]
public class CertificateEmailLog : BaseEntity
{
    public long CourseId { get; set; }
    public virtual Course? Course { get; set; }

    public long? CertificateId { get; set; }
    public virtual Certificate? Certificate { get; set; }

    [DisplayName("گیرنده")]
    [DisplayInfo(null, true, type: SystemType.String, required: true)]
    [MaxLength(256)]
    public string? RecipientEmail { get; set; }

    [DisplayName("ارسال موفق بود")]
    [DisplayInfo(null, true, type: SystemType.Boolean)]
    public bool IsSuccess { get; set; }

    [DisplayName("پیام نتیجه")]
    [DisplayInfo(null, false, type: SystemType.TextArea)]
    public string? ResultMessage { get; set; }
}
```

## 3. Controllers

- `CertificateController` — CRUD + `ListByCourseId(long courseId)` (union of both tracks for the course, showing participant name + certificate no + template). Enforces rule R7. Route `Panel/Trn/[controller]`.
- `CertificateEmailLogController` — read-only list + `ListByCourseId(long courseId)` (no edit page needed beyond view; failed sends can be retried via the resend button in §5).

## 4. Business rules (must preserve)

- **R7 — Certificate auto-numbering (BeforeAdd trigger):** `CertificateNo` is generated on first save with a per-course-field/type numbering scheme (legacy had distinct sequences per course field and certificate type — preserve the semantics: numbers are unique **within** the same course field/type series, not globally). Implement in `WebApp/Actions/Trn/CertificateAction.cs` (`BeforeAdd`); unique index on `(TemplateKey, CertificateNo)`. Read-only in UI.
- **R8 — Template selection:** the certificate template is chosen by the course's field/type at print time:
  - CNG-field courses (`CourseBank.CourseField == CourseFieldEnum.Cng`) → the `Cng` template.
  - Other courses → one of `Certificate1`…`Certificate10` according to `Course.CngCourseType` (the `CourseTypeEnum` value — e.g. warranty/online/seminar variants). Store the chosen template's key in `Certificate.TemplateKey` at issue time so reprints use the same layout even if the course type later changes.
  - Any `CourseTypeEnum` value without a dedicated template falls back to a default (pick `Certificate1` as the default) — document the final mapping table in the ReportBuilder template descriptions when creating them.
- **Salutation data:** certificate bodies use participant `Gender` for the آقا/خانم salutation (legacy SQL logic: `Gender 695` → آقا, `696` → خانم) plus `Firstname/Lastname/FatherName/NationalCode`, course title/dates/duration, and teacher name. Whoever designs each Stimulsoft template must include exactly these fields — the SavedQuery behind every certificate template selects this fixed column set (see §5).

## 5. Views & email flow

- Certificate `ListByCourseId.cshtml` (opened from the Course edit page's "گواهی‌ها" button): rows per enrolled participant (both tracks) with columns [participant, track, certificate no (empty if not yet issued), template, issue date] + per-row buttons:
  - **"صدور گواهی"** → `CertificateController.Issue` (creates the `Certificate` row via R7+R8, then opens the matching ReportBuilder template for preview/print).
  - **"ارسال ایمیل"** → sends the rendered certificate PDF to `Participant.Email`, writes a `CertificateEmailLog` row (`IsSuccess`/`ResultMessage` accordingly), shows `toastr.success/error`. Uses whatever SMTP/email service HavayarApp already provides — do not build a new mailer; if none exists, sending becomes a documented manual step (download + send) and this button only logs the manual send.
  - **"ارسال مجدد"** on failed log rows → retries the send and appends a new log row (never overwrites history).
- `CertificateEmailLog/ListByCourseId.cshtml` (opened from the Course edit page's "لاگ ایمیل گواهی" button): read-only grid [recipient, certificate no, success, message, date].
- **No C# report-rendering code** — preview/print goes through `ReportBuilderController.ViewReport`/`StimulSoftViewReport` with the certificate SavedQuery + `certificateId` parameter (mechanics per `00-Overview-and-Conventions.md` §7).
