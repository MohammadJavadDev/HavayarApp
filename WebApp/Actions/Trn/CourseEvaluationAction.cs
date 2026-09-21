using Common.Utilities;
using Data.Contracts;
using Data.Contracts.Actions;
using Entities.App.Trn;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Actions.Trn
{
	/// <summary>
	/// Step 08 — قوانین ارزیابی دوره:
	/// - R6 (BeforeAdd/BeforeUpdate): اعتبارسنجی بازه ۱–۵ سوال‌ها + محاسبه میانگین در لایه اپلیکیشن.
	/// - R5 (AfterAdd): با دومین ارزیابی، یادآور پیگیری در TodoList شرکت دوره می‌سازد + فلگ IsEvaluated دوره.
	/// - AfterDelete: بازمحاسبه فلگ IsEvaluated (ارزیابی‌شده iff حداقل یک ردیف ارزیابی).
	/// </summary>
	public class CourseEvaluationAction(IUnitOfWork unitOfWork)
	{
		[EntityAction(typeof(CourseEvaluation), EntityActionTrigger.BeforeAdd, "ComputeAverageScore", "محاسبه میانگین امتیاز ارزیابی", Priority = 1)]
		public Task ComputeBeforeAdd(CourseEvaluation entity, CancellationToken ct)
		{
			ValidateAndCompute(entity);
			return Task.CompletedTask;
		}

		[EntityAction(typeof(CourseEvaluation), EntityActionTrigger.BeforeUpdate, "ComputeAverageScoreUpdate", "محاسبه میانگین امتیاز ارزیابی پس از ویرایش", Priority = 1)]
		public Task ComputeBeforeUpdate(CourseEvaluation entity, CancellationToken ct)
		{
			ValidateAndCompute(entity);
			return Task.CompletedTask;
		}

		// R5 — Auto follow-up on 2nd evaluation + evaluated flag.
		[EntityAction(typeof(CourseEvaluation), EntityActionTrigger.AfterAdd, "FollowUpOnSecondEvaluation", "یادآور پیگیری پس از دومین ارزیابی", Priority = 1)]
		public async Task FollowUpAfterAdd(CourseEvaluation entity, CancellationToken ct)
		{
			var count = await unitOfWork.Repository<CourseEvaluation>().TableNoTracking
				.CountAsync(e => e.CourseId == entity.CourseId, ct);

			// Legacy fired when one row already existed (i.e. the new row is the 2nd) — fire exactly once.
			if (count == 2)
			{
				var course = unitOfWork.Repository<Course>().TableNoTracking
					.Include(c => c.CourseBank)
					.FirstOrDefault(c => c.Id == entity.CourseId);

				// No company → still count the evaluation, skip the TodoList silently (no error).
				if (course?.CompanyId != null)
				{
					var now = DateTime.Now;
					var todo = new TodoList
					{
						CompanyId = course.CompanyId,
						Title = "تکمیل فرم ارزیابی دوره",
						Description = "فرم های ارزیابی دوره "
							+ (course.CourseBank?.CourseFieldTitle ?? string.Empty)
							+ "(" + (course.TrainingCode ?? string.Empty) + ") تکمیل شد.",
						TodoMiladiDate = now,
						TodoShamsiDate = now.ToShamsiDate()
					};
					await unitOfWork.Repository<TodoList>().SaveAsync(todo, ct, true);
				}
			}

			await SetEvaluatedFlagAsync(entity.CourseId, true, ct);
		}

		[EntityAction(typeof(CourseEvaluation), EntityActionTrigger.AfterDelete, "RefreshEvaluatedFlag", "بازمحاسبه فلگ ارزیابی دوره پس از حذف", Priority = 1)]
		public async Task RefreshAfterDelete(CourseEvaluation entity, CancellationToken ct)
		{
			var remaining = await unitOfWork.Repository<CourseEvaluation>().TableNoTracking
				.CountAsync(e => e.CourseId == entity.CourseId, ct);
			await SetEvaluatedFlagAsync(entity.CourseId, remaining > 0, ct);
		}

		private async Task SetEvaluatedFlagAsync(long courseId, bool isEvaluated, CancellationToken ct)
		{
			var course = unitOfWork.Repository<Course>().TableNoTracking
				.FirstOrDefault(c => c.Id == courseId);
			if (course == null || course.Id == null)
				return;

			// UpdateFieldsAsync bypasses Course actions (no recursion into capacity guards).
			await unitOfWork.Repository<Course>().UpdateFieldsAsync(course.Id, new Dictionary<string, object>
			{
				[nameof(Course.IsEvaluated)] = isEvaluated
			}, ct);
		}

		/// <summary>
		/// R6: هر سوال باید ۱–۵ باشد (legacy اعتبارسنجی نداشت — مقیاس پرسشنامه ثابت است)؛
		/// میانگین روی پاسخ‌های غیرتهی، گرد شده به ۲ رقم اعشار. همه تهی → null.
		/// </summary>
		private static void ValidateAndCompute(CourseEvaluation entity)
		{
			var scores = new short?[]
			{
				entity.Q1, entity.Q2, entity.Q3, entity.Q4, entity.Q5,
				entity.Q6, entity.Q7, entity.Q8, entity.Q9, entity.Q10,
				entity.Q11, entity.Q12, entity.Q13, entity.Q14, entity.Q15,
				entity.Q16, entity.Q17, entity.Q18, entity.Q19
			};

			decimal sum = 0;
			var answered = 0;
			for (var i = 0; i < scores.Length; i++)
			{
				var score = scores[i];
				if (score == null)
					continue;
				if (score < 1 || score > 5)
					throw new Exception($"امتیاز سوال {i + 1} باید بین ۱ تا ۵ باشد.");
				sum += score.Value;
				answered++;
			}

			entity.AverageScore = answered == 0 ? null : Math.Round(sum / answered, 2);
		}
	}
}
