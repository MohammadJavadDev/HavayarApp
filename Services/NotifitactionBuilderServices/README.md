# Dynamic Expression Builder for Notification Rules

## Overview

The **DynamicExpressionBuilder** is a powerful utility that converts nested JSON rule structures (`RolesNode`) into Entity Framework Core compatible `Expression<Func<T, bool>>` expressions. This enables dynamic, runtime query generation for notification filtering based on complex business rules.

## Features

✅ **Full Operator Support**
- Comparison: `=`, `!=`, `>`, `<`, `>=`, `<=`
- String: `contains`, `starts`, `ends`
- Range: `between`, `!between`
- Null checks: `null`, `!null`
- Collection: `any`, `all`
- List: `in`, `!in`

✅ **All Field Types**
- `String`, `Int`, `Long`, `Boolean`
- `DateTime`, `Date`
- `DateTimeShamsi`, `DateShamsi` (Persian Calendar with automatic conversion)
- `Select` (dropdown fields)
- `Entity` (single navigation properties)
- `ListEntity` (collection navigation properties)

✅ **Advanced Features**
- Nested entity navigation (up to 3+ levels deep)
- Collection filtering with `Any`/`All` operators
- Complex AND/OR logic groups
- Persian DateTime automatic conversion
- Type-safe expressions
- Null-safe navigation

## Quick Start

### Basic Usage

```csharp
using Services.NotifitactionBuilderServices;
using Common.Entities.EntityMetadatas;
using Entities.Base.NotifitactionBuilder;

// 1. Define your rule (from JSON or RolesNode object)
var rule = new RolesNode
{
    Logic = "AND",
    Criteria = new List<RolesNode>
    {
        new RolesNode
        {
            Data = "NationalCode",
            Condition = "contains",
            Value = new List<string> { "1100" },
            FieldType = "string"
        }
    }
};

// 2. Get entity metadata
var entityMetadata = GetEntityMetadata(); // From your cache

// 3. Build the expression
var expression = DynamicExpressionBuilder.BuildExpression<Contact>(rule, entityMetadata);

// 4. Use with EF Core
using var context = new MyDbContext();
var results = context.Contacts.Where(expression).ToList();
```

### From JSON String

```csharp
var jsonString = @"{
  ""Logic"": ""AND"",
  ""Criteria"": [
    {
      ""Data"": ""NationalCode"",
      ""Condition"": ""contains"",
      ""Value"": [""1100""],
      ""FieldType"": ""string""
    }
  ]
}";

var rule = JsonConvert.DeserializeObject<RolesNode>(jsonString);
var expression = DynamicExpressionBuilder.BuildExpression<Contact>(rule, entityMetadata);
```

## JSON Structure Reference

### Basic Condition

```json
{
  "Data": "PropertyName",
  "Condition": "operator",
  "Value": ["value1", "value2"],
  "FieldType": "string|int|boolean|datetime|datetimeshamsi|select|entity|listentity"
}
```

### Logic Group (AND/OR)

```json
{
  "Logic": "AND|OR",
  "Criteria": [
    { /* condition or nested group */ },
    { /* condition or nested group */ }
  ]
}
```

### Nested Entity (SubField)

```json
{
  "Data": "Company",
  "Condition": "=",
  "SubField": {
    "Data": "Name",
    "Condition": "=",
    "Value": ["Acme Corp"],
    "FieldType": "string"
  },
  "FieldType": "entity"
}
```

### Collection Filtering (ListEntity)

```json
{
  "Data": "Addresses",
  "Condition": "any",
  "SubField": {
    "Data": "Type",
    "Condition": "=",
    "Value": ["1"],
    "FieldType": "select"
  },
  "FieldType": "listentity"
}
```

## Operator Examples

### String Operators

```json
// Contains
{ "Data": "Name", "Condition": "contains", "Value": ["John"], "FieldType": "string" }

// Starts with
{ "Data": "Email", "Condition": "starts", "Value": ["admin"], "FieldType": "string" }

// Ends with
{ "Data": "Domain", "Condition": "ends", "Value": [".com"], "FieldType": "string" }
```

### Numeric Operators

```json
// Greater than
{ "Data": "Age", "Condition": ">", "Value": ["18"], "FieldType": "int" }

// Between
{ "Data": "Salary", "Condition": "between", "Value": ["3000", "5000"], "FieldType": "int" }
```

