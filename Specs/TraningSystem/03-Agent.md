# Trn.Agent — Agent / Marketer Master

> Prerequisite: read `00-Overview-and-Conventions.md` first.

## 1. Purpose

Small master table of **agents/marketers** (نماینده/بازاریاب) who bring course participants to the training center. `Course` has an optional `AgentId` FK. In the legacy system this was only ever a dropdown on the Course edit page — there was **no dedicated Agent CRUD page or menu entry** at all — but a master table with no CRUD page cannot be maintained, so create a normal List/Edit page for it under Base Information.

Legacy source: `Training_Agent` entity only (no controller, no view, no menu in legacy Training System).

## 2. Entity: `Entities/App/Trn/Agent.cs`

```csharp
[Display(Name = "نماینده / بازاریاب")]
[Table("Agent", Schema = "Trn")]
public class Agent : BaseEntity
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

    [DisplayName("منطقه")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(256)]
    public string? Zone { get; set; }

    [DisplayName("تلفن همراه")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(20)]
    public string? Mobile { get; set; }

    [DisplayName("تلفن")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [MaxLength(20)]
    public string? Phone { get; set; }

    [DisplayName("آدرس")]
    [DisplayInfo(null, true, type: SystemType.TextArea)]
    public string? Address { get; set; }
}
```

Notes:
- Legacy `ZoneId` was an FK to `Crm_Zone`. The new system has no `Crm_Zone` concept — keep this as **free text `Zone`** unless whoever implements the UI discovers that `Gnr.Region` already covers the same regional partition, in which case convert to an optional FK to `Region`. This is a deliberate simplification, not an oversight — a marketer's zone here is informational only and nothing downstream joins on it.
- Keep `Firstname`/`Lastname` spelling as in the legacy schema (single `Firstname`, not `FirstName`) to avoid confusion during data migration; data migration will map `Training_Agent.Firstname/Lastname` 1:1.

## 3. Controller: `WebApp/Controllers/Dynamic/Trn/AgentController.cs`

Standard CRUD only (`Save`/`Add`/`Update`/`Delete`/`Edit`/`New`/`List`/`FetchData`/`ExportToExcel`), route `Panel/Trn/[controller]`, `[ControllerInfo("نمایندگان / بازاریاب‌ها", typeof(Agent))]`. No custom business rules.

## 4. Views

- `WebApp/Views/Panel/Trn/Agent/List.cshtml` — `<datatableprofile entity-Type="typeof(Agent)"></datatableprofile>`.
- `WebApp/Views/Panel/Trn/Agent/Edit.cshtml` — simple field form with the standard `savefn` wiring (`save`/`saveandnew`/`saveandclose`).

## 5. Downstream consumer (context only)

- `Course.AgentId` (optional FK, selected on the Course edit page) — see `06-Course.md`.
