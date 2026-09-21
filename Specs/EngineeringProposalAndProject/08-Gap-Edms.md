# 08 — مغایرت مهندسی پروژه (HTS EDMS ↔ Havayar)

> چک‌لیست برای **بررسی شما**. تا وقتی نگویید کدام ردیف باید در سیستم جدید اصلاح شود، **کدی زده نمی‌شود.**
>
> تاریخ: **2026-09-15**. شواهد از `03`/`04` (HTS) و `06` (Havayar). منوی مرده (ExtraInfo، TimeSchedule) کمبود نیست مگر هنوز در فرآیند زنده استفاده شود.
>
> **ریسک بالا:** اعداد وضعیت مدرک یکی نیستند. HTS `Issue = 5`؛ Havayar `Issue = 2`. جاب `DocumentJob.MapDocumentStatus` remap می‌کند؛ همگام درصد پیشرفت پروژه **همان اعداد را CAST می‌کند**. جزئیات `D-40`.
>
> طبقه‌بندی: **کمبود** / **ناقص** / **متفاوت** / **اضافه در سیستم جدید** / **عمداً حذف در HTS**.

ماتریس صفحه: [`09-Page-Matrix.md`](09-Page-Matrix.md).

---

## خلاصه شمارش

| طبقه‌بندی | تعداد ردیف |
|---|---|
| کمبود | 30 |
| ناقص | 11 |
| متفاوت | 13 |
| اضافه در سیستم جدید | 7 |
| عمداً حذف در HTS | 2 |
| **جمع** | **63** |

---

## ۱. صفحات

