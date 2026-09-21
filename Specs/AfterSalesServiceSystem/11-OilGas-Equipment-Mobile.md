# Part 11 — Oil & gas work report, equipment monitoring, mobile users

## Oil & gas

HTS `OilGas_WorkReport` (menu lives under Sale even though page System_FK may be 36). Havayar: `Sale.OilGasWorkReport`. Project → `Edms.Project`. Manager → `User`.

Page: `/panel/sale/oilgasworkreport/list`.

CNG work reports (`Cng_WorkReport`) are **out** of after-sales except as `HtsCngWorkReportId` on surveys.

## Equipment monitoring

`Sale.EquipmentMonitoring` from `Sale_EquipmentMonitoring`. Values child table can follow as `EquipmentMonitoringValue` if the screen shows a time series; first version is the device header List/Edit.

Page: `/panel/sale/equipmentmonitoring/list`.

## Mobile users

HTS `SaleMobileAppUsersManagement` uses HMA users, **not** a separate TotalSystem after-sales API (API remains out of scope). Havayar: `Sale.AfterSalesMobileUser` (unique `UserId`).

Page: `/panel/sale/aftersalesmobileuser/list`.

## Sync

`SyncAfterSalesPhase11FromTotalSystem.sql`.

## Status (2026-09-15)

OilGasWorkReport / EquipmentMonitoring / AfterSalesMobileUser are CRUD lists. No extra HTS DoOperation found as required beyond save. Mobile after-sale API remains out of scope. Equipment time-series child table not added.
