# Part 6 — Service request operations (mission, work report, parts)

> Prerequisite: part 5.

## Entities

| HTS | Havayar | Notes |
|---|---|---|
| `Sale_Mission` | `Sale.Mission` | Expert = `User`; `HtsPersonelId` until Personel.HtsId exists |
| `Sale_Personel_MissionSalary` | `Sale.MissionSalary` | Same user map |
| `Sale_Report` | `Sale.WorkReport` | Result lookup 40 → `MissionResultEnum` |
| `Sale_Report_Attachment` | `Sale.WorkReportAttachment` | |
| `Sale_ServiceRequest_Part` | `Sale.ServiceRequestPart` | `AfterSalesServiceTypeEnum` 148–151 |

## Inventory adapter

Havayar has no Inv voucher documents yet. `ServiceRequestPart` stores `HtsProjectVchItemId` / `HtsProjectVchPartId` / voucher number-year. Panel controllers must **not** run raw TotalSystem SQL. Capture-only helper: `WebApp/Actions/Sale/AfterSalesInventoryAdapter.cs`. When Inv documents land, replace the adapter body; forms stay the same.

## Pages

- `/panel/sale/mission/list`
- `/panel/sale/missionsalary/list` (حق ماموریت)
- `/panel/sale/workreport/list`
- `/panel/sale/servicerequestpart/list`

Deliver-to-customer (HTS Inv delivery) is adapter-only until Inv exists — documented, not a parallel Inv module.

## Sync

`SyncAfterSalesPhase6FromTotalSystem.sql`.

## Implemented (2026-09-15)

Mission `ListByParentId` from ServiceRequest. WorkReport and ServiceRequestPart are parent-scoped via Detail/Mission → ServiceRequest. `MissionAction.BeforeAdd` assigns next `HokmNumber` if empty. Mission / MissionSalary / WorkReport / ServiceRequestPart have CRUD + Data Profiles. Inventory voucher fields stay adapter longs (no TotalSystem SQL in controllers).

## Remaining

- HTS `Have_Gurantee` is a form checkbox (no auto-rule). `MissionAction` only assigns the next `HokmNumber` when the user leaves it 0.
- Work report attachments ListByParentId.
- Mission fee auto-calc (`GetMissionInfo`) not ported.
