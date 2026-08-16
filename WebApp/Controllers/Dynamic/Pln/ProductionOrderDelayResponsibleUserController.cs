using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Pln;
using Entities.App.Pln.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic.Pln
{
	[Route("Panel/Pln/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("کاربران تاخیرات سفارش ساخت", typeof(ProductionOrderDelayResponsibleUser))]
	public class ProductionOrderDelayResponsibleUserController(IUnitOfWork unitOfWork) : BaseController
	{
		[HttpGet("[action]")]
		[ActionDisplayName("صفحه تنظیم کاربران", ActionAccessType.View, ActionAccessItemType.Custom)]
		public IActionResult Index()
		{
			return View(@"\Views\Panel\Pln\ProductionOrderDelayResponsibleUser\Index.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> ListUserBy(DelayResponsibleEnum delayResponsible, CancellationToken cn)
		{
			var data = await unitOfWork.Repository<ProductionOrderDelayResponsibleUser>()
				.TableNoTracking
				.FirstOrDefaultAsync(c => c.DelayResponsible == delayResponsible, cn);

			return Ok(data ?? new ProductionOrderDelayResponsibleUser
			{
				DelayResponsible = delayResponsible
			});
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(ProductionOrderDelayResponsibleUser model, CancellationToken cn)
		{
			model.UserIds ??= new();
			model.UserNames ??= new();
			model.UsersEmail ??= new();

			var repo = unitOfWork.Repository<ProductionOrderDelayResponsibleUser>();
			var existing = await repo.Table
				.FirstOrDefaultAsync(c => c.DelayResponsible == model.DelayResponsible, cn);

			if (existing == null)
			{
				model.Id = null;
				var entity = await repo.SaveAsync(model, cn, true);
				return Ok(entity);
			}

			existing.UserIds = model.UserIds;
			existing.UserNames = model.UserNames;
			existing.UsersEmail = model.UsersEmail;
			var updated = await repo.UpdateAsync(existing, cn, true);
			return Ok(updated);
		}
	}
}
