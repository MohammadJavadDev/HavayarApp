using System;
using System.Collections.Generic;
using System.Text;

namespace WebFramework.Abstractions
{
	public interface IEnvironmentService
	{
		string EnvironmentName { get; }
		string ContentRootPath { get; }
		string WebRootPath { get; }
		bool IsDevelopment();
		bool IsProduction();
		bool IsStaging();
	}
}
