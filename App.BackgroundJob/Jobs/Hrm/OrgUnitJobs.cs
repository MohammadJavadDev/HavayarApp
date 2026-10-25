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
				.ToArrayAsync(cn);

			await jobLogger.LogInfoAsync($"تعداد کل واحد ها {rahkaranData.Length}", cn);

			var appOrgUnits = await unitOfWork.Repository<OrgUnit>().Table.ToListAsync(cn);

			await jobLogger.LogInfoAsync($"تعداد واحد های موجود در سیستم {appOrgUnits.Count}", cn);

			// HamkaranUnitId صفر واحد محلی است و کلید همگام‌سازی راهکاران نیست.
			var linkedUnits = appOrgUnits.Where(x => x.HamkaranUnitId > 0).ToList();
			var duplicateIds = linkedUnits
				.GroupBy(x => x.HamkaranUnitId)
				.Where(g => g.Count() > 1)
				.Select(g => g.Key)
				.ToList();

			if (duplicateIds.Count > 0)
			{
				await jobLogger.LogWarningAsync(
					$"شناسه همکاران تکراری است و فقط اولین رکورد به‌روز می‌شود: {string.Join("، ", duplicateIds)}",
					0,
					cn);
			}

			var appOrgUnitsDict = linkedUnits
				.GroupBy(x => x.HamkaranUnitId)
				.ToDictionary(g => g.Key, g => g.OrderBy(x => x.Id).First());

			var newOrgUnits = new List<OrgUnit>();

			foreach (var rahkaranUnit in rahkaranData)
			{
				if (rahkaranUnit.DepartmentID <= 0)
					continue;

				if (!appOrgUnitsDict.TryGetValue(rahkaranUnit.DepartmentID, out var existUnit))
				{
					var created = new OrgUnit
					{
						HamkaranUnitId = rahkaranUnit.DepartmentID,
						Title = rahkaranUnit.Title,
					};
					newOrgUnits.Add(created);
					appOrgUnitsDict[rahkaranUnit.DepartmentID] = created;
				}
				else if (existUnit.Title != rahkaranUnit.Title)
				{
					existUnit.Title = rahkaranUnit.Title;
				}
			}

			if (newOrgUnits.Count > 0)
				await unitOfWork.Repository<OrgUnit>().AddRangeAsync(newOrgUnits, cn, false);

			await unitOfWork.SaveChangesAsync(cn);
		}
	}
}
