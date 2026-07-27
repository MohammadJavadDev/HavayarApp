using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Sale;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
    [Route("Panel/Sale/[controller]")]
    [ApiController]
    [ApiResultFilter]
    [ControllerInfo("درخواست کالا-نمایندگان", typeof(ServiceRequestPartAgency))]
    public class ServiceRequestPartAgencyController(IUnitOfWork unitOfWork, IWebHostEnvironment _webHostEnvironment) : BaseController
    {
        [HttpPost("[action]")]
        [ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
        public async Task<IActionResult> Save(ServiceRequestPartAgency partAgency, CancellationToken cn)
        {
            if (partAgency.Id == null || partAgency.Id == 0)
            {
                return await Add(partAgency, cn);
            }
            var exist = await unitOfWork.Repository<ServiceRequestPartAgency>().TableNoTracking.AnyAsync(c => c.Id == partAgency.Id);
            if (exist)
            {
                return await Update(partAgency, cn);
            }
            return await Add(partAgency, cn);
        }

        [HttpPost("[action]")]
        [ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
        public async Task<IActionResult> Add(ServiceRequestPartAgency partAgency, CancellationToken cn)
        {
            var entity = await unitOfWork.Repository<ServiceRequestPartAgency>().SaveAsync(partAgency, cn, true);
            return Ok(entity);
        }

        [HttpPost("[action]")]
        [ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
        public async Task<IActionResult> Update(ServiceRequestPartAgency partAgency, CancellationToken cn)
        {
            var entity = await unitOfWork.Repository<ServiceRequestPartAgency>().UpdateAsync(partAgency, cn, true);
            return Ok(entity);
        }

        [HttpGet("[action]")]
        [ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
        public async Task<IActionResult> Delete(long id, CancellationToken cn)
        {
            var model = unitOfWork.Repository<ServiceRequestPartAgency>().TableNoTracking.FirstOrDefault(c => c.Id == id);
            if (model != null)
                await unitOfWork.Repository<ServiceRequestPartAgency>().DeleteAsync(model, cn, true);
            return Ok();
        }

        [HttpGet("[action]")]
        [ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
        public IActionResult Edit(long? id)
        {
            if (id != null && id != 0)
            {
                var entity = unitOfWork.Repository<ServiceRequestPartAgency>().TableNoTracking
                    .FirstOrDefault(c => c.Id == id);
                return View(@"\Views\Panel\Sale\ServiceRequest\AgencyProductRequest\Edit.cshtml", entity);
            }
            var newEntity = new ServiceRequestPartAgency();
            return View(@"\Views\Panel\Sale\ServiceRequest\AgencyProductRequest\Edit.cshtml", newEntity);
        }

        [HttpGet("[action]")]
        [ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
        public IActionResult New()
        {
            var newEntity = new ServiceRequestPartAgency();
            return View(@"\Views\Panel\Sale\ServiceRequest\AgencyProductRequest\Edit.cshtml", newEntity);
        }

        [HttpGet("[action]")]
        [ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
        public IActionResult List()
        {
            return View(@"\Views\Panel\Sale\ServiceRequest\AgencyProductRequest\List.cshtml");
        }

        [HttpPost("[action]")]
        [ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
        public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
        {
            var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
            var memoryStream = new MemoryStream();
            try
            {
                await unitOfWork.Repository<ServiceRequestPartAgency>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
            return Ok(await unitOfWork.Repository<ServiceRequestPartAgency>().FetchDataAsync(request, cn));
        }



        //[HttpGet("[action]")]
        //public IActionResult ShowProductDetailForm(long? serviceRequestId, CancellationToken cancellationToken)
        //{
        //    ViewBag.ServiceRequestId = serviceRequestId;
        //    var model = new ServiceRequestDetail
        //    {
        //        ServiceRequestId = serviceRequestId
        //    };
        //    return PartialView(@"\Views\Panel\Sale\ServiceRequest\ProductDetail\Edit.cshtml", model);
        //}


        //[HttpPost("[action]")]
        //[ActionDisplayName("ذخیره جزئیات", ActionAccessType.Api, ActionAccessItemType.Save)]
        //public async Task<IActionResult> AddRepairProductDetail(ServiceRequestDetail serviceRequestDetail, CancellationToken cn)
        //{
        //    if (serviceRequestDetail.Id == 0 || serviceRequestDetail.Id == null)
        //    {
        //        var entity = await unitOfWork.Repository<ServiceRequestDetail>().SaveAsync(serviceRequestDetail, cn, true);
        //        return Ok(new { isSuccess = true, message = "با موفقیت درج شد", data = entity });
        //    }
        //    else
        //    {
        //        var entity = await unitOfWork.Repository<ServiceRequestDetail>().UpdateAsync(serviceRequestDetail, cn, true);
        //        return Ok(new { isSuccess = true, message = "با موفقیت ویرایش شد", data = entity });
        //    }
        //}


        //[HttpGet("[action]")]
        //public async Task<IActionResult> GetServiceRequest(long serviceRequestId)
        //{
        //    try
        //    {
        //        var list = await unitOfWork.Repository<ServiceRequestDetail>()
        //            .TableNoTracking
        //            .Where(it => it.ServiceRequestId == serviceRequestId)
        //            .ToListAsync();

        //        return Ok(list);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error: {ex.Message}");
        //        Console.WriteLine($"Stack Trace: {ex.StackTrace}");

        //        if (ex.InnerException != null)
        //        {
        //            Console.WriteLine($"Inner Error: {ex.InnerException.Message}");
        //        }

        //        return BadRequest(new
        //        {
        //            success = false,
        //            message = ex.Message,
        //            innerMessage = ex.InnerException?.Message
        //        });
        //    }
        //}


        //[HttpDelete("[action]")]
        //public async Task<IActionResult> DeleteServiceRequest([FromQuery] long? id, CancellationToken cancellationToken)
        //{
        //    var model = await unitOfWork.Repository<ServiceRequestDetail>().Table
        //        .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        //    if (model == null)
        //        return NotFound(new { success = false, message = "رکورد یافت نشد" });

        //    await unitOfWork.Repository<ServiceRequestDetail>().DeleteAsync(model, cancellationToken, true);

        //    return Ok(new
        //    {
        //        data = new
        //        {
        //            success = true,
        //            message = "حذف با موفقیت انجام شد"
        //        }
        //    });

        //}


        //[HttpGet("[action]")]
        //public async Task<IActionResult> EditServiceRequest(long? id, long? serviceRequestId, CancellationToken cancellationToken)
        //{
        //    var entity = await unitOfWork.Repository<ServiceRequestDetail>().TableNoTracking
        //       .FirstAsync(it => it.Id == id && it.ServiceRequestId == serviceRequestId);

        //    if (entity == null)
        //    {
        //        return NotFound("رکورد مورد نظر یافت نشد");
        //    }

        //    ViewBag.serviceRequestId = entity.ServiceRequestId;

        //    return PartialView(@"\Views\Panel\Sale\ServiceRequest\ProductDetail\Edit.cshtml", entity);
        //}

    }
}
