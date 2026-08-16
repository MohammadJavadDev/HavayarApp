namespace Entities.App.Sale.DTO
{
	public sealed record MissionSalaryPersonnelDTO
	{

		/// <summary>
		/// پرسنل
		/// </summary>
		public long? PersonelId { get; set; }


		/// <summary>
		/// هزینه صبحانه
		/// </summary>
		public decimal? BreakfastFee { get; set; }


		/// <summary>
		/// هزینه ناهار
		/// </summary>
		public decimal? LunchFee { get; set; }


		//[DisplayName("هزینه شام")]
		//[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? DinnerFee { get; set; }


		//[DisplayName("حق شب")]
		//[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? NightRight { get; set; }


		//[DisplayName("حق ماموریت")]
		//[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? MissionRight { get; set; }


		//[DisplayName("مبلغ اضافه کاری")]
		//[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? WorkOvertimeFee { get; set; }


		//[DisplayName("حق تعطیل کاری")]
		//[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? MissionHolidayRight { get; set; }


		//[DisplayName("حقوق روزانه")]
		//[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? DailySalary { get; set; }

		//[DisplayName("تعداد صبحانه")]
		/// <summary>
		/// تعداد صبحانه
		/// </summary>
		public int? BreakfastQuantity { get; set; }

		/// <summary>
		/// تعداد ناهار
		/// </summary>
		public int? LunchQuantity { get; set; }

		/// <summary>
		/// تعداد شام
		/// </summary>
		public int? DinnerQuantity { get; set; }


		/// <summary>
		/// تعداد روزهای ماموریت
		/// </summary>
		public int? MissionDays { get; set; }

		/// <summary>
		/// مدت زمان ماموریت (دقیقه)
		/// </summary>
		public int? DurationInMinutes { get; set; }

		/// <summary>
		/// ساعت اضافه کاری رفت (ساعت)
		/// </summary>
		public int? WentExtraWorkHours { get; set; }

		/// <summary>
		/// ضریب حق شب
		/// </summary>
		public int? nightShiftCoefficient { get; set; }

	}
}