### DateTime Operators

```json
// Persian DateTime (automatically converted)
{
  "Data": "CreatedOn",
  "Condition": "<",
  "Value": ["1404/10/09 10:40:16"],
  "FieldType": "datetimeshamsi"
}

// Regular DateTime
{
  "Data": "ModifiedDate",
  "Condition": ">=",
  "Value": ["2024-01-01T00:00:00"],
  "FieldType": "datetime"
}
```

### Boolean Operators

```json
{ "Data": "IsActive", "Condition": "=", "Value": ["true"], "FieldType": "boolean" }
```

### Null Checks

```json
// Is Null
{ "Data": "DeletedDate", "Condition": "null", "FieldType": "datetime" }

// Is Not Null
{ "Data": "ApprovedDate", "Condition": "!null", "FieldType": "datetime" }
```

## Advanced Scenarios

### Complex AND/OR Logic

```json
{
  "Logic": "AND",
  "Criteria": [
    {
      "Data": "Status",
      "Condition": "=",
      "Value": ["Active"],
      "FieldType": "string"
    },
    {
      "Logic": "OR",
      "Criteria": [
        {
          "Data": "Priority",
          "Condition": "=",
          "Value": ["High"],
          "FieldType": "string"
        },
        {
          "Data": "UrgencyLevel",
          "Condition": ">",
          "Value": ["5"],
          "FieldType": "int"
        }
      ]
    }
  ]
}
```

**Generated Expression:** 
```csharp
x => x.Status == "Active" && (x.Priority == "High" || x.UrgencyLevel > 5)
```

### Deep Nested Navigation

```json
{
  "Data": "Addresses",
  "Condition": "any",
  "SubField": {
    "Data": "City",
    "SubField": {
      "Data": "Name",
      "Condition": "=",
      "Value": ["Tehran"],
      "FieldType": "string"
    },
    "FieldType": "entity"
  },
  "FieldType": "listentity"
}
```

**Generated Expression:**
```csharp
x => x.Addresses.Any(item => item.City.Name == "Tehran")
```

### Collection with All (Multiple Conditions)

```json
{
  "Data": "Orders",
  "Condition": "all",
  "SubField": {
    "Data": "Status",
    "Condition": "=",
    "Value": ["Completed"],
    "FieldType": "string"
  },
  "FieldType": "listentity"
}
```

**Generated Expression:**
```csharp
x => x.Orders.All(item => item.Status == "Completed")
```

## Entity Metadata Setup

The Expression Builder requires `EntityMetadata` to understand field types and navigation paths:

```csharp
var entityMetadata = new EntityMetadata
{
    EntityName = "Contact",
    EntityFullName = "MyApp.Entities.Contact",
    Properties = new List<PropertyMetadata>
    {
        new PropertyMetadata
        {
            Name = "NationalCode",
            DisplayName = "کد ملی",
            SystemType = SystemType.String,
            SearchPath = "NationalCode"
        },
        new PropertyMetadata
        {
            Name = "Addresses",
            DisplayName = "آدرس‌ها",
            SystemType = SystemType.ListEntity,
            SearchPath = "Addresses",
            RelatedEntityTypeFullName = "MyApp.Entities.Address"
        },
        // ... more properties
    }
};
```

## Persian DateTime Support

The Expression Builder automatically converts Persian (Shamsi) dates to Gregorian DateTime:

```json
{
  "Data": "CreatedOn",
  "Condition": "between",
  "Value": ["1403/01/01 00:00:00", "1403/12/29 23:59:59"],
  "FieldType": "datetimeshamsi"
}
```

**Format:** `YYYY/MM/DD HH:mm:ss` (24-hour format)

The conversion uses the existing `DateTimeExtensions.ToMiladiDateTime()` utility.

## Integration with NotificationBuilder

### In NotificationBuilderService

```csharp
public async Task ProcessNotification(long notificationId)
{
    var notification = await GetNotificationById(notificationId);
    var entityMetadata = EntityCache.Get(notification.EntityFullName);
    
    // Parse the stored conditions JSON
    var rule = JsonConvert.DeserializeObject<RolesNode>(notification.ConditionsJson);
    
    // Build the expression
    var expression = DynamicExpressionBuilder.BuildExpression<TEntity>(rule, entityMetadata);
    
    // Query affected entities
    var affectedEntities = await context.Set<TEntity>()
        .Where(expression)
        .ToListAsync();
    
    // Send notifications for each entity
    foreach (var entity in affectedEntities)
    {
        await SendNotification(notification, entity);
    }
}
```

