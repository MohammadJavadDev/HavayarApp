using Common.Attributes;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Gnr
{
	[Display(Name = "تامین کننده")]
	[Table("Supplier", Schema = "Gnr")]
	public class Supplier : BaseEntity
	{
		[DisplayName("شخص شرکت")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public Party Party { get; set; }

		public long PartyId { get; set; }


		[DisplayName("پیشوند")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(300)]
		public string? Prefix { get; set; }


		[DisplayName("کد پستی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(200)]
		public string? PostalCode { get; set; }


		[DisplayName("شماره تماس")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(200)]
		public string? PhoneNumber { get; set; }


		[DisplayName("فکس")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(200)]
		public string? Fax { get; set; }


		[DisplayName("ایمیل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(200)]
		public string? Email { get; set; }


		[DisplayName("کد تفصیل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(200)]
		public string? DlCode { get; set; }


	}
}
