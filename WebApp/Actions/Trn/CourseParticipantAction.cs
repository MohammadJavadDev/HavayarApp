using Data.Contracts;
using Data.Contracts.Actions;
using Entities.App.Trn;
using Entities.App.Trn.Enums;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Actions.Trn
{
	/// <summary>
	/// Step 07 — قوانین ثبت‌نام track عمومی (R4a..R4d):
	/// - BeforeAdd: گارد track + ظرفیت + تکراری نبودن (لایه دوم، علاوه بر کنترلر — گارد دو لایه)
	/// - AfterAdd: کاهش RemainingCapacity دوره
	/// - AfterDelete: افزایش RemainingCapacity دوره
	/// </summary>
	public class CourseParticipantAction(IUnitOfWork unitOfWork)
	{
		[EntityAction(typeof(CourseParticipant), EntityActionTrigger.BeforeAdd, "ValidatePublicEnrollment", "اعتبارسنجی ثبت‌نام عمومی", Priority = 1)]
		public async Task ValidateBeforeAdd(CourseParticipant entity, CancellationToken ct)
		{
			var course = unitOfWork.Repository<Course>().TableNoTracking
				.FirstOrDefault(c => c.Id == entity.CourseId);
			if (course == null)
				throw new Exception("دوره یافت نشد.");

			if (!course.IsPublicParticipantTrack())
				throw new Exception("ثبت‌نام عمومی فقط برای دوره‌های فراخوان عمومی یا CNG مجاز است.");

			// R4c — No overbooking.
			var remaining = course.RemainingCapacity ?? course.Capacity;
			if (remaining <= 0)
				throw new Exception("ظرفیت دوره تکمیل است و امکان ثبت‌نام وجود ندارد.");

			// R4d — No double registration (check both tracks, not just the current one).
			var duplicate =
				await unitOfWork.Repository<CourseParticipant>().TableNoTracking
					.AnyAsync(p => p.CourseId == entity.CourseId && p.ParticipantId == entity.ParticipantId, ct)
				|| await unitOfWork.Repository<CourseCompanyParticipant>().TableNoTracking
					.AnyAsync(p => p.CourseId == entity.CourseId && p.ParticipantId == entity.ParticipantId, ct);
			if (duplicate)
				throw new Exception("این شرکت‌کننده قبلاً در این دوره ثبت‌نام شده است.");

			CopyCompanyFromParticipant(entity);
		}

		[EntityAction(typeof(CourseParticipant), EntityActionTrigger.BeforeUpdate, "CopyCompanyFromParticipant", "کپی شرکت از شرکت‌کننده", Priority = 2)]
		public Task CopyCompanyOnUpdate(CourseParticipant entity, CancellationToken ct)
		{
			CopyCompanyFromParticipant(entity);
			return Task.CompletedTask;
		}

		private void CopyCompanyFromParticipant(CourseParticipant entity)
		{
			if (entity.ParticipantId == 0)
				return;
			var participant = unitOfWork.Repository<Participant>().TableNoTracking
				.FirstOrDefault(p => p.Id == entity.ParticipantId);
			if (participant != null)
				entity.CompanyId = participant.CompanyId;
		}

		// R4a — Capacity decrement (recalculated from both tracks so it self-heals).
		[EntityAction(typeof(CourseParticipant), EntityActionTrigger.AfterAdd, "DecrementRemainingCapacity", "کاهش ظرفیت باقی‌مانده پس از ثبت‌نام", Priority = 1)]
		public async Task DecrementAfterAdd(CourseParticipant entity, CancellationToken ct)
		{
			await RecalculateRemainingCapacityAsync(entity.CourseId, ct);
		}

		// R4b — Capacity increment.
		[EntityAction(typeof(CourseParticipant), EntityActionTrigger.AfterDelete, "IncrementRemainingCapacity", "افزایش ظرفیت باقی‌مانده پس از حذف ثبت‌نام", Priority = 1)]
		public async Task IncrementAfterDelete(CourseParticipant entity, CancellationToken ct)
		{
			await RecalculateRemainingCapacityAsync(entity.CourseId, ct);
		}

		private async Task RecalculateRemainingCapacityAsync(long courseId, CancellationToken ct)
		{
			var course = unitOfWork.Repository<Course>().TableNoTracking
				.FirstOrDefault(c => c.Id == courseId);
			if (course == null || course.Id == null)
				return;

			var enrolled =
				await unitOfWork.Repository<CourseParticipant>().TableNoTracking.CountAsync(p => p.CourseId == courseId, ct)
				+ await unitOfWork.Repository<CourseCompanyParticipant>().TableNoTracking.CountAsync(p => p.CourseId == courseId, ct);

			var remaining = (short)(course.Capacity - enrolled);
			if (remaining < 0)
				remaining = 0;

			// UpdateFieldsAsync bypasses Course actions (no recursion into CheckCapacityNotBelowEnrolled).
			await unitOfWork.Repository<Course>().UpdateFieldsAsync(course.Id, new Dictionary<string, object>
			{
				[nameof(Course.RemainingCapacity)] = remaining
			}, ct);
		}
	}
}
