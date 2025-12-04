using System.Data;
using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReportBuilder.Entities;
using ReportBuilder.Services;
using ReportBuilder.Services.Contracts;
using Stimulsoft.Base;
using Stimulsoft.Report;
using Stimulsoft.Report.Dictionary;
using Stimulsoft.Report.Mvc;
using WebFramework.Filtters;

namespace ReportBuilder.WebApp.Controllers
{
    [ApiController]
    [ApiResultFilter]
    [Route("System/[controller]")]
 
    [Authorize("AuthenticatedUser")]
    [ControllerInfo("گزارش ساز")]
    public class ReportBuilderController(IReportBuilderService reportBuilderService , IUnitOfWork unitOfWork  ) : Controller
    {
        
        public static string reportJsonData = "";

         
        public static List<ReportBuilderReportFonts> Fonts= new ()
        {
           new (){Name = "IRANSansWeb.ttf",Type = StiResourceType.FontTtf},
           new (){Name = "IRANSansWeb.eot",Type = StiResourceType.FontEot},
           new (){Name = "IRANSansWeb.woff",Type = StiResourceType.FontWoff},
           new (){Name = "IRANSansWeb.woff2",Type = StiResourceType.FontWoff},
           new (){Name = "IRANSansWeb_Bold.ttf",Type = StiResourceType.FontTtf},
           new (){Name = "IRANSansWeb_Bold.woff",Type = StiResourceType.FontWoff},
           new (){Name = "IRANSansWeb_Bold.woff2",Type = StiResourceType.FontWoff},
           new (){Name = "IRANSansWeb_Light.eot",Type = StiResourceType.FontEot},
           new (){Name = "IRANSansWeb_Light.ttf",Type = StiResourceType.FontTtf},
           new (){Name = "IRANSansWeb_Light.woff2",Type = StiResourceType.FontTtf},
           new (){Name = "IRANSansWeb_Medium.eot",Type = StiResourceType.FontEot},
           new (){Name = "IRANSansWeb_Medium.ttf",Type = StiResourceType.FontTtf},
           new (){Name = "IRANSansWeb_Medium.woff",Type = StiResourceType.FontWoff},
           new (){Name = "IRANSansWeb_Medium.woff2",Type = StiResourceType.FontWoff},
           new (){Name = "IRANSansWeb_UltraLight.eot",Type = StiResourceType.FontEot},
           new (){Name = "IRANSansWeb_UltraLight.ttf",Type = StiResourceType.FontTtf},
           new (){Name = "IRANSansWeb_UltraLight.woff",Type = StiResourceType.FontWoff},
           new (){Name = "IRANSansWeb_UltraLight.woff2",Type = StiResourceType.FontWoff}
        };

