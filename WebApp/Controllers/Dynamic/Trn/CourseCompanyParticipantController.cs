using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Trn;
using Entities.App.Trn.Enums;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Trn/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("شرکت‌کنندگان دوره (اختصاصی)", typeof(CourseCompanyParticipant))]
	public class CourseCompanyParticipantController(IUnitOfWork unitOfWork, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		// First layer of the two-layer enrollment guard (R4c/R4d + track guard).
		// The second layer is CourseCompanyParticipantAction.BeforeAdd, which runs regardless of caller.
		private async Task ValidateEnrollmentAsync(CourseCompanyParticipant participant, CancellationToken cn)
		{
			var course = unitOfWork.Repository<Course>().TableNoTracking
				.FirstOrDefault(c => c.Id == participant.CourseId);
			if (course == null)
				throw new Exception("دوره یافت نشد.");

			if (course.IsPublicParticipantTrack())
				throw new Exception("ثبت‌نام اختصاصی فقط برای دوره‌های اختصاصی مجاز است.");

			var remaining = course.RemainingCapacity ?? course.Capacity;
			if (remaining <= 0)
				throw new Exception("ظرفیت دوره تکمیل است و امکان ثبت‌نام وجود ندارد.");

			var duplicate =
				await unitOfWork.Repository<CourseParticipant>().TableNoTracking
					.AnyAsync(p => p.CourseId == participant.CourseId && p.ParticipantId == participant.ParticipantId, cn)
				|| await unitOfWork.Repository<CourseCompanyParticipant>().TableNoTracking
					.AnyAsync(p => p.CourseId == participant.CourseId && p.ParticipantId == participant.ParticipantId, cn);
			if (duplicate)
				throw new Exception("این شرکت‌کننده قبلاً در این دوره ثبت‌نام شده است.");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(CourseCompanyParticipant courseCompanyParticipant, CancellationToken cn)
		{
			if (courseCompanyParticipant.Id == null || courseCompanyParticipant.Id == 0)
			{
				return await Add(courseCompanyParticipant, cn);
			}
			var exist = await unitOfWork.Repository<CourseCompanyParticipant>().TableNoTracking.AnyAsync(c => c.Id == courseCompanyParticipant.Id);
			if (exist)
			{
				return await Update(courseCompanyParticipant, cn);
			}
			return await Add(courseCompanyParticipant, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(CourseCompanyParticipant courseCompanyParticipant, CancellationToken cn)
		{
			await ValidateEnrollmentAsync(courseCompanyParticipant, cn);
			var entity = await unitOfWork.Repository<CourseCompanyParticipant>().SaveAsync(courseCompanyParticipant, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(CourseCompanyParticipant courseCompanyParticipant, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<CourseCompanyParticipant>().UpdateAsync(courseCompanyParticipant, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<CourseCompanyParticipant>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<CourseCompanyParticipant>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id, long? courseId)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<CourseCompanyParticipant>().TableNoTracking
					.Include(c => c.Participant)
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Trn\CourseCompanyParticipant\Edit.cshtml", entity);
			}
			var newEntity = new CourseCompanyParticipant { CourseId = courseId ?? 0 };
			return View(@"\Views\Panel\Trn\CourseCompanyParticipant\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New(long? courseId)
		{
			var newEntity = new CourseCompanyParticipant { CourseId = courseId ?? 0 };
			return View(@"\Views\Panel\Trn\CourseCompanyParticipant\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست شرکت‌کنندگان دوره", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ListByCourseId(long courseId)
		{
			if (courseId == 0)
				throw new Exception("شناسه دوره نمیتواند خالی باشد.");

			var course = unitOfWork.Repository<Course>().TableNoTracking
				.FirstOrDefault(c => c.Id == courseId);

			var model = new CourseCompanyParticipantListByParentViewModel
			{
				CourseId = courseId,
				CourseTitle = course != null ? course.TrainingCode ?? string.Empty : string.Empty
			};

			return View(@"\Views\Panel\Trn\CourseCompanyParticipant\ListByCourseId.cshtml", model);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت شرکت‌کنندگان دوره", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetListByParentId(long? courseId, CancellationToken cn)
		{
			if (courseId == null || courseId == 0)
				return BadRequest("شناسه دوره نمیتواند خالی باشد.");

			var items = await unitOfWork.Repository<CourseCompanyParticipant>()
				.TableNoTracking
				.Include(c => c.Participant)
				.Where(c => c.CourseId == courseId)
				.Select(c => new CourseCompanyParticipantListItemViewModel
				{
					Id = c.Id,
					CourseId = c.CourseId,
					ParticipantId = c.ParticipantId,
					FirstName = c.Participant != null ? c.Participant.FirstName : null,
					LastName = c.Participant != null ? c.Participant.LastName : null,
					NationalCode = c.Participant != null ? c.Participant.NationalCode : null,
					Phone = c.Participant != null ? c.Participant.Phone : null,
					Mobile = c.Participant != null ? c.Participant.Mobile : null,
					Score = c.Score,
					ParticipantRegisterStatus = c.ParticipantRegisterStatus,
					CreatedByName = c.CreatedByName,
					CreatedOnShamsiDateTime = c.CreatedOnShamsiDateTime
				})
				.ToListAsync(cn);

			return Ok(items);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<CourseCompanyParticipant>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<CourseCompanyParticipant>().FetchDataAsync(request, cn));
		}
	}

	public class CourseCompanyParticipantListByParentViewModel
	{
		public long CourseId { get; set; }
		public string CourseTitle { get; set; } = string.Empty;
	}

	public class CourseCompanyParticipantListItemViewModel
	{
		public long? Id { get; set; }
		public long? CourseId { get; set; }
		public long? ParticipantId { get; set; }
		public string? FirstName { get; set; }
		public string? LastName { get; set; }
		public string? NationalCode { get; set; }
		public string? Phone { get; set; }
		public string? Mobile { get; set; }
		public int? Score { get; set; }
		public ParticipantRegisterStatusEnum? ParticipantRegisterStatus { get; set; }
		public string? CreatedByName { get; set; }
		public string? CreatedOnShamsiDateTime { get; set; }
	}
}
