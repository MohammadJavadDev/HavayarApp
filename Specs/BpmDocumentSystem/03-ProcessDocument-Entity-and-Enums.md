# Bpm.ProcessDocument — Entity, enums, junctions, status log

> Prerequisite: `00-Overview-and-Conventions.md` (full enum tables, trigger→EntityAction mapping, naming ban). This file is the **schema only**. Pages are `04`–`06`. Numbering/files/email are `07`.

## 1. Purpose

Skeleton of the process-document aggregate, mapped 1:1 from `Bpm_Document` + `Bpm_DocumentDetail` + CSV multi-selects, without UI workflow yet (plan phase 3). After this spec is implemented, a raw List/Edit can show fields; buttons that change status wait for `05`.

Legacy tables: `dbo.Bpm_Document` (1178 rows), `dbo.Bpm_DocumentDetail` (3643 rows), `dbo.Bpm_DocumentStatus` (19 rows).

## 2. Enums (`Entities/App/Bpm/Enums/`)

Create one file per enum. Member **value = HTS Lookup_ID or status Id**. `[Display(Name = "...")]` = Persian from `00` §6.

| File | Values |
|---|---|
| `ProcessDocumentRequestTypeEnum.cs` | 1719–1723 |
| `ProcessDocumentTypeEnum.cs` | 1724–1735, 1793, 1794, 1823, 1824, 1832, 2566, 2995 |
| `ProcessDocumentAccessLevelEnum.cs` | 1737, 1738, 1739 |
| `ProcessDocumentSourceEnum.cs` | 1740–1746, 1915, 2253 |
| `ProcessDocumentPriorityEnum.cs` | 1747, 1748, 1749 |
| `ProcessDocumentProcessSetEnum.cs` | 1750–1763 |
| `ProcessDocumentStatusEnum.cs` | 1, 2, 9–24, 26 (skip missing ids) |

For types/process-sets that have a `LookupCode`, put the code in the **enum member name** when it is a legal identifier (`R`, `CL`, `WT`, `MS`, `TI`, …). `1760` must be named so numbering can read code **`TI`** (not `IT`). `1750` has no code — name it `StrategicManagement = 1750`.

`ProcessDocumentStatusEnum` Displays = `Bpm_DocumentStatus.TitleInText` from `00` §6.7. Do **not** copy the inverted comments from `Hts.Core.Enums.BpmDocumentStatus`.

Optional helper (static, not a DB table): `ProcessDocumentTypeCodes` map enum → string LookupCode for numbering.

## 3. Entity: `Entities/App/Bpm/ProcessDocument.cs`

