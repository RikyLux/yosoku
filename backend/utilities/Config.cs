using System;

public static class Config
{
    public static string DateToKey(DateTime date)
    {
        return date.ToString("yyyy-MM-dd");
    }

    public static DateTime UTCDate(DateTime date)
    {
        return date.ToUniversalTime().Date;
    }

    public static DateTime Formatted(DateTime date)
    {
        return new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
    }

    public static DateTime JustMonth(DateTime date)
    {
        return new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    }
}