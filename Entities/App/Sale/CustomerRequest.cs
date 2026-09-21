using Common.Attributes;
using Entities.App.Gnr;
using Entities.App.Inv;
using Entities.App.Sale.Enums;
using Entities.App.SLS;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Sale
{
	[Display(Name = "درخواست مشتریان")]
	[Table("CustomerRequest", Schema = "Sale")]
	public class CustomerRequest : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("واحد سازمانی")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int OrganizationUnitId { get; set; }

		[DisplayName("نوع درخواست")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int RequestTypeId { get; set; }

		[DisplayName("نوع درخواست")]
		[DisplayInfo(null, true, type: SystemType.String, showInRelationData: true)]
		[MaxLength(200)]
		public string? RequestTypeTitle { get; set; }

		[DisplayName("کالا")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(PartId))]
		public virtual Part? Part { get; set; }
		public long? PartId { get; set; }

		[DisplayName("شرح کالا")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(128)]
		public string? PartTitle { get; set; }

		[DisplayName("متن درخواست")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? RequestText { get; set; }

		[DisplayName("مشتری")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(CustomerId))]
		public virtual Customer? Customer { get; set; }
		public long? CustomerId { get; set; }

		[DisplayName("عنوان مشتری")]
		[DisplayInfo(null, true, type: SystemType.String, showInRelationData: true)]
		[MaxLength(128)]
		public string? CustomerTitle { get; set; }

		[DisplayName("آدرس")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(512)]
		public string? CustomerAddress { get; set; }

		[DisplayName("تلفن")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(11)]
		public string? CustomerPhone { get; set; }

		[DisplayName("نام فایل پیوست")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(800)]
		public string? AttachmentFileName { get; set; }

		[DisplayName("پیوست")]
		[DisplayInfo(null, false, type: SystemType.File)]
		public byte[]? AttachmentFile { get; set; }

		[DisplayName("وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public CustomerRequestStatusEnum? StatusId { get; set; }

		[DisplayName("استان")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(ProvinceId))]
		public virtual Region? Province { get; set; }
		public long? ProvinceId { get; set; }

		[DisplayName("تولید هوایار")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? HasHavayarProduction { get; set; }

		[DisplayName("نماینده مرتبط")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(128)]
		public string? RelatedAgentName { get; set; }
	}

	public class CustomerRequestConfiguration : IEntityTypeConfiguration<CustomerRequest>
	{
		public void Configure(EntityTypeBuilder<CustomerRequest> builder)
		{
			builder.HasIndex(x => x.HtsId)
				.IsUnique()
				.HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_CustomerRequest_HtsId");
		}
	}
}
