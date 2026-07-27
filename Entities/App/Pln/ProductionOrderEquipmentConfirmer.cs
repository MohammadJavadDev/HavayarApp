using Common.Attributes;
using Entities.App.Sale.Enums;
using Entities.Auth;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Pln
{
	/// <summary>
	/// معادل HTS: Pln_ProductionOrderEquipmentConfirmer - جدول تنظیمات سراسری (نه به‌ازای هر سفارش ساخت)
	/// که برای هر «نوع دستگاه» (DeviceType) مشخص می‌کند کدام کاربر(ها) مسئول تایید مهندسی مکانیک و کدام
	/// کاربر(ها) مسئول تایید مهندسی برق آن نوع تجهیز هستند. دقیقاً مثل HTS، هر DeviceType می‌تواند چند
	/// ردیف/چند تاییدکننده هم‌زمان داشته باشد (رابطه یک‌به‌چند، نه یک‌به‌یک) - مثلاً چند نفر هم‌زمان
	/// تاییدکننده مکانیک یک نوع دستگاه باشند. این جدول برای مسیریابی/اطلاع‌رسانی هدفمند در کارتابل
	/// مهندسی استفاده می‌شود (نگاه کن به ProductionOrderJob.SendEngineeringNotificationAsync و
	/// نمایه POI_MyCartable که با EXISTS روی همین جدول کار می‌کند و ذاتاً از چند ردیف پشتیبانی می‌کند).
	/// نسخه قبلی این Entity به اشتباه به‌صورت یک رکورد تایید به‌ازای هر (سفارش ساخت، قلم) با یک کاربر
	/// تاییدکننده و IsConfirmed مدل شده بود که با مکانیزم واقعی HTS مطابقت نداشت.
	/// </summary>
	[Display(Name = "تاییدکننده تجهیز سفارش ساخت")]
	[Table("ProductionOrderEquipmentConfirmer", Schema = "Pln")]
	public class ProductionOrderEquipmentConfirmer : BaseEntity
	{
		[DisplayName("نوع دستگاه")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public ProductionOrderItemDeviceTypeEnum DeviceType { get; set; }


		[DisplayName("تاییدکننده مهندسی مکانیک")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public User? MechanicalUser { get; set; }

		public long? MechanicalUserId { get; set; }


		[DisplayName("تاییدکننده مهندسی برق")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public User? ElectricalUser { get; set; }

		public long? ElectricalUserId { get; set; }


		[DisplayName("کامنت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Comment { get; set; }

	}
}
