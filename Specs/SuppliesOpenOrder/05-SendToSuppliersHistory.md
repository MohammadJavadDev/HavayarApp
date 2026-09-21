# 05 — پیشینه ارسال به پیمانکاران

HTS Page **545** `Sup_SendToSupplyersHistory` ، مسیر `supplayers-sendToSupplyersHistory`.  
جدول: `Sup_OpenOrderRequest_EmailToSupplier` (۳۷٬۹۳۹ ردیف زنده).

---

## فرآیند HTS

لاگ فقط‌خواندنی هر ایمیلی که از **صفحه تنظیمات** با `SendEmailToContractor` ثبت شده.

**هیچ جابی در این جدول نمی‌نویسد.** `WriteData.Import_AllOpenOrderRequest` ایمیل «رسید انبار / رد QC / تغییر درخواست خرید» می‌فرستد ولی ردیف EmailToSupplier نمی‌سازد.

### ستون‌های گرید
`#`، PurchaseRequestNumber، OrderNo، Part_Code/Name/Unit، CompanyName (نام‌های کالای دنرمال)، OrderCompanyName (گیرنده)، RequiredQty، SendingCount، SendDate، SendTime، BuyTrustee (کاربر ارسال‌کننده).

بدون نوار ابزار. Textarea توضیحات در property pane به ذخیره وصل نیست.

### دسترسی — باگ شناخته‌شده HTS
- منو با Page **545**
- `Load…` اتریبیوت **Page 77 (History)** دارد و اسم متد از History کپی شده
- `GetGridData` فقط `[PermissionAuthorize(Supp)]`

برای باز شدن صفحه عملاً هم آیتم منوی 545 و هم دسترسی History 77 لازم است (مگر FullAccess).

PageAction 545: کامل، مشاهده.

CRUD سرویس: Add فقط از Config؛ Update/Delete خالی.

---

## فیلدهای موجودیت

| فیلد | نوع |
|---|---|
| OpenOrderRequest_EmailToSupplier_ID | int PK |
| OpenOrderRequest_FK | int |
| Company_FK | int → ManCompany |
| CreatedUser_FK | short |
| SendDate / SendTime | string شمسی |
| Attachment_FK | int? پیوست درخواست |
| PartAttachmentId | int? |
| EdmsDocumentAttachmentId | int? |
| Comment | string |
| SendingCount | decimal? تعداد اعلام‌شده در مودال |

---

## HavayarApp

**وجود ندارد:** موجودیت، کنترلر، ویو، منو، جاب، DataProfile.

دکمه تامین‌کننده منتخب روی Edit جدید هم ارسال ایمیل نمی‌کند.

بدون این ماژول، تاریخچه ۳۷٬۹۳۹ ارسال HTS به سیستم جدید منتقل نشده و امکان ثبت ارسال جدید هم نیست.
