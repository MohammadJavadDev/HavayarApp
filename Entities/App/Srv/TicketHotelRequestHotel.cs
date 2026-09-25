using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Srv
{
	/// <summary>هتل درخواست بلیط و هتل — معادل HTS <c>Srv_MissionHotel</c>.</summary>
	[Display(Name = "هتل درخواست بلیط و هتل")]
	[Table("TicketHotelRequestHotel", Schema = "Srv")]
	public class TicketHotelRequestHotel : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("درخواست بلیط و هتل")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(TicketHotelRequestId))]
		public virtual TicketHotelRequest? TicketHotelRequest { get; set; }
		public long TicketHotelRequestId { get; set; }

		[DisplayName("عنوان هتل")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(64)]
		public string? HotelTitle { get; set; }

		[DisplayName("تعداد ستاره")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? StarsNumber { get; set; }

		[DisplayName("مبلغ هتل")]
		[DisplayInfo(null, true, type: SystemType.Int, required: true)]
		public int HotelAmount { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? Comment { get; set; }

		[DisplayName("تاریخ ایجاد متنی")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(16)]
		public string? CreatedDateInText { get; set; }
	}

	public class TicketHotelRequestHotelConfiguration : IEntityTypeConfiguration<TicketHotelRequestHotel>
	{
		public void Configure(EntityTypeBuilder<TicketHotelRequestHotel> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Srv_TicketHotelRequestHotel_HtsId");

			builder.HasOne(x => x.TicketHotelRequest)
				.WithMany(x => x.Hotels)
				.HasForeignKey(x => x.TicketHotelRequestId)
				.OnDelete(DeleteBehavior.NoAction);
		}
	}
}
