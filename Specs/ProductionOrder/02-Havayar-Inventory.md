# سفارش ساخت و اقلام — موجودی پیاده‌سازی HavayarApp

> استخراج **فقط‌خواندنی** از سورس سیستم جدید (۲۰۲۶-۰۹-۱۵). هیچ تغییری در کد، SQL، منو یا جاب اعمال نشده است. الگوی نگارش: جداول وضعیت + شاهد `file:line` مطابق `Specs/AfterSalesServiceSystem/00-Overview-and-Conventions.md`. شناسه‌های فنی به English مانده‌اند.

**محدوده این فایل:** سربرگ `ProductionOrder`، اقلام `ProductionOrderItem`، و هر صفحه/partial/اکشن/جاب/Action/نقش/نمایه که از همین دو صفحه باز یا صدا می‌شود (BOM، کامنت/سابقه، استعلام، تاخیر توکار، توقف، Data Profile، Role، Job، EntityAction).

**خارج از این فایل:** موجودی HTS (`01-HTS-Inventory.md`)، تطبیق مغایرت (`03-Gap-Register.md`)، نمای کلی مقایسه (`00-Overview.md`).

---

## 1. هدف

فهرست شواهد آنچه **الان** در HavayarApp برای سفارش ساخت و اقلام وجود دارد تا مرحله بعدی مقایسه با HTS بدون حدس انجام شود.

منبع زنده کنترلرها:

| کنترلر | فایل | Route attribute | View folder |
|---|---|---|---|
| `ProductionOrderController` | `WebApp/Controllers/Dynamic/Sale/ProductionOrderController.cs:18-22` | `[Route("Panel/[controller]")]` → `/Panel/ProductionOrder/...` | `WebApp/Views/Panel/Sale/ProductionOrder/` |
| `ProductionOrderItemController` | `WebApp/Controllers/Dynamic/Sale/ProductionOrderItemController.cs:28-31` | `[Route("Panel/[controller]")]` → `/Panel/ProductionOrderItem/...` | `WebApp/Views/Panel/Sale/ProductionOrderItem/` |

Schema موجودیت‌ها `Sale` است (`[Table(..., Schema = "Sale")]`) در حالی که route پیشوند ماژول `Panel/Sale/` ندارد — الگوی قدیمی `Panel/[controller]` نه `Panel/Sale/[controller]` استاندارد Form Builder.

---

## 2. Route در برابر پوشه View

| مورد | شاهد |
|---|---|
| کنترلر در namespace `WebApp.Controllers.Dynamic` (بدون `Sale` در namespace) ولی فایل فیزیکی زیر `Controllers/Dynamic/Sale/` | `ProductionOrderController.cs:16-22` |
| View با مسیر صریح `return View(@"\Views\Panel\Sale\ProductionOrder\Edit.cshtml", ...)` | `ProductionOrderController.cs:155` |
| کنترلر قلم همان الگوی route | `ProductionOrderItemController.cs:28`، View: `ProductionOrderItemController.cs:137` |
| URLهای کلاینت lowercase نیستند؛ JS از `/Panel/ProductionOrder/...` و `/Panel/ProductionOrderItem/...` استفاده می‌کند | `ProductionOrder/Edit.cshtml:676`، `ProductionOrderItem/Edit.cshtml:601` |
| منوی پنل طبق قرارداد پروژه باید `Path` را **lowercase** نگه دارد (`/panel/productionorder/list`)؛ هیچ seed منویی در ریپو این Path را نمی‌نویسد (بخش ۸) | قرارداد `havayar-add-new-page.mdc` |

---

## 3. منو

**هیچ seed منویی برای سفارش ساخت / اقلام سفارش ساخت در `Data/Scripts` یافت نشد.**

جستجو روی `Data/Scripts` برای `SystemMenu` + `productionorder` / `ProductionOrderItem/list` نتیجه‌ای نداد. اسکریپت‌های منوی موجود مربوط به AfterSales / BPM / Sale Order Detail هستند، نه این دو صفحه.

انتظار محصول (طبق قرارداد پنل، نه از seed):

| صفحه | Path مورد انتظار در MenuBuilder (lowercase) | کنترلر واقعی |
|---|---|---|
| لیست سفارش ساخت | `/panel/productionorder/list` | `ProductionOrderController.List` → `GET Panel/ProductionOrder/List` |
| لیست اقلام | `/panel/productionorderitem/list` | `ProductionOrderItemController.List` → `GET Panel/ProductionOrderItem/List` |

ورود واقعی منو باید از **MenuBuilder** (`/Panel/System/MenuBuilder`) و `system.SystemMenu.Content` در دیتابیس باشد. این ریپو آن ردیف‌ها را seed نمی‌کند.

---

## 4. کنترلر سربرگ — `ProductionOrderController`

`[ControllerInfo("سفارش ساخت", typeof(ProductionOrder))]` — `ProductionOrderController.cs:21`.

### 4.1 اکشن‌ها

| HTTP | Action | `ActionDisplayName` | Access | رفتار واقعی | شاهد |
|---|---|---|---|---|---|
| POST | `Save` | ذخیره | Api / Save | اگر `Id` خالی باشد `Add`، وگرنه اگر رکورد باشد `Update` | `:24-38` |
| POST | `Add` | درج | Api / Create | `SaveAsync` مستقیم | `:40-46` |
| POST | `Update` | ویرایش | Api / Update | کپی انتخابی فیلدها روی `oldEntity`؛ **`State`، تایید مالی، منسوخ‌سازی و `HamkaranId` از فرم کپی نمی‌شوند** | `:48-109` |
| GET | `Delete` | حذف | Api / Delete | soft-delete از طریق Repository | `:111-119` |
| GET | `Edit` | ویرایش اطلاعات | View / Update | Include قرارداد/کاربران/پیوست/کامنت؛ `ViewBag.ProductionOrderDelayViewData` برای تب تاخیر | `:121-159` |
| GET | `New` | درج اطلاعات | View / Create | همان View ویرایش با entity خالی | `:161-167` |
| GET | `List` | لیست اطلاعات | View / List | فقط `<datatableprofile>` | `:169-174` |
| POST | `ExportToExcel` | خروجی اکسل | Api | `ExportLargeDataToExcelAsync` — خود Repository این متد را `NotImplementedException` می‌اندازد | `:176-192` + `Data/Repositories/Repository.cs:781-783` |
| POST | `FetchData` | دریافت اطلاعات | Api / FetchData | `GetRowSecurityPredicate()` صدا می‌شود ولی **نتیجه‌اش AND نمی‌شود**؛ سپس `FetchDataAsync` که throw می‌کند | `:194-207` |
| POST | `DoFinancialConfirm/{id}` | تایید مالی | Api / Custom | نقش `Sale.ProductionOrder.FinancialConfirm` یا Admin؛ `State` باید `Submit` یا `FinancialUnitReview` باشد؛ فیلدهای تایید مالی ست می‌شود؛ کامنت هدر با `NewState = FinancialApproval` | `:211-263` |
| POST | `DoObsolete/{id}` | منسوخ‌سازی | Api / Custom | نقش `Sale.ProductionOrder.Obsolete`؛ فقط از `FinancialApproval`؛ کسکید اقلام آخرین نسخه به `Deprecated` + `Canceled` | `:265-351` |
| GET | `ProductionOrderDelayPartial` | **بدون** `ActionDisplayName` | — | partial فرم تاخیر توکار | `:362-377` |
| POST | `SaveProductionOrderDelay` | ذخیره تاخیر سفارش ساخت | Api | Save/Update روی `Pln.ProductionOrderDelay` | `:379-394` |

