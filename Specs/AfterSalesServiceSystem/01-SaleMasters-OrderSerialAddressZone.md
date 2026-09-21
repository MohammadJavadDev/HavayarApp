# Part 1 — Sale masters: remittance/serial gaps, customer address, zone, monitoring

> Prerequisite: `00-Overview-and-Conventions.md`. Do not start Service Request (part 5) before this part is in.

## 1. Purpose

HTS pages that every service request hangs off:

| HTS | Page_ID | Havayar |
|---|---|---|
| حواله های فروش | 132 | existing `OrderDetailController.List` — keep; add gap fields + `HtsId` |
| پیوست محصولات / سریال | 159 / 133 | `OrderDetailSerialController.List` + attachment subpage |
| مانیتورینگ فروش | 151 | `OrderDetailSerialController.Monitoring` |
| سایت های مشتریان | 234 | `CustomerAddress` List/Edit |
| *(no dedicated menu)* `Crm_Zone` | — | `Zone` List/Edit (needed by address, responsible zone, cartable) |

## 2. Conscious drops / already covered

- HTS computed serial columns (`Remain_LifeTime`, `Remain_ProductGuranteeTime`, `Remain_PartGuranteeTime`, `Remain_LaunchDate`, `Remain_SendDate`, `Returned`) — not stored; derive in Data Profile SQL later if a report needs them.
- `Sale_Order.Creator_FK` — `BaseEntity.CreatedById`.
- `Crm_Customer_Address.UpdatedUserId` / `UpdatedDateInText` — `ModifiedBy*`.
- Binary `Attachment_FileContent` — `FileEntity` (+ UNC `AttachmentFilePath` in sync).

## 3. Enums

`Entities/App/SLS/Enums/CustomerGradeEnum.cs` — LookupType 62:

- `239` A, `240` B, `242` C, `243` تعطیل

`Entities/App/Crm/Enums/ZoneTypeEnum.cs` — table `Crm_ZoneType` (not lookup):

- `1` هوای فشرده, `2` CNG, `3` فرآیندی

## 4. New entities

### 4.1 `Crm.Zone` — `Entities/App/Crm/Zone.cs`

HTS `Crm_Zone` (72 rows). Schema `Crm`.

| Field | Type | HTS |
|---|---|---|
| HtsId | long unique filtered | Zone_ID |
| Title | string 400 required | Zone_Title |
| SupervisorUserId | User? | Zone_SupervisorUser_FK |
| ZoneType | ZoneTypeEnum? | ZoneType_FK |

### 4.2 `SLS.CustomerAddress` — `Entities/App/SLS/CustomerAddress.cs`

HTS `Crm_Customer_Address` (13 659 rows).

| Field | Type | HTS |
|---|---|---|
| HtsId | long | Customer_Address_ID |
| CustomerId | Customer required | Customer_FK via map §00.12 |
| Title | string 400 | Address_Title |
| Address | string 4000 | Address |
| ZoneId | Zone? | Zone_FK |
| Grade | CustomerGradeEnum? | Grade_FK |
| ProvinceId | Region? | Province_FK (join `Gnr_Province.Province_Title` → `Gnr.Region.Name`) |
| AgencyPartyId | Party? | AgencyMancompanyId → Party.HamkaranId |
| AgencyDlId | DL? | AgencyDlId → FIN.DL.HamkaranId |
| HamkaranAddId | int | Hamkaran_Add_FK |
| RahkaranId / RahkaranVersion | long? | RahkaranId / RahkaranVersion |

### 4.3 `Sale.OrderDetailSerialAttachment`

HTS `Sale_OrderDetail_Serial_Attachment`. `FileId` + Comment. Subpage from serial Edit.

## 5. Gap fields on existing entities

### `Sale.Order`

- `HtsId` ← `Order_ID`
- `ConfirmerUserId` ← `Confirmer_FK`
- `OperatorUserId` ← `Operator_FK`
- `HtsStockId` ← `Stock_FK` (no Inv warehouse entity yet — long, no FK)
- `HtsBaseVoucherTypeId` ← `BaseVchType_FK`
- `HtsVchNo` ← `VchNo` (keep `OrderNumber` as the business number already used)
- `HtsContractVchHeaderId` ← `ContractVchHeaderId`

### `Sale.OrderDetail`

- `HtsId` ← `OrderDetail_ID`
- `IsPackage`, `NotComplete`
- `HamkaranInvVchItmId` ← `Hamkaran_InvVchitm_FK`

### `Sale.OrderDetailSerial`

- `HtsId` ← `OrderDetail_Serial_ID`
- `LifeTime`
- `InvComment`
- `IndustrialConfirm`
- `FinalExitMiladiDate` / `FinalExitShamsiDate` / `ExitTimeFinal`
- **Retype** `CustomerAddress` from `Customer` to `SLS.CustomerAddress`

### `SLS.Customer`

- `HtsId` ← `Crm_Customer.Customer_ID` (additive; mapping still prefers Party/ManCompany)

## 6. Controllers / views / permissions

| Controller | Route | Actions beyond CRUD |
|---|---|---|
| `ZoneController` | `Panel/Crm/Zone` | standard |
| `CustomerAddressController` | `Panel/SLS/CustomerAddress` | standard |
| `OrderDetailSerialController` (extend) | `Panel/Sale/OrderDetailSerial` | `List`, `Monitoring`, `IndustrialConfirm`, `MonitoringExitAccess`, `MonitoringInventoryAccess`, `ShowPrice` (Custom) |
| `OrderDetailSerialAttachmentController` | `Panel/Sale/OrderDetailSerialAttachment` | `ListByParentId` |
| `OrderDetailController` | already exists | keep ShowPrice via existing Data Profiles |

Monitoring exit/inventory: dedicated Custom actions so Role UI can tick PermissionType 188/189. `IndustrialConfirm` POST toggles `IndustrialConfirm`. `DoOperation` on HTS public page is covered by standard Save on serial Edit.

## 7. Menu leaves (append under AfterSalesServiceSystem)

- فروش → حواله‌های فروش `/panel/sale/orderdetail/list`
- خدمات پس از فروش → مانیتورینگ فروش `/panel/sale/orderdetailserial/monitoring`
- خدمات پس از فروش → پیوست سریال `/panel/sale/orderdetailserial/list`
- خدمات پس از فروش → سایت‌های مشتریان `/panel/sls/customeraddress/list`
- خدمات پس از فروش → مناطق (زون) `/panel/crm/zone/list` (master, even though HTS had no menu row)

## 8. Sync

`Data/Scripts/SyncAfterSalesPhase1FromTotalSystem.sql`

Order: Zone → Customer.HtsId → CustomerAddress → Order.HtsId → OrderDetail.HtsId → OrderDetailSerial.HtsId → attachments (FileEntity UNC only).

Idempotent `MERGE` on `HtsId`. Skip rows whose Customer/Part/User map is missing (log `#Unmapped`).
