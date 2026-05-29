using Aspose.Cells;
using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Data.Repositories;
using Data.SystemAuth;
using Entities.App.Prd;
using Entities.App.Prd.Enums;
using Entities.App.Sale;
using Entities.App.Sale.Enums;
using Entities.Auth;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("درخواست توقف", typeof(StopRequst))]
	public class StopRequstController(IUnitOfWork unitOfWork, IPropertyIdentityService identityService, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(StopRequst stopRequst, CancellationToken cn)
		{
			if (stopRequst.Id == null || stopRequst.Id == 0)
			{
				return await Add(stopRequst, cn);
			}
			var exist = await unitOfWork.Repository<StopRequst>().TableNoTracking.AnyAsync(c => c.Id == stopRequst.Id);
			if (exist)
			{
				return await Update(stopRequst, cn);
			}
			return await Add(stopRequst, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(StopRequst stopRequst, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<StopRequst>().SaveAsync(stopRequst, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(StopRequst stopRequst, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<StopRequst>().UpdateAsync(stopRequst, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<StopRequst>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<StopRequst>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<StopRequst>().TableNoTracking
					.Include(c => c.ProductionOrderItem)
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Prd\StopRequst\Edit.cshtml", entity);
			}
			var newEntity = new StopRequst();
			return View(@"\Views\Panel\Prd\StopRequst\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var newEntity = new StopRequst();
			return View(@"\Views\Panel\Prd\StopRequst\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Prd\StopRequst\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<StopRequst>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<StopRequst>().FetchDataAsync(request, cn));
		}

		private async Task AddCommentToProductionOrderItem(StopRequst model , CancellationToken cn)
		{
			var now = DateTime.Now;
			var persianNow = now.ToShamsiDateTime();

			var comment = new ProductionOrderItemComment()
			{
				ProductionOrderItemId = model.ProductionOrderItemId.Value,
				StartMiladiDateTime = now,
				StartShamsiDate = persianNow,
				IsForProductionMode = true,
				StopRequestId = model.Id,
				Comment = model.StopReasonTitles + " - ایجاد شده بصورت اتوماتیک از محل ایجاد توقف در ماژول توقف"
			};

			comment.ProductionStatus = model.ProductionStatus switch
			{
				StopRequstProductionStatusEnum.StartStopInProduction
				    => ProductionOrderItemProductionStatusEnum.ProductionStageStopStarted,
				StopRequstProductionStatusEnum.StartStopInTest
				 => ProductionOrderItemProductionStatusEnum.ProductionTestStageStopStarted,
				StopRequstProductionStatusEnum.StartStopInFinalInspection
				=> ProductionOrderItemProductionStatusEnum.FinalInspectionStageStopStarted,
					StopRequstProductionStatusEnum.StartStopDueToClientVisit
				=> ProductionOrderItemProductionStatusEnum.ClientVisitStopStarted,

			};

			await unitOfWork.Repository<ProductionOrderItemComment>()
		    .AddAsync(comment, cn);

			unitOfWork.Repository<ProductionOrderItem>()
		    .UpdateFieldsAsync(
				model.ProductionOrderItemId,
				c=>c.ProductionStatus
				);

		}
	}
}
