using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Common.Utilities
{
    public class PersianDateTimeConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Logic to convert Persian date back to DateTime (if needed)
            throw new NotImplementedException("Deserialization of Persian dates is not implemented.");
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                PersianCalendar persianCalendar = new PersianCalendar();
                var persianDate = $"{persianCalendar.GetYear(value.Value)}/{persianCalendar.GetMonth(value.Value):00}/{persianCalendar.GetDayOfMonth(value.Value):00}";
                writer.WriteStringValue(persianDate);
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }

}
