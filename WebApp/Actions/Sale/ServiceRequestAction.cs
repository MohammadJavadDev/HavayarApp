using Data.Contracts;
using Data.Contracts.Actions;
using Entities.App.Hcm;
using Entities.App.Rpr;
using Entities.App.Sale;
using Entities.App.SLS;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Microsoft.EntityFrameworkCore;
using Services.Auth;
using Services.ProductionOrderServices;
using System.Text;

namespace WebApp.Actions.Sale
{
	/// <summary>
	/// معادل DoOperation درخواست پشتیبانی HTS: تلفن از سوابق، نام محصولات، همگام کارشناس و درخواست تعمیر.
	/// </summary>
	public class ServiceRequestAction(IUnitOfWork unitOfWork, IUserService userService)
	{
		[EntityAction(typeof(ServiceRequest), EntityActionTrigger.BeforeSave,
			"ServiceRequestFillDerived", "تلفن سوابق، حفظ فیلدهای غیر فرمی و نام کارشناسان", Priority = 1)]
		public async Task BeforeSave(ServiceRequest entity, CancellationToken ct)
		{
			if (entity.Id != null && entity.Id != 0)
			{
				var existing = await unitOfWork.Repository<ServiceRequest>().TableNoTracking
					.FirstOrDefaultAsync(c => c.Id == entity.Id, ct);
				if (existing != null)
				{
					entity.HtsId = existing.HtsId;
					if (entity.DispatchMiladiDate == null)
						entity.DispatchMiladiDate = existing.DispatchMiladiDate;
					if (string.IsNullOrWhiteSpace(entity.DispatchShamsiDate))
						entity.DispatchShamsiDate = existing.DispatchShamsiDate;
					if (entity.CreatedOrgUnitId == null || entity.CreatedOrgUnitId == 0)
						entity.CreatedOrgUnitId = existing.CreatedOrgUnitId;
					if (entity.IsWaitingToSend == null)
						entity.IsWaitingToSend = existing.IsWaitingToSend;
					if (string.IsNullOrWhiteSpace(entity.CustomerPhoneFromHistory))
						entity.CustomerPhoneFromHistory = existing.CustomerPhoneFromHistory;
					if (entity.ResponsiblePersonelId == null || entity.ResponsiblePersonelId == 0)
						entity.ResponsiblePersonelId = existing.ResponsiblePersonelId;
					if (entity.ResponsibleZoneId == null)
						entity.ResponsibleZoneId = existing.ResponsibleZoneId;
				}
			}

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

			if (entity.ExpertPersonelIds != null)
			{
				var personelIds = ParseIds(entity.ExpertPersonelIds);
				if (personelIds.Count > 0)
				{
					var names = await unitOfWork.Repository<Personel>().TableNoTracking
						.Where(p => p.Id != null && personelIds.Contains(p.Id.Value))
						.Select(p => ((p.Name ?? "") + " " + (p.Family ?? "")).Trim())
						.Where(n => n != "")
						.ToListAsync(ct);
					if (names.Count > 0)
					{
						var joined = string.Join(" ، ", names);
						entity.ExpertMission = joined.Length > 2048 ? joined[..2048] : joined;
					}
				}
			}

			if (entity.Id == null || entity.Id == 0)
				return;

			var productName = await ServiceRequestProductNames.BuildAsync(unitOfWork, entity.Id.Value, ct);
			if (productName != null)
				entity.AllProductName = productName;
		}

