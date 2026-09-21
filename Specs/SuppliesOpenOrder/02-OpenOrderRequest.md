# 02 — درخواست‌های باز (عملیات)

HTS: `SystemPage.OpenOrderRequest_Manage = 74` ، مسیر منو `supplayers-hamkaranOpenRequests` ، ویو `_OpenOrderRequestManagement.cshtml`.  
Havayar: `OpenOrderRequestController` ، List=`/panel/sup/openorderrequest/list` ، عملیات روی **Edit** (نه روی گرید چندردیفی مثل Kendo HTS).

---

## 1. فرآیند HTS (چرخه حیات)

```
Rahkaran PRC3 (درخواست/سفارش خرید)
    → Import_AllOpenOrderRequest (هر ۱۵ دقیقه داخل BaseHamkaranTask، ساعت ۶–۲۱)
    → Sup_Compute_OpenOrderRequest (اختتام / حذف نرم / احیا / Delay)
    → ChangeOpenOrderRequestStatusAndSendNotification (تایید خودکار مهندسی اگر مدرک اصلی کالا 264/265/271/308 یا NotNeedToAttachDocuments)
    → کارتابل مهندسی (Engineering_Accept)
    → کارتابل فروش/پروژه (HasSalesUnitConfirmation + VPIS لینک‌شده)
    → عملیات تامین/صنایع: کامنت، پیوست، وضعیت «در راه»/«استعلام»، توقف چندمرحله‌ای، خاتمه
    → اختتام وقتی OrderItemState=7 و آخرین کامنت stop نیست
    → حذف نرم وقتی در Rahkaran بسته شد یا کاربر Terminate کرد (IsForceDeletedByUser)
```

مهلت پاسخ توقف: **۲ روز** اگر `IsRoutineRequest`، وگرنه **۳ روز**.  
تایم‌اوت عامل توقف: جاب روزانه ۹:۳۵ اگر >۱۴ روز بدون گام بعدی → `StopStatusId=2821`.

### نقش سازمانی در دید گرید HTS
| فلگ | قاعده |
|---|---|
| صنایع | OrgUnit = 12 |
| مهندسی | `Accept_Engineering` یا Org ∈ {17,20,143,30} |
| تامین | Org ∈ {51,63} |
| فروش/پروژه | `SalesOrProjectPermission` |

Admin همه را می‌بیند. فروش/پروژه/تامین فقط ردیف‌های `Engineering_Accept=true` و حذف‌نشده. بقیه معمولاً باید در `Requested_Personel` باشند.

---

## 2. فیلدهای `Sup_OpenOrderRequest` ↔ `Sup.OpenOrderRequest`

