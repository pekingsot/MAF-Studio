using MAFStudio.Core.Utils;

namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("llm_model_configs")]
public class LlmModelConfig
{
    [Dapper.Contrib.Extensions.Key]
    public long Id { get; set; }

    public long LlmConfigId { get; set; }

    public string ModelName { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public string? Description { get; set; }

    public bool IsDefault { get; set; } = false;

    public string? ExtraConfig { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 生成新的雪花ID
    /// </summary>
    public void GenerateId()
    {
        Id = SnowflakeIdGenerator.Instance.NextId();
    }
}
