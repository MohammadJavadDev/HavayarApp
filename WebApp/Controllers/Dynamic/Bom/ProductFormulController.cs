using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Data.SystemAuth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Entities.Base.DataTable;
using WebFramework.Filtters;
using WebFramework.Page;
using Entities.App.Bom;
using Entities.App.Inv;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Bom/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("Bom محصول", typeof(ProductFormul))]
	public class ProductFormulController(IUnitOfWork unitOfWork, IPropertyIdentityService identityService, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(ProductFormul productFormul, CancellationToken cn)
		{
			productFormul.Items = new List<ProductFormulItem>();
			if (productFormul.Id == null || productFormul.Id == 0)
			{
				return await Add(productFormul, cn);
			}
			var exist = await unitOfWork.Repository<ProductFormul>().TableNoTracking.AnyAsync(c => c.Id == productFormul.Id);
			if (exist)
			{
				return await Update(productFormul, cn);
			}
			return await Add(productFormul, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(ProductFormul productFormul, CancellationToken cn)
		{
			productFormul.Items = new List<ProductFormulItem>();
			var entity = await unitOfWork.Repository<ProductFormul>().SaveAsync(productFormul, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(ProductFormul productFormul, CancellationToken cn)
		{
			productFormul.Items = new List<ProductFormulItem>();
			var entity = await unitOfWork.Repository<ProductFormul>().UpdateAsync(productFormul, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<ProductFormul>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<ProductFormul>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<ProductFormul>().TableNoTracking
					.Include(c => c.Product)
					.Include(c => c.ProductNameGroup)
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Bom\ProductFormul\Edit.cshtml", entity);
			}
			var newEntity = new ProductFormul();
			return View(@"\Views\Panel\Bom\ProductFormul\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var newEntity = new ProductFormul();
			return View(@"\Views\Panel\Bom\ProductFormul\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Bom\ProductFormul\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<ProductFormul>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<ProductFormul>().FetchDataAsync(request, cn));
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> GetItems(long? productFormulId, CancellationToken cn)
		{
			if (productFormulId == null || productFormulId == 0)
				return Ok(Array.Empty<object>());

			var items = await unitOfWork.Repository<ProductFormulItem>().TableNoTracking
				.Where(i => i.ProductFormulId == productFormulId)
				.OrderBy(i => i.Part.Code)
				.Select(i => new
				{
					i.Id,
					i.PartId,
					PartCode = i.Part.Code,
					PartName = i.Part.Name,
					i.UsingRate
				})
				.ToListAsync(cn);

			return Ok(items);
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> ProductFormulItemPartial(long? id, long? productFormulId, CancellationToken cn)
		{
			ProductFormulItem model;
			if (id != null && id != 0)
			{
				model = await unitOfWork.Repository<ProductFormulItem>().TableNoTracking
					.FirstOrDefaultAsync(i => i.Id == id, cn)
					?? new ProductFormulItem { ProductFormulId = productFormulId ?? 0 };
			}
			else
			{
				model = new ProductFormulItem { ProductFormulId = productFormulId ?? 0 };
			}

			return View(@"\Views\Panel\Bom\ProductFormul\_ProductFormulItemPartial.cshtml", model);
		}

		[HttpPost("[action]")]
		public async Task<IActionResult> SaveItem(ProductFormulItem item, CancellationToken cn)
		{
			if (item.ProductFormulId == 0)
				return BadRequest("ابتدا فرمول را ذخیره کنید");
			if (item.PartId == 0)
				return BadRequest("قطعه را انتخاب کنید");
			if (item.UsingRate == null)
				return BadRequest("ضریب مصرف را وارد کنید");
			if (item.UsingRate < 0)
				return BadRequest("ضریب مصرف نمی‌تواند منفی باشد");

			item.UsingRate = Math.Round(item.UsingRate.Value, 2, MidpointRounding.AwayFromZero);
			item.Part = null!;
			item.ProductFormul = null!;

			var formulExists = await unitOfWork.Repository<ProductFormul>().TableNoTracking
				.AnyAsync(f => f.Id == item.ProductFormulId, cn);
			if (!formulExists)
				return BadRequest("فرمول محصول پیدا نشد");

			var partExists = await unitOfWork.Repository<Part>().TableNoTracking
				.AnyAsync(p => p.Id == item.PartId, cn);
			if (!partExists)
				return BadRequest("قطعه پیدا نشد");

			var itemRepo = unitOfWork.Repository<ProductFormulItem>();
			if (item.Id != null && item.Id != 0)
			{
				var existing = await itemRepo.TableNoTracking.FirstOrDefaultAsync(x => x.Id == item.Id, cn);
				if (existing == null)
					return NotFound("قلم پیدا نشد");
				if (existing.ProductFormulId != item.ProductFormulId)
					return BadRequest("این قلم متعلق به این فرمول نیست");
			}

			var duplicate = await itemRepo.TableNoTracking.AnyAsync(x =>
				x.ProductFormulId == item.ProductFormulId
				&& x.PartId == item.PartId
				&& x.Id != item.Id, cn);
			if (duplicate)
				return BadRequest("این قطعه قبلاً در اقلام ثبت شده است");

			if (item.Id == null || item.Id == 0)
			{
				item.Id = null;
				await itemRepo.AddAsync(item, cn, true);
			}
			else
				await itemRepo.UpdateAsync(item, cn, true);

			return Ok(new
			{
				item.Id,
				item.ProductFormulId,
				item.PartId,
				item.UsingRate
			});
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> DeleteItem(long id, CancellationToken cn)
		{
			var itemRepo = unitOfWork.Repository<ProductFormulItem>();
			var model = await itemRepo.TableNoTracking.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (model == null)
				return NotFound("قلم پیدا نشد");

			await itemRepo.DeleteAsync(model, cn, true);
			return Ok();
		}
	}
}