```csharp
[Display(Name = "سند فرآیندی")]
[Table("ProcessDocument", Schema = "Bpm")]
public class ProcessDocument : BaseEntity
{
    public long HtsId { get; set; }

    public long? ParentId { get; set; }
    public virtual ProcessDocument? Parent { get; set; }

    public short Revision { get; set; }

    [DisplayInfo(null, true, type: SystemType.Select, required: true)]
    public ProcessDocumentRequestTypeEnum RequestType { get; set; }

    [DisplayInfo(null, true, type: SystemType.Select)]
    public ProcessDocumentTypeEnum? DocumentType { get; set; }

    [MaxLength(128)]
    public string DocumentTitle { get; set; } = "";

    [MaxLength(20)]
    public string? DocumentNumber { get; set; }

    [DisplayInfo(null, true, type: SystemType.Select, required: true)]
    public ProcessDocumentAccessLevelEnum AccessLevel { get; set; }

    [DisplayInfo(null, true, type: SystemType.Select, required: true)]
    public ProcessDocumentSourceEnum Source { get; set; }

    [DisplayInfo(null, true, type: SystemType.Select, required: true)]
    public ProcessDocumentPriorityEnum Priority { get; set; }

    [DisplayInfo(null, true, type: SystemType.Select)]
    public ProcessDocumentProcessSetEnum? ProcessSet { get; set; }

    [MaxLength(128)]
    public string? SubProcessTitle { get; set; }

    public long? ProcessOwnerId { get; set; }
    public virtual User? ProcessOwner { get; set; }

    public long? ApproverId { get; set; }
    public virtual User? Approver { get; set; }

    public long? FinalApproverId { get; set; }
    public virtual User? FinalApprover { get; set; }

    public DateTime? CompletionForecastMiladiDate { get; set; }
    public string? CompletionForecastShamsiDate { get; set; }

    public DateTime? NotificationMiladiDate { get; set; }
    public string? NotificationShamsiDate { get; set; }

    public ProcessDocumentStatusEnum? LastStatus { get; set; }
    public long? LastStatusUserId { get; set; }
    public virtual User? LastStatusUser { get; set; }

    public long? MainFileId { get; set; }
    public virtual FileEntity? MainFile { get; set; }
    public long? TempFileId { get; set; }
    public virtual FileEntity? TempFile { get; set; }
    public long? FinalFileId { get; set; }
    public virtual FileEntity? FinalFile { get; set; }
    public long? FinalPdfFileId { get; set; }
    public virtual FileEntity? FinalPdfFile { get; set; }
    public long? CommentFileId { get; set; }
    public virtual FileEntity? CommentFile { get; set; }

    [MaxLength(1024)]
    public string? RelatedDocumentsText { get; set; }

    public bool IsDeprecate { get; set; }

    public long RequesterUserId { get; set; }
    public virtual User? RequesterUser { get; set; }

    public long CreatedOrganizationUnitId { get; set; }
    public virtual OrgUnit? CreatedOrganizationUnit { get; set; }

    [MaxLength(2048)]
    public string? Comment { get; set; }

    // Junctions
    public virtual ICollection<ProcessDocumentStatusLog> StatusLogs { get; set; } = new List<ProcessDocumentStatusLog>();
    public virtual ICollection<ProcessDocumentEditingOrgUnit> EditingOrgUnits { get; set; } = new List<ProcessDocumentEditingOrgUnit>();
    public virtual ICollection<ProcessDocumentExecuterOrgUnit> ExecuterOrgUnits { get; set; } = new List<ProcessDocumentExecuterOrgUnit>();
    public virtual ICollection<ProcessDocumentProcessExecuter> ProcessExecuters { get; set; } = new List<ProcessDocumentProcessExecuter>();
    public virtual ICollection<ProcessDocumentBeneficiaryOrgUnit> BeneficiaryOrgUnits { get; set; } = new List<ProcessDocumentBeneficiaryOrgUnit>();
    public virtual ICollection<ProcessDocumentNotificationRecipient> NotificationRecipients { get; set; } = new List<ProcessDocumentNotificationRecipient>();
    public virtual ICollection<ProcessDocumentRelatedOrgUnit> RelatedOrgUnits { get; set; } = new List<ProcessDocumentRelatedOrgUnit>();
}
```

Put `[DisplayName]` / `[DisplayInfo]` Persian labels on every public field that appears on a grid or form (see `04`/`05` field lists). The sketch above omits attributes for brevity; the real class must include them.

### Field map vs HTS `Bpm_Document`