		[EntityAction(typeof(ServiceRequest), EntityActionTrigger.AfterSave,
			"ServiceRequestSyncChildren", "همگام کارشناس ماموریت و درخواست تعمیر", Priority = 2)]
		public async Task AfterSave(ServiceRequest entity, CancellationToken ct)
		{
			if (entity.Id == null || entity.Id == 0)
				return;

			if (entity.ExpertPersonelIds != null)
				await SyncExpertMissions(entity.Id.Value, entity.ExpertPersonelIds, ct);

			// ExpertPersonelIds ([NotMapped]) فقط از فرم ذخیره می‌آید. AfterSave حالا روی هر UpdateAsync هم اجرا می‌شود
			// (مثلا SendDispatch با موجودیت خوانده‌شده از دیتابیس)؛ در آن حالت LegacyRepairRequestIds مقدار کهنه است
			// و همگام‌سازی، تعمیرهای لینک‌شده از سمت درخواست تعمیر را unlink می‌کرد.
			if (entity.LegacyRepairRequestIds != null && entity.ExpertPersonelIds != null)
				await SyncRepairRequests(entity.Id.Value, entity.LegacyRepairRequestIds, ct);
		}

		private async Task SyncExpertMissions(long serviceRequestId, string csv, CancellationToken ct)
		{
			var selected = ParseIds(csv);
			var existing = await unitOfWork.Repository<ServiceRequestExpertMission>().TableNoTracking
				.Where(c => c.ServiceRequestId == serviceRequestId)
				.ToListAsync(ct);

			var keepPersonelIds = new HashSet<long>();
			foreach (var row in existing)
			{
				var personelId = row.PersonelId is > 0
					? row.PersonelId
					: await ResolvePersonelIdFromUser(row.ExpertId, ct);
				var keep = personelId != null && selected.Contains(personelId.Value);
				if (!keep)
				{
					await unitOfWork.Repository<ServiceRequestExpertMission>().DeleteAsync(row, ct, true, false);
					continue;
				}

				keepPersonelIds.Add(personelId.Value);
				if ((row.PersonelId == null || row.PersonelId == 0) && personelId != null)
				{
					row.PersonelId = personelId;
					if (row.ExpertId == null || row.ExpertId == 0)
						row.ExpertId = await ResolveUserId(personelId.Value, ct);
					await unitOfWork.Repository<ServiceRequestExpertMission>().UpdateAsync(row, ct, true, true, false);
				}
			}

			foreach (var personelId in selected)
			{
				if (keepPersonelIds.Contains(personelId))
					continue;

				var mission = new ServiceRequestExpertMission
				{
					ServiceRequestId = serviceRequestId,
					PersonelId = personelId,
					ExpertId = await ResolveUserId(personelId, ct)
				};
				await unitOfWork.Repository<ServiceRequestExpertMission>().AddAsync(mission, ct, true, true, true);
			}
		}

		private async Task SyncRepairRequests(long serviceRequestId, string csv, CancellationToken ct)
		{
			var selected = ParseIds(csv);
			var linked = await unitOfWork.Repository<RepairRequest>().TableNoTracking
				.Where(c => c.ServiceRequestId == serviceRequestId)
				.ToListAsync(ct);

			foreach (var row in linked)
			{
				if (row.Id == null || selected.Contains(row.Id.Value))
					continue;
				row.ServiceRequestId = null;
				await unitOfWork.Repository<RepairRequest>().UpdateAsync(row, ct, true, true, false);
			}

			foreach (var id in selected)
			{
				var row = await unitOfWork.Repository<RepairRequest>().TableNoTracking
					.FirstOrDefaultAsync(c => c.Id == id, ct);
				if (row == null || row.ServiceRequestId == serviceRequestId)
					continue;
				row.ServiceRequestId = serviceRequestId;
				await unitOfWork.Repository<RepairRequest>().UpdateAsync(row, ct, true, true, false);
			}
		}

		private async Task<long?> ResolveUserId(long personelId, CancellationToken ct)
		{
			var partyId = await unitOfWork.Repository<Personel>().TableNoTracking
				.Where(p => p.Id == personelId)
				.Select(p => p.PartyId)
				.FirstOrDefaultAsync(ct);
			if (partyId == null || partyId == 0)
				return null;
			return await userService.TableNoTracking
				.Where(u => u.PartyId == partyId)
				.Select(u => u.Id)
				.FirstOrDefaultAsync(ct);
		}

