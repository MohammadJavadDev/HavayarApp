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
	[Route("Panel/Crm/CustomerComplaintContact")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("راه ارتباط شکایت", typeof(CustomerComplaintContact))]
	public class CustomerComplaintContactController(IUnitOfWork unitOfWork) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(CustomerComplaintContact model, CancellationToken cn)
		{
			model.Complaint = null;
			if (model.ComplaintId is not > 0)
				return BadRequest("ابتدا شکایت را ذخیره کنید");
			if (model.Id == null || model.Id == 0)
				return Ok(await unitOfWork.Repository<CustomerComplaintContact>().SaveAsync(model, cn, true));
			var stored = await unitOfWork.Repository<CustomerComplaintContact>().TableNoTracking.FirstOrDefaultAsync(c => c.Id == model.Id, cn);
			if (stored != null)
				model.HtsId = stored.HtsId;
			return Ok(await unitOfWork.Repository<CustomerComplaintContact>().UpdateAsync(model, cn, true));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<CustomerComplaintContact>().Table.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity != null)
				await unitOfWork.Repository<CustomerComplaintContact>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id, long? complaintId)
		{
			var entity = (id != null && id != 0)
				? unitOfWork.Repository<CustomerComplaintContact>().TableNoTracking.FirstOrDefault(c => c.Id == id)
				: new CustomerComplaintContact { ComplaintId = complaintId };
			return View(@"\Views\Panel\Crm\CustomerComplaintContact\Edit.cshtml", entity ?? new CustomerComplaintContact { ComplaintId = complaintId });
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New(long? complaintId)
			=> View(@"\Views\Panel\Crm\CustomerComplaintContact\Edit.cshtml", new CustomerComplaintContact { ComplaintId = complaintId });

		[HttpGet("[action]")]
		[ActionDisplayName("لیست وابسته", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetListByParentId(long complaintId, CancellationToken cn)
		{
			var rows = await unitOfWork.Repository<CustomerComplaintContact>().TableNoTracking
				.Where(c => c.ComplaintId == complaintId)
				.OrderBy(c => c.Id)
				.Select(c => new
				{
					id = c.Id,
					prePhoneNumber = c.PrePhoneNumber,
					phoneNumber = c.PhoneNumber,
					mobileNumber = c.MobileNumber,
					responserFullName = c.ResponserFullName,
					responserJob = c.ResponserJob,
					comment = c.Comment
				})
				.ToListAsync(cn);
			return Ok(rows);
		}
	}
}
