using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Cng;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Cng/RepairsAttachment")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("پیوست تعمیرات CNG", typeof(RepairsAttachment))]
	public class CngRepairsAttachmentController(IUnitOfWork unitOfWork) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(RepairsAttachment model, CancellationToken cn)
		{
			if (model.RepairsId == 0)
				return BadRequest("شناسه والد خالی است");
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(RepairsAttachment model, CancellationToken cn)
		{
			if (model.RepairsId == 0)
				return BadRequest("شناسه والد خالی است");

			var fileIds = ParseFileIds(model.FileIds);
			if (model.FileId is > 0 && !fileIds.Contains(model.FileId.Value))
				fileIds.Insert(0, model.FileId.Value);

			if (fileIds.Count == 0)
				return BadRequest("انتخاب فایل الزامی است");

			RepairsAttachment? first = null;
			foreach (var fileId in fileIds)
			{
				var row = new RepairsAttachment
				{
					RepairsId = model.RepairsId,
					FileId = fileId,
					Comment = model.Comment,
					HtsId = 0
				};
				var saved = await unitOfWork.Repository<RepairsAttachment>().SaveAsync(row, cn, true);
				first ??= saved;
			}

			return Ok(first);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<RepairsAttachment>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null)
				await unitOfWork.Repository<RepairsAttachment>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New(long? repairsId = null)
			=> View(@"\Views\Panel\Cng\RepairsAttachment\Edit.cshtml",
				new RepairsAttachment { RepairsId = repairsId ?? 0 });

		[HttpGet("[action]")]
		[ActionDisplayName("لیست وابسته", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ListByParentId(long repairsId)
		{
			ViewBag.ParentId = repairsId;
			return View(@"\Views\Panel\Cng\RepairsAttachment\ListByParentId.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت لیست وابسته", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetListByParentId(long? repairsId, CancellationToken cn)
		{
			if (repairsId == null || repairsId == 0)
				return BadRequest("شناسه والد خالی است");

			var items = await unitOfWork.Repository<RepairsAttachment>().TableNoTracking
				.Where(c => c.RepairsId == repairsId)
				.Select(c => new
				{
					c.Id,
					c.FileId,
					FileName = c.File != null ? c.File.OriginalName : null,
					FileSize = c.File != null ? c.File.Size : (long?)null,
					c.CreatedByName,
					c.CreatedOnShamsiDateTime,
					c.CreatedOnMiladiDateTime,
					c.Comment
				})
				.ToListAsync(cn);
			return Ok(items);
		}

		[HttpGet("View")]
		[ActionDisplayName("مشاهده", ActionAccessType.Api, ActionAccessItemType.Show)]
		public async Task<IActionResult> ViewAttachment(long id, CancellationToken cn)
		{
			var fileId = await unitOfWork.Repository<RepairsAttachment>().TableNoTracking
				.Where(c => c.Id == id)
				.Select(c => c.FileId)
				.FirstOrDefaultAsync(cn);
			if (fileId == null || fileId == 0)
				return NotFound("فایل یافت نشد");
			return Redirect($"/File/download/{fileId}");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<RepairsAttachment>().FetchDataAsync(request, cn));

		private static List<long> ParseFileIds(string? raw)
		{
			var result = new List<long>();
			if (string.IsNullOrWhiteSpace(raw)) return result;
			foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			{
				if (long.TryParse(part, out var id) && id > 0 && !result.Contains(id))
					result.Add(id);
			}
			return result;
		}
	}
}
