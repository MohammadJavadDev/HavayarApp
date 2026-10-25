using Common.Attributes;
using Entities.App.Gnr;
using Entities.App.Hcm;
using Entities.App.Hrm.Enums;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Hrm
{
	/// <summary>ورود و خروج کالا (حراست/دژبان) — معادل HTS <c>HRM_Guard_PartIO</c>.</summary>
	[Display(Name = "ورود و خروج کالا (حراست/دژبان)")]
	[Table("GuardPartIo", Schema = "Hrm")]
	public class GuardPartIo : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("نوع ورود/خروج")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public GuardIoTypeEnum IoType { get; set; }

		[DisplayName("درب")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public GuardIoDoorEnum IoDoor { get; set; }

		[DisplayName("نوع اپراتور")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public GuardOperatorTypeEnum OperatorType { get; set; }

		[DisplayName("پرسنل")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(PersonId))]
		public virtual Personel? Person { get; set; }
		public long? PersonId { get; set; }

		[DisplayName("پیمانکار")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(ContractorId))]
		public virtual GuardContractor? Contractor { get; set; }
		public long? ContractorId { get; set; }

		[DisplayName("عنوان اپراتور")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(128)]
		public string? OperatorTitle { get; set; }

		[DisplayName("راننده")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(DriverId))]
		public virtual GuardDriver? Driver { get; set; }
		public long? DriverId { get; set; }

		[DisplayName("مبدأ")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(IoSourceId))]
		public virtual GuardLocation? IoSource { get; set; }
		public long? IoSourceId { get; set; }

		[DisplayName("مقصد")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(IoDestinationId))]
		public virtual GuardLocation? IoDestination { get; set; }
		public long? IoDestinationId { get; set; }

		[DisplayName("اعلام ورود قطعه")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		[ForeignKey(nameof(PartEntryNotifyId))]
		public virtual PartEntryNotify? PartEntryNotify { get; set; }
		public long? PartEntryNotifyId { get; set; }

		[DisplayName("تایید شده")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsConfirmed { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Comment { get; set; }

		/// <summary>اقلام فرم — فقط همراه ذخیرهٔ هدر نوشته می‌شوند.</summary>
		[NotMapped]
		[DisplayInfo(null, false, type: SystemType.ListEntity)]
		public List<GuardPartIoDetail>? Details { get; set; }
	}

	public class GuardPartIoConfiguration : IEntityTypeConfiguration<GuardPartIo>
	{
		public void Configure(EntityTypeBuilder<GuardPartIo> builder)
		{
			builder.HasOne(x => x.Person).WithMany().HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.Contractor).WithMany().HasForeignKey(x => x.ContractorId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.Driver).WithMany().HasForeignKey(x => x.DriverId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.IoSource).WithMany().HasForeignKey(x => x.IoSourceId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.IoDestination).WithMany().HasForeignKey(x => x.IoDestinationId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.PartEntryNotify).WithMany().HasForeignKey(x => x.PartEntryNotifyId).OnDelete(DeleteBehavior.Restrict);

			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Hrm_GuardPartIo_HtsId");
		}
	}
}
