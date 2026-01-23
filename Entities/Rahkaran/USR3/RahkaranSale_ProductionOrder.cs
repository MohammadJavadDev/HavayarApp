using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Entities.Rahkaran.USR3
{
	[Table(name: "Sale_ProductionOrder", Schema = "USR3")]
	public class RahkaranSale_ProductionOrder
	{
		[Key]
		public long Sale_ProductionOrderID { get; set; }

		public int State { get; set; }

		public long? ContractIdRef { get; set; }

		public long? ProjectDlIdRef { get; set; }

		[MaxLength(50)]
		public string? EndUser { get; set; }

		public DateTime? ProjectStartDate { get; set; }

		public DateTime? MetreDate { get; set; }

		[MaxLength(50)]
		public string?MetreNumber { get; set; }

		public DateTime? AgreedDeliveryDate { get; set; }

		public long? SalesManagerIdRef { get; set; }

		public long? ProjectManagerIdRef { get; set; }

		public long? AlternativeExpertIdRef { get; set; }

		public long? SalesAgencyIdRef { get; set; }

		public long? InstallationCityIdRef { get; set; }

		public bool? IsNeedInspectionBeforePacking { get; set; }

		public bool? HasContractor { get; set; }

		public bool? CompressorHouseIsReady { get; set; }

		public bool? InstallationIsByHy { get; set; }

		public bool? HasFinancialGuarantee { get; set; }

		[MaxLength(2048)]
		public string? SalesComment { get; set; }

		[MaxLength(2048)]
		public string? Comment { get; set; }

		public bool? CentrifugeHasCompressor { get; set; }

		public decimal? CentrifugeCompressorCount { get; set; }

		public decimal? CentrifugeElectromotorVoltage { get; set; }

		public long? ManagementConfirmationAttachment { get; set; }

		public long? DepositFactorAttachment { get; set; }

		public long? PreFactorAttachment { get; set; }

		[Required]
		[MaxLength(255)]
		public string? Number { get; set; }

		public long? ContractAttachment { get; set; }

		public bool? HasGA { get; set; }

		public int? PackingTypeId { get; set; }

		public int? DeliveryTypeId { get; set; }

		public int? ProductTypeId { get; set; }

		public int? CentrifugeSetupTypeId { get; set; }

		public bool? IsNeedProjectManager { get; set; }

		public decimal? ProductionOrderNumber { get; set; }

		public decimal? Revision { get; set; }

		public long? SalesExpertUserIdRef { get; set; }

		public long? SalesManagerUserIdRef { get; set; }

		public long? ProjectManagerUserIdRef { get; set; }

		public long? AlternativeExpertUserIdRef { get; set; }

		public long? RequirementAdvertiseIdRef { get; set; }

		[MaxLength(255)]
		public string? SalesExpert { get; set; }

		public long Creator { get; set; }

		public DateTime CreationDate { get; set; }

		public long LastModifier { get; set; }

		public DateTime LastModificationDate { get; set; }

		public byte[]? Version { get; set; }

		public bool? IsShouldUpdateHts { get; set; }

		public long? SalesExpertIdRef { get; set; }

		[MaxLength(1024)]
		public string? CustomerIndustry { get; set; }

		public decimal? ProjectDlCode { get; set; }

		public int? EdmsProjectId { get; set; }

		public long? IntroducerExpertRef { get; set; }

	}
}
