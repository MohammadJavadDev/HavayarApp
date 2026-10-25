using Aspose.Cells;
using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Data.Repositories;
using Data.SystemAuth;
using Entities.App.FIN;
using Entities.App.Inv;
using Entities.App.Qc;
using Entities.App.Sale;
using Entities.App.Sale.Enums;
using Entities.Base;
using Entities.Base.DataTable;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Entities.Base.Sms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.NotificationGroupServices;
using Services.ProductionOrderServices;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Text;
using WebApp.Models.Sale;
using WebFramework.Filtters;
using WebFramework.Page;
using License = Aspose.Cells.License;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("اقلام سفارش ساخت", typeof(ProductionOrderItem))]
	public class ProductionOrderItemController(
		IUnitOfWork unitOfWork,
		IPropertyIdentityService identityService,
		IWebHostEnvironment _webHostEnvironment,
		INotificationGroupService notificationGroupService,
		Data.ApplicationDbContext dbContext,
		global::Services.Auth.IUserService userService,
		IOnlineUserService onlineUserService,
		global::Services.Sms.ISmsService sms,
		global::Services.Sms.ISmsRecipientResolver smsRecipients,
		IProductionOrderItemBomService bomService,
		global::Services.FileServices.IFileService fileService) : BaseController
	{
		/// <summary>معادل صفحه HTS 508 / Pln_ProductionOrderItemBom — عنوان: Bom اقلام سفارش ساخت</summary>
		public const string BomManageRoleName = "Sale.ProductionOrderItemBom";

		/// <summary>
		/// یک نقش برای مشاهده BOM و اصلاح فیلدهای صنایع (مرحله ساخت، تعداد تامین‌شده، وضعیت).
		/// Name نقش در system.Role و مقدار داخل Roles کاربر باید عین همین رشته باشد؛ Name کوتاه bom این صفحه را باز نمی‌کند.
		/// </summary>
		public const string BomNewFieldsRoleName = "تکمیل اطلاعات bom اقلام سفارش ساخت صنایع";

		/// <summary>مشاهده لیست BOM بدون افزودن، ویرایش یا حذف. Name نقش باید همین رشته بماند چون HasRole روی Roles کاربر همین Name را می‌خواند.</summary>
		public const string BomViewRoleName = "Sale.ProductionOrderItemBom.View";

		/// <summary>معادل HTS ProductionPermission روی صفحه 214 / گروه 361</summary>
		public const string ProductionRoleName = "Sale.ProductionOrderItem.Production";

		/// <summary>معادل HTS QcPermission روی صفحه 214 / گروه 362</summary>
		public const string QcRoleName = "Sale.ProductionOrderItem.Qc";

		/// <summary>معادل HTS گروه 596 «اقلام سفارش ساخت - کارشناسان فروش» برای تغییر وضعیت روی ۳۱۶۷</summary>
		public const string SellTeamRoleName = "Sale.ProductionOrderItem.SellTeam";

		private bool CanManageBom() =>
			IsAdministrator || CurrentUserHasAnyRole(BomManageRoleName);

		private bool CanEditBomNewFields() =>
			IsAdministrator || CurrentUserHasAnyRole(BomNewFieldsRoleName);

		private bool CanViewBom() =>
			IsAdministrator || CurrentUserHasAnyRole(BomViewRoleName);

		private bool CanOpenBomPage() =>
			CanManageBom() || CanEditBomNewFields() || CanViewBom();

		private bool CanEditBomForm() =>
			CanManageBom() || CanEditBomNewFields();

		/// <summary>
		/// ذی‌نفع سفارش: کارشناس فروش، مدیر فروش، کارشناس جایگزین یا مدیر پروژهٔ همان سفارش (معادل HasProjectOrSalesUnitPermission در HTS).
		/// مجوز مشاهدهٔ BOM و ثبت ملاحظات بر اساس همین سمت است، نه نقش ثابت.
		/// </summary>
		private async Task<bool> IsOrderStakeholderAsync(long productionOrderItemId, CancellationToken cn)
		{
			var userId = CurrentUserId;
			if (userId is null or <= 0 || productionOrderItemId <= 0)
				return false;

			var order = await unitOfWork.Repository<ProductionOrderItem>()
				.TableNoTracking
				.Where(i => i.Id == productionOrderItemId && i.ProductionOrderId != null)
				.Select(i => new
				{
					i.ProductionOrder!.SalesExpertId,
					i.ProductionOrder.SalesManagerId,
					i.ProductionOrder.AlternativeExpertId,
					i.ProductionOrder.ProjectManagerId
				})
				.FirstOrDefaultAsync(cn);

			return order != null
				&& (order.SalesExpertId == userId
					|| order.SalesManagerId == userId
					|| order.AlternativeExpertId == userId
					|| order.ProjectManagerId == userId);
		}

		/// <summary>مشاهدهٔ BOM: نقش‌های BOM یا ذی‌نفع همان سفارش.</summary>
		private async Task<bool> CanReadBomAsync(long productionOrderItemId, CancellationToken cn) =>
			CanOpenBomPage() || await IsOrderStakeholderAsync(productionOrderItemId, cn);

		/// <summary>ثبت ملاحظات (تعیین تکلیف): فقط مدیر سیستم یا ذی‌نفع همان سفارش.</summary>
		private async Task<bool> CanConsiderBomAsync(long productionOrderItemId, CancellationToken cn) =>
			IsAdministrator || await IsOrderStakeholderAsync(productionOrderItemId, cn);

		/// <summary>
		/// شرط وضعیت قلم برای هر نوشتن روی BOM (معادل فعال بودن دکمهٔ BOM در HTS):
		/// قلم حذف نشده، آخرین نسخه، در کارتابل صنایع (۱۹۰۴/۱۹۰۵) و مرحله ساخت غیر از «تحویل فروش» (۲۲۳).
		/// مدیر سیستم معاف است. null یعنی مجاز.
		/// </summary>
		private async Task<string?> GetBomWriteBlockReasonAsync(long productionOrderItemId, CancellationToken cn)
		{
			if (IsAdministrator)
				return null;

			var item = await unitOfWork.Repository<ProductionOrderItem>()
				.TableNoTracking
				.Where(i => i.Id == productionOrderItemId)
				.Select(i => new { i.IsDeleted, i.IsLatestVersion, i.CheckStatus, i.ProductionStep })
				.FirstOrDefaultAsync(cn);

			if (item == null)
				return "قلم سفارش ساخت یافت نشد";
			if (item.IsDeleted)
				return "قلم سفارش ساخت حذف شده است و BOM آن قابل تغییر نیست";
			if (!item.IsLatestVersion)
				return "فقط BOM آخرین نسخهٔ قلم قابل تغییر است";
			if (item.CheckStatus is not (ProductionOrderItemCheckStatusEnum.IndustrialDashboardInternal
				or ProductionOrderItemCheckStatusEnum.ForeignIndustryCatalog))
				return "BOM فقط برای اقلام کارتابل صنایع قابل تغییر است";
			if (item.ProductionStep == ProductionOrderItemProductionStepEnum.DeliveredToSales)
				return "مرحله ساخت این قلم «تحویل فروش» است و BOM آن قابل افزودن یا ویرایش نیست";

			return null;
		}

		private bool HasPlanningEditRole() =>
			IsAdministrator || CurrentUserHasAnyRole("Industries", "Sale.ProductionOrderItem.AcceptIndustrial");

		/// <summary>
		/// معادل HTS HasPlanningPermission + canBeSelect: ذخیره اقلام فقط برای صنایع روی ۱۹۰۴/۱۹۰۵ و غیرحذف‌شده؛
		/// قلم جدید (بدون Id) فقط با نقش برنامه‌ریزی/صنایع.
		/// </summary>
		private bool CanSaveIndustrialItem(ProductionOrderItem? item, bool isNew)
		{
			if (IsAdministrator)
				return true;
			if (!HasPlanningEditRole())
				return false;
			if (isNew || item == null || item.Id is null or 0)
				return true;
			if (item.IsDeleted)
				return false;
			return item.CheckStatus is ProductionOrderItemCheckStatusEnum.IndustrialDashboardInternal
				or ProductionOrderItemCheckStatusEnum.ForeignIndustryCatalog;
		}

		/// <summary>
		/// دکمه «تغییر وضعیت» فقط وقتی روشن است که کمبوی وضعیت تولید برای این قلم حداقل یک مقدار مجاز داشته باشد.
		/// معادل گیت HTS (مرحله ۲۱۵/۲۱۶/۲۲۰ + تاریخ شروع/پایان، یا توقف بازدید کارفرما ۳۱۶۷) به‌علاوه نقش صنایع/مدیر.
		/// کمیته تامین و استعلام از دکمه‌های خودشان می‌روند، نه از این کمبو.
		/// </summary>
		private bool CanChangeProductionStatus(ProductionOrderItem? item)
		{
			if (item == null || item.Id is null or 0)
				return false;
			return GetVisibleChangeStatuses(item).Count > 0;
		}

		/// <summary>
		/// مقادیر مجاز کمبوی «وضعیت تولید». مدیر و صنایع همهٔ حالت‌های برقرارِ قلم را می‌بینند؛
		/// بقیه فقط حالت نقش خودشان را.
		/// </summary>
		private IReadOnlyList<ProductionOrderItemProductionStatusEnum> GetVisibleChangeStatuses(ProductionOrderItem item)
		{
			return ProductionOrderItemWorkflowRules.GetVisibleChangeStatuses(
				item.ProductionStep,
				item.ProductionStartMiladiDate.HasValue,
				item.ProductionEndMiladiDate.HasValue,
				item.ProductionStatus,
				unrestricted: IsAdministrator || HasPlanningEditRole(),
				hasProduction: CurrentUserHasAnyRole(ProductionRoleName),
				hasQc: CurrentUserHasAnyRole(QcRoleName),
				hasSellTeam: CurrentUserHasAnyRole(SellTeamRoleName));
		}

		/// <summary>
		/// معادل فیلتر کارتابل من / اعلان مهندسی: تاییدکننده تجهیز برای DeviceType قلم
		/// (نقش رشته به‌تنهایی کافی نیست مگر وقتی برای آن DeviceType تاییدکننده‌ای تنظیم نشده باشد).
		/// </summary>
		private async Task<bool> IsCurrentUserEquipmentConfirmerAsync(ProductionOrderItem item, CancellationToken cn)
		{
			if (IsAdministrator)
				return true;
			if (CurrentUserId is null or <= 0 || !item.DeviceType.HasValue)
				return false;

			var isElectrical = ElectricalDeviceTypes.Contains(item.DeviceType.Value);
			var confirmers = await unitOfWork.Repository<Entities.App.Pln.ProductionOrderEquipmentConfirmer>()
				.TableNoTracking
				.Where(c => c.DeviceType == item.DeviceType.Value)
				.Select(c => new { c.MechanicalUserId, c.ElectricalUserId })
				.ToListAsync(cn);

			if (isElectrical)
			{
				var configured = confirmers.Any(c => c.ElectricalUserId != null);
				if (configured)
					return confirmers.Any(c => c.ElectricalUserId == CurrentUserId);
				return CurrentUserHasAnyRole("Sale.ProductionOrderItem.AcceptEngineeringElectrical");
			}

			var mechConfigured = confirmers.Any(c => c.MechanicalUserId != null);
			if (mechConfigured)
				return confirmers.Any(c => c.MechanicalUserId == CurrentUserId);
			return CurrentUserHasAnyRole("Sale.ProductionOrderItem.AcceptEngineeringMechanical");
		}

		/// <summary>
		/// نقش رشته مهندسی همین قلم. نوع دستگاه خالی مثل مکانیک حساب می‌شود تا دکمه تایید
		/// برای کاربر دارای نقش، به خاطر نخواندن DeviceType حذف نشود.
		/// </summary>
		private bool HasEngineeringDisciplineRole(ProductionOrderItem item)
		{
			if (IsAdministrator)
				return true;
			var electrical = item.DeviceType.HasValue && ElectricalDeviceTypes.Contains(item.DeviceType.Value);
			return CurrentUserHasAnyRole(electrical
				? "Sale.ProductionOrderItem.AcceptEngineeringElectrical"
				: "Sale.ProductionOrderItem.AcceptEngineeringMechanical");
		}

		private IActionResult? ForbidBomUnless(bool allowed, string message) =>
			allowed ? null : Unauthorized(message);

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(ProductionOrderItem productionOrderItem, CancellationToken cn)
		{
			var isNew = productionOrderItem.Id == null || productionOrderItem.Id == 0;
			if (!isNew)
			{
				var existing = await unitOfWork.Repository<ProductionOrderItem>().TableNoTracking
					.Where(c => c.Id == productionOrderItem.Id)
					.Select(c => new { c.Id, c.CheckStatus, c.IsDeleted })
					.FirstOrDefaultAsync(cn);
				if (existing == null)
					return BadRequest("قلم سفارش ساخت یافت نشد");
				var gate = new ProductionOrderItem
				{
					Id = existing.Id,
					CheckStatus = existing.CheckStatus,
					IsDeleted = existing.IsDeleted
				};
				if (!CanSaveIndustrialItem(gate, isNew: false))
					return Unauthorized("شما دسترسی ذخیره این قلم را ندارید");
			}
			else if (!CanSaveIndustrialItem(productionOrderItem, isNew: true))
			{
				return Unauthorized("شما دسترسی ایجاد قلم سفارش ساخت را ندارید");
			}

			if (isNew)
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
			if (!CanSaveIndustrialItem(productionOrderItem, isNew: true))
				return Unauthorized("شما دسترسی ایجاد قلم سفارش ساخت را ندارید");

			if (productionOrderItem.ProductionOrderId is > 0)
			{
				var parentOrder = await unitOfWork.Repository<ProductionOrder>().TableNoTracking
					.Where(p => p.Id == productionOrderItem.ProductionOrderId)
					.Select(p => new { p.Id, p.HamkaranId })
					.FirstOrDefaultAsync(cn);
				if (parentOrder == null)
					return BadRequest("سفارش ساخت یافت نشد");
				if (parentOrder.HamkaranId.HasValue)
					return BadRequest("این سفارش ساخت در راهکاران است. افزودن قلم فقط از طریق راهکاران انجام می‌شود.");
			}

			// قلم برنامه‌ریزی سفارش ساخت ندارد. فرم این فیلد را نمی‌فرستد و پیش‌فرض موجودیت false است؛
			// بدون true شدن از لیست‌هایی که فقط آخرین نسخه را نشان می‌دهند حذف می‌شود.
			if (!(productionOrderItem.ProductionOrderId > 0))
				productionOrderItem.IsLatestVersion = true;

			// معادل HTS (افزودن قلم توسط برنامه‌ریزی): روتین کالا یا نوع دستگاه تجهیزات CNG → کارتابل صنایع ۱۹۰۴،
			// در غیر این صورت منتظر تایید مهندسی ۲۱۹۵. روتین از DesignTypeIsRoutine کالا خوانده می‌شود، نه از فرم.
			var partIsRoutine = false;
			if (productionOrderItem.PartId is > 0)
			{
				partIsRoutine = await unitOfWork.Repository<Part>()
					.TableNoTracking
					.Where(p => p.Id == productionOrderItem.PartId)
					.Select(p => p.DesignTypeIsRoutine)
					.FirstOrDefaultAsync(cn) == true;
			}

			productionOrderItem.IsRoutine = partIsRoutine;
			var sendToIndustrial = partIsRoutine
				|| productionOrderItem.DeviceType == ProductionOrderItemDeviceTypeEnum.CngEquipment;
			var initialStatus = sendToIndustrial
				? ProductionOrderItemCheckStatusEnum.IndustrialDashboardInternal
				: ProductionOrderItemCheckStatusEnum.MechanicalEngineeringApprovalPending;
			var now = DateTime.Now;
			productionOrderItem.CheckStatus = initialStatus;
			productionOrderItem.CheckStatusChangedOnMiladiDate = now;
			productionOrderItem.CheckStatusChangedOnShamsiDate = now.ToShamsiDateTime();
			if (sendToIndustrial)
				TrySetSendToIndustrial(productionOrderItem, initialStatus, now);

			var entity = await unitOfWork.Repository<ProductionOrderItem>().SaveAsync(productionOrderItem, cn, true);
			// معادل HTS DoOriginalOperation: ایجاد قلم با تاریخ شروع و بدون تاریخ پایان → کامنت ۱۷۹۲
			await AddProductionStartedCommentIfNeededAsync(entity, previousProductionStart: null, cn);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(ProductionOrderItem productionOrderItem, CancellationToken cn)
		{

			var oldEntity = await unitOfWork.Repository<ProductionOrderItem>()
				.TableNoTracking
				.Include(c => c.Part)
				.Include(c => c.ProductionOrder)
				.FirstOrDefaultAsync(
				c => c.Id == productionOrderItem.Id);

			if (oldEntity == null)
				return BadRequest("قلم سفارش ساخت یافت نشد");

			if (!CanSaveIndustrialItem(oldEntity, isNew: false))
				return Unauthorized("شما دسترسی ویرایش این قلم را ندارید");

			var previousSerial = oldEntity.Serial;
			var previousProductionStep = oldEntity.ProductionStep;
			var previousProductionStart = oldEntity.ProductionStartMiladiDate;

			// فقط فیلدهای داخل #changeDataSection که صنایع در فرم وارد یا اصلاح می‌کند.
			// فیلدهای فقط‌خواندنی (تاریخ تحویل استاندارد، آماده‌سازی مدارک، روتین، وضعیت کارتابل و بقیه)
			// در پست نیستند؛ کپی کردنشان مقدار ذخیره‌شده را با null یا پیش‌فرض پاک می‌کند.
			oldEntity.DeviceType = productionOrderItem.DeviceType;
			oldEntity.ProductionStep = productionOrderItem.ProductionStep;
			oldEntity.PlanningNumber = productionOrderItem.PlanningNumber;
			oldEntity.Serial = productionOrderItem.Serial;
			oldEntity.SerialType = productionOrderItem.SerialType;
			oldEntity.ProductionStartShamsiDate = productionOrderItem.ProductionStartShamsiDate;
			oldEntity.ProductionStartMiladiDate = productionOrderItem.ProductionStartMiladiDate;
			oldEntity.ProductionEndShamsiDate = productionOrderItem.ProductionEndShamsiDate;
			oldEntity.ProductionEndMiladiDate = productionOrderItem.ProductionEndMiladiDate;
			oldEntity.TestingEndShamsiDate = productionOrderItem.TestingEndShamsiDate;
			oldEntity.TestingEndMiladiDate = productionOrderItem.TestingEndMiladiDate;
			oldEntity.PreparationShamsiDate = productionOrderItem.PreparationShamsiDate;
			oldEntity.PreparationMiladiDate = productionOrderItem.PreparationMiladiDate;
			oldEntity.DeliveryShamsiDate = productionOrderItem.DeliveryShamsiDate;
			oldEntity.DeliveryMiladiDate = productionOrderItem.DeliveryMiladiDate;
			oldEntity.PlanningConsideration = productionOrderItem.PlanningConsideration;
			oldEntity.Status = productionOrderItem.Status;

			// اولین اقدام صنایع روی قلم در کارتابل صنایع
			if (oldEntity.CheckStatus.HasValue && IndustrialDashboardStatuses.Contains(oldEntity.CheckStatus.Value))
				TrySetFirstIndustrialChange(oldEntity, DateTime.Now, CurrentUserId);

			var part = oldEntity.Part;
			var productionOrder = oldEntity.ProductionOrder;
			oldEntity.Part = null;
			oldEntity.ProductionOrder = null;

			var entity = await unitOfWork.Repository<ProductionOrderItem>().UpdateAsync(oldEntity, cn, true);

			// معادل HTS DoOriginalOperation: تغییر مرحله ساخت از فرم ویرایش → کامنت «مرحله ساخت» (ویرایش رکورد توسط صنایع)
			if (previousProductionStep != entity.ProductionStep)
			{
				var now = DateTime.Now;
				await unitOfWork.Repository<ProductionOrderItemComment>().AddAsync(new ProductionOrderItemComment
				{
					ProductionOrderItemId = entity.Id!.Value,
					ProductionStatus = entity.CheckStatus.HasValue
						? (ProductionOrderItemProductionStatusEnum)entity.CheckStatus.Value
						: ProductionOrderItemProductionStatusEnum.NotDetermined,
					ProductionStep = entity.ProductionStep,
					Comment = "ویرایش مرحله ساخت توسط صنایع",
					StartMiladiDateTime = now,
					StartShamsiDate = now.ToShamsiDateTime(),
					IsForProductionMode = false,
					IsForProductionStepStatus = true
				}, cn, true);
			}

			// معادل HTS DoOriginalOperation: تغییر تاریخ شروع تولید، وقتی پایان تولید خالی است → کامنت ۱۷۹۲ (شروع تولید)
			await AddProductionStartedCommentIfNeededAsync(entity, previousProductionStart, cn);

			// معادل HTS TFS 1645: ویرایش قلم، اگر سریال عوض شده باشد و در «بازرسی محصول در هوایار»
			// برای همین قلم رکوردی باشد (CheckOrderItemHasInspection روی ProductionOrderItemNewId، بدون فیلتر نوع بازرسی).
			// تیک HasInspection قلم این شرط نیست.
			if ((previousSerial ?? string.Empty) != (entity.Serial ?? string.Empty)
				&& entity.Id is > 0
				&& await ItemHasProductInspectionAsync(entity.Id.Value, cn))
				await QueueSerialChangeNotificationAsync(entity, part, productionOrder, previousSerial, cn);

			return Ok(entity);
		}

		/// <summary>
		/// معادل HTS DoOriginalOperation + DoChangeProductionStatusOperationOriginal برای وضعیت ۱۷۹۲.
		/// تاریخ شروع روی خود قلم از فرم می‌آید؛ این متد فقط تاریخچهٔ «شروع تولید» و اعلان را می‌سازد.
		/// تریگر والد (ProductionOrderItemCommentAction) وضعیت تولید قلم را از روی همین کامنت به‌روز می‌کند.
		/// </summary>
		private async Task AddProductionStartedCommentIfNeededAsync(
			ProductionOrderItem entity,
			DateTime? previousProductionStart,
			CancellationToken cn)
		{
			if (entity.Id is null or 0)
				return;
			if (!entity.ProductionStartMiladiDate.HasValue || entity.ProductionEndMiladiDate.HasValue)
				return;
			if (IsSameCalendarDate(previousProductionStart, entity.ProductionStartMiladiDate))
				return;

			var comment = new ProductionOrderItemComment
			{
				ProductionOrderItemId = entity.Id.Value,
				ProductionStatus = ProductionOrderItemProductionStatusEnum.ProductionStarted,
				IsForProductionMode = true,
				IsForProductionStepStatus = false,
				StartMiladiDateTime = entity.ProductionStartMiladiDate,
				StartShamsiDate = string.IsNullOrWhiteSpace(entity.ProductionStartShamsiDate)
					? entity.ProductionStartMiladiDate.Value.ToShamsiDate()
					: entity.ProductionStartShamsiDate
			};

			await unitOfWork.Repository<ProductionOrderItemComment>().AddAsync(comment, cn, true);
			entity.ProductionStatus = ProductionOrderItemProductionStatusEnum.ProductionStarted;

			var notifyItem = await unitOfWork.Repository<ProductionOrderItem>()
				.TableNoTracking
				.Include(c => c.Part)
				.Include(c => c.ProductionOrder).ThenInclude(po => po!.Branch)
				.Include(c => c.ProductionOrder).ThenInclude(po => po!.Contract).ThenInclude(ct => ct!.Customer).ThenInclude(cu => cu!.Party)
				.FirstOrDefaultAsync(c => c.Id == entity.Id, cn);
			if (notifyItem != null)
				await QueueProductionStatusChangeNotificationAsync(notifyItem, comment, cn);
		}

		private static bool IsSameCalendarDate(DateTime? previous, DateTime? current)
		{
			if (!previous.HasValue || !current.HasValue)
				return false;
			return previous.Value.Date == current.Value.Date;
		}

		/// <summary>
		/// معادل HTS CheckOrderItemHasInspection: هر رکورد Qc.ProductInspection برای این قلم، داخل شرکت یا محل پیمانکار.
		/// </summary>
		private Task<bool> ItemHasProductInspectionAsync(long productionOrderItemId, CancellationToken cn) =>
			unitOfWork.Repository<ProductInspection>().TableNoTracking
				.AnyAsync(x => x.ProductionOrderItemId == productionOrderItemId, cn);

		/// <summary>
		/// معادل HTS SendChengedSerialNotificationEmail: اطلاع تغییر سریال به واحد کنترل کیفیت.
		/// </summary>
		private async Task QueueSerialChangeNotificationAsync(ProductionOrderItem item, Part? part, ProductionOrder? productionOrder, string? previousSerial, CancellationToken cn)
		{
			try
			{
				var to = await ProductionOrderItemNotificationHelper.GetGroupEmailsAsync(unitOfWork, ProductionOrderItemNotificationHelper.GroupSerialChangeQc, cn);
				var body = ProductionOrderItemNotificationHelper.BuildEmailShell(
					"بدینوسیله به استحضار می رساند سریال قلم سفارش ساخت ذیل که دارای بازرسی حین ساخت است تغییر یافت.<br /><br /><ul>"
					+ ProductionOrderItemNotificationHelper.Li("شماره سفارش ساخت", productionOrder?.ProductionOrderNumber ?? (object?)productionOrder?.Number)
					+ ProductionOrderItemNotificationHelper.Li("کد کالا", part?.Code)
					+ ProductionOrderItemNotificationHelper.Li("عنوان کالا", part?.Name)
					+ ProductionOrderItemNotificationHelper.Li("سریال قبلی", previousSerial)
					+ ProductionOrderItemNotificationHelper.Li("سریال جدید", item.Serial)
					+ ProductionOrderItemNotificationHelper.Li("تغییر دهنده", CurrentUserFullNameFn ?? CurrentUserFullName)
					+ "</ul>");

				await ProductionOrderItemNotificationHelper.QueueEmailAsync(unitOfWork, "اعلان تغییر سریال قلم سفارش ساخت", body,
					to, new[] { CurrentUserEmail }, item.Id, CurrentUserId ?? 0, $"Panel/ProductionOrderItem/Edit/{item.Id}", cn);
			}
			catch
			{
				// شکست اعلان نباید ذخیره را متوقف کند
			}
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
		public async Task<IActionResult> Edit(long? id, CancellationToken cn)
		{
			if (id != null && id != 0)
			{
				var entity = await unitOfWork.Repository<ProductionOrderItem>().TableNoTracking
					.Include(c => c.ProductionOrder).ThenInclude(po => po!.ManagementConfirmationAttachmentFile)
					.Include(c => c.ProductionOrder).ThenInclude(po => po!.DepositFactorAttachmentFile)
					.Include(c => c.ProductionOrder).ThenInclude(po => po!.PreFactorAttachmentFile)
					.Include(c => c.ProductionOrder).ThenInclude(po => po!.ContractAttachmentFile)
					.Include(c => c.ProductionOrder).ThenInclude(po => po!.EngineeringAttachmentFile)
					.Include(c => c.Part)
					.FirstOrDefaultAsync(c => c.Id == id, cn);

				await PopulateEditButtonFlagsAsync(entity, isNew: false, cn);
				return View(@"\Views\Panel\Sale\ProductionOrderItem\Edit.cshtml", entity);
			}

			var newEntity = new ProductionOrderItem();
			await PopulateEditButtonFlagsAsync(newEntity, isNew: true, cn);
			return View(@"\Views\Panel\Sale\ProductionOrderItem\Edit.cshtml", newEntity);
		}

		private async Task PopulateEditButtonFlagsAsync(ProductionOrderItem? entity, bool isNew, CancellationToken cn)
		{
			var isElectrical = entity != null &&
				entity.DeviceType.HasValue &&
				ElectricalDeviceTypes.Contains(entity.DeviceType.Value);
			ViewBag.IsElectricalDiscipline = isElectrical;

			var checkStatus = entity?.CheckStatus;
			var hasSerial = !string.IsNullOrWhiteSpace(entity?.Serial);
			var isEquipmentConfirmer = entity != null && !isNew
				&& await IsCurrentUserEquipmentConfirmerAsync(entity, cn);
			var hasEngineeringRole = entity != null && HasEngineeringDisciplineRole(entity);

			ViewBag.CanSaveItem = CanSaveIndustrialItem(entity, isNew);
			ViewBag.CanChangeStatus = !isNew && CanChangeProductionStatus(entity);
			ViewBag.CanAcceptEngineering = !isNew
				&& checkStatus == ProductionOrderItemCheckStatusEnum.MechanicalEngineeringApprovalPending
				&& (isEquipmentConfirmer || hasEngineeringRole);
			ViewBag.CanAcceptProjectManager = !isNew
				&& checkStatus == ProductionOrderItemCheckStatusEnum.AwaitingProjectManagerApproval
				&& (IsAdministrator || CurrentUserHasAnyRole("Sale.ProductionOrderItem.AcceptProjectManager"));
			ViewBag.CanSendInquiryResult = !isNew && (
				IsAdministrator
				|| (checkStatus == ProductionOrderItemCheckStatusEnum.InquiryNeeded && CurrentUserHasAnyRole("Sale.ProductionOrderItem.Inquirer"))
				|| (checkStatus == ProductionOrderItemCheckStatusEnum.IndustrialMinistryReviewRequired && CurrentUserHasAnyRole("Sale.ProductionOrderItem.MinistryOfIndustryInquirer")));
			ViewBag.CanCommitteeDecide = !isNew
				&& (IsAdministrator || CurrentUserHasAnyRole("Sale.ProductionOrderItem.SupplyCommitteeBoss"))
				&& checkStatus is ProductionOrderItemCheckStatusEnum.SendToSupplyCommitteeChair
					or ProductionOrderItemCheckStatusEnum.CommitteeHeadReviewPending;
			ViewBag.CanStopActions = !isNew && hasSerial && entity is { IsDeleted: false };
			ViewBag.CanViewChildLinks = !isNew;

			// ذی‌نفع سفارش (فروش/مدیر پروژه) بدون نقش BOM هم دکمهٔ «Bom محصول» را می‌بیند
			ViewBag.IsBomStakeholder = !isNew
				&& entity?.Id is > 0
				&& await IsOrderStakeholderAsync(entity.Id.Value, cn);
		}

		/// <summary>
		/// صفحهٔ BOM قلم. عمداً بدون [ActionDisplayName]: مجوز بر اساس نقش‌های BOM یا سمت در سفارش
		/// (کارشناس فروش، مدیر فروش، کارشناس جایگزین، مدیر پروژه) است و داخل کد کنترل می‌شود.
		/// اگر این مسیر در کاتالوگ نقش‌ها بود، ذی‌نفع بدون نقش BOM در middleware ۴۰۳ می‌گرفت.
		/// جدول BOM نمایه داده POI_BomByItem است (Seed_ProductionOrderItemBomByItem_DataProfile.sql)؛ همان شرط مشاهده
		/// (admin / نقش‌های BOM / ذی‌نفع سفارش) داخل SQL نمایه هم اعمال می‌شود چون /System/FetchDataProfile برای هر کاربر وارد شده باز است.
		/// </summary>
		[HttpGet("[action]")]
		public async Task<IActionResult> BomListBy(long? productionOrderItemId, CancellationToken cn)
		{
			if (productionOrderItemId == null || productionOrderItemId == 0)
				throw new Exception("شناسه قلم سفارش ساخت نمیتواند خالی باشد.");

			var forbid = ForbidBomUnless(await CanReadBomAsync(productionOrderItemId.Value, cn), "شما دسترسی مشاهده BOM اقلام سفارش ساخت را ندارید");
			if (forbid != null) return forbid;

			var item = await unitOfWork.Repository<ProductionOrderItem>().TableNoTracking
				.Where(c => c.Id == productionOrderItemId)
				.Select(c => new
				{
					c.Id,
					ProductionOrderNumber = c.ProductionOrder.ProductionOrderNumber,
					PartCode = c.Part.Code,
					PartName = c.Part.Name,
					IsRoutine = c.Part.DesignTypeIsRoutine
				})
				.FirstOrDefaultAsync(cn);

			if (item == null)
				throw new Exception("قلم سفارش ساخت یافت نشد.");

			// نوشتن روی BOM فقط وقتی مجاز است که وضعیت قلم اجازه بدهد (H4)؛ مشاهده همیشه آزاد است.
			var writeBlockReason = await GetBomWriteBlockReasonAsync(productionOrderItemId.Value, cn);
			var canWrite = writeBlockReason == null;

			var model = new ProductionOrderItemBomListByParentViewModel
			{
				ProductionOrderItemId = item.Id!.Value,
				ProductionOrderNumber = item.ProductionOrderNumber,
				PartCode = item.PartCode,
				PartName = item.PartName,
				CanManageBom = canWrite && CanManageBom(),
				CanEditBomNewFields = canWrite && CanEditBomNewFields(),
				CanConsider = canWrite && await CanConsiderBomAsync(productionOrderItemId.Value, cn),
				CanAddFromFormula = canWrite && CanManageBom() && item.IsRoutine == true,
				WriteBlockReason = writeBlockReason
			};

			return View(@"\Views\Panel\Sale\ProductionOrderItem\ListByParentId.cshtml", model);
		}

		/// <summary>
		/// تاریخچهٔ کامل یک ردیف BOM (ایجاد، ویرایش، ملاحظات) از جدول تاریخچه؛ فقط رخدادهای حذف‌نشده.
		/// معادل ادغام‌شدهٔ تب‌های «تاریخچه» و «مجموعه تغییرات» در HTS.
		/// </summary>
		[HttpGet("[action]")]
		public async Task<IActionResult> GetBomHistory(long? productionOrderItemId, long? partId, CancellationToken cn)
		{
			if (productionOrderItemId == null || productionOrderItemId == 0 || partId == null || partId == 0)
				return BadRequest("شناسه قلم سفارش ساخت یا کالا نمی‌تواند خالی باشد.");

			var forbid = ForbidBomUnless(await CanReadBomAsync(productionOrderItemId.Value, cn), "شما دسترسی مشاهده BOM اقلام سفارش ساخت را ندارید");
			if (forbid != null) return forbid;

			var rows = await unitOfWork.Repository<ProductionOrderItemBomHistory>()
				.TableNoTracking
				.Where(h => h.ProductionOrderItemId == productionOrderItemId.Value && h.PartId == partId.Value && !h.IsDeleted)
				.OrderByDescending(h => h.Revision)
				.ThenByDescending(h => h.CreatedOnMiladiDateTime)
				.ThenByDescending(h => h.Id)
				.Select(h => new
				{
					h.Id,
					h.Revision,
					EventType = (int)h.EventType,
					h.Amount,
					h.Description,
					h.SaleUnitDetails,
					h.NeedsAVL,
					h.NeedsInspection,
					ProductionStep = (int)h.ProductionStep,
					h.NumberSupplied,
					Status = (int)h.Status,
					h.CreatedByName,
					h.CreatedOnShamsiDateTime
				})
				.ToListAsync(cn);

			return Ok(rows);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public async Task<IActionResult> New(CancellationToken cn)
		{
			var newEntity = new ProductionOrderItem();
			await PopulateEditButtonFlagsAsync(newEntity, isNew: true, cn);
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
		// TODO: محدودیت پلتفرمی مشابه ProductionOrderController.FetchData - Repository.FetchDataAsync در
		// Data/Repositories/Repository.cs:746 فعلاً NotImplementedException می‌دهد (تایید شد، بدون override/جایگزین
		// در کل پروژه). GetRowSecurityPredicate() زیر آماده است تا وقتی موتور FetchData رفع شد، AND شود؛ در آن زمان
		// شرط‌های OR شده برای دیدن کارتابل نقش‌محور (مثلاً AcceptEngineeringMechanical روی اقلام در وضعیت
		// MechanicalEngineeringApprovalPending) نیز طبق پلن قابل افزودن است اما چون این متد در Runtime اصلاً
		// اجرا نمی‌شود، از افزودن منطق OR اضافه (nice-to-have) در این نسخه صرف‌نظر شد تا کدِ بلااستفاده تکراری
		// ایجاد نشود.
		_ = GetRowSecurityPredicate();
		return Ok(await unitOfWork.Repository<ProductionOrderItem>().FetchDataAsync(request, cn));
	}

	#region Row-Level Security

	/// <summary>
	/// نگاه کن به ProductionOrderController.GetRowSecurityPredicate برای شرح کامل محدودیت پلتفرمی.
	/// </summary>
	private Expression<Func<ProductionOrderItem, bool>> GetRowSecurityPredicate()
	{
		//Seed Role => Sale.ProductionOrder.ShowAll => سفارش ساخت - نمایش همه سفارش‌ها (مشترک با هدر)
		// TODO(Role): مشابه ProductionOrderController؛ کاربران این نقش همه اقلام را می‌بینند
		if (IsAdministrator || CurrentUserHasAnyRole("Sale.ProductionOrder.ShowAll"))
			return _ => true;

		var currentUserId = CurrentUserId;

		return item =>
			item.ProductionOrder != null &&
			(item.ProductionOrder.CreatedById == currentUserId ||
			 item.ProductionOrder.SalesExpertId == currentUserId ||
			 item.ProductionOrder.SalesManagerId == currentUserId ||
			 item.ProductionOrder.AlternativeExpertId == currentUserId ||
			 item.ProductionOrder.ProjectManagerId == currentUserId);
	}

	#endregion

	#region Engineering / Industrial / Project Manager / Supply Committee Workflow Actions

	// قواعد مشترک با App.BackgroundJob (لیست DeviceType های برقی، مقصد پس از تایید مهندسی و ...) در یک نقطه:
	// Entities.App.Sale.ProductionOrderItemWorkflowRules — تا کنترلر و Job از هم فاصله نگیرند.
	private static HashSet<ProductionOrderItemDeviceTypeEnum> ElectricalDeviceTypes => ProductionOrderItemWorkflowRules.ElectricalDeviceTypes;

	private static ProductionOrderItemCheckStatusEnum[] IndustrialDashboardStatuses => ProductionOrderItemWorkflowRules.IndustrialDashboardStatuses;

	/// <summary>
	/// تایید/رد/نیاز به بازنگری مهندسی (مکانیک یا برق - بر اساس DeviceType قلم تشخیص داده می‌شود).
	/// یادداشت سازگاری Enum (برای هماهنگی با فاز رفع باگ Job): ProductionOrderItemCheckStatusEnum فقط عضو
	/// مکانیکی را برای "در انتظار/تایید/عدم‌تایید مهندسی" دارد (2195/2197/2198) و معادل برقی آن (2196/2199/2200)
	/// فقط در ProductionOrderItemProductionStatusEnum تعریف شده است. بنابراین CheckStatus برای هر دو رشته از
	/// همان کدهای مکانیکی استفاده می‌کند (تشخیص رشته صرفاً از روی DeviceType انجام می‌شود)، اما کامنت تاریخچه
	/// (ProductionStatus) برای اقلام برقی مستقیماً از عضو برقی صحیح enum پر می‌شود تا تاریخچه گمراه‌کننده نباشد.
	/// </summary>
	[HttpPost("[action]/{id}")]
	[ActionDisplayName("تایید/رد مهندسی", ActionAccessType.Api, ActionAccessItemType.Custom)]
	public async Task<IActionResult> AcceptEngineering(long id, ProductionOrderItemWorkflowDecisionEnum decision, string? comment, CancellationToken cn)
	{
		try
		{
			var item = await unitOfWork.Repository<ProductionOrderItem>()
				.Table
				.Include(x => x.Part)
				.Include(x => x.ProductionOrder).ThenInclude(po => po!.ProjectManager)
				.Include(x => x.ProductionOrder).ThenInclude(po => po!.Contract).ThenInclude(ct => ct!.Customer).ThenInclude(cu => cu!.Party)
				.FirstOrDefaultAsync(x => x.Id == id, cn);

			if (item == null)
				return BadRequest("قلم سفارش ساخت یافت نشد");

			var isElectrical = item.DeviceType.HasValue && ElectricalDeviceTypes.Contains(item.DeviceType.Value);

			if (!await IsCurrentUserEquipmentConfirmerAsync(item, cn) && !HasEngineeringDisciplineRole(item))
				return Unauthorized("شما تاییدکننده مهندسی این نوع تجهیز نیستید");

			if (item.CheckStatus != ProductionOrderItemCheckStatusEnum.MechanicalEngineeringApprovalPending)
				return BadRequest("این قلم در وضعیت انتظار تایید مهندسی نیست");

			var now = DateTime.Now;
			ProductionOrderItemCheckStatusEnum newCheckStatus;
			ProductionOrderItemProductionStatusEnum productionStatus;
			string defaultComment;

			switch (decision)
			{
				case ProductionOrderItemWorkflowDecisionEnum.Approve:
					newCheckStatus = ProductionOrderItemCheckStatusEnum.EngineeringConfirmationMechanical;
					productionStatus = isElectrical
						? ProductionOrderItemProductionStatusEnum.ElectricalEngineeringApproved
						: ProductionOrderItemProductionStatusEnum.MechanicalEngineeringApproved;
					defaultComment = isElectrical ? "تایید مهندسی برق" : "تایید مهندسی مکانیک";
					break;

				case ProductionOrderItemWorkflowDecisionEnum.Reject:
					newCheckStatus = ProductionOrderItemCheckStatusEnum.MechanicalEngineeringNotApproved;
					productionStatus = isElectrical
						? ProductionOrderItemProductionStatusEnum.ElectricalEngineeringRejected
						: ProductionOrderItemProductionStatusEnum.MechanicalEngineeringRejected;
					defaultComment = isElectrical ? "عدم تایید مهندسی برق" : "عدم تایید مهندسی مکانیک";
					break;

				case ProductionOrderItemWorkflowDecisionEnum.ReviseNeeded:
					newCheckStatus = ProductionOrderItemCheckStatusEnum.EngineeringReassessmentNeeded;
					productionStatus = ProductionOrderItemProductionStatusEnum.EngineeringRejectedNeedsRevision;
					defaultComment = "عدم تایید مهندسی و نیاز به بازنگری";
					break;

				default:
					return BadRequest("نوع تصمیم نامعتبر است");
			}

			item.CheckStatus = newCheckStatus;
			item.CheckStatusChangedOnMiladiDate = now;
			item.CheckStatusChangedOnShamsiDate = now.ToShamsiDateTime();

			var itemComment = new ProductionOrderItemComment
			{
				ProductionOrderItemId = id,
				ProductionStatus = productionStatus,
				Comment = comment.HasValue() ? comment : defaultComment,
				StartMiladiDateTime = now,
				StartShamsiDate = now.ToShamsiDateTime()
			};

			await unitOfWork.Repository<ProductionOrderItem>().UpdateAsync(item, cn, true);
			await unitOfWork.Repository<ProductionOrderItemComment>().AddAsync(itemComment, cn, true);

			// معادل HTS ProductionOrderItemCommentService.AddCommentAndSendNotification — زنجیره خودکار پس از نظر مهندسی:
			//  ۲۱۹۷ تایید → (غیرروتین و ساخت داخل) یا پیشوند کالای ساخت داخل → صنایع داخلی ۱۹۰۴، وگرنه → کمیته تامین ۲۲۰۲
			//  ۲۱۹۸ عدم تایید → عدم تایید مهندسی و نیاز به بازنگری ۲۲۰۱ (بازگشت به فروش)
			switch (decision)
			{
				case ProductionOrderItemWorkflowDecisionEnum.Approve:
				{
					var next = ProductionOrderItemWorkflowRules.ResolveAfterEngineeringApproval(item.IsRoutine, item.IsBuildInside, item.Part?.Code);
					var goesToIndustrial = next == ProductionOrderItemCheckStatusEnum.IndustrialDashboardInternal;
					await ApplyAutoTransitionAsync(
						item,
						next,
						goesToIndustrial ? "ارسال خودکار به کارتابل صنایع پس از تایید مهندسی" : "ارسال خودکار به رئیس کمیته تامین پس از تایید مهندسی",
						null,
						now,
						cn);
					await QueueCheckStatusNotificationAsync(item, next, itemComment.Comment, cn);
					break;
				}
				case ProductionOrderItemWorkflowDecisionEnum.Reject:
					await ApplyAutoTransitionAsync(
						item,
						ProductionOrderItemWorkflowRules.AfterEngineeringReject,
						"عدم تایید مهندسی و نیاز به بازنگری (خودکار)",
						null,
						now,
						cn);
					await QueueCheckStatusNotificationAsync(item, ProductionOrderItemWorkflowRules.AfterEngineeringReject, itemComment.Comment, cn);
					break;
				case ProductionOrderItemWorkflowDecisionEnum.ReviseNeeded:
					await QueueCheckStatusNotificationAsync(item, newCheckStatus, itemComment.Comment, cn);
					break;
			}

			await unitOfWork.SaveChangesAsync(cn);

			return Ok(item);
		}
		catch (Exception ex)
		{
			return StatusCode(500, "خطا در تایید/رد مهندسی: " + ex.Message);
		}
	}

		public class AcceptsEngineeringViewModel
		{
			public long[] PrimaryKeyValues { get; set; } = [];
			public ProductionOrderItemWorkflowDecisionEnum decision { get; set; }
			public string? comment { get; set; }

		}
		[HttpPost("[action]")]
		[ActionDisplayName("تایید/رد مهندسی تجمیعی", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> AcceptsEngineering(AcceptsEngineeringViewModel model ,CancellationToken cn)
		{
			try
			{
				var items = new List<dynamic>();
				 foreach (var ids in model.PrimaryKeyValues)
				{
					var item =  await AcceptEngineering(ids,model.decision,model.comment,cn);
					var resutl = new
					{
						id = ids,
						value = item
					};
					items.Add(resutl);
				}

				return Ok(items);
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در تایید/رد مهندسی: " + ex.Message);
			}
		}

	// «تایید صنایع» (AcceptIndustrial) حذف شد: در HTS معادلی ندارد — واحد صنایع فقط قلم ۱۹۰۴/۱۹۰۵ را ویرایش می‌کند
	// (مرحله ساخت، سریال، تاریخ‌ها) و همان ویرایش «اولین تغییر صنایع» را ثبت می‌کند (Update).

	[HttpPost("[action]/{id}")]
	[ActionDisplayName("تایید/رد مدیر پروژه", ActionAccessType.Api, ActionAccessItemType.Custom)]
	public async Task<IActionResult> AcceptProjectManager(long id, ProductionOrderItemWorkflowDecisionEnum decision, string? comment, CancellationToken cn)
	{
		try
		{
			//Seed Role => Sale.ProductionOrderItem.AcceptProjectManager => سفارش ساخت - تایید مدیر پروژه
			// TODO(Role): Role را در پنل مدیریت نقش‌ها ایجاد و مدیران پروژه را به آن انتساب بده
			if (!IsAdministrator && !CurrentUserHasAnyRole("Sale.ProductionOrderItem.AcceptProjectManager"))
				return Unauthorized("شما دسترسی تایید مدیر پروژه را ندارید");

			var item = await unitOfWork.Repository<ProductionOrderItem>()
				.Table
				.Include(x => x.Part)
				.Include(x => x.ProductionOrder).ThenInclude(po => po!.ProjectManager)
				.Include(x => x.ProductionOrder).ThenInclude(po => po!.Contract).ThenInclude(ct => ct!.Customer).ThenInclude(cu => cu!.Party)
				.FirstOrDefaultAsync(x => x.Id == id, cn);

			if (item == null)
				return BadRequest("قلم سفارش ساخت یافت نشد");

			if (item.CheckStatus != ProductionOrderItemCheckStatusEnum.AwaitingProjectManagerApproval)
				return BadRequest("این قلم در وضعیت انتظار تایید مدیر پروژه نیست");

			var now = DateTime.Now;
			ProductionOrderItemCheckStatusEnum newCheckStatus;
			string defaultComment;

			switch (decision)
			{
				case ProductionOrderItemWorkflowDecisionEnum.Approve:
					newCheckStatus = ProductionOrderItemCheckStatusEnum.ProjectManagerApproval;
					defaultComment = "تایید مدیر پروژه";
					break;

				case ProductionOrderItemWorkflowDecisionEnum.Reject:
					newCheckStatus = ProductionOrderItemCheckStatusEnum.ProjectManagerUnapproved;
					defaultComment = "عدم تایید مدیر پروژه";
					break;

				case ProductionOrderItemWorkflowDecisionEnum.ReviseNeeded:
					newCheckStatus = ProductionOrderItemCheckStatusEnum.ProjectManagerApprovalPending;
					defaultComment = "عدم تایید مدیر پروژه و نیاز به بازنگری";
					break;

				default:
					return BadRequest("نوع تصمیم نامعتبر است");
			}

			item.CheckStatus = newCheckStatus;
			item.CheckStatusChangedOnMiladiDate = now;
			item.CheckStatusChangedOnShamsiDate = now.ToShamsiDateTime();

			var itemComment = new ProductionOrderItemComment
			{
				ProductionOrderItemId = id,
				ProductionStatus = (ProductionOrderItemProductionStatusEnum)newCheckStatus,
				Comment = comment.HasValue() ? comment : defaultComment,
				StartMiladiDateTime = now,
				StartShamsiDate = now.ToShamsiDateTime()
			};

			await unitOfWork.Repository<ProductionOrderItem>().UpdateAsync(item, cn, true);
			await unitOfWork.Repository<ProductionOrderItemComment>().AddAsync(itemComment, cn, true);

			// معادل HTS AddCommentAndSendNotification:
			//  ۲۲۵۹ تایید مدیر پروژه → زنجیره خودکار به «در انتظار تایید مهندسی (مکانیک)» ۲۱۹۵ (قلم کارتابل مهندسی را رد نمی‌کند)
			//  ۲۲۶۰ عدم تایید → «عدم تایید مدیر پروژه و نیاز به بازنگری» ۲۲۶۱ (بازگشت به فروش)
			switch (decision)
			{
				case ProductionOrderItemWorkflowDecisionEnum.Approve:
					await ApplyAutoTransitionAsync(
						item,
						ProductionOrderItemWorkflowRules.AfterProjectManagerApproval,
						"ارسال خودکار به کارتابل مهندسی پس از تایید مدیر پروژه",
						ProductionOrderItemProductionStepEnum.AwaitingEngineering,
						now,
						cn);
					await QueueCheckStatusNotificationAsync(item, ProductionOrderItemWorkflowRules.AfterProjectManagerApproval, itemComment.Comment, cn);
					break;
				case ProductionOrderItemWorkflowDecisionEnum.Reject:
					await ApplyAutoTransitionAsync(
						item,
						ProductionOrderItemWorkflowRules.AfterProjectManagerReject,
						"عدم تایید مدیر پروژه و نیاز به بازنگری (خودکار)",
						null,
						now,
						cn);
					await QueueCheckStatusNotificationAsync(item, ProductionOrderItemWorkflowRules.AfterProjectManagerReject, itemComment.Comment, cn);
					break;
				case ProductionOrderItemWorkflowDecisionEnum.ReviseNeeded:
					await QueueCheckStatusNotificationAsync(item, newCheckStatus, itemComment.Comment, cn);
					break;
			}

			await unitOfWork.SaveChangesAsync(cn);

			return Ok(item);
		}
		catch (Exception ex)
		{
			return StatusCode(500, "خطا در تایید/رد مدیر پروژه: " + ex.Message);
		}
	}

	// «ارسال به استعلام» و «ارسال به بررسی وزارت صنایع» از کارتابل صنایع حذف شدند: در HTS فقط رئیس کمیته تامین
	// (روی ۲۲۰۲/۱۹۰۹) می‌تواند قلم را به استعلام (۱۹۰۶) یا وزارت (۱۹۰۸) بفرستد — CommitteeDecide.

	/// <summary>
	/// معادل HTS DoInquiryOperation با StatusId=1909: استعلام‌گر (روی ۱۹۰۶) یا استعلام‌گر وزارت صنایع (روی ۱۹۰۸)
	/// نتیجه استعلام را با کامنت/پیوست ثبت و قلم را به رئیس کمیته تامین برمی‌گرداند.
	/// نگاشت نقش↔وضعیت عین شرط دکمه HTS: Pln_Inquirer ↔ 1906، Pln_MinistryOfIndustryInquirer ↔ 1908.
	/// </summary>
	[HttpPost("[action]/{id}")]
	[ActionDisplayName("ارسال نتیجه استعلام به کمیته تامین", ActionAccessType.Api, ActionAccessItemType.Custom)]
	public async Task<IActionResult> SendToSupplyCommittee(long id, ProductionOrderItemInquiryRequest? request, CancellationToken cn)
	{
		var currentStatus = await unitOfWork.Repository<ProductionOrderItem>()
			.TableNoTracking
			.Where(x => x.Id == id)
			.Select(x => x.CheckStatus)
			.FirstOrDefaultAsync(cn);

		if (currentStatus == null)
			return BadRequest("قلم سفارش ساخت یافت نشد");

		//Seed Role => Sale.ProductionOrderItem.Inquirer => سفارش ساخت - استعلام (روی ۱۹۰۶)
		//Seed Role => Sale.ProductionOrderItem.MinistryOfIndustryInquirer => سفارش ساخت - استعلام وزارت صنایع (روی ۱۹۰۸)
		var allowed = IsAdministrator
			|| (currentStatus == ProductionOrderItemCheckStatusEnum.InquiryNeeded && CurrentUserHasAnyRole("Sale.ProductionOrderItem.Inquirer"))
			|| (currentStatus == ProductionOrderItemCheckStatusEnum.IndustrialMinistryReviewRequired && CurrentUserHasAnyRole("Sale.ProductionOrderItem.MinistryOfIndustryInquirer"));

		if (!allowed)
			return Unauthorized("شما دسترسی ارسال نتیجه استعلام این قلم به کمیته تامین را ندارید");

		var comment = request?.Comment.HasValue() == true ? request!.Comment! : "ارسال نتیجه استعلام به رئیس کمیته تامین";

		return await TransitionCheckStatusAsync(
			id,
			ProductionOrderItemWorkflowRules.InquiryStatuses,
			ProductionOrderItemCheckStatusEnum.SendToSupplyCommitteeChair,
			comment,
			newProductionStep: null,
			invalidStateMessage: "این قلم در وضعیت استعلام (۱۹۰۶/۱۹۰۸) نیست",
			cn,
			inquiry: new InquiryDetails(null, null, request?.AttachmentFileId));
	}

	/// <summary>
	/// معادل HTS: تصمیم رئیس کمیته تامین روی قلم ۲۲۰۲/۱۹۰۹ (کمبوی «تغییر وضعیت» با مجوز Pln_SupplyCommitteeBoss).
	/// مقصد یکی از: خرید داخلی ۱۹۰۴ / خرید خارجی ۱۹۰۵ / نیاز به استعلام ۱۹۰۶ / نیاز به بررسی وزارت صنایع ۱۹۰۸.
	/// برای ۱۹۰۶/۱۹۰۸ مسئول استعلام و LeadTime الزامی است (ردیف Pln_ProductionOrderItemInquiry در HTS).
	/// </summary>
	[HttpPost("[action]/{id}")]
	[ActionDisplayName("تصمیم کمیته تامین", ActionAccessType.Api, ActionAccessItemType.Custom)]
	public async Task<IActionResult> CommitteeDecide(long id, ProductionOrderItemCommitteeDecisionRequest request, CancellationToken cn)
	{
		//Seed Role => Sale.ProductionOrderItem.SupplyCommitteeBoss => سفارش ساخت - رئیس کمیته تامین
		if (!IsAdministrator && !CurrentUserHasAnyRole("Sale.ProductionOrderItem.SupplyCommitteeBoss"))
			return Unauthorized("شما دسترسی تصمیم کمیته تامین را ندارید");

		if (!ProductionOrderItemWorkflowRules.CommitteeDecisionDestinations.Contains(request.Destination))
			return BadRequest("مقصد انتخاب‌شده برای تصمیم کمیته تامین معتبر نیست");

		var isInquiry = ProductionOrderItemWorkflowRules.IsInquiryStatus(request.Destination);
		if (isInquiry && (request.ResponsibleId == null || request.ResponsibleId <= 0))
			return BadRequest("برای ارسال به استعلام، مسئول استعلام الزامی است");

		DateTime? leadTime = null;
		if (request.LeadTimeShamsiDate.HasValue())
		{
			try { leadTime = request.LeadTimeShamsiDate!.ToMiladiDate(); }
			catch { return BadRequest("تاریخ مدت زمان اخذ استعلام معتبر نیست"); }
		}
		if (isInquiry && leadTime == null)
			return BadRequest("برای ارسال به استعلام، مدت زمان اخذ استعلام (LeadTime) الزامی است");

		var defaultComment = request.Destination switch
		{
			ProductionOrderItemCheckStatusEnum.IndustrialDashboardInternal => "تصمیم کمیته تامین: خرید/ساخت داخلی",
			ProductionOrderItemCheckStatusEnum.ForeignIndustryCatalog => "تصمیم کمیته تامین: خرید خارجی",
			ProductionOrderItemCheckStatusEnum.InquiryNeeded => "تصمیم کمیته تامین: نیاز به استعلام",
			_ => "تصمیم کمیته تامین: نیاز به بررسی وزارت صنایع"
		};

		var result = await TransitionCheckStatusAsync(
			id,
			ProductionOrderItemWorkflowRules.CommitteeChairStatuses,
			request.Destination,
			request.Comment.HasValue() ? request.Comment! : defaultComment,
			newProductionStep: null,
			invalidStateMessage: "این قلم در وضعیت انتظار بررسی رئیس کمیته تامین نیست",
			cn,
			inquiry: new InquiryDetails(request.ResponsibleId, leadTime, request.AttachmentFileId));

		// معادل HTS ProductionOrderItemInquiryService.CheckAndUpdatePermissions: مسئول استعلامی که رئیس کمیته انتخاب
		// می‌کند خودکار به گروه ۳۸۱ (استعلام‌گر) یا ۳۸۲ (استعلام‌گر وزارت) اضافه می‌شد تا بتواند نتیجه را ثبت کند.
		// در پنل معادلش انتساب نقش Inquirer / MinistryOfIndustryInquirer است.
		if (isInquiry && result is OkObjectResult && request.ResponsibleId is > 0)
			await EnsureInquiryResponsibleRoleAsync(request.ResponsibleId.Value, request.Destination, cn);

		return result;
	}

	private async Task EnsureInquiryResponsibleRoleAsync(long responsibleUserId, ProductionOrderItemCheckStatusEnum destination, CancellationToken cn)
	{
		try
		{
			var roleName = destination == ProductionOrderItemCheckStatusEnum.IndustrialMinistryReviewRequired
				? "Sale.ProductionOrderItem.MinistryOfIndustryInquirer"
				: "Sale.ProductionOrderItem.Inquirer";

			var role = await dbContext.Set<Entities.Auth.Role>().AsNoTracking().FirstOrDefaultAsync(r => r.Name == roleName, cn);
			if (role?.Id is null or 0)
				return;

			var user = await userService.GetById(responsibleUserId);
			if (user == null)
				return;

			user.RoleIds ??= [];
			user.Roles ??= [];
			if (user.RoleIds.Contains(role.Id.Value) || user.Roles.Contains(roleName, StringComparer.OrdinalIgnoreCase))
				return;

			user.RoleIds.Add(role.Id.Value);
			user.Roles.Add(roleName);
			await userService.AddAndUpdateUserAsync(user, cn);

			try { await onlineUserService.RefreshUserRoleAccessesAsync(responsibleUserId, cn); }
			catch { /* نقش در دیتابیس ذخیره شده؛ کش کاربر آنلاین با ورود بعدی تازه می‌شود */ }
		}
		catch
		{
			// شکست انتساب نقش نباید ثبت تصمیم کمیته را خراب کند (ادمین می‌تواند نقش را دستی بدهد)
		}
	}

	// «تایید کمیته تامین» / «رد کمیته تامین» حذف شدند: در HTS رئیس کمیته فقط مقصد انتخاب می‌کند (CommitteeDecide) و
	// «رد» یا «تایید ساده» وجود ندارد. منسوخ‌سازی قلم فقط از مسیر منسوخ‌سازی سربرگ (DoObsolete) انجام می‌شود.

	/// <summary>
	/// فرم مودال تصمیم کمیته / ارسال نتیجه استعلام (مسئول، LeadTime، پیوست، کامنت) — معادل پنجره «تغییر وضعیت» HTS در حالت استعلام.
	/// </summary>
	[HttpGet("[action]")]
	public IActionResult ProductionOrderItemInquiryPartial(long productionOrderItemId, bool committeeMode)
	{
		var item = unitOfWork.Repository<ProductionOrderItem>()
			.TableNoTracking
			.Where(c => c.Id == productionOrderItemId)
			.Select(c => new { c.Id, c.CheckStatus })
			.FirstOrDefault();

		if (item == null)
			return BadRequest("قلم سفارش ساخت یافت نشد");

		ViewBag.CommitteeMode = committeeMode;
		ViewBag.CurrentCheckStatus = item.CheckStatus;
		return PartialView(@"\Views\Panel\Sale\ProductionOrderItem\_ProductionOrderItemInquiryPartial.cshtml",
			new ProductionOrderItemInquiry { ProductionOrderItemId = productionOrderItemId });
	}

	/// <summary>
	/// معادل HTS LoadSparePartPage / GetSpareParts:
	/// قطعات یدکی کالای قلم + قطعات یدکی اقلام مصرفی پروژه همان سریال
	/// (برگشت از مصرف VchTypeId=7 جفت‌حذف می‌شود).
	/// </summary>
	[HttpGet("[action]")]
	public IActionResult SparePartListPartial(long productionOrderItemId)
	{
		return PartialView(@"\Views\Panel\Sale\ProductionOrderItem\_SparePartListPartial.cshtml",
			GetSpareParts(productionOrderItemId));
	}

	private List<ProductionOrderItemSparePartRow> GetSpareParts(long productionOrderItemId)
	{
		var item = unitOfWork.Repository<ProductionOrderItem>()
			.TableNoTracking
			.Include(c => c.Part)
			.FirstOrDefault(c => c.Id == productionOrderItemId);
		if (item == null)
			return new List<ProductionOrderItemSparePartRow>();

		var consumed = new List<(long PartId, double Qty, string? DlTitle)>();
		if (!string.IsNullOrWhiteSpace(item.Serial))
		{
			var partName = item.Part?.Name ?? "";
			var isDryer = partName.Contains("درایر");
			var isGenerator = partName.Contains("ژنر");
			var serial = item.Serial;

			var dls = unitOfWork.Repository<DL>()
				.TableNoTracking
				.Where(p => p.Title != null && p.Title.Contains("/"))
				.Select(p => new { p.Id, p.Title })
				.AsEnumerable()
				.Where(p =>
				{
					var title = p.Title.ToLowerInvariant();
					return (isDryer && title.Contains("dr"))
						|| (isGenerator && title.Contains("psa"))
						|| title.Contains("com");
				})
				.Where(p =>
				{
					var parts = p.Title.Split('/');
					var compareIndex = serial.Contains("/") ? parts.Length - 2 : parts.Length - 1;
					if (compareIndex < 0 || compareIndex >= parts.Length)
						return false;
					return parts[compareIndex] == serial;
				})
				.Select(p => p.Id)
				.ToArray();

			if (dls.Length > 0)
			{
				var materials = unitOfWork.Repository<ProjectUtilizedMaterial>()
					.TableNoTracking
					.Where(p => p.DLId != null && dls.Contains(p.DLId.Value) && p.PartId != null)
					.Select(p => new
					{
						p.Id,
						p.VchTypeId,
						PartId = p.PartId!.Value,
						p.Qty,
						DlTitle = p.DL != null ? p.DL.Title : null
					})
					.ToList();

				var returned = materials.Where(p => p.VchTypeId == 7).ToList();
				var removeIds = returned.Select(s => s.Id).ToHashSet();
				foreach (var r in returned)
				{
					var pair = materials.FirstOrDefault(f =>
						f.PartId == r.PartId && f.VchTypeId != r.VchTypeId && f.Qty == r.Qty);
					if (pair != null)
						removeIds.Add(pair.Id);
				}

				consumed = materials
					.Where(p => !removeIds.Contains(p.Id))
					.GroupBy(p => p.PartId)
					.Select(g => (g.Key, g.Sum(s => s.Qty), g.First().DlTitle))
					.ToList();
			}
		}

		var partIdsForSpare = new List<long>();
		if (item.PartId is > 0)
			partIdsForSpare.Add(item.PartId.Value);
		partIdsForSpare.AddRange(consumed.Select(c => c.PartId));
		partIdsForSpare = partIdsForSpare.Distinct().ToList();

		var spareRows = partIdsForSpare.Count == 0
			? new List<PartSparePart>()
			: unitOfWork.Repository<PartSparePart>()
				.TableNoTracking
				.Include(c => c.SparePart)
				.Where(c => partIdsForSpare.Contains(c.PartId) && c.IsActive == IsActiveEnum.Active)
				.ToList();

		var final = new List<(long SparePartId, ProductionOrderItemSparePartRow Row)>();

		if (item.PartId is > 0)
		{
			foreach (var part in spareRows.Where(c => c.PartId == item.PartId && c.SparePartId is > 0).OrderBy(c => c.SparePart?.Code))
			{
				final.Add((part.SparePartId!.Value, new ProductionOrderItemSparePartRow
				{
					Code = part.SparePart?.Code,
					Name = part.SparePart?.Name,
					WorkingHours = part.WorkingHours,
					Description = part.Description
				}));
			}
		}

		foreach (var consumedPart in consumed)
		{
			foreach (var part in spareRows.Where(c => c.PartId == consumedPart.PartId && c.SparePartId is > 0))
			{
				final.Add((part.SparePartId!.Value, new ProductionOrderItemSparePartRow
				{
					Code = part.SparePart?.Code,
					Name = part.SparePart?.Name,
					WorkingHours = part.WorkingHours,
					Description = part.Description,
					Qty = consumedPart.Qty,
					DlTitle = consumedPart.DlTitle
				}));
			}
		}

		return final
			.GroupBy(p => p.SparePartId)
			.Select(g => g.First().Row)
			.ToList();
	}

	/// <summary>
	/// کمک‌کننده مشترک برای اکشن‌های ساده تغییر CheckStatus (استعلام/وزارت صنایع/کمیته تامین) تا از تکرار
	/// کد بین این اکشن‌های مشابه پرهیز شود (اصل DRY).
	/// </summary>
	private async Task<IActionResult> TransitionCheckStatusAsync(
		long id,
		IReadOnlyCollection<ProductionOrderItemCheckStatusEnum> validPreStates,
		ProductionOrderItemCheckStatusEnum newCheckStatus,
		string comment,
		ProductionOrderItemProductionStepEnum? newProductionStep,
		string invalidStateMessage,
		CancellationToken cn,
		InquiryDetails? inquiry = null)
	{
		try
		{
			var item = await unitOfWork.Repository<ProductionOrderItem>()
				.Table
				.Include(x => x.Part)
				.Include(x => x.ProductionOrder).ThenInclude(po => po!.ProjectManager)
				.Include(x => x.ProductionOrder).ThenInclude(po => po!.Contract).ThenInclude(ct => ct!.Customer).ThenInclude(cu => cu!.Party)
				.FirstOrDefaultAsync(x => x.Id == id, cn);

			if (item == null)
				return BadRequest("قلم سفارش ساخت یافت نشد");

			if (item.CheckStatus == null || !validPreStates.Contains(item.CheckStatus.Value))
				return BadRequest(invalidStateMessage);

			var now = DateTime.Now;
			var wasInIndustrialDashboard = item.CheckStatus.HasValue
				&& IndustrialDashboardStatuses.Contains(item.CheckStatus.Value);

			item.CheckStatus = newCheckStatus;
			item.CheckStatusChangedOnMiladiDate = now;
			item.CheckStatusChangedOnShamsiDate = now.ToShamsiDateTime();
			TrySetSendToIndustrial(item, newCheckStatus, now);

			// اولین اقدام صنایع از کارتابل صنایع (استعلام / وزارت / کمیته و ...)
			if (wasInIndustrialDashboard)
				TrySetFirstIndustrialChange(item, now, CurrentUserId);

			if (newProductionStep.HasValue)
				item.ProductionStep = newProductionStep.Value;

			// تب «وضعیت سفارش ساخت»
			var itemComment = new ProductionOrderItemComment
			{
				ProductionOrderItemId = id,
				ProductionStatus = (ProductionOrderItemProductionStatusEnum)newCheckStatus,
				Comment = comment,
				StartMiladiDateTime = now,
				StartShamsiDate = now.ToShamsiDateTime(),
				IsForProductionMode = false,
				IsForProductionStepStatus = false
			};

			await unitOfWork.Repository<ProductionOrderItem>().UpdateAsync(item, cn, true);
			await unitOfWork.Repository<ProductionOrderItemComment>().AddAsync(itemComment, cn, true);

			if (newProductionStep.HasValue)
			{
				// تب «مرحله ساخت» — مطابق HTS هنگام تغییر ProductionStep
				await unitOfWork.Repository<ProductionOrderItemComment>().AddAsync(new ProductionOrderItemComment
				{
					ProductionOrderItemId = id,
					ProductionStatus = (ProductionOrderItemProductionStatusEnum)newCheckStatus,
					ProductionStep = newProductionStep.Value,
					Comment = comment,
					StartMiladiDateTime = now,
					StartShamsiDate = now.ToShamsiDateTime(),
					IsForProductionMode = false,
					IsForProductionStepStatus = true
				}, cn, true);
			}

			ProductionOrderItemInquiry? inquiryRow = null;
			if (IsInquiryRelatedStatus(newCheckStatus) || inquiry != null)
			{
				// تب «وضعیت استعلام» — معادل ردیف Pln_ProductionOrderItemInquiry در HTS (DoInquiryOperation).
				// مسئول/LeadTime/پیوست فقط وقتی از فرم آمده باشند پر می‌شوند؛ LeadTime دیگر با «اکنون» جعل نمی‌شود
				// تا جاب انقضای استعلام (SendExpiredInquiryItemsNotification) مبنای درستی داشته باشد.
				string? responsibleName = null;
				if (inquiry?.ResponsibleId is > 0)
				{
					responsibleName = await dbContext.Users
						.AsNoTracking()
						.Where(u => u.Id == inquiry.ResponsibleId)
						.Select(u => u.Name)
						.FirstOrDefaultAsync(cn);
				}

				inquiryRow = new ProductionOrderItemInquiry
				{
					ProductionOrderItemId = id,
					Status = newCheckStatus,
					Comment = comment,
					ResponsibleId = inquiry?.ResponsibleId is > 0 ? inquiry.ResponsibleId : null,
					ResponsibleName = responsibleName,
					LeadTimeMiladiDate = inquiry?.LeadTime,
					LeadTimeShamsiDate = inquiry?.LeadTime?.ToShamsiDate(),
					AttachmentFileId = inquiry?.AttachmentFileId is > 0 ? inquiry.AttachmentFileId : null
				};
				await unitOfWork.Repository<ProductionOrderItemInquiry>().AddAsync(inquiryRow, cn, true);
			}

			await unitOfWork.SaveChangesAsync(cn);

			await QueueInquiryNotificationAsync(item, newCheckStatus, inquiryRow, comment, cn);

			return Ok(item);
		}
		catch (Exception ex)
		{
			return StatusCode(500, "خطا در تغییر وضعیت قلم: " + ex.Message);
		}
	}

	private static bool IsInquiryRelatedStatus(ProductionOrderItemCheckStatusEnum status) =>
		status is ProductionOrderItemCheckStatusEnum.InquiryNeeded
			or ProductionOrderItemCheckStatusEnum.IndustrialMinistryReviewRequired
			or ProductionOrderItemCheckStatusEnum.SendToSupplyCommitteeChair
			or ProductionOrderItemCheckStatusEnum.CommitteeHeadReviewPending;

	/// <summary>
	/// اطلاعات فرم استعلام (معادل ستون‌های Pln_ProductionOrderItemInquiry در HTS).
	/// </summary>
	private sealed record InquiryDetails(long? ResponsibleId, DateTime? LeadTime, long? AttachmentFileId);

	/// <summary>
	/// انتقال خودکار زنجیره‌ای (معادل کامنت دومی که HTS در AddCommentAndSendNotification با CreatedUserId همان کاربر
	/// می‌سازد): وضعیت بررسی قلم + تاریخ تغییر + (اختیاری) مرحله ساخت را عوض می‌کند و کامنت «وضعیت سفارش ساخت»
	/// (و در صورت تغییر مرحله، کامنت «مرحله ساخت») ثبت می‌کند. SaveChanges با فراخوان است.
	/// </summary>
	private async Task ApplyAutoTransitionAsync(
		ProductionOrderItem item,
		ProductionOrderItemCheckStatusEnum nextCheckStatus,
		string comment,
		ProductionOrderItemProductionStepEnum? nextProductionStep,
		DateTime now,
		CancellationToken cn)
	{
		item.CheckStatus = nextCheckStatus;
		item.CheckStatusChangedOnMiladiDate = now;
		item.CheckStatusChangedOnShamsiDate = now.ToShamsiDateTime();
		TrySetSendToIndustrial(item, nextCheckStatus, now);

		if (nextProductionStep.HasValue)
			item.ProductionStep = nextProductionStep.Value;

		await unitOfWork.Repository<ProductionOrderItem>().UpdateAsync(item, cn, true);

		await unitOfWork.Repository<ProductionOrderItemComment>().AddAsync(new ProductionOrderItemComment
		{
			ProductionOrderItemId = item.Id!.Value,
			ProductionStatus = (ProductionOrderItemProductionStatusEnum)nextCheckStatus,
			Comment = comment,
			StartMiladiDateTime = now,
			StartShamsiDate = now.ToShamsiDateTime(),
			IsForProductionMode = false,
			IsForProductionStepStatus = false
		}, cn, true);

		if (nextProductionStep.HasValue)
		{
			await unitOfWork.Repository<ProductionOrderItemComment>().AddAsync(new ProductionOrderItemComment
			{
				ProductionOrderItemId = item.Id!.Value,
				ProductionStatus = (ProductionOrderItemProductionStatusEnum)nextCheckStatus,
				ProductionStep = nextProductionStep.Value,
				Comment = comment,
				StartMiladiDateTime = now,
				StartShamsiDate = now.ToShamsiDateTime(),
				IsForProductionMode = false,
				IsForProductionStepStatus = true
			}, cn, true);
		}
	}

	#region Notifications (معادل SendNotification / SendEngineeringAndProjectManagerNotification در HTS)

	/// <summary>
	/// اعلان ایمیلی تغییر وضعیت بررسی قلم پس از اکشن کاربر (معادل ProductionOrderItemCommentService.SendNotification).
	/// گیرنده بر اساس وضعیت مقصد:
	///  ۲۱۹۵ → تاییدکننده مهندسی همان نوع دستگاه (Pln.ProductionOrderEquipmentConfirmer)
	///  ۲۲۵۸ → مدیر پروژه سفارش
	///  ۲۲۰۱ / ۲۲۶۱ → تیم فروش سفارش (ایجادکننده، کارشناس، جایگزین، مدیر فروش، مدیر پروژه)
	///  ۲۲۰۲ → رئیس/کارشناسان کمیته تامین؛ تیم فروش در CC
	///  ۱۹۰۴ → گروه صنایع
	/// شکست اعلان نباید عملیات اصلی را متوقف کند.
	/// </summary>
	private async Task QueueCheckStatusNotificationAsync(ProductionOrderItem item, ProductionOrderItemCheckStatusEnum status, string? comment, CancellationToken cn)
	{
		try
		{
			var po = item.ProductionOrder;
			var to = new List<string?>();
			var cc = new List<string?> { CurrentUserEmail };
			string intro;

			switch (status)
			{
				case ProductionOrderItemCheckStatusEnum.MechanicalEngineeringApprovalPending:
					intro = "سفارش ساختی با مشخصات ذیل جهت بررسی مهندسی به کارتابل شما ارسال گردیده است";
					to.AddRange(await GetEngineeringConfirmerEmailsAsync(item, cn));
					break;
				case ProductionOrderItemCheckStatusEnum.AwaitingProjectManagerApproval:
					intro = "سفارش ساختی با مشخصات ذیل جهت بررسی به کارتابل شما (مدیر پروژه) ارسال گردیده است";
					to.Add(po?.ProjectManager?.Email);
					if (!to.Any(e => e.HasValue()) && po?.ProjectManagerId is > 0)
						to.AddRange(await ProductionOrderItemNotificationHelper.GetUserEmailsAsync(dbContext, new[] { po.ProjectManagerId.Value }, cn));
					break;
				case ProductionOrderItemCheckStatusEnum.EngineeringReassessmentNeeded:
					intro = "سفارش ساختی با مشخصات ذیل پس از اعلام نظر تیم مهندسی و <span style='color:red'>عدم تایید بخشی از اطلاعات</span>، جهت بررسی مجدد به کارتابل شما ارسال گردیده است";
					to.AddRange(await GetSalesTeamEmailsAsync(po, cn));
					break;
				case ProductionOrderItemCheckStatusEnum.ProjectManagerApprovalPending:
					intro = "سفارش ساختی با مشخصات ذیل پس از اعلام نظر مدیر پروژه و <span style='color:red'>عدم تایید بخشی از اطلاعات</span>، جهت بررسی مجدد به کارتابل شما ارسال گردیده است";
					to.AddRange(await GetSalesTeamEmailsAsync(po, cn));
					break;
				case ProductionOrderItemCheckStatusEnum.CommitteeHeadReviewPending:
					intro = "سفارش ساختی با مشخصات ذیل <span style='color:red'>پس از اعلام نظر تیم مهندسی</span>، جهت بررسی پروسه تامین به کارتابل شما ارسال گردیده است";
					to.AddRange(await ProductionOrderItemNotificationHelper.GetRoleEmailsAsync(dbContext, "Sale.ProductionOrderItem.SupplyCommitteeBoss", cn));
					to.AddRange(await ProductionOrderItemNotificationHelper.GetGroupEmailsAsync(unitOfWork, ProductionOrderItemNotificationHelper.GroupSupplyCommitteeExperts, cn));
					cc.AddRange(await GetSalesTeamEmailsAsync(po, cn));
					break;
				case ProductionOrderItemCheckStatusEnum.IndustrialDashboardInternal:
				case ProductionOrderItemCheckStatusEnum.ForeignIndustryCatalog:
					intro = "اقلام سفارش ساختی با مشخصات ذیل جهت ادامه روند به کارتابل صنایع ارسال گردیده است";
					to.AddRange(await ProductionOrderItemNotificationHelper.GetGroupEmailsAsync(unitOfWork, ProductionOrderItemNotificationHelper.GroupIndustrial, cn));
					if (po?.ProjectManager?.Email.HasValue() == true)
						to.Add(po.ProjectManager.Email);
					break;
				default:
					return;
			}

			var body = ProductionOrderItemNotificationHelper.BuildEmailShell(
				$"بدینوسیله اعلام میگردد {intro}<br /><br /><ul>"
				+ ProductionOrderItemNotificationHelper.Li("شماره سفارش ساخت", po?.ProductionOrderNumber ?? (object?)po?.Number)
				+ ProductionOrderItemNotificationHelper.Li("مشتری", po?.Contract?.Customer?.Party?.FullName)
				+ ProductionOrderItemNotificationHelper.Li("بررسی کننده", CurrentUserFullNameFn ?? CurrentUserFullName)
				+ ProductionOrderItemNotificationHelper.Li("توضیحات", comment)
				+ ProductionOrderItemNotificationHelper.Li("کد کالا", item.Part?.Code)
				+ ProductionOrderItemNotificationHelper.Li("عنوان کالا", item.Part?.Name)
				+ ProductionOrderItemNotificationHelper.Li("تعداد/مقدار", item.Amount)
				+ ProductionOrderItemNotificationHelper.Li("تاریخ تحویل توافقی", item.AgreedDeliverDate?.ToShamsiDate())
				+ ProductionOrderItemNotificationHelper.Li("نسخه(رویژن)", item.Revision)
				+ ProductionOrderItemNotificationHelper.Li("وضعیت جدید", status.ToDisplay())
				+ "</ul>");

			await ProductionOrderItemNotificationHelper.QueueEmailAsync(
				unitOfWork,
				"اعلان تغییر وضعیت سفارش ساخت",
				body,
				to,
				cc,
				item.Id,
				CurrentUserId ?? 0,
				$"Panel/ProductionOrderItem/Edit/{item.Id}",
				cn);
		}
		catch
		{
			// شکست اعلان نباید عملیات گردش‌کار را متوقف کند
		}
	}

	/// <summary>
	/// معادل HTS ProductionOrderItemInquiryService.SendNotification پس از DoInquiryOperation:
	///  ۱۹۰۴/۱۹۰۵ (تصمیم کمیته) → گروه صنایع؛ ۱۹۰۶/۱۹۰۸ → مسئول استعلام + کارشناسان کمیته؛ ۱۹۰۹ → رئیس/کارشناسان کمیته.
	/// </summary>
	private async Task QueueInquiryNotificationAsync(ProductionOrderItem item, ProductionOrderItemCheckStatusEnum status, ProductionOrderItemInquiry? inquiry, string? comment, CancellationToken cn)
	{
		try
		{
			var po = item.ProductionOrder;
			var to = new List<string?>();
			var cc = new List<string?> { CurrentUserEmail };
			string intro;

			var isIndustrial = ProductionOrderItemWorkflowRules.IsIndustrialDashboard(status);
			var isInquiry = ProductionOrderItemWorkflowRules.IsInquiryStatus(status);
			var isToCommittee = status == ProductionOrderItemCheckStatusEnum.SendToSupplyCommitteeChair;

			if (isIndustrial)
			{
				intro = $"بنا به تشخیص رئیس کمیته تامین، اقلام ذیل بایستی بصورت <strong style='color:red'>{status.ToDisplay()}</strong> تامین شده و در چرخه تامین/برنامه ریزی قرار گیرند";
				to.AddRange(await ProductionOrderItemNotificationHelper.GetGroupEmailsAsync(unitOfWork, ProductionOrderItemNotificationHelper.GroupIndustrial, cn));
			}
			else if (isInquiry)
			{
				intro = $"بنا به تشخیص رئیس کمیته تامین، اقلام ذیل بایستی توسط <strong style='color:red'>{inquiry?.ResponsibleName}</strong> جهت تامین مورد استعلام قرار گرفته و نتیجه استعلام در کارتابل ثبت و اطلاع رسانی شود<br />"
					+ (status == ProductionOrderItemCheckStatusEnum.InquiryNeeded
						? "خاطر نشان میگردد، استعلام اقلام ذیل از تامین کننده داخلی / خارجی صورت می پذیرد"
						: "خاطر نشان میگردد اقلام ذیل، نیاز به تایید وزارت صنایع دارد");
				if (inquiry?.ResponsibleId is > 0)
					to.AddRange(await ProductionOrderItemNotificationHelper.GetUserEmailsAsync(dbContext, new[] { inquiry.ResponsibleId.Value }, cn));
				to.AddRange(await ProductionOrderItemNotificationHelper.GetGroupEmailsAsync(unitOfWork, ProductionOrderItemNotificationHelper.GroupSupplyCommitteeExperts, cn));
			}
			else if (isToCommittee)
			{
				intro = "استعلام اقلام ذیل گرفته شده و نتیجه تقدیم حضور می گردد";
				to.AddRange(await ProductionOrderItemNotificationHelper.GetRoleEmailsAsync(dbContext, "Sale.ProductionOrderItem.SupplyCommitteeBoss", cn));
				to.AddRange(await ProductionOrderItemNotificationHelper.GetGroupEmailsAsync(unitOfWork, ProductionOrderItemNotificationHelper.GroupSupplyCommitteeExperts, cn));
			}
			else
			{
				return;
			}

			var details = "<ul>"
				+ ProductionOrderItemNotificationHelper.Li("شماره سفارش ساخت", po?.ProductionOrderNumber ?? (object?)po?.Number)
				+ ProductionOrderItemNotificationHelper.Li("مشتری", po?.Contract?.Customer?.Party?.FullName)
				+ (isInquiry ? ProductionOrderItemNotificationHelper.Li("مسئول استعلام", inquiry?.ResponsibleName) + ProductionOrderItemNotificationHelper.Li("مدت زمان اخذ استعلام", inquiry?.LeadTimeShamsiDate) : string.Empty)
				+ ProductionOrderItemNotificationHelper.Li("کد کالا", item.Part?.Code)
				+ ProductionOrderItemNotificationHelper.Li("عنوان کالا", item.Part?.Name)
				+ ProductionOrderItemNotificationHelper.Li("تعداد/مقدار", item.Amount)
				+ ProductionOrderItemNotificationHelper.Li("کامنت آخر", comment)
				+ "</ul>";

			var body = ProductionOrderItemNotificationHelper.BuildEmailShell($"بدینوسیله به استحضار می رساند، {intro}<br /><br />{details}");

			await ProductionOrderItemNotificationHelper.QueueEmailAsync(
				unitOfWork,
				"اعلان استعلام تامین اقلام سفارش ساخت",
				body,
				to,
				cc,
				item.Id,
				CurrentUserId ?? 0,
				$"Panel/ProductionOrderItem/Edit/{item.Id}",
				cn);
		}
		catch
		{
			// شکست اعلان نباید عملیات گردش‌کار را متوقف کند
		}
	}

	private async Task<List<string?>> GetEngineeringConfirmerEmailsAsync(ProductionOrderItem item, CancellationToken cn)
	{
		if (!item.DeviceType.HasValue)
			return new List<string?>();

		var isElectrical = ProductionOrderItemWorkflowRules.IsElectrical(item.DeviceType);
		var confirmers = await unitOfWork.Repository<Entities.App.Pln.ProductionOrderEquipmentConfirmer>()
			.TableNoTracking
			.Where(c => c.DeviceType == item.DeviceType.Value)
			.Select(c => isElectrical ? c.ElectricalUserId : c.MechanicalUserId)
			.ToListAsync(cn);

		var userIds = confirmers.Where(id => id.HasValue).Select(id => id!.Value).ToList();
		return (await ProductionOrderItemNotificationHelper.GetUserEmailsAsync(dbContext, userIds, cn)).Cast<string?>().ToList();
	}

	private async Task<List<string?>> GetSalesTeamEmailsAsync(ProductionOrder? po, CancellationToken cn)
	{
		if (po == null)
			return new List<string?>();

		var ids = new List<long>();
		if (po.CreatedById is > 0) ids.Add(po.CreatedById.Value);
		if (po.SalesExpertId is > 0) ids.Add(po.SalesExpertId.Value);
		if (po.AlternativeExpertId is > 0) ids.Add(po.AlternativeExpertId.Value);
		if (po.SalesManagerId is > 0) ids.Add(po.SalesManagerId.Value);
		if (po.ProjectManagerId is > 0) ids.Add(po.ProjectManagerId.Value);

		return (await ProductionOrderItemNotificationHelper.GetUserEmailsAsync(dbContext, ids, cn)).Cast<string?>().ToList();
	}

	#endregion

	/// <summary>
	/// معادل HTS ReceivedDate (تاریخ اخذ): هنگام ورود به صنایع داخلی (1904) یا خارجی (1905)
	/// مثل EditItemStatusAndSendNotification / InquiryService همیشه با now ست می‌شود.
	/// </summary>
	private static void TrySetSendToIndustrial(ProductionOrderItem item, ProductionOrderItemCheckStatusEnum newCheckStatus, DateTime now)
	{
		if (newCheckStatus != ProductionOrderItemCheckStatusEnum.IndustrialDashboardInternal &&
			newCheckStatus != ProductionOrderItemCheckStatusEnum.ForeignIndustryCatalog)
			return;

		item.SendToIndustrialMiladiDate = now;
		item.SendToIndustrialShamsiDate = now.ToShamsiDate();
	}

	/// <summary>
	/// اولین اقدام/ویرایش صنایع — فقط اگر خالی باشد ست می‌شود.
	/// </summary>
	private static void TrySetFirstIndustrialChange(ProductionOrderItem item, DateTime now, long? userId)
	{
		if (item.FirstIndustrialChangeMiladiDate.HasValue)
			return;

		item.FirstIndustrialChangeMiladiDate = now;
		item.FirstIndustrialChangeShamsiDate = now.ToShamsiDate();
		if (userId.HasValue)
			item.FirstIndustrialChangeById = userId;
	}

	#endregion

	/// <summary>جدول اصلی BOM فقط آخرین نسخهٔ هر (قلم، کالا) را نگه می‌دارد؛ نسخه‌های قبلی در ProductionOrderItemBomHistory هستند.</summary>
	private IQueryable<ProductionOrderItemBomViewModel> QueryBomViewModels(long productionOrderItemId)
	{
		return unitOfWork
			.Repository<ProductionOrderItemBom>()
			.TableNoTracking
			.Include(c => c.Part)
			.ThenInclude(c => c.Unit)
			.Where(c => c.ProductionOrderItemId == productionOrderItemId)
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
				NeedsInspection = c.NeedsInspection,
				PartId = c.PartId,
				PartName = c.Part.Name,
				PartCode = c.Part.Code,
				Revision = c.Revision,
				SaleUnitDetails = c.SaleUnitDetails,
				PartUnitName = c.Part.Unit.Title,
				ProductionStep = c.ProductionStep,
				NumberSupplied = c.NumberSupplied,
				Status = c.Status
			});
	}

	[HttpGet("[action]")]
	public async Task<IActionResult> ProductionOrderItemBomPartial(long? id, long productionOrderItemId, CancellationToken cn)
	{
		var forbid = ForbidBomUnless(CanEditBomForm(), "شما دسترسی ویرایش BOM اقلام سفارش ساخت را ندارید");
		if (forbid != null) return forbid;

		var block = await GetBomWriteBlockReasonAsync(productionOrderItemId, cn);
		if (block != null)
			return BadRequest(block);

		ViewBag.CanManageBom = CanManageBom();
		ViewBag.CanEditBomNewFields = CanEditBomNewFields();

		if (id != null && id > 0)
		{
			var bom = await unitOfWork.Repository<ProductionOrderItemBom>()
				.TableNoTracking
				.Include(c => c.Part)
				.FirstOrDefaultAsync(c => c.Id == id && c.ProductionOrderItemId == productionOrderItemId, cn);
			if (bom == null)
				return BadRequest("رکورد BOM یافت نشد");
			return PartialView(@"\Views\Panel\Sale\ProductionOrderItem\_ProductionOrderItemBomPartial.cshtml", bom);
		}
		
		var newBom = new ProductionOrderItemBom { ProductionOrderItemId = productionOrderItemId };
		return PartialView(@"\Views\Panel\Sale\ProductionOrderItem\_ProductionOrderItemBomPartial.cshtml", newBom);
	}

	[HttpPost("[action]")]
	[ActionDisplayName("افزودن BOM از روی فرمول محصول", ActionAccessType.Api, ActionAccessItemType.Create)]
	public async Task<IActionResult> AddBomFromProductFormula(long productionOrderItemId, CancellationToken cn)
	{
		var forbid = ForbidBomUnless(CanManageBom(), "شما دسترسی افزودن BOM را ندارید");
		if (forbid != null) return forbid;

		if (productionOrderItemId <= 0)
			return BadRequest("شناسه قلم سفارش ساخت نمیتواند خالی باشد.");

		var (count, error) = await AddBomFromProductFormulaAsync(productionOrderItemId, cn);
		if (error != null)
			return BadRequest(error);

		return Ok(count);
	}

	[HttpGet("[action]")]
	[ActionDisplayName("حذف BOM", ActionAccessType.Api, ActionAccessItemType.Delete)]
	public async Task<IActionResult> DeleteBom(long id, CancellationToken cn)
	{
		var forbid = ForbidBomUnless(CanManageBom(), "شما دسترسی حذف BOM را ندارید");
		if (forbid != null) return forbid;

		var model = await unitOfWork.Repository<ProductionOrderItemBom>().TableNoTracking.FirstOrDefaultAsync(c => c.Id == id, cn);
		if (model == null)
			return BadRequest("رکورد BOM یافت نشد");

		var block = await GetBomWriteBlockReasonAsync(model.ProductionOrderItemId, cn);
		if (block != null)
			return BadRequest(block);

		// معادل HTS: حذف قلم BOM هم مثل افزودن/ویرایش به واحد صنایع ایمیل می‌شود (پیش از حذف، چون ایمیل از روی ردیف ساخته می‌شود)
		var productionOrderItem = await LoadProductionOrderItemForBomAsync(model.ProductionOrderItemId, cn);
		if (productionOrderItem != null)
		{
			await SendBomChangeNotificationAsync(
				new List<ProductionOrderItemBom> { model },
				productionOrderItem,
				"حذف",
				shouldNotifyProjectManager: false,
				changesMadeWithProjectManager: false,
				fromExcel: false,
				cn);
		}

		// جدول اصلی: ردیف کامل پاک می‌شود. تاریخچه: IsDeleted = true روی همهٔ رخدادهای این (قلم، کالا).
		await bomService.DeleteLineAsync(model.Id!.Value, cn);

		return Ok();
	}

	[HttpGet("[action]")]
	[ActionDisplayName("دانلود نمونه اکسل BOM", ActionAccessType.Api, ActionAccessItemType.FetchData)]
	public IActionResult DownloadBomExcelTemplate()
	{
		var forbid = ForbidBomUnless(CanManageBom(), "شما دسترسی BOM اقلام سفارش ساخت را ندارید");
		if (forbid != null) return forbid;

		var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
		var wb = new Workbook();
		if (!wb.IsLicensed)
			new License().SetLicense(licensePath);

		var ws = wb.Worksheets[0];
		ws.Name = "BOM";
		ws.Cells[0, 0].PutValue("PartCode");
		ws.Cells[0, 1].PutValue("Amount");
		ws.Cells[0, 2].PutValue("Comment");
		ws.Cells[1, 0].PutValue("SAMPLE-CODE");
		ws.Cells[1, 1].PutValue(1);
		ws.Cells[1, 2].PutValue("توضیحات نمونه");
		ws.Cells.SetColumnWidthPixel(0, 160);
		ws.Cells.SetColumnWidthPixel(1, 100);
		ws.Cells.SetColumnWidthPixel(2, 250);

		using var ms = new MemoryStream();
		wb.Save(ms, SaveFormat.Xlsx);
		ms.Position = 0;
		return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ProductionOrderItemBom_Template.xlsx");
	}

	/// <summary>
	/// معادل HTS: DoBomExcelOperation / AddBomByExcel + AddItems
	/// </summary>
	[HttpPost("[action]")]
	[ActionDisplayName("افزودن BOM از اکسل", ActionAccessType.Api)]
	[RequestSizeLimit(20_000_000)]
	public async Task<IActionResult> ImportBomFromExcel(
		[FromForm] long productionOrderItemId,
		[FromForm] bool isApplyToAllSimilarBomPartsOnThisProductionOrder = false,
		IFormFile? file = null,
		CancellationToken cn = default)
	{
		var forbid = ForbidBomUnless(CanManageBom(), "شما دسترسی افزودن BOM از اکسل را ندارید");
		if (forbid != null) return forbid;

		if (productionOrderItemId <= 0)
			return BadRequest("قلم سفارش ساخت مشخص نشده است");

		if (file == null || file.Length == 0)
			return BadRequest("فایل اکسل انتخاب نشده است");

		var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
		if (extension is not (".xlsx" or ".xls"))
			return BadRequest("فقط فایل اکسل با پسوند xlsx یا xls مجاز است");

		List<Dictionary<string, string>> excelRows;
		try
		{
			await using var stream = file.OpenReadStream();
			excelRows = ReadBomExcelRows(stream);
		}
		catch (Exception ex)
		{
			return BadRequest("خطا در خواندن فایل اکسل: " + ex.Message);
		}

		if (excelRows.Count == 0)
			return BadRequest("هیچ ردیفی در فایل اکسل یافت نشد");

		var parseResult = await ParseBomExcelRowsAsync(productionOrderItemId, excelRows, cn);
		if (!parseResult.IsSuccess)
			return BadRequest(parseResult.ErrorMessage);

		var items = parseResult.Items!;
		if (items.Count == 0)
			return BadRequest("هیچ ردیف معتبری برای درج یافت نشد");

		var block = await GetBomWriteBlockReasonAsync(productionOrderItemId, cn);
		if (block != null)
			return BadRequest(block);

		var productionOrderItem = await LoadProductionOrderItemForBomAsync(productionOrderItemId, cn);
		if (productionOrderItem == null)
			return BadRequest("قلم سفارش ساخت یافت نشد");

		var duplicateError = await ValidateBomDuplicatesAsync(productionOrderItemId, items, cn);
		if (duplicateError.HasValue())
			return BadRequest(duplicateError);

		List<ProductionOrderItemBom> savedItems;
		try
		{
			savedItems = await bomService.CreateLinesAsync(
				items.Select(i => new NewBomLine
				{
					ProductionOrderItemId = productionOrderItemId,
					PartId = i.PartId,
					Amount = i.Amount,
					Description = i.Description
				}).ToList(),
				cn);
		}
		catch (Exception ex) when (ProductionOrderItemBomService.IsDuplicateKeyException(ex))
		{
			return BadRequest("یکی از کالاهای فایل قبلاً در BOM این قلم ثبت شده است");
		}

		// معادل HTS: اگر قلم در مرحلهٔ «در انتظار مهندسی» باشد و سفارش مدیر پروژه داشته باشد، قلم به کارتابل مدیر پروژه می‌رود
		var shouldNotifyProjectManager =
			productionOrderItem.ProductionOrder?.ProjectManagerId != null
			&& productionOrderItem.ProductionStep == ProductionOrderItemProductionStepEnum.AwaitingEngineering;

		if (shouldNotifyProjectManager)
		{
			await MoveItemsToProjectManagerCartableAsync(
				productionOrderItem,
				isApplyToAllSimilarBomPartsOnThisProductionOrder,
				cn);
		}

		if (isApplyToAllSimilarBomPartsOnThisProductionOrder)
			await ApplyBomToSimilarProductionOrderItemsAsync(savedItems, productionOrderItem, updateExisting: false, cn);

		// معادل HTS: فایل اکسل بارگذاری‌شده به ایمیل پیوست می‌شود
		var attachmentFileIds = new List<long>();
		try
		{
			var uploaded = await fileService.UploadAsync(file, typeof(ProductionOrderItem).FullName, null, productionOrderItemId, cn);
			if (uploaded.Id.HasValue)
				attachmentFileIds.Add(uploaded.Id.Value);
		}
		catch
		{
			// شکست ذخیرهٔ پیوست ایمیل نباید ورود BOM را متوقف کند
		}

		await SendBomChangeNotificationAsync(
			savedItems,
			productionOrderItem,
			"افزودن",
			shouldNotifyProjectManager,
			changesMadeWithProjectManager: false,
			fromExcel: true,
			cn,
			attachmentFileIds);

		return Ok(new
		{
			count = savedItems.Count
		});
	}



	/// <summary>
	/// ایجاد/ویرایش ردیف BOM. جدول اصلی فقط آخرین نسخه را نگه می‌دارد و هر ذخیره یک رخداد در تاریخچه می‌سازد.
	/// ردیف موجود از دیتابیس بارگذاری می‌شود و فقط فیلدهای مجازِ نقش اعمال می‌شود؛ کالا بعد از ایجاد قابل تغییر نیست.
	/// «توضیحات واحد فروش»، «نیاز به AVL» و «نیاز به بازرسی» از این مسیر تغییر نمی‌کنند (فقط با SaveBomConsideration).
	/// </summary>
	[HttpPost("[action]")]
	[ActionDisplayName("ذخیره BOM", ActionAccessType.Api)]
	public async Task<IActionResult> SaveBom(
		ProductionOrderItemBom bom,
		bool isApplyToAllSimilarBomPartsOnThisProductionOrder = false,
		CancellationToken cn = default)
	{
		var canManage = CanManageBom();
		var canEditNew = CanEditBomNewFields();
		if (!canManage && !canEditNew)
			return Unauthorized("شما دسترسی ذخیره BOM را ندارید");

		if (bom.PartId <= 0)
			return BadRequest("کالای BOM الزامی است");

		if (bom.ProductionOrderItemId <= 0)
			return BadRequest("قلم سفارش ساخت مشخص نشده است");

		var isAdd = bom.Id == null || bom.Id == 0;

		// افزودن ردیف جدید فقط با نقش کامل BOM (نه فقط نقش تکمیل فیلدهای جدید)
		if (isAdd && !canManage)
			return Unauthorized("افزودن ردیف BOM فقط برای نقش Bom اقلام سفارش ساخت مجاز است");

		var block = await GetBomWriteBlockReasonAsync(bom.ProductionOrderItemId, cn);
		if (block != null)
			return BadRequest(block);

		var productionOrderItem = await LoadProductionOrderItemForBomAsync(bom.ProductionOrderItemId, cn);
		if (productionOrderItem == null)
			return BadRequest("قلم سفارش ساخت یافت نشد");

		ProductionOrderItemBom entity;
		try
		{
			if (isAdd)
			{
				// معادل HTS: بررسی تکراری بودن کالا در BOM همین قلم
				var duplicatePartCode = await unitOfWork.Repository<ProductionOrderItemBom>()
					.TableNoTracking
					.Where(p => p.ProductionOrderItemId == bom.ProductionOrderItemId && p.PartId == bom.PartId)
					.Select(p => p.Part.Code)
					.FirstOrDefaultAsync(cn);

				if (duplicatePartCode.HasValue())
					return BadRequest($"قلم با کد {duplicatePartCode} تکراری است");

				entity = await bomService.CreateLineAsync(new NewBomLine
				{
					ProductionOrderItemId = bom.ProductionOrderItemId,
					PartId = bom.PartId,
					Amount = bom.Amount,
					Description = bom.Description,
					ProductionStep = canEditNew ? bom.ProductionStep : default,
					NumberSupplied = canEditNew ? bom.NumberSupplied : null,
					Status = canEditNew ? bom.Status : default
				}, cn);
			}
			else
			{
				var existing = await unitOfWork.Repository<ProductionOrderItemBom>()
					.TableNoTracking
					.FirstOrDefaultAsync(b => b.Id == bom.Id, cn);

				if (existing == null)
					return BadRequest("رکورد BOM یافت نشد");

				if (existing.ProductionOrderItemId != bom.ProductionOrderItemId)
					return BadRequest("رکورد BOM به این قلم تعلق ندارد");

				if (existing.PartId != bom.PartId)
					return BadRequest("تغییر کالا در ویرایش مجاز نیست؛ برای تعویض کالا ردیف را حذف و دوباره اضافه کنید");

				entity = await bomService.UpdateLineAsync(existing.Id!.Value, line =>
				{
					// نقش BOM: مقدار و توضیحات. نقش تکمیل صنایع: مرحله ساخت، تعداد تامین‌شده و وضعیت.
					if (canManage)
					{
						line.Amount = bom.Amount;
						line.Description = bom.Description;
					}

					if (canEditNew)
					{
						line.ProductionStep = bom.ProductionStep;
						line.NumberSupplied = bom.NumberSupplied;
						line.Status = bom.Status;
					}
				}, cn);
			}
		}
		catch (Exception ex) when (ProductionOrderItemBomService.IsDuplicateKeyException(ex))
		{
			return BadRequest("این کالا قبلاً در BOM همین قلم ثبت شده است");
		}

		// معادل HTS: اگر مرحله ساخت «در انتظار مهندسی» باشد و مدیر پروژه تعریف شده باشد،
		// قلم به کارتابل مدیر پروژه منتقل می‌شود تا خرید/ساخت تعیین تکلیف شود.
		// فقط برای ثبت و ویرایش BOM توسط مهندسی؛ تغییر فیلدهای صنایع قلم را جابه‌جا نمی‌کند.
		var shouldNotifyProjectManager =
			canManage
			&& productionOrderItem.ProductionOrder?.ProjectManagerId != null
			&& productionOrderItem.ProductionStep == ProductionOrderItemProductionStepEnum.AwaitingEngineering;

		if (shouldNotifyProjectManager)
		{
			await MoveItemsToProjectManagerCartableAsync(
				productionOrderItem,
				isApplyToAllSimilarBomPartsOnThisProductionOrder,
				cn);
		}

		// معادل HTS: اعمال کالا، مقدار و توضیحات روی سایر اقلام مشابه همان سفارش (فقط نقش BOM)
		if (isApplyToAllSimilarBomPartsOnThisProductionOrder && canManage)
		{
			await ApplyBomToSimilarProductionOrderItemsAsync(
				new List<ProductionOrderItemBom> { entity },
				productionOrderItem,
				updateExisting: !isAdd,
				cn);
		}

		// معادل HTS: SendEmailNotification پس از افزودن/ویرایش BOM
		await SendBomChangeNotificationAsync(
			new List<ProductionOrderItemBom> { entity },
			productionOrderItem,
			isAdd ? "افزودن" : "ویرایش",
			shouldNotifyProjectManager,
			changesMadeWithProjectManager: false,
			fromExcel: false,
			cn);

		var viewModel = await QueryBomViewModels(entity.ProductionOrderItemId)
			.Where(c => c.Id == entity.Id)
			.FirstOrDefaultAsync(cn);

		return Ok(viewModel);
	}

	/// <summary>
	/// «تعیین تکلیف» BOM توسط مدیر پروژه یا کارشناس فروش/جایگزین/مدیر فروش همان سفارش (معادل HTS DoBomSalesUnitOperation).
	/// عمداً بدون [ActionDisplayName]: مجوز بر اساس سمت در سفارش است و داخل کد کنترل می‌شود.
	/// مقدار یکسان روی همهٔ ردیف‌های انتخاب‌شده اعمال می‌شود، رخداد «ملاحظات» در تاریخچه ثبت می‌شود،
	/// یک ایمیل «تعیین تکلیف» می‌رود و اگر قلم «در انتظار مدیر پروژه» باشد به «در انتظار صنایع» می‌رود.
	/// </summary>
	[HttpPost("[action]")]
	public async Task<IActionResult> SaveBomConsideration(SaveBomConsiderationRequest request, CancellationToken cn)
	{
		if (request == null || request.ProductionOrderItemId <= 0)
			return BadRequest("قلم سفارش ساخت مشخص نشده است");

		var bomIds = (request.BomIds ?? new List<long>()).Where(i => i > 0).Distinct().ToList();
		if (bomIds.Count == 0)
			return BadRequest("حداقل یک ردیف BOM را انتخاب کنید");

		if (!await CanConsiderBomAsync(request.ProductionOrderItemId, cn))
			return Unauthorized("ثبت ملاحظات فقط برای مدیر پروژه، کارشناس فروش، کارشناس جایگزین و مدیر فروش همین سفارش مجاز است");

		var block = await GetBomWriteBlockReasonAsync(request.ProductionOrderItemId, cn);
		if (block != null)
			return BadRequest(block);

		var saleUnitDetails = string.IsNullOrWhiteSpace(request.SaleUnitDetails) ? null : request.SaleUnitDetails.Trim();
		if (saleUnitDetails != null && saleUnitDetails.Length > 2048)
			return BadRequest("متن ملاحظات حداکثر ۲۰۴۸ کاراکتر است");

		var validCount = await unitOfWork.Repository<ProductionOrderItemBom>()
			.TableNoTracking
			.CountAsync(b => bomIds.Contains(b.Id!.Value) && b.ProductionOrderItemId == request.ProductionOrderItemId, cn);
		if (validCount != bomIds.Count)
			return BadRequest("ردیف‌های انتخاب‌شده به این قلم تعلق ندارند");

		var productionOrderItem = await LoadProductionOrderItemForBomAsync(request.ProductionOrderItemId, cn);
		if (productionOrderItem == null)
			return BadRequest("قلم سفارش ساخت یافت نشد");

		var saved = await bomService.ApplyConsiderationAsync(
			bomIds, request.NeedsAVL, request.NeedsInspection, saleUnitDetails, cn);

		// معادل HTS: اگر قلم در انتظار مدیر پروژه (۱۳۸۰) بود → در انتظار صنایع (۲۷۴۴) + کامنت مرحلهٔ ساخت
		if (productionOrderItem.ProductionStep == ProductionOrderItemProductionStepEnum.AwaitingProjectManager)
			await MoveItemToAwaitingIndustryAfterBomDecisionAsync(productionOrderItem, cn);

		await SendBomChangeNotificationAsync(
			saved,
			productionOrderItem,
			"ویرایش",
			shouldNotifyProjectManager: false,
			changesMadeWithProjectManager: true,
			fromExcel: false,
			cn);

		return Ok(new { count = saved.Count });
	}

	/// <summary>
	/// معادل HTS: تغییر ProductionStep به 1380 + کامنت خودکار برای تعیین تکلیف BOM توسط مدیر پروژه
	/// </summary>
	private async Task MoveItemsToProjectManagerCartableAsync(
		ProductionOrderItem productionOrderItem,
		bool applyToAllSimilar,
		CancellationToken cn)
	{
		var now = DateTime.Now;
		var itemsToUpdate = new List<ProductionOrderItem> { productionOrderItem };

		if (applyToAllSimilar && productionOrderItem.ProductionOrderId > 0)
		{
			var similarItems = await unitOfWork.Repository<ProductionOrderItem>()
				.Table
				.Where(p => p.ProductionOrderId == productionOrderItem.ProductionOrderId
					&& p.PartId == productionOrderItem.PartId
					&& p.Id != productionOrderItem.Id
					&& p.IsLatestVersion
					&& p.IsActive == IsActiveEnum.Active)
				.ToListAsync(cn);

			itemsToUpdate.AddRange(similarItems);
		}

		foreach (var item in itemsToUpdate)
		{
			// معادل HTS ProductionOrderItemBomService.AddItems: فقط ProductionStepId = 1380 (در انتظار مدیر پروژه).
			// BuyStatus/CheckStatus تغییر نمی‌کند — «تعیین تکلیف BOM» مسیر جدایی از «تایید مدیر پروژه (۲۲۵۸→۲۲۵۹→۲۱۹۵)» است
			// و ست‌کردن ۲۲۵۸ در اینجا باعث می‌شد تایید مدیر پروژه، قلمِ تاییدشده مهندسی را دوباره به کارتابل مهندسی برگرداند.
			item.ProductionStep = ProductionOrderItemProductionStepEnum.AwaitingProjectManager;

			await unitOfWork.Repository<ProductionOrderItemComment>().AddAsync(new ProductionOrderItemComment
			{
				ProductionOrderItemId = item.Id!.Value,
				ProductionStatus = item.CheckStatus.HasValue
					? (ProductionOrderItemProductionStatusEnum)item.CheckStatus.Value
					: ProductionOrderItemProductionStatusEnum.NotDetermined,
				ProductionStep = ProductionOrderItemProductionStepEnum.AwaitingProjectManager,
				Comment = "ایجاد شده بصورت اتوماتیک بجهت تعیین تکلیف وضعیت اقلام BOM توسط مدیر پروژه",
				StartMiladiDateTime = now,
				StartShamsiDate = now.ToShamsiDateTime(),
				IsForProductionMode = false,
				IsForProductionStepStatus = true
			}, cn, false);
		}

		await unitOfWork.Repository<ProductionOrderItem>().UpdateRangeAsync(itemsToUpdate, cn, false);
		await unitOfWork.SaveChangesAsync(cn);
	}

	/// <summary>
	/// معادل HTS DoBomSalesUnitOperation (بخش «درج تاریخچه مرحله ساخت»): پس از تعیین تکلیف BOM توسط مدیر پروژه،
	/// مرحله قلم از ۱۳۸۰ (در انتظار مدیر پروژه) به ۲۷۴۴ (در انتظار صنایع) می‌رود + کامنت مرحله ساخت.
	/// </summary>
	private async Task MoveItemToAwaitingIndustryAfterBomDecisionAsync(ProductionOrderItem productionOrderItem, CancellationToken cn)
	{
		var now = DateTime.Now;
		productionOrderItem.ProductionStep = ProductionOrderItemProductionStepEnum.AwaitingIndustry;

		await unitOfWork.Repository<ProductionOrderItemComment>().AddAsync(new ProductionOrderItemComment
		{
			ProductionOrderItemId = productionOrderItem.Id!.Value,
			ProductionStatus = productionOrderItem.CheckStatus.HasValue
				? (ProductionOrderItemProductionStatusEnum)productionOrderItem.CheckStatus.Value
				: ProductionOrderItemProductionStatusEnum.NotDetermined,
			ProductionStep = ProductionOrderItemProductionStepEnum.AwaitingIndustry,
			Comment = "تعیین تکلیف اقلام BOM توسط مدیر/کارشناس پروژه و ارسال به صنایع",
			StartMiladiDateTime = now,
			StartShamsiDate = now.ToShamsiDateTime(),
			IsForProductionMode = false,
			IsForProductionStepStatus = true
		}, cn, false);

		await unitOfWork.Repository<ProductionOrderItem>().UpdateRangeAsync(new List<ProductionOrderItem> { productionOrderItem }, cn, false);
		await unitOfWork.SaveChangesAsync(cn);
	}

	/// <summary>
	/// درج BOM از روی فرمول محصول (Bom.vw_ProductItems) — فقط با کلیک دکمه، نه با باز شدن صفحه.
	/// شرایط: قلم حذف نشده، آخرین نسخه، CheckStatus در (۱۹۰۴/۱۹۰۵)، ProductionStep = ۲۱۳ (در انتظار مهندسی)،
	/// کالای قلم روتین باشد (Part.DesignTypeIsRoutine، مثل HTS) و هنوز BOM نداشته باشد.
	/// در صورت عدم احراز شرایط، (۰، پیام خطا) برمی‌گردد.
	/// </summary>
	private async Task<(int Count, string? Error)> AddBomFromProductFormulaAsync(long productionOrderItemId, CancellationToken cn)
	{
		var item = await unitOfWork.Repository<ProductionOrderItem>()
			.TableNoTracking
			.Where(c => c.Id == productionOrderItemId
				&& !c.IsDeleted
				&& c.IsLatestVersion
				&& c.CheckStatus != null
				&& (c.CheckStatus == ProductionOrderItemCheckStatusEnum.IndustrialDashboardInternal
					|| c.CheckStatus == ProductionOrderItemCheckStatusEnum.ForeignIndustryCatalog)
				&& c.ProductionStep == ProductionOrderItemProductionStepEnum.AwaitingEngineering)
			.Select(c => new { c.Id, c.PartId, IsRoutine = c.Part.DesignTypeIsRoutine })
			.FirstOrDefaultAsync(cn);

		if (item == null)
			return (0, "افزودن BOM از روی فرمول محصول فقط برای آخرین نسخهٔ قلم حذف‌نشده، در کارتابل صنایع (۱۹۰۴/۱۹۰۵) و در مرحلهٔ «در انتظار مهندسی» مجاز است.");

		if (item.PartId == null)
			return (0, "برای این قلم کالا مشخص نشده است.");

		if (item.IsRoutine != true)
			return (0, "افزودن BOM از روی فرمول محصول فقط برای کالای روتین مجاز است.");

		var hasBom = await unitOfWork.Repository<ProductionOrderItemBom>()
			.TableNoTracking
			.AnyAsync(b => b.ProductionOrderItemId == productionOrderItemId, cn);
		if (hasBom)
			return (0, "این قلم از قبل BOM دارد؛ افزودن BOM از روی فرمول محصول فقط برای قلم بدون BOM مجاز است.");

		var templateRows = await dbContext.vw_ProductItems
			.AsNoTracking()
			.Where(v => v.PartId == item.PartId.Value)
			.Select(v => new { v.PartItemId, v.UsingRate })
			.ToListAsync(cn);

		if (!templateRows.Any())
			return (0, "برای کالای این قلم فرمول محصول تعریف نشده است.");

		var lines = templateRows
			.Where(r => r.PartItemId > 0)
			.GroupBy(r => r.PartItemId)
			.Select(g => new NewBomLine
			{
				ProductionOrderItemId = productionOrderItemId,
				PartId = g.Key,
				Amount = g.Sum(r => r.UsingRate),
				Description = "افزوده شده از BOM روتین محصول"
			})
			.ToList();

		if (!lines.Any())
			return (0, "برای کالای این قلم فرمول محصول تعریف نشده است.");

		try
		{
			await bomService.CreateLinesAsync(lines, cn);
		}
		catch (Exception ex) when (ProductionOrderItemBomService.IsDuplicateKeyException(ex))
		{
			return (0, "این قلم از قبل BOM دارد؛ افزودن BOM از روی فرمول محصول فقط برای قلم بدون BOM مجاز است.");
		}

		return (lines.Count, null);
	}

	/// <summary>
	/// معادل HTS: اعمال BOM روی سایر اقلام مشابه همان سفارش ساخت (همان کالا، آخرین نسخهٔ قلم).
	/// قلمی که مرحله ساختش «تحویل فروش» است از این کپی کنار گذاشته می‌شود.
	/// فقط کالا، مقدار و توضیحات منتقل می‌شود؛ مرحله ساخت، تعداد تامین‌شده، وضعیت و ملاحظات فروش
	/// اختصاصی هر قلم‌اند و هرگز کپی نمی‌شوند.
	/// updateExisting: اگر ردیف آن کالا روی قلم مشابه هست مقدار/توضیحاتش به‌روز می‌شود؛ در غیر این صورت فقط
	/// ردیف‌های ناموجود ساخته می‌شوند.
	/// </summary>
	private async Task ApplyBomToSimilarProductionOrderItemsAsync(
		IReadOnlyList<ProductionOrderItemBom> sources,
		ProductionOrderItem productionOrderItem,
		bool updateExisting,
		CancellationToken cn)
	{
		if (sources.Count == 0)
			return;

		var similarItemIds = await unitOfWork.Repository<ProductionOrderItem>()
			.TableNoTracking
			.Where(p => p.ProductionOrderId == productionOrderItem.ProductionOrderId
				&& p.PartId == productionOrderItem.PartId
				&& p.Id != productionOrderItem.Id
				&& !p.IsDeleted
				&& p.IsLatestVersion
				&& p.IsActive == IsActiveEnum.Active
				&& p.ProductionStep != ProductionOrderItemProductionStepEnum.DeliveredToSales)
			.Select(p => p.Id!.Value)
			.ToListAsync(cn);

		if (similarItemIds.Count == 0)
			return;

		var partIds = sources.Select(s => s.PartId).Distinct().ToList();
		var existingLines = (await unitOfWork.Repository<ProductionOrderItemBom>()
				.TableNoTracking
				.Where(b => similarItemIds.Contains(b.ProductionOrderItemId) && partIds.Contains(b.PartId))
				.Select(b => new { b.Id, b.ProductionOrderItemId, b.PartId })
				.ToListAsync(cn))
			.ToDictionary(x => (x.ProductionOrderItemId, x.PartId), x => x.Id!.Value);

		var toCreate = new List<NewBomLine>();
		foreach (var similarItemId in similarItemIds)
		{
			foreach (var source in sources)
			{
				if (existingLines.TryGetValue((similarItemId, source.PartId), out var lineId))
				{
					if (!updateExisting)
						continue;

					await bomService.UpdateLineAsync(lineId, line =>
					{
						line.Amount = source.Amount;
						line.Description = source.Description;
					}, cn);
					continue;
				}

				toCreate.Add(new NewBomLine
				{
					ProductionOrderItemId = similarItemId,
					PartId = source.PartId,
					Amount = source.Amount,
					Description = source.Description
				});
			}
		}

		if (toCreate.Count > 0)
			await bomService.CreateLinesAsync(toCreate, cn);
	}

	/// <summary>خط یک ردیف BOM در متن ایمیل (بدون وابستگی به شناسهٔ ردیف تا برای درج دسته‌ای هم کار کند).</summary>
	private sealed record BomEmailLine(
		string? PartCode,
		string? PartName,
		decimal Amount,
		string? Description,
		string? SaleUnitDetails,
		bool NeedsAVL,
		bool NeedsInspection);

	/// <summary>شناسهٔ HTS شعب فروش که در HTS گروه ویژه‌ای را CC می‌کردند (AddEmailOfRelatedMembers).</summary>
	private static readonly long[] BomSalesBranchHtsIds = { 12, 15, 39, 49, 50, 51 };

	/// <summary>شعب توربو ماشین (۴۱) و کمپرسورهای مهندسی (۴۲): در HTS پرسنل واحد برنامه‌ریزی پروژه CC می‌شدند.</summary>
	private static readonly long[] BomTurboCompressorBranchHtsIds = { 41, 42 };

	/// <summary>
	/// معادل HTS: ProductionOrderItemBomService.SendEmailNotification
	/// </summary>
	private async Task SendBomChangeNotificationAsync(
		IReadOnlyList<ProductionOrderItemBom> bomEntities,
		ProductionOrderItem productionOrderItem,
		string actionType,
		bool shouldNotifyProjectManager,
		bool changesMadeWithProjectManager,
		bool fromExcel,
		CancellationToken cn,
		IReadOnlyList<long>? attachmentFileIds = null)
	{
		try
		{
			if (bomEntities == null || bomEntities.Count == 0)
				return;

			var partIds = bomEntities.Select(b => b.PartId).Distinct().ToList();
			var partsById = (await unitOfWork.Repository<Part>()
					.TableNoTracking
					.Where(p => partIds.Contains(p.Id!.Value))
					.Select(p => new { p.Id, p.Code, p.Name })
					.ToListAsync(cn))
				.ToDictionary(p => p.Id!.Value);

			var lines = bomEntities.Select(b =>
			{
				partsById.TryGetValue(b.PartId, out var part);
				return new BomEmailLine(part?.Code, part?.Name, b.Amount, b.Description, b.SaleUnitDetails, b.NeedsAVL, b.NeedsInspection);
			}).ToList();

			var productionOrder = productionOrderItem.ProductionOrder;
			var orderNumber = productionOrder?.ProductionOrderNumber?.ToString()
				?? productionOrder?.Number
				?? "-";
			var productCode = productionOrderItem.Part?.Code ?? "-";
			var productName = productionOrderItem.Part?.Name ?? "-";
			var userFullName = CurrentUserFullNameFn ?? CurrentUserFullName ?? "-";

			var body = BuildBomNotificationBody(
				lines,
				productionOrderItem,
				orderNumber,
				productCode,
				productName,
				userFullName,
				actionType,
				shouldNotifyProjectManager,
				changesMadeWithProjectManager,
				fromExcel);

			var title = changesMadeWithProjectManager
				? $"اعلان [تعیین تکلیف] در BOM قلم سفارش ساخت {orderNumber}"
				: $"اعلان [{actionType}] در BOM قلم سفارش ساخت {orderNumber}";

			var toEmails = new List<string>();
			var ccEmails = new List<string>();

			if (CurrentUserEmail.HasValue())
				ccEmails.Add(CurrentUserEmail!);

			if (shouldNotifyProjectManager)
			{
				var pmEmail = productionOrder?.ProjectManager?.Email;
				if (pmEmail.HasValue())
					toEmails.Add(pmEmail!);
			}
			else
			{
				toEmails.AddRange(await ProductionOrderItemNotificationHelper.GetGroupEmailsAsync(
					unitOfWork, ProductionOrderItemNotificationHelper.GroupBomIndustrial, cn));

				var pmEmail = productionOrder?.ProjectManager?.Email;
				if (pmEmail.HasValue())
					ccEmails.Add(pmEmail!);
			}

			AddProductionOrderStakeholderEmails(ccEmails, productionOrder);

			ccEmails.AddRange(await ProductionOrderItemNotificationHelper.GetGroupEmailsAsync(
				unitOfWork, ProductionOrderItemNotificationHelper.GroupBomEngineering, cn));

			// معادل HTS AddEmailOfRelatedMembers: گیرندگان ثابت بر اساس شعبهٔ فروش سفارش
			var branchHtsId = productionOrder?.Branch?.HtsId;
			if (branchHtsId.HasValue && BomSalesBranchHtsIds.Contains(branchHtsId.Value))
			{
				ccEmails.AddRange(await ProductionOrderItemNotificationHelper.GetGroupEmailsAsync(
					unitOfWork, ProductionOrderItemNotificationHelper.GroupBomSalesBranch, cn));
			}

			if (branchHtsId.HasValue && BomTurboCompressorBranchHtsIds.Contains(branchHtsId.Value))
			{
				ccEmails.AddRange(await ProductionOrderItemNotificationHelper.GetGroupEmailsAsync(
					unitOfWork, ProductionOrderItemNotificationHelper.GroupBomTurboCompressorBranch, cn));
			}

			toEmails = toEmails
				.Where(e => e.HasValue())
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
			ccEmails = ccEmails
				.Where(e => e.HasValue() && !toEmails.Contains(e, StringComparer.OrdinalIgnoreCase))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();

			if (!toEmails.Any() && !ccEmails.Any())
				return;

			// اگر To خالی باشد ولی CC داشته باشیم، CC را به To منتقل می‌کنیم تا ایمیل ارسال شود
			if (!toEmails.Any())
			{
				toEmails = ccEmails;
				ccEmails = new List<string>();
			}

			var ownerId = shouldNotifyProjectManager
				? (productionOrder?.ProjectManagerId ?? CurrentUserId ?? 0)
				: (CurrentUserId ?? 0);

			await unitOfWork.Repository<Notification>().AddAsync(new Notification
			{
				Type = NotificationType.Email,
				Title = title,
				Body = body,
				EntityId = productionOrderItem.Id,
				OwnerId = ownerId,
				ViewPath = $"Panel/ProductionOrderItem/Edit/{productionOrderItem.Id}",
				IsRead = false,
				IsSend = false,
				ToEmails = toEmails,
				CcEmails = ccEmails,
				AttachmentFileIds = attachmentFileIds is { Count: > 0 } ? attachmentFileIds.ToList() : null
			}, cn);

			await unitOfWork.SaveChangesAsync(cn);
		}
		catch
		{
			// شکست اعلان نباید ذخیره BOM را متوقف کند
		}
	}

	private static void AddProductionOrderStakeholderEmails(List<string> emails, ProductionOrder? productionOrder)
	{
		if (productionOrder == null)
			return;

		void AddIfHasEmail(string? email)
		{
			if (email.HasValue())
				emails.Add(email!);
		}

		AddIfHasEmail(productionOrder.SalesManager?.Email);
		AddIfHasEmail(productionOrder.SalesExpert?.Email);
		AddIfHasEmail(productionOrder.AlternativeExpert?.Email);
		AddIfHasEmail(productionOrder.IntroducerExpert?.Email);
		AddIfHasEmail(productionOrder.ProjectManager?.Email);
	}

	private static string BuildBomNotificationBody(
		IReadOnlyList<BomEmailLine> boms,
		ProductionOrderItem productionOrderItem,
		string orderNumber,
		string productCode,
		string productName,
		string userFullName,
		string actionType,
		bool shouldNotifyProjectManager,
		bool changesMadeWithProjectManager,
		bool fromExcel)
	{
		var sb = new StringBuilder();
		sb.AppendLine("<div style='text-align:center;direction:rtl'>");
		sb.AppendLine("<table border='1' cellspacing='0' cellpadding='5' style='text-align:right; direction: rtl' width='100%'>");
		sb.AppendLine("<tr style='width: 7.0in; background: #000aa0'>");
		sb.AppendLine("<td><div style='font-size:14.0pt;font-family:Zar;color:#FFFFFF;text-align:center;direction:rtl'>گروه صنعتی هوایار</div></td>");
		sb.AppendLine("</tr>");
		sb.AppendLine("<tr><td>");
		sb.AppendLine("<div style='font-size:14.0pt;font-family:Zar;color:#1F497D;text-align:right;direction:rtl'>");
		sb.AppendLine("با سلام و احترام <br />");

		if (changesMadeWithProjectManager)
		{
			sb.AppendLine("بدینوسیله اعلام میگردد قلم / اقلام BOM ");
			sb.AppendLine($"سفارش ساخت شماره {orderNumber} و کد محصول {productCode} توسط مدیر یا کارشناس پروژه {userFullName} <span style='color:red'>تعیین تکلیف</span> گردید");
			sb.AppendLine("<br />");
			sb.AppendLine("لذا خواهشمند است نسبت به بررسی و انجام اقدامات لازم مساعدت فرمایید");
		}
		else if (actionType == "افزودن")
		{
			sb.AppendLine("بدینوسیله اعلام میگردد قلم / اقلام جدیدی به BOM ");
			sb.AppendLine($"سفارش ساخت شماره {orderNumber} و محصول {productCode} | {productName} توسط {userFullName} <span style='color:red'>افزوده شد</span>");
			if (fromExcel)
				sb.AppendLine($" <span style='color:#1F497D'>(از طریق اکسل - {boms.Count} قلم؛ فایل پیوست است)</span>");
		}
		else
		{
			sb.AppendLine("بدینوسیله اعلام میگردد قلم ذیل در BOM ");
			sb.AppendLine($"سفارش ساخت شماره {orderNumber} و محصول {productCode} | {productName} توسط {userFullName} <span style='color:red'>{actionType} گردید</span>");
		}

		if (shouldNotifyProjectManager)
		{
			sb.AppendLine("<br />");
			sb.AppendLine("<strong style='color:red'>لذا خواهشمند است نسبت به تعیین تکلیف پروسه خرید/ساخت آن اقدام فرمایید.</strong>");
		}

		sb.AppendLine("<br /><br /><ul>");

		// معادل HTS: برای ردیف‌های افزودن/ویرایش/حذف (نه اکسل) مشخص می‌شود کالا از قبل در BOM بوده یا نه
		var existedBefore = actionType != "افزودن";

		foreach (var bom in boms)
		{
			sb.AppendLine($"<li>کد کالا : <strong>{bom.PartCode}</strong></li>");
			sb.AppendLine($"<li>عنوان کالا : <strong>{bom.PartName}</strong></li>");
			sb.AppendLine($"<li>سریال : <strong>{productionOrderItem.Serial}</strong></li>");
			sb.AppendLine($"<li>تعداد/مقدار : <strong>{bom.Amount}</strong></li>");
			sb.AppendLine($"<li>مرحله ساخت : <strong>{productionOrderItem.ProductionStep.ToDisplay()}</strong></li>");
			sb.AppendLine($"<li>کامنت : <strong>{bom.Description}</strong></li>");

			if (changesMadeWithProjectManager)
			{
				sb.AppendLine($"<li>توضیحات واحد فروش : <strong>{bom.SaleUnitDetails}</strong></li>");
				sb.AppendLine($"<li>نیاز به AVL : <strong>{(bom.NeedsAVL ? "دارد" : "ندارد")}</strong></li>");
				sb.AppendLine($"<li>نیاز به بازرسی/مدارک بازرسی : <strong>{(bom.NeedsInspection ? "دارد" : "ندارد")}</strong></li>");
			}

			if (!fromExcel && !changesMadeWithProjectManager)
			{
				var color = existedBefore ? "red" : string.Empty;
				sb.AppendLine($"<li style='margin-bottom:16px'>از قبل موجود بوده است : <strong style='color:{color}'>{(existedBefore ? "بلی" : "خیر")}</strong></li>");
			}
			else
			{
				sb.AppendLine("<li style='list-style:none;margin-bottom:16px'></li>");
			}
		}

		var customerTitle = productionOrderItem.ProductionOrder?.ProductionOrderNumber == 0
			? "* برنامه ریزی"
			: productionOrderItem.ProductionOrder?.Contract?.Customer?.Party?.FullName;

		if (customerTitle.HasValue())
			sb.AppendLine($"<li>مشتری : <strong>{customerTitle}</strong></li>");

		sb.AppendLine("</ul>");
		sb.AppendLine("</div></td></tr></table></div>");
		return sb.ToString();
	}

	private async Task<ProductionOrderItem?> LoadProductionOrderItemForBomAsync(long productionOrderItemId, CancellationToken cn)
	{
		return await unitOfWork.Repository<ProductionOrderItem>()
			.Table
			.Include(p => p.ProductionOrder)
				.ThenInclude(po => po.ProjectManager)
			.Include(p => p.ProductionOrder)
				.ThenInclude(po => po.SalesManager)
			.Include(p => p.ProductionOrder)
				.ThenInclude(po => po.SalesExpert)
			.Include(p => p.ProductionOrder)
				.ThenInclude(po => po.AlternativeExpert)
			.Include(p => p.ProductionOrder)
				.ThenInclude(po => po.IntroducerExpert)
			.Include(p => p.ProductionOrder)
				.ThenInclude(po => po.Branch)
			.Include(p => p.ProductionOrder)
				.ThenInclude(po => po.Contract)
					.ThenInclude(c => c.Customer)
						.ThenInclude(cu => cu.Party)
			.Include(p => p.Part)
			.FirstOrDefaultAsync(p => p.Id == productionOrderItemId, cn);
	}

	private static List<Dictionary<string, string>> ReadBomExcelRows(Stream stream)
	{
		var wb = new Workbook(stream);
		var licensePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Aspose.Total.NET.lic");
		if (!wb.IsLicensed)
			new License().SetLicense(licensePath);

		var ws = wb.Worksheets[0];
		var maxRow = ws.Cells.MaxDataRow;
		var maxCol = ws.Cells.MaxDataColumn;
		if (maxRow < 1 || maxCol < 0)
			return new List<Dictionary<string, string>>();

		var headers = new Dictionary<int, string>();
		for (var c = 0; c <= maxCol; c++)
		{
			var h = ws.Cells[0, c].StringValue?.Trim();
			if (!string.IsNullOrEmpty(h))
				headers[c] = h;
		}

		var rows = new List<Dictionary<string, string>>();
		for (var r = 1; r <= maxRow; r++)
		{
			var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (var header in headers)
			{
				var value = ws.Cells[r, header.Key].StringValue?.Trim() ?? string.Empty;
				row[header.Value] = value;
			}

			if (row.Values.All(string.IsNullOrWhiteSpace))
				continue;

			rows.Add(row);
		}

		return rows;
	}

	private static string? GetExcelCell(Dictionary<string, string> row, params string[] keys)
	{
		foreach (var key in keys)
		{
			var match = row.FirstOrDefault(kv =>
				string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase));
			if (!string.IsNullOrEmpty(match.Key))
				return match.Value?.Trim();
		}
		return null;
	}

	private async Task<(bool IsSuccess, string? ErrorMessage, List<ProductionOrderItemBom>? Items)> ParseBomExcelRowsAsync(
		long productionOrderItemId,
		List<Dictionary<string, string>> excelRows,
		CancellationToken cn)
	{
		var partCodes = excelRows
			.Select(r => GetExcelCell(r, "PartCode", "کد کالا", "کدکالا"))
			.Where(c => c.HasValue())
			.Select(c => c!)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();

		var parts = await unitOfWork.Repository<Part>()
			.TableNoTracking
			.Where(p => p.Code != null && partCodes.Contains(p.Code) && p.IsActive == IsActiveEnum.Active)
			.Select(p => new { p.Id, p.Code })
			.ToListAsync(cn);

		var partMap = parts
			.Where(p => p.Code.HasValue())
			.GroupBy(p => p.Code!, StringComparer.OrdinalIgnoreCase)
			.ToDictionary(g => g.Key, g => g.First().Id!.Value, StringComparer.OrdinalIgnoreCase);

		var items = new List<ProductionOrderItemBom>();
		var seenPartIds = new HashSet<long>();

		for (var i = 0; i < excelRows.Count; i++)
		{
			var rowNumber = i + 2;
			var row = excelRows[i];
			var partCode = GetExcelCell(row, "PartCode", "کد کالا", "کدکالا");
			var amountText = GetExcelCell(row, "Amount", "تعداد", "تعداد/مقدار", "مقدار");
			var comment = GetExcelCell(row, "Comment", "توضیحات", "کامنت");

			if (!partCode.HasValue() && !amountText.HasValue())
				continue;

			if (!partCode.HasValue())
				return (false, $"PartCode در ردیف {rowNumber} یافت نشد", null);

			if (!amountText.HasValue())
				return (false, $"Amount در ردیف {rowNumber} یافت نشد", null);

			if (!partMap.TryGetValue(partCode!, out var partId))
				return (false, $"کالای با کد [{partCode}] در ردیف {rowNumber} یافت نشد", null);

			if (!decimal.TryParse(amountText!.Replace(",", "").Replace("،", ""), out var amountValue))
				return (false, $"مقدار [{amountText}] در ردیف {rowNumber} معتبر نیست", null);

			if (!seenPartIds.Add(partId))
				return (false, $"کالای با کد [{partCode}] در فایل اکسل تکراری است (ردیف {rowNumber})", null);

			items.Add(new ProductionOrderItemBom
			{
				ProductionOrderItemId = productionOrderItemId,
				PartId = partId,
				Amount = amountValue,
				Description = comment,
				IsLatest = true
			});
		}

		return (true, null, items);
	}

	private async Task<string?> ValidateBomDuplicatesAsync(
		long productionOrderItemId,
		List<ProductionOrderItemBom> items,
		CancellationToken cn)
	{
		var partIds = items.Select(i => i.PartId).Distinct().ToList();
		var existItems = await unitOfWork.Repository<ProductionOrderItemBom>()
			.TableNoTracking
			.Where(p => p.ProductionOrderItemId == productionOrderItemId
				&& partIds.Contains(p.PartId))
			.Select(p => new { p.PartId, PartCode = p.Part.Code })
			.ToListAsync(cn);

		var duplicate = existItems.FirstOrDefault();
		if (duplicate != null)
			return $"قلم با کد {duplicate.PartCode} تکراری است";

		return null;
	}


	[HttpGet("[action]")]
	public IActionResult ProductionOrderItemCommentPartial(long? id, long productionOrderItemId)
	{
		var item = unitOfWork.Repository<ProductionOrderItem>()
			.TableNoTracking
			.FirstOrDefault(c => c.Id == productionOrderItemId);
		if (item == null)
			return BadRequest("قلم سفارش ساخت یافت نشد");

		var allowed = GetVisibleChangeStatuses(item).ToList();
		ProductionOrderItemComment model;
		if (id != null && id > 0)
		{
			model = unitOfWork.Repository<ProductionOrderItemComment>()
				.TableNoTracking
				.FirstOrDefault(c => c.Id == id)
				?? new ProductionOrderItemComment { ProductionOrderItemId = productionOrderItemId };

			if (model.Id > 0 && !allowed.Contains(model.ProductionStatus))
				allowed.Insert(0, model.ProductionStatus);
		}
		else
		{
			model = new ProductionOrderItemComment { ProductionOrderItemId = productionOrderItemId };
		}

		ViewBag.AllowedProductionStatuses = allowed;
		return PartialView(@"\Views\Panel\Sale\ProductionOrderItem\_ProductionOrderItemCommentPartial.cshtml", model);
	}

	[HttpPost("[action]")]
	[ActionDisplayName("ذخیره کامنت", ActionAccessType.Api)]
	public async Task<IActionResult> SaveComment(ProductionOrderItemComment comment, CancellationToken cn)
	{
		if (comment.Id == null || comment.Id == 0)
		{
			var productionOrderItem = await unitOfWork.Repository<ProductionOrderItem>()
					.Table
					.Include(c => c.Part)
					.Include(c => c.ProductionOrder).ThenInclude(po => po!.Branch)
					.Include(c => c.ProductionOrder).ThenInclude(po => po!.Contract).ThenInclude(ct => ct!.Customer).ThenInclude(cu => cu!.Party)
					.FirstOrDefaultAsync(c => c.Id == comment.ProductionOrderItemId, cn);

			if (productionOrderItem == null)
				return BadRequest("قلم سفارش ساخت یافت نشد");

			if (!CanChangeProductionStatus(productionOrderItem))
				return Unauthorized("شما دسترسی تغییر وضعیت این قلم را ندارید");

			// معادل HTS DoChangeProductionStatusOperationOriginal: وضعیت جدید نباید با آخرین وضعیت یکی باشد
			if (comment.ProductionStatus == productionOrderItem.ProductionStatus)
				throw new Exception("وضعیت جدید نمیتواند با وضعیت فعلی یکی باشد.");

			if (!GetVisibleChangeStatuses(productionOrderItem).Contains(comment.ProductionStatus))
				throw new Exception("وضعیت انتخاب‌شده برای مرحله و وضعیت فعلی این قلم مجاز نیست.");

			// معادل HTS productionOrderItemOriginal_ValidatePropertyGrid: کامنت فقط برای ۱۷۹۵ و ۱۷۹۶؛
			// علت خرابی فقط برای ۱۷۹۶. ترتیب وضعیت‌ها (مثلاً ۲۲۱۰ قبل از ۱۷۹۰) در HTS چک نمی‌شود.
			if (ProductionOrderItemWorkflowRules.RequiresComment(comment.ProductionStatus)
				&& !comment.Comment.HasValue())
				throw new Exception("کامنت نمی تواند خالی باشد.");

			if (ProductionOrderItemWorkflowRules.RequiresFailureType(comment.ProductionStatus)
				&& !comment.FailureTypeIds.HasValue())
				throw new Exception("علت خرابی نمی تواند خالی باشد.");

			// همگام‌سازی وضعیت والد توسط ProductionOrderItemCommentAction (معادل تریگر HTS)
			var entity = await unitOfWork.Repository<ProductionOrderItemComment>().SaveAsync(comment, cn, true);

			// معادل HTS: پایان تولید (۱۷۸۹) → تاریخ پایان تولید قلم؛ پایان بازرسی نهایی (۱۸۴۳) → تاریخ پایان تست؛
			// پایان بسته‌بندی (۱۷۹۱) → تاریخ آماده‌سازی. هر سه از فیلد «تاریخ پایان» فرم خوانده می‌شوند
			// (دیتاپیکر ساعت را هم دارد). (اقساط ۲ تا ۵ از PreparationDate توسط ProductionOrderItemAction محاسبه می‌شود)
			if (ProductionOrderItemWorkflowRules.TryGetDateFieldForProductionStatus(comment.ProductionStatus, out var dateField))
			{
				var eventDate = entity.EndMiladiDateTime ?? entity.StartMiladiDateTime ?? DateTime.Now;

				switch (dateField)
				{
					case ProductionStatusDateField.ProductionEnd:
						productionOrderItem.ProductionEndMiladiDate = eventDate;
						productionOrderItem.ProductionEndShamsiDate = eventDate.ToShamsiDate();
						break;
					case ProductionStatusDateField.TestingEnd:
						productionOrderItem.TestingEndMiladiDate = eventDate;
						productionOrderItem.TestingEndShamsiDate = eventDate.ToShamsiDate();
						break;
					case ProductionStatusDateField.Preparation:
						productionOrderItem.PreparationMiladiDate = eventDate;
						productionOrderItem.PreparationShamsiDate = eventDate.ToShamsiDate();
						break;
				}

				await unitOfWork.Repository<ProductionOrderItem>().UpdateAsync(productionOrderItem, cn, true);
			}

			// ایمیل «اعلان شروع تست محصول سفارش ساخت» ارسال نمی‌شود.
			// در HTS (Tfs 1774 / SendTestNotificationEmail، گروه 540) به‌خاطر مقایسه شناسه سربرگ با شناسه قلم عملاً صادر نمی‌شد
			// و در پورتال به کل مدیران خدمات پس از فروش می‌رفت، از جمله واحدهای نامرتبط با همان سفارش.
			// if (comment.ProductionStatus == ProductionOrderItemProductionStatusEnum.ProductionTestStarted
			// 	&& productionOrderItem.ProductionOrder?.ProjectManagerId is > 0)
			// {
			// 	await QueueTestStartNotificationAsync(productionOrderItem, cn);
			// }

			// معادل HTS CreateAndSendProductionStatusNotification → SendProductionStatusNotification
			await QueueProductionStatusChangeNotificationAsync(productionOrderItem, entity, cn);
			await QueueProductionStatusSmsAsync(productionOrderItem, entity, cn);

			return Ok(entity);
		}

		var updated = await unitOfWork.Repository<ProductionOrderItemComment>().UpdateAsync(comment, cn, true);
		return Ok(updated);
	}

	// معادل HTS SendTestNotificationEmail (گروه کاربری 540). ارسال قطع است؛ توضیح بالای محل فراخوانی در SaveComment.
	// private async Task QueueTestStartNotificationAsync(ProductionOrderItem item, CancellationToken cn)
	// {
	// 	try
	// 	{
	// 		var to = await ProductionOrderItemNotificationHelper.GetGroupEmailsAsync(unitOfWork, ProductionOrderItemNotificationHelper.GroupTestStart, cn);
	// 		var body = ProductionOrderItemNotificationHelper.BuildEmailShell(
	// 			"بدینوسیله به استحضار می رساند تست تولید قلم سفارش ساخت ذیل آغاز گردید؛ لطفاً هماهنگی‌های لازم (IT Support / خدمات پس از فروش) را انجام دهید.<br /><br /><ul>"
	// 			+ ProductionOrderItemNotificationHelper.Li("شماره سفارش ساخت", item.ProductionOrder?.ProductionOrderNumber ?? (object?)item.ProductionOrder?.Number)
	// 			+ ProductionOrderItemNotificationHelper.Li("کد کالا", item.Part?.Code)
	// 			+ ProductionOrderItemNotificationHelper.Li("عنوان کالا", item.Part?.Name)
	// 			+ ProductionOrderItemNotificationHelper.Li("سریال", item.Serial)
	// 			+ "</ul>");
	//
	// 		await ProductionOrderItemNotificationHelper.QueueEmailAsync(unitOfWork, "اعلان شروع تست محصول سفارش ساخت", body,
	// 			to, new[] { CurrentUserEmail }, item.Id, CurrentUserId ?? 0, $"Panel/ProductionOrderItem/Edit/{item.Id}", cn);
	// 	}
	// 	catch
	// 	{
	// 		// شکست اعلان نباید ثبت وضعیت را متوقف کند
	// 	}
	// }

	/// <summary>
	/// معادل HTS SendProductionStatusNotification: ایمیل تغییر وضعیت تولید قلم به ایجادکننده کامنت، تیم فروش سفارش
	/// (ایجادکننده، کارشناس فروش، مدیر فروش، جایگزین) و مدیر پروژه؛ CC = دریافت‌کنندگان انتخاب‌شده در فرم + گروه
	/// ثابت «تغییر وضعیت تولید». لیست‌های ایمیل سخت‌کد HTS (شعبه‌ای/شخصی) عمداً منتقل نشده‌اند.
	/// </summary>
	private async Task QueueProductionStatusChangeNotificationAsync(ProductionOrderItem item, ProductionOrderItemComment comment, CancellationToken cn)
	{
		try
		{
			var po = item.ProductionOrder;
			var to = new List<string?> { CurrentUserEmail };
			to.AddRange(await GetSalesTeamEmailsAsync(po, cn));

			var cc = new List<string?>();
			if (comment.CcReciversIds.HasValue())
			{
				var ccIds = comment.CcReciversIds!
					.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
					.Select(s => long.TryParse(s, out var v) ? v : 0)
					.Where(v => v > 0);
				cc.AddRange(await ProductionOrderItemNotificationHelper.GetUserEmailsAsync(dbContext, ccIds, cn));
			}
			cc.AddRange(await ProductionOrderItemNotificationHelper.GetGroupEmailsAsync(unitOfWork, ProductionOrderItemNotificationHelper.GroupProductionStatusChange, cn));

			var sb = new StringBuilder();
			sb.Append($"بدینوسیله به استحضار می رساند یکی از اقلام سفارش ساخت شماره {po?.ProductionOrderNumber ?? (object?)po?.Number} با مشخصات ذیل تغییر یافت<br /><br /><ul>");
			sb.Append(ProductionOrderItemNotificationHelper.Li("دپارتمان فروش", po?.Branch?.Title));
			sb.Append(ProductionOrderItemNotificationHelper.Li("مشتری", po?.Contract?.Customer?.Party?.FullName));
			sb.Append(ProductionOrderItemNotificationHelper.Li("شماره س ساخت", po?.ProductionOrderNumber ?? (object?)po?.Number));
			sb.Append(ProductionOrderItemNotificationHelper.Li("سریال کالا", item.Serial));
			sb.Append(ProductionOrderItemNotificationHelper.Li("عنوان کالا", item.Part?.Name));
			sb.Append($"<li>وضعیت جدید : <strong style='color:red'>{comment.ProductionStatus.ToDisplay()}</strong></li>");
			if (comment.FailureTypeNames.HasValue())
				sb.Append(ProductionOrderItemNotificationHelper.Li("علت خرابی", comment.FailureTypeNames));
			sb.Append(ProductionOrderItemNotificationHelper.Li("تاریخ شروع", comment.StartShamsiDate));
			if (comment.EndShamsiDate.HasValue())
				sb.Append(ProductionOrderItemNotificationHelper.Li("تاریخ پایان", comment.EndShamsiDate));
			sb.Append(ProductionOrderItemNotificationHelper.Li("کامنت", comment.Comment));
			sb.Append(ProductionOrderItemNotificationHelper.Li("ایجاد کننده", CurrentUserFullNameFn ?? CurrentUserFullName));
			sb.Append("</ul>");

			await ProductionOrderItemNotificationHelper.QueueEmailAsync(unitOfWork, "اعلان تغییر وضعیت قلم سفارش ساخت",
				ProductionOrderItemNotificationHelper.BuildEmailShell(sb.ToString()),
				to, cc, item.Id, CurrentUserId ?? 0, $"Panel/ProductionOrderItem/Edit/{item.Id}", cn);
		}
		catch
		{
			// شکست اعلان نباید ثبت وضعیت را متوقف کند
		}
	}

	/// <summary>
	/// معادل HTS ProductionOrderItemCommentService.SendSmsNotification: فقط برای وضعیت‌های
	/// 1789, 1790, 1791, 1796, 1842, 2209, 2210, 2226 (وضعیت 1843 در HTS پیامک ندارد).
	/// لیست گیرندگان هر وضعیت + کارکنان QC از GeneralSetting (کلیدهای Sms:Recipients:ProdStatus:*) خوانده می‌شود.
	/// </summary>
	private async Task QueueProductionStatusSmsAsync(ProductionOrderItem item, ProductionOrderItemComment comment, CancellationToken cn)
	{
		try
		{
			var recipientsKey = (int)comment.ProductionStatus switch
			{
				1796 => GeneralSettingKeys.SmsRecipientsProdFinalInspectionStop,
				1789 or 2209 or 2226 => GeneralSettingKeys.SmsRecipientsProdProductionAndFinal,
				2210 or 1842 => GeneralSettingKeys.SmsRecipientsProdTestEnd,
				1790 or 1791 => GeneralSettingKeys.SmsRecipientsProdInspectionStartPackaging,
				_ => null
			};
			if (recipientsKey == null)
				return;

			var statusRecipients = await smsRecipients.GetSettingNumbersAsync(recipientsKey, cn);
			var qcRecipients = await smsRecipients.GetSettingNumbersAsync(GeneralSettingKeys.SmsRecipientsQcEmployees, cn);

			var po = item.ProductionOrder;
			var body = global::Services.Sms.SmsTemplates.ProductionStatus(
				po?.ProductionOrderNumber?.ToString() ?? po?.Number,
				comment.ProductionStatus.ToDisplay(),
				item.Serial,
				po?.Contract?.Customer?.Party?.FullName);

			await sms.TryEnqueueAsync(SmsEventKeys.PlnProductionOrderStatus, statusRecipients + ";" + qcRecipients, body,
				nameof(ProductionOrderItem), item.Id, ct: cn);
		}
		catch
		{
			// شکست پیامک نباید ثبت وضعیت را متوقف کند
		}
	}

	[HttpGet("[action]")]
	public IActionResult ProductionOrderItemCommentListPartial(long productionOrderItemId)
	{
		var comments = unitOfWork.Repository<ProductionOrderItemComment>()
			.TableNoTracking
			.Where(c => c.ProductionOrderItemId == productionOrderItemId)
			.OrderBy(c => c.CreatedOnMiladiDateTime)
			.ToList();

		var inquiries = unitOfWork.Repository<ProductionOrderItemInquiry>()
			.TableNoTracking
			.Where(c => c.ProductionOrderItemId == productionOrderItemId)
			.OrderBy(c => c.CreatedOnMiladiDateTime)
			.ToList();

		var model = new ProductionOrderItemHistoryViewModel
		{
			ProductionOrderItemId = productionOrderItemId,
			OrderStatusComments = comments
				.Where(c => !c.IsForProductionMode && !c.IsForProductionStepStatus)
				.ToList(),
			InquiryHistory = inquiries,
			ProductionStatusComments = comments
				.Where(c => c.IsForProductionMode && !c.IsForProductionStepStatus)
				.ToList(),
			ProductionStepComments = comments
				.Where(c => !c.IsForProductionMode && c.IsForProductionStepStatus)
				.ToList()
		};

		return PartialView(@"\Views\Panel\Sale\ProductionOrderItem\_ProductionOrderItemCommentListPartial.cshtml", model);
	}

	[HttpGet("[action]")]
	public IActionResult StopRequestPartial(long? id, long productionOrderItemId)
	{
		if (id != null && id > 0)
		{
			var stopRequest = unitOfWork.Repository<Entities.App.Prd.StopRequst>()
				.TableNoTracking
				.FirstOrDefault(c => c.Id == id);
			return PartialView(@"\Views\Panel\Sale\ProductionOrderItem\_StopRequestPartial.cshtml", stopRequest);
		}

		var newStopRequest = new Entities.App.Prd.StopRequst { ProductionOrderItemId = productionOrderItemId };
		return PartialView(@"\Views\Panel\Sale\ProductionOrderItem\_StopRequestPartial.cshtml", newStopRequest);
	}

	[HttpPost("[action]")]
	[ActionDisplayName("ذخیره توقف", ActionAccessType.Api)]
	public async Task<IActionResult> SaveStopRequest(Entities.App.Prd.StopRequst stopRequest, CancellationToken cn)
	{
		// Prefer dedicated StopRequst workflow endpoint from client.
		// Fallback: set status and persist (POI comment/notification handled by StopRequstController.Save).
		if (stopRequest.Id == null || stopRequest.Id == 0)
		{
			stopRequest.LastStatus = stopRequest.ProductionStatus == Entities.App.Prd.Enums.StopRequstProductionStatusEnum.StartStopInTest
				? Entities.App.Prd.Enums.StopRequstModuleStatusEnum.RegisteredInProductTest
				: Entities.App.Prd.Enums.StopRequstModuleStatusEnum.PreRegister;

			if (!string.IsNullOrWhiteSpace(stopRequest.StopStartShamsiDateTime) && stopRequest.StopStartMiladiDateTime == null)
			{
				try { stopRequest.StopStartMiladiDateTime = stopRequest.StopStartShamsiDateTime.ToMiladiDateTime(); }
				catch { /* ignore */ }
			}

			var entity = await unitOfWork.Repository<Entities.App.Prd.StopRequst>().SaveAsync(stopRequest, cn, true);

			var now = DateTime.Now;
			var poiComment = new ProductionOrderItemComment
			{
				ProductionOrderItemId = entity.ProductionOrderItemId!.Value,
				StartMiladiDateTime = now,
				StartShamsiDate = now.ToShamsiDateTime(),
				IsForProductionMode = true,
				StopRequestId = entity.Id,
				Comment = (entity.StopReasonTitles ?? "") + " - ایجاد شده بصورت اتوماتیک از محل ایجاد توقف در ماژول توقف"
			};
			poiComment.ProductionStatus = entity.ProductionStatus switch
			{
				Entities.App.Prd.Enums.StopRequstProductionStatusEnum.StartStopInProduction => ProductionOrderItemProductionStatusEnum.ProductionStageStopStarted,
				Entities.App.Prd.Enums.StopRequstProductionStatusEnum.StartStopInTest => ProductionOrderItemProductionStatusEnum.ProductionTestStageStopStarted,
				Entities.App.Prd.Enums.StopRequstProductionStatusEnum.StartStopInFinalInspection => ProductionOrderItemProductionStatusEnum.FinalInspectionStageStopStarted,
				Entities.App.Prd.Enums.StopRequstProductionStatusEnum.StartStopDueToClientVisit => ProductionOrderItemProductionStatusEnum.ClientVisitStopStarted,
				_ => poiComment.ProductionStatus
			};
			await unitOfWork.Repository<ProductionOrderItemComment>().AddAsync(poiComment, cn);
			await unitOfWork.Repository<Entities.App.Prd.StopRequstComment>().AddAsync(new Entities.App.Prd.StopRequstComment
			{
				StopRequstId = entity.Id!.Value,
				Status = entity.LastStatus,
				Description = "ثبت درخواست توقف از قلم سفارش ساخت"
			}, cn);
			await unitOfWork.SaveChangesAsync(cn);
			return Ok(entity);
		}
		else
		{
			stopRequest.LastStatus = Entities.App.Prd.Enums.StopRequstModuleStatusEnum.Edited;
			if (!string.IsNullOrWhiteSpace(stopRequest.StopStartShamsiDateTime) && stopRequest.StopStartMiladiDateTime == null)
			{
				try { stopRequest.StopStartMiladiDateTime = stopRequest.StopStartShamsiDateTime.ToMiladiDateTime(); }
				catch { /* ignore */ }
			}
			var entity = await unitOfWork.Repository<Entities.App.Prd.StopRequst>().UpdateAsync(stopRequest, cn, true);
			await unitOfWork.Repository<Entities.App.Prd.StopRequstComment>().AddAsync(new Entities.App.Prd.StopRequstComment
			{
				StopRequstId = entity.Id!.Value,
				Status = entity.LastStatus,
				Description = "ویرایش درخواست توقف از قلم سفارش ساخت"
			}, cn);
			await unitOfWork.SaveChangesAsync(cn);
			return Ok(entity);
		}
	}

	[HttpGet("[action]")]
	public IActionResult StopRequestListPartial(long productionOrderItemId)
	{
		var stopRequests = unitOfWork.Repository<Entities.App.Prd.StopRequst>()
			.TableNoTracking
			.Where(c => c.ProductionOrderItemId == productionOrderItemId)
			.OrderByDescending(c => c.StopStartMiladiDateTime)
			.ToList();

		return PartialView(@"\Views\Panel\Sale\ProductionOrderItem\_StopRequestListPartial.cshtml", stopRequests);
	}

	[HttpPost("[action]")]
	[ActionDisplayName("جابه جایی قلم", ActionAccessType.Api)]
	public async Task<IActionResult> Swap(ProductionOrderItemSwapViewModel productionOrderItemSwap, CancellationToken cn)
	{
		if (productionOrderItemSwap.CurrentId <= 0 || productionOrderItemSwap.SwapId <= 0)
			return BadRequest("سریال نمی‌تواند خالی باشد");

		if (productionOrderItemSwap.CurrentId == productionOrderItemSwap.SwapId)
			return BadRequest("نمی‌توان یک رکورد را با خودش جابه‌جا کرد");

		var currentId = productionOrderItemSwap.CurrentId;
		var swapId = productionOrderItemSwap.SwapId;

		var items = await unitOfWork.Repository<ProductionOrderItem>()
			.TableNoTracking
			.Include(c => c.ProductionOrderItemComments)
			.Include(c => c.ProductionOrderItemInquiries)
			.Where(c => c.Id == currentId || c.Id == swapId)
			.ToListAsync(cn);

		var entity1 = items.FirstOrDefault(p => p.Id == currentId);
		var entity2 = items.FirstOrDefault(p => p.Id == swapId);
		if (entity1 == null || entity2 == null)
			return BadRequest("یکی از رکوردهای جابجایی یافت نشد");

		await unitOfWork.ExecuteInTransactionAsync(async () =>
		{
			if (productionOrderItemSwap.ShouldBeChangePartCode)
			{
				(entity1.PartId, entity2.PartId) = (entity2.PartId, entity1.PartId);
				(entity1.PartCode, entity2.PartCode) = (entity2.PartCode, entity1.PartCode);
			}

			// هویت تولیدی بین دو قلم جابه‌جا می‌شود — معادل HTS ProductionOrderItemService.DoSwapOperation
			(entity1.ProductionStep, entity2.ProductionStep) = (entity2.ProductionStep, entity1.ProductionStep);
			(entity1.Status, entity2.Status) = (entity2.Status, entity1.Status);
			(entity1.Serial, entity2.Serial) = (entity2.Serial, entity1.Serial);
			(entity1.PlanningNumber, entity2.PlanningNumber) = (entity2.PlanningNumber, entity1.PlanningNumber);
			(entity1.SerialType, entity2.SerialType) = (entity2.SerialType, entity1.SerialType);
			(entity1.ProductionStartMiladiDate, entity2.ProductionStartMiladiDate) = (entity2.ProductionStartMiladiDate, entity1.ProductionStartMiladiDate);
			(entity1.ProductionStartShamsiDate, entity2.ProductionStartShamsiDate) = (entity2.ProductionStartShamsiDate, entity1.ProductionStartShamsiDate);
			(entity1.ProductionStatus, entity2.ProductionStatus) = (entity2.ProductionStatus, entity1.ProductionStatus);
			(entity1.ProductionEndMiladiDate, entity2.ProductionEndMiladiDate) = (entity2.ProductionEndMiladiDate, entity1.ProductionEndMiladiDate);
			(entity1.ProductionEndShamsiDate, entity2.ProductionEndShamsiDate) = (entity2.ProductionEndShamsiDate, entity1.ProductionEndShamsiDate);
			(entity1.TestingEndMiladiDate, entity2.TestingEndMiladiDate) = (entity2.TestingEndMiladiDate, entity1.TestingEndMiladiDate);
			(entity1.TestingEndShamsiDate, entity2.TestingEndShamsiDate) = (entity2.TestingEndShamsiDate, entity1.TestingEndShamsiDate);
			(entity1.PreparationMiladiDate, entity2.PreparationMiladiDate) = (entity2.PreparationMiladiDate, entity1.PreparationMiladiDate);
			(entity1.PreparationShamsiDate, entity2.PreparationShamsiDate) = (entity2.PreparationShamsiDate, entity1.PreparationShamsiDate);
			(entity1.DeliveryMiladiDate, entity2.DeliveryMiladiDate) = (entity2.DeliveryMiladiDate, entity1.DeliveryMiladiDate);
			(entity1.DeliveryShamsiDate, entity2.DeliveryShamsiDate) = (entity2.DeliveryShamsiDate, entity1.DeliveryShamsiDate);

			var entity1Comments = entity1.ProductionOrderItemComments.Where(p => p.IsForProductionMode).ToList();
			var entity2Comments = entity2.ProductionOrderItemComments.Where(p => p.IsForProductionMode).ToList();
			var entity1Inquiries = entity1.ProductionOrderItemInquiries.ToList();
			var entity2Inquiries = entity2.ProductionOrderItemInquiries.ToList();

			ClearProductionOrderItemNavigations(entity1);
			ClearProductionOrderItemNavigations(entity2);

			await unitOfWork.Repository<ProductionOrderItem>()
				.UpdateRangeAsync(new() { entity1, entity2 }, cn, false);

			await SyncSwapInstallmentDatesAsync(entity1, cn);
			await SyncSwapInstallmentDatesAsync(entity2, cn);

			if (productionOrderItemSwap.ShouldBeChangePartCode)
			{
				await RenumberSwapRevisionsAsync(entity1.ProductionOrderId, entity1.PartId, cn);
				await RenumberSwapRevisionsAsync(entity2.ProductionOrderId, entity2.PartId, cn);
			}

			entity1Comments.ForEach(p =>
			{
				p.ProductionOrderItem = null!;
				p.ProductionOrderItemId = swapId;
			});
			entity2Comments.ForEach(p =>
			{
				p.ProductionOrderItem = null!;
				p.ProductionOrderItemId = currentId;
			});
			entity1Inquiries.ForEach(p =>
			{
				p.ProductionOrderItem = null!;
				p.ProductionOrderItemId = swapId;
			});
			entity2Inquiries.ForEach(p =>
			{
				p.ProductionOrderItem = null!;
				p.ProductionOrderItemId = currentId;
			});

			var comments = entity1Comments.Concat(entity2Comments).ToList();
			if (comments.Count > 0)
				await unitOfWork.Repository<ProductionOrderItemComment>().UpdateRangeAsync(comments, cn, false);

			var inquiries = entity1Inquiries.Concat(entity2Inquiries).ToList();
			if (inquiries.Count > 0)
				await unitOfWork.Repository<ProductionOrderItemInquiry>().UpdateRangeAsync(inquiries, cn, false);

			// معادل تریگر HTS پس از جابه‌جایی FK کامنت تولید / استعلام
			await SyncSwapProductionStatusFromCommentsAsync(currentId, cn);
			await SyncSwapProductionStatusFromCommentsAsync(swapId, cn);
			await SyncSwapCheckStatusFromInquiriesAsync(currentId, cn);
			await SyncSwapCheckStatusFromInquiriesAsync(swapId, cn);

			return true;
		}, cn);

		return Ok();
	}

	private static void ClearProductionOrderItemNavigations(ProductionOrderItem entity)
	{
		entity.ProductionOrderItemComments = null!;
		entity.ProductionOrderItemInquiries = null!;
		entity.ProductionOrderItemBom = null!;
		entity.Part = null;
		entity.ProductionOrder = null;
	}

	/// <summary>معادل تریگر HTS Serial: اقساط از PreparationDate.</summary>
	private async Task SyncSwapInstallmentDatesAsync(ProductionOrderItem model, CancellationToken ct)
	{
		if (model.Id is not > 0)
			return;

		if (model.PreparationMiladiDate == null)
		{
			await unitOfWork.Repository<ProductionOrderItem>().UpdateFieldsAsync(model.Id, new Dictionary<string, object>
			{
				[nameof(ProductionOrderItem.SecondInstallmentShamsiDate)] = null!,
				[nameof(ProductionOrderItem.ThirdInstallmentShamsiDate)] = null!,
				[nameof(ProductionOrderItem.FourthInstallmentShamsiDate)] = null!,
				[nameof(ProductionOrderItem.FifthInstallmentShamsiDate)] = null!
			}, ct);
			return;
		}

		var baseDate = model.PreparationMiladiDate.Value.Date;
		await unitOfWork.Repository<ProductionOrderItem>().UpdateFieldsAsync(model.Id, new Dictionary<string, object>
		{
			[nameof(ProductionOrderItem.SecondInstallmentShamsiDate)] = baseDate.AddDays(30).ToShamsiDate(),
			[nameof(ProductionOrderItem.ThirdInstallmentShamsiDate)] = baseDate.AddDays(60).ToShamsiDate(),
			[nameof(ProductionOrderItem.FourthInstallmentShamsiDate)] = baseDate.AddDays(90).ToShamsiDate(),
			[nameof(ProductionOrderItem.FifthInstallmentShamsiDate)] = baseDate.AddDays(120).ToShamsiDate()
		}, ct);
	}

	/// <summary>معادل تریگر HTS UpdateProductionOrderItemVersionInfo وقتی کالا هم جابه‌جا شود.</summary>
	private async Task RenumberSwapRevisionsAsync(long? productionOrderId, long? partId, CancellationToken ct)
	{
		if (productionOrderId is not > 0 || partId is not > 0)
			return;

		var siblings = await unitOfWork.Repository<ProductionOrderItem>()
			.TableNoTracking
			.Where(c => c.ProductionOrderId == productionOrderId && c.PartId == partId && !c.IsDeleted)
			.OrderBy(c => c.Id)
			.Select(c => c.Id)
			.ToListAsync(ct);

		decimal revision = 0;
		foreach (var id in siblings)
		{
			if (id == null)
				continue;
			await unitOfWork.Repository<ProductionOrderItem>().UpdateFieldsAsync(id, new Dictionary<string, object>
			{
				[nameof(ProductionOrderItem.Revision)] = revision
			}, ct);
			revision++;
		}
	}

	private async Task SyncSwapProductionStatusFromCommentsAsync(long productionOrderItemId, CancellationToken ct)
	{
		var last = await unitOfWork.Repository<ProductionOrderItemComment>()
			.TableNoTracking
			.Where(c => c.ProductionOrderItemId == productionOrderItemId
				&& c.IsForProductionMode
				&& c.IsActive == IsActiveEnum.Active)
			.OrderByDescending(c => c.Id)
			.Select(c => new { c.ProductionStatus, c.Comment })
			.FirstOrDefaultAsync(ct);

		if (last == null)
			return;

		await unitOfWork.Repository<ProductionOrderItem>().UpdateFieldsAsync(productionOrderItemId, new Dictionary<string, object>
		{
			[nameof(ProductionOrderItem.ProductionStatus)] = last.ProductionStatus,
			[nameof(ProductionOrderItem.LastComment)] = last.Comment ?? string.Empty
		}, ct);
	}

	private async Task SyncSwapCheckStatusFromInquiriesAsync(long productionOrderItemId, CancellationToken ct)
	{
		var latestStatus = await unitOfWork.Repository<ProductionOrderItemInquiry>()
			.TableNoTracking
			.Where(c => c.ProductionOrderItemId == productionOrderItemId
				&& c.IsActive == IsActiveEnum.Active)
			.OrderByDescending(c => c.Id)
			.Select(c => (ProductionOrderItemCheckStatusEnum?)c.Status)
			.FirstOrDefaultAsync(ct);

		if (latestStatus == null)
			return;

		var now = DateTime.Now;
		await unitOfWork.Repository<ProductionOrderItem>().UpdateFieldsAsync(productionOrderItemId, new Dictionary<string, object>
		{
			[nameof(ProductionOrderItem.CheckStatus)] = latestStatus.Value,
			[nameof(ProductionOrderItem.CheckStatusChangedOnMiladiDate)] = now,
			[nameof(ProductionOrderItem.CheckStatusChangedOnShamsiDate)] = now.ToShamsiDateTime()
		}, ct);
	}
	}
	public class ProductionOrderItemSwapViewModel  
	{
		public long CurrentId { get; set; }
		public long SwapId { get; set; }
		public bool ShouldBeChangePartCode { get; set; }
	}

	/// <summary>
	/// ورودی فرم «ارسال نتیجه استعلام به کمیته» (معادل کامنت + پیوست DoInquiryOperation در HTS با StatusId=1909).
	/// </summary>
	public class ProductionOrderItemInquiryRequest
	{
		public string? Comment { get; set; }
		public long? AttachmentFileId { get; set; }
	}

	/// <summary>
	/// ورودی فرم «تصمیم کمیته تامین» (معادل پنجره تغییر وضعیت HTS در حالت رئیس کمیته):
	/// مقصد ۱۹۰۴/۱۹۰۵/۱۹۰۶/۱۹۰۸ + مسئول استعلام + LeadTime + پیوست + کامنت.
	/// </summary>
	public class ProductionOrderItemCommitteeDecisionRequest
	{
		public ProductionOrderItemCheckStatusEnum Destination { get; set; }
		public long? ResponsibleId { get; set; }
		public string? LeadTimeShamsiDate { get; set; }
		public long? AttachmentFileId { get; set; }
		public string? Comment { get; set; }
	}

	/// <summary>
	/// تصمیم تایید/رد برای اکشن‌های گردش‌کار قلم سفارش ساخت (تایید مهندسی/مدیر پروژه). یک enum سبک و صرفاً
	/// مربوط به پارامتر Api، مشابه الگوی ProductionOrderItemSwapViewModel، بدون نیاز به Entity/Migration جدید.
	/// </summary>
	public enum ProductionOrderItemWorkflowDecisionEnum
	{
		[Display(Name = "تایید")]
		Approve = 1,
		[Display(Name = "رد")]
		Reject = 2,
		[Display(Name = "نیاز به بازنگری")]
		ReviseNeeded = 3,
	}

	public class ProductionOrderItemBomViewModel: ProductionOrderItemBom
	{
		public string PartName { get; set; }
		public string PartCode { get; set; }

		public string PartUnitName { get; set; }
	}

	public class ProductionOrderItemBomListByParentViewModel
	{
		public long ProductionOrderItemId { get; set; }
		public long? ProductionOrderNumber { get; set; }
		public string? PartCode { get; set; }
		public string? PartName { get; set; }

		/// <summary>افزودن/ویرایش/حذف/اکسل BOM (نقش BOM و شرط وضعیت قلم).</summary>
		public bool CanManageBom { get; set; }

		/// <summary>ویرایش فیلدهای صنایع (مرحله ساخت، تعداد تامین‌شده، وضعیت).</summary>
		public bool CanEditBomNewFields { get; set; }

		/// <summary>ثبت ملاحظات (تعیین تکلیف): مدیر سیستم یا ذی‌نفع همین سفارش.</summary>
		public bool CanConsider { get; set; }

		/// <summary>دکمهٔ «افزودن BOM از روی فرمول محصول» (کالای روتین).</summary>
		public bool CanAddFromFormula { get; set; }

		/// <summary>اگر نوشتن روی BOM به‌خاطر وضعیت قلم بسته باشد، دلیل آن.</summary>
		public string? WriteBlockReason { get; set; }
	}

	public class SaveBomConsiderationRequest
	{
		public long ProductionOrderItemId { get; set; }
		public List<long>? BomIds { get; set; }
		public bool NeedsAVL { get; set; }
		public bool NeedsInspection { get; set; }
		public string? SaleUnitDetails { get; set; }
	}
 
}
