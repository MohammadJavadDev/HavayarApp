using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data;
using Data.Contracts;
using Data.SystemAuth;
using Entities.App.Bom;
using Entities.App.Bom.Enums;
using Entities.App.Inv;
using Entities.App.Sale;
using Entities.App.Sale.Enums;
using Entities.Base;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using WebFramework.Filtters;
using WebFramework.Page;


namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Bom/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("درخواست ویرایش اقلام فرمول", typeof(FormulChangeRequest))]
	public class FormulChangeRequestController(IUnitOfWork unitOfWork,
		IPropertyIdentityService identityService,
		IWebHostEnvironment _webHostEnvironment,
		ApplicationDbContext _context
		) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(FormulChangeRequest formulChangeRequest, CancellationToken cn)
		{


			if (formulChangeRequest.Id == null || formulChangeRequest.Id == 0)
			{
				return await Add(formulChangeRequest, cn);
			}
			var exist = await unitOfWork.Repository<FormulChangeRequest>().TableNoTracking.AnyAsync(c => c.Id == formulChangeRequest.Id);
			if (exist)
			{
				return await Update(formulChangeRequest, cn);
			}
			return await Add(formulChangeRequest, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(FormulChangeRequest formulChangeRequest, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<FormulChangeRequest>().SaveAsync(formulChangeRequest, cn, true);
			await SaveComment(entity);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(FormulChangeRequest formulChangeRequest, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<FormulChangeRequest>().UpdateAsync(formulChangeRequest, cn, true);
			await SaveComment(entity);
			if (entity.Status == FormulChangeRequestStatusEnum.EndOperation)
				 await ChangeBom(entity, cn);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<FormulChangeRequest>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<FormulChangeRequest>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<FormulChangeRequest>().TableNoTracking
					.Include(c => c.Part)
					.Include(c => c.ReplacementPart)
					.Include(c => c.Part)
					.Include(c => c.ReplacementPart)
					.Include(c => c.Comments)
					.AsSplitQuery()
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Bom\FormulChangeRequest\Edit.cshtml", entity);
			}
			var newEntity = new FormulChangeRequest();
			return View(@"\Views\Panel\Bom\FormulChangeRequest\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var newEntity = new FormulChangeRequest();
			return View(@"\Views\Panel\Bom\FormulChangeRequest\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Bom\FormulChangeRequest\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<FormulChangeRequest>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<FormulChangeRequest>().FetchDataAsync(request, cn));
		}

		public async Task SaveComment(FormulChangeRequest entity, CancellationToken cn = default)
		{

			if (entity.AttachmentId != null)
			{
				var fileAttachment = unitOfWork.Repository<FileEntity>()
				.Table
				.First(c => c.Id == entity.AttachmentId);

				if (fileAttachment != null)
				{
					fileAttachment.EntityPropName = "Attachment";
					fileAttachment.EntityType = typeof(FormulChangeRequestComment).FullName;
				}
			}

			var newComment = new FormulChangeRequestComment()
			{
				AttachmentId = entity.AttachmentId,
				Description = entity.Description,
				Status = entity.Status,
				FormulChangeRequestId = entity.Id.Value
			};


			await unitOfWork.Repository<FormulChangeRequestComment>().
				AddAsync(newComment, cn, false);
		}

		public async Task ChangeBom(FormulChangeRequest entity, CancellationToken cn = default)
		{
			var hasChangedGroups = entity.ChangedProductGroupIds.HasValue();
			var hasChangedProducts = entity.ChangedProductFormulIds.HasValue();

			if (!hasChangedGroups && !hasChangedProducts)
				throw new Exception("هیچ گروه محصول یا محصولی برای ویرایش انتخاب نشده است.");

			var productFormulIdSet = new HashSet<long>();

			if (hasChangedGroups)
			{
				var productGroupIds = entity.ChangedProductGroupIds
					.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
					.Select(x => x.ToLong())
					.Where(id => id > 0)
					.Distinct()
					.ToList();

				if (productGroupIds.Count > 0)
				{
					var fromGroups = unitOfWork.Repository<ProductFormul>().TableNoTracking
						.Where(p => p.ProductNameGroupId != null && productGroupIds.Contains(p.ProductNameGroupId.Value))
						.Select(p => p.Id!.Value)
						.ToList();

					foreach (var id in fromGroups)
						productFormulIdSet.Add(id);
				}
			}

			if (hasChangedProducts)
			{
				var selectedProductFormulIds = entity.ChangedProductFormulIds
					.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
					.Select(x => x.ToLong())
					.Where(id => id > 0);

				foreach (var id in selectedProductFormulIds)
					productFormulIdSet.Add(id);
			}

			if (productFormulIdSet.Count == 0)
				return;

			var productFormulIds = productFormulIdSet.ToList();

			var itemRepo = unitOfWork.Repository<ProductFormulItem>();
			var usingRate = entity.ChangedUsingRate.HasValue
				? (decimal?)entity.ChangedUsingRate.Value
				: null;

			if (entity.OperationType == FormulChangeRequestOperationTypeEnum.Add)
			{
				if (!entity.PartId.HasValue)
					throw new Exception("قطعه برای افزودن مشخص نشده است.");

				foreach (var productFormulId in productFormulIds)
				{
					var existing = itemRepo.Table
						.FirstOrDefault(p => p.ProductFormulId == productFormulId && p.PartId == entity.PartId.Value);

					if (existing != null)
					{
						if (usingRate.HasValue)
							existing.UsingRate = usingRate;
					}
					else
					{
						await itemRepo.AddAsync(new ProductFormulItem
						{
							ProductFormulId = productFormulId,
							PartId = entity.PartId.Value,
							UsingRate = usingRate ?? 1
						}, cn, false);
					}
				}

				await unitOfWork.SaveChangesAsync(cn);
			}
			else if (entity.OperationType == FormulChangeRequestOperationTypeEnum.Replacement)
			{
				if (!entity.PartId.HasValue || !entity.ReplacementPartId.HasValue)
					throw new Exception("قطعه و قطعه جایگزین برای جایگزینی مشخص نشده‌اند.");

				var items = itemRepo.Table
					.Where(p => productFormulIds.Contains(p.ProductFormulId) && p.PartId == entity.PartId.Value)
					.ToList();

				if (items.Count == 0)
					return;

				foreach (var item in items)
				{
					var duplicate = itemRepo.Table
						.FirstOrDefault(p =>
							p.ProductFormulId == item.ProductFormulId
							&& p.PartId == entity.ReplacementPartId.Value
							&& p.Id != item.Id);

					if (duplicate != null)
					{
						duplicate.UsingRate = (duplicate.UsingRate ?? 0) + (usingRate ?? item.UsingRate ?? 0);
						await itemRepo.DeleteAsync(item, cn, false);
					}
					else
					{
						item.PartId = entity.ReplacementPartId.Value;
						item.Part = null;
						if (usingRate.HasValue && usingRate.Value != 0)
							item.UsingRate = usingRate;
					}
				}

				await unitOfWork.SaveChangesAsync(cn);
			}
			else if (entity.OperationType == FormulChangeRequestOperationTypeEnum.Deleted)
			{
				if (!entity.PartId.HasValue)
					throw new Exception("قطعه برای حذف مشخص نشده است.");

				await itemRepo.Table
					.Where(p => productFormulIds.Contains(p.ProductFormulId) && p.PartId == entity.PartId.Value)
					.ExecuteDeleteAsync(cn);
			}

			#region ارسال ایمیل اطلاع رسانی نهایی

			SendChangePartNotificationEmail(entity);

			#endregion

			await UpdateDataSheets(entity.PartId.Value, entity.ReplacementPartId, cn);
		}

		private async Task UpdateDataSheets(long partId , long? replacePartId,CancellationToken cn)
		{

			var partIds = new List<long> { partId };

			if (replacePartId.HasValue)
				partIds.Add(replacePartId.Value);

			var productIds = _context.vw_ProductItems
			    .Where(p => partIds.Contains(p.PartItemId))
			    .Select(p => p.PartId)
			    .Distinct()
			    .ToArray();

			if (!productIds.Any())
				return;

			var partExtraInfos = unitOfWork.Repository<PartExtraInfo>()
							.TableNoTracking. Where(p => productIds.Contains(p.ProductId))
							.GroupBy(p => p.ProductId)
							.Select(p => p.OrderByDescending(o => o.Id).FirstOrDefault())
							.ToList();

			if (!partExtraInfos.Any())
				return;

			 foreach(var partExtraInfo in partExtraInfos)
			{
				partExtraInfo.Id = null;
				partExtraInfo.Product = null;
				partExtraInfo.CreatedById = null;
				partExtraInfo.CreatedByName = null;
				partExtraInfo.CreatedOnMiladiDateTime = null;
				partExtraInfo.CreatedOnShamsiDateTime = null;
				partExtraInfo.ModifiedById = null;
				partExtraInfo.ModifiedByName = null;
				partExtraInfo.Revision = null;

				await new PartExtraInfoController(unitOfWork, _context, _webHostEnvironment)
					.Add(partExtraInfo, cn);
			}
		}

		private void SendChangePartNotificationEmail(FormulChangeRequest entity)
		{
			var content = GetNotificationEmailMessage(entity, out var partCode, out var hasProductionOrderItemRecord, true);
			
			if(hasProductionOrderItemRecord)
			{

			}
		
			//TODO Complete SendEmail
		}

		private string GetNotificationEmailMessage(FormulChangeRequest entity, out string partCode, out bool hasProductionOrderItemRecord, bool changedPart = false)
		{
			partCode = null;
			hasProductionOrderItemRecord = false;

			if (entity == null)
				return null;

			var item = unitOfWork.Repository<FormulChangeRequest>()
							.TableNoTracking
			    .Where(p => p.Id == entity.Id)
			    .Include(p => p.Part)
			    .Include(p => p.ReplacementPart)
			    .AsSplitQuery()
			    .FirstOrDefault();

			if (item == null)
				return null;


			var content = new StringBuilder();

			content.AppendLine("<div style='text-align:center;direction:rtl'>");
			content.AppendLine("<table border='1' cellspacing='0' cellpadding='5' style='text-align:right; direction: rtl'  width='100%' >");
			content.AppendLine("<tr style='width: 7.0in; background: #000aa0'>");
			content.AppendLine("<td colspan='2'>");
			content.AppendLine("<div  style='font-size:14.0pt;font-family:Zar;color:#FFFFFF;text-align:center;direction:rtl'>");
			content.AppendLine("گروه صنعتی هوایار");
			content.AppendLine("</div>");
			content.AppendLine("</td>");
			content.AppendLine("</tr>");

			content.AppendLine("<tr>");
			content.AppendLine("<td colspan='2'>");
			content.AppendLine("<div  style='font-size:14.0pt;font-family:Zar;color:#1F497D;text-align:right;direction:rtl'>");
			content.AppendLine("با سلام و احترام <br />");


			var hasAlternativePart = item.ReplacementPartId.HasValue;

			partCode = hasAlternativePart ? item.ReplacementPart?.Code : item.Part?.Code;

			if (hasAlternativePart)
			{
				content.Append(changedPart ? "تغییر قطعه " : "درخواست تغییر قطعه ");

				content.Append($"<strong>'{item.Part?.Code} - {item.Part.Name}'</strong> ");
				content.Append("<br/>");

				content.Append("با قطعه ");
				content.Append($"<strong>'{item.ReplacementPart.Code} - {item.ReplacementPart.Name}'</strong> ");
			}
			else
			{
				if (entity.OperationType == FormulChangeRequestOperationTypeEnum.Deleted)
					content.Append(changedPart ? "حذف قطعه " : "درخواست حذف قطعه ");
				else
					content.Append(changedPart ? "ایجاد قطعه " : "درخواست ایجاد قطعه ");

				content.Append($"<strong>'{item.Part.Code} - {item.Part.Name}'</strong> ");
				content.Append("<br/>");
			}

			if (item.ChangedProductGroupNames.HasValue())
			{
				content.Append("در گروه محصول(های) ");
				content.Append("<ul>");
				foreach (var groupItem in item.ChangedProductGroupNames.Split(","))
				{
					content.Append("<li>");
					content.Append($"<strong>{groupItem}</strong>");
					content.Append("</li>");
				}
				content.Append("</ul>");
			}

			if (item.ChangedProductFormulNames.HasValue())
			{
				content.Append("در محصول(های) ");
				content.Append("<ul>");
				foreach (var productItem in item.ChangedProductFormulNames.Split(","))
				{
					content.Append("<li>");
					content.Append($"<strong>{productItem}</strong>");
					content.Append("</li>");
				}
				content.Append("</ul>");
			}
			else if (!item.ChangedProductGroupNames.HasValue() && item.ChangedFormulNames.HasValue())
				{
					content.Append("در فرمول(های) ");

					content.Append("<ul>");
					foreach (var formulaItem in item.ChangedFormulNames.Split(","))
					{
						content.Append("<li>");
						content.Append($"<strong>{formulaItem}</strong>");
						content.Append("</li>");
					}
					content.Append("</ul>");
				}
				content.Append("<br />");

				content.Append($"بواسطه <strong>'{CurrentUserFullNameFn}'</strong> ");
				content.Append($"در تاریخ <strong>'{DateTime.Now.ToShamsiDateTime()}'</strong> ");
				content.Append(changedPart ? "انجام گردید " : "ایجاد گردید ");

			 

			#region  اقلام سفارش ساخت جهت اعلان به مهندسی BOM بررسی قلم تغییر یافته در  

			var partId = entity.ReplacementPartId ?? entity.PartId;
				var productionOrderItemBomList = unitOfWork.Repository<ProductionOrderItemBom>()
						.TableNoTracking
						.Where(p => !p.ProductionOrderItem.IsDeleted &&
								p.ProductionOrderItem.IsLatestVersion &&
								p.ProductionOrderItem.Status == ProductionOrderItemStatusEnum.NotCompleted && // وضعیت 0
								!p.ProductionOrderItem.PreparationMiladiDate.HasValue && // وضعیت 0
								p.ProductionOrderItem.Serial != null &&
								p.ProductionOrderItem.Serial.Length > 3 &&
								p.ProductionOrderItem.CheckStatus == ProductionOrderItemCheckStatusEnum.IndustrialDashboardInternal &&
								p.ProductionOrderItem.ProductionOrderId != 10529 && // 10529 -> ProductionOrderNumber == 0 
								p.PartId == partId)
											 .Select(p => new
											 {
												 p.ProductionOrderItem.Serial,
												 p.ProductionOrderItem.Part.Code,
												 p.ProductionOrderItem.Part.Name,
												 p.ProductionOrderItem.ProductionOrder.Number,
												 p.ProductionOrderItem.ProductionOrder.Contract.Customer.Party.FullName,
											 })
											 .GroupBy(p => new { p.Code, p.Number })
											 .Select(s => s.FirstOrDefault())
											 .ToList();

				if (productionOrderItemBomList.Any())
				{
					hasProductionOrderItemRecord = true;

					content.Append("<br />");
					content.Append("<br />");
					content.Append("<strong style='color:red;text-align:center'>***توجه***</strong>");
					content.Append("<br />");
					content.Append("<br />");

					content.Append("همچنین کد تغییر کرده در BOM اقلام سفارش ساخت با جزییات ذیل نیز وجود دارد که بایستی توسط کاربر مهندسی مربوطه، تغییرات لازم در این قسمت نیز لحاظ گردد");
					content.Append("<br />");

					foreach (var productionOrderItemBom in productionOrderItemBomList)
					{
						content.Append($"ش سفارش ساخت : <strong>'{productionOrderItemBom.Number}'</strong> <br />");
						content.Append($"مشتری : <strong>'{productionOrderItemBom.FullName}'</strong> <br />");
						content.Append($"کد کالا : <strong>'{productionOrderItemBom.Code}'</strong> <br />");
						content.Append($"عنوان کالا : <strong>'{productionOrderItemBom.Name}'</strong> <br />");
						content.Append($"سریال : <strong>'{productionOrderItemBom.Serial}'</strong> <br />");
						content.Append("<br />");
					}
				}

				#endregion

				
			

			content.Append("</div>");
			content.Append("</td>");
			content.Append("</tr>");
			content.Append("</table>");
			content.Append("</div>");

			return content.ToString();

		}
	}
}
