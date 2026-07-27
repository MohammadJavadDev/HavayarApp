using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Entities.App.Inq;
using Entities.App.Inq.Enums;
using Entities.Base;
using Entities.Base.DataTable;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Auth;
using Services.NotificationGroupServices;
using Services.NotificationServices;
using System.Text;
using WebApp.Services;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("استعلام فنی و مالی", typeof(TechnicalInquiry))]
	public class TechnicalInquiryController : BaseController
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IWebHostEnvironment _webHostEnvironment;
		private readonly INotificationGroupService _notificationGroupService;
		private readonly INotificationService _notificationService;
		private readonly IUserService _userService;

		public TechnicalInquiryController(
		    IUnitOfWork unitOfWork,
		    IWebHostEnvironment webHostEnvironment,
		    INotificationGroupService notificationGroupService,
		    INotificationService notificationService,
		    IUserService userService) : base()
		{
			_unitOfWork = unitOfWork;
			_webHostEnvironment = webHostEnvironment;
			_notificationGroupService = notificationGroupService;
			_notificationService = notificationService;
			_userService = userService;
		}

		#region CRUD Operations

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(TechnicalInquiry technicalInquiry, CancellationToken cn)
		{
			if (technicalInquiry.Id == null || technicalInquiry.Id == 0)
			{
				return await AddNew(technicalInquiry, cn);
			}

			var exist = await _unitOfWork.Repository<TechnicalInquiry>().TableNoTracking
			    .AnyAsync(c => c.Id == technicalInquiry.Id, cn);

			if (!exist)
				return await AddNew(technicalInquiry, cn);

			return technicalInquiry.LastStatus switch
			{
				TechnicalInquiryStatusEnum.SendToEngineering => await SendToEngineering(technicalInquiry, cn),
				TechnicalInquiryStatusEnum.Modified => await UpdateExisting(technicalInquiry, cn),
				TechnicalInquiryStatusEnum.SendEngineeringToTheProjectManager => await SendToManager(technicalInquiry, cn),
				TechnicalInquiryStatusEnum.ManagerApproved => await ManagerApproved(technicalInquiry, cn),
				TechnicalInquiryStatusEnum.ManagerReject => await SendToManager(technicalInquiry, cn),
				TechnicalInquiryStatusEnum.IssuedToVendor => await ManagerApproved(technicalInquiry, cn),
				TechnicalInquiryStatusEnum.SupplierRejectToEngineering => await ManagerApproved(technicalInquiry, cn),
				TechnicalInquiryStatusEnum.Reviewfilebyengineeringandsendingtosupplyandprocurement => await ReviewEngineering(technicalInquiry, cn),
				TechnicalInquiryStatusEnum.SendToFinancial => await SendToFinancial(technicalInquiry, cn),
				_ => await UpdateExisting(technicalInquiry, cn),
			};
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(TechnicalInquiry technicalInquiry, CancellationToken cn)
		{
			return await AddNew(technicalInquiry, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(TechnicalInquiry technicalInquiry, CancellationToken cn)
		{
			return await UpdateExisting(technicalInquiry, cn);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = await _unitOfWork.Repository<TechnicalInquiry>().Table
			    .FirstOrDefaultAsync(c => c.Id == id, cn);

			if (model != null)
			{
				model.IsActive = IsActiveEnum.Deleted;
				await _unitOfWork.Repository<TechnicalInquiry>().UpdateAsync(model, cn, true);
			}

			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = _unitOfWork.Repository<TechnicalInquiry>().TableNoTracking
				    .Include(it => it.TechnicalInquiryHistory)
				    .FirstOrDefault(c => c.Id == id);

				if (entity != null && entity.TechnicalInquiryHistory == null)
				{
					entity.TechnicalInquiryHistory = new List<TechnicalInquiryComment>();
				}

				return View(@"\Views\Panel\Inq\TechnicalInquiry\Edit.cshtml", entity);
			}

			var newEntity = new TechnicalInquiry
			{
				TechnicalInquiryHistory = new List<TechnicalInquiryComment>()
			};
			return View(@"\Views\Panel\Inq\TechnicalInquiry\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var newEntity = new TechnicalInquiry
			{
				TechnicalInquiryHistory = new List<TechnicalInquiryComment>()
			};
			return View(@"\Views\Panel\Inq\TechnicalInquiry\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Inq\TechnicalInquiry\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = Path.Combine(_webHostEnvironment.WebRootPath, "Aspose.Total.NET.lic");
			var memoryStream = new MemoryStream();

			try
			{
				await _unitOfWork.Repository<TechnicalInquiry>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"exportExcel.xlsx");
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"خطا در زمان ایجاد فایل اکسل: {ex.Message}");
			}
		}

		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		[HttpPost("[action]")]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
		{
			return Ok(await _unitOfWork.Repository<TechnicalInquiry>().FetchDataAsync(request, cn));
		}

		#endregion

		#region Private Methods - Add & Update

		private async Task<IActionResult> AddNew(TechnicalInquiry entity, CancellationToken cn)
		{
			try
			{
				var now = DateTime.Now;
				var currentUserId = CurrentUserId;
				var currentUserFullName = CurrentUserFullName;

				entity.CreatedById = currentUserId;
				entity.CreatedByName = currentUserFullName;
				entity.CreatedOnMiladiDateTime = now;
				entity.CreatedOnShamsiDateTime = now.ToShamsiDateTime();
				entity.ModifiedById = currentUserId;
				entity.ModifiedByName = currentUserFullName;
				entity.LastStatus = TechnicalInquiryStatusEnum.SendToEngineering;
				entity.IsActive = IsActiveEnum.Active;

				var result = await _unitOfWork.Repository<TechnicalInquiry>().AddAsync(entity, cn, true);

				await AddTechnicalInquiryComment(result, TechnicalInquiryStatusEnum.SendToEngineering, "ایجاد درخواست استعلام فنی و ارسال به مهندسی", cn);

				await CreateSendToEngineeringNotification(result, cn);
				await SendEmailToSupplyGroup(result, "ایجاد درخواست جدید", entity.Comment, cn);
				await CreateShiraziMahmoudNotification(result, "ایجاد درخواست جدید", cn);

				return Ok(result);
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"خطا در ایجاد درخواست: {ex.Message}");
			}
		}

		private async Task<IActionResult> UpdateExisting(TechnicalInquiry entity, CancellationToken cn)
		{
			try
			{
				var originalEntity = await _unitOfWork.Repository<TechnicalInquiry>().Table
				    .FirstOrDefaultAsync(c => c.Id == entity.Id, cn);

				if (originalEntity == null)
					return NotFound("درخواست یافت نشد");

				var now = DateTime.Now;
				var currentUserId = CurrentUserId;

				originalEntity.ModifiedById = currentUserId;
				originalEntity.ModifiedByName = CurrentUserFullName;
				originalEntity.LastStatus = TechnicalInquiryStatusEnum.Modified;
				originalEntity.ProjectTitle = entity.ProjectTitle ?? originalEntity.ProjectTitle;
				originalEntity.ProjectId = entity.ProjectId ?? originalEntity.ProjectId;
				originalEntity.ProjectManagerId = entity.ProjectManagerId ?? originalEntity.ProjectManagerId;
				originalEntity.RelevantPSLId = entity.RelevantPSLId ?? originalEntity.RelevantPSLId;
				originalEntity.PartId = entity.PartId ?? originalEntity.PartId;
				originalEntity.NeedMiladiDate = entity.NeedMiladiDate ?? originalEntity.NeedMiladiDate;
				originalEntity.NeedShamsiDate = entity.NeedShamsiDate ?? originalEntity.NeedShamsiDate;
				originalEntity.Comment = entity.Comment ?? originalEntity.Comment;
				originalEntity.DatasheetFileId = entity.DatasheetFileId ?? originalEntity.DatasheetFileId;
				originalEntity.MapFileId = entity.MapFileId ?? originalEntity.MapFileId;
				originalEntity.SpecFileId = entity.SpecFileId ?? originalEntity.SpecFileId;
				originalEntity.TcFileId = entity.TcFileId ?? originalEntity.TcFileId;
				originalEntity.OtherFileId = entity.OtherFileId ?? originalEntity.OtherFileId;

				if (TechnicalInquiryHelper.CanSaveContractorDescription(entity.LastStatus))
				{
					originalEntity.DescriptionContractor = entity.DescriptionContractor ?? originalEntity.DescriptionContractor;
				}
				else
				{
					originalEntity.DescriptionContractor = null;
				}

				var result = await _unitOfWork.Repository<TechnicalInquiry>().UpdateAsync(originalEntity, cn, true);

				await AddTechnicalInquiryComment(result, TechnicalInquiryStatusEnum.Modified, $"ویرایش درخواست: {entity?.Comment}", cn);

				await CreateModifiedNotification(result, cn);
				await SendEmailToSupplyGroup(result, "ویرایش درخواست", entity?.Comment, cn);
				await CreateShiraziMahmoudNotification(result, "ویرایش درخواست", cn);

				return Ok(result);
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"خطا در ویرایش درخواست: {ex.Message}");
			}
		}

		#endregion

		#region Workflow Methods

		private async Task<IActionResult> SendToEngineering(TechnicalInquiry entity, CancellationToken cn)
		{
			try
			{
				var originalEntity = await _unitOfWork.Repository<TechnicalInquiry>().Table
				    .FirstOrDefaultAsync(c => c.Id == entity.Id, cn);

				if (originalEntity == null)
					return NotFound("درخواست یافت نشد");

				var now = DateTime.Now;
				var currentUserId = CurrentUserId;

				originalEntity.ModifiedById = currentUserId;
				originalEntity.ModifiedByName = CurrentUserFullName;
				originalEntity.LastStatus = TechnicalInquiryStatusEnum.SendToEngineering;
				originalEntity.Comment = entity.Comment ?? originalEntity.Comment;
				originalEntity.DatasheetFileId = entity.DatasheetFileId ?? originalEntity.DatasheetFileId;
				originalEntity.MapFileId = entity.MapFileId ?? originalEntity.MapFileId;
				originalEntity.SpecFileId = entity.SpecFileId ?? originalEntity.SpecFileId;
				originalEntity.TcFileId = entity.TcFileId ?? originalEntity.TcFileId;
				originalEntity.OtherFileId = entity.OtherFileId ?? originalEntity.OtherFileId;

				var result = await _unitOfWork.Repository<TechnicalInquiry>().UpdateAsync(originalEntity, cn, true);

				await AddTechnicalInquiryComment(result, TechnicalInquiryStatusEnum.SendToEngineering, $"ارسال مجدد به مهندسی: {entity.Comment}", cn);

				await CreateSendToEngineeringNotification(result, cn);
				await SendEmailToSupplyGroup(result, "ارسال مجدد به مهندسی", entity.Comment, cn);
				await CreateShiraziMahmoudNotification(result, "ارسال مجدد به مهندسی", cn);

				return Ok(result);
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"خطا در ارسال به مهندسی: {ex.Message}");
			}
		}

		private async Task<IActionResult> SendToManager(TechnicalInquiry entity, CancellationToken cn)
		{
			try
			{
				var originalEntity = await _unitOfWork.Repository<TechnicalInquiry>().Table
				    .FirstOrDefaultAsync(c => c.Id == entity.Id, cn);

				if (originalEntity == null)
					return NotFound("درخواست یافت نشد");

				var now = DateTime.Now;
				var currentUserId = CurrentUserId;

				originalEntity.ModifiedById = currentUserId;
				originalEntity.ModifiedByName = CurrentUserFullName;

				if (entity.LastStatus == TechnicalInquiryStatusEnum.ManagerReject)
				{
					originalEntity.LastStatus = TechnicalInquiryStatusEnum.ManagerReject;
					originalEntity.Comment = entity.Comment ?? originalEntity.Comment;
					originalEntity.OtherFileId = entity.OtherFileId ?? originalEntity.OtherFileId;
					originalEntity.DescriptionContractor = null;

					var result = await _unitOfWork.Repository<TechnicalInquiry>().UpdateAsync(originalEntity, cn, true);

					await AddTechnicalInquiryComment(result, TechnicalInquiryStatusEnum.ManagerReject, $"رد توسط مدیر پروژه: {entity.Comment}", cn);

					await CreateManagerRejectNotification(result, cn);
					await SendEmailToSupplyGroup(result, "رد توسط مدیر پروژه", entity.Comment, cn);
					await CreateShiraziMahmoudNotification(result, "رد توسط مدیر پروژه", cn);

					return Ok(result);
				}
				else
				{
					originalEntity.LastStatus = TechnicalInquiryStatusEnum.ManagerApproved;
					originalEntity.NeedMiladiDate = entity.NeedMiladiDate ?? originalEntity.NeedMiladiDate;
					originalEntity.NeedShamsiDate = entity.NeedShamsiDate ?? originalEntity.NeedShamsiDate;
					originalEntity.Comment = entity.Comment ?? originalEntity.Comment;
					originalEntity.OtherFileId = entity.OtherFileId ?? originalEntity.OtherFileId;
					originalEntity.DescriptionContractor = entity.DescriptionContractor ?? originalEntity.DescriptionContractor;

					var result = await _unitOfWork.Repository<TechnicalInquiry>().UpdateAsync(originalEntity, cn, true);

					await AddTechnicalInquiryComment(result, TechnicalInquiryStatusEnum.ManagerApproved, $"تایید توسط مدیر پروژه: {entity.Comment}", cn);

					await CreateManagerApprovedNotification(result, cn);
					await SendEmailToSupplyGroup(result, "تایید توسط مدیر پروژه", entity.Comment, cn);
					await CreateShiraziMahmoudNotification(result, "تایید توسط مدیر پروژه", cn);

					return Ok(result);
				}
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"خطا در ارسال به مدیر پروژه: {ex.Message}");
			}
		}

		private async Task<IActionResult> ManagerApproved(TechnicalInquiry entity, CancellationToken cn)
		{
			try
			{
				var originalEntity = await _unitOfWork.Repository<TechnicalInquiry>().Table
				    .FirstOrDefaultAsync(c => c.Id == entity.Id, cn);

				if (originalEntity == null)
					return NotFound("درخواست یافت نشد");

				var now = DateTime.Now;
				var currentUserId = CurrentUserId;

				originalEntity.ModifiedById = currentUserId;
				originalEntity.ModifiedByName = CurrentUserFullName;

				if (entity.LastStatus == TechnicalInquiryStatusEnum.SupplierRejectToEngineering)
				{
					originalEntity.LastStatus = TechnicalInquiryStatusEnum.SupplierRejectToEngineering;
					originalEntity.Comment = entity.Comment ?? originalEntity.Comment;
					originalEntity.OtherFileId = entity.OtherFileId ?? originalEntity.OtherFileId;
					originalEntity.DescriptionContractor = null;

					var result = await _unitOfWork.Repository<TechnicalInquiry>().UpdateAsync(originalEntity, cn, true);

					await AddTechnicalInquiryComment(result, TechnicalInquiryStatusEnum.SupplierRejectToEngineering,
					    $"رد توسط تامین‌کننده - برگشت به مهندسی: {entity.Comment}", cn);

					await CreateSupplierRejectNotification(result, cn);
					await SendEmailToSupplyGroup(result, "رد توسط تامین‌کننده - برگشت به مهندسی", entity.Comment, cn);
					await CreateShiraziMahmoudNotification(result, "رد توسط تامین‌کننده", cn);

					return Ok(result);
				}
				else
				{
					originalEntity.LastStatus = TechnicalInquiryStatusEnum.IssuedToVendor;
					originalEntity.Comment = entity.Comment ?? originalEntity.Comment;
					originalEntity.OtherFileId = entity.OtherFileId ?? originalEntity.OtherFileId;
					originalEntity.DescriptionContractor = null;

					var result = await _unitOfWork.Repository<TechnicalInquiry>().UpdateAsync(originalEntity, cn, true);

					await AddTechnicalInquiryComment(result, TechnicalInquiryStatusEnum.IssuedToVendor, $"صدور برای تامین‌کننده: {entity.Comment}", cn);

					await CreateIssueToVendorNotification(result, cn);
					await CreateShiraziMahmoudNotification(result, "صدور برای تامین‌کننده", cn);

					return Ok(result);
				}
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"خطا در تایید مدیر پروژه: {ex.Message}");
			}
		}

		private async Task<IActionResult> ReviewEngineering(TechnicalInquiry entity, CancellationToken cn)
		{
			try
			{
				var originalEntity = await _unitOfWork.Repository<TechnicalInquiry>().Table
				    .FirstOrDefaultAsync(c => c.Id == entity.Id, cn);

				if (originalEntity == null)
					return NotFound("درخواست یافت نشد");

				var now = DateTime.Now;
				var currentUserId = CurrentUserId;

				originalEntity.ModifiedById = currentUserId;
				originalEntity.ModifiedByName = CurrentUserFullName;
				originalEntity.LastStatus = TechnicalInquiryStatusEnum.Reviewfilebyengineeringandsendingtosupplyandprocurement;
				originalEntity.Comment = entity.Comment ?? originalEntity.Comment;
				originalEntity.DatasheetFileId = entity.DatasheetFileId ?? originalEntity.DatasheetFileId;
				originalEntity.MapFileId = entity.MapFileId ?? originalEntity.MapFileId;
				originalEntity.SpecFileId = entity.SpecFileId ?? originalEntity.SpecFileId;
				originalEntity.TcFileId = entity.TcFileId ?? originalEntity.TcFileId;
				originalEntity.OtherFileId = entity.OtherFileId ?? originalEntity.OtherFileId;

				var result = await _unitOfWork.Repository<TechnicalInquiry>().UpdateAsync(originalEntity, cn, true);

				await AddTechnicalInquiryComment(result, TechnicalInquiryStatusEnum.Reviewfilebyengineeringandsendingtosupplyandprocurement,
				    $"بررسی مجدد توسط مهندسی: {entity.Comment}", cn);

				await CreateReviewEngineeringNotification(result, cn);
				await SendEmailToSupplyGroup(result, "بررسی مجدد توسط مهندسی", entity.Comment, cn);
				await CreateShiraziMahmoudNotification(result, "بررسی مجدد توسط مهندسی", cn);

				return Ok(result);
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"خطا در بررسی مجدد مهندسی: {ex.Message}");
			}
		}

		private async Task<IActionResult> SendToFinancial(TechnicalInquiry entity, CancellationToken cn)
		{
			try
			{
				var originalEntity = await _unitOfWork.Repository<TechnicalInquiry>().Table
				    .FirstOrDefaultAsync(c => c.Id == entity.Id, cn);

				if (originalEntity == null)
					return NotFound("درخواست یافت نشد");

				var now = DateTime.Now;
				var currentUserId = CurrentUserId;

				originalEntity.ModifiedById = currentUserId;
				originalEntity.ModifiedByName = CurrentUserFullName;
				originalEntity.LastStatus = TechnicalInquiryStatusEnum.SendToFinancial;
				originalEntity.Comment = entity.Comment ?? originalEntity.Comment;
				originalEntity.OtherFileId = entity.OtherFileId ?? originalEntity.OtherFileId;

				var result = await _unitOfWork.Repository<TechnicalInquiry>().UpdateAsync(originalEntity, cn, true);

				await AddTechnicalInquiryComment(result, TechnicalInquiryStatusEnum.SendToFinancial, $"ارسال به مالی: {entity.Comment}", cn);

				await SendEmailToFinancialGroup(result, "ارسال به مالی", entity.Comment, cn);
				await CreateShiraziMahmoudNotification(result, "ارسال به مالی", cn);

				return Ok(result);
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"خطا در ارسال به مالی: {ex.Message}");
			}
		}

		#endregion

		#region Helper Methods

		private async Task AddTechnicalInquiryComment(TechnicalInquiry inquiry, TechnicalInquiryStatusEnum status, string commentText, CancellationToken cn)
		{
			if (inquiry == null || inquiry.Id == null)
				return;

			var comment = new TechnicalInquiryComment
			{
				TechnicalInquiryId = inquiry.Id.Value,
				LastStatus = status,
				QueryType = inquiry.QueryType != null ? inquiry.QueryType : QueryTypeEnum.Internal,
				Comment = commentText ?? inquiry.Comment,
				CreatedById = CurrentUserId,
				CreatedByName = CurrentUserFullName,
				CreatedOnMiladiDateTime = DateTime.Now,
				CreatedOnShamsiDateTime = DateTime.Now.ToShamsiDateTime(),
			};

			await _unitOfWork.Repository<TechnicalInquiryComment>().AddAsync(comment, cn);
			await _unitOfWork.SaveChangesAsync(cn);
		}

		#endregion

		#region Notification Methods

		private async Task CreateSendToEngineeringNotification(TechnicalInquiry inquiry, CancellationToken cn)
		{
			try
			{
				var engineeringMembers = await _notificationGroupService.GetGroupMembersAsync("Sup.TechnicalInquiry.Engineering", cn);

				if (!engineeringMembers.Any())
					return;

				var title = "درخواست استعلام فنی جدید";
				var body = await BuildNotificationBody(inquiry, "ارسال به مهندسی", "#1a73e8", cn);

				foreach (var member in engineeringMembers)
				{
					await _notificationService.CreateAndSendAsync(new Notification
					{
						Type = NotificationType.Appliaction,
						Title = title,
						Body = body,
						EntityId = inquiry.Id.Value,
						OwnerId = (long)member.UserId,
						ViewPath = $"/Panel/TechnicalInquiry/Edit?id={inquiry.Id}",
						IsRead = false
					});
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error sending notification: {ex.Message}");
			}
		}

		private async Task CreateModifiedNotification(TechnicalInquiry inquiry, CancellationToken cn)
		{
			try
			{
				var engineeringMembers = await _notificationGroupService
				    .GetGroupMembersAsync("Sup.TechnicalInquiry.Engineering", cn);

				if (!engineeringMembers.Any())
					return;

				var title = "ویرایش درخواست استعلام فنی";
				var body = await BuildNotificationBody(inquiry, "ویرایش شده", "#e65100", cn);

				foreach (var member in engineeringMembers)
				{
					await _notificationService.CreateAndSendAsync(new Notification
					{
						Type = NotificationType.Appliaction,
						Title = title,
						Body = body,
						EntityId = inquiry.Id.Value,
						OwnerId = (long)member.UserId,
						ViewPath = $"/Panel/TechnicalInquiry/Edit?id={inquiry.Id}",
						IsRead = false
					});
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error sending notification: {ex.Message}");
			}
		}

		private async Task CreateManagerApprovedNotification(TechnicalInquiry inquiry, CancellationToken cn)
		{
			try
			{
				var supplyMembers = await _notificationGroupService.GetGroupMembersAsync("Sup.TechnicalInquiry.Supply", cn);

				if (!supplyMembers.Any())
					return;

				var title = "تایید مدیر پروژه - استعلام فنی";
				var body = await BuildNotificationBody(inquiry, "تایید توسط مدیر پروژه", "#2e7d32", cn);

				foreach (var member in supplyMembers)
				{
					await _notificationService.CreateAndSendAsync(new Notification
					{
						Type = NotificationType.Appliaction,
						Title = title,
						Body = body,
						EntityId = inquiry.Id.Value,
						OwnerId = (long)member.UserId,
						ViewPath = $"/Panel/TechnicalInquiry/Edit?id={inquiry.Id}",
						IsRead = false
					});
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error sending notification: {ex.Message}");
			}
		}

		private async Task CreateManagerRejectNotification(TechnicalInquiry inquiry, CancellationToken cn)
		{
			try
			{
				var engineeringMembers = await _notificationGroupService
				    .GetGroupMembersAsync("Sup.TechnicalInquiry.Engineering", cn);

				if (!engineeringMembers.Any())
					return;

				var title = "رد مدیر پروژه - استعلام فنی";
				var body = await BuildNotificationBody(inquiry, "رد توسط مدیر پروژه", "#c62828", cn);

				foreach (var member in engineeringMembers)
				{
					await _notificationService.CreateAndSendAsync(new Notification
					{
						Type = NotificationType.Appliaction,
						Title = title,
						Body = body,
						EntityId = inquiry.Id.Value,
						OwnerId = (long)member.UserId,
						ViewPath = $"/Panel/TechnicalInquiry/Edit?id={inquiry.Id}",
						IsRead = false
					});
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error sending notification: {ex.Message}");
			}
		}

		private async Task CreateSupplierRejectNotification(TechnicalInquiry inquiry, CancellationToken cn)
		{
			try
			{
				var engineeringMembers = await _notificationGroupService
				    .GetGroupMembersAsync("Sup.TechnicalInquiry.Engineering", cn);

				if (!engineeringMembers.Any())
					return;

				var title = "❌ رد توسط تامین‌کننده - استعلام فنی";
				var body = await BuildNotificationBody(inquiry, "رد توسط تامین‌کننده", "#c62828", cn);

				foreach (var member in engineeringMembers)
				{
					await _notificationService.CreateAndSendAsync(new Notification
					{
						Type = NotificationType.Appliaction,
						Title = title,
						Body = body,
						EntityId = inquiry.Id.Value,
						OwnerId = (long)member.UserId,
						ViewPath = $"/Panel/TechnicalInquiry/Edit?id={inquiry.Id}",
						IsRead = false
					});
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error sending notification: {ex.Message}");
			}
		}

		private async Task CreateIssueToVendorNotification(TechnicalInquiry inquiry, CancellationToken cn)
		{
			try
			{
				var supplyMembers = await _notificationGroupService
				    .GetGroupMembersAsync("Sup.TechnicalInquiry.Supply", cn);

				if (!supplyMembers.Any())
					return;

				var title = "صدور برای تامین‌کننده - استعلام فنی";
				var body = await BuildNotificationBody(inquiry, "صدور برای تامین‌کننده", "#1565c0", cn);

				foreach (var member in supplyMembers)
				{
					await _notificationService.CreateAndSendAsync(new Notification
					{
						Type = NotificationType.Appliaction,
						Title = title,
						Body = body,
						EntityId = inquiry.Id.Value,
						OwnerId = (long)member.UserId,
						ViewPath = $"/Panel/TechnicalInquiry/Edit?id={inquiry.Id}",
						IsRead = false
					});
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error sending notification: {ex.Message}");
			}
		}

		private async Task CreateReviewEngineeringNotification(TechnicalInquiry inquiry, CancellationToken cn)
		{
			try
			{
				var supplyMembers = await _notificationGroupService
				    .GetGroupMembersAsync("Sup.TechnicalInquiry.Supply", cn);

				if (!supplyMembers.Any())
					return;

				var title = "بررسی مجدد مهندسی - استعلام فنی";
				var body = await BuildNotificationBody(inquiry, "بررسی مجدد توسط مهندسی", "#1565c0", cn);

				foreach (var member in supplyMembers)
				{
					await _notificationService.CreateAndSendAsync(new Notification
					{
						Type = NotificationType.Appliaction,
						Title = title,
						Body = body,
						EntityId = inquiry.Id.Value,
						OwnerId = (long)member.UserId,
						ViewPath = $"/Panel/TechnicalInquiry/Edit?id={inquiry.Id}",
						IsRead = false
					});
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error sending notification: {ex.Message}");
			}
		}

		private async Task CreateShiraziMahmoudNotification(TechnicalInquiry inquiry, string action, CancellationToken cn)
		{
			try
			{
				var targetEmails = new List<string> { "Shirazi.Mahmoud@havayar.com" };

				var users = await _userService
				    .TableNoTracking
				    .Where(u => targetEmails.Contains(u.Email))
				    .ToListAsync(cn);

				if (!users.Any())
					return;

				var title = $"استعلام فنی و مالی - {action}";
				var body = await BuildNotificationBody(inquiry, action, "#1565c0", cn);

				foreach (var user in users)
				{
					await _unitOfWork.Repository<Notification>().AddAsync(new Notification
					{
						Type = NotificationType.Appliaction,
						Title = title,
						Body = body,
						EntityId = inquiry.Id.Value,
						OwnerId = user.Id!.Value,
						ViewPath = $"/Panel/TechnicalInquiry/Edit?id={inquiry.Id}",
						IsRead = false
					}, cn);
				}

				await _unitOfWork.SaveChangesAsync(cn);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error sending notification to Shirazi Mahmoud: {ex.Message}");
			}
		}

		private async Task<string> BuildNotificationBody(TechnicalInquiry inquiry, string action, string color, CancellationToken cn)
		{
			var lastComment = await _unitOfWork.Repository<TechnicalInquiryComment>()
			    .TableNoTracking
			    .Where(c => c.TechnicalInquiryId == inquiry.Id)
			    .OrderByDescending(c => c.CreatedOnMiladiDateTime)
			    .FirstOrDefaultAsync(cn);

			var sb = new StringBuilder();
			sb.AppendLine($@"
            <div style='direction:rtl;text-align:right;font-family:tahoma;font-size:11pt;padding:10px;'>
             <h3 style='color:{color};'>استعلام فنی و مالی - {action}</h3>
            <table style='border-collapse:collapse;width:100%;margin-top:10px;'>
                <tr>
                    <td style='padding:5px;border:1px solid #ddd;'><strong>شماره درخواست:</strong></td>
                    <td style='padding:5px;border:1px solid #ddd;'>{inquiry.Id}</td>
                </tr>
                <tr>
                    <td style='padding:5px;border:1px solid #ddd;'><strong>عنوان پروژه:</strong></td>
                    <td style='padding:5px;border:1px solid #ddd;'>{inquiry.ProjectTitle ?? "-"}</td>
                </tr>
                <tr>
                    <td style='padding:5px;border:1px solid #ddd;'><strong>تاریخ نیاز:</strong></td>
                    <td style='padding:5px;border:1px solid #ddd;'>{inquiry.NeedShamsiDate ?? "-"}</td>
                </tr>
                <tr>
                    <td style='padding:5px;border:1px solid #ddd;'><strong>وضعیت:</strong></td>
                    <td style='padding:5px;border:1px solid #ddd;'>{inquiry.LastStatus.ToDisplay()}</td>
                </tr>
          ");

			if (lastComment != null && !string.IsNullOrEmpty(lastComment.Comment))
			{
				sb.AppendLine($@"
                <tr>
                    <td style='padding:5px;border:1px solid #ddd;'><strong>آخرین توضیحات:</strong></td>
                    <td style='padding:5px;border:1px solid #ddd;'>{lastComment.Comment}</td>
                </tr>
        ");
			}

			sb.AppendLine($@"
            </table>
            <p style='margin-top:15px;'>
                <a href='/Panel/TechnicalInquiry/Edit?id={inquiry.Id}' style='background:{color};color:white;padding:8px 15px;text-decoration:none;border-radius:5px;'>
                    مشاهده و بررسی
                </a>
            </p>
        </div>
    ");

			return sb.ToString();
		}

		#endregion

		#region File Download

		[HttpGet("[action]")]
		public async Task<IActionResult> DownloadImage(long? datasheetFileId, long? mapFileId, long? specFileId, long? tcFileId, long? otherFileId)
		{
			var id = datasheetFileId ?? mapFileId ?? specFileId ?? tcFileId ?? otherFileId;

			if (id == null || id == 0)
				return BadRequest("شناسه فایل ارسال نشده است");

			FileEntity? fileEntity = null;

			if (datasheetFileId.HasValue)
			{
				fileEntity = await _unitOfWork.Repository<FileEntity>()
				    .TableNoTracking
				    .FirstOrDefaultAsync(f => f.Id == datasheetFileId.Value);
			}
			else if (mapFileId.HasValue)
			{
				fileEntity = await _unitOfWork.Repository<FileEntity>()
				    .TableNoTracking
				    .FirstOrDefaultAsync(f => f.Id == mapFileId.Value);
			}
			else if (specFileId.HasValue)
			{
				fileEntity = await _unitOfWork.Repository<FileEntity>()
				    .TableNoTracking
				    .FirstOrDefaultAsync(f => f.Id == specFileId.Value);
			}
			else if (tcFileId.HasValue)
			{
				fileEntity = await _unitOfWork.Repository<FileEntity>()
				    .TableNoTracking
				    .FirstOrDefaultAsync(f => f.Id == tcFileId.Value);
			}
			else if (otherFileId.HasValue)
			{
				fileEntity = await _unitOfWork.Repository<FileEntity>()
				    .TableNoTracking
				    .FirstOrDefaultAsync(f => f.Id == otherFileId.Value);
			}

			if (fileEntity == null)
				return NotFound("فایل یافت نشد");

			var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", fileEntity.PhysicalPath ?? "");

			if (!System.IO.File.Exists(filePath))
				return NotFound("فایل در سرور یافت نشد");

			var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
			return File(fileBytes, fileEntity.ContentType ?? "application/octet-stream", fileEntity.OriginalName);
		}

		#endregion

		#region Comment Operations

		[HttpGet("[action]")]
		public IActionResult AddCommentPartial(long technicalInquiryId)
		{
			ViewBag.TechnicalInquiryId = technicalInquiryId;
			return PartialView(@"\Views\Panel\Inq\TechnicalInquiry\_AddCommentPartial.cshtml");
		}

		[HttpPost("[action]")]
		public async Task<IActionResult> SaveComment(SaveCommentRequest request, CancellationToken cn)
		{
			try
			{
				var technicalInquiry = await _unitOfWork.Repository<TechnicalInquiry>()
				    .Table
				    .FirstOrDefaultAsync(x => x.Id == request.TechnicalInquiryId, cn);

				if (technicalInquiry == null)
					return BadRequest("استعلام فنی یافت نشد");

				var comment = new TechnicalInquiryComment
				{
					TechnicalInquiryId = request.TechnicalInquiryId,
					Comment = request.CommentValue,
					LastStatus = technicalInquiry.LastStatus,
					CreatedById = CurrentUserId,
					CreatedByName = CurrentUserFullName,
					CreatedOnMiladiDateTime = DateTime.Now,
					CreatedOnShamsiDateTime = DateTime.Now.ToShamsiDateTime()
				};

				await _unitOfWork.Repository<TechnicalInquiryComment>().AddAsync(comment, cn);
				await _unitOfWork.SaveChangesAsync(cn);

				await CreateCommentNotification(technicalInquiry, request.CommentValue, cn);

				return Ok(comment);
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در ذخیره کامنت: " + ex.Message);
			}
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> CommentListPartial(long technicalInquiryId, CancellationToken cn)
		{
			var comments = await _unitOfWork.Repository<TechnicalInquiryComment>()
			    .TableNoTracking
			    .Where(c => c.TechnicalInquiryId == technicalInquiryId)
			    .OrderByDescending(c => c.CreatedOnMiladiDateTime)
			    .ToListAsync(cn);

			ViewBag.TechnicalInquiryId = technicalInquiryId;
			return PartialView(@"\Views\Panel\Inq\TechnicalInquiry\_CommentListPartial.cshtml", comments);
		}

		private async Task CreateCommentNotification(TechnicalInquiry inquiry, string? commentText, CancellationToken cn)
		{
			try
			{
				var engineeringMembers = await _notificationGroupService
				    .GetGroupMembersAsync("Sup.TechnicalInquiry.Engineering", cn);

				var supplyMembers = await _notificationGroupService
				    .GetGroupMembersAsync("Sup.TechnicalInquiry.Supply", cn);

				var allMembers = engineeringMembers.Concat(supplyMembers)
				    .GroupBy(x => x.UserId)
				    .Select(g => g.First())
				    .ToList();

				if (!allMembers.Any())
					return;

				var title = "کامنت جدید - استعلام فنی";
				var body = $@"
                    <div style='direction:rtl;text-align:right;font-family:tahoma;font-size:11pt;padding:10px;'>
                        <h3 style='color:#1565c0;'>💬 کامنت جدید</h3>
                        <p>کاربر <strong>{CurrentUserFullName}</strong> یک کامنت جدید ثبت کرد</p>
                        <table style='border-collapse:collapse;width:100%;margin-top:10px;'>
                            <tr>
                                <td style='padding:5px;border:1px solid #ddd;'><strong>شماره درخواست:</strong></td>
                                <td style='padding:5px;border:1px solid #ddd;'>{inquiry.Id}</td>
                            </tr>
                            <tr>
                                <td style='padding:5px;border:1px solid #ddd;'><strong>متن کامنت:</strong></td>
                                <td style='padding:5px;border:1px solid #ddd;'>{commentText}</td>
                            </tr>
                            <tr>
                                <td style='padding:5px;border:1px solid #ddd;'><strong>تاریخ:</strong></td>
                                <td style='padding:5px;border:1px solid #ddd;'>{inquiry.CreatedOnShamsiDateTime}</td>
                            </tr>
                        </table>
                        <p style='margin-top:15px;'>
                            <a href='/Panel/TechnicalInquiry/Edit?id={inquiry.Id}' style='background:#1565c0;color:white;padding:8px 15px;text-decoration:none;border-radius:5px;'>
                                مشاهده و پاسخ
                            </a>
                        </p>
                    </div>
                ";

				foreach (var member in allMembers)
				{
					await _notificationService.CreateAndSendAsync(new Notification
					{
						Type = NotificationType.Appliaction,
						Title = title,
						Body = body,
						EntityId = inquiry.Id.Value,
						OwnerId = (long)member.UserId,
						ViewPath = $"/Panel/TechnicalInquiry/Edit?id={inquiry.Id}",
						IsRead = false
					});
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error sending comment notification: {ex.Message}");
			}
		}

		#endregion

		#region Email Methods

		private async Task SendEmailToSupplyGroup(TechnicalInquiry inquiry, string action, string? comment = null, CancellationToken cn = default)
		{
			try
			{
				var targetEmails = new List<string>
				   {
					  "sabermanesh.s@havayar.com",
					  "bagheri.h@havayar.com",
					  "yaltaghian.f@havayar.com",
					  "sharifi.b@havayar.com",
					  "mohammadzadeh.m@havayar.com",
					  "sohrabi.za@havayar.com",
					  "abdollahi.a@havayar.com",
					  "sarmadi.p@havayar.com",
					  "zare.m@havayar.com",
					  "dordab.y@havayar.com",
					  "behvandi.m@havayar.com",
					  "gholamifard.m@havayar.com",
					  "momeni.a@havayar.com",
					  "ashrafi.m@havayar.com",
					  "hosseini.h@havayar.com",
					  "shahpordeli.s@havayar.com",
					  "mirlohi.a@havayar.com",
					  "banafshechin.y@havayar.com",
					  "rajablou.a@havayar.com",
					  "zabihian.s@havayar.com",
					  "razaghmanesh.z@havayar.com",
					  "zamani.ar@havayar.com",
					  "parhizkari.s@havayar.com"
				   };


				var distinctEmails = targetEmails
				    .Select(e => e.Trim().ToLower())
				    .Distinct()
				    .ToList();
				var existingUsers = await _userService
				    .TableNoTracking
				    .Where(u => distinctEmails.Contains(u.Email.ToLower()))
				    .ToListAsync(cn);

				var existingEmails = existingUsers.Select(u => u.Email.ToLower()).ToList();
				var notFoundEmails = distinctEmails.Except(existingEmails).ToList();



				var title = $"استعلام فنی - {action} - شماره {inquiry.Id}";
				var body = await BuildEmailBody(inquiry, action, comment);

				foreach (var user in existingUsers)
				{
					await _unitOfWork.Repository<Notification>().AddAsync(new Notification
					{
						Type = NotificationType.Appliaction,
						Title = title,
						Body = body,
						EntityId = inquiry.Id.Value,
						OwnerId = user.Id!.Value,
						ViewPath = $"/Panel/TechnicalInquiry/Edit?id={inquiry.Id}",
						IsRead = false
					}, cn);
				}

				await _unitOfWork.SaveChangesAsync(cn);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error sending email to Supply group: {ex.Message}");
			}
		}

		private async Task SendEmailToFinancialGroup(TechnicalInquiry inquiry, string action, string? comment = null, CancellationToken cn = default)
		{
			try
			{
				var targetEmails = new List<string>
						   {
							  "ghiyabaklou.e@havayar.com",
							  "bayat.a@havayar.com",
							  "naghdi.f@havayar.com"
						   };

				var distinctEmails = targetEmails
				    .Select(e => e.Trim().ToLower())
				    .Distinct()
				    .ToList();

				var existingUsers = await _userService
				    .TableNoTracking
				    .Where(u => distinctEmails.Contains(u.Email.ToLower()))
				    .ToListAsync(cn);

				var existingEmails = existingUsers.Select(u => u.Email.ToLower()).ToList();
				var notFoundEmails = distinctEmails.Except(existingEmails).ToList();

				var title = $"استعلام مالی - {action} - شماره {inquiry.Id}";
				var body = await BuildEmailBody(inquiry, action, comment);

				foreach (var user in existingUsers)
				{
					await _unitOfWork.Repository<Notification>().AddAsync(new Notification
					{
						Type = NotificationType.Appliaction,
						Title = title,
						Body = body,
						EntityId = inquiry.Id.Value,
						OwnerId = user.Id!.Value,
						ViewPath = $"/Panel/TechnicalInquiry/Edit?id={inquiry.Id}",
						IsRead = false
					}, cn);
				}

				await _unitOfWork.SaveChangesAsync(cn);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error sending email to Financial group: {ex.Message}");
			}
		}

		private async Task<string> BuildEmailBody(TechnicalInquiry inquiry, string action, string? comment = null)
		{
			var sb = new StringBuilder();

			sb.AppendLine("<div style='text-align:center;direction:rtl'>");
			sb.AppendLine("<table border='1' cellspacing='0' cellpadding='5' style='text-align:right; direction: rtl' width='100%'>");
			sb.AppendLine("<tr style='width: 7.0in; background: #000aa0'>");
			sb.AppendLine("<td colspan='2'>");
			sb.AppendLine("<div style='font-size:14.0pt;font-family:Zar;color:#FFFFFF;text-align:center;direction:rtl'>");
			sb.AppendLine("گروه صنعتی هوایار");
			sb.AppendLine("</div>");
			sb.AppendLine("</td>");
			sb.AppendLine("</tr>");

			sb.AppendLine("<tr>");
			sb.AppendLine("<td colspan='2'>");
			sb.AppendLine("<div style='font-size:14.0pt;font-family:Zar;color:#1F497D;text-align:right;direction:rtl'>");
			sb.AppendLine("با سلام و احترام <br />");

			sb.AppendLine($"<p>{action}</p>");

			sb.AppendLine("<ul>");
			sb.AppendLine($"<li>شماره درخواست: <strong>{inquiry.Id}</strong></li>");
			sb.AppendLine($"<li>عنوان پروژه: <strong>{inquiry.ProjectTitle ?? "-"}</strong></li>");
			sb.AppendLine($"<li>شرح کالا: <strong>{inquiry.Part?.Name ?? "-"}</strong></li>");
			sb.AppendLine($"<li>تاریخ نیاز: <strong>{inquiry.NeedShamsiDate ?? "-"}</strong></li>");
			sb.AppendLine($"<li>وضعیت فعلی: <strong>{inquiry.LastStatus.ToDisplay()}</strong></li>");

			if (!string.IsNullOrEmpty(comment))
			{
				sb.AppendLine($"<li>توضیحات: <strong>{comment}</strong></li>");
			}

			sb.AppendLine("</ul>");

			sb.AppendLine("<br />");
			sb.AppendLine($"<strong>جهت مشاهده فایل‌های مربوطه به سامانه مراجعه فرمایید</strong>");
			sb.AppendLine("<br />");
			sb.AppendLine($"<a href='/Panel/TechnicalInquiry/Edit?id={inquiry.Id}' style='background:#1a73e8;color:white;padding:8px 15px;text-decoration:none;border-radius:5px;'>");
			sb.AppendLine("مشاهده درخواست در سامانه");
			sb.AppendLine("</a>");

			sb.AppendLine("</div>");
			sb.AppendLine("</td>");
			sb.AppendLine("</tr>");
			sb.AppendLine("</table>");
			sb.AppendLine("</div>");

			return sb.ToString();
		}

		#endregion

		public static class TechnicalInquiryHelper
		{
			public static bool CanSaveContractorDescription(TechnicalInquiryStatusEnum status)
			{
				return status == TechnicalInquiryStatusEnum.ManagerApproved;
			}

			public static bool ShouldClearContractorDescription(TechnicalInquiryStatusEnum status)
			{
				return status != TechnicalInquiryStatusEnum.ManagerApproved;
			}
		}
	}

	public class SaveCommentRequest
	{
		public long TechnicalInquiryId { get; set; }
		public string? CommentValue { get; set; }
	}
}