using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Edms;
using Entities.App.Edms.Enums;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Edms/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("ترانسمیتال", typeof(Transmital))]
	public class TransmitalController(IUnitOfWork unitOfWork, IPropertyIdentityService identityService, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(Transmital transmital, CancellationToken cn)
		{


			 
			if (transmital.Id == null || transmital.Id == 0)
			{
				return await Add(transmital, cn);
			}
			var exist = await unitOfWork.Repository<Transmital>().TableNoTracking.AnyAsync(c => c.Id == transmital.Id);
			if (exist)
			{
				return await Update(transmital, cn);
			}
			return await Add(transmital, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(Transmital transmital, CancellationToken cn)
		{
			var document = await unitOfWork.Repository<Document>().TableNoTracking
				.FirstOrDefaultAsync(d => d.Id == transmital.DocumentId, cn)
				?? throw new Exception("مدرک ترانسمیتال یافت نشد.");

			var isReplySheet = transmital.IsForReplySheet == true;
			if (!isReplySheet && !IsAdministrator)
			{
				if (!IsCurrentUserDcc())
					throw new Exception("فقط کاربر DCC مجاز به ترانسمیتال مدرک است.");
				if (document.Status != DocumentStatusEnums.ApprovedByDcc)
					throw new Exception("ترانسمیتال فقط برای مدرک تاییدشده توسط DCC مجاز است.");
			}

			var project = await unitOfWork.Repository<Project>()
							.TableNoTracking
							.FirstOrDefaultAsync(p => p.Id == transmital.ProjectId, cn);

			if (isReplySheet)
			{
				transmital.Number = transmital.DefinedByUserNumber;
				transmital.DefinedByUserNumber = null;
				if (string.IsNullOrEmpty(transmital.Comments))
					transmital.Comments = "is for reply sheet";
			}
			else if (project != null)
			{
				var transmitalCount = await unitOfWork.Repository<Transmital>()
					.TableNoTracking
					.CountAsync(c => !(c.IsForReplySheet ?? false) && c.ProjectId == transmital.ProjectId, cn);
				var transmitalNo = string.Concat("-", (transmitalCount + 1).ToString().PadLeft(4, '0'));
				transmital.Number = string.Concat(project.TransmissionPrefix, transmitalNo);
			}

			var entity = await unitOfWork.Repository<Transmital>().SaveAsync(transmital, cn, true);
			await AddNotReviewCommentIfNeeded(document, cn);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(Transmital transmital, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<Transmital>().UpdateAsync(transmital, cn, true);
			var document = await unitOfWork.Repository<Document>().TableNoTracking
				.FirstOrDefaultAsync(d => d.Id == entity.DocumentId, cn);
			if (document != null)
				await AddNotReviewCommentIfNeeded(document, cn);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<Transmital>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<Transmital>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<Transmital>().TableNoTracking
					.Include(c => c.Project)
					.Include(c => c.Document)
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Edms\Transmital\Edit.cshtml", entity);
			}
			var newEntity = new Transmital();
			return View(@"\Views\Panel\Edms\Transmital\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var newEntity = new Transmital();
			return View(@"\Views\Panel\Edms\Transmital\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Edms\Transmital\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<Transmital>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<Transmital>().FetchDataAsync(request, cn));
		}


		private const long DccRoleId = 200000;
		private const string DccRoleName = "EdmsDocumentsDccUsers";

		private bool IsCurrentUserDcc() =>
			CurrentUserHasRole(DccRoleId) || CurrentUserHasRole(DccRoleName);

		private async Task AddNotReviewCommentIfNeeded(Document document, CancellationToken ct)
		{
			if (document.Id is not > 0)
				return;
			if (document.Status != DocumentStatusEnums.ApprovedByDcc)
				return;

			var already = await unitOfWork.Repository<DocumentComment>().TableNoTracking
				.AnyAsync(c => c.DocumentId == document.Id && c.Status == DocumentStatusEnums.NotReview, ct);
			if (already)
				return;

			await unitOfWork.Repository<DocumentComment>().AddAsync(new DocumentComment
			{
				DocumentId = document.Id.Value,
				Status = DocumentStatusEnums.NotReview,
				Comment = "ایجاد شده از ترانسمیتال"
			}, ct, false);
		}
	}
}
