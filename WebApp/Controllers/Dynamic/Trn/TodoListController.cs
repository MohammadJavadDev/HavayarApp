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
	[ControllerInfo("تاریخچه مذاکرات / پیگیری", typeof(TodoList))]
	public class TodoListController(IUnitOfWork unitOfWork, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(TodoList todoList, CancellationToken cn)
		{
			if (todoList.Id == null || todoList.Id == 0)
			{
				return await Add(todoList, cn);
			}
			var exist = await unitOfWork.Repository<TodoList>().TableNoTracking.AnyAsync(c => c.Id == todoList.Id);
			if (exist)
			{
				return await Update(todoList, cn);
			}
			return await Add(todoList, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(TodoList todoList, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<TodoList>().SaveAsync(todoList, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(TodoList todoList, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<TodoList>().UpdateAsync(todoList, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<TodoList>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<TodoList>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id, long? companyId)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<TodoList>().TableNoTracking
					.Include(c => c.Company)
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Trn\TodoList\Edit.cshtml", entity);
			}
			var newEntity = new TodoList { CompanyId = companyId };
			if (companyId != null && companyId != 0)
			{
				newEntity.Company = unitOfWork.Repository<Company>().TableNoTracking
					.FirstOrDefault(c => c.Id == companyId);
			}
			return View(@"\Views\Panel\Trn\TodoList\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New(long? companyId)
		{
			var newEntity = new TodoList { CompanyId = companyId };
			if (companyId != null && companyId != 0)
			{
				newEntity.Company = unitOfWork.Repository<Company>().TableNoTracking
					.FirstOrDefault(c => c.Id == companyId);
			}
			return View(@"\Views\Panel\Trn\TodoList\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Trn\TodoList\List.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست تاریخچه مذاکرات شرکت", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ListByCompanyId(long companyId)
		{
			if (companyId == 0)
				throw new Exception("شناسه شرکت نمیتواند خالی باشد.");

			var company = unitOfWork.Repository<Company>().TableNoTracking
				.FirstOrDefault(c => c.Id == companyId);

			var model = new TodoListListByParentViewModel
			{
				CompanyId = companyId,
				CompanyTitle = company != null ? company.Title ?? string.Empty : string.Empty
			};

			return View(@"\Views\Panel\Trn\TodoList\ListByCompanyId.cshtml", model);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت تاریخچه مذاکرات شرکت", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetListByParentId(long? companyId, CancellationToken cn)
		{
			if (companyId == null || companyId == 0)
				return BadRequest("شناسه شرکت نمیتواند خالی باشد.");

			var items = await unitOfWork.Repository<TodoList>()
				.TableNoTracking
				.Where(c => c.CompanyId == companyId)
				.OrderByDescending(c => c.TodoMiladiDate)
				.Select(c => new TodoListItemViewModel
				{
					Id = c.Id,
					CompanyId = c.CompanyId,
					CompanyTitle = c.Company != null ? c.Company.Title : null,
					Title = c.Title,
					TodoShamsiDate = c.TodoShamsiDate,
					CreatedByName = c.CreatedByName
				})
				.ToListAsync(cn);

			return Ok(items);
		}

		// Standalone Operation-menu list: legacy rule — shows only rows created by the current user,
		// while the embedded company grid (GetListByParentId) shows all rows of that company.
		[HttpGet("[action]")]
		[ActionDisplayName("دریافت پیگیری‌های من", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetMyList(CancellationToken cn)
		{
			var currentUserId = CurrentUserId;

			var items = await unitOfWork.Repository<TodoList>()
				.TableNoTracking
				.Where(c => c.CreatedById == currentUserId)
				.OrderByDescending(c => c.TodoMiladiDate)
				.Select(c => new TodoListItemViewModel
				{
					Id = c.Id,
					CompanyId = c.CompanyId,
					CompanyTitle = c.Company != null ? c.Company.Title : null,
					Title = c.Title,
					TodoShamsiDate = c.TodoShamsiDate,
					CreatedByName = c.CreatedByName
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
				await unitOfWork.Repository<TodoList>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<TodoList>().FetchDataAsync(request, cn));
		}
	}

	public class TodoListListByParentViewModel
	{
		public long CompanyId { get; set; }
		public string CompanyTitle { get; set; } = string.Empty;
	}

	public class TodoListItemViewModel
	{
		public long? Id { get; set; }
		public long? CompanyId { get; set; }
		public string? CompanyTitle { get; set; }
		public string? Title { get; set; }
		public string? TodoShamsiDate { get; set; }
		public string? CreatedByName { get; set; }
	}
}