| شناسه | موضوع | طبقه‌بندی | شواهد HTS | شواهد Havayar | توضیح |
|---|---|---|---|---|---|
| D-01 | کارتابل شخصی | کمبود | `03` §6.2 — `GetPersonalReferralDocuments`؛ سرپرست/`Edms_Project_User` / زیرمجموعه؛ رنگ ردیف با `LastDocument_Status_FK` | `06` §5: view کارتابل نیست | List مدرک صف «باید روی آن کار کنم» نیست — **ناقص محسوب نشود؛ صفحه نیست** |
| D-02 | اقلام پروژه (برگ 558) | کمبود | `03` §6.4 — `ProjectPartManagementController`؛ entity `Edms_Project_Part` | `06` §1.1: «no separate project parts entity» | تب تو در تو 164 هم روی شناسنامه HTS هست (`D-24`) |
| D-03 | Final DCC (دریافت و ارسال اسناد) | کمبود | `03` §6.10 — همان SystemPage 221؛ `IsForEmployer`؛ `SendEmailToEmployer`؛ کانفیگ `EdmsDccUserId` | `06` §4.5: فیلتر DCC فقط نقش روی کامنت؛ صفحه Final نیست | وضعیت‌های کارفرما در کمبوی نقش DCC تا حدی هست؛ کارتابل Final و ایمیل کارفرما صفحه جدا ندارد |
| D-04 | آرشیو شخصی | کمبود | `03` §6.11 — فیلتر با FullAccess/ShowAll روی صفحه **166** | `06` §5: «no dedicated views for … three archives»؛ enum `Archived=31` فقط | |
| D-05 | آرشیو کلی | کمبود | `03` §6.11 — زنجیره Admin → ShowAll → `Edms_Project_User` → `ProjectManagerAccess` → `TakvinProjectPermission` + گروه 92 | `06` §5 | |
| D-06 | آرشیو اسناد ورودی پروژه | کمبود | `03` §6.11 — اتحاد پیوست پروژه + پروپوزال + مناقصه؛ PermissionType 75/76/79 | `06`: `ProjectAttachmentList` فقط پیوست `Edms.ProjectAttachment` است نه اتحاد سه‌منبع | |
| D-07 | گزارش تجمعی عملکرد/فعالیت | کمبود | `03` §9 — فعالیت‌ها `ModuleType_FK==386` | `06` §5 | |
| D-08 | گزارش تجمعی حجم کاری پرسنل | کمبود | `03` §9 — `LoadCumulativePage`؛ همان page 361 | `06` §4.5: فقط `PersonnelWorkLoadReport` | برگ جدا HTS است |
| D-09 | گزارش عملکرد پرسنل | کمبود | `03` §9 — `Vw_Edms_PersonelPerformance` | `06` §5 | با گزارش EPMS 245 در HTS page id مشترک دارد |
| D-10 | گزارش تأخیرات مدارک | کمبود | `03` §9 — `Vw_Edms_DocumentDelay`؛ `ProjectManagerAccess` روی 232 | `06` §5 | |
| D-11 | گزارش توقفات مدارک | کمبود | `03` §9 — `Vw_Edms_Document_Hold` | `06` §5 | |
| D-12 | گزارش پیشرفت پروژه | کمبود | `03` §9 — `Vw_Edms_ProjectProgress` | `06`: درصد پیشرفت **فرم پروژه** هست؛ گزارش منو نیست | |
| D-13 | گزارش تجمعی پروژه | کمبود | `03` §9 — `Vw_Edms_ProjectSummerized` | `06` §5 | |
| D-14 | گزارش شاخص ارزیابی | کمبود | `03` §9 — `Vw_Edms_IndexEvaluation` | `06` §5 | |
| D-15 | گزارش اسناد کارفرما / وندور | کمبود | `03` §9 — `ShowAll` یا `Edms_Project_User` | `06` §5 | |
| D-16 | راهنمای PDF فارسی/لاتین | کمبود | `03` §6.12 — `EDMS_HELP_Rev_01_Fa.pdf` / `Rev_02_En` | `06`: فایل/اکشن LoadFile معادل نیست | |
| D-17 | ثبت فعالیت‌ها | ناقص | `03` §6.1 — کامنت Issue سپس 2/4؛ فیلتر زیرمجموعه/`ShowAll` | `06` §3.8 / §4.3 — CRUD بدون جدول کامنت | `HtsId` و جاب فعالیت نیست |
| D-18 | شناسنامه پروژه | ناقص | `03` §6.3 — تو در تو: اقلام 164، پیوست 218، یادداشت؛ HoldByProject هنگام وضعیت 4↔7 | `06` §4.1 — Edit با پیشرفت + پیوست؛ بدون اقلام و بدون `ProjectNote` | کدگذاری `ProjectAction.CheckCode` با الگوریتم دیگر (`D-41`) |
| D-19 | VPIS پروژه | ناقص | `03` §6.5 — مسئولین جدولی؛ Excel `DoOperationByExcel`؛ `SimilarVpisId` | `06` §3.4 — `ReviewersId` CSV؛ `Add` یک `Document` با `NotIssue` می‌سازد | بدون Excel؛ بدون جدول `Project_Vpis_Responsible` |
| D-20 | بارگذاری اسناد / ریویژن | ناقص | `03` §6.6 — Issue=5؛ Hold/UnHold؛ `IsLatest`؛ FullAccess لیست پروژه | `06` §4.5 — `List`/`EditAsGroup`/`Save` با `IsLatest`؛ `Add` وضعیت را **Issue (2)** می‌گذارد | صفحه هست؛ کارتابل Upload و Hold روی فرم HTS کامل پیاده نشده |
| D-21 | بررسی/تایید/رد اسناد | ناقص | `03` §6.7 — کارتابل checker/approver؛ FillData: 2,4,24,8,7,26 | `06` §4.5 — `AddNewComment` + `GetFilteredStatusOptions` روی Edit | **صفحه کارتابل کنترل نیست**؛ فقط زنجیره وضعیت روی مدرک باز |
| D-22 | DCC کنترل اسناد | ناقص | `03` §6.8 — کارتابل مدارک Approveشده؛ کامنت `IsForDcc=false`؛ `ApprovedByDcc` + `*` در عنوان → OpenOrder | `06` §4.5 / §10 — نقش `Edms.Documents.DccUsers` | بدون گرید DCC و بدون فلگ `IsForDcc` |
| D-23 | ترانسمیتال | ناقص | `03` §6.9 — چند مدرک + پیوست تو در تو 220؛ NotReview خودکار | `06` §3.7 / §4.4 — **یک** `DocumentId` اجباری؛ `AddNotReviewComments` هست | بدون `HtsId` و بدون جاب همگام |
| D-24 | تب اقلام روی شناسنامه (164) | کمبود | `03` §7 — nested `Edms_Project_Part` | `06`: entity نیست | جدا از برگ منوی 558 (`D-02`) چون SystemPage جدا است؛ هر دو غایب‌اند |
| D-25 | یادداشت پروژه | کمبود | `03` §6.3 — `LoadNotePage`؛ lookup 120 | `04` §14 `Edms_ProjectNote` | در Havayar `ProjectNote` نیست |

