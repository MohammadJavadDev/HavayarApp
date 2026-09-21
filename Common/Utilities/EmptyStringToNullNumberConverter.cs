using Newtonsoft.Json;

namespace Common.Utilities
{
	public class EmptyStringToNullByteConverter : JsonConverter<byte?>
	{
		public override byte? ReadJson(JsonReader reader, Type objectType, byte? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
				return null;
			if (reader.TokenType is JsonToken.Integer or JsonToken.Float)
				return Convert.ToByte(reader.Value);
			var s = Convert.ToString(reader.Value)?.Trim();
			if (string.IsNullOrEmpty(s) || s.Contains('_') || s == "-")
				return null;
			return byte.TryParse(s, out var n) ? n : null;
		}

		public override void WriteJson(JsonWriter writer, byte? value, JsonSerializer serializer)
		{
			if (value == null) writer.WriteNull();
			else writer.WriteValue(value.Value);
		}
	}
}
