# 09 — ماتریس صفحه به صفحه (HTS منوی زنده → Havayar)

> هر برگ **زنده** منوی HTS یک ردیف است. مسیر Havayar فقط در صورتی نوشته شده که کنترلر/ویو در `05`/`06` وجود داشته باشد — **صفحه‌ای اختراع نشده.**  
> منوی Panel در دیتابیس بررسی نشده؛ مسیرها شکل route کد هستند (`/panel/{module}/{controller}/...`).  
> وضعیت: **موجود** = صفحه اختصاصی هم‌نقش؛ **ناقص** = فقط List/Edit عمومی یا بخشی از گردش (مثلاً کارتابل نیست)؛ **کمبود** = بدون معادل.  
> منوی مرده در جدول جداست و کمبود Havayar حساب نمی‌شود.

تاریخ: **2026-09-15**. اصلاح کد در انتظار بررسی `07`/`08`.

---

## 1. مهندسی پروپوزال (۱۵ برگ زنده)

| HTS href | `SystemPage` | Id | عنوان فارسی | مسیر Havayar | وضعیت |
|---|---|---|---|---|---|
| `epms-projectActivity` | `Epms_Project_Activity` | 294 | ثبت فعالیت ها - پروپوزال | `/panel/epms/proposalactivity/list` | ناقص |
| `epms-personalReferralDocuments` | `Epms_PersonalReferralDocuments` | 350 | کارتابل شخصی - پروپوزال | — | کمبود |
| `epms-proposalManagement` | `Epms_Proposal` | 252 | مدیریت مناقصه \| پروپوزال | `/panel/epms/proposal/list` (+ `/edit`) | ناقص |
| `epms-proposalInquiryPartPrice` | `Epms_ProposalInquiryPartPrice` | 265 | استعلام قیمت تجهیزات پروپوزال | — | کمبود |
| `epms-proposalVpisManagement` | `Epms_Proposal_Vpis` | 254 | مدیریت Vpis - پروپوزال | `/panel/epms/proposalvpis/list` | ناقص |
| `epms-documentUpload` | `Epms_Document` | 255 | بارگذاری مدارک فنی | تب «مدارک» روی `/panel/epms/proposal/edit` (صفحه Upload جدا نیست) | ناقص |
| `epms-controlDocuments` | `Epms_ControlDocuments` | 257 | بررسی/تایید/رد اسناد - پروپوزال | — | کمبود |
| `epms-commitedDocuments` | `Epms_CommitedDocuments` | 260 | مدارک ارسال شده به فروش | — | کمبود |
| `epms-financialVpisManagement` | `Epms_FinancialVpisManagement` | 272 | مدیریت برآورد هزینه | — | کمبود |
| `epms-financialDocument` | `Epms_FinancialDocument` | 273 | بارگذاری مدارک مالی | — | کمبود |
| `epms-metreEquipmentFinancial` | `Epms_MetreEquipmentFinancial` | 261 | برآورد هزینه تجهیزات | `/panel/epms/equipmentpriceestimate/list` | ناقص |
| `epms-commitedMetreEquipmentFinancial` | `Epms_CommitedMetreEquipmentFinancial` | 267 | برآورد هزینه ارسال شده به فروش | — | کمبود |
| `epms-personalDocumentArchive` | `Epms_Personal_Document_Archive` | 289 | آرشیو شخصی - پروپوزال | — | کمبود |
| `epms-allDocumentArchive` | `Epms_All_Document_Archive` | 264 | آرشیو کلی - پروپوزال | — | کمبود |
| `epms-epmsPersonnelPerformanceReport` | `EpmsPersonnelPerformanceReport` | 351 | گزارش عملکرد پرسنل | — | کمبود |

جمع زنده EPMS: **۱۵** — موجود **۰** · ناقص **۵** · کمبود **۱۰**.

