using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Entities.Rahkaran.USR3
{
	[Table(name: "Sale_RequirmentAdvertise", Schema = "USR3")]
	public class Sale_RequirmentAdvertise
	{
		[Key]
		public long Sale_RequirmentAdvertiseID { get; set; }

		public int State { get; set; }

		[MaxLength(255)]
		public string? ReferenceNumber { get; set; }

		public decimal? RequestNumber { get; set; }

		public bool? IsBudget { get; set; }

		public long? PartyIdRef { get; set; }

		public long? SalesAgencyIdRef { get; set; }

		public long? PartyIndustryItemIdRef { get; set; }

		[MaxLength(255)]
		public string? Project { get; set; }

		public long? RegionalDivisionIdRef { get; set; }

		public int? InquiryTypeIdRef { get; set; }

		[MaxLength(512)]
		public string? CustomerAgentInfo { get; set; }

		[MaxLength(255)]
		public string? CustomerAgentPhone { get; set; }

		public int? CurrentStatusIdRef { get; set; }

		public int? FinalStatusIdRef { get; set; }

		[MaxLength(512)]
		public string? FailureReason { get; set; }

		public decimal? ProductionNumber { get; set; }

		public decimal? ContractNumber { get; set; }

		public DateTime? ContractDate { get; set; }

		[MaxLength(255)]
		public string? CompetitiveCompany { get; set; }

		[MaxLength(1024)]
		public string? Comments { get; set; }

		public int? Priority { get; set; }

		public decimal? PreInvoiceAmountInRial { get; set; }

		public decimal? PreInvoiceAmountInForeignCurrency { get; set; }

		public long? CurrencyIdRef { get; set; }

		public int? ProjectValueIdRef { get; set; }

		public DateTime? PreInvoiceDate { get; set; }

		[MaxLength(255)]
		public string? EndUser { get; set; }

		public bool? IsMetric { get; set; }

		public long? SalesExpertIdRef { get; set; }

		public long? SaleDepartmentIdRef { get; set; }

		public long Creator { get; set; }

		public DateTime CreationDate { get; set; }

		public long LastModifier { get; set; }

		public DateTime LastModificationDate { get; set; }

		public byte[] Version { get; set; }

		[MaxLength(50)]
		public string? CreatorUsername { get; set; }

		[MaxLength(50)]
		public string? UpdatorUsername { get; set; }

		public long? IntroducerExpertRef { get; set; }

		public long? SalesAdministratorRef { get; set; }

		public long? InstallationCityRef { get; set; }

		public long? InstallationCountryRef { get; set; }

		public int? Sale_RequirementAdvertiseProbabilityOfRealization { get; set; }

	}

	[Table(name: "Sale_RequirmentAdvertiseItem", Schema = "USR3")]
	public class Sale_RequirmentAdvertiseItem
	{
		[Key]
		public long Sale_RequirmentAdvertiseItemID { get; set; }

		public long _MasterRef { get; set; }

		public long? ProductGroupIdRef { get; set; }

		public long? ProductIdRef { get; set; }

		public decimal? Quantity { get; set; }

		public long? ProductIdRefRef { get; set; }

		public long Creator { get; set; }

		public DateTime CreationDate { get; set; }

		public long LastModifier { get; set; }

		public DateTime LastModificationDate { get; set; }

		public byte[]? Version { get; set; }

		[MaxLength(50)]
		public string? Comment { get; set; }

		[MaxLength(50)]
		public string? CreatorUsername { get; set; }

		[MaxLength(50)]
		public string? UpdatorUsername { get; set; }

	}
}
