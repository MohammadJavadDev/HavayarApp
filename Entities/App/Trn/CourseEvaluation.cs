using Common.Attributes;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Trn
{
	[Display(Name = "ارزیابی دوره")]
	[Table("CourseEvaluation", Schema = "Trn")]
	public class CourseEvaluation : BaseEntity
	{
		public long CourseId { get; set; }
		public virtual Course? Course { get; set; }

		// Respondent; optional to allow anonymous/aggregate rows.
		[DisplayName("ارزیاب (شرکت‌کننده)")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public long? ParticipantId { get; set; }
		public virtual Participant? Participant { get; set; }

		// 19 scored questions (legacy Q1..Q19, each 1–5). Individual columns — reports average per question.
		[DisplayName("سوال ۱")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? Q1 { get; set; }

		[DisplayName("سوال ۲")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? Q2 { get; set; }

		[DisplayName("سوال ۳")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? Q3 { get; set; }

		[DisplayName("سوال ۴")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? Q4 { get; set; }

		[DisplayName("سوال ۵")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? Q5 { get; set; }

		[DisplayName("سوال ۶")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? Q6 { get; set; }

		[DisplayName("سوال ۷")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? Q7 { get; set; }

		[DisplayName("سوال ۸")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? Q8 { get; set; }

		[DisplayName("سوال ۹")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? Q9 { get; set; }

		[DisplayName("سوال ۱۰")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? Q10 { get; set; }

		[DisplayName("سوال ۱۱")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? Q11 { get; set; }

		[DisplayName("سوال ۱۲")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? Q12 { get; set; }

		[DisplayName("سوال ۱۳")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? Q13 { get; set; }

		[DisplayName("سوال ۱۴")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? Q14 { get; set; }

		[DisplayName("سوال ۱۵")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? Q15 { get; set; }

		[DisplayName("سوال ۱۶")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? Q16 { get; set; }

		[DisplayName("سوال ۱۷")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? Q17 { get; set; }

		[DisplayName("سوال ۱۸")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? Q18 { get; set; }

		[DisplayName("سوال ۱۹")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? Q19 { get; set; }

		[DisplayName("میانگین امتیاز")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? AverageScore { get; set; }  // computed in app layer (R6), read-only in UI

		[DisplayName("نظر / پیشنهاد")]
		[DisplayInfo(null, false, type: SystemType.String)]
		public string? Comment { get; set; }
	}
}
