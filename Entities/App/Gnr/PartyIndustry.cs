using Common.Attributes;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Gnr
{
	[Display(Name = "صنعت شخص/شرکت")]
	[Table("PartyIndustry", Schema = "Gnr")]
	public class PartyIndustry : BaseEntity
	{
		[DisplayName("عنوان")]
		[DisplayInfo("Title", true, type: SystemType.String, required: true, showInRelationData: true, regexInvalidError: "کاراکتر وارد شده غیر مجاز میباشد. ")]
		public string Title { get; set; }


		[DisplayName("کد")]
		[DisplayInfo("Code", true, type: SystemType.Long, regexInvalidError: "کاراکتر وارد شده غیر مجاز میباشد. ")]
		public long? Code { get; set; }
		public long? HamkaranId { get; set; }


		[DisplayName("زیر صنعت شخص/شرکت")]
		[DisplayInfo(null, true, type: SystemType.ListEntity)]
		public List<PartyIndustryItem> PartyIndustryItem { get; set; } = new();


	}

	[Display(Name = "زیر صنعت شخص/شرکت")]
	[Table("PartyIndustryItem", Schema = "Gnr")]
	public class PartyIndustryItem : BaseEntity
	{
		public long PartyIndustryId { get; set; }
		public PartyIndustry PartyIndustry { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(500)]
		public string Title { get; set; }


		[DisplayName("کد")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? Code { get; set; }
		public long? HamkaranId { get; set; }


	}

}
