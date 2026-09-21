# سفارش ساخت و اقلام — استخراج HTS (سیستم قدیم)

> فقط مستندسازی. هیچ کدی، منو، جاب یا SQL در این مرحله تغییر نکرده است.  
> تاریخ استخراج از سورس: **2026-09-15**. شواهد دیتابیس زنده از modernization v2/v3: **2026-09-07 / 2026-09-08**.  
> اتصال زنده MSSQL (`user-mssql-totalsystem` / `erps` / `havayar`) در زمان نگارش در وضعیت loading/error بود؛ Lookupها از `lookups.json` و `SystemPage` از enum سورس است، نه `Gnr_Page` زنده.

منابع پایه (بازنویسی نمی‌شوند):

| منبع | نقش |
|---|---|
| `docs/modernization/production-order-item-workflow-v2.md` | نقشه گردش قلم، Trigger، ابهام‌ها |
| `docs/modernization/production-order-live-findings-v3.md` | تأیید مالی راهکاران، Jobها، History |
| `docs/modernization/evidence/database-export-2026-09-07/` | تعریف SP/Trigger/Lookup |
| `docs/modernization/evidence/database-live-20260908-084254/` | Jobهای راهکاران enabled=1 |

ریشه سورس HTS: `D:\Projects\Hts Project\Hts Project\HtsProject\`. شاهد جدول‌ها به‌صورت `path:line` نسبت به همین ریشه است.

## 1. محدوده این سند

**داخل محدوده (دو صفحه زنده + فرزندان داخل صفحه):**

| صفحه زنده | href منو | `SystemPage` | کنترلر / اکشن بارگذاری |
|---|---|---|---|
| اقلام سفارش ساخت | `planning-productionOrderItem` | `Pln_ProductionOrder_Itm_Management = 214` | `ProductionOrderItemController.LoadOriginalPage` |
| سفارش ساخت | `planning-productionOrder` | `Pln_ProductionOrder = 463` | `ProductionOrderController.LoadPage` |

فرزندان داخل همین دو صفحه: گردش تأیید (`DoItemCommentOperation` / `DoOriginalOperation` / `DoChangeProductionStatusOperationOriginal` / `DoInquiryOperation`)، BOM، کامنت/سابقه، استعلام، پیوست سربرگ، سریال روی قلم زنده، جابه‌جایی قلم، توقف تولید، اعلان‌ها، جاب‌ها، تریگرها، دسترسی‌ها.

**همسایه‌های منو (فقط یک‌خط وضعیت «هست در HTS» — بدون تحلیل فیلدبه‌فیلد):** گزارش BOM، گزارش کامنت، کسری، تاخیر مستقل، گزارش تاخیر، تحویل به‌موقع، تأییدکننده تجهیز.

**خارج از محدوده:** درخت منوی سایر سیستم‌های HTS؛ مقایسه با Havayar (فایل‌های 02/03).

سه لایه موجودیت هم‌زمان در سورس وجود دارد؛ فقط لایه **Original** صفحه زنده 214 است:

| لایه | جدول | صفحه زنده؟ |
|---|---|---|
| Legacy Hdr/Itm | `Pln_ProductionOrder_Hdr` / `Pln_ProductionOrder_Itm` + Serial/Installment/Metre/Attachment | خیر — منوی `planning-productionOrderItemManagement` کامنت شده؛ `LoadPage()` |
| Legacy New | `Pln_ProductionOrder_Itm_New` | خیر — منوی `planning-productionOrderItemNewManagement` کامنت شده؛ `LoadPage(true)` |
| **Original (زنده)** | `Pln_ProductionOrder` / `Pln_ProductionOrderItem` | بله |

---

## 2. مسیر منو، `SystemPage`، دسترسی، آیتم‌های کامنت‌شده

`SystemType.Planning = 6`. منوی فروش `SystemType.MainSaleSystem`. سفارش ساخت در منوی فروش با `HasAccess(SystemType.Planning, SystemPage.Pln_ProductionOrder)` چک می‌شود نه با SystemType فروش.

### 2.1 اقلام — سیستم برنامه‌ریزی (زنده)

مسیر: **سیستم برنامه‌ریزی** → **عملیات / گزارشات** → **اقلام سفارش ساخت**.

| مورد | مقدار | شاهد |
|---|---|---|
| برچسب منو | `CaptionsLibrary.ProductionOrderItems` = اقلام سفارش ساخت | `Infrastructure/Hts.Core/Resources/CaptionsLibrary.fa-IR.resx:13575` |
| href | `planning-productionOrderItem` | `_PlanningSystemMenu.cshtml:138` |
| شرط نمایش | `user.HasAccess(systemType, SystemPage.Pln_ProductionOrder_Itm_Management)` | `_PlanningSystemMenu.cshtml:135` |
| `SystemPage` | `214` | `Infrastructure/Hts.Core/Enums/Pages.cs:169` |
| گروه والد منو | اگر کاربر یکی از صفحات 463/214/469/224/241/362/363/418/… را داشته باشد | `_PlanningSystemMenu.cshtml:99` |

BOM report بلافاصله زیر همان شرط 214 رندر می‌شود (همسایه؛ بدون `SystemPage` جدا در منو): `_PlanningSystemMenu.cshtml:144`.

### 2.2 سفارش ساخت — سیستم فروش / MainSale (زنده)

مسیر: **سیستم فروش** → **عملیات** → **سفارشات ساخت**.

| مورد | مقدار | شاهد |
|---|---|---|
| برچسب منو | `CaptionsLibrary.ProductionOrders` = سفارشات ساخت | `CaptionsLibrary.fa-IR.resx:13638` |
| href | `planning-productionOrder` | `_MainSaleSystemMenu.cshtml:159` |
| شرط نمایش | `user.HasAccess(SystemType.Planning, SystemPage.Pln_ProductionOrder)` | `_MainSaleSystemMenu.cshtml:156` |
| `SystemPage` | `463` | `Pages.cs:405` |
| گروه والد | `pageIds` شامل 463 | `_MainSaleSystemMenu.cshtml:21` |

عنوان تب از `MessageTranslator` با همان href: `Infrastructure/Hts.Web.Core/General/Helpers/ApplicationHelpers/MessageTranslator.cs:410`.

### 2.3 مسیریابی href → اکشن

`HomeController` روی `PartialTitle` سوئیچ می‌کند:

| href | `PartialTitle` | اکشن | شاهد |
|---|---|---|---|
| `planning-productionOrderItem` | `PlanningProductionOrderItem` | `ProductionOrderItem.LoadOriginalPage()` | `HomeController.cs:662` |
| `planning-productionOrder` | `PlanningProductionOrder` | `ProductionOrder.LoadPage()` | `HomeController.cs:659` |
| `planning-productionOrderItemManagement` (کامنت) | `PlanningProductionOrderItemManagement` | `ProductionOrderItem.LoadPage()` | `HomeController.cs:632` |
| `planning-productionOrderItemNewManagement` (کامنت) | `PlanningProductionOrderItemNewManagement` | `ProductionOrderItem.LoadPage(true)` | `HomeController.cs:635` |

`_Footer.cshtml:1523` فقط برای `planning-productionOrder` پنجره‌های Kendo را روی ترک صفحه destroy می‌کند.

### 2.4 آیتم‌های کامنت‌شدهٔ مرتبط در منوی برنامه‌ریزی

همه در `_PlanningSystemMenu.cshtml`:

| href | برچسب | `SystemPage` | خطوط | وضعیت |
|---|---|---|---|---|
| `planning-productionOrder` | سفارشات ساخت | `463` | `124–132` | **کامنت** در برنامه‌ریزی؛ زنده در MainSale |
| `planning-productionOrderItemManagement` | مدیریت اقلام (legacy Hdr/Itm) | `214` | `173–181` | کامنت |
| `planning-productionOrderItemNewManagement` | مدیریت اقلام New | `214` | `183–191` | کامنت |
| `planning-cashFlowManagement` | جریان نقد | `224` | `204–212` | کامنت (خارج از این دو صفحه) |
| `planning-factoryBudget` | بودجه کارخانه | `241` | `214–221` | کامنت |

صفحهٔ `Pln_ProductionOrder_Management = 226` در منو به **مدیریت قرارداد** (`planning-contractHeader`) وصل است نه به سفارش ساخت زنده: `_PlanningSystemMenu.cshtml:54`. ویوی قدیمی `_ProductionOrderHeader.cshtml` هنوز به 226 / اقساط / پیوست Hdr وابسته است و صفحه زنده 463 نیست.

### 2.5 همسایه‌های منو (فقط وضعیت وجود در HTS)

| برچسب | href | `SystemPage` | منو | کنترلر | هست در HTS |
|---|---|---|---|---|---|
| گزارش Bom اقلام سفارش ساخت | `planning-productionOrderItemBomReport` | منو با 214؛ اکشن با `508` | برنامه‌ریزی عملیات | `ProductionOrderItemBomReportController` | بله |
| گزارش کامنت/توقف اقلام سفارش ساخت | `planning-productionOrderItemComment` | `469` | برنامه‌ریزی عملیات | `ProductionOrderItemCommentController` | بله |
| گزارش کسری | `planning-productionOrderItemDeficitReport` | `546` | برنامه‌ریزی عملیات | `ProductionOrderItemDeficitReportController` | بله |
| تاخیر/توقف سفارشات ساخت | `planning-productionOrderDelay` | `362` | برنامه‌ریزی عملیات | `ProductionOrderDelayController` | بله — **از داخل 214/463 باز نمی‌شود** |
| گزارش تاخیر/توقفات سفارشات ساخت | `planning-productionOrderDelayReport` | `418` | برنامه‌ریزی عملیات | `ProductionOrderDelayReportController` | بله |
| گزارش تحویل بموقع سفارشات | `planning-timelyDeliveryReport` | `363` | برنامه‌ریزی عملیات | `PlnTimelyDeliveryReportController` | بله |
| تاییدکنندگان تجهیزات | `planning_ProductionOrderEquipmentConfirmer` | `557` | فروش عملیات | `ProductionOrderEquipmentConfirmerController` | بله — از صفحه 214 باز نمی‌شود؛ DeviceType قلم به این جدول JOIN می‌شود |

شاهد منوها: `_PlanningSystemMenu.cshtml:144,194,224,234,244,254` و `_MainSaleSystemMenu.cshtml:165`.

---

## 3. صفحات / ویوها / partialها و آنچه نشان می‌دهند

### 3.1 اقلام زنده — `_ProductionOrderItemOriginal.cshtml`

بارگذاری: `LoadOriginalPage` با `[PermissionAuthorize(Planning, 214)]` — `ProductionOrderItemController.cs:149`.  
`ViewBag.hasInPorductionOrderItemSellTeam` = عضویت در گروه کاربری **596** (اقلام سفارش ساخت - کارشناسان فروش) — همان فایل:160.

**نوار ابزار (ریبون):** `ToolbarItems` در `_ProductionOrderItemOriginal.cshtml:1674`.

| دکمه | شرط نمایش/فعال | کار |
|---|---|---|
| جدید / ویرایش / حذف / ذخیره / انصراف | حذف و جدید: `HasPlanningPermission` (80) | CRUD روی `Pln_ProductionOrderItem` |
| BOM | ردیف انتخاب‌شده + `IsLatestVersion` | پنجره `LoadBomPage` (صفحه 508) |
| تغییر وضعیت | `hasChangeStatusPermission` = تولید(132) یا QC(133) یا رئیس کمیته(137) یا استعلام‌گر(138) یا وزارت صنایع(139) یا گروه 596 | `DoChangeProductionStatusOperationOriginal` یا `DoInquiryOperation` |
| دانلود پیوست سفارش | برنامه‌ریزی یا رئیس کمیته + مسیر فایل غیرخالی | `ViewProductionOrderAttachedFile` |
| جابه‌جایی | `SwapPermission` (136) | `DoSwapOperation` |
| آماده ارسال | `ReadyToSendItemsPermission` (119) | `GetOriginalGridData(loadReadyToSendItems=true)` — همه اقلام فعال سفارش با `StatusId=579` و `ProductionStepId=221` |
| درخواست توقف | سریال غیرخالی و حذف‌نشده | `PrdStopRequest.LoadStopAddPage` |
| قطعه یدکی | همیشه پس از انتخاب | `LoadSparePartPage` |
| فیلتر پیشرفته | — | پنجره فیلتر Kendo |

شرط فعال‌شدن «تغییر وضعیت» روی کمیته هنوز **فقط `BuyStatusId === 2202`** است نه 1909: `_ProductionOrderItemOriginal.cshtml:427` و `:524`. این همان ابهام v2 (U06) است.

**گرید اصلی** (`GetOriginalGridData`): ستون‌ها شامل شعبه فروش، شماره سفارش، مشتری، کارشناس فروش، کالا، مقدار، مرحله تولید، وضعیت عمومی، سریال، نوع سریال، روتین، تاریخ‌ها، وضعیت تولید، ملاحظات، آخرین کامنت، آخرین وضعیت استعلام، نسخه/حذف، مشخصات فنی کالا، مدیر پروژه، پروژه EDMS — `:1817`.

**جزئیات ردیف (TabStrip):** `:1946`

| تب | منبع داده | معنی |
|---|---|---|
| وضعیت سفارش ساخت | `GetCommentGridData(..., isForProductionMode=false, isForProductionStepStatus=false)` | سوابق تأمین / BuyStatus |
| وضعیت استعلام | `GetInquiryGridData` | `Pln_ProductionOrderItemInquiry` + دانلود پیوست |
| وضعیت تولید | همان Comment با `isForProductionMode=true` | رخدادهای LookupType 238 + `StopRequestId` |
| مرحله تولید | Comment با `isForProductionStepStatus=true` | `ProductionStepId` |

**فیلدهای فرم چپ (PropertyGrid):** سفارش، کالا، نوع تجهیز (`DeviceType` LookupType 52)، مقدار، مرحله، شماره برنامه‌ریزی، سریال، نوع سریال، تاریخ‌های تولید/تست/آماده‌سازی/تحویل، ملاحظات برنامه‌ریزی — `:2080`. سریال روی **خود قلم** است نه جدول فرزند `Pln_ProductionOrder_Itm_Serial`.

**پنجره‌های فرزند:**

| پنجره | ویو | کنترلر |
|---|---|---|
| BOM | `ProductionOrderItmPartials/_ProductionOrderItemBom.cshtml` | `LoadBomPage` — صفحه 508؛ فروش/پروژه از `SalesExpertId` / `AlternativeExpertId` / `SalesManagerId` / `ProjectManagerId` |
| قطعه یدکی | `_ProductionOrderItemSparePart.cshtml` | `LoadSparePartPage` — **بدون** `[PermissionAuthorize]` صفحه |
| توقف تولید | `_PrdStopRequestRegister.cshtml` | `PrdStopRequestController.LoadStopAddPage:108` |

### 3.2 سفارش ساخت زنده — `_ProductionOrder.cshtml`

بارگذاری: `LoadPage` با `[PermissionAuthorize(Planning, 463)]` — `ProductionOrderController.cs:100`.

**نوار ابزار:** `:1595`

| دکمه | شرط | کار |
|---|---|---|
| CRUD سربرگ | افزودن: گروه کاربری **77** (کارشناسان فروش سفارش ساخت) | `DoOperation` |
| پنج پیوست (تأیید مدیریت، ودیعه، پیش‌فاکتور، قرارداد، مهندسی/متره) | `ViewAttachment` (20) یا مهندسی | `DownloadFiles` |
| چاپ | همیشه پس از انتخاب | `LoadReport` + `PermissionType.Print` روی اکشن |
| بروزرسانی همکاران | گروه 77 | `DoUpdateHamkaranOperation` — اگر قبلاً 2057 ثبت شده باشد رد می‌شود |
| منسوخ | گروه 77 | `DoObsoleteOperation` |
| تأیید/رد مهندسی (ریبون) | کامنت‌شده | `:1629` |
| تأیید/رد روی ردیف قلم فرزند | دکمه گرید `EngineeringFeedback` | `DoItemCommentOperation` |

**گرید سربرگ** (`GetGridData`): آخرین وضعیت، Revision، شماره، قرارداد، تفصیل، پروژه EDMS، کاربر نهایی، بسته‌بندی، نوع محصول، شهر نصب، نوع تحویل، شعبه، کارشناس/مدیر پروژه، متره، پرچم‌ها، تاریخ‌ها، کامنت — `:1687`.

**جزئیات ردیف:** تب سابقه (`Pln_ProductionOrderComment`) و تب اقلام (`GetAllItemsGridData`) با دکمه تأیید/رد مهندسی — `:1764`.

**فرم سربرگ:** اعلام نیازمندی، قرارداد، پروژه EDMS، تفصیل پروژه، کاربر نهایی، تاریخ شروع پروژه، شماره/تاریخ متره، تاریخ توافقی، شعبه، کارشناس/مدیر فروش، مدیر پروژه، نوع بسته‌بندی/محصول/تحویل، شهر نصب، پرچم‌ها (مدیر پروژه، بازرسی قبل بسته‌بندی، پیمانکار، GA، خانه کمپرسور، نصب هوایار، ضمانت مالی)، توضیحات فروش/عمومی، آپلود پنج نوع فایل — از `:1858`.

تأخیر سربرگ **داخل این صفحه نیست**. موجودیت `Pln_ProductionOrder_Delay` از صفحه همسایه 362 پر می‌شود.

### 3.3 Legacy (همان کنترلر 214، منوی کامنت‌شده)

| ویو | اکشن | نشان می‌دهد |
|---|---|---|
| `_ProductionOrderItem.cshtml` | `LoadPage()` | گرید `Vw_Pln_ProductionOrder` / Hdr+Itm؛ سریال فرزند، اقساط، متره، پکیج، پیوست Itm |
| `_ProductionOrderItemNew.cshtml` | `LoadPage(true)` | جدول `Pln_ProductionOrder_Itm_New` + Excel |
| `_ProductionItmInstallmentManagement.cshtml` | `LoadProductionOrderInstallmentsManagementPage` صفحه **227** | اقساط روی `Pln_ProductionOrder_Itm` |
| پیوست Itm | `LoadProductionOrderAttachemntManagementPage` صفحه **216** | `Pln_ProductionOrder_Itm_Attachment` |
| `_ProductionOrderHeader.cshtml` | صفحه **226** (قرارداد/Hdr قدیمی) | تأیید اداری/مالی، قیمت، اقساط، پیوست Hdr |

این‌ها دیگر از منوی زنده 214/463 باز نمی‌شوند ولی اکشن‌های `Do*` مربوط هنوز روی `ProductionOrderItemController` هستند.

### 3.4 Partialهای BOM / یدکی (فرزند 214)

BOM: گرید آخرین نسخه، تاریخچه قطعه، تغییرات واحد فروش (`Pln_ProductionOrderItemBomChanges`)، Excel، اعمال روی اقلام مشابه همان سفارش، قیمت داخلی/خارجی فقط با `ShowPrice` روی 508. اگر BOM روتین خالی باشد `AddRoutineBomList` صدا می‌شود — `GetBomGridData:1603`.

---

## 4. اکشن‌های کنترلر

سطح کلاس هر دو کنترلر: `[PermissionAuthorize(SystemType.Planning)]` — یعنی حداقل دسترسی به سیستم برنامه‌ریزی. اکشن‌های بدون `[PermissionAuthorize]` صفحه، برای هر کاربر دارای Planning (پس از Session) قابل فراخوانی‌اند مگر داخل بدنه `HasAccess` چک شود.

### 4.1 `ProductionOrderItemController`

| اکشن | هدف | مجوز صریح |
|---|---|---|
| `LoadPage` | ویوی legacy Hdr/Itm یا New | صفحه 214 |
| `LoadOriginalPage` | ویوی زنده اقلام | صفحه 214 |
| `LoadBomPage` | پنجره BOM | صفحه **508** |
| `LoadSparePartPage` | پنجره یدکی | **ندارد** |
| `ViewProductionOrderAttachedFile` | دانلود پیوست سربرگ از روی قلم | **ندارد** |
| `GetGridData` | گرید legacy؛ فیلتر FullAccess/Read/Accept_* | داخل بدنه روی 214 |
| `GetNewGridData` | گرید `Itm_New` | — |
| `GetOriginalGridData` | گرید زنده؛ فیلتر نقش (بخش 8) | داخل بدنه روی 214 |
| `GetCommentGridData` | سه تب سابقه | — |
| `GetInquiryGridData` | تب استعلام | — |
| `GetProductionOrderItemMetrePriceHistoryGridData` | تاریخچه متره legacy | — |
| `GetParts` / `GetOrderPartsByOrderId` / `GetProductionOrders` / `GetSerials` | Lookup فرم | — |
| `DownloadInquiryAttachment` | فایل استعلام | — |
| **`DoOperation`** | CRUD سریال فرزند legacy (`Pln_ProductionOrder_Itm_Serial`) | صفحه 214 |
| **`DoNewOperation`** | CRUD `Itm_New` + کامنت شروع تولید 1792 | صفحه 214 |
| **`DoOriginalOperation`** | CRUD قلم زنده؛ کامنت صنایع؛ ایمیل تغییر سریال اگر بازرسی محصول دارد | صفحه 214 |
| **`DoChangeProductionStatusOperation`** | رخداد تولید روی `Itm_New` | **ندارد** |
| **`DoChangeProductionStatusOperationOriginal`** | رخداد تولید روی قلم زنده؛ تاریخ پایان تولید/تست/بسته‌بندی؛ ایمیل تست 2209 | **ندارد** |
| **`DoExcelOperation`** | ورود Excel به `Itm_New` | صفحه 214 |
| **`DoConfirmationOperation`** | تأیید صنایع/مهندسی روی **Itm قدیمی** | **ندارد** |
| **`DoChangeBuyPriceOperation`** | متره قیمت legacy | 214 + `EditPrice` (55) |
| **`DoPackageOperationUrl`** | پکیج legacy | — (بدون attribute جدا) |
| **`DoChangePrePaymentPriceOperation`** | پیش‌پرداخت legacy | 214 + `EditPrePaymentPrice` (64) |
| **`DoChangeInstallmentsCountOperation`** | تعداد اقساط legacy | 214 + `EditInstallmentsCount` (65) |
| **`DoSwapOperation`** | جابه‌جایی دو قلم (سریال/مرحله و …) | **ندارد**؛ دکمه UI با `SwapPermission` |
| **`DoInquiryOperation`** | درج استعلام گروهی + پیوست + اعلان | داخل بدنه 137/138/139 |
| `LoadProductionOrderInstallmentsManagementPage` | partial اقساط | صفحه **227** |
| `GetProductionOrderInstallmentsGridData` | گرید اقساط | — |
| **`DoProductionOrderInstallmentOperation`** | CRUD اقساط | صفحه 227 |
| `LoadProductionOrderAttachemntManagementPage` | partial پیوست Itm | صفحه **216** |
| `GetAttachmentGridData` | گرید پیوست | — |
| **`DoAttachmentOperation`** | CRUD پیوست Itm | صفحه 216 |
| `ViewAttachedFile` / `ViewAttachedFileOld` | مشاهده فایل | 216 + `ViewAttachment` |
| `GetBomGridData` / `GetBomItemHistoryGridData` / `GetBomItemChangesGridData` | گریدهای BOM | قیمت با `ShowPrice` روی 508 |
| **`DoBomOperation`** | افزودن/حذف قطعه BOM | صفحه 508 |
| **`DoBomExcelOperation`** | ورود Excel BOM | صفحه 508 |
| **`DoBomSalesUnitOperation`** | نظر واحد فروش روی BOM؛ اگر مرحله 1380 باشد → 2744 | صفحه 508 |
| `GetSparePartGridData` | یدکی | — |

شاهد امضاها: `ProductionOrderItemController.cs:131–1801`.

### 4.2 `ProductionOrderController`

| اکشن | هدف | مجوز صریح |
|---|---|---|
| `LoadPage` | ویوی زنده سربرگ | صفحه 463 |
| `DownloadFiles` | یکی از پنج نوع فایل | **ندارد**؛ UI با `ViewAttachment` |
| `GetGridData` | گرید سربرگ + فیلتر نقش | داخل بدنه 463 `ShowAll` / `EditEngineeringProject` |
| `GetItemsGridData` | اقلام (نسخه محدود) | — |
| `GetCommentsGridData` | سابقه سربرگ | — |
| `GetAllItemsGridData` | اقلام داخل جزئیات | — |
| `GetHistoryGridData` | تاریخچه | — |
| `GetParts` / `GetDls` / `GetContractHeaders` | Lookup | — |
| `GetHamkaranContractItems` | اقلام قرارداد راهکاران برای درج | — |
| **`DoOperation`** | CRUD سربرگ + ذخیره پیوست‌ها | **ندارد**؛ UI گروه 77 |
| **`DoEngineeringProjectOperation`** | فقط `EdmsProjectId` | 463 + `EditEngineeringProject` (193) |
| **`DoCommentOperation`** | سابقه سربرگ | **ندارد** |
| **`DoItemCommentOperation`** | تأیید/رد مهندسی از روی سربرگ | **ندارد** — بدنه `CommentService.DoOperation` مسیر 2197/2198/… را می‌سازد |
| **`DoUpdateHamkaranOperation`** | صدور سند همکاران + PDF + ایمیل + سابقه **2084** | **ندارد**؛ اگر 2057 موجود باشد خطا |
| **`DoObsoleteOperation`** | منسوخ سربرگ (2207) و اقلام وابسته | **ندارد** |
| `LoadReport` | چاپ DevExpress `Pln_ProductionOrderReport` | 463 + `Print` (28) |
| `AddAttachment` / `RemoveAttachment` | آپلود موقت Kendo | — |

شاهد: `ProductionOrderController.cs:101–654`.

### 4.3 کنترلرهای همسایه (یک خط)

| کنترلر | `LoadPage` مجوز |
|---|---|
| `ProductionOrderItemBomReportController` | 508 |
| `ProductionOrderItemCommentController` | 469 |
| `ProductionOrderItemDeficitReportController` | 546 |
| `ProductionOrderDelayController` | 362 |
| `ProductionOrderDelayReportController` | 418 |
| `ProductionOrderEquipmentConfirmerController` | 557 |

---

## 5. فیلدهای موجودیت

شاهد: `Data Persistence/Hts.Data.Model/MainEntities/*.cs`. فیلدهای navigation حذف شده‌اند مگر رابطهٔ مهم.

### 5.1 `Pln_ProductionOrder` — `Pln_ProductionOrder.cs:25`

| فیلد | نوع | نقش |
|---|---|---|
| `Id` | int | PK |
| `Revision` | byte | نسخه |
| `IsLatestVersion` | bool | آخرین نسخه |
| `Number` | int | شماره سفارش (تطبیق با راهکاران) |
| `ContractId` | int | FK قرارداد فروش |
| `CustomerId` | int? | مشتری |
| `DlId` / `ProjectDlId` | int? | تفصیل / تفصیل پروژه |
| `SaleProjectReportManagementId` | int? | درخواست فروش |
| `EdmsProjectId` | int? | پروژه مهندسی |
| `MetreNumber` / `MetreDate` / `MetreDateInText` | string/date | متره |
| `EndUser` | string(50) | کاربر نهایی |
| `IsNeedProjectManager` | bool | نیاز به مدیر پروژه |
| `IsNeedInspectionBeforePacking` | bool | بازرسی قبل بسته‌بندی |
| `PackingTypeId` | short? | Lookup بسته‌بندی |
| `DeliveryLocation` | string(128) | محل تحویل |
| `HasContractor` / `HasGa` | bool | پیمانکار / GA |
| `ProductTypeId` | short? | نوع محصول |
| `ProjectManagerId` | short? | پرسنل مدیر پروژه |
| `ProjectStartDate` / `ProjectStartDateInText` | date/string | شروع پروژه |
| `SalesBranchId` | byte? | شعبه |
| `SalesExpertId` / `SalesManagerId` / `SalesAgencyId` / `AlternativeExpertId` | short? | فروش |
| `InstallationCityId` | short? | شهر نصب |
| `AgreedDeliveryDate` / `AgreedDeliveryDateInText` | date/string | تحویل توافقی |
| `CompressorHouseIsReady` / `InstallationIsByHy` | bool | خانه کمپرسور / نصب هوایار |
| `DeliveryTypeId` | short? | نوع تحویل |
| `CentrifugeSetupTypeId` / `CentrifugeElectromotorVoltage` / `CentrifugeHasCompressor` / `CentrifugeCompressorCount` | — | فیلدهای سانتریفیوژ |
| `SalesComment` | string(2048) | توضیحات فروش |
| `HasFinancialGuarantee` | bool | ضمانت مالی |
| `LastStatusId` / `LastStatusUserId` / `LastStatusComment` | — | از Trigger سابقه |
| `IsDisableForTimelyDeliveryReport` / `DisableForTimelyDeliveryReportComment` | — | گزارش تحویل به‌موقع (صفحه 362) |
| `AttachmentFile*` | name/size/path | پیوست مهندسی/متره |
| `ManagementConfirmationAttachmentFile*` | — | تأیید مدیریت |
| `DepositFactorAttachmentFile*` | — | ودیعه |
| `PreFactorAttachmentFile*` | — | پیش‌فاکتور |
| `ContractAttachmentFile*` | — | قرارداد |
| `CreatedUserId` / `CreatedOrganizationUnitId` / `CreatedDate` / `CreatedDateInText` | — | ایجاد |
| `UpdatedUserId` / `UpdatedDate` / `UpdatedDateInText` | — | ویرایش |
| `Comment` | string(2048) | توضیحات |
| `RahkaranId` / `RahkaranVersion` / `RahkaranInstallationCityId` | long? | ERP |

### 5.2 `Pln_ProductionOrderItem` — `Pln_ProductionOrderItem.cs:26`

| فیلد | نوع | نقش |
|---|---|---|
| `Id` | int | PK |
| `ProductionOrderId` | int | FK سربرگ |
| `Revision` / `IsLatestVersion` | byte/bool | نسخه قلم |
| `EquipmentTypeId` | short? | نوع تجهیز کالا |
| `PartId` | long | کالا |
| `Mount` | decimal | مقدار |
| `PartModel` / `AirendOrCategory` / `Capacity` / `GasType` / `MovingType` / `Scale` | string | مشخصات فنی |
| `InputPressureBar` / `OutputPressureBar` / `PurityPercentage` / `BarometricPressure` / `MaximumTemperature` / `RelativeHumidity` | decimal? | مشخصات فنی |
| `HasInspection` | bool | بازرسی |
| `AgreedDeliverDate` / `AgreedDeliverDateInText` | date/string | تحویل توافقی قلم |
| `SalesConsideration` | string(2048) | ملاحظات فروش |
| `BuyStatusId` | short? | LookupType **254** |
| `ReceivedDate` / `ReceivedDateInText` | — | تاریخ ورود به صنایع |
| `Serial` / `SerialTypeId` | string/short? | سریال روی قلم زنده |
| `StatusId` / `Status` | short?/string | LookupType **105** (0/1/باطل) |
| `PlanningNumber` | double? | شماره برنامه‌ریزی |
| `ProductionStepId` | short? | LookupType **58** |
| `ProductionStartDate*` / `ProductionEndDate*` / `TestingEndDate*` / `PreparationDate*` / `DeliveryDate*` | Shamsi+Miladi | تاریخ‌های تولید |
| `ProductionStatusId` | short? | LookupType **238** (از Trigger کامنت) |
| `PlanningConsideration` / `LastComment` | string(1024) | ملاحظات / آخرین کامنت تولید |
| `IsDeleted` | bool | حذف منطقی (History Status=2 → 227/581) |
| `IsRoutine` / `IsBuildInside` | bool | روتین / ساخت داخل |
| `DeviceTypeId` | short? | LookupType **52** → EquipmentConfirmer |
| `DocumentPreparationDate*` | — | آماده‌سازی مدرک |
| `ContractItemId` | int? | قلم قرارداد |
| `FirstIndustrialChangeUserId` / `FirstIndustrialChangeDate*` | — | اولین تغییر صنایع |
| `RahkaranId` / `RahkaranVersion` | long? | **Id تاریخچه راهکاران** نه Id قلم ERP |
| `Created*` / `Updated*` | — | حسابرسی |
| `Comment` | string(2048) | توضیحات قلم |
| `EngineeringConsideration` | string(1024) | از Trigger برای 2197/2198/2259/2260 |
| `StandardDeliveryDate*` | — | جاب `AddOrUpdateStandardDeliveryDates` |

### 5.3 فرزندان داخل محدوده

**`Pln_ProductionOrderComment`** (`:9`): `Id`, `ProductionOrderId`, `StatusId` (LookupType 280), `CreatedUserId`, `CreatedDate`, `CreatedDateInText`, `Comment`.

**`Pln_ProductionOrder_Itm_New_Comment`** (سابقه قلم؛ جدول مشترک New و Original) (`:17`): `Id`, `ProductionOrderItmId`, `ProductionOrderItemId`, `StatusId`, `CommentDate*`, `CommentEndDate*`, `CommentStartTime`, `CommentEndTime`, `FailureTypeIds`, `FailureTypeInText`, `IsForProductionMode`, `Created*`, `Comment`, `CcReciversIds`, `CcReciversInText`, `StopRequestId`, `IsForProductionStepStatus`.

**`Pln_ProductionOrderItemInquiry`** (`:11`): `Id`, `ProductionOrderItemId`, `ResponsibleId`, `LeadTime*`, `AttachmentFileName/Size/Path`, `StatusId`, `Created*`, `Comment`. Trigger آخرین `StatusId` را روی `BuyStatusId` قلم می‌نویسد.

**`Pln_ProductionOrderItemBom`** (`:17`): `Id`, `ProductionOrderItemId`, `Revision`, `PartId`, `Amount`, `IsLatest`, `HasNeedToBuyOrBuild`, `Created*`/`Updated*`, `Comment`, `SalesUnitComment`, `HasNeedToAvl`.

**`Pln_ProductionOrderItemBomChanges`** (`:11`): `Id`, `ProductionOrderItemBomId`, `HasNeedToBuyOrBuild`, `SalesUnitComment`, `Created*`, `Comment`, `HasNeedToAvl`.

**`Pln_ProductionOrder_Delay`** (`:11`) — موجودیت تأخیر سربرگ/قلم؛ UI مستقل 362: `Id`, `ProductionOrderId`, `ProductionOrderItemId`, `ProductionOrderItemNewId`, `DelayResponsibleId`, `FromDate*`, `ToDate*`, `DelayDateDifferenceInDay`, `DelayReason`, `Created*`, `Comment`, `TrusteePersonnelId`, `JustificationComment`, `JustificationDate*`.

**`Pln_ProductionOrder_Itm_Serial`** (`:17`) — فقط legacy Itm: `ProductionOrder_Itm_Serial_ID`, `ProductionOrder_Itm_FK`, `Serial`, تاریخ‌های تولید/تست/بسته‌بندی، `PreparationDate*`, `DeliveryDate*`, تاریخ اقساط ۲ تا ۵، `Serial_Status_FK`.

**`Pln_ProductionOrder_Itm_Installment`** (`:11`) — صفحه 227: `ProductionOrder_Itm_Installment_ID`, `ProductionOrder_Itm_FK`, `Installment_Mount`, `Installment_Date_Shamsi`, `Revision`, `CreatedUser_FK`, `Created_Date`, `Created_Time`, `Comment`.

**`Pln_ProductionOrder_Itm_Attachment`** (`:11`) — صفحه 216: `ProductionOrder_Itm_Attachment_ID`, `ProductionOrder_Itm_FK`, `Attachment_FileName`, `Attachment_FileContent` (byte[]), `Attachment_FileSize`, `AttachmentFilePath`, `CreatedUser_FK`, `CreatedDate`, `CreatedTime`.

**`Pln_ProductionOrder_Hdr_Attachment`** (`:11`) — صفحه 225 روی Hdr قدیمی: `ProductionOrder_Hdr_Attachment_ID`, `ProductionOrder_Hdr_FK`, `Attachment_FileName`, `Attachment_FileContent`, `AttachmentType_FK`, `Attachment_FileSize` (computed), `CreatedUser_FK`, `CreatedDate`, `CreatedTime`.

**`Pln_ProductionOrder_Itm_Metre`** (`:11`) — تاریخچه قیمت متره legacy: `ProductionOrder_Itm_Metre_ID`, `ProductionOrder_Itm_FK`, `MetrePrice`, `CreatedUser_FK`, `CreatedDate`, `CreatedTime`, `Comment`.

**`Pln_ProductionOrder_Itm_RoutineAttachment`** (`:11`): `ProductionOrder_Itm_RoutineAttachment_ID`, `ProductionOrder_Itm_FK`, `RoutinProduct_Attachment_FK`. صفحه `251`.

**`Pln_ProductionOrderEquipmentConfirmer`** (`:11`) — همسایه 557 + JOIN از DeviceType: `Id`, `LookupId`, `MechanicalUserId`, `ElectricalUserId`, `Comment`.

توقف تولید فرزند صفحه 214 است ولی موجودیت آن `Prd_StopRequest` (سیستم تولید) است؛ `StopRequestId` روی کامنت قلم ذخیره می‌شود.

---

## 6. خلاصه گردش کار (ارجاع به v2/v3)

جزئیات کامل، ابهام‌ها و نمودار مالی در:

- `docs/modernization/production-order-item-workflow-v2.md`
- `docs/modernization/production-order-live-findings-v3.md`

اینجا فقط کدهای وضعیت کلیدی و نقطه اتصال UI/سرویس.

### 6.1 سربرگ — LookupType 280

| کد | عنوان Lookup (شواهد 2026-09-07) | enum HTS | مسیر ایجاد |
|---|---|---|---|
| 2055 | ایجاد شده | `PreRegister` | `ProductionOrderService.AddComment` هنگام Add |
| 2056 | ویرایش شده | `Modified` | همان هنگام Update |
| 2436 | خوانده شده از راهکاران | — (در enum نیست) | SP `InsertOrUpdateProductionOrders` |
| 2084 | در انتظار تایید مالی | `IssueHamkaranDocument` | Job راهکاران State=2 **یا** `DoUpdateHamkaranOperation` |
| 2057 | تایید مالی | `FinancialConfirm` | Job راهکاران State=3 |
| 2071 | تایید مالی و آغاز فرآیند ساخت | `InitialProductionOrderProcess` | WinService پس از 2057 |
| 2207 | منسوخ شده | `IsObsoleted` | Job State دیگر **یا** `DoObsoleteOperation` |

تأیید مالی در راهکاران است (مالک سیستم، v3). Trigger ERP پس از INSERT جاب `HTS_AddProductionOrderJob` و پس از UPDATE اگر `State > 1` پرچم `IsShouldUpdateHts` و جاب Comment/Email را می‌زند: `AddProductionOrderToHts_UpdateRevisionAndApplySomeChanges.sql:26,44`.

### 6.2 قلم — BuyStatus LookupType 254

| کد | عنوان Lookup | نقش در گردش |
|---|---|---|
| 1903 | ثبت اولیه | ورود از ERP / شروع |
| 1904 | در کارتابل صنایع (داخلی) | روتین / CNG / یدکی / ساخت داخل پس از مهندسی |
| 1905 | در کارتابل صنایع (خارجی) | انتخاب کمیته |
| 1906 | نیاز به استعلام | کمیته → استعلام‌گر |
| 1908 | نیاز به بررسی وزارت صنایع | کمیته → استعلام وزارت |
| 1909 | ارسال به رئیس کمیته تامین | پاسخ استعلام (Trigger همان 1909 را می‌نویسد؛ دکمه کمیته هنوز 2202 است) |
| 2195 | در انتظار تایید مهندسی (مکانیک) | پس از مدیر پروژه یا بدون مدیر پروژه |
| 2196 | در انتظار تایید مهندسی (برق) | UI؛ سرویس ادامه خودکار ندارد |
| 2197 / 2198 | تایید / عدم تایید مکانیک | سپس 1904 یا 2202 یا 2201 |
| 2199 / 2200 | تایید / عدم تایید برق | Trigger مستقیم |
| 2201 | عدم تایید مهندسی و نیاز به بازنگری | |
| 2202 | در انتظار بررسی رئیس کمیته تامین | شرط دکمه کمیته در UI |
| 2208 | منسوخ شده | قلم |
| 2258 | در انتظار تایید مدیر پروژه | اگر `ProjectManagerId` دارد |
| 2259 / 2260 / 2261 | تایید / رد / رد+بازنگری مدیر پروژه | 2259 سپس 2195 |

شروع گردش پس از 2057 روی سربرگ: `CheckFinancialConfirmsOrders` — `ProductionOrderItemService.cs:520`. ارجاع: روتین یا DeviceType CNG/یدکی → 1904؛ وگرنه با مدیر پروژه → 2258؛ وگرنه → 2195. سپس سابقه سربرگ 2071.

زنجیره کامنت بعدی: `AddCommentAndSendNotification` — `ProductionOrderItemCommentService.cs:296`.

مهلت مهندسی یک‌روزه: `SendExpiredOrdersNotification` — `:595` (پیشوند کالا 1807101، 180229، 180211، 1808101، 1809101، 1810101، 1811101). زمان‌بندی: `TwoWaysInDayTask` ساعت 9:01 و 15:01 — `HtsTaskService.cs:201,927`.

مهلت دو‌روزه مرحله `ProductionStepId=1380` → 2744: `SendHoldedItemsToIndustrialUnitThatWasInProjectManagerCartable` — `:688` داخل `PlanningSystemTask`.

### 6.3 مرحله تولید — LookupType 58 (نمونه)

210 برنامه‌ریزی، 211 بازرگانی در راه، 213 در انتظار مهندسی، 214 منتظر تولید، 215 در حال تولید، 216 تست، 220 بسته‌بندی، 221 آماده ارسال، 223 تحویل فروش، 225 تحویل امانی، 227 باطل، 228 اختتام یافته، 246 تدارکات در راه، 1373 اصلاحیه، 1379 متوقف، 1380 در انتظار مدیر پروژه، 2744 در انتظار صنایع.

وضعیت عمومی LookupType **105**: 579=0، 580=1، 581=باطل.

### 6.4 وضعیت تولید — LookupType 238 (نمونه برای UI تغییر وضعیت)

1789 پایان تولید، 1790 شروع بازرسی نهایی، 1791 پایان بسته بندی، 1792 شروع تولید، 1795/1844 توقف تولید، 1796/1842 توقف بازرسی نهایی، 2209/2210 تست تولید، 2226/2227 توقف تست تولید، 3166/3167 توقف بازدید کارفرما (در Trigger صریح نیستند — v2/v3).

چهار بُعد باید حفظ شوند: `BuyStatusId`, `ProductionStepId`, `ProductionStatusId`, `StatusId` (v2 نتیجه اصلی).

---

## 7. جاب‌ها و WinService

تایمر سرویس: فاصله **1000ms** — `HtsTaskService.cs:76`. پنجره کاری بیشتر تسک‌ها ساعت 6–21.

### 7.1 `PlanningSystemTask` — هر 20 دقیقه (`Minute % 20 == 0 && Second == 30`) از `ModuleTask`

شاهد فراخوان: `HtsTaskService.cs:132,543,607`.

| گام | متد | کار |
|---|---|---|
| 1 | `ProductionOrderItemService.CheckFinancialConfirmsOrders` | اقلام 1903 پس از سابقه 2057 → ارجاع + 2071 |
| 2 | `ProductionOrderService.ApplyUnConfirmedProductionOrders` | سربرگ دارای 2057 بدون 2071 → درج 2071 |
| 3 | `SendHoldedItemsToIndustrialUnitThatWasInProjectManagerCartable` | مرحله 1380 بیش از ۲ روز → 2744 |
| 4 | `AddOrUpdateStandardDeliveryDates` (Task.Run) | تاریخ استاندارد تحویل |

### 7.2 سایر زمان‌بندهای مرتبط همان سرویس

| زمان | متد | کار |
|---|---|---|
| هر 15 دقیقه (`Minute % 15 == 0 && Second == 1`) | `BaseHamkaranTask` → `WriteData.Import_PlnProductionOrder` سپس `Import_PlnProductionOrderItem` | ورود از راهکاران از مسیر C#؛ رویه اقلام ممکن است دوباره اجرا شود (v2) — `HtsTaskService.cs:123,489` |
| همان import | `UpdateProductionOrderItemFirstIndustrialChangeFields` | `:498` |
| 09:01 و 15:01 | `TwoWaysInDayTask` → `SendExpiredOrdersNotification` | مهلت یک‌روزه مهندسی — `:201,927` |
| همان TwoWays | `ProductionOrderItemInquiryService.SendExpiredItemsNotification` | اعلان انقضای LeadTime استعلام 1906/1908 — `:967` |
| 09:45:01 | `SendProductionOrderItemsWithNearDeliveryDateNotifications` | هشدار نزدیک بودن تحویل — `:332` |
| داخل `ModuleTask` | `AddRoutineBomListTask` | BOM روتین خالی برای اقلام 1904/1905 و Status 0/579 — `:570,694` |

`SendExpiredOrdersNotification` داخل خود `PlanningSystemTask` نیست.

### 7.3 SQL Agent راهکاران (enabled=1 در 2026-09-08)

شاهد: `docs/modernization/evidence/database-live-20260908-084254/Q8-01.json`.

| Job | گام | Command | اثر |
|---|---|---|---|
| `HTS_AddProductionOrderJob` | 1 | `EXEC TMS.TotalSystem.dbo.InsertOrUpdateProductionOrders` | ورود/به‌روزرسانی سربرگ |
| همان | 2 | `EXEC TMS.TotalSystem.dbo.InsertOrUpdateProductionOrderItems` | ورود اقلام (رویه اول خودش هم اقلام را صدا می‌زند → تکرار) |
| `HTS_ProductionOrder_InsertCommentAndSendEmailJob` | 1 | INSERT روی `Pln_ProductionOrderComment` برای `IsShouldUpdateHts=1` و نابرابری `LastStatusId`؛ State 2→2084، 3→2057، سایر→2207؛ ایمیل + `Hts_WriteImageToDisk` + `xp_cmdshell del`؛ سپس صفر کردن پرچم با disable/enable Trigger | v3 بخش 1 |

موفقیت اجرایی Job در تاریخچه Agent در v3 بررسی نشده است.

---

## 8. تریگرها و رویه‌ها (فعال/غیرفعال)

وضعیت از خروجی ارسالی 2026-09-07 (`triggers.json`) و ERP 2026-09-07. همه تریگرهای HTS تک‌ردیفی‌اند (v2 بخش 5).

### 8.1 TotalSystem (HTS)

| Trigger | جدول | IsDisabled | اثر |
|---|---|---|---|
| `UpdatePlnProductionOrderItemLastStatus` | `Pln_ProductionOrder_Itm_New_Comment` | **فعال** | کدهای تولید → `IsForProductionMode` و `ProductionStatusId`/`LastComment`؛ LookupType 254 → `BuyStatusId`؛ 2197/2198/2259/2260 → `EngineeringConsideration` |
| `UpdateProductionOrderItemBuyStatus` | `Pln_ProductionOrderItemInquiry` | **فعال** | `BuyStatusId` = آخرین استعلام (MAX Id) |
| `UpdateProductionOrderStatus` | `Pln_ProductionOrderComment` | **فعال** | `LastStatusId/UserId/Comment` سربرگ |
| `UpdateProductionOrderItemVersionInfo` | `Pln_ProductionOrderItem` | **فعال** | بازنویسی `Revision` برای گروه سفارش+کالا |
| `UpdateProductionOrderVersionInfo` | `Pln_ProductionOrder` | **غیرفعال** | نسخه سربرگ را در رفتار فعلی فرض نکنید |

تعریف استخراج‌شده: `docs/modernization/evidence/database-export-2026-09-07/*.sql`.

### 8.2 راهکاران ERPS.USR3

| Trigger | جدول | وضعیت v3 | اثر |
|---|---|---|---|
| `AddProductionOrderToHts_UpdateRevisionAndApplySomeChanges` | `USR3.Sale_ProductionOrder` | فعال | INSERT→Job افزودن؛ UPDATE Revision++؛ `State>1`→پرچم+Job کامنت/ایمیل |
| `UpdateProductionOrderItem` | `USR3.Sale_ProductionOrderItem` | فعال | History Status 0/1/2؛ منبع `RahkaranId` قلم HTS |

### 8.3 رویه‌های HTS مرتبط

| SP / View | نقش |
|---|---|
| `InsertOrUpdateProductionOrders` | تطبیق `Number`؛ درج با 2436؛ UPDATE با اختلاف Version؛ در انتها فراخوان اقلام؛ State=4 → 2207 |
| `InsertOrUpdateProductionOrderItems` | منبع `Sale_ProductionOrderItemHistory`؛ Status 0/1 درج؛ Status 2 حذف منطقی |
| `Vw_Pln_ProductionOrderItem` / `Vw_Pln_ProductionOrderItemBoms` | گزارش/گرید |
| `OrganizationalIndex_Planning` | شاخص (همسایه) |
| `Sup_Compute_OpenOrderRequest` | درخواست باز؛ `ProjectDlId` سربرگ (v2 D19) — خارج از UI این دو صفحه |

فعال/غیرفعال بودن خود SP در evidence به‌صورت `is_disabled` جدا گزارش نشده؛ اشیای استخراج‌شده موجودند.

---

## 9. مدل دسترسی

### 9.1 صفحات `SystemPage`

| Id | Enum | استفاده |
|---|---|---|
| 214 | `Pln_ProductionOrder_Itm_Management` | منو + Load زنده/legacy اقلام |
| 216 | `Pln_ProductionOrder_Itm_Attachment` | پیوست Itm legacy |
| 225 | `Pln_ProductionOrderHdr_Attachment` | پیوست Hdr قدیمی |
| 226 | `Pln_ProductionOrder_Management` | قرارداد / Hdr قدیمی (نه 463) |
| 227 | `ProductionOrder_Itm_Installment` | اقساط legacy |
| 251 | `Pln_ProductionOrder_Itm_RoutineAttachment` | پیوست روتین |
| 362 / 363 / 418 | Delay / Timely / DelayReport | همسایه |
| 463 | `Pln_ProductionOrder` | منوی فروش + Load سربرگ |
| 469 | `Pln_ProductionOrderItemComment` | گزارش کامنت |
| 508 | `Pln_ProductionOrderItemBom` | BOM داخل صفحه + گزارش BOM |
| 546 | `Pln_ProductionOrderItemDeficitReport` | کسری |
| 557 | `Pln_ProductionOrderEquipmentConfirmer` | تأییدکننده تجهیز |

عنوان ردیف `Gnr_Page` زنده در این اجرا خوانده نشد. برچسب UI از `CaptionsLibrary` است.

### 9.2 `PermissionType` روی 214 / 463 / 508

| Id | Enum | صفحه | اثر مشاهده‌شده |
|---|---|---|---|
| 2 | `FullAccess` | 214 گرید legacy | |
| 3 | `Read` | 214 گرید legacy | |
| 7 | `ShowAll` | 214 و 463 گرید | بدون فیلتر کارتابل |
| 8 | `ShowPrice` | 508 BOM | ستون قیمت |
| 10 | `ExportToExcell` | ویو 214 (و اشتباهاً در ویو 463 هم خوانده می‌شود) | |
| 12 | `Accept_Engineering` | در GetGridData legacy کامنت شده | |
| 20 | `ViewAttachment` | 463 دانلود؛ 216 مشاهده | |
| 28 | `Print` | 463 `LoadReport` | |
| 51 / 53 / 54 | `Accept_Financial` / `Accept_Industrial` / `Accept_OfficeManagement` | گرید legacy 214 و Hdr 226 | |
| 55 / 64 / 65 / 66 | قیمت / پیش‌پرداخت / اقساط / پکیج | legacy Itm | |
| 80 | `HasPlanningPermission` | 214 UI CRUD + فیلتر گرید صنایع | |
| 81 | `HasQcApprovePermission` | در enum هست؛ دکمه QC از `QcPermission` 133 است | |
| 82 | `HasEngineeringPermission` | 463 دانلود مهندسی | |
| 119 | `ReadyToSendItemsPermission` | فیلتر آماده ارسال | |
| 121 | `Pln_OtherUnitsPermission` | در enum؛ در این دو ویو دیده نشد | |
| 132 | `ProductionPermission` | تغییر وضعیت تولید | |
| 133 | `QcPermission` | تغییر وضعیت QC | |
| 136 | `SwapPermission` | جابه‌جایی | |
| 137 | `Pln_SupplyCommitteeBoss` | کارتابل 2202/1909 + استعلام | |
| 138 | `Pln_Inquirer` | کارتابل 1906 | |
| 139 | `Pln_MinistryOfIndustryInquirer` | کارتابل 1908 | |
| 187 | `SalesOrProjectPermission` | enum؛ BOM از مقایسه UserId/PersonelId استفاده می‌کند نه این نوع | |
| 193 | `EditEngineeringProject` | 463 فیلتر گرید + `DoEngineeringProjectOperation` | |

`HasAccessWithoutCheckFullAccess` یعنی FullAccess به‌تنهایی آن دکمه را روشن نمی‌کند.

### 9.3 فیلتر گرید زنده اقلام — `GetOriginalGridData:375`

همه شاخه‌های غیر ShowAll/Admin علاوه بر نقش، وجود سابقه سربرگ **2071** را می‌خواهند (`ProductionOrderStatus.InitialProductionOrderProcess`).

| نقش | فیلتر ردیف |
|---|---|
| Admin یا `ShowAll` | همه |
| `HasPlanningPermission` | (2071 و BuyStatus 1904 یا 1905) **یا** ایجاد/ویرایش‌کننده = کاربر جاری |
| رئیس کمیته | 2071 و BuyStatus 2202 یا 1909 و آخرین نسخه و حذف‌نشده |
| استعلام‌گر | 2071 و 1906 |
| وزارت صنایع | 2071 و 1908 |
| سایر | 2071 و ((آخرین نسخه، حذف‌نشده، 1904/1905) یا ایجاد/ویرایش‌کننده یا واحد سازمانی ایجادکننده سربرگ) |

مهندسی مکانیک/مدیر پروژه از این فیلتر SQL به‌تنهایی کارتابل 2195/2258 را نمی‌گیرند مگر `ShowAll` یا ایجادکننده باشند؛ کارتابل آن‌ها عمدتاً از ایمیل `SendEngineeringAndProjectManagerNotification` و دکمه تأیید روی صفحه 463 است.

### 9.4 فیلتر گرید سربرگ — `GetGridData:191`

Admin / `ShowAll` / `EditEngineeringProject` → همه.  
وگرنه: ایجاد/ویرایش‌کننده، کارشناس/مدیر/جانشین فروش، مدیر پروژه (`PersonelId`)، واحد سازمانی ایجادکننده؛ واحدهای 262 و 263 فروش صنعتی یکدیگر را می‌بینند.

### 9.5 گروه‌های کاربری سخت‌کد

| Id | نقش در این صفحات |
|---|---|
| 77 | کارشناسان فروش سفارش ساخت — CRUD سربرگ 463 |
| 596 | کارشناسان فروش اقلام — دکمه تغییر وضعیت 214 |
| 591 | گیرندگان اعلان ایستا (`FillData` Original) |
| 538 | ایمیل تغییر سریال پس از بازرسی محصول |
| 540 | ایمیل شروع تست محصول غیرروتین اولین بار |

---

## 10. اعلان‌ها (از روی همین صفحات / جاب‌ها)

| رویداد | کانال | شاهد |
|---|---|---|
| ارجاع به صنایع / مهندسی / مدیر پروژه | ایمیل | `SendEngineeringAndProjectManagerNotification:155` — گیرنده از EquipmentConfirmer برای DeviceType |
| استعلام | ایمیل پس از `DoInquiryOperation` | `InquiryService.cs:114` |
| تغییر وضعیت تولید | ایمیل async | `CreateAndSendProductionStatusNotification:83` |
| تغییر سریال پس از بازرسی | ایمیل + CC ثابت | `SendChengedSerialNotificationEmail:2302` |
| شروع تست 2209 با مدیر پروژه | ایمیل گروه 540 | `DoChangeProductionStatusOperationOriginal:1038` — شرط `Any(x => x.Id == entity.ProductionOrderItemId)` روی **سربرگ** مشکوک به باگ Id |
| Job مالی State 2/3 | ایمیل راهکاران + پیوست دیسک | Job SQL v3 |
| صدور سند همکاران از UI | PDF گزارش + پیوست‌های سربرگ | `DoUpdateHamkaranOperation:521` |
| انقضای مهندسی / استعلام / نزدیک تحویل | ایمیل WinService | بخش 7 |

---

## 11. شواهد تأییدنشده در این اجرا

| مورد | علت |
|---|---|
| ردیف زنده `Gnr_Page` (PageTitle، System_FK، Report_FK) برای 214/463/508/… | MCP MSSQL در حالت loading/error |
| جدول مجوز صفحه (`Gnr_PagePermission` یا معادل) | همان |
| تعداد ردیف زنده جداول Pln_* | همان |
| فعال بودن عملیاتی Job Agent و موفقیت اجرا | v3 صریحاً تاریخچه Job را نخوانده |
| فعال بودن Windows Service روی سرور تولید | فقط سورس زمان‌بندی |
| عنوان دقیق Lookup در DB امروز نسبت به JSON 2026-09-07 | از evidence استفاده شد |
| اینکه کاربر واقعاً `_ProductionOrderHeader` را از منوی قرارداد باز می‌کند | مسیریابی `planning-contractHeader` به `PlnContractHeader` است نه این ویو |

مقایسه با HavayarApp و ثبت مغایرت در این فایل نیست؛ آن کار مربوط به `02-Havayar-Inventory.md` و `03-Gap-Register.md` است.
