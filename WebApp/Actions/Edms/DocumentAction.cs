using Data.Contracts;
using Data.Contracts.Actions;
using Data.SystemAuth;
using Entities.App.Edms;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Actions.Edms
{
	/// <summary>
	/// تهیه‌کننده مدرک هنگام ایجاد از تهیه‌کننده VPIS کپی و روی خود مدرک ثابت می‌شود
	/// (معادل Edms_Document.PreparedUser_FK در HTS). تغییر بعدی تهیه‌کننده VPIS روی
	/// ریویژن‌های قبلی و گزارش‌های عملکرد پرسنل اثری ندارد.
	/// </summary>
	public class DocumentAction(IUnitOfWork unitOfWork, ISdk sdk)
	{
		[EntityAction(typeof(Document), EntityActionTrigger.BeforeAdd, "SetPreparedUser", "ثبت تهیه‌کننده مدرک از VPIS", Priority = 1)]
		public async Task SetPreparedUser(Document entity, CancellationToken ct)
		{
			if (entity.PreparedUserId is > 0)
				return;

			if (entity.DocumentVpisId is > 0)
			{
				entity.PreparedUserId = await unitOfWork.Repository<ProjectVpis>().TableNoTracking
					.Where(v => v.Id == entity.DocumentVpisId.Value)
					.Select(v => v.ProducerId)
					.FirstOrDefaultAsync(ct);
			}

			if (entity.PreparedUserId is not > 0)
				entity.PreparedUserId = entity.CreatedById is > 0 ? entity.CreatedById : sdk.CurrentUser?.Id;
		}
	}
}
