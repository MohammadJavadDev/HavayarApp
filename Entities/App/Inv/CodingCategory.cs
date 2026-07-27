using Common.Attributes;
using Entities.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Entities.App.Inv
{

	[Display(Name = "دسته بندی کد کالا")]
	[Table("PartCodingCategory", Schema = "Inv")]
 
    public class PartCodingCategory : BaseEntity
	{

		[DisplayName("والد")]
		[DisplayInfo(null, true, type: SystemType.Entity)]

		public PartCodingCategory? Parent { get; set; }

		public long? ParentId { get; set; }

		[DisplayName("کد")]
		[DisplayInfo("Code", true, type: SystemType.String, regexInvalidError: "کاراکتر وارد شده غیر مجاز میباشد. ")]

		[StringLength(160)]
		public string Code { get; set; }


		[DisplayName("عنوان")]
		[DisplayInfo("Title", true, type: SystemType.String, regexInvalidError: "کاراکتر وارد شده غیر مجاز میباشد. ")]


		[StringLength(1024)]
		public string Title { get; set; }

		[DisplayName("خصوصیت ها")]
		[DisplayInfo("Atributies", true, type: SystemType.String, regexInvalidError: "کاراکتر وارد شده غیر مجاز میباشد. ")]


		[StringLength(4000)]
		public string? Atributies { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo("Description", true, type: SystemType.String, regexInvalidError: "کاراکتر وارد شده غیر مجاز میباشد. ")]


		[StringLength(4000)]
		public string? Description { get; set; }

		[DisplayName("درخواست شده")]
		[DisplayInfo("IsRequested", true, type: SystemType.Boolean)]


		public bool IsRequested { get; set; }
 
	}

	[Display(Name = "درخواست دسته بندی کد کالا")]
	[Table("PartCodingCategoryRequest", Schema = "Inv")]

	public class PartCodingCategoryRequest : BaseEntity
	{

		[DisplayName("دسته بندی کد کالا")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public PartCodingCategory PartCodingCategory { get; set; }
		public long PartCodingCategoryId { get; set; }

		[DisplayName("کد")]
		[DisplayInfo("Code", true, type: SystemType.String, regexInvalidError: "کاراکتر وارد شده غیر مجاز میباشد. ")]

		public string Code { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo("Title", true, type: SystemType.String, regexInvalidError: "کاراکتر وارد شده غیر مجاز میباشد. ")]



		[StringLength(1024)]
		public string Title { get; set; }
		[DisplayName("خصوصیت ها")]
		[DisplayInfo("Atributies", true, type: SystemType.String, regexInvalidError: "کاراکتر وارد شده غیر مجاز میباشد. ")]


		[StringLength(4000)]
		public string? Atributies { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo("Description", true, type: SystemType.String, regexInvalidError: "کاراکتر وارد شده غیر مجاز میباشد. ")]


		[StringLength(4000)]
		public string? Description { get; set; }

		[DisplayName("کالای ایجاد شده")]
		[DisplayInfo(null, true, type: SystemType.Entity)]

		public Part? CreatedPart { get; set; }
		public long? CreatedPartId { get; set; }

		[DisplayName("پیوست")]
		[DisplayInfo(null, true, type: SystemType.File, required: true, fileTypes: ".pdf,.zip,.png,.jpg")]
		public FileEntity? Attachment { get; set; }

		public long? AttachmentId { get; set; }
	}

 }
