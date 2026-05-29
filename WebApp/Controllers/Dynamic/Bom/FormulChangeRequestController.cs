using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data;
using Data.Contracts;
using Data.SystemAuth;
using Entities.App.Bom;
using Entities.App.Bom.Enums;
using Entities.App.Bom.Views;
using Entities.App.Inv;
using Entities.App.Sale;
using Entities.App.Sale.Enums;
using Entities.Base;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUglify.Helpers;
using Stimulsoft.System.Windows.Forms;
using System.Text;
using WebFramework.Filtters;
using WebFramework.Page;
using static Stimulsoft.Report.StiRecentConnections;

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
			var isProductGroupModel = entity.ChangedProductGroupIds.HasValue()
									&& entity.ChangedProductGroupIds.Length > 0;

			List<long> productFormulIds;
			List<long?> formulaIds;
			List<FormulItem> formulItems = new();
			long?[] mainProductFormulIds;

			if (isProductGroupModel)
			{
				var productGroupIds = entity.ChangedProductGroupIds.Split(",");

				mainProductFormulIds = unitOfWork.Repository<ProductFormul>().
						TableNoTracking
						.Where(p => p.ProductNameGroupId == productGroupIds[0].ToLong())
						.Select(p => p.Id)
						 .ToArray();

				if (!mainProductFormulIds.Any())
					return;


				formulaIds = unitOfWork.Repository<ProductFormulItem>().
						TableNoTracking
				    .Where(p => mainProductFormulIds.Contains(p.ProductFormulId)
					&& p.FormulId.HasValue)
				    .Select(p => p.FormulId)
				    .ToList();

				if (!formulaIds.Any())
					return;

				formulItems = formulaIds.Select(id => new FormulItem
				{
					PartId = entity.PartId,
					FormulId = id.Value,
					UsingRate = 1,
				}).ToList();


			}
			else
			{
				if (!entity.ChangedFormulIds.HasValue())
				{
					throw new Exception("هیچ فرمول قطعه یا گروه محصولی برای ویرایش انتخاب نشده است.");
				}

				formulItems = entity.ChangedFormulIds.Split(",").Select(x => new FormulItem
				{
					PartId = entity.PartId,
					FormulId = x.ToLong(),
					UsingRate = entity.ChangedUsingRate
				}).ToList();
			}

			if (entity.OperationType == FormulChangeRequestOperationTypeEnum.Add)
			{

				 foreach(var item in formulItems)
				{
					var originalItem = unitOfWork.Repository<FormulItem>().
						Table
						.FirstOrDefault(p => p.PartId == item.PartId && p.FormulId == item.FormulId);
					//
					if (originalItem != null)
					{

						originalItem.SourcePath = "BomChangeRequestController => AddFormulItems => Update";
						originalItem.UsingRate = entity.ChangedUsingRate;

						 

					}
					else
					{
						item.UsingRate = entity.ChangedUsingRate;

						item.SourcePath = "BomChangeRequestController => AddFormulItems => Create";

						 

					}
				};
				 await unitOfWork.SaveChangesAsync(cn);


			}
			else if (entity.OperationType == FormulChangeRequestOperationTypeEnum.Replacement)
			{

				if (isProductGroupModel)
				{

					var productGroupIds = entity.ChangedProductGroupIds.Split(",");
					var message = "";
					var updateResult = new List<FormulItem>();
					 foreach(var pgi in productGroupIds)
					{

						mainProductFormulIds = unitOfWork.Repository<ProductFormul>().
						TableNoTracking
						.Where(p => p.ProductNameGroupId == pgi.ToLong())
						.Select(p => p.Id)
						 .ToArray();


						productFormulIds = _context.vw_ProductItems
						    .Where(p => p.PartItemId == entity.ReplacementPartId)
						    .Select(p => p.ProductFormulId)
						    .AsEnumerable()
						    .Where(p => mainProductFormulIds.Contains(p))
						    .ToList();

						if (!productFormulIds.Any())
							return;

						formulaIds = unitOfWork.Repository<ProductFormulItem>()
							.TableNoTracking
							.Where(p => productFormulIds.Contains(p.ProductFormulId) && p.FormulId.HasValue)
							.Select(p => p.FormulId)
							.ToList();


						if (!formulaIds.Any())
							return;

						var formulaItems = unitOfWork.Repository<FormulItem>().
							 TableNoTracking
							 .Where(p => formulaIds.Contains(p.FormulId) && p.PartId == entity.PartId)
							 .Include(p => p.PartId)
							 .ToList();

						if (!formulaItems.Any())
							return;

						// اگر تعداد 0 بود یعنی فقط جایگزین شوند
						if (!entity.ChangedUsingRate.HasValue || entity.ChangedUsingRate == 0)
						{
							formulaItems.ForEach(item =>
							{

								item.SourcePath = "BomChangeRequestController => UpdateFormulaItems => Update";
								item.PartId = entity.ReplacementPartId;
								item.PartId = null;

							});

							updateResult = await unitOfWork.Repository<FormulItem>().
						    UpdateRangeAsync(formulaItems, cn);

							return;
						}
						else
						{


							formulaItems = formulaItems
							    .GroupBy(p => p.FormulId)
							    .SelectMany(p => p.Take(entity.ChangedUsingRate.Value))
							    .ToList();

							if (!formulaItems.Any())
								return;

							if (formulaItems.Any(p => p.PartId == entity.ReplacementPartId))
							{
								message += string.Format("این {0} قبلا تعریف شده است", formulaItems.First(p => p.PartId == entity.ReplacementPartId).Part.Code) + "<br/>";
								return;
							}

							formulaItems.ForEach(item =>
							{

								item.SourcePath = "BomChangeRequestController => UpdateFormulaItems => Update";
								item.PartId = entity.ReplacementPartId;
								item.Part = null;

							});

							updateResult = await unitOfWork.Repository<FormulItem>().
													   UpdateRangeAsync(formulaItems, cn);

						}

					};

				}

			}

			else if (entity.OperationType == FormulChangeRequestOperationTypeEnum.Deleted)
			{
				var partId = formulItems.First().PartId;
				var formulIds = formulItems.Select(p => p.FormulId).ToArray();

				await unitOfWork.Repository<FormulItem>()
							.TableNoTracking
							 .Where(p => p.PartId == partId && formulIds.Contains(p.FormulId))
							 .ExecuteDeleteAsync();
			}

			#region ارسال ایمیل اطلاع رسانی نهایی

			SendChangePartNotificationEmail(entity);

			#endregion


			 await UpdateDataSheets(entity.PartId.Value, entity.ReplacementPartId,cn);

			 

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
				partExtraInfo.Revision += 1;

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
			else if (item.ChangedFormulNames.HasValue()) 
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