گزارش MDR و حجم کاری در ماتریس **موجود**اند؛ اختلاف فیلتر/دسترسی در `D-26`/`D-27`.

| شناسه | موضوع | طبقه‌بندی | شواهد HTS | شواهد Havayar | توضیح |
|---|---|---|---|---|---|
| D-26 | MDR — دسترسی/فیلتر | ناقص | `03` §9 — `ProjectManagerAccess` روی صفحه **232**؛ حذف وضعیت پروژه 3 و 5 | `06` §5 — `IEdmsMdrReportService`؛ حذف `StartNotStarted` و `Completed` | صفحه هست؛ معادل مدیر پروژه روی آرشیو 232 و view `Vw_Edms_Mdr` تضمین نشده |
| D-27 | حجم کاری پرسنل — ShowAll روی صفحه مرده | ناقص | `03` §9 — `ShowAll` روی `Edms_TimeScheduleReport` **306** | `06` §4.5 — صفحه با فیلتر ProjectId **یا** PersonnelId | رفتار دسترسی HTS به صفحه مرده 306 گره خورده |

---

## ۲. فرآیند، مدرک، ترانسمیتال، کامنت

| شناسه | موضوع | طبقه‌بندی | شواهد HTS | شواهد Havayar | توضیح |
|---|---|---|---|---|---|
| D-28 | Hold/UnHold روی Upload | ناقص | `03` §6.6 / §10 — Hold 11 / UnHold 12؛ lookup 89/104 | `06` §3.6 — `LegalHolder` / `HOLDOwner` روی کامنت | مسیر Hold در مدل هست؛ صفحه Upload HTS با کمبوی Hold جدا است |
| D-29 | تغییر وضعیت پروژه 4↔7 → HoldByProject / UnHold روی مدارک | کمبود | `03` §4 گام 2 | `06` `ProjectController` Update: rebuild پیشرفت از گراف پست؛ bulk کامنت HoldByProject ذکر نشده | |
| D-30 | ایمیل Issue / کنترل / DCC / کارفرما | کمبود | `03` §11 — `SendEmail` بعد از کامنت؛ `SendEmailToEmployer` | `06` §6.2 — `DocumentCommentAction` فقط کپی وضعیت و تاریخ؛ ایمیل نیست | `EmailAddress` / گیرنده ترانسمیتال روی `Project` ذخیره می‌شود؛ ارسال SMTP معادل HTS نیست |
| D-31 | OpenOrder وقتی عنوان VPIS شامل `*` و `ApprovedByDcc` | کمبود | `03` §6.8 | `06` Actions/Edms: نیست (جاب Sup جدا در `06` §9) | |
| D-32 | `IsForDcc` روی کامنت | کمبود | `04` §10 — false در DCC، true در Final | `06` §3.6: پراپرتی نیست | بدون این، تفکیک DCC داخلی و کارفرما در تاریخچه کامنت نیست |
| D-33 | `ReceivedTransmittalNumber` روی کامنت | کمبود | `04` §10 | `06` §3.6 | |
| D-34 | ترانسمیتال چندمدرکه + `Transmital_Document` | متفاوت | `04` §12 — junction + `IsSendToEmployer` per doc | `06` §3.7 — یک مدرک؛ `DeliveryDateToEmployer` روی هدر | |
| D-35 | `NotificationMethodId` روی مدرک | کمبود | `04` §9 — lookup 323 (2418/2419) | `06` §3.5: پراپرتی نیست | |
| D-36 | `ProposalId` / `IsCreatedFromProposal` / `TenderId` روی پروژه | کمبود | `04` §4 | `06` §3.1: در لیست پراپرتی `Project` نیست | حتی با اضافه شدن `E-17` جایی برای FK نیست |

