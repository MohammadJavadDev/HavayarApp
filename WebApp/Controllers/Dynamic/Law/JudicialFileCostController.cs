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
	/// <summary>HTS: بخش «هزینه ها» داخل فرم پرونده حقوقی (فقط افزودن و حذف ردیف، بدون ویرایش).</summary>
	[Route("Panel/Law/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("هزینه پرونده حقوقی", typeof(JudicialFileCost))]
	public class JudicialFileCostController(IUnitOfWork unitOfWork) : BaseController
	{
		private bool SeeAll => IsAdministrator || CurrentUserHasRole(LawRoles.JudicialFileFull);

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(JudicialFileCost model, CancellationToken cn)
		{
			model.JudicialFile = null;

			if (model.JudicialFileId is not > 0)
				return BadRequest("ابتدا پرونده را ذخیره کنید");
			if (model.Id is > 0)
				return BadRequest("ویرایش ردیف مجاز نیست؛ ردیف را حذف و دوباره اضافه کنید");
			if (!await JudicialFileAccess.CanAccessAsync(unitOfWork, SeeAll, CurrentUserId, model.JudicialFileId, cn))
				return BadRequest(JudicialFileAccess.Denied);
			if (model.Type == null)
				return BadRequest("نوع هزینه را انتخاب کنید");
			if (model.Value == null)
				return BadRequest("مبلغ را وارد کنید");
			try
			{
				return Ok(await unitOfWork.Repository<JudicialFileCost>().SaveAsync(model, cn, true));
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
			var entity = await unitOfWork.Repository<JudicialFileCost>().TableNoTracking.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null)
				return Ok();
			if (!await JudicialFileAccess.CanAccessAsync(unitOfWork, SeeAll, CurrentUserId, entity.JudicialFileId, cn))
				return BadRequest(JudicialFileAccess.Denied);
			await unitOfWork.Repository<JudicialFileCost>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public async Task<IActionResult> New(long? judicialFileId = null, CancellationToken cn = default)
		{
			if (!await JudicialFileAccess.CanAccessAsync(unitOfWork, SeeAll, CurrentUserId, judicialFileId, cn))
				return BadRequest(JudicialFileAccess.Denied);
			return View(@"\Views\Panel\Law\JudicialFileCost\Edit.cshtml", new JudicialFileCost { JudicialFileId = judicialFileId });
		}

		// کمکی (بدون ActionDisplayName): صفحه/داده‌ی جدول وابسته؛ دسترسی با مالکیت پرونده کنترل می‌شود.
		[HttpGet("[action]")]
		public async Task<IActionResult> ListByParentId(long judicialFileId, CancellationToken cn)
		{
			if (!await JudicialFileAccess.CanAccessAsync(unitOfWork, SeeAll, CurrentUserId, judicialFileId, cn))
				return BadRequest(JudicialFileAccess.Denied);
			ViewBag.ParentId = judicialFileId;
			return View(@"\Views\Panel\Law\JudicialFileCost\ListByParentId.cshtml");
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> GetListByParentId(long judicialFileId, CancellationToken cn)
		{
			if (!await JudicialFileAccess.CanAccessAsync(unitOfWork, SeeAll, CurrentUserId, judicialFileId, cn))
				return BadRequest(JudicialFileAccess.Denied);
			var rows = await unitOfWork.Repository<JudicialFileCost>().TableNoTracking
				.Where(c => c.JudicialFileId == judicialFileId)
				.OrderBy(c => c.Id)
				.Select(c => new
				{
					c.Id,
					c.Type,
					c.Value,
					c.Comment,
					c.CreatedByName,
					c.CreatedOnShamsiDateTime,
				})
				.ToListAsync(cn);
			return Ok(rows.Select(r => new
			{
				id = r.Id,
				typeTitle = r.Type.HasValue ? r.Type.Value.ToDisplay() : null,
				value = r.Value,
				comment = r.Comment,
				createdByName = r.CreatedByName,
				createdOnShamsiDateTime = r.CreatedOnShamsiDateTime,
			}));
		}
	}
}
