# نحوه استفاده از Expression Builder به صورت Dynamic

## روش جدید: بدون نیاز به `<T>` Generic

با استفاده از متدهای جدید، دیگر نیازی نیست که نوع Entity را به صورت Generic مشخص کنید. سیستم به صورت خودکار از `EntityFullName` موجود در `EntityMetadata` استفاده می‌کند.

---

## 🎯 روش 1: استفاده ساده (بدون Cache)

```csharp
// 1. دریافت Notification از دیتابیس
var notification = await context.NotificationBuilders.FindAsync(notificationId);

// 2. تبدیل JSON به RolesNode
var rule = JsonConvert.DeserializeObject<RolesNode>(notification.ConditionsJson);

// 3. دریافت EntityMetadata
var entityMetadata = EntityCache.Get(notification.EntityFullName);

// 4. ساخت Expression به صورت Dynamic (از EntityFullName استفاده می‌کند)
var expression = DynamicExpressionBuilder.BuildExpressionDynamic(rule, entityMetadata);

// 5. استفاده با DbContext
var query = DynamicQueryHelper.ApplyRule(context, rule, entityMetadata);
var results = await query.ToListAsync();
```

---

## ⚡ روش 2: استفاده با Cache (پیشنهادی)

```csharp
// دریافت Notification
var notification = await context.NotificationBuilders.FindAsync(notificationId);
var rule = JsonConvert.DeserializeObject<RolesNode>(notification.ConditionsJson);
var entityMetadata = EntityCache.Get(notification.EntityFullName);

// ساخت یا دریافت Expression از Cache
var expression = NotificationExpressionCache.GetOrBuildExpressionDynamic(
    notificationId: notification.Id,
    rule: rule,
    entityMetadata: entityMetadata
);

// استفاده با DbContext
var query = DynamicQueryHelper.ApplyRuleWithCache(
    context,
    notification.Id,
    rule,
    entityMetadata
);

var results = await query.ToListAsync();
```

---

## 📋 مثال کامل: پردازش Contact های جدید

```csharp
public class NotificationProcessor
{
    private readonly ApplicationDbContext _context;

    public NotificationProcessor(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// زمانی که یک Contact جدید ساخته می‌شود
    /// </summary>
    public async Task OnContactCreated(long contactId)
    {
        // 1. دریافت همه Notification های مربوط به Create برای Contact
        var notifications = await _context.NotificationBuilders
            .Where(n => n.EntityFullName == "Entities.Auth.Contact" && n.Operation == "Create")
            .ToListAsync();

        foreach (var notification in notifications)
        {
            try
            {
                // 2. تبدیل JSON به Rule
                var rule = JsonConvert.DeserializeObject<RolesNode>(notification.ConditionsJson);
                
                // 3. دریافت EntityMetadata
                var entityMetadata = EntityCache.Get(notification.EntityFullName);
                
                // 4. کوئری به صورت Dynamic
                var query = DynamicQueryHelper.ApplyRuleWithCache(
                    _context,
                    notification.Id,
                    rule,
                    entityMetadata
                );
                
                // 5. چک کردن که آیا Contact جدید با Rule مطابقت دارد
                var matchingContact = await query
                    .OfType<Contact>() // Cast به نوع مورد نظر
                    .FirstOrDefaultAsync(c => c.Id == contactId);
                
                if (matchingContact != null)
                {
                    // ارسال Notification
                    await SendNotification(notification, matchingContact);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    private async Task SendNotification(NotifictionBuilder notification, Contact contact)
    {
        var message = notification.MessageTemplate
            .Replace("{FirstName}", contact.FirstName)
            .Replace("{LastName}", contact.LastName);

        // ارسال پیام...
        await Task.CompletedTask;
    }
}
```

---

## 🔄 مثال: دریافت لیست Contact های مطابق با Rule

```csharp
public async Task<List<object>> GetMatchingEntities(long notificationId)
{
    // دریافت Notification
    var notification = await _context.NotificationBuilders.FindAsync(notificationId);
    if (notification == null)
        return new List<object>();

    // Parse rule
    var rule = JsonConvert.DeserializeObject<RolesNode>(notification.ConditionsJson);
    var entityMetadata = EntityCache.Get(notification.EntityFullName);

    // کوئری Dynamic
    var query = DynamicQueryHelper.ApplyRuleWithCache(
        _context,
        notificationId,
        rule,
        entityMetadata
    );

    // دریافت نتایج
    var results = new List<object>();
    foreach (var item in query)
    {
        results.Add(item);
    }

    return results;
}
```

