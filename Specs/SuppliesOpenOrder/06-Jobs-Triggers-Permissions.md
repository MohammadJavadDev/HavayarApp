# 06 — جاب‌ها، SP، تریگرها، دسترسی‌ها

---

## 1. جاب / تسک HTS (`HtsTaskService`)

پنجره کلی همگام‌سازی ERP: ساعت **۶ تا ۲۱**؛ `BaseHamkaranTask` هر **۱۵ دقیقه** (`Minute % 15 == 0 && Second == 1`).

| تسک | زمان | اثر روی این منو |
|---|---|---|
| `BaseHamkaranTask` → `Import_AllManCompany` | هر ۱۵ دقیقه | تامین‌کنندگان COMP |
| `BaseHamkaranTask` → `Import_AllProduct` | هر ۱۵ دقیقه | کالا برای دسته/LeadTime/PartCompany |
| `BaseHamkaranTask` → `Import_AllOpenOrderRequest` | هر ۱۵ دقیقه | upsert درخواست باز + اجرای SP + ایمیل رسید/QC |
| بعد از import اگر کالا عوض شد | همان مسیر | `ChangeOpenOrderRequestStatusAndSendNotification` (تایید خودکار مهندسی) |
| `SupplysSystemTask` | **هر روز ۹:۳۵** | کامنت تامین امروز؛ مهلت پاسخ توقف؛ تایم‌اوت ۱۴روزه → 2821 |
| `AddStaticPersonsToOpenOrderRequest` | در `ImportSpecialHamkaranTask` **کامنت شده** | — |
| الصاق خودکار مدارک مهندسی به درخواست جدید | **کامنت شده** | — |

ورود اکسل LeadTime و CRUD دسته‌ها جاب ندارند.

---

## 2. Stored Procedure های TotalSystem

| نام | نقش |
|---|---|
| **`Sup_Compute_OpenOrderRequest`** | زنده — اختتام OrderItemState=7، Delay، احیای حذف نرم، حذف نرم وضعیت‌های بسته، لینک ProductionOrder، SalesUnit*، IsRoutineRequest |
| `Sup_Compute_OpenOrderRequest_New` | نسخه جایگزین در DB |
| `Sup_Compute_OpenOrderRequest_Delphi` | نسخه قدیمی |
| `Com_Compute_ForeignOpenOrderRequest` | درخواست باز خارجی (خارج از منوی فعلی) |

متن SP مرجع در ریپوی HTS: `docs/modernization/evidence/database-export-2026-09-07/Sup_Compute_OpenOrderRequest.sql`.

معادل جدید: متد خصوصی `OpenOrderRequestJob.ComputeOpenOrderRequest` + `CalculateDelaysBuyDay` داخل همان جاب همگام‌سازی Rahkaran (نه SQL جدا روی TotalSystem).

---

## 3. تریگر

روی جداول این محدوده فقط:

| تریگر | جدول | وضعیت |
|---|---|---|
| `Update_Requested_PersonelEmails` | `Sup_OpenOrderRequest` | **Disabled** |

منطق پرسنل/ایمیل در لایه اپلیکیشن (Config DoOperation) است نه تریگر فعال.

Havayar: `WebApp/Actions` برای OpenOrderRequest **وجود ندارد**. ذخیره از کنترلر/جاب می‌گذرد.

---

## 4. جاب‌های HavayarApp (تعریف + زمان‌بندی زنده)

| JobDefinition | متد | زمان‌بندی DB |
|---|---|---|
| همگام درخواست باز از راهکاران | `SyncOpenOrderRequestJobFromRahkaran` | فعال، هر **۳۶۰۰ ثانیه** |
| پیوست فعال از HTS | `SyncOpenOrderRequestAttachmentsFromHts` | فعال، روزانه (86400، نوع 1) |
| تامین‌کنندگان محصول از HTS | `SyncPartCompanyFromHts` | فعال، روزانه |
| دسته‌های خرید از راهکاران | `SyncBuyCategoryJobFromRahkaran` | تعریف+schedule؛ **آخرین اجرا 2026-09-02** |
| تغییر Revision VPIS | `CheckVpisRevisionChangesAndNotify` | تعریف هست — **JobSchedule در نتیجه کوئری نبود** |
| LeadTime از HTS | — | **نیست** |
| مهلت/تایم‌اوت توقف | — | **نیست** |
| ایمیل کامنت تامین روزانه | — | **نیست** (مسیر Notification جایگزین ناقص) |

