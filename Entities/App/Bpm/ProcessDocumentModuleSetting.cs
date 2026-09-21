using Common.Attributes;
using Common.Utilities;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Bpm
{
	[Display(Name = "تنظیمات اسناد فرآیندی")]
	[Table("ProcessDocumentModuleSetting", Schema = "Bpm")]
	public class ProcessDocumentModuleSetting : BaseEntity
	{
		[DisplayName("آخرین شماره ابلاغیه")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int LastAnnouncementNumber { get; set; }

		[DisplayName("رونوشت ثابت ابلاغ")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(2000)]
		public string? AnnouncementAlwaysCc { get; set; }

		[DisplayName("رونوشت شرطی ابلاغ")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(2000)]
		public string? AnnouncementConditionalCcJson { get; set; }

		[DisplayName("مسیر ریشه فایل‌ها")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(500)]
		public string? ProcessFilesRoot { get; set; }

		[DisplayName("پوشه فایل موقت")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(500)]
		public string? ProcessFilesTempFolder { get; set; }

		[DisplayName("پوشه تاریخچه فایل")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(500)]
		public string? ProcessFilesHistoryFolder { get; set; }
	}

	public class ProcessDocumentModuleSettingConfiguration : IEntityTypeConfiguration<ProcessDocumentModuleSetting>
	{
		public void Configure(EntityTypeBuilder<ProcessDocumentModuleSetting> builder)
		{
			var now = DateTime.Parse("2026-09-14 12:00:00");

			builder.HasData(new ProcessDocumentModuleSetting
			{
				Id = 1,
				LastAnnouncementNumber = 446,
				AnnouncementAlwaysCc = "mirlohi.m@havayar.com",
				AnnouncementConditionalCcJson = "[{\"whenToContains\":\"sajdeh.n@\",\"ccEmails\":[\"Yaghyaei.m@havayar.com\"]},{\"whenToContains\":\"momenirad.s@\",\"ccOrgUnitId\":268}]",
				ProcessFilesRoot = @"\\172.20.40.27\Uploads\Bpm\DMS\",
				ProcessFilesTempFolder = "1_Temp",
				ProcessFilesHistoryFolder = "2_History",
				CreatedById = 1,
				CreatedByName = "admin",
				ModifiedById = 1,
				ModifiedByName = "admin",
				CreatedOnShamsiDateTime = now.ToShamsiDateTime(),
				ModifiedDateShamsiDateTime = now.ToShamsiDateTime(),
				CreatedOnMiladiDateTime = now,
				ModifiedDateMiladiDateTime = now,
				IsActive = IsActiveEnum.Active
			});
		}
	}
}
