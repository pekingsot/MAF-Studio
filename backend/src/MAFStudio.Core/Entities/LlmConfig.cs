using MAFStudio.Core.Utils;

namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("llm_configs")]
public class LlmConfig : BaseEntityWithUpdate
{
    [Dapper.Contrib.Extensions.Key]
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;

    public string? ApiKey { get; set; }

    public string? Endpoint { get; set; }

    public string? DefaultModel { get; set; }

    public string UserId { get; set; } = string.Empty;

    public bool IsDefault { get; set; } = false;

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<LlmModelConfig>? Models { get; set; }

    public void GenerateId()
    {
        Id = SnowflakeIdGenerator.Instance.NextId();
    }
}