| HTS | Havayar | Notes |
|---|---|---|
| `Id` int | `HtsId` | New identity `Id` |
| `ParentId` | `ParentId` | Required when RequestType is 1720 or 1721 |
| `Revision` smallint? default 0 | `Revision` | Maintained by EntityAction (`00` §8), not SQL |
| `RequestTypeId` | `RequestType` enum | 229 |
| `DocumentTypeId` | `DocumentType` enum | 230; null allowed (1722/1723) |
| `DocumentTitle` nvarchar(128) required | `DocumentTitle` | |
| `DocumentNumber` nvarchar(20) | `DocumentNumber` | Generated in `07` |
| `PermissionTypeId` | `AccessLevel` | 231 |
| `SourceId` | `Source` | 232 |
| `PriorityId` | `Priority` | 233 |
| `ProcessId` | `ProcessSet` | 234 |
| `SubProcessTitle` | `SubProcessTitle` | Not on current HTS request/manage property grids; **keep for sync** |
| `ProcessOwnerId` / `ApproverId` / `FinalApproverId` | FKs to `system.User` | HTS `short` user ids |
| `EditingOrganizationUnitIds` + `InText` | `ProcessDocumentEditingOrgUnit` | |
| `ExecuterOrganizationUnitIds` + `InText` | `ProcessDocumentExecuterOrgUnit` | Published-list «محدود» uses this |
| `ProcessExecuterIds` + `InText` | `ProcessDocumentProcessExecuter` | Users (HTS combo is people, despite “Ids”) |
| `BeneficiaryIds` + `InText` | `ProcessDocumentBeneficiaryOrgUnit` | Used in ابلاغ email as **org unit** ids; **not** on current manage property grid — keep + show on manage (`05`) so notify still has recipients |
| `NotificationRecipientIds` + `InText` | `ProcessDocumentNotificationRecipient` | Users; محرمانه visibility |
| `RelatedUnitsRef` + `RelatedUnitsNames` | `ProcessDocumentRelatedOrgUnit` | Manage UI `cmbRelatedUnits` |
| `CompletionForecastDate` + `InText` | Shamsi/Miladi pair | |
| `NotificationDate` + `InText` | Shamsi/Miladi pair | Required when force-notify is checked |
| `LastStatusId` + `LastStatusUserId` | `LastStatus` + `LastStatusUserId` | Synced from latest StatusLog (max Id) |
| `MainFile*` / `TempFile*` / `FinalFile*` / `FinalPdfFile*` / `CommentFile*` | five `FileId` FKs | |
| `RelatedDocumentsText` | `RelatedDocumentsText` | |
| `IsDeprecate` | `IsDeprecate` | Set on notify of delete/review (`07`) |
| `RequesterUserId` | `RequesterUserId` | |
| `CreatedOrganizationUnitId` | `CreatedOrganizationUnitId` | Set on Add from current user’s unit |
| `Created*` / `Updated*` | `BaseEntity` | |
| `Comment` | `Comment` | Request description (required on request form) |
| nav `Qa_CorrectiveActionActions` | **omit** | Out of scope |

Do **not** keep `*InText` CSV mirrors as source of truth. A read-only display string on the DTO/grid may concatenate junction titles.

## 4. Status log: `Entities/App/Bpm/ProcessDocumentStatusLog.cs`

```csharp
[Display(Name = "گردش سند فرآیندی")]
[Table("ProcessDocumentStatusLog", Schema = "Bpm")]
public class ProcessDocumentStatusLog : BaseEntity
{
    public long HtsId { get; set; }

    public long ProcessDocumentId { get; set; }
    public virtual ProcessDocument? ProcessDocument { get; set; }

    [DisplayInfo(null, true, type: SystemType.Select, required: true)]
    public ProcessDocumentStatusEnum Status { get; set; }

    public DateTime? SendMiladiDate { get; set; }
    public string? SendShamsiDate { get; set; }

    public DateTime? ReceiveMiladiDate { get; set; }
    public string? ReceiveShamsiDate { get; set; }

    [MaxLength(2048)]
    public string? Comment { get; set; }
}
```

| HTS `Bpm_DocumentDetail` | Havayar |
|---|---|
| `Id` | `HtsId` |
| `DocumentId` | `ProcessDocumentId` |
| `StatusId` | `Status` |
| `SendDate` + `InText` | Shamsi/Miladi |
| `ReceiveDate` + `InText` | Shamsi/Miladi |
| `Comment` | `Comment` |
| `CreatedUserId/Date/InText` | `BaseEntity` (`CreatedById` is the actor of the transition) |

Child grid on request + manage + published: Status (`TitleInText`), Comment, Send, Receive, CreatedBy, CreatedOn.

## 5. Junction tables (all schema `Bpm`)

Each is a `BaseEntity` with FKs only (no extra payload unless noted). Unique index on `(ProcessDocumentId, {other}Id)`.

