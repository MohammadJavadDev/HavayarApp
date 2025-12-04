using Common.Attributes;
using Data.Repositories;
using Entities.Base;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Services.AccessServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace WebFramework.TagHelpers
{
    [HtmlTargetElement("entityselector")]
    public class EntitySelectorTagHelper(IDataTableProfileService dataTableProfileService,
			    IAccessMemoryStorage _accessMemoryStorage) : TagHelper
    {
        public   string? entityName { get; set; }
		public Type? entityType { get; set; }
		public required string Bind { get; set; }
        public required string Label { get; set; }
         public string ValueId { get; set; }
        public string ValueTitle { get; set; }
          public bool Required { get; set; }  

          public override void Process(TagHelperContext context, TagHelperOutput output)
        {

			var controller = _accessMemoryStorage.GetAccessControllerBy(entityType);

			if (controller != null && entityName == null)
			{
				entityName = controller.EntityType.FullName;
			}
               else
               {
                    entityName = entityType.FullName;

			}

                    var profiles = dataTableProfileService.GetDataTableProfileListByEntityName(entityName).FirstOrDefault();

         

            var showInRelationData = entityType.GetProperties()
                .FirstOrDefault(c =>
                    c.GetCustomAttributes<DisplayInfoAttribute>()?.FirstOrDefault()?.ShowInRelationData == true)
                ?.Name ?? "Id";

             

            output.TagName = "div";
            output.Attributes.Add(new TagHelperAttribute("data-action-entity-select-profile", ""));
            output.Attributes.Add(new TagHelperAttribute("data-action-entity-select-showInRelationData", showInRelationData));

               var requiredClas = Required == true ? "required" : "";

		  output.Content.SetHtmlContent($@"
	           
                 <label class='form-label {requiredClas} '>{Label}</label>
                 <div class='input-group mb-5'>
                     <input type='text' class='form-control' readonly data-bind='$$_{Bind}' value='{ValueTitle}' />
                     <input type='hidden' class='form-control' data-bind='{Bind}' value='{ValueId}' />
                     <span class='input-group-text bg-primary cursor-pointer' data-action-profile='selectentity' data-profileId='{profiles?.Id}'>
                         <i class='ki-duotone ki-click  text-white'>
                             <span class='path1'></span>
                             <span class='path2'></span>
                             <span class='path3'></span>
                             <span class='path4'></span>
                             <span class='path5'></span>
                         </i>
                     </span>
                 </div>
             
            ");
        }
    }
}
