using Common.Attributes;
using Entities.App.FIN;
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
	[Display(Name = "وصول مطالبات")]
	[Table("CollectionClaim", Schema = "Sale")]
	public class CollectionClaim : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("مشتری")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual Customer? Customer { get; set; }
		public long? CustomerId { get; set; }

		[DisplayName("تفصیل")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual DL? Dl { get; set; }
		public long? DlId { get; set; }

		[DisplayName("شماره سند")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int VchNumber { get; set; }

		[DisplayName("شناسه هدر سند حسابداری HTS")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int HtsHdrVchId { get; set; }

		[DisplayName("شناسه قلم سند حسابداری HTS")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int HtsVchItemId { get; set; }

		[DisplayName("تاریخ سند میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? VchMiladiDate { get; set; }

		[DisplayName("تاریخ سند شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? VchShamsiDate { get; set; }

		[DisplayName("عنوان تفصیل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(4000)]
		public string? DlTitle { get; set; }

		[DisplayName("شرح سند")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(4000)]
		public string? VchDescription { get; set; }

		[DisplayName("بدهکار")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long Debit { get; set; }

		[DisplayName("بستانکار")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long Credit { get; set; }
	}

	public class CollectionClaimConfiguration : IEntityTypeConfiguration<CollectionClaim>
	{
		public void Configure(EntityTypeBuilder<CollectionClaim> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_CollectionClaim_HtsId");
		}
	}

	[Display(Name = "فیش وصول")]
	[Table("CollectionClaimPayFish", Schema = "Sale")]
	public class CollectionClaimPayFish : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("مطالبه")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual CollectionClaim? CollectionClaim { get; set; }
		public long? CollectionClaimId { get; set; }

		[DisplayName("مبلغ وصول")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long CollectedPrice { get; set; }

		[DisplayName("غیرقابل وصول")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool Uncollectable { get; set; }

		[DisplayName("تخفیف")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool HasDiscount { get; set; }

		[DisplayName("نمایندگی")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsAgency { get; set; }

		[DisplayName("تاریخ فیش شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		[MaxLength(10)]
		public string? PayFishShamsiDate { get; set; }

		[DisplayName("مسئول")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual User? Responsible { get; set; }
		public long? ResponsibleId { get; set; }

		[DisplayName("شماره فیش")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(400)]
		public string? FishNumber { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(4000)]
		public string? Comment { get; set; }
	}

	public class CollectionClaimPayFishConfiguration : IEntityTypeConfiguration<CollectionClaimPayFish>
	{
		public void Configure(EntityTypeBuilder<CollectionClaimPayFish> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_CollectionClaimPayFish_HtsId");
		}
	}

	[Display(Name = "پیوست وصول")]
	[Table("CollectionClaimAttachment", Schema = "Sale")]
	public class CollectionClaimAttachment : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		public long? CollectionClaimId { get; set; }
		public virtual CollectionClaim? CollectionClaim { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? Title { get; set; }

		[DisplayName("فایل")]
		[DisplayInfo(null, true, type: SystemType.File)]
		public long? FileId { get; set; }
		public virtual FileEntity? File { get; set; }
	}
}
