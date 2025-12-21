using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Data.SystemAuth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Entities.Base.DataTable;
using WebFramework.Filtters;
using WebFramework.Page;
using Entities.App.Bom;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Bom/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("درخواست ویرایش اقلام فرمول", typeof(FormulChangeRequest))]
	public class FormulChangeRequestController(IUnitOfWork unitOfWork, IPropertyIdentityService identityService, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(FormulChangeRequest formulChangeRequest, CancellationToken cn)
		{
			if (formulChangeRequest.Id == null || formulChangeRequest.Id == 0)
			{
				return await Add(formulChangeRequest, cn);
			}
			var exist = await unitOfWork.Repository<FormulChangeRequest>().TableNoTracking.AnyAsync(c => c.Id == formulChangeRequest.Id);
			if (exist)
			{
				return await Update(formulChangeRequest, cn);
			}
			return await Add(formulChangeRequest, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(FormulChangeRequest formulChangeRequest, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<FormulChangeRequest>().SaveAsync(formulChangeRequest, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(FormulChangeRequest formulChangeRequest, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<FormulChangeRequest>().UpdateAsync(formulChangeRequest, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<FormulChangeRequest>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<FormulChangeRequest>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<FormulChangeRequest>().TableNoTracking
					.Include(c => c.Part)
					.Include(c => c.ReplacementPart)
					.Include(c => c.Part)
					.Include(c => c.ReplacementPart)
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Bom\FormulChangeRequest\Edit.cshtml", entity);
			}
			var newEntity = new FormulChangeRequest();
			return View(@"\Views\Panel\Bom\FormulChangeRequest\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var newEntity = new FormulChangeRequest();
			return View(@"\Views\Panel\Bom\FormulChangeRequest\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Bom\FormulChangeRequest\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<FormulChangeRequest>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<FormulChangeRequest>().FetchDataAsync(request, cn));
		}
	}
}
