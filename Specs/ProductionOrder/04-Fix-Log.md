# سفارش ساخت و اقلام — گزارش اصلاحات (2026-09-17)

> ادامه `03-Gap-Register.md` §12. مقایسه دوباره روی سورس فعلی HTS (`HtsProject`) و HavayarApp + دیتابیس dev `HavayarApp` (`172.20.40.42`) انجام و مغایرت‌های قطعی اصلاح شد. **هیچ اسکریپت SQL روی دیتابیس اجرا نشده**؛ فقط در `Data/Scripts` قرار گرفته (compile-check شده با `SET NOEXEC ON`). بیلد کامل `HavayarApp.sln` موفق است.

---

## 1. فایل‌های تغییریافته / جدید

| فایل | نوع | چه شد |
|---|---|---|
| `Entities/App/Sale/ProductionOrderItemWorkflowRules.cs` | جدید | قواعد مشترک گردش‌کار (DeviceType برقی، پیشوند کالای ساخت داخل، مقصد پس از تأیید مهندسی/PM، مقصدهای کمیته، نگاشت وضعیت تولید→تاریخ قلم). WebApp و Job هر دو از همین استفاده می‌کنند. |
| `Entities/App/Sale/ProductionOrder.cs` | ویرایش | `DeliveryLocation` (محل تحویل)، `EngineeringAttachmentFile/Id` (پیوست پنجم مهندسی/متره). |
| `Services/ProductionOrderServices/ProductionOrderItemNotificationHelper.cs` | جدید | صف‌کردن ایمیل (`system.Notification`)، گروه‌های اعلان (کدها معادل گروه‌های کاربری HTS)، کاربرانِ یک نقش، قالب ایمیل HTS. |
| `WebApp/Controllers/Dynamic/Sale/ProductionOrderItemController.cs` | ویرایش | زنجیره‌های خودکار مهندسی/PM، فرم تصمیم کمیته (`CommitteeDecide`) و بازگشت استعلام (`SendToSupplyCommittee` با کامنت/پیوست، فقط ۱۹۰۶→Inquirer / ۱۹۰۸→Ministry)، `SaveComment` (تاریخ‌های قلم + اعلان‌ها)، BOM (بدون ۲۲۵۸؛ تعیین تکلیف → ۲۷۴۴؛ BOM روتین)، `Update` (کامنت مرحله + اعلان تغییر سریال)، `SparePartListPartial`، `ProductionOrderItemInquiryPartial`. **حذف شد:** `AcceptIndustrial`، `SendToInquiry`، `SendToMinistryReview`، `CommitteeAccept`، `CommitteeReject`. |
| `WebApp/Controllers/Dynamic/Sale/ProductionOrderController.cs` | ویرایش | `Update` فیلدهای شعبه/محل تحویل/پیوست مهندسی/گزارش تحویل به‌موقع را کپی می‌کند؛ `Edit` پیوست پنجم و شعبه را Include می‌کند. **حذف شد:** `DoFinancialConfirm` (تأیید مالی فقط راهکاران). |
| `WebApp/Actions/Sale/ProductionOrderItemCommentAction.cs` | ویرایش | کامنت‌های «مرحله ساخت» (`IsForProductionStepStatus`) دیگر `CheckStatus` را بازنویسی نمی‌کنند (عین تریگر HTS). |
| `App.BackgroundJob/Jobs/Sale/ProductionOrderJob.cs` | ویرایش | مهلت مهندسی (فیلتر `CheckStatus=2195`، مقصد ۱۹۰۴/۲۲۰۲)، مهلت PM (مبنای کامنت ۱۳۸۰)، هشدار نزدیک‌تحویل واقعی، جاب جدید انقضای استعلام، جاب جدید BOM روتین، اعلان‌های گروهی. |
| `WebApp/Views/Panel/Sale/ProductionOrderItem/Edit.cshtml` | ویرایش | دکمه‌های تصمیم کمیته / ارسال نتیجه استعلام / قطعات یدکی / پیوست‌های سفارش؛ ورودی تاریخ تحویل؛ کارت «اطلاعات تکمیلی برنامه‌ریزی»؛ کامنت در مودال PM. دکمه‌های تأیید صنایع، ارسال به استعلام/وزارت، رد کمیته حذف شدند. |
| `WebApp/Views/Panel/Sale/ProductionOrder/Edit.cshtml` | ویرایش | دکمه «تأیید مالی» حذف شد (فقط «منسوخ کردن» روی سفارش تأیید مالی شده). |
| `Data/Scripts/Seed_ProductionOrderItem_MyCartable_DataProfile.sql` | ویرایش | شاخه State=2 (تأیید مالی) حذف؛ شاخه‌های ۱۹۰۴/۱۹۰۵، ۱۹۰۶، ۱۹۰۸ به نقش‌های دقیق HTS محدود شد. |
| `WebApp/Views/Panel/Sale/ProductionOrderItem/_ProductionOrderItemInquiryPartial.cshtml` | جدید | فرم مسئول/LeadTime/پیوست/کامنت (دو حالت کمیته / بازگشت استعلام). |
| `WebApp/Views/Panel/Sale/ProductionOrderItem/_SparePartListPartial.cshtml` | جدید | جدول قطعات یدکی کالای قلم. |
| `WebApp/Views/Panel/Sale/ProductionOrderItem/_ProductionOrderItemCommentListPartial.cshtml` | ویرایش | ستون «مدت زمان اخذ استعلام» در تب استعلام. |
| `WebApp/Views/Panel/Sale/ProductionOrder/Edit.cshtml` | ویرایش | شعبه فروش، محل تحویل، پیوست مهندسی/متره، ستون عامل تأخیر در تب تأخیرات. |
| `WebApp/Views/Panel/Sale/ProductionOrder/_ProductionOrderDelayPartial.cshtml` | ویرایش | `DelayResponsible` (الزامی) + اصلاح `data-bind` تاریخ‌ها به نام شمسی. |
| `Data/Scripts/Add_ProductionOrder_DeliveryLocation_EngineeringAttachment.sql` | جدید | دو ستون جدید سربرگ (بدون EF migration — الگوی موجود پروژه). |
| `Data/Scripts/Seed_ProductionOrder_Menu.sql` | جدید | برگ‌های منو در فروش و صنایع/برنامه‌ریزی. |
| `Data/Scripts/Seed_ProductionOrder_DataProfiles.sql` | جدید | دو نمایه سربرگ (`PO_All`، `PO_Related`) + نقش `Sale.ProductionOrder.ViewRelated` (200036) + RoleAccess. |
| `Data/Scripts/Seed_ProductionOrderItem_DataProfiles.sql` | ویرایش | بخش `[5/5]`: WHERE هدر تأیید مالی برای `POI_ViewActive` و WHERE کاربر جاری برای `POI_Sales_Related_Active`. |
| `Data/Scripts/Seed_ProductionOrderItem_NotificationGroups.sql` | جدید | ۸ گروه اعلان. |

