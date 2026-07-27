using Entities.App.Edms;
using Entities.App.Edms.Enums;
using Entities.App.Hrm;
using Entities.Auth;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Data.Services.Edms;

public class EdmsMdrReportService(ApplicationDbContext db) : IEdmsMdrReportService
{
	private sealed class VpisBundle
	{
		public ProjectVpis Vpis { get; init; } = null!;
		public Project Project { get; init; } = null!;
		public OrgUnit? SubjectUnit { get; init; }
		public List<Document> Documents { get; init; } = [];
		public Dictionary<long, List<DocumentComment>> CommentsByDocument { get; init; } = [];
		public Dictionary<long, List<Transmital>> TransmittalsByDocument { get; init; } = [];
	}

	private sealed class UserNameInfo
	{
		public string? Name { get; init; }
		public string? FullNameInEnglish { get; init; }
		public string? FirstNameInEnglish { get; init; }
		public string? LastNameInEnglish { get; init; }
	}

	public async Task<MdrReportResponse> GetReportAsync(MdrReportFilter filter, CancellationToken cancellationToken = default)
	{
		var excludedProjectStatuses = new[] { ProjectStatusEnum.StartNotStarted, ProjectStatusEnum.Completed };

		var vpisQuery =
			from v in db.Set<ProjectVpis>().AsNoTracking()
			join p in db.Set<Project>().AsNoTracking() on v.ProjectNameId equals p.Id
			join ou in db.Set<OrgUnit>().AsNoTracking() on p.SubjectUnitId equals ou.Id into ouJoin
			from ou in ouJoin.DefaultIfEmpty()
			where v.IsActive != IsActiveEnum.Deleted
			      && p.IsActive != IsActiveEnum.Deleted
			      && !excludedProjectStatuses.Contains(p.Status)
			select new { Vpis = v, Project = p, SubjectUnit = ou };

		if (filter.ProjectId.HasValue)
			vpisQuery = vpisQuery.Where(x => x.Project.Id == filter.ProjectId.Value);

		if (filter.VpisId.HasValue)
			vpisQuery = vpisQuery.Where(x => x.Vpis.Id == filter.VpisId.Value);

		if (!string.IsNullOrWhiteSpace(filter.ProjectCode))
			vpisQuery = vpisQuery.Where(x => x.Project.Code == filter.ProjectCode);

		if (!string.IsNullOrWhiteSpace(filter.DocumentNumber))
			vpisQuery = vpisQuery.Where(x => x.Vpis.Code == filter.DocumentNumber);

		var vpisList = await vpisQuery.ToListAsync(cancellationToken);

		if (vpisList.Count == 0)
			return new MdrReportResponse();

		var projectIds = vpisList.Select(v => v.Project.Id!.Value).Distinct().ToList();
		var progressByProject = await db.Set<ProjectProgressPercentage>().AsNoTracking()
			.Where(p => projectIds.Contains(p.ProjectId) && p.IsActive != IsActiveEnum.Deleted)
			.GroupBy(p => p.ProjectId)
			.ToDictionaryAsync(g => g.Key, g => g.ToList(), cancellationToken);

		var vpisIds = vpisList.Select(v => v.Vpis.Id!.Value).ToList();
		var documents = await db.Set<Document>().AsNoTracking()
			.Where(d => d.DocumentVpisId != null && vpisIds.Contains(d.DocumentVpisId.Value) && d.IsActive != IsActiveEnum.Deleted)
			.OrderBy(d => d.Revision).ThenBy(d => d.Id)
			.ToListAsync(cancellationToken);

		var documentIds = documents.Select(d => d.Id!.Value).ToList();
		var comments = documentIds.Count == 0
			? []
			: await db.Set<DocumentComment>().AsNoTracking()
				.Where(c => documentIds.Contains(c.DocumentId) && c.IsActive != IsActiveEnum.Deleted)
				.OrderBy(c => c.Id)
				.ToListAsync(cancellationToken);

		var transmittals = documentIds.Count == 0
			? []
			: await db.Set<Transmital>().AsNoTracking()
				.Where(t => documentIds.Contains(t.DocumentId) && t.IsActive != IsActiveEnum.Deleted)
				.OrderBy(t => t.Id)
				.ToListAsync(cancellationToken);

		var userIds = vpisList
			.SelectMany(v => new long?[] { v.Vpis.ProducerId, v.Vpis.ApproverId })
			.Where(id => id.HasValue)
			.Select(id => id!.Value)
			.Distinct()
			.ToList();

		var users = userIds.Count == 0
			? new Dictionary<long, UserNameInfo>()
			: await db.Set<User>().AsNoTracking()
				.Where(u => userIds.Contains(u.Id!.Value))
				.GroupJoin(
					db.Set<Entities.App.Gnr.Party>().AsNoTracking(),
					u => u.PartyId,
					p => p.Id,
					(u, parties) => new { u, party = parties.FirstOrDefault() })
				.ToDictionaryAsync(
					x => x.u.Id!.Value,
					x => new UserNameInfo
					{
						Name = x.u.Name,
						FullNameInEnglish = x.party?.FullNameInEnglish,
						FirstNameInEnglish = x.party?.FirstNameInEnglish,
						LastNameInEnglish = x.party?.LastNameInEnglish
					},
					cancellationToken);

		var commentsByDocument = comments.GroupBy(c => c.DocumentId).ToDictionary(g => g.Key, g => g.ToList());
		var transmittalsByDocument = transmittals.GroupBy(t => t.DocumentId).ToDictionary(g => g.Key, g => g.ToList());
		var documentsByVpis = documents.GroupBy(d => d.DocumentVpisId!.Value).ToDictionary(g => g.Key, g => g.ToList());

		var bundles = vpisList.Select(item =>
		{
			var vpisId = item.Vpis.Id!.Value;
			documentsByVpis.TryGetValue(vpisId, out var vpisDocuments);
			return new VpisBundle
			{
				Vpis = item.Vpis,
				Project = item.Project,
				SubjectUnit = item.SubjectUnit,
				Documents = vpisDocuments ?? [],
				CommentsByDocument = commentsByDocument,
				TransmittalsByDocument = transmittalsByDocument
			};
		}).ToList();

		var now = DateTime.Now;
		var items = new List<MdrReportItemDto>();

		foreach (var bundle in bundles)
		{
			if (progressByProject.TryGetValue(bundle.Project.Id!.Value, out var progressList))
				bundle.Project.ProgressPercentage = progressList;

			items.Add(BuildMdrItem(bundle, users, now));
		}

		return new MdrReportResponse { Items = items.OrderBy(i => i.ProjectCode).ThenBy(i => i.DocumentNumber).ToList() };
	}

