using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Law;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	/// <summary>HTS: LawSystem/JudicialFileManagementController (پرونده حقوقی + گزارش کلی پرونده ها).</summary>
	[Route("Panel/Law/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("پرونده حقوقی", typeof(JudicialFile))]
	public class JudicialFileController(
		IUnitOfWork unitOfWork,
		IWebHostEnvironment env,
		IEntityRepository entityRepository) : BaseController
	{
		private bool SeeAll => IsAdministrator || CurrentUserHasRole(LawRoles.JudicialFileFull);

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(JudicialFile model, CancellationToken cn)
		{
			ClearNav(model);
			if (model.Id == null || model.Id == 0)
				return await Add(model, cn);
			if (await unitOfWork.Repository<JudicialFile>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(JudicialFile model, CancellationToken cn)
		{
			ClearNav(model);
			if (model.Nature == null)
				return BadRequest("ماهیت پرونده را انتخاب کنید");
			if (model.Type == null)
				return BadRequest("نوع پرونده را انتخاب کنید");
			try
			{
				return Ok(await unitOfWork.Repository<JudicialFile>().SaveAsync(model, cn, true));
			}
			catch (Exception ex)
			{
				return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(JudicialFile model, CancellationToken cn)
		{
			ClearNav(model);
			if (!await JudicialFileAccess.CanAccessAsync(unitOfWork, SeeAll, CurrentUserId, model.Id, cn))
				return BadRequest(JudicialFileAccess.Denied);
			if (model.Nature == null)
				return BadRequest("ماهیت پرونده را انتخاب کنید");
			if (model.Type == null)
				return BadRequest("نوع پرونده را انتخاب کنید");
			try
			{
				return Ok(await unitOfWork.Repository<JudicialFile>().UpdateAsync(model, cn, true));
			}
			catch (Exception ex)
			{
				return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
		}

		/// <summary>HTS: کلیدهای خارجی NO_ACTION؛ پرونده‌ای که ردیف وابسته دارد حذف نمی‌شود.</summary>
		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<JudicialFile>().TableNoTracking.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null)
				return Ok();
			if (!await JudicialFileAccess.CanAccessAsync(unitOfWork, SeeAll, CurrentUserId, id, cn))
				return BadRequest(JudicialFileAccess.Denied);
			if (await HasChildrenAsync(id, cn))
				return BadRequest("این پرونده دارای اطلاعات وابسته (خواسته، طرفین، نتیجه، هزینه، پیوست و ...) است و قابل حذف نیست");
			try
			{
				await unitOfWork.Repository<JudicialFile>().DeleteAsync(entity, cn, true);
				return Ok();
			}
			catch (Exception ex)
			{
				return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public async Task<IActionResult> Edit(long? id, CancellationToken cn)
		{
			if (id is > 0)
			{
				if (!await JudicialFileAccess.CanAccessAsync(unitOfWork, SeeAll, CurrentUserId, id, cn))
					return BadRequest(JudicialFileAccess.Denied);
				var entity = await unitOfWork.Repository<JudicialFile>().TableNoTracking.FirstOrDefaultAsync(c => c.Id == id, cn);
				return View(@"\Views\Panel\Law\JudicialFile\Edit.cshtml", entity ?? new JudicialFile());
			}
			return View(@"\Views\Panel\Law\JudicialFile\Edit.cshtml", new JudicialFile());
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
			=> View(@"\Views\Panel\Law\JudicialFile\Edit.cshtml", new JudicialFile());

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Law\JudicialFile\List.cshtml");

		/// <summary>
		/// لیست و گزارش هر دو از همین صفحه و نمایه‌های داده می‌آیند. محدودیت «فقط پرونده‌های خودم»
		/// داخل SQL خود نمایه است؛ اینجا فقط درخواست بدون نمایه برای غیرادمین بسته می‌شود.
		/// </summary>
		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
		{
			if (!IsAdministrator && request.profileId is not > 0)
				return BadRequest("نمایه داده مشخص نشده است");
			return Ok(await entityRepository.FetchDataProfile(request, cn));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			if (!IsAdministrator && request.profileId is not > 0)
				return BadRequest("نمایه داده مشخص نشده است");
			var licensePath = env.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await entityRepository.ExportToExcelProfile(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex) { return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message); }
		}

		private static void ClearNav(JudicialFile model)
		{
			model.ReferralUnit = null;
			model.LegalExpert = null;
		}

		private async Task<bool> HasChildrenAsync(long id, CancellationToken cn)
		{
			return await unitOfWork.Repository<JudicialFileDemand>().TableNoTracking.AnyAsync(c => c.JudicialFileId == id, cn)
				|| await unitOfWork.Repository<JudicialFileParty>().TableNoTracking.AnyAsync(c => c.JudicialFileId == id, cn)
				|| await unitOfWork.Repository<JudicialFileResult>().TableNoTracking.AnyAsync(c => c.JudicialFileId == id, cn)
				|| await unitOfWork.Repository<JudicialFileCost>().TableNoTracking.AnyAsync(c => c.JudicialFileId == id, cn)
				|| await unitOfWork.Repository<JudicialFileRelatedWork>().TableNoTracking.AnyAsync(c => c.JudicialFileId == id, cn)
				|| await unitOfWork.Repository<JudicialReviewAuthority>().TableNoTracking.AnyAsync(c => c.JudicialFileId == id, cn)
				|| await unitOfWork.Repository<JudicialImportantDate>().TableNoTracking.AnyAsync(c => c.JudicialFileId == id, cn)
				|| await unitOfWork.Repository<JudicialFileAttachment>().TableNoTracking.AnyAsync(c => c.JudicialFileId == id, cn);
		}
	}
}
