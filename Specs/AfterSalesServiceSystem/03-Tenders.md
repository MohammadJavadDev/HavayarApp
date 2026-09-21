# Part 3 — Sale tenders (not EPMS)

> Implemented. Source of truth is HTS `Sale_TenderManagement` + `DoOperation` + `SaleTenderApproveQueueController`.

## Entity `Sale.TenderManagement`

HTS `Sale_TenderManagement`. `HtsId` = HTS `Id`. Organization is **lookup enum** (`TenderOrganizationEnum`: 929 after-sales, 931 compressed air, 1670 oil/gas), not `OrgUnit`. Company is `Party` (legal), not `Customer`.

Children:

- `TenderPart` (`Sale_TenderPart`) including `ProductGroupId` → `TenderProductGroup`
- `TenderAttachment` (`Sale_TenderFileAttachment`) → `FileEntity` + comment
- `TenderComment` (`Sale_TenderComment`) — AfterAdd returns tender to cartable and emails expert/creator
- `TenderTask` (`Sale_Tender_TaskList`) — auto-created on add / status / final-result change
- `TenderProductGroup` (`Sale_TenderProductGroup`)

## Process

`WebApp/Actions/Sale/TenderManagementAction` (no service layer):

- **Add:** status `2324` FollowingUp, `HasPrimitiveConfirm=true`, `HasSecondaryConfirm=false`, `InCartable=true`, `RequestNumber=null`, `ContractPrice = CompressorTechPrice * Rate`, auto task.
- **Update:** FinalResult `876` (BuyFromHavayar) requires at least one part; auto tasks on status/result change; `InCartable` true→false writes `SendForApprove*` and emails `Sale.Tenders.Approver` unless `SkipCartableEmail`.
- **Send for approve:** `SendForApprove` sets `InCartable=false`. Fields lock when out of cartable unless manager.
- **Duplicate company:** email `Sale.Tenders.DuplicateNotify`.
- Compressor-tech columns only when org/role is compressed air (931).

## Approve queue

Not a table and **not a second page**. Filter is **`InCartable == false && HasSecondaryConfirm != true`** (HTS cartable — *not* `InCartable == true`).

- One List (`/panel/sale/tendermanagement/list`); switch to profile `AfterSales_TenderManagement_ApproveQueue`.
- `ApproveQueue()` redirects to `List` so old bookmarks keep working.
- `FetchData` always applies list ACL; when `profileId` is the queue profile it also applies the cartable filter. Queue SQL has the same `WHERE`.
- Queue toolbar: **تایید** = `SecondaryConfirm` only (HTS has PrimaryConfirm commented out).
- `PrimaryConfirm` still exists as an API for ExtraOperations / ManagerApprove / Approver / Manager / admin; toggles primitive, returns to cartable, skips cartable email.
- `SecondaryConfirm` — ManagerApprove / Manager / admin; HTS toggle + creator email.

## Permission catalog flags (151–154)

GET `TendersExtraOperations` / `TendersCompressedAir` / `TendersOilGas` / `TendersManagerApprove` stay as **catalog flags** (`Ok()`). Behavior is in `FetchData` / `PrimaryConfirm` / `SecondaryConfirm` via `HasTenderAccess(...)` on those paths:

| Path | List filter | Confirm |
|---|---|---|
| ExtraOperations / ShowAll / Manager | all (still `ParentId` null) | ExtraOperations: primary |
| CompressedAir | `Organization == 931` | — |
| OilGas | `Organization == 1670` | — |
| ManagerApprove | all | primary + secondary |

Else: `CreatedById` OR `SaleExpertId`. Always `ParentId` null on list.

Roles: `Sale.Tenders.Approver` (500010), `DuplicateNotify` (500011), `Manager` (500012), `ShowAll` (500013), `CompressedAir` (500014), `OilGas` (500015).

## Menu / profiles

- `/panel/sale/tendermanagement/list` — one `<datatableprofile>` + `fetch-data-path` FetchData (ACL)
- Profiles (same entity, dropdown switch):
  - `AfterSales_TenderManagement_List` — full list, `ParentId` null via FetchData
  - `AfterSales_TenderManagement_ApproveQueue` — cartable filter + **تایید** button
