using Common.Utilities;
using Data.Contracts;
using Entities.App.Inv;
using Entities.App.Pln;
using Entities.App.Sale;
using Entities.App.Sale.Enums;
using Entities.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.ProductionOrderServices;
using System.Globalization;
using System.Text;

namespace WebApp.Controllers.Dynamic
{
	public partial class ProductionOrderController
	{
		/// <summary>معادل گروه ۷۷ سیستم قدیم: کارشناسان فروش (سفارش ساخت).</summary>
		public const string SalesRoleName = "Sale.ProductionOrder.Sales";

		private const int LegacyDeviceTypeOptionalBelowNumber = 7680;
		private const int LegacyWideNotificationBelowNumber = 7686;

		private bool CanEditProductionOrder(ProductionOrder? order, bool isNew)
		{
			if (isNew || order == null || order.Id is null or 0)
				return IsAdministrator || CurrentUserHasAnyRole(SalesRoleName);

			if (order.HamkaranId.HasValue)
				return false;
			if (order.State == ProductionOrderStateEnum.Obsolete)
				return false;
			if (IsAdministrator)
				return true;
			if (!CurrentUserHasAnyRole(SalesRoleName))
				return false;

			var userId = CurrentUserId;
			if (userId.HasValue && (order.CreatedById == userId || order.ModifiedById == userId))
				return true;

			var orgId = CurrentOrganizationUnitId;
			return orgId.HasValue && order.CreatedOrganizationUnitId == orgId;
		}

		private void SetSalesOrderView(ProductionOrder entity, bool isNew)
		{
			var canEdit = CanEditProductionOrder(isNew ? null : entity, isNew);
			ViewBag.CanEditOrder = canEdit;
			ViewBag.EditLockMessage = canEdit ? null : BuildEditLockMessage(isNew ? null : entity, isNew);

			if (isNew || entity?.Id is null or 0)
			{
				ViewBag.SalesItems = new List<ProductionOrderItem>();
				return;
			}

			ViewBag.SalesItems = unitOfWork.Repository<ProductionOrderItem>().TableNoTracking
				.Include(i => i.Part)
				.Where(i => i.ProductionOrderId == entity.Id && i.IsLatestVersion && !i.IsDeleted)
				.OrderBy(i => i.Id)
				.ToList();
		}

		private static string BuildEditLockMessage(ProductionOrder? order, bool isNew)
		{
			if (isNew || order == null)
				return "ایجاد سفارش ساخت فقط برای کارشناسان فروش سفارش ساخت مجاز است.";
			if (order.HamkaranId.HasValue)
				return "این سفارش ساخت در راهکاران است. ویرایش و افزودن قلم از داخل هوایار ممکن نیست.";
			if (order.State == ProductionOrderStateEnum.Obsolete)
				return "سفارش ساخت منسوخ شده و قابل ویرایش نیست.";
			return "ویرایش این سفارش ساخت فقط برای سازنده، آخرین ویرایش‌کننده یا هم‌واحد سازمانی سازنده مجاز است.";
		}

		private static string? ValidateSalesItems(IReadOnlyList<ProductionOrderItem> items, ProductionOrder order, bool isEdit)
		{
			if (items.Count < 1)
				return "اقلام سفارش ساخت نمی‌تواند خالی باشد";

			var requireDeviceType = !(isEdit && OrderNumber(order) < LegacyDeviceTypeOptionalBelowNumber);
			foreach (var item in items)
			{
				if (requireDeviceType && !item.DeviceType.HasValue)
					return "نوع دستگاه در اقلام سفارش ساخت الزامی است";
				if (!item.EquipmentType.HasValue)
					return "نوع تجهیز در اقلام سفارش ساخت الزامی است";
				if (item.PartId is null or 0)
					return "کالا در اقلام سفارش ساخت الزامی است";
				if (item.Amount is null or <= 0)
					return "تعداد / مقدار در اقلام سفارش ساخت الزامی است";
				if (!item.AgreedDeliverDate.HasValue && string.IsNullOrWhiteSpace(item.AgreedDeliverShamsiDate))
					return "تاریخ تحویل توافقی در اقلام سفارش ساخت الزامی است";
			}

			return null;
		}

