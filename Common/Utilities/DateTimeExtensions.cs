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
        public static string ToShamsiDate(this DateTime dateTime)
        {
            PersianCalendar persianCalendar = new PersianCalendar();

            int year = persianCalendar.GetYear(dateTime);
            int month = persianCalendar.GetMonth(dateTime);
            int day = persianCalendar.GetDayOfMonth(dateTime);

            return $"{year}/{month.ToString("D2")}/{day.ToString("D2")}";
        }

        public static string ToShamsiDateTime(this DateTime dateTime)
        {
            PersianCalendar persianCalendar = new PersianCalendar();

            int year = persianCalendar.GetYear(dateTime);
            int month = persianCalendar.GetMonth(dateTime);
            int day = persianCalendar.GetDayOfMonth(dateTime);
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

            PersianCalendar pc = new PersianCalendar();

            var year = shamsiDateTime.Substring(0, 4).Fa2En().ToInt();
            var mount = shamsiDateTime.Substring(4, 2).Fa2En().ToInt();
            var day = shamsiDateTime.Substring(6, 2).Fa2En().ToInt();

            DateTime miladiDateTime  = pc.ToDateTime(year, mount, day, 0, 0, 0, 0);
        
           
            return miladiDateTime;
        }
        public static DateTime ToMiladiDateTime(this string shamsiDateTime)
        {
            if (IsMiladiDateTime(shamsiDateTime))
                return DateTime.Parse(shamsiDateTime);

            shamsiDateTime =   shamsiDateTime.Replace("/", "").Replace(":","").Replace(" ","");

            PersianCalendar pc = new PersianCalendar();

            var year = shamsiDateTime.Substring(0, 4).Fa2En().ToInt();
            var mount = shamsiDateTime.Substring(4, 2).Fa2En().ToInt();
            var day = shamsiDateTime.Substring(6, 2).Fa2En().ToInt();

            var hour = shamsiDateTime.Substring(8, 2).Fa2En().ToInt();
            var min = shamsiDateTime.Substring(10, 2).Fa2En().ToInt();
            var sec = shamsiDateTime.Substring(12, 2).Fa2En().ToInt();
        
            DateTime miladiDateTime  = pc.ToDateTime(year, mount, day, hour, min, sec, 0);
        
           
            return miladiDateTime;
        }
        public static bool IsMiladiDateTime(this string dateTimeString)
        {
            return DateTime.TryParse(dateTimeString, out _);
        }
    }
}
