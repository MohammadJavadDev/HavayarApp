using Common.Attributes;
using Entities.App.Bom.Enums;
using Entities.App.Inv;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Bom
{
	[Display(Name = "درخواست ویرایش اقلام فرمول")]
	[Table("FormulChangeRequest", Schema = "Bom")]
	public class FormulChangeRequest : BaseEntity
	{
		[DisplayName("نوع عملیات")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public FormulChangeRequestOperationTypeEnum? OperationType { get; set; }


		[DisplayName("قطعه")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Part? Part { get; set; }

		public long? PartId { get; set; }

		[DisplayName("گروه محصول")]
		[DisplayInfo(null, true,required:true, type: SystemType.Entity)]

		public ProductGroup ProductGroup { get; set; }
		public long ProductGroupId { get; set; }
		 

		[DisplayName("قطعه جایگزین")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Part? ReplacementPart { get; set; }

		public long? ReplacementPartId { get; set; } 

		[DisplayName("توضیحات تغییرات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? ChangeDescription { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? Description { get; set; }


		[DisplayName("پیوست")]
		[DisplayInfo(null, true, type: SystemType.File, fileTypes: ".rar,.zip,.pdf,.excel,.word")]
		public FileEntity? Attachment { get; set; }

		public long? AttachmentId { get; set; }
		 


		[DisplayName("وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public FormulChangeRequestStatusEnum? Status { get; set; }

		 


	}
}
