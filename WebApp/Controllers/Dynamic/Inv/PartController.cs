using Azure.Core;
using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Data.SystemAuth;
using Entities.App.Bom;
using Entities.App.Inv;
using Entities.App.Inv.Enums;
using Entities.Base;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Inv/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("کالا", typeof(Part))]
	public class PartController(IUnitOfWork unitOfWork, IPropertyIdentityService identityService, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(Part part, CancellationToken cn)
		{
			 
			part.SpareParts = null!;
			part.Documents = null!;

			if (part.Id == null || part.Id == 0)
			{
				return await Add(part, cn);
			}
			var exist = await unitOfWork.Repository<Part>().TableNoTracking.AnyAsync(c => c.Id == part.Id);
			if (exist)
			{
				return await Update(part, cn);
			}
			return await Add(part, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(Part part, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<Part>().SaveAsync(part, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(Part part, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<Part>().UpdateAsync(part, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<Part>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<Part>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<Part>().TableNoTracking
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Inv\Part\Edit.cshtml", entity);
			}
			var newEntity = new Part();
			return View(@"\Views\Panel\Inv\Part\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var newEntity = new Part();
			return View(@"\Views\Panel\Inv\Part\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Inv\Part\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<Part>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<Part>().FetchDataAsync(request, cn));
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> PartDocumentInfo(long? id)
		{
			// Repositories
			var partDocumentRepo = unitOfWork.Repository<PartDocument>();
			var fileEntityRepo = unitOfWork.Repository<FileEntity>();
			var productFormulRepo = unitOfWork.Repository<ProductFormul>();
			var formulItemRepo = unitOfWork.Repository<ProductFormulItem>();
			var partRepo = unitOfWork.Repository<Part>();

			// 1. Subquery: maximum Id per (PartId, Type)
			var maxIds = partDocumentRepo.TableNoTracking
				.Where(c=>c.PartId == id)
			    .GroupBy(pd => new { pd.PartId, pd.Type })
			    .Select(g => new
			    {
				    g.Key.PartId,
				    g.Key.Type,
				    MaxId = g.Max(pd => pd.Id)
			    });

			// 2. CTE equivalent: latest attachment per part+type (joined with FileEntity)
			var lastAttachments = from pd in partDocumentRepo.TableNoTracking
							  join fe in fileEntityRepo.TableNoTracking
								 on pd.AttachmentId equals fe.Id
							  join max in maxIds
								 on new { pd.PartId, pd.Type } equals new { max.PartId, max.Type }
							  where pd.Id == max.MaxId
							  select new
							  {
								  pd.AttachmentId,
								  pd.PartId,
								  fe.OriginalName,
								  fe.Size,
								  pd.CreatedByName,
								  pd.CreatedOnShamsiDateTime,
								  pd.ModifiedDateShamsiDateTime,
								  pd.ModifiedByName,
								  pd.Type,
								  pd.Main
							  };


			var PartDocumentType = EnumExtensions.GetEnumValuesWithDisplayNames<PartDocumentTypeEnum>();

			// 3. Main query
			var query = from bf in productFormulRepo.TableNoTracking
					  join bfi in formulItemRepo.TableNoTracking
						 on bf.Id equals bfi.ProductFormulId into bfiGroup
					  from bfi in bfiGroup.DefaultIfEmpty()   // LEFT JOIN
					  from v in lastAttachments               // INNER JOIN with CTE (becomes cross + where)
					  join p in partRepo.TableNoTracking
						 on bf.ProductId equals p.Id         // INNER JOIN Part
					  where v.PartId == bf.ProductId || (bfi != null && v.PartId == bfi.PartId)
					  select new  
					  {
						  Code = p.Code,
						  Name = p.Name,
						  OriginalName = v.OriginalName,
						  Size = v.Size,
						  CreatedByName = v.CreatedByName,
						  CreatedOnShamsiDateTime = v.CreatedOnShamsiDateTime,
						  ModifiedDateShamsiDateTime = v.ModifiedDateShamsiDateTime,
						  ModifiedByName = v.ModifiedByName,
						  Type = v.Type,
						  Main = v.Main,
						  AttachmentId = v.AttachmentId
					  };

			// Execute with DISTINCT
			var result = await query.Distinct().ToListAsync();

			var resultModel = result.Select(v=> new
			{
				Code = v.Code,
				Name = v.Name,
				OriginalName = v.OriginalName,
				Size = v.Size,
				CreatedByName = v?.CreatedByName ?? "-",
				CreatedOnShamsiDateTime = v.CreatedOnShamsiDateTime,
				ModifiedDateShamsiDateTime = v.ModifiedDateShamsiDateTime,
				ModifiedByName = v?.ModifiedByName ?? "-",
				Type = PartDocumentType.FirstOrDefault(c => c.Value == (int)v.Type).Text,
				Main = (v.Main == true ? "بلی" : "خیر"),
				AttachmentId = v.AttachmentId
			});


			return Ok(resultModel);
		}
	}
}
