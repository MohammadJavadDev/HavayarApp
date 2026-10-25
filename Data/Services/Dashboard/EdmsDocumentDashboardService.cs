using Common.Utilities;
using Data;
using Entities.App.Edms;
using Entities.App.Edms.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Data.Services.Dashboard;

/// <summary>
/// داشبورد وضعیت مدارک و پروژه‌های EDMS — فقط ماندهٔ همین لحظه.
/// بدون فیلتر تاریخ. بدون نمودار تاخیر (Document تاریخ سررسید ندارد).
/// </summary>
public class EdmsDocumentDashboardService(ApplicationDbContext db) : IEdmsDocumentDashboardService
{
	private const int TopDocumentStatusesForChart = 10;
	private const string UnknownLabel = "نامشخص";
	private const string OtherLabel = "سایر";
	private const string HoldCardLabel = "معلق";

	private static readonly DocumentStatusEnums[] HoldStatuses =
	[
		DocumentStatusEnums.Hold,
		DocumentStatusEnums.HoldByProject
	];

	public async Task<EdmsDocumentDashboardResponse> GetDashboardAsync(
		CancellationToken cancellationToken = default)
	{
		var now = DateTime.Now;

		var documentGroups = await db.Set<Document>().AsNoTracking()
			.GroupBy(d => d.Status)
			.Select(g => new { Status = g.Key, Count = g.Count() })
			.ToListAsync(cancellationToken);

		var projectGroups = await db.Set<Project>().AsNoTracking()
			.GroupBy(p => p.Status)
			.Select(g => new { Status = g.Key, Count = g.Count() })
			.ToListAsync(cancellationToken);

		var documentSlicesAll = documentGroups
			.Select(g => new EdmsStatusSliceDto
			{
				Key = g.Status.HasValue ? ((int)g.Status.Value).ToString() : "null",
				Label = g.Status.HasValue
					? ToPersianDocumentStatusLabel(g.Status.Value)
					: UnknownLabel,
				Count = g.Count
			})
			.OrderByDescending(s => s.Count)
			.ThenBy(s => s.Label, StringComparer.Ordinal)
			.ToList();

		var totalDocuments = documentSlicesAll.Sum(s => s.Count);
		var holdDocuments = documentGroups
			.Where(g => g.Status.HasValue && HoldStatuses.Contains(g.Status.Value))
			.Sum(g => g.Count);

		var projectSlices = projectGroups
			.Select(g => new EdmsStatusSliceDto
			{
				Key = ((int)g.Status).ToString(),
				Label = g.Status.ToDisplay(),
				Count = g.Count
			})
			.OrderByDescending(s => s.Count)
			.ThenBy(s => s.Label, StringComparer.Ordinal)
			.ToList();

		var totalProjects = projectSlices.Sum(s => s.Count);
		var inProcessProjects = projectGroups
			.Where(g => g.Status == ProjectStatusEnum.InProcess)
			.Sum(g => g.Count);

		var filterDescription =
			"همه مدارک و پروژه‌های مهندسی بدون فیلتر تاریخ؛ " +
			"وضعیت مدرک و وضعیت پروژه از فیلد وضعیت همان موجودیت؛ " +
			"معلق شامل وضعیت معلق و معلق توسط پروژه است.";

		var response = new EdmsDocumentDashboardResponse
		{
			AsOfShamsiDateTime = now.ToShamsiDateTime(),
			MetricKindLabel = "وضعیت همین لحظه",
			FilterDescription = filterDescription,
			TotalDocuments = totalDocuments,
			TotalProjects = totalProjects,
			HoldDocuments = holdDocuments,
			InProcessProjects = inProcessProjects,
			DocumentStatusSlices = BuildTopPlusOther(documentSlicesAll, TopDocumentStatusesForChart),
			ProjectStatusSlices = projectSlices,
			Cards =
			[
				new EdmsDashboardCardDto
				{
					Key = "totalDocuments",
					Title = "کل مدارک",
					Value = totalDocuments,
					Definition = "تعداد کل مدارک در همین لحظه، بدون فیلتر تاریخ.",
					MetricKindLabel = "وضعیت همین لحظه"
				},
				new EdmsDashboardCardDto
				{
					Key = "holdDocuments",
					Title = HoldCardLabel,
					Value = holdDocuments,
					Definition = "تعداد مدارک با وضعیت معلق یا معلق توسط پروژه؛ این عدد تاخیر نیست.",
					MetricKindLabel = "وضعیت همین لحظه"
				},
				new EdmsDashboardCardDto
				{
					Key = "totalProjects",
					Title = "کل پروژه‌ها",
					Value = totalProjects,
					Definition = "تعداد کل پروژه‌های مهندسی در همین لحظه، بدون فیلتر تاریخ.",
					MetricKindLabel = "وضعیت همین لحظه"
				},
				new EdmsDashboardCardDto
				{
					Key = "inProcessProjects",
					Title = "پروژه‌های در جریان",
					Value = inProcessProjects,
					Definition = "تعداد پروژه‌هایی که وضعیت آن‌ها در جریان است.",
					MetricKindLabel = "وضعیت همین لحظه"
				}
			]
		};

		return response;
	}

	/// <summary>
	/// از Displayهای درهم مثل «NotIssue صادر نشده» فقط بخش فارسی بعد از توکن لاتین را برمی‌گرداند.
	/// </summary>
	internal static string ToPersianDocumentStatusLabel(DocumentStatusEnums status)
	{
		var display = status.ToDisplay();
		if (string.IsNullOrWhiteSpace(display))
			return UnknownLabel;

		var trimmed = display.Trim();
		var enumName = status.ToString();

		if (trimmed.StartsWith(enumName, StringComparison.OrdinalIgnoreCase))
		{
			var afterEnum = trimmed[enumName.Length..].Trim();
			if (!string.IsNullOrEmpty(afterEnum) && !IsLatinOnlyToken(afterEnum))
				return afterEnum;
		}

		var match = Regex.Match(trimmed, @"^[A-Za-z][A-Za-z0-9]*\s+(.+)$");
		if (match.Success)
		{
			var persianPart = match.Groups[1].Value.Trim();
			if (!string.IsNullOrEmpty(persianPart) && !IsLatinOnlyToken(persianPart))
				return persianPart;
		}

		if (IsLatinOnlyToken(trimmed))
			return UnknownLabel;

		return trimmed;
	}

	private static bool IsLatinOnlyToken(string value) =>
		Regex.IsMatch(value.Trim(), @"^[A-Za-z0-9_\s]+$");

	private static List<EdmsStatusSliceDto> BuildTopPlusOther(
		List<EdmsStatusSliceDto> allOrderedByCountDesc,
		int topN)
	{
		if (allOrderedByCountDesc.Count <= topN)
			return allOrderedByCountDesc.ToList();

		var top = allOrderedByCountDesc.Take(topN).ToList();
		var otherCount = allOrderedByCountDesc.Skip(topN).Sum(s => s.Count);
		if (otherCount > 0)
		{
			top.Add(new EdmsStatusSliceDto
			{
				Key = "other",
				Label = OtherLabel,
				Count = otherCount
			});
		}

		return top;
	}
}
