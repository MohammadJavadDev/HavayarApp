using Common.Utilities;
using Data;
using Entities.App.Gnr;
using Entities.App.SLS;
using Entities.App.Trn;
using Entities.App.Trn.Enums;
using Microsoft.EntityFrameworkCore;

namespace Data.Services.Trn;

/// <summary>
/// گزارش آماری دوره‌ها — معادل HTS CourseReport/GetStatisticalData
/// (spCourseReporting + GetTrainingActivityFieldReport + GetTeacherEvaluation).
/// </summary>
public class CourseStatisticalReportService(ApplicationDbContext db) : ICourseStatisticalReportService
{
	public async Task<CourseStatisticalReportResponse> GetReportAsync(
		CourseStatisticalReportFilter filter,
		CancellationToken cancellationToken = default)
	{
		var (fromDate, toDate) = ResolveDateRange(filter);

		var courses = await db.Set<Course>().AsNoTracking()
			.Where(c =>
				c.StartMiladiDate != null &&
				c.EndMiladiDate != null &&
				c.StartMiladiDate >= fromDate &&
				c.StartMiladiDate <= toDate &&
				c.EndMiladiDate >= fromDate &&
				c.EndMiladiDate <= toDate)
			.Select(c => new CourseRow
			{
				Id = c.Id ?? 0,
				TrainingCode = c.TrainingCode,
				ExecutingMethod = c.ExecutingMethod,
				DurationInMinute = c.DurationInMinute,
				Cost = c.Cost,
				CompanyId = c.CompanyId,
				TeacherId = c.TeacherId,
				SecondTeacherId = c.SecondTeacherId
			})
			.ToListAsync(cancellationToken);

		var courseIds = courses.Select(c => c.Id).ToList();

		var teacherFlags = await LoadTeacherFlagsAsync(courses, cancellationToken);
		var totals = BuildTotals(courses, teacherFlags);

		if (courseIds.Count == 0)
		{
			return new CourseStatisticalReportResponse { Totals = totals };
		}

		var evaluations = await db.Set<CourseEvaluation>().AsNoTracking()
			.Where(e => courseIds.Contains(e.CourseId))
			.Select(e => new EvaluationRow
			{
				CourseId = e.CourseId,
				Q1 = e.Q1, Q2 = e.Q2, Q3 = e.Q3, Q4 = e.Q4, Q5 = e.Q5,
				Q6 = e.Q6, Q7 = e.Q7, Q8 = e.Q8, Q9 = e.Q9, Q10 = e.Q10,
				Q11 = e.Q11, Q12 = e.Q12, Q13 = e.Q13, Q14 = e.Q14,
				Q15 = e.Q15, Q16 = e.Q16, Q17 = e.Q17, Q18 = e.Q18, Q19 = e.Q19
			})
			.ToListAsync(cancellationToken);

		var courseEval = BuildCourseEvaluations(evaluations);
		if (courseEval.Count > 0)
			totals.FinalEvalAverage = Math.Round(courseEval.Average(c => c.FinalValidTotalEval), 2);

		var teacherEvaluations = await BuildTeacherEvaluationsAsync(courses, courseEval, cancellationToken);
		var activityFields = await BuildActivityFieldsAsync(courses, courseIds, cancellationToken);

		return new CourseStatisticalReportResponse
		{
			Totals = totals,
			ActivityFields = activityFields,
			TeacherEvaluations = teacherEvaluations
		};
	}

	private static (DateTime From, DateTime To) ResolveDateRange(CourseStatisticalReportFilter filter)
	{
		DateTime from;
		DateTime to;
		try
		{
			from = string.IsNullOrWhiteSpace(filter.FromDateShamsi)
				? DateTime.Today.AddYears(-10)
				: filter.FromDateShamsi.ToMiladiDate().Date;
		}
		catch
		{
			from = DateTime.Today.AddYears(-10);
		}

		try
		{
			to = string.IsNullOrWhiteSpace(filter.ToDateShamsi)
				? DateTime.Today.AddYears(10)
				: filter.ToDateShamsi.ToMiladiDate().Date;
		}
		catch
		{
			to = DateTime.Today.AddYears(10);
		}

		if (to < from)
			(from, to) = (to, from);

		return (from, to.AddDays(1).AddTicks(-1));
	}

