using Common.Attributes;
using Entities.App.Edms.Enums;
 using Entities.App.Hrm;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Edms
{
	[Display(Name = "فعالیت های پروژه")]
	[Table("ProjectActivity", Schema = "Edms")]
	public class ProjectActivity : BaseEntity
	{
		[DisplayName("پروژه")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		public Project? Project { get; set; }

		public long? ProjectId { get; set; }


		[DisplayName("نام پروژه")]
		[DisplayInfo("ProjectName", true, type: SystemType.String, showInRelationData: true, regexInvalidError: "کاراکتر وارد شده غیر مجاز میباشد. ")]
		public string? ProjectName { get; set; }


		[DisplayName("واحد ذینفع")]
		[DisplayInfo("BeneficiaryUnit.Title", true, type: SystemType.Entity, regexInvalidError: "کاراکتر وارد شده غیر مجاز میباشد. ")]
		public OrgUnit? BeneficiaryUnit { get; set; }

	 


		[DisplayName("شناسه واحد ذینفع")]
		[DisplayInfo("BeneficiaryUnitId", true, type: SystemType.Long, regexInvalidError: "کاراکتر وارد شده غیر مجاز میباشد. ")]
		public long? BeneficiaryUnitId { get; set; }


		[DisplayName("نوع فعالیت")]
		[DisplayInfo("Type", true, type: SystemType.Select, regexInvalidError: "کاراکتر وارد شده غیر مجاز میباشد. ")]
		public ProjectActivityTypeEnum? Type { get; set; }


		[DisplayName("کامنت")]
		[DisplayInfo("Comment", true, type: SystemType.String, regexInvalidError: "کاراکتر وارد شده غیر مجاز میباشد. ")]
		public string? Comment { get; set; }


		[DisplayName("تاریخ میلادی فعالیت")]
		[DisplayInfo("MiladiDate", true, type: SystemType.Date, regexInvalidError: "کاراکتر وارد شده غیر مجاز میباشد. ")]
		public DateTime? MiladiDate { get; set; }


		[DisplayName("تاریخ شمسی فعالیت")]
		[DisplayInfo("ShamsiDate", true, type: SystemType.DateShamsi, regexInvalidError: "کاراکتر وارد شده غیر مجاز میباشد. ")]
		public string? ShamsiDate { get; set; }


		[DisplayName("طول/مدت (دقیقه)")]
		[DisplayInfo("Duration", true, type: SystemType.Long, regexInvalidError: "کاراکتر وارد شده غیر مجاز میباشد. ")]
		public long? Duration { get; set; }

		/// <summary>
		/// شناسه فعالیت در سیستم قدیم (TotalSystem / HTS) برای همگام‌سازی یکتا
		/// </summary>
		public long HtsId { get; set; }
	}

	[Display(Name = "کامنت فعالیت های پروژه")]
	[Table("ProjectActivityComment", Schema = "Edms")]
	public class ProjectActivityComment : BaseEntity
	{
		[DisplayName("فعالیت پروژه")]
		[DisplayInfo(null, false, type: SystemType.Entity, required: true)]
		public ProjectActivity ProjectActivity { get; set; }

		public long ProjectActivityId { get; set; }

		[DisplayName("وضعیت")]
		[DisplayInfo(null, false, type: SystemType.Select)]
		public DocumentStatusEnums Status { get; set; }

		[DisplayName("کامنت")]
		[DisplayInfo(null, false, type: SystemType.String)]
		public string? Comment { get; set; }

		/// <summary>
		/// شناسه کامنت در سیستم قدیم (TotalSystem / HTS) برای همگام‌سازی یکتا
		/// </summary>
		public long HtsId { get; set; }
	}
}
