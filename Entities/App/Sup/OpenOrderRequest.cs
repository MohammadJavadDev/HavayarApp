using Entities.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApp.Views.Panel.Sup
{
	//public class OpenOrderRequest  
	//{
		 
	//	public long? OrderRowId { get; set; }

	//	public byte Year { get; set; }

	//	public short? OrderNo { get; set; }

	//	[StringLength(10)]
	//	public string OrderDate { get; set; }

	//	[Column(TypeName = "date")]
	//	public DateTime? OrderDateInEurope { get; set; }

	//	public int? Acc_DL_FK { get; set; }

	//	public long Inv_Part_FK { get; set; }

	//	[StringLength(10)]
	//	public string NeedDate { get; set; }

	//	[StringLength(10)]
	//	public string OrderConfirmDate { get; set; }

	//	public decimal? SumSendQty { get; set; }

	//	public decimal FactoredCount { get; set; }

	//	public decimal? OrderQty { get; set; }

	//	public decimal RequiredQty { get; set; }

	//	[StringLength(1600)]
	//	public string OrderItmComment { get; set; }

	//	public bool? IsStop { get; set; }

	//	[StringLength(10)]
	//	public string Stop_Date { get; set; }

	//	public bool IsDeleted { get; set; }

	//	[StringLength(16)]
	//	public string IsDeletedDate { get; set; }

	//	public bool IsForceDeletedByUser { get; set; }

	//	[StringLength(10)]
	//	public string Requested_Personel_RegDate { get; set; }

	//	[StringLength(2400)]
	//	public string Requested_Personel { get; set; }

	//	[StringLength(4000)]
	//	public string Requested_PersonelEmail { get; set; }

	//	[StringLength(160)]
	//	public string OpenOrder_Status { get; set; }

	//	public bool? Changed { get; set; }

	//	public bool? Notify_Email_Requested_Personel { get; set; }

	//	[StringLength(2048)]
	//	public string Notify_Email_Send_Paper_Ids { get; set; }

	//	public bool? Engineering_Accept { get; set; }

	//	public short? Engineering_Accept_User_FK { get; set; }

	//	[StringLength(10)]
	//	public string Engineering_Accept_Date { get; set; }

	//	[Column(TypeName = "date")]
	//	public DateTime? EngineeringAcceptDateInEurope { get; set; }

	//	[StringLength(5)]
	//	public string Engineering_Accept_Time { get; set; }

	//	public bool IsAcceptedAutomaticallyByEngineering { get; set; }

	//	[StringLength(10)]
	//	public string Completion_Date { get; set; }

	//	[StringLength(5)]
	//	public string Completion_Time { get; set; }

	//	public int? DelaysBuyDay { get; set; }

	//	public int? ProductionOrderNumber { get; set; }

	//	public bool IsRejectedByInspection { get; set; }

	//	[Column(TypeName = "date")]
	//	public DateTime? FactoredDate { get; set; }

	//	[StringLength(10)]
	//	public string FactoredDateInText { get; set; }

	//	[StringLength(2024)]
	//	public string Comment { get; set; }

	//	public long PurchaseRequestItemId { get; set; }

	//	public int PurchaseRequestNumber { get; set; }

	//	[Column(TypeName = "date")]
	//	public DateTime? PurchaseRequestDate { get; set; }

	//	[StringLength(10)]
	//	public string PurchaseRequestDateInText { get; set; }

	//	public bool HasSalesUnitConfirmation { get; set; }

	//	public short? SalesUnitConfirmationUserId { get; set; }

	//	public DateTime? SalesUnitConfirmationDate { get; set; }

	//	[StringLength(16)]
	//	public string SalesUnitConfirmationDateInText { get; set; }

	//	[StringLength(2048)]
	//	public string SalesUnitConfirmationComment { get; set; }

	//	public int? ProductionOrderId { get; set; }

	//	public short? SalesUnitSalesExpertId { get; set; }

	//	public short? SalesUnitSalesManagerId { get; set; }

	//	public short? SalesUnitProjectManagerId { get; set; }

	//	public long? RelatedPartId { get; set; }

	//	[Column(TypeName = "date")]
	//	public DateTime? DeliveryVoucherDate { get; set; }

	//	[StringLength(10)]
	//	public string DeliveryVoucherDateInText { get; set; }

	//	[Column(TypeName = "date")]
	//	public DateTime? TemporaryInventoryVoucherDate { get; set; }

	//	[StringLength(10)]
	//	public string TemporaryInventoryVoucherDateInText { get; set; }

	//	[Column(TypeName = "date")]
	//	public DateTime? QcVoucherDate { get; set; }

	//	[StringLength(10)]
	//	public string QcVoucherDateInText { get; set; }

	//	[Column(TypeName = "date")]
	//	public DateTime? FinalInventoryVoucherDate { get; set; }

	//	[StringLength(10)]
	//	public string FinalInventoryVoucherDateInText { get; set; }

	//	[Column(TypeName = "date")]
	//	public DateTime? SupplyDate { get; set; }

	//	[StringLength(10)]
	//	public string SupplyDateInText { get; set; }
	//	public int? ManCompanyId { get; set; }

	//	public bool HasSalesUnitPrimitiveApprove { get; set; }

	//	public bool? IsRoutineRequest { get; set; }

	//	public short? StopStatusId { get; set; }

	//	public short? StopCheckingStatusId { get; set; }

	//	public string Requested_EngineeringPersonelEmail { get; set; }

	//	public string Requested_EngineeringPersonel { get; set; }

	 


	//}
}
