using Entities.App.Sale;

namespace WebApp.Models.Sale
{
	public class ProductionOrderItemHistoryViewModel
	{
		public long ProductionOrderItemId { get; set; }

		/// <summary>وضعیت سفارش ساخت — IsForProductionMode=false, IsForProductionStepStatus=false</summary>
		public List<ProductionOrderItemComment> OrderStatusComments { get; set; } = new();

		/// <summary>وضعیت استعلام</summary>
		public List<ProductionOrderItemInquiry> InquiryHistory { get; set; } = new();

		/// <summary>وضعیت تولید — IsForProductionMode=true, IsForProductionStepStatus=false</summary>
		public List<ProductionOrderItemComment> ProductionStatusComments { get; set; } = new();

		/// <summary>مرحله ساخت — IsForProductionMode=false, IsForProductionStepStatus=true</summary>
		public List<ProductionOrderItemComment> ProductionStepComments { get; set; } = new();
	}
}
