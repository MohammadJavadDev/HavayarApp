using Common.Utilities;
using Data.Contracts.Actions;
using Entities.App.Cng;

namespace WebApp.Actions.Cng
{
	/// <summary>
	/// معادل تریگر HTS UpdateCng_MaintenanceContractSomeInfoTrigger
	/// و محاسبه تاریخ پایان در CngMaintenanceContractController.DoOperation.
	/// BeforeSave هم برای درج و هم برای ویرایش اجرا می‌شود (Repository.AddAsync / UpdateAsync).
	/// </summary>
	public class MaintenanceContractAction
	{
		[EntityAction(typeof(MaintenanceContract), EntityActionTrigger.BeforeSave,
			"MaintenanceContractComputeTotals", "محاسبه تاریخ پایان و مجموع قیمت قرارداد نگهداشت CNG", Priority = 1)]
		public Task BeforeSave(MaintenanceContract entity, CancellationToken ct)
		{
			Apply(entity);
			return Task.CompletedTask;
		}

		/// <summary>
		/// مثل HTS: متن شمسی تاریخ شروع منبع است. پایان = AddMonths میلادی، نه جمع ماه شمسی.
		/// مجموع قیمت = قیمت ماهانه × مدت.
		/// </summary>
		private static void Apply(MaintenanceContract entity)
		{
			if (!string.IsNullOrWhiteSpace(entity.StartDateInText))
			{
				var text = entity.StartDateInText.Trim();
				if (text.Count(char.IsDigit) >= 8)
					entity.StartDate = text.ToMiladiDate().Date;
			}
			else if (entity.StartDate.HasValue)
			{
				entity.StartDate = entity.StartDate.Value.Date;
				entity.StartDateInText = entity.StartDate.Value.ToShamsiDate();
			}

			if (entity.StartDate.HasValue && entity.ContractDurationInMonth > 0)
			{
				entity.EndDate = entity.StartDate.Value.Date.AddMonths(entity.ContractDurationInMonth);
				entity.EndDateInText = entity.EndDate.Value.ToShamsiDate();
			}

			entity.TotalPrice = entity.MonthlyPrice * entity.ContractDurationInMonth;
		}
	}
}
