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
	[ControllerInfo("پیمانکار (حراست/دژبان)", typeof(GuardContractor))]
	public class GuardContractorController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		private static string? Validate(GuardContractor model)
		{
			if (string.IsNullOrWhiteSpace(model.Name))
				return "نام نمی‌تواند خالی باشد";
			if (string.IsNullOrWhiteSpace(model.Family))
				return "نام خانوادگی نمی‌تواند خالی باشد";
			if (string.IsNullOrWhiteSpace(model.PhoneNumber))
				return "شماره تماس نمی‌تواند خالی باشد";
			return null;
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(GuardContractor model, CancellationToken cn)
		{
			var error = Validate(model);
			if (error != null) return BadRequest(error);
			model.Id = null;
			return Ok(await unitOfWork.Repository<GuardContractor>().AddAsync(model, cn, true));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(GuardContractor model, CancellationToken cn)
		{
			var error = Validate(model);
			if (error != null) return BadRequest(error);
			return Ok(await unitOfWork.Repository<GuardContractor>().UpdateAsync(model, cn, true));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<GuardContractor>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null) await unitOfWork.Repository<GuardContractor>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			var entity = (id != null && id != 0)
				? unitOfWork.Repository<GuardContractor>().TableNoTracking.FirstOrDefault(c => c.Id == id)
				: new GuardContractor();
			return View(@"\Views\Panel\Hrm\GuardContractor\Edit.cshtml", entity ?? new GuardContractor());
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
			=> View(@"\Views\Panel\Hrm\GuardContractor\Edit.cshtml", new GuardContractor());

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Hrm\GuardContractor\List.cshtml");

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<GuardContractor>().FetchDataAsync(request, cn));
	}
}
