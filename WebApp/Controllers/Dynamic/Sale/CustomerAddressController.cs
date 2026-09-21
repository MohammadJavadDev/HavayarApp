using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.SLS;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/SLS/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("سایت های مشتریان", typeof(CustomerAddress))]
	public class CustomerAddressController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(CustomerAddress model, CancellationToken cn)
		{
			ClearNav(model);
			if (model.Id == null || model.Id == 0)
				return BadRequest("ایجاد سایت مشتری از این منو مجاز نیست.");
			return await Update(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(CustomerAddress model, CancellationToken cn)
		{
			ClearNav(model);
			if (model.Id == null || model.Id == 0)
				return BadRequest("ایجاد سایت مشتری از این منو مجاز نیست.");

			var existing = await unitOfWork.Repository<CustomerAddress>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == model.Id, cn);
			if (existing == null)
				return BadRequest("سایت مشتری یافت نشد.");

			model.CustomerId = existing.CustomerId;
			model.HtsId = existing.HtsId;
			model.HamkaranAddId = existing.HamkaranAddId;
			model.RahkaranId = existing.RahkaranId;
			model.RahkaranVersion = existing.RahkaranVersion;
			return Ok(await unitOfWork.Repository<CustomerAddress>().UpdateAsync(model, cn, true));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id == null || id == 0)
				return RedirectToAction(nameof(List));
			var entity = unitOfWork.Repository<CustomerAddress>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity == null)
				return RedirectToAction(nameof(List));
			return View(@"\Views\Panel\SLS\CustomerAddress\Edit.cshtml", entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\SLS\CustomerAddress\List.cshtml");

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<CustomerAddress>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message);
			}
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<CustomerAddress>().FetchDataAsync(request, cn));

		private static void ClearNav(CustomerAddress model)
		{
			model.Customer = null;
			model.Zone = null;
			model.Province = null;
			model.AgencyParty = null;
			model.AgencyDl = null;
		}
	}
}
