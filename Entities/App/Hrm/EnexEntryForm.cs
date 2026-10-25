using Common.Attributes;
using Entities.App.Hrm.Enums;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Hrm
{
	/// <summary>فرم ورود اضطراری — معادل HTS <c>Enex_EntryForm</c>.</summary>
	[Display(Name = "ورود اضطراری")]
	[Table("EnexEntryForm", Schema = "Hrm")]
	public class EnexEntryForm : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("شماره فرم")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int FormNumber { get; set; }

		[DisplayName("از فرم خروج")]
		[DisplayInfo(null, false, type: SystemType.Boolean)]
		public bool IsFromExitForm { get; set; }

		[DisplayName("فرم خروج")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		[ForeignKey(nameof(ExitFormId))]
		public virtual EnexExitForm? ExitForm { get; set; }
		public long? ExitFormId { get; set; }

		[DisplayName("عنوان واردکننده")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(128)]
		public string? ImporterTitle { get; set; }

		[DisplayName("شرکت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(128)]
		public string? CompanyTitle { get; set; }

		[DisplayName("آخرین وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public EnexEntryFormStatusEnum LastStatusId { get; set; }

		[DisplayName("واحد سازمانی")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(OrganizationUnitId))]
		public virtual OrgUnit? OrganizationUnit { get; set; }
		public long? OrganizationUnitId { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Comment { get; set; }

		[NotMapped]
		[DisplayInfo(null, false, type: SystemType.ListEntity)]
		public List<EnexFormPart>? Parts { get; set; }

		/// <summary>شناسه ردیف انتخاب‌شده هنگام جدید. ذخیره نمی‌شود؛ فقط ثابت می‌کند فرم از روی یک ردیف ساخته شده است.</summary>
		[NotMapped]
		public long? SourceEntryId { get; set; }
	}

	public class EnexEntryFormConfiguration : IEntityTypeConfiguration<EnexEntryForm>
	{
		public void Configure(EntityTypeBuilder<EnexEntryForm> builder)
		{
			builder.HasOne(x => x.ExitForm).WithMany().HasForeignKey(x => x.ExitFormId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.OrganizationUnit).WithMany().HasForeignKey(x => x.OrganizationUnitId).OnDelete(DeleteBehavior.Restrict);
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Hrm_EnexEntryForm_HtsId");
			builder.HasIndex(x => x.FormNumber).HasDatabaseName("IX_Hrm_EnexEntryForm_FormNumber");
		}
	}
}
