using Entities.App.Edms;
using Entities.App.Edms.Enums;

namespace WebApp.ViewModels.Edms;

public sealed class DocumentGroupEditViewModel
{
    public long? ProjectId { get; set; }
    public long? DocumentVpisId { get; set; }

    public Project? Project { get; set; }
    public ProjectVpis? DocumentVpis { get; set; }

    /// <summary>
    /// Currently selected/active revision.
    /// </summary>
    public Document Current { get; set; } = new();

    public List<DocumentRevisionSummaryViewModel> Revisions { get; set; } = new();
}

public sealed class DocumentRevisionSummaryViewModel
{
    public long Id { get; set; }
    public int Revision { get; set; }

    public DocumentStatusEnums? Status { get; set; }
    public DocumentGoalOfProductionEnum? GoalOfProduction { get; set; }

    public string? PublicationShamsiDate { get; set; }
    public string? ModifiedDateShamsiDateTime { get; set; }

    public int CommentCount { get; set; }

    public long? MainFileId { get; set; }
    public long? MotherFileId { get; set; }
    public long? SecondaryFileId { get; set; }
    public long? ReplySheetId { get; set; }
}

/// <summary>
/// Server-computed EditAsGroup toolbar flags (status + VPIS/DCC role).
/// </summary>
public sealed class DocumentEditActionFlags
{
	public bool CanSaveAndClose { get; set; }
	public bool CanAddComment { get; set; }
	public bool CanNewRevision { get; set; }
	public bool CanAddReplySheet { get; set; }
	public bool CanEditStatus { get; set; }
}

