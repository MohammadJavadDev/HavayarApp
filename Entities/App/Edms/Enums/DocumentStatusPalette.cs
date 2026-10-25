namespace Entities.App.Edms.Enums
{
	public static class DocumentStatusPalette
	{
		public const string DarkText = "#111111";
		public const string LightText = "#ffffff";

		public static string? Background(DocumentStatusEnums? status) => status switch
		{
			DocumentStatusEnums.ApprovedAsNoteByClient => "#0FF135",
			DocumentStatusEnums.ApproveByClient => "#077319",
			DocumentStatusEnums.ApproveByReviewer
				or DocumentStatusEnums.ApproveByApprover
				or DocumentStatusEnums.ApprovedByDcc => "#6A935B",
			DocumentStatusEnums.CommentedByReviewer
				or DocumentStatusEnums.CommentedByApprover
				or DocumentStatusEnums.CommentedByDcc
				or DocumentStatusEnums.CommentedByClient
				or DocumentStatusEnums.CommentedBySale
				or DocumentStatusEnums.CommentedInternal => "#6414CE",
			DocumentStatusEnums.ReIssued => "#F573CA",
			DocumentStatusEnums.RejectByReviewer
				or DocumentStatusEnums.RejectByApprover
				or DocumentStatusEnums.RejectByDcc
				or DocumentStatusEnums.RejectByClient
				or DocumentStatusEnums.RejectedBySale => "#E01629",
			DocumentStatusEnums.Issue
				or DocumentStatusEnums.IssueForClient => "#70C8F0",
			_ => null
		};

		public static string Foreground(DocumentStatusEnums? status) => status switch
		{
			DocumentStatusEnums.ApproveByClient
				or DocumentStatusEnums.ApproveByReviewer
				or DocumentStatusEnums.ApproveByApprover
				or DocumentStatusEnums.ApprovedByDcc
				or DocumentStatusEnums.CommentedByReviewer
				or DocumentStatusEnums.CommentedByApprover
				or DocumentStatusEnums.CommentedByDcc
				or DocumentStatusEnums.CommentedByClient
				or DocumentStatusEnums.CommentedBySale
				or DocumentStatusEnums.CommentedInternal
				or DocumentStatusEnums.RejectByReviewer
				or DocumentStatusEnums.RejectByApprover
				or DocumentStatusEnums.RejectByDcc
				or DocumentStatusEnums.RejectByClient
				or DocumentStatusEnums.RejectedBySale => LightText,
			_ => DarkText
		};

		public static string BadgeClass(DocumentStatusEnums? status) =>
			Background(status) == null ? "badge badge-light" : "badge";

		public static string BadgeStyle(DocumentStatusEnums? status)
		{
			var background = Background(status);
			if (background == null)
				return "";

			return $"background-color:{background} !important;color:{Foreground(status)} !important;";
		}
	}
}
