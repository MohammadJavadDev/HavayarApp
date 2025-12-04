using System.ComponentModel.DataAnnotations;

namespace Entities.App.Epms.Enums
{
	public enum ProposalRequestEnum
	{
		[Display(Name = "تکنیکال")]
		Technical =247,
		[Display(Name = "تجاری")]
		Commercia =248,
		[Display(Name = "تکنیکال و تجاری")]
		TechnicalAndCommercial =249
	}
}