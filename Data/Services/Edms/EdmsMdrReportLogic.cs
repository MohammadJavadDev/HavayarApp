using Entities.App.Edms;
using Entities.App.Edms.Enums;
using System.Globalization;

namespace Data.Services.Edms;

internal static class EdmsMdrReportLogic
{
	private static readonly DocumentStatusEnums[] ClientStatusNormal =
	[
		DocumentStatusEnums.ApproveByEmployer,
		DocumentStatusEnums.RejectByEmployer,
		DocumentStatusEnums.CommentedByEmployer,
		DocumentStatusEnums.ApprovedAsNoteByEmployer
	];

	private static readonly DocumentStatusEnums[] ClientStatusVendor =
	[
		DocumentStatusEnums.Issue,
		DocumentStatusEnums.RejectByDcc,
		DocumentStatusEnums.ApproveByApprover,
		DocumentStatusEnums.CommentedByDcc,
		DocumentStatusEnums.ApprovedByDcc,
		DocumentStatusEnums.ReIssued,
		DocumentStatusEnums.IssueForClient,
		DocumentStatusEnums.ApproveByEmployer,
		DocumentStatusEnums.RejectByEmployer,
		DocumentStatusEnums.CommentedByEmployer,
		DocumentStatusEnums.ApprovedAsNoteByEmployer
	];

	private static readonly DocumentStatusEnums[] ValidStatusClient =
	[
		DocumentStatusEnums.ApproveByEmployer,
		DocumentStatusEnums.RejectByEmployer,
		DocumentStatusEnums.CommentedByEmployer,
		DocumentStatusEnums.ApprovedAsNoteByEmployer,
		DocumentStatusEnums.NotReview,
		DocumentStatusEnums.ReIssued
	];

	public static string FormatDate(DateTime? date) =>
		date is null or { Year: <= 1900 } ? null! : date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

	public static string FormatShamsiDate(string? shamsi) => string.IsNullOrWhiteSpace(shamsi) ? null! : shamsi;

	public static string ToHourAndMinute(int? minutes)
	{
		if (minutes is null) return null!;
		var m = minutes.Value;
		return $"{m / 60}:{m % 60:D2}";
	}

	public static string GetUserEnglishName(string? name, string? fullNameEn, string? firstEn, string? lastEn)
	{
		if (!string.IsNullOrWhiteSpace(fullNameEn)) return fullNameEn;
		var combined = $"{firstEn} {lastEn}".Trim();
		return string.IsNullOrWhiteSpace(combined) ? name ?? string.Empty : combined;
	}

	public static string GetDocClassTitle(ProjectVpisClassDocumentEnum? value) => value switch
	{
		ProjectVpisClassDocumentEnum.IFA => "IFA",
		ProjectVpisClassDocumentEnum.IFI => "IFI",
		_ => null!
	};

	public static string GetDocTypeCode(ProjectVpisDocTypeEnum? value) => value switch
	{
		ProjectVpisDocTypeEnum.SCH => "SCH",
		ProjectVpisDocTypeEnum.RPT => "RPT",
		ProjectVpisDocTypeEnum.CHR => "CHR",
		ProjectVpisDocTypeEnum.LST => "LST",
		ProjectVpisDocTypeEnum.Dwg => "DWG",
		ProjectVpisDocTypeEnum.DGM => "DGM",
		ProjectVpisDocTypeEnum.DSN => "DSN",
		ProjectVpisDocTypeEnum.PHL => "PHL",
		ProjectVpisDocTypeEnum.Cal => "CAL",
		ProjectVpisDocTypeEnum.DSH => "DSH",
		ProjectVpisDocTypeEnum.BOM => "BOM",
		ProjectVpisDocTypeEnum.PRC => "PRC",
		ProjectVpisDocTypeEnum.Anl => "ANL",
		ProjectVpisDocTypeEnum.Spc => "SPC",
		ProjectVpisDocTypeEnum.Crt => "CRT",
		ProjectVpisDocTypeEnum.ManName => "MAN",
		_ => null!
	};

	public static string GetDisciplineCode(ProjectVpisDisplayingEnum? value) => value switch
	{
		ProjectVpisDisplayingEnum.GN => "GN",
		_ => value?.ToString()
	};

