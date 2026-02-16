# TODO: پیاده‌سازی OpenOrderRequestVpis

## توضیحات
در سیستم قبلی، قابلیتی برای لینک کردن درخواست‌های باز با مدارک پروژه (VPIS) از سیستم EDMS وجود داشت.
این قابلیت در سیستم جدید پیاده‌سازی نشده و نیاز به ایجاد Entity و Logic های مربوطه دارد.

## Entity مورد نیاز

```csharp
[Display(Name = "لینک درخواست باز با مدارک پروژه")]
[Table("OpenOrderRequestVpis", Schema = "Sup")]
public class OpenOrderRequestVpis : BaseEntity
{
    public long OpenOrderRequestId { get; set; }
    public OpenOrderRequest OpenOrderRequest { get; set; }

    [DisplayName("شناسه پروژه")]
    public long ProjectId { get; set; }
    // TODO: اضافه کردن Navigation Property به Project (اگر Entity Project در سیستم EDMS موجود باشد)

    [DisplayName("شناسه VPIS پروژه")]
    public long ProjectVpisId { get; set; }

    [DisplayName("شناسه مدرک")]
    public long DocumentId { get; set; }

    [DisplayName("شماره نسخه")]
    public string? RevisionNumber { get; set; }

    [DisplayName("آخرین نسخه")]
    public bool IsLatest { get; set; } = true;
}
```

## Functionality های مورد نیاز

### 1. دکمه "لینک با مدارک پروژه"
- باید در `form-action-buttons` اضافه شود
- فقط برای کاربران با دسترسی مهندسی نمایش داده شود
- Modal برای انتخاب پروژه و مدارک VPIS

### 2. Partial View: _LinkVpisPartial.cshtml
```html
@* نمایش لیست پروژه‌ها *@
@* Dropdown برای انتخاب پروژه *@
@* Multi-select برای انتخاب مدارک VPIS مربوط به پروژه *@
@* نمایش مدارک قبلاً لینک شده *@
```

### 3. Controller Actions مورد نیاز

#### GetEdmsProjects
```csharp
// دریافت لیست پروژه‌های EDMS که محرمانه نباشند
// فیلتر کردن پروژه‌هایی که وضعیت 3، 6، 7 دارند
```

#### GetEdmsVpisData
```csharp
// پارامترها: projectId, openOrderRequestId
// دریافت لیست VPIS های مربوط به پروژه
// فقط مداركی که:
//   - Document_Title شامل "*" باشد و ApprovedDate داشته باشد
//   - یا VendorProject باشد و بیش از 1 Comment داشته باشد  
//   - یا NotReview status داشته باشد
// همچنین مدارک قبلاً لینک شده را برگرداند
```

#### DoLinkVpisOperation
```csharp
// ذخیره لینک‌های جدید
// غیرفعال کردن IsLatest برای لینک‌های قبلی
// ایجاد رکوردهای جدید با IsLatest = true
```

### 4. بررسی تغییر Revision مدارک

در سیستم قبلی، یک Job وجود داشت که:
- هنگام افزایش Revision مدارک EDMS
- تمام OpenOrderRequest هایی که به آن مدرک لینک شده‌اند را پیدا کند
- `HasSalesUnitConfirmation` را `false` کند
- Notification به واحد فروش/پروژه ارسال کند تا دوباره تایید کنند
- Revision جدید را در OpenOrderRequestVpis ثبت کند

این Logic باید در Job مربوط به EDMS Document پیاده شود.

### 5. نکات مهم

1. **Permission Check**: 
   - فقط کاربران با `HasEngineeringPermission` می‌توانند لینک کنند
   - کد قدیمی: `hasHasEngineeringPermission`

2. **Integration با EDMS**:
   - نیاز به دسترسی به جداول:
     - `Edms_Project`
     - `Edms_Project_Vpis`
     - `Edms_Document`
     - `Edms_Document_Comment`

3. **Workflow**:
   - هنگام لینک VPIS، اگر قبلاً تایید فروش داشته، باید تایید باطل شود
   - زیرا تغییر در مدارک نیاز به بررسی مجدد دارد

4. **Display در UI**:
   - در لیست درخواست‌ها، یک ستون "دارای لینک VPIS" نمایش داده شود
   - در Edit page، لیست VPIS های لینک شده نمایش داده شود

## مراحل پیاده‌سازی

1. ایجاد Entity `OpenOrderRequestVpis` در `Entities/App/Sup/`
2. اضافه کردن Migration
3. ایجاد Partial View `_LinkVpisPartial.cshtml`
4. اضافه کردن دکمه در Edit.cshtml:
```javascript
<form-action-button color-class="btn-active-icon-dark btn-color-success" 
    icon-class="fa fa-link" action-name="linkVpis" 
    title="لینک با مدارک پروژه"></form-action-button>
```
5. پیاده‌سازی Controller Actions
6. پیاده‌سازی Logic بررسی تغییر Revision در EDMS Job
7. تست کامل Workflow

## کد Reference از سیستم قدیم

### JavaScript Modal (Old System)
```javascript
function openOrderRequestManagement_btnLinkWithProjectDocuments_click(e) {
    openOrderRequestManagement_createAndOpenWindow();
    openOrderRequestManagement_cmbProjects.select(0);
    openOrderRequestManagement_cmbVpis.dataSource.data([]);
    
    var params = { parentId: openOrderRequestManagement_selectedGridItem.Id };
    window.DoAjaxOperation(openOrderRequestManagement_GetOrderRequestProjectDataUrl, 
        params, "post", 'json').done(function(result) {
        if (result.Object !== 0) {
            openOrderRequestManagement_cmbProjects.value(result.Object);
            openOrderRequestManagement_cmbProductionOrderStatus_Change();
        }
    });
}
```

### Controller Method (Old System)
```csharp
[HttpPost]
public virtual ActionResult DoLinkVpisOperation(List<Sup_OpenOrderRequestVpis> vpisList)
{
    vpisList.ForEach(p => {
        p.UpdatedUserId = p.CreatedUserId = UserId;
    });
    var result = _openOrderRequestVpisService.DoOperation(vpisList);
    return ReturnOperationResult(result);
}
```

### Check Revision Change (Old System)
```csharp
public void CheckOpenOrderRequestVpisRevisionAndSendEmailNotification(Edms_Document document)
{
    var openOrderRequestVpis = _openOrderRequestVpisService
        .Where(p => p.ProjectVpisId == document.Project_Vpis_FK 
            && p.IsLatest && !p.Sup_OpenOrderRequest.IsDeleted)
        .ToList();

    // غیرفعال کردن لینک‌های قبلی
    openOrderRequestVpis.ForEach(p => p.IsLatest = false);
    
    // ایجاد لینک‌های جدید با Revision جدید
    // ایجاد Notification
    // غیرفعال کردن HasSalesUnitConfirmation
}
```

## اولویت
این قابلیت در اولویت پایین‌تر است و می‌تواند در مرحله بعد پیاده شود.
ابتدا workflow اصلی Stop و Confirmation ها باید کامل شوند.
