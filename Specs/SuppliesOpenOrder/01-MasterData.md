# 01 — اطلاعات پایه تدارکات

منبع حقیقت: منوی HTS «اطلاعات پایه» در `_SupplayersSystemMenu.cshtml`. دروازه منو: `SystemType.Supp` یا `Engineering` + حداقل یکی از صفحات 65/70/72/83/414.

---

## A) تامین‌کنندگان — HTS Page 65

### فرآیند
لیست/ویرایش طرف‌حساب‌های نوع **`COMP`** از جدول مشترک `Gnr_ManCompany`. دکمه **جدید در UI غیرفعال** است (کامنت شده). داده اصلی از ERP می‌آید.

### صفحه
- کنترلر: `CompanyManagementController`
- ویو: `_CompanyManagement.cshtml`
- دکمه‌ها: ویرایش، حذف، ذخیره، انصراف (+ اکسل در دسترسی)
- فیلدهای فرم: نام شرکت، کد اقتصادی، موبایل، شناسه ملی، تلفن، فکس، کد پستی، ایمیل، گرید، خدمات/محصولات، نام شخص مرتبط، آدرس
- گرید: همان‌ها + FullName + DlRef
- اعتبارسنجی کلاینت: نام شرکت اجباری؛ کد پستی حداکثر ۲۰

### فیلدهای موجودیت (`Gnr_ManCompany`) — زیرمجموعه Supplies
علاوه بر فیلدهای فرم، جدول فیلدهای CRM/کارخانه/وضعیت شریک و `Hamkaran_ManCompany_FK` دارد. صفحه Supplies فقط `ManCompany_Type='COMP'` را نشان می‌دهد و هنگام ذخیره همان نوع را اجبار می‌کند.

### جاب
`HtsTaskService.BaseHamkaranTask` هر **۱۵ دقیقه** (ساعت ۶–۲۱) → `Import_AllCustomer` → `Import_AllManCompany`. کلید: `Hamkaran_ManCompany_FK`. Grade / ServicesAndProducts / RelatedPersonName همگام نمی‌شوند (فقط HTS).

### دسترسی PageAction
بدون دسترسی، کامل، مشاهده، جدید، ویرایش، حذف، ارسال به اکسل.

### HavayarApp
| قطعه | وضعیت |
|---|---|
| موجودیت | `Gnr.Supplier` + `Gnr.Party` (نه `ManCompany`) |
| صفحه | `/panel/supplier/list` + Edit |
| فیلدهای Edit | Party، پیشوند، کد پستی، تلفن، فکس، ایمیل، کد تفصیل |
| فیلدهای Party مرتبط | نام/شرکت، شناسه ملی، موبایل، ایمیل، کد اقتصادی، HamkaranId |
| **کم است نسبت به HTS** | Grade، ServicesAndProducts، RelatedPersonName، Fax روی ManCompany (Fax روی Supplier هست)، آدرس بلند، فیلتر اجباری نوع COMP در UI Supplies |
| جاب اختصاصی Supplies | همگام‌سازی کلی Party/Supplier از HTS/Rahkaran جدا از این منو است؛ صفحه CRUD استاندارد Form Builder |
| تعداد | HTS COMP 25 133 / Havayar Supplier 28 713 (مجموعه گسترده‌تر) |

`OpenOrderRequest.ManCompany` در جدید به `Supplier` اشاره می‌کند.

---

## B) دسته‌های خرید — HTS Page 70 + پیمانکاران هر دسته Page 73

### فرآیند
CRUD دسته خرید (عنوان، متولی خرید = کاربر، نوع lookup 162، lead time روتین/غیرروتین). دکمه «پیمانکاران» صفحه فرزند `Sup_BuyCategory_Company` را باز می‌کند.

### فیلدها `Sup_BuyCategory`

| فیلد HTS | نوع | UI |
|---|---|---|
| BuyCategory_ID | short PK | مخفی |
| BuyCategory_Title | string(400) | اجباری |
| User_FK | short? | متولی خرید (کاربر) اجباری |
| TypeId | short | lookup **162** اجباری |
| RoutineLeadTimeInDay | short? | در property grid **مخفی** |
| NonRoutineLeadTimeInDay | short? | در property grid **مخفی** |

Lookup 162 زنده: **1040 = خرید** ، **1041 = ساخت**.

### فیلدها `Sup_BuyCategory_Company`
BuyCategory_Company_ID، Company_Fk → ManCompany، BuyCategory_FK.  
**شمارش زنده: ۰ ردیف** — صفحه هست ولی داده عملیاتی ندارد.

### دسترسی
Page 70: CRUD + «تامین‌کنندگان» (`PermissionType.Company=21`).  
Page 73: CRUD کامل.

### جاب HTS
ندارد (دستی).

### HavayarApp

