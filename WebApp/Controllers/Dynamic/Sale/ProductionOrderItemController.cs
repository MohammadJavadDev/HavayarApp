using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Data.SystemAuth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Entities.Base.DataTable;
using WebFramework.Filtters;
using WebFramework.Page;
using Entities.App.Sale;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("اقلام سفارش ساخت", typeof(ProductionOrderItem))]
	public class ProductionOrderItemController(IUnitOfWork unitOfWork, IPropertyIdentityService identityService, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(ProductionOrderItem productionOrderItem, CancellationToken cn)
		{
			

			if (productionOrderItem.Id == null || productionOrderItem.Id == 0)
			{
				return await Add(productionOrderItem, cn);
			}
			var exist = await unitOfWork.Repository<ProductionOrderItem>().TableNoTracking.AnyAsync(c => c.Id == productionOrderItem.Id);
			if (exist)
			{
				return await Update(productionOrderItem, cn);
			}
			return await Add(productionOrderItem, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(ProductionOrderItem productionOrderItem, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<ProductionOrderItem>().SaveAsync(productionOrderItem, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(ProductionOrderItem productionOrderItem, CancellationToken cn)
		{

			var oldEntity = await unitOfWork.Repository<ProductionOrderItem>()
				.TableNoTracking
				.FirstOrDefaultAsync(
				c => c.Id == productionOrderItem.Id);

			oldEntity.ProductionStep = productionOrderItem.ProductionStep;
			oldEntity.PlanningNumber = productionOrderItem.PlanningNumber;
			oldEntity.Serial = productionOrderItem.Serial;
			oldEntity.ProductionStartShamsiDate = productionOrderItem.ProductionStartShamsiDate;
			oldEntity.ProductionStartMiladiDate = productionOrderItem.ProductionStartMiladiDate;
			oldEntity.ProductionEndShamsiDate = productionOrderItem.ProductionEndShamsiDate;
			oldEntity.ProductionEndMiladiDate = productionOrderItem.ProductionEndMiladiDate;
			oldEntity.TestingEndShamsiDate = productionOrderItem.TestingEndShamsiDate;
			oldEntity.TestingEndMiladiDate = productionOrderItem.TestingEndMiladiDate;
			oldEntity.PreparationShamsiDate = productionOrderItem.PreparationShamsiDate;
			oldEntity.PreparationMiladiDate = productionOrderItem.PreparationMiladiDate;
			oldEntity.DeliveryShamsiDate	 = productionOrderItem.DeliveryShamsiDate;
			oldEntity.DeliveryMiladiDate = productionOrderItem.DeliveryMiladiDate;
			oldEntity.Status = productionOrderItem.Status;
			oldEntity.ProductionStatus = productionOrderItem.ProductionStatus;
			oldEntity.SalesConsideration = productionOrderItem.SalesConsideration;

			var entity = await unitOfWork.Repository<ProductionOrderItem>().UpdateAsync(oldEntity, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<ProductionOrderItem>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<ProductionOrderItem>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<ProductionOrderItem>().TableNoTracking
					.Include(c => c.ProductionOrder)
					.Include(c => c.Part)
					.FirstOrDefault(c => c.Id == id);

				 
				ViewBag.ProductionOrderItemBomViewData = unitOfWork
					.Repository<ProductionOrderItemBom>()
					.TableNoTracking
					.Include(c => c.Part)
					.ThenInclude(c=>c.Unit)
					.Where(c=>c.ProductionOrderItemId == id)
					.Select(c=> new ProductionOrderItemBomViewModel()
					{
						ProductionOrderItemId = c.ProductionOrderItemId,
						Amount = c.Amount,
						CreatedByName = c.CreatedByName,
						CreatedOnMiladiDateTime = c.CreatedOnMiladiDateTime,
						CreatedOnShamsiDateTime = c.CreatedOnShamsiDateTime,
						Description = c.Description,
						Id = c.Id,
						IsLatest = c.IsLatest,
						IsActive= c.IsActive,
						ModifiedByName = c.ModifiedByName,
						ModifiedDateMiladiDateTime= c.ModifiedDateMiladiDateTime,
						ModifiedDateShamsiDateTime =c.ModifiedDateShamsiDateTime,
						NeedsAVL = c.NeedsAVL,
						PartId = c.PartId,
						PartName = c.Part.Name,
						PartCode = c.Part.Code,
						Revision = c.Revision,
						SaleUnitDetails = c.SaleUnitDetails,
						PartUnitName = c.Part.Unit.Title,
						ProductionStep = c.ProductionStep,
						NumberSupplied = c.NumberSupplied,
						Status = c.Status
					}).ToList();

				return View(@"\Views\Panel\Sale\ProductionOrderItem\Edit.cshtml", entity);
			}
			var newEntity = new ProductionOrderItem();
			return View(@"\Views\Panel\Sale\ProductionOrderItem\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var newEntity = new ProductionOrderItem();
			return View(@"\Views\Panel\Sale\ProductionOrderItem\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Sale\ProductionOrderItem\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<ProductionOrderItem>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<ProductionOrderItem>().FetchDataAsync(request, cn));
		}

	[HttpGet("[action]")]
	public IActionResult ProductionOrderItemBomPartial(long? id, long productionOrderItemId)
	{
		if (id != null && id > 0)
		{
			var bom = unitOfWork.Repository<ProductionOrderItemBom>()
				.TableNoTracking
				.Include(c => c.Part)
				.FirstOrDefault(c => c.Id == id);
			return PartialView(@"\Views\Panel\Sale\ProductionOrderItem\_ProductionOrderItemBomPartial.cshtml", bom);
		}
		
		var newBom = new ProductionOrderItemBom { ProductionOrderItemId = productionOrderItemId };
		return PartialView(@"\Views\Panel\Sale\ProductionOrderItem\_ProductionOrderItemBomPartial.cshtml", newBom);
	}

	[HttpPost("[action]")]
	[ActionDisplayName("ذخیره BOM", ActionAccessType.Api)]
	public async Task<IActionResult> SaveBom(ProductionOrderItemBom bom, CancellationToken cn)
	{
		ProductionOrderItemBom entity;
		if (bom.Id == null || bom.Id == 0)
		{
			entity = await unitOfWork.Repository<ProductionOrderItemBom>().SaveAsync(bom, cn, true);
		}
		else
		{
			entity = await unitOfWork.Repository<ProductionOrderItemBom>().UpdateAsync(bom, cn, true);
		}

		// بارگذاری مجدد با Include برای برگرداندن اطلاعات کامل
		var viewModel = await unitOfWork.Repository<ProductionOrderItemBom>()
			.TableNoTracking
			.Include(c => c.Part)
			.ThenInclude(c => c.Unit)
			.Where(c => c.Id == entity.Id)
			.Select(c => new ProductionOrderItemBomViewModel()
			{
				ProductionOrderItemId = c.ProductionOrderItemId,
				Amount = c.Amount,
				CreatedByName = c.CreatedByName,
				CreatedOnMiladiDateTime = c.CreatedOnMiladiDateTime,
				CreatedOnShamsiDateTime = c.CreatedOnShamsiDateTime,
				Description = c.Description,
				Id = c.Id,
				IsLatest = c.IsLatest,
				IsActive = c.IsActive,
				ModifiedByName = c.ModifiedByName,
				ModifiedDateMiladiDateTime = c.ModifiedDateMiladiDateTime,
				ModifiedDateShamsiDateTime = c.ModifiedDateShamsiDateTime,
				NeedsAVL = c.NeedsAVL,
				PartId = c.PartId,
				PartName = c.Part.Name,
				PartCode = c.Part.Code,
				Revision = c.Revision,
				SaleUnitDetails = c.SaleUnitDetails,
				PartUnitName = c.Part.Unit.Title,
				ProductionStep = c.ProductionStep,
				NumberSupplied = c.NumberSupplied,
				Status = c.Status
			}).FirstOrDefaultAsync(cn);

		return Ok(viewModel);
	}

	[HttpGet("[action]")]
	public IActionResult ProductionOrderItemCommentPartial(long? id, long productionOrderItemId)
	{
		if (id != null && id > 0)
		{
			var comment = unitOfWork.Repository<ProductionOrderItemComment>()
				.TableNoTracking
				.FirstOrDefault(c => c.Id == id);
			return PartialView(@"\Views\Panel\Sale\ProductionOrderItem\_ProductionOrderItemCommentPartial.cshtml", comment);
		}

		var newComment = new ProductionOrderItemComment { ProductionOrderItemId = productionOrderItemId };
		return PartialView(@"\Views\Panel\Sale\ProductionOrderItem\_ProductionOrderItemCommentPartial.cshtml", newComment);
	}

	[HttpPost("[action]")]
	[ActionDisplayName("ذخیره کامنت", ActionAccessType.Api)]
	public async Task<IActionResult> SaveComment(ProductionOrderItemComment comment, CancellationToken cn)
	{
		if (comment.Id == null || comment.Id == 0)
		{
			var entity = await unitOfWork.Repository<ProductionOrderItemComment>().SaveAsync(comment, cn, true);
			return Ok(entity);
		}
		else
		{
			var entity = await unitOfWork.Repository<ProductionOrderItemComment>().UpdateAsync(comment, cn, true);
			return Ok(entity);
		}
	}

	[HttpGet("[action]")]
	public IActionResult ProductionOrderItemCommentListPartial(long productionOrderItemId)
	{
		var comments = unitOfWork.Repository<ProductionOrderItemComment>()
			.TableNoTracking
			.Where(c => c.ProductionOrderItemId == productionOrderItemId)
			.OrderByDescending(c => c.CreatedOnMiladiDateTime)
			.ToList();

		return PartialView(@"\Views\Panel\Sale\ProductionOrderItem\_ProductionOrderItemCommentListPartial.cshtml", comments);
	}
}

	public class ProductionOrderItemBomViewModel: ProductionOrderItemBom
	{
		public string PartName { get; set; }
		public string PartCode { get; set; }

		public string PartUnitName { get; set; }
	}
 
}
