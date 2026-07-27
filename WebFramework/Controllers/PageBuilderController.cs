using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Data.Services;
using Entities.Base.DataTable;
using Entities.Base.System;
using Entities.Base.System.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebFramework.Controllers;

[Route("Panel/System/[controller]")]
[ApiController]
[ApiResultFilter]
[ControllerInfo("صفحه‌ساز", typeof(PageDefinition))]
public class PageBuilderController(IUnitOfWork unitOfWork, IPageBuilderService pageBuilderService) : BaseController
{
	[HttpGet("[action]")]
	[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
	public IActionResult List()
	{
		return View(@"/Views/Panel/System/PageBuilder/List.cshtml");
	}

	[HttpGet("[action]")]
	[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
	public IActionResult New()
	{
		return View(@"/Views/Panel/System/PageBuilder/Edit.cshtml", new PageDefinition
		{
			SourceType = PageSourceTypeEnum.Manual,
			PageKind = PageKindEnum.Custom,
			Content = "@* New page *@\r\n<div></div>"
		});
	}

	[HttpGet("[action]")]
	[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
	public async Task<IActionResult> Edit(long? id, CancellationToken cn)
	{
		if (id == null || id == 0)
			return New();

		var page = await pageBuilderService.GetByIdAsync(id.Value, cn);
		if (page == null)
			return NotFound();

		return View(@"/Views/Panel/System/PageBuilder/Edit.cshtml", page);
	}

	[HttpPost("[action]")]
	[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
	public async Task<IActionResult> Save([FromBody] PageDefinition page, CancellationToken cn)
	{
		if (string.IsNullOrWhiteSpace(page.ViewPath))
			return BadRequest(new { success = false, message = "مسیر View الزامی است" });

		if (string.IsNullOrWhiteSpace(page.DisplayName))
			return BadRequest(new { success = false, message = "عنوان نمایشی الزامی است" });

		PageDefinition saved;
		if (page.Id == null || page.Id == 0)
			saved = await pageBuilderService.CreateAsync(page, cn);
		else
			saved = await pageBuilderService.UpdateAsync(page, cn);

		return Ok(new { success = true, id = saved.Id, message = "صفحه با موفقیت ذخیره شد" });
	}

	[HttpPost("[action]")]
	[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
	public async Task<IActionResult> Delete([FromBody] DeletePageRequest request, CancellationToken cn)
	{
		await pageBuilderService.DeleteAsync(request.Id, cn);
		return Ok(new { success = true, message = "صفحه حذف شد" });
	}

	[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
	[HttpPost("[action]")]
	public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
	{
		return Ok(await unitOfWork.Repository<PageDefinition>().FetchDataAsync(request, cn));
	}
}

public sealed class DeletePageRequest
{
	public long Id { get; set; }
}