		private static long OrderNumber(ProductionOrder order)
		{
			if (order.ProductionOrderNumber.HasValue)
				return order.ProductionOrderNumber.Value;
			return long.TryParse(order.Number, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
				? number
				: long.MaxValue;
		}

		[HttpPost("[action]")]
		public async Task<IActionResult> AddSalesItem(ProductionOrderItem item, CancellationToken cn)
		{
			if (item.ProductionOrderId is null or 0)
				return BadRequest("ابتدا سفارش ساخت را ذخیره کنید");

			var order = await unitOfWork.Repository<ProductionOrder>().TableNoTracking
				.FirstOrDefaultAsync(o => o.Id == item.ProductionOrderId, cn);
			if (order == null)
				return BadRequest("سفارش ساخت یافت نشد");
			if (!CanEditProductionOrder(order, isNew: false))
				return Unauthorized("شما دسترسی ویرایش این سفارش ساخت را ندارید");

			var itemError = ValidateSalesItems(new[] { item }, order, isEdit: true);
			if (itemError != null)
				return BadRequest(itemError);

			var saved = await unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				NormalizeSalesDates(new[] { item });
				var routineByPart = await LoadPartSnapshotsAsync(new[] { item.PartId }, cn);
				var now = DateTime.Now;
				var created = BuildNewSalesItem(order.Id!.Value, item, routineByPart, now);
				var added = await unitOfWork.Repository<ProductionOrderItem>().AddAsync(created, cn, true);
				await AddBuyStatusCommentAsync(added, ProductionOrderItemCheckStatusEnum.InitialRegistration, cn);
				await QueueSalesOrderChangeEmailAsync(order, new[] { added }, Array.Empty<(ProductionOrderItem, ProductionOrderItem)>(), Array.Empty<ProductionOrderItem>(), false, cn);
				DetachSalesItem(added);
				return added;
			}, cn);
			return Ok(saved);
		}

