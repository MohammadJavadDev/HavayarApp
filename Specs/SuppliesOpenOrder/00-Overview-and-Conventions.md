# تدارکات — مقایسه HTS و HavayarApp (محدوده منوی درخواستی)

> خوانده‌شده **2026-09-15** از کد HTS، کد HavayarApp، TotalSystem (`172.20.40.27`) و HavayarApp DB (`172.20.40.42`).  
> خروجی خام: `_extract/*.csv`. **هیچ تغییری در کد داده نشده.**

## 1. هدف

مقایسه دقیق منوی **سیستم تدارکات** (`Gnr_System.System_ID = 13`، عنوان «سیستم تدارکات»، `System_Name = Supp`) برای این شاخه‌ها:

| منوی HTS | SystemPage | Page_ID | صفحه HTS | معادل HavayarApp |
|---|---|---|---|---|
| تدارکات → اطلاعات پایه → تامین‌کنندگان | `Company` | 65 | `_CompanyManagement` | `Gnr.Supplier` `/panel/supplier/list` |
| تدارکات → اطلاعات پایه → دسته‌های خرید | `BuyCategory` | 70 (+73) | `_BuyCategoryManagement` | `Sup.BuyCategory` / `BuyCategoryItem` `/panel/sup/buycategoryitem/list` |
| تدارکات → اطلاعات پایه → اقلام دسته‌های خرید | `BuyCategory_Part` | 72 | `_BuyCategoryPartManagement` | همان `BuyCategoryItem` (ادغام شده) |
| تدارکات → اطلاعات پایه → تامین‌کنندگان محصولات | `Product_Company` | 83 | `_ProductCompanyManagement` | `Inv.PartCompany` `/panel/inv/partcompany/list` |
| تدارکات → اطلاعات پایه → زمان در راه | `Supp_LeadTime` | 414 | `_LeadTime` | `Pln.LeadTime` `/panel/pln/leadtime/list` |
| تدارکات → عملیات → درخواست‌های باز | `OpenOrderRequest_Manage` | 74 | `_OpenOrderRequestManagement` | `Sup.OpenOrderRequest` `/panel/sup/openorderrequest/list` |
| تدارکات → عملیات → تنظیمات درخواست‌های باز | `OpenOrderRequest_Config` | 76 | `_OpenOrderRequestConfigManagement` | **صفحه جدا ندارد** (نقش + مودال در Edit) |
| تدارکات → عملیات → پیشینه درخواست‌ها | `OpenOrderRequest_History` | 77 | `_OpenOrderRequestHistoryManagement` | **صفحه جدا ندارد** |
| تدارکات → عملیات → پیشینه ارسال به پیمانکاران | `Sup_SendToSupplyersHistory` | 545 | `_OpenOrderRequestEmailToSupplier` | **وجود ندارد** |

منوی زنده HTS: `Areas/SupplayersSystem/Views/Shared/Menu/_SupplayersSystemMenu.cshtml`  
منوی زنده Havayar: `system.SystemMenu` Id=3، Name=`Supply`، Title=«تامین و خرید».

## 2. محدوده / خارج از محدوده

### داخل محدوده (این پوشه)

| فایل | محتوا |
|---|---|
| این فایل | قراردادها، منو، شمارش رکورد، نقشه دسترسی کلی |
| `01-MasterData.md` | اطلاعات پایه (۵ صفحه) |
| `02-OpenOrderRequest.md` | درخواست‌های باز عملیاتی |
| `03-OpenOrderRequestConfig.md` | تنظیمات درخواست‌های باز |
| `04-OpenOrderRequestHistory.md` | پیشینه درخواست‌ها |
| `05-SendToSuppliersHistory.md` | پیشینه ارسال به پیمانکاران |
| `06-Jobs-Triggers-Permissions.md` | جاب، SP، تریگر، ماتریس دسترسی |
| `99-Discrepancies.md` | **مغایرت‌ها برای بررسی شما** |

### خارج از محدوده این موج

گزارش‌ها و عملیات دیگر منوی تدارکات (فرم وضعیت خرید، استعلام قیمت، استعلام فنی، رسید موقت/NotQc، لیگ تامین‌کنندگان، قرارداد) — مگر جایی که به درخواست باز وصل باشند.

## 3. قرارداد نام‌گذاری