	public static string GetPoiCode(DocumentGoalOfProductionEnum? value) => value switch
	{
		DocumentGoalOfProductionEnum.IFR => "IFR",
		DocumentGoalOfProductionEnum.IFA => "IFA",
		DocumentGoalOfProductionEnum.IFI => "IFI",
		DocumentGoalOfProductionEnum.AFC => "AFC",
		DocumentGoalOfProductionEnum.AB => "AB",
		_ => null!
	};

	public static string GetHoldResponsibleTitle(DocumentLegalHolderEnum? value) => value switch
	{
		DocumentLegalHolderEnum.Engineering => "مهندسی",
		DocumentLegalHolderEnum.ProjectName => "پروژه",
		DocumentLegalHolderEnum.EmployerName => "کارفرما",
		DocumentLegalHolderEnum.Supplies => "تدارکات",
		DocumentLegalHolderEnum.Sales => "فروش",
		DocumentLegalHolderEnum.Management => "مدیریت",
		DocumentLegalHolderEnum.Production => "تولید",
		DocumentLegalHolderEnum.QualityControl => "کنترل کیفیت",
		DocumentLegalHolderEnum.ForeignTrader => "بازرگانی خارجی",
		DocumentLegalHolderEnum.Services => "خدمات",
		DocumentLegalHolderEnum.Contractor => "پیمانکار",
		_ => null!
	};

	public static string GetStatusTitle(DocumentStatusEnums status) => status switch
	{
		DocumentStatusEnums.NotIssue => "Not Issued",
		DocumentStatusEnums.Issue => "Issue",
		DocumentStatusEnums.RejectByReviewer => "Reject",
		DocumentStatusEnums.ApproveByReviewer => "Approve",
		DocumentStatusEnums.CommentedByReviewer => "Commented",
		DocumentStatusEnums.RejectByApprover => "Reject",
		DocumentStatusEnums.ApproveByApprover => "Approve",
		DocumentStatusEnums.CommentedByApprover => "Commented",
		DocumentStatusEnums.ApprovedByDcc => "Approved By DCC",
		DocumentStatusEnums.RejectByDcc => "Reject By DCC",
		DocumentStatusEnums.CommentedByDcc => "Commented By DCC",
		DocumentStatusEnums.RejectByEmployer => "Reject By Employer",
		DocumentStatusEnums.ApproveByEmployer => "Approve By Employer",
		DocumentStatusEnums.ApprovedAsNoteByEmployer => "Approved As Note By Employer",
		DocumentStatusEnums.CommentedByEmployer => "Commented By Employer",
		DocumentStatusEnums.Hold => "Hold",
		DocumentStatusEnums.UnHold => "UnHold",
		DocumentStatusEnums.NotReview => "Not Review",
		DocumentStatusEnums.ReIssued => "ReIssued",
		DocumentStatusEnums.ReplaySheet => "Replay Sheet",
		DocumentStatusEnums.ReplaySheetFromClient => "Replay Sheet From Client",
		DocumentStatusEnums.IssueForClient => "Issue For Client",
		_ => status.ToString()
	};

	public static int RemoveHolidays(ref DateTime fromDate, ref DateTime toDate, bool isPersian, bool wednesdayIsHoliday = true)
	{
		if (fromDate == DateTime.MinValue)
			fromDate = toDate;

		var dates = new List<DateTime>();
		for (var date = fromDate.Date; date <= toDate.Date; date = date.AddDays(1))
			dates.Add(date);

		if (dates.Count == 0)
			return dates.Count;

		dates = FilterBusinessDays(dates, isPersian, wednesdayIsHoliday).OrderBy(p => p.Date).ToList();
		if (dates.Count == 0)
			return dates.Count;

		fromDate = dates[0];
		if (dates.Count > 1)
			toDate = dates[^1];

		return dates.Count;
	}

	private static List<DateTime> FilterBusinessDays(List<DateTime> dates, bool isPersian, bool wednesdayIsHoliday)
	{
		if (isPersian)
		{
			dates.RemoveAll(p => p.DayOfWeek == DayOfWeek.Thursday);
			if (wednesdayIsHoliday)
				dates.RemoveAll(p => p.DayOfWeek == DayOfWeek.Wednesday);
		}
		else
		{
			dates.RemoveAll(p => p.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday);
		}

		return dates;
	}

