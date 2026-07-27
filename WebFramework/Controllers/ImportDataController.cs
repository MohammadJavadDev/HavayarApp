using Common.Attributes;
using Common.Auth.Enums;
using Data.SystemAuth;
using Entities.Base.ImportDefinitions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Services.ImportDefinitionServices;
using System.Text.Json;
using WebFramework.Filtters;
using WebFramework.Page;
using WebFramework.ViewModels.ImportDefinitionsViewModels;

namespace WebFramework.Controllers
{

	[Route("[controller]")]
	[ApiController]
	[ApiResultFilter]
	[Authorize("AuthenticatedUser")]
	[ControllerInfoAttribute("ورود اطلاعات")]
	public class ImportDataController : BaseController
	{
		private readonly IImportRepository _repo;
		private readonly string _connectionString;
		private readonly ISdk _sdk;

		public ImportDataController(IImportRepository repo, IConfiguration config, ISdk sdk)
		{
			_repo = repo;
			_connectionString = config.GetConnectionString("Db");
			_sdk = sdk;
		}

		[HttpGet("/panel/importData/list")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			var vm = new ImportDataViewModel();			
			if (IsAdministrator)
			{
				vm.Definitions = _repo.GetAllDefinitions();
			}
			else
			{
				var accessImport = _sdk.CurrentUser.RoleAccess.Where(c => c.ActionAccessType == ActionAccessType.ImportData)
					.Select(c=>c.RowId).ToList();
				vm.Definitions = _repo.GetDefinitionById(accessImport);

			}


			 
			return View(@"\Views\Panel\System\ImportDefinition\ImportData.cshtml", vm);
		}



		[HttpGet("/panel/importData/downloadSample/{id}")]
		public IActionResult DownloadSample(long id)
		{
			 
			var def = _repo.GetDefinitionById(id);
			if (def == null) return NotFound();

			var bytes = ExcelImportHelper.GenerateSampleExcel(def);
			var fileName = $"Sample_{def.FullNameEntity}_{DateTime.Today:yyyyMMdd}.xlsx";
			return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
		}

		 
		[HttpPost("/panel/importData/getExcelHeaders")]
		public IActionResult GetExcelHeaders(IFormFile excelFile)
		{
			if (excelFile == null || excelFile.Length == 0)
				return BadRequest("فایل انتخاب نشده است.");

			try
			{
				using var stream = excelFile.OpenReadStream();
				var headers = ExcelImportHelper.ReadExcelHeaders(stream);
				return Json(headers);
			}
			catch (Exception ex)
			{
				return BadRequest("خطا در خواندن اکسل: " + ex.Message);
			}
		}

		public class ImportDataFromExcellViewModel
		{
			public IFormFile excelFile { get; set; }
			public int importDefinitionId { get; set; }
			public string columnMappingJson { get; set; }
			public bool rollbackOnError { get; set; }
		}
	 
		[HttpPost("/panel/importData/import")]
		public IActionResult Import(ImportDataFromExcellViewModel model)  
		{
			if (model.excelFile == null || model.excelFile.Length == 0)
				return BadRequest(new { success = false, message = "فایل انتخاب نشده است." });

			var def = _repo.GetDefinitionById(model.importDefinitionId);
			if (def == null)
				return BadRequest(new { success = false, message = "تعریف ورود اطلاعات یافت نشد." });

			// پارس mapping
			var mapping = new Dictionary<string, string>(); // definitionColumnName -> excelHeader
			if (!string.IsNullOrWhiteSpace(model.columnMappingJson))
			{
				try
				{
					// mapping ورودی: { "ExcelHeader": "DefinitionColumnName" }
					var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(model.columnMappingJson);
					// برعکس می‌کنیم: definitionColumnName -> excelHeader
					if (raw != null)
						foreach (var kv in raw)
							mapping[kv.Value] = kv.Key;
				}
				catch { /* ignore */ }
			}

			try
			{
				List<Dictionary<string, string>> rows;
				using (var stream = model.excelFile.OpenReadStream())
					rows = ExcelImportHelper.ReadExcel(stream);

				if (rows.Count == 0)
					return Ok(new { success = true, totalRows = 0, successRows = 0, failedRows = 0, logId = 0, rolledBack = false });

				 

				var executor = new ImportExecutor(_connectionString, _sdk);
				var log = executor.ExecuteImport(def, rows, mapping, model.rollbackOnError);
				log.FileName = model.excelFile.FileName;

				// ذخیره لاگ
				var logId = _repo.SaveImportLog(log);
				_repo.SaveImportLogDetails(logId, log.Details);

				// آماده‌سازی پاسخ
				var failedDetails = log.Details
				    .Where(d => !d.Success)
				    .Select(d => new { d.RowNumber, d.ErrorMessage })
				    .ToList();

				return Ok(new
				{
					success = true,
					logId,
					totalRows = log.TotalRows,
					successRows = log.SuccessRows,
					failedRows = log.FailedRows,
					rolledBack = log.RolledBack,
					failedDetails
				});
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { success = false, message = ex.Message });
			}
		}
	}
}
