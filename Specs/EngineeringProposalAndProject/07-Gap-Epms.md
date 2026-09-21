# 07 — مغایرت مهندسی پروپوزال (HTS EPMS ↔ Havayar)

> چک‌لیست برای **بررسی شما**. تا وقتی نگویید کدام ردیف باید در سیستم جدید اصلاح شود، **کدی زده نمی‌شود.**
>
> تاریخ: **2026-09-15**. شواهد فقط از `01`/`02` (HTS) و `05` (Havayar). منوی مرده HTS کمبود Havayar نیست مگر فیلد هنوز در فرآیند زنده باشد.
>
> طبقه‌بندی: **کمبود** = در Havayar نیست · **ناقص** = هست ولی کارتابل/گردش/فیلتر HTS را ندارد · **متفاوت** = هست با رفتار یا عدد دیگر · **اضافه در سیستم جدید** = فقط Havayar · **عمداً حذف در HTS** = کامنت منو؛ پورت پیش‌فرض لازم نیست.

ماتریس صفحه: [`09-Page-Matrix.md`](09-Page-Matrix.md).

---

## خلاصه شمارش

| طبقه‌بندی | تعداد ردیف |
|---|---|
| کمبود | 36 |
| ناقص | 11 |
| متفاوت | 8 |
| اضافه در سیستم جدید | 3 |
| عمداً حذف در HTS | 5 |
| **جمع** | **63** |

---

## ۱. صفحات

| شناسه | موضوع | طبقه‌بندی | شواهد HTS | شواهد Havayar | توضیح |
|---|---|---|---|---|---|
| E-01 | کارتابل شخصی پروپوزال | کمبود | `01` §7.2 — `EpmsPersonalReferralDocumentsController`؛ صف VPIS بدون مدرک + آخرین ریویژن Reject/Commented/Sale | `05` §5: هیچ view کارتابل زیر `Views/Panel/Epms` | فقط List عمومی موجودیت‌ها؛ کوئری اتحادی کارتابل نیست |
| E-02 | استعلام قیمت تجهیزات | کمبود | `01` §7.4 — `EpmsProposalInquiryPartPriceController`؛ `02` §10–11 | `05`: موجودیت/کنترلر Inquiry نیست | صفحه و جدول `Epms_Proposal_Inquiry_PartPrice` (+ پیوست) غایب |
| E-03 | بررسی/تایید/رد اسناد فنی | کمبود | `01` §7.7 — `EpmsControlDocumentsController` + `CommitOrUnCommit` + `ConvertToGeneralPackage` | `05` §3.5: `ProposalDocumentComment` روی موجودیت هست؛ در `_ProposalDocumentPartial` **رندر نشده** | حتی UI کامنت کنترل نیست؛ صفحه کارتابل کنترل جدا هم نیست |
| E-04 | مدارک ارسال‌شده به فروش | کمبود | `01` §7.8 — `EpmsCommitedDocumetsController`؛ فیلتر `IsCommited` + وضعیت فروش | `05`: اکشن/ویو committed docs نیست | کارتابل فروش روی مدرک committed |
| E-05 | مدیریت برآورد هزینه (VPIS مالی) | کمبود | `01` §7.9 — `Epms_Financial_Vpis`؛ lookup نوع 70 | `05` §3.9: یک `EquipmentPriceEstimate` به‌جای چهار صفحه مالی | موجودیت VPIS مالی جدا نیست |
| E-06 | بارگذاری مدارک مالی | کمبود | `01` §7.10 — `Epms_Financial_Document` (فایل varbinary) | `05`: صفحه/موجودیت جدا نیست | شرط کمبوی متره HTS: وجود فایل مالی |
| E-07 | برآورد ارسال‌شده به فروش | کمبود | `01` §7.12 — `EpmsCommitedMetreEquipmentFinancialController` | `05`: اکشن Commit و صفحه فروش متره نیست | |
| E-08 | آرشیو شخصی پروپوزال | کمبود | `01` §7.13 — فیلتر با `Epms_Document` FullAccess/ShowAll | `05` §5 | آرشیو فیلترشده همان مدارک است نه موجودیت جدا |
| E-09 | آرشیو کلی پروپوزال | کمبود | `01` §7.14 — ShowAll یا گروه **200** + sale ids | `05` §5 | |
| E-10 | گزارش عملکرد پرسنل EPMS | کمبود | `01` §7.15 — SQL `GetEpmsPersonelPerformance`؛ checker hours = 20٪ | `05`: کنترلر گزارش Epms نیست | منوی HTS با صفحه 351؛ authorize کنترلر با **245** (باگ داخلی HTS) |
| E-11 | بارگذاری مدارک فنی | ناقص | `01` §7.6 — صفحه Upload روی `Epms_Document` + دیسک + Issue=5 | `05` §3.4 / §4.1: تب مدارک روی `Proposal/Edit` | مدرک به **پروپوزال** وصل است نه VPIS؛ بدون صف Upload و بدون انواع فایل چندگانه |
| E-12 | مدیریت پروپوزال | ناقص | `01` §7.3 — CRUD + پیوست + تجهیز + لاگ + SendToMetre + Convert + Revision | `05` §3.1 / `Proposal/Edit.cshtml` | فرم هست؛ اکشن‌های گردش و تب تجهیز و لاگ نیست (`E-16`…) |
| E-13 | VPIS فنی | ناقص | `01` §7.5 — پسوند `REV 0n`، اسپلیت گروه 557، `UpdatePermissions` 94/123 | `05` §3.8 / `ProposalVpisController` | CRUD استاندارد؛ بدون گروه کاربری و بدون محدود کردن کمبوی پروپوزال به «ارسال‌شده به متره» |
| E-14 | ثبت فعالیت‌ها - پروپوزال | ناقص | `01` §7.1 — جدول مشترک `Edms_Project_Activity` با `ModuleType_FK=387`؛ کامنت Issue سپس 2/4 | `05` §3.6 — جدول جدا `Epms.ProjectActivity`؛ List/Edit | بدون تمایز 387؛ بدون فیلتر ShowAll/سازنده؛ کامنت با enum Edms (`E-47`) |
| E-15 | برآورد هزینه تجهیزات | ناقص | `01` §7.11 — ردیف متره + قیمت + کامنت وضعیت + Commit + Excel | `05` §3.9 / `EquipmentPriceEstimateController` | یک CRUD با دو فایل استعلام/BOM و enum وضعیت 0/1/2/3/50؛ نه خط تجهیز و نه Commit |

