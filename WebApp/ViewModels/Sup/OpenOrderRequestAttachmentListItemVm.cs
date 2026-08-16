namespace WebApp.ViewModels.Sup;

public class OpenOrderRequestAttachmentListItemVm
{
	public long Id { get; set; }

	/// <summary>
	/// پیوست معمولی یا مدرک لینک‌شده از VPIS
	/// </summary>
	public bool IsFromVpis { get; set; }

	public string? FileTypeDisplay { get; set; }

	public string? FileName { get; set; }

	public long? DownloadFileId { get; set; }

	public string? SizeDisplay { get; set; }

	public string? Comment { get; set; }

	public string? CreatedByName { get; set; }

	public string? CreatedOnShamsiDateTime { get; set; }

	public DateTime? CreatedOnMiladiDateTime { get; set; }

	public bool CanDelete { get; set; }
}
