using Data.Contracts;
using Data.Contracts.Actions;
using Data.SystemAuth;
using Entities.App.Hcm;
using Entities.App.Rpr;
using Entities.App.Rpr.Enums;
using Entities.App.SLS;
using Entities.Auth;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Microsoft.EntityFrameworkCore;
using Services.Auth;

namespace WebApp.Actions.Rpr
{
	public class RepairRequestAction(
		IUnitOfWork unitOfWork,
		IUserService userService,
		ISdk sdk,
		IConfiguration configuration)
	{
		public const string RoleAfterSale = "Rpr.RepairRequest.AfterSale";
		public const string RoleRepairs = "Rpr.RepairRequest.Repairs";
		public const string RoleView = "Rpr.RepairRequest.View";
		public const string RoleShowAll = "Rpr.RepairRequest.ShowAll";
		public const string RoleImaging = "Rpr.RepairRequest.Imaging";
		public const string RoleRepairsFull = "Rpr.Repairs";

		private RepairRequest? _previous;

		[EntityAction(typeof(RepairRequest), EntityActionTrigger.BeforeAdd,
			"RepairRequestBeforeAdd", "شماره شناسنامه و تلفن سوابق", Priority = 1)]
		public async Task BeforeAdd(RepairRequest entity, CancellationToken ct)
		{
			await FillDerivedAsync(entity, ct);
			if (string.IsNullOrWhiteSpace(entity.IdNumber))
				entity.IdNumber = await NextIdNumberAsync(ct);
		}

		[EntityAction(typeof(RepairRequest), EntityActionTrigger.BeforeUpdate,
			"RepairRequestBeforeUpdate", "قفل بخش خدمات/تعمیرات و حفظ تأییدها", Priority = 1)]
		public async Task BeforeUpdate(RepairRequest entity, CancellationToken ct)
		{
			if (entity.Id == null || entity.Id == 0)
				return;

			var existing = await unitOfWork.Repository<RepairRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == entity.Id, ct);
			_previous = existing;
			if (existing == null)
				return;

			entity.HtsId = existing.HtsId;
			if (!entity.IsConfirmOperation)
			{
				PreserveConfirms(entity, existing);
				ApplySectionLock(entity, existing);
			}

			await FillDerivedAsync(entity, ct);
		}

		[EntityAction(typeof(RepairRequest), EntityActionTrigger.AfterSave,
			"RepairRequestAfterSaveEmails", "ایمیل‌های ذخیره و تغییر وضعیت درخواست تعمیر", Priority = 2)]
		public async Task AfterSaveEmails(RepairRequest entity, CancellationToken ct)
		{
			if (entity.IsConfirmOperation)
				return;

			var previous = _previous;
			if (previous == null && entity.Id != null)
				previous = await unitOfWork.Repository<RepairRequest>().TableNoTracking
					.FirstOrDefaultAsync(c => c.Id == entity.Id, ct);

			if (entity.HasContentProductionImaging == true
				&& (previous == null || previous.HasContentProductionImaging != true))
				await SendTypedEmailAsync(entity, 1, "ثبت", null, 1, ct);

			if (previous != null)
			{
				if (!string.Equals(previous.AgreedShamsiDate, entity.AgreedShamsiDate, StringComparison.Ordinal)
					&& !string.IsNullOrWhiteSpace(entity.AgreedShamsiDate))
					await SendTypedEmailAsync(entity, 6, previous.AgreedMiladiDate == null ? "ثبت" : "ویرایش", null, 1, ct);
				if (!string.Equals(previous.Agreed2ShamsiDate, entity.Agreed2ShamsiDate, StringComparison.Ordinal)
					&& !string.IsNullOrWhiteSpace(entity.Agreed2ShamsiDate))
					await SendTypedEmailAsync(entity, 6, "ویرایش", null, 2, ct);
				if (!string.Equals(previous.Agreed3ShamsiDate, entity.Agreed3ShamsiDate, StringComparison.Ordinal)
					&& !string.IsNullOrWhiteSpace(entity.Agreed3ShamsiDate))
					await SendTypedEmailAsync(entity, 6, "ویرایش", null, 3, ct);

				if (!string.Equals(previous.QcRequestShamsiDate, entity.QcRequestShamsiDate, StringComparison.Ordinal)
					&& !string.IsNullOrWhiteSpace(entity.QcRequestShamsiDate))
					await SendTypedEmailAsync(entity, 4, "ویرایش", null, 1, ct);

				if (entity.Status == RepairRequestStatusEnum.Completed && previous.Status != RepairRequestStatusEnum.Completed)
					await SendTypedEmailAsync(entity, 9, "ویرایش", null, 1, ct);
				if (entity.Status == RepairRequestStatusEnum.DeliveredToWarehouse && previous.Status != RepairRequestStatusEnum.DeliveredToWarehouse)
					await SendTypedEmailAsync(entity, 10, "ویرایش", null, 1, ct);
			}

			await RefreshHasContractorAsync(entity.Id, ct);
		}