---

## ۲. فرآیند و اکشن

| شناسه | موضوع | طبقه‌بندی | شواهد HTS | شواهد Havayar | توضیح |
|---|---|---|---|---|---|
| E-16 | `DoSendToMetreOperation` | کمبود | `01` §7.3 — `SetIsSendToMetre` + ایمیل گروه 535/557 + لاگ 2751؛ وضعیت lookup عوض نمی‌شود | `05` §3.1 / §4.1: فیلد `IsSendToMetre` هست؛ **اکشن نیست** | کمبوی VPIS فنی HTS فقط پروپوزال‌های ارسال‌شده به متره را می‌گیرد |
| E-17 | `DoConvertToProjectOperation` | کمبود | `01` §7.3 / `02` §25 — ساخت `Edms_Project` (`IsCreatedFromProposal`, `ProposalId`, `Main_Client_FK=1247`, وضعیت 3) + کپی پیوست نوع 258/257 | `05` §7: «No convert-to-`Edms.Project` method»؛ `06` `Project` بدون `ProposalId` | بدون این اکشن و بدون FK روی پروژه، پل EPMS→EDMS قطع است |
| E-18 | `DoNewRevisionOperation` | کمبود | `01` §7.3 — ردیف جدید، `Revision++`، وضعیت `CreateProposal` 2749، ایمیل | `05`: فیلد `Revision` روی فرم disabled است؛ کلون ردیف نیست | |
| E-19 | `CommitOrUnCommit` مدارک فنی | کمبود | `01` §7.7 — فقط با `ApprovedDate`؛ `IsCommited`؛ کامنت 17/18؛ ایمیل «بررسی کارتابل فنی_مهندسی پروپوزال»؛ لاگ `SendToSale` 2779 | `05`: `PermissionType.CommitOrUnCommit` و اکشن Commit نیست | |
| E-20 | `ConvertToGeneralPackage` + تایید دوم | کمبود | `01` §7.7 — `DoSecondConfirmOperation`؛ `HasGeneralPackage`؛ کامنت نوشته‌شده `Commented` نه enum 29 | `05`: `HasGeneralPackage` روی موجودیت؛ روی فرم bind نشده؛ اکشن نیست | |
| E-21 | `CommitOrUnCommit` متره | کمبود | `01` §7.11–7.12 — ایمیل فروش روی سرویس متره | `05` §3.9: «No Commit-specific action» | `EquipmentPriceEstimateStatusEnum.SendToSell=1` فقط مقدار enum است |
| E-22 | ایمیل ارسال به متره / ریویژن جدید | کمبود | `01` §9 — `SendToMetreEmail` / `SendCreateNewRevisionEmail` | `05`: سرویس/اکشن ایمیل Epms نیست | گیرندگان: گروه 535 یا 557 + کاربر جاری + سرپرست |
| E-23 | نوتیف درون‌برنامه‌ای + SignalR | کمبود | `01` §10 — `Gnr_Notification` + `OnlineUsersHub` برای کامنت مدرک و تغییر وضعیت پروپوزال (`Tender_NotificationReciversUserIds`) | `05`: معادل ذکر نشده | |
| E-24 | `Epms_Proposal_log` | کمبود | `01` §11 / `02` §9 — رخدادهای 2749–2779 | `05`: موجودیت لاگ نیست | مسیر حسابرسی پروپوزال |
| E-25 | عضویت گروه 94 و 123 هنگام ذخیره VPIS | کمبود | `01` §6 و §7.5 — `UserGroup.EpmsFileUploaders` / `EpmsDocumentUpload` | `05` §4.3: فقط CRUD | بدون این، ACL بارگذاری/فروش HTS روی کاربر مسئول اصلی سوار نمی‌شود |
| E-26 | ورود اکسل متره | کمبود | `01` §7.11 — `DoOperationByExcel(proposalFk)` | `05` §4.5: CRUD تکی | |
| E-27 | ACL گرید پروپوزال | کمبود | `01` §7.3 — ShowAll / سرپرست واحدهای {140,30,745} / فروش صنعتی {225,278} / ProductEngineering / SalesExpertIds | `05` §4.1: `FetchData` استاندارد DataTable | `GetProposalCode` همان aliasهای 140/745/142/278 را دارد؛ فیلتر ردیف ندارد |
| E-28 | بلاک مهندسی محصول روی CRUD / متره / تبدیل | کمبود | `01` §5 — `ProductEngineering=194` | `05`: نقش معادل و گیت سرور نیست | پرچم `IsForProductEngineeringDepartment` روی add از عنوان واحد «فروش صنعتی» در HTS ست می‌شود |

