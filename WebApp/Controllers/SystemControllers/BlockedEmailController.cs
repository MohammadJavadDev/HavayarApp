using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Entities.Base;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.SystemControllers
{
	[Route("Panel/System/BlockedEmail")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("ایمیل‌های عدم ارسال", typeof(BlockedEmail))]
	public class BlockedEmailController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		private async Task<string?> ValidateAsync(BlockedEmail model, CancellationToken cn)
		{
			if (!TryNormalizeEmail(model.Email, out var email, out var error))
				return error;

			model.Email = email;
			model.PersonName = model.PersonName.HasValue() ? model.PersonName!.Trim() : null;
			model.Description = model.Description.HasValue() ? model.Description!.Trim() : null;

			var id = model.Id ?? 0;
			var normalized = email.ToLower();
			var duplicate = await unitOfWork.Repository<BlockedEmail>().TableNoTracking
				.AnyAsync(x => x.Id != id && x.Email.ToLower() == normalized, cn);
			if (duplicate)
				return "این ایمیل قبلاً ثبت شده است.";

			return null;
		}

		private static bool TryNormalizeEmail(string? raw, out string email, out string? error)
		{
			email = (raw ?? string.Empty).Trim();
			error = null;
			if (!email.HasValue())
			{
				error = "ایمیل نمی‌تواند خالی باشد.";
				return false;
			}

			try
			{
				var mail = new MailAddress(email);
				if (!mail.Address.HasValue() || !mail.Address.Contains('@'))
				{
					error = "فرمت ایمیل معتبر نیست.";
					return false;
				}

				email = mail.Address;
				return true;
			}
			catch
			{
				error = "فرمت ایمیل معتبر نیست.";
				return false;
			}
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(BlockedEmail model, CancellationToken cn)
		{
			var error = await ValidateAsync(model, cn);
			if (error != null)
				return BadRequest(error);

			model.Id = null;
			model.IsActive ??= IsActiveEnum.Active;
			return Ok(await unitOfWork.Repository<BlockedEmail>().AddAsync(model, cn, true));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(BlockedEmail model, CancellationToken cn)
		{
			var error = await ValidateAsync(model, cn);
			if (error != null)
				return BadRequest(error);

			if (model.Id is not > 0)
				return BadRequest("رکورد یافت نشد");

			var old = await unitOfWork.Repository<BlockedEmail>().TableNoTracking
				.Where(c => c.Id == model.Id)
				.Select(c => new { c.IsActive })
				.FirstOrDefaultAsync(cn);
			if (old == null)
				return NotFound();

			model.IsActive ??= old.IsActive ?? IsActiveEnum.Active;
			return Ok(await unitOfWork.Repository<BlockedEmail>().UpdateAsync(model, cn, true));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<BlockedEmail>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity != null)
				await unitOfWork.Repository<BlockedEmail>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			var entity = id is > 0
				? unitOfWork.Repository<BlockedEmail>().TableNoTracking.FirstOrDefault(c => c.Id == id)
				: new BlockedEmail { IsActive = IsActiveEnum.Active };
			return View(@"\Views\Panel\System\BlockedEmail\Edit.cshtml", entity ?? new BlockedEmail { IsActive = IsActiveEnum.Active });
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
			=> View(@"\Views\Panel\System\BlockedEmail\Edit.cshtml", new BlockedEmail { IsActive = IsActiveEnum.Active });

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\System\BlockedEmail\List.cshtml");

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			await unitOfWork.Repository<BlockedEmail>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
			memoryStream.Position = 0;
			return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<BlockedEmail>().FetchDataAsync(request, cn));
	}
}
