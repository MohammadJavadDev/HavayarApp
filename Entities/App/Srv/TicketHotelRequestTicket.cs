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
	/// <summary>بلیط درخواست بلیط و هتل — معادل HTS <c>Srv_MissionTicket</c>.</summary>
	[Display(Name = "بلیط درخواست بلیط و هتل")]
	[Table("TicketHotelRequestTicket", Schema = "Srv")]
	public class TicketHotelRequestTicket : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("درخواست بلیط و هتل")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(TicketHotelRequestId))]
		public virtual TicketHotelRequest? TicketHotelRequest { get; set; }
		public long TicketHotelRequestId { get; set; }

		[DisplayName("نوع بلیط")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public TicketTypeEnum TicketTypeId { get; set; }

		[DisplayName("شرکت هواپیمایی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(64)]
		public string? Airline { get; set; }

		[DisplayName("ترمینال مسافربری")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(64)]
		public string? PassengerTerminal { get; set; }

		[DisplayName("ایستگاه قطار")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(64)]
		public string? RailwayStation { get; set; }

		[DisplayName("آژانس مسافرتی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(64)]
		public string? TravelAgency { get; set; }

		[DisplayName("تاریخ صدور")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(10)]
		public string? IssueDate { get; set; }

		[DisplayName("شماره بلیط")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(32)]
		public string? TicketNumber { get; set; }

		[DisplayName("مبلغ بلیط")]
		[DisplayInfo(null, true, type: SystemType.Int, required: true)]
		public int TicketAmount { get; set; }

		[DisplayName("مبدا")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(50)]
		public string? Source { get; set; }

		[DisplayName("مقصد")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(50)]
		public string? Destination { get; set; }

		/// <summary>نام ستون HTS: <c>DeparturDate</c> (املای قدیم حفظ شده).</summary>
		[DisplayName("تاریخ حرکت")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(10)]
		public string? DeparturDate { get; set; }

		/// <summary>نام ستون HTS: <c>DeparturTime</c> (املای قدیم حفظ شده).</summary>
		[DisplayName("ساعت حرکت")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(5)]
		public string? DeparturTime { get; set; }

		[DisplayName("کنسل شده")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? IsCanceled { get; set; }

		[DisplayName("تاریخ کنسل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(16)]
		public string? CancelDateInText { get; set; }

		[DisplayName("علت کنسل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? CancelReason { get; set; }

		[DisplayName("پرداخت توسط متقاضی")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? IsPaidByApplicant { get; set; }

		[DisplayName("پرداخت توسط امور مالی")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? IsPaidByFinancialDepartment { get; set; }

		[DisplayName("تاریخ ایجاد متنی")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(16)]
		public string? CreatedDateInText { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? Comment { get; set; }
	}

	public class TicketHotelRequestTicketConfiguration : IEntityTypeConfiguration<TicketHotelRequestTicket>
	{
		public void Configure(EntityTypeBuilder<TicketHotelRequestTicket> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Srv_TicketHotelRequestTicket_HtsId");

			builder.HasOne(x => x.TicketHotelRequest)
				.WithMany(x => x.Tickets)
				.HasForeignKey(x => x.TicketHotelRequestId)
				.OnDelete(DeleteBehavior.NoAction);
		}
	}
}