تب «تاییدکننده تجهیز» روی سربرگ **حذف شده** است؛ کامنت کنترلر می‌گوید تنظیم اکنون سراسری و بر اساس `DeviceType` است (`ProductionOrderController.cs:396-397`).

### 4.2 امنیت ردیف (آماده، اعمال‌نشده)

`GetRowSecurityPredicate` (`:412-427`): اگر Admin یا نقش `Sale.ProductionOrder.ShowAll` → همه؛ وگرنه سازنده / کارشناس فروش / مدیر فروش / کارشناس جایگزین / مدیر پروژه. **در پاسخ `FetchData` اثری ندارد** (`:205-206`). کامنت می‌گوید فیلد واحد سازمانی ایجادکننده روی `BaseEntity` نیست (`:407-410`).

لیست واقعی پنل از **Data Profile / SavedQuery** تغذیه می‌شود نه از `FetchData` کنترلر (موتور عمومی DataTable در Repository پیاده نشده).

---

## 5. کنترلر اقلام — `ProductionOrderItemController`

`[ControllerInfo("اقلام سفارش ساخت", typeof(ProductionOrderItem))]` — `:31`.

وابستگی اعلان BOM: `INotificationGroupService`؛ ثابت‌ها:

- `Sale.ProductionOrderItemBom.Industrial` — `:38`
- `Sale.ProductionOrderItemBom.Engineering` — `:39`

### 5.1 CRUD و لیست

| HTTP | Action | `ActionDisplayName` | Access | رفتار | شاهد |
|---|---|---|---|---|---|
| POST | `Save` | ذخیره | Api / Save | Add یا Update | `:41-57` |
| POST | `Add` | درج | Api / Create | `SaveAsync` کامل مدل | `:59-65` |
| POST | `Update` | ویرایش | Api / Update | **فقط زیرمجموعه تولید/برنامه‌ریزی** روی `oldEntity` کپی می‌شود (نه کالا، نه CheckStatus، نه مشخصات فنی) | `:67-106` |
| GET | `Delete` | حذف | Api / Delete | | `:108-116` |
| GET | `Edit` | ویرایش اطلاعات | View / Update | Include سفارش+کالا؛ `ViewBag.IsElectricalDiscipline` از `ElectricalDeviceTypes` | `:118-141` |
| GET | `New` | درج اطلاعات | View / Create | | `:186-192` |
| GET | `List` | لیست اطلاعات | View / List | datatableprofile + مارکاپ مخفی مودال جابجایی | `:194-199` |
| POST | `ExportToExcel` | خروجی اکسل | Api | همان `NotImplementedException` Repository | `:201-217` |
| POST | `FetchData` | دریافت اطلاعات | Api / FetchData | predicate ردیف صدا می‌شود ولی اعمال نمی‌شود؛ سپس throw | `:219-232` |
| GET | `BomListBy` | لیست BOM قلم سفارش ساخت | View / Custom | صفحه جدا BOM | `:143-173` |
| GET | `GetBomListByParentId` | دریافت لیست BOM با شناسه قلم سفارش ساخت | Api / FetchData | JSON بدون DataTable سرور | `:175-184` |

فیلدهایی که `Update` واقعاً می‌نویسد (`:77-98`): `ProductionStep`, `PlanningNumber`, `Serial`, تاریخ‌های تولید/تست/آماده‌سازی/تحویل/آماده‌سازی مدارک/تحویل استاندارد، `PlanningConsideration`, `IsRoutine`, `Status`, `ProductionStatus`, `SerialType`. اگر قلم در کارتابل صنایع باشد `TrySetFirstIndustrialChange` (`:100-102`).

### 5.2 گردش کار قلم

`ElectricalDeviceTypes` (`:262-268`): `ControlPanel(2403)`, `ElectricalPanel(2404)`, `InverterPanel(2212)`, `Sequencer(2211)`.

`IndustrialDashboardStatuses` (`:270-274`): `IndustrialDashboardInternal(1904)`, `ForeignIndustryCatalog(1905)`.

| HTTP | Action | نمایش | نقش لازم | پیش‌وضعیت | نتیجه | شاهد |
|---|---|---|---|---|---|---|
| POST | `AcceptEngineering/{id}` | تایید/رد مهندسی | برق یا مکانیک بر اساس `DeviceType` | `MechanicalEngineeringApprovalPending(2195)` **برای هر دو رشته** | Approve → `EngineeringConfirmationMechanical` + `AwaitingProduction`؛ Reject → `MechanicalEngineeringNotApproved`؛ Revise → `EngineeringReassessmentNeeded`. `ProductionStatus` کامنت برای برق از اعضای برقی enum است | `:284-374` + یادداشت سازگاری enum `:276-282` |
| POST | `AcceptsEngineering` | تایید/رد مهندسی تجمیعی | همان حلقه روی `AcceptEngineering` | آرایه `PrimaryKeyValues` | برای کارتابل لیست | `:383-407` |
| POST | `AcceptIndustrial/{id}` | تایید صنایع | `Sale.ProductionOrderItem.AcceptIndustrial` | 1904 یا 1905 | **CheckStatus عوض نمی‌شود** (عضو «تایید صنایع» در enum نیست)؛ فقط `ProductionStep = AwaitingProduction` + کامنت مرحله ساخت | `:409-463` |
| POST | `AcceptProjectManager/{id}` | تایید/رد مدیر پروژه | `Sale.ProductionOrderItem.AcceptProjectManager` | `AwaitingProjectManagerApproval(2258)` | Approve → 2259 + `AwaitingProduction`؛ Reject → 2260؛ Revise → 2261 | `:465-535` |
| POST | `SendToInquiry/{id}` | ارسال به استعلام | `Inquirer` | کارتابل صنایع | `InquiryNeeded(1906)` + ردیف Inquiry | `:537-554` |
| POST | `SendToMinistryReview/{id}` | ارسال به بررسی وزارت صنایع | `MinistryOfIndustryInquirer` | 1904/1905/1906 | `IndustrialMinistryReviewRequired(1908)` + Inquiry | `:556-580` |
| POST | `SendToSupplyCommittee/{id}` | ارسال به کمیته تامین | یکی از Inquirer / Ministry / SupplyCommitteeBoss | 1904/1905/1906/1908 | `SendToSupplyCommitteeChair(1909)` — **نه** `CommitteeHeadReviewPending(2202)` | `:582-614` + یادداشت `:625-627` |
| POST | `CommitteeAccept/{id}` | تایید کمیته تامین | `SupplyCommitteeBoss` | 1909 **یا** 2202 | بازگشت به `IndustrialDashboardInternal` + `AwaitingProduction` | `:616-642` |
| POST | `CommitteeReject/{id}` | رد کمیته تامین | `SupplyCommitteeBoss` | 1909 یا 2202 | `Deprecated` + `Canceled` (تصمیم طراحی: رد = نهایی نه بازگشت به ثبت اولیه) | `:644-669` |