        [HttpGet("{action}")]
        [ActionDisplayName("لیست آیتم ها", ActionAccessType.View)]
        public IActionResult List()
        {
      
              return View("Views/GenerateItem/List.cshtml");
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
        public IActionResult ReportList()
		{
			return View("Views/GenerateReport/List.cshtml");
		}
        [HttpGet("Report/{action}")]
        [ActionDisplayName("ویرایش گزارش ها", ActionAccessType.View)]
        public async Task<IActionResult> Edit(long id)
        {
 

            var model = await unitOfWork.Repository<ReportBuilderReport>()
                .TableNoTracking.FirstOrDefaultAsync(c=>c.Id == id);


            return View("Views/GenerateReport/Edit.cshtml" , model);
        }

        [HttpGet("{action}")]
        public async Task<IActionResult> GetItems(long id)
        {
            var dataObject = unitOfWork.Repository<ReportBuilderTable>()
                .GetById(id);
            dataObject.ReportBuilderTableFilters =
                dataObject.FiltersContent.JsonDeserialize<List<ReportBuilderTableItem>>() ?? new List<ReportBuilderTableItem>();

            dataObject.ReportBuilderTableSelects =
                dataObject.SelectsContent.JsonDeserialize<List<ReportBuilderTableSelect>>() ?? new List<ReportBuilderTableSelect>();

            return Ok(new {Items = dataObject.ReportBuilderTableSelects , Filters =dataObject.ReportBuilderTableFilters });
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
            var report = await unitOfWork.Repository<ReportBuilderReport>().
                TableNoTracking
                .Include(c=>c.ReportBuilderReportFilters)
                .Include(c=>c.ReportBuilderReportSelects)
                .ThenInclude(c=>c.ReportBuilderReportItems)
                .FirstOrDefaultAsync(c => c.Id == id);

            

              TempData["tableName"] = report.Name;
 

            return View("Views/ViewReport/Index.cshtml", report);
        }



        [HttpPost("{action}")]
		[Authorize("AuthenticatedUser")]
		public async Task<IActionResult> FetchDataReport(DataTableRequest request , CancellationToken cn )
        {

            if (TempData.ContainsKey("tableName"))
                request.TableName = TempData["tableName"].ToString();

            TempData.Keep("tableName");
  
            return Ok(await reportBuilderService.FetchData(request, cn));

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
		public async Task<IActionResult> ExportLargeDataToExcel(DataTableRequest request, CancellationToken cn =default)
        {
            var licensePath = AppDomain.CurrentDomain.BaseDirectory + "\\Aspose.Total.NET.lic";
            var memoryStream = new MemoryStream();

            request.TableName = request?.TableName ?? TempData["tableName"].ToString();
            
            TempData.Keep("tableName");
 
            try
            {
                await reportBuilderService.ExportLargeDataToExcelAsync(request, memoryStream , licensePath);
                 
                memoryStream.Position = 0;
                 
                return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"exportExcel.xlsx");
            }
            catch (Exception ex)
            {
                // Log the error (optional) and return an error response if needed
                return StatusCode(500, "An error occurred: " + ex.Message);
            }
        }

        [HttpGet("{action}")]
       
        public IActionResult StimulSoftReport(Guid? itemId , Guid? reportId)
        {
            StiLicense.Key = "6vJhGtLLLz2GNviWmUTrhSqnOItdDwjBylQzQcAOiHkO46nMQvol4ASeg91in+mGJLnn2KMIpg3eSXQSgaFOm15+0l" +
                             "hekKip+wRGMwXsKpHAkTvorOFqnpF9rchcYoxHXtjNDLiDHZGTIWq6D/2q4k/eiJm9fV6FdaJIUbWGS3whFWRLPHWC" +
                             "BsWnalqTdZlP9knjaWclfjmUKf2Ksc5btMD6pmR7ZHQfHXfdgYK7tLR1rqtxYxBzOPq3LIBvd3spkQhKb07LTZQoyQ" +
                             "3vmRSMALmJSS6ovIS59XPS+oSm8wgvuRFqE1im111GROa7Ww3tNJTA45lkbXX+SocdwXvEZyaaq61Uc1dBg+4uFRxv" +
                             "yRWvX5WDmJz1X0VLIbHpcIjdEDJUvVAN7Z+FW5xKsV5ySPs8aegsY9ndn4DmoZ1kWvzUaz+E1mxMbOd3tyaNnmVhPZ" +
                             "eIBILmKJGN0BwnnI5fu6JHMM/9QR2tMO1Z4pIwae4P92gKBrt0MqhvnU1Q6kIaPPuG2XBIvAWykVeH2a9EP6064e11" +
                             "PFCBX4gEpJ3XFD0peE5+ddZh+h495qUc1H2B";
            if (itemId != null)
            {
                TempData["itemId"] = itemId;
            }
            if (reportId != null)
            {
                TempData["reportId"] = reportId;
            }


            return View("Views/GenerateReport/stimulsoft.cshtml");
        }

       

        public class FilterParamter
        {
            public string[] values { get; set; }
            public string condition { get; set; } = "";
        }
    [HttpGet("[action]")]

        public IActionResult StimulSoftViewReport(Guid? reportId)
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

           
            var dic = new Dictionary<string, FilterParamter>();

            foreach (var q in Request.Query)
            {
                if (q.Key != "reportId")
                {
                    var spVal = q.Value.ToString().JsonDeserialize<FilterParamter>();
                    dic.Add(q.Key, spVal);
                }
                

			}

            TempData["queryString"] = dic.JsonSerialize();


			return View("Views/GenerateReport/StimulSoftView.cshtml");
        }

		[HttpGet("[action]")]
        [HttpPost("[action]")]
        public IActionResult DesignerEvent()
        {
            if (TempData.ContainsKey("itemId"))
            {
 
                TempData.Keep("itemId");
            }

            if (TempData.ContainsKey("reportId"))
            {
          
                TempData.Keep("reportId");
            }


            var report2 = StiNetCoreDesigner.GetReportObject(this);

            if (report2 != null)
            {
                reportJsonData = report2.SaveToJsonString();

            }
           

			return StiNetCoreDesigner.DesignerEventResult(this);
        }
        [HttpPost("[action]")]

