using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Data.SystemAuth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Entities.Base.DataTable;
using WebFramework.Filtters;
using WebFramework.Page;
using Entities.App.Gnr;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("صنعت شخص/شرکت", typeof(PartyIndustry))]
	public class PartyIndustryController(IUnitOfWork unitOfWork, IPropertyIdentityService identityService, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(PartyIndustry partyIndustry, CancellationToken cn)
		{
			if (partyIndustry.Id == null || partyIndustry.Id == 0)
			{
				return await Add(partyIndustry, cn);
			}
			var exist = await unitOfWork.Repository<PartyIndustry>().TableNoTracking.AnyAsync(c => c.Id == partyIndustry.Id);
			if (exist)
			{
				return await Update(partyIndustry, cn);
			}
			return await Add(partyIndustry, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(PartyIndustry partyIndustry, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<PartyIndustry>().SaveAsync(partyIndustry, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(PartyIndustry partyIndustry, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<PartyIndustry>().UpdateAsync(partyIndustry, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<PartyIndustry>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<PartyIndustry>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<PartyIndustry>().TableNoTracking
					.Include(c => c.PartyIndustryItem)
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Gnr\PartyIndustry\Edit.cshtml", entity);
			}
			var newEntity = new PartyIndustry();
			return View(@"\Views\Panel\Gnr\PartyIndustry\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var newEntity = new PartyIndustry();
			return View(@"\Views\Panel\Gnr\PartyIndustry\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Gnr\PartyIndustry\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<PartyIndustry>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<PartyIndustry>().FetchDataAsync(request, cn));
		}

		[HttpGet("[action]")]
		public IActionResult PartyIndustryItemPartial()
		{
			return PartialView(@"\Views\Panel\Gnr\PartyIndustry\_PartyIndustryItemPartial.cshtml");
		}
	}
}
