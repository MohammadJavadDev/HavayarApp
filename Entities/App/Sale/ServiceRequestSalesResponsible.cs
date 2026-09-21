using Common.Attributes;
using Entities.App.Sale;
using Entities.Auth;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Sale
{
	[Display(Name = "مسئول فروش شعبه")]
	[Table("ServiceRequestSalesResponsible", Schema = "Sale")]
	public class ServiceRequestSalesResponsible : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("کاربر")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual User? User { get; set; }
		public long? UserId { get; set; }

		[DisplayName("شعبه")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual Branch? Branch { get; set; }
		public long? BranchId { get; set; }
	}

	public class ServiceRequestSalesResponsibleConfiguration : IEntityTypeConfiguration<ServiceRequestSalesResponsible>
	{
		public void Configure(EntityTypeBuilder<ServiceRequestSalesResponsible> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_ServiceRequestSalesResponsible_HtsId");
		}
	}
}
