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
	[ControllerInfo("پیوست مدرس", typeof(TeacherBankAttachment))]
	public class TeacherBankAttachmentController(IUnitOfWork unitOfWork) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(TeacherBankAttachment attachment, CancellationToken cn)
		{
			if (attachment.Id == null || attachment.Id == 0)
			{
				return await Add(attachment, cn);
			}
			var exist = await unitOfWork.Repository<TeacherBankAttachment>().TableNoTracking.AnyAsync(c => c.Id == attachment.Id);
			if (exist)
			{
				return await Update(attachment, cn);
			}
			return await Add(attachment, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(TeacherBankAttachment attachment, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<TeacherBankAttachment>().SaveAsync(attachment, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(TeacherBankAttachment attachment, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<TeacherBankAttachment>().UpdateAsync(attachment, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<TeacherBankAttachment>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<TeacherBankAttachment>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id, long? teacherBankId)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<TeacherBankAttachment>().TableNoTracking
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Trn\TeacherBankAttachment\Edit.cshtml", entity);
			}
			var newEntity = new TeacherBankAttachment { TeacherBankId = teacherBankId ?? 0 };
			return View(@"\Views\Panel\Trn\TeacherBankAttachment\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New(long? teacherBankId)
		{
			var newEntity = new TeacherBankAttachment { TeacherBankId = teacherBankId ?? 0 };
			return View(@"\Views\Panel\Trn\TeacherBankAttachment\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Trn\TeacherBankAttachment\List.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست پیوست‌های مدرس", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ListByTeacherBankId(long teacherBankId)
		{
			if (teacherBankId == 0)
				throw new Exception("شناسه مدرس نمیتواند خالی باشد.");

			var teacher = unitOfWork.Repository<TeacherBank>().TableNoTracking
				.FirstOrDefault(c => c.Id == teacherBankId);

			var model = new TeacherBankAttachmentListByParentViewModel
			{
				TeacherBankId = teacherBankId,
				TeacherName = teacher != null ? $"{teacher.FirstName} {teacher.LastName}".Trim() : string.Empty
			};

			return View(@"\Views\Panel\Trn\TeacherBankAttachment\ListByTeacherBankId.cshtml", model);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت پیوست‌های مدرس", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetListByParentId(long? teacherBankId, CancellationToken cn)
		{
			if (teacherBankId == null || teacherBankId == 0)
				return BadRequest("شناسه مدرس نمیتواند خالی باشد.");

			var items = await unitOfWork.Repository<TeacherBankAttachment>()
				.TableNoTracking
				.Where(c => c.TeacherBankId == teacherBankId)
				.Select(c => new TeacherBankAttachmentListItemViewModel
				{
					Id = c.Id,
					TeacherBankId = c.TeacherBankId,
					Title = c.Title,
					FileId = c.FileId,
					FileName = c.File != null ? c.File.OriginalName : null
				})
				.ToListAsync(cn);

			return Ok(items);
		}

		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		[HttpPost("[action]")]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
		{
			return Ok(await unitOfWork.Repository<TeacherBankAttachment>().FetchDataAsync(request, cn));
		}
	}

	public class TeacherBankAttachmentListByParentViewModel
	{
		public long TeacherBankId { get; set; }
		public string TeacherName { get; set; } = string.Empty;
	}

	public class TeacherBankAttachmentListItemViewModel
	{
		public long? Id { get; set; }
		public long? TeacherBankId { get; set; }
		public string? Title { get; set; }
		public long? FileId { get; set; }
		public string? FileName { get; set; }
	}
}
