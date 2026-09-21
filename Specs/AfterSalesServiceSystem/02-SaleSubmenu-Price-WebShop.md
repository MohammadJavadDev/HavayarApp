# Part 2 — Sale submenu: price config, approved price, web shop

> Prerequisite: `00`, `01`. Public web shop storefront is **out of scope**.

## Entities

| HTS | Havayar | Notes |
|---|---|---|
| `Sale_PriceConfig` | `Sale.PriceConfig` | Profit/Overhead/Shipping/Gomrok percents |
| `Sale_PartPrice` | `Sale.PartPrice` | Part + Price + three SaleFactor decimals |
| `WebShop_Order` | `Sale.WebShopOrder` | Admin list of internet orders; `Order_FK` → `Sale.Order.HtsId` when mapped |
| `Sale_WebShop_PartForSale` | `Sale.WebShopPartForSale` | Part + `WebShopPartTypeEnum` (lookup 67) + Price |
| `Sale_WebShop_PartGroup` | `Sale.WebShopPartGroup` | Prefix + title (needed by admin grouping) |

Skip `Sale_WebShop_PartForSale_old`, `WebshopTest`. Service kit / manual item tables only if the admin form shows them — include as child ListByParentId if HTS edit screen has them; otherwise document as later.

## Enum

`WebShopPartTypeEnum` lookup 67: 277 روغن, 278 برد, 279 المنت فیلتر, 280 فیلتر هوا, 281 فیلتر سپراتور, 282 فیلتر روغن, 283 کیت.

## Pages / menu

- `/panel/sale/priceconfig/list`
- `/panel/sale/partprice/list`
- `/panel/sale/webshoporder/list`
- `/panel/sale/webshoppartforsale/list`

Permissions: standard CRUD. ShowPrice (8) on PartPrice Fetch/List if the HTS page had it.

## Sync

`Data/Scripts/SyncAfterSalesPhase2FromTotalSystem.sql` — MERGE on HtsId. Part via `Inv.Part.HtsId`.

## Implemented (2026-09-15)

Field/CRUD parity: PriceConfig (title + four percents), PartPrice (part + price + three factors), WebShopPartForSale (part + type enum 67 + price), WebShopPartGroup (prefix + title), WebShopOrder admin list (order code, customer code, prices, factor, confirmer). HTS PriceConfig/PartPrice `DoOperation` is generic EntityService — no extra process. Public storefront out of scope.

Conscious omissions: WebShop `Confirm_Date`/`Confirm_Time`/`FactoredTime` fold into `BaseEntity` audit + `FactoredShamsiDate`. `ShowPrice` was not a separate HTS permission on Sale_PartPrice (page-level Sale only). Inventory voucher gap not touched.