کمک‌کننده مشترک `TransitionCheckStatusAsync` (`:675-764`): کامنت وضعیت سفارش؛ اگر `newProductionStep` باشد کامنت مرحله ساخت؛ اگر وضعیت استعلام‌مرتبط باشد ردیف `ProductionOrderItemInquiry` (`:743-754`). وضعیت‌های استعلام‌مرتبط: 1906, 1908, 1909, 2202 (`:766-770`). ورود به 1904/1905 تاریخ `SendToIndustrial*` را ست می‌کند (`:776-784`).

`ProductionOrderItemWorkflowDecisionEnum` داخل همان فایل کنترلر است (`:1854-1862`): Approve=1, Reject=2, ReviseNeeded=3.

### 5.3 BOM

| HTTP | Action | نمایش | توضیح | شاهد |
|---|---|---|---|---|
| GET | `ProductionOrderItemBomPartial` | **بدون** ActionDisplayName | فرم مودال BOM | `:837-851` |
| GET | `DeleteBom` | حذف BOM | Delete | `:853-863` |
| GET | `DownloadBomExcelTemplate` | دانلود نمونه اکسل BOM | ستون‌های `PartCode`, `Amount`, `Comment` | `:865-890` |
| POST | `ImportBomFromExcel` | افزودن BOM از اکسل | سقف ۲۰MB؛ اختیاری اعمال روی اقلام مشابه؛ اعلان؛ انتقال به کارتابل PM اگر مرحله `AwaitingEngineering` و مدیر پروژه دارد | `:895-984` |
| POST | `SaveBom` | ذخیره BOM | تکراری بودن Part آخرین نسخه؛ همان منطق PM و اعلان | `:988-1063` |

منطق کمکی BOM: `MoveItemsToProjectManagerCartableAsync` (`:1068-1112`) → `ProductionStep=AwaitingProjectManager(1380)` و `CheckStatus=2258`. `ApplyBomToSimilarProductionOrderItemsAsync` (`:1117-1165`). `SendBomChangeNotificationAsync` (`:1170-1295`) صف `Notification` از نوع Email با `ViewPath = Panel/ProductionOrderItem/Edit/{id}`. شکست اعلان ذخیره BOM را متوقف نمی‌کند (`:1291-1294`).

گروه‌های اعلان در اولین ذخیره BOM در صورت نبود ساخته می‌شوند (`EnsureBomNotificationGroupsExistAsync` `:1297-1322`).

### 5.4 کامنت، سابقه، توقف، جابجایی

| HTTP | Action | نمایش | توضیح | شاهد |
|---|---|---|---|---|
| GET | `ProductionOrderItemCommentPartial` | بدون DisplayName | فرم تغییر وضعیت | `:1576-1589` |
| POST | `SaveComment` | ذخیره کامنت | در Add اگر `ProductionStatus` با وضعیت فعلی یکی باشد Exception؛ همگام‌سازی والد با Action | `:1591-1615` |
| GET | `ProductionOrderItemCommentListPartial` | بدون DisplayName | چهار تب سابقه (سفارش / استعلام / تولید / مرحله ساخت) | `:1617-1648` |
| GET | `StopRequestPartial` | بدون DisplayName | فرم `Prd.StopRequst` | `:1650-1663` |
| POST | `SaveStopRequest` | ذخیره توقف | کامنت ترجیح می‌دهد کلاینت از کنترلر اختصاصی StopRequst استفاده کند؛ fallback همین‌جاست | `:1665-1731` |
| GET | `StopRequestListPartial` | بدون DisplayName | جدول توقف‌های همان قلم | `:1733-1743` |
| POST | `Swap` | جابه جایی قلم | تعویض فیلدهای تولید/سریال/مرحله (+ اختیاری PartId) و جابجایی کامنت‌ها بین دو قلم | `:1745-1841` |

فرم اختصاصی **ثبت استعلام** (مسئول، LeadTime، پیوست) در کنترلر **وجود ندارد**. ردیف Inquiry فقط از `TransitionCheckStatusAsync` ساخته می‌شود. نمایش در تب «وضعیت استعلام» سابقه است.

امنیت ردیف قلم (`:239-255`): Admin/`ShowAll` یا ارتباط از طریق هدر سفارش (همان پنج نقش سربرگ). اعمال نشده.

---

## 6. کنترلرهای Pln مرتبط — آیا از این دو صفحه باز می‌شوند؟

| کنترلر | Route | از صفحه سفارش/اقلام باز می‌شود؟ | شاهد |
|---|---|---|---|
| `ProductionOrderDelayController` | `[Route("Panel/[controller]")]` → `/Panel/ProductionOrderDelay/...` (`ProductionOrderDelayController.cs:22-26`) | **خیر.** تب تاخیر سربرگ از `ProductionOrder/ProductionOrderDelayPartial` و `SaveProductionOrderDelay` استفاده می‌کند، نه این کنترلر. لیست مستقل `GET List` → `Views/Panel/Pln/ProductionOrderDelay/List.cshtml` (`:124-128`) | `ProductionOrder/Edit.cshtml:764-788` در برابر `ProductionOrderDelayController.cs:124-128` |
| `ProductionOrderEquipmentConfirmerController` | `/Panel/ProductionOrderEquipmentConfirmer/...` (`:14-17`) | **خیر.** اکشن‌های توکار سربرگ حذف شده‌اند (`ProductionOrderController.cs:396-397`). CRUD سراسری بر اساس `DeviceType`. Job اعلان مهندسی از همین جدول می‌خواند | `ProductionOrderJob.cs:3053-3058` |
| `ProductionOrderItemDeficitController` | `[Route("Panel/Pln/[controller]")]` → `/Panel/Pln/ProductionOrderItemDeficit/...` (`:14-17`) | **خیر.** هیچ `addPage` / لینکی در Viewهای `Sale/ProductionOrder*` به کسری نیست | جستجوی Views |
| `ProductionOrderDelayResponsibleUserController` | `/Panel/Pln/ProductionOrderDelayResponsibleUser/...` (`:13-16`) | **خیر.** تنظیم کاربران ایمیل عامل توقف برای ماژول تاخیر مستقل | — |

