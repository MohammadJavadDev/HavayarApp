using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Sale;
using Entities.App.Sale.Enums;
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
        public IActionResult New(long? serviceRequestId)
        {
            var newEntity = new ServiceRequestPartAgency
            {
                ServiceRequestId = serviceRequestId
            };
            return View(@"\Views\Panel\Sale\ServiceRequest\AgencyProductRequest\Edit.cshtml", newEntity);
        }

        [HttpGet("[action]")]
        [ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
        public IActionResult List()
        {
            return View(@"\Views\Panel\Sale\ServiceRequest\AgencyProductRequest\List.cshtml");
        }

        [HttpGet("[action]")]
        [ActionDisplayName("لیست اطلاعات با شناسه درخواست پشتیبانی", ActionAccessType.View, ActionAccessItemType.Custom)]
        public IActionResult ListBy(long? serviceRequestId)
        {
            if (serviceRequestId == null || serviceRequestId == 0)
                throw new Exception("شناسه درخواست پشتیبانی نمیتواند خالی باشد.");

            var model = new ServiceRequestPartAgencyListByParentViewModel
            {
                ServiceRequestId = serviceRequestId.Value
            };

            return View(@"\Views\Panel\Sale\ServiceRequest\AgencyProductRequest\ListByParentId.cshtml", model);
        }

        [HttpGet("[action]")]
        [ActionDisplayName("دریافت لیست با شناسه درخواست پشتیبانی", ActionAccessType.Api, ActionAccessItemType.FetchData)]
        public IActionResult GetListByParentId(long? serviceRequestId)
        {
            if (serviceRequestId == null || serviceRequestId == 0)
                return BadRequest("شناسه درخواست پشتیبانی نمیتواند خالی باشد.");

            var items = unitOfWork
                .Repository<ServiceRequestPartAgency>()
                .TableNoTracking
                .Where(c => c.ServiceRequestId == serviceRequestId)
                .Select(c => new ServiceRequestPartAgencyListItemViewModel
                {
                    Id = c.Id,
                    ServiceRequestId = c.ServiceRequestId,
                    VoucherNumber = c.VoucherNumber,
                    VoucherShamsiDate = c.VoucherShamsiDate,
                    PartId = c.PartId,
                    PartCode = c.Part.Code,
                    PartName = c.Part.Name,
                    Mount = c.Mount,
                    RelatedProducts = c.RelatedProducts,
                    ReplacePartId = c.ReplacePartId,
                    ReplacePartCode = c.ReplacePart.Code,
                    ReplacePartName = c.ReplacePart.Name,
                    UnitPrice = c.UnitPrice,
                    ServiceTypes = c.ServiceTypes,
                    CostCenterId = c.CostCenterId,
                    CostCenterTitle = c.CostCenter.Title,
                    SupplierId = c.SupplierId,
                    SupplierName = c.Supplier.Party.FullName,
                    HasReturn_DamagedPart = c.HasReturn_DamagedPart,
                    ReturnLicence = c.ReturnLicence,
                    Comment = c.Comment
                })
                .ToList();

            return Ok(items);
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
    }

    public class ServiceRequestPartAgencyListByParentViewModel
    {
        public long ServiceRequestId { get; set; }
        public List<ServiceRequestPartAgencyListItemViewModel> Items { get; set; } = new();
    }

    public class ServiceRequestPartAgencyListItemViewModel
    {
        public long? Id { get; set; }
        public long? ServiceRequestId { get; set; }
        public long? VoucherNumber { get; set; }
        public string? VoucherShamsiDate { get; set; }
        public long? PartId { get; set; }
        public string? PartCode { get; set; }
        public string? PartName { get; set; }
        public decimal? Mount { get; set; }
        public string? RelatedProducts { get; set; }
        public long? ReplacePartId { get; set; }
        public string? ReplacePartCode { get; set; }
        public string? ReplacePartName { get; set; }
        public long? UnitPrice { get; set; }
        public ServiceTypesEnum ServiceTypes { get; set; }
        public long? CostCenterId { get; set; }
        public string? CostCenterTitle { get; set; }
        public long? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public bool HasReturn_DamagedPart { get; set; }
        public bool ReturnLicence { get; set; }
        public string? Comment { get; set; }
    }
}
