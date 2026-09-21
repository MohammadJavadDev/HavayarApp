using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Trn;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Trn/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("فرد کلیدی شرکت", typeof(KeyParticipant))]
	public class KeyParticipantController(IUnitOfWork unitOfWork) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(KeyParticipant keyParticipant, CancellationToken cn)
		{
			if (keyParticipant.Id == null || keyParticipant.Id == 0)
			{
				return await Add(keyParticipant, cn);
			}
			var exist = await unitOfWork.Repository<KeyParticipant>().TableNoTracking.AnyAsync(c => c.Id == keyParticipant.Id);
			if (exist)
			{
				return await Update(keyParticipant, cn);
			}
			return await Add(keyParticipant, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(KeyParticipant keyParticipant, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<KeyParticipant>().SaveAsync(keyParticipant, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(KeyParticipant keyParticipant, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<KeyParticipant>().UpdateAsync(keyParticipant, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<KeyParticipant>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<KeyParticipant>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id, long? companyId)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<KeyParticipant>().TableNoTracking
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Trn\KeyParticipant\Edit.cshtml", entity);
			}
			var newEntity = new KeyParticipant { CompanyId = companyId ?? 0 };
			return View(@"\Views\Panel\Trn\KeyParticipant\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New(long? companyId)
		{
			var newEntity = new KeyParticipant { CompanyId = companyId ?? 0 };
			return View(@"\Views\Panel\Trn\KeyParticipant\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست افراد کلیدی شرکت", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ListByCompanyId(long companyId)
		{
			if (companyId == 0)
				throw new Exception("شناسه شرکت نمیتواند خالی باشد.");

			var company = unitOfWork.Repository<Company>().TableNoTracking
				.FirstOrDefault(c => c.Id == companyId);

			var model = new KeyParticipantListByParentViewModel
			{
				CompanyId = companyId,
				CompanyTitle = company != null ? company.Title ?? string.Empty : string.Empty
			};

			return View(@"\Views\Panel\Trn\KeyParticipant\ListByCompanyId.cshtml", model);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت افراد کلیدی شرکت", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetListByParentId(long? companyId, CancellationToken cn)
		{
			if (companyId == null || companyId == 0)
				return BadRequest("شناسه شرکت نمیتواند خالی باشد.");

			var items = await unitOfWork.Repository<KeyParticipant>()
				.TableNoTracking
				.Where(c => c.CompanyId == companyId)
				.Select(c => new KeyParticipantListItemViewModel
				{
					Id = c.Id,
					CompanyId = c.CompanyId,
					FirstName = c.FirstName,
					LastName = c.LastName,
					Position = c.Position,
					Mobile = c.Mobile,
					Email = c.Email
				})
				.ToListAsync(cn);

			return Ok(items);
		}

		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		[HttpPost("[action]")]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
		{
			return Ok(await unitOfWork.Repository<KeyParticipant>().FetchDataAsync(request, cn));
		}
	}

	public class KeyParticipantListByParentViewModel
	{
		public long CompanyId { get; set; }
		public string CompanyTitle { get; set; } = string.Empty;
	}

	public class KeyParticipantListItemViewModel
	{
		public long? Id { get; set; }
		public long? CompanyId { get; set; }
		public string? FirstName { get; set; }
		public string? LastName { get; set; }
		public string? Position { get; set; }
		public string? Mobile { get; set; }
		public string? Email { get; set; }
	}
}
