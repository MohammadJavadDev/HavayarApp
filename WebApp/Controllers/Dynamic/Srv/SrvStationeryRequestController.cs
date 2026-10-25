using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Entities.App.Hcm;
using Entities.App.Srv;
using Entities.App.Srv.Enums;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Auth;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Srv/StationeryRequest")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("درخواست لوازم التحریر", typeof(StationeryRequest))]
	public class SrvStationeryRequestController(
		IUnitOfWork unitOfWork,
		IWebHostEnvironment env,
		IUserService userService) : BaseController
	{
		private const string RoleSupport = "Srv.StationeryRequest.Support";
		private const string RoleRequesterConfirm = "Srv.StationeryRequest.RequesterConfirm";
		private const string RoleHavayarControl = "Srv.StationeryRequest.HavayarControl";
		private const string RoleRequester = "Srv.StationeryRequest.Requester";
		private const string RoleView = "Srv.StationeryRequest.View";
		private const string RoleShowAllMenus = "ShowAllMenus";

		private const short StatusUnderReview = 1032;
		private const short StatusApproved = 1033;
		private const short StatusRejected = 1034;
		private const short StatusDone = 1047;

		private const int MaxAttachments = 3;
		private const long MaxAttachmentBytes = 2048L * 1024;

		private bool CanShowAllRows =>
			IsAdministrator || CurrentUserHasAnyRole(RoleShowAllMenus);

		private bool CanViewPage =>
			CanShowAllRows
			|| CurrentUserHasAnyRole(RoleSupport, RoleRequesterConfirm, RoleHavayarControl, RoleRequester, RoleView);

		/// <summary>
		/// HTS صفحه 400: جدید/ویرایش/حذف برای 257، 408 و FullAccess گروه‌های 258 و 272.
		/// مشاهده (350) و هوایار کنترل خالص (413) این دسترسی را ندارند.
		/// </summary>
		private bool CanCrud =>
			IsAdministrator
			|| CurrentUserHasAnyRole(RoleSupport, RoleRequesterConfirm, RoleRequester, RoleShowAllMenus);

		private bool CanConfirmAction =>
			IsAdministrator
			|| CurrentUserHasAnyRole(RoleSupport, RoleRequesterConfirm, RoleHavayarControl, RoleShowAllMenus);

		private bool CanAttachments =>
			IsAdministrator
			|| CurrentUserHasAnyRole(RoleSupport, RoleRequesterConfirm, RoleRequester, RoleShowAllMenus);

		#region Primary CRUD

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(StationeryRequest model, CancellationToken cn)
		{
			if (!CanCrud) return BadRequest("دسترسی ذخیره وجود ندارد");
			ClearNav(model);
			var err = ValidateHeader(model);
			if (err != null) return BadRequest(err);

			if (model.Id == null || model.Id == 0)
				return await Add(model, cn);
			if (await unitOfWork.Repository<StationeryRequest>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(StationeryRequest model, CancellationToken cn)
		{
			if (!CanCrud) return BadRequest("دسترسی ذخیره وجود ندارد");
			ClearNav(model);
			var err = ValidateHeader(model);
			if (err != null) return BadRequest(err);

			model.HtsId = 0;
			model.StatusId = StatusUnderReview;
			model.CreatedDateInText = ToPersianDateTimeShort(DateTime.Now);
			model.LastComment = null;
			model.DoneDateTimeInText = null;
			model.Comments = null;
			model.Attachments = null;

			var saved = await unitOfWork.Repository<StationeryRequest>().SaveAsync(model, cn, true);
			return Ok(await LoadForEditAsync(saved.Id!.Value, cn));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(StationeryRequest model, CancellationToken cn)
		{
			if (!CanCrud) return BadRequest("دسترسی ذخیره وجود ندارد");
			ClearNav(model);
			var old = await unitOfWork.Repository<StationeryRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == model.Id, cn);
			if (old == null) return BadRequest("رکورد یافت نشد");
			if (!await CanAccessRowAsync(old, cn)) return BadRequest("دسترسی به این رکورد وجود ندارد");

			var err = ValidateHeader(model);
			if (err != null) return BadRequest(err);

			model.CreatedById = old.CreatedById;
			model.CreatedByName = old.CreatedByName;
			model.CreatedOnMiladiDateTime = old.CreatedOnMiladiDateTime;
			model.CreatedOnShamsiDateTime = old.CreatedOnShamsiDateTime;
			model.CreatedDateInText = old.CreatedDateInText;
			model.HtsId = old.HtsId;
			model.StatusId = old.StatusId;
			model.LastComment = old.LastComment;
			model.DoneDateTimeInText = old.DoneDateTimeInText;
			model.Comments = null;
			model.Attachments = null;

			await unitOfWork.Repository<StationeryRequest>().UpdateAsync(model, cn, true);
			return Ok(await LoadForEditAsync(model.Id!.Value, cn));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			if (!CanCrud) return BadRequest("دسترسی حذف وجود ندارد");
			var entity = await unitOfWork.Repository<StationeryRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return Ok();
			if (!await CanAccessRowAsync(entity, cn)) return BadRequest("دسترسی به این رکورد وجود ندارد");

			var hasComment = await unitOfWork.Repository<StationeryRequestComment>().TableNoTracking
				.AnyAsync(c => c.StationeryRequestId == id, cn);
			var hasAttachment = await unitOfWork.Repository<StationeryRequestAttachment>().TableNoTracking
				.AnyAsync(c => c.StationeryRequestId == id, cn);
			if (hasComment || hasAttachment)
				return BadRequest("این درخواست دارای کامنت یا پیوست است و قابل حذف نیست");

			await unitOfWork.Repository<StationeryRequest>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public async Task<IActionResult> Edit(long? id, CancellationToken cn)
		{
			if (!CanCrud) return BadRequest("دسترسی ویرایش وجود ندارد");
			StationeryRequest entity;
			if (id != null && id != 0)
			{
				entity = await LoadForEditAsync(id.Value, cn) ?? new StationeryRequest();
				if (entity.Id is > 0 && !await CanAccessRowAsync(entity, cn))
					return BadRequest("دسترسی به این رکورد وجود ندارد");
			}
			else
			{
				entity = new StationeryRequest();
			}

			return View(@"\Views\Panel\Srv\StationeryRequest\Edit.cshtml", entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			if (!CanCrud) return BadRequest("دسترسی درج وجود ندارد");
			return View(@"\Views\Panel\Srv\StationeryRequest\Edit.cshtml", new StationeryRequest());
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			if (!CanViewPage) return BadRequest("دسترسی مشاهده وجود ندارد");
			return View(@"\Views\Panel\Srv\StationeryRequest\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<StationeryRequest>().FetchDataAsync(request, cn));

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + @"\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<StationeryRequest>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message);
			}
		}

		[HttpPost("[action]")]
		[ActionDisplayName("بررسی و تایید", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> Confirm(ConfirmStationeryRequestRequest model, CancellationToken cn)
		{
			if (!CanConfirmAction) return BadRequest("دسترسی بررسی و تایید وجود ندارد");
			if (model == null || model.Id <= 0) return BadRequest("شناسه درخواست نامعتبر است");
			if (!model.Comment.HasValue()) return BadRequest("توضیحات اجباری است");

			var entity = await unitOfWork.Repository<StationeryRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == model.Id, cn);
			if (entity == null) return BadRequest("رکورد یافت نشد");
			if (!await CanAccessRowAsync(entity, cn)) return BadRequest("دسترسی به این رکورد وجود ندارد");

			var nowText = ToPersianDateTimeShort(DateTime.Now);
			var comment = new StationeryRequestComment
			{
				HtsId = 0,
				StationeryRequestId = entity.Id!.Value,
				Comment = model.Comment!.Trim(),
				StatusId = model.StatusId,
				CreatedDateInText = nowText
			};
			await unitOfWork.Repository<StationeryRequestComment>().SaveAsync(comment, cn, true);

			if (model.StatusId.HasValue)
				entity.StatusId = model.StatusId.Value;

			entity.LastComment = model.Comment.Trim();
			if (entity.StatusId == StatusDone)
				entity.DoneDateTimeInText = nowText;

			entity.Comments = null;
			entity.Attachments = null;

			await unitOfWork.Repository<StationeryRequest>().UpdateAsync(entity, cn, true);
			return Ok();
		}

		#endregion

		#region Attachments

		[HttpGet("[action]")]
		[ActionDisplayName("پیوست مدارک", ActionAccessType.View, ActionAccessItemType.Custom)]
		public async Task<IActionResult> Attachments(long id, CancellationToken cn)
		{
			if (!CanAttachments) return BadRequest("دسترسی پیوست وجود ندارد");
			var parent = await unitOfWork.Repository<StationeryRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (parent == null) return BadRequest("درخواست یافت نشد");
			if (!await CanAccessRowAsync(parent, cn)) return BadRequest("دسترسی به این رکورد وجود ندارد");

			ViewBag.ParentId = id;
			return View(@"\Views\Panel\Srv\StationeryRequest\Attachments.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره پیوست", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> SaveAttachment(SaveStationeryRequestAttachmentRequest request, CancellationToken cn)
		{
			if (!CanAttachments) return BadRequest("دسترسی پیوست وجود ندارد");
			if (request == null || request.StationeryRequestId <= 0)
				return BadRequest("شناسه درخواست نامعتبر است");
			if (!request.FileBase64.HasValue() && (request.FileContent == null || request.FileContent.Length == 0))
				return BadRequest("فایل اجباری است");

			var parent = await unitOfWork.Repository<StationeryRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == request.StationeryRequestId, cn);
			if (parent == null) return BadRequest("درخواست یافت نشد");
			if (!await CanAccessRowAsync(parent, cn)) return BadRequest("دسترسی به این رکورد وجود ندارد");

			var existingCount = await unitOfWork.Repository<StationeryRequestAttachment>().TableNoTracking
				.CountAsync(c => c.StationeryRequestId == request.StationeryRequestId, cn);
			if (existingCount >= MaxAttachments)
				return BadRequest("حداکثر ۳ فایل پیوست برای هر درخواست مجاز است");

			var bytes = request.FileContent;
			if ((bytes == null || bytes.Length == 0) && request.FileBase64.HasValue())
				bytes = DecodeBase64File(request.FileBase64!);
			if (bytes == null || bytes.Length == 0)
				return BadRequest("فایل اجباری است");
			if (bytes.LongLength > MaxAttachmentBytes)
				return BadRequest("حجم فایل نباید بیشتر از ۲ مگابایت باشد");

			var now = DateTime.Now;
			var entity = new StationeryRequestAttachment
			{
				HtsId = 0,
				StationeryRequestId = request.StationeryRequestId,
				FileName = request.FileName.HasValue() ? request.FileName!.Trim() : "file",
				FileContent = bytes,
				FileSize = bytes.LongLength,
				Comment = request.Comment.HasValue() ? request.Comment!.Trim() : "",
				CreatedDate = now.ToShamsiDate(),
				CreatedTime = $"{now.Hour:D2}:{now.Minute:D2}"
			};

			var saved = await unitOfWork.Repository<StationeryRequestAttachment>().SaveAsync(entity, cn, true);
			saved.FileContent = Array.Empty<byte>();
			return Ok(saved);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("حذف پیوست", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> DeleteAttachment(long id, CancellationToken cn)
		{
			if (!CanAttachments) return BadRequest("دسترسی حذف پیوست وجود ندارد");
			var entity = await unitOfWork.Repository<StationeryRequestAttachment>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return Ok();

			var parent = await unitOfWork.Repository<StationeryRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == entity.StationeryRequestId, cn);
			if (parent == null) return BadRequest("درخواست یافت نشد");
			if (!await CanAccessRowAsync(parent, cn)) return BadRequest("دسترسی به این رکورد وجود ندارد");

			await unitOfWork.Repository<StationeryRequestAttachment>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("مشاهده پیوست", ActionAccessType.View, ActionAccessItemType.Custom)]
		public async Task<IActionResult> ViewAttachment(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<StationeryRequestAttachment>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return NotFound();

			var parent = await unitOfWork.Repository<StationeryRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == entity.StationeryRequestId, cn);
			if (parent == null) return BadRequest("درخواست یافت نشد");
			if (!await CanAccessRowAsync(parent, cn)) return BadRequest("دسترسی به این رکورد وجود ندارد");

			if (entity.FileContent == null || entity.FileContent.Length == 0)
				return BadRequest("محتوای فایل خالی است");

			var fileName = entity.FileName.HasValue() ? entity.FileName! : "attachment";
			return File(entity.FileContent, GuessContentType(fileName), fileName);
		}

		#endregion

		#region Helpers (no ActionDisplayName)

		[HttpGet("[action]")]
		public async Task<IActionResult> Comments(long stationeryRequestId, CancellationToken cn)
		{
			var parent = await unitOfWork.Repository<StationeryRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == stationeryRequestId, cn);
			if (parent == null) return BadRequest("درخواست یافت نشد");
			if (!await CanAccessRowAsync(parent, cn)) return BadRequest("دسترسی به این رکورد وجود ندارد");

			var items = await unitOfWork.Repository<StationeryRequestComment>().TableNoTracking
				.Where(c => c.StationeryRequestId == stationeryRequestId)
				.OrderByDescending(c => c.Id)
				.Select(c => new
				{
					c.Id,
					c.StatusId,
					StatusTitle = c.StatusId == StatusUnderReview ? "در حال بررسی"
						: c.StatusId == StatusApproved ? "تاییده شده"
						: c.StatusId == StatusRejected ? "رد شده"
						: c.StatusId == StatusDone ? "انجام شد"
						: "",
					c.Comment,
					CreatedByName = c.CreatedByName,
					CreatedDateInText = c.CreatedDateInText
				})
				.ToListAsync(cn);

			return Ok(items);
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> GetAttachments(long stationeryRequestId, CancellationToken cn)
		{
			var parent = await unitOfWork.Repository<StationeryRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == stationeryRequestId, cn);
			if (parent == null) return BadRequest("درخواست یافت نشد");
			if (!await CanAccessRowAsync(parent, cn)) return BadRequest("دسترسی به این رکورد وجود ندارد");

			var items = await unitOfWork.Repository<StationeryRequestAttachment>().TableNoTracking
				.Where(c => c.StationeryRequestId == stationeryRequestId)
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

		public class ConfirmStationeryRequestRequest
		{
			public long Id { get; set; }
			public short? StatusId { get; set; }
			public string? Comment { get; set; }
		}

		public class SaveStationeryRequestAttachmentRequest
		{
			public long StationeryRequestId { get; set; }
			public string? Comment { get; set; }
			public string? FileName { get; set; }
			public string? FileBase64 { get; set; }
			public byte[]? FileContent { get; set; }
		}

		private async Task<bool> CanAccessRowAsync(StationeryRequest entity, CancellationToken cn)
		{
			if (CanShowAllRows) return true;

			if (CurrentUserHasAnyRole(RoleSupport))
			{
				var currentBranch = await ResolveCurrentBranchCodeAsync(cn);
				if (currentBranch == null) return false;
				var creatorBranch = await GetCreatorBranchCodeAsync(entity.CreatedById, cn);
				if (creatorBranch == null) return false;
				return creatorBranch == currentBranch
					|| (creatorBranch != 2 && currentBranch == 1);
			}

			return entity.CreatedById == CurrentUserId;
		}

		private async Task<int?> ResolveCurrentBranchCodeAsync(CancellationToken cn)
		{
			var personel = await GetCurrentPersonelAsync(cn);
			return personel?.BranchCode;
		}

		private async Task<int?> GetCreatorBranchCodeAsync(long? createdById, CancellationToken cn)
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

		private static void ClearNav(StationeryRequest model)
		{
			model.Comments = null;
			model.Attachments = null;
		}

		private static string? ValidateHeader(StationeryRequest model)
		{
			if ((int)model.StationeryTypeId == 0) return "نوع لوازم اجباری است";
			if (model.RequestDescription != null && model.RequestDescription.Length > 256)
				return "متن درخواست حداکثر ۲۵۶ نویسه است";
			return null;
		}

		private async Task<StationeryRequest?> LoadForEditAsync(long id, CancellationToken cn)
		{
			return await unitOfWork.Repository<StationeryRequest>().TableNoTracking
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
				".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
				".xls" => "application/vnd.ms-excel",
				".doc" => "application/msword",
				".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
				".zip" => "application/zip",
				_ => "application/octet-stream"
			};
		}

		#endregion
	}
}
