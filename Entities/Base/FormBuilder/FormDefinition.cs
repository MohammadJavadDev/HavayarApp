using Common.Attributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Base.FormBuilder
{
	[Display(Name = "تعریف فرم")]
	[Table("FormDefinition", Schema = "System")]
	public class FormDefinition : BaseEntity
	{
		[DisplayName("نام موجودیت")]
		[DisplayInfo(null, true, SystemType.String, required: true)]
		[MaxLength(200)]
		public string EntityName { get; set; }

		[DisplayName("نام نمایشی")]
		[DisplayInfo(null, true, SystemType.String, required: true)]
		[MaxLength(200)]
		public string DisplayName { get; set; }

		[DisplayName("ماژول")]
		[DisplayInfo(null, true, SystemType.String, required: true)]
		[MaxLength(100)]
		public string Module { get; set; }

		[DisplayName("Schema")]
		[DisplayInfo(null, true, SystemType.String)]
		[MaxLength(50)]
		public string Schema { get; set; } = "dbo";

		[DisplayName("Namespace")]
		[DisplayInfo(null, false, SystemType.String)]
		[MaxLength(500)]
		public string? Namespace { get; set; }

		[DisplayName("موجودیت پایه")]
		[DisplayInfo(null, false, SystemType.String)]
		[MaxLength(200)]
		public string? BaseEntityType { get; set; } = "BaseEntity";

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, SystemType.String)]
		[MaxLength(1000)]
		public string? Description { get; set; }

	//[DisplayName("ویژگی‌ها")]
	//[DisplayInfo(null, true, SystemType.ListEntity)]
	//public List<FormProperty> Properties { get; set; } = new();

	[DisplayName("بخش‌ها")]
	[DisplayInfo(null, false, SystemType.ListEntity)]
	public List<FormSection> Sections { get; set; } = new();

		[DisplayName("آیا از موجودیت موجود ایجاد شده")]
		[DisplayInfo(null, false, SystemType.Boolean)]
		public bool IsFromExistingEntity { get; set; } = false;

		[DisplayName("نام کامل موجودیت مبدا")]
		[DisplayInfo(null, false, SystemType.String)]
		[MaxLength(500)]
		public string? SourceEntityFullName { get; set; }
	}

	[Display(Name = "ویژگی فرم")]
	[Table("FormProperty", Schema = "System")]
	public class FormProperty : BaseEntity
	{
		public long FormDefinitionId { get; set; }
		public FormDefinition? FormDefinition { get; set; }

		public long? ParentPropertyId { get; set; }
		public FormProperty? ParentProperty { get; set; }

		public long? FormSectionId { get; set; }
		public FormSection? FormSection { get; set; }

		[DisplayName("نام ویژگی")]
		[DisplayInfo(null, true, SystemType.String, required: true)]
		[MaxLength(200)]
		public string PropertyName { get; set; }

		[DisplayName("نام نمایشی")]
		[DisplayInfo(null, true, SystemType.String, required: true)]
		[MaxLength(200)]
		public string DisplayName { get; set; }

		[DisplayName("نوع سیستم")]
		[DisplayInfo(null, true, SystemType.Select, required: true)]
		public SystemType SystemType { get; set; }

		[DisplayName("مسیر جستجو")]
		[DisplayInfo(null, false, SystemType.String)]
		[MaxLength(300)]
		public string? SearchPath { get; set; }

		[DisplayName("افزودن به جدول")]
		[DisplayInfo(null, true, SystemType.Boolean)]
		public bool AddToTable { get; set; } = false;

		[DisplayName("ویژگی سیستمی")]
		[DisplayInfo(null, true, SystemType.Boolean)]
		public bool SystemProperty { get; set; } = false;

		[DisplayName("نمایش در اطلاعات رابطه")]
		[DisplayInfo(null, true, SystemType.Boolean)]
		public bool ShowInRelationData { get; set; } = false;

		[DisplayName("الزامی")]
		[DisplayInfo(null, true, SystemType.Boolean)]
		public bool Required { get; set; } = false;

		[DisplayName("انواع فایل")]
		[DisplayInfo(null, false, SystemType.String)]
		[MaxLength(500)]
		public string? FileTypes { get; set; }

		[DisplayName("حداکثر حجم فایل (MB)")]
		[DisplayInfo(null, false, SystemType.Int)]
		public int MaxFileSize { get; set; } = 10;

		[DisplayName("الگوی Regex")]
		[DisplayInfo(null, false, SystemType.String)]
		[MaxLength(500)]
		public string? Regex { get; set; }

		[DisplayName("پیام خطای Regex")]
		[DisplayInfo(null, false, SystemType.String)]
		[MaxLength(500)]
		public string? RegexInvalidError { get; set; }

		[DisplayName("شروع AutoNumber")]
		[DisplayInfo(null, false, SystemType.Long)]
		public long AutoNumberStart { get; set; } = 1;

		[DisplayName("گام AutoNumber")]
		[DisplayInfo(null, false, SystemType.Long)]
		public long AutoNumberStep { get; set; } = 1;

		[DisplayName("حداکثر طول")]
		[DisplayInfo(null, false, SystemType.Int)]
		public int? MaxLength { get; set; }

		[DisplayName("موجودیت مرتبط")]
		[DisplayInfo(null, false, SystemType.String)]
		[MaxLength(500)]
		public string? RelatedEntityFullName { get; set; }

		[DisplayName("نام موجودیت مرتبط")]
		[DisplayInfo(null, false, SystemType.String)]
		[MaxLength(200)]
		public string? RelatedEntityName { get; set; }

		[DisplayName("نام Enum")]
		[DisplayInfo(null, false, SystemType.String)]
		[MaxLength(200)]
		public string? EnumName { get; set; }

		[DisplayName("نام کلاس فرزند")]
		[DisplayInfo(null, false, SystemType.String)]
		[MaxLength(200)]
		public string? ChildEntityName { get; set; }

		[DisplayName("نام نمایشی کلاس فرزند")]
		[DisplayInfo(null, false, SystemType.String)]
		[MaxLength(200)]
		public string? ChildEntityDisplayName { get; set; }

		[DisplayName("ترتیب")]
		[DisplayInfo(null, true, SystemType.Int)]
		public int OrderIndex { get; set; } = 0;

		[DisplayName("سایز Col")]
		[DisplayInfo(null, false, SystemType.String)]
		[MaxLength(50)]
		public string ColSize { get; set; } = "col-md-6";

		[DisplayName("گزینه‌های Enum")]
		[DisplayInfo(null, false, SystemType.ListEntity)]
		public List<FormPropertyEnumOption> EnumOptions { get; set; } = new();

		[DisplayName("ویژگی‌های فرزند")]
		[DisplayInfo(null, false, SystemType.ListEntity)]
		public List<FormProperty> ChildProperties { get; set; } = new();

	[DisplayName("قالب نمایش")]
	[DisplayInfo(null, false, SystemType.String)]
	[MaxLength(500)]
	public string? DisplayTemplate { get; set; }
}

