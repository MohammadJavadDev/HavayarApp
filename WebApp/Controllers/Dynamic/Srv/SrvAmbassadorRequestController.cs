using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Entities.App.Hcm;
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
	[Route("Panel/Srv/AmbassadorRequest")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("درخواست پیک", typeof(AmbassadorRequest))]
	public class SrvAmbassadorRequestController(
		IUnitOfWork unitOfWork,
		IWebHostEnvironment env,
		IUserService userService) : BaseController
	{
		private const string RoleSupport = "Srv.AmbassadorRequest.Support";
		private const string RoleRequesterConfirm = "Srv.AmbassadorRequest.RequesterConfirm";
		private const string RoleHavayarControl = "Srv.AmbassadorRequest.HavayarControl";
		private const string RoleRequester = "Srv.AmbassadorRequest.Requester";
		private const string RoleView = "Srv.AmbassadorRequest.View";
		private const string RoleShowAllMenus = "ShowAllMenus";

		private const short StatusUnderReview = 1032;
		private const short StatusApproved = 1033;
		private const short StatusRejected = 1034;
		private const short StatusDone = 1047;

		private const int MaxAttachmentCount = 3;
		private const long MaxAttachmentBytes = 1024L * 1024L; // HTS fileSize > 1024 (KB)

		private static readonly string[] DriverPersonelCodes =
		[
			"999999070", "99079", "91036", "88031", "92003", "99018"
		];

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
		public async Task<IActionResult> Save(AmbassadorRequest model, CancellationToken cn)
		{
			if (!CanCrud) return BadRequest("دسترسی ذخیره وجود ندارد");
			ClearNav(model);
			var err = ValidateHeader(model);
			if (err != null) return BadRequest(err);

			if (model.Id == null || model.Id == 0)
				return await Add(model, cn);
			if (await unitOfWork.Repository<AmbassadorRequest>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(AmbassadorRequest model, CancellationToken cn)
		{
			if (!CanCrud) return BadRequest("دسترسی ذخیره وجود ندارد");
			ClearNav(model);
			var err = ValidateHeader(model);
			if (err != null) return BadRequest(err);

			ApplyNeedDateFromText(model);

			model.HtsId = 0;
			model.StatusId = StatusUnderReview;
			model.CreatedDateInText = ToPersianDateTimeShort(DateTime.Now);
			model.AmbassadorId = null;
			model.OutdoorAmbassadorServiceId = null;
			model.LastComment = null;
			model.DoneDateTimeInText = null;
			model.Comments = null;
			model.Attachments = null;

			var saved = await unitOfWork.Repository<AmbassadorRequest>().SaveAsync(model, cn, true);
			return Ok(await LoadForEditAsync(saved.Id!.Value, cn));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(AmbassadorRequest model, CancellationToken cn)
		{
			if (!CanCrud) return BadRequest("دسترسی ذخیره وجود ندارد");
			ClearNav(model);
			var old = await unitOfWork.Repository<AmbassadorRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == model.Id, cn);
			if (old == null) return BadRequest("رکورد یافت نشد");
			if (!await CanAccessRowAsync(old, cn)) return BadRequest("دسترسی به این رکورد وجود ندارد");

			var err = ValidateHeader(model);
			if (err != null) return BadRequest(err);
			ApplyNeedDateFromText(model);

			// Preserve non-form fields; only Type/JobType/Need*/WaitingTime/Destination/PackageDescription change.
			model.CreatedById = old.CreatedById;
			model.CreatedByName = old.CreatedByName;
			model.CreatedOnMiladiDateTime = old.CreatedOnMiladiDateTime;
			model.CreatedOnShamsiDateTime = old.CreatedOnShamsiDateTime;
			model.CreatedDateInText = old.CreatedDateInText;
			model.HtsId = old.HtsId;
			model.StatusId = old.StatusId;
			model.AmbassadorId = old.AmbassadorId;
			model.OutdoorAmbassadorServiceId = old.OutdoorAmbassadorServiceId;
			model.LastComment = old.LastComment;
			model.DoneDateTimeInText = old.DoneDateTimeInText;
			model.Comments = null;
			model.Attachments = null;

			await unitOfWork.Repository<AmbassadorRequest>().UpdateAsync(model, cn, true);
			return Ok(await LoadForEditAsync(model.Id!.Value, cn));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			if (!CanCrud) return BadRequest("دسترسی حذف وجود ندارد");
			var entity = await unitOfWork.Repository<AmbassadorRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return Ok();
			if (!await CanAccessRowAsync(entity, cn)) return BadRequest("دسترسی به این رکورد وجود ندارد");

			var hasComment = await unitOfWork.Repository<AmbassadorRequestComment>().TableNoTracking
				.AnyAsync(c => c.AmbassadorRequestId == id, cn);
			var hasAttachment = await unitOfWork.Repository<AmbassadorRequestAttachment>().TableNoTracking
				.AnyAsync(c => c.AmbassadorRequestId == id, cn);
			if (hasComment || hasAttachment)
				return BadRequest("این درخواست دارای کامنت یا پیوست است و قابل حذف نیست");

			await unitOfWork.Repository<AmbassadorRequest>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public async Task<IActionResult> Edit(long? id, CancellationToken cn)
		{
			if (!CanCrud) return BadRequest("دسترسی ویرایش وجود ندارد");
			AmbassadorRequest entity;
			if (id != null && id != 0)
			{
				entity = await LoadForEditAsync(id.Value, cn) ?? new AmbassadorRequest();
				if (entity.Id is > 0 && !await CanAccessRowAsync(entity, cn))
					return BadRequest("دسترسی به این رکورد وجود ندارد");
			}
			else
			{
				entity = new AmbassadorRequest();
			}

			return View(@"\Views\Panel\Srv\AmbassadorRequest\Edit.cshtml", entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			if (!CanCrud) return BadRequest("دسترسی درج وجود ندارد");
			return View(@"\Views\Panel\Srv\AmbassadorRequest\Edit.cshtml", new AmbassadorRequest());
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			if (!CanViewPage) return BadRequest("دسترسی مشاهده وجود ندارد");
			return View(@"\Views\Panel\Srv\AmbassadorRequest\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<AmbassadorRequest>().FetchDataAsync(request, cn));

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + @"\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<AmbassadorRequest>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
		public async Task<IActionResult> Confirm(ConfirmAmbassadorRequestRequest model, CancellationToken cn)
		{
			if (!CanConfirm) return BadRequest("دسترسی بررسی و تایید وجود ندارد");
			if (model == null || model.Id <= 0) return BadRequest("شناسه درخواست نامعتبر است");
			if (!model.Comment.HasValue()) return BadRequest("توضیحات اجباری است");

			var entity = await unitOfWork.Repository<AmbassadorRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == model.Id, cn);
			if (entity == null) return BadRequest("رکورد یافت نشد");
			if (!await CanAccessRowAsync(entity, cn)) return BadRequest("دسترسی به این رکورد وجود ندارد");

			var hasAmbassador = model.AmbassadorId is > 0;
			var hasOutdoor = model.OutdoorAmbassadorServiceId is > 0;
			if (hasAmbassador && hasOutdoor)
				return BadRequest("پیک داخلی و پیک خارج از سازمان همزمان قابل انتخاب نیستند");

			var nowText = ToPersianDateTimeShort(DateTime.Now);
			var comment = new AmbassadorRequestComment
			{
				HtsId = 0,
				AmbassadorRequestId = entity.Id!.Value,
				Comment = model.Comment!.Trim(),
				StatusId = model.StatusId,
				CreatedDateInText = nowText
			};
			await unitOfWork.Repository<AmbassadorRequestComment>().SaveAsync(comment, cn, true);

			if (model.StatusId.HasValue)
				entity.StatusId = model.StatusId.Value;

			if (hasAmbassador)
			{
				entity.AmbassadorId = model.AmbassadorId;
				entity.OutdoorAmbassadorServiceId = null;
			}
			else if (hasOutdoor)
			{
				entity.OutdoorAmbassadorServiceId = (OutdoorAmbassadorServiceEnum?)model.OutdoorAmbassadorServiceId;
				entity.AmbassadorId = null;
			}

			entity.LastComment = model.Comment.Trim();
			if (entity.StatusId == StatusDone)
				entity.DoneDateTimeInText = nowText;

			entity.Comments = null;
			entity.Attachments = null;
			entity.Ambassador = null;

			await unitOfWork.Repository<AmbassadorRequest>().UpdateAsync(entity, cn, true);

			if (hasAmbassador && entity.StatusId == StatusApproved)
			{
				// TODO: later SMS to the driver's mobile with the old message body.
				// Do not call any SMS API and do not write a sender.
			}

			return Ok();
		}

		#endregion

		#region Attachments

		[HttpGet("[action]")]
		[ActionDisplayName("پیوست مدارک", ActionAccessType.View, ActionAccessItemType.Custom)]
		public async Task<IActionResult> Attachments(long id, CancellationToken cn)
		{
			if (!CanAttachments) return BadRequest("دسترسی پیوست وجود ندارد");
			var parent = await unitOfWork.Repository<AmbassadorRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (parent == null) return BadRequest("درخواست یافت نشد");
			if (!await CanAccessRowAsync(parent, cn)) return BadRequest("دسترسی به این رکورد وجود ندارد");

			ViewBag.ParentId = id;
			return View(@"\Views\Panel\Srv\AmbassadorRequest\Attachments.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره پیوست", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> SaveAttachment(SaveAmbassadorRequestAttachmentRequest request, CancellationToken cn)
		{
			if (!CanAttachments) return BadRequest("دسترسی پیوست وجود ندارد");
			if (request == null || request.AmbassadorRequestId <= 0)
				return BadRequest("شناسه درخواست نامعتبر است");
			if (!request.FileBase64.HasValue() && (request.FileContent == null || request.FileContent.Length == 0))
				return BadRequest("فایل اجباری است");

			var parent = await unitOfWork.Repository<AmbassadorRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == request.AmbassadorRequestId, cn);
			if (parent == null) return BadRequest("درخواست یافت نشد");
			if (!await CanAccessRowAsync(parent, cn)) return BadRequest("دسترسی به این رکورد وجود ندارد");

			var existingCount = await unitOfWork.Repository<AmbassadorRequestAttachment>().TableNoTracking
				.CountAsync(c => c.AmbassadorRequestId == request.AmbassadorRequestId, cn);
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
			var entity = new AmbassadorRequestAttachment
			{
				HtsId = 0,
				AmbassadorRequestId = request.AmbassadorRequestId,
				FileName = request.FileName.HasValue() ? request.FileName!.Trim() : "file",
				FileContent = bytes,
				FileSize = bytes.LongLength,
				Comment = request.Comment.HasValue() ? request.Comment!.Trim() : null,
				CreatedDate = now.ToShamsiDate(),
				CreatedTime = $"{now.Hour:D2}:{now.Minute:D2}"
			};

			var saved = await unitOfWork.Repository<AmbassadorRequestAttachment>().SaveAsync(entity, cn, true);
			saved.FileContent = Array.Empty<byte>();
			return Ok(saved);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("حذف پیوست", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> DeleteAttachment(long id, CancellationToken cn)
		{
			if (!CanAttachments) return BadRequest("دسترسی حذف پیوست وجود ندارد");
			var entity = await unitOfWork.Repository<AmbassadorRequestAttachment>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return Ok();

			var parent = await unitOfWork.Repository<AmbassadorRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == entity.AmbassadorRequestId, cn);
			if (parent == null) return BadRequest("درخواست یافت نشد");
			if (!await CanAccessRowAsync(parent, cn)) return BadRequest("دسترسی به این رکورد وجود ندارد");

			await unitOfWork.Repository<AmbassadorRequestAttachment>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("مشاهده پیوست", ActionAccessType.View, ActionAccessItemType.Custom)]
		public async Task<IActionResult> ViewAttachment(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<AmbassadorRequestAttachment>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return NotFound();

			var parent = await unitOfWork.Repository<AmbassadorRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == entity.AmbassadorRequestId, cn);
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
		public async Task<IActionResult> Comments(long ambassadorRequestId, CancellationToken cn)
		{
			var parent = await unitOfWork.Repository<AmbassadorRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == ambassadorRequestId, cn);
			if (parent == null) return BadRequest("درخواست یافت نشد");
			if (!await CanAccessRowAsync(parent, cn)) return BadRequest("دسترسی به این رکورد وجود ندارد");

			var items = await unitOfWork.Repository<AmbassadorRequestComment>().TableNoTracking
				.Where(c => c.AmbassadorRequestId == ambassadorRequestId)
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
					CreatedByName = c.CreatedByName,
					CreatedDateInText = c.CreatedDateInText
				})
				.ToListAsync(cn);

			return Ok(items);
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> Drivers(CancellationToken cn)
		{
			var items = await unitOfWork.Repository<Personel>().TableNoTracking
				.Where(p => p.Code != null && DriverPersonelCodes.Contains(p.Code))
				.OrderBy(p => p.Family)
				.ThenBy(p => p.Name)
				.Select(p => new
				{
					p.Id,
					Title = ((p.Name ?? "") + " " + (p.Family ?? "")).Trim()
				})
				.ToListAsync(cn);
			return Ok(items);
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> GetAttachments(long ambassadorRequestId, CancellationToken cn)
		{
			var parent = await unitOfWork.Repository<AmbassadorRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == ambassadorRequestId, cn);
			if (parent == null) return BadRequest("درخواست یافت نشد");
			if (!await CanAccessRowAsync(parent, cn)) return BadRequest("دسترسی به این رکورد وجود ندارد");

			var items = await unitOfWork.Repository<AmbassadorRequestAttachment>().TableNoTracking
				.Where(c => c.AmbassadorRequestId == ambassadorRequestId)
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

		public class ConfirmAmbassadorRequestRequest
		{
			public long Id { get; set; }
			public long? AmbassadorId { get; set; }
			public int? OutdoorAmbassadorServiceId { get; set; }
			public short? StatusId { get; set; }
			public string? Comment { get; set; }
		}

		public class SaveAmbassadorRequestAttachmentRequest
		{
			public long AmbassadorRequestId { get; set; }
			public string? Comment { get; set; }
			public string? FileName { get; set; }
			public string? FileBase64 { get; set; }
			public byte[]? FileContent { get; set; }
		}

		private async Task<bool> CanAccessRowAsync(AmbassadorRequest entity, CancellationToken cn)
		{
			if (CanShowAllRows) return true;

			if (CurrentUserHasAnyRole(RoleSupport))
			{
				var currentPersonel = await GetCurrentPersonelAsync(cn);
				if (currentPersonel?.BranchCode == null) return false;

				var creatorBranch = await ResolveCreatorBranchCodeAsync(entity.CreatedById, cn);
				if (creatorBranch == null) return false;

				var currentBranch = currentPersonel.BranchCode.Value;
				return creatorBranch.Value == currentBranch
					|| (creatorBranch.Value != 2 && currentBranch == 1);
			}

			return entity.CreatedById == CurrentUserId;
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

		private static void ClearNav(AmbassadorRequest model)
		{
			model.Ambassador = null;
			model.Comments = null;
			model.Attachments = null;
		}

		private static string? ValidateHeader(AmbassadorRequest model)
		{
			if ((int)model.TypeId == 0) return "نوع اجباری است";
			if ((int)model.JobTypeId == 0) return "نوع کار اجباری است";
			if (!model.NeedDateInText.HasValue() && model.NeedDate == default)
				return "تاریخ نیاز اجباری است";
			if (!model.NeedTime.HasValue()) return "ساعت نیاز اجباری است";
			if (!model.Destination.HasValue()) return "مقصد اجباری است";
			return null;
		}

		private static void ApplyNeedDateFromText(AmbassadorRequest model)
		{
			if (!model.NeedDateInText.HasValue()) return;
			try
			{
				model.NeedDate = model.NeedDateInText!.ToMiladiDate().Date;
			}
			catch
			{
				// leave NeedDate as posted; validation may catch
			}
		}

		private async Task<AmbassadorRequest?> LoadForEditAsync(long id, CancellationToken cn)
		{
			return await unitOfWork.Repository<AmbassadorRequest>().TableNoTracking
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
