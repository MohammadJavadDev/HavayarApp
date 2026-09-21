using Common.Attributes;
using Entities.App.Sale.Enums;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Sale
{
	[Display(Name = "نظر درخواست مشتری")]
	[Table("CustomerRequestComment", Schema = "Sale")]
	public class CustomerRequestComment : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("درخواست")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(CustomerRequestId))]
		public virtual CustomerRequest? CustomerRequest { get; set; }
		public long? CustomerRequestId { get; set; }

		[DisplayName("وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public CustomerRequestStatusEnum? StatusId { get; set; }

		[DisplayName("نظر")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(512)]
		public string? Comment { get; set; }
	}

	public class CustomerRequestCommentConfiguration : IEntityTypeConfiguration<CustomerRequestComment>
	{
		public void Configure(EntityTypeBuilder<CustomerRequestComment> builder)
		{
			builder.HasOne(x => x.CustomerRequest)
				.WithMany()
				.HasForeignKey(x => x.CustomerRequestId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasIndex(x => x.HtsId)
				.IsUnique()
				.HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_CustomerRequestComment_HtsId");
		}
	}
}
