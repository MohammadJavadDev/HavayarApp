using Microsoft.AspNetCore.Html;
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
	public class FormActionButton : TagHelper
	{
		public string ColorClass { get; set; }
		public string IconClass { get; set; }
		public string Title { get; set; }
		public string ActionName { get; set; }
		public override void Process(TagHelperContext context, TagHelperOutput output)
		{
			output.TagName = "button";

			output.AddClass("btn-sm", HtmlEncoder.Default);
			output.AddClass("btn", HtmlEncoder.Default);
			output.AddClass("btn-icon", HtmlEncoder.Default);
			output.Attributes.Add("data-action", ActionName);
			output.Attributes.Add("title", Title);
			output.Attributes.Add("data-bs-toggle", "tooltip");
			output.Attributes.Add("data-bs-placement", "top");

			if (ColorClass != null)
			{
				foreach (var item in ColorClass.Split(" "))
				{
					if (item.Length > 0)
					{
						output.AddClass(item, HtmlEncoder.Default);
					}
				}
			}
			output.Content.SetHtmlContent($@"
				<i class='fs-2 {IconClass}'></i>
				");

			return;
		}
	}

}
