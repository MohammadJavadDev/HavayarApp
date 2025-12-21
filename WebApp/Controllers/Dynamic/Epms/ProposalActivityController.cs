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
    [ControllerInfo("فعالیت های پروپوزال", typeof(ProposalActivity))]
    public class ProposalActivityController(IUnitOfWork unitOfWork  , IPropertyIdentityService identityService, IWebHostEnvironment _webHostEnvironment) : BaseController
    {
        [HttpPost("[action]")]
        [ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
        public async Task<IActionResult> Save(ProposalActivity proposalactivity , CancellationToken cn)
        {
            // Save logic here
           if(proposalactivity.Id == null || proposalactivity.Id == 0)
        {
         return await  Add(proposalactivity, cn);
        }
         var exist = await unitOfWork.Repository<ProposalActivity>().TableNoTracking.AnyAsync(c => c.Id == proposalactivity.Id);
           if(exist)
           {
         return await  Update(proposalactivity, cn);
           }
              return await  Add(proposalactivity, cn);
        }

        [HttpPost("[action]")]
        [ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
        public async Task<IActionResult> Add(ProposalActivity proposalactivity , CancellationToken cn)
        {
            // Add logic here
           var entity = await unitOfWork.Repository<ProposalActivity>().SaveAsync(proposalactivity ,cn,true);
            return Ok(entity);
        }

        [HttpPost("[action]")]
        [ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
        public async Task<IActionResult> Update(ProposalActivity proposalactivity , CancellationToken cn)
        {
            // Update logic here
           var entity = await unitOfWork.Repository<ProposalActivity>().UpdateAsync(proposalactivity, cn, true);
            return Ok(entity);
        }

        [HttpGet("[action]")]
        [ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
            public  async Task<IActionResult> Delete(long id, CancellationToken cn)
        {
            // Delete logic here
      var model = unitOfWork.Repository<ProposalActivity>().TableNoTracking.FirstOrDefault(c => c.Id == id);
       if (model != null)
         await unitOfWork.Repository<ProposalActivity>().DeleteAsync(model ,cn , true);
            return Ok();
        }

        [HttpGet("[action]")]
        [ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
        public IActionResult Edit(long? id)
        {
            // Get logic here
            if (id != null && id != 0)
            {
             var entity =   unitOfWork.Repository<ProposalActivity>().TableNoTracking
                         .Include(c=>c.BeneficiaryUnit)
                         .Include(c=>c.Proposal)
                         .Include(c=>c.ProposalActivityComments)
                         .FirstOrDefault(c => c.Id == id);
                return View(@"\Views\Panel\Epms\ProposalActivity\Edit.cshtml", entity);
             }
              var newEntity = new ProposalActivity(); 
               
               return View(@"\Views\Panel\Epms\ProposalActivity\Edit.cshtml",newEntity);
        }
        [HttpGet("[action]")]
        [ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
        public IActionResult New()
        {
              var newEntity = new ProposalActivity(); 
               
               return View(@"\Views\Panel\Epms\ProposalActivity\Edit.cshtml",newEntity);
        }
       [HttpGet("[action]")]
        [ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
        public IActionResult List()
        {
              return View(@"\Views\Panel\Epms\ProposalActivity\List.cshtml");
        }
       [HttpPost("[action]")]
         [ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
       public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
        {
          var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
         var memoryStream = new MemoryStream();
        try
        {
              await unitOfWork.Repository<ProposalActivity>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);

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
              return Ok(await unitOfWork.Repository<ProposalActivity>().FetchDataAsync(request ,cn));
        }
       [HttpGet("[action]")]
        public IActionResult ProposalActivityCommentPartial()
        {
              return PartialView(@"\Views\Panel\Epms\ProposalActivity\_ProposalActivityCommentPartial.cshtml");
        }

    }
}
