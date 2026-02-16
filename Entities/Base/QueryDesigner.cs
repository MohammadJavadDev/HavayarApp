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
		public int Id { get; set; }

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

		 
	}

	/// <summary>
	/// اطلاعات جدول یا ویو
	/// </summary>
	public class TableInfo
	{
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
		public string SourceTable { get; set; }
		public string SourceColumn { get; set; }
		public string TargetTable { get; set; }
		public string TargetColumn { get; set; }
		public string JoinType { get; set; } // "INNER", "LEFT", "RIGHT"
		public bool IsAutoDetected { get; set; }
	}

	/// <summary>
	/// پارامتر داینامیک گزارش - ورودی در زمان اجرا
	/// </summary>
	public class ReportParameter
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
	public class ReportColumn
	{
		public string TableName { get; set; }
		public string ColumnName { get; set; }
		public string DisplayName { get; set; }
		public SortDirection? SortDirection { get; set; } // Ascending, Descending
		public int? SortOrder { get; set; }
		public bool GroupBy { get; set; }
		public string Aggregate { get; set; } // null, "Count", "Max", "Min", "Avg", "Sum", "CountDistinct", "AvgDistinct", "SumDistinct"
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
		public List<ReportParameter> Parameters { get; set; }
	}

	/// <summary>
	/// طراحی کامل گزارش
	/// </summary>
	public class ReportDesign
	{
		public string Name { get; set; }
		public string Title { get; set; }
		public ReportType Type { get; set; }
		public string? EntityName { get; set; }
		public QueryDesign QueryDesign { get; set; }
		public List<ReportColumn> Columns { get; set; }
	}

    public enum ReportType
    {
		Report,
		DataProfile
    }

    /// <summary>
    /// نتیجه اجرای Query
    /// </summary>
    public class ReportResult
	{
		public List<string> ColumnNames { get; set; }
		public List<string> ColumnTypes { get; set; }
		public List<Dictionary<string, object>> Rows { get; set; }
		public int TotalRows { get; set; }
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
