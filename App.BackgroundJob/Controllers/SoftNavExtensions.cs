using Microsoft.AspNetCore.Mvc;

namespace App.BackgroundJob.Controllers
{
	public static class SoftNavExtensions
	{
		public static bool IsPartialRequest(this Controller controller)
		{
			var request = controller.Request;
			if (string.Equals(request.Headers["X-Partial"], "1", StringComparison.Ordinal))
				return true;

			if (request.Query.TryGetValue("partial", out var partial) &&
			    (partial == "1" || string.Equals(partial, "true", StringComparison.OrdinalIgnoreCase)))
				return true;

			return false;
		}

		public static IActionResult SoftView(this Controller controller, object? model = null)
		{
			if (controller.IsPartialRequest())
				return controller.PartialView(model);

			return controller.View(model);
		}

		public static IActionResult SoftView(this Controller controller, string viewName, object? model = null)
		{
			if (controller.IsPartialRequest())
				return controller.PartialView(viewName, model);

			return controller.View(viewName, model);
		}
	}
}
