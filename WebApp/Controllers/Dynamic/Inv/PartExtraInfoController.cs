using Common.Attributes;
using Common.Auth.Enums;
using Data;
using Data.Contracts;
using Data.SystemAuth;
using Entities.App.Inv;
using Entities.App.Inv.Enums;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Stimulsoft.Blockly.Model;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Inv/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("شناسنامه محصولات", typeof(PartExtraInfo))]
	public class PartExtraInfoController(IUnitOfWork unitOfWork ,ApplicationDbContext _context, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(PartExtraInfo partExtraInfo, CancellationToken cn)
		{
			if (partExtraInfo.Id == null || partExtraInfo.Id == 0)
			{
				return await Add(partExtraInfo, cn);
			}
			var exist = await unitOfWork.Repository<PartExtraInfo>().TableNoTracking.AnyAsync(c => c.Id == partExtraInfo.Id);
			if (exist)
			{
				return await Update(partExtraInfo, cn);
			}
			return await Add(partExtraInfo, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(PartExtraInfo partExtraInfo, CancellationToken cn)
		{
			if(partExtraInfo.InfoType == PartExtraInfoInfoTypeEnum.OilFreeScrewCompressor)
			{
				partExtraInfo = ComputeAndFillBomFields(partExtraInfo);
			}
			 
			partExtraInfo = ComputeAndFillBomFieldsJson(partExtraInfo);

			var	entity = await unitOfWork.Repository<PartExtraInfo>().SaveAsync(partExtraInfo, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(PartExtraInfo partExtraInfo, CancellationToken cn)
		{

			if (partExtraInfo.InfoType == PartExtraInfoInfoTypeEnum.OilFreeScrewCompressor)
			{
				partExtraInfo = ComputeAndFillBomFields(partExtraInfo);
			}
			partExtraInfo = ComputeAndFillBomFieldsJson(partExtraInfo);

			var entity = await unitOfWork.Repository<PartExtraInfo>().UpdateAsync(partExtraInfo, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<PartExtraInfo>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<PartExtraInfo>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<PartExtraInfo>().TableNoTracking
					.Include(c => c.Product)
					.Include(c => c.Product)
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Inv\PartExtraInfo\Edit.cshtml", entity);
			}
			var newEntity = new PartExtraInfo();
			return View(@"\Views\Panel\Inv\PartExtraInfo\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var newEntity = new PartExtraInfo();
			return View(@"\Views\Panel\Inv\PartExtraInfo\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Inv\PartExtraInfo\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<PartExtraInfo>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<PartExtraInfo>().FetchDataAsync(request, cn));
		}



		private PartExtraInfo ComputeAndFillBomFields(PartExtraInfo entity)
		{
			try
			{
				var productItems = (from invPartExtraInfo in unitOfWork.Repository<PartExtraInfo>().TableNoTracking
								join bomProductItem in _context.vw_ProductItems.AsNoTracking() on invPartExtraInfo.ProductId equals bomProductItem.PartId
								join part in unitOfWork.Repository<Part>().TableNoTracking on bomProductItem.PartItemId equals part.Id
								where part.DataSheetUsage && part.DataSheet.HasValue
								select new
								{
									bomProductItem.PartId,
									part.DataSheet,
									part.BrandInDataSheet,
									bomProductItem.UsingRate,
									PartItemId = part.Id
								}).ToArray();


				productItems = productItems.Where(p => p.PartId == entity.ProductId)
				    .Distinct()
				    .ToArray();


				var result = productItems.Where(p => p.DataSheet == (PartDataSheetEnum)637).ToArray();
				if (result.Any())
				{
					entity.AirendQty = result.Sum(p => p.UsingRate);
					entity.AirendBrand = result.First().BrandInDataSheet;
				}

				result = productItems.Where(p => p.DataSheet  == (PartDataSheetEnum) 638).ToArray();
				if (result.Any())
				{
					entity.ElectroMotorQty = result.Sum(p => p.UsingRate);
					entity.ElectroMotorBrand = result.First().BrandInDataSheet;
				}

				result = productItems.Where(p => p.DataSheet  == (PartDataSheetEnum) 639).ToArray();
				if (result.Any())
				{
					entity.CoolingFanQty = result.Sum(p => p.UsingRate);
					entity.CoolingFanBrand = result.First().BrandInDataSheet;
				}

				result = productItems.Where(p => p.DataSheet  == (PartDataSheetEnum) 640).ToArray();
				if (result.Any())
				{
					entity.UnloaderValveQty = result.Sum(p => p.UsingRate);
					entity.UnloaderValveBrand = result.First().BrandInDataSheet;
				}

				result = productItems.Where(p => p.DataSheet  == (PartDataSheetEnum) 641).ToArray();
				if (result.Any())
				{
					entity.MinimumPressureValveQty = result.Sum(p => p.UsingRate);
					entity.MinimumPressureValveBrand = result.First().BrandInDataSheet;
				}

				result = productItems.Where(p => p.DataSheet  == (PartDataSheetEnum) 642).ToArray();
				if (result.Any())
				{
					entity.OilTermostaticValveQty = result.Sum(p => p.UsingRate);
					entity.OilTermostaticValveBrand = result.First().BrandInDataSheet;
				}

				result = productItems.Where(p => p.DataSheet  == (PartDataSheetEnum) 643).ToArray();
				if (result.Any())
				{
					entity.AirOilFilterSepratorQty = result.Sum(p => p.UsingRate);
					entity.AirOilFilterSepratorBrand = result.First().BrandInDataSheet;
				}

				result = productItems.Where(p => p.DataSheet  == (PartDataSheetEnum) 644).ToArray();
				if (result.Any())
				{
					entity.AirIntakeFilterQty = result.Sum(p => p.UsingRate);
					entity.AirIntakeFilterBrand = result.First().BrandInDataSheet;
				}

				result = productItems.Where(p => p.DataSheet  == (PartDataSheetEnum) 645).ToArray();
				if (result.Any())
				{
					entity.TemperatureSensorQty = result.Sum(p => p.UsingRate);
					entity.TemperatureSensorBrand = result.First().BrandInDataSheet;
				}

				result = productItems.Where(p => p.DataSheet  == (PartDataSheetEnum) 646).ToArray();
				if (result.Any())
				{
					entity.PressureSensorQty = result.Sum(p => p.UsingRate);
					entity.PressureSensorBrand = result.First().BrandInDataSheet;
				}

				result = productItems.Where(p => p.DataSheet  == (PartDataSheetEnum) 647).ToArray();
				if (result.Any())
				{
					entity.PressureIndicatorQty = result.Sum(p => p.UsingRate);
					entity.PressureIndicatorBrand = result.First().BrandInDataSheet;
				}

				var safetyValveIds = new List<int> { 648, 1270, 1271, 1272, 1273, 1274, 1275, 1276, 1277, 1278, 1279 };
				result = productItems.Where(p => p.DataSheet.HasValue && safetyValveIds.Contains((int)p.DataSheet)).ToArray();
				if (result.Any())
				{
					entity.SafetyValveQty = result.Sum(p => p.UsingRate);
					entity.SafetyValveBrand = result.First().BrandInDataSheet;
				}

				result = productItems.Where(p => p.DataSheet  == (PartDataSheetEnum) 649).ToArray();
				if (result.Any())
				{
					var groupedCouplingPulleyItems = result.GroupBy(p => p.PartItemId).ToList();
					switch (groupedCouplingPulleyItems.Count)
					{
						case 1:
							entity.CouplingPulleyQty = result.Sum(p => p.UsingRate);
							entity.CouplingPulleyBrand = result.First().BrandInDataSheet;
							break;
						case 2:
							entity.CouplingPulleyQty = groupedCouplingPulleyItems[0].Sum(p => p.UsingRate);
							entity.CouplingPulleyBrand = groupedCouplingPulleyItems[0].First().BrandInDataSheet;

							entity.CouplingPulley2Qty = groupedCouplingPulleyItems[1].Sum(p => p.UsingRate);
							entity.CouplingPulley2Brand = groupedCouplingPulleyItems[1].First().BrandInDataSheet;
							break;
					}
				}

				result = productItems.Where(p => p.DataSheet  == (PartDataSheetEnum) 650).ToArray();
				if (result.Any())
				{
					entity.CoolerModelQty = result.Sum(p => p.UsingRate);
					entity.CoolerModelBrand = result.First().BrandInDataSheet;
				}

				result = productItems.Where(p => p.DataSheet  == (PartDataSheetEnum) 651).ToArray();
				if (result.Any())
				{
					entity.OilFilterQty = result.Sum(p => p.UsingRate);
					entity.OilFilterBrand = result.First().BrandInDataSheet;
				}

				result = productItems.Where(p => p.DataSheet  == (PartDataSheetEnum) 652).ToArray();
				if (result.Any())
				{
					entity.BeltSizeQty = result.Sum(p => p.UsingRate);
					entity.BeltSizeBrand = result.First().BrandInDataSheet;
				}

				result = productItems.Where(p => p.DataSheet  == (PartDataSheetEnum) 653).ToArray();
				if (result.Any())
				{
					entity.OilCapacityQty = result.Sum(p => p.UsingRate);
					entity.OilCapacityBrand = result.First().BrandInDataSheet;
				}

				return entity;
			}
			catch (Exception ex)
			{
				 
			}
			return new PartExtraInfo();
		}


		public class ProductItemInfo
		{
			public decimal Qty { get; set; }
			public string Brand { get; set; }
		}

		private PartExtraInfo ComputeAndFillBomFieldsJson(PartExtraInfo entity)
		{
			 
			var productItems =
			    (from extra in unitOfWork.Repository<PartExtraInfo>().TableNoTracking
				join bom in _context.vw_ProductItems.AsNoTracking()
				    on extra.ProductId equals bom.PartId
				join part in unitOfWork.Repository<Part>().TableNoTracking
				    on bom.PartItemId equals part.Id
				where extra.ProductId == entity.ProductId
					 && part.DataSheetUsage
					 && part.DataSheet.HasValue
				select new
				{
					DataSheet = (PartDataSheetEnum)part.DataSheet.Value,
					part.BrandInDataSheet,
					bom.UsingRate
				})
			    .ToList();

		 
			var groupedItems = productItems
			    .GroupBy(x => x.DataSheet)
			    .ToDictionary(
				   g => g.Key.ToString(),  
				   g => new ProductItemInfo
				   {
					   Qty = g.Sum(x => x.UsingRate),
					   Brand = g.Select(x => x.BrandInDataSheet).FirstOrDefault()
				   }
			    );

			// 3. تبدیل به JSON و ذخیره در فیلد جدید
			entity.DryerExtraInfo = JsonConvert.SerializeObject(
			    groupedItems,
			    new JsonSerializerSettings
			    {
				    NullValueHandling = NullValueHandling.Ignore
			    }
			);

			return entity;
		}


	}


}
