using Entities.Base.ImportDefinitions;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Text;

namespace WebFramework.ViewModels.ImportDefinitionsViewModels
{

	public class ImportDefinitionViewModel
	{
		public ImportDefinition Definition { get; set; }
		public SelectList DataTypeList { get; set; }
		public SelectList SystemVariableList { get; set; }
	}

	public class ImportDataViewModel
	{
		public List<ImportDefinition> Definitions { get; set; }
		public int? SelectedDefinitionId { get; set; }
	}

	public class ImportResultViewModel
	{
		public int ImportLogId { get; set; }
		public string DefinitionTitle { get; set; }
		public string FileName { get; set; }
		public int TotalRows { get; set; }
		public int SuccessRows { get; set; }
		public int FailedRows { get; set; }
		public DateTime ImportedAt { get; set; }
		public List<ImportLogDetail> FailedDetails { get; set; }
	}

	public class ColumnMappingModel
	{
		public int ImportDefinitionId { get; set; }
		public string ExcelColumnName { get; set; }
		public string SystemColumnName { get; set; }
	}
}
