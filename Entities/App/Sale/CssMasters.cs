using Common.Attributes;
using Entities.App.Crm;
using Entities.App.Gnr;
using Entities.App.Hrm;
using Entities.App.Inv;
using Entities.App.Sale.Enums;
using Entities.App.SLS;
using Entities.Auth;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Sale
{
	[Display(Name = "نقطه سفارش")]
	[Table("OrderPoint", Schema = "Sale")]
	public class OrderPoint : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("کاردکس انبار HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsPartCardexId { get; set; }

		[DisplayName("کالای جایگزین")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual Part? AlternativePart { get; set; }
		public long? AlternativePartId { get; set; }

		[DisplayName("نادیده گرفتن")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsIgnore { get; set; }

		[DisplayName("خارجی")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsExternal { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? Comment { get; set; }
	}

	public class OrderPointConfiguration : IEntityTypeConfiguration<OrderPoint>
	{
		public void Configure(EntityTypeBuilder<OrderPoint> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_OrderPoint_HtsId");
		}
	}

	[Display(Name = "گارانتی مجاز مشتریان")]
	[Table("CustomerAllowedGuarantee", Schema = "Sale")]
	public class CustomerAllowedGuarantee : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("مشتری")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual Customer? Customer { get; set; }
		public long? CustomerId { get; set; }

		[DisplayName("شماره سفارش ساخت")]
		[DisplayInfo(null, true, type: SystemType.Int, required: true)]
		public int ProductionOrderNumber { get; set; }

		[DisplayName("مقدار گارانتی مجاز قطعه")]
		[DisplayInfo(null, true, type: SystemType.Long, required: true)]
		public long AllowedGuaranteePartAmount { get; set; }

		[DisplayName("مقدار گارانتی مجاز ماموریت")]
		[DisplayInfo(null, true, type: SystemType.Long, required: true)]
		public long AllowedMissionGuaranteeAmount { get; set; }
	}

	public class CustomerAllowedGuaranteeConfiguration : IEntityTypeConfiguration<CustomerAllowedGuarantee>
	{
		public void Configure(EntityTypeBuilder<CustomerAllowedGuarantee> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_CustomerAllowedGuarantee_HtsId");
		}
	}

	[Display(Name = "مسئولین مناطق")]
	[Table("ResponsibleZone", Schema = "Sale")]
	public class ResponsibleZone : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("مسئول")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual User? Responsible { get; set; }
		public long? ResponsibleId { get; set; }

		[DisplayName("استان")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual Region? Province { get; set; }
		public long? ProvinceId { get; set; }

		[DisplayName("منطقه")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual Zone? Zone { get; set; }
		public long? ZoneId { get; set; }

		[DisplayName("عنوان مشتریان")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? CustomersTitle { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Comment { get; set; }

		public virtual ICollection<ResponsibleZoneCustomer> Customers { get; set; } = new List<ResponsibleZoneCustomer>();

		/// <summary>
		/// Posted with the header (HTS master-detail save). Not a table column.
		/// </summary>
		[NotMapped]
		[DisplayInfo(null, false, type: SystemType.ListLong)]
		public List<long>? CustomerIds { get; set; }
	}

	[Display(Name = "یادداشت نقطه سفارش")]
	[Table("OrderPointComment", Schema = "Sale")]
	public class OrderPointComment : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		public long? OrderPointId { get; set; }
		public virtual OrderPoint? OrderPoint { get; set; }

		[DisplayName("متن")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(2048)]
		public string? Comment { get; set; }
	}

	public class OrderPointCommentConfiguration : IEntityTypeConfiguration<OrderPointComment>
	{
		public void Configure(EntityTypeBuilder<OrderPointComment> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_OrderPointComment_HtsId");
		}
	}

	public class ResponsibleZoneConfiguration : IEntityTypeConfiguration<ResponsibleZone>
	{
		public void Configure(EntityTypeBuilder<ResponsibleZone> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_ResponsibleZone_HtsId");
			builder.HasMany(x => x.Customers)
				.WithOne(x => x.ResponsibleZone)
				.HasForeignKey(x => x.ResponsibleZoneId)
				.OnDelete(DeleteBehavior.Cascade);
		}
	}

	[Display(Name = "مشتری مسئول منطقه")]
	[Table("ResponsibleZoneCustomer", Schema = "Sale")]
	public class ResponsibleZoneCustomer : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		public long? ResponsibleZoneId { get; set; }
		public virtual ResponsibleZone? ResponsibleZone { get; set; }

		[DisplayName("مشتری")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual Customer? Customer { get; set; }
		public long? CustomerId { get; set; }
	}

	public class ResponsibleZoneCustomerConfiguration : IEntityTypeConfiguration<ResponsibleZoneCustomer>
	{
		public void Configure(EntityTypeBuilder<ResponsibleZoneCustomer> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_ResponsibleZoneCustomer_HtsId");
		}
	}

	[Display(Name = "وصول مسئول منطقه")]
	[Table("ResponsibleZoneReceipt", Schema = "Sale")]
	public class ResponsibleZoneReceipt : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("مسئول")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual User? Responsible { get; set; }
		public long? ResponsibleId { get; set; }

		[DisplayName("از تاریخ میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? FromMiladiDate { get; set; }

		[DisplayName("از تاریخ شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? FromShamsiDate { get; set; }

		[DisplayName("تا تاریخ میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? ToMiladiDate { get; set; }

		[DisplayName("تا تاریخ شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? ToShamsiDate { get; set; }

		[DisplayName("مبلغ وصول")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long ReceiptAmount { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Comment { get; set; }
	}

	public class ResponsibleZoneReceiptConfiguration : IEntityTypeConfiguration<ResponsibleZoneReceipt>
	{
		public void Configure(EntityTypeBuilder<ResponsibleZoneReceipt> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_ResponsibleZoneReceipt_HtsId");
		}
	}

	[Display(Name = "جزئیات سرویس دوره‌ای")]
	[Table("ProductActivityItem", Schema = "Sale")]
	public class ProductActivityItem : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("کالای محصول")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(ProductId))]
		public virtual Part? Product { get; set; }
		public long? ProductId { get; set; }

		[DisplayName("نوع فعالیت")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public ProductActivityTypeEnum ActivityType { get; set; }

		[DisplayName("عنوان اقدام")]
		[DisplayInfo(null, true, type: SystemType.String, showInRelationData: true)]
		[MaxLength(800)]
		public string? ActionTitle { get; set; }

		[DisplayName("قطعه")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(PartId))]
		public virtual Part? Part { get; set; }
		public long? PartId { get; set; }

		[DisplayName("مجری")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ActivityExecutorEnum? ExecutiveCompany { get; set; }

		[DisplayName("مدت")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int Duration { get; set; }

		[DisplayName("مقدار")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal Mount { get; set; }

		[DisplayName("هزینه سرویس")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? ServiceCost { get; set; }

		[DisplayName("واحد سازمانی")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual OrgUnit? RelevantOrganizationUnit { get; set; }
		public long? RelevantOrganizationUnitId { get; set; }
	}

	public class ProductActivityItemConfiguration : IEntityTypeConfiguration<ProductActivityItem>
	{
		public void Configure(EntityTypeBuilder<ProductActivityItem> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_ProductActivityItem_HtsId");
		}
	}

	[Display(Name = "اختصاص قطعه به محصول")]
	[Table("OrderDetailProduct", Schema = "Sale")]
	public class OrderDetailProduct : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("قلم حواله")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(OrderDetailId))]
		public virtual OrderDetail? OrderDetail { get; set; }
		public long? OrderDetailId { get; set; }

		[DisplayName("قلم محصول")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(ProductOrderDetailId))]
		public virtual OrderDetail? ProductOrderDetail { get; set; }
		public long? ProductOrderDetailId { get; set; }

		[DisplayName("سریال")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual OrderDetailSerial? OrderDetailSerial { get; set; }
		public long? OrderDetailSerialId { get; set; }

		[DisplayName("مقدار")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal Mount { get; set; }
	}

	public class OrderDetailProductConfiguration : IEntityTypeConfiguration<OrderDetailProduct>
	{
		public void Configure(EntityTypeBuilder<OrderDetailProduct> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_OrderDetailProduct_HtsId");
		}
	}
}
