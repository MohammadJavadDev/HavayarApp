using Common.Entities.EntityMetadatas;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Common.Utilities
{
    public static class EnumExtensions
    {
        public static IEnumerable<T> GetEnumValues<T>(this T input) where T : struct
        {

            if (!typeof(T).IsEnum)
            { throw new NotSupportedException(); }
            return Enum.GetValues(input.GetType()).Cast<T>();

        }

        public static IEnumerable<T> GetEnumFlags<T>(this T input) where T : struct
        {

            if (!typeof(T).IsEnum)
            {
                throw new NotSupportedException();
            }

            foreach (var value in Enum.GetValues(input.GetType()))
            {
                if ((input as Enum).HasFlag(value as Enum))
                    yield return (T)value;
            }

        }

        public static string ToDisplay(this Enum value, string property = "Name")
        {
            Assert.NotNull(value, nameof(value));

            var attribute = value.GetType()?.GetField(value.ToString())
                ?.GetCustomAttributes(false)
                .OfType<DisplayAttribute>()
                .FirstOrDefault();

            if (attribute == null)
                return value.ToString();

            var propValue = attribute.GetType().GetProperty(property).GetValue(attribute, null);
            return propValue.ToString();
        }
		public static List<PropertyMetadataOption> GetEnumValuesWithDisplayNames<T>() where T : Enum
		{
			var type = typeof(T);
			var values = Enum.GetValues(type).Cast<T>();

			return values.Select(v =>
			{
				var member = type.GetMember(v.ToString()).First();
				var display = member.GetCustomAttribute<DisplayAttribute>();
				return
				 new PropertyMetadataOption()
				 {
					 Value = Convert.ToInt32(v),
					 Text= display?.Name ?? v.ToString(),
                          ExteraData =v.ToString()

				 };
				 
			}).ToList();
		}

		public static List<PropertyMetadataOption> GetEnumValuesWithDisplayNames<T>(this IEnumerable<T> source)
	   where T : Enum
		{
			if (source == null)
				return new List<PropertyMetadataOption>();

			return source.Select(v =>
			{
				var member = typeof(T).GetMember(v.ToString()).First();
				var display = member.GetCustomAttribute<DisplayAttribute>();
				return new PropertyMetadataOption
				{
					Value = Convert.ToInt32(v),
					Text = display?.Name ?? v.ToString(),
					ExteraData = v.ToString()
				};
			}).ToList();
		}

		private static readonly Dictionary<string, List<PropertyMetadataOption>> _cache = new();
		 

		public static List<PropertyMetadataOption> GetEnumValuesWithDisplayNamesByTypeName(string enumFullName)
		{
			// بررسی کش
			if (_cache.TryGetValue(enumFullName, out var cachedResult))
				return cachedResult;

			// جستجو در تمام Assembly‌های بارگذاری شده
			var type = FindTypeInAllAssemblies(enumFullName);

			if (type == null)
				throw new ArgumentException($"نوع با نام '{enumFullName}' در هیچ Assembly ای یافت نشد.");

			if (!type.IsEnum)
				throw new ArgumentException($"نوع '{enumFullName}' یک Enum نیست.");

			var values = Enum.GetValues(type);
			var result = new List<PropertyMetadataOption>();

			foreach (var value in values)
			{
				var member = type.GetMember(value.ToString()).First();
				var display = member.GetCustomAttribute<DisplayAttribute>();

				result.Add(new PropertyMetadataOption()
				{
					Value = Convert.ToInt32(value),
					Text = display?.Name ?? value.ToString(),
					ExteraData = value.ToString()
				});
			}

			// ذخیره در کش
			_cache[enumFullName] = result;

			return result;
		}

		private static Type FindTypeInAllAssemblies(string typeFullName)
		{
			// ابتدا سعی می‌کنیم با روش معمولی پیدا کنیم
			var type = Type.GetType(typeFullName);
			if (type != null)
				return type;

			// جستجو در تمام Assembly‌های بارگذاری شده در AppDomain فعلی
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				// بررسی می‌کنیم که Assembly خالی نباشد (برای جلوگیری از خطا)
				if (assembly.IsDynamic)
					continue;

				try
				{
					type = assembly.GetType(typeFullName);
					if (type != null)
						return type;
				}
				catch
				{
					// در صورت بروز خطا در بعضی Assembly‌ها، ادامه می‌دهیم
					continue;
				}
			}

			return null;
		}

		public static List<PropertyMetadataOption>? GetEnumValuesFromProperty(PropertyInfo prop)
		{
			if (prop == null)
				throw new ArgumentNullException(nameof(prop));

			// نوع پراپرتی را بگیر
			var propType = prop.PropertyType;

			// اگر nullable بود (مثل Enum?) نوع داخلیش را بگیر
			if (Nullable.GetUnderlyingType(propType) is Type underlying)
				propType = underlying;

			// بررسی کن که enum هست یا نه
			if (!propType.IsEnum)
				return null;

			// لیست مقادیر enum را استخراج کن
			var values = Enum.GetValues(propType).Cast<Enum>();

			var result = values.Select(v =>
			{
				var member = propType.GetMember(v.ToString()).First();
				var display = member.GetCustomAttribute<DisplayAttribute>();
				return new PropertyMetadataOption()
				{
					Value = Convert.ToInt32(v),
					Text = display?.Name ?? v.ToString(),
					ExteraData = v.ToString()

				};
			}).ToList();

			return result;
		}

		public static IEnumerable<TEnum> GetEnumSelectListFiltered<TEnum>(
		    Func<TEnum, bool> filter = null) where TEnum : Enum
				{
					var values = Enum.GetValues(typeof(TEnum)).Cast<TEnum>();
					if (filter != null)
						values = values.Where(filter);

					return values;
				}
	}
}
