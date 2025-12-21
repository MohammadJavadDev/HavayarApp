using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Entities.Base.DataTable;
using WebFramework.Filtters;
using WebFramework.Page;
using Entities.App.Epms;

namespace WebApp.Controllers.Dynamic.Epms
{
	[Route("Panel/Epms/[controller]")]
	[ApiController]
    [ApiResultFilter]
    [ControllerInfo("اسناد VPIS", typeof(VpisType))]
    public class VpisTypeController(IUnitOfWork unitOfWork  , IPropertyIdentityService identityService, IWebHostEnvironment _webHostEnvironment) : BaseController
    {
        [HttpPost("[action]")]
        [ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
        public async Task<IActionResult> Save(VpisType vpistype , CancellationToken cn)
        {
            // Save logic here
           if(vpistype.Id == null || vpistype.Id == 0)
        {
         return await  Add(vpistype, cn);
        }
         var exist = await unitOfWork.Repository<VpisType>().TableNoTracking.AnyAsync(c => c.Id == vpistype.Id);
           if(exist)
           {
         return await  Update(vpistype, cn);
           }
              return await  Add(vpistype, cn);
        }

        [HttpPost("[action]")]
        [ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
        public async Task<IActionResult> Add(VpisType vpistype , CancellationToken cn)
        {
            // Add logic here
           var entity = await unitOfWork.Repository<VpisType>().SaveAsync(vpistype ,cn,true);
            return Ok(entity);
        }

        [HttpPost("[action]")]
        [ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
        public async Task<IActionResult> Update(VpisType vpistype , CancellationToken cn)
        {
            // Update logic here
           var entity = await unitOfWork.Repository<VpisType>().UpdateAsync(vpistype, cn, true);
            return Ok(entity);
        }

        [HttpGet("[action]")]
        [ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
            public  async Task<IActionResult> Delete(long id, CancellationToken cn)
        {
            // Delete logic here
      var model = unitOfWork.Repository<VpisType>().TableNoTracking.FirstOrDefault(c => c.Id == id);
       if (model != null)
         await unitOfWork.Repository<VpisType>().DeleteAsync(model ,cn , true);
            return Ok();
        }

        [HttpGet("[action]")]
        [ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
        public IActionResult Edit(long? id)
        {
            // Get logic here
            if (id != null && id != 0)
            {
             var entity =   unitOfWork.Repository<VpisType>().TableNoTracking.FirstOrDefault(c => c.Id == id);
                return View(@"\Views\Panel\Epms\VpisType\Edit.cshtml", entity);
             }
              var newEntity = new VpisType(); 
               
               return View(@"\Views\Panel\Epms\VpisType\Edit.cshtml", newEntity);
        }
        [HttpGet("[action]")]
        [ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
        public IActionResult New()
        {
              var newEntity = new VpisType(); 
               
               return View(@"\Views\Panel\Epms\VpisType\Edit.cshtml", newEntity);
        }
       [HttpGet("[action]")]
        [ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
        public IActionResult List()
        {
              return View(@"\Views\Panel\Epms\VpisType\List.cshtml");
        }
       [HttpPost("[action]")]
         [ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
       public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
        {
          var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
         var memoryStream = new MemoryStream();
        try
        {
              await unitOfWork.Repository<VpisType>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);

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
              return Ok(await unitOfWork.Repository<VpisType>().FetchDataAsync(request ,cn));
        }

    }
}
