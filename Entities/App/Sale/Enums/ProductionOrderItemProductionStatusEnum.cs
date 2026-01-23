using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Entities.App.Sale.Enums
{
    public enum ProductionOrderItemProductionStatusEnum
    {
		[Display(Name = "پایان تولید")]
		ProductionCompleted = 1789,
    
    [Display(Name = "شروع بازرسی نهایی")]
		FinalInspectionStarted = 1790,
    
    [Display(Name = "پایان بسته بندی")]
		PackagingCompleted = 1791,
    
    [Display(Name = "شروع توقف در مرحله تولید")]
		ProductionStageStopStarted = 1795,
    
    [Display(Name = "شروع توقف در مرحله بازرسی نهایی")]
		FinalInspectionStageStopStarted = 1796,
    
    [Display(Name = "پایان توقف در مرحله بازرسی نهایی")]
		FinalInspectionStageStopEnded = 1842,
    
    [Display(Name = "پایان بازرسی نهایی")]
		FinalInspectionCompleted = 1843,
    
    [Display(Name = "پایان توقف در مرحله تولید")]
		ProductionStageStopEnded = 1844,
    
    [Display(Name = "شروع تست تولید")]
		ProductionTestStarted = 2209,
    
    [Display(Name = "پایان تست تولید")]
		ProductionTestCompleted = 2210,
    
    [Display(Name = "شروع توقف در مرحله تست تولید")]
		ProductionTestStageStopStarted = 2226,
    
    [Display(Name = "پایان توقف در مرحله تست تولید")]
		ProductionTestStageStopEnded = 2227,
    
    [Display(Name = "پایان توقف بازدید کارفرما")]
		ClientVisitStopEnded = 3166,
    
    [Display(Name = "شروع توقف به دلیل بازدید کارفرما")]
		ClientVisitStopStarted = 3167,
    
    // Committee/approval statuses (ExtraData: 254)
    [Display(Name = "در کارتابل صنایع (داخلی)")]
		InIndustriesInternalInbox = 1904,
    
    [Display(Name = "در کارتابل صنایع (خارجی)")]
		InIndustriesExternalInbox = 1905,
    
    [Display(Name = "نیاز به استعلام")]
		NeedsInquiry = 1906,
    
    [Display(Name = "نیاز به بررسی وزارت صنایع")]
		NeedsMinistryOfIndustriesReview = 1908,
    
    [Display(Name = "ارسال به رئیس کمیته تامین")]
		SentToProcurementCommitteeHead = 1909,
    
    [Display(Name = "در انتظار تایید مهندسی (مکانیک)")]
		AwaitingMechanicalEngineeringApproval = 2195,
    
    [Display(Name = "در انتظار تایید مهندسی (برق)")]
		AwaitingElectricalEngineeringApproval = 2196,
    
    [Display(Name = "تایید مهندسی (مکانیک)")]
		MechanicalEngineeringApproved = 2197,
    
    [Display(Name = "عدم تایید مهندسی (مکانیک)")]
		MechanicalEngineeringRejected = 2198,
    
    [Display(Name = "تایید مهندسی (برق)")]
		ElectricalEngineeringApproved = 2199,
    
    [Display(Name = "عدم تایید مهندسی (برق)")]
		ElectricalEngineeringRejected = 2200,
    
    [Display(Name = "عدم تایید مهندسی و نیاز به بازنگری")]
		EngineeringRejectedNeedsRevision = 2201,
    
    [Display(Name = "در انتظار بررسی رئیس کمیته تامین")]
		AwaitingProcurementCommitteeHeadReview = 2202,
    
    [Display(Name = "منسوخ شده")]
		Obsolete = 2208,
    
    [Display(Name = "در انتظار تایید مدیر پروژه")]
		AwaitingProjectManagerApproval = 2258,
    
    [Display(Name = "تایید مدیر پروژه")]
		ProjectManagerApproved = 2259,
    
    [Display(Name = "عدم تایید مدیر پروژه")]
		ProjectManagerRejected = 2260,
    
    [Display(Name = "عدم تایید مدیر پروژه و نیاز به بازنگری")]
		ProjectManagerRejectedNeedsRevision = 2261
    }
}
