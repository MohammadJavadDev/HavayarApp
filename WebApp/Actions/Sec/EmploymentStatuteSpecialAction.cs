using Data.Contracts;
using Data.Contracts.Actions;
using Entities.App.Sec;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Actions.Sec
{
	public class EmploymentStatuteSpecialAction(IUnitOfWork unitOfWork) : IEntityAction<EmploymentStatuteSpecial>
	{
		[EntityAction(typeof(EmploymentStatuteSpecial), EntityActionTrigger.AfterSave, "SyncSecretariatEmploymentDate", "به‌روزرسانی تاریخ شروع به کار پرسنل دبیرخانه", Priority = 1)]
		public Task SyncOnSave(EmploymentStatuteSpecial entity, CancellationToken ct)
			=> SyncEmploymentDate(entity, ct);

		[EntityAction(typeof(EmploymentStatuteSpecial), EntityActionTrigger.AfterDelete, "SyncSecretariatEmploymentDateOnDelete", "به‌روزرسانی تاریخ شروع به کار پرسنل دبیرخانه پس از حذف", Priority = 1)]
		public Task SyncOnDelete(EmploymentStatuteSpecial entity, CancellationToken ct)
			=> SyncEmploymentDate(entity, ct);

		private async Task SyncEmploymentDate(EmploymentStatuteSpecial entity, CancellationToken ct)
		{
			if (entity.PersonelId == 0)
				return;

			var personnel = await unitOfWork.Repository<Personnel>().Table
				.Where(c => c.PersonelId == entity.PersonelId)
				.OrderBy(c => c.Id)
				.FirstOrDefaultAsync(ct);
			if (personnel == null)
				return;

			var next = Normalize(entity.RealEmploymentShamsiDate);
			var current = Normalize(personnel.EmploymentDate);
			if (string.Equals(current, next, StringComparison.Ordinal))
				return;

			personnel.EmploymentDate = next;
			await unitOfWork.Repository<Personnel>().UpdateAsync(personnel, ct, true);
		}

		private static string? Normalize(string? value)
		{
			var trimmed = value?.Trim();
			return string.IsNullOrEmpty(trimmed) ? null : trimmed;
		}
	}
}
