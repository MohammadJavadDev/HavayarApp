using Data.Contracts.Actions;
using Entities.App.Sale;
using Entities.App.Sale.Enums;

namespace WebApp.Actions.Sale
{
	public class ServiceRequestPartAction
	{
		[EntityAction(typeof(ServiceRequestPart), EntityActionTrigger.BeforeSave,
			"ServiceRequestPartValidate", "اعتبارسنجی درخواست کالا مطابق HTS", Priority = 1)]
		public Task BeforeSave(ServiceRequestPart entity, CancellationToken ct)
		{
			if (entity.ServiceRequestDetailId is null or 0)
				throw new InvalidOperationException("محصول مرتبط را از محصولات همین درخواست پشتیبانی انتخاب کنید");
			if (entity.ServiceType == 0)
				throw new InvalidOperationException("نوع خدمت نمی‌تواند خالی باشد");
			if (entity.Mount <= 0)
				throw new InvalidOperationException("مقدار نمی‌تواند خالی باشد");
			if (entity.ReturnDamagedPart == true && entity.DamagedPartType == null)
				throw new InvalidOperationException("نوع داغی نمی‌تواند خالی باشد");
			if (entity.HasFailure == true)
			{
				if (string.IsNullOrWhiteSpace(entity.FailurePartIds))
					throw new InvalidOperationException("انتخاب قطعه معیوب اجباری می‌باشد");
				if (string.IsNullOrWhiteSpace(entity.FailureDescription))
					throw new InvalidOperationException("توضیحات قطعه معیوب اجباری می‌باشد");
			}
			if (entity.ServiceType == AfterSalesServiceTypeEnum.Warranty && entity.UnitPrice == null)
				entity.UnitPrice = 0;
			return Task.CompletedTask;
		}
	}
}