	public static string GetHavayarDelay(DateTime fromDate, DateTime toDate, int havayarDelayDay, bool isRevision0, bool isPersian)
	{
		var from = fromDate;
		var to = toDate;
		var daysCount = RemoveHolidays(ref from, ref to, isPersian);
		daysCount = daysCount < 1 ? 1 : daysCount;
		return isRevision0
			? Math.Max(0, daysCount).ToString(CultureInfo.InvariantCulture)
			: Math.Max(0, daysCount - havayarDelayDay).ToString(CultureInfo.InvariantCulture);
	}

	public static string GetClientDelay(DateTime fromDate, DateTime toDate, int clientDelayDay, out int daysCount, bool isPersian)
	{
		var from = fromDate;
		var to = toDate;
		daysCount = RemoveHolidays(ref from, ref to, isPersian);
		daysCount = daysCount < 1 ? 1 : daysCount;
		return Math.Max(0, daysCount - clientDelayDay).ToString(CultureInfo.InvariantCulture);
	}

	public static int GerProgress(
		ProjectVpisClassDocumentEnum? docClass,
		Project project,
		List<DocumentComment> lastComments,
		DateTime issueDate,
		ICollection<DocumentStatusEnums> clientStatus,
		DocumentGoalOfProductionEnum? poiId)
	{
		if (issueDate == DateTime.MinValue)
			return 0;

		var validPoiIds = new[] { DocumentGoalOfProductionEnum.AFC, DocumentGoalOfProductionEnum.AB };
		var reIssuedProgress = project.ProgressPercentage
			.FirstOrDefault(p => p.Status == DocumentStatusEnums.ReIssued);

		if (reIssuedProgress?.Percentage is not null && poiId.HasValue && validPoiIds.Contains(poiId.Value))
			return reIssuedProgress.Percentage.Value;

		switch (docClass)
		{
			case ProjectVpisClassDocumentEnum.IFI:
				return 100;

			case ProjectVpisClassDocumentEnum.IFA:
				var lastComment = lastComments.LastOrDefault();
				if (lastComment?.Status is null)
					break;

				if (lastComment.Status == DocumentStatusEnums.NotReview && lastComments.Count > 1)
				{
					var beforeLastStatus = lastComments[^2].Status;
					if (beforeLastStatus is DocumentStatusEnums.CommentedByEmployer or DocumentStatusEnums.ApprovedAsNoteByEmployer)
					{
						var projectProgress = project.ProgressPercentage
							.FirstOrDefault(p => p.Status == beforeLastStatus);
						if (projectProgress?.Percentage is not null)
							return projectProgress.Percentage.Value;
					}
				}

				var clientLastProgressComment = lastComments.LastOrDefault(p =>
					(p.Status.HasValue && clientStatus.Contains(p.Status.Value)) ||
					p.Status == DocumentStatusEnums.NotReview);

				if (clientLastProgressComment?.Status is not null)
				{
					var projectProgress = project.ProgressPercentage
						.FirstOrDefault(p => p.Status == clientLastProgressComment.Status);
					if (projectProgress?.Percentage is not null)
						return projectProgress.Percentage.Value;
				}
				break;
		}

		return 0;
	}

