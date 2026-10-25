using Common.Attributes;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Trn
{
	[Display(Name = "دفترچه تلفن")]
	[Table("PhoneBook", Schema = "Trn")]
	public class PhoneBook : BaseEntity
	{
		public long CompanyId { get; set; }
		public virtual Company? Company { get; set; }

		[DisplayName("واحد سازمانی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? DepartmentTitle { get; set; }

		[DisplayName("شماره تماس")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? PhoneNumber { get; set; }

		[DisplayName("فکس")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? FaxNumber { get; set; }
	}
}
