using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Trn;
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
	[ControllerInfo("ارزیابی دوره", typeof(CourseEvaluation))]
	public class CourseEvaluationController(IUnitOfWork unitOfWork, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(CourseEvaluation courseEvaluation, CancellationToken cn)
		{
			if (courseEvaluation.Id == null || courseEvaluation.Id == 0)
			{
				return await Add(courseEvaluation, cn);
			}
			var exist = await unitOfWork.Repository<CourseEvaluation>().TableNoTracking.AnyAsync(c => c.Id == courseEvaluation.Id);
			if (exist)
			{
				return await Update(courseEvaluation, cn);
			}
			return await Add(courseEvaluation, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(CourseEvaluation courseEvaluation, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<CourseEvaluation>().SaveAsync(courseEvaluation, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(CourseEvaluation courseEvaluation, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<CourseEvaluation>().UpdateAsync(courseEvaluation, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<CourseEvaluation>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<CourseEvaluation>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id, long? courseId)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<CourseEvaluation>().TableNoTracking
					.Include(c => c.Participant)
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Trn\CourseEvaluation\Edit.cshtml", entity);
			}
			var newEntity = new CourseEvaluation { CourseId = courseId ?? 0 };
			return View(@"\Views\Panel\Trn\CourseEvaluation\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New(long? courseId)
		{
			var newEntity = new CourseEvaluation { CourseId = courseId ?? 0 };
			return View(@"\Views\Panel\Trn\CourseEvaluation\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست ارزیابی‌های دوره", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ListByCourseId(long courseId)
		{
			if (courseId == 0)
				throw new Exception("شناسه دوره نمیتواند خالی باشد.");

			var course = unitOfWork.Repository<Course>().TableNoTracking
				.FirstOrDefault(c => c.Id == courseId);

			var model = new CourseEvaluationListByParentViewModel
			{
				CourseId = courseId,
				CourseTitle = course != null ? course.TrainingCode ?? string.Empty : string.Empty
			};

			return View(@"\Views\Panel\Trn\CourseEvaluation\ListByCourseId.cshtml", model);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت ارزیابی‌های دوره", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetListByParentId(long? courseId, CancellationToken cn)
		{
			if (courseId == null || courseId == 0)
				return BadRequest("شناسه دوره نمیتواند خالی باشد.");

			var items = await unitOfWork.Repository<CourseEvaluation>()
				.TableNoTracking
				.Include(c => c.Participant)
				.Where(c => c.CourseId == courseId)
				.Select(c => new CourseEvaluationListItemViewModel
				{
					Id = c.Id,
					CourseId = c.CourseId,
					ParticipantId = c.ParticipantId,
					FirstName = c.Participant != null ? c.Participant.FirstName : null,
					LastName = c.Participant != null ? c.Participant.LastName : null,
					AverageScore = c.AverageScore,
					Comment = c.Comment,
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
				await unitOfWork.Repository<CourseEvaluation>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<CourseEvaluation>().FetchDataAsync(request, cn));
		}
	}

	public class CourseEvaluationListByParentViewModel
	{
		public long CourseId { get; set; }
		public string CourseTitle { get; set; } = string.Empty;
	}

	public class CourseEvaluationListItemViewModel
	{
		public long? Id { get; set; }
		public long? CourseId { get; set; }
		public long? ParticipantId { get; set; }
		public string? FirstName { get; set; }
		public string? LastName { get; set; }
		public decimal? AverageScore { get; set; }
		public string? Comment { get; set; }
		public string? CreatedOnShamsiDateTime { get; set; }
	}
}
