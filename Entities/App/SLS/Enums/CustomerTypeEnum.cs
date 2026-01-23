using System.ComponentModel.DataAnnotations;

namespace Entities.App.SLS.Enums
{
	public enum CustomerTypeEnum
	{
		[Display(Name = "هوای فشرده")]
		CompressedAir = 20,
		[Display(Name = "سی ان جی")]
		Cng = 21,
		[Display(Name = "متفرقه- هوای فشرده")]
		MiscellaneousCompressedAir = 24,
		[Display(Name = "کمپرسورهای فرآیندی و پروژه های خاص")]
		ProcessCompressorsAndSpecialProjects = 26,
	}
}
