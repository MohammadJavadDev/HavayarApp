using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Entities.App.Law;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	/// <summary>HTS: بخش «تاریخ های مهم» داخل فرم پرونده حقوقی (فقط افزودن و حذف ردیف، بدون ویرایش).</summary>
	[Route("Panel/Law/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("تاریخ مهم پرونده حقوقی", typeof(JudicialImportantDate))]
	public class JudicialImportantDateController(IUnitOfWork unitOfWork) : BaseController
	{
		private bool SeeAll => IsAdministrator || CurrentUserHasRole(LawRoles.JudicialFileFull);

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(JudicialImportantDate model, CancellationToken cn)
		{
			model.JudicialFile = null;

			if (model.JudicialFileId is not > 0)
				return BadRequest("ابتدا پرونده را ذخیره کنید");
			if (model.Id is > 0)
				return BadRequest("ویرایش ردیف مجاز نیست؛ ردیف را حذف و دوباره اضافه کنید");
			if (!await JudicialFileAccess.CanAccessAsync(unitOfWork, SeeAll, CurrentUserId, model.JudicialFileId, cn))
				return BadRequest(JudicialFileAccess.Denied);
			if (string.IsNullOrWhiteSpace(model.Title))
				return BadRequest("عنوان را وارد کنید");
			if (string.IsNullOrWhiteSpace(model.DateShamsiDate))
				return BadRequest("تاریخ را وارد کنید");
			try
			{
				return Ok(await unitOfWork.Repository<JudicialImportantDate>().SaveAsync(model, cn, true));
			}
			catch (Exception ex)
			{
				return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<JudicialImportantDate>().TableNoTracking.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null)
				return Ok();
			if (!await JudicialFileAccess.CanAccessAsync(unitOfWork, SeeAll, CurrentUserId, entity.JudicialFileId, cn))
				return BadRequest(JudicialFileAccess.Denied);
			await unitOfWork.Repository<JudicialImportantDate>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public async Task<IActionResult> New(long? judicialFileId = null, CancellationToken cn = default)
		{
			if (!await JudicialFileAccess.CanAccessAsync(unitOfWork, SeeAll, CurrentUserId, judicialFileId, cn))
				return BadRequest(JudicialFileAccess.Denied);
			return View(@"\Views\Panel\Law\JudicialImportantDate\Edit.cshtml", new JudicialImportantDate { JudicialFileId = judicialFileId });
		}

		// کمکی (بدون ActionDisplayName): صفحه/داده‌ی جدول وابسته؛ دسترسی با مالکیت پرونده کنترل می‌شود.
		[HttpGet("[action]")]
		public async Task<IActionResult> ListByParentId(long judicialFileId, CancellationToken cn)
		{
			if (!await JudicialFileAccess.CanAccessAsync(unitOfWork, SeeAll, CurrentUserId, judicialFileId, cn))
				return BadRequest(JudicialFileAccess.Denied);
			ViewBag.ParentId = judicialFileId;
			return View(@"\Views\Panel\Law\JudicialImportantDate\ListByParentId.cshtml");
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> GetListByParentId(long judicialFileId, CancellationToken cn)
		{
			if (!await JudicialFileAccess.CanAccessAsync(unitOfWork, SeeAll, CurrentUserId, judicialFileId, cn))
				return BadRequest(JudicialFileAccess.Denied);
			var rows = await unitOfWork.Repository<JudicialImportantDate>().TableNoTracking
				.Where(c => c.JudicialFileId == judicialFileId)
				.OrderBy(c => c.Id)
				.Select(c => new
				{
					c.Id,
					c.Title,
					c.Recipients,
					c.DateShamsiDate,
					c.CreatedByName,
					c.CreatedOnShamsiDateTime,
				})
				.ToListAsync(cn);
			return Ok(rows.Select(r => new
			{
				id = r.Id,
				title = r.Title,
				recipients = r.Recipients,
				dateShamsiDate = r.DateShamsiDate,
				createdByName = r.CreatedByName,
				createdOnShamsiDateTime = r.CreatedOnShamsiDateTime,
			}));
		}
	}
}
