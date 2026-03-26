namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("llm_configs")]
public class LlmConfig
{
    [Dapper.Contrib.Extensions.Key]
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;

    public string? ApiKey { get; set; }

    public string? Endpoint { get; set; }

    public string? DefaultModel { get; set; }

    public string? ExtraConfig { get; set; }

    public string UserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
