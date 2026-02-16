using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Data.SystemAuth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Entities.Base.DataTable;
using WebFramework.Filtters;
using WebFramework.Page;
using Entities.App.Sup;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Sup/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("دسته های خرید", typeof( BuyCategoryItem))]
	public class BuyCategoryItemController(IUnitOfWork unitOfWork, IPropertyIdentityService identityService, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(BuyCategoryItem buyCategory, CancellationToken cn)
		{
			if (buyCategory.Id == null || buyCategory.Id == 0)
			{
				return await Add(buyCategory, cn);
			}
			var exist = await unitOfWork.Repository<BuyCategoryItem>().TableNoTracking.AnyAsync(c => c.Id == buyCategory.Id);
			if (exist)
			{
				return await Update(buyCategory, cn);
			}
			return await Add(buyCategory, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(BuyCategoryItem buyCategory, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<BuyCategoryItem>().SaveAsync(buyCategory, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(BuyCategoryItem buyCategory, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<BuyCategoryItem>().UpdateAsync(buyCategory, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<BuyCategoryItem>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<BuyCategoryItem>().DeleteAsync(model, cn, true);
			return Ok();
		}

	

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Sup\BuyCategory\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<BuyCategoryItem>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<BuyCategoryItem>().FetchDataAsync(request, cn));
		}

		[HttpGet("[action]")]
		public IActionResult BuyCategoryItemPartial()
		{
			return PartialView(@"\Views\Panel\Sup\BuyCategory\_BuyCategoryItemPartial.cshtml");
		}
	}
}
