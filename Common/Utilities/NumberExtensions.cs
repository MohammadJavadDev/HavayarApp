using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Utilities
{
    public static class NumberExtensions
	{

		public static string MinutesToTimeFormat(this int? totalMinutes)
		{
			if (totalMinutes == null)
			{
				return "0:00";
			}
			int hours = totalMinutes.Value / 60;
			int minutes = totalMinutes.Value % 60;

			// خروجی به فرمت H:MM
			return $"{hours}:{minutes:D2}";
		}

		public static string MinutesToTimeFormat(this long? totalMinutes)
		{
			long hours = totalMinutes.Value / 60;
			long minutes = totalMinutes.Value % 60;

			return $"{hours}:{minutes:D2}";
		}
	}
}
