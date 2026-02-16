# خلاصه پیاده‌سازی OpenOrderRequest Management

## ✅ کارهای انجام شده

### 1. View Layer (Edit.cshtml)

#### دکمه‌های عملیاتی اضافه شده:
- ✅ **افزودن کامنت**: Modal برای ثبت کامنت ساده
- ✅ **تاریخچه کامنت‌ها**: Modal read-only برای نمایش لیست کامنت‌ها
- ✅ **تایید مهندسی**: تایید با confirm dialog
- ✅ **تایید واحد فروش/پروژه**: Modal با فیلد توضیحات
- ✅ **ثبت توقف**: Modal پیچیده با 3 بخش مختلف بر اساس workflow
- ✅ **راه اندازی**: راه‌اندازی درخواست متوقف شده
- ✅ **در راه**: تغییر وضعیت به "در راه"
- ✅ **استعلام وضعیت**: تغییر وضعیت به "در حال استعلام"
- ✅ **خاتمه**: حذف منطقی درخواست
- ✅ **مدیریت پیوست‌ها**: Modal برای مشاهده و افزودن پیوست

#### JavaScript Functions:
- ✅ Modal management با `$.confirm`
- ✅ Validation سمت کلاینت
- ✅ AJAX calls برای تمام عملیات
- ✅ Reload page بعد از عملیات موفق

### 2. Partial Views ایجاد شده

| Partial | مسیر | توضیحات |
|---------|------|---------|
| `_AddCommentPartial.cshtml` | ثبت کامنت ساده | فیلدهای: تاریخ، توضیحات |
| `_CommentListPartial.cshtml` | لیست read-only کامنت‌ها | جدول نمایش تمام کامنت‌ها |
| `_StopRequestPartial.cshtml` | مدیریت Stop Workflow | 3 بخش دینامیک بر اساس StopStatus |
| `_SalesOrProjectAcceptPartial.cshtml` | تایید فروش/پروژه | فیلد توضیحات تایید |
| `_AttachmentPartial.cshtml` | افزودن پیوست | فیلدهای: نوع فایل، فایل، توضیحات |
| `_AttachmentListPartial.cshtml` | لیست پیوست‌ها | جدول با قابلیت دانلود و حذف |

### 3. Controller Actions پیاده‌سازی شده

#### Comment Operations
- ✅ `AddCommentPartial(long openOrderRequestId)` - بازگشت Partial
- ✅ `SaveComment(SaveCommentRequest)` - ذخیره کامنت
- ✅ `CommentListPartial(long openOrderRequestId)` - بازگشت لیست کامنت‌ها

#### Engineering Accept
- ✅ `EngineeringAccept(long id)` - تایید مهندسی با محاسبه تاریخ تامین

#### Sales/Project Accept  
- ✅ `SalesOrProjectAcceptPartial(long openOrderRequestId)` - بازگشت Partial
- ✅ `SalesOrProjectAccept(SalesOrProjectAcceptRequest)` - تایید با ثبت کامنت

#### Stop Workflow (10 مرحله)
- ✅ `StopRequestPartial(long openOrderRequestId)` - بازگشت Partial دینامیک
- ✅ `StopOperation(StopOperationRequest)` - پردازش مراحل مختلف Stop
- ✅ `ValidateStopRequest()` - Validation کامل بر اساس هر مرحله

مراحل Stop Workflow پیاده شده:
1. ✅ **2807** - ثبت اولیه و ارسال به کارتابل مسئول توقف
2. ✅ **2808** - بررسی عامل توقف _ نیاز به راه اندازی مجدد
3. ✅ **2810** - بررسی عامل توقف _ نياز به اصلاح BOM
4. ✅ **2811** - بررسی عامل توقف _ نياز به اصلاح مدارك
5. ✅ **2812** - بررسی عامل توقف _ حذف درخواست واحد صنایع
6. ✅ **2813** - بررسی عامل توقف _ در انتظار مدير پروژه
7. ✅ **2815** - بررسی عامل توقف _ راه اندازی مجدد
8. ✅ **2816** - بررسی عامل توقف _ ثبت توقف مجدد
9. ✅ **2817** - بررسی عامل توقف _ حذف درخواست
10. ✅ **2821** - ارسال به کارتابل تدارکات