---

## 2. ترتیب اجرای اسکریپت‌ها (روی هر محیط)

1. `Add_ProductionOrder_DeliveryLocation_EngineeringAttachment.sql` — قبل از deploy کد جدید (entity دو ستون جدید دارد).
2. `Seed_ProductionOrderItem_NotificationGroups.sql` — سپس اعضای گروه‌ها را از پنل «گروه‌های اعلان» اضافه کنید (بدون عضو، ایمیل‌ها صف نمی‌شوند و فقط لاگ می‌شود).
3. `Seed_ProductionOrderItem_DataProfiles.sql` (نسخه جدید — پیش‌نیاز: SavedQuery 104).
4. `Seed_ProductionOrder_DataProfiles.sql` (پیش‌نیاز: نقش 200020 از اسکریپت ۳).
5. `Seed_ProductionOrderItem_MyCartable_DataProfile.sql` (نسخه هم‌تراز با DB زنده: تاییدکننده تجهیز + حذف شاخه مالی + نقش‌های دقیق ۱۹۰۶/۱۹۰۸).
6. `Seed_ProductionOrder_Menu.sql`.
7. Restart WebApp (اکشن‌های جدید کنترلر: `CommitteeDecide`, `ProductionOrderItemInquiryPartial`, `SparePartListPartial` باید در کاتالوگ دسترسی دیده شوند) و تیک اکشن‌ها در Role UI برای نقش‌های `SupplyCommitteeBoss` / `Inquirer` / `MinistryOfIndustryInquirer`. ردیف‌های `system.RoleAccess` اکشن‌های حذف‌شده (`AcceptIndustrial`, `SendToInquiry`, `SendToMinistryReview`, `CommitteeAccept`, `CommitteeReject`, `DoFinancialConfirm`) بی‌اثر می‌شوند و می‌توان پاک کرد.