	private async Task<Dictionary<long, bool>> LoadTeacherFlagsAsync(
		List<CourseRow> courses,
		CancellationToken cancellationToken)
	{
		var teacherIds = courses
			.SelectMany(c => new long?[] { c.TeacherId, c.SecondTeacherId })
			.Where(id => id.HasValue && id.Value > 0)
			.Select(id => id!.Value)
			.Distinct()
			.ToList();

		if (teacherIds.Count == 0)
			return [];

		return await db.Set<TeacherBank>().AsNoTracking()
			.Where(t => teacherIds.Contains(t.Id ?? 0))
			.Select(t => new { t.Id, t.IsHavayarPerson })
			.ToDictionaryAsync(t => t.Id ?? 0, t => t.IsHavayarPerson, cancellationToken);
	}

	private static CourseStatisticalTotals BuildTotals(
		List<CourseRow> courses,
		Dictionary<long, bool> teacherFlags)
	{
		var totals = new CourseStatisticalTotals
		{
			AllCourseCount = courses.Count,
			AllCourseHour = courses.Sum(c => c.DurationInMinute ?? 0),
			AllCourseCountCng = courses.Count(IsCng),
			AllCourseHourCng = courses.Where(IsCng).Sum(c => c.DurationInMinute ?? 0),
			AllPublicCourseCount = courses.Count(c => c.ExecutingMethod == CourseExecutingMethodEnum.PublicCall),
			AllPublicCourseHour = courses.Where(c => c.ExecutingMethod == CourseExecutingMethodEnum.PublicCall)
				.Sum(c => c.DurationInMinute ?? 0),
			AllPrivateCourseCount = courses.Count(c => c.ExecutingMethod == CourseExecutingMethodEnum.Private),
			AllPrivateCourseHour = courses.Where(c => c.ExecutingMethod == CourseExecutingMethodEnum.Private)
				.Sum(c => c.DurationInMinute ?? 0),
			CourseCost = courses.Sum(c => c.Cost ?? 0)
		};

		totals.AllCourseCountHy = totals.AllCourseCount - totals.AllCourseCountCng;
		totals.AllCourseHourHy = totals.AllCourseHour - totals.AllCourseHourCng;

		totals.AllCourseCountCngPercentage = ToPercent(totals.AllCourseCountCng, totals.AllCourseCount);
		totals.AllCourseCountHyPercentage = ToPercent(totals.AllCourseCountHy, totals.AllCourseCount);
		totals.AllPublicCourseCountPercentage = ToPercent(totals.AllPublicCourseCount, totals.AllCourseCount);
		totals.AllPrivateCourseCountPercentage = ToPercent(totals.AllPrivateCourseCount, totals.AllCourseCount);
		totals.AllCourseHourCngPercentage = ToPercent(totals.AllCourseHourCng, totals.AllCourseHour);
		totals.AllCourseHourHyPercentage = ToPercent(totals.AllCourseHourHy, totals.AllCourseHour);
		totals.AllPublicCourseHourPercentage = ToPercent(totals.AllPublicCourseHour, totals.AllCourseHour);
		totals.AllPrivateCourseHourPercentage = ToPercent(totals.AllPrivateCourseHour, totals.AllCourseHour);

		var havayar = 0;
		var notHavayar = 0;
		foreach (var course in courses)
		{
			CountTeacher(course.TeacherId, teacherFlags, ref havayar, ref notHavayar);
			CountTeacher(course.SecondTeacherId, teacherFlags, ref havayar, ref notHavayar);
		}

		totals.AllHavayariTeacherCount = havayar;
		totals.AllNotHavayariTeacherCount = notHavayar;
		return totals;
	}

	private static void CountTeacher(
		long? teacherId,
		Dictionary<long, bool> teacherFlags,
		ref int havayar,
		ref int notHavayar)
	{
		if (teacherId == null || teacherId <= 0)
			return;
		if (!teacherFlags.TryGetValue(teacherId.Value, out var isHavayar))
			return;
		if (isHavayar)
			havayar++;
		else
			notHavayar++;
	}

	private static bool IsCng(CourseRow course) =>
		course.TrainingCode != null &&
		course.TrainingCode.Contains("CNG", StringComparison.OrdinalIgnoreCase);

	private static string ToPercent(int part, int whole)
	{
		if (whole <= 0)
			return "0%";
		return Math.Round(part * 100d / whole, 0) + "%";
	}