[Display(Name = "بخش فرم")]
[Table("FormSection", Schema = "System")]
public class FormSection : BaseEntity
{
	public long FormDefinitionId { get; set; }
	public FormDefinition? FormDefinition { get; set; }

	[DisplayName("عنوان بخش")]
	[DisplayInfo(null, true, SystemType.String, required: true)]
	[MaxLength(200)]
	public string Title { get; set; }

	[DisplayName("ترتیب")]
	[DisplayInfo(null, true, SystemType.Int)]
	public int OrderIndex { get; set; } = 0;

	[DisplayName("ویژگی‌ها")]
	[DisplayInfo(null, false, SystemType.ListEntity)]
	public List<FormProperty> Properties { get; set; } = new();
}

	[Display(Name = "گزینه Enum ویژگی فرم")]
	[Table("FormPropertyEnumOption", Schema = "System")]
	public class FormPropertyEnumOption : BaseEntity
	{
		public long FormPropertyId { get; set; }
		public FormProperty FormProperty { get; set; }

		[DisplayName("مقدار")]
		[DisplayInfo(null, true, SystemType.Int, required: true)]
		public int Value { get; set; }

		[DisplayName("عنوان فارسی")]
		[DisplayInfo(null, true, SystemType.String, required: true)]
		[MaxLength(200)]
		public string Title { get; set; }

		[DisplayName("نام انگلیسی")]
		[DisplayInfo(null, true, SystemType.String, required: true)]
		[MaxLength(200)]
		public string EnglishName { get; set; }

		[DisplayName("ترتیب")]
		[DisplayInfo(null, true, SystemType.Int)]
		public int OrderIndex { get; set; } = 0;
	}
}

