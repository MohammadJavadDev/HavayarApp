namespace Entities.Base.Sms
{
	/// <summary>
	/// کد گروه‌های اعلان (system.NotificationGroup) که گیرنده پیامک‌اند. با Seed_GeneralSetting_SmsRecipients.sql ساخته می‌شوند؛
	/// اعضا را از پنل «گروه‌های اعلان» اضافه کنید. موبایل از User.PartyId به Personel.Mobile گرفته می‌شود.
	/// </summary>
	public static class SmsRecipientGroups
	{
		/// <summary>معادل گروه کاربری 269 در HTS (پیامک درخواست PM کارکنان کارخانه).</summary>
		public const string PmRequestFactory = "Srv.PmRequest.Factory";

		/// <summary>معادل گروه تکنسین‌های پرسش فنی (TQ_TechnicalTechnician_UserGroup) در HTS.</summary>
		public const string TechnicalQueryTechnician = "Sale.TechnicalQuery.Technician";
	}
}
