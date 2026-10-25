using Common.Attributes;
using Data;
using Data.Contracts;
using Entities.App.Hrm;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Services.Job;

namespace App.BackgroundJob.Jobs.Hrm
{
	public class OrgPostJobs(RahkaranDbContext Rdb, IUnitOfWork unitOfWork)
	{
		[JobHandler("همگام‌سازی پست سازمانی از راهکاران")]
		public async Task SyncFromRahkaran(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			var rahkaranPosts = await Rdb.RahkaranPosts
				.AsNoTracking()
				.ToArrayAsync(cn);

			await jobLogger?.LogInfoAsync($"تعداد پست‌های راهکاران: {rahkaranPosts.Length}", cn);

			var appPosts = await unitOfWork.Repository<OrgPost>().Table.ToListAsync(cn);
			var linked = appPosts.Where(x => x.HamkaranPostId is > 0).ToList();
			var duplicateIds = linked
				.GroupBy(x => x.HamkaranPostId!.Value)
				.Where(g => g.Count() > 1)
				.Select(g => g.Key)
				.ToList();

			if (duplicateIds.Count > 0)
			{
				await jobLogger?.LogWarningAsync(
					$"شناسه راهکاران پست تکراری است و فقط اولین رکورد به‌روز می‌شود: {string.Join("، ", duplicateIds)}",
					0,
					cn);
			}

			var byHamkaran = linked
				.GroupBy(x => x.HamkaranPostId!.Value)
				.ToDictionary(g => g.Key, g => g.OrderBy(x => x.Id).First());

			var newPosts = new List<OrgPost>();
			var seen = new HashSet<long>();

			foreach (var rahkaranPost in rahkaranPosts)
			{
				if (rahkaranPost.PostID <= 0 || !seen.Add(rahkaranPost.PostID))
					continue;

				var title = NormalizeTitle(rahkaranPost.Title);
				if (!byHamkaran.TryGetValue(rahkaranPost.PostID, out var exist))
				{
					var created = new OrgPost
					{
						HamkaranPostId = rahkaranPost.PostID,
						Title = title,
						ExternalPost = false,
						IsActive = IsActiveEnum.Active
					};
					newPosts.Add(created);
					byHamkaran[rahkaranPost.PostID] = created;
				}
				else if (exist.Title != title)
				{
					exist.Title = title;
				}
			}

			if (newPosts.Count > 0)
				await unitOfWork.Repository<OrgPost>().AddRangeAsync(newPosts, cn, false);

			await unitOfWork.SaveChangesAsync(cn);
			await jobLogger?.LogInfoAsync($"پست جدید: {newPosts.Count}", cn);
		}

		private static string? NormalizeTitle(string? title)
		{
			if (string.IsNullOrWhiteSpace(title))
				return null;

			title = title.Trim();
			return title.Length <= 500 ? title : title[..500];
		}
	}
}