### 2.1 اجرای انجام‌شده — 2026-09-17 21:0x روی `PortalSRV\PORTAL` / `HavayarApp` (همان سرور connection string Development و Production)

| اسکریپت | نتیجه |
|---|---|
| `Add_ProductionOrder_DeliveryLocation_EngineeringAttachment.sql` | ستون‌های `DeliveryLocation`, `EngineeringAttachmentId` + FK + ایندکس ایجاد شد |
| `Seed_ProductionOrderItem_NotificationGroups.sql` | ۶ گروه جدید (Id 7–12) ایجاد؛ دو گروه BOM از قبل بودند (هر یک ۲ عضو). گروه‌های جدید **بدون عضو** |
| `Seed_ProductionOrderItem_DataProfiles.sql` | ۱۰ نمایه POI به‌روزرسانی؛ `POI_Sales_Related_Active` (Id 230) ایجاد؛ RoleAccess بازسازی؛ کاربران دارای نقش‌های سفارش ساخت ۹۸ → ۱۱۵ |
| `Seed_ProductionOrder_DataProfiles.sql` | `PO_All` (Id 231)، `PO_Related` (Id 232)، نقش `Sale.ProductionOrder.ViewRelated` (200036) |
| `Seed_ProductionOrderItem_MyCartable_DataProfile.sql` | `POI_MyCartable` (Id 118) به‌روزرسانی؛ RoleAccess نقش `FinancialConfirm` روی آن حذف شد |
| `Seed_ProductionOrder_Menu.sql` | برگ «سفارش ساخت» به منوی فروش اضافه شد؛ صنایع/برنامه‌ریزی از قبل کامل بود |

پشتیبان پیش از اجرا (SavedQuery/SystemMenu/RoleAccess مرتبط): `C:\Users\Padidar\Documents\HavayarApp_PO_backup_20260917.json`.
کوئری همه نمایه‌های جدید/تغییریافته با `@CurrentUserId` تست شد و اجرا می‌شود.

| اسکریپت (اجرای دوم — 21:2x) | نتیجه |
|---|---|
| `Seed_ProductionOrderItem_WorkflowRoleAccess.sql` | `committeedecide/{id}` → رئیس کمیته؛ `sendtosupplycommittee/{id}` → استعلام‌گر + استعلام‌گر وزارت؛ دسترسی صفحه (list/edit/new) برای استعلام‌گر وزارت که نداشت. ردیف‌های اکشن‌های حذف‌شده (acceptindustrial، sendtoinquiry، sendtoministryreview، committeeaccept، committeereject) پاک شد. Partialها (`ProductionOrderItemInquiryPartial`, `SparePartListPartial`) بدون `[ActionDisplayName]` هستند و ردیف لازم ندارند. |
| `Seed_ProductionOrderItem_NotificationGroupMembers.sql` | ۵۰ عضو از سیستم قدیم: کمیته ۱ (گروه 380)، شروع تست ۸ (گروه 540)، نزدیک‌تحویل ۱ (گروه 567)، تغییر سریال ۲ (گیرندگان واقعی کد)، صنایع ۱۰ (لیست‌های ثابت)، تغییر وضعیت تولید ۲۸ (لیست ثابت + گروه 591). ۸ نام قدیمی اضافه نشد: ۴ نفر در هر دو سیستم غیرفعال (kaboli.f، Khanipour.p، zamani.z، Zandi.n) و ۴ نام فقط در کد HTS بودند و کاربر ندارند (Asgari.sh، Bakhtiari.a، Sharifi.f، shokrgozar.m). |