        public IActionResult GetReport()
        {
            long reportId = 0;
			long itemId = 0;
            if (TempData.ContainsKey("itemId"))
            {
                itemId = TempData["itemId"].ToString().ToLong();
                TempData.Keep("itemId");
            }

            if (TempData.ContainsKey("reportId"))
            {
                reportId = TempData["reportId"].ToString().ToLong();
                TempData.Keep("reportId");
            }

            var dataObject = unitOfWork.Repository<ReportBuilderTable>()
                .GetById(itemId);
            dataObject.ReportBuilderTableFilters =
                dataObject.FiltersContent.JsonDeserialize<List<ReportBuilderTableItem>>() ?? new List<ReportBuilderTableItem>();

            dataObject.ReportBuilderTableSelects =
                dataObject.SelectsContent.JsonDeserialize<List<ReportBuilderTableSelect>>() ?? new List<ReportBuilderTableSelect>();

            var report = StiReport.CreateNewReport();

            if (reportId != 0)
            {
                var existReport =   unitOfWork.Repository<ReportBuilderReport>().
                    TableNoTracking
                     
                    .FirstOrDefault(c => c.Id == reportId);

                report.LoadFromJson(existReport.Content);
            }
            report.DataSources.Clear();
            report.Dictionary.Databases.Clear();
            report.Dictionary.Clear();
            report.Dictionary.Synchronize();

            

            var tempFilters =
                new List<FetchDataReportStoreProcViewModelReq.FetchDataReportStoreProcViewModelReqFilters>();

            dataObject.ReportBuilderTableFilters.ForEach(z =>
            {
                var neFilter = new FetchDataReportStoreProcViewModelReq.FetchDataReportStoreProcViewModelReqFilters();
                var colType = z.DataType;
                if (colType == "datetime" || colType == "datetime2")
                {
                    var format = "yyyy-MM-dd HH:mm:ss:fff";
                    neFilter.Value = DateTime.Now.ToString(format);

                }
                else if (colType == "nvarchar")
                {
                    neFilter.Value = "";
                }
                else if (colType == "bigint" || colType == "int" || colType == "integer")
                {
                    neFilter.Value = "1";
                }
                else if (colType == "bit" || colType == "boolean" || colType == "bool")
                {
                    neFilter.Value = "0";
                }
                else if (colType == "uniqueidentifier")
                {
                    neFilter.Value = Guid.NewGuid().ToString();
                }

                neFilter.ColumnName = z.ColumnName;
                tempFilters.Add(neFilter);
            });

            if (dataObject.ObjectType == "Store_Procedures")
            {
                var tmpData = reportBuilderService.FetchDataStoreProc(new FetchDataReportStoreProcViewModelReq()
                {
                    StoreProcName = dataObject.ObjectSchema + "." + dataObject.ObjectName,
                    Filters = tempFilters
                });

                var dataSet = new DataSet();
                for (int i = 0; i < dataObject.ReportBuilderTableSelects.Count; i++)
                {

                    var dt = new DataTable(dataObject.ReportBuilderTableSelects[i].Title);
                    var a = tmpData[i];
                    var s = a[0].Keys.ToList();


                    s.ForEach(x =>
                    {

                        dt.AddColumn(x);
                    });

                    a.Take(20).ToList().ForEach(x =>
                    {
                        DataRow row;
                        row = dt.NewRow();
                        foreach (var o in x)
                        {
                            row[o.Key] = o.Value;
                        }


                        dt.Rows.Add(row);
                    });


                    dataSet.Tables.Add(dt);
                }


                report.RegData(dataSet);
            }
            else
            {
                var tmpData =
                    reportBuilderService.FetchSampleData(dataObject.ObjectSchema + "." + dataObject.ObjectName);
              var dataSet = new DataSet();
              for (int i = 0; i < dataObject.ReportBuilderTableSelects.Count; i++)
              {

                  var dt = new DataTable(dataObject.ReportBuilderTableSelects[i].Title);
                  var a = tmpData;
                  var s = a[0].Keys.ToList();


                  s.ForEach(x =>
                  {

                      dt.AddColumn(x);
                  });

                  a.ToList().ForEach(x =>
                  {
                      DataRow row;
                      row = dt.NewRow();
                      foreach (var o in x)
                      {
                          row[o.Key] = o.Value;
                      }


                      dt.Rows.Add(row);
                  });


                  dataSet.Tables.Add(dt);
              }


              report.RegData(dataSet);
            }

            foreach (var f in Fonts)
            {
                if (!f.Loaded)
                {
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "panelLib", "fonts", f.Name);
                    var fileContent = System.IO.File.ReadAllBytes(path);
                    var resource = new StiResource(f.Name.Split(".")[0], f.Name.Split(".")[0], false, StiResourceType.FontTtf, fileContent, false);
                    report.Dictionary.Resources.Add(resource);
                    f.Loaded = true;
                    f.Bytes = fileContent;
                }
                else
                {
                    var fileContent = f.Bytes;
                    var resource = new StiResource("IRANSansWeb", "IRANSansWeb", false, StiResourceType.FontTtf, fileContent, false);
                    report.Dictionary.Resources.Add(resource);
                }

               
            }

        
            report.Dictionary.Synchronize();

