using System.ComponentModel.DataAnnotations;

namespace Entities.App.Law.Enums
{
	/// <summary>ماهیت پرونده (HTS Gnr_Lookup نوع 148). مقدار = Lookup_ID.</summary>
	public enum JudicialFileNatureEnum
	{
		[Display(Name = "کیفری")]
		Criminal = 953,
		[Display(Name = "حقوقی")]
		Legal = 954,
		[Display(Name = "اداره کار")]
		LaborOffice = 955,
		[Display(Name = "ادارات دولتی")]
		GovernmentOffices = 956
	}

	/// <summary>نوع پرونده (HTS Gnr_Lookup نوع 149).</summary>
	public enum JudicialFileTypeEnum
	{
		[Display(Name = "له")]
		ForCompany = 963,
		[Display(Name = "علیه")]
		AgainstCompany = 964
	}

	/// <summary>وضعیت فعلی پرونده (در HTS داخل کنترلر هاردکد بود: 1، 2، 3).</summary>
	public enum JudicialFileStatusEnum
	{
		[Display(Name = "مختومه بنفع شرکت")]
		ConcludedForCompany = 1,
		[Display(Name = "مختومه بر علیه شرکت")]
		ConcludedAgainstCompany = 2,
		[Display(Name = "مطرح رسیدگی")]
		RaisedTheIssue = 3
	}

	/// <summary>خواسته (HTS Gnr_Lookup نوع 150).</summary>
	public enum JudicialDemandTypeEnum
	{
		[Display(Name = "مطالبه خسارت و ضرر زیان")]
		DamagesClaim = 938,
		[Display(Name = "مطالبه حق الوکاله وکیل")]
		LawyerFeeClaim = 939,
		[Display(Name = "استرداد لاشه چک")]
		ReturnOfCheckStub = 940,
		[Display(Name = "استرداد لاشه سفته")]
		ReturnOfPromissoryNoteStub = 941,
		[Display(Name = "ابطال چک")]
		CheckAnnulment = 943,
		[Display(Name = "تایید فسخ قرارداد")]
		ContractTerminationConfirmation = 944,
		[Display(Name = "الزام به ایفاء تعهد")]
		SpecificPerformance = 945,
		[Display(Name = "مطالبه وجه")]
		PaymentClaim = 946,
		[Display(Name = "مطالبه وجه چک")]
		CheckPaymentClaim = 947,
		[Display(Name = "مطالبه وجه سفته")]
		PromissoryNotePaymentClaim = 948,
		[Display(Name = "ابطال رای داوری")]
		ArbitrationAwardAnnulment = 949,
		[Display(Name = "الزام به انتقال سهم یا سهام شرکت")]
		ShareTransferObligation = 950,
		[Display(Name = "ابطال ورقه اختراع")]
		PatentAnnulment = 951,
		[Display(Name = "مطالبه ثمن معامله")]
		PriceClaim = 952,
		[Display(Name = "دعاوی غیر مالی")]
		NonFinancialClaims = 1035,
		[Display(Name = "دستور موقت")]
		TemporaryOrder = 1036,
		[Display(Name = "تامین خواسته")]
		SecurityOfClaim = 1037,
		[Display(Name = "تامین دلیل")]
		SecurityOfEvidence = 1038,
		[Display(Name = "ابطال ضمانت نامه")]
		GuaranteeAnnulment = 1120,
		[Display(Name = "اعلام مفقودی چک")]
		LostCheckNotice = 1325
	}

	/// <summary>نتیجه نهایی پرونده (HTS Gnr_Lookup نوع 151).</summary>
	public enum JudicialFinalResultEnum
	{
		[Display(Name = "منتهی به صدور قرار منع تعقیب")]
		NoProsecutionOrder = 965,
		[Display(Name = "منتهی به صدور قرار موقوفی تعقیب")]
		ProsecutionSuspensionOrder = 966,
		[Display(Name = "منتهی به صدور قرار جلب به دادرسی")]
		SummonsToTrialOrder = 973,
		[Display(Name = "منتهی به صدور رای بدوی")]
		PrimaryVerdict = 974,
		[Display(Name = "صدور قرار رد دعوا")]
		ClaimRejectionOrder = 975,
		[Display(Name = "صدور قرار رد دادخواست")]
		PetitionRejectionOrder = 976,
		[Display(Name = "صدور قرار ابطال دادخواست")]
		PetitionAnnulmentOrder = 977,
		[Display(Name = "صدور قرار عدم استماع دعوا")]
		ClaimInadmissibilityOrder = 978,
		[Display(Name = "صدور قرار اناطه دعوا")]
		ClaimSuspensionOrder = 979,
		[Display(Name = "صدور رای")]
		Verdict = 980,
		[Display(Name = "صدور قرار کارشناسی")]
		ExpertOpinionOrder = 1121
	}

	/// <summary>هزینه پرونده (HTS Gnr_Lookup نوع 152).</summary>
	public enum JudicialCostTypeEnum
	{
		[Display(Name = "هزینه دادرسی")]
		CourtFee = 957,
		[Display(Name = "حق الوکاله وکیل")]
		LawyerFee = 958,
		[Display(Name = "خسارت احتمالی")]
		PossibleDamages = 959,
		[Display(Name = "کارشناسی")]
		ExpertFee = 960,
		[Display(Name = "تمبر")]
		Stamp = 961,
		[Display(Name = "سایر هزینه های قانونی")]
		OtherLegalCosts = 962
	}

	/// <summary>عنوان طرفین (HTS Gnr_Lookup نوع 153).</summary>
	public enum JudicialPartyTypeEnum
	{
		[Display(Name = "خواهان")]
		Plaintiff = 967,
		[Display(Name = "خوانده")]
		Defendant = 968,
		[Display(Name = "وکیل خواهان")]
		PlaintiffLawyer = 969,
		[Display(Name = "وکیل خوانده")]
		DefendantLawyer = 970,
		[Display(Name = "شاکی")]
		Complainant = 971,
		[Display(Name = "متشاکی")]
		Accused = 972,
		[Display(Name = "وکیل شاکی")]
		ComplainantLawyer = 1326,
		[Display(Name = "وکیل متشاکی")]
		AccusedLawyer = 1327
	}
}
