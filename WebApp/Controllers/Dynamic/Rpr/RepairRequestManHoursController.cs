using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Prp;
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
    [ControllerInfo("نفرساعت", typeof(RepairRequestManHours))]
    public class RepairRequestManHoursController(IUnitOfWork unitOfWork, IWebHostEnvironment WebHostEnvironment) : BaseController
    {
        public List<RepairRequestManHours> RepairRequestManHoursList { get; set; }
        [HttpPost("[action]")]
        [ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
        public async Task<IActionResult> Save(RepairRequestManHours repairRequestManHours, CancellationToken cn)
        {

            var exist = await unitOfWork.Repository<RepairRequestManHours>().TableNoTracking.AnyAsync(c => c.Id == repairRequestManHours.Id);
            if (exist)
            {
                return await Update(repairRequestManHours, cn);
            }
            return await Add(repairRequestManHours, cn);
        }

        [HttpPost("[action]")]
        [ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
        public async Task<IActionResult> Add(RepairRequestManHours repairRequestManHours, CancellationToken cn)
        {
            var entity = await unitOfWork.Repository<RepairRequestManHours>().SaveAsync(repairRequestManHours, cn, true);
            return Ok(entity);
        }

        [HttpPost("[action]")]
        [ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
        public async Task<IActionResult> Update(RepairRequestManHours repairRequestManHours, CancellationToken cn)
        {
            var entity = await unitOfWork.Repository<RepairRequestManHours>().UpdateAsync(repairRequestManHours, cn, true);
            return Ok(entity);
        }

        [HttpDelete("[action]")]
        [ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
        public async Task<IActionResult> Delete(long id, CancellationToken cn)
        {

            var model = unitOfWork.Repository<RepairRequestManHours>().Table.FirstOrDefault(c => c.Id == id);
            if (model != null)
                await unitOfWork.Repository<RepairRequestManHours>().DeleteAsync(model, cn, true);
            return Ok();
        }

        [HttpGet("[action]")]
        [ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
        public IActionResult Edit(long? id)
        {
            ViewBag.Title = "ویرایش کامنت";
            if (id != null && id != 0)
            {
                var entity = unitOfWork.Repository<RepairRequestManHours>().TableNoTracking
                    .FirstOrDefault(c => c.Id == id);

                // ریختن entity در ViewBag
                ViewBag.RepairRequestViewData = entity;

                return View(@"\Views\Panel\Rpr\RepairRequest\Comment\Edit.cshtml", entity);
            }

            var newEntity = new RepairRequestManHours();
            return View(@"\Views\Panel\Rpr\RepairRequest\Comment\Edit.cshtml", newEntity);
        }

        [HttpGet("[action]")]
        [ActionDisplayName("درج نفر ساعت", ActionAccessType.View, ActionAccessItemType.Create)]
        public IActionResult New()
        {
            ViewBag.Title = "نفر ساعت";
            var newEntity = new RepairRequestManHours();
            return View(@"\Views\Panel\Rpr\RepairRequest\Comment\Edit.cshtml", newEntity);

        }

        [HttpGet("[action]")]
        [ActionDisplayName("لیست نفر ساعت", ActionAccessType.View, ActionAccessItemType.List)]
        public IActionResult List()
        {
            return View(@"\Views\Panel\Rpr\RepairRequest\Comment\List.cshtml");
        }

        [HttpPost("[action]")]
        [ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
        public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
        {
            var licensePath = WebHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
            var memoryStream = new MemoryStream();
            try
            {
                await unitOfWork.Repository<RepairRequestManHours>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
            return Ok(await unitOfWork.Repository<RepairRequestManHours>().FetchDataAsync(request, cn));
        }


        [HttpGet("[action]")]
        public IActionResult CommentRepairRequest(long repairRequestId)
        {

            var comment = unitOfWork.Repository<RepairRequestManHours>()
                .TableNoTracking
                .Include(c => c.RepairRequest)
                .FirstOrDefault(c => c.Id == repairRequestId);
            return PartialView(@"\Views\Panel\Rpr\RepairRequest\Comment\_AddComment.cshtml", comment);
        }


        [HttpGet("[action]")]
        public IActionResult AddManHoursForm(long repairRequestId, CancellationToken cancellationToken)
        {
            ViewBag.RepairRequestId = repairRequestId;
            var model = new RepairRequestManHours
            {
                RepairRequestId = repairRequestId
            };
            return PartialView(@"\Views\Panel\Rpr\RepairRequest\PersonHour\AddManHours.cshtml", model);
        }




        [HttpPost("[action]")]
        public async Task<IActionResult> CreateRepairRequestManHours(RepairRequestManHours request, CancellationToken cn)
        {
            try
            {
                if (request.RepairRequestId == 0 || request.RepairRequestId == null)
                {
                    request.RepairRequestId = 10;
                }
                await unitOfWork.Repository<RepairRequestManHours>().AddAsync(request, cn);
                await unitOfWork.SaveChangesAsync(cn);

                return Ok(new
                {
                    message = "کامنت با موفقیت ثبت شد",
                    commentId = request.Id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "خطا در ثبت اطلاعات: " + ex.Message);
            }
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetManHoursList(long repairRequestId)
        {
            var list = await unitOfWork.Repository<RepairRequestManHours>()
                .TableNoTracking.Where(it=> it.RepairRequestId ==  repairRequestId)
                .Include(x => x.Personel)
                .OrderByDescending(x => x.WorkDate)
                .ThenBy(x => x.StartTime)
                .ToListAsync();

            return PartialView(@"\Views\Panel\Rpr\RepairRequest\PersonHour\List.cshtml", list);
            
        }


        [HttpDelete("[action]")]
        public async Task<IActionResult> DeleteManHours([FromQuery]long id, CancellationToken cn)
        {
            var model = await unitOfWork.Repository<RepairRequestManHours>()
                .Table
                .FirstOrDefaultAsync(x => x.Id == id, cn);

            if (model == null)
                return NotFound(new { success = false, message = "رکورد یافت نشد" });

            await unitOfWork.Repository<RepairRequestManHours>().DeleteAsync(model, cn, true);

            return Ok(new { success = true, message = "حذف با موفقیت انجام شد" });
        }
    }

}