### Complete Notification Rule Example

```csharp
// Notification rule stored in database
var notificationBuilder = new NotifictionBuilder
{
    EntityFullName = "MyApp.Entities.Contact",
    Operation = "Create",
    ConditionsJson = @"{
        ""Logic"": ""AND"",
        ""Criteria"": [
            {
                ""Data"": ""NationalCode"",
                ""Condition"": ""contains"",
                ""Value"": [""1100""],
                ""FieldType"": ""string""
            },
            {
                ""Data"": ""Addresses"",
                ""Condition"": ""any"",
                ""SubField"": {
                    ""Data"": ""Type"",
                    ""Condition"": ""="",
                    ""Value"": [""1""],
                    ""FieldType"": ""select""
                },
                ""FieldType"": ""listentity""
            }
        ]
    }",
    MessageTemplate = "New contact {FirstName} {LastName} was created.",
    MessageTitle = "New Contact Alert",
    UserId = 123
};

// When a new Contact is created, evaluate the rule
var rule = JsonConvert.DeserializeObject<RolesNode>(notificationBuilder.ConditionsJson);
var expression = DynamicExpressionBuilder.BuildExpression<Contact>(rule, entityMetadata);

// Check if the new contact matches the rule
var matches = expression.Compile()(newContact);
if (matches)
{
    // Send notification
    await SendNotification(notificationBuilder, newContact);
}
```

## Error Handling

The Expression Builder is designed to be resilient:

- **Unknown Properties**: Silently skipped (returns `true`)
- **Invalid Values**: Default values used
- **Type Mismatches**: Automatic conversion attempted
- **Null Values**: Handled safely with null checks

For debugging, you can inspect the generated expression:

```csharp
var expression = DynamicExpressionBuilder.BuildExpression<Contact>(rule, entityMetadata);
Console.WriteLine(expression.ToString());
// Output: x => (x.NationalCode != null && x.NationalCode.Contains("1100"))
```

## Performance Considerations

1. **Expression Compilation**: Expressions are compiled by EF Core, not cached by default
2. **Entity Metadata**: Cache your `EntityMetadata` to avoid repeated lookups
3. **Complex Queries**: Deep nesting may generate complex SQL - test query performance
4. **Collection Filtering**: `Any`/`All` operations translate to SQL `EXISTS` - generally efficient

## Limitations

- Maximum recommended nesting depth: 3-4 levels
- Collection operations require indexed properties for optimal performance
- Persian DateTime conversion expects specific format: `YYYY/MM/DD HH:mm:ss`

## Troubleshooting

### Expression Returns No Results

Check:
1. Property names match exactly (case-sensitive)
2. Field types are correct in JSON
3. Values are in the expected format
4. Entity metadata includes all properties

### Type Conversion Errors

Ensure:
1. `FieldType` matches the actual property type
2. Values can be parsed to the target type
3. Persian dates use correct format

### Navigation Property Issues

Verify:
1. `SearchPath` is set correctly in metadata
2. Navigation properties are loaded (use `.Include()` if needed)
3. Related entity metadata is available

## API Reference

### Main Method

```csharp
public static Expression<Func<T, bool>> BuildExpression<T>(
    RolesNode rule, 
    EntityMetadata entityMetadata)
```

**Parameters:**
- `rule`: The root RolesNode containing the rule structure
- `entityMetadata`: Cached metadata for the entity type

**Returns:** EF Core compatible expression tree

**Throws:** 
- `ArgumentNullException`: If rule or entityMetadata is null

## See Also

- `DynamicExpressionBuilder.Examples.cs` - Comprehensive usage examples
- `RolesNode` class definition in `Entities/Base/NotifitactionBuilder/`
- `EntityMetadata` and `PropertyMetadata` in `Common/Entities/EntityMetadatas/`
- `DateTimeExtensions` in `Common/Utilities/` for Persian date conversion

## Version History

- **v1.0** (2024-10-22): Initial implementation
  - All operators supported
  - Nested entity navigation
  - Collection filtering
  - Persian DateTime conversion
  - Complete documentation

---

**Need Help?** Check the examples file or contact the development team.

