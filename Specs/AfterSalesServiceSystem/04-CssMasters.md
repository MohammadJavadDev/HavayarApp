# Part 4 — After-sales master data

> Prerequisite: `00`, `01` (zone/customer/serial). Required before part 5.

## Entities

| HTS | Havayar | Notes |
|---|---|---|
| `Sale_OrderPoint` | `Sale.OrderPoint` | `HtsPartCardexId` **no FK** (Inv cardex not in Havayar). `AlternativePartId` → Part. Comments as `OrderPointComment` |
| `Sale_Customer_AllowedGuarantee` | `Sale.CustomerAllowedGuarantee` | Customer + production order number + two amounts |
| `Sale_ResponsibleZone` | `Sale.ResponsibleZone` | User + Province(Region) + Zone + comment |
| `Sale_ResponsibleZoneCustomer` | `Sale.ResponsibleZoneCustomer` | junction |
| `Sale_ResponsibleZoneReceipt` | `Sale.ResponsibleZoneReceipt` | User + from/to dates + amount |
| `Sale_ProductActivity_Itm` | `Sale.ProductActivityItem` | activity type 55, executor 56, part, duration, mount, cost, org unit |
| `Sale_OrderDetail_Product` | `Sale.OrderDetailProduct` | parent OrderDetail + product OrderDetail + Serial + Mount |
| `Sale_RequestType` | enum `ServiceRequestTypeEnum` ids 1–13 | closed set |
| `Sale_ActionType` | enum `MissionActionTypeEnum` 1–2 | |
| `Sale_ProjectUtilizedMaterial` | existing entity | add `HtsId`, `VchItemId`, `VchHdrId`, `VchTypeId`, `ReplacedPartId`, `ReplacedQty`, `Comment` + List/Edit |

**Out:** `Sale_ProductActivity` header and `Sale_AllowedGuarantee` (commented menu).

## Pages / menu

Paths in `00` §6. OrderPoint: Inv adapter note — cardex id is stored until Inv documents exist.

## Sync

`SyncAfterSalesPhase4FromTotalSystem.sql`.

## Implemented (2026-09-15)

CRUD + Data Profiles exist for OrderPoint, CustomerAllowedGuarantee, ResponsibleZone, ResponsibleZoneReceipt, ProductActivityItem, OrderDetailProduct, ProjectUtilizedMaterial. Enums `ServiceRequestTypeEnum` / `MissionActionTypeEnum` match HTS closed sets. HTS pages are master-data CRUD (no cartable).

`OrderPointComment` and `ResponsibleZoneCustomer` have List/Edit/`ListByParentId` and parent Edit buttons (HTS comment subgrid / customer grid).

## Remaining

- Inv cardex id on OrderPoint stays a long (`HtsPartCardexId`) until Inv documents exist.
- HTS OrderPoint is a part-centric operational screen (`VwSaleOrderPoint`, ignore+comment ribbon); Havayar keeps row CRUD plus comment history.
- ResponsibleZone save does not auto-merge duplicate (Responsible, Province, Zone) the way HTS DoOperation does.
