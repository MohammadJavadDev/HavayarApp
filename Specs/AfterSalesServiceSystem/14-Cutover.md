# Part 14 — Cutover

## Access

Restart WebApp so `[ControllerInfo]` / `[ActionDisplayName]` are discovered. Assign Role checkboxes for every new action (including Custom: AfterSaleAccess, RepairsAccess, Tenders*, ShowPrice, SendingSatisfactionLink).

## Menu

`Data/Scripts/Seed_AfterSalesFull_Menu.sql` updates `system.SystemMenu` name `AfterSalesServiceSystem`. Paths are lowercase `/panel/...`.

## Jobs (HTS → Havayar)

| HTS job | Decision |
|---|---|
| `Import_AfterSalesVch` | Keep on TotalSystem until Inv adapter issues documents; then a Hangfire/Windows job calling the adapter |
| Serial lifetime / guarantee calc | Derived in Data Profile SQL, not a nightly overwrite |
| Survey SMS | Application action `SendingSatisfactionLink`, not a silent DB job |

## Checklist vs HTS

- [ ] Field parity per spec (conscious drops listed)
- [ ] Cartable / approve queues
- [ ] PermissionType extras as Custom actions
- [ ] Sync sample row per table (`HtsId` match)
- [ ] Stimulsoft templates designed in Report Builder UI (not C#)
- [ ] No `HtsDbContext` from Panel
- [ ] No parallel `Sale.Order` / `Customer` / `ProductionOrder`

## Remaining polish (known)

- Many generated Edit forms still show DisplayInfo placeholder copy; DataTable still edits via default binders. Fill EntitySelectors like `ServiceRequest` / `PartPrice` / `PriceConfig`.
- Repair child tabs (parts, man-hours, WBS) not all ported as pages.
- EF migration must be run: `.\add-migration.ps1 -Name "AddAfterSalesServiceSystem"` then `.\update-database.ps1`.
- RoleAccess SQL seed is optional; UI Role editor is the supported path after restart.
