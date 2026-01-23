using Common.Attributes;
using Data;
using Data.Contracts;
using Entities.App.SLS;
using Microsoft.EntityFrameworkCore;
using Services.Job;
using System.Linq;

namespace App.BackgroundJob.Jobs.Sls
{
	public class SalesOfficeJob(RahkaranDbContext Rdb, IUnitOfWork unitOfWork)
	{

		[JobHandler("افزودن مراکز هزینه از راهکاران")]
		public async Task AddSalesOfficeFromRahkaran(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("شروع همگام‌سازی مراکز هزینه از راهکاران", cn);

				var sqlRow = @"SELECT Code, [Name] AS BranchName FROM SLS3.SalesOffice";

				await jobLogger?.LogInfoAsync("در حال اجرای کوئری برای دریافت داده‌های مراکز هزینه از راهکاران...", cn);
				var rahakarnData = await Rdb.Database.SqlQueryRaw<AddSalesOfficeDto>(sqlRow).ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد {rahakarnData.Count} رکورد مرکز هزینه از راهکاران دریافت شد", cn);

				// بارگذاری SalesOffice های موجود
				var appSalesOffices = await unitOfWork.Repository<SalesOffice>()
					.Table
					.ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد {appSalesOffices.Count} مرکز هزینه در پایگاه داده برنامه موجود است", cn);

				// ایجاد Dictionary برای نگاشت Code به SalesOffice
				var appSalesOfficesDict = appSalesOffices
					.Where(x => !string.IsNullOrEmpty(x.Code))
					.ToDictionary(x => x.Code!);

				var newSalesOffices = new List<SalesOffice>();
				int updatedSalesOfficesCount = 0;

				foreach (var rahkaranData in rahakarnData)
				{
					if (string.IsNullOrEmpty(rahkaranData.Code))
					{
						await jobLogger?.LogWarningAsync("رکوردی با Code خالی یافت شد و رد شد", 0, cn);
						continue;
					}

					if (!appSalesOfficesDict.TryGetValue(rahkaranData.Code, out var existSalesOffice))
					{
						// ایجاد رکورد جدید
						newSalesOffices.Add(new SalesOffice
						{
							Code = rahkaranData.Code,
							Name = rahkaranData.BranchName
						});
					}
					else
					{
						// به‌روزرسانی رکورد موجود
						bool isModified = false;

						if (existSalesOffice.Name != rahkaranData.BranchName)
						{
							existSalesOffice.Name = rahkaranData.BranchName;
							isModified = true;
						}

						if (isModified)
						{
							updatedSalesOfficesCount++;
						}
					}
				}

				if (newSalesOffices.Any())
				{
					await jobLogger?.LogInfoAsync($"افزودن {newSalesOffices.Count} مرکز هزینه جدید به پایگاه داده", cn);
					await unitOfWork.Repository<SalesOffice>().AddRangeAsync(newSalesOffices, cn, false);
				}

				if (updatedSalesOfficesCount > 0)
				{
					await jobLogger?.LogInfoAsync($"به‌روزرسانی {updatedSalesOfficesCount} مرکز هزینه موجود", cn);
				}

				await unitOfWork.SaveChangesAsync(cn);
				await jobLogger?.LogInfoAsync("همگام‌سازی مراکز هزینه با موفقیت انجام شد", cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
				throw;
			}
		}

		private class AddSalesOfficeDto
		{
			public string? Code { get; set; }
			public string? BranchName { get; set; }
		}
	}
}
