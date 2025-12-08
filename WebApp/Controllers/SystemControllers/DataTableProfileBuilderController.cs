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
                    Type = "guid",
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

            // Process filters
            if (model.Filters != null && model.Filters.Any())
            {
                foreach (var col in model.Filters)
                {
                    if (col == null || string.IsNullOrWhiteSpace(col.Name))
                    {
                        continue;
                    }

                    var filter = new SystemDataTableProfileSelectViewModel();
                    var existData = listSelectViewModels.FirstOrDefault(c => c.Name == col.Name);

                    if (existData != null)
                    {
                        // Reuse existing column definition
                        filter.Label = existData.Label;
                        filter.Alliance = existData.Alliance;
                        filter.Name = existData.Name;
                        filter.Title = existData.Title;
                        filter.Level = existData.Level;
                        filter.PropName = existData.Name.Split(".", StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? existData.PropName;
                        filter.Type = existData.Type;
                    }
                    else
                    {
                        // Create new filter definition
                        var splitName = col.Name.Split(".", StringSplitOptions.RemoveEmptyEntries);
                        if (splitName.Length == 0)
                        {
                            continue;
                        }

                        var propName = splitName[0];
                        var prop = properties.FirstOrDefault(c => c.Name == propName);

                        if (prop == null)
                        {
                            continue;
                        }

                        var propType = prop.DataType ?? "string";

                        if (propType == "entity" && splitName.Length > 1)
                        {
                            filter = CreateJoinString(
                                prop,
                                col.Name,
                                0,
                                "m0",
                                ref listSelectViewModels,
                                ref joinCounter,
                                ref existingJoins,
                                tableSchema);
                            filter.showTitle = col.ShowTitle;
                        }
                        else
                        {
                            filter.PropName = propName;
                            filter.Type = propType;
                            filter.Label = "m0";
                            filter.TableName = tableName;
                            filter.Name = col.Name;
                            filter.showTitle = col.ShowTitle;
                        }
                    }

                    filter.Criteria = col.Criteria;
                    filter.Filter = true;
                    filter.Value = col.Value ?? new List<string>();
                    listSelectViewModels.Add(filter);
                }
            }

            // Build filter strings (with SQL injection protection)
            foreach (var part in listSelectViewModels.Where(c => c.Filter))
            {
                part.FilterString = BuildFilterString(part);
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

            // Build default filter query
            var defaultFilter = string.Join(" AND ",
                listSelectViewModels
                    .Where(c => c.Filter && !string.IsNullOrWhiteSpace(c.FilterString))
                    .Select(c => c.FilterString));

            // Prepare entity data
            var columnsJson = listSelectViewModels
                .Where(c => !string.IsNullOrWhiteSpace(c.Alliance))
                .JsonSerialize() ?? "[]";

            var filtersJson = listSelectViewModels
                .Where(c => c.Filter)
                .JsonSerialize() ?? "[]";

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

                default:
                    throw new ArgumentException($"Unsupported operator: {part.Criteria}");
            }
        }




        public class SaveSystemDataTableProfileViewModel
        {
            public long? Id { get; set; }
            public required string EntityName { get; set; }
            public required string Title { get; set; }
            public required List<SaveSystemDataTableProfileColsViewModel> Columns { get; set; }
            public List<SaveSystemDataTableProfileFilterViewModel>? Filters { get; set; }
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

        public class SaveSystemDataTableProfileFilterViewModel
        {
            public required string Name { get; set; }
            public List<string> Value { get; set; } = new List<string>();
            public required string Type { get; set; }
            public required string Criteria { get; set; }
            public string ShowTitle { get; set; } = string.Empty;
        }
    }
}