اکشن‌های کنترلر تاخیر مستقل (برای ثبت «وجود صفحه همسایه»، نه تحلیل فیلدبه‌فیلد): Save/Add/Update/Delete، Edit/New/List، `GetByProductionOrderId`، کاربران ایمیل عامل توقف، بازه تاخیر محاسبه‌شده، اطلاعات سفارش، `ExcludeFromDelayCycle` (ست کردن `ProductionOrder.DelayNotCalculated`)، Excel، FetchData — `ProductionOrderDelayController.cs:29-231`. دکمه Data Profile «خارج کردن از سیکل تاخیر» در `Data/Scripts/Update_ProductionOrderDelay_ExcludeFromCycle_Button.sql:45` به `/Panel/ProductionOrderDelay/ExcludeFromDelayCycle` پست می‌کند — این از **لیست تاخیرات Pln** است نه از Edit سفارش ساخت.

---

## 7. Viewها

### 7.1 سربرگ

| فایل | نقش |
|---|---|
| `Views/Panel/Sale/ProductionOrder/List.cshtml:1-2` | فقط `<datatableprofile entity-Type="typeof(ProductionOrder)">` |
| `Views/Panel/Sale/ProductionOrder/Edit.cshtml` | فرم سربرگ + دکمه‌های تایید مالی / منسوخ + چهار تب |
| `Views/Panel/Sale/ProductionOrder/_ProductionOrderDelayPartial.cshtml` | مودال افزودن/ویرایش تاخیر توکار |

تب‌های Edit (`Edit.cshtml:51-70`): اطلاعات کلی، تاریخچه کامنت هدر، پیوست‌ها، تاخیرات. **تب تاییدکننده تجهیز در View نیست.**

دکمه‌ها با نقش Razor (`:15-21`, `:28-31`): `financialConfirm`، `obsolete`. JS پست به `/Panel/ProductionOrder/DoFinancialConfirm/{id}` و `DoObsolete/{id}` (`:676`, `:717`).

فیلدهای فرم سربرگ (شاهد `data-bind` / EntitySelector در `Edit.cshtml`): `state` (disabled)، `number`، `revision`، `ContractId`، `ProjectDlId`، `projectDlCode`، `endUser`، `projectStartDate`، `metreDate`، `metreNumber`، `agreedDeliveryDate`، `SalesManagerId`، `ProjectManagerId`، `AlternativeExpertId`، `SalesAgencyId`، `InstallationCityId`، چک‌باکس‌های بازرسی/پیمانکار/کمپرسورخانه/نصب هوایار/تعهد مالی/GA/سانتریفیوژ/نیاز به مدیر پروژه/`delayNotCalculated`، `salesComment`، `comment`، فیلدهای سانتریفیوژ، `packingType`/`deliveryType`/`productType`/`centrifugeSetupType` (int خام، نه enum)، `productionOrderNumber`، `SalesExpertId`، `customerIndustry`، `edmsProject`، `IntroducerExpertId`، پیوست‌های چهارگانه.

تاریخچه هدر فقط خواندنی از `Model.Comments` (`:491-525`). تاخیر: جدول ViewBag + `data-action="addNewDelay"` / `openDelayModal` (`:571-618`).

Partial تاخیر (`_ProductionOrderDelayPartial.cshtml:8-61`): قلم، `delayDays`، `delayStartMiladiDate`، `delayEndMiladiDate`، `delayReason`، `description`. **`DelayResponsible` روی فرم توکار نیست** در حالی که روی entity الزامی است (`ProductionOrderDelay.cs:29-31`).

### 7.2 اقلام

| فایل | نقش |
|---|---|
| `List.cshtml` | datatableprofile + `#bodySawpModal` (EntitySelector قلم مقصد + چک‌باکس تعویض کالا). **اسکریپت POST به `Swap` در این فایل نیست** |
| `Edit.cshtml` | کارت اطلاعات پایه (اغلب read-only در ویرایش) + بخش «تولید» قابل ذخیره + دکمه‌های گردش کار |
| `ListByParentId.cshtml` | صفحه BOM: DataTable کلاینت، افزودن/ویرایش مودال، اکسل، حذف |
| `_ProductionOrderItemBomPartial.cshtml` | فرم BOM |
| `_ProductionOrderItemCommentPartial.cshtml` | فرم تغییر وضعیت (وضعیت تولید، بازه، FailureType، کامنت، CC) |
| `_ProductionOrderItemCommentListPartial.cshtml` | چهار تب سابقه |
| `_StopRequestPartial.cshtml` | فرم توقف |
| `_StopRequestListPartial.cshtml` | جدول توقف‌ها |

دکمه‌های Edit (`Edit.cshtml:51-77`): تغییر وضعیت، تاریخچه، ثبت توقف، لیست توقفات، Bom محصول (`appController.addPage('/Panel/ProductionOrderItem/BomListBy?...')` `:601`)، تایید/رد مهندسی، تایید صنایع، تایید/رد مدیر پروژه، ارسال به استعلام / وزارت صنایع / کمیته، تایید/رد کمیته.

فعال‌سازی دکمه بر اساس `CheckStatus` عددی (`:534-583`) با همان کدهای Lookup HTS.

بخش تولید قابل ویرایش (`:394-486`): `deviceType`، `productionStep`، `planningNumber`، `serial`، `serialType`، تاریخ شروع/پایان تولید، پایان تست، آماده‌سازی، `status`، `planningConsideration`. در حالت New: انتخاب سفارش، کالا، مقدار.

فیلدهای entity که روی Edit قلم **به‌صورت input نیستند** (فقط بعضی در کارت read-only): `HasInspection`، `SalesConsideration`، `EngineeringConsideration`، `IsRoutine`، `ProductionStatus`، `LastComment`، تاریخ تحویل/مدارک/تحویل استاندارد، اقساط، `SendToIndustrial*`، `FirstIndustrialChange*`، `HamkaranId`، `RahkaranHistoryId`. `Update` با این حال `Delivery*` / `DocumentPreparation*` / `StandardDelivery*` / `IsRoutine` / `ProductionStatus` را می‌پذیرد اگر کلاینت بفرستد (`ProductionOrderItemController.cs:88-97`) — UI فعلی آن‌ها را نشان نمی‌دهد.

### 7.3 سابقه و استعلام

`ProductionOrderItemHistoryViewModel` (`WebApp/Models/Sale/ProductionOrderItemHistoryViewModel.cs:7-19`):

| تب | منبع |
|---|---|
| وضعیت سفارش ساخت | کامنت‌هایی با `!IsForProductionMode && !IsForProductionStepStatus` |
| وضعیت استعلام | `ProductionOrderItemInquiry` |
| وضعیت تولید | `IsForProductionMode && !IsForProductionStepStatus` |
| مرحله ساخت | `!IsForProductionMode && IsForProductionStepStatus` |

Inquiry در UI سابقه: Status، ResponsibleName، پیوست دانلود، Comment (`_ProductionOrderItemCommentListPartial.cshtml:105-135`). فرم ورود مسئول/LeadTime/پیوست در این صفحات نیست.

---

## 8. موجودیت‌ها و enumها

همه از `BaseEntity` ارث می‌برند (`Id`، audit، `IsActive`). DbSet دستی لازم نیست.

### 8.1 `Sale.ProductionOrder` — `Entities/App/Sale/ProductionOrder.cs:14-323`

