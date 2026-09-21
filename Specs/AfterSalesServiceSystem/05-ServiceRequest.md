# Part 5 — Service Request core

> Prerequisite: parts `00`–`04`. Do **not** restore the deleted HtsDbContext Panel port.

## Entities

| HTS | Havayar |
|---|---|
| `Sale_ServiceRequest` | `Sale.ServiceRequest` (`HtsId`) |
| `Sale_ServiceRequest_Detail` | `Sale.ServiceRequestDetail` |
| `Sale_ServiceRequest_ExpertMission` | `Sale.ServiceRequestExpertMission` |
| `Sale_ServiceRequest_Attachment` | `Sale.ServiceRequestAttachment` (`FileEntity`) |
| `Sale_ServiceRequest_SalesResponsible` | `Sale.ServiceRequestSalesResponsible` |

`RepairRequestsIds` CSV is stored as `LegacyRepairRequestIds` for sync only. Real link is `Rpr.RepairRequest.ServiceRequestId` (part 8).

## Pages / permissions

| Path | Action |
|---|---|
| `/panel/sale/servicerequest/list` | مدیریت درخواست پشتیبانی |
| `/panel/sale/servicerequest/cartable` | کارتابل مسئول منطقه |
| `/panel/sale/servicerequest/dispatch` | اعزام کارشناس |
| `/panel/sale/servicerequest/customerrequests` | درخواست مشتریان |
| `/panel/sale/servicerequest/printsheet` | چاپ → Stimulsoft `ServiceRequestSheet` via `appController.addPage(ViewReportByName/...)` |

Child grids (details / dispatch / attachments) open with `appController.addPage` like `ProductionOrderItem`.

## Sync

`Data/Scripts/SyncAfterSalesPhase5FromTotalSystem.sql` — MERGE header/detail/expert/attachment on `HtsId`. Customer via `SLS.Customer.HtsId`. Zone via `Crm.Zone.HtsId`. Address via `SLS.CustomerAddress.HtsId`. User via `#UserMap`. OrgUnit via `Hrm.OrgUnit.HamkaranUnitId` / HTS org map. Attachment binaries → `FileEntity` + UNC path (same as serial attachments in phase 1).

## Conscious drops

HTS `CreatedUser_FK` / `CreatedDate` / `CreatedTime` → `BaseEntity` audit. `DispachUpdatedDate*` not on form (audit). Print layout is Stimulsoft, not DevExpress/Kendo.

## Implemented (2026-09-15)

- `ServiceRequestAction` fills `CustomerPhoneFromHistory` from `Customer.Party` phone/mobile and rebuilds `AllProductName` from details.
- `ServiceRequestDetailAction` refreshes product names after child save.
- `SendDispatch` sets `IsWaitingToSend` and emails zone `SupervisorUserId` (HTS emailed zone personnel; hardcoded Personel 15330 waiting-slot is not ported).
- `ServiceRequestExpertMissionAction` emails zone supervisor on expert add.
- Children open `ListByParentId` (detail / expert / attachment / mission / work report / parts).
- Cartable Data Profile `AfterSales_ServiceRequest_Cartable` filters ResponsibleZone / zone supervisor (`@CurrentUserId`).
- Print: Stimulsoft `ServiceRequestSheet` via `ViewReportByName` (template must exist in ReportBuilder).

## Remaining

- Related-person after-sale agent upsert (`Crm_Customer_RelatedPerson`) not ported — Havayar has no `CustomerRelatedPerson` table to upsert into.
- Inline product grid on header (HTS posted details in DoOperation) — children are separate pages.
- `RepairRequestsInText` display of linked Rpr rows.
