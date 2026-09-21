# سفارش ساخت و اقلام — ثبت مغایرت برای بررسی

> فقط مستندسازی. **هیچ کدی اجرا یا تغییر داده نشده است.**  
> تطبیق سه‌لایه: فرآیند، فیلد/صفحه، جاب/تریگر/دسترسی. تاریخ: **2026-09-15**.  
> قرارداد وضعیت و شدت: `00-Overview.md`. موجودی‌ها را بازنویسی نکنید: `01-HTS-Inventory.md`، `02-Havayar-Inventory.md`.  
> ستون **تصمیم** خالی است تا خودتان پر کنید (مثلاً: اصلاح / حفظ / بعداً / رد ادعا).

ریشه شاهد HTS: `D:\Projects\Hts Project\Hts Project\HtsProject\`.  
ریشه شاهد Havayar: `D:\Projects\Havayar\HavayarApp\`.

Match خالص اینجا نیست. اگر معادل زیر نام دیگر پیدا شد، وضعیت `Partial` است نه `Missing`.

---

## 1. خلاصه شمارش

| حوزه | High | Medium | Low | جمع |
|---|---:|---:|---:|---:|
| MENU | 1 | 0 | 1 | 2 |
| FIELD | 0 | 6 | 3 | 9 |
| PROCESS | 7 | 7 | 0 | 14 |
| PAGE | 2 | 5 | 7 | 14 |
| JOB | 0 | 3 | 1 | 4 |
| TRIGGER | 0 | 0 | 2 | 2 |
| ACCESS | 3 | 5 | 1 | 9 |
| NOTIFY | 1 | 5 | 1 | 7 |
| **جمع** | **14** | **31** | **16** | **61** |

وضعیت ردیف‌ها (بدون Match):

| وضعیت | تعداد |
|---|---:|
| Missing | 24 |
| Partial | 27 |
| IntentionalChange | 7 |
| NewInHavayar | 3 |

---

## 2. فرضیه‌های ردشده (Missing نیستند)

این موارد در پلن به‌عنوان حدس آمده بودند؛ با شاهد **معادل دارند** یا از منوی زنده 214/463 باز نمی‌شوند.

| فرض | نتیجه | شاهد |
|---|---|---|
| جدول فرزند سریال روی صفحه زنده اقلام نیست | روی Original زنده، سریال **روی خود قلم** است؛ Havayar هم `ProductionOrderItem.Serial` دارد. جدول `Pln_ProductionOrder_Itm_Serial` فقط legacy است. | 01 §3.1؛ `ProductionOrderItem.cs:189-192` |
| کسری از 214/463 باز می‌شود | در HTS هم از این دو صفحه باز نمی‌شود (همسایه `546`). | 01 §2.5؛ 02 §6 |
| تأییدکننده تجهیز از 214 باز می‌شود | در HTS هم از 214 باز نمی‌شود (همسایه `557` + JOIN روی `DeviceType`). تب توکار سربرگ Havayar حذف شده — آن بخش `GAP-PAGE-013` است نه «صفحه وجود ندارد». | 01 §2.5؛ `ProductionOrderController.cs:396-397` |
| پیوست روتین صفحه `251` فرزند زنده 214 است | در نوار ابزار Original نیست. | 01 §3.1 نوار ابزار |
| متره قیمت / اقساط صفحه جدا روی 214 زنده | اقساط `227` و متره قیمت legacy هستند. سربرگ زنده `MetreNumber`/`MetreDate` دارد؛ اقساط زنده روی فیلدهای قلم + Action هستند. | 01 §3.3؛ `ProductionOrderItemAction.cs:65-66` |

---

## 3. MENU

| شناسه | وضعیت | شدت | شاهد HTS | شاهد Havayar | اثر کسب‌وکار | پیشنهاد (اجرا نشود) | تصمیم |
|---|---|---|---|---|---|---|---|
| GAP-MENU-001 | Partial | Low | سفارش ساخت در MainSale با چک Planning/`463`: `_MainSaleSystemMenu.cshtml:156-159`. اقلام در برنامه‌ریزی/`214`: `_PlanningSystemMenu.cshtml:135-138`. همان سفارش در منوی برنامه‌ریزی کامنت است: `:124-132`. | هر دو کنترلر `[Route("Panel/[controller]")]` بدون پیشوند `Sale`؛ View و جدول `Sale`: `ProductionOrderController.cs:18-22`، `ProductionOrder.cs:15`. | کاربر HTS سربرگ را از فروش و اقلام را از برنامه‌ریزی می‌بیند. در پنل هر دو زیر یک الگوی URL هستند. گم‌شدن در منو با GAP-MENU-002 ترکیب می‌شود. | در MenuBuilder دو برگ جدا با همان گروه‌بندی HTS بگذارید؛ schema `Sale` را عوض نکنید مگر تصمیم ماژول جدا باشد. |  |
| GAP-MENU-002 | Missing | High | منوی زنده با `HasAccess` صفحه 214/463 رندر می‌شود. | هیچ `Seed_*Menu*` برای این Path در `Data/Scripts` نیست (02 §3). Path مورد انتظار lowercase: `/panel/productionorder/list` و `/panel/productionorderitem/list`. | بدون ردیف MenuBuilder در دیتابیس، صفحه از منوی پنل دیده نمی‌شود حتی اگر RoleAccess اکشن را داشته باشد. | یک seed منو مطابق `havayar-add-new-page.mdc` بنویسید؛ Path را lowercase کنید. |  |

---

## 4. FIELD

| شناسه | وضعیت | شدت | شاهد HTS | شاهد Havayar | اثر کسب‌وکار | پیشنهاد (اجرا نشود) | تصمیم |
|---|---|---|---|---|---|---|---|
| GAP-FIELD-001 | Partial | Medium | هفت کد LookupType 280 از جمله جدا بودن `2084`، `2057`، `2071`، `2436` (01 §6.1؛ Job v3). | `ProductionOrderStateEnum` فقط 1..4؛ کات‌اور `2057` و `2071` هر دو `FinancialApproval`: `ProductionOrderJob.cs:2372-2378`. | گزارش/فیلتر «آغاز فرآیند» از «فقط تأیید مالی» روی `State` ممکن نیست؛ جاب ارجاع به `FinancialApproval` وابسته است. | اگر گزارش 2071 لازم است، یا `State` را گسترش دهید یا از متن/نوع کامنت هدر (`AutoStartProcessComment`) فیلتر کنید. |  |
| GAP-FIELD-002 | Partial | Medium | `CustomerId`, `DlId`, `ProjectDlId`, `DeliveryLocation`, `SaleProjectReportManagementId`: `Pln_ProductionOrder.cs:35-41,64`. | `ProjectDlId` هست؛ `CustomerId`/`DlId`/`DeliveryLocation`/`SaleProjectReportManagementId` روی موجودیت سربرگ نیستند. مشتری احتمالاً از `Contract`. `CustomerIndustry` رشته است: `ProductionOrder.cs:245-248`. | فیلتر/نمایش مشتری و محل تحویل سربرگ بدون پیوستن قرارداد ناقص است. تفصیل غیرپروژه (`DlId`) منتقل نشده. | نگاشت مشتری از قرارداد را در List/گزارش صریح کنید؛ اگر `DlId` هنوز در داده زنده پر است، فیلد یا View کمکی اضافه کنید. |  |
| GAP-FIELD-003 | Missing | Medium | پنج پیوست سربرگ از جمله `AttachmentFile*` مهندسی/متره: `Pln_ProductionOrder.cs:129-135`؛ ریبون 463 (01 §3.2). | چهار `FileEntity`: مدیریت، ودیعه، پیش‌فاکتور، قرارداد. `Edit.cshtml` همان چهار تا. `Update` آن‌ها را کپی می‌کند: `ProductionOrderController.cs:84-88`. | فایل متره/مهندسی سربرگ در پنل جایی برای نگهداری ندارد. | پنجمین پیوست را مطابق الگوی FileEntity اضافه کنید یا تصمیم بگیرید متره فقط فیلد شماره/تاریخ بماند. |  |
| GAP-FIELD-004 | Partial | Low | `SalesBranchId` روی سربرگ و گرید زنده (01 §5.1، §3.2). | `BranchId` روی موجودیت هست: `ProductionOrder.cs:92-96`؛ روی Edit سربرگ `data-bind` ندارد (02 §7.1). | شعبه در فرم ویرایش دیده/ویرایش نمی‌شود؛ sync جاب ممکن است مقدار بگذارد. | فیلد را به Edit اضافه کنید یا در Data Profile سربرگ نشان دهید. |  |
| GAP-FIELD-005 | Partial | Medium | UI و Lookup: `2196`/`2199`/`2200` برق؛ سرویس ادامه خودکار برق کامنت است (v2 §3، `ProductionOrderItemCommentService.cs:388-448`). | `ProductionOrderItemCheckStatusEnum` فقط 2195/2197/2198: `ProductionOrderItemCheckStatusEnum.cs:19-24`. تشخیص رشته با `ElectricalDeviceTypes`: `ProductionOrderItemController.cs:261-282`. تاریخچه `ProductionStatus` برقی پر می‌شود. | کارتابل برق با DeviceType + 2195 کار می‌کند؛ گزارش‌هایی که `BuyStatusId=2196` می‌خواهند تهی می‌مانند. | یا اعضای برقی را به `CheckStatus` برگردانید یا در گزارش‌ها همان قرارداد DeviceType را مستند و اعمال کنید. |  |
| GAP-FIELD-006 | Partial | Medium | فرم Original: DeviceType، مقدار، مرحله، سریال، تاریخ‌های تولید/تست/آماده/تحویل، ملاحظات برنامه‌ریزی (01 §3.1). مشخصات فنی روی قلم هست. | Edit قلم بخش تولید دارد؛ `HasInspection`, `SalesConsideration`, `IsRoutine`, تاریخ تحویل/مدارک/استاندارد روی input نیستند. `Update` بعضی را اگر کلاینت بفرستد می‌نویسد: `ProductionOrderItemController.cs:77-98` در برابر 02 §7.2. | صنایع/برنامه نمی‌توانند روتین، بازرسی و تاریخ تحویل را از همین فرم ببینند/عوض کنند. | فیلدهای عملیاتی را به بخش تولید Edit برگردانید؛ مشخصات فنی را حداقل read-only نشان دهید. |  |
| GAP-FIELD-007 | Partial | Medium | تأخیر HTS موجودیت `DelayResponsibleId` دارد؛ UI صفحه 362 نه 463 (01 §5.3). | `DelayResponsible` روی entity **الزامی** است: `ProductionOrderDelay.cs:29-31`. Partial توکار فیلد ندارد: `_ProductionOrderDelayPartial.cshtml:8-61`. | ذخیره تأخیر از تب سربرگ یا مقدار پیش‌فرض 0 می‌گذارد یا اعتبارسنجی می‌شکند؛ عامل تأخیر غلط می‌شود. | `DelayResponsible` را به partial اضافه کنید (enum موجود است) یا الزام entity را با UI هماهنگ کنید. |  |
| GAP-FIELD-008 | Partial | Low | `TrusteePersonnelId`, `JustificationComment`, `JustificationDate*` روی `Pln_ProductionOrder_Delay` (01 §5.3). | در `ProductionOrderDelay` Havayar نیستند. | توجیه/متولی تأخیر در تب توکار ثبت نمی‌شود (صفحه مستقل Pln ممکن است جدا باشد). | اگر تب توکار جایگزین 362 است، این فیلدها را هم بیاورید؛ وگرنه تأخیر رسمی را همان List تاخیرات نگه دارید. |  |

فیلدهای **NewInHavayar** سربرگ معادل ستون زنده HTS ندارند؛ شکاف منفی نیستند — فقط برای تصمیم همگام‌سازی با راهکاران:

| شناسه | وضعیت | شدت | شاهد HTS | شاهد Havayar | اثر کسب‌وکار | پیشنهاد (اجرا نشود) | تصمیم |
|---|---|---|---|---|---|---|---|
| GAP-FIELD-009 | NewInHavayar | Low | none برای این ستون‌ها روی `Pln_ProductionOrder` | `ProductionOrder.cs:261-315` | در برش HTS نیستند؛ برش راهکاران/پنل ممکن است به آن‌ها وابسته شود. | در قرارداد sync مشخص کنید از ERP می‌آیند یا فقط پنل. |  |

---

## 5. PROCESS

| شناسه | وضعیت | شدت | شاهد HTS | شاهد Havayar | اثر کسب‌وکار | پیشنهاد (اجرا نشود) | تصمیم |
|---|---|---|---|---|---|---|---|
| GAP-PROCESS-001 | Missing | High | پس از تأیید PM: کامنت `2259` سپس خودکار `2195`: `ProductionOrderItemCommentService.cs:313-324`؛ v2 جدول §3. | `AcceptProjectManager` Approve → `2259` + `ProductionStep = AwaitingProduction`: `ProductionOrderItemController.cs:492-495`. زنجیره `2195` ساخته نمی‌شود. | قلم پس از PM مستقیم «منتظر تولید» می‌شود و کارتابل مهندسی را رد می‌کند. | پس از Approve، همان زنجیره HTS (کامنت 2195 + مرحله انتظار مهندسی) را اضافه کنید مگر محصول عمداً PM را جایگزین مهندسی کرده باشد. |  |
| GAP-PROCESS-002 | Missing | High | پس از `2197`: اگر غیرروتین+ساخت داخل یا پیشوند کالا → `1904` وگرنه → `2202`: `ProductionOrderItemCommentService.cs:325-357`. پیشوندها: `1807101`, `180229`, `180211`, `1808101`, `1809101`, `1810101`, `1811101`. | `AcceptEngineering` Approve → `2197` + `AwaitingProduction`: `:324-330`. به کمیته/صنایع نمی‌رود. | همه تأییدهای مهندسی به تولید می‌روند؛ کمیته تأمین برای کالای غیرساخت‌داخل تشکیل نمی‌شود. | منطق `IsBuildInside` / `IsRoutine` / پیشوند کالا را بعد از 2197 پیاده کنید (مقصد 1904 یا 2202). |  |
| GAP-PROCESS-003 | Partial | High | کارتابل صنایع `1904`/`1905` است؛ خروج با استعلام/کمیته یا رخداد تولید، نه با «تأیید صنایع» جدا که BuyStatus را نگه دارد (01 §6.2، v2). | `AcceptIndustrial` صریح CheckStatus را عوض نمی‌کند؛ فقط `ProductionStep = AwaitingProduction`: `:433-435`. `POI_MyCartable` با `ProductionStep∈{0,2744}` دور می‌زند: `Seed_ProductionOrderItem_MyCartable_DataProfile.sql:122-133`. نمایه `POI_Cartable_Industrial` فقط 1904/1905 است (02 §11.2). | دکمه تأیید صنایع قلم را از کارتابل «همه صنایع» بیرون نمی‌آورد؛ هزاران قلم قدیمی 1904 در نمایه می‌مانند مگر MyCartable. | یا عضو CheckStatus «پس از صنایع» تعریف کنید یا فیلتر همه نمایه‌های صنایع را مثل MyCartable به مرحله محدود کنید. |  |
| GAP-PROCESS-004 | Partial | Medium | Trigger استعلام `BuyStatusId=1909` می‌نویسد نه 2202 (v2 U06). دکمه کمیته UI فقط `BuyStatusId === 2202`: `_ProductionOrderItemOriginal.cshtml:427,524`. | `SendToSupplyCommittee` → `1909`: `:606-610`. `CommitteeAccept` هر دو `1909` و `2202` را می‌پذیرد: `:625-632`. هیچ اکشنی به 2202 نمی‌برد. | در Havayar مسیر کمیته با 1909 کار می‌کند (برخلاف بن‌بست UI قدیم). داده کات‌اور 2202 هنوز قابل تأیید است. | همین پذیرش دوتایی را نگه دارید؛ در UI برچسب 1909/2202 را یکی کنید تا کاربر گیج نشود. |  |
| GAP-PROCESS-005 | Partial | High | رئیس کمیته روی 2202 مقصد `1904`/`1905`/`1906`/`1908` را انتخاب می‌کند (v2 §3 کمیته؛ `DoInquiryOperation` با Status مقصد). | `CommitteeAccept` همیشه → `1904` + `AwaitingProduction`: `:634-640`. ارسال استعلام/وزارت دکمه‌های جدا از کارتابل صنایع‌اند نه از تصمیم کمیته. | کمیته نمی‌تواند خارجی/استعلام/وزارت را به‌عنوان خروجی بررسی انتخاب کند؛ همه «تأیید کمیته» داخلی+تولید می‌شوند. | در تأیید کمیته مقصد را مثل HTS از کاربر بگیرید یا ویزارد چنددکمه‌ای معادل بگذارید. |  |
| GAP-PROCESS-006 | IntentionalChange | Medium | کمیته در مسیر مستندشده «رد نهایی منسوخ» ندارد؛ انتخاب مقصد کارتابل است (v2). | `CommitteeReject` → `Deprecated` + `Canceled` با کامنت تصمیم طراحی: `:659-667`. | رد کمیته قلم را می‌بندد نه به ثبت اولیه برمی‌گرداند. | اگر کسب‌وکار رد=بازگشت به صنایع/ثبت است، مقصد را عوض کنید؛ اگر بستن نهایی است، همین را تأیید کنید. |  |
| GAP-PROCESS-007 | Missing | High | فرم استعلام: `ResponsibleId`, `LeadTimeInText`, پیوست؛ پست `DoInquiryOperation`: `_ProductionOrderItemOriginal.cshtml:1263-1276`؛ سرویس `ProductionOrderItemInquiryService.cs:66-114`. | ردیف Inquiry فقط در `TransitionCheckStatusAsync` بدون مسئول/LeadTime/فایل: `ProductionOrderItemController.cs:743-754`. فرم ورود در کنترلر نیست (02 §5.4). | LeadTime خالی → جاب انقضای استعلام HTS معادلی برای کار ندارد (GAP-JOB-001). مسئول و پیوست استعلام ثبت نمی‌شود. | صفحه/مودال استعلام با همان سه فیلد؛ سپس Transition فقط وضعیت را عوض کند. |  |
| GAP-PROCESS-008 | Partial | High | مهلت یک‌روزه مهندسی: مقصد `1904` یا `2202` با همان قاعده ساخت داخل/پیشوند: `ProductionOrderItemService.cs:639-656`. | `SendExpiredEngineeringItemsToIndustrial` همیشه `1904` + `AwaitingProduction`: `ProductionOrderJob.cs:1698-1701`. | کالای کمیته‌ای پس از SLA به صنایع می‌رود نه کمیته. | همان شاخه 1904/2202 HTS را در جاب مهندسی کپی کنید. |  |
| GAP-PROCESS-009 | Partial | Medium | مهلت PM: فقط `ProductionStepId` 1380 → 2744؛ BuyStatus را عوض نمی‌کند: `ProductionOrderItemService.cs:694-724`. | همان مهلت ۲ روز؛ علاوه بر مرحله، `CheckStatus = 1904`: `ProductionOrderJob.cs:1620-1623`. | در Havayar کارتابل صنایع پر می‌شود؛ در HTS ممکن است هنوز 2258 بماند و فقط مرحله 2744 شود. MyCartable صنایع به 2744 حساس است. | تصمیم بگیرید هماهنگی CheckStatus (Havayar) درست است یا فقط مرحله (HTS). |  |
| GAP-PROCESS-010 | IntentionalChange | Medium | تأیید مالی در راهکاران؛ Job SQL سابقه 2057 (v3 §1). دکمه UI صدور سند: `DoUpdateHamkaranOperation`. | `DoFinancialConfirm` روی Edit سربرگ: `ProductionOrderController.cs:211-257`. جاب ورود `State` را از Rahkaran کپی می‌کند: `ProductionOrderJob.cs:131,231-233`. | دو مسیر تأیید (ERP و پنل) می‌توانند از هم واگرا شوند. | مالک مشخص کند منبع حقیقت فقط راهکاران است یا پنل هم مجاز است؛ مسیر دوم را قفل یا همگام کنید. |  |
| GAP-PROCESS-011 | Partial | High | `DoChangeProductionStatusOperationOriginal` برای 1789/1843/1791 تاریخ پایان تولید/تست/آماده‌سازی قلم را می‌نویسد: `ProductionOrderItemController.cs:1049-1082`. ایمیل 2209 همان اکشن (`:1038-1046`). | `SaveComment` فقط کامنت می‌سازد؛ Action وضعیت را همگام می‌کند؛ تاریخ‌های قلم از رخداد پر نمی‌شوند: `WebApp/Controllers/Dynamic/Sale/ProductionOrderItemController.cs:1591-1610`. | «پایان تولید» در سابقه هست ولی ستون تاریخ قلم خالی می‌ماند؛ گزارش تحویل/اقساط وابسته به `PreparationDate` غلط می‌شود. | هنگام SaveComment همان نگاشت تاریخ HTS را روی قلم اعمال کنید. |  |
| GAP-PROCESS-012 | Missing | Medium | `CheckSerialIsExist`: `ProductionOrderItemService.cs:738-741`. | در WebApp/Job جستجوی معادل پیدا نشد. | سریال تکراری بین اقلام ممکن است. | یکتایی سریال فعالِ آخرین نسخه را در Save/Update قلم چک کنید. |  |
| GAP-PROCESS-013 | Missing | Medium | `DoUpdateHamkaranOperation`: صدور سند + PDF + ایمیل + سابقه 2084؛ اگر 2057 باشد خطا (01 §4.2). | اکشن معادل روی `ProductionOrderController` نیست. ورود از راهکاران جایگزین «هل دادن به همکاران» است. | کاربر فروش نمی‌تواند از پنل سند همکاران را صادر کند. | اگر هنوز از HTS این دکمه زده می‌شود، معادل پنل یا لینک ERP لازم است؛ اگر ERP تنها منبع است، IntentionalChange کنید. |  |
| GAP-PROCESS-014 | Partial | Medium | نظر واحد فروش BOM: `DoBomSalesUnitOperation`؛ اگر مرحله 1380 باشد → 2744 (01 §4.1). | ذخیره/ورود BOM در صورت `AwaitingEngineering` و وجود PM به کارتابل PM (`1380`/`2258`) می‌برد: `ProductionOrderItemController.cs:1068-1112`. اکشن جدا برای نظر فروش→صنایع نیست. | مسیر «فروش روی BOM نظر داد، از PM به صنایع برو» در پنل نیست؛ مسیر «BOM خورد، برو PM» هست. | دکمه/وضعیت نظر واحد فروش را با اثر 1380→2744 مشخص کنید. |  |

---

## 6. PAGE

| شناسه | وضعیت | شدت | شاهد HTS | شاهد Havayar | اثر کسب‌وکار | پیشنهاد (اجرا نشود) | تصمیم |
|---|---|---|---|---|---|---|---|
| GAP-PAGE-001 | Missing | Medium | دکمه قطعه یدکی پس از انتخاب ردیف؛ `LoadSparePartPage` / `GetSparePartGridData`: `_ProductionOrderItemOriginal.cshtml:1723-1725`؛ کنترلر `:196,1801`. | در View/کنترلر قلم صفحه یدکی نیست (02 §14). | از کارتابل اقلام نمی‌توان یدکی کالای مرتبط را دید. | ListByParent یا مودال فقط‌خواندنی روی `Part` مرتبط. |  |
| GAP-PAGE-002 | Missing | High | پنجره تغییر وضعیت + کمبو مسئول استعلام + LeadTime + پیوست (01 §3.1 تب استعلام؛ JS `:1263`). | تب سابقه استعلام نمایش دارد: `_ProductionOrderItemCommentListPartial.cshtml:98-135`. فرم ورود نیست. | همان GAP-PROCESS-007 از دید UI. | مودال ثبت استعلام جدا از «تغییر وضعیت تولید». |  |
| GAP-PAGE-003 | Missing | Medium | چاپ DevExpress `LoadReport` + `PermissionType.Print` روی 463 (01 §4.2). | چاپ Stimulsoft/`ViewReportByName` برای این دو صفحه در Viewها نیست. | فرم رسمی سفارش از پنل چاپ نمی‌شود. | قالب ReportBuilder جدا از گرید؛ ورود با `appController.addPage`. |  |
| GAP-PAGE-004 | Missing | Medium | «آماده ارسال»: `ReadyToSendItemsPermission` (119)؛ فیلتر `StatusId=579` و `ProductionStepId=221` (01 §3.1). | دکمه/نمایه معادل در seed POI_* نیست. | فیلتر روزانه آماده ارسال باید دستی در Data Profile ساخته شود. | نمایه یا دکمه سفارشی با همان دو شرط. |  |
| GAP-PAGE-005 | Partial | Medium | جابه‌جایی با `SwapPermission` از ریبون Original. | `POST Swap` هست: `ProductionOrderItemController.cs:1745-1841`. `List.cshtml` فقط HTML مودال است؛ اسکریپت POST ندارد (02 §14). احتمالاً `EventScriptsJson` دیتابیس Id=104. | اگر اسکریپت پروفایل در DB نباشد، دکمه جابه‌جایی از لیست کار نمی‌کند. | اسکریپت `data-action` را در List یا CustomActionButtonsJson ریپو کنید. |  |
| GAP-PAGE-006 | Missing | High | گرید سربرگ `GetGridData` صفحه 463. | `List.cshtml` سربرگ فقط `datatableprofile` است؛ seed SavedQuery برای `ProductionOrder` در اسکریپت‌های POI نیست (02 §11.1). | لیست سربرگ بدون پروفایل ذخیره‌شده ستون/فیلتر عملیاتی ندارد. | SavedQuery + RoleAccess برای `typeof(ProductionOrder)` مثل اقلام. |  |
| GAP-PAGE-007 | Missing | Medium | اگر BOM روتین خالی باشد `AddRoutineBomList` هنگام `GetBomGridData`: کنترلر HTS `:1609-1616`. | `AddRoutineBomList` در HavayarApp پیدا نشد. ورود دستی/اکسل هست. | قلم روتین بدون BOM می‌ماند تا کسی دستی وارد کند. | هنگام باز شدن BomListBy برای `IsRoutine`، الگوی روتین را درج کنید (جاب HTS هم جداست: GAP-JOB-002). |  |
| GAP-PAGE-008 | Partial | Low | تأیید/رد مهندسی از گرید اقلام داخل سربرگ 463: `DoItemCommentOperation`؛ دکمه `EngineeringFeedback` در `_ProductionOrder.cshtml:1808-1810`. | مهندسی روی Edit قلم است (`AcceptEngineering`) نه روی Edit سربرگ. | مسیر کاربر عوض شده؛ قابلیت هست. | لینک «اقلام این سفارش» / دکمه کارتابل از سربرگ کافی است اگر آموزش داده شود. |  |
| GAP-PAGE-009 | Missing | Low | دانلود پیوست سفارش از ریبون اقلام: `ViewProductionOrderAttachedFile` (01 §3.1). | از Edit قلم لینک دانلود پیوست‌های سربرگ دیده نشد. | کاربر اقلام باید سربرگ را جدا باز کند. | دکمه «پیوست سفارش» به Edit سربرگ یا download فایل. |  |
| GAP-PAGE-010 | IntentionalChange | Low | سریال فرزند فقط legacy Itm (01 §5.3). | سریال روی قلم + اقساط محاسبه‌ای: `ProductionOrderItemAction.cs:10-12,65-66`. | رفتار زنده حفظ شده؛ صفحه سریال قدیم نیامده. | نیازی به بازسازی جدول Serial برای Original نیست مگر داده legacy لازم باشد. |  |
| GAP-PAGE-011 | IntentionalChange | Low | اقساط صفحه `227` روی Itm قدیمی. | تاریخ اقساط ۲–۵ روی قلم. | مدیریت اقساط به‌صورت صفحه CRUD نیست. | اگر مالی هنوز صفحه 227 را دارد، Data Profile روی همان چهار فیلد. |  |
| GAP-PAGE-012 | IntentionalChange | Low | متره قیمت `DoChangeBuyPriceOperation` legacy. | `MetreNumber`/`MetreDate` روی سربرگ. | تاریخچه قیمت متره قلم زنده نیست. | فقط اگر گزارش قیمت متره زنده است، موجودیت تاریخچه بسازید. |  |
| GAP-PAGE-013 | IntentionalChange | Low | تنظیم تأییدکننده صفحه 557 همسایه؛ از 214 باز نمی‌شود. | تب سربرگ حذف شده؛ CRUD سراسری `ProductionOrderEquipmentConfirmerController` (02 §4.1، §6). Job اعلان از همان جدول می‌خواند: `ProductionOrderJob.cs:3053-3058`. | تنظیم از صفحه سفارش نمی‌شود؛ از صفحه جدا می‌شود. | منوی همسایه را به همان کنترلر وصل کنید (خارج از این دو صفحه طبق پلن). |  |
| GAP-PAGE-014 | NewInHavayar | Low | تأخیر داخل 463 نیست (01 §3.2). | تب تأخیرات روی `ProductionOrder/Edit.cshtml`. | ثبت تأخیر از سربرگ ممکن است؛ ناقصی فیلد: GAP-FIELD-007. | یا تب را کامل کنید یا به List تاخیرات Pln هدایت کنید. |  |

---

## 7. JOB

| شناسه | وضعیت | شدت | شاهد HTS | شاهد Havayar | اثر کسب‌وکار | پیشنهاد (اجرا نشود) | تصمیم |
|---|---|---|---|---|---|---|---|
| GAP-JOB-001 | Missing | Medium | `SendExpiredItemsNotification` روی LeadTime استعلام 1906/1908: `ProductionOrderItemInquiryService.cs:126-144`؛ زمان 09:01/15:01 در `HtsTaskService.cs:967`. | هندلر معادل در `ProductionOrderJob` نیست. Inquiry بدون LeadTime هم ساخته می‌شود (PROCESS-007). | انقضای استعلام ایمیل نمی‌شود. | جاب + فرم LeadTime با هم. |  |
| GAP-JOB-002 | Missing | Medium | `AddRoutineBomListTask` داخل `ModuleTask`: `HtsTaskService.cs:570,694`. | معادل در `ProductionOrderJob` نیست. | BOM روتین خالی برای 1904/1905 پر نمی‌شود. | هندلر جاب با همان فیلتر وضعیت/Status 0. |  |
| GAP-JOB-003 | NewInHavayar | Low | none (کات‌اور یک‌باره عملیاتی HTS نیست). | `SyncProductionOrderOperationalFieldsFromHts`: `ProductionOrderJob.cs:1801-1808`. | ابزار مهاجرت است نه فرآیند روزانه. | بعد از برش نهایی disable/حذف از زمان‌بندی پنل. |  |
| GAP-JOB-004 | Partial | Medium | فاصله و ساعت در `HtsTaskService` سخت‌کد است (01 §7). | `[JobHandler]` بدون cron؛ TODO زمان‌بندی پنل: `ProductionOrderJob.cs:1668,1748`. | اگر ادمین جاب را فعال نکند، ارجاع 1903 و مهلت‌ها نمی‌دوند. | چک‌لیست cutover: فعال‌سازی همان هشت هندلر در پنل Job. |  |

ورود راهکاران، `CheckFinancialConfirmsOrders`, `ApplyUnConfirmedProductionOrders`, `AddOrUpdateStandardDeliveryDates` معادل دارند (Match — اینجا نیستند). انحراف مقصد SLA مهندسی در GAP-PROCESS-008 است.

---

## 8. TRIGGER

| شناسه | وضعیت | شدت | شاهد HTS | شاهد Havayar | اثر کسب‌وکار | پیشنهاد (اجرا نشود) | تصمیم |
|---|---|---|---|---|---|---|---|
| GAP-TRIGGER-001 | IntentionalChange | Low | چهار تریگر فعال تک‌ردیفی (v2 §5؛ 01 §8.1). | `EntityAction` روی مجموعه خواهر و برادر حلقه می‌زند، مثلاً `RenumberRevisionsAsync`: `ProductionOrderItemAction.cs:45-62`. Inquiry/کامنت آخرین رکورد فعال را می‌گیرند. | واردسازی دسته‌ای در Havayar باید کامل‌تر از Trigger قدیمی باشد. | در آزمون کات‌اور چندردیفی را یک‌بار با HTS مقایسه کنید؛ رفتار جدید را به‌عنوان اصلاح نگه دارید مگر مغایرت داده ببینید. |  |
| GAP-TRIGGER-002 | Partial | Low | حذف سابقه تأمین ممکن است BuyStatus را روی رکورد حذف‌شده بگذارد (v2 §5). | `SyncAfterDelete` والد را از کامنت‌های باقی‌مانده بازسازی می‌کند: `ProductionOrderItemCommentAction.cs:59-62` (رفتار جزئی در 02 §9). | حذف تاریخچه اثر متفاوتی نسبت به HTS دارد. | سیاست «حذف سابقه مجاز نیست» را حفظ کنید؛ بازسازی را عمدی بدانید. |  |

---

## 9. ACCESS

| شناسه | وضعیت | شدت | شاهد HTS | شاهد Havayar | اثر کسب‌وکار | پیشنهاد (اجرا نشود) | تصمیم |
|---|---|---|---|---|---|---|---|
| GAP-ACCESS-001 | Partial | Medium | گرید Kendo از `GetOriginalGridData` / `GetGridData`. | `FetchData`/`ExportToExcel` کنترلر → `NotImplementedException`: `Repository.cs:746-750,781-783`؛ کنترلر سربرگ `:194-207`. لیست واقعی Data Profile/SavedQuery است. | Excel دکمه کنترلر می‌شکند. خروجی پروفایل اگر از موتور SavedQuery برود ممکن است کار کند. | Excel را از toolbar پروفایل بسنجید؛ FetchData کنترلر را با پروفایل یکی ندانید. |  |
| GAP-ACCESS-002 | Partial | High | گرید زنده غیر ShowAll به نقش+سابقه 2071 محدود است: `GetOriginalGridData` (01 §9.3). | `POI_Sales_Related_Active`: فقط حذف‌نشده + آخرین نسخه؛ **فیلتر شخص جاری در JSON نیست** (02 §11.2). `GetRowSecurityPredicate` صدا می‌شود و AND نمی‌شود: `ProductionOrderController.cs:205-206`. | نقش «مشاهده مرتبط» عملاً همه اقلام فعال را می‌بیند. | WHERE سازنده/کارشناس/مدیر/PM مثل predicate آماده‌شده. |  |
| GAP-ACCESS-003 | Partial | Medium | فیلتر سربرگ: ایجادکننده، فروش، PM، واحد سازمانی؛ واحد 262 و 263 یکدیگر را می‌بینند (01 §9.4). | Predicate آماده بدون واحد سازمانی (فیلد نیست): `:407-427`. روی FetchData اثر ندارد. Data Profile سربرگ هم seed نشده (PAGE-006). | امنیت ردیف سربرگ در لیست اعمال نشده. | پس از پروفایل سربرگ، همان شرط‌ها را در SavedQuery بگذارید. |  |
| GAP-ACCESS-004 | Partial | High | همه شاخه‌های غیر Admin/ShowAll وجود 2071 روی سربرگ را می‌خواهند (01 §9.3). | `POI_All_Active` / `POI_ViewActive` فقط `IsDeleted`/`IsLatestVersion`. کارتابل‌ها روی CheckStatus قلم‌اند نه State هدر. | اقلام 1903 پیش از آغاز فرآیند در «همه فعال» دیده می‌شوند (در HTS برای غیر ShowAll نه). | فیلتر `ProductionOrder.State = FinancialApproval` یا وجود کامنت آغاز فرآیند برای نمایه‌های غیر ShowAll. |  |
| GAP-ACCESS-005 | Partial | High | — | `POI_Cartable_Industrial` = CheckStatus 1904/1905 بدون محدودیت مرحله (02 §11.2). `AcceptIndustrial` مرحله را 214 می‌کند و CheckStatus را نمی‌گذارد (PROCESS-003). | کارتابل صنایع نمایه ثابت پس از «تأیید صنایع» خالی نمی‌شود. | فیلتر مرحله را با MyCartable یکی کنید. |  |
| GAP-ACCESS-006 | Missing | Low | واحدهای 262 و 263 فروش صنعتی یکدیگر را در گرید سربرگ می‌بینند (01 §9.4). | در predicate نیست. | دو واحد صنعتی همدیگر را در پنل نمی‌بینند مگر ShowAll. | اگر هنوز در سازمان معتبر است، OR روی OrgUnit کاربران. |  |
| GAP-ACCESS-007 | Partial | Medium | گروه کاربری 77 CRUD سربرگ؛ 596 دکمه تغییر وضعیت اقلام (01 §9.5). مجوزهای `PermissionType` 80/132/133/136/137/138/139. | Roleهای 200020–200032 در SQL seed (02 §11.1). کامنت `TODO(Role)` در کنترلر/ویو با وجود seed ناهماهنگ است. | اگر فقط seed Role ساخته شده و RoleAccess اکشن/کاربر ناقص باشد، دکمه‌ها 401 می‌دهند. | انتساب کاربر و تیک اکشن‌های Custom را در Role UI با seed چک کنید؛ TODOها را با واقعیت seed عوض کنید. |  |
| GAP-ACCESS-008 | Missing | Medium | `ShowPrice` (8) روی صفحه BOM 508 ستون قیمت را نشان می‌دهد (01 §9.2). | مجوز جدا برای قیمت BOM در کنترلر قلم ثبت نشده (02 §5.3). | قیمت داخلی/خارجی BOM ممکن است برای همه دیده شود یا اصلاً نباشد. | `ActionDisplayName`/`ShowPrice` روی BomListBy یا ستون پروفایل. |  |
| GAP-ACCESS-009 | Partial | Medium | تأیید مالی در ERP نه در گرید اقلام. | `POI_MyCartable` شاخه `State=2` برای نقش FinancialConfirm: `Seed_ProductionOrderItem_MyCartable_DataProfile.sql:177-181`. دکمه تأیید روی Edit سربرگ است: `ProductionOrderController.cs:211-213`. | مالی اقلام State=2 را در کارتابل قلم می‌بیند ولی تأیید را باید روی سربرگ بزند. | یا لینک Edit سربرگ از آن نمایه، یا این شاخه را از MyCartable بردارید. |  |

---

## 10. NOTIFY

| شناسه | وضعیت | شدت | شاهد HTS | شاهد Havayar | اثر کسب‌وکار | پیشنهاد (اجرا نشود) | تصمیم |
|---|---|---|---|---|---|---|---|
| GAP-NOTIFY-001 | Missing | High | `SendProductionOrderItemsWithNearDeliveryDateNotifications` ساعت 09:45 ایمیل (01 §7.2، §10). | هندلر قلم‌ها را پیدا می‌کند؛ `SendNearDeliveryDateNotification` فقط `LogInfo` است با TODO سرویس اعلان: `ProductionOrderJob.cs:1749-1793,3198-3205`. | هشدار نزدیک‌تحویل به هیچ Inbox/ایمیلی نمی‌رسد. | صف `Notification` مثل اعلان مهندسی (`:3093-3105`). |  |
| GAP-NOTIFY-002 | Missing | Medium | ایمیل تغییر سریال پس از بازرسی؛ گروه 538: `SendChengedSerialNotificationEmail` (01 §10). | در کنترلر `Update` قلم اعلان سریال نیست. | QC/بازرسی از تغییر سریال مطلع نمی‌شود. | بعد از تغییر Serial اگر `HasInspection`، همان گروه/نقش را ایمیل کنید. |  |
| GAP-NOTIFY-003 | Missing | Medium | شروع تست `2209` + گروه 540؛ شرط Id مشکوک در HTS ثبت شده (01 §10). | `SaveComment` ایمیل نمی‌فرستد. | اعلان شروع تست محصول غیرروتین قطع است. | روی ProductionStatus=2209 همان گروه را صف کنید. |  |
| GAP-NOTIFY-004 | Missing | Medium | تغییر وضعیت تولید ایمیل async: `CreateAndSendProductionStatusNotification` (01 §10). | `SaveComment` بدون اعلان. | تولید/فروش از رخداد خط مطلع نمی‌شوند مگر کارتابل را باز کنند. | اعلان به CC کامنت (`CcReciversIds`) + گروه تولید. |  |
| GAP-NOTIFY-005 | Missing | Medium | پس از `DoInquiryOperation` ایمیل: `ProductionOrderItemInquiryService.cs:114`. | Transition استعلام `Notification` نمی‌سازد. | استعلام‌گر فقط اگر کارتابل 1906 را باز کند می‌بیند. | بعد از درج Inquiry، ایمیل به `ResponsibleId` (پس از وجود فرم). |  |
| GAP-NOTIFY-006 | Partial | Low | — | گروه‌های BOM `Sale.ProductionOrderItemBom.Industrial/Engineering` در اولین SaveBom ساخته می‌شوند؛ seed SQL ندارند (02 §12). | تا اولین ذخیره BOM، گروه خالی است. | seed گروه در اسکریپت Data Profile. |  |
| GAP-NOTIFY-007 | Missing | Medium | Job راهکاران State 2/3: ایمیل + نوشتن تصویر دیسک + حذف موقت (v3 §1). | جاب C# ورود State را همگام می‌کند؛ معادل ارسال ایمیل پیوست‌دار آن Job SQL دیده نشد. | اعلان مالی ERP در پنل تکرار نمی‌شود (ممکن است هنوز از Agent SQL برود). | اگر Agent SQL خاموش شود، اعلان مالی را در `AddProductionOrderFromRahkaran` صف کنید. |  |

اعلان مهندسی/PM پس از ارجاع جاب در Havayar صف می‌شود (`SendEngineeringNotificationAsync`) — Match نیست در این جدول.

---

## 11. ترتیب پیشنهادی بررسی (اجرا نیست)

1. Highهای مسدودکننده ورود: `GAP-MENU-002`, `GAP-PAGE-006`.  
2. Highهای وضعیت غلط: `GAP-PROCESS-001`, `GAP-PROCESS-002`, `GAP-PROCESS-003`, `GAP-PROCESS-005`, `GAP-PROCESS-007`, `GAP-PROCESS-008`, `GAP-PROCESS-011`.  
3. High فرم استعلام و اعلان تحویل: `GAP-PAGE-002`, `GAP-NOTIFY-001`.  
4. High دسترسی داده: `GAP-ACCESS-002`, `GAP-ACCESS-004`, `GAP-ACCESS-005`.  
5. بقیه Medium/Low پس از تصمیم محصول.

---

## 12. وضعیت اجرا — 2026-09-17

بازبینی دوم روی سورس فعلی HTS و HavayarApp (+ دیتابیس dev `HavayarApp`) انجام و مغایرت‌های قطعی اصلاح شد. جزئیات فنی هر اصلاح، ترتیب اجرای اسکریپت‌ها و سوال‌های باز در `04-Fix-Log.md`.

| وضعیت | معنی |
|---|---|
| ✅ اصلاح شد | در کد/اسکریپت ریپو اعمال شد (بیلد موفق) |
| ❎ رد ادعا | با شاهد جدید، مغایرت وجود ندارد یا در DB زنده از قبل حل شده |
| ⏸ حفظ | اختلاف آگاهانه؛ تغییری لازم نیست |
| ❓ سوال | تصمیم کسب‌وکار لازم است — در `04-Fix-Log.md` §سوال‌ها |

| شناسه | وضعیت | خلاصه |
|---|---|---|
| GAP-MENU-001 | ✅ اصلاح شد | `Seed_ProductionOrder_Menu.sql`: برگ «سفارش ساخت» و «اقلام سفارش ساخت» در منوی **فروش** (Sell) و گروه **برنامه‌ریزی** منوی صنایع (Industries). منوی «سیستم برنامه‌ریزی» جدا در پنل وجود ندارد؛ معادلش گروه برنامه‌ریزی زیر صنایع است. |
| GAP-MENU-002 | ✅ اصلاح شد | همان seed (idempotent روی Path). در DB dev، صنایع/برنامه‌ریزی هر دو برگ را داشت؛ فروش فقط اقلام را داشت. |
| GAP-FIELD-001 | ⏸ حفظ | `State` چهارعضوی می‌ماند؛ «آغاز فرآیند» با کامنت خودکار هدر (`AutoStartProcessComment`) قابل تشخیص است. |
| GAP-FIELD-002 | ✅ اصلاح شد | `DeliveryLocation` اضافه شد (entity + Edit + پروفایل). تصمیم 2026-09-17: مشتری/تفصیل مستقل اضافه نمی‌شود (از راهکاران/قرارداد می‌آید). |
| GAP-FIELD-003 | ✅ اصلاح شد | پیوست پنجم `EngineeringAttachment` (مهندسی/متره) روی entity، Edit سربرگ و دانلود از Edit قلم. اسکریپت `Add_ProductionOrder_DeliveryLocation_EngineeringAttachment.sql`. |
| GAP-FIELD-004 | ✅ اصلاح شد | `BranchId` (شعبه فروش) روی Edit سربرگ + کپی در `Update`. |
| GAP-FIELD-005 | ⏸ حفظ | برق/مکانیک با `2195` + `DeviceType`؛ HTS هم زنجیره برق را کامنت کرده بود. |
| GAP-FIELD-006 | ✅ اصلاح شد | ورودی «تاریخ تحویل» + کارت فقط‌خواندنی (ارسال به صنایع، تحویل استاندارد، مدارک، اولین تغییر صنایع، روتین، ملاحظات فروش/مهندسی، آخرین کامنت). تصمیم 2026-09-17: «مقدار» فقط‌خواندنی می‌ماند (از راهکاران). |
| GAP-FIELD-007 | ✅ اصلاح شد | `DelayResponsible` به partial توکار اضافه شد + رفع باگ `data-bind` تاریخ‌ها (Miladi→Shamsi). |
| GAP-FIELD-008 | ⏸ حفظ | ماژول تأخیر مستقل (Pln) هم‌زمان در حال تغییر است؛ خارج از این اصلاح. |
| GAP-FIELD-009 | ⏸ حفظ | — |
| GAP-PROCESS-001 | ✅ اصلاح شد | `AcceptProjectManager`: تأیید ۲۲۵۹ → زنجیره خودکار ۲۱۹۵ + مرحله انتظار مهندسی؛ رد ۲۲۶۰ → ۲۲۶۱. کامنت اختیاری اضافه شد. |
| GAP-PROCESS-002 | ✅ اصلاح شد | `AcceptEngineering`: تأیید ۲۱۹۷ → (غیرروتین و ساخت داخل) یا پیشوند کالا → ۱۹۰۴ وگرنه ۲۲۰۲؛ رد ۲۱۹۸ → ۲۲۰۱. قاعده مشترک در `ProductionOrderItemWorkflowRules`. |
| GAP-PROCESS-003 | ✅ اصلاح شد | تصمیم 2026-09-17: `AcceptIndustrial` حذف شد (HTS «تأیید صنایع» ندارد؛ صنایع فقط قلم را ویرایش می‌کند). کارتابل صنایع = گرید ۱۹۰۴/۱۹۰۵ مثل HTS. |
| GAP-PROCESS-004 | ✅ اصلاح شد | پذیرش دوگانه ۱۹۰۹/۲۲۰۲ حفظ؛ فرم واحد «تصمیم کمیته» جای دو دکمه. |
| GAP-PROCESS-005 | ✅ اصلاح شد | `CommitteeDecide`: مقصد ۱۹۰۴/۱۹۰۵/۱۹۰۶/۱۹۰۸ + مسئول + LeadTime + پیوست + کامنت (عین کمبوی HTS). `CommitteeAccept` به‌عنوان سازگاری = مقصد ۱۹۰۴. |
| GAP-PROCESS-006 | ✅ اصلاح شد | تصمیم 2026-09-17: `CommitteeReject` و `CommitteeAccept` حذف شدند؛ کمیته فقط مقصد انتخاب می‌کند (`CommitteeDecide`) — عین HTS. |
| GAP-PROCESS-007 | ✅ اصلاح شد | فرم استعلام (`_ProductionOrderItemInquiryPartial`) + ردیف Inquiry با مسئول/LeadTime/پیوست؛ LeadTime دیگر با «اکنون» جعل نمی‌شود. |
| GAP-PROCESS-008 | ✅ اصلاح شد | جاب مهلت مهندسی: فیلتر روی `CheckStatus=2195` (نه مرحله)، مقصد ۱۹۰۴/۲۲۰۲ با همان قاعده، اعلان صنایع/کمیته. |
| GAP-PROCESS-009 | ✅ اصلاح شد | مبنای مهلت PM = آخرین کامنت مرحله ۱۳۸۰ (مثل HTS). تصمیم 2026-09-17: رفتار سیستم جدید تأیید شد — قلمِ ۲۲۵۸ پس از مهلت به صنایع ۱۹۰۴ می‌رود (رفع بن‌بست HTS)؛ اقلام مسیر BOM فقط مرحله ۲۷۴۴. |
| GAP-PROCESS-010 | ✅ اصلاح شد | تصمیم 2026-09-17: دکمه/اکشن `DoFinancialConfirm` از پنل حذف شد؛ منبع حقیقت تأیید مالی فقط راهکاران (Job) — عین HTS. |
| GAP-PROCESS-011 | ✅ اصلاح شد | `SaveComment`: ۱۷۸۹→پایان تولید، ۱۸۴۳→پایان تست، ۱۷۹۱→آماده‌سازی روی قلم. |
| GAP-PROCESS-012 | ❎ رد ادعا | `CheckSerialIsExist` در HTS چک «وجود سریال» برای ماژول تولید (PrdActivity) است، نه یکتایی سریال قلم. |
| GAP-PROCESS-013 | ❓ سوال | `DoUpdateHamkaranOperation` — Q8. |
| GAP-PROCESS-014 | ✅ اصلاح شد | ویرایش فیلدهای واحد فروش BOM (توضیحات فروش/AVL/مرحله) روی قلمِ ۱۳۸۰ → مرحله ۲۷۴۴ + کامنت + اعلان «تعیین تکلیف». BOM→PM دیگر CheckStatus را ۲۲۵۸ نمی‌کند (عین HTS). |
| GAP-PAGE-001 | ✅ اصلاح شد | دکمه «قطعات یدکی» (مودال فقط‌خواندنی از `Inv.PartSparePart`). |
| GAP-PAGE-002 | ✅ اصلاح شد | همان فرم استعلام (PROCESS-007). |
| GAP-PAGE-003 | ⏸ بعداً | تصمیم 2026-09-17: چاپ فعلاً پیاده نمی‌شود؛ بعداً قالب Stimulsoft در ReportBuilder Designer + `ViewReportByName`. |
| GAP-PAGE-004 | ❎ رد ادعا | نمایه `POI_All_ReadyForShipping` (Id 123، نقش `Sale.ProductionOrderItem.ReadyForShipping`) در DB dev وجود دارد؛ فقط در seed ریپو نیست. |
| GAP-PAGE-005 | ❎ رد ادعا | اسکریپت جابه‌جایی در `CustomActionButtonsJson` نمایه 104 (و کپی‌ها) در DB است و به `Swap` پست می‌کند. |
| GAP-PAGE-006 | ✅ اصلاح شد | `Seed_ProductionOrder_DataProfiles.sql`: `PO_All`، `PO_Cartable_Financial`، `PO_Related` با ستون‌های گرید HTS + RoleAccess. (نمایه دستی 36 هم در DB بود.) |
| GAP-PAGE-007 | ✅ اصلاح شد | `EnsureRoutineBomAsync` هنگام باز شدن BOM قلم روتین (از `Bom.vw_ProductItems`). |
| GAP-PAGE-008 | ⏸ حفظ | — |
| GAP-PAGE-009 | ✅ اصلاح شد | دکمه «پیوست‌های سفارش ساخت» روی Edit قلم (۵ پیوست سربرگ، دانلود). |
| GAP-PAGE-010..014 | ⏸ حفظ | — |
| GAP-JOB-001 | ✅ اصلاح شد | جاب `SendExpiredInquiryItemsNotification`. |
| GAP-JOB-002 | ✅ اصلاح شد | جاب `AddRoutineBomListTask`. |
| GAP-JOB-003 | ❓ سوال | غیرفعال‌سازی جاب کات‌اور پس از برش — Q10. |
| GAP-JOB-004 | ⏸ چک‌لیست | فعال‌سازی ۱۱ هندلر در پنل Job — فهرست در `04-Fix-Log.md`. |
| GAP-TRIGGER-001/002 | ⏸ حفظ | — (اصلاح جزئی: کامنت‌های «مرحله ساخت» دیگر `CheckStatus` را بازنویسی نمی‌کنند — عین تریگر HTS). |
| GAP-ACCESS-001 | ⏸ حفظ | محدودیت پلتفرمی `FetchData`؛ لیست‌ها Data Profile هستند. |
| GAP-ACCESS-002 | ✅ اصلاح شد | `POI_Sales_Related_Active`: WHERE کاربر جاری (ایجاد/ویرایش‌کننده، تیم فروش، مدیر پروژه) + هدر تأیید مالی. |
| GAP-ACCESS-003 | ✅ اصلاح شد | `PO_Related` (نقش جدید `Sale.ProductionOrder.ViewRelated` = 200036). |
| GAP-ACCESS-004 | ✅ اصلاح شد | `POI_ViewActive`: `ProductionOrder.State = 3`. |
| GAP-ACCESS-005 | ❎ رد ادعا | نمایه ثابت صنایع = گرید HTS (همه ۱۹۰۴/۱۹۰۵)؛ کارتابل «منتظر اقدام» همان `POI_MyCartable` است. |
| GAP-ACCESS-006 | ❓ سوال | واحد سازمانی ۲۶۲/۲۶۳ — فیلد واحد روی سربرگ نیست — Q11. |
| GAP-ACCESS-007 | ✅ جزئی | `TODO(Role)` در اکشن‌های اصلاح‌شده حذف شد؛ نقش‌ها seed هستند. انتساب کاربر با UI نقش. |
| GAP-ACCESS-008 | ❓ سوال | ستون قیمت BOM در پنل وجود ندارد — Q12. |
| GAP-ACCESS-009 | ✅ اصلاح شد | با حذف تأیید مالی از پنل، شاخه State=2 از `POI_MyCartable` حذف شد و کارتابل مالی ساخته نمی‌شود (عین HTS). |
| GAP-NOTIFY-001 | ✅ اصلاح شد | هشدار نزدیک‌تحویل با قاعده تاریخ HTS (روز دقیق ۱۵، max(توافقی، استاندارد)) → گروه اعلان `Sale.ProductionOrderItem.NearDeliveryDate`. |
| GAP-NOTIFY-002 | ✅ اصلاح شد | تغییر سریال قلم دارای بازرسی → گروه `SerialChangeQc`. |
| GAP-NOTIFY-003 | ✅ اصلاح شد | ۲۲۰۹ + مدیر پروژه → گروه `TestStart`. |
| GAP-NOTIFY-004 | ✅ اصلاح شد | اعلان تغییر وضعیت تولید (تیم فروش + CC فرم + گروه `ProductionStatusChange`). لیست‌های ایمیل شعبه‌ای سخت‌کد HTS عمداً منتقل نشد. |
| GAP-NOTIFY-005 | ✅ اصلاح شد | اعلان استعلام (مسئول/کمیته/صنایع) پس از تصمیم کمیته و بازگشت ۱۹۰۹. |
| GAP-NOTIFY-006 | ✅ اصلاح شد | `Seed_ProductionOrderItem_NotificationGroups.sql` (۸ گروه). |
| GAP-NOTIFY-007 | ❓ سوال | اعلان مالی راهکاران — Q13. |

---

*پایان ثبت مغایرت.*
