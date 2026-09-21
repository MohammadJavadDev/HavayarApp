using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Entities.App.Inv;
using Entities.App.Sale;
using Entities.App.Sale.DTO;
using Entities.App.Sale.Enums;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using WebApp.Actions.Sale;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Sale/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("قطعه درخواست پشتیبانی", typeof(ServiceRequestPart))]
	public class ServiceRequestPartController(
		IUnitOfWork unitOfWork,
		IWebHostEnvironment env,
		IConfiguration configuration) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(ServiceRequestPartSaveRequest model, CancellationToken cn)
		{
			if (await ValidateRelatedProduct(model, cn) is { } invalid)
				return invalid;
			if (model.Id == null || model.Id == 0)
				return await Add(model, cn);
			if (await unitOfWork.Repository<ServiceRequestPart>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(ServiceRequestPartSaveRequest model, CancellationToken cn)
		{
			var pieces = model.Pieces ?? [];
			if (pieces.Count == 0)
			{
				var saved = await unitOfWork.Repository<ServiceRequestPart>().SaveAsync(ToEntity(model), cn, true);
				return Ok(saved);
			}

			ServiceRequestPart? last = null;
			foreach (var piece in pieces)
			{
				var entity = ToEntity(model);
				entity.Id = null;
				ApplyPiece(entity, piece, model);
				last = await unitOfWork.Repository<ServiceRequestPart>().SaveAsync(entity, cn, true);
			}
			return Ok(last);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(ServiceRequestPartSaveRequest model, CancellationToken cn)
		{
			var existing = await unitOfWork.Repository<ServiceRequestPart>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == model.Id, cn);
			if (existing == null)
				return BadRequest("ردیف یافت نشد");
			var entity = ToEntity(model);
			entity.HtsId = existing.HtsId;
			entity.IsRegisteredManually = existing.IsRegisteredManually;
			if (model.Pieces is { Count: 1 })
				ApplyPiece(entity, model.Pieces[0], model);
			var saved = await unitOfWork.Repository<ServiceRequestPart>().UpdateAsync(entity, cn, true);
			return Ok(saved);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<ServiceRequestPart>().TableNoTracking.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null)
				return Ok();
			if (!entity.IsRegisteredManually && entity.CreatedById != CurrentUserId && !IsAdministrator)
				return BadRequest("حذف این ردیف فقط برای ایجادکننده مجاز است");
			await unitOfWork.Repository<ServiceRequestPart>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public async Task<IActionResult> Edit(long? id, long? serviceRequestId = null, long? serviceRequestDetailId = null, string? manual = null, CancellationToken cn = default)
		{
			var isManual = IsManualFlag(manual);
			await FillFormBags(serviceRequestId ?? 0, isManual, cn);
			if (id != null && id != 0)
			{
				var entity = await unitOfWork.Repository<ServiceRequestPart>().TableNoTracking.FirstOrDefaultAsync(c => c.Id == id, cn);
				if (entity != null)
					isManual = entity.IsRegisteredManually;
				ViewBag.IsManual = isManual;
				return View(@"\Views\Panel\Sale\ServiceRequestPart\Edit.cshtml", entity ?? new ServiceRequestPart { ServiceRequestDetailId = serviceRequestDetailId, IsRegisteredManually = isManual });
			}
			return View(@"\Views\Panel\Sale\ServiceRequestPart\Edit.cshtml", new ServiceRequestPart { ServiceRequestDetailId = serviceRequestDetailId, IsRegisteredManually = isManual });
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public async Task<IActionResult> New(long? serviceRequestId = null, long? serviceRequestDetailId = null, string? manual = null, CancellationToken cn = default)
		{
			var isManual = IsManualFlag(manual);
			await FillFormBags(serviceRequestId ?? 0, isManual, cn);
			return View(@"\Views\Panel\Sale\ServiceRequestPart\Edit.cshtml", new ServiceRequestPart { ServiceRequestDetailId = serviceRequestDetailId, IsRegisteredManually = isManual });
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Sale\ServiceRequestPart\List.cshtml");

		[HttpGet("[action]")]
		[ActionDisplayName("لیست وابسته", ActionAccessType.View, ActionAccessItemType.List)]
		public async Task<IActionResult> ListByParentId(long serviceRequestId, string? manual = null, CancellationToken cn = default)
		{
			ViewBag.ParentId = serviceRequestId;
			ViewBag.IsManual = IsManualFlag(manual);
			ViewBag.ShowPrice = CanShowPrice();
			ViewBag.CurrentUserId = CurrentUserId ?? 0;
			ViewBag.IsAdministrator = IsAdministrator;
			return View(@"\Views\Panel\Sale\ServiceRequestPart\ListByParentId.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت لیست وابسته", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetListByParentId(long? serviceRequestId, string? manual = null, CancellationToken cn = default)
		{
			if (serviceRequestId == null || serviceRequestId == 0)
				return BadRequest("شناسه والد خالی است");
			var isManual = IsManualFlag(manual);
			var showPrice = CanShowPrice();
			var items = await unitOfWork.Repository<ServiceRequestPart>().TableNoTracking
				.Where(c => c.ServiceRequestDetail != null
					&& c.ServiceRequestDetail.ServiceRequestId == serviceRequestId
					&& c.IsRegisteredManually == isManual)
				.Select(c => new
				{
					c.Id,
					c.CreatedById,
					ServiceRequestId = c.ServiceRequestDetail!.ServiceRequestId,
					CostCenterTitle = c.CostCenter != null ? c.CostCenter.Title : null,
					OtherCostCenter = c.OtherCostCenterParty != null ? c.OtherCostCenterParty.FullName : null,
					ServiceType = (int)c.ServiceType,
					ServiceTypeTitle = c.ServiceType.ToString(),
					c.ProjectVchNum,
					c.ProjectVchShamsiDate,
					PartCode = c.ReplacePart != null ? c.ReplacePart.Code : null,
					PartName = c.ReplacePart != null ? c.ReplacePart.Name : null,
					ProductCode = c.ServiceRequestDetail.OrderDetail != null && c.ServiceRequestDetail.OrderDetail.Part != null
						? c.ServiceRequestDetail.OrderDetail.Part.Code
						: (c.ServiceRequestDetail.OtherPart != null ? c.ServiceRequestDetail.OtherPart.Code : null),
					ProductName = c.ServiceRequestDetail.OrderDetail != null && c.ServiceRequestDetail.OrderDetail.Part != null
						? c.ServiceRequestDetail.OrderDetail.Part.Name
						: (c.ServiceRequestDetail.OtherPart != null ? c.ServiceRequestDetail.OtherPart.Name : null),
					Serial = c.ServiceRequestDetail.OrderDetailSerial != null
						? c.ServiceRequestDetail.OrderDetailSerial.Serial
						: c.ServiceRequestDetail.OtherPartSerial,
					c.Mount,
					UnitPrice = showPrice ? c.UnitPrice : (long?)null,
					ReplacePartName = c.ReplacePart != null ? c.ReplacePart.Code + " " + c.ReplacePart.Name : null,
					c.DamagedParts,
					c.DamagedPartDescription,
					c.HasFailure,
					c.FailureParts,
					c.FailureDescription,
					c.Comment
				})
				.ToListAsync(cn);

			var titled = items.Select(c => new
			{
				c.Id,
				c.CreatedById,
				c.ServiceRequestId,
				c.CostCenterTitle,
				c.OtherCostCenter,
				c.ServiceType,
				ServiceTypeTitle = EnumDisplay(c.ServiceType),
				c.ProjectVchNum,
				c.ProjectVchShamsiDate,
				c.PartCode,
				c.PartName,
				c.ProductCode,
				c.ProductName,
				c.Serial,
				c.Mount,
				c.UnitPrice,
				c.ReplacePartName,
				c.DamagedParts,
				c.DamagedPartDescription,
				c.HasFailure,
				c.FailureParts,
				c.FailureDescription,
				c.Comment
			});
			return Ok(titled);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("واکشی اقلام سند", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> FetchPieces(int vchNo, string year, long? saleCenterId, CancellationToken cn)
		{
			var result = await AfterSalesInventoryAdapter.FetchPiecesAsync(
				configuration.GetConnectionString("Hts"), vchNo, year, ResolveSaleCenterHtsId(saleCenterId), cn);
			if (!result.Success)
				return BadRequest(result.Message);
			await MapPieceParts(result.Items, cn);
			return Ok(result.Items.Select(ToPieceDto));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("اقلام ردیف", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> GetPiecesData(long serviceRequestPartId, CancellationToken cn)
		{
			var item = await unitOfWork.Repository<ServiceRequestPart>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == serviceRequestPartId, cn);
			if (item == null)
				return BadRequest("ردیف یافت نشد");

			var year = item.ProjectVchYear?.ToString() ?? "";
			var vchNo = item.ProjectVchNum ?? 0;
			long? saleCenterId = null;
			if (item.OrderDetailId is > 0)
			{
				saleCenterId = await unitOfWork.Repository<OrderDetail>().TableNoTracking
					.Where(c => c.Id == item.OrderDetailId)
					.Select(c => c.Sale_Order != null ? c.Sale_Order.BranchId : null)
					.FirstOrDefaultAsync(cn);
			}

			var result = await AfterSalesInventoryAdapter.FetchPiecesAsync(
				configuration.GetConnectionString("Hts"), vchNo, year, ResolveSaleCenterHtsId(saleCenterId), cn);
			if (!result.Success)
				return Ok(new { saleCenterId, year, selectedId = item.HtsProjectVchItemId ?? item.OrderDetailId, dataSource = Array.Empty<object>() });

			await MapPieceParts(result.Items, cn);
			long? orderDetailHtsId = null;
			if (item.OrderDetailId is > 0)
			{
				orderDetailHtsId = await unitOfWork.Repository<OrderDetail>().TableNoTracking
					.Where(c => c.Id == item.OrderDetailId)
					.Select(c => (long?)c.HtsId)
					.FirstOrDefaultAsync(cn);
			}
			var selectedId = item.HtsProjectVchItemId ?? orderDetailHtsId ?? item.OrderDetailId;
			return Ok(new
			{
				saleCenterId,
				year,
				selectedId,
				dataSource = result.Items.Select(ToPieceDto)
			});
		}

		[HttpGet("[action]")]
		[ActionDisplayName("محصولات مرتبط", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> GetRelatedDetails(long serviceRequestId, CancellationToken cn)
		{
			var rows = await unitOfWork.Repository<ServiceRequestDetail>().TableNoTracking
				.Where(c => c.ServiceRequestId == serviceRequestId)
				.Select(c => new
				{
					c.Id,
					PartCode = c.OrderDetailSerial != null && c.OrderDetailSerial.OrderDetail.Part != null ? c.OrderDetailSerial.OrderDetail.Part.Code : (c.OtherPart != null ? c.OtherPart.Code : null),
					PartName = c.OrderDetailSerial != null && c.OrderDetailSerial.OrderDetail.Part != null ? c.OrderDetailSerial.OrderDetail.Part.Name : (c.OtherPart != null ? c.OtherPart.Name : null),
					Serial = c.OrderDetailSerial != null ? c.OrderDetailSerial.Serial : c.OtherPartSerial,
					SendDate = c.OrderDetail != null && c.OrderDetail.Sale_Order != null ? c.OrderDetail.Sale_Order.VchDateShamsiDate : null
				})
				.ToListAsync(cn);
			return Ok(rows.Select(c => new
			{
				c.Id,
				c.PartCode,
				c.PartName,
				c.Serial,
				c.SendDate,
				text = (c.PartCode ?? "") + " | " + (c.PartName ?? "") + " | سریال :" + (c.Serial ?? "")
					+ (string.IsNullOrWhiteSpace(c.SendDate) ? "" : " | تاریخ ارسال :" + c.SendDate)
			}));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("سال و مرکز فروش", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> GetFormLookups(CancellationToken cn)
		{
			var branches = await unitOfWork.Repository<Branch>().TableNoTracking
				.Where(c => c.IsActive == Entities.Base.IsActiveEnum.Active)
				.OrderBy(c => c.Title)
				.Select(c => new { id = c.Id, text = c.Title, htsId = c.HtsId })
				.ToListAsync(cn);

			var now = DateTime.Now.ToShamsiDate();
			var currentPersian = 1404;
			if (!string.IsNullOrWhiteSpace(now) && now.Length >= 4 && int.TryParse(now[..4], out var py))
				currentPersian = py;
			var years = Enumerable.Range(0, 8)
				.Select(i => currentPersian - i)
				.Select(p => new { id = p.ToString(), text = p.ToString() })
				.ToList();
			return Ok(new { branches, years });
		}

		[HttpGet("[action]")]
		[ActionDisplayName("مشاهده قیمت", ActionAccessType.View, ActionAccessItemType.Custom)]
		public IActionResult ShowPrice() => Ok();

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<ServiceRequestPart>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex) { return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message); }
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<ServiceRequestPart>().FetchDataAsync(request, cn));

		private static bool IsManualFlag(string? value)
			=> value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

		private async Task FillFormBags(long serviceRequestId, bool manual, CancellationToken cn)
		{
			ViewBag.ServiceRequestId = serviceRequestId;
			ViewBag.IsManual = manual;
			ViewBag.ShowPrice = CanShowPrice();
			await Task.CompletedTask;
		}

		private async Task<IActionResult?> ValidateRelatedProduct(ServiceRequestPartSaveRequest model, CancellationToken cn)
		{
			if (model.ServiceRequestDetailId is null or 0)
				return BadRequest("محصول مرتبط را از محصولات همین درخواست پشتیبانی انتخاب کنید");
			var query = unitOfWork.Repository<ServiceRequestDetail>().TableNoTracking
				.Where(c => c.Id == model.ServiceRequestDetailId);
			if (model.ServiceRequestId is > 0)
				query = query.Where(c => c.ServiceRequestId == model.ServiceRequestId);
			if (!await query.AnyAsync(cn))
				return BadRequest("محصول مرتبط باید از محصولات همین درخواست پشتیبانی باشد");
			return null;
		}

		private bool CanShowPrice()
		{
			if (IsAdministrator)
				return true;
			return sdk.CurrentUser?.RoleAccess?.Any(c =>
				c.Path != null && c.Path.Equals("/panel/sale/servicerequestpart/showprice", StringComparison.OrdinalIgnoreCase)) == true;
		}

		private long? ResolveSaleCenterHtsId(long? saleCenterId)
		{
			if (saleCenterId is null or 0)
				return null;
			var hts = unitOfWork.Repository<Branch>().TableNoTracking
				.Where(c => c.Id == saleCenterId)
				.Select(c => c.HtsId)
				.FirstOrDefault();
			return hts > 0 ? hts : saleCenterId;
		}

		private async Task MapPieceParts(List<InventoryPieceRow> items, CancellationToken cn)
		{
			var partRefs = items.Where(c => c.Partref != null).Select(c => c.Partref!.Value).Distinct().ToList();
			if (partRefs.Count == 0)
				return;
			var parts = await unitOfWork.Repository<Part>().TableNoTracking
				.Where(c => partRefs.Contains(c.HtsId) || (c.HamkaranId != null && partRefs.Contains(c.HamkaranId.Value)))
				.Select(c => new { c.Id, c.HtsId, c.HamkaranId })
				.ToListAsync(cn);
			var orderIds = items.Where(c => c.FromSaleOrder).Select(c => c.Id).Distinct().ToList();
			var orders = orderIds.Count == 0
				? []
				: await unitOfWork.Repository<OrderDetail>().TableNoTracking
					.Where(c => orderIds.Contains(c.HtsId) || orderIds.Contains(c.Id ?? 0))
					.Select(c => new { c.Id, c.HtsId })
					.ToListAsync(cn);

			foreach (var item in items)
			{
				if (item.Partref != null)
				{
					item.HavayarPartId = parts.FirstOrDefault(p => p.HtsId == item.Partref)?.Id
						?? parts.FirstOrDefault(p => p.HamkaranId == item.Partref)?.Id;
				}
				if (item.FromSaleOrder)
					item.HavayarOrderDetailId = orders.FirstOrDefault(o => o.HtsId == item.Id || o.Id == item.Id)?.Id;
			}
		}

		private static ServiceRequestPartPieceDto ToPieceDto(InventoryPieceRow row) => new()
		{
			Id = row.Id,
			Date = row.Date,
			ProductTitle = row.ProductTitle,
			Value = row.Value,
			Partref = row.Partref,
			MunitRef = row.MunitRef,
			LastBuyPrice = row.LastBuyPrice,
			HavayarPartId = row.HavayarPartId,
			HavayarOrderDetailId = row.HavayarOrderDetailId,
			FromSaleOrder = row.FromSaleOrder
		};

		private static ServiceRequestPart ToEntity(ServiceRequestPartSaveRequest model) => new()
		{
			Id = model.Id,
			ServiceRequestDetailId = model.ServiceRequestDetailId,
			OrderDetailId = model.OrderDetailId,
			ReplacePartId = model.ReplacePartId,
			ServiceType = model.ServiceType,
			Mount = model.Mount,
			UnitPrice = model.UnitPrice,
			IsRegisteredManually = model.IsRegisteredManually,
			HtsProjectVchItemId = model.HtsProjectVchItemId,
			HtsProjectVchPartId = model.HtsProjectVchPartId,
			ProjectVchNum = model.ProjectVchNum,
			ProjectVchYear = model.ProjectVchYear,
			ProjectVchShamsiDate = model.ProjectVchShamsiDate,
			ProjectVchMiladiDate = model.ProjectVchMiladiDate,
			PartUnitId = model.PartUnitId,
			CostCenterId = model.CostCenterId,
			OtherCostCenterPartyId = model.OtherCostCenterPartyId,
			ReturnLicence = model.ReturnLicence,
			ReturnDamagedPart = model.ReturnDamagedPart,
			DamagedPartType = model.DamagedPartType,
			DamagedPartIds = model.DamagedPartIds,
			DamagedParts = model.DamagedParts,
			DamagedPartDescription = model.DamagedPartDescription,
			HasFailure = model.HasFailure,
			FailurePartIds = model.FailurePartIds,
			FailureParts = model.FailureParts,
			FailureDescription = model.FailureDescription,
			Comment = model.Comment
		};

		private static void ApplyPiece(ServiceRequestPart entity, ServiceRequestPartPieceDto piece, ServiceRequestPartSaveRequest model)
		{
			if (!string.IsNullOrWhiteSpace(piece.Date))
			{
				entity.ProjectVchShamsiDate = piece.Date;
				try { entity.ProjectVchMiladiDate = piece.Date.ToMiladiDate(); } catch { /* keep null */ }
			}
			if (piece.Value is > 0 && (model.Pieces?.Count ?? 0) > 1)
				entity.Mount = piece.Value.Value;
			if (piece.LastBuyPrice is > 0)
				entity.UnitPrice = (long)Math.Round(entity.Mount * piece.LastBuyPrice.Value);
			else if (entity.Id is null or 0 && entity.UnitPrice is > 0)
				entity.UnitPrice = (long)Math.Round(entity.Mount * entity.UnitPrice.Value);

			if (piece.HavayarPartId is > 0 && entity.ReplacePartId is null or 0)
				entity.ReplacePartId = piece.HavayarPartId;
			if (piece.Partref is > 0 && entity.HtsProjectVchPartId is null)
				entity.HtsProjectVchPartId = piece.Partref;

			if (piece.FromSaleOrder)
			{
				entity.OrderDetailId = piece.HavayarOrderDetailId ?? piece.Id;
			}
			else if (piece.Id > 0 && piece.Id <= int.MaxValue)
			{
				entity.HtsProjectVchItemId = (int)piece.Id;
			}
		}

		private static string EnumDisplay(int serviceType)
		{
			if (!Enum.IsDefined(typeof(AfterSalesServiceTypeEnum), serviceType))
				return serviceType.ToString();
			var value = (AfterSalesServiceTypeEnum)serviceType;
			var attr = value.GetType().GetField(value.ToString())
				?.GetCustomAttributes(typeof(DisplayAttribute), false)
				.OfType<DisplayAttribute>()
				.FirstOrDefault();
			return attr?.Name ?? value.ToString();
		}
	}
}
