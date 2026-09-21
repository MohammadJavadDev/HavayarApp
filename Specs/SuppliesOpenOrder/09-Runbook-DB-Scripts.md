# 09 — Runbook اجرای اسکریپت‌های DB (تدارکات / Sup) — 2026-09-17

هدف: `HavayarApp` روی `172.20.40.42` (`PortalSRV\PORTAL`). همهٴ اسکریپت‌ها idempotent هستند؛ اسکریپت‌های دارای `@DryRun` اول با ۱ و بعد با ۰ اجرا می‌شوند. لاگ خام اجراها: `%TEMP%\havayar-compare\run\`.

## الف) اجراشده در این جلسه (قبل از استقرار کد جدید — امن)

| # | اسکریپت | نتیجه |
|---|---|---|
| 1 | `Data/Scripts/Add_LeadTime_NonRoutineLeadTimeInDay.sql` (با `@RegisterEfMigration=1`) | ستون `Pln.LeadTime.NonRoutineLeadTimeInDay` اضافه شد؛ `20260917194320_AddLeadTimeNonRoutineLeadTime` در `__EFMigrationsHistory` ثبت شد |
| 2 | `Data/Scripts/Seed_LeadTime_ImportDefinition.sql` | `ImportDefinition` Id=11 «ورود اطلاعات زمان در راه (اکسل)» + ۴ ردیف RoleAccess |
| 3 | `Data/Scripts/Seed_LeadTime_DataProfile.sql` | پروفایل `leadtime_listinfo` (Id=41) به‌روزرسانی شد + ۳ ردیف RoleAccess (ShowAllMenus, Sup.OpenOrderRequest.Supply, SupplierMenu, SupplyAndPurchase) |
| 4 | `Data/Scripts/Migrate_LeadTime_FromHts.sql` (DryRun → Apply) | ۳٬۲۰۲ ردیف منبع، ۳٬۲۰۲ تطبیق (HtsId)، ۰ نامنطبق، **۳٬۲۰۲ درج**، ۰ به‌روزرسانی؛ `Pln.LeadTime` = ۳٬۲۰۲ ردیف |

## ب) در انتظار اجرا (این جلسه — بعد از بازبینی)

- WP5: `Report_Party_DuplicateHamkaranId.sql` (فقط SELECT)
- WP2: `Verify_OpenOrderRequest_DataProfiles.sql` → `Update_OpenOrderRequest_DataProfiles_HtsParity.sql` (DryRun → Apply) → Verify مجدد
- WP4: `Seed_OpenOrderRequest_RoleAccess.sql` → `Seed_OpenOrderRequest_RoleMembers_FromHts.sql` (DryRun → Apply)
- WP8: `Seed_OpenOrderRequest_NotificationGroups.sql` → `Seed_OpenOrderRequest_NotificationGroupMembers.sql` (DryRun → Apply)

## پ) فقط پس از استقرار کد جدید (WebApp + App.BackgroundJob)

| ترتیب | اسکریپت | چرا بعد از استقرار |
|---|---|---|
| 1 | `Reset_JobSchedule_SyncBuyCategoryJob.sql` | کد قدیمی جاب همچنان روی HamkaranId تکراری می‌خوابد |
| 2 | `Backfill_BuyCategory_LeadTime_FromHts.sql` (DryRun → Apply) | کد قدیمی جاب با NULL راهکاران مقدار را پاک می‌کند |
| 3 | `Seed_BuyCategory_DataProfiles.sql` → restart WebApp | پروفایل صفحهٴ جدید `/panel/sup/buycategory/list` |
| 4 | `Seed_BuyCategory_Menu.sql` | آیتم منو به مسیر جدید اشاره می‌کند (قبل از استقرار 404) |
| 5 | `Seed_OpenOrderRequestStopJobs_Schedule.sql` | `JobDiscoveryService` نسخهٴ قدیمی ردیف‌های JobDefinition ناشناس را پاک می‌کند |
| 6 | اسکریپت‌های WP1 (جدول `Sup.OpenOrderRequestEmailToSupplier`، مهاجرت ۳۷٬۹۵۲ ردیف، پروفایل/منو) | جدول جدید + صفحهٴ جدید |
| 7 | اسکریپت‌های WP7 (VPIS) و WP9 (فیلدهای تامین‌کننده) | ستون/جدول جدید |

یادآوری: جاب ایمیل (JobDefinition 38) طبق Q10 **غیرفعال می‌ماند**؛ فقط اعضای گروه‌های اعلان پر می‌شوند.

## WP9 اجرا

`Add_Supplier_HtsFields` (`@RegisterEfMigration=1` در کپی run): چهار ستون `Grade` / `ServicesAndProducts` / `RelatedPersonName` / `Address` به `Gnr.Supplier` اضافه شد؛ `20260917203247_AddSupplierHtsFields` در `__EFMigrationsHistory` ثبت شد. `Update_Supplier_DataProfile_HtsFields`: `supplier_listinfo` (Id=38) به‌روز شد؛ هر چهار نام ستون در `ColumnsJson` هست. Add_* در WP9b تکرار نشد.

WP9b (2026-09-18): در `Migrate_Supplier_HtsFields_FromHts.sql` ستون‌های تشخیصی `#Src.MatchBy` و `ManCompanyType` به `nvarchar(100)` عریض شد (قبلاً `MatchBy nvarchar(20)` مقدار `HamkaranId=ManCompany_ID` را به `HamkaranId=ManCompan` برش می‌داد). DryRun: منبع **۲۹٬۱۷۵**؛ دارای حداقل یکی از چهار فیلد **۱۷٬۳۱۸**؛ تطبیق **۲۸٬۸۳۶** (HamkaranFK=۲۸٬۷۱۴، ManCompany_ID=۶۷، HtsId=۵۵)؛ نامنطبق کل ۳۳۹؛ نامنطبقِ دارای مقدار **۱۸۶** (۱٫۰۷٪ ≤ ۱۰٪)؛ would-update **۱۷٬۰۶۵**؛ بدون خطا. Apply: بله — **۱۷٬۰۶۵** تامین‌کننده به‌روز شد. پس از اعمال، غیرتهی روی `Gnr.Supplier`: Grade=۱، ServicesAndProducts=۱، RelatedPersonName=۱، Address=۱۷٬۰۶۵. لاگ: `%TEMP%\havayar-compare\run\wp9b\`.

