using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Entities.Base.ImportDefinitions
{
	[Display(Name = "تاریخچه ورود اطلاعات")]
	[Table("ImportLog", Schema = "System")]
	public class ImportLog:BaseEntity
	{

		public string FileName { get; set; }
		public int TotalRows { get; set; }
		public int SuccessRows { get; set; }
		public int FailedRows { get; set; }

		public long ImportDefinitionId { get; set; }
		public ImportDefinition ImportDefinition { get; set; }
		public List<ImportLogDetail> Details { get; set; } = new List<ImportLogDetail>();
	}
	[Display(Name = "جزئیات تاریخچه ورود اطلاعات")]
	[Table("ImportLogDetail", Schema = "System")]

	public class ImportLogDetail : BaseEntity
	{
		public int ImportLogId { get; set; }
		public ImportLog ImportLog { get; set; }
		public int RowNumber { get; set; }
		public bool Success { get; set; }
		public string? ErrorMessage { get; set; }
	}
}
