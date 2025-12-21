using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Data.SystemAuth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Entities.Base.DataTable;
using WebFramework.Filtters;
using WebFramework.Page;
using Entities.App.Epms;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Epms/[controller]")]
	[ApiController]
    [ApiResultFilter]
    [ControllerInfo("پروپوزال Vpis", typeof(ProposalVpis))]
    public class ProposalVpisController(IUnitOfWork unitOfWork  , IPropertyIdentityService identityService, IWebHostEnvironment _webHostEnvironment) : BaseController
    {
        [HttpPost("[action]")]
        [ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
        public async Task<IActionResult> Save(ProposalVpis proposalvpis , CancellationToken cn)
        {
            // Save logic here
           if(proposalvpis.Id == null || proposalvpis.Id == 0)
        {
         return await  Add(proposalvpis, cn);
        }
         var exist = await unitOfWork.Repository<ProposalVpis>().TableNoTracking.AnyAsync(c => c.Id == proposalvpis.Id);
           if(exist)
           {
         return await  Update(proposalvpis, cn);
           }
              return await  Add(proposalvpis, cn);
        }

        [HttpPost("[action]")]
        [ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
        public async Task<IActionResult> Add(ProposalVpis proposalvpis , CancellationToken cn)
        {
            // Add logic here
           var entity = await unitOfWork.Repository<ProposalVpis>().SaveAsync(proposalvpis ,cn,true);
            return Ok(entity);
        }

        [HttpPost("[action]")]
        [ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
        public async Task<IActionResult> Update(ProposalVpis proposalvpis , CancellationToken cn)
        {
            // Update logic here
           var entity = await unitOfWork.Repository<ProposalVpis>().UpdateAsync(proposalvpis, cn, true);
            return Ok(entity);
        }

        [HttpGet("[action]")]
        [ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
            public  async Task<IActionResult> Delete(long id, CancellationToken cn)
        {
            // Delete logic here
      var model = unitOfWork.Repository<ProposalVpis>().TableNoTracking.FirstOrDefault(c => c.Id == id);
       if (model != null)
         await unitOfWork.Repository<ProposalVpis>().DeleteAsync(model ,cn , true);
            return Ok();
        }

        [HttpGet("[action]")]
        [ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
        public IActionResult Edit(long? id)
        {
            // Get logic here
            if (id != null && id != 0)
            {
             var entity =   unitOfWork.Repository<ProposalVpis>().TableNoTracking.
                         Include(c=>c.MainResponsible).
                         FirstOrDefault(c => c.Id == id);
                return View(@"\Views\Panel\Epms\ProposalVpis\Edit.cshtml", entity);
             }
              var newEntity = new ProposalVpis(); 
               
               return View(@"\Views\Panel\Epms\ProposalVpis\Edit.cshtml",newEntity);
        }
        [HttpGet("[action]")]
        [ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
        public IActionResult New()
        {
              var newEntity = new ProposalVpis(); 
               
               return View(@"\Views\Panel\Epms\ProposalVpis\Edit.cshtml",newEntity);
        }
       [HttpGet("[action]")]
        [ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
        public IActionResult List()
        {
              return View(@"\Views\Panel\Epms\ProposalVpis\List.cshtml");
        }
       [HttpPost("[action]")]
         [ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
       public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
        {
          var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
         var memoryStream = new MemoryStream();
        try
        {
              await unitOfWork.Repository<ProposalVpis>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);

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
              return Ok(await unitOfWork.Repository<ProposalVpis>().FetchDataAsync(request ,cn));
        }

    }
}
