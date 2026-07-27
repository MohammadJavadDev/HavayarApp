using Common.Attributes;
using Entities.Base.System.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Base.System;

[Display(Name = "تعریف صفحه")]
[Table("PageDefinition", Schema = "System")]
public class PageDefinition : BaseEntity
{
	[DisplayName("مسیر View")]
	[DisplayInfo(null, true, SystemType.String, required: true)]
	[MaxLength(500)]
	public string ViewPath { get; set; } = "";

	[DisplayName("عنوان نمایشی")]
	[DisplayInfo(null, true, SystemType.String, required: true)]
	[MaxLength(200)]
	public string DisplayName { get; set; } = "";

	[DisplayName("محتوای Razor")]
	[DisplayInfo(null, true, SystemType.String, required: true)]
	public string Content { get; set; } = "";

	[DisplayName("نوع صفحه")]
	[DisplayInfo(null, true, SystemType.Select, required: true)]
	public PageKindEnum PageKind { get; set; } = PageKindEnum.Custom;

	[DisplayName("منبع")]
	[DisplayInfo(null, true, SystemType.Select, required: true)]
	public PageSourceTypeEnum SourceType { get; set; } = PageSourceTypeEnum.Manual;

	[DisplayName("شناسه فرم")]
	[DisplayInfo(null, false, SystemType.Long)]
	public long? FormDefinitionId { get; set; }

	[DisplayName("ماژول")]
	[DisplayInfo(null, false, SystemType.String)]
	[MaxLength(100)]
	public string? Module { get; set; }

	[DisplayName("نسخه")]
	[DisplayInfo(null, false, SystemType.Int)]
	public int Version { get; set; }
}
