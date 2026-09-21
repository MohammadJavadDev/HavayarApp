using Data.Contracts;
using Data.Contracts.Actions;
using Entities.App.Sale;

namespace WebApp.Actions.Sale
{
	/// <summary>
	/// معادل FK CASCADE در HTS روی Sale_ResponsibleZoneCustomer.ResponsibleZoneId.
	/// ادغام تکراری مسئول+استان+منطقه (HTS DoOperation هنگام تغییر مسئول) در
	/// <c>ResponsibleZoneController.Update</c> است چون ردیف باقی‌مانده باید به کلاینت برگردد.
	/// </summary>
	public class ResponsibleZoneAction(IUnitOfWork unitOfWork)
	{
		[EntityAction(typeof(ResponsibleZone), EntityActionTrigger.BeforeDelete,
			"DeleteResponsibleZoneCustomers", "حذف مشتریان مسئول منطقه هنگام حذف سربرگ", Priority = 1)]
		public async Task DeleteCustomers(ResponsibleZone entity, CancellationToken ct)
		{
			if (entity.Id == null || entity.Id == 0)
				return;

			await unitOfWork.Repository<ResponsibleZoneCustomer>()
				.DeleteWhereAsync(c => c.ResponsibleZoneId == entity.Id, ct);
		}
	}
}
