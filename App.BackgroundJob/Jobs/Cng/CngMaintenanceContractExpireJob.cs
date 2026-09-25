using Common.Attributes;
using Data.Contracts;
using Entities.App.Cng;
using Entities.App.Cng.Enums;
using Microsoft.EntityFrameworkCore;
using Services.Job;

namespace App.BackgroundJob.Jobs.Cng
{
	/// <summary>
	/// معادل HTS CngMaintenanceContractService.CheckExpiredItems —
	/// قراردادهایی که تاریخ پایان‌شان گذشته و وضعیت غیر از اتمام است را به ۲۰۸۶ می‌برد.
	/// </summary>
	public class CngMaintenanceContractExpireJob(IUnitOfWork unitOfWork)
	{
		[JobHandler("اتمام خودکار قراردادهای تعمیر و نگهداشت CNG")]
		public async Task CheckExpiredItems(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			var today = DateTime.Now.Date;
			await jobLogger?.LogInfoAsync("شروع بررسی قراردادهای منقضی CNG تا " + today.ToString("yyyy-MM-dd"), cn);

			var expired = await unitOfWork.Repository<MaintenanceContract>().Table
				.Where(p => p.EndDate != null
					&& p.EndDate < today
					&& p.Status != MaintenanceContractStatusEnum.Finished)
				.ToListAsync(cn);

			if (expired.Count == 0)
			{
				await jobLogger?.LogInfoAsync("قرارداد منقضی برای به‌روزرسانی یافت نشد", cn);
				return;
			}

			foreach (var item in expired)
				item.Status = MaintenanceContractStatusEnum.Finished;

			await unitOfWork.Repository<MaintenanceContract>().UpdateRangeAsync(expired, cn, true);
			await jobLogger?.LogInfoAsync($"وضعیت {expired.Count} قرارداد به اتمام قرارداد تغییر کرد", cn);
		}
	}
}
