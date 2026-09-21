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
	[ControllerInfo("دوره‌های آموزشی", typeof(Course))]
	public class CourseController(IUnitOfWork unitOfWork, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		private void ValidateCourse(Course course)
		{
			// R3 — Private courses require a company.
			if (course.ExecutingMethod == CourseExecutingMethodEnum.Private && course.CompanyId == null)
				throw new Exception("برای دوره اختصاصی انتخاب شرکت الزامی است.");

			if (course.DurationInMinute == null)
				throw new Exception("مدت (دقیقه) الزامی است.");
			if (course.LocationId == null)
				throw new Exception("محل برگزاری الزامی است.");
			if (course.Status == null)
				throw new Exception("وضعیت دوره الزامی است.");
			if (course.FinancialStatus == null)
				throw new Exception("وضعیت مالی الزامی است.");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(Course course, CancellationToken cn)
		{
			if (course.Id == null || course.Id == 0)
			{
				return await Add(course, cn);
			}
			var exist = await unitOfWork.Repository<Course>().TableNoTracking.AnyAsync(c => c.Id == course.Id);
			if (exist)
			{
				return await Update(course, cn);
			}
			return await Add(course, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(Course course, CancellationToken cn)
		{
			ValidateCourse(course);
			var entity = await unitOfWork.Repository<Course>().SaveAsync(course, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(Course course, CancellationToken cn)
		{
			ValidateCourse(course);
			var entity = await unitOfWork.Repository<Course>().UpdateAsync(course, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<Course>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<Course>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<Course>().TableNoTracking
					.Include(c => c.CourseBank)
					.Include(c => c.Teacher)
					.Include(c => c.SecondTeacher)
					.Include(c => c.Agent)
					.Include(c => c.Company)
					.Include(c => c.RelatedUnit)
					.Include(c => c.Location)
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Trn\Course\Edit.cshtml", entity);
			}
			var newEntity = new Course();
			return View(@"\Views\Panel\Trn\Course\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New(long? courseBankId)
		{
			var newEntity = new Course();
			if (courseBankId != null && courseBankId != 0)
			{
				newEntity.CourseBankId = courseBankId.Value;
				newEntity.CourseBank = unitOfWork.Repository<CourseBank>().TableNoTracking
					.FirstOrDefault(c => c.Id == courseBankId);
			}
			return View(@"\Views\Panel\Trn\Course\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Trn\Course\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<Course>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<Course>().FetchDataAsync(request, cn));
		}
	}
}
