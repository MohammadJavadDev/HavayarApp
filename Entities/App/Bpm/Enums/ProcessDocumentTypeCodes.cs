namespace Entities.App.Bpm.Enums
{
	/// <summary>
	/// LookupCode برای شماره‌گذاری (فاز ۷). برای نوع سند نام enum همان کد است.
	/// مجموعه فرآیندی ۱۷۵۰ کد ندارد؛ ۱۷۶۰ کد TI است نه IT.
	/// </summary>
	public static class ProcessDocumentTypeCodes
	{
		public static string GetLookupCode(ProcessDocumentTypeEnum documentType) => documentType.ToString();

		public static string? GetLookupCode(ProcessDocumentProcessSetEnum processSet)
		{
			if (processSet == ProcessDocumentProcessSetEnum.StrategicManagement)
				return null;

			return processSet.ToString();
		}
	}
}
