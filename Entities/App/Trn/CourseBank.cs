using Common.Attributes;
using Entities.App.Trn.Enums;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Trn
{
	[Display(Name = "بانک دوره‌ها")]
	[Table("CourseBank", Schema = "Trn")]
	public class CourseBank : BaseEntity
	{
		[DisplayName("زمینه دوره")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public CourseFieldEnum CourseField { get; set; }

		[DisplayName("عنوان زمینه دوره")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(256)]
		public string? CourseFieldTitle { get; set; }

		[DisplayName("عنوان زمینه دوره (لاتین)")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? CourseFieldTitleInLatin { get; set; }

		[DisplayName("نوع دوره")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public CourseBankTypeEnum? Type { get; set; }

		[DisplayName("مدت زمان (ساعت)")]
		[DisplayInfo(null, true, type: SystemType.Int, required: true)]
		public short Duration { get; set; }
	}
}
