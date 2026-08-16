using Aspose.Cells;
using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Data.SystemAuth;
using Entities.App.Prd;
using Entities.App.Prd.Enums;
using Entities.App.Sale;
using Entities.App.Sale.Enums;
using Entities.Auth;
using Entities.Base.DataTable;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Auth;
using System.Text;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("درخواست توقف", typeof(StopRequst))]
	public class StopRequstController(
		IUnitOfWork unitOfWork,
		IPropertyIdentityService identityService,
		IWebHostEnvironment _webHostEnvironment,
		IUserService userService) : BaseController
	{
		private const string RoleRequester = "Prd.StopRequst.Requester";
		private const string RoleExpert = "Prd.StopsManagement.ProductionExpert";
		private const string RoleResponsible = "Prd.StopsManagement.ResponsibleUsers";
		private const string RoleCft = "Prd.StopsManagement.CftTeam";
		private const string RoleQc = "Prd.StopsManagement.QcManager";
		private const string RoleShowAll = "Prd.StopsManagement.ShowAll";

		#region CRUD

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(StopRequst stopRequst, CancellationToken cn)
		{
			if (stopRequst.Id == null || stopRequst.Id == 0)
				return await Add(stopRequst, cn);

			var exist = await unitOfWork.Repository<StopRequst>().TableNoTracking.AnyAsync(c => c.Id == stopRequst.Id, cn);
			return exist ? await Update(stopRequst, cn) : await Add(stopRequst, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(StopRequst stopRequst, CancellationToken cn)
		{
			NormalizeDates(stopRequst);

			var validation = ValidateRequesterFields(stopRequst);
			if (validation != null)
				return BadRequest(validation);

			stopRequst.LastStatus = stopRequst.ProductionStatus == StopRequstProductionStatusEnum.StartStopInTest
				? StopRequstModuleStatusEnum.RegisteredInProductTest
				: StopRequstModuleStatusEnum.PreRegister;

			var entity = await unitOfWork.Repository<StopRequst>().SaveAsync(stopRequst, cn, true);

			await AddCommentToProductionOrderItem(entity, isEndStop: false, cn);
			await AddStopComment(entity.Id!.Value, entity.LastStatus, "ثبت درخواست توقف", cn);
			await SendStatusNotification(entity, cn);

			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(StopRequst stopRequst, CancellationToken cn)
		{
			NormalizeDates(stopRequst);

			var existing = await unitOfWork.Repository<StopRequst>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == stopRequst.Id, cn);
			if (existing == null)
				return BadRequest("درخواست توقف یافت نشد");

			var editableByRequester = existing.LastStatus is StopRequstModuleStatusEnum.PreRegister
				or StopRequstModuleStatusEnum.Edited
				or StopRequstModuleStatusEnum.RegisteredInProductTest;

			if (IsAdministrator)
			{
				stopRequst.LastStatus ??= existing.LastStatus;
			}
			else
			{
				var isOwner = existing.CreatedById != null && existing.CreatedById == CurrentUserId;
				if (!editableByRequester || !isOwner)
					return BadRequest("در این وضعیت امکان ویرایش وجود ندارد");

				var validation = ValidateRequesterFields(stopRequst);
				if (validation != null)
					return BadRequest(validation);

				stopRequst.LastStatus = StopRequstModuleStatusEnum.Edited;
			}

			var entity = await unitOfWork.Repository<StopRequst>().UpdateAsync(stopRequst, cn, true);

			if (!IsAdministrator || editableByRequester)
			{
				await AddStopComment(entity.Id!.Value, entity.LastStatus, "ویرایش درخواست توقف", cn);
				await SendStatusNotification(entity, cn);
			}

			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = await unitOfWork.Repository<StopRequst>().Table.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (model == null)
				return Ok();

			model.LastStatus = StopRequstModuleStatusEnum.Deleted;
			await unitOfWork.Repository<StopRequst>().UpdateAsync(model, cn, true);
			await AddStopComment(id, StopRequstModuleStatusEnum.Deleted, "حذف درخواست توقف", cn);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			SetWorkflowViewBag();

			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<StopRequst>().TableNoTracking
					.Include(c => c.ProductionOrderItem).ThenInclude(p => p!.ProductionOrder)
					.Include(c => c.ProductionOrderItem).ThenInclude(p => p!.Part)
					.Include(c => c.Comments)
					.Include(c => c.Attachments).ThenInclude(a => a.Attachment)
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Prd\StopRequst\Edit.cshtml", entity ?? new StopRequst());
			}

			return View(@"\Views\Panel\Prd\StopRequst\Edit.cshtml", new StopRequst());
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			SetWorkflowViewBag();
			return View(@"\Views\Panel\Prd\StopRequst\Edit.cshtml", new StopRequst());
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Prd\StopRequst\List.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("مدیریت توقفات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ManagementList()
		{
			return View(@"\Views\Panel\Prd\StopsManagement\List.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش مدیریت توقفات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult ManagementEdit(long? id)
		{
			// یک صفحه واحد: ایجاد + تغییر وضعیت در StopRequst/Edit
			if (id == null || id == 0)
				return RedirectToAction(nameof(New));
			return RedirectToAction(nameof(Edit), new { id });
		}

		[HttpGet("[action]")]
		[ActionDisplayName("گزارش توقفات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ReportList()
		{
			return View(@"\Views\Panel\Prd\StopsManagement\Report.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<StopRequst>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
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
			return Ok(await unitOfWork.Repository<StopRequst>().FetchDataAsync(request, cn));
		}

		#endregion

		#region Workflow actions

		[HttpPost("[action]")]
		[ActionDisplayName("تایید کارشناس تولید", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> ExpertConfirm([FromBody] StopWorkflowRequest request, CancellationToken cn)
		{
			if (!IsAdministrator && !CurrentUserHasAnyRole(RoleExpert, RoleShowAll))
				return Unauthorized("شما دسترسی تایید کارشناس تولید را ندارید");

			var entity = await LoadStop(request.Id, cn);
			if (entity == null) return BadRequest("درخواست توقف یافت نشد");

			if (entity.LastStatus is not (StopRequstModuleStatusEnum.PreRegister
				or StopRequstModuleStatusEnum.Edited
				or StopRequstModuleStatusEnum.StopingFactorDisApprove
				or StopRequstModuleStatusEnum.RegisteredInProductTest))
				return BadRequest("وضعیت فعلی مجاز برای تایید کارشناس نیست");

			if (string.IsNullOrWhiteSpace(request.ResponsibleUnitIds) || string.IsNullOrWhiteSpace(request.ResponsibleUserIds))
				return BadRequest("واحد و مسئول عامل توقف الزامی است");

			entity.ResponsibleUnitIds = request.ResponsibleUnitIds;
			entity.ResponsibleUnitTitles = request.ResponsibleUnitTitles;
			entity.ResponsibleUserIds = request.ResponsibleUserIds;
			entity.ResponsibleUserNames = request.ResponsibleUserNames;
			if (!string.IsNullOrWhiteSpace(request.BeneficiarieUserIds))
			{
				entity.BeneficiarieUserIds = request.BeneficiarieUserIds;
				entity.BeneficiarieUserNames = request.BeneficiarieUserNames;
			}
			if (!string.IsNullOrWhiteSpace(request.Description))
				entity.Description = request.Description;

			entity.LastStatus = StopRequstModuleStatusEnum.ProductionExpertApprove;
			await unitOfWork.Repository<StopRequst>().UpdateAsync(entity, cn, true);
			await AddStopComment(entity.Id!.Value, entity.LastStatus, request.Comment ?? "تایید کارشناس تولید", cn);
			await SendStatusNotification(entity, cn);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("رد کارشناس تولید", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> ExpertReject([FromBody] StopWorkflowRequest request, CancellationToken cn)
		{
			if (!IsAdministrator && !CurrentUserHasAnyRole(RoleExpert, RoleShowAll))
				return Unauthorized("شما دسترسی رد کارشناس تولید را ندارید");

			if (string.IsNullOrWhiteSpace(request.Comment))
				return BadRequest("شرح رد الزامی است");

			var entity = await LoadStop(request.Id, cn);
			if (entity == null) return BadRequest("درخواست توقف یافت نشد");

			if (entity.LastStatus is not (StopRequstModuleStatusEnum.PreRegister
				or StopRequstModuleStatusEnum.Edited
				or StopRequstModuleStatusEnum.StopingFactorDisApprove
				or StopRequstModuleStatusEnum.RegisteredInProductTest))
				return BadRequest("وضعیت فعلی مجاز برای رد کارشناس نیست");

			entity.LastStatus = StopRequstModuleStatusEnum.ProductionExpertDisApprove;
			await unitOfWork.Repository<StopRequst>().UpdateAsync(entity, cn, true);
			await AddStopComment(entity.Id!.Value, entity.LastStatus, request.Comment, cn);
			await AddCommentToProductionOrderItem(entity, isEndStop: true, cn, request.Comment + " - عدم تایید کارشناس تولید");
			await SendStatusNotification(entity, cn);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("تایید مسئول عامل", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> ResponsibleConfirm([FromBody] StopWorkflowRequest request, CancellationToken cn)
		{
			if (!IsAdministrator && !CurrentUserHasAnyRole(RoleResponsible, RoleShowAll))
				return Unauthorized("شما دسترسی تایید مسئول عامل را ندارید");

			var entity = await LoadStop(request.Id, cn);
			if (entity == null) return BadRequest("درخواست توقف یافت نشد");

			if (!CanActAsResponsible(entity))
				return Unauthorized("شما مسئول عامل این توقف نیستید");

			if (entity.LastStatus is not (StopRequstModuleStatusEnum.ProductionExpertApprove
				or StopRequstModuleStatusEnum.RegisteredInProductTest
				or StopRequstModuleStatusEnum.CftLeadApproved))
				return BadRequest("وضعیت فعلی مجاز برای تایید مسئول عامل نیست");

			if (string.IsNullOrWhiteSpace(request.Comment))
				return BadRequest("شرح اقدامات الزامی است");

			entity.ResponsibleUserDescription = request.Comment;
			if (!string.IsNullOrWhiteSpace(request.SuggestedDateForFinishOfStopShamsi))
			{
				entity.SuggestedDateForFinishOfStopShamsi = request.SuggestedDateForFinishOfStopShamsi;
				entity.SuggestedDateForFinishOfStop = ParseShamsiDate(request.SuggestedDateForFinishOfStopShamsi)
					?? request.SuggestedDateForFinishOfStop;
			}
			else if (request.SuggestedDateForFinishOfStop.HasValue)
			{
				entity.SuggestedDateForFinishOfStop = request.SuggestedDateForFinishOfStop;
				entity.SuggestedDateForFinishOfStopShamsi = request.SuggestedDateForFinishOfStop.Value.ToShamsiDate();
			}

			entity.LastStatus = StopRequstModuleStatusEnum.StopingFactorApprove;
			await unitOfWork.Repository<StopRequst>().UpdateAsync(entity, cn, true);
			await AddStopComment(entity.Id!.Value, entity.LastStatus, "تایید مسئول عامل توقف", cn);
			await SendStatusNotification(entity, cn);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("رد مسئول عامل", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> ResponsibleReject([FromBody] StopWorkflowRequest request, CancellationToken cn)
		{
			if (!IsAdministrator && !CurrentUserHasAnyRole(RoleResponsible, RoleShowAll))
				return Unauthorized("شما دسترسی رد مسئول عامل را ندارید");

			var entity = await LoadStop(request.Id, cn);
			if (entity == null) return BadRequest("درخواست توقف یافت نشد");

			if (!CanActAsResponsible(entity))
				return Unauthorized("شما مسئول عامل این توقف نیستید");

			if (entity.LastStatus is not (StopRequstModuleStatusEnum.ProductionExpertApprove
				or StopRequstModuleStatusEnum.RegisteredInProductTest
				or StopRequstModuleStatusEnum.CftLeadApproved))
				return BadRequest("وضعیت فعلی مجاز برای رد مسئول عامل نیست");

			if (string.IsNullOrWhiteSpace(request.Comment))
				return BadRequest("شرح رد الزامی است");

			entity.ResponsibleUserDescription = request.Comment;
			if (!string.IsNullOrWhiteSpace(request.ResponsibleUnitIds))
			{
				entity.ResponsibleUnitIds = request.ResponsibleUnitIds;
				entity.ResponsibleUnitTitles = request.ResponsibleUnitTitles;
			}
			if (!string.IsNullOrWhiteSpace(request.ResponsibleUserIds))
			{
				entity.ResponsibleUserIds = request.ResponsibleUserIds;
				entity.ResponsibleUserNames = request.ResponsibleUserNames;
			}

			entity.LastStatus = StopRequstModuleStatusEnum.StopingFactorDisApprove;
			await unitOfWork.Repository<StopRequst>().UpdateAsync(entity, cn, true);
			await AddStopComment(entity.Id!.Value, entity.LastStatus, request.Comment, cn);
			await SendStatusNotification(entity, cn, request.ResponsibleUserNames, request.ResponsibleUnitTitles);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ارسال به CFT", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> SendToCft([FromBody] StopWorkflowRequest request, CancellationToken cn)
		{
			if (!IsAdministrator && !CurrentUserHasAnyRole(RoleExpert, RoleShowAll))
				return Unauthorized("شما دسترسی ارسال به CFT را ندارید");

			var entity = await LoadStop(request.Id, cn);
			if (entity == null) return BadRequest("درخواست توقف یافت نشد");

			if (entity.LastStatus != StopRequstModuleStatusEnum.StopingFactorApprove
				&& entity.LastStatus != StopRequstModuleStatusEnum.EffectivenessMeasuresDisApproved)
				return BadRequest("وضعیت فعلی مجاز برای ارسال به CFT نیست");

			entity.IsNeedToCftTeam = request.IsNeedToCftTeam ?? true;
			entity.SessionResult = request.Comment;
			entity.ProductionSessionShamsiDate = request.ProductionSessionShamsiDate;
			entity.ProductionSessionDate = ParseShamsiDate(request.ProductionSessionShamsiDate) ?? request.ProductionSessionDate;
			entity.SessionUserIds = request.SessionUserIds;
			entity.SessionUserNames = request.SessionUserNames;

			if (entity.IsNeedToCftTeam == true)
			{
				entity.LastStatus = StopRequstModuleStatusEnum.SendToCFTTeam;
				await unitOfWork.Repository<StopRequst>().UpdateAsync(entity, cn, true);
				await AddStopComment(entity.Id!.Value, entity.LastStatus, request.Comment ?? "ارسال به کارتابل تیم CFT", cn);
			}
			else
			{
				if (!string.IsNullOrWhiteSpace(request.ResponsibleUnitIds))
				{
					entity.ResponsibleUnitIds = request.ResponsibleUnitIds;
					entity.ResponsibleUnitTitles = request.ResponsibleUnitTitles;
				}
				if (!string.IsNullOrWhiteSpace(request.ResponsibleUserIds))
				{
					entity.ResponsibleUserIds = request.ResponsibleUserIds;
					entity.ResponsibleUserNames = request.ResponsibleUserNames;
				}
				if (!string.IsNullOrWhiteSpace(request.BeneficiarieUserIds))
				{
					entity.BeneficiarieUserIds = request.BeneficiarieUserIds;
					entity.BeneficiarieUserNames = request.BeneficiarieUserNames;
				}
				entity.LastStatus = StopRequstModuleStatusEnum.ProductionExpertApprove;
				await unitOfWork.Repository<StopRequst>().UpdateAsync(entity, cn, true);
				await AddStopComment(entity.Id!.Value, entity.LastStatus, request.Comment ?? "بازگشت به مسئول عامل پس از جلسه", cn);
			}

			await SendStatusNotification(entity, cn);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("اقدام تیم CFT", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> CftOperate([FromBody] StopWorkflowRequest request, CancellationToken cn)
		{
			if (!IsAdministrator && !CurrentUserHasAnyRole(RoleCft, RoleShowAll))
				return Unauthorized("شما دسترسی تیم CFT را ندارید");

			var entity = await LoadStop(request.Id, cn);
			if (entity == null) return BadRequest("درخواست توقف یافت نشد");

			if (entity.LastStatus != StopRequstModuleStatusEnum.SendToCFTTeam)
				return BadRequest("وضعیت فعلی مجاز برای اقدام CFT نیست");

			entity.CftDescription = request.Comment;
			entity.IsNeedToCftTeam = true;
			if (!string.IsNullOrWhiteSpace(request.ResponsibleUnitIds))
			{
				entity.ResponsibleUnitIds = request.ResponsibleUnitIds;
				entity.ResponsibleUnitTitles = request.ResponsibleUnitTitles;
			}
			if (!string.IsNullOrWhiteSpace(request.ResponsibleUserIds))
			{
				entity.ResponsibleUserIds = request.ResponsibleUserIds;
				entity.ResponsibleUserNames = request.ResponsibleUserNames;
			}
			if (!string.IsNullOrWhiteSpace(request.BeneficiarieUserIds))
			{
				entity.BeneficiarieUserIds = request.BeneficiarieUserIds;
				entity.BeneficiarieUserNames = request.BeneficiarieUserNames;
			}

			entity.LastStatus = StopRequstModuleStatusEnum.CftLeadApproved;
			await unitOfWork.Repository<StopRequst>().UpdateAsync(entity, cn, true);
			await AddStopComment(entity.Id!.Value, entity.LastStatus, request.Comment ?? "تایید مدیر CFT", cn);
			await SendStatusNotification(entity, cn);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("تایید اثربخشی", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> EffectivityApprove([FromBody] StopWorkflowRequest request, CancellationToken cn)
		{
			if (!IsAdministrator && !CurrentUserHasAnyRole(RoleExpert, RoleShowAll))
				return Unauthorized("شما دسترسی تایید اثربخشی را ندارید");

			var entity = await LoadStop(request.Id, cn);
			if (entity == null) return BadRequest("درخواست توقف یافت نشد");

			if (entity.LastStatus != StopRequstModuleStatusEnum.StopingFactorApprove)
				return BadRequest("وضعیت فعلی مجاز برای تایید اثربخشی نیست");

			var now = DateTime.Now;
			entity.IsEffective = true;
			entity.EffectivityDescription = request.Comment;
			entity.StopFinishDate = now;
			entity.StopFinishShamsiDate = now.ToShamsiDateTime();

			if (!string.IsNullOrWhiteSpace(request.BeneficiarieUserIds))
			{
				entity.BeneficiarieUserIds = request.BeneficiarieUserIds;
				entity.BeneficiarieUserNames = request.BeneficiarieUserNames;
			}

			if (entity.ProductionStatus == StopRequstProductionStatusEnum.StartStopInFinalInspection)
			{
				entity.LastStatus = StopRequstModuleStatusEnum.CheckByQcManager;
				await unitOfWork.Repository<StopRequst>().UpdateAsync(entity, cn, true);
				await AddStopComment(entity.Id!.Value, entity.LastStatus, request.Comment ?? "ارسال به مدیر QC جهت بازرسی نهایی", cn);
			}
			else
			{
				entity.LastStatus = StopRequstModuleStatusEnum.EffectivenessMeasuresApproved;
				await unitOfWork.Repository<StopRequst>().UpdateAsync(entity, cn, true);
				await AddStopComment(entity.Id!.Value, entity.LastStatus, request.Comment ?? "تایید اثر بخش بودن اقدامات", cn);
				await AddCommentToProductionOrderItem(entity, isEndStop: true, cn, "پایان توقف - تایید اثربخشی");
			}

			await SendStatusNotification(entity, cn);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("عدم تایید اثربخشی", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> EffectivityDisapprove([FromBody] StopWorkflowRequest request, CancellationToken cn)
		{
			if (!IsAdministrator && !CurrentUserHasAnyRole(RoleExpert, RoleShowAll))
				return Unauthorized("شما دسترسی عدم تایید اثربخشی را ندارید");

			var entity = await LoadStop(request.Id, cn);
			if (entity == null) return BadRequest("درخواست توقف یافت نشد");

			if (entity.LastStatus != StopRequstModuleStatusEnum.StopingFactorApprove)
				return BadRequest("وضعیت فعلی مجاز برای عدم تایید اثربخشی نیست");

			entity.IsEffective = false;
			entity.EffectivityDescription = request.Comment;
			entity.LastStatus = StopRequstModuleStatusEnum.EffectivenessMeasuresDisApproved;
			await unitOfWork.Repository<StopRequst>().UpdateAsync(entity, cn, true);
			await AddStopComment(entity.Id!.Value, entity.LastStatus, request.Comment ?? "عدم تایید اثر بخش بودن اقدامات", cn);
			await SendStatusNotification(entity, cn);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("تایید مدیر QC", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> QcManagerApprove([FromBody] StopWorkflowRequest request, CancellationToken cn)
		{
			if (!IsAdministrator && !CurrentUserHasAnyRole(RoleQc, RoleShowAll))
				return Unauthorized("شما دسترسی تایید مدیر QC را ندارید");

			var entity = await LoadStop(request.Id, cn);
			if (entity == null) return BadRequest("درخواست توقف یافت نشد");

			if (entity.LastStatus != StopRequstModuleStatusEnum.CheckByQcManager)
				return BadRequest("وضعیت فعلی مجاز برای تایید مدیر QC نیست");

			var now = DateTime.Now;
			entity.IsEffective = true;
			entity.EffectivityDescription = request.Comment;
			entity.StopFinishDate = ParseShamsiDate(request.StopFinishShamsiDate) ?? request.StopFinishDate ?? now;
			entity.StopFinishShamsiDate = request.StopFinishShamsiDate ?? entity.StopFinishDate.Value.ToShamsiDateTime();
			entity.LastStatus = StopRequstModuleStatusEnum.EffectivenessMeasuresApproved;

			await unitOfWork.Repository<StopRequst>().UpdateAsync(entity, cn, true);
			await AddStopComment(entity.Id!.Value, entity.LastStatus, request.Comment ?? "تایید اثر بخشی توسط مدیر QC", cn);
			await AddCommentToProductionOrderItem(entity, isEndStop: true, cn, (request.Comment ?? "") + " - تایید مدیر QC");
			await SendStatusNotification(entity, cn);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("رد مدیر QC", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> QcManagerDisapprove([FromBody] StopWorkflowRequest request, CancellationToken cn)
		{
			if (!IsAdministrator && !CurrentUserHasAnyRole(RoleQc, RoleShowAll))
				return Unauthorized("شما دسترسی رد مدیر QC را ندارید");

			var entity = await LoadStop(request.Id, cn);
			if (entity == null) return BadRequest("درخواست توقف یافت نشد");

			if (entity.LastStatus != StopRequstModuleStatusEnum.CheckByQcManager)
				return BadRequest("وضعیت فعلی مجاز برای رد مدیر QC نیست");

			entity.IsEffective = false;
			entity.EffectivityDescription = request.Comment;
			entity.LastStatus = StopRequstModuleStatusEnum.EffectivenessMeasuresDisApproved;
			await unitOfWork.Repository<StopRequst>().UpdateAsync(entity, cn, true);
			await AddStopComment(entity.Id!.Value, entity.LastStatus, request.Comment ?? "عدم تایید اثر بخشی توسط مدیر QC", cn);
			await SendStatusNotification(entity, cn);
			return Ok(entity);
		}

		#endregion

		#region Attachments

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره پیوست", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> SaveAttachment(StopRequstAttachment attachment, CancellationToken cn)
		{
			if (attachment.StopRequstId <= 0)
				return BadRequest("شناسه درخواست توقف نامعتبر است");

			if (attachment.Id == null || attachment.Id == 0)
			{
				var entity = await unitOfWork.Repository<StopRequstAttachment>().SaveAsync(attachment, cn, true);
				return Ok(entity);
			}

			var updated = await unitOfWork.Repository<StopRequstAttachment>().UpdateAsync(attachment, cn, true);
			return Ok(updated);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف پیوست", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> DeleteAttachment(long id, CancellationToken cn)
		{
			var model = await unitOfWork.Repository<StopRequstAttachment>().Table.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (model != null)
				await unitOfWork.Repository<StopRequstAttachment>().DeleteAsync(model, cn, true);
			return Ok();
		}

		#endregion

		#region Helpers

		private void SetWorkflowViewBag()
		{
			ViewBag.IsExpert = IsAdministrator || CurrentUserHasAnyRole(RoleExpert, RoleShowAll);
			ViewBag.IsResponsible = IsAdministrator || CurrentUserHasAnyRole(RoleResponsible, RoleShowAll);
			ViewBag.IsCft = IsAdministrator || CurrentUserHasAnyRole(RoleCft, RoleShowAll);
			ViewBag.IsQc = IsAdministrator || CurrentUserHasAnyRole(RoleQc, RoleShowAll);
			ViewBag.CurrentUserId = CurrentUserId;
			ViewBag.BypassResponsibleAssignment = IsAdministrator || CurrentUserHasAnyRole(RoleShowAll);
		}

		private async Task<StopRequst?> LoadStop(long id, CancellationToken cn)
		{
			return await unitOfWork.Repository<StopRequst>().Table
				.Include(c => c.ProductionOrderItem)
				.FirstOrDefaultAsync(c => c.Id == id, cn);
		}

		private bool CanActAsResponsible(StopRequst entity)
		{
			if (IsAdministrator || CurrentUserHasAnyRole(RoleShowAll))
				return true;
			if (string.IsNullOrWhiteSpace(entity.ResponsibleUserIds) || CurrentUserId == null)
				return false;
			var ids = entity.ResponsibleUserIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			return ids.Contains(CurrentUserId.Value.ToString());
		}

		private static string? ValidateRequesterFields(StopRequst model)
		{
			if (model.ProductionOrderItemId == null || model.ProductionOrderItemId <= 0)
				return "قلم سفارش ساخت الزامی است";
			if (string.IsNullOrWhiteSpace(model.StopStartShamsiDateTime) && model.StopStartMiladiDateTime == null)
				return "تاریخ شروع توقف الزامی است";
			if (model.ObservationLocation == null)
				return "محل مشاهده الزامی است";
			if (model.ProductionStatus == null)
				return "وضعیت تولید الزامی است";
			if (string.IsNullOrWhiteSpace(model.StopReasonIds))
				return "علت توقف الزامی است";
			if (string.IsNullOrWhiteSpace(model.Description))
				return "شرح الزامی است";
			return null;
		}

		private static void NormalizeDates(StopRequst model)
		{
			if (!string.IsNullOrWhiteSpace(model.StopStartShamsiDateTime) && model.StopStartMiladiDateTime == null)
			{
				try { model.StopStartMiladiDateTime = model.StopStartShamsiDateTime.ToMiladiDateTime(); }
				catch { /* leave null; validation catches */ }
			}
			else if (model.StopStartMiladiDateTime.HasValue && string.IsNullOrWhiteSpace(model.StopStartShamsiDateTime))
			{
				model.StopStartShamsiDateTime = model.StopStartMiladiDateTime.Value.ToShamsiDateTime();
			}

			if (!string.IsNullOrWhiteSpace(model.SuggestedDateForFinishOfStopShamsi) && model.SuggestedDateForFinishOfStop == null)
				model.SuggestedDateForFinishOfStop = ParseShamsiDate(model.SuggestedDateForFinishOfStopShamsi);

			if (!string.IsNullOrWhiteSpace(model.ProductionSessionShamsiDate) && model.ProductionSessionDate == null)
				model.ProductionSessionDate = ParseShamsiDate(model.ProductionSessionShamsiDate);

			if (!string.IsNullOrWhiteSpace(model.StopFinishShamsiDate) && model.StopFinishDate == null)
			{
				try { model.StopFinishDate = model.StopFinishShamsiDate.ToMiladiDateTime(); }
				catch { }
			}
		}

		private static DateTime? ParseShamsiDate(string? shamsi)
		{
			if (string.IsNullOrWhiteSpace(shamsi)) return null;
			try
			{
				return shamsi.Length > 10
					? shamsi.ToMiladiDateTime()
					: shamsi.ToMiladiDate();
			}
			catch { return null; }
		}

		private async Task AddStopComment(long stopId, StopRequstModuleStatusEnum? status, string description, CancellationToken cn)
		{
			await unitOfWork.Repository<StopRequstComment>().AddAsync(new StopRequstComment
			{
				StopRequstId = stopId,
				Status = status,
				Description = description
			}, cn);
			await unitOfWork.SaveChangesAsync(cn);
		}

		private async Task AddCommentToProductionOrderItem(StopRequst model, bool isEndStop, CancellationToken cn, string? extraComment = null)
		{
			if (model.ProductionOrderItemId == null || model.ProductionOrderItemId <= 0)
				return;

			var now = DateTime.Now;
			var comment = new ProductionOrderItemComment
			{
				ProductionOrderItemId = model.ProductionOrderItemId.Value,
				StartMiladiDateTime = now,
				StartShamsiDate = now.ToShamsiDateTime(),
				IsForProductionMode = true,
				StopRequestId = model.Id,
				Comment = (extraComment ?? model.StopReasonTitles) + (isEndStop
					? " - ایجاد شده بصورت اتوماتیک از محل پایان/رد توقف در ماژول توقف"
					: " - ایجاد شده بصورت اتوماتیک از محل ایجاد توقف در ماژول توقف")
			};

			if (isEndStop)
			{
				comment.ProductionStatus = model.ProductionStatus switch
				{
					StopRequstProductionStatusEnum.StartStopInProduction => ProductionOrderItemProductionStatusEnum.ProductionStageStopEnded,
					StopRequstProductionStatusEnum.StartStopInTest => ProductionOrderItemProductionStatusEnum.ProductionTestStageStopEnded,
					StopRequstProductionStatusEnum.StartStopInFinalInspection => ProductionOrderItemProductionStatusEnum.FinalInspectionStageStopEnded,
					StopRequstProductionStatusEnum.StartStopDueToClientVisit => ProductionOrderItemProductionStatusEnum.ClientVisitStopEnded,
					_ => comment.ProductionStatus
				};
			}
			else
			{
				comment.ProductionStatus = model.ProductionStatus switch
				{
					StopRequstProductionStatusEnum.StartStopInProduction => ProductionOrderItemProductionStatusEnum.ProductionStageStopStarted,
					StopRequstProductionStatusEnum.StartStopInTest => ProductionOrderItemProductionStatusEnum.ProductionTestStageStopStarted,
					StopRequstProductionStatusEnum.StartStopInFinalInspection => ProductionOrderItemProductionStatusEnum.FinalInspectionStageStopStarted,
					StopRequstProductionStatusEnum.StartStopDueToClientVisit => ProductionOrderItemProductionStatusEnum.ClientVisitStopStarted,
					_ => comment.ProductionStatus
				};
			}

			await unitOfWork.Repository<ProductionOrderItemComment>().AddAsync(comment, cn);
			await unitOfWork.SaveChangesAsync(cn);
		}

		private async Task SendStatusNotification(StopRequst stop, CancellationToken cn, string? suggestedUserName = null, string? suggestedUnitName = null)
		{
			var status = stop.LastStatus ?? StopRequstModuleStatusEnum.PreRegister;
			var users = (await userService.GetAll())?.ToList() ?? new List<User>();
			var toEmails = new List<string>();
			var ccEmails = new List<string>();

			void AddEmailsByIds(string? csv, List<string> target)
			{
				if (string.IsNullOrWhiteSpace(csv)) return;
				foreach (var part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
				{
					if (!long.TryParse(part, out var uid)) continue;
					var email = users.FirstOrDefault(u => u.Id == uid)?.Email;
					if (!string.IsNullOrWhiteSpace(email))
						target.Add(email);
				}
			}

			async Task AddRoleEmails(string roleName, List<string> target)
			{
				var roleUsers = await userService.GetUsersByRoleName(roleName);
				if (roleUsers == null) return;
				foreach (var u in roleUsers)
				{
					if (!string.IsNullOrWhiteSpace(u.Email))
						target.Add(u.Email);
				}
			}

			switch (status)
			{
				case StopRequstModuleStatusEnum.PreRegister:
				case StopRequstModuleStatusEnum.Edited:
					await AddRoleEmails(RoleExpert, toEmails);
					AddEmailsByIds(stop.BeneficiarieUserIds, toEmails);
					AddEmailsByIds(stop.AlternativeBeneficiarieUserIds, ccEmails);
					break;

				case StopRequstModuleStatusEnum.RegisteredInProductTest:
					AddEmailsByIds(stop.BeneficiarieUserIds, toEmails);
					AddEmailsByIds(stop.AlternativeBeneficiarieUserIds, ccEmails);
					AddEmailsByIds(stop.ResponsibleUserIds, toEmails);
					break;

				case StopRequstModuleStatusEnum.ProductionExpertDisApprove:
					{
						var creatorEmail = users.FirstOrDefault(u => u.Id == stop.CreatedById)?.Email;
						if (!string.IsNullOrWhiteSpace(creatorEmail))
							toEmails.Add(creatorEmail);
					}
					break;

				case StopRequstModuleStatusEnum.ProductionExpertApprove:
					AddEmailsByIds(stop.BeneficiarieUserIds, toEmails);
					AddEmailsByIds(stop.AlternativeBeneficiarieUserIds, ccEmails);
					AddEmailsByIds(stop.ResponsibleUserIds, toEmails);
					{
						var creatorEmail = users.FirstOrDefault(u => u.Id == stop.CreatedById)?.Email;
						if (!string.IsNullOrWhiteSpace(creatorEmail))
							ccEmails.Add(creatorEmail);
					}
					break;

				case StopRequstModuleStatusEnum.StopingFactorDisApprove:
				case StopRequstModuleStatusEnum.StopingFactorApprove:
				case StopRequstModuleStatusEnum.SendToStopingFactorExpert:
					await AddRoleEmails(RoleExpert, toEmails);
					break;

				case StopRequstModuleStatusEnum.SendToCFTTeam:
					await AddRoleEmails(RoleCft, toEmails);
					break;

				case StopRequstModuleStatusEnum.CftLeadApproved:
					AddEmailsByIds(stop.ResponsibleUserIds, toEmails);
					await AddRoleEmails(RoleExpert, ccEmails);
					break;

				case StopRequstModuleStatusEnum.EffectivenessMeasuresApproved:
					AddEmailsByIds(stop.BeneficiarieUserIds, toEmails);
					AddEmailsByIds(stop.AlternativeBeneficiarieUserIds, ccEmails);
					break;

				case StopRequstModuleStatusEnum.EffectivenessMeasuresDisApproved:
					AddEmailsByIds(stop.ResponsibleUserIds, toEmails);
					await AddRoleEmails(RoleExpert, ccEmails);
					break;

				case StopRequstModuleStatusEnum.CheckByQcManager:
					await AddRoleEmails(RoleQc, toEmails);
					break;
			}

			toEmails = toEmails.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			ccEmails = ccEmails.Where(e => !string.IsNullOrWhiteSpace(e) && !toEmails.Contains(e, StringComparer.OrdinalIgnoreCase))
				.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

			if (toEmails.Count == 0 && ccEmails.Count == 0)
				return;

			var title = $"اعلان ثبت/ویرایش توقف شماره {stop.Id}";
			var body = BuildNotificationBody(stop, status, suggestedUserName, suggestedUnitName);

			await unitOfWork.Repository<Notification>().AddAsync(new Notification
			{
				Type = NotificationType.Email,
				Title = title,
				Body = body,
				EntityId = stop.Id,
				OwnerId = CurrentUserId ?? 0,
				ViewPath = $"/Panel/StopRequst/Edit?id={stop.Id}",
				IsRead = false,
				IsSend = false,
				ToEmails = toEmails,
				CcEmails = ccEmails
			}, cn);
			await unitOfWork.SaveChangesAsync(cn);
		}

		private string BuildNotificationBody(StopRequst stop, StopRequstModuleStatusEnum status, string? suggestedUserName, string? suggestedUnitName)
		{
			var sb = new StringBuilder();
			sb.AppendLine("<div style='text-align:center;direction:rtl'>");
			sb.AppendLine("<table border='1' cellspacing='0' cellpadding='5' style='text-align:right; direction: rtl' width='100%'>");
			sb.AppendLine("<tr style='background: #000aa0'><td colspan='2'><div style='font-size:14.0pt;font-family:Zar;color:#FFFFFF;text-align:center'>گروه صنعتی هوایار</div></td></tr>");
			sb.AppendLine("<tr><td colspan='2'><div style='font-size:12pt;font-family:Zar;text-align:right;direction:rtl'>");

			var actor = CurrentUserFullName ?? "سیستم";
			sb.Append(status switch
			{
				StopRequstModuleStatusEnum.PreRegister or StopRequstModuleStatusEnum.RegisteredInProductTest
					=> $"احتراماَ فرم <strong>توقف</strong> شماره <strong>{stop.Id}</strong> توسط <strong>{actor}</strong> ایجاد گردید",
				StopRequstModuleStatusEnum.Edited
					=> $"احتراماَ فرم <strong>توقف</strong> شماره <strong>{stop.Id}</strong> توسط <strong>{actor}</strong> ویرایش گردید",
				_
					=> $"احتراماَ وضعیت فرم <strong>توقف</strong> شماره <strong>{stop.Id}</strong> توسط <strong>{actor}</strong> به «{status.ToDisplay()}» تغییر یافت"
			});

			if (!string.IsNullOrWhiteSpace(suggestedUserName) || !string.IsNullOrWhiteSpace(suggestedUnitName))
			{
				sb.Append("<br/>پیشنهاد مسئول/واحد: ");
				sb.Append(suggestedUserName);
				if (!string.IsNullOrWhiteSpace(suggestedUnitName))
					sb.Append($" / {suggestedUnitName}");
			}

			sb.AppendLine("</div></td></tr>");
			sb.AppendLine("<tr><td>علت توقف</td><td>" + (stop.StopReasonTitles ?? "-") + "</td></tr>");
			sb.AppendLine("<tr><td>واحد عامل</td><td>" + (stop.ResponsibleUnitTitles ?? "-") + "</td></tr>");
			sb.AppendLine("<tr><td>مسئول عامل</td><td>" + (stop.ResponsibleUserNames ?? "-") + "</td></tr>");
			sb.AppendLine("<tr><td>ذینفعان</td><td>" + (stop.BeneficiarieUserNames ?? "-") + "</td></tr>");
			sb.AppendLine("<tr><td>شرح</td><td>" + (stop.Description ?? "-") + "</td></tr>");
			sb.AppendLine("<tr><td>وضعیت</td><td>" + status.ToDisplay() + "</td></tr>");
			sb.AppendLine("</table></div>");
			return sb.ToString();
		}

		#endregion
	}

	public class StopWorkflowRequest
	{
		public long Id { get; set; }
		public string? Comment { get; set; }
		public string? Description { get; set; }
		public string? ResponsibleUnitIds { get; set; }
		public string? ResponsibleUnitTitles { get; set; }
		public string? ResponsibleUserIds { get; set; }
		public string? ResponsibleUserNames { get; set; }
		public string? BeneficiarieUserIds { get; set; }
		public string? BeneficiarieUserNames { get; set; }
		public string? SuggestedDateForFinishOfStopShamsi { get; set; }
		public DateTime? SuggestedDateForFinishOfStop { get; set; }
		public bool? IsNeedToCftTeam { get; set; }
		public string? ProductionSessionShamsiDate { get; set; }
		public DateTime? ProductionSessionDate { get; set; }
		public string? SessionUserIds { get; set; }
		public string? SessionUserNames { get; set; }
		public string? StopFinishShamsiDate { get; set; }
		public DateTime? StopFinishDate { get; set; }
	}
}
