using Common.Attributes;
using Entities.App.Edms;
using Entities.App.FIN;
using Entities.App.Gnr;
using Entities.App.Hrm;
using Entities.App.Srv.Enums;
using Entities.Auth;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Srv
{
	/// <summary>درخواست بلیط و هتل — معادل HTS <c>Srv_Mission</c> (اسکیمای Srv تا با Sale.Mission قاطی نشود).</summary>
	[Display(Name = "درخواست بلیط و هتل")]
	[Table("TicketHotelRequest", Schema = "Srv")]
	public class TicketHotelRequest : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("نوع درخواست")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public RequestTypeEnum RequestTypeId { get; set; }

		[DisplayName("نوع خدمات")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public ServiceTypeEnum ServiceTypeId { get; set; }

		[DisplayName("متن نوع خدمات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(64)]
		public string? ServiceTypeText { get; set; }

		[DisplayName("مرکز هزینه")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(CostCenterId))]
		public virtual CostCenter? CostCenter { get; set; }
		public long? CostCenterId { get; set; }

		[DisplayName("واحد سازمانی")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		[ForeignKey(nameof(DepartmentId))]
		public virtual OrgUnit? Department { get; set; }
		public long? DepartmentId { get; set; }

		[DisplayName("علت درخواست")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public RequestReasonEnum RequestReasonId { get; set; }

		[DisplayName("متن علت درخواست")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(64)]
		public string? RequestReasonText { get; set; }

		[DisplayName("شماره ماموریت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(32)]
		public string? MissionNumber { get; set; }

		[DisplayName("مرتبط با پروژه")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsRelatedToProject { get; set; }

		[DisplayName("شرکت")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(ManCompanyId))]
		public virtual Party? ManCompany { get; set; }
		public long? ManCompanyId { get; set; }

		/// <summary>پروژه EDMS قدیمی — تاریخی؛ روی فرم نمایش داده نمی‌شود.</summary>
		[DisplayName("پروژه")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		[ForeignKey(nameof(ProjectId))]
		public virtual Project? Project { get; set; }
		public long? ProjectId { get; set; }

		[DisplayName("شناسه متقاضیان")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(50)]
		public string? ApplicantIds { get; set; }

		[DisplayName("متقاضیان")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(255)]
		public string? ApplicantsInText { get; set; }

		[DisplayName("مبدا رفت")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(64)]
		public string? DepartureSource { get; set; }

		[DisplayName("مقصد رفت")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(64)]
		public string? DepartureDestination { get; set; }

		[DisplayName("مبدا برگشت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(64)]
		public string? ReturnSource { get; set; }

		[DisplayName("مقصد برگشت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(64)]
		public string? ReturnDestination { get; set; }

		[DisplayName("تاریخ رفت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(10)]
		public string? DepartureDate { get; set; }

		[DisplayName("ساعت رفت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(5)]
		public string? DepartureTime { get; set; }

		[DisplayName("تاریخ برگشت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(10)]
		public string? ReturnDate { get; set; }

		[DisplayName("ساعت برگشت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(5)]
		public string? ReturnTime { get; set; }

		[DisplayName("وضعیت درخواست")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public RequestStatusEnum RequestStatusId { get; set; }

		[DisplayName("تایید")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? IsApproved { get; set; }

		[DisplayName("تاییدکننده")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(ApproverId))]
		public virtual User? Approver { get; set; }
		public long? ApproverId { get; set; }

		/// <summary>ستون HTS — فیلد فرم ندارد؛ دیتا خالی است.</summary>
		[DisplayName("توضیح تایید")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(256)]
		public string? ApproverComment { get; set; }

		[DisplayName("تاریخ تایید")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(16)]
		public string? ApprovedDateTimeInText { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? Comment { get; set; }

		[DisplayName("تاریخ ایجاد متنی")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(16)]
		public string? CreatedDateInText { get; set; }

		[DisplayName("پروژه (تفصیل)")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(ProjectDlId))]
		public virtual DL? ProjectDl { get; set; }
		public long? ProjectDlId { get; set; }

		public virtual ICollection<TicketHotelRequestTicket>? Tickets { get; set; }
		public virtual ICollection<TicketHotelRequestHotel>? Hotels { get; set; }
	}

	public class TicketHotelRequestConfiguration : IEntityTypeConfiguration<TicketHotelRequest>
	{
		public void Configure(EntityTypeBuilder<TicketHotelRequest> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Srv_TicketHotelRequest_HtsId");

			builder.HasOne(x => x.CostCenter)
				.WithMany()
				.HasForeignKey(x => x.CostCenterId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.HasOne(x => x.Department)
				.WithMany()
				.HasForeignKey(x => x.DepartmentId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.HasOne(x => x.ManCompany)
				.WithMany()
				.HasForeignKey(x => x.ManCompanyId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.HasOne(x => x.Project)
				.WithMany()
				.HasForeignKey(x => x.ProjectId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.HasOne(x => x.ProjectDl)
				.WithMany()
				.HasForeignKey(x => x.ProjectDlId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.HasOne(x => x.Approver)
				.WithMany()
				.HasForeignKey(x => x.ApproverId)
				.OnDelete(DeleteBehavior.NoAction);
		}
	}
}
