# Trn.Company — Legal Customer (مشتری حقوقی) + Child Entities

> Prerequisite: read `00-Overview-and-Conventions.md` first.

## 1. Purpose

The **company/legal-customer** master (مشتری حقوقی). Companies send employees to private courses (`Course.CompanyId`), and the Company page is the hub for four embedded child grids (phone book rows, key contacts, linked participants, negotiation history) plus a standalone negotiation-history list page.

Legacy source: `Training_Company` entity, `CompanyController`, `_Company.cshtml`. Menu: Training System → Base Information → Customers → Legal (حقوقی).

> **Legacy bug to fix, not copy:** the legacy standalone `ConnectorParticipantController/Edit` page is miswired (its Edit action loads a `Training_Company` row but renders the connector-participant view against it, so the page is broken). The **canonical** behavior is the embedded child grid inside the Company edit page (`_Company.cshtml` renders connector rows via `Training_ConnectorParticipant`) — port that, and give the child its own proper controller (see §3) so the link works.

## 2. Entities

### `Entities/App/Trn/Company.cs`

```csharp
[Display(Name = "مشتری حقوقی / شرکت")]
[Table("Company", Schema = "Trn")]
public class Company : BaseEntity
{
    [DisplayName("عنوان شرکت")]
    [DisplayInfo(null, true, type: SystemType.String, required: true)]
    [MaxLength(256)]
    public string? Title { get; set; }

    [DisplayName("مدیرعامل")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(256)]
    public string? CeoName { get; set; }

    [DisplayName("وب‌سایت")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(256)]
    public string? Website { get; set; }

    [DisplayName("ایمیل")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(256)]
    public string? Email { get; set; }

    [DisplayName("تلفن")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(50)]
    public string? Phone { get; set; }

    [DisplayName("فکس")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(50)]
    public string? Fax { get; set; }

    [DisplayName("آدرس")]
    [DisplayInfo(null, true, type: SystemType.TextArea)]
    public string? Address { get; set; }

    [DisplayName("نوع فعالیت")]
    [DisplayInfo(null, false, type: SystemType.TextArea)]
    public string? ActivityKind { get; set; }

    [DisplayName("توضیحات")]
    [DisplayInfo(null, false, type: SystemType.TextArea)]
    public string? Description { get; set; }

    // Optional link to the shared Party master (mirrors legacy optional ManCompanyId → Gnr_ManCompany).
    public long? PartyId { get; set; }
    public virtual Party? Party { get; set; }

    public virtual ICollection<PhoneBook> PhoneBooks { get; set; } = new List<PhoneBook>();
    public virtual ICollection<KeyParticipant> KeyParticipants { get; set; } = new List<KeyParticipant>();
    public virtual ICollection<ConnectorParticipant> ConnectorParticipants { get; set; } = new List<ConnectorParticipant>();
    public virtual ICollection<TodoList> TodoLists { get; set; } = new List<TodoList>();
}
```

