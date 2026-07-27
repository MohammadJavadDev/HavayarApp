using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Data.Services.QueryBuilderServices;
using Entities.Base;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using ReportBuilder.Entities;
using ReportBuilder.Services;
using ReportBuilder.Services.Contracts;
using ReportBuilder.WebApp.ViewModels;
using Stimulsoft.Base;
 
using Stimulsoft.Report;
using Stimulsoft.Report.Dictionary;
using Stimulsoft.Report.Mvc;
using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using WebFramework.Filtters;
using WebFramework.Page;
 
 

namespace ReportBuilder.WebApp.Controllers
{
    [ApiController]
    [ApiResultFilter]
    [Route("System/[controller]")]
 
    [Authorize("AuthenticatedUser")]
    [ControllerInfo("گزارش ساز")]
    public class ReportBuilderController(IReportBuilderService reportBuilderService ,
         IUnitOfWork unitOfWork,
	   IQueryService _queryService) 
          : BaseController
    {
        
        public static string reportJsonData = "";
		public static string dashboardJsonData = "";


		/// <summary>
		/// فقط فایل‌های TTF — برای لیست فونت Designer و رندر Viewer لازم است با StiFontCollection هم ثبت شوند.
		/// نام واقعی خانواده داخل فایل‌ها: IRANSansWeb(FaNum)
		/// </summary>
		public static List<ReportBuilderReportFonts> Fonts = new()
		{
			new() { Name = "IRANSansWeb.ttf", Type = StiResourceType.FontTtf },
			new() { Name = "IRANSansWeb_Bold.ttf", Type = StiResourceType.FontTtf },
			new() { Name = "IRANSansWeb_Medium.ttf", Type = StiResourceType.FontTtf },
			new() { Name = "IRANSansWeb_Light.ttf", Type = StiResourceType.FontTtf },
			new() { Name = "IRANSansWeb_UltraLight.ttf", Type = StiResourceType.FontTtf },
		};

		private static bool _stimulsoftFontsRegistered;

		static ReportBuilderController()
		{
			RegisterStimulsoftFonts();
		}

		/// <summary>
		/// ثبت فونت‌ها در StiFontCollection تا در Font Picker طراح ظاهر شوند و در Viewer/Export قابل استفاده باشند.
		/// </summary>
		private static void RegisterStimulsoftFonts()
		{
			if (_stimulsoftFontsRegistered) return;

			foreach (var f in Fonts)
			{
				if (!f.Name.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase))
					continue;

				var path = ResolveFontFilePath(f.Name);
				if (path == null) continue;

				try
				{
					// alias کوتاه برای سازگاری با گزارش‌هایی که Font=IRANSansWeb دارند
					var alias = f.Name.StartsWith("IRANSansWeb_", StringComparison.OrdinalIgnoreCase)
						? null
						: "IRANSansWeb";
					if (alias != null)
						StiFontCollection.AddFontFile(path, alias);
					else
						StiFontCollection.AddFontFile(path);
				}
				catch
				{
					// اگر قبلاً ثبت شده باشد، نادیده بگیر
				}
			}

			_stimulsoftFontsRegistered = true;
		}

		private static string? ResolveFontFilePath(string fileName)
		{
			var candidates = new[]
			{
				Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "panelLib", "fonts", fileName),
				Path.Combine(AppContext.BaseDirectory, "wwwroot", "panelLib", "fonts", fileName),
			};

			foreach (var path in candidates)
			{
				if (System.IO.File.Exists(path))
					return path;
			}

