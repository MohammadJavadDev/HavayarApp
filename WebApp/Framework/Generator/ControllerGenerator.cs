using Common.Attributes;
using Common.Utilities;
using Entities.Base;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using System.Linq;
using Entities.Services;

namespace WebApp.Framework.Generator
{
    public class ControllerGenerator()
    {
        private string viewFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Views");

        private string controllerFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Controllers");
        private string entityName { get; set; }
        private string prefixPath { get; set; }
        private string module { get; set; }
        private string listPath { get; set; }
        private string editPath { get; set; }
        private string layout { get; set; } = "/Views/Panel/Shared/_PanelLayout.cshtml";
        private Type entityType;

        public StringBuilder AddToEndController { get; set; } = new StringBuilder();
        public StringBuilder addToEditActionController { get; set; } = new StringBuilder();
        public StringBuilder addToEndScripts { get; set; } = new StringBuilder();
        public StringBuilder addToSaveScripts { get; set; } = new StringBuilder();

        // Store ListEntity properties for tab manager generation
        private List<(PropertyInfo prop, Type itemType, string displayName, string nameProperty)> listEntityProperties = new List<(PropertyInfo, Type, string, string)>();

        private readonly IEntityMetadataCache _entityMetadataCache;


        public void GenerateEntity(Type modelType, string _prefixPath = "Panel", string _layout = "/Views/Panel/Shared/_PanelLayout.cshtml")
        {

            prefixPath = _prefixPath;
            layout = _layout;
            entityType = modelType;
            entityName = modelType.Name;

            module = entityType.Namespace.Split(".").Last();

            // Reset listEntityProperties for each entity generation
            listEntityProperties.Clear();

            listPath = GenerateListView();
            editPath = GenerateEditView();


            GenerateController();

        }

