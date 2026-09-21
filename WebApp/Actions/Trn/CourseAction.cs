using Common.Utilities;
using Data.Contracts;
using Data.Contracts.Actions;
using Entities.App.Trn;
using Entities.App.Trn.Enums;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Actions.Trn
{
	public class CourseAction(IUnitOfWork unitOfWork) : IEntityAction<Course>
	{
		// R1 — TrainingCode auto-generation (runs on every Add regardless of caller).
		// Prefix TRCNG for CNG-field courses, TRHY otherwise; sequential number, unique via index.
		[EntityAction(typeof(Course), EntityActionTrigger.BeforeAdd, "GenerateTrainingCode", "تولید کد آموزش", Priority = 1)]
		public async Task GenerateTrainingCode(Course entity, CancellationToken ct)
		{
			// R2-init — RemainingCapacity starts equal to Capacity on first save.
			entity.RemainingCapacity = entity.Capacity;

			if (entity.TrainingCode.HasValue())
			{
				await Task.CompletedTask;
				return;
			}

			var bank = unitOfWork.Repository<CourseBank>().TableNoTracking
				.FirstOrDefault(b => b.Id == entity.CourseBankId);
			var prefix = bank?.CourseField == CourseFieldEnum.Cng ? "TRCNG" : "TRHY";

			var codes = await unitOfWork.Repository<Course>().TableNoTracking
				.Where(c => c.TrainingCode != null && c.TrainingCode.StartsWith(prefix))
				.Select(c => c.TrainingCode!)
				.ToListAsync(ct);

			var maxSeq = 0;
			foreach (var code in codes)
			{
				var suffix = code.Length > prefix.Length ? code.Substring(prefix.Length) : string.Empty;
				if (int.TryParse(suffix, out var parsed) && parsed > maxSeq)
					maxSeq = parsed;
			}

			var seq = maxSeq + 1;
			string candidate;
			do
			{
				candidate = $"{prefix}{seq:0000}";
				seq++;
			} while (await unitOfWork.Repository<Course>().TableNoTracking.AnyAsync(c => c.TrainingCode == candidate, ct));

			entity.TrainingCode = candidate;
			await Task.CompletedTask;
		}

		// R2-guard — Capacity may not drop below the already-registered count (both tracks).
		[EntityAction(typeof(Course), EntityActionTrigger.BeforeUpdate, "CheckCapacityNotBelowEnrolled", "کنترل عدم کاهش ظرفیت زیر تعداد ثبت‌نام‌شدگان", Priority = 1)]
		public async Task CheckCapacityNotBelowEnrolled(Course entity, CancellationToken ct)
		{
			if (entity.Id == null)
			{
				await Task.CompletedTask;
				return;
			}

			var enrolled =
				await unitOfWork.Repository<CourseParticipant>().TableNoTracking.CountAsync(p => p.CourseId == entity.Id, ct)
				+ await unitOfWork.Repository<CourseCompanyParticipant>().TableNoTracking.CountAsync(p => p.CourseId == entity.Id, ct);

			if (entity.Capacity < enrolled)
				throw new Exception($"ظرفیت دوره ({entity.Capacity}) نمی‌تواند کمتر از تعداد ثبت‌نام‌شدگان ({enrolled}) باشد.");

			await Task.CompletedTask;
		}
	}
}