**باقی‌مانده دستی:** Restart WebApp (کاتالوگ endpointها + کش دسترسی کاربران آنلاین بازخوانی شود)؛ زمان‌بندی جاب‌ها (§3).

## 3. جاب‌هایی که باید در پنل Job فعال/زمان‌بندی شوند (GAP-JOB-004)

| هندلر | زمان پیشنهادی (معادل HTS) |
|---|---|
| `AddProductionOrderFromRahkaran` | هر ۱۵ دقیقه |
| `CheckFinancialConfirmsOrders` | هر ۲۰ دقیقه |
| `ApplyUnConfirmedProductionOrders` | هر ۲۰ دقیقه |
| `AddOrUpdateStandardDeliveryDates` | هر ۲۰ دقیقه |
| `SendHoldedItemsToIndustrialUnitThatWasInProjectManagerCartable` | هر ۲۰ دقیقه |
| `SendExpiredEngineeringItemsToIndustrial` | ۰۹:۰۱ و ۱۵:۰۱ |
| `SendExpiredInquiryItemsNotification` **(جدید)** | ۰۹:۰۱ و ۱۵:۰۱ |
| `SendProductionOrderItemsWithNearDeliveryDateNotifications` | ۰۹:۴۵ روزانه |
| `AddRoutineBomListTask` **(جدید)** | هر ۲۰ دقیقه |
| `AddProductionOrderItemBomFromHts` | فقط مهاجرت |
| `SyncProductionOrderOperationalFieldsFromHts` | فقط کات‌اور (Q10) |

---

## 4. گردش‌کار اصلاح‌شده (خلاصه رفتار جدید = HTS)

```
ورود از راهکاران → تأیید مالی هدر (فقط راهکاران/Job) → CheckFinancialConfirmsOrders
  روتین / CNG / یدکی ─────────────────────────────────► ۱۹۰۴ صنایع داخلی
  هدر مدیر پروژه دارد ──► ۲۲۵۸ ──تأیید PM──► ۲۲۵۹ ──خودکار──► ۲۱۹۵ مهندسی
                              └─رد PM──► ۲۲۶۰ ──خودکار──► ۲۲۶۱ (بازگشت به فروش)
  وگرنه ─────────────────────────────────────────────► ۲۱۹۵ مهندسی (برق/مکانیک با DeviceType)
۲۱۹۵ ──تأیید──► ۲۱۹۷ ──خودکار──► (غیرروتین∧ساخت داخل) ∨ پیشوند کالا ? ۱۹۰۴ : ۲۲۰۲
     └─رد──► ۲۱۹۸ ──خودکار──► ۲۲۰۱ (بازگشت به فروش)
     └─مهلت ۱ روز (Job) ──► همان قاعده ۱۹۰۴/۲۲۰۲
۲۲۰۲ / ۱۹۰۹ ──تصمیم رئیس کمیته──► ۱۹۰۴ | ۱۹۰۵ | ۱۹۰۶ (مسئول+LeadTime) | ۱۹۰۸ (مسئول+LeadTime)
۱۹۰۶ / ۱۹۰۸ ──استعلام‌گر: نتیجه + پیوست──► ۱۹۰۹ ──► دوباره تصمیم کمیته
LeadTime گذشته (Job) ──► ایمیل مسئول + کمیته
BOM روی مرحله ۲۱۳ + مدیر پروژه ──► مرحله ۱۳۸۰ (CheckStatus دست‌نخورده) ──تعیین تکلیف BOM──► مرحله ۲۷۴۴
تغییر وضعیت تولید: ۱۷۸۹→تاریخ پایان تولید، ۱۸۴۳→پایان تست، ۱۷۹۱→آماده‌سازی (+اقساط ۲–۵)؛ ۲۲۰۹ با PM → اعلان IT Support
```

---

## 5. تصمیم‌های گرفته‌شده (2026-09-17) و اعمال آن‌ها

