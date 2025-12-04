using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Base
{
	public class EntityInfo
	{
		public string Name { get; set; }
		public string Description { get; set; }
		public string Title { get; set; }
		public string Type { get; set; }
		public string FullName { get; set; }
		public string NameSpace { get; set; }
		public long? ParentId { get; set; }
		public List<EntityInfo>? Propertise { get; set; }

	}
}
