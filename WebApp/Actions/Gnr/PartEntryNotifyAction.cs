using Data.Contracts;
using Data.Contracts.Actions;
using Entities.App.Gnr;
using Entities.App.Hrm;
using Entities.App.Hrm.Enums;
using Entities.App.Inv;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace WebApp.Actions.Gnr
{
	/// <summary>
	/// معادل HTS <c>GnrPartEntryNotifyService.ConvertAndAddEntityToGuardModule</c>:
	/// با ثبت هر اعلام ورود قطعه، یک «ورود و خروج کالا» در ماژول حراست/دژبان ساخته می‌شود
	/// (مکان و کالای حراست در صورت نبود ساخته می‌شوند).
	/// </summary>
	public class PartEntryNotifyAction(IUnitOfWork unitOfWork, ILogger<PartEntryNotifyAction> logger)
	{
		private const string AutoComment = "ایجاد شده از طریق اعلان ورود کالا";

		/// <summary>
		/// HTS مقدار «گروه قطعه» (Gnr_Lookup نوع 214) را با عنوان با «علت» (نوع 212) تطبیق می‌داد؛
		/// عنوان‌های بدون تطابق (نمونه، سایر، برگشت به تامین کننده) «نامشخص» می‌شدند.
		/// </summary>
		private static readonly Dictionary<int, GuardIoReasonEnum> PartGroupToReason = new()
		{
			[1475] = GuardIoReasonEnum.ReturnFromProjectSite,
			[1476] = GuardIoReasonEnum.ReturnFromSale,
			[1478] = GuardIoReasonEnum.Trust,
			[1480] = GuardIoReasonEnum.Replacement,
			[1481] = GuardIoReasonEnum.Repair,
			[1482] = GuardIoReasonEnum.ReturnFromContractor,
			[1538] = GuardIoReasonEnum.Defective,
			[1539] = GuardIoReasonEnum.Calibration,
			[1540] = GuardIoReasonEnum.Warranty,
			[1541] = GuardIoReasonEnum.Preparation,
			[1542] = GuardIoReasonEnum.Manufacturing,
			[1543] = GuardIoReasonEnum.Correction
		};

		/// <summary>PartOwnerUnitId اعلام ورود، شناسه واحد HTS است (HRM_OrgUnit.OrgUnit_ID) → شناسه همکاران واحد.</summary>
		private static readonly Dictionary<int, long> HtsUnitToHamkaranUnit = new()
		{
			[146] = 121000130 // توزیع سوخت (مقدار ثابت دکمه «اعلام ورود قطعه» تعمیرات CNG)
		};

		[EntityAction(typeof(PartEntryNotify), EntityActionTrigger.AfterAdd,
			"PartEntryNotifyCreateGuardPartIo", "ایجاد ورود/خروج کالای حراست از اعلام ورود قطعه", Priority = 100)]
		public async Task AfterAdd(PartEntryNotify entity, CancellationToken ct)
		{
			if (entity.Id == null || entity.Id == 0)
				return;

			// مثل HTS: خطا در ساخت رکورد حراست نباید ثبت اعلام ورود را خراب کند.
			try
			{
				await CreateGuardPartIoAsync(entity, ct);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "ساخت ورود/خروج کالای حراست از اعلام ورود {Id} ناموفق بود", entity.Id);
			}
		}

		private async Task CreateGuardPartIoAsync(PartEntryNotify entity, CancellationToken ct)
		{

			// --- تامین کننده → مکان حراست
			GuardLocation? location = null;
			var supplierTitle = entity.SupplierTitle;
			if (!string.IsNullOrWhiteSpace(supplierTitle))
			{
				location = await unitOfWork.Repository<GuardLocation>().TableNoTracking
					.FirstOrDefaultAsync(l => l.Title == supplierTitle, ct)
					?? await unitOfWork.Repository<GuardLocation>().SaveAsync(
						new GuardLocation { Title = supplierTitle, Comment = AutoComment }, ct, true);
			}

			// --- کالا → کالای حراست
			string? partTitle;
			if (entity.PartId.HasValue)
			{
				var part = await unitOfWork.Repository<Part>().TableNoTracking
					.Where(p => p.Id == entity.PartId)
					.Select(p => new { p.Code, p.Name })
					.FirstOrDefaultAsync(ct);
				partTitle = part == null ? entity.PartTitle : $"{part.Code} - {part.Name}";
			}
			else
				partTitle = entity.PartTitle;

			if (string.IsNullOrWhiteSpace(partTitle))
				return;
			if (partTitle.Length > 256)
				partTitle = partTitle[..256];

			var guardPart = await unitOfWork.Repository<GuardPart>().TableNoTracking
				.FirstOrDefaultAsync(p => p.Title == partTitle, ct)
				?? await unitOfWork.Repository<GuardPart>().SaveAsync(new GuardPart
				{
					Title = partTitle,
					EnumerationUnit = GuardPartUnitEnum.Adad,
					Comment = !string.IsNullOrWhiteSpace(entity.Comment) ? entity.Comment : AutoComment
				}, ct, true);

			// --- علت
			var reason = entity.PartGroupId.HasValue && PartGroupToReason.TryGetValue(entity.PartGroupId.Value, out var r)
				? r : GuardIoReasonEnum.Unknown;

			// --- واحد مرتبط
			long? relatedUnitId = null;
			if (entity.PartOwnerUnitId.HasValue
				&& HtsUnitToHamkaranUnit.TryGetValue(entity.PartOwnerUnitId.Value, out var hamkaranUnitId))
			{
				relatedUnitId = await unitOfWork.Repository<OrgUnit>().TableNoTracking
					.Where(u => u.HamkaranUnitId == hamkaranUnitId)
					.Select(u => u.Id)
					.FirstOrDefaultAsync(ct);
				if (relatedUnitId == 0) relatedUnitId = null;
			}

			var operatorTitle = entity.CreatedByName;
			if (operatorTitle != null && operatorTitle.Length > 128)
				operatorTitle = operatorTitle[..128];

			var partIo = await unitOfWork.Repository<GuardPartIo>().SaveAsync(new GuardPartIo
			{
				PartEntryNotifyId = entity.Id,
				IoType = GuardIoTypeEnum.EntryNotify,
				IoDoor = GuardIoDoorEnum.Upper,
				OperatorType = GuardOperatorTypeEnum.Miscellaneous,
				OperatorTitle = operatorTitle,
				IoSourceId = location?.Id,
				Comment = Truncate($"ایجاد از طریق اعلان ورود کالا - تاریخ تقریبی ورود {entity.ApproximateEntryShamsiDate} - {entity.Comment}", 2048)
			}, ct, true);

			await unitOfWork.Repository<GuardPartIoDetail>().SaveAsync(new GuardPartIoDetail
			{
				PartIoId = partIo.Id!.Value,
				PartId = guardPart.Id!.Value,
				Mount = entity.Mount,
				IoReason = reason,
				RelatedUnitId = relatedUnitId
			}, ct, true);
		}

		private static string Truncate(string value, int max)
			=> value.Length <= max ? value : value[..max];
	}
}