---

## ۳. فیلد و enum (غیر از جدول کامل وضعیت)

| شناسه | موضوع | طبقه‌بندی | شواهد HTS | شواهد Havayar | توضیح |
|---|---|---|---|---|---|
| D-37 | مشتری اصلی | متفاوت | `04` §4 — `Main_Client_FK` اجباری `Gnr_ManCompany` | `06` §3.1 — `ContractPartyCustomer` رشته؛ `Title` از JS | |
| D-38 | `Edms_Project_User` / زیرمجموعه / محرمانه جدولی | ناقص | `04` §14 — جداول فیلتر کارتابل/آرشیو | `06` §3.1 — `SensitiveUsersIds` متن؛ `HasAccessToArchiveAndInputDocumentsId` **یک** کاربر | بدون جدول membership، فیلتر آرشیو/کارتابل HTS قابل پورت نیست |
| D-39 | مسئولین VPIS جدولی + `SimilarVpisId` + `OrgUnit_FK` | کمبود | `04` §7–8 | `06` §3.4 — CSV + Producer از sync `IsMain` | ورود Excel HTS هم غایب است (`D-19`) |
| D-40 | **اعداد وضعیت مدرک (ریسک بالا)** | متفاوت | `04` §2 — `Issue=5`, `Approve=2`, `Commented=4`, `ApprovedByDcc=6`, … | `06` §2.1 — `NotIssue=1`, `Issue=2`, `ApproveByReviewer=4`, `ApprovedByDcc=9`, … | جدول نگاشت پایین. هر کدی که عدد HTS را مستقیم در Havayar ذخیره کند خراب است |
| D-41 | الگوریتم کد پروژه | متفاوت | `03` §6.3 — `IProjectService.GetProjectCode`؛ تبدیل از پروپوزال هم همین را صدا می‌زند | `06` §6.1 — `{SP\|VP\|TP}/{prefix}/{سال}/{counter}`؛ `switch` واحد سازمانی **غیرقابل‌وصول**؛ `"01"+1` → `"011"` | |
| D-42 | وضعیت پروژه | متفاوت | `03` §12 — lookup نوع 2 (توضیح کد: 3/4/5/7) | `06` §2.2 — enum 0–7؛ `ProjectJob.MapProjectStatus` از 3,4,5,6,7,2432,2748,2773 | مقادیر ذخیره‌شده 0–7 هستند نه idهای lookup |
| D-43 | نوع/دیسیپلین/سایز VPIS | متفاوت | `04` §15 — جداول `Edms_DocType` / `DocDisipline` / `PageSize` | `06` §2.4–2.7 — enum؛ جاب `htsValue-1` (صفحه: نگاشت جدا 1→A1 … 6→A6) | |
| D-44 | `ProjectAttachmentTypeEnum`: `Contract` و `Document` هر دو **262** | متفاوت | `04` lookup نوع 65 | `06` §2.8 | مقدار تکراری در سیستم جدید |
| D-45 | `HoldCause` lookup 89 در برابر `DocumentLegalHolderEnum` | متفاوت | `04` §3 / §10 | `06` §2.9 — جاب `holdCauseFk - 409` | |
| D-46 | `DocPoi` در برابر `GoalOfProduction` | متفاوت | `04` §9 / §15 | `06` §2.10 — `docPoiFk - 1` | |
| D-47 | درصد پیشرفت: CAST وضعیت HTS | متفاوت | `04` §14 — `Document_Status_FK` = همان enum HTS | `06` §7 — `ProjectJob.MapDocumentStatus` اگر `Enum.IsDefined(..., (int)statusFk)` همان عدد را می‌ریزد | با `DocumentJob` که **remap** می‌کند تناقض دارد: HTS `5` روی مدرک → Havayar `Issue=2`؛ روی پیشرفت اگر 5 تعریف شده باشد → `CommentedByReviewer=5` |
| D-48 | `UnHoldByEmployer` | کمبود | `04` §2 — id 14 | `06` §2.1: عضو جدا نیست؛ جاب 14 را به `UnHold` می‌برد | |
| D-49 | `ApprovedAsNote` داخلی (HTS 3) | متفاوت | `04` §2 — id 3 | `06` جاب: `CommentedByApprover=8`؛ اعضای جدا `CommentedInternal=34` / `ApprovalInternal=37` در جاب 1..29 نیستند | |

