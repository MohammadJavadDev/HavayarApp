using Common.Attributes;
using Entities.App.Crm;
using Entities.App.FIN;
using Entities.App.Gnr;
using Entities.App.SLS.Enums;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.SLS
{
	[Display(Name = "سایت های مشتریان")]
	[Table("CustomerAddress", Schema = "SLS")]
	public class CustomerAddress : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("مشتری")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(CustomerId))]
		public virtual Customer? Customer { get; set; }
		public long? CustomerId { get; set; }

		[DisplayName("نام جایگاه")]
		[DisplayInfo(null, true, type: SystemType.String, showInRelationData: true)]
		[MaxLength(400)]
		public string? Title { get; set; }

		[DisplayName("آدرس جایگاه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(4000)]
		public string? Address { get; set; }

		[DisplayName("منطقه")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(ZoneId))]
		public virtual Zone? Zone { get; set; }
		public long? ZoneId { get; set; }

		[DisplayName("گرید")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public CustomerGradeEnum? Grade { get; set; }

		[DisplayName("استان")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(ProvinceId))]
		public virtual Region? Province { get; set; }
		public long? ProvinceId { get; set; }

		[DisplayName("شخص|شرکت نمایندگی")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(AgencyPartyId))]
		public virtual Party? AgencyParty { get; set; }
		public long? AgencyPartyId { get; set; }

		[DisplayName("تفصیل نمایندگی")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(AgencyDlId))]
		public virtual DL? AgencyDl { get; set; }
		public long? AgencyDlId { get; set; }

		[DisplayName("شناسه آدرس راهکاران")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int HamkaranAddId { get; set; }

		[DisplayName("شناسه راهکاران")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? RahkaranId { get; set; }

		[DisplayName("نسخه راهکاران")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? RahkaranVersion { get; set; }
	}

	public class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
	{
		public void Configure(EntityTypeBuilder<CustomerAddress> builder)
		{
			builder.HasOne(x => x.Customer)
				.WithMany()
				.HasForeignKey(x => x.CustomerId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.Zone)
				.WithMany()
				.HasForeignKey(x => x.ZoneId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.Province)
				.WithMany()
				.HasForeignKey(x => x.ProvinceId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasIndex(x => x.HtsId)
				.IsUnique()
				.HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_SLS_CustomerAddress_HtsId");
		}
	}
}
