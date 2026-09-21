using Common.Attributes;
using Entities.App.Bpm.Enums;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Bpm
{
	[Display(Name = "گردش سند فرآیندی")]
	[Table("ProcessDocumentStatusLog", Schema = "Bpm")]
	public class ProcessDocumentStatusLog : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		public long ProcessDocumentId { get; set; }
		public virtual ProcessDocument? ProcessDocument { get; set; }

		[DisplayName("وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public ProcessDocumentStatusEnum Status { get; set; }

		[DisplayName("تاریخ ارسال")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? SendMiladiDate { get; set; }

		[DisplayName("تاریخ ارسال")]
		[DisplayInfo("SendMiladiDate", true, type: SystemType.DateShamsi)]
		[MaxLength(30)]
		public string? SendShamsiDate { get; set; }

		[DisplayName("تاریخ دریافت")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? ReceiveMiladiDate { get; set; }

		[DisplayName("تاریخ دریافت")]
		[DisplayInfo("ReceiveMiladiDate", true, type: SystemType.DateShamsi)]
		[MaxLength(30)]
		public string? ReceiveShamsiDate { get; set; }

		[DisplayName("توضیح")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Comment { get; set; }
	}

	public class ProcessDocumentStatusLogConfiguration : IEntityTypeConfiguration<ProcessDocumentStatusLog>
	{
		public void Configure(EntityTypeBuilder<ProcessDocumentStatusLog> builder)
		{
			builder.HasIndex(x => x.HtsId)
				.IsUnique()
				.HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Bpm_ProcessDocumentStatusLog_HtsId");
		}
	}
}