### D-40 — نگاشت `DocumentJob.MapDocumentStatus` (HTS FK → Havayar)

منبع: `App.BackgroundJob/Jobs/Edms/DocumentJob.cs` (`06` §7.1). HTS 2 از روی هویت نویسنده کامنت به Approve reviewer/approver می‌رود.

| HTS `EdmsDocumentStatus` | عدد HTS | Havayar `DocumentStatusEnums` | عدد Havayar |
|---|---|---|---|
| Reject | 1 | `RejectByDcc` | 10 |
| Approve | 2 | `ApproveByReviewer` **یا** `ApproveByApprover` | 4 یا 7 |
| ApprovedAsNote | 3 | `CommentedByApprover` | 8 |
| Commented | 4 | `CommentedByDcc` اگر `IsForDcc`، وگرنه `CommentedByReviewer` | 11 یا 5 |
| **Issue** | **5** | **`Issue`** | **2** |
| ApprovedByDcc | 6 | `ApprovedByDcc` | 9 |
| RejectByEmployer | 7 | `RejectByClient` | 12 |
| ApproveByEmployer | 8 | `ApproveByClient` | 13 |
| ApprovedAsNoteByEmployer | 9 | `ApprovedAsNoteByClient` | 14 |
| CommentedByEmployer | 10 | `CommentedByClient` | 15 |
| Hold | 11 | `Hold` | 16 |
| UnHold | 12 | `UnHold` | 17 |
| NotReview | 13 | `NotReview` | 18 |
| UnHoldByEmployer | 14 | `UnHold` (ادغام) | 17 |
| SendEmailToClient | 15 | `SendEmailToClient` | 20 |
| ReIssued | 16 | `ReIssued` | 21 |
| Commited … ConvertToGeneralPackage | 17–29 | اعضای 22–33 به‌ترتیب جاب | 22–33 |
| *(خالی 28 در HTS)* | 28 | جاب → `UnHold` | 17 |

Havayar `NotIssue=1` معادل HTS ندارد (در HTS 1 = Reject). مدارک جدید Havayar با `NotIssue` ساخته می‌شوند (`ProjectVpis.Add`) بعد `Document.Add` آن‌ها را `Issue` می‌کند.

---

## ۴. دسترسی، Action، جاب

