using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Hts.Edms
{
	[Table("Edms_Project_Vpis")]
	public class Hts_Edms_Project_Vpis
	{
		[Key]
		public int Project_Vpis_ID { get; set; }

		public int Project_FK { get; set; }

		[StringLength(256)]
		public string? Document_Number { get; set; }

		[StringLength(1024)]
		public string? Document_Title { get; set; }

		public decimal? Document_Weight { get; set; }

		public byte? DocType_FK { get; set; }

		public short? DocClass_FK { get; set; }

		public short? DocDisipline_FK { get; set; }

		public byte? PageSize_FK { get; set; }

		public short? CheckedUser_FK { get; set; }

		public short? ApprovedUser_FK { get; set; }

		public DateTime? First_Issue_Baseline { get; set; }

		[StringLength(10)]
		public string? First_Issue_BaselineInText { get; set; }

		public DateTime? First_Issue_Plan { get; set; }

		[StringLength(10)]
		public string? First_Issue_PlanInText { get; set; }

		public int? Rev0_ManHour { get; set; }

		public int? Rev1_ManHour { get; set; }

		public byte? RevN_ManHourPercent { get; set; }

		public int? RevN_ManHour { get; set; }

		[StringLength(4000)]
		public string? Description { get; set; }
	}
}
