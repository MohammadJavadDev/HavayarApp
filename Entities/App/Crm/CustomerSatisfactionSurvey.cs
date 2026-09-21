using Common.Attributes;
using Entities.App.Crm.Enums;
using Entities.App.Sale;
using Entities.App.SLS;
using Entities.Auth;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Crm
{
	[Display(Name = "رضایت‌سنجی مشتری")]
	[Table("CustomerSatisfactionSurvey", Schema = "Crm")]
	public class CustomerSatisfactionSurvey : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("نوع فرم")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public CustomerSatisfactionSurveyTypeEnum TypeId { get; set; } = CustomerSatisfactionSurveyTypeEnum.AfterSales;

		[DisplayName("حواله فروش")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual Order? SaleOrder { get; set; }
		public long? SaleOrderId { get; set; }

		[DisplayName("قلم حواله")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual OrderDetail? SaleOrderDetail { get; set; }
		public long? SaleOrderDetailId { get; set; }

		[DisplayName("گزارش کار")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual WorkReport? WorkReport { get; set; }
		public long? WorkReportId { get; set; }

		[DisplayName("ماموریت")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual Mission? Mission { get; set; }
		public long? MissionId { get; set; }

		[DisplayName("شناسه گزارش کار CNG HTS")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int? HtsCngWorkReportId { get; set; }

		[DisplayName("مشتری")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual Customer? Customer { get; set; }
		public long? CustomerId { get; set; }

		[DisplayName("مصاحبه‌گر")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual User? Interviewer { get; set; }
		public long? InterviewerId { get; set; }

		[DisplayName("سریال فرم")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? FormSerial { get; set; }

		[DisplayName("بازخورد مشتری")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? CustomerFeedbackText { get; set; }

		[DisplayName("توضیحات تکمیلی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? AdditionalComments { get; set; }

		[DisplayName("آدرس")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? CustomerAddress { get; set; }

		[DisplayName("تلفن")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? CustomerTell { get; set; }

		[DisplayName("موبایل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? CustomerMobile { get; set; }

		[DisplayName("نام پاسخ‌دهنده")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? ResponderFullname { get; set; }

		[DisplayName("وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? CurrentStatusId { get; set; }
	}

	public class CustomerSatisfactionSurveyConfiguration : IEntityTypeConfiguration<CustomerSatisfactionSurvey>
	{
		public void Configure(EntityTypeBuilder<CustomerSatisfactionSurvey> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Crm_CustomerSatisfactionSurvey_HtsId");
		}
	}

	[Display(Name = "نتیجه رضایت‌سنجی")]
	[Table("CustomerSatisfactionSurveyResult", Schema = "Crm")]
	public class CustomerSatisfactionSurveyResult : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		public long? SurveyId { get; set; }
		public virtual CustomerSatisfactionSurvey? Survey { get; set; }

		[DisplayName("شناسه سؤال HTS")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int QuestionHtsId { get; set; }

		[DisplayName("امتیاز")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? Score { get; set; }

		[DisplayName("پاسخ متنی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(4000)]
		public string? AnswerText { get; set; }
	}

	[Display(Name = "پیوست رضایت‌سنجی")]
	[Table("CustomerSatisfactionSurveyAttachment", Schema = "Crm")]
	public class CustomerSatisfactionSurveyAttachment : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		public long? SurveyId { get; set; }
		public virtual CustomerSatisfactionSurvey? Survey { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? Title { get; set; }

		[DisplayName("فایل")]
		[DisplayInfo(null, true, type: SystemType.File)]
		public long? FileId { get; set; }
		public virtual FileEntity? File { get; set; }
	}
}
