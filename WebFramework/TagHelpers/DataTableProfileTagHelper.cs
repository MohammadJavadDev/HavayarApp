using Common.Attributes;
using Common.Auth.Enums;
using Common.System;
using Common.Utilities;
using Data.Repositories;
using Data.Services.QueryBuilderServices;
using Data.SystemAuth;
using Entities.Base;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Services.AccessServices;
using System.Text.Encodings.Web;

namespace WebFramework.TagHelpers
{
	[HtmlTargetElement("datatableprofile")]
	public class DataTableProfileTagHelper(
		IQueryService queryService,
		IRoleMemoryStorage _roleMemoryStorage,
		IAccessMemoryStorage _accessMemoryStorage,
		ISdk sdk) :TagHelper
    {
         public  string? entityName { get; set; }
	    public string? unicode { get; set; }
	    public  Type? EntityType { get; set; }
	   public bool newButton { get; set; } = false;
	   public string? newEntityPath { get; set; }
	   public string? editEntityPath { get; set; }
        public string? deleteEntityPath { get; set; }
	   public string? exportExcelPath { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {

			if(unicode.HasValue())
			{
				entityName = unicode;
			}
			 

			if (entityName == null && EntityType == null)
			{
				output.TagName = "div";
				output.AddClass("card", HtmlEncoder.Default);
				output.AddClass("p-5", HtmlEncoder.Default);
				output.Content.SetHtmlContent("<span class='badge badge-danger fs-2hx'>نام یا نوع موجودیت تعریف نشده است</span>");
				return;

			}

			if (!unicode.HasValue())
			{
				var controller = EntityType != null
					? _accessMemoryStorage.GetAccessControllerBy(EntityType)
					: _accessMemoryStorage.GetAccessControllerBy(entityName!);

				if (controller == null)
				{
					output.TagName = "div";
					output.AddClass("card", HtmlEncoder.Default);
					output.AddClass("p-5", HtmlEncoder.Default);
					output.Content.SetHtmlContent("<span class='badge badge-danger fs-2hx'>نام یا نوع موجودیت تعریف نشده است</span>");
					return;

				}

				if (controller != null && entityName == null)
				{
					entityName = controller.EntityType.FullName;
				}


				var existEntityCreateAction = controller?.Actions.FirstOrDefault(c => c.ActionAccessType == ActionAccessType.View && c.ActionAccessItemType == ActionAccessItemType.Create);
				var existEntityUpdateAction = controller?.Actions.FirstOrDefault(c => c.ActionAccessType == ActionAccessType.View && c.ActionAccessItemType == ActionAccessItemType.Update);
				var existEntityDelteAction = controller?.Actions.FirstOrDefault(c => c.ActionAccessType == ActionAccessType.Api && c.ActionAccessItemType == ActionAccessItemType.Delete);


				if (!newEntityPath.HasValue(true) && existEntityCreateAction != null)
				{

					newEntityPath = existEntityCreateAction
						.Path;

				}

				if (!editEntityPath.HasValue(true) && existEntityUpdateAction != null)
				{
					editEntityPath = existEntityUpdateAction
										.Path;
				}
				if (!deleteEntityPath.HasValue(true) && existEntityDelteAction != null)
				{
					deleteEntityPath = existEntityDelteAction
										.Path;
				}
			}

			 
			var profiles = new List<SavedQuery>();

			if (sdk.CurrentUser.IsAdministrator)
			{
				profiles = queryService.GetDataTableProfileListByEntityName(entityName);
			}
			else
			{
				var profilesAccess = sdk.CurrentUser.RoleAccess
				.Where(c => c.ActionAccessType == ActionAccessType.DataProfile
		          	&& c.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase)   
					&& c.RowId != null)
				.Select(c => c.RowId) .ToList();
				profiles = queryService.GetDataTableProfileById(profilesAccess , entityName);
			}
				

 

		   var listOptions = "";
	        if (profiles == null || profiles.Count == 0)
	        {
		        listOptions = "<option value=''>بدون نمایه داده</option>";

	        }
			else
	        {
		        profiles.ForEach(c =>
		        {
					listOptions += $"<option value='{c.Id}'>{c.Title}</option>";

				});

			}


			var disableActions = "disabled";
			
			if(sdk.CurrentUser.IsAdministrator)
			{
				disableActions = "";
			}

			///System/ReportBuilder/ExportDataToExcel

		 
			var guid = Guid.NewGuid().ToString().Replace("-", "");


			output.TagName = "div";
			output.AddClass("card",HtmlEncoder.Default);
            output.Content.SetHtmlContent($@"
	       
	            <div class=""card-body p-1"" >
				    <div class='row justify-content-end position-relative'>
					<div class=""col-md-5 text-center  position-absolute mt-3"" data-place=""ProfileSelector"">
			
				            <div class=""input-group mb-3"">
					            <a   class="" btn btn-icon   btn-active-color-warning   me-1 {disableActions} "" data-action=""editDataProfile"" >
						            <i class=""ki-duotone ki-pencil fs-2"">
							            <span class=""path1""></span>
							            <span class=""path2""></span>
						            </i>
					            </a>
					            <a class="" btn btn-icon   btn-active-color-success  me-1 {disableActions} "" data-action=""newDataProfile"" >
						            <i class=""fa fa-plus fs-2"">
						            </i>
					            </a>
							 <a class="" btn btn-icon   btn-active-color-danger  me-1 {disableActions} "" data-action=""removeDataProfile"" >
						            <i class=""fa fa-trash fs-2"">
						            </i>
					            </a>

							<a class="" btn btn-icon   btn-active-color-info  me-1 {disableActions} "" data-action=""copyDataProfile"" >
						            <i class=""ki-copy-success ki-outline fs-2"">
						            </i>
					            </a>
					            <select class=""form-select"" data-action=""dataProfile"" data-entityName=""{entityName}"" data-edit-path=""{editEntityPath}"" data-delete-path=""{deleteEntityPath}""    data-new-path=""{newEntityPath}"" data-exportExcell-path=""{exportExcelPath}"">
						            {listOptions}
					            </select>
				            </div>
  
		            </div>
					</div>

				<div class=""collapse"" id=""searchBuilderCollapse{guid}"" data-place=""searchBuilderCollapse"">
		 
					   <div id=""searchBuilderContainer"" ></div>
			 
				</div>
                  <div class='table-responsive'>
		            <table id=""itemsTable"" class=""table table-rounded table-striped border table-bordered nowrap table-hover"" style=""width: 100%;direction: rtl;""  >
                        
		            </table>
				</div>
	            </div>
");
        }
    }
}