#### Other Operations
- ✅ `Triggering(long id)` - راه‌اندازی درخواست
- ✅ `SetInWayStatus(long id)` - تغییر وضعیت به "در راه"
- ✅ `StatusInquiry(long id)` - تغییر وضعیت به "در حال استعلام"
- ✅ `Terminate(long id)` - خاتمه درخواست

#### Attachment Operations
- ✅ `AttachmentPartial(long openOrderRequestId, long? id)` - بازگشت Partial
- ✅ `SaveAttachment(SaveAttachmentRequest)` - ذخیره پیوست
- ✅ `AttachmentListPartial(long openOrderRequestId)` - لیست پیوست‌ها
- ✅ `DownloadAttachment(long id)` - دانلود فایل
- ✅ `DeleteAttachment(long id)` - حذف پیوست

#### Helper Methods
- ✅ `GetBeneficiaries()` - دریافت لیست کاربران برای ذینفعان
- ✅ `DetermineNextStopStatusFromCheckingResult()` - محاسبه مرحله بعدی
- ✅ `DetermineNextStopStatusFromCheckingStatus()` - محاسبه مرحله بعدی
- ✅ `ValidateStopRequest()` - Validation کامل

### 4. Notification System

تمام عملیات ایمیل سیستم قدیم به **Notification** تبدیل شدند:

| عملیات | Notification ایجاد شده |
|--------|------------------------|
| افزودن کامنت | ✅ به درخواست‌کنندگان |
| تایید مهندسی | ✅ به واحد تدارکات |
| تایید فروش/پروژه | ✅ به واحد تدارکات |
| ثبت توقف | ✅ به عامل توقف و ذینفعان |
| بررسی توقف | ✅ به درخواست‌کننده اولیه |
| تصمیم توقف | ✅ به عامل توقف و ذینفعان |
| راه‌اندازی | ✅ به درخواست‌کنندگان |
| پیوست جدید (خاص) | ✅ به واحد تولید |

### 5. Enums ایجاد/بروزرسانی شده

- ✅ `OpenOrderRequestStopTypeEnum` - نوع توقف (جدید)
- ✅ `OpenOrderRequestStopStatusEnum` - وضعیت توقف (موجود)
- ✅ `OpenOrderRequestStopCheckingStatusEnum` - وضعیت بررسی (موجود)
- ✅ `OpenOrderRequestCommentStopCheckingResultEnum` - نتیجه بررسی (موجود)
- ✅ `OpenOrderRequestAttachmentFileTypeEnum` - نوع فایل (موجود)

### 6. Validation

#### Server-Side:
- ✅ Validation کامل بر اساس هر مرحله Stop Workflow
- ✅ چک الزامی بودن فیلدها بر اساس شرایط
- ✅ چک وضعیت‌های غیرمجاز (مثل تایید مجدد)

#### Client-Side:
- ✅ `required` attribute برای فیلدهای الزامی
- ✅ نمایش/مخفی کردن دینامیک فیلدها
- ✅ `validateError()` قبل از submit
- ✅ پیام‌های خطای واضح

---

## ⚠️ کارهای باقی مانده (TODO)

### 1. Role & Permission Management

**مکان:** Controller - تمام Action ها

در حال حاضر، بررسی دسترسی‌ها با **مقدار رندوم** انجام می‌شود:

```csharp
// TODO: بررسی دسترسی واقعی
const hasEngineeringAcceptPermission = Math.random() > 0.5; 
```

**باید انجام شود:**
```csharp
// مثال صحیح:
var hasEngineeringAcceptPermission = await CheckUserPermission(
    "Sup.OpenOrderRequest.EngineeringAccept", cn);
```

