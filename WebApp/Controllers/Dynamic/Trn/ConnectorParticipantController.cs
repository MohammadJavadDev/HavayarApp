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
	[ControllerInfo("رابط شرکت", typeof(ConnectorParticipant))]
	public class ConnectorParticipantController(IUnitOfWork unitOfWork) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(ConnectorParticipant connectorParticipant, CancellationToken cn)
		{
			// Business rule (§04-Company.md): prevent duplicate (CompanyId, ParticipantId) pairs.
			var duplicate = await unitOfWork.Repository<ConnectorParticipant>().TableNoTracking
				.AnyAsync(c => c.CompanyId == connectorParticipant.CompanyId
					&& c.ParticipantId == connectorParticipant.ParticipantId
					&& c.Id != connectorParticipant.Id, cn);
			if (duplicate)
				return BadRequest("این رابط قبلاً برای این شرکت ثبت شده است.");

			if (connectorParticipant.Id == null || connectorParticipant.Id == 0)
			{
				return await Add(connectorParticipant, cn);
			}
			var exist = await unitOfWork.Repository<ConnectorParticipant>().TableNoTracking.AnyAsync(c => c.Id == connectorParticipant.Id);
			if (exist)
			{
				return await Update(connectorParticipant, cn);
			}
			return await Add(connectorParticipant, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(ConnectorParticipant connectorParticipant, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<ConnectorParticipant>().SaveAsync(connectorParticipant, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(ConnectorParticipant connectorParticipant, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<ConnectorParticipant>().UpdateAsync(connectorParticipant, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<ConnectorParticipant>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<ConnectorParticipant>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id, long? companyId)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<ConnectorParticipant>().TableNoTracking
					.Include(c => c.Participant)
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Trn\ConnectorParticipant\Edit.cshtml", entity);
			}
			var newEntity = new ConnectorParticipant { CompanyId = companyId ?? 0 };
			return View(@"\Views\Panel\Trn\ConnectorParticipant\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New(long? companyId)
		{
			var newEntity = new ConnectorParticipant { CompanyId = companyId ?? 0 };
			return View(@"\Views\Panel\Trn\ConnectorParticipant\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست رابطین شرکت", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ListByCompanyId(long companyId)
		{
			if (companyId == 0)
				throw new Exception("شناسه شرکت نمیتواند خالی باشد.");

			var company = unitOfWork.Repository<Company>().TableNoTracking
				.FirstOrDefault(c => c.Id == companyId);

			var model = new ConnectorParticipantListByParentViewModel
			{
				CompanyId = companyId,
				CompanyTitle = company != null ? company.Title ?? string.Empty : string.Empty
			};

			return View(@"\Views\Panel\Trn\ConnectorParticipant\ListByCompanyId.cshtml", model);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت رابطین شرکت", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetListByParentId(long? companyId, CancellationToken cn)
		{
			if (companyId == null || companyId == 0)
				return BadRequest("شناسه شرکت نمیتواند خالی باشد.");

			var items = await unitOfWork.Repository<ConnectorParticipant>()
				.TableNoTracking
				.Where(c => c.CompanyId == companyId)
				.Select(c => new ConnectorParticipantListItemViewModel
				{
					Id = c.Id,
					CompanyId = c.CompanyId,
					ParticipantId = c.ParticipantId,
					ParticipantName = c.Participant != null ? c.Participant.FirstName + " " + c.Participant.LastName : "-",
					Position = c.Position,
					Description = c.Description
				})
				.ToListAsync(cn);

			return Ok(items);
		}

		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		[HttpPost("[action]")]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
		{
			return Ok(await unitOfWork.Repository<ConnectorParticipant>().FetchDataAsync(request, cn));
		}
	}

	public class ConnectorParticipantListByParentViewModel
	{
		public long CompanyId { get; set; }
		public string CompanyTitle { get; set; } = string.Empty;
	}

	public class ConnectorParticipantListItemViewModel
	{
		public long? Id { get; set; }
		public long? CompanyId { get; set; }
		public long ParticipantId { get; set; }
		public string? ParticipantName { get; set; }
		public string? Position { get; set; }
		public string? Description { get; set; }
	}
}