نکته: `VpisType` در HTS فقط کمبو است نه برگ منو. صفحه `/panel/epms/vpistype/list` در جدول اضافه (§3) آمده.

---

## 2. مهندسی پروژه (۲۴ برگ عملیاتی + ۲ راهنما)

| # | HTS href | `SystemPage` | Id | عنوان فارسی | مسیر Havayar | وضعیت |
|---|---|---|---|---|---|---|
| 1 | `edms-projectActivity` | `Edms_Project_Activity` | 288 | ثبت فعالیت ها | `/panel/edms/projectactivity/list` | ناقص |
| 2 | `edms-personalReferralDocuments` | `Edms_PersonalReferralDocuments` | 275 | کارتابل شخصی | — | کمبود |
| 3 | `edms-projectManagement` | `Edms_Project` | 163 | شناسنامه پروژه ها | `/panel/edms/project/list` (+ `/edit`) | ناقص |
| 4 | `edms-ProjectParts` | `Edms_ProjectParts` | 558 | اقلام پروژه | — | کمبود |
| 5 | `edms-projectVpisManagement` | `Edms_Project_Vpis` | 165 | مدیریت Vpis | `/panel/edms/projectvpis/list` | ناقص |
| 6 | `edms-documentUpload` | `Edms_Document` | 166 | بارگذاری اسناد | `/panel/edms/document/list` (+ `editasgroup`) | ناقص |
| 7 | `edms-controlDocuments` | `Edms_ControlDocuments` | 223 | بررسی/تایید/رد اسناد | کامنت روی Edit مدرک — صفحه کارتابل کنترل نیست | ناقص |
| 8 | `edms-documentControlCenter` | `Edms_Document_Control_Center` | 221 | کنترل اسناد (DCC) | نقش `Edms.Documents.DccUsers` روی فیلتر کامنت — صفحه DCC نیست | ناقص |
| 9 | `edms-transmitalManage` | `Edms_Transmital_Manage` | 169 | ترانسمیتال | `/panel/edms/transmital/list` | ناقص |
| 10 | `edms-finalDocumentControlCenter` | `Edms_Document_Control_Center` | 221 | دریافت و ارسال اسناد | — | کمبود |
| 11 | `edms-mdrReport` | `Edms_Mdr_Report` | 235 | گزارش MDR | `/panel/edms/document/mdrreport` | موجود |
| 12 | `edms-cumulativePerformanceReport` | `Edms_CumulativePerformanceReport` | 291 | گزارش تجمعی عملکرد/فعالیت | — | کمبود |
| 13 | `edms-personnelWorkLoadReport` | `Edms_PersonnelWorkLoadReport` | 361 | گزارش حجم کاری پرسنل | `/panel/edms/document/personnelworkloadreport` | موجود |
| 14 | `edms-personnelCumulativeWorkLoadReport` | `Edms_PersonnelWorkLoadReport` | 361 | گزارش تجمعی حجم کاری پرسنل | — | کمبود |
| 15 | `edms-personnelPerformanceReport` | `Edms_PersonnelPerformance_Report` | 245 | گزارش عملکرد پرسنل | — | کمبود |
| 16 | `edms-documentDelayReport` | `Edms_DocumentDelayReport` | 238 | گزارش تاخیرات مدارک | — | کمبود |
| 17 | `edms-documentHoldReport` | `Edms_DocumentHoldReport` | 248 | گزارش توقفات مدارک | — | کمبود |
| 18 | `edms-projectProgressReport` | `Edms_ProjectProgress_Report` | 246 | گزارش پیشرفت پروژه | — | کمبود |
| 19 | `edms-projectSummerizedReport` | `Edms_ProjectSummerized_Report` | 282 | گزارش تجمعی پروژه | — | کمبود |
| 20 | `edms-indexEvaluationReport` | `Edms_IndexEvaluationReport` | 380 | گزارش شاخص ارزیابی | — | کمبود |
| 21 | `edms-clientAndVendorDocumentReport` | `EdmsClientAndVendorDocumentReport` | 532 | گزارش اسناد کارفرما / وندور | — | کمبود |
| 22 | `edms-personalDocumentArchive` | `Edms_Personal_Document_Archive` | 233 | آرشیو شخصی | — | کمبود |
| 23 | `edms-allDocumentArchive` | `Edms_All_Document_Archive` | 232 | آرشیو کلی | — | کمبود |
| 24 | `edms-projectAttachmentsArchive` | `Edms_Project_Attachment_Archive` | 279 | آرشیو اسناد ورودی پروژه | — | کمبود |
| 25 | `Home.LoadFile` (Fa) | — | — | نسخه فارسی - Fa | — | کمبود |
| 26 | `Home.LoadFile` (En) | — | — | نسخه لاتین - En | — | کمبود |

