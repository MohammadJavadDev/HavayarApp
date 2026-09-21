using System.ComponentModel.DataAnnotations;

namespace Entities.App.Bpm.Enums
{
	/// <summary>
	/// نوع مجموعه فرآیندی — معادل Gnr_Lookup LookupType 234 (Bpm_ProcessType).
	/// نام عضو = LookupCode وقتی شناسهٔ معتبر است؛ ۱۷۵۰ کد ندارد.
	/// کد ۱۷۶۰ برابر TI است (عنوان فارسی IT دارد).
	/// </summary>
	public enum ProcessDocumentProcessSetEnum
	{
		[Display(Name = "مدیریت استراتژیک")]
		StrategicManagement = 1750,

		[Display(Name = "بازاریابی و فروش محصولات و خدمات (MS)")]
		MS = 1751,

		[Display(Name = "مدیریت پروژه (PM)")]
		PM = 1752,

		[Display(Name = "طراحی، مهندسی و توسعه محصول (DE)")]
		DE = 1753,

		[Display(Name = "تأمین متریال و خدمات (PU)")]
		PU = 1754,

		[Display(Name = "مدیریت لجستیک و انبارداری (LW)")]
		LW = 1755,

		[Display(Name = "تولید، تحویل محصول و ارائه خدمات (PD)")]
		PD = 1756,

		[Display(Name = "مدیریت خدمات مشتریان(CS)")]
		CS = 1757,

		[Display(Name = "مدیریت منابع مالی(FI)")]
		FI = 1758,

		[Display(Name = "مدیریت ارتباطات (RM)")]
		RM = 1759,

		[Display(Name = "مدیریت فناوری اطلاعات (IT)")]
		TI = 1760,

		[Display(Name = "توسعه و مدیریت قابلیتهای کسب و کار (BM)")]
		BM = 1761,

		[Display(Name = "مدیریت زیرساختها و داراییها (AS)")]
		AS = 1762,

		[Display(Name = "توسعه و مدیریت سرمایه های انسانی (HC)")]
		HC = 1763
	}
}
