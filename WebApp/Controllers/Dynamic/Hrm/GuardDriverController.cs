using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Hrm;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Hrm/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("راننده (حراست/دژبان)", typeof(GuardDriver))]
	public class GuardDriverController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		private static string? Validate(GuardDriver model)
		{
			if (string.IsNullOrWhiteSpace(model.Name))
				return "نام نمی‌تواند خالی باشد";
			if (string.IsNullOrWhiteSpace(model.Family))
				return "نام خانوادگی نمی‌تواند خالی باشد";
			if (!Enum.IsDefined(model.CarType))
				return "نوع وسیله نقلیه نمی‌تواند خالی باشد";
			return null;
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(GuardDriver model, CancellationToken cn)
		{
			var error = Validate(model);
			if (error != null) return BadRequest(error);
			model.Id = null;
			return Ok(await unitOfWork.Repository<GuardDriver>().AddAsync(model, cn, true));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(GuardDriver model, CancellationToken cn)
		{
			var error = Validate(model);
			if (error != null) return BadRequest(error);
			return Ok(await unitOfWork.Repository<GuardDriver>().UpdateAsync(model, cn, true));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<GuardDriver>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null) await unitOfWork.Repository<GuardDriver>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			var entity = (id != null && id != 0)
				? unitOfWork.Repository<GuardDriver>().TableNoTracking.FirstOrDefault(c => c.Id == id)
				: new GuardDriver();
			return View(@"\Views\Panel\Hrm\GuardDriver\Edit.cshtml", entity ?? new GuardDriver());
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
			=> View(@"\Views\Panel\Hrm\GuardDriver\Edit.cshtml", new GuardDriver());

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Hrm\GuardDriver\List.cshtml");

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<GuardDriver>().FetchDataAsync(request, cn));
	}
}
