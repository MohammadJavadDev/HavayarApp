using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace WebFramework.TagHelpers
{
	public class FormActionButtons : TagHelper
	{
		public  Type? entityType { get; set; }
		public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
		{
			output.TagName = "div";

               var extraItems = await output.GetChildContentAsync();

			output.AddClass("gap-2", HtmlEncoder.Default);
			output.AddClass("d-md-flex", HtmlEncoder.Default);
			output.AddClass("justify-content-start", HtmlEncoder.Default);
			output.AddClass("align-items-center", HtmlEncoder.Default);
			output.AddClass("col-md-auto", HtmlEncoder.Default);
			output.AddClass("me-auto", HtmlEncoder.Default);
			output.Content.SetHtmlContent(@$" 
                           
                                 <button class=""btn-sm btn btn-icon btn-active-icon-dark btn-color-primary"" data-action=""new_page"" data-bs-toggle=""tooltip"" data-bs-placement=""top"" title=""جدید"">
                                      <i class=""fs-2 fa-jelly fa-light fa-circle-plus"" style=""padding-right: 2px;padding-top: 2px;""></i>
                                 </button>

                                 <button class=""btn-sm btn btn-icon btn-active-icon-dark btn-color-primary "" data-action=""save"" data-bs-toggle=""tooltip"" data-bs-placement=""top"" title=""ذخیر"">
                                      <i class=""fs-2 fa-light fa-floppy-disk"" style="" padding-top: 2px;   padding-right: 2px;""></i>

                                 </button>
                                 <button class=""btn-sm btn btn-icon btn-active-icon-dark btn-color-success p-2"" data-action=""saveandnew"" data-bs-toggle=""tooltip"" data-bs-placement=""top"" title=""ذخیر و جدید"">
                                      <i class=""fs-2 fa-light fa-floppy-disk-circle-arrow-right""></i>

                                 </button>

                                 <button class=""btn-sm btn btn-icon btn-active-icon-dark btn-color-danger"" data-action=""saveandclose"" data-bs-toggle=""tooltip"" data-bs-placement=""top"" title=""ذخیر و  بستن"">
                                      <i class=""fs-2 fa-light fa-floppy-disk-circle-xmark"" style=""padding-right: 2px;padding-top: 2px;""></i>

                                 </button>
     

                                 <button class=""btn-sm btn btn-icon btn-active-icon-dark btn-color-info "" data-system-action=""history"" data-action=""history"" data-system-history-type=""{entityType?.Name}"" data-bs-toggle=""tooltip"" data-bs-placement=""top"" title=""تاریخچه"">
                                      <i class=""fs-2 fa-light fa-files-medical"" style="" padding-right: 2px;padding-top: 2px;""></i>
            
                                 </button>
                                   {extraItems.GetContent()}
							");
			 
		}
	}

}
