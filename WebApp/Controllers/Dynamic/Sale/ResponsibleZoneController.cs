using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Sale;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Sale/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("مسئولین مناطق", typeof(ResponsibleZone))]
	public class ResponsibleZoneController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		public const long AfterSalesOrgUnitId = 119;

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(ResponsibleZone model, CancellationToken cn)
		{
			if (model.Id == null || model.Id == 0) return await Add(model, cn);
			if (await unitOfWork.Repository<ResponsibleZone>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(ResponsibleZone model, CancellationToken cn)
		{
			var customerIds = NormalizeCustomerIds(model);
			var invalid = ValidateHeader(model, customerIds);
			if (invalid != null) return invalid;

			DetachChildren(model);
			var saved = await unitOfWork.Repository<ResponsibleZone>().SaveAsync(model, cn, true);
			await InsertCustomersAsync(saved.Id!.Value, customerIds, cn);
			saved.CustomerIds = customerIds;
			return Ok(saved);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(ResponsibleZone model, CancellationToken cn)
		{
			var customerIds = NormalizeCustomerIds(model);
			var invalid = ValidateHeader(model, customerIds);
			if (invalid != null) return invalid;

			var original = await unitOfWork.Repository<ResponsibleZone>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == model.Id, cn);
			if (original == null)
				return BadRequest("ردیف یافت نشد");

			DetachChildren(model);

			// HTS DoOperation Update: wipe posted-row customers first.
			await unitOfWork.Repository<ResponsibleZoneCustomer>()
				.DeleteWhereAsync(c => c.ResponsibleZoneId == model.Id, cn);

			// HTS: only when ResponsibleId changes, merge into existing Responsible+Province+Zone.
			if (model.ResponsibleId != original.ResponsibleId)
			{
				var existing = await unitOfWork.Repository<ResponsibleZone>().TableNoTracking
					.FirstOrDefaultAsync(p =>
						p.Id != model.Id
						&& p.ResponsibleId == model.ResponsibleId
						&& p.ProvinceId == model.ProvinceId
						&& p.ZoneId == model.ZoneId, cn);

				if (existing != null)
				{
					await unitOfWork.Repository<ResponsibleZone>().DeleteAsync(original, cn, true);
					await InsertCustomersAsync(existing.Id!.Value, customerIds, cn);
					existing.CustomerIds = customerIds;
					return Ok(existing);
				}
			}

			var saved = await unitOfWork.Repository<ResponsibleZone>().UpdateAsync(model, cn, true);
			await InsertCustomersAsync(saved.Id!.Value, customerIds, cn);
			saved.CustomerIds = customerIds;
			return Ok(saved);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<ResponsibleZone>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null) await unitOfWork.Repository<ResponsibleZone>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			var entity = (id != null && id != 0)
				? unitOfWork.Repository<ResponsibleZone>().TableNoTracking.FirstOrDefault(c => c.Id == id)
				: new ResponsibleZone();
			return View(@"\Views\Panel\Sale\ResponsibleZone\Edit.cshtml", entity ?? new ResponsibleZone());
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New() => View(@"\Views\Panel\Sale\ResponsibleZone\Edit.cshtml", new ResponsibleZone());

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Sale\ResponsibleZone\List.cshtml");

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<ResponsibleZone>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex) { return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message); }
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<ResponsibleZone>().FetchDataAsync(request, cn));

		private static List<long> NormalizeCustomerIds(ResponsibleZone model)
			=> (model.CustomerIds ?? [])
				.Where(id => id > 0)
				.Distinct()
				.ToList();

		private static IActionResult? ValidateHeader(ResponsibleZone model, List<long> customerIds)
		{
			if (model.ResponsibleId is null or 0)
				return new BadRequestObjectResult("مسئول الزامی است");
			if (model.ProvinceId is null or 0)
				return new BadRequestObjectResult("استان الزامی است");
			if (customerIds.Count == 0)
				return new BadRequestObjectResult("حداقل یک مشتری الزامی است");
			return null;
		}

		private static void DetachChildren(ResponsibleZone model)
		{
			model.Customers = new List<ResponsibleZoneCustomer>();
		}

		private async Task InsertCustomersAsync(long zoneId, List<long> customerIds, CancellationToken cn)
		{
			if (customerIds.Count == 0)
				return;

			var rows = customerIds.Select(id => new ResponsibleZoneCustomer
			{
				ResponsibleZoneId = zoneId,
				CustomerId = id
			}).ToList();

			await unitOfWork.Repository<ResponsibleZoneCustomer>().AddRangeAsync(rows, cn);
		}
	}
}