		public async Task SendConfirmEmailAsync(RepairRequest entity, RepairRequestConfirmType type, CancellationToken ct)
		{
			switch (type)
			{
				case RepairRequestConfirmType.PreCheckedConfirm:
					if (!string.IsNullOrWhiteSpace(entity.PreCheckShamsiDate))
						await SendTypedEmailAsync(entity, 5, null, null, 1, ct);
					break;
				case RepairRequestConfirmType.PreCheckedConfirm2:
					if (!string.IsNullOrWhiteSpace(entity.PreCheck2ShamsiDate))
						await SendTypedEmailAsync(entity, 5, null, null, 2, ct);
					break;
				case RepairRequestConfirmType.FinancialProposalConfirm:
					if (!string.IsNullOrWhiteSpace(entity.FinancialProposalShamsiDate))
						await SendTypedEmailAsync(entity, 2, null, null, 1, ct);
					break;
				case RepairRequestConfirmType.FinancialProposalConfirm2:
					if (!string.IsNullOrWhiteSpace(entity.FinancialProposal2ShamsiDate))
						await SendTypedEmailAsync(entity, 2, null, null, 2, ct);
					break;
				case RepairRequestConfirmType.FullConfirm:
					if (entity.ConfirmerId != null)
						await SendTypedEmailAsync(entity, 7, null, null, 1, ct);
					break;
				case RepairRequestConfirmType.FullConfirm2:
					if (!string.IsNullOrWhiteSpace(entity.Confirm2ShamsiDate))
						await SendTypedEmailAsync(entity, 7, null, null, 2, ct);
					break;
				case RepairRequestConfirmType.SendFinancialProposal:
					if (!string.IsNullOrWhiteSpace(entity.FinancialProposalSentShamsiDate))
						await SendTypedEmailAsync(entity, 8, null, null, 1, ct);
					break;
			}
		}

		public async Task SendContractorCostEmailAsync(RepairRequest entity, RepairRequestContractor row, CancellationToken ct)
		{
			entity.Comment = row.Comment;
			await SendTypedEmailAsync(entity, 3, "ثبت", BuildContractorLine(row), 1, ct);
		}

		public bool HasAfterSaleAccess()
			=> HasRole(RoleAfterSale, RoleRepairsFull) || HasPath("aftersaleaccess");

		public bool HasRepairsAccess()
			=> HasRole(RoleRepairs, RoleRepairsFull) || HasPath("repairsaccess");

		public bool HasConfirmAccess()
			=> HasRole(RoleAfterSale, RoleRepairsFull) || HasPath("confirm");

		public bool HasImagingAccess()
			=> HasRole(RoleImaging, RoleRepairsFull) || HasPath("contentproductionimaging");

		private bool HasRole(params string[] names)
		{
			if (sdk.CurrentUser == null)
				return false;
			if (sdk.HasRole("ShowAllMenus") || sdk.HasRole("admin"))
				return true;
			return names.Any(n => sdk.HasRole(n));
		}

		private bool HasPath(string action)
		{
			if (sdk.CurrentUser == null)
				return false;
			var needle = "/panel/rpr/repairrequest/" + action;
			return sdk.CurrentUser.RoleAccess?.Any(c =>
				c.Path != null && c.Path.Equals(needle, StringComparison.OrdinalIgnoreCase)) == true;
		}

		private void ApplySectionLock(RepairRequest entity, RepairRequest existing)
		{
			var afterSale = HasAfterSaleAccess();
			var repairs = HasRepairsAccess();
			if (afterSale && repairs)
				return;
			if (!afterSale)
				CopyAfterSale(entity, existing);
			if (!repairs)
				CopyRepairs(entity, existing);
		}