| # | موضوع | تصمیم | اعمال |
|---|---|---|---|
| Q1/Q2/Q3 | اکشن‌های افزوده سیستم جدید: «تأیید صنایع»، «ارسال به استعلام/وزارت از کارتابل صنایع»، «تأیید/رد کمیته» | **همه حذف — دقیقاً مثل HTS** | `AcceptIndustrial`، `SendToInquiry`، `SendToMinistryReview`، `CommitteeAccept`، `CommitteeReject` از کنترلر و Edit حذف شدند. تنها اکشن‌های کمیته: `CommitteeDecide` (رئیس کمیته روی ۲۲۰۲/۱۹۰۹ → ۱۹۰۴/۱۹۰۵/۱۹۰۶/۱۹۰۸) و `SendToSupplyCommittee` (استعلام‌گر فقط روی ۱۹۰۶، استعلام‌گر وزارت فقط روی ۱۹۰۸ → ۱۹۰۹). صنایع مثل HTS فقط قلم را ویرایش می‌کند. |
| Q4 | مهلت ۲ روزه مدیر پروژه | **رفتار سیستم جدید** | قلمِ ۲۲۵۸ پس از مهلت → مرحله ۲۷۴۴ + صنایع ۱۹۰۴؛ اقلام مسیر BOM فقط مرحله. |
| Q7 | تأیید مالی سربرگ | **فقط راهکاران** | `DoFinancialConfirm` + دکمه «تأیید مالی» حذف شد؛ شاخه State=2 از `POI_MyCartable` و نمایه «کارتابل تأیید مالی» از seed حذف شد. State فقط با Job همگام می‌شود. نقش `Sale.ProductionOrder.FinancialConfirm` (200021) بلااستفاده شد — می‌توان از پنل حذف کرد. |
| Q9 | چاپ فرم سفارش ساخت | **بعداً** | — |
| Q5/Q6 | مشتری/تفصیل مستقل روی سربرگ؛ ویرایش «مقدار» قلم | **بسته بمانند** | از راهکاران/قرارداد می‌آیند. |

## 5.1 سوال‌های باقی‌مانده (کم‌اهمیت — هر وقت خواستید)

| # | موضوع | پیش‌فرض فعلی |
|---|---|---|
| Q8 | «بروزرسانی همکاران» (`DoUpdateHamkaranOperation`) با ورود از راهکاران لازم نیست؟ | پیاده نشده. |
| Q10 | جاب کات‌اور `SyncProductionOrderOperationalFieldsFromHts` بعد از برش نهایی غیرفعال شود؟ | فعال (اجرای دستی). |
| Q11 | دید متقابل واحدهای ۲۶۲/۲۶۳ در گرید سربرگ | پیاده نشده. |
| Q12 | قیمت داخلی/خارجی BOM (`ShowPrice`) | پیاده نشده. |
| Q13 | ایمیل State مالی راهکاران هنوز از SQL Agent می‌رود؟ | فرض: بله. |
| Q14 | اعضای گروه‌های اعلان | **انجام شد** — از گروه‌های کاربری/لیست‌های ثابت HTS منتقل شد (`Seed_ProductionOrderItem_NotificationGroupMembers.sql`). |

---

## 6. آنچه عمداً منتقل نشد

- لیست‌های ایمیل سخت‌کد HTS (اشخاص، شعبه‌های ۴۹/۵۰/۵۱/۱۲/۱۵/۳۹، `mirlohi.m`, `khalaj.m`, `qci` …) → گروه‌های اعلان قابل مدیریت.
- SMS تغییر وضعیت تولید (`SendSmsNotification` با شماره‌های ثابت).
- ~~افزودن خودکار مسئول استعلام به گروه‌های کاربری ۳۸۱/۳۸۲ (`CheckAndUpdatePermissions`)~~ → **منتقل شد** (2026-09-17 21:3x): `CommitteeDecide` پس از انتخاب مقصد استعلام/وزارت، نقش `Inquirer` / `MinistryOfIndustryInquirer` را خودکار به مسئول انتخاب‌شده می‌دهد (`EnsureInquiryResponsibleRoleAsync`) تا مثل HTS بتواند نتیجه را ثبت کند.
- صفحات legacy (`Itm`/`Itm_New`، اقساط ۲۲۷، پیوست ۲۱۶، متره قیمت).
