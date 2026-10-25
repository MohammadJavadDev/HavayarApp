namespace Entities.App.Qc
{
	public static class ProductInspectionConstants
	{
		/// <summary>محل بازرسی داخل هوایار — Lookup 469 در HTS.</summary>
		public const short InCompany = 469;

		/// <summary>بازرسی محصول در محل پیمانکار — Lookup 470 در HTS.</summary>
		public const short OutCompany = 470;

		/// <summary>کد پرسنل بازرس‌های مجاز فرم، همان Personel_IDهای ثابت صفحه قدیم.</summary>
		public static readonly string[] TestPersonelCodes =
		[
			"87077", "87105", "89054", "87043", "84036", "91038", "93005",
			"93026", "93060", "94039", "96021", "98065", "99035"
		];
	}
}
