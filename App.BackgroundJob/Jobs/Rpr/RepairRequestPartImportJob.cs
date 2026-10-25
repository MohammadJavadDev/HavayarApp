using Common.Attributes;
using Data.Contracts;
using Services.Job;

namespace App.BackgroundJob.Jobs.Rpr
{
	/// <summary>
	/// جاب دوره‌ای ورود اقلام مصرفی تعمیرات از راهکاران.
	/// معادل HTS AfterSalesAndRepairSystemTask → Import_Rpr_RepairRequest_Part.
	/// </summary>
	public class RepairRequestPartImportJob(IRepairRequestPartImporter importer)
	{
		[JobHandler("ورود اقلام مصرفی تعمیرات از راهکاران")]
		public async Task ImportFromRahkaran(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			var now = DateTime.Now;
			if (now.Hour < 6 || now.Hour > 21)
			{
				await jobLogger?.LogInfoAsync($"خارج از بازه ساعتی ۶ تا ۲۱ (ساعت فعلی: {now.Hour}) — اجرا نشد.", cn);
				return;
			}

			await jobLogger?.LogInfoAsync("شروع ورود اقلام مصرفی تعمیرات از راهکاران", cn);
			var inserted = await importer.ImportAsync(null, cn);
			await jobLogger?.LogInfoAsync($"ورود تمام شد — ردیف‌های جدید: {inserted}", cn);
		}
	}
}
