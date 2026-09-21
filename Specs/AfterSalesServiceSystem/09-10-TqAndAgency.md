# Parts 9–10 — Technical query and agency

> Prerequisite: parts 1, 5. Inventory cardex uses the same adapter as part 6.

## TQ (part 9)

| HTS | Havayar |
|---|---|
| `Sale_TQ` | `Sale.TechnicalQuery` |
| `Sale_TQComments` | `Sale.TechnicalQueryComment` |

Pages: `/panel/sale/technicalquery/list`, `/panel/sale/technicalquery/approvequeue`.

EPMS tender / `RequirmentAdvertise` is **not** this entity.

## Agency (part 10)

| HTS | Havayar |
|---|---|
| `Sale_AgencyPartCardex` | `Sale.AgencyPartCardex` (`HtsInvVoucherId`, no Inv FK) |
| `Sale_AgencyCartable` | `Sale.AgencyCartable` |

Agency customer = `SLS.Customer` (HTS `AgencyId` → `Crm_Customer`).

Pages: `/panel/sale/agencypartcardex/list`, `/panel/sale/agencycartable/list`.

## Sync

`SyncAfterSalesPhase9FromTotalSystem.sql`, `SyncAfterSalesPhase10FromTotalSystem.sql`.

## Status (2026-09-15)

TQ List + ApproveQueue (`FetchApproveQueue` + profile WHERE `CartableStatusId IS NOT NULL`). `Approve` / `Reject` set cartable status 1/2, write a `TechnicalQueryComment`, email the creator. Comment `ListByParentId` from Edit. HTS extra steps (DeployTeam / DetermineActor / FinishOperation) are not ported — those fields are not on the Havayar entity.

Agency cartable `Accept` / `Reject` toggle `IsDone` (HTS has no accept/reject workflow; only `IsDone` + debit calc). Inv voucher ids stay longs.