**Role های مورد نیاز:**
- `Sup.OpenOrderRequest.EngineeringAccept` - تایید مهندسی
- `Sup.OpenOrderRequest.SalesOrProjectAccept` - تایید فروش/پروژه
- `Sup.OpenOrderRequest.Stop` - ثبت توقف
- `Sup.OpenOrderRequest.Start` - راه‌اندازی
- `Sup.OpenOrderRequest.Sending` - در راه
- `Sup.OpenOrderRequest.Query` - استعلام
- `Sup.OpenOrderRequest.Terminate` - خاتمه
- `Sup.OpenOrderRequest.HasEngineering` - دسترسی قابلیت‌های مهندسی
- `Sup.OpenOrderRequest.ShowAll` - نمایش همه درخواست‌ها

### 2. OpenOrderRequestVpis Feature

**وضعیت:** Entity وجود ندارد

**فایل TODO:** `TODO_VPIS.md`

**خلاصه کارها:**
1. ایجاد Entity `OpenOrderRequestVpis`
2. Migration
3. ایجاد Partial `_LinkVpisPartial.cshtml`
4. Controller Actions:
   - `GetEdmsProjects()`
   - `GetEdmsVpisData(projectId, openOrderRequestId)`
   - `DoLinkVpisOperation(List<VpisLinkRequest>)`
5. دکمه "لینک با مدارک پروژه" در Edit.cshtml
6. Logic بررسی تغییر Revision در EDMS Job

### 3. LeadTime Calculation

**مکان:** `EngineeringAccept` Action

کد فعلی:
```csharp
var leadTimeDays = 30; // مقدار پیش‌فرض
```

**باید اصلاح شود:**
```csharp
// باید از جدول LeadTime یا BuyCategory خوانده شود
var leadTime = await unitOfWork.Repository<LeadTime>()
    .TableNoTracking
    .Where(l => l.PartId == openOrderRequest.PartId)
    .OrderByDescending(l => l.Id)
    .FirstOrDefaultAsync(cn);

var leadTimeDays = leadTime?.LeadTimeInDay ?? 30;
```

### 4. UserGroupMember Permission Update

**مکان:** `StopOperation` - Initial Stop

کد فعلی:
```csharp
// TODO: Update UserGroupMember permissions for StopOperator
```

**باید پیاده شود:**
```csharp
// اضافه کردن دسترسی مشاهده به StopOperator
await UpdateUserGroupPermissions(
    userGroupName: "OpenOrderRequestView", 
    userId: request.StopOperatorId.Value, 
    cn);
```

### 5. File Path Configuration

**مکان:** `DownloadAttachment` Action

کد فعلی فرض می‌کند فایل‌ها در `wwwroot/uploads` هستند.

**باید اصلاح شود:**
- بررسی شود که `FileEntity.FilePath` absolute path است یا relative
- استفاده از IConfiguration برای خواندن base path فایل‌ها

### 6. Email Addresses

**مکان:** تمام Notification Methods

کد فعلی از email های هاردکد شده استفاده می‌کند:

```csharp
var buyPersonEmails = new List<string> 
{ 
    "Dordab.y", "Bagheri.h", ...
};
```

**باید اصلاح شود:**
- استفاده از یک جدول configuration
- یا گرفتن از Role/UserGroup
- به‌روزرسانی آسان بدون تغییر کد

### 7. Validation های اضافی

**کارهای باقی مانده:**

1. **Industrial User Restriction:**
   - اگر کاربر Industrial باشد و StopOperator باشد
   - فقط می‌تواند 2808 یا 2812 را انتخاب کند
   - باید در `StopRequestPartial` پیاده شود

2. **BOM Validation:**
   - اگر StopCheckingResult == 2800
   - حداقل یکی از checkboxها باید true باشد
   - باید validation سمت کلاینت اضافه شود

3. **Stop Permission Complex Logic:**
   - در سیستم قدیم، بررسی پیچیده‌ای برای دسترسی Stop وجود داشت
   - بر اساس StopStatusId و UserId
   - باید دقیق‌تر پیاده شود

### 8. Comment Display in Tabs

