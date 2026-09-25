using Common.Utilities;
using Data.Contracts.Actions;
using Entities.App.Cng;

namespace WebApp.Actions.Cng
{
	/// <summary>
	/// معادل تریگر HTS UpdateCng_MaintenanceContractSomeInfoTrigger
	/// و محاسبه تاریخ پایان در CngMaintenanceContractController.DoOperation.
	/// </summary>
	public class MaintenanceContractAction
	{
		[EntityAction(typeof(MaintenanceContract), EntityActionTrigger.BeforeSave,
			"MaintenanceContractComputeTotals", "محاسبه تاریخ پایان و مجموع قیمت قرارداد نگهداشت CNG", Priority = 1)]
		public Task BeforeSave(MaintenanceContract entity, CancellationToken ct)
		{
			if (entity.StartDate.HasValue && entity.ContractDurationInMonth > 0)
			{
				entity.EndDate = entity.StartDate.Value.AddMonths(entity.ContractDurationInMonth);
				entity.EndDateInText = entity.EndDate.Value.ToShamsiDate();
			}

			entity.TotalPrice = entity.MonthlyPrice * entity.ContractDurationInMonth;
			return Task.CompletedTask;
		}
	}
}
