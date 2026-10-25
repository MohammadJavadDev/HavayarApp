using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Sec;
using Entities.Base;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Sec/EmploymentStatuteSpecial")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("لیست استثنائات سابقه حضور", typeof(EmploymentStatuteSpecial))]
	public class EmploymentStatuteSpecialController(IUnitOfWork unitOfWork) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(EmploymentStatuteSpecial model, CancellationToken cn)
		{
			Normalize(model);
			if (model.PersonelId == 0)
				return BadRequest("پرسنل الزامی است");

			if (model.Id == null || model.Id == 0)
				return await Add(model, cn);

			if (await unitOfWork.Repository<EmploymentStatuteSpecial>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);

			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(EmploymentStatuteSpecial model, CancellationToken cn)
		{
			Normalize(model);
			if (model.PersonelId == 0)
				return BadRequest("پرسنل الزامی است");

			model.IsActive ??= IsActiveEnum.Active;
			var entity = await unitOfWork.Repository<EmploymentStatuteSpecial>().SaveAsync(model, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(EmploymentStatuteSpecial model, CancellationToken cn)
		{
			Normalize(model);
			if (model.PersonelId == 0)
				return BadRequest("پرسنل الزامی است");

			var existing = await unitOfWork.Repository<EmploymentStatuteSpecial>().Table
				.FirstOrDefaultAsync(c => c.Id == model.Id, cn);
			if (existing == null)
				return BadRequest("رکورد یافت نشد");

			existing.PersonelId = model.PersonelId;
			existing.RealEmploymentShamsiDate = model.RealEmploymentShamsiDate;
			existing.RealEmploymentMiladiDate = model.RealEmploymentMiladiDate;
			existing.AdditionalYear = model.AdditionalYear;
			existing.AdditionalMonth = model.AdditionalMonth;

			var entity = await unitOfWork.Repository<EmploymentStatuteSpecial>().SaveAsync(existing, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = await unitOfWork.Repository<EmploymentStatuteSpecial>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (model != null)
				await unitOfWork.Repository<EmploymentStatuteSpecial>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<EmploymentStatuteSpecial>().TableNoTracking
					.Include(c => c.Personel)
					.FirstOrDefault(c => c.Id == id) ?? new EmploymentStatuteSpecial();
				return View(@"\Views\Panel\Sec\EmploymentStatuteSpecial\Edit.cshtml", entity);
			}

			return View(@"\Views\Panel\Sec\EmploymentStatuteSpecial\Edit.cshtml", new EmploymentStatuteSpecial());
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
			=> View(@"\Views\Panel\Sec\EmploymentStatuteSpecial\Edit.cshtml", new EmploymentStatuteSpecial());

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Sec\EmploymentStatuteSpecial\List.cshtml");

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<EmploymentStatuteSpecial>().FetchDataAsync(request, cn));

		private static void Normalize(EmploymentStatuteSpecial model)
		{
			var shamsi = model.RealEmploymentShamsiDate?.Trim();
			model.RealEmploymentShamsiDate = string.IsNullOrEmpty(shamsi) ? null : shamsi;
			if (model.RealEmploymentShamsiDate == null)
				model.RealEmploymentMiladiDate = null;
		}
	}
}
