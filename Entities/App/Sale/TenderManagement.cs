using Common.Attributes;
using Entities.App.Gnr;
using Entities.App.Inv;
using Entities.App.Sale.Enums;
using Entities.Auth;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Sale
{
	[Display(Name = "مناقصه خدمات پس از فروش")]
	[Table("TenderManagement", Schema = "Sale")]
	public class TenderManagement : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("شماره درخواست")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int? RequestNumber { get; set; }

		[DisplayName("مناقصه تجدیدی")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		public virtual TenderManagement? Parent { get; set; }
		public long? ParentId { get; set; }

		[DisplayName("واحد سازمانی")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public TenderOrganizationEnum? Organization { get; set; }

		[DisplayName("شرکت هلدینگ")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual Party? Company { get; set; }
		public long? CompanyId { get; set; }

		[DisplayName("نماینده فروش")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public TenderSalesAgentEnum? SalesAgent { get; set; }

		[DisplayName("نام پروژه")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(400)]
		public string? ProjectName { get; set; }

		[DisplayName("نوع استعلام")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public TenderInquiryTypeEnum? InquiryType { get; set; }

		[DisplayName("کارشناس فروش")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual User? SaleExpert { get; set; }
		public long? SaleExpertId { get; set; }

		[DisplayName("بودجه‌ای")]
		[DisplayInfo(null, false, type: SystemType.Boolean)]
		public bool? IsBudget { get; set; }

		[DisplayName("دارای بودجه")]
		[DisplayInfo(null, false, type: SystemType.Boolean)]
		public bool? HasBudget { get; set; }

		[DisplayName("دارای بودجه زیرمجموعه")]
		[DisplayInfo(null, false, type: SystemType.Boolean)]
		public bool? HasBudgetChild { get; set; }

		[DisplayName("وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public TenderStatusEnum? CurrentStatus { get; set; }

		[DisplayName("نتیجه نهایی")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public TenderFinalResultEnum? FinalResult { get; set; }

		[DisplayName("متن علت شکست")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(128)]
		public string? FailureReasonText { get; set; }

		[DisplayName("در کارتابل")]
		[DisplayInfo(null, false, type: SystemType.Boolean)]
		public bool? InCartable { get; set; }

		[DisplayName("تایید اولیه")]
		[DisplayInfo(null, false, type: SystemType.Boolean)]
		public bool? HasPrimitiveConfirm { get; set; }

		[DisplayName("تایید نهایی")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? HasSecondaryConfirm { get; set; }

		[DisplayName("کشور نصب")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual Region? InstallCountry { get; set; }
		public long? InstallCountryId { get; set; }

		[DisplayName("استان نصب")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual Region? InstallProvince { get; set; }
		public long? InstallProvinceId { get; set; }

		[DisplayName("نماینده کارفرما")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? CustomerAgentInfo { get; set; }

		[DisplayName("تلفن نماینده کارفرما")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? CustomerAgentPhone { get; set; }

		[DisplayName("کاربر نهایی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? EndUser { get; set; }

		[DisplayName("شماره قرارداد")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(20)]
		public string? ContractNumber { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(4000)]
		public string? Explain { get; set; }

		[DisplayName("مبلغ پیش‌فاکتور")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? PreInvoicePrice { get; set; }

		[DisplayName("واحد قیمت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public PriceUnitEnum? PriceUnit { get; set; }

		[DisplayName("تاریخ پیش‌فاکتور میلادی")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? PreInvoiceMiladiDate { get; set; }

		[DisplayName("تاریخ پیش‌فاکتور شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		[MaxLength(16)]
		public string? PreInvoiceShamsiDate { get; set; }

		[DisplayName("مبلغ پیش‌فاکتور ۲")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? PreInvoicePrice2 { get; set; }

		[DisplayName("واحد پیش‌فاکتور ۲")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public PriceUnitEnum? PreInvoicePriceUnit2 { get; set; }

		[DisplayName("ارزش پروژه")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ProjectFinancialValueEnum? ProjectValue { get; set; }

		[DisplayName("قیمت ما")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? OurPrice { get; set; }

		[DisplayName("واحد قیمت ما")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public PriceUnitEnum? OurPriceUnit { get; set; }

		[DisplayName("قیمت ثابت")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? FixedPrice { get; set; }

		[DisplayName("قیمت رقبا")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? CompetitivePrice { get; set; }

		[DisplayName("شرکت رقیب")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? CompetitiveCompany { get; set; }

		[DisplayName("قیمت یورو")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? EuroPrice { get; set; }

		[DisplayName("قیمت")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? Price { get; set; }

		[DisplayName("هوایار")]
		[DisplayInfo(null, false, type: SystemType.Boolean)]
		public bool? IsHavayar { get; set; }

		[DisplayName("مبلغ قرارداد")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? ContractPrice { get; set; }

		[DisplayName("تاریخ ارسال به تایید میلادی")]
		[DisplayInfo(null, false, type: SystemType.DateTime)]
		public DateTime? SendForApproveMiladiDateTime { get; set; }

		[DisplayName("تاریخ ارسال به تایید شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateTimeShamsi)]
		[MaxLength(16)]
		public string? SendForApproveShamsiDateTime { get; set; }

		[DisplayName("محاسبه‌شده در آمار فروش")]
		[DisplayInfo(null, false, type: SystemType.Boolean)]
		public bool IsComputedForSalesStatistics { get; set; }

		[DisplayName("واحد قیمت تکنیک کمپرسور")]
		[DisplayInfo(null, false, type: SystemType.Select)]
		public PriceUnitEnum? CompressorTechPriceUnit { get; set; }

		[DisplayName("قیمت تکنیک کمپرسور")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? CompressorTechPrice { get; set; }

		[DisplayName("نرخ تکنیک کمپرسور")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int? CompressorTechPriceRate { get; set; }

		[DisplayName("قیمت تکنیک کمپرسور به ریال")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? CompressorTechPriceInRial { get; set; }

		[DisplayName("شماره مناقصه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? TenderNumber { get; set; }

		[DisplayName("نوع مناقصه")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public TenderTypeEnum? TenderType { get; set; }

		[DisplayName("شماره ضمانت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(128)]
		public string? WarrantyNumber { get; set; }

		[DisplayName("نوع ضمانت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public WarrantyTypeEnum? WarrantyType { get; set; }

		[DisplayName("مبلغ ضمانت")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? WarrantyPrice { get; set; }

		[DisplayName("وضعیت ضمانت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public WarrantyStatusEnum? WarrantyStatus { get; set; }

		[DisplayName("سررسید ضمانت میلادی")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? WarrantyDueMiladiDate { get; set; }

		[DisplayName("سررسید ضمانت شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		[MaxLength(16)]
		public string? WarrantyDueShamsiDate { get; set; }

		[DisplayName("شناسه گزارش پروژه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? SaleProjectReportId { get; set; }

		[DisplayName("شاخص / پیش‌سفارش")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? IndicatorId { get; set; }

		[DisplayName("تاریخ ثبت مناقصه میلادی")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? TenderRegisterMiladiDate { get; set; }

		[DisplayName("تاریخ ثبت مناقصه شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		[MaxLength(16)]
		public string? TenderRegisterShamsiDate { get; set; }

		[DisplayName("مهلت ارسال اسناد میلادی")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? SendingDocumentsDeadlineMiladiDate { get; set; }

		[DisplayName("مهلت ارسال اسناد شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		[MaxLength(16)]
		public string? SendingDocumentsDeadlineShamsiDate { get; set; }

		[DisplayName("تاریخ ارسال اسناد فنی میلادی")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? SendingTechnicalDocumentsMiladiDate { get; set; }

		[DisplayName("تاریخ ارسال اسناد فنی شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		[MaxLength(16)]
		public string? SendingTechnicalDocumentsShamsiDate { get; set; }

		[DisplayName("تاریخ ارسال پیشنهاد فنی میلادی")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? SendingTechnicalProposalMiladiDate { get; set; }

		[DisplayName("تاریخ ارسال پیشنهاد فنی شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		[MaxLength(16)]
		public string? SendingTechnicalProposalShamsiDate { get; set; }

		[DisplayName("تعداد شرکت‌کنندگان")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? NumberOfParticipatingCompanies { get; set; }

		[DisplayName("نام شرکت‌کنندگان")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? ParticipatingTenderCompaniesNames { get; set; }

		[DisplayName("برنده مناقصه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? TenderWinnerName { get; set; }

		[DisplayName("حد نصاب ارزیابی کیفی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(16)]
		public string? QualitativeAssessmentQuorum { get; set; }

		[DisplayName("امتیاز هوایار")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(16)]
		public string? HavayarScore { get; set; }

		[DisplayName("رتبه هوایار")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? HavayarRank { get; set; }

		[DisplayName("تبدیل پیش‌فاکتور به پیشنهاد مالی")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? ChangePreInvoiceToFinancialProposal { get; set; }

		[DisplayName("علت شکست")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public TenderFailureCauseEnum? FailureCause { get; set; }

		[DisplayName("شرح علت شکست")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? FailureCauseDescription { get; set; }

		[DisplayName("آدرس شرکت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(4000)]
		public string? CompanyAddress { get; set; }

		[DisplayName("تلفن شرکت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(200)]
		public string? CompanyPhone { get; set; }

		[DisplayName("ریز صنعت مشتری")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual PartyIndustryItem? CompanyIndustryItem { get; set; }
		public long? CompanyIndustryItemId { get; set; }

		[NotMapped]
		[DisplayName("صنعت مشتری")]
		[DisplayInfo(null, false, type: SystemType.String)]
		public string? CompanyIndustryTitle { get; set; }

		[NotMapped]
		[DisplayName("ریز صنعت مشتری")]
		[DisplayInfo(null, false, type: SystemType.String)]
		public string? CompanyIndustryItemTitle { get; set; }

		[NotMapped]
		public TenderStatusEnum? PreviousCurrentStatus { get; set; }

		[NotMapped]
		public TenderFinalResultEnum? PreviousFinalResult { get; set; }

		[NotMapped]
		public bool? PreviousInCartable { get; set; }

		[NotMapped]
		public bool SkipCartableEmail { get; set; }
	}

	public class TenderManagementConfiguration : IEntityTypeConfiguration<TenderManagement>
	{
		public void Configure(EntityTypeBuilder<TenderManagement> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_TenderManagement_HtsId");
			builder.HasOne(x => x.Parent)
				.WithMany()
				.HasForeignKey(x => x.ParentId)
				.OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.Company)
				.WithMany()
				.HasForeignKey(x => x.CompanyId)
				.OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.InstallCountry)
				.WithMany()
				.HasForeignKey(x => x.InstallCountryId)
				.OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.InstallProvince)
				.WithMany()
				.HasForeignKey(x => x.InstallProvinceId)
				.OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.CompanyIndustryItem)
				.WithMany()
				.HasForeignKey(x => x.CompanyIndustryItemId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}

	[Display(Name = "گروه کالای مناقصه")]
	[Table("TenderProductGroup", Schema = "Sale")]
	public class TenderProductGroup : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(50)]
		public string? Title { get; set; }
	}

	public class TenderProductGroupConfiguration : IEntityTypeConfiguration<TenderProductGroup>
	{
		public void Configure(EntityTypeBuilder<TenderProductGroup> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_TenderProductGroup_HtsId");
		}
	}

	[Display(Name = "قلم مناقصه")]
	[Table("TenderPart", Schema = "Sale")]
	public class TenderPart : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("مناقصه")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual TenderManagement? Tender { get; set; }
		public long? TenderId { get; set; }

		[DisplayName("گروه کالا")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual TenderProductGroup? ProductGroup { get; set; }
		public long? ProductGroupId { get; set; }

		[DisplayName("کالا")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual Part? Part { get; set; }
		public long? PartId { get; set; }

		[DisplayName("تعداد")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? Number { get; set; }

		[DisplayName("کالاهای ناموجود")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(128)]
		public string? NotExistProducts { get; set; }
	}

	public class TenderPartConfiguration : IEntityTypeConfiguration<TenderPart>
	{
		public void Configure(EntityTypeBuilder<TenderPart> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_TenderPart_HtsId");
		}
	}

	[Display(Name = "پیوست مناقصه")]
	[Table("TenderAttachment", Schema = "Sale")]
	public class TenderAttachment : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("مناقصه")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual TenderManagement? Tender { get; set; }
		public long? TenderId { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? Title { get; set; }

		[DisplayName("فایل")]
		[DisplayInfo(null, true, type: SystemType.File)]
		public long? FileId { get; set; }
		public virtual FileEntity? File { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2400)]
		public string? Comment { get; set; }
	}

	public class TenderAttachmentConfiguration : IEntityTypeConfiguration<TenderAttachment>
	{
		public void Configure(EntityTypeBuilder<TenderAttachment> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_TenderAttachment_HtsId");
		}
	}

	[Display(Name = "یادداشت مناقصه")]
	[Table("TenderComment", Schema = "Sale")]
	public class TenderComment : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("مناقصه")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual TenderManagement? Tender { get; set; }
		public long? TenderId { get; set; }

		[DisplayName("متن")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(512)]
		public string? Comment { get; set; }
	}

	public class TenderCommentConfiguration : IEntityTypeConfiguration<TenderComment>
	{
		public void Configure(EntityTypeBuilder<TenderComment> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_TenderComment_HtsId");
		}
	}

	[Display(Name = "تسک مناقصه")]
	[Table("TenderTask", Schema = "Sale")]
	public class TenderTask : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("مناقصه")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual TenderManagement? Tender { get; set; }
		public long? TenderId { get; set; }

		[DisplayName("شرح")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(2056)]
		public string? Comment { get; set; }
	}

	public class TenderTaskConfiguration : IEntityTypeConfiguration<TenderTask>
	{
		public void Configure(EntityTypeBuilder<TenderTask> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_TenderTask_HtsId");
		}
	}
}
