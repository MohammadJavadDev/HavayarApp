using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Hts.Edms
{
	[Table("Edms_Project")]
	public class Hts_Edms_Project
	{
		[Key]
		public int Edms_Project_ID { get; set; }

		[StringLength(128)]
		public string? Project_Code { get; set; }

		[StringLength(256)]
		public string? Project_Name { get; set; }

		[StringLength(256)]
		public string? Project_Title { get; set; }

		public int Main_Client_FK { get; set; }

		public short? Project_Manager_FK { get; set; }

		public short? BeneficiaryUnit_FK { get; set; }

		public short? Project_Engineer_FK { get; set; }

		public short? Project_Coordinator_FK { get; set; }

		public short? DccUserId { get; set; }

		[StringLength(10)]
		public string? Project_Startdate { get; set; }

		[StringLength(10)]
		public string? Project_Startdate_Shamsi { get; set; }

		[StringLength(10)]
		public string? Project_Enddate { get; set; }

		[StringLength(10)]
		public string? Project_Enddate_Shamsi { get; set; }

		public short Project_Status_FK { get; set; }

		[StringLength(2048)]
		public string? Transmital_PreField { get; set; }

		[StringLength(4000)]
		public string? Transmital_Recipient { get; set; }

		[StringLength(4000)]
		public string? Transmital_CC { get; set; }

		[StringLength(1024)]
		public string? MiscEmailAddress { get; set; }

		[StringLength(2400)]
		public string? Project_Description { get; set; }

		public byte? AllowedHavayar_DelayDay { get; set; }

		public byte? AllowedEmployer_DelayDay { get; set; }

		public short? SalesExpertUserId { get; set; }

		public short? ProcessExpertId { get; set; }

		public short? MechanicalExpertId { get; set; }

		public short? ControlExpertId { get; set; }

		public short? InstrumentationExpertId { get; set; }

		public short? ElectricalExpertId { get; set; }

		public short? PipingExpertId { get; set; }

		public bool IsVendorProject { get; set; }

		public short? ProjectInspectorId { get; set; }

		public short? MechanicalSuppliesResponsibleId { get; set; }

		public short? ElectricalSuppliesResponsibleId { get; set; }

		public short? ExternalSuppliesResponsibleId { get; set; }

		public bool IsConfidential { get; set; }

		[StringLength(50)]
		public string? CustomPermissionUserIds { get; set; }

		[StringLength(512)]
		public string? CustomPermissionUsersInText { get; set; }

		public bool IsTakvinProject { get; set; }

		[StringLength(512)]
		public string? BeneficiariesIds { get; set; }

		public short CreatedUserId { get; set; }

		public short UpdatedUserId { get; set; }

		public DateTime CreatedDate { get; set; }

		public DateTime UpdatedDate { get; set; }
	}
}
