using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Srv
{
	/// <summary>کامنت درخواست لوازم‌التحریر — معادل ردیف‌های <c>Srv_RequestComment</c> با <c>StationeryRequestId</c>.</summary>
	[Display(Name = "کامنت درخواست لوازم التحریر")]
	[Table("StationeryRequestComment", Schema = "Srv")]
	public class StationeryRequestComment : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("درخواست لوازم التحریر")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(StationeryRequestId))]
		public virtual StationeryRequest? StationeryRequest { get; set; }
		public long StationeryRequestId { get; set; }

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

	public class StationeryRequestCommentConfiguration : IEntityTypeConfiguration<StationeryRequestComment>
	{
		public void Configure(EntityTypeBuilder<StationeryRequestComment> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Srv_StationeryRequestComment_HtsId");

			builder.HasOne(x => x.StationeryRequest)
				.WithMany(x => x.Comments)
				.HasForeignKey(x => x.StationeryRequestId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}
}