جمع زنده EDMS: **۲۶** — موجود **۲** · ناقص **۷** · کمبود **۱۷**.

صفحه تو در تو HTS که برگ منو نیستند (در ماتریس زنده نمی‌آیند؛ در `08` آمده‌اند): `Edms_Project_Part` 164، `Edms_Document_Comment` 168، `Edms_Project_Attachment` 218، `Edms_Transmital_Attachment` 220. معادل جزئی: تب پیوست روی Edit پروژه؛ کامنت روی Edit مدرک.

---

## 3. صفحات اضافه در Havayar (برگ منوی زنده HTS نیستند)

| مسیر کد | `[ControllerInfo]` / DisplayName | یادداشت |
|---|---|---|
| `/panel/epms/vpistype/list` | اسناد VPIS | در HTS فقط جدول `Epms_VpisType` برای کمبو است |
| `/panel/edms/project/projectattachmentlist` | لیست اطلاعات پیوست پروژه | نزدیک به تب تو در تو 218؛ معادل آرشیو 279 نیست |
| `/panel/edms/document/documentproducts/listdocumentproduct` | مدارک محصولات | کوئری Bom/Inv؛ منوی EDMS زنده HTS این برگ را ندارد |
| `Document/DocumentProducts/List.cshtml` | — | اکشن کامنت شده؛ orphan |

---

## 4. منوی مرده HTS (کمبود Havayar نیست)

از `_EpmsMenu.cshtml` کامنت‌شده. کنترلر در HTS هنوز هست.

### EPMS

| Href | `SystemPage` | Id | عنوان (fa-IR) |
|---|---|---|---|
| `epms-tenderManagement` | `Epms_Tender` | 249 | مدیریت مناقصات (`TendersManagement`) — کامنت `@*حذف شود*@` |
| `epms-allMetreEquipmentFinancialArchive` | `Epms_All_MetreEquipmentFinancial_Archive` | 268 | آرشیو کلی برآورد هزینه تجهیزات |
| `epms-allFinancialDocumentArchive` | `Epms_All_FinancialDocument_Archive` | 274 | آرشیو کلی مدارک مالی |
| `epms-allTenderAttachmentsArchive` | `Epms_AllTenderAttachmentsArchive` | 325 | آرشیو اسناد مناقصات |
| `edms-cumulativePerformanceReport` | `Edms_CumulativePerformanceReport` | 291 | داخل گروه گزارش EPMS کامنت شده؛ صفحه **EDMS** است |

`Epms_Tender` همچنان FK پروپوزال زنده است (`07` ردیف فیلد، نه این جدول).

### EDMS

| Href | `SystemPage` | Id | عنوان (fa-IR) |
|---|---|---|---|
| `edms-documentExtraInfo` | `Edms_Document_ExtrInfo` | 543 | اطلاعات تکمیلی اسناد مهندسی |
| `edms-timeScheduleReport` | `Edms_TimeScheduleReport` | 306 | گزارش برنامه زمانی — در HTS، `ShowAll` گزارش حجم کاری هنوز روی **همین** صفحه چک می‌شود |
