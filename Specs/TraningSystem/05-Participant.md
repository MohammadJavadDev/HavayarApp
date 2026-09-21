# Trn.Participant — Individual Customer (مشتری حقیقی / شرکت‌کننده)

> Prerequisite: read `00-Overview-and-Conventions.md` first.

## 1. Purpose

Master table of **individuals/real customers** (مشتری حقیقی). Participants are enrolled into courses via `CourseParticipant` (public track) or `CourseCompanyParticipant` (private track) — see `07-CourseParticipant-and-CourseCompanyParticipant.md` — and can be linked to a company as its contact person via `ConnectorParticipant` (see `04-Company.md`).

Legacy source: `Training_Participant` entity, `ParticipantController`, `_Participant.cshtml`. Menu: Training System → Base Information → Customers → Real (حقیقی).

## 2. Entity: `Entities/App/Trn/Participant.cs`

```csharp
[Display(Name = "مشتری حقیقی / شرکت‌کننده")]
[Table("Participant", Schema = "Trn")]
public class Participant : BaseEntity
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

    [DisplayName("نام پدر")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(100)]
    public string? FatherName { get; set; }

    [DisplayName("کد ملی")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(10)]
    public string? NationalCode { get; set; }

    [DisplayName("شماره شناسنامه")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(20)]
    public string? BirthCertificateNo { get; set; }

    [DisplayName("جنسیت")]
    [DisplayInfo(null, true, type: SystemType.Select)]
    public GenderEnum? Gender { get; set; }

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

    [DisplayName("تلفن")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(20)]
    public string? Phone { get; set; }

    [DisplayName("ایمیل")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(256)]
    public string? Email { get; set; }

    [DisplayName("آدرس")]
    [DisplayInfo(null, true, type: SystemType.TextArea)]
    public string? Address { get; set; }

    [DisplayName("محل کار")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(256)]
    public string? Workplace { get; set; }

    [DisplayName("سمت شغلی")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(256)]
    public string? JobPosition { get; set; }

    [DisplayName("شرکت (محل اشتغال)")]
    [DisplayInfo(null, true, type: SystemType.EntitySelector)]
    public long? CompanyId { get; set; }
    public virtual Company? Company { get; set; }

    [DisplayName("عکس")]
    [DisplayInfo(null, true, type: SystemType.File)]
    public long? PhotoFileId { get; set; }
    public virtual FileEntity? PhotoFile { get; set; }

    [DisplayName("توضیحات")]
    [DisplayInfo(null, false, type: SystemType.TextArea)]
    public string? Description { get; set; }

    // Optional link to the shared Party master (symmetric with Company.PartyId — per user decision).
    public long? PartyId { get; set; }
    public virtual Party? Party { get; set; }
}
```

### Enums

- `GenderEnum` (`Entities/App/Trn/Enums/GenderEnum.cs`): confirmed from legacy `Gnr_Lookup` (`LookupType_FK = 121`, "جنسیت", queried 2026-09-09) — exactly two rows, no further members: `695` = آقا (Male), `696` = خانم (Female). Matches the certificate-generation SQL salutation logic.
- `EducationEnum`: **shared** with `TeacherBank.Education` (see `02-TeacherBank.md`) — confirmed members from legacy `Gnr_Lookup` where `LookupType_FK = 100` ("تحصیلات"): `506` = دیپلم, `507` = فوق دیپلم, `508` = لیسانس, `509` = فوق لیسانس, `510` = دکترا, `2457` = فوق دکتری, `2458` = دانشجوی (فوق دیپلم), `2459` = دانشجوی (لیسانس), `2460` = دانشجوی (فوق لیسانس), `2461` = دانشجوی (دکتری).

### Legacy-field notes

- `Training_Participant.WorkPlace` (legacy spelling) → `Workplace`.
- Photo: legacy stored the image inline; new system uses `FileEntity` FK (`PhotoFileId`) — do not port any `byte[]` column.
- `CompanyId` (optional employer link) is informational only — it does not constrain which company courses the participant can join.
- Legacy concatenated `FullName` columns/usages → computed display value; never a stored column.

## 3. Controller: `WebApp/Controllers/Dynamic/Trn/ParticipantController.cs`

Standard CRUD (`Save`/`Add`/`Update`/`Delete`/`Edit`/`New`/`List`/`FetchData`/`ExportToExcel`), route `Panel/Trn/[controller]`, `[ControllerInfo("مشتریان حقیقی (شرکت‌کنندگان)", typeof(Participant))]`, plus one business rule:

1. **Duplicate-name warning (legacy behavior to preserve):** on `Add`/`Save` for a new row, if another participant already exists with the same `Firstname + Lastname`, do **not** block the save — show a warning (`toastr.warning("شرکت‌کننده‌ای با همین نام وجود دارد")`) and continue saving. (Legacy showed a client-side warning only; keep it non-blocking.)

## 4. Views

- `WebApp/Views/Panel/Trn/Participant/List.cshtml` — `<datatableprofile entity-Type="typeof(Participant)"></datatableprofile>`. The legacy participant list also surfaced related company/course info inline; in the new system the equivalent is clicking through to the participant's course history — no denormalized columns needed on the entity.
- `WebApp/Views/Panel/Trn/Participant/Edit.cshtml` — field form (all fields above; `CompanyId` via `Html.EntitySelector<Company>(...)`; photo via the framework file-upload control) with standard `savefn` wiring. Show the duplicate-name warning returned from the save response without losing the form state.

## 5. Downstream consumers (context only)

- `CourseParticipant.ParticipantId` and `CourseCompanyParticipant.ParticipantId` — see `07-CourseParticipant-and-CourseCompanyParticipant.md`.
- `ConnectorParticipant.ParticipantId` (company contact person) — see `04-Company.md`.
- Certificate printing resolves participant identity fields (name, father name, national code, gender salutation) — see `09-Certificate-and-CertificateEmailLog.md`.
