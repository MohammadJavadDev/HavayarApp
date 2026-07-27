using Common.Attributes;
using Entities.App.Bom;
using Entities.App.Edms;
using Entities.App.Hcm;
using Entities.App.Inq.Enums;
using Entities.App.Inv;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Entities.App.Inq
{

	[Display(Name = "استعلام فنی و مالی")]
	[Table("TechnicalInquiry", Schema = "Inq")]
	public class TechnicalInquiry : BaseEntity
	{
		[DisplayName("عنوان پروژه")]
		[DisplayInfo(null, true, SystemType.String)]
		[MaxLength(500)]
		public string? ProjectTitle { get; set; }

		[DisplayName("پروژه")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Project? Project { get; set; }
		public long? ProjectId { get; set; }

		[DisplayName("مدیر پروژه")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(ProjectManagerId))]
		public Personel? ProjectManager { get; set; }
		public long? ProjectManagerId { get; set; }

		[DisplayName("مربوطهPSL")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Personel? RelevantPSL { get; set; }
		public long? RelevantPSLId { get; set; }

		[DisplayName("کالا")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Part? Part { get; set; }
		public long? PartId { get; set; }

		[DisplayName("تاریخ نیاز میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? NeedMiladiDate { get; set; } = null;

		[DisplayName("تاریخ نیاز شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? NeedShamsiDate { get; set; } = null;

		[DisplayName("آخرین وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public TechnicalInquiryStatusEnum LastStatus { get; set; }

		[DisplayName("عنوان فایل DataSheet")]
		[DisplayInfo(null, true, type: SystemType.File, fileTypes: ".jpg,.png")]
		public FileEntity? DatasheetFile { get; set; }
		public long? DatasheetFileId { get; set; }

		[DisplayName("عنوان فایل MAP")]
		[DisplayInfo(null, true, type: SystemType.File, fileTypes: ".jpg,.png")]
		public FileEntity? MapFile { get; set; }
		public long? MapFileId { get; set; }

		[DisplayName("عنوان فایل SPEC")]
		[DisplayInfo(null, true, type: SystemType.File, fileTypes: ".jpg,.png")]
		public FileEntity? SpecFile { get; set; }
		public long? SpecFileId { get; set; }

		[DisplayName("عنوان فایل TC")]
		[DisplayInfo(null, true, type: SystemType.File, fileTypes: ".jpg,.png")]
		public FileEntity? TcFile { get; set; }
		public long? TcFileId { get; set; }

		[DisplayName("عنوان فایل متفرقه")]
		[DisplayInfo(null, true, type: SystemType.File, fileTypes: ".jpg,.png")]
		public FileEntity? OtherFile { get; set; }
		public long? OtherFileId { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(4000)]
		public string? Comment { get; set; }

		[DisplayName("نوع استعلام")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public QueryTypeEnum QueryType { get; set; }

		[DisplayName("توضیحات پیمانکار")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(400)]
		public string? DescriptionContractor { get; set; }


		[DisplayName("کامنت ها")]
		[DisplayInfo(null, false, type: SystemType.ListEntity)]
		public virtual ICollection<TechnicalInquiryComment> TechnicalInquiryHistory { get; set; }

	}
	[Display(Name = "توضیحات استعلام فنی ")]
	[Table("TechnicalInquiryComment", Schema = "Inq")]
	public class TechnicalInquiryComment : BaseEntity
	{


		[DisplayName("استعلام فنی و مالی")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public TechnicalInquiry? TechnicalInquiry { get; set; }
		public long? TechnicalInquiryId { get; set; }


		[DisplayName("آخرین وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public TechnicalInquiryStatusEnum LastStatus { get; set; }


		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[StringLength(4000)]
		public string? Comment { get; set; }

		[DisplayName("نوع استعلام")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public QueryTypeEnum QueryType { get; set; }

	}

}