| فیلد | نوع / نکته | Display | خط |
|---|---|---|---|
| `State` | `ProductionOrderStateEnum` required | وضعیت | `:18-20` |
| `ContractId` / `Contract` | FK | قرارداد | `:23-27` |
| `ProjectDlId` / `ProjectDl` | `FIN.DL` | تفصیل پروژه | `:30-34` |
| `EndUser` | string 50 | کاربر نهایی | `:37-40` |
| `ProjectStartDate`, `MetreDate`, `AgreedDeliveryDate` | DateTime? | تاریخ‌ها | `:43-61` |
| `MetreNumber` | string 50 | شماره متر | `:53-56` |
| `SalesManagerId`, `ProjectManagerId`, `AlternativeExpertId`, `SalesExpertId`, `IntroducerExpertId` | User FK | نقش‌های فروش/پروژه | `:64-82`, `:238-242`, `:261-265` |
| `SalesAgencyId` | `SLS.Customer` | نمایندگی | `:85-89` |
| `BranchId` / `Branch` | `Sale.Branch` | دپارتمان فروش — **روی Edit سربرگ نیست** | `:92-96` |
| `InstallationCityId` | `Gnr.Region` | شهر نصب | `:100-104` |
| فلگ‌ها | bool | بازرسی بسته‌بندی، پیمانکار، کمپرسورخانه، نصب هوایار، تعهد مالی، GA، سانتریفیوژ، نیاز به مدیر پروژه، `DelayNotCalculated` | `:107-129`, `:198-200`, `:144-146`, `:223-225`, `:313-315` |
| `SalesComment`, `Comment` | 2048 | توضیحات | `:132-141` |
| `CentrifugeCompressorCount`, `CentrifugeElectromotorVoltage` | decimal? | | `:149-156` |
| چهار پیوست FileEntity | File | تایید مدیریت، فاکتور پیش‌پرداخت، پیش‌فاکتور، قرارداد | `:159-195` |
| `Number` | string 255 required | شماره سفارش ساخت (متنی) | `:184-187` |
| `PackingType`, `DeliveryType`, `ProductType`, `CentrifugeSetupType` | int? خام | | `:203-220` |
| `ProductionOrderNumber` | long? | شماره عددی راهکاران | `:228-230` |
| `Revision` | int? | | `:233-235` |
| `CustomerIndustry` | 1024 | | `:245-248` |
| `ProjectDlCode` | decimal? | | `:251-253` |
| `EdmsProject` | int? | | `:256-258` |
| `HamkaranId` | long? | همگام راهکاران | `:266` |
| `FinancialConfirmedById` + تاریخ شمسی/میلادی | | | `:269-283` |
| `ObsoletedById` + تاریخ | | | `:286-300` |
| `IsDisableForTimelyDeliveryReport` + Comment | | **روی Edit سربرگ نیست** | `:303-311` |
| `Items`, `Comments` | navigation | | `:317-321` |

`Sale.ProductionOrderComment` (`:326-353`): `ProductionOrderId`, `PreviousState`, `NewState`, `Comment`, `HtsId`.

`ProductionOrderStateEnum` (`Enums/ProductionOrderStateEnum.cs:8-18`): Submit=1، FinancialUnitReview=2، FinancialApproval=3، Obsolete=4.

### 8.2 `Sale.ProductionOrderItem` — `ProductionOrderItem.cs:12-384`

| گروه | فیلدها | خط |
|---|---|---|
| پیوند | `ProductionOrderId`, `PartId`, `Amount`, `AgreedDeliverDate` | `:16-52` |
| مشخصات فروش/فنی | `IsBuildInside`, `DeviceType`, `EquipmentType`, `PartModel`, `AirendOrCategory`, فشارها، `Capacity`/`Scale`/`GasType`، دما/خلوص/رطوبت/بارومتر، `MovingType`, `HasInspection`, `SalesConsideration` | `:23-129` |
| نسخه | `Revision`, `IsLatestVersion`, `Version`, `PartCode`, `HamkaranId`, `RahkaranHistoryId` | `:132-178` |
| کارتابل | `CheckStatus` + تاریخ تغییر | `:141-154` |
| تولید | `ProductionStep`, `PlanningNumber`, `Serial`, `SerialType`, تاریخ‌های تولید/تست/آماده‌سازی/تحویل/مدارک/استاندارد، اقساط ۲–۵ شمسی | `:180-264` |
| صنایع | `IsDeleted`, `SendToIndustrial*`, `FirstIndustrialChangeBy*`, `EngineeringConsideration`, `PlanningConsideration`, `IsRoutine` | `:267-351` |
| وضعیت‌ها | `Status` (579/580/581)، `ProductionStatus`, `LastComment` | `:354-367` |
| فرزند | Bom، Comments، Inquiries | `:370-382` |

`ProductionOrderItemComment` (`:386-456`): `ProductionStatus`, `ProductionStep?`, بازه شروع/پایان، `FailureTypeIds/Names`, `Comment`, `CcReciversIds/Names`, `IsForProductionMode`, `IsForProductionStepStatus`, `StopRequestId`, `HtsId`.

`ProductionOrderItemInquiry` (`:458-498`): `Status` (`CheckStatusEnum`)، `ResponsibleId`/`ResponsibleName`، `LeadTime*`، `AttachmentFileId`، `Comment`.

`ProductionOrderItemBom` (`:501-558`): `Revision`, `PartId` required، `Amount` required، `IsLatest`, `Description`, `SaleUnitDetails`, `NeedsAVL`, `ProductionStep` (`BomProductionStepEnum`)، `NumberSupplied`, `Status` (`BomEnum` 579/580/581).

### 8.3 Enumهای قلم (مقادیر = کد Lookup HTS مگر نشده)

- `ProductionOrderItemCheckStatusEnum` — `CheckStatusEnum.cs:6-39` (1903…2261؛ **اعضای برقی 2196/2199/2200 اینجا نیستند**).
- `ProductionOrderItemProductionStatusEnum` — شامل هم وضعیت تولید (238) و هم کدهای کارتابل (254) به‌علاوه 2196/2199/2200 برقی — `ProductionStatusEnum.cs:8-117`.
- `ProductionOrderItemProductionStepEnum` — `ProductionStepEnum.cs:8-75` (210…2744).
- `ProductionOrderItemDeviceTypeEnum` — `DeviceTypeEnum.cs:6-49`.
- `ProductionOrderItemEquipmentTypeEnum` — اصلی 1947 / متعلقات 1948.
- `ProductionOrderItemSerialTypeEnum` — پیشوند سریال، مقادیر 1309…3015.
- `ProductionOrderItemStatusEnum` و `ProductionOrderItemBomEnum` — هر دو 579/580/581.
- `ProductionOrderItemBomProductionStepEnum` — همان مقادیر مرحله ساخت قلم.

### 8.4 Pln (مرتبط)

`Pln.ProductionOrderDelay` (`Entities/App/Pln/ProductionOrderDelay.cs:12-70`): `ProductionOrderId`, `ProductionOrderItemId`, `DelayResponsible`, `DelayDays`, `DelayReason`, بازه تاخیر، `Description`.

