using Common.Attributes;
using Entities.App.Edms;
using Entities.App.Gnr;
using Entities.App.Inv;
using Entities.App.SLS;
using Entities.Auth;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Sale
{
	[Display(Name = "گزارش کار نفت و گاز")]
	[Table("OilGasWorkReport", Schema = "Sale")]
	public class OilGasWorkReport : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("پروژه")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual Project? Project { get; set; }
		public long? ProjectId { get; set; }

		[DisplayName("کارفرما")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1000)]
		public string? Employer { get; set; }

		[DisplayName("محل پروژه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1000)]
		public string? ProjectLocation { get; set; }

		[DisplayName("شماره فرم")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int FormNumber { get; set; }

		[DisplayName("مدیر")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual User? Manager { get; set; }
		public long? ManagerId { get; set; }

		[DisplayName("مدیر پروژه کارفرما")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2000)]
		public string? EmployerProjectManager { get; set; }

		[DisplayName("تلفن مدیر پروژه کارفرما")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(11)]
		public string? EmployerProjectManagerPhoneNumber { get; set; }

		[DisplayName("گزارش روزانه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? DailyReport { get; set; }

		[DisplayName("مرکز هزینه")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual CostCenter? CostCenter { get; set; }
		public long? CostCenterId { get; set; }
	}

	public class OilGasWorkReportConfiguration : IEntityTypeConfiguration<OilGasWorkReport>
	{
		public void Configure(EntityTypeBuilder<OilGasWorkReport> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_OilGasWorkReport_HtsId");
		}
	}

	[Display(Name = "مانیتورینگ تجهیزات")]
	[Table("EquipmentMonitoring", Schema = "Sale")]
	public class EquipmentMonitoring : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("شماره سریال")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(20)]
		public string SerialNumber { get; set; } = "";

		[DisplayName("شناسه سخت‌افزار")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? HardwareId { get; set; }

		[DisplayName("نسخه سخت‌افزار")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(20)]
		public string? HardwareVersion { get; set; }

		[DisplayName("نسخه نرم‌افزار")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(20)]
		public string? SoftwareVersion { get; set; }

		[DisplayName("سریال حواله")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual OrderDetailSerial? OrderDetailSerial { get; set; }
		public long? OrderDetailSerialId { get; set; }

		[DisplayName("مشتری")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual Customer? Customer { get; set; }
		public long? CustomerId { get; set; }

		[DisplayName("کالا")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual Part? Part { get; set; }
		public long? PartId { get; set; }

		[DisplayName("متصل")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsConnected { get; set; }

		[DisplayName("آخرین اتصال میلادی")]
		[DisplayInfo(null, true, type: SystemType.DateTime)]
		public DateTime? LastConnectionMiladiDate { get; set; }

		[DisplayName("مدت اتصال (ثانیه)")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int ConnectionDurationInSecond { get; set; }

		[DisplayName("آی‌پی کلاینت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(40)]
		public string? ClientIpAddress { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? Comment { get; set; }
	}

	public class EquipmentMonitoringConfiguration : IEntityTypeConfiguration<EquipmentMonitoring>
	{
		public void Configure(EntityTypeBuilder<EquipmentMonitoring> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_EquipmentMonitoring_HtsId");
		}
	}

	[Display(Name = "کاربر موبایل خدمات پس از فروش")]
	[Table("AfterSalesMobileUser", Schema = "Sale")]
	public class AfterSalesMobileUser : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("کاربر")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual User? User { get; set; }
		public long? UserId { get; set; }

		[DisplayName("فعال در اپ")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsMobileEnabled { get; set; } = true;
	}

	public class AfterSalesMobileUserConfiguration : IEntityTypeConfiguration<AfterSalesMobileUser>
	{
		public void Configure(EntityTypeBuilder<AfterSalesMobileUser> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_AfterSalesMobileUser_HtsId");
			builder.HasIndex(x => x.UserId).IsUnique().HasFilter("[UserId] IS NOT NULL")
				.HasDatabaseName("IX_Sale_AfterSalesMobileUser_UserId");
		}
	}
}
