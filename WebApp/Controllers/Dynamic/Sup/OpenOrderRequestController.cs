using Aspose.Cells;
using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Data.SystemAuth;
using Entities.App.Edms;
using Entities.App.Edms.Enums;
using Entities.App.Hrm;
using Entities.App.Pln;
using Entities.App.Sup;
using Entities.App.Sup.Enums;
using Entities.Auth;
using Entities.Base.DataTable;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Auth;
using Services.NotificationGroupServices;
using Services.NotificationServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Sup/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("درخواست های باز", typeof(OpenOrderRequest))]
	public class OpenOrderRequestController(
		IUnitOfWork unitOfWork,
		IWebHostEnvironment _webHostEnvironment,
		IUserService userService,
		INotificationGroupService _notificationGroupService,
		INotificationService notificationService
		) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(OpenOrderRequest openOrderRequest, CancellationToken cn)
		{
			if (openOrderRequest.Id == null || openOrderRequest.Id == 0)
			{
				return await Add(openOrderRequest, cn);
			}
			var exist = await unitOfWork.Repository<OpenOrderRequest>().TableNoTracking.AnyAsync(c => c.Id == openOrderRequest.Id);
			if (exist)
			{
				return await Update(openOrderRequest, cn);
			}
			return await Add(openOrderRequest, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(OpenOrderRequest openOrderRequest, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<OpenOrderRequest>().SaveAsync(openOrderRequest, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(OpenOrderRequest openOrderRequest, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<OpenOrderRequest>().UpdateAsync(openOrderRequest, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<OpenOrderRequest>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<OpenOrderRequest>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<OpenOrderRequest>().TableNoTracking
					.Include(c => c.Dl)
					.Include(c => c.Part)
					.Include(c => c.EngineeringAcceptUser)
					.Include(c => c.SalesUnitConfirmationUser)
					.Include(c => c.ProductionOrder)
					.Include(c => c.SalesUnitSalesExpert)
					.Include(c => c.SalesUnitSalesManager)
					.Include(c => c.SalesUnitProjectManager)
					.Include(c => c.RelatedPart)
					.Include(c => c.ManCompany)
					.Include(c => c.Attachments)
					.Include(c => c.Comments)
					.Include(c=>c.Attachments)
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Sup\OpenOrderRequest\Edit.cshtml", entity);
			}
			var newEntity = new OpenOrderRequest();
			return View(@"\Views\Panel\Sup\OpenOrderRequest\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var newEntity = new OpenOrderRequest();
			return View(@"\Views\Panel\Sup\OpenOrderRequest\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Sup\OpenOrderRequest\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<OpenOrderRequest>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<OpenOrderRequest>().FetchDataAsync(request, cn));
		}

		[HttpGet("[action]")]
		public IActionResult OpenOrderRequestAttachmentPartial()
		{
			return PartialView(@"\Views\Panel\Sup\OpenOrderRequest\_OpenOrderRequestAttachmentPartial.cshtml");
		}

		[HttpGet("[action]")]
		public IActionResult OpenOrderRequestCommentPartial()
		{
			return PartialView(@"\Views\Panel\Sup\OpenOrderRequest\_OpenOrderRequestCommentPartial.cshtml");
		}

		#region Comment Operations

		[HttpGet("[action]")]
		public IActionResult AddCommentPartial(long openOrderRequestId)
		{
			ViewBag.OpenOrderRequestId = openOrderRequestId;
			return PartialView(@"\Views\Panel\Sup\OpenOrderRequest\_AddCommentPartial.cshtml");
		}

		[HttpPost("[action]")]
		public async Task<IActionResult> SaveComment(SaveCommentRequest request, CancellationToken cn)
		{
			try
			{
				var openOrderRequest = await unitOfWork
					.Repository<OpenOrderRequest>()
					.TableNoTracking
					.FirstOrDefaultAsync(x => x.Id == request.OpenOrderRequestId, cn);

				if (openOrderRequest == null)
					return BadRequest("درخواست باز یافت نشد");

				var now = DateTime.Now;
				var comment = new OpenOrderRequestComment
				{
					OpenOrderRequestId = request.OpenOrderRequestId,
					ShamsiDate = request.CommentShamsiDate,
					MiladiDate = string.IsNullOrEmpty(request.CommentShamsiDate) ? null : request.CommentShamsiDate.ToMiladiDate(),
					CommentValue = request.CommentValue
				};

				await unitOfWork.Repository<OpenOrderRequestComment>().AddAsync(comment, cn);
				await unitOfWork.SaveChangesAsync(cn);

				// Create notification for requested personnel
				await CreateCommentNotification(openOrderRequest, comment, cn);

				return Ok(openOrderRequest);
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در ذخیره کامنت: " + ex.Message);
			}
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> CommentListPartial(long openOrderRequestId, CancellationToken cn)
		{
			var comments = await unitOfWork.Repository<OpenOrderRequestComment>()
				.TableNoTracking
				.Include(c => c.StopOperator)
				.Where(c => c.OpenOrderRequestId == openOrderRequestId)
				.OrderByDescending(c => c.CreatedOnMiladiDateTime)
				.ToListAsync(cn);

			ViewBag.OpenOrderRequestId = openOrderRequestId;
			return PartialView(@"\Views\Panel\Sup\OpenOrderRequest\_CommentListPartial.cshtml", comments);
		}

		#endregion

		#region Engineering Accept

		[HttpPost("[action]/{id}")]
		public async Task<IActionResult> EngineeringAccept(long id, CancellationToken cn)
		{
			try
			{

				//Seed Role => Sup.OpenOrderRequest.EngineeringAccept => تامین و خرید -درخواست های باز - تایید مهندسی 
				//برسی داشتن دسترسی برای تایید مهندسی 
				if (!CurrentUserHasAnyRole("Sup.OpenOrderRequest.EngineeringAccept")) 
					return Unauthorized("شما دسترسی تایید مهندسی را ندارید");

				var openOrderRequest = await unitOfWork.Repository<OpenOrderRequest>()
					.Table
					.Include(o => o.Part)
				 
					.FirstOrDefaultAsync(x => x.Id == id, cn);

				if (openOrderRequest == null)
					return BadRequest("درخواست باز یافت نشد");

				if (openOrderRequest.EngineeringAccept)
					return BadRequest("این درخواست قبلاً تایید مهندسی شده است");

				if (openOrderRequest.IsStop)
					return BadRequest("درخواست متوقف شده قابل تایید نیست");

				var now = DateTime.Now;
				var currentUserId = CurrentUserId;

				openOrderRequest.EngineeringAccept = true;
				openOrderRequest.EngineeringAcceptUserId = currentUserId;
				openOrderRequest.EngineeringAcceptShamsiDateTime = now.ToShamsiDateTime();
				openOrderRequest.EngineeringConfirmationMiladiDateTime = now;

				var leadTime = await unitOfWork.Repository<LeadTime>().TableNoTracking
					.OrderByDescending(c=>c.CreatedOnMiladiDateTime)
					.Select(c=>new { c.LeadTimeDay, c.PartId  , c.Id})
					.FirstOrDefaultAsync(c => c.PartId == openOrderRequest.PartId);

				if(leadTime != null)
				{
				 
					openOrderRequest.SupplyMiladiDate = now.AddDays(leadTime.LeadTimeDay);
					openOrderRequest.SupplyShamsiDate = openOrderRequest.SupplyMiladiDate.Value.ToShamsiDate();

				}

				await unitOfWork.Repository<OpenOrderRequest>().UpdateAsync(openOrderRequest, cn, true);

				// ایجاد Notification برای کاربران مربوط
				await CreateEngineeringAcceptNotification(openOrderRequest.Id, cn);

				return Ok(openOrderRequest);
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در تایید مهندسی: " + ex.Message);
			}
		}

		#endregion

		#region Sales/Project Accept

		[HttpGet("[action]")]
		public IActionResult SalesOrProjectAcceptPartial(long openOrderRequestId)
		{
			ViewBag.OpenOrderRequestId = openOrderRequestId;
			return PartialView(@"\Views\Panel\Sup\OpenOrderRequest\_SalesOrProjectAcceptPartial.cshtml");
		}

		[HttpPost("[action]")]
		public async Task<IActionResult> SalesOrProjectAccept(SalesOrProjectAcceptRequest request, CancellationToken cn)
		{
			try
			{
				//Seed Role => Sup.OpenOrderRequest.SalesOrProjectAccept => تامین و خرید -درخواست های باز - تایید فروش/پروژه 
				//برسی داشتن دسترسی برای تایید فروش/پروژه 
				if (!CurrentUserHasAnyRole("Sup.OpenOrderRequest.SalesOrProjectAccept"))
					return Unauthorized("شما دسترسی تایید فروش/پروژه را ندارید");


				 

				var openOrderRequest = await unitOfWork.Repository<OpenOrderRequest>()
					.Table.FirstOrDefaultAsync(x => x.Id == request.OpenOrderRequestId, cn);

				if (openOrderRequest == null)
					return BadRequest("درخواست باز یافت نشد");

				if (!openOrderRequest.EngineeringAccept)
					return BadRequest("ابتدا باید تایید مهندسی انجام شود");

				if (openOrderRequest.IsAcceptedAutomaticallyByEngineering)
					return BadRequest("این درخواست به صورت خودکار تایید مهندسی شده و نیاز به تایید واحد فروش/پروژه ندارد");

				if (openOrderRequest.HasSalesUnitConfirmation)
					return BadRequest("این درخواست قبلاً تایید شده است");

				var now = DateTime.Now;
				var currentUserId = CurrentUserId;

				openOrderRequest.HasSalesUnitConfirmation = true;
				openOrderRequest.HasSalesUnitPrimitiveApprove = true;
				openOrderRequest.SalesUnitConfirmationUserId = currentUserId;
				openOrderRequest.SalesUnitConfirmationMiladiDateTime = now;
				openOrderRequest.SalesUnitConfirmationShamsiDateTime = now.ToShamsiDateTime();
				openOrderRequest.SalesUnitConfirmationComment = request.SalesUnitConfirmationComment;

				await unitOfWork.Repository<OpenOrderRequest>().UpdateAsync(openOrderRequest, cn, true);

				// ثبت کامنت تایید
				var comment = new OpenOrderRequestComment
				{
					OpenOrderRequestId = request.OpenOrderRequestId,
					ShamsiDate = now.ToShamsiDate(),
					MiladiDate = now,
					HasSalesUnitConfirmation = true,
					SalesUnitConfirmationComment = "تایید درخواست باز توسط واحد پروژه/فروش",
					CommentValue = request.SalesUnitConfirmationComment
				};

				await unitOfWork.Repository<OpenOrderRequestComment>().AddAsync(comment, cn);
				await unitOfWork.SaveChangesAsync(cn);

				// TODO: ایجاد Notification - سیستم قدیم به تدارکات ایمیل می‌فرستاد
				await CreateSalesConfirmationNotification(openOrderRequest, cn);

				return Ok(openOrderRequest);
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در تایید: " + ex.Message);
			}
		}

		#endregion

		#region Stop Operations

		[HttpGet("[action]")]
		public async Task<IActionResult> StopRequestPartial(long openOrderRequestId, CancellationToken cn)
		{
			var openOrderRequest = await unitOfWork.Repository<OpenOrderRequest>()
				.TableNoTracking
				.FirstOrDefaultAsync(x => x.Id == openOrderRequestId, cn);

			return PartialView(@"\Views\Panel\Sup\OpenOrderRequest\_StopRequestPartial.cshtml", openOrderRequest);
		}

		[HttpPost("[action]")]
		public async Task<IActionResult> StopOperation(StopOperationRequest request, CancellationToken cn)
		{
			try
			{
				var openOrderRequest = await unitOfWork.Repository<OpenOrderRequest>()
					.Table
					.Include(o => o.Part)
					.Include(o => o.Comments)
					.FirstOrDefaultAsync(x => x.Id == request.OpenOrderRequestId, cn);

				if (openOrderRequest == null)
					return BadRequest("درخواست باز یافت نشد");

				// Validation
				var validationError = ValidateStopRequest(request, (int?)openOrderRequest.StopStatus ?? 0);
				if (!string.IsNullOrEmpty(validationError))
					return BadRequest(validationError);

				var now = DateTime.Now;
				var currentUserId = CurrentUserId;
				var currentStopStatus = (int?)openOrderRequest.StopStatus ?? 0;

				OpenOrderRequestComment comment;
				OpenOrderRequestStopStatusEnum nextStopStatus;

				// تعیین مرحله بعدی بر اساس وضعیت فعلی
				switch (currentStopStatus)
				{
					case 0: // Initial Stop
					case 2815: // Restart
						nextStopStatus = OpenOrderRequestStopStatusEnum.InitialSubmissionAndSendToStopBoard;
						
						comment = new OpenOrderRequestComment
						{
							OpenOrderRequestId = request.OpenOrderRequestId,
							ShamsiDate = now.ToShamsiDate(),
							MiladiDate = now,
							CommentValue = request.CommentValue,
							IsStop = true,
							StopType = request.StopTypeEnum.HasValue ? (OpenOrderRequestStopTypeEnum)request.StopTypeEnum.Value : null,
							StopOperatorId = request.StopOperatorId,
							BeneficiariesIds = request.BeneficiariesIds,
							AttachmentId = request.AttachmentId,
							ResponseDeadlineMiladiDate = now.AddDays(openOrderRequest.IsRoutineRequest ? 2 : 3),
						};
						comment.ResponseDeadlineShamsiDate = comment.ResponseDeadlineMiladiDate.Value.ToShamsiDate();

						openOrderRequest.IsStop = true;
						openOrderRequest.StopStatus = nextStopStatus;
						openOrderRequest.StopShamsiDate = now.ToShamsiDate();
						openOrderRequest.StopMiladiDate = now;

						await unitOfWork.Repository<OpenOrderRequestComment>().AddAsync(comment, cn);

						await CreateStopInitNotification(openOrderRequest, comment, cn);


						break;

					case 2807: // Step1_InitialAndSendToResponsible
					case 2813: // Step6_WaitingForProjectManager
					case 2816: // Step8_RegisterStopAgain
						// Operator Review
						nextStopStatus = DetermineNextStopStatusFromCheckingResult(request.StopCheckingResult);

						var lastComment = openOrderRequest.Comments
							.Where(c => c.IsStop)
							.OrderByDescending(c => c.Id)
							.FirstOrDefault();

						if (lastComment != null)
						{
							if (currentStopStatus == 2813) // If waiting for project manager, create new comment
							{
								comment = new OpenOrderRequestComment
								{
									OpenOrderRequestId = request.OpenOrderRequestId,
									ShamsiDate = now.ToShamsiDate(),
									MiladiDate = now,
									CommentValue = "ادامه روند توقف پس از اعمال نظر مدیر پروژه / کارفرما",
									IsStop = true,
									StopType = lastComment.StopType,
									StopOperatorId = lastComment.StopOperatorId,
									BeneficiariesIds = lastComment.BeneficiariesIds,
									StopCheckingResult = request.StopCheckingResult,
									StopCheckingMiladiDateTime = now,
									StopCheckingShamsiDateTime = now.ToShamsiDateTime(),
									StopCheckingComment = request.StopCheckingComment,
									StopCheckingDelayReasonText = request.StopCheckingDelayReasonText,
									IsNeedToUpdateBom = request.IsNeedToUpdateBom,
									IsNeedToDeleteBom = request.IsNeedToDeleteBom,
									ProjectManagerApproximateCommentShamsiDate = request.ProjectManagerApproximateCommentShamsiDate,
									ProjectManagerApproximateCommentMiladiDate = string.IsNullOrEmpty(request.ProjectManagerApproximateCommentShamsiDate) 
										? null : request.ProjectManagerApproximateCommentShamsiDate.ToMiladiDate(),
									AlternativeAttachmentId = request.AlternativeAttachmentId
								};

								await unitOfWork.Repository<OpenOrderRequestComment>().AddAsync(comment, cn);
							}
							else
							{
								lastComment.StopCheckingResult = request.StopCheckingResult;
								lastComment.StopCheckingMiladiDateTime = now;
								lastComment.StopCheckingShamsiDateTime = now.ToShamsiDateTime();
								lastComment.StopCheckingComment = request.StopCheckingComment;
								lastComment.StopCheckingDelayReasonText = request.StopCheckingDelayReasonText;
								lastComment.IsNeedToUpdateBom = request.IsNeedToUpdateBom;
								lastComment.IsNeedToDeleteBom = request.IsNeedToDeleteBom;
								lastComment.ProjectManagerApproximateCommentShamsiDate = request.ProjectManagerApproximateCommentShamsiDate;
								lastComment.ProjectManagerApproximateCommentMiladiDate = string.IsNullOrEmpty(request.ProjectManagerApproximateCommentShamsiDate)
									? null : request.ProjectManagerApproximateCommentShamsiDate.ToMiladiDate();
								lastComment.AlternativeAttachmentId = request.AlternativeAttachmentId;

								await unitOfWork.Repository<OpenOrderRequestComment>().UpdateAsync(lastComment, cn, false);
								comment = lastComment;
							}
						}
						else
						{
							return BadRequest("کامنت توقف یافت نشد");
						}

						openOrderRequest.StopStatus = nextStopStatus;
						await unitOfWork.Repository<OpenOrderRequest>().UpdateAsync(openOrderRequest, cn, true);

						// Send notification
						await CreateStopOperatorReviewNotification(openOrderRequest, comment, cn);

						break;

					case 2808: // Step2_NeedToRestartRequest
					case 2810: // Step3_NeedToEdit
					case 2811: // Step4_NeedToEditDocuments
					case 2812: // Step5_DeleteIndustrialRequest
					case 2821: // Step10_SendToSupplyCartable
						// Requester Decision
						nextStopStatus = DetermineNextStopStatusFromCheckingStatus(request.StopCheckingStatus);

						var currentComment = openOrderRequest.Comments
							.Where(c => c.IsStop)
							.OrderByDescending(c => c.Id)
							.FirstOrDefault();

						if (currentComment == null)
							return BadRequest("کامنت توقف یافت نشد");

						if (nextStopStatus == OpenOrderRequestStopStatusEnum.CheckStopFactorReRegisterStop)
						{
							// Create new stop comment
							comment = new OpenOrderRequestComment
							{
								OpenOrderRequestId = request.OpenOrderRequestId,
								ShamsiDate = now.ToShamsiDate(),
								MiladiDate = now,
								CommentValue = request.ReStopCommentValue ?? request.CommentValue,
								IsStop = true,
								StopType = request.ReStopTypeEnum.HasValue ? (OpenOrderRequestStopTypeEnum)request.ReStopTypeEnum.Value : null,
								StopOperatorId = request.ReStopOperatorId ?? request.StopOperatorId,
								BeneficiariesIds = currentComment.BeneficiariesIds,
								StopCheckingStatus = request.StopCheckingStatus,
								StopCheckingStatusComment = request.StopCheckingStatusComment,
								AttachmentId = request.AttachmentId
							};

							await unitOfWork.Repository<OpenOrderRequestComment>().AddAsync(comment, cn);
						}
						else
						{
							currentComment.StopCheckingStatus = request.StopCheckingStatus;
							currentComment.StopCheckingStatusComment = request.StopCheckingStatusComment;
							await unitOfWork.Repository<OpenOrderRequestComment>().UpdateAsync(currentComment, cn, false);
							comment = currentComment;
						}

						if (nextStopStatus == OpenOrderRequestStopStatusEnum.RestartStopCauseCheck)
						{
							// Restart request
							openOrderRequest.IsStop = false;
							
							var restartComment = new OpenOrderRequestComment
							{
								OpenOrderRequestId = request.OpenOrderRequestId,
								ShamsiDate = now.ToShamsiDate(),
								MiladiDate = now,
								CommentValue = $"درخواست توسط کاربر راه اندازی شد",
								IsStop = false,
								IsLaunched = true
							};
							await unitOfWork.Repository<OpenOrderRequestComment>().AddAsync(restartComment, cn);
						}

						openOrderRequest.StopStatus = nextStopStatus;
						await unitOfWork.Repository<OpenOrderRequest>().UpdateAsync(openOrderRequest, cn, true);

						await CreateStopRequesterDecisionNotification(openOrderRequest, comment, cn);

						break;

					default:
						return BadRequest("وضعیت توقف نامعتبر است");
				}

				await unitOfWork.SaveChangesAsync(cn);

				return Ok(openOrderRequest);
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در ثبت توقف: " + ex.Message);
			}
		}

		#endregion

		#region Triggering (Start)

		[HttpPost("[action]/{id}")]
		public async Task<IActionResult> Triggering(long id, CancellationToken cn)
		{
			try
			{
				// TODO: بررسی دسترسی
				var openOrderRequest = await unitOfWork.Repository<OpenOrderRequest>()
					.Table.FirstOrDefaultAsync(x => x.Id == id, cn);

				if (openOrderRequest == null)
					return BadRequest("درخواست باز یافت نشد");

				var now = DateTime.Now;
				
				openOrderRequest.IsStop = false;
				openOrderRequest.Status = "راه اندازی شده";

				await unitOfWork.Repository<OpenOrderRequest>().UpdateAsync(openOrderRequest, cn, false);

				// ثبت کامنت
				var comment = new OpenOrderRequestComment
				{
					OpenOrderRequestId = id,
					ShamsiDate = now.ToShamsiDate(),
					MiladiDate = now,
					CommentValue = "درخواست راه اندازی شد",
					IsStop = false,
					IsLaunched = true
				};

				await unitOfWork.Repository<OpenOrderRequestComment>().AddAsync(comment, cn);
				await unitOfWork.SaveChangesAsync(cn);

				await CreateTriggeringNotification(openOrderRequest, cn);

				return Ok(openOrderRequest);
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در راه‌اندازی: " + ex.Message);
			}
		}

		#endregion

		#region InWay and StatusInquiry

		[HttpPost("[action]/{id}")]
		public async Task<IActionResult> SetInWayStatus(long id, CancellationToken cn)
		{
			try
			{
				// TODO: بررسی دسترسی ارسال
				var openOrderRequest = await unitOfWork.Repository<OpenOrderRequest>()
					.Table.FirstOrDefaultAsync(x => x.Id == id, cn);

				if (openOrderRequest == null)
					return BadRequest("درخواست باز یافت نشد");

				openOrderRequest.Status = "در راه";

				await unitOfWork.Repository<OpenOrderRequest>().UpdateAsync(openOrderRequest, cn, true);

				return Ok(openOrderRequest);
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در تغییر وضعیت: " + ex.Message);
			}
		}

		[HttpPost("[action]/{id}")]
		public async Task<IActionResult> StatusInquiry(long id, CancellationToken cn)
		{
			try
			{
				 
				var openOrderRequest = await unitOfWork.Repository<OpenOrderRequest>()
					.Table.FirstOrDefaultAsync(x => x.Id == id, cn);

				if (openOrderRequest == null)
					return BadRequest("درخواست باز یافت نشد");

				openOrderRequest.Status = "در حال استعلام";

				await unitOfWork.Repository<OpenOrderRequest>().UpdateAsync(openOrderRequest, cn, true);

				return Ok(openOrderRequest);
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در استعلام: " + ex.Message);
			}
		}

		#endregion

		#region Terminate

		[HttpPost("[action]/{id}")]
		public async Task<IActionResult> Terminate(long id, CancellationToken cn)
		{
			try
			{
				// TODO: بررسی دسترسی خاتمه
				var openOrderRequest = await unitOfWork.Repository<OpenOrderRequest>()
					.Table.FirstOrDefaultAsync(x => x.Id == id, cn);

				if (openOrderRequest == null)
					return BadRequest("درخواست باز یافت نشد");

				var now = DateTime.Now;

				openOrderRequest.IsForceDeletedByUser = true;
				openOrderRequest.IsDeleted = true;
				openOrderRequest.IsDeletedMiladiDate = now;
				openOrderRequest.IsDeletedShamsiDate = now.ToShamsiDateTime();
				openOrderRequest.Comment += " || خاتمه توسط کاربر";

				await unitOfWork.Repository<OpenOrderRequest>().UpdateAsync(openOrderRequest, cn, false);

				// ثبت کامنت خاتمه
				var comment = new OpenOrderRequestComment
				{
					OpenOrderRequestId = id,
					ShamsiDate = now.ToShamsiDate(),
					MiladiDate = now,
					CommentValue = $"اختتام یافته در Portal توسط {CurrentUserId}"
				};

				await unitOfWork.Repository<OpenOrderRequestComment>().AddAsync(comment, cn);
				await unitOfWork.SaveChangesAsync(cn);

				return Ok(openOrderRequest);
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در خاتمه: " + ex.Message);
			}
		}

		#endregion

		#region Attachment Operations

		[HttpGet("[action]")]
		public async Task<IActionResult> AttachmentPartial(long openOrderRequestId, long? id, CancellationToken cn)
		{
			OpenOrderRequestAttachment? model = null;
			
			if (id.HasValue && id.Value > 0)
			{
				model = await unitOfWork.Repository<OpenOrderRequestAttachment>()
					.TableNoTracking
					.Include(a => a.Attachment)
					.FirstOrDefaultAsync(x => x.Id == id, cn);
			}

			ViewBag.OpenOrderRequestId = openOrderRequestId;
			return PartialView(@"\Views\Panel\Sup\OpenOrderRequest\_AttachmentPartial.cshtml", model);
		}

		[HttpPost("[action]")]
		public async Task<IActionResult> SaveAttachment(SaveAttachmentRequest request, CancellationToken cn)
		{
			try
			{
				var openOrderRequest = await unitOfWork.Repository<OpenOrderRequest>()
					.Table.FirstOrDefaultAsync(x => x.Id == request.OpenOrderRequestId, cn);

				if (openOrderRequest == null)
					return BadRequest("درخواست باز یافت نشد");

				OpenOrderRequestAttachment attachment;

				if (request.Id.HasValue && request.Id.Value > 0)
				{
					attachment = await unitOfWork.Repository<OpenOrderRequestAttachment>()
						.Table.FirstOrDefaultAsync(x => x.Id == request.Id, cn);
					
					if (attachment == null)
						return BadRequest("پیوست یافت نشد");

					attachment.FileType = request.FileType;
					attachment.Comment = request.Comment;
					attachment.AttachmentId = request.AttachmentId;

					await unitOfWork.Repository<OpenOrderRequestAttachment>().UpdateAsync(attachment, cn, true);
				}
				else
				{
					attachment = new OpenOrderRequestAttachment
					{
						OpenOrderRequestId = request.OpenOrderRequestId,
						FileType = request.FileType,
						Comment = request.Comment,
						AttachmentId = request.AttachmentId
					};

					await unitOfWork.Repository<OpenOrderRequestAttachment>().AddAsync(attachment, cn);
					await unitOfWork.SaveChangesAsync(cn);
				}

				// Mark as changed if has existing attachments
				openOrderRequest.Changed = true;
				await unitOfWork.Repository<OpenOrderRequest>().UpdateAsync(openOrderRequest, cn, true);

				// TODO: Send notification for new attachment (سیستم قدیم برای برخی نوع فایل‌ها به تولید ایمیل می‌فرستاد)
				var specialDocumentTypes = new[] { 
					OpenOrderRequestAttachmentFileTypeEnum.DataSheet, 
					OpenOrderRequestAttachmentFileTypeEnum.WiringDiagram, 
					OpenOrderRequestAttachmentFileTypeEnum.TechnicalDocuments 
				};
				
				if (request.FileType.HasValue && specialDocumentTypes.Contains(request.FileType.Value))
				{
					await CreateNewAttachmentNotification(openOrderRequest, attachment, cn);
				}

				return Ok(attachment);
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در ذخیره پیوست: " + ex.Message);
			}
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> AttachmentListPartial(long openOrderRequestId, CancellationToken cn)
		{
			var attachments = await unitOfWork.Repository<OpenOrderRequestAttachment>()
				.TableNoTracking
				.Include(a => a.Attachment)
				.Where(a => a.OpenOrderRequestId == openOrderRequestId)
				.OrderByDescending(a => a.CreatedOnMiladiDateTime)
				.ToListAsync(cn);

			ViewBag.OpenOrderRequestId = openOrderRequestId;
			return PartialView(@"\Views\Panel\Sup\OpenOrderRequest\_AttachmentListPartial.cshtml", attachments);
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> DownloadAttachment(long id, CancellationToken cn)
		{
			var attachment = await unitOfWork.Repository<OpenOrderRequestAttachment>()
				.TableNoTracking
				.Include(a => a.Attachment)
				.FirstOrDefaultAsync(x => x.Id == id, cn);

			if (attachment?.Attachment == null)
				return NotFound("فایل یافت نشد");

			var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", attachment.Attachment.PhysicalPath ?? "");
			
			if (!System.IO.File.Exists(filePath))
				return NotFound("فایل در مسیر ذخیره‌سازی یافت نشد");

			var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath, cn);
			return File(fileBytes, "application/octet-stream", attachment.Attachment.OriginalName);
		}

		[HttpPost("[action]")]
		public async Task<IActionResult> DeleteAttachment(long id, CancellationToken cn)
		{
			try
			{
				var attachment = await unitOfWork.Repository<OpenOrderRequestAttachment>()
					.Table
					.Include(a => a.Attachment)
					.FirstOrDefaultAsync(x => x.Id == id, cn);

				if (attachment == null)
					return BadRequest("پیوست یافت نشد");

				await unitOfWork.Repository<OpenOrderRequestAttachment>().DeleteAsync(attachment, cn, true);

				// TODO: Delete physical file if needed
				
				return Ok();
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در حذف پیوست: " + ex.Message);
			}
		}

		#endregion

		#region Helper Methods

		[HttpGet("[action]")]
		public async Task<IActionResult> GetBeneficiaries(CancellationToken cn)
		{
			var users = await userService
				.TableNoTracking
				.Where(u => u.IsActive == Entities.Base.IsActiveEnum.Active)
				.Select(u => new { id = u.Id, name = u.Name })
				.ToListAsync(cn);

			return Ok(users);
		}

		private OpenOrderRequestStopStatusEnum DetermineNextStopStatusFromCheckingResult(OpenOrderRequestCommentStopCheckingResultEnum? result)
		{
			return result switch
			{
				OpenOrderRequestCommentStopCheckingResultEnum.NeedsRestart => OpenOrderRequestStopStatusEnum.StopCauseRequiresRestart,
				OpenOrderRequestCommentStopCheckingResultEnum.NeedsBOMCorrection => OpenOrderRequestStopStatusEnum.CheckStopFactorNeedToFixBom,
				OpenOrderRequestCommentStopCheckingResultEnum.NeedsDocumentOrItemDescriptionCorrection => OpenOrderRequestStopStatusEnum.NeedsDocumentRevision,
				OpenOrderRequestCommentStopCheckingResultEnum.DeleteIndustrialUnitRelatedRequest => OpenOrderRequestStopStatusEnum.IndustrialUnitStopCauseDeletionRequestCheck,
				OpenOrderRequestCommentStopCheckingResultEnum.AwaitingProjectManagerOrEmployer => OpenOrderRequestStopStatusEnum.ProjectManagerPendingStopReason,
				_ => OpenOrderRequestStopStatusEnum.InitialSubmissionAndSendToStopBoard
			};
		}

		private OpenOrderRequestStopStatusEnum DetermineNextStopStatusFromCheckingStatus(OpenOrderRequestStopCheckingStatusEnum? status)
		{
			return status switch
			{
				OpenOrderRequestStopCheckingStatusEnum.RetryRequest => OpenOrderRequestStopStatusEnum.RestartStopCauseCheck,
				OpenOrderRequestStopCheckingStatusEnum.ReRegisterStop => OpenOrderRequestStopStatusEnum.CheckStopFactorReRegisterStop,
				OpenOrderRequestStopCheckingStatusEnum.DeleteRequest => OpenOrderRequestStopStatusEnum.StoppingFactorForDeletionRequest,
				_ => OpenOrderRequestStopStatusEnum.InitialSubmissionAndSendToStopBoard
			};
		}

		#endregion

		#region Notification Methods

		private async Task CreateCommentNotification(OpenOrderRequest openOrderRequest, OpenOrderRequestComment comment, CancellationToken cn)
		{
			var recipientEmails = new List<string>();
			var recipientIds = new List<long>();
			
			if (!string.IsNullOrEmpty(openOrderRequest.RequestedPersonelEmail))
				recipientEmails.AddRange(openOrderRequest.RequestedPersonelEmail.Split(',', StringSplitOptions.RemoveEmptyEntries));
			
			if (!string.IsNullOrEmpty(openOrderRequest.RequestedEngineeringPersonelEmail))
				recipientEmails.AddRange(openOrderRequest.RequestedEngineeringPersonelEmail.Split(',', StringSplitOptions.RemoveEmptyEntries));

			if(openOrderRequest.RequestedPersonelIds != null)
				recipientIds.AddRange(openOrderRequest.RequestedPersonelIds);

			if (openOrderRequest.RequestedEngineeringPersonelIds != null)
				recipientIds.AddRange(openOrderRequest.RequestedEngineeringPersonelIds);
			 
			// اضافه کردن شناسه های واحد صنایع
			var SupMembers = await _notificationGroupService.GetGroupMembersAsync("Sup.OpenOrderRequest.Industrial", cn);
			recipientEmails.AddRange(SupMembers.Select(c=>c.Email));
			recipientIds.AddRange(SupMembers.Select(c => (long)c.UserId));


			// اگر کامنت گذار کسی جز واحد تدارکات بود، برای متولی خرید
			// که از بچه های تدارکاته ایمیل برود
			// بشرطی که تایید مهندسی داشته باشه

			if (openOrderRequest.EngineeringAccept == true )
			{
				// شناسه 121000108 شناسه واحد تامین و خرید در راهکاران
				//این شناسه ثابت میباشد اما Id OrgUnit ممکنه متفاوت باشه 
				var isSupOrgUnit = await unitOfWork.Repository<OrgUnit>().TableNoTracking
					.AnyAsync(c => c.Id == CurrentOrganizationUnitId && c.HamkaranUnitId == 121000108);

				if(isSupOrgUnit)
				{
					// دریافت کاربر متولی خرید 
					var purchaseResponsible = await unitOfWork.Repository<BuyCategoryItem>()
						.TableNoTracking
						.Select(c => new {c.Id , c.PartId, PurchaseResponsibleId = c.BuyCategory.PurchaseResponsible.Id , PurchaseResponsibleEmail= c.BuyCategory.PurchaseResponsible.Email })
					.FirstOrDefaultAsync(c => c.PartId == openOrderRequest.PartId,cn);

					if(purchaseResponsible != null && purchaseResponsible.PurchaseResponsibleId.HasValue)
					{
						recipientIds.Add((long)purchaseResponsible.PurchaseResponsibleId);
						if(purchaseResponsible.PurchaseResponsibleEmail.HasValue())
							recipientEmails.AddRange(purchaseResponsible.PurchaseResponsibleEmail);
					}


				}
				// اگر تایید مهندسی داشت به کاربر مهندسی هم ایمیل می رود
				if(openOrderRequest.EngineeringAcceptUserId.HasValue)
				{
					var enginneringAcceptUser = await userService.GetById((long)openOrderRequest.EngineeringAcceptUserId);
					
					if(enginneringAcceptUser !=null)
					recipientIds.Add((long)enginneringAcceptUser.Id);
					if (enginneringAcceptUser.Email.HasValue())
						recipientEmails.Add(enginneringAcceptUser.Email);
				}
			}

			var title = "کامنت جدید درخواست خرید";
			var body = BuildCommentNotificationBody(openOrderRequest, comment);

			await SendNotificationToEmailRecipients(recipientEmails, recipientIds, title, body, openOrderRequest.Id!.Value, cn);
		}

		private string BuildCommentNotificationBody(OpenOrderRequest openOrderRequest, OpenOrderRequestComment comment)
		{
			var sb = new StringBuilder();
			sb.AppendLine("<div style='direction:rtl;text-align:right;padding:5px; margin:5px; font-family:tahoma; font-size: 11pt'>");
			sb.AppendLine($"کاربر گرامی کامنت ذیل برای درخواست خرید شما توسط <strong>{CurrentUserFullName}</strong> صادر شد");
			sb.AppendLine("<br/><br/>");
			sb.AppendLine("<ul>");
			sb.AppendLine($"<li>شماره درخواست: <strong>{openOrderRequest.PurchaseRequestNumber}</strong></li>");
			sb.AppendLine($"<li>شماره سفارش: <strong>{openOrderRequest.OrderNo}</strong></li>");
			sb.AppendLine($"<li>تاریخ: <strong>{comment.ShamsiDate}</strong></li>");
			sb.AppendLine($"<li>توضیحات: <strong>{comment.CommentValue}</strong></li>");
			sb.AppendLine("</ul>");
			sb.AppendLine("</div>");
			return sb.ToString();
		}

		private async Task SendNotificationToEmailRecipients(List<string> emails,List<long> recipientIds, string title, string body, long entityId, CancellationToken cn)
		{
			foreach (var id in recipientIds)
			{
				await unitOfWork.Repository<Notification>().AddAsync(new Notification
				{
					Type = NotificationType.Appliaction,
					Title = title,
					Body = body,
					EntityId = entityId,
					OwnerId = id,
					ViewPath = $"/Panel/Sup/OpenOrderRequest/Edit?id={entityId}",
					IsRead = false
				}, cn);
			}

			await unitOfWork.SaveChangesAsync(cn);
		}

		private async Task CreateEngineeringAcceptNotification(long? openOrderRequestId, CancellationToken cn)
		{

			var openOrderRequest = await unitOfWork.Repository<OpenOrderRequest>()
				.TableNoTracking
				 .Include(c => c.Part) 
				.ThenInclude(p => p.BuyCategory) 
				.Select(c => new { 
				c.Id,
				c.PurchaseRequestNumber,
				c.OrderNo,
				c.SupplyShamsiDate,
				c.EngineeringAcceptUserId,
				c.SalesUnitConfirmationUserId,
				c.SalesUnitSalesExpertId,
				c.SalesUnitSalesManagerId,
				c.SalesUnitProjectManagerId,
				PurchaseResponsibleId = c.Part.BuyCategory != null && c.Part.BuyCategory.PurchaseResponsible != null
								  ? c.Part.BuyCategory.PurchaseResponsible.Id
								  : (long?)null,
				PurchaseResponsibleUsername = c.Part.BuyCategory != null && c.Part.BuyCategory.PurchaseResponsible != null
								? c.Part.BuyCategory.PurchaseResponsible.Username
								: (string?)null
				}).FirstOrDefaultAsync(c=>c.Id == openOrderRequestId,cn);



			var SupMembers = await _notificationGroupService.GetGroupMembersAsync("Sup.OpenOrderRequest.SupplyUnit", cn);
			 
			var title = "تایید مهندسی درخواست خرید";
			var body = $@"
				<div style='text-align:center;direction:rtl'>
					<strong>درخواست خرید به تایید مهندسی رسید</strong>
					<ul>
						<li>شماره درخواست: <strong>{openOrderRequest.PurchaseRequestNumber}</strong></li>
						<li>شماره سفارش: <strong>{openOrderRequest.OrderNo}</strong></li>
						<li>تاییدکننده: <strong>{CurrentUserFullName}</strong></li>
						<li>تاریخ تامین پیش‌بینی شده: <strong>{openOrderRequest.SupplyShamsiDate}</strong></li>
					</ul>
				</div>
			";

			var purchaseResponsibleId = openOrderRequest.PurchaseResponsibleId.HasValue && openOrderRequest.PurchaseResponsibleId.Value > 0;
			var userRecivers = new List<long?>();

			if (openOrderRequest.SalesUnitSalesExpertId.HasValue)
			{
				userRecivers.Add(openOrderRequest.SalesUnitConfirmationUserId);
				userRecivers.Add(openOrderRequest.SalesUnitProjectManagerId);
				userRecivers.Add(openOrderRequest.SalesUnitSalesExpertId);
				userRecivers.Add(openOrderRequest.SalesUnitSalesManagerId);
				
				if (purchaseResponsibleId)
					userRecivers.Add(openOrderRequest.PurchaseResponsibleId);
				else
					userRecivers.AddRange(SupMembers.Select(c => c.UserId));
			}
			else
			{
				if (purchaseResponsibleId)
					userRecivers.Add(openOrderRequest.PurchaseResponsibleId);
				else
					userRecivers.AddRange(SupMembers.Select(c => c.UserId));
			}


			//به درخواست تدارکات اگر کارشناس تدارکات در دریافت کنندگان بود 
			//سرپرست اون کارشناس هم در دریافت کنندگان اضافه شود 
			if(purchaseResponsibleId)
				AddSupplierSupervisorToRecivers(userRecivers , openOrderRequest.PurchaseResponsibleUsername);


			foreach (var member in userRecivers.Distinct())
			{

				await notificationService.CreateAndSendAsync(new Notification
				{
					Type = NotificationType.Appliaction,
					Title = title,
					Body = body,
					EntityId = openOrderRequest.Id,
					OwnerId = member!.Value,
					ViewPath = $"/Panel/Sup/OpenOrderRequest/Edit?id={openOrderRequest.Id}",
					IsRead = false,
				});
				 
			}

			await unitOfWork.SaveChangesAsync(cn);
		}
		public void AddSupplierSupervisorToRecivers(List<long?> userRecivers, string purchaseResponsibleUsername)
		{

			if (string.IsNullOrEmpty(purchaseResponsibleUsername) || userRecivers == null)
				return;

			var userSupervisorDic = userService
				.TableNoTracking
				.Where(c =>
				  c.Username.ToLower() == "bagheri.h" ||
				  c.Username.ToLower() == "yaltaghian.f" ||
				  c.Username.ToLower() == "parhizkari.s"
				).
				Select(c => new { Username =c.Username.ToLower(), c.Id}).ToDictionary(c=>c.Username,c=>c.Id);
			var purchaseResponsibleUsernameLow = purchaseResponsibleUsername.ToLower();

			string supervisorKey = null;

			switch (purchaseResponsibleUsernameLow)
			{
				case "sohrabi.za":
				case "razaghmanesh.z":
				case "sarmadi.p":
					supervisorKey = "bagheri.h";
					break;

				case "shahpordeli.s":
				case "rajablou.a":
					supervisorKey = "yaltaghian.f";
					break;

				case "ashrafi.m":
				case "sharifi.b":
				case "zabihian.s":
					supervisorKey = "parhizkari.s";
					break;

				default:
					// هیچ سرپرستی پیدا نشد
					return;
			}

			// بررسی وجود کلید در دیکشنری
			if (supervisorKey != null && userSupervisorDic.TryGetValue(supervisorKey, out var supervisorId))
			{
				// بررسی تکراری نبودن
				if (!userRecivers.Contains(supervisorId))
				{
					userRecivers.Add(supervisorId);
				}
			}


		}

		private async Task CreateSalesConfirmationNotification(OpenOrderRequest openOrderRequest, CancellationToken cn)
		{
			var supplyEmails = new List<string> 
			{ 
				"Dordab.y", "Bagheri.h", "Yaltaghian.f", "Shahpordeli.s", 
				"Sharifi.b", "sohrabi.za", "Ashrafi.m", "Abdollahi.a", 
				"zare.m", "Rajablou.a", "sarmadi.p"
			};

			var emails = supplyEmails.Select(e => e + "@havayar.com").ToList();
			
			var users = await userService.TableNoTracking
				.Where(u => emails.Contains(u.Email))
				.ToListAsync(cn);

			var title = "تایید واحد پروژه/فروش - درخواست خرید";
			var body = $@"
				<div style='text-align:center;direction:rtl'>
					درخواست خرید به تایید واحد پروژه/فروش رسید
					<ul>
						<li>شماره درخواست: <strong>{openOrderRequest.PurchaseRequestNumber}</strong></li>
						<li>شماره سفارش: <strong>{openOrderRequest.OrderNo}</strong></li>
						<li>توضیحات تایید: <strong>{openOrderRequest.SalesUnitConfirmationComment}</strong></li>
					</ul>
				</div>
			";

			foreach (var user in users)
			{
				await unitOfWork.Repository<Notification>().AddAsync(new Notification
				{
					Type = NotificationType.Appliaction,
					Title = title,
					Body = body,
					EntityId = openOrderRequest.Id,
					OwnerId = user.Id!.Value,
					ViewPath = $"/Panel/Sup/OpenOrderRequest/Edit?id={openOrderRequest.Id}",
					IsRead = false
				}, cn);
			}

			await unitOfWork.SaveChangesAsync(cn);
		}

		private async Task CreateStopInitNotification(OpenOrderRequest openOrderRequest, OpenOrderRequestComment comment, CancellationToken cn)
		{
			var recipientEmails = new List<string>();
			var ccRecipientEmails = new List<string>();
			var userData =  await userService.GetAll();
			 
			comment.StopOperator = userData.FirstOrDefault(u => u.Id == comment.StopOperatorId);

			var title = "توقف درخواست خرید";
			var body = BuildStopInitNotificationBody(openOrderRequest, comment);

			if(comment.StopOperator?.Email.HasValue() ?? false)
			  recipientEmails.Add(comment.StopOperator.Email);

			if (CurrentUserEmail.HasValue())
				ccRecipientEmails.Add(CurrentUserEmail);

			var buyCategoryPurchaseResponsibleEmail =  unitOfWork.Repository<BuyCategoryItem>()
				.TableNoTracking
				.Include(c => c.BuyCategory)
				.ThenInclude(bc => bc.PurchaseResponsible)
				.Where(c => c.PartId == openOrderRequest.PartId && c.BuyCategory.PurchaseResponsible != null && c.BuyCategory.PurchaseResponsible.Email != null)
				.Select(c => c.BuyCategory.PurchaseResponsible.Email)
				.FirstOrDefault();

			if(buyCategoryPurchaseResponsibleEmail.HasValue())
				ccRecipientEmails.Add(buyCategoryPurchaseResponsibleEmail);

			if (comment.BeneficiariesIds.HasValue())
			{
				var beneficiariesIds = comment.BeneficiariesIds.Split(',').Where(p => !p.HasValue()).Select(long.Parse).ToList();

				var beneficiariesEmails = userData.Where(p => beneficiariesIds.Contains((long)p.Id)).Select(p => p.Email).ToArray();
				if (beneficiariesEmails.Any())
					ccRecipientEmails.AddRange(beneficiariesEmails);
			}

			if (!string.IsNullOrEmpty(openOrderRequest.RequestedPersonelEmail))
				ccRecipientEmails.AddRange(openOrderRequest.RequestedPersonelEmail.Split(',', StringSplitOptions.RemoveEmptyEntries));

			if (!string.IsNullOrEmpty(openOrderRequest.RequestedEngineeringPersonelEmail))
				ccRecipientEmails.AddRange(openOrderRequest.RequestedEngineeringPersonelEmail.Split(',', StringSplitOptions.RemoveEmptyEntries));

			if (openOrderRequest.EngineeringAcceptUserId.HasValue)
			{
				var engineeringAcceptUser = userData.FirstOrDefault(p => p.Id == openOrderRequest.EngineeringAcceptUserId);
				if (engineeringAcceptUser != null && engineeringAcceptUser.Email.HasValue())
					ccRecipientEmails.Add(engineeringAcceptUser.Email);
			}

			if (openOrderRequest.SalesUnitProjectManagerId.HasValue)
			{
				var salesUnitProjectManager = userData.FirstOrDefault(p => p.Id == openOrderRequest.SalesUnitProjectManagerId);
				if (salesUnitProjectManager != null && salesUnitProjectManager.Email.HasValue())
					ccRecipientEmails.Add(salesUnitProjectManager.Email);
			}

			await unitOfWork.Repository<Notification>().AddAsync(new Notification
			{
				Type = NotificationType.Email,
				Title = title,
				Body = body,
				EntityId = openOrderRequest.Id,
				OwnerId = (long)CurrentUserId,
				ViewPath = $"/Panel/Sup/OpenOrderRequest/Edit?id={openOrderRequest.Id}",
				IsRead = false,
				IsSend = false,
				CcEmails = ccRecipientEmails,
				ToEmails = recipientEmails

			}, cn);

		  
		}

		private async Task CreateStopOperatorReviewNotification(OpenOrderRequest openOrderRequest, OpenOrderRequestComment comment, CancellationToken cn)
		{
			var recipientEmails = new List<string>();
			var ccRecipientEmails = new List<string>();
			var userData = await userService.GetAll();

			if (!string.IsNullOrEmpty(openOrderRequest.RequestedPersonelEmail))
				recipientEmails.AddRange(openOrderRequest.RequestedPersonelEmail.Split(';', StringSplitOptions.RemoveEmptyEntries));

			if (!string.IsNullOrEmpty(openOrderRequest.RequestedEngineeringPersonelEmail))
				recipientEmails.AddRange(openOrderRequest.RequestedEngineeringPersonelEmail.Split(';', StringSplitOptions.RemoveEmptyEntries));

			var title = "نتیجه بررسی توقف درخواست خرید";
			var body = BuildStopReviewNotificationBody(openOrderRequest, comment);

			if (CurrentUserEmail.HasValue())
				ccRecipientEmails.Add(CurrentUserEmail);

			var buyCategoryPurchaseResponsibleEmail = unitOfWork.Repository<BuyCategoryItem>()
				.TableNoTracking
				.Include(c => c.BuyCategory)
				.ThenInclude(bc => bc.PurchaseResponsible)
				.Where(c => c.PartId == openOrderRequest.PartId && c.BuyCategory.PurchaseResponsible != null && c.BuyCategory.PurchaseResponsible.Email != null)
				.Select(c => c.BuyCategory.PurchaseResponsible.Email)
				.FirstOrDefault();

			if (buyCategoryPurchaseResponsibleEmail.HasValue())
				ccRecipientEmails.Add(buyCategoryPurchaseResponsibleEmail);

			if (comment.BeneficiariesIds.HasValue())
			{
				var beneficiariesIds = comment.BeneficiariesIds.Split(',').Where(p => !p.HasValue()).Select(long.Parse).ToList();

				var beneficiariesEmails = userData.Where(p => beneficiariesIds.Contains((long)p.Id)).Select(p => p.Email).ToArray();
				if (beneficiariesEmails.Any())
					ccRecipientEmails.AddRange(beneficiariesEmails);
			}

			if (!string.IsNullOrEmpty(openOrderRequest.RequestedPersonelEmail))
				ccRecipientEmails.AddRange(openOrderRequest.RequestedPersonelEmail.Split(',', StringSplitOptions.RemoveEmptyEntries));

			if (!string.IsNullOrEmpty(openOrderRequest.RequestedEngineeringPersonelEmail))
				ccRecipientEmails.AddRange(openOrderRequest.RequestedEngineeringPersonelEmail.Split(',', StringSplitOptions.RemoveEmptyEntries));

			if (openOrderRequest.EngineeringAcceptUserId.HasValue)
			{
				var engineeringAcceptUser = userData.FirstOrDefault(p => p.Id == openOrderRequest.EngineeringAcceptUserId);
				if (engineeringAcceptUser != null && engineeringAcceptUser.Email.HasValue())
					ccRecipientEmails.Add(engineeringAcceptUser.Email);
			}

			if (openOrderRequest.SalesUnitProjectManagerId.HasValue)
			{
				var salesUnitProjectManager = userData.FirstOrDefault(p => p.Id == openOrderRequest.SalesUnitProjectManagerId);
				if (salesUnitProjectManager != null && salesUnitProjectManager.Email.HasValue())
					ccRecipientEmails.Add(salesUnitProjectManager.Email);
			}

			await unitOfWork.Repository<Notification>().AddAsync(new Notification
			{
				Type = NotificationType.Email,
				Title = title,
				Body = body,
				EntityId = openOrderRequest.Id,
				OwnerId = (long)CurrentUserId,
				ViewPath = $"/Panel/Sup/OpenOrderRequest/Edit?id={openOrderRequest.Id}",
				IsRead = false,
				IsSend = false,
				CcEmails = ccRecipientEmails,
				ToEmails = recipientEmails

			}, cn);
		}

		private string BuildStopReviewNotificationBody(OpenOrderRequest openOrderRequest, OpenOrderRequestComment comment)
		{
			var sb = new StringBuilder();
			sb.AppendLine("<div style='text-align:center;direction:rtl'>");
			sb.AppendLine("<table border='1' cellspacing='0' cellpadding='5' style='text-align:right; direction: rtl' width='100%'>");
			sb.AppendLine("<tr style='background: #000aa0'>");
			sb.AppendLine("<td><div style='font-size:14.0pt;font-family:Zar;color:#FFFFFF;text-align:center'>گروه صنعتی هوایار</div></td>");
			sb.AppendLine("</tr>");
			sb.AppendLine("<tr><td>");
			sb.AppendLine($"توقف درخواست خرید توسط <strong>{CurrentUserFullName}</strong> بررسی شد");
			sb.AppendLine("<ul>");
			sb.AppendLine($"<li>شماره درخواست: <strong>{openOrderRequest.PurchaseRequestNumber}</strong></li>");
			sb.AppendLine($"<li>شماره سفارش: <strong>{openOrderRequest.OrderNo}</strong></li>");
			sb.AppendLine($"<li>نتیجه بررسی: <strong style='color:red'>{comment.StopCheckingResult?.ToDisplay()}</strong></li>");
			sb.AppendLine($"<li>توضیحات: <strong>{comment.StopCheckingComment}</strong></li>");
			
			if (comment.IsNeedToUpdateBom)
				sb.AppendLine("<li>نیاز به بروزرسانی BOM: <strong style='color:red'>بلی</strong></li>");
			
			if (comment.IsNeedToDeleteBom)
				sb.AppendLine("<li>نیاز به حذف BOM: <strong style='color:red'>بلی</strong></li>");
			
			if (!string.IsNullOrEmpty(comment.ProjectManagerApproximateCommentShamsiDate))
				sb.AppendLine($"<li>تاریخ تقریبی اعمال نظر: <strong>{comment.ProjectManagerApproximateCommentShamsiDate}</strong></li>");
			
			sb.AppendLine("</ul>");
			sb.AppendLine("</td></tr>");
			sb.AppendLine("</table>");
			sb.AppendLine("</div>");
			return sb.ToString();
		}

		private string BuildStopInitNotificationBody(OpenOrderRequest openOrderRequest, OpenOrderRequestComment comment)
		{
			var sb = new StringBuilder();
			sb.AppendLine("<div style='text-align:center;direction:rtl'>");
			sb.AppendLine("<table border='1' cellspacing='0' cellpadding='5' style='text-align:right; direction: rtl' width='100%'>");
			sb.AppendLine("<tr style='background: #000aa0'>");
			sb.AppendLine("<td><div style='font-size:14.0pt;font-family:Zar;color:#FFFFFF;text-align:center'>گروه صنعتی هوایار</div></td>");
			sb.AppendLine("</tr>");
			sb.AppendLine("<tr><td>");
			sb.AppendLine($"بدینوسیله اعلام می گردد توقف جدیدی توسط <strong style='color:red'>{CurrentUserFullNameFn}</strong> در ماژول درخواست های باز و با مشخصات ذیل ثبت گردید، خواهشمند است نسبت به بررسی و ادامه روند آن اقدام فرمایید");
			sb.AppendLine("<ul>");
			sb.AppendLine($"<li>شماره درخواست خرید: <strong>{openOrderRequest.PurchaseRequestNumber}</strong></li>");
			sb.AppendLine($"<li>شماره سفارش: <strong>{openOrderRequest.OrderNo}</strong></li>");
			sb.AppendLine($"<li>توضیحات: <strong>{comment.StopCheckingComment}</strong></li>");

			sb.AppendLine("<li>");
			sb.AppendLine($"نوع توقف : <strong style='color:red'>{(comment.StopType.HasValue ? comment.StopType.Value.ToDisplay() : "نامشخص")}</strong>");
			sb.AppendLine("</li>");

			sb.AppendLine("<li>");
			sb.AppendLine($"عامل توقف : <strong style='color:red'>{comment?.StopOperator?.NameFa ?? "-"}</strong>");
			sb.AppendLine("</li>");

			sb.AppendLine("</ul>");
			sb.AppendLine("</td></tr>");
			sb.AppendLine("</table>");
			sb.AppendLine("</div>");
			return sb.ToString();
		}

		private async Task CreateStopRequesterDecisionNotification(OpenOrderRequest openOrderRequest, OpenOrderRequestComment comment, CancellationToken cn)
		{
			// Notification to stop operator and beneficiaries
			var recipientIds = new List<long>();
			var recipientEmails = new List<string>();
			var ccRecipientEmails = new List<string>();
			var userData = await userService.GetAll();

			if (comment.StopOperatorId.HasValue)
				recipientIds.Add(comment.StopOperatorId.Value);

			if (!string.IsNullOrEmpty(comment.BeneficiariesIds))
			{
				var ids = comment.BeneficiariesIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
					.Select(long.Parse);
				recipientIds.AddRange(ids);
			}

			var title = "تصمیم درخواست‌کننده توقف";
			var body = $@"
				<div style='text-align:center;direction:rtl'>
					درخواست‌کننده توقف تصمیم خود را اعلام کرد
					<ul>
						<li>شماره درخواست: <strong>{openOrderRequest.PurchaseRequestNumber}</strong></li>
						<li>وضعیت: <strong>{comment.StopCheckingStatus?.ToDisplay()}</strong></li>
						<li>توضیحات: <strong>{comment.StopCheckingStatusComment}</strong></li>
					</ul>
				</div>
			";

			if (CurrentUserEmail.HasValue())
				ccRecipientEmails.Add(CurrentUserEmail);

			var buyCategoryPurchaseResponsibleEmail = unitOfWork.Repository<BuyCategoryItem>()
				.TableNoTracking
				.Include(c => c.BuyCategory)
				.ThenInclude(bc => bc.PurchaseResponsible)
				.Where(c => c.PartId == openOrderRequest.PartId && c.BuyCategory.PurchaseResponsible != null && c.BuyCategory.PurchaseResponsible.Email != null)
				.Select(c => c.BuyCategory.PurchaseResponsible.Email)
				.FirstOrDefault();

			if (buyCategoryPurchaseResponsibleEmail.HasValue())
				ccRecipientEmails.Add(buyCategoryPurchaseResponsibleEmail);

			if (comment.BeneficiariesIds.HasValue())
			{
				var beneficiariesIds = comment.BeneficiariesIds.Split(',').Where(p => !p.HasValue()).Select(long.Parse).ToList();

				var beneficiariesEmails = userData.Where(p => beneficiariesIds.Contains((long)p.Id)).Select(p => p.Email).ToArray();
				if (beneficiariesEmails.Any())
					ccRecipientEmails.AddRange(beneficiariesEmails);
			}

			if (!string.IsNullOrEmpty(openOrderRequest.RequestedPersonelEmail))
				ccRecipientEmails.AddRange(openOrderRequest.RequestedPersonelEmail.Split(',', StringSplitOptions.RemoveEmptyEntries));

			if (!string.IsNullOrEmpty(openOrderRequest.RequestedEngineeringPersonelEmail))
				ccRecipientEmails.AddRange(openOrderRequest.RequestedEngineeringPersonelEmail.Split(',', StringSplitOptions.RemoveEmptyEntries));

			if (openOrderRequest.EngineeringAcceptUserId.HasValue)
			{
				var engineeringAcceptUser = userData.FirstOrDefault(p => p.Id == openOrderRequest.EngineeringAcceptUserId);
				if (engineeringAcceptUser != null && engineeringAcceptUser.Email.HasValue())
					ccRecipientEmails.Add(engineeringAcceptUser.Email);
			}

			if (openOrderRequest.SalesUnitProjectManagerId.HasValue)
			{
				var salesUnitProjectManager = userData.FirstOrDefault(p => p.Id == openOrderRequest.SalesUnitProjectManagerId);
				if (salesUnitProjectManager != null && salesUnitProjectManager.Email.HasValue())
					ccRecipientEmails.Add(salesUnitProjectManager.Email);
			}

			await unitOfWork.Repository<Notification>().AddAsync(new Notification
			{
				Type = NotificationType.Email,
				Title = title,
				Body = body,
				EntityId = openOrderRequest.Id,
				OwnerId = (long)CurrentUserId,
				ViewPath = $"/Panel/Sup/OpenOrderRequest/Edit?id={openOrderRequest.Id}",
				IsRead = false,
				IsSend = false,
				CcEmails = ccRecipientEmails,
				ToEmails = recipientEmails

			}, cn);
		}

		private async Task CreateTriggeringNotification(OpenOrderRequest openOrderRequest, CancellationToken cn)
		{
			var recipientEmails = new List<string>();
			
			if (!string.IsNullOrEmpty(openOrderRequest.RequestedPersonelEmail))
				recipientEmails.AddRange(openOrderRequest.RequestedPersonelEmail.Split(';', StringSplitOptions.RemoveEmptyEntries));
			
			if (!string.IsNullOrEmpty(openOrderRequest.RequestedEngineeringPersonelEmail))
				recipientEmails.AddRange(openOrderRequest.RequestedEngineeringPersonelEmail.Split(';', StringSplitOptions.RemoveEmptyEntries));

			var users = await userService.TableNoTracking
				.Where(u => recipientEmails.Contains(u.Email))
				.ToListAsync(cn);

			var title = "راه اندازی درخواست خرید";
			var body = $@"
				<div style='direction:rtl;text-align:right'>
					کاربر گرامی درخواست کالای شما توسط واحد صنایع راه اندازی شده است
					<ul>
						<li>شماره درخواست: <strong>{openOrderRequest.PurchaseRequestNumber}</strong></li>
						<li>شماره سفارش: <strong>{openOrderRequest.OrderNo}</strong></li>
					</ul>
				</div>
			";

			foreach (var user in users)
			{
				await unitOfWork.Repository<Notification>().AddAsync(new Notification
				{
					Type = NotificationType.Appliaction,
					Title = title,
					Body = body,
					EntityId = openOrderRequest.Id,
					OwnerId = user.Id!.Value,
					ViewPath = $"/Panel/Sup/OpenOrderRequest/Edit?id={openOrderRequest.Id}",
					IsRead = false
				}, cn);
			}

			await unitOfWork.SaveChangesAsync(cn);
		}

		private async Task CreateNewAttachmentNotification(OpenOrderRequest openOrderRequest, OpenOrderRequestAttachment attachment, CancellationToken cn)
		{
			// TODO: به واحد تولید اعلان داده شود
			var productionEmails = new List<string> { "Sohrabi.z", "golestaneh.a", "Eftekhari.a", "Estaki.a" }
				.Select(e => e + "@havayar.com");

			var users = await userService.TableNoTracking
				.Where(u => productionEmails.Contains(u.Email))
				.ToListAsync(cn);

			var title = "اعلام ایجاد پیوست درخواست باز";
			var body = $@"
				<div style='direction:rtl;text-align:right'>
					مدرک/مدارک جدیدی جهت درخواست باز آپلود گردید
					<ul>
						<li>شماره درخواست: <strong>{openOrderRequest.PurchaseRequestNumber}</strong></li>
						<li>شماره سفارش: <strong>{openOrderRequest.OrderNo}</strong></li>
						<li>نوع مدرک: <strong>{attachment.FileType?.ToDisplay()}</strong></li>
					</ul>
				</div>
			";

			foreach (var user in users)
			{
				await unitOfWork.Repository<Notification>().AddAsync(new Notification
				{
					Type = NotificationType.Appliaction,
					Title = title,
					Body = body,
					EntityId = openOrderRequest.Id,
					OwnerId = user.Id!.Value,
					ViewPath = $"/Panel/Sup/OpenOrderRequest/Edit?id={openOrderRequest.Id}",
					IsRead = false
				}, cn);
			}

			await unitOfWork.SaveChangesAsync(cn);
		}

		private string? ValidateStopRequest(StopOperationRequest request, int currentStopStatus)
		{
			switch (currentStopStatus)
			{
				case 0: // Initial
				case 2815: // Restart
					if (!request.StopTypeEnum.HasValue)
						return "نوع توقف الزامی است";
					if (!request.StopOperatorId.HasValue)
						return "عامل توقف الزامی است";
					if (string.IsNullOrEmpty(request.BeneficiariesIds))
						return "انتخاب ذینفعان الزامی است";
					if (string.IsNullOrEmpty(request.CommentValue))
						return "علت توقف الزامی است";
					break;

				case 2807: // InitialAndSendToResponsible
				case 2813: // WaitingForProjectManager
				case 2816: // RegisterStopAgain
					if (!request.StopCheckingResult.HasValue)
						return "نتیجه بررسی الزامی است";
					if (string.IsNullOrEmpty(request.StopCheckingComment))
						return "توضیحات بررسی الزامی است";
					
					// اگر نیاز به اصلاح BOM است، حداقل یکی از گزینه‌ها باید انتخاب شود
					if (request.StopCheckingResult == OpenOrderRequestCommentStopCheckingResultEnum.NeedsBOMCorrection)
					{
						if (!request.IsNeedToUpdateBom && !request.IsNeedToDeleteBom)
							return "لطفاً حداقل یکی از گزینه‌های 'نیاز به بروزرسانی BOM' یا 'نیاز به حذف BOM' را انتخاب کنید";
					}
					
					// اگر در انتظار مدیر پروژه است، تاریخ تقریبی الزامی است
					if (request.StopCheckingResult == OpenOrderRequestCommentStopCheckingResultEnum.AwaitingProjectManagerOrEmployer)
					{
						if (string.IsNullOrEmpty(request.ProjectManagerApproximateCommentShamsiDate))
							return "تاریخ تقریبی اعمال نظر مدیر پروژه الزامی است";
					}
					break;

				case 2808: // NeedToRestartRequest
				case 2810: // NeedToEdit
				case 2811: // NeedToEditDocuments
				case 2812: // DeleteIndustrialRequest
				case 2821: // SendToSupplyCartable
					if (!request.StopCheckingStatus.HasValue)
						return "وضعیت بررسی الزامی است";
					if (string.IsNullOrEmpty(request.StopCheckingStatusComment))
						return "توضیحات الزامی است";
					
					// اگر ثبت توقف مجدد انتخاب شده، فیلدهای مربوطه الزامی است
					if (request.StopCheckingStatus == OpenOrderRequestStopCheckingStatusEnum.ReRegisterStop)
					{
						if (!request.ReStopTypeEnum.HasValue)
							return "نوع توقف مجدد الزامی است";
						if (!request.ReStopOperatorId.HasValue)
							return "عامل توقف مجدد الزامی است";
						if (string.IsNullOrEmpty(request.ReStopCommentValue))
							return "علت توقف مجدد الزامی است";
					}
					break;
			}

			return null;
		}

		#endregion

		#region VPIS Operations

		[HttpGet("[action]")]
		public async Task<IActionResult> LinkVpisPartial(long openOrderRequestId, CancellationToken cn)
		{
			ViewBag.OpenOrderRequestId = openOrderRequestId;
			return PartialView(@"\Views\Panel\Sup\OpenOrderRequest\_LinkVpisPartial.cshtml");
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> GetCurrentProjectForVpis(long openOrderRequestId, CancellationToken cn)
		{
			try
			{
				// دریافت پروژه فعلی از VPIS های لینک شده
				var projectId = await unitOfWork.Repository<OpenOrderRequestVpis>()
					.TableNoTracking
					.Where(v => v.OpenOrderRequestId == openOrderRequestId && v.IsLatest)
					.Select(v => v.ProjectId)
					.FirstOrDefaultAsync(cn);

				return Ok(projectId);
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در دریافت پروژه: " + ex.Message);
			}
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> GetVpisListByProject(long projectId, long openOrderRequestId, CancellationToken cn)
		{
			try
			{
				// پروژه‌هایی که نباید نمایش داده شوند
				var ignoredProjectStatusIds = new List<short> { 3, 6, 7 };

				// TODO: این قسمت نیاز به بررسی دقیق navigation properties دارد
				// فعلاً به صورت ساده پیاده می‌شود
				
				// دریافت تمام ProjectVpis های پروژه
				var projectVpisList = await unitOfWork.Repository<ProjectVpis>()
					.TableNoTracking
					.Include(pv => pv.ProjectName)
					.Where(pv => pv.ProjectNameId == projectId)
					.ToListAsync(cn);

				if (!projectVpisList.Any())
					return Ok(new { vpisList = new List<object>(), linkedDocumentIds = new List<long>() });

				var projectVpisIds = projectVpisList.Select(pv => pv.Id!.Value).ToList();

				// دریافت آخرین Documents برای هر ProjectVpis
				var allDocuments = await unitOfWork.Repository<Document>()
					.TableNoTracking
					.Include(d => d.Comments)
					.Where(d => projectVpisIds.Contains(d.DocumentVpisId!.Value))
					.ToListAsync(cn);

				// فیلتر بر اساس شرایط سیستم قدیم
				var validDocuments = allDocuments
					.Where(d => 
						// 1. ApprovedDate داشته باشد
						d.ApprovedMiladiDateTime != null ||
						// 2. بیش از 1 کامنت داشته باشد
						(d.Comments != null && d.Comments.Count > 1) ||
						// 3. کامنت با وضعیت NotReview داشته باشد
						(d.Comments != null && d.Comments.Any(c => c.Status == DocumentStatusEnums.NotReview))
					)
					.GroupBy(d => d.DocumentVpisId)
					.Select(g => g.OrderByDescending(d => d.Id).FirstOrDefault())
					.ToList();

				var lastSendedDocuments = validDocuments.Where(d => d != null).ToList();

				if (!lastSendedDocuments.Any())
					return Ok(new { vpisList = new List<object>(), linkedDocumentIds = new List<long>() });

				// دریافت مجدد ProjectVpis برای نمایش
				var vpisIdsInDocuments = lastSendedDocuments.Select(d => d.DocumentVpisId!.Value).ToList();
				var projectVpisForDisplay = await unitOfWork.Repository<ProjectVpis>()
					.TableNoTracking
					.Include(pv => pv.ProjectName)
					.Where(pv => vpisIdsInDocuments.Contains(pv.Id!.Value))
					.ToListAsync(cn);

				// ساخت لیست برای نمایش
				var vpisList = lastSendedDocuments.Select(d =>
				{
					var vpis = projectVpisForDisplay.FirstOrDefault(pv => pv.Id == d.DocumentVpisId);
					return new
					{
						id = d.Id!.Value,
						projectVpisId = d.DocumentVpisId!.Value,
						documentNumber = vpis?.Code,
						documentTitle = vpis?.Title,
						projectName = vpis?.ProjectName?.ProjectName,
						revision = d.Revision,
						documentFullTitle = $"{vpis?.ProjectName?.ProjectName} | {vpis?.Title} | {vpis?.Code} | Ver({d.Revision})"
					};
				}).ToList();

				// دریافت مدارک لینک شده فعلی
				var linkedDocumentIds = await unitOfWork.Repository<OpenOrderRequestVpis>()
					.TableNoTracking
					.Where(v => v.OpenOrderRequestId == openOrderRequestId && v.IsLatest)
					.Select(v => v.DocumentId)
					.ToListAsync(cn);

				return Ok(new { vpisList, linkedDocumentIds });
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در دریافت لیست VPIS: " + ex.Message);
			}
		}

		[HttpPost("[action]")]
		public async Task<IActionResult> DoLinkVpis(LinkVpisRequest request, CancellationToken cn)
		{
			try
			{
				// TODO: بررسی دسترسی
				if (request.SelectedVpisIds == null || !request.SelectedVpisIds.Any())
					return BadRequest("لطفاً حداقل یک مدرک VPIS انتخاب کنید");

				if (!request.ProjectId.HasValue)
					return BadRequest("لطفاً پروژه را انتخاب کنید");

				var openOrderRequest = await unitOfWork.Repository<OpenOrderRequest>()
					.Table.FirstOrDefaultAsync(x => x.Id == request.OpenOrderRequestId, cn);

				if (openOrderRequest == null)
					return BadRequest("درخواست باز یافت نشد");

				// دریافت اطلاعات Documents
				var documents = await unitOfWork.Repository<Document>()
					.TableNoTracking
					.Include(d => d.DocumentVpis)
					.Where(d => request.SelectedVpisIds.Contains(d.Id!.Value))
					.ToListAsync(cn);

				if (!documents.Any())
					return BadRequest("مدارک انتخاب شده یافت نشد");

				var now = DateTime.Now;
				var currentUserId = CurrentUserId;

				// غیرفعال کردن لینک‌های قبلی
				var oldLinks = await unitOfWork.Repository<OpenOrderRequestVpis>()
					.Table
					.Where(v => v.OpenOrderRequestId == request.OpenOrderRequestId && v.IsLatest)
					.ToListAsync(cn);

				foreach (var oldLink in oldLinks)
				{
					oldLink.IsLatest = false;
				}

				await unitOfWork.SaveChangesAsync(cn);

				// ایجاد لینک‌های جدید
				var newLinks = new List<OpenOrderRequestVpis>();
				foreach (var document in documents)
				{
					newLinks.Add(new OpenOrderRequestVpis
					{
						OpenOrderRequestId = request.OpenOrderRequestId,
						ProjectId = request.ProjectId.Value,
						DocumentId = document.Id,
						RevisionNumber = document.Revision,
						IsLatest = true,
						Comment = "لینک توسط کاربر"
					});
				}

				foreach (var link in newLinks)
				{
					await unitOfWork.Repository<OpenOrderRequestVpis>().AddAsync(link, cn);
				}

				await unitOfWork.SaveChangesAsync(cn);

				return Ok(new { message = "لینک با موفقیت ثبت شد" });
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در ثبت لینک VPIS: " + ex.Message);
			}
		}



		#endregion

		#region AddRequsted
		[HttpGet("[action]")]
		public async Task<IActionResult> AddRequestedPersonelPartial(long openOrderRequestId, CancellationToken cn)
		{
			ViewBag.OpenOrderRequestId = openOrderRequestId;
			//دریافت نفرات ثابت 
			 var  staticPersonels = await userService.GetUsersByRoleName("Sup.OpenOrderRequest.ConfigManageStaticPersonel");
			ViewBag.StaticPersonels = staticPersonels?.Select(c => new { c.Id, c.Username, c.Name, c.NameFa, c.Email }) ?? [];
			return PartialView(@"\Views\Panel\Sup\OpenOrderRequest\_AddRequestedPersonelPartial.cshtml");
		}

		[HttpPost("[action]")]
		public async Task<IActionResult> AddRequestedPersonelToOpenRequest(AddRequestedPersonel request, CancellationToken cn)
		{
			try
			{
				// TODO: بررسی دسترسی
				if (request.OpenOrderRequestId == null || request.OpenOrderRequestId == 0)
					return BadRequest("درخواست باز یافت نشد");
				 

				var openOrderRequest = await unitOfWork
					.Repository<OpenOrderRequest>()
					.Table
					.FirstOrDefaultAsync(x => x.Id == request.OpenOrderRequestId, cn);

				if (openOrderRequest == null)
					return BadRequest("درخواست باز یافت نشد");

			   

				var user = await userService.TableNoTracking
					.Where(c => request.requestedEngineeringPersonelIds.Contains((long)c.Id) ||
							  request.RequestedPersonelIds.Contains((long)c.Id) 
							  )
					.ToArrayAsync(cn);

				if(request.requestedEngineeringPersonelIds.Any())
				{
					openOrderRequest.RequestedEngineeringPersonelIds = request.requestedEngineeringPersonelIds;
					openOrderRequest.RequestedEngineeringPersonel = string.Join(',',user.Where(c=> request.requestedEngineeringPersonelIds.
																Contains((long)c.Id)).Select(c=>c.NameFa)
																.ToArray());
					openOrderRequest.RequestedEngineeringPersonelEmail = string.Join(',', user.Where(c => request.requestedEngineeringPersonelIds.
																Contains((long)c.Id)
																).Select(c => c.Email)
																.ToArray());
				}

				if(request.RequestedPersonelIds.Any())
				{

					openOrderRequest.RequestedPersonelIds = request.RequestedPersonelIds;
					openOrderRequest.RequestedPersonel = string.Join(',', user.Where(c => request.RequestedPersonelIds.
																Contains((long)c.Id)).Select(c => c.NameFa)
																.ToArray());
					openOrderRequest.RequestedPersonelEmail = string.Join(',', user.Where(c => request.requestedEngineeringPersonelIds.
																Contains((long)c.Id)).Select(c => c.Email)
																.ToArray());


				}

				if (openOrderRequest.RequestedPersonelRegShamsiDate == null)
					openOrderRequest.RequestedPersonelRegShamsiDate = DateTime.Now.ToShamsiDate();
				 
				await unitOfWork.SaveChangesAsync(cn);

				return Ok(new { message = "لینک با موفقیت ثبت شد" });
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در ثبت لینک VPIS: " + ex.Message);
			}
		}
		#endregion

		#region Request DTOs

		public class AddRequestedPersonel
		{
			public long? OpenOrderRequestId { get; set; }
			public List<string>? RequestedPersonel { get; set; }
			public List<long> RequestedPersonelIds { get; set; } = new();
			public List<string>? RequestedEngineeringPersonel { get; set; }
			public List<long> requestedEngineeringPersonelIds { get; set; } = new();
  
		}
		public class SaveCommentRequest
		{
			public long OpenOrderRequestId { get; set; }
			public string? CommentShamsiDate { get; set; }
			public string? CommentValue { get; set; }
		}

		public class SalesOrProjectAcceptRequest
		{
			public long OpenOrderRequestId { get; set; }
			public string? SalesUnitConfirmationComment { get; set; }
		}

		public class StopOperationRequest
		{
			public long OpenOrderRequestId { get; set; }
			public int? StopTypeEnum { get; set; }
			public long? StopOperatorId { get; set; }
			public string? BeneficiariesIds { get; set; }
			public string? CommentValue { get; set; }
			public long? AttachmentId { get; set; }
			
			// Section 2 fields
			public OpenOrderRequestCommentStopCheckingResultEnum? StopCheckingResult { get; set; }
			public string? StopCheckingComment { get; set; }
			public string? StopCheckingDelayReasonText { get; set; }
			public bool IsNeedToUpdateBom { get; set; }
			public bool IsNeedToDeleteBom { get; set; }
			public string? ProjectManagerApproximateCommentShamsiDate { get; set; }
			public long? AlternativeAttachmentId { get; set; }
			
			// Section 3 fields
			public OpenOrderRequestStopCheckingStatusEnum? StopCheckingStatus { get; set; }
			public string? StopCheckingStatusComment { get; set; }
			
			// Re-stop fields
			public int? ReStopTypeEnum { get; set; }
			public long? ReStopOperatorId { get; set; }
			public string? ReStopCommentValue { get; set; }
		}

		public class SaveAttachmentRequest
		{
			public long? Id { get; set; }
			public long OpenOrderRequestId { get; set; }
			public OpenOrderRequestAttachmentFileTypeEnum? FileType { get; set; }
			public string? Comment { get; set; }
			public long? AttachmentId { get; set; }
		}

		public class LinkVpisRequest
		{
			public long OpenOrderRequestId { get; set; }
			public long? ProjectId { get; set; }
			public List<long>? SelectedVpisIds { get; set; }
		}

		#endregion
	}
}
