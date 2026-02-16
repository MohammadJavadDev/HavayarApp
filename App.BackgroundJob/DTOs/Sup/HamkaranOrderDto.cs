namespace App.BackgroundJob.DTOs.Sup;

public class HamkaranOrderDto
{
    public int Year { get; set; }
    public long PurchaseRequestItemID { get; set; }
    public long? OrdRowId { get; set; }
    public string? PartCode { get; set; }
    public string? PartName { get; set; }
    public string? PurchaseRequestNumber { get; set; }
    public DateTime? PurchaseRequestDate { get; set; }
    public string? PurchaseRequestDateInText { get; set; }
    public string? OrdItmComment { get; set; }
    public int? Seq { get; set; }
    public string? OrdNo { get; set; }
    public long? PartRef { get; set; }
    public decimal? Ratio { get; set; }
    public decimal? upRatio { get; set; }
    public int? OrderStatus { get; set; }
    public int? OrdItmStatus { get; set; }
    public DateTime? OrdDate { get; set; }
    public DateTime? OrderConfirmDate { get; set; }
    public DateTime? NeedDate { get; set; }
    public string? DlRef { get; set; }
    public string? ProductionOrderNumber { get; set; }
    public long? HtsProductionOrderId { get; set; }
    public long? RelatedPartId { get; set; }
    public decimal? ReqQty { get; set; }
    public decimal? ordQty { get; set; }
    public decimal? FinalSendQty { get; set; }
    public decimal? RetQty { get; set; }
    public decimal? OpenSendQty { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public DateTime? TemporaryVoucherDate { get; set; }
    public DateTime? FinalVoucherDate { get; set; }
    public decimal? AllFactoredCount { get; set; }
    public int? PurchaseRequestStatus { get; set; }
    public int? PurchaseRequestItemStatus { get; set; }
}