		private static void PreserveConfirms(RepairRequest entity, RepairRequest existing)
		{
			entity.ConfirmerId = existing.ConfirmerId;
			entity.ConfirmMiladiDate = existing.ConfirmMiladiDate;
			entity.ConfirmShamsiDate = existing.ConfirmShamsiDate;
			entity.ConfirmTime = existing.ConfirmTime;
			entity.Confirm2MiladiDate = existing.Confirm2MiladiDate;
			entity.Confirm2ShamsiDate = existing.Confirm2ShamsiDate;
			entity.IsConfirmed2 = existing.IsConfirmed2;
			entity.PreCheckMiladiDate = existing.PreCheckMiladiDate;
			entity.PreCheckShamsiDate = existing.PreCheckShamsiDate;
			entity.PreCheck2MiladiDate = existing.PreCheck2MiladiDate;
			entity.PreCheck2ShamsiDate = existing.PreCheck2ShamsiDate;
			entity.IsPreChecked2 = existing.IsPreChecked2;
			entity.FinancialProposalMiladiDate = existing.FinancialProposalMiladiDate;
			entity.FinancialProposalShamsiDate = existing.FinancialProposalShamsiDate;
			entity.FinancialProposal2MiladiDate = existing.FinancialProposal2MiladiDate;
			entity.FinancialProposal2ShamsiDate = existing.FinancialProposal2ShamsiDate;
			entity.IsApprovedFinancialProposal2 = existing.IsApprovedFinancialProposal2;
			entity.IsSentFinancialProposal = existing.IsSentFinancialProposal;
			entity.FinancialProposalSentMiladiDate = existing.FinancialProposalSentMiladiDate;
			entity.FinancialProposalSentShamsiDate = existing.FinancialProposalSentShamsiDate;
			entity.HasContractor = existing.HasContractor;
		}

		private static void CopyAfterSale(RepairRequest entity, RepairRequest existing)
		{
			entity.IdNumber = existing.IdNumber;
			entity.ServiceRequestId = existing.ServiceRequestId;
			entity.ProductId = existing.ProductId;
			entity.Serial = existing.Serial;
			entity.CustomerId = existing.CustomerId;
			entity.CustomerAgencyId = existing.CustomerAgencyId;
			entity.CustomerAddressId = existing.CustomerAddressId;
			entity.CustomerAddressManual = existing.CustomerAddressManual;
			entity.ZoneId = existing.ZoneId;
			entity.ZoneSupervisorId = existing.ZoneSupervisorId;
			entity.CustomerPhone = existing.CustomerPhone;
			entity.CustomerPhoneFromHistory = existing.CustomerPhoneFromHistory;
			entity.RepresentationReception = existing.RepresentationReception;
			entity.DestinationAfterRepair = existing.DestinationAfterRepair;
			entity.DestinationAddress = existing.DestinationAddress;
			entity.IsGuarantee = existing.IsGuarantee;
			entity.CostCenterId = existing.CostCenterId;
			entity.EntryMiladiDate = existing.EntryMiladiDate;
			entity.EntryShamsiDate = existing.EntryShamsiDate;
			entity.CustomerNeedMiladiDate = existing.CustomerNeedMiladiDate;
			entity.CustomerNeedShamsiDate = existing.CustomerNeedShamsiDate;
			entity.JobDescription = existing.JobDescription;
			entity.PreFactorNumber = existing.PreFactorNumber;
			entity.LoanFormNumber = existing.LoanFormNumber;
			entity.SafekeepingReturnFormNumber = existing.SafekeepingReturnFormNumber;
			entity.SafekeepingReturnMiladiDate = existing.SafekeepingReturnMiladiDate;
			entity.SafekeepingReturnShamsiDate = existing.SafekeepingReturnShamsiDate;
			entity.FailureReason = existing.FailureReason;
			entity.Comment = existing.Comment;
			entity.HasContentProductionImaging = existing.HasContentProductionImaging;
			entity.IsHavayarEquipment = existing.IsHavayarEquipment;
		}

