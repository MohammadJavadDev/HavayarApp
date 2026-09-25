using Data.Contracts;
using Data.Contracts.Actions;
using Entities.App.Cng;
using Entities.App.SLS;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Actions.Cng
{
	/// <summary>
	/// معادل منطق HTS CngRepairsController.DoOperation:
	/// تولید شماره شناسنامه، حفظ فیلدهای تایید، تلفن از سوابق مشتری.
	/// </summary>
	public class RepairsAction(IUnitOfWork unitOfWork)
	{
		[EntityAction(typeof(Repairs), EntityActionTrigger.BeforeAdd,
			"RepairsGenerateIdNumber", "تولید شماره شناسنامه تعمیرات CNG", Priority = 1)]
		public async Task BeforeAdd(Repairs entity, CancellationToken ct)
		{
			if (!string.IsNullOrWhiteSpace(entity.IdNumber))
				return;

			var lastIdNumber = await unitOfWork.Repository<Repairs>().TableNoTracking
				.Where(r => r.IdNumber != null && r.IdNumber != "")
				.OrderByDescending(r => r.Id)
				.Select(r => r.IdNumber)
				.FirstOrDefaultAsync(ct);

			const string defaultIdNumber = "1001";
			entity.IdNumber = lastIdNumber != null && int.TryParse(lastIdNumber, out var n)
				? (n + 1).ToString()
				: defaultIdNumber;
		}

		[EntityAction(typeof(Repairs), EntityActionTrigger.BeforeUpdate,
			"RepairsPreserveProtectedFields", "حفظ شماره شناسنامه و فیلدهای تایید", Priority = 1)]
		public async Task BeforeUpdate(Repairs entity, CancellationToken ct)
		{
			if (entity.Id == null || entity.Id == 0)
				return;

			var existing = await unitOfWork.Repository<Repairs>().TableNoTracking
				.FirstOrDefaultAsync(r => r.Id == entity.Id, ct);
			if (existing == null)
				return;

			entity.IdNumber = existing.IdNumber;
			entity.ConfirmerId = existing.ConfirmerId;
			entity.ConfirmMiladiDate = existing.ConfirmMiladiDate;
			entity.ConfirmShamsiDate = existing.ConfirmShamsiDate;
			entity.ConfirmTime = existing.ConfirmTime;
			entity.HtsId = existing.HtsId;
			if (entity.PartEntryNotifyId == null || entity.PartEntryNotifyId == 0)
				entity.PartEntryNotifyId = existing.PartEntryNotifyId;
		}

		[EntityAction(typeof(Repairs), EntityActionTrigger.BeforeSave,
			"RepairsFillPhoneFromHistory", "پر کردن تلفن مشتری از سوابق", Priority = 2)]
		public async Task BeforeSave(Repairs entity, CancellationToken ct)
		{
			if (entity.CustomerId == null || entity.CustomerId == 0)
				return;

			var party = await unitOfWork.Repository<Customer>().TableNoTracking
				.Where(c => c.Id == entity.CustomerId)
				.Select(c => c.Party)
				.FirstOrDefaultAsync(ct);
			if (party == null)
				return;

			var phones = string.Join(" | ", new[] { party.Phone, party.Mobile }
				.Where(s => !string.IsNullOrWhiteSpace(s)));
			if (phones.Length == 0)
				return;

			entity.CustomerPhoneFromHistory = phones.Length > 2048 ? phones[..2048] : phones;
		}
	}
}
