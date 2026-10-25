using Common.Attributes;
using Entities.App.Srv.Enums;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Srv
{
	/// <summary>درخواست لوازم‌التحریر — معادل HTS <c>Srv_StationeryRequest</c>.</summary>
	[Display(Name = "درخواست لوازم التحریر")]
	[Table("StationeryRequest", Schema = "Srv")]
	public class StationeryRequest : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("نوع لوازم")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public StationeryTypeEnum StationeryTypeId { get; set; }

		[DisplayName("تعداد")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? Count { get; set; }

		[DisplayName("متن درخواست")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? RequestDescription { get; set; }

		[DisplayName("کامنت آخر")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? LastComment { get; set; }

		/// <summary>
		/// وضعیت عددی HTS (lookup 161). برای برچسب‌های شناخته‌شده از مقادیر ثابت استفاده کنید
		/// ولی این ستون را به enum نگاشت نکنید.
		/// </summary>
		[DisplayName("وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Int, required: true)]
		public short StatusId { get; set; }

		[DisplayName("زمان انجام")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(20)]
		public string? DoneDateTimeInText { get; set; }

		[DisplayName("تاریخ ایجاد")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(20)]
		public string? CreatedDateInText { get; set; }

		public virtual ICollection<StationeryRequestComment>? Comments { get; set; }
		public virtual ICollection<StationeryRequestAttachment>? Attachments { get; set; }
	}

	public class StationeryRequestConfiguration : IEntityTypeConfiguration<StationeryRequest>
	{
		public void Configure(EntityTypeBuilder<StationeryRequest> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Srv_StationeryRequest_HtsId");
		}
	}
}
