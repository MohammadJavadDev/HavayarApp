# 03 — تنظیمات درخواست‌های باز

HTS Page **76** `OpenOrderRequest_Config` ، مسیر `supplayers-openRequestsConfig`.  
جدول داده: همان `Sup_OpenOrderRequest` با `IsDeleted = 0` — **بدون** فیلتر نقش Manage.

---

## فرآیند HTS

1. نمایش **همه** درخواست‌های حذف‌نشده (کارتابل تنظیمات، نه کارتابل واحد).
2. تخصیص ذینفعان (`UserCategoryId = 2829` = سایر ذینفعان) و درخواست‌کنندگان مهندسی (`2830`).
   - نوشته در `Sup_OpenOrderRequest_Requested_Personel`
   - دنرمال: `Requested_Personel` / Email و `Requested_EngineeringPersonel` / Email روی والد
   - چک‌باکس نفرات ثابت صنایع + انتخاب واحد سازمانی برای افزودن پرسنل آن واحد
3. **ارسال ایمیل به پیمانکار** (تک‌ردیفی، فقط اگر `IsHasCompany`) → درج `Sup_OpenOrderRequest_EmailToSupplier`، `Changed=false`، `ManCompanyId`.
4. **افزودن تامین‌کننده منتخب** فله‌ای → فقط `ManCompanyId`.

گرید جزئیات هر ردیف: تاریخچه ارسال همان EmailToSupplier (CompanyName، SendingCount، SendDate/Time، Comment، BuyTrustee).

رنگ ردیف: سبز=ارسال‌شده، بنفش=Changed، قرمز=IsStop.

---

## فیلدهای درگیر

والد: Requested_* ، Changed، ManCompanyId.  
فرزند پرسنل: PK، OpenOrderRequest_FK، RequestedPersonel_FK، CreatedUser/Date/Time، UserCategoryId.  
فرزند ایمیل: PK، OpenOrderRequest_FK، Company_FK، CreatedUser_FK، SendDate/Time، Attachment_FK / PartAttachmentId / EdmsDocumentAttachmentId، Comment، SendingCount.

---

## اکشن کنترلر HTS

| اکشن | دسترسی |
|---|---|
| Load page | صفحه 76 |
| Grid data | فقط Supp کلاسی — همه `!IsDeleted` |
| EmailToSupplier child grid | کلاس |
| SendEmailToContractor | صفحه 76 + **SendEmail=27** |
| GetPersonnels | کلاس |
| AddComponyMansToRequests | کلاس |
| DoOperation (پرسنل) | صفحه 76 |

PageAction کاتالوگ 76: بدون دسترسی، کامل، مشاهده، ارسال ایمیل — **Edit در کاتالوگ نیست** ولی DoOperation اجرا می‌شود.

ستون `ManCompanyName` فقط اگر `ViewBag.isSupply` (Org 51/63).

---

## HavayarApp

| قابلیت HTS | جدید |
|---|---|
| صفحه/منوی جدا | **نیست** |
| نقش | `Sup.OpenOrderRequest.ConfigManage` (100011) |
| نفرات ثابت | نقش `ConfigManageStaticPersonel` (100012) به‌جای لیست هاردکد صنایع |
| تخصیص پرسنل | مودال `_AddRequestedPersonelPartial` از **Edit یک ردیف** (API می‌تواند چند Id بگیرد اما UI از Edit است نه گرید چندانتخابی تنظیمات) |
| جدول Requested_Personel | **نیست** — Ids/نام/ایمیل روی والد |
| ارسال ایمیل به پیمانکار | **نیست** (جدول و اکشن ندارد) |
| تامین‌کننده منتخب | دکمه `addSupplier` روی Edit **هندلر خالی است** |
| گرید همه فعال‌ها بدون فیلتر نقش | DataProfile `OOR_Config` در راهنما پیشنهاد شده؛ در `system.SavedQuery` **نیست** |

بدون صفحه تنظیمات، عملیات ۲ و ۴ عملاً از Edit تکی انجام می‌شود (۴ هم فعلاً کار نمی‌کند) و عملیات ۳ کلاً غایب است.
