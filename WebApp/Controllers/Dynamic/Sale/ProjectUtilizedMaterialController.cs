using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Sale;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Sale/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("اقلام مصرفی در پروژه", typeof(ProjectUtilizedMaterial))]
	public class ProjectUtilizedMaterialController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		private const string ImportOnlyMessage = "ردیف‌های اقلام مصرفی از اسناد انبار وارد می‌شوند و درج/حذف دستی ندارند.";

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(ProjectUtilizedMaterial model, CancellationToken cn)
		{
			if (model.Id == null || model.Id == 0)
				return BadRequest(ImportOnlyMessage);
			if (!await unitOfWork.Repository<ProjectUtilizedMaterial>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return BadRequest("ردیف یافت نشد");
			return await Update(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public Task<IActionResult> Add(ProjectUtilizedMaterial model, CancellationToken cn)
			=> Task.FromResult<IActionResult>(BadRequest(ImportOnlyMessage));

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(ProjectUtilizedMaterial model, CancellationToken cn)
		{
			if (model.Id == null || model.Id == 0)
				return BadRequest(ImportOnlyMessage);
			if (model.ReplacedPartId == null || model.ReplacedPartId == 0)
				return BadRequest("کالای جایگزین نمی‌تواند خالی باشد");
			if (model.ReplacedQty == 0)
				return BadRequest("تعداد/مقدار جایگزین نمی‌تواند خالی باشد");

			var repo = unitOfWork.Repository<ProjectUtilizedMaterial>();
			var existing = await repo.Table.FirstOrDefaultAsync(c => c.Id == model.Id, cn);
			if (existing == null)
				return BadRequest("ردیف یافت نشد");

			existing.ReplacedPartId = model.ReplacedPartId;
			existing.ReplacedQty = model.ReplacedQty;
			existing.Comment = model.Comment;
			existing.ReplacedPart = null;
			existing.Part = null;
			existing.DL = null;
			existing.DLType = null;

			return Ok(await repo.UpdateAsync(existing, cn, true));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public IActionResult Delete(long id)
			=> BadRequest(ImportOnlyMessage);

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			var entity = LoadForEdit(id) ?? new ProjectUtilizedMaterial();
			return View(@"\Views\Panel\Sale\ProjectUtilizedMaterial\Edit.cshtml", entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
			=> View(@"\Views\Panel\Sale\ProjectUtilizedMaterial\Edit.cshtml", new ProjectUtilizedMaterial());

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Sale\ProjectUtilizedMaterial\List.cshtml");

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<ProjectUtilizedMaterial>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex) { return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message); }
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<ProjectUtilizedMaterial>().FetchDataAsync(request, cn));

		private ProjectUtilizedMaterial? LoadForEdit(long? id)
		{
			if (id == null || id == 0)
				return null;
			return unitOfWork.Repository<ProjectUtilizedMaterial>().TableNoTracking
				.Include(c => c.DL)
				.Include(c => c.Part)
				.Include(c => c.ReplacedPart)
				.FirstOrDefault(c => c.Id == id);
		}
	}
}
