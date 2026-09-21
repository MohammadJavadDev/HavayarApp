using Common.Utilities;
using Data.Contracts;
using Data.Contracts.Actions;
using Entities.App.Bpm;
using Entities.App.Bpm.Enums;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Actions.Bpm
{
	/// <summary>
	/// معادل تریگر HTS روی Bpm_Document (بازنگری) و درج ردیف Issue هنگام ایجاد درخواست.
	/// GetDocumentNumber: پسوند عددی max، بدون وضعیت‌های لغو و بدون نوع حذف/بازنگری.
	/// فایل روی مسیر آپلود FileService می‌ماند؛ به سهم COTMS/DMS قدیمی منتقل نمی‌شود.
	/// </summary>
	public class ProcessDocumentAction(IUnitOfWork unitOfWork)
	{
		private static readonly ProcessDocumentTypeEnum[] SequentialNumberTypes =
		[
			ProcessDocumentTypeEnum.R,
			ProcessDocumentTypeEnum.P,
			ProcessDocumentTypeEnum.W,
			ProcessDocumentTypeEnum.WT,
			ProcessDocumentTypeEnum.C,
			ProcessDocumentTypeEnum.F,
			ProcessDocumentTypeEnum.M,
			ProcessDocumentTypeEnum.T
		];

		[EntityAction(typeof(ProcessDocument), EntityActionTrigger.BeforeSave, "ApplyProcessDocumentRevision", "اعمال بازنگری مطابق تریگر HTS", Priority = 1)]
		public async Task ApplyRevision(ProcessDocument entity, CancellationToken ct)
		{
			if (IsSystemOwner(entity) || entity.IsDeprecate)
				return;

			if (entity.RequestType == ProcessDocumentRequestTypeEnum.Create)
			{
				entity.Revision = 0;
				return;
			}

			if (entity.RequestType == ProcessDocumentRequestTypeEnum.Review)
				return;

			if (entity.ParentId is > 0)
			{
				var parentRevision = await unitOfWork.Repository<ProcessDocument>().TableNoTracking
					.Where(c => c.Id == entity.ParentId)
					.Select(c => (short?)c.Revision)
					.FirstOrDefaultAsync(ct);
				if (parentRevision != null)
					entity.Revision = parentRevision.Value;
			}
			else if (entity.Id is null or 0)
			{
				entity.Revision = 0;
			}
		}

		[EntityAction(typeof(ProcessDocument), EntityActionTrigger.BeforeSave, "ApplyProcessDocumentNumber", "تولید شماره سند فرآیندی", Priority = 2)]
		public async Task ApplyDocumentNumber(ProcessDocument entity, CancellationToken ct)
		{
			if (IsSystemOwner(entity) || entity.IsDeprecate)
				return;

			if (entity.RequestType is ProcessDocumentRequestTypeEnum.Delete or ProcessDocumentRequestTypeEnum.Review)
			{
				if (!string.IsNullOrWhiteSpace(entity.DocumentNumber))
					return;
				if (entity.ParentId is > 0)
				{
					var parentNumber = await unitOfWork.Repository<ProcessDocument>().TableNoTracking
						.Where(c => c.Id == entity.ParentId)
						.Select(c => c.DocumentNumber)
						.FirstOrDefaultAsync(ct);
					if (!string.IsNullOrWhiteSpace(parentNumber))
						entity.DocumentNumber = parentNumber;
				}
				return;
			}

			if (!string.IsNullOrWhiteSpace(entity.DocumentNumber))
				return;
			if (entity.RequestType != ProcessDocumentRequestTypeEnum.Create)
				return;

			var number = await GetDocumentNumberAsync(entity, ct);
			entity.DocumentNumber = string.IsNullOrWhiteSpace(number) ? null : number;
		}

		[EntityAction(typeof(ProcessDocument), EntityActionTrigger.AfterAdd, "InsertIssueStatusLog", "ثبت گردش ایجاد شده", Priority = 1)]
		public async Task InsertIssueStatusLog(ProcessDocument entity, CancellationToken ct)
		{
			if (entity.Id is null or 0)
				return;
			if (IsSystemOwner(entity))
				return;

			var hasIssue = await unitOfWork.Repository<ProcessDocumentStatusLog>().TableNoTracking
				.AnyAsync(c => c.ProcessDocumentId == entity.Id && c.Status == ProcessDocumentStatusEnum.Issue, ct);
			if (hasIssue)
				return;

			await unitOfWork.Repository<ProcessDocumentStatusLog>().AddAsync(new ProcessDocumentStatusLog
			{
				ProcessDocumentId = entity.Id.Value,
				Status = ProcessDocumentStatusEnum.Issue
			}, ct, true);
		}

		private async Task<string?> GetDocumentNumberAsync(ProcessDocument entity, CancellationToken ct)
		{
			if (entity.DocumentType == null)
				return null;

			if (SequentialNumberTypes.Contains(entity.DocumentType.Value))
			{
				var lookupCode = ProcessDocumentTypeCodes.GetLookupCode(entity.DocumentType.Value);
				if (string.IsNullOrWhiteSpace(lookupCode))
					return null;

				var prefix = lookupCode + "-";
				var maxSuffix = await GetMaxNumericSuffixAsync(prefix, ct);
				if (maxSuffix == null)
					return $"{lookupCode}-001";

				return $"{lookupCode}-{(maxSuffix.Value + 1).ToString().PadLeft(2, '0')}";
			}

			if (entity.DocumentType == ProcessDocumentTypeEnum.CL)
			{
				var processCode = entity.ProcessSet == null
					? ""
					: ProcessDocumentTypeCodes.GetLookupCode(entity.ProcessSet.Value) ?? "";
				var prefix = $"CL-{processCode}-";
				var maxSuffix = await GetMaxNumericSuffixAsync(prefix, ct);
				if (maxSuffix == null)
					return $"CL-{processCode}-01";

				return $"CL-{processCode}-{(maxSuffix.Value + 1).ToString().PadLeft(2, '0')}";
			}

			return null;
		}

		private async Task<int?> GetMaxNumericSuffixAsync(string prefix, CancellationToken ct)
		{
			var prefixLower = prefix.ToLowerInvariant();
			var rows = await unitOfWork.Repository<ProcessDocument>().TableNoTracking
				.Where(d =>
					d.DocumentNumber != null
					&& d.RequestType != ProcessDocumentRequestTypeEnum.Delete
					&& d.RequestType != ProcessDocumentRequestTypeEnum.Review
					&& d.DocumentNumber.ToLower().StartsWith(prefixLower)
					&& d.StatusLogs.Any()
					&& !d.StatusLogs.Any(s =>
						s.Status == ProcessDocumentStatusEnum.CancelRequest
						|| s.Status == ProcessDocumentStatusEnum.BpmCancelRequest))
				.Select(d => d.DocumentNumber)
				.ToListAsync(ct);

			int? max = null;
			foreach (var number in rows)
			{
				if (string.IsNullOrWhiteSpace(number))
					continue;
				var last = number.Split('-', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
				if (last != null && int.TryParse(last, out var suffix))
					max = max == null ? suffix : Math.Max(max.Value, suffix);
			}

			return max;
		}

		private static bool IsSystemOwner(ProcessDocument entity)
			=> string.Equals(entity.Comment, ProcessDocumentNotificationConstants.SystemOwnerComment, StringComparison.OrdinalIgnoreCase);
	}
}
