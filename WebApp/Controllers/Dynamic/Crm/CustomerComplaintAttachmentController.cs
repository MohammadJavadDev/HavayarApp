using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Crm;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Crm/CustomerComplaintAttachment")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("پیوست شکایت", typeof(CustomerComplaintAttachment))]
	public class CustomerComplaintAttachmentController(IUnitOfWork unitOfWork) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(CustomerComplaintAttachment model, CancellationToken cn)
		{
			model.Complaint = null;
			model.File = null;
			if (model.ComplaintId is not > 0)
				return BadRequest("ابتدا شکایت را ذخیره کنید");
			if (model.FileId is not > 0)
				return BadRequest("فایل را انتخاب کنید");
			if (model.Id == null || model.Id == 0)
				return Ok(await unitOfWork.Repository<CustomerComplaintAttachment>().SaveAsync(model, cn, true));
			var stored = await unitOfWork.Repository<CustomerComplaintAttachment>().TableNoTracking.FirstOrDefaultAsync(c => c.Id == model.Id, cn);
			if (stored != null)
			{
				model.HtsId = stored.HtsId;
				if (model.FileId is not > 0)
					model.FileId = stored.FileId;
			}
			return Ok(await unitOfWork.Repository<CustomerComplaintAttachment>().UpdateAsync(model, cn, true));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<CustomerComplaintAttachment>().Table.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity != null)
				await unitOfWork.Repository<CustomerComplaintAttachment>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id, long? complaintId)
		{
			var entity = (id != null && id != 0)
				? unitOfWork.Repository<CustomerComplaintAttachment>().TableNoTracking.Include(c => c.File).FirstOrDefault(c => c.Id == id)
				: new CustomerComplaintAttachment { ComplaintId = complaintId };
			return View(@"\Views\Panel\Crm\CustomerComplaintAttachment\Edit.cshtml", entity ?? new CustomerComplaintAttachment { ComplaintId = complaintId });
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New(long? complaintId)
			=> View(@"\Views\Panel\Crm\CustomerComplaintAttachment\Edit.cshtml", new CustomerComplaintAttachment { ComplaintId = complaintId });

		[HttpGet("[action]")]
		[ActionDisplayName("لیست پیوست", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ListByParentId(long complaintId)
		{
			ViewBag.ParentId = complaintId;
			return View(@"\Views\Panel\Crm\CustomerComplaintAttachment\ListByParentId.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست وابسته", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetListByParentId(long complaintId, CancellationToken cn)
		{
			var rows = await unitOfWork.Repository<CustomerComplaintAttachment>().TableNoTracking
				.Where(c => c.ComplaintId == complaintId)
				.OrderByDescending(c => c.Id)
				.Select(c => new
				{
					id = c.Id,
					fileId = c.FileId,
					fileName = c.File != null ? c.File.OriginalName : null,
					comment = c.Comment,
					createdOnShamsiDateTime = c.CreatedOnShamsiDateTime
				})
				.ToListAsync(cn);
			return Ok(rows);
		}
	}
}
