using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Hrm
{
	/// <summary>
	/// هدر فرم خروج اضطراری — معادل ستون‌هایی از HTS <c>Enex_ExitForm</c> که فرم ورود، دانلود پیوست و یادآوری بازگشت می‌خوانند.
	/// صفحه خروج در این مرحله ساخته نمی‌شود.
	/// </summary>
	[Display(Name = "فرم خروج اضطراری")]
	[Table("EnexExitForm", Schema = "Hrm")]
	public class EnexExitForm : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("شماره فرم")]
		[DisplayInfo(null, true, type: SystemType.Int, showInRelationData: true)]
		public int FormNumber { get; set; }

		[DisplayName("برگشت‌پذیر")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsReturnable { get; set; }

		/// <summary>شناسه وضعیت HTS. یادآوری بازگشت فقط وضعیت 775 (تایید حراست) را می‌خواند.</summary>
		[DisplayName("آخرین وضعیت")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public short LastStatusId { get; set; }

		[DisplayName("تاریخ برگشت اعلام‌شده")]
		[DisplayInfo("ClaimedReturnMiladiDate", true, type: SystemType.DateShamsi)]
		[MaxLength(16)]
		public string? ClaimedReturnShamsiDate { get; set; }

		[Column(TypeName = "date")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? ClaimedReturnMiladiDate { get; set; }

		[DisplayName("بازگشت شده")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsReturned { get; set; }

		[DisplayName("تاریخ بازگشت")]
		[DisplayInfo("ReturnMiladiDate", true, type: SystemType.DateShamsi)]
		[MaxLength(16)]
		public string? ReturnShamsiDate { get; set; }

		[Column(TypeName = "date")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? ReturnMiladiDate { get; set; }

		[DisplayName("فایل پیوست")]
		[DisplayInfo(null, true, type: SystemType.File)]
		[ForeignKey(nameof(FileId))]
		public virtual FileEntity? File { get; set; }
		public long? FileId { get; set; }

		[DisplayName("نام پیوست HTS")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(1024)]
		public string? AttachmentFileName { get; set; }

		/// <summary>مسیر فایل در HTS — منبع جاب انتقال یک‌باره.</summary>
		[DisplayName("مسیر پیوست HTS")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(1024)]
		public string? HtsAttachmentPath { get; set; }

		[DisplayName("واحد سازمانی")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(OrganizationUnitId))]
		public virtual OrgUnit? OrganizationUnit { get; set; }
		public long? OrganizationUnitId { get; set; }
	}

	public class EnexExitFormConfiguration : IEntityTypeConfiguration<EnexExitForm>
	{
		public void Configure(EntityTypeBuilder<EnexExitForm> builder)
		{
			builder.HasOne(x => x.File).WithMany().HasForeignKey(x => x.FileId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.OrganizationUnit).WithMany().HasForeignKey(x => x.OrganizationUnitId).OnDelete(DeleteBehavior.Restrict);
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Hrm_EnexExitForm_HtsId");
			builder.HasIndex(x => x.FormNumber).HasDatabaseName("IX_Hrm_EnexExitForm_FormNumber");
		}
	}
}
