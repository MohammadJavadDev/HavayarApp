using Common.Attributes;
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
	[Display(Name = "سفارش فروشگاه اینترنتی")]
	[Table("WebShopOrder", Schema = "Sale")]
	public class WebShopOrder : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("حواله فروش")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual Order? SaleOrder { get; set; }
		public long? SaleOrderId { get; set; }

		[DisplayName("کد مشتری")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int CustomerCode { get; set; }

		[DisplayName("کد سفارش")]
		[DisplayInfo(null, true, type: SystemType.String, showInRelationData: true)]
		[MaxLength(80)]
		public string? OrderCode { get; set; }

		[DisplayName("نام")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2040)]
		public string? FirstName { get; set; }

		[DisplayName("نام خانوادگی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2040)]
		public string? LastName { get; set; }

		[DisplayName("مبلغ خرید")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long BuyPrice { get; set; }

		[DisplayName("ارزش افزوده")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long VatPrice { get; set; }

		[DisplayName("تخفیف")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long TotalDiscount { get; set; }

		[DisplayName("مالیات")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long TaxPrice { get; set; }

		[DisplayName("مبلغ کل")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long TotalPrice { get; set; }

		[DisplayName("فاکتور راهکاران")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? HamkaranFactorId { get; set; }

		[DisplayName("مبلغ فاکتور شده")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? FactoredPrice { get; set; }

		[DisplayName("تاریخ فاکتور شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		[MaxLength(10)]
		public string? FactoredShamsiDate { get; set; }

		[DisplayName("تاییدکننده")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual User? ConfirmedUser { get; set; }
		public long? ConfirmedUserId { get; set; }
	}

	public class WebShopOrderConfiguration : IEntityTypeConfiguration<WebShopOrder>
	{
		public void Configure(EntityTypeBuilder<WebShopOrder> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_WebShopOrder_HtsId");
		}
	}

	[Display(Name = "محصول فروشگاه اینترنتی")]
	[Table("WebShopPartForSale", Schema = "Sale")]
	public class WebShopPartForSale : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("کالا")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual Part? Part { get; set; }
		public long? PartId { get; set; }

		[DisplayName("نوع قطعه")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public WebShopPartTypeEnum PartType { get; set; }

		[DisplayName("قیمت")]
		[DisplayInfo(null, true, type: SystemType.Long, required: true)]
		public long Price { get; set; }
	}

	public class WebShopPartForSaleConfiguration : IEntityTypeConfiguration<WebShopPartForSale>
	{
		public void Configure(EntityTypeBuilder<WebShopPartForSale> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_WebShopPartForSale_HtsId");
		}
	}

	[Display(Name = "گروه کالای فروشگاه")]
	[Table("WebShopPartGroup", Schema = "Sale")]
	public class WebShopPartGroup : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("پیش‌شماره")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short PrefixCode { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(400)]
		public string Title { get; set; } = "";
	}

	public class WebShopPartGroupConfiguration : IEntityTypeConfiguration<WebShopPartGroup>
	{
		public void Configure(EntityTypeBuilder<WebShopPartGroup> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_WebShopPartGroup_HtsId");
		}
	}
}
