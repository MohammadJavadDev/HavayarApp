using WebFramework.Abstractions;

namespace WebApp.Services
{
	public class EnvironmentService : IEnvironmentService
	{
		private readonly IWebHostEnvironment _env;

		public EnvironmentService(IWebHostEnvironment env)
		{
			_env = env;
		}

		public string EnvironmentName => _env.EnvironmentName;
		public string ContentRootPath => _env.ContentRootPath;
		public string WebRootPath => _env.WebRootPath;

		public bool IsDevelopment() => _env.IsDevelopment();
		public bool IsProduction() => _env.IsProduction();
		public bool IsStaging() => _env.IsStaging();
	}
}