	private static List<CourseEvalAggregate> BuildCourseEvaluations(List<EvaluationRow> rows)
	{
		var result = new List<CourseEvalAggregate>();
		foreach (var group in rows.GroupBy(r => r.CourseId))
		{
			var scored = group.Select(ComputeScores).Where(s => s != null).Select(s => s!).ToList();
			if (scored.Count == 0)
				continue;

			var mean = scored.Average(s => s.TotalEvaluation);
			var variance = (int)Math.Round(scored.Average(s => Math.Pow((double)(s.TotalEvaluation - mean), 2)), 0);
			var stdDev = (int)Math.Round(Math.Sqrt(variance), 0);
			var valid = scored
				.Where(s => s.TotalEvaluation >= mean - stdDev && s.TotalEvaluation <= mean + stdDev)
				.ToList();
			if (valid.Count == 0)
				continue;

			result.Add(new CourseEvalAggregate
			{
				CourseId = group.Key,
				FinalValidTotalEval = Math.Round(valid.Average(s => s.TotalEvaluation), 2),
				ValidTotalTeacherEvalOw = Math.Round(valid.Average(s => s.TeacherEvalOw) / 60m * 100m, 2)
			});
		}

		return result;
	}

	private static EvalScores? ComputeScores(EvaluationRow row)
	{
		var t1 = new[] { row.Q1, row.Q2, row.Q3, row.Q4, row.Q5 };
		var t2 = new[] { row.Q6, row.Q7, row.Q8, row.Q9, row.Q10 };
		if (t1.All(v => v == null) && t2.All(v => v == null))
			return null;

		decimal teacherValue;
		if (t2.All(v => (v ?? 0) == 0))
		{
			teacherValue = t1.Sum(v => v ?? 0);
		}
		else
		{
			teacherValue = 0;
			for (var i = 0; i < 5; i++)
				teacherValue += ((t1[i] ?? 0) + (t2[i] ?? 0)) / 2m;
		}

		var teacherOw = Math.Round((teacherValue * 100m / 25m) * 0.6m, 2);
		var contentSum = (row.Q11 ?? 0) + (row.Q12 ?? 0) + (row.Q13 ?? 0) + (row.Q14 ?? 0);
		var organizerSum = (row.Q15 ?? 0) + (row.Q16 ?? 0) + (row.Q17 ?? 0) + (row.Q18 ?? 0) + (row.Q19 ?? 0);
		var contentOw = Math.Round((contentSum * 100m / 20m) * 0.2m, 2);
		var organizerOw = Math.Round((organizerSum * 100m / 25m) * 0.2m, 3);
		organizerOw = Math.Round(organizerOw, 2);

		return new EvalScores
		{
			TeacherEvalOw = teacherOw,
			TotalEvaluation = teacherOw + contentOw + organizerOw
		};
	}

	private async Task<List<CourseTeacherEvaluationDto>> BuildTeacherEvaluationsAsync(
		List<CourseRow> courses,
		List<CourseEvalAggregate> courseEval,
		CancellationToken cancellationToken)
	{
		if (courseEval.Count == 0)
			return [];

		var evalByCourse = courseEval.ToDictionary(c => c.CourseId);
		var evaluatedCourses = courses.Where(c => evalByCourse.ContainsKey(c.Id)).ToList();
		var teacherIds = evaluatedCourses
			.SelectMany(c => new long?[] { c.TeacherId, c.SecondTeacherId })
			.Where(id => id.HasValue && id.Value > 0)
			.Select(id => id!.Value)
			.Distinct()
			.ToList();

		if (teacherIds.Count == 0)
			return [];

		var names = await db.Set<TeacherBank>().AsNoTracking()
			.Where(t => teacherIds.Contains(t.Id ?? 0))
			.Select(t => new { t.Id, t.FirstName, t.LastName })
			.ToDictionaryAsync(
				t => t.Id ?? 0,
				t => ((t.FirstName ?? "") + " " + (t.LastName ?? "")).Trim(),
				cancellationToken);

		var buckets = new Dictionary<string, List<decimal>>(StringComparer.Ordinal);
		foreach (var course in evaluatedCourses)
		{
			var score = evalByCourse[course.Id].ValidTotalTeacherEvalOw;
			AddTeacherScore(buckets, names, course.TeacherId, score);
			AddTeacherScore(buckets, names, course.SecondTeacherId, score);
		}

		return buckets
			.Select(kv => new CourseTeacherEvaluationDto
			{
				FullName = kv.Key,
				CourseCount = kv.Value.Count,
				TeacherEvalOw = Math.Round(kv.Value.Average(), 2)
			})
			.OrderByDescending(t => t.TeacherEvalOw)
			.ToList();
	}

