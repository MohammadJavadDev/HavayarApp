using Common.Attributes;
using Services.Job;

namespace App.BackgroundJob.Jobs.Inv
{
	 
	public class PartJob
	{
		[JobHandler("افزودن اطلاعات کالا از راهکاران")]
		public async Task AddPartsFromRahkaran()
		{
			Console.WriteLine("Archiving orders...");
			await Task.Delay(1000); // شبیه‌سازی کار سنگین
		}
	}
}