			return null;
		}

        [HttpGet("{action}")]
        [ActionDisplayName("لیست آیتم ها", ActionAccessType.View)]
        public async Task<IActionResult> List()
        {

			var reports = await _queryService.GetAllSavedQueriesForReportsAsync();

		 
			return View("Views/GenerateItem/List.cshtml", reports);
        }

        [HttpGet("[action]/{objectName}/{type}")]
        [ActionDisplayName("ویرایش آیتم ها", ActionAccessType.View)]
        public async Task<IActionResult> Edit(string objectName, string type)
        {
             var  data =  await reportBuilderService.DbObjectWithItems(objectName , type);
             var tb = objectName.Split(".");
            ViewBag.objectName = tb[1];
             ViewBag.objectSchema = tb[0];
                
             ViewBag.id = data?.TableItemId!;
             ViewBag.tableTitle = data.TableTitle!;
             ViewBag.objectType = data.TableType;

            return View("Views/GenerateItem/Edit.cshtml" , data);

        }

        [HttpPost("[action]")]
        [ActionDisplayName("ذخیر آیتم", ActionAccessType.Api)]
        public async Task<IActionResult> SaveItem(ReportBuilderTable model , CancellationToken cn)
        {

            model.FiltersContent = model.ReportBuilderTableFilters.JsonSerialize();
            model.SelectsContent = model.ReportBuilderTableSelects.JsonSerialize();

            if (model.Id == null || model.Id == 0)
            {
                 
                model =   await unitOfWork.Repository<ReportBuilderTable>().AddAsync(model, cn);
                 
                return Ok(model);
            }

            var exist = await unitOfWork.Repository<ReportBuilderTable>()
                .TableNoTracking.AnyAsync(c => c.Id == model.Id);

            if (exist)
            {
                  await unitOfWork.Repository<ReportBuilderTable>().UpdateAsync(model, cn);
                 
                return Ok(model);
            }
            model = await unitOfWork.Repository<ReportBuilderTable>().AddAsync(model, cn);
             
            return Ok(model);
        }
      
        [HttpPost("[action]")]
        [ActionDisplayName("دریافت اطلاعات آیتم ها", ActionAccessType.Api)]
        public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
        {
            return Ok( );
        }

		[HttpGet("{action}")]
        [ActionDisplayName("لیست گزارش ها", ActionAccessType.View)]
		public async Task<IActionResult> ReportList()
		{
			// نکته: ستون Content می‌تواند حاوی Resource های حجیمی باشد که کاربر در طراح گزارش اضافه کرده
			// (مثل فایل اکسل/JSON/CSV به‌صورت Base64 embedded). برای جلوگیری از بارگذاری غیرضروری این حجم
			// در لیست گزارش‌ها (که اصلاً از Content استفاده نمی‌کند)، فقط فیلدهای لازم Select می‌شوند.
			var reports = await unitOfWork.Repository<ReportBuilderReport>()
				.TableNoTracking
				.OrderByDescending(c => c.ModifiedDateMiladiDateTime ?? c.CreatedOnMiladiDateTime)
				.Select(c => new ReportBuilderReport
				{
					Id = c.Id,
					Name = c.Name,
					Title = c.Title,
					Type = c.Type,
					ObjectType = c.ObjectType,
					SavedQueryId = c.SavedQueryId,
					IsActive = c.IsActive,
					CreatedByName = c.CreatedByName,
					CreatedOnMiladiDateTime = c.CreatedOnMiladiDateTime,
					ModifiedDateMiladiDateTime = c.ModifiedDateMiladiDateTime
				})
				.ToListAsync();
			return View("Views/GenerateReport/List.cshtml", reports);
		}
          [HttpGet("Report/{action}")]
          [ActionDisplayName("ویرایش گزارش ها", ActionAccessType.View)]
          public async Task<IActionResult> Edit(long id)
          {
               var model = await unitOfWork.Repository<ReportBuilderReport>()
                   .TableNoTracking.FirstOrDefaultAsync(c => c.Id == id);
               TempData["reportId"] = id;

			ViewBag.SavedQueries = await _queryService.GetAllSavedQueriesForReportsFillterByRoleAsync();
                

			return View("Views/GenerateReport/Edit.cshtml", model);
        }

        [HttpGet("{action}")]
        public async Task<IActionResult> GetItems(long id, [FromQuery] bool fromSavedQuery = false)
        {
            if (fromSavedQuery)
            {
                return await GetSavedQueryItems(id);
            }

            var dataObject = unitOfWork.Repository<ReportBuilderTable>()
                .GetById(id);
            dataObject.ReportBuilderTableFilters =
                dataObject.FiltersContent.JsonDeserialize<List<ReportBuilderTableItem>>() ?? new List<ReportBuilderTableItem>();

            dataObject.ReportBuilderTableSelects =
                dataObject.SelectsContent.JsonDeserialize<List<ReportBuilderTableSelect>>() ?? new List<ReportBuilderTableSelect>();

            return Ok(new {Items = dataObject.ReportBuilderTableSelects , Filters =dataObject.ReportBuilderTableFilters });
        }

        /// <summary>
        /// دریافت آیتم‌های گزارش از SavedQuery (QueryDesigner)
        /// </summary>
        private async Task<IActionResult> GetSavedQueryItems(long savedQueryId)
        {
            var report = await _queryService.GetReportAsync(savedQueryId);
            if (report == null)
                return NotFound();

            var columns = JsonSerializer.Deserialize<List<QueryColumn>>(report.ColumnsJson) ?? new List<QueryColumn>();
            var queryDesign =  JsonSerializer.Deserialize<QueryDesign>(report.QueryJson);

            var filters = (queryDesign?.Parameters ?? new List<QueryParameter>())
                .Select(p => new { id = p.Id, title = p.DisplayName ?? p.Name, columnName = p.Name, dataType = MapSystemTypeToDataType(p.DataType) })
                .ToList();

            var items = new List<object>
            {
                new
                {
                    reportBuilderReportItems = columns.Select(c => new
                    {
                        columnName = c.ColumnName,
                        dataType = MapSystemTypeToDataType(c.SystemType?.ToString() ?? "String"),
                        systemType = c.SystemType,
                        systemTypeName = c.SystemType.ToString(),
				    alliance =c.Alliance,
				    title = c.DisplayName ?? c.ColumnName
                    }).ToList()
                }
            };

            return Ok(new { Items = items, Filters = filters });
        }

        private static string MapSystemTypeToDataType(string systemType)
        {
            return systemType?.ToLower() switch
            {
                "datetime" or "datetimeshamsi" => "datetime",
                "date" or "dateshamsi" => "date",
                "long" or "int" or "decimal" => systemType?.ToLower() ?? "nvarchar",
                "boolean" => "bit",
                _ => "nvarchar"
            };
        }

        [HttpGet("{action}")]
        public async Task<IActionResult> GetItemsWithValue(long id , CancellationToken cn)
        {
             

            var reportBuilderTable = await unitOfWork.Repository<ReportBuilderTable>()
                .TableNoTracking.FirstOrDefaultAsync(c => c.Id == id , cn);


            var dataItems =  reportBuilderService.FetchSampleData(
                $"{reportBuilderTable.ObjectSchema}.{reportBuilderTable.ObjectName}");


            return Ok(new
            {
                dataSet  = dataItems,
                reportBuilderTable,
                addDataSource = new{}
            });
        }

          [HttpPost("[action]")]
          [ActionDisplayName("ذخیر گزارش ها", ActionAccessType.Api)]
          public async Task<IActionResult> SaveReportBuilderTable(ReportBuilderReport model, CancellationToken cn)
          {

               model.Content = reportJsonData;

               if (model.SavedQueryId != null)
               {
                 var query = await _queryService.GetReportAsync((long)model.SavedQueryId);
                  model.BaseQuery = JsonSerializer.Deserialize<QueryDesign>(query.QueryJson).CustomQuery;

			}

			if (model.Id == null || model.Id == 0)
            {
                model = await unitOfWork.Repository<ReportBuilderReport>().AddAsync(model, cn);
                reportJsonData = "";
                return Ok();
            }


            var exist = await unitOfWork.Repository<ReportBuilderReport>()
                .TableNoTracking.AnyAsync(c => c.Id == model.Id);

            if (exist)
            {

             var listSelects  =  await unitOfWork.Repository<ReportBuilderReportSelect>()
                    .TableNoTracking
                    .Include(c=>c.ReportBuilderReportItems)
                    .Where(c => c.ReportBuilderReportId == model.Id).ToListAsync(cn);

             var filters = await unitOfWork.Repository<ReportBuilderReportItem>()
                 .TableNoTracking
 
                 .Where(c => c.ReportBuilderReportId == model.Id).ToListAsync(cn);


             await unitOfWork.Repository<ReportBuilderReportItem>()
                 .DeleteRangeAsync(listSelects.SelectMany(c=>c.ReportBuilderReportItems).ToList(), cn);


                await unitOfWork.Repository<ReportBuilderReportSelect>()
                    .DeleteRangeAsync(listSelects, cn);

                await unitOfWork.Repository<ReportBuilderReportItem>()
                    .DeleteRangeAsync(filters, cn);

                await unitOfWork.Repository<ReportBuilderReport>().UpdateAsync(model, cn);

                reportJsonData = "";
                return Ok();
            }

            model = await unitOfWork.Repository<ReportBuilderReport>().AddAsync(model, cn);

            reportJsonData = "";
            return Ok();

             
        }


          [HttpGet("{action}/{id}")]
		[Authorize("AuthenticatedUser")]
		public async Task<IActionResult> ViewReport(long id)
        {
            var report = await unitOfWork.Repository<ReportBuilderReport>()
                .TableNoTracking
                .Include(c => c.ReportBuilderReportFilters)
                .Include(c => c.ReportBuilderReportSelects)
                .ThenInclude(c => c.ReportBuilderReportItems)
                .AsSplitQuery()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (report == null)
                return NotFound();


			var filters = new Dictionary<string, FilterParamter>();
               ViewBag.AutoRun = false;

			foreach (var q in Request.Query)
			{
                    if (q.Key == "autorun")
                    {
                         ViewBag.AutoRun = true;

				}
				else if (q.Key != "reportId" && q.Key != "itemId" && q.Key != "fromSavedQuery"  )
				{
					filters.Add(q.Key, q.Value.ToString().JsonDeserialize<FilterParamter>());
				}
			}
		
			ViewBag.QueryParams = filters;

			TempData["tableName"] = report.Name;
            TempData["reportId"] = report.Id;
            TempData["savedQueryId"] = report.SavedQueryId;

            ViewBag.TableColumns = await GetReportTableColumnsAsync(report);

            return View("Views/ViewReport/Index.cshtml", report);
        }

		[HttpGet("{action}/{name}")]
		[Authorize("AuthenticatedUser")]
		public async Task<IActionResult> ViewReportByName(string name)
		{
			var report = await unitOfWork.Repository<ReportBuilderReport>()
			    .TableNoTracking
			    .Include(c => c.ReportBuilderReportFilters)
			    .Include(c => c.ReportBuilderReportSelects)
			    .ThenInclude(c => c.ReportBuilderReportItems)
                   .AsSplitQuery()
			    .FirstOrDefaultAsync(c => c.Name == name);

			if (report == null)
				return NotFound();


			var filters = new Dictionary<string, FilterParamter>();
			ViewBag.AutoRun = false;

			foreach (var q in Request.Query)
			{
				if (q.Key == "autorun")
				{
					ViewBag.AutoRun = true;

				}
				else if (q.Key != "reportId" && q.Key != "itemId" && q.Key != "fromSavedQuery")
				{
					filters.Add(q.Key, q.Value.ToString().JsonDeserialize<FilterParamter>());
				}
			}

			ViewBag.QueryParams = filters;

			TempData["tableName"] = report.Name;
			TempData["reportId"] = report.Id;
			TempData["savedQueryId"] = report.SavedQueryId;

			ViewBag.TableColumns = await GetReportTableColumnsAsync(report);

			return View("Views/ViewReport/Index.cshtml", report);
		}

		private async Task<List<ReportColumnConfig>> GetReportTableColumnsAsync(ReportBuilderReport report)
        {
            var columns = new List<ReportColumnConfig>();
            var items = report.ReportBuilderReportSelects?.FirstOrDefault()?.ReportBuilderReportItems;
            if (items != null && items.Any())
            {
                foreach (var col in items)
                {
                    columns.Add(new ReportColumnConfig
                    {
                        Data = col.Alliance,
                        Type = col.DataType.ConvertSqlTypeToCSharpType(),
                        Name = col.ColumnName.ToCamelCase(),
                        Title = col.Title ?? col.ColumnName,
                        ShowInRelationData = false,
                        Options = Array.Empty<object>(),
                        TableName = ""
                    });
                }
                return columns;
            }
            if (report.SavedQueryId.HasValue && report.SavedQueryId > 0)
            {
                var savedReport = await _queryService.GetReportAsync(report.SavedQueryId.Value);
                if (savedReport != null)
                {
                    var reportCols =  JsonSerializer.Deserialize<List<QueryColumn>>(savedReport.ColumnsJson) ?? new List<QueryColumn>();
                    foreach (var col in reportCols)
                    {
                        var dataType = MapSystemTypeToDataType(col.SystemType?.ToString() ?? "String");
                        columns.Add(new ReportColumnConfig
                        {
                            Data = col.ColumnName.ToCamelCase(),
                            Type = dataType.ConvertSqlTypeToCSharpType(),
                            Name = col.ColumnName.ToCamelCase(),
                            Title = col.DisplayName ?? col.ColumnName,
                            ShowInRelationData = false,
                            Options = Array.Empty<object>(),
                            TableName = ""
                        });
                    }
                }
            }
            return columns;
        }



          [HttpPost("{action}")]
		[Authorize("AuthenticatedUser")]
		public async Task<IActionResult> FetchDataReport(DataTableRequest request , CancellationToken cn )
        {
            TempData.Keep("tableName");
            TempData.Keep("savedQueryId");
            TempData.Keep("reportId");

            var savedQueryId = GetSavedQueryIdFromTempData(TempData);
            if (savedQueryId == null && TempData["reportId"] != null)
            {
                var reportId = Convert.ToInt64(TempData["reportId"].ToString());
                var report = await unitOfWork.Repository<ReportBuilderReport>()
                    .TableNoTracking.FirstOrDefaultAsync(c => c.Id == reportId, cn);
                savedQueryId = report?.SavedQueryId;
            }

            var paramValues = _queryService.ExtractParameterValuesFromRequest(request);


		  if (savedQueryId.HasValue && savedQueryId.Value > 0)
            {
        
                QueryResult result;
                if (request.length > 0)
                {
                    result = await _queryService.ExecuteReportAsync(savedQueryId.Value, paramValues, request.start, request.length, User?.Identity?.Name, User?.Identity?.Name);
                }
                else
                {
                    result = await _queryService.ExecuteReportAsync(savedQueryId.Value, paramValues, User?.Identity?.Name, User?.Identity?.Name);
                }
                var response = new DataTableResponse
                {
                    Draw = request.draw,
                    RecordsTotal = result.TotalRows,
                    RecordsFiltered = result.TotalRows,
                    Data = result.Rows
                };
                return Ok(response);
            }

            if (TempData.ContainsKey("tableName"))
                request.TableName = TempData["tableName"].ToString();

            return Ok(await reportBuilderService.FetchData(request, cn));
        }

        private static long? GetSavedQueryIdFromTempData(ITempDataDictionary tempData)
        {
            var val = tempData["savedQueryId"];
            if (val == null) return null;
            if (val is long l && l > 0) return l;
            if (val is int i && i > 0) return i;
            if (val is string s && long.TryParse(s, out var parsed) && parsed > 0) return parsed;
            return null;
        }

     
        [HttpPost("{action}")]
        [Authorize("AuthenticatedUser")]
        public async Task<IActionResult> FetchDataReportStoreProc(FetchDataReportStoreProcViewModelReq request, CancellationToken cn = default)
        {

            if (TempData.ContainsKey("tableName"))
                request.StoreProcName = TempData["tableName"].ToString();

            TempData.Keep("tableName");
            var data =   reportBuilderService.FetchDataStoreProc(request);


            return Ok(data);

        }
      

          [HttpPost("ExportDataToExcel")]
		[Authorize("AuthenticatedUser")]
		public async Task<IActionResult> ExportLargeDataToExcel(DataTableRequest request, CancellationToken cn = default)
        {
            var licensePath = AppDomain.CurrentDomain.BaseDirectory + "\\Aspose.Total.NET.lic";
            var memoryStream = new MemoryStream();

            TempData.Keep("tableName");
            TempData.Keep("savedQueryId");
            TempData.Keep("reportId");

            var savedQueryId = GetSavedQueryIdFromTempData(TempData);
            if (savedQueryId == null && TempData["reportId"] != null)
            {
                var reportId = Convert.ToInt64(TempData["reportId"].ToString());
                var report = await unitOfWork.Repository<ReportBuilderReport>()
                    .TableNoTracking.FirstOrDefaultAsync(c => c.Id == reportId, cn);
                savedQueryId = report?.SavedQueryId;
            }

			var paramValues = _queryService.ExtractParameterValuesFromRequest(request);

			try
            {
                if (savedQueryId.HasValue && savedQueryId.Value > 0)
                {
          
                    var result = await _queryService.ExecuteReportAsync(savedQueryId.Value, paramValues, User?.Identity?.Name, User?.Identity?.Name);
                    await ExportSavedQueryToExcelAsync(result, memoryStream, licensePath);
                }
                else
                {
                    request.TableName = request?.TableName ?? TempData["tableName"]?.ToString();
                    await reportBuilderService.ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
                }

                memoryStream.Position = 0;
                return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred: " + ex.Message);
            }
        }

        private static async Task ExportSavedQueryToExcelAsync(QueryResult result, Stream outputStream, string licensePath)
        {
            var workbook = new Aspose.Cells.Workbook();
            if (!workbook.IsLicensed)
                new Aspose.Cells.License().SetLicense(licensePath);

            var worksheet = workbook.Worksheets[0];
            worksheet.Name = "Report";

            if (result.Rows != null && result.Rows.Any())
            {
                var headers = result.Rows.First().Keys.ToList();
                for (var i = 0; i < headers.Count; i++)
                    worksheet.Cells[0, i].PutValue(headers[i]);

                var row = 1;
                foreach (var record in result.Rows)
                {
                    var col = 0;
                    foreach (var key in headers)
                    {
                        var value = record.TryGetValue(key, out var v) ? v : null;
                        worksheet.Cells[row, col].PutValue(value?.ToString() ?? "");
                        col++;
                    }
                    row++;
                }
            }

            worksheet.AutoFitColumns();
            workbook.Save(outputStream, Aspose.Cells.SaveFormat.Xlsx);
            await Task.CompletedTask;
        }

        [HttpGet("{action}")]
        public IActionResult StimulSoftReport(long? itemId, long? reportId, [FromQuery] bool fromSavedQuery = false)
        {
            StiLicense.Key = "6vJhGtLLLz2GNviWmUTrhSqnOItdDwjBylQzQcAOiHkO46nMQvol4ASeg91in+mGJLnn2KMIpg3eSXQSgaFOm15+0l" +
                             "hekKip+wRGMwXsKpHAkTvorOFqnpF9rchcYoxHXtjNDLiDHZGTIWq6D/2q4k/eiJm9fV6FdaJIUbWGS3whFWRLPHWC" +
                             "BsWnalqTdZlP9knjaWclfjmUKf2Ksc5btMD6pmR7ZHQfHXfdgYK7tLR1rqtxYxBzOPq3LIBvd3spkQhKb07LTZQoyQ" +
                             "3vmRSMALmJSS6ovIS59XPS+oSm8wgvuRFqE1im111GROa7Ww3tNJTA45lkbXX+SocdwXvEZyaaq61Uc1dBg+4uFRxv" +
                             "yRWvX5WDmJz1X0VLIbHpcIjdEDJUvVAN7Z+FW5xKsV5ySPs8aegsY9ndn4DmoZ1kWvzUaz+E1mxMbOd3tyaNnmVhPZ" +
                             "eIBILmKJGN0BwnnI5fu6JHMM/9QR2tMO1Z4pIwae4P92gKBrt0MqhvnU1Q6kIaPPuG2XBIvAWykVeH2a9EP6064e11" +
                             "PFCBX4gEpJ3XFD0peE5+ddZh+h495qUc1H2B";

            if (itemId != null)
                TempData["itemId"] = itemId;
            if (reportId != null)
                TempData["reportId"] = reportId;
            TempData["fromSavedQuery"] = fromSavedQuery;

 

			var dic = new Dictionary<string, string>();

			foreach (var q in Request.Query)
			{
				var spVal = q.Value.ToString();
				dic.Add(q.Key, spVal);
			}

			ViewBag.ReportId = reportId;
			ViewBag.QueryParams = dic;

			return View("Views/GenerateReport/stimulsoft.cshtml");
        }

       

        public class FilterParamter
        {
            public string[] values { get; set; }
            public string condition { get; set; } = "";
        }
    [HttpGet("[action]")]

        public IActionResult StimulSoftViewReport(long? reportId)
        {
	        StiLicense.Key = "6vJhGtLLLz2GNviWmUTrhSqnOItdDwjBylQzQcAOiHkO46nMQvol4ASeg91in+mGJLnn2KMIpg3eSXQSgaFOm15+0l" +
	                         "hekKip+wRGMwXsKpHAkTvorOFqnpF9rchcYoxHXtjNDLiDHZGTIWq6D/2q4k/eiJm9fV6FdaJIUbWGS3whFWRLPHWC" +
	                         "BsWnalqTdZlP9knjaWclfjmUKf2Ksc5btMD6pmR7ZHQfHXfdgYK7tLR1rqtxYxBzOPq3LIBvd3spkQhKb07LTZQoyQ" +
	                         "3vmRSMALmJSS6ovIS59XPS+oSm8wgvuRFqE1im111GROa7Ww3tNJTA45lkbXX+SocdwXvEZyaaq61Uc1dBg+4uFRxv" +
	                         "yRWvX5WDmJz1X0VLIbHpcIjdEDJUvVAN7Z+FW5xKsV5ySPs8aegsY9ndn4DmoZ1kWvzUaz+E1mxMbOd3tyaNnmVhPZ" +
	                         "eIBILmKJGN0BwnnI5fu6JHMM/9QR2tMO1Z4pIwae4P92gKBrt0MqhvnU1Q6kIaPPuG2XBIvAWykVeH2a9EP6064e11" +
	                         "PFCBX4gEpJ3XFD0peE5+ddZh+h495qUc1H2B";
	        
	        if (reportId != null)
	        {
		        TempData["reportId"] = reportId;
            }


			var dic = new Dictionary<string, string>();

			foreach (var q in Request.Query)
			{
				var spVal = q.Value.ToString();
				dic.Add(q.Key, spVal);
			}

			ViewBag.ReportId = reportId;
			ViewBag.QueryParams = dic;

			return View("Views/GenerateReport/StimulSoftView.cshtml");
		}

        [HttpGet("[action]")]
        [HttpPost("[action]")]
        public IActionResult DesignerEvent()
        {
            TempData.Keep("itemId");
            TempData.Keep("reportId");
            TempData.Keep("fromSavedQuery");

		

			var report2 = StiNetCoreDesigner.GetReportObject(this);

            if (report2 != null)
            {
                reportJsonData = report2.SaveToJsonString();

            }
           

			return StiNetCoreDesigner.DesignerEventResult(this);
        }
		[HttpPost("[action]")]
		public async Task<IActionResult> GetReport()
		{
			var filters = new Dictionary<string, string>();

			foreach (var q in Request.Query)
			{
				if (q.Key != "reportId" && q.Key != "itemId" && q.Key != "fromSavedQuery")
				{
					filters.Add(q.Key, q.Value.ToString());
				}
			}

			long itemId = Request.Query.ContainsKey("itemId")
			    ? Request.Query["itemId"].ToString().ToLong()
			    : 0;

			long reportId = Request.Query.ContainsKey("reportId")
			    ? Request.Query["reportId"].ToString().ToLong()
			    : 0;

			bool fromSavedQuery = Request.Query.ContainsKey("fromSavedQuery")
			    && bool.TryParse(Request.Query["fromSavedQuery"], out var b) && b;

			var report = StiReport.CreateNewReport();

			if (reportId != 0)
			{
				var existReport = unitOfWork.Repository<ReportBuilderReport>()
				    .TableNoTracking
				    .FirstOrDefault(c => c.Id == reportId);

				if (existReport?.Content != null && existReport.Content.HasValue())
					report.LoadFromJson(existReport.Content);
			}

			// نکته مهم: اینجا دیگر Dictionary/DataSources/Databases به‌صورت کامل پاک نمی‌شوند.
			// این کار قبلاً باعث می‌شد هر Resource ای که کاربر خودش در طراح اضافه کرده (مثل فایل اکسل/JSON/CSV/XML
			// که به‌صورت StiExcelDatabase/StiJsonDatabase/... و DataSource مرتبط با آن در Dictionary ذخیره شده)
			// در هر بار باز شدن گزارش در طراح از بین برود. RegData به‌صورت خودکار DataSource هایی که هم‌نام باشند
			// (مثل "Report"/"Select1"/...) را به‌روزرسانی می‌کند، نه این‌که تکراری اضافه کند؛ پس نیازی به پاک کردن نیست.
			DataSet dataSet;

			if (fromSavedQuery && itemId > 0)
			{
				dataSet = await BuildDataSetFromSavedQueryAsync(itemId, filters);
			}
			else if (itemId > 0)
			{
				var dataObject = unitOfWork.Repository<ReportBuilderTable>().GetById(itemId);

				if (dataObject == null)
					return BadRequest("آیتم گزارش یافت نشد");

				dataObject.ReportBuilderTableFilters =
				    dataObject.FiltersContent.JsonDeserialize<List<ReportBuilderTableItem>>() ?? new List<ReportBuilderTableItem>();

				dataObject.ReportBuilderTableSelects =
				    dataObject.SelectsContent.JsonDeserialize<List<ReportBuilderTableSelect>>() ?? new List<ReportBuilderTableSelect>();

				dataSet = BuildDataSetFromReportBuilderTable(dataObject);
			}
			else
			{
				dataSet = new DataSet();
			}

			if (dataSet.Tables.Count > 0)
				report.RegData(dataSet);

			AddFontResourcesToReport(report);
			report.Dictionary.Synchronize();

			return StiNetCoreDesigner.GetReportResult(this, report);
		}


		private async Task<DataSet> BuildDataSetFromSavedQueryAsync(long savedQueryId , Dictionary<string,string> defualtParams)
        {
            try
            {
                var userName = User?.Identity?.Name ?? string.Empty;
                // از ExecuteReportMultiAsync استفاده می‌شود تا وقتی Query چند SELECT دارد (حالت نوشتن Query)،
                // dataset مربوط به هر SELECT به‌صورت جداگانه به DataSources/Dictionary طراح اضافه شود.
                var multiResult = await _queryService.ExecuteReportMultiAsync(savedQueryId, defualtParams, userName, userName);
                return BuildDataSetFromMultiResults(multiResult.ResultSets, multiResult.Selects, maxRowsPerTable: 100);
            }
            catch
            {
                // در صورت خطا، DataSet خالی بازگردانده می‌شود
                return new DataSet();
            }
        }

        /// <summary>
        /// ساخت DataSet از نتیجهٔ اجرای یک SavedQuery که ممکن است چند SELECT داشته باشد.
        /// وقتی فقط یک result-set وجود دارد (رفتار قبلی/رایج)، دقیقاً مثل قبل یک DataTable به نام "Report" ساخته می‌شود
        /// تا گزارش‌های موجود که با این نام طراحی شده‌اند دچار مشکل نشوند.
        /// وقتی چند result-set وجود دارد، برای هرکدام یک DataTable مجزا (با نام برگرفته از QuerySelectInfo، یا Select{n} به‌صورت پیش‌فرض) ساخته می‌شود.
        /// </summary>
        private static DataSet BuildDataSetFromMultiResults(
            List<QueryResult> resultSets,
            List<QuerySelectInfo> selects,
            int? maxRowsPerTable,
            List<ReportBuilderReportItem> filtersForDateFormatting = null)
        {
            var dataSet = new DataSet();
            if (resultSets == null || resultSets.Count == 0)
                return dataSet;

            if (resultSets.Count == 1)
            {
                var dt = BuildDataTableFromQueryResult(resultSets[0], "Report", maxRowsPerTable, filtersForDateFormatting);
                if (dt.Columns.Count > 0)
                    dataSet.Tables.Add(dt);
                return dataSet;
            }

            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < resultSets.Count; i++)
            {
                var meta = selects?.FirstOrDefault(s => s.Index == i);
                var baseName = SanitizeTableName(meta?.Name, $"Select{i + 1}");
                var uniqueName = MakeUniqueTableName(baseName, usedNames);
                usedNames.Add(uniqueName);

                var dt = BuildDataTableFromQueryResult(resultSets[i], uniqueName, maxRowsPerTable, filtersForDateFormatting);
                if (dt.Columns.Count > 0)
                    dataSet.Tables.Add(dt);
            }
            return dataSet;
        }

        private static DataTable BuildDataTableFromQueryResult(
            QueryResult result,
            string tableName,
            int? maxRows,
            List<ReportBuilderReportItem> filtersForDateFormatting)
        {
            var dt = new DataTable(tableName);
            if (result?.ColumnNames == null || result.ColumnNames.Count == 0)
                return dt;

            foreach (var col in result.ColumnNames)
            {
                if (!dt.Columns.Contains(col))
                    dt.Columns.Add(col);
            }

            if (result.Rows == null || result.Rows.Count == 0)
                return dt;

            var rows = maxRows.HasValue ? result.Rows.Take(maxRows.Value) : result.Rows;
            foreach (var record in rows)
            {
                var row = dt.NewRow();
                foreach (var kv in record)
                {
                    if (!dt.Columns.Contains(kv.Key)) continue;

                    var value = kv.Value;
                    if (filtersForDateFormatting != null && value != null && value != DBNull.Value)
                    {
                        var colType = filtersForDateFormatting
                            .FirstOrDefault(c => c.ColumnName?.Equals(kv.Key, StringComparison.OrdinalIgnoreCase) == true)?.DataType;
                        if (colType is "datetime" or "datetime2")
                        {
                            try { value = DateTime.Parse(value.ToString()!).ToShamsiDateTime(); }
                            catch { /* در صورت خطا مقدار اصلی حفظ می‌شود */ }
                        }
                    }
                    row[kv.Key] = value ?? DBNull.Value;
                }
                dt.Rows.Add(row);
            }
            return dt;
        }

        private static string SanitizeTableName(string name, string fallback)
        {
            if (string.IsNullOrWhiteSpace(name))
                return fallback;

            var sanitized = Regex.Replace(name.Trim(), @"[^A-Za-z0-9_\u0600-\u06FF]", "_");
            if (sanitized.Length == 0)
                return fallback;
            if (char.IsDigit(sanitized[0]))
                sanitized = "_" + sanitized;

            return sanitized;
        }

        private static string MakeUniqueTableName(string baseName, HashSet<string> usedNames)
        {
            var candidate = baseName;
            var suffix = 1;
            while (usedNames.Contains(candidate))
            {
                candidate = $"{baseName}_{suffix}";
                suffix++;
            }
            return candidate;
        }

        private DataSet BuildDataSetFromReportBuilderTable(ReportBuilderTable dataObject)
        {
            var dataSet = new DataSet();
            var tempFilters = BuildDefaultFilters(dataObject.ReportBuilderTableFilters ?? new List<ReportBuilderTableItem>());

            if (dataObject.ObjectType == "Store_Procedures")
            {
                var tmpData = reportBuilderService.FetchDataStoreProc(new FetchDataReportStoreProcViewModelReq
                {
                    StoreProcName = dataObject.ObjectSchema + "." + dataObject.ObjectName,
                    Filters = tempFilters
                });

                var selects = dataObject.ReportBuilderTableSelects ?? new List<ReportBuilderTableSelect>();
                for (var i = 0; i < tmpData.Count && i < selects.Count; i++)
                {
                    var resultSet = tmpData[i];
                    if (resultSet == null || resultSet.Count == 0) continue;

                    var tableTitle = selects[i].Title ?? $"ResultSet{i}";
                    var dt = CreateDataTableFromDictionary(resultSet, tableTitle, 20);
                    dataSet.Tables.Add(dt);
                }
            }
            else
            {
                var tmpData = reportBuilderService.FetchSampleData(dataObject.ObjectSchema + "." + dataObject.ObjectName);
                if (tmpData != null && tmpData.Count > 0)
                {
                    var tableTitle = dataObject.ReportBuilderTableSelects?.FirstOrDefault()?.Title ?? dataObject.Title ?? "Data";
                    var dt = CreateDataTableFromDictionary(tmpData, tableTitle);
                    dataSet.Tables.Add(dt);
                }
            }
            return dataSet;
        }

        private static List<FetchDataReportStoreProcViewModelReq.FetchDataReportStoreProcViewModelReqFilters> BuildDefaultFilters(
            List<ReportBuilderTableItem> filters)
        {
            var tempFilters = new List<FetchDataReportStoreProcViewModelReq.FetchDataReportStoreProcViewModelReqFilters>();
            foreach (var z in filters)
            {
                var colType = z.DataType?.ToLowerInvariant() ?? "";
                var neFilter = new FetchDataReportStoreProcViewModelReq.FetchDataReportStoreProcViewModelReqFilters
                {
                    ColumnName = z.ColumnName,
                    Value = colType switch
                    {
                        "datetime" or "datetime2" => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff"),
                        "nvarchar" or "varchar" or "nchar" or "char" => "",
                        "bigint" or "int" or "integer" => "1",
                        "bit" or "boolean" or "bool" => "0",
                        "uniqueidentifier" => Guid.NewGuid().ToString(),
                        _ => ""
                    }
                };
                tempFilters.Add(neFilter);
            }
            return tempFilters;
        }

        private static DataTable CreateDataTableFromDictionary(
            List<Dictionary<string, object>> data,
            string tableName,
            int? maxRows = null)
        {
            var dt = new DataTable(tableName);
            if (data.Count == 0) return dt;

            foreach (var col in data[0].Keys)
                dt.Columns.Add(col);

            var rows = maxRows.HasValue ? data.Take(maxRows.Value) : data;
            foreach (var record in rows)
            {
                var row = dt.NewRow();
                foreach (var kv in record)
                    row[kv.Key] = kv.Value ?? DBNull.Value;
                dt.Rows.Add(row);
            }
            return dt;
        }

        private static void AddFontResourcesToReport(StiReport report)
        {
			RegisterStimulsoftFonts();

            foreach (var f in Fonts)
            {
				// فقط TTF معتبر است؛ eot/woff قبلاً اشتباه به‌عنوان FontTtf ثبت می‌شدند.
				if (f.Type != StiResourceType.FontTtf)
					continue;

                byte[] fileContent;
                string resourceName = Path.GetFileNameWithoutExtension(f.Name);
                if (!f.Loaded)
                {
                    var path = ResolveFontFilePath(f.Name);
                    if (path == null) continue;
                    fileContent = System.IO.File.ReadAllBytes(path);
                    f.Loaded = true;
                    f.Bytes = fileContent;
                }
                else
                {
                    fileContent = f.Bytes ?? Array.Empty<byte>();
                }

				if (fileContent.Length == 0)
					continue;

				// availableInTheViewer=true ضروری است تا Viewer فونت را به کلاینت بفرستد.
				// اگر Resource از قبل در Content ذخیره شده (گاهی با AvailableInTheViewer=false یا بایت خراب)،
				// آن را با فایل TTF درست به‌روزرسانی می‌کنیم.
				if (report.Dictionary.Resources.Contains(resourceName))
				{
					var existing = report.Dictionary.Resources[resourceName];
					existing.Type = f.Type;
					existing.Content = fileContent;
					existing.AvailableInTheViewer = true;
					if (string.IsNullOrWhiteSpace(existing.Alias))
						existing.Alias = resourceName;
				}
				else
				{
					var resource = new StiResource(resourceName, resourceName, false, f.Type, fileContent, true);
					report.Dictionary.Resources.Add(resource);
				}
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> GetViewReport()
        {
            var reportId = TempData.ContainsKey("reportId") ? (TempData["reportId"]?.ToString() ?? "0").ToLong() : 0L;
            var paramsQuery = TempData.ContainsKey("queryString")
                ? (TempData["queryString"]?.ToString() ?? "{}").JsonDeserialize<Dictionary<string, FilterParamter>>() ?? new Dictionary<string, FilterParamter>()
                : new Dictionary<string, FilterParamter>();



               var filters = new Dictionary<string, FilterParamter>();

			foreach (var q in Request.Query)
			{
				if (q.Key != "reportId" && q.Key != "itemId" && q.Key != "fromSavedQuery")
				{
					filters.Add(q.Key, q.Value.ToString().JsonDeserialize<FilterParamter>());
				}
			}


			var existReport = unitOfWork.Repository<ReportBuilderReport>()
	                  .TableNoTracking
	                  .AsSplitQuery()
	                  .Include(c => c.ReportBuilderReportFilters)
	                  .Include(c => c.ReportBuilderReportSelects)
		                 .ThenInclude(c => c.ReportBuilderReportItems)
	                  .FirstOrDefault(c => c.Id == reportId);


			if (existReport == null)
                return BadRequest("گزارش یافت نشد");

            var report = StiReport.CreateNewReport();
            report.LoadFromJson(existReport.Content);

            // نکته مهم: Dictionary/DataSources/Databases اینجا دیگر پاک نمی‌شوند تا Resource هایی که کاربر
            // خودش در طراح اضافه کرده (مثلاً فایل اکسل/JSON/CSV/XML) از بین نروند. RegData به‌صورت خودکار
            // DataSource های هم‌نام (مثل "Report"/"Select1"/...) را با داده جدید به‌روزرسانی می‌کند.
            DataSet dataSet = existReport.ObjectType == "Store_Procedures"
                ? BuildDataSetFromStoreProc(existReport, filters)
                : await BuildDataSetFromTableOrQuery(existReport, filters);

			report.RegData(dataSet);
			AddFontResourcesToReport(report);
            report.Dictionary.Synchronize();

            return StiNetCoreViewer.GetReportResult(this, report);
        }

        private DataSet BuildDataSetFromStoreProc(ReportBuilderReport existReport, Dictionary<string, FilterParamter> paramsQuery)
        {
            var filters = existReport.ReportBuilderReportFilters ?? new List<ReportBuilderReportItem>();
            var tempFilters = new List<FetchDataReportStoreProcViewModelReq.FetchDataReportStoreProcViewModelReqFilters>();
            foreach (var x in paramsQuery)
            {
                var filterDef = filters.FirstOrDefault(c => c.ColumnName?.Equals(x.Key, StringComparison.OrdinalIgnoreCase) == true);
                if (filterDef == null) continue;

                var val = x.Value?.values?.FirstOrDefault();
                tempFilters.Add(new FetchDataReportStoreProcViewModelReq.FetchDataReportStoreProcViewModelReqFilters
                {
                    ColumnName = x.Key,
                    Value = val ?? ""
                });
            }

            var tmpData = reportBuilderService.FetchDataStoreProc(new FetchDataReportStoreProcViewModelReq
            {
                StoreProcName = existReport.Name,
                Filters = tempFilters
            });

            var dataSet = new DataSet();
            var selects = existReport.ReportBuilderReportSelects ?? new List<ReportBuilderReportSelect>();
            for (var i = 0; i < tmpData.Count && i < selects.Count; i++)
            {
                var resultSet = tmpData[i];
                if (resultSet == null || resultSet.Count == 0) continue;

                var tableName = selects[i].Title ?? $"ResultSet{i}";
                var dt = CreateDataTableFromDictionaryForView(resultSet, tableName, existReport);
                dataSet.Tables.Add(dt);
            }
            return dataSet;
        }

        private async Task<DataSet> BuildDataSetFromTableOrQuery(ReportBuilderReport existReport, Dictionary<string, FilterParamter> paramsQuery)
        {
            var filters = existReport.ReportBuilderReportFilters ?? new List<ReportBuilderReportItem>();

            var paramValues = paramsQuery.ToDictionary(
                x => x.Key,
                x => x.Value?.values?.FirstOrDefault() ?? "",
                StringComparer.OrdinalIgnoreCase);

            // گزارش‌های ساخته‌شده از SavedQuery (QueryDesigner) ممکن است چند SELECT داشته باشند؛
            // برای این‌که همهٔ dataset های آن به‌درستی به Dictionary/DataSources استیمول‌سافت اضافه شوند
            // مستقیماً از طریق QueryService (که از NextResultAsync پشتیبانی می‌کند) اجرا می‌شوند.
            if (existReport.SavedQueryId.HasValue && existReport.SavedQueryId.Value > 0)
            {
                try
                {
                    var userName = User?.Identity?.Name ?? string.Empty;
                    var multiResult = await _queryService.ExecuteReportMultiAsync(existReport.SavedQueryId.Value, paramValues, userName, userName);
                    return BuildDataSetFromMultiResults(multiResult.ResultSets, multiResult.Selects, maxRowsPerTable: null, filters);
                }
                catch
                {
                    return new DataSet();
                }
            }

            var tempFilters = new List<FetchDataReportTableViewModelReq.FetchDataReportTableViewModelReqFilters>();
            foreach (var x in paramsQuery)
            {
                var filterDef = filters.FirstOrDefault(c => c.ColumnName?.Equals(x.Key, StringComparison.OrdinalIgnoreCase) == true);
                if (filterDef == null) continue;

                tempFilters.Add(new FetchDataReportTableViewModelReq.FetchDataReportTableViewModelReqFilters
                {
                    ColumnName = x.Key,
                    Values = x.Value?.values ?? Array.Empty<string>(),
                    Condition = x.Value?.condition ?? "="
                });
            }

            var tmpData = reportBuilderService.FetchTableDataStimilSoft(new FetchDataReportTableViewModelReq
            {
                TableName = existReport.Name,
                BaseQuery = existReport.BaseQuery,
                ParameterValues = !string.IsNullOrWhiteSpace(existReport.BaseQuery) ? paramValues : null,
                Filters = tempFilters
            });

            var dataSet = new DataSet();
            if (tmpData == null || tmpData.Count == 0)
                return dataSet;

            var tableName = existReport.ReportBuilderReportSelects?.FirstOrDefault()?.Title ?? existReport.Title ?? "Data";
            var dt = CreateDataTableFromDictionaryForView(tmpData, tableName, existReport);
            dataSet.Tables.Add(dt);
            return dataSet;
        }

        private static DataTable CreateDataTableFromDictionaryForView(
            List<Dictionary<string, object>> data,
            string tableName,
            ReportBuilderReport existReport)
        {
            var dt = new DataTable("Report");
            if (data.Count == 0) return dt;

            foreach (var col in data[0].Keys)
                dt.Columns.Add(col);

            var filters = existReport.ReportBuilderReportFilters ?? new List<ReportBuilderReportItem>();
            foreach (var record in data)
            {
                var row = dt.NewRow();
                foreach (var kv in record)
                {
                    var colType = filters.FirstOrDefault(c => c.ColumnName?.Equals(kv.Key, StringComparison.OrdinalIgnoreCase) == true)?.DataType;
                    if (colType is "datetime" or "datetime2" && kv.Value != null && kv.Value != DBNull.Value)
                    {
                        try
                        {
                            row[kv.Key] = DateTime.Parse(kv.Value.ToString()!).ToShamsiDateTime();
                        }
                        catch
                        {
                            row[kv.Key] = kv.Value;
                        }
                    }
                    else
                    {
                        row[kv.Key] = kv.Value ?? DBNull.Value;
                    }
                }
                dt.Rows.Add(row);
            }
            return dt;
        }

       
       
        [HttpGet("[action]")]
        [HttpPost("[action]")]
		public IActionResult ViewerEvent()
        {
            return StiNetCoreViewer.ViewerEventResult(this);
        }
        [HttpPost("[action]")]
        public IActionResult SaveReport()
        {
            TempData.Keep("itemId");
            TempData.Keep("reportId");
            TempData.Keep("fromSavedQuery");

            var report2 = StiNetCoreDesigner.GetReportObject(this);


            reportJsonData = report2.SaveToJsonString();

            return StiNetCoreDesigner.SaveReportResult(this);
        }


 


	}
}
