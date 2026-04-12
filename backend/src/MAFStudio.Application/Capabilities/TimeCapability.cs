using System.ComponentModel;
using System.Reflection;

namespace MAFStudio.Application.Capabilities;

public class TimeCapability : ICapability
{
    public string Name => "Time";
    public string Description => "Get current date/time, timestamps, and perform time calculations";

    public IEnumerable<MethodInfo> GetTools()
    {
        return typeof(TimeCapability).GetMethods()
            .Where(m => m.GetCustomAttribute<ToolAttribute>() != null);
    }

    [Tool("Get the current date and time.")]
    public string GetCurrentDateTime(
        [Description("Custom format string, e.g. 'yyyy-MM-dd HH:mm:ss'. Default 'yyyy-MM-dd HH:mm:ss'")] string? format = null)
    {
        try
        {
            var now = DateTime.Now;
            if (string.IsNullOrEmpty(format))
            {
                return $"Current date and time: {now:yyyy-MM-dd HH:mm:ss}";
            }
            return $"Current date and time: {now.ToString(format)}";
        }
        catch (Exception ex)
        {
            return $"Failed to get current date time: {ex.Message}";
        }
    }

    [Tool("Get the current date.")]
    public string GetCurrentDate(
        [Description("Custom format string, e.g. 'yyyy-MM-dd'. Default 'yyyy-MM-dd'")] string? format = null)
    {
        try
        {
            var today = DateTime.Today;
            if (string.IsNullOrEmpty(format))
            {
                return $"Current date: {today:yyyy-MM-dd}";
            }
            return $"Current date: {today.ToString(format)}";
        }
        catch (Exception ex)
        {
            return $"Failed to get current date: {ex.Message}";
        }
    }

    [Tool("Get the current Unix timestamp in both seconds and milliseconds.")]
    public string GetCurrentTimestamp()
    {
        try
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var timestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return $"Current timestamp (seconds): {timestamp}\nCurrent timestamp (milliseconds): {timestampMs}";
        }
        catch (Exception ex)
        {
            return $"Failed to get timestamp: {ex.Message}";
        }
    }

    [Tool("Convert a Unix timestamp to a human-readable date and time.")]
    public string TimestampToDateTime(
        [Description("Unix timestamp in seconds, e.g. 1705312200")] long timestamp,
        [Description("Custom format string for the output. Default 'yyyy-MM-dd HH:mm:ss'")] string? format = null)
    {
        try
        {
            var dateTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime;
            if (string.IsNullOrEmpty(format))
            {
                return $"Timestamp {timestamp} corresponds to: {dateTime:yyyy-MM-dd HH:mm:ss}";
            }
            return $"Timestamp {timestamp} corresponds to: {dateTime.ToString(format)}";
        }
        catch (Exception ex)
        {
            return $"Failed to convert timestamp: {ex.Message}";
        }
    }

    [Tool("Convert a date and time string to a Unix timestamp.")]
    public string DateTimeToTimestamp(
        [Description("Date and time string, e.g. '2024-01-15 10:30:00' or '2024/01/15'")] string dateTimeStr)
    {
        try
        {
            if (DateTime.TryParse(dateTimeStr, out var dateTime))
            {
                var timestamp = new DateTimeOffset(dateTime).ToUnixTimeSeconds();
                return $"Date time '{dateTimeStr}' corresponds to timestamp: {timestamp}";
            }
            return $"Unable to parse date time: {dateTimeStr}";
        }
        catch (Exception ex)
        {
            return $"Failed to convert date time: {ex.Message}";
        }
    }

    [Tool("Calculate the time difference between two date/time strings. Returns the difference in days, hours, minutes and seconds.")]
    public string CalculateTimeDifference(
        [Description("Start date/time string, e.g. '2024-01-15 10:30:00'")] string startTime,
        [Description("End date/time string, e.g. '2024-01-20 15:45:00'")] string endTime)
    {
        try
        {
            if (DateTime.TryParse(startTime, out var start) && DateTime.TryParse(endTime, out var end))
            {
                var diff = end - start;
                return $"Time difference:\n" +
                       $"  Start: {start:yyyy-MM-dd HH:mm:ss}\n" +
                       $"  End: {end:yyyy-MM-dd HH:mm:ss}\n" +
                       $"  Days: {diff.TotalDays:F2}\n" +
                       $"  Hours: {diff.TotalHours:F2}\n" +
                       $"  Minutes: {diff.TotalMinutes:F2}\n" +
                       $"  Seconds: {diff.TotalSeconds:F2}";
            }
            return $"Unable to parse date time strings";
        }
        catch (Exception ex)
        {
            return $"Failed to calculate time difference: {ex.Message}";
        }
    }
}