---

## ۳. فیلد و مدل داده

| شناسه | موضوع | طبقه‌بندی | شواهد HTS | شواهد Havayar | توضیح |
|---|---|---|---|---|---|
| E-29 | `Tender_FK` + فیلدهای کارفرما | کمبود | `02` §4 و §12 — کارفرما/تلفن/ایمیل/آدرس روی `Epms_Tender`؛ در UI پروپوزال از join | `05` §3.1: فقط `TenderName` + تاریخ مناقصه | صفحه مناقصه مرده است (`E-59`) ولی FK زنده است |
| E-30 | `Action_Priority` | کمبود | `02` §4 — `byte?`؛ caption الویت اقدام | `05` §3.1: پراپرتی نیست | منبع کمبو در `FillData` HTS هم خالی بود |
| E-31 | `ProposalSecondStatus` | کمبود | `02` §4 | `05` §3.1 | |
| E-32 | موجودیت استعلام + پیوست | کمبود | `02` §10–11 | `05` ماژول map | همان E-02 در لایه داده |
| E-33 | چهار موجودیت مالی HTS | کمبود | `02` §17–21 — Financial_Vpis / Financial_Document / Metre + Comment + Price | `05` §3.9 | قیمت روی `Metre_Price`/`Vw_*`؛ در Havayar دو `FileEntity` |
| E-34 | اتصال مدرک فنی به VPIS | متفاوت | `02` §14 — `Proposal_Vpis_FK` | `05` §3.4 — `ProposalId` روی `ProposalDocument` | مدل HTS مدرک را زیر VPIS می‌گذارد؛ Havayar زیر پروپوزال |
| E-35 | آماده/بررسی/تأیید/تأیید دوم / Due / `IsCommited` روی مدرک فنی | کمبود | `02` §14 | `05` §3.4 — Attachment, Revision, ConsumedManHours, LastCommentStatus, Comment | بدون این‌ها کنترل و Commit معنی نمی‌دهد |
| E-36 | فایل Native / Secondary / DeviationList | کمبود | `02` §14؛ `01` §7.6 `DocumentFileType` | `05` §3.4: یک `FileEntity` | |
| E-37 | `SecondApprovedUser_FK` روی VPIS | کمبود | `02` §7 | `05` §3.8 | |
| E-38 | `HtsId` روی جداول Epms | کمبود | — (منبع TotalSystem) | `05` §1: هیچ `HtsId`؛ جاب Epms نیست | برای کات‌اوور بعدی شناسایی ردیف HTS نیست |
| E-39 | `IsSendToMetre` روی فرم | ناقص | `02` §4 | `05` §3.1: «Edit does **not** bind `IsSendToMetre`» | فیلد دیتابیس بدون UI و بدون اکشن |
| E-40 | `IsForProductEngineeringDepartment` روی فرم | ناقص | `02` §4؛ کپی به VPIS در `01` §7.5 | `05` §3.1 و §3.8: روی VPIS هم نیست | |
| E-41 | `HasGeneralPackage` روی فرم | ناقص | `02` §4 | `05` §3.1 | |
| E-42 | تب تجهیزات روی Edit | ناقص | `01` §7.3 — گرید اقلام؛ replace کامل هنگام Update | `05` §3.1: موجودیت + `_ProposalEquipmentPartial`؛ **از تب‌های Edit صدا زده نمی‌شود** | `Edit` حتی `ProposalEquipments` را Include نمی‌کند |
| E-43 | `IsSendToProjectAttachments` روی پیوست | ناقص | `02` §5 — کپی به پروژه نوع 258 | `05` §3.3: پراپرتی هست؛ در partial bind نشده | |
| E-44 | UI کامنت مدرک پروپوزال | ناقص | `02` §15 | `05` §3.5: «Nested comments are **not** rendered» | |
| E-45 | `ProposalStatusEnum` در برابر lookup نوع 2 | متفاوت | `02` §2.1 — FK از `Gnr_Lookup` نوع 2 (3–7)؛ 2749+ فقط روی **لاگ** | `05` §2.1 — همان 3–7 به‌علاوه Loss 2748 / Win 2773 / SendToMetre 2774 / WaitingSeller 2775 روی **خود Status** | عملیات و وضعیت در Havayar قاطی شده‌اند |
| E-46 | وضعیت مدرک فنی | متفاوت | `02` §2.2 — `EdmsDocumentStatus` (Issue=**5**) | `05` §2.3 — `ProposalDocumentStatusEnum` 0..5 (`Submit=0` … `Reject=5`) | اعداد با EDMS Havayar و با HTS یکی نیستند |
| E-47 | کامنت فعالیت = `DocumentStatusEnums` Edms | متفاوت | `02` §23 — وضعیت 2 و 4 (+ Issue هنگام ایجاد) | `05` §3.7 — `Entities.App.Edms.Enums.DocumentStatusEnums` روی فرم فعالیت | کمبو کامنت فعالیت مقادیر DCC/کارفرما/فروش را هم نشان می‌دهد |
| E-48 | مدیر/سرپرست فروش = `User` | متفاوت | `02` §4 — `HRM_Personel` | `05` §3.1 — `User?` | |
| E-49 | فعالیت: `ModuleType` 387 در برابر اسکیما جدا | متفاوت | `02` §22 — یک جدول با 386/387 | `05` §3.6 — `Epms.ProjectActivity` جدا از `Edms.ProjectActivity` | |
| E-50 | ذخیره فایل (دیسک / varbinary) | متفاوت | `01` §7.6 دیسک؛ مالی/متره varbinary | `05`: `FileEntity` | مهاجرت باینری جدا از مدل صفحه است |

