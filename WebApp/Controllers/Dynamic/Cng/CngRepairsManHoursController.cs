using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Cng;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Cng/RepairsManHours")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("نفرساعت تعمیرات CNG", typeof(RepairsManHours))]
	public class CngRepairsManHoursController(IUnitOfWork unitOfWork) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(RepairsManHours model, CancellationToken cn)
		{
			if (model.RepairsId == 0)
				return BadRequest("شناسه والد خالی است");
			if (model.PersonelId == 0)
				return BadRequest("پرسنل نمی‌تواند خالی باشد");

			ClearNav(model);
			if (model.Id == null || model.Id == 0)
				return await Add(model, cn);
			if (await unitOfWork.Repository<RepairsManHours>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(RepairsManHours model, CancellationToken cn)
		{
			ClearNav(model);
			return Ok(await unitOfWork.Repository<RepairsManHours>().SaveAsync(model, cn, true));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(RepairsManHours model, CancellationToken cn)
		{
			ClearNav(model);
			return Ok(await unitOfWork.Repository<RepairsManHours>().UpdateAsync(model, cn, true));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<RepairsManHours>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null)
				await unitOfWork.Repository<RepairsManHours>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id, long? repairsId = null)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<RepairsManHours>().TableNoTracking
					.Include(c => c.Personel)
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Cng\RepairsManHours\Edit.cshtml",
					entity ?? new RepairsManHours { RepairsId = repairsId ?? 0 });
			}
			return View(@"\Views\Panel\Cng\RepairsManHours\Edit.cshtml",
				new RepairsManHours { RepairsId = repairsId ?? 0 });
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New(long? repairsId = null)
			=> View(@"\Views\Panel\Cng\RepairsManHours\Edit.cshtml", new RepairsManHours { RepairsId = repairsId ?? 0 });

		[HttpGet("[action]")]
		[ActionDisplayName("لیست وابسته", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ListByParentId(long repairsId)
		{
			ViewBag.ParentId = repairsId;
			return View(@"\Views\Panel\Cng\RepairsManHours\ListByParentId.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت لیست وابسته", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetListByParentId(long? repairsId, CancellationToken cn)
		{
			if (repairsId == null || repairsId == 0)
				return BadRequest("شناسه والد خالی است");

			var items = await unitOfWork.Repository<RepairsManHours>().TableNoTracking
				.Where(c => c.RepairsId == repairsId)
				.Select(c => new
				{
					c.Id,
					c.PersonelId,
					PersonelName = c.Personel != null
						? ((c.Personel.Name ?? "") + " " + (c.Personel.Family ?? "")).Trim()
						: null,
					c.WorkDate,
					c.StartTime,
					c.EndTime,
					c.ManHourPrice,
					c.Comment
				})
				.ToListAsync(cn);
			return Ok(items);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<RepairsManHours>().FetchDataAsync(request, cn));

		private static void ClearNav(RepairsManHours model)
		{
			model.Repairs = null;
			model.Personel = null;
		}
	}
}
