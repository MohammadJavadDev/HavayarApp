using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Rpr;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic.Rpr
{
    [Route("Panel/Rpr/[controller]")]
    [ApiController]
    [ApiResultFilter]
    [ControllerInfo("اقلام مصرفی", typeof(RepairRequestPart))]
    public class RepairRequestPartController(IUnitOfWork unitOfWork, IWebHostEnvironment _webHostEnvironment) : BaseController
    {
        [HttpPost("[action]")]
        [ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
        public async Task<IActionResult> Save(RepairRequestPart repairRequestPart, CancellationToken cn)
        {

            if (repairRequestPart.Id == null || repairRequestPart.Id == 0)
            {
                return await Add(repairRequestPart, cn);
            }
            var exist = await unitOfWork.Repository<RepairRequestPart>().TableNoTracking.AnyAsync(c => c.Id == repairRequestPart.Id);
            if (exist)
            {
                return await Update(repairRequestPart, cn);
            }
            return await Add(repairRequestPart, cn);
        }

        [HttpPost("[action]")]
        [ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
        public async Task<IActionResult> Add(RepairRequestPart repairRequestPart, CancellationToken cn)
        {
            var entity = await unitOfWork.Repository<RepairRequestPart>().SaveAsync(repairRequestPart, cn, true);
            return Ok(entity);
        }

        [HttpPost("[action]")]
        [ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
        public async Task<IActionResult> Update(RepairRequestPart repairRequestPart, CancellationToken cn)
        {
            var entity = await unitOfWork.Repository<RepairRequestPart>().UpdateAsync(repairRequestPart, cn, true);
            return Ok(entity);
        }

        [HttpGet("[action]")]
        [ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
        public async Task<IActionResult> Delete(long id, CancellationToken cn)
        {
            var model = unitOfWork.Repository<RepairRequestPart>().TableNoTracking.FirstOrDefault(c => c.Id == id);
            if (model != null)
                await unitOfWork.Repository<RepairRequestPart>().DeleteAsync(model, cn, true);
            return Ok();
        }

        [HttpGet("[action]")]
        [ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
        public IActionResult Edit(long? id)
        {
            if (id != null && id != 0)
            {
                var entity = unitOfWork.Repository<RepairRequestPart>().TableNoTracking
                    .FirstOrDefault(c => c.Id == id);
                return View(@"\Views\Panel\Rpr\RepairRequest\RepairRequestPart\Edit.cshtml", entity);
            }
            var newEntity = new RepairRequestPart();
            return View(@"\Views\Panel\Rpr\RepairRequest\RepairRequestPart\Edit.cshtml", newEntity);
        }

        [HttpGet("[action]")]
        [ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
        public IActionResult New()
        {
            var newEntity = new RepairRequestPart();
            return View(@"\Views\Panel\Rpr\RepairRequest\RepairRequestPart\Edit.cshtml", newEntity);
        }

        [HttpGet("[action]")]
        [ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
        public IActionResult List()
        {
            return View(@"\Views\Panel\Rpr\RepairRequest\RepairRequestPart\List.cshtml");
        }

        [HttpPost("[action]")]
        [ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
        public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
        {
            var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
            var memoryStream = new MemoryStream();
            try
            {
                await unitOfWork.Repository<RepairRequestPart>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
                memoryStream.Position = 0;
                return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"exportExcel.xlsx");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message);
            }
        }

        //[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
        //[HttpPost("[action]")]
        //public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
        //{
        //    return Ok(await unitOfWork.Repository<Part>().FetchDataAsync(request, cn));
        //}

        //[HttpGet("[action]")]
        //public IActionResult PartSparePartPartial()
        //{
        //    return PartialView(@"\Views\Panel\Inv\Part\_PartSparePartPartial.cshtml");
        //}

        //[HttpGet("[action]")]
        //public IActionResult PartDocumentPartial()
        //{
        //    return PartialView(@"\Views\Panel\Inv\Part\_PartDocumentPartial.cshtml");
        //}
    }

}
