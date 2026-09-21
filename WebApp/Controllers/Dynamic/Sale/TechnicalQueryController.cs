using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Sale;
using Entities.Base.DataTable;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Auth;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Sale/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("پرسش فنی", typeof(TechnicalQuery))]
	public class TechnicalQueryController(IUnitOfWork unitOfWork, IWebHostEnvironment env, IEntityRepository entityRepository, IUserService userService) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(TechnicalQuery model, CancellationToken cn)
		{
			if (model.Id == null || model.Id == 0) return await Add(model, cn);
			if (await unitOfWork.Repository<TechnicalQuery>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(TechnicalQuery model, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<TechnicalQuery>().SaveAsync(model, cn, true));

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(TechnicalQuery model, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<TechnicalQuery>().UpdateAsync(model, cn, true));

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<TechnicalQuery>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null) await unitOfWork.Repository<TechnicalQuery>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			var entity = (id != null && id != 0)
				? unitOfWork.Repository<TechnicalQuery>().TableNoTracking.FirstOrDefault(c => c.Id == id)
				: new TechnicalQuery();
			return View(@"\Views\Panel\Sale\TechnicalQuery\Edit.cshtml", entity ?? new TechnicalQuery());
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New() => View(@"\Views\Panel\Sale\TechnicalQuery\Edit.cshtml", new TechnicalQuery());

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Sale\TechnicalQuery\List.cshtml");

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<TechnicalQuery>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex) { return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message); }
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<TechnicalQuery>().FetchDataAsync(request, cn));

		[HttpGet("[action]")]
		[ActionDisplayName("بررسی و تایید پرسش فنی", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ApproveQueue()
			=> View(@"\Views\Panel\Sale\TechnicalQuery\ApproveQueue.cshtml");

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت کارتابل پرسش فنی", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchApproveQueue(DataTableRequest request, CancellationToken cn)
			=> Ok(await entityRepository.FetchDataProfile(request, cn));

		[HttpPost("[action]")]
		[ActionDisplayName("تایید پرسش فنی", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> Approve(long id, CancellationToken cn)
			=> await ChangeCartable(id, 1, "تایید شده توسط مدیر", cn);

		[HttpPost("[action]")]
		[ActionDisplayName("رد پرسش فنی", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> Reject(long id, CancellationToken cn)
			=> await ChangeCartable(id, 2, "رد شده توسط مدیر", cn);

		private async Task<IActionResult> ChangeCartable(long id, short statusId, string comment, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<TechnicalQuery>().TableNoTracking.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return BadRequest("پرسش فنی یافت نشد");
			entity.CartableStatusId = statusId;
			entity.InApproveQueue = false;
			try
			{
				await unitOfWork.Repository<TechnicalQuery>().UpdateAsync(entity, cn, true);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}

			await unitOfWork.Repository<TechnicalQueryComment>().SaveAsync(new TechnicalQueryComment
			{
				TechnicalQueryId = entity.Id,
				UserId = CurrentUserId,
				Comment = comment
			}, cn, true);

			if (entity.CreatedById != null)
			{
				var creator = await userService.GetById(entity.CreatedById.Value);
				if (creator?.Id != null)
				{
					await unitOfWork.Repository<Notification>().AddAsync(new Notification
					{
						Type = NotificationType.Email,
						Title = comment,
						Body = "<div style='direction:rtl;text-align:right'>" + comment
							+ "<br/>شماره پرسش فنی: " + entity.Id + "</div>",
						EntityId = entity.Id,
						OwnerId = creator.Id.Value,
						ViewPath = "/Panel/Sale/TechnicalQuery/Edit?id=" + entity.Id,
						IsRead = false,
						IsSend = false,
						ToEmails = string.IsNullOrWhiteSpace(creator.Email) ? null : new List<string> { creator.Email }
					}, cn);
				}
			}
			return Ok(entity);
		}
	}
}
