using Common.Attributes;
using Entities.App.Gnr;
using Entities.App.SLS.Enums;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.SLS
{
	[Display(Name = "مشتری")]
	[Table("Customer", Schema = "SLS")]
	public class Customer : BaseEntity
	{
		[DisplayName("کد")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? Code { get; set; }


		[DisplayName("مشتری")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public Party Party { get; set; }

		public long PartyId { get; set; }


		[DisplayName("نوع")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public CustomerTypeEnum? Type { get; set; }


		[DisplayName("نوع سازمان")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public CustomerOrganizationTypeEnum? OrganizationType { get; set; }


		[DisplayName("صنعت")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public PartyIndustry? Industry { get; set; }

		public long? IndustryId { get; set; }


		[DisplayName("استان")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Region? Province { get; set; }

		public long? ProvinceId { get; set; }


		[DisplayName("شهر")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Region? City { get; set; }
		public long? CityId { get; set; }


		[DisplayName("منطقه")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Region? Area { get; set; }
		public long? AreaId { get; set; }


		[DisplayName("شهرک صنعتی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1000)]
		public string? IndustrialTown { get; set; }


		[DisplayName("رابطه هوایاری")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Party? AirRelation { get; set; }

		public long? AirRelationId { get; set; }


		[DisplayName("وضعیت کارخانه")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public CustomerFactoryStatusEnum? FactoryStatus { get; set; }

		public long HamkaranId { get; set; }


	}
}
