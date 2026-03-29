namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("system_logs")]
public class SystemLog
{
    [Dapper.Contrib.Extensions.Key]
    public long Id { get; set; }

    public string Level { get; set; } = "Info";

    public string Category { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? Exception { get; set; }

    public string? StackTrace { get; set; }

    public long? UserId { get; set; }

    public string? RequestPath { get; set; }

    public string? AdditionalData { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
