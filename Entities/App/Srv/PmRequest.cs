using Common.Attributes;
using Entities.App.Hrm;
using Entities.App.Srv.Enums;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Srv
{
	/// <summary>درخواست PM — معادل HTS <c>Srv_PmRequest</c>.</summary>
	[Display(Name = "درخواست PM")]
	[Table("PmRequest", Schema = "Srv")]
	public class PmRequest : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("نوع")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public PmRequestTypeEnum TypeId { get; set; }

		[DisplayName("متن درخواست")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? RequestText { get; set; }

		[DisplayName("کامنت آخر")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? LastComment { get; set; }

		[DisplayName("تاریخ تقریبی اجرا")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(20)]
		public string? ApproximateExecutionDate { get; set; }

		/// <summary>
		/// وضعیت عددی HTS. برای برچسب‌های شناخته‌شده از <see cref="PmRequestStatusEnum"/> استفاده کنید
		/// ولی این ستون را به آن enum نگاشت نکنید.
		/// </summary>
		[DisplayName("وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Int, required: true)]
		public short StatusId { get; set; }

		[DisplayName("زمان انجام")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(20)]
		public string? DoneDateTimeInText { get; set; }

		[DisplayName("واحد سازمانی")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(OrganizationUnitId))]
		public virtual OrgUnit? OrganizationUnit { get; set; }
		public long? OrganizationUnitId { get; set; }

		/// <summary>
		/// عنوان واحد سازمانی HTS وقتی به <see cref="Hrm.OrgUnit"/> وصل نشود
		/// (واحدهای بدون Hamkaran_Unit_FK یا عنوان تکراری).
		/// </summary>
		[DisplayName("عنوان واحد سازمانی")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(500)]
		public string? CreatedOrganizationUnitTitle { get; set; }

		[DisplayName("تاریخ ایجاد")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(20)]
		public string? CreatedDateInText { get; set; }

		public virtual ICollection<PmRequestComment>? Comments { get; set; }
		public virtual ICollection<PmRequestAttachment>? Attachments { get; set; }
	}

	public class PmRequestConfiguration : IEntityTypeConfiguration<PmRequest>
	{
		public void Configure(EntityTypeBuilder<PmRequest> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Srv_PmRequest_HtsId");

			builder.HasOne(x => x.OrganizationUnit)
				.WithMany()
				.HasForeignKey(x => x.OrganizationUnitId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}
}
