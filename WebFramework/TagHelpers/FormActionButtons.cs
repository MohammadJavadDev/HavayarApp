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
		public Type? entityType { get; set; }
		public bool newPageBtn { get; set; } = true;
		public bool saveBtn { get; set; } = true;
		public bool saveAndNewBtn { get; set; } = true;
		public bool saveAndCloseBtn { get; set; } = true;
		public bool historyBtn { get; set; } = true;

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

			var htmlContent = new StringBuilder();

			htmlContent.AppendLine(@"<div class=""form-action-buttons"">");

			if (entityType != null)
			{
				if (newPageBtn)
				{
					htmlContent.AppendLine(@"
					  <button class=""btn-sm btn btn-icon btn-active-icon-dark btn-color-primary d-none"" data-action=""new_page"" data-bs-toggle=""tooltip"" data-bs-placement=""top"" title=""جدید"">
						 <i class=""fs-2 fa-jelly fa-light fa-circle-plus"" style=""padding-right: 2px;padding-top: 2px;""></i>
					  </button>");
				}

				if (saveBtn)
				{
					htmlContent.AppendLine(@"
            <button class=""btn-sm btn btn-icon btn-active-icon-dark btn-color-primary"" data-action=""save"" data-bs-toggle=""tooltip"" data-bs-placement=""top"" title=""ذخیره"">
                <i class=""fs-2 fa-light fa-floppy-disk"" style=""padding-top: 2px; padding-right: 2px;""></i>
            </button>");
				}

				if (saveAndNewBtn)
				{
					htmlContent.AppendLine(@"
				  <button class=""btn-sm btn btn-icon btn-active-icon-dark btn-color-success p-2 d-none"" data-action=""saveandnew"" data-bs-toggle=""tooltip"" data-bs-placement=""top"" title=""ذخیره و جدید"">
					 <i class=""fs-2 fa-light fa-floppy-disk-circle-arrow-right""></i>
				  </button>");
				}

				if (saveAndCloseBtn)
				{
					htmlContent.AppendLine(@"
            <button class=""btn-sm btn btn-icon btn-active-icon-dark btn-color-danger"" data-action=""saveandclose"" data-bs-toggle=""tooltip"" data-bs-placement=""top"" title=""ذخیره و بستن"">
                <i class=""fs-2 fa-light fa-floppy-disk-circle-xmark"" style=""padding-right: 2px;padding-top: 2px;""></i>
            </button>");
				}

				if (historyBtn)
				{
					var historyType = entityType?.Name ?? "";
					htmlContent.AppendLine($@"
            <button class=""btn-sm btn btn-icon btn-active-icon-dark btn-color-info"" data-system-action=""history"" data-action=""history"" data-system-history-type=""{historyType}"" data-bs-toggle=""tooltip"" data-bs-placement=""top"" title=""تاریخچه"">
                <i class=""fs-2 fa-light fa-files-medical"" style=""padding-right: 2px;padding-top: 2px;""></i>
            </button>");
				}
			}

			htmlContent.Append(extraItems.GetContent());

			htmlContent.AppendLine("</div>");

			output.Content.SetHtmlContent(htmlContent.ToString());
		}

	}
}


