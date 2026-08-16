using Aspose.Cells;
using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Data.Repositories;
using Data.SystemAuth;
using Entities.App.Inv;
using Entities.App.Sale;
using Entities.App.Sale.Enums;
using Entities.Base;
using Entities.Base.DataTable;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.NotificationGroupServices;
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
		INotificationGroupService notificationGroupService) : BaseController
	{
		private const string BomIndustrialNotificationGroupCode = "Sale.ProductionOrderItemBom.Industrial";
		private const string BomEngineeringNotificationGroupCode = "Sale.ProductionOrderItemBom.Engineering";

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
			oldEntity.PlanningConsideration = productionOrderItem.PlanningConsideration;
			oldEntity.IsRoutine = productionOrderItem.IsRoutine;
			oldEntity.Status = productionOrderItem.Status;
			oldEntity.ProductionStatus = productionOrderItem.ProductionStatus;
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
		[ActionDisplayName("لیست BOM قلم سفارش ساخت", ActionAccessType.View, ActionAccessItemType.Custom)]
		public IActionResult BomListBy(long? productionOrderItemId)
		{
			if (productionOrderItemId == null || productionOrderItemId == 0)
				throw new Exception("شناسه قلم سفارش ساخت نمیتواند خالی باشد.");

			var item = unitOfWork.Repository<ProductionOrderItem>().TableNoTracking
				.Where(c => c.Id == productionOrderItemId)
				.Select(c => new
				{
					c.Id,
					ProductionOrderNumber = c.ProductionOrder.ProductionOrderNumber,
					PartCode = c.Part.Code,
					PartName = c.Part.Name
				})
				.FirstOrDefault();

			if (item == null)
				throw new Exception("قلم سفارش ساخت یافت نشد.");

			var model = new ProductionOrderItemBomListByParentViewModel
			{
				ProductionOrderItemId = item.Id!.Value,
				ProductionOrderNumber = item.ProductionOrderNumber,
				PartCode = item.PartCode,
				PartName = item.PartName
			};

			return View(@"\Views\Panel\Sale\ProductionOrderItem\ListByParentId.cshtml", model);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت لیست BOM با شناسه قلم سفارش ساخت", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public IActionResult GetBomListByParentId(long? productionOrderItemId)
		{
			if (productionOrderItemId == null || productionOrderItemId == 0)
				return BadRequest("شناسه قلم سفارش ساخت نمیتواند خالی باشد.");

			var items = QueryBomViewModels(productionOrderItemId.Value).ToList();
			return Ok(items);
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

	[HttpGet("[action]")]
	[ActionDisplayName("حذف BOM", ActionAccessType.Api, ActionAccessItemType.Delete)]
	public async Task<IActionResult> DeleteBom(long id, CancellationToken cn)
	{
		var model = unitOfWork.Repository<ProductionOrderItemBom>().TableNoTracking.FirstOrDefault(c => c.Id == id);
		if (model == null)
			return BadRequest("رکورد BOM یافت نشد");

		await unitOfWork.Repository<ProductionOrderItemBom>().DeleteAsync(model, cn, true);
		return Ok();
	}

	[HttpGet("[action]")]
	[ActionDisplayName("دانلود نمونه اکسل BOM", ActionAccessType.Api, ActionAccessItemType.FetchData)]
	public IActionResult DownloadBomExcelTemplate()
	{
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

		var productionOrderItem = await LoadProductionOrderItemForBomAsync(productionOrderItemId, cn);
		if (productionOrderItem == null)
			return BadRequest("قلم سفارش ساخت یافت نشد");

		var duplicateError = await ValidateBomDuplicatesAsync(productionOrderItemId, items, cn);
		if (duplicateError.HasValue())
			return BadRequest(duplicateError);

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

		var savedItems = new List<ProductionOrderItemBom>();
		foreach (var item in items)
		{
			item.IsLatest = true;
			var saved = await unitOfWork.Repository<ProductionOrderItemBom>().SaveAsync(item, cn, true);
			savedItems.Add(saved);
		}

		if (isApplyToAllSimilarBomPartsOnThisProductionOrder)
		{
			foreach (var saved in savedItems)
				await ApplyBomToSimilarProductionOrderItemsAsync(saved, productionOrderItem, cn);
		}

		await SendBomChangeNotificationAsync(
			savedItems,
			productionOrderItem,
			isAdd: true,
			shouldNotifyProjectManager,
			changesMadeWithProjectManager: false,
			fromExcel: true,
			cn);

		return Ok(new
		{
			count = savedItems.Count,
			ids = savedItems.Select(c => c.Id).ToList()
		});
	}



	[HttpPost("[action]")]
	[ActionDisplayName("ذخیره BOM", ActionAccessType.Api)]
	public async Task<IActionResult> SaveBom(
		ProductionOrderItemBom bom,
		bool isApplyToAllSimilarBomPartsOnThisProductionOrder = false,
		CancellationToken cn = default)
	{
		if (bom.PartId <= 0)
			return BadRequest("کالای BOM الزامی است");

		if (bom.ProductionOrderItemId <= 0)
			return BadRequest("قلم سفارش ساخت مشخص نشده است");

		var isAdd = bom.Id == null || bom.Id == 0;

		// معادل HTS: بررسی تکراری بودن Part در BOMهای آخرین نسخه همین قلم
		var duplicatePartCode = await unitOfWork.Repository<ProductionOrderItemBom>()
			.TableNoTracking
			.Where(p => p.ProductionOrderItemId == bom.ProductionOrderItemId
				&& p.IsLatest
				&& p.PartId == bom.PartId
				&& p.IsActive == IsActiveEnum.Active
				&& (isAdd || p.Id != bom.Id))
			.Select(p => p.Part.Code)
			.FirstOrDefaultAsync(cn);

		if (duplicatePartCode.HasValue())
			return BadRequest($"قلم با کد {duplicatePartCode} تکراری است");

		var productionOrderItem = await LoadProductionOrderItemForBomAsync(bom.ProductionOrderItemId, cn);

		if (productionOrderItem == null)
			return BadRequest("قلم سفارش ساخت یافت نشد");

		// معادل HTS: اگر مرحله ساخت «در انتظار مهندسی» باشد و مدیر پروژه تعریف شده باشد،
		// قلم به کارتابل مدیر پروژه منتقل می‌شود تا خرید/ساخت تعیین تکلیف شود
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

		ProductionOrderItemBom entity;
		if (isAdd)
			entity = await unitOfWork.Repository<ProductionOrderItemBom>().SaveAsync(bom, cn, true);
		else
			entity = await unitOfWork.Repository<ProductionOrderItemBom>().UpdateAsync(bom, cn, true);

		// معادل HTS: اعمال همان BOM روی سایر اقلام مشابه (همان کالا در همان سفارش ساخت)
		if (isApplyToAllSimilarBomPartsOnThisProductionOrder && isAdd)
		{
			await ApplyBomToSimilarProductionOrderItemsAsync(entity, productionOrderItem, cn);
		}

		// معادل HTS: SendEmailNotification پس از افزودن/ویرایش BOM
		await SendBomChangeNotificationAsync(
			new List<ProductionOrderItemBom> { entity },
			productionOrderItem,
			isAdd,
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
			item.ProductionStep = ProductionOrderItemProductionStepEnum.AwaitingProjectManager;
			item.CheckStatus = ProductionOrderItemCheckStatusEnum.AwaitingProjectManagerApproval;
			item.CheckStatusChangedOnMiladiDate = now;
			item.CheckStatusChangedOnShamsiDate = now.ToShamsiDateTime();

			await unitOfWork.Repository<ProductionOrderItemComment>().AddAsync(new ProductionOrderItemComment
			{
				ProductionOrderItemId = item.Id!.Value,
				ProductionStatus = ProductionOrderItemProductionStatusEnum.AwaitingProjectManagerApproval,
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
	/// معادل HTS: کپی BOM روی سایر اقلام مشابه همان سفارش ساخت
	/// </summary>
	private async Task ApplyBomToSimilarProductionOrderItemsAsync(
		ProductionOrderItemBom sourceBom,
		ProductionOrderItem productionOrderItem,
		CancellationToken cn)
	{
		var similarEntityIds = await unitOfWork.Repository<ProductionOrderItem>()
			.TableNoTracking
			.Where(p => p.ProductionOrderId == productionOrderItem.ProductionOrderId
				&& p.PartId == productionOrderItem.PartId
				&& p.Id != productionOrderItem.Id
				&& p.IsLatestVersion
				&& p.IsActive == IsActiveEnum.Active)
			.Select(p => p.Id!.Value)
			.ToListAsync(cn);

		if (!similarEntityIds.Any())
			return;

		// جلوگیری از ایجاد BOM تکراری روی اقلام مشابه
		var existingPartOnSimilar = await unitOfWork.Repository<ProductionOrderItemBom>()
			.TableNoTracking
			.Where(p => similarEntityIds.Contains(p.ProductionOrderItemId)
				&& p.PartId == sourceBom.PartId
				&& p.IsLatest
				&& p.IsActive == IsActiveEnum.Active)
			.Select(p => p.ProductionOrderItemId)
			.Distinct()
			.ToListAsync(cn);

		var targetIds = similarEntityIds.Except(existingPartOnSimilar).ToList();
		if (!targetIds.Any())
			return;

		var similarBoms = targetIds.Select(poiId => new ProductionOrderItemBom
		{
			ProductionOrderItemId = poiId,
			PartId = sourceBom.PartId,
			Amount = sourceBom.Amount,
			Description = sourceBom.Description,
			NeedsAVL = sourceBom.NeedsAVL,
			SaleUnitDetails = sourceBom.SaleUnitDetails,
			ProductionStep = sourceBom.ProductionStep,
			Status = sourceBom.Status,
			NumberSupplied = sourceBom.NumberSupplied,
			IsLatest = true
		}).ToList();

		await unitOfWork.Repository<ProductionOrderItemBom>().AddRangeAsync(similarBoms, cn, true);
	}

	/// <summary>
	/// معادل HTS: ProductionOrderItemBomService.SendEmailNotification
	/// </summary>
	private async Task SendBomChangeNotificationAsync(
		IReadOnlyList<ProductionOrderItemBom> bomEntities,
		ProductionOrderItem productionOrderItem,
		bool isAdd,
		bool shouldNotifyProjectManager,
		bool changesMadeWithProjectManager,
		bool fromExcel,
		CancellationToken cn)
	{
		try
		{
			if (bomEntities == null || bomEntities.Count == 0)
				return;

			await EnsureBomNotificationGroupsExistAsync(cn);

			var bomIds = bomEntities.Where(b => b.Id.HasValue).Select(b => b.Id!.Value).ToList();
			var bomsWithPart = await unitOfWork.Repository<ProductionOrderItemBom>()
				.TableNoTracking
				.Include(b => b.Part)
				.Where(b => bomIds.Contains(b.Id!.Value))
				.ToListAsync(cn);

			if (!bomsWithPart.Any())
				return;

			var productionOrder = productionOrderItem.ProductionOrder;
			var actionType = isAdd ? "افزودن" : "ویرایش";
			var orderNumber = productionOrder?.ProductionOrderNumber?.ToString()
				?? productionOrder?.Number
				?? "-";
			var productCode = productionOrderItem.Part?.Code ?? "-";
			var productName = productionOrderItem.Part?.Name ?? "-";
			var userFullName = CurrentUserFullNameFn ?? CurrentUserFullName ?? "-";

			var body = BuildBomNotificationBody(
				bomsWithPart,
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
				var industrialMembers = await notificationGroupService.GetGroupMembersAsync(BomIndustrialNotificationGroupCode, cn);
				toEmails.AddRange(industrialMembers
					.Where(m => m.Email.HasValue())
					.Select(m => m.Email));

				var pmEmail = productionOrder?.ProjectManager?.Email;
				if (pmEmail.HasValue())
					ccEmails.Add(pmEmail!);
			}

			AddProductionOrderStakeholderEmails(ccEmails, productionOrder);

			var engineeringMembers = await notificationGroupService.GetGroupMembersAsync(BomEngineeringNotificationGroupCode, cn);
			ccEmails.AddRange(engineeringMembers
				.Where(m => m.Email.HasValue())
				.Select(m => m.Email));

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
				CcEmails = ccEmails
			}, cn);

			await unitOfWork.SaveChangesAsync(cn);
		}
		catch
		{
			// شکست اعلان نباید ذخیره BOM را متوقف کند
		}
	}

	private async Task EnsureBomNotificationGroupsExistAsync(CancellationToken cn)
	{
		var existingCodes = await unitOfWork.Repository<NotificationGroup>()
			.TableNoTracking
			.Where(g => g.Code == BomIndustrialNotificationGroupCode || g.Code == BomEngineeringNotificationGroupCode)
			.Select(g => g.Code)
			.ToListAsync(cn);

		if (!existingCodes.Contains(BomIndustrialNotificationGroupCode))
		{
			await notificationGroupService.CreateGroupAsync(
				BomIndustrialNotificationGroupCode,
				"سفارش ساخت - BOM - واحد صنایع",
				"گیرندگان اصلی اعلان افزودن/ویرایش BOM (معادل لیست صنایع در HTS)",
				cn);
		}

		if (!existingCodes.Contains(BomEngineeringNotificationGroupCode))
		{
			await notificationGroupService.CreateGroupAsync(
				BomEngineeringNotificationGroupCode,
				"سفارش ساخت - BOM - واحد مهندسی",
				"گیرندگان CC اعلان BOM (معادل لیست مهندسی در HTS)",
				cn);
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
		IReadOnlyList<ProductionOrderItemBom> boms,
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
				sb.AppendLine($" <span style='color:#1F497D'>(از طریق اکسل - {boms.Count} قلم)</span>");
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

		foreach (var bom in boms)
		{
			sb.AppendLine($"<li>کد کالا : <strong>{bom.Part?.Code}</strong></li>");
			sb.AppendLine($"<li>عنوان کالا : <strong>{bom.Part?.Name}</strong></li>");
			sb.AppendLine($"<li>سریال : <strong>{productionOrderItem.Serial}</strong></li>");
			sb.AppendLine($"<li>تعداد/مقدار : <strong>{bom.Amount}</strong></li>");
			sb.AppendLine($"<li>مرحله ساخت : <strong>{productionOrderItem.ProductionStep.ToDisplay()}</strong></li>");
			sb.AppendLine($"<li style='margin-bottom:16px'>کامنت : <strong>{bom.Description}</strong></li>");
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
				&& p.IsLatest
				&& p.IsActive == IsActiveEnum.Active
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
					.Select(c => new { c.Id, c.ProductionStatus })
					.FirstOrDefaultAsync(c => c.Id == comment.ProductionOrderItemId);

			if (productionOrderItem == null)
				return BadRequest("قلم سفارش ساخت یافت نشد");

			if (comment.ProductionStatus == productionOrderItem.ProductionStatus)
				throw new Exception("وضعیت جدید نمیتواند با وضعیت فعلی یکی باشد.");

			// همگام‌سازی وضعیت والد توسط ProductionOrderItemCommentAction (معادل تریگر HTS)
			var entity = await unitOfWork.Repository<ProductionOrderItemComment>().SaveAsync(comment, cn, true);
			return Ok(entity);
		}

		var updated = await unitOfWork.Repository<ProductionOrderItemComment>().UpdateAsync(comment, cn, true);
		return Ok(updated);
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

	public class ProductionOrderItemBomListByParentViewModel
	{
		public long ProductionOrderItemId { get; set; }
		public long? ProductionOrderNumber { get; set; }
		public string? PartCode { get; set; }
		public string? PartName { get; set; }
	}
 
}
