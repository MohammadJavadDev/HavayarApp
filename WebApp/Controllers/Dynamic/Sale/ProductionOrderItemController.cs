using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Data.Repositories;
using Data.SystemAuth;
using Entities.App.Sale;
using Entities.App.Sale.Enums;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using WebApp.Models.Sale;
using WebFramework.Filtters;
using WebFramework.Page;

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
			oldEntity.DocumentPreparationShamsiDate = productionOrderItem.DocumentPreparationShamsiDate;
			oldEntity.DocumentPreparationMiladiDate = productionOrderItem.DocumentPreparationMiladiDate;
			oldEntity.StandardDeliveryShamsiDate = productionOrderItem.StandardDeliveryShamsiDate;
			oldEntity.StandardDeliveryMiladiDate = productionOrderItem.StandardDeliveryMiladiDate;
			oldEntity.EngineeringConsideration = productionOrderItem.EngineeringConsideration;
			oldEntity.PlanningConsideration = productionOrderItem.PlanningConsideration;
			oldEntity.IsRoutine = productionOrderItem.IsRoutine;
			oldEntity.Status = productionOrderItem.Status;
			oldEntity.ProductionStatus = productionOrderItem.ProductionStatus;
			oldEntity.SalesConsideration = productionOrderItem.SalesConsideration;
			oldEntity.SerialType = productionOrderItem.SerialType;

			// اولین اقدام صنایع روی قلم در کارتابل صنایع
			if (oldEntity.CheckStatus.HasValue && IndustrialDashboardStatuses.Contains(oldEntity.CheckStatus.Value))
				TrySetFirstIndustrialChange(oldEntity, DateTime.Now, CurrentUserId);

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
					}).ToList();

				// فاز ۶ - رابط کاربری: تشخیص رشته مهندسی (مکانیک/برق) این قلم برای نمایش انتخابی دکمه تایید/رد
				// مهندسی سمت کلاینت. حتماً از همان لیست ElectricalDeviceTypes استفاده می‌شود که AcceptEngineering
				// برای تشخیص دسترسی سمت سرور استفاده می‌کند تا منطق در دو جا تکرار/ناهماهنگ نشود.
				ViewBag.IsElectricalDiscipline = entity != null &&
					entity.DeviceType.HasValue &&
					ElectricalDeviceTypes.Contains(entity.DeviceType.Value);

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

	// TODO: لیست دقیق DeviceType های برقی را با واحد مهندسی نهایی کن
	private static readonly HashSet<ProductionOrderItemDeviceTypeEnum> ElectricalDeviceTypes = new()
	{
		ProductionOrderItemDeviceTypeEnum.ControlPanel,
		ProductionOrderItemDeviceTypeEnum.ElectricalPanel,
		ProductionOrderItemDeviceTypeEnum.InverterPanel,
		ProductionOrderItemDeviceTypeEnum.Sequencer,
	};

	private static readonly ProductionOrderItemCheckStatusEnum[] IndustrialDashboardStatuses =
	{
		ProductionOrderItemCheckStatusEnum.IndustrialDashboardInternal,
		ProductionOrderItemCheckStatusEnum.ForeignIndustryCatalog
	};

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
				.FirstOrDefaultAsync(x => x.Id == id, cn);

			if (item == null)
				return BadRequest("قلم سفارش ساخت یافت نشد");

			var isElectrical = item.DeviceType.HasValue && ElectricalDeviceTypes.Contains(item.DeviceType.Value);

			if (isElectrical)
			{
				//Seed Role => Sale.ProductionOrderItem.AcceptEngineeringElectrical => سفارش ساخت - تایید مهندسی برق
				// TODO(Role): Role را در پنل مدیریت نقش‌ها ایجاد و کاربران واحد مهندسی برق را به آن انتساب بده
				if (!IsAdministrator && !CurrentUserHasAnyRole("Sale.ProductionOrderItem.AcceptEngineeringElectrical"))
					return Unauthorized("شما دسترسی تایید مهندسی برق را ندارید");
			}
			else
			{
				//Seed Role => Sale.ProductionOrderItem.AcceptEngineeringMechanical => سفارش ساخت - تایید مهندسی مکانیک
				// TODO(Role): Role را در پنل مدیریت نقش‌ها ایجاد و کاربران واحد مهندسی مکانیک را به آن انتساب بده
				if (!IsAdministrator && !CurrentUserHasAnyRole("Sale.ProductionOrderItem.AcceptEngineeringMechanical"))
					return Unauthorized("شما دسترسی تایید مهندسی مکانیک را ندارید");
			}

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
					item.ProductionStep = ProductionOrderItemProductionStepEnum.AwaitingProduction;
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
			await unitOfWork.SaveChangesAsync(cn);

			return Ok(item);
		}
		catch (Exception ex)
		{
			return StatusCode(500, "خطا در تایید/رد مهندسی: " + ex.Message);
		}
	}

	[HttpPost("[action]/{id}")]
	[ActionDisplayName("تایید صنایع", ActionAccessType.Api, ActionAccessItemType.Custom)]
	public async Task<IActionResult> AcceptIndustrial(long id, CancellationToken cn)
	{
		try
		{
			//Seed Role => Sale.ProductionOrderItem.AcceptIndustrial => سفارش ساخت - تایید صنایع
			// TODO(Role): Role را در پنل مدیریت نقش‌ها ایجاد و کاربران واحد صنایع را به آن انتساب بده
			if (!IsAdministrator && !CurrentUserHasAnyRole("Sale.ProductionOrderItem.AcceptIndustrial"))
				return Unauthorized("شما دسترسی تایید صنایع را ندارید");

			var item = await unitOfWork.Repository<ProductionOrderItem>()
				.Table
				.FirstOrDefaultAsync(x => x.Id == id, cn);

			if (item == null)
				return BadRequest("قلم سفارش ساخت یافت نشد");

			if (item.CheckStatus != ProductionOrderItemCheckStatusEnum.IndustrialDashboardInternal &&
				item.CheckStatus != ProductionOrderItemCheckStatusEnum.ForeignIndustryCatalog)
				return BadRequest("این قلم در کارتابل صنایع نیست");

			var now = DateTime.Now;

			// CheckStatus تغییر نمی‌کند چون عضو مجزایی برای "تایید صنایع" در Enum وجود ندارد (تایید شد)؛
			// صرفاً مرحله ساخت (ProductionStep) به سمت تولید حرکت می‌کند
			item.ProductionStep = ProductionOrderItemProductionStepEnum.AwaitingProduction;
			item.CheckStatusChangedOnMiladiDate = now;
			item.CheckStatusChangedOnShamsiDate = now.ToShamsiDateTime();
			TrySetFirstIndustrialChange(item, now, CurrentUserId);

			// تغییر ProductionStep → تب «مرحله ساخت»
			var itemComment = new ProductionOrderItemComment
			{
				ProductionOrderItemId = id,
				ProductionStatus = (ProductionOrderItemProductionStatusEnum)item.CheckStatus!.Value,
				ProductionStep = item.ProductionStep,
				Comment = "تایید صنایع و ارسال به تولید",
				StartMiladiDateTime = now,
				StartShamsiDate = now.ToShamsiDateTime(),
				IsForProductionMode = false,
				IsForProductionStepStatus = true
			};

			await unitOfWork.Repository<ProductionOrderItem>().UpdateAsync(item, cn, true);
			await unitOfWork.Repository<ProductionOrderItemComment>().AddAsync(itemComment, cn, true);
			await unitOfWork.SaveChangesAsync(cn);

			return Ok(item);
		}
		catch (Exception ex)
		{
			return StatusCode(500, "خطا در تایید صنایع: " + ex.Message);
		}
	}

	[HttpPost("[action]/{id}")]
	[ActionDisplayName("تایید/رد مدیر پروژه", ActionAccessType.Api, ActionAccessItemType.Custom)]
	public async Task<IActionResult> AcceptProjectManager(long id, ProductionOrderItemWorkflowDecisionEnum decision, CancellationToken cn)
	{
		try
		{
			//Seed Role => Sale.ProductionOrderItem.AcceptProjectManager => سفارش ساخت - تایید مدیر پروژه
			// TODO(Role): Role را در پنل مدیریت نقش‌ها ایجاد و مدیران پروژه را به آن انتساب بده
			if (!IsAdministrator && !CurrentUserHasAnyRole("Sale.ProductionOrderItem.AcceptProjectManager"))
				return Unauthorized("شما دسترسی تایید مدیر پروژه را ندارید");

			var item = await unitOfWork.Repository<ProductionOrderItem>()
				.Table
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
					item.ProductionStep = ProductionOrderItemProductionStepEnum.AwaitingProduction;
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
				Comment = defaultComment,
				StartMiladiDateTime = now,
				StartShamsiDate = now.ToShamsiDateTime()
			};

			await unitOfWork.Repository<ProductionOrderItem>().UpdateAsync(item, cn, true);
			await unitOfWork.Repository<ProductionOrderItemComment>().AddAsync(itemComment, cn, true);
			await unitOfWork.SaveChangesAsync(cn);

			return Ok(item);
		}
		catch (Exception ex)
		{
			return StatusCode(500, "خطا در تایید/رد مدیر پروژه: " + ex.Message);
		}
	}

	[HttpPost("[action]/{id}")]
	[ActionDisplayName("ارسال به استعلام", ActionAccessType.Api, ActionAccessItemType.Custom)]
	public async Task<IActionResult> SendToInquiry(long id, CancellationToken cn)
	{
		//Seed Role => Sale.ProductionOrderItem.Inquirer => سفارش ساخت - استعلام
		// TODO(Role): Role را در پنل مدیریت نقش‌ها ایجاد و کاربران مسئول استعلام را به آن انتساب بده
		if (!IsAdministrator && !CurrentUserHasAnyRole("Sale.ProductionOrderItem.Inquirer"))
			return Unauthorized("شما دسترسی ارسال به استعلام را ندارید");

		return await TransitionCheckStatusAsync(
			id,
			IndustrialDashboardStatuses,
			ProductionOrderItemCheckStatusEnum.InquiryNeeded,
			"ارسال به استعلام",
			newProductionStep: null,
			invalidStateMessage: "این قلم در کارتابل صنایع نیست تا برای استعلام ارسال شود",
			cn);
	}

	[HttpPost("[action]/{id}")]
	[ActionDisplayName("ارسال به بررسی وزارت صنایع", ActionAccessType.Api, ActionAccessItemType.Custom)]
	public async Task<IActionResult> SendToMinistryReview(long id, CancellationToken cn)
	{
		//Seed Role => Sale.ProductionOrderItem.MinistryOfIndustryInquirer => سفارش ساخت - استعلام وزارت صنایع
		// TODO(Role): Role را در پنل مدیریت نقش‌ها ایجاد و کاربران مسئول بررسی وزارت صنایع را به آن انتساب بده
		if (!IsAdministrator && !CurrentUserHasAnyRole("Sale.ProductionOrderItem.MinistryOfIndustryInquirer"))
			return Unauthorized("شما دسترسی ارسال به بررسی صنایع را ندارید");

		var validPreStates = new[]
		{
			ProductionOrderItemCheckStatusEnum.IndustrialDashboardInternal,
			ProductionOrderItemCheckStatusEnum.ForeignIndustryCatalog,
			ProductionOrderItemCheckStatusEnum.InquiryNeeded
		};

		return await TransitionCheckStatusAsync(
			id,
			validPreStates,
			ProductionOrderItemCheckStatusEnum.IndustrialMinistryReviewRequired,
			"ارسال به بررسی صنایع",
			newProductionStep: null,
			invalidStateMessage: "این قلم در وضعیت مناسب برای ارسال به صنایع نیست",
			cn);
	}

	[HttpPost("[action]/{id}")]
	[ActionDisplayName("ارسال به کمیته تامین", ActionAccessType.Api, ActionAccessItemType.Custom)]
	public async Task<IActionResult> SendToSupplyCommittee(long id, CancellationToken cn)
	{
		// تصمیم طراحی: ارسال به کمیته تامین معمولاً توسط همان کاربری انجام می‌شود که مسئول استعلام/بررسی وزارت
		// صنایع است (نه صرفاً رئیس کمیته)، بنابراین داشتن هر یک از این سه نقش برای این اکشن کافی است
		//Seed Role => Sale.ProductionOrderItem.Inquirer => سفارش ساخت - استعلام
		//Seed Role => Sale.ProductionOrderItem.MinistryOfIndustryInquirer => سفارش ساخت - استعلام وزارت صنایع
		//Seed Role => Sale.ProductionOrderItem.SupplyCommitteeBoss => سفارش ساخت - رئیس کمیته تامین
		// TODO(Role): این سه Role را در پنل مدیریت نقش‌ها ایجاد و کاربران مربوطه را به آن انتساب بده
		if (!IsAdministrator && !CurrentUserHasAnyRole(
				"Sale.ProductionOrderItem.Inquirer",
				"Sale.ProductionOrderItem.MinistryOfIndustryInquirer",
				"Sale.ProductionOrderItem.SupplyCommitteeBoss"))
			return Unauthorized("شما دسترسی ارسال به کمیته تامین را ندارید");

		var validPreStates = new[]
		{
			ProductionOrderItemCheckStatusEnum.IndustrialDashboardInternal,
			ProductionOrderItemCheckStatusEnum.ForeignIndustryCatalog,
			ProductionOrderItemCheckStatusEnum.InquiryNeeded,
			ProductionOrderItemCheckStatusEnum.IndustrialMinistryReviewRequired
		};

		return await TransitionCheckStatusAsync(
			id,
			validPreStates,
			ProductionOrderItemCheckStatusEnum.SendToSupplyCommitteeChair,
			"ارسال به رئیس کمیته تامین",
			newProductionStep: null,
			invalidStateMessage: "این قلم در وضعیت مناسب برای ارسال به کمیته تامین نیست",
			cn);
	}

	[HttpPost("[action]/{id}")]
	[ActionDisplayName("تایید کمیته تامین", ActionAccessType.Api, ActionAccessItemType.Custom)]
	public async Task<IActionResult> CommitteeAccept(long id, CancellationToken cn)
	{
		//Seed Role => Sale.ProductionOrderItem.SupplyCommitteeBoss => سفارش ساخت - رئیس کمیته تامین
		// TODO(Role): Role را در پنل مدیریت نقش‌ها ایجاد و رئیس کمیته تامین را به آن انتساب بده
		if (!IsAdministrator && !CurrentUserHasAnyRole("Sale.ProductionOrderItem.SupplyCommitteeBoss"))
			return Unauthorized("شما دسترسی تایید کمیته تامین را ندارید");

		// یادداشت سازگاری Enum: هیچ اکشنی در حال حاضر CheckStatus را به CommitteeHeadReviewPending(2202) نمی‌برد؛
		// SendToSupplyCommittee آن را به SendToSupplyCommitteeChair(1909) می‌برد. این دو مقدار از نظر معنایی
		// معادل «در انتظار بررسی رئیس کمیته تامین» هستند، بنابراین هر دو به‌عنوان پیش‌وضعیت مجاز پذیرفته می‌شوند.
		var validPreStates = new[]
		{
			ProductionOrderItemCheckStatusEnum.SendToSupplyCommitteeChair,
			ProductionOrderItemCheckStatusEnum.CommitteeHeadReviewPending
		};

		return await TransitionCheckStatusAsync(
			id,
			validPreStates,
			ProductionOrderItemCheckStatusEnum.IndustrialDashboardInternal,
			"تایید رئیس کمیته تامین و بازگشت به روند اصلی",
			newProductionStep: ProductionOrderItemProductionStepEnum.AwaitingProduction,
			invalidStateMessage: "این قلم در وضعیت انتظار بررسی رئیس کمیته تامین نیست",
			cn);
	}

	[HttpPost("[action]/{id}")]
	[ActionDisplayName("رد کمیته تامین", ActionAccessType.Api, ActionAccessItemType.Custom)]
	public async Task<IActionResult> CommitteeReject(long id, CancellationToken cn)
	{
		//Seed Role => Sale.ProductionOrderItem.SupplyCommitteeBoss => سفارش ساخت - رئیس کمیته تامین
		// TODO(Role): Role را در پنل مدیریت نقش‌ها ایجاد و رئیس کمیته تامین را به آن انتساب بده
		if (!IsAdministrator && !CurrentUserHasAnyRole("Sale.ProductionOrderItem.SupplyCommitteeBoss"))
			return Unauthorized("شما دسترسی رد کمیته تامین را ندارید");

		var validPreStates = new[]
		{
			ProductionOrderItemCheckStatusEnum.SendToSupplyCommitteeChair,
			ProductionOrderItemCheckStatusEnum.CommitteeHeadReviewPending
		};

		// تصمیم طراحی: رد رئیس کمیته تامین به‌عنوان یک وضعیت پایانی/نهایی در نظر گرفته شد (Deprecated) و نه
		// بازگشت به InitialRegistration، چون از نظر معنایی «رد» با «نیاز به بازنگری» متفاوت است
		return await TransitionCheckStatusAsync(
			id,
			validPreStates,
			ProductionOrderItemCheckStatusEnum.Deprecated,
			"رد رئیس کمیته تامین",
			newProductionStep: ProductionOrderItemProductionStepEnum.Canceled,
			invalidStateMessage: "این قلم در وضعیت انتظار بررسی رئیس کمیته تامین نیست",
			cn);
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
		CancellationToken cn)
	{
		try
		{
			var item = await unitOfWork.Repository<ProductionOrderItem>()
				.Table
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

			if (IsInquiryRelatedStatus(newCheckStatus))
			{
				// تب «وضعیت استعلام»
				await unitOfWork.Repository<ProductionOrderItemInquiry>().AddAsync(new ProductionOrderItemInquiry
				{
					ProductionOrderItemId = id,
					Status = newCheckStatus,
					Comment = comment,
					LeadTimeMiladiDate = now,
					LeadTimeShamsiDate = now.ToShamsiDateTime()
				}, cn, true);
			}

			await unitOfWork.SaveChangesAsync(cn);

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
				var productionOrderItem = await unitOfWork.Repository<ProductionOrderItem>()
						.TableNoTracking
						.Select(c=>new {c.Id , c.ProductionStatus})
						.FirstOrDefaultAsync(c => c.Id == comment.ProductionOrderItemId);

				if(comment.ProductionStatus == productionOrderItem.ProductionStatus)
				{
					throw new Exception("وضعیت جدید نمیتواند با وضعیت فعلی یکی باشد.");
				}

			// تغییر وضعیت تولید → تب «وضعیت تولید»
			comment.IsForProductionMode = true;
			comment.IsForProductionStepStatus = false;

			var entity = await unitOfWork.Repository<ProductionOrderItemComment>().SaveAsync(comment, cn, true);

				 unitOfWork.Repository<ProductionOrderItem>()
					.UpdateFields(productionOrderItem.Id,
					c => c.ProductionStatus,
					comment.ProductionStatus);

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
		if (stopRequest.Id == null || stopRequest.Id == 0)
		{
			var entity = await unitOfWork.Repository<Entities.App.Prd.StopRequst>().SaveAsync(stopRequest, cn, true);
			return Ok(entity);
		}
		else
		{
			var entity = await unitOfWork.Repository<Entities.App.Prd.StopRequst>().UpdateAsync(stopRequest, cn, true);
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
	public async Task<IActionResult> Swap(ProductionOrderItemSwapViewModel productionOrderItemSwap,
									CancellationToken cn)
		{

			var result = await unitOfWork.Repository<ProductionOrderItem>()
						.TableNoTracking
						.Include(c=>c.ProductionOrderItemComments)
						.Where(c=>c.Id == productionOrderItemSwap.CurrentId || c.Id == productionOrderItemSwap.SwapId)
						.ToListAsync();

			var entity1 = result.First(p => p.Id == productionOrderItemSwap.CurrentId);
			var entity2 = result.First(p => p.Id == productionOrderItemSwap.SwapId);

			if(productionOrderItemSwap.ShouldBeChangePartCode)
			{
				var entity1PartId = entity1.PartId;
				entity1.PartId = entity2.PartId;
				entity2.PartId = entity1PartId;
			}

			var ProductionStep = entity1.ProductionStep;
			var Status = entity1.Status;
			var Serial = entity1.Serial;
			var PlanningNumber = entity1.PlanningNumber;
			var SerialType = entity1.SerialType;
			var ProductionStartMiladiDate = entity1.ProductionStartMiladiDate;
			var ProductionStartShamsiDate = entity1.ProductionStartShamsiDate;
			var ProductionStatus = entity1.ProductionStatus;
			var ProductionEndMiladiDate = entity1.ProductionEndMiladiDate;
			var ProductionEndShamsiDate = entity1.ProductionEndShamsiDate;
			var TestingEndMiladiDate = entity1.TestingEndMiladiDate;
			var TestingEndShamsiDate = entity1.TestingEndShamsiDate;
			var PreparationMiladiDate = entity1.PreparationMiladiDate;
			var PreparationShamsiDate = entity1.PreparationShamsiDate;
			var DeliveryMiladiDate = entity1.DeliveryMiladiDate;
			var DeliveryShamsiDate = entity1.DeliveryShamsiDate;

			entity2.ProductionStep = ProductionStep;
			entity2.Status = Status;
			entity2.Serial = Serial;
			entity2.PlanningNumber = PlanningNumber;
			entity2.SerialType = SerialType;
			entity2.ProductionStartMiladiDate =ProductionStartMiladiDate ;
			entity2.ProductionStartShamsiDate =ProductionStartShamsiDate ;
			entity2.ProductionStatus = ProductionStatus;
			entity2.ProductionEndMiladiDate =ProductionEndMiladiDate ;
			entity2.ProductionEndShamsiDate =ProductionEndShamsiDate ;
			entity2.TestingEndMiladiDate =TestingEndMiladiDate ;
			entity2.TestingEndShamsiDate =TestingEndShamsiDate ;
			entity2.PreparationMiladiDate =PreparationMiladiDate ;
			entity2.PreparationShamsiDate =PreparationShamsiDate ;
			entity2.DeliveryMiladiDate =DeliveryMiladiDate ;
			entity2.DeliveryShamsiDate = DeliveryShamsiDate;


			// IsForProductionMode = فقط وضعیت های تولید
			var entity1Comments = entity1.ProductionOrderItemComments;
			var entity2Comments = entity2.ProductionOrderItemComments;


			entity1Comments.ForEach(p =>
			{
				p.Id = null;
				p.ProductionOrderItem = null;
				p.ProductionOrderItemId = productionOrderItemSwap.SwapId;
			});

			entity2Comments.ForEach(p =>
			{
				p.Id = null;
				p.ProductionOrderItem = null;
				p.ProductionOrderItemId = productionOrderItemSwap.CurrentId;
			});

		 
 

				await unitOfWork.Repository<ProductionOrderItemComment>()
			    .DeleteRangeAsync(entity1Comments, cn,false);

				await unitOfWork.Repository<ProductionOrderItemComment>()
					.DeleteRangeAsync(entity2Comments, cn, false);

				entity1.ProductionOrderItemComments = entity2Comments;
				entity2.ProductionOrderItemComments = entity1Comments;

				await unitOfWork.Repository<ProductionOrderItem>()
					.UpdateRangeAsync(new() { entity1, entity2 }, cn, false);

		     	await unitOfWork.SaveChangesAsync(cn);

				  

			return Ok();
		}
	}
	public class ProductionOrderItemSwapViewModel  
	{
		public long CurrentId { get; set; }
		public long SwapId { get; set; }
		public bool ShouldBeChangePartCode { get; set; }
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
 
}
