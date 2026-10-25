namespace Services.Sms
{
	/// <summary>کدهای وضعیت دلیوری sms.ir (مستندات رسمی: «کدهای وضعیت دلیوری»).</summary>
	public static class SmsDeliveryStates
	{
		public static string Text(int? code) => code switch
		{
			1 => "رسیده به گوشی",
			2 => "نرسیده به گوشی",
			3 => "پردازش در مخابرات",
			4 => "نرسیده به مخابرات",
			5 => "رسیده به مخابرات",
			6 => "خطا",
			7 => "لیست سیاه",
			null => "نامشخص",
			_ => $"کد {code}"
		};

		public static SmsDeliveryOutcome ToOutcome(int? code) => code switch
		{
			1 => SmsDeliveryOutcome.Delivered,
			2 or 4 or 6 or 7 => SmsDeliveryOutcome.NotDelivered,
			3 or 5 => SmsDeliveryOutcome.Pending,
			_ => SmsDeliveryOutcome.Unknown
		};
	}
}
