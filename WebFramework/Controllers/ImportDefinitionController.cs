using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Entities.Base.ImportDefinitions;
using Entities.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Services.ImportDefinitionServices;
using System.Xml.XPath;
using WebFramework.Filtters;
using WebFramework.Page;
using WebFramework.ViewModels.ImportDefinitionsViewModels;

namespace WebFramework.Controllers
{

	[Route("[controller]")]
	[ApiController]
	[ApiResultFilter]
	[Authorize("AuthenticatedUser")]
	[ControllerInfoAttribute("تعریف ورود اطلاعات")]
	public class ImportDefinitionController  : BaseController 
	{
		private readonly IImportRepository _repo;
		private readonly IEntityMetadataCache _entityMetadataCache;
		private readonly ImportApiEndpointInspector _apiInspector;

		public ImportDefinitionController(
			IImportRepository repo,
			IEntityMetadataCache entityMetadataCache,
			ImportApiEndpointInspector apiInspector)
		{
			_repo = repo;
			_entityMetadataCache = entityMetadataCache;
			_apiInspector = apiInspector;
		}

		[HttpGet("/panel/importDefinition/list")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
 
			return View(@"\Views\Panel\System\ImportDefinition\List.cshtml");
		}

		[HttpGet("/panel/importDefinition/new")]
		[ActionDisplayName("جدید", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var vm = BuildViewModel(new ImportDefinition());
			return View(@"\Views\Panel\System\ImportDefinition\Edit.cshtml");
		}

		[HttpGet("/panel/importDefinition/edit")]
		[ActionDisplayName("ویرایش", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(int id)
		{
			var def = _repo.GetDefinitionById(id);
			if (def == null) return NotFound();
			var vm = BuildViewModel(def);
			return View(@"\Views\Panel\System\ImportDefinition\Edit.cshtml", vm);
 
		}


		[HttpPost("/panel/importDefinition/save")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
 
 
		public IActionResult Save(ImportDefinition model)
		{
			if (string.IsNullOrWhiteSpace(model.FullNameEntity) ||
			    string.IsNullOrWhiteSpace(model.Title))
			{
				throw new Exception("فیلدهای الزامی را پر کنید.");
			}

			var columns = string.IsNullOrWhiteSpace(model.Columns)
				? null
				: model.Columns.JsonDeserialize<List<ImportDefinitionColumn>>();

			if (columns == null || columns.Count == 0)
			{
				throw new Exception("حداقل یک ستون تعریف کنید.");
			}

			if (model.ImportType == ImportSourceType.Sql)
			{
				if (string.IsNullOrWhiteSpace(model.SqlQuery))
					throw new Exception("برای نوع SQL، کوئری SQL الزامی است.");
			}
			else if (model.ImportType == ImportSourceType.Api)
			{
				if (string.IsNullOrWhiteSpace(model.ApiUrl))
					throw new Exception("برای نوع API، آدرس API الزامی است.");

				if (string.IsNullOrWhiteSpace(model.ApiHttpMethod))
					model.ApiHttpMethod = "POST";

				model.ApiCallMode ??= ApiCallMode.PerRow;
				model.SqlQuery = null;
			}
			else
			{
				throw new Exception("نوع ورود اطلاعات نامعتبر است.");
			}

			_repo.SaveDefinition(model);
			 
			TempData["Success"] = "تعریف ورود اطلاعات با موفقیت ذخیره شد.";
			return Ok();
		}

		[HttpGet("/panel/importDefinition/getAvailableApis")]
		[ActionDisplayName("لیست APIهای سیستم", ActionAccessType.Api)]
		public IActionResult GetAvailableApis()
		{
			var apis = _apiInspector.GetAvailableApis()
				.Select(a => new
				{
					controller = a.Controller,
					controllerDisplayName = a.ControllerDisplayName,
					action = a.Action,
					actionDisplayName = a.ActionDisplayName,
					path = a.Path,
					httpMethod = a.HttpMethod,
					label = $"{a.ControllerDisplayName} / {a.ActionDisplayName} ({a.HttpMethod} {a.Path})"
				})
				.ToList();

			return Ok(apis);
		}

		[HttpPost("/panel/importDefinition/inspectApi")]
		[ActionDisplayName("بازرسی API", ActionAccessType.Api)]
		public IActionResult InspectApi([FromBody] ImportApiInspectRequest request)
		{
			var result = _apiInspector.Inspect(request);
			return Ok(new
			{
				apiUrl = result.ApiUrl,
				httpMethod = result.HttpMethod,
				controller = result.Controller,
				action = result.Action,
				columns = result.Columns.Select(c => new
				{
					columnName = c.ColumnName,
					displayName = c.DisplayName,
					dataType = c.DataType,
					isRequired = c.IsRequired
				})
			});
		}

		[HttpPost("/panel/importDefinition/delete")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Delete)]
 
		public IActionResult Delete(int id)
		{
			_repo.DeleteDefinition(id);
			TempData["Success"] = "تعریف با موفقیت حذف شد.";
			return Ok();
		}

		[HttpGet("/panel/importDefinition/getColumns/{id}")]
		public IActionResult GetColumns(int id)
		{
			  
			var columns = _repo.GetColumnsByDefinitionId(id)
			    .Where(c => !c.IsSystemVariable)
			    .OrderBy(c => c.SortOrder)
			    .Select(c => new { c.ColumnName, c.DisplayName, c.DataType, c.IsRequired })
			    .ToList();
			return  Ok(columns);
		}

		[HttpGet("/panel/importDefinition/getAvailableEntities")]
		[ActionDisplayName("دریافت موجودیت‌ها", ActionAccessType.Api)]
		public IActionResult GetAvailableEntities()
		{
			var entities = _entityMetadataCache.GetAll()
				.Select(e => new
				{
					name = e.EntityName,
					fullName = e.EntityFullName,
					displayName = e.DisplayName,
					schema = e.Schema,
					tableName = e.TabelName
				})
				.OrderBy(e => e.displayName)
				.ToList();

			return Ok(entities);
		}

		[HttpPost("/panel/importDefinition/generateFromEntity")]
		[ActionDisplayName("تولید از موجودیت", ActionAccessType.Api)]
		public IActionResult GenerateFromEntity([FromBody] GenerateFromEntityRequest request)
		{
			if (string.IsNullOrWhiteSpace(request?.EntityFullName))
				throw new Exception("موجودیت انتخاب نشده است.");

			var entityMeta = _entityMetadataCache.Get(request.EntityFullName);
			if (entityMeta == null)
				throw new Exception("موجودیت یافت نشد.");

			var result = ImportDefinitionEntityGenerator.Generate(entityMeta);
			return Ok(result);
		}

		// -------------------------------------------------------
		private ImportDefinitionViewModel BuildViewModel(ImportDefinition def) =>
		    new ImportDefinitionViewModel
		    {
			    Definition = def,
			    DataTypeList = new SelectList(EnumExtensions.GetEnumValuesWithDisplayNames<SystemType>(), "Key", "Text"),
			    SystemVariableList = new SelectList(SystemVariables.GetAll(), "Key", "Value")
		    };

		public class ImportDefinitionSaveModel
		{
			public ImportDefinition Definition { get; set; }
			public List<string> ColColumnName { get; set; }
			public List<string> ColDisplayName { get; set; }
			public List<SystemType> ColDataType { get; set; }
			public List<Nullable<bool>> ColIsSystemVariable { get; set; }
			public List<string> ColSystemVariable { get; set; }
			public List<Nullable<bool>> ColIsRequired { get; set; }
		}

		public class GenerateFromEntityRequest
		{
			public string EntityFullName { get; set; } = "";
		}
	}
}
