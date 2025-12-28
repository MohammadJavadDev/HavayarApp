using Common.Attributes;
using Data;
using Data.Contracts;
using Entities.App.Gnr;
using Entities.App.Hrm;
using Microsoft.EntityFrameworkCore;
using Services.Job;

namespace App.BackgroundJob.Jobs.Hrm
{
	public class OrgUnitJobs(RahkaranDbContext Rdb, IUnitOfWork unitOfWork)
	{


		[JobHandler("افزودن واحد های سازمانی از راهکاران )")]
		public async Task AddPartsFromRahkaran(IJobLogger jobLogger = null, CancellationToken cn = default)
		{
			var rahkaranData = await Rdb.RahkaranDepartments
				  .AsNoTracking()
				  .ToArrayAsync();

			await jobLogger.LogInfoAsync($"تعداد کل واحد ها {rahkaranData.Length}" , cn);
			
			var appOrgUnits = await unitOfWork.Repository<OrgUnit>().Table.ToListAsync(cn);

			await jobLogger.LogInfoAsync($"تعداد  واحد های موجود در سیستم{rahkaranData.Length}", cn);

			var appOrgUnitsDict = appOrgUnits
			    .ToDictionary(x => x.HamkaranUnitId);

			var newOrgUnits = new List<OrgUnit>();

			foreach (var rahkaranUnit in rahkaranData)
			{
				if (!appOrgUnitsDict.TryGetValue(rahkaranUnit.DepartmentID, out var existUnit))
				{
					newOrgUnits.Add(

						new OrgUnit() { 
						
						HamkaranUnitId = rahkaranUnit.DepartmentID,
						Title = rahkaranUnit.Title,
						}); 
				}
				else
				{
					if(existUnit.Title != rahkaranUnit.Title)
					{
						existUnit.Title = rahkaranUnit.Title;
					}
				}
			}

			if(newOrgUnits.Any())
			{

			await unitOfWork.Repository<OrgUnit>().AddRangeAsync(newOrgUnits,cn,false);
			}

			await unitOfWork.SaveChangesAsync(cn);

		}
	}
}
