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
	[ControllerInfo("اماکن / مناطق (حراست/دژبان)", typeof(GuardLocation))]
	public class GuardLocationController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		private static string? Validate(GuardLocation model)
		{
			if (string.IsNullOrWhiteSpace(model.Title))
				return "عنوان نمی‌تواند خالی باشد";
			return null;
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(GuardLocation model, CancellationToken cn)
		{
			var error = Validate(model);
			if (error != null) return BadRequest(error);
			model.Id = null;
			return Ok(await unitOfWork.Repository<GuardLocation>().AddAsync(model, cn, true));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(GuardLocation model, CancellationToken cn)
		{
			var error = Validate(model);
			if (error != null) return BadRequest(error);
			return Ok(await unitOfWork.Repository<GuardLocation>().UpdateAsync(model, cn, true));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<GuardLocation>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null) await unitOfWork.Repository<GuardLocation>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			var entity = (id != null && id != 0)
				? unitOfWork.Repository<GuardLocation>().TableNoTracking.FirstOrDefault(c => c.Id == id)
				: new GuardLocation();
			return View(@"\Views\Panel\Hrm\GuardLocation\Edit.cshtml", entity ?? new GuardLocation());
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
			=> View(@"\Views\Panel\Hrm\GuardLocation\Edit.cshtml", new GuardLocation());

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Hrm\GuardLocation\List.cshtml");

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<GuardLocation>().FetchDataAsync(request, cn));
	}
}