		private async Task<long?> ResolvePersonelIdFromUser(long? expertId, CancellationToken ct)
		{
			if (expertId == null || expertId == 0)
				return null;
			var partyId = await userService.TableNoTracking
				.Where(u => u.Id == expertId)
				.Select(u => u.PartyId)
				.FirstOrDefaultAsync(ct);
			if (partyId == null || partyId == 0)
				return null;
			return await unitOfWork.Repository<Personel>().TableNoTracking
				.Where(p => p.PartyId == partyId)
				.Select(p => p.Id)
				.FirstOrDefaultAsync(ct);
		}

		internal static List<long> ParseIds(string? csv)
		{
			if (string.IsNullOrWhiteSpace(csv))
				return new List<long>();
			return csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
				.Select(s => long.TryParse(s.Trim(), out var id) ? id : 0)
				.Where(id => id > 0)
				.Distinct()
				.ToList();
		}
	}

	public class ServiceRequestDetailAction(IUnitOfWork unitOfWork)
	{
		[EntityAction(typeof(ServiceRequestDetail), EntityActionTrigger.AfterSave,
			"ServiceRequestDetailRefreshProducts", "به‌روزرسانی نام محصولات درخواست", Priority = 1)]
		public async Task AfterSave(ServiceRequestDetail entity, CancellationToken ct)
			=> await ServiceRequestProductNames.ApplyAsync(unitOfWork, entity.ServiceRequestId, ct);

		[EntityAction(typeof(ServiceRequestDetail), EntityActionTrigger.AfterDelete,
			"ServiceRequestDetailRefreshProductsOnDelete", "به‌روزرسانی نام محصولات پس از حذف", Priority = 1)]
		public async Task AfterDelete(ServiceRequestDetail entity, CancellationToken ct)
			=> await ServiceRequestProductNames.ApplyAsync(unitOfWork, entity.ServiceRequestId, ct);
	}

	internal static class ServiceRequestProductNames
	{
		public static async Task ApplyAsync(IUnitOfWork unitOfWork, long? serviceRequestId, CancellationToken ct)
		{
			if (serviceRequestId == null || serviceRequestId == 0)
				return;
			var request = await unitOfWork.Repository<ServiceRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == serviceRequestId, ct);
			if (request == null)
				return;
			request.AllProductName = await BuildAsync(unitOfWork, serviceRequestId.Value, ct);
			await unitOfWork.Repository<ServiceRequest>().UpdateAsync(request, ct, true, true, false);
		}

