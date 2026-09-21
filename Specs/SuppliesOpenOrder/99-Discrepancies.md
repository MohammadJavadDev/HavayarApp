# 99 — مغایرت‌ها، راستی‌آزمایی و برنامه اصلاح

نسخه: **2026-09-17** (1405/06/26) — جایگزین نسخه 2026-09-15.  
منبع: کد HTS + TotalSystem (`172.20.40.27`) + کد HavayarApp + HavayarApp DB (`172.20.40.42`)، همه فقط‌خواندنی. ماتریس سطر‌به‌سطر: `07-Comparison-Matrix.md`. بسته شواهد: `%LOCALAPPDATA%\Temp\havayar-compare\` (`02-hts-inventory.md`, `03-havayar-inventory.md`, `04-db-truth.md`, `db\*`).  
**هیچ تغییری در کد داده نشده.** شماره‌گذاری D1–D33 نسخه قبل حفظ شده؛ D34–D52 جدیدند.

اولویت: **P0** فرآیند HTS قطع است · **P1** فرآیند ناقص / داده خالی / ریسک داده · **P2** فیلد، دسترسی، UI · **P3** کم‌اثر یا آگاهانه.  
حکم راستی‌آزمایی: `تایید` = هنوز درست · `تغییر` = بخشی درست، توضیح داده شده · `اصلاح‌شده/نادرست` = شواهد رد می‌کند.  
هر مورد باز یک «راه‌حل پیشنهادی» (مطابق قراردادهای ریپو: Data Profile روی List موجود، مودال `$.confirm` برای فرزند، جاب در `App.BackgroundJob`، Action در `WebApp\Actions`، seed در `Data\Scripts`) و یک «نیاز به تصمیم؟» دارد.

---

## 0. خلاصه

| شمارش | تعداد |
|---|---|
| D1–D33 → `تایید` | **26** |
| D1–D33 → `تغییر` | **7** (D3, D4, D8, D12, D24, D28, D32) |
| D1–D33 → `اصلاح‌شده/نادرست` | **0** (فقط دو ادعای جزئی در D3/D4 و 02-OpenOrderRequest.md §5 با DB امروز نمی‌خواند — در متن توضیح داده شده) |
| مغایرت جدید D34–D52 | **19** |
| اولویت کاهش‌یافته (به P3) | D14, D15, D18, D19, D23 |

### ۱۰ مورد مهم باز

| # | D | یک‌خطی |
|---|---|---|
| 1 | D1, D2, D26 | «پیشینه ارسال به پیمانکاران» + ایمیل به پیمانکار + تامین‌کننده منتخب کاملاً غایب؛ HTS هنوز روزانه ردیف می‌زند (۳۷٬۹۵۲ تا 1405/06/25) |
| 2 | D34 | INNER JOIN دسته خرید در ۸ پروفایل: **۱۴۷ درخواست فعال نامرئی**، ۳۹ درخواست تکراری |
| 3 | D12 | جاب دسته خرید ۳۰ روز خراب (کلید تکراری `HamkaranId=286` بین دو کاربر)؛ دسته/اقلام کهنه |
| 4 | D36, D11, D20 | VPIS: ۰ ردیف در جدید در برابر ۲٬۸۵۹ (HTS هنوز خودکار درج می‌کند)، جاب رویژن بدون زمان‌بندی، گیت فروش/پروژه بدون VPIS |
| 5 | D9, D35 | هیچ چک نقش سرور روی توقف/راه‌اندازی/در راه/استعلام/خاتمه/پیوست/VPIS/پرسنل؛ Delete فیزیکی و ایجاد دستی برای ۴ نقش باز است |
| 6 | D38, D39 | نگاشت اعضای گروه‌های HTS انجام نشده (۱۹۳→۴۴، ۹۳→۲۵، ۹→۱)؛ نقش «مشاهده» و «مشاهده پیشینه» وجود ندارد (۱۰۶ + ۴۳ کاربر) |
| 7 | D40, D24, D43 | جاب ایمیل غیرفعال از 2026-08-16 + گروه‌های اعلان Sup ۰ عضو + لیست‌های هاردکد → عملاً کسی ایمیل نمی‌گیرد |
| 8 | D7 | جاب روزانه ۹:۳۵ (یادآوری مهلت، ۱۴ روز → 2821، گزارش کامنت) نیست |
| 9 | D5, D37, D17, D6 | LeadTime ۰ ردیف و lead time ۱۱۴ دسته NULL → تاریخ تامین و تاخیر خرید عملاً محاسبه نمی‌شود |
| 10 | D3, D4, D41, D22, D45 | تنظیمات/پیشینه حالا پروفایل دارند ولی ستون‌ها، رنگ‌ها، فقط‌خواندنی و نقش‌ها با HTS نمی‌خواند |

---

## 1. راستی‌آزمایی D1–D33 — جدول خلاصه

| D | عنوان | اولویت | حکم | شواهد کلیدی |
|---|---|---|---|---|
| D1 | پیشینه ارسال به پیمانکاران | P0 | `تایید` | `Sup_OpenOrderRequest_EmailToSupplier` 37,952 ردیف، آخرین 1405/06/25 [`04-db-truth` A8]؛ جدید: جدول/صفحه/پروفایل/منو هیچ [B1.1, B2] |
| D2 | ایمیل به پیمانکار + تامین‌کننده منتخب | P0 | `تایید` | `Edit.cshtml` L612–618 هندلر `addSupplier` فقط Id چک می‌کند؛ اکشن ارسال ایمیل و `AddComponyMansToRequests` معادل ندارند |
| D3 | صفحه تنظیمات = گرید همه فعال‌ها | P0→**P1** | `تغییر` | پروفایل 99 `vw_openRequestConfig` («تنظیمات درخواست های باز»، IsDeleted=0، multiSelect، دکمه «افزودن درخواست کنندگان و ذینفعان» → `AddRequestedPersonelToOpenRequest`، نقش‌های 100011/100009/100017/200052) **وجود دارد** → تخصیص گروهی پرسنل حل شده. باقی: ایمیل/تامین‌کننده (D2)، گرید فرزند ایمیل (D1)، ستون‌ها (D41)، رنگ (D22)، واحد سازمانی (D47)، hygiene (D49) |
| D4 | صفحه پیشینه درخواست‌ها | P0→**P1** | `تغییر` | پروفایل 73 `vw_openOrderRequestPurchaseCompleted` عنوان «پیشینه درخواست ها» و فیلتر دقیقاً `IsDeleted=1` است (ادعای «معادل کامل نیست» نسخه قبل نادقیق بود). باقی: نقش مشاهده پیشینه (D39)، delete فعال (D45)، ستون‌ها (D41)، فیلتر part-permission (D8) |
| D5 | LeadTime خالی | P1 | `تایید` | `Pln.LeadTime` 0 ردیف [B1.1]؛ `LeadTime.cs` بدون HtsId؛ هیچ JobDefinition [B5] |
| D6 | ورود اکسل LeadTime | P1 | `تایید` | `LeadTimeController` فقط CRUD؛ ImportDefinition برای LeadTime تعریف نشده (قابل استفاده بدون کد جدید) |
| D7 | جاب ۹:۳۵ (مهلت، 2821، گزارش روزانه) | P1 | `تایید` | JobDefinition لیست [B5] چنین جابی ندارد؛ 2821 در DB جدید ۰ استفاده |
| D8 | فیلتر دید گرید | P1 | `تغییر` | `FetchData` L149 بدون فیلتر؛ پروفایل‌ها بخش بزرگی از ماتریس را پوشش می‌دهند [07 §3.2] ولی: 103 به کاربر جاری محدود نیست، مهندسی (100000) فقط «منتظر تایید» را دارد، INNER JOIN (D34) |
| D9 | چک نقش سرور اکشن‌ها | P1 | `تایید` | کنترلر: `StopOperation` L382 بدون چک؛ `Triggering` L603، `SetInWayStatus` L650، `Terminate` L702، `DoLinkVpis` L1854 = TODO؛ `StatusInquiry`، `SaveAttachment`، `DeleteAttachment`، `AddRequestedPersonelToOpenRequest` بدون چک. نقش‌های 100002–100006 و 100012 بدون RoleAccess (فلگ) [B4]؛ Sending/Query ۰ عضو (با HTS سازگار: فقط FullAccess داشتند) |
| D10 | List دسته = قلم | P1 | `تایید` | فقط `BuyCategoryItemController`؛ پروفایل 40 با new/edit/delete غیرفعال؛ `BuyCategory/Edit.cshtml` بدون مسیر [`03-havayar-inventory` §2.2, §3.4] |
| D11 | جاب VPIS revision | P1 | `تایید` | Def 32 بدون JobSchedule، هرگز اجرا نشده [B5] + جدول VPIS خالی (D36) |
| D12 | جاب دسته خرید «احتمالاً» متوقف | P1 | `تغییر` (قطعی و ریشه‌یابی‌شده) | ۳۴۸/۳۴۸ خطا از 2026-08-18، آخرین تلاش 2026-09-02، `NextRunTime` گیر [B5]. علت (بررسی زنده 2026-09-17): `BuyCategoryJob.cs` L42–46 `ToDictionaryAsync(x => x.HamkaranId)` روی کاربران؛ ۵ `Gnr.Party.HamkaranId` بین دو `system.User` مشترک است: 286 (Soleimani.a / soleimani.ali — هر دو غیرفعال)، 121003468 (RamezanKhanlou.y / ramezani.y)، 121003756 (Asgari.v / asgari.a)، 121005539 (cnc / Farokhi.h)، 121006161 (ahvaz.la1 / Bosaghzadeh.e) |
| D13 | جدول فرزند Requested_Personel | P2 | `تایید` | ستون‌های `RequestedPersonelIds` / `RequestedEngineeringPersonelIds` [B1]؛ HTS 998,245 ردیف |
| D14 | `OpenOrderRequest_Event` | P2→**P3** | `تایید` | در HTS هم مرده: 67 ردیف، همه 1394/09 [A2] |
| D15 | Type دسته 1/2 در برابر 1040/1041 | P2→**P3** | `تایید` | DB جدید Type 1/2؛ مصرف‌کننده‌ای که 1040 بخواهد در جدید نیست |
| D16 | فیلدهای تامین‌کننده صفحه 65 | P2 | `تایید` | `Gnr.Supplier`: PartyId, Prefix, PostalCode, PhoneNumber, Fax, Email, DlCode [B1]؛ Grade/ServicesAndProducts/RelatedPersonName/آدرس نیست |
| D17 | `LeadTime.NonRoutineLeadTimeInDay` | P2 | `تایید` | `Entities/App/Pln/LeadTime.cs`: PartId, LeadTimeDay, Supplier, Comment |
| D18 | Completion_Time جدا | P2→**P3** | `تایید` | `CompletionMiladiDate` DateTime ساعت را نگه می‌دارد؛ عملاً هم‌ارز |
| D19 | `Inv_Part.CompanyNames` | P2→**P3** | `تایید` | فقط FK در `Inv.PartCompany`؛ در پروفایل‌ها ستونی برایش نیست |
| D20 | تایید فروش/پروژه سست‌تر | P2 | `تایید` | کنترلر L302–365: نقش + EngineeringAccept + !auto + !تکراری؛ نه VPIS نه تطبیق Expert/Manager/PM. توجه: با VPIS خالی (D36) افزودن گیت VPIS همه تاییدها را می‌بندد → ترتیب: D36 اول |
| D21 | گیت کامنت بدون ذینفع | P2 | `تایید` | `SaveComment` L178–211 |
| D22 | رنگ‌های گرید | P2 | `تایید` | `onRowAdded` همه ۸ پروفایل فقط `IsStop→table-danger` [`_summary.md`] |
| D23 | پیوست سه‌منبعی در یک گرید | P2→**P3** | `تایید` (`نامشخص`) | Edit لیست پیوست + VPIS می‌سازد؛ پیوست کالا (`Inv_Part_Attachment`) تست UI لازم |
| D24 | گیرندگان ایمیل → NotificationGroup | P2 | `تغییر` (بدتر) | گروه‌های `Sup.OpenOrderRequest.SupplyUnit` و `.Industrial` **۰ عضو** [B6] → هیچ‌کس اعلان گروهی نمی‌گیرد؛ لیست‌های هاردکد هم باقی‌اند (D43) |
| D25 | صفحه 71 دسترسی جدا | P2 | `تایید` | `SaveAttachment` L761 / `DeleteAttachment` L923 بدون ActionDisplayName و نقش |
| D26 | باگ دسترسی HTS 545 | P2 | `تایید` | یادداشت پورت؛ اقدامی تا D1 ندارد |
| D27 | enum Supplier دوگانه (1/2 vs 6/51) | P3 | `تایید` | `BuyCategoryItemSupplierEnum` / `LeadTimeSupplierEnum` |
| D28 | تعداد فعال | P2 | `تغییر` (ارقام جدید) | HTS 1,053 / جدید 1,398 (Δ 345)؛ ۱۴۷ فعال جدید بدون BuyCategoryItem [07 §9] |
| D29 | `Sup_BuyCategory_Company` | P3 | `تایید` | ۰ ردیف |
| D30 | تریگر Disabled | P3 | `تایید` | `is_disabled=1` [A5] |
| D31 | New تامین‌کننده | P3 | `تایید` | پروفایل 38 `new` فعال؛ HTS کامنت |
| D32 | الصاق خودکار مدارک / AddStaticPersons | P3 | `تغییر` | کد `HtsTaskService` کامنت است، ولی `Sup_OpenOrderRequestVpis` ردیف‌های «درج بصورت اتوماتیک توسط سیستم» (کاربر 1) تا 2026-09-16 دارد → مسیر خودکار دیگری در HTS فعال است (بررسی در WP7) |
| D33 | منوهای خارج از محدوده | P3 | `تایید` | — |

---

## 2. جزئیات D1–D33 (راه‌حل + تصمیم)

### D1. پیشینه ارسال به پیمانکاران — P0 — `تایید`
**راه‌حل پیشنهادی:** موجودیت `Entities/App/Sup/OpenOrderRequestEmailToSupplier.cs` (`[Table("OpenOrderRequestEmailToSupplier", Schema="Sup")]`: OpenOrderRequestId, SupplierId→Gnr.Supplier, SendShamsiDate/SendMiladiDateTime, AttachmentId→FileEntity?, PartAttachmentId?, EdmsDocumentId?, Comment, SendingCount decimal, **HtsId**) + migration. کنترلر `WebApp/Controllers/Dynamic/Sup/OpenOrderRequestEmailToSupplierController.cs` (List/FetchData/ExportToExcel + `ListByParentId`) و `Views/Panel/Sup/OpenOrderRequestEmailToSupplier/List.cshtml` با `<datatableprofile>`؛ پروفایل `vw_openOrderRequestEmailToSupplier` با ستون‌های صفحه 545 (شماره درخواست، حواله، کد/نام/واحد کالا، شرکت گیرنده، تعداد نیاز، SendingCount، تاریخ/ساعت، ارسال‌کننده) + seed RoleAccess (نقش جدید `Sup.OpenOrderRequest.SendHistoryView`). گرید فرزند در Edit درخواست باز (تب جدید) با `ListByParentId`. مهاجرت ۳۷٬۹۵۲ ردیف: بخش جدید در `Data/Scripts/SyncOpenOrderRequestFromTotalSystem.sql` (نگاشت Company_FK→Gnr.Party.HamkaranId→Supplier، CreatedUser_FK→Username) + جاب پل روزانه مثل Def 41 تا زمان خاموشی HTS. منو: «پیشینه ارسال به پیمانکاران» در منوی 3.  
**نیاز به تصمیم؟** **بله** — Q1 (دامنه موج فعلی).

### D2. ایمیل به پیمانکار + تامین‌کننده منتخب — P0 — `تایید`
**راه‌حل پیشنهادی:** روی پروفایل 99 دو دکمه سفارشی (`requiresMultiSelection`): «ارسال ایمیل به پیمانکار» → `$.confirm` با `_SendEmailToSupplierPartial` (انتخاب Supplier با EntitySelector، SendingCount، Comment، انتخاب پیوست از سه منبع) → `POST SendEmailToSupplier` (ثبت ردیف D1، `Changed=false`، `ManCompanyId`، Notification Type=Email با پیوست)؛ «تامین‌کننده منتخب» → `POST SetSelectedSupplier(ids[], supplierId)`. همان دکمه‌ها روی Edit (`addSupplier` را وصل کنید). هر دو اکشن با `[ActionDisplayName]` و چک نقش `Sup.OpenOrderRequest.SendEmail` (نقش جدید = Permission 27) / `ConfigManage`. وابسته به D40 (جاب ایمیل).  
**نیاز به تصمیم؟** **بله** — Q1.

### D3. صفحه تنظیمات — P1 — `تغییر`
**راه‌حل پیشنهادی:** پروفایل 99 را نگه دارید (روش صحیح ریپو). تکمیل: ستون‌های EmailSendToSupplier / DelaysBuyDay / CompletionShamsiDate / ManCompany (D41)، رنگ سبز/بنفش (D22)، انتخاب واحد سازمانی در `_AddRequestedPersonelPartial` (D47)، حذف `debugger` و `openOrderRequestId=1` (D49)، دکمه‌های D2. بدون منوی جدا (پروفایل داخل همان List) مگر Q7.  
**نیاز به تصمیم؟** خیر — رفتار HTS روشن است (منو: Q7).

### D4. صفحه پیشینه — P1 — `تغییر`
**راه‌حل پیشنهادی:** پروفایل 73 را نگه دارید؛ `delete` را غیرفعال کنید (D45)؛ نقش `Sup.OpenOrderRequest.HistoryView` + RoleAccess به `dataProfile_73` و List/FetchData/Edit (D39)؛ ستون‌های PartCodingTitle / RelatedPart / DelaysBuyDay / Completion / BuyProgress (D41)؛ در Edit وقتی `Model.IsDeleted` دکمه‌های عملیاتی خاموش.  
**نیاز به تصمیم؟** خیر.

### D5. LeadTime خالی — P1 — `تایید`
**راه‌حل پیشنهادی:** فیلد `HtsId` روی `Pln.LeadTime` + جاب `App.BackgroundJob/Jobs/Pln/LeadTimeJob.SyncLeadTimeFromHts` (الگوی Def 41: موجودیت آینه `Entities/Hts/Pln/Hts_Pln_LeadTime`، نگاشت PartId با `Inv.Part.HamkaranId`/Code، SupplierUnitId→enum 6/51) روزانه؛ یا اسکریپت یک‌باره `Data/Scripts/Import_LeadTime_FromTotalSystem.sql` روی linked server TMS.  
**نیاز به تصمیم؟** **بله** — Q3 (یک‌باره یا همگام مداوم).

### D6. ورود اکسل LeadTime — P1 — `تایید`
**راه‌حل پیشنهادی:** بدون کد جدید: یک `ImportDefinition` در `System/ImportDefinition` برای `Pln.LeadTime` با ستون‌های PartCode→PartId (lookup)، LeadTimeDay، Supplier، Comment؛ دکمه «ورود از اکسل» به‌عنوان اکشن سفارشی پروفایل LeadTime. اگر ImportDefinition نگاشت FK با کد را پشتیبانی نکند → اکشن `ImportExcel` در `LeadTimeController` با Aspose (الگوی ExportToExcel).  
**نیاز به تصمیم؟** خیر (فرمت HTS روشن: PartCode, LeadTime, SupplierId∈{6,51}, Comment).

### D7. جاب ۹:۳۵ توقف — P1 — `تایید`
**راه‌حل پیشنهادی:** `App.BackgroundJob/Jobs/Sup/OpenOrderRequestStopJob.cs` با سه `[JobHandler]`: (۱) `SendStopResponseDeadlineReminders` — کامنت‌های توقف باز با `ResponseDeadlineMiladiDate ≤ امروز` → Notification Type=Email به `StopOperatorId` + CC ذینفعان؛ (۲) `EscalateStaleStopsToSupply` — هدرهای با `StopStatus ∈ {2808,2810,2811,2812,2813}` که آخرین کامنت توقف > ۱۴ روز است → کامنت سیستمی + `StopStatus=2821` + اعلان تدارکات (گروه SupplyUnit)؛ (۳) `SendDailySupplyCommentDigest` — کامنت‌های امروز واحد تدارکات. JobDefinition + JobSchedule (ScheduleType=1، DailyTime 09:35). ۲۸۲۱ را مستقل از موفقیت ایمیل ثبت کنید (رفع ابهام #9 HTS).  
**نیاز به تصمیم؟** **بله** — Q6 (وابستگی 2821 به موفقیت ایمیل).

### D8. فیلتر دید گرید — P1 — `تغییر`
**راه‌حل پیشنهادی:** (۱) پروفایل 103 «مشاهده عمومی»: فیلتر `RequestedPersonelIds LIKE '%"@CurrentUserId"%'` (یا `RequestedEngineeringPersonelIds`) به‌جای `IS NOT NULL`؛ (۲) نقش 100000 را به پروفایل 102 (همه فعال) هم وصل کنید (مهندسی HTS همه فعال‌ها را می‌دید)؛ (۳) D34 (LEFT JOIN)؛ (۴) `FetchData` بدون پروفایل را برای نقش‌های غیرادمین بسته نگه دارید (RoleAccess فقط از مسیر پروفایل).  
**نیاز به تصمیم؟** خیر — ماتریس HTS روشن است.

### D9. چک نقش سرور — P1 — `تایید`
**راه‌حل پیشنهادی:** در `OpenOrderRequestController` برای `StopOperation`, `Triggering`, `SetInWayStatus`, `StatusInquiry`, `Terminate`, `DoLinkVpis`, `SaveAttachment`, `DeleteAttachment`, `AddRequestedPersonelToOpenRequest`, `SaveComment`: `[ActionDisplayName("…", ActionAccessType.Api)]` + `if (!CurrentUserHasAnyRole("Sup.OpenOrderRequest.Stop")) return Unauthorized(...)` (Start→Start، Sending→Sending، Query→Query، Terminate→Terminate، VPIS→HasEngineering|EngineeringAccept، پیوست→نقش‌های صفحه 71 = هر نقش عملیاتی، پرسنل→ConfigManage). در `Edit.cshtml` بسته `stopOpersions` را بشکنید: هر دکمه با فلگ خودش (`hasStartPermission`…). seed RoleAccess برای مسیرهای جدید در `Data/Scripts/Seed_Sup_OpenOrderRequest_RoleAccess.sql`.  
**نیاز به تصمیم؟** خیر — HTS هر Permission را جدا می‌دهد (Sending/Query فقط FullAccess).

### D10. List دسته = قلم — P1 — `تایید`
**راه‌حل پیشنهادی:** کنترلر `BuyCategoryController` واقعی روی `typeof(BuyCategory)` (List/Edit/FetchData/ExportToExcel) + `List.cshtml` با `<datatableprofile entity-Type="typeof(BuyCategory)">` (ستون‌ها: عنوان، متولی، نوع، lead time روتین/غیرروتین، تعداد قلم) + Edit موجود؛ اقلام به‌صورت `ListByParentId` با مودال `$.confirm`. منو «دسته های خرید» → `/panel/sup/buycategory/list`؛ پروفایل 40 برای «اقلام دسته» بماند. حق نوشتن وابسته به Q2.  
**نیاز به تصمیم؟** **بله** — Q2 (مرجع داده دسته: Rahkaran یا سیستم جدید).

### D11. جاب VPIS revision — P1 — `تایید`
**راه‌حل پیشنهادی:** ردیف `JobSchedule` برای Def 32 (روزانه، بعد از Def 48 `SyncProjectVpisFromHts` 07:00 → 07:30). بی‌فایده تا D36 حل شود.  
**نیاز به تصمیم؟** خیر.

### D12. جاب دسته خرید — P1 (فوری) — `تغییر`
**راه‌حل پیشنهادی:** `BuyCategoryJob.cs` L42–46: به‌جای `ToDictionaryAsync(x => x.HamkaranId, …)` → `GroupBy(x => x.HamkaranId).ToDictionary(g => g.Key, g => g.OrderByDescending(u => u.IsActive).ThenBy(u => u.Id).First().Id)` (همان الگو برای `partsDict` روی Code به‌صورت پیشگیرانه). سپس `UPDATE system.JobSchedule SET LastStatus=0, NextRunTime=GETDATE() WHERE JobDefinitionId=30`. جدا: پاک‌سازی ۵ کاربر تکراری (`Soleimani.a`/`soleimani.ali` …) در داده.  
**نیاز به تصمیم؟** خیر.

### D13. جدول فرزند Requested_Personel — P2 — `تایید`
**راه‌حل پیشنهادی (اگر انتخاب شود):** `Sup.OpenOrderRequestRequestedPersonel` (OpenOrderRequestId, UserId, UserCategory enum 2829/2830, HtsId) + مهاجرت ۹۹۸k ردیف + همگام‌سازی دوطرفه با ستون‌های دنرمال در `WebApp/Actions/Sup/OpenOrderRequestRequestedPersonelAction` (AfterSave/AfterDelete → بازنویسی `RequestedPersonel*`). پیشنهاد جایگزین: نگه‌داشتن مدل دنرمال فعلی.  
**نیاز به تصمیم؟** **بله** — Q4.

### D14. `Event` — P3 — `تایید`
**راه‌حل پیشنهادی:** اقدامی ندارد (در HTS مرده). Notification جایگزین است.  
**نیاز به تصمیم؟** خیر.

### D15. Type دسته 1/2 — P3 — `تایید`
**راه‌حل پیشنهادی:** فقط مستندسازی نگاشت 1040→1 / 1041→2 در `BuyCategoryTypeEnum` (کامنت).  
**نیاز به تصمیم؟** خیر.

### D16. فیلدهای تامین‌کننده HTS-only — P2 — `تایید`
**راه‌حل پیشنهادی (اگر انتخاب شود):** چهار فیلد `Grade`, `ServicesAndProducts`, `RelatedPersonName`, `Address` روی `Gnr.Supplier` + migration + Edit + پروفایل 38؛ کپی یک‌باره از `Gnr_ManCompany` با `Party.HamkaranId = Hamkaran_ManCompany_FK`.  
**نیاز به تصمیم؟** **بله** — Q5.

### D17. `NonRoutineLeadTimeInDay` — P2 — `تایید`
**راه‌حل پیشنهادی:** فیلد `int? NonRoutineLeadTimeInDay` روی `Pln.LeadTime` + migration + Edit (label «زمان در راه غیرروتین (روز)»)؛ مهاجرت با D5.  
**نیاز به تصمیم؟** خیر.

### D18. Completion_Time — P3 — `تایید`
**راه‌حل پیشنهادی:** اقدامی ندارد (ساعت در `CompletionMiladiDate`)؛ در پروفایل ستون «تاریخ و ساعت اختتام» با فرمت DateTime.  
**نیاز به تصمیم؟** خیر.

### D19. `CompanyNames` دنرمال — P3 — `تایید`
**راه‌حل پیشنهادی:** ستون محاسباتی در CustomQuery پروفایل‌ها: `STRING_AGG(Party.FullName, '، ')` از `Inv.PartCompany`→`Gnr.Supplier`→`Gnr.Party` به‌صورت زیرکوئری (بدون فیلد دنرمال).  
**نیاز به تصمیم؟** خیر.

### D20. تایید فروش/پروژه — P2 — `تایید`
**راه‌حل پیشنهادی:** در `SalesOrProjectAccept`: (۱) `if (!isAdmin && currentUserId ∉ {SalesUnitSalesExpertId, SalesUnitSalesManagerId, SalesUnitProjectManagerId}) return Unauthorized`؛ (۲) `if (!Vpis.Any(v => v.OpenOrderRequestId == id && v.IsLatest)) return BadRequest("ابتدا مدارک پروژه (VPIS) را لینک کنید")`. **فقط بعد از D36.**  
**نیاز به تصمیم؟** خیر — رفتار HTS روشن است.

### D21. گیت کامنت — P2 — `تایید`
**راه‌حل پیشنهادی:** در `SaveComment`: اگر `RequestedPersonelIds` و `RequestedEngineeringPersonelIds` خالی → `BadRequest("ابتدا درخواست‌کننده/ذینفع تعریف کنید")` (Admin مستثنی مثل HTS).  
**نیاز به تصمیم؟** خیر.

### D22. رنگ‌های گرید — P2 — `تایید`
**راه‌حل پیشنهادی:** `EventScriptsJson.onRowAdded` مشترک ۸ پروفایل: قرمز `t1_IsStop`، بنفش `t1_Changed`، نارنجی `t1_HasSalesUnitConfirmation`، آبی `|SupplyShamsiDate − امروز| ≤ 3 && !IsStop`، سبز `EmailSendToSupplier` (بعد از D1/D41). ترتیب اولویت رنگ مثل HTS (قرمز > بنفش > سبز > آبی > نارنجی).  
**نیاز به تصمیم؟** خیر.

### D23. پیوست سه‌منبعی — P3 — `تایید/نامشخص`
**راه‌حل پیشنهادی:** تست UI تب «مدیریت پیوست‌ها»؛ اگر پیوست کالا نمی‌آید، `Edit` VM را با `Inv.PartAttachment` (اگر موجود) تکمیل کنید.  
**نیاز به تصمیم؟** خیر.

### D24. گروه‌های اعلان — P2→**P1** — `تغییر`
**راه‌حل پیشنهادی:** `Data/Scripts/Seed_Sup_NotificationGroupMembers.sql`: `SupplyUnit` ← اعضای نقش 100010 Supply (۲۲) یا گروه HTS 27؛ `Industrial` ← اعضای نقش 100009 (۹) یا گروه HTS 28 (۲۰). سپس D43.  
**نیاز به تصمیم؟** خیر — منبع اعضا در HTS روشن است.

### D25. دسترسی پیوست — P2 — `تایید`
**راه‌حل پیشنهادی:** بخشی از D9: `[ActionDisplayName]` روی `SaveAttachment`/`DeleteAttachment`/`DownloadAttachment`؛ حذف فقط برای نقش‌های عملیاتی (Supply/Industrial/EngineeringAccept/ConfigManage)؛ دانلود برای همه نقش‌های دارای List.  
**نیاز به تصمیم؟** خیر.

### D26. باگ 545 — P2 — `تایید`
**راه‌حل پیشنهادی:** در D1 نقش مستقل `SendHistoryView` با RoleAccess خودش (نه وابسته به پیشینه 77).  
**نیاز به تصمیم؟** خیر.

### D27. enum دوگانه — P3 — `تایید`
**راه‌حل پیشنهادی:** اقدامی ندارد؛ در مستند نگاشت 1↔51؟ خیر — دو مفهوم مستقل‌اند (کانال قلم دسته از Rahkaran / واحد تامین LeadTime). فقط کامنت در enum.  
**نیاز به تصمیم؟** خیر.

### D28. تعداد فعال — P2 — `تغییر`
**راه‌حل پیشنهادی:** کوئری reconcile یک‌باره روی linked server: `PurchaseRequestItemId` فعال در جدید ∖ فعال در HTS؛ بررسی سه شاخه SP (state 5/6/9، بازگشت از رسید موقت 9979→9982، غایب در Rahkaran) در `ComputeOpenOrderRequest`. بعد از D34 (۱۴۷ ردیف بدون دسته ممکن است همان اختلاف باشد).  
**نیاز به تصمیم؟** خیر.

### D29–D31, D33 — P3 — `تایید`
**راه‌حل پیشنهادی:** D29 هیچ؛ D30 هیچ؛ D31 `new` را در پروفایل 38 غیرفعال کنید (تامین‌کننده از Def 18 می‌آید)؛ D33 هیچ.  
**نیاز به تصمیم؟** خیر.

### D32. درج خودکار VPIS — P3→**P2** — `تغییر`
**راه‌حل پیشنهادی:** در WP7 مسیر HTS که ردیف‌های «درج بصورت اتوماتیک توسط سیستم» را می‌زند پیدا شود (احتمالاً سرویس Edms هنگام لینک مدرک به پروژه) و در `CheckVpisRevisionChangesAndNotify` یا `WebApp/Actions/Edms/DocumentAction` (AfterSave) پورت شود.  
**نیاز به تصمیم؟** خیر (ابتدا واقعیت‌یابی).

---

## 3. مغایرت‌های جدید D34–D52

### D34. INNER JOIN دسته خرید در پروفایل‌ها — **P1**
همه ۸ پروفایل `Sup.OpenOrderRequest` روی `Sup.BuyCategoryItem.PartId` **INNER** join دارند [`db\new_dataprofiles\_summary.md` Relations]. HTS `Vw_Sup_OpenOrderRequest` LEFT OUTER می‌گیرد [`db\legacy_defs\Vw_Sup_OpenOrderRequest.sql.txt` L29–33]. زنده 2026-09-17: **۱۴۷ از ۱٬۳۹۸ درخواست فعال در هیچ لیستی نیستند**؛ ۳۹ درخواست (کالای چنددسته‌ای) تکراری؛ ۹٬۱۱۶ حذف‌شده هم در پیشینه نیستند. HTS با ایندکس یکتای `InvPart_Fk` یک دسته به ازای کالا داشت.  
**راه‌حل پیشنهادی:** در ۸ SavedQuery (32, 72, 73, 74, 99, 102, 103, 141) رابطه را LEFT کنید — بهتر: join مستقیم `Inv.Part.BuyCategoryId → Sup.BuyCategory` (ستون موجود، بدون تکرار) و حذف `BuyCategoryItem` از کوئری؛ اسکریپت `Data/Scripts/Fix_Sup_OpenOrderRequest_DataProfiles_Join.sql`. ایندکس یکتای `BuyCategoryItem.PartId` بعد از پاک‌سازی تکراری‌ها (WP5).  
**نیاز به تصمیم؟** خیر.

### D35. Delete فیزیکی و ایجاد دستی درخواست باز — **P1**
`Delete` (hard `DeleteAsync`) و `Add/New/Save` با `[ActionDisplayName]` در کاتالوگ و RoleAccess برای 100017/100000/100009/3 (+ `new` برای 100010/100011/100008/100001/100007) [`db\N04_roleaccess`]؛ `delete` در پروفایل‌های 32/72/73/74/103 فعال. HTS: نه Delete نه New روی 74 (فقط Terminate نرم؛ درخواست فقط از ERP).  
**راه‌حل پیشنهادی:** `delete`/`new` را در ActionOptions همه پروفایل‌ها غیرفعال کنید؛ RoleAccess مسیرهای `/delete`, `/add`, `/new`, `/save` را برای همه نقش‌ها جز 200052 حذف کنید (seed)؛ یا `Delete` را به `Terminate` (نرم) تبدیل کنید.  
**نیاز به تصمیم؟** خیر — HTS روشن است.

### D36. جدول VPIS خالی — **P1**
`Sup.OpenOrderRequestVpis` = **0** در برابر 2,859 در HTS (آخرین 2026-09-16، بخشی خودکار) [`04-db-truth` A8, B1]. بدون آن: جاب رویژن (D11) بی‌معنی، گیت VPIS فروش (D20) غیرقابل‌فعال‌سازی، تب مدارک Edit خالی.  
**راه‌حل پیشنهادی:** بخش VPIS `SyncOpenOrderRequestFromTotalSystem.sql` اجرا/اصلاح شود (نگاشت `Edms.Project/ProjectVpis/Document` با HtsId — Def 48 `SyncProjectVpisFromHts` پیش‌نیاز)؛ جاب پل `SyncOpenOrderRequestVpisFromHts` (الگوی Def 41) روزانه تا خاموشی HTS؛ کشف و پورت مسیر درج خودکار (D32).  
**نیاز به تصمیم؟** **بله** — Q8 (همگام مداوم یا یک‌باره).

### D37. lead time دسته‌های خرید همه NULL — **P1**
`Sup.BuyCategory.RoutineLeadTimeInDay / NonRoutineLeadTimeInDay`: ۰ از ۱۱۴ مقدار دارد؛ HTS مقادیر عملیاتی دارد (ابزار دقیق روتین 45/90، موتور 60/120، پکیج روغن 180/180 …) [`04-db-truth` A7, B1]. `CalculateDelaysBuyDay` (جاب L1352–1358) به این دو ستون تکیه می‌کند → `DelaysBuyDay` عملاً محاسبه نمی‌شود.  
**راه‌حل پیشنهادی:** `Data/Scripts/Backfill_BuyCategory_LeadTimes_FromTotalSystem.sql` (`UPDATE … FROM [TMS].TotalSystem.dbo.Sup_BuyCategory h ON h.BuyCategory_ID = BuyCategory.HamkaranId`)؛ در `BuyCategoryJob` این دو ستون (و `PurchaseResponsibleId` اگر Rahkaran ندارد) بازنویسی نشوند؛ نمایش/ویرایش در Edit والد (D10).  
**نیاز به تصمیم؟** خیر (Q2 فقط برای حق ویرایش آینده).

### D38. نگاشت اعضای گروه‌های HTS → نقش‌های جدید — **P1**
اعضا [`04-db-truth` A1.1 vs B4]: ShowAll 193→44، EngineeringAccept 93(+20+24)→25، SalesOrProjectAccept 9→1، Terminate 11→5، ConfigManage 21→14، Industrial 20→9، Supply/Admin 29+12→22+9؛ گرنت مستقیم ۲۸/۲۴/۵ کاربر روی 74 معادل ندارد.  
**راه‌حل پیشنهادی:** `Data/Scripts/Seed_Sup_RoleMembers_FromHts.sql`: جدول نگاشت (گروه HTS → نقش جدید) طبق `07 §6.2` + join `Gnr_User.UserName ↔ system.User.UserName` روی linked server → افزودن Id نقش به `system.User.RoleIds` (JSON) برای کاربران فعال؛ گزارش کاربران بی‌معادل. اجرا فقط بعد از تایید فهرست.  
**نیاز به تصمیم؟** خیر — نگاشت از HTS خوانده می‌شود (فهرست نهایی قبل از اجرا نشان داده می‌شود).

### D39. نقش «مشاهده» و «مشاهده پیشینه» وجود ندارد — **P1**
پروفایل 103 «مشاهده عمومی» فقط به Industrial/ShowAllMenus وصل است؛ پروفایل 73 «پیشینه» بدون نقش مشاهده. HTS: گروه 442 (۱۰۶)، 172 (۴۰)، 303، 33، 60 (۳۱)، 350 (۱۵) فقط Read 74؛ گروه 395 (۴۳)، 44 (۱۹۳)، 70 (۹۳)، 28 Read 77.  
**راه‌حل پیشنهادی:** دو نقش seed در `Entities/Auth/Role.cs` HasData: 100013 `Sup.OpenOrderRequest.View` «مشاهده درخواست‌های باز»، 100014 `Sup.OpenOrderRequest.HistoryView` «مشاهده پیشینه»؛ RoleAccess: View → list/fetchdata/edit + `dataProfile_103`؛ HistoryView → list/fetchdata/edit + `dataProfile_73` (+ 100000/100008/100009 هم به 73). اعضا از D38.  
**نیاز به تصمیم؟** خیر.

### D40. جاب ایمیل غیرفعال — **P1**
`System.EmailJob.SendPendingEmailsAsync` (Def 38) `IsActive=0` از 2026-08-16 [`db\N05`]. اعلان‌های Type=Email توقف (و هر ایمیل سیستم) ارسال نمی‌شود؛ HTS SMTP همزمان می‌زد.  
**راه‌حل پیشنهادی:** بررسی علت غیرفعال‌شدن (لاگ Def 38) + تنظیمات SMTP؛ `UPDATE system.JobSchedule SET IsActive=1` برای Def 38؛ پایش صف اعلان‌های ارسال‌نشده. پیش‌نیاز D2/D7.  
**نیاز به تصمیم؟** خیر.

### D41. ستون‌های گرید HTS غایب در پروفایل‌ها — P2
HaveAttachment، PurchaseCompleted/BuyProgress، ManCompany (تامین‌کننده منتخب)، EmailSendToSupplier/آخرین شرکت، آخرین کامنت تدارکات/صنایع/سایر، Requested_EngineeringPersonel، RelatedPart، DelaysBuyDay، Completion، SalesBranch، Part.CompanyNames، PartCodingTitle [`07 §3.3`]. برچسب غلط: «کامنت آخر» روی `OrderItemComment` (شرح قلم ERP). کامنت جدید `CreatedOrgUnit` ندارد → «آخرین کامنت به ازای واحد» باید با نقش نویسنده بازسازی شود.  
**راه‌حل پیشنهادی:** CustomQuery مشترک را با LEFT JOIN `Gnr.Supplier`+`Gnr.Party` (ManCompany)، زیرکوئری `EXISTS Attachment`، زیرکوئری آخرین `CommentValue` (کل / نویسنده با نقش Supply / نویسنده با نقش Industrial)، `Sale.ProductionOrder→SalesBranch`، `CASE FactoredCount>=RequiredQty` تکمیل کنید؛ برچسب‌ها اصلاح؛ EmailSendToSupplier بعد از D1. اسکریپت seed یک‌جا برای ۸ پروفایل.  
**نیاز به تصمیم؟** خیر.

### D42. نقش HasEngineering به دکمه VPIS وصل نیست — P2
`Edit.cshtml` L16–18: `linkVpis` با `isEngineering` (نقش EngineeringAccept) روشن می‌شود؛ نقش 100007 HasEngineering (۱۸ عضو) فقط در RoleAccess پروفایل‌ها استفاده شده؛ `DoLinkVpis` TODO. HTS: 82 مستقل از 12 (گروه 462 با ۵۱ عضو فقط 82 دارد).  
**راه‌حل پیشنهادی:** `var hasEngineeringPermission = isAdmin || sdk.HasRole("Sup.OpenOrderRequest.HasEngineering") || isEngineering;` برای `linkVpis`؛ سرور `DoLinkVpis` همان دو نقش.  
**نیاز به تصمیم؟** خیر.

### D43. لیست‌های ایمیل هاردکد باقی‌مانده — P2
`CreateSalesConfirmationNotification` (لیست `@havayar.com` تدارکات)، `CreateNewAttachmentNotification` (ایمیل‌های تولید)، نگاشت سرپرست در `CreateEngineeringAcceptNotification` [`03-havayar-inventory` §8].  
**راه‌حل پیشنهادی:** سه گروه seed در `Entities/Base/NotificationGroup.cs`: `Sup.OpenOrderRequest.SalesConfirmationRecipients`، `.ProductionAttachmentRecipients`، `.EngineeringSupervisors` + اعضا از لیست‌های HTS (`Seed_Sup_NotificationGroupMembers.sql`)؛ کنترلر از گروه بخواند.  
**نیاز به تصمیم؟** خیر — اعضا از کد HTS؛ بعداً در UI قابل ویرایش.

### D44. ایندکس یکتای درخواست باز — P2
HTS: unique (OrderRowId, PurchaseRequestItemId, PurchaseRequestNumber). جدید: فقط PK/FK [`04-db-truth` B1]. با upsert سه‌کلیدی جاب ریسک ردیف تکراری هست.  
**راه‌حل پیشنهادی:** بررسی تکراری‌ها (`GROUP BY … HAVING COUNT(*)>1`)، سپس migration `HasIndex(o => new { o.OrderRowId, o.PurchaseRequestItemId, o.PurchaseRequestNumber }).IsUnique()` (filtered برای NULL).  
**نیاز به تصمیم؟** خیر.

### D45. پروفایل پیشینه قابل ویرایش/حذف — P2
پروفایل 73 `edit`, `delete` فعال؛ Edit همان دکمه‌های عملیاتی را برای ردیف حذف‌شده هم روشن می‌کند. HTS 77 فقط‌خواندنی (+پیوست).  
**راه‌حل پیشنهادی:** `delete=false` در 73؛ در `initializePage()` اگر `openOrderRequest.isDeleted` → همه دکمه‌ها جز history/پیوست‌ها/کامنت‌ها خاموش.  
**نیاز به تصمیم؟** خیر.

### D46. مسیر منو برای کاربران مهندسی — P2 — `نامشخص`
منوی 1 «مهندسی» غیرفعال [`db\N02`]؛ «درخواست های باز» در منوهای 3/2/9. HTS منو را برای `SystemType.Eng` هم نشان می‌داد. ۲۶ عضو نقش 100000 فقط اگر عضو SupplierMenu/IndustriesMenu/Sell.Menu باشند لینک را می‌بینند.  
**راه‌حل پیشنهادی:** کوئری اعضای 100000 ∖ (400008 ∪ 400007 ∪ 200053 …)؛ افزودن نقش 100000/100007 به `AccessRoleIds` منوی 3 یا فعال‌کردن منوی 1 با آیتم درخواست‌های باز.  
**نیاز به تصمیم؟** خیر (بعد از واقعیت‌یابی).

### D47. انتخاب واحد سازمانی برای افزودن پرسنل — P2
Config HTS: چک‌باکس نفرات ثابت صنایع + انتخاب واحد سازمانی → افزودن همه پرسنل واحد. جدید: فقط لیست نقش 100012 در `_AddRequestedPersonelPartial`.  
**راه‌حل پیشنهادی:** EntitySelector واحد سازمانی (اگر موجودیت `Hrm.OrgUnit`/دپارتمان در جدید هست) که کاربران آن واحد را به `requestedPersonelIds` اضافه کند؛ در غیر این صورت انتخاب بر اساس نقش.  
**نیاز به تصمیم؟** خیر.

### D48. بازه جاب Rahkaran و خطاهای آن — P2
Def 31 هر ۶۰ دقیقه (HTS ۱۵ دقیقه، ۶–۲۱)؛ ۹ خطای «Incorrect syntax near ')'» (27 Jul–24 Aug) و ۱۱۰۶ خطای بی‌متن در ۶۰ روز [`db\N05`, `N07`].  
**راه‌حل پیشنهادی:** `IntervalSeconds=900`؛ رفع تولید `IN ()` خالی در کوئری Rahkaran؛ پیام خطا در `JobLog`.  
**نیاز به تصمیم؟** خیر — HTS ۱۵ دقیقه.

### D49. hygiene اسکریپت پروفایل‌ها — P3
`debugger` + `console.log` در دکمه «لیست کامنت ها» (۷ پروفایل)، `openOrderRequestId=1` هاردکد و `debugger` در دکمه پرسنل پروفایل 99 [`_summary.md`].  
**راه‌حل پیشنهادی:** به‌روزرسانی `CustomActionButtonsJson` همان ۸ ردیف در اسکریپت D41.  
**نیاز به تصمیم؟** خیر.

### D50. NULL در وضعیت‌ها — P3
`StopStatus` NULL ×1,848 (HTS default 0)؛ `StopCheckingStatus` کامنت‌های مهاجرت‌شده همه NULL (HTS 2816 ×21 یعنی تصمیم‌ها ثبت شده بود) [`04-db-truth` B1].  
**راه‌حل پیشنهادی:** `UPDATE … SET StopStatus=0 WHERE NULL`؛ بررسی نگاشت `StopCheckingStatusId` در بخش کامنت `SyncOpenOrderRequestFromTotalSystem.sql`.  
**نیاز به تصمیم؟** خیر.

### D51. وضعیت متنی «راه اندازی شده» — P3 — `اضافه در جدید`
HTS Start فقط `IsLaunch` کامنت را می‌زند؛ جدید `Status='راه اندازی شده'` هم می‌نویسد. بی‌خطر.  
**راه‌حل پیشنهادی:** نگه‌داشتن؛ فقط در مستند.  
**نیاز به تصمیم؟** خیر.

### D52. Permission 13 «تایید» و 26 «بازخوانی» — P3
13 در UI فعلی HTS دکمه‌ای ندارد (۳ گرنت مستقیم قدیمی)؛ 26 = رفرش گرید.  
**راه‌حل پیشنهادی:** هیچ.  
**نیاز به تصمیم؟** خیر.

---

## 4. هم‌ارزها (تایید مجدد 2026-09-17)

- موجودیت درخواست باز (۵۵ فیلد HTS → همه نگاشت دارند، `07 §2.1`)، کامنت، پیوست (+HtsId، جاب روزانه)، مدل VPIS
- ماشین توقف با همان IDها و مهلت ۲/۳ روز؛ تایید مهندسی دستی با چک نقش؛ منطق تایید خودکار مهندسی
- جاب Rahkaran + Compute (اختتام/حذف نرم/احیا/PO/SalesUnit/IsRoutine) + اعلان رسید/QC
- جاب‌های تامین‌کننده (Def 18)، کالا (Def 4)، پیوست HTS (Def 41)، PartCompany (Def 45)
- پروفایل‌های «تنظیمات» (99) و «پیشینه» (73) به‌عنوان مکانیزم صحیح (تکمیل لازم)
- نقش‌های seed 100000–100012 (اعضا ناقص: D38)؛ نفرات ثابت به‌جای هاردکد صنایع

---

## 5. برنامه اصلاح — بسته‌های کاری

| WP | عنوان | موارد | فایل‌های محتمل | وابستگی | تصمیم؟ | اندازه |
|---|---|---|---|---|---|---|
| **WP1** | پیشینه ارسال به پیمانکاران + ایمیل به پیمانکار + تامین‌کننده منتخب | D1, D2, D26, D22 (سبز), D41 (EmailSendToSupplier) | `Entities/App/Sup/OpenOrderRequestEmailToSupplier.cs`, migration, `Controllers/Dynamic/Sup/OpenOrderRequestEmailToSupplierController.cs`, `Views/Panel/Sup/OpenOrderRequestEmailToSupplier/List.cshtml`, `_SendEmailToSupplierPartial.cshtml`, `OpenOrderRequestController.SendEmailToSupplier/SetSelectedSupplier`, `Edit.cshtml`, SavedQuery 99 دکمه‌ها, `Data/Scripts/Seed_Sup_EmailToSupplier_*.sql`, `SyncOpenOrderRequestFromTotalSystem.sql`, جاب پل, `Role.cs` (SendEmail, SendHistoryView), منو 3 | WP8 (ایمیل), WP4 (نقش) | **بله** Q1 | **L** |
| **WP2** | هم‌ترازی پروفایل‌های تنظیمات/پیشینه/لیست | D3, D4, D8, D22, D34, D41, D45, D47, D49 | `system.SavedQuery` ×8 (QueryJson/CustomQuery/EventScripts/ActionOptions/Buttons) via `Data/Scripts/Seed_Sup_OpenOrderRequest_DataProfiles_v2.sql`, `_AddRequestedPersonelPartial.cshtml`, `Edit.cshtml` (isDeleted) | WP1 برای ستون/رنگ ایمیل | خیر (Q7 فقط منو) | **M** |
| **WP3** | جاب‌های توقف (مهلت، 2821، گزارش روزانه) | D7, D50 | `App.BackgroundJob/Jobs/Sup/OpenOrderRequestStopJob.cs`, seed JobDefinition/JobSchedule 09:35 | WP8 | **بله** Q6 (جزئی) | **M** |
| **WP4** | سخت‌سازی دسترسی سرور + RoleAccess + اعضای نقش + نقش‌های مشاهده | D9, D20 (هویت), D21, D25, D35, D38, D39, D42, D46, D52 | `OpenOrderRequestController.cs`, `Edit.cshtml`, `Entities/Auth/Role.cs`, `Data/Scripts/Seed_Sup_OpenOrderRequest_RoleAccess.sql`, `Seed_Sup_RoleMembers_FromHts.sql`, `system.SystemMenu.AccessRoleIds` | — | خیر (فهرست اعضا پیش از اجرا نمایش داده می‌شود) | **M–L** |
| **WP5** | دسته‌های خرید: رفع جاب، لیست/Edit والد، lead time، یکتایی قلم | D10, D12, D15, D27, D34 (ایندکس), D37 | `App.BackgroundJob/Jobs/Sup/BuyCategoryJob.cs`, `JobSchedule` reset, `Controllers/Dynamic/Sup/BuyCategoryController.cs` (والد), `Views/Panel/Sup/BuyCategory/List.cshtml`, پروفایل جدید, `Backfill_BuyCategory_LeadTimes_FromTotalSystem.sql`, migration ایندکس | — | **بله** Q2 (حق ویرایش) — رفع جاب و backfill بدون تصمیم | **M** |
| **WP6** | زمان در راه: همگام/ورود + غیرروتین | D5, D6, D17 | `Entities/App/Pln/LeadTime.cs`, migration, `Entities/Hts/Pln/Hts_Pln_LeadTime.cs`, `App.BackgroundJob/Jobs/Pln/LeadTimeJob.cs`, ImportDefinition, `Views/Panel/Pln/LeadTime/Edit.cshtml` | — | **بله** Q3 | **S–M** |
| **WP7** | VPIS: مهاجرت، جاب پل، زمان‌بندی رویژن، گیت فروش/پروژه | D36, D11, D20, D32, D23 | `SyncOpenOrderRequestFromTotalSystem.sql` (VPIS), `OpenOrderRequestJob.SyncOpenOrderRequestVpisFromHts`, `JobSchedule` Def 32, `OpenOrderRequestController.SalesOrProjectAccept`, بررسی منبع درج خودکار HTS | Def 48 ProjectVpis | **بله** Q8 | **M** |
| **WP8** | اعلان‌ها: فعال‌سازی جاب ایمیل، اعضای گروه‌ها، حذف هاردکد | D40, D24, D43 | `JobSchedule` Def 38, تنظیمات SMTP, `Entities/Base/NotificationGroup.cs`, `Data/Scripts/Seed_Sup_NotificationGroupMembers.sql`, `OpenOrderRequestController` (Create*Notification) | — | خیر | **S–M** |
| **WP9** | شکاف‌های فیلدی و داده‌ای | D13, D16, D18, D19, D28, D44, D48, D51 | `Entities/App/Sup/*`, `Gnr/Supplier.cs`, migration, `OpenOrderRequestJob.cs` (IN خالی), `JobSchedule` 31, کوئری reconcile | WP2 | **بله** Q4, Q5 | **M** |

ترتیب پیشنهادی اجرا (کم‌ریسک → پرریسک): WP5 (رفع جاب + backfill) → WP8 → WP2 → WP4 → WP7 → WP3 → WP6 → WP1 → WP9.

---

## 6. سوالات تصمیم (فقط انتخاب‌های کسب‌وکاری)

| # | سوال | گزینه‌ها | مرتبط |
|---|---|---|---|
| **Q1** | «پیشینه ارسال به پیمانکاران» و ارسال ایمیل به پیمانکار در موج فعلی؟ | (a) پورت کامل: موجودیت + صفحه + ارسال از پروفایل تنظیمات + مهاجرت ۳۷٬۹۵۲ ردیف + جاب پل **(پیشنهاد)** · (b) فقط تاریخچه فقط‌خواندنی (مهاجرت + صفحه)، ارسال در موج بعد · (c) تعویق کامل | D1, D2, WP1 |
| **Q2** | مرجع داده «دسته‌های خرید / اقلام دسته» از این به بعد؟ | (a) Rahkaran USR3 مرجع؛ جدید فقط‌خواندنی (وضع فعلی) · (b) سیستم جدید مرجع: CRUD مثل صفحات 70/72 و توقف جاب Def 30 · (c) ترکیبی: دسته/قلم از Rahkaran، اما متولی خرید و lead time روتین/غیرروتین در جدید ویرایش‌پذیر و جاب آن‌ها را بازنویسی نکند **(پیشنهاد)** | D10, D37, WP5 |
| **Q3** | منبع «زمان در راه» (۳٬۲۰۲ ردیف HTS)؟ | (a) کپی یک‌باره از HTS، سپس نگهداری در جدید (CRUD + ورود اکسل) **(پیشنهاد)** · (b) همگام روزانه از HTS تا خاموشی HTS (جدید فقط‌خواندنی) · (c) خالی بماند؛ فقط lead time دسته استفاده شود | D5, D6, WP6 |
| **Q4** | جدول فرزند «درخواست‌کنندگان/ذینفعان» با تاریخچه (مثل HTS، ۹۹۸k ردیف)؟ | (a) مدل دنرمال فعلی (`RequestedPersonelIds`) بماند؛ تاریخچه از Entity History **(پیشنهاد)** · (b) موجودیت فرزند `OpenOrderRequestRequestedPersonel` + مهاجرت | D13, WP9 |
| **Q5** | فیلدهای HTS-only تامین‌کننده (گرید، خدمات/محصولات، شخص مرتبط، آدرس)؟ | (a) افزودن به `Gnr.Supplier` + کپی یک‌باره از `Gnr_ManCompany` **(پیشنهاد اگر تدارکات هنوز استفاده می‌کند)** · (b) حذف از دامنه | D16, WP9 |
| **Q6** | ارسال خودکار به کارتابل تدارکات (2821) بعد از ۱۴ روز — مستقل از موفقیت ایمیل؟ | (a) وضعیت همیشه ثبت شود؛ ایمیل/اعلان جدا **(پیشنهاد)** · (b) عین HTS: فقط اگر ایمیل موفق بود | D7, WP3 |
| **Q7** | آیتم منو برای تنظیمات / پیشینه / پیشینه ارسال؟ | (a) یک آیتم «درخواست‌های باز»؛ پروفایل داخل صفحه انتخاب می‌شود (قرارداد ریپو) **(پیشنهاد)** · (b) سه آیتم منوی جدا (مثل HTS) که همان List را با پروفایل پیش‌فرض باز کنند (نیاز به پشتیبانی پارامتر پروفایل در List) | D3, D4, D1, WP2 |
| **Q8** | داده VPIS (۲٬۸۵۹ ردیف؛ HTS هنوز روزانه درج خودکار دارد)؟ | (a) جاب پل روزانه از HTS تا خاموشی + پورت درج خودکار در جدید **(پیشنهاد)** · (b) مهاجرت یک‌باره و از آن پس فقط لینک دستی در جدید | D36, D32, WP7 |

بعد از پاسخ به Q1–Q8، بسته‌های WP5/WP8/WP2/WP4 (بدون وابستگی به تصمیم) می‌توانند فوراً شروع شوند.

---

## 7. WP5 — انجام‌شده / باقی‌مانده (2026-09-17؛ بازبینی پس از Q2)

**تصمیم Q2 = (a)** (`08-Decisions-2026-09-17.md`): راهکاران مرجع کامل است؛ در سامانه جدید دسته‌های خرید، اقلام، متولی خرید و lead time **فقط‌خواندنی** هستند و جاب ساعتی Def 30 برقرار می‌ماند. پیاده‌سازی مطابق آن اصلاح شد (بندهای «صفحه والد» و «ویوها» زیر). یادداشت: 08-Decisions می‌گوید backfill از راهکاران؛ چون راهکاران برای همهٴ ۱۱۲ دسته NULL می‌دهد (همان علت D37)، طبق دستور بعدی مالک محصول backfill یک‌بارهٴ HTS نگه داشته شد — فقط شکاف NULL پر می‌شود و با فقط‌خواندنی بودن تناقض ندارد.

### انجام‌شده (کد — build سبز: `App.BackgroundJob`, `WebApp`)

| مورد | فایل | تغییر |
|---|---|---|
| D12 جاب دسته خرید | `App.BackgroundJob/Jobs/Sup/BuyCategoryJob.cs` | `usersDict`/`partsDict` با `GroupBy` (برنده: کاربر/کالای فعال، سپس کم‌ترین Id) + لاگ warning فهرست HamkaranId/کد تکراری؛ پردازش هر دسته و هر قلم در try/catch جدا (لاگ + ادامه، detach ردیف ناموفق از change tracker)؛ `Part.BuyCategoryId` با انتخاب قطعی یک قلم به ازای کالا (D34) |
| D37 بازنویسی lead time | همان فایل | قاعده «NULL راهکاران مقدار موجود را پاک نمی‌کند» برای `RoutineLeadTimeInDay` / `NonRoutineLeadTimeInDay` / `PurchaseResponsibleId` / `Type` (فقط مقدار غیر NULL راهکاران بازنویسی می‌کند) → backfill یک‌بارهٴ HTS پایدار می‌ماند. `Title` مثل قبل همیشه از راهکاران |
| D12 (هم‌کلاس) | `App.BackgroundJob/Jobs/Sup/InquiryPartPriceJob.cs` | همان گارد `GroupBy` برای `ToDictionaryAsync(HamkaranId)` کالا/کاربر، `Username` و `PriceUnit.Title` (دو متد) |
| D10 صفحه والد (Q2=a) | `WebApp/Controllers/Dynamic/Sup/BuyCategoryController.cs` | کلاس جدید `BuyCategoryController` (`typeof(BuyCategory)`, `/panel/sup/buycategory/*`): List/FetchData/ExportToExcel + `Edit` = صفحهٴ **نمایش** فقط‌خواندنی (Include اقلام + کالا). `Save/Add/Update/Delete/New` دفاعی مانده‌اند و همیشه `isSuccess=false` با پیام «دسته‌های خرید از راهکاران همگام می‌شوند و در این سامانه قابل ویرایش نیستند» برمی‌گردانند. `BuyCategoryItemController` → عنوان «اقلام دسته های خرید»، List آن به `Views/Panel/Sup/BuyCategoryItem/List.cshtml`، و اکشن‌های نوشتن آن هم همیشه `isSuccess=false` |
| D10 ویوها (Q2=a) | `Views/Panel/Sup/BuyCategory/List.cshtml` (والد)، `Views/Panel/Sup/BuyCategoryItem/List.cshtml` (اقلام، جدید)، `Views/Panel/Sup/BuyCategory/Edit.cshtml` | Edit والد = صفحهٴ نمایش: عنوان، نوع، متولی خرید، **lead time روتین + غیرروتین** همه `readonly disabled`؛ فقط دکمهٴ «بستن» (بدون Save/SaveAndNew/SaveAndClose، بدون اسکریپت ذخیره)؛ نشان «همگام از راهکاران — کد …» + «فقط‌خواندنی — ویرایش در راهکاران»؛ اقلام به‌صورت جدول فقط‌خواندنی (کد/نام کالا، زمان در راه، تامین‌کننده، توضیحات) تا `MaxInlineItems=300` قلم، بالاتر ارجاع به «اقلام دسته های خرید». `_BuyCategoryItemPartial` دیگر در این صفحه استفاده نمی‌شود |
| D15 / D27 | `Entities/App/Sup/Enums/BuyCategoryTypeEnum.cs`, `BuyCategoryItemSupplierEnum.cs` | کامنت نگاشت 1040→1 / 1041→2 و استقلال از `LeadTimeSupplierEnum` (6/51) |

### انجام‌شده (SQL — فقط فایل؛ هیچ‌کدام اجرا نشده)

ترتیب اجرا (روی HavayarApp):

1. `Data/Scripts/Report_Party_DuplicateHamkaranId.sql` — فقط SELECT: ۵ زوج کاربر با HamkaranId مشترک + برنده‌ای که جاب انتخاب می‌کند (ایندکس یکتا عمداً اضافه نشد).
2. *(بعد از deploy کد جدید)* `Data/Scripts/Reset_JobSchedule_SyncBuyCategoryJob.sql` — `LastStatus=Idle`, `NextRunTime=GETDATE()` برای Def 30 (تطبیق با `MethodName`).
3. `Data/Scripts/Backfill_BuyCategory_LeadTime_FromHts.sql` — پرکردن یک‌بارهٴ شکاف (هدر توضیح Q2=a دارد): `@DryRun=1` پیش‌فرض (پیش‌نمایش + شمارش قبل/بعد + ROLLBACK)؛ تطبیق `HamkaranId = [TMS].TotalSystem.dbo.Sup_BuyCategory.BuyCategory_ID` و به‌صورت پیش‌فرض فقط ردیف‌های با عنوان یکسان (`@RequireTitleMatch=1`)؛ فقط NULL ها (`@OverwriteExisting=0`). می‌تواند قبل یا بعد از ۲ اجرا شود (جاب اصلاح‌شده مقدار را پاک نمی‌کند).
4. `Data/Scripts/Seed_BuyCategory_DataProfiles.sql` — نمایه `BuyCategory_List` (عنوان، نوع، متولی، lead time روتین/غیرروتین، تعداد اقلام، کد راهکاران، آخرین به‌روزرسانی)؛ ActionOptions: **new/edit/delete غیرفعال**، فقط اکسل + دکمه‌های سفارشی «مشاهده دسته» (→ صفحه نمایش) و «اقلام دسته های خرید»؛ RoleAccess نمایه + مسیرهای **فقط** list/fetchdata/exporttoexcel/edit برای نقش‌هایی که `/panel/sup/buycategoryitem/list` دارند (100017, 200052)؛ پاک‌سازی هر ردیف save/update/add/new/delete دسته و اقلام؛ قفل نمایهٴ موجود اقلام (`buycategoryitem_listinfo`) به new/edit/delete=false. بعد از اجرا: restart WebApp (کش نمایه + کاتالوگ دسترسی کنترلر جدید).
5. `Data/Scripts/Seed_BuyCategory_Menu.sql` — منوی «تامین و خرید»: برگ موجود → «اقلام دسته های خرید»، برگ جدید «دسته های خرید» → `/panel/sup/buycategory/list` درست قبل از آن.

### باقی‌مانده

| مورد | چرا |
|---|---|
| D34 ایندکس یکتای `BuyCategoryItem.PartId` + migration | ۳۹ درخواست فعال با کالای چنددسته‌ای باید ابتدا پاک‌سازی شوند (تصمیم داده‌ای)؛ جاب فعلاً با انتخاب قطعی (کم‌ترین Id قلم) دور می‌زند |
| اقلام دسته‌های >300 قلم در صفحه نمایش والد | برای دسته‌های بزرگ (Flange-Fitting 2,461، لوازم برقی 1,729 …) جدول رندر نمی‌شود و به لیست «اقلام دسته های خرید» ارجاع می‌دهد؛ اگر لازم شد، `ListByParentId` فقط‌خواندنی با DataTable سمت سرور |
| پاک‌سازی ۵ کاربر تکراری (Soleimani.a/soleimani.ali، …) | تصمیم تیم داده؛ گزارش آماده است |
| CRUD دسته/اقلام در پنل | **بسته شد** (Q2=a): همهٴ اکشن‌های نوشتن دفاعی `isSuccess=false`؛ هیچ RoleAccess نوشتنی seed نمی‌شود |
| اجرای SQL ها و تست UI روی سرور | طبق دستور اجرا نشد؛ منتظر تایید |

---

## 8. WP3 — انجام‌شده / باقی‌مانده (2026-09-17) — جاب‌های روزانه توقف (HTS 09:35)

تصمیم‌های اعمال‌شده: **Q6-a** (2821 همیشه ثبت می‌شود، مستقل از اعلان/ایمیل) · **Q10** (جاب ایمیل Def 38 تا go-live غیرفعال می‌ماند؛ جاب فقط ردیف `system.Notification` ثبت می‌کند — اعلان درون‌برنامه‌ای فوری، ایمیل از همان صف `System.EmailJob.SendPendingEmailsAsync` وقتی فعال شود؛ هیچ SMTP مستقیم).  
منبع HTS: `Services\EntityServices\Hts.EntityServices\Common\Supplies\OpenOrderRequestCommentService.cs` (L193–300 یادآوری، L305–453 تایم‌اوت ۱۴ روز) که از `HtsTaskService.SupplysSystemTask` (L194–197, 642–656؛ `Hour==9 && Minute==35 && Second==1`) صدا می‌شد.

### فایل‌ها

| فایل | نوع | توضیح |
|---|---|---|
| `App.BackgroundJob/Jobs/Sup/OpenOrderRequestStopJob.cs` | **جدید** | کلاس جاب با دو `[JobHandler]`: `SendStopDeadlineReminders` و `EscalateStaleStops`. DI خودکار (`AddJobServices()` هر کلاس دارای `[JobHandler]` را Scoped ثبت می‌کند) و `JobDefinition` خودکار در استارت (`JobDiscoveryService`) — **هیچ تغییری در `Program.cs` لازم نبود**. الگو: `IUnitOfWork.Repository<T>().AddAsync/UpdateAsync` (Actions/audit اجرا می‌شوند)، `Notification` مثل `OpenOrderRequestJob`، گروه اعلان از `dbContext.NotificationGroupMembers` |
| `Data/Scripts/Seed_OpenOrderRequestStopJobs_Schedule.sql` | **جدید** (اجرا نشده) | idempotent: درج `JobDefinition` اگر نبود (AssemblyName از ردیف `OpenOrderRequestJob` موجود) + درج `JobSchedule` فقط اگر برای آن تعریف ردیفی نباشد؛ عین ردیف Def 41 (`ScheduleType=1`, `IntervalSeconds=86400`, `DailyIntervalDays=1`, `HourlyMinute=0`, `LastStatus=0`, `IsActive=1`, `NextRunTime` = اولین وقوع آینده). یادآوری **09:35**، ارسال به تدارکات **09:36** (یک دقیقه بعد، مانند الگوی 07:00/07:01 ردیف‌های 41/45). دستور غیرفعال‌سازی در کامنت ابتدای فایل |
| `Specs/SuppliesOpenOrder/99-Discrepancies.md` | ویرایش | این بخش |

بدون ستون/موجودیت/migration جدید: تکراری‌گیری روزانه با جدول `system.Notification` انجام می‌شود (نه فیلد `LastReminderDate`). `WebApp\Controllers\Dynamic\Sup\OpenOrderRequestController.cs`، `WebApp\Views\Panel\Sup\*` و `BuyCategoryJob.cs` دست نخورده‌اند.

### قاعده ۱ — `SendStopDeadlineReminders` (روزانه 09:35)

| | پیاده‌سازی | HTS |
|---|---|---|
| **شرط** | درخواست `IsDeleted=0 ∧ IsForceDeletedByUser=0 ∧ IsStop=1 ∧ StopStatus ∈ {2807, 2816}` (در انتظار بررسی عامل توقف) که آخرین کامنت توقف آن (`IsStop=1`, بیشترین Id — همان قاعده `StopOperation`) `StopOperatorId` دارد، `StopCheckingResult` ندارد و **مهلت پاسخ ≤ امروز** است. مهلت = `ResponseDeadlineMiladiDate`؛ اگر خالی (کنترلر برای «ثبت توقف مجدد» 2816 مهلت نمی‌نویسد) = تاریخ کامنت + ۲ روز روتین / ۳ روز غیرروتین (همان قاعده کنترلر) | کامنت `IsStop ∧ StopOperatorId ∧ ResponseDeadlineDate == امروز ∧ StopCheckingResultId IS NULL ∧ !IsDeleted` — **فقط روز مهلت**، بدون شرط وضعیت هدر |
| **اقدام** | یک ردیف `Notification` برای عامل توقف: `Type=Email`, `OwnerId=عامل`, `ToEmails=[ایمیل عامل]`, `CcEmails=[ایمیل ثبت‌کننده توقف]`, `EntityId=Id درخواست`, `ViewPath=/Panel/Sup/OpenOrderRequest/Edit?id={id}&stopReminder={commentId}` + یک ردیف درون‌برنامه‌ای (`Type=Appliaction`) برای ثبت‌کننده توقف تا در کارتابل ببیند (ایمیل او در CC است؛ ایمیل تکراری نمی‌رود). اگر کاربر ایمیل ندارد فقط درون‌برنامه‌ای (ردیف خطادار در صف ایمیل نمی‌ماند). عامل توقف ناموجود/غیرفعال ⇒ warning در لاگ + فقط ثبت‌کننده | `SendEmail(To=عامل توقف, CC=ایجادکننده)` |
| **گیرندگان** | To: عامل توقف (`StopOperatorId`) · CC/درون‌برنامه‌ای: ثبت‌کننده توقف (`CreatedById` کامنت) | همان |
| **تکراری‌گیری** | حداکثر یک بار در روز به ازای هر کامنت توقف: وجود `Notification` با `Title` یادآوری، `EntityId` درخواست، `ViewPath` حاوی `stopReminder={commentId}` و `CreatedOnMiladiDateTime ≥ امروز` ⇒ رد می‌شود؛ اجرای مجدد همان روز چیزی نمی‌سازد | — (فقط یک روز اجرا می‌شد) |
| **عنوان** | «هشدار پایان مهلت پاسخگویی به توقف درخواست خرید» | همان |
| **متن** | «با سلام و احترام / کاربر گرامی، لیست زیر نمایانگر درخواست هایی است که توقفی جهت آنها صادر شده و **مهلت پاسخگویی به آنها فرا رسیده است** و تا این لحظه اقدامی از جانب شما صورت نگردیده است» + شماره درخواست خرید، شماره سفارش، کد کالا، عنوان کالا، ایجادکننده، توضیحات (`CommentValue`) + **مهلت پاسخ** (افزوده) — همان قالب جدول «گروه صنعتی هوایار» (Zar, #000aa0). مقادیر آزاد HtmlEncode می‌شوند | همان متن |

> تفاوت آگاهانه با HTS: یادآوری از روز مهلت **هر روز** تا اقدام عامل توقف تکرار می‌شود (یک بار در روز). برای برگشت به رفتار HTS (فقط روز مهلت) شرط `deadline.Value.Date > today` در `SendStopDeadlineReminders` به `deadline.Value.Date != today` تغییر کند — تصمیم محصول.

### قاعده ۲ — `EscalateStaleStops` (روزانه 09:36)

| | پیاده‌سازی | HTS |
|---|---|---|
| **شرط** | درخواست `IsDeleted=0 ∧ IsForceDeletedByUser=0 ∧ IsStop=1 ∧ StopStatus ∈ {2808, 2810, 2811, 2812, 2813}` (عامل توقف نتیجه را اعلام کرده، گام بعدی — تصمیم ثبت‌کننده / ادامه پس از مدیر پروژه — انجام نشده) و `تاریخ مرجع + 14 روز < امروز`؛ تاریخ مرجع = `StopCheckingMiladiDateTime` آخرین کامنت توقف، در نبود آن `ModifiedDateMiladiDateTime` → `MiladiDate` → `CreatedOnMiladiDateTime`. درخواست بدون کامنت توقف: warning و رد (گام بعدی کنترلر هم بدون کامنت خطا می‌دهد) | کامنت `IsStop ∧ StopOperatorId ∧ StopCheckingResultId ∈ {2808..2813} ∧ AddDays(UpdatedDate,14) < امروز` (توجه: `StopCheckingResultId` به Lookup 347 = 2799–2803 اشاره می‌کند ولی با مقادیر 2808–2813 از Lookup 349 مقایسه می‌شود؛ با شمارش `StopStatusId=2821` = ۰ در TotalSystem [`04-db-truth` A3] به نظر می‌رسد این مسیر در HTS عملاً هرگز اجرا نشده — استنتاج، نه شواهد مستقیم از داده کامنت‌ها) |
| **اقدام ۱ (همیشه — Q6)** | در یک `SaveChanges`: `OpenOrderRequest.StopStatus = 2821` (`UpdateAsync` با audit/Actions) + کامنت توقف جدید (`AddAsync`) مانند مسیر «ادامه روند توقف» کنترلر: `IsStop=1`, `CommentValue` = «ارسال خودکار درخواست توقف به کارتابل واحد تدارکات توسط مدیر سیستم (بعلت عدم بررسی در زمان مشخص)»، انتقال `StopType / StopOperatorId / BeneficiariesIds+Names / StopCheckingResult + تاریخ + توضیحات / BOM flags / تاریخ تقریبی مدیر پروژه` از کامنت جاری تا برای گام بعدی (تصمیم تدارکات در 2821) «کامنت توقف جاری» باشد، `AdditionalDescription` = «تغییر خودکار وضعیت توقف از «…» به «ارسال به کارتابل تدارکات…» پس از بیش از 14 روز بدون اقدام»، `CreatedById=1` / `CreatedByName=مدیر سیستم`. خطا ⇒ detach همان ردیف‌ها، ادامه با درخواست بعدی | فقط اگر ایمیل موفق: `StopCheckingResultId=2821` (فیلد نادرست) + `StopCheckingComment` + هدر `StopStatusId=2821` |
| **اقدام ۲ (اعلان)** | یک ردیف `Notification` به ازای هر گیرنده فعال (`Type=Email` با `ToEmails=[ایمیل]`؛ بدون ایمیل ⇒ درون‌برنامه‌ای)، `EntityId` درخواست، `ViewPath=/Panel/Sup/OpenOrderRequest/Edit?id={id}`. خطای اعلان وضعیت را برنمی‌گرداند (warning) | ایمیل To=عامل، CC=ایجادکننده+عامل |
| **گیرندگان** | اعضای فعال گروه اعلان `Sup.OpenOrderRequest.SupplyUnit` (Id 1 — **۰ عضو فعلاً، D24/WP8**؛ warning در لاگ) ∪ متولی خرید دسته کالا (`Part.BuyCategory.PurchaseResponsibleId`) ∪ ثبت‌کننده توقف (`CreatedById` کامنت) ∪ عامل توقف (`StopOperatorId`) | عامل توقف + ایجادکننده |
| **ایدم‌پوتنت** | پس از ثبت 2821 درخواست دیگر در شرط انتخاب نیست؛ اجرای مجدد بی‌اثر | — |
| **عنوان** | «ارسال خودکار توقف درخواست خرید به کارتابل تدارکات (عدم بررسی در زمان مشخص)» (در HTS همان عنوان یادآوری بود؛ برای تمایز کارتابل و تکراری‌گیری جدا شد) | «هشدار پایان مهلت پاسخگویی به توقف درخواست خرید» |
| **متن** | «با سلام و احترام / کاربر گرامی، لیست زیر نمایانگر درخواست هایی است که توسط عامل توقف ({نام عامل}) وبیش از 2 هفته است که تعیین تکلیف شده ولی اقدامی جهت ادامه روند آن صورت نپذیرفته است. لذا توقف مذکور به صورت اتوماتيك از كارتابل عامل توقف خارج شده و به كارتابل تامين و خريد، تحت عنوان 'درخواست بررسی حذف' ارسال مي گردد.» + شماره درخواست خرید، شماره سفارش (برچسب HTS اشتباهاً «شماره درخواست خرید» بود — اصلاح شد)، کد کالا، عنوان کالا، ایجادکننده، توضیحات، **نتیجه بررسی عامل توقف**، آخرین وضعیت قبلی، وضعیت جدید | همان متن |

### زمان‌بندی و راه‌اندازی

1. deploy نسخه جدید `App.BackgroundJob` و یک بار اجرا (JobDiscoveryService دو `JobDefinition` با JobId `App.BackgroundJob.Jobs.Sup.OpenOrderRequestStopJob.SendStopDeadlineReminders` / `…EscalateStaleStops` می‌سازد).
2. اجرای `Data/Scripts/Seed_OpenOrderRequestStopJobs_Schedule.sql` روی HavayarApp (اگر قبل از deploy اجرا شود و سرویس قدیمی ری‌استارت شود، JobDiscoveryService تعریف‌های بی‌متد را همراه schedule حذف می‌کند — ترتیب مهم است). SELECT پایانی اسکریپت دو ردیف زمان‌بندی را نشان می‌دهد.
3. تا فعال‌شدن Def 38 (`SendPendingEmailsAsync`) فقط اعلان درون‌برنامه‌ای دیده می‌شود؛ ردیف‌های `Type=Email` با `IsSend=0` در صف می‌مانند و بعد از فعال‌سازی ارسال می‌شوند (Q10).
4. غیرفعال‌سازی: `UPDATE system.JobSchedule SET IsActive=0` برای دو JobId بالا (متن کامل در سر اسکریپت).

### قاعده ۳ — `SendUnsentDispatchDigest` (روزانه 09:37) — follow-up 2026-09-17

| | پیاده‌سازی | HTS |
|---|---|---|
| **شرط** | کامنت `IsStop=0` با `MiladiDate == امروز` روی درخواست `IsDeleted=0 ∧ IsForceDeletedByUser=0`؛ یک کامنت آخر به ازای هر درخواست | همان + `CreatedOrgUnit_FK == 51` (تدارکات) |
| **فرض** | فیلد واحد سازمانی روی کامنت جدید نیست — فیلتر واحد اعمال نمی‌شود (همهٴ کامنت‌های غیرتوقف امروز) | واحد ۵۱ |
| **گیرندگان** | گروه `Sup.OpenOrderRequest.UnsentDispatchDigest`؛ اگر خالی ⇒ `SupplyUnit` + warning | To=`Dordab.y` CC=`Yaltaghian.f;Bagheri.h;Shahpordeli.s;Sohrabi.za;Sharifi.b` |
| **تکراری‌گیری** | یک بار در روز: `Title` + `ViewPath=...List?digest=unsentDispatch&date=yyyyMMdd` | — |
| **متن** | همان مقدمه HTS + ul شماره درخواست/سفارش، کد/عنوان کالا، شرکت (`ManCompany.Party.FullName`)، تعداد درخواست/خریداری، تاریخ تامین، توضیحات | همان |

یادآوری مهلت اکنون **فقط روز مهلت** است (`RemindOnDeadlineDayOnly = true`). برای تکرار روزانه تا اقدام عامل، همان ثابت را `false` کنید.

### باقی‌مانده / نیاز به تصمیم

| مورد | چرا |
|---|---|
| اعضای گروه `SupplyUnit` / `UnsentDispatchDigest` | تا اجرای seed WP8 اعلان‌ها فقط به متولی/ثبت‌کننده/عامل (و دایجست هیچ‌کس) می‌رسد |
| 2813 تاریخ تقریبی مدیر پروژه | مطابق HTS در نظر گرفته نمی‌شود |
| اجرا روی DB | seed زمان‌بندی **پس از استقرار** App.BackgroundJob |

---

## 8. WP6 — «زمان در راه» (HTS صفحه 414) — انجام‌شده / باقی‌مانده (2026-09-17)

تصمیم‌ها: **Q3-a** (کپی یک‌باره ۳٬۲۰۲ ردیف HTS، سپس نگهداری در جدید با CRUD + ورود اکسل) · **Q9** اجرای SQL توسط مالک محصول. مبنا: کد HTS `Areas/SupplayersSystem/Controllers/LeadTimeController.cs` + `_LeadTime.cshtml` + `Pln_LeadTime.cs`.

### انجام‌شده (کد — `dotnet build WebApp` سبز، 0 خطا / 0 هشدار)

| مورد | فایل | تغییر |
|---|---|---|
| D17 فیلد غیرروتین | `Entities/App/Pln/LeadTime.cs` | `int? NonRoutineLeadTimeInDay` با `[DisplayInfo]` «زمان در راه غیرروتین (روز)» (فیلدهای قبلی دست‌نخورده) |
| D17 migration | `Data/Migrations/ApplicationDb/20260917194320_AddLeadTimeNonRoutineLeadTime.cs` | **ساخته شد** (`.\add-migration.ps1`)؛ فقط `AddColumn NonRoutineLeadTimeInDay int NULL` روی `Pln.LeadTime` — بدون تغییر ناخواسته مدل. **اعمال نشده** (`update-database` اجرا نشد). |
| D17 فرم | `WebApp/Views/Panel/Pln/LeadTime/Edit.cshtml` | فیلد جدید با همان ماسک عددی؛ همه wrapperها `form-group-inline`؛ رفع `$(this.element)` → `$($btnAction.element)` در `savefn` (دکمه block نمی‌شد) |
| D27 | `Entities/App/Pln/Enums/LeadTimeSupplierEnum.cs` | فقط کامنت: 6/51 = شناسه `HRM_OrgUnit` در HTS، مستقل از `BuyCategoryItemSupplierEnum` (1/2) |

### انجام‌شده (SQL — فقط فایل؛ هیچ‌کدام اجرا نشده) — ترتیب اجرا روی HavayarApp

1. `.\update-database.ps1` (migration بالا) **یا** `Data/Scripts/Add_LeadTime_NonRoutineLeadTimeInDay.sql` (idempotent؛ اگر به‌جای migration اجرا شد `@RegisterEfMigration = 1` تا ردیف `20260917194320_AddLeadTimeNonRoutineLeadTime` در `__EFMigrationsHistory` ثبت شود).
2. `Data/Scripts/Seed_LeadTime_ImportDefinition.sql` — تعریف ورود اکسل (بخش «روش ورود اکسل» زیر) + RoleAccess.
3. `Data/Scripts/Seed_LeadTime_DataProfile.sql` — نمایه `leadtime_listinfo` «زمان در راه» با ستون‌های صفحه 414 (کد کالا، نام کالا، دسته خرید از `Inv.Part.BuyCategoryId`، زمان در راه (روز)، **زمان در راه غیرروتین (روز)**، تامین کننده (enum)، ایجاد کننده، تاریخ ایجاد، کامنت)؛ new/edit/delete/exportExcell فعال؛ دکمه سفارشی «ورود از اکسل» → `appController.addPage('/panel/importData/list')`. اگر قبلاً نمایه‌ای برای `entities.app.pln.leadtime` وجود داشته باشد همان ردیف UPDATE می‌شود (بدون تکرار). RoleAccess فقط ردیف‌های غایب: `SupplyAndPurchase`, `Sup.OpenOrderRequest.Supply`, `SupplierMenu`, `ShowAllMenus`. بعد از اجرا: restart WebApp / پاک‌کردن `DataTableProfileCacheDb`.
4. `Data/Scripts/Migrate_LeadTime_FromHts.sql` — ابتدا با `@DryRun = 1` (پیش‌فرض: شمارش‌ها + پیش‌نمایش INSERT/UPDATE + فهرست بی‌تطبیق‌ها + کاربران بدون معادل، بدون تغییر)، سپس `@DryRun = 0` (تراکنش واحد). Idempotent با upsert روی `(PartId, Supplier)`.

### جدول نگاشت مهاجرت (`[TMS].TotalSystem.dbo.Pln_LeadTime` → `Pln.LeadTime`)

| HTS | جدید | نگاشت |
|---|---|---|
| `PartId` (→ `Inv_Part`) | `PartId` | `Inv.Part.HtsId = Pln_LeadTime.PartId` → fallback `Inv.Part.HamkaranId = COALESCE(Inv_Part.Hamkaran_Part_FK, RahkaranPartId)` (یک کالا به ازای HamkaranId: فعال، سپس کم‌ترین Id) → fallback `Inv.Part.Code = Inv_Part.Part_Code`؛ بی‌تطبیق → رد + گزارش (کد/نام کالا) |
| `LeadTimeInDay` | `LeadTimeDay` | عیناً (مقادیر ≤0 گزارش می‌شوند ولی کپی می‌شوند) |
| `NonRoutineLeadTimeInDay` | `NonRoutineLeadTimeInDay` | عیناً |
| `LeadTimeInMinute` (computed) | — | کپی نمی‌شود |
| `SupplierUnitId` (6 / 51) | `Supplier` (`LeadTimeSupplierEnum` 6 / 51) | بدون تبدیل؛ مقدار دیگر → رد + گزارش |
| `Comment` | `Comment` | trim؛ خالی → NULL |
| `CreatedUserId` (→ `Gnr_User`) | `CreatedById` / `CreatedByName` | `Gnr_User.Username = system.User.Username` → fallback `LOWER(ActiveDirectoryUsername) = LOWER(Username)`؛ بی‌تطبیق → `CreatedById = NULL`, `CreatedByName = Gnr_User.FullName` |
| `CreatedDate` / `CreatedDateInText` | `CreatedOnMiladiDateTime` / `CreatedOnShamsiDateTime` (+ Modified* همان) | عیناً |
| — | `IsActive` | 1 |
| چند ردیف برای یک (کالا، تامین‌کننده) | یک ردیف | جدیدترین (`CreatedDate`, `Id`) برنده؛ تعداد ادغام‌شده در خروجی |

### روش ورود اکسل (D6) — ImportDefinition موجود، بدون کد جدید

ابزار عمومی `System/ImportDefinition` (حالت SQL، `ImportExecutor` هر ردیف اکسل را با پارامترهای `@<ColumnName>` + متغیرهای سیستمی اجرا می‌کند و خطا را به ازای ردیف لاگ می‌کند) دقیقاً نیاز HTS را پوشش می‌داد، پس به‌جای اکشن `ImportExcel` سفارشی، یک تعریف seed شد (`FullNameEntity = Entities.App.Pln.LeadTime` → در صفحه نقش زیر کنترلر «زمان در راه» دیده می‌شود):

| ستون تعریف | عنوان اکسل | نوع | الزامی | اعتبارسنجی (در `SqlQuery` هر ردیف) |
|---|---|---|---|---|
| `PartCode` | کد کالا | String | ✔ | `Inv.Part.Code` موجود و فعال، وگرنه «کد کالا «…» یافت نشد یا غیرفعال است» (= HTS) |
| `LeadTime` | زمان در راه (روز) | Int | ✔ | عدد صحیح > 0 |
| `NonRoutineLeadTime` | زمان در راه غیرروتین (روز) | Int | — | اگر پر باشد > 0 (ستون جدید D17) |
| `SupplierId` | تامین کننده | Select (enum سیستمی `LeadTimeSupplierEnum`) | ✔ | 6 / 51 یا عنوان فارسی؛ `ConvertSelectValue` + چک صریح `IN (6,51)` (= HTS) |
| `Comment` | کامنت | String | — | trim |

رفتار روی ردیف موجود: کلید `(PartId, Supplier)` → **UPDATE** (`LeadTimeDay` بازنویسی؛ `NonRoutine`/`Comment` فقط اگر در اکسل پر باشند) وگرنه INSERT — تفاوت آگاهانه با HTS (HTS ردیف «کالا+تامین‌کننده+زمان» تکراری را خطا می‌داد و در غیر این صورت ردیف دوم درج می‌کرد؛ کل فایل all-or-nothing بود). all-or-nothing در جدید با گزینه «عدم بارگذاری رکوردها در صورت خطا» صفحه ورود در دسترس است. دسترسی: `importData_<Id>` (Type 4) + `/panel/importdata/list` (View) برای `SupplyAndPurchase`, `ShowAllMenus` (HTS: گروه 291 + FullAccess). فایل‌های قدیمی HTS با سرستون انگلیسی (`PartCode, LeadTime, SupplierId, Comment`) در مرحله «مپ کردن ستون‌ها» نگاشت می‌شوند؛ نمونه اکسل جدید از همان صفحه دانلود می‌شود.

### باقی‌مانده

| مورد | چرا |
|---|---|
| نقش‌های مستقل «کارشناسان زمان در راه» (HTS گروه 291، ۶ کاربر: New/Edit/Delete/Import) و «مشاهده زمان در راه» (524، ۸ کاربر) + نگاشت اعضا | نیاز به seed در `Entities/Auth/Role.cs` و اسکریپت اعضا → **WP4 / D38**؛ فعلاً نمایه به نقش‌های تامین و خرید/منو و ورود اکسل به `SupplyAndPurchase`/`ShowAllMenus` داده شد |
| پیش‌انتخاب تعریف در صفحه ورود اطلاعات از دکمه «ورود از اکسل» | `ImportDataController.List()` (در `WebFramework`، خارج از محدوده WP6) پارامتر `definitionId` ندارد؛ `SelectedDefinitionId` در VM هست — یک تغییر کوچک در WebFramework |
| اعتبارسنجی سرور در `LeadTimeController.Save` (کالا/تامین‌کننده الزامی، `LeadTimeDay > 0`، یکتایی `(PartId, Supplier)`) | HTS هم فقط اعتبارسنجی کلاینت داشت؛ پیشنهاد: `WebApp/Actions/Pln/LeadTimeAction` (BeforeSave) در موج بعد |
| `HtsId` روی `Pln.LeadTime` | با Q3-a (کپی یک‌باره + upsert روی کلید طبیعی) لازم نیست؛ اگر بعداً جاب پل خواسته شد اضافه شود |
| اجرای migration/SQL ها و تست UI (نمایه، دکمه ورود از اکسل، فایل نمونه) | طبق دستور اجرا نشد (Q9) |

---

## 9. WP4 — چک نقش سرور + RoleAccess + اعضای نقش + نقش‌های مشاهده — انجام‌شده / باقی‌مانده (2026-09-17)

مبنا: کد HTS `OpenOrderRequestManagementController.cs` (`PermissionAuthorize`, `HasStopStartPermissions`, `HasStatusPermissions`, `DoAttachmentOperation`) + `_OpenOrderRequestManagement.cshtml` (فعال‌سازی دکمه‌ها L440–545) + `PermissionAuthorizeAttribute.cs` + `04-db-truth` A1.1/B4. هیچ SQL اجرا نشده؛ `dotnet build WebApp` سبز (0 خطا).

### انجام‌شده (کد)

| مورد | فایل | تغییر |
|---|---|---|
| D9, D25, D42, D20 (هویت), D21 | `WebApp/Controllers/Dynamic/Sup/OpenOrderRequestController.cs` | `[ActionDisplayName]` روی ۱۴ اکشن کاربرپذیر که خارج کاتالوگ بودند (`SaveComment`, `EngineeringAccept`, `SalesOrProjectAccept`, `StopOperation`, `Triggering`, `SetInWayStatus`, `StatusInquiry`, `Terminate`, `SaveAttachment`, `DeleteAttachment`, `DownloadAttachment`, `AttachmentListPartial`, `DoLinkVpis`, `AddRequestedPersonelToOpenRequest`). چک نقش با همان الگوی قبلی (`CurrentUserHasAnyRole`) در helper `HasOpenOrderRequestPermission(params roles)` = `IsAdministrator || CurrentUserHasAnyRole` (میان‌بر ادمین مثل `isAdmin || sdk.HasRole` در Edit و `AppSdk.hasRole`). عدم دسترسی → `AccessDenied(msg)` = `JsonResult(ApiResult(false, UnAuthorized, msg))` با HTTP 403 (به‌جای `Unauthorized(string)` که text/plain 401 می‌داد). چهار TODO (`Triggering`, `SetInWayStatus`, `Terminate`, `DoLinkVpis`) برداشته شد. `StopOperation`: قاعده مرحله‌ای HTS (`CheckStopOperationAccess`): 0/2815 → نقش Stop؛ 2807/2813/2816 → فقط `StopOperatorId` آخرین کامنت توقف؛ 2808/2810/2811/2812/2821 → فقط `CreatedById` آخرین کامنت توقف؛ ادمین همه. `SalesOrProjectAccept`: + تطبیق هویت (کارشناس/مدیر فروش/مدیر پروژه همان درخواست؛ ادمین مستثنی — عین JS HTS). `SaveComment`: + گیت D21 «درخواست‌کننده/ذینفع خالی» (پیام HTS). |
| D35 | همان | `New` → غیرادمین `RedirectToAction(List)`؛ `Add`/`Save`/`Update` → فقط ادمین (`AccessDenied`) — هیچ فراخوان UI ندارند (Edit بدون save، پروفایل‌ها `new` خاموش). `Delete` (مسیر دکمه «حذف» گرید پروفایل‌ها) → **خاتمه نرم** (`SoftTerminateAsync` مشترک با `Terminate`: IsDeleted + IsForceDeletedByUser + کامنت «اختتام یافته در Portal توسط …») با همان نقش Terminate؛ حذف فیزیکی دیگر وجود ندارد. |
| D25 | همان | `BuildAttachmentListItemsAsync`: `CanDelete = CanManageAttachments` (دکمه حذف پیوست در `_AttachmentListPartial` فقط برای نقش‌های صفحه 71) |
| D9 UI, D42 | `WebApp/Views/Panel/Sup/OpenOrderRequest/Edit.cshtml` | بسته `stopOpersions` حذف شد؛ هر دکمه با فلگ خودش (Razor `isAdmin || sdk.HasRole(...)` → `permissions`): `stopRequest` ← `canOperateStop` (همان قاعده مرحله‌ای سرور، محاسبه در Razor از `Model.StopStatus` + آخرین کامنت توقف)؛ `triggeringRequest` ← Start؛ `inWayStatus` ← Sending؛ `statusInquiry` ← Query؛ `terminateRequest` ← Terminate؛ `linkVpis` ← HasEngineering ∨ EngineeringAccept ∨ SupplyAndPurchase؛ `engineeringAccept` ← EngineeringAccept ∧ !accepted ∧ !isStop؛ `salesOrProjectAccept` ← SalesOrProjectAccept ∧ هویت ∧ accepted ∧ !auto ∧ !confirmed؛ `addComment` / دکمه تب کامنت ← `canComment`؛ `addRequestedPersonel` / `addSupplier` ← ConfigManage ∨ Industrial ∨ SupplyAndPurchase؛ دکمه‌های «افزودن پیوست» و «افزودن کامنت» تب‌ها فقط با مجوز رندر می‌شوند. کاربر فقط‌مشاهده: همه دکمه‌ها جز `history` خاموش. دو `debugger` حذف شد؛ گارد پاسخ غیر HTML در `reloadAttachmentsList`. |

### ماتریس نهایی HTS PageAction → اکشن جدید → نقش (سرور + RoleAccess)

| HTS (Page·Permission) | اکشن / مسیر جدید | نقش‌ها (RoleAccess + چک کنترلر) |
|---|---|---|
| 74·Read(3) | `list`, `fetchdata`, `edit`(View) | V = SupplyAndPurchase, EngineeringAccept, SalesOrProjectAccept, Stop, Start, Sending, Query, Terminate, HasEngineering, ShowAll, Industrial, Supply, ConfigManage, **View**, **HistoryView**, Industries(3), ShowAllMenus |
| 74·ExportToExcel(10) | `exporttoexcel` | SupplyAndPurchase, EngineeringAccept, Industrial, Supply, ShowAll, View, HistoryView, Industries |
| 71·Read/ViewAttachment | `attachmentlistpartial`, `downloadattachment` | V |
| 74·Edit(5) = ثبت کامنت | `savecomment` | SupplyAndPurchase, Supply, EngineeringAccept, Industrial(+Industries) — Industrial چون در RoleAccess فعلی Edit/Update داشت |
| 74·Accept_Engineering(12) | `engineeringaccept/{id}` | EngineeringAccept (HTS: WithoutCheckFullAccess → FullAccess شامل نیست) |
| 74·SalesOrProjectPermission(187) | `salesorprojectaccept` | SalesOrProjectAccept + هویت (Expert/Manager/PM همان درخواست) |
| 74·Stop(22) + DoStopOperation | `stopoperation` | RoleAccess: V؛ کنترلر: ثبت اولیه = Stop ∨ SupplyAndPurchase؛ بررسی = عامل توقف؛ تصمیم = ثبت‌کننده آخرین کامنت توقف؛ ادمین همه |
| 74·Start(23) | `triggering/{id}` | Start, SupplyAndPurchase |
| 74·Sending(24) | `setinwaystatus/{id}` | Sending, SupplyAndPurchase (HTS: فقط FullAccess گروه 17) |
| 74·Query(25) | `statusinquiry/{id}` | Query, SupplyAndPurchase |
| 74·Terminate(101) | `terminate/{id}`, `delete` (خاتمه نرم) | Terminate, SupplyAndPurchase |
| 74·HasEngineeringPermission(82) | `dolinkvpis` | HasEngineering, EngineeringAccept, SupplyAndPurchase |
| 71·New/Edit/Delete (FullAccess 71) | `saveattachment`, `deleteattachment` | SupplyAndPurchase, Supply, EngineeringAccept |
| 76·FullAccess | `addrequestedpersoneltoopenrequest` | ConfigManage, Industrial(+Industries), SupplyAndPurchase |
| 74·Read فقط‌مشاهده (442, 172, 303, 33, 60, 350, 462, 593 …) | نقش جدید **100013 `Sup.OpenOrderRequest.View`** | list/fetchdata/edit/exporttoexcel/پیوست + `dataProfile` «مشاهده عمومی» (103) |
| 77·Read (395, 44, 70, 28, 303, 33, 60, 350) | نقش جدید **100014 `Sup.OpenOrderRequest.HistoryView`** | list/fetchdata/edit/exporttoexcel/پیوست + `dataProfile` «پیشینه درخواست ها» (73)؛ + 73 برای EngineeringAccept و ShowAll (D39) |
| (هیچ) New/Delete | `/new`, `/add`, `/save`, `/update`, `/delete` | RoleAccess برای همه نقش‌ها جز ShowAllMenus حذف؛ `/delete` فقط Terminate/SupplyAndPurchase (نرم) |
| 74·Accept(13), Refresh(26) | — | بی‌اثر (D52) |

### انجام‌شده (SQL — فقط فایل؛ اجرا نشده) — ترتیب اجرا روی HavayarApp

1. `Data/Scripts/Seed_OpenOrderRequest_RoleAccess.sql` — نقش‌های 100013/100014 (IDENTITY_INSERT، همان بازه `Role.cs`)؛ پاک‌سازی D35؛ ردیف‌های RoleAccess ماتریس بالا (join روی `system.Role.Name`، idempotent)؛ دسترسی پروفایل‌های 103/73. گزارش نقش/پروفایل غایب. **سپس restart WebApp** (کاتالوگ دسترسی + کش پروفایل).
2. `Data/Scripts/Seed_OpenOrderRequest_RoleMembers_FromHts.sql` — `@DryRun = 1` پیش‌فرض: از `[TMS].TotalSystem.dbo.Vw_Permission` (System 13، صفحات 74/76/77، مستقیم + گروهی) + اعضای فعلی `Gnr_UserGroupMember`؛ نگاشت Permission→نقش (جدول سرآیند فایل؛ «هر مجوز 74 جز NoAccess → View»، 77 → HistoryView، 76 Full → ConfigManage) + گروه 27 → Supply، 28 → Industrial؛ تطبیق `COALESCE(ActiveDirectoryUsername, Username)` ↔ `system.User.Username` (+ نام جایگزین)؛ PRINT شمارش هر نقش (current/add/already/missing/inactive) + فهرست کامل برای بازبینی. `@DryRun = 0` فقط **اضافه** می‌کند (`RoleIds`/`Roles` JSON)؛ هیچ عضویتی حذف نمی‌شود. کاربران باید دوباره لاگین کنند.

### باقی‌مانده / نیاز به تصمیم

| مورد | چرا |
|---|---|
| ثبت نقش‌های 100013/100014 در `Entities/Auth/Role.cs` (HasData) | نیاز به migration؛ WP4 فقط SQL خواست — بعد از تایید IDها یک migration `HasData` اضافه شود (وگرنه در محیط تازه فقط با SQL ساخته می‌شوند) |
| D46 مسیر منو برای کاربران مهندسی / View / HistoryView (نقش‌های 100000/100007/100013/100014 در `AccessRoleIds` منوی 3 یا فعال‌کردن منوی 1) | تصمیم منو؛ عمداً در seed اعضا نقش‌های منو (SupplierMenu …) داده نشد |
| پیوست برای QC (گروه 33، FullAccess 71) و ConfigManage/Industrial (پیشنهاد D25) | طبق گرنت‌های HTS فقط 17/27/70 روی 71 FullAccess داشتند → SupplyAndPurchase/Supply/EngineeringAccept؛ اگر صنایع باید پیوست بزند، دو ردیف RoleAccess + `CanManageAttachments` اضافه شود |
| نقش‌های «زمان در راه» (HTS 291/524) و 100012 نفرات ثابت | خارج از صفحات 74/76/77؛ نگاشت اعضا ندارد |
| `Industries` (نقش 3 قدیمی، ۱ عضو) | مثل Industrial رفتار شد (ردیف‌های حذف/new پاک، savecomment/stopoperation/پرسنل اضافه)؛ پیشنهاد: ادغام در 100009 |
| D45 خاموش‌کردن دکمه‌ها برای ردیف `IsDeleted` و غیرفعال‌کردن `delete`/`new` در `ActionOptions` پروفایل‌ها | WP2 (پروفایل‌ها)؛ حالا `delete` گرید = خاتمه نرم و فقط برای Terminate کار می‌کند |
| `SaveComment` برای نقش‌های فقط‌مشاهده | عمداً بسته (HTS Edit(5) لازم داشت) — اگر کسب‌وکار بخواهد، View را به فهرست `savecomment` اضافه کنید |
| گیت VPIS در `SalesOrProjectAccept` (D20 بخش دوم) | WP7 (جدول VPIS خالی) |
| اجرای SQL ها، restart، تست UI با کاربر هر نقش | طبق دستور اجرا نشد؛ منتظر تایید |

---

## 10. WP8 — اعلان‌ها: گروه‌ها + اعضا + حذف هاردکد جاب (2026-09-17)

تصمیم Q10-b: جاب ایمیل (Def 38) **غیرفعال می‌ماند**؛ فقط اعضا پر می‌شوند و گیرندگان از `NotificationGroup` خوانده می‌شوند.

### انجام‌شده

| فایل | تغییر |
|---|---|
| `Data/Scripts/Seed_OpenOrderRequest_NotificationGroups.sql` | گروه‌های ArrivalWarehouse / ArrivalPartial / QcRejection / UnsentDispatchDigest (+ اطمینان از وجود SupplyUnit و Industrial) |
| `Data/Scripts/Seed_OpenOrderRequest_NotificationGroupMembers.sql` | `@DryRun=1`؛ نگاشت Username → User؛ بدون حذف عضویت موجود |
| `App.BackgroundJob/Jobs/Sup/OpenOrderRequestJob.cs` | آرایه‌های `ArrivalExtraEmails` و `QcRejectionExtraEmails` حذف؛ بارگذاری از گروه. `GetPairedExtraEmails` (جفت asadpour/khodabandeh/vahedian) باقی ماند چون وابسته به درخواست است نه لیست ثابت |

### رویداد → گروه

| رویداد | گروه |
|---|---|
| رسید کامل انبار | `ArrivalWarehouse` + جفت‌های درخواستی |
| رسید قسمتی | `ArrivalPartial` |
| عدم تایید QC | `QcRejection` |
| اسکالیشن 2821 | `SupplyUnit` (+ متولی/ثبت‌کننده/عامل) |
| دایجست برگ ارسال‌نخورده | `UnsentDispatchDigest` (fallback `SupplyUnit`) |
| اعلان صنایع در جاب | `Industrial` (از قبل) |

### باقی‌مانده در کنترلر (خارج از دامنه این بسته)

| محل | موضوع |
|---|---|
| `OpenOrderRequestController.cs` ~L1450 | لیست هاردکد تایید فروش/پروژه (`Dordab.y` … `sarmadi.p`) |
| همان ~L1837 | لیست هاردکد پیوست به تولید (`Sohrabi.z`, `golestaneh.a`, `Eftekhari.a`, `Estaki.a`) |
| همان ~L1420 | دیکشنری سرپرست (rajablou→yaltaghian و …) |

### ترتیب SQL

1. `Seed_OpenOrderRequest_NotificationGroups.sql`
2. `Seed_OpenOrderRequest_NotificationGroupMembers.sql` (`@DryRun=1` سپس `0`)
3. جاب ایمیل را فعال نکنید (Q10)

---

## 12. WP2 — انجام‌شده / باقی‌مانده (2026-09-17) — هم‌ترازی نمایه‌های درخواست باز

تصمیم **Q7**: یک List (`/panel/sup/openorderrequest/list`)؛ تنظیمات/پیشینه پروفایل داخل همان صفحه (منوی جدا نیست). SQL فقط فایل؛ روی DB اجرا نشد. JSON با `ConvertFrom-Json` در `%TEMP%\havayar-wp2-oor-json` معتبر شد.

### فایل‌ها

| فایل | نوع |
|---|---|
| `Data/Scripts/Update_OpenOrderRequest_DataProfiles_HtsParity.sql` | اصلاح جراحی — `@DryRun=1`؛ upsert با Name؛ RoleAccess rebuild با `system.Role.Name`؛ hygiene = `IsActive=0` نه DELETE |
| `Data/Scripts/Verify_OpenOrderRequest_DataProfiles.sql` | جدید، فقط SELECT |

### نقص‌های رفع‌شده در اسکریپت

- D8: «مشاهده عمومی» دیگر `RequestedPersonel IS NOT NULL` نیست → `OPENJSON(RequestedPersonelIds / RequestedEngineeringPersonelIds) = @CurrentUserId` (لیست `List<long>` به‌صورت `[1,2,3]` ذخیره می‌شود؛ LIKE با کوتیشن کار نمی‌کرد)
- D34: بدون `INNER JOIN BuyCategoryItem`؛ `OUTER APPLY TOP(1)` + `COALESCE(..., Part.BuyCategoryId)`
- D45: `delete=false` روی پیشینه (و همه نمایه‌ها)؛ `new=false`
- D22: رنگ HTS همان ترتیب if صفحه 74 (Changed بنفش، Stop قرمز، تایید فروش نارنجی سلول دوم، پنجره تامین آبی `[Supply−۳روز ، Supply]` آخرین نوشتن)؛ ایمیل سبز = `-- TODO WP1`
- D41: ستون‌های صفحه 74/76/77 در CustomQuery/ColumnsJson؛ Address ستون‌های محاسباتی با SELECT یکی شد؛ placeholder ایمیل = `CROSS APPLY [tEml]`
- `DiagramJson = {}` (نه `[]`)؛ `ISJSON` روی Columns/Events/Actions/Buttons/QueryJson
- پیشینه: `ManCompanyVisible=1` (ستون تامین‌کننده منتخب صفحه 77)

### خلاصه هر نمایه

| Name | عنوان | فیلتر | اکشن | رنگ | نقش |
|---|---|---|---|---|---|
| `openorderrequest_listinfo` | تمامی درخواست ها | — | edit+اکسل | کامل 74 | ShowAllMenus, SupplyAndPurchase |
| `vw_OpenOrderRequestAllActive` | همه فعال | IsDeleted=0 | edit+اکسل | کامل 74 | ShowAll, Industrial, Industries, EngineeringAccept, HasEngineering, SupplyAndPurchase, ShowAllMenus |
| `openorderrequest_listinfonew` | منتظر تایید مهندسی | فعال ∧ !EngineeringAccept | edit+اکسل | کامل 74 | EngineeringAccept, Industrial, Industries, SupplyAndPurchase, ShowAllMenus |
| `vw_OpenOrderRequestEngineeringAccepted` | تأیید مهندسی شده | فعال ∧ EngineeringAccept | edit+اکسل | کامل 74 | SalesOrProjectAccept, Supply, SupplyAndPurchase, ShowAllMenus |
| `vw_OpenOrderRequestMyRequestes` | درخواست های من | فعال ∧ متولی=@CurrentUserId | edit+اکسل | کامل 74 | Supply, SupplyAndPurchase, ShowAllMenus |
| `vw_OpenOrderRequestOtherView` | مشاهده عمومی | فعال ∧ EngineeringAccept ∧ Ids کاربر جاری | edit+اکسل | کامل 74 | Stop, View, SupplyAndPurchase, ShowAllMenus |
| `vw_openRequestConfig` | تنظیمات درخواست های باز | IsDeleted=0 بدون فیلتر واحد | multiSelect+اکسل+پرسنل | پایه 76 | ConfigManage, Industrial, Industries, SupplyAndPurchase, ShowAllMenus |
| `vw_openOrderRequestPurchaseCompleted` | پیشینه درخواست ها | IsDeleted=1 | edit+اکسل (`delete` خاموش) | پایه 77 | ShowAll, Industrial, Industries, EngineeringAccept, Supply, HistoryView, SupplyAndPurchase, ShowAllMenus |

### باقی‌مانده / نگاشت‌نشده

| مورد | چرا |
|---|---|
| EmailSendToSupplier / CompanyName_EmailSended واقعی + رنگ سبز + دکمه ارسال | WP1 (placeholder + TODO در اسکریپت) |
| PartCodingTitle | `Inv.Part` ستون کدینگ ندارد |
| Engineering_Accept_Time جدا | داخل `EngineeringAcceptShamsiDateTime` |
| Acc_DL_FK عددی | نمایش `FIN.DL.Code` |
| فیلتر part-permission (HTS `PartPermissionService`) | جدولی معادل نیست |
| لاگین شرکت/تامین‌کننده | در جدید نیست |
| تفکیک کامنت با `CreatedOrgUnit_FK` | تقریب با نقش Supply / Industrial |
| D47 انتخاب واحد سازمانی | نیاز به cshtml؛ خارج از SQL |
| نقش View / HistoryView اگر در DB نباشد | RoleAccess رد می‌شود تا WP4 |
| اجرای `@DryRun=0` + Verify روی PortalSRV + restart / کش نمایه | **اعمال شد 2026-09-18** (۱۴۷ پنهان→۰؛ Verify Msg 156 L80 `RowCount` + Msg 208 L223 `IntendedDup`؛ کش WebApp هنوز) |

---

## 13. WP9 — انجام‌شده / باقی‌مانده (2026-09-17) — فقط D16 فیلدهای HTS-only تامین‌کننده

تصمیم **Q5-a**: چهار فیلد روی `Gnr.Supplier` + کپی یک‌باره از HTS. `Party.Address` از قبل هست (nvarchar 2000) و دست نخورده ماند تا آدرس مشتری/عمومی آلوده نشود؛ آدرس صفحه 65 روی خود Supplier است.

### انجام‌شده (کد — migration ساخته شد، `update-database` اجرا نشد)

| مورد | فایل | تغییر |
|---|---|---|
| D16 موجودیت | `Entities/App/Gnr/Supplier.cs` | `Grade` (50)، `ServicesAndProducts` (256)، `RelatedPersonName` (128)، `Address` (2000) با `[DisplayInfo]` برچسب فارسی |
| D16 فرم | `WebApp/Views/Panel/Gnr/Supplier/Edit.cshtml` | همان چهار فیلد؛ آدرس `textarea`؛ wrapperها `form-group-inline`؛ `savefn` → `$($btnAction.element)` |
| D16 migration | `Data/Migrations/ApplicationDb/20260917203247_AddSupplierHtsFields.cs` | فقط چهار `AddColumn` روی `Gnr.Supplier` — بدون تغییر ناخواسته مدل |

`OpenOrderRequestController` / ویوهای درخواست باز / `BuyCategory*` / جاب‌ها / SQL پروفایل درخواست باز دست نخورده‌اند. List دوم ساخته نشد.

`dotnet build WebApp` خطا داد روی `OpenOrderRequestController.cs` L2439 (`long?`→`long`) — فایل کارگر دیگر؛ لمس نشد. پروژه `Data` هنگام `add-migration` سبز بود.

### انجام‌شده (SQL — فقط فایل؛ هیچ‌کدام اجرا نشده) — ترتیب روی HavayarApp

1. `.\update-database.ps1` **یا** `Data/Scripts/Add_Supplier_HtsFields.sql` (idempotent؛ اگر به‌جای migration اجرا شد `@RegisterEfMigration = 1` تا ردیف `20260917203247_AddSupplierHtsFields` ثبت شود).
2. `Data/Scripts/Update_Supplier_DataProfile_HtsFields.sql` — فقط **UPDATE** نمایه موجود `supplier_listinfo` (ستون‌های گرید / خدمات‌محصولات / شخص مرتبط / آدرس کامل). اگر نمایه نبود ردیف جدید ساخته نمی‌شود. ActionOptions و RoleAccess دست نخورده. سپس restart WebApp / پاک‌کردن کش نمایه.
3. `Data/Scripts/Migrate_Supplier_HtsFields_FromHts.sql` — ابتدا `@DryRun = 1` (پیش‌فرض)، سپس `@DryRun = 0`. `@OverwriteExisting = 0` فقط فیلد خالی را پر می‌کند.

### نگاشت `[TMS].TotalSystem.dbo.Gnr_ManCompany` → `Gnr.Supplier`

| HTS | جدید | تطبیق / نکته |
|---|---|---|
| `Grade` nvarchar(50) | `Grade` | عیناً (trim؛ خالی → NULL) |
| `ServicesAndProducts` nvarchar(256) | `ServicesAndProducts` | عیناً |
| `RelatedPersonName` nvarchar(128) | `RelatedPersonName` | عیناً |
| `Address` nvarchar(1024) | `Address` nvarchar(2000) | عیناً؛ **نه** `Party.Address` |
| تطبیق ۱ | `Party.HamkaranId = Hamkaran_ManCompany_FK` | پیشنهاد D16 |
| تطبیق ۲ | `Party.HamkaranId = ManCompany_ID` | fallback |
| تطبیق ۳ | `Party.Id = ManCompany_ID` | HtsId |
| چند ردیف HTS برای یک Supplier | یک ردیف | نوع `COMP`، سپس `UpdatedDate`، سپس `ManCompany_ID` |

`Gnr.Supplier` خودش `HtsId`/`HamkaranId` ندارد. بی‌تطبیق‌هایی که حداقل یکی از چهار فیلد را دارند چاپ می‌شوند.

### باقی‌مانده

| مورد | چرا |
|---|---|
| D13, D18, D19, D21 | خارج از این بسته (طبق دستور فقط D16) |
| D31 دکمه «جدید» پروفایل 38 | دست نخورده (HTS کامنت بود) |
| جاب `AddSupplierFromRahkaran` | این چهار فیلد را از راهکاران نمی‌آورد (عمدی؛ HTS-only) |
| `dotnet build WebApp` | خطای ازپیش‌موجود کنترلر درخواست باز (کارگر دیگر) |
| اجرای SQL / تست UI Edit و نمایه | طبق دستور اجرا نشد |

---

## WP1 — انجام‌شده / باقی‌مانده (2026-09-17) — پیشینه ارسال به پیمانکاران + ارسال + تامین‌کننده منتخب

تصمیم **Q1-a** + **Q10-b**: موجودیت/صفحه/ارسال از تنظیمات + مهاجرت ~۳۷٬۹۵۲ + صف `system.Notification` Type=Email (بدون SMTP). **Q7-a**: تنظیمات همان پروفایل `vw_openRequestConfig` روی List درخواست باز است. HtsParity (WP2) ویرایش نشد.

### فایل‌ها (کد)

| فایل | نقش |
|---|---|
| `Entities/App/Sup/OpenOrderRequestEmailToSupplier.cs` | Schema=Sup؛ FK درخواست/Supplier/User؛ SendMiladi/Shamsi؛ ToEmail/Subject/Body؛ Comment؛ SendingCount؛ HtsId |
| `Data/Migrations/ApplicationDb/20260917204222_AddOpenOrderRequestEmailToSupplier.cs` | فقط جدول + ایندکس یکتای فیلترشده HtsId — `update-database` اجرا نشد |
| `WebApp/Controllers/Dynamic/Sup/OpenOrderRequestEmailToSupplierController.cs` | List/FetchData/Export/Edit فقط‌خواندنی؛ دسترسی مستقل (D26) |
| `Views/Panel/Sup/OpenOrderRequestEmailToSupplier/{List,Edit}.cshtml` | datatableprofile + جزئیات فقط‌خواندنی |
| `OpenOrderRequestController.SendToSupplier` / `SetSelectedSupplier` (+ Partial) | نقش ConfigManage(+SupplyAndPurchase) / ConfigManage+Industrial+Industries+SupplyAndPurchase |
| `Views/Panel/Sup/OpenOrderRequest/_{SendToSupplier,SetSelectedSupplier}Partial.cshtml` | EntitySelector + `$.confirm({rtl:true})` |
| `Edit.cshtml` | stub `addSupplier` → مودال SetSelectedSupplier |

### ارسال (صفحه ۷۶)

`POST /Panel/Sup/OpenOrderRequest/SendToSupplier` (ids + supplierId + message اختیاری): ایمیل پیمانکار از Supplier/Party → یک `Notification` Email با `ToEmails` (+ CC فروش/پروژه/متولی خرید دسته) → یک ردیف پیشینه به ازای هر درخواست → `Changed=false` + `ManCompanyId=supplier`. Envelope `{isSuccess,message,data}`.

### SQL (اجرا نشد) — ترتیب

1. `.\update-database.ps1` **یا** `Add_OpenOrderRequestEmailToSupplier_Table.sql` (`@RegisterEfMigration=1` → `20260917204222_AddOpenOrderRequestEmailToSupplier`)
2. `Update_OpenOrderRequest_DataProfiles_HtsParity.sql` اگر هنوز نرفته (WP2؛ پیش‌نیاز placeholder `[tEml]`)
3. `Update_OpenOrderRequest_DataProfiles_EmailToSupplier.sql` — OUTER APPLY آخرین ارسال، رنگ سبز، دکمه «ارسال به پیمانکار» روی `vw_openRequestConfig`
4. `Seed_OpenOrderRequestEmailToSupplier_DataProfile.sql` — نقش **100015** `SendHistoryView` + RoleAccess مستقل از پیشینه ۷۷
5. `Seed_OpenOrderRequestEmailToSupplier_Menu.sql` — منوی تامین و خرید (Id 3)
6. `Seed_OpenOrderRequest_SendToSupplier_RoleAccess.sql`
7. `Migrate_OpenOrderRequestEmailToSupplier_FromHts.sql` — `[TMS]`، `@DryRun=1`؛ درخواست با PurchaseRequestItemId+OrderRowId (HtsId والد نیست)؛ پیمانکار Hamkaran؛ کاربر Username؛ upsert با HtsId

`dotnet build` WebApp و App.BackgroundJob سبز.

### باقی‌مانده WP1

| مورد | چرا |
|---|---|
| پیوست‌های HTS (`Attachment_FK` / Part / Edms) | در موجودیت جدید نیست |
| `Role.cs` HasData 100015 | فقط seed SQL |
| دکمه ارسال روی Edit | فقط تنظیمات + تامین‌کننده منتخب روی Edit |
| SMTP / EmailJob | Q10-b عمداً خاموش |
| اجرای SQL / restart / کش نمایه / تست UI | طبق دستور اجرا نشد |

---

## WP7 — انجام‌شده / باقی‌مانده (2026-09-17) — پل VPIS + درج خودکار + گیت فروش + زمان‌بندی رویژن

تصمیم **Q8-a**: پل روزانه از HTS تا خاموشی + پورت درج خودکار + زمان‌بندی `CheckVpisRevisionChangesAndNotify`.

### انجام‌شده

- `OpenOrderRequestJob.SyncOpenOrderRequestVpisFromHts` — کپی لینک‌های غایب؛ تطبیق درخواست PurchaseRequestItemId+OrderRowId؛ پروژه/VPIS/مدرک با HtsId؛ unmatched لاگ؛ سپس AutoInsert
- `AutoInsertVpisLinksForRequestsAsync` — شرط «درج بصورت اتوماتیک توسط سیستم» (آخرین مدرک معتبر GetVpisListByProject؛ `ProductionOrder.EdmsProject` → `Project.HtsId`)؛ جراحی بعد از insert راهکاران + انتهای جاب پل. سورس C# HTS برای این درج پیدا نشد (D32)
- `Seed_OpenOrderRequestVpisJobs_Schedule.sql` — Sync **07:02**، CheckVpisRevision **07:30** (پس از استقرار باینری جاب؛ وگرنه JobDiscovery پاک می‌کند)
- `SalesOrProjectAccept`: نقش موجود + هویت کارشناس/مدیر فروش/مدیر پروژه همان ردیف (یا Admin) + **وجود حداقل یک لینک VPIS** (D20)

### باقی‌مانده WP7

| مورد | چرا |
|---|---|
| UI تایید فروش وقتی VPIS ندارد | فقط گیت سرور |
| `Sup.OpenOrderRequestVpis` هنوز ۰ تا اجرای جاب/SQL همگام | داده |
| نگاشت SQL VPIS در `SyncOpenOrderRequestFromTotalSystem.sql` | شناسه HTS را به‌جای Id محلی می‌نوشت؛ جاب جایگزین است |
| اجرای seed زمان‌بندی / تست جاب | طبق دستور اجرا نشد |
