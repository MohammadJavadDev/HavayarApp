using Common.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Entities.EntityMetadatas
{
 
	public class EntityMetadata
	{
		public string EntityName { get; set; } = "";
		public string DisplayName { get; set; } = "";
		public string Schema { get; set; } = "dbo";
		public string Type { get; set; } = "string";
		public string EntityFullName { get; set; } = "string";
		public string TabelName { get; set; }
		public List<PropertyMetadata> Properties { get; set; } = new();
	}

	public class PropertyMetadata
	{
		public string Name { get; set; } = "";
		public string DisplayName { get; set; } = "";
		public string Type { get; set; } = "";
		public string DataType { get; set; } = "";
		public string? SearchPath { get; set; }
		public bool AddToTable { get; set; }
		public bool SystemProperty { get; set; }
		public bool ShowInRelationData { get; set; }
		public string FileTypes { get; set; } = "";
		public int MaxFileSize { get; set; }
		public bool Required { get; set; }
		public string? Regex { get; set; }
		public string? RegexInvalidError { get; set; }
		public long Start { get; set; }
		public long Step { get; set; }

		// روابط
		public string? RelatedEntity { get; set; }
		public string? RelatedEntityType { get; set; }
		public string? RelatedEntityTypeFullName { get; set; }
		public string? RelatedDisplayProp { get; set; }
		public string? ParentEntityName { get; set; }
		public string? ParentEntityTypeName { get; set; }
		public SystemType SystemType { get; set; }
		public List<PropertyMetadataOption> Options { get; set; } = new();
	}

	public class PropertyMetadataOption {
		public int Value { get; set; }
		public string Text { get; set; } = string.Empty;
		public string ExteraData { get; set; } = string.Empty;
	}
}
