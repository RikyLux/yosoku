using System;
using System.Collections.Generic;
using System.Linq;

public static class TimeHelper
{
    public static List<DateTime> DaysInBetween(DateTime from, DateTime? to)
    {
        var myTo = to ?? DateTime.UtcNow;
        DateTime cleanTo = Config.Formatted(myTo);
        var daysCount = (int)Math.Ceiling(Math.Min(Math.Abs((from - myTo).TotalDays), 1));
        return Enumerable.Range(0, daysCount).Select((x, i) => cleanTo.AddDays(-i)).Reverse().ToList();
    }

    public static DateTime OneYearAgoStartMonth()
    {
        var yearAgo = DateTime.UtcNow.AddYears(-1);
        return new DateTime(yearAgo.Year, yearAgo.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    public static DateTime SixMonthStartMonth()
    {
        var sixMonth = DateTime.UtcNow.AddMonths(6);
        return new DateTime(sixMonth.Year, sixMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    }
}