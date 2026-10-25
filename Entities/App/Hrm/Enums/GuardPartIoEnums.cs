using System.ComponentModel.DataAnnotations;

namespace Entities.App.Hrm.Enums
{
	/// <summary>نوع ورود/خروج کالا — Gnr_Lookup LookupType_FK = 209.</summary>
	public enum GuardIoTypeEnum
	{
		[Display(Name = "امانی")]
		Trust = 1433,

		[Display(Name = "قطعی")]
		Final = 1434,

		[Display(Name = "قابل برگشت")]
		Returnable = 1435,

		[Display(Name = "غیرقابل برگشت")]
		NonReturnable = 1436,

		[Display(Name = "اعلان ورود کالا")]
		EntryNotify = 1564,

		[Display(Name = "ابزار آلات")]
		Tools = 2233,

		[Display(Name = "برگشت خروج اضطراری")]
		EmergencyExitReturn = 2567
	}

	/// <summary>درب ورود/خروج — Gnr_Lookup LookupType_FK = 210.</summary>
	public enum GuardIoDoorEnum
	{
		[Display(Name = "درب بالا (اصلی)")]
		Upper = 1437,

		[Display(Name = "درب پایین (فرعی)")]
		Lower = 1438
	}

	/// <summary>نوع میهمان — Gnr_Lookup LookupType_FK = 207.</summary>
	public enum GuardGuestTypeEnum
	{
		[Display(Name = "ارباب رجوع")]
		Client = 1410,

		[Display(Name = "جلسات، میهمان و مدعوین")]
		MeetingGuest = 1411,

		[Display(Name = "میهمان ویژه")]
		SpecialGuest = 1412,

		[Display(Name = "اتباع بیگانه")]
		ForeignNational = 1413
	}

	/// <summary>نوع اپراتور — Gnr_Lookup LookupType_FK = 211.</summary>
	public enum GuardOperatorTypeEnum
	{
		[Display(Name = "پرسنل")]
		Personnel = 1439,

		[Display(Name = "پیمانکار")]
		Contractor = 1440,

		[Display(Name = "متفرقه")]
		Miscellaneous = 1441
	}

	/// <summary>علت ورود/خروج کالا — Gnr_Lookup LookupType_FK = 212.</summary>
	public enum GuardIoReasonEnum
	{
		[Display(Name = "فروش")]
		Sale = 1444,

		[Display(Name = "تعویض")]
		Replacement = 1445,

		[Display(Name = "تعمیر")]
		Repair = 1446,

		[Display(Name = "خدمات")]
		Service = 1456,

		[Display(Name = "امانی")]
		Trust = 1457,

		[Display(Name = "اظطراری")]
		Emergency = 1458,

		[Display(Name = "ورودی")]
		Incoming = 1459,

		[Display(Name = "داغی")]
		Defective = 1460,

		[Display(Name = "برگشتی")]
		Returned = 1461,

		[Display(Name = "خروج دستی")]
		ManualExit = 1462,

		[Display(Name = "ارسال به پیمانکار")]
		SendToContractor = 1463,

		[Display(Name = "سفارش به پیمانکار")]
		OrderToContractor = 1464,

		[Display(Name = "خرید")]
		Purchase = 1465,

		[Display(Name = "نامنطبق")]
		NonConforming = 1466,

		[Display(Name = "انتقال بین انبارها")]
		WarehouseTransfer = 1467,

		[Display(Name = "مصرف پروژه")]
		ProjectConsumption = 1468,

		[Display(Name = "برگشت از سایت پروژه")]
		ReturnFromProjectSite = 1565,

		[Display(Name = "برگشت از پیمانکار")]
		ReturnFromContractor = 1567,

		[Display(Name = "کالیبراسیون")]
		Calibration = 1568,

		[Display(Name = "گارانتی")]
		Warranty = 1569,

		[Display(Name = "آماده سازی")]
		Preparation = 1570,

		[Display(Name = "ساخت")]
		Manufacturing = 1571,

		[Display(Name = "اصلاح")]
		Correction = 1572,

		[Display(Name = "نامشخص")]
		Unknown = 1573,

		[Display(Name = "برگشت از فروش")]
		ReturnFromSale = 1656,

		[Display(Name = "برگشت از رسید موقت خرید داخلی")]
		ReturnFromTemporaryReceipt = 3028
	}
}
