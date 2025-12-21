using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Data.SystemAuth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Entities.Base.DataTable;
using WebFramework.Filtters;
using WebFramework.Page;
using Entities.App.Epms;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Epms/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("پروپوزال", typeof(Proposal))]
	public class ProposalController(IUnitOfWork unitOfWork, IPropertyIdentityService identityService, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(Proposal proposal, CancellationToken cn)
		{
			// Save logic here
			if (proposal.Id == null || proposal.Id == 0)
			{
				return await Add(proposal, cn);
			}
			var exist = await unitOfWork.Repository<Proposal>().TableNoTracking.AnyAsync(c => c.Id == proposal.Id);
			if (exist)
			{
				return await Update(proposal, cn);
			}
			return await Add(proposal, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(Proposal proposal, CancellationToken cn)
		{
			// Add logic here
			var entity = await unitOfWork.Repository<Proposal>().SaveAsync(proposal, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(Proposal proposal, CancellationToken cn)
		{
			// Update logic here
			var entity = await unitOfWork.Repository<Proposal>().UpdateAsync(proposal, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			// Delete logic here
			var model = unitOfWork.Repository<Proposal>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<Proposal>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			// Get logic here
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<Proposal>().TableNoTracking
					.Include(c=>c.ProposalAttachment)
				 
					.Include(c=>c.ProposalDocuments)
				 
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Epms\Proposal\Edit.cshtml", entity);
			}
			var newEntity = new Proposal();

			return View(@"\Views\Panel\Epms\Proposal\Edit.cshtml", newEntity);
		}
		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var newEntity = new Proposal();

			return View(@"\Views\Panel\Epms\Proposal\Edit.cshtml", newEntity);
		}
		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Epms\Proposal\List.cshtml");
		}
		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<Proposal>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);

				memoryStream.Position = 0;

				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"exportExcel.xlsx");
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
			return Ok(await unitOfWork.Repository<Proposal>().FetchDataAsync(request, cn));
		}
		[HttpGet("[action]")]
		public IActionResult ProposalEquipmentPartial()
		{
			return PartialView(@"\Views\Panel\Epms\Proposal\_ProposalEquipmentPartial.cshtml");
		}
		[HttpGet("[action]")]
		public IActionResult ProposalAttachmentPartial()
		{
			return PartialView(@"\Views\Panel\Epms\Proposal\_ProposalAttachmentPartial.cshtml");
		}
		[HttpGet("[action]")]
		public IActionResult ProposalDocumentPartial()
		{
			return PartialView(@"\Views\Panel\Epms\Proposal\_ProposalDocumentPartial.cshtml");
		}

	}
}