		private static void CopyRepairs(RepairRequest entity, RepairRequest existing)
		{
			entity.InspectionPersonelId = existing.InspectionPersonelId;
			entity.DlId = existing.DlId;
			entity.ExpertIds = existing.ExpertIds;
			entity.ExpertsInText = existing.ExpertsInText;
			entity.Status = existing.Status;
			entity.StartMiladiDate = existing.StartMiladiDate;
			entity.StartShamsiDate = existing.StartShamsiDate;
			entity.EndMiladiDate = existing.EndMiladiDate;
			entity.EndShamsiDate = existing.EndShamsiDate;
			entity.RepairEntranceMiladiDate = existing.RepairEntranceMiladiDate;
			entity.RepairEntranceShamsiDate = existing.RepairEntranceShamsiDate;
			entity.DeliveryFormNumber = existing.DeliveryFormNumber;
			entity.WarehouseDeliveryMiladiDate = existing.WarehouseDeliveryMiladiDate;
			entity.WarehouseDeliveryShamsiDate = existing.WarehouseDeliveryShamsiDate;
			entity.AgreedMiladiDate = existing.AgreedMiladiDate;
			entity.AgreedShamsiDate = existing.AgreedShamsiDate;
			entity.Agreed2MiladiDate = existing.Agreed2MiladiDate;
			entity.Agreed2ShamsiDate = existing.Agreed2ShamsiDate;
			entity.Agreed3MiladiDate = existing.Agreed3MiladiDate;
			entity.Agreed3ShamsiDate = existing.Agreed3ShamsiDate;
			entity.DeliveringRecipient = existing.DeliveringRecipient;
			entity.BuyRequestMiladiDate = existing.BuyRequestMiladiDate;
			entity.BuyRequestShamsiDate = existing.BuyRequestShamsiDate;
			entity.SupplyMiladiDate = existing.SupplyMiladiDate;
			entity.SupplyShamsiDate = existing.SupplyShamsiDate;
			entity.QcRequestMiladiDate = existing.QcRequestMiladiDate;
			entity.QcRequestShamsiDate = existing.QcRequestShamsiDate;
			entity.StationId = existing.StationId;
			entity.PartFractionIds = existing.PartFractionIds;
			entity.PartFractionsInText = existing.PartFractionsInText;
			entity.RepairDescription = existing.RepairDescription;
			entity.Unrepairable = existing.Unrepairable;
			entity.NeedForSendToEmployer = existing.NeedForSendToEmployer;
			entity.ExitMiladiDate = existing.ExitMiladiDate;
			entity.ExitShamsiDate = existing.ExitShamsiDate;
			entity.HasDelay = existing.HasDelay;
		}

		private async Task FillDerivedAsync(RepairRequest entity, CancellationToken ct)
		{
			if (entity.CustomerId != null && entity.CustomerId != 0)
			{
				var party = await unitOfWork.Repository<Customer>().TableNoTracking
					.Where(c => c.Id == entity.CustomerId)
					.Select(c => c.Party)
					.FirstOrDefaultAsync(ct);
				if (party != null)
				{
					var phones = string.Join(" | ", new[] { party.Phone, party.Mobile }
						.Where(s => !string.IsNullOrWhiteSpace(s)));
					if (phones.Length > 0)
						entity.CustomerPhoneFromHistory = phones.Length > 2048 ? phones[..2048] : phones;
				}
			}

			if (!string.IsNullOrWhiteSpace(entity.ExpertIds))
			{
				var ids = entity.ExpertIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
					.Select(x => long.TryParse(x, out var id) ? id : 0)
					.Where(x => x > 0)
					.ToList();
				if (ids.Count > 0)
				{
					var names = await unitOfWork.Repository<Personel>().TableNoTracking
						.Where(p => p.Id != null && (
							ids.Contains(p.Id.Value)
							|| (p.HamkaranId != null && ids.Contains(p.HamkaranId.Value))))
						.Select(p => ((p.Name ?? "") + " " + (p.Family ?? "")).Trim())
						.Where(n => n != "")
						.ToListAsync(ct);
					if (names.Count > 0)
						entity.ExpertsInText = string.Join(" ، ", names);
				}
			}

			if (!string.IsNullOrWhiteSpace(entity.PartFractionIds))
			{
				var ids = entity.PartFractionIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
					.Select(x => long.TryParse(x, out var id) ? id : 0)
					.Where(x => x > 0)
					.ToList();
				if (ids.Count > 0)
				{
					var titles = await unitOfWork.Repository<RepairRequestPartFraction>().TableNoTracking
						.Where(p => p.Id != null && (ids.Contains(p.Id.Value) || ids.Contains(p.HtsId)))
						.Select(p => p.Title)
						.ToListAsync(ct);
					if (titles.Count > 0)
						entity.PartFractionsInText = string.Join(" ، ", titles);
				}
			}
		}