	private static MdrReportItemDto BuildMdrItem(VpisBundle bundle, Dictionary<long, UserNameInfo> users, DateTime now)
	{
		var vpis = bundle.Vpis;
		var project = bundle.Project;
		var isVendor = project.ProjectIsVendoriType;
		var clientStatus = EdmsMdrReportLogic.GetClientStatuses(isVendor);
		var culture = CultureInfo.InvariantCulture;

		var revisions = BuildRevisions(bundle, isVendor, out var lastRevision, out var lastPoi, out var finalStatus, out var lastPoiId);

		var allComments = bundle.Documents
			.SelectMany(d => bundle.CommentsByDocument.GetValueOrDefault(d.Id!.Value) ?? [])
			.OrderBy(c => c.Id)
			.ToList();

		DocumentComment? lastVpisComment = allComments.LastOrDefault();
		var holdThisDocument = lastVpisComment?.Status == DocumentStatusEnums.Hold;

		string? havayarReplayTime = null;
		string? clientReplyTime = null;
		string? havayarLastSendDate = null;
		string? havayarLastSendTransmittal = null;
		string? clientLastSentDateValue = null;
		string? clientLastRepliedTransmittalValue = null;
		string? havayarRdsDate = null;
		string? havayarRdsNo = null;
		string? clientLastRepliedRdsDate = null;
		string? clientLastRepliedRdsNo = null;
		var progress = 0;
		decimal overallProgress = 0;
		var issueDate = DateTime.MinValue;

		if (bundle.Documents.Count > 0)
		{
			Transmital? transmital = null;
			var lastIssuedDocument = isVendor
				? bundle.Documents.LastOrDefault()
				: bundle.Documents.LastOrDefault(d => bundle.TransmittalsByDocument.GetValueOrDefault(d.Id!.Value)?.Count > 0);

			if (lastIssuedDocument is not null)
			{
				if (isVendor)
				{
					issueDate = lastIssuedDocument.CreatedOnMiladiDateTime ?? DateTime.MinValue;
					havayarLastSendDate = EdmsMdrReportLogic.FormatDate(issueDate);
				}
				else
				{
					var transmittalDoc = bundle.TransmittalsByDocument.GetValueOrDefault(lastIssuedDocument.Id!.Value)?.LastOrDefault();
					transmital = transmittalDoc;
					if (transmital?.CreatedOnMiladiDateTime is not null)
					{
						issueDate = transmital.CreatedOnMiladiDateTime.Value;
						havayarLastSendDate = EdmsMdrReportLogic.FormatDate(issueDate);
						havayarLastSendTransmittal = transmital.Number;
					}
				}
			}

			var lastReplaySheetComment = allComments.LastOrDefault(c => c.Status == DocumentStatusEnums.ReplaySheet);
			if (lastReplaySheetComment is not null)
			{
				havayarRdsDate = EdmsMdrReportLogic.FormatDate(lastReplaySheetComment.CreatedOnMiladiDateTime);
				var replayDoc = bundle.Documents.FirstOrDefault(d => d.Id == lastReplaySheetComment.DocumentId);
				if (replayDoc is not null)
				{
					var replayTransmittal = EdmsMdrReportLogic.FindReplaySheetTransmittal(
						replayDoc, lastReplaySheetComment, bundle.TransmittalsByDocument.GetValueOrDefault(replayDoc.Id!.Value) ?? []);
					havayarRdsNo = replayTransmittal?.Number;
				}
			}

			var lastReplaySheetClientComment = allComments.LastOrDefault(c => c.Status == DocumentStatusEnums.ReplaySheetFromClient);
			if (lastReplaySheetClientComment is not null)
			{
				clientLastRepliedRdsDate = EdmsMdrReportLogic.FormatDate(lastReplaySheetClientComment.CreatedOnMiladiDateTime);
				clientLastRepliedRdsNo = lastReplaySheetClientComment.Comment;
			}

			var lastIssuedOrReplayedComments = allComments
				.Where(c => c.Status.HasValue && (clientStatus.Contains(c.Status.Value) || c.Status == DocumentStatusEnums.NotReview))
				.OrderBy(c => c.Id)
				.ToList();

			if (lastIssuedOrReplayedComments.Count > 0)
			{
				var lastComment = lastIssuedOrReplayedComments.Last();
				var lastClientComment = lastIssuedOrReplayedComments.LastOrDefault(c => c.Status != DocumentStatusEnums.NotReview);

				if (lastClientComment is not null && (!isVendor || lastClientComment.Status != DocumentStatusEnums.Issue))
					clientLastSentDateValue = EdmsMdrReportLogic.FormatDate(lastClientComment.CreatedOnMiladiDateTime);

				if (lastComment.Status != DocumentStatusEnums.ApproveByEmployer)
				{
					var specialClientStatus = new[]
					{
						DocumentStatusEnums.RejectByEmployer,
						DocumentStatusEnums.CommentedByEmployer,
						DocumentStatusEnums.ApprovedAsNoteByEmployer
					};

					var lastCommentIsNotReview = lastComment.Status == DocumentStatusEnums.NotReview;
					if (lastCommentIsNotReview || !lastIssuedOrReplayedComments.Any(c => c.Status.HasValue && specialClientStatus.Contains(c.Status.Value)))
					{
						if ((!isVendor && transmital is not null) || (isVendor && issueDate != DateTime.MinValue))
							clientReplyTime = Math.Max(1, (now - issueDate).Days).ToString(culture);
					}
					else if (lastClientComment?.CreatedOnMiladiDateTime is not null)
					{
						havayarReplayTime = Math.Max(1, (now - lastClientComment.CreatedOnMiladiDateTime.Value).Days).ToString(culture);
					}
				}

				progress = EdmsMdrReportLogic.GerProgress(
					vpis.ClassDocument, project, lastIssuedOrReplayedComments, issueDate, clientStatus, lastPoiId);
				overallProgress = Convert.ToDecimal(progress * (vpis.Weight ?? 0)) / 100;
			}
		}

		if (bundle.Documents.Count > 1 && finalStatus == "Not Review")
		{
			var lastRevisionNumber = int.TryParse(lastRevision, out var revNo) ? revNo : -1;
			var lastRevisionDocumentIds = bundle.Documents
				.Where(d => d.Revision == lastRevisionNumber)
				.Select(d => d.Id!.Value)
				.ToHashSet();

			var previousComments = allComments
				.Where(c => !lastRevisionDocumentIds.Contains(c.DocumentId))
				.Where(c => c.Status.HasValue && (clientStatus.Contains(c.Status.Value) || c.Status == DocumentStatusEnums.NotReview))
				.ToList();

			var progressTemp = EdmsMdrReportLogic.GerProgress(
				vpis.ClassDocument, project, previousComments, now, clientStatus, lastPoiId);

			if (progressTemp > progress)
				progress = progressTemp;
		}

		users.TryGetValue(vpis.ProducerId ?? 0, out var checkerUser);
		users.TryGetValue(vpis.ApproverId ?? 0, out var approverUser);

		var firstDocument = bundle.Documents.FirstOrDefault();

		return new MdrReportItemDto
		{
			ProjectCode = project.Code,
			ProjectName = project.Name,
			BeneficiaryUnit = bundle.SubjectUnit?.Title,
			DocumentNumber = vpis.Code,
			DocumentTitle = vpis.Title,
			Disipline = EdmsMdrReportLogic.GetDisciplineCode(vpis.Displaying),
			MainResponsibleId = vpis.ProducerId,
			Responsible = vpis.ReviewerNames,
			Checker = checkerUser is null ? null : EdmsMdrReportLogic.GetUserEnglishName(checkerUser.Name, checkerUser.FullNameInEnglish, checkerUser.FirstNameInEnglish, checkerUser.LastNameInEnglish),
			Approver = approverUser is null ? null : EdmsMdrReportLogic.GetUserEnglishName(approverUser.Name, approverUser.FullNameInEnglish, approverUser.FirstNameInEnglish, approverUser.LastNameInEnglish),
			OverallProgress = overallProgress,
			WeightFactor = vpis.Weight,
			Progress = progress,
			DocClass = EdmsMdrReportLogic.GetDocClassTitle(vpis.ClassDocument),
			DocType = EdmsMdrReportLogic.GetDocTypeCode(vpis.Type),
			ConsumedManHoursInText = firstDocument is null ? null : EdmsMdrReportLogic.ToHourAndMinute(firstDocument.HourPrePerson),
			FirstIssueDate = EdmsMdrReportLogic.FormatDate(vpis.FirstDegreeDocumentMiladiDate),
			RePlanDate = EdmsMdrReportLogic.FormatDate(vpis.ReplaceMiladiDate),
			HoldDocuments = holdThisDocument,
			DescriptionForHoldDocuments = holdThisDocument ? lastVpisComment?.Comment : null,
			HoldResponsible = holdThisDocument ? EdmsMdrReportLogic.GetHoldResponsibleTitle(lastVpisComment?.LegalHolder) : null,
			LastRev = lastRevision,
			HavayarLastSendDate = havayarLastSendDate,
			HavayarLastSendTransmittal = isVendor ? null : havayarLastSendTransmittal,
			HavayarRdsDate = havayarRdsDate,
			HavayarRdsNo = havayarRdsNo,
			LastPoi = lastPoi,
			ClientLastSentDate = clientLastSentDateValue,
			ClientLastRepliedTransmittal = isVendor ? null : clientLastRepliedTransmittalValue,
			ClientLastRepliedRdsDate = clientLastRepliedRdsDate,
			ClientLastRepliedRdsNo = clientLastRepliedRdsNo,
			HavayarRepliedTime = havayarReplayTime,
			ClientRepliedTime = clientReplyTime,
			FinalStatus = finalStatus,
			InternalStatus = EdmsMdrReportLogic.GetInternalStatus(lastVpisComment, allComments),
			IsVendorProject = isVendor,
			Revisions = revisions
		};
	}

