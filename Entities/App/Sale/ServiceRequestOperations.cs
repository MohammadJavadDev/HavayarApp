using Common.Attributes;
using Common.Utilities;
using Entities.App.Gnr;
using Entities.App.Hcm;
using Entities.App.Inv;
using Entities.App.Sale.Enums;
using Entities.Auth;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Sale
{
	[Display(Name = "ماموریت خدمات پس از فروش")]
	[Table("Mission", Schema = "Sale")]
	public class Mission : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("درخواست پشتیبانی")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual ServiceRequest? ServiceRequest { get; set; }
		public long? ServiceRequestId { get; set; }

		[DisplayName("کارشناس")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual User? Expert { get; set; }
		public long? ExpertId { get; set; }

		[DisplayName("پرسنل")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual Personel? Personel { get; set; }
		public long? PersonelId { get; set; }

		[DisplayName("شناسه پرسنل HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsPersonelId { get; set; }

		[DisplayName("مرکز هزینه")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual CostCenter? CostCenter { get; set; }
		public long? CostCenterId { get; set; }

		[DisplayName("گارانتی")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool HaveGuarantee { get; set; }

		[DisplayName("شماره حکم")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int HokmNumber { get; set; }

		[DisplayName("تاریخ اعزام میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? DispatchMiladiDate { get; set; }

		[DisplayName("تاریخ اعزام شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? DispatchShamsiDate { get; set; }

		[DisplayName("ساعت اعزام")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(5)]
		public string? DispatchTime { get; set; }

		[DisplayName("تاریخ بازگشت میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? ReturnMiladiDate { get; set; }

		[DisplayName("تاریخ بازگشت شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? ReturnShamsiDate { get; set; }

		[DisplayName("ساعت بازگشت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(5)]
		public string? ReturnTime { get; set; }

		[DisplayName("صبحانه")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? BreakfastFee { get; set; }
		[JsonConverter(typeof(EmptyStringToNullByteConverter))]
		public byte? Breakfast { get; set; }

		[DisplayName("ناهار")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? LunchFee { get; set; }
		[JsonConverter(typeof(EmptyStringToNullByteConverter))]
		public byte? Lunch { get; set; }

		[DisplayName("شام")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? DinnerFee { get; set; }
		[JsonConverter(typeof(EmptyStringToNullByteConverter))]
		public byte? Dinner { get; set; }

		[DisplayName("حق شب")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? NightRight { get; set; }
		[JsonConverter(typeof(EmptyStringToNullByteConverter))]
		public byte? NightRightFactor { get; set; }

		[DisplayName("اضافه‌کاری")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? WorkOvertimeFee { get; set; }

		[JsonConverter(typeof(MinutesFromHourMinJsonConverter))]
		public short? WorkOvertimeHour { get; set; }

		[DisplayName("حق ماموریت")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? MissionRight { get; set; }
		[JsonConverter(typeof(EmptyStringToNullByteConverter))]
		public byte? MissionRightFactor { get; set; }

		[DisplayName("حق تعطیل")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? MissionHolidayRight { get; set; }
		[JsonConverter(typeof(EmptyStringToNullByteConverter))]
		public byte? MissionHolidayRightFactor { get; set; }

		[DisplayName("حمل شخصی")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? TransportationPersonal { get; set; }

		[DisplayName("حمل رانندگی")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? TransportationDriving { get; set; }
	}

	public class MissionConfiguration : IEntityTypeConfiguration<Mission>
	{
		public void Configure(EntityTypeBuilder<Mission> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_Mission_HtsId");
		}
	}

	[Display(Name = "حق ماموریت پرسنل")]
	[Table("MissionSalary", Schema = "Sale")]
	public class MissionSalary : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("کارشناس")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual User? Expert { get; set; }
		public long? ExpertId { get; set; }

		[DisplayName("پرسنل")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual Personel? Personel { get; set; }
		public long? PersonelId { get; set; }

		[DisplayName("شناسه پرسنل HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsPersonelId { get; set; }

		[DisplayName("صبحانه")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long BreakfastFee { get; set; }

		[DisplayName("ناهار")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long LunchFee { get; set; }

		[DisplayName("شام")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long DinnerFee { get; set; }

		[DisplayName("حق شب")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long NightRight { get; set; }

		[DisplayName("اضافه‌کاری")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long WorkOvertimeFee { get; set; }

		[DisplayName("حق ماموریت")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long MissionRight { get; set; }

		[DisplayName("حق تعطیل")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long MissionHolidayRight { get; set; }

		[DisplayName("حقوق روزانه")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long DailySalary { get; set; }
	}

	public class MissionSalaryConfiguration : IEntityTypeConfiguration<MissionSalary>
	{
		public void Configure(EntityTypeBuilder<MissionSalary> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_MissionSalary_HtsId");
		}
	}

	[Display(Name = "گزارش کار ماموریت")]
	[Table("WorkReport", Schema = "Sale")]
	public class WorkReport : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("ماموریت")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual Mission? Mission { get; set; }
		public long? MissionId { get; set; }

		[DisplayName("جزئیات درخواست")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual ServiceRequestDetail? ServiceRequestDetail { get; set; }
		public long? ServiceRequestDetailId { get; set; }

		[DisplayName("شماره گزارش")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? ReportNumber { get; set; }

		[DisplayName("شروع سرویس میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date, required: true)]
		public DateTime? ServiceStartMiladiDate { get; set; }

		[DisplayName("شروع سرویس شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? ServiceStartShamsiDate { get; set; }

		[DisplayName("ساعت شروع")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(5)]
		public string? ServiceStartTime { get; set; }

		[DisplayName("پایان سرویس میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date, required: true)]
		public DateTime? ServiceEndMiladiDate { get; set; }

		[DisplayName("پایان سرویس شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? ServiceEndShamsiDate { get; set; }

		[DisplayName("ساعت پایان")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(5)]
		public string? ServiceEndTime { get; set; }

		[DisplayName("شرح درخواست")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(4000)]
		public string? RequestDescription { get; set; }

		[DisplayName("موانع اجرایی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(4000)]
		public string? ExecutiveBarriersDescription { get; set; }

		[DisplayName("نتیجه ماموریت")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public MissionResultEnum MissionResult { get; set; }

		[DisplayName("وضعیت خانه کمپرسور")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool CompressorHouseStatus { get; set; }

		[DisplayName("وضعیت تابلو برق")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool ElectricalPanelStatus { get; set; }

		[DisplayName("وضعیت پایپینگ")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool PipingStatus { get; set; }

		[DisplayName("ساعت کارکرد")]
		[DisplayInfo(null, true, type: SystemType.Long, required: true)]
		public long TotalRunTime { get; set; }

		[DisplayName("ساعت کارکرد زیربار")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? WorkHoursUnderload { get; set; }

		[DisplayName("شدت جریان زیربار")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? FlowIntensityUnderload { get; set; }

		[DisplayName("شدت جریان بی‌بار")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? FlowIntensityWithoutLoad { get; set; }

		[DisplayName("حداکثر فشار کاری")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? MaximumWorkingPressure { get; set; }

		[DisplayName("حداقل فشار کاری")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? MinimumWorkingPressure { get; set; }

		[DisplayName("دمای محیط")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? EnvironmentTemperature { get; set; }

		[DisplayName("دمای دستگاه")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? DeviceTemperature { get; set; }

		[DisplayName("ولتاژ شبکه")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? VoltagePowerGrid { get; set; }

		[DisplayName("دیوپوینت درایر")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? AmountDryerDewpoint { get; set; }

		[DisplayName("ساعت کارکرد قبلی")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? OldRunTime { get; set; }

		[DisplayName("تاریخ گارانتی کارشناس میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? ExpertGuaranteeMiladiDate { get; set; }

		[DisplayName("تاریخ گارانتی کارشناس شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		[MaxLength(10)]
		public string? ExpertGuaranteeShamsiDate { get; set; }

		[DisplayName("توضیح گارانتی کارشناس")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(4000)]
		public string? ExpertGuaranteeComment { get; set; }

		[DisplayName("شرح خرابی")]
		[DisplayInfo(null, true, type: SystemType.ListLong)]
		[MaxLength(512)]
		public string? FailureTypeIds { get; set; }

		[DisplayName("شرح خرابی متن")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(4000)]
		public string? FailureTypeInText { get; set; }

		[DisplayName("کد عملیاتی")]
		[DisplayInfo(null, true, type: SystemType.ListLong)]
		[MaxLength(512)]
		public string? RepairsTypeIds { get; set; }

		[DisplayName("کد عملیاتی متن")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(4000)]
		public string? RepairsTypeInText { get; set; }

		[DisplayName("لینک رضایت‌سنجی ارسال شده")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? IsSurveyLinkSended { get; set; }
	}

	[Display(Name = "نوع خرابی گزارش کار")]
	[Table("AfterSalesFailureType", Schema = "Sale")]
	public class AfterSalesFailureType : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(256)]
		public string Title { get; set; } = "";

		[DisplayName("کد")]
		[DisplayInfo(null, true, type: SystemType.String, showInRelationData: true)]
		[MaxLength(64)]
		public string? Code { get; set; }
	}

	[Display(Name = "کد عملیاتی گزارش کار")]
	[Table("AfterSalesRepairsType", Schema = "Sale")]
	public class AfterSalesRepairsType : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(256)]
		public string Title { get; set; } = "";

		[DisplayName("کد")]
		[DisplayInfo(null, true, type: SystemType.String, showInRelationData: true)]
		[MaxLength(64)]
		public string? Code { get; set; }
	}

	public class WorkReportConfiguration : IEntityTypeConfiguration<WorkReport>
	{
		public void Configure(EntityTypeBuilder<WorkReport> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_WorkReport_HtsId");
		}
	}

	public class AfterSalesFailureTypeConfiguration : IEntityTypeConfiguration<AfterSalesFailureType>
	{
		public void Configure(EntityTypeBuilder<AfterSalesFailureType> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_AfterSalesFailureType_HtsId");
		}
	}

	public class AfterSalesRepairsTypeConfiguration : IEntityTypeConfiguration<AfterSalesRepairsType>
	{
		public void Configure(EntityTypeBuilder<AfterSalesRepairsType> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_AfterSalesRepairsType_HtsId");
		}
	}

	public class WorkReportAttachmentConfiguration : IEntityTypeConfiguration<WorkReportAttachment>
	{
		public void Configure(EntityTypeBuilder<WorkReportAttachment> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_WorkReportAttachment_HtsId");
		}
	}

	[Display(Name = "پیوست گزارش کار")]
	[Table("WorkReportAttachment", Schema = "Sale")]
	public class WorkReportAttachment : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		public long? WorkReportId { get; set; }
		public virtual WorkReport? WorkReport { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? Title { get; set; }

		[DisplayName("فایل")]
		[DisplayInfo(null, true, type: SystemType.File)]
		public long? FileId { get; set; }
		public virtual FileEntity? File { get; set; }
	}

	[Display(Name = "قطعه درخواست پشتیبانی")]
	[Table("ServiceRequestPart", Schema = "Sale")]
	public class ServiceRequestPart : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("جزئیات درخواست")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual ServiceRequestDetail? ServiceRequestDetail { get; set; }
		public long? ServiceRequestDetailId { get; set; }

		[DisplayName("قلم حواله")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual OrderDetail? OrderDetail { get; set; }
		public long? OrderDetailId { get; set; }

		[DisplayName("قطعه جایگزین")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual Part? ReplacePart { get; set; }
		public long? ReplacePartId { get; set; }

		[DisplayName("نوع خدمت")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public AfterSalesServiceTypeEnum ServiceType { get; set; }

		[DisplayName("مقدار")]
		[DisplayInfo(null, true, type: SystemType.Decimal, required: true)]
		public decimal Mount { get; set; }

		[DisplayName("قیمت واحد")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? UnitPrice { get; set; }

		[DisplayName("ثبت دستی")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsRegisteredManually { get; set; }

		[DisplayName("شناسه قلم سند انبار HTS")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int? HtsProjectVchItemId { get; set; }

		[DisplayName("شناسه کالای سند انبار HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? HtsProjectVchPartId { get; set; }

		[DisplayName("شماره سند انبار")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? ProjectVchNum { get; set; }

		[DisplayName("سال سند انبار")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? ProjectVchYear { get; set; }

		[DisplayName("تاریخ سند شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		[MaxLength(10)]
		public string? ProjectVchShamsiDate { get; set; }

		[DisplayName("تاریخ سند میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? ProjectVchMiladiDate { get; set; }

		[DisplayName("واحد کالا")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		public virtual PartUnit? PartUnit { get; set; }
		public long? PartUnitId { get; set; }

		[DisplayName("مرکز هزینه")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual CostCenter? CostCenter { get; set; }
		public long? CostCenterId { get; set; }

		[DisplayName("سایر مراکز هزینه")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(OtherCostCenterPartyId))]
		public virtual Party? OtherCostCenterParty { get; set; }
		public long? OtherCostCenterPartyId { get; set; }

		[DisplayName("مجوز برگشت دارد")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? ReturnLicence { get; set; }

		[DisplayName("داغی دارد")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? ReturnDamagedPart { get; set; }

		[DisplayName("نوع داغی")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public DamagedPartTypeEnum? DamagedPartType { get; set; }

		[DisplayName("شناسه قطعات داغی")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(2048)]
		public string? DamagedPartIds { get; set; }

		[DisplayName("قطعات داغی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? DamagedParts { get; set; }

		[DisplayName("توضیح قطعه داغی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? DamagedPartDescription { get; set; }

		[DisplayName("خرابی دارد")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? HasFailure { get; set; }

		[DisplayName("شناسه قطعات معیوب")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(2048)]
		public string? FailurePartIds { get; set; }

		[DisplayName("قطعات معیوب")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? FailureParts { get; set; }

		[DisplayName("شرح خرابی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? FailureDescription { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(4000)]
		public string? Comment { get; set; }
	}

	public class ServiceRequestPartConfiguration : IEntityTypeConfiguration<ServiceRequestPart>
	{
		public void Configure(EntityTypeBuilder<ServiceRequestPart> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_ServiceRequestPart_HtsId");
		}
	}
}
