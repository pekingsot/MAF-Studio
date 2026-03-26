namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("system_configs")]
public class SystemConfig
{
    [Dapper.Contrib.Extensions.Key]
    public Guid Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string? Value { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
