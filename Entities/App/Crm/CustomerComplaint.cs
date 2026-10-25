using Common.Attributes;
using Entities.App.Crm.Enums;
using Entities.App.Hcm;
using Entities.App.Hrm;
using Entities.App.Inv;
using Entities.App.SLS;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Crm
{
	[Display(Name = "شکایت مشتری")]
	[Table("CustomerComplaint", Schema = "Crm")]
	public class CustomerComplaint : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("نوع ماژول")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public short ModuleTypeId { get; set; } = ComplaintRules.ModuleCustomerComplaint;

		[DisplayName("شماره سریال")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(20)]
		public string? SerialNumber { get; set; }

		[DisplayName("کد رهگیری")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(20)]
		public string? TrackingCode { get; set; }

		[DisplayName("مشتری")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual Customer? Customer { get; set; }
		public long? CustomerId { get; set; }

		[DisplayName("نوع مشتری")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ComplaintCustomerTypeEnum? CustomerType { get; set; }

		[DisplayName("عنوان مشتری")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(128)]
		public string? CustomerTitle { get; set; }

		[DisplayName("نوع")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ComplaintOwnerTypeEnum? OwnerType { get; set; }

		[DisplayName("تاریخ تکمیل")]
		[DisplayInfo("CompletionMiladiDate", true, SystemType.DateShamsi, required: true)]
		[MaxLength(10)]
		public string? CompletionShamsiDate { get; set; }

		[DisplayName("تاریخ تکمیل میلادی")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		[Column(TypeName = "date")]
		public DateTime? CompletionMiladiDate { get; set; }

		[DisplayName("آدرس")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(512)]
		public string? Address { get; set; }

		[DisplayName("تلفن")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(50)]
		public string? PhoneNumber { get; set; }

		[DisplayName("موبایل")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(50)]
		public string? MobileNumber { get; set; }

		[DisplayName("فکس")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(50)]
		public string? Fax { get; set; }

		[DisplayName("دریافت کننده شکایت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? ComplaintReceiver { get; set; }

		[DisplayName("پرسنل دریافت کننده")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		public virtual Personel? ComplaintReceiverPersonel { get; set; }
		public long? ComplaintReceiverPersonelId { get; set; }

		[DisplayName("طرح کننده شکایت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? ComplaintPresenter { get; set; }

		[DisplayName("سمت طرح کننده")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(128)]
		public string? ComplaintPresenterJob { get; set; }

		[DisplayName("واحد سازمانی")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		public virtual OrgUnit? OrgUnit { get; set; }
		public long? OrgUnitId { get; set; }

		[DisplayName("شناسه واحدها")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(255)]
		public string? OrganizationUnitIds { get; set; }

		[DisplayName("واحد سازمانی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(255)]
		public string? OrganizationUnitInText { get; set; }

		[DisplayName("کارشناس یا مدیر")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		public virtual Personel? ExpertPersonel { get; set; }
		public long? ExpertPersonelId { get; set; }

		[DisplayName("شناسه کارشناسان")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(255)]
		public string? ExpertOrManagerIds { get; set; }

		[DisplayName("کارشناس یا مدیر")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(255)]
		public string? ExpertOrManagerInText { get; set; }

		[DisplayName("اولویت")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public ComplaintPriorityEnum? Priority { get; set; }

		[DisplayName("نوع شکایت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ComplaintImportanceEnum? ComplaintType { get; set; }

		[DisplayName("کانال ارتباط")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual ComplaintLookupItem? CommunicationChannel { get; set; }
		public long? CommunicationChannelId { get; set; }

		[DisplayName("مشاهده‌گران")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(512)]
		public string? ViewersIds { get; set; }

		[DisplayName("موضوع کلی")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ComplaintGeneralSubjectEnum? GeneralComplaintSubject { get; set; }

		[DisplayName("عنوان موضوع کلی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(128)]
		public string? GeneralComplaintSubjectTitle { get; set; }

		[DisplayName("کالا")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		public virtual Part? Part { get; set; }
		public long? PartId { get; set; }

		[DisplayName("موضوع شکایت")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual ComplaintLookupItem? ComplaintSubject { get; set; }
		public long? ComplaintSubjectId { get; set; }

		[DisplayName("نمایندگی خاطی")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		public virtual ComplaintAgency? Agency { get; set; }
		public long? AgencyId { get; set; }

		[DisplayName("خرابی کمپرسور")]
		[DisplayInfo(null, false, type: SystemType.Select)]
		public ComplaintCompressorFailureEnum? CompressorFailure { get; set; }

		[DisplayName("شرح شکایت")]
		[DisplayInfo(null, false, type: SystemType.String)]
		public string? ComplaintDescription { get; set; }

		[DisplayName("موضوعات مطرح شده")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? RaisedSubjects { get; set; }

		[DisplayName("شناسه موضوعات مطرح شده")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(256)]
		public string? RaisedSubjectIds { get; set; }

		[DisplayName("تکرارپذیری")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool Repeatability { get; set; }

		[DisplayName("بار مالی")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool HasFinancialLoad { get; set; }

		[DisplayName("فوریت رسیدگی")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool HasUrgencyHandling { get; set; }

		[DisplayName("پیچیدگی")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool HasComplexity { get; set; }

		[DisplayName("اطلاع از دریافت شکایت")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual ComplaintLookupItem? InformForReceive { get; set; }
		public long? InformForReceiveComplaintId { get; set; }

		[DisplayName("شرح اقدامات")]
		[DisplayInfo(null, false, type: SystemType.String)]
		public string? ActionsDescription { get; set; }

		[DisplayName("وضعیت شکایت")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public ComplaintStatusEnum? ComplaintStatus { get; set; }

		[DisplayName("شرح رسیدگی نهایی")]
		[DisplayInfo(null, false, type: SystemType.String)]
		public string? FinalHandlingDescription { get; set; }

		[DisplayName("شناسه نتیجه رسیدگی")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(256)]
		public string? HandlingResultIds { get; set; }

		[DisplayName("نتیجه رسیدگی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(255)]
		public string? HandlingResultIdsInText { get; set; }

		[DisplayName("اطلاع به شاکی")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ComplaintInformComplainantEnum? InformComplainant { get; set; }

		[DisplayName("رضایت شاکی")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ComplaintConsentEnum? ComplaintConsent { get; set; }

		[DisplayName("توضیح مدیر")]
		[DisplayInfo(null, false, type: SystemType.String)]
		public string? ManagerDescription { get; set; }

		[DisplayName("وضعیت نهایی")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public ComplaintFinalStatusEnum? FinalComplaintStatus { get; set; }

		[DisplayName("تاریخ بستن شکایت")]
		[DisplayInfo("ComplaintClosingMiladiDate", true, SystemType.DateShamsi)]
		[MaxLength(10)]
		public string? ComplaintClosingShamsiDate { get; set; }

		[DisplayName("تاریخ بستن میلادی")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		[Column(TypeName = "date")]
		public DateTime? ComplaintClosingMiladiDate { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Comment { get; set; }

		[DisplayName("علت تاخیر")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? DelayReason { get; set; }

		[DisplayName("موضوع اصلی")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ComplaintMainSubjectEnum? ComplaintMainSubject { get; set; }

		[DisplayName("شناسه موضوعات اصلی")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(256)]
		public string? MainSubjectsIds { get; set; }

		[DisplayName("موضوعات اصلی")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(1024)]
		public string? MainSubjectsInText { get; set; }

		public long? ParentId { get; set; }
		public virtual CustomerComplaint? Parent { get; set; }

		[DisplayName("دارای نظرسنجی")]
		[DisplayInfo(null, false, type: SystemType.Boolean)]
		public bool HasComplaintSurvey { get; set; }

		[DisplayName("لینک نظرسنجی ارسال شده")]
		[DisplayInfo(null, false, type: SystemType.Boolean)]
		public bool? IsSurveyLinkSended { get; set; }

		[DisplayName("تاریخ فرم نظرسنجی")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(10)]
		public string? SurveyCompletionShamsiDate { get; set; }

		public long? SurveyInterviewerPersonelId { get; set; }
		public virtual Personel? SurveyInterviewerPersonel { get; set; }

		[DisplayName("پاسخ‌دهنده نظرسنجی")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(64)]
		public string? SurveyResponder { get; set; }

		[DisplayName("توضیح نظرسنجی")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(1024)]
		public string? SurveyDescription { get; set; }

		[DisplayName("تاریخ ارسال لینک")]
		[DisplayInfo("SurveyLinkSentMiladiDateTime", false, SystemType.DateTimeShamsi)]
		[MaxLength(16)]
		public string? SurveyLinkSentShamsiDateTime { get; set; }

		[DisplayInfo(null, false, type: SystemType.DateTime)]
		public DateTime? SurveyLinkSentMiladiDateTime { get; set; }

		[DisplayName("تاریخ پاسخ نظرسنجی")]
		[DisplayInfo("SurveyResponseMiladiDateTime", false, SystemType.DateTimeShamsi)]
		[MaxLength(16)]
		public string? SurveyResponseShamsiDateTime { get; set; }

		[DisplayInfo(null, false, type: SystemType.DateTime)]
		public DateTime? SurveyResponseMiladiDateTime { get; set; }

		[MaxLength(256)]
		public string? SurveySubjectsIds { get; set; }

		[MaxLength(1024)]
		public string? SurveySubjectsInText { get; set; }

		public short? SurveyStatusId { get; set; }

		public virtual ICollection<CustomerComplaintAction> Actions { get; set; } = new List<CustomerComplaintAction>();
		public virtual ICollection<CustomerComplaintContact> Contacts { get; set; } = new List<CustomerComplaintContact>();
		public virtual ICollection<CustomerComplaintAttachment> Attachments { get; set; } = new List<CustomerComplaintAttachment>();
	}

	public class CustomerComplaintConfiguration : IEntityTypeConfiguration<CustomerComplaint>
	{
		public void Configure(EntityTypeBuilder<CustomerComplaint> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Crm_CustomerComplaint_HtsId");

			builder.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.OrgUnit).WithMany().HasForeignKey(x => x.OrgUnitId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.ExpertPersonel).WithMany().HasForeignKey(x => x.ExpertPersonelId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.ComplaintReceiverPersonel).WithMany().HasForeignKey(x => x.ComplaintReceiverPersonelId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.SurveyInterviewerPersonel).WithMany().HasForeignKey(x => x.SurveyInterviewerPersonelId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.Part).WithMany().HasForeignKey(x => x.PartId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.Agency).WithMany().HasForeignKey(x => x.AgencyId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.CommunicationChannel).WithMany().HasForeignKey(x => x.CommunicationChannelId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.ComplaintSubject).WithMany().HasForeignKey(x => x.ComplaintSubjectId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.InformForReceive).WithMany().HasForeignKey(x => x.InformForReceiveComplaintId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.Parent).WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);

			builder.HasMany(x => x.Actions).WithOne(x => x.Complaint).HasForeignKey(x => x.ComplaintId).OnDelete(DeleteBehavior.Cascade);
			builder.HasMany(x => x.Contacts).WithOne(x => x.Complaint).HasForeignKey(x => x.ComplaintId).OnDelete(DeleteBehavior.Cascade);
			builder.HasMany(x => x.Attachments).WithOne(x => x.Complaint).HasForeignKey(x => x.ComplaintId).OnDelete(DeleteBehavior.Cascade);
		}
	}
}
