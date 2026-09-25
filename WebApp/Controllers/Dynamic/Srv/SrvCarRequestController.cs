using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Entities.App.Gnr;
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
	[Route("Panel/Srv/CarRequest")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("درخواست خودرو", typeof(CarRequest))]
	public class SrvCarRequestController(
		IUnitOfWork unitOfWork,
		IWebHostEnvironment env,
		IUserService userService) : BaseController
	{
		private const string RoleSupport = "Srv.CarRequest.Support";
		private const string RoleRequesterConfirm = "Srv.CarRequest.RequesterConfirm";
		private const string RoleHavayarControl = "Srv.CarRequest.HavayarControl";
		private const string RoleRequester = "Srv.CarRequest.Requester";
		private const string RoleView = "Srv.CarRequest.View";
		private const string RoleShowAllMenus = "ShowAllMenus";

		private const short StatusUnderReview = 1032;
		private const short StatusApproved = 1033;
		private const short StatusDone = 1047;

		private static readonly long[] DriverJobHtsIds = [630, 4387];

		private bool CanShowAllRows =>
			IsAdministrator || CurrentUserHasAnyRole(RoleShowAllMenus);

		private bool CanCrud =>
			IsAdministrator
			|| CurrentUserHasAnyRole(RoleSupport, RoleRequesterConfirm, RoleRequester, RoleShowAllMenus);

		private bool CanConfirmAction =>
			IsAdministrator
			|| CurrentUserHasAnyRole(RoleSupport, RoleRequesterConfirm, RoleHavayarControl, RoleShowAllMenus);

		private bool CanAttachments =>
			IsAdministrator
			|| CurrentUserHasAnyRole(RoleSupport, RoleRequesterConfirm, RoleRequester, RoleShowAllMenus);

		private bool CanViewPage =>
			CanShowAllRows
			|| CurrentUserHasAnyRole(RoleSupport, RoleRequesterConfirm, RoleHavayarControl, RoleRequester, RoleView);

		#region Primary CRUD

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(CarRequest model, CancellationToken cn)
		{
			if (!CanCrud) return BadRequest("دسترسی ذخیره وجود ندارد");
			ClearNav(model);
			var err = ValidateHeader(model);
			if (err != null) return BadRequest(err);

			if (model.Id == null || model.Id == 0)
				return await Add(model, cn);
			if (await unitOfWork.Repository<CarRequest>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(CarRequest model, CancellationToken cn)
		{
			if (!CanCrud) return BadRequest("دسترسی ذخیره وجود ندارد");
			ClearNav(model);
			var err = ValidateHeader(model);
			if (err != null) return BadRequest(err);
			err = ValidateOutOfTownNeedDate(model);
			if (err != null) return BadRequest(err);

			ApplyNeedDateFromText(model);
			err = ValidateOutOfTownNeedDate(model);
			if (err != null) return BadRequest(err);

			model.HtsId = 0;
			model.StatusId = StatusUnderReview;
			model.CreatedDateInText = ToPersianDateTimeShort(DateTime.Now);
			model.DriverId = null;
			model.OutdoorCarServiceId = null;
			model.LastComment = null;
			model.DoneDateTimeInText = null;
			model.Comments = null;
			model.Attachments = null;

			var saved = await unitOfWork.Repository<CarRequest>().SaveAsync(model, cn, true);
			return Ok(await LoadForEditAsync(saved.Id!.Value, cn));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(CarRequest model, CancellationToken cn)
		{
			if (!CanCrud) return BadRequest("دسترسی ذخیره وجود ندارد");
			ClearNav(model);
			var old = await unitOfWork.Repository<CarRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == model.Id, cn);
			if (old == null) return BadRequest("رکورد یافت نشد");
			if (!await CanAccessRowAsync(old, cn)) return BadRequest("دسترسی به این رکورد وجود ندارد");

			var err = ValidateHeader(model);
			if (err != null) return BadRequest(err);
			ApplyNeedDateFromText(model);
			err = ValidateOutOfTownNeedDate(model);
			if (err != null) return BadRequest(err);

			model.CreatedById = old.CreatedById;
			model.CreatedByName = old.CreatedByName;
			model.CreatedOnMiladiDateTime = old.CreatedOnMiladiDateTime;
			model.CreatedOnShamsiDateTime = old.CreatedOnShamsiDateTime;
			model.CreatedDateInText = old.CreatedDateInText;
			model.HtsId = old.HtsId;
			model.StatusId = old.StatusId;
			model.DriverId = old.DriverId;
			model.OutdoorCarServiceId = old.OutdoorCarServiceId;
			model.LastComment = old.LastComment;
			model.DoneDateTimeInText = old.DoneDateTimeInText;
			model.Comments = null;
			model.Attachments = null;

			await unitOfWork.Repository<CarRequest>().UpdateAsync(model, cn, true);
			return Ok(await LoadForEditAsync(model.Id!.Value, cn));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			if (!CanCrud) return BadRequest("دسترسی حذف وجود ندارد");
			var entity = await unitOfWork.Repository<CarRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return Ok();
			if (!await CanAccessRowAsync(entity, cn)) return BadRequest("دسترسی به این رکورد وجود ندارد");

			var hasComment = await unitOfWork.Repository<CarRequestComment>().TableNoTracking
				.AnyAsync(c => c.CarRequestId == id, cn);
			var hasAttachment = await unitOfWork.Repository<CarRequestAttachment>().TableNoTracking
				.AnyAsync(c => c.CarRequestId == id, cn);
			if (hasComment || hasAttachment)
				return BadRequest("این درخواست دارای کامنت یا پیوست است و قابل حذف نیست");

			await unitOfWork.Repository<CarRequest>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public async Task<IActionResult> Edit(long? id, CancellationToken cn)
		{
			if (!CanCrud) return BadRequest("دسترسی ویرایش وجود ندارد");
			CarRequest entity;
			if (id != null && id != 0)
			{
				entity = await LoadForEditAsync(id.Value, cn) ?? new CarRequest();
				if (entity.Id is > 0 && !await CanAccessRowAsync(entity, cn))
					return BadRequest("دسترسی به این رکورد وجود ندارد");
			}
			else
			{
				entity = new CarRequest();
				entity.PassengerId = await GetCurrentPersonelIdAsync(cn);
			}

			return View(@"\Views\Panel\Srv\CarRequest\Edit.cshtml", entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public async Task<IActionResult> New(CancellationToken cn)
		{
			if (!CanCrud) return BadRequest("دسترسی درج وجود ندارد");
			var entity = new CarRequest
			{
				PassengerId = await GetCurrentPersonelIdAsync(cn)
			};
			return View(@"\Views\Panel\Srv\CarRequest\Edit.cshtml", entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			if (!CanViewPage) return BadRequest("دسترسی مشاهده وجود ندارد");
			return View(@"\Views\Panel\Srv\CarRequest\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<CarRequest>().FetchDataAsync(request, cn));

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + @"\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<CarRequest>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
		public async Task<IActionResult> Confirm(ConfirmCarRequestRequest model, CancellationToken cn)
		{
			if (!CanConfirmAction) return BadRequest("دسترسی بررسی و تایید وجود ندارد");
			if (model == null || model.Id <= 0) return BadRequest("شناسه درخواست نامعتبر است");
			if (!model.Comment.HasValue()) return BadRequest("توضیحات اجباری است");

			var entity = await unitOfWork.Repository<CarRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == model.Id, cn);
			if (entity == null) return BadRequest("رکورد یافت نشد");
			if (!await CanAccessRowAsync(entity, cn)) return BadRequest("دسترسی به این رکورد وجود ندارد");

			var hasDriver = model.DriverId is > 0;
			var hasOutdoor = model.OutdoorCarServiceId is > 0;
			if (hasDriver && hasOutdoor)
				return BadRequest("راننده داخلی و سرویس بیرونی همزمان قابل انتخاب نیستند");

			var nowText = ToPersianDateTimeShort(DateTime.Now);
			var comment = new CarRequestComment
			{
				HtsId = 0,
				CarRequestId = entity.Id!.Value,
				Comment = model.Comment!.Trim(),
				StatusId = model.StatusId,
				CreatedDateInText = nowText
			};
			await unitOfWork.Repository<CarRequestComment>().SaveAsync(comment, cn, true);

			if (model.StatusId.HasValue)
				entity.StatusId = model.StatusId.Value;

			if (hasDriver)
			{
				entity.DriverId = model.DriverId;
				entity.OutdoorCarServiceId = null;
			}
			else if (hasOutdoor)
			{
				entity.OutdoorCarServiceId = (OutdoorCarServiceEnum?)model.OutdoorCarServiceId;
				entity.DriverId = null;
			}

			entity.LastComment = model.Comment.Trim();
			if (entity.StatusId == StatusDone)
				entity.DoneDateTimeInText = nowText;

			entity.Comments = null;
			entity.Attachments = null;
			entity.Passenger = null;
			entity.Driver = null;
			entity.CostCenter = null;

			await unitOfWork.Repository<CarRequest>().UpdateAsync(entity, cn, true);

			if (hasDriver && entity.StatusId == StatusApproved)
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
			var parent = await unitOfWork.Repository<CarRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (parent == null) return BadRequest("درخواست یافت نشد");
			if (!await CanAccessRowAsync(parent, cn)) return BadRequest("دسترسی به این رکورد وجود ندارد");

			ViewBag.ParentId = id;
			return View(@"\Views\Panel\Srv\CarRequest\Attachments.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره پیوست", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> SaveAttachment(SaveCarRequestAttachmentRequest request, CancellationToken cn)
		{
			if (!CanAttachments) return BadRequest("دسترسی پیوست وجود ندارد");
			if (request == null || request.CarRequestId <= 0)
				return BadRequest("شناسه درخواست نامعتبر است");
			if (!request.Comment.HasValue())
				return BadRequest("توضیحات پیوست اجباری است");
			if (!request.FileBase64.HasValue() && (request.FileContent == null || request.FileContent.Length == 0))
				return BadRequest("فایل اجباری است");

			var parent = await unitOfWork.Repository<CarRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == request.CarRequestId, cn);
			if (parent == null) return BadRequest("درخواست یافت نشد");
			if (!await CanAccessRowAsync(parent, cn)) return BadRequest("دسترسی به این رکورد وجود ندارد");

			var bytes = request.FileContent;
			if ((bytes == null || bytes.Length == 0) && request.FileBase64.HasValue())
				bytes = DecodeBase64File(request.FileBase64!);
			if (bytes == null || bytes.Length == 0)
				return BadRequest("فایل اجباری است");

			var now = DateTime.Now;
			var entity = new CarRequestAttachment
			{
				HtsId = 0,
				CarRequestId = request.CarRequestId,
				FileName = request.FileName.HasValue() ? request.FileName!.Trim() : "file",
				FileContent = bytes,
				FileSize = bytes.LongLength,
				Comment = request.Comment!.Trim(),
				CreatedDate = now.ToShamsiDate(),
				CreatedTime = $"{now.Hour:D2}:{now.Minute:D2}"
			};

			var saved = await unitOfWork.Repository<CarRequestAttachment>().SaveAsync(entity, cn, true);
			saved.FileContent = Array.Empty<byte>();
			return Ok(saved);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("حذف پیوست", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> DeleteAttachment(long id, CancellationToken cn)
		{
			if (!CanAttachments) return BadRequest("دسترسی حذف پیوست وجود ندارد");
			var entity = await unitOfWork.Repository<CarRequestAttachment>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return Ok();

			var parent = await unitOfWork.Repository<CarRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == entity.CarRequestId, cn);
			if (parent == null) return BadRequest("درخواست یافت نشد");
			if (!await CanAccessRowAsync(parent, cn)) return BadRequest("دسترسی به این رکورد وجود ندارد");

			await unitOfWork.Repository<CarRequestAttachment>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("مشاهده پیوست", ActionAccessType.View, ActionAccessItemType.Custom)]
		public async Task<IActionResult> ViewAttachment(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<CarRequestAttachment>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return NotFound();

			var parent = await unitOfWork.Repository<CarRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == entity.CarRequestId, cn);
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
		public async Task<IActionResult> Comments(long carRequestId, CancellationToken cn)
		{
			var parent = await unitOfWork.Repository<CarRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == carRequestId, cn);
			if (parent == null) return BadRequest("درخواست یافت نشد");
			if (!await CanAccessRowAsync(parent, cn)) return BadRequest("دسترسی به این رکورد وجود ندارد");

			var items = await unitOfWork.Repository<CarRequestComment>().TableNoTracking
				.Where(c => c.CarRequestId == carRequestId)
				.OrderByDescending(c => c.Id)
				.Select(c => new
				{
					c.Id,
					c.StatusId,
					StatusTitle = c.StatusId == StatusUnderReview ? "در حال بررسی"
						: c.StatusId == StatusApproved ? "تایید شده"
						: c.StatusId == 1034 ? "رد شده"
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
			var jobIds = await unitOfWork.Repository<Job>().TableNoTracking
				.Where(j => DriverJobHtsIds.Contains(j.HtsId))
				.Select(j => j.Id)
				.ToListAsync(cn);

			var items = await unitOfWork.Repository<Personel>().TableNoTracking
				.Where(p => p.IsActive == IsActiveEnum.Active
					&& p.JobId != null
					&& jobIds.Contains(p.JobId.Value))
				.OrderBy(p => p.Family)
				.ThenBy(p => p.Name)
				.Select(p => new
				{
					p.Id,
					Title = ((p.Name ?? "") + " " + (p.FamilyDisplay ?? p.Family ?? "")).Trim()
				})
				.ToListAsync(cn);
			return Ok(items);
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> GetAttachments(long carRequestId, CancellationToken cn)
		{
			var parent = await unitOfWork.Repository<CarRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == carRequestId, cn);
			if (parent == null) return BadRequest("درخواست یافت نشد");
			if (!await CanAccessRowAsync(parent, cn)) return BadRequest("دسترسی به این رکورد وجود ندارد");

			var items = await unitOfWork.Repository<CarRequestAttachment>().TableNoTracking
				.Where(c => c.CarRequestId == carRequestId)
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

		public class ConfirmCarRequestRequest
		{
			public long Id { get; set; }
			public long? DriverId { get; set; }
			public int? OutdoorCarServiceId { get; set; }
			public short? StatusId { get; set; }
			public string? Comment { get; set; }
		}

		public class SaveCarRequestAttachmentRequest
		{
			public long CarRequestId { get; set; }
			public string? Comment { get; set; }
			public string? FileName { get; set; }
			public string? FileBase64 { get; set; }
			public byte[]? FileContent { get; set; }
		}

		private async Task<bool> CanAccessRowAsync(CarRequest entity, CancellationToken cn)
		{
			if (CanShowAllRows) return true;

			if (CurrentUserHasAnyRole(RoleSupport))
			{
				var branchId = await ResolveSupportBranchIdAsync(cn);
				return entity.BranchId.HasValue && (int)entity.BranchId.Value == branchId;
			}

			return entity.CreatedById == CurrentUserId;
		}

		private async Task<int> ResolveSupportBranchIdAsync(CancellationToken cn)
		{
			var personel = await GetCurrentPersonelAsync(cn);
			return personel?.BranchCode == 1 ? 43 : 44;
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

		private async Task<long?> GetCurrentPersonelIdAsync(CancellationToken cn)
		{
			var p = await GetCurrentPersonelAsync(cn);
			return p?.Id;
		}

		private static void ClearNav(CarRequest model)
		{
			model.Passenger = null;
			model.Driver = null;
			model.CostCenter = null;
			model.Comments = null;
			model.Attachments = null;
		}

		private static string? ValidateHeader(CarRequest model)
		{
			if (model.BranchId == null || (int)model.BranchId == 0) return "محل خدمت اجباری است";
			if ((int)model.TypeId == 0) return "نوع اجباری است";
			if (!model.NeedDateInText.HasValue() && model.NeedDate == default)
				return "تاریخ نیاز اجباری است";
			if (!model.NeedTime.HasValue()) return "ساعت نیاز اجباری است";
			if (!model.Destination.HasValue()) return "مقصد اجباری است";
			if (model.WaitingTime == null) return "مدت انتظار اجباری است";
			if (model.CostCenterId is null or 0) return "مرکز هزینه اجباری است";
			return null;
		}

		private static void ApplyNeedDateFromText(CarRequest model)
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

		private static string? ValidateOutOfTownNeedDate(CarRequest model)
		{
			if (model.TypeId != CarRequestTypeEnum.OutOfTown) return null;
			var need = model.NeedDate;
			if (need == default && model.NeedDateInText.HasValue())
			{
				try { need = model.NeedDateInText!.ToMiladiDate().Date; }
				catch { return null; }
			}
			if (need == default) return null;
			if (need.Date < DateTime.Today.AddDays(1))
				return "تاریخ نیاز می بایست 1 روز قبل از تاریخ ثبت درخواست باشد!";
			return null;
		}

		private async Task<CarRequest?> LoadForEditAsync(long id, CancellationToken cn)
		{
			return await unitOfWork.Repository<CarRequest>().TableNoTracking
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
