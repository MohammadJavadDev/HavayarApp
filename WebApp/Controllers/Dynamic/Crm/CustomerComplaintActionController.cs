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
	[Route("Panel/Crm/CustomerComplaintAction")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("اقدام شکایت", typeof(CustomerComplaintAction))]
	public class CustomerComplaintActionController(IUnitOfWork unitOfWork) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(CustomerComplaintAction model, CancellationToken cn)
		{
			model.Complaint = null;
			if (model.ComplaintId is not > 0)
				return BadRequest("ابتدا شکایت را ذخیره کنید");
			if (string.IsNullOrWhiteSpace(model.ActionShamsiDate))
				return BadRequest("تاریخ اقدام را وارد کنید");
			if (string.IsNullOrWhiteSpace(model.ActionDescription))
				return BadRequest("شرح اقدام را وارد کنید");
			if (model.Id == null || model.Id == 0)
				return Ok(await unitOfWork.Repository<CustomerComplaintAction>().SaveAsync(model, cn, true));
			var stored = await unitOfWork.Repository<CustomerComplaintAction>().TableNoTracking.FirstOrDefaultAsync(c => c.Id == model.Id, cn);
			if (stored != null)
				model.HtsId = stored.HtsId;
			return Ok(await unitOfWork.Repository<CustomerComplaintAction>().UpdateAsync(model, cn, true));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<CustomerComplaintAction>().Table.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity != null)
				await unitOfWork.Repository<CustomerComplaintAction>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id, long? complaintId)
		{
			var entity = (id != null && id != 0)
				? unitOfWork.Repository<CustomerComplaintAction>().TableNoTracking.FirstOrDefault(c => c.Id == id)
				: new CustomerComplaintAction { ComplaintId = complaintId };
			return View(@"\Views\Panel\Crm\CustomerComplaintAction\Edit.cshtml", entity ?? new CustomerComplaintAction { ComplaintId = complaintId });
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New(long? complaintId)
			=> View(@"\Views\Panel\Crm\CustomerComplaintAction\Edit.cshtml", new CustomerComplaintAction { ComplaintId = complaintId });

		[HttpGet("[action]")]
		[ActionDisplayName("لیست وابسته", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetListByParentId(long complaintId, CancellationToken cn)
		{
			var rows = await unitOfWork.Repository<CustomerComplaintAction>().TableNoTracking
				.Where(c => c.ComplaintId == complaintId)
				.OrderByDescending(c => c.ActionMiladiDate)
				.Select(c => new
				{
					id = c.Id,
					actionShamsiDate = c.ActionShamsiDate,
					actionMiladiDate = c.ActionMiladiDate,
					actionDescription = c.ActionDescription,
					nextActionShamsiDate = c.NextActionShamsiDate,
					nextActionMiladiDate = c.NextActionMiladiDate,
					nextActionDescription = c.NextActionDescription,
					comment = c.Comment
				})
				.ToListAsync(cn);
			return Ok(rows);
		}
	}
}
