using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Hrm
{
	[Display(Name = "جذب‌شده درخواست نیرو")]
	[Table("StaffRequestStaffer", Schema = "Hrm")]
	public class StaffRequestStaffer : BaseEntity
	{
		public long HtsId { get; set; }

		[ForeignKey(nameof(StaffRequestId))]
		public virtual StaffRequest? StaffRequest { get; set; }

		public long StaffRequestId { get; set; }

		[DisplayName("نام")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(64)]
		public string? StafferName { get; set; }

		[DisplayName("توضیح")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? StafferDescription { get; set; }

		[DisplayName("شناسه وضعیت جذب HTS")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int? HtsHiringStateId { get; set; }

		[DisplayName("تاریخ جذب")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(20)]
		public string? HiringDateInText { get; set; }

		[DisplayName("تاریخ تکمیل")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(20)]
		public string? FillingDateInText { get; set; }
	}

	public class StaffRequestStafferConfiguration : IEntityTypeConfiguration<StaffRequestStaffer>
	{
		public void Configure(EntityTypeBuilder<StaffRequestStaffer> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)").HasDatabaseName("IX_Hrm_StaffRequestStaffer_HtsId");
			builder.HasOne(x => x.StaffRequest).WithMany(x => x.Staffers).HasForeignKey(x => x.StaffRequestId).OnDelete(DeleteBehavior.Cascade);
		}
	}
}
