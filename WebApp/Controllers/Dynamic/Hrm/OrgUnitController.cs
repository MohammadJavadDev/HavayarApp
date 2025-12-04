using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Data.SystemAuth;
using Entities.App;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Entities.Base.DataTable;
using WebFramework.Filtters;
using WebFramework.Page;
using Entities.App.Hrm;

namespace WebApp.Controllers.Dynamic.Hrm
{
    [Route("Panel/[controller]")]
    [ApiController]
    [ApiResultFilter]
    [ControllerInfo("واحد سازمانی", typeof(OrgUnit))]
    public class OrgUnitController(IUnitOfWork unitOfWork  , IPropertyIdentityService identityService, IWebHostEnvironment _webHostEnvironment) : BaseController
    {
        [HttpPost("[action]")]
        [ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
        public async Task<IActionResult> Save(OrgUnit orgunit , CancellationToken cn)
        {
            // Save logic here
           if(orgunit.Id == null || orgunit.Id == 0)
        {
         return await  Add(orgunit, cn);
        }
         var exist = await unitOfWork.Repository<OrgUnit>().TableNoTracking.AnyAsync(c => c.Id == orgunit.Id);
           if(exist)
           {
         return await  Update(orgunit, cn);
           }
              return await  Add(orgunit, cn);
        }

        [HttpPost("[action]")]
        [ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
        public async Task<IActionResult> Add(OrgUnit orgunit , CancellationToken cn)
        {
            // Add logic here
           var entity = await unitOfWork.Repository<OrgUnit>().SaveAsync(orgunit ,cn,true);
            return Ok(entity);
        }

        [HttpPost("[action]")]
        [ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
        public async Task<IActionResult> Update(OrgUnit orgunit , CancellationToken cn)
        {
            // Update logic here
           var entity = await unitOfWork.Repository<OrgUnit>().UpdateAsync(orgunit, cn, true);
            return Ok(entity);
        }

        [HttpGet("[action]")]
        [ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
            public  async Task<IActionResult> Delete(long id, CancellationToken cn)
        {
            // Delete logic here
      var model = unitOfWork.Repository<OrgUnit>().TableNoTracking.FirstOrDefault(c => c.Id == id);
       if (model != null)
         await unitOfWork.Repository<OrgUnit>().DeleteAsync(model ,cn , true);
            return Ok();
        }

        [HttpGet("[action]")]
        [ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
        public IActionResult Edit(long? id)
        {
            // Get logic here
            if (id != null && id != 0)
            {
             var entity =   unitOfWork.Repository<OrgUnit>().TableNoTracking.FirstOrDefault(c => c.Id == id);
                return View(@"\Views\Panel\Hrm\OrgUnit\Edit.cshtml", entity);
             }
              var newEntity = new OrgUnit(); 
               
               return View(@"\Views\Panel\Hrm\OrgUnit\Edit.cshtml", newEntity);
        }
        [HttpGet("[action]")]
        [ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
        public IActionResult New()
        {
              var newEntity = new OrgUnit(); 
               
               return View(@"\Views\Panel\Hrm\OrgUnit\Edit.cshtml", newEntity);
        }
       [HttpGet("[action]")]
        [ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
        public IActionResult List()
        {
              return View(@"\Views\Panel\Hrm\OrgUnit\List.cshtml");
        }
       [HttpPost("[action]")]
         [ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
       public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
        {
          var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
         var memoryStream = new MemoryStream();
        try
        {
              await unitOfWork.Repository<OrgUnit>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);

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
              return Ok(await unitOfWork.Repository<OrgUnit>().FetchDataAsync(request ,cn));
        }

    }
}
