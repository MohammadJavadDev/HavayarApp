# Part 8 — Repairs (`Rpr`)

> Prerequisite: parts 1, 5. Schema **`Rpr`**.

## Enum

`RepairRequestStatusEnum` = `Gnr_Lookup` type **71** (keep Lookup_ID): 301, 302, 304, 305, 1535, 1536, 1537, 1774, 1871, 2092, 2093, 2236, 2413, 3169, 3175, 3176.

## Entities

| HTS | Havayar |
|---|---|
| `Rpr_RepairRequest` | `Rpr.RepairRequest` + `ServiceRequestId` FK (replaces CSV) |
| `Rpr_EstimatedCost` | `Rpr.EstimatedCost` |
| `Rpr_ContractorOrder` | `Rpr.ContractorOrder` |

Child part/man-hour/comment/attachment tables: add as ListByParentId when a form tab exists; otherwise sync later and list in cutover as remaining HTS tabs.

Computed HTS columns (`IsConfirmed`, durations) are **not** stored — derive in Data Profile SQL if a report needs them.

## Permissions (separate Custom actions)

| HTS PermissionType | Action |
|---|---|
| AfterSaleAccess = 70 | `RepairRequestController.AfterSaleAccess` |
| RepairsAccess = 71 | `RepairRequestController.RepairsAccess` |

## Pages

- `/panel/rpr/repairrequest/list`
- `/panel/rpr/estimatedcost/list`
- `/panel/rpr/contractororder/list`
- `/panel/rpr/repairrequest/delaylist` — tabular Data Profile
- `/panel/rpr/repairrequest/delaychart` — ApexCharts (`DelayChart` + `DelayChartData` in controller, no new service)

Print/label = Stimulsoft `RepairRequestLabel`.

## Sync

`SyncAfterSalesPhase8FromTotalSystem.sql`.

## Status (2026-09-15)

RepairRequest CRUD + children ListByParentId (parts/manhour/comment/attachment/contractor). `Confirm` sets `ConfirmerId` + status Completed; `SendToContractor` sets `HasContractor` + status SentToContractor. `RepairRequestLabel` is already wired via `ViewReportByName`. `AfterSaleAccess` / `RepairsAccess` remain catalog flags. HTS multi-step PreCheck/FinancialProposal date confirms and ~80 extra header columns are not on the entity.
