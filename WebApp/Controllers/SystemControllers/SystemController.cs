using Common.Utilities;
using Data.Contracts;
using Data.Repositories;
using Entities.Base;
using Entities.Base.DataTable;
using Entities.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Auth;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using System.Text.Json;
using WebFramework.Filtters;
using WebFramework.Page;
using static System.Net.WebRequestMethods;

namespace WebApp.Controllers.SystemControllers
{
    [ApiController]
    [ApiResultFilter]
    [Route("[controller]")]

    [Authorize("AuthenticatedUser")]
    public class SystemController(
        IEntityRepository _service,
        IEntityHistoryRepository repositoryHistoryRepository,
        IDataTableProfileService dataTableProfile,
         IWebHostEnvironment _webHostEnvironment
        , IEntityMetadataCache _entityMetadataCache,
        HttpClient http) : BaseController
    {


        [HttpPost("{action}")]
        public async Task<IActionResult> FetchData(DataTableRequest request)
        {

            return Ok(await _service.FetchData(request));

        }
        [HttpPost("[action]")]
        public async Task<IActionResult> FetchDataProfile(DataTableRequest request)
        {
            var res = await _service.FetchDataProfile(request);


            return Ok(res);

        }
        private static string GetEnumDisplayName(Enum enumValue)
        {
            var displayAttribute = enumValue.GetType()
                .GetMember(enumValue.ToString())
                .First()
                .GetCustomAttribute<DisplayAttribute>();
            return displayAttribute?.Name ?? enumValue.ToString();
        }
        [HttpPost("[action]")]
        public IActionResult FetchDataTableProfile(GetDataProfileViewModel request)
        {
            var data = dataTableProfile.GetDataTableProfileById(request.Id);
            var cols = data.Columns.JsonDeserialize<List<SystemDataTableProfileSelectViewModel>>();



            foreach (var dat in cols.Where(c => c.Type == "select").ToList())
            {

				var tableName = dat.TableName;
				var entityType = _entityMetadataCache.Get(data.EntityName);

				if (entityType == null)
                    throw new Exception("Invalid table name.");

                var prop = entityType.Properties.FirstOrDefault(c => c.Name == dat.PropName);
				dat.options = new();
				if (prop != null)
                    {
                        
					foreach (var value in prop.Options)
					{

						dat.options.Add(
						    new OptionViewModel()
						    {
							    Value = value.Value.ToString(),
							    Name = value.Text
						    });
					}
				}


             

              
            }
            var dataTableColumns = cols.Select(c =>
            {
                 return new
                 {
                      name = c.Alliance,
                      data = c.Alliance,
                      type = c?.Type ?? "string",
                      title = c.Title,
                      render = c?.Render ?? "",
                      c?.options,
                      showInRelationData = c.Name == request.ShowInRelationData,
                      c.Visible

                 };

		  }).ToList();

            return Ok(dataTableColumns);

        }
        [HttpPost("[action]")]
        public async Task<IActionResult> ExportToExcelProfile(DataTableRequest request)
        {


            var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
            var memoryStream = new MemoryStream();


            try
            {
                await _service.ExportToExcelProfile(request, memoryStream, licensePath);

                memoryStream.Position = 0;

                return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"exportExcel.xlsx");
            }
            catch (Exception ex)
            {
                // Log the error (optional) and return an error response if needed
                return StatusCode(500, "An error occurred: " + ex.Message);
            }

        }
        //      [HttpPost("{action}")]
        //      public async Task<IActionResult> Save(SaveEntity request)
        //      {
        //         var entity = await _service.AddEntity(request.EntityName, request.Entity);

        //          return Ok(entity);
        //      }

        //      [HttpGet("{action}")]
        //      public async Task<IActionResult> Remove(string entityName, Guid id)
        //      {
        //         await _service.RemoveEntity(entityName, id);

        //          return Ok();
        //      }
        [HttpPost("[action]")]
        public async Task<IActionResult> GetHistory(GetHistoryViewModel model)
        {
            return Ok(await repositoryHistoryRepository.GetEntityHistoryById(model.Id, model.type));
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> TranslateAsync(string text)
        {
            try
            {
                var _model = "zongwei/gemma3-translator:1b";
                var _baseUrl = "http://localhost:11434";
                var payload = new
                {
                    model = _model,
                    prompt = $@"
                         You are a professional .NET naming assistant.

                         Task:
                         Convert the following Persian field name into a clean and valid C# property name.

                         Rules:
                         - Output MUST be only ONE word (no explanation).
                         - Use PascalCase.
                         - Use correct English meaning.
                         - Do NOT add punctuation.
                         - Do NOT return quotes.
                         - Do NOT return camelCase or snake_case.
                         - If the phrase contains multiple words, merge them in PascalCase.
                         - Remove spaces, hyphens, and non-alphanumeric characters.
                         - Always choose standard English naming that is used in C# data models.

                         Example Conversions:
                         'نام' → FirstName  
                         'نام خانوادگی' → LastName  
                         'شماره ملی' → NationalId  
                         'تاریخ تولد' → BirthDate  
                         'کد پستی' → PostalCode  
                         'تلفن همراه' → MobileNumber  

                         Now convert this:
                         {text}

                         Return ONLY the final C# property name.",
                    stream = false
                };

                var json = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await http.PostAsync($"{_baseUrl}/api/generate", json);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadAsStringAsync();
                var parsed = JsonSerializer.Deserialize<OllamaResponse>(result);

                var translatedName = parsed?.response?.Trim() ?? "Property";
                return Ok(translatedName);
            }
            catch (Exception ex)
            {
                return Ok("Property"); // Fallback on error
            }
        }

        private class OllamaResponse
        {
            public string? response { get; set; }
        }



        public class GetHistoryViewModel
        {
            public long Id { get; set; }
            public string? type { get; set; }
        }
        public class GetDataProfileViewModel
        {
            public long Id { get; set; }
            public string ShowInRelationData { get; set; } = "Id";
        }
    }

}


