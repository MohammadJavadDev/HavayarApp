using Common.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Entities.Base.ImportDefinitions
{
	[Display(Name = "تعریف ورود اطلاعات")]
	[Table("ImportDefinition", Schema = "System")]
	public class ImportDefinition:BaseEntity
	{
		[Required(ErrorMessage = "نام موجودیت الزامی است")]
		[DisplayName("نام موجودیت")]
		[DisplayInfo(null, true, SystemType.String, required: true)]
		[StringLength(200)]
		public string FullNameEntity { get; set; }

		[Required(ErrorMessage = "عنوان الزامی است")]
		[StringLength(200)]
		[DisplayInfo(null, true, SystemType.String, required: true)]
		[DisplayName("عنوان")]
		public string Title { get; set; }

		[StringLength(500)]
		[DisplayInfo(null, true, SystemType.String)]
		[DisplayName("توضیحات")]
		public string Description { get; set; }

		[Required(ErrorMessage = "کوئری SQL الزامی است")]
		[DisplayName("کوئری SQL")]
		[DisplayInfo(null, true, SystemType.String, required: true)]
		public string SqlQuery { get; set; }
 

		public string Columns { get; set; }
	}
	 

	public class ImportDefinitionColumn 
	{
 

		// نام پارامتر در SQL (مثلاً "FirstName" برای @FirstName)
		public string ColumnName { get; set; }

		// عنوانی که در هدر اکسل نشان داده می‌شود
		public string DisplayName { get; set; }

		// Text | Integer | Decimal | Date | DateTime | Boolean
		public SystemType DataType { get; set; } =  SystemType.String;

		// آیا این ستون از متغیرهای سیستمی است؟
		public bool IsSystemVariable { get; set; } = false;

		// CurrentUserId | CurrentDate | CurrentDateTime | CurrentUserName
		public string? SystemVariable { get; set; }

		public bool IsRequired { get; set; } = false;
		public int SortOrder { get; set; } = 0;

		public OptionSetting? OptionSettings { get; set; } 

		// Navigation
		public ImportDefinition ImportDefinition { get; set; }
		public long ImportDefinitionId { get; set; }
	}

	public static class SystemVariables
	{
		public const string CurrentUserId = "CurrentUserId";
		public const string CurrentDate = "CurrentDate";
		public const string CurrentDateTime = "CurrentDateTime";
		public const string CurrentUserName = "CurrentUserName";

		public static Dictionary<string, string> GetAll() => new Dictionary<string, string>
	   {
		  { CurrentUserId,   "شناسه کاربر جاری" },
		  { CurrentDate,     "تاریخ جاری" },
		  { CurrentDateTime, "تاریخ و ساعت جاری" },
		  { CurrentUserName, "نام کاربر جاری" }
	   };
	}
}
