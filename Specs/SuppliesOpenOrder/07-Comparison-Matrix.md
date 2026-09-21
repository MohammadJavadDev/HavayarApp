# 07 — ماتریس مقایسه دقیق HTS ↔ HavayarApp (تدارکات / درخواست‌های باز)

تاریخ: **2026-09-17** (1405/06/26) — بازبینی مقایسه 2026-09-15.  
منابع: کد HTS (`D:\Projects\Hts Project\Hts Project\HtsProject`)، TotalSystem (`172.20.40.27`)، کد HavayarApp، HavayarApp DB (`172.20.40.42`) — همه **فقط‌خواندنی**. بسته شواهد خام این دور: `%LOCALAPPDATA%\Temp\havayar-compare\` (`02-hts-inventory.md`, `03-havayar-inventory.md`, `04-db-truth.md`, `db\*`).  
**هیچ تغییری در کد داده نشده.** برای تصمیم‌ها و راه‌حل‌ها → `99-Discrepancies.md` (نسخه 2026-09-17).

## 0. راهنما

| وضعیت | معنی |
|---|---|
| `مطابق` | رفتار/داده هم‌ارز HTS است (اختلاف نام یا نوع بی‌اثر) |
| `تفاوت` | وجود دارد ولی رفتار/دامنه/دسترسی با HTS فرق دارد |
| `غایب` | در جدید نیست |
| `اضافه در جدید` | در HTS نبود؛ در جدید هست |
| `نامشخص` | نیازمند تست UI / دسترسی بیشتر |

برچسب شواهد: **[H]** کد HTS · **[T]** TotalSystem DB · **[C]** کد HavayarApp · **[N]** HavayarApp DB. مسیرهای `db\…` نسبت به بسته شواهد هستند.

---

## 1. فرآیند

### 1.1 چرخه حیات درخواست باز

| گام | HTS | HavayarApp | وضعیت | D |
|---|---|---|---|---|
| ورود/به‌روزرسانی از ERP | `WriteData.Import_AllOpenOrderRequest` هر **۱۵ دقیقه**، ساعت ۶–۲۱ [H `HtsTaskService.cs`] | `OpenOrderRequestJob.SyncOpenOrderRequestJobFromRahkaran` هر **۳۶۰۰ ثانیه**؛ ۷۰۶/۷۳۷ اجرا موفق، ۹ خطای «Incorrect syntax near ')'» [N `db\N05_jobs`] | `تفاوت` (بازه + خطا) | D48 |
| محاسبه (اختتام، حذف نرم، احیا، Delay، PO، SalesUnit، IsRoutine) | `dbo.Sup_Compute_OpenOrderRequest` (۲۰KB، 2025-08-13) [T `db\legacy_defs`] | `ComputeOpenOrderRequest` + `CalculateDelaysBuyDay` داخل همان جاب [C `OpenOrderRequestJob.cs` L1270–1358] | `مطابق` (منطق) — Delay عملاً بی‌اثر: lead time هر ۱۱۴ دسته NULL | D37 |
| تایید خودکار مهندسی | مدرک اصلی 264/265/271/308 یا NotNeedToAttachDocuments؛ SupplyDate از `Pln_LeadTime` [H `OpenOrderRequestService.cs` ~898] | `AutoAcceptEngineeringForRoutineParts`: Data_Sheet/Detail_Drawing/Wiring_Diagram/Technical_Documents + `Part.DocumentsNotRequired`؛ SupplyDate از `Pln.LeadTime` (**۰ ردیف**) [C L965–1058] | `مطابق` (منطق) / `تفاوت` (داده) | D5 |
| تایید مهندسی دستی | `DoSetEngineeringAcceptOperation` — Accept_Engineering(12)، نه‌متوقف، نه‌تاییدشده | `EngineeringAccept` — `CurrentUserHasAnyRole("…EngineeringAccept")`، همان دو شرط [C L231–289] | `مطابق` | — |
| تایید فروش/پروژه | 187 + **VPIS لینک‌شده** + کاربر ∈ SalesExpert/SalesManager/PM ردیف | فقط نقش + تایید مهندسی + !auto + !تکراری [C L302–365] | `تفاوت` | D20, D36 |
| ثبت کامنت | بدون Requested_Personel ذخیره نمی‌شود | `SaveComment` بدون گیت [C L178–211] | `تفاوت` | D21 |
| پیوست | صفحه 71؛ `Changed=true`؛ ایمیل تولید برای انواع خاص | `SaveAttachment` (بدون ActionDisplayName/نقش)؛ `Changed=true`؛ اعلان هاردکد/TODO [C L761–805] | `تفاوت` | D25, D43 |
| وضعیت متنی «در راه» / «در حال استعلام» | Sending(24)/Query(25) — فقط FullAccess عملاً | `SetInWayStatus`/`StatusInquiry` (TODO دسترسی) | `مطابق` (رفتار) / `تفاوت` (دسترسی) | D9 |
| «راه اندازی شده» به‌عنوان وضعیت متنی | ندارد (Start فقط IsLaunch کامنت) [T `OpenOrder_Status` فقط NULL/در راه/در حال استعلام] | `Triggering` می‌نویسد `Status='راه اندازی شده'` | `اضافه در جدید` (بی‌خطر) | D51 |
| توقف چندمرحله‌ای | Stop(22) + کارتابل عامل توقف | `StopOperation` — همان ماشین، مهلت ۲/۳ روز، **بدون چک نقش** [C L382] | `مطابق` (ماشین) / `تفاوت` (دسترسی) | D9 |
| یادآوری مهلت پاسخ توقف | جاب روزانه ۹:۳۵ | — | `غایب` | D7 |
| تایم‌اوت ۱۴ روز → 2821 | جاب روزانه ۹:۳۵ (فقط اگر ایمیل OK) | — | `غایب` | D7 |
| گزارش روزانه کامنت‌های تدارکات | جاب روزانه ۹:۳۵ (`SupplysSystemTask`) | — | `غایب` | D7 |
| خاتمه دستی | Terminate(101) → IsForceDeletedByUser + IsDeleted | `Terminate` همان، TODO دسترسی [C L698–714] | `مطابق` / `تفاوت` (دسترسی) | D9 |
| حذف فیزیکی / ایجاد دستی درخواست | ندارد | `Delete` (hard) و `Add/New/Save` با ActionDisplayName و RoleAccess برای ۴ نقش؛ `delete` در ۵ پروفایل فعال [N `db\N04`, `_summary.md`] | `اضافه در جدید` (ریسک) | D35 |
| ایمیل به پیمانکار (از تنظیمات) | `SendEmailToContractor` → `EmailToSupplier` + `Changed=false` + `ManCompanyId` | — (`addSupplier` هندلر خالی [C `Edit.cshtml` L612–618]) | `غایب` | D1, D2 |
| تامین‌کننده منتخب گروهی | `AddComponyMansToRequests` | — | `غایب` | D2 |
| تخصیص پرسنل 2829/2830 | جدول `Requested_Personel` + دنرمال | `AddRequestedPersonelToOpenRequest` (چند Id) — فقط دنرمال | `تفاوت` | D13 |
| تغییر رویژن VPIS → ایمیل فروش/پروژه | HTS (رویداد Edms) | `CheckVpisRevisionChangesAndNotify` تعریف شده، **بدون JobSchedule**؛ `Sup.OpenOrderRequestVpis` = ۰ ردیف | `غایب` (عملاً) | D11, D36 |
| رسید انبار / رسید جزئی / رد QC | ایمیل SMTP به درخواست‌کننده‌ها | اعلان درون‌برنامه‌ای (۱۸٬۵۱۳ ردیف) — ایمیل ارسال نمی‌شود (جاب ایمیل غیرفعال) | `تفاوت` | D40 |

### 1.2 Lookupها / وضعیت‌ها

| LookupType HTS [T `db\L03c`] | مقادیر | معادل جدید [C `Entities/App/Sup/Enums`] | وضعیت |
|---|---|---|---|
| 349 StopStatus | 2807, 2808, 2810, 2811, 2812, 2813, 2815, 2816, 2817, 2821 | `OpenOrderRequestStopStatusEnum` همان IDها؛ در DB int (0 ×122,683، **NULL ×1,848**) | `مطابق` (NULL≠0 فقط ظاهری) D50 |
| 346 StopType | 2796, 2797, 2798 | `OpenOrderRequestStopTypeEnum` | `مطابق` |
| 347 StopCheckingResult | 2799–2803 | `OpenOrderRequestCommentStopCheckingResultEnum` | `مطابق` |
| 348 StopCheckingStatus | 2804–2806 | `OpenOrderRequestStopCheckingStatusEnum`؛ روی کامنت‌های مهاجرت‌شده همه NULL | `مطابق` (مدل) / `نامشخص` (داده) D50 |
| 350 UserCategory | 2829 سایر ذینفعان / 2830 مهندسی | `RequestedPersonelIds` / `RequestedEngineeringPersonelIds` | `تفاوت` (بدون جدول فرزند) D13 |
| 162 BuyCategoryType | 1040 خرید / 1041 ساخت | `BuyCategoryTypeEnum` 1 / 2 | `تفاوت` (بی‌اثر؛ مصرف‌کننده‌ای 1040 نمی‌خواهد) D15 |
| 66 نوع مدرک پیوست | 22 نوع | `OpenOrderRequestAttachmentFileTypeEnum` همان IDها | `مطابق` |
| 351 InquiryPriceStatus | 2831–2834 | `InquiryPartPricePriceStatusEnum` | `مطابق` (خارج از محدوده) |
| `OpenOrder_Status` متنی | NULL / در راه / در حال استعلام | همان + «راه اندازی شده» | `اضافه در جدید` D51 |

### 1.3 گذارهای توقف

| از → به | محرک HTS | پیاده‌سازی جدید | وضعیت |
|---|---|---|---|
| 0 یا 2815 → 2807 | ثبت توقف (Stop 22) | `StopOperation` case 0/2815 [C L410] | `مطابق` (نقش سرور ✘) |
| 2807/2813/2816 → 2808/2810/2811/2812/2813 | نتیجه عامل توقف (347) | `StopOperation` | `مطابق` |
| 2808–2812/2821 → 2815 (IsStop=false) | تصمیم 2804 | `StopOperation` | `مطابق` |
| 2808–2812/2821 → 2816 | تصمیم 2805 | `StopOperation` | `مطابق` |
| 2808–2812/2821 → 2817 (حذف) | تصمیم 2806 | `StopOperation` | `مطابق` (بدون تست E2E) |
| 2808–2813 → 2821 | جاب ۹:۳۵، >۱۴ روز | — | `غایب` D7 |

---

## 2. فیلدها

### 2.1 `Sup_OpenOrderRequest` ↔ `Sup.OpenOrderRequest` [T `db\L01b`] [N `db\N01`]

| HTS | جدید | وضعیت |
|---|---|---|
| OpenOrderRequest_ID | Id | `مطابق` (Id قدیم نگه نمی‌شود) |
| OrderRowId / Year / OrderNo | همان | `مطابق` |
| OrderDate / OrderDateInEurope | OrderShamsiDate / OrderMiladiDate | `مطابق` |
| Acc_DL_FK | DlId → FIN.DL | `مطابق` |
| Inv_Part_FK | PartId (اجباری) | `مطابق` |
| NeedDate / OrderConfirmDate | *ShamsiDate / *MiladiDate | `مطابق` |
| SumSendQty / FactoredCount / OrderQty / RequiredQty (dec 12,2) | long? / int / int? / int | `تفاوت` (اعشار حذف؛ بی‌اثر برای تعداد صحیح) |
| OrderItmComment | OrderItemComment | `مطابق` — در پروفایل با برچسب غلط «کامنت آخر» D41 |
| IsStop / Stop_Date | IsStop / StopShamsi+Miladi | `مطابق` |
| IsDeleted / IsDeletedDate / IsForceDeletedByUser | همان | `مطابق` |
| Requested_Personel_RegDate / Requested_Personel / Requested_PersonelEmail | همان + **RequestedPersonelIds** | `تفاوت` D13 |
| OpenOrder_Status | Status | `مطابق` |
| Changed / Notify_Email_Requested_Personel / Notify_Email_Send_Paper_Ids | همان | `مطابق` |
| Engineering_Accept / _User_FK / _Date / _Time / DateInEurope | EngineeringAccept / UserId / ShamsiDateTime / ConfirmationMiladiDateTime | `مطابق` (ساعت در DateTime) |
| IsAcceptedAutomaticallyByEngineering | همان | `مطابق` |
| Completion_Date / Completion_Time | CompletionShamsi/MiladiDate | `مطابق` (ساعت در MiladiDate) D18 |
| DelaysBuyDay | DelaysBuyDay | `مطابق` (مقدار وابسته به D37) — در هیچ پروفایلی نیست D41 |
| ProductionOrderNumber / ProductionOrderId | همان → Sale.ProductionOrder | `مطابق` |
| IsRejectedByInspection | همان | `مطابق` |
| FactoredDate / InText | FactoredMiladi/Shamsi | `مطابق` |
| Comment | Comment | `مطابق` |
| PurchaseRequestItemId / Number / Date | همان جفت | `مطابق` |
| HasSalesUnitConfirmation + UserId/Date/Comment | همان | `مطابق` |
| SalesUnitSalesExpertId / ManagerId / ProjectManagerId | همان → system.User | `مطابق` (پرشدن از جاب) |
| RelatedPartId | RelatedPartId | `مطابق` — در پروفایل نیست D41 |
| Delivery/Temporary/Qc/FinalInventory VoucherDate | جفت Shamsi/Miladi | `مطابق` |
| SupplyDate | SupplyShamsi/Miladi | `مطابق` (منبع LeadTime خالی) D5 |
| HasSalesUnitPrimitiveApprove / IsRoutineRequest | همان (IsRoutine غیر nullable) | `مطابق` |
| StopStatusId / StopCheckingStatusId | StopStatus / StopCheckingStatus (enum) | `مطابق` |
| Requested_EngineeringPersonel / Email | همان + Ids | `تفاوت` D13 — در پروفایل نیست D41 |
| ManCompanyId → Gnr_ManCompany | ManCompanyId → Gnr.Supplier | `مطابق` — بدون UI نوشتن D2؛ در پروفایل نیست D41 |
| ایندکس یکتا (OrderRowId, PurchaseRequestItemId, PurchaseRequestNumber) | فقط PK + ایندکس FK | `غایب` D44 |
| ستون‌های محاسباتی ویو: HaveAttachment، PurchaseCompleted، EmailSendToSupplier، CompanyName_EmailSended، آخرین کامنت 51/12/سایر، SalesBranch، Inv_Part.CompanyNames | هیچ‌کدام در پروفایل‌ها | `غایب` D41, D1, D19 |

### 2.2 `Sup_OpenOrderRequest_Comment` (۴۲ ستون) ↔ `Sup.OpenOrderRequestComment` (۳۳)

| HTS | جدید | وضعیت |
|---|---|---|
| Comment_Date / Comment_DateInLatin | ShamsiDate / MiladiDate | `مطابق` |
| CreatedUser_FK / CreatedDate / Time / UpdatedUserId / Date | BaseEntity Created*/Modified* | `مطابق` |
| **CreatedOrgUnit_FK** (51 تدارکات / 12 صنایع — پایه `Vw_Sup_Open_LastUnitComment`) | — | `غایب` (ستون «آخرین کامنت تدارکات/صنایع» بازسازی‌ناپذیر) D41 |
| IsStop / IsLaunch / HasSalesUnitConfirmation / SalesUnitConfirmationComment | IsStop / IsLaunched / … | `مطابق` |
| Comment_Value | CommentValue | `مطابق` |
| SubjectId (همه NULL) | — | `غایب` (مرده) |
| StopTypeId / StopOperatorId / BeneficiariesIds | StopType / StopOperatorId / BeneficiariesIds + **BeneficiariesNames** | `مطابق` |
| ResponseDeadlineDate | ResponseDeadline Miladi/Shamsi | `مطابق` |
| AttachmentFileName/Path/Size ؛ AlternativeAttachment* | AttachmentId / AlternativeAttachmentId → FileEntity | `مطابق` |
| StopCheckingResultId / Date / Comment / DelayReasonText | همان | `مطابق` |
| IsNeedToUpdateBom / IsNeedToDeleteBom | همان | `مطابق` |
| ProjectManagerApproximateCommentDate | همان جفت | `مطابق` |
| StopCheckingStatusId / Comment | StopCheckingStatus / StopCheckingStatusComment — روی داده مهاجرت‌شده همه NULL | `مطابق` (مدل) / `نامشخص` (داده) D50 |
| AdditionalDescription | همان | `مطابق` |

### 2.3 پیوست، VPIS، پرسنل، ایمیل، رخداد

| جدول HTS (ردیف) | جدید (ردیف) | وضعیت |
|---|---|---|
| `Sup_OpenOrderRequest_Attachment` (15,514) — فایل مسیر/varbinary، نوع 66، Comment | `Sup.OpenOrderRequestAttachment` (15,587) — AttachmentId→FileEntity، FileType، Comment، **HtsId**؛ جاب روزانه 41 | `مطابق` |
| `Sup_OpenOrderRequestVpis` (2,859، آخرین 2026-09-16، بخشی خودکار «درج بصورت اتوماتیک توسط سیستم») | `Sup.OpenOrderRequestVpis` (**0**) — ProjectId/ProjectVpisId/DocumentId/RevisionNumber/IsLatest/Comment | `مطابق` (مدل) / `غایب` (داده + درج خودکار) D36, D32 |
| `Sup_OpenOrderRequest_Requested_Personel` (998,245) — RequestedPersonel_FK→HRM_Personel، UserCategoryId 2829/2830، یکتا | — (دو ستون Ids روی والد) | `تفاوت` D13 |
| `Sup_OpenOrderRequest_EmailToSupplier` (37,952؛ 31,911 درخواست؛ 105 شرکت؛ تا 1405/06/25) — OpenOrderRequest_FK، Company_FK، CreatedUser_FK، SendDate، SendTime، Attachment_FK، PartAttachmentId، EdmsDocumentAttachmentId، Comment، SendingCount | — | `غایب` D1 |
| `Sup_OpenOrderRequest_Event` (67، همه «اختتام» 1394) | — | `غایب` (مرده در HTS) D14 |

### 2.4 اطلاعات پایه

| موجودیت HTS | فیلد HTS | جدید | وضعیت |
|---|---|---|---|
| `Sup_BuyCategory` (111) | BuyCategory_ID (smallint، غیر identity) | BuyCategory.HamkaranId (= ID قدیم) | `مطابق` |
| | BuyCategory_Title | Title | `مطابق` |
| | User_FK متولی خرید | PurchaseResponsibleId → system.User | `مطابق` |
| | TypeId 1040/1041 | Type 1/2 | `تفاوت` (بی‌اثر) D15 |
| | RoutineLeadTimeInDay / NonRoutineLeadTimeInDay (مقدار دارد: ابزار دقیق 45/90، موتور 60/120، پکیج روغن 180/180) | همان دو ستون — **۱۱۴/۱۱۴ NULL** | `غایب` (داده) D37 |
| `Sup_BuyCategory_Part` (23,002) | InvPart_Fk (**یکتا**) ، BuyCategory_FK | `BuyCategoryItem.PartId` (بدون یکتایی؛ ۳۹ درخواست فعال با کالای چنددسته‌ای) ، BuyCategoryId ، + TimeInWay، Supplier (1/2)، Comments، HamkaranId | `تفاوت` D34, D27 |
| `Sup_BuyCategory_Company` (**0**) | Company_Fk، BuyCategory_FK | — | `غایب` (خالی در HTS) D29 |
| `Pln_LeadTime` (3,202) | PartId | `Pln.LeadTime.PartId` (nullable) | `مطابق` |
| | LeadTimeInDay | LeadTimeDay | `مطابق` |
| | LeadTimeInMinute (محاسبه‌ای) | — | `غایب` (بی‌اثر) |
| | **NonRoutineLeadTimeInDay** | — | `غایب` D17 |
| | SupplierUnitId 6/51 | Supplier enum 6/51 | `مطابق` |
| | Comment | Comment | `مطابق` |
| | CreatedUserId/Date، RahkaranPartId | BaseEntity / — | `مطابق` |
| | داده | **۰ ردیف** | `غایب` D5 |
| `Gnr_ManCompany` COMP (25,133) | نام، کد اقتصادی، موبایل، شناسه ملی، تلفن، فکس، کد پستی، ایمیل | `Gnr.Party` (FullName، EconomicCode، NationalId، Mobile، Email، HamkaranId) + `Gnr.Supplier` (PostalCode، PhoneNumber، Fax، Email، DlCode، Prefix) (28,715) | `مطابق` |
| | **Grade، ServicesAndProducts، RelatedPersonName، آدرس بلند** | — | `غایب` D16 |
| `Inv_Part_Company` (8,432) | PartId، CompanyId | `Inv.PartCompany` (8,423) PartId، SupplierId؛ جاب روزانه 45 | `مطابق` |
| `Inv_Part.CompanyNames` / `Company_FK` | رشته دنرمال + تک‌تامین‌کننده | — | `غایب` D19 |

---

## 3. صفحات

### 3.1 نقشه صفحه → صفحه/پروفایل

| HTS Page | معادل جدید | ستون‌ها/دکمه‌ها | وضعیت | D |
|---|---|---|---|---|
| 65 تامین‌کنندگان (`_CompanyManagement`) | `/panel/supplier/list` + پروفایل 38 `supplier_listinfo` (FullName، EconomicCode، Phone، PhoneNumber، Fax، PostalCode، Email؛ new/edit/delete/excel فعال؛ فقط نقش 100017) | New در HTS کامنت بود؛ در جدید فعال. Grade/خدمات/شخص مرتبط/آدرس نیست | `تفاوت` | D16, D31 |
| 70 دسته‌های خرید (`_BuyCategoryManagement`) | **صفحه والد ندارد**؛ `/panel/sup/buycategoryitem/list` (پروفایل 40: نام کالا، کد کالا، عنوان دسته، متولی — همه اکشن‌ها جز اکسل غیرفعال) | CRUD دسته HTS (عنوان/متولی/نوع/lead time) در UI جدید نیست؛ `BuyCategory/Edit.cshtml` بدون مسیر | `تفاوت` | D10, D37 |
| 72 اقلام دسته‌های خرید (انتساب گروهی کالا→دسته) | همان پروفایل 40 (فقط‌خواندنی؛ داده از Rahkaran USR3) | انتساب گروهی نیست | `تفاوت` | D10 |
| 73 پیمانکاران هر دسته [hidden] | — | ۰ ردیف در HTS | `غایب` (بی‌اثر) | D29 |
| 83 تامین‌کنندگان محصولات (`_ProductCompanyManagement`) | `/panel/inv/partcompany/list` + مودال `AddSuppliers` | CompanyNames و ایمیل هشدار 176240 نیست | `مطابق` (عملیات) | D19 |
| 414 زمان در راه (`_LeadTime` + ورود اکسل) | `/panel/pln/leadtime/list` + Edit | **۰ ردیف**؛ اکسل نیست؛ NonRoutine نیست | `تفاوت` | D5, D6, D17 |
| 74 درخواست‌های باز (`_OpenOrderRequestManagement` Kendo + ریبون + کامنت/توقف) | `/panel/sup/openorderrequest/list` (۸ پروفایل §3.2) + `Edit.cshtml` (۴ تب، ۱۱ دکمه) | ستون‌های غایب §3.3؛ عملیات از Edit تک‌ردیفی؛ اکسل ✔ | `تفاوت` | D8, D9, D22, D34, D41 |
| 71 پیوست درخواست‌های باز [hidden] | تب «مدیریت پیوست‌ها» در Edit + `_AttachmentListPartial` | بدون ActionDisplayName/نقش؛ ViewAttachment جدا نیست | `تفاوت` | D25 |
| 76 تنظیمات درخواست‌های باز (`_OpenOrderRequestConfigManagement`) | پروفایل 99 `vw_openRequestConfig` «تنظیمات درخواست های باز»: IsDeleted=0، defaultMultiSelect، دکمه «افزودن درخواست کنندگان و ذینفعان» → `AddRequestedPersonelToOpenRequest`؛ نقش‌ها 100011/100009/100017/200052 | ✔ گرید همه فعال‌ها + تخصیص گروهی پرسنل. ✘ ایمیل به پیمانکار، تامین‌کننده منتخب، گرید فرزند ایمیل، انتخاب واحد سازمانی، ستون‌های EmailSendToSupplier/DelaysBuyDay/Completion_Date/BuyProgress/ManCompanyName، رنگ سبز/بنفش، `debugger` و `openOrderRequestId=1` هاردکد | `تفاوت` (نیمه) | D3, D2, D1, D41, D22, D47, D49 |
| 77 پیشینه درخواست‌ها (`_OpenOrderRequestHistoryManagement`) | پروفایل 73 `vw_openOrderRequestPurchaseCompleted` «پیشینه درخواست ها»: IsDeleted=1؛ edit/delete/excel فعال؛ نقش‌ها 100009/100017/200052/100007 | ✔ آرشیو حذف‌شده‌ها + اکسل. ✘ فقط‌خواندنی نیست (delete فعال)، نقش «مشاهده پیشینه» نیست (گروه‌های 44/395/70/… بی‌معادل)، ستون‌های PartCodingTitle/BuyProgress/RelatedPart/DelaysBuyDay/Completion، فیلتر part-permission | `تفاوت` (نیمه) | D4, D39, D45, D41, D8 |
| 545 پیشینه ارسال به پیمانکاران (`_OpenOrderRequestEmailToSupplier`: PurchaseRequestNumber، OrderNo، Part_Code/Name/Unit، CompanyName، OrderCompanyName، RequiredQty، SendingCount، SendDate/Time، BuyTrustee) | — | — | `غایب` | D1, D26 |
| منوی HTS برای `SystemType.Supp` **یا** `Eng` | `/panel/sup/openorderrequest/list` در منوهای 3 (تامین و خرید)، 2 (صنایع)، 9 (فروش)؛ منوی 1 (مهندسی) **غیرفعال** | مسیر منو برای ۲۶ عضو نقش تایید مهندسی وابسته به عضویت منوهای دیگر | `نامشخص` | D46 |

### 3.2 پروفایل‌های داده روی `Sup.OpenOrderRequest` [N `db\new_dataprofiles\_summary.md`]

همه ۸ پروفایل یک `CustomQuery` مشترک ۳۹ ستونی دارند: `Sup.OpenOrderRequest` LEFT `Inv.Part` **INNER `Sup.BuyCategoryItem`** LEFT `Sup.BuyCategory` LEFT `system.User`×3 LEFT `Inv.PartUnit` LEFT `Pln.LeadTime` LEFT `FIN.DL`. در HTS `Vw_Sup_OpenOrderRequest` دسته را **LEFT OUTER** می‌گیرد [T `db\legacy_defs\Vw_Sup_OpenOrderRequest.sql.txt` L29–33]. نتیجه زنده 2026-09-17: **۱۴۷ از ۱٬۳۹۸ درخواست فعال در هیچ پروفایلی دیده نمی‌شوند** و **۳۹** درخواست چندبار تکرار می‌شوند (D34).

| Id | Name | عنوان | فیلتر | اکشن‌ها | نقش‌ها | معادل HTS | وضعیت |
|---|---|---|---|---|---|---|---|
| 32 | openorderrequest_listinfo | تمامی درخواست ها | — (شامل حذف‌شده) | edit, delete, excel | 100009, 100017, 3, 200052 | Admin/FullAccess | `اضافه در جدید` (delete) D35 |
| 74 | openorderrequest_listinfonew | منتظر تایید مهندسی | EngineeringAccept=0 ∧ IsDeleted=0 | edit, delete, excel | 100000, 100009, 100017, 200052 | کارتابل مهندسی | `تفاوت` — مهندسی HTS همه فعال‌ها را می‌دید D8 |
| 72 | vw_OpenOrderRequestEngineeringAccepted | تأیید مهندسی شده | IsDeleted=0 ∧ EngineeringAccept=1 | edit, delete, excel | 100001, 100009, 100017, 100010, 200052 | دید فروش/پروژه/تامین | `مطابق` |
| 73 | vw_openOrderRequestPurchaseCompleted | پیشینه درخواست ها | IsDeleted=1 | edit, delete, excel | 100009, 100017, 200052, 100007 | صفحه 77 | `تفاوت` D4, D45 |
| 99 | vw_openRequestConfig | تنظیمات درخواست های باز | IsDeleted=0 | excel, multiSelect + دکمه پرسنل | 100011, 100009, 100017, 200052 | صفحه 76 | `تفاوت` D3 |
| 102 | vw_OpenOrderRequestAllActive | همه فعال | IsDeleted=0 | excel | 100008, 100009, 200052, 100007 | ShowAll(7) | `مطابق` |
| 103 | vw_OpenOrderRequestOtherView | مشاهده عمومی | IsDeleted=0 ∧ EngineeringAccept=1 ∧ RequestedPersonel IS NOT NULL | edit, delete, excel | **100009, 200052** | «بقیه فقط درخواست‌های خودشان» | `تفاوت` — به کاربر جاری محدود نیست؛ نقش مشاهده ندارد D8, D39 |
| 141 | vw_OpenOrderRequestMyRequestes | درخواست های من | IsDeleted=0 ∧ BuyCategory.PurchaseResponsibleId=@CurrentUserId | edit, excel | 100010, 200052 | فیلتر متولی | `اضافه در جدید` (مفید) |

رنگ ردیف: فقط `IsStop==='بله' → table-danger` در همه ۸ پروفایل. HTS Manage: نارنجی تایید فروش، آبی پنجره تامین ±۳ روز، سبز ایمیل پیمانکار، بنفش Changed، قرمز توقف؛ Config: سبز/بنفش/قرمز (D22).

### 3.3 ستون‌های گرید HTS صفحه 74/76/77 که در ۳۹ ستون مشترک نیستند

| ستون HTS | منبع | جدید | D |
|---|---|---|---|
| HaveAttachment | `att.MaxOpenOrderRequest_Attachment_ID` | — | D41 |
| PurchaseCompleted / BuyProgress | محاسبه FactoredCount vs RequiredQty | — | D41 |
| ManCompanyName (تامین‌کننده منتخب) | ManCompanyId (فقط برای Org 51/63) | ستون نیست (فیلد هست) | D41, D2 |
| EmailSendToSupplier / CompanyName_EmailSended | EmailToSupplier | — | D1, D22 |
| آخرین کامنت تدارکات / صنایع / سایر | `Vw_Sup_Open_LastUnitComment` (CreatedOrgUnit_FK) | فقط دکمه «لیست کامنت ها» | D41 |
| Requested_EngineeringPersonel | والد | ستون نیست | D41 |
| RelatedPart | والد | ستون نیست (در Edit هست) | D41 |
| DelaysBuyDay | SP | ستون نیست (در Edit هست) | D41, D37 |
| Completion_Date | SP | ستون نیست | D41 |
| SalesBranch | Pln_ProductionOrder→Sale_Branch | — | D41 |
| CompanyName (تامین‌کنندگان کالا) | `Inv_Part.CompanyNames` | — | D19 |
| PartCodingTitle (77) | Inv_Part coding | — | D41 |
| Engineering_Accept_Time جدا | والد | داخل DateTime | `مطابق` |

---

## 4. جاب‌ها

| اتوماسیون HTS | زمان HTS | جدید | زمان‌بندی زنده [N `db\N05`] | وضعیت | D |
|---|---|---|---|---|---|
| `Import_AllOpenOrderRequest` + `EXEC Sup_Compute_OpenOrderRequest` | هر ۱۵ دقیقه، ۶–۲۱ | Def 31 `SyncOpenOrderRequestJobFromRahkaran` (شامل Compute) | هر ۳۶۰۰s، فعال، آخرین 2026-09-17 22:00 OK | `تفاوت` (بازه) | D48 |
| `ChangeOpenOrderRequestStatusAndSendNotification` (تایید خودکار مهندسی) | بعد از import | داخل Def 31 `AutoAcceptEngineeringForRoutineParts` | هر ساعت | `مطابق` (گیرنده اعلان = گروه Industrial با ۰ عضو) | D24 |
| ایمیل رسید انبار / رسید جزئی / رد QC | داخل import | داخل Def 31 → Notification درون‌برنامه‌ای | — | `تفاوت` (ایمیل ✘) | D40 |
| `SupplysSystemTask`: یادآوری مهلت توقف، ۱۴ روز → 2821، گزارش کامنت تدارکات | روزانه ۰۹:۳۵:۰۱ | — | — | `غایب` | D7 |
| `Import_AllManCompany` (تامین‌کنندگان) | هر ۱۵ دقیقه | Def 18 `AddSupplierFromRahkaran` | هر 14400s، OK | `مطابق` | — |
| `Import_AllProduct` (کالا) | هر ۱۵ دقیقه | Def 4 `AddPartsFromRahkaran` | هر ساعت | `مطابق` | — |
| CRUD دستی دسته خرید (بدون جاب) | — | Def 30 `SyncBuyCategoryJobFromRahkaran` از USR3 | هر 3600s — **۳۴۸/۳۴۸ خطا از 2026-08-18؛ متوقف از 2026-09-02** («Key: 286») | `تفاوت` (منبع) + **خراب** | D10, D12 |
| — | — | Def 41 `SyncOpenOrderRequestAttachmentsFromHts` | روزانه 07:00، 27/27 OK | `اضافه در جدید` (پل مهاجرت) | — |
| — | — | Def 45 `SyncPartCompanyFromHts` | روزانه 07:01 | `اضافه در جدید` (پل) | — |
| ایمیل تغییر رویژن VPIS | رویداد Edms | Def 32 `CheckVpisRevisionChangesAndNotify` | **بدون JobSchedule**؛ هرگز اجرا نشده | `غایب` | D11, D36 |
| همگام `Pln_LeadTime` | (دستی + اکسل) | — | — | `غایب` | D5, D6 |
| ارسال ایمیل (SMTP مستقیم در HTS) | همزمان | Def 38 `System.EmailJob.SendPendingEmailsAsync` | **غیرفعال** از 2026-08-16 | `تفاوت` (ایمیل خاموش) | D40 |

---

## 5. تریگرها / پروسیجرها / ویوها

| شیء HTS [T `db\L05`, `L06`] | نقش | جدید | وضعیت | D |
|---|---|---|---|---|
| تریگر `Update_Requested_PersonelEmails` (Sup_OpenOrderRequest) | بازنویسی ایمیل پرسنل — **Disabled** 2024-04 | منطق در `AddRequestedPersonelToOpenRequest` | `مطابق` (پورت لازم نیست) | D30 |
| تریگر `UpdateProductionOrderItemBuyStatus` (Pln_ProductionOrderItemInquiry) | خارج از محدوده (سفارش ساخت) | `Sale.ProductionOrderItemInquiry` (۰ ردیف) | خارج از محدوده | — |
| SP `Sup_Compute_OpenOrderRequest` | §1.1 | `ComputeOpenOrderRequest` در جاب | `مطابق` | D37 |
| SP `_Delphi` / `_New` | نسخه‌های قدیمی | — | `غایب` (بی‌اثر) | — |
| SP `OrganizationalIndex_Supplies` | KPI (خارج از منو) | — | خارج از محدوده | — |
| fn `Get_OpenOrderRequest_RequestPersonEmail` | برای تریگر Disabled | — | `غایب` (بی‌اثر) | D30 |
| ویو `Vw_Sup_OpenOrderRequest` (LEFT دسته، آخرین ایمیل، آخرین کامنت واحد، پیوست، PO/شعبه) | گرید 74/76/77 | CustomQuery پروفایل‌ها (INNER دسته، بدون ایمیل/کامنت واحد/پیوست/شعبه) | `تفاوت` | D34, D41 |
| ویو `Vw_Sup_OpenOrderRequest_Active` | IsDeleted=0 | پروفایل 102 | `مطابق` | — |
| ویو `Vw_Sup_Open_LastUnitComment` | آخرین کامنت به ازای واحد | — (CreatedOrgUnit در کامنت جدید نیست) | `غایب` | D41 |
| `WebApp/Actions/Sup/*` | — | **وجود ندارد** | — (بدون نیاز فعلی) | — |
| DB جدید: تریگر/پروسیجر/ویو Sup | — | هیچ | — | — |

---

## 6. دسترسی‌ها

### 6.1 PageAction HTS → معادل جدید [T `db\L02c`] [N `db\N04`]

| Page · Permission | معادل جدید | ActionDisplayName / RoleAccess | چک سرور | وضعیت | D |
|---|---|---|---|---|---|
| 74 · Read(3) | `List`/`FetchData` + دسترسی پروفایل `dataProfile_<id>` | ✔ / ✔ | ✔ (کاتالوگ) | `مطابق` (دامنه دید: D8) | D8 |
| 74 · ExportToExcel(10) | `ExportToExcel` + `exportExcell` پروفایل | ✔ / ✔ | ✔ | `مطابق` | — |
| 74 · Edit(5) | `Edit`/`Update`/`Save` | ✔ / ✔ | ✔ | `مطابق` (Edit جدید فقط‌نمایش + کامنت) | — |
| 74 · Accept_Engineering(12) | `EngineeringAccept` + نقش 100000 (۲۶ عضو / HTS گروه 70: ۹۳ + 28: ۲۰ + مستقیم ۲۴) | ✘ / پروفایل 74 | ✔ `CurrentUserHasAnyRole` | `مطابق` (سرور) / `تفاوت` (اعضا) | D38 |
| 74 · SalesOrProjectPermission(187) | `SalesOrProjectAccept` + نقش 100001 (**۱ عضو** / HTS گروه 536: ۹) | ✘ / پروفایل 72 | ✔ نقش؛ ✘ VPIS/هویت | `تفاوت` | D20, D38 |
| 74 · HasEngineeringPermission(82) | نقش 100007 (۱۸ / HTS گروه 462: ۵۱ + 70: ۹۳) — فقط دسترسی پروفایل 73/102؛ دکمه linkVpis با نقش **EngineeringAccept** روشن می‌شود؛ `DoLinkVpis` TODO | ✘ / پروفایل | ✘ | `تفاوت` | D42, D9 |
| 74 · Stop(22) | `StopOperation` + نقش 100002 (۸۳ / HTS: گروه 44 ۱۹۳ + 27 ۲۹ + 593 ۳ + مستقیم ۵) | ✘ / **بدون RoleAccess** (فلگ) | ✘ | `تفاوت` | D9, D38 |
| 74 · Start(23) | `Triggering` + نقش 100003 (۱۲ / HTS گروه 28: ۲۰) | ✘ / فلگ | ✘ TODO | `تفاوت` | D9 |
| 74 · Sending(24) | `SetInWayStatus` + نقش 100004 (**۰** / HTS: فقط FullAccess گروه 17: ۱۲) | ✘ / فلگ | ✘ TODO | `تفاوت` | D9 |
| 74 · Query(25) | `StatusInquiry` + نقش 100005 (**۰** / HTS: فقط FullAccess) | ✘ / فلگ | ✘ | `تفاوت` | D9 |
| 74 · Terminate(101) | `Terminate` + نقش 100006 (۵ / HTS گروه 233: ۱۱) | ✘ / فلگ | ✘ TODO | `تفاوت` | D9, D38 |
| 74 · ShowAll(7) | نقش 100008 (۴۵ / HTS گروه 44: ۱۹۳ + مستقیم ۱) → پروفایل 102 | ✘ / پروفایل | — | `مطابق` (مکانیزم) / `تفاوت` (اعضا) | D38 |
| 74 · Attachment(19) ؛ 71 · Read/New/Edit/Delete/ViewAttachment | `SaveAttachment`/`DeleteAttachment`/`DownloadAttachment` | ✘ / ✘ | ✘ | `تفاوت` | D25 |
| 74 · Accept(13) «تایید» | دکمه‌ای در UI فعلی HTS ندارد (قدیمی؛ ۳ کاربر مستقیم) | — | — | `نامشخص` (بی‌اثر) | D52 |
| 74 · Refresh(26) | رفرش DataTable | — | — | `مطابق` (ضمنی) | D52 |
| 74 · FullAccess(2) | `isAdmin` در Edit + نقش 100017/200052 | — | — | `مطابق` | — |
| 74 · (هیچ) Delete / New | `Delete` (hard) و `New/Add/Save` با RoleAccess برای 100017/100000/100009/3 (+ `new` برای 100010/100011/100008/100001/100007) | ✔ / ✔ | ✔ | `اضافه در جدید` (ریسک) | D35 |
| 76 · Read(3) | پروفایل 99 + نقش 100011 (۱۴ / HTS گروه 430: ۲۱ + 28: ۲۰) | — / پروفایل | — | `مطابق` | D38 |
| 76 · SendEmail(27) | — | — | — | `غایب` | D2 |
| 76 · (DoOperation پرسنل، بدون PageAction) | `AddRequestedPersonelToOpenRequest` — UI با `AppSdk.hasRole("…ConfigManage")`، سرور بدون چک | ✘ / ✘ | ✘ | `تفاوت` | D9 |
| 76 · نفرات ثابت | نقش 100012 (۷ عضو) به‌جای لیست هاردکد صنایع | — | — | `مطابق` (بهتر) | — |
| 77 · Read(3)/ExportToExcel/Attachment | پروفایل 73 برای 100009/100017/200052/100007 — **گروه‌های 44 (۱۹۳)، 395 (۴۳)، 70 (۹۳)، 28، 303، 33، 60، 350 بی‌معادل** | — / پروفایل | — | `تفاوت` | D39, D4 |
| 545 · Read/FullAccess (گروه 525: ۶) | — | — | — | `غایب` | D1 |
| 65 · Read/New/Edit/Delete/Excel | پروفایل 38 فقط 100017 (۹) — HTS گروه‌های 17/27/60/350 و … | — | — | `تفاوت` (اعضا) | D38 |
| 70/72 · Read/…؛ 565 مشاهده دسته (۲) | پروفایل 40 فقط 100017/200052 | — | — | `تفاوت` | D10, D38 |
| 414 · Read/New/Edit/Delete (گروه 291: ۶ / 524: ۸) | `/panel/pln/leadtime/*` — RoleAccess در استخراج `/sup/` نبود | — | — | `نامشخص` | D38 |
| 83 · Read | `/panel/inv/partcompany/*` | — | — | `نامشخص` | — |

### 6.2 گروه HTS → نقش جدید (اعضا)

| گروه HTS (اعضا) | دسترسی HTS | نقش جدید (اعضای فعال) | وضعیت |
|---|---|---|---|
| 17 مدیر سیستم تدارکات (12) | FullAccess همه | 100017 SupplyAndPurchase (9) + 400008 SupplierMenu (20) | `مطابق` تقریبی |
| 27 کارشناسان تدارکات (29) | 74: Edit, Stop؛ 65/71/72/73/77 Full | 100010 Supply (22) + 100002 Stop (83) | `تفاوت` (نگاشت دستی) |
| 44 کارشناسان عمومی — درخواست‌های باز (193) | 74: Read, Excel, ShowAll, Stop؛ 77: Read؛ 71: View | 100008 ShowAll (44) — بدون پیشینه | `تفاوت` (۱۴۹ نفر کم) D38, D39 |
| 28 کارشناسان صنایع (20) | 74: Accept_Engineering, Excel, Start؛ 76 Full؛ 77 Read | 100009 Industrial (9) + 100003 Start (12) | `تفاوت` |
| 70 کارشناسان تایید مهندسی (93) | 74: Accept_Engineering, Edit, HasEng؛ 71 Full؛ 77 Read | 100000 EngineeringAccept (25) — بدون پیشینه | `تفاوت` (۶۸ نفر کم) |
| 462 ارتباط درخواست‌ها و پروژه‌ها (51) | 74: Read, HasEng | 100007 HasEngineering (18) — linkVpis برایشان غیرفعال | `تفاوت` D42 |
| 442 مشاهده درخواست‌های باز (106) | 74: Read؛ 71: View | **نقش مشاهده وجود ندارد** (پروفایل 103 فقط Industrial) | `غایب` D39 |
| 395 مشاهده پیشینه (43) | 77: Read, Attachment | **نقش مشاهده پیشینه وجود ندارد** | `غایب` D39 |
| 172 خدمات پس از فروش (40)، 303 مدیران پروژه (1)، 33 کنترل کیفیت (6)، 60 مشاهده (31)، 350 مشاهده کلی (15) | 74/77: Read (+Attachment/Excel) | نقش مشاهده ندارد | `غایب` D39 |
| 536 پروژه/فروش (9) | 74: Read, Attachment, SalesOrProject | 100001 SalesOrProjectAccept (1) | `تفاوت` (۸ نفر کم) |
| 233 خاتمه/حذف (11) | 74: Terminate | 100006 Terminate (5) | `تفاوت` |
| 430 تنظیمات (21) | 76: Full | 100011 ConfigManage (14) | `تفاوت` |
| 593 توقف (3) | 74: Read, Stop | 100002 Stop (83) | `مطابق` |
| 525 پیشینه ارسال به پیمانکاران (6) | 545: Full | — | `غایب` D1 |
| 291/524 زمان در راه (6/8) | 414 | ? | `نامشخص` |
| گرنت مستقیم کاربر روی 74 (Read ۲۸، Accept_Engineering ۲۴، Stop ۵، Accept ۳، ShowAll ۱) | — | مدل جدید فقط نقش | `تفاوت` (باید در نقش ادغام شود) D38 |
| — | — | 100004 Sending (0)، 100005 Query (0)، 100012 StaticPersonel (7) | `مطابق` با HTS (Sending/Query فقط FullAccess) |

---

## 7. اکشن‌ها (سطح دکمه)

| دکمه HTS (Permission) | دکمه جدید (`action-name` / پروفایل) | شرط UI جدید [C `Edit.cshtml` L551–607] | چک سرور [C `OpenOrderRequestController.cs`] | ActionDisplayName | وضعیت | D |
|---|---|---|---|---|---|---|
| ویرایش (5) | `edit` پروفایل → Edit (فقط‌نمایش) + `addComment` | همیشه فعال | `SaveComment` بدون گیت | ✘ | `تفاوت` | D21 |
| پیوست (19 / صفحه 71) | تب پیوست‌ها: افزودن/حذف/دانلود | همیشه | ✘ | ✘ | `تفاوت` | D25 |
| اتصال مدارک پروژه VPIS (82) | `linkVpis` | `isEngineering` (نقش EngineeringAccept) | `DoLinkVpis` TODO L1854 | ✘ | `تفاوت` | D42, D9 |
| تایید مهندسی (12) | `engineeringAccept` | `isEngineering && !accepted && !isStop` | ✔ L239 | ✘ | `مطابق` | — |
| تایید فروش/پروژه (187) | `salesOrProjectAccept` | `isProjectOrSalesUnit` | ✔ نقش L309؛ VPIS/هویت ✘ | ✘ | `تفاوت` | D20 |
| توقف (22) | `stopRequest` | بسته `stopOpersions` = Industrial ∨ Supply ∨ Engineering ∨ Stop | ✘ L382 | ✘ | `تفاوت` | D9 |
| راه‌اندازی (23) | `triggeringRequest` | بسته (مهندسیِ غیرصنایع خاموش) | TODO L603 | ✘ | `تفاوت` | D9 |
| در راه (24) | `inWayStatus` | بسته | TODO L650 | ✘ | `تفاوت` | D9 |
| درحال استعلام (25) | `statusInquiry` | بسته | ✘ | ✘ | `تفاوت` | D9 |
| خاتمه دادن (101) | `terminateRequest` | بسته + `hasTerminatePermission` در کلیک L1034 | TODO L702 | ✘ | `تفاوت` | D9 |
| ارسال به اکسل (10) | `exportExcell` پروفایل | نقش پروفایل | ✔ | ✔ | `مطابق` | — |
| نمایش همه (7) | پروفایل 102 | نقش 100008 | — | — | `مطابق` | — |
| بازخوانی (26) | رفرش DataTable | — | — | — | `مطابق` | D52 |
| تنظیمات → ویرایش/ذخیره پرسنل | `addRequestedPersonel` (Edit) + دکمه چندانتخابی پروفایل 99 | `hasConfigManagePermission` / `AppSdk.hasRole` | ✘ | ✘ | `تفاوت` (سرور) | D9 |
| تنظیمات → ارسال ایمیل به پیمانکار (27) | — | — | — | — | `غایب` | D2 |
| تنظیمات → افزودن تامین‌کننده منتخب | `addSupplier` — هندلر خالی L612 | همیشه فعال | — | — | `غایب` | D2 |
| پیشینه → مشاهده پیوست | Edit همان ردیف | — | — | — | `مطابق` تقریبی | — |
| — | `history` (تاریخچه موجودیت) | همیشه | — | — | `اضافه در جدید` | — |
| — | «لیست کامنت ها» (۷ پروفایل؛ با `debugger`/`console.log`) | انتخاب ردیف | `CommentListPartial` | ✘ | `اضافه در جدید` | D49 |
| — | `delete` پروفایل → `Delete` hard | نقش پروفایل | ✔ | ✔ | `اضافه در جدید` (ریسک) | D35 |

---

## 8. نوتیفیکیشن‌ها

| رویداد | گیرنده HTS | گیرنده جدید [C کنترلر/جاب] [N `db\N06`] | وضعیت | D |
|---|---|---|---|---|
| ثبت کامنت | Requested_Personel + ایمیل‌ها | Requested*Ids/Emails + گروه `Sup.OpenOrderRequest.Industrial` (**۰ عضو**) + متولی/تاییدکننده مهندسی | `تفاوت` (گروه خالی) | D24 |
| تایید مهندسی | مسیرهای ایمیل مهندسی/درخواست‌کننده | Sales* + متولی یا گروه `SupplyUnit` (**۰ عضو**) + نگاشت سرپرست هاردکد | `تفاوت` | D24, D43 |
| تایید فروش/پروژه | ایمیل تدارکات | لیست هاردکد `@havayar.com` | `تفاوت` (هاردکد باقی) | D43 |
| توقف: ثبت / بررسی عامل / تصمیم درخواست‌کننده | ایمیل عامل توقف + CC ذینفعان | Notification **Type=Email** به عامل، CC ذینفعان/درخواست‌کننده/متولی/مهندسی/PM — جاب ایمیل غیرفعال (۳ مورد قبل از 08-16 ارسال شد) | `تفاوت` (ارسال نمی‌شود) | D40 |
| راه‌اندازی | ایمیل درخواست‌کننده‌ها | Notification درون‌برنامه‌ای | `مطابق` (کانال متفاوت) | — |
| پیوست انواع خاص | ایمیل تولید (هاردکد) | هاردکد تولید + TODO | `تفاوت` | D43 |
| رسید انبار / جزئی / رد QC (جاب) | ایمیل درخواست‌کننده‌ها | ۱۵٬۳۷۲ + ۹۳۴ + ۲٬۱۶۸ اعلان درون‌برنامه‌ای (۸۲ مالک) | `مطابق` (کانال) / `تفاوت` (ایمیل ✘) | D40 |
| تایید خودکار مهندسی (جاب) | ایمیل | اعلان به درخواست‌کننده + گروه Industrial (۰ عضو) | `تفاوت` | D24 |
| تغییر رویژن VPIS (جاب) | ایمیل فروش/پروژه | تعریف شده، هرگز اجرا نشده | `غایب` | D11 |
| یادآوری مهلت توقف / 2821 / گزارش روزانه (جاب ۹:۳۵) | ایمیل | — | `غایب` | D7 |
| ایمیل به پیمانکار | SMTP + پیوست (درخواست/کالا/EDMS) + لاگ EmailToSupplier | — | `غایب` | D1, D2 |
| گروه‌های ایمیل 577/581 (اختلاف قیمت / اقلام بدون مبلغ) | — | — | خارج از محدوده (استعلام قیمت) | D33 |
| `NotificationBuilder` برای Sup | — | هیچ | — | — |

---

## 9. شمارش زنده 2026-09-17

| مورد | HTS | جدید |
|---|---|---|
| درخواست فعال (`IsDeleted=0`) | 1,053 (1,006 + 47 متوقف) | 1,398 (1,343 + 55) — **۱۴۷ بدون BuyCategoryItem (نامرئی)**، ۳۹ چنددسته‌ای (تکراری) |
| درخواست حذف‌شده | 124,569 | 124,276 |
| کامنت / پیوست / VPIS | 73,862 / 15,514 / 2,859 | 73,525 / 15,587 / **0** |
| EmailToSupplier | 37,952 (تا 1405/06/25) | **جدول ندارد** |
| Requested_Personel | 998,245 | — (دنرمال) |
| BuyCategory / Item | 111 / 23,002 | 114 / 23,087 (lead time دسته: ۰/۱۱۴) |
| LeadTime | 3,202 | **0** |
| Supplier / PartCompany | 25,133 COMP / 8,432 | 28,715 / 8,423 |
| گروه اعلان Sup با عضو | — | 0 از 4 |
| جاب خراب | (Agent ناخوانا) | BuyCategory (Def 30) از 2026-08-18؛ EmailJob (Def 38) غیرفعال؛ VPIS (Def 32) بدون زمان‌بندی |
