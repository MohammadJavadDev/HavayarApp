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


		/// <summary>
		/// گرید تامین‌کننده (HTS: Gnr_ManCompany.Grade، nvarchar(50)) — D16 / Q5-a
		/// </summary>
		[DisplayName("گرید")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? Grade { get; set; }


		/// <summary>
		/// خدمات/محصولات (HTS: Gnr_ManCompany.ServicesAndProducts، nvarchar(256)) — D16 / Q5-a
		/// </summary>
		[DisplayName("خدمات/محصولات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? ServicesAndProducts { get; set; }


		/// <summary>
		/// شخص مرتبط (HTS: Gnr_ManCompany.RelatedPersonName، nvarchar(128)) — D16 / Q5-a
		/// </summary>
		[DisplayName("شخص مرتبط")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(128)]
		public string? RelatedPersonName { get; set; }


		/// <summary>
		/// آدرس کامل تامین‌کننده (HTS: Gnr_ManCompany.Address، nvarchar(1024)).
		/// روی Supplier مانده تا Gnr.Party.Address (مشتری/عمومی) آلوده نشود — D16 / Q5-a
		/// </summary>
		[DisplayName("آدرس کامل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2000)]
		public string? Address { get; set; }


	}
}
