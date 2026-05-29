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
	[ControllerInfo("استعلام قیمت کالا", typeof(InquiryPartPrice))]
	public class InquiryPartPriceController(IUnitOfWork unitOfWork, IPropertyIdentityService identityService, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(InquiryPartPrice inquiryPartPrice, CancellationToken cn)
		{
			if (inquiryPartPrice.Id == null || inquiryPartPrice.Id == 0)
			{
				return await Add(inquiryPartPrice, cn);
			}
			var exist = await unitOfWork.Repository<InquiryPartPrice>().TableNoTracking.AnyAsync(c => c.Id == inquiryPartPrice.Id);
			if (exist)
			{
				return await Update(inquiryPartPrice, cn);
			}
			return await Add(inquiryPartPrice, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(InquiryPartPrice inquiryPartPrice, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<InquiryPartPrice>().SaveAsync(inquiryPartPrice, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(InquiryPartPrice inquiryPartPrice, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<InquiryPartPrice>().UpdateAsync(inquiryPartPrice, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<InquiryPartPrice>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<InquiryPartPrice>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<InquiryPartPrice>().TableNoTracking
					.Include(c => c.Part)
					.Include(c => c.PriceUnit)
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Sup\InquiryPartPrice\Edit.cshtml", entity);
			}
			var newEntity = new InquiryPartPrice();
			return View(@"\Views\Panel\Sup\InquiryPartPrice\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var newEntity = new InquiryPartPrice();
			return View(@"\Views\Panel\Sup\InquiryPartPrice\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Sup\InquiryPartPrice\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<InquiryPartPrice>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<InquiryPartPrice>().FetchDataAsync(request, cn));
		}
	}
}
