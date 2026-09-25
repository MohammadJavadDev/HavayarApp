using Common.Attributes;
using Entities.App.Cng.Enums;
using Entities.App.SLS;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Cng
{
	/// <summary>اطلاعات مشتری/جایگاه CNG — معادل HTS <c>Cng_StationInfo</c>.</summary>
	[Display(Name = "اطلاعات مشتری/جایگاه")]
	[Table("StationInfo", Schema = "Cng")]
	public class StationInfo : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("مشتری")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual Customer? Customer { get; set; }
		public long? CustomerId { get; set; }

		[DisplayName("جایگاه")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(128)]
		public string StationTitle { get; set; } = "";

		[DisplayName("نوع تجهیز")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public StationEquipmentTypeEnum? EquipmentType { get; set; }

		[DisplayName("سریال کمپرسور")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? CompressorSerial { get; set; }

		[DisplayName("سریال درایر")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? DryerSerial { get; set; }

		[DisplayName("ماژول ذخیره سازی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? StorageModule { get; set; }

		[DisplayName("سریال دیسپنسر1")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? Dispenser1Serial { get; set; }

		[DisplayName("سریال دیسپنسر2")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? Dispenser2Serial { get; set; }

		[DisplayName("سریال دیسپنسر3")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? Dispenser3Serial { get; set; }

		[DisplayName("سریال دیسپنسر4")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? Dispenser4Serial { get; set; }

		[DisplayName("تاریخ میلادی راه اندازی")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? LaunchMiladiDate { get; set; }

		[DisplayName("تاریخ راه اندازی")]
		[MaxLength(10)]
		[DisplayInfo("LaunchMiladiDate", true, type: SystemType.DateShamsi)]
		public string? LaunchShamsiDate { get; set; }

		[DisplayName("تاریخ میلادی تحویل نهایی")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? PermanentDeliveryMiladiDate { get; set; }

		[DisplayName("تاریخ تحویل نهایی")]
		[MaxLength(10)]
		[DisplayInfo("PermanentDeliveryMiladiDate", true, type: SystemType.DateShamsi)]
		public string? PermanentDeliveryShamsiDate { get; set; }

		[DisplayName("عنوان و سمت نماینده")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(128)]
		public string? RepresentativeName { get; set; }

		[DisplayName("شماره تلفن نماینده")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? RepresentativeTellNumber { get; set; }

		[DisplayName("شماره موبایل نماینده")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? RepresentativeMobileNumber { get; set; }

		[DisplayName("آدرس ایمیل نماینده")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(128)]
		public string? RepresentativeEmailAddress { get; set; }

		[DisplayName("کامنت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Comment { get; set; }
	}

	public class StationInfoConfiguration : IEntityTypeConfiguration<StationInfo>
	{
		public void Configure(EntityTypeBuilder<StationInfo> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Cng_StationInfo_HtsId");

			builder.HasOne(x => x.Customer)
				.WithMany()
				.HasForeignKey(x => x.CustomerId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}
}
