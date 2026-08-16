using System.Globalization;

public static class DateHelper
{
	public static DateTime ToMiladiDateTimeWithSplitData(this string persianDate)
	{
		if (string.IsNullOrEmpty(persianDate))
			throw new ArgumentException("تاریخ وارد شده معتبر نیست");

		persianDate = persianDate.Trim();

		if (persianDate.Contains(' '))
		{
			persianDate = persianDate.Split(' ')[0];
		}

		persianDate = ConvertPersianToEnglishNumbers(persianDate);

		var separator = persianDate.Contains('/') ? '/' : '-';
		var dateParts = persianDate.Split(separator);

		if (dateParts.Length != 3)
			throw new ArgumentException("فرمت تاریخ باید سال/ماه/روز باشد");

		var year = int.Parse(dateParts[0]);
		var month = int.Parse(dateParts[1]);
		var day = int.Parse(dateParts[2]);

		if (year < 1300 || year > 1500)
			throw new ArgumentException("سال باید بین 1300 تا 1500 باشد");

		if (month < 1 || month > 12)
			throw new ArgumentException("ماه باید بین 1 تا 12 باشد");

		if (day < 1 || day > 31)
			throw new ArgumentException("روز باید بین 1 تا 31 باشد");

		var persianCalendar = new PersianCalendar();
		var miladiDate = persianCalendar.ToDateTime(year, month, day, 0, 0, 0, 0);

		return miladiDate;
	}

	private static string ConvertPersianToEnglishNumbers(string input)
	{
		if (string.IsNullOrEmpty(input))
			return input;

		var persianNumbers = new Dictionary<char, char>
	   {
		  {'۰', '0'}, {'۱', '1'}, {'۲', '2'}, {'۳', '3'}, {'۴', '4'},
		  {'۵', '5'}, {'۶', '6'}, {'۷', '7'}, {'۸', '8'}, {'۹', '9'}
	   };

		var result = new char[input.Length];
		for (int i = 0; i < input.Length; i++)
		{
			result[i] = persianNumbers.TryGetValue(input[i], out var englishDigit)
			    ? englishDigit
			    : input[i];
		}

		return new string(result);
	}
}
