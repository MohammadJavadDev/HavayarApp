using System;
using System.Collections.Generic;
using System.Text;

namespace Entities.App.Sale.DTO
{
	public record ProductDTO()
	{
		public long? Id { get; set; }
		public long? Code { get; set; }
		public string Text { get; set; }
	}

}
