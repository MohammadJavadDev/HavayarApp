using Common.Attributes;
using Data.Contracts;
using Services.Job;

namespace App.BackgroundJob.Jobs.Cng
{
	/// <summary>
	/// جاب دوره‌ای ورود قطعات مصرفی تعمیرات CNG از راهکاران.
	/// معادل HTS AfterSalesAndRepairSystemTask → WriteData.Import_Cng_Repairing_UsedPart.
	/// </summary>
	public class CngRepairsUsedPartImportJob(ICngRepairsUsedPartImporter importer)
	{
		[JobHandler("ورود قطعات مصرفی تعمیرات CNG از راهکاران")]
		public async Task ImportFromRahkaran(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			await jobLogger?.LogInfoAsync("شروع ورود قطعات مصرفی تعمیرات CNG از راهکاران", cn);
			var inserted = await importer.ImportAsync(cn);
			await jobLogger?.LogInfoAsync($"ورود تمام شد — ردیف‌های جدید: {inserted}", cn);
		}
	}
}
