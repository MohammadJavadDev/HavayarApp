using Common.Attributes;
using Entities.App.Edms.Enums;
using Entities.Auth;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Edms
{
	[Display(Name = "Vpis پروژه")]
	[Table("ProjectVpis", Schema = "Edms")]
	public class ProjectVpis : BaseEntity
	{
		[DisplayName("پروژه")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		public Project? ProjectName { get; set; }

		public long? ProjectNameId { get; set; }


		[DisplayName("کد")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(200)]
		public string? Code { get; set; }


		[DisplayName("عنوان")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(500)]
		public string? Title { get; set; }


		[DisplayName("وزن")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int? Weight { get; set; }


		[DisplayName("نوع")]
		[DisplayInfo(null, false, type: SystemType.Select)]
		public ProjectVpisDocTypeEnum? Type { get; set; }


		[DisplayName("کلاس مدرک")]
		[DisplayInfo(null, false, type: SystemType.Select)]
		public ProjectVpisClassDocumentEnum? ClassDocument { get; set; }


		[DisplayName("دیسپلین")]
		[DisplayInfo(null, false, type: SystemType.Select)]
		public ProjectVpisDisplayingEnum? Displaying { get; set; }


		[DisplayName("سایز صفحه")]
		[DisplayInfo(null, false, type: SystemType.Select)]
		public ProjectVpisPageSizeEnum? PageSize { get; set; }


		[DisplayName("تاریخ مبنای اولین مدرک (میلادی)")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? FirstDegreeDocumentMiladiDate { get; set; }


		[DisplayName("تاریخ مبنای اولین مدرک (شمسی)")]
		[DisplayInfo(null, false, type: SystemType.DateShamsi)]
		public string? FirstDegreeDocumentShamsiDate { get; set; }


		[DisplayName("تاریخ برنامه جایگزین")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? ReplaceMiladiDate { get; set; }


		[DisplayName("تاریخ برنامه جایگزین شمسی")]
		[DisplayInfo(null, false, type: SystemType.DateShamsi)]
		public string? ReplaceShamsiDate { get; set; }


		[DisplayName("نفر ساعت برای ریویژن 0")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int? PersonHourRevisionZero { get; set; }


		[DisplayName("نفر ساعت برای ریویژن 1")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int? PersonHourRevisionOne { get; set; }


		[DisplayName("نفر ساعت برای ریویژن 2")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int? PersonHourRevisionTwo { get; set; }


		[DisplayName("نفر ساعت برای ریویژن 3")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int? PersonHourRevisionThree { get; set; }


		[DisplayName("بررسی کننده")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		public User? Reviewer { get; set; }

		public long? ReviewerId { get; set; }


		[DisplayName("تایید کننده")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		public User? Approver { get; set; }

		public long? ApproverId { get; set; }


		[DisplayName("توضیحات")]
		[DisplayInfo(null, false, type: SystemType.String)]
		public string? Description { get; set; }


		[DisplayName("ویژگی جدید")]
		[DisplayInfo(null, false, type: SystemType.String)]
		public string? NewProperty { get; set; }


	}
}