---

## ۴. دسترسی و EntityAction

| شناسه | موضوع | طبقه‌بندی | شواهد HTS | شواهد Havayar | توضیح |
|---|---|---|---|---|---|
| E-51 | ماتریس `SystemPage` + `PermissionType` | کمبود | `01` §4–5 — FullAccess/ShowAll/CommitOrUnCommit/Checker/ViewAttachment/ProductEngineering/SuperVisor | `05` §4 — CRUD با `[ActionDisplayName]`؛ بدون معادل Commit/ShowAll/Checker | Helperهای Partial **بدون** ActionDisplayName و خارج از کاتالوگ نقش |
| E-52 | گروه‌ها و واحدهای هاردکد | کمبود | `01` §6 — 94, 123, 200, 535, 557؛ واحد 140/745/30/142/278/225/249/149 | `05`: نقش seed شده Epms نیست؛ فقط alias کد پروپوزال | |
| E-53 | سید منو / `RoleAccess` | کمبود | منوی `_EpmsMenu` | `05` §6: هیچ seed در `Data/Scripts`؛ منوی زنده DB **بررسی نشد** (MCP) | |
| E-54 | Partial بدون `[ActionDisplayName]` | متفاوت | هر Load صفحه `[PermissionAuthorize(SystemPage)]` دارد | `05` §4.1 — `ProposalEquipmentPartial` / Attachment / Document | برای کاربر authenticated در کاتالوگ نقش دیده نمی‌شوند |
| E-55 | هیچ `EntityAction` برای Epms | کمبود | منطق در کنترلر/سرویس HTS (متره، تبدیل، لاگ) | `05` §1 / §6: پوشه `WebApp/Actions/Epms` وجود ندارد | در Havayar الگوی «همیشه با Save» برای کد/تبدیل/متره وجود ندارد |

