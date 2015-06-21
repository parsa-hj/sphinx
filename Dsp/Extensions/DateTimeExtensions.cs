﻿namespace Dsp.Extensions
{
    using System;
    using System.Globalization;

    public static class DateTimeExtensions
    {
        public static int GetWeekOfYear(DateTime time)
        {
            return CultureInfo.GetCultureInfo("en-US").Calendar.GetWeekOfYear(time, CalendarWeekRule.FirstDay, DayOfWeek.Sunday);
        }

        public static DateTime FirstDateOfWeek(int year, int weekOfYear, CultureInfo ci)
        {
            var jan1 = new DateTime(year, 1, 1);
            var daysOffset = (int)ci.DateTimeFormat.FirstDayOfWeek - (int)jan1.DayOfWeek;
            var firstWeekDay = jan1.AddDays(daysOffset);
            var firstWeek = ci.Calendar.GetWeekOfYear(jan1, ci.DateTimeFormat.CalendarWeekRule, ci.DateTimeFormat.FirstDayOfWeek);
            if (firstWeek <= 1 || firstWeek > 50)
            {
                weekOfYear -= 1;
            }
            return firstWeekDay.AddDays(weekOfYear * 7);
        }

        public static DateTime LastDateOfWeek(int year, int weekOfYear, CultureInfo ci)
        {
            return FirstDateOfWeek(year, weekOfYear, ci).AddDays(6);
        }
    }
}