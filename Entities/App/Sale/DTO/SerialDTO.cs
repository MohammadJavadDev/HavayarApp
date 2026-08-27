using System;
using System.Collections.Generic;
using System.Text;

namespace Entities.App.Sale.DTO
{
	public record SerialDTO()
	{
		public long? Id { get; set; }
		public string? Code { get; set; }
		public string? Text { get; set; }
		public string Serial { get; set; }
	}
}
