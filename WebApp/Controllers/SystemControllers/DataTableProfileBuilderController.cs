using Common.Attributes;
using Common.Auth.Enums;
using Common.Entities.EntityMetadatas;
using Common.Utilities;

using Data.Contracts;
using Data.Repositories;
using Entities.Base;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.InMemoryData;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.SystemControllers
{
    [ApiController]
    [ApiResultFilter]
    [Route("[controller]")]
    [Authorize("AuthenticatedUser")]
    [ControllerInfoAttribute("نمایه داده")]
    public class DataTableProfileBuilderController(IUnitOfWork unitOfWork,
        IDataTableProfileService dataTableProfileService, IEntityMetadataCache entityMetadataCache) : BaseController
    {
        [ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View)]
        [HttpGet("[action]")]
        public IActionResult Edit(long id)
        {
            var model = unitOfWork.Repository<SystemDataTableProfile>().TableNoTracking
                .FirstOrDefault(c => c.Id == id);

            if (model != null)
            {
                model.SystemDataTableProfileSelectViewModels =
                    model.Columns?.JsonDeserialize<List<SystemDataTableProfileSelectViewModel>>() ?? new List<SystemDataTableProfileSelectViewModel>();

                model.SystemDataTableProfileFilterViewModels =
                    model.Filters?.JsonDeserialize<List<SystemDataTableProfileSelectViewModel>>() ?? new List<SystemDataTableProfileSelectViewModel>();
            }

            return View("Views/Panel/System/DataTableProfileBuilder/Edit.cshtml", model);
        }


        [ActionDisplayName("جدید", ActionAccessType.View)]
        [HttpGet("[action]")]
        public IActionResult New(string entityName)
        {
            ViewBag.entityName = entityName;

            return View("Views/Panel/System/DataTableProfileBuilder/Edit.cshtml");
        }

        [ActionDisplayName("لیست اطلاعات", ActionAccessType.View)]
        [HttpGet("[action]")]
        public IActionResult List()
        {
            return View("Views/Panel/System/DataTableProfileBuilder/List.cshtml");
        }

        [HttpGet("[action]")]
        public IActionResult GetEntityProp(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Entity name cannot be null or empty.", nameof(name));
            }

            var listCols = new List<ColumnTable>();

            var entityType = entityMetadataCache.Get(name);

            if (entityType == null)
            {
                throw new ArgumentException($"Invalid Entity name: {name}", nameof(name));
            }

            var properties = entityType.Properties;


            var tableSchema = entityType.Schema;

            var typeDisplayname = entityType.DisplayName;

            var entityTitle = typeDisplayname != null ? typeDisplayname : entityType.EntityName;

            if (entityType != null)
            {



                // Generate table headers from property names
                foreach (var prop in properties)
                {
                    var tmpColumnTable = new ColumnTable();

                    if (prop == null)
                    {
                        continue;
                    }

                    string displayName = !string.IsNullOrWhiteSpace(prop.DisplayName) ? prop.DisplayName : prop.Name;
                    var addToTable = prop.AddToTable;

                    string searchPath = prop.SearchPath ?? prop.Name;
                    string typeProperty = prop.Type ?? "string";
                    var showInRelationData = prop.ShowInRelationData;
                    string dataName = prop.Name;

                    tmpColumnTable.name = dataName;


                    if (prop.DataType == "entity")
                    {



                        tmpColumnTable.tableName = typeProperty;

                        if (prop.RelatedEntity == null)
                        {

                            searchPath = prop.Name + ".Id";
                        }
                        else
                        {
                            dataName = dataName + "" + prop.Name;
                            searchPath = prop.Name + "." + prop.Name;
                        }

                        tmpColumnTable.name = searchPath;

                        tmpColumnTable.RelatedEntityTypeFullName = prop.RelatedEntityTypeFullName;


                    }
                    else if (typeProperty == "select")
                    {
                        //var enumType = prop.PropertyType;

                    }


                    tmpColumnTable.title = displayName;
                    tmpColumnTable.data = searchPath;

                    tmpColumnTable.type = prop.DataType;


                    if (prop.DataType != "listentity")
                        listCols.Add(tmpColumnTable);



                }


            }

            return Ok(new { columns = listCols, title = entityTitle });
        }

        /// <summary>
        /// Returns entity properties in the format expected by QueryBuilder
        /// </summary>
        [HttpGet("[action]")]
        public IActionResult GetEntityPropForQueryBuilder(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Entity name cannot be null or empty.", nameof(name));
            }

            var entityType = entityMetadataCache.Get(name);

            if (entityType == null)
            {
                throw new ArgumentException($"Invalid Entity name: {name}", nameof(name));
            }

            

            return Ok(entityType);
        }


        [HttpPost("[action]")]
        public IActionResult Save(SaveSystemDataTableProfileViewModel model, CancellationToken cancellationToken)
        {
            // Input validation
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (string.IsNullOrWhiteSpace(model.EntityName))
            {
                throw new ArgumentException("Entity name cannot be null or empty.", nameof(model.EntityName));
            }

            if (string.IsNullOrWhiteSpace(model.Title))
            {
                throw new ArgumentException("Title cannot be null or empty.", nameof(model.Title));
            }

            if (model.Columns == null || !model.Columns.Any())
            {
                throw new ArgumentException("Columns cannot be null or empty.", nameof(model.Columns));
            }

            var entityType = entityMetadataCache.Get(model.EntityName);

            if (entityType == null)
            {
                throw new ArgumentException($"Invalid Entity name: {model.EntityName}", nameof(model.EntityName));
            }

            var tableName = entityType.TabelName;
            var tableSchema = entityType.Schema;
            var properties = entityType.Properties ?? new List<PropertyMetadata>();

            // Local variables instead of instance fields for thread safety
            var listSelectViewModels = new List<SystemDataTableProfileSelectViewModel>();
            var joinCounter = 0;
            var existingJoins = new Dictionary<string, SystemDataTableProfileSelectViewModel>();
            // Ensure Id column exists
            if (!model.Columns.Any(c => c.Name == "Id"))
            {
                model.Columns.Insert(0, new SaveSystemDataTableProfileColsViewModel
                {
                    Type = "long",
                    Name = "Id",
                    Order = "",
                    ShowTitle = "شناسه",
                    Title = "شناسه",
                    Visible = true
                });
            }

            // Process columns
            foreach (var col in model.Columns)
            {
                if (col == null || string.IsNullOrWhiteSpace(col.Name))
                {
                    continue;
                }

                var splitName = col.Name.Split(".", StringSplitOptions.RemoveEmptyEntries);
                if (splitName.Length == 0)
                {
                    throw new ArgumentException($"Invalid column name: {col.Name}");
                }

                var propName = splitName[0];
                var prop = properties.FirstOrDefault(c => c.Name == propName);

                if (prop == null)
                {
                    throw new ArgumentException($"ستون با نام '{propName}' در موجودیت '{model.EntityName}' یافت نشد.");
                }

                SystemDataTableProfileSelectViewModel select;

                var propType = col.Type == "button" ? "button" : (prop.DataType ?? "string");

                if (propType == "entity" && splitName.Length > 1)
                {
                    // Handle entity relationships with joins
                    select = CreateJoinString(
                        prop,
                        col.Name,
                        0,
                        "m0",
                        ref listSelectViewModels,
                        ref joinCounter,
                        ref existingJoins,
                        tableSchema);
                    select.Title = col.Title ?? prop.DisplayName ?? prop.Name;
                }
                else
                {
                    // Simple column without join
                    select = new SystemDataTableProfileSelectViewModel
                    {
                        PropName = propName,
                        Type = propType,
                        Label = "m0",
                        TableName = tableName ?? string.Empty,
                        Title = col.Title ?? prop.DisplayName ?? prop.Name,
                        Name = col.Name,
                        Render = col.Render ?? string.Empty,
                        showTitle = col.ShowTitle ?? string.Empty,
                        Visible = col.Visible ?? true
                    };
                }

                listSelectViewModels.Add(select);
            }

            // Assign alliance (column aliases) to non-join columns
            var columnIndex = 0;
            foreach (var selectViewModel in listSelectViewModels.Where(c => string.IsNullOrWhiteSpace(c.JoinString)))
            {
                selectViewModel.Alliance = selectViewModel.PropName == "Id"
                    ? "id"
                    : $"c{columnIndex++}";
            }

            // Build SELECT query
            var selectQuery = string.Join(", ",
                listSelectViewModels
                    .Where(c => !string.IsNullOrWhiteSpace(c.Alliance))
                    .Select(z => $"[{z.Label}].[{z.PropName}] {z.Alliance}"));

            // Process filters from QueryBuilder format
            var filterViewModels = new List<SystemDataTableProfileSelectViewModel>();
            string defaultFilter = string.Empty;

            if (model.Filters != null && model.Filters.Criteria != null && model.Filters.Criteria.Any())
            {
                // Extract filter fields and build joins
                ExtractFilterFields(
                    model.Filters,
                    properties,
                    tableName,
                    tableSchema,
                    ref listSelectViewModels,
                    ref filterViewModels,
                    ref joinCounter,
                    ref existingJoins);

                // Build SQL filter string from QueryBuilder rules
                defaultFilter = BuildQueryBuilderFilterString(model.Filters, filterViewModels);
            }

            // Build FROM query with joins
            var joinStrings = listSelectViewModels
                .Where(c => !string.IsNullOrWhiteSpace(c.JoinString))
                .Select(z => z.JoinString)
                .Distinct()
                .ToList();

            var fromQuery = $"FROM [{tableSchema}].[{tableName}] m0";
            if (joinStrings.Any())
            {
                fromQuery += "\n" + string.Join("\n", joinStrings);
            }

            // Prepare entity data
            var columnsJson = listSelectViewModels
                .Where(c => !string.IsNullOrWhiteSpace(c.Alliance))
                .JsonSerialize() ?? "[]";

            // Store QueryBuilder filter structure as JSON
            var filtersJson = model.Filters?.JsonSerialize() ?? "null";

            var entity = new SystemDataTableProfile
            {
                Columns = columnsJson,
                Filters = filtersJson,
                SelectQuery = selectQuery,
                FromQuery = fromQuery,
                FilterQuery = defaultFilter,
                Title = model.Title,
                EntityName = entityType.EntityFullName,
                EntitySchema = tableSchema
            };

            if (model.Id.HasValue && model.Id.Value > 0)
            {
                entity.Id = model.Id.Value;
                entity = unitOfWork.Repository<SystemDataTableProfile>().Update(entity);
            }
            else
            {
                entity = unitOfWork.Repository<SystemDataTableProfile>().Add(entity);
            }

            dataTableProfileService.SaveDataProfile(entity);

            return Ok();
        }

        /// <summary>
        /// Creates join string recursively for entity relationships
        /// </summary>
        private SystemDataTableProfileSelectViewModel CreateJoinString(
            PropertyMetadata prop,
            string name,
            int index,
            string parentLabel,
            ref List<SystemDataTableProfileSelectViewModel> listSelectViewModels,
            ref int joinCounter,
            ref Dictionary<string, SystemDataTableProfileSelectViewModel> existingJoins,
            string baseTableSchema)
        {
            if (prop == null)
            {
                throw new ArgumentNullException(nameof(prop));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Name cannot be null or empty.", nameof(name));
            }

            var splitName = name.Split(".", StringSplitOptions.RemoveEmptyEntries);
            if (index >= splitName.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index exceeds split name length.");
            }

            var select = new SystemDataTableProfileSelectViewModel
            {
                PropName = splitName[index],
                Level = index,
                Name = name,
                Type = prop.Type
            };

            // Check if we need to create a join (entity or list entity)
            if (prop.SystemType == SystemType.Entity || prop.SystemType == SystemType.ListEntity)
            {
                if (string.IsNullOrWhiteSpace(prop.RelatedEntityTypeFullName))
                {
                    throw new InvalidOperationException($"RelatedEntityTypeFullName is null for property '{prop.Name}'");
                }

                var propEntity = entityMetadataCache.Get(prop.RelatedEntityTypeFullName);
                if (propEntity == null)
                {
                    throw new InvalidOperationException($"Entity metadata not found for '{prop.RelatedEntityTypeFullName}'");
                }

                select.TableName = propEntity.TabelName;
                select.Type = propEntity.Type ?? "string";

                // Create a unique key for join identification
                // Use table name + property name + level to identify unique joins
                var joinKey = $"{propEntity.TabelName}_{prop.Name}_{index}";

                // Check if this join already exists
                if (existingJoins.TryGetValue(joinKey, out var existingJoin))
                {
                    select.Label = existingJoin.Label;
                    select.JoinString = existingJoin.JoinString;
                }
                else
                {
                    // Create new join
                    select.Label = $"t{joinCounter++}";

                    // Foreign key name should be based on the property name, not table name
                    var foreignKeyName = $"{prop.Name}Id";
                    var tableSchema = propEntity.Schema ?? baseTableSchema;

                    select.ForgesKeyName = foreignKeyName;
                    select.JoinString = $"LEFT JOIN [{tableSchema}].[{select.TableName}] AS {select.Label} ON [{parentLabel}].[{foreignKeyName}] = [{select.Label}].[Id]";

                    // Store join for reuse
                    existingJoins[joinKey] = select;

                    // Add to list only if not already added
                    if (!listSelectViewModels.Any(j => j.JoinString == select.JoinString && j.Label == select.Label))
                    {
                        listSelectViewModels.Add(select);
                    }
                }

                // Continue recursion if there are more parts in the path
                if (index + 1 < splitName.Length)
                {
                    var nextPropName = splitName[index + 1];
                    var nextProp = propEntity.Properties?.FirstOrDefault(c => c.Name == nextPropName);

                    if (nextProp == null)
                    {
                        throw new InvalidOperationException($"Property '{nextPropName}' not found in entity '{propEntity.EntityName}'");
                    }

                    return CreateJoinString(
                        nextProp,
                        name,
                        index + 1,
                        select.Label ?? parentLabel,
                        ref listSelectViewModels,
                        ref joinCounter,
                        ref existingJoins,
                        baseTableSchema);
                }
            }
            else
            {
                // Non-entity property - use parent label
                select.Label = parentLabel;
                var lastJoin = listSelectViewModels.LastOrDefault(j => !string.IsNullOrWhiteSpace(j.JoinString));
                select.TableName = lastJoin?.TableName ?? select.TableName;
            }

            return select;
        }

        /// <summary>
        /// Builds filter string with SQL injection protection
        /// </summary>
        private string BuildFilterString(SystemDataTableProfileSelectViewModel part)
        {
            if (part == null || string.IsNullOrWhiteSpace(part.Criteria))
            {
                return string.Empty;
            }

            var label = part.Label ?? "m0";
            var propName = part.PropName ?? string.Empty;

            // Sanitize values to prevent SQL injection
            var sanitizeValue = new Func<string, string>(value =>
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return string.Empty;
                }
                // Remove single quotes and escape them
                return value.Replace("'", "''");
            });

            switch (part.Criteria)
            {
                case "=":
                    if (part.Value == null || !part.Value.Any())
                    {
                        return string.Empty;
                    }
                    var eqValue = sanitizeValue(part.Value[0]);
                    return $"[{label}].[{propName}] = N'{eqValue}'";

                case "!=":
                    if (part.Value == null || !part.Value.Any())
                    {
                        return string.Empty;
                    }
                    var neValue = sanitizeValue(part.Value[0]);
                    return $"[{label}].[{propName}] <> N'{neValue}'";

                case ">":
                    if (part.Value == null || !part.Value.Any())
                    {
                        return string.Empty;
                    }
                    var gtValue = sanitizeValue(part.Value[0]);
                    return $"[{label}].[{propName}] > N'{gtValue}'";

                case ">=":
                    if (part.Value == null || !part.Value.Any())
                    {
                        return string.Empty;
                    }
                    var gteValue = sanitizeValue(part.Value[0]);
                    return $"[{label}].[{propName}] >= N'{gteValue}'";

                case "<":
                    if (part.Value == null || !part.Value.Any())
                    {
                        return string.Empty;
                    }
                    var ltValue = sanitizeValue(part.Value[0]);
                    return $"[{label}].[{propName}] < N'{ltValue}'";

                case "<=":
                    if (part.Value == null || !part.Value.Any())
                    {
                        return string.Empty;
                    }
                    var lteValue = sanitizeValue(part.Value[0]);
                    return $"[{label}].[{propName}] <= N'{lteValue}'";

                case "contains":
                    if (part.Value == null || !part.Value.Any())
                    {
                        return string.Empty;
                    }
                    var containsValue = sanitizeValue(part.Value[0]);
                    // Handle list types (listlong, liststring) - check if JSON array contains value
                    if (part.Type == "listlong" || part.Type == "liststring")
                    {
                        return $"EXISTS (SELECT value FROM OPENJSON([{label}].[{propName}]) WHERE value = N'{containsValue}')";
                    }
                    return $"[{label}].[{propName}] LIKE N'%{containsValue}%'";

                case "!contains":
                    if (part.Value == null || !part.Value.Any())
                    {
                        return string.Empty;
                    }
                    var notContainsValue = sanitizeValue(part.Value[0]);
                    return $"[{label}].[{propName}] NOT LIKE N'%{notContainsValue}%'";

                case "starts":
                    if (part.Value == null || !part.Value.Any())
                    {
                        return string.Empty;
                    }
                    var startsValue = sanitizeValue(part.Value[0]);
                    return $"[{label}].[{propName}] LIKE N'{startsValue}%'";

                case "!starts":
                    if (part.Value == null || !part.Value.Any())
                    {
                        return string.Empty;
                    }
                    var notStartsValue = sanitizeValue(part.Value[0]);
                    return $"[{label}].[{propName}] NOT LIKE N'{notStartsValue}%'";

                case "ends":
                    if (part.Value == null || !part.Value.Any())
                    {
                        return string.Empty;
                    }
                    var endsValue = sanitizeValue(part.Value[0]);
                    return $"[{label}].[{propName}] LIKE N'%{endsValue}'";

                case "!ends":
                    if (part.Value == null || !part.Value.Any())
                    {
                        return string.Empty;
                    }
                    var notEndsValue = sanitizeValue(part.Value[0]);
                    return $"[{label}].[{propName}] NOT LIKE N'%{notEndsValue}'";

                case "IN":
                    if (part.Value == null || !part.Value.Any())
                    {
                        return string.Empty;
                    }
                    var inValues = string.Join(",", part.Value.Select(v => $"N'{sanitizeValue(v)}'"));
                    return $"[{label}].[{propName}] IN ({inValues})";

                case "NOT IN":
                    if (part.Value == null || !part.Value.Any())
                    {
                        return string.Empty;
                    }
                    var notInValues = string.Join(",", part.Value.Select(v => $"N'{sanitizeValue(v)}'"));
                    return $"[{label}].[{propName}] NOT IN ({notInValues})";

                case "null":
                    return $"[{label}].[{propName}] IS NULL";

                case "!null":
                    return $"[{label}].[{propName}] IS NOT NULL";

                case "between":
                    if (part.Value == null || part.Value.Count < 2)
                    {
                        return string.Empty;
                    }
                    var betweenFrom = sanitizeValue(part.Value[0]);
                    var betweenTo = sanitizeValue(part.Value[1]);
                    return $"[{label}].[{propName}] BETWEEN N'{betweenFrom}' AND N'{betweenTo}'";

                case "!between":
                    if (part.Value == null || part.Value.Count < 2)
                    {
                        return string.Empty;
                    }
                    var notBetweenFrom = sanitizeValue(part.Value[0]);
                    var notBetweenTo = sanitizeValue(part.Value[1]);
                    return $"[{label}].[{propName}] NOT BETWEEN N'{notBetweenFrom}' AND N'{notBetweenTo}'";

                case "currentUser.Id":
                    return $"[{label}].[{propName}] = N'{{currentUser.Id}}'";

                // List operators (for listlong, liststring field types)
                // Note: These generate SQL for JSON arrays stored in database
                case "containsany":
                    if (part.Value == null || !part.Value.Any())
                    {
                        return string.Empty;
                    }
                    // For SQL Server JSON array: check if any value exists
                    var anyValues = string.Join(",", part.Value.Select(v => $"N'{sanitizeValue(v)}'"));
                    return $"EXISTS (SELECT value FROM OPENJSON([{label}].[{propName}]) WHERE value IN ({anyValues}))";

                case "containsall":
                    if (part.Value == null || !part.Value.Any())
                    {
                        return string.Empty;
                    }
                    // For SQL Server JSON array: check if all values exist
                    var allConditions = part.Value.Select(v => 
                        $"EXISTS (SELECT value FROM OPENJSON([{label}].[{propName}]) WHERE value = N'{sanitizeValue(v)}')"
                    );
                    return $"({string.Join(" AND ", allConditions)})";

                default:
                    throw new ArgumentException($"Unsupported operator: {part.Criteria}");
            }
        }

        /// <summary>
        /// Extracts filter fields from QueryBuilder structure and creates necessary joins
        /// </summary>
        private void ExtractFilterFields(
            QueryBuilderFilterGroup group,
            List<PropertyMetadata> properties,
            string tableName,
            string tableSchema,
            ref List<SystemDataTableProfileSelectViewModel> listSelectViewModels,
            ref List<SystemDataTableProfileSelectViewModel> filterViewModels,
            ref int joinCounter,
            ref Dictionary<string, SystemDataTableProfileSelectViewModel> existingJoins)
        {
            if (group?.Criteria == null) return;

            foreach (var item in group.Criteria)
            {
                if (item.IsGroup)
                {
                    // Recursively process nested group
                    var nestedGroup = new QueryBuilderFilterGroup
                    {
                        Logic = item.Logic ?? "AND",
                        Criteria = item.Criteria
                    };
                    ExtractFilterFields(nestedGroup, properties, tableName, tableSchema,
                        ref listSelectViewModels, ref filterViewModels, ref joinCounter, ref existingJoins);
                }
                else if (!string.IsNullOrWhiteSpace(item.Data))
                {
                    // Process filter condition
                    var fieldName = item.Data;
                    
                    // Handle subField for entity types
                    var fullPath = fieldName;
                    if (item.SubField != null)
                    {
                        fullPath = BuildSubFieldPath(fieldName, item.SubField);
                    }

                    var filter = CreateFilterViewModel(
                        fullPath,
                        properties,
                        tableName,
                        tableSchema,
                        ref listSelectViewModels,
                        ref joinCounter,
                        ref existingJoins);

                    if (filter != null)
                    {
                        filter.Criteria = item.Condition;
                        filter.Filter = true;
                        filter.Value = item.Value ?? new List<string>();
                        filter.Type = item.FieldType ?? filter.Type ?? "string";
                        filterViewModels.Add(filter);
                    }
                }
            }
        }

        /// <summary>
        /// Builds the full field path from subField structure
        /// </summary>
        private string BuildSubFieldPath(string basePath, object subField)
        {
            if (subField == null) return basePath;

            // subField can be a string or an object with nested data
            if (subField is string subFieldStr)
            {
                return $"{basePath}.{subFieldStr}";
            }

            if (subField is System.Text.Json.JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    return $"{basePath}.{jsonElement.GetString()}";
                }
                else if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    if (jsonElement.TryGetProperty("data", out var dataElement))
                    {
                        var subPath = dataElement.GetString();
                        basePath = $"{basePath}.{subPath}";
                        
                        if (jsonElement.TryGetProperty("subField", out var nestedSubField))
                        {
                            return BuildSubFieldPath(basePath, nestedSubField);
                        }
                    }
                }
            }

            return basePath;
        }

        /// <summary>
        /// Creates a filter view model for a field path
        /// </summary>
        private SystemDataTableProfileSelectViewModel? CreateFilterViewModel(
            string fullPath,
            List<PropertyMetadata> properties,
            string tableName,
            string tableSchema,
            ref List<SystemDataTableProfileSelectViewModel> listSelectViewModels,
            ref int joinCounter,
            ref Dictionary<string, SystemDataTableProfileSelectViewModel> existingJoins)
        {
            if (string.IsNullOrWhiteSpace(fullPath)) return null;

            var splitName = fullPath.Split(".", StringSplitOptions.RemoveEmptyEntries);
            if (splitName.Length == 0) return null;

            var propName = splitName[0];
            var prop = properties.FirstOrDefault(c => c.Name == propName);

            if (prop == null) return null;

            // Check if filter already exists in select list
            var existData = listSelectViewModels.FirstOrDefault(c => c.Name == fullPath);

            if (existData != null)
            {
                return new SystemDataTableProfileSelectViewModel
                {
                    Label = existData.Label,
                    Alliance = existData.Alliance,
                    Name = existData.Name,
                    Title = existData.Title,
                    Level = existData.Level,
                    PropName = splitName.LastOrDefault() ?? existData.PropName,
                    Type = existData.Type
                };
            }

            var propType = prop.DataType ?? "string";

            if (propType == "entity" && splitName.Length > 1)
            {
                var filter = CreateJoinString(
                    prop,
                    fullPath,
                    0,
                    "m0",
                    ref listSelectViewModels,
                    ref joinCounter,
                    ref existingJoins,
                    tableSchema);
                filter.Name = fullPath;
                return filter;
            }

            return new SystemDataTableProfileSelectViewModel
            {
                PropName = propName,
                Type = propType,
                Label = "m0",
                TableName = tableName,
                Name = fullPath
            };
        }

        /// <summary>
        /// Builds SQL filter string from QueryBuilder rules with proper grouping
        /// </summary>
        private string BuildQueryBuilderFilterString(QueryBuilderFilterGroup group, List<SystemDataTableProfileSelectViewModel> filterViewModels)
        {
            if (group?.Criteria == null || !group.Criteria.Any()) return string.Empty;

            var filterIndex = 0;
            return BuildFilterGroupString(group, filterViewModels, ref filterIndex);
        }

        private string BuildFilterGroupString(QueryBuilderFilterGroup group, List<SystemDataTableProfileSelectViewModel> filterViewModels, ref int filterIndex)
        {
            if (group?.Criteria == null || !group.Criteria.Any()) return string.Empty;

            var parts = new List<string>();
            var logic = group.Logic?.ToUpper() == "OR" ? " OR " : " AND ";

            foreach (var item in group.Criteria)
            {
                if (item.IsGroup)
                {
                    var nestedGroup = new QueryBuilderFilterGroup
                    {
                        Logic = item.Logic ?? "AND",
                        Criteria = item.Criteria
                    };
                    var nestedFilter = BuildFilterGroupString(nestedGroup, filterViewModels, ref filterIndex);
                    if (!string.IsNullOrWhiteSpace(nestedFilter))
                    {
                        parts.Add($"({nestedFilter})");
                    }
                }
                else if (!string.IsNullOrWhiteSpace(item.Data) && !string.IsNullOrWhiteSpace(item.Condition))
                {
                    if (filterIndex < filterViewModels.Count)
                    {
                        var viewModel = filterViewModels[filterIndex];
                        viewModel.Criteria = item.Condition;
                        viewModel.Value = item.Value ?? new List<string>();
                        
                        var filterStr = BuildFilterString(viewModel);
                        if (!string.IsNullOrWhiteSpace(filterStr))
                        {
                            parts.Add(filterStr);
                        }
                    }
                    filterIndex++;
                }
            }

            return string.Join(logic, parts);
        }

        public class SaveSystemDataTableProfileViewModel
        {
            public long? Id { get; set; }
            public required string EntityName { get; set; }
            public required string Title { get; set; }
            public required List<SaveSystemDataTableProfileColsViewModel> Columns { get; set; }
            public QueryBuilderFilterGroup? Filters { get; set; }
        }

        public class SaveSystemDataTableProfileColsViewModel
        {
            public string Name { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Order { get; set; } = string.Empty;
            public string Type { get; set; } = "string";
            public string ShowTitle { get; set; } = string.Empty;
            public string? Render { get; set; }
            public bool? Visible { get; set; } = true;
        }

        /// <summary>
        /// QueryBuilder filter group (supports nested groups with AND/OR logic)
        /// </summary>
        public class QueryBuilderFilterGroup
        {
            public string Logic { get; set; } = "AND";
            public List<QueryBuilderFilterItem>? Criteria { get; set; }
        }

        /// <summary>
        /// QueryBuilder filter item (can be a condition or a nested group)
        /// </summary>
        public class QueryBuilderFilterItem
        {
            // For condition
            public string? Data { get; set; }
            public string? Condition { get; set; }
            public List<string>? Value { get; set; }
            public string? FieldType { get; set; }
            public object? SubField { get; set; }

            // For nested group
            public string? Logic { get; set; }
            public List<QueryBuilderFilterItem>? Criteria { get; set; }

            public bool IsGroup => !string.IsNullOrEmpty(Logic) && Criteria != null;
        }
    }
}