| Class | Other FK | Replaces HTS |
|---|---|---|
| `ProcessDocumentEditingOrgUnit` | `OrgUnitId` | `EditingOrganizationUnitIds` |
| `ProcessDocumentExecuterOrgUnit` | `OrgUnitId` | `ExecuterOrganizationUnitIds` |
| `ProcessDocumentProcessExecuter` | `UserId` | `ProcessExecuterIds` |
| `ProcessDocumentBeneficiaryOrgUnit` | `OrgUnitId` | `BeneficiaryIds` (treated as unit ids in `SendAnnouncementNotificationEmail`) |
| `ProcessDocumentNotificationRecipient` | `UserId` | `NotificationRecipientIds` |
| `ProcessDocumentRelatedOrgUnit` | `OrgUnitId` | `RelatedUnitsRef` |

HTS announcement code also does `userIds.AddRange(editingOrganizationUnitIds.Split)` and looks those numbers up in `Gnr_User` — i.e. it **treats editing-unit CSV as user ids**. That contradicts the manage UI (org-unit multi-select). **Havayar:** editing junctions are **org units**; email to personnel of those units (`07`). Do not port the user-id mix-up.

## 6. Module setting (needed by `07`, created here)

```csharp
[Display(Name = "تنظیمات اسناد فرآیندی")]
[Table("ProcessDocumentModuleSetting", Schema = "Bpm")]
public class ProcessDocumentModuleSetting : BaseEntity
{
    public int LastAnnouncementNumber { get; set; }  // HTS live 446
    [MaxLength(2000)]
    public string? AnnouncementAlwaysCc { get; set; }          // default mirlohi.m@…
    [MaxLength(2000)]
    public string? AnnouncementConditionalCcJson { get; set; } // sajdeh.n → Yaghyaei.m; momenirad.s → org 268
    [MaxLength(500)]
    public string? ProcessFilesRoot { get; set; }              // UNC
    [MaxLength(500)]
    public string? ProcessFilesTempFolder { get; set; }        // 1_Temp
    [MaxLength(500)]
    public string? ProcessFilesHistoryFolder { get; set; }     // 2_History
}
```

Seed one row. Sync copies `Bpm_LastAnnouncementNumber`.

## 7. Controller / views in **this** phase

A raw `ProcessDocumentController` with standard List/Edit is allowed so fields can be inspected after the EF migration. **Do not** expose workflow buttons yet (`05`). `FetchData` may return everything for admins only until `04`/`05` cartable filters land — or implement the filters early; they must match `04`/`05`/`06` exactly once those pages exist.

Migration: `.\add-migration.ps1 -Name "AddBpmProcessDocument"` then `.\update-database.ps1`. No SQL trigger in the migration.

## 8. EntityAction stubs (filled in `05`/`07`)

`WebApp/Actions/Bpm/ProcessDocumentStatusLogAction.cs`

- AfterAdd/AfterDelete: set parent `LastStatus` / `LastStatusUserId` from the log row with max `Id` (trigger `UpdateBpmDocumentLastStatus`).

`WebApp/Actions/Bpm/ProcessDocumentAction.cs`

- BeforeSave: numbering (`07`); revision rules from `UpdateBpmDocumentTrigger` (`00` §8).
- AfterSave: optional file-move (`07`).

Skip when `Comment == "Created by system owner"` (HTS trigger guard) — only relevant if a sync job uses that sentinel.

## 9. Acceptance criteria

1. Enum numeric values equal live Lookup_IDs / status Ids; 16 = FinalApproverApproved / «تایید، تصویب کننده»; 17 = FinalApproverCommented / «کامنت، تصویب کننده».
2. No `Edms` schema types. Table names `Bpm.ProcessDocument` and `Bpm.ProcessDocumentStatusLog`.
3. Every HTS CSV multi-select has a junction table; the entity has **no** `EditingOrganizationUnitIds` string column.
4. Five file FKs are `FileEntity`, not path+size+name triplets.
5. Dates are Shamsi/Miladi pairs, not `*InText` columns.
6. `HtsId` present on document, status log, and (optionally) unused on junctions.
7. EF migration applies without creating SQL triggers.
8. No `IProcessDocumentService` / `Data/Services/Bpm`.
9. `Qa_CorrectiveAction` FK absent.
