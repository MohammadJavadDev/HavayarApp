using System.Text;
using System.Text.RegularExpressions;

namespace Services.Sms
{
	public static class SmsHelpers
	{
		private static readonly Regex ValidMobile = new(@"^09\d{9}$", RegexOptions.Compiled);
		private static readonly char[] Separators = [';', ',', '،', '\n', '\r'];

		/// <summary>تبدیل ارقام فارسی/عربی به لاتین.</summary>
		public static string ToLatinDigits(string? input)
		{
			if (string.IsNullOrEmpty(input))
				return string.Empty;

			var sb = new StringBuilder(input.Length);
			foreach (var c in input)
			{
				sb.Append(c switch
				{
					>= '\u06F0' and <= '\u06F9' => (char)('0' + (c - '\u06F0')),
					>= '\u0660' and <= '\u0669' => (char)('0' + (c - '\u0660')),
					_ => c
				});
			}
			return sb.ToString();
		}

		/// <summary>
		/// نرمال‌سازی شماره موبایل ایران به قالب 09xxxxxxxxx.
		/// خروجی null یعنی شماره قابل تشخیص نیست (در این حالت <paramref name="isValid"/> هم false است).
		/// </summary>
		public static string Normalize(string? raw, out bool isValid)
		{
			var digits = new string(ToLatinDigits(raw).Where(char.IsDigit).ToArray());

			if (digits.StartsWith("0098")) digits = "0" + digits[4..];
			else if (digits.StartsWith("98") && digits.Length == 12) digits = "0" + digits[2..];
			else if (digits.Length == 10 && digits.StartsWith('9')) digits = "0" + digits;

			isValid = ValidMobile.IsMatch(digits);
			return isValid ? digits : (raw ?? string.Empty).Trim();
		}

		/// <summary>جداکردن رشته چند گیرنده (جداکننده ; , - فاصله ...) و نرمال‌سازی هرکدام.</summary>
		public static List<(string Mobile, bool IsValid)> SplitReceivers(string? receivers)
		{
			var result = new List<(string, bool)>();
			if (string.IsNullOrWhiteSpace(receivers))
				return result;

			var seen = new HashSet<string>(StringComparer.Ordinal);
			foreach (var part in ExpandParts(ToLatinDigits(receivers)))
			{
				var normalized = Normalize(part, out var ok);
				if (string.IsNullOrEmpty(normalized) || !seen.Add(normalized))
					continue;
				result.Add((normalized, ok));
			}
			return result;
		}

		/// <summary>
		/// جداکننده‌ها ; , ، و خط جدید هستند. «-» فقط وقتی جداکننده است که هر تکه خودش یک شماره کامل (حداقل ۱۰ رقم) باشد
		/// (سیستم قبلی «-» را هم جداکننده می‌گرفت، ولی 0912-123-4567 یک شماره است).
		/// </summary>
		private static IEnumerable<string> ExpandParts(string input)
		{
			foreach (var part in input.Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			{
				if (part.Contains('-'))
				{
					var pieces = part.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
					if (pieces.Length > 1 && pieces.All(p => p.Count(char.IsDigit) >= 10))
					{
						foreach (var piece in pieces)
							yield return piece;
						continue;
					}
				}
				yield return part;
			}
		}

		/// <summary>تعداد بخش‌های پیامک (فارسی: ۷۰/۶۷ نویسه، لاتین: ۱۶۰/۱۵۳).</summary>
		public static int CountSegments(string? text)
		{
			if (string.IsNullOrEmpty(text))
				return 1;

			var isUnicode = text.Any(c => c > 127);
			var single = isUnicode ? 70 : 160;
			var multi = isUnicode ? 67 : 153;
			return text.Length <= single ? 1 : (int)Math.Ceiling(text.Length / (double)multi);
		}
	}
}
