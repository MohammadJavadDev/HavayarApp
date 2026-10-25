using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Entities.App.Hcm;
using Entities.App.Qc;
using Entities.App.Sale;
using Entities.Base;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Qc;
using System.Text.Json;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Qc/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("بازرسی محصول در هوایار", typeof(ProductInspection))]
	public class ProductInspectionController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		private static readonly JsonSerializerOptions JsonOptions = new()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(ProductInspection model, CancellationToken cn)
		{
			var failures = model.Failures?.ToList() ?? [];
			var parameters = model.MeasuredParameters?.ToList() ?? [];
			model.Failures = [];
			model.MeasuredParameters = [];
			model.Attachments = [];

			var error = await ValidateAsync(model, failures, parameters, cn);
			if (error != null)
				return BadRequest(error);

			var isNew = model.Id is null or 0;
			long id;
			if (isNew)
			{
				model.Id = null;
				model.HtsId = 0;
				var saved = await unitOfWork.Repository<ProductInspection>().SaveAsync(model, cn, true);
				id = saved.Id!.Value;
			}
			else
			{
				var existing = await unitOfWork.Repository<ProductInspection>().Table
					.FirstOrDefaultAsync(c => c.Id == model.Id, cn);
				if (existing == null)
					return NotFound();

				existing.ProductionOrderNumber = model.ProductionOrderNumber;
				existing.ProductionOrderItemId = model.ProductionOrderItemId;
				existing.SalesDepartmentId = model.SalesDepartmentId;
				existing.ProjectTitle = model.ProjectTitle;
				existing.ManualSerial = model.ManualSerial;
				existing.Contractor = model.Contractor;
				existing.TestStartShamsiDate = model.TestStartShamsiDate;
				existing.TestStartMiladiDate = model.TestStartMiladiDate;
				existing.TestStartTime = model.TestStartTime;
				existing.TestEndShamsiDate = model.TestEndShamsiDate;
				existing.TestEndMiladiDate = model.TestEndMiladiDate;
				existing.TestEndTime = model.TestEndTime;
				existing.TestPersonelId = model.TestPersonelId;
				existing.InspectionResult = model.InspectionResult;
				existing.IsCompleted = model.IsCompleted;
				existing.Comment = model.Comment;
				var now = DateTime.Now;
				existing.ModifiedById = CurrentUserId;
				existing.ModifiedByName = CurrentUserFullName;
				existing.ModifiedDateMiladiDateTime = now;
				existing.ModifiedDateShamsiDateTime = now.ToShamsiDateTime();
				await unitOfWork.SaveChangesAsync(cn);
				id = existing.Id!.Value;

				await unitOfWork.Repository<ProductInspectionFailure>().Table
					.Where(f => f.ProductInspectionId == id)
					.ExecuteDeleteAsync(cn);
				await unitOfWork.Repository<ProductInspectionMeasuredParameter>().Table
					.Where(f => f.ProductInspectionId == id)
					.ExecuteDeleteAsync(cn);
			}

			foreach (var row in failures)
			{
				row.Id = null;
				row.HtsId = 0;
				row.ProductInspection = null;
				row.FailureType = null;
				row.ProductionOrderItemComment = null;
				row.ProductInspectionId = id;
				row.FailureStartMiladiDate ??= TryMiladi(row.FailureStartShamsiDate);
				row.FailureEndMiladiDate ??= TryMiladi(row.FailureEndShamsiDate);
				await unitOfWork.Repository<ProductInspectionFailure>().AddAsync(row, cn, saveNow: false);
			}

			foreach (var row in parameters)
			{
				row.Id = null;
				row.HtsId = 0;
				row.ProductInspection = null;
				row.MeasurementParameterItem = null;
				row.MeasurementParameterSubItem = null;
				row.ProductInspectionId = id;
				if (model.InspectionType == ProductInspectionConstants.InCompany)
				{
					row.MeasurementStartShamsiDate = model.TestStartShamsiDate;
					row.MeasurementStartMiladiDate = model.TestStartMiladiDate;
					row.MeasurementStartTime = model.TestStartTime;
					row.MeasurementEndShamsiDate = model.TestEndShamsiDate;
					row.MeasurementEndMiladiDate = model.TestEndMiladiDate;
					row.MeasurementEndTime = model.TestEndTime;
				}
				else
				{
					row.MeasurementStartMiladiDate ??= TryMiladi(row.MeasurementStartShamsiDate);
					row.MeasurementEndMiladiDate ??= TryMiladi(row.MeasurementEndShamsiDate);
				}
				await unitOfWork.Repository<ProductInspectionMeasuredParameter>().AddAsync(row, cn, saveNow: false);
			}

			if (failures.Count > 0 || parameters.Count > 0)
				await unitOfWork.SaveChangesAsync(cn);

			if (model.InspectionType == ProductInspectionConstants.InCompany)
			{
				await unitOfWork.Repository<ProductionOrderItem>()
					.UpdateFieldsAsync(model.ProductionOrderItemId, c => c.HasInspection, true, cn);

				if (isNew)
				{
					try
					{
						await ProductInspectionFailureSync.AddFromProductionOrderItemCommentsAsync(unitOfWork, cn);
					}
					catch
					{
						// درج بازرسی نباید به‌خاطر همگام‌سازی خرابی کامنت برگردد؛ جاب دوره‌ای دوباره تلاش می‌کند.
					}
				}
			}

			return Ok(await LoadFormAsync(id, cn));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public Task<IActionResult> Add(ProductInspection model, CancellationToken cn) => Save(model, cn);

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public Task<IActionResult> Update(ProductInspection model, CancellationToken cn) => Save(model, cn);

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<ProductInspection>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null)
				return Ok();

			if (entity.InspectionType == ProductInspectionConstants.OutCompany)
				return BadRequest("حذف بازرسی محل پیمانکار مجاز نیست");

			var itemId = entity.ProductionOrderItemId;
			await unitOfWork.Repository<ProductInspection>().DeleteAsync(entity, cn, true);

			var stillHas = await unitOfWork.Repository<ProductInspection>().TableNoTracking
				.AnyAsync(c => c.ProductionOrderItemId == itemId && c.InspectionType == ProductInspectionConstants.InCompany, cn);
			if (!stillHas)
			{
				await unitOfWork.Repository<ProductionOrderItem>()
					.UpdateFieldsAsync(itemId, c => c.HasInspection, false, cn);
			}

			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public async Task<IActionResult> Edit(long? id, long? copyFrom, CancellationToken cn)
		{
			var sourceId = id is > 0 ? id : copyFrom;
			if (sourceId is > 0)
			{
				var entity = await unitOfWork.Repository<ProductInspection>().TableNoTracking
					.Include(c => c.ProductionOrderItem)
					.FirstOrDefaultAsync(c => c.Id == sourceId, cn);
				if (entity == null || !CanOpenType(entity.InspectionType))
					return NotFound();

				var copying = id is null or 0;
				if (copying)
				{
					entity.Id = null;
					entity.HtsId = 0;
				}

				entity.ProductionOrderId = entity.ProductionOrderItem?.ProductionOrderId;
				SetTypeFlags(locked: true);
				await FillLookupsAsync(entity.TestPersonelId, cn);
				ViewBag.FailuresJson = JsonSerializer.Serialize(await LoadFailuresAsync(sourceId.Value, copying, cn), JsonOptions);
				ViewBag.ParametersJson = JsonSerializer.Serialize(await LoadParametersAsync(sourceId.Value, copying, cn), JsonOptions);
				return View(@"\Views\Panel\Qc\ProductInspection\Edit.cshtml", entity);
			}

			await FillLookupsAsync(null, cn);
			ViewBag.FailuresJson = "[]";
			ViewBag.ParametersJson = "[]";
			var canIn = CanCreateInCompany();
			var canOut = CanCreateOutCompany();
			SetTypeFlags(locked: !(canIn && canOut));
			return View(@"\Views\Panel\Qc\ProductInspection\Edit.cshtml", new ProductInspection
			{
				InspectionType = canIn || !canOut
					? ProductInspectionConstants.InCompany
					: ProductInspectionConstants.OutCompany
			});
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public Task<IActionResult> New(CancellationToken cn) => Edit(null, null, cn);

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Qc\ProductInspection\List.cshtml");

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<ProductInspection>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message);
			}
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
		{
			return Ok(await unitOfWork.Repository<ProductInspection>().FetchDataAsync(request, cn));
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> GetProductionOrderItems(long productionOrderId, CancellationToken cn)
		{
			var items = await unitOfWork.Repository<ProductionOrderItem>().TableNoTracking
				.Where(p => p.ProductionOrderId == productionOrderId
					&& p.IsLatestVersion
					&& !p.IsDeleted
					&& p.Serial != null
					&& !p.Serial.Contains("."))
				.OrderBy(p => p.Serial)
				.Select(p => new
				{
					id = p.Id,
					text = p.Serial + " | " + (p.Part != null ? p.Part.Code : "") + " | " + (p.Part != null ? p.Part.Name : "")
				})
				.ToListAsync(cn);
			return Ok(items);
		}

		private async Task<string?> ValidateAsync(
			ProductInspection model,
			List<ProductInspectionFailure> failures,
			List<ProductInspectionMeasuredParameter> parameters,
			CancellationToken cn)
		{
			if (model.ProductionOrderItemId <= 0)
				return "قلم سفارش ساخت نمی تواند خالی باشد";
			if (!model.TestStartShamsiDate.HasValue())
				return "تاریخ شروع تست نمی تواند خالی باشد";
			if (!model.TestStartTime.HasValue())
				return "ساعت شروع تست نمی تواند خالی باشد";
			if (!model.TestEndShamsiDate.HasValue())
				return "تاریخ پایان تست نمی تواند خالی باشد";
			if (!model.TestEndTime.HasValue())
				return "ساعت پایان تست نمی تواند خالی باشد";
			if (model.TestPersonelId <= 0)
				return "بازرس نمی تواند خالی باشد";

			if (model.Id is null or 0)
			{
				if (model.InspectionType != ProductInspectionConstants.InCompany
					&& model.InspectionType != ProductInspectionConstants.OutCompany)
					model.InspectionType = ProductInspectionConstants.InCompany;
			}
			else
			{
				var savedType = await unitOfWork.Repository<ProductInspection>().TableNoTracking
					.Where(p => p.Id == model.Id)
					.Select(p => (short?)p.InspectionType)
					.FirstOrDefaultAsync(cn);
				if (savedType == null)
					return "بازرسی یافت نشد";
				model.InspectionType = savedType.Value;
			}

			if (model.InspectionType == ProductInspectionConstants.OutCompany)
			{
				if (!CanCreateOutCompany())
					return "ثبت بازرسی محل پیمانکار برای شما مجاز نیست";
			}
			else if (!CanCreateInCompany())
			{
				return "ثبت بازرسی در هوایار برای شما مجاز نیست";
			}

			if (model.InspectionType == ProductInspectionConstants.InCompany)
			{
				model.Contractor = null;
				model.ManualSerial = null;
			}
			else
			{
				model.Contractor = model.Contractor.HasValue() ? model.Contractor!.Trim() : null;
				model.ManualSerial = model.ManualSerial.HasValue() ? model.ManualSerial!.Trim() : null;
			}

			if (parameters.Count == 0 && !model.ConfirmEmptyMeasurement)
				return "پارامتر اندازه گیری نمی تواند خالی باشد";

			model.TestStartMiladiDate ??= TryMiladi(model.TestStartShamsiDate);
			model.TestEndMiladiDate ??= TryMiladi(model.TestEndShamsiDate);
			if (model.TestStartMiladiDate == null || model.TestEndMiladiDate == null)
				return "تاریخ تست نامعتبر است";

			var item = await unitOfWork.Repository<ProductionOrderItem>().TableNoTracking
				.Include(p => p.ProductionOrder)
				.FirstOrDefaultAsync(p => p.Id == model.ProductionOrderItemId, cn);
			if (item == null || item.IsDeleted || !item.IsLatestVersion || !item.Serial.HasValue() || item.Serial!.Contains('.'))
				return "قلم سفارش ساخت نامعتبر است";
			if (model.ProductionOrderId is > 0 && item.ProductionOrderId != model.ProductionOrderId)
				return "قلم سفارش ساخت با شماره سفارش ساخت انتخاب‌شده یکی نیست";

			model.ProductionOrderNumber = ParseOrderNumber(item.ProductionOrder?.Number, item.ProductionOrder?.ProductionOrderNumber);
			if (model.ProductionOrderNumber <= 0)
				return "شماره سفارش ساخت نمی تواند خالی باشد";

			var person = await unitOfWork.Repository<Personel>().TableNoTracking
				.FirstOrDefaultAsync(p => p.Id == model.TestPersonelId, cn);
			if (person == null)
				return "بازرس نمی تواند خالی باشد";

			var allowed = ProductInspectionConstants.TestPersonelCodes.Contains(person.Code);
			if (!allowed)
			{
				var sameAsSaved = model.Id is > 0 && await unitOfWork.Repository<ProductInspection>().TableNoTracking
					.AnyAsync(p => p.Id == model.Id && p.TestPersonelId == model.TestPersonelId, cn);
				if (!sameAsSaved)
					return "بازرس انتخاب‌شده مجاز نیست";
			}

			foreach (var row in failures)
			{
				if (row.FailureTypeId <= 0)
					return "عنوان خرابی نمی تواند خالی باشد";
				if (!row.FailureStartShamsiDate.HasValue())
					return "تاریخ شروع گزارش خرابی نمی تواند خالی باشد";
				if (!row.FailureEndShamsiDate.HasValue())
					return "تاریخ پایان گزارش خرابی نمی تواند خالی باشد";
			}

			foreach (var row in parameters)
			{
				if (row.MeasurementParameterItemId <= 0)
					return "پارامتر اندازه گیری نمی تواند خالی باشد";
				if (model.InspectionType != ProductInspectionConstants.OutCompany)
					continue;
				if (!row.MeasurementStartShamsiDate.HasValue())
					return "تاریخ شروع اندازه گیری نمی تواند خالی باشد";
				if (!row.MeasurementStartTime.HasValue())
					return "ساعت شروع اندازه گیری نمی تواند خالی باشد";
				if (!row.MeasurementEndShamsiDate.HasValue())
					return "تاریخ پایان اندازه گیری نمی تواند خالی باشد";
				if (!row.MeasurementEndTime.HasValue())
					return "ساعت خاتمه اندازه گیری نمی تواند خالی باشد";
				row.MeasurementStartMiladiDate ??= TryMiladi(row.MeasurementStartShamsiDate);
				row.MeasurementEndMiladiDate ??= TryMiladi(row.MeasurementEndShamsiDate);
				if (row.MeasurementStartMiladiDate == null || row.MeasurementEndMiladiDate == null)
					return "تاریخ اندازه گیری نامعتبر است";
			}

			return null;
		}

		private bool CanCreateInCompany() =>
			IsAdministrator || CurrentUserHasAnyRole(
				"ShowAllMenus",
				"Qc.ProductInspection.Experts",
				"Qc.ProductInspection.SystemExperts",
				"Qc.ProductInspection.EngineeringExperts");

		private bool CanCreateOutCompany() => CanCreateInCompany();

		private bool CanOpenType(short inspectionType) =>
			inspectionType == ProductInspectionConstants.OutCompany
				? CanCreateOutCompany()
				: CanCreateInCompany();

		private void SetTypeFlags(bool locked)
		{
			ViewBag.CanInCompany = CanCreateInCompany();
			ViewBag.CanOutCompany = CanCreateOutCompany();
			ViewBag.LockInspectionType = locked;
		}

		private async Task FillLookupsAsync(long? currentPersonelId, CancellationToken cn)
		{
			var codes = ProductInspectionConstants.TestPersonelCodes;
			var persons = await unitOfWork.Repository<Personel>().TableNoTracking
				.Where(p => (p.Code != null && codes.Contains(p.Code)) || (currentPersonelId != null && p.Id == currentPersonelId))
				.OrderBy(p => p.Family)
				.ThenBy(p => p.Name)
				.Select(p => new { p.Id, Name = ((p.Name ?? "") + " " + (p.Family ?? "")).Trim() })
				.ToListAsync(cn);

			var groups = await unitOfWork.Repository<MeasurementParameter>().TableNoTracking
				.Where(p => p.IsActive != IsActiveEnum.Deleted)
				.OrderBy(p => p.Id)
				.Select(p => new { p.Id, p.Title })
				.ToListAsync(cn);

			var items = await unitOfWork.Repository<MeasurementParameterItem>().TableNoTracking
				.Where(p => p.IsActive != IsActiveEnum.Deleted)
				.OrderBy(p => p.Id)
				.Select(p => new { p.Id, p.Title, p.MeasurementParameterId, p.CommonSymbol })
				.ToListAsync(cn);

			var subItems = await unitOfWork.Repository<MeasurementParameterSubItem>().TableNoTracking
				.OrderBy(p => p.Id)
				.Select(p => new { p.Id, p.Title, p.MeasurementParameterItemId })
				.ToListAsync(cn);

			var failureTypes = await unitOfWork.Repository<FailureType>().TableNoTracking
				.Where(p => p.IsActive != IsActiveEnum.Deleted)
				.OrderBy(p => p.Title)
				.Select(p => new { p.Id, p.Title })
				.ToListAsync(cn);

			ViewBag.PersonsJson = JsonSerializer.Serialize(persons, JsonOptions);
			ViewBag.LookupsJson = JsonSerializer.Serialize(new { groups, items, subItems, failureTypes }, JsonOptions);
		}

		private async Task<object> LoadFormAsync(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<ProductInspection>().TableNoTracking
				.Include(c => c.ProductionOrderItem)
				.FirstAsync(c => c.Id == id, cn);

			return new
			{
				id = entity.Id,
				inspectionType = entity.InspectionType,
				productionOrderId = entity.ProductionOrderItem?.ProductionOrderId,
				productionOrderItemId = entity.ProductionOrderItemId,
				productionOrderNumber = entity.ProductionOrderNumber,
				manualSerial = entity.ManualSerial,
				salesDepartmentId = entity.SalesDepartmentId,
				projectTitle = entity.ProjectTitle,
				contractor = entity.Contractor,
				testStartShamsiDate = entity.TestStartShamsiDate,
				testStartTime = entity.TestStartTime,
				testEndShamsiDate = entity.TestEndShamsiDate,
				testEndTime = entity.TestEndTime,
				testPersonelId = entity.TestPersonelId,
				inspectionResult = entity.InspectionResult,
				isCompleted = entity.IsCompleted,
				comment = entity.Comment,
				createdByName = entity.CreatedByName,
				createdOnShamsiDateTime = entity.CreatedOnShamsiDateTime,
				modifiedByName = entity.ModifiedByName,
				modifiedDateShamsiDateTime = entity.ModifiedDateShamsiDateTime,
				failures = await LoadFailuresAsync(id, false, cn),
				measuredParameters = await LoadParametersAsync(id, false, cn)
			};
		}

		private async Task<List<object>> LoadFailuresAsync(long inspectionId, bool copying, CancellationToken cn)
		{
			var rows = await unitOfWork.Repository<ProductInspectionFailure>().TableNoTracking
				.Where(f => f.ProductInspectionId == inspectionId)
				.OrderBy(f => f.Id)
				.Select(f => new
				{
					f.Id,
					f.FailureTypeId,
					FailureTypeTitle = f.FailureType != null ? f.FailureType.Title : "",
					f.ProductionOrderItemCommentId,
					f.FailureStartShamsiDate,
					f.FailureStartTime,
					f.FailureEndShamsiDate,
					f.FailureEndTime,
					f.Comment,
					f.CreatedByName,
					f.CreatedOnShamsiDateTime
				})
				.ToListAsync(cn);

			return rows.Select(f => (object)new
			{
				id = copying ? (long?)null : f.Id,
				failureTypeId = f.FailureTypeId,
				failureTypeTitle = f.FailureTypeTitle,
				productionOrderItemCommentId = copying ? null : f.ProductionOrderItemCommentId,
				failureStartShamsiDate = f.FailureStartShamsiDate,
				failureStartTime = f.FailureStartTime,
				failureEndShamsiDate = f.FailureEndShamsiDate,
				failureEndTime = f.FailureEndTime,
				comment = f.Comment,
				createdByName = copying ? null : f.CreatedByName,
				createdOnShamsiDateTime = copying ? null : f.CreatedOnShamsiDateTime
			}).ToList();
		}

		private async Task<List<object>> LoadParametersAsync(long inspectionId, bool copying, CancellationToken cn)
		{
			var rows = await unitOfWork.Repository<ProductInspectionMeasuredParameter>().TableNoTracking
				.Where(f => f.ProductInspectionId == inspectionId)
				.OrderBy(f => f.Id)
				.Select(f => new
				{
					f.Id,
					GroupTitle = f.MeasurementParameterItem != null && f.MeasurementParameterItem.MeasurementParameter != null
						? f.MeasurementParameterItem.MeasurementParameter.Title
						: "",
					ItemTitle = f.MeasurementParameterItem != null ? f.MeasurementParameterItem.Title : "",
					CommonSymbol = f.MeasurementParameterItem != null ? f.MeasurementParameterItem.CommonSymbol : "",
					f.MeasurementParameterItemId,
					SubTitle = f.MeasurementParameterSubItem != null ? f.MeasurementParameterSubItem.Title : "",
					f.MeasurementParameterSubItemId,
					f.MeasurementStartShamsiDate,
					f.MeasurementStartTime,
					f.MeasurementEndShamsiDate,
					f.MeasurementEndTime,
					f.MeasurementResult,
					f.MeasurementValue,
					f.Comment
				})
				.ToListAsync(cn);

			return rows.Select(f => (object)new
			{
				id = copying ? (long?)null : f.Id,
				groupTitle = f.GroupTitle,
				itemTitle = f.ItemTitle,
				commonSymbol = f.CommonSymbol,
				measurementParameterItemId = f.MeasurementParameterItemId,
				subTitle = f.SubTitle,
				measurementParameterSubItemId = f.MeasurementParameterSubItemId,
				measurementStartShamsiDate = f.MeasurementStartShamsiDate,
				measurementStartTime = f.MeasurementStartTime,
				measurementEndShamsiDate = f.MeasurementEndShamsiDate,
				measurementEndTime = f.MeasurementEndTime,
				measurementResult = f.MeasurementResult,
				measurementValue = f.MeasurementValue,
				comment = f.Comment
			}).ToList();
		}

		private static DateTime? TryMiladi(string? shamsi)
		{
			if (!shamsi.HasValue())
				return null;
			try
			{
				return shamsi!.Trim().Split(' ')[0].ToMiladiDate();
			}
			catch
			{
				return null;
			}
		}

		private static int ParseOrderNumber(string? number, long? numeric)
		{
			if (int.TryParse(number?.Trim(), out var parsed) && parsed > 0)
				return parsed;
			if (numeric is > 0 and <= int.MaxValue)
				return (int)numeric.Value;
			return 0;
		}
	}
}
