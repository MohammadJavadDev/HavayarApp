using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace WebApp.Models
{
    public static class AppError
    {
        public static string GetModelStateErrors(this ModelStateDictionary modelState)
        {
            var errors = modelState
                .Where(ms => ms.Value.Errors.Any())
                .SelectMany(ms => ms.Value.Errors.Select(e => e.ErrorMessage))
                .ToList();

            return string.Join("; ", errors);
        }

    }
}
