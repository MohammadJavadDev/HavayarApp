using Entities.App.Bpm;
using Entities.App.Bpm.Enums;

namespace WebApp.Controllers.Dynamic
{
	internal static class ProcessDocumentWorkflow
	{
		public static readonly ProcessDocumentStatusEnum[] SupervisorStatusIds =
		[
			ProcessDocumentStatusEnum.Issue,
			ProcessDocumentStatusEnum.Updated,
			ProcessDocumentStatusEnum.BpmUpdated,
			ProcessDocumentStatusEnum.ProcessOwnerCommented,
			ProcessDocumentStatusEnum.ApproverCommented,
			ProcessDocumentStatusEnum.FinalApproverCommented,
			ProcessDocumentStatusEnum.FinalApproverApproved,
			ProcessDocumentStatusEnum.Notified,
			ProcessDocumentStatusEnum.BpmExpertApproved
		];

		public static readonly ProcessDocumentStatusEnum[] CommentRequiredStatuses =
		[
			ProcessDocumentStatusEnum.Reject,
			ProcessDocumentStatusEnum.ProcessOwnerCommented,
			ProcessDocumentStatusEnum.ApproverCommented,
			ProcessDocumentStatusEnum.FinalApproverCommented,
			ProcessDocumentStatusEnum.BpmReject
		];

		public static readonly ProcessDocumentStatusEnum[] CancelStatuses =
		[
			ProcessDocumentStatusEnum.CancelRequest,
			ProcessDocumentStatusEnum.BpmCancelRequest
		];

		public const string AutoOwnerApprovedComment = "بصورت اتوماتیک بعلت تطابق مالک و تصویب کننده";
		public const string DuplicateDocumentNumberMessage = "شماره سند در اسناد دیگر استفاده شده";

		public static List<ProcessDocumentStatusEnum> GetAllowedNextStatuses(
			ProcessDocumentStatusEnum? lastStatus,
			long? currentUserId,
			long? processOwnerId,
			long? approverId,
			long? finalApproverId)
		{
			switch (lastStatus)
			{
				case ProcessDocumentStatusEnum.Issue:
				case ProcessDocumentStatusEnum.Updated:
				case ProcessDocumentStatusEnum.BpmUpdated:
					return [ProcessDocumentStatusEnum.Reject, ProcessDocumentStatusEnum.InProgress];

				case ProcessDocumentStatusEnum.InProgress:
				case ProcessDocumentStatusEnum.BpmAcceptComments:
					return [ProcessDocumentStatusEnum.BpmExpertApproved];

				case ProcessDocumentStatusEnum.BpmExpertApproved:
					return
					[
						ProcessDocumentStatusEnum.BpmReject,
						ProcessDocumentStatusEnum.BpmApproved,
						ProcessDocumentStatusEnum.BpmCancelRequest
					];

				case ProcessDocumentStatusEnum.BpmApproved:
					if (currentUserId != null && currentUserId == finalApproverId)
						return [ProcessDocumentStatusEnum.FinalApproverApproved, ProcessDocumentStatusEnum.FinalApproverCommented];
					if (currentUserId != null && currentUserId == approverId)
						return [ProcessDocumentStatusEnum.ApproverCommented, ProcessDocumentStatusEnum.ApproverApproved];
					return [ProcessDocumentStatusEnum.ProcessOwnerCommented, ProcessDocumentStatusEnum.ProcessOwnerApproved];

				case ProcessDocumentStatusEnum.ProcessOwnerCommented:
				case ProcessDocumentStatusEnum.ApproverCommented:
				case ProcessDocumentStatusEnum.FinalApproverCommented:
					return
					[
						ProcessDocumentStatusEnum.BpmCancelRequest,
						ProcessDocumentStatusEnum.BpmAcceptComments,
						ProcessDocumentStatusEnum.BpmApproved
					];

				case ProcessDocumentStatusEnum.ProcessOwnerApproved:
					return [ProcessDocumentStatusEnum.ApproverCommented, ProcessDocumentStatusEnum.ApproverApproved];

				case ProcessDocumentStatusEnum.ApproverApproved:
					return [ProcessDocumentStatusEnum.FinalApproverApproved, ProcessDocumentStatusEnum.FinalApproverCommented];

				case ProcessDocumentStatusEnum.FinalApproverApproved:
					return [ProcessDocumentStatusEnum.Notified];

				case ProcessDocumentStatusEnum.BpmReject:
					return [ProcessDocumentStatusEnum.BpmExpertApproved];

				default:
					return [];
			}
		}

