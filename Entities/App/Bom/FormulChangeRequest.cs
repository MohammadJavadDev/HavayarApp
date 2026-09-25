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

		[DisplayName("نام های گروه محصول")]
		[DisplayInfo(null, true, type: SystemType.ListString)]

		public string? ProductGroupNames { get; set; }

		[DisplayName("شناسه های گروه محصول")]
		[DisplayInfo(null, true, type: SystemType.ListLong)]
		public string? ProductGroupIds { get; set; }

		[DisplayName("نام های محصول")]
		[DisplayInfo(null, true, type: SystemType.ListString)]
		public string? ProductFormulNames { get; set; }

		[DisplayName("شناسه های محصول")]
		[DisplayInfo(null, true, type: SystemType.ListLong)]
		public string? ProductFormulIds { get; set; }

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


		[DisplayName("ضریب مصرف")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? UsingRate { get; set; }




		[DisplayName("نام های گروه محصول ویرایش شده")]
		[DisplayInfo(null, true, type: SystemType.ListString)]

		public string? ChangedProductGroupNames { get; set; }

		[DisplayName("شناسه های گروه محصول ویرایش شده")]
		[DisplayInfo(null, true, type: SystemType.ListLong)]
		public string? ChangedProductGroupIds { get; set; }

		[DisplayName("نام های محصول ویرایش شده")]
		[DisplayInfo(null, true, type: SystemType.ListString)]
		public string? ChangedProductFormulNames { get; set; }

		[DisplayName("شناسه های محصول ویرایش شده")]
		[DisplayInfo(null, true, type: SystemType.ListLong)]
		public string? ChangedProductFormulIds { get; set; }

		[DisplayName("نام های فرمول ویرایش شده")]
		[DisplayInfo(null, true, type: SystemType.ListString)]

		public string? ChangedFormulNames { get; set; }

		[DisplayName("شناسه های فرمول ویرایش شده")]
		[DisplayInfo(null, true, type: SystemType.ListLong)]
		public string? ChangedFormulIds { get; set; }

		[DisplayName("ضریب مصرف ویرایش شده")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? ChangedUsingRate { get; set; }



		[DisplayName("کامنت ها")]
		[DisplayInfo(null, false, type: SystemType.ListEntity)]

		public List<FormulChangeRequestComment> Comments { get; set; } = new();

	}

	[Display(Name = "کامنت های درخواست ویرایش اقلام فرمول")]
	[Table("FormulChangeRequestComment", Schema = "Bom")]
	public class FormulChangeRequestComment:BaseEntity
	{
		public long FormulChangeRequestId { get; set; }
		public FormulChangeRequest FormulChangeRequest { get; set; }
		 
		[DisplayName("وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public FormulChangeRequestStatusEnum? Status { get; set; }


		[DisplayName("پیوست")]
		[DisplayInfo(null, true, type: SystemType.File, fileTypes: ".rar,.zip,.pdf,.excel,.word")]
		public FileEntity? Attachment { get; set; }

		public long? AttachmentId { get; set; }


		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? Description { get; set; }

	}
}
