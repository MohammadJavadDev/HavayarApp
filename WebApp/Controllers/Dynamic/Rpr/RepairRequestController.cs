using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Entities.Base.DataTable;
using WebFramework.Filtters;
using WebFramework.Page;
using Entities.App.Prp;

namespace WebApp.Controllers.Dynamic.Rpr
{
    [Route("Panel/Rpr/[controller]")]
    [ApiController]
    [ApiResultFilter]
    [ControllerInfo("درخواست تعمیرات ", typeof(RepairRequest))]
    public class RepairRequestController(IUnitOfWork unitOfWork, IWebHostEnvironment WebHostEnvironment) : BaseController
    {
        //[HttpPost("[action]")]
        //[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
        //public async Task<IActionResult> Save(RepairRequest repairRequest, CancellationToken cn)
        //{
        //    if (repairRequest.Id == 0)
        //    {
        //        return await Add(repairRequest, cn);
        //    }
        //    var exist = await unitOfWork.Repository<RepairRequest>().TableNoTracking.AnyAsync(c => c.Id == repairRequest.Id);
        //    if (exist)
        //    {
        //        return await Update(repairRequest, cn);
        //    }
        //    return await Add(repairRequest, cn);
        //}

        [HttpPost("[action]")]
        [ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
        public async Task<IActionResult> Save(RepairRequest repairRequest, CancellationToken cn)
        {
            if (repairRequest.Id == 0)
            {
                var result = await Add(repairRequest, cn);
                if (result is OkResult || result is OkObjectResult)
                {
                    return Ok(new { id = repairRequest.Id });
                }
                return result;
            }

            var exist = await unitOfWork.Repository<RepairRequest>().TableNoTracking.AnyAsync(c => c.Id == repairRequest.Id);
            if (exist)
            {
                var result = await Update(repairRequest, cn);
                if (result is OkResult || result is OkObjectResult)
                {
                    return Ok(new { id = repairRequest.Id });
                }
                return result;
            }

            var addResult = await Add(repairRequest, cn);
            if (addResult is OkResult || addResult is OkObjectResult)
            {
                return Ok(new { id = repairRequest.Id });
            }
            return addResult;
        }



        [HttpPost("[action]")]
        [ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
        public async Task<IActionResult> Add(RepairRequest repairRequest, CancellationToken cn)
        {
            var entity = await unitOfWork.Repository<RepairRequest>().SaveAsync(repairRequest, cn, true);
            return Ok(entity);
        }

        [HttpPost("[action]")]
        [ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
        public async Task<IActionResult> Update(RepairRequest repairRequest, CancellationToken cn)
        {
            var entity = await unitOfWork.Repository<RepairRequest>().UpdateAsync(repairRequest, cn, true);
            return Ok(entity);
        }

        [HttpDelete("[action]")]
        [ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
        public async Task<IActionResult> Delete(long id, CancellationToken cn)
        {

            var model = unitOfWork.Repository<RepairRequest>().Table.FirstOrDefault(c => c.Id == id);
            if (model != null)
                await unitOfWork.Repository<RepairRequest>().DeleteAsync(model, cn, true);
            return Ok();
        }

        [HttpGet("[action]")]
        [ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
        public IActionResult Edit(long? id)
        {
            ViewBag.Title = "ویرایش تعمیرات";
            if (id != null && id != 0)
            {
                var entity = unitOfWork.Repository<RepairRequest>().TableNoTracking
                    .Include(c => c.Customer)
                    .Include(c => c.DL)
                    .FirstOrDefault(c => c.Id == id);
                return View(@"\Views\Panel\Rpr\RepairRequest\Edit.cshtml", entity);
            }
            var newEntity = new RepairRequest();
            return View(@"\Views\Panel\Rpr\RepairRequest\Edit.cshtml", newEntity);
        }

        [HttpGet("[action]")]
        [ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
        public IActionResult New()
        {
            ViewBag.Title = "درخواست تعمیرات";
            var newEntity = new RepairRequest();
            return View(@"\Views\Panel\Rpr\RepairRequest\Edit.cshtml", newEntity);

        }

        [HttpGet("[action]")]
        [ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
        public IActionResult List()
        {
            return View(@"\Views\Panel\Rpr\RepairRequest\List.cshtml");
        }

        [HttpPost("[action]")]
        [ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
        public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
        {
            var licensePath = WebHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
            var memoryStream = new MemoryStream();
            try
            {
                await unitOfWork.Repository<RepairRequest>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
        public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
        {
            return Ok(await unitOfWork.Repository<RepairRequest>().FetchDataAsync(request, cn));
        }


        [HttpGet("[action]")]
        public IActionResult RepairRequestPartItems(CancellationToken cn)
        {
            return PartialView(@"\Views\Panel\Rpr\RepairRequest\_RepairRequestPartItems.cshtml");
        }

        //[HttpGet("[action]")]
        //public IActionResult RepairRequestComment(long repairRequestId, CancellationToken cancellationToken)
        //{
        //    ViewBag.RepairRequestId = repairRequestId;
        //    return PartialView(@"\Views\Panel\Rpr\RepairRequest\Comment\List.cshtml");
        //}


        [HttpGet("[action]")]
        public async Task<IActionResult> GetRepairRequestComment(long repairRequestId)
        {
            var list = await unitOfWork.Repository<RepairRequestComment>()
                .TableNoTracking.Where(it => it.RepairRequestId == repairRequestId)
                .ToListAsync();
            return PartialView(@"\Views\Panel\Rpr\RepairRequest\Comment\List.cshtml", list);

        }

        [HttpDelete("[action]")]
        public async Task<IActionResult> DeleteComment([FromQuery] long id, CancellationToken cancellationToken)
        {
            var model = await unitOfWork.Repository<RepairRequestComment>()
                .Table
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (model == null)
                return NotFound(new { success = false, message = "رکورد یافت نشد" });

            await unitOfWork.Repository<RepairRequestComment>().DeleteAsync(model, cancellationToken, true);

            return Ok(new { success = true, message = "حذف با موفقیت انجام شد" });
        }



        [HttpGet("[action]")]
        public IActionResult ShowContractorForm(long repairRequestId, CancellationToken cancellationToken)
        {
            ViewBag.RepairRequestId = repairRequestId;
            var model = new RepairRequestContractor
            {
                RepairRequestId = repairRequestId
            };
            return PartialView(@"\Views\Panel\Rpr\RepairRequest\Contractor\Edit.cshtml", model);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> EditContractorForm(long? id, long? repairRequestId, CancellationToken cancellationToken)
        {
            var entity = await unitOfWork.Repository<RepairRequestContractor>().TableNoTracking
               .FirstAsync(it => it.Id == id && it.RepairRequestId == repairRequestId);

            if (entity == null)
            {
                return NotFound("رکورد مورد نظر یافت نشد");
            }

            ViewBag.RepairRequestId = entity.RepairRequestId;

            return PartialView(@"\Views\Panel\Rpr\RepairRequest\Contractor\Edit.cshtml", entity);
        }


        [HttpPost("[action]")]
        [ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
        public async Task<IActionResult> AddRepairRequestContractor(RepairRequestContractor repairRequestContractor, CancellationToken cn)
        {
            var entity = await unitOfWork.Repository<RepairRequestContractor>().SaveAsync(repairRequestContractor, cn, true);
            return Ok(entity);
        }



        [HttpGet("[action]")]
        public async Task<IActionResult> GetRepairRequestContractor(long repairRequestId)
        {
            try
            {
                var list = await unitOfWork.Repository<RepairRequestContractor>()
                    .TableNoTracking
                    .Where(it => it.RepairRequestId == repairRequestId)
                    .Include(it => it.Contractor)
                    .Include(it => it.Part)
                    .ToListAsync();

                return Ok(list);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Error: {ex.InnerException.Message}");
                }

                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                    innerMessage = ex.InnerException?.Message
                });
            }
        }


        [HttpDelete("[action]")]
        public async Task<IActionResult> DeleteContractor([FromQuery] long id, CancellationToken cancellationToken)
        {
            var model = await unitOfWork.Repository<RepairRequestContractor>()
                .Table
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (model == null)
                return NotFound(new { success = false, message = "رکورد یافت نشد" });

            await unitOfWork.Repository<RepairRequestContractor>().DeleteAsync(model, cancellationToken, true);

            return Ok(new { success = true, message = "حذف با موفقیت انجام شد" });
        }

        [HttpGet("[action]")]
        public IActionResult ShowPartFractionForm(CancellationToken cancellationToken)
        {            
            return PartialView(@"\Views\Panel\Rpr\RepairRequest\PartFraction\Edit.cshtml");
        }

        [HttpPost("[action]")]
        [ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
        public async Task<IActionResult> AddPartFraction(RepairRequestPartFraction partFraction, CancellationToken cn)
        {
            var entity = await unitOfWork.Repository<RepairRequestPartFraction>().SaveAsync(partFraction, cn, true);
            return Ok(entity);
        }

    }
}