| HTS | Havayar | یادداشت |
|---|---|---|
| OpenOrderRequest_ID | Id | |
| OrderRowId | OrderRowId | |
| Year | Year | |
| OrderNo | OrderNo | |
| OrderDate / OrderDateInEurope | OrderShamsiDate / OrderMiladiDate | |
| Acc_DL_FK | DlId | |
| Inv_Part_FK | PartId | اجباری در جدید |
| NeedDate | NeedDateShamsiDate / NeedDateMiladiDate | |
| OrderConfirmDate | OrderConfirmShamsiDate / OrderConfirmMiladiDate | |
| SumSendQty | SumSendQty | decimal→long? |
| FactoredCount | FactoredCount | decimal→int |
| OrderQty | OrderQty | |
| RequiredQty | RequiredQty | |
| OrderItmComment | OrderItemComment | |
| IsStop / Stop_Date | IsStop / StopShamsi+Miladi | |
| IsDeleted / IsDeletedDate | IsDeleted / Shamsi+Miladi | |
| IsForceDeletedByUser | IsForceDeletedByUser | |
| Requested_Personel_RegDate | RequestedPersonelRegShamsi/Miladi | |
| Requested_Personel / Email | همان + `RequestedPersonelIds` | جدول فرزند در جدید نیست |
| OpenOrder_Status | Status | |
| Changed | Changed | |
| Notify_Email_Requested_Personel | NotifyEmailRequestedPersonel | |
| Notify_Email_Send_Paper_Ids | NotifyEmailSendPaperIds | |
| Engineering_Accept + User/Date/Time/Europe | EngineeringAccept + User + ShamsiDateTime + ConfirmationMiladiDateTime | Time جدا ادغام شده |
| IsAcceptedAutomaticallyByEngineering | IsAcceptedAutomaticallyByEngineering | |
| Completion_Date / Completion_Time | CompletionShamsi/MiladiDate | **Time جدا نیست** |
| DelaysBuyDay | DelaysBuyDay | |
| ProductionOrderNumber / ProductionOrderId | همان | |
| IsRejectedByInspection | IsRejectedByInspection | |
| FactoredDate / InText | FactoredMiladi/Shamsi | |
| Comment | Comment | |
| PurchaseRequestItemId / Number / Date | همان جفت شمسی/میلادی | |
| HasSalesUnitConfirmation + user/date/comment | همان | |
| SalesUnitSalesExpert/Manager/ProjectManager | همان User FK | |
| RelatedPartId | RelatedPartId | |
| Delivery/Temporary/Qc/Final voucher dates | جفت Shamsi/Miladi | |
| SupplyDate | SupplyMiladi/Shamsi | |
| HasSalesUnitPrimitiveApprove | HasSalesUnitPrimitiveApprove | |
| IsRoutineRequest | IsRoutineRequest | پیش‌فرض false در جدید (HTS nullable) |
| StopStatusId / StopCheckingStatusId | StopStatus / StopCheckingStatus enums | |
| Requested_EngineeringPersonel / Email | + Ids | |
| ManCompanyId | ManCompanyId → Supplier | |

### جداول فرزند HTS

| جدول | Havayar |
|---|---|
| Comment | `OpenOrderRequestComment` — تقریباً کامل |
| Attachment | `OpenOrderRequestAttachment` + FileEntity + HtsId |
| Vpis | `OpenOrderRequestVpis` |
| Requested_Personel (2829/2830) | **ندارد** — دنرمال روی والد |
| EmailToSupplier | **ندارد** |
| Event | **ندارد** (اعلان + NotifyEmailSendPaperIds) |

---

## 3. اکشن‌های صفحه Manage

### HTS (ریبون گرید)

| دکمه | PermissionType | رفتار |
|---|---|---|
| ویرایش/کامنت | Edit روی 74 | بدون Requested_Personel ذخیره نمی‌شود |
| پیوست | صفحه 71 | Changed=true + ایمیل |
| لینک VPIS | HasEngineeringPermission=82 | |
| تایید مهندسی | Accept_Engineering=12 | LeadTime → SupplyDate |
| تایید فروش/پروژه | 187 + کاربر ∈ Expert/Manager/PM | **نیاز به VPIS لینک‌شده** |
| توقف چندمرحله‌ای | Stop=22 + کارتابل وضعیت | 2807…2821 |
| راه‌اندازی قدیمی | Start=23 | |
| در راه | Sending=24 | Status متنی |
| استعلام | Query=25 | Status متنی |
| خاتمه | Terminate=101 | IsForceDeletedByUser |

### Havayar Edit buttons

| action-name | نقش UI | کنترلر سرور `[ActionDisplayName]` | بررسی نقش سرور |
|---|---|---|---|
| addComment | همه (اگر حذف نشده) | خیر | خیر |
| engineeringAccept | EngineeringAccept | خیر | **بله** |
| salesOrProjectAccept | SalesOrProjectAccept | خیر | **بله نقش** — VPIS و تطبیق Expert/Manager/PM ردیف **نیست** |
| linkVpis | EngineeringAccept (فلگ مهندسی) | خیر | HasEngineering جدا نیست؛ با همان نقش مهندسی UI |
| stopRequest | Stop **یا** Industrial/Supply/Engineering | خیر | خیر (TODO ندارد ولی چک نقش هم ندارد) |
| triggeringRequest / inWay / statusInquiry / terminate | در UI اگر stopOpersions | خیر | Terminate صریحاً `TODO: بررسی دسترسی خاتمه` |
| addRequestedPersonel | ConfigManage | خیر | خیر |
| addSupplier | همیشه enable | — | **هندلر خالی** (فقط چک Id) |

