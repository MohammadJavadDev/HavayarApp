using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Cng;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Cng/PartPriceAttachment")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("پیوست قیمت قطعات", typeof(PartPriceAttachment))]
	public class CngPartPriceAttachmentController(IUnitOfWork unitOfWork) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(PartPriceAttachment model, CancellationToken cn)
		{
			if (model.PartPriceId == 0)
				return BadRequest("شناسه والد خالی است");
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(PartPriceAttachment model, CancellationToken cn)
		{
			if (model.PartPriceId == 0)
				return BadRequest("شناسه والد خالی است");

			var fileIds = ParseFileIds(model.FileIds);
			if (model.FileId is > 0 && !fileIds.Contains(model.FileId.Value))
				fileIds.Insert(0, model.FileId.Value);

			if (fileIds.Count == 0)
				return BadRequest("انتخاب فایل الزامی است");

			PartPriceAttachment? first = null;
			foreach (var fileId in fileIds)
			{
				var row = new PartPriceAttachment
				{
					PartPriceId = model.PartPriceId,
					FileId = fileId,
					Comment = model.Comment,
					HtsId = 0
				};
				var saved = await unitOfWork.Repository<PartPriceAttachment>().SaveAsync(row, cn, true);
				first ??= saved;
			}

			return Ok(first);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<PartPriceAttachment>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null) await unitOfWork.Repository<PartPriceAttachment>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New(long? partPriceId = null)
			=> View(@"\Views\Panel\Cng\PartPriceAttachment\Edit.cshtml", new PartPriceAttachment { PartPriceId = partPriceId ?? 0 });

		[HttpGet("[action]")]
		[ActionDisplayName("لیست وابسته", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ListByParentId(long partPriceId)
		{
			ViewBag.ParentId = partPriceId;
			return View(@"\Views\Panel\Cng\PartPriceAttachment\ListByParentId.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت لیست وابسته", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetListByParentId(long? partPriceId, CancellationToken cn)
		{
			if (partPriceId == null || partPriceId == 0)
				return BadRequest("شناسه والد خالی است");

			var items = await unitOfWork.Repository<PartPriceAttachment>().TableNoTracking
				.Where(c => c.PartPriceId == partPriceId)
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
