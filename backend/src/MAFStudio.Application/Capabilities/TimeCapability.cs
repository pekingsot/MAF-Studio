using System.Reflection;

namespace MAFStudio.Application.Capabilities;

public class TimeCapability : ICapability
{
    public string Name => "时间操作";
    public string Description => "提供获取当前时间、日期、时间戳等操作能力";

    public IEnumerable<MethodInfo> GetTools()
    {
        return typeof(TimeCapability).GetMethods()
            .Where(m => m.GetCustomAttribute<ToolAttribute>() != null);
    }

    [Tool("获取当前日期时间")]
    public string GetCurrentDateTime(string? format = null)
    {
        try
        {
            var now = DateTime.Now;
            if (string.IsNullOrEmpty(format))
            {
                return $"当前日期时间：{now:yyyy-MM-dd HH:mm:ss}";
            }
            return $"当前日期时间：{now.ToString(format)}";
        }
        catch (Exception ex)
        {
            return $"获取当前日期时间失败：{ex.Message}";
        }
    }

    [Tool("获取当前日期")]
    public string GetCurrentDate(string? format = null)
    {
        try
        {
            var today = DateTime.Today;
            if (string.IsNullOrEmpty(format))
            {
                return $"当前日期：{today:yyyy-MM-dd}";
            }
            return $"当前日期：{today.ToString(format)}";
        }
        catch (Exception ex)
        {
            return $"获取当前日期失败：{ex.Message}";
        }
    }

    [Tool("获取当前时间")]
    public string GetCurrentTime(string? format = null)
    {
        try
        {
            var now = DateTime.Now;
            if (string.IsNullOrEmpty(format))
            {
                return $"当前时间：{now:HH:mm:ss}";
            }
            return $"当前时间：{now.ToString(format)}";
        }
        catch (Exception ex)
        {
            return $"获取当前时间失败：{ex.Message}";
        }
    }

    [Tool("获取当前时间戳")]
    public string GetCurrentTimestamp()
    {
        try
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var timestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return $"当前时间戳（秒）：{timestamp}\n当前时间戳（毫秒）：{timestampMs}";
        }
        catch (Exception ex)
        {
            return $"获取当前时间戳失败：{ex.Message}";
        }
    }

    [Tool("获取时区信息")]
    public string GetTimeZoneInfo()
    {
        try
        {
            var timeZone = TimeZoneInfo.Local;
            var utcNow = DateTime.UtcNow;
            var localNow = DateTime.Now;
            var offset = timeZone.GetUtcOffset(localNow);

            return $"时区信息：\n" +
                   $"  时区名称：{timeZone.DisplayName}\n" +
                   $"  标准名称：{timeZone.StandardName}\n" +
                   $"  夏令时名称：{timeZone.DaylightName}\n" +
                   $"  UTC偏移：{offset}\n" +
                   $"  当前UTC时间：{utcNow:yyyy-MM-dd HH:mm:ss}\n" +
                   $"  当前本地时间：{localNow:yyyy-MM-dd HH:mm:ss}";
        }
        catch (Exception ex)
        {
            return $"获取时区信息失败：{ex.Message}";
        }
    }

    [Tool("时间戳转日期时间")]
    public string TimestampToDateTime(long timestamp, string? format = null)
    {
        try
        {
            var dateTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime;
            if (string.IsNullOrEmpty(format))
            {
                return $"时间戳 {timestamp} 对应的日期时间：{dateTime:yyyy-MM-dd HH:mm:ss}";
            }
            return $"时间戳 {timestamp} 对应的日期时间：{dateTime.ToString(format)}";
        }
        catch (Exception ex)
        {
            return $"时间戳转日期时间失败：{ex.Message}";
        }
    }

    [Tool("日期时间转时间戳")]
    public string DateTimeToTimestamp(string dateTimeStr)
    {
        try
        {
            if (DateTime.TryParse(dateTimeStr, out var dateTime))
            {
                var timestamp = new DateTimeOffset(dateTime).ToUnixTimeSeconds();
                return $"日期时间 {dateTimeStr} 对应的时间戳：{timestamp}";
            }
            return $"无法解析日期时间：{dateTimeStr}";
        }
        catch (Exception ex)
        {
            return $"日期时间转时间戳失败：{ex.Message}";
        }
    }

    [Tool("计算时间差")]
    public string CalculateTimeDifference(string startTime, string endTime)
    {
        try
        {
            if (DateTime.TryParse(startTime, out var start) && DateTime.TryParse(endTime, out var end))
            {
                var diff = end - start;
                return $"时间差：\n" +
                       $"  开始时间：{start:yyyy-MM-dd HH:mm:ss}\n" +
                       $"  结束时间：{end:yyyy-MM-dd HH:mm:ss}\n" +
                       $"  相差天数：{diff.TotalDays:F2}\n" +
                       $"  相差小时：{diff.TotalHours:F2}\n" +
                       $"  相差分钟：{diff.TotalMinutes:F2}\n" +
                       $"  相差秒数：{diff.TotalSeconds:F2}";
            }
            return $"无法解析日期时间";
        }
        catch (Exception ex)
        {
            return $"计算时间差失败：{ex.Message}";
        }
    }
}
