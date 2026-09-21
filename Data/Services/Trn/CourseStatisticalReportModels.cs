namespace Data.Services.Trn;

public class CourseStatisticalReportFilter
{
	public string? FromDateShamsi { get; set; }
	public string? ToDateShamsi { get; set; }
}

public class CourseStatisticalReportResponse
{
	public CourseStatisticalTotals Totals { get; set; } = new();
	public List<CourseActivityFieldDto> ActivityFields { get; set; } = [];
	public List<CourseTeacherEvaluationDto> TeacherEvaluations { get; set; } = [];
}

public class CourseStatisticalTotals
{
	public int AllCourseCount { get; set; }
	public decimal? FinalEvalAverage { get; set; }

	public int AllCourseCountCng { get; set; }
	public string AllCourseCountCngPercentage { get; set; } = "0%";
	public int AllCourseCountHy { get; set; }
	public string AllCourseCountHyPercentage { get; set; } = "0%";

	public int AllPublicCourseCount { get; set; }
	public string AllPublicCourseCountPercentage { get; set; } = "0%";
	public int AllPrivateCourseCount { get; set; }
	public string AllPrivateCourseCountPercentage { get; set; } = "0%";

	public int AllCourseHour { get; set; }
	public int AllCourseHourCng { get; set; }
	public string AllCourseHourCngPercentage { get; set; } = "0%";
	public int AllCourseHourHy { get; set; }
	public string AllCourseHourHyPercentage { get; set; } = "0%";

	public int AllPublicCourseHour { get; set; }
	public string AllPublicCourseHourPercentage { get; set; } = "0%";
	public int AllPrivateCourseHour { get; set; }
	public string AllPrivateCourseHourPercentage { get; set; } = "0%";

	public int AllHavayariTeacherCount { get; set; }
	public int AllNotHavayariTeacherCount { get; set; }
	public decimal? CourseCost { get; set; }
}

public class CourseActivityFieldDto
{
	public string ActivityField { get; set; } = string.Empty;
	public int Count { get; set; }
}

public class CourseTeacherEvaluationDto
{
	public string FullName { get; set; } = string.Empty;
	public int CourseCount { get; set; }
	public decimal TeacherEvalOw { get; set; }
}