		public static async Task<string?> BuildAsync(IUnitOfWork unitOfWork, long serviceRequestId, CancellationToken ct)
		{
			var names = await unitOfWork.Repository<ServiceRequestDetail>().TableNoTracking
				.Where(d => d.ServiceRequestId == serviceRequestId)
				.Select(d =>
					d.OtherPart != null ? d.OtherPart.Name
					: d.OrderDetail != null && d.OrderDetail.Part != null ? d.OrderDetail.Part.Name
					: d.OrderDetailSerial != null && d.OrderDetailSerial.OrderDetail != null && d.OrderDetailSerial.OrderDetail.Part != null
						? d.OrderDetailSerial.OrderDetail.Part.Name
					: d.OtherPartSerial)
				.Where(n => n != null && n != "")
				.Distinct()
				.ToListAsync(ct);
			if (names.Count == 0)
				return null;
			var joined = string.Join(" ، ", names);
			return joined.Length > 4000 ? joined[..4000] : joined;
		}
	}

	public class ServiceRequestExpertMissionAction(IUnitOfWork unitOfWork, IUserService userService)
	{
		[EntityAction(typeof(ServiceRequestExpertMission), EntityActionTrigger.BeforeSave,
			"ServiceRequestExpertResolveUser", "پر کردن کاربر از پرسنل", Priority = 1)]
		public async Task BeforeSave(ServiceRequestExpertMission entity, CancellationToken ct)
		{
			if (entity.PersonelId == null || entity.PersonelId == 0)
				return;
			if (entity.ExpertId != null && entity.ExpertId != 0)
				return;

			var partyId = await unitOfWork.Repository<Personel>().TableNoTracking
				.Where(p => p.Id == entity.PersonelId)
				.Select(p => p.PartyId)
				.FirstOrDefaultAsync(ct);
			if (partyId == null || partyId == 0)
				return;

			var userId = await userService.TableNoTracking
				.Where(u => u.PartyId == partyId)
				.Select(u => u.Id)
				.FirstOrDefaultAsync(ct);
			if (userId != null && userId != 0)
				entity.ExpertId = userId;
		}

		[EntityAction(typeof(ServiceRequestExpertMission), EntityActionTrigger.AfterAdd,
			"ServiceRequestExpertNotify", "اعلان اعزام کارشناس به مسئول منطقه", Priority = 1)]
		public async Task AfterAdd(ServiceRequestExpertMission entity, CancellationToken ct)
		{
			if (entity.ServiceRequestId == null || entity.ServiceRequestId == 0)
				return;

			var request = await unitOfWork.Repository<ServiceRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == entity.ServiceRequestId, ct);
			if (request == null)
				return;

			var expertName = "";
			if (entity.PersonelId != null)
			{
				var personel = await unitOfWork.Repository<Personel>().TableNoTracking
					.FirstOrDefaultAsync(p => p.Id == entity.PersonelId, ct);
				if (personel != null)
					expertName = ((personel.Name ?? "") + " " + (personel.Family ?? "")).Trim();
			}
			if (expertName.Length == 0 && entity.ExpertId != null)
			{
				var expert = await userService.GetById(entity.ExpertId.Value);
				expertName = expert?.Name ?? "";
			}

			await ServiceRequestResponsibleNotify.NotifyAsync(
				unitOfWork,
				userService,
				request,
				expertName,
				ct);
		}
	}

	internal static class ServiceRequestResponsibleNotify
	{
		public static async Task NotifyAsync(
			IUnitOfWork unitOfWork,
			IUserService userService,
			ServiceRequest request,
			string? expertName,
			CancellationToken ct)
		{
			if (request.ResponsiblePersonelId is not > 0)
				return;

			var personel = await unitOfWork.Repository<Personel>().TableNoTracking
				.FirstOrDefaultAsync(p => p.Id == request.ResponsiblePersonelId, ct);
			if (personel == null)
				return;

			long? userId = null;
			string? userEmail = null;
			if (personel.PartyId is > 0)
			{
				var user = await userService.TableNoTracking
					.Where(u => u.PartyId == personel.PartyId)
					.Select(u => new { u.Id, u.Email })
					.FirstOrDefaultAsync(ct);
				userId = user?.Id;
				userEmail = user?.Email;
			}

			var email = !string.IsNullOrWhiteSpace(personel.Email) ? personel.Email : userEmail;
			if (userId is not > 0 && string.IsNullOrWhiteSpace(email))
				return;

			var bodyHtml = await BuildDispatchEmailAsync(unitOfWork, request, expertName, ct);

			await unitOfWork.Repository<Notification>().AddAsync(new Notification
			{
				Type = NotificationType.Email,
				Title = "اعزام کارشناس",
				Body = bodyHtml,
				EntityId = request.Id,
				OwnerId = userId is > 0 ? userId.Value : 1,
				ViewPath = "/Panel/Sale/ServiceRequest/Edit?id=" + request.Id,
				IsRead = false,
				IsSend = false,
				ToEmails = string.IsNullOrWhiteSpace(email) ? null : new List<string> { email }
			}, ct);
		}

		private static async Task<string> BuildDispatchEmailAsync(
			IUnitOfWork unitOfWork,
			ServiceRequest request,
			string? expertName,
			CancellationToken ct)
		{
			var info = request.Id is not > 0
				? null
				: await unitOfWork.Repository<ServiceRequest>().TableNoTracking
					.Where(c => c.Id == request.Id)
					.Select(c => new
					{
						c.Id,
						CustomerCode = c.Customer != null ? c.Customer.Code : null,
						CustomerName = c.Customer != null && c.Customer.Party != null ? c.Customer.Party.FullName : null,
						SiteTitle = c.CustomerAddress != null ? c.CustomerAddress.Title : null,
						SiteAddress = c.CustomerAddress != null ? c.CustomerAddress.Address : null,
						ZoneTitle = c.DispatchZone != null ? c.DispatchZone.Title : null,
						c.DispatchShamsiDate,
						c.Comment,
						c.AllProductName,
						c.CustomerAgentName,
						c.CustomerAgentJobPosition,
						c.CustomerAgentTell,
						c.ExpertMission
					})
					.FirstOrDefaultAsync(ct);

			var customer = JoinParts(info?.CustomerCode?.ToString(), info?.CustomerName);
			var agent = JoinParts(info?.CustomerAgentName, info?.CustomerAgentJobPosition);
			var experts = info?.ExpertMission;
			if (string.IsNullOrWhiteSpace(experts))
				experts = expertName;
			else if (!string.IsNullOrWhiteSpace(expertName)
				&& experts.IndexOf(expertName, StringComparison.Ordinal) < 0)
				experts = experts + " ، " + expertName;

			if (string.IsNullOrWhiteSpace(experts) && request.Id is > 0)
			{
				var rows = await unitOfWork.Repository<ServiceRequestExpertMission>().TableNoTracking
					.Where(m => m.ServiceRequestId == request.Id)
					.Select(m => new
					{
						Name = m.Personel != null ? m.Personel.Name : null,
						Family = m.Personel != null ? m.Personel.Family : null,
						UserName = m.Expert != null ? m.Expert.Name : null
					})
					.ToListAsync(ct);
				experts = string.Join(" ، ", rows
					.Select(r =>
					{
						var full = ((r.Name ?? "") + " " + (r.Family ?? "")).Trim();
						return full.Length > 0 ? full : r.UserName;
					})
					.Where(n => !string.IsNullOrWhiteSpace(n))
					.Distinct());
			}

			var dispatchDate = !string.IsNullOrWhiteSpace(info?.DispatchShamsiDate)
				? info!.DispatchShamsiDate
				: request.DispatchShamsiDate;

			var items = new StringBuilder();
			items.Append(ProductionOrderItemNotificationHelper.Li("شماره اندیکاتور", info?.Id ?? request.Id));
			items.Append(ProductionOrderItemNotificationHelper.Li("مشتری", customer));
			AppendIf(items, "سایت مشتری", info?.SiteTitle);
			AppendIf(items, "آدرس سایت", info?.SiteAddress);
			AppendIf(items, "منطقه", info?.ZoneTitle);
			items.Append(ProductionOrderItemNotificationHelper.Li("کارشناس", experts));
			items.Append(ProductionOrderItemNotificationHelper.Li("تاریخ اعزام کارشناس", dispatchDate));
			items.Append(ProductionOrderItemNotificationHelper.Li("علت اعزام کارشناس", info?.Comment ?? request.Comment));
			AppendIf(items, "محصولات", info?.AllProductName);
			AppendIf(items, "نماینده مشتری", agent);
			AppendIf(items, "تلفن نماینده", info?.CustomerAgentTell);

			return ProductionOrderItemNotificationHelper.BuildEmailShell(
				"درخواست اعزام کارشناس با مشخصات ذیل در سیستم ثبت شد:<br /><br /><ul>"
				+ items
				+ "</ul>");
		}

		private static void AppendIf(StringBuilder sb, string label, string? value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return;
			sb.Append(ProductionOrderItemNotificationHelper.Li(label, value));
		}

		private static string? JoinParts(string? left, string? right)
		{
			var parts = new[] { left, right }
				.Where(s => !string.IsNullOrWhiteSpace(s))
				.Select(s => s!.Trim())
				.ToArray();
			return parts.Length == 0 ? null : string.Join(" | ", parts);
		}
	}
}