		private async Task<string> NextIdNumberAsync(CancellationToken ct)
		{
			var last = await unitOfWork.Repository<RepairRequest>().TableNoTracking
				.Where(x => x.IdNumber != null && x.IdNumber != "")
				.OrderByDescending(x => x.IdNumber)
				.Select(x => x.IdNumber)
				.FirstOrDefaultAsync(ct);
			if (long.TryParse(last, out var n))
				return (n + 1).ToString();
			return "1";
		}

		private async Task RefreshHasContractorAsync(long? repairRequestId, CancellationToken ct)
		{
			if (repairRequestId == null || repairRequestId == 0)
				return;
			var has = await unitOfWork.Repository<RepairRequestContractor>().TableNoTracking
				.AnyAsync(c => c.RepairRequestId == repairRequestId, ct);
			var row = await unitOfWork.Repository<RepairRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == repairRequestId, ct);
			if (row == null || row.HasContractor == has)
				return;
			row.HasContractor = has;
			row.IsConfirmOperation = true;
			await unitOfWork.Repository<RepairRequest>().UpdateAsync(row, ct, true, true, false);
		}

		private async Task SendTypedEmailAsync(
			RepairRequest entity,
			int type,
			string? operation,
			string? extraHtml,
			int version,
			CancellationToken ct)
		{
			var supervisorEmail = await ResolveSupervisorEmailAsync(entity.ZoneSupervisorId, ct);
			var (to, cc, subject) = Recipients(type, version, supervisorEmail);
			if (type == 1)
			{
				var imaging = await userService.GetUsersByRoleName(RoleImaging);
				if (imaging != null)
				{
					foreach (var user in imaging)
					{
						var mail = ToHavayarEmail(user.Email ?? user.Username);
						if (!string.IsNullOrWhiteSpace(mail))
							to.Add(mail);
					}
				}
			}
			if (to.Count == 0)
				return;

			var product = entity.ProductId == null ? null : await unitOfWork.Repository<Entities.App.Inv.Part>().TableNoTracking
				.Where(p => p.Id == entity.ProductId)
				.Select(p => new { p.Code, p.Name })
				.FirstOrDefaultAsync(ct);

			var body = BuildBody(entity, type, operation, version, extraHtml, product?.Code, product?.Name);
			await unitOfWork.Repository<Notification>().AddAsync(new Notification
			{
				Type = NotificationType.Email,
				Title = subject,
				Body = body,
				EntityId = entity.Id,
				OwnerId = sdk.CurrentUser?.Id ?? 1,
				ViewPath = "/Panel/Rpr/RepairRequest/Edit?id=" + entity.Id,
				IsRead = false,
				IsSend = false,
				ToEmails = to,
				CcEmails = cc.Count == 0 ? null : cc
			}, ct);
			await unitOfWork.SaveChangesAsync(ct);
		}

		private async Task<string?> ResolveSupervisorEmailAsync(long? userId, CancellationToken ct)
		{
			if (userId == null || userId == 0)
				return null;
			var user = await userService.TableNoTracking
				.Where(u => u.Id == userId)
				.Select(u => new { u.Email, u.Username })
				.FirstOrDefaultAsync(ct);
			return ToHavayarEmail(user?.Email ?? user?.Username);
		}

		private static (List<string> To, List<string> Cc, string Subject) Recipients(int type, int version, string? supervisor)
		{
			var to = new List<string>();
			var cc = new List<string>();
			var subject = "";
			void Add(params string[] items)
			{
				foreach (var item in items)
				{
					if (string.IsNullOrWhiteSpace(item))
						continue;
					to.Add(item.Contains('@') ? item : item + "@havayar.com");
				}
			}

			switch (type)
			{
				case 1:
					subject = "تصویر برداری تولید محتوا";
					break;
				case 2:
					Add("Asadpour.h@havayar.com", "Khodabandeh.f@havayar.com", "Kamandar.m@havayar.com", "Ahsani.e@havayar.com");
					subject = version == 1 ? "ارسال پیشنهاد مالی درخواست تعمیر" : "ارسال پیشنهاد مالی درخواست تعمیر " + version;
					break;
				case 3:
					Add("Khodabandeh.f@havayar.com", "Kamandar.m@havayar.com", "Ahsani.e@havayar.com", "kalhor.m@havayar.com", supervisor);
					subject = "هزینه تعمیر پیمانکاری";
					break;
				case 4:
					Add("Badrkhani.s@havayar.com", "Mehrabi.s@havayar.com");
					cc.Add("Salim.sh@havayar.com");
					cc.Add("Khodabandeh.f@havayar.com");
					subject = "تاریخ درخواست تست";
					break;
				case 5:
					Add("Khodabandeh.f@havayar.com", "Kamandar.m@havayar.com", "Asadpour.h@havayar.com", supervisor);
					subject = version == 1 ? "ارسال پیش بررسی درخواست تعمیر" : "ارسال پیش بررسی " + version + " درخواست تعمیر";
					break;
				case 6:
					Add("Khodabandeh.f@havayar.com", "Kamandar.m@havayar.com", "Ahsani.e@havayar.com", "khederzadeh.s@havayar.com", "kalhor.m@havayar.com", supervisor);
					subject = version == 1 ? "تاریخ توافقی درخواست تعمیر" : "تاریخ توافقی " + version + " درخواست تعمیر";
					break;
				case 7:
					Add("Asadpour.h@havayar.com", "Khodabandeh.f@havayar.com", "Kamandar.m@havayar.com", "Ahsani.e@havayar.com", supervisor);
					subject = version == 1 ? "تایید نهایی درخواست تعمیر" : "تایید نهایی درخواست تعمیر " + version;
					break;
				case 8:
					Add("Khodabandeh.f@havayar.com", supervisor);
					subject = "ثبت تاریخ ارسال پیشنهاد مالی درخواست تعمیر";
					break;
				case 9:
					Add("Khodabandeh.f@havayar.com", "Kamandar.m@havayar.com", "Ahsani.e@havayar.com", "kalhor.m@havayar.com", supervisor);
					subject = "تغییر وضعیت شناسنامه تعمیر به حالت تکمیل شده";
					break;
				case 10:
					Add("Khodabandeh.f@havayar.com", "Kamandar.m@havayar.com", "Ahsani.e@havayar.com", "kalhor.m@havayar.com", supervisor);
					subject = "تغییر وضعیت شناسنامه تعمیر به حالت تحویل به انبار";
					break;
			}

			return (to.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
				cc.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
				subject);
		}

		private string BuildBody(
			RepairRequest entity,
			int type,
			string? operation,
			int version,
			string? extraHtml,
			string? partCode,
			string? partName)
		{
			var line = type switch
			{
				1 => $"درخواست تعمیر با شماره شناسنامه {entity.IdNumber} با ویژگی تصویربرداری محتوا در سیستم {operation} گردید.",
				2 => version == 1
					? $"درخواست تعمیر با شماره شناسنامه {entity.IdNumber} در وضعیت ارسال پیشنهاد مالی قرار گرفت."
					: $"درخواست تعمیر با شماره شناسنامه {entity.IdNumber} در وضعیت ارسال پیشنهاد مالی {version} قرار گرفت.",
				4 => $"درخواست تعمیر با شماره شناسنامه {entity.IdNumber} در وضعیت در انتظار تست برای تاریخ {entity.QcRequestShamsiDate} قرار گرفته است.",
				5 => version == 1
					? $"درخواست تعمیر با شماره شناسنامه {entity.IdNumber} در وضعیت ارسال پیش بررسی قرار گرفت."
					: $"درخواست تعمیر با شماره شناسنامه {entity.IdNumber} در وضعیت ارسال پیش بررسی {version} قرار گرفت.",
				6 => version == 1
					? $"تاریخ توافقی درخواست تعمیر با شماره شناسنامه {entity.IdNumber} به {entity.AgreedShamsiDate} {operation} گردید"
					: version == 2
						? $"تاریخ توافقی {version} درخواست تعمیر با شماره شناسنامه {entity.IdNumber} به {entity.Agreed2ShamsiDate} {operation} گردید"
						: $"تاریخ توافقی {version} درخواست تعمیر با شماره شناسنامه {entity.IdNumber} به {entity.Agreed3ShamsiDate} {operation} گردید",
				7 => version == 1
					? $"درخواست تعمیر با شماره شناسنامه {entity.IdNumber} در وضعیت تایید نهایی قرار گرفت."
					: $"درخواست تعمیر با شماره شناسنامه {entity.IdNumber} در وضعیت تایید نهایی {version} قرار گرفت.",
				8 => $"برای درخواست تعمیر با شماره شناسنامه {entity.IdNumber} تاریخ ارسال پیشنهاد مالی به تاریخ {entity.FinancialProposalSentShamsiDate} ثبت گردید",
				9 => $"درخواست تعمیر با شماره شناسنامه {entity.IdNumber} در وضعیت تکمیل شده قرار گرفته است.",
				10 => $"درخواست تعمیر با شماره شناسنامه {entity.IdNumber} در وضعیت تحویل به انبار قرار گرفته است.",
				_ => extraHtml ?? ""
			};

			return "<div style='text-align:center;direction:rtl'>"
				+ "<table border='1' cellspacing='0' cellpadding='5' style='text-align:right;direction:rtl' width='672'>"
				+ "<tr style='background:#000aa0'><td colspan='2'><div style='font-size:14pt;color:#fff;text-align:center'>گروه صنعتی هوایار</div></td></tr>"
				+ "<tr><td colspan='2'><div style='font-size:14pt;color:#1F497D;text-align:right'>"
				+ "با سلام و احترام <br />" + line + "<br/>"
				+ (extraHtml ?? "")
				+ "<ul>"
				+ $"<li>سایت مشتری : <strong>{entity.CustomerAddressManual}</strong></li>"
				+ (string.IsNullOrWhiteSpace(partCode) ? "" : $"<li>کد تجهیز : <strong>{partCode}</strong></li>")
				+ (string.IsNullOrWhiteSpace(partName) ? "" : $"<li>عنوان تجهیز : <strong>{partName}</strong></li>")
				+ "</ul></div></td></tr></table></div>";
		}

		private static string BuildContractorLine(RepairRequestContractor row)
			=> $"برای درخواست تعمیر هزینه تعمیر پیمانکار {row.ContractorTitle} به مبلغ {row.Cost} ریال در نظر گرفته شده است.<br/>توضیحات : <strong>{row.Comment}</strong><br/>";

		private static string? ToHavayarEmail(string? value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return null;
			var trimmed = value.Trim();
			return trimmed.Contains('@') ? trimmed : trimmed + "@havayar.com";
		}
	}

	public class RepairRequestContractorAction(IUnitOfWork unitOfWork, RepairRequestAction headerAction)
	{
		[EntityAction(typeof(RepairRequestContractor), EntityActionTrigger.AfterSave,
			"RepairRequestContractorAfterSave", "پرچم پیمانکار و ایمیل هزینه", Priority = 1)]
		public async Task AfterSave(RepairRequestContractor entity, CancellationToken ct)
		{
			if (entity.RepairRequestId == null)
				return;
			var header = await unitOfWork.Repository<RepairRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == entity.RepairRequestId, ct);
			if (header == null)
				return;
			header.HasContractor = true;
			header.IsConfirmOperation = true;
			await unitOfWork.Repository<RepairRequest>().UpdateAsync(header, ct, true, true, false);
			if (entity.Cost != null && entity.Cost != 0)
				await headerAction.SendContractorCostEmailAsync(header, entity, ct);
		}

		[EntityAction(typeof(RepairRequestContractor), EntityActionTrigger.AfterDelete,
			"RepairRequestContractorAfterDelete", "به‌روزرسانی پرچم پیمانکار", Priority = 1)]
		public async Task AfterDelete(RepairRequestContractor entity, CancellationToken ct)
		{
			if (entity.RepairRequestId == null)
				return;
			var has = await unitOfWork.Repository<RepairRequestContractor>().TableNoTracking
				.AnyAsync(c => c.RepairRequestId == entity.RepairRequestId, ct);
			var header = await unitOfWork.Repository<RepairRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == entity.RepairRequestId, ct);
			if (header == null)
				return;
			header.HasContractor = has;
			header.IsConfirmOperation = true;
			await unitOfWork.Repository<RepairRequest>().UpdateAsync(header, ct, true, true, false);
		}
	}

	public class RepairRequestPartAction(IUnitOfWork unitOfWork)
	{
		[EntityAction(typeof(RepairRequestPart), EntityActionTrigger.BeforeSave,
			"RepairRequestPartTotals", "جمع ریالی قطعه تعمیر", Priority = 1)]
		public Task BeforeSave(RepairRequestPart entity, CancellationToken ct)
		{
			var net = entity.UsedMount - entity.ReturnMount;
			// HTS trigger stores totals on the part row; Havayar has no TotalPrice column — keep prices as entered.
			_ = net;
			_ = unitOfWork;
			_ = ct;
			return Task.CompletedTask;
		}
	}
}
