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
	[ControllerInfo("مشتریان حقیقی (شرکت‌کنندگان)", typeof(Participant))]
	public class ParticipantController(IUnitOfWork unitOfWork, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(Participant participant, CancellationToken cn)
		{
			if (participant.Id == null || participant.Id == 0)
			{
				return await Add(participant, cn);
			}
			var exist = await unitOfWork.Repository<Participant>().TableNoTracking.AnyAsync(c => c.Id == participant.Id);
			if (exist)
			{
				return await Update(participant, cn);
			}
			return await Add(participant, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(Participant participant, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<Participant>().SaveAsync(participant, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(Participant participant, CancellationToken cn)
		{
			await PreserveHiddenFields(participant, cn);
			var entity = await unitOfWork.Repository<Participant>().UpdateAsync(participant, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<Participant>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<Participant>().DeleteAsync(model, cn, true);
			return Ok();
		}

		// Similar-name confirm (legacy CheckSellerExist): Contains on first and last name.
		// The page asks before insert; cancel does not save.
		[HttpGet("[action]")]
		[ActionDisplayName("بررسی تکراری بودن نام", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> CheckDuplicateName(string? firstName, string? lastName, CancellationToken cn)
		{
			firstName = firstName?.Trim();
			lastName = lastName?.Trim();
			if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
				return Ok(new { matches = Array.Empty<object>(), hasMore = false });

			var rows = await unitOfWork.Repository<Participant>().TableNoTracking
				.Where(p => p.FirstName != null && p.LastName != null
					&& p.FirstName.Contains(firstName) && p.LastName.Contains(lastName))
				.OrderBy(p => p.FirstName)
				.ThenBy(p => p.LastName)
				.Select(p => new
				{
					fullName = p.FirstName + " " + p.LastName,
					company = p.Company != null ? p.Company.Title : null
				})
				.Take(101)
				.ToListAsync(cn);

			return Ok(new
			{
				matches = rows.Take(100).ToList(),
				hasMore = rows.Count > 100
			});
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			FillCompanies();
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<Participant>().TableNoTracking
					.Include(p => p.PhotoFile)
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Trn\Participant\Edit.cshtml", entity);
			}
			var newEntity = new Participant();
			return View(@"\Views\Panel\Trn\Participant\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			FillCompanies();
			var newEntity = new Participant();
			return View(@"\Views\Panel\Trn\Participant\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Trn\Participant\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<Participant>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<Participant>().FetchDataAsync(request, cn));
		}

		private void FillCompanies()
		{
			ViewBag.Companies = unitOfWork.Repository<Company>().TableNoTracking
				.OrderBy(c => c.Title)
				.Select(c => new Company { Id = c.Id, Title = c.Title })
				.ToList();
		}

		private async Task PreserveHiddenFields(Participant participant, CancellationToken cn)
		{
			if (participant.Id == null || participant.Id == 0)
				return;

			var old = await unitOfWork.Repository<Participant>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == participant.Id, cn);
			if (old == null)
				return;

			participant.JobPosition = old.JobPosition;
			participant.FieldOfStudy = old.FieldOfStudy;
			participant.Description = old.Description;
			participant.PartyId = old.PartyId;
		}
	}
}
