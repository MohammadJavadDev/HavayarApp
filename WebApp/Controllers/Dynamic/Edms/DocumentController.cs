using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Data.SystemAuth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Entities.Base.DataTable;
using WebFramework.Filtters;
using WebFramework.Page;
using Entities.App.Edms;

namespace WebApp.Controllers.Dynamic
{
    [Route("Panel/[controller]")]
    [ApiController]
    [ApiResultFilter]
    [ControllerInfo("مدارک مهندسی", typeof(Document))]
    public class DocumentController(IUnitOfWork unitOfWork  , IPropertyIdentityService identityService, IWebHostEnvironment _webHostEnvironment) : BaseController
    {
        [HttpPost("[action]")]
        [ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
        public async Task<IActionResult> Save(Document document , CancellationToken cn)
        {
            // Save logic here
           if(document.Id == null || document.Id == 0)
        {
         return await  Add(document, cn);
        }
         var exist = await unitOfWork.Repository<Document>().TableNoTracking.AnyAsync(c => c.Id == document.Id);
           if(exist)
           {
         return await  Update(document, cn);
           }
              return await  Add(document, cn);
        }

        [HttpPost("[action]")]
        [ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
        public async Task<IActionResult> Add(Document document , CancellationToken cn)
        {
            // Add logic here
           var entity = await unitOfWork.Repository<Document>().SaveAsync(document ,cn,true);
            return Ok(entity);
        }

        [HttpPost("[action]")]
        [ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
        public async Task<IActionResult> Update(Document document , CancellationToken cn)
        {
            // Update logic here
           var entity = await unitOfWork.Repository<Document>().UpdateAsync(document, cn, true);
            return Ok(entity);
        }

        [HttpGet("[action]")]
        [ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
            public  async Task<IActionResult> Delete(long id, CancellationToken cn)
        {
            // Delete logic here
      var model = unitOfWork.Repository<Document>().TableNoTracking.FirstOrDefault(c => c.Id == id);
       if (model != null)
         await unitOfWork.Repository<Document>().DeleteAsync(model ,cn , true);
            return Ok();
        }

        [HttpGet("[action]")]
        [ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
        public IActionResult Edit(long? id)
        {
            // Get logic here
            if (id != null && id != 0)
            {
             var entity =   unitOfWork.Repository<Document>().TableNoTracking.FirstOrDefault(c => c.Id == id);
                return View(@"\Views\Panel\Edms\Document\Edit.cshtml", entity);
             }
              var newEntity = new Document(); 
               
               return View(@"\Views\Panel\Edms\Document\Edit.cshtml",newEntity);
        }
        [HttpGet("[action]")]
        [ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
        public IActionResult New()
        {
              var newEntity = new Document(); 
               
               return View(@"\Views\Panel\Edms\Document\Edit.cshtml",newEntity);
        }
       [HttpGet("[action]")]
        [ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
        public IActionResult List()
        {
              return View(@"\Views\Panel\Edms\Document\List.cshtml");
        }
       [HttpPost("[action]")]
         [ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
       public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
        {
          var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
         var memoryStream = new MemoryStream();
        try
        {
              await unitOfWork.Repository<Document>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);

            memoryStream.Position = 0;

            return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"exportExcel.xlsx");
        }
        catch (Exception ex)
        {
         
            return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message);
        }
        }
        [ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
       [HttpPost("[action]")]
         public async Task<IActionResult> FetchData(DataTableRequest request , CancellationToken cn)
        {
              return Ok(await unitOfWork.Repository<Document>().FetchDataAsync(request ,cn));
        }
       [HttpGet("[action]")]
        public IActionResult DocumentCommentPartial()
        {
              return PartialView(@"\Views\Panel\Edms\Document\_DocumentCommentPartial.cshtml");
        }

    }
}
