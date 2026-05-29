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
    [ControllerInfo("کامنت", typeof(RepairRequestComment))]
    public class RepairRequestCommentController(IUnitOfWork unitOfWork, IWebHostEnvironment WebHostEnvironment) : BaseController
    {
        [HttpPost("[action]")]
        [ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
        public async Task<IActionResult> Save(RepairRequestComment repairRequestComment, CancellationToken cn)
        {

            var exist = await unitOfWork.Repository<RepairRequestComment>().TableNoTracking.AnyAsync(c => c.Id == repairRequestComment.Id);
            if (exist)
            {
                return await Update(repairRequestComment, cn);
            }
            return await Add(repairRequestComment, cn);
        }

        [HttpPost("[action]")]
        [ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
        public async Task<IActionResult> Add(RepairRequestComment repairRequestComment, CancellationToken cn)
        {
            var entity = await unitOfWork.Repository<RepairRequestComment>().SaveAsync(repairRequestComment, cn, true);
            return Ok(entity);
        }

        [HttpPost("[action]")]
        [ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
        public async Task<IActionResult> Update(RepairRequestComment repairRequestComment, CancellationToken cn)
        {
            var entity = await unitOfWork.Repository<RepairRequestComment>().UpdateAsync(repairRequestComment, cn, true);
            return Ok(entity);
        }

        [HttpDelete("[action]")]
        [ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
        public async Task<IActionResult> Delete(long id, CancellationToken cn)
        {

            var model = unitOfWork.Repository<RepairRequestComment>().Table.FirstOrDefault(c => c.Id == id);
            if (model != null)
                await unitOfWork.Repository<RepairRequestComment>().DeleteAsync(model, cn, true);
            return Ok();
        }

        [HttpGet("[action]")]
        [ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
        public IActionResult Edit(long? id)
        {
            ViewBag.Title = "ویرایش کامنت";
            if (id != null && id != 0)
            {
                var entity = unitOfWork.Repository<RepairRequestComment>().TableNoTracking
                    .FirstOrDefault(c => c.Id == id);

                // ریختن entity در ViewBag
                ViewBag.RepairRequestViewData = entity;

                return View(@"\Views\Panel\Rpr\RepairRequest\Comment\Edit.cshtml", entity);
            }

            var newEntity = new RepairRequestComment();
            return View(@"\Views\Panel\Rpr\RepairRequest\Comment\Edit.cshtml", newEntity);
        }

        [HttpGet("[action]")]
        [ActionDisplayName("درج کامنت", ActionAccessType.View, ActionAccessItemType.Create)]
        public IActionResult New()
        {
            ViewBag.Title = "درخواست کامنت";
            var newEntity = new RepairRequestComment();
            return View(@"\Views\Panel\Rpr\RepairRequest\Comment\Edit.cshtml", newEntity);

        }

        [HttpGet("[action]")]
        [ActionDisplayName("لیست کامنت", ActionAccessType.View, ActionAccessItemType.List)]
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
                await unitOfWork.Repository<RepairRequestComment>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
            return Ok(await unitOfWork.Repository<RepairRequestComment>().FetchDataAsync(request, cn));
        }


        [HttpGet("[action]")]
        public IActionResult CommentRepairRequest(long repairRequestId)
        {

            var comment = unitOfWork.Repository<RepairRequestComment>()
                .TableNoTracking
                .Include(c => c.RepairRequest)
                .FirstOrDefault(c => c.Id == repairRequestId);
            return PartialView(@"\Views\Panel\Rpr\RepairRequest\Comment\_AddComment.cshtml", comment);
        }


        [HttpGet("[action]")]
        public IActionResult AddRepairRequestComment(long? id, long repairRequestCommentId)
        {
            if (id != null && id > 0)
            {
                var comment = unitOfWork.Repository<RepairRequestComment>()
                    .TableNoTracking
                    .Include(c => c.RepairRequest)
                    .FirstOrDefault(c => c.Id == id);
                return PartialView(@"\Views\Panel\Rpr\RepairRequest\Comment\_AddComments.cshtml", comment);
            }

            var newComment = new RepairRequestComment
            {
                RepairRequestId = repairRequestCommentId
            };

            return PartialView(@"\Views\Panel\Rpr\RepairRequest\Comment\_AddComments.cshtml", newComment);
        }


        /// <summary>
        /// Confirm Date تایید تاریخ
        /// </summary>
        /// <param name="id"></param>
        /// <param name="repairRequestCommentId"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        public IActionResult ConfirmDate(long? id, DateTime? PreCheckDate, DateTime? FinancialProposalMiladiDate,DateTime? IsApprovedFinancialProposalDate)
        {
            if (id != null && id > 0)
            {
                var comment = unitOfWork.Repository<RepairRequest>()
                    .TableNoTracking
                    .FirstOrDefault(c => c.Id == id);
                return PartialView(@"\Views\Panel\Rpr\RepairRequest\ConfirmDate\_AddConfirmDate.cshtml", comment);
            }

            var newComment = new RepairRequest
            {
                Id = id
            };

            return PartialView(@"\Views\Panel\Rpr\RepairRequest\ConfirmDate\_AddConfirmDate.cshtml", newComment);
        }




        [HttpPost("[action]")]
        public async Task<IActionResult> UpdateConfirmDate(RepairRequest request, CancellationToken cn)
        {
            try
            {
                if (request.Id == 0 || request.Id == null)
                {
                    request.Id = 10;
                }
                await unitOfWork.Repository<RepairRequest>().UpdateAsync(request, cn);
                await unitOfWork.SaveChangesAsync(cn);

                return Ok(new
                {
                    message = "تاریخ با موفقیت ثبت شد",
                    commentId = request.Id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "خطا در ثبت اطلاعات: " + ex.Message);
            }
        }



        [HttpPost("[action]")]
        public async Task<IActionResult> CreateRepairRequestComment(RepairRequestComment request, CancellationToken cn)
        {
            try
            {
                if (request.RepairRequestId == 0 || request.RepairRequestId == null)
                {
                    request.RepairRequestId = 10;
                }
                await unitOfWork.Repository<RepairRequestComment>().AddAsync(request, cn);
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


        //[HttpGet("[action]")]
        //[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
        //public IActionResult Edit(long? id)
        //{
        //    if (id != null && id != 0)
        //    {
        //        var entity = unitOfWork.Repository<ProductionOrderItem>().TableNoTracking
        //            .Include(c => c.ProductionOrder)
        //            .Include(c => c.Part)
        //            .FirstOrDefault(c => c.Id == id);


        //        ViewBag.ProductionOrderItemBomViewData = unitOfWork
        //            .Repository<ProductionOrderItemBom>()
        //            .TableNoTracking
        //            .Include(c => c.Part)
        //            .ThenInclude(c => c.Unit)
        //            .Where(c => c.ProductionOrderItemId == id)
        //            .Select(c => new ProductionOrderItemBomViewModel()
        //            {
        //                ProductionOrderItemId = c.ProductionOrderItemId,
        //                Amount = c.Amount,
        //                CreatedByName = c.CreatedByName,
        //                CreatedOnMiladiDateTime = c.CreatedOnMiladiDateTime,
        //                CreatedOnShamsiDateTime = c.CreatedOnShamsiDateTime,
        //                Description = c.Description,
        //                Id = c.Id,
        //                IsLatest = c.IsLatest,
        //                IsActive = c.IsActive,
        //                ModifiedByName = c.ModifiedByName,
        //                ModifiedDateMiladiDateTime = c.ModifiedDateMiladiDateTime,
        //                ModifiedDateShamsiDateTime = c.ModifiedDateShamsiDateTime,
        //                NeedsAVL = c.NeedsAVL,
        //                PartId = c.PartId,
        //                PartName = c.Part.Name,
        //                PartCode = c.Part.Code,
        //                Revision = c.Revision,
        //                SaleUnitDetails = c.SaleUnitDetails,
        //                PartUnitName = c.Part.Unit.Title,
        //                ProductionStep = c.ProductionStep,
        //                NumberSupplied = c.NumberSupplied,
        //                Status = c.Status
        //            }).ToList();

        //        return View(@"\Views\Panel\Sale\ProductionOrderItem\Edit.cshtml", entity);
        //    }
        //    var newEntity = new ProductionOrderItem();
        //    return View(@"\Views\Panel\Sale\ProductionOrderItem\Edit.cshtml", newEntity);
        //}



    }

}
