# Expression Builder - Complete Usage Guide

## Understanding Expression Persistence

### ❌ You CANNOT Save Expressions to Database
Expression trees are **compiled code structures** that cannot be serialized to a database.

### ✅ You CAN Save JSON Rules
Your `NotifictionBuilder` entity already stores the rule as JSON in `ConditionsJson`.

```
┌─────────────────────────────────────────────────────────────┐
│  Save to Database          In-Memory (Runtime)              │
│  ─────────────────         ─────────────────                │
│                                                              │
│  RolesNode (JSON)   ──────>  Expression<Func<T, bool>>     │
│  ConditionsJson              (Built on-demand or cached)    │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

## Complete Workflow

### Step 1: Save Notification Rule to Database

```csharp
public async Task<long> CreateNotificationRule(SaveNotifictionBuilder model)
{
    // 1. Create the notification entity
    var notification = new NotifictionBuilder
    {
        EntityFullName = "MyApp.Entities.Contact",
        Operation = "Create",
        
        // Save the RolesNode as JSON - THIS IS WHAT PERSISTS!
        ConditionsJson = JsonConvert.SerializeObject(model.Create.Roles),
        
        MessageTemplate = "New contact {FirstName} {LastName} created",
        MessageTitle = "New Contact Alert",
        UserId = currentUserId
    };

    // 2. Save to database
    await dbContext.NotificationBuilders.AddAsync(notification);
    await dbContext.SaveChangesAsync();

    return notification.Id;
}
```

### Step 2: Load and Use Rule (WITHOUT Caching)

```csharp
public async Task ProcessNewContact(Contact newContact)
{
    // 1. Load notification rules from database
    var notifications = await dbContext.NotificationBuilders
        .Where(n => n.EntityFullName == "MyApp.Entities.Contact" 
                 && n.Operation == "Create")
        .ToListAsync();

    foreach (var notification in notifications)
    {
        // 2. Deserialize JSON to RolesNode
        var rule = JsonConvert.DeserializeObject<RolesNode>(notification.ConditionsJson);
        
        // 3. Get entity metadata
        var entityMetadata = GetEntityMetadata("MyApp.Entities.Contact");
        
        // 4. Build expression (rebuilds each time - very fast)
        var expression = DynamicExpressionBuilder.BuildExpression<Contact>(rule, entityMetadata);
        
        // 5. Check if new contact matches the rule
        var compiledFunc = expression.Compile();
        if (compiledFunc(newContact))
        {
            // Send notification
            await SendNotification(notification, newContact);
        }
    }
}
```

### Step 3: Load and Use Rule (WITH Caching - RECOMMENDED)

```csharp
public async Task ProcessNewContactWithCaching(Contact newContact)
{
    // 1. Load notification rules from database
    var notifications = await dbContext.NotificationBuilders
        .Where(n => n.EntityFullName == "MyApp.Entities.Contact" 
                 && n.Operation == "Create")
        .ToListAsync();

    foreach (var notification in notifications)
    {
        // 2. Deserialize JSON to RolesNode
        var rule = JsonConvert.DeserializeObject<RolesNode>(notification.ConditionsJson);
        
        // 3. Get entity metadata
        var entityMetadata = GetEntityMetadata("MyApp.Entities.Contact");
        
        // 4. Get or build expression WITH CACHE
        var compiledFunc = NotificationExpressionCache.GetOrBuildCompiledFunc<Contact>(
            notificationId: notification.Id,
            rule: rule,
            entityMetadata: entityMetadata
        );
        
        // 5. Check if new contact matches the rule
        if (compiledFunc(newContact))
        {
            await SendNotification(notification, newContact);
        }
    }
}
```

## Usage Scenarios

### Scenario 1: Query Database (Use with EF Core)

```csharp
public async Task<List<Contact>> FindMatchingContacts(long notificationId)
{
    // Load notification from database
    var notification = await dbContext.NotificationBuilders.FindAsync(notificationId);
    var rule = JsonConvert.DeserializeObject<RolesNode>(notification.ConditionsJson);
    var entityMeta = GetEntityMetadata(notification.EntityFullName);

    // Build expression WITH CACHE (for EF Core queries)
    var expression = NotificationExpressionCache.GetOrBuildExpression<Contact>(
        notificationId: notification.Id,
        rule: rule,
        entityMetadata: entityMeta
    );

    // Query database
    var matchingContacts = await dbContext.Contacts
        .Where(expression)  // ✅ EF Core translates to SQL
        .ToListAsync();

    return matchingContacts;
}
```

### Scenario 2: Check Single Entity (In-Memory)

```csharp
public async Task<bool> CheckIfContactMatchesRule(Contact contact, long notificationId)
{
    // Load notification
    var notification = await dbContext.NotificationBuilders.FindAsync(notificationId);
    var rule = JsonConvert.DeserializeObject<RolesNode>(notification.ConditionsJson);
    var entityMeta = GetEntityMetadata(notification.EntityFullName);

    // Get COMPILED function (faster for in-memory checks)
    var compiledFunc = NotificationExpressionCache.GetOrBuildCompiledFunc<Contact>(
        notificationId: notification.Id,
        rule: rule,
        entityMetadata: entityMeta
    );

    // Check the contact
    return compiledFunc(contact);
}
```

### Scenario 3: Bulk Processing

```csharp
public async Task ProcessBulkContactsWithRule(List<Contact> contacts, long notificationId)
{
    // Load notification once
    var notification = await dbContext.NotificationBuilders.FindAsync(notificationId);
    var rule = JsonConvert.DeserializeObject<RolesNode>(notification.ConditionsJson);
    var entityMeta = GetEntityMetadata(notification.EntityFullName);

    // Get compiled function (cached)
    var compiledFunc = NotificationExpressionCache.GetOrBuildCompiledFunc<Contact>(
        notificationId: notification.Id,
        rule: rule,
        entityMetadata: entityMeta
    );

    // Process all contacts
    foreach (var contact in contacts)
    {
        if (compiledFunc(contact))
        {
            await SendNotification(notification, contact);
        }
    }
}
```

### Scenario 4: Update Rule (Clear Cache)

```csharp
public async Task UpdateNotificationRule(long notificationId, SaveNotifictionBuilder model)
{
    // 1. Load existing notification
    var notification = await dbContext.NotificationBuilders.FindAsync(notificationId);

    // 2. Update the JSON
    notification.ConditionsJson = JsonConvert.SerializeObject(model.Create.Roles);
    notification.MessageTemplate = model.Create.Title;

    // 3. Save to database
    await dbContext.SaveChangesAsync();

    // 4. IMPORTANT: Clear the cache!
    NotificationExpressionCache.ClearCache(notificationId);
}
```

### Scenario 5: Delete Rule (Clear Cache)

```csharp
public async Task DeleteNotificationRule(long notificationId)
{
    // 1. Delete from database
    var notification = await dbContext.NotificationBuilders.FindAsync(notificationId);
    dbContext.NotificationBuilders.Remove(notification);
    await dbContext.SaveChangesAsync();

    // 2. Clear the cache
    NotificationExpressionCache.ClearCache(notificationId);
}
```

## Complete Real-World Example

```csharp
public class NotificationProcessor
{
    private readonly ApplicationDbContext _context;