	private static void AddTeacherScore(
		Dictionary<string, List<decimal>> buckets,
		Dictionary<long, string> names,
		long? teacherId,
		decimal score)
	{
		if (teacherId == null || teacherId <= 0)
			return;
		if (!names.TryGetValue(teacherId.Value, out var name) || string.IsNullOrWhiteSpace(name))
			return;
		if (!buckets.TryGetValue(name, out var list))
		{
			list = [];
			buckets[name] = list;
		}
		list.Add(score);
	}

	private async Task<List<CourseActivityFieldDto>> BuildActivityFieldsAsync(
		List<CourseRow> courses,
		List<long> courseIds,
		CancellationToken cancellationToken)
	{
		var companyIds = courses
			.Where(c => c.CompanyId != null)
			.Select(c => c.CompanyId!.Value)
			.ToHashSet();

		if (courseIds.Count > 0)
		{
			var participantCompanyIds = await db.Set<CourseParticipant>().AsNoTracking()
				.Where(p => courseIds.Contains(p.CourseId) && p.CompanyId != null)
				.Select(p => p.CompanyId!.Value)
				.Distinct()
				.ToListAsync(cancellationToken);
			foreach (var id in participantCompanyIds)
				companyIds.Add(id);
		}

		if (companyIds.Count == 0)
			return [];

		var companies = await db.Set<Company>().AsNoTracking()
			.Where(c => companyIds.Contains(c.Id ?? 0))
			.Select(c => new { c.Id, c.ActivityKind, c.PartyId })
			.ToListAsync(cancellationToken);

		var partyIds = companies.Where(c => c.PartyId != null).Select(c => c.PartyId!.Value).Distinct().ToList();
		var industryByParty = new Dictionary<long, string>();
		if (partyIds.Count > 0)
		{
			var rows = await (
				from customer in db.Set<Customer>().AsNoTracking()
				join industry in db.Set<PartyIndustry>().AsNoTracking()
					on customer.IndustryId equals industry.Id
				where partyIds.Contains(customer.PartyId) && industry.Title != null
				select new { customer.PartyId, industry.Title }
			).ToListAsync(cancellationToken);

			foreach (var row in rows)
			{
				if (!industryByParty.ContainsKey(row.PartyId))
					industryByParty[row.PartyId] = row.Title;
			}
		}

		return companies
			.Select(c =>
			{
				if (!string.IsNullOrWhiteSpace(c.ActivityKind))
					return c.ActivityKind.Trim();
				if (c.PartyId != null && industryByParty.TryGetValue(c.PartyId.Value, out var title)
					&& !string.IsNullOrWhiteSpace(title))
					return title.Trim();
				return null;
			})
			.Where(title => !string.IsNullOrWhiteSpace(title))
			.GroupBy(title => title!, StringComparer.Ordinal)
			.Select(g => new CourseActivityFieldDto
			{
				ActivityField = g.Key,
				Count = g.Count()
			})
			.OrderByDescending(x => x.Count)
			.ThenBy(x => x.ActivityField)
			.ToList();
	}

	private sealed class CourseRow
	{
		public long Id { get; init; }
		public string? TrainingCode { get; init; }
		public CourseExecutingMethodEnum ExecutingMethod { get; init; }
		public int? DurationInMinute { get; init; }
		public decimal? Cost { get; init; }
		public long? CompanyId { get; init; }
		public long TeacherId { get; init; }
		public long? SecondTeacherId { get; init; }
	}

	private sealed class EvaluationRow
	{
		public long CourseId { get; init; }
		public short? Q1 { get; init; }
		public short? Q2 { get; init; }
		public short? Q3 { get; init; }
		public short? Q4 { get; init; }
		public short? Q5 { get; init; }
		public short? Q6 { get; init; }
		public short? Q7 { get; init; }
		public short? Q8 { get; init; }
		public short? Q9 { get; init; }
		public short? Q10 { get; init; }
		public short? Q11 { get; init; }
		public short? Q12 { get; init; }
		public short? Q13 { get; init; }
		public short? Q14 { get; init; }
		public short? Q15 { get; init; }
		public short? Q16 { get; init; }
		public short? Q17 { get; init; }
		public short? Q18 { get; init; }
		public short? Q19 { get; init; }
	}

	private sealed class EvalScores
	{
		public decimal TeacherEvalOw { get; init; }
		public decimal TotalEvaluation { get; init; }
	}

	private sealed class CourseEvalAggregate
	{
		public long CourseId { get; init; }
		public decimal FinalValidTotalEval { get; init; }
		public decimal ValidTotalTeacherEvalOw { get; init; }
	}
}