نکته UI: `hasStartPermission` / `hasSendingPermission` / `hasQueryPermission` / `hasTerminatePermission` محاسبه می‌شوند ولی برای enable جداگانه استفاده نمی‌شوند؛ یک بسته `stopOpersions` هر پنج دکمه را با هم روشن می‌کند.

---

## 4. ماشین وضعیت توقف (مشترک — IDها یکسان)

LookupType 346/347/348 در TotalSystem با enumهای جدید **هم‌ارز** هستند.

```
(null/0 یا 2815) --ثبت--> 2807
2807 / 2816 / 2813 --نتیجه عامل--> 2808|2810|2811|2812|2813
2808..2812|2821 --تصمیم درخواست‌کننده--> 2815 | 2816 | 2817
جاب >۱۴ روز --> 2821
2815 --> IsStop=false
```

نتایج عامل (347): 2799→2808 ، 2800→2810 ، 2801→2811 ، 2802→2812 ، 2803→2813.  
تصمیم‌ها (348): 2804→2815 ، 2805→2816 ، 2806→2817.

در جدید منطق StopOperation در کنترلر پیاده شده و مهلت ۲/۳ روز هم هست. **جاب تایم‌اوت ۱۴روزه و ایمیل مهلت پاسخ در Havayar پیدا نشد.**

---

## 5. فیلتر گرید

### HTS
فیلتر سطری Kendo + رنگ:
- نارنجی: تایید فروش
- آبی: پنجره SupplyDate ±۳ روز و غیرتوقف
- سبز: ایمیل به پیمانکار
- بنفش: Changed
- قرمز: IsStop

### Havayar
`<datatableprofile entity-Type="typeof(OpenOrderRequest)">` + SavedQueryهای DataProfile:

| Name | نقش تقریبی |
|---|---|
| `openorderrequest_listinfo` / `...new` | پروفایل پیش‌فرض لیست |
| `vw_OpenOrderRequestAllActive` | همه فعال |
| `vw_OpenOrderRequestEngineeringAccepted` | تایید مهندسی‌شده |
| `vw_OpenOrderRequestOtherView` | سایر |
| `vw_OpenOrderRequestMyRequestes` | درخواست‌های من |
| `vw_openOrderRequestPurchaseCompleted` | خرید تکمیل‌شده |

`FetchData` کنترلر فیلتر نقش را در کد اعمال نمی‌کند؛ فیلتر باید از DataProfile/SavedQuery بیاید. پروفایل‌های راهنما `OOR_Config` و `OOR_History_Deleted` در DB **نیستند**.

---

## 6. اعلان‌ها (خلاصه)

HTS ایمیل SMTP با لیست‌های هاردکد (Saemian، Mosayebi، واحد تولید، …).  
جدید: `INotificationService` + گروه NotificationGroup. تبدیل کامل ۱:۱ گیرندگان هاردکد بررسی نشده؛ مسیر کلی جایگزین شده.

رویدادهای جاب جدید داخل `SyncOpenOrderRequestJobFromRahkaran`: رسید انبار، رسید جزئی، رد QC، تایید خودکار مهندسی.

---

## 7. صفحات/اکشن‌های کمکی HTS که به Manage وصل‌اند

- صفحه پیوست 71 (از History هم باز می‌شود)
- `OpenOrderRequestNotQc` صفحه 104 — خارج از منوی درخواستی شما (گزارش رسید موقت)
