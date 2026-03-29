namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("system_configs")]
public class SystemConfig : BaseEntityWithUpdate
{
    [Dapper.Contrib.Extensions.Key]
    public long Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string? Value { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