---

## ۵. اضافه در سیستم جدید

| شناسه | موضوع | طبقه‌بندی | شواهد HTS | شواهد Havayar | توضیح |
|---|---|---|---|---|---|
| E-56 | صفحه CRUD `VpisType` | اضافه در سیستم جدید | `02` §8 — جدول کمبو؛ برگ منو نیست | `05` §4.2 — `/panel/epms/vpistype/list` | Edit این صفحه toolbar قدیمی دارد (`05`) |
| E-57 | `EquipmentPriceEstimate` با فایل استعلام + BOM | اضافه در سیستم جدید | چهار موجودیت مالی جدا | `05` §3.9 | مدل جایگزین است نه پورت فیلدبه‌فیلد (`E-15`/`E-33`) |
| E-58 | `ProposalActivityStatusEnum` روی خود فعالیت | اضافه در سیستم جدید | وضعیت روی **کامنت** (`02` §23) | `05` §2.4 و §3.6 — `Status` روی والد | |

---

## ۶. عمداً حذف در منوی HTS

این‌ها کمبود Havayar نیستند. کنترلر HTS ممکن است با `partialType` قدیمی هنوز باز شود.

| شناسه | موضوع | طبقه‌بندی | شواهد | یادداشت |
|---|---|---|---|---|
| E-59 | مدیریت مناقصات | عمداً حذف در HTS | `01` §8.1 — `@*حذف شود*@`؛ `Epms_Tender=249` | FK پروپوزال زنده است → `E-29` |
| E-60 | آرشیو کلی برآورد متره | عمداً حذف در HTS | `01` §8.2 — صفحه 268 | |
| E-61 | آرشیو کلی مدارک مالی | عمداً حذف در HTS | `01` §8.3 — صفحه 274 | |
| E-62 | آرشیو اسناد مناقصات | عمداً حذف در HTS | `01` §8.4 — صفحه 325 | |
| E-63 | گزارش تجمعی داخل گروه گزارش EPMS | عمداً حذف در HTS | `01` §3 — href `edms-cumulativePerformanceReport` کامنت | صفحه EDMS است نه EPMS |

---

## ۷. اگر بعداً اصلاح شود

یک خط برای همه ردیف‌ها: **پس از تیک شما**، اولویت پیشنهادی همان کارتابل/ارسال به متره/تبدیل به پروژه/Commit است؛ جزئیات پیاده‌سازی در این فایل نیامده.
