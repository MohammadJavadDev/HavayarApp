using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Data.SystemAuth;
using Entities.App.Gnr;
using Entities.App.Inv;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Inv/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("تامین کنندگان محصولات", typeof(PartCompany))]
	public class PartCompanyController(IUnitOfWork unitOfWork, IPropertyIdentityService identityService, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(PartCompany partCompany, CancellationToken cn)
		{
			if (partCompany.Id == null || partCompany.Id == 0)
			{
				return await Add(partCompany, cn);
			}
			var exist = await unitOfWork.Repository<PartCompany>().TableNoTracking.AnyAsync(c => c.Id == partCompany.Id);
			if (exist)
			{
				return await Update(partCompany, cn);
			}
			return await Add(partCompany, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(PartCompany partCompany, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<PartCompany>().SaveAsync(partCompany, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(PartCompany partCompany, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<PartCompany>().UpdateAsync(partCompany, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<PartCompany>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<PartCompany>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<PartCompany>().TableNoTracking
					.Include(c => c.Part)
					.Include(c => c.Supplier)
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Inv\PartCompany\Edit.cshtml", entity);
			}
			var newEntity = new PartCompany();
			return View(@"\Views\Panel\Inv\PartCompany\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var newEntity = new PartCompany();
			return View(@"\Views\Panel\Inv\PartCompany\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Inv\PartCompany\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<PartCompany>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<PartCompany>().FetchDataAsync(request, cn));
		}


		[HttpPost("[action]")]
		[ActionDisplayName("افزودن تامین کنندگان", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> AddSuppliers(AddSuppliersViewModel model, CancellationToken cancellationToken)
		{
		 
			if (model.PartIds == null || model.PartIds.Length == 0)
				return BadRequest("حداقل یک کالا باید انتخاب شود.");


			var partIds = model.PartIds
			    .Select(id => id)
			    .Distinct()
			    .ToArray();

			if (model.SupplierIds == null || model.SupplierIds.Length == 0)
			{
				var repo = unitOfWork.Repository<PartCompany>();

				// حذف روابط موجود
				var relationsToDelete = repo.Table
				    .Where(pc => partIds.Contains(pc.PartId));

				await relationsToDelete.ExecuteDeleteAsync(cancellationToken);

				return Ok();
			}
				  
			var supplierIds = model.SupplierIds
			    .Select(id => id)
			    .Distinct()
			    .ToArray();

			if (!partIds.Any() || !supplierIds.Any())
				return BadRequest("شناسه‌های نامعتبر ارسال شده‌اند.");

			// 2. بررسی وجود موجودیت‌ها
			var existingParts = await unitOfWork.Repository<Part>()
			    .Table
			    .Where(p => partIds.Contains(p.Id.Value))
			    .Select(p => p.Id)
			    .ToArrayAsync(cancellationToken);

			if (existingParts.Length != partIds.Length)
				return BadRequest("برخی از کالاها یافت نشدند.");

			var existingSuppliers = await unitOfWork.Repository<Supplier>()
			    .Table
			    .Where(s => supplierIds.Contains(s.Id.Value))
			    .Select(s => s.Id)
			    .ToArrayAsync(cancellationToken);

			if (existingSuppliers.Length != supplierIds.Length)
				return BadRequest("برخی از تامین‌کنندگان یافت نشدند.");

 
			return await unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var repo = unitOfWork.Repository<PartCompany>();

 
				var relationsToDelete = repo.Table
				    .Where(pc => partIds.Contains(pc.PartId));

				await relationsToDelete.ExecuteDeleteAsync(cancellationToken);

 
				var newRelations = new List<PartCompany>();
				foreach (var partId in partIds)
				{
					foreach (var supplierId in supplierIds)
					{
						newRelations.Add(new PartCompany
						{
							PartId = partId,
							SupplierId = supplierId
						});
					}
				}

				await repo.AddRangeAsync(newRelations, cancellationToken);

			 
				await unitOfWork.SaveChangesAsync(cancellationToken);

				return Ok(new
				{
					message = "تامین‌کنندگان با موفقیت به‌روزرسانی شدند.",
					affectedParts = partIds.Length,
					addedSuppliersPerPart = supplierIds.Length,
					totalRelationsAdded = newRelations.Count
				});
			}, cancellationToken);
		}
		public class AddSuppliersViewModel
		{
			public long[] PartIds { get; set; } = Array.Empty<long>();
			public long[] SupplierIds { get; set; } = Array.Empty<long>();
		}
	}
}
