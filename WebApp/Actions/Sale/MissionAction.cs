using Data.Contracts;
using Data.Contracts.Actions;
using Entities.App.Hcm;
using Entities.App.Sale;
using Microsoft.EntityFrameworkCore;
using Services.Auth;

namespace WebApp.Actions.Sale
{
	/// <summary>
	/// HTS DoMissionManagementOperation only converts Shamsi dates (already bound) and stamps creator.
	/// Hokm_Number / Have_Gurantee are user-entered on the HTS form; on Add we assign the next hokm if empty.
	/// کارشناس روی فرم پرسنل است؛ ExpertId از Party پر می‌شود.
	/// </summary>
	public class MissionAction(IUnitOfWork unitOfWork, IUserService userService)
	{
		[EntityAction(typeof(Mission), EntityActionTrigger.BeforeAdd,
			"MissionAssignHokm", "تولید شماره حکم ماموریت در صورت خالی بودن", Priority = 1)]
		public async Task BeforeAdd(Mission entity, CancellationToken ct)
		{
			if (entity.HokmNumber != 0)
				return;
			var max = await unitOfWork.Repository<Mission>().TableNoTracking
				.Select(c => (int?)c.HokmNumber)
				.MaxAsync(ct);
			entity.HokmNumber = (max ?? 0) + 1;
		}

		[EntityAction(typeof(Mission), EntityActionTrigger.BeforeSave,
			"MissionSyncPersonelExpert", "همگام‌سازی کارشناس و پرسنل", Priority = 2)]
		public async Task BeforeSave(Mission entity, CancellationToken ct)
		{
			if (entity.PersonelId is > 0 && (entity.ExpertId == null || entity.ExpertId == 0))
			{
				var partyId = await unitOfWork.Repository<Personel>().TableNoTracking
					.Where(p => p.Id == entity.PersonelId)
					.Select(p => p.PartyId)
					.FirstOrDefaultAsync(ct);
				if (partyId is not > 0)
					return;

				var userIds = await userService.TableNoTracking
					.Where(u => u.PartyId == partyId)
					.Select(u => u.Id)
					.ToListAsync(ct);
				if (userIds.Count == 0)
					return;

				if (userIds.Count == 1)
				{
					entity.ExpertId = userIds[0];
					return;
				}

				var preferred = await unitOfWork.Repository<MissionSalary>().TableNoTracking
					.Where(s => s.PersonelId == entity.PersonelId && s.ExpertId != null && userIds.Contains(s.ExpertId.Value))
					.Select(s => s.ExpertId)
					.FirstOrDefaultAsync(ct);
				entity.ExpertId = preferred ?? userIds.Min();
				return;
			}

			if ((entity.PersonelId == null || entity.PersonelId == 0) && entity.ExpertId is > 0)
			{
				var partyId = await userService.TableNoTracking
					.Where(u => u.Id == entity.ExpertId)
					.Select(u => u.PartyId)
					.FirstOrDefaultAsync(ct);
				if (partyId is not > 0)
					return;
				var personelId = await unitOfWork.Repository<Personel>().TableNoTracking
					.Where(p => p.PartyId == partyId)
					.Select(p => p.Id)
					.FirstOrDefaultAsync(ct);
				if (personelId is > 0)
					entity.PersonelId = personelId;
			}
		}
	}
}
