using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Srv
{
	/// <summary>کامنت درخواست خودرو — معادل ردیف‌های <c>Srv_RequestComment</c> با <c>CarRequestId</c>.</summary>
	[Display(Name = "کامنت درخواست خودرو")]
	[Table("CarRequestComment", Schema = "Srv")]
	public class CarRequestComment : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("درخواست خودرو")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(CarRequestId))]
		public virtual CarRequest? CarRequest { get; set; }
		public long CarRequestId { get; set; }

		[DisplayName("کامنت")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(256)]
		public string Comment { get; set; } = "";

		[DisplayName("وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? StatusId { get; set; }

		[DisplayName("تاریخ تقریبی اجرا")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(20)]
		public string? ApproximateExecutionDate { get; set; }

		[DisplayName("تاریخ ایجاد")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(20)]
		public string? CreatedDateInText { get; set; }
	}

	public class CarRequestCommentConfiguration : IEntityTypeConfiguration<CarRequestComment>
	{
		public void Configure(EntityTypeBuilder<CarRequestComment> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Srv_CarRequestComment_HtsId");

			builder.HasOne(x => x.CarRequest)
				.WithMany(x => x.Comments)
				.HasForeignKey(x => x.CarRequestId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}
}
