using Common.Attributes;

namespace WebFramework.ViewModels.FormBuilder
{
	/// <summary>
	/// اطلاعات Connection String
	/// </summary>
	public class ConnectionStringInfo
	{
		public string Name { get; set; }
		public string Value { get; set; }
		public bool IsFromConfig { get; set; } = true;
	}

	/// <summary>
	/// نتیجه تست اتصال به دیتابیس
	/// </summary>
	public class ConnectionTestResult
	{
		public bool IsSuccess { get; set; }
		public string Message { get; set; }
		public string? DatabaseName { get; set; }
		public string? ServerVersion { get; set; }
	}

	/// <summary>
	/// اطلاعات یک جدول دیتابیس
	/// </summary>
	public class DatabaseTableInfo
	{
		public string Schema { get; set; }
		public string TableName { get; set; }
		public long RowCount { get; set; }
		public string FullName => $"{Schema}.{TableName}";
	}

	/// <summary>
	/// اطلاعات کامل یک ستون جدول
	/// </summary>
	public class TableColumnInfo
	{
		public string ColumnName { get; set; }
		public string DataType { get; set; }
		public int? MaxLength { get; set; }
		public bool IsNullable { get; set; }
		public bool IsPrimaryKey { get; set; }
		public bool IsForeignKey { get; set; }
		public string? ForeignKeyTable { get; set; }
		public string? ForeignKeyColumn { get; set; }
		public string? DefaultValue { get; set; }
		public SystemType MappedSystemType { get; set; }
		public string SuggestedDisplayName { get; set; }
		public int? NumericPrecision { get; set; }
		public int? NumericScale { get; set; }
		
		// فیلدهای قابل ویرایش توسط کاربر
		public bool Required { get; set; }
		public bool AddToTable { get; set; } = true;
		public string? RelatedEntityFullName { get; set; }
		public string? RelatedEntityName { get; set; }
	}

	/// <summary>
	/// درخواست ایجاد فرم از جدول
	/// </summary>
	public class GenerateFormFromTableRequest
	{
		public string ConnectionString { get; set; }
		public string Schema { get; set; }
		public string TableName { get; set; }
		public string ModuleName { get; set; }
		public string ImportMode { get; set; } // "replace" or "append"
		public long? ExistingFormDefinitionId { get; set; }
		public string? SectionName { get; set; }
		public List<TableColumnInfo>? SelectedColumns { get; set; }
	}
}












