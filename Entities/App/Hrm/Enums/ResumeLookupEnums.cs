using System.ComponentModel.DataAnnotations;

namespace Entities.App.Hrm.Enums
{
	/// <summary>جنسیت رزومه — همان <c>Gnr_Sex</c>. عنوان محاسباتی قدیم: غیر از ۲ یعنی زن.</summary>
	public enum ResumeSexEnum
	{
		[Display(Name = "مرد")]
		Male = 2,

		[Display(Name = "زن")]
		Female = 3
	}

	/// <summary>مقطع — همان <c>Gnr_Education.Education_ID</c>.</summary>
	public enum ResumeEducationEnum
	{
		[Display(Name = "بیسواد")]
		Illiterate = 1,

		[Display(Name = "ابتدائی")]
		Primary = 2,

		[Display(Name = "سیکل")]
		MiddleSchool = 3,

		[Display(Name = "زیردیپلم")]
		BelowDiploma = 4,

		[Display(Name = "دیپلم")]
		Diploma = 5,

		[Display(Name = "فوق دیپلم")]
		Associate = 6,

		[Display(Name = "لیسانس")]
		Bachelor = 7,

		[Display(Name = "فوق لیسانس")]
		Master = 8,

		[Display(Name = "دکتری")]
		Doctorate = 9,

		[Display(Name = "فوق دکتری")]
		PostDoctorate = 10,

		[Display(Name = "دانشجوی (فوق دیپلم)")]
		StudentAssociate = 11,

		[Display(Name = "دانشجوی (لیسانس)")]
		StudentBachelor = 12,

		[Display(Name = "دانشجوی (فوق لیسانس)")]
		StudentMaster = 13,

		[Display(Name = "دانشجوی (دکتری)")]
		StudentDoctorate = 14
	}

	/// <summary>وضعیت رزومه — LookupType 14.</summary>
	public enum ResumeStatusEnum
	{
		[Display(Name = "بایگانی")]
		Archive = 52,

		[Display(Name = "نامناسب")]
		Unsuitable = 53,

		[Display(Name = "رزرو")]
		Reserve = 54,

		[Display(Name = "جذب")]
		Hired = 73,

		[Display(Name = "عدم مصاحبه")]
		NoInterview = 1715,

		[Display(Name = "عدم حضور در مصاحبه")]
		AbsentFromInterview = 1716,

		[Display(Name = "انصراف")]
		Withdrawn = 1717,

		[Display(Name = "برای پست های سازمانی هوایار مناسب نمی باشد")]
		NotSuitableForHavayarPosts = 1718
	}

	/// <summary>سازمان — LookupType 183.</summary>
	public enum ResumeOrganizationEnum
	{
		[Display(Name = "هوایار")]
		Havayar = 1185,

		[Display(Name = "هوایار کنترل پیشرو")]
		HavayarControlPishro = 1186,

		[Display(Name = "هوایار سوخت")]
		HavayarFuel = 1187,

		[Display(Name = "هوایار گاز")]
		HavayarGas = 1188
	}

	/// <summary>بررسی هیئت مدیره — LookupType 226.</summary>
	public enum ResumeBoardCheckStatusEnum
	{
		[Display(Name = "تایید شده")]
		Approved = 1694,

		[Display(Name = "ریجکت شده")]
		Rejected = 1695
	}

	/// <summary>وضعیت بررسی رزومه — LookupType 294.</summary>
	public enum ResumeReviewStatusEnum
	{
		[Display(Name = "دعوت به مصاحبه")]
		InviteToInterview = 2240,

		[Display(Name = "عدم تایید")]
		Rejected = 2241,

		[Display(Name = "در دست بررسی")]
		UnderReview = 2242,

		[Display(Name = "رزرو")]
		Reserve = 2243
	}

	/// <summary>وضعیت مصاحبه تخصصی — LookupType 295.</summary>
	public enum ResumeSpecializedInterviewStatusEnum
	{
		[Display(Name = "عدم حضور مصاحبه شونده بدون هماهنگی قبلی")]
		AbsentWithoutNotice = 2244,

		[Display(Name = "تایید")]
		Approved = 2245,

		[Display(Name = "عدم تایید")]
		Rejected = 2246,

		[Display(Name = "در دست بررسی")]
		UnderReview = 2247,

		[Display(Name = "رزرو")]
		Reserve = 2248
	}

	/// <summary>وضعیت مصاحبه منابع انسانی — LookupType 296.</summary>
	public enum ResumeHrInterviewStatusEnum
	{
		[Display(Name = "عدم حضور مصاحبه شونده بدون هماهنگی قبلی")]
		AbsentWithoutNotice = 2249,

		[Display(Name = "تایید")]
		Approved = 2250,

		[Display(Name = "عدم تایید")]
		Rejected = 2251,

		[Display(Name = "نظر منابع انسانی مثبت بوده ولی مصاحبه شونده مبلغ و شرایط کاری را نپذیرفته")]
		AcceptedButOfferDeclined = 2252
	}

	/// <summary>سطح آشنایی — LookupType 12.</summary>
	public enum ResumeKnowledgeLevelEnum
	{
		[Display(Name = "کم")]
		Low = 46,

		[Display(Name = "متوسط")]
		Medium = 47,

		[Display(Name = "خوب")]
		Good = 48,

		[Display(Name = "عالی")]
		Excellent = 49
	}

	/// <summary>نوع همکاری سابقه کار — LookupType 10.</summary>
	public enum ResumeEmploymentTypeEnum
	{
		[Display(Name = "تمام وقت")]
		FullTime = 40,

		[Display(Name = "پاره وقت")]
		PartTime = 41,

		[Display(Name = "نیمه وقت")]
		HalfTime = 42
	}

	/// <summary>نحوه یادگیری نرم‌افزار — LookupType 205.</summary>
	public enum ResumeSoftwareLearnTypeEnum
	{
		[Display(Name = "تجربه حین کار")]
		OnTheJob = 1397,

		[Display(Name = "دوره آموزشی")]
		TrainingCourse = 1398
	}
}
