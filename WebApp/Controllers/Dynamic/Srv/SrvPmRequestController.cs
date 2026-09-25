using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Entities.App.Hcm;
using Entities.App.Hrm;
using Entities.App.Srv;
using Entities.App.Srv.Enums;
using Entities.Base;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Auth;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Srv/PmRequest")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("درخواست PM", typeof(PmRequest))]
	public class SrvPmRequestController(
		IUnitOfWork unitOfWork,
		IWebHostEnvironment env,
		IUserService userService) : BaseController
	{
		private const string RoleSupport = "Srv.PmRequest.Support";
		private const string RoleRequesterConfirm = "Srv.PmRequest.RequesterConfirm";
		private const string RoleHavayarControl = "Srv.PmRequest.HavayarControl";
		private const string RoleRequester = "Srv.PmRequest.Requester";
		private const string RoleView = "Srv.PmRequest.View";
		private const string RoleShowAllMenus = "ShowAllMenus";

		private const short StatusUnderReview = 1032;
		private const short StatusApproved = 1033;
		private const short StatusRejected = 1034;
		private const short StatusDone = 1047;

		private const int MaxAttachmentCount = 3;
		private const long MaxAttachmentBytes = 1024L * 1024L; // HTS getUploadedFileSize: fileSize > 1024 (KB)

		private bool CanShowAllRows =>
			IsAdministrator || CurrentUserHasAnyRole(RoleShowAllMenus);

		private bool CanCrud =>
			IsAdministrator
			|| CurrentUserHasAnyRole(RoleSupport, RoleRequesterConfirm, RoleRequester, RoleShowAllMenus);

		private bool CanConfirm =>
			IsAdministrator
			|| CurrentUserHasAnyRole(RoleSupport, RoleRequesterConfirm, RoleHavayarControl, RoleShowAllMenus);

		private bool CanAttachments =>
			IsAdministrator
			|| CurrentUserHasAnyRole(RoleSupport, RoleRequesterConfirm, RoleRequester, RoleShowAllMenus);

		private bool CanViewPage =>
			IsAdministrator
			|| CurrentUserHasAnyRole(RoleSupport, RoleRequesterConfirm, RoleHavayarControl, RoleRequester, RoleView, RoleShowAllMenus);

		#region Primary CRUD

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(PmRequest model, CancellationToken cn)
		{
			if (!CanCrud) return BadRequest("دسترسی ذخیره وجود ندارد");
			ClearNav(model);
			var err = ValidateHeader(model);
			if (err != null) return BadRequest(err);

			if (model.Id == null || model.Id == 0)
				return await Add(model, cn);
			if (await unitOfWork.Repository<PmRequest>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(PmRequest model, CancellationToken cn)
		{
			if (!CanCrud) return BadRequest("دسترسی ذخیره وجود ندارد");
			ClearNav(model);
			var err = ValidateHeader(model);
			if (err != null) return BadRequest(err);

			model.HtsId = 0;
			model.StatusId = StatusUnderReview;
			model.CreatedDateInText = ToPersianDateTimeShort(DateTime.Now);
			model.LastComment = null;
			model.ApproximateExecutionDate = null;
			model.DoneDateTimeInText = null;
			model.Comments = null;
			model.Attachments = null;

			var personel = await GetCurrentPersonelAsync(cn);
			model.OrganizationUnitId = personel?.OrgUnitId;
			if (model.OrganizationUnitId is > 0)
			{
				model.CreatedOrganizationUnitTitle = await unitOfWork.Repository<OrgUnit>().TableNoTracking
					.Where(o => o.Id == model.OrganizationUnitId)
					.Select(o => o.Title)
					.FirstOrDefaultAsync(cn);
			}
			else
			{
				model.CreatedOrganizationUnitTitle = null;
			}

			var saved = await unitOfWork.Repository<PmRequest>().SaveAsync(model, cn, true);

			// TODO: SMS + desktop notification (do NOT call any SMS API / sender here).
			// Old HTS Add (PmRequestService.SendNotification + controller double-send for central):
			// Message body:
			//   "درخواست PM\nنوع: {Type}\nمتن: {RequestText truncated 30}+(… if len>25)\nاز: {CreatedUser}"
			// Recipients:
			//   - Factory (creator BranchCode=2): mobiles of group-269 members with Personel.BranchCode=2,
			//     plus fixed numbers 09123670651;09191030853;09120218648
			//   - Central / other: 09352201099 once (controller also sent this for BranchCode=1 —
			//     old double-send bug; when implemented later send central number only once)
			// Desktop notification: OnlineUsersHub to user ids in group 269 only (not all Support / 258).
			// Audience for SMS TODO is group 269 only, not the whole Support role.

			return Ok(await LoadForEditAsync(saved.Id!.Value, cn));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(PmRequest model, CancellationToken cn)
		{
			if (!CanCrud) return BadRequest("دسترسی ذخیره وجود ندارد");
			ClearNav(model);
			var old = await unitOfWork.Repository<PmRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == model.Id, cn);
			if (old == null) return BadRequest("رکورد یافت نشد");
			if (!await CanAccessRowAsync(old, cn)) return BadRequest("دسترسی به این رکورد وجود ندارد");
			if (old.StatusId == StatusDone)
				return BadRequest("درخواست انجام‌شده قابل ویرایش نیست");

			var err = ValidateHeader(model);
			if (err != null) return BadRequest(err);

			// Only TypeId and RequestText change; preserve everything else.
			old.TypeId = model.TypeId;
			old.RequestText = model.RequestText;
			old.OrganizationUnit = null;
			old.Comments = null;
			old.Attachments = null;

			await unitOfWork.Repository<PmRequest>().UpdateAsync(old, cn, true);
			return Ok(await LoadForEditAsync(old.Id!.Value, cn));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			if (!CanCrud) return BadRequest("دسترسی حذف وجود ندارد");
			var entity = await unitOfWork.Repository<PmRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return Ok();
			if (!await CanAccessRowAsync(entity, cn)) return BadRequest("دسترسی به این رکورد وجود ندارد");
			if (entity.StatusId == StatusDone)
				return BadRequest("درخواست انجام‌شده قابل حذف نیست");

			var hasComment = await unitOfWork.Repository<PmRequestComment>().TableNoTracking
				.AnyAsync(c => c.PmRequestId == id, cn);
			var hasAttachment = await unitOfWork.Repository<PmRequestAttachment>().TableNoTracking
				.AnyAsync(c => c.PmRequestId == id, cn);
			if (hasComment || hasAttachment)
				return BadRequest("این درخواست دارای کامنت یا پیوست است و قابل حذف نیست");

			await unitOfWork.Repository<PmRequest>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public async Task<IActionResult> Edit(long? id, CancellationToken cn)
		{
			if (!CanCrud) return BadRequest("دسترسی ویرایش وجود ندارد");
			PmRequest entity;
			if (id != null && id != 0)
			{
				entity = await LoadForEditAsync(id.Value, cn) ?? new PmRequest();
				if (entity.Id is > 0)
				{
					if (!await CanAccessRowAsync(entity, cn))
						return BadRequest("دسترسی به این رکورد وجود ندارد");
					if (entity.StatusId == StatusDone)
						return BadRequest("درخواست انجام‌شده قابل ویرایش نیست");
				}
			}
			else
			{
				entity = new PmRequest();
			}

			return View(@"\Views\Panel\Srv\PmRequest\Edit.cshtml", entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			if (!CanCrud) return BadRequest("دسترسی درج وجود ندارد");
			return View(@"\Views\Panel\Srv\PmRequest\Edit.cshtml", new PmRequest());
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			if (!CanViewPage) return BadRequest("دسترسی مشاهده وجود ندارد");
			return View(@"\Views\Panel\Srv\PmRequest\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<PmRequest>().FetchDataAsync(request, cn));

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + @"\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<PmRequest>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats.officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message);
			}
		}

		[HttpPost("[action]")]
		[ActionDisplayName("بررسی و تایید", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> Confirm(ConfirmPmRequestRequest model, CancellationToken cn)
		{
			if (!CanConfirm) return BadRequest("دسترسی بررسی و تایید وجود ندارد");
			if (model == null || model.Id <= 0) return BadRequest("شناسه درخواست نامعتبر است");
			if (!model.Comment.HasValue()) return BadRequest("توضیحات اجباری است");

			var entity = await unitOfWork.Repository<PmRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == model.Id, cn);
			if (entity == null) return BadRequest("رکورد یافت نشد");
			if (!await CanAccessRowAsync(entity, cn)) return BadRequest("دسترسی به این رکورد وجود ندارد");

			var nowText = ToPersianDateTimeShort(DateTime.Now);
			var comment = new PmRequestComment
			{
				HtsId = 0,
				PmRequestId = entity.Id!.Value,
				Comment = model.Comment!.Trim(),
				StatusId = model.StatusId,
				ApproximateExecutionDate = model.ApproximateExecutionDate.HasValue()
					? model.ApproximateExecutionDate!.Trim()
					: null,
				CreatedDateInText = nowText
			};
			await unitOfWork.Repository<PmRequestComment>().SaveAsync(comment, cn, true);

			if (model.StatusId.HasValue)
				entity.StatusId = model.StatusId.Value;

			entity.LastComment = model.Comment.Trim();
			if (model.ApproximateExecutionDate.HasValue())
				entity.ApproximateExecutionDate = model.ApproximateExecutionDate!.Trim();
			if (entity.StatusId == StatusDone)
				entity.DoneDateTimeInText = nowText;

			entity.Comments = null;
			entity.Attachments = null;
			entity.OrganizationUnit = null;

			await unitOfWork.Repository<PmRequest>().UpdateAsync(entity, cn, true);
			return Ok();
		}

		#endregion

		#region Attachments

		[HttpGet("[action]")]
		[ActionDisplayName("پیوست مدارک", ActionAccessType.View, ActionAccessItemType.Custom)]
		public async Task<IActionResult> Attachments(long id, CancellationToken cn)
		{
			if (!CanAttachments) return BadRequest("دسترسی پیوست وجود ندارد");
			var parent = await unitOfWork.Repository<PmRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (parent == null) return BadRequest("درخواست یافت نشد");
			if (!await CanAccessRowAsync(parent, cn)) return BadRequest("دسترسی به این رکورد وجود ندارد");

			ViewBag.ParentId = id;
			return View(@"\Views\Panel\Srv\PmRequest\Attachments.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره پیوست", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> SaveAttachment(SavePmRequestAttachmentRequest request, CancellationToken cn)
		{
			if (!CanAttachments) return BadRequest("دسترسی پیوست وجود ندارد");
			if (request == null || request.PmRequestId <= 0)
				return BadRequest("شناسه درخواست نامعتبر است");
			if (!request.Comment.HasValue())
				return BadRequest("توضیحات پیوست اجباری است");
			if (!request.FileBase64.HasValue() && (request.FileContent == null || request.FileContent.Length == 0))
				return BadRequest("فایل اجباری است");

			var parent = await unitOfWork.Repository<PmRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == request.PmRequestId, cn);
			if (parent == null) return BadRequest("درخواست یافت نشد");
			if (!await CanAccessRowAsync(parent, cn)) return BadRequest("دسترسی به این رکورد وجود ندارد");

			var existingCount = await unitOfWork.Repository<PmRequestAttachment>().TableNoTracking
				.CountAsync(c => c.PmRequestId == request.PmRequestId, cn);
			if (existingCount >= MaxAttachmentCount)
				return BadRequest("حداکثر ۳ پیوست برای هر درخواست مجاز است");

			var bytes = request.FileContent;
			if ((bytes == null || bytes.Length == 0) && request.FileBase64.HasValue())
				bytes = DecodeBase64File(request.FileBase64!);
			if (bytes == null || bytes.Length == 0)
				return BadRequest("فایل اجباری است");
			if (bytes.LongLength > MaxAttachmentBytes)
				return BadRequest("حجم فایل بیش از حد مجاز است");

			var now = DateTime.Now;
			var entity = new PmRequestAttachment
			{
				HtsId = 0,
				PmRequestId = request.PmRequestId,
				FileName = request.FileName.HasValue() ? request.FileName!.Trim() : "file",
				FileContent = bytes,
				FileSize = bytes.LongLength,
				Comment = request.Comment!.Trim(),
				CreatedDate = now.ToShamsiDate(),
				CreatedTime = $"{now.Hour:D2}:{now.Minute:D2}"
			};

			var saved = await unitOfWork.Repository<PmRequestAttachment>().SaveAsync(entity, cn, true);
			saved.FileContent = Array.Empty<byte>();
			return Ok(saved);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("حذف پیوست", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> DeleteAttachment(long id, CancellationToken cn)
		{
			if (!CanAttachments) return BadRequest("دسترسی حذف پیوست وجود ندارد");
			var entity = await unitOfWork.Repository<PmRequestAttachment>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return Ok();

			var parent = await unitOfWork.Repository<PmRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == entity.PmRequestId, cn);
			if (parent == null) return BadRequest("درخواست یافت نشد");
			if (!await CanAccessRowAsync(parent, cn)) return BadRequest("دسترسی به این رکورد وجود ندارد");

			await unitOfWork.Repository<PmRequestAttachment>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("مشاهده پیوست", ActionAccessType.View, ActionAccessItemType.Custom)]
		public async Task<IActionResult> ViewAttachment(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<PmRequestAttachment>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return NotFound();

			var parent = await unitOfWork.Repository<PmRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == entity.PmRequestId, cn);
			if (parent == null) return BadRequest("درخواست یافت نشد");
			if (!await CanAccessRowAsync(parent, cn)) return BadRequest("دسترسی به این رکورد وجود ندارد");

			if (entity.FileContent == null || entity.FileContent.Length == 0)
				return BadRequest("محتوای فایل خالی است");

			var fileName = entity.FileName.HasValue() ? entity.FileName! : "attachment";
			var contentType = GuessContentType(fileName);
			return File(entity.FileContent, contentType, fileName);
		}

		#endregion

		#region Helpers (no ActionDisplayName)

		[HttpGet("[action]")]
		public async Task<IActionResult> Comments(long pmRequestId, CancellationToken cn)
		{
			var parent = await unitOfWork.Repository<PmRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == pmRequestId, cn);
			if (parent == null) return BadRequest("درخواست یافت نشد");
			if (!await CanAccessRowAsync(parent, cn)) return BadRequest("دسترسی به این رکورد وجود ندارد");

			var items = await unitOfWork.Repository<PmRequestComment>().TableNoTracking
				.Where(c => c.PmRequestId == pmRequestId)
				.OrderByDescending(c => c.Id)
				.Select(c => new
				{
					c.Id,
					c.StatusId,
					StatusTitle = c.StatusId == StatusUnderReview ? "در حال بررسی"
						: c.StatusId == StatusApproved ? "تاییده شده"
						: c.StatusId == StatusRejected ? "رد شده"
						: c.StatusId == StatusDone ? "انجام شد"
						: c.StatusId == 0 ? "0"
						: c.StatusId.HasValue ? c.StatusId.Value.ToString() : "",
					c.Comment,
					c.ApproximateExecutionDate,
					CreatedByName = c.CreatedByName,
					CreatedDateInText = c.CreatedDateInText
				})
				.ToListAsync(cn);

			return Ok(items);
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> GetAttachments(long pmRequestId, CancellationToken cn)
		{
			var parent = await unitOfWork.Repository<PmRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == pmRequestId, cn);
			if (parent == null) return BadRequest("درخواست یافت نشد");
			if (!await CanAccessRowAsync(parent, cn)) return BadRequest("دسترسی به این رکورد وجود ندارد");

			var items = await unitOfWork.Repository<PmRequestAttachment>().TableNoTracking
				.Where(c => c.PmRequestId == pmRequestId)
				.OrderByDescending(c => c.Id)
				.Select(c => new
				{
					c.Id,
					c.FileName,
					c.FileSize,
					c.Comment,
					c.CreatedDate,
					c.CreatedTime,
					c.CreatedByName
				})
				.ToListAsync(cn);
			return Ok(items);
		}

		#endregion

		#region Private

		public class ConfirmPmRequestRequest
		{
			public long Id { get; set; }
			public short? StatusId { get; set; }
			public string? ApproximateExecutionDate { get; set; }
			public string? Comment { get; set; }
		}

		public class SavePmRequestAttachmentRequest
		{
			public long PmRequestId { get; set; }
			public string? Comment { get; set; }
			public string? FileName { get; set; }
			public string? FileBase64 { get; set; }
			public byte[]? FileContent { get; set; }
		}

		private async Task<bool> CanAccessRowAsync(PmRequest entity, CancellationToken cn)
		{
			if (CanShowAllRows) return true;

			var currentPersonel = await GetCurrentPersonelAsync(cn);
			if (currentPersonel?.BranchCode == null) return false;
			var currentBranch = currentPersonel.BranchCode.Value;

			if (CurrentUserHasAnyRole(RoleSupport))
			{
				var creatorBranch = await ResolveCreatorBranchCodeAsync(entity.CreatedById, cn);
				if (creatorBranch == null) return false;
				return creatorBranch.Value == currentBranch
					|| (creatorBranch.Value != 2 && currentBranch == 1);
			}

			if (entity.CreatedById == CurrentUserId)
				return true;

			if (entity.OrganizationUnitId is > 0
				&& currentPersonel.OrgUnitId is > 0
				&& entity.OrganizationUnitId == currentPersonel.OrgUnitId)
			{
				var creatorBranch = await ResolveCreatorBranchCodeAsync(entity.CreatedById, cn);
				return creatorBranch.HasValue && creatorBranch.Value == currentBranch;
			}

			return false;
		}

		private async Task<int?> ResolveCreatorBranchCodeAsync(long? createdById, CancellationToken cn)
		{
			if (createdById is null or 0) return null;
			var partyId = await userService.TableNoTracking
				.Where(u => u.Id == createdById)
				.Select(u => u.PartyId)
				.FirstOrDefaultAsync(cn);
			if (partyId is null or 0) return null;
			return await unitOfWork.Repository<Personel>().TableNoTracking
				.Where(p => p.PartyId == partyId)
				.Select(p => p.BranchCode)
				.FirstOrDefaultAsync(cn);
		}

		private async Task<Personel?> GetCurrentPersonelAsync(CancellationToken cn)
		{
			if (CurrentUserId is null or 0) return null;
			var partyId = await userService.TableNoTracking
				.Where(u => u.Id == CurrentUserId)
				.Select(u => u.PartyId)
				.FirstOrDefaultAsync(cn);
			if (partyId is null or 0) return null;
			return await unitOfWork.Repository<Personel>().TableNoTracking
				.FirstOrDefaultAsync(p => p.PartyId == partyId, cn);
		}

		private static void ClearNav(PmRequest model)
		{
			model.OrganizationUnit = null;
			model.Comments = null;
			model.Attachments = null;
		}

		private static string? ValidateHeader(PmRequest model)
		{
			if ((int)model.TypeId == 0) return "نوع اجباری است";
			return null;
		}

		private async Task<PmRequest?> LoadForEditAsync(long id, CancellationToken cn)
		{
			return await unitOfWork.Repository<PmRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
		}

		private static string ToPersianDateTimeShort(DateTime now)
			=> $"{now.ToShamsiDate()} {now.Hour:D2}:{now.Minute:D2}";

		private static byte[]? DecodeBase64File(string base64)
		{
			try
			{
				var raw = base64.Contains(',') ? base64[(base64.IndexOf(',') + 1)..] : base64;
				return Convert.FromBase64String(raw);
			}
			catch
			{
				return null;
			}
		}

		private static string GuessContentType(string fileName)
		{
			var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
			return ext switch
			{
				".pdf" => "application/pdf",
				".png" => "image/png",
				".jpg" or ".jpeg" => "image/jpeg",
				".gif" => "image/gif",
				".xlsx" => "application/vnd.openxmlformats.officedocument.spreadsheetml.sheet",
				".xls" => "application/vnd.ms-excel",
				".doc" => "application/msword",
				".docx" => "application/vnd.openxmlformats.officedocument.wordprocessingml.document",
				".zip" => "application/zip",
				_ => "application/octet-stream"
			};
		}

		#endregion
	}
}