`DelayResponsibleEnum` — `Entities/App/Pln/Enums/DelayResponsibleEnum.cs:8-74` (752…2540).

`Pln.ProductionOrderEquipmentConfirmer` (`:24-50`): تنظیم **سراسری** `DeviceType` + `MechanicalUserId` + `ElectricalUserId` + `Comment`. چند ردیف per DeviceType مجاز است.

`Pln.ProductionOrderItemDeficit` (`:16-163`): کسری، پیشرفت، اولویت، تاریخ‌های خرید/تامین، متولی خرید — **از صفحات این محدوده باز نمی‌شود.**

`Pln.ProductionOrderDelayResponsibleUser` (`:12-24`): یک ردیف یکتا per `DelayResponsible` با لیست UserIds/Emails.

---

## 9. EntityActionها — `WebApp/Actions/Sale/*ProductionOrder*`

همه از Repository بعد از Add/Update/Delete صدا می‌شوند؛ کنترلر آن‌ها را صدا نمی‌زند.

| کلاس | Trigger | Name | توضیح فارسی attribute | کار واقعی | شاهد |
|---|---|---|---|---|---|
| `ProductionOrderItemAction` | AfterAdd | `RenumberProductionOrderItemRevision` | شماره‌گذاری نسخه قلم پس از درج | `Revision` از 1- برای خواهر و برادر `ProductionOrderId+PartId`؛ اقساط از `PreparationMiladiDate` +30/60/90/120 | `ProductionOrderItemAction.cs:16-95` |
| همان | AfterUpdate | `RenumberProductionOrderItemRevisionUpdate` | | همان | `:21-24` |
| همان | AfterDelete | `RenumberProductionOrderItemRevisionDelete` | | فقط شماره‌گذاری | `:26-29` |
| `ProductionOrderItemBomAction` | AfterAdd/Update/Delete | `SyncBomVersionInfo*` | همگام‌سازی نسخه BOM | `Revision` از 1-؛ `IsLatest` فقط روی max Id | `ProductionOrderItemBomAction.cs:14-60` |
| `ProductionOrderItemCommentAction` | AfterAdd/Update/Delete | `SyncProductionOrderItemFromComment*` | همگام‌سازی وضعیت قلم از کامنت | اگر وضعیت در لیست تولید (238+3166/3167) → `IsForProductionMode` + `ProductionStatus`/`LastComment`؛ اگر مقدار در `CheckStatusEnum` تعریف شده → `CheckStatus`؛ مهندسی 2197/2198/2259/2260 → `EngineeringConsideration`؛ سپس همیشه آخرین کامنت production-mode | `ProductionOrderItemCommentAction.cs:49-142` |
| `ProductionOrderCommentAction` | AfterAdd/Update/Delete | `SyncProductionOrderStateFromComment*` | همگام‌سازی State هدر | آخرین کامنت فعال با `NewState` → `ProductionOrder.State` | `ProductionOrderCommentAction.cs:15-52` |
| `ProductionOrderItemInquiryAction` | AfterAdd/Update/Delete | `SyncBuyStatusFromInquiry*` | همگام‌سازی CheckStatus از استعلام | آخرین Inquiry فعال → `CheckStatus` + تاریخ تغییر | `ProductionOrderItemInquiryAction.cs:17-58` |

تریگرهای HTS معادل در کامنت کلاس‌ها آمده‌اند (شماره‌گذاری نسخه قلم، Serial اقساط، Bom version، LastStatus کامنت، UpdateProductionOrderStatus، BuyStatus استعلام).

---

## 10. جاب — `App.BackgroundJob/Jobs/Sale/ProductionOrderJob.cs`

کلاس: `ProductionOrderJob(RahkaranDbContext, HtsDbContext, IUnitOfWork, ApplicationDbContext)` — `:27`. زمان‌بندی در پنل Job است نه در attribute.

| `[JobHandler]` | متد C# | کار | شاهد |
|---|---|---|---|
| افزودن سفارش ساخت از راهکاران | `AddProductionOrderFromRahkaran` | همگام هدر از `RahkaranSale_ProductionOrder` (نگاشت Contract/DL/Customer/Region/Personel→User) سپس اقلام/نسخه‌ها از راهکاران | `:31-973` |
| افزودن Bom سفارش ساخت از Hts | `AddProductionOrderItemBomFromHts` | BOM از HTS روی اقلام App بر اساس شماره سفارش + کد کالا؛ batch | `:976-1252` |
| بررسی و تغییر وضعیت قلم سفارش ساخت | `CheckFinancialConfirmsOrders` | اقلام `IsLatest` با هدر `FinancialApproval` و `CheckStatus=InitialRegistration` را `RouteInitialRegistrationItems` می‌کند؛ کامنت هدر «آغاز فرآیند»؛ اعلان مهندسی/PM | `:1257-1321` |
| اعمال سفارش‌های تایید مالی بدون آغاز فرآیند | `ApplyUnConfirmedProductionOrders` | هدرهای تایید مالی بدون کامنت خودکار + روت اقلام ثبت اولیه | `:1323-1378` |
| محاسبه و بروزرسانی تاریخ تحویل استاندارد اقلام سفارش ساخت | `AddOrUpdateStandardDeliveryDates` | LeadTime/BOM/KW از نام کالا؛ معادل HTS `CalculateStandardDeliveryDate` + پنجره تعطیلات پایان سال | `:1380-1585`, `ApplyStandardDeliveryDate` `:2994-3013` |
| تغییر وضعیت اقلامی که بیش از تایم مشخصی در کارتابل مدیر پروژه بوده اند | `SendHoldedItemsToIndustrialUnitThatWasInProjectManagerCartable` | مهلت **۲ روز** از `CheckStatusChangedOnMiladiDate`؛ → صنایع داخلی + `AwaitingIndustry` | `:1588-1666` (`deadLineInDay = 2` `:1595`) |
| انتقال خودکار اقلام منقضی‌شده در کارتابل مهندسی به صنایع | `SendExpiredEngineeringItemsToIndustrial` | مهلت **۱ روز**؛ → 1904 + `AwaitingProduction` | `:1669-1746` |
| اطلاع‌رسانی اقلام سفارش ساخت نزدیک به تاریخ تحویل | `SendProductionOrderItemsWithNearDeliveryDateNotifications` | `AgreedDeliverDate` در ۱۵ روز آینده، به‌جز مراحل پایان‌یافته | `:1749-1799` |
| کات‌اور یک‌باره سفارش ساخت از HTS (وضعیت، فیلدهای عملیاتی و کامنت‌ها) | `SyncProductionOrderOperationalFieldsFromHts` | یک‌باره/قابل تکرار؛ تطبیق `RahkaranHistoryId`↔HTS `RahkaranId`؛ upsert کامنت با `HtsId`؛ دوطرفه نیست | `:1801-1808` |

روت اولیه (`RouteInitialRegistrationItems` `:2909-2966`):

