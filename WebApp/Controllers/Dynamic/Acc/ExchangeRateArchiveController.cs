using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Data.SystemAuth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Entities.Base.DataTable;
using WebFramework.Filtters;
using WebFramework.Page;
using Entities.App.Acc;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Acc/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("بایگانی نرخ ارز", typeof(ExchangeRateArchive))]
	public class ExchangeRateArchiveController(IUnitOfWork unitOfWork, IPropertyIdentityService identityService, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(ExchangeRateArchive exchangeRateArchive, CancellationToken cn)
		{
			if (exchangeRateArchive.Id == null || exchangeRateArchive.Id == 0)
			{
				return await Add(exchangeRateArchive, cn);
			}
			var exist = await unitOfWork.Repository<ExchangeRateArchive>().TableNoTracking.AnyAsync(c => c.Id == exchangeRateArchive.Id);
			if (exist)
			{
				return await Update(exchangeRateArchive, cn);
			}
			return await Add(exchangeRateArchive, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(ExchangeRateArchive exchangeRateArchive, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<ExchangeRateArchive>().SaveAsync(exchangeRateArchive, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(ExchangeRateArchive exchangeRateArchive, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<ExchangeRateArchive>().UpdateAsync(exchangeRateArchive, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<ExchangeRateArchive>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<ExchangeRateArchive>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<ExchangeRateArchive>().TableNoTracking
					.Include(c => c.PriceUnit)
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Acc\ExchangeRateArchive\Edit.cshtml", entity);
			}
			var newEntity = new ExchangeRateArchive();
			return View(@"\Views\Panel\Acc\ExchangeRateArchive\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var newEntity = new ExchangeRateArchive();
			return View(@"\Views\Panel\Acc\ExchangeRateArchive\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Acc\ExchangeRateArchive\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<ExchangeRateArchive>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<ExchangeRateArchive>().FetchDataAsync(request, cn));
		}
	}
}
