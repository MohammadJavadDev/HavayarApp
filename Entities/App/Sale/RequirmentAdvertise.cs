using Common.Attributes;
using Entities.App.Gnr;
using Entities.App.Hrm;
using Entities.App.Inv;
using Entities.App.Sale.Enums;
using Entities.App.SLS;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Sale
{
	[Display(Name = "اعلام نیازمندی")]
	[Table("RequirmentAdvertise", Schema = "Sale")]
	public class RequirmentAdvertise : BaseEntity
	{
 

		[DisplayName("شماره درخواست")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? RequestNumber { get; set; }


		[DisplayName("باجت")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool Budget { get; set; } = false;


		[DisplayName("واحد سازمانی")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public OrgUnit? OrganizationUnit { get; set; }

		public long? OrganizationUnitId { get; set; }


		[DisplayName("کارشناس فروش")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Party? SalesExpert { get; set; }

		public long? SalesExpertId { get; set; }


		[DisplayName("سرپرست فروش")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Party? SalesSupervisor { get; set; }

		public long? SalesSupervisorId { get; set; }


		[DisplayName("شخص/شرکت")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Party? PersonOrCompany { get; set; }

		public long? PersonOrCompanyId { get; set; }


		[DisplayName("زیر صنعت")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public PartyIndustry? SubIndustry { get; set; }

		public long? SubIndustryId { get; set; }


		[DisplayName("نمایندگی فروش")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Customer? SalesRepresentative { get; set; }

		public long? SalesRepresentativeId { get; set; }


		[DisplayName("کارشناس معرف")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Party? ReferrerExpert { get; set; }

		public long? ReferrerExpertId { get; set; }


		[DisplayName("نوع استعلام")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public RequirementAdvertiseInquiryTypeEnum? InquiryType { get; set; }


		[DisplayName("نام و سمت نماینده مشتری")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1500)]
		public string? RepresentativeDetails { get; set; }


		[DisplayName("کشور محل نصب")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Region? CountryOfInstallation { get; set; }

		public long? CountryOfInstallationId { get; set; }


		[DisplayName("استان محل نصب")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Region? InstallationProvince { get; set; }

		public long? InstallationProvinceId { get; set; }


		[DisplayName("شهر محل نصب")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Region? InstallationCity { get; set; }

		public long? InstallationCityId { get; set; }


		[DisplayName("شماره تماس نماینده مشتری")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(255)]
		public string? RepresentativeContactNumber { get; set; }


		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? Description { get; set; }


		[DisplayName("مبلغ پیش فاکتور(ریال)")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? PreInvoiceAmountRial { get; set; }


		[DisplayName("مبلغ پیش فاکتور(ارزی)")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? PreInvoiceAmount { get; set; }


		[DisplayName("نوع ارز")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? CurrencyType { get; set; }


		[DisplayName("تاریخ پیش فاکتور")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? PreInvoiceMiladiDate { get; set; }


		[DisplayName("تاریخ پیش فاکتور شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? PreInvoiceShamsiDate { get; set; }


		[DisplayName("اولویت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public RequirementAdvertisePriorityEnum? Priority { get; set; }


		[DisplayName("ارزش مالی پروژه")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public RequirmentAdvertiseProjectFinancialValueEnum? ProjectFinancialValue { get; set; }


		[DisplayName("کاربر نهایی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? EndUser { get; set; }


		[DisplayName("متریک")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool Metric { get; set; } = false;


		[DisplayName("اقلام")]
		[DisplayInfo(null, true, type: SystemType.ListEntity)]
		public List<RequirmentAdvertiseItem> Items { get; set; } = new();

		public long? HamkaranId { get; set; }


	}

	[Display(Name = "اقلام اعلام نیازمندی")]
	[Table("RequirmentAdvertiseItem", Schema = "Sale")]
	public class RequirmentAdvertiseItem : BaseEntity
	{
		public long RequirmentAdvertiseId { get; set; }
		public RequirmentAdvertise RequirmentAdvertise { get; set; }

		[DisplayName("کالا")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Part? Product { get; set; }

		public long? ProductId { get; set; }


		[DisplayName("تعداد")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? Quantity { get; set; }

		public long? HamkaranId { get; set; }


	}


	
}
