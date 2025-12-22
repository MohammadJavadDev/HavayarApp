using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Attributes
{
	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	public class JobHandlerAttribute : Attribute
	{
		public string DisplayName { get; }
		public string? Description { get; }

		public JobHandlerAttribute(string displayName, string description = "")
		{
			DisplayName = displayName;
			Description = description;
		}

		public JobHandlerAttribute(string displayName)
		{
			DisplayName = displayName;
		 
		}

		 
	}

}
