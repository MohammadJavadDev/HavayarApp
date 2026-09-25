using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Entities.App.FIN;
using Entities.App.Gnr;
using Entities.App.Hcm;
using Entities.App.Hrm;
using Entities.App.Srv;
using Entities.App.Srv.Enums;
using Entities.Base.DataTable;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Auth;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Srv/TicketHotelRequest")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("درخواست بلیط و هتل", typeof(TicketHotelRequest))]
	public class SrvTicketHotelRequestController(
		IUnitOfWork unitOfWork,
		IWebHostEnvironment env,
		IUserService userService) : BaseController
	{
		private const string RoleRequester = "Srv.TicketHotel.Requester";
		private const string RoleApprover = "Srv.TicketHotel.Approver";
		private const string RoleFullAccess = "Srv.TicketHotel.FullAccess";
		private const string RoleFullAccessConfirm = "Srv.TicketHotel.FullAccessConfirm";

		private bool CanShowAllRows =>
			IsAdministrator || CurrentUserHasAnyRole(RoleFullAccess, RoleFullAccessConfirm);

		private bool CanCrud =>
			IsAdministrator || CurrentUserHasAnyRole(RoleRequester, RoleApprover, RoleFullAccess, RoleFullAccessConfirm);

		private bool CanConfirmAction =>
			IsAdministrator || CurrentUserHasAnyRole(RoleApprover, RoleFullAccessConfirm);

		#region Primary CRUD

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(TicketHotelRequest model, CancellationToken cn)
		{
			if (!CanCrud) return BadRequest("دسترسی ذخیره وجود ندارد");
			ClearNav(model);
			var err = ValidateHeader(model);
			if (err != null) return BadRequest(err);

			if (model.Id == null || model.Id == 0)
				return await Add(model, cn);
			if (await unitOfWork.Repository<TicketHotelRequest>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(TicketHotelRequest model, CancellationToken cn)
		{
			if (!CanCrud) return BadRequest("دسترسی ذخیره وجود ندارد");
			ClearNav(model);
			var err = ValidateHeader(model);
			if (err != null) return BadRequest(err);

			if (CurrentOrganizationUnitId is null or 0)
				return BadRequest("واحد سازمانی کاربر جاری مشخص نیست");

			await ApplyApplicantsAsync(model, cn);
			model.DepartmentId = CurrentOrganizationUnitId;
			model.CreatedDateInText = ToPersianDateTimeShort(DateTime.Now);
			model.Tickets = null;
			model.Hotels = null;

			var saved = await unitOfWork.Repository<TicketHotelRequest>().SaveAsync(model, cn, true);
			await SendAddEmailAsync(saved, cn);
			return Ok(await LoadForEditAsync(saved.Id!.Value, cn));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(TicketHotelRequest model, CancellationToken cn)
		{
			if (!CanCrud) return BadRequest("دسترسی ذخیره وجود ندارد");
			ClearNav(model);
			var old = await unitOfWork.Repository<TicketHotelRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == model.Id, cn);
			if (old == null) return BadRequest("رکورد یافت نشد");
			if (!CanAccessRow(old)) return BadRequest("دسترسی به این رکورد وجود ندارد");

			var err = ValidateHeader(model);
			if (err != null) return BadRequest(err);

			if (CurrentOrganizationUnitId is null or 0)
				return BadRequest("واحد سازمانی کاربر جاری مشخص نیست");

			await ApplyApplicantsAsync(model, cn);
			model.DepartmentId = CurrentOrganizationUnitId;
			model.ApproverComment = old.ApproverComment;
			model.IsApproved = old.IsApproved;
			model.ApproverId = old.ApproverId;
			model.ApprovedDateTimeInText = old.ApprovedDateTimeInText;
			model.CreatedDateInText = old.CreatedDateInText;
			model.HtsId = old.HtsId;
			model.ProjectId = old.ProjectId;
			model.Tickets = null;
			model.Hotels = null;

			await unitOfWork.Repository<TicketHotelRequest>().UpdateAsync(model, cn, true);
			return Ok(await LoadForEditAsync(model.Id!.Value, cn));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			if (!CanCrud) return BadRequest("دسترسی حذف وجود ندارد");
			var entity = await unitOfWork.Repository<TicketHotelRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return Ok();
			if (!CanAccessRow(entity)) return BadRequest("دسترسی به این رکورد وجود ندارد");

			var hasTicket = await unitOfWork.Repository<TicketHotelRequestTicket>().TableNoTracking
				.AnyAsync(c => c.TicketHotelRequestId == id, cn);
			var hasHotel = await unitOfWork.Repository<TicketHotelRequestHotel>().TableNoTracking
				.AnyAsync(c => c.TicketHotelRequestId == id, cn);
			if (hasTicket || hasHotel)
				return BadRequest("این درخواست دارای بلیط یا هتل است و قابل حذف نیست");

			await unitOfWork.Repository<TicketHotelRequest>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public async Task<IActionResult> Edit(long? id, CancellationToken cn)
		{
			if (!CanCrud) return BadRequest("دسترسی ویرایش وجود ندارد");
			TicketHotelRequest entity;
			if (id != null && id != 0)
			{
				entity = await LoadForEditAsync(id.Value, cn) ?? new TicketHotelRequest();
				if (entity.Id is > 0 && !CanAccessRow(entity))
					return BadRequest("دسترسی به این رکورد وجود ندارد");
			}
			else
				entity = new TicketHotelRequest();

			return View(@"\Views\Panel\Srv\TicketHotelRequest\Edit.cshtml", entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			if (!CanCrud) return BadRequest("دسترسی درج وجود ندارد");
			return View(@"\Views\Panel\Srv\TicketHotelRequest\Edit.cshtml", new TicketHotelRequest());
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Srv\TicketHotelRequest\List.cshtml");

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<TicketHotelRequest>().FetchDataAsync(request, cn));

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + @"\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<TicketHotelRequest>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message);
			}
		}

		[HttpPost("[action]")]
		[ActionDisplayName("تایید", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> Confirm(long id, CancellationToken cn)
		{
			if (!CanConfirmAction) return BadRequest("دسترسی تایید وجود ندارد");
			var entity = await unitOfWork.Repository<TicketHotelRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return BadRequest("رکورد یافت نشد");
			if (!CanAccessRow(entity)) return BadRequest("دسترسی به این رکورد وجود ندارد");
			if (entity.IsApproved == true) return BadRequest("این درخواست قبلاً تایید شده است");

			entity.IsApproved = true;
			entity.ApproverId = CurrentUserId;
			entity.ApprovedDateTimeInText = ToPersianDateTimeShort(DateTime.Now);
			await unitOfWork.Repository<TicketHotelRequest>().UpdateAsync(entity, cn, true);
			return Ok();
		}

		#endregion

		#region Ticket helpers (no ActionDisplayName)

		[HttpGet("[action]")]
		public async Task<IActionResult> GetTickets(long ticketHotelRequestId, CancellationToken cn)
		{
			var parent = await unitOfWork.Repository<TicketHotelRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == ticketHotelRequestId, cn);
			if (parent == null) return BadRequest("درخواست یافت نشد");
			if (!CanAccessRow(parent)) return BadRequest("دسترسی به این رکورد وجود ندارد");

			var items = await unitOfWork.Repository<TicketHotelRequestTicket>().TableNoTracking
				.Where(c => c.TicketHotelRequestId == ticketHotelRequestId)
				.OrderBy(c => c.Id)
				.ToListAsync(cn);

			var result = items.Select(c => new
			{
				c.Id,
				c.TicketTypeId,
				TicketTypeTitle = c.TicketTypeId.ToDisplay(),
				c.Airline,
				c.PassengerTerminal,
				c.RailwayStation,
				c.TravelAgency,
				c.IssueDate,
				c.TicketNumber,
				c.TicketAmount,
				c.Source,
				c.Destination,
				c.DeparturDate,
				c.DeparturTime,
				c.IsCanceled,
				c.CancelDateInText,
				c.CancelReason,
				c.IsPaidByApplicant,
				c.IsPaidByFinancialDepartment,
				c.Comment,
				c.CreatedByName,
				c.CreatedDateInText
			});
			return Ok(result);
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> TicketNew(long ticketHotelRequestId, CancellationToken cn)
		{
			var parentErr = await EnsureParentAccessAsync(ticketHotelRequestId, cn);
			if (parentErr != null) return parentErr;
			return View(@"\Views\Panel\Srv\TicketHotelRequest\_TicketForm.cshtml",
				new TicketHotelRequestTicket { TicketHotelRequestId = ticketHotelRequestId });
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> TicketEdit(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<TicketHotelRequestTicket>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return BadRequest("بلیط یافت نشد");
			var parentErr = await EnsureParentAccessAsync(entity.TicketHotelRequestId, cn);
			if (parentErr != null) return parentErr;
			return View(@"\Views\Panel\Srv\TicketHotelRequest\_TicketForm.cshtml", entity);
		}

		[HttpPost("[action]")]
		public async Task<IActionResult> SaveTicket(TicketHotelRequestTicket model, CancellationToken cn)
		{
			if (!CanCrud) return BadRequest("دسترسی ذخیره وجود ندارد");
			ClearTicketNav(model);
			var err = ValidateTicket(model);
			if (err != null) return BadRequest(err);

			var parentErr = await EnsureParentAccessAsync(model.TicketHotelRequestId, cn);
			if (parentErr != null) return parentErr;

			if (model.Id == null || model.Id == 0)
			{
				model.CreatedDateInText = ToPersianDateTimeShort(DateTime.Now);
				var saved = await unitOfWork.Repository<TicketHotelRequestTicket>().SaveAsync(model, cn, true);
				return Ok(saved);
			}

			var old = await unitOfWork.Repository<TicketHotelRequestTicket>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == model.Id, cn);
			if (old == null) return BadRequest("بلیط یافت نشد");
			model.CreatedDateInText = old.CreatedDateInText;
			model.IsCanceled = old.IsCanceled;
			model.CancelDateInText = old.CancelDateInText;
			model.CancelReason = old.CancelReason;
			model.IsPaidByApplicant = old.IsPaidByApplicant;
			model.IsPaidByFinancialDepartment = old.IsPaidByFinancialDepartment;
			model.HtsId = old.HtsId;
			return Ok(await unitOfWork.Repository<TicketHotelRequestTicket>().UpdateAsync(model, cn, true));
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> DeleteTicket(long id, CancellationToken cn)
		{
			if (!CanCrud) return BadRequest("دسترسی حذف وجود ندارد");
			var entity = await unitOfWork.Repository<TicketHotelRequestTicket>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return Ok();
			var parentErr = await EnsureParentAccessAsync(entity.TicketHotelRequestId, cn);
			if (parentErr != null) return parentErr;
			await unitOfWork.Repository<TicketHotelRequestTicket>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> CancelTicketForm(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<TicketHotelRequestTicket>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return BadRequest("بلیط یافت نشد");
			var parentErr = await EnsureParentAccessAsync(entity.TicketHotelRequestId, cn);
			if (parentErr != null) return parentErr;
			if (entity.IsCanceled == true)
				return BadRequest("این بلیط قبلاً کنسل شده است");
			return View(@"\Views\Panel\Srv\TicketHotelRequest\_CancelTicketForm.cshtml", entity);
		}

		[HttpPost("[action]")]
		public async Task<IActionResult> CancelTicket(CancelTicketRequest model, CancellationToken cn)
		{
			if (!CanCrud) return BadRequest("دسترسی ذخیره وجود ندارد");
			if (model == null || model.Id <= 0) return BadRequest("شناسه بلیط نامعتبر است");
			var entity = await unitOfWork.Repository<TicketHotelRequestTicket>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == model.Id, cn);
			if (entity == null) return BadRequest("بلیط یافت نشد");
			var parentErr = await EnsureParentAccessAsync(entity.TicketHotelRequestId, cn);
			if (parentErr != null) return parentErr;
			if (entity.IsCanceled == true)
				return BadRequest("این بلیط قبلاً کنسل شده است");
			if (string.IsNullOrWhiteSpace(model.CancelReason))
				return BadRequest("علت کنسل اجباری است");
			if (model.PaySideId != (int)PaySideEnum.Applicant && model.PaySideId != (int)PaySideEnum.FinancialDepartment)
				return BadRequest("طرف پرداخت اجباری است");

			var now = DateTime.Now;
			entity.IsCanceled = true;
			entity.CancelDateInText = ToPersianDateTimeShort(now);
			entity.CancelReason = model.CancelReason.Trim();
			if (model.PaySideId == (int)PaySideEnum.FinancialDepartment)
			{
				entity.IsPaidByFinancialDepartment = true;
				entity.IsPaidByApplicant = false;
			}
			else
			{
				entity.IsPaidByApplicant = true;
				entity.IsPaidByFinancialDepartment = false;
			}

			await unitOfWork.Repository<TicketHotelRequestTicket>().UpdateAsync(entity, cn, true);

			var parent = await unitOfWork.Repository<TicketHotelRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == entity.TicketHotelRequestId, cn);
			if (parent != null)
				await SendCancelEmailAsync(parent, entity, now, cn);

			return Ok(entity);
		}

		public class CancelTicketRequest
		{
			public long Id { get; set; }
			public int PaySideId { get; set; }
			public string? CancelReason { get; set; }
		}

		#endregion

		#region Hotel helpers (no ActionDisplayName)

		[HttpGet("[action]")]
		public async Task<IActionResult> GetHotels(long ticketHotelRequestId, CancellationToken cn)
		{
			var parent = await unitOfWork.Repository<TicketHotelRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == ticketHotelRequestId, cn);
			if (parent == null) return BadRequest("درخواست یافت نشد");
			if (!CanAccessRow(parent)) return BadRequest("دسترسی به این رکورد وجود ندارد");

			var items = await unitOfWork.Repository<TicketHotelRequestHotel>().TableNoTracking
				.Where(c => c.TicketHotelRequestId == ticketHotelRequestId)
				.OrderBy(c => c.Id)
				.Select(c => new
				{
					c.Id,
					c.HotelTitle,
					c.StarsNumber,
					c.HotelAmount,
					c.Comment,
					c.CreatedByName,
					c.CreatedDateInText
				})
				.ToListAsync(cn);
			return Ok(items);
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> HotelNew(long ticketHotelRequestId, CancellationToken cn)
		{
			var parentErr = await EnsureParentAccessAsync(ticketHotelRequestId, cn);
			if (parentErr != null) return parentErr;
			return View(@"\Views\Panel\Srv\TicketHotelRequest\_HotelForm.cshtml",
				new TicketHotelRequestHotel { TicketHotelRequestId = ticketHotelRequestId });
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> HotelEdit(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<TicketHotelRequestHotel>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return BadRequest("هتل یافت نشد");
			var parentErr = await EnsureParentAccessAsync(entity.TicketHotelRequestId, cn);
			if (parentErr != null) return parentErr;
			return View(@"\Views\Panel\Srv\TicketHotelRequest\_HotelForm.cshtml", entity);
		}

		[HttpPost("[action]")]
		public async Task<IActionResult> SaveHotel(TicketHotelRequestHotel model, CancellationToken cn)
		{
			if (!CanCrud) return BadRequest("دسترسی ذخیره وجود ندارد");
			ClearHotelNav(model);
			var err = ValidateHotel(model);
			if (err != null) return BadRequest(err);

			var parentErr = await EnsureParentAccessAsync(model.TicketHotelRequestId, cn);
			if (parentErr != null) return parentErr;

			if (model.Id == null || model.Id == 0)
			{
				model.CreatedDateInText = ToPersianDateTimeShort(DateTime.Now);
				return Ok(await unitOfWork.Repository<TicketHotelRequestHotel>().SaveAsync(model, cn, true));
			}

			var old = await unitOfWork.Repository<TicketHotelRequestHotel>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == model.Id, cn);
			if (old == null) return BadRequest("هتل یافت نشد");
			model.CreatedDateInText = old.CreatedDateInText;
			model.HtsId = old.HtsId;
			return Ok(await unitOfWork.Repository<TicketHotelRequestHotel>().UpdateAsync(model, cn, true));
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> DeleteHotel(long id, CancellationToken cn)
		{
			if (!CanCrud) return BadRequest("دسترسی حذف وجود ندارد");
			var entity = await unitOfWork.Repository<TicketHotelRequestHotel>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return Ok();
			var parentErr = await EnsureParentAccessAsync(entity.TicketHotelRequestId, cn);
			if (parentErr != null) return parentErr;
			await unitOfWork.Repository<TicketHotelRequestHotel>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		#endregion

		#region Private

		private bool CanAccessRow(TicketHotelRequest entity)
		{
			if (CanShowAllRows) return true;
			if (CurrentUserHasAnyRole(RoleApprover))
				return entity.DepartmentId == CurrentOrganizationUnitId;
			return entity.CreatedById == CurrentUserId;
		}

		private async Task<IActionResult?> EnsureParentAccessAsync(long parentId, CancellationToken cn)
		{
			if (!CanCrud) return BadRequest("دسترسی وجود ندارد");
			var parent = await unitOfWork.Repository<TicketHotelRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == parentId, cn);
			if (parent == null) return BadRequest("درخواست یافت نشد");
			if (!CanAccessRow(parent)) return BadRequest("دسترسی به این رکورد وجود ندارد");
			return null;
		}

		private static void ClearNav(TicketHotelRequest model)
		{
			model.CostCenter = null;
			model.Department = null;
			model.ManCompany = null;
			model.Project = null;
			model.ProjectDl = null;
			model.Approver = null;
			model.Tickets = null;
			model.Hotels = null;
		}

		private static void ClearTicketNav(TicketHotelRequestTicket model)
		{
			model.TicketHotelRequest = null;
		}

		private static void ClearHotelNav(TicketHotelRequestHotel model)
		{
			model.TicketHotelRequest = null;
		}

		private static string? ValidateHeader(TicketHotelRequest model)
		{
			if ((int)model.RequestTypeId == 0) return "نوع درخواست اجباری است";
			if ((int)model.ServiceTypeId == 0) return "نوع خدمات اجباری است";
			if ((int)model.RequestReasonId == 0) return "علت درخواست اجباری است";
			if (model.ManCompanyId is null or 0) return "شرکت اجباری است";
			if (model.ProjectDlId is null or 0) return "پروژه اجباری است";
			if ((int)model.RequestStatusId == 0) return "وضعیت درخواست اجباری است";
			if (model.CostCenterId is null or 0) return "مرکز هزینه اجباری است";
			if (!model.DepartureSource.HasValue()) return "مبدا رفت اجباری است";
			if (!model.DepartureDestination.HasValue()) return "مقصد رفت اجباری است";
			return null;
		}

		private static string? ValidateTicket(TicketHotelRequestTicket model)
		{
			if (model.TicketHotelRequestId <= 0) return "شناسه درخواست خالی است";
			if ((int)model.TicketTypeId == 0) return "نوع بلیط اجباری است";
			if (!model.IssueDate.HasValue()) return "تاریخ صدور اجباری است";
			if (!model.TicketNumber.HasValue()) return "شماره بلیط اجباری است";
			if (model.TicketAmount <= 0) return "مبلغ بلیط اجباری است";
			if (!model.Source.HasValue()) return "مبدا اجباری است";
			if (!model.Destination.HasValue()) return "مقصد اجباری است";
			if (!model.DeparturDate.HasValue()) return "تاریخ حرکت اجباری است";
			if (!model.DeparturTime.HasValue()) return "ساعت حرکت اجباری است";
			return null;
		}

		private static string? ValidateHotel(TicketHotelRequestHotel model)
		{
			if (model.TicketHotelRequestId <= 0) return "شناسه درخواست خالی است";
			if (!model.HotelTitle.HasValue()) return "نام هتل اجباری است";
			if (model.HotelAmount <= 0) return "مبلغ هتل اجباری است";
			if (model.StarsNumber is < 1 or > 5) return "تعداد ستاره باید بین ۱ تا ۵ باشد";
			return null;
		}

		private async Task ApplyApplicantsAsync(TicketHotelRequest model, CancellationToken cn)
		{
			var ids = ParseIdList(model.ApplicantIds);
			if (ids.Count == 0)
			{
				model.ApplicantIds = null;
				if (!model.ApplicantsInText.HasValue())
					model.ApplicantsInText = null;
				return;
			}

			model.ApplicantIds = string.Join(",", ids);
			var names = await unitOfWork.Repository<Personel>().TableNoTracking
				.Where(p => p.Id != null && ids.Contains(p.Id.Value))
				.Select(p => ((p.Name ?? "") + " " + (p.FamilyDisplay ?? p.Family ?? "")).Trim())
				.ToListAsync(cn);
			model.ApplicantsInText = string.Join("، ", names.Where(n => n.HasValue()));
		}

		private static List<long> ParseIdList(string? csv)
		{
			if (string.IsNullOrWhiteSpace(csv)) return [];
			return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Select(s => long.TryParse(s, out var id) ? id : 0)
				.Where(id => id > 0)
				.Distinct()
				.ToList();
		}

		private async Task<TicketHotelRequest?> LoadForEditAsync(long id, CancellationToken cn)
		{
			return await unitOfWork.Repository<TicketHotelRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
		}

		private static string ToPersianDateTimeShort(DateTime now)
		{
			// MaxLength(16): yyyy/MM/dd HH:mm
			return $"{now.ToShamsiDate()} {now.Hour:D2}:{now.Minute:D2}";
		}

		private async Task<string?> GetCurrentPersonelEmailAsync(CancellationToken cn)
		{
			if (CurrentUserId is null or 0) return null;
			var partyId = await userService.TableNoTracking
				.Where(u => u.Id == CurrentUserId)
				.Select(u => u.PartyId)
				.FirstOrDefaultAsync(cn);
			if (partyId is null or 0)
				return CurrentUserEmail;

			var email = await unitOfWork.Repository<Personel>().TableNoTracking
				.Where(p => p.PartyId == partyId)
				.Select(p => p.Email)
				.FirstOrDefaultAsync(cn);
			return email.HasValue() ? email : CurrentUserEmail;
		}

		private async Task SendAddEmailAsync(TicketHotelRequest entity, CancellationToken cn)
		{
			try
			{
				var receiver = await GetCurrentPersonelEmailAsync(cn);
				if (!receiver.HasValue()) return;

				var costCenterTitle = "";
				if (entity.CostCenterId is > 0)
				{
					costCenterTitle = await unitOfWork.Repository<CostCenter>().TableNoTracking
						.Where(c => c.Id == entity.CostCenterId)
						.Select(c => c.Title)
						.FirstOrDefaultAsync(cn) ?? "";
				}

				var projectTitle = "";
				if (entity.ProjectDlId is > 0)
				{
					projectTitle = await unitOfWork.Repository<DL>().TableNoTracking
						.Where(c => c.Id == entity.ProjectDlId)
						.Select(c => c.Title)
						.FirstOrDefaultAsync(cn) ?? "";
				}

				var body = $@"<html>
<body>
<div style='padding:25px 200px; display: block;font-family: B Zar;'>
<table style='width: 100%; direction: rtl;background-color: rgb(247, 247, 247); '>
<tr>
<td colspan='4' style='text-align: center;background-color: aquamarine;font-weight: bold;'>
مشخصات متقاضی بلیط<br>
گروه صنعتی هوایار
</td>
</tr>
<tr style='height: 50px;'>
<td style='width: 150px;'>نام و نام خانوادگی</td>
<td style='font-weight: bold;' colspan='3'>{HtmlEncode(entity.ApplicantsInText)}</td>
</tr>
<tr style='height: 50px;'>
<td>مبداء(رفت)</td>
<td style='font-weight: bold;'>{HtmlEncode(entity.DepartureSource)}</td>
<td>مقصد (رفت)</td>
<td style='font-weight: bold;'>{HtmlEncode(entity.DepartureDestination)}</td>
</tr>
<tr style='height: 50px;'>
<td>تاریخ (رفت)</td>
<td style='font-weight: bold;'>{HtmlEncode(entity.DepartureDate)}</td>
<td>ساعت (رفت)</td>
<td style='font-weight: bold;'>{HtmlEncode(entity.DepartureTime)}</td>
</tr>
<tr style='height: 50px;'>
<td>مبداء(بازگشت)</td>
<td style='font-weight: bold;'>{HtmlEncode(entity.ReturnSource)}</td>
<td>مقصد (بازگشت)</td>
<td style='font-weight: bold;'>{HtmlEncode(entity.ReturnDestination)}</td>
</tr>
<tr style='height: 50px;'>
<td>تاریخ (بازگشت)</td>
<td style='font-weight: bold;'>{HtmlEncode(entity.ReturnDate)}</td>
<td>ساعت (بازگشت)</td>
<td style='font-weight: bold;'>{HtmlEncode(entity.ReturnTime)}</td>
</tr>
<tr style='height: 50px;'>
<td>مرکز هزینه</td>
<td style='font-weight: bold;'>{HtmlEncode(costCenterTitle)}</td>
<td>کد پروژه</td>
<td style='font-weight: bold;'>{HtmlEncode(projectTitle)}</td>
</tr>
<tr>
<td colspan='4'>توضیحات:<br>{HtmlEncode(entity.Comment)}</td>
</tr>
</table>
</div>
</body>
</html>";

				await unitOfWork.Repository<Notification>().AddAsync(new Notification
				{
					Type = NotificationType.Email,
					Title = "جزئیات بلیط",
					Body = body,
					EntityId = entity.Id,
					OwnerId = CurrentUserId ?? 0,
					ViewPath = $"/Panel/Srv/TicketHotelRequest/Edit?id={entity.Id}",
					IsRead = false,
					IsSend = false,
					ToEmails = [receiver!]
				}, cn);
				await unitOfWork.SaveChangesAsync(cn);
			}
			catch
			{
				// ایمیل نباید ذخیره را متوقف کند
			}
		}

		private async Task SendCancelEmailAsync(TicketHotelRequest parent, TicketHotelRequestTicket ticket, DateTime now, CancellationToken cn)
		{
			try
			{
				var receiver = await GetCurrentPersonelEmailAsync(cn);
				if (!receiver.HasValue()) return;

				var orgTitle = "";
				if (CurrentOrganizationUnitId is > 0)
				{
					orgTitle = await unitOfWork.Repository<OrgUnit>().TableNoTracking
						.Where(c => c.Id == CurrentOrganizationUnitId)
						.Select(c => c.Title)
						.FirstOrDefaultAsync(cn) ?? "";
				}

				var cancelDt = ToPersianDateTimeShort(now);
				var body = $@"<html>
<body>
<div style='padding:25px 200px; display: block;font-family: B Zar;'>
<table style='width: 100%; direction: rtl;background-color: rgb(247, 247, 247); '>
<tr>
<td colspan='4' style='text-align: center;background-color: #ff7474;font-weight: bold;'>
کنسلی بلیط<br>
گروه صنعتی هوایار
</td>
</tr>
<tr style='height: 50px;'>
<td style='width: 150px;'>نام و نام خانوادگی</td>
<td style='font-weight: bold;'>{HtmlEncode(parent.ApplicantsInText)}</td>
<td style='width: 150px;'>واحد سازمانی</td>
<td style='font-weight: bold;'>{HtmlEncode(orgTitle)}</td>
</tr>
<tr style='height: 50px;'>
<td>مبداء(رفت)</td>
<td style='font-weight: bold;'>{HtmlEncode(ticket.Source)}</td>
<td>مقصد (رفت)</td>
<td style='font-weight: bold;'>{HtmlEncode(ticket.Destination)}</td>
</tr>
<tr style='height: 50px;'>
<td>تاریخ (رفت)</td>
<td style='font-weight: bold;'>{HtmlEncode(ticket.DeparturDate)}</td>
<td>ساعت (رفت)</td>
<td style='font-weight: bold;'>{HtmlEncode(ticket.DeparturTime)}</td>
</tr>
<tr style='height: 50px;'>
<td>شماره بلیط</td>
<td style='font-weight: bold;'>{HtmlEncode(ticket.TicketNumber)}</td>
<td>روز وساعت كنسل شدن بليط</td>
<td style='font-weight: bold;'>{HtmlEncode(cancelDt)}</td>
</tr>
<tr style='height: 50px;'>
<td>نام آژانس</td>
<td style='font-weight: bold;'>{HtmlEncode(ticket.TravelAgency)}</td>
<td>نام شرکت هواپیمایی</td>
<td style='font-weight: bold;'>{HtmlEncode(ticket.Airline)}</td>
</tr>
<tr>
<td style='font-weight: bold;'>علت كنسلي  پرواز:<br>{HtmlEncode(ticket.CancelReason)}</td>
<td style='font-weight: bold;'>کامنت:<br>{HtmlEncode(ticket.Comment)}</td>
</tr>
</table>
</div>
</body>
</html>";

				await unitOfWork.Repository<Notification>().AddAsync(new Notification
				{
					Type = NotificationType.Email,
					Title = "جزئیات بلیط",
					Body = body,
					EntityId = parent.Id,
					OwnerId = CurrentUserId ?? 0,
					ViewPath = $"/Panel/Srv/TicketHotelRequest/Edit?id={parent.Id}",
					IsRead = false,
					IsSend = false,
					ToEmails = [receiver!]
				}, cn);
				await unitOfWork.SaveChangesAsync(cn);
			}
			catch
			{
				// ایمیل نباید کنسل را متوقف کند
			}
		}

		private static string HtmlEncode(string? value)
			=> System.Net.WebUtility.HtmlEncode(value ?? "");

		#endregion
	}
}