Legacy-field mapping notes:
- `Training_Company.Title` → `Title`; `CeoName` → `CeoName`; `Website/Email/Phone/Fax` likewise.
- `ActivityFieldId` (legacy lookup, `LookupType_FK = 103`, "زمینه فعالیت شرکت" — 29 rows confirmed 2026-09-09 from `TotalSystem.dbo.Gnr_Lookup`: `542` = صنایع غذایی و آشامیدنی, `543` = فولاد, `544` = پزشکی, `545` = دارویی و بهداشتی, `546` = نساجی, `547` = صنایع خودرو, `548` = رنگ و رزین, `549` = سیمان, `550` = نفت، گاز، پالایش و پتروشیمی, `551` = صنایع شیمیایی, `552` = کاشی و سرامیک, `553` = بهداشتی و آرایشی, `554` = لاستیک و پلاستیک, `555` = نیروگاهی، برق و الکترونیک, `556` = سایر, `730` = آهن, `731` = سلولزی, `732` = آرد, `733` = ماشین آلات, `734` = چینی بهداشتی, `735` = صنایع چوب, `736` = تولید بتن, `737` = تولید کفش, `738` = صنعت شیشه, `739` = صنعت گچ, `740` = فرش و موکت, `741` = لوازم خانگی, `742` = لوله و اتصالات, `743` = چاپ و بسته بندی کاغذ) is **excluded** — it is loaded in the legacy code but never shown in the UI and never used in any rule; do not create `ActivityFieldEnum` unless the business explicitly asks for it later (the full value list above is kept here for traceability). The free-text `ActivityField` legacy column → `ActivityKind`.
- `ManCompanyId` (legacy optional FK to the old general company master) → optional `PartyId` FK to `Entities.App.Gnr.Party` in the new system (per the user's architecture decision; see `00-Overview-and-Conventions.md` §2). Purely informational — no cascade, no sync, nullable.
- Legacy `CreatedUserId/CreatedDate/CreatedDateInText` audit columns → framework `BaseEntity` audit fields (do not port).

### `Entities/App/Trn/PhoneBook.cs`

One row = one phone-book entry of the company (internal contacts list). Legacy: `Training_PhoneBook`.

```csharp
[Display(Name = "دفتر تلفن شرکت")]
[Table("PhoneBook", Schema = "Trn")]
public class PhoneBook : BaseEntity
{
    public long CompanyId { get; set; }
    public virtual Company? Company { get; set; }

    [DisplayName("نام")]
    [DisplayInfo(null, true, type: SystemType.String, required: true)]
    [MaxLength(100)]
    public string? Firstname { get; set; }

    [DisplayName("نام خانوادگی")]
    [DisplayInfo(null, true, type: SystemType.String, required: true)]
    [MaxLength(100)]
    public string? Lastname { get; set; }

    [DisplayName("سمت")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(256)]
    public string? Position { get; set; }

    [DisplayName("تلفن داخلی")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(20)]
    public string? InnerPhone { get; set; }

    [DisplayName("تلفن مستقیم")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(20)]
    public string? DirectPhone { get; set; }

    [DisplayName("تلفن همراه")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(20)]
    public string? Mobile { get; set; }

    [DisplayName("ایمیل")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(256)]
    public string? Email { get; set; }

    [DisplayName("توضیحات")]
    [DisplayInfo(null, false, type: SystemType.TextArea)]
    public string? Description { get; set; }
}
```

### `Entities/App/Trn/KeyParticipant.cs`

One row = one "key person" (فرد کلیدی/تصمیم‌گیر) at the company. Legacy: `Training_KeyParticipant`.

```csharp
[Display(Name = "فرد کلیدی شرکت")]
[Table("KeyParticipant", Schema = "Trn")]
public class KeyParticipant : BaseEntity
{
    public long CompanyId { get; set; }
    public virtual Company? Company { get; set; }

    [DisplayName("نام")]
    [DisplayInfo(null, true, type: SystemType.String, required: true)]
    [MaxLength(100)]
    public string? Firstname { get; set; }

    [DisplayName("نام خانوادگی")]
    [DisplayInfo(null, true, type: SystemType.String, required: true)]
    [MaxLength(100)]
    public string? Lastname { get; set; }

    [DisplayName("سمت")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(256)]
    public string? Position { get; set; }

    [DisplayName("تلفن همراه")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(20)]
    public string? Mobile { get; set; }

    [DisplayName("ایمیل")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(256)]
    public string? Email { get; set; }

    [DisplayName("توضیحات")]
    [DisplayInfo(null, false, type: SystemType.TextArea)]
    public string? Description { get; set; }
}
```

### `Entities/App/Trn/ConnectorParticipant.cs`

Junction: links a `Participant` (individual master, see `05-Participant.md`) to this company as its "contact person" (رابط). Legacy: `Training_ConnectorParticipant` (`CompanyId` + `ParticipantId` + optional extra info).

```csharp
[Display(Name = "رابط شرکت")]
[Table("ConnectorParticipant", Schema = "Trn")]
public class ConnectorParticipant : BaseEntity
{
    public long CompanyId { get; set; }
    public virtual Company? Company { get; set; }

    [DisplayName("شرکت‌کننده (رابط)")]
    [DisplayInfo(null, true, type: SystemType.EntitySelector, required: true)]
    public long ParticipantId { get; set; }
    public virtual Participant? Participant { get; set; }

    [DisplayName("سمت در شرکت")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(256)]
    public string? Position { get; set; }

    [DisplayName("توضیحات")]
    [DisplayInfo(null, false, type: SystemType.TextArea)]
    public string? Description { get; set; }
}
```

Business rule: the `ParticipantId` picker must show the `Participant` master list (search by name/national code). Prevent duplicate `(CompanyId, ParticipantId)` pairs — return a friendly `toastr.error` on save if the same participant is already linked to this company.

### `Entities/App/Trn/TodoList.cs`

One row = one negotiation-history / follow-up note (تاریخچه مذاکرات / پیگیری). Legacy: `Training_TodoList` (simple fields: title/description, date, creator).

```csharp
[Display(Name = "تاریخچه مذاکرات / پیگیری")]
[Table("TodoList", Schema = "Trn")]
public class TodoList : BaseEntity
{
    public long? CompanyId { get; set; }
    public virtual Company? Company { get; set; }

    [DisplayName("عنوان")]
    [DisplayInfo(null, true, type: SystemType.String, required: true)]
    [MaxLength(256)]
    public string? Title { get; set; }

    [DisplayName("شرح پیگیری")]
    [DisplayInfo(null, true, type: SystemType.TextArea, required: true)]
    public string? Description { get; set; }

    [DisplayName("تاریخ پیگیری")]
    [DisplayInfo(null, true, type: SystemType.PersionDatePicker)]
    public DateTime? TodoMiladiDate { get; set; }
    public string? TodoShamsiDate { get; set; }
}
```

Follow-up date uses the standard Shamsi/Miladi pair convention (see `00-Overview-and-Conventions.md` §8).

## 3. Controllers

### `WebApp/Controllers/Dynamic/Trn/CompanyController.cs`

Standard CRUD (`Save`/`Add`/`Update`/`Delete`/`Edit`/`New`/`List`/`FetchData`/`ExportToExcel`), route `Panel/Trn/[controller]`, `[ControllerInfo("مشتریان حقوقی (شرکت‌ها)", typeof(Company))]`. No custom business rules on the company itself.

### Child controllers (each follows the `TeacherBankAttachment` child-entity pattern)

- `PhoneBookController` — with `ListByCompanyId(long companyId)`.
- `KeyParticipantController` — with `ListByCompanyId(long companyId)`.
- `ConnectorParticipantController` — with `ListByCompanyId(long companyId)` (+ the duplicate-link guard described above). This **replaces** the broken legacy standalone connector page — implement it correctly as a company-scoped child controller.
- `TodoListController` — with `ListByCompanyId(long companyId)` **and** a standalone `List()` page (see §4 — it appears in the Operation menu independently of any company).

## 4. Views

- `WebApp/Views/Panel/Trn/Company/List.cshtml` — `<datatableprofile entity-Type="typeof(Company)"></datatableprofile>`.
- `WebApp/Views/Panel/Trn/Company/Edit.cshtml` — company field form + **four** `form-action-buttons` buttons — "دفتر تلفن"، "افراد کلیدی"، "رابطین"، "تاریخچه مذاکرات" — each wired like `openSubPage` in `Course/Edit.cshtml`: `appController.addPage('/Panel/Trn/<Child>/ListByCompanyId?companyId=' + id)`. Show these buttons only when editing an existing company (`id > 0`).
- Each child gets `ListByCompanyId.cshtml` + `Edit.cshtml` views. `TodoList` additionally gets a normal `List.cshtml` (standalone page).
- **TodoList dual access** (legacy behavior to preserve): the menu Operation → "تاریخچه مذاکرات" opens the standalone `TodoList/List` page. In the legacy system this standalone list was filtered server-side to rows created by the current user (creator-based row visibility) — preserve that rule: the standalone list shows only rows the current user created (filter `CreatedById == current user`), while the embedded company-scoped grid (`ListByCompanyId`) shows all rows of that company regardless of creator. Company picker on the TodoList edit form is an `EntitySelector<Company>`.