- No menu leaf for «بررسی و تایید مناقصات»
- Row color: first data cell (`td.eq(1)` = شماره مرجع) red / yellow / green from `HasPrimitiveConfirm` / `HasSecondaryConfirm` via `EventScriptsJson.onRowAdded`

### Grid columns (HTS order)

شماره مرجع (`Id`), واحد سازمانی، عنوان مناقصه، نوع مناقصه، شرکت، صنعت مشتری (`Customer.Industry` → `PartyIndustry.Title`)، آدرس/تلفن شرکت (`Party`)، نمایندگی فروش، نوع استعلام، کارشناس فروش، نتیجه نهایی، آخرین وضعیت، **تایید نهایی** (`HasSecondaryConfirm`)، علت راکدی، شماره قرارداد، کشور/استان نصب، نماینده/تلفن کارفرما، مبالغ و واحد پیش‌فاکتور ریالی/ارزی، تاریخ پیش‌فاکتور، شماره پیش‌فاکتور (`IndicatorId`)، ارزش پروژه، کاربر نهایی، توضیحات، تاریخ ارسال به تایید، قیمت هوایار / تمام‌شده / واحد / رقیب / یورو / شرکت رقیب، نوع/وضعیت/شماره/سررسید ضمانت، ایجادکننده، تاریخ ایجاد، آخرین بروزرسانی.

Hidden in SELECT (not deleted): `InCartable`, `HasPrimitiveConfirm` (for `onRowAdded`), بودجه‌ای/دارای بودجه/زیرمجموعه، مناقصه تجدیدی، تکنیک کمپرسور، `RequestNumber`.

### Edit form

- «شماره سفارش» = `Id` (readonly). HTS `ReferenceNumber = Id`. `RequestNumber` is hidden, not shown.
- Process flags (کارتابل / تاییدها / بودجه / هوایار / تجدیدی) are hidden inputs, not checkboxes.
- Compressor-tech section stays in DOM; shown when Organization = 931.
- Competitor prices open in `$.confirm` (rtl) when FinalResult is 880–884 or 1116.
- Products + tracking history load via `page.getPartialView` of `TenderPart/ListByParentId` and `TenderTask/ListByParentId` after the tender has an `Id`.
- Toolbar: ذخیره / ارسال به تایید / پیوست / یادداشت (no duplicate اقلام/تسک‌ها).

## Sync

`SyncAfterSalesPhase3FromTotalSystem.sql`. Company: `Gnr_ManCompany.Hamkaran_ManCompany_FK` (else `ManCompany_ID`) → `Gnr.Party.HamkaranId`, fallback `SLS.Customer.HamkaranId` → `PartyId`. Expert via `Gnr_User.Personel_FK`. Region via `RahkaranId` / name. **Not** `Customer.HtsId = CompanyId`.

TMS row parity (2026-09-15): Tender 80/80 (all with CompanyId), ProductGroup 62, Part 9, Comment 2, Task 131, Attachment metadata 2. ParentId none in TMS.

Migration: `20260915194236_TenderManagementHtsParity`.

## Conscious omissions

- Attachment **file blobs** are not copied (title + comment + HtsId only). New uploads use `FileEntity`.
- `IndicatorId` and `SaleProjectReportId` are stored as **longs**, not entity selectors (HTS FKs to Indicator / SaleProjectReport).
- `PreInvoiceMiladiDate` exists on the entity for Shamsi/Miladi pair convention; HTS only has text `PreInvoiceDate` → synced to `PreInvoiceShamsiDate` only. Miladi stays unused unless a user edits the Shamsi picker.
- Status dropdown still lists legacy 904/905 (HTS hid them on create). Live ids 2308–2324 / 2565 are preferred.
- Grid does **not** show HTS `ProductionOrderNumber`, `ContractDate`, or `Periority` — those come from `Sale_ProjectReportManagement` / a lookup; Havayar has only `SaleProjectReportId` (long), no valid join.
- Grid **ریز صنعت** (`CompanyIndustryItm`) is omitted: `Customer` stores `IndustryId` → `PartyIndustry` (صنعت مشتری) only; the selected `PartyIndustryItem` on the Edit form is NotMapped and not persisted on `TenderManagement`.
