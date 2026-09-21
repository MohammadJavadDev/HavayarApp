# 04 — پیشینه درخواست‌ها

HTS Page **77** `OpenOrderRequest_History` ، مسیر `supplayers-openRequestsHistory`.

---

## فرآیند HTS

آرشیو همان `Sup_OpenOrderRequest` با **`IsDeleted = true`**.

منبع حذف نرم:
- جاب/Import وقتی مقدار فاکتور با مقدار درخواست برابر شد (رسید کامل انبار) + `Sup_Compute_OpenOrderRequest` برای وضعیت‌های بسته Rahkaran
- خاتمه دستی Manage (`IsForceDeletedByUser`) هم در این گرید دیده می‌شود

صفحه **خواندنی** است (بدون Stop/تایید/خاتمه). فقط **پیوست** هنوز CRUD دارد (صفحه 71).

گرید ستون‌های بیشتر از Manage دارد: LeadTimeInDay، PartCodingTitle، RelatedPart، تاریخ اسناد تحویل/موقت/دائم، BuyProgress، …

تب جزئیات: کامنت‌ها + متن SuppComment.  
اکسل با `ExportAllPages`. اندازه صفحه تا ۱۰٬۰۰۰.

### دسترسی PageAction
بدون دسترسی، کامل، مشاهده، اکسل، پیوست مدارک.

`GetOpenOrderRequestHistoryGridData` با `UserId` فیلتر part-permission می‌گیرد (برخلاف Config).

نکته: Load این صفحه `ViewBag.isSupply` را ست نمی‌کند؛ ستون ManCompanyName عملاً نمایش داده نمی‌شود.

---

## شمارش
HTS حذف‌شده ≈ 124 532 / Havayar حذف‌شده ≈ 124 226 — همان جمعیت با اختلاف همگام‌سازی.

---

## HavayarApp

| قابلیت | وضعیت |
|---|---|
| صفحه/منوی جدا | **نیست** |
| فیلتر `IsDeleted=true` به‌صورت DataProfile | راهنما `OOR_History_Deleted` — **seed نشده** |
| نزدیک‌ترین پروفایل | `vw_openOrderRequestPurchaseCompleted` (خرید تکمیل‌شده، معادل کامل History نیست) |
| دیدن ردیف حذف‌شده از List پیش‌فرض | بستگی به SavedQuery لیست دارد؛ کنترلر FetchData فیلتر اجباری `IsDeleted=0` ندارد |
| پیوست روی آرشیو | از Edit همان موجودیت اگر کاربر ردیف حذف‌شده را باز کند |
| اکسل آرشیو اختصاصی | ندارد |

History در HTS یک **صفحه عملیاتی جدا با گرید آرشیو** است؛ در جدید فقط فلگ + تاریخچه موجودیت (`FormActionButtons.history`) روی Edit.