| قطعه | وضعیت |
|---|---|
| موجودیت والد | `Sup.BuyCategory` — Title، PurchaseResponsibleId، Routine/NonRoutineLeadTimeInDay، Type enum، HamkaranId، Items |
| موجودیت قلم | `Sup.BuyCategoryItem` — PartId، TimeInWay، Supplier enum، Comments، HamkaranId |
| کنترلر | فقط `BuyCategoryItemController` روی `typeof(BuyCategoryItem)` — List همان قلم است |
| Edit والد | `Views/Panel/Sup/BuyCategory/Edit.cshtml` برای `BuyCategory` وجود دارد ولی از List قلم‌ها به آن وصل نیست |
| Type enum | `Purchase=1` ، `Build=2` — **نه 1040/1041** |
| داده Type در DB جدید | 71 ردیف Type=1 ، 43 ردیف Type=2 |
| داده Type در HTS | 70×1040 ، 41×1041 |
| جاب | `BuyCategoryJob.SyncBuyCategoryJobFromRahkaran` — از USR3 Rahkaran؛ TypeRef را مستقیم به enum می‌ریزد |
| زمان‌بندی | تعریف شده؛ آخرین اجرا 2026-09-02 (ممکن است عقب افتاده باشد) |
| پیمانکاران دسته | **موجودیت/صفحه ندارد** (و در HTS هم ۰ ردیف) |

`BuyCategoryItem.Supplier`: ForeignTrade=1 / SupplyAndPurchase=2 — با LeadTime که 6 و 51 است یکسان نیست.

---

## C) اقلام دسته‌های خرید — HTS Page 72

### فرآیند
گرید همه کالاها LEFT JOIN به دسته. فقط **Update**: انتخاب چند کالا + انتخاب دسته → upsert `Sup_BuyCategory_Part`. Add/Delete در سرویس no-op. دکمه حذف UI کامنت شده.

### فیلدها `Sup_BuyCategory_Part`
BuyCategory_Part_ID، InvPart_Fk، BuyCategory_FK.

### دسترسی PageAction
بدون دسترسی، کامل، مشاهده، **حذف** — New/Edit در کاتالوگ PageAction نیست؛ کنترلر دستی `HasCrudPermissions` می‌گیرد.

### HavayarApp
ادغام در `BuyCategoryItem` + جاب Rahkaran. صفحه جدا برای «انتساب گروهی کالا به دسته» مثل HTS نیست؛ List استاندارد قلم + CRUD تکی.

---

## D) تامین‌کنندگان محصولات — HTS Page 83

### فرآیند
انتخاب چند کالا + MultiSelect پیمانکار (شرکت‌هایی با Email طول>1). برای هر کالا: حذف لینک‌های قبلی `Inv_Part_Company`، درج جدید، بازسازی `Inv_Part.CompanyNames`. ایمیل هشدار اگر `Company_FK` از مقدار هاردکد 176240 عوض شود (مسیر قدیمی؛ UI دیگر Company_FK نمی‌فرستد).

### فیلدها `Inv_Part_Company`
Id، PartId، CompanyId.

روی `Inv_Part`: `Company_FK` (تک‌تامین‌کننده قدیمی)، `CompanyNames` (دنرمالایز).

### دسترسی
بدون دسترسی، کامل، مشاهده. CRUD از طریق reflection روی همان صفحه.

### جاب HTS
ندارد.

### HavayarApp
| قطعه | وضعیت |
|---|---|
| موجودیت | `Inv.PartCompany` — PartId، SupplierId → `Supplier` |
| صفحه | List + مودال چندتایی `AddSuppliers` |
| جاب | `PartCompanyJob.SyncPartCompanyFromHts` روزانه (`IntervalSeconds=86400`) |
| شمارش | HTS 8 432 / جدید 8 423 |
| CompanyNames دنرمالایز | در موجودیت PartCompany نیست؛ اگر جایی به رشته نام‌ها وابسته باشد باید از Join ساخته شود |
| ایمیل هشدار تعویض تامین‌کننده خاص | پیاده نشده |

---

## E) زمان در راه — HTS Page 414

### فرآیند
CRUD روی `Pln_LeadTime` (نه جدول Sup). تامین‌کننده = واحد سازمانی **6 یا 51**. ورود اکسل: ستون‌های PartCode، LeadTime، SupplierId، Comment.

### فیلدها `Pln_LeadTime`

| فیلد | نوع | UI |
|---|---|---|
| Id | long | PK |
| PartId | long | DDL کالا |
| LeadTimeInDay | int | اجباری |
| LeadTimeInMinute | int? محاسبه‌ای | خیر |
| NonRoutineLeadTimeInDay | int? | **روی فرم نیست** |
| SupplierUnitId | short | واحد 6/51 |
| CreatedUserId / CreatedDate / CreatedDateInText | | هر ذخیره بازنویسی می‌شود |
| Comment | string(2048) | |
| RahkaranPartId | long? | |

### دسترسی
مشاهده، جدید، ویرایش، حذف. (FullAccess در extract این صفحه دیده نشد.)

### جاب HTS
ندارد. مصرف در محاسبه تاریخ تامین درخواست باز و شاخص‌ها.

### HavayarApp
| قطعه | وضعیت |
|---|---|
| موجودیت | `Pln.LeadTime` — PartId، LeadTimeDay، Supplier enum (6/51)، Comment |
| صفحه | List/Edit استاندارد؛ **ورود اکسل ندارد** |
| NonRoutineLeadTimeInDay | **نیست** |
| جاب همگام‌سازی از HTS | **نیست** |
| شمارش | HTS **3 202** / Havayar **0** |
| منو | هست (`/panel/pln/leadtime/list`) ولی جدول خالی است |

OpenOrderRequest در جدید برای تاریخ تامین از `LeadTime.LeadTimeDay` استفاده می‌کند — با جدول خالی این شاخه عملاً کار نمی‌کند مگر داده دستی وارد شود.