	public static string GetValidStatus(DocumentComment? documentComment, List<DocumentComment> allDocumentComment)
	{
		if (documentComment?.Status is null)
			return "Not Issued";

		var status = documentComment.Status.Value;

		switch (status)
		{
			case DocumentStatusEnums.RejectByEmployer:
			case DocumentStatusEnums.ApproveByEmployer:
			case DocumentStatusEnums.ApprovedAsNoteByEmployer:
			case DocumentStatusEnums.CommentedByEmployer:
			case DocumentStatusEnums.NotReview:
				return GetStatusTitle(status);

			case DocumentStatusEnums.Hold:
				if (allDocumentComment.Count <= 1)
					return "Not Issued";
				return allDocumentComment.Where(p => p.Status.HasValue && ValidStatusClient.Contains(p.Status.Value))
					.LastOrDefault()?.Status is { } holdStatus
					? GetStatusTitle(holdStatus)
					: "Not Issued";

			case DocumentStatusEnums.UnHold:
				if (allDocumentComment.Count <= 2)
					return "Not Issued";
				return allDocumentComment.Where(p => p.Status.HasValue && ValidStatusClient.Contains(p.Status.Value))
					.LastOrDefault()?.Status is { } unHoldStatus
					? GetStatusTitle(unHoldStatus)
					: "Not Issued";

			case DocumentStatusEnums.ReIssued:
				if (allDocumentComment.Count <= 2)
					return "Re Issued";
				return allDocumentComment.Where(p => p.Status.HasValue && ValidStatusClient.Contains(p.Status.Value))
					.LastOrDefault()?.Status is { } reIssuedStatus
					? GetStatusTitle(reIssuedStatus)
					: "Re Issued";

			default:
				return "Not Issued";
		}
	}

	public static string GetVendorValidStatus(DocumentComment? documentComment, List<DocumentComment> allDocumentComment)
	{
		if (documentComment?.Status is null)
			return "Not Issued";

		var status = documentComment.Status.Value;

		switch (status)
		{
			case DocumentStatusEnums.Issue:
			case DocumentStatusEnums.ApproveByApprover:
			case DocumentStatusEnums.ReplaySheet:
				return "Not Review";

			case DocumentStatusEnums.CommentedByDcc:
				return GetStatusTitle(status);

			case DocumentStatusEnums.ApprovedByDcc:
				if (allDocumentComment.Count > 1)
					return GetStatusTitle(allDocumentComment[^2].Status!.Value);
				return allDocumentComment.Count <= 0
					? "Not Issued"
					: GetStatusTitle(allDocumentComment[^1].Status!.Value);

			case DocumentStatusEnums.Hold:
				if (allDocumentComment.Count > 1)
					return GetStatusTitle(allDocumentComment[^2].Status!.Value);
				return "Not Issued";

			case DocumentStatusEnums.UnHold:
				if (allDocumentComment.Count > 2)
					return GetStatusTitle(allDocumentComment[^3].Status!.Value);
				return "Not Issued";

			case DocumentStatusEnums.IssueForClient:
			case DocumentStatusEnums.RejectByEmployer:
			case DocumentStatusEnums.ApproveByEmployer:
			case DocumentStatusEnums.ApprovedAsNoteByEmployer:
			case DocumentStatusEnums.CommentedByEmployer:
			case DocumentStatusEnums.ReIssued:
				return GetStatusTitle(status);

			default:
				return "Not Valid";
		}
	}

	public static string GetInternalStatus(DocumentComment? documentComment, List<DocumentComment> allDocumentComment)
	{
		if (documentComment?.Status is null)
			return "Not Issued";

		return documentComment.Status.Value switch
		{
			DocumentStatusEnums.Issue => "Not Review",
			DocumentStatusEnums.Hold or DocumentStatusEnums.UnHold or DocumentStatusEnums.CommentedByDcc
				or DocumentStatusEnums.ApprovedAsNoteByEmployer or DocumentStatusEnums.RejectByDcc
				or DocumentStatusEnums.RejectByReviewer or DocumentStatusEnums.RejectByApprover
				=> GetStatusTitle(documentComment.Status.Value),
			DocumentStatusEnums.ApprovedByDcc or DocumentStatusEnums.NotReview => "Issued",
			_ => string.Empty
		};
	}

	public static DocumentStatusEnums[] GetClientStatuses(bool isVendor) =>
		isVendor ? ClientStatusVendor : ClientStatusNormal;

	public static Transmital? FindReplaySheetTransmittal(
		Document document,
		DocumentComment replaySheetComment,
		List<Transmital> transmittals)
	{
		var replayDate = replaySheetComment.CreatedOnMiladiDateTime ?? DateTime.MinValue;
		foreach (var transmittal in transmittals.Where(t => t.DocumentId == document.Id).OrderByDescending(t => t.Id))
		{
			if (transmittal.CreatedOnMiladiDateTime is null)
				continue;
			if (transmittal.CreatedOnMiladiDateTime > replayDate)
				return transmittal;
		}

		return null;
	}
}
