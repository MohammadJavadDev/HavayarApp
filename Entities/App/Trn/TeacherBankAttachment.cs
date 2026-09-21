using Common.Attributes;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Trn
{
	[Display(Name = "پیوست مدرس")]
	[Table("TeacherBankAttachment", Schema = "Trn")]
	public class TeacherBankAttachment : BaseEntity
	{
		public long TeacherBankId { get; set; }
		public virtual TeacherBank? TeacherBank { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(256)]
		public string? Title { get; set; }

		[DisplayName("فایل")]
		[DisplayInfo(null, true, type: SystemType.File, required: true)]
		public long? FileId { get; set; }
		public virtual FileEntity? File { get; set; }
	}
}