    public NotificationProcessor(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Process a newly created contact and send notifications
    /// </summary>
    public async Task OnContactCreated(Contact newContact)
    {
        // 1. Get all "Create" notifications for Contact entity
        var notifications = await _context.NotificationBuilders
            .Where(n => n.EntityFullName == "MyApp.Entities.Contact" 
                     && n.Operation == "Create")
            .ToListAsync();

        foreach (var notification in notifications)
        {
            try
            {
                // 2. Deserialize the rule
                var rule = JsonConvert.DeserializeObject<RolesNode>(notification.ConditionsJson);
                
                // 3. Get entity metadata (should be cached)
                var entityMeta = EntityMetadataCache.Get("MyApp.Entities.Contact");
                
                // 4. Get compiled evaluation function (cached)
                var evaluator = NotificationExpressionCache.GetOrBuildCompiledFunc<Contact>(
                    notificationId: notification.Id,
                    rule: rule,
                    entityMetadata: entityMeta
                );
                
                // 5. Check if contact matches the rule
                if (evaluator(newContact))
                {
                    // 6. Send notification to user
                    await SendNotificationToUser(notification, newContact);
                }
            }
            catch (Exception ex)
            {
                // Log error but continue with other notifications
                Console.WriteLine($"Error processing notification {notification.Id}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Get all contacts matching a notification rule
    /// </summary>
    public async Task<List<Contact>> GetContactsMatchingRule(long notificationId)
    {
        // 1. Load notification
        var notification = await _context.NotificationBuilders.FindAsync(notificationId);
        if (notification == null)
            return new List<Contact>();

        // 2. Deserialize rule
        var rule = JsonConvert.DeserializeObject<RolesNode>(notification.ConditionsJson);
        
        // 3. Get metadata
        var entityMeta = EntityMetadataCache.Get(notification.EntityFullName);
        
        // 4. Get expression (cached)
        var expression = NotificationExpressionCache.GetOrBuildExpression<Contact>(
            notificationId: notification.Id,
            rule: rule,
            entityMetadata: entityMeta
        );
        
        // 5. Query database with expression
        return await _context.Contacts
            .Where(expression)
            .ToListAsync();
    }

    private async Task SendNotificationToUser(NotifictionBuilder notification, Contact contact)
    {
        // Replace template placeholders
        var message = notification.MessageTemplate
            .Replace("{FirstName}", contact.FirstName)
            .Replace("{LastName}", contact.LastName)
            .Replace("{NationalCode}", contact.NationalCode);

        // Your notification sending logic here
        Console.WriteLine($"Notification to User {notification.UserId}: {message}");
        
        await Task.CompletedTask;
    }
}
```

## Performance Comparison

| Method | Speed | Use Case |
|--------|-------|----------|
| **No Cache** | ~100μs | Low-frequency rules |
| **Expression Cache** | ~1μs | EF Core queries |
| **Compiled Cache** | ~0.1μs | In-memory checks |

Building an expression is **very fast** (~100 microseconds), so caching is only needed for:
- Rules evaluated **thousands of times per second**
- Performance-critical paths

## Cache Management

### Clear Cache on Application Startup (Optional)

```csharp
public class Startup
{
    public void Configure(IApplicationBuilder app)
    {
        // Clear cache on startup to ensure fresh rules
        NotificationExpressionCache.ClearAllCache();
    }
}
```

### Monitor Cache Size

```csharp
var stats = NotificationExpressionCache.GetStatistics();
Console.WriteLine($"Cached Expressions: {stats.ExpressionCacheCount}");
Console.WriteLine($"Compiled Functions: {stats.CompiledCacheCount}");
Console.WriteLine($"Estimated Memory: {stats.TotalMemoryEstimate / 1024} KB");
```

## Summary

| What | Where | When |
|------|-------|------|
| **JSON Rule** | Database (`ConditionsJson`) | Always persisted |
| **Expression** | Memory (cache or rebuild) | Built on-demand |
| **Compiled Func** | Memory (cache) | For performance |

**Key Points:**
1. ✅ Save JSON to database (`ConditionsJson`)
2. ✅ Build expression from JSON (fast, can rebuild)
3. ✅ Cache expression for performance (optional)
4. ✅ Clear cache when rules change
5. ❌ Never try to serialize expressions to database

The system is designed correctly - you're already saving what you need!