1. اگر `Part.EngineeringRoutine` یا `DeviceType` ∈ {CNG، قطعه یدکی} → کارتابل صنایع + `AwaitingProduction`
2. وگرنه اگر هدر `ProjectManagerId` دارد → کارتابل مدیر پروژه
3. وگرنه → انتظار مهندسی مکانیک (`2195`) + `AwaitingEngineering`

اعلان مهندسی از `Pln.ProductionOrderEquipmentConfirmer` بر اساس DeviceType (`SendEngineeringNotificationAsync` `:3036+`).

`SendNearDeliveryDateNotification` (`:3198-3206`) **فقط لاگ Job** است؛ Notification نمی‌سازد (TODO صریح).

---

## 11. نقش‌ها و Data Profile

منبع: `Data/Scripts/Seed_ProductionOrderItem_DataProfiles.sql` (Idempotent) + `Seed_ProductionOrderItem_MyCartable_DataProfile.sql`.

### 11.1 Roleهای seed

| Id | Name | Title |
|---|---|---|
| 200020 | `Sale.ProductionOrder.ShowAll` | فروش - سفارش ساخت - مشاهده همه |
| 200021 | `Sale.ProductionOrder.FinancialConfirm` | فروش - سفارش ساخت - تایید مالی |
| 200022 | `Sale.ProductionOrder.Obsolete` | فروش - سفارش ساخت - منسوخ‌سازی |
| 200023 | `Sale.ProductionOrderItem.AcceptIndustrial` | فروش - اقلام سفارش ساخت - تایید صنایع |
| 200024 | `Sale.ProductionOrderItem.AcceptEngineeringMechanical` | … تایید مهندسی مکانیک |
| 200025 | `Sale.ProductionOrderItem.AcceptEngineeringElectrical` | … تایید مهندسی برق |
| 200026 | `Sale.ProductionOrderItem.AcceptProjectManager` | … تایید مدیر پروژه |
| 200027 | `Sale.ProductionOrderItem.Inquirer` | … استعلام |
| 200028 | `Sale.ProductionOrderItem.MinistryOfIndustryInquirer` | … استعلام وزارت صنایع |
| 200029 | `Sale.ProductionOrderItem.SupplyCommitteeBoss` | … رئیس کمیته تامین |
| 200030 | `Sale.ProductionOrderItem.ViewRelated` | … مشاهده مرتبط |
| 200031 | `Sale.ProductionOrderItem.History` | … پیشینه |
| 200032 | `Sale.ProductionOrderItem.ViewActive` | … مشاهده فعال |

شاهد ایجاد: `Seed_ProductionOrderItem_DataProfiles.sql:13-154`. بخش `[4/4]` کاربران HTS را با Username به این نقش‌ها می‌چسباند (`:319-552`). **انتساب کاربر ≠ ایجاد Role در پنل؛ نقش در SQL ساخته می‌شود.** کامنت‌های `TODO(Role)` در کنترلر/ویو هنوز می‌گویند «نقش را در پنل بساز» — با seed فعلی ناهماهنگ‌اند (نقش seed شده؛ انتساب عملیاتی ممکن است ناقص باشد).

**هیچ seed نقش/نمایه برای لیست سربرگ `ProductionOrder` یافت نشد** (فقط اقلام). `List.cshtml` سربرگ به datatableprofile روی `typeof(ProductionOrder)` وابسته است؛ SavedQuery پایه آن در این اسکریپت‌ها نیست.

### 11.2 نمایه‌های اقلام (کپی از SavedQuery Id=104)

پایه: `system.SavedQuery` Id=104 با نام مستند `productionorderitem_listinfonew` (`Seed_ProductionOrderItem_MyCartable_DataProfile.sql:20`). اسکریپت اول Filters را جایگزین می‌کند و `CustomActionButtonsJson` / `EventScriptsJson` را **عیناً از 104 کپی** می‌کند (`Seed_ProductionOrderItem_DataProfiles.sql:175-176`, `:229-230`). JSON دکمه‌ها در ریپو نیست.

| Name | Title | فیلتر کلیدی | RoleAccess |
|---|---|---|---|
| `POI_All_Active` | اقلام سفارش ساخت — همه فعال | `IsDeleted=false` AND `IsLatestVersion=true` | ShowAll |
| `POI_ViewActive` | مشاهده فعال | همان | ViewActive |
| `POI_Cartable_Industrial` | کارتابل صنایع | CheckStatus in 1904,1905 | AcceptIndustrial |
| `POI_Cartable_Engineering_Mechanical` | کارتابل مهندسی مکانیک | 2195 AND DeviceType notIn 2403,2404,2212,2211 | AcceptEngineeringMechanical |
| `POI_Cartable_Engineering_Electrical` | کارتابل مهندسی برق | 2195 AND DeviceType in همان چهار مقدار | AcceptEngineeringElectrical |
| `POI_Cartable_ProjectManager` | کارتابل مدیر پروژه | 2258 | AcceptProjectManager |
| `POI_Cartable_Inquiry` | کارتابل استعلام | 1906 | Inquirer |
| `POI_Cartable_Ministry` | کارتابل وزارت صنایع | 1908 | MinistryOfIndustryInquirer |
| `POI_Cartable_SupplyCommittee` | کارتابل رئیس کمیته تامین | 1909,2202 | SupplyCommitteeBoss |
| `POI_Sales_Related_Active` | مرتبط با من | فقط حذف‌نشده + آخرین نسخه (**فیلتر شخص جاری در JSON نیست**) | ViewRelated |
| `POI_History_Deleted` | پیشینه حذف‌شده | `IsDeleted=true` | History + ShowAll |

`RoleAccess.ActionAccessType = 3` (DataProfile)، Path = `dataProfile_{SavedQueryId}` — `:292-317`.

### 11.3 `POI_MyCartable`

`Seed_ProductionOrderItem_MyCartable_DataProfile.sql`: WHERE پویا بر اساس `RoleIds` کاربر جاری (`@CurrentUserId`) + `ProjectManagerId` هدر.

شاخه‌ها (`:117-183`): صنایع 1904/1905 فقط اگر `ProductionStep` برابر 2744 یا 0؛ مهندسی مکانیک/برق؛ PM (نقش یا `ProjectManagerId=@CuId`) با 2258؛ استعلام 1906؛ وزارت 1908؛ کمیته 1909/2202؛ **اقلام سفارش‌هایی با `State=2` برای نقش FinancialConfirm** (کامنت اسکریپت: عملیات تایید مالی از صفحه اقلام دیده می‌شود — در حالی که دکمه تایید مالی روی **Edit سربرگ** است).

RoleAccess برای ShowAll، Financial، Industrial، دو مهندسی، PM، Inquirer، Ministry، Committee — `:249-259`.

**این SQL از `ProductionOrderEquipmentConfirmer` در WHERE استفاده نمی‌کند** (برخلاف کامنت entity Confirmer). تشخیص برق/مکانیک با DeviceType ثابت است.

---

## 12. گروه‌های اعلان BOM