	private static List<MdrRevisionDto> BuildRevisions(
		VpisBundle bundle,
		bool isVendor,
		out string? lastRevision,
		out string? lastPoi,
		out string finalStatus,
		out DocumentGoalOfProductionEnum? lastPoiId)
	{
		lastRevision = null;
		lastPoi = null;
		finalStatus = "Not Issued";
		lastPoiId = null;

		var revisions = new List<MdrRevisionDto>();
		var firstIssueBaseline = bundle.Vpis.FirstDegreeDocumentMiladiDate ?? DateTime.MinValue;
		var havayarDelayDay = bundle.Project.LegalDelayHavayar ?? 0;
		var clientDelayDay = bundle.Project.LegalClientDayDelay ?? 0;
		var index = 0;
		string? lastValidStatus = null;
		var allVpisComments = bundle.Documents
			.SelectMany(d => bundle.CommentsByDocument.GetValueOrDefault(d.Id!.Value) ?? [])
			.ToList();

		foreach (var document in bundle.Documents)
		{
			var docComments = bundle.CommentsByDocument.GetValueOrDefault(document.Id!.Value) ?? [];
			var docTransmittals = bundle.TransmittalsByDocument.GetValueOrDefault(document.Id!.Value) ?? [];

			var revisionDto = isVendor
				? BuildVendorRevision(document, docComments, docTransmittals, bundle.Documents, allVpisComments, index, firstIssueBaseline, havayarDelayDay, clientDelayDay)
				: BuildNormalRevision(document, docComments, docTransmittals, bundle.Documents, allVpisComments, index, ref lastValidStatus, firstIssueBaseline, havayarDelayDay, clientDelayDay);

			revisionDto.Index = index;
			revisions.Add(revisionDto);

			lastPoi = revisionDto.Poi;
			lastPoiId = document.GoalOfProduction;
			if (!string.IsNullOrWhiteSpace(revisionDto.Status))
				finalStatus = revisionDto.Status;

			if (!string.IsNullOrWhiteSpace(revisionDto.RevNo))
				lastRevision = revisionDto.RevNo;

			index++;
		}

		return revisions;
	}