| شناسه | موضوع | طبقه‌بندی | شواهد HTS | شواهد Havayar | توضیح |
|---|---|---|---|---|---|
| D-50 | ماتریس SystemPage + PermissionType (FullAccess, ShowAll, ProjectManagerAccess, TenderDocType_*, Confidential, ForecastExecutionTime, …) | کمبود | `03` §3 | `06` §10 — Role UI روی `[ActionDisplayName]`؛ DCC یک **نقش نام‌دار** است | کارتابل/آرشیو بدون این فیلترها همه ردیف را می‌بینند اگر صفحه ساخته شود |
| D-51 | سید منو / RoleAccess | کمبود | `_EpmsMenu` Edms block | `06` §8: seed نیست؛ DB بررسی نشد | |
| D-52 | Helper و `AddNewComment` بدون ActionDisplayName | متفاوت | Load کامنت با SystemPage 168 | `06` §4.5 helpers | خارج از کاتالوگ نقش |
| D-53 | `Document.Edit` = Custom در برابر `EditAsGroup` = Update | متفاوت | یک صفحه Upload | `06` §4.5 / §10 | دو سطح ویرایش؛ New به EditAsGroup می‌رود |
| D-54 | جاب همگام Project / VPIS / Document | اضافه در سیستم جدید | HTS منبع است | `06` §7 | برای کات‌اوور لازم است؛ خطرش remap وضعیت است (`D-40`/`D-47`) |
| D-55 | جاب ترانسمیتال / فعالیت / ExtraInfo | کمبود | جداول HTS زنده یا مرده-منو | `06` §7: «No job for Transmital, ProjectActivity» | |
| D-56 | `ProjectAction.CheckCode` | اضافه در سیستم جدید | تولید کد در سرویس HTS هنگام Add | `06` §6.1 | رفتار **متفاوت** با HTS (`D-41`)؛ وجود Action اضافه است |
| D-57 | `DocumentCommentAction.ChangeDocumentStatus` | اضافه در سیستم جدید | دنرمال LastStatus احتمالاً تریگر SQL | `06` §6.2 | جایگزین تریگر؛ ایمیل ندارد (`D-30`) |

---

## ۵. اضافه در سیستم جدید (صفحه/مدل)

| شناسه | موضوع | طبقه‌بندی | شواهد | یادداشت |
|---|---|---|---|---|
| D-58 | `ListDocumentProduct` | اضافه در سیستم جدید | منوی زنده EDMS این برگ را ندارد | `06` §4.5 — کوئری Bom/Inv؛ خارج از محدوده BOM ولی صفحه Edms است |
| D-59 | `ProjectAttachmentList` جدا | اضافه در سیستم جدید | HTS پیوست تو در تو 218 + آرشیو 279 | `06` §4.1 — List با `unicode=ProjectAttachment` |
| D-60 | اعضای اضافی وضعیت (`NotIssue`, `ReviewIssuer`, `CommentedInternal`, …) | اضافه در سیستم جدید | `04` §2 پرش 28؛ بدون NotIssue | `06` §2.1 | |
| D-61 | `EditAsGroup` / چند ریویژن در یک صفحه | اضافه در سیستم جدید | HTS ریویژن = ردیف جدید Upload | `06` §3.5 / §4.5 | ایده نزدیک است؛ UI فرق دارد |

`D-54`/`D-56`/`D-57` هم «اضافه»اند؛ برای جلوگیری از دوبل‌شماری در جدول بالا تکرار نشدند و در §۴ آمده‌اند. در جمع کل هر شناسه یک‌بار است.

---

## ۶. عمداً حذف در منوی HTS

| شناسه | موضوع | طبقه‌بندی | شواهد | یادداشت |
|---|---|---|---|---|
| D-62 | اطلاعات تکمیلی اسناد (`Edms_Document_ExtraInfo`) | عمداً حذف در HTS | `03` §8 — href کامنت؛ کنترلر زنده | موجودیت در `04` §11؛ در Havayar نیست — اگر محصول هنوز URL مستقیم بزند، جدا تصمیم بگیرید |
| D-63 | گزارش برنامه زمانی | عمداً حذف در HTS | `03` §8 — صفحه 306 | هنوز گیت ShowAll حجم کاری HTS | 

---

## ۷. اگر بعداً اصلاح شود

پس از تیک شما: اولویت پیشنهادی کارتابل، یکسان‌سازی اعداد وضعیت (و یک‌دست کردن جاب پیشرفت با `DocumentJob`)، DCC/Final DCC، اقلام پروژه، و آرشیوها است. در این مرحله پیاده‌سازی نمی‌شود.