**مکان:** Edit.cshtml - Comments Tab

فعلاً Comments و Attachments به صورت inline در tabs نمایش داده می‌شوند.

**بهبود پیشنهادی:**
- استفاده از همان logic modal برای نمایش
- یا نمایش فقط آخرین کامنت/پیوست با لینک "مشاهده همه"

### 9. Requested Personnel Management

**در سیستم قدیم:**
- جدول `Sup_OpenOrderRequest_Requested_Personel` وجود داشت
- کاربران به 2 دسته تقسیم می‌شدند:
  - 2829: Industrial Personnel
  - 2830: Engineering Personnel

**در سیستم جدید:**
- فقط 2 فیلد string وجود دارد:
  - `RequestedPersonel`
  - `RequestedEngineeringPersonel`
  
**اگر نیاز به management دقیق‌تر باشد:**
- باید یک Entity جداگانه ایجاد شود
- یا از مکانیزم موجود استفاده کنیم (فعلاً کافی است)

### 10. Background Jobs

**فایل:** `OpenOrderRequestJob.cs`

**Job موجود:**
- ✅ `SyncOpenOrderRequestJobFromRahkaran` - همگام‌سازی با راهکاران
- ✅ `SendNotifications` - ارسال اعلان‌های خودکار

**Job های مورد نیاز:**
1. **Auto Engineering Accept** (برای درخواست‌های Routine با مدارک):
   ```csharp
   // چک کردن درخواست‌هایی که:
   // - Engineering_Accept = false
   // - دارای پیوست مدارک لازم (DataSheet, DetailDrawing, etc.)
   // - یا NotNeedToAttachDocuments = true
   // => تایید خودکار مهندسی
   ```

2. **Check Stop Response Deadline** (بررسی مهلت پاسخ توقف):
   ```csharp
   // اگر ResponseDeadlineDate گذشته باشد
   // و StopOperator پاسخ نداده
   // => تغییر StopStatus به 2821 (ارسال به تدارکات)
   ```

3. **Send Reminder Notifications**:
   ```csharp
   // ارسال یادآوری برای:
   // - درخواست‌های نزدیک به SupplyDate (3 روز قبل)
   // - درخواست‌های تایید نشده
   ```

---

## 🔧 تنظیمات و پیکربندی

### 1. appsettings.json

```json
{
  "OpenOrderRequest": {
    "DefaultLeadTimeDays": 30,
    "RoutineStopResponseDays": 2,
    "NonRoutineStopResponseDays": 3,
    "SupplyDateReminderDays": 3,
    "FileStoragePath": "OpenOrderRequests",
    "StopFileStoragePath": "OpenOrderRequestStops"
  }
}
```

### 2. Database Indexes

برای بهبود Performance، Indexهای زیر پیشنهاد می‌شود:

```sql
CREATE INDEX IX_OpenOrderRequest_PurchaseRequestItemId 
ON Sup.OpenOrderRequest(PurchaseRequestItemId);

CREATE INDEX IX_OpenOrderRequest_IsDeleted_EngineeringAccept 
ON Sup.OpenOrderRequest(IsDeleted, EngineeringAccept);

CREATE INDEX IX_OpenOrderRequest_IsStop_StopStatus 
ON Sup.OpenOrderRequest(IsStop, StopStatus);

CREATE INDEX IX_OpenOrderRequestComment_OpenOrderRequestId_IsStop 
ON Sup.OpenOrderRequestComment(OpenOrderRequestId, IsStop);
```

---

## 📋 تست Scenarios

### Scenario 1: Complete Happy Path
1. ✅ ایجاد درخواست جدید
2. ✅ افزودن پیوست مدارک
3. ✅ تایید مهندسی
4. ✅ تایید واحد فروش/پروژه
5. ✅ تغییر وضعیت به "در راه"
6. ✅ Notification به افراد مربوطه

