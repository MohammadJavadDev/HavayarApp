using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Rpr;
using Entities.App.Sale;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Sale/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("داشبورد خدمات پس از فروش")]
	public class AfterSalesDashboardController(IUnitOfWork unitOfWork) : BaseController
	{
		[HttpGet("[action]")]
		[ActionDisplayName("داشبورد خدمات پس از فروش", ActionAccessType.View, ActionAccessItemType.Dashbord)]
		public IActionResult Report()
			=> View(@"\Views\Panel\Sale\AfterSalesDashboard\Report.cshtml");

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات داشبورد", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetData(CancellationToken cn)
		{
			var serviceRequestCount = await unitOfWork.Repository<ServiceRequest>().TableNoTracking.CountAsync(cn);
			var waitingCount = await unitOfWork.Repository<ServiceRequest>().TableNoTracking.CountAsync(x => x.IsWaitingToSend == true, cn);
			var missionCount = await unitOfWork.Repository<Mission>().TableNoTracking.CountAsync(cn);
			var repairCount = await unitOfWork.Repository<RepairRequest>().TableNoTracking.CountAsync(cn);
			var delayCount = await unitOfWork.Repository<RepairRequest>().TableNoTracking.CountAsync(x => x.HasDelay, cn);
			var collectionCount = await unitOfWork.Repository<CollectionClaim>().TableNoTracking.CountAsync(cn);

			return Ok(new
			{
				serviceRequestCount,
				waitingCount,
				missionCount,
				repairCount,
				delayCount,
				collectionCount
			});
		}
	}
}
