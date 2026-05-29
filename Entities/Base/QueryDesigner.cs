using Common.Attributes;
using Entities.Base.DataTable;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Entities.Base
{
	/// <summary>
	/// مدل گزارش ذخیره شده
	/// </summary>
	public class SavedQuery:BaseEntity
	{
 
		[Required]
		[MaxLength(100)]
		public string Name { get; set; }

		[Required]
		[MaxLength(200)]
		public string Title { get; set; }

		[Required]
		public string QueryJson { get; set; }

		[Required]
		public string ColumnsJson { get; set; }

		public string DiagramJson { get; set; }

		public string CustomActionButtonsJson { get; set; }
		public string? EventScriptsJson { get; set; }
		public ModeQuery Mode { get; set; } = 0;
		public TypeQuery Type { get; set; } = 0;
		public string? EntityFullName { get; set; }
		public string ActionOptions { get; set; }


	}

	public enum ModeQuery
	{
		Diagram,
		Query
	}
	public enum TypeQuery
	{
		Report,
		DataProfile
	}

	/// <summary>
	/// اطلاعات Stored Procedure
	/// </summary>
	public class StoredProcedureInfo
	{
		public string Schema { get; set; }
		public string Name { get; set; }
		public string FullName => $"{Schema}.{Name}";
	}

	/// <summary>
	/// پارامتر Stored Procedure
	/// </summary>
	public class StoredProcedureParameter
	{
		public string Name { get; set; }
		public string DataType { get; set; }
		public bool IsOutput { get; set; }
		public int MaxLength { get; set; }
	}

	/// <summary>
	/// اطلاعات جدول یا ویو
	/// </summary>
	public class TableInfo
	{
		public string BoxId { get; set; }
		public string InstanceId { get; set; }
		public string Schema { get; set; }
		public string Name { get; set; }
		public string? DisplayName { get; set; }
		public string? EntityFullName { get; set; }
		public bool HasEntity { get; set; } = false;
		public string Type { get; set; } // Table یا View
		public List<ColumnInfo> Columns { get; set; }
		public double PositionX { get; set; }
		public double PositionY { get; set; }
	}

	/// <summary>
	/// اطلاعات ستون
	/// </summary>
	public class ColumnInfo
	{
		public string Name { get; set; }
		public string DataType { get; set; }
		public bool IsNullable { get; set; }
		public bool IsPrimaryKey { get; set; }
		public bool IsForeignKey { get; set; }
		public string DisplayName { get; set; }
		public bool IsSelected { get; set; }
	}

	/// <summary>
	/// رابطه بین جداول
	/// </summary>
	public class TableRelation
	{
		public string Id { get; set; }
		public string SourceBoxId { get; set; }
		public string SourceTable { get; set; }
		public string SourceColumn { get; set; }
		public string TargetBoxId { get; set; }
		public string TargetTable { get; set; }
		public string TargetColumn { get; set; }
		public string JoinType { get; set; } // "INNER", "LEFT", "RIGHT"
		public bool IsAutoDetected { get; set; }
	}

	/// <summary>
	/// پارامتر داینامیک گزارش - ورودی در زمان اجرا
	/// </summary>
	public class QueryParameter
	{
		public string Id { get; set; }
		public string Name { get; set; }       // نام پارامتر بدون @ (مثلاً MyParam)
		public string DisplayName { get; set; } // عنوان نمایشی
		public string DefaultValue { get; set; }
		public string DataType { get; set; }    // string, int, datetime, bool
	}

	/// <summary>
	/// شرط فیلتر
	/// </summary>
	public class FilterCondition
	{
		public string Id { get; set; }
		public string TableName { get; set; }
		public string ColumnName { get; set; }
		public string DataType { get; set; }
		public string Operator { get; set; } // equals, greater, like, etc.
		public string Value { get; set; }
		public string LogicalOperator { get; set; } // AND, OR
	}

	/// <summary>
	/// تنظیمات ستون در گزارش
	/// </summary>
	public class QueryColumn
	{
		public string TableName { get; set; }
		public string ColumnName { get; set; }
		public string DisplayName { get; set; }
		public string Alliance { get; set; }
		public string Address { get; set; }
		public string? SystemTypeName { get; set; }
		public bool Visible { get; set; } = true;
		public bool PrimaryKey { get; set; } = false;
		public SystemType? SystemType { get; set; }
		public SortDirection? SortDirection { get; set; } // Ascending, Descending
		public int? SortOrder { get; set; }
		public bool GroupBy { get; set; }
		public List<SelectOptions> Options { get; set; } = new();
		public OptionSetting? OptionSetting { get; set; }
		public string Aggregate { get; set; } // null, "Count", "Max", "Min", "Avg", "Sum", "CountDistinct", "AvgDistinct", "SumDistinct"
          public string? Render { get; set; }
		public bool IsCustom { get; set; } = false;
		public string? CustomColType { get; set; }  // "data", "button", "input", "html"
		public string? HtmlTemplate { get; set; }
		public BtnConfig? BtnConfig { get; set; }
		public InputConfig? InputConfig { get; set; }
	}

	public class BtnConfig
	{
		public string? ColorClass { get; set; }
		public string? Icon { get; set; }
		public string? Text { get; set; }
	}

	public class InputConfig
	{
		public string? Type { get; set; }
		public string? CssClass { get; set; }
		public string? DataAttr { get; set; }
	}


	/// <summary>
	/// تعریف یک دکمه اکشن سفارشی برای نوار ابزار DataTable
	/// </summary>
	public class CustomActionButton
	{
		public string Id { get; set; }

		/// <summary>عنوان tooltip دکمه</summary>
		public string Title { get; set; }

		/// <summary>کلاس رنگ Bootstrap/Metronic مثل btn-color-primary</summary>
		public string? ColorClass { get; set; }

		/// <summary>کلاس آیکون Font Awesome یا Bootstrap Icons</summary>
		public string? IconClass { get; set; }

		/// <summary>آیا دکمه نیاز به انتخاب ردیف دارد</summary>
		public bool RequiresSelection { get; set; }

		/// <summary>اگر true باشد از Html استفاده می‌شود</summary>
		public bool UseHtml { get; set; }

		/// <summary>HTML سفارشی دکمه (وقتی UseHtml = true)</summary>
		public string? Html { get; set; }

		/// <summary>تابع JS به صورت رشته: function(ctx) { ... }</summary>
		public string ActionScript { get; set; }
	}

	public class OptionSetting
	{
		public List<SelectOptions> ListOptions { get; set; } = new();
		public TypeOptionEnum TypeOption { get; set; }
		public string? SystemTypeName { get; set; }
		 
	}
	public enum TypeOptionEnum
	{
		Defination = 1,
		Exist= 2,
		System = 3
	}
 
	public class SelectOptions
	{
		public int Value { get; set; }
		public string Name { get; set; }

	}
	/// <summary>
	/// طراحی Query
	/// </summary>
	public class QueryDesign
	{
		public List<TableInfo> Tables { get; set; }
		public List<TableRelation> Relations { get; set; }
		public List<FilterCondition> Filters { get; set; }
		public string CustomQuery { get; set; }
		public List<QueryParameter> Parameters { get; set; }
	}

	/// <summary>
	/// طراحی کامل گزارش
	/// </summary>
	public class QueryDesignRequest
	{
		public string Name { get; set; }
		public string Title { get; set; }
		public ModeQuery Mode { get; set; } = 0;
		public TypeQuery Type { get; set; } = 0;
		public string? EntityFullName { get; set; }
		public QueryDesign QueryDesign { get; set; }
		public List<QueryColumn> Columns { get; set; }
		public string ActionOptions { get; set; }
		public string? CustomActionButtons { get; set; }
		public string? EventScripts { get; set; }

	}

	public enum QueryType
	{
		Report,
		DataProfile
    }

    /// <summary>
    /// نتیجه اجرای Query
    /// </summary>
    public class QueryResult
	{
		public List<string> ColumnNames { get; set; }
		public List<string> ColumnTypes { get; set; }
		public List<Dictionary<string, object>> Rows { get; set; }
		public int TotalRows { get; set; }
		public int TotalFilterdRows { get; set; }
		public string GeneratedQuery { get; set; }
	}

	public enum JoinType
	{
		Inner,
		Left,
		Right
	}

	public enum SortDirection
	{
		Ascending,
		Descending,
	}
}