        public void GenerateController()
        {
            // Define the controller name and file path
            string controllerName = entityName + "Controller.cs";



            string ControllerFolder = Path.Combine(controllerFolderPath, "Dynamic", module);
            if (!Directory.Exists(ControllerFolder))
            {
                Directory.CreateDirectory(ControllerFolder);
            }


            string path = Path.Combine(controllerFolderPath, "Dynamic", module, controllerName);


            var typeDisplayname = entityType.GetCustomAttribute<DisplayAttribute>();

            var entityTitle = typeDisplayname != null ? typeDisplayname?.Name : entityType.Name;

            // Generate the controller code using a string builder
            var controllerCode = new StringBuilder();

            controllerCode.AppendLine($"using Common.Attributes;\r\nusing Common.Auth.Enums;\r\nusing Data.Contracts;\r\nusing Data.SystemAuth;\r\nusing Microsoft.AspNetCore.Mvc;\r\nusing Microsoft.EntityFrameworkCore;\r\nusing Entities.Base.DataTable;\r\nusing WebFramework.Filtters;\r\nusing WebFramework.Page;"); // You can adjust the namespace as needed
            controllerCode.AppendLine($"using {entityType.Namespace};");
            controllerCode.AppendLine("");
            controllerCode.AppendLine($"namespace WebApp.Controllers.Dynamic");
            controllerCode.AppendLine("{");
            controllerCode.AppendLine($"    [Route(\"{prefixPath}/[controller]\")]");
            controllerCode.AppendLine($"    [ApiController]");
            controllerCode.AppendLine($"    [ApiResultFilter]");
            controllerCode.AppendLine($"    [ControllerInfo(\"{entityTitle}\", typeof({entityName}))]");
            controllerCode.AppendLine($"    public class {entityName}Controller(IUnitOfWork unitOfWork  , IPropertyIdentityService identityService, IWebHostEnvironment _webHostEnvironment) : BaseController");
            controllerCode.AppendLine("    {");

            // Save method
            controllerCode.AppendLine("        [HttpPost(\"[action]\")]");
            controllerCode.AppendLine("        [ActionDisplayName(\"ذخیره\", ActionAccessType.Api, ActionAccessItemType.Save)]");
            controllerCode.AppendLine($"        public async Task<IActionResult> Save({entityName} {entityName.ToLower()} , CancellationToken cn)");
            controllerCode.AppendLine("        {");
            controllerCode.AppendLine("            // Save logic here");
            controllerCode.AppendLine($"           if({entityName.ToLower()}.Id == null || {entityName.ToLower()}.Id == 0)");
            controllerCode.AppendLine("        {");
            controllerCode.AppendLine($"         return await  Add({entityName.ToLower()}, cn);");
            controllerCode.AppendLine("        }");
            controllerCode.AppendLine($"         var exist = await unitOfWork.Repository<{entityName}>().TableNoTracking.AnyAsync(c => c.Id == {entityName.ToLower()}.Id);");

            controllerCode.AppendLine("           if(exist)");
            controllerCode.AppendLine("           {");
            controllerCode.AppendLine($"         return await  Update({entityName.ToLower()}, cn);");
            controllerCode.AppendLine("           }");
            controllerCode.AppendLine($"              return await  Add({entityName.ToLower()}, cn);");
            controllerCode.AppendLine("        }");
            controllerCode.AppendLine("");

            // Add method
            controllerCode.AppendLine("        [HttpPost(\"[action]\")]");
            controllerCode.AppendLine("        [ActionDisplayName(\"درج\", ActionAccessType.Api, ActionAccessItemType.Create)]");
            controllerCode.AppendLine($"        public async Task<IActionResult> Add({entityName} {entityName.ToLower()} , CancellationToken cn)");
            controllerCode.AppendLine("        {");
            controllerCode.AppendLine("            // Add logic here");
            controllerCode.AppendLine($"           var entity = await unitOfWork.Repository<{entityName}>().SaveAsync({entityName.ToLower()} ,cn,true);");
            controllerCode.AppendLine("            return Ok(entity);");
            controllerCode.AppendLine("        }");
            controllerCode.AppendLine("");
            // Update method
            controllerCode.AppendLine("        [HttpPost(\"[action]\")]");
            controllerCode.AppendLine("        [ActionDisplayName(\"ویرایش\", ActionAccessType.Api, ActionAccessItemType.Update)]");
            controllerCode.AppendLine($"        public async Task<IActionResult> Update({entityName} {entityName.ToLower()} , CancellationToken cn)");
            controllerCode.AppendLine("        {");
            controllerCode.AppendLine("            // Update logic here");
            controllerCode.AppendLine($"           var entity = await unitOfWork.Repository<{entityName}>().UpdateAsync({entityName.ToLower()}, cn, true);");
            controllerCode.AppendLine("            return Ok(entity);");
            controllerCode.AppendLine("        }");
            controllerCode.AppendLine("");
            // Delete method
            controllerCode.AppendLine("        [HttpGet(\"[action]\")]");
            controllerCode.AppendLine("        [ActionDisplayName(\"حذف\", ActionAccessType.Api, ActionAccessItemType.Delete)]");
            controllerCode.AppendLine($"            public  async Task<IActionResult> Delete(long id, CancellationToken cn)");
            controllerCode.AppendLine("        {");
            controllerCode.AppendLine("            // Delete logic here");
            controllerCode.AppendLine($"      var model = unitOfWork.Repository<{entityName}>().TableNoTracking.FirstOrDefault(c => c.Id == id);");
            controllerCode.AppendLine($"       if (model != null)");
            controllerCode.AppendLine($"         await unitOfWork.Repository<{entityName}>().DeleteAsync(model ,cn , true);");
            controllerCode.AppendLine("            return Ok();");
            controllerCode.AppendLine("        }");
            controllerCode.AppendLine("");
            // Select/Get method
            controllerCode.AppendLine($"        [HttpGet(\"[action]\")]");
            controllerCode.AppendLine("        [ActionDisplayName(\"ویرایش اطلاعات\", ActionAccessType.View, ActionAccessItemType.Update)]");
            controllerCode.AppendLine($"        public IActionResult Edit(long? id)");
            controllerCode.AppendLine("        {");
            controllerCode.AppendLine("            // Get logic here");
            controllerCode.AppendLine("            if (id != null && id != 0)");
            controllerCode.AppendLine("            {");
            controllerCode.AppendLine($"             var entity =   unitOfWork.Repository<{entityName}>().TableNoTracking.FirstOrDefault(c => c.Id == id);");
            controllerCode.AppendLine($"                return View(@\"{editPath}\", entity);");
            controllerCode.AppendLine("             }");
            controllerCode.AppendLine($"              var newEntity = new {entityName}(); ");
            controllerCode.AppendLine($"               {addToEditActionController}");
            controllerCode.AppendLine($"               return View(@\"{editPath}\",newEntity);");
            controllerCode.AppendLine("        }");

            controllerCode.AppendLine($"        [HttpGet(\"[action]\")]");
            controllerCode.AppendLine("        [ActionDisplayName(\"درج اطلاعات\", ActionAccessType.View, ActionAccessItemType.Create)]");
            controllerCode.AppendLine($"        public IActionResult New()");
            controllerCode.AppendLine("        {");
            controllerCode.AppendLine($"              var newEntity = new {entityName}(); ");
            controllerCode.AppendLine($"               {addToEditActionController}");
            controllerCode.AppendLine($"               return View(@\"{editPath}\",newEntity);");
            controllerCode.AppendLine("        }");


            controllerCode.AppendLine($"       [HttpGet(\"[action]\")]");
            controllerCode.AppendLine("        [ActionDisplayName(\"لیست اطلاعات\", ActionAccessType.View, ActionAccessItemType.List)]");
            controllerCode.AppendLine($"        public IActionResult List()");
            controllerCode.AppendLine("        {");
            controllerCode.AppendLine($"              return View(@\"{listPath}\");");
            controllerCode.AppendLine("        }");

            //excel 
            controllerCode.AppendLine($"       [HttpPost(\"[action]\")]");
            controllerCode.AppendLine("         [ActionDisplayName(\"خروجی اکسل\", ActionAccessType.Api)]");
            controllerCode.AppendLine($"       public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)");
            controllerCode.AppendLine("        {");
            controllerCode.AppendLine("          var licensePath = _webHostEnvironment.WebRootPath + \"\\\\Aspose.Total.NET.lic\";");
            controllerCode.AppendLine("         var memoryStream = new MemoryStream();");
            controllerCode.AppendLine($@"        try
        {{
              await unitOfWork.Repository<{entityName}>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);

            memoryStream.Position = 0;

            return File(memoryStream, ""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"", $""exportExcel.xlsx"");
        }}
        catch (Exception ex)
        {{
         
            return StatusCode(500, ""خطا در زمان ایجاد فایل اکسل: "" + ex.Message);
        }}");
            controllerCode.AppendLine("        }");

            //fetch
            controllerCode.AppendLine("        [ActionDisplayName(\"دریافت اطلاعات\", ActionAccessType.Api, ActionAccessItemType.FetchData)]");
            controllerCode.AppendLine($"       [HttpPost(\"[action]\")]");
            controllerCode.AppendLine($"         public async Task<IActionResult> FetchData(DataTableRequest request , CancellationToken cn)");
            controllerCode.AppendLine("        {");
            controllerCode.AppendLine($"              return Ok(await unitOfWork.Repository<{entityName}>().FetchDataAsync(request ,cn));");
            controllerCode.AppendLine("        }");
            controllerCode.AppendLine(AddToEndController.ToString());

            controllerCode.AppendLine("    }");
            controllerCode.AppendLine("}");



            // Write the controller code to a file
            System.IO.File.WriteAllText(path, controllerCode.ToString());

            Console.WriteLine($"Controller for {entityName} created successfully at {path}");
        }

