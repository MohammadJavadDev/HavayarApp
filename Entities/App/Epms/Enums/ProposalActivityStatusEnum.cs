using System.ComponentModel.DataAnnotations;

namespace Entities.App.Epms.Enums
{
	public enum ProposalActivityStatusEnum
	{
		[Display(Name = "ثبت اولیه")]
		Issue = 1,
		[Display(Name = "کامنت")]
		Commented = 2,
		[Display(Name = "عدم تایید")]
		Reject = 3,
		[Display(Name = "تایید شده")]
		Approve = 4,

	}
}