		public static bool CanOpenTransitionUi(
			ProcessDocument entity,
			bool isSupervisor,
			bool isExpert,
			bool isAdmin,
			long? currentUserId)
		{
			var status = entity.LastStatus;
			var hasType = entity.DocumentType != null;

			return status switch
			{
				ProcessDocumentStatusEnum.Issue or ProcessDocumentStatusEnum.Updated
					=> isSupervisor,
				ProcessDocumentStatusEnum.ProcessOwnerCommented
					or ProcessDocumentStatusEnum.ApproverCommented
					or ProcessDocumentStatusEnum.FinalApproverApproved
					or ProcessDocumentStatusEnum.FinalApproverCommented
					or ProcessDocumentStatusEnum.BpmExpertApproved
					or ProcessDocumentStatusEnum.BpmUpdated
					=> isSupervisor && hasType,
				ProcessDocumentStatusEnum.InProgress
					or ProcessDocumentStatusEnum.BpmReject
					or ProcessDocumentStatusEnum.BpmAcceptComments
					=> isExpert && hasType,
				ProcessDocumentStatusEnum.BpmApproved
					=> isAdmin || (currentUserId != null && currentUserId == entity.ProcessOwnerId),
				ProcessDocumentStatusEnum.ProcessOwnerApproved
					=> isAdmin || (currentUserId != null && currentUserId == entity.ApproverId),
				ProcessDocumentStatusEnum.ApproverApproved
					=> isAdmin || (currentUserId != null && currentUserId == entity.FinalApproverId),
				_ => false
			};
		}

		public static bool MatchesManageCartable(
			ProcessDocument entity,
			bool isAdminOrShowAll,
			bool isSupervisor,
			bool isExpert,
			long? currentUserId)
		{
			if (isAdminOrShowAll)
				return true;

			var status = entity.LastStatus;
			if (isSupervisor)
			{
				if (status != null && SupervisorStatusIds.Contains(status.Value))
					return true;
				if (status == ProcessDocumentStatusEnum.InProgress)
					return true;
				if (status == ProcessDocumentStatusEnum.BpmApproved && entity.ProcessOwnerId == currentUserId)
					return true;
				if (status == ProcessDocumentStatusEnum.ProcessOwnerApproved && entity.ApproverId == currentUserId)
					return true;
				if (status == ProcessDocumentStatusEnum.ApproverApproved && entity.FinalApproverId == currentUserId)
					return true;
				return false;
			}

			if (isExpert)
			{
				if (status is ProcessDocumentStatusEnum.InProgress
					or ProcessDocumentStatusEnum.BpmReject
					or ProcessDocumentStatusEnum.BpmAcceptComments)
					return true;
				return status != null && SupervisorStatusIds.Contains(status.Value);
			}

			return (status == ProcessDocumentStatusEnum.BpmApproved && entity.ProcessOwnerId == currentUserId)
				|| (status == ProcessDocumentStatusEnum.ProcessOwnerApproved && entity.ApproverId == currentUserId)
				|| (status == ProcessDocumentStatusEnum.ApproverApproved && entity.FinalApproverId == currentUserId);
		}

		public static bool CanDownloadTemp(
			ProcessDocument entity,
			bool isSupervisor,
			bool isExpert,
			bool viewAllAttachments,
			long? currentUserId)
		{
			if (entity.TempFileId is null or 0)
				return false;
			if (isSupervisor || isExpert || viewAllAttachments)
				return true;
			return entity.ProcessOwnerId != currentUserId
				&& entity.ApproverId != currentUserId
				&& entity.FinalApproverId != currentUserId;
		}

		public static bool CanDownloadComment(
			ProcessDocument entity,
			bool isSupervisor,
			bool isExpert,
			bool viewAllAttachments)
		{
			if (entity.CommentFileId is null or 0)
				return false;
			return isSupervisor || isExpert || viewAllAttachments;
		}
	}
}
