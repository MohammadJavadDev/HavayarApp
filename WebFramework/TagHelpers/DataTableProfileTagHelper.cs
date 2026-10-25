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
	   public string? fetchDataPath { get; set; }
	   /// <summary>
	   /// Optional comma-separated SavedQuery.Name keys. When set, only these profiles
	   /// appear as tabs (admin and RoleAccess users). Use this so List and Monitoring
	   /// of the same entity do not share every profile.
	   /// </summary>
	   public string? ProfileNames { get; set; }

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
				var updateActions = controller?.Actions
					.Where(c => c.ActionAccessType == ActionAccessType.View && c.ActionAccessItemType == ActionAccessItemType.Update)
					.ToList();
				var existEntityUpdateAction = updateActions?
					.FirstOrDefault(c => c.Path != null && c.Path.EndsWith("/Edit", StringComparison.OrdinalIgnoreCase))
					?? updateActions?.FirstOrDefault();
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

			profiles = FilterProfiles(profiles);

			var isAdministrator = sdk.CurrentUser.IsAdministrator;
			if (profiles.Count == 0)
			{
				var noneDefined = isAdministrator || !PageHasAnyDataProfile(entityName);
				output.TagName = "div";
				output.AddClass("card", HtmlEncoder.Default);
				output.Content.SetHtmlContent(BuildEmptyDataProfileHtml(
					entityName,
					editEntityPath,
					deleteEntityPath,
					newEntityPath,
					exportExcelPath,
					fetchDataPath,
					noneDefined,
					isAdministrator));
				return;
			}

		   var listOptions = "";
	        profiles.ForEach(c =>
	        {
				listOptions += $"<option value='{c.Id}'>{System.Net.WebUtility.HtmlEncode(c.Title)}</option>";
			});


			var profileActionButtons = "";
			if (isAdministrator)
			{
				profileActionButtons = @"
					            <a class=""btn btn-icon btn-active-color-warning me-1"" data-action=""editDataProfile"">
						            <i class=""ki-duotone ki-pencil fs-2"">
							            <span class=""path1""></span>
							            <span class=""path2""></span>
						            </i>
					            </a>
					            <a class=""btn btn-icon btn-active-color-success me-1"" data-action=""newDataProfile"">
						            <i class=""fa fa-plus fs-2""></i>
					            </a>
					            <a class=""btn btn-icon btn-active-color-danger me-1"" data-action=""removeDataProfile"">
						            <i class=""fa fa-trash fs-2""></i>
					            </a>
					            <a class=""btn btn-icon btn-active-color-info me-1"" data-action=""copyDataProfile"">
						            <i class=""ki-copy-success ki-outline fs-2""></i>
					            </a>
					            <a class=""btn btn-icon btn-active-color-primary me-1"" data-action=""saveDataProfileColumnWidths"" data-bs-toggle=""tooltip"" data-bs-placement=""top"" title=""ذخیره عرض ستون‌ها"">
						            <i class=""fa fa-arrows-h fs-2""></i>
					            </a>";
			}

			var selectorColumnClass = isAdministrator ? "col-md-7" : "col-md-4";

			///System/ReportBuilder/ExportDataToExcel

		 
			var guid = Guid.NewGuid().ToString().Replace("-", "");


			output.TagName = "div";
			output.AddClass("card",HtmlEncoder.Default);
            output.Content.SetHtmlContent($@"
	       
	            <div class=""card-body p-1"" >
				    <div class='row justify-content-end position-relative'>
					<div class=""{selectorColumnClass} text-center position-absolute mt-3"" data-place=""ProfileSelector"">
			
				            <div class=""input-group mb-3 flex-nowrap"">
					            {profileActionButtons}
					            <span class=""input-group-text"">نمایه داده ها</span>
					            <select class=""form-select"" data-action=""dataProfile"" data-entityName=""{entityName}"" data-edit-path=""{editEntityPath}"" data-delete-path=""{deleteEntityPath}""    data-new-path=""{newEntityPath}"" data-exportExcell-path=""{exportExcelPath}"" data-fetch-path=""{fetchDataPath}"">
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

		private List<SavedQuery> FilterProfiles(List<SavedQuery>? profiles)
		{
			if (profiles == null || profiles.Count == 0)
				return new List<SavedQuery>();

			if (!string.IsNullOrWhiteSpace(ProfileNames))
			{
				var wanted = ProfileNames
					.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				var byName = profiles
					.Where(c => !string.IsNullOrWhiteSpace(c.Name))
					.GroupBy(c => c.Name!, StringComparer.OrdinalIgnoreCase)
					.ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
				return wanted
					.Where(n => byName.ContainsKey(n))
					.Select(n => byName[n])
					.ToList();
			}

			return profiles
				.Select((profile, index) => new { profile, index })
				.OrderBy(x => IsBomItemProfile(x.profile) ? 1 : 0)
				.ThenBy(x => x.index)
				.Select(x => x.profile)
				.ToList();
		}

		private bool PageHasAnyDataProfile(string? entityName)
		{
			if (string.IsNullOrWhiteSpace(entityName))
				return false;

			var all = queryService.GetDataTableProfileListByEntityName(entityName);
			return FilterProfiles(all).Count > 0;
		}

		private static string BuildEmptyDataProfileHtml(
			string? entityName,
			string? editPath,
			string? deletePath,
			string? newPath,
			string? exportPath,
			string? fetchPath,
			bool noneDefined,
			bool isAdministrator)
		{
			string title;
			string lead;
			string why;
			string badge;
			string reason;

			if (noneDefined)
			{
				badge = "نمایه داده ندارد";
				reason = "missing";
				title = "نمایه داده‌ای برای این صفحه ساخته نشده است";
				if (isAdministrator)
				{
					lead = "تا یک نمایه داده نسازید، این صفحه نمی‌تواند ستون‌ها و ردیف‌های اطلاعات را نشان بدهد.";
					why = "نمایه داده قالبی است که مشخص می‌کند چه ستون‌ها، فیلترها و دکمه‌هایی در این فهرست دیده شوند. برای این صفحه هنوز نمایه‌ای ثبت نشده است.";
				}
				else
				{
					lead = "اطلاعاتی برای نمایش آماده نیست، چون برای این صفحه هنوز نمایه داده تعریف نشده است.";
					why = "این فهرست فقط از طریق نمایه داده پر می‌شود. از مدیر سیستم بخواهید یک نمایه داده برای این صفحه بسازد.";
				}
			}
			else
			{
				badge = "بدون دسترسی";
				reason = "forbidden";
				title = "به نمایه داده این صفحه دسترسی ندارید";
				lead = "نمایه داده این صفحه ساخته شده، اما نقش شما اجازه دیدن آن را ندارد و به همین خاطر فهرست نمایش داده نمی‌شود.";
				why = "دسترسی به هر نمایه داده برای نقش‌ها جداگانه تنظیم می‌شود. از مدیر سیستم بخواهید در مدیریت نقش‌ها، دسترسی نمایه داده این صفحه را برای نقش شما فعال کند.";
			}

			var action = noneDefined && isAdministrator
				? @"<button type=""button"" class=""btn btn-primary dt-profile-empty__action"" data-action=""newDataProfile"">
						<i class=""fa fa-plus""></i>
						<span>ساخت نمایه داده</span>
					</button>"
				: "";

			var icon = noneDefined ? EmptyProfileIcon() : NoAccessProfileIcon();

			return $@"
				<div class=""card-body dt-profile-empty-body"">
					<select class=""d-none"" aria-hidden=""true"" tabindex=""-1"" data-action=""dataProfile"" data-entityName=""{E(entityName)}"" data-edit-path=""{E(editPath)}"" data-delete-path=""{E(deletePath)}"" data-new-path=""{E(newPath)}"" data-exportExcell-path=""{E(exportPath)}"" data-fetch-path=""{E(fetchPath)}"">
						<option value=""""></option>
					</select>
					<div class=""dt-profile-empty"" data-empty-reason=""{reason}"">
						<div class=""dt-profile-empty__icon"" aria-hidden=""true"">{icon}</div>
						<span class=""dt-profile-empty__badge"">{badge}</span>
						<h2 class=""dt-profile-empty__title"">{title}</h2>
						<p class=""dt-profile-empty__lead"">{lead}</p>
						<div class=""dt-profile-empty__why"">
							<span class=""dt-profile-empty__why-label"">چرا این را می‌بینید؟</span>
							<p>{why}</p>
						</div>
						{action}
					</div>
				</div>";
		}

		private static string E(string? value) => HtmlEncoder.Default.Encode(value ?? "");

		private static string EmptyProfileIcon() => @"
			<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 64 64"" width=""64"" height=""64"" fill=""none"">
				<rect x=""7"" y=""11"" width=""50"" height=""38"" rx=""8"" stroke=""currentColor"" stroke-width=""2""/>
				<path d=""M7 23h50"" stroke=""currentColor"" stroke-width=""2""/>
				<path d=""M18 32h18M18 40h11"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" opacity="".4""/>
				<circle cx=""47"" cy=""46"" r=""12"" fill=""var(--bs-body-bg,#fff)"" stroke=""currentColor"" stroke-width=""2""/>
				<path d=""M47 40.5v11M41.5 46h11"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round""/>
			</svg>";

		private static string NoAccessProfileIcon() => @"
			<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 64 64"" width=""64"" height=""64"" fill=""none"">
				<rect x=""7"" y=""11"" width=""50"" height=""38"" rx=""8"" stroke=""currentColor"" stroke-width=""2""/>
				<path d=""M7 23h50"" stroke=""currentColor"" stroke-width=""2""/>
				<path d=""M18 32h16M18 40h10"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" opacity="".4""/>
				<circle cx=""47"" cy=""46"" r=""12"" fill=""var(--bs-body-bg,#fff)"" stroke=""currentColor"" stroke-width=""2""/>
				<rect x=""42.2"" y=""45.2"" width=""9.6"" height=""7.4"" rx=""1.4"" stroke=""currentColor"" stroke-width=""1.7""/>
				<path d=""M44.2 45.2v-2a2.8 2.8 0 0 1 5.6 0v2"" stroke=""currentColor"" stroke-width=""1.7"" stroke-linecap=""round""/>
			</svg>";

		private static bool IsBomItemProfile(SavedQuery profile)
		{
			var name = profile.Name ?? "";
			var title = profile.Title ?? "";
			return name.Contains("_Bom_", StringComparison.OrdinalIgnoreCase)
				|| title.Contains("اقلام BOM", StringComparison.OrdinalIgnoreCase);
		}
    }
}