## WP2 اجرا روی DB (2026-09-18)

`Verify_OpenOrderRequest_DataProfiles.sql` سپس `Update_OpenOrderRequest_DataProfiles_HtsParity.sql` (`@DryRun=1` → ROLLBACK، بعد کپی با `@DryRun=0` → COMMIT) روی `HavayarApp` / `PortalSRV\PORTAL`. قبل: ۸ نمایه Mode=0 و ۳۹ ستون؛ فعال پنهان با INNER JOIN برابر **۱۴۷** (۱۳۹۸−۱۲۵۱، D34)؛ تکرار Id جوین مطلوب **۰**؛ تکرار INNER قدیمی **۷۳۴** شناسه. بعد از اعمال: ۸× UPDATE (ستون ۳۹→۶۴، INNER→LEFT/APPLY، RoleAccess ۲۹→۳۶)؛ پنهان در نمایه **۱۴۷→۰**؛ تکرار مطلوب همچنان **۰**. خطا: Verify رسمی Msg 156 L80 (`AS RowCount`؛ اجرا با `[RowCount]` فقط در پوشهٔ run) و Msg 208 L223 (`IntendedDup` خارج از CTE — بخش ۵/۶ ناقص). نقش‌های View/HistoryView غایب (WP4). لاگ: `%TEMP%\havayar-compare\run\wp2\`. WebApp/کش نمایه را ری‌استارت کنید.

## WP4 اجرا روی DB

`Seed_OpenOrderRequest_RoleAccess.sql` سپس `Seed_OpenOrderRequest_RoleMembers_FromHts.sql` (`@DryRun=1` → گزارش، بعد کپی با `@DryRun=0` → COMMIT) روی `HavayarApp` / `PortalSRV\PORTAL` (Q9). `[TMS]` موجود. نقش **100013 View** و **100014 HistoryView** ساخته شد. RoleAccess اکشن: حذف D35=**۳۱**، درج اکشن=**۱۰۵**، درج پروفایل=**۲**؛ پس از اعمال **۱۴۱** ردیف `Path LIKE '/panel/sup/openorderrequest/%'`. اعضا DryRun: HTS grants=**۱۴۴۳**؛ add=**۵۰۷** (View ۱۸۲، HistoryView ۱۶۹، HasEngineering ۴۴، EngineeringAccept ۳۶، ShowAll ۲۷، Stop ۲۵، ConfigManage ۱۷، SalesOrProjectAccept ۳، SupplyAndPurchase ۴)؛ already بدون حذف؛ missing-user=**allahverdi.s**، **drghoroori** (غیرکشنده). Apply: **۵۰۷** عضویت اضافه شد. پس از اعمال: View=۱۸۲، HistoryView=۱۶۹، Stop=۱۱۴، ShowAll=۱۰۲، EngineeringAccept=۸۵، HasEngineering=۶۳، Supply=۳۴، ConfigManage=۳۳، SupplyAndPurchase=۲۵، Start=۱۲، Industrial=۱۱، ConfigManageStaticPersonel=۸، SalesOrProjectAccept=۶، Terminate=۵، Sending/Query=۰. Verify اصلاح‌شده (`ProfileRowCount` + CTE `IntendedDup` در اسکوپ): Msg 156/208 رفع؛ بخش ۵/۶ کامل؛ View/HistoryView «هست». کاربران باید دوباره لاگین کنند. لاگ: `%TEMP%\havayar-compare\run\wp4\`.

## اجرای امن باقی‌مانده (2026-09-18، Q9)

WP8: `Seed_OpenOrderRequest_NotificationGroups.sql` — Apply بله؛ **۴** گروه درج شد (ArrivalWarehouse Id=۱۳، ArrivalPartial=۱۴، QcRejection=۱۵، UnsentDispatchDigest=۱۶؛ SupplyUnit/Industrial از قبل بودند). `Seed_OpenOrderRequest_NotificationGroupMembers.sql` (`@DryRun=1` سپس ۰) — Apply بله، additive؛ **۵۲** عضو؛ پس از اعمال ArrivalWarehouse=**۸**، ArrivalPartial=**۱**، QcRejection=**۲۰**، UnsentDispatchDigest=**۶** (همه >۰)، SupplyUnit=۷، Industrial=۱۰. بی‌تطبیق غیرکشنده: `zamani.z`، `Zandi.n`، `Banafshechin.y`، `Hosseini.h`، `Tafakor.s`. WP1: `Migrate_OpenOrderRequestEmailToSupplier_FromHts.sql` — جدول خالی بود. DryRun: منبع **۳۷٬۹۵۲**؛ mappable **۳۷٬۱۳۴**؛ UnmatchedRequest=۰؛ UnmatchedSupplier=**۸۱۸** (۲٫۱۶٪ ≤۵٪)؛ WouldInsert=۳۷٬۱۳۴. Apply بله پس از اصلاح PRINT/Msg 1046 (زیرکوئری در `PRINT`) و تکرار با `QUOTED_IDENTIFIER ON` (`-I`؛ Apply اول Msg 1934 و rollback، جدول همچنان ۰). درج **۳۷٬۱۳۴**؛ شمار نهایی `Sup.OpenOrderRequestEmailToSupplier`=**۳۷٬۱۳۴** (همه با HtsId). ShamsiFn=۰. WP5: `Report_Party_DuplicateHamkaranId.sql` فقط SELECT — DuplicatePartyHamkaranIds_AcrossUsers=**۵** (۲۸۶=`Soleimani.a`/`soleimani.ali` و ۴ زوج دیگر)؛ DuplicateUserHamkaranIds=۰؛ DuplicatePartyHamkaranIds=۰؛ BuyCategory=۱۱۴ (۳ بدون مسئول). لاگ: `%TEMP%\havayar-compare\run\safe-now\`. اسکریپت‌های post-deploy اجرا نشد.
