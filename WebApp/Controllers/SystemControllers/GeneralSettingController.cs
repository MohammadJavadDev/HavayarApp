using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.Base;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Settings;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.SystemControllers
{
	/// <summary>
	/// ویرایش تنظیمات عمومی (کلید/مقدار). مقدار تنظیمات محرمانه (IsSecret) هرگز به مرورگر برگردانده نمی‌شود؛
	/// در ویرایش، خالی گذاشتن مقدار یعنی «بدون تغییر».
	/// </summary>
	[Route("Panel/System/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("تنظیمات عمومی", typeof(GeneralSetting))]
	public class GeneralSettingController(
		IUnitOfWork unitOfWork,
		IGeneralSettingService settingService,
		IWebHostEnvironment env) : BaseController
	{
		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\System\GeneralSetting\List.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public async Task<IActionResult> Edit(long? id, CancellationToken cn)
		{
			if (id is null or 0)
				return View(@"\Views\Panel\System\GeneralSetting\Edit.cshtml", new GeneralSetting());

			var entity = await unitOfWork.Repository<GeneralSetting>().TableNoTracking
				.FirstOrDefaultAsync(x => x.Id == id, cn);
			if (entity == null)
				return NotFound();

			// مقدار محرمانه به view داده نمی‌شود.
			if (entity.IsSecret)
				entity.SettingValue = null;

			return View(@"\Views\Panel\System\GeneralSetting\Edit.cshtml", entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			return View(@"\Views\Panel\System\GeneralSetting\Edit.cshtml", new GeneralSetting());
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(GeneralSetting model, CancellationToken cn)
		{
			var key = model.SettingKey?.Trim();
			if (string.IsNullOrWhiteSpace(key))
				return BadRequest("کلید تنظیم الزامی است.");

			var repo = unitOfWork.Repository<GeneralSetting>();

			var duplicate = await repo.TableNoTracking.AnyAsync(x => x.SettingKey == key && x.Id != model.Id, cn);
			if (duplicate)
				return BadRequest($"کلید «{key}» قبلاً ثبت شده است.");

			GeneralSetting saved;
			if (model.Id is null or 0)
			{
				model.Id = null;
				model.SettingKey = key;
				saved = await repo.AddAsync(model, cn, saveAudit: true);
			}
			else
			{
				var existing = await repo.Table.FirstOrDefaultAsync(x => x.Id == model.Id, cn);
				if (existing == null)
					return NotFound("تنظیم یافت نشد.");

				existing.Title = model.Title;
				existing.Description = model.Description;
				existing.IsSecret = model.IsSecret;

				// محرمانه: مقدار خالی یعنی تغییری داده نشود.
				if (!(existing.IsSecret && string.IsNullOrEmpty(model.SettingValue)))
					existing.SettingValue = model.SettingValue;

				saved = await repo.UpdateAsync(existing, cn, saveAudit: true);
			}

			settingService.InvalidateCache();

			return Ok(new
			{
				id = saved.Id,
				settingKey = saved.SettingKey,
				settingValue = saved.IsSecret ? null : saved.SettingValue,
				title = saved.Title,
				description = saved.Description,
				isSecret = saved.IsSecret,
				createdByName = saved.CreatedByName,
				createdOnShamsiDateTime = saved.CreatedOnShamsiDateTime,
				modifiedByName = saved.ModifiedByName,
				modifiedDateShamsiDateTime = saved.ModifiedDateShamsiDateTime
			});
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<GeneralSetting>().TableNoTracking.FirstOrDefaultAsync(x => x.Id == id, cn);
			if (entity != null)
			{
				await unitOfWork.Repository<GeneralSetting>().DeleteAsync(entity, cn, true);
				settingService.InvalidateCache();
			}
			return Ok();
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<GeneralSetting>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
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
			return Ok(await unitOfWork.Repository<GeneralSetting>().FetchDataAsync(request, cn));
		}
	}
}
