using Common.Attributes;
using Data;
using Microsoft.EntityFrameworkCore;
using Services.Job;

namespace App.BackgroundJob.Jobs.Inv
{
	 
	public class PartJob(RahkaranDbContext db)
	{
		[JobHandler("افزودن اطلاعات کالا از راهکاران")]
		public async Task AddPartsFromRahkaran()
		{
			var data = await db.RahkaranParts.Take(10).ToListAsync();

			var part = data[0];	

		}
	}
}