	private static DocumentComment? FindPreviousClientComment(
		List<Document> allRevisions,
		Dictionary<long, List<DocumentComment>> commentsByDocument,
		Document currentDocument,
		DocumentComment currentClientComment,
		ICollection<DocumentStatusEnums> clientStatus)
	{
		return allRevisions
			.Where(d => d.Revision <= currentDocument.Revision)
			.SelectMany(d => commentsByDocument.GetValueOrDefault(d.Id!.Value) ?? [])
			.Where(c => c.Status.HasValue && clientStatus.Contains(c.Status.Value))
			.OrderByDescending(c => c.Id)
			.FirstOrDefault(c => c.Id != currentClientComment.Id);
	}

	private static MdrRevisionDto BuildNormalRevision(
		Document document,
		List<DocumentComment> docComments,
		List<Transmital> docTransmittals,
		List<Document> allRevisions,
		List<DocumentComment> allVpisComments,
		int index,
		ref string? lastValidStatus,
		DateTime firstIssueBaseline,
		int havayarDelayDay,
		int clientDelayDay)
	{
		var acceptable = new HashSet<DocumentStatusEnums>
		{
			DocumentStatusEnums.ApproveByEmployer, DocumentStatusEnums.RejectByEmployer,
			DocumentStatusEnums.CommentedByEmployer, DocumentStatusEnums.ApprovedAsNoteByEmployer,
			DocumentStatusEnums.ReIssued, DocumentStatusEnums.NotReview,
			DocumentStatusEnums.ReplaySheet, DocumentStatusEnums.ReplaySheetFromClient,
			DocumentStatusEnums.Hold, DocumentStatusEnums.UnHold
		};

		var clientStatus = EdmsMdrReportLogic.GetClientStatuses(false);
		var commentsByDocument = allRevisions.ToDictionary(
			d => d.Id!.Value,
			d => allVpisComments.Where(c => c.DocumentId == d.Id).ToList());
		var thisRevisionComments = docComments.Where(c => c.Status.HasValue && acceptable.Contains(c.Status.Value)).OrderBy(c => c.Id).ToList();
		var lastValidComments = thisRevisionComments.Where(c => c.Status is not (DocumentStatusEnums.ReplaySheet or DocumentStatusEnums.ReplaySheetFromClient)).ToList();
		var lastComment = lastValidComments.LastOrDefault();
		var lastStatusValue = EdmsMdrReportLogic.GetValidStatus(lastComment, lastValidComments);

		if (lastStatusValue != "Not Issued")
			lastValidStatus = lastStatusValue;

		var firstRevIsNotIssued = index == 0 && lastStatusValue == "Not Issued";
		if (index > 0 && lastStatusValue == "Not Issued")
			lastStatusValue = lastValidStatus ?? "Not Issued";

		var revisionNumber = firstRevIsNotIssued ? null : document.Revision.ToString(CultureInfo.InvariantCulture);
		var poiValue = firstRevIsNotIssued ? null : EdmsMdrReportLogic.GetPoiCode(document.GoalOfProduction);

		var issueDate = DateTime.MinValue;
		Transmital? transmital = null;
		string? issueDateInText = null;
		string? transmittalNo = null;
		string? havayarRdsDateValue = null;
		string? havayarRdsNoValue = null;
		string? clientRdsDateValue = null;
		string? clientRdsNoValue = null;
		string? clientTimeValue = null;
		string? clientReplyDateInText = null;
		string? receivedTransmittalNumber = null;
		string? havayarDelay = null;
		string? clientDelay = null;

		var replaySheetComment = thisRevisionComments.LastOrDefault(c => c.Status == DocumentStatusEnums.ReplaySheet);
		if (replaySheetComment is not null)
		{
			havayarRdsDateValue = EdmsMdrReportLogic.FormatShamsiDate(replaySheetComment.CreatedOnShamsiDateTime)
			                     ?? EdmsMdrReportLogic.FormatDate(replaySheetComment.CreatedOnMiladiDateTime);
			var replayTransmittal = EdmsMdrReportLogic.FindReplaySheetTransmittal(document, replaySheetComment, docTransmittals);
			if (replayTransmittal is not null)
			{
				havayarRdsNoValue = replayTransmittal.Number;
				var issueTransmittal = docTransmittals.LastOrDefault(t => t.Id != replayTransmittal.Id) ?? docTransmittals.LastOrDefault();
				transmital = issueTransmittal;
			}
		}

		transmital ??= docTransmittals.LastOrDefault();

		if (transmital?.CreatedOnMiladiDateTime is not null)
		{
			issueDate = transmital.CreatedOnMiladiDateTime.Value;
			issueDateInText = EdmsMdrReportLogic.FormatDate(issueDate);
			transmittalNo = transmital.Number;
		}

		var clientLastDocumentComments = thisRevisionComments.Where(c => c.Status.HasValue && clientStatus.Contains(c.Status.Value)).ToList();
		if (clientLastDocumentComments.Count > 0)
		{
			var clientLastComment = clientLastDocumentComments.Last();
			var replayClient = thisRevisionComments.LastOrDefault(c => c.Status == DocumentStatusEnums.ReplaySheetFromClient && !string.IsNullOrWhiteSpace(c.Comment));
			clientRdsDateValue = EdmsMdrReportLogic.FormatShamsiDate(replayClient?.CreatedOnShamsiDateTime)
			                     ?? EdmsMdrReportLogic.FormatDate(replayClient?.CreatedOnMiladiDateTime);
			clientRdsNoValue = replayClient?.Comment;

			var received = thisRevisionComments.LastOrDefault(c => c.Status != DocumentStatusEnums.ReplaySheetFromClient && !string.IsNullOrWhiteSpace(c.Comment));
			receivedTransmittalNumber = received?.Comment;
			clientReplyDateInText = EdmsMdrReportLogic.FormatDate(clientLastComment.CreatedOnMiladiDateTime);

			if (transmital is not null && clientLastComment.CreatedOnMiladiDateTime is not null)
			{
				var clientReplyDate = clientLastComment.CreatedOnMiladiDateTime.Value;
				if (document.Revision == 0)
					havayarDelay = EdmsMdrReportLogic.GetHavayarDelay(firstIssueBaseline, issueDate, havayarDelayDay, true, true);
				else
				{
					var clientBeforeLastComment = FindPreviousClientComment(allRevisions, commentsByDocument, document, clientLastComment, clientStatus);
					var fromDate = clientBeforeLastComment?.CreatedOnMiladiDateTime ?? clientReplyDate;
					havayarDelay = EdmsMdrReportLogic.GetHavayarDelay(fromDate, issueDate, havayarDelayDay, false, true);
				}

				clientDelay = EdmsMdrReportLogic.GetClientDelay(issueDate, clientReplyDate, clientDelayDay, out var daysCount, true);
				clientTimeValue = daysCount.ToString(CultureInfo.InvariantCulture);
			}
		}

		return new MdrRevisionDto
		{
			RevNo = revisionNumber,
			HavayarSendingDate = issueDateInText,
			HavayarSendingTransmittal = transmittalNo,
			HavayarRdsDate = havayarRdsDateValue,
			HavayarRdsNo = havayarRdsNoValue,
			Poi = poiValue,
			ClientTime = clientTimeValue,
			ClientReplyDate = clientReplyDateInText,
			ClientReplyTransmittal = receivedTransmittalNumber,
			ClientRdsDate = clientRdsDateValue,
			ClientRdsNo = clientRdsNoValue,
			HavayarDelay = havayarDelay,
			ClientDelay = clientDelay,
			Status = firstRevIsNotIssued ? null : lastStatusValue
		};
	}

