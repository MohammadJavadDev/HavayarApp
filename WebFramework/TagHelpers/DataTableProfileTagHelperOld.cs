using Common.Attributes;
using Common.Auth.Enums;
using Common.System;
using Common.Utilities;
using Data.Repositories;
using Data.SystemAuth;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Services.AccessServices;
using System.Text.Encodings.Web;

namespace WebFramework.TagHelpers
{
	[HtmlTargetElement("datatableprofileold")]
	public class DataTableProfileTagHelperOld(
		IDataTableProfileService dataTableProfileService,
		IRoleMemoryStorage _roleMemoryStorage,
		IAccessMemoryStorage _accessMemoryStorage,
		ISdk sdk) :TagHelper
    {
        public  string? entityName { get; set; }
	   public  Type? EntityType { get; set; }
	   public bool newButton { get; set; } = false;
	   public string? newEntityPath { get; set; }
	   public string? editEntityPath { get; set; }
        public string? deleteEntityPath { get; set; }
	   public string? exportExcelPath { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {

			if (entityName == null && EntityType == null) {
				output.TagName = "div";
				output.AddClass("card", HtmlEncoder.Default);
				output.AddClass("p-5", HtmlEncoder.Default);
				output.Content.SetHtmlContent("<span class='badge badge-danger fs-2hx'>نام یا نوع موجودیت تعریف نشده است</span>");
				return;

			}

			var controller = _accessMemoryStorage.GetAccessControllerBy(EntityType);

			if (controller == null )
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



			

			var profiles = new List<SystemDataTableProfile>();

			if (sdk.CurrentUser.IsAdministrator)
			{
				profiles = dataTableProfileService.GetDataTableProfileListByEntityName(entityName);
			}
			else
			{
				var profilesAccess = sdk.CurrentUser.RoleAccess
				.Where(c => c.ActionAccessType == ActionAccessType.DataProfile
		          	&& c.EntityName == entityName)
				.Select(c => c.RowId).ToArray();
				profiles = dataTableProfileService.GetDataTableProfileById(profilesAccess);
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

			var existEntityCreateAction = controller?.Actions.FirstOrDefault(c => c.ActionAccessType == ActionAccessType.View && c.ActionAccessItemType== ActionAccessItemType.Create);
			var existEntityUpdateAction = controller?.Actions.FirstOrDefault(c => c.ActionAccessType == ActionAccessType.View && c.ActionAccessItemType == ActionAccessItemType.Update);
		     var existEntityDelteAction = controller?.Actions.FirstOrDefault(c => c.ActionAccessType == ActionAccessType.Api && c.ActionAccessItemType == ActionAccessItemType.Delete);


			if(!newEntityPath.HasValue(true) && existEntityCreateAction != null)
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
			  
           

			///System/ReportBuilder/ExportDataToExcel

		 
			var guid = Guid.NewGuid().ToString().Replace("-", "");


			output.TagName = "div";
			output.AddClass("card",HtmlEncoder.Default);
            output.Content.SetHtmlContent($@"
	       
	            <div class=""card-body p-1"" >
				    <div class='row justify-content-end position-relative'>
					<div class=""col-md-5 text-center  position-absolute mt-3"" data-place=""ProfileSelector"">
			
				            <div class=""input-group mb-3"">
					            <a   class="" btn btn-icon btn-bg-light btn-active-color-warning   me-1"" data-action=""editDataProfile"">
						            <i class=""ki-duotone ki-pencil fs-2"">
							            <span class=""path1""></span>
							            <span class=""path2""></span>
						            </i>
					            </a>
					            <a class="" btn btn-icon btn-bg-light btn-active-color-success  me-1"" data-action=""newDataProfile"">
						            <i class=""fa fa-plus fs-2"">
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
