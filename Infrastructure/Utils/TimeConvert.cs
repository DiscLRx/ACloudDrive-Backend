using System.Text;

namespace Infrastructure.Utils;

public class TimeConvert
{
    public enum Granularity
    {
        Day = 1,
        Hour = 2,
        Minute = 3,
        Second = 4,
        Millisecond = 5
    }

    private const long DayMs = 24 * 60 * 60 * 1000;
    private const long HourMs = 60 * 60 * 1000;
    private const long MinuteMs = 60 * 1000;
    private const long SecondMs = 1000;

    public static string TimeSpanToString(TimeSpan timeSpan, Granularity granularity) =>
        MilliecondsToString(Convert.ToInt64(timeSpan.TotalMilliseconds), granularity);

    public static string DaysToString(long days, Granularity granularity) =>
        MilliecondsToString(days * DayMs, granularity);

    public static string HoursToString(long hours, Granularity granularity) =>
        MilliecondsToString(hours * HourMs, granularity);

    public static string MinutesToString(long minutes, Granularity granularity) =>
        MilliecondsToString(minutes * MinuteMs, granularity);

    public static string SecondsToString(long seconds, Granularity granularity) =>
        MilliecondsToString(seconds * SecondMs, granularity);

    public static string MilliecondsToString(long milliseconds, Granularity granularity)
    {
        var ms = Convert.ToInt64(milliseconds);
        var day = ms / DayMs;
        ms %= DayMs;
        var hour = ms / HourMs;
        ms %= HourMs;
        var minute = ms / MinuteMs;
        ms %= MinuteMs;
        var second = ms / SecondMs;
        ms %= SecondMs;
        var sb = new StringBuilder();
        if (day != 0 || granularity == Granularity.Day)
        {
            sb.Append($"{day}天");
        }
        if (((hour != 0 || day != 0) && granularity > Granularity.Hour) || granularity == Granularity.Hour)
        {
            sb.Append($"{hour}小时");
        }
        if (((minute != 0 || hour != 0 || day != 0) && granularity > Granularity.Minute) || granularity == Granularity.Minute)
        {
            sb.Append($"{minute}分钟");
        }
        if (((second != 0 || minute != 0 || hour != 0 || day != 0) && granularity > Granularity.Second) || granularity == Granularity.Second)
        {
            sb.Append($"{second}秒");
        }
        if (((ms != 0 || second != 0 || minute != 0 || hour != 0 || day != 0) && granularity > Granularity.Millisecond) || granularity == Granularity.Millisecond)
        {
            sb.Append($"{ms}毫秒");
        }
        return sb.ToString();
    }
}