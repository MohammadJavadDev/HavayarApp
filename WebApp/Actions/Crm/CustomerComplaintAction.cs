using Common.Utilities;
using Data.Contracts;
using Data.Contracts.Actions;
using Entities.App.Crm;
using Entities.App.Hcm;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Entities.Base.Sms;
using Microsoft.EntityFrameworkCore;
using Services.Auth;
using Services.Sms;

namespace WebApp.Actions.Crm
{
	/// <summary>معادل تریگر GetCustomerComplaintSerialNumber و پیامک/ایمیل ذخیره شکایت.</summary>
	public class CustomerComplaintSaveAction(IUnitOfWork unitOfWork, ISmsService sms, IUserService userService)
	{
		[EntityAction(typeof(CustomerComplaint), EntityActionTrigger.BeforeAdd, "AssignComplaintSerial", "شماره سریال و کد رهگیری شکایت", Priority = 1)]
		public async Task AssignSerial(CustomerComplaint entity, CancellationToken ct)
		{
			if (entity.ModuleTypeId == 0)
				entity.ModuleTypeId = ComplaintRules.ModuleCustomerComplaint;
			if (entity.ModuleTypeId != ComplaintRules.ModuleCustomerComplaint)
				return;
			if (!string.IsNullOrWhiteSpace(entity.SerialNumber) && !string.IsNullOrWhiteSpace(entity.TrackingCode))
				return;

			var year = DateTime.Now.ToShamsiDate();
			year = year.Length >= 4 ? year[..4] : year;
			var repo = unitOfWork.Repository<CustomerComplaint>().TableNoTracking
				.Where(c => c.ModuleTypeId == ComplaintRules.ModuleCustomerComplaint);

			if (string.IsNullOrWhiteSpace(entity.SerialNumber))
			{
				var prefix = year + "-C-";
				var last = await MaxSuffix(repo.Where(c => c.SerialNumber != null && c.SerialNumber.StartsWith(prefix)).Select(c => c.SerialNumber!), 7, ct);
				entity.SerialNumber = prefix + (last + 1).ToString("000");
			}

			if (string.IsNullOrWhiteSpace(entity.TrackingCode))
			{
				var prefix = year + "-";
				var last = await MaxSuffix(repo.Where(c => c.TrackingCode != null && c.TrackingCode.StartsWith(prefix) && !c.TrackingCode.Contains("-C-")).Select(c => c.TrackingCode!), 5, ct);
				entity.TrackingCode = prefix + (last + 1).ToString("000");
			}
		}

		[EntityAction(typeof(CustomerComplaint), EntityActionTrigger.BeforeUpdate, "KeepComplaintIdentity", "حفظ سریال و شناسه HTS", Priority = 1)]
		public async Task KeepIdentity(CustomerComplaint entity, CustomerComplaint? oldEntity, CancellationToken ct)
		{
			if (oldEntity == null)
				return;
			entity.HtsId = oldEntity.HtsId;
			entity.ModuleTypeId = oldEntity.ModuleTypeId == 0 ? ComplaintRules.ModuleCustomerComplaint : oldEntity.ModuleTypeId;
			if (!string.IsNullOrWhiteSpace(oldEntity.SerialNumber))
				entity.SerialNumber = oldEntity.SerialNumber;
			if (!string.IsNullOrWhiteSpace(oldEntity.TrackingCode))
				entity.TrackingCode = oldEntity.TrackingCode;
			await Task.CompletedTask;
		}

		[EntityAction(typeof(CustomerComplaint), EntityActionTrigger.AfterAdd, "NotifyComplaintExperts", "پیامک کارشناسان شکایت جدید", Priority = 20)]
		public async Task NotifyExperts(CustomerComplaint entity, CancellationToken ct)
		{
			if (entity.ModuleTypeId != ComplaintRules.ModuleCustomerComplaint)
				return;
			var ids = ParseIds(entity.ExpertOrManagerIds);
			if (ids.Count == 0)
				return;

			var mobiles = await unitOfWork.Repository<Personel>().TableNoTracking
				.Where(p => p.Id != null && ids.Contains(p.Id.Value) && p.Mobile != null && p.Mobile != "")
				.Select(p => p.Mobile!)
				.ToListAsync(ct);
			if (mobiles.Count == 0)
				return;

			await sms.TryEnqueueAsync(
				SmsEventKeys.CrmComplaintNotifyStaff,
				string.Join(";", mobiles.Distinct()),
				SmsTemplates.ComplaintNotifyStaff(entity.SerialNumber),
				nameof(CustomerComplaint),
				entity.Id,
				ct: ct);
		}

		[EntityAction(typeof(CustomerComplaint), EntityActionTrigger.AfterSave, "EmailComplaintViewers", "ایمیل مشاهده‌گران شکایت", Priority = 30)]
		public async Task EmailViewers(CustomerComplaint entity, CustomerComplaint? oldEntity, CancellationToken ct)
		{
			if (entity.ModuleTypeId != ComplaintRules.ModuleCustomerComplaint || entity.Id is not > 0)
				return;

			var current = ParseIds(entity.ViewersIds);
			var previous = ParseIds(oldEntity?.ViewersIds);
			var added = current.Except(previous).ToList();
			if (added.Count == 0)
				return;

			var users = await userService.TableNoTracking
				.Where(u => u.Id != null && added.Contains(u.Id.Value))
				.Select(u => new { u.Id, u.Email })
				.ToListAsync(ct);

			var body = "شکایت مشتری"
				+ (string.IsNullOrWhiteSpace(entity.SerialNumber) ? "" : " به شماره سریال " + entity.SerialNumber)
				+ " جهت مشاهده برای شما ارسال شد.";
			var path = "/Panel/Crm/CustomerComplaint/Edit?id=" + entity.Id;

			foreach (var user in users)
			{
				if (user.Id is not > 0)
					continue;
				await unitOfWork.Repository<Notification>().AddAsync(new Notification
				{
					Type = NotificationType.Email,
					Title = "شکایات مشتری",
					Body = body,
					EntityId = entity.Id,
					OwnerId = user.Id.Value,
					ViewPath = path,
					IsRead = false,
					IsSend = false,
					ToEmails = string.IsNullOrWhiteSpace(user.Email) ? null : new List<string> { user.Email }
				}, ct, InvokeAction: false);
			}
		}

		private static async Task<int> MaxSuffix(IQueryable<string> values, int startIndex, CancellationToken ct)
		{
			var list = await values.ToListAsync(ct);
			var max = 0;
			foreach (var value in list)
			{
				if (string.IsNullOrWhiteSpace(value) || value.Length <= startIndex)
					continue;
				var tail = value[startIndex..];
				if (int.TryParse(tail, out var n) && n > max)
					max = n;
			}
			return max;
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
