using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Cng;
using Entities.App.Cng.Enums;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Cng/MaintenanceContract")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("قرارداد تعمیر و نگهداشت", typeof(MaintenanceContract))]
	public class CngMaintenanceContractController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(MaintenanceContract model, CancellationToken cn)
		{
			if (model.AgencyId == 0)
				return BadRequest("نمایندگی نمی‌تواند خالی باشد");
			if (model.MonthlyPrice <= 0)
				return BadRequest("قیمت ماهانه باید بزرگ‌تر از صفر باشد");

			ClearNavigations(model);

			if (model.Id == null || model.Id == 0) return await Add(model, cn);
			if (await unitOfWork.Repository<MaintenanceContract>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(MaintenanceContract model, CancellationToken cn)
		{
			ClearNavigations(model);
			return Ok(await unitOfWork.Repository<MaintenanceContract>().SaveAsync(model, cn, true));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(MaintenanceContract model, CancellationToken cn)
		{
			ClearNavigations(model);
			return Ok(await unitOfWork.Repository<MaintenanceContract>().UpdateAsync(model, cn, true));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<MaintenanceContract>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null) await unitOfWork.Repository<MaintenanceContract>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			var entity = (id != null && id != 0)
				? unitOfWork.Repository<MaintenanceContract>().TableNoTracking
					.Include(c => c.Agency).ThenInclude(a => a!.Party)
					.Include(c => c.Station)
					.Include(c => c.StationBeneficiary).ThenInclude(b => b!.Party)
					.FirstOrDefault(c => c.Id == id)
				: CreateNew();
			return View(@"\Views\Panel\Cng\MaintenanceContract\Edit.cshtml", entity ?? CreateNew());
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
			=> View(@"\Views\Panel\Cng\MaintenanceContract\Edit.cshtml", CreateNew());

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Cng\MaintenanceContract\List.cshtml");

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + @"\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<MaintenanceContract>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex) { return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message); }
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<MaintenanceContract>().FetchDataAsync(request, cn));

		private static void ClearNavigations(MaintenanceContract model)
		{
			model.Agency = null;
			model.Station = null;
			model.StationBeneficiary = null;
		}

		private static MaintenanceContract CreateNew()
			=> new() { Status = MaintenanceContractStatusEnum.Current };
	}
}