		[HttpPost("[action]")]
		public async Task<IActionResult> UpdateSalesItem(ProductionOrderItem item, CancellationToken cn)
		{
			if (item.Id is null or 0)
				return BadRequest("قلم سفارش ساخت یافت نشد");

			var original = await unitOfWork.Repository<ProductionOrderItem>().Table
				.FirstOrDefaultAsync(i => i.Id == item.Id && i.IsLatestVersion && !i.IsDeleted, cn);
			if (original == null)
				return BadRequest("قلم سفارش ساخت یافت نشد");

			var order = await unitOfWork.Repository<ProductionOrder>().TableNoTracking
				.FirstOrDefaultAsync(o => o.Id == original.ProductionOrderId, cn);
			if (order == null)
				return BadRequest("سفارش ساخت یافت نشد");
			if (!CanEditProductionOrder(order, isNew: false))
				return Unauthorized("شما دسترسی ویرایش این سفارش ساخت را ندارید");

			var itemError = ValidateSalesItems(new[] { item }, order, isEdit: true);
			if (itemError != null)
				return BadRequest(itemError);

			NormalizeSalesDates(new[] { item });
			if (SalesFieldsEqual(original, item))
			{
				DetachSalesItem(original);
				return Ok(original);
			}

			var saved = await unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var routineByPart = await LoadPartSnapshotsAsync(new[] { item.PartId, original.PartId }, cn);
				var now = DateTime.Now;
				var hasFinancialApproval = order.State == ProductionOrderStateEnum.FinancialApproval;
				original.IsLatestVersion = false;
				await unitOfWork.Repository<ProductionOrderItem>().UpdateAsync(original, cn, true);

				var checkStatus = ResolveRevisionCheckStatus(order, original, item, routineByPart, hasFinancialApproval);
				var revision = BuildRevision(order.Id!.Value, original, item, routineByPart, checkStatus, now);
				var added = await unitOfWork.Repository<ProductionOrderItem>().AddAsync(revision, cn, true);
				await AddBuyStatusCommentAsync(added, checkStatus, cn);
				if (original.PartId == item.PartId)
					await CopyBomFromItemAsync(original.Id!.Value, added.Id!.Value, cn);

				await QueueSalesOrderChangeEmailAsync(
					order,
					Array.Empty<ProductionOrderItem>(),
					new[] { (original, added) },
					Array.Empty<ProductionOrderItem>(),
					false,
					cn);
				if (checkStatus != ProductionOrderItemCheckStatusEnum.InitialRegistration)
					await QueueEngineeringNotificationAsync(order, new List<ProductionOrderItem> { added }, cn);

				DetachSalesItem(added);
				return added;
			}, cn);
			return Ok(saved);
		}

		[HttpPost("[action]")]
		public async Task<IActionResult> DeleteSalesItem(long id, CancellationToken cn)
		{
			var original = await unitOfWork.Repository<ProductionOrderItem>().Table
				.FirstOrDefaultAsync(i => i.Id == id && i.IsLatestVersion && !i.IsDeleted, cn);
			if (original == null)
				return BadRequest("قلم سفارش ساخت یافت نشد");

			var order = await unitOfWork.Repository<ProductionOrder>().TableNoTracking
				.FirstOrDefaultAsync(o => o.Id == original.ProductionOrderId, cn);
			if (order == null)
				return BadRequest("سفارش ساخت یافت نشد");
			if (!CanEditProductionOrder(order, isNew: false))
				return Unauthorized("شما دسترسی ویرایش این سفارش ساخت را ندارید");

			original.IsDeleted = true;
			original.ProductionStep = ProductionOrderItemProductionStepEnum.Canceled;
			original.Status = ProductionOrderItemStatusEnum.Invalid;
			await unitOfWork.Repository<ProductionOrderItem>().UpdateAsync(original, cn, true);
			await QueueSalesOrderChangeEmailAsync(order, Array.Empty<ProductionOrderItem>(), Array.Empty<(ProductionOrderItem, ProductionOrderItem)>(), new[] { original }, false, cn);
			return Ok();
		}

		private async Task<Dictionary<long, (string? Code, bool Routine)>> LoadPartSnapshotsAsync(IEnumerable<long?> partIds, CancellationToken cn)
		{
			var ids = partIds.Where(id => id is > 0).Select(id => id!.Value).Distinct().ToList();
			if (ids.Count == 0)
				return new Dictionary<long, (string? Code, bool Routine)>();

			return await unitOfWork.Repository<Part>().TableNoTracking
				.Where(p => ids.Contains(p.Id!.Value))
				.Select(p => new { p.Id, p.Code, p.DesignTypeIsRoutine })
				.ToDictionaryAsync(p => p.Id!.Value, p => (Code: p.Code, Routine: p.DesignTypeIsRoutine == true), cn);
		}

		private async Task CopyBomFromItemAsync(long sourceItemId, long targetItemId, CancellationToken cn)
		{
			var bomRepo = unitOfWork.Repository<ProductionOrderItemBom>();
			var sourceBoms = await bomRepo.TableNoTracking
				.Where(b => b.ProductionOrderItemId == sourceItemId)
				.ToListAsync(cn);
			if (sourceBoms.Count == 0)
				return;
			if (await bomRepo.TableNoTracking.AnyAsync(b => b.ProductionOrderItemId == targetItemId, cn))
				return;

			// جدول اصلی فقط آخرین نسخهٔ هر کالا را دارد؛ برای قلم جدید هر ردیف با یک رخداد «ایجاد» در تاریخچه ثبت می‌شود.
			await bomService.CreateLinesAsync(
				sourceBoms.Select(bom => new NewBomLine
				{
					ProductionOrderItemId = targetItemId,
					PartId = bom.PartId,
					Amount = bom.Amount,
					Revision = bom.Revision ?? 0,
					Description = bom.Description,
					SaleUnitDetails = bom.SaleUnitDetails,
					NeedsAVL = bom.NeedsAVL,
					NeedsInspection = bom.NeedsInspection,
					ProductionStep = bom.ProductionStep,
					NumberSupplied = bom.NumberSupplied,
					Status = bom.Status
				}).ToList(),
				cn);
		}

		private static void DetachSalesItem(ProductionOrderItem item)
		{
			item.ProductionOrder = null;
			item.Part = null;
			item.FirstIndustrialChangeBy = null;
			item.ProductionOrderItemComments = new List<ProductionOrderItemComment>();
			item.ProductionOrderItemInquiries = new List<ProductionOrderItemInquiry>();
			item.ProductionOrderItemBom = new List<ProductionOrderItemBom>();
		}

		private static void NormalizeSalesDates(IEnumerable<ProductionOrderItem> items)
		{
			foreach (var item in items)
			{
				if (!item.AgreedDeliverDate.HasValue && !string.IsNullOrWhiteSpace(item.AgreedDeliverShamsiDate))
					item.AgreedDeliverDate = item.AgreedDeliverShamsiDate.ToMiladiDate();
				else if (item.AgreedDeliverDate.HasValue && string.IsNullOrWhiteSpace(item.AgreedDeliverShamsiDate))
					item.AgreedDeliverShamsiDate = item.AgreedDeliverDate.Value.ToShamsiDate();
			}
		}

		private static ProductionOrderItem BuildNewSalesItem(
			long orderId,
			ProductionOrderItem posted,
			IReadOnlyDictionary<long, (string? Code, bool Routine)> routineByPart,
			DateTime now)
		{
			var item = new ProductionOrderItem();
			ApplyPostedSalesFields(item, posted);
			item.ProductionOrderId = orderId;
			item.Id = null;
			item.IsLatestVersion = true;
			item.IsDeleted = false;
			item.Revision = 0;
			item.Status = ProductionOrderItemStatusEnum.NotCompleted;
			item.CheckStatus = ProductionOrderItemCheckStatusEnum.InitialRegistration;
			item.CheckStatusChangedOnMiladiDate = now;
			item.CheckStatusChangedOnShamsiDate = now.ToShamsiDateTime();
			ApplyPartSnapshot(item, routineByPart);
			return item;
		}

		private static ProductionOrderItem BuildRevision(
			long orderId,
			ProductionOrderItem original,
			ProductionOrderItem posted,
			IReadOnlyDictionary<long, (string? Code, bool Routine)> routineByPart,
			ProductionOrderItemCheckStatusEnum checkStatus,
			DateTime now)
		{
			var item = new ProductionOrderItem
			{
				ProductionOrderId = orderId,
				Serial = original.Serial,
				SerialType = original.SerialType,
				PlanningNumber = original.PlanningNumber,
				ProductionStartMiladiDate = original.ProductionStartMiladiDate,
				ProductionStartShamsiDate = original.ProductionStartShamsiDate,
				ProductionEndMiladiDate = original.ProductionEndMiladiDate,
				ProductionEndShamsiDate = original.ProductionEndShamsiDate,
				TestingEndMiladiDate = original.TestingEndMiladiDate,
				TestingEndShamsiDate = original.TestingEndShamsiDate,
				PreparationMiladiDate = original.PreparationMiladiDate,
				PreparationShamsiDate = original.PreparationShamsiDate,
				DeliveryMiladiDate = original.DeliveryMiladiDate,
				DeliveryShamsiDate = original.DeliveryShamsiDate,
				SecondInstallmentShamsiDate = original.SecondInstallmentShamsiDate,
				ThirdInstallmentShamsiDate = original.ThirdInstallmentShamsiDate,
				FourthInstallmentShamsiDate = original.FourthInstallmentShamsiDate,
				FifthInstallmentShamsiDate = original.FifthInstallmentShamsiDate,
				PlanningConsideration = original.PlanningConsideration,
				EngineeringConsideration = original.EngineeringConsideration,
				ProductionStatus = original.ProductionStatus,
				LastComment = original.LastComment,
				DocumentPreparationMiladiDate = original.DocumentPreparationMiladiDate,
				DocumentPreparationShamsiDate = original.DocumentPreparationShamsiDate,
				StandardDeliveryMiladiDate = original.StandardDeliveryMiladiDate,
				StandardDeliveryShamsiDate = original.StandardDeliveryShamsiDate,
				FirstIndustrialChangeById = original.FirstIndustrialChangeById,
				FirstIndustrialChangeMiladiDate = original.FirstIndustrialChangeMiladiDate,
				FirstIndustrialChangeShamsiDate = original.FirstIndustrialChangeShamsiDate,
				Version = original.Version,
				IsLatestVersion = true,
				IsDeleted = false,
				ProductionStep = default,
				Status = ProductionOrderItemStatusEnum.NotCompleted,
				Revision = (original.Revision ?? 0) + 1,
				SendToIndustrialMiladiDate = now,
				SendToIndustrialShamsiDate = now.ToShamsiDate(),
				CheckStatus = checkStatus,
				CheckStatusChangedOnMiladiDate = now,
				CheckStatusChangedOnShamsiDate = now.ToShamsiDateTime()
			};
			ApplyPostedSalesFields(item, posted);
			ApplyPartSnapshot(item, routineByPart);
			return item;
		}

		private static void ApplyPostedSalesFields(ProductionOrderItem target, ProductionOrderItem posted)
		{
			target.IsBuildInside = posted.IsBuildInside;
			target.DeviceType = posted.DeviceType;
			target.EquipmentType = posted.EquipmentType;
			target.PartId = posted.PartId;
			target.Amount = posted.Amount;
			target.AgreedDeliverDate = posted.AgreedDeliverDate;
			target.AgreedDeliverShamsiDate = posted.AgreedDeliverShamsiDate;
			target.PartModel = posted.PartModel;
			target.AirendOrCategory = posted.AirendOrCategory;
			target.InputPressureBar = posted.InputPressureBar;
			target.OutputPressureBar = posted.OutputPressureBar;
			target.Capacity = posted.Capacity;
			target.Scale = posted.Scale;
			target.GasType = posted.GasType;
			target.MaximumTemperature = posted.MaximumTemperature;
			target.PurityPercentage = posted.PurityPercentage;
			target.RelativeHumidity = posted.RelativeHumidity;
			target.BarometricPressure = posted.BarometricPressure;
			target.MovingType = posted.MovingType;
			target.HasInspection = posted.HasInspection;
			target.SalesConsideration = posted.SalesConsideration;
		}

		private static void ApplyPartSnapshot(ProductionOrderItem item, IReadOnlyDictionary<long, (string? Code, bool Routine)> routineByPart)
		{
			if (item.PartId is > 0 && routineByPart.TryGetValue(item.PartId.Value, out var part))
			{
				item.PartCode = part.Code;
				item.IsRoutine = part.Routine;
			}
		}

		private static ProductionOrderItemCheckStatusEnum ResolveRevisionCheckStatus(
			ProductionOrder order,
			ProductionOrderItem original,
			ProductionOrderItem posted,
			IReadOnlyDictionary<long, (string? Code, bool Routine)> routineByPart,
			bool hasFinancialApproval)
		{
			if (!hasFinancialApproval || original.PartId == posted.PartId)
				return ProductionOrderItemCheckStatusEnum.InitialRegistration;

			var routine = posted.PartId is > 0 && routineByPart.TryGetValue(posted.PartId.Value, out var part) && part.Routine;
			var specialDevice = posted.DeviceType is ProductionOrderItemDeviceTypeEnum.CngEquipment
				or ProductionOrderItemDeviceTypeEnum.SparePart;
			if (routine || specialDevice)
				return ProductionOrderItemCheckStatusEnum.IndustrialDashboardInternal;
			if (order.ProjectManagerId.HasValue)
				return ProductionOrderItemCheckStatusEnum.AwaitingProjectManagerApproval;
			return ProductionOrderItemCheckStatusEnum.MechanicalEngineeringApprovalPending;
		}

		private static bool SalesFieldsEqual(ProductionOrderItem original, ProductionOrderItem posted)
		{
			return original.PartId == posted.PartId
				&& original.DeviceType == posted.DeviceType
				&& original.EquipmentType == posted.EquipmentType
				&& original.IsBuildInside == posted.IsBuildInside
				&& original.Amount == posted.Amount
				&& original.PartModel == posted.PartModel
				&& original.AirendOrCategory == posted.AirendOrCategory
				&& original.Capacity == posted.Capacity
				&& original.InputPressureBar == posted.InputPressureBar
				&& original.OutputPressureBar == posted.OutputPressureBar
				&& original.GasType == posted.GasType
				&& original.PurityPercentage == posted.PurityPercentage
				&& original.BarometricPressure == posted.BarometricPressure
				&& original.MaximumTemperature == posted.MaximumTemperature
				&& original.RelativeHumidity == posted.RelativeHumidity
				&& original.Scale == posted.Scale
				&& original.HasInspection == posted.HasInspection
				&& SameDate(original.AgreedDeliverDate, posted.AgreedDeliverDate)
				&& original.AgreedDeliverShamsiDate == posted.AgreedDeliverShamsiDate
				&& original.SalesConsideration == posted.SalesConsideration;
		}

		private static bool SameDate(DateTime? left, DateTime? right)
		{
			if (!left.HasValue && !right.HasValue)
				return true;
			if (!left.HasValue || !right.HasValue)
				return false;
			return left.Value.Date == right.Value.Date;
		}

		private async Task AddBuyStatusCommentAsync(ProductionOrderItem item, ProductionOrderItemCheckStatusEnum status, CancellationToken cn)
		{
			if (item.Id is null or 0)
				return;

			var now = DateTime.Now;
			await unitOfWork.Repository<ProductionOrderItemComment>().AddAsync(new ProductionOrderItemComment
			{
				ProductionOrderItemId = item.Id.Value,
				ProductionStatus = (ProductionOrderItemProductionStatusEnum)(int)status,
				IsForProductionMode = false,
				IsForProductionStepStatus = false,
				StartMiladiDateTime = now,
				StartShamsiDate = now.ToShamsiDateTime()
			}, cn, true);
		}

		private async Task QueueSalesOrderChangeEmailAsync(
			ProductionOrder order,
			IReadOnlyList<ProductionOrderItem> added,
			IReadOnlyList<(ProductionOrderItem Previous, ProductionOrderItem Current)> revised,
			IReadOnlyList<ProductionOrderItem> deleted,
			bool isNewOrder,
			CancellationToken cn)
		{
			try
			{
				var detail = await unitOfWork.Repository<ProductionOrder>().TableNoTracking
					.Include(o => o.Contract).ThenInclude(c => c!.Customer).ThenInclude(cu => cu!.Party)
					.Include(o => o.ProjectDl)
					.Include(o => o.SalesExpert)
					.Include(o => o.AlternativeExpert)
					.Include(o => o.ProjectManager)
					.FirstOrDefaultAsync(o => o.Id == order.Id, cn);
				if (detail == null)
					return;

				var partIds = added.Select(i => i.PartId)
					.Concat(revised.Select(p => p.Current.PartId))
					.Concat(deleted.Select(i => i.PartId))
					.Where(id => id is > 0)
					.Select(id => id!.Value)
					.Distinct()
					.ToList();
				var parts = partIds.Count == 0
					? new Dictionary<long, Part>()
					: await unitOfWork.Repository<Part>().TableNoTracking
						.Where(p => partIds.Contains(p.Id!.Value))
						.ToDictionaryAsync(p => p.Id!.Value, cn);

				var verb = isNewOrder ? "ایجاد" : "ویرایش";
				var body = new StringBuilder();
				body.Append("بدینوسیله اعلام میگردد سفارش ساختی با مشخصات ذیل در سامانه ");
				body.Append(isNewOrder ? "ایجاد گردید" : "ویرایش گردید");
				body.Append("<br /><br /><ul>");
				body.Append(ProductionOrderItemNotificationHelper.Li("شماره سفارش ساخت", detail.ProductionOrderNumber ?? (object?)detail.Number));
				body.Append(ProductionOrderItemNotificationHelper.Li("نسخه (رویژن)", detail.Revision));
				body.Append(ProductionOrderItemNotificationHelper.Li("مشتری", detail.Contract?.Customer?.Party?.FullName));
				body.Append(ProductionOrderItemNotificationHelper.Li("تفضیل", detail.ProjectDl == null ? null : $"{detail.ProjectDl.Title} | {detail.ProjectDl.HamkaranId}"));
				body.Append(ProductionOrderItemNotificationHelper.Li("ایجاد کننده", detail.CreatedByName));
				body.Append("</ul>");
				AppendItemLines(body, "اقلام جدید", added, parts);
				AppendItemLines(body, "اقلام حذف شده", deleted, parts);
				if (revised.Count > 0)
				{
					body.Append("<br />اقلام ویرایش شده<br /><ul>");
					foreach (var pair in revised)
					{
						parts.TryGetValue(pair.Current.PartId ?? 0, out var part);
						body.Append(ProductionOrderItemNotificationHelper.Li("کد کالا", part?.Code ?? pair.Current.PartCode));
						body.Append(ProductionOrderItemNotificationHelper.Li("عنوان کالا", part?.Name));
						body.Append(ProductionOrderItemNotificationHelper.Li("تعداد/مقدار", pair.Current.Amount));
						body.Append(ProductionOrderItemNotificationHelper.Li("تاریخ تحویل توافقی", pair.Current.AgreedDeliverShamsiDate));
						body.Append(ProductionOrderItemNotificationHelper.Li("نسخه(رویژن)", pair.Current.Revision));
					}
					body.Append("</ul>");
				}

				var creatorEmails = detail.CreatedById is > 0
					? await ProductionOrderItemNotificationHelper.GetUserEmailsAsync(dbContext, new[] { detail.CreatedById.Value }, cn)
					: new List<string>();
				var creator = creatorEmails.FirstOrDefault();
				var cc = new List<string?>
				{
					detail.SalesExpert?.Email,
					detail.ProjectManager?.Email,
					detail.AlternativeExpert?.Email
				};
				if (detail.State == ProductionOrderStateEnum.FinancialApproval || OrderNumber(detail) < LegacyWideNotificationBelowNumber)
					cc.AddRange(await ProductionOrderItemNotificationHelper.GetGroupEmailsAsync(unitOfWork, ProductionOrderItemNotificationHelper.GroupItemAdded, cn));

				AddRelatedMemberCopies(cc);

				await ProductionOrderItemNotificationHelper.QueueEmailAsync(
					unitOfWork,
					$"اعلان {verb} سفارش ساخت",
					ProductionOrderItemNotificationHelper.BuildEmailShell(body.ToString()),
					new[] { creator },
					cc,
					detail.Id,
					CurrentUserId ?? 0,
					detail.Id is > 0 ? $"Panel/ProductionOrder/Edit/{detail.Id}" : null,
					cn);
			}
			catch
			{
				// شکست اعلان نباید ذخیره را متوقف کند
			}
		}

		private async Task QueueEngineeringNotificationAsync(ProductionOrder order, List<ProductionOrderItem> items, CancellationToken cn)
		{
			try
			{
				var detail = await unitOfWork.Repository<ProductionOrder>().TableNoTracking
					.Include(o => o.Contract).ThenInclude(c => c!.Customer).ThenInclude(cu => cu!.Party)
					.Include(o => o.SalesExpert)
					.Include(o => o.ProjectManager)
					.FirstOrDefaultAsync(o => o.Id == order.Id, cn);
				if (detail == null)
					return;

				var partIds = items.Where(i => i.PartId is > 0).Select(i => i.PartId!.Value).Distinct().ToList();
				var parts = await unitOfWork.Repository<Part>().TableNoTracking
					.Where(p => partIds.Contains(p.Id!.Value))
					.ToDictionaryAsync(p => p.Id!.Value, cn);

				var deviceTypes = items.Where(i => i.DeviceType.HasValue).Select(i => i.DeviceType!.Value).Distinct().ToList();
				var confirmers = await unitOfWork.Repository<ProductionOrderEquipmentConfirmer>().TableNoTracking
					.Where(c => deviceTypes.Contains(c.DeviceType) && c.MechanicalUserId != null)
					.Select(c => new { c.DeviceType, c.MechanicalUserId })
					.ToListAsync(cn);
				var mechanicalIds = confirmers.Where(c => c.MechanicalUserId.HasValue).Select(c => c.MechanicalUserId!.Value).Distinct().ToList();
				var mechanicalUsers = mechanicalIds.Count == 0
					? new List<User>()
					: await dbContext.Users.AsNoTracking().Where(u => u.Id != null && mechanicalIds.Contains(u.Id.Value)).ToListAsync(cn);
				var mechanicalEmails = mechanicalUsers.Where(u => u.Id.HasValue).ToDictionary(u => u.Id!.Value, u => u.Email);

				var creatorEmails = detail.CreatedById is > 0
					? await ProductionOrderItemNotificationHelper.GetUserEmailsAsync(dbContext, new[] { detail.CreatedById.Value }, cn)
					: new List<string>();
				var creatorEmail = creatorEmails.FirstOrDefault();

				foreach (var group in items.GroupBy(i => i.DeviceType))
				{
					if (!group.Key.HasValue)
						continue;
					var first = group.First();
					var intro = first.CheckStatus == ProductionOrderItemCheckStatusEnum.IndustrialDashboardInternal
						? "بدینوسیله اعلام میگردد اقلام سفارش ساختی با مشخصات ذیل، توسط تیم فروش ایجاد و جهت ادامه روند به کارتابل شما ارسال گردیده است"
						: "بدینوسیله اعلام میگردد اقلام سفارش ساختی با مشخصات ذیل، توسط تیم فروش ایجاد و جهت بررسی به کارتابل شما ارسال گردیده است";
					var body = new StringBuilder();
					body.Append(intro);
					body.Append("<br /><br /><ul>");
					body.Append(ProductionOrderItemNotificationHelper.Li("شماره سفارش ساخت", detail.ProductionOrderNumber ?? (object?)detail.Number));
					body.Append(ProductionOrderItemNotificationHelper.Li("مشتری", detail.Contract?.Customer?.Party?.FullName));
					body.Append("<li style='margin-top:15px'><span style='color:green'>اقلام</span>");
					AppendItemLines(body, null, group.ToList(), parts);
					body.Append("</li></ul>");

					var to = new List<string?>();
					if (first.CheckStatus == ProductionOrderItemCheckStatusEnum.IndustrialDashboardInternal)
						to.AddRange(await ProductionOrderItemNotificationHelper.GetGroupEmailsAsync(unitOfWork, ProductionOrderItemNotificationHelper.GroupIndustrial, cn));
					if (detail.ProjectManager?.Email.HasValue() == true)
						to.Add(detail.ProjectManager.Email);
					foreach (var confirmer in confirmers.Where(c => c.DeviceType == group.Key && c.MechanicalUserId.HasValue))
					{
						if (mechanicalEmails.TryGetValue(confirmer.MechanicalUserId!.Value, out var email))
							to.Add(email);
					}

					await ProductionOrderItemNotificationHelper.QueueEmailAsync(
						unitOfWork,
						"اعلان تغییر وضعیت سفارش ساخت",
						ProductionOrderItemNotificationHelper.BuildEmailShell(body.ToString()),
						to,
						new[] { detail.SalesExpert?.Email, creatorEmail },
						detail.Id,
						CurrentUserId ?? 0,
						detail.Id is > 0 ? $"Panel/ProductionOrder/Edit/{detail.Id}" : null,
						cn);
				}
			}
			catch
			{
				// شکست اعلان نباید ذخیره را متوقف کند
			}
		}

		private static void AppendItemLines(StringBuilder body, string? title, IReadOnlyList<ProductionOrderItem> items, IReadOnlyDictionary<long, Part> parts)
		{
			if (items.Count == 0)
				return;
			if (title.HasValue())
				body.Append("<br />").Append(title).Append("<br />");
			body.Append("<ul>");
			foreach (var item in items)
			{
				parts.TryGetValue(item.PartId ?? 0, out var part);
				body.Append(ProductionOrderItemNotificationHelper.Li("کد کالا", part?.Code ?? item.PartCode));
				body.Append(ProductionOrderItemNotificationHelper.Li("عنوان کالا", part?.Name));
				body.Append(ProductionOrderItemNotificationHelper.Li("تعداد/مقدار", item.Amount));
				body.Append(ProductionOrderItemNotificationHelper.Li("تاریخ تحویل توافقی", item.AgreedDeliverShamsiDate));
				body.Append(ProductionOrderItemNotificationHelper.Li("نسخه(رویژن)", item.Revision));
			}
			body.Append("</ul>");
		}

		/// <summary>معادل HTS AddEmailOfRelatedMembers وقتی سفارش null است: فقط وابستگی نام کاربری.</summary>
		private static void AddRelatedMemberCopies(List<string?> emails)
		{
			if (emails.Any(e => e != null && e.Contains("mehrafzoon", StringComparison.OrdinalIgnoreCase)))
				emails.Add("faraji.p@havayar.com");
			if (emails.Any(e => e != null && e.Contains("khalaj.m", StringComparison.OrdinalIgnoreCase)))
				emails.Add("Bayat.f@havayar.com");
		}
	}
}
