using Common.Attributes;
using Common.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Services.ImportDefinitionServices
{
	public class ImportApiEndpointInfo
	{
		public string Controller { get; set; } = "";
		public string ControllerDisplayName { get; set; } = "";
		public string Action { get; set; } = "";
		public string ActionDisplayName { get; set; } = "";
		public string Path { get; set; } = "";
		public string HttpMethod { get; set; } = "POST";
	}

	public class ImportApiColumnInfo
	{
		public string ColumnName { get; set; } = "";
		public string DisplayName { get; set; } = "";
		public SystemType DataType { get; set; } = SystemType.String;
		public bool IsRequired { get; set; }
	}

	public class ImportApiInspectResult
	{
		public string ApiUrl { get; set; } = "";
		public string HttpMethod { get; set; } = "POST";
		public string Controller { get; set; } = "";
		public string Action { get; set; } = "";
		public List<ImportApiColumnInfo> Columns { get; set; } = new();
	}

	public class ImportApiInspectRequest
	{
		public string? Controller { get; set; }
		public string? Action { get; set; }
		public string? Path { get; set; }
	}

	public class ImportApiEndpointInspector
	{
		private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;

		public ImportApiEndpointInspector(IActionDescriptorCollectionProvider actionDescriptorCollectionProvider)
		{
			_actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
		}

		public List<ImportApiEndpointInfo> GetAvailableApis()
		{
			var result = new List<ImportApiEndpointInfo>();

			var actionDescriptors = _actionDescriptorCollectionProvider.ActionDescriptors.Items
				.OfType<ControllerActionDescriptor>();

			foreach (var actionDescriptor in actionDescriptors)
			{
				var controllerType = actionDescriptor.ControllerTypeInfo;
				var controllerInfo = controllerType.GetCustomAttribute<ControllerInfoAttribute>()?.Name;
				if (!controllerInfo.HasValue(true))
					continue;

				var methodInfo = actionDescriptor.MethodInfo;
				var actionDisplay = methodInfo.GetCustomAttribute<ActionDisplayNameAttribute>()?.Name;
				if (!actionDisplay.HasValue(true))
					continue;

				var httpMethod = ResolveHttpMethod(methodInfo, actionDescriptor);
				// فقط متدهایی که معمولاً برای ورود داده معنی دارند
				if (httpMethod is not ("POST" or "PUT" or "PATCH"))
					continue;

				var path = ResolvePath(actionDescriptor);
				if (string.IsNullOrWhiteSpace(path))
					continue;

				result.Add(new ImportApiEndpointInfo
				{
					Controller = actionDescriptor.ControllerName,
					ControllerDisplayName = controllerInfo,
					Action = actionDescriptor.ActionName,
					ActionDisplayName = actionDisplay,
					Path = path,
					HttpMethod = httpMethod
				});
			}

			return result
				.OrderBy(x => x.ControllerDisplayName)
				.ThenBy(x => x.ActionDisplayName)
				.ToList();
		}

		public ImportApiInspectResult Inspect(ImportApiInspectRequest request)
		{
			if (request == null)
				throw new Exception("درخواست نامعتبر است.");

			var actionDescriptors = _actionDescriptorCollectionProvider.ActionDescriptors.Items
				.OfType<ControllerActionDescriptor>();

			ControllerActionDescriptor? match = null;

			if (!string.IsNullOrWhiteSpace(request.Controller) && !string.IsNullOrWhiteSpace(request.Action))
			{
				match = actionDescriptors.FirstOrDefault(ad =>
					string.Equals(ad.ControllerName, request.Controller, StringComparison.OrdinalIgnoreCase) &&
					string.Equals(ad.ActionName, request.Action, StringComparison.OrdinalIgnoreCase));
			}
			else if (!string.IsNullOrWhiteSpace(request.Path))
			{
				var normalized = NormalizePath(request.Path);
				match = actionDescriptors.FirstOrDefault(ad =>
					string.Equals(NormalizePath(ResolvePath(ad)), normalized, StringComparison.OrdinalIgnoreCase));
			}

			if (match == null)
				throw new Exception("API مورد نظر یافت نشد.");

			var controllerInfo = match.ControllerTypeInfo.GetCustomAttribute<ControllerInfoAttribute>()?.Name;
			if (!controllerInfo.HasValue(true))
				throw new Exception("این کنترلر برای ورود اطلاعات قابل استفاده نیست.");

			var methodInfo = match.MethodInfo;
			var columns = ExtractInputColumns(methodInfo);
			var path = ResolvePath(match);
			var httpMethod = ResolveHttpMethod(methodInfo, match);

			return new ImportApiInspectResult
			{
				ApiUrl = path,
				HttpMethod = httpMethod,
				Controller = match.ControllerName,
				Action = match.ActionName,
				Columns = columns
			};
		}

		private static List<ImportApiColumnInfo> ExtractInputColumns(MethodInfo methodInfo)
		{
			var columns = new List<ImportApiColumnInfo>();
			var sort = 0;

			foreach (var param in methodInfo.GetParameters())
			{
				if (param.ParameterType == typeof(CancellationToken))
					continue;

				if (IsSimpleType(param.ParameterType))
				{
					if (param.GetCustomAttribute<FromServicesAttribute>() != null ||
					    param.GetCustomAttribute<FromHeaderAttribute>() != null ||
					    param.GetCustomAttribute<FromFormAttribute>() != null)
						continue;

					// route/query ساده معمولاً برای import body مناسب نیست مگر FromBody روی نوع ساده
					if (param.GetCustomAttribute<FromBodyAttribute>() == null &&
					    (param.GetCustomAttribute<FromRouteAttribute>() != null ||
					     param.GetCustomAttribute<FromQueryAttribute>() != null))
						continue;

					if (param.GetCustomAttribute<FromBodyAttribute>() != null)
					{
						columns.Add(CreateColumn(param.Name ?? "value", param.ParameterType, IsRequiredParameter(param), sort++));
					}
					continue;
				}

				// Complex type: FromBody یا پیش‌فرض body برای API
				if (param.GetCustomAttribute<FromServicesAttribute>() != null)
					continue;

				var fromQuery = param.GetCustomAttribute<FromQueryAttribute>() != null;
				var fromRoute = param.GetCustomAttribute<FromRouteAttribute>() != null;
				if (fromQuery || fromRoute)
					continue;

				foreach (var prop in param.ParameterType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
				{
					if (!prop.CanWrite && !prop.CanRead)
						continue;
					if (prop.GetIndexParameters().Length > 0)
						continue;
					if (prop.GetCustomAttribute<System.Text.Json.Serialization.JsonIgnoreAttribute>() != null)
						continue;

					var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
					if (!IsSimpleType(propType) && !propType.IsEnum)
					{
						// فقط یک سطح برای ستون‌های اکسل
						continue;
					}

					var required = prop.GetCustomAttribute<RequiredAttribute>() != null
						|| (!IsNullableProperty(prop) && propType.IsValueType);

					var display = prop.GetCustomAttribute<DisplayAttribute>()?.Name
						?? prop.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName
						?? prop.Name;

					columns.Add(new ImportApiColumnInfo
					{
						ColumnName = prop.Name,
						DisplayName = display ?? prop.Name,
						DataType = MapToSystemType(propType),
						IsRequired = required
					});
					sort++;
				}
			}

			return columns
				.GroupBy(c => c.ColumnName, StringComparer.OrdinalIgnoreCase)
				.Select(g => g.First())
				.ToList();
		}

		private static ImportApiColumnInfo CreateColumn(string name, Type type, bool required, int sort)
		{
			var underlying = Nullable.GetUnderlyingType(type) ?? type;
			return new ImportApiColumnInfo
			{
				ColumnName = name,
				DisplayName = name,
				DataType = MapToSystemType(underlying),
				IsRequired = required
			};
		}

		private static bool IsRequiredParameter(ParameterInfo param)
		{
			if (param.GetCustomAttribute<RequiredAttribute>() != null)
				return true;
			if (param.HasDefaultValue)
				return false;
			var type = param.ParameterType;
			if (Nullable.GetUnderlyingType(type) != null)
				return false;
			return type.IsValueType;
		}

		private static bool IsNullableProperty(PropertyInfo prop)
		{
			var nullability = new NullabilityInfoContext().Create(prop);
			return nullability.WriteState == NullabilityState.Nullable
				|| nullability.ReadState == NullabilityState.Nullable
				|| Nullable.GetUnderlyingType(prop.PropertyType) != null;
		}

		private static bool IsSimpleType(Type type)
		{
			type = Nullable.GetUnderlyingType(type) ?? type;
			return type.IsPrimitive
				|| type.IsEnum
				|| type == typeof(string)
				|| type == typeof(decimal)
				|| type == typeof(DateTime)
				|| type == typeof(DateTimeOffset)
				|| type == typeof(Guid)
				|| type == typeof(TimeSpan);
		}

		private static SystemType MapToSystemType(Type type)
		{
			type = Nullable.GetUnderlyingType(type) ?? type;

			if (type == typeof(bool))
				return SystemType.Boolean;
			if (type == typeof(int) || type == typeof(short) || type == typeof(byte))
				return SystemType.Int;
			if (type == typeof(long))
				return SystemType.Long;
			if (type == typeof(decimal) || type == typeof(double) || type == typeof(float))
				return SystemType.Decimal;
			if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
				return SystemType.DateTime;
			if (type.IsEnum)
				return SystemType.Select;

			return SystemType.String;
		}

		private static string ResolveHttpMethod(MethodInfo methodInfo, ControllerActionDescriptor actionDescriptor)
		{
			var httpMethodAttrs = methodInfo.GetCustomAttributes()
				.OfType<HttpMethodAttribute>()
				.SelectMany(a => a.HttpMethods)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();

			if (httpMethodAttrs.Count > 0)
				return httpMethodAttrs[0].ToUpperInvariant();

			if (actionDescriptor.ActionConstraints != null)
			{
				foreach (var constraint in actionDescriptor.ActionConstraints)
				{
					var httpMethodsProp = constraint.GetType().GetProperty("HttpMethods");
					if (httpMethodsProp?.GetValue(constraint) is IEnumerable<string> methods)
					{
						var m = methods.FirstOrDefault();
						if (!string.IsNullOrWhiteSpace(m))
							return m.ToUpperInvariant();
					}
				}
			}

			return "POST";
		}

		private static string ResolvePath(ControllerActionDescriptor actionDescriptor)
		{
			var template = actionDescriptor.AttributeRouteInfo?.Template;
			if (string.IsNullOrWhiteSpace(template))
				return $"/{actionDescriptor.ControllerName}/{actionDescriptor.ActionName}".ToLowerInvariant();

			var path = "/" + template
				.Replace("{action}", actionDescriptor.ActionName, StringComparison.OrdinalIgnoreCase)
				.Replace("[action]", actionDescriptor.ActionName, StringComparison.OrdinalIgnoreCase)
				.TrimStart('/');

			return path.ToLowerInvariant();
		}

		private static string NormalizePath(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
				return "";
			path = path.Trim();
			if (!path.StartsWith('/'))
				path = "/" + path;
			return path.ToLowerInvariant();
		}
	}
}