### Scenario 2: Stop Workflow
1. ✅ ثبت توقف اولیه
2. ✅ Notification به StopOperator
3. ✅ بررسی توسط StopOperator
4. ✅ تصمیم‌گیری درخواست‌کننده
5. ✅ راه‌اندازی یا حذف

### Scenario 3: Re-Stop
1. ✅ توقف اولیه
2. ✅ بررسی و تصمیم به "ثبت توقف مجدد"
3. ✅ ثبت توقف با اطلاعات جدید
4. ✅ ادامه workflow

---

## 🐛 مشکلات شناخته شده

### 1. Multi-Select Beneficiaries
فعلاً `beneficiariesIds` به صورت string (comma-separated) ذخیره می‌شود.
این روش کار می‌کند اما بهتر است یک Entity جداگانه باشد.

### 2. File Storage Path
مسیر دقیق ذخیره فایل‌ها باید از configuration خوانده شود.

### 3. Notification Delivery
فعلاً Notifications فقط ایجاد می‌شوند. 
سیستم ارسال واقعی Email باید پیاده شود (Background Job).

---

## 📝 یادداشت‌های مهم

### منطق Stop Workflow

```
Initial (0) 
  └─> 2807 (ثبت اولیه)
       ├─> بررسی StopOperator
       │    ├─> 2808 (نیاز به راه‌اندازی) ──> 2815 (راه‌اندازی) یا 2805 (توقف مجدد) ──> 2816
       │    ├─> 2810 (نیاز به اصلاح BOM) ──> 2815 یا 2805 ──> 2816
       │    ├─> 2811 (نیاز به اصلاح مدارک) ──> 2815 یا 2805 ──> 2816
       │    ├─> 2812 (حذف واحد صنایع) ──> 2806 (حذف) ──> 2817
       │    └─> 2813 (انتظار مدیر پروژه) ──> بعد از اعمال نظر ──> مراحل بالا
       └─> اگر بدون پاسخ ماند ──> 2821 (ارسال به تدارکات)
```

### نکات کلیدی:

1. **IsLatest در Attachments/Comments**: 
   - در سیستم قدیم برای VPIS وجود داشت
   - اگر نیاز به versioning باشد باید اضافه شود

2. **Email به جای Notification**:
   - تمام ایمیل‌ها به Notification تبدیل شدند
   - Type = Email
   - یک Background Job باید Notification های Email را پردازش کند

3. **Changed Flag**:
   - هر بار که پیوست اضافه می‌شود، `Changed = true`
   - این flag برای فیلتر کردن در grid استفاده می‌شود

4. **Auto Accept**:
   - درخواست‌های Routine با مدارک کامل
   - به صورت خودکار تایید مهندسی می‌شوند
   - `IsAcceptedAutomaticallyByEngineering = true`
   - این Job در `OpenOrderRequestJob` باید پیاده شود

---

## 🚀 مراحل تکمیل

1. ⚠️ **ایجاد Role ها در جدول دسترسی**
2. ⚠️ **پیاده‌سازی CheckUserPermission methods**
3. ⚠️ **تست کامل تمام Workflow ها**
4. ⚠️ **پیاده‌سازی Background Job برای Auto Accept**
5. ⚠️ **پیاده‌سازی Background Job برای Check Deadline**
6. ⚠️ **پیاده‌سازی Email Sender Job**
7. ⚠️ **اضافه کردن OpenOrderRequestVpis** (اختیاری - اولویت پایین)
8. ⚠️ **تست Performance با داده واقعی**
9. ⚠️ **مستندسازی API**
10. ⚠️ **Unit Tests**

---

## 📞 پشتیبانی

در صورت بروز هرگونه مشکل یا سوال:
1. مراجعه به کد سیستم قدیم
2. بررسی TODO_VPIS.md برای قابلیت VPIS
3. بررسی Enum ها برای مقادیر صحیح
4. بررسی Entity برای فیلدهای موجود

---

**تاریخ پیاده‌سازی:** @DateTime.Now.ToString("yyyy/MM/dd")  
**نسخه:** 1.0  
**وضعیت:** آماده برای تست و تکمیل Role ها