---

## 💾 بروزرسانی Rule (با پاک کردن Cache)

```csharp
public async Task UpdateNotificationRule(long notificationId, SaveNotifictionBuilder model)
{
    // 1. دریافت Notification
    var notification = await _context.NotificationBuilders.FindAsync(notificationId);
    
    // 2. بروزرسانی JSON
    notification.ConditionsJson = JsonConvert.SerializeObject(model.Create.Roles);
    notification.MessageTemplate = model.Create.Title;
    
    // 3. ذخیره در دیتابیس
    await _context.SaveChangesAsync();
    
    // 4. مهم: پاک کردن Cache!
    NotificationExpressionCache.ClearCache(notificationId);
}
```

---

## 🎨 مثال با چندین Entity مختلف

```csharp
public async Task ProcessMultipleEntityTypes()
{
    // دریافت همه Notification ها
    var notifications = await _context.NotificationBuilders
        .Where(n => n.Operation == "Create")
        .ToListAsync();

    foreach (var notification in notifications)
    {
        var rule = JsonConvert.DeserializeObject<RolesNode>(notification.ConditionsJson);
        var entityMetadata = EntityCache.Get(notification.EntityFullName);

        // هر Notification می‌تواند برای Entity متفاوتی باشد
        // سیستم به صورت خودکار نوع را تشخیص می‌دهد
        
        var query = DynamicQueryHelper.ApplyRuleWithCache(
            _context,
            notification.Id,
            rule,
            entityMetadata
        );

        var count = query.Count();
        Console.WriteLine($"Entity: {notification.EntityFullName}, Matches: {count}");
    }
}
```

---

## ⚙️ مقایسه روش قدیم و جدید

### ❌ روش قدیم (نیاز به Generic Type):
```csharp
// باید از قبل بدانید که Entity از نوع Contact است
var expression = DynamicExpressionBuilder.BuildExpression<Contact>(rule, entityMetadata);
var results = context.Contacts.Where(expression).ToList();
```

### ✅ روش جدید (Dynamic):
```csharp
// نیازی به مشخص کردن نوع نیست - از EntityFullName استفاده می‌کند
var query = DynamicQueryHelper.ApplyRule(context, rule, entityMetadata);
var results = await query.ToListAsync();
```

---

## 📊 مزایای روش Dynamic

1. ✅ **انعطاف‌پذیری بالا** - نیازی به مشخص کردن Type از قبل نیست
2. ✅ **کد تمیزتر** - بدون Generic Type Parameters
3. ✅ **مناسب برای سیستم‌های داینامیک** - Entity Type در Runtime مشخص می‌شود
4. ✅ **سازگار با Cache** - همه چیز به صورت خودکار Cache می‌شود
5. ✅ **استفاده آسان با DbContext** - با `DynamicQueryHelper`

---

## 🔍 نکات مهم

### 1. EntityFullName باید صحیح باشد
```csharp
// صحیح
entityMetadata.EntityFullName = "Entities.Auth.Contact"

// غلط
entityMetadata.EntityFullName = null
entityMetadata.EntityFullName = ""
```

### 2. Assembly باید Load شده باشد
Type باید در یکی از Assembly های Load شده موجود باشد. اگر خطا دریافت کردید:
```csharp
// اطمینان حاصل کنید که Assembly مربوطه Load شده
Assembly.Load("Entities");
```

### 3. Cast به نوع مشخص در صورت نیاز
```csharp
var query = DynamicQueryHelper.ApplyRule(context, rule, entityMetadata);

// اگر نیاز به استفاده از Property های مشخص دارید:
var typedResults = query.OfType<Contact>().ToList();
```

---

## 🎯 خلاصه

| ویژگی | روش Generic | روش Dynamic |
|-------|-------------|-------------|
| نیاز به `<T>` | ✅ بله | ❌ خیر |
| استفاده از EntityFullName | ❌ خیر | ✅ بله |
| انعطاف‌پذیری | متوسط | بالا |
| سرعت | سریع | سریع |
| Cache | دارد | دارد |

**توصیه:** برای سیستم Notification که Entity Type در Runtime مشخص می‌شود، از روش **Dynamic** استفاده کنید.