        public string GenerateListView()
        {
            string viewFolder = Path.Combine(viewFolderPath, prefixPath, module, entityName);
            if (!Directory.Exists(viewFolder))
            {
                Directory.CreateDirectory(viewFolder);
            }

            string viewFilePath = Path.Combine(viewFolder, "List.cshtml");


            StringBuilder viewBuilder = new StringBuilder();

            viewBuilder.AppendLine($"@using {entityType.Namespace};");
            viewBuilder.AppendLine($"<datatableprofile entity-Type=\"typeof({entityName})\" ></datatableprofile>");

            System.IO.File.WriteAllText(viewFilePath, viewBuilder.ToString());

            return viewFilePath.Replace(Directory.GetCurrentDirectory(), "");
        }

        public string? GenerateColumnTabel(PropertyInfo prop)
        {
            var displayNameAttr = prop.GetCustomAttribute<DisplayInfoAttribute>();
            var addToTable = displayNameAttr?.AddToTable ?? false;
            if (addToTable)
            {

                var disnameattr = prop.GetCustomAttribute<DisplayNameAttribute>();
                string displayName = disnameattr != null ? disnameattr.DisplayName : prop.Name;
                string searchPath = displayNameAttr != null ? (displayNameAttr?.SearchPath ?? prop.Name) : prop.Name;
                string typeProperty = displayNameAttr != null ? displayNameAttr.type.ToString() : prop.PropertyType.Name.ToLower();
                var showInRelationData = displayNameAttr?.ShowInRelationData ?? false;
                string dataName = prop.Name.ToCamelCase();
                string tableName = "";
                var options = new StringBuilder(" [");

                if (typeProperty == "entity")
                {
                    var entityType = prop.PropertyType;
                    tableName = entityType.Name;
                    var entityProperties = entityType.GetProperties();
                    var relevantProperties = entityProperties
                        .Where(p => p.GetCustomAttribute<DisplayNameAttribute>() != null &&
                                    p.GetCustomAttribute<DisplayInfoAttribute>()?.ShowInRelationData == true)
                        .Select(p => new
                        {
                            DisplayName = p.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? p.Name,
                            PropertyName = p.Name
                        }).FirstOrDefault();

                    if (relevantProperties == null)
                    {

                        searchPath = entityType.Name + ".Id";
                    }
                    else
                    {
                        dataName = dataName + "" + relevantProperties.PropertyName;
                        searchPath = entityType.Name + "." + relevantProperties.PropertyName;
                    }

                }
                else if (typeProperty == "select")
                {
                    var enumType = prop.PropertyType;
                    var enumValues = Enum.GetValues(enumType);

                    foreach (var value in enumValues)
                    {
                        var enumValue = (Enum)value;
                        var titleEnum = GetEnumDisplayName(enumValue);
                        var enumNumber = Convert.ToInt32(enumValue);
                        options.Append($@"{{value :'{enumNumber}' , name : '{titleEnum}'}},");
                    }


                }
                else if (typeProperty == "liststring" || typeProperty == "listlong")
                {
                    // ListString and ListLong are displayed as comma-separated values in tables
                    // No options needed for these types
                }
                options.Append("] ");



                return $"{{ 'data': '{dataName}', 'type': '{typeProperty}', 'name': '{searchPath.ToCamelCase()}' , title:'{displayName}' , showInRelationData :{showInRelationData.ToString().ToLower()} , options: {options} ,tableName:'{tableName}' }},";

            }

            return null;
        }

        public string GenerateColumnsType(Type type)
        {
            StringBuilder viewBuilder = new StringBuilder();
            PropertyInfo[] properties = type.GetProperties();


            viewBuilder.Append($"{{ 'data': 'id', 'type': 'button' ,'sortable': false , 'render': function(data, type, row) {{ return `<td> <a href='#' class= 'btn btn-bg-light btn-icon btn-primary btn-sm '> <i class='fs-2 ki-duotone ki-plus lh-0'></i> </a> </td>`}} }},");
            foreach (var prop in properties)
            {
                var p = GenerateColumnTabel(prop);
                if (p != null)
                    viewBuilder.Append(p);
            }

            return viewBuilder.ToString();
        }

        public static string GetEnumDisplayName(Enum enumValue)
        {
            var displayAttribute = enumValue.GetType()
                .GetMember(enumValue.ToString())
                .First()
                .GetCustomAttribute<DisplayAttribute>();
            return displayAttribute?.Name ?? enumValue.ToString();
        }