            return StiNetCoreDesigner.GetReportResult(this, report);
        }

        [HttpPost("[action]")]

        public IActionResult GetViewReport()
        {
            long reportId = 0;
            var paramsQuery = new Dictionary<string, FilterParamter>();
          

            if (TempData.ContainsKey("reportId"))
            {
                reportId = TempData["reportId"].ToString().ToLong();
           
            }

            if (TempData.ContainsKey("queryString"))
            {
                paramsQuery = TempData["queryString"].ToString().JsonDeserialize<System.Collections.Generic.Dictionary<string, FilterParamter>>();
              
            }


            var report = StiReport.CreateNewReport();

            var existReport = unitOfWork.Repository<ReportBuilderReport>().
                TableNoTracking
                .Include(c=>c.ReportBuilderReportFilters)
                .Include(c=>c.ReportBuilderReportSelects)
                .ThenInclude(c=>c.ReportBuilderReportItems)
                .FirstOrDefault(c => c.Id == reportId);

            report.LoadFromJson(existReport.Content);

            report.DataSources.Clear();
            report.Dictionary.Databases.Clear();
            report.Dictionary.Clear();
            report.Dictionary.Synchronize();



        

            if (existReport.ObjectType == "Store_Procedures")
            {
                var tempFilters =
                    new List<FetchDataReportStoreProcViewModelReq.FetchDataReportStoreProcViewModelReqFilters>();
                foreach (var x in paramsQuery)
                {
                    if (!existReport.ReportBuilderReportFilters.Any(c => c.ColumnName.ToLower() == x.Key.ToLower()))
                        continue;
                    var neFilter = new FetchDataReportStoreProcViewModelReq.FetchDataReportStoreProcViewModelReqFilters();
                  
                    var colType = existReport.ReportBuilderReportFilters.FirstOrDefault(c => c.ColumnName.ToLower() == x.Key.ToLower()).DataType;
                 
                    if (colType == "datetime" || colType == "datetime2")
                    {
                        var format = "yyyy-MM-dd HH:mm:ss:fff";
                        neFilter.Value = x.Value.values[0];

                    }
                    else if (colType == "nvarchar")
                    {
                        neFilter.Value = x.Value.values[0];
                    }
                    else if (colType == "bigint" || colType == "int" || colType == "integer")
                    {
                        neFilter.Value = x.Value.values[0];
                    }
                    else if (colType == "bit" || colType == "boolean" || colType == "bool")
                    {
                        neFilter.Value = x.Value.values[0];
                    }
                    else if (colType == "uniqueidentifier")
                    {
                        neFilter.Value = x.Value.values[0];
                    }
                    
                    neFilter.ColumnName = x.Key;
                    tempFilters.Add(neFilter);
                }

                var tmpData = reportBuilderService.FetchDataStoreProc(new FetchDataReportStoreProcViewModelReq()
                {
                    StoreProcName = existReport.Name,
                    Filters = tempFilters
                });

                var dataSet = new DataSet();
                for (int i = 0; i < tmpData.Count; i++)
                {

                    var dt = new DataTable(existReport.ReportBuilderReportSelects[i].Title);
                    var a = tmpData[i];
                    var s = a[0].Keys.ToList();


                    s.ForEach(x =>
                    {

                        dt.AddColumn(x);
                    });

                    a.ToList().ForEach(x =>
                    {
                        DataRow row;
                        row = dt.NewRow();
                        foreach (var o in x)
                        {
                            var colType = existReport.ReportBuilderReportFilters.FirstOrDefault(c => c.ColumnName.ToLower() == o.Key.ToLower())?.DataType;

                            if (colType == "datetime" || colType == "datetime2")
                            {
                                if(o.Value != null)
                                 row[o.Key] = DateTime.Parse(o.Value.ToString()).ToShamsiDateTime();
                                else
                                {
                                    row[o.Key] = null;
                                }
                            }
                            else
                            {
                                row[o.Key] = o.Value;
                            }
                            
                        }

                        dt.Rows.Add(row);
                    });


                    dataSet.Tables.Add(dt);
                }

                report.RegData(dataSet);
            }
            else
            {
                var tempFilters =
                    new List<FetchDataReportTableViewModelReq.FetchDataReportTableViewModelReqFilters>();
                foreach (var x in paramsQuery)
                {
                    if (!existReport.ReportBuilderReportFilters.Any(c => c.ColumnName.ToLower() == x.Key.ToLower()))
                        continue;
                    var neFilter = new FetchDataReportTableViewModelReq.FetchDataReportTableViewModelReqFilters();

                    var colType = existReport.ReportBuilderReportFilters.FirstOrDefault(c => c.ColumnName.ToLower() == x.Key.ToLower()).DataType;

                    if (colType == "datetime" || colType == "datetime2")
                    {
                        var format = "yyyy-MM-dd HH:mm:ss:fff";
                        neFilter.Values = x.Value.values;

                    }
                    else if (colType == "nvarchar")
                    {
                        neFilter.Values = x.Value.values;
                    }
                    else if (colType == "bigint" || colType == "int" || colType == "integer")
                    {
                        neFilter.Values = x.Value.values;
                    }
                    else if (colType == "bit" || colType == "boolean" || colType == "bool")
                    {
                        neFilter.Values = x.Value.values;
                    }
                    else if (colType == "uniqueidentifier")
                    {
                        neFilter.Values = x.Value.values;
                    }
                  
                    neFilter.ColumnName = x.Key;
                    neFilter.Condition = x.Value.condition;
                    tempFilters.Add(neFilter);
                }

                var tmpData = reportBuilderService.FetchTableDataStimilSoft(new FetchDataReportTableViewModelReq()
                {
                    TableName = existReport.Name,
                    Filters = tempFilters
                });

                var dataSet = new DataSet();
                for (int i = 0; i < existReport.ReportBuilderReportSelects.Count; i++)
                {

                    var dt = new DataTable(existReport.ReportBuilderReportSelects[i].Title);
                    var a = tmpData;
                    if (a.Count > 0)
                    {
                        var s = a[0].Keys.ToList();


                        s.ForEach(x =>
                        {

                            dt.AddColumn(x);
                        });

                        a.ToList().ForEach(x =>
                        {
                            DataRow row;
                            row = dt.NewRow();
                            foreach (var o in x)
                            {
                                var colType = existReport.ReportBuilderReportFilters.FirstOrDefault(c => c.ColumnName.ToLower() == o.Key.ToLower())?.DataType;

                                if (colType == "datetime" || colType == "datetime2")
                                {
                                    if (o.Value != null)
                                        row[o.Key] = DateTime.Parse(o.Value.ToString()).ToShamsiDateTime();
                                    else
                                    {
                                        row[o.Key] = null;
                                    }
                                }
                                else
                                {
                                    row[o.Key] = o.Value;
                                }
                            }


                            dt.Rows.Add(row);
                        });


                        dataSet.Tables.Add(dt);
                    }
                    
                }


                report.RegData(dataSet);
            }

          

            
            foreach (var f in Fonts)
            {
                if (!f.Loaded)
                {
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "panelLib", "fonts", f.Name);
                    var fileContent = System.IO.File.ReadAllBytes(path);
                    var resource = new StiResource(f.Name.Split(".")[0], f.Name.Split(".")[0], false, StiResourceType.FontTtf, fileContent, false);
                    report.Dictionary.Resources.Add(resource);
                    f.Loaded = true;
                    f.Bytes = fileContent;
                }
                else
                {
                    var fileContent = f.Bytes;
                    var resource = new StiResource(f.Name.Split(".")[0], f.Name.Split(".")[0], false, StiResourceType.FontTtf, fileContent, false);
                    report.Dictionary.Resources.Add(resource);
                }
                 
            }


            report.Dictionary.Synchronize();

            return StiNetCoreViewer.GetReportResult(this, report);
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
            if (TempData.ContainsKey("itemId"))
            {
           
                TempData.Keep("itemId");
            }

            if (TempData.ContainsKey("reportId"))
            {
	 
	            TempData.Keep("reportId");
            }

 
            var report2 = StiNetCoreDesigner.GetReportObject(this);


            reportJsonData = report2.SaveToJsonString();

            return StiNetCoreDesigner.SaveReportResult(this);
        }


    }
}
