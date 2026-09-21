using Common.Attributes;
using Data.Contracts;
using Entities.App.Inv;
using Entities.App.Sale;
using Microsoft.EntityFrameworkCore;
using Services.Job;

namespace App.BackgroundJob.Jobs.Sale
{
	/// <summary>
	/// معادل HTS SaleAgencyPartCardexService.AddNewItemsByCheckSaleOrders
	/// افزایش از حواله فروش دیروز، کاهش از درخواست کالای دستی نمایندگان دیروز.
	/// </summary>
	public class AgencyPartCardexJob(IUnitOfWork unitOfWork)
	{
		[JobHandler("بروزرسانی کاردکس قطعه نمایندگی از حواله و درخواست کالا")]
		public async Task AddNewItemsByCheckSaleOrders(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			var now = DateTime.Now;
			if (now.Hour < 6 || now.Hour > 21)
			{
				await jobLogger?.LogInfoAsync("خارج از بازه ۶ تا ۲۱ — اجرا نشد (مطابق HTS)", cn);
				return;
			}

			var today = now.Date;
			var yesterday = today.AddDays(-1);
			await jobLogger?.LogInfoAsync("شروع بروزرسانی کاردکس قطعه نمایندگی برای " + yesterday.ToString("yyyy-MM-dd"), cn);

			var agencyIds = await unitOfWork.Repository<AgencyPartCardex>().TableNoTracking
				.Where(c => c.AgencyId != null)
				.Select(c => c.AgencyId!.Value)
				.Distinct()
				.ToListAsync(cn);
			if (agencyIds.Count == 0)
			{
				await jobLogger?.LogInfoAsync("نمایندگی با کاردکس یافت نشد", cn);
				return;
			}

			var increases = await unitOfWork.Repository<OrderDetail>().TableNoTracking
				.Where(c => c.Sale_Order != null
					&& c.Sale_Order.CustomerId != null
					&& agencyIds.Contains(c.Sale_Order.CustomerId.Value)
					&& c.Sale_Order.VchDateMiladiDate != null
					&& c.Sale_Order.VchDateMiladiDate.Value.Date == yesterday
					&& c.PartId != null)
				.Select(c => new { AgencyId = c.Sale_Order.CustomerId!.Value, PartId = c.PartId!.Value, c.Qty })
				.ToListAsync(cn);

			var inserted = 0;
			foreach (var row in increases)
			{
				if (await TryInsertCardexDelta(row.AgencyId, row.PartId, row.Qty, today,
					"ایجاد شده بصورت اتوماتیک به علت بروزرسانی فیلد مقدار که از حواله فروش خوانده شده است", cn))
					inserted++;
			}

			var decreases = await unitOfWork.Repository<ServiceRequestPart>().TableNoTracking
				.Where(c => c.IsRegisteredManually
					&& c.CreatedOnMiladiDateTime != null
					&& c.CreatedOnMiladiDateTime.Value.Date == yesterday
					&& c.ServiceRequestDetail != null
					&& c.ServiceRequestDetail.ServiceRequest != null
					&& c.ServiceRequestDetail.ServiceRequest.CustomerId != null
					&& agencyIds.Contains(c.ServiceRequestDetail.ServiceRequest.CustomerId.Value))
				.Select(c => new
				{
					AgencyId = c.ServiceRequestDetail!.ServiceRequest!.CustomerId!.Value,
					ReplacePartId = c.ReplacePartId,
					OrderDetailPartId = c.OrderDetail != null ? c.OrderDetail.PartId : null,
					c.HtsProjectVchPartId,
					c.Mount
				})
				.ToListAsync(cn);

			var htsPartIds = decreases
				.Where(c => c.ReplacePartId is null or 0 && c.OrderDetailPartId is null or 0 && c.HtsProjectVchPartId is > 0)
				.Select(c => c.HtsProjectVchPartId!.Value)
				.Distinct()
				.ToList();
			var htsPartMap = new Dictionary<long, long>();
			if (htsPartIds.Count > 0)
			{
				var mapped = await unitOfWork.Repository<Part>().TableNoTracking
					.Where(c => htsPartIds.Contains(c.HtsId) || (c.HamkaranId != null && htsPartIds.Contains(c.HamkaranId.Value)))
					.Select(c => new { c.Id, c.HtsId, c.HamkaranId })
					.ToListAsync(cn);
				foreach (var p in mapped)
				{
					if (p.Id is > 0 && p.HtsId != 0)
						htsPartMap.TryAdd(p.HtsId, p.Id.Value);
					if (p.Id is > 0 && p.HamkaranId is > 0)
						htsPartMap.TryAdd(p.HamkaranId.Value, p.Id.Value);
				}
			}

			foreach (var row in decreases)
			{
				long? partId = row.ReplacePartId ?? row.OrderDetailPartId;
				if (partId is null or 0 && row.HtsProjectVchPartId is > 0 && htsPartMap.TryGetValue(row.HtsProjectVchPartId.Value, out var mappedId))
					partId = mappedId;
				if (partId is null or 0)
					continue;
				if (await TryInsertCardexDelta(row.AgencyId, partId.Value, -row.Mount, today,
					"ایجاد شده بصورت اتوماتیک به علت بروزرسانی فیلد مقدار که از درخواست کالا (دستی) ثبت شده توسط نمایندگان خوانده شده است", cn))
					inserted++;
			}

			await jobLogger?.LogInfoAsync("تعداد ردیف کاردکس جدید: " + inserted, cn);
		}

		private async Task<bool> TryInsertCardexDelta(long agencyId, long partId, decimal delta, DateTime today, string comment, CancellationToken cn)
		{
			var latest = await unitOfWork.Repository<AgencyPartCardex>().TableNoTracking
				.Where(c => c.AgencyId == agencyId
					&& c.PartId == partId
					&& (c.CreatedOnMiladiDateTime == null || c.CreatedOnMiladiDateTime.Value.Date != today))
				.OrderByDescending(c => c.Id)
				.FirstOrDefaultAsync(cn);
			if (latest == null)
				return false;

			var entity = new AgencyPartCardex
			{
				AgencyId = agencyId,
				PartId = partId,
				Amount = latest.Amount + delta,
				Comment = comment,
				CreatedById = 1
			};
			await unitOfWork.Repository<AgencyPartCardex>().SaveAsync(entity, cn, true);
			return true;
		}
	}
}