داخل `SyncOpenOrderRequestJobFromRahkaran`: Compute، اعلان رسید/QC، تایید خودکار مهندسی.

اسکریپت ops: `Data/Scripts/SyncOpenOrderRequestFromTotalSystem.sql` برای کات‌اوور از HTS (نه جایگزین جاب دوره‌ای).

---

## 5. ماتریس دسترسی HTS PageAction (کاتالوگ)

استخراج `_extract/page_actions.csv`.

| Page | عنوان | Permission_IDهای ثبت‌شده |
|---|---|---|
| 65 | تامین‌کنندگان | 1,2,3,4,5,6,10 |
| 70 | دسته‌های خرید | 1,2,3,4,5,6, **21 تامین‌کنندگان** |
| 71 | پیوست درخواست باز | 1,2,3,4,5,6,20 |
| 72 | اقلام دسته | 1,2,3,6 |
| 73 | پیمانکاران هر دسته | 1,2,3,4,5,6 |
| 74 | درخواست‌های باز | 1,2,3,5,7,10,12,13,19,22,23,24,25,26,82,101,187 |
| 76 | تنظیمات | 1,2,3, **27 ارسال ایمیل** |
| 77 | پیشینه | 1,2,3,10,19 |
| 83 | تامین‌کنندگان محصول | 1,2,3 |
| 414 | زمان در راه | 3,4,5,6 |
| 545 | پیشینه ارسال | 2,3 |

معنی مهم 74: نمایش همه=7، تایید مهندسی=12، تایید=13، پیوست=19، توقف=22، راه‌اندازی=23، در راه=24، استعلام=25، بازخوانی=26، دسترسی مهندسی=82، خاتمه=101، فروش/پروژه=187.

---

## 6. نقش‌های seed شده Havayar (`system.Role`)

| Id | Name | معادل تقریبی HTS |
|---|---|---|
| 100000 | EngineeringAccept | Accept_Engineering 12 + بخشی از org مهندسی |
| 100001 | SalesOrProjectAccept | SalesOrProjectPermission 187 |
| 100002 | Stop | Stop 22 |
| 100003 | Start | Start 23 |
| 100004 | Sending | Sending 24 |
| 100005 | Query | Query 25 |
| 100006 | Terminate | Terminate 101 |
| 100007 | HasEngineering | HasEngineeringPermission 82 |
| 100008 | ShowAll | ShowAll 7 |
| 100009 | Industrial | Org 12 |
| 100010 | Supply | Org 51/63 |
| 100011 | ConfigManage | صفحه 76 |
| 100012 | ConfigManageStaticPersonel | نفرات ثابت (جایگزین لیست هاردکد) |

نقش‌های منو جدا: `SupplyAndPurchase`، `SupplierMenu`.

CRUD لیست/ویرایش از `[ActionDisplayName]` روی List/Edit/FetchData است. اکشن‌های عملیاتی درخواست باز **در کاتالوگ RoleAccess نیستند** (بدون ActionDisplayName) و به جز دو مورد تایید، روی سرور نقش را اجباری نمی‌کنند.

---

## 7. Lookupهای مشترک (باید حفظ شوند)

نوع مدرک 66، نوع دسته 162، نوع توقف 346، نتیجه بررسی 347، وضعیت بررسی 348، طبقه کاربری 350 (2829/2830).  
enumهای توقف/پیوست در جدید همان ID را دارند. **Type دسته خرید در جدید 1/2 است نه 1040/1041.**
