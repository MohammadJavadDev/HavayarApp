using System.ComponentModel.DataAnnotations;

namespace Entities.App.Srv.Enums
{
	/// <summary>نوع لوازم‌التحریر — Gnr_Lookup type 160 (HTS Srv_StationeryRequest.StationeryTypeId).</summary>
	public enum StationeryTypeEnum
	{
		[Display(Name = "قیچی")]
		Scissors = 1019,

		[Display(Name = "ماشین حساب")]
		Calculator = 1020,

		[Display(Name = "گیره کاغذ")]
		PaperClip = 1021,

		[Display(Name = "کاغذ یادداشت")]
		NotePaper = 1022,

		[Display(Name = "ماژیک")]
		Marker = 1023,

		[Display(Name = "گیره پوشه")]
		FolderClip = 1024,

		[Display(Name = "ماژیک هایلایت")]
		Highlighter = 1025,

		[Display(Name = "چسب نواری")]
		Tape = 1026,

		[Display(Name = "کش پول")]
		RubberBand = 1027,

		[Display(Name = "پایه چسب")]
		TapeDispenser = 1028,

		[Display(Name = "زونکن")]
		Binder = 1029,

		[Display(Name = "کازیه")]
		Tray = 1030,

		[Display(Name = "خودکار آبی")]
		BluePen = 1048,

		[Display(Name = "خودکار قرمز")]
		RedPen = 1049,

		[Display(Name = "خودکار مشکی")]
		BlackPen = 1050,

		[Display(Name = "خودکار سبز")]
		GreenPen = 1051,

		[Display(Name = "برگه A4")]
		A4Paper = 1058,

		[Display(Name = "باطری قلمی")]
		AABattery = 1059,

		[Display(Name = "باطری نیم قلمی")]
		AAABattery = 1060,

		[Display(Name = "چسب آبکی")]
		LiquidGlue = 1061,

		[Display(Name = "استیکر یادداشت رنگی")]
		StickyNotes = 1062,

		[Display(Name = "استمپ")]
		Stamp = 1063,

		[Display(Name = "جوهر استمپ")]
		StampInk = 1064,

		[Display(Name = "تقویم رومیزی")]
		DeskCalendar = 1065,

		[Display(Name = "پانچ")]
		HolePunch = 1066,

		[Display(Name = "مداد")]
		Pencil = 1067,

		[Display(Name = "مدادتراش")]
		Sharpener = 1068,

		[Display(Name = "مداد نوکی")]
		MechanicalPencil = 1069,

		[Display(Name = "پاک کن")]
		Eraser = 1070,

		[Display(Name = "دفترچه یادداشت")]
		Notebook = 1071,

		[Display(Name = "منگنه")]
		Stapler = 1078,

		[Display(Name = "سوزن منگنه")]
		Staples = 1079,

		[Display(Name = "سوزن کش")]
		StapleRemover = 1080,

		[Display(Name = "چسب 5 سانتی")]
		Tape5Cm = 1112,

		[Display(Name = "ماژیک سی دی")]
		CdMarker = 1113,

		[Display(Name = "پاکت نامه")]
		Envelope = 1554,

		[Display(Name = "پاکت نامه (A4)")]
		EnvelopeA4 = 1555
	}
}