	private static MdrRevisionDto BuildVendorRevision(
		Document document,
		List<DocumentComment> docComments,
		List<Transmital> docTransmittals,
		List<Document> allRevisions,
		List<DocumentComment> allVpisComments,
		int index,
		DateTime firstIssueBaseline,
		int havayarDelayDay,
		int clientDelayDay)
	{
		var acceptable = new HashSet<DocumentStatusEnums>
		{
			DocumentStatusEnums.Issue, DocumentStatusEnums.RejectByDcc, DocumentStatusEnums.ApproveByApprover,
			DocumentStatusEnums.CommentedByDcc, DocumentStatusEnums.ApprovedByDcc, DocumentStatusEnums.ReIssued,
			DocumentStatusEnums.IssueForClient, DocumentStatusEnums.ApproveByEmployer, DocumentStatusEnums.RejectByEmployer,
			DocumentStatusEnums.CommentedByEmployer, DocumentStatusEnums.ApprovedAsNoteByEmployer,
			DocumentStatusEnums.ReplaySheet, DocumentStatusEnums.Hold, DocumentStatusEnums.UnHold
		};

		var clientStatus = EdmsMdrReportLogic.GetClientStatuses(true);
		var commentsByDocument = allRevisions.ToDictionary(
			d => d.Id!.Value,
			d => allVpisComments.Where(c => c.DocumentId == d.Id).ToList());

		var thisRevisionComments = docComments.Where(c => c.Status.HasValue && acceptable.Contains(c.Status.Value)).OrderBy(c => c.Id).ToList();
		var lastComment = thisRevisionComments.LastOrDefault();
		var lastStatusValue = EdmsMdrReportLogic.GetVendorValidStatus(lastComment, thisRevisionComments);

		var issueDate = document.CreatedOnMiladiDateTime ?? DateTime.MinValue;
		var issueDateInText = EdmsMdrReportLogic.FormatDate(issueDate);

		string? havayarRdsDateValue = null;
		string? havayarRdsNoValue = null;
		string? clientRdsDateValue = null;
		string? clientRdsNoValue = null;
		string? clientTimeValue = null;
		string? clientReplyDateInText = null;
		string? havayarDelay = null;
		string? clientDelay = null;

		var replaySheetComment = thisRevisionComments.LastOrDefault(c => c.Status == DocumentStatusEnums.ReplaySheet);
		if (replaySheetComment is not null)
		{
			havayarRdsDateValue = EdmsMdrReportLogic.FormatShamsiDate(replaySheetComment.CreatedOnShamsiDateTime)
			                     ?? EdmsMdrReportLogic.FormatDate(replaySheetComment.CreatedOnMiladiDateTime);
			havayarRdsNoValue = EdmsMdrReportLogic.FindReplaySheetTransmittal(document, replaySheetComment, docTransmittals)?.Number;
		}

		var clientLastDocumentComments = thisRevisionComments
			.Where(c => c.Status.HasValue && clientStatus.Contains(c.Status.Value) && c.Status != DocumentStatusEnums.Issue)
			.ToList();

		if (clientLastDocumentComments.Count > 0)
		{
			var clientLastComment = clientLastDocumentComments.Last();
			var replayClient = thisRevisionComments.LastOrDefault(c => c.Status == DocumentStatusEnums.ReplaySheetFromClient && !string.IsNullOrWhiteSpace(c.Comment));
			clientRdsDateValue = EdmsMdrReportLogic.FormatShamsiDate(replayClient?.CreatedOnShamsiDateTime)
			                     ?? EdmsMdrReportLogic.FormatDate(replayClient?.CreatedOnMiladiDateTime);
			clientRdsNoValue = replayClient?.Comment;
			clientReplyDateInText = EdmsMdrReportLogic.FormatDate(clientLastComment.CreatedOnMiladiDateTime);

			if (clientLastComment.CreatedOnMiladiDateTime is not null)
			{
				var clientReplyDate = clientLastComment.CreatedOnMiladiDateTime.Value;
				if (document.Revision == 0)
					havayarDelay = EdmsMdrReportLogic.GetHavayarDelay(firstIssueBaseline, issueDate, havayarDelayDay, true, false);
				else
				{
					var clientBeforeLastComment = FindPreviousClientComment(allRevisions, commentsByDocument, document, clientLastComment, clientStatus);
					var fromDate = clientBeforeLastComment?.CreatedOnMiladiDateTime ?? clientReplyDate;
					havayarDelay = EdmsMdrReportLogic.GetHavayarDelay(fromDate, issueDate, havayarDelayDay, false, false);
				}

				clientDelay = EdmsMdrReportLogic.GetClientDelay(issueDate, clientReplyDate, clientDelayDay, out var daysCount, false);
				clientTimeValue = daysCount.ToString(CultureInfo.InvariantCulture);
			}
		}

		return new MdrRevisionDto
		{
			RevNo = document.Revision.ToString(CultureInfo.InvariantCulture),
			HavayarSendingDate = issueDateInText,
			Poi = EdmsMdrReportLogic.GetPoiCode(document.GoalOfProduction),
			HavayarRdsDate = havayarRdsDateValue,
			HavayarRdsNo = havayarRdsNoValue,
			ClientTime = clientTimeValue,
			ClientReplyDate = clientReplyDateInText,
			ClientRdsDate = clientRdsDateValue,
			ClientRdsNo = clientRdsNoValue,
			HavayarDelay = havayarDelay,
			ClientDelay = clientDelay,
			Status = lastStatusValue
		};
	}
}
