using Newtonsoft.Json;

namespace Common.Utilities
{
	/// <summary>
	/// HTS stores Work_Overtime_Hour as minutes and edits it as HHH:MM (e.g. 02:43 → 163).
	/// Accepts null, integer minutes, or an hour:minute string; incomplete masks become null.
	/// </summary>
	public class MinutesFromHourMinJsonConverter : JsonConverter<short?>
	{
		public override short? ReadJson(JsonReader reader, Type objectType, short? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
				return null;
			if (reader.TokenType is JsonToken.Integer or JsonToken.Float)
				return Convert.ToInt16(reader.Value);

			var s = Convert.ToString(reader.Value)?.Trim();
			if (string.IsNullOrEmpty(s) || s.Contains('_') || s == "-" || s == ":")
				return null;
			if (s.Contains(':'))
				return HourMinToMin(s);
			return short.TryParse(s, out var n) ? n : null;
		}

		public override void WriteJson(JsonWriter writer, short? value, JsonSerializer serializer)
		{
			if (value == null)
				writer.WriteNull();
			else
				writer.WriteValue(value.Value);
		}

		public static short? HourMinToMin(string? input)
		{
			if (string.IsNullOrWhiteSpace(input) || input.Contains('_'))
				return null;
			var arr = input.Trim().Split(':');
			var hour = arr.Length > 0 && int.TryParse(arr[0], out var h) ? h : 0;
			var min = arr.Length > 1 && int.TryParse(arr[1], out var m) ? m : 0;
			return (short)(hour * 60 + min);
		}

		public static string MinToHourMin(short? input, bool zeroPadding = true, int hourLength = 3)
		{
			if (input == null)
				return "";
			var value = input.Value;
			var isMinus = value < 0;
			value = Math.Abs(value);
			var hour = value / 60;
			var min = value % 60;
			var result = zeroPadding
				? hour.ToString().PadLeft(hourLength, '0') + ":" + min.ToString().PadLeft(2, '0')
				: hour + ":" + min.ToString().PadLeft(2, '0');
			return isMinus ? "-" + result : result;
		}
	}
}
