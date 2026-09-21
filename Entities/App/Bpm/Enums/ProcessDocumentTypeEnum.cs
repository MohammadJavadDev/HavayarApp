using System.ComponentModel.DataAnnotations;

namespace Entities.App.Bpm.Enums
{
	/// <summary>
	/// نوع سند — معادل Gnr_Lookup LookupType 230 (Bpm_DocumentType).
	/// نام عضو = LookupCode؛ مقدار = Lookup_ID.
	/// </summary>
	public enum ProcessDocumentTypeEnum
	{
		[Display(Name = "آیین نامه")]
		R = 1724,

		[Display(Name = "طبقه بندی فرآیند")]
		CL = 1725,

		[Display(Name = "فلوچارت فرآیند")]
		FL = 1726,

		[Display(Name = "نقشه فرآیندی")]
		BPM = 1727,

		[Display(Name = "شناسنامه فرآیند")]
		PI = 1728,

		[Display(Name = "شناسنامه شاخص")]
		IN = 1729,

		[Display(Name = "نظامنامه کیفیت")]
		QM = 1730,

		[Display(Name = "خط مشی کیفیت")]
		QP = 1731,

		[Display(Name = "روش اجرایی")]
		P = 1732,

		[Display(Name = "دستورالعمل کاری")]
		W = 1733,

		[Display(Name = "دستورالعمل فنی")]
		WT = 1734,

		[Display(Name = "بخشنامه")]
		C = 1735,

		[Display(Name = "فرم")]
		F = 1793,

		[Display(Name = "ماتریس")]
		M = 1794,

		[Display(Name = "جدول")]
		T = 1823,

		[Display(Name = "کمیته کارگروه")]
		CO = 1824,

		[Display(Name = "نظامنامه گارانتی")]
		WM = 1832,

		[Display(Name = "نمودار سلسله مراتبی")]
		HI = 2566,

		[Display(Name = "سند استراتژی")]
		ST = 2995
	}
}
