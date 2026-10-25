namespace Entities.App.Crm
{
	/// <summary>شناسه‌های HTS که منطق صفحه شکایت به آن‌ها وابسته است.</summary>
	public static class ComplaintRules
	{
		public const short ModuleCustomerComplaint = 1691;
		public const short ModuleSatisfactionProcess = 2256;

		public const short LookupChannel = 173;
		public const short LookupSubject = 174;
		public const short LookupInformReceive = 177;
		public const short LookupHandlingResult = 179;

		public const long SubjectAddNewHtsId = 1156;
		public const short OwnerTypeHavayar = 1499;
		public const long ChannelAddNewHtsId = 1134;
		public const long InformReceiveAddNewHtsId = 1167;
		public const long HandlingResultAddNewHtsId = 1177;

		public const short FinalClosed = 1182;
		public const short FinalNotClosed = 1183;

		public static readonly long[] WrongdoerAgencySubjectHtsIds = [1154, 2054, 2083, 2521];
		public const long CompressorFailureSubjectHtsId = 1155;
		public const short GeneralSubjectProduct = 1495;
		public const short GeneralSubjectProductAndProcess = 1775;
	}
}
