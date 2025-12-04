using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Base
{
	public class FileEntity :BaseEntity
	{
		public string PhysicalPath { get; set; } // /uploads/20251104/abcd1234.pdf
		public string OriginalName { get; set; }
		public string ContentType { get; set; }
		public long Size { get; set; }
		public string EntityType { get; set; }
		public string EntityPropName { get; set; }
		public long? EntityId { get; set; }
 
	}
}
