using Entities.App.Edms;
using Entities.App.Edms.Enums;

namespace Data.Services.Sup;

/// <summary>
/// شرط لینک/نمایش مدرک پروژه روی درخواست باز — معادل HTS GetEdmsVpisData.
/// </summary>
public static class OpenOrderRequestVpisRules
{
	public static bool IsHtsApproveComment(DocumentStatusEnums? status)
		=> status is DocumentStatusEnums.ApproveByReviewer or DocumentStatusEnums.ApproveByApprover;

	public static bool IsEligibleForOpenOrderLink(
		DateTime? approvedMiladiDateTime,
		IReadOnlyCollection<DocumentStatusEnums?> commentStatuses,
		string? vpisTitle,
		bool isVendorProject)
	{
		var statuses = commentStatuses ?? Array.Empty<DocumentStatusEnums?>();
		var hasStar = !string.IsNullOrEmpty(vpisTitle) && vpisTitle.Contains('*');

		if (hasStar
			&& approvedMiladiDateTime != null
			&& statuses.Any(IsHtsApproveComment))
			return true;

		if (isVendorProject && statuses.Count > 1)
			return true;

		if (!isVendorProject && statuses.Any(s => s == DocumentStatusEnums.NotReview))
			return true;

		return false;
	}

	public static bool IsEligibleForOpenOrderLink(Document document, string? vpisTitle, bool isVendorProject)
	{
		var statuses = (document.Comments ?? [])
			.Select(c => c.Status)
			.ToList();
		return IsEligibleForOpenOrderLink(document.ApprovedMiladiDateTime, statuses, vpisTitle, isVendorProject);
	}

	public static bool IsEligibleForOpenOrderLink(Document? document, ProjectVpis? vpis, Project? project)
	{
		if (document == null)
			return false;

		var isVendor = project?.ProjectIsVendoriType == true
			|| vpis?.ProjectName?.ProjectIsVendoriType == true;
		return IsEligibleForOpenOrderLink(document, vpis?.Title, isVendor);
	}

	public static IReadOnlyList<Document> SelectLatestEligiblePerVpis(
		IEnumerable<Document> documents,
		Func<long, string?> getVpisTitle,
		Func<long, bool> getIsVendorProject)
	{
		return documents
			.Where(d => d.DocumentVpisId.HasValue)
			.Where(d =>
			{
				var vpisId = d.DocumentVpisId!.Value;
				return IsEligibleForOpenOrderLink(d, getVpisTitle(vpisId), getIsVendorProject(vpisId));
			})
			.GroupBy(d => d.DocumentVpisId)
			.Select(g => g.OrderByDescending(d => d.Id).First())
			.ToList();
	}
}