| Code | Title ساخته‌شده در runtime | نقش |
|---|---|---|
| `Sale.ProductionOrderItemBom.Industrial` | سفارش ساخت - BOM - واحد صنایع | گیرندگان To وقتی PM notify نمی‌شود | `ProductionOrderItemController.cs:38`, `:1305-1311` |
| `Sale.ProductionOrderItemBom.Engineering` | سفارش ساخت - BOM - واحد مهندسی | همیشه CC | `:39`, `:1314-1320` |

ایجاد: `notificationGroupService.CreateGroupAsync` در اولین `SaveBom`/`ImportBomFromExcel` موفق که به اعلان برسد. **seed SQL برای این دو گروه در `Data/Scripts` نیست.**

علاوه بر گروه: CC ذینفعان هدر (SalesManager/Expert/Alternative/Introducer/PM) — `:1324-1340`. اگر `shouldNotifyProjectManager`، To = ایمیل مدیر پروژه.

صف: `Entities.Base.Notification.Notification` با `Type = Email`, `IsSend = false` (`:1275-1287`).

---

## 13. گردش کار سربرگ و قلم (آنچه کد انجام می‌دهد)

```
سربرگ: Submit(1) / FinancialUnitReview(2)
        --DoFinancialConfirm--> FinancialApproval(3)  [کامنت هدر؛ State از Action]
        --DoObsolete--> Obsolete(4)  فقط از 3؛ اقلام Latest → Deprecated + Canceled

قلم پس از تایید مالی (Job):
  روتین / CNG / یدکی → 1904 + AwaitingProduction
  وگرنه اگر PM روی هدر     → 2258 + AwaitingProjectManager
  وگرنه                     → 2195 + AwaitingEngineering

مهلت Job: PM 2 روز → صنایع؛ مهندسی 1 روز → صنایع.
BOM در AwaitingEngineering + وجود PM → کارتابل PM.
```

`AcceptIndustrial` CheckStatus را عوض نمی‌کند؛ کارتابل صنایع در `POI_MyCartable` با `ProductionStep∈{0,2744}` محدود شده تا اقلام قدیمی 1904 بعد از تایید صنایع در کارتابل نمانند.

---

## 14. آنچه در این محدوده **نیست** (شاهد منفی)

| مورد | شاهد |
|---|---|
| Seed منو | هیچ فایل `Seed_*Menu*.sql` برای ProductionOrder |
| صفحه سریال جدا / اقساط جدا / متره قیمت / قطعه یدکی روتین به‌عنوان صفحه فرزند | فقط فیلد روی entity / اقساط در Action |
| فرم استعلام با مسئول و پیوست | Inquiry فقط از Transition + تب سابقه |
| تب تاییدکننده تجهیز روی Edit سربرگ | حذف‌شده در کنترلر `:396-397`؛ در View نیست |
| لینک به کسری / گزارش تحویل به‌موقع / تأییدکننده تجهیز از این دو صفحه | جستجوی Views |
| Data Profile seed برای خود `ProductionOrder` List | فقط POI_* |
| `FetchData`/`ExportToExcel` عملیاتی از Repository | `Repository.cs:746-750`, `:781-783` |
| اعلان نزدیک تحویل واقعی | `ProductionOrderJob.cs:3200-3205` |
| `DelayResponsible` روی partial تاخیر توکار | `_ProductionOrderDelayPartial.cshtml` در برابر `ProductionOrderDelay.cs:29-31` |
| فیلد `BranchId` و `IsDisableForTimelyDeliveryReport` روی Edit سربرگ | entity هست، View نیست |
| اسکریپت `Swap` در `List.cshtml` | فقط HTML مودال؛ احتمالاً EventScripts دیتابیس Id=104 |

---

## 15. TODO / کامنت پیاده‌سازی‌نشده در کد

| محل | متن / اثر |
|---|---|
| `ProductionOrderController.cs:198-204` | `FetchDataAsync` = `NotImplementedException`؛ predicate ردیف آماده و بی‌اثر |
| همان `:218`, `:272`, `:415` | `TODO(Role)` ساخت/انتساب نقش مالی، منسوخ، ShowAll در پنل — نقش‌ها در seed SQL وجود دارند |
| `ProductionOrderItemController.cs:223-229` | همان محدودیت FetchData |
| همان `:242` | TODO(Role) ShowAll روی اقلام |
| همان `:261` | TODO نهایی‌کردن لیست DeviceType برقی با واحد مهندسی |
| همان `:302`, `:309`, `:416`, `:472`, `:542`, `:561`, `:591`, `:621`, `:649` | TODO(Role) برای هر اکشن گردش کار |
| `ProductionOrderItem/Edit.cshtml:21-43`, `:556` | همان TODO(Role) سمت Razor/JS |
| `ProductionOrder/Edit.cshtml:16-20` | TODO(Role) مالی و منسوخ |
| `ProductionOrderJob.cs:3200` | سیم‌کشی `INotificationService`/`NotificationGroupService` برای نزدیک‌تحویل انجام نشده؛ فقط لاگ |
| `ProductionOrderJob.cs:1668`, `:1748` | زمان‌بندی Job باید در پنل ادمین ست شود |
| `FixProductionOrderEquipmentConfirmer.sql:17` | اشاره به TODO خالی نسخه‌های قبلی مدل تاییدکننده |
| `AcceptIndustrial` `:433-434` | صریح: عضو CheckStatus برای «تایید صنایع» وجود ندارد |
| `CommitteeAccept` `:625-627` | هیچ اکشنی CheckStatus را به `2202` نمی‌برد؛ `SendToSupplyCommittee` به `1909` می‌برد؛ هر دو به‌عنوان پیش‌وضعیت قبول می‌شوند |
| سازگاری enum مهندسی `:276-282` | CheckStatus برق از کدهای مکانیکی 2195/2197/2198 استفاده می‌کند |

`GetRowSecurityPredicate` هر دو کنترلر برای آینده مستند شده و در runtime لیست Data Profile اثری ندارد.

---

## 16. فایل‌های کمکی (خارج از رفتار runtime صفحات)

| فایل | نقش |
|---|---|
| `Docs/ProductionOrder-UserGuide.html`, `ProductionOrderUserGuide.html`, `ProductionOrderScreenshotGuide.html` | راهنمای کاربر؛ منبع موجودی این سند نیستند |
| `Data/Scripts/ProductionOrderItem_DataProfiles_Guide.html` | راهنمای اجرای seed نمایه |
| اسکریپت‌های Alter/Add ستون (`AddProductionOrderOperationalFields.sql`, `AddProductionOrderCommentHtsId.sql`, `AddProductionOrderItemRahkaranHistoryId.sql`, `Alter_ProductionOrderItem_PlnTriggersColumns.sql`) | مهاجرت ستون؛ رفتار UI را تعریف نمی‌کنند |
| `Entities/Hts/Pln/Hts_Pln_ProductionOrder*.cs` و `Entities/Rahkaran/USR3/RahkaranSale_ProductionOrder*.cs` | مدل خواندن HTS/راهکاران برای Job |

---

*پایان موجودی Havayar. مقایسه با HTS در `03-Gap-Register.md` پس از تکمیل `01-HTS-Inventory.md`.*
