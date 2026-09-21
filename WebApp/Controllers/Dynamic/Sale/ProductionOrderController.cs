using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Data.SystemAuth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Entities.Base.DataTable;
using WebFramework.Filtters;
using WebFramework.Page;
using Entities.App.Sale;
using Entities.App.Sale.Enums;
using Entities.App.Pln;
using System.Linq.Expressions;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("سفارش ساخت", typeof(ProductionOrder))]
	public class ProductionOrderController(IUnitOfWork unitOfWork, IPropertyIdentityService identityService, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(ProductionOrder productionOrder, CancellationToken cn)
		{
			if (productionOrder.Id == null || productionOrder.Id == 0)
			{
				return await Add(productionOrder, cn);
			}
			var exist = await unitOfWork.Repository<ProductionOrder>().TableNoTracking.AnyAsync(c => c.Id == productionOrder.Id);
			if (exist)
			{
				return await Update(productionOrder, cn);
			}
			return await Add(productionOrder, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(ProductionOrder productionOrder, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<ProductionOrder>().SaveAsync(productionOrder, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(ProductionOrder productionOrder, CancellationToken cn)
		{
			var oldEntity = await unitOfWork.Repository<ProductionOrder>()
				.TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == productionOrder.Id, cn);

			if (oldEntity == null)
				return BadRequest("سفارش ساخت یافت نشد");

			// جلوگیری از تغییر مستقیم گردش‌کار (State) و فیلدهای ردیابی تایید مالی/منسوخ‌سازی از طریق فرم عمومی ویرایش؛
			// این فیلدها فقط باید از طریق Job همگام‌سازی راهکاران (تایید مالی) و اکشن DoObsolete تغییر کنند (مطابق الگوی
			// ProductionOrderItemController.Update که فیلدهای گردش‌کار را به‌صورت انتخابی از oldEntity کپی می‌کند)
			oldEntity.ContractId = productionOrder.ContractId;
			oldEntity.ProjectDlId = productionOrder.ProjectDlId;
			oldEntity.EndUser = productionOrder.EndUser;
			oldEntity.ProjectStartDate = productionOrder.ProjectStartDate;
			oldEntity.MetreDate = productionOrder.MetreDate;
			oldEntity.MetreNumber = productionOrder.MetreNumber;
			oldEntity.AgreedDeliveryDate = productionOrder.AgreedDeliveryDate;
			oldEntity.SalesManagerId = productionOrder.SalesManagerId;
			oldEntity.ProjectManagerId = productionOrder.ProjectManagerId;
			oldEntity.AlternativeExpertId = productionOrder.AlternativeExpertId;
			oldEntity.SalesAgencyId = productionOrder.SalesAgencyId;
			oldEntity.InstallationCityId = productionOrder.InstallationCityId;
			oldEntity.IsNeedInspectionBeforePacking = productionOrder.IsNeedInspectionBeforePacking;
			oldEntity.HasContractor = productionOrder.HasContractor;
			oldEntity.CompressorHouseIsReady = productionOrder.CompressorHouseIsReady;
			oldEntity.InstallationIsByHy = productionOrder.InstallationIsByHy;
			oldEntity.HasFinancialGuarantee = productionOrder.HasFinancialGuarantee;
			oldEntity.SalesComment = productionOrder.SalesComment;
			oldEntity.Comment = productionOrder.Comment;
			oldEntity.CentrifugeHasCompressor = productionOrder.CentrifugeHasCompressor;
			oldEntity.CentrifugeCompressorCount = productionOrder.CentrifugeCompressorCount;
			oldEntity.CentrifugeElectromotorVoltage = productionOrder.CentrifugeElectromotorVoltage;
			oldEntity.ManagementConfirmationAttachmentId = productionOrder.ManagementConfirmationAttachmentId;
			oldEntity.DepositFactorAttachmentId = productionOrder.DepositFactorAttachmentId;
			oldEntity.PreFactorAttachmentId = productionOrder.PreFactorAttachmentId;
			oldEntity.Number = productionOrder.Number;
			oldEntity.ContractAttachmentId = productionOrder.ContractAttachmentId;
			oldEntity.HasGA = productionOrder.HasGA;
			oldEntity.PackingType = productionOrder.PackingType;
			oldEntity.DeliveryType = productionOrder.DeliveryType;
			oldEntity.ProductType = productionOrder.ProductType;
			oldEntity.CentrifugeSetupType = productionOrder.CentrifugeSetupType;
			oldEntity.IsNeedProjectManager = productionOrder.IsNeedProjectManager;
			oldEntity.ProductionOrderNumber = productionOrder.ProductionOrderNumber;
			oldEntity.Revision = productionOrder.Revision;
			oldEntity.SalesExpertId = productionOrder.SalesExpertId;
			oldEntity.CustomerIndustry = productionOrder.CustomerIndustry;
			oldEntity.ProjectDlCode = productionOrder.ProjectDlCode;
			oldEntity.EdmsProject = productionOrder.EdmsProject;
			oldEntity.IntroducerExpertId = productionOrder.IntroducerExpertId;
			oldEntity.DelayNotCalculated = productionOrder.DelayNotCalculated;
			// فیلدهای فرم زنده HTS (صفحه ۴۶۳) که روی Edit نبودند: شعبه فروش، محل تحویل، پیوست مهندسی/متره
			oldEntity.BranchId = productionOrder.BranchId;
			oldEntity.DeliveryLocation = productionOrder.DeliveryLocation;
			oldEntity.EngineeringAttachmentId = productionOrder.EngineeringAttachmentId;
			oldEntity.IsDisableForTimelyDeliveryReport = productionOrder.IsDisableForTimelyDeliveryReport;
			oldEntity.DisableForTimelyDeliveryReportComment = productionOrder.DisableForTimelyDeliveryReportComment;
			// State ،FinancialConfirmedById/OnMiladiDate/OnShamsiDate ،ObsoletedById/OnMiladiDate/OnShamsiDate و
			// HamkaranId عمداً از ورودی productionOrder کپی نمی‌شوند تا فقط از طریق Job راهکاران / DoObsolete
			// و همگام‌سازی راهکاران تغییر کنند

			var entity = await unitOfWork.Repository<ProductionOrder>().UpdateAsync(oldEntity, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<ProductionOrder>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<ProductionOrder>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<ProductionOrder>().TableNoTracking
					.Include(c => c.Contract)
					.Include(c => c.ProjectDl)
					.Include(c => c.SalesManager)
					.Include(c => c.ProjectManager)
					.Include(c => c.AlternativeExpert)
					.Include(c => c.SalesAgency)
					.Include(c => c.InstallationCity)
					.Include(c => c.SalesExpert)
					.Include(c => c.IntroducerExpert)
					.Include(c => c.Comments)
					.Include(c => c.ManagementConfirmationAttachmentFile)
					.Include(c => c.DepositFactorAttachmentFile)
					.Include(c => c.PreFactorAttachmentFile)
					.Include(c => c.ContractAttachmentFile)
					.Include(c => c.EngineeringAttachmentFile)
					.Include(c => c.Branch)
					.FirstOrDefault(c => c.Id == id);

				// تب‌های «تاخیرات» و «تاییدکننده تجهیز» در Edit.cshtml (فاز ۶ رابط کاربری): چون موتور FetchData
				// عمومی غیرفعال است (نگاه کن به TODO در FetchData پایین)، لیست‌ها مستقیماً از طریق ViewBag با
				// همان الگوی ViewBag.ProductionOrderItemBomViewData در ProductionOrderItemController.Edit تامین می‌شوند
				ViewBag.ProductionOrderDelayViewData = unitOfWork.Repository<ProductionOrderDelay>()
					.TableNoTracking
					.Include(c => c.ProductionOrderItem)
					.ThenInclude(c => c.Part)
					.Where(c => c.ProductionOrderId == id)
					.OrderByDescending(c => c.CreatedOnMiladiDateTime)
					.ToList();

				return View(@"\Views\Panel\Sale\ProductionOrder\Edit.cshtml", entity);
			}
			var newEntity = new ProductionOrder();
			return View(@"\Views\Panel\Sale\ProductionOrder\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var newEntity = new ProductionOrder();
			return View(@"\Views\Panel\Sale\ProductionOrder\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Sale\ProductionOrder\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<ProductionOrder>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			// TODO: محدودیت پلتفرمی - بررسی شد: Repository.FetchDataAsync در Data/Repositories/Repository.cs:746
			// فعلاً به‌صورت واقعی `throw new NotImplementedException()` دارد. جستجوی سراسری کل پروژه برای
			// "FetchDataAsync" نشان می‌دهد هیچ override/کلاس مشتق/ثبت DI جایگزینی برای Repository<TEntity> وجود
			// ندارد؛ تمام کنترلرهای دیگر (از جمله OpenOrderRequestController) نیز مستقیماً همین متد پایه را
			// صدا می‌زنند و در Runtime با همین Exception مواجه می‌شوند. رفع کامل موتور DataTable خارج از scope
			// این تغییر است. GetRowSecurityPredicate() از هم‌اکنون آماده است تا به محض رفع FetchDataAsync،
			// نتیجه‌اش به‌صورت predicate پایه (AND) با کوئری اصلی ترکیب شود.
			_ = GetRowSecurityPredicate(); // فراخوانی مستند/آماده برای سیم‌کشی آینده؛ فعلاً در پاسخ اثری ندارد
			return Ok(await unitOfWork.Repository<ProductionOrder>().FetchDataAsync(request, cn));
		}

		#region Obsolete Workflow Action

		// «تایید مالی» از پنل حذف شد (تصمیم 2026-09-17): مثل HTS، منبع حقیقت تایید مالی فقط راهکاران است و State
		// از طریق Job AddProductionOrderFromRahkaran / CheckFinancialConfirmsOrders همگام می‌شود.

		[HttpPost("[action]/{id}")]
		[ActionDisplayName("منسوخ‌سازی", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> DoObsolete(long id, CancellationToken cn)
		{
			try
			{
				//Seed Role => Sale.ProductionOrder.Obsolete => سفارش ساخت - منسوخ‌سازی
				// TODO(Role): Role را در پنل مدیریت نقش‌ها ایجاد و کاربران مجاز به منسوخ‌سازی سفارش ساخت را به آن انتساب بده
				if (!IsAdministrator && !CurrentUserHasAnyRole("Sale.ProductionOrder.Obsolete"))
					return Unauthorized("شما دسترسی منسوخ‌سازی سفارش ساخت را ندارید");

				var productionOrder = await unitOfWork.Repository<ProductionOrder>()
					.Table
					.FirstOrDefaultAsync(x => x.Id == id, cn);

				if (productionOrder == null)
					return BadRequest("سفارش ساخت یافت نشد");

				if (productionOrder.State == ProductionOrderStateEnum.Obsolete)
					return BadRequest("این سفارش قبلاً منسوخ شده است");

				if (productionOrder.State != ProductionOrderStateEnum.FinancialApproval)
					return BadRequest("فقط سفارش‌های تایید مالی شده قابل منسوخ‌سازی هستند");

				var previousState = productionOrder.State;
				var now = DateTime.Now;

				// State از طریق ProductionOrderCommentAction پس از درج کامنت همگام می‌شود
				productionOrder.ObsoletedById = CurrentUserId;
				productionOrder.ObsoletedOnMiladiDate = now;
				productionOrder.ObsoletedOnShamsiDate = now.ToShamsiDateTime();

				var headerComment = new ProductionOrderComment
				{
					ProductionOrderId = id,
					PreviousState = previousState,
					NewState = ProductionOrderStateEnum.Obsolete,
					Comment = "منسوخ‌سازی سفارش ساخت"
				};

				// کسکید به تمام اقلامِ آخرین نسخه که هنوز حذف/منسوخ نشده‌اند
				var items = await unitOfWork.Repository<ProductionOrderItem>()
					.Table
					.Where(x => x.ProductionOrderId == id &&
								x.IsLatestVersion &&
								x.IsActive != Entities.Base.IsActiveEnum.Deleted &&
								x.CheckStatus != ProductionOrderItemCheckStatusEnum.Deprecated)
					.ToListAsync(cn);

				var itemComments = new List<ProductionOrderItemComment>(items.Count);
				foreach (var item in items)
				{
					item.CheckStatus = ProductionOrderItemCheckStatusEnum.Deprecated;
					item.CheckStatusChangedOnMiladiDate = now;
					item.CheckStatusChangedOnShamsiDate = now.ToShamsiDateTime();
					item.ProductionStep = ProductionOrderItemProductionStepEnum.Canceled;

					itemComments.Add(new ProductionOrderItemComment
					{
						ProductionOrderItemId = (long)item.Id!,
						ProductionStatus = ProductionOrderItemProductionStatusEnum.Obsolete,
						Comment = "منسوخ‌سازی خودکار به علت منسوخ‌سازی سفارش ساخت",
						StartMiladiDateTime = now,
						StartShamsiDate = now.ToShamsiDateTime()
					});
				}

				// productionOrder و items هر دو از طریق Table (Tracked) بارگذاری شده‌اند؛ UpdateAsync زیر همان
				// DbContext را Save می‌کند و در نتیجه تغییرات آیتم‌های کسکید شده را هم به‌صورت یک‌جا ذخیره می‌کند
				await unitOfWork.Repository<ProductionOrder>().UpdateAsync(productionOrder, cn, true);
				await unitOfWork.Repository<ProductionOrderComment>().AddAsync(headerComment, cn, true);

				if (itemComments.Count > 0)
					await unitOfWork.Repository<ProductionOrderItemComment>().AddRangeAsync(itemComments, cn, false);

				await unitOfWork.SaveChangesAsync(cn);

				productionOrder = await unitOfWork.Repository<ProductionOrder>()
					.TableNoTracking.FirstAsync(x => x.Id == id, cn);

				return Ok(productionOrder);
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در منسوخ‌سازی سفارش ساخت: " + ex.Message);
			}
		}

		#endregion

		#region Delay / Equipment Confirmer Embedded Tabs (فاز ۶ - رابط کاربری)

		// اکشن‌های جزئی/ذخیره زیر، مطابق دقیق الگوی ProductionOrderItemBomPartial/SaveBom در
		// ProductionOrderItemController، برای پشتیبانی از تب‌های «تاخیرات» و «تاییدکننده تجهیز» در
		// ProductionOrder/Edit.cshtml اضافه شده‌اند (چون موتور FetchData عمومی غیرفعال است و امکان استفاده
		// از datatableprofile برای این لیست‌های تعبیه‌شده وجود ندارد).

		[HttpGet("[action]")]
		public IActionResult ProductionOrderDelayPartial(long? id, long productionOrderId)
		{
			if (id != null && id > 0)
			{
				var delay = unitOfWork.Repository<ProductionOrderDelay>()
					.TableNoTracking
					.Include(c => c.ProductionOrderItem)
					.ThenInclude(c => c.Part)
					.FirstOrDefault(c => c.Id == id);
				return PartialView(@"\Views\Panel\Sale\ProductionOrder\_ProductionOrderDelayPartial.cshtml", delay);
			}

			var newDelay = new ProductionOrderDelay { ProductionOrderId = productionOrderId };
			return PartialView(@"\Views\Panel\Sale\ProductionOrder\_ProductionOrderDelayPartial.cshtml", newDelay);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره تاخیر سفارش ساخت", ActionAccessType.Api)]
		public async Task<IActionResult> SaveProductionOrderDelay(ProductionOrderDelay productionOrderDelay, CancellationToken cn)
		{
			ProductionOrderDelay entity = (productionOrderDelay.Id == null || productionOrderDelay.Id == 0)
				? await unitOfWork.Repository<ProductionOrderDelay>().SaveAsync(productionOrderDelay, cn, true)
				: await unitOfWork.Repository<ProductionOrderDelay>().UpdateAsync(productionOrderDelay, cn, true);

			var viewModel = await unitOfWork.Repository<ProductionOrderDelay>()
				.TableNoTracking
				.Include(c => c.ProductionOrderItem)
				.ThenInclude(c => c.Part)
				.FirstOrDefaultAsync(c => c.Id == entity.Id, cn);

			return Ok(viewModel);
		}

		// توجه: اکشن‌های مدیریت «تاییدکننده تجهیز» به‌ازای هر سفارش ساخت حذف شدند؛ این تنظیم اکنون
		// یک تنظیم سراسری بر اساس نوع دستگاه است (نگاه کن به ProductionOrderEquipmentConfirmerController).

		#endregion

		#region Row-Level Security

		/// <summary>
		/// Predicate سطح ردیف برای محدودسازی نمایش سفارش‌های ساخت به کاربران مرتبط.
		/// در حال حاضر به دلیل غیرفعال بودن موتور FetchData (نگاه کن به TODO در اکشن FetchData) در پاسخ واقعی
		/// اعمال نمی‌شود؛ اما آماده است تا به محض رفع محدودیت پلتفرمی در ساخت کوئری اصلی AND شود.
		/// نکته: BaseEntity/ProductionOrder فیلد "CreatedOrganizationUnitId" ندارند (فقط CreatedById) - بنابراین
		/// شرط واحد سازمانیِ ایجادکننده که در پلن ذکر شده، در این نسخه قابل افزودن نبود؛ برای پیاده‌سازی کامل آن
		/// باید یا این فیلد به BaseEntity/ProductionOrder اضافه شود یا از طریق join با جدول Users
		/// (CreatedById -> OrganizationUnitId) فیلتر گردد.
		/// </summary>
		private Expression<Func<ProductionOrder, bool>> GetRowSecurityPredicate()
		{
			//Seed Role => Sale.ProductionOrder.ShowAll => سفارش ساخت - نمایش همه سفارش‌ها
			// TODO(Role): Role را در پنل مدیریت نقش‌ها ایجاد و کاربران ناظر/مدیریت ارشد را به آن انتساب بده
			if (IsAdministrator || CurrentUserHasAnyRole("Sale.ProductionOrder.ShowAll"))
				return _ => true;

			var currentUserId = CurrentUserId;

			return po =>
				po.CreatedById == currentUserId ||
				po.SalesExpertId == currentUserId ||
				po.SalesManagerId == currentUserId ||
				po.AlternativeExpertId == currentUserId ||
				po.ProjectManagerId == currentUserId;
		}

		#endregion
	}
}
