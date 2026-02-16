namespace App.BackgroundJob.DTOs.Sup;

public class HamkaranNewSendItemDto
{
    public long PurchaseRequestItemID { get; set; }
    public string? PurchaseRequestNumber { get; set; }
    public long? VchItmID { get; set; }
    public long? SendRefNo { get; set; }
    public string? SendNo { get; set; }
    public string? OrdNo { get; set; }
    public string? SendDate { get; set; }
    public string? PartCode { get; set; }
    public string? PartName { get; set; }
    public string? Comment { get; set; }
    public string? VchDate { get; set; }
    public DateTime? VchDateLatin { get; set; }
    public string? TempVchNum { get; set; }
    public string? FinalVchNum { get; set; }
    public long? TempVchItemId { get; set; }
    public long? FinalVchItemId { get; set; }
    public int? InspctnFlag { get; set; }
    public string? InspectionComment { get; set; }
    public long? RowSendId { get; set; }
    public decimal? ordQty { get; set; }
    public decimal? SendQty { get; set; }
    public decimal? TempVchQty { get; set; }
    public decimal? FinalVchQty { get; set; }
    public DateTime? FactoredDate { get; set; }
    public string? DlTitle { get; set; }
    public string? DlTempTitle { get; set; }
    public decimal? AllFactoredCount { get; set; }
}
