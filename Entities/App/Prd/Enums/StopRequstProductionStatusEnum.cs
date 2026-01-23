using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Entities.App.Prd.Enums
{
    public enum StopRequstProductionStatusEnum
    {

		[Display(Name = "شروع توقف در مرحله تولید")]
		StartStopInProduction = 2576,
		[Display(Name = "شروع توقف در مرحله تست تولید")]
		StartStopInTest = 2577,

		[Display(Name = "شروع توقف در مرحله بازرسی نهایی")]
		StartStopInFinalInspection = 2578,

		[Display(Name = "شروع توقف به دلیل بازدید کارفرما")]
		StartStopDueToClientVisit = 3165,
    }
}
