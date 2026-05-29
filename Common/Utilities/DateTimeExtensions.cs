using Common.Entities.EntityMetadatas;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Utilities
{
    public static class DateTimeExtensions
    {
		private static readonly PersianCalendar Pc = new PersianCalendar();

		public static List<PropertyMetadataOption> MonthsList = new List<PropertyMetadataOption>
	   {
		  new PropertyMetadataOption
		  {
			 Value = 1,
			 Text = "فروردین",
			 ExteraData = "01/01-01/31"
		  },
		  new PropertyMetadataOption
		  {
			 Value = 2,
			 Text = "اردیبهشت",
			 ExteraData = "02/01-02/31"
		  },
		  new PropertyMetadataOption
		  {
			 Value = 3,
			 Text = "خرداد",
			 ExteraData = "03/01-03/31"
		  },
		  new PropertyMetadataOption
		  {
			 Value = 4,
			 Text = "تیر",
				ExteraData = "04/01-04/31"
		  },
		  new PropertyMetadataOption
		  {
			  Value = 5,
			 Text = "مرداد",
			 ExteraData = "05/01-05/31"
		  },
		  new PropertyMetadataOption
		  {
				Value = 6,
			 Text = "شهریور",
			 ExteraData = "06/01-06/31"
		  },
		  new PropertyMetadataOption
		  {
			 Value = 7,
			 Text = "مهر",
			 ExteraData = "07/01-07/30"
		  },
		  new PropertyMetadataOption
		  {
			 Value = 8,
			 Text = "آبان",
			 ExteraData = "08/01-08/30"
		  },
		  new PropertyMetadataOption
		  {
			 Value = 9,
			 Text = "آذر",
			 ExteraData = "09/01-09/30"
		  },
		  new PropertyMetadataOption
		  {
			 Value = 10,
			 Text = "دی",
			 ExteraData = "10/01-10/30"
		  },
		  new PropertyMetadataOption
		  {
			 Value = 11,
			 Text = "بهمن",
			 ExteraData = "11/01-11/30"
		  },
		  new PropertyMetadataOption
		  {
			 Value = 12,
			 Text = "اسفند",
			 ExteraData = "12/01-12/29"
		  },
	   };
		public static string ToShamsiDate(this DateTime dateTime)
        {
          

            int year = Pc.GetYear(dateTime);
            int month = Pc.GetMonth(dateTime);
            int day = Pc.GetDayOfMonth(dateTime);

            return $"{year}/{month.ToString("D2")}/{day.ToString("D2")}";
        }

        public static string ToShamsiDateTime(this DateTime dateTime)
        {
         

            int year = Pc.GetYear(dateTime);
            int month = Pc.GetMonth(dateTime);
            int day = Pc.GetDayOfMonth(dateTime);
            int hour = dateTime.Hour;
            int minute = dateTime.Minute;
            int second = dateTime.Second;

            return $"{year}/{month.ToString("D2")}/{day.ToString("D2")} {hour.ToString("D2")}:{minute.ToString("D2")}:{second.ToString("D2")}";
        }

        public static DateTime ToMiladiDate(this string shamsiDateTime)
        {
            if (IsMiladiDateTime(shamsiDateTime))
                return DateTime.Parse(shamsiDateTime);

            shamsiDateTime =   shamsiDateTime.Replace("/", "").Replace(":","").Replace(" ","");

          

            var year = shamsiDateTime.Substring(0, 4).Fa2En().ToInt();
            var mount = shamsiDateTime.Substring(4, 2).Fa2En().ToInt();
            var day = shamsiDateTime.Substring(6, 2).Fa2En().ToInt();

            DateTime miladiDateTime  = Pc.ToDateTime(year, mount, day, 0, 0, 0, 0);
        
           
            return miladiDateTime;
        }
        public static DateTime ToMiladiDateTime(this string shamsiDateTime)
        {
            if (IsMiladiDateTime(shamsiDateTime))
                return DateTime.Parse(shamsiDateTime);

            shamsiDateTime =   shamsiDateTime.Replace("/", "").Replace(":","").Replace(" ","");

       

            var year = shamsiDateTime.Substring(0, 4).Fa2En().ToInt();
            var mount = shamsiDateTime.Substring(4, 2).Fa2En().ToInt();
            var day = shamsiDateTime.Substring(6, 2).Fa2En().ToInt();

            var hour = shamsiDateTime.Substring(8, 2).Fa2En().ToInt();
            var min = shamsiDateTime.Substring(10, 2).Fa2En().ToInt();
            var sec = shamsiDateTime.Substring(12, 2).Fa2En().ToInt();
        
            DateTime miladiDateTime  = Pc.ToDateTime(year, mount, day, hour, min, sec, 0);
        
           
            return miladiDateTime;
        }
        public static bool IsMiladiDateTime(this string dateTimeString)
        {
            return DateTime.TryParse(dateTimeString, out _);
        }

		public static int GetShamsiYear(this DateTime date)
		{
			return Pc.GetYear(date);
		}
	}
}