| موضوع | HTS | HavayarApp |
|---|---|---|
| اسکیما / ماژول | جداول `Sup_*` / `Pln_LeadTime` / `Inv_Part_Company` / `Gnr_ManCompany` | اسکیما `Sup` / `Pln` / `Inv` / `Gnr` |
| مسیر کنترلر | Area `SupplayersSystem` | `Panel/Sup/[controller]` (LeadTime زیر `Panel/Pln`، PartCompany زیر `Panel/Inv`، Supplier زیر `Panel/Gnr`) |
| دسترسی صفحه | `Gnr_Page` + `Gnr_PageAction` + `PermissionType` | `[ActionDisplayName]` → `RoleAccess` **به‌علاوه** نقش‌های seed شده `Sup.OpenOrderRequest.*` |
| لاجیک ذخیره | Service در `Hts.EntityServices` | کنترلر + جاب؛ **بدون** لایه سرویس جدید |
| تاریخ | رشته شمسی + گاهی میلادی جدا | جفت Shamsi/Miladi روی `BaseEntity` و فیلدهای دامنه |
| اعلان | SMTP مستقیم `UtilityHelper.SendEmail` | `Notification` + گروه `Sup.OpenOrderRequest.Industrial` / `SupplyUnit` |

## 4. شمارش زنده (2026-09-15)

### TotalSystem

| جدول / فیلتر | تعداد |
|---|---|
| `Sup_OpenOrderRequest` فعال (`IsDeleted=0`) | 1 051 |
| `Sup_OpenOrderRequest` حذف‌شده (`IsDeleted=1`) | 124 532 |
| `Sup_OpenOrderRequest_EmailToSupplier` | 37 939 |
| `Sup_BuyCategory` | 111 |
| `Sup_BuyCategory_Part` | 23 002 |
| `Sup_BuyCategory_Company` | **0** (جدول خالی) |
| `Pln_LeadTime` | 3 202 |
| `Inv_Part_Company` | 8 432 |
| `Gnr_ManCompany` نوع `COMP` | 25 133 |

### HavayarApp

| جدول / فیلتر | تعداد |
|---|---|
| `Sup.OpenOrderRequest` فعال | 1 407 |
| `Sup.OpenOrderRequest` حذف‌شده | 124 226 |
| `Sup.BuyCategory` | 114 |
| `Sup.BuyCategoryItem` | 23 087 |
| `Pln.LeadTime` | **0** |
| `Inv.PartCompany` | 8 423 |
| `Gnr.Supplier` | 28 713 |
| `Sup.OpenOrderRequestEmailToSupplier` | **جدول ندارد** |

## 5. درخت منوی Havayar «تامین و خرید» (Id=3)

مسیرهای واقعی از `system.SystemMenu.Content`:

| برچسب تقریبی | Path |
|---|---|
| تامین‌کنندگان → لیست | `/panel/supplier/list` |
| تامین‌کنندگان محصولات | `/panel/inv/partcompany/list` |
| دسته‌های خرید | `/panel/sup/buycategoryitem/list` |
| زمان در راه | `/panel/pln/leadtime/list` |
| درخواست‌های باز | `/panel/sup/openorderrequest/list` |
| کالا | `/panel/inv/part/list` |
| استعلام قیمت کالا | `/panel/sup/inquirypartprice/list` |
| (سایر اقلام خارج از محدوده) | سفارش ساخت / کسری / استعلام فنی / Bom |

**در منوی جدید نیست:** تنظیمات درخواست باز، پیشینه درخواست‌ها، پیشینه ارسال به پیمانکاران، صفحه جدا «اقلام دسته خرید»، صفحه جدا «پیمانکاران هر دسته».

## 6. منابع کد HTS

| نقش | مسیر |
|---|---|
| منو | `HtsWebApplication/Areas/SupplayersSystem/Views/Shared/Menu/_SupplayersSystemMenu.cshtml` |
| صفحات | `.../Enums/Pages.cs` ، `PermissionType.cs` |
| کنترلرها | `Areas/SupplayersSystem/Controllers/*` |
| موجودیت‌ها | `Hts.Data.Model/MainEntities/Sup_*.cs` |
| جاب ویندوز | `HtsTaskService/HtsTaskService.cs` |
| همگام‌سازی Rahkaran | `WriteData.Import_AllOpenOrderRequest` ، `Import_AllManCompany` |
| SP | `dbo.Sup_Compute_OpenOrderRequest` |

## 7. منابع کد HavayarApp

| نقش | مسیر |
|---|---|
| موجودیت درخواست باز | `Entities/App/Sup/OpenOrderRequest.cs` |
| کنترلر | `WebApp/Controllers/Dynamic/Sup/OpenOrderRequestController.cs` |
| ویوها | `WebApp/Views/Panel/Sup/OpenOrderRequest/` |
| جاب | `App.BackgroundJob/Jobs/Sup/OpenOrderRequestJob.cs` |
| نقش‌ها | `Entities/Auth/Role.cs` (Id 100000–100012) |
| راهنمای DataProfile (اعمال ناقص) | `Data/Scripts/OpenOrderRequest_DataProfiles_Guide.html` |
