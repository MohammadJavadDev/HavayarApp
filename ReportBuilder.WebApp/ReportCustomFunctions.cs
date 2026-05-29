using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUglify.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;

namespace RB.WebApp
{
	public static class ReportCustomFunctions
	{
		public static string Json(string json, string path)
		{
			 

			if (string.IsNullOrWhiteSpace(json))
				return "";

			try
			{
				var jObj = JObject.Parse(json);
				var token = jObj.SelectToken(path);

				if (token == null)
					return "";

				return token.ToString();
			}
			catch
			{
				return "";
			}
		}

		public static string EnumDisplayName(string enumName, object value)
		{
			if (string.IsNullOrWhiteSpace(enumName) || value == null )
				return "";

			try
			{
				// پیدا کردن تایپ enum
				var enumType = AppDomain.CurrentDomain
				    .GetAssemblies()
				    .SelectMany(a => a.GetTypes())
				    .FirstOrDefault(t => t.IsEnum && t.Name == enumName);

				if (enumType == null)
					return value.ToString();

				if (value.ToString().IsNullOrWhiteSpace())
					return "";

				var numericValue = Convert.ToInt32(value);
				var enumValue = Enum.ToObject(enumType, numericValue);

				var member = enumType.GetMember(enumValue.ToString()).FirstOrDefault();
				if (member != null)
				{
					var display = member.GetCustomAttribute<DisplayAttribute>();
					if (display != null)
						return display.Name;
				}

				return enumValue.ToString();
			}
			catch
			{
				return value.ToString();
			}
		}
	}
}
