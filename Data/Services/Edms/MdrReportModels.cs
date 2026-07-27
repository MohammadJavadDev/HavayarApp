namespace Data.Services.Edms;

public class MdrReportFilter
{
	public long? ProjectId { get; set; }
	public long? VpisId { get; set; }
	public string? ProjectCode { get; set; }
	public string? DocumentNumber { get; set; }
}

public class MdrReportResponse
{
	public List<MdrReportItemDto> Items { get; set; } = [];
}

public class MdrReportItemDto
{
	public string? ProjectCode { get; set; }
	public string? ProjectName { get; set; }
	public string? BeneficiaryUnit { get; set; }
	public string? DocumentNumber { get; set; }
	public string? DocumentTitle { get; set; }
	public string? Disipline { get; set; }
	public long? MainResponsibleId { get; set; }
	public string? Responsible { get; set; }
	public string? Checker { get; set; }
	public string? Approver { get; set; }
	public decimal OverallProgress { get; set; }
	public decimal? WeightFactor { get; set; }
	public int Progress { get; set; }
	public string? DocClass { get; set; }
	public string? DocType { get; set; }
	public string? ConsumedManHoursInText { get; set; }
	public string? FirstIssueDate { get; set; }
	public string? RePlanDate { get; set; }
	public bool HoldDocuments { get; set; }
	public string? DescriptionForHoldDocuments { get; set; }
	public string? HoldResponsible { get; set; }
	public string? LastRev { get; set; }
	public string? HavayarLastSendDate { get; set; }
	public string? HavayarLastSendTransmittal { get; set; }
	public string? HavayarRdsDate { get; set; }
	public string? HavayarRdsNo { get; set; }
	public string? LastPoi { get; set; }
	public string? ClientLastSentDate { get; set; }
	public string? ClientLastRepliedTransmittal { get; set; }
	public string? ClientLastRepliedRdsDate { get; set; }
	public string? ClientLastRepliedRdsNo { get; set; }
	public string? HavayarRepliedTime { get; set; }
	public string? ClientRepliedTime { get; set; }
	public string? FinalStatus { get; set; }
	public string? InternalStatus { get; set; }
	public bool IsVendorProject { get; set; }
	public List<MdrRevisionDto> Revisions { get; set; } = [];
}

public class MdrRevisionDto
{
	public int Index { get; set; }
	public string? RevNo { get; set; }
	public string? HavayarSendingDate { get; set; }
	public string? HavayarSendingTransmittal { get; set; }
	public string? HavayarRdsDate { get; set; }
	public string? HavayarRdsNo { get; set; }
	public string? Poi { get; set; }
	public string? ClientTime { get; set; }
	public string? ClientReplyDate { get; set; }
	public string? ClientReplyTransmittal { get; set; }
	public string? ClientRdsDate { get; set; }
	public string? ClientRdsNo { get; set; }
	public string? HavayarDelay { get; set; }
	public string? ClientDelay { get; set; }
	public string? Status { get; set; }
}
