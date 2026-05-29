using Common.Attributes;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Acc
{
	[Display(Name = "بایگانی نرخ ارز")]
	[Table("ExchangeRateArchive", Schema = "Acc")]
	public class ExchangeRateArchive : BaseEntity
	{
		[DisplayName("واحد ارز")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public PriceUnit? PriceUnit { get; set; }

		public long? PriceUnitId { get; set; }


		[DisplayName("درصد تغییر")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? PercentageChange { get; set; }


		[DisplayName("نرخ تغییر")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? RateofChange { get; set; }


		[DisplayName("قیمت نهایی")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? FinalPrice { get; set; }


		[DisplayName("بیشترین قیمت")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? MaxPrice { get; set; }


		[DisplayName("کمترین قیمت")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? MinPrice { get; set; }


		[DisplayName("قیمت اولیه")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? Price { get; set; }


		[DisplayName("درصد تغییر سنا")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? SanaPercentageChange { get; set; }


		[DisplayName("نرخ تغییر سنا")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? SanaRateofChange { get; set; }


		[DisplayName("قیمت نهایی سنا")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? SanaFinalPrice { get; set; }


		[DisplayName("بیشترین قیمت سنا")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? SanaMaxPrice { get; set; }


		[DisplayName("کمترین قیمت سنا")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? SanaMinPrice { get; set; }


		[DisplayName("قیمت اولیه سنا")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? SanaInitialPrice { get; set; }


		[DisplayName("آیا محاسبه و جمع دستی است")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsComputeAndAddManually { get; set; } = false;


		[DisplayName("کامنت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Comment { get; set; }


	}
}
