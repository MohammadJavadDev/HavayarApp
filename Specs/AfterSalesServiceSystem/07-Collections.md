# Part 7 — Collections

> Prerequisite: customer map from part 1.

## Entities

| HTS | Havayar |
|---|---|
| `Sale_CollectionClaim` | `Sale.CollectionClaim` |
| `Sale_CollectionClaim_PayFish` | `Sale.CollectionClaimPayFish` |
| `Sale_CollectionClaim_Attachment` | `Sale.CollectionClaimAttachment` |

Accounting voucher ids stay as `HtsHdrVchId` / `HtsVchItemId` (no Acc voucher entity in Havayar). `Dl` maps via `FIN.DL` when Hamkaran/Hts id exists; `DlTitle` is denormalized from HTS.

## Pages

- `/panel/sale/collectionclaim/list`
- Pay fish as child list / Edit

Printable fish = Stimulsoft template `CollectionClaimPayFish` (design in Report Builder UI). On-screen grid = Data Profile on the List page.

## Status (2026-09-15)

CRUD + Data Profiles for CollectionClaim and PayFish. PayFish child button on claim list. PayFish Edit already wires `ViewReportByName/CollectionClaimPayFish` (template must exist in ReportBuilder). HTS CollectionClaim DoOperation is pay-fish CRUD only — no in-app accounting voucher posting. DL ids stay adapter longs.

## Sync

`SyncAfterSalesPhase7FromTotalSystem.sql`.