        private string GenerateItemFields(PropertyInfo[] itemProperties, Type itemType, string nameproperty)
        {
            StringBuilder itemBuilder = new StringBuilder();
            foreach (var itemProp in itemProperties)
            {
                string itemName = itemProp.Name.ToCamelCase();
                var displayNameAttr = itemProp.GetCustomAttribute<DisplayNameAttribute>();
                string displayName = displayNameAttr != null ? displayNameAttr.DisplayName : itemProp.Name;

                itemBuilder.AppendLine($@"<div class='col-md-4'>
                                    <label>{displayName}</label>
                                    <input class='form-control' data-bind='{nameproperty}.{itemName}' value='@item.{itemProp.Name}' />
                                </div>");
            }
            return itemBuilder.ToString();
        }

        private string GenerateNewItemFields(PropertyInfo[] itemProperties, Type itemType, string nameproperty)
        {
            StringBuilder newItemBuilder = new StringBuilder();
            newItemBuilder.AppendLine("<div class='list-item row'>");
            foreach (var itemProp in itemProperties)
            {
                string itemName = itemProp.Name.ToCamelCase();
                var displayNameAttr = itemProp.GetCustomAttribute<DisplayNameAttribute>();
                string displayName = displayNameAttr != null ? displayNameAttr.DisplayName : itemProp.Name;

                newItemBuilder.AppendLine($@"<div class='col-md-4'>
                                        <label>{displayName}</label>
                                        <input class='form-control' data-bind='{nameproperty}.{itemName}' />
                                    </div>");
            }
            newItemBuilder.AppendLine("<div class='col-md-2'><button class='btn btn-danger remove-item' type='button'>حذف</button></div>");
            newItemBuilder.AppendLine("</div>");
            return newItemBuilder.ToString();
        }

        private string GeneratePartialView(Type item, string parentName = "", string arrayPropertyName = "")
        {
            string viewFolder = Path.Combine(viewFolderPath, prefixPath, module, parentName);

            if (!Directory.Exists(viewFolder))
            {
                Directory.CreateDirectory(viewFolder);
            }

            string viewFilePath = Path.Combine(viewFolder, $"_{item.Name}Partial.cshtml");

            StringBuilder viewBuilder = new StringBuilder();



            // Basic structure of the partial view
            viewBuilder.AppendLine($"@using {item.Namespace}");
            viewBuilder.AppendLine($"@model {item.Name}");

            viewBuilder.AppendLine("<div class=\"entity-item\">");
            // Hidden input with array name binding
            var idBind = string.IsNullOrEmpty(arrayPropertyName) ? "id" : $"{arrayPropertyName}.id";
            viewBuilder.AppendLine($"<input class='form-control' type=\"hidden\" data-bind='{idBind}' value='@Model?.Id' />");

            // Generate form elements for each property
            PropertyInfo[] properties = item.GetProperties();
            foreach (var prop in properties)
            {
                var displayNameAttr = prop.GetCustomAttribute<DisplayInfoAttribute>();
                var discriminate = prop.GetCustomAttribute<DisplayNameAttribute>();

                if (displayNameAttr != null)
                {
                    string displayName = discriminate != null ? discriminate.DisplayName : prop.Name;
                    string typeProperty = displayNameAttr.type.ToString() ?? prop.PropertyType.Name.ToLower();
                    var nameproperty = prop.Name.ToCamelCase();
                    var reqClass = displayNameAttr.Required ? "required" : "";

                    // Render input based on the type
                    if (typeProperty == "list")
                    {
                        // Handle lists (e.g., for handling OrderItems)
                        if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            var itemType = prop.PropertyType.GetGenericArguments()[0];
                            viewBuilder.AppendLine($@"<div class='col-md-12'>
                        <h5>{displayName}</h5>
                        @Html.Partial(""_{itemType.Name}Partial.cshtml"", Model.{prop.Name})
                    </div>");
                        }
                    }
                    else
                    {
                        viewBuilder.AppendLine(GenerateFieldPartial(prop, arrayPropertyName));
                    }
                }
            }


            viewBuilder.AppendLine("</div>");



            System.IO.File.WriteAllText(viewFilePath, viewBuilder.ToString());

            return viewFilePath.Replace(Directory.GetCurrentDirectory(), "");
        }

        private string GenerateField(PropertyInfo prop)
        {
            var displayNameAttr = prop.GetCustomAttribute<DisplayInfoAttribute>();
            var disnameattr = prop.GetCustomAttribute<DisplayNameAttribute>();
            var viewBuilder = new StringBuilder();

            if (displayNameAttr != null)
            {

                string displayName = disnameattr != null ? disnameattr.DisplayName : prop.Name;

                string searchPath = displayNameAttr != null ? (displayNameAttr?.SearchPath ?? prop.Name) : prop.Name;
                var typeProperty = displayNameAttr != null ? displayNameAttr.type : SystemType.String;

                var addToTable = displayNameAttr?.AddToTable ?? false;
                var systemAttr = displayNameAttr?.SystemProprty ?? false;
                var nameproperty = prop.Name.ToCamelCase();
                var customFileTypeFormat = displayNameAttr != null ? displayNameAttr.FileTypes : "";
                var maxFileSize = displayNameAttr != null ? displayNameAttr.MaxFileSize : 10;
                var required = displayNameAttr != null ? displayNameAttr.Required : false;
                var regex = displayNameAttr != null ? displayNameAttr?.Regex : null;

                var regexInput = "";

                if (regex != null)
                {
                    regexInput = $"data-invalidMessage='{displayNameAttr.RegexInvalidError}' data-inputmask=\"'regex':'{regex}' , 'placeholder' : ''\"";
                }
                var reqClass = required == true ? "required" : "";

                if (!systemAttr)
                {

                    if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType) && prop.PropertyType.IsGenericType)
                    {
                        // Handling List properties
                        var itemType = prop.PropertyType.GetGenericArguments()[0];

                        // Check if it's ListString or ListLong
                        if (typeProperty == SystemType.ListString || typeProperty == SystemType.ListLong)
                        {
                            // Handle ListString and ListLong as multi-select or tag input
                            var isListString = typeProperty == SystemType.ListString;
                            var inputType = isListString ? "text" : "number";
                            var placeholder = isListString ? "مقدار را وارد کنید و Enter بزنید" : "شناسه را وارد کنید و Enter بزنید";

                            viewBuilder.AppendLine($@"                                                   <div class='col-md-12'>
                                                    <h4>{displayName}</h4>
                                                    <div id='{prop.Name}Container' class=""list-item-container"">
                                                       
                                                         @if(Model?.{prop.Name} != null && Model.{prop.Name}.Any())
                                                           {{
                                                            @foreach(var item in Model.{prop.Name})
                                                                {{
                                                                   <div class='list-item-tag mb-2'>
                                                                        <span class='badge badge-primary'> </span>
                                                                        <button type='button' class='btn btn-sm btn-icon btn-light remove-tag' data-value=''>
                                                                            <i class='ki-duotone ki-cross fs-5'>
                                                                                <span class='path1'></span>
                                                                                <span class='path2'></span>
                                                                            </i>
                                                                        </button>
                                                                    </div>
                                                                }}
                                                            }}
                                                      
                                                    </div>
                                                    <div class='input-group mt-2'>
                                                        <input type='{inputType}' class='form-control' id='{prop.Name}Input' placeholder='{placeholder}' />
                                                        <button type='button' class='btn btn-primary' id='add{prop.Name}Item'>افزودن</button>
                                                    </div>
                                                    <input type='hidden' data-bind='{nameproperty}' value='@(Model?.{prop.Name} != null ? string.Join("","" , Model.{prop.Name}) : """")' />
                                                </div>");

                            addToSaveScripts.AppendLine($"model.{prop.Name} = $(\"#{prop.Name}Container\").find('.badge').map((i, el) => $(el).text().trim()).get().filter(v => v !== '');");

                            addToEndScripts.AppendLine($@"
                                                             $('#add{prop.Name}Item').click(function() {{
                                                                var value = $('#{prop.Name}Input').val().trim();
                                                                if (value) {{
                                                                    var tagHtml = '<div class=""list-item-tag mb-2""><span class=""badge badge-primary"">' + value + '</span><button type=""button"" class=""btn btn-sm btn-icon btn-light remove-tag"" data-value=""' + value + '""><i class=""ki-duotone ki-cross fs-5""><span class=""path1""></span><span class=""path2""></span></i></button></div>';
                                                                    $('#{prop.Name}Container').append(tagHtml);
                                                                    $('#{prop.Name}Input').val('');
                                                                }}
                                                             }});
                                                             
                                                             $(document).on('click', '#{prop.Name}Container .remove-tag', function() {{
                                                                $(this).closest('.list-item-tag').remove();
                                                             }});
                                                             
                                                             $('#{prop.Name}Input').keypress(function(e) {{
                                                                if (e.which === 13) {{
                                                                    e.preventDefault();
                                                                    $('#add{prop.Name}Item').click();
                                                                }}
                                                             }});   ");
                        }
                        else
                        {
                            // Handling ListEntity (complex objects) - Store for tab manager generation
                            var pathPartial = GeneratePartialView(itemType, entityName, nameproperty);

                            // Store ListEntity info for tab manager generation
                            listEntityProperties.Add((prop, itemType, displayName, nameproperty));

                            // Generate controller action for partial view
                            AddToEndController.AppendLine($"       [HttpGet(\"[action]\")]");
                            AddToEndController.AppendLine($"        public IActionResult {itemType.Name}Partial()");
                            AddToEndController.AppendLine("        {");
                            AddToEndController.AppendLine($"              return PartialView(@\"{pathPartial}\");");
                            AddToEndController.AppendLine("        }");
                        }
                    }
                    else
                    {
                        switch (typeProperty)
                        {
                            case SystemType.String:
                                viewBuilder.AppendLine($@"                                        <div class='col-md-6'>
                                                                <label class='{reqClass}'>{displayName}</label>
                                                                <input class='form-control'  data-bind='{nameproperty}' {reqClass} {regexInput} asp-for='{prop.Name}'/>
                                                                <div data-invalidmessagespan class='my-1 mx-1'> <span class='text-danger' ></span> </div>
                                                            </div>");
                                break;
                            case SystemType.Boolean:

                                viewBuilder.AppendLine($@"
                                                              <div class='col-md-6 align-content-center'>
                                                                   <div class=""form-check"">
                                                                        <input data-bind='{nameproperty}' class=""form-check-input"" type=""checkbox"" asp-for='{prop.Name}' />
                                                                        <label class=""form-check-label"" asp-for='{prop.Name}'>
                                                                            {displayName}
                                                                        </label>
                                                                    </div>
                                                                </div>
                                                                ");
                                break;


                            case SystemType.Long:
                            case SystemType.Int:
                            case SystemType.Decimal:
                                viewBuilder.AppendLine($@"                                        <div class='col-md-6'>
                                                                <label class='{reqClass}'>{displayName}</label>
                                                                <input class='form-control' data-bind='{nameproperty}' {reqClass} data-invalidMessage='فقط عداد' data-inputmask=""'regex':'^[\u06F0-\u06F90-9]+$' , 'placeholder' : ''"" asp-for='{prop.Name}' />
                                                                   <div data-invalidmessagespan class='my-1 mx-1'> <span class='text-danger' ></span> </div>
                                                            </div>");
                                break;
                            case SystemType.AutoNumber:

                                addToEditActionController.AppendLine($"newEntity.{prop.Name} = identityService.GenerateNewValueIdentity(\"{prop.Name}\",\"{entityName}\");");
                                viewBuilder.AppendLine($@"                                        <div class='col-md-6'>
                                                                <label class='{reqClass}'>{displayName}</label>
                                                                <input class='form-control' disabled=""disabled"" data-bind='{nameproperty}' {reqClass} data-invalidMessage='فقط عداد' data-inputmask=""'regex':'^[\u06F0-\u06F90-9]+$' , 'placeholder' : ''"" asp-for='{prop.Name}' />
                                                                   <div data-invalidmessagespan class='my-1 mx-1'> <span class='text-danger' ></span> </div>
                                                            </div>");
                                break;
                            case SystemType.Select:

                                var enumType = prop.PropertyType;

                                viewBuilder.AppendLine($@"                                     <div class='col-md-6'>
                                     <label class='{reqClass}'>{displayName}</label>
                                     <select class='form-control' data-bind='{nameproperty}' {reqClass} asp-for='{prop.Name}' asp-items=""Html.GetEnumSelectList(typeof({enumType}))"">
                                          
                                     </select>
                                     <div data-invalidmessagespan class='my-1 mx-1'> <span class='text-danger' ></span> </div>
                                 </div>");
                                break;

                            case SystemType.Entity:

                                var entityTypeEntity = prop.PropertyType;
                                var entityProperties = entityTypeEntity.GetProperties();
                                var relevantProperties = entityProperties
                                    .Where(p => p.GetCustomAttribute<DisplayNameAttribute>() != null &&
                                                p.GetCustomAttribute<DisplayInfoAttribute>()?.ShowInRelationData == true)
                                    .Select(p => new
                                    {
                                        DisplayName = p.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? p.Name,
                                        PropertyName = p.Name
                                    }).FirstOrDefault();



                                viewBuilder.AppendLine($@"                                                 <div class=""col-md-6"">
                                            <entityselector value-id=""@Model?.{prop.Name}?.Id"" value-title=""@Model?.{prop.Name}?.{relevantProperties?.PropertyName}"" bind=""{prop.Name}Id"" entity-type=""typeof({prop.PropertyType})"" label=""{displayName}""></entityselector>

                                        </div>");


                                break;
                            case SystemType.File:
                                viewBuilder.AppendLine($@"      <div class='col-md-6'>
	                         <fileuploader bindid=""{nameproperty}"" file-id=""@Model?.{prop.Name}Id"" file-entity=""@Model?.{prop.Name}""
                                                  Label =""{displayName}""
				                             entity-type=""{entityType.Name}""
				                             entity-prop-name=""{prop.Name}Id""
                                                 bind=""{prop.Name}""
                                                  max-file-size=""{maxFileSize}""
				                             accepted-file-types=""{customFileTypeFormat}""
                                        ></fileuploader>
                             </div>");
                                break;
                            case SystemType.DateTime:

                                viewBuilder.AppendLine($@"                                         <div class='col-md-6'>
                                                                <label class='{reqClass}'>
                                                                  {displayName}</label>
                                                                                                                                <input class='form-control' {reqClass} data-bind='{nameproperty}' data-persionDatePicker=""true"" persion-datetimepicker='{{""format"": ""YYYY/MM/DD HH:mm:ss"",""autoClose"": true,""initialValue"": false,""timePicker"":{{""enabled"": true}}}}"" value='@Model?.{prop.Name}' />
                                                            </div>");
                                break;
                            case SystemType.Date:

                                viewBuilder.AppendLine($@"                                         <div class='col-md-6'>
                                                                <label class='{reqClass}'>
                                                                  {displayName}</label>
                                                          <input class='form-control' {reqClass} data-bind='{nameproperty}' data-persionDatePicker=""true"" persion-datetimepicker='{{""format"": ""YYYY/MM/DD"",""autoClose"": true,""initialValue"": false}}' value='@Model?.{prop.Name}' />
                                                            </div>");

                                break;
                            case SystemType.DateShamsi:

                                viewBuilder.AppendLine($@"                                         <div class='col-md-6'>
                                                                <label class='{reqClass}'>
                                                                  {displayName}</label>
                                                          <input class='form-control' {reqClass} data-bind='{nameproperty}' data-persionDatePicker=""true"" persion-datetimepicker='{{""format"": ""YYYY/MM/DD"",""autoClose"": true,""initialValue"": false}}' value='@Model?.{prop.Name}' />
                                                            </div>");

                                break;
                            case SystemType.DateTimeShamsi:

                                viewBuilder.AppendLine($@"                                         <div class='col-md-6'>
                                                                <label class='{reqClass}'>
                                                                  {displayName}</label>
                                                          <input class='form-control' {reqClass} data-bind='{nameproperty}' data-persionDatePicker=""true"" persion-datetimepicker='{{""format"": ""YYYY/MM/DD HH:mm:ss"",""autoClose"": true,""initialValue"": false,""timePicker"":{{""enabled"": true}}}}' value='@Model?.{prop.Name}' />
                                                            </div>");

                                break;
                            default:
                                viewBuilder.AppendLine($@"                                        <div class='col-md-6'>
                                                                <label class='{reqClass}'>{displayName}</label>
                                                                <input class='form-control'  data-bind='{nameproperty}' {reqClass} {regexInput} asp-for='{prop.Name}'/>
                                                                <div data-invalidmessagespan class='my-1 mx-1'> <span class='text-danger' ></span> </div>
                                                            </div>");
                                break;


                        }
                    }
                }
            }

            return viewBuilder.ToString();
        }
        private string GenerateFieldPartial(PropertyInfo prop, string arrayPropertyName = "")
        {
            var displayNameAttr = prop.GetCustomAttribute<DisplayInfoAttribute>();
            var disnameattr = prop.GetCustomAttribute<DisplayNameAttribute>();
            var viewBuilder = new StringBuilder();

            if (displayNameAttr != null)
            {

                string displayName = disnameattr != null ? disnameattr.DisplayName : prop.Name;

                string searchPath = displayNameAttr != null ? (displayNameAttr?.SearchPath ?? prop.Name) : prop.Name;
                var typeProperty = displayNameAttr != null ? displayNameAttr.type : SystemType.String;
                var addToTable = displayNameAttr?.AddToTable ?? false;
                var systemAttr = displayNameAttr?.SystemProprty ?? false;
                var nameproperty = prop.Name.ToCamelCase();
                // Build data-bind path: if arrayPropertyName exists, use "arrayName.propertyName", otherwise just "propertyName"
                var bindPath = string.IsNullOrEmpty(arrayPropertyName) ? nameproperty : $"{arrayPropertyName}.{nameproperty}";
                var customFileTypeFormat = displayNameAttr != null ? displayNameAttr.FileTypes : "";
                var maxFileSize = displayNameAttr != null ? displayNameAttr.MaxFileSize : 10;
                var required = displayNameAttr != null ? displayNameAttr.Required : false;
                var regex = displayNameAttr != null ? displayNameAttr?.Regex : null;

                var regexInput = "";

                if (regex != null)
                {
                    regexInput = $"data-invalidMessage='{displayNameAttr.RegexInvalidError}' data-inputmask=\"'regex':'{regex}' , 'placeholder' : ''\"";
                }
                var reqClass = required == true ? "required" : "";

                if (!systemAttr)
                {

                    if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType) && prop.PropertyType.IsGenericType)
                    {
                        return "";

                    }
                    else
                    {



                        switch (typeProperty)
                        {
                            case SystemType.String:
                                viewBuilder.AppendLine($@"                                        <div class='col-md-3'>
                                                                <label class='{reqClass}'>{displayName}</label>
                                                                <input class='form-control' data-bind='{bindPath}' {reqClass} {regexInput} value='@Model?.{prop.Name}'/>
                                                                <div data-invalidmessagespan class='my-1 mx-1'> <span class='text-danger' ></span> </div>
                                                            </div>");
                                break;
                            case SystemType.Boolean:
                                break;
                            case SystemType.Long:
                            case SystemType.Int:
                            case SystemType.Decimal:
                                viewBuilder.AppendLine($@"                                        <div class='col-md-3'>
                                                                <label class='{reqClass}'>{displayName}</label>
                                                                <input class='form-control' data-bind='{bindPath}' {reqClass} data-invalidMessage='فقط عداد' data-inputmask=""'regex':'^[\u06F0-\u06F90-9]+$' , 'placeholder' : ''"" value='@Model?.{prop.Name}' />
                                                                   <div data-invalidmessagespan class='my-1 mx-1'> <span class='text-danger' ></span> </div>
                                                            </div>");
                                break;

                            case SystemType.AutoNumber:
                                viewBuilder.AppendLine($@"                                        <div class='col-md-3'>
                                                                <label class='{reqClass}'>{displayName}</label>
                                                                <input class='form-control' disabled=""disabled"" data-bind='{bindPath}' {reqClass} data-invalidMessage='فقط عداد' data-inputmask=""'regex':'^[\u06F0-\u06F90-9]+$' , 'placeholder' : ''"" value='@Model?.{prop.Name}' />
                                                                   <div data-invalidmessagespan class='my-1 mx-1'> <span class='text-danger' ></span> </div>
                                                            </div>");
                                break;
                            case SystemType.Select:

                                var enumType = prop.PropertyType;

                                viewBuilder.AppendLine($@"                                     <div class='col-md-6'>
                                     <label class='{reqClass}'>{displayName}</label>
                                     <select class='form-control' data-bind='{bindPath}' {reqClass} asp-for='{prop.Name}' asp-items=""Html.GetEnumSelectList(typeof({enumType}))"">
                                          
                                     </select>
                                     <div data-invalidmessagespan class='my-1 mx-1'> <span class='text-danger' ></span> </div>
                                 </div>");
                                break;

                            case SystemType.Entity:

                                var entityTypeEntity = prop.PropertyType;
                                var entityProperties = entityTypeEntity.GetProperties();
                                var relevantProperties = entityProperties
                                    .Where(p => p.GetCustomAttribute<DisplayNameAttribute>() != null &&
                                                p.GetCustomAttribute<DisplayInfoAttribute>()?.ShowInRelationData == true)
                                    .Select(p => new
                                    {
                                        DisplayName = p.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? p.Name,
                                        PropertyName = p.Name
                                    }).FirstOrDefault();

                                viewBuilder.AppendLine($@"                                                 <div class=""col-md-3"">
                                            <entityselector value-id=""@Model?.{prop.Name}?.Id"" value-title=""@Model?.{prop.Name}?.{relevantProperties?.PropertyName}"" bind=""{prop.Name}Id"" entity-type=""typeof({prop.PropertyType})"" label=""{displayName}""></entityselector>

                                        </div>");


                                break;
                            case SystemType.File:
                                // In GenerateFieldPartial, we need to determine the entity type
                                // For partial views, use the parent entity name (entityName) or item type if available
                                var fileEntityTypeName = entityName; // Use parent entity name for partial views
                                // For file uploader, bindid should use bindPath (arrayName.propertyName format)
                                viewBuilder.AppendLine($@"      <div class='col-md-3'>
	                         <fileuploader bind=""{bindPath}"" file-id=""@Model?.{prop.Name}Id"" file-entity=""@Model?.{prop.Name}""
                                                  Label =""{displayName}""
				                             entity-type=""{fileEntityTypeName}""
				                             entity-prop-name=""{prop.Name}Id""
                                                  max-file-size =""{maxFileSize}""
				                             accepted-file-types=""{customFileTypeFormat}""></fileuploader>
                             </div>");
                                break;
                            case SystemType.DateTime:

                                viewBuilder.AppendLine($@"                                         <div class='col-md-3'>
                                                                <label class='{reqClass}'>
                                                                  {displayName}</label>
                                                                <input class='form-control' {reqClass} data-bind='{bindPath}' data-persionDatePicker=""true"" persion-datetimepicker='{{""format"": ""YYYY/MM/DD HH:mm:ss"",""autoClose"": true,""initialValue"": false,""timePicker"":{{""enabled"": true}}}}"" value='@Model?.{prop.Name}' />
                                                            </div>");
                                break;
                            case SystemType.Date:

                                viewBuilder.AppendLine($@"                                         <div class='col-md-3'>
                                                                <label class='{reqClass}'>
                                                                  {displayName}</label>
                                                                <input class='form-control' {reqClass} data-bind='{bindPath}' data-persionDatePicker=""true"" persion-datetimepicker='{{""format"": ""YYYY/MM/DD"",""autoClose"": true,""initialValue"": false}}' value='@Model?.{prop.Name}' />
                                                            </div>");

                                break;
                            case SystemType.DateShamsi:

                                viewBuilder.AppendLine($@"                                         <div class='col-md-3'>
                                                                <label class='{reqClass}'>
                                                                  {displayName}</label>
                                                                <input class='form-control' {reqClass} data-bind='{bindPath}' data-persionDatePicker=""true"" persion-datetimepicker='{{""format"": ""YYYY/MM/DD"",""autoClose"": true,""initialValue"": false}}' value='@Model?.{prop.Name}' />
                                                            </div>");

                                break;
                            case SystemType.DateTimeShamsi:

                                viewBuilder.AppendLine($@"                                         <div class='col-md-6'>
                                                                <label class='{reqClass}'>
                                                                  {displayName}</label>
                                                          <input class='form-control' {reqClass} data-bind='{bindPath}' data-persionDatePicker=""true"" persion-datetimepicker='{{""format"": ""YYYY/MM/DD HH:mm:ss"",""autoClose"": true,""initialValue"": false,""timePicker"":{{""enabled"": true}}}}' value='@Model?.{prop.Name}' />
                                                            </div>");

                                break;
                            default:
                                viewBuilder.AppendLine($@"                                        <div class='col-md-6'>
                                                                <label class='{reqClass}'>{displayName}</label>
                                                                <input class='form-control'  data-bind='{bindPath}' {reqClass} {regexInput} asp-for='{prop.Name}'/>
                                                                <div data-invalidmessagespan class='my-1 mx-1'> <span class='text-danger' ></span> </div>
                                                            </div>");
                                break;

                        }
                    }
                }
            }

            return viewBuilder.ToString();
        }
        public string GenerateEditView()
        {
            string viewFolder = Path.Combine(viewFolderPath, prefixPath, module, entityName);

            if (!Directory.Exists(viewFolder))
            {
                Directory.CreateDirectory(viewFolder);
            }

            string viewFilePath = Path.Combine(viewFolder, "Edit.cshtml");


            StringBuilder viewBuilder = new StringBuilder();



            var tableName = entityName;


            var typeDisplayname = entityType.GetCustomAttribute<DisplayAttribute>();

            var entityTitle = typeDisplayname != null ? typeDisplayname?.Name : entityType.Name;

            viewBuilder.AppendLine($"@using {entityType.Namespace};");
            viewBuilder.AppendLine($"@model {entityName}");

            viewBuilder.AppendLine("<div class=\"card\">");
            viewBuilder.AppendLine($@"  <div class=""form-action-buttons""><form-action-buttons entity-type=""typeof({entityType.Name})""></form-action-buttons></div>");

            viewBuilder.AppendLine("    <div class=\"card-body row\">");

            viewBuilder.AppendLine($"       <input class='form-control' data-bind='id'  value='@Model?.Id' type='hidden'/>");


            if (entityType != null)
            {
                PropertyInfo[] properties = entityType.GetProperties();
                foreach (var prop in properties)
                {

                    viewBuilder.AppendLine(GenerateField(prop));

                }
            }

            // Generate tab manager for ListEntity properties if any exist
            if (listEntityProperties.Any())
            {
                var managerId = $"{entityName.ToCamelCase()}Items";
                viewBuilder.AppendLine($@"        <div class='col-md-12'>
            <div data-tab-item-manager=""{managerId}"" data-item-selector="".entity-item"">
                <ul class=""nav nav-tabs nav-line-tabs nav-line-tabs-2x border-transparent fs-4 fw-bold mb-5"" role=""tablist"">");

                bool isFirst = true;
                foreach (var (prop, itemType, displayName, nameProperty) in listEntityProperties)
                {
                    var tabId = $"{prop.Name.ToCamelCase()}Tab";
                    var activeClass = isFirst ? "active" : "";
                    var ariaSelected = isFirst ? "true" : "false";

                    viewBuilder.AppendLine($@"                    <li class=""nav-item"" role=""presentation"">
                        <a class=""nav-link {activeClass}"" data-bs-toggle=""tab"" href=""#{tabId}"" role=""tab"" aria-selected=""{ariaSelected}"">
                            <span class=""svg-icon svg-icon-2 me-2""></span>
                            {displayName}
                        </a>
                    </li>");

                    isFirst = false;
                }

                viewBuilder.AppendLine(@"                </ul>
                <div class=""tab-content"">");

                isFirst = true;
                foreach (var (prop, itemType, displayName, nameProperty) in listEntityProperties)
                {
                    var tabId = $"{prop.Name.ToCamelCase()}Tab";
                    var showClass = isFirst ? "show active" : "";
                    var containerId = $"{prop.Name}Container";

                    viewBuilder.AppendLine($@"                    <div class=""tab-pane fade {showClass}"" id=""{tabId}"" role=""tabpanel"" data-add-url=""/{prefixPath}/{entityName}/{itemType.Name}Partial"">
                        <div id='{containerId}' class=""entity-item-container"">
                            @if(Model?.{prop.Name} != null)
                            {{
                                @foreach(var item in Model.{prop.Name})
                                {{
                                    <div data-item-id=""@(item.Id ?? 0)"">
                                        @await Html.PartialAsync(""_{itemType.Name}Partial.cshtml"", item)
                                    </div>
                                }}
                            }}
                        </div>
                    </div>");

                    isFirst = false;
                }

                viewBuilder.AppendLine(@"                </div>
            </div>
        </div>");
            }

            viewBuilder.AppendLine("    </div>");
            viewBuilder.AppendLine(@"               <div class='card-footer row'>
                        <div class='col-md-3'>
                            <span>ایجاد کننده :</span>
                            <span data-bind=""createdByName"">@Model?.CreatedByName</span>
                        </div>

                        <div class='col-md-3'>
                            <span>تاریخ ایجاد :</span>
                            <span data-bind=""createdOnShamsiDateTime"">@Model?.CreatedOnShamsiDateTime</span>
                        </div>

                            <div class='col-md-3'>
                            <span>ویرایش کننده :</span>
                            <span data-bind=""modifiedByName"">@Model?.ModifiedByName</span>
                        </div>

                        <div class='col-md-3'>
                            <span>تاریخ ویرایش :</span>
                            <span data-bind=""modifiedDateShamsiDateTime"">@Model?.ModifiedDateShamsiDateTime</span>
                        </div>
                </div>");
            viewBuilder.AppendLine("</div>");

            viewBuilder.AppendLine("");
            viewBuilder.AppendLine("");


            viewBuilder.AppendLine($@" 
                                        <script>

                                      	  function savefn($btnAction){{
				   if(validateError($$('.card')))
					   {{ return; }}
				   var model = $$('.card').dataBind();
					 const $btn = $(this.element).block();
				   $$.post('save',
					   model,
					   function(r) {{
						   $btn.block(false);
						   if (!r.isSuccess) return toastr.error(`${{r.message}}`, 'خطا');
						   toastr.success('ذخیره سازی با موفقیت انجام شد .');
						   $$('.card').dataBind(r.data);
						   $btnAction.baseaction();
					   }})
			    }}

	     
           self.FormActionButtons.save.onclick(function() {{savefn(this)}})
           self.FormActionButtons.saveandnew.onclick(function() {{savefn(this)}})
          self.FormActionButtons.saveandclose.onclick(function() {{savefn(this)}})
                                            
                                                {addToEndScripts}
                                        </script>
                                  
                                    ");

            System.IO.File.WriteAllText(viewFilePath, viewBuilder.ToString());

            return viewFilePath.Replace(Directory.GetCurrentDirectory(), "");
        }
    }
}


