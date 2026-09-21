using Common.Utilities;
using Data.Contracts;
using Data.Contracts.Actions;
using Entities.App.Sale;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Actions.Sale
{
	public class WorkReportAction(IUnitOfWork unitOfWork)
	{
		[EntityAction(typeof(WorkReport), EntityActionTrigger.BeforeSave,
			"WorkReportValidate", "اعتبار گزارش کار مطابق HTS", Priority = 1)]
		public async Task BeforeSave(WorkReport entity, CancellationToken ct)
		{
			await FillLookupTexts(entity, ct);

			var header = await LoadHeader(entity, ct);
			var unreal = header?.Unreal == true;

			if (!unreal)
			{
				if (entity.MissionId is null or 0)
					throw new InvalidOperationException("ماموریت نمی‌تواند خالی باشد");
				if (entity.ReportNumber is null or 0)
					throw new InvalidOperationException("شماره گزارش نمی‌تواند خالی باشد");
			}

			if (entity.ServiceRequestDetailId is null or 0)
				throw new InvalidOperationException("محصول نمی‌تواند خالی باشد");
			if (string.IsNullOrWhiteSpace(entity.ServiceStartShamsiDate))
				throw new InvalidOperationException("تاریخ شروع سرویس نمی‌تواند خالی باشد");
			if (string.IsNullOrWhiteSpace(entity.ServiceEndShamsiDate))
				throw new InvalidOperationException("تاریخ پایان سرویس نمی‌تواند خالی باشد");
			if (string.IsNullOrWhiteSpace(entity.ServiceStartTime) || entity.ServiceStartTime.Contains('_'))
				throw new InvalidOperationException("ساعت شروع سرویس نمی‌تواند خالی باشد");
			if (string.IsNullOrWhiteSpace(entity.ServiceEndTime) || entity.ServiceEndTime.Contains('_'))
				throw new InvalidOperationException("ساعت پایان سرویس نمی‌تواند خالی باشد");
			if (entity.TotalRunTime <= 0)
				throw new InvalidOperationException("ساعت کارکرد نمی‌تواند خالی باشد");
			if (entity.MissionResult == 0)
				throw new InvalidOperationException("نتیجه ماموریت نمی‌تواند خالی باشد");

			if (!string.IsNullOrWhiteSpace(entity.ServiceStartShamsiDate))
				entity.ServiceStartMiladiDate = entity.ServiceStartShamsiDate.ToMiladiDate();
			if (!string.IsNullOrWhiteSpace(entity.ServiceEndShamsiDate))
				entity.ServiceEndMiladiDate = entity.ServiceEndShamsiDate.ToMiladiDate();
			if (!string.IsNullOrWhiteSpace(entity.ExpertGuaranteeShamsiDate))
				entity.ExpertGuaranteeMiladiDate = entity.ExpertGuaranteeShamsiDate.ToMiladiDate();

			if (entity.ServiceStartMiladiDate != null && entity.ServiceEndMiladiDate != null
				&& entity.ServiceEndMiladiDate < entity.ServiceStartMiladiDate)
				throw new InvalidOperationException("بازه تاریخ انتخاب‌شده صحیح نیست");

			if (entity.ServiceRequestDetailId is > 0)
			{
				var notice = await unitOfWork.Repository<ServiceRequestDetail>().TableNoTracking
					.Where(d => d.Id == entity.ServiceRequestDetailId)
					.Select(d => d.NoticeShamsiDate)
					.FirstOrDefaultAsync(ct);
				if (!string.IsNullOrWhiteSpace(notice) && entity.ServiceStartMiladiDate != null)
				{
					var noticeMiladi = notice.ToMiladiDate();
					if (noticeMiladi != default && entity.ServiceStartMiladiDate < noticeMiladi)
						throw new InvalidOperationException("تاریخ شروع سرویس نمی‌تواند قبل از تاریخ اعلام باشد");
				}
			}

			var duplicate = await unitOfWork.Repository<WorkReport>().TableNoTracking
				.AnyAsync(c =>
					c.ServiceRequestDetailId == entity.ServiceRequestDetailId
					&& c.MissionId == entity.MissionId
					&& c.Id != entity.Id, ct);
			if (duplicate)
				throw new InvalidOperationException("این محصول برای این ماموریت قبلاً ثبت شده است");
		}

		private async Task FillLookupTexts(WorkReport entity, CancellationToken ct)
		{
			entity.FailureTypeInText = await JoinTitles<AfterSalesFailureType>(entity.FailureTypeIds, ct);
			entity.RepairsTypeInText = await JoinTitles<AfterSalesRepairsType>(entity.RepairsTypeIds, ct);
		}

		private async Task<string?> JoinTitles<TLookup>(string? idsCsv, CancellationToken ct)
		{
			var ids = ParseIds(idsCsv);
			if (ids.Count == 0)
				return null;

			if (typeof(TLookup) == typeof(AfterSalesFailureType))
			{
				var rows = await unitOfWork.Repository<AfterSalesFailureType>().TableNoTracking
					.Where(c => c.Id != null && ids.Contains(c.Id.Value))
					.Select(c => ((c.Code ?? "") + " | " + (c.Title ?? "")).Trim(' ', '|'))
					.ToListAsync(ct);
				return rows.Count == 0 ? null : string.Join(",", rows);
			}

			var repairRows = await unitOfWork.Repository<AfterSalesRepairsType>().TableNoTracking
				.Where(c => c.Id != null && ids.Contains(c.Id.Value))
				.Select(c => ((c.Code ?? "") + " | " + (c.Title ?? "")).Trim(' ', '|'))
				.ToListAsync(ct);
			return repairRows.Count == 0 ? null : string.Join(",", repairRows);
		}

		private async Task<ServiceRequest?> LoadHeader(WorkReport entity, CancellationToken ct)
		{
			if (entity.ServiceRequestDetailId is > 0)
			{
				var headerId = await unitOfWork.Repository<ServiceRequestDetail>().TableNoTracking
					.Where(d => d.Id == entity.ServiceRequestDetailId)
					.Select(d => d.ServiceRequestId)
					.FirstOrDefaultAsync(ct);
				if (headerId is > 0)
					return await unitOfWork.Repository<ServiceRequest>().TableNoTracking
						.FirstOrDefaultAsync(c => c.Id == headerId, ct);
			}

			if (entity.MissionId is > 0)
			{
				var headerId = await unitOfWork.Repository<Mission>().TableNoTracking
					.Where(m => m.Id == entity.MissionId)
					.Select(m => m.ServiceRequestId)
					.FirstOrDefaultAsync(ct);
				if (headerId is > 0)
					return await unitOfWork.Repository<ServiceRequest>().TableNoTracking
						.FirstOrDefaultAsync(c => c.Id == headerId, ct);
			}

			return null;
		}

		private static List<long> ParseIds(string? csv)
		{
			if (string.IsNullOrWhiteSpace(csv))
				return [];
			return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Select(s => long.TryParse(s, out var id) ? id : 0)
				.Where(id => id > 0)
				.Distinct()
				.ToList();
		}
	}
}
